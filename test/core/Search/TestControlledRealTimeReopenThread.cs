/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;


namespace Lucene.Net.Search
{
	public class TestControlledRealTimeReopenThread : ThreadedIndexingAndSearchingTestCase
	{
		private SearcherManager nrtNoDeletes;

		private SearcherManager nrtDeletes;

		private TrackingIndexWriter genWriter;

		private ControlledRealTimeReopenThread<IndexSearcher> nrtDeletesThread;

		private ControlledRealTimeReopenThread<IndexSearcher> nrtNoDeletesThread;

		private readonly ThreadLocal<long> lastGens = new ThreadLocal<long>();

		private bool warmCalled;

		// Not guaranteed to reflect deletes:
		// Is guaranteed to reflect deletes:
		/// <exception cref="System.Exception"></exception>
		public virtual void TestControlledRealTimeReopenThread()
		{
			RunTest("TestControlledRealTimeReopenThread");
		}

		/// <exception cref="System.Exception"></exception>
		protected override IndexSearcher GetFinalSearcher()
		{
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: finalSearcher maxGen=" + maxGen);
			}
			nrtDeletesThread.WaitForGeneration(maxGen);
			return nrtDeletes.Acquire();
		}

		protected override Directory GetDirectory(Directory @in)
		{
			// Randomly swap in NRTCachingDir
			if (Random().NextBoolean())
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: wrap NRTCachingDir");
				}
				return new NRTCachingDirectory(@in, 5.0, 60.0);
			}
			else
			{
				return @in;
			}
		}

		/// <exception cref="System.Exception"></exception>
		protected override void UpdateDocuments<_T0>(Term id, IList<_T0> docs)
		{
			long gen = genWriter.UpdateDocuments(id, docs.AsIterable());
			// Randomly verify the update "took":
			if (Random().Next(20) == 2)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine(Thread.CurrentThread.Name + ": nrt: verify "
						 + id);
				}
				nrtDeletesThread.WaitForGeneration(gen);
				IndexSearcher s = nrtDeletes.Acquire();
				if (VERBOSE)
				{
					System.Console.Out.WriteLine(Thread.CurrentThread.Name + ": nrt: got searcher="
						 + s);
				}
				try
				{
					AreEqual(docs.Count, s.Search(new TermQuery(id), 10).TotalHits
						);
				}
				finally
				{
					nrtDeletes.Release(s);
				}
			}
			lastGens.Set(gen);
		}

		/// <exception cref="System.Exception"></exception>
		protected override void AddDocuments<_T0>(Term id, IList<_T0> docs)
		{
			long gen = genWriter.AddDocuments(docs.AsIterable());
			// Randomly verify the add "took":
			if (Random().Next(20) == 2)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine(Thread.CurrentThread.Name + ": nrt: verify "
						 + id);
				}
				nrtNoDeletesThread.WaitForGeneration(gen);
				IndexSearcher s = nrtNoDeletes.Acquire();
				if (VERBOSE)
				{
					System.Console.Out.WriteLine(Thread.CurrentThread.Name + ": nrt: got searcher="
						 + s);
				}
				try
				{
					AreEqual(docs.Count, s.Search(new TermQuery(id), 10).TotalHits
						);
				}
				finally
				{
					nrtNoDeletes.Release(s);
				}
			}
			lastGens.Set(gen);
		}

		/// <exception cref="System.Exception"></exception>
		protected override void AddDocument<_T0>(Term id, IEnumerable<_T0> doc)
		{
			long gen = genWriter.AddDocument(doc);
			// Randomly verify the add "took":
			if (Random().Next(20) == 2)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine(Thread.CurrentThread.Name + ": nrt: verify "
						 + id);
				}
				nrtNoDeletesThread.WaitForGeneration(gen);
				IndexSearcher s = nrtNoDeletes.Acquire();
				if (VERBOSE)
				{
					System.Console.Out.WriteLine(Thread.CurrentThread.Name + ": nrt: got searcher="
						 + s);
				}
				try
				{
					AreEqual(1, s.Search(new TermQuery(id), 10).TotalHits);
				}
				finally
				{
					nrtNoDeletes.Release(s);
				}
			}
			lastGens.Set(gen);
		}

		/// <exception cref="System.Exception"></exception>
		protected override void UpdateDocument<_T0>(Term id, IEnumerable<_T0> doc)
		{
			long gen = genWriter.UpdateDocument(id, doc);
			// Randomly verify the udpate "took":
			if (Random().Next(20) == 2)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine(Thread.CurrentThread.Name + ": nrt: verify "
						 + id);
				}
				nrtDeletesThread.WaitForGeneration(gen);
				IndexSearcher s = nrtDeletes.Acquire();
				if (VERBOSE)
				{
					System.Console.Out.WriteLine(Thread.CurrentThread.Name + ": nrt: got searcher="
						 + s);
				}
				try
				{
					AreEqual(1, s.Search(new TermQuery(id), 10).TotalHits);
				}
				finally
				{
					nrtDeletes.Release(s);
				}
			}
			lastGens.Set(gen);
		}

		/// <exception cref="System.Exception"></exception>
		protected override void DeleteDocuments(Term id)
		{
			long gen = genWriter.DeleteDocuments(id);
			// randomly verify the delete "took":
			if (Random().Next(20) == 7)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine(Thread.CurrentThread.Name + ": nrt: verify del "
						 + id);
				}
				nrtDeletesThread.WaitForGeneration(gen);
				IndexSearcher s = nrtDeletes.Acquire();
				if (VERBOSE)
				{
					System.Console.Out.WriteLine(Thread.CurrentThread.Name + ": nrt: got searcher="
						 + s);
				}
				try
				{
					AreEqual(0, s.Search(new TermQuery(id), 10).TotalHits);
				}
				finally
				{
					nrtDeletes.Release(s);
				}
			}
			lastGens.Set(gen);
		}

		/// <exception cref="System.Exception"></exception>
		protected override void DoAfterWriter(ExecutorService es)
		{
			double minReopenSec = 0.01 + 0.05 * Random().NextDouble();
			double maxReopenSec = minReopenSec * (1.0 + 10 * Random().NextDouble());
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: make SearcherManager maxReopenSec=" + maxReopenSec
					 + " minReopenSec=" + minReopenSec);
			}
			genWriter = new TrackingIndexWriter(writer);
			SearcherFactory sf = new _SearcherFactory_222(this, es);
			nrtNoDeletes = new SearcherManager(writer, false, sf);
			nrtDeletes = new SearcherManager(writer, true, sf);
			nrtDeletesThread = new ControlledRealTimeReopenThread<IndexSearcher>(genWriter, nrtDeletes
				, maxReopenSec, minReopenSec);
			nrtDeletesThread.SetName("NRTDeletes Reopen Thread");
			nrtDeletesThread.SetPriority(Math.Min(Thread.CurrentThread().GetPriority(
				) + 2, Thread.MAX_PRIORITY));
			nrtDeletesThread.SetDaemon(true);
			nrtDeletesThread.Start();
			nrtNoDeletesThread = new ControlledRealTimeReopenThread<IndexSearcher>(genWriter, 
				nrtNoDeletes, maxReopenSec, minReopenSec);
			nrtNoDeletesThread.SetName("NRTNoDeletes Reopen Thread");
			nrtNoDeletesThread.SetPriority(Math.Min(Thread.CurrentThread().GetPriority
				() + 2, Thread.MAX_PRIORITY));
			nrtNoDeletesThread.SetDaemon(true);
			nrtNoDeletesThread.Start();
		}

		private sealed class _SearcherFactory_222 : SearcherFactory
		{
			public _SearcherFactory_222(TestControlledRealTimeReopenThread _enclosing, ExecutorService
				 es)
			{
				this._enclosing = _enclosing;
				this.es = es;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override IndexSearcher NewSearcher(IndexReader r)
			{
				this._enclosing.warmCalled = true;
				IndexSearcher s = new IndexSearcher(r, es);
				s.Search(new TermQuery(new Term("body", "united")), 10);
				return s;
			}

			private readonly TestControlledRealTimeReopenThread _enclosing;

			private readonly ExecutorService es;
		}

		protected override void DoAfterIndexingThreadDone()
		{
			long gen = lastGens.Get();
			if (gen != null)
			{
				AddMaxGen(gen);
			}
		}

		private long maxGen = -1;

		private void AddMaxGen(long gen)
		{
			lock (this)
			{
				maxGen = Math.Max(gen, maxGen);
			}
		}

		/// <exception cref="System.Exception"></exception>
		protected override void DoSearching(ExecutorService es, long stopTime)
		{
			RunSearchThreads(stopTime);
		}

		/// <exception cref="System.Exception"></exception>
		protected override IndexSearcher GetCurrentSearcher()
		{
			// Test doesn't 
			//HM:revisit 
			//assert deletions until the end, so we
			// can randomize whether dels must be applied
			SearcherManager nrt;
			if (Random().NextBoolean())
			{
				nrt = nrtDeletes;
			}
			else
			{
				nrt = nrtNoDeletes;
			}
			return nrt.Acquire();
		}

		/// <exception cref="System.Exception"></exception>
		protected override void ReleaseSearcher(IndexSearcher s)
		{
			// NOTE: a bit iffy... technically you should release
			// against the same SearcherManager you acquired from... but
			// both impls just decRef the underlying reader so we
			// can get away w/ cheating:
			nrtNoDeletes.Release(s);
		}

		/// <exception cref="System.Exception"></exception>
		protected override void DoClose()
		{
			IsTrue(warmCalled);
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: now close SearcherManagers");
			}
			nrtDeletesThread.Dispose();
			nrtDeletes.Dispose();
			nrtNoDeletesThread.Dispose();
			nrtNoDeletes.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestThreadStarvationNoDeleteNRTReader()
		{
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			conf.SetMergePolicy(Random().NextBoolean() ? NoMergePolicy.COMPOUND_FILES : NoMergePolicy
				.NO_COMPOUND_FILES);
			Directory d = NewDirectory();
			CountdownEvent latch = new CountdownEvent(1);
			CountdownEvent signal = new CountdownEvent(1);
			TestControlledRealTimeReopenThread.LatchedIndexWriter _writer = new TestControlledRealTimeReopenThread.LatchedIndexWriter
				(d, conf, latch, signal);
			TrackingIndexWriter writer = new TrackingIndexWriter(_writer);
			SearcherManager manager = new SearcherManager(_writer, false, null);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewTextField("test", "test", Field.Store.YES));
			writer.AddDocument(doc);
			manager.MaybeRefresh();
			Thread t = new _Thread_321(signal, manager, writer, latch);
			// kick off another reopen so we inc. the internal gen
			// let the add below finish
			t.Start();
			_writer.waitAfterUpdate = true;
			// wait in addDocument to let some reopens go through
			long lastGen = writer.UpdateDocument(new Term("foo", "bar"), doc);
			// once this returns the doc is already reflected in the last reopen
			IsFalse(manager.IsSearcherCurrent());
			// false since there is a delete in the queue
			IndexSearcher searcher = manager.Acquire();
			try
			{
				AreEqual(2, searcher.IndexReader.NumDocs);
			}
			finally
			{
				manager.Release(searcher);
			}
			ControlledRealTimeReopenThread<IndexSearcher> thread = new ControlledRealTimeReopenThread
				<IndexSearcher>(writer, manager, 0.01, 0.01);
			thread.Start();
			// start reopening
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("waiting now for generation " + lastGen);
			}
			AtomicBoolean finished = new AtomicBoolean(false);
			Thread waiter = new _Thread_355(thread, lastGen, finished);
			waiter.Start();
			manager.MaybeRefresh();
			waiter.Join(1000);
			if (!finished.Get())
			{
				waiter.Interrupt();
				Fail("thread deadlocked on waitForGeneration");
			}
			thread.Dispose();
			thread.Join();
			IOUtils.Close(manager, _writer, d);
		}

		private sealed class _Thread_321 : Thread
		{
			public _Thread_321(CountdownEvent signal, SearcherManager manager, TrackingIndexWriter
				 writer, CountdownEvent latch)
			{
				this.signal = signal;
				this.manager = manager;
				this.writer = writer;
				this.latch = latch;
			}

			public override void Run()
			{
				try
				{
					signal.Await();
					manager.MaybeRefresh();
					writer.DeleteDocuments(new TermQuery(new Term("foo", "barista")));
					manager.MaybeRefresh();
				}
				catch (Exception e)
				{
					Runtime.PrintStackTrace(e);
				}
				finally
				{
					latch.CountDown();
				}
			}

			private readonly CountdownEvent signal;

			private readonly SearcherManager manager;

			private readonly TrackingIndexWriter writer;

			private readonly CountdownEvent latch;
		}

		private sealed class _Thread_355 : Thread
		{
			public _Thread_355(ControlledRealTimeReopenThread<IndexSearcher> thread, long lastGen
				, AtomicBoolean finished)
			{
				this.thread = thread;
				this.lastGen = lastGen;
				this.finished = finished;
			}

			public override void Run()
			{
				try
				{
					thread.WaitForGeneration(lastGen);
				}
				catch (Exception ie)
				{
					Thread.CurrentThread().Interrupt();
					throw new SystemException(ie);
				}
				finished.Set(true);
			}

			private readonly ControlledRealTimeReopenThread<IndexSearcher> thread;

			private readonly long lastGen;

			private readonly AtomicBoolean finished;
		}

		public class LatchedIndexWriter : IndexWriter
		{
			private CountdownEvent latch;

			internal bool waitAfterUpdate = false;

			private CountdownEvent signal;

			/// <exception cref="System.IO.IOException"></exception>
			public LatchedIndexWriter(Directory d, IndexWriterConfig conf, CountdownEvent latch
				, CountdownEvent signal) : base(d, conf)
			{
				this.latch = latch;
				this.signal = signal;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void UpdateDocument<_T0>(Term term, IEnumerable<_T0> doc, Analyzer analyzer
				)
			{
				base.UpdateDocument(term, doc, analyzer);
				try
				{
					if (waitAfterUpdate)
					{
						signal.CountDown();
						latch.Await();
					}
				}
				catch (Exception e)
				{
					throw new ThreadInterruptedException(e);
				}
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestEvilSearcherFactory()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), dir);
			w.Commit();
			IndexReader other = DirectoryReader.Open(dir);
			SearcherFactory theEvilOne = new _SearcherFactory_417(other);
			try
			{
				new SearcherManager(w.w, false, theEvilOne);
				Fail("didn't hit expected exception");
			}
			catch (InvalidOperationException)
			{
			}
			// expected
			w.Dispose();
			other.Dispose();
			dir.Dispose();
		}

		private sealed class _SearcherFactory_417 : SearcherFactory
		{
			public _SearcherFactory_417(IndexReader other)
			{
				this.other = other;
			}

			public override IndexSearcher NewSearcher(IndexReader ignored)
			{
				return LuceneTestCase.NewSearcher(other);
			}

			private readonly IndexReader other;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestListenerCalled()
		{
			Directory dir = NewDirectory();
			IndexWriter iw = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, 
				null));
			AtomicBoolean afterRefreshCalled = new AtomicBoolean(false);
			SearcherManager sm = new SearcherManager(iw, true, new SearcherFactory());
			sm.AddListener(new _RefreshListener_440(afterRefreshCalled));
			iw.AddDocument(new Lucene.Net.Documents.Document());
			iw.Commit();
			IsFalse(afterRefreshCalled.Get());
			sm.MaybeRefreshBlocking();
			IsTrue(afterRefreshCalled.Get());
			sm.Dispose();
			iw.Dispose();
			dir.Dispose();
		}

		private sealed class _RefreshListener_440 : ReferenceManager.RefreshListener
		{
			public _RefreshListener_440(AtomicBoolean afterRefreshCalled)
			{
				this.afterRefreshCalled = afterRefreshCalled;
			}

			public void BeforeRefresh()
			{
			}

			public void AfterRefresh(bool didRefresh)
			{
				if (didRefresh)
				{
					afterRefreshCalled.Set(true);
				}
			}

			private readonly AtomicBoolean afterRefreshCalled;
		}

		// LUCENE-5461
		/// <exception cref="System.Exception"></exception>
		public virtual void TestCRTReopen()
		{
			//test behaving badly
			//should be high enough
			int maxStaleSecs = 20;
			//build crap data just to store it.
			string s = "        abcdefghijklmnopqrstuvwxyz     ";
			char[] chars = s.ToCharArray();
			StringBuilder builder = new StringBuilder(2048);
			for (int i = 0; i < 2048; i++)
			{
				builder.Append(chars[Random().Next(chars.Length)]);
			}
			string content = builder.ToString();
			SnapshotDeletionPolicy sdp = new SnapshotDeletionPolicy(new KeepOnlyLastCommitDeletionPolicy
				());
			Directory dir = new NRTCachingDirectory(NewFSDirectory(CreateTempDir("nrt")), 5, 
				128);
			IndexWriterConfig config = new IndexWriterConfig(Version.LUCENE_46, new MockAnalyzer
				(Random()));
			config.SetIndexDeletionPolicy(sdp);
			config.SetOpenMode(IndexWriterConfig.OpenMode.CREATE_OR_APPEND);
			IndexWriter iw = new IndexWriter(dir, config);
			SearcherManager sm = new SearcherManager(iw, true, new SearcherFactory());
			TrackingIndexWriter tiw = new TrackingIndexWriter(iw);
			ControlledRealTimeReopenThread<IndexSearcher> controlledRealTimeReopenThread = new 
				ControlledRealTimeReopenThread<IndexSearcher>(tiw, sm, maxStaleSecs, 0);
			controlledRealTimeReopenThread.SetDaemon(true);
			controlledRealTimeReopenThread.Start();
			IList<Thread> commitThreads = new List<Thread>();
			for (int i_1 = 0; i_1 < 500; i_1++)
			{
				if (i_1 > 0 && i_1 % 50 == 0)
				{
					Thread commitThread = new Thread(new _Runnable_496(iw, sdp, dir));
					//distribute, and backup
					//System.out.println(names);
					commitThread.Start();
					commitThreads.Add(commitThread);
				}
				Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
				d.Add(new TextField("count", i_1 + string.Empty, Field.Store.NO));
				d.Add(new TextField("content", content, Field.Store.YES));
				long start = DateTime.Now.CurrentTimeMillis();
				long l = tiw.AddDocument(d);
				controlledRealTimeReopenThread.WaitForGeneration(l);
				long wait = DateTime.Now.CurrentTimeMillis() - start;
				IsTrue("waited too long for generation " + wait, wait < (maxStaleSecs
					 * 1000));
				IndexSearcher searcher = sm.Acquire();
				TopDocs td = searcher.Search(new TermQuery(new Term("count", i_1 + string.Empty))
					, 10);
				sm.Release(searcher);
				AreEqual(1, td.TotalHits);
			}
			foreach (Thread commitThread_1 in commitThreads)
			{
				commitThread_1.Join();
			}
			controlledRealTimeReopenThread.Dispose();
			sm.Dispose();
			iw.Dispose();
			dir.Dispose();
		}

		private sealed class _Runnable_496 : Runnable
		{
			public _Runnable_496(IndexWriter iw, SnapshotDeletionPolicy sdp, Directory dir)
			{
				this.iw = iw;
				this.sdp = sdp;
				this.dir = dir;
			}

			public void Run()
			{
				try
				{
					iw.Commit();
					IndexCommit ic = sdp.Snapshot();
					foreach (string name in ic.FileNames)
					{
						IsTrue(LuceneTestCase.SlowFileExists(dir, name));
					}
				}
				catch (Exception e)
				{
					throw new SystemException(e);
				}
			}

			private readonly IndexWriter iw;

			private readonly SnapshotDeletionPolicy sdp;

			private readonly Directory dir;
		}
	}
}
