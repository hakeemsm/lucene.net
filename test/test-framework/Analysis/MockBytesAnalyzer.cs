/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.IO;
using Lucene.Net.TestFramework.Analysis;
using Sharpen;

namespace Lucene.Net.TestFramework.Analysis
{
	/// <summary>Analyzer for testing that encodes terms as UTF-16 bytes.</summary>
	/// <remarks>Analyzer for testing that encodes terms as UTF-16 bytes.</remarks>
	public class MockBytesAnalyzer : Analyzer
	{
		private readonly MockBytesAttributeFactory factory = new MockBytesAttributeFactory
			();

		protected override Analyzer.TokenStreamComponents CreateComponents(string fieldName
			, StreamReader reader)
		{
			Tokenizer t = new MockTokenizer(factory, reader, MockTokenizer.KEYWORD, false, MockTokenizer
				.DEFAULT_MAX_TOKEN_LENGTH);
			return new Analyzer.TokenStreamComponents(t);
		}
	}
}
