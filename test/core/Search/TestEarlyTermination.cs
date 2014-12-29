/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using NUnit.Framework;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search
{
	public class TestEarlyTermination : LuceneTestCase
	{
		internal Directory dir;

		internal RandomIndexWriter writer;

		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			dir = NewDirectory();
			writer = new RandomIndexWriter(Random(), dir);
			int numDocs = AtLeast(100);
			for (int i = 0; i < numDocs; i++)
			{
				writer.AddDocument(new Lucene.Net.Documents.Document());
				if (Rarely())
				{
					writer.Commit();
				}
			}
		}

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			base.TearDown();
			writer.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestEarlyTermination()
		{
			int iters = AtLeast(5);
			IndexReader reader = writer.Reader;
			for (int i = 0; i < iters; ++i)
			{
				IndexSearcher searcher = NewSearcher(reader);
				Collector collector = new _Collector_61();
				searcher.Search(new MatchAllDocsQuery(), collector);
			}
			reader.Dispose();
		}

		private sealed class _Collector_61 : Collector
		{
			public _Collector_61()
			{
				this.outOfOrder = LuceneTestCase.Random().NextBoolean();
				this.collectionTerminated = true;
			}

			internal readonly bool outOfOrder;

			internal bool collectionTerminated;

			/// <exception cref="System.IO.IOException"></exception>
			public override void SetScorer(Scorer scorer)
			{
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Collect(int doc)
			{
				IsFalse(this.collectionTerminated);
				if (LuceneTestCase.Rarely())
				{
					this.collectionTerminated = true;
					throw new CollectionTerminatedException();
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void SetNextReader(AtomicReaderContext context)
			{
				if (LuceneTestCase.Random().NextBoolean())
				{
					this.collectionTerminated = true;
					throw new CollectionTerminatedException();
				}
				else
				{
					this.collectionTerminated = false;
				}
			}

			public override bool AcceptsDocsOutOfOrder()
			{
				return this.outOfOrder;
			}
		}
	}
}
