/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;


namespace Lucene.Net.Search
{
	/// <summary>random sloppy phrase query tests</summary>
	public class TestSloppyPhraseQuery2 : SearchEquivalenceTestBase
	{
		/// <summary>"A B"~N âŠ† "A B"~N+1</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestIncreasingSloppiness()
		{
			Term t1 = RandomTerm();
			Term t2 = RandomTerm();
			PhraseQuery q1 = new PhraseQuery();
			q1.Add(t1);
			q1.Add(t2);
			PhraseQuery q2 = new PhraseQuery();
			q2.Add(t1);
			q2.Add(t2);
			for (int i = 0; i < 10; i++)
			{
				q1.SetSlop(i);
				q2.SetSlop(i + 1);
				AssertSubsetOf(q1, q2);
			}
		}

		/// <summary>same as the above with posincr</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestIncreasingSloppinessWithHoles()
		{
			Term t1 = RandomTerm();
			Term t2 = RandomTerm();
			PhraseQuery q1 = new PhraseQuery();
			q1.Add(t1);
			q1.Add(t2, 2);
			PhraseQuery q2 = new PhraseQuery();
			q2.Add(t1);
			q2.Add(t2, 2);
			for (int i = 0; i < 10; i++)
			{
				q1.SetSlop(i);
				q2.SetSlop(i + 1);
				AssertSubsetOf(q1, q2);
			}
		}

		/// <summary>"A B C"~N âŠ† "A B C"~N+1</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestIncreasingSloppiness3()
		{
			Term t1 = RandomTerm();
			Term t2 = RandomTerm();
			Term t3 = RandomTerm();
			PhraseQuery q1 = new PhraseQuery();
			q1.Add(t1);
			q1.Add(t2);
			q1.Add(t3);
			PhraseQuery q2 = new PhraseQuery();
			q2.Add(t1);
			q2.Add(t2);
			q2.Add(t3);
			for (int i = 0; i < 10; i++)
			{
				q1.SetSlop(i);
				q2.SetSlop(i + 1);
				AssertSubsetOf(q1, q2);
			}
		}

		/// <summary>same as the above with posincr</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestIncreasingSloppiness3WithHoles()
		{
			Term t1 = RandomTerm();
			Term t2 = RandomTerm();
			Term t3 = RandomTerm();
			int pos1 = 1 + Random().Next(3);
			int pos2 = pos1 + 1 + Random().Next(3);
			PhraseQuery q1 = new PhraseQuery();
			q1.Add(t1);
			q1.Add(t2, pos1);
			q1.Add(t3, pos2);
			PhraseQuery q2 = new PhraseQuery();
			q2.Add(t1);
			q2.Add(t2, pos1);
			q2.Add(t3, pos2);
			for (int i = 0; i < 10; i++)
			{
				q1.SetSlop(i);
				q2.SetSlop(i + 1);
				AssertSubsetOf(q1, q2);
			}
		}

		/// <summary>"A A"~N âŠ† "A A"~N+1</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestRepetitiveIncreasingSloppiness()
		{
			Term t = RandomTerm();
			PhraseQuery q1 = new PhraseQuery();
			q1.Add(t);
			q1.Add(t);
			PhraseQuery q2 = new PhraseQuery();
			q2.Add(t);
			q2.Add(t);
			for (int i = 0; i < 10; i++)
			{
				q1.SetSlop(i);
				q2.SetSlop(i + 1);
				AssertSubsetOf(q1, q2);
			}
		}

		/// <summary>same as the above with posincr</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestRepetitiveIncreasingSloppinessWithHoles()
		{
			Term t = RandomTerm();
			PhraseQuery q1 = new PhraseQuery();
			q1.Add(t);
			q1.Add(t, 2);
			PhraseQuery q2 = new PhraseQuery();
			q2.Add(t);
			q2.Add(t, 2);
			for (int i = 0; i < 10; i++)
			{
				q1.SetSlop(i);
				q2.SetSlop(i + 1);
				AssertSubsetOf(q1, q2);
			}
		}

		/// <summary>"A A A"~N âŠ† "A A A"~N+1</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestRepetitiveIncreasingSloppiness3()
		{
			Term t = RandomTerm();
			PhraseQuery q1 = new PhraseQuery();
			q1.Add(t);
			q1.Add(t);
			q1.Add(t);
			PhraseQuery q2 = new PhraseQuery();
			q2.Add(t);
			q2.Add(t);
			q2.Add(t);
			for (int i = 0; i < 10; i++)
			{
				q1.SetSlop(i);
				q2.SetSlop(i + 1);
				AssertSubsetOf(q1, q2);
			}
		}

		/// <summary>same as the above with posincr</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestRepetitiveIncreasingSloppiness3WithHoles()
		{
			Term t = RandomTerm();
			int pos1 = 1 + Random().Next(3);
			int pos2 = pos1 + 1 + Random().Next(3);
			PhraseQuery q1 = new PhraseQuery();
			q1.Add(t);
			q1.Add(t, pos1);
			q1.Add(t, pos2);
			PhraseQuery q2 = new PhraseQuery();
			q2.Add(t);
			q2.Add(t, pos1);
			q2.Add(t, pos2);
			for (int i = 0; i < 10; i++)
			{
				q1.SetSlop(i);
				q2.SetSlop(i + 1);
				AssertSubsetOf(q1, q2);
			}
		}

		/// <summary>MultiPhraseQuery~N âŠ† MultiPhraseQuery~N+1</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestRandomIncreasingSloppiness()
		{
			long seed = Random().NextLong();
			MultiPhraseQuery q1 = RandomPhraseQuery(seed);
			MultiPhraseQuery q2 = RandomPhraseQuery(seed);
			for (int i = 0; i < 10; i++)
			{
				q1.SetSlop(i);
				q2.SetSlop(i + 1);
				AssertSubsetOf(q1, q2);
			}
		}

		private MultiPhraseQuery RandomPhraseQuery(long seed)
		{
			Random random = new Random(seed);
			int length = TestUtil.NextInt(random, 2, 5);
			MultiPhraseQuery pq = new MultiPhraseQuery();
			int position = 0;
			for (int i = 0; i < length; i++)
			{
				int depth = TestUtil.NextInt(random, 1, 3);
				Term[] terms = new Term[depth];
				for (int j = 0; j < depth; j++)
				{
					terms[j] = new Term("field", string.Empty + (char)TestUtil.NextInt(random, 'a', 'z'
						));
				}
				pq.Add(terms, position);
				position += TestUtil.NextInt(random, 1, 3);
			}
			return pq;
		}
	}
}
