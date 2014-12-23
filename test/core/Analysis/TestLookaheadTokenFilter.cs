/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Analysis
{
	public class TestLookaheadTokenFilter : BaseTokenStreamTestCase
	{
		/// <exception cref="System.Exception"></exception>
		public virtual void TestRandomStrings()
		{
			Analyzer a = new _Analyzer_27();
			CheckRandomData(Random(), a, 200 * RANDOM_MULTIPLIER, 8192);
		}

		private sealed class _Analyzer_27 : Analyzer
		{
			public _Analyzer_27()
			{
			}

			protected override Analyzer.TokenStreamComponents CreateComponents(string fieldName
				, StreamReader reader)
			{
				Random random = LuceneTestCase.Random();
				Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, random.
					NextBoolean());
				TokenStream output = new MockRandomLookaheadTokenFilter(random, tokenizer);
				return new Analyzer.TokenStreamComponents(tokenizer, output);
			}
		}

		private class NeverPeeksLookaheadTokenFilter : LookaheadTokenFilter<LookaheadTokenFilter.Position
			>
		{
			protected NeverPeeksLookaheadTokenFilter(TokenStream input) : base(input)
			{
			}

			protected override LookaheadTokenFilter.Position NewPosition()
			{
				return new LookaheadTokenFilter.Position();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override bool IncrementToken()
			{
				return NextToken();
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestNeverCallingPeek()
		{
			Analyzer a = new _Analyzer_56();
			CheckRandomData(Random(), a, 200 * RANDOM_MULTIPLIER, 8192);
		}

		private sealed class _Analyzer_56 : Analyzer
		{
			public _Analyzer_56()
			{
			}

			protected override Analyzer.TokenStreamComponents CreateComponents(string fieldName
				, StreamReader reader)
			{
				Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, LuceneTestCase
					.Random().NextBoolean());
				TokenStream output = new TestLookaheadTokenFilter.NeverPeeksLookaheadTokenFilter(
					tokenizer);
				return new Analyzer.TokenStreamComponents(tokenizer, output);
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestMissedFirstToken()
		{
			Analyzer analyzer = new _Analyzer_68();
			AssertAnalyzesTo(analyzer, "Only he who is running knows .", new string[] { "Only"
				, "Only-huh?", "he", "he-huh?", "who", "who-huh?", "is", "is-huh?", "running", "running-huh?"
				, "knows", "knows-huh?", ".", ".-huh?" });
		}

		private sealed class _Analyzer_68 : Analyzer
		{
			public _Analyzer_68()
			{
			}

			protected override Analyzer.TokenStreamComponents CreateComponents(string fieldName
				, StreamReader reader)
			{
				Tokenizer source = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
				TrivialLookaheadFilter filter = new TrivialLookaheadFilter(source);
				return new Analyzer.TokenStreamComponents(source, filter);
			}
		}
	}
}
