/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;


namespace Lucene.Net.Search
{
	public class TestTermVectors : LuceneTestCase
	{
		private static IndexReader reader;

		private static Directory directory;

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.BeforeClass]
		public static void BeforeClass()
		{
			directory = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), directory, NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random(), MockTokenizer.SIMPLE, true)).SetMergePolicy
				(NewLogMergePolicy()));
			//writer.setNoCFSRatio(1.0);
			//writer.infoStream = System.out;
			for (int i = 0; i < 1000; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				FieldType ft = new FieldType(TextField.TYPE_STORED);
				int mod3 = i % 3;
				int mod2 = i % 2;
				if (mod2 == 0 && mod3 == 0)
				{
					ft.StoreTermVectors = true;
					ft.StoreTermVectorOffsets = true;
					ft.StoreTermVectorPositions = true;
				}
				else
				{
					if (mod2 == 0)
					{
						ft.StoreTermVectors = true;
						ft.StoreTermVectorPositions = true;
					}
					else
					{
						if (mod3 == 0)
						{
							ft.StoreTermVectors = true;
							ft.StoreTermVectorOffsets = true;
						}
						else
						{
							ft.StoreTermVectors = true;
						}
					}
				}
				doc.Add(new Field("field", English.IntToEnglish(i), ft));
				//test no term vectors too
				doc.Add(new TextField("noTV", English.IntToEnglish(i), Field.Store.YES));
				writer.AddDocument(doc);
			}
			reader = writer.Reader;
			writer.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.AfterClass]
		public static void AfterClass()
		{
			reader.Dispose();
			directory.Dispose();
			reader = null;
			directory = null;
		}

		// In a single doc, for the same field, mix the term
		// vectors up
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestMixedVectrosVectors()
		{
			RandomIndexWriter writer = new RandomIndexWriter(Random(), directory, NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random(), MockTokenizer.SIMPLE, true)).SetOpenMode
				(IndexWriterConfig.OpenMode.CREATE));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			FieldType ft2 = new FieldType(TextField.TYPE_STORED);
			ft2.StoreTermVectors = true;
			FieldType ft3 = new FieldType(TextField.TYPE_STORED);
			ft3.StoreTermVectors = true;
			ft3.StoreTermVectorPositions = true;
			FieldType ft4 = new FieldType(TextField.TYPE_STORED);
			ft4.StoreTermVectors = true;
			ft4.StoreTermVectorOffsets = true;
			FieldType ft5 = new FieldType(TextField.TYPE_STORED);
			ft5.StoreTermVectors = true;
			ft5.StoreTermVectorOffsets = true;
			ft5.StoreTermVectorPositions = true;
			doc.Add(NewTextField("field", "one", Field.Store.YES));
			doc.Add(NewField("field", "one", ft2));
			doc.Add(NewField("field", "one", ft3));
			doc.Add(NewField("field", "one", ft4));
			doc.Add(NewField("field", "one", ft5));
			writer.AddDocument(doc);
			IndexReader reader = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(reader);
			Query query = new TermQuery(new Term("field", "one"));
			ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
			AreEqual(1, hits.Length);
			Fields vectors = searcher.reader.GetTermVectors(hits[0].Doc);
			IsNotNull(vectors);
			AreEqual(1, vectors.Size());
			Terms vector = vectors.Terms("field");
			IsNotNull(vector);
			AreEqual(1, vector.Size());
			TermsEnum termsEnum = vector.Iterator(null);
			IsNotNull(termsEnum.Next());
			AreEqual("one", termsEnum.Term().Utf8ToString());
			AreEqual(5, termsEnum.TotalTermFreq);
			DocsAndPositionsEnum dpEnum = termsEnum.DocsAndPositions(null, null);
			IsNotNull(dpEnum);
			IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
			AreEqual(5, dpEnum.Freq);
			for (int i = 0; i < 5; i++)
			{
				AreEqual(i, dpEnum.NextPosition());
			}
			dpEnum = termsEnum.DocsAndPositions(null, dpEnum);
			IsNotNull(dpEnum);
			IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
			AreEqual(5, dpEnum.Freq);
			for (int i_1 = 0; i_1 < 5; i_1++)
			{
				dpEnum.NextPosition();
				AreEqual(4 * i_1, dpEnum.StartOffset());
				AreEqual(4 * i_1 + 3, dpEnum.EndOffset());
			}
			reader.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private IndexWriter CreateWriter(Directory dir)
		{
			return new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetMaxBufferedDocs(2)));
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void CreateDir(Directory dir)
		{
			IndexWriter writer = CreateWriter(dir);
			writer.AddDocument(CreateDoc());
			writer.Dispose();
		}

		private Lucene.Net.Documents.Document CreateDoc()
		{
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			FieldType ft = new FieldType(TextField.TYPE_STORED);
			ft.StoreTermVectors = true;
			ft.StoreTermVectorOffsets = true;
			ft.StoreTermVectorPositions = true;
			doc.Add(NewField("c", "aaa", ft));
			return doc;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void VerifyIndex(Directory dir)
		{
			IndexReader r = DirectoryReader.Open(dir);
			int numDocs = r.NumDocs;
			for (int i = 0; i < numDocs; i++)
			{
				IsNotNull("term vectors should not have been null for document "
					 + i, r.GetTermVectors(i).Terms("c"));
			}
			r.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestFullMergeAddDocs()
		{
			Directory target = NewDirectory();
			IndexWriter writer = CreateWriter(target);
			// with maxBufferedDocs=2, this results in two segments, so that forceMerge
			// actually does something.
			for (int i = 0; i < 4; i++)
			{
				writer.AddDocument(CreateDoc());
			}
			writer.ForceMerge(1);
			writer.Dispose();
			VerifyIndex(target);
			target.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestFullMergeAddIndexesDir()
		{
			Directory[] input = new Directory[] { NewDirectory(), NewDirectory() };
			Directory target = NewDirectory();
			foreach (Directory dir in input)
			{
				CreateDir(dir);
			}
			IndexWriter writer = CreateWriter(target);
			writer.AddIndexes(input);
			writer.ForceMerge(1);
			writer.Dispose();
			VerifyIndex(target);
			IOUtils.Close(target, input[0], input[1]);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestFullMergeAddIndexesReader()
		{
			Directory[] input = new Directory[] { NewDirectory(), NewDirectory() };
			Directory target = NewDirectory();
			foreach (Directory dir in input)
			{
				CreateDir(dir);
			}
			IndexWriter writer = CreateWriter(target);
			foreach (Directory dir_1 in input)
			{
				IndexReader r = DirectoryReader.Open(dir_1);
				writer.AddIndexes(r);
				r.Dispose();
			}
			writer.ForceMerge(1);
			writer.Dispose();
			VerifyIndex(target);
			IOUtils.Close(target, input[0], input[1]);
		}
	}
}
