/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Search.Spans;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search.Spans
{
	/// <summary>Tests the span query bug in Lucene.</summary>
	/// <remarks>
	/// Tests the span query bug in Lucene. It demonstrates that SpanTermQuerys don't
	/// work correctly in a BooleanQuery.
	/// </remarks>
	public class TestSpansAdvanced : LuceneTestCase
	{
		protected internal Directory mDirectory;

		protected internal IndexReader reader;

		protected internal IndexSearcher searcher;

		private static readonly string FIELD_ID = "ID";

		protected internal static readonly string FIELD_TEXT = "TEXT";

		// location to the index
		// field names in the index
		/// <summary>Initializes the tests by adding 4 identical documents to the index.</summary>
		/// <remarks>Initializes the tests by adding 4 identical documents to the index.</remarks>
		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			// create test index
			mDirectory = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), mDirectory, NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random(), MockTokenizer.SIMPLE, true, MockTokenFilter
				.ENGLISH_STOPSET)).SetMergePolicy(NewLogMergePolicy()).SetSimilarity(new DefaultSimilarity
				()));
			AddDocument(writer, "1", "I think it should work.");
			AddDocument(writer, "2", "I think it should work.");
			AddDocument(writer, "3", "I think it should work.");
			AddDocument(writer, "4", "I think it should work.");
			reader = writer.GetReader();
			writer.Dispose();
			searcher = NewSearcher(reader);
			searcher.SetSimilarity(new DefaultSimilarity());
		}

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			reader.Dispose();
			mDirectory.Dispose();
			mDirectory = null;
			base.TearDown();
		}

		/// <summary>Adds the document to the index.</summary>
		/// <remarks>Adds the document to the index.</remarks>
		/// <param name="writer">the Lucene index writer</param>
		/// <param name="id">the unique id of the document</param>
		/// <param name="text">the text of the document</param>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual void AddDocument(RandomIndexWriter writer, string id, 
			string text)
		{
			Lucene.Net.Documents.Document document = new Lucene.Net.Documents.Document
				();
			document.Add(NewStringField(FIELD_ID, id, Field.Store.YES));
			document.Add(NewTextField(FIELD_TEXT, text, Field.Store.YES));
			writer.AddDocument(document);
		}

		/// <summary>Tests two span queries.</summary>
		/// <remarks>Tests two span queries.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestBooleanQueryWithSpanQueries()
		{
			DoTestBooleanQueryWithSpanQueries(searcher, 0.3884282f);
		}

		/// <summary>Tests two span queries.</summary>
		/// <remarks>Tests two span queries.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual void DoTestBooleanQueryWithSpanQueries(IndexSearcher s
			, float expectedScore)
		{
			Query spanQuery = new SpanTermQuery(new Term(FIELD_TEXT, "work"));
			BooleanQuery query = new BooleanQuery();
			query.Add(spanQuery, BooleanClause.Occur.MUST);
			query.Add(spanQuery, BooleanClause.Occur.MUST);
			string[] expectedIds = new string[] { "1", "2", "3", "4" };
			float[] expectedScores = new float[] { expectedScore, expectedScore, expectedScore
				, expectedScore };
			AssertHits(s, query, "two span queries", expectedIds, expectedScores);
		}

		/// <summary>Checks to see if the hits are what we expected.</summary>
		/// <remarks>Checks to see if the hits are what we expected.</remarks>
		/// <param name="query">the query to execute</param>
		/// <param name="description">the description of the search</param>
		/// <param name="expectedIds">the expected document ids of the hits</param>
		/// <param name="expectedScores">the expected scores of the hits</param>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal static void AssertHits(IndexSearcher s, Query query, string description
			, string[] expectedIds, float[] expectedScores)
		{
			QueryUtils.Check(Random(), query, s);
			float tolerance = 1e-5f;
			// Hits hits = searcher.search(query);
			// hits normalizes and throws things off if one score is greater than 1.0
			TopDocs topdocs = s.Search(query, null, 10000);
			// did we get the hits we expected
			AreEqual(expectedIds.Length, topdocs.TotalHits);
			for (int i = 0; i < topdocs.TotalHits; i++)
			{
				// System.out.println(i + " exp: " + expectedIds[i]);
				// System.out.println(i + " field: " + hits.Doc(i).get(FIELD_ID));
				int id = topdocs.ScoreDocs[i].Doc;
				float score = topdocs.ScoreDocs[i].score;
				Lucene.Net.Documents.Document doc = s.Doc(id);
				AreEqual(expectedIds[i], doc.Get(FIELD_ID));
				bool scoreEq = Math.Abs(expectedScores[i] - score) < tolerance;
				if (!scoreEq)
				{
					System.Console.Out.WriteLine(i + " warning, expected score: " + expectedScores[i]
						 + ", actual " + score);
					System.Console.Out.WriteLine(s.Explain(query, id));
				}
				AreEqual(expectedScores[i], score, tolerance);
				AreEqual(s.Explain(query, id).GetValue(), score, tolerance
					);
			}
		}
	}
}
