using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.TestFramework;
using Lucene.Net.TestFramework.Util;
using NUnit.Framework;
using Lucene.Net.Codecs.Lucene41;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.TestFramework.Index;

using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Test.Index
{
    [TestFixture]
	public class TestDirectoryReader : LuceneTestCase
	{
		[Test]
		public virtual void TestDocument()
		{
			SegmentReader[] readers = new SegmentReader[2];
			Directory dir = NewDirectory();
			var doc1 = new Lucene.Net.Documents.Document();
			var doc2 = new Lucene.Net.Documents.Document();
			DocHelper.SetupDoc(doc1);
			DocHelper.SetupDoc(doc2);
			DocHelper.WriteDoc(Random(), dir, doc1);
			DocHelper.WriteDoc(Random(), dir, doc2);
			DirectoryReader reader = DirectoryReader.Open(dir);
			IsTrue(reader != null);
			IsTrue(reader is StandardDirectoryReader);
			Lucene.Net.Documents.Document newDoc1 = reader.Document(0);
			IsTrue(newDoc1 != null);
			IsTrue(DocHelper.NumFields(newDoc1) == DocHelper.NumFields
				(doc1) - DocHelper.unstored.Count);
			Lucene.Net.Documents.Document newDoc2 = reader.Document(1);
			IsTrue(newDoc2 != null);
			IsTrue(DocHelper.NumFields(newDoc2) == DocHelper.NumFields
				(doc2) - DocHelper.unstored.Count);
			Terms vector = reader.GetTermVectors(0).Terms(DocHelper.TEXT_FIELD_2_KEY);
			IsNotNull(vector);
			reader.Dispose();
			if (readers[0] != null)
			{
				readers[0].Dispose();
			}
			if (readers[1] != null)
			{
				readers[1].Dispose();
			}
			dir.Dispose();
		}

		[Test]
		public virtual void TestMultiTermDocs()
		{
			Directory ramDir1 = NewDirectory();
			AddDoc(Random(), ramDir1, "test foo", true);
			Directory ramDir2 = NewDirectory();
			AddDoc(Random(), ramDir2, "test blah", true);
			Directory ramDir3 = NewDirectory();
			AddDoc(Random(), ramDir3, "test wow", true);
			IndexReader[] readers1 = { DirectoryReader.Open(ramDir1), DirectoryReader.Open(ramDir3) };
			IndexReader[] readers2 = { DirectoryReader.Open(ramDir1), DirectoryReader.Open(ramDir2), DirectoryReader.Open(ramDir3) };
			MultiReader mr2 = new MultiReader(readers1);
			MultiReader mr3 = new MultiReader(readers2);
			// test mixing up TermDocs and TermEnums from different readers.
			TermsEnum te2 = MultiFields.GetTerms(mr2, "body").Iterator(null);
			te2.SeekCeil(new BytesRef("wow"));
			DocsEnum td = TestUtil.Docs(Random(), mr2, "body", te2.Term, MultiFields.GetLiveDocs
				(mr2), null, 0);
			TermsEnum te3 = MultiFields.GetTerms(mr3, "body").Iterator(null);
			te3.SeekCeil(new BytesRef("wow"));
			td = TestUtil.Docs(Random(), te3, MultiFields.GetLiveDocs(mr3), td, 0);
			int ret = 0;
			// This should blow up if we forget to check that the TermEnum is from the same
			// reader as the TermDocs.
			while (td.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
			{
				ret += td.DocID;
			}
			// really a dummy 
			
			//assert to ensure that we got some docs and to ensure that
			// nothing is eliminated by hotspot
			IsTrue(ret > 0);
			readers1[0].Dispose();
			readers1[1].Dispose();
			readers2[0].Dispose();
			readers2[1].Dispose();
			readers2[2].Dispose();
			ramDir1.Dispose();
			ramDir2.Dispose();
			ramDir3.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void AddDoc(Random random, Directory ramDir1, string s, bool create)
		{
			IndexWriter iw = new IndexWriter(ramDir1, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(random)).SetOpenMode(create ? IndexWriterConfig.OpenMode.CREATE
				 : IndexWriterConfig.OpenMode.APPEND));
			var doc = new Lucene.Net.Documents.Document {NewTextField("body", s, Field.Store.NO)};
		    iw.AddDocument(doc);
			iw.Dispose();
		}

		[Test]
		public virtual void TestIsCurrent()
		{
			Directory d = NewDirectory();
			IndexWriter writer = new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			AddDocumentWithFields(writer);
			writer.Dispose();
			// set up reader:
			DirectoryReader reader = DirectoryReader.Open(d);
			IsTrue(reader.IsCurrent);
			// modify index by adding another document:
			writer = new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND));
			AddDocumentWithFields(writer);
			writer.Dispose();
			IsFalse(reader.IsCurrent);
			// re-create index:
			writer = new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetOpenMode(IndexWriterConfig.OpenMode.CREATE));
			AddDocumentWithFields(writer);
			writer.Dispose();
			IsFalse(reader.IsCurrent);
			reader.Dispose();
			d.Dispose();
		}

		/// <summary>Tests the IndexReader.getFieldNames implementation</summary>
		[Test]
		public virtual void TestGetFieldNames()
		{
			Directory d = NewDirectory();
			// set up writer
			IndexWriter writer = new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
		    FieldType customType3 = new FieldType {Stored = (true)};
		    var doc = new Lucene.Net.Documents.Document
		    {
		        new StringField("keyword", "test1", Field.Store.YES),
		        new TextField("text", "test1", Field.Store.YES),
		        new Field("unindexed", "test1", customType3),
		        new TextField("unstored", "test1", Field.Store.NO)
		    };
		    writer.AddDocument(doc);
			writer.Dispose();
			// set up reader
			DirectoryReader reader = DirectoryReader.Open(d);
			FieldInfos fieldInfos = MultiFields.GetMergedFieldInfos(reader);
			IsNotNull(fieldInfos.FieldInfo("keyword"));
			IsNotNull(fieldInfos.FieldInfo("text"));
			IsNotNull(fieldInfos.FieldInfo("unindexed"));
			IsNotNull(fieldInfos.FieldInfo("unstored"));
			reader.Dispose();
			// add more documents
			writer = new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND).SetMergePolicy(NewLogMergePolicy
				()));
			// want to get some more segments here
			int mergeFactor = ((LogMergePolicy)writer.Config.MergePolicy).MergeFactor;
			for (int i = 0; i < 5 * mergeFactor; i++)
			{
				doc = new Lucene.Net.Documents.Document();
				doc.Add(new StringField("keyword", "test1", Field.Store.YES));
				doc.Add(new TextField("text", "test1", Field.Store.YES));
				doc.Add(new Field("unindexed", "test1", customType3));
				doc.Add(new TextField("unstored", "test1", Field.Store.NO));
				writer.AddDocument(doc);
			}
			// new fields are in some different segments (we hope)
			for (int i_1 = 0; i_1 < 5 * mergeFactor; i_1++)
			{
				doc = new Lucene.Net.Documents.Document();
				doc.Add(new StringField("keyword2", "test1", Field.Store.YES));
				doc.Add(new TextField("text2", "test1", Field.Store.YES));
				doc.Add(new Field("unindexed2", "test1", customType3));
				doc.Add(new TextField("unstored2", "test1", Field.Store.NO));
				writer.AddDocument(doc);
			}
			// new termvector fields
			FieldType customType5 = new FieldType(TextField.TYPE_STORED);
			customType5.StoreTermVectors = true;
			FieldType customType6 = new FieldType(TextField.TYPE_STORED);
			customType6.StoreTermVectors = true;
			customType6.StoreTermVectorOffsets = true;
			FieldType customType7 = new FieldType(TextField.TYPE_STORED);
			customType7.StoreTermVectors = true;
			customType7.StoreTermVectorPositions = true;
			FieldType customType8 = new FieldType(TextField.TYPE_STORED);
			customType8.StoreTermVectors = true;
			customType8.StoreTermVectorOffsets = true;
			customType8.StoreTermVectorPositions = true;
			for (int i_2 = 0; i_2 < 5 * mergeFactor; i_2++)
			{
				doc = new Lucene.Net.Documents.Document();
				doc.Add(new TextField("tvnot", "tvnot", Field.Store.YES));
				doc.Add(new Field("termvector", "termvector", customType5));
				doc.Add(new Field("tvoffset", "tvoffset", customType6));
				doc.Add(new Field("tvposition", "tvposition", customType7));
				doc.Add(new Field("tvpositionoffset", "tvpositionoffset", customType8));
				writer.AddDocument(doc);
			}
			writer.Dispose();
			// verify fields again
			reader = DirectoryReader.Open(d);
			fieldInfos = MultiFields.GetMergedFieldInfos(reader);
			ICollection<string> allFieldNames = new HashSet<string>();
			ICollection<string> indexedFieldNames = new HashSet<string>();
			ICollection<string> notIndexedFieldNames = new HashSet<string>();
			ICollection<string> tvFieldNames = new HashSet<string>();
			foreach (FieldInfo fieldInfo in fieldInfos)
			{
				string name = fieldInfo.name;
				allFieldNames.Add(name);
				if (fieldInfo.IsIndexed)
				{
					indexedFieldNames.Add(name);
				}
				else
				{
					notIndexedFieldNames.Add(name);
				}
				if (fieldInfo.HasVectors)
				{
					tvFieldNames.Add(name);
				}
			}
			IsTrue(allFieldNames.Contains("keyword"));
			IsTrue(allFieldNames.Contains("text"));
			IsTrue(allFieldNames.Contains("unindexed"));
			IsTrue(allFieldNames.Contains("unstored"));
			IsTrue(allFieldNames.Contains("keyword2"));
			IsTrue(allFieldNames.Contains("text2"));
			IsTrue(allFieldNames.Contains("unindexed2"));
			IsTrue(allFieldNames.Contains("unstored2"));
			IsTrue(allFieldNames.Contains("tvnot"));
			IsTrue(allFieldNames.Contains("termvector"));
			IsTrue(allFieldNames.Contains("tvposition"));
			IsTrue(allFieldNames.Contains("tvoffset"));
			IsTrue(allFieldNames.Contains("tvpositionoffset"));
			// verify that only indexed fields were returned
			AssertEquals(11, indexedFieldNames.Count);
			// 6 original + the 5 termvector fields 
			IsTrue(indexedFieldNames.Contains("keyword"));
			IsTrue(indexedFieldNames.Contains("text"));
			IsTrue(indexedFieldNames.Contains("unstored"));
			IsTrue(indexedFieldNames.Contains("keyword2"));
			IsTrue(indexedFieldNames.Contains("text2"));
			IsTrue(indexedFieldNames.Contains("unstored2"));
			IsTrue(indexedFieldNames.Contains("tvnot"));
			IsTrue(indexedFieldNames.Contains("termvector"));
			IsTrue(indexedFieldNames.Contains("tvposition"));
			IsTrue(indexedFieldNames.Contains("tvoffset"));
			IsTrue(indexedFieldNames.Contains("tvpositionoffset"));
			// verify that only unindexed fields were returned
			AssertEquals(2, notIndexedFieldNames.Count);
			// the following fields
			IsTrue(notIndexedFieldNames.Contains("unindexed"));
			IsTrue(notIndexedFieldNames.Contains("unindexed2"));
			// verify index term vector fields  
			AssertEquals(tvFieldNames.ToString(), 4, tvFieldNames.Count);
			// 4 field has term vector only
			IsTrue(tvFieldNames.Contains("termvector"));
			reader.Dispose();
			d.Dispose();
		}

		[Test]
		public virtual void TestTermVectors()
		{
			Directory d = NewDirectory();
			// set up writer
			IndexWriter writer = new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy()));
			// want to get some more segments here
			// new termvector fields
			int mergeFactor = ((LogMergePolicy)writer.Config.MergePolicy).MergeFactor;
			FieldType customType5 = new FieldType(TextField.TYPE_STORED);
			customType5.StoreTermVectors = true;
			FieldType customType6 = new FieldType(TextField.TYPE_STORED);
			customType6.StoreTermVectors = true;
			customType6.StoreTermVectorOffsets = true;
			FieldType customType7 = new FieldType(TextField.TYPE_STORED);
			customType7.StoreTermVectors = true;
			customType7.StoreTermVectorPositions = true;
			FieldType customType8 = new FieldType(TextField.TYPE_STORED);
			customType8.StoreTermVectors = true;
			customType8.StoreTermVectorOffsets = true;
			customType8.StoreTermVectorPositions = true;
			for (int i = 0; i < 5 * mergeFactor; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(new TextField("tvnot", "one two two three three three", Field.Store.YES));
				doc.Add(new Field("termvector", "one two two three three three", customType5));
				doc.Add(new Field("tvoffset", "one two two three three three", customType6));
				doc.Add(new Field("tvposition", "one two two three three three", customType7));
				doc.Add(new Field("tvpositionoffset", "one two two three three three", customType8
					));
				writer.AddDocument(doc);
			}
			writer.Dispose();
			d.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void AssertTermDocsCount(string msg, IndexReader reader, Term term
			, int expected)
		{
			DocsEnum tdocs = TestUtil.Docs(Random(), reader, term.Field, new BytesRef(term.Text), MultiFields.GetLiveDocs(reader), null, 0);
			int count = 0;
			if (tdocs != null)
			{
				while (tdocs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
				{
					count++;
				}
			}
			AssertEquals(msg + ", count mismatch", expected, count);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestBinaryFields()
		{
			Directory dir = NewDirectory();
			var bin = new sbyte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy()));
			for (int i = 0; i < 10; i++)
			{
				AddDoc(writer, "document number " + (i + 1));
				AddDocumentWithFields(writer);
				AddDocumentWithDifferentFields(writer);
				AddDocumentWithTermVectorFields(writer);
			}
			writer.Dispose();
			writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND).SetMergePolicy(NewLogMergePolicy
				()));
			var doc = new Lucene.Net.Documents.Document
			{
			    new StoredField("bin1", bin),
			    new TextField("junk", "junk text", Field.Store.NO)
			};
		    writer.AddDocument(doc);
			writer.Dispose();
			DirectoryReader reader = DirectoryReader.Open(dir);
			Lucene.Net.Documents.Document doc2 = reader.Document(reader.MaxDoc - 1);
			IIndexableField[] fields = doc2.GetFields("bin1");
			IsNotNull(fields);
			AssertEquals(1, fields.Length);
			IIndexableField b1 = fields[0];
			IsTrue(b1.BinaryValue != null);
			BytesRef bytesRef = b1.BinaryValue;
			AssertEquals(bin.Length, bytesRef.length);
			for (int i_1 = 0; i_1 < bin.Length; i_1++)
			{
				AssertEquals(bin[i_1], bytesRef.bytes[i_1 + bytesRef.offset]);
			}
			reader.Dispose();
			// force merge
			writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND).SetMergePolicy(NewLogMergePolicy
				()));
			writer.ForceMerge(1);
			writer.Dispose();
			reader = DirectoryReader.Open(dir);
			doc2 = reader.Document(reader.MaxDoc - 1);
			fields = doc2.GetFields("bin1");
			IsNotNull(fields);
			AssertEquals(1, fields.Length);
			b1 = fields[0];
			IsTrue(b1.BinaryValue != null);
			bytesRef = b1.BinaryValue;
			AssertEquals(bin.Length, bytesRef.length);
			for (int i_2 = 0; i_2 < bin.Length; i_2++)
			{
				AssertEquals(bin[i_2], bytesRef.bytes[i_2 + bytesRef.offset]);
			}
			reader.Dispose();
			dir.Dispose();
		}

		[Test]
		public virtual void TestFilesOpenClose()
		{
			// Create initial data set
			DirectoryInfo dirFile = CreateTempDir("TestIndexReader.testFilesOpenClose");
			Directory dir = NewFSDirectory(dirFile);
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
			AddDoc(writer, "test");
			writer.Dispose();
			dir.Dispose();
			// Try to erase the data - this ensures that the writer closed all files
            dirFile.Delete(true);
			
			dir = NewFSDirectory(dirFile);
			// Now create the data set again, just as before
			writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode.CREATE));
			AddDoc(writer, "test");
			writer.Dispose();
			dir.Dispose();
			// Now open existing directory and test that reader closes all files
			dir = NewFSDirectory(dirFile);
			DirectoryReader reader1 = DirectoryReader.Open(dir);
			reader1.Dispose();
			dir.Dispose();
			// The following will fail if reader did not close
			// all files
			dirFile.Delete(true); //this is needed
		}

		[Test]
		public virtual void TestOpenReaderAfterDelete()
		{
			DirectoryInfo dirFile = CreateTempDir("deletetest");
			Directory dir = NewFSDirectory(dirFile);
			try
			{
				DirectoryReader.Open(dir);
				Fail("expected FileNotFoundException/NoSuchFileException");
			}
			catch (IOException)
			{
			}
			// expected
			dirFile.Delete();
			// Make sure we still get a CorruptIndexException (not NPE):
			try
			{
				DirectoryReader.Open(dir);
				Fail("expected FileNotFoundException/NoSuchFileException");
			}
			catch (IOException)
			{
			}
			// expected
			dir.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal static void AddDocumentWithFields(IndexWriter writer)
		{
		    FieldType customType3 = new FieldType {Stored = (true)};
		    var doc = new Lucene.Net.Documents.Document
		    {
		        NewStringField("keyword", "test1", Field.Store.YES),
		        NewTextField("text", "test1", Field.Store.YES),
		        NewField("unindexed", "test1", customType3),
		        new TextField("unstored", "test1", Field.Store.NO)
		    };
		    writer.AddDocument(doc);
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal static void AddDocumentWithDifferentFields(IndexWriter writer)
		{
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			FieldType customType3 = new FieldType();
			customType3.Stored = (true);
			doc.Add(NewStringField("keyword2", "test1", Field.Store.YES));
			doc.Add(NewTextField("text2", "test1", Field.Store.YES));
			doc.Add(NewField("unindexed2", "test1", customType3));
			doc.Add(new TextField("unstored2", "test1", Field.Store.NO));
			writer.AddDocument(doc);
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal static void AddDocumentWithTermVectorFields(IndexWriter writer)
		{
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			FieldType customType5 = new FieldType(TextField.TYPE_STORED);
			customType5.StoreTermVectors = true;
			FieldType customType6 = new FieldType(TextField.TYPE_STORED);
			customType6.StoreTermVectors = true;
			customType6.StoreTermVectorOffsets = true;
			FieldType customType7 = new FieldType(TextField.TYPE_STORED);
			customType7.StoreTermVectors = true;
			customType7.StoreTermVectorPositions = true;
			FieldType customType8 = new FieldType(TextField.TYPE_STORED);
			customType8.StoreTermVectors = true;
			customType8.StoreTermVectorOffsets = true;
			customType8.StoreTermVectorPositions = true;
			doc.Add(NewTextField("tvnot", "tvnot", Field.Store.YES));
			doc.Add(NewField("termvector", "termvector", customType5));
			doc.Add(NewField("tvoffset", "tvoffset", customType6));
			doc.Add(NewField("tvposition", "tvposition", customType7));
			doc.Add(NewField("tvpositionoffset", "tvpositionoffset", customType8));
			writer.AddDocument(doc);
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal static void AddDoc(IndexWriter writer, string value)
		{
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewTextField("content", value, Field.Store.NO));
			writer.AddDocument(doc);
		}

		// TODO: maybe this can reuse the logic of test dueling codecs?
		/// <exception cref="System.IO.IOException"></exception>
		public static void AssertIndexEquals(DirectoryReader index1, DirectoryReader index2
			)
		{
			AssertEquals("IndexReaders have different values for numDocs."
				, index1.NumDocs, index2.NumDocs);
			AssertEquals("IndexReaders have different values for maxDoc.", 
				index1.MaxDoc, index2.MaxDoc);
			AssertEquals("Only one IndexReader has deletions.", index1.HasDeletions, index2.HasDeletions);
			AssertEquals("Single segment test differs.", index1.Leaves.Count
				 == 1, index2.Leaves.Count == 1);
			// check field names
			FieldInfos fieldInfos1 = MultiFields.GetMergedFieldInfos(index1);
			FieldInfos fieldInfos2 = MultiFields.GetMergedFieldInfos(index2);
			AssertEquals("IndexReaders have different numbers of fields.", fieldInfos1.Size, fieldInfos2.Size);
			int numFields = fieldInfos1.Size;
			for (int fieldID = 0; fieldID < numFields; fieldID++)
			{
				FieldInfo fieldInfo1 = fieldInfos1.FieldInfo(fieldID);
				FieldInfo fieldInfo2 = fieldInfos2.FieldInfo(fieldID);
				AssertEquals("Different field names.", fieldInfo1.name, fieldInfo2
					.name);
			}
			// check norms
			foreach (FieldInfo fieldInfo in fieldInfos1)
			{
				string curField = fieldInfo.name;
				NumericDocValues norms1 = MultiDocValues.GetNormValues(index1, curField);
				NumericDocValues norms2 = MultiDocValues.GetNormValues(index2, curField);
				if (norms1 != null && norms2 != null)
				{
					// todo: generalize this (like TestDuelingCodecs 
					//HM:revisit 
					//assert)
					for (int i = 0; i < index1.MaxDoc; i++)
					{
						AssertEquals("Norm different for doc " + i + " and field '" + 
							curField + "'.", norms1.Get(i), norms2.Get(i));
					}
				}
				else
				{
					IsNull(norms1);
					IsNull(norms2);
				}
			}
			// check deletions
			IBits liveDocs1 = MultiFields.GetLiveDocs(index1);
			IBits liveDocs2 = MultiFields.GetLiveDocs(index2);
			for (int i = 0; i < index1.MaxDoc; i++)
			{
				AssertEquals("Doc " + i + " only deleted in one index.", liveDocs1
					 == null || !liveDocs1[i], liveDocs2 == null || !liveDocs2[i]);
			}
			// check stored fields
			for (int i = 0; i < index1.MaxDoc; i++)
			{
				if (liveDocs1 == null || liveDocs1[i])
				{
					Lucene.Net.Documents.Document doc1 = index1.Document(i);
					Lucene.Net.Documents.Document doc2 = index2.Document(i);
					IList<IIndexableField> field1 = doc1.GetFields();
					IList<IIndexableField> field2 = doc2.GetFields();
					AssertEquals("Different numbers of fields for doc " + i + "."
						, field1.Count, field2.Count);
					IEnumerator<IIndexableField> itField1 = field1.GetEnumerator();
					IEnumerator<IIndexableField> itField2 = field2.GetEnumerator();
					while (itField1.MoveNext())
					{
						Field curField1 = (Field)itField1.Current;
						Field curField2 = (Field)itField2.Current;
						AssertEquals("Different fields names for doc " + i + ".", curField1.Name, curField2.Name);
						AssertEquals("Different field values for doc " + i + ".", curField1
							.StringValue, curField2.StringValue);
					}
				}
			}
			// check dictionary and posting lists
			Fields fields1 = MultiFields.GetFields(index1);
			Fields fields2 = MultiFields.GetFields(index2);
			IEnumerator<string> fenum2 = fields2.GetEnumerator();
			IBits liveDocs = MultiFields.GetLiveDocs(index1);
			foreach (string field1_1 in fields1)
			{
				AssertEquals("Different fields", field1_1, fenum2.Current);
				Terms terms1 = fields1.Terms(field1_1);
				if (terms1 == null)
				{
					IsNull(fields2.Terms(field1_1));
					continue;
				}
				TermsEnum enum1 = terms1.Iterator(null);
				Terms terms2 = fields2.Terms(field1_1);
				IsNotNull(terms2);
				TermsEnum enum2 = terms2.Iterator(null);
				while (enum1.Next() != null)
				{
					AssertEquals("Different terms", enum1.Term, enum2.Next());
					DocsAndPositionsEnum tp1 = enum1.DocsAndPositions(liveDocs, null);
					DocsAndPositionsEnum tp2 = enum2.DocsAndPositions(liveDocs, null);
					while (tp1.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
					{
						IsTrue(tp2.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
						AssertEquals("Different doc id in postinglist of term " + enum1
							.Term + ".", tp1.DocID, tp2.DocID);
						AssertEquals("Different term frequence in postinglist of term "
							 + enum1.Term + ".", tp1.Freq, tp2.Freq);
						for (int i = 0; i < tp1.Freq; i++)
						{
							AssertEquals("Different positions in postinglist of term " + enum1
								.Term + ".", tp1.NextPosition(), tp2.NextPosition());
						}
					}
				}
			}
			IsFalse(fenum2.MoveNext());
		}

		[Test]
		public virtual void TestGetIndexCommit()
		{
			Directory d = NewDirectory();
			// set up writer
			IndexWriter writer = new IndexWriter(d, ((IndexWriterConfig)NewIndexWriterConfig(
				TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2)).SetMergePolicy
				(NewLogMergePolicy(10)));
			for (int i = 0; i < 27; i++)
			{
				AddDocumentWithFields(writer);
			}
			writer.Dispose();
			SegmentInfos sis = new SegmentInfos();
			sis.Read(d);
			DirectoryReader r = DirectoryReader.Open(d);
			IndexCommit c = r.IndexCommit;
			AssertEquals(sis.SegmentsFileName, c.SegmentsFileName);
			IsTrue(c.Equals(r.IndexCommit));
			// Change the index
			writer = new IndexWriter(d, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND).SetMaxBufferedDocs
				(2)).SetMergePolicy(NewLogMergePolicy(10)));
			for (int i_1 = 0; i_1 < 7; i_1++)
			{
				AddDocumentWithFields(writer);
			}
			writer.Dispose();
			DirectoryReader r2 = DirectoryReader.OpenIfChanged(r);
			IsNotNull(r2);
			IsFalse(c.Equals(r2.IndexCommit));
			IsFalse(r2.IndexCommit.SegmentCount == 1);
			r2.Dispose();
			writer = new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND));
			writer.ForceMerge(1);
			writer.Dispose();
			r2 = DirectoryReader.OpenIfChanged(r);
			IsNotNull(r2);
			IsNull(DirectoryReader.OpenIfChanged(r2));
			AssertEquals(1, r2.IndexCommit.SegmentCount);
			r.Dispose();
			r2.Dispose();
			d.Dispose();
		}

		internal static Lucene.Net.Documents.Document CreateDocument(string id)
		{
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			FieldType customType = new FieldType(TextField.TYPE_STORED);
			customType.Tokenized = (false);
			customType.OmitNorms = (true);
			doc.Add(NewField("id", id, customType));
			return doc;
		}

		// LUCENE-1468 -- make sure on attempting to open an
		// DirectoryReader on a non-existent directory, you get a
		// good exception
		[Test]
		public virtual void TestNoDir()
		{
			DirectoryInfo tempDir = CreateTempDir("doesnotexist");
            tempDir.Delete(true);
			
			Directory dir = NewFSDirectory(tempDir);
			try
			{
				DirectoryReader.Open(dir);
				Fail("did not hit expected exception");
			}
			catch (NoSuchDirectoryException)
			{
			}
			// expected
			dir.Dispose();
		}

		// LUCENE-1509
		[Test]
		public virtual void TestNoDupCommitFileNames()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2)));
			writer.AddDocument(CreateDocument("a"));
			writer.AddDocument(CreateDocument("a"));
			writer.AddDocument(CreateDocument("a"));
			writer.Dispose();
			ICollection<IndexCommit> commits = DirectoryReader.ListCommits(dir);
			foreach (IndexCommit commit in commits)
			{
				ICollection<string> files = commit.FileNames;
				HashSet<string> seen = new HashSet<string>();
				foreach (string fileName in files)
				{
					AssertTrue("file " + fileName + " was duplicated", !seen.Contains(fileName));
					seen.Add(fileName);
				}
			}
			dir.Dispose();
		}

		// LUCENE-1579: Ensure that on a reopened reader, that any
		// shared segments reuse the doc values arrays in
		// FieldCache
		[Test]
		public virtual void TestFieldCacheReuseAfterReopen()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy(10)));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
			{
			    NewStringField("number", "17", Field.Store.NO)
			};
		    writer.AddDocument(doc);
			writer.Commit();
			// Open reader1
			DirectoryReader r = DirectoryReader.Open(dir);
			AtomicReader r1 = GetOnlySegmentReader(r);
			FieldCache.Ints ints = FieldCache.DEFAULT.GetInts(r1, "number", false);
			AssertEquals(17, ints.Get(0));
			// Add new segment
			writer.AddDocument(doc);
			writer.Commit();
			// Reopen reader1 --> reader2
			DirectoryReader r2 = DirectoryReader.OpenIfChanged(r);
			IsNotNull(r2);
			r.Dispose();
			AtomicReader sub0 = ((AtomicReader)r2.Leaves[0].Reader);
			FieldCache.Ints ints2 = FieldCache.DEFAULT.GetInts(sub0, "number", false);
			r2.Dispose();
			IsTrue(ints == ints2);
			writer.Dispose();
			dir.Dispose();
		}

		// LUCENE-1586: getUniqueTermCount
		[Test]
		public virtual void TestUniqueTermCount()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			var doc = new Lucene.Net.Documents.Document
			{
			    NewTextField("field", "a b c d e f g h i j k l m n o p q r s t u v w x y z"
			        , Field.Store.NO),
			    NewTextField("number", "0 1 2 3 4 5 6 7 8 9", Field.Store.NO)
			};
		    writer.AddDocument(doc);
			writer.AddDocument(doc);
			writer.Commit();
			DirectoryReader r = DirectoryReader.Open(dir);
			AtomicReader r1 = GetOnlySegmentReader(r);
			AssertEquals(36, r1.Fields.UniqueTermCount);
			writer.AddDocument(doc);
			writer.Commit();
			DirectoryReader r2 = DirectoryReader.OpenIfChanged(r);
			IsNotNull(r2);
			r.Dispose();
			foreach (AtomicReaderContext s in r2.Leaves)
			{
				AssertEquals(36, ((AtomicReader)s.Reader).Fields.UniqueTermCount);
			}
			r2.Dispose();
			writer.Dispose();
			dir.Dispose();
		}

		// LUCENE-1609: don't load terms index
		[Test]
		public virtual void TestNoTermsIndex()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetCodec(TestUtil.AlwaysPostingsFormat(new Lucene41PostingsFormat
				())));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewTextField("field", "a b c d e f g h i j k l m n o p q r s t u v w x y z"
				, Field.Store.NO));
			doc.Add(NewTextField("number", "0 1 2 3 4 5 6 7 8 9", Field.Store.NO));
			writer.AddDocument(doc);
			writer.AddDocument(doc);
			writer.Dispose();
			DirectoryReader r = DirectoryReader.Open(dir, -1);
			try
			{
				r.DocFreq(new Term("field", "f"));
				Fail("did not hit expected exception");
			}
			catch (InvalidOperationException)
			{
			}
			// expected
			AssertEquals(-1, ((SegmentReader)r.Leaves[0].Reader).TermInfosIndexDivisor);
			writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetCodec(TestUtil.AlwaysPostingsFormat(new Lucene41PostingsFormat())
				).SetMergePolicy(NewLogMergePolicy(10)));
			writer.AddDocument(doc);
			writer.Dispose();
			// LUCENE-1718: ensure re-open carries over no terms index:
			DirectoryReader r2 = DirectoryReader.OpenIfChanged(r);
			IsNotNull(r2);
			IsNull(DirectoryReader.OpenIfChanged(r2));
			r.Dispose();
			IList<AtomicReaderContext> leaves = r2.Leaves;
			AssertEquals(2, leaves.Count);
			foreach (AtomicReaderContext ctx in leaves)
			{
				try
				{
					((AtomicReader)ctx.Reader).DocFreq(new Term("field", "f"));
					Fail("did not hit expected exception");
				}
				catch (InvalidOperationException)
				{
				}
			}
			// expected
			r2.Dispose();
			dir.Dispose();
		}

		// LUCENE-2046
		[Test]
		public virtual void TestPrepareCommitIsCurrent()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			writer.Commit();
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
			writer.AddDocument(doc);
			DirectoryReader r = DirectoryReader.Open(dir);
			IsTrue(r.IsCurrent);
			writer.AddDocument(doc);
			writer.PrepareCommit();
			IsTrue(r.IsCurrent);
			DirectoryReader r2 = DirectoryReader.OpenIfChanged(r);
			IsNull(r2);
			writer.Commit();
			IsFalse(r.IsCurrent);
			writer.Dispose();
			r.Dispose();
			dir.Dispose();
		}

		// LUCENE-2753
		[Test]
		public virtual void TestListCommits()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, null).SetIndexDeletionPolicy(new SnapshotDeletionPolicy(new KeepOnlyLastCommitDeletionPolicy
				())));
			SnapshotDeletionPolicy sdp = (SnapshotDeletionPolicy)writer.Config.IndexDeletionPolicy;
			writer.AddDocument(new Lucene.Net.Documents.Document());
			writer.Commit();
			sdp.Snapshot();
			writer.AddDocument(new Lucene.Net.Documents.Document());
			writer.Commit();
			sdp.Snapshot();
			writer.AddDocument(new Lucene.Net.Documents.Document());
			writer.Commit();
			sdp.Snapshot();
			writer.Dispose();
			long currentGen = 0;
			foreach (IndexCommit ic in DirectoryReader.ListCommits(dir))
			{
				AssertTrue("currentGen=" + currentGen + " commitGen=" + ic.Generation, currentGen < ic.Generation);
				currentGen = ic.Generation;
			}
			dir.Dispose();
		}

		// Make sure totalTermFreq works correctly in the terms
		// dict cache
		[Test]
		public virtual void TestTotalTermFreqCached()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
			d.Add(NewTextField("f", "a a b", Field.Store.NO));
			writer.AddDocument(d);
			DirectoryReader r = writer.Reader;
			writer.Dispose();
			try
			{
				// Make sure codec impls totalTermFreq (eg PreFlex doesn't)
				Assume.That(r.TotalTermFreq(new Term("f", new BytesRef("b"))) != -1);
				AssertEquals(1, r.TotalTermFreq(new Term("f", new BytesRef("b"
					))));
				AssertEquals(2, r.TotalTermFreq(new Term("f", new BytesRef("a"
					))));
				AssertEquals(1, r.TotalTermFreq(new Term("f", new BytesRef("b"
					))));
			}
			finally
			{
				r.Dispose();
				dir.Dispose();
			}
		}

		[Test]
		public virtual void TestGetSumDocFreq()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
			d.Add(NewTextField("f", "a", Field.Store.NO));
			writer.AddDocument(d);
			d = new Lucene.Net.Documents.Document();
			d.Add(NewTextField("f", "b", Field.Store.NO));
			writer.AddDocument(d);
			DirectoryReader r = writer.Reader;
			writer.Dispose();
			try
			{
				// Make sure codec impls getSumDocFreq (eg PreFlex doesn't)
				Assume.That(r.GetSumDocFreq("f") != -1);
				AssertEquals(2, r.GetSumDocFreq("f"));
			}
			finally
			{
				r.Dispose();
				dir.Dispose();
			}
		}

		[Test]
		public virtual void TestGetDocCount()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			var d = new Lucene.Net.Documents.Document {NewTextField("f", "a", Field.Store.NO)};
		    writer.AddDocument(d);
			d = new Lucene.Net.Documents.Document();
			d.Add(NewTextField("f", "a", Field.Store.NO));
			writer.AddDocument(d);
			DirectoryReader r = writer.Reader;
			writer.Dispose();
			try
			{
				// Make sure codec impls getSumDocFreq (eg PreFlex doesn't)
				Assume.That(r.GetDocCount("f") != -1);
				AssertEquals(2, r.GetDocCount("f"));
			}
			finally
			{
				r.Dispose();
				dir.Dispose();
			}
		}

		[Test]
		public virtual void TestGetSumTotalTermFreq()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
			d.Add(NewTextField("f", "a b b", Field.Store.NO));
			writer.AddDocument(d);
			d = new Lucene.Net.Documents.Document();
			d.Add(NewTextField("f", "a a b", Field.Store.NO));
			writer.AddDocument(d);
			DirectoryReader r = writer.Reader;
			writer.Dispose();
			try
			{
				// Make sure codec impls getSumDocFreq (eg PreFlex doesn't)
				Assume.That(r.GetSumTotalTermFreq("f") != -1);
				AssertEquals(6, r.GetSumTotalTermFreq("f"));
			}
			finally
			{
				r.Dispose();
				dir.Dispose();
			}
		}

		// LUCENE-2474
		[Test]
		public virtual void TestReaderFinishedListener()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy()));
			((LogMergePolicy)writer.Config.MergePolicy).MergeFactor = (3);
			writer.AddDocument(new Lucene.Net.Documents.Document());
			writer.Commit();
			writer.AddDocument(new Lucene.Net.Documents.Document());
			writer.Commit();
			DirectoryReader reader = writer.Reader;
			int[] closeCount = new int[1];
			IndexReader.IReaderClosedListener listener = new AnonymousReaderClosedListener(closeCount);
			reader.AddReaderClosedListener(listener);
			reader.Dispose();
			// Close the top reader, its the only one that should be closed
			AssertEquals(1, closeCount[0]);
			writer.Dispose();
			DirectoryReader reader2 = DirectoryReader.Open(dir);
			reader2.AddReaderClosedListener(listener);
			closeCount[0] = 0;
			reader2.Dispose();
			AssertEquals(1, closeCount[0]);
			dir.Dispose();
		}

		private sealed class AnonymousReaderClosedListener : IndexReader.IReaderClosedListener
		{
			public AnonymousReaderClosedListener(int[] closeCount)
			{
				this.closeCount = closeCount;
			}

			public void OnClose(IndexReader reader)
			{
				closeCount[0]++;
			}

			private readonly int[] closeCount;
		}

		[Test]
		public virtual void TestOOBDocID()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			writer.AddDocument(new Lucene.Net.Documents.Document());
			DirectoryReader r = writer.Reader;
			writer.Dispose();
			r.Document(0);
			try
			{
				r.Document(1);
				Fail("did not hit exception");
			}
			catch (ArgumentException)
			{
			}
			// expected
			r.Dispose();
			dir.Dispose();
		}

		[Test]
		public virtual void TestTryIncRef()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			writer.AddDocument(new Lucene.Net.Documents.Document());
			writer.Commit();
			DirectoryReader r = DirectoryReader.Open(dir);
			IsTrue(r.TryIncRef());
			r.DecRef();
			r.Dispose();
			IsFalse(r.TryIncRef());
			writer.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		[Test]
		public virtual void TestStressTryIncRef()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			writer.AddDocument(new Lucene.Net.Documents.Document());
			writer.Commit();
			DirectoryReader r = DirectoryReader.Open(dir);
			int numThreads = AtLeast(2);
			var threads = new Thread[numThreads];
		    Exception failed = null;
			for (int i = 0; i < threads.Length; i++)
			{
				threads[i] = new Thread(o =>
				{
				    IndexReader toInc = (IndexReader) o;
                    try
                    {
                        while (toInc.TryIncRef())
                        {
                            IsFalse(toInc.HasDeletions);
                            toInc.DecRef();
                        }
                        IsFalse(toInc.TryIncRef());
                    }
                    catch (Exception e)
                    {
                        failed = e;
                    }
				});
				threads[i].Start(r);
			}
			Thread.Sleep(100);
			IsTrue(r.TryIncRef());
			r.DecRef();
			r.Dispose();
			for (int i = 0; i < threads.Length; i++)
			{
				threads[i].Join();
				IsNull(failed);
			}
			IsFalse(r.TryIncRef());
			writer.Dispose();
			dir.Dispose();
		}

		

		[Test]
		public virtual void TestLoadCertainFields()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewStringField("field1", "foobar", Field.Store.YES));
			doc.Add(NewStringField("field2", "foobaz", Field.Store.YES));
			writer.AddDocument(doc);
			DirectoryReader r = writer.Reader;
			writer.Close();
			var fieldsToLoad = new HashSet<string>();
			AssertEquals(0, r.Document(0, fieldsToLoad).GetFields().Count);
			fieldsToLoad.Add("field1");
			Lucene.Net.Documents.Document doc2 = r.Document(0, fieldsToLoad);
			AssertEquals(1, doc2.GetFields().Count);
			AssertEquals("foobar", doc2.Get("field1"));
			r.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		
		[Obsolete(@"just to ensure IndexReader static methods work")]
        [Test]
		public virtual void TestBackwards()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetCodec(TestUtil.AlwaysPostingsFormat(new Lucene41PostingsFormat
				())));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewTextField("field", "a b c d e f g h i j k l m n o p q r s t u v w x y z"
				, Field.Store.NO));
			doc.Add(NewTextField("number", "0 1 2 3 4 5 6 7 8 9", Field.Store.NO));
			writer.AddDocument(doc);
			// open(IndexWriter, boolean)
			DirectoryReader r = IndexReader.Open(writer, true);
			AssertEquals(1, r.DocFreq(new Term("field", "f")));
			r.Dispose();
			writer.AddDocument(doc);
			writer.Dispose();
			// open(Directory)
			r = IndexReader.Open(dir);
			AssertEquals(2, r.DocFreq(new Term("field", "f")));
			r.Dispose();
			// open(IndexCommit)
			IList<IndexCommit> commits = DirectoryReader.ListCommits(dir);
			AssertEquals(1, commits.Count);
			r = IndexReader.Open(commits[0]);
			AssertEquals(2, r.DocFreq(new Term("field", "f")));
			r.Dispose();
			// open(Directory, int)
			r = IndexReader.Open(dir, -1);
			try
			{
				r.DocFreq(new Term("field", "f"));
				Fail("did not hit expected exception");
			}
			catch (InvalidOperationException)
			{
			}
			// expected
			AssertEquals(-1, ((SegmentReader)((AtomicReader)r.Leaves[0].Reader)).TermInfosIndexDivisor);
			r.Dispose();
			// open(IndexCommit, int)
			r = IndexReader.Open(commits[0], -1);
			try
			{
				r.DocFreq(new Term("field", "f"));
				Fail("did not hit expected exception");
			}
			catch (InvalidOperationException)
			{
			}
			// expected
			AssertEquals(-1, ((SegmentReader)((AtomicReader)r.Leaves[0].Reader)).TermInfosIndexDivisor);
			r.Dispose();
			dir.Dispose();
		}

		[Test]
		public virtual void TestIndexExistsOnNonExistentDirectory()
		{
			DirectoryInfo tempDir = CreateTempDir("testIndexExistsOnNonExistentDirectory");
			tempDir.Delete();
			Directory dir = NewFSDirectory(tempDir);
			System.Console.Out.WriteLine("dir=" + dir);
			IsFalse(DirectoryReader.IndexExists(dir));
			dir.Dispose();
		}
	}
}
