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


namespace Lucene.Net.Search
{
	/// <summary>DateFilter JUnit tests.</summary>
	/// <remarks>DateFilter JUnit tests.</remarks>
	public class TestDateFilter : LuceneTestCase
	{
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestBefore()
		{
			// create an index
			Directory indexStore = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), indexStore);
			long now = DateTime.Now.CurrentTimeMillis();
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			// add time that is in the past
			doc.Add(NewStringField("datefield", DateTools.TimeToString(now - 1000, DateTools.Resolution
				.MILLISECOND), Field.Store.YES));
			doc.Add(NewTextField("body", "Today is a very sunny day in New York City", Field.Store
				.YES));
			writer.AddDocument(doc);
			IndexReader reader = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(reader);
			// filter that should preserve matches
			// DateFilter df1 = DateFilter.Before("datefield", now);
			TermRangeFilter df1 = TermRangeFilter.NewStringRange("datefield", DateTools.TimeToString
				(now - 2000, DateTools.Resolution.MILLISECOND), DateTools.TimeToString(now, DateTools.Resolution
				.MILLISECOND), false, true);
			// filter that should discard matches
			// DateFilter df2 = DateFilter.Before("datefield", now - 999999);
			TermRangeFilter df2 = TermRangeFilter.NewStringRange("datefield", DateTools.TimeToString
				(0, DateTools.Resolution.MILLISECOND), DateTools.TimeToString(now - 2000, DateTools.Resolution
				.MILLISECOND), true, false);
			// search something that doesn't exist with DateFilter
			Query query1 = new TermQuery(new Term("body", "NoMatchForThis"));
			// search for something that does exists
			Query query2 = new TermQuery(new Term("body", "sunny"));
			ScoreDoc[] result;
			// ensure that queries return expected results without DateFilter first
			result = searcher.Search(query1, null, 1000).ScoreDocs;
			AreEqual(0, result.Length);
			result = searcher.Search(query2, null, 1000).ScoreDocs;
			AreEqual(1, result.Length);
			// run queries with DateFilter
			result = searcher.Search(query1, df1, 1000).ScoreDocs;
			AreEqual(0, result.Length);
			result = searcher.Search(query1, df2, 1000).ScoreDocs;
			AreEqual(0, result.Length);
			result = searcher.Search(query2, df1, 1000).ScoreDocs;
			AreEqual(1, result.Length);
			result = searcher.Search(query2, df2, 1000).ScoreDocs;
			AreEqual(0, result.Length);
			reader.Dispose();
			indexStore.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestAfter()
		{
			// create an index
			Directory indexStore = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), indexStore);
			long now = DateTime.Now.CurrentTimeMillis();
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			// add time that is in the future
			doc.Add(NewStringField("datefield", DateTools.TimeToString(now + 888888, DateTools.Resolution
				.MILLISECOND), Field.Store.YES));
			doc.Add(NewTextField("body", "Today is a very sunny day in New York City", Field.Store
				.YES));
			writer.AddDocument(doc);
			IndexReader reader = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(reader);
			// filter that should preserve matches
			// DateFilter df1 = DateFilter.After("datefield", now);
			TermRangeFilter df1 = TermRangeFilter.NewStringRange("datefield", DateTools.TimeToString
				(now, DateTools.Resolution.MILLISECOND), DateTools.TimeToString(now + 999999, DateTools.Resolution
				.MILLISECOND), true, false);
			// filter that should discard matches
			// DateFilter df2 = DateFilter.After("datefield", now + 999999);
			TermRangeFilter df2 = TermRangeFilter.NewStringRange("datefield", DateTools.TimeToString
				(now + 999999, DateTools.Resolution.MILLISECOND), DateTools.TimeToString(now + 999999999
				, DateTools.Resolution.MILLISECOND), false, true);
			// search something that doesn't exist with DateFilter
			Query query1 = new TermQuery(new Term("body", "NoMatchForThis"));
			// search for something that does exists
			Query query2 = new TermQuery(new Term("body", "sunny"));
			ScoreDoc[] result;
			// ensure that queries return expected results without DateFilter first
			result = searcher.Search(query1, null, 1000).ScoreDocs;
			AreEqual(0, result.Length);
			result = searcher.Search(query2, null, 1000).ScoreDocs;
			AreEqual(1, result.Length);
			// run queries with DateFilter
			result = searcher.Search(query1, df1, 1000).ScoreDocs;
			AreEqual(0, result.Length);
			result = searcher.Search(query1, df2, 1000).ScoreDocs;
			AreEqual(0, result.Length);
			result = searcher.Search(query2, df1, 1000).ScoreDocs;
			AreEqual(1, result.Length);
			result = searcher.Search(query2, df2, 1000).ScoreDocs;
			AreEqual(0, result.Length);
			reader.Dispose();
			indexStore.Dispose();
		}
	}
}
