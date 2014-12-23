/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search
{
	public class TestFieldValueFilter : LuceneTestCase
	{
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestFieldValueFilterNoValue()
		{
			Directory directory = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), directory, NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
			int docs = AtLeast(10);
			int[] docStates = BuildIndex(writer, docs);
			int numDocsNoValue = 0;
			for (int i = 0; i < docStates.Length; i++)
			{
				if (docStates[i] == 0)
				{
					numDocsNoValue++;
				}
			}
			IndexReader reader = DirectoryReader.Open(directory);
			IndexSearcher searcher = NewSearcher(reader);
			TopDocs search = searcher.Search(new TermQuery(new Term("all", "test")), new FieldValueFilter
				("some", true), docs);
			NUnit.Framework.Assert.AreEqual(search.totalHits, numDocsNoValue);
			ScoreDoc[] scoreDocs = search.scoreDocs;
			foreach (ScoreDoc scoreDoc in scoreDocs)
			{
				NUnit.Framework.Assert.IsNull(reader.Document(scoreDoc.doc).Get("some"));
			}
			reader.Close();
			directory.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestFieldValueFilter()
		{
			Directory directory = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), directory, NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
			int docs = AtLeast(10);
			int[] docStates = BuildIndex(writer, docs);
			int numDocsWithValue = 0;
			for (int i = 0; i < docStates.Length; i++)
			{
				if (docStates[i] == 1)
				{
					numDocsWithValue++;
				}
			}
			IndexReader reader = DirectoryReader.Open(directory);
			IndexSearcher searcher = NewSearcher(reader);
			TopDocs search = searcher.Search(new TermQuery(new Term("all", "test")), new FieldValueFilter
				("some"), docs);
			NUnit.Framework.Assert.AreEqual(search.totalHits, numDocsWithValue);
			ScoreDoc[] scoreDocs = search.scoreDocs;
			foreach (ScoreDoc scoreDoc in scoreDocs)
			{
				NUnit.Framework.Assert.AreEqual("value", reader.Document(scoreDoc.doc).Get("some"
					));
			}
			reader.Close();
			directory.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private int[] BuildIndex(RandomIndexWriter writer, int docs)
		{
			int[] docStates = new int[docs];
			for (int i = 0; i < docs; i++)
			{
				Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
					();
				if (Random().NextBoolean())
				{
					docStates[i] = 1;
					doc.Add(NewTextField("some", "value", Field.Store.YES));
				}
				doc.Add(NewTextField("all", "test", Field.Store.NO));
				doc.Add(NewTextField("id", string.Empty + i, Field.Store.YES));
				writer.AddDocument(doc);
			}
			writer.Commit();
			int numDeletes = Random().Next(docs);
			for (int i_1 = 0; i_1 < numDeletes; i_1++)
			{
				int docID = Random().Next(docs);
				writer.DeleteDocuments(new Term("id", string.Empty + docID));
				docStates[docID] = 2;
			}
			writer.Close();
			return docStates;
		}
	}
}
