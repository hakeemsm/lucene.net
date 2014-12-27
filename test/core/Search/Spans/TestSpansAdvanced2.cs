/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Test.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Search.Spans;
using Sharpen;

namespace Lucene.Net.Search.Spans
{
	/// <summary>
	/// Some expanded tests to make sure my patch doesn't break other SpanTermQuery
	/// functionality.
	/// </summary>
	/// <remarks>
	/// Some expanded tests to make sure my patch doesn't break other SpanTermQuery
	/// functionality.
	/// </remarks>
	public class TestSpansAdvanced2 : TestSpansAdvanced
	{
		internal IndexSearcher searcher2;

		internal IndexReader reader2;

		/// <summary>Initializes the tests by adding documents to the index.</summary>
		/// <remarks>Initializes the tests by adding documents to the index.</remarks>
		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			// create test index
			RandomIndexWriter writer = new RandomIndexWriter(Random(), mDirectory, NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random(), MockTokenizer.SIMPLE, true, MockTokenFilter
				.ENGLISH_STOPSET)).SetOpenMode(IndexWriterConfig.OpenMode.APPEND).SetMergePolicy
				(NewLogMergePolicy()).SetSimilarity(new DefaultSimilarity()));
			AddDocument(writer, "A", "Should we, could we, would we?");
			AddDocument(writer, "B", "It should.  Should it?");
			AddDocument(writer, "C", "It shouldn't.");
			AddDocument(writer, "D", "Should we, should we, should we.");
			reader2 = writer.GetReader();
			writer.Dispose();
			// re-open the searcher since we added more docs
			searcher2 = NewSearcher(reader2);
			searcher2.SetSimilarity(new DefaultSimilarity());
		}

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			reader2.Dispose();
			base.TearDown();
		}

		/// <summary>Verifies that the index has the correct number of documents.</summary>
		/// <remarks>Verifies that the index has the correct number of documents.</remarks>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestVerifyIndex()
		{
			IndexReader reader = DirectoryReader.Open(mDirectory);
			AreEqual(8, reader.NumDocs);
			reader.Dispose();
		}

		/// <summary>Tests a single span query that matches multiple documents.</summary>
		/// <remarks>Tests a single span query that matches multiple documents.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestSingleSpanQuery()
		{
			Query spanQuery = new SpanTermQuery(new Term(FIELD_TEXT, "should"));
			string[] expectedIds = new string[] { "B", "D", "1", "2", "3", "4", "A" };
			float[] expectedScores = new float[] { 0.625f, 0.45927936f, 0.35355338f, 0.35355338f
				, 0.35355338f, 0.35355338f, 0.26516503f };
			AssertHits(searcher2, spanQuery, "single span query", expectedIds, expectedScores
				);
		}

		/// <summary>Tests a single span query that matches multiple documents.</summary>
		/// <remarks>Tests a single span query that matches multiple documents.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestMultipleDifferentSpanQueries()
		{
			Query spanQuery1 = new SpanTermQuery(new Term(FIELD_TEXT, "should"));
			Query spanQuery2 = new SpanTermQuery(new Term(FIELD_TEXT, "we"));
			BooleanQuery query = new BooleanQuery();
			query.Add(spanQuery1, BooleanClause.Occur.MUST);
			query.Add(spanQuery2, BooleanClause.Occur.MUST);
			string[] expectedIds = new string[] { "D", "A" };
			// these values were pre LUCENE-413
			// final float[] expectedScores = new float[] { 0.93163157f, 0.20698164f };
			float[] expectedScores = new float[] { 1.0191123f, 0.93163157f };
			AssertHits(searcher2, query, "multiple different span queries", expectedIds, expectedScores
				);
		}

		/// <summary>Tests two span queries.</summary>
		/// <remarks>Tests two span queries.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public override void TestBooleanQueryWithSpanQueries()
		{
			DoTestBooleanQueryWithSpanQueries(searcher2, 0.73500174f);
		}
	}
}
