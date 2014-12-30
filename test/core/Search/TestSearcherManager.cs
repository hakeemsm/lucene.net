/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search
{
	public class TestSearcherManager : ThreadedIndexingAndSearchingTestCase
	{
		internal bool warmCalled;

		private SearcherLifetimeManager.Pruner pruner;

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSearcherManager()
		{
			pruner = new SearcherLifetimeManager.PruneByAge(TEST_NIGHTLY ? TestUtil.NextInt(Random
				(), 1, 20) : 1);
			RunTest("TestSearcherManager");
		}

		/// <exception cref="System.Exception"></exception>
		protected override IndexSearcher GetFinalSearcher()
		{
			if (!isNRT)
			{
				writer.Commit();
			}
			IsTrue(mgr.MaybeRefresh() || mgr.IsSearcherCurrent());
			return mgr.Acquire();
		}

		private SearcherManager mgr;

		private SearcherLifetimeManager lifetimeMGR;

		private readonly IList<long> pastSearchers = new List<long>();

		private bool isNRT;

		/// <exception cref="System.Exception"></exception>
		protected override void DoAfterWriter(ExecutorService es)
		{
			SearcherFactory factory = new _SearcherFactory_75(this, es);
			if (Random().NextBoolean())
			{
				// TODO: can we randomize the applyAllDeletes?  But
				// somehow for final searcher we must apply
				// deletes...
				mgr = new SearcherManager(writer, true, factory);
				isNRT = true;
			}
			else
			{
				// SearcherManager needs to see empty commit:
				writer.Commit();
				mgr = new SearcherManager(dir, factory);
				isNRT = false;
				assertMergedSegmentsWarmed = false;
			}
			lifetimeMGR = new SearcherLifetimeManager();
		}

		private sealed class _SearcherFactory_75 : SearcherFactory
		{
			public _SearcherFactory_75(TestSearcherManager _enclosing, ExecutorService es)
			{
				this._enclosing = _enclosing;
				this.es = es;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override IndexSearcher NewSearcher(IndexReader r)
			{
				IndexSearcher s = new IndexSearcher(r, es);
				this._enclosing.warmCalled = true;
				s.Search(new TermQuery(new Term("body", "united")), 10);
				return s;
			}

			private readonly TestSearcherManager _enclosing;

			private readonly ExecutorService es;
		}

		/// <exception cref="System.Exception"></exception>
		protected override void DoSearching(ExecutorService es, long stopTime)
		{
			Thread reopenThread = new _Thread_104(this, stopTime);
			reopenThread.SetDaemon(true);
			reopenThread.Start();
			RunSearchThreads(stopTime);
			reopenThread.Join();
		}

		private sealed class _Thread_104 : Thread
		{
			public _Thread_104(TestSearcherManager _enclosing, long stopTime)
			{
				this._enclosing = _enclosing;
				this.stopTime = stopTime;
			}

			public override void Run()
			{
				try
				{
					if (LuceneTestCase.VERBOSE)
					{
						System.Console.Out.WriteLine("[" + Thread.CurrentThread.Name + "]: launch reopen thread"
							);
					}
					while (DateTime.Now.CurrentTimeMillis() < stopTime)
					{
						Thread.Sleep(TestUtil.NextInt(LuceneTestCase.Random(), 1, 100));
						this._enclosing.writer.Commit();
						Thread.Sleep(TestUtil.NextInt(LuceneTestCase.Random(), 1, 5));
						bool block = LuceneTestCase.Random().NextBoolean();
						if (block)
						{
							this._enclosing.mgr.MaybeRefreshBlocking();
							this._enclosing.lifetimeMGR.Prune(this._enclosing.pruner);
						}
						else
						{
							if (this._enclosing.mgr.MaybeRefresh())
							{
								this._enclosing.lifetimeMGR.Prune(this._enclosing.pruner);
							}
						}
					}
				}
				catch (Exception t)
				{
					if (LuceneTestCase.VERBOSE)
					{
						System.Console.Out.WriteLine("TEST: reopen thread hit exc");
						t.printStackTrace();
					}
					this._enclosing.failed.Set(true);
					throw new SystemException(t);
				}
			}

			private readonly TestSearcherManager _enclosing;

			private readonly long stopTime;
		}

		/// <exception cref="System.Exception"></exception>
		protected override IndexSearcher GetCurrentSearcher()
		{
			if (Random().Next(10) == 7)
			{
				// NOTE: not best practice to call maybeReopen
				// synchronous to your search threads, but still we
				// test as apps will presumably do this for
				// simplicity:
				if (mgr.MaybeRefresh())
				{
					lifetimeMGR.Prune(pruner);
				}
			}
			IndexSearcher s = null;
			lock (pastSearchers)
			{
				while (pastSearchers.Count != 0 && Random().NextDouble() < 0.25)
				{
					// 1/4 of the time pull an old searcher, ie, simulate
					// a user doing a follow-on action on a previous
					// search (drilling down/up, clicking next/prev page,
					// etc.)
					long token = pastSearchers[Random().Next(pastSearchers.Count)];
					s = lifetimeMGR.Acquire(token);
					if (s == null)
					{
						// Searcher was pruned
						pastSearchers.Remove(token);
					}
					else
					{
						break;
					}
				}
			}
			if (s == null)
			{
				s = mgr.Acquire();
				if (s.IndexReader.NumDocs != 0)
				{
					long token = lifetimeMGR.Record(s);
					lock (pastSearchers)
					{
						if (!pastSearchers.Contains(token))
						{
							pastSearchers.Add(token);
						}
					}
				}
			}
			return s;
		}

		/// <exception cref="System.Exception"></exception>
		protected override void ReleaseSearcher(IndexSearcher s)
		{
			s.IndexReader.DecRef();
		}

		/// <exception cref="System.Exception"></exception>
		protected override void DoClose()
		{
			IsTrue(warmCalled);
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: now close SearcherManager");
			}
			mgr.Dispose();
			lifetimeMGR.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestIntermediateClose()
		{
			Directory dir = NewDirectory();
			// Test can deadlock if we use SMS:
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetMergeScheduler(new ConcurrentMergeScheduler()));
			writer.AddDocument(new Lucene.Net.Documents.Document());
			writer.Commit();
			CountdownEvent awaitEnterWarm = new CountdownEvent(1);
			CountdownEvent awaitClose = new CountdownEvent(1);
			AtomicBoolean triedReopen = new AtomicBoolean(false);
			ExecutorService es = Random().NextBoolean() ? null : Executors.NewCachedThreadPool
				(new NamedThreadFactory("testIntermediateClose"));
			SearcherFactory factory = new _SearcherFactory_214(triedReopen, awaitEnterWarm, awaitClose
				, es);
			//
			SearcherManager searcherManager = Random().NextBoolean() ? new SearcherManager(dir
				, factory) : new SearcherManager(writer, Random().NextBoolean(), factory);
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("sm created");
			}
			IndexSearcher searcher = searcherManager.Acquire();
			try
			{
				AreEqual(1, searcher.IndexReader.NumDocs);
			}
			finally
			{
				searcherManager.Release(searcher);
			}
			writer.AddDocument(new Lucene.Net.Documents.Document());
			writer.Commit();
			AtomicBoolean success = new AtomicBoolean(false);
			Exception[] exc = new Exception[1];
			Thread thread = new Thread(new _Runnable_244(triedReopen, searcherManager
				, success, exc));
			// expected
			// use success as the barrier here to make sure we see the write
			thread.Start();
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("THREAD started");
			}
			awaitEnterWarm.Await();
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("NOW call close");
			}
			searcherManager.Dispose();
			awaitClose.CountDown();
			thread.Join();
			try
			{
				searcherManager.Acquire();
				Fail("already closed");
			}
			catch (AlreadyClosedException)
			{
			}
			// expected
			IsFalse(success.Get());
			IsTrue(triedReopen.Get());
			IsNull(string.Empty + exc[0], exc[0]);
			writer.Dispose();
			dir.Dispose();
			if (es != null)
			{
				es.Shutdown();
				es.AwaitTermination(1, TimeUnit.SECONDS);
			}
		}

		private sealed class _SearcherFactory_214 : SearcherFactory
		{
			public _SearcherFactory_214(AtomicBoolean triedReopen, CountdownEvent awaitEnterWarm
				, CountdownEvent awaitClose, ExecutorService es)
			{
				this.triedReopen = triedReopen;
				this.awaitEnterWarm = awaitEnterWarm;
				this.awaitClose = awaitClose;
				this.es = es;
			}

			public override IndexSearcher NewSearcher(IndexReader r)
			{
				try
				{
					if (triedReopen.Get())
					{
						awaitEnterWarm.CountDown();
						awaitClose.Await();
					}
				}
				catch (Exception)
				{
				}
				return new IndexSearcher(r, es);
			}

			private readonly AtomicBoolean triedReopen;

			private readonly CountdownEvent awaitEnterWarm;

			private readonly CountdownEvent awaitClose;

			private readonly ExecutorService es;
		}

		private sealed class _Runnable_244 : Runnable
		{
			public _Runnable_244(AtomicBoolean triedReopen, SearcherManager searcherManager, 
				AtomicBoolean success, Exception[] exc)
			{
				this.triedReopen = triedReopen;
				this.searcherManager = searcherManager;
				this.success = success;
				this.exc = exc;
			}

			public void Run()
			{
				try
				{
					triedReopen.Set(true);
					if (LuceneTestCase.VERBOSE)
					{
						System.Console.Out.WriteLine("NOW call maybeReopen");
					}
					searcherManager.MaybeRefresh();
					success.Set(true);
				}
				catch (AlreadyClosedException)
				{
				}
				catch (Exception e)
				{
					if (LuceneTestCase.VERBOSE)
					{
						System.Console.Out.WriteLine("FAIL: unexpected exc");
						e.printStackTrace();
					}
					exc[0] = e;
					success.Set(false);
				}
			}

			private readonly AtomicBoolean triedReopen;

			private readonly SearcherManager searcherManager;

			private readonly AtomicBoolean success;

			private readonly Exception[] exc;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestCloseTwice()
		{
			// test that we can close SM twice (per Closeable's contract).
			Directory dir = NewDirectory();
			new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, null)).Dispose();
			SearcherManager sm = new SearcherManager(dir, null);
			sm.Dispose();
			sm.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestReferenceDecrementIllegally()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetMergeScheduler(new ConcurrentMergeScheduler()));
			SearcherManager sm = new SearcherManager(writer, false, new SearcherFactory());
			writer.AddDocument(new Lucene.Net.Documents.Document());
			writer.Commit();
			sm.MaybeRefreshBlocking();
			IndexSearcher acquire = sm.Acquire();
			IndexSearcher acquire2 = sm.Acquire();
			sm.Release(acquire);
			sm.Release(acquire2);
			acquire = sm.Acquire();
			acquire.IndexReader.DecRef();
			sm.Release(acquire);
			try
			{
				sm.Acquire();
				Fail("acquire should have thrown an IllegalStateException since we modified the refCount outside of the manager"
					);
			}
			catch (InvalidOperationException)
			{
			}
			//
			// sm.close(); -- already closed
			writer.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestEnsureOpen()
		{
			Directory dir = NewDirectory();
			new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, null)).Dispose();
			SearcherManager sm = new SearcherManager(dir, null);
			IndexSearcher s = sm.Acquire();
			sm.Dispose();
			// this should succeed;
			sm.Release(s);
			try
			{
				// this should fail
				sm.Acquire();
			}
			catch (AlreadyClosedException)
			{
			}
			// ok
			try
			{
				// this should fail
				sm.MaybeRefresh();
			}
			catch (AlreadyClosedException)
			{
			}
			// ok
			dir.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestListenerCalled()
		{
			Directory dir = NewDirectory();
			IndexWriter iw = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, 
				null));
			AtomicBoolean afterRefreshCalled = new AtomicBoolean(false);
			SearcherManager sm = new SearcherManager(iw, false, new SearcherFactory());
			sm.AddListener(new _RefreshListener_368(afterRefreshCalled));
			iw.AddDocument(new Lucene.Net.Documents.Document());
			iw.Commit();
			IsFalse(afterRefreshCalled.Get());
			sm.MaybeRefreshBlocking();
			IsTrue(afterRefreshCalled.Get());
			sm.Dispose();
			iw.Dispose();
			dir.Dispose();
		}

		private sealed class _RefreshListener_368 : ReferenceManager.RefreshListener
		{
			public _RefreshListener_368(AtomicBoolean afterRefreshCalled)
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

		/// <exception cref="System.Exception"></exception>
		public virtual void TestEvilSearcherFactory()
		{
			Random random = Random();
			Directory dir = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(random, dir);
			w.Commit();
			IndexReader other = DirectoryReader.Open(dir);
			SearcherFactory theEvilOne = new _SearcherFactory_397(other);
			try
			{
				new SearcherManager(dir, theEvilOne);
			}
			catch (InvalidOperationException)
			{
			}
			// expected
			try
			{
				new SearcherManager(w.w, random.NextBoolean(), theEvilOne);
			}
			catch (InvalidOperationException)
			{
			}
			// expected
			w.Dispose();
			other.Dispose();
			dir.Dispose();
		}

		private sealed class _SearcherFactory_397 : SearcherFactory
		{
			public _SearcherFactory_397(IndexReader other)
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
		public virtual void TestMaybeRefreshBlockingLock()
		{
			// make sure that maybeRefreshBlocking releases the lock, otherwise other
			// threads cannot obtain it.
			Directory dir = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), dir);
			w.Dispose();
			SearcherManager sm = new SearcherManager(dir, null);
			Thread t = new _Thread_428(sm);
			// this used to not release the lock, preventing other threads from obtaining it.
			t.Start();
			t.Join();
			// if maybeRefreshBlocking didn't release the lock, this will fail.
			IsTrue("failde to obtain the refreshLock!", sm.MaybeRefresh
				());
			sm.Dispose();
			dir.Dispose();
		}

		private sealed class _Thread_428 : Thread
		{
			public _Thread_428(SearcherManager sm)
			{
				this.sm = sm;
			}

			public override void Run()
			{
				try
				{
					sm.MaybeRefreshBlocking();
				}
				catch (Exception e)
				{
					throw new SystemException(e);
				}
			}

			private readonly SearcherManager sm;
		}
	}
}
