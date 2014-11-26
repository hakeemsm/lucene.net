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

using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Analysis.Compound
{

    /*
     * Base class for decomposition token filters.
     */

    public abstract class CompoundWordTokenFilterBase : TokenFilter
    {
        /*
         * The default for minimal word length that gets decomposed
         */
        public static readonly int DEFAULT_MIN_WORD_SIZE = 5;

        /*
         * The default for minimal length of subwords that get propagated to the output of this filter
         */
        public static readonly int DEFAULT_MIN_SUBWORD_SIZE = 2;

        /*
         * The default for maximal length of subwords that get propagated to the output of this filter
         */
        public static readonly int DEFAULT_MAX_SUBWORD_SIZE = 15;

        protected readonly CharArraySet dictionary;
        protected readonly LinkedList<CompoundToken> tokens;
        protected readonly int minWordSize;
        protected readonly int minSubwordSize;
        protected readonly int maxSubwordSize;
        protected readonly bool onlyLongestMatch;
        protected readonly Version matchVersion;

        protected ICharTermAttribute termAtt;
        private IOffsetAttribute offsetAtt;
        private IFlagsAttribute flagsAtt;
        private IPositionIncrementAttribute posIncAtt;
        private ITypeAttribute typeAtt;
        private IPayloadAttribute payloadAtt;
        private State current;

        private readonly Token wrapper = new Token();

        protected CompoundWordTokenFilterBase(Version matchVersion, TokenStream input, CharArraySet dictionary,
            bool onlyLongestMatch)
            : this(
                matchVersion, input, dictionary, DEFAULT_MIN_WORD_SIZE, DEFAULT_MIN_SUBWORD_SIZE,
                DEFAULT_MAX_SUBWORD_SIZE, onlyLongestMatch)
        {

        }

        protected CompoundWordTokenFilterBase(Version matchVersion, TokenStream input, CharArraySet dictionary)
            : this(
                matchVersion, input, dictionary, DEFAULT_MIN_WORD_SIZE, DEFAULT_MIN_SUBWORD_SIZE,
                DEFAULT_MAX_SUBWORD_SIZE, false)
        {

        }



        protected CompoundWordTokenFilterBase(Version matchVersion, TokenStream input, CharArraySet dictionary,
            int minWordSize, int minSubwordSize, int maxSubwordSize, bool onlyLongestMatch)
            : base(input)
        {
            termAtt = AddAttribute<ICharTermAttribute>();
            offsetAtt = AddAttribute<IOffsetAttribute>();
            posIncAtt = AddAttribute<IPositionIncrementAttribute>();
            this.matchVersion = matchVersion;
            this.tokens = new LinkedList<CompoundToken>();
            if (minWordSize < 0)
            {
                throw new ArgumentException("minWordSize cannot be negative");
            }
            this.minWordSize = minWordSize;
            if (minSubwordSize < 0)
            {
                throw new ArgumentException("minSubwordSize cannot be negative");
            }
            this.minSubwordSize = minSubwordSize;
            if (maxSubwordSize < 0)
            {
                throw new ArgumentException("maxSubwordSize cannot be negative");
            }
            this.maxSubwordSize = maxSubwordSize;
            this.onlyLongestMatch = onlyLongestMatch;
            this.dictionary = dictionary;
        }


        public override sealed bool IncrementToken()
        {
            if (tokens.Any())
            {
                CompoundToken token = tokens.First();
                tokens.RemoveFirst();
                RestoreState(current);
                // keep all other attributes untouched
                termAtt.SetEmpty().Append(token.txt);
                offsetAtt.SetOffset(token.StartOffset, token.EndOffset);
                posIncAtt.PositionIncrement = 0;
                return true;
            }
            current = null;
            // not really needed, but for safety
            if (input.IncrementToken())
            {
                // Only words longer than minWordSize get processed
                if (termAtt.Length >= this.minWordSize)
                {
                    Decompose();
                    // only capture the state if we really need it for producing new tokens
                    if (tokens.Any())
                    {
                        current = CaptureState();
                    }
                }
                // return original token:
                return true;
            }
            return false;
        }

       

        /// <summary>
        /// Decomposes the current
        /// <see cref="termAtt">termAtt</see>
        /// and places
        /// <see cref="CompoundToken">CompoundToken</see>
        /// instances in the
        /// <see cref="tokens">tokens</see>
        /// list.
        /// The original token may not be placed in the list, as it is automatically passed through this filter.
        /// </summary>
        protected abstract void Decompose();

       

        public override void Reset()
        {
            base.Reset();
            tokens.Clear();
            current = null;
        }


        /// <summary>Helper class to hold decompounded token information</summary>
        protected class CompoundToken
        {
            public readonly ICharSequence txt;

            public readonly int StartOffset;

            public readonly int EndOffset;

            /// <summary>C
            /// Construct the compound token based on a slice of the current
            /// <see cref="CompoundWordTokenFilterBase.termAtt">CompoundWordTokenFilterBase.termAtt
            /// 	</see>
            /// .
            /// </summary>
            public CompoundToken(CompoundWordTokenFilterBase _enclosing, int offset, int length)
            {
                this._enclosing = _enclosing;
                this.txt = this._enclosing.termAtt.SubSequence(offset, offset + length);
                // offsets of the original word
                int startOff = this._enclosing.offsetAtt.StartOffset;
                int endOff = this._enclosing.offsetAtt.EndOffset;
                if (_enclosing.matchVersion.OnOrAfter(Version.LUCENE_44) || endOff
                    - startOff != this._enclosing.termAtt.Length)
                {
                    // if length by start + end offsets doesn't match the term text then assume
                    // this is a synonym and don't adjust the offsets.
                    this.StartOffset = startOff;
                    this.EndOffset = endOff;
                }
                else
                {
                    int newStart = startOff + offset;
                    this.StartOffset = newStart;
                    this.EndOffset = newStart + length;
                }
            }

            private readonly CompoundWordTokenFilterBase _enclosing;
        }
    }
}