using System;
using System.Threading;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.TestFramework;
using Lucene.Net.TestFramework.Index;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Index
{
	/// <summary>Tests DocValues integration into IndexWriter</summary>
	[TestFixture]
    public class TestDocValuesIndexing : LuceneTestCase
	{
		/// <exception cref="System.IO.IOException"></exception>
		[Test]
        public virtual void TestAddIndexes()
		{
			Directory d1 = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), d1);
			var doc = new Lucene.Net.Documents.Document
			{
			    NewStringField("id", "1", Field.Store.YES),
			    new NumericDocValuesField("dv", 1)
			};
		    w.AddDocument(doc);
			IndexReader r1 = w.Reader;
			w.Close();
			Directory d2 = NewDirectory();
			w = new RandomIndexWriter(Random(), d2);
			doc = new Lucene.Net.Documents.Document
			{
			    NewStringField("id", "2", Field.Store.YES),
			    new NumericDocValuesField("dv", 2)
			};
		    w.AddDocument(doc);
			IndexReader r2 = w.Reader;
			w.Close();
			Directory d3 = NewDirectory();
			w = new RandomIndexWriter(Random(), d3);
			w.AddIndexes(SlowCompositeReaderWrapper.Wrap(r1), SlowCompositeReaderWrapper.Wrap
				(r2));
			r1.Dispose();
			d1.Dispose();
			r2.Dispose();
			d2.Dispose();
			w.ForceMerge(1);
			DirectoryReader r3 = w.Reader;
			w.Close();
			AtomicReader sr = GetOnlySegmentReader(r3);
			AreEqual(2, sr.NumDocs);
			NumericDocValues docValues = sr.GetNumericDocValues("dv");
			IsNotNull(docValues);
			r3.Dispose();
			d3.Dispose();
		}

		[Test]
		public virtual void TestMultiValuedDocValuesField()
		{
			Directory d = NewDirectory();
			var w = new RandomIndexWriter(Random(), d);
			var doc = new Lucene.Net.Documents.Document();
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
			DirectoryReader r = w.Reader;
			w.Close();
			AreEqual(17, FieldCache.DEFAULT.GetInts(GetOnlySegmentReader
				(r), "field", false).Get(0));
			r.Dispose();
			d.Dispose();
		}

		[Test]
		public virtual void TestDifferentTypedDocValuesField()
		{
			Directory d = NewDirectory();
			var w = new RandomIndexWriter(Random(), d);
			var doc = new Lucene.Net.Documents.Document();
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
			doc = new Lucene.Net.Documents.Document {f};
		    w.AddDocument(doc);
			w.ForceMerge(1);
			DirectoryReader r = w.Reader;
			w.Close();
			AreEqual(17, FieldCache.DEFAULT.GetInts(GetOnlySegmentReader
				(r), "field", false).Get(0));
			r.Dispose();
			d.Dispose();
		}

		[Test]
		public virtual void TestDifferentTypedDocValuesField2()
		{
			Directory d = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), d);
			var doc = new Lucene.Net.Documents.Document();
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
			doc = new Lucene.Net.Documents.Document {f};
		    w.AddDocument(doc);
			w.ForceMerge(1);
			DirectoryReader r = w.Reader;
			AreEqual(17, GetOnlySegmentReader(r).GetNumericDocValues("field"
				).Get(0));
			r.Dispose();
			w.Close();
			d.Dispose();
		}

		// LUCENE-3870
		[Test]
		public virtual void TestLengthPrefixAcrossTwoPages()
		{
			Directory d = NewDirectory();
			IndexWriter w = new IndexWriter(d, new IndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
			var bytes = new sbyte[32764];
			BytesRef b = new BytesRef {bytes = bytes, length = bytes.Length};
		    doc.Add(new SortedDocValuesField("field", b));
			w.AddDocument(doc);
			bytes[0] = 1;
			w.AddDocument(doc);
			w.ForceMerge(1);
			DirectoryReader r = w.Reader;
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
			r.Dispose();
			w.Dispose();
			d.Dispose();
		}

		[Test]
		public virtual void TestDocValuesUnstored()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig iwconfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			iwconfig.SetMergePolicy(NewLogMergePolicy());
			IndexWriter writer = new IndexWriter(dir, iwconfig);
			for (int i = 0; i < 50; i++)
			{
				var doc = new Lucene.Net.Documents.Document
				{
				    new NumericDocValuesField("dv", i),
				    new TextField("docId", string.Empty + i, Field.Store.YES)
				};
			    writer.AddDocument(doc);
			}
			DirectoryReader r = writer.Reader;
			AtomicReader slow = SlowCompositeReaderWrapper.Wrap(r);
			FieldInfos fi = slow.FieldInfos;
			FieldInfo dvInfo = fi.FieldInfo("dv");
			IsTrue(dvInfo.HasDocValues);
			NumericDocValues dv = slow.GetNumericDocValues("dv");
			for (int i = 0; i < 50; i++)
			{
				AreEqual(i, dv.Get(i));
				Lucene.Net.Documents.Document d = slow.Document(i);
				// cannot use d.get("dv") due to another bug!
				IsNull(d.GetField("dv"));
				AreEqual(i.ToString(), d.Get("docId"));
			}
			slow.Dispose();
			writer.Dispose();
			dir.Dispose();
		}

		// Same field in one document as different types:
		[Test]
		public virtual void TestMixedTypesSameDocument()
		{
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())));
			var doc = new Lucene.Net.Documents.Document
			{
			    new NumericDocValuesField("foo", 0),
			    new SortedDocValuesField("foo", new BytesRef("hello"))
			};
		    try
			{
				w.AddDocument(doc);
			}
			catch (ArgumentException)
			{
			}
			// expected
			w.Dispose();
			dir.Dispose();
		}

		// Two documents with same field as different types:
		[Test]
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
			w.Dispose();
			dir.Dispose();
		}

		[Test]
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
			iwriter.Dispose();
			directory.Dispose();
		}

		[Test]
		public virtual void TestAddBinaryTwice()
		{
			Analyzer analyzer = new MockAnalyzer(Random());
			Directory directory = NewDirectory();
			// we don't use RandomIndexWriter because it might add more docvalues than we expect !!!!1
			IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			iwc.SetMergePolicy(NewLogMergePolicy());
			IndexWriter iwriter = new IndexWriter(directory, iwc);
			var doc = new Lucene.Net.Documents.Document
			{
			    new BinaryDocValuesField("dv", new BytesRef("foo!")),
			    new BinaryDocValuesField("dv", new BytesRef("bar!"))
			};
		    try
			{
				iwriter.AddDocument(doc);
				Fail("didn't hit expected exception");
			}
			catch (ArgumentException)
			{
			}
			// expected
			iwriter.Dispose();
			directory.Dispose();
		}

		[Test]
		public virtual void TestAddNumericTwice()
		{
			Analyzer analyzer = new MockAnalyzer(Random());
			Directory directory = NewDirectory();
			// we don't use RandomIndexWriter because it might add more docvalues than we expect !!!!1
			IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			iwc.SetMergePolicy(NewLogMergePolicy());
			IndexWriter iwriter = new IndexWriter(directory, iwc);
			var doc = new Lucene.Net.Documents.Document {new NumericDocValuesField("dv", 1), new NumericDocValuesField("dv", 2)};
		    try
			{
				iwriter.AddDocument(doc);
				Fail("didn't hit expected exception");
			}
			catch (ArgumentException)
			{
			}
			// expected
			iwriter.Dispose();
			directory.Dispose();
		}

		[Test]
		public virtual void TestTooLargeSortedBytes()
		{
			Analyzer analyzer = new MockAnalyzer(Random());
			Directory directory = NewDirectory();
			// we don't use RandomIndexWriter because it might add more docvalues than we expect !!!!1
			IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			iwc.SetMergePolicy(NewLogMergePolicy());
			IndexWriter iwriter = new IndexWriter(directory, iwc);
			var doc = new Lucene.Net.Documents.Document();
			var bytes = new sbyte[100000];
			BytesRef b = new BytesRef(bytes);
			Random().NextBytes(bytes.ToBytes());
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
			iwriter.Dispose();
			directory.Dispose();
		}

		[Test]
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
			var bytes = new sbyte[100000];
			BytesRef b = new BytesRef(bytes);
			Random().NextBytes(bytes.ToBytes());
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
			iwriter.Dispose();
			directory.Dispose();
		}

		// Two documents across segments
		[Test]
		public virtual void TestMixedTypesDifferentSegments()
		{
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())));
			var doc = new Lucene.Net.Documents.Document {new NumericDocValuesField("foo", 0)};
		    w.AddDocument(doc);
			w.Commit();
			doc = new Lucene.Net.Documents.Document {new SortedDocValuesField("foo", new BytesRef("hello"))};
		    try
			{
				w.AddDocument(doc);
			}
			catch (ArgumentException)
			{
			}
			// expected
			w.Dispose();
			dir.Dispose();
		}

		// Add inconsistent document after deleteAll
		[Test]
		public virtual void TestMixedTypesAfterDeleteAll()
		{
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())));
			var doc = new Lucene.Net.Documents.Document {new NumericDocValuesField("foo", 0)};
		    w.AddDocument(doc);
			w.DeleteAll();
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new SortedDocValuesField("foo", new BytesRef("hello")));
			w.AddDocument(doc);
			w.Dispose();
			dir.Dispose();
		}

		// Add inconsistent document after reopening IW w/ create
		[Test]
		public virtual void TestMixedTypesAfterReopenCreate()
		{
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new NumericDocValuesField("foo", 0));
			w.AddDocument(doc);
			w.Dispose();
			IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			iwc.SetOpenMode(IndexWriterConfig.OpenMode.CREATE);
			w = new IndexWriter(dir, iwc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new SortedDocValuesField("foo", new BytesRef("hello")));
			w.AddDocument(doc);
			w.Dispose();
			dir.Dispose();
		}

		// Two documents with same field as different types, added
		// from separate threads:
		[Test]
		public virtual void TestMixedTypesDifferentThreads()
		{
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())));
			CountdownEvent startingGun = new CountdownEvent(1);
			AtomicBoolean hitExc = new AtomicBoolean();
			Thread[] threads = new Thread[3];
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
				var doc = new Lucene.Net.Documents.Document {field};
			    threads[i] = new Thread(new ThreadRunner(startingGun, w, doc, hitExc).Run);
				// expected
				threads[i].Start();
			}
			startingGun.Signal();
			foreach (Thread t in threads)
			{
				t.Join();
			}
			IsTrue(hitExc.Get());
			w.Dispose();
			dir.Dispose();
		}

		private sealed class ThreadRunner
		{
			public ThreadRunner(CountdownEvent startingGun, IndexWriter w, Lucene.Net.Documents.Document
				 doc, AtomicBoolean hitExc)
			{
				this.startingGun = startingGun;
				this.w = w;
				this.doc = doc;
				this.hitExc = hitExc;
			}

			public void Run()
			{
				try
				{
					startingGun.Wait();
					w.AddDocument(doc);
				}
				catch (ArgumentException)
				{
					hitExc.Set(true);
				}
				catch (Exception e)
				{
					throw new SystemException();
				}
			}

			private readonly CountdownEvent startingGun;

			private readonly IndexWriter w;

			private readonly Lucene.Net.Documents.Document doc;

			private readonly AtomicBoolean hitExc;
		}

		// Adding documents via addIndexes
		[Test]
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
			w2.Dispose();
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
			r.Dispose();
			dir2.Dispose();
			w.Dispose();
			dir.Dispose();
		}

		[Test]
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
			doc = new Lucene.Net.Documents.Document {new SortedDocValuesField("dv", new BytesRef("foo"))};
		    try
			{
				writer.AddDocument(doc);
				Fail("did not hit exception");
			}
			catch (ArgumentException)
			{
			}
			// expected
			writer.Dispose();
			dir.Dispose();
		}

		[Test]
		public virtual void TestIllegalTypeChangeAcrossSegments()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			IndexWriter writer = new IndexWriter(dir, (IndexWriterConfig) conf.Clone());
			var doc = new Lucene.Net.Documents.Document {new NumericDocValuesField("dv", 0L)};
		    writer.AddDocument(doc);
			writer.Dispose();
			writer = new IndexWriter(dir, (IndexWriterConfig) conf.Clone());
			doc = new Lucene.Net.Documents.Document {new SortedDocValuesField("dv", new BytesRef("foo"))};
		    try
			{
				writer.AddDocument(doc);
				Fail("did not hit exception");
			}
			catch (ArgumentException)
			{
			}
			// expected
			writer.Dispose();
			dir.Dispose();
		}

		[Test]
		public virtual void TestTypeChangeAfterCloseAndDeleteAll()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			IndexWriter writer = new IndexWriter(dir, (IndexWriterConfig) conf.Clone());
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new NumericDocValuesField("dv", 0L));
			writer.AddDocument(doc);
			writer.Dispose();
			writer = new IndexWriter(dir, (IndexWriterConfig) conf.Clone());
			writer.DeleteAll();
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new SortedDocValuesField("dv", new BytesRef("foo")));
			writer.AddDocument(doc);
			writer.Dispose();
			dir.Dispose();
		}

		[Test]
		public virtual void TestTypeChangeAfterDeleteAll()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			IndexWriter writer = new IndexWriter(dir, conf);
			var doc = new Lucene.Net.Documents.Document {new NumericDocValuesField("dv", 0L)};
		    writer.AddDocument(doc);
			writer.DeleteAll();
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new SortedDocValuesField("dv", new BytesRef("foo")));
			writer.AddDocument(doc);
			writer.Dispose();
			dir.Dispose();
		}

		[Test]
		public virtual void TestTypeChangeAfterCommitAndDeleteAll()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			IndexWriter writer = new IndexWriter(dir, conf);
			var doc = new Lucene.Net.Documents.Document {new NumericDocValuesField("dv", 0L)};
		    writer.AddDocument(doc);
			writer.Commit();
			writer.DeleteAll();
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new SortedDocValuesField("dv", new BytesRef("foo")));
			writer.AddDocument(doc);
			writer.Dispose();
			dir.Dispose();
		}

		[Test]
		public virtual void TestTypeChangeAfterOpenCreate()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			IndexWriter writer = new IndexWriter(dir, (IndexWriterConfig) conf.Clone());
			var doc = new Lucene.Net.Documents.Document {new NumericDocValuesField("dv", 0L)};
		    writer.AddDocument(doc);
			writer.Dispose();
			conf.SetOpenMode(IndexWriterConfig.OpenMode.CREATE);
			writer = new IndexWriter(dir, (IndexWriterConfig) conf.Clone());
			doc = new Lucene.Net.Documents.Document {new SortedDocValuesField("dv", new BytesRef("foo"))};
		    writer.AddDocument(doc);
			writer.Dispose();
			dir.Dispose();
		}

		[Test]
		public virtual void TestTypeChangeViaAddIndexes()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			IndexWriter writer = new IndexWriter(dir, (IndexWriterConfig) conf.Clone());
			var doc = new Lucene.Net.Documents.Document {new NumericDocValuesField("dv", 0L)};
		    writer.AddDocument(doc);
			writer.Dispose();
			Directory dir2 = NewDirectory();
			writer = new IndexWriter(dir2, (IndexWriterConfig) conf.Clone());
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
			writer.Dispose();
			dir.Dispose();
			dir2.Dispose();
		}

		[Test]
		public virtual void TestTypeChangeViaAddIndexesIR()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			IndexWriter writer = new IndexWriter(dir, (IndexWriterConfig) conf.Clone());
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document {new NumericDocValuesField("dv", 0L)};
		    writer.AddDocument(doc);
			writer.Dispose();
			Directory dir2 = NewDirectory();
			writer = new IndexWriter(dir2, (IndexWriterConfig) conf.Clone());
			doc = new Lucene.Net.Documents.Document {new SortedDocValuesField("dv", new BytesRef("foo"))};
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
			readers[0].Dispose();
			writer.Dispose();
			dir.Dispose();
			dir2.Dispose();
		}

		[Test]
		public virtual void TestTypeChangeViaAddIndexes2()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			IndexWriter writer = new IndexWriter(dir, (IndexWriterConfig) conf.Clone());
			var doc = new Lucene.Net.Documents.Document {new NumericDocValuesField("dv", 0L)};
		    writer.AddDocument(doc);
			writer.Dispose();
			Directory dir2 = NewDirectory();
			writer = new IndexWriter(dir2, (IndexWriterConfig) conf.Clone());
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
			writer.Dispose();
			dir2.Dispose();
			dir.Dispose();
		}

		[Test]
		public virtual void TestTypeChangeViaAddIndexesIR2()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			IndexWriter writer = new IndexWriter(dir, (IndexWriterConfig) conf.Clone());
			var doc = new Lucene.Net.Documents.Document {new NumericDocValuesField("dv", 0L)};
		    writer.AddDocument(doc);
			writer.Dispose();
			Directory dir2 = NewDirectory();
			writer = new IndexWriter(dir2, (IndexWriterConfig) conf.Clone());
			IndexReader[] readers = { DirectoryReader.Open(dir) };
			writer.AddIndexes(readers);
			readers[0].Dispose();
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
			writer.Dispose();
			dir2.Dispose();
			dir.Dispose();
		}

		[Test]
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
			doc = new Lucene.Net.Documents.Document
			{
			    new TextField("dv", "some text", Field.Store.NO),
			    new NumericDocValuesField("dv", 0L)
			};
		    writer.AddDocument(doc);
			DirectoryReader r = writer.Reader;
			writer.Dispose();
			AtomicReader subR = ((AtomicReader)r.Leaves[0].Reader);
			AreEqual(2, subR.NumDocs);
			IBits bits = FieldCache.DEFAULT.GetDocsWithField(subR, "dv");
			IsTrue(bits[0]);
			IsTrue(bits[1]);
			r.Dispose();
			dir.Dispose();
		}

		[Test]
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
			dir.Dispose();
		}
	}
}
