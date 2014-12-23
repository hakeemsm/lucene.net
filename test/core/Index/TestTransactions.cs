/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
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

		private abstract class TimedThread : Sharpen.Thread
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

			public override void Run()
			{
				long stopTime = Runtime.CurrentTimeMillis() + (long)(RUN_TIME_MSEC);
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
					while (Runtime.CurrentTimeMillis() < stopTime);
				}
				catch (Exception e)
				{
					System.Console.Out.WriteLine(Sharpen.Thread.CurrentThread() + ": exc");
					Sharpen.Runtime.PrintStackTrace(e, System.Console.Out);
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

		private class IndexerThread : TestTransactions.TimedThread
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
				((ConcurrentMergeScheduler)writer1.GetConfig().GetMergeScheduler()).SetSuppressExceptions
					();
				// Intentionally use different params so flush/merge
				// happen @ different times
				IndexWriter writer2 = new IndexWriter(this.dir2, ((IndexWriterConfig)LuceneTestCase
					.NewIndexWriterConfig(LuceneTestCase.TEST_VERSION_CURRENT, new MockAnalyzer(LuceneTestCase
					.Random())).SetMaxBufferedDocs(2)).SetMergeScheduler(new ConcurrentMergeScheduler
					()).SetMergePolicy(LuceneTestCase.NewLogMergePolicy(3)));
				((ConcurrentMergeScheduler)writer2.GetConfig().GetMergeScheduler()).SetSuppressExceptions
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
				writer1.Close();
				writer2.Close();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public virtual void Update(IndexWriter writer)
			{
				// Add 10 docs:
				FieldType customType = new FieldType(StringField.TYPE_NOT_STORED);
				customType.SetStoreTermVectors(true);
				for (int j = 0; j < 10; j++)
				{
					Lucene.Net.Document.Document d = new Lucene.Net.Document.Document();
					int n = LuceneTestCase.Random().Next();
					d.Add(LuceneTestCase.NewField("id", Sharpen.Extensions.ToString(this.nextID++), customType
						));
					d.Add(LuceneTestCase.NewTextField("contents", English.IntToEnglish(n), Field.Store
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
							r1.Close();
						}
						if (r2 != null)
						{
							r2.Close();
						}
						return;
					}
				}
				if (r1.NumDocs() != r2.NumDocs())
				{
					throw new RuntimeException("doc counts differ: r1=" + r1.NumDocs() + " r2=" + r2.
						NumDocs());
				}
				r1.Close();
				r2.Close();
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void InitIndex(Directory dir)
		{
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			for (int j = 0; j < 7; j++)
			{
				Lucene.Net.Document.Document d = new Lucene.Net.Document.Document();
				int n = Random().Next();
				d.Add(NewTextField("contents", English.IntToEnglish(n), Field.Store.NO));
				writer.AddDocument(d);
			}
			writer.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestTransactions()
		{
			// we cant use non-ramdir on windows, because this test needs to double-write.
			MockDirectoryWrapper dir1 = new MockDirectoryWrapper(Random(), new RAMDirectory()
				);
			MockDirectoryWrapper dir2 = new MockDirectoryWrapper(Random(), new RAMDirectory()
				);
			dir1.SetPreventDoubleWrite(false);
			dir2.SetPreventDoubleWrite(false);
			dir1.FailOn(new TestTransactions.RandomFailure(this));
			dir2.FailOn(new TestTransactions.RandomFailure(this));
			dir1.SetFailOnOpenInput(false);
			dir2.SetFailOnOpenInput(false);
			// We throw exceptions in deleteFile, which creates
			// leftover files:
			dir1.SetAssertNoUnrefencedFilesOnClose(false);
			dir2.SetAssertNoUnrefencedFilesOnClose(false);
			InitIndex(dir1);
			InitIndex(dir2);
			TestTransactions.TimedThread[] threads = new TestTransactions.TimedThread[3];
			int numThread = 0;
			TestTransactions.IndexerThread indexerThread = new TestTransactions.IndexerThread
				(this, this, dir1, dir2, threads);
			threads[numThread++] = indexerThread;
			indexerThread.Start();
			TestTransactions.SearcherThread searcherThread1 = new TestTransactions.SearcherThread
				(this, dir1, dir2, threads);
			threads[numThread++] = searcherThread1;
			searcherThread1.Start();
			TestTransactions.SearcherThread searcherThread2 = new TestTransactions.SearcherThread
				(this, dir1, dir2, threads);
			threads[numThread++] = searcherThread2;
			searcherThread2.Start();
			for (int i = 0; i < numThread; i++)
			{
				threads[i].Join();
			}
			for (int i_1 = 0; i_1 < numThread; i_1++)
			{
				NUnit.Framework.Assert.IsTrue(!threads[i_1].failed);
			}
			dir1.Close();
			dir2.Close();
		}
	}
}
