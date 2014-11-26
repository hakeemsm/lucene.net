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
using System.IO;
using System.Collections;

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Analysis.NGram
{

    /*
     * Tokenizes the input into n-grams of the given size(s).
     */
    public class NGramTokenizer : Tokenizer
    {
        public static int DEFAULT_MIN_NGRAM_SIZE = 1;
        public static int DEFAULT_MAX_NGRAM_SIZE = 2;

        private CharacterUtils charUtils;

        private CharacterUtils.CharacterBuffer charBuffer;

        private int[] buffer;
        private int minGram, maxGram;
        private int bufferStart;

        private int bufferEnd;

        private int offset;
        private int gramSize;
        private int pos = 0;
        private int inLen;
        private bool exhausted;

        private int lastCheckedChar;
        private int lastNonTokenChar;

        private bool edgesOnly;

        private ICharTermAttribute termAtt;
        private IOffsetAttribute offsetAtt;
        private IPositionIncrementAttribute posIncAtt;
        private IPositionLengthAttribute posLenAtt;

        /*
         * Creates NGramTokenizer with given min and max n-grams.
         * <param name="input"><see cref="TextReader"/> holding the input to be tokenized</param>
         * <param name="minGram">the smallest n-gram to generate</param>
         * <param name="maxGram">the largest n-gram to generate</param>
         */
        public NGramTokenizer(Version version, TextReader input, int minGram, int maxGram, bool edgesOnly)
            : base(input)
        {
            Init(version, minGram, maxGram, edgesOnly);
        }

        private void Init(Version version, int minGram, int maxGram, bool edgesOnly)
        {
            if (!version.OnOrAfter(Version.LUCENE_44))
            {
                throw new ArgumentException("This class only works with Lucene 4.4+. To emulate the old (broken) behavior of NGramTokenizer, use Lucene43NGramTokenizer/Lucene43EdgeNGramTokenizer"
                    );
            }
            charUtils = version.OnOrAfter( Version.LUCENE_44) ? CharacterUtils.
                GetInstance(version) : CharacterUtils.GetInstance();
            if (minGram < 1)
            {
                throw new ArgumentException("minGram must be greater than zero");
            }
            if (minGram > maxGram)
            {
                throw new ArgumentException("minGram must not be greater than maxGram");
            }
            this.minGram = minGram;
            this.maxGram = maxGram;
            this.edgesOnly = edgesOnly;
            charBuffer = CharacterUtils.NewCharacterBuffer(2 * maxGram + 1024);
            // 2 * maxGram in case all code points require 2 chars and + 1024 for buffering to not keep polling the Reader
            buffer = new int[charBuffer.Buffer.Length];
            // Make the term att large enough
            termAtt.ResizeBuffer(2 * maxGram);
        }

        /*
         * Creates NGramTokenizer with given min and max n-grams.
         * <param name="version"><see cref="Lucene.Net.Util.Version"/> to use</param>
         * <param name="input"><see cref="TextReader"/> holding the input to be tokenized</param>
         * <param name="minGram">the smallest n-gram to generate</param>
         * <param name="maxGram">the largest n-gram to generate</param>
         */
        public NGramTokenizer(Version version, TextReader input, int minGram, int maxGram)
            : this(version, input, minGram,maxGram,false)
        {
            
        }

        /*
         * Creates NGramTokenizer with given min and max n-grams.
         * <param name="version"><see cref="Lucene.Net.Util.Version"/> to use</param>
         * <param name="factory"><see cref="AttributeSource.AttributeFactory"/> to use</param>
         * <param name="input"><see cref="TextReader"/> holding the input to be tokenized</param>
         * <param name="minGram">the smallest n-gram to generate</param>
         * <param name="maxGram">the largest n-gram to generate</param>
         */
        public NGramTokenizer(Version version, AttributeFactory factory, TextReader input, int minGram, int maxGram, bool edgesOnly)
            : base(factory, input)
        {
            Init(version,minGram, maxGram,edgesOnly);
        }

        /*
         * Creates NGramTokenizer with given min and max n-grams.
         * <param name="version"><see cref="Lucene.Net.Util.Version"/> to use</param>
         * <param name="factory"><see cref="AttributeSource.AttributeFactory"/> to use</param>
         * <param name="input"><see cref="TextReader"/> holding the input to be tokenized</param>
         * <param name="minGram">the smallest n-gram to generate</param>
         * <param name="maxGram">the largest n-gram to generate</param>
         */
        public NGramTokenizer(Version version, AttributeFactory factory, TextReader input, int minGram, int maxGram)
            : this(version,factory, input,minGram,maxGram,false)
        {
            
        }

        /*
         * Creates NGramTokenizer with default min and max n-grams.
         * <param name="input"><see cref="TextReader"/> holding the input to be tokenized</param>
         */
        public NGramTokenizer(Version version, TextReader input)
            : this(version,input, DEFAULT_MIN_NGRAM_SIZE, DEFAULT_MAX_NGRAM_SIZE)
        {

        }

        /* Returns the next token in the stream, or null at EOS. */
        public override bool IncrementToken()
        {
            ClearAttributes();
			while (true)
			{
				// compact
				if (bufferStart >= bufferEnd - maxGram - 1 && !exhausted)
				{
					System.Array.Copy(buffer, bufferStart, buffer, 0, bufferEnd - bufferStart);
					bufferEnd -= bufferStart;
					lastCheckedChar -= bufferStart;
					lastNonTokenChar -= bufferStart;
					bufferStart = 0;
					// fill in remaining space
					exhausted = !charUtils.Fill(charBuffer, input, buffer.Length - bufferEnd);
					// convert to code points
					bufferEnd += charUtils.ToCodePoints(charBuffer.Buffer, 0, charBuffer.Length, buffer, bufferEnd);
				}
				// should we go to the next offset?
				if (gramSize > maxGram || (bufferStart + gramSize) > bufferEnd)
				{
					if (bufferStart + 1 + minGram > bufferEnd)
					{
						//HM:revisit 
						//assert exhausted;
						return false;
					}
					Consume();
					gramSize = minGram;
				}
				UpdateLastNonTokenChar();
				// retry if the token to be emitted was going to not only contain token chars
				bool termContainsNonTokenChar = lastNonTokenChar >= bufferStart && lastNonTokenChar
					 < (bufferStart + gramSize);
				bool isEdgeAndPreviousCharIsTokenChar = edgesOnly && lastNonTokenChar != bufferStart
					 - 1;
				if (termContainsNonTokenChar || isEdgeAndPreviousCharIsTokenChar)
				{
					Consume();
					gramSize = minGram;
					continue;
				}
				int length = charUtils.ToChars(buffer, bufferStart, gramSize, termAtt.Buffer, 0
					);
				termAtt.SetLength(length);
				posIncAtt.PositionIncrement = 1;
				posLenAtt.PositionLength = 1;
				offsetAtt.SetOffset(CorrectOffset(offset), CorrectOffset(offset + length));
				++gramSize;
				return true;
			}
		}

		private void UpdateLastNonTokenChar()
		{
			int termEnd = bufferStart + gramSize - 1;
			if (termEnd > lastCheckedChar)
			{
				for (int i = termEnd; i > lastCheckedChar; --i)
				{
					if (!IsTokenChar(buffer[i]))
					{
						lastNonTokenChar = i;
						break;
					}
				}
				lastCheckedChar = termEnd;
			}
		}
		/// <summary>Consume one code point.</summary>
		/// <remarks>Consume one code point.</remarks>
		private void Consume()
		{
			offset += Character.CharCount(buffer[bufferStart++]);
		}

		/// <summary>Only collect characters which satisfy this condition.</summary>
		/// <remarks>Only collect characters which satisfy this condition.</remarks>
		protected virtual bool IsTokenChar(int chr)
		{
			return true;
		}
        public override void End()
        {
			base.End();
			//HM:revisit 
			//assert bufferStart <= bufferEnd;
			int endOffset = offset;
			for (int i = bufferStart; i < bufferEnd; ++i)
			{
				endOffset += Character.CharCount(buffer[i]);
			}
			endOffset = CorrectOffset(endOffset);
			// set final offset
			offsetAtt.SetOffset(endOffset, endOffset);
        }

		/// <exception cref="System.IO.IOException"></exception>
        public override void Reset()
        {
			base.Reset();
			bufferStart = bufferEnd = buffer.Length;
			lastNonTokenChar = lastCheckedChar = bufferStart - 1;
			offset = 0;
			gramSize = minGram;
			exhausted = false;
			charBuffer.Reset();
        }
    }
}