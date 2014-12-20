using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;

namespace Lucene.Net.TestFramework.Analysis
{
	/// <summary>TokenStream from a canned list of Tokens.</summary>
	/// <remarks>TokenStream from a canned list of Tokens.</remarks>
	public sealed class CannedTokenStream : TokenStream
	{
		private readonly Token[] tokens;

		private int upto = 0;

		private readonly CharTermAttribute termAtt = AddAttribute<CharTermAttribute>();

		private readonly PositionIncrementAttribute posIncrAtt = AddAttribute<PositionIncrementAttribute
			>();

		private readonly PositionLengthAttribute posLengthAtt = AddAttribute<PositionLengthAttribute
			>();

		private readonly OffsetAttribute offsetAtt = AddAttribute<OffsetAttribute>();

		private readonly PayloadAttribute payloadAtt = AddAttribute<PayloadAttribute>();

		private readonly int finalOffset;

		private readonly int finalPosInc;

		public CannedTokenStream(params Token[] tokens)
		{
			this.tokens = tokens;
			finalOffset = 0;
			finalPosInc = 0;
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
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void End()
		{
			base.End();
			posIncrAtt.SetPositionIncrement(finalPosInc);
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
				posIncrAtt.SetPositionIncrement(token.GetPositionIncrement());
				posLengthAtt.SetPositionLength(token.GetPositionLength());
				offsetAtt.SetOffset(token.StartOffset(), token.EndOffset());
				payloadAtt.SetPayload(token.GetPayload());
				return true;
			}
			else
			{
				return false;
			}
		}
	}
}
