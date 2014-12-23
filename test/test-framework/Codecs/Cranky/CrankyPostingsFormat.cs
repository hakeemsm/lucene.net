using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Codecs.Cranky.TestFramework
{
	internal class CrankyPostingsFormat : PostingsFormat
	{
		internal readonly PostingsFormat pFormat;

		internal readonly Random random;

		internal CrankyPostingsFormat(PostingsFormat del, Random random) : base(del.Name)
		{
			// we impersonate the passed-in codec, so we don't need to be in SPI,
			// and so we dont change file formats
			this.pFormat = del;
			this.random = random;
		}

		
		public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
		{
			if (random.Next(100) == 0)
			{
				throw new IOException("Fake IOException from PostingsFormat.fieldsConsumer()");
			}
			return new CrankyFieldsConsumer(pFormat.FieldsConsumer(state), random);
		}

		
		public override FieldsProducer FieldsProducer(SegmentReadState state)
		{
			return pFormat.FieldsProducer(state);
		}

		internal class CrankyFieldsConsumer : FieldsConsumer
		{
			internal readonly FieldsConsumer fldConsumer;

			internal readonly Random random;

			internal CrankyFieldsConsumer(FieldsConsumer del, Random random)
			{
				this.fldConsumer = del;
				this.random = random;
			}

			
			public override TermsConsumer AddField(FieldInfo field)
			{
				if (random.Next(100) == 0)
				{
					throw new IOException("Fake IOException from FieldsConsumer.addField()");
				}
				return new CrankyTermsConsumer(fldConsumer.AddField(field), random);
			}

			
			public override void Merge(MergeState mergeState, Fields fields)
			{
				if (random.Next(100) == 0)
				{
					throw new IOException("Fake IOException from FieldsConsumer.merge()");
				}
				base.Merge(mergeState, fields);
			}


		    protected override void Dispose(bool disposing)
			{
				fldConsumer.Dispose();
				if (random.Next(100) == 0)
				{
					throw new IOException("Fake IOException from FieldsConsumer.close()");
				}
			}
		}

		internal class CrankyTermsConsumer : TermsConsumer
		{
			internal readonly TermsConsumer trmConsumer;

			internal readonly Random random;

			internal CrankyTermsConsumer(TermsConsumer del, Random random)
			{
				this.trmConsumer = del;
				this.random = random;
			}

			
			public override PostingsConsumer StartTerm(BytesRef text)
			{
				if (random.Next(10000) == 0)
				{
					throw new IOException("Fake IOException from TermsConsumer.startTerm()");
				}
				return new CrankyPostingsConsumer(trmConsumer.StartTerm(text), random);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void FinishTerm(BytesRef text, TermStats stats)
			{
				if (random.Next(10000) == 0)
				{
					throw new IOException("Fake IOException from TermsConsumer.finishTerm()");
				}
				trmConsumer.FinishTerm(text, stats);
			}

			
			public override void Finish(long sumTotalTermFreq, long sumDocFreq, int docCount)
			{
				if (random.Next(100) == 0)
				{
					throw new IOException("Fake IOException from TermsConsumer.finish()");
				}
				trmConsumer.Finish(sumTotalTermFreq, sumDocFreq, docCount);
			}

			
			public override IComparer<BytesRef> Comparator
			{
			    get
			    {
			        if (random.Next(100) == 0)
			        {
			            throw new IOException("Fake IOException from TermsConsumer.getComparator()");
			        }
			        return trmConsumer.Comparator;
			    }
			}

			
			public override void Merge(MergeState mergeState, FieldInfo.IndexOptions indexOptions, TermsEnum termsEnum)
			{
				if (random.Next(100) == 0)
				{
					throw new IOException("Fake IOException from TermsConsumer.merge()");
				}
				base.Merge(mergeState, indexOptions, termsEnum);
			}
		}

		internal class CrankyPostingsConsumer : PostingsConsumer
		{
			internal readonly PostingsConsumer pConsumer;

			internal readonly Random random;

			internal CrankyPostingsConsumer(PostingsConsumer del, Random random)
			{
				this.pConsumer = del;
				this.random = random;
			}

			
			public override void StartDoc(int docID, int freq)
			{
				if (random.Next(10000) == 0)
				{
					throw new IOException("Fake IOException from PostingsConsumer.startDoc()");
				}
				pConsumer.StartDoc(docID, freq);
			}

			
			public override void FinishDoc()
			{
				if (random.Next(10000) == 0)
				{
					throw new IOException("Fake IOException from PostingsConsumer.finishDoc()");
				}
				pConsumer.FinishDoc();
			}

			
			public override void AddPosition(int position, BytesRef payload, int startOffset, int endOffset)
			{
				if (random.Next(10000) == 0)
				{
					throw new IOException("Fake IOException from PostingsConsumer.addPosition()");
				}
				pConsumer.AddPosition(position, payload, startOffset, endOffset);
			}

			
			public override TermStats Merge(MergeState mergeState, FieldInfo.IndexOptions indexOptions
				, DocsEnum postings, FixedBitSet visitedDocs)
			{
				if (random.Next(10000) == 0)
				{
					throw new IOException("Fake IOException from PostingsConsumer.merge()");
				}
				return base.Merge(mergeState, indexOptions, postings, visitedDocs);
			}
		}
	}
}
