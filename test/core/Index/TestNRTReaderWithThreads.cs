/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	public class TestNRTReaderWithThreads : LuceneTestCase
	{
		internal AtomicInteger seq = new AtomicInteger(1);

		/// <exception cref="System.Exception"></exception>
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
			IndexReader reader = writer.GetReader();
			// start pooling readers
			reader.Close();
			TestNRTReaderWithThreads.RunThread[] indexThreads = new TestNRTReaderWithThreads.RunThread
				[4];
			for (int x = 0; x < indexThreads.Length; x++)
			{
				indexThreads[x] = new TestNRTReaderWithThreads.RunThread(this, x % 2, writer);
				indexThreads[x].SetName("Thread " + x);
				indexThreads[x].Start();
			}
			long startTime = Runtime.CurrentTimeMillis();
			long duration = 1000;
			while ((Runtime.CurrentTimeMillis() - startTime) < duration)
			{
				Sharpen.Thread.Sleep(100);
			}
			int delCount = 0;
			int addCount = 0;
			for (int x_1 = 0; x_1 < indexThreads.Length; x_1++)
			{
				indexThreads[x_1].run = false;
				NUnit.Framework.Assert.IsNull("Exception thrown: " + indexThreads[x_1].ex, indexThreads
					[x_1].ex);
				addCount += indexThreads[x_1].addCount;
				delCount += indexThreads[x_1].delCount;
			}
			for (int x_2 = 0; x_2 < indexThreads.Length; x_2++)
			{
				indexThreads[x_2].Join();
			}
			for (int x_3 = 0; x_3 < indexThreads.Length; x_3++)
			{
				NUnit.Framework.Assert.IsNull("Exception thrown: " + indexThreads[x_3].ex, indexThreads
					[x_3].ex);
			}
			//System.out.println("addCount:"+addCount);
			//System.out.println("delCount:"+delCount);
			writer.Close();
			mainDir.Close();
		}

		public class RunThread : Sharpen.Thread
		{
			internal IndexWriter writer;

			internal volatile bool run = true;

			internal volatile Exception ex;

			internal int delCount = 0;

			internal int addCount = 0;

			internal int type;

			internal readonly Random r = new Random(LuceneTestCase.Random().NextLong());

			public RunThread(TestNRTReaderWithThreads _enclosing, int type, IndexWriter writer
				)
			{
				this._enclosing = _enclosing;
				this.type = type;
				this.writer = writer;
			}

			public override void Run()
			{
				try
				{
					while (this.run)
					{
						//int n = random.nextInt(2);
						if (this.type == 0)
						{
							int i = this._enclosing.seq.AddAndGet(1);
							Lucene.Net.Document.Document doc = DocHelper.CreateDocument(i, "index1", 10
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
								IndexReader reader = this.writer.GetReader();
								int id = this.r.Next(this._enclosing.seq);
								Term term = new Term("id", Sharpen.Extensions.ToString(id));
								int count = TestIndexWriterReader.Count(term, reader);
								this.writer.DeleteDocuments(term);
								reader.Close();
								this.delCount += count;
							}
						}
					}
				}
				catch (Exception ex)
				{
					Sharpen.Runtime.PrintStackTrace(ex, System.Console.Out);
					this.ex = ex;
					this.run = false;
				}
			}

			private readonly TestNRTReaderWithThreads _enclosing;
		}
	}
}
