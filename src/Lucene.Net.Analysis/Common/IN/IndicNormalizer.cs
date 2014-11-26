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
using System.Collections;
using Lucene.Analysis.Util;
using Lucene.Net.Support;

namespace Lucene.Net.Analysis.IN
{
    /// <summary>Normalizes the Unicode representation of text in Indian languages.</summary>
    /// <remarks>
    /// Normalizes the Unicode representation of text in Indian languages.
    /// <p>
    /// Follows guidelines from Unicode 5.2, chapter 6, South Asian Scripts I
    /// and graphical decompositions from http://ldc.upenn.edu/myl/IndianScriptsUnicode.html
    /// </p>
    /// </remarks>
    public class IndicNormalizer
    {
        private class ScriptData
        {
            internal readonly int Flag;

            internal readonly int Base;

            internal BitArray DecompMask;

            internal ScriptData(int flag, int @base)
            {
                this.Flag = flag;
                this.Base = @base;
            }
        }

        private static readonly IdentityHashMap<Character.UnicodeBlock, ScriptData> Scripts = new IdentityHashMap<Character.UnicodeBlock, ScriptData>();

        private static int Flag(Character.UnicodeBlock ub)
        {
            return Scripts[ub].Flag;
        }

      

        /// <summary>
        /// Decompositions according to Unicode 5.2,
        /// and http://ldc.upenn.edu/myl/IndianScriptsUnicode.html
        /// Most of these are not handled by unicode normalization anyway.
        /// </summary>
        /// <remarks>
        /// Decompositions according to Unicode 5.2,
        /// and http://ldc.upenn.edu/myl/IndianScriptsUnicode.html
        /// Most of these are not handled by unicode normalization anyway.
        /// The numbers here represent offsets into the respective codepages,
        /// with -1 representing null and 0xFF representing zero-width joiner.
        /// the columns are: ch1, ch2, ch3, res, flags
        /// ch1, ch2, and ch3 are the decomposition
        /// res is the composition, and flags are the scripts to which it applies.
        /// </remarks>
        private static readonly int[][] decompositions = new[]
		{ new[] { unchecked(
			(int)(0x05)), unchecked((int)(0x3E)), unchecked((int)(0x45)), unchecked((int)(0x11
			)), Flag(Character.UnicodeBlock.DEVANAGARI) | Flag(Character.UnicodeBlock.GUJARATI) }, new int[] { unchecked((int)(0x05)), unchecked(
			(int)(0x3E)), unchecked((int)(0x46)), unchecked((int)(0x12)), Flag(Character.UnicodeBlock.DEVANAGARI) }
			, new int[] { unchecked((int)(0x05)), unchecked((int)(0x3E)), unchecked((int)(0x47
			)), unchecked((int)(0x13)), Flag(Character.UnicodeBlock.DEVANAGARI) | Flag(Character.UnicodeBlock.GUJARATI) }, new int[] { unchecked(
			(int)(0x05)), unchecked((int)(0x3E)), unchecked((int)(0x48)), unchecked((int)(0x14
			)), Flag(Character.UnicodeBlock.DEVANAGARI) | Flag(Character.UnicodeBlock.GUJARATI) }, new int[] { unchecked((int)(0x05)), unchecked(
			(int)(0x3E)), -1, unchecked((int)(0x06)), Flag(Character.UnicodeBlock.DEVANAGARI) | Flag(Character.UnicodeBlock.BENGALI) | Flag
			(Character.UnicodeBlock.GURMUKHI) | Flag(Character.UnicodeBlock.GUJARATI) | Flag(Character.UnicodeBlock.ORIYA) }, new int[] { unchecked((int)(0x05)), 
			unchecked((int)(0x45)), -1, unchecked((int)(0x72)), Flag(Character.UnicodeBlock.DEVANAGARI) }, new int[
			] { unchecked((int)(0x05)), unchecked((int)(0x45)), -1, unchecked((int)(0x0D)), 
			Flag(Character.UnicodeBlock.GUJARATI) }, new int[] { unchecked((int)(0x05)), unchecked((int)(0x46)), -1
			, unchecked((int)(0x04)), Flag(Character.UnicodeBlock.DEVANAGARI) }, new int[] { unchecked((int)(0x05))
			, unchecked((int)(0x47)), -1, unchecked((int)(0x0F)), Flag(Character.UnicodeBlock.GUJARATI) }, new int[
			] { unchecked((int)(0x05)), unchecked((int)(0x48)), -1, unchecked((int)(0x10)), 
			Flag(Character.UnicodeBlock.GURMUKHI) | Flag(Character.UnicodeBlock.GUJARATI) }, new int[] { unchecked((int)(0x05)), unchecked(
			(int)(0x49)), -1, unchecked((int)(0x11)), Flag(Character.UnicodeBlock.DEVANAGARI) | Flag(Character.UnicodeBlock.GUJARATI) }, new 
			int[] { unchecked((int)(0x05)), unchecked((int)(0x4A)), -1, unchecked((int)(0x12
			)), Flag(Character.UnicodeBlock.DEVANAGARI) }, new int[] { unchecked((int)(0x05)), unchecked((int)(0x4B
			)), -1, unchecked((int)(0x13)), Flag(Character.UnicodeBlock.DEVANAGARI) | Flag(Character.UnicodeBlock.GUJARATI) }, new int[] { 
			unchecked((int)(0x05)), unchecked((int)(0x4C)), -1, unchecked((int)(0x14)), Flag
			(Character.UnicodeBlock.DEVANAGARI) | Flag(Character.UnicodeBlock.GURMUKHI) | Flag(Character.UnicodeBlock.GUJARATI) }, new int[] { unchecked((int)(0x06
			)), unchecked((int)(0x45)), -1, unchecked((int)(0x11)), Flag(Character.UnicodeBlock.DEVANAGARI) | Flag(
			Character.UnicodeBlock.GUJARATI) }, new int[] { unchecked((int)(0x06)), unchecked((int)(0x46)), -1, unchecked(
			(int)(0x12)), Flag(Character.UnicodeBlock.DEVANAGARI) }, new int[] { unchecked((int)(0x06)), unchecked(
			(int)(0x47)), -1, unchecked((int)(0x13)), Flag(Character.UnicodeBlock.DEVANAGARI) | Flag(Character.UnicodeBlock.GUJARATI) }, new 
			int[] { unchecked((int)(0x06)), unchecked((int)(0x48)), -1, unchecked((int)(0x14
			)), Flag(Character.UnicodeBlock.DEVANAGARI) | Flag(Character.UnicodeBlock.GUJARATI) }, new int[] { unchecked((int)(0x07)), unchecked(
			(int)(0x57)), -1, unchecked((int)(0x08)), Flag(Character.UnicodeBlock.MALAYALAM) }, new int[] { unchecked(
			(int)(0x09)), unchecked((int)(0x41)), -1, unchecked((int)(0x0A)), Flag(Character.UnicodeBlock.DEVANAGARI
			) }, new int[] { unchecked((int)(0x09)), unchecked((int)(0x57)), -1, unchecked((
			int)(0x0A)), Flag(Character.UnicodeBlock.TAMIL) | Flag(Character.UnicodeBlock.MALAYALAM) }, new int[] { unchecked((int)(0x0E))
			, unchecked((int)(0x46)), -1, unchecked((int)(0x10)), Flag(Character.UnicodeBlock.MALAYALAM) }, new int
			[] { unchecked((int)(0x0F)), unchecked((int)(0x45)), -1, unchecked((int)(0x0D)), 
			Flag(Character.UnicodeBlock.DEVANAGARI) }, new int[] { unchecked((int)(0x0F)), unchecked((int)(0x46)), 
			-1, unchecked((int)(0x0E)), Flag(Character.UnicodeBlock.DEVANAGARI) }, new int[] { unchecked((int)(0x0F
			)), unchecked((int)(0x47)), -1, unchecked((int)(0x10)), Flag(Character.UnicodeBlock.DEVANAGARI) }, new 
			int[] { unchecked((int)(0x0F)), unchecked((int)(0x57)), -1, unchecked((int)(0x10
			)), Flag(Character.UnicodeBlock.ORIYA) }, new int[] { unchecked((int)(0x12)), unchecked((int)(0x3E)), -
			1, unchecked((int)(0x13)), Flag(Character.UnicodeBlock.MALAYALAM) }, new int[] { unchecked((int)(0x12))
			, unchecked((int)(0x4C)), -1, unchecked((int)(0x14)), Flag(Character.UnicodeBlock.TELUGU) | Flag(Character.UnicodeBlock.KANNADA
			) }, new int[] { unchecked((int)(0x12)), unchecked((int)(0x55)), -1, unchecked((
			int)(0x13)), Flag(Character.UnicodeBlock.TELUGU) }, new int[] { unchecked((int)(0x12)), unchecked((int)
			(0x57)), -1, unchecked((int)(0x14)), Flag(Character.UnicodeBlock.TAMIL) | Flag(Character.UnicodeBlock.MALAYALAM) }, new int[] 
			{ unchecked((int)(0x13)), unchecked((int)(0x57)), -1, unchecked((int)(0x14)), Flag
			(Character.UnicodeBlock.ORIYA) }, new int[] { unchecked((int)(0x15)), unchecked((int)(0x3C)), -1, unchecked(
			(int)(0x58)), Flag(Character.UnicodeBlock.DEVANAGARI) }, new int[] { unchecked((int)(0x16)), unchecked(
			(int)(0x3C)), -1, unchecked((int)(0x59)), Flag(Character.UnicodeBlock.DEVANAGARI) | Flag(Character.UnicodeBlock.GURMUKHI) }, new 
			int[] { unchecked((int)(0x17)), unchecked((int)(0x3C)), -1, unchecked((int)(0x5A
			)), Flag(Character.UnicodeBlock.DEVANAGARI) | Flag(Character.UnicodeBlock.GURMUKHI) }, new int[] { unchecked((int)(0x1C)), unchecked(
			(int)(0x3C)), -1, unchecked((int)(0x5B)), Flag(Character.UnicodeBlock.DEVANAGARI) | Flag(Character.UnicodeBlock.GURMUKHI) }, new 
			int[] { unchecked((int)(0x21)), unchecked((int)(0x3C)), -1, unchecked((int)(0x5C
			)), Flag(Character.UnicodeBlock.DEVANAGARI) | Flag(Character.UnicodeBlock.BENGALI) | Flag(Character.UnicodeBlock.ORIYA) }, new int[] { unchecked((int
			)(0x22)), unchecked((int)(0x3C)), -1, unchecked((int)(0x5D)), Flag(Character.UnicodeBlock.DEVANAGARI) |
			 Flag(Character.UnicodeBlock.BENGALI) | Flag(Character.UnicodeBlock.ORIYA) }, new int[] { unchecked((int)(0x23)), unchecked((int
			)(0x4D)), unchecked((int)(0xFF)), unchecked((int)(0x7A)), Flag(Character.UnicodeBlock.MALAYALAM) }, new 
			int[] { unchecked((int)(0x24)), unchecked((int)(0x4D)), unchecked((int)(0xFF)), 
			unchecked((int)(0x4E)), Flag(Character.UnicodeBlock.BENGALI) }, new int[] { unchecked((int)(0x28)), unchecked(
			(int)(0x3C)), -1, unchecked((int)(0x29)), Flag(Character.UnicodeBlock.DEVANAGARI) }, new int[] { unchecked(
			(int)(0x28)), unchecked((int)(0x4D)), unchecked((int)(0xFF)), unchecked((int)(0x7B
			)), Flag(Character.UnicodeBlock.MALAYALAM) }, new int[] { unchecked((int)(0x2B)), unchecked((int)(0x3C)
			), -1, unchecked((int)(0x5E)), Flag(Character.UnicodeBlock.DEVANAGARI) | Flag(Character.UnicodeBlock.GURMUKHI) }, new int[] { 
			unchecked((int)(0x2F)), unchecked((int)(0x3C)), -1, unchecked((int)(0x5F)), Flag
			(Character.UnicodeBlock.DEVANAGARI) | Flag(Character.UnicodeBlock.BENGALI) }, new int[] { unchecked((int)(0x2C)), unchecked((int
			)(0x41)), unchecked((int)(0x41)), unchecked((int)(0x0B)), Flag(Character.UnicodeBlock.TELUGU) }, new int
			[] { unchecked((int)(0x30)), unchecked((int)(0x3C)), -1, unchecked((int)(0x31)), 
			Flag(Character.UnicodeBlock.DEVANAGARI) }, new int[] { unchecked((int)(0x30)), unchecked((int)(0x4D)), 
			unchecked((int)(0xFF)), unchecked((int)(0x7C)), Flag(Character.UnicodeBlock.MALAYALAM) }, new int[] { unchecked(
			(int)(0x32)), unchecked((int)(0x4D)), unchecked((int)(0xFF)), unchecked((int)(0x7D
			)), Flag(Character.UnicodeBlock.MALAYALAM) }, new int[] { unchecked((int)(0x33)), unchecked((int)(0x3C)
			), -1, unchecked((int)(0x34)), Flag(Character.UnicodeBlock.DEVANAGARI) }, new int[] { unchecked((int)(0x33
			)), unchecked((int)(0x4D)), unchecked((int)(0xFF)), unchecked((int)(0x7E)), Flag
			(Character.UnicodeBlock.MALAYALAM) }, new int[] { unchecked((int)(0x35)), unchecked((int)(0x41)), -1, unchecked(
			(int)(0x2E)), Flag(Character.UnicodeBlock.TELUGU) }, new int[] { unchecked((int)(0x3E)), unchecked((int
			)(0x45)), -1, unchecked((int)(0x49)), Flag(Character.UnicodeBlock.DEVANAGARI) | Flag(Character.UnicodeBlock.GUJARATI) }, new int
			[] { unchecked((int)(0x3E)), unchecked((int)(0x46)), -1, unchecked((int)(0x4A)), 
			Flag(Character.UnicodeBlock.DEVANAGARI) }, new int[] { unchecked((int)(0x3E)), unchecked((int)(0x47)), 
			-1, unchecked((int)(0x4B)), Flag(Character.UnicodeBlock.DEVANAGARI) | Flag(Character.UnicodeBlock.GUJARATI) }, new int[] { unchecked(
			(int)(0x3E)), unchecked((int)(0x48)), -1, unchecked((int)(0x4C)), Flag(Character.UnicodeBlock.DEVANAGARI
			) | Flag(Character.UnicodeBlock.GUJARATI) }, new int[] { unchecked((int)(0x3F)), unchecked((int)(0x55))
			, -1, unchecked((int)(0x40)), Flag(Character.UnicodeBlock.KANNADA) }, new int[] { unchecked((int)(0x41)
			), unchecked((int)(0x41)), -1, unchecked((int)(0x42)), Flag(Character.UnicodeBlock.GURMUKHI) }, new int
			[] { unchecked((int)(0x46)), unchecked((int)(0x3E)), -1, unchecked((int)(0x4A)), 
			Flag(Character.UnicodeBlock.TAMIL) | Flag(Character.UnicodeBlock.MALAYALAM) }, new int[] { unchecked((int)(0x46)), unchecked((
			int)(0x42)), unchecked((int)(0x55)), unchecked((int)(0x4B)), Flag(Character.UnicodeBlock.KANNADA) }, new 
			int[] { unchecked((int)(0x46)), unchecked((int)(0x42)), -1, unchecked((int)(0x4A
			)), Flag(Character.UnicodeBlock.KANNADA) }, new int[] { unchecked((int)(0x46)), unchecked((int)(0x46)), 
			-1, unchecked((int)(0x48)), Flag(Character.UnicodeBlock.MALAYALAM) }, new int[] { unchecked((int)(0x46)
			), unchecked((int)(0x55)), -1, unchecked((int)(0x47)), Flag(Character.UnicodeBlock.TELUGU) | Flag(Character.UnicodeBlock.KANNADA
			) }, new int[] { unchecked((int)(0x46)), unchecked((int)(0x56)), -1, unchecked((
			int)(0x48)), Flag(Character.UnicodeBlock.TELUGU) | Flag(Character.UnicodeBlock.KANNADA) }, new int[] { unchecked((int)(0x46)), 
			unchecked((int)(0x57)), -1, unchecked((int)(0x4C)), Flag(Character.UnicodeBlock.TAMIL) | Flag(Character.UnicodeBlock.MALAYALAM
			) }, new int[] { unchecked((int)(0x47)), unchecked((int)(0x3E)), -1, unchecked((
			int)(0x4B)), Flag(Character.UnicodeBlock.BENGALI) | Flag(Character.UnicodeBlock.ORIYA) | Flag(Character.UnicodeBlock.TAMIL) | Flag(Character.UnicodeBlock.MALAYALAM) }, new 
			int[] { unchecked((int)(0x47)), unchecked((int)(0x57)), -1, unchecked((int)(0x4C
			)), Flag(Character.UnicodeBlock.BENGALI) | Flag(Character.UnicodeBlock.ORIYA) }, new int[] { unchecked((int)(0x4A)), unchecked(
			(int)(0x55)), -1, unchecked((int)(0x4B)), Flag(Character.UnicodeBlock.KANNADA) }, new int[] { unchecked(
			(int)(0x72)), unchecked((int)(0x3F)), -1, unchecked((int)(0x07)), Flag(Character.UnicodeBlock.GURMUKHI)
			 }, new int[] { unchecked((int)(0x72)), unchecked((int)(0x40)), -1, unchecked((int
			)(0x08)), Flag(Character.UnicodeBlock.GURMUKHI) }, new int[] { unchecked((int)(0x72)), unchecked((int)(
			0x47)), -1, unchecked((int)(0x0F)), Flag(Character.UnicodeBlock.GURMUKHI) }, new int[] { unchecked((int
			)(0x73)), unchecked((int)(0x41)), -1, unchecked((int)(0x09)), Flag(Character.UnicodeBlock.GURMUKHI) }, 
			new int[] { unchecked((int)(0x73)), unchecked((int)(0x42)), -1, unchecked((int)(
			0x0A)), Flag(Character.UnicodeBlock.GURMUKHI) }, new int[] { unchecked((int)(0x73)), unchecked((int)(0x4B
			)), -1, unchecked((int)(0x13)), Flag(Character.UnicodeBlock.GURMUKHI) } };

        static IndicNormalizer()
        {
            Scripts[Character.UnicodeBlock.DEVANAGARI] = new ScriptData(1, unchecked((int)(0x0900)));
            Scripts[Character.UnicodeBlock.BENGALI] = new ScriptData(2, unchecked((int)(0x0980)));
            Scripts[Character.UnicodeBlock.GURMUKHI] = new ScriptData(4, unchecked((int)(0x0A00)));
            Scripts[Character.UnicodeBlock.GUJARATI] = new ScriptData(8, unchecked((int)(0x0A80)));
            Scripts[Character.UnicodeBlock.ORIYA] = new ScriptData(16, unchecked((int)(0x0B00)));
            Scripts[Character.UnicodeBlock.TAMIL] = new ScriptData(32, unchecked((int)(0x0B80)));
            Scripts[Character.UnicodeBlock.TELUGU] = new ScriptData(64, unchecked((int)(0x0C00)));
            Scripts[Character.UnicodeBlock.KANNADA] = new ScriptData(128, unchecked((int)(0x0C80)));
            Scripts[Character.UnicodeBlock.MALAYALAM] = new ScriptData(256, unchecked((int)(0x0D00)));
            foreach (ScriptData sd in Scripts.Values)
            {
                sd.DecompMask = new BitArray(unchecked((int)(0x7F)));
                for (int i = 0; i < decompositions.Length; i++)
                {
                    int ch = decompositions[i][0];
                    int flags = decompositions[i][4];
                    if ((flags & sd.Flag) != 0)
                    {
                        sd.DecompMask.Set(ch);
                    }
                }
            }
        }

        /// <summary>Normalizes input text, and returns the new length.</summary>
        /// <remarks>
        /// Normalizes input text, and returns the new length.
        /// The length will always be less than or equal to the existing length.
        /// </remarks>
        /// <param name="text">input text</param>
        /// <param name="len">valid length</param>
        /// <returns>normalized length</returns>
        public virtual int Normalize(char[] text, int len)
        {
            for (int i = 0; i < len; i++)
            {
                Character.UnicodeBlock ub = Character.UnicodeBlock.UNDEFINED;
                Character.UnicodeBlock block = ub.Of(text[i]);
                var sd = Scripts[block];
                if (sd != null)
                {
                    int ch = text[i] - sd.Base;
                    if (sd.DecompMask.Get(ch))
                    {
                        len = Compose(ch, block, sd, text, i, len);
                    }
                }
            }
            return len;
        }

        /// <summary>Compose into standard form any compositions in the decompositions table.
        /// 	</summary>
        /// <remarks>Compose into standard form any compositions in the decompositions table.
        /// 	</remarks>
        private int Compose(int ch0, Character.UnicodeBlock block0, ScriptData sd, char[] text, int pos, int len)
        {
            if (pos + 1 >= len)
            {
                return len;
            }
            int ch1 = text[pos + 1] - sd.Base;
            Character.UnicodeBlock ub=Character.UnicodeBlock.UNDEFINED;
            Character.UnicodeBlock block1 = ub.Of(text[pos + 1]);
            if (block1 != block0)
            {
                return len;
            }
            int ch2 = -1;
            if (pos + 2 < len)
            {
                ch2 = text[pos + 2] - sd.Base;
                Character.UnicodeBlock block2 = ub.Of(text[pos + 2]);
                if (text[pos + 2] == '\u200D')
                {
                    // ZWJ
                    ch2 = unchecked((int)(0xFF));
                }
                else
                {
                    if (block2 != block1)
                    {
                        // still allow a 2-char match
                        ch2 = -1;
                    }
                }
            }
            for (int i = 0; i < decompositions.Length; i++)
            {
                if (decompositions[i][0] == ch0 && (decompositions[i][4] & sd.Flag) != 0)
                {
                    if (decompositions[i][1] == ch1 && (decompositions[i][2] < 0 || decompositions[i]
                        [2] == ch2))
                    {
                        text[pos] = (char)(sd.Base + decompositions[i][3]);
                        len = StemmerUtil.Delete(text, pos + 1, len);
                        if (decompositions[i][2] >= 0)
                        {
                            len = StemmerUtil.Delete(text, pos + 1, len);
                        }
                        return len;
                    }
                }
            }
            return len;
        }
    }
}
