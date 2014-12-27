using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Lucene.Net.Analysis;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Lucene40.TestFramework;
using Lucene.Net.Codecs.Lucene41.TestFramrwork;
using Lucene.Net.Codecs.Lucene42.TestFramework;
using Lucene.Net.Codecs.Lucene45;
using Lucene.Net.Codecs.Lucene45.TestFramework;
using Lucene.Net.Codecs.Lucene46;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Support;
using Lucene.Net.TestFramework.Util;
using Lucene.Net.Util;
using Lucene.Net.Store;
using NUnit.Framework;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Test.Index
{
    [TestFixture]
	public class TestBinaryDocValuesUpdates : LuceneTestCase
	{
		internal static long GetValue(BinaryDocValues bdv, int idx, BytesRef scratch)
		{
			bdv.Get(idx, scratch);
			idx = scratch.offset;
			byte b = (byte) scratch.bytes[idx++];
			long value = b & unchecked((long)(0x7FL));
			for (int shift = 7; (b & unchecked((long)(0x80L))) != 0; shift += 7)
			{
				b = (byte) scratch.bytes[idx++];
				value |= (b & unchecked((long)(0x7FL))) << shift;
			}
			return value;
		}

		// encodes a long into a BytesRef as VLong so that we get varying number of bytes when we update
		internal static BytesRef ToBytes(long value)
		{
			//    long orig = value;
			BytesRef bytes = new BytesRef(10);
			// negative longs may take 10 bytes
			while ((value & ~unchecked((long)(0x7FL))) != 0L)
			{
				bytes.bytes[bytes.length++] = (sbyte)((value & 0x7FL) | (0x80L));
				value = (long)(((ulong)value) >> 7);
			}
			bytes.bytes[bytes.length++] = ((sbyte)value);
			//    System.err.println("[" + Thread.currentThread().getName() + "] value=" + orig + ", bytes=" + bytes);
			return bytes;
		}

		private Lucene.Net.Documents.Document Doc(int id)
		{
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new StringField("id", "doc-" + id, Field.Store.NO));
			doc.Add(new BinaryDocValuesField("val", ToBytes(id + 1)));
			return doc;
		}

		[Test]
		public virtual void TestUpdatesAreFlushed()
		{
			var dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random(), MockTokenizer.WHITESPACE, false
				)).SetRAMBufferSizeMB(0.00000001)));
			writer.AddDocument(Doc(0));
			// val=1
			writer.AddDocument(Doc(1));
			// val=2
			writer.AddDocument(Doc(3));
			// val=2
			writer.Commit();
			AreEqual(1, writer.FlushDeletesCount);
			writer.UpdateBinaryDocValue(new Term("id", "doc-0"), "val", ToBytes(5));
			AreEqual(2, writer.FlushDeletesCount);
			writer.UpdateBinaryDocValue(new Term("id", "doc-1"), "val", ToBytes(6));
			AreEqual(3, writer.FlushDeletesCount);
			writer.UpdateBinaryDocValue(new Term("id", "doc-2"), "val", ToBytes(7));
			AreEqual(4, writer.FlushDeletesCount);
			writer.Config.SetRAMBufferSizeMB(1000d);
			writer.UpdateBinaryDocValue(new Term("id", "doc-2"), "val", ToBytes(7));
			AreEqual(4, writer.FlushDeletesCount);
			writer.Dispose();
			dir.Dispose();
		}

		[Test]
		public virtual void TestSimple()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			// make sure random config doesn't flush on us
			conf.SetMaxBufferedDocs(10);
			conf.SetRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH);
			IndexWriter writer = new IndexWriter(dir, conf);
			writer.AddDocument(Doc(0));
			// val=1
			writer.AddDocument(Doc(1));
			// val=2
			if (Random().NextBoolean())
			{
				// randomly commit before the update is sent
				writer.Commit();
			}
			writer.UpdateBinaryDocValue(new Term("id", "doc-0"), "val", ToBytes(2));
			// doc=0, exp=2
			DirectoryReader reader;
			if (Random().NextBoolean())
			{
				// not NRT
				writer.Dispose();
				reader = DirectoryReader.Open(dir);
			}
			else
			{
				// NRT
				reader = DirectoryReader.Open(writer, true);
				writer.Dispose();
			}
			AreEqual(1, reader.Leaves.Count);
			AtomicReader r = ((AtomicReader)reader.Leaves[0].Reader);
			BinaryDocValues bdv = r.GetBinaryDocValues("val");
			BytesRef scratch = new BytesRef();
			AreEqual(2, GetValue(bdv, 0, scratch));
			AreEqual(2, GetValue(bdv, 1, scratch));
			reader.Dispose();
			dir.Dispose();
		}

		[Test]
		public virtual void TestUpdateFewSegments()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			conf.SetMaxBufferedDocs(2);
			// generate few segments
			conf.SetMergePolicy(NoMergePolicy.COMPOUND_FILES);
			// prevent merges for this test
			IndexWriter writer = new IndexWriter(dir, conf);
			int numDocs = 10;
			long[] expectedValues = new long[numDocs];
			for (int i = 0; i < numDocs; i++)
			{
				writer.AddDocument(Doc(i));
				expectedValues[i] = i + 1;
			}
			writer.Commit();
			// update few docs
			for (int i_1 = 0; i_1 < numDocs; i_1++)
			{
				if (Random().NextDouble() < 0.4)
				{
					long value = (i_1 + 1) * 2;
					writer.UpdateBinaryDocValue(new Term("id", "doc-" + i_1), "val", ToBytes(value));
					expectedValues[i_1] = value;
				}
			}
			DirectoryReader reader;
			if (Random().NextBoolean())
			{
				// not NRT
				writer.Dispose();
				reader = DirectoryReader.Open(dir);
			}
			else
			{
				// NRT
				reader = DirectoryReader.Open(writer, true);
				writer.Dispose();
			}
			BytesRef scratch = new BytesRef();
			foreach (AtomicReaderContext context in reader.Leaves)
			{
				AtomicReader r = ((AtomicReader)context.Reader);
				BinaryDocValues bdv = r.GetBinaryDocValues("val");
				IsNotNull(bdv);
				for (int i_2 = 0; i_2 < r.MaxDoc; i_2++)
				{
					long expected = expectedValues[i_2 + context.docBase];
					long actual = GetValue(bdv, i_2, scratch);
					AreEqual(expected, actual);
				}
			}
			reader.Dispose();
			dir.Dispose();
		}

		[Test]
		public virtual void TestReopen()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			IndexWriter writer = new IndexWriter(dir, conf);
			writer.AddDocument(Doc(0));
			writer.AddDocument(Doc(1));
			bool isNRT = Random().NextBoolean();
			DirectoryReader reader1;
			if (isNRT)
			{
				reader1 = DirectoryReader.Open(writer, true);
			}
			else
			{
				writer.Commit();
				reader1 = DirectoryReader.Open(dir);
			}
			// update doc
			writer.UpdateBinaryDocValue(new Term("id", "doc-0"), "val", ToBytes(10));
			// update doc-0's value to 10
			if (!isNRT)
			{
				writer.Commit();
			}
			// reopen reader and 
			//HM:revisit 
			//assert only it sees the update
			DirectoryReader reader2 = DirectoryReader.OpenIfChanged(reader1);
			IsNotNull(reader2);
			IsTrue(reader1 != reader2);
			BytesRef scratch = new BytesRef();
			BinaryDocValues bdv1 = ((AtomicReader)reader1.Leaves[0].Reader).GetBinaryDocValues
				("val");
			BinaryDocValues bdv2 = ((AtomicReader)reader2.Leaves[0].Reader).GetBinaryDocValues
				("val");
			AreEqual(1, GetValue(bdv1, 0, scratch));
			AreEqual(10, GetValue(bdv2, 0, scratch));
			IOUtils.Close(writer, reader1, reader2, dir);
		}

		[Test]
		public virtual void TestUpdatesAndDeletes()
		{
			// create an index with a segment with only deletes, a segment with both
			// deletes and updates and a segment with only updates
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			conf.SetMaxBufferedDocs(10);
			// control segment flushing
			conf.SetMergePolicy(NoMergePolicy.COMPOUND_FILES);
			// prevent merges for this test
			IndexWriter writer = new IndexWriter(dir, conf);
			for (int i = 0; i < 6; i++)
			{
				writer.AddDocument(Doc(i));
				if (i % 2 == 1)
				{
					writer.Commit();
				}
			}
			// create 2-docs segments
			// delete doc-1 and doc-2
			writer.DeleteDocuments(new Term("id", "doc-1"), new Term("id", "doc-2"));
			// 1st and 2nd segments
			// update docs 3 and 5
			writer.UpdateBinaryDocValue(new Term("id", "doc-3"), "val", ToBytes(17L));
			writer.UpdateBinaryDocValue(new Term("id", "doc-5"), "val", ToBytes(17L));
			DirectoryReader reader;
			if (Random().NextBoolean())
			{
				// not NRT
				writer.Dispose();
				reader = DirectoryReader.Open(dir);
			}
			else
			{
				// NRT
				reader = DirectoryReader.Open(writer, true);
				writer.Dispose();
			}
			AtomicReader slow = SlowCompositeReaderWrapper.Wrap(reader);
			IBits liveDocs = slow.LiveDocs;
			bool[] expectedLiveDocs = { true, false, false, true, true, true };
			for (int i_1 = 0; i_1 < expectedLiveDocs.Length; i_1++)
			{
				AreEqual(expectedLiveDocs[i_1], liveDocs[i_1]);
			}
			long[] expectedValues = new long[] { 1, 2, 3, 17, 5, 17 };
			BinaryDocValues bdv = slow.GetBinaryDocValues("val");
			BytesRef scratch = new BytesRef();
			for (int i_2 = 0; i_2 < expectedValues.Length; i_2++)
			{
				AreEqual(expectedValues[i_2], GetValue(bdv, i_2, scratch));
			}
			reader.Dispose();
			dir.Dispose();
		}

		[Test]
		public virtual void TestUpdatesWithDeletes()
		{
			// update and delete different documents in the same commit session
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			conf.SetMaxBufferedDocs(10);
			// control segment flushing
			IndexWriter writer = new IndexWriter(dir, conf);
			writer.AddDocument(Doc(0));
			writer.AddDocument(Doc(1));
			if (Random().NextBoolean())
			{
				writer.Commit();
			}
			writer.DeleteDocuments(new Term("id", "doc-0"));
			writer.UpdateBinaryDocValue(new Term("id", "doc-1"), "val", ToBytes(17L));
			DirectoryReader reader;
			if (Random().NextBoolean())
			{
				// not NRT
				writer.Dispose();
				reader = DirectoryReader.Open(dir);
			}
			else
			{
				// NRT
				reader = DirectoryReader.Open(writer, true);
				writer.Dispose();
			}
			AtomicReader r = ((AtomicReader)reader.Leaves[0].Reader);
			IsFalse(r.LiveDocs[0]);
			AreEqual(17, GetValue(r.GetBinaryDocValues("val"), 1, new 
				BytesRef()));
			reader.Dispose();
			dir.Dispose();
		}

		[Test]
		public virtual void TestUpdateAndDeleteSameDocument()
		{
			// update and delete same document in same commit session
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			conf.SetMaxBufferedDocs(10);
			// control segment flushing
			IndexWriter writer = new IndexWriter(dir, conf);
			writer.AddDocument(Doc(0));
			writer.AddDocument(Doc(1));
			if (Random().NextBoolean())
			{
				writer.Commit();
			}
			writer.DeleteDocuments(new Term("id", "doc-0"));
			writer.UpdateBinaryDocValue(new Term("id", "doc-0"), "val", ToBytes(17L));
			DirectoryReader reader;
			if (Random().NextBoolean())
			{
				// not NRT
				writer.Dispose();
				reader = DirectoryReader.Open(dir);
			}
			else
			{
				// NRT
				reader = DirectoryReader.Open(writer, true);
				writer.Dispose();
			}
			AtomicReader r = ((AtomicReader)reader.Leaves[0].Reader);
			IsFalse(r.LiveDocs[0]);
			AreEqual(1, GetValue(r.GetBinaryDocValues("val"), 0, new BytesRef
				()));
			// deletes are currently applied first
			reader.Dispose();
			dir.Dispose();
		}

		[Test]
		public virtual void TestMultipleDocValuesTypes()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			conf.SetMaxBufferedDocs(10);
			// prevent merges
			IndexWriter writer = new IndexWriter(dir, conf);
			for (int i = 0; i < 4; i++)
			{
				var doc = new Lucene.Net.Documents.Document
				{
				    new StringField("dvUpdateKey", "dv", Field.Store.NO),
				    new NumericDocValuesField("ndv", i),
				    new BinaryDocValuesField("bdv", new BytesRef(i.ToString())),
				    new SortedDocValuesField("sdv", new BytesRef(i.ToString())),
				    new SortedSetDocValuesField("ssdv", new BytesRef(i.ToString())),
				    new SortedSetDocValuesField("ssdv", new BytesRef((i*2).ToString()))
				};
			    writer.AddDocument(doc);
			}
			writer.Commit();
			// update all docs' bdv field
			writer.UpdateBinaryDocValue(new Term("dvUpdateKey", "dv"), "bdv", ToBytes(17L));
			writer.Dispose();
			DirectoryReader reader = DirectoryReader.Open(dir);
			AtomicReader r = ((AtomicReader)reader.Leaves[0].Reader);
			NumericDocValues ndv = r.GetNumericDocValues("ndv");
			BinaryDocValues bdv = r.GetBinaryDocValues("bdv");
			SortedDocValues sdv = r.GetSortedDocValues("sdv");
			SortedSetDocValues ssdv = r.GetSortedSetDocValues("ssdv");
			BytesRef scratch = new BytesRef();
			for (int i_1 = 0; i_1 < r.MaxDoc; i_1++)
			{
				AreEqual(i_1, ndv.Get(i_1));
				AreEqual(17, GetValue(bdv, i_1, scratch));
				sdv.Get(i_1, scratch);
				AreEqual(new BytesRef((i_1).ToString()), scratch);
				ssdv.SetDocument(i_1);
				long ord = ssdv.NextOrd();
				ssdv.LookupOrd(ord, scratch);
				AreEqual(i_1, System.Convert.ToInt32(scratch.Utf8ToString(
					)));
				if (i_1 != 0)
				{
					ord = ssdv.NextOrd();
					ssdv.LookupOrd(ord, scratch);
					AreEqual(i_1 * 2, System.Convert.ToInt32(scratch.Utf8ToString
						()));
				}
				AreEqual(SortedSetDocValues.NO_MORE_ORDS, ssdv.NextOrd());
			}
			reader.Dispose();
			dir.Dispose();
		}

		[Test]
		public virtual void TestMultipleBinaryDocValues()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			conf.SetMaxBufferedDocs(10);
			// prevent merges
			IndexWriter writer = new IndexWriter(dir, conf);
			for (int i = 0; i < 2; i++)
			{
				var doc = new Lucene.Net.Documents.Document
				{
				    new StringField("dvUpdateKey", "dv", Field.Store.NO),
				    new BinaryDocValuesField("bdv1", ToBytes(i)),
				    new BinaryDocValuesField("bdv2", ToBytes(i))
				};
			    writer.AddDocument(doc);
			}
			writer.Commit();
			// update all docs' bdv1 field
			writer.UpdateBinaryDocValue(new Term("dvUpdateKey", "dv"), "bdv1", ToBytes(17L));
			writer.Dispose();
			DirectoryReader reader = DirectoryReader.Open(dir);
			AtomicReader r = ((AtomicReader)reader.Leaves[0].Reader);
			BinaryDocValues bdv1 = r.GetBinaryDocValues("bdv1");
			BinaryDocValues bdv2 = r.GetBinaryDocValues("bdv2");
			BytesRef scratch = new BytesRef();
			for (int i_1 = 0; i_1 < r.MaxDoc; i_1++)
			{
				AreEqual(17, GetValue(bdv1, i_1, scratch));
				AreEqual(i_1, GetValue(bdv2, i_1, scratch));
			}
			reader.Dispose();
			dir.Dispose();
		}

		[Test]
		public virtual void TestDocumentWithNoValue()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			IndexWriter writer = new IndexWriter(dir, conf);
			for (int i = 0; i < 2; i++)
			{
				var doc = new Lucene.Net.Documents.Document {new StringField("dvUpdateKey", "dv", Field.Store.NO)};
			    if (i == 0)
				{
					// index only one document with value
					doc.Add(new BinaryDocValuesField("bdv", ToBytes(5L)));
				}
				writer.AddDocument(doc);
			}
			writer.Commit();
			// update all docs' bdv field
			writer.UpdateBinaryDocValue(new Term("dvUpdateKey", "dv"), "bdv", ToBytes(17L));
			writer.Dispose();
			DirectoryReader reader = DirectoryReader.Open(dir);
			AtomicReader r = ((AtomicReader)reader.Leaves[0].Reader);
			BinaryDocValues bdv = r.GetBinaryDocValues("bdv");
			BytesRef scratch = new BytesRef();
			for (int i_1 = 0; i_1 < r.MaxDoc; i_1++)
			{
				AreEqual(17, GetValue(bdv, i_1, scratch));
			}
			reader.Dispose();
			dir.Dispose();
		}

		[Test]
		public virtual void TestUnsetValue()
		{
			AssumeTrue("codec does not support docsWithField", DefaultCodecSupportsDocsWithField
				());
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			IndexWriter writer = new IndexWriter(dir, conf);
			for (int i = 0; i < 2; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(new StringField("id", "doc" + i, Field.Store.NO));
				doc.Add(new BinaryDocValuesField("bdv", ToBytes(5L)));
				writer.AddDocument(doc);
			}
			writer.Commit();
			// unset the value of 'doc0'
			writer.UpdateBinaryDocValue(new Term("id", "doc0"), "bdv", null);
			writer.Dispose();
			DirectoryReader reader = DirectoryReader.Open(dir);
			AtomicReader r = ((AtomicReader)reader.Leaves[0].Reader);
			BinaryDocValues bdv = r.GetBinaryDocValues("bdv");
			BytesRef scratch = new BytesRef();
			for (int i_1 = 0; i_1 < r.MaxDoc; i_1++)
			{
				if (i_1 == 0)
				{
					bdv.Get(i_1, scratch);
					AreEqual(0, scratch.length);
				}
				else
				{
					AreEqual(5, GetValue(bdv, i_1, scratch));
				}
			}
			IBits docsWithField = r.GetDocsWithField("bdv");
			IsFalse(docsWithField[0]);
			IsTrue(docsWithField[1]);
			reader.Dispose();
			dir.Dispose();
		}

		[Test]
		public virtual void TestUnsetAllValues()
		{
			AssumeTrue("codec does not support docsWithField", DefaultCodecSupportsDocsWithField
				());
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			IndexWriter writer = new IndexWriter(dir, conf);
			for (int i = 0; i < 2; i++)
			{
				var doc = new Lucene.Net.Documents.Document
				{
				    new StringField("id", "doc", Field.Store.NO),
				    new BinaryDocValuesField("bdv", ToBytes(5L))
				};
			    writer.AddDocument(doc);
			}
			writer.Commit();
			// unset the value of 'doc'
			writer.UpdateBinaryDocValue(new Term("id", "doc"), "bdv", null);
			writer.Dispose();
			DirectoryReader reader = DirectoryReader.Open(dir);
			AtomicReader r = ((AtomicReader)reader.Leaves[0].Reader);
			BinaryDocValues bdv = r.GetBinaryDocValues("bdv");
			BytesRef scratch = new BytesRef();
			for (int i_1 = 0; i_1 < r.MaxDoc; i_1++)
			{
				bdv.Get(i_1, scratch);
				AreEqual(0, scratch.length);
			}
			IBits docsWithField = r.GetDocsWithField("bdv");
			IsFalse(docsWithField[0]);
			IsFalse(docsWithField[1]);
			reader.Dispose();
			dir.Dispose();
		}

		[Test]
		public virtual void TestUpdateNonBinaryDocValuesField()
		{
			// we don't support adding new fields or updating existing non-binary-dv
			// fields through binary updates
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
			IndexWriter writer = new IndexWriter(dir, conf);
			var doc = new Lucene.Net.Documents.Document
			{
			    new StringField("key", "doc", Field.Store.NO),
			    new StringField("foo", "bar", Field.Store.NO)
			};
		    writer.AddDocument(doc);
			// flushed document
			writer.Commit();
			writer.AddDocument(doc);
			// in-memory document
			try
			{
				writer.UpdateBinaryDocValue(new Term("key", "doc"), "bdv", ToBytes(17L));
				Fail("should not have allowed creating new fields through update");
			}
			catch (ArgumentException)
			{
			}
			// ok
			try
			{
				writer.UpdateBinaryDocValue(new Term("key", "doc"), "foo", ToBytes(17L));
				Fail("should not have allowed updating an existing field to binary-dv"
					);
			}
			catch (ArgumentException)
			{
			}
			// ok
			writer.Dispose();
			dir.Dispose();
		}

		[Test]
		public virtual void TestDifferentDVFormatPerField()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			conf.SetCodec(new _Lucene46Codec_576());
			IndexWriter writer = new IndexWriter(dir, conf);
			var doc = new Lucene.Net.Documents.Document
			{
			    new StringField("key", "doc", Field.Store.NO),
			    new BinaryDocValuesField("bdv", ToBytes(5L)),
			    new SortedDocValuesField("sorted", new BytesRef("value"))
			};
		    writer.AddDocument(doc);
			// flushed document
			writer.Commit();
			writer.AddDocument(doc);
			// in-memory document
			writer.UpdateBinaryDocValue(new Term("key", "doc"), "bdv", ToBytes(17L));
			writer.Dispose();
			DirectoryReader reader = DirectoryReader.Open(dir);
			AtomicReader r = SlowCompositeReaderWrapper.Wrap(reader);
			BinaryDocValues bdv = r.GetBinaryDocValues("bdv");
			SortedDocValues sdv = r.GetSortedDocValues("sorted");
			BytesRef scratch = new BytesRef();
			for (int i = 0; i < r.MaxDoc; i++)
			{
				AreEqual(17, GetValue(bdv, i, scratch));
				sdv.Get(i, scratch);
				AreEqual(new BytesRef("value"), scratch);
			}
			reader.Dispose();
			dir.Dispose();
		}

		private sealed class _Lucene46Codec_576 : Lucene46Codec
		{
			public _Lucene46Codec_576()
			{
			}

			public override DocValuesFormat GetDocValuesFormatForField(string field)
			{
				return new Lucene45DocValuesFormat();
			}
		}

		[Test]
		public virtual void TestUpdateSameDocMultipleTimes()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			IndexWriter writer = new IndexWriter(dir, conf);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new StringField("key", "doc", Field.Store.NO));
			doc.Add(new BinaryDocValuesField("bdv", ToBytes(5L)));
			writer.AddDocument(doc);
			// flushed document
			writer.Commit();
			writer.AddDocument(doc);
			// in-memory document
			writer.UpdateBinaryDocValue(new Term("key", "doc"), "bdv", ToBytes(17L));
			// update existing field
			writer.UpdateBinaryDocValue(new Term("key", "doc"), "bdv", ToBytes(3L));
			// update existing field 2nd time in this commit
			writer.Dispose();
			DirectoryReader reader = DirectoryReader.Open(dir);
			AtomicReader r = SlowCompositeReaderWrapper.Wrap(reader);
			BinaryDocValues bdv = r.GetBinaryDocValues("bdv");
			BytesRef scratch = new BytesRef();
			for (int i = 0; i < r.MaxDoc; i++)
			{
				AreEqual(3, GetValue(bdv, i, scratch));
			}
			reader.Dispose();
			dir.Dispose();
		}

		[Test]
		public virtual void TestSegmentMerges()
		{
			Directory dir = NewDirectory();
			Random random = Random();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(random));
			IndexWriter writer = new IndexWriter(dir, (IndexWriterConfig) conf.Clone());
			int docid = 0;
			int numRounds = AtLeast(10);
			for (int rnd = 0; rnd < numRounds; rnd++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(new StringField("key", "doc", Field.Store.NO));
				doc.Add(new BinaryDocValuesField("bdv", ToBytes(-1)));
				int numDocs = AtLeast(30);
				for (int i = 0; i < numDocs; i++)
				{
					doc.RemoveField("id");
					doc.Add(new StringField("id", (docid++).ToString(), Field.Store.NO
						));
					writer.AddDocument(doc);
				}
				long value = rnd + 1;
				writer.UpdateBinaryDocValue(new Term("key", "doc"), "bdv", ToBytes(value));
				if (random.NextDouble() < 0.2)
				{
					// randomly delete some docs
                    writer.DeleteDocuments(new Term("id", random.Next(docid).ToString()));
				}
				// randomly commit or reopen-IW (or nothing), before forceMerge
				if (random.NextDouble() < 0.4)
				{
					writer.Commit();
				}
				else
				{
					if (random.NextDouble() < 0.1)
					{
						writer.Dispose();
						writer = new IndexWriter(dir, (IndexWriterConfig) conf.Clone());
					}
				}
				// add another document with the current value, to be sure forceMerge has
				// something to merge (for instance, it could be that CMS finished merging
				// all segments down to 1 before the delete was applied, so when
				// forceMerge is called, the index will be with one segment and deletes
				// and some MPs might now merge it, thereby invalidating test's
				// assumption that the reader has no deletes).
				doc = new Lucene.Net.Documents.Document();
				doc.Add(new StringField("id", (docid++).ToString(), Field.Store.NO
					));
				doc.Add(new StringField("key", "doc", Field.Store.NO));
				doc.Add(new BinaryDocValuesField("bdv", ToBytes(value)));
				writer.AddDocument(doc);
				writer.ForceMerge(1, true);
				DirectoryReader reader;
				if (random.NextBoolean())
				{
					writer.Commit();
					reader = DirectoryReader.Open(dir);
				}
				else
				{
					reader = DirectoryReader.Open(writer, true);
				}
				AreEqual(1, reader.Leaves.Count);
				AtomicReader r = ((AtomicReader)reader.Leaves[0].Reader);
				IsNull(r.LiveDocs, "index should have no deletes after forceMerge");
				BinaryDocValues bdv = r.GetBinaryDocValues("bdv");
				IsNotNull(bdv);
				BytesRef scratch = new BytesRef();
				for (int i_1 = 0; i_1 < r.MaxDoc; i_1++)
				{
					AreEqual(value, GetValue(bdv, i_1, scratch));
				}
				reader.Dispose();
			}
			writer.Dispose();
			dir.Dispose();
		}

		[Test]
		public virtual void TestUpdateDocumentByMultipleTerms()
		{
			// make sure the order of updates is respected, even when multiple terms affect same document
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			IndexWriter writer = new IndexWriter(dir, conf);
			var doc = new Lucene.Net.Documents.Document
			{
			    new StringField("k1", "v1", Field.Store.NO),
			    new StringField("k2", "v2", Field.Store.NO),
			    new BinaryDocValuesField("bdv", ToBytes(5L))
			};
		    writer.AddDocument(doc);
			// flushed document
			writer.Commit();
			writer.AddDocument(doc);
			// in-memory document
			writer.UpdateBinaryDocValue(new Term("k1", "v1"), "bdv", ToBytes(17L));
			writer.UpdateBinaryDocValue(new Term("k2", "v2"), "bdv", ToBytes(3L));
			writer.Dispose();
			DirectoryReader reader = DirectoryReader.Open(dir);
			AtomicReader r = SlowCompositeReaderWrapper.Wrap(reader);
			BinaryDocValues bdv = r.GetBinaryDocValues("bdv");
			BytesRef scratch = new BytesRef();
			for (int i = 0; i < r.MaxDoc; i++)
			{
				AreEqual(3, GetValue(bdv, i, scratch));
			}
			reader.Dispose();
			dir.Dispose();
		}

		[Test]
		public virtual void TestManyReopensAndFields()
		{
			Directory dir = NewDirectory();
			Random random = Random();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(random));
			LogMergePolicy lmp = NewLogMergePolicy();
			lmp.MergeFactor = (3);
			// merge often
			conf.SetMergePolicy(lmp);
			IndexWriter writer = new IndexWriter(dir, conf);
			bool isNRT = random.NextBoolean();
			DirectoryReader reader;
			if (isNRT)
			{
				reader = DirectoryReader.Open(writer, true);
			}
			else
			{
				writer.Commit();
				reader = DirectoryReader.Open(dir);
			}
			int numFields = random.Next(4) + 3;
			// 3-7
			long[] fieldValues = new long[numFields];
			bool[] fieldHasValue = new bool[numFields];
			Arrays.Fill(fieldHasValue, true);
			for (int i = 0; i < fieldValues.Length; i++)
			{
				fieldValues[i] = 1;
			}
			int numRounds = AtLeast(15);
			int docID = 0;
			for (int i_1 = 0; i_1 < numRounds; i_1++)
			{
				int numDocs = AtLeast(5);
				//      System.out.println("[" + Thread.currentThread().getName() + "]: round=" + i + ", numDocs=" + numDocs);
				for (int j = 0; j < numDocs; j++)
				{
					var doc = new Lucene.Net.Documents.Document
					{
					    new StringField("id", "doc-" + docID, Field.Store.NO),
					    new StringField("key", "all", Field.Store.NO)
					};
				    // update key
					// add all fields with their current value
					for (int f = 0; f < fieldValues.Length; f++)
					{
						doc.Add(new BinaryDocValuesField("f" + f, ToBytes(fieldValues[f])));
					}
					writer.AddDocument(doc);
					++docID;
				}
				// if field's value was unset before, unset it from all new added documents too
				for (int field = 0; field < fieldHasValue.Length; field++)
				{
					if (!fieldHasValue[field])
					{
						writer.UpdateBinaryDocValue(new Term("key", "all"), "f" + field, null);
					}
				}
				int fieldIdx = random.Next(fieldValues.Length);
				string updateField = "f" + fieldIdx;
				if (random.NextBoolean())
				{
					//        System.out.println("[" + Thread.currentThread().getName() + "]: unset field '" + updateField + "'");
					fieldHasValue[fieldIdx] = false;
					writer.UpdateBinaryDocValue(new Term("key", "all"), updateField, null);
				}
				else
				{
					fieldHasValue[fieldIdx] = true;
					writer.UpdateBinaryDocValue(new Term("key", "all"), updateField, ToBytes(++fieldValues
						[fieldIdx]));
				}
				//        System.out.println("[" + Thread.currentThread().getName() + "]: updated field '" + updateField + "' to value " + fieldValues[fieldIdx]);
				if (random.NextDouble() < 0.2)
				{
					int deleteDoc = random.Next(docID);
					// might also delete an already deleted document, ok!
					writer.DeleteDocuments(new Term("id", "doc-" + deleteDoc));
				}
				//        System.out.println("[" + Thread.currentThread().getName() + "]: deleted document: doc-" + deleteDoc);
				// verify reader
				if (!isNRT)
				{
					writer.Commit();
				}
				//      System.out.println("[" + Thread.currentThread().getName() + "]: reopen reader: " + reader);
				DirectoryReader newReader = DirectoryReader.OpenIfChanged(reader);
				IsNotNull(newReader);
				reader.Dispose();
				reader = newReader;
				//      System.out.println("[" + Thread.currentThread().getName() + "]: reopened reader: " + reader);
				IsTrue(reader.NumDocs > 0);
				// we delete at most one document per round
				BytesRef scratch = new BytesRef();
				foreach (AtomicReaderContext context in reader.Leaves)
				{
					AtomicReader r = ((AtomicReader)context.Reader);
					//        System.out.println(((SegmentReader) r).getSegmentName());
					IBits liveDocs = r.LiveDocs;
					for (int field_1 = 0; field_1 < fieldValues.Length; field_1++)
					{
						string f = "f" + field_1;
						BinaryDocValues bdv = r.GetBinaryDocValues(f);
						IBits docsWithField = r.GetDocsWithField(f);
						IsNotNull(bdv);
						int maxDoc = r.MaxDoc;
						for (int doc = 0; doc < maxDoc; doc++)
						{
							if (liveDocs == null || liveDocs[doc])
							{
								//              System.out.println("doc=" + (doc + context.docBase) + " f='" + f + "' vslue=" + getValue(bdv, doc, scratch));
								if (fieldHasValue[field_1])
								{
									IsTrue(docsWithField[doc]);
									AreEqual(fieldValues[field_1], GetValue(bdv, doc, scratch), "invalid value for doc=" + doc + ", field=" + f +
										                                   ", reader=" + r);
								}
								else
								{
									IsFalse(docsWithField[doc]);
								}
							}
						}
					}
				}
			}
			//      System.out.println();
			IOUtils.Close(writer, reader, dir);
		}

		[Test]
		public virtual void TestUpdateSegmentWithNoDocValues()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			// prevent merges, otherwise by the time updates are applied
			// (writer.close()), the segments might have merged and that update becomes
			// legit.
			conf.SetMergePolicy(NoMergePolicy.COMPOUND_FILES);
			IndexWriter writer = new IndexWriter(dir, conf);
			// first segment with BDV
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new StringField("id", "doc0", Field.Store.NO));
			doc.Add(new BinaryDocValuesField("bdv", ToBytes(3L)));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new StringField("id", "doc4", Field.Store.NO));
			// document without 'bdv' field
			writer.AddDocument(doc);
			writer.Commit();
			// second segment with no BDV
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new StringField("id", "doc1", Field.Store.NO));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new StringField("id", "doc2", Field.Store.NO));
			// document that isn't updated
			writer.AddDocument(doc);
			writer.Commit();
			// update document in the first segment - should not affect docsWithField of
			// the document without BDV field
			writer.UpdateBinaryDocValue(new Term("id", "doc0"), "bdv", ToBytes(5L));
			// update document in the second segment - field should be added and we should
			// be able to handle the other document correctly (e.g. no NPE)
			writer.UpdateBinaryDocValue(new Term("id", "doc1"), "bdv", ToBytes(5L));
			writer.Dispose();
			DirectoryReader reader = DirectoryReader.Open(dir);
			BytesRef scratch = new BytesRef();
			foreach (AtomicReaderContext context in reader.Leaves)
			{
				AtomicReader r = ((AtomicReader)context.Reader);
				BinaryDocValues bdv = r.GetBinaryDocValues("bdv");
				IBits docsWithField = r.GetDocsWithField("bdv");
				IsNotNull(docsWithField);
				IsTrue(docsWithField[0]);
				AreEqual(5L, GetValue(bdv, 0, scratch));
				IsFalse(docsWithField[1]);
				bdv.Get(1, scratch);
				AreEqual(0, scratch.length);
			}
			reader.Dispose();
			dir.Dispose();
		}

		[Test]
		public virtual void TestUpdateSegmentWithPostingButNoDocValues()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			// prevent merges, otherwise by the time updates are applied
			// (writer.close()), the segments might have merged and that update becomes
			// legit.
			conf.SetMergePolicy(NoMergePolicy.COMPOUND_FILES);
			IndexWriter writer = new IndexWriter(dir, conf);
			// first segment with BDV
			var doc = new Lucene.Net.Documents.Document
			{
			    new StringField("id", "doc0", Field.Store.NO),
			    new StringField("bdv", "mock-value", Field.Store.NO),
			    new BinaryDocValuesField("bdv", ToBytes(5L))
			};
		    writer.AddDocument(doc);
			writer.Commit();
			// second segment with no BDV
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new StringField("id", "doc1", Field.Store.NO));
			doc.Add(new StringField("bdv", "mock-value", Field.Store.NO));
			writer.AddDocument(doc);
			writer.Commit();
			// update document in the second segment
			writer.UpdateBinaryDocValue(new Term("id", "doc1"), "bdv", ToBytes(5L));
			writer.Dispose();
			DirectoryReader reader = DirectoryReader.Open(dir);
			BytesRef scratch = new BytesRef();
			foreach (AtomicReaderContext context in reader.Leaves)
			{
				AtomicReader r = ((AtomicReader)context.Reader);
				BinaryDocValues bdv = r.GetBinaryDocValues("bdv");
				for (int i = 0; i < r.MaxDoc; i++)
				{
					AreEqual(5L, GetValue(bdv, i, scratch));
				}
			}
			reader.Dispose();
			dir.Dispose();
		}

		[Test]
		public virtual void TestUpdateBinaryDVFieldWithSameNameAsPostingField()
		{
			// this used to fail because FieldInfos.Builder neglected to update
			// globalFieldMaps.docValueTypes map
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			var writer = new IndexWriter(dir, conf);
			var doc = new Lucene.Net.Documents.Document
			{
			    new StringField("f", "mock-value", Field.Store.NO),
			    new BinaryDocValuesField("f", ToBytes(5L))
			};
		    writer.AddDocument(doc);
			writer.Commit();
			writer.UpdateBinaryDocValue(new Term("f", "mock-value"), "f", ToBytes(17L));
			writer.Dispose();
			DirectoryReader r = DirectoryReader.Open(dir);
			BinaryDocValues bdv = ((AtomicReader)r.Leaves[0].Reader).GetBinaryDocValues("f"
				);
			AreEqual(17, GetValue(bdv, 0, new BytesRef()));
			r.Dispose();
			dir.Dispose();
		}

		[Test]
		public virtual void TestUpdateOldSegments()
		{
			Codec[] oldCodecs = new Codec[] { new Lucene40RWCodec(), new Lucene41RWCodec(), new 
				Lucene42RWCodec(), new Lucene45RWCodec() };
			Directory dir = NewDirectory();
			bool oldValue = OLD_FORMAT_IMPERSONATION_IS_ACTIVE;
			// create a segment with an old Codec
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			conf.SetCodec(oldCodecs[Random().Next(oldCodecs.Length)]);
			OLD_FORMAT_IMPERSONATION_IS_ACTIVE = true;
			IndexWriter writer = new IndexWriter(dir, conf);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new StringField("id", "doc", Field.Store.NO));
			doc.Add(new BinaryDocValuesField("f", ToBytes(5L)));
			writer.AddDocument(doc);
			writer.Dispose();
			conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
			writer = new IndexWriter(dir, conf);
			writer.UpdateBinaryDocValue(new Term("id", "doc"), "f", ToBytes(4L));
			OLD_FORMAT_IMPERSONATION_IS_ACTIVE = false;
			try
			{
				writer.Dispose();
				Fail("should not have succeeded to update a segment written with an old Codec"
					);
			}
			catch (NotSupportedException)
			{
				writer.Rollback();
			}
			finally
			{
				OLD_FORMAT_IMPERSONATION_IS_ACTIVE = oldValue;
			}
			dir.Dispose();
		}

		[Test]
		public virtual void TestStressMultiThreading()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			IndexWriter writer = new IndexWriter(dir, conf);
			// create index
			int numThreads = TestUtil.NextInt(Random(), 3, 6);
			int numDocs = AtLeast(2000);
			for (int i = 0; i < numDocs; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(new StringField("id", "doc" + i, Field.Store.NO));
				double group = Random().NextDouble();
				string g;
				if (group < 0.1)
				{
					g = "g0";
				}
				else
				{
					if (group < 0.5)
					{
						g = "g1";
					}
					else
					{
						if (group < 0.8)
						{
							g = "g2";
						}
						else
						{
							g = "g3";
						}
					}
				}
				doc.Add(new StringField("updKey", g, Field.Store.NO));
				for (int j = 0; j < numThreads; j++)
				{
					long value = Random().Next();
					doc.Add(new BinaryDocValuesField("f" + j, ToBytes(value)));
					doc.Add(new BinaryDocValuesField("cf" + j, ToBytes(value * 2)));
				}
				// control, always updated to f * 2
				writer.AddDocument(doc);
			}
			CountdownEvent done = new CountdownEvent(numThreads);
			AtomicInteger numUpdates = new AtomicInteger(AtLeast(100));
			// same thread updates a field as well as reopens
			Thread[] threads = new Thread[numThreads];
			for (int i = 0; i < threads.Length; i++)
			{
				string f = "f" + i;
				string cf = "cf" + i;
				threads[i] = new Thread(numUpdates, writer, f, cf, numDocs, done, "UpdateThread-"
					 + i);
			}
			//              System.out.println("[" + Thread.currentThread().getName() + "] numUpdates=" + numUpdates + " updateTerm=" + t);
			// sometimes unset a value
			// delete a random document
			//                System.out.println("[" + Thread.currentThread().getName() + "] deleteDoc=doc" + doc);
			// commit every 20 updates on average
			//                  System.out.println("[" + Thread.currentThread().getName() + "] commit");
			// reopen NRT reader (apply updates), on average once every 10 updates
			//                  System.out.println("[" + Thread.currentThread().getName() + "] open NRT");
			//                  System.out.println("[" + Thread.currentThread().getName() + "] reopen NRT");
			//            System.out.println("[" + Thread.currentThread().getName() + "] DONE");
			// suppress this exception only if there was another exception
			foreach (Thread t in threads)
			{
				t.Start();
			}
			done.Await();
			writer.Dispose();
			DirectoryReader reader = DirectoryReader.Open(dir);
			BytesRef scratch = new BytesRef();
			foreach (AtomicReaderContext context in reader.Leaves)
			{
				AtomicReader r = ((AtomicReader)context.Reader);
				for (int i_2 = 0; i_2 < numThreads; i_2++)
				{
					BinaryDocValues bdv = r.GetBinaryDocValues("f" + i_2);
					BinaryDocValues control = r.GetBinaryDocValues("cf" + i_2);
					Bits docsWithBdv = r.GetDocsWithField("f" + i_2);
					Bits docsWithControl = r.GetDocsWithField("cf" + i_2);
					Bits liveDocs = r.LiveDocs;
					for (int j = 0; j < r.MaxDoc; j++)
					{
						if (liveDocs == null || liveDocs.Get(j))
						{
							AreEqual(docsWithBdv.Get(j), docsWithControl.Get(j));
							if (docsWithBdv.Get(j))
							{
								AreEqual(GetValue(control, j, scratch), GetValue(bdv, j, scratch
									) * 2);
							}
						}
					}
				}
			}
			reader.Dispose();
			dir.Dispose();
		}

		private sealed class _Thread_1034 : Sharpen.Thread
		{
			public _Thread_1034(AtomicInteger numUpdates, IndexWriter writer, string f, string
				 cf, int numDocs, CountDownLatch done, string baseArg1) : base(baseArg1)
			{
				this.numUpdates = numUpdates;
				this.writer = writer;
				this.f = f;
				this.cf = cf;
				this.numDocs = numDocs;
				this.done = done;
			}

			public override void Run()
			{
				DirectoryReader reader = null;
				bool success = false;
				try
				{
					Random random = LuceneTestCase.Random();
					while (numUpdates.GetAndDecrement() > 0)
					{
						double group = random.NextDouble();
						Term t;
						if (group < 0.1)
						{
							t = new Term("updKey", "g0");
						}
						else
						{
							if (group < 0.5)
							{
								t = new Term("updKey", "g1");
							}
							else
							{
								if (group < 0.8)
								{
									t = new Term("updKey", "g2");
								}
								else
								{
									t = new Term("updKey", "g3");
								}
							}
						}
						if (random.NextBoolean())
						{
							writer.UpdateBinaryDocValue(t, f, null);
							writer.UpdateBinaryDocValue(t, cf, null);
						}
						else
						{
							long updValue = random.Next();
							writer.UpdateBinaryDocValue(t, f, TestBinaryDocValuesUpdates.ToBytes(updValue));
							writer.UpdateBinaryDocValue(t, cf, TestBinaryDocValuesUpdates.ToBytes(updValue * 
								2));
						}
						if (random.NextDouble() < 0.2)
						{
							int doc = random.Next(numDocs);
							writer.DeleteDocuments(new Term("id", "doc" + doc));
						}
						if (random.NextDouble() < 0.05)
						{
							writer.Commit();
						}
						if (random.NextDouble() < 0.1)
						{
							if (reader == null)
							{
								reader = DirectoryReader.Open(writer, true);
							}
							else
							{
								DirectoryReader r2 = DirectoryReader.OpenIfChanged(reader, writer, true);
								if (r2 != null)
								{
									reader.Dispose();
									reader = r2;
								}
							}
						}
					}
					success = true;
				}
				catch (IOException e)
				{
					throw new RuntimeException(e);
				}
				finally
				{
					if (reader != null)
					{
						try
						{
							reader.Dispose();
						}
						catch (IOException e)
						{
							if (success)
							{
								throw new RuntimeException(e);
							}
						}
					}
					done.CountDown();
				}
			}

			private readonly AtomicInteger numUpdates;

			private readonly IndexWriter writer;

			private readonly string f;

			private readonly string cf;

			private readonly int numDocs;

			private readonly CountDownLatch done;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestUpdateDifferentDocsInDifferentGens()
		{
			// update same document multiple times across generations
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			conf.SetMaxBufferedDocs(4);
			IndexWriter writer = new IndexWriter(dir, conf);
			int numDocs = AtLeast(10);
			for (int i = 0; i < numDocs; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(new StringField("id", "doc" + i, Field.Store.NO));
				long value = Random().Next();
				doc.Add(new BinaryDocValuesField("f", ToBytes(value)));
				doc.Add(new BinaryDocValuesField("cf", ToBytes(value * 2)));
				writer.AddDocument(doc);
			}
			int numGens = AtLeast(5);
			BytesRef scratch = new BytesRef();
			for (int i_1 = 0; i_1 < numGens; i_1++)
			{
				int doc = Random().Next(numDocs);
				Term t = new Term("id", "doc" + doc);
				long value = Random().NextLong();
				writer.UpdateBinaryDocValue(t, "f", ToBytes(value));
				writer.UpdateBinaryDocValue(t, "cf", ToBytes(value * 2));
				DirectoryReader reader = DirectoryReader.Open(writer, true);
				foreach (AtomicReaderContext context in reader.Leaves)
				{
					AtomicReader r = ((AtomicReader)context.Reader);
					BinaryDocValues fbdv = r.GetBinaryDocValues("f");
					BinaryDocValues cfbdv = r.GetBinaryDocValues("cf");
					for (int j = 0; j < r.MaxDoc; j++)
					{
						AreEqual(GetValue(cfbdv, j, scratch), GetValue(fbdv, j, scratch
							) * 2);
					}
				}
				reader.Dispose();
			}
			writer.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestChangeCodec()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			conf.SetMergePolicy(NoMergePolicy.COMPOUND_FILES);
			// disable merges to simplify test assertions.
			conf.SetCodec(new _Lucene46Codec_1176());
			IndexWriter writer = new IndexWriter(dir, conf.Clone());
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new StringField("id", "d0", Field.Store.NO));
			doc.Add(new BinaryDocValuesField("f1", ToBytes(5L)));
			doc.Add(new BinaryDocValuesField("f2", ToBytes(13L)));
			writer.AddDocument(doc);
			writer.Dispose();
			// change format
			conf.SetCodec(new _Lucene46Codec_1191());
			writer = new IndexWriter(dir, conf.Clone());
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new StringField("id", "d1", Field.Store.NO));
			doc.Add(new BinaryDocValuesField("f1", ToBytes(17L)));
			doc.Add(new BinaryDocValuesField("f2", ToBytes(2L)));
			writer.AddDocument(doc);
			writer.UpdateBinaryDocValue(new Term("id", "d0"), "f1", ToBytes(12L));
			writer.Dispose();
			DirectoryReader reader = DirectoryReader.Open(dir);
			AtomicReader r = SlowCompositeReaderWrapper.Wrap(reader);
			BinaryDocValues f1 = r.GetBinaryDocValues("f1");
			BinaryDocValues f2 = r.GetBinaryDocValues("f2");
			BytesRef scratch = new BytesRef();
			AreEqual(12L, GetValue(f1, 0, scratch));
			AreEqual(13L, GetValue(f2, 0, scratch));
			AreEqual(17L, GetValue(f1, 1, scratch));
			AreEqual(2L, GetValue(f2, 1, scratch));
			reader.Dispose();
			dir.Dispose();
		}

		private sealed class _Lucene46Codec_1176 : Lucene46Codec
		{
			public _Lucene46Codec_1176()
			{
			}

			public override DocValuesFormat GetDocValuesFormatForField(string field)
			{
				return new Lucene45DocValuesFormat();
			}
		}

		private sealed class _Lucene46Codec_1191 : Lucene46Codec
		{
			public _Lucene46Codec_1191()
			{
			}

			public override DocValuesFormat GetDocValuesFormatForField(string field)
			{
				return new AssertingDocValuesFormat();
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestAddIndexes()
		{
			Directory dir1 = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			IndexWriter writer = new IndexWriter(dir1, conf);
			int numDocs = AtLeast(50);
			int numTerms = TestUtil.NextInt(Random(), 1, numDocs / 5);
			ICollection<string> randomTerms = new HashSet<string>();
			while (randomTerms.Count < numTerms)
			{
				randomTerms.AddItem(TestUtil.RandomSimpleString(Random()));
			}
			// create first index
			for (int i = 0; i < numDocs; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(new StringField("id", RandomPicks.RandomFrom(Random(), randomTerms), Field.Store
					.NO));
				doc.Add(new BinaryDocValuesField("bdv", ToBytes(4L)));
				doc.Add(new BinaryDocValuesField("control", ToBytes(8L)));
				writer.AddDocument(doc);
			}
			if (Random().NextBoolean())
			{
				writer.Commit();
			}
			// update some docs to a random value
			long value = Random().Next();
			Term term = new Term("id", RandomPicks.RandomFrom(Random(), randomTerms));
			writer.UpdateBinaryDocValue(term, "bdv", ToBytes(value));
			writer.UpdateBinaryDocValue(term, "control", ToBytes(value * 2));
			writer.Dispose();
			Directory dir2 = NewDirectory();
			conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
			writer = new IndexWriter(dir2, conf);
			if (Random().NextBoolean())
			{
				writer.AddIndexes(dir1);
			}
			else
			{
				DirectoryReader reader = DirectoryReader.Open(dir1);
				writer.AddIndexes(reader);
				reader.Dispose();
			}
			writer.Dispose();
			DirectoryReader reader_1 = DirectoryReader.Open(dir2);
			BytesRef scratch = new BytesRef();
			foreach (AtomicReaderContext context in reader_1.Leaves)
			{
				AtomicReader r = ((AtomicReader)context.Reader);
				BinaryDocValues bdv = r.GetBinaryDocValues("bdv");
				BinaryDocValues control = r.GetBinaryDocValues("control");
				for (int i_1 = 0; i_1 < r.MaxDoc; i_1++)
				{
					AreEqual(GetValue(bdv, i_1, scratch) * 2, GetValue(control
						, i_1, scratch));
				}
			}
			reader_1.Dispose();
			IOUtils.Close(dir1, dir2);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestDeleteUnusedUpdatesFiles()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			IndexWriter writer = new IndexWriter(dir, conf);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new StringField("id", "d0", Field.Store.NO));
			doc.Add(new BinaryDocValuesField("f", ToBytes(1L)));
			writer.AddDocument(doc);
			// create first gen of update files
			writer.UpdateBinaryDocValue(new Term("id", "d0"), "f", ToBytes(2L));
			writer.Commit();
			int numFiles = dir.ListAll().Length;
			DirectoryReader r = DirectoryReader.Open(dir);
			BytesRef scratch = new BytesRef();
			AreEqual(2L, GetValue(((AtomicReader)r.Leaves[0].Reader(
				)).GetBinaryDocValues("f"), 0, scratch));
			r.Dispose();
			// create second gen of update files, first gen should be deleted
			writer.UpdateBinaryDocValue(new Term("id", "d0"), "f", ToBytes(5L));
			writer.Commit();
			AreEqual(numFiles, dir.ListAll().Length);
			r = DirectoryReader.Open(dir);
			AreEqual(5L, GetValue(((AtomicReader)r.Leaves[0].Reader(
				)).GetBinaryDocValues("f"), 0, scratch));
			r.Dispose();
			writer.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestTonsOfUpdates()
		{
			// LUCENE-5248: make sure that when there are many updates, we don't use too much RAM
			Directory dir = NewDirectory();
			Random random = Random();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(random));
			conf.SetRAMBufferSizeMB(IndexWriterConfig.DEFAULT_RAM_BUFFER_SIZE_MB);
			conf.SetMaxBufferedDocs(IndexWriterConfig.DISABLE_AUTO_FLUSH);
			// don't flush by doc
			IndexWriter writer = new IndexWriter(dir, conf);
			// test data: lots of documents (few 10Ks) and lots of update terms (few hundreds)
			int numDocs = AtLeast(20000);
			int numBinaryFields = AtLeast(5);
			int numTerms = TestUtil.NextInt(random, 10, 100);
			// terms should affect many docs
			ICollection<string> updateTerms = new HashSet<string>();
			while (updateTerms.Count < numTerms)
			{
				updateTerms.AddItem(TestUtil.RandomSimpleString(random));
			}
			//    System.out.println("numDocs=" + numDocs + " numBinaryFields=" + numBinaryFields + " numTerms=" + numTerms);
			// build a large index with many BDV fields and update terms
			for (int i = 0; i < numDocs; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				int numUpdateTerms = TestUtil.NextInt(random, 1, numTerms / 10);
				for (int j = 0; j < numUpdateTerms; j++)
				{
					doc.Add(new StringField("upd", RandomPicks.RandomFrom(random, updateTerms), Field.Store
						.NO));
				}
				for (int j_1 = 0; j_1 < numBinaryFields; j_1++)
				{
					long val = random.Next();
					doc.Add(new BinaryDocValuesField("f" + j_1, ToBytes(val)));
					doc.Add(new BinaryDocValuesField("cf" + j_1, ToBytes(val * 2)));
				}
				writer.AddDocument(doc);
			}
			writer.Commit();
			// commit so there's something to apply to
			// set to flush every 2048 bytes (approximately every 12 updates), so we get
			// many flushes during binary updates
			writer.Config.SetRAMBufferSizeMB(2048.0 / 1024 / 1024);
			int numUpdates = AtLeast(100);
			//    System.out.println("numUpdates=" + numUpdates);
			for (int i_1 = 0; i_1 < numUpdates; i_1++)
			{
				int field = random.Next(numBinaryFields);
				Term updateTerm = new Term("upd", RandomPicks.RandomFrom(random, updateTerms));
				long value = random.Next();
				writer.UpdateBinaryDocValue(updateTerm, "f" + field, ToBytes(value));
				writer.UpdateBinaryDocValue(updateTerm, "cf" + field, ToBytes(value * 2));
			}
			writer.Dispose();
			DirectoryReader reader = DirectoryReader.Open(dir);
			BytesRef scratch = new BytesRef();
			foreach (AtomicReaderContext context in reader.Leaves)
			{
				for (int i_2 = 0; i_2 < numBinaryFields; i_2++)
				{
					AtomicReader r = ((AtomicReader)context.Reader);
					BinaryDocValues f = r.GetBinaryDocValues("f" + i_2);
					BinaryDocValues cf = r.GetBinaryDocValues("cf" + i_2);
					for (int j = 0; j < r.MaxDoc; j++)
					{
						AreEqual("reader=" + r + ", field=f" + i_2 + ", doc=" + j, 
							GetValue(cf, j, scratch), GetValue(f, j, scratch) * 2);
					}
				}
			}
			reader.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestUpdatesOrder()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			IndexWriter writer = new IndexWriter(dir, conf);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new StringField("upd", "t1", Field.Store.NO));
			doc.Add(new StringField("upd", "t2", Field.Store.NO));
			doc.Add(new BinaryDocValuesField("f1", ToBytes(1L)));
			doc.Add(new BinaryDocValuesField("f2", ToBytes(1L)));
			writer.AddDocument(doc);
			writer.UpdateBinaryDocValue(new Term("upd", "t1"), "f1", ToBytes(2L));
			// update f1 to 2
			writer.UpdateBinaryDocValue(new Term("upd", "t1"), "f2", ToBytes(2L));
			// update f2 to 2
			writer.UpdateBinaryDocValue(new Term("upd", "t2"), "f1", ToBytes(3L));
			// update f1 to 3
			writer.UpdateBinaryDocValue(new Term("upd", "t2"), "f2", ToBytes(3L));
			// update f2 to 3
			writer.UpdateBinaryDocValue(new Term("upd", "t1"), "f1", ToBytes(4L));
			// update f1 to 4 (but not f2)
			writer.Dispose();
			DirectoryReader reader = DirectoryReader.Open(dir);
			BytesRef scratch = new BytesRef();
			AreEqual(4, GetValue(((AtomicReader)reader.Leaves[0].Reader
				()).GetBinaryDocValues("f1"), 0, scratch));
			AreEqual(3, GetValue(((AtomicReader)reader.Leaves[0].Reader
				()).GetBinaryDocValues("f2"), 0, scratch));
			reader.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestUpdateAllDeletedSegment()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			IndexWriter writer = new IndexWriter(dir, conf);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new StringField("id", "doc", Field.Store.NO));
			doc.Add(new BinaryDocValuesField("f1", ToBytes(1L)));
			writer.AddDocument(doc);
			writer.AddDocument(doc);
			writer.Commit();
			writer.DeleteDocuments(new Term("id", "doc"));
			// delete all docs in the first segment
			writer.AddDocument(doc);
			writer.UpdateBinaryDocValue(new Term("id", "doc"), "f1", ToBytes(2L));
			writer.Dispose();
			DirectoryReader reader = DirectoryReader.Open(dir);
			AreEqual(1, reader.Leaves.Count);
			AreEqual(2L, GetValue(((AtomicReader)reader.Leaves[0].Reader
				()).GetBinaryDocValues("f1"), 0, new BytesRef()));
			reader.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestUpdateTwoNonexistingTerms()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			IndexWriter writer = new IndexWriter(dir, conf);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new StringField("id", "doc", Field.Store.NO));
			doc.Add(new BinaryDocValuesField("f1", ToBytes(1L)));
			writer.AddDocument(doc);
			// update w/ multiple nonexisting terms in same field
			writer.UpdateBinaryDocValue(new Term("c", "foo"), "f1", ToBytes(2L));
			writer.UpdateBinaryDocValue(new Term("c", "bar"), "f1", ToBytes(2L));
			writer.Dispose();
			DirectoryReader reader = DirectoryReader.Open(dir);
			AreEqual(1, reader.Leaves.Count);
			AreEqual(1L, GetValue(((AtomicReader)reader.Leaves[0].Reader
				()).GetBinaryDocValues("f1"), 0, new BytesRef()));
			reader.Dispose();
			dir.Dispose();
		}
	}
}
