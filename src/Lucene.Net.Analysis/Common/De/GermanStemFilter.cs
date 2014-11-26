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
using Lucene.Net.Analysis.Tokenattributes;

namespace Lucene.Net.Analysis.De
{
    /// <summary>
    /// A filter that stems German words. It supports a table of words that should
    /// not be stemmed at all. The stemmer used can be changed at runtime after the
    /// filter object is created (as long as it is a GermanStemmer).
    /// </summary>
    public sealed class GermanStemFilter : TokenFilter
    {
        /**
     * The actual token in the input stream.
     */
        private GermanStemmer stemmer = new GermanStemmer();

        private CharTermAttribute termAtt;
        private KeywordAttribute keywordAttr;

        /**
     * Creates a {@link GermanStemFilter} instance
     * @param in the source {@link TokenStream} 
     */

        public GermanStemFilter(TokenStream ts) : base(ts)
        {
            termAtt = AddAttribute<CharTermAttribute>();
            keywordAttr = AddAttribute<KeywordAttribute>();
        }

        /**
     * @return  Returns true for next token in the stream, or false at EOS
     */

        public override bool IncrementToken()
        {
            if (input.IncrementToken())
            {
                String term = termAtt.ToString();

                if (!keywordAttr.IsKeyword)
                {
                    String s = stemmer.Stem(term);
                    // If not stemmed, don't waste the time adjusting the token.
                    if ((s != null) && !s.Equals(term))
                        termAtt.SetEmpty().Append(s);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        /**
     * Set a alternative/custom {@link GermanStemmer} for this filter.
     */

        public void SetStemmer(GermanStemmer stemmer)
        {
            if (stemmer != null)
            {
                this.stemmer = stemmer;
            }
        }
    }
}