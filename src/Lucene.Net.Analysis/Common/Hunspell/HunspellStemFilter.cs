///* 
// * Licensed to the Apache Software Foundation (ASF) under one or more
// * contributor license agreements.  See the NOTICE file distributed with
// * this work for additional information regarding copyright ownership.
// * The ASF licenses this file to You under the Apache License, Version 2.0
// * (the "License"); you may not use this file except in compliance with
// * the License.  You may obtain a copy of the License at
// * 
// * http://www.apache.org/licenses/LICENSE-2.0
// * 
// * Unless required by applicable law or agreed to in writing, software
// * distributed under the License is distributed on an "AS IS" BASIS,
// * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// * See the License for the specific language governing permissions and
// * limitations under the License.
// */

//using System.Collections.Generic;
//using System.Linq;
//using Lucene.Net.Analysis;
//using Lucene.Net.Analysis.Hunspell;
//using Lucene.Net.Analysis.Tokenattributes;
//using Lucene.Net.Util;

//namespace Lucene.Analysis.Hunspell
//{
//    /// <summary>TokenFilter that uses hunspell affix rules and words to stem tokens.</summary>
//    /// <remarks>
//    /// TokenFilter that uses hunspell affix rules and words to stem tokens.  Since hunspell supports a word having multiple
//    /// stems, this filter can emit multiple tokens for each consumed token
//    /// <p>
//    /// Note: This filter is aware of the
//    /// <see cref="Lucene.Net.Analysis.Tokenattributes.KeywordAttribute">Lucene.Net.Analysis.Tokenattributes.KeywordAttribute
//    /// 	</see>
//    /// . To prevent
//    /// certain terms from being passed to the stemmer
//    /// <see cref="Lucene.Net.Analysis.Tokenattributes.KeywordAttribute.IsKeyword()
//    /// 	">Lucene.Net.Analysis.Tokenattributes.KeywordAttribute.IsKeyword()</see>
//    /// should be set to <code>true</code>
//    /// in a previous
//    /// <see cref="TokenStream">Lucene.Net.Analysis.TokenStream
//    /// 	</see>
//    /// .
//    /// Note: For including the original term as well as the stemmed version, see
//    /// <see cref="Lucene.Net.Analysis.Miscellaneous.KeywordRepeatFilterFactory">Lucene.Net.Analysis.Miscellaneous.KeywordRepeatFilterFactory
//    /// 	</see>
//    /// </p>
//    /// </remarks>
//    /// <lucene.experimental></lucene.experimental>
//    public sealed class HunspellStemFilter : TokenFilter
//    {
//        private readonly ICharTermAttribute termAtt;
//        private readonly IPositionIncrementAttribute posIncAtt;

//        private readonly IKeywordAttribute keywordAtt;

//        private readonly HunspellStemmer stemmer;

//        private IList<CharsRef> buffer;

//        private State savedState;

//        private readonly bool dedup;

//        private readonly bool longestOnly;

//        /// <summary>
//        /// Create a
//        /// <see cref="HunspellStemFilter">HunspellStemFilter</see>
//        /// outputting all possible stems.
//        /// </summary>
//        /// <seealso cref="HunspellStemFilter(Lucene.Net.Analysis.TokenStream, Dictionary, bool)
//        /// 	"></seealso>
//        public HunspellStemFilter(TokenStream input, HunspellDictionary dictionary)
//            : this(input,dictionary, true)
//        {
//        }

//        /// <summary>
//        /// Create a
//        /// <see cref="HunspellStemFilter">HunspellStemFilter</see>
//        /// outputting all possible stems.
//        /// </summary>
//        /// <seealso cref="HunspellStemFilter(Lucene.Net.Analysis.TokenStream, Dictionary, bool, bool)
//        /// 	"></seealso>
//        public HunspellStemFilter(TokenStream input, HunspellDictionary dictionary, bool dedup) :
//            this(input, dictionary, dedup, false)
//        {
//        }

//        /// <summary>
//        /// Creates a new HunspellStemFilter that will stem tokens from the given TokenStream using affix rules in the provided
//        /// Dictionary
//        /// </summary>
//        /// <param name="input">TokenStream whose tokens will be stemmed</param>
//        /// <param name="dictionary">HunspellDictionary containing the affix rules and words that will be used to stem the tokens
//        /// 	</param>
//        /// <param name="longestOnly">true if only the longest term should be output.</param>
//        public HunspellStemFilter(TokenStream input, HunspellDictionary dictionary, bool dedup, bool
//             longestOnly)
//            : base(input)
//        {
//            termAtt = AddAttribute<ICharTermAttribute>();
//            posIncAtt = AddAttribute<IPositionIncrementAttribute>();
//            keywordAtt = AddAttribute<IKeywordAttribute>();
//            this.dedup = dedup && longestOnly == false;
//            // don't waste time deduping if longestOnly is set
//            this.stemmer = new HunspellStemmer(dictionary);
//            this.longestOnly = longestOnly;
//        }

//        /// <exception cref="System.IO.IOException"></exception>
//        public override bool IncrementToken()
//        {
//            if (buffer != null && buffer.Any())
//            {
//                CharsRef nextStem = buffer[0];
//                buffer.RemoveAt(0);
//                RestoreState(savedState);
//                posIncAtt.PositionIncrement=0;
//                termAtt.SetEmpty().Append(nextStem);
//                return true;
//            }
//            if (!input.IncrementToken())
//            {
//                return false;
//            }
//            if (keywordAtt.IsKeyword)
//            {
//                return true;
//            }
//            buffer = dedup ? stemmer.UniqueStems(termAtt.Buffer, termAtt.Length) : stemmer.
//                Stem(termAtt.Buffer, termAtt.Length);
//            if (!buffer.Any())
//            {
//                // we do not know this word, return it unchanged
//                return true;
//            }
//            if (longestOnly && buffer.Count > 1)
//            {
//                buffer.Sort(lengthComparator);
//            }
//            CharsRef stem = buffer.Remove(0);
//            termAtt.SetEmpty().Append(stem);
//            if (longestOnly)
//            {
//                buffer.Clear();
//            }
//            else
//            {
//                if (!buffer.IsEmpty())
//                {
//                    savedState = CaptureState();
//                }
//            }
//            return true;
//        }

//        /// <exception cref="System.IO.IOException"></exception>
//        public override void Reset()
//        {
//            base.Reset();
//            buffer = null;
//        }

//        private sealed class _IComparer_136 : IComparer<CharsRef>
//        {
//            public _IComparer_136()
//            {
//            }

//            public int Compare(CharsRef o1, CharsRef o2)
//            {
//                if (o2.length == o1.length)
//                {
//                    // tie break on text
//                    return o2.CompareTo(o1);
//                }
//                else
//                {
//                    return o2.length < o1.length ? -1 : 1;
//                }
//            }
//        }

//        internal static readonly IComparer<CharsRef> lengthComparator = new _IComparer_136
//            ();
//    }
//}
