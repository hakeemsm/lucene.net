/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Lucene46;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Test.Index
{
	public class TestIndexWriterThreadsToSegments : LuceneTestCase
	{
		// LUCENE-5644: for first segment, two threads each indexed one doc (likely concurrently), but for second segment, each thread indexed the
		// doc NOT at the same time, and should have shared the same thread state / segment
		/// <exception cref="System.Exception"></exception>
		public virtual void TestSegmentCountOnFlushBasic()
		{
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, 
				new MockAnalyzer(Random())));
			CountDownLatch startingGun = new CountDownLatch(1);
			CountDownLatch startDone = new CountDownLatch(2);
			CountDownLatch middleGun = new CountDownLatch(1);
			CountDownLatch finalGun = new CountDownLatch(1);
			Sharpen.Thread[] threads = new Sharpen.Thread[2];
			for (int i = 0; i < threads.Length; i++)
			{
				int threadID = i;
				threads[i] = new _Thread_55(startingGun, w, startDone, middleGun, threadID, finalGun
					);
				threads[i].Start();
			}
			startingGun.CountDown();
			startDone.Await();
			IndexReader r = DirectoryReader.Open(w, true);
			AreEqual(2, r.NumDocs);
			int numSegments = r.Leaves.Count;
			// 1 segment if the threads ran sequentially, else 2:
			IsTrue(numSegments <= 2);
			r.Dispose();
			middleGun.CountDown();
			threads[0].Join();
			finalGun.CountDown();
			threads[1].Join();
			r = DirectoryReader.Open(w, true);
			AreEqual(4, r.NumDocs);
			// Both threads should have shared a single thread state since they did not try to index concurrently:
			AreEqual(1 + numSegments, r.Leaves.Count);
			r.Dispose();
			w.Dispose();
			dir.Dispose();
		}

		private sealed class _Thread_55 : Sharpen.Thread
		{
			public _Thread_55(CountDownLatch startingGun, IndexWriter w, CountDownLatch startDone
				, CountDownLatch middleGun, int threadID, CountDownLatch finalGun)
			{
				this.startingGun = startingGun;
				this.w = w;
				this.startDone = startDone;
				this.middleGun = middleGun;
				this.threadID = threadID;
				this.finalGun = finalGun;
			}

			public override void Run()
			{
				try
				{
					startingGun.Await();
					Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
						();
					doc.Add(LuceneTestCase.NewTextField("field", "here is some text", Field.Store.NO)
						);
					w.AddDocument(doc);
					startDone.CountDown();
					middleGun.Await();
					if (threadID == 0)
					{
						w.AddDocument(doc);
					}
					else
					{
						finalGun.Await();
						w.AddDocument(doc);
					}
				}
				catch (Exception e)
				{
					throw new RuntimeException(e);
				}
			}

			private readonly CountDownLatch startingGun;

			private readonly IndexWriter w;

			private readonly CountDownLatch startDone;

			private readonly CountDownLatch middleGun;

			private readonly int threadID;

			private readonly CountDownLatch finalGun;
		}

		/// <summary>Maximum number of simultaneous threads to use for each iteration.</summary>
		/// <remarks>Maximum number of simultaneous threads to use for each iteration.</remarks>
		private const int MAX_THREADS_AT_ONCE = 10;

		internal class CheckSegmentCount : Runnable, IDisposable
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
					int maxThreadStates = w.Config.GetMaxThreadStates();
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
					throw new RuntimeException(e);
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

			/// <exception cref="System.IO.IOException"></exception>
			public virtual void Close()
			{
				r.Dispose();
				r = null;
			}
		}

		// LUCENE-5644: index docs w/ multiple threads but in between flushes we limit how many threads can index concurrently in the next
		// iteration, and then verify that no more segments were flushed than number of threads:
		/// <exception cref="System.Exception"></exception>
		public virtual void TestSegmentCountOnFlushRandom()
		{
			Directory dir = NewFSDirectory(CreateTempDir());
			IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			int maxThreadStates = TestUtil.NextInt(Random(), 1, 12);
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
			TestIndexWriterThreadsToSegments.CheckSegmentCount checker = new TestIndexWriterThreadsToSegments.CheckSegmentCount
				(w, maxThreadCount, indexingCount);
			// We spin up 10 threads up front, but then in between flushes we limit how many can run on each iteration
			int ITERS = 100;
			Sharpen.Thread[] threads = new Sharpen.Thread[MAX_THREADS_AT_ONCE];
			// We use this to stop all threads once they've indexed their docs in the current iter, and pull a new NRT reader, and verify the
			// segment count:
			CyclicBarrier barrier = new CyclicBarrier(MAX_THREADS_AT_ONCE, checker);
			for (int i = 0; i < threads.Length; i++)
			{
				threads[i] = new _Thread_199(ITERS, indexingCount, maxThreadCount, w, barrier);
				// We get to index on this cycle:
				// We lose: no indexing for us on this cycle
				threads[i].Start();
			}
			foreach (Sharpen.Thread t in threads)
			{
				t.Join();
			}
			IOUtils.Close(checker, w, dir);
		}

		private sealed class _Thread_199 : Sharpen.Thread
		{
			public _Thread_199(int ITERS, AtomicInteger indexingCount, AtomicInteger maxThreadCount
				, IndexWriter w, CyclicBarrier barrier)
			{
				this.ITERS = ITERS;
				this.indexingCount = indexingCount;
				this.maxThreadCount = maxThreadCount;
				this.w = w;
				this.barrier = barrier;
			}

			public override void Run()
			{
				try
				{
					for (int iter = 0; iter < ITERS; iter++)
					{
						if (indexingCount.IncrementAndGet() <= maxThreadCount.Get())
						{
							if (LuceneTestCase.VERBOSE)
							{
								System.Console.Out.WriteLine("TEST: " + Sharpen.Thread.CurrentThread().GetName() 
									+ ": do index");
							}
							Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
								();
							doc.Add(new TextField("field", "here is some text that is a bit longer than normal trivial text"
								, Field.Store.NO));
							for (int j = 0; j < 200; j++)
							{
								w.AddDocument(doc);
							}
						}
						else
						{
							if (LuceneTestCase.VERBOSE)
							{
								System.Console.Out.WriteLine("TEST: " + Sharpen.Thread.CurrentThread().GetName() 
									+ ": don't index");
							}
						}
						barrier.Await();
					}
				}
				catch (Exception e)
				{
					throw new RuntimeException(e);
				}
			}

			private readonly int ITERS;

			private readonly AtomicInteger indexingCount;

			private readonly AtomicInteger maxThreadCount;

			private readonly IndexWriter w;

			private readonly CyclicBarrier barrier;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestManyThreadsClose()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), dir);
			w.SetDoRandomForceMerge(false);
			Sharpen.Thread[] threads = new Sharpen.Thread[TestUtil.NextInt(Random(), 4, 30)];
			CountDownLatch startingGun = new CountDownLatch(1);
			for (int i = 0; i < threads.Length; i++)
			{
				threads[i] = new _Thread_245(startingGun, w);
				// ok
				threads[i].Start();
			}
			startingGun.CountDown();
			Sharpen.Thread.Sleep(100);
			w.Dispose();
			foreach (Sharpen.Thread t in threads)
			{
				t.Join();
			}
			dir.Dispose();
		}

		private sealed class _Thread_245 : Sharpen.Thread
		{
			public _Thread_245(CountDownLatch startingGun, RandomIndexWriter w)
			{
				this.startingGun = startingGun;
				this.w = w;
			}

			public override void Run()
			{
				try
				{
					startingGun.Await();
					Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
						();
					doc.Add(new TextField("field", "here is some text that is a bit longer than normal trivial text"
						, Field.Store.NO));
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
					throw new RuntimeException(e);
				}
			}

			private readonly CountDownLatch startingGun;

			private readonly RandomIndexWriter w;
		}

		/// <exception cref="System.Exception"></exception>
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
			CountDownLatch startingGun = new CountDownLatch(1);
			Sharpen.Thread[] threads = new Sharpen.Thread[2];
			for (int i = 0; i < threads.Length; i++)
			{
				int threadID = i;
				threads[i] = new _Thread_287(startingGun, threadID, w);
				threads[i].Start();
			}
			startingGun.CountDown();
			foreach (Sharpen.Thread t in threads)
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
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(NewStringField("field", "threadIDmain", Field.Store.NO));
				w.AddDocument(doc);
				foreach (string fileName in dir.ListAll())
				{
					if (fileName.EndsWith(".si"))
					{
						string segName = IndexFileNames.ParseSegmentName(fileName);
						if (segSeen.Contains(segName) == false)
						{
							segSeen.AddItem(segName);
							SegmentInfo si = new Lucene46SegmentInfoFormat().GetSegmentInfoReader().Read(dir, 
								segName, IOContext.DEFAULT);
							si.SetCodec(codec);
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

		private sealed class _Thread_287 : Sharpen.Thread
		{
			public _Thread_287(CountDownLatch startingGun, int threadID, IndexWriter w)
			{
				this.startingGun = startingGun;
				this.threadID = threadID;
				this.w = w;
			}

			public override void Run()
			{
				try
				{
					startingGun.Await();
					for (int j = 0; j < 1000; j++)
					{
						Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
							();
						doc.Add(LuceneTestCase.NewStringField("field", "threadID" + threadID, Field.Store
							.NO));
						w.AddDocument(doc);
					}
				}
				catch (Exception e)
				{
					throw new RuntimeException(e);
				}
			}

			private readonly CountDownLatch startingGun;

			private readonly int threadID;

			private readonly IndexWriter w;
		}
	}
}
