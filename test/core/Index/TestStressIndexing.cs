using System;
using System.Threading;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Support;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.TestFramework;
using Lucene.Net.TestFramework.Util;
using Lucene.Net.Util;
using NUnit.Framework;


namespace Lucene.Net.Test.Index
{
	public class TestStressIndexing : LuceneTestCase
	{
		private abstract class TimedThread
		{
			internal volatile bool failed;

			internal int count;

			private static int RUN_TIME_MSEC = AtLeast(1000);

			private TestStressIndexing.TimedThread[] allThreads;

			/// <exception cref="System.Exception"></exception>
			public abstract void DoWork();

			internal TimedThread(TestStressIndexing.TimedThread[] threads)
			{
				this.allThreads = threads;
			}

			public void Run()
			{
				long stopTime = DateTime.Now.CurrentTimeMillis() + RUN_TIME_MSEC;
				count = 0;
				try
				{
					do
					{
						if (AnyErrors())
						{
							break;
						}
						DoWork();
						count++;
					}
					while (DateTime.Now.CurrentTimeMillis() < stopTime);
				}
				catch (Exception e)
				{
					System.Console.Out.WriteLine(Thread.CurrentThread + ": exc");
					e.printStackTrace();
					failed = true;
				}
			}

			private bool AnyErrors()
			{
				for (int i = 0; i < allThreads.Length; i++)
				{
					if (allThreads[i] != null && allThreads[i].failed)
					{
						return true;
					}
				}
				return false;
			}
		}

		private class IndexerThread : TestStressIndexing.TimedThread
		{
			internal IndexWriter writer;

			internal int nextID;

			public IndexerThread(TestStressIndexing _enclosing, IndexWriter writer, TestStressIndexing.TimedThread
				[] threads) : base(threads)
			{
				this._enclosing = _enclosing;
				this.writer = writer;
			}

			/// <exception cref="System.Exception"></exception>
			public override void DoWork()
			{
				// Add 10 docs:
				for (int j = 0; j < 10; j++)
				{
					Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
					int n = LuceneTestCase.Random().Next();
                    d.Add(LuceneTestCase.NewStringField("id", (nextID++).ToString(), Field.Store.YES));
					d.Add(LuceneTestCase.NewTextField("contents", English.IntToEnglish(n), Field.Store
						.NO));
					this.writer.AddDocument(d);
				}
				// Delete 5 docs:
				int deleteID = this.nextID - 1;
				for (int j_1 = 0; j_1 < 5; j_1++)
				{
					this.writer.DeleteDocuments(new Term("id", string.Empty + deleteID));
					deleteID -= 2;
				}
			}

			private readonly TestStressIndexing _enclosing;
		}

		private class SearcherThread : TestStressIndexing.TimedThread
		{
			private Directory directory;

			public SearcherThread(Directory directory, TestStressIndexing.TimedThread[] threads
				) : base(threads)
			{
				this.directory = directory;
			}

			/// <exception cref="System.Exception"></exception>
			public override void DoWork()
			{
				for (int i = 0; i < 100; i++)
				{
					IndexReader ir = DirectoryReader.Open(directory);
					IndexSearcher @is = NewSearcher(ir);
					ir.Dispose();
				}
				count += 100;
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void RunStressTest(Directory directory, MergeScheduler mergeScheduler
			)
		{
			IndexWriter modifier = new IndexWriter(directory, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode
				.CREATE).SetMaxBufferedDocs(10)).SetMergeScheduler(mergeScheduler));
			modifier.Commit();
			var timedThreads = new TimedThread[4];
			var threads = new Thread[4];
			int numThread = 0;
			// One modifier that writes 10 docs then removes 5, over
			// and over:
		    var idxThread = new IndexerThread(this, modifier, timedThreads);
		    var indexerThread = new Thread(idxThread.Run);
			timedThreads[numThread++] = idxThread;
			threads[numThread++] = indexerThread;
			indexerThread.Start();
			var idxThread2 = new IndexerThread(this, modifier, timedThreads);
		    var indexerThread2 = new Thread(idxThread2.Run);
			timedThreads[numThread++] = idxThread2;
			threads[numThread++] = indexerThread2;
			indexerThread2.Start();
			// Two searchers that constantly just re-instantiate the
			// searcher:
			var searcherThread1 = new SearcherThread(directory, timedThreads);
			var srchThread1 = new Thread(searcherThread1.Run);
			timedThreads[numThread++] = searcherThread1;
			threads[numThread++] = srchThread1;
			srchThread1.Start();
			var searcherThread2 = new SearcherThread(directory, timedThreads);
            var srchThread2 = new Thread(searcherThread2.Run);
			timedThreads[numThread++] = searcherThread2;
			threads[numThread++] = srchThread2;
			srchThread2.Start();
			for (int i = 0; i < numThread; i++)
			{
				threads[i].Join();
			}
			modifier.Dispose();
			for (int i_1 = 0; i_1 < numThread; i_1++)
			{
				IsTrue(!timedThreads[i_1].failed);
			}
		}

		//System.out.println("    Writer: " + indexerThread.count + " iterations");
		//System.out.println("Searcher 1: " + searcherThread1.count + " searchers created");
		//System.out.println("Searcher 2: " + searcherThread2.count + " searchers created");
		[Test]
		public virtual void TestStressIndexAndSearching()
		{
			Directory directory = NewDirectory();
			if (directory is MockDirectoryWrapper)
			{
				((MockDirectoryWrapper)directory).SetAssertNoUnrefencedFilesOnClose(true);
			}
			RunStressTest(directory, new ConcurrentMergeScheduler());
			directory.Dispose();
		}
	}
}
