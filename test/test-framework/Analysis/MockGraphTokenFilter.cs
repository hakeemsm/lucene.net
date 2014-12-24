/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.TestFramework.Analysis;
using Lucene.Net.TestFramework.Analysis.Tokenattributes;
using Lucene.Net.TestFramework.Util;
using Sharpen;

namespace Lucene.Net.TestFramework.Analysis
{
	/// <summary>
	/// Randomly inserts overlapped (posInc=0) tokens with
	/// posLength sometimes &gt; 1.
	/// </summary>
	/// <remarks>
	/// Randomly inserts overlapped (posInc=0) tokens with
	/// posLength sometimes &gt; 1.  The chain must have
	/// an OffsetAttribute.
	/// </remarks>
	public sealed class MockGraphTokenFilter : LookaheadTokenFilter<LookaheadTokenFilter.Position
		>
	{
		private static bool DEBUG = false;

		private readonly CharTermAttribute termAtt = AddAttribute<CharTermAttribute>();

		private readonly long seed;

		private Random random;

		public MockGraphTokenFilter(Random random, TokenStream input) : base(input)
		{
			// TODO: sometimes remove tokens too...?
			seed = random.NextLong();
		}

		protected internal override LookaheadTokenFilter.Position NewPosition()
		{
			return new LookaheadTokenFilter.Position();
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal override void AfterPosition()
		{
			if (DEBUG)
			{
				System.Console.Out.WriteLine("MockGraphTF.afterPos");
			}
			if (random.Next(7) == 5)
			{
				int posLength = TestUtil.NextInt(random, 1, 5);
				if (DEBUG)
				{
					System.Console.Out.WriteLine("  do insert! posLen=" + posLength);
				}
				LookaheadTokenFilter.Position posEndData = positions.Get(outputPos + posLength);
				// Look ahead as needed until we figure out the right
				// endOffset:
				while (!end && posEndData.endOffset == -1 && inputPos <= (outputPos + posLength))
				{
					if (!PeekToken())
					{
						break;
					}
				}
				if (posEndData.endOffset != -1)
				{
					// Notify super class that we are injecting a token:
					InsertToken();
					ClearAttributes();
					posLenAtt.SetPositionLength(posLength);
					termAtt.Append(TestUtil.RandomUnicodeString(random));
					posIncAtt.SetPositionIncrement(0);
					offsetAtt.SetOffset(positions.Get(outputPos).startOffset, posEndData.endOffset);
					if (DEBUG)
					{
						System.Console.Out.WriteLine("  inject: outputPos=" + outputPos + " startOffset="
							 + offsetAtt.StartOffset() + " endOffset=" + offsetAtt.EndOffset() + " posLength="
							 + posLenAtt.GetPositionLength());
					}
				}
			}
		}

		// TODO: set TypeAtt too?
		// Either 1) the tokens ended before our posLength,
		// or 2) our posLength ended inside a hole from the
		// input.  In each case we just skip the inserted
		// token.
		/// <exception cref="System.IO.IOException"></exception>
		public override void Reset()
		{
			base.Reset();
			// NOTE: must be "deterministically random" because
			// BaseTokenStreamTestCase pulls tokens twice on the
			// same input and asserts they are the same:
			this.random = new Random(seed);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			base.Close();
			this.random = null;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override bool IncrementToken()
		{
			if (DEBUG)
			{
				System.Console.Out.WriteLine("MockGraphTF.incr inputPos=" + inputPos + " outputPos="
					 + outputPos);
			}
			if (random == null)
			{
				throw new InvalidOperationException("incrementToken called in wrong state!");
			}
			return NextToken();
		}
	}
}
