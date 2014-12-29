/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using NUnit.Framework;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

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
		[BeforeClass]
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
				FieldType customType = new FieldType(StringField.TYPE_STORED);
				customType.OmitsNorms = (true);
				for (int i = 0; i < 200; i++)
				{
					Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
					d.Add(NewField("id", i.ToString(), customType));
					d.Add(NewField("contents", English.IntToEnglish(i), customType));
					writer.AddDocument(d);
				}
				((LogMergePolicy)writer.Config.MergePolicy).MergeFactor = (4);
				Thread[] threads = new Thread[NUM_THREADS];
				for (int i_1 = 0; i_1 < NUM_THREADS; i_1++)
				{
					int iFinal = i_1;
					IndexWriter writerFinal = writer;
					threads[i_1] = new _Thread_89(this, writerFinal, iFinal, iterFinal, customType);
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
				AreEqual("index=" + writer.SegString() + " numDocs=" + writer
					.NumDocs + " maxDoc=" + writer.MaxDoc + " config=" + writer.Config, expectedDocCount
					, writer.NumDocs);
				AreEqual("index=" + writer.SegString() + " numDocs=" + writer
					.NumDocs + " maxDoc=" + writer.MaxDoc + " config=" + writer.Config, expectedDocCount
					, writer.MaxDoc);
				writer.Dispose();
				writer = new IndexWriter(directory, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
					, ANALYZER).SetOpenMode(IndexWriterConfig.OpenMode.APPEND).SetMaxBufferedDocs(2)
					));
				DirectoryReader reader = DirectoryReader.Open(directory);
				AreEqual("reader=" + reader, 1, reader.Leaves.Count);
				AreEqual(expectedDocCount, reader.NumDocs);
				reader.Dispose();
			}
			writer.Dispose();
		}

		private sealed class _Thread_89 : Thread
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

			public override void Run()
			{
				try
				{
					for (int j = 0; j < Lucene.Net.Index.TestThreadedForceMerge.NUM_ITER2; j++)
					{
						writerFinal.ForceMerge(1, false);
						for (int k = 0; k < 17 * (1 + iFinal); k++)
						{
							Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
							d.Add(LuceneTestCase.NewField("id", iterFinal + "_" + iFinal + "_" + j + "_" + k, 
								customType));
							d.Add(LuceneTestCase.NewField("contents", English.IntToEnglish(iFinal + k), customType
								));
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
					System.Console.Out.WriteLine(Thread.CurrentThread().GetName() + ": hit exception"
						);
					Sharpen.Runtime.PrintStackTrace(t, System.Console.Out);
				}
			}

			private readonly TestThreadedForceMerge _enclosing;

			private readonly IndexWriter writerFinal;

			private readonly int iFinal;

			private readonly int iterFinal;

			private readonly FieldType customType;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestThreadedForceMerge()
		{
			Directory directory = NewDirectory();
			RunTest(Random(), directory);
			directory.Dispose();
		}
	}
}
