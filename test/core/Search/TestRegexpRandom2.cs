/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Codecs;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using Sharpen;

namespace Lucene.Net.Search
{
	/// <summary>
	/// Create an index with random unicode terms
	/// Generates random regexps, and validates against a simple impl.
	/// </summary>
	/// <remarks>
	/// Create an index with random unicode terms
	/// Generates random regexps, and validates against a simple impl.
	/// </remarks>
	public class TestRegexpRandom2 : LuceneTestCase
	{
		protected internal IndexSearcher searcher1;

		protected internal IndexSearcher searcher2;

		private IndexReader reader;

		private Directory dir;

		protected internal string fieldName;

		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			dir = NewDirectory();
			fieldName = Random().NextBoolean() ? "field" : string.Empty;
			// sometimes use an empty string as field name
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, ((IndexWriterConfig
				)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random(), MockTokenizer
				.KEYWORD, false)).SetMaxBufferedDocs(TestUtil.NextInt(Random(), 50, 1000))));
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			Field field = NewStringField(fieldName, string.Empty, Field.Store.NO);
			doc.Add(field);
			IList<string> terms = new AList<string>();
			int num = AtLeast(200);
			for (int i = 0; i < num; i++)
			{
				string s = TestUtil.RandomUnicodeString(Random());
				field.SetStringValue(s);
				terms.AddItem(s);
				writer.AddDocument(doc);
			}
			if (VERBOSE)
			{
				// utf16 order
				terms.Sort();
				System.Console.Out.WriteLine("UTF16 order:");
				foreach (string s in terms)
				{
					System.Console.Out.WriteLine("  " + UnicodeUtil.ToHexString(s));
				}
			}
			reader = writer.GetReader();
			searcher1 = NewSearcher(reader);
			searcher2 = NewSearcher(reader);
			writer.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			reader.Close();
			dir.Close();
			base.TearDown();
		}

		/// <summary>a stupid regexp query that just blasts thru the terms</summary>
		private class DumbRegexpQuery : MultiTermQuery
		{
			private readonly Lucene.Net.Util.Automaton.Automaton automaton;

			internal DumbRegexpQuery(TestRegexpRandom2 _enclosing, Term term, int flags) : base
				(term.Field())
			{
				this._enclosing = _enclosing;
				RegExp re = new RegExp(term.Text(), flags);
				this.automaton = re.ToAutomaton();
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected override TermsEnum GetTermsEnum(Terms terms, AttributeSource atts)
			{
				return new TestRegexpRandom2.DumbRegexpQuery.SimpleAutomatonTermsEnum(this, terms
					.Iterator(null));
			}

			private class SimpleAutomatonTermsEnum : FilteredTermsEnum
			{
				internal CharacterRunAutomaton runAutomaton = new CharacterRunAutomaton(this._enclosing
					.automaton);

				internal CharsRef utf16 = new CharsRef(10);

				public SimpleAutomatonTermsEnum(DumbRegexpQuery _enclosing, TermsEnum tenum) : base
					(tenum)
				{
					this._enclosing = _enclosing;
					this.SetInitialSeekTerm(new BytesRef(string.Empty));
				}

				/// <exception cref="System.IO.IOException"></exception>
				protected override FilteredTermsEnum.AcceptStatus Accept(BytesRef term)
				{
					UnicodeUtil.UTF8toUTF16(term.bytes, term.offset, term.length, this.utf16);
					return this.runAutomaton.Run(this.utf16.chars, 0, this.utf16.length) ? FilteredTermsEnum.AcceptStatus
						.YES : FilteredTermsEnum.AcceptStatus.NO;
				}

				private readonly DumbRegexpQuery _enclosing;
			}

			public override string ToString(string field)
			{
				return field.ToString() + this.automaton.ToString();
			}

			private readonly TestRegexpRandom2 _enclosing;
		}

		/// <summary>test a bunch of random regular expressions</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestRegexps()
		{
			// we generate aweful regexps: good for testing.
			// but for preflex codec, the test can be very slow, so use less iterations.
			int num = Codec.GetDefault().GetName().Equals("Lucene3x") ? 100 * RANDOM_MULTIPLIER
				 : AtLeast(1000);
			for (int i = 0; i < num; i++)
			{
				string reg = AutomatonTestUtil.RandomRegexp(Random());
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: regexp=" + reg);
				}
				AssertSame(reg);
			}
		}

		/// <summary>
		/// check that the # of hits is the same as from a very
		/// simple regexpquery implementation.
		/// </summary>
		/// <remarks>
		/// check that the # of hits is the same as from a very
		/// simple regexpquery implementation.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual void AssertSame(string regexp)
		{
			RegexpQuery smart = new RegexpQuery(new Term(fieldName, regexp), RegExp.NONE);
			TestRegexpRandom2.DumbRegexpQuery dumb = new TestRegexpRandom2.DumbRegexpQuery(this
				, new Term(fieldName, regexp), RegExp.NONE);
			TopDocs smartDocs = searcher1.Search(smart, 25);
			TopDocs dumbDocs = searcher2.Search(dumb, 25);
			CheckHits.CheckEqual(smart, smartDocs.scoreDocs, dumbDocs.scoreDocs);
		}
	}
}
