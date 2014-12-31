using System.Collections.Generic;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.TestFramework;
using Lucene.Net.TestFramework.Index;
using Lucene.Net.TestFramework.Util;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Index
{
	/// <summary>Tests MultiDocValues versus ordinary segment merging</summary>
	[TestFixture]
    public class TestMultiDocValues : LuceneTestCase
	{
		[Test]
		public virtual void TestNumerics()
		{
			Directory dir = NewDirectory();
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
			Field field = new NumericDocValuesField("numbers", 0);
			doc.Add(field);
			IndexWriterConfig iwc = NewIndexWriterConfig(Random(), TEST_VERSION_CURRENT, null);
			iwc.SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, iwc);
			int numDocs = AtLeast(500);
			for (int i = 0; i < numDocs; i++)
			{
				field.SetLongValue(Random().NextLong(0,long.MaxValue));
				iw.AddDocument(doc);
				if (Random().Next(17) == 0)
				{
					iw.Commit();
				}
			}
			DirectoryReader ir = iw.Reader;
			iw.ForceMerge(1);
			DirectoryReader ir2 = iw.Reader;
			AtomicReader merged = GetOnlySegmentReader(ir2);
			iw.Dispose();
			NumericDocValues multi = MultiDocValues.GetNumericValues(ir, "numbers");
			NumericDocValues single = merged.GetNumericDocValues("numbers");
			for (int i_1 = 0; i_1 < numDocs; i_1++)
			{
				AreEqual(single.Get(i_1), multi.Get(i_1));
			}
			ir.Dispose();
			ir2.Dispose();
			dir.Dispose();
		}

		[Test]
		public virtual void TestBinary()
		{
			Directory dir = NewDirectory();
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			BytesRef @ref = new BytesRef();
			Field field = new BinaryDocValuesField("bytes", @ref);
			doc.Add(field);
			IndexWriterConfig iwc = NewIndexWriterConfig(Random(), TEST_VERSION_CURRENT, null
				);
			iwc.SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, iwc);
			int numDocs = AtLeast(500);
			for (int i = 0; i < numDocs; i++)
			{
				@ref.CopyChars(TestUtil.RandomUnicodeString(Random()));
				iw.AddDocument(doc);
				if (Random().Next(17) == 0)
				{
					iw.Commit();
				}
			}
			DirectoryReader ir = iw.Reader;
			iw.ForceMerge(1);
			DirectoryReader ir2 = iw.Reader;
			AtomicReader merged = GetOnlySegmentReader(ir2);
			iw.Dispose();
			BinaryDocValues multi = MultiDocValues.GetBinaryValues(ir, "bytes");
			BinaryDocValues single = merged.GetBinaryDocValues("bytes");
			BytesRef actual = new BytesRef();
			BytesRef expected = new BytesRef();
			for (int i_1 = 0; i_1 < numDocs; i_1++)
			{
				single.Get(i_1, expected);
				multi.Get(i_1, actual);
				AreEqual(expected, actual);
			}
			ir.Dispose();
			ir2.Dispose();
			dir.Dispose();
		}

		[Test]
		public virtual void TestSorted()
		{
			Directory dir = NewDirectory();
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			BytesRef @ref = new BytesRef();
			Field field = new SortedDocValuesField("bytes", @ref);
			doc.Add(field);
			IndexWriterConfig iwc = NewIndexWriterConfig(Random(), TEST_VERSION_CURRENT, null
				);
			iwc.SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, iwc);
			int numDocs = AtLeast(500);
			for (int i = 0; i < numDocs; i++)
			{
				@ref.CopyChars(TestUtil.RandomUnicodeString(Random()));
				if (DefaultCodecSupportsDocsWithField() && Random().Next(7) == 0)
				{
					iw.AddDocument(new Lucene.Net.Documents.Document());
				}
				iw.AddDocument(doc);
				if (Random().Next(17) == 0)
				{
					iw.Commit();
				}
			}
			DirectoryReader ir = iw.Reader;
			iw.ForceMerge(1);
			DirectoryReader ir2 = iw.Reader;
			AtomicReader merged = GetOnlySegmentReader(ir2);
			iw.Dispose();
			SortedDocValues multi = MultiDocValues.GetSortedValues(ir, "bytes");
			SortedDocValues single = merged.GetSortedDocValues("bytes");
			AreEqual(single.ValueCount, multi.ValueCount);
			BytesRef actual = new BytesRef();
			BytesRef expected = new BytesRef();
			for (int i_1 = 0; i_1 < numDocs; i_1++)
			{
				// check ord
				AreEqual(single.GetOrd(i_1), multi.GetOrd(i_1));
				// check value
				single.Get(i_1, expected);
				multi.Get(i_1, actual);
				AreEqual(expected, actual);
			}
			ir.Dispose();
			ir2.Dispose();
			dir.Dispose();
		}

		// tries to make more dups than testSorted
		[Test]
		public virtual void TestSortedWithLotsOfDups()
		{
			Directory dir = NewDirectory();
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			BytesRef @ref = new BytesRef();
			Field field = new SortedDocValuesField("bytes", @ref);
			doc.Add(field);
			IndexWriterConfig iwc = NewIndexWriterConfig(Random(), TEST_VERSION_CURRENT, null
				);
			iwc.SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, iwc);
			int numDocs = AtLeast(500);
			for (int i = 0; i < numDocs; i++)
			{
				@ref.CopyChars(TestUtil.RandomSimpleString(Random(), 2));
				iw.AddDocument(doc);
				if (Random().Next(17) == 0)
				{
					iw.Commit();
				}
			}
			DirectoryReader ir = iw.Reader;
			iw.ForceMerge(1);
			DirectoryReader ir2 = iw.Reader;
			AtomicReader merged = GetOnlySegmentReader(ir2);
			iw.Dispose();
			SortedDocValues multi = MultiDocValues.GetSortedValues(ir, "bytes");
			SortedDocValues single = merged.GetSortedDocValues("bytes");
			AreEqual(single.ValueCount, multi.ValueCount);
			BytesRef actual = new BytesRef();
			BytesRef expected = new BytesRef();
			for (int i_1 = 0; i_1 < numDocs; i_1++)
			{
				// check ord
				AreEqual(single.GetOrd(i_1), multi.GetOrd(i_1));
				// check ord value
				single.Get(i_1, expected);
				multi.Get(i_1, actual);
				AreEqual(expected, actual);
			}
			ir.Dispose();
			ir2.Dispose();
			dir.Dispose();
		}

		[Test]
		public virtual void TestSortedSet()
		{
			AssumeTrue("codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
			Directory dir = NewDirectory();
			IndexWriterConfig iwc = NewIndexWriterConfig(Random(), TEST_VERSION_CURRENT, null
				);
			iwc.SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, iwc);
			int numDocs = AtLeast(500);
			for (int i = 0; i < numDocs; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				int numValues = Random().Next(5);
				for (int j = 0; j < numValues; j++)
				{
					doc.Add(new SortedSetDocValuesField("bytes", new BytesRef(TestUtil.RandomUnicodeString
						(Random()))));
				}
				iw.AddDocument(doc);
				if (Random().Next(17) == 0)
				{
					iw.Commit();
				}
			}
			DirectoryReader ir = iw.Reader;
			iw.ForceMerge(1);
			DirectoryReader ir2 = iw.Reader;
			AtomicReader merged = GetOnlySegmentReader(ir2);
			iw.Dispose();
			SortedSetDocValues multi = MultiDocValues.GetSortedSetValues(ir, "bytes");
			SortedSetDocValues single = merged.GetSortedSetDocValues("bytes");
			if (multi == null)
			{
				IsNull(single);
			}
			else
			{
				AreEqual(single.ValueCount, multi.ValueCount);
				BytesRef actual = new BytesRef();
				BytesRef expected = new BytesRef();
				// check values
				for (long i_1 = 0; i_1 < single.ValueCount; i_1++)
				{
					single.LookupOrd(i_1, expected);
					multi.LookupOrd(i_1, actual);
					AreEqual(expected, actual);
				}
				// check ord list
				for (int i_2 = 0; i_2 < numDocs; i_2++)
				{
					single.SetDocument(i_2);
					List<long> expectedList = new List<long>();
					long ord;
					while ((ord = single.NextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
					{
						expectedList.Add(ord);
					}
					multi.SetDocument(i_2);
					int upto = 0;
					while ((ord = multi.NextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
					{
						AreEqual(expectedList[upto], ord);
						upto++;
					}
					AreEqual(expectedList.Count, upto);
				}
			}
			ir.Dispose();
			ir2.Dispose();
			dir.Dispose();
		}

		// tries to make more dups than testSortedSet
		[Test]
		public virtual void TestSortedSetWithDups()
		{
			AssumeTrue("codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
			Directory dir = NewDirectory();
			IndexWriterConfig iwc = NewIndexWriterConfig(Random(), TEST_VERSION_CURRENT, null
				);
			iwc.SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, iwc);
			int numDocs = AtLeast(500);
			for (int i = 0; i < numDocs; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				int numValues = Random().Next(5);
				for (int j = 0; j < numValues; j++)
				{
					doc.Add(new SortedSetDocValuesField("bytes", new BytesRef(TestUtil.RandomSimpleString
						(Random(), 2))));
				}
				iw.AddDocument(doc);
				if (Random().Next(17) == 0)
				{
					iw.Commit();
				}
			}
			DirectoryReader ir = iw.Reader;
			iw.ForceMerge(1);
			DirectoryReader ir2 = iw.Reader;
			AtomicReader merged = GetOnlySegmentReader(ir2);
			iw.Dispose();
			SortedSetDocValues multi = MultiDocValues.GetSortedSetValues(ir, "bytes");
			SortedSetDocValues single = merged.GetSortedSetDocValues("bytes");
			if (multi == null)
			{
				IsNull(single);
			}
			else
			{
				AreEqual(single.ValueCount, multi.ValueCount);
				BytesRef actual = new BytesRef();
				BytesRef expected = new BytesRef();
				// check values
				for (long i_1 = 0; i_1 < single.ValueCount; i_1++)
				{
					single.LookupOrd(i_1, expected);
					multi.LookupOrd(i_1, actual);
					AreEqual(expected, actual);
				}
				// check ord list
				for (int i_2 = 0; i_2 < numDocs; i_2++)
				{
					single.SetDocument(i_2);
					List<long> expectedList = new List<long>();
					long ord;
					while ((ord = single.NextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
					{
						expectedList.Add(ord);
					}
					multi.SetDocument(i_2);
					int upto = 0;
					while ((ord = multi.NextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
					{
						AreEqual(expectedList[upto], ord);
						upto++;
					}
					AreEqual(expectedList.Count, upto);
				}
			}
			ir.Dispose();
			ir2.Dispose();
			dir.Dispose();
		}

		[Test]
		public virtual void TestDocsWithField()
		{
			AssumeTrue("codec does not support docsWithField", DefaultCodecSupportsDocsWithField
				());
			Directory dir = NewDirectory();
			IndexWriterConfig iwc = NewIndexWriterConfig(Random(), TEST_VERSION_CURRENT, null
				);
			iwc.SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, iwc);
			int numDocs = AtLeast(500);
			for (int i = 0; i < numDocs; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				if (Random().Next(4) >= 0)
				{
					doc.Add(new NumericDocValuesField("numbers", Random().NextLong(0,long.MaxValue)));
				}
				doc.Add(new NumericDocValuesField("numbersAlways", Random().NextLong(0,long.MaxValue)));
				iw.AddDocument(doc);
				if (Random().Next(17) == 0)
				{
					iw.Commit();
				}
			}
			DirectoryReader ir = iw.Reader;
			iw.ForceMerge(1);
			DirectoryReader ir2 = iw.Reader;
			AtomicReader merged = GetOnlySegmentReader(ir2);
			iw.Dispose();
			IBits multi = MultiDocValues.GetDocsWithField(ir, "numbers");
			IBits single = merged.GetDocsWithField("numbers");
			if (multi == null)
			{
				IsNull(single);
			}
			else
			{
				AreEqual(single.Length, multi.Length);
				for (int i = 0; i < numDocs; i++)
				{
					AreEqual(single[i], multi[i]);
				}
			}
			multi = MultiDocValues.GetDocsWithField(ir, "numbersAlways");
			single = merged.GetDocsWithField("numbersAlways");
			AreEqual(single.Length, multi.Length);
			for (int j = 0; j < numDocs; j++)
			{
				AreEqual(single[j], multi[j]);
			}
			ir.Dispose();
			ir2.Dispose();
			dir.Dispose();
		}
	}
}
