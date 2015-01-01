using System;
using System.Threading;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.TestFramework;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.TestFramework.Util;
using NUnit.Framework;


namespace Lucene.Net.Test.Index
{
	public class TestThreadedForceMerge : LuceneTestCase
	{
		private static Analyzer ANALYZER;

		private const int NUM_THREADS = 3;

		private const int NUM_ITER = 1;

		private const int NUM_ITER2 = 1;

		private volatile bool failed;

		//private final static int NUM_THREADS = 5;
		[SetUp]
		public static void Setup()
		{
			ANALYZER = new MockAnalyzer(Random(), MockTokenizer.SIMPLE, true);
		}

		private void SetFailed()
		{
			failed = true;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void RunTest(Random random, Directory directory)
		{
			IndexWriter writer = new IndexWriter(directory, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, ANALYZER).SetOpenMode(IndexWriterConfig.OpenMode.CREATE).
				SetMaxBufferedDocs(2)).SetMergePolicy(NewLogMergePolicy()));
			for (int iter = 0; iter < NUM_ITER; iter++)
			{
				int iterFinal = iter;
				((LogMergePolicy)writer.Config.MergePolicy).MergeFactor = (1000);
				FieldType customType = new FieldType(StringField.TYPE_STORED) {OmitNorms = (true)};
			    for (int i = 0; i < 200; i++)
				{
					Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
					d.Add(NewField("id", i.ToString(), customType));
					d.Add(NewField("contents", English.IntToEnglish(i), customType));
					writer.AddDocument(d);
				}
				((LogMergePolicy)writer.Config.MergePolicy).MergeFactor = (4);
				Thread[] threads = new Thread[NUM_THREADS];
				for (int i = 0; i < NUM_THREADS; i++)
				{
					int iFinal = i;
					IndexWriter writerFinal = writer;
				    threads[i] = new Thread(new _Thread_89(this, writerFinal, iFinal, iterFinal, customType).Run);
				}
				for (int i_2 = 0; i_2 < NUM_THREADS; i_2++)
				{
					threads[i_2].Start();
				}
				for (int i_3 = 0; i_3 < NUM_THREADS; i_3++)
				{
					threads[i_3].Join();
				}
				IsTrue(!failed);
				int expectedDocCount = (int)((1 + iter) * (200 + 8 * NUM_ITER2 * (NUM_THREADS / 2.0
					) * (1 + NUM_THREADS)));
				AssertEquals("index=" + writer.SegString() + " numDocs=" + writer
					.NumDocs + " maxDoc=" + writer.MaxDoc + " config=" + writer.Config, expectedDocCount
					, writer.NumDocs);
				AssertEquals("index=" + writer.SegString() + " numDocs=" + writer
					.NumDocs + " maxDoc=" + writer.MaxDoc + " config=" + writer.Config, expectedDocCount
					, writer.MaxDoc);
				writer.Dispose();
				writer = new IndexWriter(directory, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
					, ANALYZER).SetOpenMode(IndexWriterConfig.OpenMode.APPEND).SetMaxBufferedDocs(2)
					));
				DirectoryReader reader = DirectoryReader.Open(directory);
                AssertEquals("reader=" + reader, 1, reader.Leaves.Count);
				AreEqual(expectedDocCount, reader.NumDocs);
				reader.Dispose();
			}
			writer.Dispose();
		}

		private sealed class _Thread_89 
		{
			public _Thread_89(TestThreadedForceMerge _enclosing, IndexWriter writerFinal, int
				 iFinal, int iterFinal, FieldType customType)
			{
				this._enclosing = _enclosing;
				this.writerFinal = writerFinal;
				this.iFinal = iFinal;
				this.iterFinal = iterFinal;
				this.customType = customType;
			}

			public void Run()
			{
				try
				{
					for (int j = 0; j < NUM_ITER2; j++)
					{
						writerFinal.ForceMerge(1, false);
						for (int k = 0; k < 17 * (1 + iFinal); k++)
						{
							Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document
							{
							    NewField("id", iterFinal + "_" + iFinal + "_" + j + "_" + k,customType),
							    NewField("contents", English.IntToEnglish(iFinal + k), customType)
							};
						    writerFinal.AddDocument(d);
						}
						for (int k_1 = 0; k_1 < 9 * (1 + iFinal); k_1++)
						{
							writerFinal.DeleteDocuments(new Term("id", iterFinal + "_" + iFinal + "_" + j + "_"
								 + k_1));
						}
						writerFinal.ForceMerge(1);
					}
				}
				catch (Exception t)
				{
					this._enclosing.SetFailed();
					System.Console.Out.WriteLine(Thread.CurrentThread.Name + ": hit exception"
						);
					t.printStackTrace();
				}
			}

			private readonly TestThreadedForceMerge _enclosing;

			private readonly IndexWriter writerFinal;

			private readonly int iFinal;

			private readonly int iterFinal;

			private readonly FieldType customType;
		}

		[Test]
		public virtual void TestForceMergeThreaded()
		{
			Directory directory = NewDirectory();
			RunTest(Random(), directory);
			directory.Dispose();
		}
	}
}
