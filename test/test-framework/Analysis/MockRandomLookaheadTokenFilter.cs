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
using Sharpen;

namespace Lucene.Net.TestFramework.Analysis
{
	/// <summary>
	/// Uses
	/// <see cref="LookaheadTokenFilter{T}">LookaheadTokenFilter&lt;T&gt;</see>
	/// to randomly peek at future tokens.
	/// </summary>
	public sealed class MockRandomLookaheadTokenFilter : LookaheadTokenFilter<LookaheadTokenFilter.Position
		>
	{
		private const bool DEBUG = false;

		private readonly CharTermAttribute termAtt = AddAttribute<CharTermAttribute>();

		private readonly Random random;

		private readonly long seed;

		public MockRandomLookaheadTokenFilter(Random random, TokenStream @in) : base(@in)
		{
			this.seed = random.NextLong();
			this.random = new Random(seed);
		}

		protected internal override LookaheadTokenFilter.Position NewPosition()
		{
			return new LookaheadTokenFilter.Position();
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal override void AfterPosition()
		{
			if (!end && random.Next(4) == 2)
			{
				PeekToken();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override bool IncrementToken()
		{
			if (!end)
			{
				while (true)
				{
					if (random.Next(3) == 1)
					{
						if (!PeekToken())
						{
							break;
						}
					}
					else
					{
						break;
					}
				}
			}
			bool result = NextToken();
			if (result)
			{
			}
			return result;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Reset()
		{
			base.Reset();
			random.SetSeed(seed);
		}
	}
}
