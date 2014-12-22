/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Org.Apache.Lucene.Codecs;
using Lucene.Net.Codecs.Ramonly;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Store;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Lucene.Net.Codecs.Ramonly
{
	/// <summary>
	/// Stores all postings data in RAM, but writes a small
	/// token (header + single int) to identify which "slot" the
	/// index is using in RAM HashMap.
	/// </summary>
	/// <remarks>
	/// Stores all postings data in RAM, but writes a small
	/// token (header + single int) to identify which "slot" the
	/// index is using in RAM HashMap.
	/// NOTE: this codec sorts terms by reverse-unicode-order!
	/// </remarks>
	public sealed class RAMOnlyPostingsFormat : PostingsFormat
	{
		private sealed class _IComparer_66 : IComparer<BytesRef>
		{
			public _IComparer_66()
			{
			}

			// For fun, test that we can override how terms are
			// sorted, and basic things still work -- this comparator
			// sorts in reversed unicode code point order:
			public int Compare(BytesRef t1, BytesRef t2)
			{
				byte[] b1 = t1.bytes;
				byte[] b2 = t2.bytes;
				int b1Stop;
				int b1Upto = t1.offset;
				int b2Upto = t2.offset;
				if (t1.length < t2.length)
				{
					b1Stop = t1.offset + t1.length;
				}
				else
				{
					b1Stop = t1.offset + t2.length;
				}
				while (b1Upto < b1Stop)
				{
					int bb1 = b1[b1Upto++] & unchecked((int)(0xff));
					int bb2 = b2[b2Upto++] & unchecked((int)(0xff));
					if (bb1 != bb2)
					{
						//System.out.println("cmp 1=" + t1 + " 2=" + t2 + " return " + (bb2-bb1));
						return bb2 - bb1;
					}
				}
				// One is prefix of another, or they are equal
				return t2.length - t1.length;
			}

			public override bool Equals(object other)
			{
				return this == other;
			}
		}

		private static readonly IComparer<BytesRef> reverseUnicodeComparator = new _IComparer_66
			();

		public RAMOnlyPostingsFormat() : base("RAMOnly")
		{
		}

		internal class RAMPostings : FieldsProducer
		{
			internal readonly IDictionary<string, RAMOnlyPostingsFormat.RAMField> fieldToTerms
				 = new SortedDictionary<string, RAMOnlyPostingsFormat.RAMField>();

			// Postings state:
			public override Org.Apache.Lucene.Index.Terms Terms(string field)
			{
				return fieldToTerms.Get(field);
			}

			public override int Size()
			{
				return fieldToTerms.Count;
			}

			public override Sharpen.Iterator<string> Iterator()
			{
				return Sharpen.Collections.UnmodifiableSet(fieldToTerms.Keys).Iterator();
			}

			public override void Close()
			{
			}

			public override long RamBytesUsed()
			{
				long sizeInBytes = 0;
				foreach (RAMOnlyPostingsFormat.RAMField field in fieldToTerms.Values)
				{
					sizeInBytes += field.RamBytesUsed();
				}
				return sizeInBytes;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void CheckIntegrity()
			{
			}
		}

		internal class RAMField : Terms
		{
			internal readonly string field;

			internal readonly SortedDictionary<string, RAMOnlyPostingsFormat.RAMTerm> termToDocs
				 = new SortedDictionary<string, RAMOnlyPostingsFormat.RAMTerm>();

			internal long sumTotalTermFreq;

			internal long sumDocFreq;

			internal int docCount;

			internal readonly FieldInfo info;

			internal RAMField(string field, FieldInfo info)
			{
				this.field = field;
				this.info = info;
			}

			/// <summary>Returns approximate RAM bytes used</summary>
			public virtual long RamBytesUsed()
			{
				long sizeInBytes = 0;
				foreach (RAMOnlyPostingsFormat.RAMTerm term in termToDocs.Values)
				{
					sizeInBytes += term.RamBytesUsed();
				}
				return sizeInBytes;
			}

			public override long Size()
			{
				return termToDocs.Count;
			}

			public override long GetSumTotalTermFreq()
			{
				return sumTotalTermFreq;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override long GetSumDocFreq()
			{
				return sumDocFreq;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int GetDocCount()
			{
				return docCount;
			}

			public override TermsEnum Iterator(TermsEnum reuse)
			{
				return new RAMOnlyPostingsFormat.RAMTermsEnum(this);
			}

			public override IComparer<BytesRef> GetComparator()
			{
				return reverseUnicodeComparator;
			}

			public override bool HasFreqs()
			{
				return info.GetIndexOptions().CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS) >=
					 0;
			}

			public override bool HasOffsets()
			{
				return info.GetIndexOptions().CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
					) >= 0;
			}

			public override bool HasPositions()
			{
				return info.GetIndexOptions().CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS
					) >= 0;
			}

			public override bool HasPayloads()
			{
				return info.HasPayloads();
			}
		}

		internal class RAMTerm
		{
			internal readonly string term;

			internal long totalTermFreq;

			internal readonly IList<RAMOnlyPostingsFormat.RAMDoc> docs = new AList<RAMOnlyPostingsFormat.RAMDoc
				>();

			public RAMTerm(string term)
			{
				this.term = term;
			}

			/// <summary>Returns approximate RAM bytes used</summary>
			public virtual long RamBytesUsed()
			{
				long sizeInBytes = 0;
				foreach (RAMOnlyPostingsFormat.RAMDoc rDoc in docs)
				{
					sizeInBytes += rDoc.RamBytesUsed();
				}
				return sizeInBytes;
			}
		}

		internal class RAMDoc
		{
			internal readonly int docID;

			internal readonly int[] positions;

			internal byte[][] payloads;

			public RAMDoc(int docID, int freq)
			{
				this.docID = docID;
				positions = new int[freq];
			}

			/// <summary>Returns approximate RAM bytes used</summary>
			public virtual long RamBytesUsed()
			{
				long sizeInBytes = 0;
				sizeInBytes += (positions != null) ? RamUsageEstimator.SizeOf(positions) : 0;
				if (payloads != null)
				{
					foreach (byte[] payload in payloads)
					{
						sizeInBytes += (payload != null) ? RamUsageEstimator.SizeOf(payload) : 0;
					}
				}
				return sizeInBytes;
			}
		}

		private class RAMFieldsConsumer : FieldsConsumer
		{
			private readonly RAMOnlyPostingsFormat.RAMPostings postings;

			private readonly RAMOnlyPostingsFormat.RAMTermsConsumer termsConsumer = new RAMOnlyPostingsFormat.RAMTermsConsumer
				();

			public RAMFieldsConsumer(RAMOnlyPostingsFormat.RAMPostings postings)
			{
				// Classes for writing to the postings state
				this.postings = postings;
			}

			public override TermsConsumer AddField(FieldInfo field)
			{
				if (field.GetIndexOptions().CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
					) >= 0)
				{
					throw new NotSupportedException("this codec cannot index offsets");
				}
				RAMOnlyPostingsFormat.RAMField ramField = new RAMOnlyPostingsFormat.RAMField(field
					.name, field);
				postings.fieldToTerms.Put(field.name, ramField);
				termsConsumer.Reset(ramField);
				return termsConsumer;
			}

			public override void Close()
			{
			}
			// TODO: finalize stuff
		}

		private class RAMTermsConsumer : TermsConsumer
		{
			private RAMOnlyPostingsFormat.RAMField field;

			private readonly RAMOnlyPostingsFormat.RAMPostingsWriterImpl postingsWriter = new 
				RAMOnlyPostingsFormat.RAMPostingsWriterImpl();

			internal RAMOnlyPostingsFormat.RAMTerm current;

			internal virtual void Reset(RAMOnlyPostingsFormat.RAMField field)
			{
				this.field = field;
			}

			public override PostingsConsumer StartTerm(BytesRef text)
			{
				string term = text.Utf8ToString();
				current = new RAMOnlyPostingsFormat.RAMTerm(term);
				postingsWriter.Reset(current);
				return postingsWriter;
			}

			public override IComparer<BytesRef> GetComparator()
			{
				return BytesRef.GetUTF8SortedAsUnicodeComparator();
			}

			public override void FinishTerm(BytesRef text, TermStats stats)
			{
				 
				//assert stats.docFreq > 0;
				 
				//assert stats.docFreq == current.docs.size();
				current.totalTermFreq = stats.totalTermFreq;
				field.termToDocs.Put(current.term, current);
			}

			public override void Finish(long sumTotalTermFreq, long sumDocFreq, int docCount)
			{
				field.sumTotalTermFreq = sumTotalTermFreq;
				field.sumDocFreq = sumDocFreq;
				field.docCount = docCount;
			}
		}

		internal class RAMPostingsWriterImpl : PostingsConsumer
		{
			private RAMOnlyPostingsFormat.RAMTerm term;

			private RAMOnlyPostingsFormat.RAMDoc current;

			private int posUpto = 0;

			public virtual void Reset(RAMOnlyPostingsFormat.RAMTerm term)
			{
				this.term = term;
			}

			public override void StartDoc(int docID, int freq)
			{
				current = new RAMOnlyPostingsFormat.RAMDoc(docID, freq);
				term.docs.AddItem(current);
				posUpto = 0;
			}

			public override void AddPosition(int position, BytesRef payload, int startOffset, 
				int endOffset)
			{
				 
				//assert startOffset == -1;
				 
				//assert endOffset == -1;
				current.positions[posUpto] = position;
				if (payload != null && payload.length > 0)
				{
					if (current.payloads == null)
					{
						current.payloads = new byte[current.positions.Length][];
					}
					byte[] bytes = current.payloads[posUpto] = new byte[payload.length];
					System.Array.Copy(payload.bytes, payload.offset, bytes, 0, payload.length);
				}
				posUpto++;
			}

			public override void FinishDoc()
			{
			}
			 
			//assert posUpto == current.positions.length;
		}

		internal class RAMTermsEnum : TermsEnum
		{
			internal Iterator<string> it;

			internal string current;

			private readonly RAMOnlyPostingsFormat.RAMField ramField;

			public RAMTermsEnum(RAMOnlyPostingsFormat.RAMField field)
			{
				this.ramField = field;
			}

			public override IComparer<BytesRef> GetComparator()
			{
				return BytesRef.GetUTF8SortedAsUnicodeComparator();
			}

			public override BytesRef Next()
			{
				if (it == null)
				{
					if (current == null)
					{
						it = ramField.termToDocs.Keys.Iterator();
					}
					else
					{
						it = ramField.termToDocs.TailMap(current).Keys.Iterator();
					}
				}
				if (it.HasNext())
				{
					current = it.Next();
					return new BytesRef(current);
				}
				else
				{
					return null;
				}
			}

			public override TermsEnum.SeekStatus SeekCeil(BytesRef term)
			{
				current = term.Utf8ToString();
				it = null;
				if (ramField.termToDocs.ContainsKey(current))
				{
					return TermsEnum.SeekStatus.FOUND;
				}
				else
				{
					if (Sharpen.Runtime.CompareOrdinal(current, ramField.termToDocs.LastKey()) > 0)
					{
						return TermsEnum.SeekStatus.END;
					}
					else
					{
						return TermsEnum.SeekStatus.NOT_FOUND;
					}
				}
			}

			public override void SeekExact(long ord)
			{
				throw new NotSupportedException();
			}

			public override long Ord()
			{
				throw new NotSupportedException();
			}

			public override BytesRef Term()
			{
				// TODO: reuse BytesRef
				return new BytesRef(current);
			}

			public override int DocFreq()
			{
				return ramField.termToDocs.Get(current).docs.Count;
			}

			public override long TotalTermFreq()
			{
				return ramField.termToDocs.Get(current).totalTermFreq;
			}

			public override DocsEnum Docs(Bits liveDocs, DocsEnum reuse, int flags)
			{
				return new RAMOnlyPostingsFormat.RAMDocsEnum(ramField.termToDocs.Get(current), liveDocs
					);
			}

			public override DocsAndPositionsEnum DocsAndPositions(Bits liveDocs, DocsAndPositionsEnum
				 reuse, int flags)
			{
				return new RAMOnlyPostingsFormat.RAMDocsAndPositionsEnum(ramField.termToDocs.Get(
					current), liveDocs);
			}
		}

		private class RAMDocsEnum : DocsEnum
		{
			private readonly RAMOnlyPostingsFormat.RAMTerm ramTerm;

			private readonly Bits liveDocs;

			private RAMOnlyPostingsFormat.RAMDoc current;

			internal int upto = -1;

			internal int posUpto = 0;

			public RAMDocsEnum(RAMOnlyPostingsFormat.RAMTerm ramTerm, Bits liveDocs)
			{
				this.ramTerm = ramTerm;
				this.liveDocs = liveDocs;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Advance(int targetDocID)
			{
				return SlowAdvance(targetDocID);
			}

			// TODO: override bulk read, for better perf
			public override int NextDoc()
			{
				while (true)
				{
					upto++;
					if (upto < ramTerm.docs.Count)
					{
						current = ramTerm.docs[upto];
						if (liveDocs == null || liveDocs.Get(current.docID))
						{
							posUpto = 0;
							return current.docID;
						}
					}
					else
					{
						return NO_MORE_DOCS;
					}
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Freq()
			{
				return current.positions.Length;
			}

			public override int DocID()
			{
				return current.docID;
			}

			public override long Cost()
			{
				return ramTerm.docs.Count;
			}
		}

		private class RAMDocsAndPositionsEnum : DocsAndPositionsEnum
		{
			private readonly RAMOnlyPostingsFormat.RAMTerm ramTerm;

			private readonly Bits liveDocs;

			private RAMOnlyPostingsFormat.RAMDoc current;

			internal int upto = -1;

			internal int posUpto = 0;

			public RAMDocsAndPositionsEnum(RAMOnlyPostingsFormat.RAMTerm ramTerm, Bits liveDocs
				)
			{
				this.ramTerm = ramTerm;
				this.liveDocs = liveDocs;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Advance(int targetDocID)
			{
				return SlowAdvance(targetDocID);
			}

			// TODO: override bulk read, for better perf
			public override int NextDoc()
			{
				while (true)
				{
					upto++;
					if (upto < ramTerm.docs.Count)
					{
						current = ramTerm.docs[upto];
						if (liveDocs == null || liveDocs.Get(current.docID))
						{
							posUpto = 0;
							return current.docID;
						}
					}
					else
					{
						return NO_MORE_DOCS;
					}
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Freq()
			{
				return current.positions.Length;
			}

			public override int DocID()
			{
				return current.docID;
			}

			public override int NextPosition()
			{
				return current.positions[posUpto++];
			}

			public override int StartOffset()
			{
				return -1;
			}

			public override int EndOffset()
			{
				return -1;
			}

			public override BytesRef GetPayload()
			{
				if (current.payloads != null && current.payloads[posUpto - 1] != null)
				{
					return new BytesRef(current.payloads[posUpto - 1]);
				}
				else
				{
					return null;
				}
			}

			public override long Cost()
			{
				return ramTerm.docs.Count;
			}
		}

		private readonly IDictionary<int, RAMOnlyPostingsFormat.RAMPostings> state = new 
			Dictionary<int, RAMOnlyPostingsFormat.RAMPostings>();

		private readonly AtomicInteger nextID = new AtomicInteger();

		private readonly string RAM_ONLY_NAME = "RAMOnly";

		private const int VERSION_START = 0;

		private const int VERSION_LATEST = VERSION_START;

		private static readonly string ID_EXTENSION = "id";

		// Holds all indexes created, keyed by the ID assigned in fieldsConsumer
		/// <exception cref="System.IO.IOException"></exception>
		public override FieldsConsumer FieldsConsumer(SegmentWriteState writeState)
		{
			int id = nextID.GetAndIncrement();
			// TODO -- ok to do this up front instead of
			// on close....?  should be ok?
			// Write our ID:
			string idFileName = IndexFileNames.SegmentFileName(writeState.segmentInfo.name, writeState
				.segmentSuffix, ID_EXTENSION);
			IndexOutput @out = writeState.directory.CreateOutput(idFileName, writeState.context
				);
			bool success = false;
			try
			{
				CodecUtil.WriteHeader(@out, RAM_ONLY_NAME, VERSION_LATEST);
				@out.WriteVInt(id);
				success = true;
			}
			finally
			{
				if (!success)
				{
					IOUtils.CloseWhileHandlingException(@out);
				}
				else
				{
					IOUtils.Close(@out);
				}
			}
			RAMOnlyPostingsFormat.RAMPostings postings = new RAMOnlyPostingsFormat.RAMPostings
				();
			RAMOnlyPostingsFormat.RAMFieldsConsumer consumer = new RAMOnlyPostingsFormat.RAMFieldsConsumer
				(postings);
			lock (state)
			{
				state.Put(id, postings);
			}
			return consumer;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FieldsProducer FieldsProducer(SegmentReadState readState)
		{
			// Load our ID:
			string idFileName = IndexFileNames.SegmentFileName(readState.segmentInfo.name, readState
				.segmentSuffix, ID_EXTENSION);
			IndexInput @in = readState.directory.OpenInput(idFileName, readState.context);
			bool success = false;
			int id;
			try
			{
				CodecUtil.CheckHeader(@in, RAM_ONLY_NAME, VERSION_START, VERSION_LATEST);
				id = @in.ReadVInt();
				success = true;
			}
			finally
			{
				if (!success)
				{
					IOUtils.CloseWhileHandlingException(@in);
				}
				else
				{
					IOUtils.Close(@in);
				}
			}
			lock (state)
			{
				return state.Get(id);
			}
		}
	}
}
