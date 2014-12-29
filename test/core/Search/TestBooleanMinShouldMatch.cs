/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Globalization;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search
{
	/// <summary>Test that BooleanQuery.setMinimumNumberShouldMatch works.</summary>
	/// <remarks>Test that BooleanQuery.setMinimumNumberShouldMatch works.</remarks>
	public class TestBooleanMinShouldMatch : LuceneTestCase
	{
		private static Directory index;

		private static IndexReader r;

		private static IndexSearcher s;

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.BeforeClass]
		public static void BeforeClass()
		{
			string[] data = new string[] { "A 1 2 3 4 5 6", "Z       4 5 6", null, "B   2   4 5 6"
				, "Y     3   5 6", null, "C     3     6", "X       4 5 6" };
			index = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), index);
			for (int i = 0; i < data.Length; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(NewStringField("id", i.ToString(), Field.Store.YES));
				//Field.Keyword("id",String.valueOf(i)));
				doc.Add(NewStringField("all", "all", Field.Store.YES));
				//Field.Keyword("all","all"));
				if (null != data[i])
				{
					doc.Add(NewTextField("data", data[i], Field.Store.YES));
				}
				//Field.Text("data",data[i]));
				w.AddDocument(doc);
			}
			r = w.Reader;
			s = NewSearcher(r);
			w.Dispose();
		}

		//System.out.println("Set up " + getName());
		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.AfterClass]
		public static void AfterClass()
		{
			s = null;
			r.Dispose();
			r = null;
			index.Dispose();
			index = null;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void VerifyNrHits(Query q, int expected)
		{
			// bs1
			ScoreDoc[] h = s.Search(q, null, 1000).ScoreDocs;
			if (expected != h.Length)
			{
				PrintHits(GetTestName(), h, s);
			}
			AreEqual("result count", expected, h.Length);
			//System.out.println("TEST: now check");
			// bs2
			TopScoreDocCollector collector = TopScoreDocCollector.Create(1000, true);
			s.Search(q, collector);
			ScoreDoc[] h2 = collector.TopDocs().ScoreDocs;
			if (expected != h2.Length)
			{
				PrintHits(GetTestName(), h2, s);
			}
			AreEqual("result count (bs2)", expected, h2.Length);
			QueryUtils.Check(Random(), q, s);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestAllOptional()
		{
			BooleanQuery q = new BooleanQuery();
			for (int i = 1; i <= 4; i++)
			{
				q.Add(new TermQuery(new Term("data", string.Empty + i)), BooleanClause.Occur.SHOULD
					);
			}
			//false, false);
			q.SetMinimumNumberShouldMatch(2);
			// match at least two of 4
			VerifyNrHits(q, 2);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestOneReqAndSomeOptional()
		{
			BooleanQuery q = new BooleanQuery();
			q.Add(new TermQuery(new Term("all", "all")), BooleanClause.Occur.MUST);
			//true,  false);
			q.Add(new TermQuery(new Term("data", "5")), BooleanClause.Occur.SHOULD);
			//false, false);
			q.Add(new TermQuery(new Term("data", "4")), BooleanClause.Occur.SHOULD);
			//false, false);
			q.Add(new TermQuery(new Term("data", "3")), BooleanClause.Occur.SHOULD);
			//false, false);
			q.SetMinimumNumberShouldMatch(2);
			// 2 of 3 optional 
			VerifyNrHits(q, 5);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSomeReqAndSomeOptional()
		{
			BooleanQuery q = new BooleanQuery();
			q.Add(new TermQuery(new Term("all", "all")), BooleanClause.Occur.MUST);
			//true,  false);
			q.Add(new TermQuery(new Term("data", "6")), BooleanClause.Occur.MUST);
			//true,  false);
			q.Add(new TermQuery(new Term("data", "5")), BooleanClause.Occur.SHOULD);
			//false, false);
			q.Add(new TermQuery(new Term("data", "4")), BooleanClause.Occur.SHOULD);
			//false, false);
			q.Add(new TermQuery(new Term("data", "3")), BooleanClause.Occur.SHOULD);
			//false, false);
			q.SetMinimumNumberShouldMatch(2);
			// 2 of 3 optional 
			VerifyNrHits(q, 5);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestOneProhibAndSomeOptional()
		{
			BooleanQuery q = new BooleanQuery();
			q.Add(new TermQuery(new Term("data", "1")), BooleanClause.Occur.SHOULD);
			//false, false);
			q.Add(new TermQuery(new Term("data", "2")), BooleanClause.Occur.SHOULD);
			//false, false);
			q.Add(new TermQuery(new Term("data", "3")), BooleanClause.Occur.MUST_NOT);
			//false, true );
			q.Add(new TermQuery(new Term("data", "4")), BooleanClause.Occur.SHOULD);
			//false, false);
			q.SetMinimumNumberShouldMatch(2);
			// 2 of 3 optional 
			VerifyNrHits(q, 1);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSomeProhibAndSomeOptional()
		{
			BooleanQuery q = new BooleanQuery();
			q.Add(new TermQuery(new Term("data", "1")), BooleanClause.Occur.SHOULD);
			//false, false);
			q.Add(new TermQuery(new Term("data", "2")), BooleanClause.Occur.SHOULD);
			//false, false);
			q.Add(new TermQuery(new Term("data", "3")), BooleanClause.Occur.MUST_NOT);
			//false, true );
			q.Add(new TermQuery(new Term("data", "4")), BooleanClause.Occur.SHOULD);
			//false, false);
			q.Add(new TermQuery(new Term("data", "C")), BooleanClause.Occur.MUST_NOT);
			//false, true );
			q.SetMinimumNumberShouldMatch(2);
			// 2 of 3 optional 
			VerifyNrHits(q, 1);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestOneReqOneProhibAndSomeOptional()
		{
			BooleanQuery q = new BooleanQuery();
			q.Add(new TermQuery(new Term("data", "6")), BooleanClause.Occur.MUST);
			// true,  false);
			q.Add(new TermQuery(new Term("data", "5")), BooleanClause.Occur.SHOULD);
			//false, false);
			q.Add(new TermQuery(new Term("data", "4")), BooleanClause.Occur.SHOULD);
			//false, false);
			q.Add(new TermQuery(new Term("data", "3")), BooleanClause.Occur.MUST_NOT);
			//false, true );
			q.Add(new TermQuery(new Term("data", "2")), BooleanClause.Occur.SHOULD);
			//false, false);
			q.Add(new TermQuery(new Term("data", "1")), BooleanClause.Occur.SHOULD);
			//false, false);
			q.SetMinimumNumberShouldMatch(3);
			// 3 of 4 optional 
			VerifyNrHits(q, 1);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSomeReqOneProhibAndSomeOptional()
		{
			BooleanQuery q = new BooleanQuery();
			q.Add(new TermQuery(new Term("all", "all")), BooleanClause.Occur.MUST);
			//true,  false);
			q.Add(new TermQuery(new Term("data", "6")), BooleanClause.Occur.MUST);
			//true,  false);
			q.Add(new TermQuery(new Term("data", "5")), BooleanClause.Occur.SHOULD);
			//false, false);
			q.Add(new TermQuery(new Term("data", "4")), BooleanClause.Occur.SHOULD);
			//false, false);
			q.Add(new TermQuery(new Term("data", "3")), BooleanClause.Occur.MUST_NOT);
			//false, true );
			q.Add(new TermQuery(new Term("data", "2")), BooleanClause.Occur.SHOULD);
			//false, false);
			q.Add(new TermQuery(new Term("data", "1")), BooleanClause.Occur.SHOULD);
			//false, false);
			q.SetMinimumNumberShouldMatch(3);
			// 3 of 4 optional 
			VerifyNrHits(q, 1);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestOneReqSomeProhibAndSomeOptional()
		{
			BooleanQuery q = new BooleanQuery();
			q.Add(new TermQuery(new Term("data", "6")), BooleanClause.Occur.MUST);
			//true,  false);
			q.Add(new TermQuery(new Term("data", "5")), BooleanClause.Occur.SHOULD);
			//false, false);
			q.Add(new TermQuery(new Term("data", "4")), BooleanClause.Occur.SHOULD);
			//false, false);
			q.Add(new TermQuery(new Term("data", "3")), BooleanClause.Occur.MUST_NOT);
			//false, true );
			q.Add(new TermQuery(new Term("data", "2")), BooleanClause.Occur.SHOULD);
			//false, false);
			q.Add(new TermQuery(new Term("data", "1")), BooleanClause.Occur.SHOULD);
			//false, false);
			q.Add(new TermQuery(new Term("data", "C")), BooleanClause.Occur.MUST_NOT);
			//false, true );
			q.SetMinimumNumberShouldMatch(3);
			// 3 of 4 optional 
			VerifyNrHits(q, 1);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSomeReqSomeProhibAndSomeOptional()
		{
			BooleanQuery q = new BooleanQuery();
			q.Add(new TermQuery(new Term("all", "all")), BooleanClause.Occur.MUST);
			//true,  false);
			q.Add(new TermQuery(new Term("data", "6")), BooleanClause.Occur.MUST);
			//true,  false);
			q.Add(new TermQuery(new Term("data", "5")), BooleanClause.Occur.SHOULD);
			//false, false);
			q.Add(new TermQuery(new Term("data", "4")), BooleanClause.Occur.SHOULD);
			//false, false);
			q.Add(new TermQuery(new Term("data", "3")), BooleanClause.Occur.MUST_NOT);
			//false, true );
			q.Add(new TermQuery(new Term("data", "2")), BooleanClause.Occur.SHOULD);
			//false, false);
			q.Add(new TermQuery(new Term("data", "1")), BooleanClause.Occur.SHOULD);
			//false, false);
			q.Add(new TermQuery(new Term("data", "C")), BooleanClause.Occur.MUST_NOT);
			//false, true );
			q.SetMinimumNumberShouldMatch(3);
			// 3 of 4 optional 
			VerifyNrHits(q, 1);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestMinHigherThenNumOptional()
		{
			BooleanQuery q = new BooleanQuery();
			q.Add(new TermQuery(new Term("all", "all")), BooleanClause.Occur.MUST);
			//true,  false);
			q.Add(new TermQuery(new Term("data", "6")), BooleanClause.Occur.MUST);
			//true,  false);
			q.Add(new TermQuery(new Term("data", "5")), BooleanClause.Occur.SHOULD);
			//false, false);
			q.Add(new TermQuery(new Term("data", "4")), BooleanClause.Occur.SHOULD);
			//false, false);
			q.Add(new TermQuery(new Term("data", "3")), BooleanClause.Occur.MUST_NOT);
			//false, true );
			q.Add(new TermQuery(new Term("data", "2")), BooleanClause.Occur.SHOULD);
			//false, false);
			q.Add(new TermQuery(new Term("data", "1")), BooleanClause.Occur.SHOULD);
			//false, false);
			q.Add(new TermQuery(new Term("data", "C")), BooleanClause.Occur.MUST_NOT);
			//false, true );
			q.SetMinimumNumberShouldMatch(90);
			// 90 of 4 optional ?!?!?!
			VerifyNrHits(q, 0);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestMinEqualToNumOptional()
		{
			BooleanQuery q = new BooleanQuery();
			q.Add(new TermQuery(new Term("all", "all")), BooleanClause.Occur.SHOULD);
			//false, false);
			q.Add(new TermQuery(new Term("data", "6")), BooleanClause.Occur.MUST);
			//true,  false);
			q.Add(new TermQuery(new Term("data", "3")), BooleanClause.Occur.MUST);
			//true,  false);
			q.Add(new TermQuery(new Term("data", "2")), BooleanClause.Occur.SHOULD);
			//false, false);
			q.SetMinimumNumberShouldMatch(2);
			// 2 of 2 optional 
			VerifyNrHits(q, 1);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestOneOptionalEqualToMin()
		{
			BooleanQuery q = new BooleanQuery();
			q.Add(new TermQuery(new Term("all", "all")), BooleanClause.Occur.MUST);
			//true,  false);
			q.Add(new TermQuery(new Term("data", "3")), BooleanClause.Occur.SHOULD);
			//false, false);
			q.Add(new TermQuery(new Term("data", "2")), BooleanClause.Occur.MUST);
			//true,  false);
			q.SetMinimumNumberShouldMatch(1);
			// 1 of 1 optional 
			VerifyNrHits(q, 1);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestNoOptionalButMin()
		{
			BooleanQuery q = new BooleanQuery();
			q.Add(new TermQuery(new Term("all", "all")), BooleanClause.Occur.MUST);
			//true,  false);
			q.Add(new TermQuery(new Term("data", "2")), BooleanClause.Occur.MUST);
			//true,  false);
			q.SetMinimumNumberShouldMatch(1);
			// 1 of 0 optional 
			VerifyNrHits(q, 0);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestNoOptionalButMin2()
		{
			BooleanQuery q = new BooleanQuery();
			q.Add(new TermQuery(new Term("all", "all")), BooleanClause.Occur.MUST);
			//true,  false);
			q.SetMinimumNumberShouldMatch(1);
			// 1 of 0 optional 
			VerifyNrHits(q, 0);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestRandomQueries()
		{
			string field = "data";
			string[] vals = new string[] { "1", "2", "3", "4", "5", "6", "A", "Z", "B", "Y", 
				"Z", "X", "foo" };
			int maxLev = 4;
			// callback object to set a random setMinimumNumberShouldMatch
			TestBoolean2.Callback minNrCB = new _Callback_317(field, vals);
			// also add a random negation
			// increase number of iterations for more complete testing      
			int num = AtLeast(20);
			for (int i = 0; i < num; i++)
			{
				int lev = Random().Next(maxLev);
				long seed = Random().NextLong();
				BooleanQuery q1 = TestBoolean2.RandBoolQuery(new Random(seed), true, lev, field, 
					vals, null);
				// BooleanQuery q2 = TestBoolean2.randBoolQuery(new Random(seed), lev, field, vals, minNrCB);
				BooleanQuery q2 = TestBoolean2.RandBoolQuery(new Random(seed), true, lev, field, 
					vals, null);
				// only set minimumNumberShouldMatch on the top level query since setting
				// at a lower level can change the score.
				minNrCB.PostCreate(q2);
				// Can't use Hits because normalized scores will mess things
				// up.  The non-sorting version of search() that returns TopDocs
				// will not normalize scores.
				TopDocs top1 = s.Search(q1, null, 100);
				TopDocs top2 = s.Search(q2, null, 100);
				if (i < 100)
				{
					QueryUtils.Check(Random(), q1, s);
					QueryUtils.Check(Random(), q2, s);
				}
				AssertSubsetOfSameScores(q2, top1, top2);
			}
		}

		private sealed class _Callback_317 : TestBoolean2.Callback
		{
			public _Callback_317(string field, string[] vals)
			{
				this.field = field;
				this.vals = vals;
			}

			public void PostCreate(BooleanQuery q)
			{
				BooleanClause[] c = q.GetClauses();
				int opt = 0;
				for (int i = 0; i < c.Length; i++)
				{
					if (c[i].GetOccur() == BooleanClause.Occur.SHOULD)
					{
						opt++;
					}
				}
				q.SetMinimumNumberShouldMatch(LuceneTestCase.Random().Next(opt + 2));
				if (LuceneTestCase.Random().NextBoolean())
				{
					Term randomTerm = new Term(field, vals[LuceneTestCase.Random().Next(vals.Length)]
						);
					q.Add(new TermQuery(randomTerm), BooleanClause.Occur.MUST_NOT);
				}
			}

			private readonly string field;

			private readonly string[] vals;
		}

		// System.out.println("Total hits:"+tot);
		private void AssertSubsetOfSameScores(Query q, TopDocs top1, TopDocs top2)
		{
			// The constrained query
			// should be a subset to the unconstrained query.
			if (top2.TotalHits > top1.TotalHits)
			{
				Fail("Constrained results not a subset:\n" + CheckHits.TopdocsString
					(top1, 0, 0) + CheckHits.TopdocsString(top2, 0, 0) + "for query:" + q.ToString()
					);
			}
			for (int hit = 0; hit < top2.TotalHits; hit++)
			{
				int id = top2.ScoreDocs[hit].Doc;
				float score = top2.ScoreDocs[hit].score;
				bool found = false;
				// find this doc in other hits
				for (int other = 0; other < top1.TotalHits; other++)
				{
					if (top1.ScoreDocs[other].Doc == id)
					{
						found = true;
						float otherScore = top1.ScoreDocs[other].score;
						// check if scores match
						AreEqual("Doc " + id + " scores don't match\n" + CheckHits
							.TopdocsString(top1, 0, 0) + CheckHits.TopdocsString(top2, 0, 0) + "for query:" 
							+ q.ToString(), score, otherScore, CheckHits.ExplainToleranceDelta(score, otherScore
							));
					}
				}
				// check if subset
				if (!found)
				{
					Fail("Doc " + id + " not found\n" + CheckHits.TopdocsString
						(top1, 0, 0) + CheckHits.TopdocsString(top2, 0, 0) + "for query:" + q.ToString()
						);
				}
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestRewriteCoord1()
		{
			Similarity oldSimilarity = s.GetSimilarity();
			try
			{
				s.SetSimilarity(new _DefaultSimilarity_401());
				BooleanQuery q1 = new BooleanQuery();
				q1.Add(new TermQuery(new Term("data", "1")), BooleanClause.Occur.SHOULD);
				BooleanQuery q2 = new BooleanQuery();
				q2.Add(new TermQuery(new Term("data", "1")), BooleanClause.Occur.SHOULD);
				q2.SetMinimumNumberShouldMatch(1);
				TopDocs top1 = s.Search(q1, null, 100);
				TopDocs top2 = s.Search(q2, null, 100);
				AssertSubsetOfSameScores(q2, top1, top2);
			}
			finally
			{
				s.SetSimilarity(oldSimilarity);
			}
		}

		private sealed class _DefaultSimilarity_401 : DefaultSimilarity
		{
			public _DefaultSimilarity_401()
			{
			}

			public override float Coord(int overlap, int maxOverlap)
			{
				return overlap / ((float)maxOverlap + 1);
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestRewriteNegate()
		{
			Similarity oldSimilarity = s.GetSimilarity();
			try
			{
				s.SetSimilarity(new _DefaultSimilarity_423());
				BooleanQuery q1 = new BooleanQuery();
				q1.Add(new TermQuery(new Term("data", "1")), BooleanClause.Occur.SHOULD);
				BooleanQuery q2 = new BooleanQuery();
				q2.Add(new TermQuery(new Term("data", "1")), BooleanClause.Occur.SHOULD);
				q2.Add(new TermQuery(new Term("data", "Z")), BooleanClause.Occur.MUST_NOT);
				TopDocs top1 = s.Search(q1, null, 100);
				TopDocs top2 = s.Search(q2, null, 100);
				AssertSubsetOfSameScores(q2, top1, top2);
			}
			finally
			{
				s.SetSimilarity(oldSimilarity);
			}
		}

		private sealed class _DefaultSimilarity_423 : DefaultSimilarity
		{
			public _DefaultSimilarity_423()
			{
			}

			public override float Coord(int overlap, int maxOverlap)
			{
				return overlap / ((float)maxOverlap + 1);
			}
		}

		/// <exception cref="System.Exception"></exception>
		protected internal virtual void PrintHits(string test, ScoreDoc[] h, IndexSearcher
			 searcher)
		{
			System.Console.Error.WriteLine("------- " + test + " -------");
			DecimalFormat f = new DecimalFormat("0.000000", DecimalFormatSymbols.GetInstance(
				CultureInfo.ROOT));
			for (int i = 0; i < h.Length; i++)
			{
				Lucene.Net.Documents.Document d = searcher.Doc(h[i].Doc);
				float score = h[i].score;
				System.Console.Error.WriteLine("#" + i + ": " + f.Format(score) + " - " + d.Get("id"
					) + " - " + d.Get("data"));
			}
		}
	}
}
