/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Index;
using Lucene.Net.Search;
using Sharpen;

namespace Lucene.Net.Search
{
	/// <summary>Basic equivalence tests for core queries</summary>
	public class TestSimpleSearchEquivalence : SearchEquivalenceTestBase
	{
		// TODO: we could go a little crazy for a lot of these,
		// but these are just simple minimal cases in case something 
		// goes horribly wrong. Put more intense tests elsewhere.
		/// <summary>A âŠ† (A B)</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestTermVersusBooleanOr()
		{
			Term t1 = RandomTerm();
			Term t2 = RandomTerm();
			TermQuery q1 = new TermQuery(t1);
			BooleanQuery q2 = new BooleanQuery();
			q2.Add(new TermQuery(t1), BooleanClause.Occur.SHOULD);
			q2.Add(new TermQuery(t2), BooleanClause.Occur.SHOULD);
			AssertSubsetOf(q1, q2);
		}

		/// <summary>A âŠ† (+A B)</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestTermVersusBooleanReqOpt()
		{
			Term t1 = RandomTerm();
			Term t2 = RandomTerm();
			TermQuery q1 = new TermQuery(t1);
			BooleanQuery q2 = new BooleanQuery();
			q2.Add(new TermQuery(t1), BooleanClause.Occur.MUST);
			q2.Add(new TermQuery(t2), BooleanClause.Occur.SHOULD);
			AssertSubsetOf(q1, q2);
		}

		/// <summary>(A -B) âŠ† A</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestBooleanReqExclVersusTerm()
		{
			Term t1 = RandomTerm();
			Term t2 = RandomTerm();
			BooleanQuery q1 = new BooleanQuery();
			q1.Add(new TermQuery(t1), BooleanClause.Occur.MUST);
			q1.Add(new TermQuery(t2), BooleanClause.Occur.MUST_NOT);
			TermQuery q2 = new TermQuery(t1);
			AssertSubsetOf(q1, q2);
		}

		/// <summary>(+A +B) âŠ† (A B)</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestBooleanAndVersusBooleanOr()
		{
			Term t1 = RandomTerm();
			Term t2 = RandomTerm();
			BooleanQuery q1 = new BooleanQuery();
			q1.Add(new TermQuery(t1), BooleanClause.Occur.SHOULD);
			q1.Add(new TermQuery(t2), BooleanClause.Occur.SHOULD);
			BooleanQuery q2 = new BooleanQuery();
			q2.Add(new TermQuery(t1), BooleanClause.Occur.SHOULD);
			q2.Add(new TermQuery(t2), BooleanClause.Occur.SHOULD);
			AssertSubsetOf(q1, q2);
		}

		/// <summary>(A B) = (A | B)</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestDisjunctionSumVersusDisjunctionMax()
		{
			Term t1 = RandomTerm();
			Term t2 = RandomTerm();
			BooleanQuery q1 = new BooleanQuery();
			q1.Add(new TermQuery(t1), BooleanClause.Occur.SHOULD);
			q1.Add(new TermQuery(t2), BooleanClause.Occur.SHOULD);
			DisjunctionMaxQuery q2 = new DisjunctionMaxQuery(0.5f);
			q2.Add(new TermQuery(t1));
			q2.Add(new TermQuery(t2));
			AssertSameSet(q1, q2);
		}

		/// <summary>"A B" âŠ† (+A +B)</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestExactPhraseVersusBooleanAnd()
		{
			Term t1 = RandomTerm();
			Term t2 = RandomTerm();
			PhraseQuery q1 = new PhraseQuery();
			q1.Add(t1);
			q1.Add(t2);
			BooleanQuery q2 = new BooleanQuery();
			q2.Add(new TermQuery(t1), BooleanClause.Occur.MUST);
			q2.Add(new TermQuery(t2), BooleanClause.Occur.MUST);
			AssertSubsetOf(q1, q2);
		}

		/// <summary>same as above, with posincs</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestExactPhraseVersusBooleanAndWithHoles()
		{
			Term t1 = RandomTerm();
			Term t2 = RandomTerm();
			PhraseQuery q1 = new PhraseQuery();
			q1.Add(t1);
			q1.Add(t2, 2);
			BooleanQuery q2 = new BooleanQuery();
			q2.Add(new TermQuery(t1), BooleanClause.Occur.MUST);
			q2.Add(new TermQuery(t2), BooleanClause.Occur.MUST);
			AssertSubsetOf(q1, q2);
		}

		/// <summary>"A B" âŠ† "A B"~1</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestPhraseVersusSloppyPhrase()
		{
			Term t1 = RandomTerm();
			Term t2 = RandomTerm();
			PhraseQuery q1 = new PhraseQuery();
			q1.Add(t1);
			q1.Add(t2);
			PhraseQuery q2 = new PhraseQuery();
			q2.Add(t1);
			q2.Add(t2);
			q2.SetSlop(1);
			AssertSubsetOf(q1, q2);
		}

		/// <summary>same as above, with posincs</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestPhraseVersusSloppyPhraseWithHoles()
		{
			Term t1 = RandomTerm();
			Term t2 = RandomTerm();
			PhraseQuery q1 = new PhraseQuery();
			q1.Add(t1);
			q1.Add(t2, 2);
			PhraseQuery q2 = new PhraseQuery();
			q2.Add(t1);
			q2.Add(t2, 2);
			q2.SetSlop(1);
			AssertSubsetOf(q1, q2);
		}

		/// <summary>"A B" âŠ† "A (B C)"</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestExactPhraseVersusMultiPhrase()
		{
			Term t1 = RandomTerm();
			Term t2 = RandomTerm();
			PhraseQuery q1 = new PhraseQuery();
			q1.Add(t1);
			q1.Add(t2);
			Term t3 = RandomTerm();
			MultiPhraseQuery q2 = new MultiPhraseQuery();
			q2.Add(t1);
			q2.Add(new Term[] { t2, t3 });
			AssertSubsetOf(q1, q2);
		}

		/// <summary>same as above, with posincs</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestExactPhraseVersusMultiPhraseWithHoles()
		{
			Term t1 = RandomTerm();
			Term t2 = RandomTerm();
			PhraseQuery q1 = new PhraseQuery();
			q1.Add(t1);
			q1.Add(t2, 2);
			Term t3 = RandomTerm();
			MultiPhraseQuery q2 = new MultiPhraseQuery();
			q2.Add(t1);
			q2.Add(new Term[] { t2, t3 }, 2);
			AssertSubsetOf(q1, q2);
		}

		/// <summary>"A B"~âˆž = +A +B if A != B</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestSloppyPhraseVersusBooleanAnd()
		{
			Term t1 = RandomTerm();
			Term t2 = null;
			do
			{
				// semantics differ from SpanNear: SloppyPhrase handles repeats,
				// so we must ensure t1 != t2
				t2 = RandomTerm();
			}
			while (t1.Equals(t2));
			PhraseQuery q1 = new PhraseQuery();
			q1.Add(t1);
			q1.Add(t2);
			q1.SetSlop(int.MaxValue);
			BooleanQuery q2 = new BooleanQuery();
			q2.Add(new TermQuery(t1), BooleanClause.Occur.MUST);
			q2.Add(new TermQuery(t2), BooleanClause.Occur.MUST);
			AssertSameSet(q1, q2);
		}
	}
}
