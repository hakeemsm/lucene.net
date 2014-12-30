/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Com.Carrotsearch.Randomizedtesting.Generators;
using NUnit.Framework;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Packed;
using Sharpen;

namespace Lucene.Net.Util.Packed
{
	public class TestPackedInts : LuceneTestCase
	{
		//HM:revisit
		//import Lucene.Net.util.packed.PackedInts.FormatHelper;
		//HM:revisit. testcase commented out bcoz the jar doesnt have FormatHelper
		public virtual void TestBitsRequired()
		{
			AreEqual(61, PackedInts.BitsRequired((long)Math.Pow(2, 61)
				 - 1));
			AreEqual(61, PackedInts.BitsRequired(unchecked((long)(0x1FFFFFFFFFFFFFFFL
				))));
			AreEqual(62, PackedInts.BitsRequired(unchecked((long)(0x3FFFFFFFFFFFFFFFL
				))));
			AreEqual(63, PackedInts.BitsRequired(unchecked((long)(0x7FFFFFFFFFFFFFFFL
				))));
		}

		public virtual void TestMaxValues()
		{
			AreEqual("1 bit -> max == 1", 1, PackedInts.MaxValue(1));
			AreEqual("2 bit -> max == 3", 3, PackedInts.MaxValue(2));
			AreEqual("8 bit -> max == 255", 255, PackedInts.MaxValue(8
				));
			AreEqual("63 bit -> max == Long.MAX_VALUE", long.MaxValue, 
				PackedInts.MaxValue(63));
			AreEqual("64 bit -> max == Long.MAX_VALUE (same as for 63 bit)"
				, long.MaxValue, PackedInts.MaxValue(64));
		}

		//HM:revisit. testcase commented out bcoz the jar doesnt have FormatHelper
		//HM:revisit. testcase commented out bcoz the jar doesnt have FormatHelper
		public virtual void TestControlledEquality()
		{
			int VALUE_COUNT = 255;
			int BITS_PER_VALUE = 8;
			IList<PackedInts.Mutable> packedInts = CreatePackedInts(VALUE_COUNT, BITS_PER_VALUE
				);
			foreach (PackedInts.Mutable packedInt in packedInts)
			{
				for (int i = 0; i < packedInt.Size(); i++)
				{
					packedInt.Set(i, i + 1);
				}
			}
			AssertListEquality(packedInts);
		}

		public virtual void TestRandomBulkCopy()
		{
			int numIters = AtLeast(3);
			for (int iter = 0; iter < numIters; iter++)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("\nTEST: iter=" + iter);
				}
				int valueCount = AtLeast(100000);
				int bits1 = TestUtil.NextInt(Random(), 1, 64);
				int bits2 = TestUtil.NextInt(Random(), 1, 64);
				if (bits1 > bits2)
				{
					int tmp = bits1;
					bits1 = bits2;
					bits2 = tmp;
				}
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("  valueCount=" + valueCount + " bits1=" + bits1 + " bits2="
						 + bits2);
				}
				PackedInts.Mutable packed1 = PackedInts.GetMutable(valueCount, bits1, PackedInts.
					COMPACT);
				PackedInts.Mutable packed2 = PackedInts.GetMutable(valueCount, bits2, PackedInts.
					COMPACT);
				long maxValue = PackedInts.MaxValue(bits1);
				for (int i = 0; i < valueCount; i++)
				{
					long val = TestUtil.NextLong(Random(), 0, maxValue);
					packed1.Set(i, val);
					packed2.Set(i, val);
				}
				long[] buffer = new long[valueCount];
				// Copy random slice over, 20 times:
				for (int iter2 = 0; iter2 < 20; iter2++)
				{
					int start = Random().Next(valueCount - 1);
					int len = TestUtil.NextInt(Random(), 1, valueCount - start);
					int offset;
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("  copy " + len + " values @ " + start);
					}
					if (len == valueCount)
					{
						offset = 0;
					}
					else
					{
						offset = Random().Next(valueCount - len);
					}
					if (Random().NextBoolean())
					{
						int got = packed1.Get(start, buffer, offset, len);
						IsTrue(got <= len);
						int sot = packed2.Set(start, buffer, offset, got);
						IsTrue(sot <= got);
					}
					else
					{
						PackedInts.Copy(packed1, offset, packed2, offset, len, Random().Next(10 * len));
					}
				}
				for (int i_1 = 0; i_1 < valueCount; i_1++)
				{
					AreEqual("value " + i_1, packed1.Get(i_1), packed2.Get(i_1
						));
				}
			}
		}

		public virtual void TestRandomEquality()
		{
			int numIters = AtLeast(2);
			for (int i = 0; i < numIters; ++i)
			{
				int valueCount = TestUtil.NextInt(Random(), 1, 300);
				for (int bitsPerValue = 1; bitsPerValue <= 64; bitsPerValue++)
				{
					AssertRandomEquality(valueCount, bitsPerValue, Random().NextLong());
				}
			}
		}

		private static void AssertRandomEquality(int valueCount, int bitsPerValue, long randomSeed
			)
		{
			IList<PackedInts.Mutable> packedInts = CreatePackedInts(valueCount, bitsPerValue);
			foreach (PackedInts.Mutable packedInt in packedInts)
			{
				try
				{
					Fill(packedInt, PackedInts.MaxValue(bitsPerValue), randomSeed);
				}
				catch (Exception e)
				{
					Runtime.PrintStackTrace(e, System.Console.Error);
					Fail(string.Format(CultureInfo.ROOT, "Exception while filling %s: valueCount=%d, bitsPerValue=%s"
						, packedInt.GetType().Name, valueCount, bitsPerValue));
				}
			}
			AssertListEquality(packedInts);
		}

		private static IList<PackedInts.Mutable> CreatePackedInts(int valueCount, int bitsPerValue
			)
		{
			IList<PackedInts.Mutable> packedInts = new List<PackedInts.Mutable>();
			if (bitsPerValue <= 8)
			{
				packedInts.Add(new Direct8(valueCount));
			}
			if (bitsPerValue <= 16)
			{
				packedInts.Add(new Direct16(valueCount));
			}
			if (bitsPerValue <= 24 && valueCount <= Packed8ThreeBlocks.MAX_SIZE)
			{
				packedInts.Add(new Packed8ThreeBlocks(valueCount));
			}
			if (bitsPerValue <= 32)
			{
				packedInts.Add(new Direct32(valueCount));
			}
			if (bitsPerValue <= 48 && valueCount <= Packed16ThreeBlocks.MAX_SIZE)
			{
				packedInts.Add(new Packed16ThreeBlocks(valueCount));
			}
			if (bitsPerValue <= 63)
			{
				packedInts.Add(new Packed64(valueCount, bitsPerValue));
			}
			packedInts.Add(new Direct64(valueCount));
			for (int bpv = bitsPerValue; bpv <= Packed64SingleBlock.MAX_SUPPORTED_BITS_PER_VALUE
				; ++bpv)
			{
				if (Packed64SingleBlock.IsSupported(bpv))
				{
					packedInts.Add(Packed64SingleBlock.Create(valueCount, bpv));
				}
			}
			return packedInts;
		}

		private static void Fill(PackedInts.Mutable packedInt, long maxValue, long randomSeed
			)
		{
			Random rnd2 = new Random(randomSeed);
			for (int i = 0; i < packedInt.Size(); i++)
			{
				long value = TestUtil.NextLong(rnd2, 0, maxValue);
				packedInt.Set(i, value);
				AreEqual(string.Format(CultureInfo.ROOT, "The set/get of the value at index %d should match for %s"
					, i, packedInt.GetType().Name), value, packedInt.Get(i));
			}
		}

		private static void AssertListEquality<_T0>(IList<_T0> packedInts) where _T0:PackedInts.Reader
		{
			AssertListEquality(string.Empty, packedInts);
		}

		private static void AssertListEquality<_T0>(string message, IList<_T0> packedInts
			) where _T0:PackedInts.Reader
		{
			if (packedInts.Count == 0)
			{
				return;
			}
			PackedInts.Reader @base = packedInts[0];
			int valueCount = @base.Size();
			foreach (PackedInts.Reader packedInt in packedInts)
			{
				AreEqual(message + ". The number of values should be the same "
					, valueCount, packedInt.Size());
			}
			for (int i = 0; i < valueCount; i++)
			{
				for (int j = 1; j < packedInts.Count; j++)
				{
					AreEqual(string.Format(CultureInfo.ROOT, "%s. The value at index %d should be the same for %s and %s"
						, message, i, @base.GetType().Name, packedInts[j].GetType().Name), @base.Get(i), 
						packedInts[j].Get(i));
				}
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSingleValue()
		{
			for (int bitsPerValue = 1; bitsPerValue <= 64; ++bitsPerValue)
			{
				Directory dir = NewDirectory();
				IndexOutput @out = dir.CreateOutput("out", NewIOContext(Random()));
				PackedInts.Writer w = PackedInts.GetWriter(@out, 1, bitsPerValue, PackedInts.DEFAULT
					);
				long value = 17L & PackedInts.MaxValue(bitsPerValue);
				w.Add(value);
				w.Finish();
				long end = @out.FilePointer;
				@out.Dispose();
				IndexInput @in = dir.OpenInput("out", NewIOContext(Random()));
				PackedInts.Reader reader = PackedInts.GetReader(@in);
				string msg = "Impl=" + w.GetType().Name + ", bitsPerValue=" + bitsPerValue;
				AreEqual(msg, 1, reader.Size());
				AreEqual(msg, value, reader.Get(0));
				AreEqual(msg, end, @in.FilePointer);
				@in.Dispose();
				dir.Dispose();
			}
		}

		public virtual void TestSecondaryBlockChange()
		{
			PackedInts.Mutable mutable = new Packed64(26, 5);
			mutable.Set(24, 31);
			AreEqual("The value #24 should be correct", 31, mutable.Get
				(24));
			mutable.Set(4, 16);
			AreEqual("The value #24 should remain unchanged", 31, mutable
				.Get(24));
		}

		public virtual void TestIntOverflow()
		{
			int INDEX = (int)Math.Pow(2, 30) + 1;
			int BITS = 2;
			Packed64 p64 = null;
			try
			{
				p64 = new Packed64(INDEX, BITS);
			}
			catch (OutOfMemoryException)
			{
			}
			// This can easily happen: we're allocating a
			// long[] that needs 256-273 MB.  Heap is 512 MB,
			// but not all of that is available for large
			// objects ... empirical testing shows we only
			// have ~ 67 MB free.
			if (p64 != null)
			{
				p64.Set(INDEX - 1, 1);
				AreEqual("The value at position " + (INDEX - 1) + " should be correct for Packed64"
					, 1, p64.Get(INDEX - 1));
				p64 = null;
			}
			Packed64SingleBlock p64sb = null;
			try
			{
				p64sb = Packed64SingleBlock.Create(INDEX, BITS);
			}
			catch (OutOfMemoryException)
			{
			}
			// Ignore: see comment above
			if (p64sb != null)
			{
				p64sb.Set(INDEX - 1, 1);
				AreEqual("The value at position " + (INDEX - 1) + " should be correct for "
					 + p64sb.GetType().Name, 1, p64sb.Get(INDEX - 1));
			}
			int index = int.MaxValue / 24 + 1;
			Packed8ThreeBlocks p8 = null;
			try
			{
				p8 = new Packed8ThreeBlocks(index);
			}
			catch (OutOfMemoryException)
			{
			}
			// Ignore: see comment above
			if (p8 != null)
			{
				p8.Set(index - 1, 1);
				AreEqual("The value at position " + (index - 1) + " should be correct for Packed8ThreeBlocks"
					, 1, p8.Get(index - 1));
				p8 = null;
			}
			index = int.MaxValue / 48 + 1;
			Packed16ThreeBlocks p16 = null;
			try
			{
				p16 = new Packed16ThreeBlocks(index);
			}
			catch (OutOfMemoryException)
			{
			}
			// Ignore: see comment above
			if (p16 != null)
			{
				p16.Set(index - 1, 1);
				AreEqual("The value at position " + (index - 1) + " should be correct for Packed16ThreeBlocks"
					, 1, p16.Get(index - 1));
				p16 = null;
			}
		}

		public virtual void TestFill()
		{
			int valueCount = 1111;
			int from = Random().Next(valueCount + 1);
			int to = from + Random().Next(valueCount + 1 - from);
			for (int bpv = 1; bpv <= 64; ++bpv)
			{
				long val = TestUtil.NextLong(Random(), 0, PackedInts.MaxValue(bpv));
				IList<PackedInts.Mutable> packedInts = CreatePackedInts(valueCount, bpv);
				foreach (PackedInts.Mutable ints in packedInts)
				{
					string msg = ints.GetType().Name + " bpv=" + bpv + ", from=" + from + ", to=" + to
						 + ", val=" + val;
					ints.Fill(0, ints.Size(), 1);
					ints.Fill(from, to, val);
					for (int i = 0; i < ints.Size(); ++i)
					{
						if (i >= from && i < to)
						{
							AreEqual(msg + ", i=" + i, val, ints.Get(i));
						}
						else
						{
							AreEqual(msg + ", i=" + i, 1, ints.Get(i));
						}
					}
				}
			}
		}

		public virtual void TestPackedIntsNull()
		{
			// must be > 10 for the bulk reads below
			int size = TestUtil.NextInt(Random(), 11, 256);
			PackedInts.Reader packedInts = new PackedInts.NullReader(size);
			AreEqual(0, packedInts.Get(TestUtil.NextInt(Random(), 0, size
				 - 1)));
			long[] arr = new long[size + 10];
			int r;
			Arrays.Fill(arr, 1);
			r = packedInts.Get(0, arr, 0, size - 1);
			AreEqual(size - 1, r);
			for (r--; r >= 0; r--)
			{
				AreEqual(0, arr[r]);
			}
			Arrays.Fill(arr, 1);
			r = packedInts.Get(10, arr, 0, size + 10);
			AreEqual(size - 10, r);
			for (int i = 0; i < size - 10; i++)
			{
				AreEqual(0, arr[i]);
			}
		}

		public virtual void TestBulkGet()
		{
			int valueCount = 1111;
			int index = Random().Next(valueCount);
			int len = TestUtil.NextInt(Random(), 1, valueCount * 2);
			int off = Random().Next(77);
			for (int bpv = 1; bpv <= 64; ++bpv)
			{
				long mask = PackedInts.MaxValue(bpv);
				IList<PackedInts.Mutable> packedInts = CreatePackedInts(valueCount, bpv);
				foreach (PackedInts.Mutable ints in packedInts)
				{
					for (int i = 0; i < ints.Size(); ++i)
					{
						ints.Set(i, (31L * i - 1099) & mask);
					}
					long[] arr = new long[off + len];
					string msg = ints.GetType().Name + " valueCount=" + valueCount + ", index=" + index
						 + ", len=" + len + ", off=" + off;
					int gets = ints.Get(index, arr, off, len);
					IsTrue(msg, gets > 0);
					IsTrue(msg, gets <= len);
					IsTrue(msg, gets <= ints.Size() - index);
					for (int i_1 = 0; i_1 < arr.Length; ++i_1)
					{
						string m = msg + ", i=" + i_1;
						if (i_1 >= off && i_1 < off + gets)
						{
							AreEqual(m, ints.Get(i_1 - off + index), arr[i_1]);
						}
						else
						{
							AreEqual(m, 0, arr[i_1]);
						}
					}
				}
			}
		}

		public virtual void TestBulkSet()
		{
			int valueCount = 1111;
			int index = Random().Next(valueCount);
			int len = TestUtil.NextInt(Random(), 1, valueCount * 2);
			int off = Random().Next(77);
			long[] arr = new long[off + len];
			for (int bpv = 1; bpv <= 64; ++bpv)
			{
				long mask = PackedInts.MaxValue(bpv);
				IList<PackedInts.Mutable> packedInts = CreatePackedInts(valueCount, bpv);
				for (int i = 0; i < arr.Length; ++i)
				{
					arr[i] = (31L * i + 19) & mask;
				}
				foreach (PackedInts.Mutable ints in packedInts)
				{
					string msg = ints.GetType().Name + " valueCount=" + valueCount + ", index=" + index
						 + ", len=" + len + ", off=" + off;
					int sets = ints.Set(index, arr, off, len);
					IsTrue(msg, sets > 0);
					IsTrue(msg, sets <= len);
					for (int i_1 = 0; i_1 < ints.Size(); ++i_1)
					{
						string m = msg + ", i=" + i_1;
						if (i_1 >= index && i_1 < index + sets)
						{
							AreEqual(m, arr[off - index + i_1], ints.Get(i_1));
						}
						else
						{
							AreEqual(m, 0, ints.Get(i_1));
						}
					}
				}
			}
		}

		public virtual void TestCopy()
		{
			int valueCount = TestUtil.NextInt(Random(), 5, 600);
			int off1 = Random().Next(valueCount);
			int off2 = Random().Next(valueCount);
			int len = Random().Next(Math.Min(valueCount - off1, valueCount - off2));
			int mem = Random().Next(1024);
			for (int bpv = 1; bpv <= 64; ++bpv)
			{
				long mask = PackedInts.MaxValue(bpv);
				foreach (PackedInts.Mutable r1 in CreatePackedInts(valueCount, bpv))
				{
					for (int i = 0; i < r1.Size(); ++i)
					{
						r1.Set(i, (31L * i - 1023) & mask);
					}
					foreach (PackedInts.Mutable r2 in CreatePackedInts(valueCount, bpv))
					{
						string msg = "src=" + r1 + ", dest=" + r2 + ", srcPos=" + off1 + ", destPos=" + off2
							 + ", len=" + len + ", mem=" + mem;
						PackedInts.Copy(r1, off1, r2, off2, len, mem);
						for (int i_1 = 0; i_1 < r2.Size(); ++i_1)
						{
							string m = msg + ", i=" + i_1;
							if (i_1 >= off2 && i_1 < off2 + len)
							{
								AreEqual(m, r1.Get(i_1 - off2 + off1), r2.Get(i_1));
							}
							else
							{
								AreEqual(m, 0, r2.Get(i_1));
							}
						}
					}
				}
			}
		}

		public virtual void TestGrowableWriter()
		{
			int valueCount = 113 + Random().Next(1111);
			GrowableWriter wrt = new GrowableWriter(1, valueCount, PackedInts.DEFAULT);
			wrt.Set(4, 2);
			wrt.Set(7, 10);
			wrt.Set(valueCount - 10, 99);
			wrt.Set(99, 999);
			wrt.Set(valueCount - 1, 1 << 10);
			AreEqual(1 << 10, wrt.Get(valueCount - 1));
			wrt.Set(99, (1 << 23) - 1);
			AreEqual(1 << 10, wrt.Get(valueCount - 1));
			wrt.Set(1, long.MaxValue);
			wrt.Set(2, -3);
			AreEqual(64, wrt.GetBitsPerValue());
			AreEqual(1 << 10, wrt.Get(valueCount - 1));
			AreEqual(long.MaxValue, wrt.Get(1));
			AreEqual(-3L, wrt.Get(2));
			AreEqual(2, wrt.Get(4));
			AreEqual((1 << 23) - 1, wrt.Get(99));
			AreEqual(10, wrt.Get(7));
			AreEqual(99, wrt.Get(valueCount - 10));
			AreEqual(1 << 10, wrt.Get(valueCount - 1));
			AreEqual(RamUsageEstimator.SizeOf(wrt), wrt.RamBytesUsed()
				);
		}

		public virtual void TestPagedGrowableWriter()
		{
			int pageSize = 1 << (TestUtil.NextInt(Random(), 6, 30));
			// supports 0 values?
			PagedGrowableWriter writer = new PagedGrowableWriter(0, pageSize, TestUtil.NextInt
				(Random(), 1, 64), Random().NextFloat());
			AreEqual(0, writer.Size());
			// compare against AppendingDeltaPackedLongBuffer
			AppendingDeltaPackedLongBuffer buf = new AppendingDeltaPackedLongBuffer();
			int size = Random().Next(1000000);
			long max = 5;
			for (int i = 0; i < size; ++i)
			{
				buf.Add(TestUtil.NextLong(Random(), 0, max));
				if (Rarely())
				{
					max = PackedInts.MaxValue(Rarely() ? TestUtil.NextInt(Random(), 0, 63) : TestUtil
						.NextInt(Random(), 0, 31));
				}
			}
			writer = new PagedGrowableWriter(size, pageSize, TestUtil.NextInt(Random(), 1, 64
				), Random().NextFloat());
			AreEqual(size, writer.Size());
			for (int i_1 = size - 1; i_1 >= 0; --i_1)
			{
				writer.Set(i_1, buf.Get(i_1));
			}
			for (int i_2 = 0; i_2 < size; ++i_2)
			{
				AreEqual(buf.Get(i_2), writer.Get(i_2));
			}
			// test ramBytesUsed
			AreEqual(RamUsageEstimator.SizeOf(writer), writer.RamBytesUsed
				(), 8);
			// test copy
			PagedGrowableWriter copy = writer.Resize(TestUtil.NextLong(Random(), writer.Size(
				) / 2, writer.Size() * 3 / 2));
			for (long i_3 = 0; i_3 < copy.Size(); ++i_3)
			{
				if (i_3 < writer.Size())
				{
					AreEqual(writer.Get(i_3), copy.Get(i_3));
				}
				else
				{
					AreEqual(0, copy.Get(i_3));
				}
			}
			// test grow
			PagedGrowableWriter grow = writer.Grow(TestUtil.NextLong(Random(), writer.Size() 
				/ 2, writer.Size() * 3 / 2));
			for (long i_4 = 0; i_4 < grow.Size(); ++i_4)
			{
				if (i_4 < writer.Size())
				{
					AreEqual(writer.Get(i_4), grow.Get(i_4));
				}
				else
				{
					AreEqual(0, grow.Get(i_4));
				}
			}
		}

		public virtual void TestPagedMutable()
		{
			int bitsPerValue = TestUtil.NextInt(Random(), 1, 64);
			long max = PackedInts.MaxValue(bitsPerValue);
			int pageSize = 1 << (TestUtil.NextInt(Random(), 6, 30));
			// supports 0 values?
			PagedMutable writer = new PagedMutable(0, pageSize, bitsPerValue, Random().NextFloat
				() / 2);
			AreEqual(0, writer.Size());
			// compare against AppendingDeltaPackedLongBuffer
			AppendingDeltaPackedLongBuffer buf = new AppendingDeltaPackedLongBuffer();
			int size = Random().Next(1000000);
			for (int i = 0; i < size; ++i)
			{
				buf.Add(bitsPerValue == 64 ? Random().NextLong() : TestUtil.NextLong(Random(), 0, 
					max));
			}
			writer = new PagedMutable(size, pageSize, bitsPerValue, Random().NextFloat());
			AreEqual(size, writer.Size());
			for (int i_1 = size - 1; i_1 >= 0; --i_1)
			{
				writer.Set(i_1, buf.Get(i_1));
			}
			for (int i_2 = 0; i_2 < size; ++i_2)
			{
				AreEqual(buf.Get(i_2), writer.Get(i_2));
			}
			// test ramBytesUsed
			AreEqual(RamUsageEstimator.SizeOf(writer) - RamUsageEstimator
				.SizeOf(writer.format), writer.RamBytesUsed());
			// test copy
			PagedMutable copy = writer.Resize(TestUtil.NextLong(Random(), writer.Size() / 2, 
				writer.Size() * 3 / 2));
			for (long i_3 = 0; i_3 < copy.Size(); ++i_3)
			{
				if (i_3 < writer.Size())
				{
					AreEqual(writer.Get(i_3), copy.Get(i_3));
				}
				else
				{
					AreEqual(0, copy.Get(i_3));
				}
			}
			// test grow
			PagedMutable grow = writer.Grow(TestUtil.NextLong(Random(), writer.Size() / 2, writer
				.Size() * 3 / 2));
			for (long i_4 = 0; i_4 < grow.Size(); ++i_4)
			{
				if (i_4 < writer.Size())
				{
					AreEqual(writer.Get(i_4), grow.Get(i_4));
				}
				else
				{
					AreEqual(0, grow.Get(i_4));
				}
			}
		}

		// memory hole
		[Ignore]
		public virtual void TestPagedGrowableWriterOverflow()
		{
			long size = TestUtil.NextLong(Random(), 2 * (long)int.MaxValue, 3 * (long)int.MaxValue
				);
			int pageSize = 1 << (TestUtil.NextInt(Random(), 16, 30));
			PagedGrowableWriter writer = new PagedGrowableWriter(size, pageSize, 1, Random().
				NextFloat());
			long index = TestUtil.NextLong(Random(), (long)int.MaxValue, size - 1);
			writer.Set(index, 2);
			AreEqual(2, writer.Get(index));
			for (int i = 0; i < 1000000; ++i)
			{
				long idx = TestUtil.NextLong(Random(), 0, size);
				if (idx == index)
				{
					AreEqual(2, writer.Get(idx));
				}
				else
				{
					AreEqual(0, writer.Get(idx));
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestSave()
		{
			int valueCount = TestUtil.NextInt(Random(), 1, 2048);
			for (int bpv = 1; bpv <= 64; ++bpv)
			{
				int maxValue = (int)Math.Min(PackedInts.MaxValue(31), PackedInts.MaxValue(bpv));
				RAMDirectory directory = new RAMDirectory();
				IList<PackedInts.Mutable> packedInts = CreatePackedInts(valueCount, bpv);
				foreach (PackedInts.Mutable mutable in packedInts)
				{
					for (int i = 0; i < mutable.Size(); ++i)
					{
						mutable.Set(i, Random().Next(maxValue));
					}
					IndexOutput @out = directory.CreateOutput("packed-ints.bin", IOContext.DEFAULT);
					mutable.Save(@out);
					@out.Dispose();
					IndexInput @in = directory.OpenInput("packed-ints.bin", IOContext.DEFAULT);
					PackedInts.Reader reader = PackedInts.GetReader(@in);
					AreEqual(mutable.GetBitsPerValue(), reader.GetBitsPerValue
						());
					AreEqual(valueCount, reader.Size());
					if (mutable is Packed64SingleBlock)
					{
						// make sure that we used the right format so that the reader has
						// the same performance characteristics as the mutable that has been
						// serialized
						IsTrue(reader is Packed64SingleBlock);
					}
					else
					{
						IsFalse(reader is Packed64SingleBlock);
					}
					for (int i_1 = 0; i_1 < valueCount; ++i_1)
					{
						AreEqual(mutable.Get(i_1), reader.Get(i_1));
					}
					@in.Dispose();
					directory.DeleteFile("packed-ints.bin");
				}
				directory.Dispose();
			}
		}

		//HM:revisit. testcase commented out bcoz the jar doesnt have FormatHelper
		private static bool Equals(int[] ints, long[] longs)
		{
			if (ints.Length != longs.Length)
			{
				return false;
			}
			for (int i = 0; i < ints.Length; ++i)
			{
				if ((ints[i] & unchecked((long)(0xFFFFFFFFL))) != longs[i])
				{
					return false;
				}
			}
			return true;
		}

		internal enum DataType
		{
			PACKED,
			DELTA_PACKED,
			MONOTONIC
		}

		public virtual void TestAppendingLongBuffer()
		{
			long[] arr = new long[RandomInts.RandomIntBetween(Random(), 1, 1000000)];
			float[] ratioOptions = new float[] { PackedInts.DEFAULT, PackedInts.COMPACT, PackedInts
				.FAST };
			foreach (int bpv in new int[] { 0, 1, 63, 64, RandomInts.RandomIntBetween(Random(
				), 2, 62) })
			{
				foreach (TestPackedInts.DataType dataType in TestPackedInts.DataType.Values())
				{
					int pageSize = 1 << TestUtil.NextInt(Random(), 6, 20);
					int initialPageCount = TestUtil.NextInt(Random(), 0, 16);
					float acceptableOverheadRatio = ratioOptions[TestUtil.NextInt(Random(), 0, ratioOptions
						.Length - 1)];
					AbstractAppendingLongBuffer buf;
					int inc;
					switch (dataType)
					{
						case TestPackedInts.DataType.PACKED:
						{
							buf = new AppendingPackedLongBuffer(initialPageCount, pageSize, acceptableOverheadRatio
								);
							inc = 0;
							break;
						}

						case TestPackedInts.DataType.DELTA_PACKED:
						{
							buf = new AppendingDeltaPackedLongBuffer(initialPageCount, pageSize, acceptableOverheadRatio
								);
							inc = 0;
							break;
						}

						case TestPackedInts.DataType.MONOTONIC:
						{
							buf = new MonotonicAppendingLongBuffer(initialPageCount, pageSize, acceptableOverheadRatio
								);
							inc = TestUtil.NextInt(Random(), -1000, 1000);
							break;
						}

						default:
						{
							throw new SystemException("added a type and forgot to add it here?");
						}
					}
					if (bpv == 0)
					{
						arr[0] = Random().NextLong();
						for (int i = 1; i < arr.Length; ++i)
						{
							arr[i] = arr[i - 1] + inc;
						}
					}
					else
					{
						if (bpv == 64)
						{
							for (int i = 0; i < arr.Length; ++i)
							{
								arr[i] = Random().NextLong();
							}
						}
						else
						{
							long minValue = TestUtil.NextLong(Random(), long.MinValue, long.MaxValue - PackedInts
								.MaxValue(bpv));
							for (int i = 0; i < arr.Length; ++i)
							{
								arr[i] = minValue + inc * i + Random().NextLong() & PackedInts.MaxValue(bpv);
							}
						}
					}
					// TestUtil.nextLong is too slow
					for (int i_1 = 0; i_1 < arr.Length; ++i_1)
					{
						buf.Add(arr[i_1]);
					}
					AreEqual(arr.Length, buf.Size());
					if (Random().NextBoolean())
					{
						buf.Freeze();
						if (Random().NextBoolean())
						{
							// Make sure double freeze doesn't break anything
							buf.Freeze();
						}
					}
					AreEqual(arr.Length, buf.Size());
					for (int i_2 = 0; i_2 < arr.Length; ++i_2)
					{
						AreEqual(arr[i_2], buf.Get(i_2));
					}
					AbstractAppendingLongBuffer.IEnumerator it = buf.IEnumerator();
					for (int i_3 = 0; i_3 < arr.Length; ++i_3)
					{
						if (Random().NextBoolean())
						{
							IsTrue(it.HasNext());
						}
						AreEqual(arr[i_3], it.Next());
					}
					IsFalse(it.HasNext());
					long[] target = new long[arr.Length + 1024];
					// check the request for more is OK.
					for (int i_4 = 0; i_4 < arr.Length; i_4 += TestUtil.NextInt(Random(), 0, 10000))
					{
						int lenToRead = Random().Next(buf.PageSize() * 2) + 1;
						lenToRead = Math.Min(lenToRead, target.Length - i_4);
						int lenToCheck = Math.Min(lenToRead, arr.Length - i_4);
						int off = i_4;
						while (off < arr.Length && lenToRead > 0)
						{
							int read = buf.Get(off, target, off, lenToRead);
							IsTrue(read > 0);
							IsTrue(read <= lenToRead);
							lenToRead -= read;
							off += read;
						}
						for (int j = 0; j < lenToCheck; j++)
						{
							AreEqual(arr[j + i_4], target[j + i_4]);
						}
					}
					long expectedBytesUsed = RamUsageEstimator.SizeOf(buf);
					long computedBytesUsed = buf.RamBytesUsed();
					AreEqual(expectedBytesUsed, computedBytesUsed);
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestPackedInputOutput()
		{
			long[] longs = new long[Random().Next(8192)];
			int[] bitsPerValues = new int[longs.Length];
			bool[] skip = new bool[longs.Length];
			for (int i = 0; i < longs.Length; ++i)
			{
				int bpv = RandomInts.RandomIntBetween(Random(), 1, 64);
				bitsPerValues[i] = Random().NextBoolean() ? bpv : TestUtil.NextInt(Random(), bpv, 
					64);
				if (bpv == 64)
				{
					longs[i] = Random().NextLong();
				}
				else
				{
					longs[i] = TestUtil.NextLong(Random(), 0, PackedInts.MaxValue(bpv));
				}
				skip[i] = Rarely();
			}
			Directory dir = NewDirectory();
			IndexOutput @out = dir.CreateOutput("out.bin", IOContext.DEFAULT);
			PackedDataOutput pout = new PackedDataOutput(@out);
			long totalBits = 0;
			for (int i_1 = 0; i_1 < longs.Length; ++i_1)
			{
				pout.WriteLong(longs[i_1], bitsPerValues[i_1]);
				totalBits += bitsPerValues[i_1];
				if (skip[i_1])
				{
					pout.Flush();
					totalBits = 8 * (long)Math.Ceil((double)totalBits / 8);
				}
			}
			pout.Flush();
			AreEqual((long)Math.Ceil((double)totalBits / 8), @out.GetFilePointer
				());
			@out.Dispose();
			IndexInput @in = dir.OpenInput("out.bin", IOContext.READONCE);
			PackedDataInput pin = new PackedDataInput(@in);
			for (int i_2 = 0; i_2 < longs.Length; ++i_2)
			{
				AreEqual(string.Empty + i_2, longs[i_2], pin.ReadLong(bitsPerValues
					[i_2]));
				if (skip[i_2])
				{
					pin.SkipToNextByte();
				}
			}
			AreEqual((long)Math.Ceil((double)totalBits / 8), @in.GetFilePointer
				());
			@in.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestBlockPackedReaderWriter()
		{
			int iters = AtLeast(2);
			for (int iter = 0; iter < iters; ++iter)
			{
				int blockSize = 1 << TestUtil.NextInt(Random(), 6, 18);
				int valueCount = Random().Next(1 << 18);
				long[] values = new long[valueCount];
				long minValue = 0;
				int bpv = 0;
				for (int i = 0; i < valueCount; ++i)
				{
					if (i % blockSize == 0)
					{
						minValue = Rarely() ? Random().Next(256) : Rarely() ? -5 : Random().NextLong();
						bpv = Random().Next(65);
					}
					if (bpv == 0)
					{
						values[i] = minValue;
					}
					else
					{
						if (bpv == 64)
						{
							values[i] = Random().NextLong();
						}
						else
						{
							values[i] = minValue + TestUtil.NextLong(Random(), 0, (1L << bpv) - 1);
						}
					}
				}
				Directory dir = NewDirectory();
				IndexOutput @out = dir.CreateOutput("out.bin", IOContext.DEFAULT);
				BlockPackedWriter writer = new BlockPackedWriter(@out, blockSize);
				for (int i_1 = 0; i_1 < valueCount; ++i_1)
				{
					AreEqual(i_1, writer.Ord());
					writer.Add(values[i_1]);
				}
				AreEqual(valueCount, writer.Ord());
				writer.Finish();
				AreEqual(valueCount, writer.Ord());
				long fp = @out.FilePointer;
				@out.Dispose();
				IndexInput in1 = dir.OpenInput("out.bin", IOContext.DEFAULT);
				byte[] buf = new byte[(int)fp];
				in1.ReadBytes(buf, 0, (int)fp);
				in1.Seek(0L);
				ByteArrayDataInput in2 = new ByteArrayDataInput(buf);
				DataInput @in = Random().NextBoolean() ? in1 : in2;
				BlockPackedReaderIterator it = new BlockPackedReaderIterator(@in, PackedInts.VERSION_CURRENT
					, blockSize, valueCount);
				for (int i_2 = 0; i_2 < valueCount; )
				{
					if (Random().NextBoolean())
					{
						AreEqual(string.Empty + i_2, values[i_2], it.Next());
						++i_2;
					}
					else
					{
						LongsRef nextValues = it.Next(TestUtil.NextInt(Random(), 1, 1024));
						for (int j = 0; j < nextValues.length; ++j)
						{
							AreEqual(string.Empty + (i_2 + j), values[i_2 + j], nextValues
								.longs[nextValues.offset + j]);
						}
						i_2 += nextValues.length;
					}
					AreEqual(i_2, it.Ord());
				}
				AreEqual(fp, @in is ByteArrayDataInput ? ((ByteArrayDataInput
					)@in).GetPosition() : ((IndexInput)@in).FilePointer);
				try
				{
					it.Next();
					IsTrue(false);
				}
				catch (IOException)
				{
				}
				// OK
				if (@in is ByteArrayDataInput)
				{
					((ByteArrayDataInput)@in).SetPosition(0);
				}
				else
				{
					((IndexInput)@in).Seek(0L);
				}
				BlockPackedReaderIterator it2 = new BlockPackedReaderIterator(@in, PackedInts.VERSION_CURRENT
					, blockSize, valueCount);
				int i_3 = 0;
				while (true)
				{
					int skip = TestUtil.NextInt(Random(), 0, valueCount - i_3);
					it2.Skip(skip);
					i_3 += skip;
					AreEqual(i_3, it2.Ord());
					if (i_3 == valueCount)
					{
						break;
					}
					else
					{
						AreEqual(values[i_3], it2.Next());
						++i_3;
					}
				}
				AreEqual(fp, @in is ByteArrayDataInput ? ((ByteArrayDataInput
					)@in).GetPosition() : ((IndexInput)@in).FilePointer);
				try
				{
					it2.Skip(1);
					IsTrue(false);
				}
				catch (IOException)
				{
				}
				// OK
				in1.Seek(0L);
				BlockPackedReader reader = new BlockPackedReader(in1, PackedInts.VERSION_CURRENT, 
					blockSize, valueCount, Random().NextBoolean());
				AreEqual(in1.FilePointer, in1.Length());
				for (i_3 = 0; i_3 < valueCount; ++i_3)
				{
					AreEqual("i=" + i_3, values[i_3], reader.Get(i_3));
				}
				in1.Dispose();
				dir.Dispose();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestMonotonicBlockPackedReaderWriter()
		{
			int iters = AtLeast(2);
			for (int iter = 0; iter < iters; ++iter)
			{
				int blockSize = 1 << TestUtil.NextInt(Random(), 6, 18);
				int valueCount = Random().Next(1 << 18);
				long[] values = new long[valueCount];
				if (valueCount > 0)
				{
					values[0] = Random().NextBoolean() ? Random().Next(10) : Random().Next(int.MaxValue
						);
					int maxDelta = Random().Next(64);
					for (int i = 1; i < valueCount; ++i)
					{
						if (Random().NextDouble() < 0.1d)
						{
							maxDelta = Random().Next(64);
						}
						values[i] = Math.Max(0, values[i - 1] + TestUtil.NextInt(Random(), -16, maxDelta)
							);
					}
				}
				Directory dir = NewDirectory();
				IndexOutput @out = dir.CreateOutput("out.bin", IOContext.DEFAULT);
				MonotonicBlockPackedWriter writer = new MonotonicBlockPackedWriter(@out, blockSize
					);
				for (int i_1 = 0; i_1 < valueCount; ++i_1)
				{
					AreEqual(i_1, writer.Ord());
					writer.Add(values[i_1]);
				}
				AreEqual(valueCount, writer.Ord());
				writer.Finish();
				AreEqual(valueCount, writer.Ord());
				long fp = @out.FilePointer;
				@out.Dispose();
				IndexInput @in = dir.OpenInput("out.bin", IOContext.DEFAULT);
				MonotonicBlockPackedReader reader = new MonotonicBlockPackedReader(@in, PackedInts
					.VERSION_CURRENT, blockSize, valueCount, Random().NextBoolean());
				AreEqual(fp, @in.FilePointer);
				for (int i_2 = 0; i_2 < valueCount; ++i_2)
				{
					AreEqual("i=" + i_2, values[i_2], reader.Get(i_2));
				}
				@in.Dispose();
				dir.Dispose();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		[LuceneTestCase.Nightly]
		public virtual void TestBlockReaderOverflow()
		{
			long valueCount = TestUtil.NextLong(Random(), 1L + int.MaxValue, (long)int.MaxValue
				 * 2);
			int blockSize = 1 << TestUtil.NextInt(Random(), 20, 22);
			Directory dir = NewDirectory();
			IndexOutput @out = dir.CreateOutput("out.bin", IOContext.DEFAULT);
			BlockPackedWriter writer = new BlockPackedWriter(@out, blockSize);
			long value = Random().Next() & unchecked((long)(0xFFFFFFFFL));
			long valueOffset = TestUtil.NextLong(Random(), 0, valueCount - 1);
			for (long i = 0; i < valueCount; )
			{
				AreEqual(i, writer.Ord());
				if ((i & (blockSize - 1)) == 0 && (i + blockSize < valueOffset || i > valueOffset
					 && i + blockSize < valueCount))
				{
					writer.AddBlockOfZeros();
					i += blockSize;
				}
				else
				{
					if (i == valueOffset)
					{
						writer.Add(value);
						++i;
					}
					else
					{
						writer.Add(0);
						++i;
					}
				}
			}
			writer.Finish();
			@out.Dispose();
			IndexInput @in = dir.OpenInput("out.bin", IOContext.DEFAULT);
			BlockPackedReaderIterator it = new BlockPackedReaderIterator(@in, PackedInts.VERSION_CURRENT
				, blockSize, valueCount);
			it.Skip(valueOffset);
			AreEqual(value, it.Next());
			@in.Seek(0L);
			BlockPackedReader reader = new BlockPackedReader(@in, PackedInts.VERSION_CURRENT, 
				blockSize, valueCount, Random().NextBoolean());
			AreEqual(value, reader.Get(valueOffset));
			for (int i_1 = 0; i_1 < 5; ++i_1)
			{
				long offset = TestUtil.NextLong(Random(), 0, valueCount - 1);
				if (offset == valueOffset)
				{
					AreEqual(value, reader.Get(offset));
				}
				else
				{
					AreEqual(0, reader.Get(offset));
				}
			}
			@in.Dispose();
			dir.Dispose();
		}
	}
}
