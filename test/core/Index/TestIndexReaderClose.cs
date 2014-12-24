/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	public class TestIndexReaderClose : LuceneTestCase
	{
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestCloseUnderException()
		{
			int iters = 1000 + 1 + Random().Next(20);
			for (int j = 0; j < iters; j++)
			{
				Directory dir = NewDirectory();
				IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(Random(), TEST_VERSION_CURRENT
					, new MockAnalyzer(Random())));
				writer.Commit();
				writer.Close();
				DirectoryReader open = DirectoryReader.Open(dir);
				bool throwOnClose = !Rarely();
				AtomicReader wrap = SlowCompositeReaderWrapper.Wrap(open);
				FilterAtomicReader reader = new _FilterAtomicReader_44(throwOnClose, wrap);
				IList<IndexReader.ReaderClosedListener> listeners = new AList<IndexReader.ReaderClosedListener
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
					reader.Close();
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
					reader.Fields();
					Fail("we are closed");
				}
				catch (AlreadyClosedException)
				{
				}
				if (Random().NextBoolean())
				{
					reader.Close();
				}
				// call it again
				AreEqual(0, count.Get());
				wrap.Close();
				dir.Close();
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
			protected override void DoClose()
			{
				base.DoClose();
				if (throwOnClose)
				{
					throw new InvalidOperationException("BOOM!");
				}
			}

			private readonly bool throwOnClose;
		}

		private sealed class CountListener : IndexReader.ReaderClosedListener
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

		private sealed class FaultyListener : IndexReader.ReaderClosedListener
		{
			public void OnClose(IndexReader reader)
			{
				throw new InvalidOperationException("GRRRRRRRRRRRR!");
			}
		}
	}
}
