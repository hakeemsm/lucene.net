/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Search.Spans;


namespace Lucene.Net.Search
{
	/// <summary>
	/// TestExplanations subclass that builds up super crazy complex queries
	/// on the assumption that if the explanations work out right for them,
	/// they should work for anything.
	/// </summary>
	/// <remarks>
	/// TestExplanations subclass that builds up super crazy complex queries
	/// on the assumption that if the explanations work out right for them,
	/// they should work for anything.
	/// </remarks>
	public class TestComplexExplanations : TestExplanations
	{
		/// <summary>
		/// Override the Similarity used in our searcher with one that plays
		/// nice with boosts of 0.0
		/// </summary>
		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			searcher.SetSimilarity(CreateQnorm1Similarity());
		}

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			searcher.SetSimilarity(IndexSearcher.DefaultSimilarity);
			base.TearDown();
		}

		// must be static for weight serialization tests 
		private static DefaultSimilarity CreateQnorm1Similarity()
		{
			return new _DefaultSimilarity_50();
		}

		private sealed class _DefaultSimilarity_50 : DefaultSimilarity
		{
			public _DefaultSimilarity_50()
			{
			}

			public override float QueryNorm(float sumOfSquaredWeights)
			{
				return 1.0f;
			}
		}

		// / (float) Math.sqrt(1.0f + sumOfSquaredWeights);
		/// <exception cref="System.Exception"></exception>
		public virtual void Test1()
		{
			BooleanQuery q = new BooleanQuery();
			PhraseQuery phraseQuery = new PhraseQuery();
			phraseQuery.SetSlop(1);
			phraseQuery.Add(new Term(FIELD, "w1"));
			phraseQuery.Add(new Term(FIELD, "w2"));
			q.Add(phraseQuery, BooleanClause.Occur.MUST);
			q.Add(Snear(St("w2"), Sor("w5", "zz"), 4, true), BooleanClause.Occur.SHOULD);
			q.Add(Snear(Sf("w3", 2), St("w2"), St("w3"), 5, true), BooleanClause.Occur.SHOULD
				);
			Query t = new FilteredQuery(new TermQuery(new Term(FIELD, "xx")), new TestExplanations.ItemizedFilter
				(new int[] { 1, 3 }));
			t.SetBoost(1000);
			q.Add(t, BooleanClause.Occur.SHOULD);
			t = new ConstantScoreQuery(new TestExplanations.ItemizedFilter(new int[] { 0, 2 }
				));
			t.SetBoost(30);
			q.Add(t, BooleanClause.Occur.SHOULD);
			DisjunctionMaxQuery dm = new DisjunctionMaxQuery(0.2f);
			dm.Add(Snear(St("w2"), Sor("w5", "zz"), 4, true));
			dm.Add(new TermQuery(new Term(FIELD, "QQ")));
			BooleanQuery xxYYZZ = new BooleanQuery();
			xxYYZZ.Add(new TermQuery(new Term(FIELD, "xx")), BooleanClause.Occur.SHOULD);
			xxYYZZ.Add(new TermQuery(new Term(FIELD, "yy")), BooleanClause.Occur.SHOULD);
			xxYYZZ.Add(new TermQuery(new Term(FIELD, "zz")), BooleanClause.Occur.MUST_NOT);
			dm.Add(xxYYZZ);
			BooleanQuery xxW1 = new BooleanQuery();
			xxW1.Add(new TermQuery(new Term(FIELD, "xx")), BooleanClause.Occur.MUST_NOT);
			xxW1.Add(new TermQuery(new Term(FIELD, "w1")), BooleanClause.Occur.MUST_NOT);
			dm.Add(xxW1);
			DisjunctionMaxQuery dm2 = new DisjunctionMaxQuery(0.5f);
			dm2.Add(new TermQuery(new Term(FIELD, "w1")));
			dm2.Add(new TermQuery(new Term(FIELD, "w2")));
			dm2.Add(new TermQuery(new Term(FIELD, "w3")));
			dm.Add(dm2);
			q.Add(dm, BooleanClause.Occur.SHOULD);
			BooleanQuery b = new BooleanQuery();
			b.SetMinimumNumberShouldMatch(2);
			b.Add(Snear("w1", "w2", 1, true), BooleanClause.Occur.SHOULD);
			b.Add(Snear("w2", "w3", 1, true), BooleanClause.Occur.SHOULD);
			b.Add(Snear("w1", "w3", 3, true), BooleanClause.Occur.SHOULD);
			q.Add(b, BooleanClause.Occur.SHOULD);
			Qtest(q, new int[] { 0, 1, 2 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void Test2()
		{
			BooleanQuery q = new BooleanQuery();
			PhraseQuery phraseQuery = new PhraseQuery();
			phraseQuery.SetSlop(1);
			phraseQuery.Add(new Term(FIELD, "w1"));
			phraseQuery.Add(new Term(FIELD, "w2"));
			q.Add(phraseQuery, BooleanClause.Occur.MUST);
			q.Add(Snear(St("w2"), Sor("w5", "zz"), 4, true), BooleanClause.Occur.SHOULD);
			q.Add(Snear(Sf("w3", 2), St("w2"), St("w3"), 5, true), BooleanClause.Occur.SHOULD
				);
			Query t = new FilteredQuery(new TermQuery(new Term(FIELD, "xx")), new TestExplanations.ItemizedFilter
				(new int[] { 1, 3 }));
			t.SetBoost(1000);
			q.Add(t, BooleanClause.Occur.SHOULD);
			t = new ConstantScoreQuery(new TestExplanations.ItemizedFilter(new int[] { 0, 2 }
				));
			t.SetBoost(-20.0f);
			q.Add(t, BooleanClause.Occur.SHOULD);
			DisjunctionMaxQuery dm = new DisjunctionMaxQuery(0.2f);
			dm.Add(Snear(St("w2"), Sor("w5", "zz"), 4, true));
			dm.Add(new TermQuery(new Term(FIELD, "QQ")));
			BooleanQuery xxYYZZ = new BooleanQuery();
			xxYYZZ.Add(new TermQuery(new Term(FIELD, "xx")), BooleanClause.Occur.SHOULD);
			xxYYZZ.Add(new TermQuery(new Term(FIELD, "yy")), BooleanClause.Occur.SHOULD);
			xxYYZZ.Add(new TermQuery(new Term(FIELD, "zz")), BooleanClause.Occur.MUST_NOT);
			dm.Add(xxYYZZ);
			BooleanQuery xxW1 = new BooleanQuery();
			xxW1.Add(new TermQuery(new Term(FIELD, "xx")), BooleanClause.Occur.MUST_NOT);
			xxW1.Add(new TermQuery(new Term(FIELD, "w1")), BooleanClause.Occur.MUST_NOT);
			dm.Add(xxW1);
			DisjunctionMaxQuery dm2 = new DisjunctionMaxQuery(0.5f);
			dm2.Add(new TermQuery(new Term(FIELD, "w1")));
			dm2.Add(new TermQuery(new Term(FIELD, "w2")));
			dm2.Add(new TermQuery(new Term(FIELD, "w3")));
			dm.Add(dm2);
			q.Add(dm, BooleanClause.Occur.SHOULD);
			BooleanQuery b = new BooleanQuery();
			b.SetMinimumNumberShouldMatch(2);
			b.Add(Snear("w1", "w2", 1, true), BooleanClause.Occur.SHOULD);
			b.Add(Snear("w2", "w3", 1, true), BooleanClause.Occur.SHOULD);
			b.Add(Snear("w1", "w3", 3, true), BooleanClause.Occur.SHOULD);
			b.SetBoost(0.0f);
			q.Add(b, BooleanClause.Occur.SHOULD);
			Qtest(q, new int[] { 0, 1, 2 });
		}

		// :TODO: we really need more crazy complex cases.
		// //////////////////////////////////////////////////////////////////
		// The rest of these aren't that complex, but they are <i>somewhat</i>
		// complex, and they expose weakness in dealing with queries that match
		// with scores of 0 wrapped in other queries
		/// <exception cref="System.Exception"></exception>
		public virtual void TestT3()
		{
			TermQuery query = new TermQuery(new Term(FIELD, "w1"));
			query.SetBoost(0);
			Bqtest(query, new int[] { 0, 1, 2, 3 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestMA3()
		{
			Query q = new MatchAllDocsQuery();
			q.SetBoost(0);
			Bqtest(q, new int[] { 0, 1, 2, 3 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestFQ5()
		{
			TermQuery query = new TermQuery(new Term(FIELD, "xx"));
			query.SetBoost(0);
			Bqtest(new FilteredQuery(query, new TestExplanations.ItemizedFilter(new int[] { 1
				, 3 })), new int[] { 3 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestCSQ4()
		{
			Query q = new ConstantScoreQuery(new TestExplanations.ItemizedFilter(new int[] { 
				3 }));
			q.SetBoost(0);
			Bqtest(q, new int[] { 3 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestDMQ10()
		{
			DisjunctionMaxQuery q = new DisjunctionMaxQuery(0.5f);
			BooleanQuery query = new BooleanQuery();
			query.Add(new TermQuery(new Term(FIELD, "yy")), BooleanClause.Occur.SHOULD);
			TermQuery boostedQuery = new TermQuery(new Term(FIELD, "w5"));
			boostedQuery.SetBoost(100);
			query.Add(boostedQuery, BooleanClause.Occur.SHOULD);
			q.Add(query);
			TermQuery xxBoostedQuery = new TermQuery(new Term(FIELD, "xx"));
			xxBoostedQuery.SetBoost(0);
			q.Add(xxBoostedQuery);
			q.SetBoost(0.0f);
			Bqtest(q, new int[] { 0, 2, 3 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestMPQ7()
		{
			MultiPhraseQuery q = new MultiPhraseQuery();
			q.Add(Ta(new string[] { "w1" }));
			q.Add(Ta(new string[] { "w2" }));
			q.SetSlop(1);
			q.SetBoost(0.0f);
			Bqtest(q, new int[] { 0, 1, 2 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestBQ12()
		{
			// NOTE: using qtest not bqtest
			BooleanQuery query = new BooleanQuery();
			query.Add(new TermQuery(new Term(FIELD, "w1")), BooleanClause.Occur.SHOULD);
			TermQuery boostedQuery = new TermQuery(new Term(FIELD, "w2"));
			boostedQuery.SetBoost(0);
			query.Add(boostedQuery, BooleanClause.Occur.SHOULD);
			Qtest(query, new int[] { 0, 1, 2, 3 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestBQ13()
		{
			// NOTE: using qtest not bqtest
			BooleanQuery query = new BooleanQuery();
			query.Add(new TermQuery(new Term(FIELD, "w1")), BooleanClause.Occur.SHOULD);
			TermQuery boostedQuery = new TermQuery(new Term(FIELD, "w5"));
			boostedQuery.SetBoost(0);
			query.Add(boostedQuery, BooleanClause.Occur.MUST_NOT);
			Qtest(query, new int[] { 1, 2, 3 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestBQ18()
		{
			// NOTE: using qtest not bqtest
			BooleanQuery query = new BooleanQuery();
			TermQuery boostedQuery = new TermQuery(new Term(FIELD, "w1"));
			boostedQuery.SetBoost(0);
			query.Add(boostedQuery, BooleanClause.Occur.MUST);
			query.Add(new TermQuery(new Term(FIELD, "w2")), BooleanClause.Occur.SHOULD);
			Qtest(query, new int[] { 0, 1, 2, 3 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestBQ21()
		{
			BooleanQuery query = new BooleanQuery();
			query.Add(new TermQuery(new Term(FIELD, "w1")), BooleanClause.Occur.MUST);
			query.Add(new TermQuery(new Term(FIELD, "w2")), BooleanClause.Occur.SHOULD);
			query.SetBoost(0);
			Bqtest(query, new int[] { 0, 1, 2, 3 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestBQ22()
		{
			BooleanQuery query = new BooleanQuery();
			TermQuery boostedQuery = new TermQuery(new Term(FIELD, "w1"));
			boostedQuery.SetBoost(0);
			query.Add(boostedQuery, BooleanClause.Occur.MUST);
			query.Add(new TermQuery(new Term(FIELD, "w2")), BooleanClause.Occur.SHOULD);
			query.SetBoost(0);
			Bqtest(query, new int[] { 0, 1, 2, 3 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestST3()
		{
			SpanQuery q = St("w1");
			q.SetBoost(0);
			Bqtest(q, new int[] { 0, 1, 2, 3 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestST6()
		{
			SpanQuery q = St("xx");
			q.SetBoost(0);
			Qtest(q, new int[] { 2, 3 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSF3()
		{
			SpanQuery q = Sf(("w1"), 1);
			q.SetBoost(0);
			Bqtest(q, new int[] { 0, 1, 2, 3 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSF7()
		{
			SpanQuery q = Sf(("xx"), 3);
			q.SetBoost(0);
			Bqtest(q, new int[] { 2, 3 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSNot3()
		{
			SpanQuery q = Snot(Sf("w1", 10), St("QQ"));
			q.SetBoost(0);
			Bqtest(q, new int[] { 0, 1, 2, 3 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSNot6()
		{
			SpanQuery q = Snot(Sf("w1", 10), St("xx"));
			q.SetBoost(0);
			Bqtest(q, new int[] { 0, 1, 2, 3 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSNot8()
		{
			// NOTE: using qtest not bqtest
			SpanQuery f = Snear("w1", "w3", 10, true);
			f.SetBoost(0);
			SpanQuery q = Snot(f, St("xx"));
			Qtest(q, new int[] { 0, 1, 3 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSNot9()
		{
			// NOTE: using qtest not bqtest
			SpanQuery t = St("xx");
			t.SetBoost(0);
			SpanQuery q = Snot(Snear("w1", "w3", 10, true), t);
			Qtest(q, new int[] { 0, 1, 3 });
		}
	}
}
