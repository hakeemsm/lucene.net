using System;
using Lucene.Net.Codecs.Compressing;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.TestFramework.Util;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Codecs.Compressing
{
    [TestFixture]
	public abstract class AbstractTestCompressionMode : LuceneTestCase
	{
		internal CompressionMode mode;

		internal static byte[] RandomArray()
		{
			int max = Random().NextBoolean() ? Random().Next(4) : Random().Next(256);
			int length = Random().NextBoolean() ? Random().Next(20) : Random().Next(192 * 1024
				);
			return RandomArray(length, max);
		}

		internal static byte[] RandomArray(int length, int max)
		{
			byte[] arr = new byte[length];
			for (int i = 0; i < arr.Length; ++i)
			{
				arr[i] = unchecked((byte)RandomInts.RandomIntBetween(Random(), 0, max));
			}
			return arr;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual byte[] Compress(byte[] decompressed, int off, int len)
		{
			Compressor compressor = mode.NewCompressor();
			return Compress(compressor, decompressed, off, len);
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal static byte[] Compress(Compressor compressor, byte[] decompressed, int off, int len)
		{
			var compressed = new byte[len * 2 + 16];
			// should be enough
			ByteArrayDataOutput @out = new ByteArrayDataOutput(compressed);
			compressor.Compress(Array.ConvertAll(decompressed,Convert.ToSByte), off, len, @out);
			int compressedLen = @out.Position;
			return Arrays.CopyOf(compressed, compressedLen);
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual byte[] Decompress(byte[] compressed, int originalLength)
		{
			Decompressor decompressor = mode.NewDecompressor();
			return Decompress(decompressor, compressed, originalLength);
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal static byte[] Decompress(Decompressor decompressor, byte[] compressed, int
			 originalLength)
		{
			BytesRef bytes = new BytesRef();
			decompressor.Decompress(new ByteArrayDataInput(compressed), originalLength, 0, originalLength
				, bytes);
		    byte[] convertedBytes = Array.ConvertAll(bytes.bytes, Convert.ToByte);
		    return Arrays.CopyOfRange(convertedBytes, bytes.offset, bytes.offset + bytes.length);
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual byte[] Decompress(byte[] compressed, int originalLength, int offset
			, int length)
		{
			Decompressor decompressor = mode.NewDecompressor();
			BytesRef bytes = new BytesRef();
			decompressor.Decompress(new ByteArrayDataInput(compressed), originalLength, offset
				, length, bytes);
            byte[] convertedBytes = Array.ConvertAll(bytes.bytes, Convert.ToByte);
            return Arrays.CopyOfRange(convertedBytes, bytes.offset, bytes.offset + bytes.length);
		}

		[Test]
		public virtual void TestDecompress()
		{
			int iterations = AtLeast(10);
			for (int i = 0; i < iterations; ++i)
			{
				byte[] decompressed = RandomArray();
				int off = Random().NextBoolean() ? 0 : TestUtil.NextInt(Random(), 0, decompressed
					.Length);
				int len = Random().NextBoolean() ? decompressed.Length - off : TestUtil.NextInt(Random
					(), 0, decompressed.Length - off);
				byte[] compressed = Compress(decompressed, off, len);
				byte[] restored = Decompress(compressed, len);
				Assert.AreEqual(Arrays.CopyOfRange(decompressed, off, off + len), restored);
			}
		}

		[Test]
		public virtual void TestPartialDecompress()
		{
			int iterations = AtLeast(10);
			for (int i = 0; i < iterations; ++i)
			{
				byte[] decompressed = RandomArray();
				byte[] compressed = Compress(decompressed, 0, decompressed.Length);
				int offset;
				int length;
				if (decompressed.Length == 0)
				{
					offset = length = 0;
				}
				else
				{
					offset = Random().Next(decompressed.Length);
					length = Random().Next(decompressed.Length - offset);
				}
				byte[] restored = Decompress(compressed, decompressed.Length, offset, length);
				Assert.AreEqual(Arrays.CopyOfRange(decompressed, offset, offset + length), restored);
                //AssertArrayEquals(Arrays.CopyOfRange(decompressed, offset, offset + length), restored);
                
			}
		}

		
		public virtual byte[] Test(byte[] decompressed)
		{
			return Test(decompressed, 0, decompressed.Length);
		}

		[Test]
		public virtual byte[] Test(byte[] decompressed, int off, int len)
		{
			byte[] compressed = Compress(decompressed, off, len);
			byte[] restored = Decompress(compressed, len);
			AreEqual(len, restored.Length);
			return compressed;
		}

		[Test]
		public virtual void TestEmptySequence()
		{
			Test(new byte[0]);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestShortSequence()
		{
			Test(new byte[] { unchecked((byte)Random().Next(256)) });
		}

		[Test]
		public virtual void TestIncompressible()
		{
			byte[] decompressed = new byte[RandomInts.RandomIntBetween(Random(), 20, 256)];
			for (int i = 0; i < decompressed.Length; ++i)
			{
				decompressed[i] = unchecked((byte)i);
			}
			Test(decompressed);
		}

		[Test]
		public virtual void TestConstant()
		{
			byte[] decompressed = new byte[TestUtil.NextInt(Random(), 1, 10000)];
			Arrays.Fill(decompressed, unchecked((byte)Random().Next()));
			Test(decompressed);
		}

		[Test]
		public virtual void TestLUCENE5201()
		{
			byte[] data = new byte[] { 14, 72, 14, 85, 3, 72, 14, 85, 3, 72, 14, 72, 14, 72, 
				14, 85, 3, 72, 14, 72, 14, 72, 14, 72, 14, 72, 14, 72, 14, 85, 3, 72, 14, 85, 3, 
				72, 14, 85, 3, 72, 14, 85, 3, 72, 14, 85, 3, 72, 14, 85, 3, 72, 14, 50, 64, 0, 46
				, unchecked((byte)(-1)), 0, 0, 0, 29, 3, 85, 8, unchecked((byte)(-113)), 0, 68, 
				unchecked((byte)(-97)), 3, 0, 2, 3, unchecked((byte)(-97)), 6, 0, 68, unchecked(
				(byte)(-113)), 0, 2, 3, unchecked((byte)(-97)), 6, 0, 68, unchecked((byte)(-113)
				), 0, 2, 3, 85, 8, unchecked((byte)(-113)), 0, 68, unchecked((byte)(-97)), 3, 0, 
				2, 3, unchecked((byte)(-97)), 6, 0, 68, unchecked((byte)(-113)), 0, 2, 3, unchecked(
				(byte)(-97)), 6, 0, 68, unchecked((byte)(-113)), 0, 2, 3, unchecked((byte)(-97))
				, 6, 0, 68, unchecked((byte)(-113)), 0, 2, 3, unchecked((byte)(-97)), 6, 0, 68, 
				unchecked((byte)(-113)), 0, 2, 3, unchecked((byte)(-97)), 6, 0, 68, unchecked((byte
				)(-113)), 0, 2, 3, unchecked((byte)(-97)), 6, 0, 68, unchecked((byte)(-113)), 0, 
				2, 3, unchecked((byte)(-97)), 6, 0, 68, unchecked((byte)(-113)), 0, 2, 3, unchecked(
				(byte)(-97)), 6, 0, 68, unchecked((byte)(-113)), 0, 2, 3, unchecked((byte)(-97))
				, 6, 0, 68, unchecked((byte)(-113)), 0, 2, 3, unchecked((byte)(-97)), 6, 0, 68, 
				unchecked((byte)(-113)), 0, 50, 64, 0, 47, unchecked((byte)(-105)), 0, 0, 0, 30, 
				3, unchecked((byte)(-97)), 6, 0, 68, unchecked((byte)(-113)), 0, 2, 3, unchecked(
				(byte)(-97)), 6, 0, 68, unchecked((byte)(-113)), 0, 2, 3, 85, 8, unchecked((byte
				)(-113)), 0, 68, unchecked((byte)(-97)), 3, 0, 2, 3, 85, 8, unchecked((byte)(-113
				)), 0, 68, unchecked((byte)(-97)), 3, 0, 2, 3, 85, 8, unchecked((byte)(-113)), 0
				, 68, unchecked((byte)(-97)), 3, 0, 2, unchecked((byte)(-97)), 6, 0, 2, 3, 85, 8
				, unchecked((byte)(-113)), 0, 68, unchecked((byte)(-97)), 3, 0, 2, 3, unchecked(
				(byte)(-97)), 6, 0, 68, unchecked((byte)(-113)), 0, 2, 3, unchecked((byte)(-97))
				, 6, 0, 68, unchecked((byte)(-113)), 0, 120, 64, 0, 48, 4, 0, 0, 0, 31, 34, 72, 
				29, 72, 37, 72, 35, 72, 45, 72, 23, 72, 46, 72, 20, 72, 40, 72, 33, 72, 25, 72, 
				39, 72, 38, 72, 26, 72, 28, 72, 42, 72, 24, 72, 27, 72, 36, 72, 41, 72, 32, 72, 
				18, 72, 30, 72, 22, 72, 31, 72, 43, 72, 19, 72, 34, 72, 29, 72, 37, 72, 35, 72, 
				45, 72, 23, 72, 46, 72, 20, 72, 40, 72, 33, 72, 25, 72, 39, 72, 38, 72, 26, 72, 
				28, 72, 42, 72, 24, 72, 27, 72, 36, 72, 41, 72, 32, 72, 18, 72, 30, 72, 22, 72, 
				31, 72, 43, 72, 19, 72, 34, 72, 29, 72, 37, 72, 35, 72, 45, 72, 23, 72, 46, 72, 
				20, 72, 40, 72, 33, 72, 25, 72, 39, 72, 38, 72, 26, 72, 28, 72, 42, 72, 24, 72, 
				27, 72, 36, 72, 41, 72, 32, 72, 18, 16, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
				, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 39, 24, 32, 34, 124, 0, 120, 64, 0, 48, 
				80, 0, 0, 0, 31, 30, 72, 22, 72, 31, 72, 43, 72, 19, 72, 34, 72, 29, 72, 37, 72, 
				35, 72, 45, 72, 23, 72, 46, 72, 20, 72, 40, 72, 33, 72, 25, 72, 39, 72, 38, 72, 
				26, 72, 28, 72, 42, 72, 24, 72, 27, 72, 36, 72, 41, 72, 32, 72, 18, 72, 30, 72, 
				22, 72, 31, 72, 43, 72, 19, 72, 34, 72, 29, 72, 37, 72, 35, 72, 45, 72, 23, 72, 
				46, 72, 20, 72, 40, 72, 33, 72, 25, 72, 39, 72, 38, 72, 26, 72, 28, 72, 42, 72, 
				24, 72, 27, 72, 36, 72, 41, 72, 32, 72, 18, 72, 30, 72, 22, 72, 31, 72, 43, 72, 
				19, 72, 34, 72, 29, 72, 37, 72, 35, 72, 45, 72, 23, 72, 46, 72, 20, 72, 40, 72, 
				33, 72, 25, 72, 39, 72, 38, 72, 26, 72, 28, 72, 42, 72, 24, 72, 27, 72, 36, 72, 
				41, 72, 32, 72, 18, 72, 30, 72, 22, 72, 31, 72, 43, 72, 19, 72, 34, 72, 29, 72, 
				37, 72, 35, 72, 45, 72, 23, 72, 46, 72, 20, 72, 40, 72, 33, 72, 25, 72, 39, 72, 
				38, 72, 26, 72, 28, 72, 42, 72, 24, 72, 27, 72, 36, 72, 41, 72, 32, 72, 18, 72, 
				30, 72, 22, 72, 31, 72, 43, 72, 19, 72, 34, 72, 29, 72, 37, 72, 35, 72, 45, 72, 
				23, 72, 46, 72, 20, 72, 40, 72, 33, 72, 25, 72, 39, 72, 38, 72, 26, 72, 28, 72, 
				42, 72, 24, 72, 27, 72, 36, 72, 41, 72, 32, 72, 18, 72, 30, 72, 22, 72, 31, 72, 
				43, 72, 19, 72, 34, 72, 29, 72, 37, 72, 35, 72, 45, 72, 23, 72, 46, 72, 20, 72, 
				40, 72, 33, 72, 25, 72, 39, 72, 38, 72, 26, 72, 28, 72, 42, 72, 24, 72, 27, 72, 
				36, 72, 41, 72, 32, 72, 18, 72, 30, 72, 22, 72, 31, 72, 43, 72, 19, 72, 34, 72, 
				29, 72, 37, 72, 35, 72, 45, 72, 23, 72, 46, 72, 20, 72, 40, 72, 33, 72, 25, 72, 
				39, 72, 38, 72, 26, 72, 28, 72, 42, 72, 24, 72, 27, 72, 36, 72, 41, 72, 32, 72, 
				18, 72, 30, 72, 22, 72, 31, 72, 43, 72, 19, 50, 64, 0, 49, 20, 0, 0, 0, 32, 3, unchecked(
				(byte)(-97)), 6, 0, 68, unchecked((byte)(-113)), 0, 2, 3, 85, 8, unchecked((byte
				)(-113)), 0, 68, unchecked((byte)(-97)), 3, 0, 2, 3, unchecked((byte)(-97)), 6, 
				0, 68, unchecked((byte)(-113)), 0, 2, 3, unchecked((byte)(-97)), 6, 0, 68, unchecked(
				(byte)(-113)), 0, 2, 3, unchecked((byte)(-97)), 6, 0, 68, unchecked((byte)(-113)
				), 0, 2, 3, 85, 8, unchecked((byte)(-113)), 0, 68, unchecked((byte)(-97)), 3, 0, 
				2, 3, unchecked((byte)(-97)), 6, 0, 68, unchecked((byte)(-113)), 0, 2, 3, unchecked(
				(byte)(-97)), 6, 0, 68, unchecked((byte)(-113)), 0, 2, 3, unchecked((byte)(-97))
				, 6, 0, 68, unchecked((byte)(-113)), 0, 2, 3, unchecked((byte)(-97)), 6, 0, 68, 
				unchecked((byte)(-113)), 0, 2, 3, unchecked((byte)(-97)), 6, 0, 68, unchecked((byte
				)(-113)), 0, 2, 3, unchecked((byte)(-97)), 6, 0, 68, unchecked((byte)(-113)), 0, 
				2, 3, unchecked((byte)(-97)), 6, 0, 50, 64, 0, 50, 53, 0, 0, 0, 34, 3, unchecked(
				(byte)(-97)), 6, 0, 68, unchecked((byte)(-113)), 0, 2, 3, 85, 8, unchecked((byte
				)(-113)), 0, 68, unchecked((byte)(-113)), 0, 2, 3, unchecked((byte)(-97)), 6, 0, 
				68, unchecked((byte)(-113)), 0, 2, 3, 85, 8, unchecked((byte)(-113)), 0, 68, unchecked(
				(byte)(-113)), 0, 2, 3, unchecked((byte)(-97)), 6, 0, 68, unchecked((byte)(-113)
				), 0, 2, 3, unchecked((byte)(-97)), 6, 0, 68, unchecked((byte)(-113)), 0, 2, 3, 
				unchecked((byte)(-97)), 6, 0, 68, unchecked((byte)(-113)), 0, 2, 3, 85, 8, unchecked(
				(byte)(-113)), 0, 68, unchecked((byte)(-97)), 3, 0, 2, 3, unchecked((byte)(-97))
				, 6, 0, 68, unchecked((byte)(-113)), 0, 2, 3, 85, 8, unchecked((byte)(-113)), 0, 
				68, unchecked((byte)(-97)), 3, 0, 2, 3, 85, 8, unchecked((byte)(-113)), 0, 68, unchecked(
				(byte)(-97)), 3, 0, 2, 3, unchecked((byte)(-97)), 6, 0, 68, unchecked((byte)(-113
				)), 0, 2, 3, 85, 8, unchecked((byte)(-113)), 0, 68, unchecked((byte)(-97)), 3, 0
				, 2, 3, 85, 8, unchecked((byte)(-113)), 0, 68, unchecked((byte)(-97)), 3, 0, 2, 
				3, unchecked((byte)(-97)), 6, 0, 68, unchecked((byte)(-113)), 0, 2, 3, unchecked(
				(byte)(-97)), 6, 0, 68, unchecked((byte)(-113)), 0, 2, 3, unchecked((byte)(-97))
				, 6, 0, 68, unchecked((byte)(-113)), 0, 2, 3, 85, 8, unchecked((byte)(-113)), 0, 
				68, unchecked((byte)(-97)), 3, 0, 2, 3, 85, 8, unchecked((byte)(-113)), 0, 68, unchecked(
				(byte)(-97)), 3, 0, 2, 3, 85, 8, unchecked((byte)(-113)), 0, 68, unchecked((byte
				)(-97)), 3, 0, 2, 3, unchecked((byte)(-97)), 6, 0, 50, 64, 0, 51, 85, 0, 0, 0, 36
				, 3, 85, 8, unchecked((byte)(-113)), 0, 68, unchecked((byte)(-97)), 3, 0, 2, 3, 
				unchecked((byte)(-97)), 6, 0, 68, unchecked((byte)(-113)), 0, 2, 3, unchecked((byte
				)(-97)), 6, 0, 68, unchecked((byte)(-113)), 0, 2, 3, unchecked((byte)(-97)), 6, 
				0, 68, unchecked((byte)(-113)), 0, 2, 3, unchecked((byte)(-97)), 6, 0, 68, unchecked(
				(byte)(-113)), 0, 2, unchecked((byte)(-97)), 5, 0, 2, 3, 85, 8, unchecked((byte)
				(-113)), 0, 68, unchecked((byte)(-97)), 3, 0, 2, 3, unchecked((byte)(-97)), 6, 0
				, 68, unchecked((byte)(-113)), 0, 2, 3, unchecked((byte)(-97)), 6, 0, 68, unchecked(
				(byte)(-113)), 0, 2, 3, unchecked((byte)(-97)), 6, 0, 68, unchecked((byte)(-113)
				), 0, 2, 3, unchecked((byte)(-97)), 6, 0, 68, unchecked((byte)(-113)), 0, 2, 3, 
				unchecked((byte)(-97)), 6, 0, 50, unchecked((byte)(-64)), 0, 51, unchecked((byte
				)(-45)), 0, 0, 0, 37, 68, unchecked((byte)(-113)), 0, 2, 3, unchecked((byte)(-97
				)), 6, 0, 68, unchecked((byte)(-113)), 0, 2, 3, unchecked((byte)(-97)), 6, 0, 68
				, unchecked((byte)(-113)), 0, 2, 3, unchecked((byte)(-97)), 6, 0, 68, unchecked(
				(byte)(-113)), 0, 2, 3, unchecked((byte)(-97)), 6, 0, 68, unchecked((byte)(-113)
				), 0, 2, 3, 85, 8, unchecked((byte)(-113)), 0, 68, unchecked((byte)(-113)), 0, 2
				, 3, unchecked((byte)(-97)), 6, 0, 68, unchecked((byte)(-113)), 0, 2, 3, 85, 8, 
				unchecked((byte)(-113)), 0, 68, unchecked((byte)(-97)), 3, 0, 2, 3, 85, 8, unchecked(
				(byte)(-113)), 0, 68, unchecked((byte)(-97)), 3, 0, 120, 64, 0, 52, unchecked((byte
				)(-88)), 0, 0, 0, 39, 13, 85, 5, 72, 13, 85, 5, 72, 13, 85, 5, 72, 13, 72, 13, 85
				, 5, 72, 13, 85, 5, 72, 13, 85, 5, 72, 13, 85, 5, 72, 13, 72, 13, 85, 5, 72, 13, 
				85, 5, 72, 13, 72, 13, 72, 13, 85, 5, 72, 13, 85, 5, 72, 13, 85, 5, 72, 13, 85, 
				5, 72, 13, 85, 5, 72, 13, 85, 5, 72, 13, 72, 13, 72, 13, 72, 13, 85, 5, 72, 13, 
				85, 5, 72, 13, 72, 13, 85, 5, 72, 13, 85, 5, 72, 13, 85, 5, 72, 13, 85, 5, 72, 13
				, 85, 5, 72, 13, 85, 5, 72, 13, 85, 5, 72, 13, 85, 5, 72, 13, 85, 5, 72, 13, 85, 
				5, 72, 13, 85, 5, 72, 13, 85, 5, 72, 13, 72, 13, 72, 13, 72, 13, 85, 5, 72, 13, 
				85, 5, 72, 13, 85, 5, 72, 13, 72, 13, 85, 5, 72, 13, 72, 13, 85, 5, 72, 13, 72, 
				13, 85, 5, 72, 13, unchecked((byte)(-19)), unchecked((byte)(-24)), unchecked((byte
				)(-101)), unchecked((byte)(-35)) };
			Test(data, 9, data.Length - 9);
		}
	}
}
