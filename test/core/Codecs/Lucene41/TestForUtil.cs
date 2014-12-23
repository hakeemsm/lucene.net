/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Com.Carrotsearch.Randomizedtesting.Generators;
using Lucene.Net.Codecs.Lucene41;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Packed;
using Sharpen;

namespace Lucene.Net.Codecs.Lucene41
{
	public class TestForUtil : LuceneTestCase
	{
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestEncodeDecode()
		{
			int iterations = RandomInts.RandomIntBetween(Random(), 1, 1000);
			float acceptableOverheadRatio = Random().NextFloat();
			int[] values = new int[(iterations - 1) * Lucene41PostingsFormat.BLOCK_SIZE + ForUtil
				.MAX_DATA_SIZE];
			for (int i = 0; i < iterations; ++i)
			{
				int bpv = Random().Next(32);
				if (bpv == 0)
				{
					int value = RandomInts.RandomIntBetween(Random(), 0, int.MaxValue);
					for (int j = 0; j < Lucene41PostingsFormat.BLOCK_SIZE; ++j)
					{
						values[i * Lucene41PostingsFormat.BLOCK_SIZE + j] = value;
					}
				}
				else
				{
					for (int j = 0; j < Lucene41PostingsFormat.BLOCK_SIZE; ++j)
					{
						values[i * Lucene41PostingsFormat.BLOCK_SIZE + j] = RandomInts.RandomIntBetween(Random
							(), 0, (int)PackedInts.MaxValue(bpv));
					}
				}
			}
			Directory d = new RAMDirectory();
			long endPointer;
			{
				// encode
				IndexOutput @out = d.CreateOutput("test.bin", IOContext.DEFAULT);
				ForUtil forUtil = new ForUtil(acceptableOverheadRatio, @out);
				for (int i_1 = 0; i_1 < iterations; ++i_1)
				{
					forUtil.WriteBlock(Arrays.CopyOfRange(values, i_1 * Lucene41PostingsFormat.BLOCK_SIZE
						, values.Length), new byte[ForUtil.MAX_ENCODED_SIZE], @out);
				}
				endPointer = @out.GetFilePointer();
				@out.Close();
			}
			{
				// decode
				IndexInput @in = d.OpenInput("test.bin", IOContext.READONCE);
				ForUtil forUtil = new ForUtil(@in);
				for (int i_1 = 0; i_1 < iterations; ++i_1)
				{
					if (Random().NextBoolean())
					{
						forUtil.SkipBlock(@in);
						continue;
					}
					int[] restored = new int[ForUtil.MAX_DATA_SIZE];
					forUtil.ReadBlock(@in, new byte[ForUtil.MAX_ENCODED_SIZE], restored);
					AssertArrayEquals(Arrays.CopyOfRange(values, i_1 * Lucene41PostingsFormat.BLOCK_SIZE
						, (i_1 + 1) * Lucene41PostingsFormat.BLOCK_SIZE), Arrays.CopyOf(restored, Lucene41PostingsFormat
						.BLOCK_SIZE));
				}
				NUnit.Framework.Assert.AreEqual(endPointer, @in.GetFilePointer());
				@in.Close();
			}
		}
	}
}
