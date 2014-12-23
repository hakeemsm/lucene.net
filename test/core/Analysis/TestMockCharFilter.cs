/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.IO;
using Lucene.Net.Analysis;
using Sharpen;

namespace Lucene.Net.Analysis
{
	public class TestMockCharFilter : BaseTokenStreamTestCase
	{
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void Test()
		{
			Analyzer analyzer = new _Analyzer_26();
			AssertAnalyzesTo(analyzer, "ab", new string[] { "aab" }, new int[] { 0 }, new int
				[] { 2 });
			AssertAnalyzesTo(analyzer, "aba", new string[] { "aabaa" }, new int[] { 0 }, new 
				int[] { 3 });
			AssertAnalyzesTo(analyzer, "abcdefga", new string[] { "aabcdefgaa" }, new int[] { 
				0 }, new int[] { 8 });
		}

		private sealed class _Analyzer_26 : Analyzer
		{
			public _Analyzer_26()
			{
			}

			protected override Analyzer.TokenStreamComponents CreateComponents(string fieldName
				, StreamReader reader)
			{
				Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
				return new Analyzer.TokenStreamComponents(tokenizer, tokenizer);
			}

			protected override StreamReader InitReader(string fieldName, StreamReader reader)
			{
				return new MockCharFilter(reader, 7);
			}
		}
	}
}
