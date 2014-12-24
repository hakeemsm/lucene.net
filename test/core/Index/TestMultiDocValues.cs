/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	/// <summary>Tests MultiDocValues versus ordinary segment merging</summary>
	public class TestMultiDocValues : LuceneTestCase
	{
		/// <exception cref="System.Exception"></exception>
		public virtual void TestNumerics()
		{
			Directory dir = NewDirectory();
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			Field field = new NumericDocValuesField("numbers", 0);
			doc.Add(field);
			IndexWriterConfig iwc = NewIndexWriterConfig(Random(), TEST_VERSION_CURRENT, null
				);
			iwc.SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, iwc);
			int numDocs = AtLeast(500);
			for (int i = 0; i < numDocs; i++)
			{
				field.SetLongValue(Random().NextLong());
				iw.AddDocument(doc);
				if (Random().Next(17) == 0)
				{
					iw.Commit();
				}
			}
			DirectoryReader ir = iw.GetReader();
			iw.ForceMerge(1);
			DirectoryReader ir2 = iw.GetReader();
			AtomicReader merged = GetOnlySegmentReader(ir2);
			iw.Close();
			NumericDocValues multi = MultiDocValues.GetNumericValues(ir, "numbers");
			NumericDocValues single = merged.GetNumericDocValues("numbers");
			for (int i_1 = 0; i_1 < numDocs; i_1++)
			{
				AreEqual(single.Get(i_1), multi.Get(i_1));
			}
			ir.Close();
			ir2.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
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
			DirectoryReader ir = iw.GetReader();
			iw.ForceMerge(1);
			DirectoryReader ir2 = iw.GetReader();
			AtomicReader merged = GetOnlySegmentReader(ir2);
			iw.Close();
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
			ir.Close();
			ir2.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
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
			DirectoryReader ir = iw.GetReader();
			iw.ForceMerge(1);
			DirectoryReader ir2 = iw.GetReader();
			AtomicReader merged = GetOnlySegmentReader(ir2);
			iw.Close();
			SortedDocValues multi = MultiDocValues.GetSortedValues(ir, "bytes");
			SortedDocValues single = merged.GetSortedDocValues("bytes");
			AreEqual(single.GetValueCount(), multi.GetValueCount());
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
			ir.Close();
			ir2.Close();
			dir.Close();
		}

		// tries to make more dups than testSorted
		/// <exception cref="System.Exception"></exception>
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
			DirectoryReader ir = iw.GetReader();
			iw.ForceMerge(1);
			DirectoryReader ir2 = iw.GetReader();
			AtomicReader merged = GetOnlySegmentReader(ir2);
			iw.Close();
			SortedDocValues multi = MultiDocValues.GetSortedValues(ir, "bytes");
			SortedDocValues single = merged.GetSortedDocValues("bytes");
			AreEqual(single.GetValueCount(), multi.GetValueCount());
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
			ir.Close();
			ir2.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
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
			DirectoryReader ir = iw.GetReader();
			iw.ForceMerge(1);
			DirectoryReader ir2 = iw.GetReader();
			AtomicReader merged = GetOnlySegmentReader(ir2);
			iw.Close();
			SortedSetDocValues multi = MultiDocValues.GetSortedSetValues(ir, "bytes");
			SortedSetDocValues single = merged.GetSortedSetDocValues("bytes");
			if (multi == null)
			{
				IsNull(single);
			}
			else
			{
				AreEqual(single.GetValueCount(), multi.GetValueCount());
				BytesRef actual = new BytesRef();
				BytesRef expected = new BytesRef();
				// check values
				for (long i_1 = 0; i_1 < single.GetValueCount(); i_1++)
				{
					single.LookupOrd(i_1, expected);
					multi.LookupOrd(i_1, actual);
					AreEqual(expected, actual);
				}
				// check ord list
				for (int i_2 = 0; i_2 < numDocs; i_2++)
				{
					single.SetDocument(i_2);
					AList<long> expectedList = new AList<long>();
					long ord;
					while ((ord = single.NextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
					{
						expectedList.AddItem(ord);
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
			ir.Close();
			ir2.Close();
			dir.Close();
		}

		// tries to make more dups than testSortedSet
		/// <exception cref="System.Exception"></exception>
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
			DirectoryReader ir = iw.GetReader();
			iw.ForceMerge(1);
			DirectoryReader ir2 = iw.GetReader();
			AtomicReader merged = GetOnlySegmentReader(ir2);
			iw.Close();
			SortedSetDocValues multi = MultiDocValues.GetSortedSetValues(ir, "bytes");
			SortedSetDocValues single = merged.GetSortedSetDocValues("bytes");
			if (multi == null)
			{
				IsNull(single);
			}
			else
			{
				AreEqual(single.GetValueCount(), multi.GetValueCount());
				BytesRef actual = new BytesRef();
				BytesRef expected = new BytesRef();
				// check values
				for (long i_1 = 0; i_1 < single.GetValueCount(); i_1++)
				{
					single.LookupOrd(i_1, expected);
					multi.LookupOrd(i_1, actual);
					AreEqual(expected, actual);
				}
				// check ord list
				for (int i_2 = 0; i_2 < numDocs; i_2++)
				{
					single.SetDocument(i_2);
					AList<long> expectedList = new AList<long>();
					long ord;
					while ((ord = single.NextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
					{
						expectedList.AddItem(ord);
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
			ir.Close();
			ir2.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
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
					doc.Add(new NumericDocValuesField("numbers", Random().NextLong()));
				}
				doc.Add(new NumericDocValuesField("numbersAlways", Random().NextLong()));
				iw.AddDocument(doc);
				if (Random().Next(17) == 0)
				{
					iw.Commit();
				}
			}
			DirectoryReader ir = iw.GetReader();
			iw.ForceMerge(1);
			DirectoryReader ir2 = iw.GetReader();
			AtomicReader merged = GetOnlySegmentReader(ir2);
			iw.Close();
			Bits multi = MultiDocValues.GetDocsWithField(ir, "numbers");
			Bits single = merged.GetDocsWithField("numbers");
			if (multi == null)
			{
				IsNull(single);
			}
			else
			{
				AreEqual(single.Length(), multi.Length());
				for (int i_1 = 0; i_1 < numDocs; i_1++)
				{
					AreEqual(single.Get(i_1), multi.Get(i_1));
				}
			}
			multi = MultiDocValues.GetDocsWithField(ir, "numbersAlways");
			single = merged.GetDocsWithField("numbersAlways");
			AreEqual(single.Length(), multi.Length());
			for (int i_2 = 0; i_2 < numDocs; i_2++)
			{
				AreEqual(single.Get(i_2), multi.Get(i_2));
			}
			ir.Close();
			ir2.Close();
			dir.Close();
		}
	}
}
