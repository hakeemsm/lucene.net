/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Spans;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using Sharpen;

namespace Lucene.Net.Search.Spans
{
	public class TestSpanFirstQuery : LuceneTestCase
	{
		/// <exception cref="System.Exception"></exception>
		public virtual void TestStartPositions()
		{
			Directory dir = NewDirectory();
			// mimic StopAnalyzer
			CharacterRunAutomaton stopSet = new CharacterRunAutomaton(new RegExp("the|a|of").
				ToAutomaton());
			Analyzer analyzer = new MockAnalyzer(Random(), MockTokenizer.SIMPLE, true, stopSet
				);
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, analyzer);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewTextField("field", "the quick brown fox", Field.Store.NO));
			writer.AddDocument(doc);
			Lucene.Net.Documents.Document doc2 = new Lucene.Net.Documents.Document
				();
			doc2.Add(NewTextField("field", "quick brown fox", Field.Store.NO));
			writer.AddDocument(doc2);
			IndexReader reader = writer.GetReader();
			IndexSearcher searcher = NewSearcher(reader);
			// user queries on "starts-with quick"
			SpanQuery sfq = new SpanFirstQuery(new SpanTermQuery(new Term("field", "quick")), 
				1);
			AreEqual(1, searcher.Search(sfq, 10).TotalHits);
			// user queries on "starts-with the quick"
			SpanQuery include = new SpanFirstQuery(new SpanTermQuery(new Term("field", "quick"
				)), 2);
			sfq = new SpanNotQuery(include, sfq);
			AreEqual(1, searcher.Search(sfq, 10).TotalHits);
			writer.Close();
			reader.Close();
			dir.Close();
		}
	}
}
