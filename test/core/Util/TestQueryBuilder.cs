/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.IO;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Test.Analysis.Tokenattributes;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using Sharpen;

namespace Lucene.Net.Util
{
	public class TestQueryBuilder : LuceneTestCase
	{
		public virtual void TestTerm()
		{
			TermQuery expected = new TermQuery(new Term("field", "test"));
			QueryBuilder builder = new QueryBuilder(new MockAnalyzer(Random()));
			AreEqual(expected, builder.CreateBooleanQuery("field", "test"
				));
		}

		public virtual void TestBoolean()
		{
			BooleanQuery expected = new BooleanQuery();
			expected.Add(new TermQuery(new Term("field", "foo")), BooleanClause.Occur.SHOULD);
			expected.Add(new TermQuery(new Term("field", "bar")), BooleanClause.Occur.SHOULD);
			QueryBuilder builder = new QueryBuilder(new MockAnalyzer(Random()));
			AreEqual(expected, builder.CreateBooleanQuery("field", "foo bar"
				));
		}

		public virtual void TestBooleanMust()
		{
			BooleanQuery expected = new BooleanQuery();
			expected.Add(new TermQuery(new Term("field", "foo")), BooleanClause.Occur.MUST);
			expected.Add(new TermQuery(new Term("field", "bar")), BooleanClause.Occur.MUST);
			QueryBuilder builder = new QueryBuilder(new MockAnalyzer(Random()));
			AreEqual(expected, builder.CreateBooleanQuery("field", "foo bar"
				, BooleanClause.Occur.MUST));
		}

		public virtual void TestMinShouldMatchNone()
		{
			QueryBuilder builder = new QueryBuilder(new MockAnalyzer(Random()));
			AreEqual(builder.CreateBooleanQuery("field", "one two three four"
				), builder.CreateMinShouldMatchQuery("field", "one two three four", 0f));
		}

		public virtual void TestMinShouldMatchAll()
		{
			QueryBuilder builder = new QueryBuilder(new MockAnalyzer(Random()));
			AreEqual(builder.CreateBooleanQuery("field", "one two three four"
				, BooleanClause.Occur.MUST), builder.CreateMinShouldMatchQuery("field", "one two three four"
				, 1f));
		}

		public virtual void TestMinShouldMatch()
		{
			BooleanQuery expected = new BooleanQuery();
			expected.Add(new TermQuery(new Term("field", "one")), BooleanClause.Occur.SHOULD);
			expected.Add(new TermQuery(new Term("field", "two")), BooleanClause.Occur.SHOULD);
			expected.Add(new TermQuery(new Term("field", "three")), BooleanClause.Occur.SHOULD
				);
			expected.Add(new TermQuery(new Term("field", "four")), BooleanClause.Occur.SHOULD
				);
			expected.SetMinimumNumberShouldMatch(0);
			QueryBuilder builder = new QueryBuilder(new MockAnalyzer(Random()));
			AreEqual(expected, builder.CreateMinShouldMatchQuery("field"
				, "one two three four", 0.1f));
			AreEqual(expected, builder.CreateMinShouldMatchQuery("field"
				, "one two three four", 0.24f));
			expected.SetMinimumNumberShouldMatch(1);
			AreEqual(expected, builder.CreateMinShouldMatchQuery("field"
				, "one two three four", 0.25f));
			AreEqual(expected, builder.CreateMinShouldMatchQuery("field"
				, "one two three four", 0.49f));
			expected.SetMinimumNumberShouldMatch(2);
			AreEqual(expected, builder.CreateMinShouldMatchQuery("field"
				, "one two three four", 0.5f));
			AreEqual(expected, builder.CreateMinShouldMatchQuery("field"
				, "one two three four", 0.74f));
			expected.SetMinimumNumberShouldMatch(3);
			AreEqual(expected, builder.CreateMinShouldMatchQuery("field"
				, "one two three four", 0.75f));
			AreEqual(expected, builder.CreateMinShouldMatchQuery("field"
				, "one two three four", 0.99f));
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestPhraseQueryPositionIncrements()
		{
			PhraseQuery expected = new PhraseQuery();
			expected.Add(new Term("field", "1"));
			expected.Add(new Term("field", "2"), 2);
			CharacterRunAutomaton stopList = new CharacterRunAutomaton(new RegExp("[sS][tT][oO][pP]"
				).ToAutomaton());
			Analyzer analyzer = new MockAnalyzer(Random(), MockTokenizer.WHITESPACE, false, stopList
				);
			QueryBuilder builder = new QueryBuilder(analyzer);
			AreEqual(expected, builder.CreatePhraseQuery("field", "1 stop 2"
				));
		}

		public virtual void TestEmpty()
		{
			QueryBuilder builder = new QueryBuilder(new MockAnalyzer(Random()));
			IsNull(builder.CreateBooleanQuery("field", string.Empty));
		}

		/// <summary>adds synonym of "dog" for "dogs".</summary>
		/// <remarks>adds synonym of "dog" for "dogs".</remarks>
		internal class MockSynonymAnalyzer : Analyzer
		{
			protected override Analyzer.TokenStreamComponents CreateComponents(string fieldName
				, StreamReader reader)
			{
				MockTokenizer tokenizer = new MockTokenizer(reader);
				return new Analyzer.TokenStreamComponents(tokenizer, new TestQueryBuilder.MockSynonymFilter
					(tokenizer));
			}
		}

		/// <summary>adds synonym of "dog" for "dogs".</summary>
		/// <remarks>adds synonym of "dog" for "dogs".</remarks>
		protected internal class MockSynonymFilter : TokenFilter
		{
			internal CharTermAttribute termAtt = AddAttribute<CharTermAttribute>();

			internal PositionIncrementAttribute posIncAtt = AddAttribute<PositionIncrementAttribute
				>();

			internal bool addSynonym = false;

			protected MockSynonymFilter(TokenStream input) : base(input)
			{
			}

			/// <exception cref="System.IO.IOException"></exception>
			public sealed override bool IncrementToken()
			{
				if (addSynonym)
				{
					// inject our synonym
					ClearAttributes();
					termAtt.SetEmpty().Append("dog");
					posIncAtt.PositionIncrement = (0);
					addSynonym = false;
					return true;
				}
				if (input.IncrementToken())
				{
					addSynonym = termAtt.ToString().Equals("dogs");
					return true;
				}
				else
				{
					return false;
				}
			}
		}

		/// <summary>simple synonyms test</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestSynonyms()
		{
			BooleanQuery expected = new BooleanQuery(true);
			expected.Add(new TermQuery(new Term("field", "dogs")), BooleanClause.Occur.SHOULD
				);
			expected.Add(new TermQuery(new Term("field", "dog")), BooleanClause.Occur.SHOULD);
			QueryBuilder builder = new QueryBuilder(new TestQueryBuilder.MockSynonymAnalyzer(
				));
			AreEqual(expected, builder.CreateBooleanQuery("field", "dogs"
				));
			AreEqual(expected, builder.CreatePhraseQuery("field", "dogs"
				));
			AreEqual(expected, builder.CreateBooleanQuery("field", "dogs"
				, BooleanClause.Occur.MUST));
			AreEqual(expected, builder.CreatePhraseQuery("field", "dogs"
				));
		}

		/// <summary>forms multiphrase query</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestSynonymsPhrase()
		{
			MultiPhraseQuery expected = new MultiPhraseQuery();
			expected.Add(new Term("field", "old"));
			expected.Add(new Term[] { new Term("field", "dogs"), new Term("field", "dog") });
			QueryBuilder builder = new QueryBuilder(new TestQueryBuilder.MockSynonymAnalyzer(
				));
			AreEqual(expected, builder.CreatePhraseQuery("field", "old dogs"
				));
		}

		protected internal class SimpleCJKTokenizer : Tokenizer
		{
			private CharTermAttribute termAtt = AddAttribute<CharTermAttribute>();

			protected SimpleCJKTokenizer(StreamReader input) : base(input)
			{
			}

			/// <exception cref="System.IO.IOException"></exception>
			public sealed override bool IncrementToken()
			{
				int ch = input.Read();
				if (ch < 0)
				{
					return false;
				}
				ClearAttributes();
				termAtt.SetEmpty().Append((char)ch);
				return true;
			}
		}

		private class SimpleCJKAnalyzer : Analyzer
		{
			protected override Analyzer.TokenStreamComponents CreateComponents(string fieldName
				, StreamReader reader)
			{
				return new Analyzer.TokenStreamComponents(new TestQueryBuilder.SimpleCJKTokenizer
					(reader));
			}

			internal SimpleCJKAnalyzer(TestQueryBuilder _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly TestQueryBuilder _enclosing;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestCJKTerm()
		{
			// individual CJK chars as terms
			TestQueryBuilder.SimpleCJKAnalyzer analyzer = new TestQueryBuilder.SimpleCJKAnalyzer
				(this);
			BooleanQuery expected = new BooleanQuery();
			expected.Add(new TermQuery(new Term("field", "ä¸­")), BooleanClause.Occur.SHOULD);
			expected.Add(new TermQuery(new Term("field", "å›½")), BooleanClause.Occur.SHOULD);
			QueryBuilder builder = new QueryBuilder(analyzer);
			AreEqual(expected, builder.CreateBooleanQuery("field", "ä¸­å›½"
				));
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestCJKPhrase()
		{
			// individual CJK chars as terms
			TestQueryBuilder.SimpleCJKAnalyzer analyzer = new TestQueryBuilder.SimpleCJKAnalyzer
				(this);
			PhraseQuery expected = new PhraseQuery();
			expected.Add(new Term("field", "ä¸­"));
			expected.Add(new Term("field", "å›½"));
			QueryBuilder builder = new QueryBuilder(analyzer);
			AreEqual(expected, builder.CreatePhraseQuery("field", "ä¸­å›½"
				));
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestCJKSloppyPhrase()
		{
			// individual CJK chars as terms
			TestQueryBuilder.SimpleCJKAnalyzer analyzer = new TestQueryBuilder.SimpleCJKAnalyzer
				(this);
			PhraseQuery expected = new PhraseQuery();
			expected.SetSlop(3);
			expected.Add(new Term("field", "ä¸­"));
			expected.Add(new Term("field", "å›½"));
			QueryBuilder builder = new QueryBuilder(analyzer);
			AreEqual(expected, builder.CreatePhraseQuery("field", "ä¸­å›½"
				, 3));
		}

		/// <summary>adds synonym of "åœ‹" for "å›½".</summary>
		/// <remarks>adds synonym of "åœ‹" for "å›½".</remarks>
		protected internal class MockCJKSynonymFilter : TokenFilter
		{
			internal CharTermAttribute termAtt = AddAttribute<CharTermAttribute>();

			internal PositionIncrementAttribute posIncAtt = AddAttribute<PositionIncrementAttribute
				>();

			internal bool addSynonym = false;

			protected MockCJKSynonymFilter(TokenStream input) : base(input)
			{
			}

			/// <exception cref="System.IO.IOException"></exception>
			public sealed override bool IncrementToken()
			{
				if (addSynonym)
				{
					// inject our synonym
					ClearAttributes();
					termAtt.SetEmpty().Append("åœ‹");
					posIncAtt.PositionIncrement = (0);
					addSynonym = false;
					return true;
				}
				if (input.IncrementToken())
				{
					addSynonym = termAtt.ToString().Equals("å›½");
					return true;
				}
				else
				{
					return false;
				}
			}
		}

		internal class MockCJKSynonymAnalyzer : Analyzer
		{
			protected override Analyzer.TokenStreamComponents CreateComponents(string fieldName
				, StreamReader reader)
			{
				Tokenizer tokenizer = new TestQueryBuilder.SimpleCJKTokenizer(reader);
				return new Analyzer.TokenStreamComponents(tokenizer, new TestQueryBuilder.MockCJKSynonymFilter
					(tokenizer));
			}
		}

		/// <summary>simple CJK synonym test</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestCJKSynonym()
		{
			BooleanQuery expected = new BooleanQuery(true);
			expected.Add(new TermQuery(new Term("field", "å›½")), BooleanClause.Occur.SHOULD);
			expected.Add(new TermQuery(new Term("field", "åœ‹")), BooleanClause.Occur.SHOULD);
			QueryBuilder builder = new QueryBuilder(new TestQueryBuilder.MockCJKSynonymAnalyzer
				());
			AreEqual(expected, builder.CreateBooleanQuery("field", "å›½"
				));
			AreEqual(expected, builder.CreatePhraseQuery("field", "å›½"
				));
			AreEqual(expected, builder.CreateBooleanQuery("field", "å›½"
				, BooleanClause.Occur.MUST));
		}

		/// <summary>synonyms with default OR operator</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestCJKSynonymsOR()
		{
			BooleanQuery expected = new BooleanQuery();
			expected.Add(new TermQuery(new Term("field", "ä¸­")), BooleanClause.Occur.SHOULD);
			BooleanQuery inner = new BooleanQuery(true);
			inner.Add(new TermQuery(new Term("field", "å›½")), BooleanClause.Occur.SHOULD);
			inner.Add(new TermQuery(new Term("field", "åœ‹")), BooleanClause.Occur.SHOULD);
			expected.Add(inner, BooleanClause.Occur.SHOULD);
			QueryBuilder builder = new QueryBuilder(new TestQueryBuilder.MockCJKSynonymAnalyzer
				());
			AreEqual(expected, builder.CreateBooleanQuery("field", "ä¸­å›½"
				));
		}

		/// <summary>more complex synonyms with default OR operator</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestCJKSynonymsOR2()
		{
			BooleanQuery expected = new BooleanQuery();
			expected.Add(new TermQuery(new Term("field", "ä¸­")), BooleanClause.Occur.SHOULD);
			BooleanQuery inner = new BooleanQuery(true);
			inner.Add(new TermQuery(new Term("field", "å›½")), BooleanClause.Occur.SHOULD);
			inner.Add(new TermQuery(new Term("field", "åœ‹")), BooleanClause.Occur.SHOULD);
			expected.Add(inner, BooleanClause.Occur.SHOULD);
			BooleanQuery inner2 = new BooleanQuery(true);
			inner2.Add(new TermQuery(new Term("field", "å›½")), BooleanClause.Occur.SHOULD);
			inner2.Add(new TermQuery(new Term("field", "åœ‹")), BooleanClause.Occur.SHOULD);
			expected.Add(inner2, BooleanClause.Occur.SHOULD);
			QueryBuilder builder = new QueryBuilder(new TestQueryBuilder.MockCJKSynonymAnalyzer
				());
			AreEqual(expected, builder.CreateBooleanQuery("field", "ä¸­å›½å›½"
				));
		}

		/// <summary>synonyms with default AND operator</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestCJKSynonymsAND()
		{
			BooleanQuery expected = new BooleanQuery();
			expected.Add(new TermQuery(new Term("field", "ä¸­")), BooleanClause.Occur.MUST);
			BooleanQuery inner = new BooleanQuery(true);
			inner.Add(new TermQuery(new Term("field", "å›½")), BooleanClause.Occur.SHOULD);
			inner.Add(new TermQuery(new Term("field", "åœ‹")), BooleanClause.Occur.SHOULD);
			expected.Add(inner, BooleanClause.Occur.MUST);
			QueryBuilder builder = new QueryBuilder(new TestQueryBuilder.MockCJKSynonymAnalyzer
				());
			AreEqual(expected, builder.CreateBooleanQuery("field", "ä¸­å›½"
				, BooleanClause.Occur.MUST));
		}

		/// <summary>more complex synonyms with default AND operator</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestCJKSynonymsAND2()
		{
			BooleanQuery expected = new BooleanQuery();
			expected.Add(new TermQuery(new Term("field", "ä¸­")), BooleanClause.Occur.MUST);
			BooleanQuery inner = new BooleanQuery(true);
			inner.Add(new TermQuery(new Term("field", "å›½")), BooleanClause.Occur.SHOULD);
			inner.Add(new TermQuery(new Term("field", "åœ‹")), BooleanClause.Occur.SHOULD);
			expected.Add(inner, BooleanClause.Occur.MUST);
			BooleanQuery inner2 = new BooleanQuery(true);
			inner2.Add(new TermQuery(new Term("field", "å›½")), BooleanClause.Occur.SHOULD);
			inner2.Add(new TermQuery(new Term("field", "åœ‹")), BooleanClause.Occur.SHOULD);
			expected.Add(inner2, BooleanClause.Occur.MUST);
			QueryBuilder builder = new QueryBuilder(new TestQueryBuilder.MockCJKSynonymAnalyzer
				());
			AreEqual(expected, builder.CreateBooleanQuery("field", "ä¸­å›½å›½"
				, BooleanClause.Occur.MUST));
		}

		/// <summary>forms multiphrase query</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestCJKSynonymsPhrase()
		{
			MultiPhraseQuery expected = new MultiPhraseQuery();
			expected.Add(new Term("field", "ä¸­"));
			expected.Add(new Term[] { new Term("field", "å›½"), new Term("field", "åœ‹") });
			QueryBuilder builder = new QueryBuilder(new TestQueryBuilder.MockCJKSynonymAnalyzer
				());
			AreEqual(expected, builder.CreatePhraseQuery("field", "ä¸­å›½"
				));
			expected.SetSlop(3);
			AreEqual(expected, builder.CreatePhraseQuery("field", "ä¸­å›½"
				, 3));
		}
	}
}
