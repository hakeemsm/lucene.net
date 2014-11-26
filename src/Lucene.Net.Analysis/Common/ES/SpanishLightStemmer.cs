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

namespace Lucene.Net.Analysis.ES
{
    /// <summary>
    /// Light Stemmer for Spanish
    /// <p>
    /// This stemmer implements the algorithm described in:
    /// <i>Report on CLEF-2001 Experiments</i>
    /// Jacques Savoy
    /// </summary>
    public class SpanishLightStemmer
    {
        public virtual int Stem(char[] s, int len)
        {
            if (len < 5)
            {
                return len;
            }
            switch (s[len - 1])
            {
                case 'o':
                case 'a':
                case 'e':
                    {
                        //HM:uncomment
                        return len - 1;
                    }

                case 's':
                    {
                        if (s[len - 2] == 'e' && s[len - 3] == 's' && s[len - 4] == 'e')
                        {
                            return len - 2;
                        }
                        if (s[len - 2] == 'e' && s[len - 3] == 'c')
                        {
                            s[len - 3] = 'z';
                            return len - 2;
                        }
                        if (s[len - 2] == 'o' || s[len - 2] == 'a' || s[len - 2] == 'e')
                        {
                            return len - 2;
                        }
                    }
                    break;
            }
            return len;
        }
    }
}
