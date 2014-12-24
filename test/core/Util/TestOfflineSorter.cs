/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Util
{
	/// <summary>Tests for on-disk merge sorting.</summary>
	/// <remarks>Tests for on-disk merge sorting.</remarks>
	public class TestOfflineSorter : LuceneTestCase
	{
		private FilePath tempDir;

		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			tempDir = CreateTempDir("mergesort");
			TestUtil.Rm(tempDir);
			tempDir.Mkdirs();
		}

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			if (tempDir != null)
			{
				TestUtil.Rm(tempDir);
			}
			base.TearDown();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestEmpty()
		{
			CheckSort(new OfflineSorter(), new byte[][] {  });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSingleLine()
		{
			CheckSort(new OfflineSorter(), new byte[][] { Sharpen.Runtime.GetBytesForString("Single line only."
				, StandardCharsets.UTF_8) });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestIntermediateMerges()
		{
			// Sort 20 mb worth of data with 1mb buffer, binary merging.
			OfflineSorter.SortInfo info = CheckSort(new OfflineSorter(OfflineSorter.DEFAULT_COMPARATOR
				, OfflineSorter.BufferSize.Megabytes(1), OfflineSorter.DefaultTempDir(), 2), GenerateRandom
				((int)OfflineSorter.MB * 20));
			IsTrue(info.mergeRounds > 10);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSmallRandom()
		{
			// Sort 20 mb worth of data with 1mb buffer.
			OfflineSorter.SortInfo sortInfo = CheckSort(new OfflineSorter(OfflineSorter.DEFAULT_COMPARATOR
				, OfflineSorter.BufferSize.Megabytes(1), OfflineSorter.DefaultTempDir(), OfflineSorter
				.MAX_TEMPFILES), GenerateRandom((int)OfflineSorter.MB * 20));
			AreEqual(1, sortInfo.mergeRounds);
		}

		/// <exception cref="System.Exception"></exception>
		[LuceneTestCase.Nightly]
		public virtual void TestLargerRandom()
		{
			// Sort 100MB worth of data with 15mb buffer.
			CheckSort(new OfflineSorter(OfflineSorter.DEFAULT_COMPARATOR, OfflineSorter.BufferSize
				.Megabytes(16), OfflineSorter.DefaultTempDir(), OfflineSorter.MAX_TEMPFILES), GenerateRandom
				((int)OfflineSorter.MB * 100));
		}

		private byte[][] GenerateRandom(int howMuchData)
		{
			AList<byte[]> data = new AList<byte[]>();
			while (howMuchData > 0)
			{
				byte[] current = new byte[Random().Next(256)];
				Random().NextBytes(current);
				data.AddItem(current);
				howMuchData -= current.Length;
			}
			byte[][] bytes = Sharpen.Collections.ToArray(data, new byte[data.Count][]);
			return bytes;
		}

		private sealed class _IComparer_101 : IComparer<byte[]>
		{
			public _IComparer_101()
			{
			}

			public int Compare(byte[] left, byte[] right)
			{
				int max = Math.Min(left.Length, right.Length);
				for (int i = 0; i < max; i++, j++)
				{
					int diff = (left[i] & unchecked((int)(0xff))) - (right[j] & unchecked((int)(0xff)
						));
					if (diff != 0)
					{
						return diff;
					}
				}
				return left.Length - right.Length;
			}
		}

		internal static readonly IComparer<byte[]> unsignedByteOrderComparator = new _IComparer_101
			();

		/// <summary>
		/// Check sorting data on an instance of
		/// <see cref="OfflineSorter">OfflineSorter</see>
		/// .
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		private OfflineSorter.SortInfo CheckSort(OfflineSorter sort, byte[][] data)
		{
			FilePath unsorted = WriteAll("unsorted", data);
			Arrays.Sort(data, unsignedByteOrderComparator);
			FilePath golden = WriteAll("golden", data);
			FilePath sorted = new FilePath(tempDir, "sorted");
			OfflineSorter.SortInfo sortInfo = sort.Sort(unsorted, sorted);
			//System.out.println("Input size [MB]: " + unsorted.length() / (1024 * 1024));
			//System.out.println(sortInfo);
			AssertFilesIdentical(golden, sorted);
			return sortInfo;
		}

		/// <summary>Make sure two files are byte-byte identical.</summary>
		/// <remarks>Make sure two files are byte-byte identical.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		private void AssertFilesIdentical(FilePath golden, FilePath sorted)
		{
			AreEqual(golden.Length(), sorted.Length());
			byte[] buf1 = new byte[64 * 1024];
			byte[] buf2 = new byte[64 * 1024];
			int len;
			DataInputStream is1 = new DataInputStream(new FileInputStream(golden));
			DataInputStream is2 = new DataInputStream(new FileInputStream(sorted));
			while ((len = is1.Read(buf1)) > 0)
			{
				is2.ReadFully(buf2, 0, len);
				for (int i = 0; i < len; i++)
				{
					AreEqual(buf1[i], buf2[i]);
				}
			}
			IOUtils.Close(is1, is2);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private FilePath WriteAll(string name, byte[][] data)
		{
			FilePath file = new FilePath(tempDir, name);
			OfflineSorter.ByteSequencesWriter w = new OfflineSorter.ByteSequencesWriter(file);
			foreach (byte[] datum in data)
			{
				w.Write(datum);
			}
			w.Close();
			return file;
		}

		public virtual void TestRamBuffer()
		{
			int numIters = AtLeast(10000);
			for (int i = 0; i < numIters; i++)
			{
				OfflineSorter.BufferSize.Megabytes(1 + Random().Next(2047));
			}
			OfflineSorter.BufferSize.Megabytes(2047);
			OfflineSorter.BufferSize.Megabytes(1);
			try
			{
				OfflineSorter.BufferSize.Megabytes(2048);
				Fail("max mb is 2047");
			}
			catch (ArgumentException)
			{
			}
			try
			{
				OfflineSorter.BufferSize.Megabytes(0);
				Fail("min mb is 0.5");
			}
			catch (ArgumentException)
			{
			}
			try
			{
				OfflineSorter.BufferSize.Megabytes(-1);
				Fail("min mb is 0.5");
			}
			catch (ArgumentException)
			{
			}
		}
	}
}
