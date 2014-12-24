using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.TestFramework.Analysis;

namespace Lucene.Net.Test.Analysis
{
    /// <summary>
    /// Simple example of a filter that seems to show some problems with LookaheadTokenFilter.
    /// </summary>

    public sealed class TrivialLookaheadFilter : LookaheadTokenFilter<TestPosition>
    {
        private readonly TokenStream _input;
        private readonly CharTermAttribute termAtt;

        private readonly PositionIncrementAttribute posIncAtt;

        private readonly OffsetAttribute offsetAtt;

        private int insertUpto;

        protected internal TrivialLookaheadFilter(TokenStream input)
            : base(input)
        {
            _input = input;
            termAtt = AddAttribute<CharTermAttribute>();
            posIncAtt = AddAttribute<PositionIncrementAttribute>();
            offsetAtt = AddAttribute<OffsetAttribute>();
        }

        protected override TestPosition NewPosition()
        {
            return new TestPosition(_input);
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
                posIncAtt.PositionIncrement = 0;
                termAtt.Append(positions.Get(outputPos).Fact);
                offsetAtt.SetOffset(positions.Get(outputPos).startOffset, positions.Get(outputPos+ 1).endOffset);
                insertUpto = outputPos;
            }
        }

        /// <exception cref="System.IO.IOException"></exception>
        private void PeekSentence()
        {
            var facts = new List<string>();
            bool haveSentence = false;
            do
            {
                if (PeekToken())
                {
                    string term = new string(termAtt.Buffer, 0, termAtt.Length);
                    facts.Add(term + "-huh?");
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
                positions.Get(outputPos + x).Fact = facts[x];
            }
        }
    }
}
