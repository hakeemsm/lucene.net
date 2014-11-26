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
using System.Text;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Analysis.Query
{
    /*
     * An {@link Analyzer} used primarily at query time to wrap another analyzer and provide a layer of protection
     * which prevents very common words from being passed into queries. 
     * <p>
     * For very large indexes the cost
     * of reading TermDocs for a very common word can be  high. This analyzer was created after experience with
     * a 38 million doc index which had a term in around 50% of docs and was causing TermQueries for 
     * this term to take 2 seconds.
     * </p>
     * <p>
     * Use the various "addStopWords" methods in this class to automate the identification and addition of 
     * stop words found in an already existing index.
     * </p>
     */
    public class QueryAutoStopWordAnalyzer : AnalyzerWrapper
    {
        Analyzer _delegate;
        HashMap<String, ICollection<String>> stopWordsPerField = new HashMap<String, ICollection<String>>();
        //The default maximum percentage (40%) of index documents which
        //can contain a term, after which the term is considered to be a stop word.
        public const float defaultMaxDocFreqPercent = 0.4f;
        private readonly Version matchVersion;

        /*
         * Initializes this analyzer with the Analyzer object that actually produces the tokens
         *
         * @param _delegate The choice of {@link Analyzer} that is used to produce the token stream which needs filtering
         */
        public QueryAutoStopWordAnalyzer(Version matchVersion, Analyzer _delegate, IndexReader
                   indexReader)
            : this(matchVersion, _delegate, indexReader, defaultMaxDocFreqPercent)
        {
        }
        /// <summary>
        /// Creates a new QueryAutoStopWordAnalyzer with stopwords calculated for all
        /// indexed fields from terms with a document frequency greater than the given
        /// maxDocFreq
        /// </summary>
        /// <param name="matchVersion">
        /// Version to be used in
        /// <see cref="Lucene.Net.Analysis.Core.StopFilter">Lucene.Net.Analysis.Core.StopFilter
        /// 	</see>
        /// </param>
        /// <param name="delegate_">Analyzer whose TokenStream will be filtered</param>
        /// <param name="indexReader">IndexReader to identify the stopwords from</param>
        /// <param name="maxDocFreq">Document frequency terms should be above in order to be stopwords
        /// 	</param>
        /// <exception cref="System.IO.IOException">Can be thrown while reading from the IndexReader
        /// 	</exception>
        public QueryAutoStopWordAnalyzer(Version matchVersion, Analyzer delegate_, IndexReader
             indexReader, int maxDocFreq)
            : this(matchVersion, delegate_, indexReader, MultiFields
                .GetIndexedFields(indexReader), maxDocFreq)
        {
        }

        /// <summary>
        /// Creates a new QueryAutoStopWordAnalyzer with stopwords calculated for all
        /// indexed fields from terms with a document frequency percentage greater than
        /// the given maxPercentDocs
        /// </summary>
        /// <param name="matchVersion">
        /// Version to be used in
        /// <see cref="Lucene.Net.Analysis.Core.StopFilter">Lucene.Net.Analysis.Core.StopFilter
        /// 	</see>
        /// </param>
        /// <param name="delegate_">Analyzer whose TokenStream will be filtered</param>
        /// <param name="indexReader">IndexReader to identify the stopwords from</param>
        /// <param name="maxPercentDocs">
        /// The maximum percentage (between 0.0 and 1.0) of index documents which
        /// contain a term, after which the word is considered to be a stop word
        /// </param>
        /// <exception cref="System.IO.IOException">Can be thrown while reading from the IndexReader
        /// 	</exception>
        public QueryAutoStopWordAnalyzer(Version matchVersion, Analyzer delegate_, IndexReader
             indexReader, float maxPercentDocs)
            : this(matchVersion, delegate_, indexReader,
                MultiFields.GetIndexedFields(indexReader), maxPercentDocs)
        {
        }

        /// <summary>
        /// Creates a new QueryAutoStopWordAnalyzer with stopwords calculated for the
        /// given selection of fields from terms with a document frequency percentage
        /// greater than the given maxPercentDocs
        /// </summary>
        /// <param name="matchVersion">
        /// Version to be used in
        /// <see cref="Lucene.Net.Analysis.Core.StopFilter">Lucene.Net.Analysis.Core.StopFilter
        /// 	</see>
        /// </param>
        /// <param name="delegate_">Analyzer whose TokenStream will be filtered</param>
        /// <param name="indexReader">IndexReader to identify the stopwords from</param>
        /// <param name="fields">Selection of fields to calculate stopwords for</param>
        /// <param name="maxPercentDocs">
        /// The maximum percentage (between 0.0 and 1.0) of index documents which
        /// contain a term, after which the word is considered to be a stop word
        /// </param>
        /// <exception cref="System.IO.IOException">Can be thrown while reading from the IndexReader
        /// 	</exception>
        public QueryAutoStopWordAnalyzer(Version matchVersion, Analyzer delegate_, IndexReader
             indexReader, ICollection<string> fields, float maxPercentDocs)
            : this(matchVersion
                , delegate_, indexReader, fields, (int)(indexReader.NumDocs * maxPercentDocs))
        {
        }

        /// <summary>
        /// Creates a new QueryAutoStopWordAnalyzer with stopwords calculated for the
        /// given selection of fields from terms with a document frequency greater than
        /// the given maxDocFreq
        /// </summary>
        /// <param name="matchVersion">
        /// Version to be used in
        /// <see cref="Lucene.Net.Analysis.Core.StopFilter">Lucene.Net.Analysis.Core.StopFilter
        /// 	</see>
        /// </param>
        /// <param name="delegate_">Analyzer whose TokenStream will be filtered</param>
        /// <param name="indexReader">IndexReader to identify the stopwords from</param>
        /// <param name="fields">Selection of fields to calculate stopwords for</param>
        /// <param name="maxDocFreq">Document frequency terms should be above in order to be stopwords
        /// 	</param>
        /// <exception cref="System.IO.IOException">Can be thrown while reading from the IndexReader
        /// 	</exception>
        public QueryAutoStopWordAnalyzer(Version matchVersion, Analyzer delegate_, IndexReader
             indexReader, ICollection<string> fields, int maxDocFreq)
            : base(delegate_.GetReuseStrategy())
        {
            //The default maximum percentage (40%) of index documents which
            //can contain a term, after which the term is considered to be a stop word.
            this.matchVersion = matchVersion;
            this._delegate = delegate_;
            foreach (string field in fields)
            {
                ISet<string> stopWords = new HashSet<string>();
                Terms terms = MultiFields.GetTerms(indexReader, field);
                CharsRef spare = new CharsRef();
                if (terms != null)
                {
                    TermsEnum te = terms.Iterator(null);
                    BytesRef text;
                    while ((text = te.Next()) != null)
                    {
                        if (te.DocFreq > maxDocFreq)
                        {
                            UnicodeUtil.UTF8toUTF16(text, spare);
                            stopWords.Add(spare.ToString());
                        }
                    }
                }
                stopWordsPerField[field] = stopWords;
            }
        }

        protected override Analyzer GetWrappedAnalyzer(string fieldName)
        {
            return _delegate;
        }


        protected override TokenStreamComponents WrapComponents(string fieldName, TokenStreamComponents components)
        {
            var stopWords = stopWordsPerField[fieldName] as ICollection<object>;
            if (stopWords == null)
            {
                return components;
            }
            StopFilter stopFilter = new StopFilter(matchVersion, components.TokenStream,
                new CharArraySet(matchVersion, stopWords, false));
            return new TokenStreamComponents(components.Tokenizer, stopFilter);
        }

        /*
         * Provides information on which stop words have been identified for a field
         *
         * @param fieldName The field for which stop words identified in "addStopWords"
         *                  method calls will be returned
         * @return the stop words identified for a field
         */
        public String[] GetStopWords(String fieldName)
        {
            String[] result;
            var stopWords = stopWordsPerField[fieldName];
            if (stopWords != null)
            {
                result = stopWords.ToArray();
            }
            else
            {
                result = new String[0];
            }
            return result;
        }

        /*
         * Provides information on which stop words have been identified for all fields
         *
         * @return the stop words (as terms)
         */
        public Term[] GetStopWords()
        {
            List<Term> allStopWords = new List<Term>();
            foreach (var fieldName in stopWordsPerField.Keys)
            {
                var stopWords = stopWordsPerField[fieldName];
                foreach (var text in stopWords)
                {
                    allStopWords.Add(new Term(fieldName, text));
                }
            }
            return allStopWords.ToArray();
        }

    }
}
