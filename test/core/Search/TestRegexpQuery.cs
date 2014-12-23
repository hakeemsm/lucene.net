/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using Sharpen;

namespace Lucene.Net.Search
{
	/// <summary>Some simple regex tests, mostly converted from contrib's TestRegexQuery.
	/// 	</summary>
	/// <remarks>Some simple regex tests, mostly converted from contrib's TestRegexQuery.
	/// 	</remarks>
	public class TestRegexpQuery : LuceneTestCase
	{
		private IndexSearcher searcher;

		private IndexReader reader;

		private Directory directory;

		private readonly string FN = "field";

		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			directory = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), directory);
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			doc.Add(NewTextField(FN, "the quick brown fox jumps over the lazy ??? dog 493432 49344"
				, Field.Store.NO));
			writer.AddDocument(doc);
			reader = writer.GetReader();
			writer.Close();
			searcher = NewSearcher(reader);
		}

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			reader.Close();
			directory.Close();
			base.TearDown();
		}

		private Term NewTerm(string value)
		{
			return new Term(FN, value);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private int RegexQueryNrHits(string regex)
		{
			RegexpQuery query = new RegexpQuery(NewTerm(regex));
			return searcher.Search(query, 5).totalHits;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestRegex1()
		{
			NUnit.Framework.Assert.AreEqual(1, RegexQueryNrHits("q.[aeiou]c.*"));
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestRegex2()
		{
			NUnit.Framework.Assert.AreEqual(0, RegexQueryNrHits(".[aeiou]c.*"));
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestRegex3()
		{
			NUnit.Framework.Assert.AreEqual(0, RegexQueryNrHits("q.[aeiou]c"));
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestNumericRange()
		{
			NUnit.Framework.Assert.AreEqual(1, RegexQueryNrHits("<420000-600000>"));
			NUnit.Framework.Assert.AreEqual(0, RegexQueryNrHits("<493433-600000>"));
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestRegexComplement()
		{
			NUnit.Framework.Assert.AreEqual(1, RegexQueryNrHits("4934~[3]"));
			// not the empty lang, i.e. match all docs
			NUnit.Framework.Assert.AreEqual(1, RegexQueryNrHits("~#"));
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestCustomProvider()
		{
			AutomatonProvider myProvider = new _AutomatonProvider_98();
			// automaton that matches quick or brown
			RegexpQuery query = new RegexpQuery(NewTerm("<quickBrown>"), RegExp.ALL, myProvider
				);
			NUnit.Framework.Assert.AreEqual(1, searcher.Search(query, 5).totalHits);
		}

		private sealed class _AutomatonProvider_98 : AutomatonProvider
		{
			public _AutomatonProvider_98()
			{
				this.quickBrownAutomaton = BasicOperations.Union(Arrays.AsList(BasicAutomata.MakeString
					("quick"), BasicAutomata.MakeString("brown"), BasicAutomata.MakeString("bob")));
			}

			private Lucene.Net.Util.Automaton.Automaton quickBrownAutomaton;

			public Lucene.Net.Util.Automaton.Automaton GetAutomaton(string name)
			{
				if (name.Equals("quickBrown"))
				{
					return this.quickBrownAutomaton;
				}
				else
				{
					return null;
				}
			}
		}

		/// <summary>
		/// Test a corner case for backtracking: In this case the term dictionary has
		/// 493432 followed by 49344.
		/// </summary>
		/// <remarks>
		/// Test a corner case for backtracking: In this case the term dictionary has
		/// 493432 followed by 49344. When backtracking from 49343... to 4934, its
		/// necessary to test that 4934 itself is ok before trying to append more
		/// characters.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestBacktracking()
		{
			NUnit.Framework.Assert.AreEqual(1, RegexQueryNrHits("4934[314]"));
		}
	}
}
