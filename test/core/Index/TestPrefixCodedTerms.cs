/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Test.Index
{
	public class TestPrefixCodedTerms : LuceneTestCase
	{
		public virtual void TestEmpty()
		{
			PrefixCodedTerms.Builder b = new PrefixCodedTerms.Builder();
			PrefixCodedTerms pb = b.Finish();
			IsFalse(pb.Iterator().HasNext());
		}

		public virtual void TestOne()
		{
			Term term = new Term("foo", "bogus");
			PrefixCodedTerms.Builder b = new PrefixCodedTerms.Builder();
			b.Add(term);
			PrefixCodedTerms pb = b.Finish();
			Iterator<Term> iterator = pb.Iterator();
			IsTrue(iterator.HasNext());
			AreEqual(term, iterator.Next());
		}

		public virtual void TestRandom()
		{
			ICollection<Term> terms = new TreeSet<Term>();
			int nterms = AtLeast(10000);
			for (int i = 0; i < nterms; i++)
			{
				Term term = new Term(TestUtil.RandomUnicodeString(Random(), 2), TestUtil.RandomUnicodeString
					(Random()));
				terms.Add(term);
			}
			PrefixCodedTerms.Builder b = new PrefixCodedTerms.Builder();
			foreach (Term @ref in terms)
			{
				b.Add(@ref);
			}
			PrefixCodedTerms pb = b.Finish();
			Iterator<Term> expected = terms.Iterator();
			foreach (Term t in pb)
			{
				IsTrue(expected.HasNext());
				AreEqual(expected.Next(), t);
			}
			IsFalse(expected.HasNext());
		}

		public virtual void TestMergeOne()
		{
			Term t1 = new Term("foo", "a");
			PrefixCodedTerms.Builder b1 = new PrefixCodedTerms.Builder();
			b1.Add(t1);
			PrefixCodedTerms pb1 = b1.Finish();
			Term t2 = new Term("foo", "b");
			PrefixCodedTerms.Builder b2 = new PrefixCodedTerms.Builder();
			b2.Add(t2);
			PrefixCodedTerms pb2 = b2.Finish();
			Iterator<Term> merged = new MergedIterator<Term>(pb1.Iterator(), pb2.Iterator());
			IsTrue(merged.HasNext());
			AreEqual(t1, merged.Next());
			IsTrue(merged.HasNext());
			AreEqual(t2, merged.Next());
		}

		public virtual void TestMergeRandom()
		{
			PrefixCodedTerms[] pb = new PrefixCodedTerms[TestUtil.NextInt(Random(), 2, 10)];
			ICollection<Term> superSet = new TreeSet<Term>();
			for (int i = 0; i < pb.Length; i++)
			{
				ICollection<Term> terms = new TreeSet<Term>();
				int nterms = TestUtil.NextInt(Random(), 0, 10000);
				for (int j = 0; j < nterms; j++)
				{
					Term term = new Term(TestUtil.RandomUnicodeString(Random(), 2), TestUtil.RandomUnicodeString
						(Random(), 4));
					terms.Add(term);
				}
				Sharpen.Collections.AddAll(superSet, terms);
				PrefixCodedTerms.Builder b = new PrefixCodedTerms.Builder();
				foreach (Term @ref in terms)
				{
					b.Add(@ref);
				}
				pb[i] = b.Finish();
			}
			IList<Iterator<Term>> subs = new List<Iterator<Term>>();
			for (int i_1 = 0; i_1 < pb.Length; i_1++)
			{
				subs.Add(pb[i_1].Iterator());
			}
			Iterator<Term> expected = superSet.Iterator();
			Iterator<Term> actual = new MergedIterator<Term>(Sharpen.Collections.ToArray(subs
				, new Iterator[0]));
			while (actual.HasNext())
			{
				IsTrue(expected.HasNext());
				AreEqual(expected.Next(), actual.Next());
			}
			IsFalse(expected.HasNext());
		}
	}
}
