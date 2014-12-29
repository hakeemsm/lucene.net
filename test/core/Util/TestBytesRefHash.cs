/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Util
{
	public class TestBytesRefHash : LuceneTestCase
	{
		internal BytesRefHash hash;

		internal ByteBlockPool pool;

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.SetUp]
		public override void SetUp()
		{
			base.SetUp();
			pool = NewPool();
			hash = NewHash(pool);
		}

		private ByteBlockPool NewPool()
		{
			return Random().NextBoolean() && pool != null ? pool : new ByteBlockPool(new RecyclingByteBlockAllocator
				(ByteBlockPool.BYTE_BLOCK_SIZE, Random().Next(25)));
		}

		private BytesRefHash NewHash(ByteBlockPool blockPool)
		{
			int initSize = 2 << 1 + Random().Next(5);
			return Random().NextBoolean() ? new BytesRefHash(blockPool) : new BytesRefHash(blockPool
				, initSize, new BytesRefHash.DirectBytesStartArray(initSize));
		}

		/// <summary>
		/// Test method for
		/// <see cref="BytesRefHash.Size()">BytesRefHash.Size()</see>
		/// .
		/// </summary>
		[NUnit.Framework.Test]
		public virtual void TestSize()
		{
			BytesRef @ref = new BytesRef();
			int num = AtLeast(2);
			for (int j = 0; j < num; j++)
			{
				int mod = 1 + Random().Next(39);
				for (int i = 0; i < 797; i++)
				{
					string str;
					do
					{
						str = TestUtil.RandomRealisticUnicodeString(Random(), 1000);
					}
					while (str.Length == 0);
					@ref.CopyChars(str);
					int count = hash.Size();
					int key = hash.Add(@ref);
					if (key < 0)
					{
						AreEqual(hash.Size(), count);
					}
					else
					{
						AreEqual(hash.Size(), count + 1);
					}
					if (i % mod == 0)
					{
						hash.Clear();
						AreEqual(0, hash.Size());
						hash.Reinit();
					}
				}
			}
		}

		/// <summary>
		/// Test method for
		/// <see cref="BytesRefHash.Get(int, BytesRef)">BytesRefHash.Get(int, BytesRef)</see>
		/// .
		/// </summary>
		[NUnit.Framework.Test]
		public virtual void TestGet()
		{
			BytesRef @ref = new BytesRef();
			BytesRef scratch = new BytesRef();
			int num = AtLeast(2);
			for (int j = 0; j < num; j++)
			{
				IDictionary<string, int> strings = new Dictionary<string, int>();
				int uniqueCount = 0;
				for (int i = 0; i < 797; i++)
				{
					string str;
					do
					{
						str = TestUtil.RandomRealisticUnicodeString(Random(), 1000);
					}
					while (str.Length == 0);
					@ref.CopyChars(str);
					int count = hash.Size();
					int key = hash.Add(@ref);
					if (key >= 0)
					{
						IsNull(strings.Put(str, Sharpen.Extensions.ValueOf(key)));
						AreEqual(uniqueCount, key);
						uniqueCount++;
						AreEqual(hash.Size(), count + 1);
					}
					else
					{
						IsTrue((-key) - 1 < count);
						AreEqual(hash.Size(), count);
					}
				}
				foreach (KeyValuePair<string, int> entry in strings.EntrySet())
				{
					@ref.CopyChars(entry.Key);
					AreEqual(@ref, hash.Get(entry.Value, scratch));
				}
				hash.Clear();
				AreEqual(0, hash.Size());
				hash.Reinit();
			}
		}

		/// <summary>
		/// Test method for
		/// <see cref="BytesRefHash.Compact()">BytesRefHash.Compact()</see>
		/// .
		/// </summary>
		[NUnit.Framework.Test]
		public virtual void TestCompact()
		{
			BytesRef @ref = new BytesRef();
			int num = AtLeast(2);
			for (int j = 0; j < num; j++)
			{
				int numEntries = 0;
				int size = 797;
				BitSet bits = new BitSet(size);
				for (int i = 0; i < size; i++)
				{
					string str;
					do
					{
						str = TestUtil.RandomRealisticUnicodeString(Random(), 1000);
					}
					while (str.Length == 0);
					@ref.CopyChars(str);
					int key = hash.Add(@ref);
					if (key < 0)
					{
						IsTrue(bits.Get((-key) - 1));
					}
					else
					{
						IsFalse(bits.Get(key));
						bits.Set(key);
						numEntries++;
					}
				}
				AreEqual(hash.Size(), bits.Cardinality());
				AreEqual(numEntries, bits.Cardinality());
				AreEqual(numEntries, hash.Size());
				int[] compact = hash.Compact();
				IsTrue(numEntries < compact.Length);
				for (int i_1 = 0; i_1 < numEntries; i_1++)
				{
					bits.Set(compact[i_1], false);
				}
				AreEqual(0, bits.Cardinality());
				hash.Clear();
				AreEqual(0, hash.Size());
				hash.Reinit();
			}
		}

		/// <summary>
		/// Test method for
		/// <see cref="BytesRefHash.Sort(System.Collections.Generic.IComparer{T})">BytesRefHash.Sort(System.Collections.Generic.IComparer&lt;T&gt;)
		/// 	</see>
		/// .
		/// </summary>
		[NUnit.Framework.Test]
		public virtual void TestSort()
		{
			BytesRef @ref = new BytesRef();
			int num = AtLeast(2);
			for (int j = 0; j < num; j++)
			{
				ICollection<string> strings = new TreeSet<string>();
				for (int i = 0; i < 797; i++)
				{
					string str;
					do
					{
						str = TestUtil.RandomRealisticUnicodeString(Random(), 1000);
					}
					while (str.Length == 0);
					@ref.CopyChars(str);
					hash.Add(@ref);
					strings.Add(str);
				}
				// We use the UTF-16 comparator here, because we need to be able to
				// compare to native String.compareTo() [UTF-16]:
				int[] sort = hash.Sort(BytesRef.GetUTF8SortedAsUTF16Comparator());
				IsTrue(strings.Count < sort.Length);
				int i_1 = 0;
				BytesRef scratch = new BytesRef();
				foreach (string @string in strings)
				{
					@ref.CopyChars(@string);
					AreEqual(@ref, hash.Get(sort[i_1++], scratch));
				}
				hash.Clear();
				AreEqual(0, hash.Size());
				hash.Reinit();
			}
		}

		/// <summary>
		/// Test method for
		/// <see cref="BytesRefHash.Add(BytesRef)">BytesRefHash.Add(BytesRef)</see>
		/// .
		/// </summary>
		[NUnit.Framework.Test]
		public virtual void TestAdd()
		{
			BytesRef @ref = new BytesRef();
			BytesRef scratch = new BytesRef();
			int num = AtLeast(2);
			for (int j = 0; j < num; j++)
			{
				ICollection<string> strings = new HashSet<string>();
				int uniqueCount = 0;
				for (int i = 0; i < 797; i++)
				{
					string str;
					do
					{
						str = TestUtil.RandomRealisticUnicodeString(Random(), 1000);
					}
					while (str.Length == 0);
					@ref.CopyChars(str);
					int count = hash.Size();
					int key = hash.Add(@ref);
					if (key >= 0)
					{
						IsTrue(strings.Add(str));
						AreEqual(uniqueCount, key);
						AreEqual(hash.Size(), count + 1);
						uniqueCount++;
					}
					else
					{
						IsFalse(strings.Add(str));
						IsTrue((-key) - 1 < count);
						AreEqual(str, hash.Get((-key) - 1, scratch).Utf8ToString()
							);
						AreEqual(count, hash.Size());
					}
				}
				AssertAllIn(strings, hash);
				hash.Clear();
				AreEqual(0, hash.Size());
				hash.Reinit();
			}
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestFind()
		{
			BytesRef @ref = new BytesRef();
			BytesRef scratch = new BytesRef();
			int num = AtLeast(2);
			for (int j = 0; j < num; j++)
			{
				ICollection<string> strings = new HashSet<string>();
				int uniqueCount = 0;
				for (int i = 0; i < 797; i++)
				{
					string str;
					do
					{
						str = TestUtil.RandomRealisticUnicodeString(Random(), 1000);
					}
					while (str.Length == 0);
					@ref.CopyChars(str);
					int count = hash.Size();
					int key = hash.Find(@ref);
					//hash.add(ref);
					if (key >= 0)
					{
						// string found in hash
						IsFalse(strings.Add(str));
						IsTrue(key < count);
						AreEqual(str, hash.Get(key, scratch).Utf8ToString());
						AreEqual(count, hash.Size());
					}
					else
					{
						key = hash.Add(@ref);
						IsTrue(strings.Add(str));
						AreEqual(uniqueCount, key);
						AreEqual(hash.Size(), count + 1);
						uniqueCount++;
					}
				}
				AssertAllIn(strings, hash);
				hash.Clear();
				AreEqual(0, hash.Size());
				hash.Reinit();
			}
		}

		public virtual void TestLargeValue()
		{
			int[] sizes = new int[] { Random().Next(5), ByteBlockPool.BYTE_BLOCK_SIZE - 33 + 
				Random().Next(31), ByteBlockPool.BYTE_BLOCK_SIZE - 1 + Random().Next(37) };
			BytesRef @ref = new BytesRef();
			for (int i = 0; i < sizes.Length; i++)
			{
				@ref.bytes = new byte[sizes[i]];
				@ref.offset = 0;
				@ref.length = sizes[i];
				try
				{
					AreEqual(i, hash.Add(@ref));
				}
				catch (BytesRefHash.MaxBytesLengthExceededException e)
				{
					if (i < sizes.Length - 1)
					{
						Fail("unexpected exception at size: " + sizes[i]);
					}
					throw;
				}
			}
		}

		/// <summary>
		/// Test method for
		/// <see cref="BytesRefHash.AddByPoolOffset(int)">BytesRefHash.AddByPoolOffset(int)</see>
		/// .
		/// </summary>
		[NUnit.Framework.Test]
		public virtual void TestAddByPoolOffset()
		{
			BytesRef @ref = new BytesRef();
			BytesRef scratch = new BytesRef();
			BytesRefHash offsetHash = NewHash(pool);
			int num = AtLeast(2);
			for (int j = 0; j < num; j++)
			{
				ICollection<string> strings = new HashSet<string>();
				int uniqueCount = 0;
				for (int i = 0; i < 797; i++)
				{
					string str;
					do
					{
						str = TestUtil.RandomRealisticUnicodeString(Random(), 1000);
					}
					while (str.Length == 0);
					@ref.CopyChars(str);
					int count = hash.Size();
					int key = hash.Add(@ref);
					if (key >= 0)
					{
						IsTrue(strings.Add(str));
						AreEqual(uniqueCount, key);
						AreEqual(hash.Size(), count + 1);
						int offsetKey = offsetHash.AddByPoolOffset(hash.ByteStart(key));
						AreEqual(uniqueCount, offsetKey);
						AreEqual(offsetHash.Size(), count + 1);
						uniqueCount++;
					}
					else
					{
						IsFalse(strings.Add(str));
						IsTrue((-key) - 1 < count);
						AreEqual(str, hash.Get((-key) - 1, scratch).Utf8ToString()
							);
						AreEqual(count, hash.Size());
						int offsetKey = offsetHash.AddByPoolOffset(hash.ByteStart((-key) - 1));
						IsTrue((-offsetKey) - 1 < count);
						AreEqual(str, hash.Get((-offsetKey) - 1, scratch).Utf8ToString
							());
						AreEqual(count, hash.Size());
					}
				}
				AssertAllIn(strings, hash);
				foreach (string @string in strings)
				{
					@ref.CopyChars(@string);
					int key = hash.Add(@ref);
					BytesRef bytesRef = offsetHash.Get((-key) - 1, scratch);
					AreEqual(@ref, bytesRef);
				}
				hash.Clear();
				AreEqual(0, hash.Size());
				offsetHash.Clear();
				AreEqual(0, offsetHash.Size());
				hash.Reinit();
				// init for the next round
				offsetHash.Reinit();
			}
		}

		private void AssertAllIn(ICollection<string> strings, BytesRefHash hash)
		{
			BytesRef @ref = new BytesRef();
			BytesRef scratch = new BytesRef();
			int count = hash.Size();
			foreach (string @string in strings)
			{
				@ref.CopyChars(@string);
				int key = hash.Add(@ref);
				// add again to check duplicates
				AreEqual(@string, hash.Get((-key) - 1, scratch).Utf8ToString
					());
				AreEqual(count, hash.Size());
				IsTrue("key: " + key + " count: " + count + " string: " + 
					@string, key < count);
			}
		}
	}
}
