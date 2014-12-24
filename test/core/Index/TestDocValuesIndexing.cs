/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	/// <summary>Tests DocValues integration into IndexWriter</summary>
	public class TestDocValuesIndexing : LuceneTestCase
	{
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestAddIndexes()
		{
			Directory d1 = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), d1);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewStringField("id", "1", Field.Store.YES));
			doc.Add(new NumericDocValuesField("dv", 1));
			w.AddDocument(doc);
			IndexReader r1 = w.GetReader();
			w.Close();
			Directory d2 = NewDirectory();
			w = new RandomIndexWriter(Random(), d2);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("id", "2", Field.Store.YES));
			doc.Add(new NumericDocValuesField("dv", 2));
			w.AddDocument(doc);
			IndexReader r2 = w.GetReader();
			w.Close();
			Directory d3 = NewDirectory();
			w = new RandomIndexWriter(Random(), d3);
			w.AddIndexes(SlowCompositeReaderWrapper.Wrap(r1), SlowCompositeReaderWrapper.Wrap
				(r2));
			r1.Close();
			d1.Close();
			r2.Close();
			d2.Close();
			w.ForceMerge(1);
			DirectoryReader r3 = w.GetReader();
			w.Close();
			AtomicReader sr = GetOnlySegmentReader(r3);
			AreEqual(2, sr.NumDocs());
			NumericDocValues docValues = sr.GetNumericDocValues("dv");
			IsNotNull(docValues);
			r3.Close();
			d3.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestMultiValuedDocValuesField()
		{
			Directory d = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), d);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			Field f = new NumericDocValuesField("field", 17);
			// Index doc values are single-valued so we should not
			// be able to add same field more than once:
			doc.Add(f);
			doc.Add(f);
			try
			{
				w.AddDocument(doc);
				Fail("didn't hit expected exception");
			}
			catch (ArgumentException)
			{
			}
			// expected
			doc = new Lucene.Net.Documents.Document();
			doc.Add(f);
			w.AddDocument(doc);
			w.ForceMerge(1);
			DirectoryReader r = w.GetReader();
			w.Close();
			AreEqual(17, FieldCache.DEFAULT.GetInts(GetOnlySegmentReader
				(r), "field", false).Get(0));
			r.Close();
			d.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestDifferentTypedDocValuesField()
		{
			Directory d = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), d);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			// Index doc values are single-valued so we should not
			// be able to add same field more than once:
			Field f;
			doc.Add(f = new NumericDocValuesField("field", 17));
			doc.Add(new BinaryDocValuesField("field", new BytesRef("blah")));
			try
			{
				w.AddDocument(doc);
				Fail("didn't hit expected exception");
			}
			catch (ArgumentException)
			{
			}
			// expected
			doc = new Lucene.Net.Documents.Document();
			doc.Add(f);
			w.AddDocument(doc);
			w.ForceMerge(1);
			DirectoryReader r = w.GetReader();
			w.Close();
			AreEqual(17, FieldCache.DEFAULT.GetInts(GetOnlySegmentReader
				(r), "field", false).Get(0));
			r.Close();
			d.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestDifferentTypedDocValuesField2()
		{
			Directory d = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), d);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			// Index doc values are single-valued so we should not
			// be able to add same field more than once:
			Field f = new NumericDocValuesField("field", 17);
			doc.Add(f);
			doc.Add(new SortedDocValuesField("field", new BytesRef("hello")));
			try
			{
				w.AddDocument(doc);
				Fail("didn't hit expected exception");
			}
			catch (ArgumentException)
			{
			}
			// expected
			doc = new Lucene.Net.Documents.Document();
			doc.Add(f);
			w.AddDocument(doc);
			w.ForceMerge(1);
			DirectoryReader r = w.GetReader();
			AreEqual(17, GetOnlySegmentReader(r).GetNumericDocValues("field"
				).Get(0));
			r.Close();
			w.Close();
			d.Close();
		}

		// LUCENE-3870
		/// <exception cref="System.Exception"></exception>
		public virtual void TestLengthPrefixAcrossTwoPages()
		{
			Directory d = NewDirectory();
			IndexWriter w = new IndexWriter(d, new IndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			byte[] bytes = new byte[32764];
			BytesRef b = new BytesRef();
			b.bytes = bytes;
			b.length = bytes.Length;
			doc.Add(new SortedDocValuesField("field", b));
			w.AddDocument(doc);
			bytes[0] = 1;
			w.AddDocument(doc);
			w.ForceMerge(1);
			DirectoryReader r = w.GetReader();
			BinaryDocValues s = FieldCache.DEFAULT.GetTerms(GetOnlySegmentReader(r), "field", 
				false);
			BytesRef bytes1 = new BytesRef();
			s.Get(0, bytes1);
			AreEqual(bytes.Length, bytes1.length);
			bytes[0] = 0;
			AreEqual(b, bytes1);
			s.Get(1, bytes1);
			AreEqual(bytes.Length, bytes1.length);
			bytes[0] = 1;
			AreEqual(b, bytes1);
			r.Close();
			w.Close();
			d.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestDocValuesUnstored()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig iwconfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			iwconfig.SetMergePolicy(NewLogMergePolicy());
			IndexWriter writer = new IndexWriter(dir, iwconfig);
			for (int i = 0; i < 50; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(new NumericDocValuesField("dv", i));
				doc.Add(new TextField("docId", string.Empty + i, Field.Store.YES));
				writer.AddDocument(doc);
			}
			DirectoryReader r = writer.GetReader();
			AtomicReader slow = SlowCompositeReaderWrapper.Wrap(r);
			FieldInfos fi = slow.GetFieldInfos();
			FieldInfo dvInfo = fi.FieldInfo("dv");
			IsTrue(dvInfo.HasDocValues());
			NumericDocValues dv = slow.GetNumericDocValues("dv");
			for (int i_1 = 0; i_1 < 50; i_1++)
			{
				AreEqual(i_1, dv.Get(i_1));
				Lucene.Net.Documents.Document d = slow.Document(i_1);
				// cannot use d.get("dv") due to another bug!
				IsNull(d.GetField("dv"));
				AreEqual(Sharpen.Extensions.ToString(i_1), d.Get("docId"));
			}
			slow.Close();
			writer.Close();
			dir.Close();
		}

		// Same field in one document as different types:
		/// <exception cref="System.Exception"></exception>
		public virtual void TestMixedTypesSameDocument()
		{
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new NumericDocValuesField("foo", 0));
			doc.Add(new SortedDocValuesField("foo", new BytesRef("hello")));
			try
			{
				w.AddDocument(doc);
			}
			catch (ArgumentException)
			{
			}
			// expected
			w.Close();
			dir.Close();
		}

		// Two documents with same field as different types:
		/// <exception cref="System.Exception"></exception>
		public virtual void TestMixedTypesDifferentDocuments()
		{
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new NumericDocValuesField("foo", 0));
			w.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new SortedDocValuesField("foo", new BytesRef("hello")));
			try
			{
				w.AddDocument(doc);
			}
			catch (ArgumentException)
			{
			}
			// expected
			w.Close();
			dir.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestAddSortedTwice()
		{
			Analyzer analyzer = new MockAnalyzer(Random());
			Directory directory = NewDirectory();
			// we don't use RandomIndexWriter because it might add more docvalues than we expect !!!!1
			IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			iwc.SetMergePolicy(NewLogMergePolicy());
			IndexWriter iwriter = new IndexWriter(directory, iwc);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new SortedDocValuesField("dv", new BytesRef("foo!")));
			doc.Add(new SortedDocValuesField("dv", new BytesRef("bar!")));
			try
			{
				iwriter.AddDocument(doc);
				Fail("didn't hit expected exception");
			}
			catch (ArgumentException)
			{
			}
			// expected
			iwriter.Close();
			directory.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestAddBinaryTwice()
		{
			Analyzer analyzer = new MockAnalyzer(Random());
			Directory directory = NewDirectory();
			// we don't use RandomIndexWriter because it might add more docvalues than we expect !!!!1
			IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			iwc.SetMergePolicy(NewLogMergePolicy());
			IndexWriter iwriter = new IndexWriter(directory, iwc);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new BinaryDocValuesField("dv", new BytesRef("foo!")));
			doc.Add(new BinaryDocValuesField("dv", new BytesRef("bar!")));
			try
			{
				iwriter.AddDocument(doc);
				Fail("didn't hit expected exception");
			}
			catch (ArgumentException)
			{
			}
			// expected
			iwriter.Close();
			directory.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestAddNumericTwice()
		{
			Analyzer analyzer = new MockAnalyzer(Random());
			Directory directory = NewDirectory();
			// we don't use RandomIndexWriter because it might add more docvalues than we expect !!!!1
			IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			iwc.SetMergePolicy(NewLogMergePolicy());
			IndexWriter iwriter = new IndexWriter(directory, iwc);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new NumericDocValuesField("dv", 1));
			doc.Add(new NumericDocValuesField("dv", 2));
			try
			{
				iwriter.AddDocument(doc);
				Fail("didn't hit expected exception");
			}
			catch (ArgumentException)
			{
			}
			// expected
			iwriter.Close();
			directory.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestTooLargeSortedBytes()
		{
			Analyzer analyzer = new MockAnalyzer(Random());
			Directory directory = NewDirectory();
			// we don't use RandomIndexWriter because it might add more docvalues than we expect !!!!1
			IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			iwc.SetMergePolicy(NewLogMergePolicy());
			IndexWriter iwriter = new IndexWriter(directory, iwc);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			byte[] bytes = new byte[100000];
			BytesRef b = new BytesRef(bytes);
			Random().NextBytes(bytes);
			doc.Add(new SortedDocValuesField("dv", b));
			try
			{
				iwriter.AddDocument(doc);
				Fail("did not get expected exception");
			}
			catch (ArgumentException)
			{
			}
			// expected
			iwriter.Close();
			directory.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestTooLargeTermSortedSetBytes()
		{
			AssumeTrue("codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
			Analyzer analyzer = new MockAnalyzer(Random());
			Directory directory = NewDirectory();
			// we don't use RandomIndexWriter because it might add more docvalues than we expect !!!!1
			IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			iwc.SetMergePolicy(NewLogMergePolicy());
			IndexWriter iwriter = new IndexWriter(directory, iwc);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			byte[] bytes = new byte[100000];
			BytesRef b = new BytesRef(bytes);
			Random().NextBytes(bytes);
			doc.Add(new SortedSetDocValuesField("dv", b));
			try
			{
				iwriter.AddDocument(doc);
				Fail("did not get expected exception");
			}
			catch (ArgumentException)
			{
			}
			// expected
			iwriter.Close();
			directory.Close();
		}

		// Two documents across segments
		/// <exception cref="System.Exception"></exception>
		public virtual void TestMixedTypesDifferentSegments()
		{
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new NumericDocValuesField("foo", 0));
			w.AddDocument(doc);
			w.Commit();
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new SortedDocValuesField("foo", new BytesRef("hello")));
			try
			{
				w.AddDocument(doc);
			}
			catch (ArgumentException)
			{
			}
			// expected
			w.Close();
			dir.Close();
		}

		// Add inconsistent document after deleteAll
		/// <exception cref="System.Exception"></exception>
		public virtual void TestMixedTypesAfterDeleteAll()
		{
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new NumericDocValuesField("foo", 0));
			w.AddDocument(doc);
			w.DeleteAll();
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new SortedDocValuesField("foo", new BytesRef("hello")));
			w.AddDocument(doc);
			w.Close();
			dir.Close();
		}

		// Add inconsistent document after reopening IW w/ create
		/// <exception cref="System.Exception"></exception>
		public virtual void TestMixedTypesAfterReopenCreate()
		{
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new NumericDocValuesField("foo", 0));
			w.AddDocument(doc);
			w.Close();
			IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			iwc.SetOpenMode(IndexWriterConfig.OpenMode.CREATE);
			w = new IndexWriter(dir, iwc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new SortedDocValuesField("foo", new BytesRef("hello")));
			w.AddDocument(doc);
			w.Close();
			dir.Close();
		}

		// Two documents with same field as different types, added
		// from separate threads:
		/// <exception cref="System.Exception"></exception>
		public virtual void TestMixedTypesDifferentThreads()
		{
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())));
			CountDownLatch startingGun = new CountDownLatch(1);
			AtomicBoolean hitExc = new AtomicBoolean();
			Sharpen.Thread[] threads = new Sharpen.Thread[3];
			for (int i = 0; i < 3; i++)
			{
				Field field;
				if (i == 0)
				{
					field = new SortedDocValuesField("foo", new BytesRef("hello"));
				}
				else
				{
					if (i == 1)
					{
						field = new NumericDocValuesField("foo", 0);
					}
					else
					{
						field = new BinaryDocValuesField("foo", new BytesRef("bazz"));
					}
				}
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(field);
				threads[i] = new _Thread_457(startingGun, w, doc, hitExc);
				// expected
				threads[i].Start();
			}
			startingGun.CountDown();
			foreach (Sharpen.Thread t in threads)
			{
				t.Join();
			}
			IsTrue(hitExc.Get());
			w.Close();
			dir.Close();
		}

		private sealed class _Thread_457 : Sharpen.Thread
		{
			public _Thread_457(CountDownLatch startingGun, IndexWriter w, Lucene.Net.Documents.Document
				 doc, AtomicBoolean hitExc)
			{
				this.startingGun = startingGun;
				this.w = w;
				this.doc = doc;
				this.hitExc = hitExc;
			}

			public override void Run()
			{
				try
				{
					startingGun.Await();
					w.AddDocument(doc);
				}
				catch (ArgumentException)
				{
					hitExc.Set(true);
				}
				catch (Exception e)
				{
					throw new RuntimeException(e);
				}
			}

			private readonly CountDownLatch startingGun;

			private readonly IndexWriter w;

			private readonly Lucene.Net.Documents.Document doc;

			private readonly AtomicBoolean hitExc;
		}

		// Adding documents via addIndexes
		/// <exception cref="System.Exception"></exception>
		public virtual void TestMixedTypesViaAddIndexes()
		{
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new NumericDocValuesField("foo", 0));
			w.AddDocument(doc);
			// Make 2nd index w/ inconsistent field
			Directory dir2 = NewDirectory();
			IndexWriter w2 = new IndexWriter(dir2, NewIndexWriterConfig(TEST_VERSION_CURRENT, 
				new MockAnalyzer(Random())));
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new SortedDocValuesField("foo", new BytesRef("hello")));
			w2.AddDocument(doc);
			w2.Close();
			try
			{
				w.AddIndexes(new Directory[] { dir2 });
			}
			catch (ArgumentException)
			{
			}
			// expected
			IndexReader r = DirectoryReader.Open(dir2);
			try
			{
				w.AddIndexes(new IndexReader[] { r });
			}
			catch (ArgumentException)
			{
			}
			// expected
			r.Close();
			dir2.Close();
			w.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestIllegalTypeChange()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			IndexWriter writer = new IndexWriter(dir, conf);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new NumericDocValuesField("dv", 0L));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new SortedDocValuesField("dv", new BytesRef("foo")));
			try
			{
				writer.AddDocument(doc);
				Fail("did not hit exception");
			}
			catch (ArgumentException)
			{
			}
			// expected
			writer.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestIllegalTypeChangeAcrossSegments()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			IndexWriter writer = new IndexWriter(dir, conf.Clone());
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new NumericDocValuesField("dv", 0L));
			writer.AddDocument(doc);
			writer.Close();
			writer = new IndexWriter(dir, conf.Clone());
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new SortedDocValuesField("dv", new BytesRef("foo")));
			try
			{
				writer.AddDocument(doc);
				Fail("did not hit exception");
			}
			catch (ArgumentException)
			{
			}
			// expected
			writer.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestTypeChangeAfterCloseAndDeleteAll()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			IndexWriter writer = new IndexWriter(dir, conf.Clone());
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new NumericDocValuesField("dv", 0L));
			writer.AddDocument(doc);
			writer.Close();
			writer = new IndexWriter(dir, conf.Clone());
			writer.DeleteAll();
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new SortedDocValuesField("dv", new BytesRef("foo")));
			writer.AddDocument(doc);
			writer.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestTypeChangeAfterDeleteAll()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			IndexWriter writer = new IndexWriter(dir, conf);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new NumericDocValuesField("dv", 0L));
			writer.AddDocument(doc);
			writer.DeleteAll();
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new SortedDocValuesField("dv", new BytesRef("foo")));
			writer.AddDocument(doc);
			writer.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestTypeChangeAfterCommitAndDeleteAll()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			IndexWriter writer = new IndexWriter(dir, conf);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new NumericDocValuesField("dv", 0L));
			writer.AddDocument(doc);
			writer.Commit();
			writer.DeleteAll();
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new SortedDocValuesField("dv", new BytesRef("foo")));
			writer.AddDocument(doc);
			writer.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestTypeChangeAfterOpenCreate()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			IndexWriter writer = new IndexWriter(dir, conf.Clone());
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new NumericDocValuesField("dv", 0L));
			writer.AddDocument(doc);
			writer.Close();
			conf.SetOpenMode(IndexWriterConfig.OpenMode.CREATE);
			writer = new IndexWriter(dir, conf.Clone());
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new SortedDocValuesField("dv", new BytesRef("foo")));
			writer.AddDocument(doc);
			writer.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestTypeChangeViaAddIndexes()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			IndexWriter writer = new IndexWriter(dir, conf.Clone());
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new NumericDocValuesField("dv", 0L));
			writer.AddDocument(doc);
			writer.Close();
			Directory dir2 = NewDirectory();
			writer = new IndexWriter(dir2, conf.Clone());
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new SortedDocValuesField("dv", new BytesRef("foo")));
			writer.AddDocument(doc);
			try
			{
				writer.AddIndexes(dir);
				Fail("did not hit exception");
			}
			catch (ArgumentException)
			{
			}
			// expected
			writer.Close();
			dir.Close();
			dir2.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestTypeChangeViaAddIndexesIR()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			IndexWriter writer = new IndexWriter(dir, conf.Clone());
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new NumericDocValuesField("dv", 0L));
			writer.AddDocument(doc);
			writer.Close();
			Directory dir2 = NewDirectory();
			writer = new IndexWriter(dir2, conf.Clone());
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new SortedDocValuesField("dv", new BytesRef("foo")));
			writer.AddDocument(doc);
			IndexReader[] readers = new IndexReader[] { DirectoryReader.Open(dir) };
			try
			{
				writer.AddIndexes(readers);
				Fail("did not hit exception");
			}
			catch (ArgumentException)
			{
			}
			// expected
			readers[0].Close();
			writer.Close();
			dir.Close();
			dir2.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestTypeChangeViaAddIndexes2()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			IndexWriter writer = new IndexWriter(dir, conf.Clone());
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new NumericDocValuesField("dv", 0L));
			writer.AddDocument(doc);
			writer.Close();
			Directory dir2 = NewDirectory();
			writer = new IndexWriter(dir2, conf.Clone());
			writer.AddIndexes(dir);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new SortedDocValuesField("dv", new BytesRef("foo")));
			try
			{
				writer.AddDocument(doc);
				Fail("did not hit exception");
			}
			catch (ArgumentException)
			{
			}
			// expected
			writer.Close();
			dir2.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestTypeChangeViaAddIndexesIR2()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			IndexWriter writer = new IndexWriter(dir, conf.Clone());
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new NumericDocValuesField("dv", 0L));
			writer.AddDocument(doc);
			writer.Close();
			Directory dir2 = NewDirectory();
			writer = new IndexWriter(dir2, conf.Clone());
			IndexReader[] readers = new IndexReader[] { DirectoryReader.Open(dir) };
			writer.AddIndexes(readers);
			readers[0].Close();
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new SortedDocValuesField("dv", new BytesRef("foo")));
			try
			{
				writer.AddDocument(doc);
				Fail("did not hit exception");
			}
			catch (ArgumentException)
			{
			}
			// expected
			writer.Close();
			dir2.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestDocsWithField()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			IndexWriter writer = new IndexWriter(dir, conf);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new NumericDocValuesField("dv", 0L));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new TextField("dv", "some text", Field.Store.NO));
			doc.Add(new NumericDocValuesField("dv", 0L));
			writer.AddDocument(doc);
			DirectoryReader r = writer.GetReader();
			writer.Close();
			AtomicReader subR = ((AtomicReader)r.Leaves()[0].Reader());
			AreEqual(2, subR.NumDocs());
			Bits bits = FieldCache.DEFAULT.GetDocsWithField(subR, "dv");
			IsTrue(bits.Get(0));
			IsTrue(bits.Get(1));
			r.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSameFieldNameForPostingAndDocValue()
		{
			// LUCENE-5192: FieldInfos.Builder neglected to update
			// globalFieldNumbers.docValuesType map if the field existed, resulting in
			// potentially adding the same field with different DV types.
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			IndexWriter writer = new IndexWriter(dir, conf);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new StringField("f", "mock-value", Field.Store.NO));
			doc.Add(new NumericDocValuesField("f", 5));
			writer.AddDocument(doc);
			writer.Commit();
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new BinaryDocValuesField("f", new BytesRef("mock")));
			try
			{
				writer.AddDocument(doc);
				Fail("should not have succeeded to add a field with different DV type than what already exists"
					);
			}
			catch (ArgumentException)
			{
				writer.Rollback();
			}
			dir.Close();
		}
	}
}
