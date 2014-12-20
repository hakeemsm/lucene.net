/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.TestFramework.Analysis;
using Lucene.Net.TestFramework.Analysis.Tokenattributes;
using Lucene.Net.TestFramework.Util;
using Sharpen;

namespace Lucene.Net.TestFramework.Analysis
{
	/// <summary>Randomly injects holes (similar to what a stopfilter would do)</summary>
	public sealed class MockHoleInjectingTokenFilter : TokenFilter
	{
		private readonly long randomSeed;

		private Random random;

		private readonly PositionIncrementAttribute posIncAtt = AddAttribute<PositionIncrementAttribute
			>();

		private readonly PositionLengthAttribute posLenAtt = AddAttribute<PositionLengthAttribute
			>();

		private int maxPos;

		private int pos;

		public MockHoleInjectingTokenFilter(Random random, TokenStream @in) : base(@in)
		{
			// TODO: maybe, instead to be more "natural", we should make
			// a MockRemovesTokensTF, ideally subclassing FilteringTF
			// (in modules/analysis)
			randomSeed = random.NextLong();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Reset()
		{
			base.Reset();
			random = new Random(randomSeed);
			maxPos = -1;
			pos = -1;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override bool IncrementToken()
		{
			if (input.IncrementToken())
			{
				int posInc = posIncAtt.GetPositionIncrement();
				int nextPos = pos + posInc;
				// Carefully inject a hole only where it won't mess up
				// the graph:
				if (posInc > 0 && maxPos <= nextPos && random.Next(5) == 3)
				{
					int holeSize = TestUtil.NextInt(random, 1, 5);
					posIncAtt.SetPositionIncrement(posInc + holeSize);
					nextPos += holeSize;
				}
				pos = nextPos;
				maxPos = Math.Max(maxPos, pos + posLenAtt.GetPositionLength());
				return true;
			}
			else
			{
				return false;
			}
		}
		// TODO: end?
	}
}
