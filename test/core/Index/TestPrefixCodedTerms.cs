using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.TestFramework;
using Lucene.Net.TestFramework.Util;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Index
{
	public class TestPrefixCodedTerms : LuceneTestCase
	{
        [Test]
		public virtual void TestEmpty()
		{
			PrefixCodedTerms.Builder b = new PrefixCodedTerms.Builder();
			PrefixCodedTerms pb = b.Finish();
			IsFalse(pb.GetEnumerator().MoveNext());
		}

        [Test]
		public virtual void TestOne()
		{
			Term term = new Term("foo", "bogus");
			PrefixCodedTerms.Builder b = new PrefixCodedTerms.Builder();
			b.Add(term);
			PrefixCodedTerms pb = b.Finish();
			IEnumerator<Term> iterator = pb.GetEnumerator();
			IsTrue(iterator.MoveNext());
			AreEqual(term, iterator.Current);
		}

        [Test]
		public virtual void TestRandom()
		{
			ICollection<Term> terms = new HashSet<Term>();
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
			IEnumerator<Term> expected = terms.GetEnumerator();
			foreach (Term t in pb)
			{
				IsTrue(expected.MoveNext());
				AreEqual(expected.Current, t);
			}
			IsFalse(expected.MoveNext());
		}

		[Test]
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
			IEnumerator<Term> merged = new MergedIterator<Term>(pb1.GetEnumerator(), pb2.GetEnumerator());
			IsTrue(merged.MoveNext());
			AreEqual(t1, merged.Current);
			IsTrue(merged.MoveNext());
			AreEqual(t2, merged.Current);
		}

        [Test]
		public virtual void TestMergeRandom()
		{
			PrefixCodedTerms[] pb = new PrefixCodedTerms[Random().NextInt(2, 10)];
			ICollection<Term> superSet = new HashSet<Term>();
			for (int i = 0; i < pb.Length; i++)
			{
				ICollection<Term> terms = new HashSet<Term>();
				int nterms = Random().NextInt(0, 10000);
				for (int j = 0; j < nterms; j++)
				{
					Term term = new Term(TestUtil.RandomUnicodeString(Random(), 2), TestUtil.RandomUnicodeString
						(Random(), 4));
					terms.Add(term);
				}
                terms.ToList().ForEach(superSet.Add);
                
				PrefixCodedTerms.Builder b = new PrefixCodedTerms.Builder();
				foreach (Term @ref in terms)
				{
					b.Add(@ref);
				}
				pb[i] = b.Finish();
			}
			IList<IEnumerator<Term>> subs = pb.Select(t => t.GetEnumerator()).ToList();
            IEnumerator<Term> expected = superSet.GetEnumerator();
			IEnumerator<Term> actual = new MergedIterator<Term>(subs.ToArray());
			while (actual.MoveNext())
			{
				IsTrue(expected.MoveNext());
				AreEqual(expected.Current, actual.Current);
			}
			IsFalse(expected.MoveNext());
		}
	}
}
