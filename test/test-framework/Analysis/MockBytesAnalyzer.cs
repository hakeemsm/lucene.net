using System.IO;
using Lucene.Net.Analysis;

namespace Lucene.Net.TestFramework.Analysis
{
	/// <summary>Analyzer for testing that encodes terms as UTF-16 bytes.</summary>
	/// <remarks>Analyzer for testing that encodes terms as UTF-16 bytes.</remarks>
	public class MockBytesAnalyzer : Analyzer
	{
		private readonly MockBytesAttributeFactory factory = new MockBytesAttributeFactory();

	    public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
		{
			Tokenizer t = new MockTokenizer(factory, reader, MockTokenizer.KEYWORD, false, MockTokenizer
				.DEFAULT_MAX_TOKEN_LENGTH);
			return new TokenStreamComponents(t);
		}
	}
}
