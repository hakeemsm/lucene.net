using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.TestFramework.Analysis;
using NUnit.Framework;

namespace Lucene.Net.Test.Analysis
{
    [TestFixture]
	public class TestMockCharFilter : BaseTokenStreamTestCase
	{
		[Test]
		public virtual void TestAnalyzer()
		{
			Analyzer analyzer = new AnonymousAnalyzer();
			AssertAnalyzesTo(analyzer, "ab", new[] { "aab" }, new[] { 0 }, new[] { 2 });
			AssertAnalyzesTo(analyzer, "aba", new[] { "aabaa" }, new[] { 0 }, new[] { 3 });
			AssertAnalyzesTo(analyzer, "abcdefga", new[] { "aabcdefgaa" }, new[] { 0 }, new[] { 8 });
		}

		private sealed class AnonymousAnalyzer : Analyzer
		{
		    public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
			{
				Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
				return new Analyzer.TokenStreamComponents(tokenizer, tokenizer);
			}

		    public override TextReader InitReader(string fieldName, TextReader reader)
			{
				return new MockCharFilter((StreamReader) reader, 7);
			}
		}
	}
}
