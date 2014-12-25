using System;
using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;

namespace Lucene.Net.Codecs.Blockterms
{
	/// <summary>
	/// Writes terms dict, block-encoding (column stride) each
	/// term's metadata for each set of terms between two
	/// index terms.
	/// </summary>
	/// <remarks>
	/// Writes terms dict, block-encoding (column stride) each
	/// term's metadata for each set of terms between two
	/// index terms.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public class BlockTermsWriter : FieldsConsumer
	{
		internal static readonly string CODEC_NAME = "BLOCK_TERMS_DICT";

		public const int VERSION_START = 0;

		public const int VERSION_APPEND_ONLY = 1;

		public const int VERSION_META_ARRAY = 2;

		public const int VERSION_CHECKSUM = 3;

		public const int VERSION_CURRENT = VERSION_CHECKSUM;

		/// <summary>Extension of terms file</summary>
		internal static readonly string TERMS_EXTENSION = "tib";

		protected internal IndexOutput indexOut;

		internal readonly PostingsWriterBase postingsWriter;

		internal readonly FieldInfos fieldInfos;

		internal FieldInfo currentField;

		private readonly TermsIndexWriterBase termsIndexWriter;

		private class FieldMetaData
		{
			public readonly FieldInfo fieldInfo;

			public readonly long numTerms;

			public readonly long termsStartPointer;

			public readonly long sumTotalTermFreq;

			public readonly long sumDocFreq;

			public readonly int docCount;

			public readonly int longsSize;

			public FieldMetaData(FieldInfo fieldInfo, long numTerms, long termsStartPointer, 
				long sumTotalTermFreq, long sumDocFreq, int docCount, int longsSize)
			{
				// TODO: currently we encode all terms between two indexed
				// terms as a block; but, we could decouple the two, ie
				// allow several blocks in between two indexed terms
				// Initial format
				//HM:revisit 
				//
				//HM:revisit 
				//assert numTerms > 0;
				this.fieldInfo = fieldInfo;
				this.termsStartPointer = termsStartPointer;
				this.numTerms = numTerms;
				this.sumTotalTermFreq = sumTotalTermFreq;
				this.sumDocFreq = sumDocFreq;
				this.docCount = docCount;
				this.longsSize = longsSize;
			}
		}

		private readonly IList<FieldMetaData> fields = new List<FieldMetaData>();

		/// <exception cref="System.IO.IOException"></exception>
		public BlockTermsWriter(TermsIndexWriterBase termsIndexWriter, SegmentWriteState 
			state, PostingsWriterBase postingsWriter)
		{
			// private final String segment;
			string termsFileName = IndexFileNames.SegmentFileName(state.segmentInfo.name, state
				.segmentSuffix, TERMS_EXTENSION);
			this.termsIndexWriter = termsIndexWriter;
			indexOut = state.directory.CreateOutput(termsFileName, state.context);
			bool success = false;
			try
			{
				fieldInfos = state.fieldInfos;
				WriteHeader(indexOut);
				currentField = null;
				this.postingsWriter = postingsWriter;
				// segment = state.segmentName;
				//System.out.println("BTW.init seg=" + state.segmentName);
				postingsWriter.Init(indexOut);
				// have consumer write its format/header
				success = true;
			}
			finally
			{
				if (!success)
				{
					IOUtils.CloseWhileHandlingException((IDisposable)indexOut);
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void WriteHeader(IndexOutput @out)
		{
			CodecUtil.WriteHeader(@out, CODEC_NAME, VERSION_CURRENT);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override TermsConsumer AddField(FieldInfo field)
		{
			//System.out.println("\nBTW.addField seg=" + segment + " field=" + field.name);
			//HM:revisit 
			//
			//HM:revisit 
			//assert currentField == null || currentField.name.compareTo(field.name) < 0;
			currentField = field;
			TermsIndexWriterBase.FieldWriter fieldIndexWriter = termsIndexWriter.AddField(field
				, indexOut.FilePointer);
			return new BlockTermsWriter.TermsWriter(this, fieldIndexWriter, field, postingsWriter
				);
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected override void Dispose(bool disposing)
		{
			if (indexOut != null)
			{
				try
				{
					long dirStart = indexOut.FilePointer;
					indexOut.WriteVInt(fields.Count);
					foreach (BlockTermsWriter.FieldMetaData field in fields)
					{
						indexOut.WriteVInt(field.fieldInfo.number);
						indexOut.WriteVLong(field.numTerms);
						indexOut.WriteVLong(field.termsStartPointer);
						if (field.fieldInfo.IndexOptionsValue != FieldInfo.IndexOptions.DOCS_ONLY)
						{
							indexOut.WriteVLong(field.sumTotalTermFreq);
						}
						indexOut.WriteVLong(field.sumDocFreq);
						indexOut.WriteVInt(field.docCount);
						if (VERSION_CURRENT >= VERSION_META_ARRAY)
						{
							indexOut.WriteVInt(field.longsSize);
						}
					}
					WriteTrailer(dirStart);
					CodecUtil.WriteFooter(indexOut);
				}
				finally
				{
					IOUtils.Close(indexOut, postingsWriter, termsIndexWriter);
					indexOut = null;
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void WriteTrailer(long dirStart)
		{
			indexOut.WriteLong(dirStart);
		}

		private class TermEntry
		{
			public readonly BytesRef term = new BytesRef();

			public BlockTermState state;
		}

		internal class TermsWriter : TermsConsumer
		{
			private readonly FieldInfo fieldInfo;

			private readonly PostingsWriterBase postingsWriter;

			private readonly long termsStartPointer;

			private long numTerms;

			private readonly TermsIndexWriterBase.FieldWriter fieldIndexWriter;

			internal long sumTotalTermFreq;

			internal long sumDocFreq;

			internal int docCount;

			internal int longsSize;

			private BlockTermsWriter.TermEntry[] pendingTerms;

			private int pendingCount;

			internal TermsWriter(BlockTermsWriter _enclosing, TermsIndexWriterBase.FieldWriter
				 fieldIndexWriter, FieldInfo fieldInfo, PostingsWriterBase postingsWriter)
			{
				this._enclosing = _enclosing;
				this.fieldInfo = fieldInfo;
				this.fieldIndexWriter = fieldIndexWriter;
				this.pendingTerms = new BlockTermsWriter.TermEntry[32];
				for (int i = 0; i < this.pendingTerms.Length; i++)
				{
					this.pendingTerms[i] = new BlockTermsWriter.TermEntry();
				}
				this.termsStartPointer = this._enclosing.indexOut.FilePointer;
				this.postingsWriter = postingsWriter;
				this.longsSize = postingsWriter.SetField(fieldInfo);
			}

			public override IComparer<BytesRef> Comparator
			{
			    get { return BytesRef.UTF8SortedAsUnicodeComparer; }
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override PostingsConsumer StartTerm(BytesRef text)
			{
				//System.out.println("BTW: startTerm term=" + fieldInfo.name + ":" + text.utf8ToString() + " " + text + " seg=" + segment);
				this.postingsWriter.StartTerm();
				return this.postingsWriter;
			}

			private readonly BytesRef lastPrevTerm = new BytesRef();

			/// <exception cref="System.IO.IOException"></exception>
			public override void FinishTerm(BytesRef text, TermStats stats)
			{
				//HM:revisit 
				//
				//HM:revisit 
				//assert stats.docFreq > 0;
				//System.out.println("BTW: finishTerm term=" + fieldInfo.name + ":" + text.utf8ToString() + " " + text + " seg=" + segment + " df=" + stats.docFreq);
				bool isIndexTerm = this.fieldIndexWriter.CheckIndexTerm(text, stats);
				if (isIndexTerm)
				{
					if (this.pendingCount > 0)
					{
						// Instead of writing each term, live, we gather terms
						// in RAM in a pending buffer, and then write the
						// entire block in between index terms:
						this.FlushBlock();
					}
					this.fieldIndexWriter.Add(text, stats, this._enclosing.indexOut.FilePointer);
				}
				//System.out.println("  index term!");
				if (this.pendingTerms.Length == this.pendingCount)
				{
					BlockTermsWriter.TermEntry[] newArray = new BlockTermsWriter.TermEntry[ArrayUtil.
						Oversize(this.pendingCount + 1, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
					System.Array.Copy(this.pendingTerms, 0, newArray, 0, this.pendingCount);
					for (int i = this.pendingCount; i < newArray.Length; i++)
					{
						newArray[i] = new BlockTermsWriter.TermEntry();
					}
					this.pendingTerms = newArray;
				}
				BlockTermsWriter.TermEntry te = this.pendingTerms[this.pendingCount];
				te.term.CopyBytes(text);
				te.state = this.postingsWriter.NewTermState();
				te.state.docFreq = stats.docFreq;
				te.state.totalTermFreq = stats.totalTermFreq;
				this.postingsWriter.FinishTerm(te.state);
				this.pendingCount++;
				this.numTerms++;
			}

			// Finishes all terms in this field
			/// <exception cref="System.IO.IOException"></exception>
			public override void Finish(long sumTotalTermFreq, long sumDocFreq, int docCount)
			{
				if (this.pendingCount > 0)
				{
					this.FlushBlock();
				}
				// EOF marker:
				this._enclosing.indexOut.WriteVInt(0);
				this.sumTotalTermFreq = sumTotalTermFreq;
				this.sumDocFreq = sumDocFreq;
				this.docCount = docCount;
				this.fieldIndexWriter.Finish(this._enclosing.indexOut.FilePointer);
				if (this.numTerms > 0)
				{
					this._enclosing.fields.Add(new BlockTermsWriter.FieldMetaData(this.fieldInfo, 
						this.numTerms, this.termsStartPointer, sumTotalTermFreq, sumDocFreq, docCount, this
						.longsSize));
				}
			}

			private int SharedPrefix(BytesRef term1, BytesRef term2)
			{
				//HM:revisit 
				//
				//HM:revisit 
				//assert term1.offset == 0;
				//HM:revisit 
				//
				//HM:revisit 
				//assert term2.offset == 0;
				int pos1 = 0;
				int pos1End = pos1 + Math.Min(term1.length, term2.length);
				int pos2 = 0;
				while (pos1 < pos1End)
				{
					if (term1.bytes[pos1] != term2.bytes[pos2])
					{
						return pos1;
					}
					pos1++;
					pos2++;
				}
				return pos1;
			}

			private readonly RAMOutputStream bytesWriter = new RAMOutputStream();

			private readonly RAMOutputStream bufferWriter = new RAMOutputStream();

			/// <exception cref="System.IO.IOException"></exception>
			private void FlushBlock()
			{
				//System.out.println("BTW.flushBlock seg=" + segment + " pendingCount=" + pendingCount + " fp=" + out.getFilePointer());
				// First pass: compute common prefix for all terms
				// in the block, against term before first term in
				// this block:
				int commonPrefix = this.SharedPrefix(this.lastPrevTerm, this.pendingTerms[0].term
					);
				for (int termCount = 1; termCount < this.pendingCount; termCount++)
				{
					commonPrefix = Math.Min(commonPrefix, this.SharedPrefix(this.lastPrevTerm, this.pendingTerms
						[termCount].term));
				}
				this._enclosing.indexOut.WriteVInt(this.pendingCount);
				this._enclosing.indexOut.WriteVInt(commonPrefix);
				// 2nd pass: write suffixes, as separate byte[] blob
				for (int termCount_1 = 0; termCount_1 < this.pendingCount; termCount_1++)
				{
					int suffix = this.pendingTerms[termCount_1].term.length - commonPrefix;
					// TODO: cutover to better intblock codec, instead
					// of interleaving here:
					this.bytesWriter.WriteVInt(suffix);
					this.bytesWriter.WriteBytes(this.pendingTerms[termCount_1].term.bytes, commonPrefix
						, suffix);
				}
				this._enclosing.indexOut.WriteVInt((int)this.bytesWriter.FilePointer);
				this.bytesWriter.WriteTo(this._enclosing.indexOut);
				this.bytesWriter.Reset();
				// 3rd pass: write the freqs as byte[] blob
				// TODO: cutover to better intblock codec.  simple64?
				// write prefix, suffix first:
				for (int termCount_2 = 0; termCount_2 < this.pendingCount; termCount_2++)
				{
					BlockTermState state = this.pendingTerms[termCount_2].state;
					//HM:revisit 
					//
					//HM:revisit 
					//assert state != null;
					this.bytesWriter.WriteVInt(state.docFreq);
					if (this.fieldInfo.IndexOptionsValue != FieldInfo.IndexOptions.DOCS_ONLY)
					{
						this.bytesWriter.WriteVLong(state.totalTermFreq - state.docFreq);
					}
				}
				this._enclosing.indexOut.WriteVInt((int)this.bytesWriter.FilePointer);
				this.bytesWriter.WriteTo(this._enclosing.indexOut);
				this.bytesWriter.Reset();
				// 4th pass: write the metadata 
				long[] longs = new long[this.longsSize];
				bool absolute = true;
				for (int termCount_3 = 0; termCount_3 < this.pendingCount; termCount_3++)
				{
					BlockTermState state = this.pendingTerms[termCount_3].state;
					this.postingsWriter.EncodeTerm(longs, this.bufferWriter, this.fieldInfo, state, absolute
						);
					for (int i = 0; i < this.longsSize; i++)
					{
						this.bytesWriter.WriteVLong(longs[i]);
					}
					this.bufferWriter.WriteTo(this.bytesWriter);
					this.bufferWriter.Reset();
					absolute = false;
				}
				this._enclosing.indexOut.WriteVInt((int)this.bytesWriter.FilePointer);
				this.bytesWriter.WriteTo(this._enclosing.indexOut);
				this.bytesWriter.Reset();
				this.lastPrevTerm.CopyBytes(this.pendingTerms[this.pendingCount - 1].term);
				this.pendingCount = 0;
			}

			private readonly BlockTermsWriter _enclosing;
		}
	}
}
