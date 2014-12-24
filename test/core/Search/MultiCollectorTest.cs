/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search
{
	public class MultiCollectorTest : LuceneTestCase
	{
		private class DummyCollector : Collector
		{
			internal bool acceptsDocsOutOfOrderCalled = false;

			internal bool collectCalled = false;

			internal bool setNextReaderCalled = false;

			internal bool setScorerCalled = false;

			public override bool AcceptsDocsOutOfOrder()
			{
				acceptsDocsOutOfOrderCalled = true;
				return true;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Collect(int doc)
			{
				collectCalled = true;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void SetNextReader(AtomicReaderContext context)
			{
				setNextReaderCalled = true;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void SetScorer(Scorer scorer)
			{
				setScorerCalled = true;
			}
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestNullCollectors()
		{
			// Tests that the collector rejects all null collectors.
			try
			{
				MultiCollector.Wrap(null, null);
				Fail("only null collectors should not be supported");
			}
			catch (ArgumentException)
			{
			}
			// expected
			// Tests that the collector handles some null collectors well. If it
			// doesn't, an NPE would be thrown.
			Collector c = MultiCollector.Wrap(new MultiCollectorTest.DummyCollector(), null, 
				new MultiCollectorTest.DummyCollector());
			IsTrue(c is MultiCollector);
			IsTrue(c.AcceptsDocsOutOfOrder());
			c.Collect(1);
			c.SetNextReader(null);
			c.SetScorer(null);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestSingleCollector()
		{
			// Tests that if a single Collector is input, it is returned (and not MultiCollector).
			MultiCollectorTest.DummyCollector dc = new MultiCollectorTest.DummyCollector();
			AreSame(dc, MultiCollector.Wrap(dc));
			AreSame(dc, MultiCollector.Wrap(dc, null));
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestCollector()
		{
			// Tests that the collector delegates calls to input collectors properly.
			// Tests that the collector handles some null collectors well. If it
			// doesn't, an NPE would be thrown.
			MultiCollectorTest.DummyCollector[] dcs = new MultiCollectorTest.DummyCollector[]
				 { new MultiCollectorTest.DummyCollector(), new MultiCollectorTest.DummyCollector
				() };
			Collector c = MultiCollector.Wrap(dcs);
			IsTrue(c.AcceptsDocsOutOfOrder());
			c.Collect(1);
			c.SetNextReader(null);
			c.SetScorer(null);
			foreach (MultiCollectorTest.DummyCollector dc in dcs)
			{
				IsTrue(dc.acceptsDocsOutOfOrderCalled);
				IsTrue(dc.collectCalled);
				IsTrue(dc.setNextReaderCalled);
				IsTrue(dc.setScorerCalled);
			}
		}
	}
}
