using System;
using System.Threading;
using Lucene.Net.Analysis;
using Lucene.Net.Support;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.TestFramework;
using Lucene.Net.TestFramework.Index;
using NUnit.Framework;

namespace Lucene.Net.Test.Index
{
    [TestFixture]
	public class TestNRTReaderWithThreads : LuceneTestCase
	{
		internal AtomicInteger seq = new AtomicInteger(1);

		[Test]
		public virtual void TestIndexing()
		{
			Directory mainDir = NewDirectory();
			if (mainDir is MockDirectoryWrapper)
			{
				((MockDirectoryWrapper)mainDir).SetAssertNoDeleteOpenFile(true);
			}
			IndexWriter writer = new IndexWriter(mainDir, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(10)).SetMergePolicy
				(NewLogMergePolicy(false, 2)));
			IndexReader reader = writer.Reader;
			// start pooling readers
			reader.Dispose();
			var threads = new Thread[4];
		    var runThreads = new RunThread[4];
		    for (int x = 0; x < threads.Length; x++)
			{
			    var runThread = new RunThread(this, x%2, writer);
			    threads[x] = new Thread(runThread.Run) {Name = ("Thread " + x)};
			    threads[x].Start();
			    runThreads[x] = runThread;
			}
			long startTime = DateTime.Now.CurrentTimeMillis();
			long duration = 1000;
			while ((DateTime.Now.CurrentTimeMillis() - startTime) < duration)
			{
				Thread.Sleep(100);
			}
			int delCount = 0;
			int addCount = 0;
			foreach (RunThread t in runThreads)
			{
			    t.run = false;
			    AssertNull("Exception thrown: " + t.ex, t.ex);
			    addCount += t.addCount;
			    delCount += t.delCount;
			}
			foreach (Thread t in threads)
			{
			    t.Join();
			}
		    foreach (RunThread t in runThreads)
		    {
		        AssertNull("Exception thrown: " + t.ex, t.ex);
		    }
		    //System.out.println("addCount:"+addCount);
			//System.out.println("delCount:"+delCount);
			writer.Dispose();
			mainDir.Dispose();
		}

		public class RunThread
		{
			internal IndexWriter writer;

			internal volatile bool run = true;

			internal volatile Exception ex;

			internal int delCount = 0;

			internal int addCount = 0;

			internal int type;

			internal readonly Random r = new Random(Random().Next());

			public RunThread(TestNRTReaderWithThreads _enclosing, int type, IndexWriter writer
				)
			{
				this._enclosing = _enclosing;
				this.type = type;
				this.writer = writer;
			}

			public void Run()
			{
				try
				{
					while (this.run)
					{
						//int n = random.nextInt(2);
						if (this.type == 0)
						{
							int i = this._enclosing.seq.AddAndGet(1);
							Lucene.Net.Documents.Document doc = DocHelper.CreateDocument(i, "index1", 10
								);
							this.writer.AddDocument(doc);
							this.addCount++;
						}
						else
						{
							if (this.type == 1)
							{
								// we may or may not delete because the term may not exist,
								// however we're opening and closing the reader rapidly
								IndexReader reader = this.writer.Reader;
								int id = this.r.Next(this._enclosing.seq.Get());
								Term term = new Term("id", id.ToString());
								int count = TestIndexWriterReader.Count(term, reader);
								this.writer.DeleteDocuments(term);
								reader.Dispose();
								this.delCount += count;
							}
						}
					}
				}
				catch (Exception ex)
				{
					ex.printStackTrace();
					this.ex = ex;
					this.run = false;
				}
			}

			private readonly TestNRTReaderWithThreads _enclosing;
		}
	}
}
