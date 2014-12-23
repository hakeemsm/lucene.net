/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Util
{
	public class TestLongBitSet : LuceneTestCase
	{
		internal virtual void DoGet(BitSet a, LongBitSet b)
		{
			long max = b.Length();
			for (int i = 0; i < max; i++)
			{
				if (a.Get(i) != b.Get(i))
				{
					NUnit.Framework.Assert.Fail("mismatch: BitSet=[" + i + "]=" + a.Get(i));
				}
			}
		}

		internal virtual void DoNextSetBit(BitSet a, LongBitSet b)
		{
			int aa = -1;
			long bb = -1;
			do
			{
				aa = a.NextSetBit(aa + 1);
				bb = bb < b.Length() - 1 ? b.NextSetBit(bb + 1) : -1;
				NUnit.Framework.Assert.AreEqual(aa, bb);
			}
			while (aa >= 0);
		}

		internal virtual void DoPrevSetBit(BitSet a, LongBitSet b)
		{
			int aa = a.Size() + Random().Next(100);
			long bb = aa;
			do
			{
				// aa = a.prevSetBit(aa-1);
				aa--;
				while ((aa >= 0) && (!a.Get(aa)))
				{
					aa--;
				}
				if (b.Length() == 0)
				{
					bb = -1;
				}
				else
				{
					if (bb > b.Length() - 1)
					{
						bb = b.PrevSetBit(b.Length() - 1);
					}
					else
					{
						if (bb < 1)
						{
							bb = -1;
						}
						else
						{
							bb = bb >= 1 ? b.PrevSetBit(bb - 1) : -1;
						}
					}
				}
				NUnit.Framework.Assert.AreEqual(aa, bb);
			}
			while (aa >= 0);
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void DoRandomSets(int maxSize, int iter, int mode)
		{
			BitSet a0 = null;
			LongBitSet b0 = null;
			for (int i = 0; i < iter; i++)
			{
				int sz = TestUtil.NextInt(Random(), 2, maxSize);
				BitSet a = new BitSet(sz);
				LongBitSet b = new LongBitSet(sz);
				// test the various ways of setting bits
				if (sz > 0)
				{
					int nOper = Random().Next(sz);
					for (int j = 0; j < nOper; j++)
					{
						int idx;
						idx = Random().Next(sz);
						a.Set(idx);
						b.Set(idx);
						idx = Random().Next(sz);
						a.Clear(idx);
						b.Clear(idx);
						idx = Random().Next(sz);
						a.Flip(idx);
						b.Flip(idx, idx + 1);
						idx = Random().Next(sz);
						a.Flip(idx);
						b.Flip(idx, idx + 1);
						bool val2 = b.Get(idx);
						bool val = b.GetAndSet(idx);
						NUnit.Framework.Assert.IsTrue(val2 == val);
						NUnit.Framework.Assert.IsTrue(b.Get(idx));
						if (!val)
						{
							b.Clear(idx);
						}
						NUnit.Framework.Assert.IsTrue(b.Get(idx) == val);
					}
				}
				// test that the various ways of accessing the bits are equivalent
				DoGet(a, b);
				// test ranges, including possible extension
				int fromIndex;
				int toIndex;
				fromIndex = Random().Next(sz / 2);
				toIndex = fromIndex + Random().Next(sz - fromIndex);
				BitSet aa = (BitSet)a.Clone();
				aa.Flip(fromIndex, toIndex);
				LongBitSet bb = b.Clone();
				bb.Flip(fromIndex, toIndex);
				fromIndex = Random().Next(sz / 2);
				toIndex = fromIndex + Random().Next(sz - fromIndex);
				aa = (BitSet)a.Clone();
				aa.Clear(fromIndex, toIndex);
				bb = b.Clone();
				bb.Clear(fromIndex, toIndex);
				DoNextSetBit(aa, bb);
				// a problem here is from clear() or nextSetBit
				DoPrevSetBit(aa, bb);
				fromIndex = Random().Next(sz / 2);
				toIndex = fromIndex + Random().Next(sz - fromIndex);
				aa = (BitSet)a.Clone();
				aa.Set(fromIndex, toIndex);
				bb = b.Clone();
				bb.Set(fromIndex, toIndex);
				DoNextSetBit(aa, bb);
				// a problem here is from set() or nextSetBit
				DoPrevSetBit(aa, bb);
				if (b0 != null && b0.Length() <= b.Length())
				{
					NUnit.Framework.Assert.AreEqual(a.Cardinality(), b.Cardinality());
					BitSet a_and = (BitSet)a.Clone();
					a_and.And(a0);
					BitSet a_or = (BitSet)a.Clone();
					a_or.Or(a0);
					BitSet a_xor = (BitSet)a.Clone();
					a_xor.Xor(a0);
					BitSet a_andn = (BitSet)a.Clone();
					a_andn.AndNot(a0);
					LongBitSet b_and = b.Clone();
					NUnit.Framework.Assert.AreEqual(b, b_and);
					b_and.And(b0);
					LongBitSet b_or = b.Clone();
					b_or.Or(b0);
					LongBitSet b_xor = b.Clone();
					b_xor.Xor(b0);
					LongBitSet b_andn = b.Clone();
					b_andn.AndNot(b0);
					NUnit.Framework.Assert.AreEqual(a0.Cardinality(), b0.Cardinality());
					NUnit.Framework.Assert.AreEqual(a_or.Cardinality(), b_or.Cardinality());
					NUnit.Framework.Assert.AreEqual(a_and.Cardinality(), b_and.Cardinality());
					NUnit.Framework.Assert.AreEqual(a_or.Cardinality(), b_or.Cardinality());
					NUnit.Framework.Assert.AreEqual(a_xor.Cardinality(), b_xor.Cardinality());
					NUnit.Framework.Assert.AreEqual(a_andn.Cardinality(), b_andn.Cardinality());
				}
				a0 = a;
				b0 = b;
			}
		}

		// large enough to flush obvious bugs, small enough to run in <.5 sec as part of a
		// larger testsuite.
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestSmall()
		{
			DoRandomSets(AtLeast(1200), AtLeast(1000), 1);
			DoRandomSets(AtLeast(1200), AtLeast(1000), 2);
		}

		// uncomment to run a bigger test (~2 minutes).
		public virtual void TestEquals()
		{
			// This test can't handle numBits==0:
			int numBits = Random().Next(2000) + 1;
			LongBitSet b1 = new LongBitSet(numBits);
			LongBitSet b2 = new LongBitSet(numBits);
			NUnit.Framework.Assert.IsTrue(b1.Equals(b2));
			NUnit.Framework.Assert.IsTrue(b2.Equals(b1));
			for (int iter = 0; iter < 10 * RANDOM_MULTIPLIER; iter++)
			{
				int idx = Random().Next(numBits);
				if (!b1.Get(idx))
				{
					b1.Set(idx);
					NUnit.Framework.Assert.IsFalse(b1.Equals(b2));
					NUnit.Framework.Assert.IsFalse(b2.Equals(b1));
					b2.Set(idx);
					NUnit.Framework.Assert.IsTrue(b1.Equals(b2));
					NUnit.Framework.Assert.IsTrue(b2.Equals(b1));
				}
			}
			// try different type of object
			NUnit.Framework.Assert.IsFalse(b1.Equals(new object()));
		}

		public virtual void TestHashCodeEquals()
		{
			// This test can't handle numBits==0:
			int numBits = Random().Next(2000) + 1;
			LongBitSet b1 = new LongBitSet(numBits);
			LongBitSet b2 = new LongBitSet(numBits);
			NUnit.Framework.Assert.IsTrue(b1.Equals(b2));
			NUnit.Framework.Assert.IsTrue(b2.Equals(b1));
			for (int iter = 0; iter < 10 * RANDOM_MULTIPLIER; iter++)
			{
				int idx = Random().Next(numBits);
				if (!b1.Get(idx))
				{
					b1.Set(idx);
					NUnit.Framework.Assert.IsFalse(b1.Equals(b2));
					NUnit.Framework.Assert.IsFalse(b1.GetHashCode() == b2.GetHashCode());
					b2.Set(idx);
					NUnit.Framework.Assert.AreEqual(b1, b2);
					NUnit.Framework.Assert.AreEqual(b1.GetHashCode(), b2.GetHashCode());
				}
			}
		}

		public virtual void TestSmallBitSets()
		{
			// Make sure size 0-10 bit sets are OK:
			for (int numBits = 0; numBits < 10; numBits++)
			{
				LongBitSet b1 = new LongBitSet(numBits);
				LongBitSet b2 = new LongBitSet(numBits);
				NUnit.Framework.Assert.IsTrue(b1.Equals(b2));
				NUnit.Framework.Assert.AreEqual(b1.GetHashCode(), b2.GetHashCode());
				NUnit.Framework.Assert.AreEqual(0, b1.Cardinality());
				if (numBits > 0)
				{
					b1.Set(0, numBits);
					NUnit.Framework.Assert.AreEqual(numBits, b1.Cardinality());
					b1.Flip(0, numBits);
					NUnit.Framework.Assert.AreEqual(0, b1.Cardinality());
				}
			}
		}

		private LongBitSet MakeLongFixedBitSet(int[] a, int numBits)
		{
			LongBitSet bs;
			if (Random().NextBoolean())
			{
				int bits2words = LongBitSet.Bits2words(numBits);
				long[] words = new long[bits2words + Random().Next(100)];
				for (int i = bits2words; i < words.Length; i++)
				{
					words[i] = Random().NextLong();
				}
				bs = new LongBitSet(words, numBits);
			}
			else
			{
				bs = new LongBitSet(numBits);
			}
			foreach (int e in a)
			{
				bs.Set(e);
			}
			return bs;
		}

		private BitSet MakeBitSet(int[] a)
		{
			BitSet bs = new BitSet();
			foreach (int e in a)
			{
				bs.Set(e);
			}
			return bs;
		}

		private void CheckPrevSetBitArray(int[] a, int numBits)
		{
			LongBitSet obs = MakeLongFixedBitSet(a, numBits);
			BitSet bs = MakeBitSet(a);
			DoPrevSetBit(bs, obs);
		}

		public virtual void TestPrevSetBit()
		{
			CheckPrevSetBitArray(new int[] {  }, 0);
			CheckPrevSetBitArray(new int[] { 0 }, 1);
			CheckPrevSetBitArray(new int[] { 0, 2 }, 3);
		}

		private void CheckNextSetBitArray(int[] a, int numBits)
		{
			LongBitSet obs = MakeLongFixedBitSet(a, numBits);
			BitSet bs = MakeBitSet(a);
			DoNextSetBit(bs, obs);
		}

		public virtual void TestNextBitSet()
		{
			int[] setBits = new int[0 + Random().Next(1000)];
			for (int i = 0; i < setBits.Length; i++)
			{
				setBits[i] = Random().Next(setBits.Length);
			}
			CheckNextSetBitArray(setBits, setBits.Length + Random().Next(10));
			CheckNextSetBitArray(new int[0], setBits.Length + Random().Next(10));
		}

		public virtual void TestEnsureCapacity()
		{
			LongBitSet bits = new LongBitSet(5);
			bits.Set(1);
			bits.Set(4);
			LongBitSet newBits = LongBitSet.EnsureCapacity(bits, 8);
			// grow within the word
			NUnit.Framework.Assert.IsTrue(newBits.Get(1));
			NUnit.Framework.Assert.IsTrue(newBits.Get(4));
			newBits.Clear(1);
			// we align to 64-bits, so even though it shouldn't have, it re-allocated a long[1]
			NUnit.Framework.Assert.IsTrue(bits.Get(1));
			NUnit.Framework.Assert.IsFalse(newBits.Get(1));
			newBits.Set(1);
			newBits = LongBitSet.EnsureCapacity(newBits, newBits.Length() - 2);
			// reuse
			NUnit.Framework.Assert.IsTrue(newBits.Get(1));
			bits.Set(1);
			newBits = LongBitSet.EnsureCapacity(bits, 72);
			// grow beyond one word
			NUnit.Framework.Assert.IsTrue(newBits.Get(1));
			NUnit.Framework.Assert.IsTrue(newBits.Get(4));
			newBits.Clear(1);
			// we grew the long[], so it's not shared
			NUnit.Framework.Assert.IsTrue(bits.Get(1));
			NUnit.Framework.Assert.IsFalse(newBits.Get(1));
		}
	}
}
