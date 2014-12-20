/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Org.Apache.Lucene.Codecs;
using Org.Apache.Lucene.Codecs.Asserting;
using Org.Apache.Lucene.Codecs.Lucene41;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Codecs.Asserting
{
	/// <summary>
	/// Just like
	/// <see cref="Org.Apache.Lucene.Codecs.Lucene41.Lucene41PostingsFormat">Org.Apache.Lucene.Codecs.Lucene41.Lucene41PostingsFormat
	/// 	</see>
	/// but with additional asserts.
	/// </summary>
	public sealed class AssertingPostingsFormat : PostingsFormat
	{
		private readonly PostingsFormat @in = new Lucene41PostingsFormat();

		public AssertingPostingsFormat() : base("Asserting")
		{
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Org.Apache.Lucene.Codecs.FieldsConsumer FieldsConsumer(SegmentWriteState
			 state)
		{
			return new AssertingPostingsFormat.AssertingFieldsConsumer(@in.FieldsConsumer(state
				));
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Org.Apache.Lucene.Codecs.FieldsProducer FieldsProducer(SegmentReadState
			 state)
		{
			return new AssertingPostingsFormat.AssertingFieldsProducer(@in.FieldsProducer(state
				));
		}

		internal class AssertingFieldsProducer : FieldsProducer
		{
			private readonly FieldsProducer @in;

			internal AssertingFieldsProducer(FieldsProducer @in)
			{
				this.@in = @in;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Close()
			{
				@in.Close();
			}

			public override Sharpen.Iterator<string> Iterator()
			{
				Sharpen.Iterator<string> iterator = @in.Iterator();
				//HM:revisit 
				//assert iterator != null;
				return iterator;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override Org.Apache.Lucene.Index.Terms Terms(string field)
			{
				Org.Apache.Lucene.Index.Terms terms = @in.Terms(field);
				return terms == null ? null : new AssertingAtomicReader.AssertingTerms(terms);
			}

			public override int Size()
			{
				return @in.Size();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override long GetUniqueTermCount()
			{
				return @in.GetUniqueTermCount();
			}

			public override long RamBytesUsed()
			{
				return @in.RamBytesUsed();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void CheckIntegrity()
			{
				@in.CheckIntegrity();
			}
		}

		internal class AssertingFieldsConsumer : FieldsConsumer
		{
			private readonly FieldsConsumer @in;

			internal AssertingFieldsConsumer(FieldsConsumer @in)
			{
				this.@in = @in;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override TermsConsumer AddField(FieldInfo field)
			{
				TermsConsumer consumer = @in.AddField(field);
				//HM:revisit 
				//assert consumer != null;
				return new AssertingPostingsFormat.AssertingTermsConsumer(consumer, field);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Close()
			{
				@in.Close();
			}
		}

		internal enum TermsConsumerState
		{
			INITIAL,
			START,
			FINISHED
		}

		internal class AssertingTermsConsumer : TermsConsumer
		{
			private readonly TermsConsumer @in;

			private readonly FieldInfo fieldInfo;

			private BytesRef lastTerm = null;

			private AssertingPostingsFormat.TermsConsumerState state = AssertingPostingsFormat.TermsConsumerState
				.INITIAL;

			private AssertingPostingsFormat.AssertingPostingsConsumer lastPostingsConsumer = 
				null;

			private long sumTotalTermFreq = 0;

			private long sumDocFreq = 0;

			private OpenBitSet visitedDocs = new OpenBitSet();

			internal AssertingTermsConsumer(TermsConsumer @in, FieldInfo fieldInfo)
			{
				this.@in = @in;
				this.fieldInfo = fieldInfo;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override PostingsConsumer StartTerm(BytesRef text)
			{
				//HM:revisit 
				//assert state == TermsConsumerState.INITIAL || state == TermsConsumerState.START && lastPostingsConsumer.docFreq == 0;
				state = AssertingPostingsFormat.TermsConsumerState.START;
				//HM:revisit 
				//assert lastTerm == null || in.getComparator().compare(text, lastTerm) > 0;
				lastTerm = BytesRef.DeepCopyOf(text);
				return lastPostingsConsumer = new AssertingPostingsFormat.AssertingPostingsConsumer
					(@in.StartTerm(text), fieldInfo, visitedDocs);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void FinishTerm(BytesRef text, TermStats stats)
			{
				//HM:revisit 
				//assert state == TermsConsumerState.START;
				state = AssertingPostingsFormat.TermsConsumerState.INITIAL;
				//HM:revisit 
				//assert text.equals(lastTerm);
				//HM:revisit 
				//assert stats.docFreq > 0; // otherwise, this method should not be called.
				//HM:revisit 
				//assert stats.docFreq == lastPostingsConsumer.docFreq;
				sumDocFreq += stats.docFreq;
				if (fieldInfo.GetIndexOptions() == FieldInfo.IndexOptions.DOCS_ONLY)
				{
				}
				else
				{
					//HM:revisit 
					//assert stats.totalTermFreq == -1;
					//HM:revisit 
					//assert stats.totalTermFreq == lastPostingsConsumer.totalTermFreq;
					sumTotalTermFreq += stats.totalTermFreq;
				}
				@in.FinishTerm(text, stats);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Finish(long sumTotalTermFreq, long sumDocFreq, int docCount)
			{
				//HM:revisit 
				//assert state == TermsConsumerState.INITIAL || state == TermsConsumerState.START && lastPostingsConsumer.docFreq == 0;
				state = AssertingPostingsFormat.TermsConsumerState.FINISHED;
				//HM:revisit 
				//assert docCount >= 0;
				//HM:revisit 
				//assert docCount == visitedDocs.cardinality();
				//HM:revisit 
				//assert sumDocFreq >= docCount;
				//HM:revisit 
				//assert sumDocFreq == this.sumDocFreq;
				if (fieldInfo.GetIndexOptions() == FieldInfo.IndexOptions.DOCS_ONLY)
				{
				}
				//HM:revisit 
				//assert sumTotalTermFreq == -1;
				//HM:revisit 
				//assert sumTotalTermFreq >= sumDocFreq;
				//HM:revisit 
				//assert sumTotalTermFreq == this.sumTotalTermFreq;
				@in.Finish(sumTotalTermFreq, sumDocFreq, docCount);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override IComparer<BytesRef> GetComparator()
			{
				return @in.GetComparator();
			}
		}

		internal enum PostingsConsumerState
		{
			INITIAL,
			START
		}

		internal class AssertingPostingsConsumer : PostingsConsumer
		{
			private readonly PostingsConsumer @in;

			private readonly FieldInfo fieldInfo;

			private readonly OpenBitSet visitedDocs;

			private AssertingPostingsFormat.PostingsConsumerState state = AssertingPostingsFormat.PostingsConsumerState
				.INITIAL;

			private int freq;

			private int positionCount;

			private int lastPosition = 0;

			private int lastStartOffset = 0;

			internal int docFreq = 0;

			internal long totalTermFreq = 0;

			internal AssertingPostingsConsumer(PostingsConsumer @in, FieldInfo fieldInfo, OpenBitSet
				 visitedDocs)
			{
				this.@in = @in;
				this.fieldInfo = fieldInfo;
				this.visitedDocs = visitedDocs;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void StartDoc(int docID, int freq)
			{
				//HM:revisit 
				//assert state == PostingsConsumerState.INITIAL;
				state = AssertingPostingsFormat.PostingsConsumerState.START;
				//HM:revisit 
				//assert docID >= 0;
				if (fieldInfo.GetIndexOptions() == FieldInfo.IndexOptions.DOCS_ONLY)
				{
					//HM:revisit 
					//assert freq == -1;
					this.freq = 0;
				}
				else
				{
					// we don't expect any positions here
					//HM:revisit 
					//assert freq > 0;
					this.freq = freq;
					totalTermFreq += freq;
				}
				this.positionCount = 0;
				this.lastPosition = 0;
				this.lastStartOffset = 0;
				docFreq++;
				visitedDocs.Set(docID);
				@in.StartDoc(docID, freq);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void AddPosition(int position, BytesRef payload, int startOffset, 
				int endOffset)
			{
				//HM:revisit 
				//assert state == PostingsConsumerState.START;
				//HM:revisit 
				//assert positionCount < freq;
				positionCount++;
				//HM:revisit 
				//assert position >= lastPosition || position == -1; /* we still allow -1 from old 3.x indexes */
				lastPosition = position;
				if (fieldInfo.GetIndexOptions() == FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS)
				{
					//HM:revisit 
					//assert startOffset >= 0;
					//HM:revisit 
					//assert startOffset >= lastStartOffset;
					lastStartOffset = startOffset;
				}
				//HM:revisit 
				//assert endOffset >= startOffset;
				//HM:revisit 
				//assert startOffset == -1;
				//HM:revisit 
				//assert endOffset == -1;
				if (payload != null)
				{
				}
				//HM:revisit 
				//assert fieldInfo.hasPayloads();
				@in.AddPosition(position, payload, startOffset, endOffset);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void FinishDoc()
			{
				//HM:revisit 
				//assert state == PostingsConsumerState.START;
				state = AssertingPostingsFormat.PostingsConsumerState.INITIAL;
				if (fieldInfo.GetIndexOptions().CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS
					) < 0)
				{
				}
				//HM:revisit 
				//assert positionCount == 0; // we should not have fed any positions!
				//HM:revisit 
				//assert positionCount == freq;
				@in.FinishDoc();
			}
		}
	}
}
