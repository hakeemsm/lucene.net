/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search
{
	public class TestNGramPhraseQuery : LuceneTestCase
	{
		private static IndexReader reader;

		private static Directory directory;

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.BeforeClass]
		public static void BeforeClass()
		{
			directory = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), directory);
			writer.Close();
			reader = DirectoryReader.Open(directory);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.AfterClass]
		public static void AfterClass()
		{
			reader.Close();
			reader = null;
			directory.Close();
			directory = null;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestRewrite()
		{
			// bi-gram test ABC => AB/BC => AB/BC
			PhraseQuery pq1 = new NGramPhraseQuery(2);
			pq1.Add(new Term("f", "AB"));
			pq1.Add(new Term("f", "BC"));
			Query q = pq1.Rewrite(reader);
			IsTrue(q is NGramPhraseQuery);
			AreSame(pq1, q);
			pq1 = (NGramPhraseQuery)q;
			AssertArrayEquals(new Term[] { new Term("f", "AB"), new Term("f", "BC") }, pq1.GetTerms
				());
			AssertArrayEquals(new int[] { 0, 1 }, pq1.GetPositions());
			// bi-gram test ABCD => AB/BC/CD => AB//CD
			PhraseQuery pq2 = new NGramPhraseQuery(2);
			pq2.Add(new Term("f", "AB"));
			pq2.Add(new Term("f", "BC"));
			pq2.Add(new Term("f", "CD"));
			q = pq2.Rewrite(reader);
			IsTrue(q is PhraseQuery);
			AreNotSame(pq2, q);
			pq2 = (PhraseQuery)q;
			AssertArrayEquals(new Term[] { new Term("f", "AB"), new Term("f", "CD") }, pq2.GetTerms
				());
			AssertArrayEquals(new int[] { 0, 2 }, pq2.GetPositions());
			// tri-gram test ABCDEFGH => ABC/BCD/CDE/DEF/EFG/FGH => ABC///DEF//FGH
			PhraseQuery pq3 = new NGramPhraseQuery(3);
			pq3.Add(new Term("f", "ABC"));
			pq3.Add(new Term("f", "BCD"));
			pq3.Add(new Term("f", "CDE"));
			pq3.Add(new Term("f", "DEF"));
			pq3.Add(new Term("f", "EFG"));
			pq3.Add(new Term("f", "FGH"));
			q = pq3.Rewrite(reader);
			IsTrue(q is PhraseQuery);
			AreNotSame(pq3, q);
			pq3 = (PhraseQuery)q;
			AssertArrayEquals(new Term[] { new Term("f", "ABC"), new Term("f", "DEF"), new Term
				("f", "FGH") }, pq3.GetTerms());
			AssertArrayEquals(new int[] { 0, 3, 5 }, pq3.GetPositions());
			// LUCENE-4970: boosting test
			PhraseQuery pq4 = new NGramPhraseQuery(2);
			pq4.Add(new Term("f", "AB"));
			pq4.Add(new Term("f", "BC"));
			pq4.Add(new Term("f", "CD"));
			pq4.SetBoost(100.0F);
			q = pq4.Rewrite(reader);
			AreNotSame(pq4, q);
			AreEqual(pq4.GetBoost(), q.GetBoost(), 0.1f);
		}
	}
}
