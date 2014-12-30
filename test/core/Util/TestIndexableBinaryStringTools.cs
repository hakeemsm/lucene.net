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
using System.Collections.Generic;

using NUnit.Framework;

namespace Lucene.Net.Util
{
	
    [TestFixture]
	public class TestIndexableBinaryStringTools:LuceneTestCase
	{
		private const int NUM_RANDOM_TESTS = 20000;
		private const int MAX_RANDOM_BINARY_LENGTH = 300;
		
        [Test]
		public virtual void  TestSingleBinaryRoundTrip()
		{
            byte[] binary = new byte[] {(byte)0x23, (byte)0x98, (byte)0x13, (byte)0xE4, (byte)0x76, (byte)0x41, (byte)0xB2, (byte)0xC9, (byte)0x7F, (byte)0x0A, (byte)0xA6, (byte)0xD8 };
			int encodedLen = IndexableBinaryStringTools.GetEncodedLength(binary, 0, binary.Length
				);
			char[] encoded = new char[encodedLen];
			IndexableBinaryStringTools.Encode(binary, 0, binary.Length, encoded, 0, encoded.Length
				);
			int decodedLen = IndexableBinaryStringTools.GetDecodedLength(encoded, 0, encoded.
				Length);
			byte[] decoded = new byte[decodedLen];
			IndexableBinaryStringTools.Decode(encoded, 0, encoded.Length, decoded, 0, decoded
				.Length);
            Assert.AreEqual(BinaryDump(binary, binary.Length), BinaryDump
				(decoded, decoded.Length), "Round trip decode/decode returned different results:" + System.Environment.NewLine + "original: " + BinaryDump(binaryBuf) + System.Environment.NewLine + " encoded: " + CharArrayDump(encoded) + System.Environment.NewLine + " decoded: " + BinaryDump(decoded));

		}
		
        [Test]
		public virtual void  TestEncodedSortability()
		{
            System.Random random = NewRandom();
            byte[] originalArray1 = new byte[MAX_RANDOM_BINARY_LENGTH];
            char[] originalString1 = new char[MAX_RANDOM_BINARY_LENGTH];
			char[] encoded1 = new char[MAX_RANDOM_BINARY_LENGTH * 10];
            byte[] original2 = new byte[MAX_RANDOM_BINARY_LENGTH];
            char[] originalString2 = new char[MAX_RANDOM_BINARY_LENGTH];
			char[] encoded2 = new char[MAX_RANDOM_BINARY_LENGTH * 10];
            for (int testNum = 0; testNum < NUM_RANDOM_TESTS; ++testNum)
            {
                int numBytes1 = random.Next(MAX_RANDOM_BINARY_LENGTH - 1) + 1; // Min == 1
                
                for (int byteNum = 0; byteNum < numBytes1; ++byteNum)
                {
                    int randomInt = random.Next(0x100);
                    originalArray1[byteNum] = (byte) randomInt;
                    originalString1[byteNum] = (char) randomInt;
                }
                
                int numBytes2 = random.Next(MAX_RANDOM_BINARY_LENGTH - 1) + 1; // Min == 1
                for (int byteNum = 0; byteNum < numBytes2; ++byteNum)
                {
                    int randomInt = random.Next(0x100);
                    original2[byteNum] = (byte) randomInt;
                    originalString2[byteNum] = (char) randomInt;
                }
                // put in strings to compare ordinals
				int originalComparison = Runtime.CompareOrdinal(new string(originalString1
					, 0, numBytes1), new string(originalString2, 0, numBytes2));
				originalComparison = originalComparison < 0 ? -1 : originalComparison > 0 ? 1 : 0;
				int encodedLen1 = IndexableBinaryStringTools.GetEncodedLength(originalArray1, 0, 
					numBytes1);
				if (encodedLen1 > encoded1.Length)
				{
					encoded1 = new char[ArrayUtil.Oversize(encodedLen1, RamUsageEstimator.NUM_BYTES_CHAR
						)];
				}
				IndexableBinaryStringTools.Encode(originalArray1, 0, numBytes1, encoded1, 0, encodedLen1
					);
				int encodedLen2 = IndexableBinaryStringTools.GetEncodedLength(original2, 0, numBytes2
					);
				if (encodedLen2 > encoded2.Length)
				{
					encoded2 = new char[ArrayUtil.Oversize(encodedLen2, RamUsageEstimator.NUM_BYTES_CHAR
						)];
				}
				IndexableBinaryStringTools.Encode(original2, 0, numBytes2, encoded2, 0, encodedLen2
					);
				int encodedComparison = Runtime.CompareOrdinal(new string(encoded1, 0, encodedLen1
					), new string(encoded2, 0, encodedLen2));
				encodedComparison = encodedComparison < 0 ? -1 : encodedComparison > 0 ? 1 : 0;
                
                Assert.AreEqual(originalComparison, encodedComparison, "Test #" + (testNum + 1) + ": Original bytes and encoded chars compare differently:" + System.Environment.NewLine + " binary 1: " + BinaryDump(originalBuf1) + System.Environment.NewLine + " binary 2: " + BinaryDump(originalBuf2) + System.Environment.NewLine + "encoded 1: " + CharArrayDump(encodedBuf1) + System.Environment.NewLine + "encoded 2: " + CharArrayDump(encodedBuf2) + System.Environment.NewLine);
            }
		}
		
		[Test]
		public virtual void  TestEmptyInput()
		{
			byte[] binary = new byte[0];
			int encodedLen = IndexableBinaryStringTools.GetEncodedLength(binary, 0, binary.Length
				);
			char[] encoded = new char[encodedLen];
			IndexableBinaryStringTools.Encode(binary, 0, binary.Length, encoded, 0, encoded.Length
				);
			int decodedLen = IndexableBinaryStringTools.GetDecodedLength(encoded, 0, encoded.
				Length);
			byte[] decoded = new byte[decodedLen];
			IndexableBinaryStringTools.Decode(encoded, 0, encoded.Length, decoded, 0, decoded
				.Length);
			Assert.AreEqual(decoded.Capacity, 0, "decoded empty input was not empty");
		}
		
        [Test]
		public virtual void  TestAllNullInput()
		{
			byte[] binary = new byte[]{0, 0, 0, 0, 0, 0, 0, 0, 0};
			int encodedLen = IndexableBinaryStringTools.GetEncodedLength(binary, 0, binary.Length
				);
			char[] encoded = new char[encodedLen];
			IndexableBinaryStringTools.Encode(binary, 0, binary.Length, encoded, 0, encoded.Length
				);
			int decodedLen = IndexableBinaryStringTools.GetDecodedLength(encoded, 0, encoded.
				Length);
			byte[] decoded = new byte[decodedLen];
			IndexableBinaryStringTools.Decode(encoded, 0, encoded.Length, decoded, 0, decoded
				.Length);
			Assert.AreEqual(binaryBuf, decodedBuf, "Round trip decode/decode returned different results:" + System.Environment.NewLine + "  original: " + BinaryDump(binaryBuf) + System.Environment.NewLine + "decodedBuf: " + BinaryDump(decodedBuf));
		}
		
        [Test]
		public virtual void  TestRandomBinaryRoundTrip()
		{
			System.Random random = NewRandom();
			byte[] binary = new byte[MAX_RANDOM_BINARY_LENGTH];
			char[] encoded = new char[MAX_RANDOM_BINARY_LENGTH * 10];
			byte[] decoded = new byte[MAX_RANDOM_BINARY_LENGTH];
			for (int testNum = 0; testNum < NUM_RANDOM_TESTS; ++testNum)
			{
				int numBytes = random.Next(MAX_RANDOM_BINARY_LENGTH - 1) + 1; // Min == 1
				for (int byteNum = 0; byteNum < numBytes; ++byteNum)
				{
					binary[byteNum] = (byte) random.Next(0x100);
				}
				int encodedLen = IndexableBinaryStringTools.GetEncodedLength(binary, 0, numBytes);
				if (encoded.Length < encodedLen)
				{
					encoded = new char[ArrayUtil.Oversize(encodedLen, RamUsageEstimator.NUM_BYTES_CHAR
						)];
				}
				IndexableBinaryStringTools.Encode(binary, 0, numBytes, encoded, 0, encodedLen);
				int decodedLen = IndexableBinaryStringTools.GetDecodedLength(encoded, 0, encodedLen
					);
				IndexableBinaryStringTools.Decode(encoded, 0, encodedLen, decoded, 0, decodedLen);
				Assert.AreEqual(binaryBuf, decodedBuf, "Test #" + (testNum + 1) + ": Round trip decode/decode returned different results:" + System.Environment.NewLine + "  original: " + BinaryDump(binaryBuf) + System.Environment.NewLine + "encodedBuf: " + CharArrayDump(encodedBuf) + System.Environment.NewLine + "decodedBuf: " + BinaryDump(decodedBuf));
			}
		}
		
		public virtual string BinaryDump(byte[] binary, int numBytes)
		{
			System.Text.StringBuilder buf = new System.Text.StringBuilder();
			for (int byteNum = 0; byteNum < numBytes; ++byteNum)
			{
				System.String hex = System.Convert.ToString((int) binaryBuf[byteNum] & 0xFF, 16);
				if (hex.Length == 1)
				{
					buf.Append('0');
				}
				buf.Append(hex.ToUpper());
				if (byteNum < numBytes - 1)
				{
					buf.Append(' ');
				}
			}
			return buf.ToString();
		}
		
		public virtual string CharArrayDump(char[] charArray, int numBytes)
		{
			System.Text.StringBuilder buf = new System.Text.StringBuilder();
			for (int charNum = 0; charNum < numBytes; ++charNum)
			{
				System.String hex = System.Convert.ToString((int) charBuf[charNum], 16);
				for (int digit = 0; digit < 4 - hex.Length; ++digit)
				{
					buf.Append('0');
				}
				buf.Append(hex.ToUpper());
				if (charNum < numBytes - 1)
				{
					buf.Append(' ');
				}
			}
			return buf.ToString();
		}
	}
}