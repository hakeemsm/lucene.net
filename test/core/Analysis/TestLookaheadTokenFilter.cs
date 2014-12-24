using System;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.TestFramework.Analysis;
using NUnit.Framework;

namespace Lucene.Net.Test.Analysis
{
    [TestFixture]
	public class TestLookaheadTokenFilter : BaseTokenStreamTestCase
	{
		[Test]
		public virtual void TestRandomStrings()
		{
			Analyzer a = new AnonymousAnalyzer();
			CheckRandomData(Random(), a, 200 * RANDOM_MULTIPLIER, 8192);
		}

		private sealed class AnonymousAnalyzer : Analyzer
		{
		    public override Analyzer.TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
			{
				Random random = Random();
				Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, random.
					NextBoolean());
				TokenStream output = new MockRandomLookaheadTokenFilter(random, tokenizer);
				return new TokenStreamComponents(tokenizer, output);
			}
		}

		private class NeverPeeksLookaheadTokenFilter : LookaheadTokenFilter<Position>
		{
		    public NeverPeeksLookaheadTokenFilter(TokenStream input) : base(input)
			{
			}

			protected override Position NewPosition()
			{
				return new Position();
			}

			
			public override bool IncrementToken()
			{
				return NextToken();
			}
		}

		[Test]
		public virtual void TestNeverCallingPeek()
		{
			Analyzer a = new AnonymousAnalyzer2();
			CheckRandomData(Random(), a, 200 * RANDOM_MULTIPLIER, 8192);
		}

		private sealed class AnonymousAnalyzer2 : Analyzer
		{
		    public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
			{
				Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, Random().NextBoolean());
				TokenStream output = new NeverPeeksLookaheadTokenFilter(tokenizer);
				return new Analyzer.TokenStreamComponents(tokenizer, output);
			}
		}

		[Test]
		public virtual void TestMissedFirstToken()
		{
			Analyzer analyzer = new AnonymousAnalyzer3();
			AssertAnalyzesTo(analyzer, "Only he who is running knows .", new string[] { "Only"
				, "Only-huh?", "he", "he-huh?", "who", "who-huh?", "is", "is-huh?", "running", "running-huh?"
				, "knows", "knows-huh?", ".", ".-huh?" });
		}

		private sealed class AnonymousAnalyzer3 : Analyzer
		{
		    public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
			{
				Tokenizer source = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
				TrivialLookaheadFilter filter = new TrivialLookaheadFilter(source);
				return new Analyzer.TokenStreamComponents(source, filter);
			}
		}
	}
}
