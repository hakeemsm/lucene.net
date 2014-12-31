/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Spans;


namespace Lucene.Net.Search.Spans
{
	/// <summary>Basic equivalence tests for span queries</summary>
	public class TestSpanSearchEquivalence : SearchEquivalenceTestBase
	{
		// TODO: we could go a little crazy for a lot of these,
		// but these are just simple minimal cases in case something 
		// goes horribly wrong. Put more intense tests elsewhere.
		/// <summary>SpanTermQuery(A) = TermQuery(A)</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestSpanTermVersusTerm()
		{
			Term t1 = RandomTerm();
			AssertSameSet(new TermQuery(t1), new SpanTermQuery(t1));
		}

		/// <summary>SpanOrQuery(A, B) = (A B)</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestSpanOrVersusBoolean()
		{
			Term t1 = RandomTerm();
			Term t2 = RandomTerm();
			BooleanQuery q1 = new BooleanQuery();
			q1.Add(new TermQuery(t1), BooleanClause.Occur.SHOULD);
			q1.Add(new TermQuery(t2), BooleanClause.Occur.SHOULD);
			SpanOrQuery q2 = new SpanOrQuery(new SpanTermQuery(t1), new SpanTermQuery(t2));
			AssertSameSet(q1, q2);
		}

		/// <summary>SpanNotQuery(A, B) âŠ† SpanTermQuery(A)</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestSpanNotVersusSpanTerm()
		{
			Term t1 = RandomTerm();
			Term t2 = RandomTerm();
			AssertSubsetOf(new SpanNotQuery(new SpanTermQuery(t1), new SpanTermQuery(t2)), new 
				SpanTermQuery(t1));
		}

		/// <summary>SpanFirstQuery(A, 10) âŠ† SpanTermQuery(A)</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestSpanFirstVersusSpanTerm()
		{
			Term t1 = RandomTerm();
			AssertSubsetOf(new SpanFirstQuery(new SpanTermQuery(t1), 10), new SpanTermQuery(t1
				));
		}

		/// <summary>SpanNearQuery([A, B], 0, true) = "A B"</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestSpanNearVersusPhrase()
		{
			Term t1 = RandomTerm();
			Term t2 = RandomTerm();
			SpanQuery[] subquery = new SpanQuery[] { new SpanTermQuery(t1), new SpanTermQuery
				(t2) };
			SpanNearQuery q1 = new SpanNearQuery(subquery, 0, true);
			PhraseQuery q2 = new PhraseQuery();
			q2.Add(t1);
			q2.Add(t2);
			AssertSameSet(q1, q2);
		}

		/// <summary>SpanNearQuery([A, B], âˆž, false) = +A +B</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestSpanNearVersusBooleanAnd()
		{
			Term t1 = RandomTerm();
			Term t2 = RandomTerm();
			SpanQuery[] subquery = new SpanQuery[] { new SpanTermQuery(t1), new SpanTermQuery
				(t2) };
			SpanNearQuery q1 = new SpanNearQuery(subquery, int.MaxValue, false);
			BooleanQuery q2 = new BooleanQuery();
			q2.Add(new TermQuery(t1), BooleanClause.Occur.MUST);
			q2.Add(new TermQuery(t2), BooleanClause.Occur.MUST);
			AssertSameSet(q1, q2);
		}

		/// <summary>SpanNearQuery([A B], 0, false) âŠ† SpanNearQuery([A B], 1, false)</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestSpanNearVersusSloppySpanNear()
		{
			Term t1 = RandomTerm();
			Term t2 = RandomTerm();
			SpanQuery[] subquery = new SpanQuery[] { new SpanTermQuery(t1), new SpanTermQuery
				(t2) };
			SpanNearQuery q1 = new SpanNearQuery(subquery, 0, false);
			SpanNearQuery q2 = new SpanNearQuery(subquery, 1, false);
			AssertSubsetOf(q1, q2);
		}

		/// <summary>SpanNearQuery([A B], 3, true) âŠ† SpanNearQuery([A B], 3, false)</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestSpanNearInOrderVersusOutOfOrder()
		{
			Term t1 = RandomTerm();
			Term t2 = RandomTerm();
			SpanQuery[] subquery = new SpanQuery[] { new SpanTermQuery(t1), new SpanTermQuery
				(t2) };
			SpanNearQuery q1 = new SpanNearQuery(subquery, 3, true);
			SpanNearQuery q2 = new SpanNearQuery(subquery, 3, false);
			AssertSubsetOf(q1, q2);
		}
	}
}
