/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Util
{
	public class TestInPlaceMergeSorter : BaseSortTestCase
	{
		public TestInPlaceMergeSorter() : base(true)
		{
		}

		public override Sorter NewSorter(BaseSortTestCase.Entry[] arr)
		{
			return new ArrayInPlaceMergeSorter<BaseSortTestCase.Entry>(arr, ArrayUtil.NaturalComparator
				<BaseSortTestCase.Entry>());
		}
	}
}
