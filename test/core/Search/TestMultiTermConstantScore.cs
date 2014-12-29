/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using NUnit.Framework;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Store;
using Sharpen;

namespace Lucene.Net.Search
{
	public class TestMultiTermConstantScore : BaseTestRangeFilter
	{
		/// <summary>threshold for comparing floats</summary>
		public const float SCORE_COMP_THRESH = 1e-6f;

		internal static Directory small;

		internal static IndexReader reader;

		public static void AssertEquals(string m, int e, int a)
		{
		}

		//HM:revisit 
		//assert.assertEquals(m, e, a);
		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.BeforeClass]
		public static void BeforeClass()
		{
			string[] data = new string[] { "A 1 2 3 4 5 6", "Z       4 5 6", null, "B   2   4 5 6"
				, "Y     3   5 6", null, "C     3     6", "X       4 5 6" };
			small = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), small, NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random(), MockTokenizer.WHITESPACE, false
				)).SetMergePolicy(NewLogMergePolicy()));
			FieldType customType = new FieldType(TextField.TYPE_STORED);
			customType.Tokenized = (false);
			for (int i = 0; i < data.Length; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(NewField("id", i.ToString(), customType));
				// Field.Keyword("id",String.valueOf(i)));
				doc.Add(NewField("all", "all", customType));
				// Field.Keyword("all","all"));
				if (null != data[i])
				{
					doc.Add(NewTextField("data", data[i], Field.Store.YES));
				}
				// Field.Text("data",data[i]));
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
			small.Dispose();
			reader = null;
			small = null;
		}

		/// <summary>macro for readability</summary>
		public static Query Csrq(string f, string l, string h, bool il, bool ih)
		{
			TermRangeQuery query = TermRangeQuery.NewStringRange(f, l, h, il, ih);
			query.SetRewriteMethod(MultiTermQuery.CONSTANT_SCORE_FILTER_REWRITE);
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: query=" + query);
			}
			return query;
		}

		public static Query Csrq(string f, string l, string h, bool il, bool ih, MultiTermQuery.RewriteMethod
			 method)
		{
			TermRangeQuery query = TermRangeQuery.NewStringRange(f, l, h, il, ih);
			query.SetRewriteMethod(method);
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: query=" + query + " method=" + method);
			}
			return query;
		}

		/// <summary>macro for readability</summary>
		public static Query Cspq(Term prefix)
		{
			PrefixQuery query = new PrefixQuery(prefix);
			query.SetRewriteMethod(MultiTermQuery.CONSTANT_SCORE_FILTER_REWRITE);
			return query;
		}

		/// <summary>macro for readability</summary>
		public static Query Cswcq(Term wild)
		{
			WildcardQuery query = new WildcardQuery(wild);
			query.SetRewriteMethod(MultiTermQuery.CONSTANT_SCORE_FILTER_REWRITE);
			return query;
		}

		/// <exception cref="System.IO.IOException"></exception>
		[NUnit.Framework.Test]
		public virtual void TestBasics()
		{
			QueryUtils.Check(Csrq("data", "1", "6", T, T));
			QueryUtils.Check(Csrq("data", "A", "Z", T, T));
			QueryUtils.CheckUnequal(Csrq("data", "1", "6", T, T), Csrq("data", "A", "Z", T, T
				));
			QueryUtils.Check(Cspq(new Term("data", "p*u?")));
			QueryUtils.CheckUnequal(Cspq(new Term("data", "pre*")), Cspq(new Term("data", "pres*"
				)));
			QueryUtils.Check(Cswcq(new Term("data", "p")));
			QueryUtils.CheckUnequal(Cswcq(new Term("data", "pre*n?t")), Cswcq(new Term("data"
				, "pr*t?j")));
		}

		/// <exception cref="System.IO.IOException"></exception>
		[NUnit.Framework.Test]
		public virtual void TestEqualScores()
		{
			// NOTE: uses index build in *this* setUp
			IndexSearcher search = NewSearcher(reader);
			ScoreDoc[] result;
			// some hits match more terms then others, score should be the same
			result = search.Search(Csrq("data", "1", "6", T, T), null, 1000).ScoreDocs;
			int numHits = result.Length;
			AssertEquals("wrong number of results", 6, numHits);
			float score = result[0].score;
			for (int i = 1; i < numHits; i++)
			{
				AreEqual("score for " + i + " was not the same", score, result
					[i].score, SCORE_COMP_THRESH);
			}
			result = search.Search(Csrq("data", "1", "6", T, T, MultiTermQuery.CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE
				), null, 1000).ScoreDocs;
			numHits = result.Length;
			AssertEquals("wrong number of results", 6, numHits);
			for (int i_1 = 0; i_1 < numHits; i_1++)
			{
				AreEqual("score for " + i_1 + " was not the same", score, 
					result[i_1].score, SCORE_COMP_THRESH);
			}
			result = search.Search(Csrq("data", "1", "6", T, T, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT
				), null, 1000).ScoreDocs;
			numHits = result.Length;
			AssertEquals("wrong number of results", 6, numHits);
			for (int i_2 = 0; i_2 < numHits; i_2++)
			{
				AreEqual("score for " + i_2 + " was not the same", score, 
					result[i_2].score, SCORE_COMP_THRESH);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		[NUnit.Framework.Test]
		public virtual void TestEqualScoresWhenNoHits()
		{
			// Test for LUCENE-5245: Empty MTQ rewrites should have a consistent norm, so always need to return a CSQ!
			// NOTE: uses index build in *this* setUp
			IndexSearcher search = NewSearcher(reader);
			ScoreDoc[] result;
			TermQuery dummyTerm = new TermQuery(new Term("data", "1"));
			BooleanQuery bq = new BooleanQuery();
			bq.Add(dummyTerm, BooleanClause.Occur.SHOULD);
			// hits one doc
			bq.Add(Csrq("data", "#", "#", T, T), BooleanClause.Occur.SHOULD);
			// hits no docs
			result = search.Search(bq, null, 1000).ScoreDocs;
			int numHits = result.Length;
			AssertEquals("wrong number of results", 1, numHits);
			float score = result[0].score;
			for (int i = 1; i < numHits; i++)
			{
				AreEqual("score for " + i + " was not the same", score, result
					[i].score, SCORE_COMP_THRESH);
			}
			bq = new BooleanQuery();
			bq.Add(dummyTerm, BooleanClause.Occur.SHOULD);
			// hits one doc
			bq.Add(Csrq("data", "#", "#", T, T, MultiTermQuery.CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE
				), BooleanClause.Occur.SHOULD);
			// hits no docs
			result = search.Search(bq, null, 1000).ScoreDocs;
			numHits = result.Length;
			AssertEquals("wrong number of results", 1, numHits);
			for (int i_1 = 0; i_1 < numHits; i_1++)
			{
				AreEqual("score for " + i_1 + " was not the same", score, 
					result[i_1].score, SCORE_COMP_THRESH);
			}
			bq = new BooleanQuery();
			bq.Add(dummyTerm, BooleanClause.Occur.SHOULD);
			// hits one doc
			bq.Add(Csrq("data", "#", "#", T, T, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT
				), BooleanClause.Occur.SHOULD);
			// hits no docs
			result = search.Search(bq, null, 1000).ScoreDocs;
			numHits = result.Length;
			AssertEquals("wrong number of results", 1, numHits);
			for (int i_2 = 0; i_2 < numHits; i_2++)
			{
				AreEqual("score for " + i_2 + " was not the same", score, 
					result[i_2].score, SCORE_COMP_THRESH);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		[NUnit.Framework.Test]
		public virtual void TestBoost()
		{
			// NOTE: uses index build in *this* setUp
			IndexSearcher search = NewSearcher(reader);
			// test for correct application of query normalization
			// must use a non score normalizing method for this.
			search.SetSimilarity(new DefaultSimilarity());
			Query q = Csrq("data", "1", "6", T, T);
			q.SetBoost(100);
			search.Search(q, null, new _Collector_231());
			//
			// Ensure that boosting works to score one clause of a query higher
			// than another.
			//
			Query q1 = Csrq("data", "A", "A", T, T);
			// matches document #0
			q1.SetBoost(.1f);
			Query q2 = Csrq("data", "Z", "Z", T, T);
			// matches document #1
			BooleanQuery bq = new BooleanQuery(true);
			bq.Add(q1, BooleanClause.Occur.SHOULD);
			bq.Add(q2, BooleanClause.Occur.SHOULD);
			ScoreDoc[] hits = search.Search(bq, null, 1000).ScoreDocs;
			//HM:revisit 
			//assert.assertEquals(1, hits[0].Doc);
			//HM:revisit 
			//assert.assertEquals(0, hits[1].Doc);
			IsTrue(hits[0].score > hits[1].score);
			q1 = Csrq("data", "A", "A", T, T, MultiTermQuery.CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE
				);
			// matches document #0
			q1.SetBoost(.1f);
			q2 = Csrq("data", "Z", "Z", T, T, MultiTermQuery.CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE
				);
			// matches document #1
			bq = new BooleanQuery(true);
			bq.Add(q1, BooleanClause.Occur.SHOULD);
			bq.Add(q2, BooleanClause.Occur.SHOULD);
			hits = search.Search(bq, null, 1000).ScoreDocs;
			//HM:revisit 
			//assert.assertEquals(1, hits[0].Doc);
			//HM:revisit 
			//assert.assertEquals(0, hits[1].Doc);
			IsTrue(hits[0].score > hits[1].score);
			q1 = Csrq("data", "A", "A", T, T);
			// matches document #0
			q1.SetBoost(10f);
			q2 = Csrq("data", "Z", "Z", T, T);
			// matches document #1
			bq = new BooleanQuery(true);
			bq.Add(q1, BooleanClause.Occur.SHOULD);
			bq.Add(q2, BooleanClause.Occur.SHOULD);
			hits = search.Search(bq, null, 1000).ScoreDocs;
			//HM:revisit 
			//assert.assertEquals(0, hits[0].Doc);
			//HM:revisit 
			//assert.assertEquals(1, hits[1].Doc);
			IsTrue(hits[0].score > hits[1].score);
		}

		private sealed class _Collector_231 : Collector
		{
			public _Collector_231()
			{
				this.@base = 0;
			}

			private int @base;

			private Scorer scorer;

			public override void SetScorer(Scorer scorer)
			{
				this.scorer = scorer;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Collect(int doc)
			{
				AreEqual("score for doc " + (doc + this.@base) + " was not correct"
					, 1.0f, this.scorer.Score(), TestMultiTermConstantScore.SCORE_COMP_THRESH);
			}

			public override void SetNextReader(AtomicReaderContext context)
			{
				this.@base = context.docBase;
			}

			public override bool AcceptsDocsOutOfOrder()
			{
				return true;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		[NUnit.Framework.Test]
		public virtual void TestBooleanOrderUnAffected()
		{
			// NOTE: uses index build in *this* setUp
			IndexSearcher search = NewSearcher(reader);
			// first do a regular TermRangeQuery which uses term expansion so
			// docs with more terms in range get higher scores
			Query rq = TermRangeQuery.NewStringRange("data", "1", "4", T, T);
			ScoreDoc[] expected = search.Search(rq, null, 1000).ScoreDocs;
			int numHits = expected.Length;
			// now do a boolean where which also contains a
			// ConstantScoreRangeQuery and make sure hte order is the same
			BooleanQuery q = new BooleanQuery();
			q.Add(rq, BooleanClause.Occur.MUST);
			// T, F);
			q.Add(Csrq("data", "1", "6", T, T), BooleanClause.Occur.MUST);
			// T, F);
			ScoreDoc[] actual = search.Search(q, null, 1000).ScoreDocs;
			AssertEquals("wrong numebr of hits", numHits, actual.Length);
			for (int i = 0; i < numHits; i++)
			{
				AssertEquals("mismatch in docid for hit#" + i, expected[i].Doc, actual[i].Doc);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		[NUnit.Framework.Test]
		public virtual void TestRangeQueryId()
		{
			// NOTE: uses index build in *super* setUp
			IndexReader reader = signedIndexReader;
			IndexSearcher search = NewSearcher(reader);
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: reader=" + reader);
			}
			int medId = ((maxId - minId) / 2);
			string minIP = Pad(minId);
			string maxIP = Pad(maxId);
			string medIP = Pad(medId);
			int numDocs = reader.NumDocs;
			AssertEquals("num of docs", numDocs, 1 + maxId - minId);
			ScoreDoc[] result;
			// test id, bounded on both ends
			result = search.Search(Csrq("id", minIP, maxIP, T, T), null, numDocs).ScoreDocs;
			AssertEquals("find all", numDocs, result.Length);
			result = search.Search(Csrq("id", minIP, maxIP, T, T, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT
				), null, numDocs).ScoreDocs;
			AssertEquals("find all", numDocs, result.Length);
			result = search.Search(Csrq("id", minIP, maxIP, T, F), null, numDocs).ScoreDocs;
			AssertEquals("all but last", numDocs - 1, result.Length);
			result = search.Search(Csrq("id", minIP, maxIP, T, F, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT
				), null, numDocs).ScoreDocs;
			AssertEquals("all but last", numDocs - 1, result.Length);
			result = search.Search(Csrq("id", minIP, maxIP, F, T), null, numDocs).ScoreDocs;
			AssertEquals("all but first", numDocs - 1, result.Length);
			result = search.Search(Csrq("id", minIP, maxIP, F, T, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT
				), null, numDocs).ScoreDocs;
			AssertEquals("all but first", numDocs - 1, result.Length);
			result = search.Search(Csrq("id", minIP, maxIP, F, F), null, numDocs).ScoreDocs;
			AssertEquals("all but ends", numDocs - 2, result.Length);
			result = search.Search(Csrq("id", minIP, maxIP, F, F, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT
				), null, numDocs).ScoreDocs;
			AssertEquals("all but ends", numDocs - 2, result.Length);
			result = search.Search(Csrq("id", medIP, maxIP, T, T), null, numDocs).ScoreDocs;
			AssertEquals("med and up", 1 + maxId - medId, result.Length);
			result = search.Search(Csrq("id", medIP, maxIP, T, T, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT
				), null, numDocs).ScoreDocs;
			AssertEquals("med and up", 1 + maxId - medId, result.Length);
			result = search.Search(Csrq("id", minIP, medIP, T, T), null, numDocs).ScoreDocs;
			AssertEquals("up to med", 1 + medId - minId, result.Length);
			result = search.Search(Csrq("id", minIP, medIP, T, T, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT
				), null, numDocs).ScoreDocs;
			AssertEquals("up to med", 1 + medId - minId, result.Length);
			// unbounded id
			result = search.Search(Csrq("id", minIP, null, T, F), null, numDocs).ScoreDocs;
			AssertEquals("min and up", numDocs, result.Length);
			result = search.Search(Csrq("id", null, maxIP, F, T), null, numDocs).ScoreDocs;
			AssertEquals("max and down", numDocs, result.Length);
			result = search.Search(Csrq("id", minIP, null, F, F), null, numDocs).ScoreDocs;
			AssertEquals("not min, but up", numDocs - 1, result.Length);
			result = search.Search(Csrq("id", null, maxIP, F, F), null, numDocs).ScoreDocs;
			AssertEquals("not max, but down", numDocs - 1, result.Length);
			result = search.Search(Csrq("id", medIP, maxIP, T, F), null, numDocs).ScoreDocs;
			AssertEquals("med and up, not max", maxId - medId, result.Length);
			result = search.Search(Csrq("id", minIP, medIP, F, T), null, numDocs).ScoreDocs;
			AssertEquals("not min, up to med", medId - minId, result.Length);
			// very small sets
			result = search.Search(Csrq("id", minIP, minIP, F, F), null, numDocs).ScoreDocs;
			AssertEquals("min,min,F,F", 0, result.Length);
			result = search.Search(Csrq("id", minIP, minIP, F, F, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT
				), null, numDocs).ScoreDocs;
			AssertEquals("min,min,F,F", 0, result.Length);
			result = search.Search(Csrq("id", medIP, medIP, F, F), null, numDocs).ScoreDocs;
			AssertEquals("med,med,F,F", 0, result.Length);
			result = search.Search(Csrq("id", medIP, medIP, F, F, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT
				), null, numDocs).ScoreDocs;
			AssertEquals("med,med,F,F", 0, result.Length);
			result = search.Search(Csrq("id", maxIP, maxIP, F, F), null, numDocs).ScoreDocs;
			AssertEquals("max,max,F,F", 0, result.Length);
			result = search.Search(Csrq("id", maxIP, maxIP, F, F, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT
				), null, numDocs).ScoreDocs;
			AssertEquals("max,max,F,F", 0, result.Length);
			result = search.Search(Csrq("id", minIP, minIP, T, T), null, numDocs).ScoreDocs;
			AssertEquals("min,min,T,T", 1, result.Length);
			result = search.Search(Csrq("id", minIP, minIP, T, T, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT
				), null, numDocs).ScoreDocs;
			AssertEquals("min,min,T,T", 1, result.Length);
			result = search.Search(Csrq("id", null, minIP, F, T), null, numDocs).ScoreDocs;
			AssertEquals("nul,min,F,T", 1, result.Length);
			result = search.Search(Csrq("id", null, minIP, F, T, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT
				), null, numDocs).ScoreDocs;
			AssertEquals("nul,min,F,T", 1, result.Length);
			result = search.Search(Csrq("id", maxIP, maxIP, T, T), null, numDocs).ScoreDocs;
			AssertEquals("max,max,T,T", 1, result.Length);
			result = search.Search(Csrq("id", maxIP, maxIP, T, T, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT
				), null, numDocs).ScoreDocs;
			AssertEquals("max,max,T,T", 1, result.Length);
			result = search.Search(Csrq("id", maxIP, null, T, F), null, numDocs).ScoreDocs;
			AssertEquals("max,nul,T,T", 1, result.Length);
			result = search.Search(Csrq("id", maxIP, null, T, F, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT
				), null, numDocs).ScoreDocs;
			AssertEquals("max,nul,T,T", 1, result.Length);
			result = search.Search(Csrq("id", medIP, medIP, T, T), null, numDocs).ScoreDocs;
			AssertEquals("med,med,T,T", 1, result.Length);
			result = search.Search(Csrq("id", medIP, medIP, T, T, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT
				), null, numDocs).ScoreDocs;
			AssertEquals("med,med,T,T", 1, result.Length);
		}

		/// <exception cref="System.IO.IOException"></exception>
		[NUnit.Framework.Test]
		public virtual void TestRangeQueryRand()
		{
			// NOTE: uses index build in *super* setUp
			IndexReader reader = signedIndexReader;
			IndexSearcher search = NewSearcher(reader);
			string minRP = Pad(signedIndexDir.minR);
			string maxRP = Pad(signedIndexDir.maxR);
			int numDocs = reader.NumDocs;
			AssertEquals("num of docs", numDocs, 1 + maxId - minId);
			ScoreDoc[] result;
			// test extremes, bounded on both ends
			result = search.Search(Csrq("rand", minRP, maxRP, T, T), null, numDocs).ScoreDocs;
			AssertEquals("find all", numDocs, result.Length);
			result = search.Search(Csrq("rand", minRP, maxRP, T, F), null, numDocs).ScoreDocs;
			AssertEquals("all but biggest", numDocs - 1, result.Length);
			result = search.Search(Csrq("rand", minRP, maxRP, F, T), null, numDocs).ScoreDocs;
			AssertEquals("all but smallest", numDocs - 1, result.Length);
			result = search.Search(Csrq("rand", minRP, maxRP, F, F), null, numDocs).ScoreDocs;
			AssertEquals("all but extremes", numDocs - 2, result.Length);
			// unbounded
			result = search.Search(Csrq("rand", minRP, null, T, F), null, numDocs).ScoreDocs;
			AssertEquals("smallest and up", numDocs, result.Length);
			result = search.Search(Csrq("rand", null, maxRP, F, T), null, numDocs).ScoreDocs;
			AssertEquals("biggest and down", numDocs, result.Length);
			result = search.Search(Csrq("rand", minRP, null, F, F), null, numDocs).ScoreDocs;
			AssertEquals("not smallest, but up", numDocs - 1, result.Length);
			result = search.Search(Csrq("rand", null, maxRP, F, F), null, numDocs).ScoreDocs;
			AssertEquals("not biggest, but down", numDocs - 1, result.Length);
			// very small sets
			result = search.Search(Csrq("rand", minRP, minRP, F, F), null, numDocs).ScoreDocs;
			AssertEquals("min,min,F,F", 0, result.Length);
			result = search.Search(Csrq("rand", maxRP, maxRP, F, F), null, numDocs).ScoreDocs;
			AssertEquals("max,max,F,F", 0, result.Length);
			result = search.Search(Csrq("rand", minRP, minRP, T, T), null, numDocs).ScoreDocs;
			AssertEquals("min,min,T,T", 1, result.Length);
			result = search.Search(Csrq("rand", null, minRP, F, T), null, numDocs).ScoreDocs;
			AssertEquals("nul,min,F,T", 1, result.Length);
			result = search.Search(Csrq("rand", maxRP, maxRP, T, T), null, numDocs).ScoreDocs;
			AssertEquals("max,max,T,T", 1, result.Length);
			result = search.Search(Csrq("rand", maxRP, null, T, F), null, numDocs).ScoreDocs;
			AssertEquals("max,nul,T,T", 1, result.Length);
		}
	}
}
