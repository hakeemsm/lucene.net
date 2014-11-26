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
using System.IO;
using Lucene.Net.Analysis.Cjk;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Analysis.CJK
{
    /// <summary>
    /// Filters CJKTokenizer with StopFilter.
    /// 
    /// <author>Che, Dong</author>
    /// </summary>
    public class CJKAnalyzer : StopwordAnalyzerBase
    {
        /// <summary>File containing default CJK stopwords.</summary>
        /// <remarks>
        /// File containing default CJK stopwords.
        /// <p/>
        /// Currently it contains some common English words that are not usually
        /// useful for searching and some double-byte interpunctions.
        /// </remarks>
        public static readonly string DEFAULT_STOPWORD_FILE = "CJKstopwords.txt";

        //~ Instance fields --------------------------------------------------------

        /// <summary>
        /// Returns an unmodifiable instance of the default stop-words set.
        /// </summary>
        /// <returns>Returns an unmodifiable instance of the default stop-words set.</returns>
        public static CharArraySet GetDefaultStopSet
        {
            get { return DefaultSetHolder.DEFAULT_STOP_SET; }
        }

        private class DefaultSetHolder
        {
            internal static readonly CharArraySet DEFAULT_STOP_SET;

            static DefaultSetHolder()
            {
                try
                {
                    DEFAULT_STOP_SET = LoadStopwordSet(false, typeof(CJKAnalyzer), DEFAULT_STOPWORD_FILE, "#");
                }
                catch (IOException)
                {
                    // default set should always be present as it is part of the
                    // distribution (JAR)
                    throw new Exception("Unable to load default stopword set");
                }
            }
        }

        /// <summary>
        /// Builds an analyzer which removes words in
        /// <see cref="GetDefaultStopSet()">GetDefaultStopSet()</see>
        /// .
        /// </summary>
        protected internal CJKAnalyzer(Version matchVersion)
            : this(matchVersion, CJKAnalyzer.DefaultSetHolder
                .DEFAULT_STOP_SET)
        {
        }

        /// <summary>
        /// <summary>Builds an analyzer with the given stop words</summary>
        /// <param name="matchVersion">lucene compatibility version</param>
        /// <param name="stopwords">a stopword set</param>
        protected internal CJKAnalyzer(Version matchVersion, CharArraySet stopwords)
            : base
                (matchVersion, stopwords)
        {
        }

        public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            if (matchVersion.Value.OnOrAfter(Version.LUCENE_36))
            {
                Tokenizer source = new StandardTokenizer(matchVersion, reader);
                // run the widthfilter first before bigramming, it sometimes combines characters.
                TokenStream result = new CJKWidthFilter(source);
                result = new LowerCaseFilter(matchVersion, result);
                result = new CJKBigramFilter(result);
                return new Analyzer.TokenStreamComponents(source, new StopFilter(matchVersion, result
                    , stopwords));
            }
            else
            {
                Tokenizer source = new CJKTokenizer(reader);
                return new Analyzer.TokenStreamComponents(source, new StopFilter(matchVersion, source
                    , stopwords));
            }
        }
    }
}
