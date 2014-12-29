/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search
{
	public class TestTotalHitCountCollector : LuceneTestCase
	{
		/// <exception cref="System.Exception"></exception>
		public virtual void TestBasics()
		{
			Directory indexStore = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), indexStore);
			for (int i = 0; i < 5; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(new StringField("string", "a" + i, Field.Store.NO));
				doc.Add(new StringField("string", "b" + i, Field.Store.NO));
				writer.AddDocument(doc);
			}
			IndexReader reader = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(reader);
			TotalHitCountCollector c = new TotalHitCountCollector();
			searcher.Search(new MatchAllDocsQuery(), null, c);
			AreEqual(5, c.GetTotalHits());
			reader.Dispose();
			indexStore.Dispose();
		}
	}
}
