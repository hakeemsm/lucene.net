/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Util;
using Lucene.Net.Util.Packed;
using Sharpen;

namespace Lucene.Net.Util.Packed
{
	public class TestEliasFanoSequence : LuceneTestCase
	{
		private static EliasFanoEncoder MakeEncoder(long[] values, long indexInterval)
		{
			long upperBound = -1L;
			foreach (long value in values)
			{
				NUnit.Framework.Assert.IsTrue(value >= upperBound);
				// test data ok
				upperBound = value;
			}
			EliasFanoEncoder efEncoder = new EliasFanoEncoder(values.Length, upperBound, indexInterval
				);
			foreach (long value_1 in values)
			{
				efEncoder.EncodeNext(value_1);
			}
			return efEncoder;
		}

		private static void TstDecodeAllNext(long[] values, EliasFanoDecoder efd)
		{
			efd.ToBeforeSequence();
			long nextValue = efd.NextValue();
			foreach (long expValue in values)
			{
				NUnit.Framework.Assert.IsFalse("nextValue at end too early", EliasFanoDecoder.NO_MORE_VALUES
					 == nextValue);
				NUnit.Framework.Assert.AreEqual(expValue, nextValue);
				nextValue = efd.NextValue();
			}
			NUnit.Framework.Assert.AreEqual(EliasFanoDecoder.NO_MORE_VALUES, nextValue);
		}

		private static void TstDecodeAllPrev(long[] values, EliasFanoDecoder efd)
		{
			efd.ToAfterSequence();
			for (int i = values.Length - 1; i >= 0; i--)
			{
				long previousValue = efd.PreviousValue();
				NUnit.Framework.Assert.IsFalse("previousValue at end too early", EliasFanoDecoder
					.NO_MORE_VALUES == previousValue);
				NUnit.Framework.Assert.AreEqual(values[i], previousValue);
			}
			NUnit.Framework.Assert.AreEqual(EliasFanoDecoder.NO_MORE_VALUES, efd.PreviousValue
				());
		}

		private static void TstDecodeAllAdvanceToExpected(long[] values, EliasFanoDecoder
			 efd)
		{
			efd.ToBeforeSequence();
			long previousValue = -1L;
			long index = 0;
			foreach (long expValue in values)
			{
				if (expValue > previousValue)
				{
					long advanceValue = efd.AdvanceToValue(expValue);
					NUnit.Framework.Assert.IsFalse("advanceValue at end too early", EliasFanoDecoder.
						NO_MORE_VALUES == advanceValue);
					NUnit.Framework.Assert.AreEqual(expValue, advanceValue);
					NUnit.Framework.Assert.AreEqual(index, efd.CurrentIndex());
					previousValue = expValue;
				}
				index++;
			}
			long advanceValue_1 = efd.AdvanceToValue(previousValue + 1);
			NUnit.Framework.Assert.AreEqual("at end", EliasFanoDecoder.NO_MORE_VALUES, advanceValue_1
				);
		}

		private static void TstDecodeAdvanceToMultiples(long[] values, EliasFanoDecoder efd
			, long m)
		{
			// test advancing to multiples of m
			//HM:revisit 
			//assert m > 0;
			long previousValue = -1L;
			long index = 0;
			long mm = m;
			efd.ToBeforeSequence();
			foreach (long expValue in values)
			{
				// mm > previousValue
				if (expValue >= mm)
				{
					long advanceValue = efd.AdvanceToValue(mm);
					NUnit.Framework.Assert.IsFalse("advanceValue at end too early", EliasFanoDecoder.
						NO_MORE_VALUES == advanceValue);
					NUnit.Framework.Assert.AreEqual(expValue, advanceValue);
					NUnit.Framework.Assert.AreEqual(index, efd.CurrentIndex());
					previousValue = expValue;
					do
					{
						mm += m;
					}
					while (mm <= previousValue);
				}
				index++;
			}
			long advanceValue_1 = efd.AdvanceToValue(mm);
			NUnit.Framework.Assert.AreEqual(EliasFanoDecoder.NO_MORE_VALUES, advanceValue_1);
		}

		private static void TstDecodeBackToMultiples(long[] values, EliasFanoDecoder efd, 
			long m)
		{
			// test backing to multiples of m
			//HM:revisit 
			//assert m > 0;
			efd.ToAfterSequence();
			int index = values.Length - 1;
			if (index < 0)
			{
				long advanceValue = efd.BackToValue(0);
				NUnit.Framework.Assert.AreEqual(EliasFanoDecoder.NO_MORE_VALUES, advanceValue);
				return;
			}
			// empty values, nothing to go back to/from
			long expValue = values[index];
			long previousValue = expValue + 1;
			long mm = (expValue / m) * m;
			while (index >= 0)
			{
				expValue = values[index];
				//HM:revisit 
				//assert mm < previousValue;
				if (expValue <= mm)
				{
					long backValue = efd.BackToValue(mm);
					NUnit.Framework.Assert.IsFalse("backToValue at end too early", EliasFanoDecoder.NO_MORE_VALUES
						 == backValue);
					NUnit.Framework.Assert.AreEqual(expValue, backValue);
					NUnit.Framework.Assert.AreEqual(index, efd.CurrentIndex());
					previousValue = expValue;
					do
					{
						mm -= m;
					}
					while (mm >= previousValue);
				}
				index--;
			}
			long backValue_1 = efd.BackToValue(mm);
			NUnit.Framework.Assert.AreEqual(EliasFanoDecoder.NO_MORE_VALUES, backValue_1);
		}

		private static void TstEqual(string mes, long[] exp, long[] act)
		{
			NUnit.Framework.Assert.AreEqual(mes + ".length", exp.Length, act.Length);
			for (int i = 0; i < exp.Length; i++)
			{
				if (exp[i] != act[i])
				{
					NUnit.Framework.Assert.Fail(mes + "[" + i + "] " + exp[i] + " != " + act[i]);
				}
			}
		}

		private static void TstDecodeAll(EliasFanoEncoder efEncoder, long[] values)
		{
			TstDecodeAllNext(values, efEncoder.GetDecoder());
			TstDecodeAllPrev(values, efEncoder.GetDecoder());
			TstDecodeAllAdvanceToExpected(values, efEncoder.GetDecoder());
		}

		private static void TstEFS(long[] values, long[] expHighLongs, long[] expLowLongs
			)
		{
			EliasFanoEncoder efEncoder = MakeEncoder(values, EliasFanoEncoder.DEFAULT_INDEX_INTERVAL
				);
			TstEqual("upperBits", expHighLongs, efEncoder.GetUpperBits());
			TstEqual("lowerBits", expLowLongs, efEncoder.GetLowerBits());
			TstDecodeAll(efEncoder, values);
		}

		private static void TstEFS2(long[] values)
		{
			EliasFanoEncoder efEncoder = MakeEncoder(values, EliasFanoEncoder.DEFAULT_INDEX_INTERVAL
				);
			TstDecodeAll(efEncoder, values);
		}

		private static void TstEFSadvanceToAndBackToMultiples(long[] values, long maxValue
			, long minAdvanceMultiple)
		{
			EliasFanoEncoder efEncoder = MakeEncoder(values, EliasFanoEncoder.DEFAULT_INDEX_INTERVAL
				);
			for (long m = minAdvanceMultiple; m <= maxValue; m += 1)
			{
				TstDecodeAdvanceToMultiples(values, efEncoder.GetDecoder(), m);
				TstDecodeBackToMultiples(values, efEncoder.GetDecoder(), m);
			}
		}

		private EliasFanoEncoder TstEFVI(long[] values, long indexInterval, long[] expIndexBits
			)
		{
			EliasFanoEncoder efEncVI = MakeEncoder(values, indexInterval);
			TstEqual("upperZeroBitPositionIndex", expIndexBits, efEncVI.GetIndexBits());
			return efEncVI;
		}

		public virtual void TestEmpty()
		{
			long[] values = new long[0];
			long[] expHighBits = new long[0];
			long[] expLowBits = new long[0];
			TstEFS(values, expHighBits, expLowBits);
		}

		public virtual void TestOneValue1()
		{
			long[] values = new long[] { 0 };
			long[] expHighBits = new long[] { unchecked((long)(0x1L)) };
			long[] expLowBits = new long[] {  };
			TstEFS(values, expHighBits, expLowBits);
		}

		public virtual void TestTwoValues1()
		{
			long[] values = new long[] { 0, 0 };
			long[] expHighBits = new long[] { unchecked((long)(0x3L)) };
			long[] expLowBits = new long[] {  };
			TstEFS(values, expHighBits, expLowBits);
		}

		public virtual void TestOneValue2()
		{
			long[] values = new long[] { 63 };
			long[] expHighBits = new long[] { 2 };
			long[] expLowBits = new long[] { 31 };
			TstEFS(values, expHighBits, expLowBits);
		}

		public virtual void TestOneMaxValue()
		{
			long[] values = new long[] { long.MaxValue };
			long[] expHighBits = new long[] { 2 };
			long[] expLowBits = new long[] { long.MaxValue / 2 };
			TstEFS(values, expHighBits, expLowBits);
		}

		public virtual void TestTwoMinMaxValues()
		{
			long[] values = new long[] { 0, long.MaxValue };
			long[] expHighBits = new long[] { unchecked((int)(0x11)) };
			long[] expLowBits = new long[] { unchecked((long)(0xE000000000000000L)), unchecked(
				(long)(0x03FFFFFFFFFFFFFFL)) };
			TstEFS(values, expHighBits, expLowBits);
		}

		public virtual void TestTwoMaxValues()
		{
			long[] values = new long[] { long.MaxValue, long.MaxValue };
			long[] expHighBits = new long[] { unchecked((int)(0x18)) };
			long[] expLowBits = new long[] { -1L, unchecked((long)(0x03FFFFFFFFFFFFFFL)) };
			TstEFS(values, expHighBits, expLowBits);
		}

		public virtual void TestExample1()
		{
			// Figure 1 from Vigna 2012 paper
			long[] values = new long[] { 5, 8, 8, 15, 32 };
			long[] expLowBits = new long[] { long.Parse("0011000001", 2) };
			// reverse block and bit order
			long[] expHighBits = new long[] { long.Parse("1000001011010", 2) };
			// reverse block and bit order
			TstEFS(values, expHighBits, expLowBits);
		}

		public virtual void TestHashCodeEquals()
		{
			long[] values = new long[] { 5, 8, 8, 15, 32 };
			EliasFanoEncoder efEncoder1 = MakeEncoder(values, EliasFanoEncoder.DEFAULT_INDEX_INTERVAL
				);
			EliasFanoEncoder efEncoder2 = MakeEncoder(values, EliasFanoEncoder.DEFAULT_INDEX_INTERVAL
				);
			NUnit.Framework.Assert.AreEqual(efEncoder1, efEncoder2);
			NUnit.Framework.Assert.AreEqual(efEncoder1.GetHashCode(), efEncoder2.GetHashCode(
				));
			EliasFanoEncoder efEncoder3 = MakeEncoder(new long[] { 1, 2, 3 }, EliasFanoEncoder
				.DEFAULT_INDEX_INTERVAL);
			NUnit.Framework.Assert.IsFalse(efEncoder1.Equals(efEncoder3));
			NUnit.Framework.Assert.IsFalse(efEncoder3.Equals(efEncoder1));
			NUnit.Framework.Assert.IsFalse(efEncoder1.GetHashCode() == efEncoder3.GetHashCode
				());
		}

		// implementation ok for these.
		public virtual void TestMonotoneSequences()
		{
			//for (int s = 2; s < 1222; s++) {
			for (int s = 2; s < 4422; s++)
			{
				long[] values = new long[s];
				for (int i = 0; i < s; i++)
				{
					values[i] = (i / 2);
				}
				// upperbound smaller than number of values, only upper bits encoded
				TstEFS2(values);
			}
		}

		public virtual void TestStrictMonotoneSequences()
		{
			// for (int s = 2; s < 1222; s++) {
			for (int s = 2; s < 4422; s++)
			{
				long[] values = new long[s];
				for (int i = 0; i < s; i++)
				{
					values[i] = i * ((long)i - 1) / 2;
				}
				// Add a gap of (s-1) to previous
				// s = (s*(s+1) - (s-1)*s)/2
				TstEFS2(values);
			}
		}

		public virtual void TestHighBitLongZero()
		{
			int s = 65;
			long[] values = new long[s];
			for (int i = 0; i < s - 1; i++)
			{
				values[i] = 0;
			}
			values[s - 1] = 128;
			long[] expHighBits = new long[] { -1, 0, 0, 1 };
			long[] expLowBits = new long[0];
			TstEFS(values, expHighBits, expLowBits);
		}

		public virtual void TestAdvanceToAndBackToMultiples()
		{
			for (int s = 2; s < 130; s++)
			{
				long[] values = new long[s];
				for (int i = 0; i < s; i++)
				{
					values[i] = i * ((long)i + 1) / 2;
				}
				// Add a gap of s to previous
				// s = (s*(s+1) - (s-1)*s)/2
				TstEFSadvanceToAndBackToMultiples(values, values[s - 1], 10);
			}
		}

		public virtual void TestEmptyIndex()
		{
			long indexInterval = 2;
			long[] emptyLongs = new long[0];
			TstEFVI(emptyLongs, indexInterval, emptyLongs);
		}

		public virtual void TestMaxContentEmptyIndex()
		{
			long indexInterval = 2;
			long[] twoLongs = new long[] { 0, 1 };
			long[] emptyLongs = new long[0];
			TstEFVI(twoLongs, indexInterval, emptyLongs);
		}

		public virtual void TestMinContentNonEmptyIndex()
		{
			long indexInterval = 2;
			long[] twoLongs = new long[] { 0, 2 };
			long[] indexLongs = new long[] { 3 };
			// high bits 1001, index position after zero bit.
			TstEFVI(twoLongs, indexInterval, indexLongs);
		}

		public virtual void TestIndexAdvanceToLast()
		{
			long indexInterval = 2;
			long[] twoLongs = new long[] { 0, 2 };
			long[] indexLongs = new long[] { 3 };
			// high bits 1001
			EliasFanoEncoder efEncVI = TstEFVI(twoLongs, indexInterval, indexLongs);
			NUnit.Framework.Assert.AreEqual(2, efEncVI.GetDecoder().AdvanceToValue(2));
		}

		public virtual void TestIndexAdvanceToAfterLast()
		{
			long indexInterval = 2;
			long[] twoLongs = new long[] { 0, 2 };
			long[] indexLongs = new long[] { 3 };
			// high bits 1001
			EliasFanoEncoder efEncVI = TstEFVI(twoLongs, indexInterval, indexLongs);
			NUnit.Framework.Assert.AreEqual(EliasFanoDecoder.NO_MORE_VALUES, efEncVI.GetDecoder
				().AdvanceToValue(3));
		}

		public virtual void TestIndexAdvanceToFirst()
		{
			long indexInterval = 2;
			long[] twoLongs = new long[] { 0, 2 };
			long[] indexLongs = new long[] { 3 };
			// high bits 1001
			EliasFanoEncoder efEncVI = TstEFVI(twoLongs, indexInterval, indexLongs);
			NUnit.Framework.Assert.AreEqual(0, efEncVI.GetDecoder().AdvanceToValue(0));
		}

		public virtual void TestTwoIndexEntries()
		{
			long indexInterval = 2;
			long[] twoLongs = new long[] { 0, 1, 2, 3, 4, 5 };
			long[] indexLongs = new long[] { 4 + 8 * 16 };
			// high bits 0b10101010101
			EliasFanoEncoder efEncVI = TstEFVI(twoLongs, indexInterval, indexLongs);
			EliasFanoDecoder efDecVI = efEncVI.GetDecoder();
			NUnit.Framework.Assert.AreEqual("advance 0", 0, efDecVI.AdvanceToValue(0));
			NUnit.Framework.Assert.AreEqual("advance 5", 5, efDecVI.AdvanceToValue(5));
			NUnit.Framework.Assert.AreEqual("advance 6", EliasFanoDecoder.NO_MORE_VALUES, efDecVI
				.AdvanceToValue(5));
		}

		public virtual void TestExample2a()
		{
			// Figure 2 from Vigna 2012 paper
			long indexInterval = 4;
			long[] values = new long[] { 5, 8, 8, 15, 32 };
			// two low bits, high values 1,2,2,3,8.
			long[] indexLongs = new long[] { 8 + 12 * 16 };
			// high bits 0b 0001 0000 0101 1010
			EliasFanoEncoder efEncVI = TstEFVI(values, indexInterval, indexLongs);
			EliasFanoDecoder efDecVI = efEncVI.GetDecoder();
			NUnit.Framework.Assert.AreEqual("advance 22", 32, efDecVI.AdvanceToValue(22));
		}

		public virtual void TestExample2b()
		{
			// Figure 2 from Vigna 2012 paper
			long indexInterval = 4;
			long[] values = new long[] { 5, 8, 8, 15, 32 };
			// two low bits, high values 1,2,2,3,8.
			long[] indexLongs = new long[] { 8 + 12 * 16 };
			// high bits 0b 0001 0000 0101 1010
			EliasFanoEncoder efEncVI = TstEFVI(values, indexInterval, indexLongs);
			EliasFanoDecoder efDecVI = efEncVI.GetDecoder();
			NUnit.Framework.Assert.AreEqual("initial next", 5, efDecVI.NextValue());
			NUnit.Framework.Assert.AreEqual("advance 22", 32, efDecVI.AdvanceToValue(22));
		}

		public virtual void TestExample2NoIndex1()
		{
			// Figure 2 from Vigna 2012 paper, no index, test broadword selection.
			long indexInterval = 16;
			long[] values = new long[] { 5, 8, 8, 15, 32 };
			// two low bits, high values 1,2,2,3,8.
			long[] indexLongs = new long[0];
			// high bits 0b 0001 0000 0101 1010
			EliasFanoEncoder efEncVI = TstEFVI(values, indexInterval, indexLongs);
			EliasFanoDecoder efDecVI = efEncVI.GetDecoder();
			NUnit.Framework.Assert.AreEqual("advance 22", 32, efDecVI.AdvanceToValue(22));
		}

		public virtual void TestExample2NoIndex2()
		{
			// Figure 2 from Vigna 2012 paper, no index, test broadword selection.
			long indexInterval = 16;
			long[] values = new long[] { 5, 8, 8, 15, 32 };
			// two low bits, high values 1,2,2,3,8.
			long[] indexLongs = new long[0];
			// high bits 0b 0001 0000 0101 1010
			EliasFanoEncoder efEncVI = TstEFVI(values, indexInterval, indexLongs);
			EliasFanoDecoder efDecVI = efEncVI.GetDecoder();
			NUnit.Framework.Assert.AreEqual("initial next", 5, efDecVI.NextValue());
			NUnit.Framework.Assert.AreEqual("advance 22", 32, efDecVI.AdvanceToValue(22));
		}
	}
}
