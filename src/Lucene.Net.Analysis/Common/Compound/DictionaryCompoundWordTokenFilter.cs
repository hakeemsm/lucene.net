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

using System;
using System.Collections.Generic;
using Lucene.Net.Analysis.Util;

namespace Lucene.Net.Analysis.Compound
{
    /*
 * A {@link TokenFilter} that decomposes compound words found in many Germanic languages.
 * <p>
 * "Donaudampfschiff" becomes Donau, dampf, schiff so that you can find
 * "Donaudampfschiff" even when you only enter "schiff". 
 *  It uses a brute-force algorithm to achieve this.
 * </p>
 */
    public class DictionaryCompoundWordTokenFilter : CompoundWordTokenFilterBase
    {
        /// <summary>
        /// Creates a new
        /// <see cref="DictionaryCompoundWordTokenFilter">DictionaryCompoundWordTokenFilter</see>
        /// </summary>
        /// <param name="matchVersion">
        /// Lucene version to enable correct Unicode 4.0 behavior in the
        /// dictionaries if Version &gt; 3.0. See &lt;a
        /// href="CompoundWordTokenFilterBase.html#version"
        /// &gt;CompoundWordTokenFilterBase</a> for details.
        /// </param>
        /// <param name="input">
        /// the
        /// <see cref="Lucene.Net.Analysis.TokenStream">Lucene.Net.Analysis.TokenStream
        /// 	</see>
        /// to process
        /// </param>
        /// <param name="dictionary">the word dictionary to match against.</param>
        protected internal DictionaryCompoundWordTokenFilter(Lucene.Net.Util.Version matchVersion, TokenStream
             input, CharArraySet dictionary)
            : base(matchVersion, input, dictionary)
        {
            if (dictionary == null)
            {
                throw new ArgumentException("dictionary cannot be null");
            }
        }

        /*
         * 
         * @param input the {@link TokenStream} to process
         * @param dictionary the word dictionary to match against
         * @param minWordSize only words longer than this get processed
         * @param minSubwordSize only subwords longer than this get to the output stream
         * @param maxSubwordSize only subwords shorter than this get to the output stream
         * @param onlyLongestMatch Add only the longest matching subword to the stream
         */
        public DictionaryCompoundWordTokenFilter(Lucene.Net.Util.Version matchVersion, TokenStream input, CharArraySet dictionary,
            int minWordSize, int minSubwordSize, int maxSubwordSize, bool onlyLongestMatch)
            : base(matchVersion, input, dictionary, minWordSize, minSubwordSize, maxSubwordSize, onlyLongestMatch)
        {
            if (dictionary == null)
            {
                throw new ArgumentException("dictionary cannot be null");
            }
        }
        

       protected override void Decompose()
        {
            int len = termAtt.Length;
            for (int i = 0; i <= len - this.minSubwordSize; ++i)
            {
                CompoundToken longestMatchToken = null;
                for (int j = this.minSubwordSize; j <= this.maxSubwordSize; ++j)
                {
                    if (i + j > len)
                    {
                        break;
                    }
                    if (dictionary.Contains(termAtt.Buffer, i, j))
                    {
                        if (this.onlyLongestMatch)
                        {
                            if (longestMatchToken != null)
                            {
                                if (longestMatchToken.txt.Length < j)
                                {
                                    longestMatchToken = new CompoundToken(this, i, j);
                                }
                            }
                            else
                            {
                                longestMatchToken = new CompoundToken(this, i, j);
                            }
                        }
                        else
                        {
                            tokens.AddLast(new CompoundToken(this, i, j));
                        }
                    }
                }
                if (this.onlyLongestMatch && longestMatchToken != null)
                {
                    tokens.AddLast(longestMatchToken);
                }
            }
        }
    }
}
