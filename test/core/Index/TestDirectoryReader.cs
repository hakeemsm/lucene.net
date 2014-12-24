/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Codecs.Lucene41;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	public class TestDirectoryReader : LuceneTestCase
	{
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestDocument()
		{
			SegmentReader[] readers = new SegmentReader[2];
			Directory dir = NewDirectory();
			Lucene.Net.Documents.Document doc1 = new Lucene.Net.Documents.Document
				();
			Lucene.Net.Documents.Document doc2 = new Lucene.Net.Documents.Document
				();
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
			reader.Close();
			if (readers[0] != null)
			{
				readers[0].Close();
			}
			if (readers[1] != null)
			{
				readers[1].Close();
			}
			dir.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestMultiTermDocs()
		{
			Directory ramDir1 = NewDirectory();
			AddDoc(Random(), ramDir1, "test foo", true);
			Directory ramDir2 = NewDirectory();
			AddDoc(Random(), ramDir2, "test blah", true);
			Directory ramDir3 = NewDirectory();
			AddDoc(Random(), ramDir3, "test wow", true);
			IndexReader[] readers1 = new IndexReader[] { DirectoryReader.Open(ramDir1), DirectoryReader
				.Open(ramDir3) };
			IndexReader[] readers2 = new IndexReader[] { DirectoryReader.Open(ramDir1), DirectoryReader
				.Open(ramDir2), DirectoryReader.Open(ramDir3) };
			MultiReader mr2 = new MultiReader(readers1);
			MultiReader mr3 = new MultiReader(readers2);
			// test mixing up TermDocs and TermEnums from different readers.
			TermsEnum te2 = MultiFields.GetTerms(mr2, "body").Iterator(null);
			te2.SeekCeil(new BytesRef("wow"));
			DocsEnum td = TestUtil.Docs(Random(), mr2, "body", te2.Term(), MultiFields.GetLiveDocs
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
			//HM:revisit 
			//assert to ensure that we got some docs and to ensure that
			// nothing is eliminated by hotspot
			IsTrue(ret > 0);
			readers1[0].Close();
			readers1[1].Close();
			readers2[0].Close();
			readers2[1].Close();
			readers2[2].Close();
			ramDir1.Close();
			ramDir2.Close();
			ramDir3.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void AddDoc(Random random, Directory ramDir1, string s, bool create)
		{
			IndexWriter iw = new IndexWriter(ramDir1, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(random)).SetOpenMode(create ? IndexWriterConfig.OpenMode.CREATE
				 : IndexWriterConfig.OpenMode.APPEND));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewTextField("body", s, Field.Store.NO));
			iw.AddDocument(doc);
			iw.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestIsCurrent()
		{
			Directory d = NewDirectory();
			IndexWriter writer = new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			AddDocumentWithFields(writer);
			writer.Close();
			// set up reader:
			DirectoryReader reader = DirectoryReader.Open(d);
			IsTrue(reader.IsCurrent());
			// modify index by adding another document:
			writer = new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND));
			AddDocumentWithFields(writer);
			writer.Close();
			IsFalse(reader.IsCurrent());
			// re-create index:
			writer = new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetOpenMode(IndexWriterConfig.OpenMode.CREATE));
			AddDocumentWithFields(writer);
			writer.Close();
			IsFalse(reader.IsCurrent());
			reader.Close();
			d.Close();
		}

		/// <summary>Tests the IndexReader.getFieldNames implementation</summary>
		/// <exception cref="System.Exception">on error</exception>
		public virtual void TestGetFieldNames()
		{
			Directory d = NewDirectory();
			// set up writer
			IndexWriter writer = new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			FieldType customType3 = new FieldType();
			customType3.SetStored(true);
			doc.Add(new StringField("keyword", "test1", Field.Store.YES));
			doc.Add(new TextField("text", "test1", Field.Store.YES));
			doc.Add(new Field("unindexed", "test1", customType3));
			doc.Add(new TextField("unstored", "test1", Field.Store.NO));
			writer.AddDocument(doc);
			writer.Close();
			// set up reader
			DirectoryReader reader = DirectoryReader.Open(d);
			FieldInfos fieldInfos = MultiFields.GetMergedFieldInfos(reader);
			IsNotNull(fieldInfos.FieldInfo("keyword"));
			IsNotNull(fieldInfos.FieldInfo("text"));
			IsNotNull(fieldInfos.FieldInfo("unindexed"));
			IsNotNull(fieldInfos.FieldInfo("unstored"));
			reader.Close();
			// add more documents
			writer = new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND).SetMergePolicy(NewLogMergePolicy
				()));
			// want to get some more segments here
			int mergeFactor = ((LogMergePolicy)writer.GetConfig().GetMergePolicy()).GetMergeFactor
				();
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
			writer.Close();
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
				allFieldNames.AddItem(name);
				if (fieldInfo.IsIndexed())
				{
					indexedFieldNames.AddItem(name);
				}
				else
				{
					notIndexedFieldNames.AddItem(name);
				}
				if (fieldInfo.HasVectors())
				{
					tvFieldNames.AddItem(name);
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
			AreEqual(11, indexedFieldNames.Count);
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
			AreEqual(2, notIndexedFieldNames.Count);
			// the following fields
			IsTrue(notIndexedFieldNames.Contains("unindexed"));
			IsTrue(notIndexedFieldNames.Contains("unindexed2"));
			// verify index term vector fields  
			AreEqual(tvFieldNames.ToString(), 4, tvFieldNames.Count);
			// 4 field has term vector only
			IsTrue(tvFieldNames.Contains("termvector"));
			reader.Close();
			d.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestTermVectors()
		{
			Directory d = NewDirectory();
			// set up writer
			IndexWriter writer = new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy()));
			// want to get some more segments here
			// new termvector fields
			int mergeFactor = ((LogMergePolicy)writer.GetConfig().GetMergePolicy()).GetMergeFactor
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
			writer.Close();
			d.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void AssertTermDocsCount(string msg, IndexReader reader, Term term
			, int expected)
		{
			DocsEnum tdocs = TestUtil.Docs(Random(), reader, term.Field(), new BytesRef(term.
				Text()), MultiFields.GetLiveDocs(reader), null, 0);
			int count = 0;
			if (tdocs != null)
			{
				while (tdocs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
				{
					count++;
				}
			}
			AreEqual(msg + ", count mismatch", expected, count);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestBinaryFields()
		{
			Directory dir = NewDirectory();
			byte[] bin = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy()));
			for (int i = 0; i < 10; i++)
			{
				AddDoc(writer, "document number " + (i + 1));
				AddDocumentWithFields(writer);
				AddDocumentWithDifferentFields(writer);
				AddDocumentWithTermVectorFields(writer);
			}
			writer.Close();
			writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND).SetMergePolicy(NewLogMergePolicy
				()));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new StoredField("bin1", bin));
			doc.Add(new TextField("junk", "junk text", Field.Store.NO));
			writer.AddDocument(doc);
			writer.Close();
			DirectoryReader reader = DirectoryReader.Open(dir);
			Lucene.Net.Documents.Document doc2 = reader.Document(reader.MaxDoc - 1);
			IIndexableField[] fields = doc2.GetFields("bin1");
			IsNotNull(fields);
			AreEqual(1, fields.Length);
			IIndexableField b1 = fields[0];
			IsTrue(b1.BinaryValue() != null);
			BytesRef bytesRef = b1.BinaryValue();
			AreEqual(bin.Length, bytesRef.length);
			for (int i_1 = 0; i_1 < bin.Length; i_1++)
			{
				AreEqual(bin[i_1], bytesRef.bytes[i_1 + bytesRef.offset]);
			}
			reader.Close();
			// force merge
			writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND).SetMergePolicy(NewLogMergePolicy
				()));
			writer.ForceMerge(1);
			writer.Close();
			reader = DirectoryReader.Open(dir);
			doc2 = reader.Document(reader.MaxDoc - 1);
			fields = doc2.GetFields("bin1");
			IsNotNull(fields);
			AreEqual(1, fields.Length);
			b1 = fields[0];
			IsTrue(b1.BinaryValue() != null);
			bytesRef = b1.BinaryValue();
			AreEqual(bin.Length, bytesRef.length);
			for (int i_2 = 0; i_2 < bin.Length; i_2++)
			{
				AreEqual(bin[i_2], bytesRef.bytes[i_2 + bytesRef.offset]);
			}
			reader.Close();
			dir.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestFilesOpenClose()
		{
			// Create initial data set
			FilePath dirFile = CreateTempDir("TestIndexReader.testFilesOpenClose");
			Directory dir = NewFSDirectory(dirFile);
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			AddDoc(writer, "test");
			writer.Close();
			dir.Close();
			// Try to erase the data - this ensures that the writer closed all files
			TestUtil.Rm(dirFile);
			dir = NewFSDirectory(dirFile);
			// Now create the data set again, just as before
			writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetOpenMode(IndexWriterConfig.OpenMode.CREATE));
			AddDoc(writer, "test");
			writer.Close();
			dir.Close();
			// Now open existing directory and test that reader closes all files
			dir = NewFSDirectory(dirFile);
			DirectoryReader reader1 = DirectoryReader.Open(dir);
			reader1.Close();
			dir.Close();
			// The following will fail if reader did not close
			// all files
			TestUtil.Rm(dirFile);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestOpenReaderAfterDelete()
		{
			FilePath dirFile = CreateTempDir("deletetest");
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
			dir.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal static void AddDocumentWithFields(IndexWriter writer)
		{
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			FieldType customType3 = new FieldType();
			customType3.SetStored(true);
			doc.Add(NewStringField("keyword", "test1", Field.Store.YES));
			doc.Add(NewTextField("text", "test1", Field.Store.YES));
			doc.Add(NewField("unindexed", "test1", customType3));
			doc.Add(new TextField("unstored", "test1", Field.Store.NO));
			writer.AddDocument(doc);
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal static void AddDocumentWithDifferentFields(IndexWriter writer)
		{
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			FieldType customType3 = new FieldType();
			customType3.SetStored(true);
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
			AreEqual("IndexReaders have different values for numDocs."
				, index1.NumDocs(), index2.NumDocs());
			AreEqual("IndexReaders have different values for maxDoc.", 
				index1.MaxDoc, index2.MaxDoc);
			AreEqual("Only one IndexReader has deletions.", index1.HasDeletions
				(), index2.HasDeletions());
			AreEqual("Single segment test differs.", index1.Leaves().Count
				 == 1, index2.Leaves().Count == 1);
			// check field names
			FieldInfos fieldInfos1 = MultiFields.GetMergedFieldInfos(index1);
			FieldInfos fieldInfos2 = MultiFields.GetMergedFieldInfos(index2);
			AreEqual("IndexReaders have different numbers of fields.", 
				fieldInfos1.Size(), fieldInfos2.Size());
			int numFields = fieldInfos1.Size();
			for (int fieldID = 0; fieldID < numFields; fieldID++)
			{
				FieldInfo fieldInfo1 = fieldInfos1.FieldInfo(fieldID);
				FieldInfo fieldInfo2 = fieldInfos2.FieldInfo(fieldID);
				AreEqual("Different field names.", fieldInfo1.name, fieldInfo2
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
						AreEqual("Norm different for doc " + i + " and field '" + 
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
			Bits liveDocs1 = MultiFields.GetLiveDocs(index1);
			Bits liveDocs2 = MultiFields.GetLiveDocs(index2);
			for (int i_1 = 0; i_1 < index1.MaxDoc; i_1++)
			{
				AreEqual("Doc " + i_1 + " only deleted in one index.", liveDocs1
					 == null || !liveDocs1.Get(i_1), liveDocs2 == null || !liveDocs2.Get(i_1));
			}
			// check stored fields
			for (int i_2 = 0; i_2 < index1.MaxDoc; i_2++)
			{
				if (liveDocs1 == null || liveDocs1.Get(i_2))
				{
					Lucene.Net.Documents.Document doc1 = index1.Document(i_2);
					Lucene.Net.Documents.Document doc2 = index2.Document(i_2);
					IList<IIndexableField> field1 = doc1.GetFields();
					IList<IIndexableField> field2 = doc2.GetFields();
					AreEqual("Different numbers of fields for doc " + i_2 + "."
						, field1.Count, field2.Count);
					Iterator<IIndexableField> itField1 = field1.Iterator();
					Iterator<IIndexableField> itField2 = field2.Iterator();
					while (itField1.HasNext())
					{
						Field curField1 = (Field)itField1.Next();
						Field curField2 = (Field)itField2.Next();
						AreEqual("Different fields names for doc " + i_2 + ".", curField1
							.Name(), curField2.Name());
						AreEqual("Different field values for doc " + i_2 + ".", curField1
							.StringValue = ), curField2.StringValue = ));
					}
				}
			}
			// check dictionary and posting lists
			Fields fields1 = MultiFields.GetFields(index1);
			Fields fields2 = MultiFields.GetFields(index2);
			Iterator<string> fenum2 = fields2.Iterator();
			Bits liveDocs = MultiFields.GetLiveDocs(index1);
			foreach (string field1_1 in fields1)
			{
				AreEqual("Different fields", field1_1, fenum2.Next());
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
					AreEqual("Different terms", enum1.Term(), enum2.Next());
					DocsAndPositionsEnum tp1 = enum1.DocsAndPositions(liveDocs, null);
					DocsAndPositionsEnum tp2 = enum2.DocsAndPositions(liveDocs, null);
					while (tp1.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
					{
						IsTrue(tp2.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
						AreEqual("Different doc id in postinglist of term " + enum1
							.Term() + ".", tp1.DocID, tp2.DocID);
						AreEqual("Different term frequence in postinglist of term "
							 + enum1.Term() + ".", tp1.Freq, tp2.Freq);
						for (int i = 0; i_2 < tp1.Freq; i_2++)
						{
							AreEqual("Different positions in postinglist of term " + enum1
								.Term() + ".", tp1.NextPosition(), tp2.NextPosition());
						}
					}
				}
			}
			IsFalse(fenum2.HasNext());
		}

		/// <exception cref="System.IO.IOException"></exception>
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
			writer.Close();
			SegmentInfos sis = new SegmentInfos();
			sis.Read(d);
			DirectoryReader r = DirectoryReader.Open(d);
			IndexCommit c = r.GetIndexCommit();
			AreEqual(sis.GetSegmentsFileName(), c.GetSegmentsFileName(
				));
			IsTrue(c.Equals(r.GetIndexCommit()));
			// Change the index
			writer = new IndexWriter(d, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND).SetMaxBufferedDocs
				(2)).SetMergePolicy(NewLogMergePolicy(10)));
			for (int i_1 = 0; i_1 < 7; i_1++)
			{
				AddDocumentWithFields(writer);
			}
			writer.Close();
			DirectoryReader r2 = DirectoryReader.OpenIfChanged(r);
			IsNotNull(r2);
			IsFalse(c.Equals(r2.GetIndexCommit()));
			IsFalse(r2.GetIndexCommit().GetSegmentCount() == 1);
			r2.Close();
			writer = new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND));
			writer.ForceMerge(1);
			writer.Close();
			r2 = DirectoryReader.OpenIfChanged(r);
			IsNotNull(r2);
			IsNull(DirectoryReader.OpenIfChanged(r2));
			AreEqual(1, r2.GetIndexCommit().GetSegmentCount());
			r.Close();
			r2.Close();
			d.Close();
		}

		internal static Lucene.Net.Documents.Document CreateDocument(string id)
		{
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			FieldType customType = new FieldType(TextField.TYPE_STORED);
			customType.SetTokenized(false);
			customType.SetOmitNorms(true);
			doc.Add(NewField("id", id, customType));
			return doc;
		}

		// LUCENE-1468 -- make sure on attempting to open an
		// DirectoryReader on a non-existent directory, you get a
		// good exception
		/// <exception cref="System.Exception"></exception>
		public virtual void TestNoDir()
		{
			FilePath tempDir = CreateTempDir("doesnotexist");
			TestUtil.Rm(tempDir);
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
			dir.Close();
		}

		// LUCENE-1509
		/// <exception cref="System.Exception"></exception>
		public virtual void TestNoDupCommitFileNames()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2)));
			writer.AddDocument(CreateDocument("a"));
			writer.AddDocument(CreateDocument("a"));
			writer.AddDocument(CreateDocument("a"));
			writer.Close();
			ICollection<IndexCommit> commits = DirectoryReader.ListCommits(dir);
			foreach (IndexCommit commit in commits)
			{
				ICollection<string> files = commit.GetFileNames();
				HashSet<string> seen = new HashSet<string>();
				foreach (string fileName in files)
				{
					IsTrue("file " + fileName + " was duplicated", !seen.Contains
						(fileName));
					seen.AddItem(fileName);
				}
			}
			dir.Close();
		}

		// LUCENE-1579: Ensure that on a reopened reader, that any
		// shared segments reuse the doc values arrays in
		// FieldCache
		/// <exception cref="System.Exception"></exception>
		public virtual void TestFieldCacheReuseAfterReopen()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy(10)));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewStringField("number", "17", Field.Store.NO));
			writer.AddDocument(doc);
			writer.Commit();
			// Open reader1
			DirectoryReader r = DirectoryReader.Open(dir);
			AtomicReader r1 = GetOnlySegmentReader(r);
			FieldCache.Ints ints = FieldCache.DEFAULT.GetInts(r1, "number", false);
			AreEqual(17, ints.Get(0));
			// Add new segment
			writer.AddDocument(doc);
			writer.Commit();
			// Reopen reader1 --> reader2
			DirectoryReader r2 = DirectoryReader.OpenIfChanged(r);
			IsNotNull(r2);
			r.Close();
			AtomicReader sub0 = ((AtomicReader)r2.Leaves()[0].Reader());
			FieldCache.Ints ints2 = FieldCache.DEFAULT.GetInts(sub0, "number", false);
			r2.Close();
			IsTrue(ints == ints2);
			writer.Close();
			dir.Close();
		}

		// LUCENE-1586: getUniqueTermCount
		/// <exception cref="System.Exception"></exception>
		public virtual void TestUniqueTermCount()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewTextField("field", "a b c d e f g h i j k l m n o p q r s t u v w x y z"
				, Field.Store.NO));
			doc.Add(NewTextField("number", "0 1 2 3 4 5 6 7 8 9", Field.Store.NO));
			writer.AddDocument(doc);
			writer.AddDocument(doc);
			writer.Commit();
			DirectoryReader r = DirectoryReader.Open(dir);
			AtomicReader r1 = GetOnlySegmentReader(r);
			AreEqual(36, r1.Fields().GetUniqueTermCount());
			writer.AddDocument(doc);
			writer.Commit();
			DirectoryReader r2 = DirectoryReader.OpenIfChanged(r);
			IsNotNull(r2);
			r.Close();
			foreach (AtomicReaderContext s in r2.Leaves())
			{
				AreEqual(36, ((AtomicReader)s.Reader()).Fields().GetUniqueTermCount
					());
			}
			r2.Close();
			writer.Close();
			dir.Close();
		}

		// LUCENE-1609: don't load terms index
		/// <exception cref="System.Exception"></exception>
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
			writer.Close();
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
			AreEqual(-1, ((SegmentReader)((AtomicReader)r.Leaves()[0].
				Reader())).GetTermInfosIndexDivisor());
			writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetCodec(TestUtil.AlwaysPostingsFormat(new Lucene41PostingsFormat())
				).SetMergePolicy(NewLogMergePolicy(10)));
			writer.AddDocument(doc);
			writer.Close();
			// LUCENE-1718: ensure re-open carries over no terms index:
			DirectoryReader r2 = DirectoryReader.OpenIfChanged(r);
			IsNotNull(r2);
			IsNull(DirectoryReader.OpenIfChanged(r2));
			r.Close();
			IList<AtomicReaderContext> leaves = r2.Leaves();
			AreEqual(2, leaves.Count);
			foreach (AtomicReaderContext ctx in leaves)
			{
				try
				{
					((AtomicReader)ctx.Reader()).DocFreq(new Term("field", "f"));
					Fail("did not hit expected exception");
				}
				catch (InvalidOperationException)
				{
				}
			}
			// expected
			r2.Close();
			dir.Close();
		}

		// LUCENE-2046
		/// <exception cref="System.Exception"></exception>
		public virtual void TestPrepareCommitIsCurrent()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			writer.Commit();
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			writer.AddDocument(doc);
			DirectoryReader r = DirectoryReader.Open(dir);
			IsTrue(r.IsCurrent());
			writer.AddDocument(doc);
			writer.PrepareCommit();
			IsTrue(r.IsCurrent());
			DirectoryReader r2 = DirectoryReader.OpenIfChanged(r);
			IsNull(r2);
			writer.Commit();
			IsFalse(r.IsCurrent());
			writer.Close();
			r.Close();
			dir.Close();
		}

		// LUCENE-2753
		/// <exception cref="System.Exception"></exception>
		public virtual void TestListCommits()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, null).SetIndexDeletionPolicy(new SnapshotDeletionPolicy(new KeepOnlyLastCommitDeletionPolicy
				())));
			SnapshotDeletionPolicy sdp = (SnapshotDeletionPolicy)writer.GetConfig().GetIndexDeletionPolicy
				();
			writer.AddDocument(new Lucene.Net.Documents.Document());
			writer.Commit();
			sdp.Snapshot();
			writer.AddDocument(new Lucene.Net.Documents.Document());
			writer.Commit();
			sdp.Snapshot();
			writer.AddDocument(new Lucene.Net.Documents.Document());
			writer.Commit();
			sdp.Snapshot();
			writer.Close();
			long currentGen = 0;
			foreach (IndexCommit ic in DirectoryReader.ListCommits(dir))
			{
				IsTrue("currentGen=" + currentGen + " commitGen=" + ic.GetGeneration
					(), currentGen < ic.GetGeneration());
				currentGen = ic.GetGeneration();
			}
			dir.Close();
		}

		// Make sure totalTermFreq works correctly in the terms
		// dict cache
		/// <exception cref="System.Exception"></exception>
		public virtual void TestTotalTermFreqCached()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
			d.Add(NewTextField("f", "a a b", Field.Store.NO));
			writer.AddDocument(d);
			DirectoryReader r = writer.GetReader();
			writer.Close();
			try
			{
				// Make sure codec impls totalTermFreq (eg PreFlex doesn't)
				Assume.AssumeTrue(r.TotalTermFreq(new Term("f", new BytesRef("b"))) != -1);
				AreEqual(1, r.TotalTermFreq(new Term("f", new BytesRef("b"
					))));
				AreEqual(2, r.TotalTermFreq(new Term("f", new BytesRef("a"
					))));
				AreEqual(1, r.TotalTermFreq(new Term("f", new BytesRef("b"
					))));
			}
			finally
			{
				r.Close();
				dir.Close();
			}
		}

		/// <exception cref="System.Exception"></exception>
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
			DirectoryReader r = writer.GetReader();
			writer.Close();
			try
			{
				// Make sure codec impls getSumDocFreq (eg PreFlex doesn't)
				Assume.AssumeTrue(r.GetSumDocFreq("f") != -1);
				AreEqual(2, r.GetSumDocFreq("f"));
			}
			finally
			{
				r.Close();
				dir.Close();
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestGetDocCount()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
			d.Add(NewTextField("f", "a", Field.Store.NO));
			writer.AddDocument(d);
			d = new Lucene.Net.Documents.Document();
			d.Add(NewTextField("f", "a", Field.Store.NO));
			writer.AddDocument(d);
			DirectoryReader r = writer.GetReader();
			writer.Close();
			try
			{
				// Make sure codec impls getSumDocFreq (eg PreFlex doesn't)
				Assume.AssumeTrue(r.GetDocCount("f") != -1);
				AreEqual(2, r.GetDocCount("f"));
			}
			finally
			{
				r.Close();
				dir.Close();
			}
		}

		/// <exception cref="System.Exception"></exception>
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
			DirectoryReader r = writer.GetReader();
			writer.Close();
			try
			{
				// Make sure codec impls getSumDocFreq (eg PreFlex doesn't)
				Assume.AssumeTrue(r.GetSumTotalTermFreq("f") != -1);
				AreEqual(6, r.GetSumTotalTermFreq("f"));
			}
			finally
			{
				r.Close();
				dir.Close();
			}
		}

		// LUCENE-2474
		/// <exception cref="System.Exception"></exception>
		public virtual void TestReaderFinishedListener()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy()));
			((LogMergePolicy)writer.GetConfig().GetMergePolicy()).SetMergeFactor(3);
			writer.AddDocument(new Lucene.Net.Documents.Document());
			writer.Commit();
			writer.AddDocument(new Lucene.Net.Documents.Document());
			writer.Commit();
			DirectoryReader reader = writer.GetReader();
			int[] closeCount = new int[1];
			IndexReader.ReaderClosedListener listener = new _ReaderClosedListener_1016(closeCount
				);
			reader.AddReaderClosedListener(listener);
			reader.Close();
			// Close the top reader, its the only one that should be closed
			AreEqual(1, closeCount[0]);
			writer.Close();
			DirectoryReader reader2 = DirectoryReader.Open(dir);
			reader2.AddReaderClosedListener(listener);
			closeCount[0] = 0;
			reader2.Close();
			AreEqual(1, closeCount[0]);
			dir.Close();
		}

		private sealed class _ReaderClosedListener_1016 : IndexReader.ReaderClosedListener
		{
			public _ReaderClosedListener_1016(int[] closeCount)
			{
				this.closeCount = closeCount;
			}

			public void OnClose(IndexReader reader)
			{
				closeCount[0]++;
			}

			private readonly int[] closeCount;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestOOBDocID()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			writer.AddDocument(new Lucene.Net.Documents.Document());
			DirectoryReader r = writer.GetReader();
			writer.Close();
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
			r.Close();
			dir.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
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
			r.Close();
			IsFalse(r.TryIncRef());
			writer.Close();
			dir.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestStressTryIncRef()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			writer.AddDocument(new Lucene.Net.Documents.Document());
			writer.Commit();
			DirectoryReader r = DirectoryReader.Open(dir);
			int numThreads = AtLeast(2);
			TestDirectoryReader.IncThread[] threads = new TestDirectoryReader.IncThread[numThreads
				];
			for (int i = 0; i < threads.Length; i++)
			{
				threads[i] = new TestDirectoryReader.IncThread(r, Random());
				threads[i].Start();
			}
			Sharpen.Thread.Sleep(100);
			IsTrue(r.TryIncRef());
			r.DecRef();
			r.Close();
			for (int i_1 = 0; i_1 < threads.Length; i_1++)
			{
				threads[i_1].Join();
				IsNull(threads[i_1].failed);
			}
			IsFalse(r.TryIncRef());
			writer.Close();
			dir.Close();
		}

		internal class IncThread : Sharpen.Thread
		{
			internal readonly IndexReader toInc;

			internal readonly Random random;

			internal Exception failed;

			internal IncThread(IndexReader toInc, Random random)
			{
				this.toInc = toInc;
				this.random = random;
			}

			public override void Run()
			{
				try
				{
					while (toInc.TryIncRef())
					{
						IsFalse(toInc.HasDeletions());
						toInc.DecRef();
					}
					IsFalse(toInc.TryIncRef());
				}
				catch (Exception e)
				{
					failed = e;
				}
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestLoadCertainFields()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewStringField("field1", "foobar", Field.Store.YES));
			doc.Add(NewStringField("field2", "foobaz", Field.Store.YES));
			writer.AddDocument(doc);
			DirectoryReader r = writer.GetReader();
			writer.Close();
			ICollection<string> fieldsToLoad = new HashSet<string>();
			AreEqual(0, r.Document(0, fieldsToLoad).GetFields().Count);
			fieldsToLoad.AddItem("field1");
			Lucene.Net.Documents.Document doc2 = r.Document(0, fieldsToLoad);
			AreEqual(1, doc2.GetFields().Count);
			AreEqual("foobar", doc2.Get("field1"));
			r.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		[Obsolete]
		[System.ObsoleteAttribute(@"just to ensure IndexReader static methods work")]
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
			AreEqual(1, r.DocFreq(new Term("field", "f")));
			r.Close();
			writer.AddDocument(doc);
			writer.Close();
			// open(Directory)
			r = IndexReader.Open(dir);
			AreEqual(2, r.DocFreq(new Term("field", "f")));
			r.Close();
			// open(IndexCommit)
			IList<IndexCommit> commits = DirectoryReader.ListCommits(dir);
			AreEqual(1, commits.Count);
			r = IndexReader.Open(commits[0]);
			AreEqual(2, r.DocFreq(new Term("field", "f")));
			r.Close();
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
			AreEqual(-1, ((SegmentReader)((AtomicReader)r.Leaves()[0].
				Reader())).GetTermInfosIndexDivisor());
			r.Close();
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
			AreEqual(-1, ((SegmentReader)((AtomicReader)r.Leaves()[0].
				Reader())).GetTermInfosIndexDivisor());
			r.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestIndexExistsOnNonExistentDirectory()
		{
			FilePath tempDir = CreateTempDir("testIndexExistsOnNonExistentDirectory");
			tempDir.Delete();
			Directory dir = NewFSDirectory(tempDir);
			System.Console.Out.WriteLine("dir=" + dir);
			IsFalse(DirectoryReader.IndexExists(dir));
			dir.Close();
		}
	}
}
