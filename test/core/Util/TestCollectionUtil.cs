using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Util
{
    [TestFixture]
    public class TestCollectionUtil : LuceneTestCase
    {
        private List<int> CreateRandomList(int maxSize)
        {
            Random rnd = new Random();
            int[] a = new int[rnd.Next(maxSize) + 1];
            for (int i = 0; i < a.Length; i++)
            {
                a[i] = rnd.Next(a.Length);
            }
            return a.ToList();
        }
		public virtual void TestIntroSort()
		{
			for (int i = 0; i < c; i++)
			{
				IList<int> list1 = CreateRandomList(2000);
				IList<int> list2 = new AList<int>(list1);
				CollectionUtil.IntroSort(list1);
				list2.Sort();
				AreEqual(list2, list1);
				list1 = CreateRandomList(2000);
				list2 = new AList<int>(list1);
				CollectionUtil.IntroSort(list1, Sharpen.Collections.ReverseOrder());
				list2.Sort(Sharpen.Collections.ReverseOrder());
				AreEqual(list2, list1);
				// reverse back, so we can test that completely backwards sorted array (worst case) is working:
				CollectionUtil.IntroSort(list1);
				list2.Sort();
				AreEqual(list2, list1);
			}
		}
		public virtual void TestTimSort()
		{
			for (int i = 0; i < c; i++)
			{
				IList<int> list1 = CreateRandomList(2000);
				IList<int> list2 = new AList<int>(list1);
				CollectionUtil.TimSort(list1);
				list2.Sort();
				AreEqual(list2, list1);
				list1 = CreateRandomList(2000);
				list2 = new AList<int>(list1);
				CollectionUtil.TimSort(list1, Sharpen.Collections.ReverseOrder());
				list2.Sort(Sharpen.Collections.ReverseOrder());
				AreEqual(list2, list1);
				// reverse back, so we can test that completely backwards sorted array (worst case) is working:
				CollectionUtil.TimSort(list1);
				list2.Sort();
				AreEqual(list2, list1);
			}
		}
        [Test]
        public void TestQuickSort()
        {
            for (int i = 0, c = AtLeast(500); i < c; i++)
            {
                var list1 = CreateRandomList(2000);
                var list2 = new List<int>(list1);
                CollectionUtil.QuickSort(list1);
                Collections.Sort(list2);
                assertEquals(list2, list1);

                list1 = CreateRandomList(2000);
                list2 = new List<int>(list1);
                CollectionUtil.QuickSort(list1, Collections.ReverseOrder());
                Collections.Sort(list2, Collections.ReverseOrder());
                assertEquals(list2, list1);
                // reverse back, so we can test that completely backwards sorted array (worst case) is working:
                CollectionUtil.QuickSort(list1);
                Collections.Sort(list2);
                assertEquals(list2, list1);
            }
        }

        [Test]
        public void TestMergeSort()
        {
            for (int i = 0, c = AtLeast(500); i < c; i++)
            {
                var list1 = CreateRandomList(2000); 
                var list2 = new List<int>(list1);
                CollectionUtil.MergeSort(list1);
                Collections.Sort(list2);
                assertEquals(list2, list1);

                list1 = CreateRandomList(2000);
                list2 = new List<int>(list1);
                CollectionUtil.MergeSort(list1, Collections.ReverseOrder());
                Collections.Sort(list2, Collections.ReverseOrder());
                assertEquals(list2, list1);
                // reverse back, so we can test that completely backwards sorted array (worst case) is working:
                CollectionUtil.MergeSort(list1);
                Collections.Sort(list2);
                assertEquals(list2, list1);
            }
        }

        [Test]
        public void TestTimSort()
        {
            for (int i = 0, c = AtLeast(500); i < c; i++)
            {
                var list1 = CreateRandomList(2000);
                var list2 = new List<int>(list1);
                CollectionUtil.TimSort(list1);
                Collections.Sort(list2);
                assertEquals(list2, list1);

                list1 = CreateRandomList(2000);
                list2 = new List<int>(list1);
                CollectionUtil.TimSort(list1, Collections.ReverseOrder());
                Collections.Sort(list2, Collections.ReverseOrder());
                assertEquals(list2, list1);
                // reverse back, so we can test that completely backwards sorted array (worst case) is working:
                CollectionUtil.TimSort(list1);
                Collections.Sort(list2);
                assertEquals(list2, list1);
            }
        }

        [Test]
        public void TestInsertionSort()
        {
            for (int i = 0, c = AtLeast(500); i < c; i++)
            {
                var list1 = CreateRandomList(30);
                var list2 = new List<int>(list1);
                CollectionUtil.InsertionSort(list1);
                Collections.Sort(list2);
                assertEquals(list2, list1);

                list1 = CreateRandomList(30);
                list2 = new List<int>(list1);
                CollectionUtil.InsertionSort(list1, Collections.ReverseOrder());
                Collections.Sort(list2, Collections.ReverseOrder());
                assertEquals(list2, list1);
                // reverse back, so we can test that completely backwards sorted array (worst case) is working:
                CollectionUtil.InsertionSort(list1);
                Collections.Sort(list2);
                assertEquals(list2, list1);
            }
        }

        [Test]
        public void TestBinarySort()
        {
            for (int i = 0, c = AtLeast(500); i < c; i++)
            {
                List<int> list1 = CreateRandomList(30), list2 = new List<int>(list1);
                CollectionUtil.BinarySort(list1);
                Collections.Sort(list2);
                assertEquals(list2, list1);

                list1 = CreateRandomList(30);
                list2 = new List<int>(list1);
                CollectionUtil.BinarySort(list1, Collections.ReverseOrder());
                Collections.Sort(list2, Collections.ReverseOrder());
                assertEquals(list2, list1);
                // reverse back, so we can test that completely backwards sorted array (worst case) is working:
                CollectionUtil.BinarySort(list1);
                Collections.Sort(list2);
                assertEquals(list2, list1);
            }
        }

        [Test]
        public void TestEmptyListSort()
        {
            // should produce no exceptions
			IList<int> list = Arrays.AsList(new int[0]);
			// LUCENE-2989
			CollectionUtil.IntroSort(list);
			CollectionUtil.TimSort(list);
			CollectionUtil.IntroSort(list, Sharpen.Collections.ReverseOrder());
			CollectionUtil.TimSort(list, Sharpen.Collections.ReverseOrder());
			// check that empty non-random access lists pass sorting without ex (as sorting is not needed)
			list = new List<int>();
			CollectionUtil.IntroSort(list);
			CollectionUtil.TimSort(list);
			CollectionUtil.IntroSort(list, Sharpen.Collections.ReverseOrder());
			CollectionUtil.TimSort(list, Sharpen.Collections.ReverseOrder());
        }

        [Test]
        public void TestOneElementListSort()
        {
			// check that one-element non-random access lists pass sorting without ex (as sorting is not needed)
			IList<int> list = new List<int>();
			list.AddItem(1);
			CollectionUtil.IntroSort(list);
			CollectionUtil.TimSort(list);
			CollectionUtil.IntroSort(list, Sharpen.Collections.ReverseOrder());
			CollectionUtil.TimSort(list, Sharpen.Collections.ReverseOrder());
        }
    }
}
