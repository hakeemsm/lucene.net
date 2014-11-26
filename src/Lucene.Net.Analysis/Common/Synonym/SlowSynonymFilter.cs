/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Synonym;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Analysis.Synonym
{
	/// <summary>SynonymFilter handles multi-token synonyms with variable position increment offsets.
	/// 	</summary>
	/// <remarks>
	/// SynonymFilter handles multi-token synonyms with variable position increment offsets.
	/// <p>
	/// The matched tokens from the input stream may be optionally passed through (includeOrig=true)
	/// or discarded.  If the original tokens are included, the position increments may be modified
	/// to retain absolute positions after merging with the synonym tokenstream.
	/// <p>
	/// Generated synonyms will start at the same position as the first matched source token.
	/// </remarks>
	[System.ObsoleteAttribute(@"(3.4) use SynonymFilterFactory instead. only for precise index backwards compatibility. this factory will be removed in Lucene 5.0"
		)]
	internal sealed class SlowSynonymFilter : TokenFilter
	{
		private readonly SlowSynonymMap map;

		private Iterator<AttributeSource> replacement;

		public SlowSynonymFilter(TokenStream @in, SlowSynonymMap map) : base(@in)
		{
			// Map<String, SynonymMap>
			// iterator over generated tokens
			if (map == null)
			{
				throw new ArgumentException("map is required");
			}
			this.map = map;
			// just ensuring these attributes exist...
			AddAttribute<CharTermAttribute>();
			AddAttribute<PositionIncrementAttribute>();
			AddAttribute<OffsetAttribute>();
			AddAttribute<TypeAttribute>();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override bool IncrementToken()
		{
			while (true)
			{
				// if there are any generated tokens, return them... don't try any
				// matches against them, as we specifically don't want recursion.
				if (replacement != null && replacement.HasNext())
				{
					Copy(this, replacement.Next());
					return true;
				}
				// common case fast-path of first token not matching anything
				AttributeSource firstTok = NextTok();
				if (firstTok == null)
				{
					return false;
				}
				CharTermAttribute termAtt = firstTok.AddAttribute<CharTermAttribute>();
				SlowSynonymMap result = map.submap != null ? map.submap.Get(termAtt.Buffer, 0, 
					termAtt.Length) : null;
				if (result == null)
				{
					Copy(this, firstTok);
					return true;
				}
				// fast-path failed, clone ourselves if needed
				if (firstTok == this)
				{
					firstTok = CloneAttributes();
				}
				// OK, we matched a token, so find the longest match.
				matched = new List<AttributeSource>();
				result = Match(result);
				if (result == null)
				{
					// no match, simply return the first token read.
					Copy(this, firstTok);
					return true;
				}
				// reuse, or create new one each time?
				AList<AttributeSource> generated = new AList<AttributeSource>(result.synonyms.Length
					 + matched.Count + 1);
				//
				// there was a match... let's generate the new tokens, merging
				// in the matched tokens (position increments need adjusting)
				//
				AttributeSource lastTok = matched.IsEmpty() ? firstTok : matched.GetLast();
				bool includeOrig = result.IncludeOrig();
				AttributeSource origTok = includeOrig ? firstTok : null;
				PositionIncrementAttribute firstPosIncAtt = firstTok.AddAttribute<PositionIncrementAttribute
					>();
				int origPos = firstPosIncAtt.GetPositionIncrement();
				// position of origTok in the original stream
				int repPos = 0;
				// curr position in replacement token stream
				int pos = 0;
				// current position in merged token stream
				for (int i = 0; i < result.synonyms.Length; i++)
				{
					Token repTok = result.synonyms[i];
					AttributeSource newTok = firstTok.CloneAttributes();
					CharTermAttribute newTermAtt = newTok.AddAttribute<CharTermAttribute>();
					OffsetAttribute newOffsetAtt = newTok.AddAttribute<OffsetAttribute>();
					PositionIncrementAttribute newPosIncAtt = newTok.AddAttribute<PositionIncrementAttribute
						>();
					OffsetAttribute lastOffsetAtt = lastTok.AddAttribute<OffsetAttribute>();
					newOffsetAtt.SetOffset(newOffsetAtt.StartOffset(), lastOffsetAtt.EndOffset());
					newTermAtt.CopyBuffer(repTok.Buffer(), 0, repTok.Length);
					repPos += repTok.GetPositionIncrement();
					if (i == 0)
					{
						repPos = origPos;
					}
					// make position of first token equal to original
					// if necessary, insert original tokens and adjust position increment
					while (origTok != null && origPos <= repPos)
					{
						PositionIncrementAttribute origPosInc = origTok.AddAttribute<PositionIncrementAttribute
							>();
						origPosInc.SetPositionIncrement(origPos - pos);
						generated.AddItem(origTok);
						pos += origPosInc.GetPositionIncrement();
						origTok = matched.IsEmpty() ? null : matched.RemoveFirst();
						if (origTok != null)
						{
							origPosInc = origTok.AddAttribute<PositionIncrementAttribute>();
							origPos += origPosInc.GetPositionIncrement();
						}
					}
					newPosIncAtt.SetPositionIncrement(repPos - pos);
					generated.AddItem(newTok);
					pos += newPosIncAtt.GetPositionIncrement();
				}
				// finish up any leftover original tokens
				while (origTok != null)
				{
					PositionIncrementAttribute origPosInc = origTok.AddAttribute<PositionIncrementAttribute
						>();
					origPosInc.SetPositionIncrement(origPos - pos);
					generated.AddItem(origTok);
					pos += origPosInc.GetPositionIncrement();
					origTok = matched.IsEmpty() ? null : matched.RemoveFirst();
					if (origTok != null)
					{
						origPosInc = origTok.AddAttribute<PositionIncrementAttribute>();
						origPos += origPosInc.GetPositionIncrement();
					}
				}
				// what if we replaced a longer sequence with a shorter one?
				// a/0 b/5 =>  foo/0
				// should I re-create the gap on the next buffered token?
				replacement = generated.Iterator();
			}
		}

		private List<AttributeSource> buffer;

		private List<AttributeSource> matched;

		private bool exhausted;

		// Now return to the top of the loop to read and return the first
		// generated token.. The reason this is done is that we may have generated
		// nothing at all, and may need to continue with more matching logic.
		//
		// Defer creation of the buffer until the first time it is used to
		// optimize short fields with no matches.
		//
		/// <exception cref="System.IO.IOException"></exception>
		private AttributeSource NextTok()
		{
			if (buffer != null && !buffer.IsEmpty())
			{
				return buffer.RemoveFirst();
			}
			else
			{
				if (!exhausted && input.IncrementToken())
				{
					return this;
				}
				else
				{
					exhausted = true;
					return null;
				}
			}
		}

		private void PushTok(AttributeSource t)
		{
			if (buffer == null)
			{
				buffer = new List<AttributeSource>();
			}
			buffer.AddFirst(t);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private SlowSynonymMap Match(SlowSynonymMap map)
		{
			SlowSynonymMap result = null;
			if (map.submap != null)
			{
				AttributeSource tok = NextTok();
				if (tok != null)
				{
					// clone ourselves.
					if (tok == this)
					{
						tok = CloneAttributes();
					}
					// check for positionIncrement!=1?  if>1, should not match, if==0, check multiple at this level?
					CharTermAttribute termAtt = tok.GetAttribute<CharTermAttribute>();
					SlowSynonymMap subMap = map.submap.Get(termAtt.Buffer, 0, termAtt.Length);
					if (subMap != null)
					{
						// recurse
						result = Match(subMap);
					}
					if (result != null)
					{
						matched.AddFirst(tok);
					}
					else
					{
						// push back unmatched token
						PushTok(tok);
					}
				}
			}
			// if no longer sequence matched, so if this node has synonyms, it's the match.
			if (result == null && map.synonyms != null)
			{
				result = map;
			}
			return result;
		}

		private void Copy(AttributeSource target, AttributeSource source)
		{
			if (target != source)
			{
				source.CopyTo(target);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Reset()
		{
			input.Reset();
			replacement = null;
			exhausted = false;
		}
	}
}
