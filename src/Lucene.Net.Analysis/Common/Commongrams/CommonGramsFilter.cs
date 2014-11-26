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

using System.Text;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;

namespace Lucene.Net.Analysis.Commongrams
{
	/// <summary>Construct bigrams for frequently occurring terms while indexing.</summary>
	/// <remarks>
	/// Construct bigrams for frequently occurring terms while indexing. Single terms
	/// are still indexed too, with bigrams overlaid. This is achieved through the
	/// use of
	/// <see cref="Lucene.Net.Analysis.Tokenattributes.PositionIncrementAttribute.SetPositionIncrement(int)
	/// 	">Lucene.Net.Analysis.Tokenattributes.PositionIncrementAttribute.SetPositionIncrement(int)
	/// 	</see>
	/// . Bigrams have a type
	/// of
	/// <see cref="GRAM_TYPE">GRAM_TYPE</see>
	/// Example:
	/// <ul>
	/// <li>input:"the quick brown fox"</li>
	/// <li>output:|"the","the-quick"|"brown"|"fox"|</li>
	/// <li>"the-quick" has a position increment of 0 so it is in the same position
	/// as "the" "the-quick" has a term.type() of "gram"</li>
	/// </ul>
	/// </remarks>
	public sealed class CommonGramsFilter : TokenFilter
	{
		public static readonly string GRAM_TYPE = "gram";

		private const char SEPARATOR = '_';

		private readonly CharArraySet commonWords;

		private readonly StringBuilder buffer = new StringBuilder();

	    private readonly CharTermAttribute termAttribute;

		private readonly OffsetAttribute offsetAttribute;

	    private readonly TypeAttribute typeAttribute;

	    private readonly PositionIncrementAttribute posIncAttribute;

	    private readonly PositionLengthAttribute posLenAttribute;

		private int lastStartOffset;

		private bool lastWasCommon;

		private AttributeSource.State savedState;

		/// <summary>
		/// Construct a token stream filtering the given input using a Set of common
		/// words to create bigrams.
		/// </summary>
		/// <remarks>
		/// Construct a token stream filtering the given input using a Set of common
		/// words to create bigrams. Outputs both unigrams with position increment and
		/// bigrams with position increment 0 type=gram where one or both of the words
		/// in a potential bigram are in the set of common words .
		/// </remarks>
		/// <param name="input">TokenStream input in filter chain</param>
		/// <param name="commonWords">The set of common words.</param>
		public CommonGramsFilter(Version matchVersion, TokenStream input, CharArraySet commonWords) : base(input)
		{
            termAttribute = AddAttribute<CharTermAttribute>();
            offsetAttribute = AddAttribute<OffsetAttribute>();
            typeAttribute = AddAttribute<TypeAttribute>();
            posIncAttribute = AddAttribute<PositionIncrementAttribute>();
            posLenAttribute = AddAttribute<PositionLengthAttribute>();
			this.commonWords = commonWords;
		}

		/// <summary>Inserts bigrams for common words into a token stream.</summary>
		/// <remarks>
		/// Inserts bigrams for common words into a token stream. For each input token,
		/// output the token. If the token and/or the following token are in the list
		/// of common words also output a bigram with position increment 0 and
		/// type="gram"
		/// TODO:Consider adding an option to not emit unigram stopwords
		/// as in CDL XTF BigramStopFilter, CommonGramsQueryFilter would need to be
		/// changed to work with this.
		/// TODO: Consider optimizing for the case of three
		/// commongrams i.e "man of the year" normally produces 3 bigrams: "man-of",
		/// "of-the", "the-year" but with proper management of positions we could
		/// eliminate the middle bigram "of-the"and save a disk seek and a whole set of
		/// position lookups.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public override bool IncrementToken()
		{
			// get the next piece of input
			if (savedState != null)
			{
				RestoreState(savedState);
				savedState = null;
				SaveTermBuffer();
				return true;
			}
			else
			{
				if (!input.IncrementToken())
				{
					return false;
				}
			}
			if (lastWasCommon || (IsCommon() && buffer.Length > 0))
			{
				savedState = CaptureState();
				GramToken();
				return true;
			}
			SaveTermBuffer();
			return true;
		}

		/// <summary><inheritDoc></inheritDoc></summary>
		/// <exception cref="System.IO.IOException"></exception>
		public override void Reset()
		{
			base.Reset();
			lastWasCommon = false;
			savedState = null;
			buffer.Length = 0;
		}

		// ================================================= Helper Methods ================================================
		/// <summary>Determines if the current token is a common term</summary>
		/// <returns>
		/// 
		/// <code>true</code>
		/// if the current token is a common term,
		/// <code>false</code>
		/// otherwise
		/// </returns>
		private bool IsCommon()
		{
			return commonWords != null && commonWords.Contains(termAttribute.Buffer, 0, termAttribute.Length);
		}

		/// <summary>Saves this information to form the left part of a gram</summary>
		private void SaveTermBuffer()
		{
			buffer.Length = 0;
			buffer.Append(termAttribute.Buffer, 0, termAttribute.Length);
			buffer.Append(SEPARATOR);
			lastStartOffset = offsetAttribute.StartOffset;
			lastWasCommon = IsCommon();
		}

		/// <summary>Constructs a compound token.</summary>
		/// <remarks>Constructs a compound token.</remarks>
		private void GramToken()
		{
			buffer.Append(termAttribute.Buffer, 0, termAttribute.Length);
			int endOffset = offsetAttribute.EndOffset;
			ClearAttributes();
			int length = buffer.Length;
			char[] termText = termAttribute.Buffer;
			if (length > termText.Length)
			{
				termText = termAttribute.ResizeBuffer(length);
			}
			buffer.ToString().GetChars(0, length, termText, 0);
			termAttribute.SetLength(length);
			posIncAttribute.PositionIncrement=0;
			posLenAttribute.PositionLength=2;
			// bigram
			offsetAttribute.SetOffset(lastStartOffset, endOffset);
			typeAttribute.Type=GRAM_TYPE;
			buffer.Length = 0;
		}
	}
}
