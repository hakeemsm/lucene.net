/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Util
{
	public class TestSentinelIntSet : LuceneTestCase
	{
		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void Test()
		{
			SentinelIntSet set = new SentinelIntSet(10, -1);
			NUnit.Framework.Assert.IsFalse(set.Exists(50));
			set.Put(50);
			NUnit.Framework.Assert.IsTrue(set.Exists(50));
			NUnit.Framework.Assert.AreEqual(1, set.Size());
			NUnit.Framework.Assert.AreEqual(-11, set.Find(10));
			NUnit.Framework.Assert.AreEqual(1, set.Size());
			set.Clear();
			NUnit.Framework.Assert.AreEqual(0, set.Size());
			NUnit.Framework.Assert.AreEqual(50, set.Hash(50));
			//force a rehash
			for (int i = 0; i < 20; i++)
			{
				set.Put(i);
			}
			NUnit.Framework.Assert.AreEqual(20, set.Size());
			NUnit.Framework.Assert.AreEqual(24, set.rehashCount);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestRandom()
		{
			for (int i = 0; i < 10000; i++)
			{
				int initSz = Random().Next(20);
				int num = Random().Next(30);
				int maxVal = (Random().NextBoolean() ? Random().Next(50) : Random().Next(int.MaxValue
					)) + 1;
				HashSet<int> a = new HashSet<int>(initSz);
				SentinelIntSet b = new SentinelIntSet(initSz, -1);
				for (int j = 0; j < num; j++)
				{
					int val = Random().Next(maxVal);
					bool exists = !a.AddItem(val);
					bool existsB = b.Exists(val);
					NUnit.Framework.Assert.AreEqual(exists, existsB);
					int slot = b.Find(val);
					NUnit.Framework.Assert.AreEqual(exists, slot >= 0);
					b.Put(val);
					NUnit.Framework.Assert.AreEqual(a.Count, b.Size());
				}
			}
		}
	}
}
