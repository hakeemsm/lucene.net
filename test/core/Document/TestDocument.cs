/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using Lucene.Net.Index;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Documents
{	
	/// <summary>Tests {@link Document} class.</summary>	
	public class TestDocument:LuceneTestCase
	{		
		internal System.String binaryVal = "this text will be stored as a byte array in the index";
		internal System.String binaryVal2 = "this text will be also stored as a byte array in the index";

	    [Test]
		public virtual void TestBinaryField()
	    {
	        Document doc = new Document();

	        FieldType ft = new FieldType();
	        ft.Stored = true;
	        IIndexableField stringFld = new Field("string", binaryVal, ft);
	        IIndexableField binaryFld = new StoredField("binary", binaryVal.getBytes("UTF-8"));
	        IIndexableField binaryFld2 = new StoredField("binary", binaryVal2.getBytes("UTF-8"));

	        doc.Add(stringFld);
	        doc.Add(binaryFld);

	        assertEquals(2, doc.GetFields().Count);

	        assertTrue(binaryFld.BinaryValue != null);
	        assertTrue(binaryFld.FieldTypeValue.Stored);
	        assertFalse(binaryFld.FieldTypeValue.Indexed);

	        String binaryTest = doc.GetBinaryValue("binary").Utf8ToString();
	        assertTrue(binaryTest.equals(binaryVal));

	        String stringTest = doc.Get("string");
	        assertTrue(binaryTest.equals(stringTest));

	        doc.Add(binaryFld2);

	        assertEquals(3, doc.GetFields().Count);

	        BytesRef[] binaryTests = doc.GetBinaryValues("binary");

	        assertEquals(2, binaryTests.Length);

	        binaryTest = binaryTests[0].Utf8ToString();
	        String binaryTest2 = binaryTests[1].Utf8ToString();

	        assertFalse(binaryTest.equals(binaryTest2));

	        assertTrue(binaryTest.equals(binaryVal));
	        assertTrue(binaryTest2.equals(binaryVal2));

	        doc.RemoveField("string");
	        assertEquals(2, doc.GetFields().Count);

	        doc.RemoveFields("binary");
	        assertEquals(0, doc.GetFields().Count);
	    }

	    /// <summary> Tests {@link Document#RemoveField(String)} method for a brand new Document
	    /// that has not been indexed yet.
	    /// 
	    /// </summary>
	    /// <throws>  Exception on error </throws>
	    [Test]
		public virtual void TestRemoveForNewDocument()
	    {
	        Document doc = MakeDocumentWithFields();
	        assertEquals(8, doc.GetFields().size());
	        doc.RemoveFields("keyword");
	        assertEquals(6, doc.GetFields().size());
	        doc.RemoveFields("doesnotexists"); // removing non-existing fields is
	        // siltenlty ignored
	        doc.RemoveFields("keyword"); // removing a field more than once
	        assertEquals(6, doc.GetFields().size());
	        doc.RemoveField("text");
	        assertEquals(5, doc.GetFields().size());
	        doc.RemoveField("text");
	        assertEquals(4, doc.GetFields().size());
	        doc.RemoveField("text");
	        assertEquals(4, doc.GetFields().size());
	        doc.RemoveField("doesnotexists"); // removing non-existing fields is
	        // siltenlty ignored
	        assertEquals(4, doc.GetFields().size());
	        doc.RemoveFields("unindexed");
	        assertEquals(2, doc.GetFields().size());
	        doc.RemoveFields("unstored");
	        assertEquals(0, doc.GetFields().size());
	        doc.RemoveFields("doesnotexists"); // removing non-existing fields is
	        // siltenlty ignored
			AreEqual(2, doc.GetFields().Count);
			doc.RemoveFields("indexed_not_tokenized");
	        assertEquals(0, doc.GetFields().size());
	    }

	    [Test]
		public virtual void TestConstructorExceptions()
        {
            FieldType ft = new FieldType();
            ft.Stored = true;
            new Field("name", "value", ft); // okay
            new StringField("name", "value", Field.Store.NO); // okay
            try
            {
                new Field("name", "value", new FieldType());
                fail();
            }
            catch (ArgumentException e)
            {
                // expected exception
            }
            new Field("name", "value", ft); // okay
            try
            {
                FieldType ft2 = new FieldType();
                ft2.Stored = true;
                ft2.StoreTermVectors = true;
                new Field("name", "value", ft2);
                fail();
            }
            catch (ArgumentException e)
            {
                // expected exception
            }
        }
		
		/// <summary> Tests {@link Document#GetValues(String)} method for a brand new Document
		/// that has not been indexed yet.
		/// 
		/// </summary>
		/// <throws>  Exception on error </throws>
		[Test]
		public virtual void TestGetValuesForNewDocument()
		{
			DoAssert(MakeDocumentWithFields(), false);
		}

	    /// <summary> Tests {@link Document#GetValues(String)} method for a Document retrieved from
	    /// an index.
	    /// 
	    /// </summary>
	    /// <throws>  Exception on error </throws>
//	    [Test]
//	    public void testGetValuesForIndexedDocument()
//	    {
//	        Directory dir = newDirectory();
//	        RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
//	        writer.addDocument(makeDocumentWithFields());
//	        IndexReader reader = writer.getReader();
//
//	        IndexSearcher searcher = newSearcher(reader);
//
//	        // search for something that does exists
//	        Query query = new TermQuery(new Term("keyword", "test1"));
//
//	        // ensure that queries return expected results without DateFilter first
//	        ScoreDoc[] hits = searcher.search(query, null, 1000).ScoreDocs;
//	        assertEquals(1, hits.length);
//
//	        doAssert(searcher.Doc(hits[0].Doc), true);
//	        writer.close();
//	        reader.close();
//	        dir.close();
//	    }

		public virtual void TestGetValues()
		{
			Lucene.Net.Documents.Document doc = MakeDocumentWithFields();
			AreEqual(new string[] { "test1", "test2" }, doc.GetValues(
				"keyword"));
			AreEqual(new string[] { "test1", "test2" }, doc.GetValues(
				"text"));
			AreEqual(new string[] { "test1", "test2" }, doc.GetValues(
				"unindexed"));
			AreEqual(new string[0], doc.GetValues("nope"));
		}
		public virtual void TestPositionIncrementMultiFields()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			writer.AddDocument(MakeDocumentWithFields());
			IndexReader reader = writer.Reader;
			IndexSearcher searcher = NewSearcher(reader);
			PhraseQuery query = new PhraseQuery();
			query.Add(new Term("indexed_not_tokenized", "test1"));
			query.Add(new Term("indexed_not_tokenized", "test2"));
			ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
			AreEqual(1, hits.Length);
			DoAssert(searcher.Doc(hits[0].Doc), true);
			writer.Dispose();
			reader.Dispose();
			dir.Dispose();
		}
		private Lucene.Net.Documents.Document MakeDocumentWithFields()
		{
            Document doc = new Document();
            FieldType stored = new FieldType();
            stored.Stored = true;
			FieldType indexedNotTokenized = new FieldType();
			indexedNotTokenized.Indexed(true);
			indexedNotTokenized.Tokenized = (false);
            doc.Add(new StringField("keyword", "test1", Field.Store.YES));
            doc.Add(new StringField("keyword", "test2", Field.Store.YES));
            doc.Add(new TextField("text", "test1", Field.Store.YES));
            doc.Add(new TextField("text", "test2", Field.Store.YES));
            doc.Add(new Field("unindexed", "test1", stored));
            doc.Add(new Field("unindexed", "test2", stored));
            doc
                .Add(new TextField("unstored", "test1", Field.Store.NO));
            doc
                .Add(new TextField("unstored", "test2", Field.Store.NO));
			doc.Add(new Field("indexed_not_tokenized", "test1", indexedNotTokenized));
			doc.Add(new Field("indexed_not_tokenized", "test2", indexedNotTokenized));
            return doc;
		}

		private void DoAssert(Lucene.Net.Documents.Document doc, bool fromIndex)
        {
            IIndexableField[] keywordFieldValues = doc.GetFields("keyword");
            IIndexableField[] textFieldValues = doc.GetFields("text");
            IIndexableField[] unindexedFieldValues = doc.GetFields("unindexed");
            IIndexableField[] unstoredFieldValues = doc.GetFields("unstored");

            assertTrue(keywordFieldValues.Length == 2);
            assertTrue(textFieldValues.Length == 2);
            assertTrue(unindexedFieldValues.Length == 2);
            // this test cannot work for documents retrieved from the index
            // since unstored fields will obviously not be returned
            if (!fromIndex)
            {
                assertTrue(unstoredFieldValues.Length == 2);
            }

            assertTrue(keywordFieldValues[0].StringValue.equals("test1"));
            assertTrue(keywordFieldValues[1].StringValue.equals("test2"));
            assertTrue(textFieldValues[0].StringValue.equals("test1"));
            assertTrue(textFieldValues[1].StringValue.equals("test2"));
            assertTrue(unindexedFieldValues[0].StringValue.equals("test1"));
            assertTrue(unindexedFieldValues[1].StringValue.equals("test2"));
            // this test cannot work for documents retrieved from the index
            // since unstored fields will obviously not be returned
            if (!fromIndex)
            {
                assertTrue(unstoredFieldValues[0].StringValue.equals("test1"));
                assertTrue(unstoredFieldValues[1].StringValue.equals("test2"));
            }
        }

//	    [Test]
//	    public void testFieldSetValue()
//	    {
//
//	        Field field = new StringField("id", "id1", Field.Store.YES);
//	        Document doc = new Document();
//	        doc.Add(field);
//	        doc.Add(new StringField("keyword", "test", Field.Store.YES));
//
//	        Directory dir = newDirectory();
//	        RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
//	        writer.addDocument(doc);
//	        field.setStringValue("id2");
//	        writer.addDocument(doc);
//	        field.setStringValue("id3");
//	        writer.addDocument(doc);
//
//	        IndexReader reader = writer.getReader();
//	        IndexSearcher searcher = newSearcher(reader);
//
//	        Query query = new TermQuery(new Term("keyword", "test"));
//
//	        // ensure that queries return expected results without DateFilter first
//	        ScoreDoc[] hits = searcher.search(query, null, 1000).ScoreDocs;
//	        assertEquals(3, hits.length);
//	        int result = 0;
//	        for (int i = 0; i < 3; i++)
//	        {
//	            Document doc2 = searcher.Doc(hits[i].Doc);
//	            Field f = (Field) doc2.getField("id");
//	            if (f.stringValue().equals("id1")) result |= 1;
//	            else if (f.stringValue().equals("id2")) result |= 2;
//	            else if (f.stringValue().equals("id3")) result |= 4;
//	            else fail("unexpected id field");
//	        }
//	        writer.close();
//	        reader.close();
//	        dir.close();
//	        assertEquals("did not see all IDs", 7, result);
//	    }

		public virtual void TestInvalidFields()
		{
			try
			{
				new Field("foo", new MockTokenizer(new StringReader(string.Empty)), StringField.TYPE_STORED
					);
				Fail("did not hit expected exc");
			}
			catch (ArgumentException)
			{
			}
		}
		public virtual void TestTransitionAPI()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new Field("stored", "abc", Field.Store.YES, Field.Index.NO));
			doc.Add(new Field("stored_indexed", "abc xyz", Field.Store.YES, Field.Index.NOT_ANALYZED
				));
			doc.Add(new Field("stored_tokenized", "abc xyz", Field.Store.YES, Field.Index.ANALYZED
				));
			doc.Add(new Field("indexed", "abc xyz", Field.Store.NO, Field.Index.NOT_ANALYZED)
				);
			doc.Add(new Field("tokenized", "abc xyz", Field.Store.NO, Field.Index.ANALYZED));
			doc.Add(new Field("tokenized_reader", new StringReader("abc xyz")));
			doc.Add(new Field("tokenized_tokenstream", w.w.GetAnalyzer().TokenStream("tokenized_tokenstream"
				, new StringReader("abc xyz"))));
			doc.Add(new Field("binary", new byte[10]));
			doc.Add(new Field("tv", "abc xyz", Field.Store.NO, Field.Index.ANALYZED, Field.TermVector
				.YES));
			doc.Add(new Field("tv_pos", "abc xyz", Field.Store.NO, Field.Index.ANALYZED, Field.TermVector
				.WITH_POSITIONS));
			doc.Add(new Field("tv_off", "abc xyz", Field.Store.NO, Field.Index.ANALYZED, Field.TermVector
				.WITH_OFFSETS));
			doc.Add(new Field("tv_pos_off", "abc xyz", Field.Store.NO, Field.Index.ANALYZED, 
				Field.TermVector.WITH_POSITIONS_OFFSETS));
			w.AddDocument(doc);
			IndexReader r = w.Reader;
			w.Dispose();
			doc = r.Document(0);
			// 4 stored fields
			AreEqual(4, doc.GetFields().Count);
			AreEqual("abc", doc.Get("stored"));
			AreEqual("abc xyz", doc.Get("stored_indexed"));
			AreEqual("abc xyz", doc.Get("stored_tokenized"));
			BytesRef br = doc.GetBinaryValue("binary");
			IsNotNull(br);
			AreEqual(10, br.length);
			IndexSearcher s = new IndexSearcher(r);
			AreEqual(1, s.Search(new TermQuery(new Term("stored_indexed"
				, "abc xyz")), 1).TotalHits);
			AreEqual(1, s.Search(new TermQuery(new Term("stored_tokenized"
				, "abc")), 1).TotalHits);
			AreEqual(1, s.Search(new TermQuery(new Term("stored_tokenized"
				, "xyz")), 1).TotalHits);
			AreEqual(1, s.Search(new TermQuery(new Term("indexed", "abc xyz"
				)), 1).TotalHits);
			AreEqual(1, s.Search(new TermQuery(new Term("tokenized", "abc"
				)), 1).TotalHits);
			AreEqual(1, s.Search(new TermQuery(new Term("tokenized", "xyz"
				)), 1).TotalHits);
			AreEqual(1, s.Search(new TermQuery(new Term("tokenized_reader"
				, "abc")), 1).TotalHits);
			AreEqual(1, s.Search(new TermQuery(new Term("tokenized_reader"
				, "xyz")), 1).TotalHits);
			AreEqual(1, s.Search(new TermQuery(new Term("tokenized_tokenstream"
				, "abc")), 1).TotalHits);
			AreEqual(1, s.Search(new TermQuery(new Term("tokenized_tokenstream"
				, "xyz")), 1).TotalHits);
			foreach (string field in new string[] { "tv", "tv_pos", "tv_off", "tv_pos_off" })
			{
				Fields tvFields = r.GetTermVectors(0);
				Terms tvs = tvFields.Terms(field);
				IsNotNull(tvs);
				AreEqual(2, tvs.Size());
				TermsEnum tvsEnum = tvs.Iterator(null);
				AreEqual(new BytesRef("abc"), tvsEnum.Next());
				DocsAndPositionsEnum dpEnum = tvsEnum.DocsAndPositions(null, null);
				if (field.Equals("tv"))
				{
					IsNull(dpEnum);
				}
				else
				{
					IsNotNull(dpEnum);
				}
				AreEqual(new BytesRef("xyz"), tvsEnum.Next());
				IsNull(tvsEnum.Next());
			}
			r.Dispose();
			dir.Dispose();
		}
		public virtual void TestNumericFieldAsString()
		{
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new IntField("int", 5, Field.Store.YES));
			AreEqual("5", doc.Get("int"));
			IsNull(doc.Get("somethingElse"));
			doc.Add(new IntField("int", 4, Field.Store.YES));
			AssertArrayEquals(new string[] { "5", "4" }, doc.GetValues("int"));
			Directory dir = NewDirectory();
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir);
			iw.AddDocument(doc);
			DirectoryReader ir = iw.Reader;
			Lucene.Net.Documents.Document sdoc = ir.Document(0);
			AreEqual("5", sdoc.Get("int"));
			IsNull(sdoc.Get("somethingElse"));
			AssertArrayEquals(new string[] { "5", "4" }, sdoc.GetValues("int"));
			ir.Dispose();
			iw.Dispose();
			dir.Dispose();
		}
	}
}