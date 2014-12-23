/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Util
{
	public abstract class BaseSortTestCase : LuceneTestCase
	{
		public class Entry : Comparable<BaseSortTestCase.Entry>
		{
			public readonly int value;

			public readonly int ord;

			public Entry(int value, int ord)
			{
				this.value = value;
				this.ord = ord;
			}

			public virtual int CompareTo(BaseSortTestCase.Entry other)
			{
				return value < other.value ? -1 : value == other.value ? 0 : 1;
			}
		}

		private readonly bool stable;

		public BaseSortTestCase(bool stable)
		{
			this.stable = stable;
		}

		public abstract Sorter NewSorter(BaseSortTestCase.Entry[] arr);

		public virtual void AssertSorted(BaseSortTestCase.Entry[] original, BaseSortTestCase.Entry
			[] sorted)
		{
			NUnit.Framework.Assert.AreEqual(original.Length, sorted.Length);
			BaseSortTestCase.Entry[] actuallySorted = Arrays.CopyOf(original, original.Length
				);
			Arrays.Sort(actuallySorted);
			for (int i = 0; i < original.Length; ++i)
			{
				NUnit.Framework.Assert.AreEqual(actuallySorted[i].value, sorted[i].value);
				if (stable)
				{
					NUnit.Framework.Assert.AreEqual(actuallySorted[i].ord, sorted[i].ord);
				}
			}
		}

		public virtual void Test(BaseSortTestCase.Entry[] arr)
		{
			int o = Random().Next(1000);
			BaseSortTestCase.Entry[] toSort = new BaseSortTestCase.Entry[o + arr.Length + Random
				().Next(3)];
			System.Array.Copy(arr, 0, toSort, o, arr.Length);
			Sorter sorter = NewSorter(toSort);
			sorter.Sort(o, o + arr.Length);
			AssertSorted(arr, Arrays.CopyOfRange(toSort, o, o + arr.Length));
		}

		internal enum Strategy
		{
			RANDOM,
			RANDOM_LOW_CARDINALITY,
			ASCENDING,
			DESCENDING,
			STRICTLY_DESCENDING,
			ASCENDING_SEQUENCES,
			MOSTLY_ASCENDING
		}

		internal class StrategyHelper
		{
			//public abstract void set(Entry[] arr, int i);
			public static void Set(BaseSortTestCase.Strategy strat, BaseSortTestCase.Entry[] 
				arr, int i)
			{
				switch (strat)
				{
					case BaseSortTestCase.Strategy.RANDOM:
					{
						arr[i] = new BaseSortTestCase.Entry(Random().Next(), i);
						goto case BaseSortTestCase.Strategy.RANDOM_LOW_CARDINALITY;
					}

					case BaseSortTestCase.Strategy.RANDOM_LOW_CARDINALITY:
					{
						arr[i] = new BaseSortTestCase.Entry(Random().Next(6), i);
						goto case BaseSortTestCase.Strategy.ASCENDING;
					}

					case BaseSortTestCase.Strategy.ASCENDING:
					{
						arr[i] = i == 0 ? new BaseSortTestCase.Entry(Random().Next(6), 0) : new BaseSortTestCase.Entry
							(arr[i - 1].value + Random().Next(6), i);
						goto case BaseSortTestCase.Strategy.DESCENDING;
					}

					case BaseSortTestCase.Strategy.DESCENDING:
					{
						arr[i] = i == 0 ? new BaseSortTestCase.Entry(Random().Next(6), 0) : new BaseSortTestCase.Entry
							(arr[i - 1].value - Random().Next(6), i);
						goto case BaseSortTestCase.Strategy.STRICTLY_DESCENDING;
					}

					case BaseSortTestCase.Strategy.STRICTLY_DESCENDING:
					{
						arr[i] = i == 0 ? new BaseSortTestCase.Entry(Random().Next(6), 0) : new BaseSortTestCase.Entry
							(arr[i - 1].value - TestUtil.NextInt(Random(), 1, 5), i);
						goto case BaseSortTestCase.Strategy.ASCENDING_SEQUENCES;
					}

					case BaseSortTestCase.Strategy.ASCENDING_SEQUENCES:
					{
						arr[i] = i == 0 ? new BaseSortTestCase.Entry(Random().Next(6), 0) : new BaseSortTestCase.Entry
							(Rarely() ? Random().Next(1000) : arr[i - 1].value + Random().Next(6), i);
						goto case BaseSortTestCase.Strategy.MOSTLY_ASCENDING;
					}

					case BaseSortTestCase.Strategy.MOSTLY_ASCENDING:
					{
						arr[i] = i == 0 ? new BaseSortTestCase.Entry(Random().Next(6), 0) : new BaseSortTestCase.Entry
							(arr[i - 1].value + TestUtil.NextInt(Random(), -8, 10), i);
					}
				}
			}
		}

		public virtual void Test(BaseSortTestCase.Strategy strategy, int length)
		{
			BaseSortTestCase.Entry[] arr = new BaseSortTestCase.Entry[length];
			for (int i = 0; i < arr.Length; ++i)
			{
				BaseSortTestCase.StrategyHelper.Set(strategy, arr, i);
			}
			Test(arr);
		}

		public virtual void Test(BaseSortTestCase.Strategy strategy)
		{
			Test(strategy, Random().Next(20000));
		}

		public virtual void TestEmpty()
		{
			Test(new BaseSortTestCase.Entry[0]);
		}

		public virtual void TestOne()
		{
			Test(BaseSortTestCase.Strategy.RANDOM, 1);
		}

		public virtual void TestTwo()
		{
			Test(BaseSortTestCase.Strategy.RANDOM_LOW_CARDINALITY, 2);
		}

		public virtual void TestRandom()
		{
			Test(BaseSortTestCase.Strategy.RANDOM);
		}

		public virtual void TestRandomLowCardinality()
		{
			Test(BaseSortTestCase.Strategy.RANDOM_LOW_CARDINALITY);
		}

		public virtual void TestAscending()
		{
			Test(BaseSortTestCase.Strategy.ASCENDING);
		}

		public virtual void TestAscendingSequences()
		{
			Test(BaseSortTestCase.Strategy.ASCENDING_SEQUENCES);
		}

		public virtual void TestDescending()
		{
			Test(BaseSortTestCase.Strategy.DESCENDING);
		}

		public virtual void TestStrictlyDescending()
		{
			Test(BaseSortTestCase.Strategy.STRICTLY_DESCENDING);
		}
	}
}
