/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Search;
using Lucene.Net.Util;
using Lucene.Net.Util.Packed;
using Sharpen;

namespace Lucene.Net.Util.Packed
{
	public class TestEliasFanoDocIdSet : BaseDocIdSetTestCase<EliasFanoDocIdSet>
	{
		/// <exception cref="System.IO.IOException"></exception>
		public override EliasFanoDocIdSet CopyOf(BitSet bs, int numBits)
		{
			EliasFanoDocIdSet set = new EliasFanoDocIdSet(bs.Cardinality(), numBits - 1);
			set.EncodeFromDisi(new _DocIdSetIterator_31(bs));
			//HM:revisit 
			//assert doc < numBits;
			return set;
		}

		private sealed class _DocIdSetIterator_31 : DocIdSetIterator
		{
			public _DocIdSetIterator_31(BitSet bs)
			{
				this.bs = bs;
				this.Doc = -1;
			}

			internal int doc;

			/// <exception cref="System.IO.IOException"></exception>
			public override int NextDoc()
			{
				this.Doc = bs.NextSetBit(this.Doc + 1);
				if (this.Doc == -1)
				{
					this.Doc = DocIdSetIterator.NO_MORE_DOCS;
				}
				return this.Doc;
			}

			public override int DocID
			{
				return this.Doc;
			}

			public override long Cost()
			{
				return bs.Cardinality();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Advance(int target)
			{
				return this.SlowAdvance(target);
			}

			private readonly BitSet bs;
		}
	}
}
