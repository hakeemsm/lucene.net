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

using System.IO;
using System.Collections;

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Util;

namespace Lucene.Net.Analysis.NGram
{

    /*
     * Tokenizes the input from an edge into n-grams of given size(s).
     * <p>
     * This <see cref="Tokenizer"/> create n-grams from the beginning edge or ending edge of a input token.
     * MaxGram can't be larger than 1024 because of limitation.
     * </p>
     */
    public sealed class EdgeNGramTokenizer : NGramTokenizer
    {
        public static Side DEFAULT_SIDE = Side.FRONT;
        public static int DEFAULT_MAX_GRAM_SIZE = 1;
        public static int DEFAULT_MIN_GRAM_SIZE = 1;

        private ICharTermAttribute termAtt;
        private IOffsetAttribute offsetAtt;

        /* Specifies which side of the input the n-gram should be generated from */
        // Moved Side enum from this class to external definition

        private int minGram;
        private int maxGram;
        private int gramSize;
        private Side side;
        private bool started = false;
        private int inLen;
        private string inStr;

        /// <summary>Creates EdgeNGramTokenizer that can generate n-grams in the sizes of the given range
        /// 	</summary>
        /// <param name="version">the <a href="#version">Lucene match version</a></param>
        /// <param name="input">
        /// 
        /// <see cref="System.IO.StreamReader">System.IO.StreamReader</see>
        /// holding the input to be tokenized
        /// </param>
        /// <param name="minGram">the smallest n-gram to generate</param>
        /// <param name="maxGram">the largest n-gram to generate</param>
        public EdgeNGramTokenizer(Version version, TextReader input, int minGram, int maxGram)
            : base(version, input, minGram, maxGram, true)
        {
        }
        /*
         * Creates EdgeNGramTokenizer that can generate n-grams in the sizes of the given range
         * 
         * <param name="factory"><see cref="AttributeSource.AttributeFactory"/> to use</param>
         * <param name="input"><see cref="TextReader"/> holding the input to be tokenized</param>
         * <param name="side">the <see cref="Side"/> from which to chop off an n-gram</param>
         * <param name="minGram">the smallest n-gram to generate</param>
         * <param name="maxGram">the largest n-gram to generate</param>
         */
        public EdgeNGramTokenizer(Version version, AttributeFactory factory, TextReader input, Side side, int minGram, int maxGram)
            : base(version,factory, input,minGram,maxGram,true)
        {

           
        }
    }
}