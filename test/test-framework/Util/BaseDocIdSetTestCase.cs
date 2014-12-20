/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.TestFramework.Search;
using Lucene.Net.TestFramework.Util;
using Sharpen;

namespace Lucene.Net.TestFramework.Util
{
	/// <summary>
	/// Base test class for
	/// <see cref="Lucene.NetSearch.DocIdSet">Lucene.NetSearch.DocIdSet</see>
	/// s.
	/// </summary>
	public abstract class BaseDocIdSetTestCase<T> : LuceneTestCase where T:DocIdSet
	{
		/// <summary>
		/// Create a copy of the given
		/// <see cref="Sharpen.BitSet">Sharpen.BitSet</see>
		/// which has <code>length</code> bits.
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public abstract T CopyOf(BitSet bs, int length);

		/// <summary>Create a random set which has <code>numBitsSet</code> of its <code>numBits</code> bits set.
		/// 	</summary>
		/// <remarks>Create a random set which has <code>numBitsSet</code> of its <code>numBits</code> bits set.
		/// 	</remarks>
		protected internal static BitSet RandomSet(int numBits, int numBitsSet)
		{
			//HM:revisit 
			//assert numBitsSet <= numBits;
			BitSet set = new BitSet(numBits);
			if (numBitsSet == numBits)
			{
				set.Set(0, numBits);
			}
			else
			{
				for (int i = 0; i < numBitsSet; ++i)
				{
					while (true)
					{
						int o = Random().Next(numBits);
						if (!set.Get(o))
						{
							set.Set(o);
							break;
						}
					}
				}
			}
			return set;
		}

		/// <summary>
		/// Same as
		/// <see cref="BaseDocIdSetTestCase{T}.RandomSet(int, int)">BaseDocIdSetTestCase&lt;T&gt;.RandomSet(int, int)
		/// 	</see>
		/// but given a load factor.
		/// </summary>
		protected internal static BitSet RandomSet(int numBits, float percentSet)
		{
			return RandomSet(numBits, (int)(percentSet * numBits));
		}

		/// <summary>Test length=0.</summary>
		/// <remarks>Test length=0.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestNoBit()
		{
			BitSet bs = new BitSet(1);
			T copy = CopyOf(bs, 0);
			AssertEquals(0, bs, copy);
		}

		/// <summary>Test length=1.</summary>
		/// <remarks>Test length=1.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void Test1Bit()
		{
			BitSet bs = new BitSet(1);
			if (Random().NextBoolean())
			{
				bs.Set(0);
			}
			T copy = CopyOf(bs, 1);
			AssertEquals(1, bs, copy);
		}

		/// <summary>Test length=2.</summary>
		/// <remarks>Test length=2.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void Test2Bits()
		{
			BitSet bs = new BitSet(2);
			if (Random().NextBoolean())
			{
				bs.Set(0);
			}
			if (Random().NextBoolean())
			{
				bs.Set(1);
			}
			T copy = CopyOf(bs, 2);
			AssertEquals(2, bs, copy);
		}

		/// <summary>
		/// Compare the content of the set against a
		/// <see cref="Sharpen.BitSet">Sharpen.BitSet</see>
		/// .
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestAgainstBitSet()
		{
			int numBits = TestUtil.NextInt(Random(), 100, 1 << 20);
			// test various random sets with various load factors
			foreach (float percentSet in new float[] { 0f, 0.0001f, Random().NextFloat() / 2, 
				0.9f, 1f })
			{
				BitSet set = RandomSet(numBits, percentSet);
				T copy = CopyOf(set, numBits);
				AssertEquals(numBits, set, copy);
			}
			// test one doc
			BitSet set_1 = new BitSet(numBits);
			set_1.Set(0);
			// 0 first
			T copy_1 = CopyOf(set_1, numBits);
			AssertEquals(numBits, set_1, copy_1);
			set_1.Clear(0);
			set_1.Set(Random().Next(numBits));
			copy_1 = CopyOf(set_1, numBits);
			// then random index
			AssertEquals(numBits, set_1, copy_1);
			// test regular increments
			for (int inc = 2; inc < 1000; inc += TestUtil.NextInt(Random(), 1, 100))
			{
				set_1 = new BitSet(numBits);
				for (int d = Random().Next(10); d < numBits; d += inc)
				{
					set_1.Set(d);
				}
				copy_1 = CopyOf(set_1, numBits);
				AssertEquals(numBits, set_1, copy_1);
			}
		}

		/// <summary>
		/// //HM:revisit
		/// //assert that the content of the
		/// <see cref="Lucene.NetSearch.DocIdSet">Lucene.NetSearch.DocIdSet</see>
		/// is the same as the content of the
		/// <see cref="Sharpen.BitSet">Sharpen.BitSet</see>
		/// .
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void AssertEquals(int numBits, BitSet ds1, T ds2)
		{
			// nextDoc
			DocIdSetIterator it2 = ds2.Iterator();
			if (it2 == null)
			{
				NUnit.Framework.Assert.AreEqual(-1, ds1.NextSetBit(0));
			}
			else
			{
				NUnit.Framework.Assert.AreEqual(-1, it2.DocID());
				for (int doc = ds1.NextSetBit(0); doc != -1; doc = ds1.NextSetBit(doc + 1))
				{
					NUnit.Framework.Assert.AreEqual(doc, it2.NextDoc());
					NUnit.Framework.Assert.AreEqual(doc, it2.DocID());
				}
				NUnit.Framework.Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, it2.NextDoc());
				NUnit.Framework.Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, it2.DocID());
			}
			// nextDoc / advance
			it2 = ds2.Iterator();
			if (it2 == null)
			{
				NUnit.Framework.Assert.AreEqual(-1, ds1.NextSetBit(0));
			}
			else
			{
				for (int doc = -1; doc != DocIdSetIterator.NO_MORE_DOCS; )
				{
					if (Random().NextBoolean())
					{
						doc = ds1.NextSetBit(doc + 1);
						if (doc == -1)
						{
							doc = DocIdSetIterator.NO_MORE_DOCS;
						}
						NUnit.Framework.Assert.AreEqual(doc, it2.NextDoc());
						NUnit.Framework.Assert.AreEqual(doc, it2.DocID());
					}
					else
					{
						int target = doc + 1 + Random().Next(Random().NextBoolean() ? 64 : Math.Max(numBits
							 / 8, 1));
						doc = ds1.NextSetBit(target);
						if (doc == -1)
						{
							doc = DocIdSetIterator.NO_MORE_DOCS;
						}
						NUnit.Framework.Assert.AreEqual(doc, it2.Advance(target));
						NUnit.Framework.Assert.AreEqual(doc, it2.DocID());
					}
				}
			}
			// bits()
			Bits bits = ds2.Bits();
			if (bits != null)
			{
				// test consistency between bits and iterator
				it2 = ds2.Iterator();
				for (int previousDoc = -1; ; previousDoc = doc, doc = it2.NextDoc())
				{
					int max = doc == DocIdSetIterator.NO_MORE_DOCS ? bits.Length() : doc;
					for (int i = previousDoc + 1; i < max; ++i)
					{
						NUnit.Framework.Assert.AreEqual(false, bits.Get(i));
					}
					if (doc == DocIdSetIterator.NO_MORE_DOCS)
					{
						break;
					}
					NUnit.Framework.Assert.AreEqual(true, bits.Get(doc));
				}
			}
		}
	}
}
