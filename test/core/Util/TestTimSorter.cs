/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Util
{
	public class TestTimSorter : BaseSortTestCase
	{
		public TestTimSorter() : base(true)
		{
		}

		public override Sorter NewSorter(BaseSortTestCase.Entry[] arr)
		{
			return new ArrayTimSorter<BaseSortTestCase.Entry>(arr, ArrayUtil.NaturalComparator
				<BaseSortTestCase.Entry>(), TestUtil.NextInt(Random(), 0, arr.Length));
		}
	}
}
