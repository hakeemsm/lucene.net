/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Analysis.Util;

namespace Lucene.Net.Analysis.Fr
{
    /*
     * Removes elisions from a {@link TokenStream}. For example, "l'avion" (the plane) will be
     * tokenized as "avion" (plane).
     * <p>
     * Note that {@link StandardTokenizer} sees " ' " as a space, and cuts it out.
     * 
     * @see <a href="http://fr.wikipedia.org/wiki/%C3%89lision">Elision in Wikipedia</a>
     */
    public sealed class ElisionFilter : TokenFilter
    {
        private CharArraySet articles = null;
        private ICharTermAttribute termAtt;

        /**
   * Constructs an elision filter with a Set of stop words
   * @param input the source {@link TokenStream}
   * @param articles a set of stopword articles
   */
        public ElisionFilter(TokenStream input, CharArraySet articles)
            : base(input)
        {

            this.articles = articles;
        }

        /**
         * Increments the {@link TokenStream} with a {@link CharTermAttribute} without elisioned start
         */

        public override bool IncrementToken()
        {
            if (input.IncrementToken())
            {
                char[] termBuffer = termAtt.Buffer;
                int termLength = termAtt.Length;

                int index = -1;
                for (int i = 0; i < termLength; i++)
                {
                    char ch = termBuffer[i];
                    if (ch == '\'' || ch == '\u2019')
                    {
                        index = i;
                        break;
                    }
                }

                // An apostrophe has been found. If the prefix is an article strip it off.
                if (index >= 0 && articles.Contains(termBuffer, 0, index))
                {
                    termAtt.CopyBuffer(termBuffer, index + 1, termLength - (index + 1));
                }

                return true;
            }
            return false;
        }
    }
}
