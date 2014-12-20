/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.TestFramework.Analysis;
using Lucene.Net.TestFramework.Analysis.Tokenattributes;
using Lucene.Net.TestFramework.Util;
using Sharpen;

namespace Lucene.Net.TestFramework.Analysis
{
	/// <summary>
	/// A TokenFilter that checks consistency of the tokens (eg
	/// offsets are consistent with one another).
	/// </summary>
	/// <remarks>
	/// A TokenFilter that checks consistency of the tokens (eg
	/// offsets are consistent with one another).
	/// </remarks>
	public sealed class ValidatingTokenFilter : TokenFilter
	{
		private int pos;

		private int lastStartOffset;

		private readonly IDictionary<int, int> posToStartOffset = new Dictionary<int, int
			>();

		private readonly IDictionary<int, int> posToEndOffset = new Dictionary<int, int>(
			);

		private readonly PositionIncrementAttribute posIncAtt;

		private readonly PositionLengthAttribute posLenAtt;

		private readonly OffsetAttribute offsetAtt;

		private readonly CharTermAttribute termAtt;

		private readonly bool offsetsAreCorrect;

		private readonly string name;

		// TODO: rename to OffsetsXXXTF?  ie we only validate
		// offsets (now anyway...)
		// TODO: also make a DebuggingTokenFilter, that just prints
		// all att values that come through it...
		// TODO: BTSTC should just append this to the chain
		// instead of checking itself:
		// Maps position to the start/end offset:
		// Returns null if the attr wasn't already added
		private A GetAttrIfExists<A>() where A:Attribute
		{
			System.Type att = typeof(A);
			if (HasAttribute(att))
			{
				return GetAttribute(att);
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// The name arg is used to identify this stage when
		/// throwing exceptions (useful if you have more than one
		/// instance in your chain).
		/// </summary>
		/// <remarks>
		/// The name arg is used to identify this stage when
		/// throwing exceptions (useful if you have more than one
		/// instance in your chain).
		/// </remarks>
		public ValidatingTokenFilter(TokenStream @in, string name, bool offsetsAreCorrect
			) : base(@in)
		{
			posIncAtt = GetAttrIfExists<PositionIncrementAttribute>();
			posLenAtt = GetAttrIfExists<PositionLengthAttribute>();
			offsetAtt = GetAttrIfExists<OffsetAttribute>();
			termAtt = GetAttrIfExists<CharTermAttribute>();
			this.name = name;
			this.offsetsAreCorrect = offsetsAreCorrect;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override bool IncrementToken()
		{
			if (!input.IncrementToken())
			{
				return false;
			}
			int startOffset = 0;
			int endOffset = 0;
			int posLen = 0;
			if (posIncAtt != null)
			{
				pos += posIncAtt.GetPositionIncrement();
				if (pos == -1)
				{
					throw new InvalidOperationException("first posInc must be > 0");
				}
			}
			// System.out.println("  got token=" + termAtt + " pos=" + pos);
			if (offsetAtt != null)
			{
				startOffset = offsetAtt.StartOffset();
				endOffset = offsetAtt.EndOffset();
				if (offsetsAreCorrect && offsetAtt.StartOffset() < lastStartOffset)
				{
					throw new InvalidOperationException(name + ": offsets must not go backwards startOffset="
						 + startOffset + " is < lastStartOffset=" + lastStartOffset);
				}
				lastStartOffset = offsetAtt.StartOffset();
			}
			posLen = posLenAtt == null ? 1 : posLenAtt.GetPositionLength();
			if (offsetAtt != null && posIncAtt != null && offsetsAreCorrect)
			{
				if (!posToStartOffset.ContainsKey(pos))
				{
					// First time we've seen a token leaving from this position:
					posToStartOffset.Put(pos, startOffset);
				}
				else
				{
					//System.out.println("  + s " + pos + " -> " + startOffset);
					// We've seen a token leaving from this position
					// before; verify the startOffset is the same:
					//System.out.println("  + vs " + pos + " -> " + startOffset);
					int oldStartOffset = posToStartOffset.Get(pos);
					if (oldStartOffset != startOffset)
					{
						throw new InvalidOperationException(name + ": inconsistent startOffset at pos=" +
							 pos + ": " + oldStartOffset + " vs " + startOffset + "; token=" + termAtt);
					}
				}
				int endPos = pos + posLen;
				if (!posToEndOffset.ContainsKey(endPos))
				{
					// First time we've seen a token arriving to this position:
					posToEndOffset.Put(endPos, endOffset);
				}
				else
				{
					//System.out.println("  + e " + endPos + " -> " + endOffset);
					// We've seen a token arriving to this position
					// before; verify the endOffset is the same:
					//System.out.println("  + ve " + endPos + " -> " + endOffset);
					int oldEndOffset = posToEndOffset.Get(endPos);
					if (oldEndOffset != endOffset)
					{
						throw new InvalidOperationException(name + ": inconsistent endOffset at pos=" + endPos
							 + ": " + oldEndOffset + " vs " + endOffset + "; token=" + termAtt);
					}
				}
			}
			return true;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void End()
		{
			base.End();
		}

		// TODO: what else to validate
		// TODO: check that endOffset is >= max(endOffset)
		// we've seen
		/// <exception cref="System.IO.IOException"></exception>
		public override void Reset()
		{
			base.Reset();
			pos = -1;
			posToStartOffset.Clear();
			posToEndOffset.Clear();
			lastStartOffset = 0;
		}
	}
}
