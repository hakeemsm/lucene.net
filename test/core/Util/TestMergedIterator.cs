/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections;
using System.Collections.Generic;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Util
{
	public class TestMergedIterator : LuceneTestCase
	{
		private const int REPEATS = 2;

		private const int VALS_TO_MERGE = 15000;

		public virtual void TestMergeEmpty()
		{
			Iterator<int> merged = new MergedIterator<int>();
			IsFalse(merged.HasNext());
			merged = new MergedIterator<int>(new List<int>().Iterator());
			IsFalse(merged.HasNext());
			Iterator<int>[] itrs = new Iterator[Random().Next(100)];
			for (int i = 0; i < itrs.Length; i++)
			{
				itrs[i] = new List<int>().Iterator();
			}
			merged = new MergedIterator<int>(itrs);
			IsFalse(merged.HasNext());
		}

		public virtual void TestNoDupsRemoveDups()
		{
			TestCase(1, 1, true);
		}

		public virtual void TestOffItrDupsRemoveDups()
		{
			TestCase(3, 1, true);
		}

		public virtual void TestOnItrDupsRemoveDups()
		{
			TestCase(1, 3, true);
		}

		public virtual void TestOnItrRandomDupsRemoveDups()
		{
			TestCase(1, -3, true);
		}

		public virtual void TestBothDupsRemoveDups()
		{
			TestCase(3, 3, true);
		}

		public virtual void TestBothDupsWithRandomDupsRemoveDups()
		{
			TestCase(3, -3, true);
		}

		public virtual void TestNoDupsKeepDups()
		{
			TestCase(1, 1, false);
		}

		public virtual void TestOffItrDupsKeepDups()
		{
			TestCase(3, 1, false);
		}

		public virtual void TestOnItrDupsKeepDups()
		{
			TestCase(1, 3, false);
		}

		public virtual void TestOnItrRandomDupsKeepDups()
		{
			TestCase(1, -3, false);
		}

		public virtual void TestBothDupsKeepDups()
		{
			TestCase(3, 3, false);
		}

		public virtual void TestBothDupsWithRandomDupsKeepDups()
		{
			TestCase(3, -3, false);
		}

		private void TestCase(int itrsWithVal, int specifiedValsOnItr, bool removeDups)
		{
			// Build a random number of lists
			IList<int> expected = new List<int>();
			Random random = new Random(Random().NextLong());
			int numLists = itrsWithVal + random.Next(1000 - itrsWithVal);
			IList<int>[] lists = new IList[numLists];
			for (int i = 0; i < numLists; i++)
			{
				lists[i] = new List<int>();
			}
			int start = random.Next(1000000);
			int end = start + VALS_TO_MERGE / itrsWithVal / Math.Abs(specifiedValsOnItr);
			for (int i_1 = start; i_1 < end; i_1++)
			{
				int maxList = lists.Length;
				int maxValsOnItr = 0;
				int sumValsOnItr = 0;
				for (int itrWithVal = 0; itrWithVal < itrsWithVal; itrWithVal++)
				{
					int list = random.Next(maxList);
					int valsOnItr = specifiedValsOnItr < 0 ? (1 + random.Next(-specifiedValsOnItr)) : 
						specifiedValsOnItr;
					maxValsOnItr = Math.Max(maxValsOnItr, valsOnItr);
					sumValsOnItr += valsOnItr;
					for (int valOnItr = 0; valOnItr < valsOnItr; valOnItr++)
					{
						lists[list].Add(i_1);
					}
					maxList = maxList - 1;
					ArrayUtil.Swap(lists, list, maxList);
				}
				int maxCount = removeDups ? maxValsOnItr : sumValsOnItr;
				for (int count = 0; count < maxCount; count++)
				{
					expected.Add(i_1);
				}
			}
			// Now check that they get merged cleanly
			Iterator<int>[] itrs = new Iterator[numLists];
			for (int i_2 = 0; i_2 < numLists; i_2++)
			{
				itrs[i_2] = lists[i_2].Iterator();
			}
			MergedIterator<int> mergedItr = new MergedIterator<int>(removeDups, itrs);
			Iterator<int> expectedItr = expected.Iterator();
			while (expectedItr.HasNext())
			{
				IsTrue(mergedItr.HasNext());
				AreEqual(expectedItr.Next(), mergedItr.Next());
			}
			IsFalse(mergedItr.HasNext());
		}
	}
}
