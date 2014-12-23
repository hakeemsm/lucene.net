/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Util
{
	public class TestDocIdBitSet : BaseDocIdSetTestCase<DocIdBitSet>
	{
		/// <exception cref="System.IO.IOException"></exception>
		public override DocIdBitSet CopyOf(BitSet bs, int length)
		{
			return new DocIdBitSet((BitSet)bs.Clone());
		}
	}
}
