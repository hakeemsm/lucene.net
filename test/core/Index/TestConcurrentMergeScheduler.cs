/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.IO;
using NUnit.Framework;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Codecs.Lucene41;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Test.Index
{
	public class TestConcurrentMergeScheduler : LuceneTestCase
	{
		private class FailOnlyOnFlush : MockDirectoryWrapper.Failure
		{
			internal bool doFail;

			internal bool hitExc;

			public override void SetDoFail()
			{
				this.doFail = true;
				this.hitExc = false;
			}

			public override void ClearDoFail()
			{
				this.doFail = false;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Eval(MockDirectoryWrapper dir)
			{
				if (this.doFail && this._enclosing.IsTestThread())
				{
					bool isDoFlush = false;
					bool isClose = false;
					StackTraceElement[] trace = new Exception().GetStackTrace();
					for (int i = 0; i < trace.Length; i++)
					{
						if (isDoFlush && isClose)
						{
							break;
						}
						if ("flush".Equals(trace[i].GetMethodName()))
						{
							isDoFlush = true;
						}
						if ("close".Equals(trace[i].GetMethodName()))
						{
							isClose = true;
						}
					}
					if (isDoFlush && !isClose && LuceneTestCase.Random().NextBoolean())
					{
						this.hitExc = true;
						throw new IOException(Sharpen.Thread.CurrentThread().GetName() + ": now failing during flush"
							);
					}
				}
			}

			internal FailOnlyOnFlush(TestConcurrentMergeScheduler _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly TestConcurrentMergeScheduler _enclosing;
		}

		// Make sure running BG merges still work fine even when
		// we are hitting exceptions during flushing.
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestFlushExceptions()
		{
			MockDirectoryWrapper directory = NewMockDirectory();
			TestConcurrentMergeScheduler.FailOnlyOnFlush failure = new TestConcurrentMergeScheduler.FailOnlyOnFlush
				(this);
			directory.FailOn(failure);
			IndexWriter writer = new IndexWriter(directory, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2)));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			Field idField = NewStringField("id", string.Empty, Field.Store.YES);
			doc.Add(idField);
			int extraCount = 0;
			for (int i = 0; i < 10; i++)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: iter=" + i);
				}
				for (int j = 0; j < 20; j++)
				{
					idField.StringValue = Sharpen.Extensions.ToString(i * 20 + j));
					writer.AddDocument(doc);
				}
				// must cycle here because sometimes the merge flushes
				// the doc we just added and so there's nothing to
				// flush, and we don't hit the exception
				while (true)
				{
					writer.AddDocument(doc);
					failure.SetDoFail();
					try
					{
						writer.Flush(true, true);
						if (failure.hitExc)
						{
							Fail("failed to hit IOException");
						}
						extraCount++;
					}
					catch (IOException ioe)
					{
						if (VERBOSE)
						{
							Sharpen.Runtime.PrintStackTrace(ioe, System.Console.Out);
						}
						failure.ClearDoFail();
						break;
					}
				}
				AreEqual(20 * (i + 1) + extraCount, writer.NumDocs);
			}
			writer.Dispose();
			IndexReader reader = DirectoryReader.Open(directory);
			AreEqual(200 + extraCount, reader.NumDocs);
			reader.Dispose();
			directory.Dispose();
		}

		// Test that deletes committed after a merge started and
		// before it finishes, are correctly merged back:
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestDeleteMerging()
		{
			Directory directory = NewDirectory();
			LogDocMergePolicy mp = new LogDocMergePolicy();
			// Force degenerate merging so we can get a mix of
			// merging of segments with and without deletes at the
			// start:
			mp.SetMinMergeDocs(1000);
			IndexWriter writer = new IndexWriter(directory, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetMergePolicy(mp));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			Field idField = NewStringField("id", string.Empty, Field.Store.YES);
			doc.Add(idField);
			for (int i = 0; i < 10; i++)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("\nTEST: cycle");
				}
				for (int j = 0; j < 100; j++)
				{
					idField.StringValue = Sharpen.Extensions.ToString(i * 100 + j));
					writer.AddDocument(doc);
				}
				int delID = i;
				while (delID < 100 * (1 + i))
				{
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("TEST: del " + delID);
					}
					writer.DeleteDocuments(new Term("id", string.Empty + delID));
					delID += 10;
				}
				writer.Commit();
			}
			writer.Dispose();
			IndexReader reader = DirectoryReader.Open(directory);
			// Verify that we did not lose any deletes...
			AreEqual(450, reader.NumDocs);
			reader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestNoExtraFiles()
		{
			Directory directory = NewDirectory();
			IndexWriter writer = new IndexWriter(directory, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2)));
			for (int iter = 0; iter < 7; iter++)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: iter=" + iter);
				}
				for (int j = 0; j < 21; j++)
				{
					Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
						();
					doc.Add(NewTextField("content", "a b c", Field.Store.NO));
					writer.AddDocument(doc);
				}
				writer.Dispose();
				TestIndexWriter.AssertNoUnreferencedFiles(directory, "testNoExtraFiles");
				// Reopen
				writer = new IndexWriter(directory, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
					, new MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND).SetMaxBufferedDocs
					(2)));
			}
			writer.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestNoWaitClose()
		{
			Directory directory = NewDirectory();
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			Field idField = NewStringField("id", string.Empty, Field.Store.YES);
			doc.Add(idField);
			IndexWriter writer = new IndexWriter(directory, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2)).SetMergePolicy
				(NewLogMergePolicy(100)));
			for (int iter = 0; iter < 10; iter++)
			{
				for (int j = 0; j < 201; j++)
				{
					idField.StringValue = Sharpen.Extensions.ToString(iter * 201 + j));
					writer.AddDocument(doc);
				}
				int delID = iter * 201;
				for (int j_1 = 0; j_1 < 20; j_1++)
				{
					writer.DeleteDocuments(new Term("id", Sharpen.Extensions.ToString(delID)));
					delID += 5;
				}
				// Force a bunch of merge threads to kick off so we
				// stress out aborting them on close:
				((LogMergePolicy)writer.Config.MergePolicy).MergeFactor = (3);
				writer.AddDocument(doc);
				writer.Commit();
				writer.Close(false);
				IndexReader reader = DirectoryReader.Open(directory);
				AreEqual((1 + iter) * 182, reader.NumDocs);
				reader.Dispose();
				// Reopen
				writer = new IndexWriter(directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
					MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND).SetMergePolicy
					(NewLogMergePolicy(100)));
			}
			writer.Dispose();
			directory.Dispose();
		}

		// LUCENE-4544
		/// <exception cref="System.Exception"></exception>
		public virtual void TestMaxMergeCount()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			int maxMergeCount = TestUtil.NextInt(Random(), 1, 5);
			int maxMergeThreads = TestUtil.NextInt(Random(), 1, maxMergeCount);
			CountDownLatch enoughMergesWaiting = new CountDownLatch(maxMergeCount);
			AtomicInteger runningMergeCount = new AtomicInteger(0);
			AtomicBoolean failed = new AtomicBoolean();
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: maxMergeCount=" + maxMergeCount + " maxMergeThreads="
					 + maxMergeThreads);
			}
			ConcurrentMergeScheduler cms = new _ConcurrentMergeScheduler_275(runningMergeCount
				, maxMergeCount, enoughMergesWaiting, failed);
			// Stall all incoming merges until we see
			// maxMergeCount:
			// Stall this merge until we see exactly
			// maxMergeCount merges waiting
			// Then sleep a bit to give a chance for the bug
			// (too many pending merges) to appear:
			cms.SetMaxMergesAndThreads(maxMergeCount, maxMergeThreads);
			iwc.SetMergeScheduler(cms);
			iwc.SetMaxBufferedDocs(2);
			TieredMergePolicy tmp = new TieredMergePolicy();
			iwc.SetMergePolicy(tmp);
			tmp.SetMaxMergeAtOnce(2);
			tmp.SetSegmentsPerTier(2);
			IndexWriter w = new IndexWriter(dir, iwc);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewField("field", "field", TextField.TYPE_NOT_STORED));
			while (enoughMergesWaiting.GetCount() != 0 && !failed.Get())
			{
				for (int i = 0; i < 10; i++)
				{
					w.AddDocument(doc);
				}
			}
			w.Close(false);
			dir.Dispose();
		}

		private sealed class _ConcurrentMergeScheduler_275 : ConcurrentMergeScheduler
		{
			public _ConcurrentMergeScheduler_275(AtomicInteger runningMergeCount, int maxMergeCount
				, CountDownLatch enoughMergesWaiting, AtomicBoolean failed)
			{
				this.runningMergeCount = runningMergeCount;
				this.maxMergeCount = maxMergeCount;
				this.enoughMergesWaiting = enoughMergesWaiting;
				this.failed = failed;
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected override void DoMerge(MergePolicy.OneMerge merge)
			{
				try
				{
					int count = runningMergeCount.IncrementAndGet();
					try
					{
						IsTrue("count=" + count + " vs maxMergeCount=" + maxMergeCount
							, count <= maxMergeCount);
						enoughMergesWaiting.CountDown();
						while (true)
						{
							if (enoughMergesWaiting.Await(10, TimeUnit.MILLISECONDS) || failed.Get())
							{
								break;
							}
						}
						Sharpen.Thread.Sleep(20);
						base.DoMerge(merge);
					}
					finally
					{
						runningMergeCount.DecrementAndGet();
					}
				}
				catch (Exception t)
				{
					failed.Set(true);
					this.writer.MergeFinish(merge);
					throw new RuntimeException(t);
				}
			}

			private readonly AtomicInteger runningMergeCount;

			private readonly int maxMergeCount;

			private readonly CountDownLatch enoughMergesWaiting;

			private readonly AtomicBoolean failed;
		}

		private class TrackingCMS : ConcurrentMergeScheduler
		{
			internal long totMergedBytes;

			public TrackingCMS()
			{
				SetMaxMergesAndThreads(5, 5);
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected override void DoMerge(MergePolicy.OneMerge merge)
			{
				totMergedBytes += merge.TotalBytesSize();
				base.DoMerge(merge);
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestTotalBytesSize()
		{
			Directory d = NewDirectory();
			if (d is MockDirectoryWrapper)
			{
				((MockDirectoryWrapper)d).SetThrottling(MockDirectoryWrapper.Throttling.NEVER);
			}
			IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			iwc.SetMaxBufferedDocs(5);
			iwc.SetMergeScheduler(new TestConcurrentMergeScheduler.TrackingCMS());
			if (TestUtil.GetPostingsFormat("id").Equals("SimpleText"))
			{
				// no
				iwc.SetCodec(TestUtil.AlwaysPostingsFormat(new Lucene41PostingsFormat()));
			}
			IndexWriter w = new IndexWriter(d, iwc);
			for (int i = 0; i < 1000; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(new StringField("id", string.Empty + i, Field.Store.NO));
				w.AddDocument(doc);
				if (Random().NextBoolean())
				{
					w.DeleteDocuments(new Term("id", string.Empty + Random().Next(i + 1)));
				}
			}
			IsTrue(((TestConcurrentMergeScheduler.TrackingCMS)w.GetConfig
				().GetMergeScheduler()).totMergedBytes != 0);
			w.Dispose();
			d.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestLiveMaxMergeCount()
		{
			Directory d = NewDirectory();
			IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			TieredMergePolicy tmp = new TieredMergePolicy();
			tmp.SetSegmentsPerTier(1000);
			tmp.SetMaxMergeAtOnce(1000);
			tmp.SetMaxMergeAtOnceExplicit(10);
			iwc.SetMergePolicy(tmp);
			iwc.SetMaxBufferedDocs(2);
			iwc.SetRAMBufferSizeMB(-1);
			AtomicInteger maxRunningMergeCount = new AtomicInteger();
			ConcurrentMergeScheduler cms = new _ConcurrentMergeScheduler_384(maxRunningMergeCount
				);
			// evil?
			cms.SetMaxMergesAndThreads(5, 3);
			iwc.SetMergeScheduler(cms);
			IndexWriter w = new IndexWriter(d, iwc);
			// Makes 100 segments
			for (int i = 0; i < 200; i++)
			{
				w.AddDocument(new Lucene.Net.Documents.Document());
			}
			// No merges should have run so far, because TMP has high segmentsPerTier:
			AreEqual(0, maxRunningMergeCount.Get());
			w.ForceMerge(1);
			// At most 5 merge threads should have launched at once:
			IsTrue("maxRunningMergeCount=" + maxRunningMergeCount, maxRunningMergeCount
				.Get() <= 5);
			maxRunningMergeCount.Set(0);
			// Makes another 100 segments
			for (int i_1 = 0; i_1 < 200; i_1++)
			{
				w.AddDocument(new Lucene.Net.Documents.Document());
			}
			((ConcurrentMergeScheduler)w.Config.GetMergeScheduler()).SetMaxMergesAndThreads
				(1, 1);
			w.ForceMerge(1);
			// At most 1 merge thread should have launched at once:
			AreEqual(1, maxRunningMergeCount.Get());
			w.Dispose();
			d.Dispose();
		}

		private sealed class _ConcurrentMergeScheduler_384 : ConcurrentMergeScheduler
		{
			public _ConcurrentMergeScheduler_384(AtomicInteger maxRunningMergeCount)
			{
				this.maxRunningMergeCount = maxRunningMergeCount;
				this.runningMergeCount = new AtomicInteger();
			}

			internal readonly AtomicInteger runningMergeCount;

			/// <exception cref="System.IO.IOException"></exception>
			protected override void DoMerge(MergePolicy.OneMerge merge)
			{
				int count = this.runningMergeCount.IncrementAndGet();
				lock (this)
				{
					if (count > maxRunningMergeCount.Get())
					{
						maxRunningMergeCount.Set(count);
					}
				}
				try
				{
					base.DoMerge(merge);
				}
				finally
				{
					this.runningMergeCount.DecrementAndGet();
				}
			}

			private readonly AtomicInteger maxRunningMergeCount;
		}
	}
}
