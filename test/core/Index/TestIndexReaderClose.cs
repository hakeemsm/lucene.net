using System;
using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.TestFramework;
using NUnit.Framework;

namespace Lucene.Net.Test.Index
{
    [TestFixture]
	public class TestIndexReaderClose : LuceneTestCase
	{
		[Test]
		public virtual void TestCloseUnderException()
		{
			int iters = 1000 + 1 + Random().Next(20);
			for (int j = 0; j < iters; j++)
			{
				Directory dir = NewDirectory();
				IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(Random(), TEST_VERSION_CURRENT
					, new MockAnalyzer(Random())));
				writer.Commit();
				writer.Dispose();
				DirectoryReader open = DirectoryReader.Open(dir);
				bool throwOnClose = !Rarely();
				AtomicReader wrap = SlowCompositeReaderWrapper.Wrap(open);
				FilterAtomicReader reader = new _FilterAtomicReader_44(throwOnClose, wrap);
				IList<IndexReader.IReaderClosedListener> listeners = new List<IndexReader.IReaderClosedListener
					>();
				int listenerCount = Random().Next(20);
				AtomicInteger count = new AtomicInteger();
				bool faultySet = false;
				for (int i = 0; i < listenerCount; i++)
				{
					if (Rarely())
					{
						faultySet = true;
						reader.AddReaderClosedListener(new TestIndexReaderClose.FaultyListener());
					}
					else
					{
						count.IncrementAndGet();
						reader.AddReaderClosedListener(new TestIndexReaderClose.CountListener(count));
					}
				}
				if (!faultySet && !throwOnClose)
				{
					reader.AddReaderClosedListener(new TestIndexReaderClose.FaultyListener());
				}
				try
				{
					reader.Dispose();
					Fail("expected Exception");
				}
				catch (InvalidOperationException ex)
				{
					if (throwOnClose)
					{
						AreEqual("BOOM!", ex.Message);
					}
					else
					{
						AreEqual("GRRRRRRRRRRRR!", ex.Message);
					}
				}
				try
				{
				    var fields = reader.Fields;
				    Fail("we are closed");
				}
				catch (AlreadyClosedException)
				{
				}
				if (Random().NextBoolean())
				{
					reader.Dispose();
				}
				// call it again
				AreEqual(0, count.Get());
				wrap.Dispose();
				dir.Dispose();
			}
		}

		private sealed class _FilterAtomicReader_44 : FilterAtomicReader
		{
			public _FilterAtomicReader_44(bool throwOnClose, AtomicReader baseArg1) : base(baseArg1
				)
			{
				this.throwOnClose = throwOnClose;
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected internal override void DoClose()
			{
				base.DoClose();
				if (throwOnClose)
				{
					throw new InvalidOperationException("BOOM!");
				}
			}

			private readonly bool throwOnClose;
		}

		private sealed class CountListener : IndexReader.IReaderClosedListener
		{
			private readonly AtomicInteger count;

			public CountListener(AtomicInteger count)
			{
				this.count = count;
			}

			public void OnClose(IndexReader reader)
			{
				count.DecrementAndGet();
			}
		}

		private sealed class FaultyListener : IndexReader.IReaderClosedListener
		{
			public void OnClose(IndexReader reader)
			{
				throw new InvalidOperationException("GRRRRRRRRRRRR!");
			}
		}
	}
}
