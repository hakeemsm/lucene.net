/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Com.Carrotsearch.Randomizedtesting.Generators;
using Lucene.Net.TestFramework.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.TestFramework.Index;
using Lucene.Net.TestFramework.Store;
using Lucene.Net.TestFramework.Util;
using Lucene.Net.TestFramework.Util.Packed;
using Sharpen;

namespace Lucene.Net.TestFramework.Index
{
	/// <summary>
	/// Extends
	/// <see cref="BaseDocValuesFormatTestCase">BaseDocValuesFormatTestCase</see>
	/// to add compression checks.
	/// </summary>
	public abstract class BaseCompressingDocValuesFormatTestCase : BaseDocValuesFormatTestCase
	{
		/// <exception cref="System.IO.IOException"></exception>
		internal static long DirSize(Directory d)
		{
			long size = 0;
			foreach (string file in d.ListAll())
			{
				size += d.FileLength(file);
			}
			return size;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestUniqueValuesCompression()
		{
			Directory dir = new RAMDirectory();
			IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			IndexWriter iwriter = new IndexWriter(dir, iwc);
			int uniqueValueCount = TestUtil.NextInt(Random(), 1, 256);
			IList<long> values = new List<long>();
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			NumericDocValuesField dvf = new NumericDocValuesField("dv", 0);
			doc.Add(dvf);
			for (int i = 0; i < 300; ++i)
			{
				long value;
				if (values.Count < uniqueValueCount)
				{
					value = Random().NextLong();
					values.Add(value);
				}
				else
				{
					value = RandomPicks.RandomFrom(Random(), values);
				}
				dvf.SetLongValue(value);
				iwriter.AddDocument(doc);
			}
			iwriter.ForceMerge(1);
			long size1 = DirSize(dir);
			for (int i_1 = 0; i_1 < 20; ++i_1)
			{
				dvf.SetLongValue(RandomPicks.RandomFrom(Random(), values));
				iwriter.AddDocument(doc);
			}
			iwriter.ForceMerge(1);
			long size2 = DirSize(dir);
			// make sure the new longs did not cost 8 bytes each
			NUnit.Framework.Assert.IsTrue(size2 < size1 + 8 * 20);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestDateCompression()
		{
			Directory dir = new RAMDirectory();
			IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			IndexWriter iwriter = new IndexWriter(dir, iwc);
			long @base = 13;
			// prime
			long day = 1000L * 60 * 60 * 24;
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			NumericDocValuesField dvf = new NumericDocValuesField("dv", 0);
			doc.Add(dvf);
			for (int i = 0; i < 300; ++i)
			{
				dvf.SetLongValue(@base + Random().Next(1000) * day);
				iwriter.AddDocument(doc);
			}
			iwriter.ForceMerge(1);
			long size1 = DirSize(dir);
			for (int i_1 = 0; i_1 < 50; ++i_1)
			{
				dvf.SetLongValue(@base + Random().Next(1000) * day);
				iwriter.AddDocument(doc);
			}
			iwriter.ForceMerge(1);
			long size2 = DirSize(dir);
			// make sure the new longs costed less than if they had only been packed
			NUnit.Framework.Assert.IsTrue(size2 < size1 + (PackedInts.BitsRequired(day) * 50)
				 / 8);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestSingleBigValueCompression()
		{
			Directory dir = new RAMDirectory();
			IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			IndexWriter iwriter = new IndexWriter(dir, iwc);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			NumericDocValuesField dvf = new NumericDocValuesField("dv", 0);
			doc.Add(dvf);
			for (int i = 0; i < 20000; ++i)
			{
				dvf.SetLongValue(i & 1023);
				iwriter.AddDocument(doc);
			}
			iwriter.ForceMerge(1);
			long size1 = DirSize(dir);
			dvf.SetLongValue(long.MaxValue);
			iwriter.AddDocument(doc);
			iwriter.ForceMerge(1);
			long size2 = DirSize(dir);
			// make sure the new value did not grow the bpv for every other value
			NUnit.Framework.Assert.IsTrue(size2 < size1 + (20000 * (63 - 10)) / 8);
		}
	}
}
