using System.Text;
using Lucene.Net.Codecs.Compressing;
using Lucene.Net.Randomized.Generators;
using NUnit.Framework;

namespace Lucene.Net.Test.Codecs.Compressing
{
    [TestFixture]
    public abstract class AbstractTestLZ4CompressionMode : AbstractTestCompressionMode
	{
		[Test]
		public override byte[] Test(byte[] decompressed)
		{
			byte[] compressed = base.Test(decompressed);
			int off = 0;
			int decompressedOff = 0;
			for (; ; )
			{
				int token = compressed[off++] & (0xFF);
				int literalLen = (int)(((uint)token) >> 4);
				if (literalLen == (0x0F))
				{
					while (compressed[off] == 0xFF)
					{
						literalLen += (0xFF);
						++off;
					}
					literalLen += compressed[off++] & (0xFF);
				}
				// skip literals
				off += literalLen;
				decompressedOff += literalLen;
				// check that the stream ends with literals and that there are at least
				// 5 of them
				if (off == compressed.Length)
				{
					AreEqual(decompressed.Length, decompressedOff);
					IsTrue(literalLen >= LZ4.LAST_LITERALS || literalLen == decompressed.Length,"lastLiterals=" + literalLen + ", bytes=" + decompressed
						.Length);
					break;
				}
				int matchDec = (compressed[off++] & (0xFF)) | ((compressed[off++]& (0xFF)) << 8);
				// check that match dec is not 0
				IsTrue(matchDec > 0 && matchDec <= decompressedOff, matchDec + " " + decompressedOff);
				int matchLen = token & (0x0F);
				if (matchLen == (0x0F))
				{
					while (compressed[off] == 0xFF)
					{
						matchLen += (0xFF);
						++off;
					}
					matchLen += compressed[off++] & (0xFF);
				}
				matchLen += LZ4.MIN_MATCH;
				// if the match ends prematurely, the next sequence should not have
				// literals or this means we are wasting space
				if (decompressedOff + matchLen < decompressed.Length - LZ4.LAST_LITERALS)
				{
					bool moreCommonBytes = decompressed[decompressedOff + matchLen] == decompressed[decompressedOff
						 - matchDec + matchLen];
					bool nextSequenceHasLiterals = (compressed[off] & 0xFF >> 4) != 0;
					IsTrue(!moreCommonBytes || !nextSequenceHasLiterals);
				}
				decompressedOff += matchLen;
			}
			AreEqual(decompressed.Length, decompressedOff);
			return compressed;
		}

		[Test]
		public virtual void TestShortLiteralsAndMatchs()
		{
			// literals and matchs lengths <= 15
		    byte[] decompressed = Encoding.UTF8.GetBytes("1234562345673456745678910123");
			Test(decompressed);
		}

		[Test]
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

		[Test]
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

		[Test]
		public virtual void TestMatchRightBeforeLastLiterals()
		{
			Test(new byte[] { 1, 2, 3, 4, 1, 2, 3, 4, 1, 2, 3, 4, 5 });
		}
	}
}
