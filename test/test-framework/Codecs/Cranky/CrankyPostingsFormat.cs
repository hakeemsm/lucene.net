/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using System.IO;
using Org.Apache.Lucene.Codecs;
using Lucene.Net.Codecs.Cranky;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Lucene.Net.Codecs.Cranky
{
	internal class CrankyPostingsFormat : PostingsFormat
	{
		internal readonly PostingsFormat delegate_;

		internal readonly Random random;

		internal CrankyPostingsFormat(PostingsFormat delegate_, Random random) : base(delegate_
			.GetName())
		{
			// we impersonate the passed-in codec, so we don't need to be in SPI,
			// and so we dont change file formats
			this.delegate_ = delegate_;
			this.random = random;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Lucene.Net.Codecs.FieldsConsumer FieldsConsumer(SegmentWriteState
			 state)
		{
			if (random.Next(100) == 0)
			{
				throw new IOException("Fake IOException from PostingsFormat.fieldsConsumer()");
			}
			return new CrankyPostingsFormat.CrankyFieldsConsumer(delegate_.FieldsConsumer(state
				), random);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Lucene.Net.Codecs.FieldsProducer FieldsProducer(SegmentReadState
			 state)
		{
			return delegate_.FieldsProducer(state);
		}

		internal class CrankyFieldsConsumer : FieldsConsumer
		{
			internal readonly FieldsConsumer delegate_;

			internal readonly Random random;

			internal CrankyFieldsConsumer(FieldsConsumer delegate_, Random random)
			{
				this.delegate_ = delegate_;
				this.random = random;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override TermsConsumer AddField(FieldInfo field)
			{
				if (random.Next(100) == 0)
				{
					throw new IOException("Fake IOException from FieldsConsumer.addField()");
				}
				return new CrankyPostingsFormat.CrankyTermsConsumer(delegate_.AddField(field), random
					);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Merge(MergeState mergeState, Fields fields)
			{
				if (random.Next(100) == 0)
				{
					throw new IOException("Fake IOException from FieldsConsumer.merge()");
				}
				base.Merge(mergeState, fields);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Close()
			{
				delegate_.Close();
				if (random.Next(100) == 0)
				{
					throw new IOException("Fake IOException from FieldsConsumer.close()");
				}
			}
		}

		internal class CrankyTermsConsumer : TermsConsumer
		{
			internal readonly TermsConsumer delegate_;

			internal readonly Random random;

			internal CrankyTermsConsumer(TermsConsumer delegate_, Random random)
			{
				this.delegate_ = delegate_;
				this.random = random;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override PostingsConsumer StartTerm(BytesRef text)
			{
				if (random.Next(10000) == 0)
				{
					throw new IOException("Fake IOException from TermsConsumer.startTerm()");
				}
				return new CrankyPostingsFormat.CrankyPostingsConsumer(delegate_.StartTerm(text), 
					random);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void FinishTerm(BytesRef text, TermStats stats)
			{
				if (random.Next(10000) == 0)
				{
					throw new IOException("Fake IOException from TermsConsumer.finishTerm()");
				}
				delegate_.FinishTerm(text, stats);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Finish(long sumTotalTermFreq, long sumDocFreq, int docCount)
			{
				if (random.Next(100) == 0)
				{
					throw new IOException("Fake IOException from TermsConsumer.finish()");
				}
				delegate_.Finish(sumTotalTermFreq, sumDocFreq, docCount);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override IComparer<BytesRef> GetComparator()
			{
				if (random.Next(100) == 0)
				{
					throw new IOException("Fake IOException from TermsConsumer.getComparator()");
				}
				return delegate_.GetComparator();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Merge(MergeState mergeState, FieldInfo.IndexOptions indexOptions
				, TermsEnum termsEnum)
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
			internal readonly PostingsConsumer delegate_;

			internal readonly Random random;

			internal CrankyPostingsConsumer(PostingsConsumer delegate_, Random random)
			{
				this.delegate_ = delegate_;
				this.random = random;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void StartDoc(int docID, int freq)
			{
				if (random.Next(10000) == 0)
				{
					throw new IOException("Fake IOException from PostingsConsumer.startDoc()");
				}
				delegate_.StartDoc(docID, freq);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void FinishDoc()
			{
				if (random.Next(10000) == 0)
				{
					throw new IOException("Fake IOException from PostingsConsumer.finishDoc()");
				}
				delegate_.FinishDoc();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void AddPosition(int position, BytesRef payload, int startOffset, 
				int endOffset)
			{
				if (random.Next(10000) == 0)
				{
					throw new IOException("Fake IOException from PostingsConsumer.addPosition()");
				}
				delegate_.AddPosition(position, payload, startOffset, endOffset);
			}

			/// <exception cref="System.IO.IOException"></exception>
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
