/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Util
{
	public class TestPForDeltaDocIdSet : BaseDocIdSetTestCase<PForDeltaDocIdSet>
	{
		/// <exception cref="System.IO.IOException"></exception>
		public override PForDeltaDocIdSet CopyOf(BitSet bs, int length)
		{
			PForDeltaDocIdSet.Builder builder = new PForDeltaDocIdSet.Builder().SetIndexInterval
				(TestUtil.NextInt(Random(), 1, 20));
			for (int doc = bs.NextSetBit(0); doc != -1; doc = bs.NextSetBit(doc + 1))
			{
				builder.Add(doc);
			}
			return builder.Build();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void AssertEquals(int numBits, BitSet ds1, PForDeltaDocIdSet ds2)
		{
			base.AssertEquals(numBits, ds1, ds2);
			AreEqual(ds1.Cardinality(), ds2.Cardinality());
		}
	}
}
