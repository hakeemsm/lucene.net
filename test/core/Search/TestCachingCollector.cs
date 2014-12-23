/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using NUnit.Framework;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search
{
	public class TestCachingCollector : LuceneTestCase
	{
		private const double ONE_BYTE = 1.0 / (1024 * 1024);

		private class MockScorer : Scorer
		{
			public MockScorer() : base((Weight)null)
			{
			}

			// 1 byte out of MB
			/// <exception cref="System.IO.IOException"></exception>
			public override float Score()
			{
				return 0;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Freq()
			{
				return 0;
			}

			public override int DocID()
			{
				return 0;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int NextDoc()
			{
				return 0;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Advance(int target)
			{
				return 0;
			}

			public override long Cost()
			{
				return 1;
			}
		}

		private class NoOpCollector : Collector
		{
			private readonly bool acceptDocsOutOfOrder;

			public NoOpCollector(bool acceptDocsOutOfOrder)
			{
				this.acceptDocsOutOfOrder = acceptDocsOutOfOrder;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void SetScorer(Scorer scorer)
			{
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Collect(int doc)
			{
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void SetNextReader(AtomicReaderContext context)
			{
			}

			public override bool AcceptsDocsOutOfOrder()
			{
				return acceptDocsOutOfOrder;
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestBasic()
		{
			foreach (bool cacheScores in new bool[] { false, true })
			{
				CachingCollector cc = CachingCollector.Create(new TestCachingCollector.NoOpCollector
					(false), cacheScores, 1.0);
				cc.SetScorer(new TestCachingCollector.MockScorer());
				// collect 1000 docs
				for (int i = 0; i < 1000; i++)
				{
					cc.Collect(i);
				}
				// now replay them
				cc.Replay(new _Collector_91());
			}
		}

		private sealed class _Collector_91 : Collector
		{
			public _Collector_91()
			{
				this.prevDocID = -1;
			}

			internal int prevDocID;

			public override void SetScorer(Scorer scorer)
			{
			}

			public override void SetNextReader(AtomicReaderContext context)
			{
			}

			public override void Collect(int doc)
			{
				NUnit.Framework.Assert.AreEqual(this.prevDocID + 1, doc);
				this.prevDocID = doc;
			}

			public override bool AcceptsDocsOutOfOrder()
			{
				return false;
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestIllegalStateOnReplay()
		{
			CachingCollector cc = CachingCollector.Create(new TestCachingCollector.NoOpCollector
				(false), true, 50 * ONE_BYTE);
			cc.SetScorer(new TestCachingCollector.MockScorer());
			// collect 130 docs, this should be enough for triggering cache abort.
			for (int i = 0; i < 130; i++)
			{
				cc.Collect(i);
			}
			NUnit.Framework.Assert.IsFalse("CachingCollector should not be cached due to low memory limit"
				, cc.IsCached());
			try
			{
				cc.Replay(new TestCachingCollector.NoOpCollector(false));
				NUnit.Framework.Assert.Fail("replay should fail if CachingCollector is not cached"
					);
			}
			catch (InvalidOperationException)
			{
			}
		}

		// expected
		/// <exception cref="System.Exception"></exception>
		public virtual void TestIllegalCollectorOnReplay()
		{
			// tests that the Collector passed to replay() has an out-of-order mode that
			// is valid with the Collector passed to the ctor
			// 'src' Collector does not support out-of-order
			CachingCollector cc = CachingCollector.Create(new TestCachingCollector.NoOpCollector
				(false), true, 50 * ONE_BYTE);
			cc.SetScorer(new TestCachingCollector.MockScorer());
			for (int i = 0; i < 10; i++)
			{
				cc.Collect(i);
			}
			cc.Replay(new TestCachingCollector.NoOpCollector(true));
			// this call should not fail
			cc.Replay(new TestCachingCollector.NoOpCollector(false));
			// this call should not fail
			// 'src' Collector supports out-of-order
			cc = CachingCollector.Create(new TestCachingCollector.NoOpCollector(true), true, 
				50 * ONE_BYTE);
			cc.SetScorer(new TestCachingCollector.MockScorer());
			for (int i_1 = 0; i_1 < 10; i_1++)
			{
				cc.Collect(i_1);
			}
			cc.Replay(new TestCachingCollector.NoOpCollector(true));
			// this call should not fail
			try
			{
				cc.Replay(new TestCachingCollector.NoOpCollector(false));
				// this call should fail
				NUnit.Framework.Assert.Fail("should have failed if an in-order Collector was given to replay(), "
					 + "while CachingCollector was initialized with out-of-order collection");
			}
			catch (ArgumentException)
			{
			}
		}

		// ok
		/// <exception cref="System.Exception"></exception>
		public virtual void TestCachedArraysAllocation()
		{
			// tests the cached arrays allocation -- if the 'nextLength' was too high,
			// caching would terminate even if a smaller length would suffice.
			// set RAM limit enough for 150 docs + random(10000)
			int numDocs = Random().Next(10000) + 150;
			foreach (bool cacheScores in new bool[] { false, true })
			{
				int bytesPerDoc = cacheScores ? 8 : 4;
				CachingCollector cc = CachingCollector.Create(new TestCachingCollector.NoOpCollector
					(false), cacheScores, bytesPerDoc * ONE_BYTE * numDocs);
				cc.SetScorer(new TestCachingCollector.MockScorer());
				for (int i = 0; i < numDocs; i++)
				{
					cc.Collect(i);
				}
				NUnit.Framework.Assert.IsTrue(cc.IsCached());
				// The 151's document should terminate caching
				cc.Collect(numDocs);
				NUnit.Framework.Assert.IsFalse(cc.IsCached());
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestNoWrappedCollector()
		{
			foreach (bool cacheScores in new bool[] { false, true })
			{
				// create w/ null wrapped collector, and test that the methods work
				CachingCollector cc = CachingCollector.Create(true, cacheScores, 50 * ONE_BYTE);
				cc.SetNextReader(null);
				cc.SetScorer(new TestCachingCollector.MockScorer());
				cc.Collect(0);
				NUnit.Framework.Assert.IsTrue(cc.IsCached());
				cc.Replay(new TestCachingCollector.NoOpCollector(true));
			}
		}
	}
}
