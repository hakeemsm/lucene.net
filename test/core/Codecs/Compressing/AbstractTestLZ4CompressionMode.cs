/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Com.Carrotsearch.Randomizedtesting.Generators;
using Lucene.Net.Codecs.Compressing;
using Sharpen;

namespace Lucene.Net.Codecs.Compressing
{
	public abstract class AbstractTestLZ4CompressionMode : AbstractTestCompressionMode
	{
		/// <exception cref="System.IO.IOException"></exception>
		public override byte[] Test(byte[] decompressed)
		{
			byte[] compressed = base.Test(decompressed);
			int off = 0;
			int decompressedOff = 0;
			for (; ; )
			{
				int token = compressed[off++] & unchecked((int)(0xFF));
				int literalLen = (int)(((uint)token) >> 4);
				if (literalLen == unchecked((int)(0x0F)))
				{
					while (compressed[off] == unchecked((byte)unchecked((int)(0xFF))))
					{
						literalLen += unchecked((int)(0xFF));
						++off;
					}
					literalLen += compressed[off++] & unchecked((int)(0xFF));
				}
				// skip literals
				off += literalLen;
				decompressedOff += literalLen;
				// check that the stream ends with literals and that there are at least
				// 5 of them
				if (off == compressed.Length)
				{
					NUnit.Framework.Assert.AreEqual(decompressed.Length, decompressedOff);
					NUnit.Framework.Assert.IsTrue("lastLiterals=" + literalLen + ", bytes=" + decompressed
						.Length, literalLen >= LZ4.LAST_LITERALS || literalLen == decompressed.Length);
					break;
				}
				int matchDec = (compressed[off++] & unchecked((int)(0xFF))) | ((compressed[off++]
					 & unchecked((int)(0xFF))) << 8);
				// check that match dec is not 0
				NUnit.Framework.Assert.IsTrue(matchDec + " " + decompressedOff, matchDec > 0 && matchDec
					 <= decompressedOff);
				int matchLen = token & unchecked((int)(0x0F));
				if (matchLen == unchecked((int)(0x0F)))
				{
					while (compressed[off] == unchecked((byte)unchecked((int)(0xFF))))
					{
						matchLen += unchecked((int)(0xFF));
						++off;
					}
					matchLen += compressed[off++] & unchecked((int)(0xFF));
				}
				matchLen += LZ4.MIN_MATCH;
				// if the match ends prematurely, the next sequence should not have
				// literals or this means we are wasting space
				if (decompressedOff + matchLen < decompressed.Length - LZ4.LAST_LITERALS)
				{
					bool moreCommonBytes = decompressed[decompressedOff + matchLen] == decompressed[decompressedOff
						 - matchDec + matchLen];
					bool nextSequenceHasLiterals = ((int)(((uint)(compressed[off] & unchecked((int)(0xFF
						)))) >> 4)) != 0;
					NUnit.Framework.Assert.IsTrue(!moreCommonBytes || !nextSequenceHasLiterals);
				}
				decompressedOff += matchLen;
			}
			NUnit.Framework.Assert.AreEqual(decompressed.Length, decompressedOff);
			return compressed;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestShortLiteralsAndMatchs()
		{
			// literals and matchs lengths <= 15
			byte[] decompressed = Sharpen.Runtime.GetBytesForString("1234562345673456745678910123"
				, StandardCharsets.UTF_8);
			Test(decompressed);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestLongMatchs()
		{
			// match length >= 20
			byte[] decompressed = new byte[RandomInts.RandomIntBetween(Random(), 300, 1024)];
			for (int i = 0; i < decompressed.Length; ++i)
			{
				decompressed[i] = unchecked((byte)i);
			}
			Test(decompressed);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestLongLiterals()
		{
			// long literals (length >= 16) which are not the last literals
			byte[] decompressed = RandomArray(RandomInts.RandomIntBetween(Random(), 400, 1024
				), 256);
			int matchRef = Random().Next(30);
			int matchOff = RandomInts.RandomIntBetween(Random(), decompressed.Length - 40, decompressed
				.Length - 20);
			int matchLength = RandomInts.RandomIntBetween(Random(), 4, 10);
			System.Array.Copy(decompressed, matchRef, decompressed, matchOff, matchLength);
			Test(decompressed);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestMatchRightBeforeLastLiterals()
		{
			Test(new byte[] { 1, 2, 3, 4, 1, 2, 3, 4, 1, 2, 3, 4, 5 });
		}
	}
}
