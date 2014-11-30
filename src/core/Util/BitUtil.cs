/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using Lucene.Net.Support;

namespace Lucene.Net.Util
{
    // from org.apache.solr.util rev 555343

    /// <summary>A variety of high efficiencly bit twiddling routines.
    /// 
    /// </summary>
    /// <version>  $Id$
    /// </version>
    public static class BitUtil
    {
        private static readonly byte[] BYTE_COUNTS = new byte[] { 0, 1, 1, 2, 1, 2, 2, 3, 
			1, 2, 2, 3, 2, 3, 3, 4, 1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5, 1, 2, 2, 
			3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5, 2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 
			5, 6, 1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5, 2, 3, 3, 4, 3, 4, 4, 5, 3, 
			4, 4, 5, 4, 5, 5, 6, 2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6, 3, 4, 4, 5, 
			4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7, 1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 
			5, 2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6, 2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 
			4, 5, 4, 5, 5, 6, 3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7, 2, 3, 3, 4, 3, 
			4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6, 3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7, 
			3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7, 4, 5, 5, 6, 5, 6, 6, 7, 5, 6, 6, 
			7, 6, 7, 7, 8 };

        /// <summary>Returns the number of bits set in the long </summary>
        private static readonly int[] BIT_LISTS =
		{ unchecked(0x0), unchecked(
		    (0x1)), unchecked(0x2), unchecked(0x21), unchecked((0x3)
		        ), unchecked((0x31)), unchecked((0x32)), unchecked((0x321)), unchecked(
		            (0x4)), unchecked((0x41)), unchecked((0x42)), unchecked((0x421
		                )), unchecked((0x43)), unchecked((0x431)), unchecked((0x432)), unchecked(
		                    (0x4321)), unchecked((0x5)), unchecked((0x51)), unchecked((0x52
		                        )), unchecked((0x521)), unchecked((0x53)), unchecked((0x531)), unchecked(
		                            (0x532)), unchecked((0x5321)), unchecked((0x54)), unchecked(
		                                (0x541)), unchecked((0x542)), unchecked((0x5421)), unchecked((0x543
		                                    )), unchecked((0x5431)), unchecked((0x5432)), unchecked((0x54321)
		                                        ), unchecked((0x6)), unchecked((0x61)), unchecked((0x62)), unchecked(
		                                            (0x621)), unchecked((0x63)), unchecked((0x631)), unchecked((
		                                                0x632)), unchecked((0x6321)), unchecked((0x64)), unchecked((0x641
		                                                    )), unchecked((0x642)), unchecked((0x6421)), unchecked((0x643)), 
		    unchecked((0x6431)), unchecked((0x6432)), unchecked((0x64321)), unchecked(
		        (0x65)), unchecked((0x651)), unchecked((0x652)), unchecked((
		            0x6521)), unchecked((0x653)), unchecked((0x6531)), unchecked((0x6532
		                )), unchecked((0x65321)), unchecked((0x654)), unchecked((0x6541))
		    , unchecked((0x6542)), unchecked((0x65421)), unchecked((0x6543)), 
		    unchecked((0x65431)), unchecked((0x65432)), unchecked((0x654321))
		    , unchecked((0x7)), unchecked((0x71)), unchecked((0x72)), unchecked(
		        (0x721)), unchecked((0x73)), unchecked((0x731)), unchecked((
		            0x732)), unchecked((0x7321)), unchecked((0x74)), unchecked((0x741
		                )), unchecked((0x742)), unchecked((0x7421)), unchecked((0x743)), 
		    unchecked((0x7431)), unchecked((0x7432)), unchecked((0x74321)), unchecked(
		        (0x75)), unchecked((0x751)), unchecked((0x752)), unchecked((
		            0x7521)), unchecked((0x753)), unchecked((0x7531)), unchecked((0x7532
		                )), unchecked((0x75321)), unchecked((0x754)), unchecked((0x7541))
		    , unchecked((0x7542)), unchecked((0x75421)), unchecked((0x7543)), 
		    unchecked((0x75431)), unchecked((0x75432)), unchecked((0x754321))
		    , unchecked((0x76)), unchecked((0x761)), unchecked((0x762)), unchecked(
		        (0x7621)), unchecked((0x763)), unchecked((0x7631)), unchecked((int
		            )(0x7632)), unchecked((0x76321)), unchecked((0x764)), unchecked((
		                0x7641)), unchecked((0x7642)), unchecked((0x76421)), unchecked((0x7643
		                    )), unchecked((0x76431)), unchecked((0x76432)), unchecked((0x764321
		                        )), unchecked((0x765)), unchecked((0x7651)), unchecked((0x7652)), 
		    unchecked((0x76521)), unchecked((0x7653)), unchecked((0x76531)), 
		    unchecked((0x76532)), unchecked((0x765321)), unchecked((0x7654)), 
		    unchecked((0x76541)), unchecked((0x76542)), unchecked((0x765421))
		    , unchecked((0x76543)), unchecked((0x765431)), unchecked((0x765432
		        )), unchecked((0x7654321)), unchecked((0x8)), unchecked((0x81)), 
		    unchecked((0x82)), unchecked((0x821)), unchecked((0x83)), unchecked(
		        (0x831)), unchecked((0x832)), unchecked((0x8321)), unchecked((int
		            )(0x84)), unchecked((0x841)), unchecked((0x842)), unchecked((0x8421
		                )), unchecked((0x843)), unchecked((0x8431)), unchecked((0x8432)), 
		    unchecked((0x84321)), unchecked((0x85)), unchecked((0x851)), unchecked(
		        (0x852)), unchecked((0x8521)), unchecked((0x853)), unchecked((int
		            )(0x8531)), unchecked((0x8532)), unchecked((0x85321)), unchecked(
		                (0x854)), unchecked((0x8541)), unchecked((0x8542)), unchecked((0x85421
		                    )), unchecked((0x8543)), unchecked((0x85431)), unchecked((0x85432
		                        )), unchecked((0x854321)), unchecked((0x86)), unchecked((0x861)), 
		    unchecked((0x862)), unchecked((0x8621)), unchecked((0x863)), unchecked(
		        (0x8631)), unchecked((0x8632)), unchecked((0x86321)), unchecked((
		            int)(0x864)), unchecked((0x8641)), unchecked((0x8642)), unchecked((int
		                )(0x86421)), unchecked((0x8643)), unchecked((0x86431)), unchecked((int
		                    )(0x86432)), unchecked((0x864321)), unchecked((0x865)), unchecked((int
		                        )(0x8651)), unchecked((0x8652)), unchecked((0x86521)), unchecked(
		                            (0x8653)), unchecked((0x86531)), unchecked((0x86532)), unchecked(
		                                (0x865321)), unchecked((0x8654)), unchecked((0x86541)), unchecked((int
		                                    )(0x86542)), unchecked((0x865421)), unchecked((0x86543)), unchecked((int
		                                        )(0x865431)), unchecked((0x865432)), unchecked((0x8654321)), unchecked(
		                                            (0x87)), unchecked((0x871)), unchecked((0x872)), unchecked((
		                                                0x8721)), unchecked((0x873)), unchecked((0x8731)), unchecked((0x8732
		                                                    )), unchecked((0x87321)), unchecked((0x874)), unchecked((0x8741))
		    , unchecked((0x8742)), unchecked((0x87421)), unchecked((0x8743)), 
		    unchecked((0x87431)), unchecked((0x87432)), unchecked((0x874321))
		    , unchecked((0x875)), unchecked((0x8751)), unchecked((0x8752)), unchecked(
		        (0x87521)), unchecked((0x8753)), unchecked((0x87531)), unchecked(
		            (0x87532)), unchecked((0x875321)), unchecked((0x8754)), unchecked(
		                (0x87541)), unchecked((0x87542)), unchecked((0x875421)), unchecked(
		                    (0x87543)), unchecked((0x875431)), unchecked((0x875432)), unchecked(
		                        (0x8754321)), unchecked((0x876)), unchecked((0x8761)), unchecked(
		                            (0x8762)), unchecked((0x87621)), unchecked((0x8763)), unchecked((
		                                int)(0x87631)), unchecked((0x87632)), unchecked((0x876321)), unchecked(
		                                    (0x8764)), unchecked((0x87641)), unchecked((0x87642)), unchecked(
		                                        (0x876421)), unchecked((0x87643)), unchecked((0x876431)), unchecked(
		                                            (0x876432)), unchecked((0x8764321)), unchecked((0x8765)), unchecked(
		                                                (0x87651)), unchecked((0x87652)), unchecked((0x876521)), unchecked(
		                                                    (0x87653)), unchecked((0x876531)), unchecked((0x876532)), unchecked(
		                                                        (0x8765321)), unchecked((0x87654)), unchecked((0x876541)), unchecked(
		                                                            (0x876542)), unchecked((0x8765421)), unchecked((0x876543)), unchecked(
		                                                                (0x8765431)), unchecked((0x8765432)), unchecked((int)(0x87654321)) };

        /// <summary> Returns the number of set bits in an array of longs. </summary>
        public static int BitCount(byte b)
        {
            return BYTE_COUNTS[b & unchecked((0xFF))];
        }
        public static int BitList(byte b)
        {
            return BIT_LISTS[b & unchecked((0xFF))];
        }
        public static long Pop_array(long[] arr, int wordOffset, int numWords)
        {
            long popCount = 0;
            for (int i = wordOffset, end = wordOffset + numWords; i < end; ++i)
            {
                popCount += arr[i].BitCount();
            }
            return popCount;
        }

        /// <summary>Returns the popcount or cardinality of the two sets after an intersection.
        /// Neither array is modified.
        /// </summary>
        public static long Pop_intersect(long[] arr1, long[] arr2, int wordOffset, int numWords
            )
        {
            long popCount = 0;
            for (int i = wordOffset, end = wordOffset + numWords; i < end; ++i)
            {
                popCount += (arr1[i] & arr2[i]).BitCount();
            }
            return popCount;
        }

        /// <summary>Returns the popcount or cardinality of the union of two sets.
        /// Neither array is modified.
        /// </summary>
        public static long Pop_union(long[] arr1, long[] arr2, int wordOffset, int numWords)
        {
            long popCount = 0;
            for (int i = wordOffset, end = wordOffset + numWords; i < end; ++i)
            {
                popCount += (arr1[i] | arr2[i]).BitCount();
            }
            return popCount;
        }

        /// <summary>Returns the popcount or cardinality of A &amp; ~B
        /// Neither array is modified.
        /// </summary>
        public static long Pop_andnot(long[] arr1, long[] arr2, int wordOffset, int numWords)
        {
            long popCount = 0;
            for (int i = wordOffset, end = wordOffset + numWords; i < end; ++i)
            {
                popCount += (arr1[i] & ~arr2[i]).BitCount();
            }
            return popCount;
        }

        public static long Pop_xor(long[] arr1, long[] arr2, int wordOffset, int numWords)
        {
            long popCount = 0;
            for (int i = wordOffset, end = wordOffset + numWords; i < end; ++i)
            {
                popCount += (arr1[i] ^ arr2[i]).BitCount();
            }
            return popCount;
        }

        /// <summary>returns the next highest power of two, or the current value if it's already a power of two or zero
        /// 	</summary>
        /// <summary>returns the next highest power of two, or the current value if it's already a power of two or zero</summary>
        public static int NextHighestPowerOfTwo(int v)
        {
            v--;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            v++;
            return v;
        }

        /// <summary>returns the next highest power of two, or the current value if it's already a power of two or zero</summary>
        public static long NextHighestPowerOfTwo(long v)
        {
            v--;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            v |= v >> 32;
            v++;
            return v;
        }
    }
}