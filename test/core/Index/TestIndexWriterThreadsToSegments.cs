using System;
using System.Collections.Generic;
using System.Threading;
using Lucene.Net.Analysis;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Lucene46;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.TestFramework;
using Lucene.Net.TestFramework.Index;
using Lucene.Net.TestFramework.Util;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Index
{
    [TestFixture]
	public class TestIndexWriterThreadsToSegments : LuceneTestCase
	{
		// LUCENE-5644: for first segment, two threads each indexed one doc (likely concurrently), but for second segment, each thread indexed the
		// doc NOT at the same time, and should have shared the same thread state / segment
		[Test]
		public virtual void TestSegmentCountOnFlushBasic()
		{
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, 
				new MockAnalyzer(Random())));
			CountdownEvent startingGun = new CountdownEvent(1);
			CountdownEvent startDone = new CountdownEvent(2);
			CountdownEvent middleGun = new CountdownEvent(1);
			CountdownEvent finalGun = new CountdownEvent(1);
			Thread[] threads = new Thread[2];
			for (int i = 0; i < threads.Length; i++)
			{
				int threadID = i;
				threads[i] = new Thread(new WaitingThread(startingGun, w, startDone, middleGun, threadID, finalGun).Run);
				threads[i].Start();
			}
			startingGun.Signal();
			startDone.Wait();
			IndexReader r = DirectoryReader.Open(w, true);
			AreEqual(2, r.NumDocs);
			int numSegments = r.Leaves.Count;
			// 1 segment if the threads ran sequentially, else 2:
			IsTrue(numSegments <= 2);
			r.Dispose();
			middleGun.Signal();
			threads[0].Join();
			finalGun.Signal();
			threads[1].Join();
			r = DirectoryReader.Open(w, true);
			AreEqual(4, r.NumDocs);
			// Both threads should have shared a single thread state since they did not try to index concurrently:
			AreEqual(1 + numSegments, r.Leaves.Count);
			r.Dispose();
			w.Dispose();
			dir.Dispose();
		}

		private sealed class WaitingThread
		{
			public WaitingThread(CountdownEvent startingGun, IndexWriter w, CountdownEvent startDone
				, CountdownEvent middleGun, int threadID, CountdownEvent finalGun)
			{
				this.startingGun = startingGun;
				this.w = w;
				this.startDone = startDone;
				this.middleGun = middleGun;
				this.threadID = threadID;
				this.finalGun = finalGun;
			}

			public void Run()
			{
				try
				{
					startingGun.Wait();
					var doc = new Lucene.Net.Documents.Document
					{
					    NewTextField("field", "here is some text", Field.Store.NO)
					};
				    w.AddDocument(doc);
					startDone.Signal();
					middleGun.Wait();
					if (threadID == 0)
					{
						w.AddDocument(doc);
					}
					else
					{
						finalGun.Wait();
						w.AddDocument(doc);
					}
				}
				catch (Exception e)
				{
					throw new SystemException(e.Message,e);
				}
			}

			private readonly CountdownEvent startingGun;

			private readonly IndexWriter w;

			private readonly CountdownEvent startDone;

			private readonly CountdownEvent middleGun;

			private readonly int threadID;

			private readonly CountdownEvent finalGun;
		}

		/// <summary>Maximum number of simultaneous threads to use for each iteration.</summary>
		/// <remarks>Maximum number of simultaneous threads to use for each iteration.</remarks>
		private const int MAX_THREADS_AT_ONCE = 10;

		internal class CheckSegmentCount : IDisposable
		{
			private readonly IndexWriter w;

			private readonly AtomicInteger maxThreadCountPerIter;

			private readonly AtomicInteger indexingCount;

			private DirectoryReader r;

			/// <exception cref="System.IO.IOException"></exception>
			public CheckSegmentCount(IndexWriter w, AtomicInteger maxThreadCountPerIter, AtomicInteger
				 indexingCount)
			{
				this.w = w;
				this.maxThreadCountPerIter = maxThreadCountPerIter;
				this.indexingCount = indexingCount;
				r = DirectoryReader.Open(w, true);
				AreEqual(0, r.Leaves.Count);
				SetNextIterThreadCount();
			}

			public virtual void Run()
			{
				try
				{
					int oldSegmentCount = r.Leaves.Count;
					DirectoryReader r2 = DirectoryReader.OpenIfChanged(r);
					IsNotNull(r2);
					r.Dispose();
					r = r2;
					int maxThreadStates = w.Config.MaxThreadStates;
					int maxExpectedSegments = oldSegmentCount + Math.Min(maxThreadStates, maxThreadCountPerIter
						.Get());
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("TEST: iter done; now verify oldSegCount=" + oldSegmentCount
							 + " newSegCount=" + r2.Leaves.Count + " maxExpected=" + maxExpectedSegments);
					}
					// NOTE: it won't necessarily be ==, in case some threads were strangely scheduled and never conflicted with one another (should be uncommon...?):
					IsTrue(r.Leaves.Count <= maxExpectedSegments);
					SetNextIterThreadCount();
				}
				catch (Exception e)
				{
					throw new SystemException(e.Message,e);
				}
			}

			private void SetNextIterThreadCount()
			{
				indexingCount.Set(0);
				maxThreadCountPerIter.Set(TestUtil.NextInt(Random(), 1, MAX_THREADS_AT_ONCE));
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: iter set maxThreadCount=" + maxThreadCountPerIter
						.Get());
				}
			}

			
			public virtual void Dispose()
			{
				r.Dispose();
				r = null;
			}
		}

		// LUCENE-5644: index docs w/ multiple threads but in between flushes we limit how many threads can index concurrently in the next
		// iteration, and then verify that no more segments were flushed than number of threads:
		[Test]
		public virtual void TestSegmentCountOnFlushRandom()
		{
			Directory dir = NewFSDirectory(CreateTempDir());
			IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			int maxThreadStates = Random().NextInt(1, 12);
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: maxThreadStates=" + maxThreadStates);
			}
			// Never trigger flushes (so we only flush on getReader):
			iwc.SetMaxBufferedDocs(100000000);
			iwc.SetRAMBufferSizeMB(-1);
			iwc.SetMaxThreadStates(maxThreadStates);
			// Never trigger merges (so we can simplistically count flushed segments):
			iwc.SetMergePolicy(NoMergePolicy.NO_COMPOUND_FILES);
			IndexWriter w = new IndexWriter(dir, iwc);
			// How many threads are indexing in the current cycle:
			AtomicInteger indexingCount = new AtomicInteger();
			// How many threads we will use on each cycle:
			AtomicInteger maxThreadCount = new AtomicInteger();
			var checker = new CheckSegmentCount(w, maxThreadCount, indexingCount);
			// We spin up 10 threads up front, but then in between flushes we limit how many can run on each iteration
			int ITERS = 100;
			Thread[] threads = new Thread[MAX_THREADS_AT_ONCE];
			// We use this to stop all threads once they've indexed their docs in the current iter, and pull a new NRT reader, and verify the
			// segment count:
			Barrier barrier = new Barrier(MAX_THREADS_AT_ONCE, b=>checker.Run());
			for (int i = 0; i < threads.Length; i++)
			{
			    threads[i] = new Thread(new BarrierThread(ITERS, indexingCount, maxThreadCount, w, barrier).Run);
				// We get to index on this cycle:
				// We lose: no indexing for us on this cycle
				threads[i].Start();
			}
			foreach (Thread t in threads)
			{
				t.Join();
			}
			IOUtils.Close(checker, w, dir);
		}

		private sealed class BarrierThread 
		{
			public BarrierThread(int ITERS, AtomicInteger indexingCount, AtomicInteger maxThreadCount
				, IndexWriter w, Barrier barrier)
			{
				this.ITERS = ITERS;
				this.indexingCount = indexingCount;
				this.maxThreadCount = maxThreadCount;
				this.w = w;
				this.barrier = barrier;
			}

			public void Run()
			{
				try
				{
					for (int iter = 0; iter < ITERS; iter++)
					{
						if (indexingCount.IncrementAndGet() <= maxThreadCount.Get())
						{
							if (LuceneTestCase.VERBOSE)
							{
								System.Console.Out.WriteLine("TEST: " + Thread.CurrentThread.Name 
									+ ": do index");
							}
							Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
							{
							    new TextField("field", "here is some text that is a bit longer than normal trivial text"
							        , Field.Store.NO)
							};
						    for (int j = 0; j < 200; j++)
							{
								w.AddDocument(doc);
							}
						}
						else
						{
							if (LuceneTestCase.VERBOSE)
							{
								System.Console.Out.WriteLine("TEST: " + Thread.CurrentThread.Name 
									+ ": don't index");
							}
						}
						barrier.SignalAndWait();
					}
				}
				catch (Exception e)
				{
					throw new SystemException(e.Message,e);
				}
			}

			private readonly int ITERS;

			private readonly AtomicInteger indexingCount;

			private readonly AtomicInteger maxThreadCount;

			private readonly IndexWriter w;

			private readonly Barrier barrier;
		}

		[Test]
		public virtual void TestManyThreadsClose()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), dir);
			w.SetDoRandomForceMerge(false);
			Thread[] threads = new Thread[Random().NextInt(4, 30)];
			CountdownEvent startingGun = new CountdownEvent(1);
			for (int i = 0; i < threads.Length; i++)
			{
			    threads[i] = new Thread(new ThreadRunner(startingGun, w).Run);
				// ok
				threads[i].Start();
			}
			startingGun.Signal();
			Thread.Sleep(100);
			w.Close();
			foreach (Thread t in threads)
			{
				t.Join();
			}
			dir.Dispose();
		}

		private sealed class ThreadRunner
		{
			public ThreadRunner(CountdownEvent startingGun, RandomIndexWriter w)
			{
				this.startingGun = startingGun;
				this.w = w;
			}

			public void Run()
			{
				try
				{
					startingGun.Signal();
					var doc = new Lucene.Net.Documents.Document
					{
					    new TextField("field", "here is some text that is a bit longer than normal trivial text"
					        , Field.Store.NO)
					};
				    while (true)
					{
						w.AddDocument(doc);
					}
				}
				catch (AlreadyClosedException)
				{
				}
				catch (Exception e)
				{
					throw new SystemException(e.Message,e);
				}
			}

			private readonly CountdownEvent startingGun;

			private readonly RandomIndexWriter w;
		}

		[Test]
		public virtual void TestDocsStuckInRAMForever()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			iwc.SetRAMBufferSizeMB(.2);
			Codec codec = Codec.ForName("Lucene46");
			iwc.SetCodec(codec);
			iwc.SetMergePolicy(NoMergePolicy.NO_COMPOUND_FILES);
			IndexWriter w = new IndexWriter(dir, iwc);
			CountdownEvent startingGun = new CountdownEvent(1);
			Thread[] threads = new Thread[2];
			for (int i = 0; i < threads.Length; i++)
			{
				int threadID = i;
			    threads[i] = new Thread(new DocThread(startingGun, threadID, w).Run);
				threads[i].Start();
			}
			startingGun.Signal();
			foreach (Thread t in threads)
			{
				t.Join();
			}
			ICollection<string> segSeen = new HashSet<string>();
			int thread0Count = 0;
			int thread1Count = 0;
			// At this point the writer should have 2 thread states w/ docs; now we index with only 1 thread until we see all 1000 thread0 & thread1
			// docs flushed.  If the writer incorrectly holds onto previously indexed docs forever then this will run forever:
			while (thread0Count < 1000 || thread1Count < 1000)
			{
				var doc = new Lucene.Net.Documents.Document {NewStringField("field", "threadIDmain", Field.Store.NO)};
			    w.AddDocument(doc);
				foreach (string fileName in dir.ListAll())
				{
					if (fileName.EndsWith(".si"))
					{
						string segName = IndexFileNames.ParseSegmentName(fileName);
						if (segSeen.Contains(segName) == false)
						{
							segSeen.Add(segName);
							SegmentInfo si = new Lucene46SegmentInfoFormat().SegmentInfoReader.Read(dir, 
								segName, IOContext.DEFAULT);
							si.Codec = (codec);
							SegmentCommitInfo sci = new SegmentCommitInfo(si, 0, -1, -1);
							SegmentReader sr = new SegmentReader(sci, 1, IOContext.DEFAULT);
							try
							{
								thread0Count += sr.DocFreq(new Term("field", "threadID0"));
								thread1Count += sr.DocFreq(new Term("field", "threadID1"));
							}
							finally
							{
								sr.Dispose();
							}
						}
					}
				}
			}
			w.Dispose();
			dir.Dispose();
		}

		private sealed class DocThread
		{
			public DocThread(CountdownEvent startingGun, int threadID, IndexWriter w)
			{
				this.startingGun = startingGun;
				this.threadID = threadID;
				this.w = w;
			}

			public void Run()
			{
				try
				{
					startingGun.Wait();
					for (int j = 0; j < 1000; j++)
					{
						var doc = new Lucene.Net.Documents.Document
						{
						    NewStringField("field", "threadID" + threadID, Field.Store
						        .NO)
						};
					    w.AddDocument(doc);
					}
				}
				catch (Exception e)
				{
					throw new SystemException(e.Message,e);
				}
			}

			private readonly CountdownEvent startingGun;

			private readonly int threadID;

			private readonly IndexWriter w;
		}
	}
}
