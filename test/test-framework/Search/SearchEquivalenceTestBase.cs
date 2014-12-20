/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Text;
using Lucene.Net.TestFramework.Analysis;
using Lucene.NetDocument;
using Lucene.Net.TestFramework.Index;
using Lucene.Net.TestFramework.Search;
using Lucene.Net.TestFramework.Store;
using Lucene.Net.TestFramework.Util;
using Lucene.Net.TestFramework.Util.Automaton;
using Sharpen;

namespace Lucene.Net.TestFramework.Search
{
	/// <summary>Simple base class for checking search equivalence.</summary>
	/// <remarks>
	/// Simple base class for checking search equivalence.
	/// Extend it, and write tests that create
	/// <see cref="RandomTerm()">RandomTerm()</see>
	/// s
	/// (all terms are single characters a-z), and use
	/// <see cref="AssertSameSet(Query, Query)">AssertSameSet(Query, Query)</see>
	/// and
	/// <see cref="AssertSubsetOf(Query, Query)">AssertSubsetOf(Query, Query)</see>
	/// </remarks>
	public abstract class SearchEquivalenceTestBase : LuceneTestCase
	{
		protected internal static IndexSearcher s1;

		protected internal static IndexSearcher s2;

		protected internal static Directory directory;

		protected internal static IndexReader reader;

		protected internal static Analyzer analyzer;

		protected internal static string stopword;

		// we always pick a character as a stopword
		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.BeforeClass]
		public static void BeforeClass()
		{
			Random random = Random();
			directory = NewDirectory();
			stopword = string.Empty + RandomChar();
			CharacterRunAutomaton stopset = new CharacterRunAutomaton(BasicAutomata.MakeString
				(stopword));
			analyzer = new MockAnalyzer(random, MockTokenizer.WHITESPACE, false, stopset);
			RandomIndexWriter iw = new RandomIndexWriter(random, directory, analyzer);
			Lucene.NetDocument.Document doc = new Lucene.NetDocument.Document
				();
			Field id = new StringField("id", string.Empty, Field.Store.NO);
			Field field = new TextField("field", string.Empty, Field.Store.NO);
			doc.Add(id);
			doc.Add(field);
			// index some docs
			int numDocs = AtLeast(1000);
			for (int i = 0; i < numDocs; i++)
			{
				id.SetStringValue(Sharpen.Extensions.ToString(i));
				field.SetStringValue(RandomFieldContents());
				iw.AddDocument(doc);
			}
			// delete some docs
			int numDeletes = numDocs / 20;
			for (int i_1 = 0; i_1 < numDeletes; i_1++)
			{
				Term toDelete = new Term("id", Sharpen.Extensions.ToString(random.Next(numDocs)));
				if (random.NextBoolean())
				{
					iw.DeleteDocuments(toDelete);
				}
				else
				{
					iw.DeleteDocuments(new TermQuery(toDelete));
				}
			}
			reader = iw.GetReader();
			s1 = NewSearcher(reader);
			s2 = NewSearcher(reader);
			iw.Close();
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.AfterClass]
		public static void AfterClass()
		{
			reader.Close();
			directory.Close();
			analyzer.Close();
			reader = null;
			directory = null;
			analyzer = null;
			s1 = s2 = null;
		}

		/// <summary>populate a field with random contents.</summary>
		/// <remarks>
		/// populate a field with random contents.
		/// terms should be single characters in lowercase (a-z)
		/// tokenization can be assumed to be on whitespace.
		/// </remarks>
		internal static string RandomFieldContents()
		{
			// TODO: zipf-like distribution
			StringBuilder sb = new StringBuilder();
			int numTerms = Random().Next(15);
			for (int i = 0; i < numTerms; i++)
			{
				if (sb.Length > 0)
				{
					sb.Append(' ');
				}
				// whitespace
				sb.Append(RandomChar());
			}
			return sb.ToString();
		}

		/// <summary>returns random character (a-z)</summary>
		internal static char RandomChar()
		{
			return (char)TestUtil.NextInt(Random(), 'a', 'z');
		}

		/// <summary>returns a term suitable for searching.</summary>
		/// <remarks>
		/// returns a term suitable for searching.
		/// terms are single characters in lowercase (a-z)
		/// </remarks>
		protected internal virtual Term RandomTerm()
		{
			return new Term("field", string.Empty + RandomChar());
		}

		/// <summary>Returns a random filter over the document set</summary>
		protected internal virtual Filter RandomFilter()
		{
			return new QueryWrapperFilter(TermRangeQuery.NewStringRange("field", "a", string.Empty
				 + RandomChar(), true, true));
		}

		/// <summary>
		/// Asserts that the documents returned by <code>q1</code>
		/// are the same as of those returned by <code>q2</code>
		/// </summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void AssertSameSet(Query q1, Query q2)
		{
			AssertSubsetOf(q1, q2);
			AssertSubsetOf(q2, q1);
		}

		/// <summary>
		/// Asserts that the documents returned by <code>q1</code>
		/// are a subset of those returned by <code>q2</code>
		/// </summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void AssertSubsetOf(Query q1, Query q2)
		{
			// test without a filter
			AssertSubsetOf(q1, q2, null);
			// test with a filter (this will sometimes cause advance'ing enough to test it)
			AssertSubsetOf(q1, q2, RandomFilter());
		}

		/// <summary>
		/// Asserts that the documents returned by <code>q1</code>
		/// are a subset of those returned by <code>q2</code>.
		/// </summary>
		/// <remarks>
		/// Asserts that the documents returned by <code>q1</code>
		/// are a subset of those returned by <code>q2</code>.
		/// Both queries will be filtered by <code>filter</code>
		/// </remarks>
		/// <exception cref="System.Exception"></exception>
		protected internal virtual void AssertSubsetOf(Query q1, Query q2, Filter filter)
		{
			// TRUNK ONLY: test both filter code paths
			if (filter != null && Random().NextBoolean())
			{
				q1 = new FilteredQuery(q1, filter, TestUtil.RandomFilterStrategy(Random()));
				q2 = new FilteredQuery(q2, filter, TestUtil.RandomFilterStrategy(Random()));
				filter = null;
			}
			// not efficient, but simple!
			TopDocs td1 = s1.Search(q1, filter, reader.MaxDoc());
			TopDocs td2 = s2.Search(q2, filter, reader.MaxDoc());
			NUnit.Framework.Assert.IsTrue(td1.totalHits <= td2.totalHits);
			// fill the superset into a bitset
			BitSet bitset = new BitSet();
			for (int i = 0; i < td2.scoreDocs.Length; i++)
			{
				bitset.Set(td2.scoreDocs[i].doc);
			}
			// check in the subset, that every bit was set by the super
			for (int i_1 = 0; i_1 < td1.scoreDocs.Length; i_1++)
			{
				NUnit.Framework.Assert.IsTrue(bitset.Get(td1.scoreDocs[i_1].doc));
			}
		}
	}
}
