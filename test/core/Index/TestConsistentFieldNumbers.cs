/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	public class TestConsistentFieldNumbers : LuceneTestCase
	{
		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
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
					writer.Close();
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
				writer.Close();
				SegmentInfos sis = new SegmentInfos();
				sis.Read(dir);
				AreEqual(2, sis.Size());
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
				writer.Close();
				sis = new SegmentInfos();
				sis.Read(dir);
				AreEqual(1, sis.Size());
				FieldInfos fis3 = SegmentReader.ReadFieldInfos(sis.Info(0));
				AreEqual("f1", fis3.FieldInfo(0).name);
				AreEqual("f2", fis3.FieldInfo(1).name);
				AreEqual("f3", fis3.FieldInfo(2).name);
				AreEqual("f4", fis3.FieldInfo(3).name);
				dir.Close();
			}
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestAddIndexes()
		{
			Directory dir1 = NewDirectory();
			Directory dir2 = NewDirectory();
			IndexWriter writer = new IndexWriter(dir1, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetMergePolicy(NoMergePolicy.COMPOUND_FILES));
			Lucene.Net.Documents.Document d1 = new Lucene.Net.Documents.Document(
				);
			d1.Add(new TextField("f1", "first field", Field.Store.YES));
			d1.Add(new TextField("f2", "second field", Field.Store.YES));
			writer.AddDocument(d1);
			writer.Close();
			writer = new IndexWriter(dir2, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetMergePolicy(NoMergePolicy.COMPOUND_FILES));
			Lucene.Net.Documents.Document d2 = new Lucene.Net.Documents.Document(
				);
			FieldType customType2 = new FieldType(TextField.TYPE_STORED);
			customType2.StoreTermVectors = true;
			d2.Add(new TextField("f2", "second field", Field.Store.YES));
			d2.Add(new Field("f1", "first field", customType2));
			d2.Add(new TextField("f3", "third field", Field.Store.YES));
			d2.Add(new TextField("f4", "fourth field", Field.Store.YES));
			writer.AddDocument(d2);
			writer.Close();
			writer = new IndexWriter(dir1, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetMergePolicy(NoMergePolicy.COMPOUND_FILES));
			writer.AddIndexes(dir2);
			writer.Close();
			SegmentInfos sis = new SegmentInfos();
			sis.Read(dir1);
			AreEqual(2, sis.Size());
			FieldInfos fis1 = SegmentReader.ReadFieldInfos(sis.Info(0));
			FieldInfos fis2 = SegmentReader.ReadFieldInfos(sis.Info(1));
			AreEqual("f1", fis1.FieldInfo(0).name);
			AreEqual("f2", fis1.FieldInfo(1).name);
			// make sure the ordering of the "external" segment is preserved
			AreEqual("f2", fis2.FieldInfo(0).name);
			AreEqual("f1", fis2.FieldInfo(1).name);
			AreEqual("f3", fis2.FieldInfo(2).name);
			AreEqual("f4", fis2.FieldInfo(3).name);
			dir1.Close();
			dir2.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
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
					writer.Close();
					SegmentInfos sis = new SegmentInfos();
					sis.Read(dir);
					AreEqual(1, sis.Size());
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
					d.Add(new StoredField("f3", new byte[] { 1, 2, 3 }));
					writer.AddDocument(d);
					writer.Close();
					SegmentInfos sis = new SegmentInfos();
					sis.Read(dir);
					AreEqual(2, sis.Size());
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
					d.Add(new StoredField("f3", new byte[] { 1, 2, 3, 4, 5 }));
					writer.AddDocument(d);
					writer.Close();
					SegmentInfos sis = new SegmentInfos();
					sis.Read(dir);
					AreEqual(3, sis.Size());
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
					writer.Close();
				}
				IndexWriter writer_1 = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
					, new MockAnalyzer(Random())).SetMergePolicy(new LogByteSizeMergePolicy()).SetInfoStream
					(new FailOnNonBulkMergesInfoStream()));
				writer_1.ForceMerge(1);
				writer_1.Close();
				SegmentInfos sis_1 = new SegmentInfos();
				sis_1.Read(dir);
				AreEqual(1, sis_1.Size());
				FieldInfos fis1_1 = SegmentReader.ReadFieldInfos(sis_1.Info(0));
				AreEqual("f1", fis1_1.FieldInfo(0).name);
				AreEqual("f2", fis1_1.FieldInfo(1).name);
				AreEqual("f3", fis1_1.FieldInfo(2).name);
				dir.Close();
			}
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
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
			writer.Close();
			SegmentInfos sis = new SegmentInfos();
			sis.Read(dir);
			foreach (SegmentCommitInfo si in sis)
			{
				FieldInfos fis = SegmentReader.ReadFieldInfos(si);
				foreach (FieldInfo fi in fis)
				{
					Field expected = GetField(System.Convert.ToInt32(fi.name));
					AreEqual(expected.FieldType().Indexed(), fi.IsIndexed());
					AreEqual(expected.FieldType().StoreTermVectors(), fi.HasVectors
						());
				}
			}
			dir.Close();
		}

		private Field GetField(int number)
		{
			int mode = number % 16;
			string fieldName = string.Empty + number;
			FieldType customType = new FieldType(TextField.TYPE_STORED);
			FieldType customType2 = new FieldType(TextField.TYPE_STORED);
			customType2.SetTokenized(false);
			FieldType customType3 = new FieldType(TextField.TYPE_NOT_STORED);
			customType3.SetTokenized(false);
			FieldType customType4 = new FieldType(TextField.TYPE_NOT_STORED);
			customType4.SetTokenized(false);
			customType4.StoreTermVectors = true;
			customType4.StoreTermVectorOffsets = true;
			FieldType customType5 = new FieldType(TextField.TYPE_NOT_STORED);
			customType5.StoreTermVectors = true;
			customType5.StoreTermVectorOffsets = true;
			FieldType customType6 = new FieldType(TextField.TYPE_STORED);
			customType6.SetTokenized(false);
			customType6.StoreTermVectors = true;
			customType6.StoreTermVectorOffsets = true;
			FieldType customType7 = new FieldType(TextField.TYPE_NOT_STORED);
			customType7.SetTokenized(false);
			customType7.StoreTermVectors = true;
			customType7.StoreTermVectorOffsets = true;
			FieldType customType8 = new FieldType(TextField.TYPE_STORED);
			customType8.SetTokenized(false);
			customType8.StoreTermVectors = true;
			customType8.StoreTermVectorPositions = true;
			FieldType customType9 = new FieldType(TextField.TYPE_NOT_STORED);
			customType9.StoreTermVectors = true;
			customType9.StoreTermVectorPositions = true;
			FieldType customType10 = new FieldType(TextField.TYPE_STORED);
			customType10.SetTokenized(false);
			customType10.StoreTermVectors = true;
			customType10.StoreTermVectorPositions = true;
			FieldType customType11 = new FieldType(TextField.TYPE_NOT_STORED);
			customType11.SetTokenized(false);
			customType11.StoreTermVectors = true;
			customType11.StoreTermVectorPositions = true;
			FieldType customType12 = new FieldType(TextField.TYPE_STORED);
			customType12.StoreTermVectors = true;
			customType12.StoreTermVectorOffsets = true;
			customType12.StoreTermVectorPositions = true;
			FieldType customType13 = new FieldType(TextField.TYPE_NOT_STORED);
			customType13.StoreTermVectors = true;
			customType13.StoreTermVectorOffsets = true;
			customType13.StoreTermVectorPositions = true;
			FieldType customType14 = new FieldType(TextField.TYPE_STORED);
			customType14.SetTokenized(false);
			customType14.StoreTermVectors = true;
			customType14.StoreTermVectorOffsets = true;
			customType14.StoreTermVectorPositions = true;
			FieldType customType15 = new FieldType(TextField.TYPE_NOT_STORED);
			customType15.SetTokenized(false);
			customType15.StoreTermVectors = true;
			customType15.StoreTermVectorOffsets = true;
			customType15.StoreTermVectorPositions = true;
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
