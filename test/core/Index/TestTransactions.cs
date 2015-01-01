using System;
using System.IO;
using System.Threading;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.TestFramework;
using Lucene.Net.TestFramework.Util;
using NUnit.Framework;
using Directory = Lucene.Net.Store.Directory;


namespace Lucene.Net.Test.Index
{
	public class TestTransactions : LuceneTestCase
	{
		private static volatile bool doFail;

		private class RandomFailure : MockDirectoryWrapper.Failure
		{
			/// <exception cref="System.IO.IOException"></exception>
			public override void Eval(MockDirectoryWrapper dir)
			{
				if (TestTransactions.doFail && LuceneTestCase.Random().Next() % 10 <= 3)
				{
					throw new IOException("now failing randomly but on purpose");
				}
			}

			internal RandomFailure(TestTransactions _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly TestTransactions _enclosing;
		}

		private abstract class TimedThread
		{
			internal volatile bool failed;

			private static float RUN_TIME_MSEC = AtLeast(500);

			private TestTransactions.TimedThread[] allThreads;

			/// <exception cref="System.Exception"></exception>
			public abstract void DoWork();

			internal TimedThread(TestTransactions.TimedThread[] threads)
			{
				this.allThreads = threads;
			}

			public void Run()
			{
				long stopTime = DateTime.Now.CurrentTimeMillis() + (long)(RUN_TIME_MSEC);
				try
				{
					do
					{
						if (AnyErrors())
						{
							break;
						}
						DoWork();
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

		private class IndexerThread : TimedThread
		{
			internal Directory dir1;

			internal Directory dir2;

			internal object Lock;

			internal int nextID;

			public IndexerThread(TestTransactions _enclosing, object Lock, Directory dir1, Directory
				 dir2, TestTransactions.TimedThread[] threads) : base(threads)
			{
				this._enclosing = _enclosing;
				this.Lock = Lock;
				this.dir1 = dir1;
				this.dir2 = dir2;
			}

			/// <exception cref="System.Exception"></exception>
			public override void DoWork()
			{
				IndexWriter writer1 = new IndexWriter(this.dir1, ((IndexWriterConfig)LuceneTestCase
					.NewIndexWriterConfig(LuceneTestCase.TEST_VERSION_CURRENT, new MockAnalyzer(LuceneTestCase
					.Random())).SetMaxBufferedDocs(3)).SetMergeScheduler(new ConcurrentMergeScheduler
					()).SetMergePolicy(LuceneTestCase.NewLogMergePolicy(2)));
				((ConcurrentMergeScheduler)writer1.Config.MergeScheduler).SetSuppressExceptions
					();
				// Intentionally use different params so flush/merge
				// happen @ different times
				IndexWriter writer2 = new IndexWriter(this.dir2, ((IndexWriterConfig)LuceneTestCase
					.NewIndexWriterConfig(LuceneTestCase.TEST_VERSION_CURRENT, new MockAnalyzer(LuceneTestCase
					.Random())).SetMaxBufferedDocs(2)).SetMergeScheduler(new ConcurrentMergeScheduler
					()).SetMergePolicy(LuceneTestCase.NewLogMergePolicy(3)));
				((ConcurrentMergeScheduler)writer2.Config.MergeScheduler).SetSuppressExceptions
					();
				this.Update(writer1);
				this.Update(writer2);
				TestTransactions.doFail = true;
				try
				{
					lock (this.Lock)
					{
						try
						{
							writer1.PrepareCommit();
						}
						catch
						{
							writer1.Rollback();
							writer2.Rollback();
							return;
						}
						try
						{
							writer2.PrepareCommit();
						}
						catch
						{
							writer1.Rollback();
							writer2.Rollback();
							return;
						}
						writer1.Commit();
						writer2.Commit();
					}
				}
				finally
				{
					TestTransactions.doFail = false;
				}
				writer1.Dispose();
				writer2.Dispose();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public virtual void Update(IndexWriter writer)
			{
				// Add 10 docs:
				FieldType customType = new FieldType(StringField.TYPE_NOT_STORED);
				customType.StoreTermVectors = true;
				for (int j = 0; j < 10; j++)
				{
					Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
					int n = Random().Next();
                    d.Add(NewField("id", (nextID++).ToString(), customType
						));
					d.Add(NewTextField("contents", English.IntToEnglish(n), Field.Store
						.NO));
					writer.AddDocument(d);
				}
				// Delete 5 docs:
				int deleteID = this.nextID - 1;
				for (int j_1 = 0; j_1 < 5; j_1++)
				{
					writer.DeleteDocuments(new Term("id", string.Empty + deleteID));
					deleteID -= 2;
				}
			}

			private readonly TestTransactions _enclosing;
		}

		private class SearcherThread : TestTransactions.TimedThread
		{
			internal Directory dir1;

			internal Directory dir2;

			internal object Lock;

			public SearcherThread(object Lock, Directory dir1, Directory dir2, TestTransactions.TimedThread
				[] threads) : base(threads)
			{
				this.Lock = Lock;
				this.dir1 = dir1;
				this.dir2 = dir2;
			}

			/// <exception cref="System.Exception"></exception>
			public override void DoWork()
			{
				IndexReader r1 = null;
				IndexReader r2 = null;
				lock (Lock)
				{
					try
					{
						r1 = DirectoryReader.Open(dir1);
						r2 = DirectoryReader.Open(dir2);
					}
					catch (IOException e)
					{
						if (!e.Message.Contains("on purpose"))
						{
							throw;
						}
						if (r1 != null)
						{
							r1.Dispose();
						}
						if (r2 != null)
						{
							r2.Dispose();
						}
						return;
					}
				}
				if (r1.NumDocs != r2.NumDocs)
				{
					throw new SystemException("doc counts differ: r1=" + r1.NumDocs + " r2=" + r2.NumDocs);
				}
				r1.Dispose();
				r2.Dispose();
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void InitIndex(Directory dir)
		{
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			for (int j = 0; j < 7; j++)
			{
				Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
				int n = Random().Next();
				d.Add(NewTextField("contents", English.IntToEnglish(n), Field.Store.NO));
				writer.AddDocument(d);
			}
			writer.Dispose();
		}

		[Test]
		public virtual void TestTransactionswithThreads()
		{
			// we cant use non-ramdir on windows, because this test needs to double-write.
			MockDirectoryWrapper dir1 = new MockDirectoryWrapper(Random(), new RAMDirectory());
			MockDirectoryWrapper dir2 = new MockDirectoryWrapper(Random(), new RAMDirectory());
			dir1.SetPreventDoubleWrite(false);
			dir2.SetPreventDoubleWrite(false);
			dir1.FailOn(new RandomFailure(this));
			dir2.FailOn(new RandomFailure(this));
			dir1.SetFailOnOpenInput(false);
			dir2.SetFailOnOpenInput(false);
			// We throw exceptions in deleteFile, which creates
			// leftover files:
			dir1.SetAssertNoUnrefencedFilesOnClose(false);
			dir2.SetAssertNoUnrefencedFilesOnClose(false);
			InitIndex(dir1);
			InitIndex(dir2);
			var threads = new Thread[3];
			var timedThreads = new TimedThread[3];
			int numThread = 0;
			var indexerThread = new IndexerThread(this, this, dir1, dir2, timedThreads);
		    var threadRunner = new Thread(indexerThread.Run);
		    threads[numThread++] = threadRunner;
			threadRunner.Start();
			var searcherThread1 = new SearcherThread(this, dir1, dir2, timedThreads);
		    var srchThreadRunner = new Thread(searcherThread1.Run);
		    threads[numThread++] = srchThreadRunner;
			srchThreadRunner.Start();
			var searcherThread2 = new SearcherThread(this, dir1, dir2, timedThreads);
		    var srchThreadRunner2 = new Thread(searcherThread2.Run);
		    threads[numThread++] = srchThreadRunner2;
			srchThreadRunner2.Start();
			for (int i = 0; i < numThread; i++)
			{
				threads[i].Join();
			}
			for (int i_1 = 0; i_1 < numThread; i_1++)
			{
				IsTrue(!timedThreads[i_1].failed);
			}
			dir1.Dispose();
			dir2.Dispose();
		}
	}
}
