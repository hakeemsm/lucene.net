using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Util;

namespace Lucene.Net.TestFramework.Analysis
{
	/// <summary>
	/// Wraps a whitespace tokenizer with a filter that sets
	/// the first token, and odd tokens to posinc=1, and all others
	/// to 0, encoding the position as pos: XXX in the payload.
	/// </summary>
	/// <remarks>
	/// Wraps a whitespace tokenizer with a filter that sets
	/// the first token, and odd tokens to posinc=1, and all others
	/// to 0, encoding the position as pos: XXX in the payload.
	/// </remarks>
	public sealed class MockPayloadAnalyzer : Analyzer
	{
	    public override Analyzer.TokenStreamComponents CreateComponents(string fieldName
			, TextReader reader)
		{
			Tokenizer result = new MockTokenizer(reader, MockTokenizer.WHITESPACE, true);
			return new Analyzer.TokenStreamComponents(result, new MockPayloadFilter(result, fieldName
				));
		}
	}

	internal sealed class MockPayloadFilter : TokenFilter
	{
		internal string fieldName;

		internal int pos;

		internal int i;

		internal readonly PositionIncrementAttribute posIncrAttr;

		internal readonly PayloadAttribute payloadAttr;

		internal readonly CharTermAttribute termAttr;

		public MockPayloadFilter(TokenStream input, string fieldName) : base(input)
		{
			this.fieldName = fieldName;
			pos = 0;
			i = 0;
			posIncrAttr = input.AddAttribute<PositionIncrementAttribute>();
			payloadAttr = input.AddAttribute<PayloadAttribute>();
			termAttr = input.AddAttribute<CharTermAttribute>();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override bool IncrementToken()
		{
			if (input.IncrementToken())
			{
				payloadAttr.SetPayload(new BytesRef(Sharpen.Runtime.GetBytesForString(("pos: " + 
					pos), StandardCharsets.UTF_8)));
				int posIncr;
				if (pos == 0 || i % 2 == 1)
				{
					posIncr = 1;
				}
				else
				{
					posIncr = 0;
				}
				posIncrAttr.SetPositionIncrement(posIncr);
				pos += posIncr;
				i++;
				return true;
			}
			else
			{
				return false;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Reset()
		{
			base.Reset();
			i = 0;
			pos = 0;
		}
	}
}
