/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/


using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;
using Lucene.Net.Util;

namespace Lucene.Net.Analysis.Cjk
{
	/// <summary>
	/// Forms bigrams of CJK terms that are generated from StandardTokenizer
	/// or ICUTokenizer.
	/// </summary>
	/// <remarks>
	/// Forms bigrams of CJK terms that are generated from StandardTokenizer
	/// or ICUTokenizer.
	/// <p>
	/// CJK types are set by these tokenizers, but you can also use
	/// <see cref="CJKBigramFilter(TokenStream, int)">CJKBigramFilter(Lucene.Net.Analysis.TokenStream, int)
	/// 	</see>
	/// to explicitly control which
	/// of the CJK scripts are turned into bigrams.
	/// <p>
	/// By default, when a CJK character has no adjacent characters to form
	/// a bigram, it is output in unigram form. If you want to always output
	/// both unigrams and bigrams, set the <code>outputUnigrams</code>
	/// flag in
	/// <see cref="CJKBigramFilter(TokenStream, int, bool)">CJKBigramFilter(Lucene.Net.Analysis.TokenStream, int, bool)
	/// 	</see>
	/// .
	/// This can be used for a combined unigram+bigram approach.
	/// <p>
	/// In all cases, all non-CJK input is passed thru unmodified.
	/// </remarks>
	public sealed class CJKBigramFilter : TokenFilter
	{
		/// <summary>bigram flag for Han Ideographs</summary>
		public const int HAN = 1;

		/// <summary>bigram flag for Hiragana</summary>
		public const int HIRAGANA = 2;

		/// <summary>bigram flag for Katakana</summary>
		public const int KATAKANA = 4;

		/// <summary>bigram flag for Hangul</summary>
		public const int HANGUL = 8;

		/// <summary>when we emit a bigram, its then marked as this type</summary>
		public static readonly string DOUBLE_TYPE = "<DOUBLE>";

		/// <summary>when we emit a unigram, its then marked as this type</summary>
		public static readonly string SINGLE_TYPE = "<SINGLE>";

		private static readonly string HAN_TYPE = StandardTokenizer.TOKEN_TYPES[StandardTokenizer
			.IDEOGRAPHIC];

		private static readonly string HIRAGANA_TYPE = StandardTokenizer.TOKEN_TYPES[StandardTokenizer
			.HIRAGANA];

		private static readonly string KATAKANA_TYPE = StandardTokenizer.TOKEN_TYPES[StandardTokenizer
			.KATAKANA];

		private static readonly string HANGUL_TYPE = StandardTokenizer.TOKEN_TYPES[StandardTokenizer
			.HANGUL];

		private static readonly object NO = new object();

		private readonly object doHan;

		private readonly object doHiragana;

		private readonly object doKatakana;

		private readonly object doHangul;

		private readonly bool outputUnigrams;

		private bool ngramState;

	    private readonly CharTermAttribute termAtt;

	    private readonly TypeAttribute typeAtt;

	    private readonly OffsetAttribute offsetAtt;

	    private readonly PositionIncrementAttribute posIncAtt;

	    private readonly PositionLengthAttribute posLengthAtt;

		internal int[] buffer = new int[8];

		internal int[] startOffset = new int[8];

		internal int[] endOffset = new int[8];

		internal int bufferLen;

		internal int index;

		internal int lastEndOffset;

		private bool exhausted;

	    /// <summary>
		/// Calls
		/// <see cref="CJKBigramFilter(TokenStream, int, bool)">CJKBigramFilter(in, flags, false)
		/// 	</see>
		/// </summary>
		public CJKBigramFilter(TokenStream ts, int flags = HAN | HIRAGANA | KATAKANA | HANGUL) : this(ts, flags, false)
		{
		}

		/// <summary>
		/// Create a new CJKBigramFilter, specifying which writing systems should be bigrammed,
		/// and whether or not unigrams should also be output.
		/// </summary>
		/// <remarks>
		/// Create a new CJKBigramFilter, specifying which writing systems should be bigrammed,
		/// and whether or not unigrams should also be output.
		/// </remarks>
		/// <param name="flags">
		/// OR'ed set from
		/// <see cref="HAN">HAN</see>
		/// ,
		/// <see cref="HIRAGANA">HIRAGANA</see>
		/// ,
		/// <see cref="KATAKANA">KATAKANA</see>
		/// ,
		/// <see cref="HANGUL">HANGUL</see>
		/// </param>
		/// <param name="outputUnigrams">
		/// true if unigrams for the selected writing systems should also be output.
		/// when this is false, this is only done when there are no adjacent characters to form
		/// a bigram.
		/// </param>
		public CJKBigramFilter(TokenStream ts, int flags, bool outputUnigrams) : base(ts)
		{
			// configuration
			// the types from standardtokenizer
			// sentinel value for ignoring a script 
			// these are set to either their type or NO if we want to pass them thru
			// true if we should output unigram tokens always
			// false = output unigram, true = output bigram
			// buffers containing codepoint and offsets in parallel
			// length of valid buffer
			// current buffer index
			// the last end offset, to determine if we should bigram across tokens
            termAtt = AddAttribute<CharTermAttribute>();
            typeAtt = AddAttribute<TypeAttribute>();
            offsetAtt = AddAttribute<OffsetAttribute>();
            posIncAtt = AddAttribute<PositionIncrementAttribute>();
            posLengthAtt = AddAttribute<PositionLengthAttribute>();

			doHan = (flags & HAN) == 0 ? NO : HAN_TYPE;
			doHiragana = (flags & HIRAGANA) == 0 ? NO : HIRAGANA_TYPE;
			doKatakana = (flags & KATAKANA) == 0 ? NO : KATAKANA_TYPE;
			doHangul = (flags & HANGUL) == 0 ? NO : HANGUL_TYPE;
			this.outputUnigrams = outputUnigrams;
		}

	    

	    /// <exception cref="System.IO.IOException"></exception>
		public override bool IncrementToken()
		{
			while (true)
			{
			    if (HasBufferedBigram())
				{
					// case 1: we have multiple remaining codepoints buffered,
					// so we can emit a bigram here.
					if (outputUnigrams)
					{
						// when also outputting unigrams, we output the unigram first,
						// then rewind back to revisit the bigram.
						// so an input of ABC is A + (rewind)AB + B + (rewind)BC + C
						// the logic in hasBufferedUnigram ensures we output the C, 
						// even though it did actually have adjacent CJK characters.
						if (ngramState)
						{
							FlushBigram();
						}
						else
						{
							FlushUnigram();
							index--;
						}
						ngramState = !ngramState;
					}
					else
					{
						FlushBigram();
					}
					return true;
				}
			    if (DoNext())
			    {
			        // case 2: look at the token type. should we form any n-grams?
			        string type = typeAtt.Type;
			        if (type == (string) doHan || type == (string) doHiragana || type == (string) doKatakana || type == (string) doHangul)
			        {
			            // acceptable CJK type: we form n-grams from these.
			            // as long as the offsets are aligned, we just add these to our current buffer.
			            // otherwise, we clear the buffer and start over.
			            if (offsetAtt.StartOffset != lastEndOffset)
			            {
			                // unaligned, clear queue
			                if (HasBufferedUnigram())
			                {
			                    // we have a buffered unigram, and we peeked ahead to see if we could form
			                    // a bigram, but we can't, because the offsets are unaligned. capture the state 
			                    // of this peeked data to be revisited next time thru the loop, and dump our unigram.
			                    loneState = CaptureState();
			                    FlushUnigram();
			                    return true;
			                }
			                index = 0;
			                bufferLen = 0;
			            }
			            Refill();
			        }
			        else
			        {
			            // not a CJK type: we just return these as-is.
			            if (HasBufferedUnigram())
			            {
			                // we have a buffered unigram, and we peeked ahead to see if we could form
			                // a bigram, but we can't, because its not a CJK type. capture the state 
			                // of this peeked data to be revisited next time thru the loop, and dump our unigram.
			                loneState = CaptureState();
			                FlushUnigram();
			                return true;
			            }
			            return true;
			        }
			    }
			    else
			    {
			        // case 3: we have only zero or 1 codepoints buffered, 
			        // so not enough to form a bigram. But, we also have no
			        // more input. So if we have a buffered codepoint, emit
			        // a unigram, otherwise, its end of stream.
			        if (HasBufferedUnigram())
			        {
			            FlushUnigram();
			            // flush our remaining unigram
			            return true;
			        }
			        return false;
			    }
			}
		}

		private State loneState;

		// rarely used: only for "lone cjk characters", where we emit unigrams
		/// <summary>looks at next input token, returning false is none is available</summary>
		/// <exception cref="System.IO.IOException"></exception>
		private bool DoNext()
		{
			if (loneState != null)
			{
				RestoreState(loneState);
				loneState = null;
				return true;
			}
			else
			{
				if (exhausted)
				{
					return false;
				}
				else
				{
					if (input.IncrementToken())
					{
						return true;
					}
					else
					{
						exhausted = true;
						return false;
					}
				}
			}
		}

		/// <summary>refills buffers with new data from the current token.</summary>
		/// <remarks>refills buffers with new data from the current token.</remarks>
		private void Refill()
		{
			// compact buffers to keep them smallish if they become large
			// just a safety check, but technically we only need the last codepoint
			if (bufferLen > 64)
			{
				int last = bufferLen - 1;
				buffer[0] = buffer[last];
				startOffset[0] = startOffset[last];
				endOffset[0] = endOffset[last];
				bufferLen = 1;
				index -= last;
			}
			char[] termBuffer = termAtt.Buffer;
			int len = termAtt.Length;
			int start = offsetAtt.StartOffset;
			int end = offsetAtt.EndOffset;
			int newSize = bufferLen + len;
			buffer = ArrayUtil.Grow(buffer, newSize);
			startOffset = ArrayUtil.Grow(startOffset, newSize);
			endOffset = ArrayUtil.Grow(endOffset, newSize);
			lastEndOffset = end;
		    var charUtils = CharacterUtils.GetInstance();
		    if (end - start != len)
			{
				// crazy offsets (modified by synonym or charfilter): just preserve
				for (int i = 0, cp=0; i < len; i += Character.CharCount(cp))
				{
					cp = buffer[bufferLen] = charUtils.CodePointAt(termBuffer, i, len);
					startOffset[bufferLen] = start;
					endOffset[bufferLen] = end;
					bufferLen++;
				}
			}
			else
			{
				// normal offsets
				for (int i = 0, cp=0,cpLen=0; i < len; i += cpLen)
				{
					cp = buffer[bufferLen] = charUtils.CodePointAt(termBuffer, i, len);
					cpLen = Character.CharCount(cp);
					startOffset[bufferLen] = start;
					start = endOffset[bufferLen] = start + cpLen;
					bufferLen++;
				}
			}
		}

		/// <summary>
		/// Flushes a bigram token to output from our buffer
		/// This is the normal case, e.g.
		/// </summary>
		/// <remarks>
		/// Flushes a bigram token to output from our buffer
		/// This is the normal case, e.g. ABC -&gt; AB BC
		/// </remarks>
		private void FlushBigram()
		{
			ClearAttributes();
			char[] termBuffer = termAtt.ResizeBuffer(4);
			// maximum bigram length in code units (2 supplementaries)
			int len1 = Character.ToChars(buffer[index], termBuffer, 0);
			int len2 = len1 + Character.ToChars(buffer[index + 1], termBuffer, len1);
			termAtt.SetLength(len2);
			offsetAtt.SetOffset(startOffset[index], endOffset[index + 1]);
			typeAtt.Type= DOUBLE_TYPE;
			// when outputting unigrams, all bigrams are synonyms that span two unigrams
			if (outputUnigrams)
			{
				posIncAtt.PositionIncrement = 0;
				posLengthAtt.PositionLength = 2;
			}
			index++;
		}

		/// <summary>Flushes a unigram token to output from our buffer.</summary>
		/// <remarks>
		/// Flushes a unigram token to output from our buffer.
		/// This happens when we encounter isolated CJK characters, either the whole
		/// CJK string is a single character, or we encounter a CJK character surrounded
		/// by space, punctuation, english, etc, but not beside any other CJK.
		/// </remarks>
		private void FlushUnigram()
		{
			ClearAttributes();
			char[] termBuffer = termAtt.ResizeBuffer(2);
			// maximum unigram length (2 surrogates)
			int len = Character.ToChars(buffer[index], termBuffer, 0);
			termAtt.SetLength(len);
			offsetAtt.SetOffset(startOffset[index], endOffset[index]);
			typeAtt.Type = SINGLE_TYPE;
			index++;
		}

		/// <summary>True if we have multiple codepoints sitting in our buffer</summary>
		private bool HasBufferedBigram()
		{
			return bufferLen - index > 1;
		}

		/// <summary>
		/// True if we have a single codepoint sitting in our buffer, where its future
		/// (whether it is emitted as unigram or forms a bigram) depends upon not-yet-seen
		/// inputs.
		/// </summary>
		/// <remarks>
		/// True if we have a single codepoint sitting in our buffer, where its future
		/// (whether it is emitted as unigram or forms a bigram) depends upon not-yet-seen
		/// inputs.
		/// </remarks>
		private bool HasBufferedUnigram()
		{
			if (outputUnigrams)
			{
				// when outputting unigrams always
				return bufferLen - index == 1;
			}
			else
			{
				// otherwise its only when we have a lone CJK character
				return bufferLen == 1 && index == 0;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Reset()
		{
			base.Reset();
			bufferLen = 0;
			index = 0;
			lastEndOffset = 0;
			loneState = null;
			exhausted = false;
			ngramState = false;
		}
	}
}
