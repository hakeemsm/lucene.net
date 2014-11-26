/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Bloom;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using Sharpen;

namespace Lucene.Net.Codecs.Bloom
{
	/// <summary>
	/// <p>
	/// A
	/// <see cref="Lucene.Net.Codecs.PostingsFormat">Lucene.Net.Codecs.PostingsFormat
	/// 	</see>
	/// useful for low doc-frequency fields such as primary
	/// keys. Bloom filters are maintained in a ".blm" file which offers "fast-fail"
	/// for reads in segments known to have no record of the key. A choice of
	/// delegate PostingsFormat is used to record all other Postings data.
	/// </p>
	/// <p>
	/// A choice of
	/// <see cref="BloomFilterFactory">BloomFilterFactory</see>
	/// can be passed to tailor Bloom Filter
	/// settings on a per-field basis. The default configuration is
	/// <see cref="DefaultBloomFilterFactory">DefaultBloomFilterFactory</see>
	/// which allocates a ~8mb bitset and hashes
	/// values using
	/// <see cref="MurmurHash2">MurmurHash2</see>
	/// . This should be suitable for most purposes.
	/// </p>
	/// <p>
	/// The format of the blm file is as follows:
	/// </p>
	/// <ul>
	/// <li>BloomFilter (.blm) --&gt; Header, DelegatePostingsFormatName,
	/// NumFilteredFields, Filter<sup>NumFilteredFields</sup>, Footer</li>
	/// <li>Filter --&gt; FieldNumber, FuzzySet</li>
	/// <li>FuzzySet --&gt;See
	/// <see cref="FuzzySet.Serialize(Lucene.Net.Store.DataOutput)">FuzzySet.Serialize(Lucene.Net.Store.DataOutput)
	/// 	</see>
	/// </li>
	/// <li>Header --&gt;
	/// <see cref="Lucene.Net.Codecs.CodecUtil.WriteHeader(Lucene.Net.Store.DataOutput, string, int)
	/// 	">CodecHeader</see>
	/// </li>
	/// <li>DelegatePostingsFormatName --&gt;
	/// <see cref="Lucene.Net.Store.DataOutput.WriteString(string)">String</see>
	/// The name of a ServiceProvider registered
	/// <see cref="Lucene.Net.Codecs.PostingsFormat">Lucene.Net.Codecs.PostingsFormat
	/// 	</see>
	/// </li>
	/// <li>NumFilteredFields --&gt;
	/// <see cref="Lucene.Net.Store.DataOutput.WriteInt(int)">Uint32</see>
	/// </li>
	/// <li>FieldNumber --&gt;
	/// <see cref="Lucene.Net.Store.DataOutput.WriteInt(int)">Uint32</see>
	/// The number of the
	/// field in this segment</li>
	/// <li>Footer --&gt;
	/// <see cref="Lucene.Net.Codecs.CodecUtil.WriteFooter(Lucene.Net.Store.IndexOutput)
	/// 	">CodecFooter</see>
	/// </li>
	/// </ul>
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public sealed class BloomFilteringPostingsFormat : PostingsFormat
	{
		public static readonly string BLOOM_CODEC_NAME = "BloomFilter";

		public const int VERSION_START = 1;

		public const int VERSION_CHECKSUM = 2;

		public const int VERSION_CURRENT = VERSION_CHECKSUM;

		/// <summary>Extension of Bloom Filters file</summary>
		internal static readonly string BLOOM_EXTENSION = "blm";

		internal BloomFilterFactory bloomFilterFactory = new DefaultBloomFilterFactory();

		private PostingsFormat delegatePostingsFormat;

		/// <summary>Creates Bloom filters for a selection of fields created in the index.</summary>
		/// <remarks>
		/// Creates Bloom filters for a selection of fields created in the index. This
		/// is recorded as a set of Bitsets held as a segment summary in an additional
		/// "blm" file. This PostingsFormat delegates to a choice of delegate
		/// PostingsFormat for encoding all other postings data.
		/// </remarks>
		/// <param name="delegatePostingsFormat">
		/// The PostingsFormat that records all the non-bloom filter data i.e.
		/// postings info.
		/// </param>
		/// <param name="bloomFilterFactory">
		/// The
		/// <see cref="BloomFilterFactory">BloomFilterFactory</see>
		/// responsible for sizing BloomFilters
		/// appropriately
		/// </param>
		public BloomFilteringPostingsFormat(PostingsFormat delegatePostingsFormat, BloomFilterFactory
			 bloomFilterFactory) : base(BLOOM_CODEC_NAME)
		{
			this.delegatePostingsFormat = delegatePostingsFormat;
			this.bloomFilterFactory = bloomFilterFactory;
		}

		/// <summary>Creates Bloom filters for a selection of fields created in the index.</summary>
		/// <remarks>
		/// Creates Bloom filters for a selection of fields created in the index. This
		/// is recorded as a set of Bitsets held as a segment summary in an additional
		/// "blm" file. This PostingsFormat delegates to a choice of delegate
		/// PostingsFormat for encoding all other postings data. This choice of
		/// constructor defaults to the
		/// <see cref="DefaultBloomFilterFactory">DefaultBloomFilterFactory</see>
		/// for
		/// configuring per-field BloomFilters.
		/// </remarks>
		/// <param name="delegatePostingsFormat">
		/// The PostingsFormat that records all the non-bloom filter data i.e.
		/// postings info.
		/// </param>
		public BloomFilteringPostingsFormat(PostingsFormat delegatePostingsFormat) : this
			(delegatePostingsFormat, new DefaultBloomFilterFactory())
		{
		}

		public BloomFilteringPostingsFormat() : base(BLOOM_CODEC_NAME)
		{
		}

		// Used only by core Lucene at read-time via Service Provider instantiation -
		// do not use at Write-time in application code.
		/// <exception cref="System.IO.IOException"></exception>
		public override Lucene.Net.Codecs.FieldsConsumer FieldsConsumer(SegmentWriteState
			 state)
		{
			if (delegatePostingsFormat == null)
			{
				throw new NotSupportedException("Error - " + GetType().FullName + " has been constructed without a choice of PostingsFormat"
					);
			}
			return new BloomFilteringPostingsFormat.BloomFilteredFieldsConsumer(this, delegatePostingsFormat
				.FieldsConsumer(state), state, delegatePostingsFormat);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Lucene.Net.Codecs.FieldsProducer FieldsProducer(SegmentReadState
			 state)
		{
			return new BloomFilteringPostingsFormat.BloomFilteredFieldsProducer(this, state);
		}

		public class BloomFilteredFieldsProducer : FieldsProducer
		{
			private FieldsProducer delegateFieldsProducer;

			internal Dictionary<string, FuzzySet> bloomsByFieldName = new Dictionary<string, 
				FuzzySet>();

			/// <exception cref="System.IO.IOException"></exception>
			public BloomFilteredFieldsProducer(BloomFilteringPostingsFormat _enclosing, SegmentReadState
				 state)
			{
				this._enclosing = _enclosing;
				string bloomFileName = IndexFileNames.SegmentFileName(state.segmentInfo.name, state
					.segmentSuffix, BloomFilteringPostingsFormat.BLOOM_EXTENSION);
				ChecksumIndexInput bloomIn = null;
				bool success = false;
				try
				{
					bloomIn = state.directory.OpenChecksumInput(bloomFileName, state.context);
					int version = CodecUtil.CheckHeader(bloomIn, BloomFilteringPostingsFormat.BLOOM_CODEC_NAME
						, BloomFilteringPostingsFormat.VERSION_START, BloomFilteringPostingsFormat.VERSION_CURRENT
						);
					// // Load the hash function used in the BloomFilter
					// hashFunction = HashFunction.forName(bloomIn.readString());
					// Load the delegate postings format
					PostingsFormat delegatePostingsFormat = PostingsFormat.ForName(bloomIn.ReadString
						());
					this.delegateFieldsProducer = delegatePostingsFormat.FieldsProducer(state);
					int numBlooms = bloomIn.ReadInt();
					for (int i = 0; i < numBlooms; i++)
					{
						int fieldNum = bloomIn.ReadInt();
						FuzzySet bloom = FuzzySet.Deserialize(bloomIn);
						FieldInfo fieldInfo = state.fieldInfos.FieldInfo(fieldNum);
						this.bloomsByFieldName.Put(fieldInfo.name, bloom);
					}
					if (version >= BloomFilteringPostingsFormat.VERSION_CHECKSUM)
					{
						CodecUtil.CheckFooter(bloomIn);
					}
					else
					{
						CodecUtil.CheckEOF(bloomIn);
					}
					IOUtils.Close(bloomIn);
					success = true;
				}
				finally
				{
					if (!success)
					{
						IOUtils.CloseWhileHandlingException(bloomIn, this.delegateFieldsProducer);
					}
				}
			}

			public override Sharpen.Iterator<string> Iterator()
			{
				return this.delegateFieldsProducer.Iterator();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Close()
			{
				this.delegateFieldsProducer.Close();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override Lucene.Net.Index.Terms Terms(string field)
			{
				FuzzySet filter = this.bloomsByFieldName.Get(field);
				if (filter == null)
				{
					return this.delegateFieldsProducer.Terms(field);
				}
				else
				{
					Lucene.Net.Index.Terms result = this.delegateFieldsProducer.Terms(field);
					if (result == null)
					{
						return null;
					}
					return new BloomFilteringPostingsFormat.BloomFilteredFieldsProducer.BloomFilteredTerms
						(this, result, filter);
				}
			}

			public override int Size()
			{
				return this.delegateFieldsProducer.Size();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override long GetUniqueTermCount()
			{
				return this.delegateFieldsProducer.GetUniqueTermCount();
			}

			internal class BloomFilteredTerms : Terms
			{
				private Terms delegateTerms;

				private FuzzySet filter;

				public BloomFilteredTerms(BloomFilteredFieldsProducer _enclosing, Terms terms, FuzzySet
					 filter)
				{
					this._enclosing = _enclosing;
					this.delegateTerms = terms;
					this.filter = filter;
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override TermsEnum Intersect(CompiledAutomaton compiled, BytesRef startTerm
					)
				{
					return this.delegateTerms.Intersect(compiled, startTerm);
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override TermsEnum Iterator(TermsEnum reuse)
				{
					if ((reuse != null) && (reuse is BloomFilteringPostingsFormat.BloomFilteredFieldsProducer.BloomFilteredTermsEnum
						))
					{
						// recycle the existing BloomFilteredTermsEnum by asking the delegate
						// to recycle its contained TermsEnum
						BloomFilteringPostingsFormat.BloomFilteredFieldsProducer.BloomFilteredTermsEnum bfte
							 = (BloomFilteringPostingsFormat.BloomFilteredFieldsProducer.BloomFilteredTermsEnum
							)reuse;
						if (bfte.filter == this.filter)
						{
							bfte.Reset(this.delegateTerms, bfte.delegateTermsEnum);
							return bfte;
						}
					}
					// We have been handed something we cannot reuse (either null, wrong
					// class or wrong filter) so allocate a new object
					return new BloomFilteringPostingsFormat.BloomFilteredFieldsProducer.BloomFilteredTermsEnum
						(this, this.delegateTerms, reuse, this.filter);
				}

				public override IComparer<BytesRef> GetComparator()
				{
					return this.delegateTerms.GetComparator();
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override long Size()
				{
					return this.delegateTerms.Size();
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override long GetSumTotalTermFreq()
				{
					return this.delegateTerms.GetSumTotalTermFreq();
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override long GetSumDocFreq()
				{
					return this.delegateTerms.GetSumDocFreq();
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override int GetDocCount()
				{
					return this.delegateTerms.GetDocCount();
				}

				public override bool HasFreqs()
				{
					return this.delegateTerms.HasFreqs();
				}

				public override bool HasOffsets()
				{
					return this.delegateTerms.HasOffsets();
				}

				public override bool HasPositions()
				{
					return this.delegateTerms.HasPositions();
				}

				public override bool HasPayloads()
				{
					return this.delegateTerms.HasPayloads();
				}

				private readonly BloomFilteredFieldsProducer _enclosing;
			}

			internal sealed class BloomFilteredTermsEnum : TermsEnum
			{
				private Terms delegateTerms;

				private TermsEnum delegateTermsEnum;

				private TermsEnum reuseDelegate;

				private readonly FuzzySet filter;

				/// <exception cref="System.IO.IOException"></exception>
				public BloomFilteredTermsEnum(BloomFilteredFieldsProducer _enclosing, Terms delegateTerms
					, TermsEnum reuseDelegate, FuzzySet filter)
				{
					this._enclosing = _enclosing;
					this.delegateTerms = delegateTerms;
					this.reuseDelegate = reuseDelegate;
					this.filter = filter;
				}

				/// <exception cref="System.IO.IOException"></exception>
				internal void Reset(Terms delegateTerms, TermsEnum reuseDelegate)
				{
					this.delegateTerms = delegateTerms;
					this.reuseDelegate = reuseDelegate;
					this.delegateTermsEnum = null;
				}

				/// <exception cref="System.IO.IOException"></exception>
				private TermsEnum Delegate()
				{
					if (this.delegateTermsEnum == null)
					{
						this.delegateTermsEnum = this.delegateTerms.Iterator(this.reuseDelegate);
					}
					return this.delegateTermsEnum;
				}

				/// <exception cref="System.IO.IOException"></exception>
				public sealed override BytesRef Next()
				{
					return this.Delegate().Next();
				}

				public sealed override IComparer<BytesRef> GetComparator()
				{
					return this.delegateTerms.GetComparator();
				}

				/// <exception cref="System.IO.IOException"></exception>
				public sealed override bool SeekExact(BytesRef text)
				{
					// The magical fail-fast speed up that is the entire point of all of
					// this code - save a disk seek if there is a match on an in-memory
					// structure
					// that may occasionally give a false positive but guaranteed no false
					// negatives
					if (this.filter.Contains(text) == FuzzySet.ContainsResult.NO)
					{
						return false;
					}
					return this.Delegate().SeekExact(text);
				}

				/// <exception cref="System.IO.IOException"></exception>
				public sealed override TermsEnum.SeekStatus SeekCeil(BytesRef text)
				{
					return this.Delegate().SeekCeil(text);
				}

				/// <exception cref="System.IO.IOException"></exception>
				public sealed override void SeekExact(long ord)
				{
					this.Delegate().SeekExact(ord);
				}

				/// <exception cref="System.IO.IOException"></exception>
				public sealed override BytesRef Term()
				{
					return this.Delegate().Term();
				}

				/// <exception cref="System.IO.IOException"></exception>
				public sealed override long Ord()
				{
					return this.Delegate().Ord();
				}

				/// <exception cref="System.IO.IOException"></exception>
				public sealed override int DocFreq()
				{
					return this.Delegate().DocFreq();
				}

				/// <exception cref="System.IO.IOException"></exception>
				public sealed override long TotalTermFreq()
				{
					return this.Delegate().TotalTermFreq();
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override DocsAndPositionsEnum DocsAndPositions(Bits liveDocs, DocsAndPositionsEnum
					 reuse, int flags)
				{
					return this.Delegate().DocsAndPositions(liveDocs, reuse, flags);
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override DocsEnum Docs(Bits liveDocs, DocsEnum reuse, int flags)
				{
					return this.Delegate().Docs(liveDocs, reuse, flags);
				}

				private readonly BloomFilteredFieldsProducer _enclosing;
			}

			public override long RamBytesUsed()
			{
				long sizeInBytes = ((this.delegateFieldsProducer != null) ? this.delegateFieldsProducer
					.RamBytesUsed() : 0);
				foreach (KeyValuePair<string, FuzzySet> entry in this.bloomsByFieldName.EntrySet(
					))
				{
					sizeInBytes += entry.Key.Length * RamUsageEstimator.NUM_BYTES_CHAR;
					sizeInBytes += entry.Value.RamBytesUsed();
				}
				return sizeInBytes;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void CheckIntegrity()
			{
				this.delegateFieldsProducer.CheckIntegrity();
			}

			private readonly BloomFilteringPostingsFormat _enclosing;
		}

		internal class BloomFilteredFieldsConsumer : FieldsConsumer
		{
			private FieldsConsumer delegateFieldsConsumer;

			private IDictionary<FieldInfo, FuzzySet> bloomFilters = new Dictionary<FieldInfo, 
				FuzzySet>();

			private SegmentWriteState state;

			public BloomFilteredFieldsConsumer(BloomFilteringPostingsFormat _enclosing, FieldsConsumer
				 fieldsConsumer, SegmentWriteState state, PostingsFormat delegatePostingsFormat)
			{
				this._enclosing = _enclosing;
				this.delegateFieldsConsumer = fieldsConsumer;
				this.state = state;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override TermsConsumer AddField(FieldInfo field)
			{
				FuzzySet bloomFilter = this._enclosing.bloomFilterFactory.GetSetForField(this.state
					, field);
				if (bloomFilter != null)
				{
					//HM:revisit 
					//assert bloomFilters.containsKey(field) == false;
					this.bloomFilters.Put(field, bloomFilter);
					return new BloomFilteringPostingsFormat.WrappedTermsConsumer(this, this.delegateFieldsConsumer
						.AddField(field), bloomFilter);
				}
				else
				{
					// No, use the unfiltered fieldsConsumer - we are not interested in
					// recording any term Bitsets.
					return this.delegateFieldsConsumer.AddField(field);
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Close()
			{
				this.delegateFieldsConsumer.Close();
				// Now we are done accumulating values for these fields
				IList<KeyValuePair<FieldInfo, FuzzySet>> nonSaturatedBlooms = new AList<KeyValuePair
					<FieldInfo, FuzzySet>>();
				foreach (KeyValuePair<FieldInfo, FuzzySet> entry in this.bloomFilters.EntrySet())
				{
					FuzzySet bloomFilter = entry.Value;
					if (!this._enclosing.bloomFilterFactory.IsSaturated(bloomFilter, entry.Key))
					{
						nonSaturatedBlooms.AddItem(entry);
					}
				}
				string bloomFileName = IndexFileNames.SegmentFileName(this.state.segmentInfo.name
					, this.state.segmentSuffix, BloomFilteringPostingsFormat.BLOOM_EXTENSION);
				IndexOutput bloomOutput = null;
				try
				{
					bloomOutput = this.state.directory.CreateOutput(bloomFileName, this.state.context
						);
					CodecUtil.WriteHeader(bloomOutput, BloomFilteringPostingsFormat.BLOOM_CODEC_NAME, 
						BloomFilteringPostingsFormat.VERSION_CURRENT);
					// remember the name of the postings format we will delegate to
					bloomOutput.WriteString(this._enclosing.delegatePostingsFormat.GetName());
					// First field in the output file is the number of fields+blooms saved
					bloomOutput.WriteInt(nonSaturatedBlooms.Count);
					foreach (KeyValuePair<FieldInfo, FuzzySet> entry_1 in nonSaturatedBlooms)
					{
						FieldInfo fieldInfo = entry_1.Key;
						FuzzySet bloomFilter = entry_1.Value;
						bloomOutput.WriteInt(fieldInfo.number);
						this.SaveAppropriatelySizedBloomFilter(bloomOutput, bloomFilter, fieldInfo);
					}
					CodecUtil.WriteFooter(bloomOutput);
				}
				finally
				{
					IOUtils.Close(bloomOutput);
				}
				//We are done with large bitsets so no need to keep them hanging around
				this.bloomFilters.Clear();
			}

			/// <exception cref="System.IO.IOException"></exception>
			private void SaveAppropriatelySizedBloomFilter(IndexOutput bloomOutput, FuzzySet 
				bloomFilter, FieldInfo fieldInfo)
			{
				FuzzySet rightSizedSet = this._enclosing.bloomFilterFactory.Downsize(fieldInfo, bloomFilter
					);
				if (rightSizedSet == null)
				{
					rightSizedSet = bloomFilter;
				}
				rightSizedSet.Serialize(bloomOutput);
			}

			private readonly BloomFilteringPostingsFormat _enclosing;
		}

		internal class WrappedTermsConsumer : TermsConsumer
		{
			private TermsConsumer delegateTermsConsumer;

			private FuzzySet bloomFilter;

			public WrappedTermsConsumer(BloomFilteringPostingsFormat _enclosing, TermsConsumer
				 termsConsumer, FuzzySet bloomFilter)
			{
				this._enclosing = _enclosing;
				this.delegateTermsConsumer = termsConsumer;
				this.bloomFilter = bloomFilter;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override PostingsConsumer StartTerm(BytesRef text)
			{
				return this.delegateTermsConsumer.StartTerm(text);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void FinishTerm(BytesRef text, TermStats stats)
			{
				// Record this term in our BloomFilter
				if (stats.docFreq > 0)
				{
					this.bloomFilter.AddValue(text);
				}
				this.delegateTermsConsumer.FinishTerm(text, stats);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Finish(long sumTotalTermFreq, long sumDocFreq, int docCount)
			{
				this.delegateTermsConsumer.Finish(sumTotalTermFreq, sumDocFreq, docCount);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override IComparer<BytesRef> GetComparator()
			{
				return this.delegateTermsConsumer.GetComparator();
			}

			private readonly BloomFilteringPostingsFormat _enclosing;
		}
	}
}
