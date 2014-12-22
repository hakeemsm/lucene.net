using System;
using System.Collections.Generic;
using System.Diagnostics;
using Lucene.Net.Codecs.Lucene41;
using Lucene.Net.Index;
using Lucene.Net.TestFramework.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Codecs.Asserting.TestFramework
{
	/// <summary>
	/// Just like
	/// <see cref="Lucene41PostingsFormat">Lucene.Net.Codecs.Lucene41.Lucene41PostingsFormat
	/// 	</see>
	/// but with additional asserts.
	/// </summary>
	public sealed class AssertingPostingsFormat : PostingsFormat
	{
		private readonly PostingsFormat @in = new Lucene41PostingsFormat();

		public AssertingPostingsFormat() : base("Asserting")
		{
		}

		
		public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
		{
			return new AssertingFieldsConsumer(@in.FieldsConsumer(state));
		}

		
		public override FieldsProducer FieldsProducer(SegmentReadState state)
		{
			return new AssertingFieldsProducer(@in.FieldsProducer(state));
		}

		internal class AssertingFieldsProducer : FieldsProducer
		{
			private readonly FieldsProducer @in;

			internal AssertingFieldsProducer(FieldsProducer @in)
			{
				this.@in = @in;
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected override void Dispose(bool disposing)
			{
				@in.Dispose();
			}

			public override IEnumerator<string> GetEnumerator()
			{
				IEnumerator<string> iterator = @in.GetEnumerator();
				Debug.Assert(iterator != null);
				return iterator;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override Terms Terms(string field)
			{
				Terms terms = @in.Terms(field);
				return terms == null ? null : new AssertingAtomicReader.AssertingTerms(terms);
			}

			public override int Size
			{
			    get { return @in.Size; }
			}


		    [Obsolete]
		    public override long UniqueTermCount
			{
			    get { return @in.UniqueTermCount; }
			}

			public override long RamBytesUsed
			{
			    get { return @in.RamBytesUsed; }
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
				 
				//assert consumer != null;
				return new AssertingPostingsFormat.AssertingTermsConsumer(consumer, field);
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected override void Dispose(bool disposing)
			{
				@in.Dispose();
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

			private TermsConsumerState state = TermsConsumerState.INITIAL;

			private AssertingPostingsConsumer lastPostingsConsumer = null;

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
				Debug.Assert(state == TermsConsumerState.INITIAL || state == TermsConsumerState.START && lastPostingsConsumer.docFreq == 0);
				state = TermsConsumerState.START;
				
				//Debug.Assert(lastTerm == null || in.getComparator().compare(text, lastTerm) > 0);
				lastTerm = BytesRef.DeepCopyOf(text);
				return lastPostingsConsumer = new AssertingPostingsConsumer(@in.StartTerm(text), fieldInfo, visitedDocs);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void FinishTerm(BytesRef text, TermStats stats)
			{
				
				Debug.Assert(state == TermsConsumerState.START);
				state = TermsConsumerState.INITIAL;
				Debug.Assert(text.equals(lastTerm));
				Debug.Assert(stats.docFreq > 0); // otherwise, this method should not be called.
				Debug.Assert(stats.docFreq == lastPostingsConsumer.docFreq);
				sumDocFreq += stats.docFreq;
				if (fieldInfo.IndexOptionsValue.GetValueOrDefault() != FieldInfo.IndexOptions.DOCS_ONLY)
				{
					Debug.Assert(stats.totalTermFreq == lastPostingsConsumer.totalTermFreq);
					sumTotalTermFreq += stats.totalTermFreq;
				}
				@in.FinishTerm(text, stats);
			}

			
			public override void Finish(long sumTotalTermFreq, long sumDocFreq, int docCount)
			{
				 
				//assert state == TermsConsumerState.INITIAL || state == TermsConsumerState.START && lastPostingsConsumer.docFreq == 0;
				state = TermsConsumerState.FINISHED;
				 
				//assert docCount >= 0;
				 
				//assert docCount == visitedDocs.cardinality();
				 
				//assert sumDocFreq >= docCount;
				 
				//assert sumDocFreq == this.sumDocFreq;
			    if (fieldInfo.IndexOptionsValue.GetValueOrDefault() == FieldInfo.IndexOptions.DOCS_ONLY)
			    {
			        Debug.Assert(sumTotalTermFreq == -1);
			    }
			    else
			    {
			        Debug.Assert(sumTotalTermFreq >= sumDocFreq);
			        Debug.Assert(sumTotalTermFreq == this.sumTotalTermFreq);
			        @in.Finish(sumTotalTermFreq, sumDocFreq, docCount);
			    }
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override IComparer<BytesRef> Comparator
			{
			    get { return @in.Comparator; }
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

		    private int positionCount;

		    internal int docFreq = 0;

			internal long totalTermFreq = 0;

		    private int freq2;
            private int lastStartOffset = 0;
		    private int lastPosition=0;
		    private PostingsConsumerState state;

		    internal AssertingPostingsConsumer(PostingsConsumer @in, FieldInfo fieldInfo, OpenBitSet visitedDocs)
			{
				this.@in = @in;
				this.fieldInfo = fieldInfo;
				this.visitedDocs = visitedDocs;
			}

			
			public override void StartDoc(int docID, int freq)
			{
				//assert state == PostingsConsumerState.INITIAL;

			    //assert docID >= 0;
				if (fieldInfo.IndexOptionsValue.GetValueOrDefault() == FieldInfo.IndexOptions.DOCS_ONLY)
				{
					Debug.Assert(freq == -1);
                    freq2 = 0;
				}
				else
				{
					// we don't expect any positions here
					 
					Debug.Assert(freq > 0);
				    freq2 = freq;
				    totalTermFreq += freq;
				}
				this.positionCount = 0;
			    docFreq++;
				visitedDocs.Set(docID);
				@in.StartDoc(docID, freq);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void AddPosition(int position, BytesRef payload, int startOffset, int endOffset)
			{
				 
				//assert state == PostingsConsumerState.START;
				 
				//assert positionCount < freq;
				positionCount++;
                lastPosition = position; 
				//assert position >= lastPosition || position == -1; /* we still allow -1 from old 3.x indexes */
			    if (fieldInfo.IndexOptionsValue.GetValueOrDefault() == FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS)
				{
					 
					//assert startOffset >= 0;
					 
					//assert startOffset >= lastStartOffset;
                    lastStartOffset = startOffset;
				}
				 
				//assert endOffset >= startOffset;
				 
				//assert startOffset == -1;
				 
				//assert endOffset == -1;
				if (payload != null)
				{
				}
				 
				//assert fieldInfo.hasPayloads();
				@in.AddPosition(position, payload, startOffset, endOffset);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void FinishDoc()
			{
				 
				//assert state == PostingsConsumerState.START;
                state = PostingsConsumerState.INITIAL;
			    if (fieldInfo.IndexOptionsValue.GetValueOrDefault().CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS
			        ) < 0)
			    {
			        Debug.Assert(positionCount == 0); // we should not have fed any positions!
			    }
			    else
			    {
			        Debug.Assert(positionCount == freq2);
			    }
			    @in.FinishDoc();
			}
		}
	}
}
