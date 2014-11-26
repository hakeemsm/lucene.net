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
using System.IO;
using System.Linq;
using Lucene.Net.Analysis.AR;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Analysis.Fa
{
    /*
     * {@link Analyzer} for Persian.
     * <p>
     * This Analyzer uses {@link ArabicLetterTokenizer} which implies tokenizing around
     * zero-width non-joiner in addition to whitespace. Some persian-specific variant forms (such as farsi
     * yeh and keheh) are standardized. "Stemming" is accomplished via stopwords.
     * </p>
     */
	public sealed class PersianAnalyzer : StopwordAnalyzerBase
    {

        /*
         * File containing default Persian stopwords.
         * 
         * Default stopword list is from
         * http://members.unine.ch/jacques.savoy/clef/index.html The stopword list is
         * BSD-Licensed.
         * 
         */
        public readonly static String DEFAULT_STOPWORD_FILE = "stopwords.txt";

        /*
         * Contains the stopwords used with the StopFilter.
         */
        private readonly ISet<string> stoptable;

        /*
         * The comment character in the stopwords file. All lines prefixed with this
         * will be ignored
         */
        public static readonly String STOPWORDS_COMMENT = "#";

        /*
         * Returns an unmodifiable instance of the default stop-words set.
         * @return an unmodifiable instance of the default stop-words set.
         */
		public static CharArraySet GetDefaultStopSet()
        {
            return DefaultSetHolder.DEFAULT_STOP_SET;
        }

        /*
         * Atomically loads the DEFAULT_STOP_SET in a lazy fashion once the outer class 
         * accesses the static final set the first time.;
         */

	    private static class DefaultSetHolder
	    {
	        internal static readonly CharArraySet DEFAULT_STOP_SET;

	        static DefaultSetHolder()
	        {
	            try
	            {
	                DEFAULT_STOP_SET = LoadStopwordSet(false, typeof (PersianAnalyzer), DEFAULT_STOPWORD_FILE
	                    , STOPWORDS_COMMENT);
	            }
	            catch (IOException ex)
	            {
	                // default set should always be present as it is part of the
	                // distribution (JAR)
	                throw new Exception("Unable to load default stopword set");
	            }
	        }
	    }

	    /// <summary>
		/// Builds an analyzer with the default stop words:
		/// <see cref="DEFAULT_STOPWORD_FILE">DEFAULT_STOPWORD_FILE</see>
		/// .
		/// </summary>
        public PersianAnalyzer(Version matchVersion)
            : this(matchVersion, DefaultSetHolder.DEFAULT_STOP_SET)
        {

        }

        /*
         * Builds an analyzer with the given stop words 
         * 
         * @param matchVersion
         *          lucene compatibility version
         * @param stopwords
         *          a stopword set
         */
		protected internal PersianAnalyzer(Version matchVersion, CharArraySet stopwords) : 
			base(matchVersion, stopwords)
		{
		}

		/// <summary>
		/// Creates
		/// <see cref="Lucene.Net.Analysis.Analyzer.TokenStreamComponents">Lucene.Net.Analysis.Analyzer.TokenStreamComponents
		/// 	</see>
		/// used to tokenize all the text in the provided
		/// <see cref="System.IO.StreamReader">System.IO.StreamReader</see>
		/// .
		/// </summary>
		/// <returns>
		/// 
		/// <see cref="Lucene.Net.Analysis.Analyzer.TokenStreamComponents">Lucene.Net.Analysis.Analyzer.TokenStreamComponents
		/// 	</see>
		/// built from a
		/// <see cref="Lucene.Net.Analysis.Standard.StandardTokenizer">Lucene.Net.Analysis.Standard.StandardTokenizer
		/// 	</see>
		/// filtered with
		/// <see cref="Lucene.Net.Analysis.Core.LowerCaseFilter">Lucene.Net.Analysis.Core.LowerCaseFilter
		/// 	</see>
		/// ,
		/// <see cref="Lucene.Net.Analysis.AR.ArabicNormalizationFilter">Lucene.Net.Analysis.AR.ArabicNormalizationFilter
		/// 	</see>
		/// ,
		/// <see cref="PersianNormalizationFilter">PersianNormalizationFilter</see>
		/// and Persian Stop words
		/// </returns>
		public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
		{
			Tokenizer source;
			if (matchVersion.Value.OnOrAfter(Version.LUCENE_31))
			{
				source = new StandardTokenizer(matchVersion, reader);
			}
			else
			{
				source = new ArabicLetterTokenizer(matchVersion.Value, reader);
			}
			TokenStream result = new LowerCaseFilter(matchVersion, source);
			result = new ArabicNormalizationFilter(result);
			result = new PersianNormalizationFilter(result);
			return new Analyzer.TokenStreamComponents(source, new StopFilter(matchVersion, result
				, stopwords));
		}

        private class SavedStreams
        {
            protected internal Tokenizer source;
            protected internal TokenStream result;
        }

       
    }
}
