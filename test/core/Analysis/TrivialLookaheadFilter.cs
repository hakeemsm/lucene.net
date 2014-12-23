/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Test.Analysis.TokenAttributes;
using Sharpen;

namespace Lucene.Net.Analysis
{
	/// <summary>Simple example of a filter that seems to show some problems with LookaheadTokenFilter.
	/// 	</summary>
	/// <remarks>Simple example of a filter that seems to show some problems with LookaheadTokenFilter.
	/// 	</remarks>
	public sealed class TrivialLookaheadFilter : LookaheadTokenFilter<TestPosition>
	{
		private readonly CharTermAttribute termAtt = AddAttribute<CharTermAttribute>();

		private readonly PositionIncrementAttribute posIncAtt = AddAttribute<PositionIncrementAttribute
			>();

		private readonly OffsetAttribute offsetAtt = AddAttribute<OffsetAttribute>();

		private int insertUpto;

		protected TrivialLookaheadFilter(TokenStream input) : base(input)
		{
		}

		protected override TestPosition NewPosition()
		{
			return new TestPosition();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override bool IncrementToken()
		{
			// At the outset, getMaxPos is -1. So we'll peek. When we reach the end of the sentence and go to the
			// first token of the next sentence, maxPos will be the prev sentence's end token, and we'll go again.
			if (positions.GetMaxPos() < outputPos)
			{
				PeekSentence();
			}
			return NextToken();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Reset()
		{
			base.Reset();
			insertUpto = -1;
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected override void AfterPosition()
		{
			if (insertUpto < outputPos)
			{
				InsertToken();
				// replace term with 'improved' term.
				ClearAttributes();
				termAtt.SetEmpty();
				posIncAtt.SetPositionIncrement(0);
				termAtt.Append(positions.Get(outputPos).GetFact());
				offsetAtt.SetOffset(positions.Get(outputPos).startOffset, positions.Get(outputPos
					 + 1).endOffset);
				insertUpto = outputPos;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void PeekSentence()
		{
			IList<string> facts = new AList<string>();
			bool haveSentence = false;
			do
			{
				if (PeekToken())
				{
					string term = new string(termAtt.Buffer(), 0, termAtt.Length);
					facts.AddItem(term + "-huh?");
					if (".".Equals(term))
					{
						haveSentence = true;
					}
				}
				else
				{
					haveSentence = true;
				}
			}
			while (!haveSentence);
			// attach the (now disambiguated) analyzed tokens to the positions.
			for (int x = 0; x < facts.Count; x++)
			{
				// sentenceTokens is just relative to sentence, positions is absolute.
				positions.Get(outputPos + x).SetFact(facts[x]);
			}
		}
	}
}
