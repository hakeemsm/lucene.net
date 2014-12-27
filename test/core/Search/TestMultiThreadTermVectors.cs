/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.IO;
using System.Text;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search
{
	public class TestMultiThreadTermVectors : LuceneTestCase
	{
		private Directory directory;

		public int numDocs = 100;

		public int numThreads = 3;

		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			directory = NewDirectory();
			IndexWriter writer = new IndexWriter(directory, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy()));
			//writer.setNoCFSRatio(0.0);
			//writer.infoStream = System.out;
			FieldType customType = new FieldType(TextField.TYPE_STORED);
			customType.SetTokenized(false);
			customType.StoreTermVectors = true;
			for (int i = 0; i < numDocs; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				Field fld = NewField("field", English.IntToEnglish(i), customType);
				doc.Add(fld);
				writer.AddDocument(doc);
			}
			writer.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			directory.Dispose();
			base.TearDown();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void Test()
		{
			IndexReader reader = null;
			try
			{
				reader = DirectoryReader.Open(directory);
				for (int i = 1; i <= numThreads; i++)
				{
					TestTermPositionVectors(reader, i);
				}
			}
			catch (IOException ioe)
			{
				Fail(ioe.Message);
			}
			finally
			{
				if (reader != null)
				{
					try
					{
						reader.Dispose();
					}
					catch (IOException ioe)
					{
						Sharpen.Runtime.PrintStackTrace(ioe);
					}
				}
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestTermPositionVectors(IndexReader reader, int threadCount)
		{
			MultiThreadTermVectorsReader[] mtr = new MultiThreadTermVectorsReader[threadCount
				];
			for (int i = 0; i < threadCount; i++)
			{
				mtr[i] = new MultiThreadTermVectorsReader();
				mtr[i].Init(reader);
			}
			int threadsAlive = mtr.Length;
			while (threadsAlive > 0)
			{
				//System.out.println("Threads alive");
				Sharpen.Thread.Sleep(10);
				threadsAlive = mtr.Length;
				for (int i_1 = 0; i_1 < mtr.Length; i_1++)
				{
					if (mtr[i_1].IsAlive() == true)
					{
						break;
					}
					threadsAlive--;
				}
			}
			long totalTime = 0L;
			for (int i_2 = 0; i_2 < mtr.Length; i_2++)
			{
				totalTime += mtr[i_2].timeElapsed;
				mtr[i_2] = null;
			}
		}
		//System.out.println("threadcount: " + mtr.length + " average term vector time: " + totalTime/mtr.length);
	}

	internal class MultiThreadTermVectorsReader : Runnable
	{
		private IndexReader reader = null;

		private Sharpen.Thread t = null;

		private readonly int runsToDo = 100;

		internal long timeElapsed = 0;

		public virtual void Init(IndexReader reader)
		{
			this.reader = reader;
			timeElapsed = 0;
			t = new Sharpen.Thread(this);
			t.Start();
		}

		public virtual bool IsAlive()
		{
			if (t == null)
			{
				return false;
			}
			return t.IsAlive();
		}

		public virtual void Run()
		{
			try
			{
				// run the test 100 times
				for (int i = 0; i < runsToDo; i++)
				{
					TestTermVectors();
				}
			}
			catch (Exception e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
			return;
		}

		/// <exception cref="System.Exception"></exception>
		private void TestTermVectors()
		{
			// check:
			int numDocs = reader.NumDocs;
			long start = 0L;
			for (int docId = 0; docId < numDocs; docId++)
			{
				start = DateTime.Now.CurrentTimeMillis();
				Fields vectors = reader.GetTermVectors(docId);
				timeElapsed += DateTime.Now.CurrentTimeMillis() - start;
				// verify vectors result
				VerifyVectors(vectors, docId);
				start = DateTime.Now.CurrentTimeMillis();
				Terms vector = reader.GetTermVectors(docId).Terms("field");
				timeElapsed += DateTime.Now.CurrentTimeMillis() - start;
				VerifyVector(vector.Iterator(null), docId);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void VerifyVectors(Fields vectors, int num)
		{
			foreach (string field in vectors)
			{
				Terms terms = vectors.Terms(field);
				//HM:revisit 
				//assert terms != null;
				VerifyVector(terms.Iterator(null), num);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void VerifyVector(TermsEnum vector, int num)
		{
			StringBuilder temp = new StringBuilder();
			while (vector.Next() != null)
			{
				temp.Append(vector.Term().Utf8ToString());
			}
			if (!English.IntToEnglish(num).Trim().Equals(temp.ToString().Trim()))
			{
				System.Console.Out.WriteLine("wrong term result");
			}
		}
	}
}
