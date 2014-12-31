using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.TestFramework;
using Lucene.Net.TestFramework.Util;
using Lucene.Net.Util;
using NUnit.Framework;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Test.Index
{
	/// <summary>MultiThreaded IndexWriter tests</summary>
	[TestFixture]
    public class TestIndexWriterWithThreads : LuceneTestCase
	{
		private class IndexerThread 
		{
			internal bool diskFull;

			internal Exception error;

			internal AlreadyClosedException ace;

			internal IndexWriter writer;

			internal bool noErrors;

			internal volatile int addCount;

			public IndexerThread(TestIndexWriterWithThreads _enclosing, IndexWriter writer, bool
				 noErrors)
			{
				this._enclosing = _enclosing;
				// Used by test cases below
				this.writer = writer;
				this.noErrors = noErrors;
			}

			public void Run()
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				FieldType customType = new FieldType(TextField.TYPE_STORED);
				customType.StoreTermVectors = true;
				customType.StoreTermVectorPositions = true;
				customType.StoreTermVectorOffsets = true;
				doc.Add(LuceneTestCase.NewField("field", "aaa bbb ccc ddd eee fff ggg hhh iii jjj"
					, customType));
				doc.Add(new NumericDocValuesField("dv", 5));
				int idUpto = 0;
				int fullCount = 0;
				long stopTime = DateTime.Now.CurrentTimeMillis() + 200;
				do
				{
					try
					{
						this.writer.UpdateDocument(new Term("id", string.Empty + (idUpto++)), doc);
						this.addCount++;
					}
					catch (IOException ioe)
					{
						if (LuceneTestCase.VERBOSE)
						{
							System.Console.Out.WriteLine("TEST: expected exc:");
							ioe.printStackTrace();
						}
						//System.out.println(Thread.currentThread().getName() + ": hit exc");
						//ioe.printStackTrace(System.out);
						if (ioe.Message.StartsWith("fake disk full at") || ioe.Message.Equals("now failing on purpose"
							))
						{
							this.diskFull = true;
							try
							{
								Thread.Sleep(1);
							}
							catch (Exception ie)
							{
								throw new ThreadInterruptedException(ie.Message,ie);
							}
							if (fullCount++ >= 5)
							{
								break;
							}
						}
						else
						{
							if (this.noErrors)
							{
								System.Console.Out.WriteLine(Thread.CurrentThread.Name + ": ERROR: unexpected IOException:"
									);
								ioe.printStackTrace();
								this.error = ioe;
							}
							break;
						}
					}
					catch (Exception t)
					{
						//t.printStackTrace(System.out);
						if (this.noErrors)
						{
							System.Console.Out.WriteLine(Thread.CurrentThread.Name + ": ERROR: unexpected Throwable:"
								);
							t.printStackTrace();
							this.error = t;
						}
						break;
					}
				}
				while (DateTime.Now.CurrentTimeMillis() < stopTime);
			}

			private readonly TestIndexWriterWithThreads _enclosing;
		}

		// LUCENE-1130: make sure immediate disk full on creating
		// an IndexWriter (hit during DW.ThreadState.init()), with
		// multiple threads, is OK:
		[Test]
		public virtual void TestImmediateDiskFullWithThreads()
		{
			int NUM_THREADS = 3;
			int numIterations = TEST_NIGHTLY ? 10 : 3;
			for (int iter = 0; iter < numIterations; iter++)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("\nTEST: iter=" + iter);
				}
				MockDirectoryWrapper dir = NewMockDirectory();
				IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
					(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2)).SetMergeScheduler
					(new ConcurrentMergeScheduler()).SetMergePolicy(NewLogMergePolicy(4)));
				((ConcurrentMergeScheduler)writer.Config.MergeScheduler).SetSuppressExceptions
					();
				dir.SetMaxSizeInBytes(4 * 1024 + 20 * iter);
				var threads = new Thread[NUM_THREADS];
				var indexerThreads = new IndexerThread[NUM_THREADS];
				for (int i = 0; i < NUM_THREADS; i++)
				{
				    var indexerThread = new IndexerThread(this, writer, true);
				    threads[i] = new Thread(indexerThread.Run);
				    indexerThreads[i] = indexerThread;
				}
			    for (int j = 0; j < NUM_THREADS; j++)
				{
					threads[j].Start();
				}
				for (int k = 0; k < NUM_THREADS; k++)
				{
					// Without fix for LUCENE-1130: one of the
					// threads will hang
					threads[k].Join();
					AssertTrue("hit unexpected Throwable", indexerThreads[k].error == null
						);
				}
				// Make sure once disk space is avail again, we can
				// cleanly close:
				dir.SetMaxSizeInBytes(0);
				writer.Dispose(false);
				dir.Dispose();
			}
		}

		// LUCENE-1130: make sure we can close() even while
		// threads are trying to add documents.  Strictly
		// speaking, this isn't valid us of Lucene's APIs, but we
		// still want to be robust to this case:
		[Test]
		public virtual void TestCloseWithThreads()
		{
			int NUM_THREADS = 3;
			int numIterations = TEST_NIGHTLY ? 7 : 3;
			for (int iter = 0; iter < numIterations; iter++)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("\nTEST: iter=" + iter);
				}
				Directory dir = NewDirectory();
				IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
					(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(10)).SetMergeScheduler
					(new ConcurrentMergeScheduler()).SetMergePolicy(NewLogMergePolicy(4)));
				((ConcurrentMergeScheduler)writer.Config.MergeScheduler).SetSuppressExceptions
					();
				var threads = new Thread[NUM_THREADS];
                var indexerThreads = new IndexerThread[NUM_THREADS];
				for (int i = 0; i < NUM_THREADS; i++)
				{
                    var indexerThread = new IndexerThread(this, writer, false);
                    threads[i] = new Thread(indexerThread.Run);
                    indexerThreads[i] = indexerThread;
				}
				for (int j = 0; j < NUM_THREADS; j++)
				{
					threads[j].Start();
				}
				bool done = false;
				while (!done)
				{
					Thread.Sleep(100);
					for (int k = 0; k < NUM_THREADS; k++)
					{
					    // only stop when at least one thread has added a doc
						if (indexerThreads[k].addCount > 0)
						{
							done = true;
							break;
						}
					    if (!threads[k].IsAlive)
					    {
					        Fail("thread failed before indexing a single document");
					    }
					}
				}
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("\nTEST: now close");
				}
				writer.Dispose(false);
				// Make sure threads that are adding docs are not hung:
				for (int i_3 = 0; i_3 < NUM_THREADS; i_3++)
				{
					// Without fix for LUCENE-1130: one of the
					// threads will hang
					threads[i_3].Join();
					if (threads[i_3].IsAlive)
					{
						Fail("thread seems to be hung");
					}
				}
				// Quick test to make sure index is not corrupt:
				IndexReader reader = DirectoryReader.Open(dir);
				DocsEnum tdocs = TestUtil.Docs(Random(), reader, "field", new BytesRef("aaa"), MultiFields
					.GetLiveDocs(reader), null, 0);
				int count = 0;
				while (tdocs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
				{
					count++;
				}
				IsTrue(count > 0);
				reader.Dispose();
				dir.Dispose();
			}
		}

		// Runs test, with multiple threads, using the specific
		// failure to trigger an IOException
		[Test]
		public virtual void TestMultipleThreadsFailure(MockDirectoryWrapper.Failure failure)
		{
			int NUM_THREADS = 3;
			for (int iter = 0; iter < 2; iter++)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: iter=" + iter);
				}
				MockDirectoryWrapper dir = NewMockDirectory();
				IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
					(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2)).SetMergeScheduler
					(new ConcurrentMergeScheduler()).SetMergePolicy(NewLogMergePolicy(4)));
				((ConcurrentMergeScheduler)writer.Config.MergeScheduler).SetSuppressExceptions
					();
				var threads = new Thread[NUM_THREADS];
			    var indexerThreads = new IndexerThread[NUM_THREADS];
			    for (int i = 0; i < NUM_THREADS; i++)
				{
                    var indexerThread = new IndexerThread(this, writer, true);
                    threads[i] = new Thread(indexerThread.Run);
				    indexerThreads[i] = indexerThread;
				}
				for (int j = 0; j < NUM_THREADS; j++)
				{
					threads[j].Start();
				}
				Thread.Sleep(10);
				dir.FailOn(failure);
				failure.SetDoFail();
				for (int k = 0; k < NUM_THREADS; k++)
				{
					threads[k].Join();
					AssertTrue("hit unexpected Throwable", indexerThreads[k].error == null
						);
				}
				bool success = false;
				try
				{
					writer.Dispose(false);
					success = true;
				}
				catch (IOException)
				{
					failure.ClearDoFail();
					writer.Dispose(false);
				}
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: success=" + success);
				}
				if (success)
				{
					IndexReader reader = DirectoryReader.Open(dir);
					IBits delDocs = MultiFields.GetLiveDocs(reader);
					for (int j = 0; j < reader.MaxDoc; j++)
					{
						if (delDocs == null || !delDocs[j])
						{
							reader.Document(j);
							reader.GetTermVectors(j);
						}
					}
					reader.Dispose();
				}
				dir.Dispose();
			}
		}

		// Runs test, with one thread, using the specific failure
		// to trigger an IOException
		[Test]
		public virtual void TestSingleThreadFailure(MockDirectoryWrapper.Failure failure
			)
		{
			MockDirectoryWrapper dir = NewMockDirectory();
			IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2)).SetMergeScheduler
				(new ConcurrentMergeScheduler()));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			FieldType customType = new FieldType(TextField.TYPE_STORED);
			customType.StoreTermVectors = true;
			customType.StoreTermVectorPositions = true;
			customType.StoreTermVectorOffsets = true;
			doc.Add(NewField("field", "aaa bbb ccc ddd eee fff ggg hhh iii jjj", customType));
			for (int i = 0; i < 6; i++)
			{
				writer.AddDocument(doc);
			}
			dir.FailOn(failure);
			failure.SetDoFail();
			try
			{
				writer.AddDocument(doc);
				writer.AddDocument(doc);
				writer.Commit();
				Fail("did not hit exception");
			}
			catch (IOException)
			{
			}
			failure.ClearDoFail();
			writer.AddDocument(doc);
			writer.Dispose(false);
			dir.Dispose();
		}

		private class FailOnlyOnAbortOrFlush : MockDirectoryWrapper.Failure
		{
			private bool onlyOnce;

			public FailOnlyOnAbortOrFlush(bool onlyOnce)
			{
				// Throws IOException during FieldsWriter.flushDocument and during DocumentsWriter.abort
				this.onlyOnce = onlyOnce;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Eval(MockDirectoryWrapper dir)
			{
				// Since we throw exc during abort, eg when IW is
				// attempting to delete files, we will leave
				// leftovers: 
				dir.SetAssertNoUnrefencedFilesOnClose(false);
				if (doFail)
				{
					var trace = new StackTrace(new Exception()).GetFrames();
					bool sawAbortOrFlushDoc = false;
					bool sawClose = false;
					bool sawMerge = false;
					for (int i = 0; i < trace.Length; i++)
					{
						if (sawAbortOrFlushDoc && sawMerge && sawClose)
						{
							break;
						}
						if ("abort".Equals(trace[i].GetMethod().Name) || "finishDocument".Equals(trace[i].
							GetMethod().Name))
						{
							sawAbortOrFlushDoc = true;
						}
						if ("merge".Equals(trace[i].GetMethod().Name))
						{
							sawMerge = true;
						}
						if ("close".Equals(trace[i].GetMethod().Name))
						{
							sawClose = true;
						}
					}
					if (sawAbortOrFlushDoc && !sawClose && !sawMerge)
					{
						if (onlyOnce)
						{
							doFail = false;
						}
						//System.out.println(Thread.currentThread().getName() + ": now fail");
						//new Throwable().printStackTrace(System.out);
						throw new IOException("now failing on purpose");
					}
				}
			}
		}

		// LUCENE-1130: make sure initial IOException, and then 2nd
		// IOException during rollback(), is OK:
		[Test]
		public virtual void TestIOExceptionDuringAbort()
		{
			TestSingleThreadFailure(new TestIndexWriterWithThreads.FailOnlyOnAbortOrFlush(false
				));
		}

		// LUCENE-1130: make sure initial IOException, and then 2nd
		// IOException during rollback(), is OK:
		[Test]
		public virtual void TestIOExceptionDuringAbortOnlyOnce()
		{
			TestSingleThreadFailure(new TestIndexWriterWithThreads.FailOnlyOnAbortOrFlush(true
				));
		}

		// LUCENE-1130: make sure initial IOException, and then 2nd
		// IOException during rollback(), with multiple threads, is OK:
		[Test]
		public virtual void TestIOExceptionDuringAbortWithThreads()
		{
			TestMultipleThreadsFailure(new TestIndexWriterWithThreads.FailOnlyOnAbortOrFlush
				(false));
		}

		// LUCENE-1130: make sure initial IOException, and then 2nd
		// IOException during rollback(), with multiple threads, is OK:
		[Test]
		public virtual void TestIOExceptionDuringAbortWithThreadsOnlyOnce()
		{
			TestMultipleThreadsFailure(new TestIndexWriterWithThreads.FailOnlyOnAbortOrFlush
				(true));
		}

		private class FailOnlyInWriteSegment : MockDirectoryWrapper.Failure
		{
			private bool onlyOnce;

			public FailOnlyInWriteSegment(bool onlyOnce)
			{
				// Throws IOException during DocumentsWriter.writeSegment
				this.onlyOnce = onlyOnce;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Eval(MockDirectoryWrapper dir)
			{
				if (doFail)
				{
					var trace = new StackTrace(new Exception()).GetFrames();
					for (int i = 0; i < trace.Length; i++)
					{
						if ("flush".Equals(trace[i].GetMethod().Name) && "Lucene.Net.index.DocFieldProcessor"
							.Equals(trace[i].GetMethod().DeclaringType.FullName))
						{
							if (onlyOnce)
							{
								doFail = false;
							}
							//System.out.println(Thread.currentThread().getName() + ": NOW FAIL: onlyOnce=" + onlyOnce);
							//new Throwable().printStackTrace(System.out);
							throw new IOException("now failing on purpose");
						}
					}
				}
			}
		}

		// LUCENE-1130: test IOException in writeSegment
		[Test]
		public virtual void TestIOExceptionDuringWriteSegment()
		{
			TestSingleThreadFailure(new TestIndexWriterWithThreads.FailOnlyInWriteSegment(false
				));
		}

		// LUCENE-1130: test IOException in writeSegment
		[Test]
		public virtual void TestIOExceptionDuringWriteSegmentOnlyOnce()
		{
			TestSingleThreadFailure(new TestIndexWriterWithThreads.FailOnlyInWriteSegment(true
				));
		}

		// LUCENE-1130: test IOException in writeSegment, with threads
		[Test]
		public virtual void TestIOExceptionDuringWriteSegmentWithThreads()
		{
			TestMultipleThreadsFailure(new FailOnlyInWriteSegment(false));
		}

		// LUCENE-1130: test IOException in writeSegment, with threads
		[Test]
		public virtual void TestIOExceptionDuringWriteSegmentWithThreadsOnlyOnce()
		{
			TestMultipleThreadsFailure(new FailOnlyInWriteSegment(true));
		}

		//  LUCENE-3365: Test adding two documents with the same field from two different IndexWriters 
		//  that we attempt to open at the same time.  As long as the first IndexWriter completes
		//  and closes before the second IndexWriter time's out trying to get the Lock,
		//  we should see both documents
		/// <exception cref="System.IO.IOException"></exception>
		[Test]
		public virtual void TestOpenTwoIndexWritersOnDifferentThreads()
		{
			Directory dir = NewDirectory();
			CountdownEvent oneIWConstructed = new CountdownEvent(1);
		    var thread1Target = new DelayedIndexAndCloseRunnable(dir, oneIWConstructed);
		    var thread2Target = new DelayedIndexAndCloseRunnable(dir, oneIWConstructed);
		    var thread1 = new Thread(thread1Target.Run);
		    var thread2 = new Thread(thread2Target.Run);
			thread1.Start();
			thread2.Start();
			oneIWConstructed.Signal();
			thread1Target.StartIndexing();
			thread2Target.StartIndexing();
			thread1.Join();
			thread2.Join();
			// ensure the directory is closed if we hit the timeout and throw assume
			// TODO: can we improve this in LuceneTestCase? I dont know what the logic would be...
			try
			{
				AssumeFalse("aborting test: timeout obtaining lock", thread1Target.failure is LockObtainFailedException
					);
				AssumeFalse("aborting test: timeout obtaining lock", thread2Target.failure is LockObtainFailedException
					);
				AssertFalse("Failed due to: " + thread1Target.failure, thread1Target.failed
					);
                AssertFalse("Failed due to: " + thread2Target.failure, thread2Target.failed
					);
				// now verify that we have two documents in the index
				IndexReader reader = DirectoryReader.Open(dir);
				AssertEquals("IndexReader should have one document per thread running"
					, 2, reader.NumDocs);
				reader.Dispose();
			}
			finally
			{
				dir.Dispose();
			}
		}

		internal class DelayedIndexAndCloseRunnable
		{
			private readonly Directory dir;

			internal bool failed = false;

			internal Exception failure = null;

			private readonly CountdownEvent startIndexing = new CountdownEvent(1);

			private CountdownEvent iwConstructed;

			public DelayedIndexAndCloseRunnable(Directory dir, CountdownEvent iwConstructed)
			{
				this.dir = dir;
				this.iwConstructed = iwConstructed;
			}

			public virtual void StartIndexing()
			{
				this.startIndexing.Signal();
			}

			public void Run()
			{
				try
				{
					var doc = new Lucene.Net.Documents.Document
						();
					Field field = NewTextField("field", "testData", Field.Store.YES);
					doc.Add(field);
					IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
						, new MockAnalyzer(Random())));
					iwConstructed.Signal();
					startIndexing.Wait();
					writer.AddDocument(doc);
					writer.Dispose();
				}
				catch (Exception e)
				{
					failed = true;
					failure = e;
					failure.printStackTrace();
				}
			}
		}

		// LUCENE-4147
        //[Test]
        //public virtual void TestRollbackAndCommitWithThreads()
        //{
        //    BaseDirectoryWrapper d = NewDirectory();
        //    if (d is MockDirectoryWrapper)
        //    {
        //        ((MockDirectoryWrapper)d).SetPreventDoubleWrite(false);
        //    }
        //    int threadCount = TestUtil.NextInt(Random(), 2, 6);
        //    AtomicReference<IndexWriter> writerRef = new AtomicReference<IndexWriter>();
        //    MockAnalyzer analyzer = new MockAnalyzer(Random());
        //    analyzer.SetMaxTokenLength(TestUtil.NextInt(Random(), 1, IndexWriter.MAX_TERM_LENGTH
        //        ));
        //    writerRef.Set(new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer
        //        )));
        //    LineFileDocs docs = new LineFileDocs(Random());
        //    Thread[] threads = new Thread[threadCount];
        //    int iters = AtLeast(100);
        //    AtomicBoolean failed = new AtomicBoolean();
        //    Lock rollbackLock = new ReentrantLock();
        //    Lock commitLock = new ReentrantLock();
        //    for (int threadID = 0; threadID < threadCount; threadID++)
        //    {
        //        threads[threadID] = new _Thread_564(iters, failed, rollbackLock, writerRef, d, commitLock
        //            , docs);
        //        //final int x = random().nextInt(5);
        //        // ok
        //        // ok
        //        // ok
        //        // ok
        //        // ok
        //        threads[threadID].Start();
        //    }
        //    for (int threadID_1 = 0; threadID_1 < threadCount; threadID_1++)
        //    {
        //        threads[threadID_1].Join();
        //    }
        //    IsTrue(!failed.Get());
        //    writerRef.Get().Dispose();
        //    d.Dispose();
        //}

        //private sealed class _Thread_564 : Thread
        //{
        //    public _Thread_564(int iters, AtomicBoolean failed, Lock rollbackLock, AtomicReference
        //        <IndexWriter> writerRef, BaseDirectoryWrapper d, Lock commitLock, LineFileDocs docs
        //        )
        //    {
        //        this.iters = iters;
        //        this.failed = failed;
        //        this.rollbackLock = rollbackLock;
        //        this.writerRef = writerRef;
        //        this.d = d;
        //        this.commitLock = commitLock;
        //        this.docs = docs;
        //    }

        //    public override void Run()
        //    {
        //        for (int iter = 0; iter < iters && !failed.Get(); iter++)
        //        {
        //            int x = LuceneTestCase.Random().Next(3);
        //            try
        //            {
        //                switch (x)
        //                {
        //                    case 0:
        //                    {
        //                        rollbackLock.Lock();
        //                        if (LuceneTestCase.VERBOSE)
        //                        {
        //                            System.Console.Out.WriteLine("\nTEST: " + Thread.CurrentThread().GetName(
        //                                ) + ": now rollback");
        //                        }
        //                        try
        //                        {
        //                            writerRef.Get().Rollback();
        //                            if (LuceneTestCase.VERBOSE)
        //                            {
        //                                System.Console.Out.WriteLine("TEST: " + Thread.CurrentThread.Name 
        //                                    + ": rollback done; now open new writer");
        //                            }
        //                            writerRef.Set(new IndexWriter(d, LuceneTestCase.NewIndexWriterConfig(LuceneTestCase
        //                                .TEST_VERSION_CURRENT, new MockAnalyzer(LuceneTestCase.Random()))));
        //                        }
        //                        finally
        //                        {
        //                            rollbackLock.Unlock();
        //                        }
        //                        break;
        //                    }

        //                    case 1:
        //                    {
        //                        commitLock.Lock();
        //                        if (LuceneTestCase.VERBOSE)
        //                        {
        //                            System.Console.Out.WriteLine("\nTEST: " + Thread.CurrentThread().GetName(
        //                                ) + ": now commit");
        //                        }
        //                        try
        //                        {
        //                            if (LuceneTestCase.Random().NextBoolean())
        //                            {
        //                                writerRef.Get().PrepareCommit();
        //                            }
        //                            writerRef.Get().Commit();
        //                        }
        //                        catch (AlreadyClosedException)
        //                        {
        //                        }
        //                        catch (ArgumentNullException)
        //                        {
        //                        }
        //                        finally
        //                        {
        //                            commitLock.Unlock();
        //                        }
        //                        break;
        //                    }

        //                    case 2:
        //                    {
        //                        if (LuceneTestCase.VERBOSE)
        //                        {
        //                            System.Console.Out.WriteLine("\nTEST: " + Thread.CurrentThread().GetName(
        //                                ) + ": now add");
        //                        }
        //                        try
        //                        {
        //                            writerRef.Get().AddDocument(docs.NextDoc());
        //                        }
        //                        catch (AlreadyClosedException)
        //                        {
        //                        }
        //                        catch (ArgumentNullException)
        //                        {
        //                        }
        //                        catch (Exception)
        //                        {
        //                        }
        //                        break;
        //                    }
        //                }
        //            }
        //            catch (Exception t)
        //            {
        //                failed.Set(true);
        //                throw new SystemException(t);
        //            }
        //        }
        //    }

        //    private readonly int iters;

        //    private readonly AtomicBoolean failed;

        //    private readonly Lock rollbackLock;

        //    private readonly AtomicReference<IndexWriter> writerRef;

        //    private readonly BaseDirectoryWrapper d;

        //    private readonly Lock commitLock;

        //    private readonly LineFileDocs docs;
        //}
	}
}
