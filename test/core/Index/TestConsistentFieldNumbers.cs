using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Store;
using Lucene.Net.TestFramework;
using Lucene.Net.TestFramework.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Index
{
    [TestFixture]
	public class TestConsistentFieldNumbers : LuceneTestCase
	{
		
		[Test]
		public virtual void TestSameFieldNumbersAcrossSegments()
		{
			for (int i = 0; i < 2; i++)
			{
				Directory dir = NewDirectory();
				IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
					, new MockAnalyzer(Random())).SetMergePolicy(NoMergePolicy.COMPOUND_FILES));
				Lucene.Net.Documents.Document d1 = new Lucene.Net.Documents.Document(
					);
				d1.Add(new StringField("f1", "first field", Field.Store.YES));
				d1.Add(new StringField("f2", "second field", Field.Store.YES));
				writer.AddDocument(d1);
				if (i == 1)
				{
					writer.Dispose();
					writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
						(Random())).SetMergePolicy(NoMergePolicy.COMPOUND_FILES));
				}
				else
				{
					writer.Commit();
				}
				Lucene.Net.Documents.Document d2 = new Lucene.Net.Documents.Document(
					);
				FieldType customType2 = new FieldType(TextField.TYPE_STORED);
				customType2.StoreTermVectors = true;
				d2.Add(new TextField("f2", "second field", Field.Store.NO));
				d2.Add(new Field("f1", "first field", customType2));
				d2.Add(new TextField("f3", "third field", Field.Store.NO));
				d2.Add(new TextField("f4", "fourth field", Field.Store.NO));
				writer.AddDocument(d2);
				writer.Dispose();
				SegmentInfos sis = new SegmentInfos();
				sis.Read(dir);
				AreEqual(2, sis.Count);
				FieldInfos fis1 = SegmentReader.ReadFieldInfos(sis.Info(0));
				FieldInfos fis2 = SegmentReader.ReadFieldInfos(sis.Info(1));
				AreEqual("f1", fis1.FieldInfo(0).name);
				AreEqual("f2", fis1.FieldInfo(1).name);
				AreEqual("f1", fis2.FieldInfo(0).name);
				AreEqual("f2", fis2.FieldInfo(1).name);
				AreEqual("f3", fis2.FieldInfo(2).name);
				AreEqual("f4", fis2.FieldInfo(3).name);
				writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
					(Random())));
				writer.ForceMerge(1);
				writer.Dispose();
				sis = new SegmentInfos();
				sis.Read(dir);
				AreEqual(1, sis.Count);
				FieldInfos fis3 = SegmentReader.ReadFieldInfos(sis.Info(0));
				AreEqual("f1", fis3.FieldInfo(0).name);
				AreEqual("f2", fis3.FieldInfo(1).name);
				AreEqual("f3", fis3.FieldInfo(2).name);
				AreEqual("f4", fis3.FieldInfo(3).name);
				dir.Dispose();
			}
		}

		
		[Test]
		public virtual void TestAddIndexes()
		{
			Directory dir1 = NewDirectory();
			Directory dir2 = NewDirectory();
			IndexWriter writer = new IndexWriter(dir1, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetMergePolicy(NoMergePolicy.COMPOUND_FILES));
			var d1 = new Lucene.Net.Documents.Document
			{
			    new TextField("f1", "first field", Field.Store.YES),
			    new TextField("f2", "second field", Field.Store.YES)
			};
		    writer.AddDocument(d1);
			writer.Dispose();
			writer = new IndexWriter(dir2, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetMergePolicy(NoMergePolicy.COMPOUND_FILES));
			Lucene.Net.Documents.Document d2 = new Lucene.Net.Documents.Document();
			FieldType customType2 = new FieldType(TextField.TYPE_STORED);
			customType2.StoreTermVectors = true;
			d2.Add(new TextField("f2", "second field", Field.Store.YES));
			d2.Add(new Field("f1", "first field", customType2));
			d2.Add(new TextField("f3", "third field", Field.Store.YES));
			d2.Add(new TextField("f4", "fourth field", Field.Store.YES));
			writer.AddDocument(d2);
			writer.Dispose();
			writer = new IndexWriter(dir1, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetMergePolicy(NoMergePolicy.COMPOUND_FILES));
			writer.AddIndexes(dir2);
			writer.Dispose();
			SegmentInfos sis = new SegmentInfos();
			sis.Read(dir1);
			AreEqual(2, sis.Count);
			FieldInfos fis1 = SegmentReader.ReadFieldInfos(sis.Info(0));
			FieldInfos fis2 = SegmentReader.ReadFieldInfos(sis.Info(1));
			AreEqual("f1", fis1.FieldInfo(0).name);
			AreEqual("f2", fis1.FieldInfo(1).name);
			// make sure the ordering of the "external" segment is preserved
			AreEqual("f2", fis2.FieldInfo(0).name);
			AreEqual("f1", fis2.FieldInfo(1).name);
			AreEqual("f3", fis2.FieldInfo(2).name);
			AreEqual("f4", fis2.FieldInfo(3).name);
			dir1.Dispose();
			dir2.Dispose();
		}

		[Test]
		public virtual void TestFieldNumberGaps()
		{
			int numIters = AtLeast(13);
			for (int i = 0; i < numIters; i++)
			{
				Directory dir = NewDirectory();
				{
					IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
						, new MockAnalyzer(Random())).SetMergePolicy(NoMergePolicy.NO_COMPOUND_FILES));
					Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
					d.Add(new TextField("f1", "d1 first field", Field.Store.YES));
					d.Add(new TextField("f2", "d1 second field", Field.Store.YES));
					writer.AddDocument(d);
					writer.Dispose();
					SegmentInfos sis = new SegmentInfos();
					sis.Read(dir);
					AreEqual(1, sis.Count);
					FieldInfos fis1 = SegmentReader.ReadFieldInfos(sis.Info(0));
					AreEqual("f1", fis1.FieldInfo(0).name);
					AreEqual("f2", fis1.FieldInfo(1).name);
				}
				{
					IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
						, new MockAnalyzer(Random())).SetMergePolicy(Random().NextBoolean() ? NoMergePolicy
						.NO_COMPOUND_FILES : NoMergePolicy.COMPOUND_FILES));
					Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
					d.Add(new TextField("f1", "d2 first field", Field.Store.YES));
					d.Add(new StoredField("f3", new sbyte[] { 1, 2, 3 }));
					writer.AddDocument(d);
					writer.Dispose();
					SegmentInfos sis = new SegmentInfos();
					sis.Read(dir);
					AreEqual(2, sis.Count);
					FieldInfos fis1 = SegmentReader.ReadFieldInfos(sis.Info(0));
					FieldInfos fis2 = SegmentReader.ReadFieldInfos(sis.Info(1));
					AreEqual("f1", fis1.FieldInfo(0).name);
					AreEqual("f2", fis1.FieldInfo(1).name);
					AreEqual("f1", fis2.FieldInfo(0).name);
					IsNull(fis2.FieldInfo(1));
					AreEqual("f3", fis2.FieldInfo(2).name);
				}
				{
					IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
						, new MockAnalyzer(Random())).SetMergePolicy(Random().NextBoolean() ? NoMergePolicy
						.NO_COMPOUND_FILES : NoMergePolicy.COMPOUND_FILES));
					Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
					d.Add(new TextField("f1", "d3 first field", Field.Store.YES));
					d.Add(new TextField("f2", "d3 second field", Field.Store.YES));
					d.Add(new StoredField("f3", new sbyte[] { 1, 2, 3, 4, 5 }));
					writer.AddDocument(d);
					writer.Dispose();
					SegmentInfos sis = new SegmentInfos();
					sis.Read(dir);
					AreEqual(3, sis.Count);
					FieldInfos fis1 = SegmentReader.ReadFieldInfos(sis.Info(0));
					FieldInfos fis2 = SegmentReader.ReadFieldInfos(sis.Info(1));
					FieldInfos fis3 = SegmentReader.ReadFieldInfos(sis.Info(2));
					AreEqual("f1", fis1.FieldInfo(0).name);
					AreEqual("f2", fis1.FieldInfo(1).name);
					AreEqual("f1", fis2.FieldInfo(0).name);
					IsNull(fis2.FieldInfo(1));
					AreEqual("f3", fis2.FieldInfo(2).name);
					AreEqual("f1", fis3.FieldInfo(0).name);
					AreEqual("f2", fis3.FieldInfo(1).name);
					AreEqual("f3", fis3.FieldInfo(2).name);
				}
				{
					IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
						, new MockAnalyzer(Random())).SetMergePolicy(Random().NextBoolean() ? NoMergePolicy
						.NO_COMPOUND_FILES : NoMergePolicy.COMPOUND_FILES));
					writer.DeleteDocuments(new Term("f1", "d1"));
					// nuke the first segment entirely so that the segment with gaps is
					// loaded first!
					writer.ForceMergeDeletes();
					writer.Dispose();
				}
				IndexWriter writer_1 = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
					, new MockAnalyzer(Random())).SetMergePolicy(new LogByteSizeMergePolicy()).SetInfoStream
					(new FailOnNonBulkMergesInfoStream()));
				writer_1.ForceMerge(1);
				writer_1.Dispose();
				SegmentInfos sis_1 = new SegmentInfos();
				sis_1.Read(dir);
				AreEqual(1, sis_1.Count);
				FieldInfos fis1_1 = SegmentReader.ReadFieldInfos(sis_1.Info(0));
				AreEqual("f1", fis1_1.FieldInfo(0).name);
				AreEqual("f2", fis1_1.FieldInfo(1).name);
				AreEqual("f3", fis1_1.FieldInfo(2).name);
				dir.Dispose();
			}
		}

		
		[Test]
		public virtual void TestManyFields()
		{
			int NUM_DOCS = AtLeast(200);
			int MAX_FIELDS = AtLeast(50);
			int[][] docs = new int[NUM_DOCS][];
			//HM:revisit 2nd index removed
			for (int i = 0; i < docs.Length; i++)
			{
				for (int j = 0; j < docs[i].Length; j++)
				{
					docs[i][j] = Random().Next(MAX_FIELDS);
				}
			}
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			for (int i_1 = 0; i_1 < NUM_DOCS; i_1++)
			{
				Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
				for (int j = 0; j < docs[i_1].Length; j++)
				{
					d.Add(GetField(docs[i_1][j]));
				}
				writer.AddDocument(d);
			}
			writer.ForceMerge(1);
			writer.Dispose();
			SegmentInfos sis = new SegmentInfos();
			sis.Read(dir);
			foreach (SegmentCommitInfo si in sis)
			{
				FieldInfos fis = SegmentReader.ReadFieldInfos(si);
				foreach (FieldInfo fi in fis)
				{
					Field expected = GetField(System.Convert.ToInt32(fi.name));
					AreEqual(expected.FieldTypeValue.Indexed, fi.IsIndexed);
					AreEqual(expected.FieldTypeValue.StoreTermVectors, fi.HasVectors);
				}
			}
			dir.Dispose();
		}

		private Field GetField(int number)
		{
			int mode = number % 16;
			string fieldName = string.Empty + number;
			var customType = new FieldType(TextField.TYPE_STORED);
			var customType2 = new FieldType(TextField.TYPE_STORED) {Tokenized = (false)};
		    var customType3 = new FieldType(TextField.TYPE_NOT_STORED) {Tokenized = (false)};
		    var customType4 = new FieldType(TextField.TYPE_NOT_STORED)
		    {
		        Tokenized = (false),
		        StoreTermVectors = true,
		        StoreTermVectorOffsets = true
		    };
		    var customType5 = new FieldType(TextField.TYPE_NOT_STORED)
		    {
		        StoreTermVectors = true,
		        StoreTermVectorOffsets = true
		    };
		    var customType6 = new FieldType(TextField.TYPE_STORED)
		    {
		        Tokenized = (false),
		        StoreTermVectors = true,
		        StoreTermVectorOffsets = true
		    };
		    var customType7 = new FieldType(TextField.TYPE_NOT_STORED)
		    {
		        Tokenized = (false),
		        StoreTermVectors = true,
		        StoreTermVectorOffsets = true
		    };
		    var customType8 = new FieldType(TextField.TYPE_STORED)
		    {
		        Tokenized = (false),
		        StoreTermVectors = true,
		        StoreTermVectorPositions = true
		    };
		    var customType9 = new FieldType(TextField.TYPE_NOT_STORED)
		    {
		        StoreTermVectors = true,
		        StoreTermVectorPositions = true
		    };
		    var customType10 = new FieldType(TextField.TYPE_STORED)
		    {
		        Tokenized = (false),
		        StoreTermVectors = true,
		        StoreTermVectorPositions = true
		    };
		    var customType11 = new FieldType(TextField.TYPE_NOT_STORED)
		    {
		        Tokenized = (false),
		        StoreTermVectors = true,
		        StoreTermVectorPositions = true
		    };
		    var customType12 = new FieldType(TextField.TYPE_STORED)
		    {
		        StoreTermVectors = true,
		        StoreTermVectorOffsets = true,
		        StoreTermVectorPositions = true
		    };
		    var customType13 = new FieldType(TextField.TYPE_NOT_STORED)
		    {
		        StoreTermVectors = true,
		        StoreTermVectorOffsets = true,
		        StoreTermVectorPositions = true
		    };
		    var customType14 = new FieldType(TextField.TYPE_STORED)
		    {
		        Tokenized = (false),
		        StoreTermVectors = true,
		        StoreTermVectorOffsets = true,
		        StoreTermVectorPositions = true
		    };
		    var customType15 = new FieldType(TextField.TYPE_NOT_STORED)
		    {
		        Tokenized = (false),
		        StoreTermVectors = true,
		        StoreTermVectorOffsets = true,
		        StoreTermVectorPositions = true
		    };
		    switch (mode)
			{
				case 0:
				{
					return new Field(fieldName, "some text", customType);
				}

				case 1:
				{
					return new TextField(fieldName, "some text", Field.Store.NO);
				}

				case 2:
				{
					return new Field(fieldName, "some text", customType2);
				}

				case 3:
				{
					return new Field(fieldName, "some text", customType3);
				}

				case 4:
				{
					return new Field(fieldName, "some text", customType4);
				}

				case 5:
				{
					return new Field(fieldName, "some text", customType5);
				}

				case 6:
				{
					return new Field(fieldName, "some text", customType6);
				}

				case 7:
				{
					return new Field(fieldName, "some text", customType7);
				}

				case 8:
				{
					return new Field(fieldName, "some text", customType8);
				}

				case 9:
				{
					return new Field(fieldName, "some text", customType9);
				}

				case 10:
				{
					return new Field(fieldName, "some text", customType10);
				}

				case 11:
				{
					return new Field(fieldName, "some text", customType11);
				}

				case 12:
				{
					return new Field(fieldName, "some text", customType12);
				}

				case 13:
				{
					return new Field(fieldName, "some text", customType13);
				}

				case 14:
				{
					return new Field(fieldName, "some text", customType14);
				}

				case 15:
				{
					return new Field(fieldName, "some text", customType15);
				}

				default:
				{
					return null;
					break;
				}
			}
		}
	}
}
