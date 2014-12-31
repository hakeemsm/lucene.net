/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Util;


namespace Lucene.Net.Util
{
	public class TestIntroSorter : BaseSortTestCase
	{
		public TestIntroSorter() : base(false)
		{
		}

		public override Sorter NewSorter(BaseSortTestCase.Entry[] arr)
		{
			return new ArrayIntroSorter<BaseSortTestCase.Entry>(arr, ArrayUtil.NaturalComparator
				<BaseSortTestCase.Entry>());
		}
	}
}
