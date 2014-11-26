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


using Lucene.Analysis.Util;
using Lucene.Net.Analysis.Tokenattributes;

namespace Lucene.Net.Analysis.Cjk
{
    /// <summary>
    /// A
    /// <see cref="TokenFilter">Lucene.Net.Analysis.TokenFilter
    /// 	</see>
    /// that normalizes CJK width differences:
    /// <ul>
    /// <li>Folds fullwidth ASCII variants into the equivalent basic latin
    /// <li>Folds halfwidth Katakana variants into the equivalent kana
    /// </ul>
    /// <p>
    /// NOTE: this filter can be viewed as a (practical) subset of NFKC/NFKD
    /// Unicode normalization. See the normalization support in the ICU package
    /// for full normalization.
    /// </summary>
    public sealed class CJKWidthFilter : TokenFilter
    {
        private CharTermAttribute termAtt;

        private static readonly int[] KANA_NORM =
		{ unchecked(0x30fb), unchecked(0x30f2), unchecked(0x30a1), unchecked(0x30a3), unchecked(0x30a5), unchecked(0x30a7), unchecked((int)(0x30a9)), unchecked(0x30e3), unchecked(0x30e5), unchecked(0x30e7), unchecked(0x30c3), unchecked(0x30fc), unchecked(0x30a2), unchecked(0x30a4)
		    , unchecked(0x30a6), unchecked(0x30a8), unchecked(0x30aa), 
		    unchecked(0x30ab), unchecked(0x30ad), unchecked(0x30af), unchecked(
		        (0x30b1)), unchecked((0x30b3)), unchecked((0x30b5)), unchecked((int
		            )(0x30b7)), unchecked((0x30b9)), unchecked((0x30bb)), unchecked((
		                0x30bd)), unchecked((0x30bf)), unchecked((0x30c1)), unchecked((0x30c4
		                    )), unchecked((0x30c6)), unchecked((0x30c8)), unchecked((0x30ca))
		    , unchecked((0x30cb)), unchecked((0x30cc)), unchecked((0x30cd)), 
		    unchecked((0x30ce)), unchecked((0x30cf)), unchecked((0x30d2)), unchecked(
		        (0x30d5)), unchecked((0x30d8)), unchecked((0x30db)), unchecked((int
		            )(0x30de)), unchecked((0x30df)), unchecked((0x30e0)), unchecked((
		                0x30e1)), unchecked((0x30e2)), unchecked((0x30e4)), unchecked((0x30e6
		                    )), unchecked((0x30e8)), unchecked((0x30e9)), unchecked((0x30ea))
		    , unchecked((0x30eb)), unchecked((0x30ec)), unchecked((0x30ed)), 
		    unchecked((0x30ef)), unchecked((0x30f3)), unchecked((0x3099)), unchecked(
		        (0x309A)) };

        public CJKWidthFilter(TokenStream input)
            : base(input)
        {
            termAtt = AddAttribute<CharTermAttribute>();
        }

        /// <exception cref="System.IO.IOException"></exception>
        public override bool IncrementToken()
        {
            if (input.IncrementToken())
            {
                char[] text = termAtt.Buffer;
                int length = termAtt.Length;
                for (int i = 0; i < length; i++)
                {
                    char ch = text[i];
                    if (ch >= unchecked(0xFF01) && ch <= unchecked(0xFF5E))
                    {
                        // Fullwidth ASCII variants
                        text[i] -= (char)unchecked((int)(0xFEE0));
                    }
                    else
                    {
                        if (ch >= unchecked(0xFF65) && ch <= unchecked(0xFF9F))
                        {
                            // Halfwidth Katakana variants
                            if ((ch == unchecked(0xFF9E) || ch == unchecked(0xFF9F)) && i > 0 &&
                                 Combine(text, i, ch))
                            {
                                length = StemmerUtil.Delete(text, i--, length);
                            }
                            else
                            {
                                text[i] = (char)KANA_NORM[ch - unchecked(0xFF65)];
                            }
                        }
                    }
                }
                termAtt.SetLength(length);
                return true;
            }
            return false;
        }

        private static readonly byte[] KANA_COMBINE_VOICED =
		{ 78, 0, 0, 0, 0, 1
		    , 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 0, 1, 0, 
		    1, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 
		    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 8, 8, 8, 8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		    0, 1 };

        private static readonly byte[] KANA_COMBINE_HALF_VOICED =
		{ 0, 0, 0, 0, 
		    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 2, 0, 0, 2, 0, 0, 2, 0, 0, 2, 0, 0, 0, 0, 
		    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		    0, 0, 0 };

        /// <summary>returns true if we successfully combined the voice mark</summary>
        private static bool Combine(char[] text, int pos, char ch)
        {
            char prev = text[pos - 1];
            if (prev >= unchecked((int)(0x30A6)) && prev <= unchecked((int)(0x30FD)))
            {
                text[pos - 1] += (char)(ch == unchecked((int)(0xFF9F))) ? KANA_COMBINE_HALF_VOICED
                    [prev - unchecked((int)(0x30A6))] : KANA_COMBINE_VOICED[prev - unchecked((int)(0x30A6
                    ))];
                return text[pos - 1] != prev;
            }
            return false;
        }
    }
}
