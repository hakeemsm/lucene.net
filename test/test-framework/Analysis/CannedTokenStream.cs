using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;

namespace Lucene.Net.TestFramework.Analysis
{
	/// <summary>TokenStream from a canned list of Tokens.</summary>
	/// <remarks>TokenStream from a canned list of Tokens.</remarks>
	public sealed class CannedTokenStream : TokenStream
	{
		private readonly Token[] tokens;

		private int upto;

	    private CharTermAttribute termAtt;

	    private PositionIncrementAttribute posIncrAtt;

	    private PositionLengthAttribute posLengthAtt;

	    private OffsetAttribute offsetAtt;

	    private PayloadAttribute payloadAtt;

		private readonly int finalOffset;

		private readonly int finalPosInc;

		public CannedTokenStream(params Token[] tokens)
		{
			this.tokens = tokens;
			finalOffset = 0;
			finalPosInc = 0;
		    InitAttributeObjects();
		}

		/// <summary>
		/// If you want trailing holes, pass a non-zero
		/// finalPosInc.
		/// </summary>
		/// <remarks>
		/// If you want trailing holes, pass a non-zero
		/// finalPosInc.
		/// </remarks>
		public CannedTokenStream(int finalPosInc, int finalOffset, params Token[] tokens)
		{
			this.tokens = tokens;
			this.finalOffset = finalOffset;
			this.finalPosInc = finalPosInc;
		    InitAttributeObjects();
		}

	    private void InitAttributeObjects()
	    {
            termAtt = AddAttribute<CharTermAttribute>();
            posIncrAtt = AddAttribute<PositionIncrementAttribute>();
            posLengthAtt = AddAttribute<PositionLengthAttribute>();
            offsetAtt = AddAttribute<OffsetAttribute>();
            payloadAtt = AddAttribute<PayloadAttribute>();
	    }

	    /// <exception cref="System.IO.IOException"></exception>
		public override void End()
		{
			base.End();
			posIncrAtt.PositionIncrement = finalPosInc;
			offsetAtt.SetOffset(finalOffset, finalOffset);
		}

		public override bool IncrementToken()
		{
		    if (upto < tokens.Length)
			{
				Token token = tokens[upto++];
				// TODO: can we just capture/restoreState so
				// we get all attrs...?
				ClearAttributes();
				termAtt.SetEmpty();
				termAtt.Append(token.ToString());
				posIncrAtt.PositionIncrement = token.PositionIncrement;
				posLengthAtt.PositionLength = token.PositionLength;
				offsetAtt.SetOffset(token.StartOffset, token.EndOffset);
				payloadAtt.Payload = token.Payload;
				return true;
			}
		    return false;
		}
	}
}
