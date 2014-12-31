/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Util;


namespace Lucene.Net.Util
{
	public class TestWAH8DocIdSet : BaseDocIdSetTestCase<WAH8DocIdSet>
	{
		/// <exception cref="System.IO.IOException"></exception>
		public override WAH8DocIdSet CopyOf(BitSet bs, int length)
		{
			int indexInterval = TestUtil.NextInt(Random(), 8, 256);
			WAH8DocIdSet.Builder builder = ((WAH8DocIdSet.Builder)new WAH8DocIdSet.Builder().
				SetIndexInterval(indexInterval));
			for (int i = bs.NextSetBit(0); i != -1; i = bs.NextSetBit(i + 1))
			{
				builder.Add(i);
			}
			return builder.Build();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void AssertEquals(int numBits, BitSet ds1, WAH8DocIdSet ds2)
		{
			base.AssertEquals(numBits, ds1, ds2);
			AreEqual(ds1.Cardinality(), ds2.Cardinality());
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestUnion()
		{
			int numBits = TestUtil.NextInt(Random(), 100, 1 << 20);
			int numDocIdSets = TestUtil.NextInt(Random(), 0, 4);
			IList<BitSet> fixedSets = new List<BitSet>(numDocIdSets);
			for (int i = 0; i < numDocIdSets; ++i)
			{
				fixedSets.Add(RandomSet(numBits, Random().NextFloat() / 16));
			}
			IList<WAH8DocIdSet> compressedSets = new List<WAH8DocIdSet>(numDocIdSets);
			foreach (BitSet set in fixedSets)
			{
				compressedSets.Add(CopyOf(set, numBits));
			}
			WAH8DocIdSet union = WAH8DocIdSet.Union(compressedSets);
			BitSet expected = new BitSet(numBits);
			foreach (BitSet set_1 in fixedSets)
			{
				for (int doc = set_1.NextSetBit(0); doc != -1; doc = set_1.NextSetBit(doc + 1))
				{
					expected.Set(doc);
				}
			}
			AssertEquals(numBits, expected, union);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestIntersection()
		{
			int numBits = TestUtil.NextInt(Random(), 100, 1 << 20);
			int numDocIdSets = TestUtil.NextInt(Random(), 1, 4);
			IList<BitSet> fixedSets = new List<BitSet>(numDocIdSets);
			for (int i = 0; i < numDocIdSets; ++i)
			{
				fixedSets.Add(RandomSet(numBits, Random().NextFloat()));
			}
			IList<WAH8DocIdSet> compressedSets = new List<WAH8DocIdSet>(numDocIdSets);
			foreach (BitSet set in fixedSets)
			{
				compressedSets.Add(CopyOf(set, numBits));
			}
			WAH8DocIdSet union = WAH8DocIdSet.Intersect(compressedSets);
			BitSet expected = new BitSet(numBits);
			expected.Set(0, expected.Size());
			foreach (BitSet set_1 in fixedSets)
			{
				for (int previousDoc = -1; ; previousDoc = doc, doc = set_1.NextSetBit(doc + 1))
				{
					if (doc == -1)
					{
						expected.Clear(previousDoc + 1, set_1.Size());
						break;
					}
					else
					{
						expected.Clear(previousDoc + 1, doc);
					}
				}
			}
			AssertEquals(numBits, expected, union);
		}
	}
}
