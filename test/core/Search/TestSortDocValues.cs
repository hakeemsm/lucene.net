/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search
{
	/// <summary>Tests basic sorting on docvalues fields.</summary>
	/// <remarks>
	/// Tests basic sorting on docvalues fields.
	/// These are mostly like TestSort's tests, except each test
	/// indexes the field up-front as docvalues, and checks no fieldcaches were made
	/// </remarks>
	public class TestSortDocValues : LuceneTestCase
	{
		// avoid codecs that don't support "missing"
		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			// ensure there is nothing in fieldcache before test starts
			FieldCache.DEFAULT.PurgeAllCaches();
		}

		private void AssertNoFieldCaches()
		{
			// docvalues sorting should NOT create any fieldcache entries!
			AreEqual(0, FieldCache.DEFAULT.GetCacheEntries().Length);
		}

		/// <summary>Tests sorting on type string</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestString()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new SortedDocValuesField("value", new BytesRef("foo")));
			doc.Add(NewStringField("value", "foo", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new SortedDocValuesField("value", new BytesRef("bar")));
			doc.Add(NewStringField("value", "bar", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.GetReader();
			writer.Close();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.STRING));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(2, td.TotalHits);
			// 'bar' comes before 'foo'
			AreEqual("bar", searcher.Doc(td.scoreDocs[0].doc).Get("value"
				));
			AreEqual("foo", searcher.Doc(td.scoreDocs[1].doc).Get("value"
				));
			AssertNoFieldCaches();
			ir.Close();
			dir.Close();
		}

		/// <summary>Tests reverse sorting on type string</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestStringReverse()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new SortedDocValuesField("value", new BytesRef("bar")));
			doc.Add(NewStringField("value", "bar", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new SortedDocValuesField("value", new BytesRef("foo")));
			doc.Add(NewStringField("value", "foo", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.GetReader();
			writer.Close();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.STRING, true));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(2, td.TotalHits);
			// 'foo' comes after 'bar' in reverse order
			AreEqual("foo", searcher.Doc(td.scoreDocs[0].doc).Get("value"
				));
			AreEqual("bar", searcher.Doc(td.scoreDocs[1].doc).Get("value"
				));
			AssertNoFieldCaches();
			ir.Close();
			dir.Close();
		}

		/// <summary>Tests sorting on type string_val</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestStringVal()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new BinaryDocValuesField("value", new BytesRef("foo")));
			doc.Add(NewStringField("value", "foo", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new BinaryDocValuesField("value", new BytesRef("bar")));
			doc.Add(NewStringField("value", "bar", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.GetReader();
			writer.Close();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.STRING_VAL));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(2, td.TotalHits);
			// 'bar' comes before 'foo'
			AreEqual("bar", searcher.Doc(td.scoreDocs[0].doc).Get("value"
				));
			AreEqual("foo", searcher.Doc(td.scoreDocs[1].doc).Get("value"
				));
			AssertNoFieldCaches();
			ir.Close();
			dir.Close();
		}

		/// <summary>Tests reverse sorting on type string_val</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestStringValReverse()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new BinaryDocValuesField("value", new BytesRef("bar")));
			doc.Add(NewStringField("value", "bar", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new BinaryDocValuesField("value", new BytesRef("foo")));
			doc.Add(NewStringField("value", "foo", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.GetReader();
			writer.Close();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.STRING_VAL, true));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(2, td.TotalHits);
			// 'foo' comes after 'bar' in reverse order
			AreEqual("foo", searcher.Doc(td.scoreDocs[0].doc).Get("value"
				));
			AreEqual("bar", searcher.Doc(td.scoreDocs[1].doc).Get("value"
				));
			AssertNoFieldCaches();
			ir.Close();
			dir.Close();
		}

		/// <summary>Tests sorting on type string_val, but with a SortedDocValuesField</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestStringValSorted()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new SortedDocValuesField("value", new BytesRef("foo")));
			doc.Add(NewStringField("value", "foo", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new SortedDocValuesField("value", new BytesRef("bar")));
			doc.Add(NewStringField("value", "bar", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.GetReader();
			writer.Close();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.STRING_VAL));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(2, td.TotalHits);
			// 'bar' comes before 'foo'
			AreEqual("bar", searcher.Doc(td.scoreDocs[0].doc).Get("value"
				));
			AreEqual("foo", searcher.Doc(td.scoreDocs[1].doc).Get("value"
				));
			AssertNoFieldCaches();
			ir.Close();
			dir.Close();
		}

		/// <summary>Tests reverse sorting on type string_val, but with a SortedDocValuesField
		/// 	</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestStringValReverseSorted()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new SortedDocValuesField("value", new BytesRef("bar")));
			doc.Add(NewStringField("value", "bar", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new SortedDocValuesField("value", new BytesRef("foo")));
			doc.Add(NewStringField("value", "foo", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.GetReader();
			writer.Close();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.STRING_VAL, true));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(2, td.TotalHits);
			// 'foo' comes after 'bar' in reverse order
			AreEqual("foo", searcher.Doc(td.scoreDocs[0].doc).Get("value"
				));
			AreEqual("bar", searcher.Doc(td.scoreDocs[1].doc).Get("value"
				));
			AssertNoFieldCaches();
			ir.Close();
			dir.Close();
		}

		/// <summary>Tests sorting on type byte</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestByte()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new NumericDocValuesField("value", 23));
			doc.Add(NewStringField("value", "23", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new NumericDocValuesField("value", -1));
			doc.Add(NewStringField("value", "-1", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new NumericDocValuesField("value", 4));
			doc.Add(NewStringField("value", "4", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.GetReader();
			writer.Close();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.BYTE));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(3, td.TotalHits);
			// numeric order
			AreEqual("-1", searcher.Doc(td.scoreDocs[0].doc).Get("value"
				));
			AreEqual("4", searcher.Doc(td.scoreDocs[1].doc).Get("value"
				));
			AreEqual("23", searcher.Doc(td.scoreDocs[2].doc).Get("value"
				));
			AssertNoFieldCaches();
			ir.Close();
			dir.Close();
		}

		/// <summary>Tests sorting on type byte in reverse</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestByteReverse()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new NumericDocValuesField("value", 23));
			doc.Add(NewStringField("value", "23", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new NumericDocValuesField("value", -1));
			doc.Add(NewStringField("value", "-1", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new NumericDocValuesField("value", 4));
			doc.Add(NewStringField("value", "4", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.GetReader();
			writer.Close();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.BYTE, true));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(3, td.TotalHits);
			// reverse numeric order
			AreEqual("23", searcher.Doc(td.scoreDocs[0].doc).Get("value"
				));
			AreEqual("4", searcher.Doc(td.scoreDocs[1].doc).Get("value"
				));
			AreEqual("-1", searcher.Doc(td.scoreDocs[2].doc).Get("value"
				));
			AssertNoFieldCaches();
			ir.Close();
			dir.Close();
		}

		/// <summary>Tests sorting on type short</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestShort()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new NumericDocValuesField("value", 300));
			doc.Add(NewStringField("value", "300", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new NumericDocValuesField("value", -1));
			doc.Add(NewStringField("value", "-1", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new NumericDocValuesField("value", 4));
			doc.Add(NewStringField("value", "4", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.GetReader();
			writer.Close();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.SHORT));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(3, td.TotalHits);
			// numeric order
			AreEqual("-1", searcher.Doc(td.scoreDocs[0].doc).Get("value"
				));
			AreEqual("4", searcher.Doc(td.scoreDocs[1].doc).Get("value"
				));
			AreEqual("300", searcher.Doc(td.scoreDocs[2].doc).Get("value"
				));
			AssertNoFieldCaches();
			ir.Close();
			dir.Close();
		}

		/// <summary>Tests sorting on type short in reverse</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestShortReverse()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new NumericDocValuesField("value", 300));
			doc.Add(NewStringField("value", "300", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new NumericDocValuesField("value", -1));
			doc.Add(NewStringField("value", "-1", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new NumericDocValuesField("value", 4));
			doc.Add(NewStringField("value", "4", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.GetReader();
			writer.Close();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.SHORT, true));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(3, td.TotalHits);
			// reverse numeric order
			AreEqual("300", searcher.Doc(td.scoreDocs[0].doc).Get("value"
				));
			AreEqual("4", searcher.Doc(td.scoreDocs[1].doc).Get("value"
				));
			AreEqual("-1", searcher.Doc(td.scoreDocs[2].doc).Get("value"
				));
			AssertNoFieldCaches();
			ir.Close();
			dir.Close();
		}

		/// <summary>Tests sorting on type int</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestInt()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new NumericDocValuesField("value", 300000));
			doc.Add(NewStringField("value", "300000", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new NumericDocValuesField("value", -1));
			doc.Add(NewStringField("value", "-1", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new NumericDocValuesField("value", 4));
			doc.Add(NewStringField("value", "4", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.GetReader();
			writer.Close();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.INT));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(3, td.TotalHits);
			// numeric order
			AreEqual("-1", searcher.Doc(td.scoreDocs[0].doc).Get("value"
				));
			AreEqual("4", searcher.Doc(td.scoreDocs[1].doc).Get("value"
				));
			AreEqual("300000", searcher.Doc(td.scoreDocs[2].doc).Get("value"
				));
			AssertNoFieldCaches();
			ir.Close();
			dir.Close();
		}

		/// <summary>Tests sorting on type int in reverse</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestIntReverse()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new NumericDocValuesField("value", 300000));
			doc.Add(NewStringField("value", "300000", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new NumericDocValuesField("value", -1));
			doc.Add(NewStringField("value", "-1", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new NumericDocValuesField("value", 4));
			doc.Add(NewStringField("value", "4", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.GetReader();
			writer.Close();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.INT, true));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(3, td.TotalHits);
			// reverse numeric order
			AreEqual("300000", searcher.Doc(td.scoreDocs[0].doc).Get("value"
				));
			AreEqual("4", searcher.Doc(td.scoreDocs[1].doc).Get("value"
				));
			AreEqual("-1", searcher.Doc(td.scoreDocs[2].doc).Get("value"
				));
			AssertNoFieldCaches();
			ir.Close();
			dir.Close();
		}

		/// <summary>Tests sorting on type int with a missing value</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestIntMissing()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new NumericDocValuesField("value", -1));
			doc.Add(NewStringField("value", "-1", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new NumericDocValuesField("value", 4));
			doc.Add(NewStringField("value", "4", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.GetReader();
			writer.Close();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.INT));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(3, td.TotalHits);
			// null is treated as a 0
			AreEqual("-1", searcher.Doc(td.scoreDocs[0].doc).Get("value"
				));
			IsNull(searcher.Doc(td.scoreDocs[1].doc).Get("value"));
			AreEqual("4", searcher.Doc(td.scoreDocs[2].doc).Get("value"
				));
			ir.Close();
			dir.Close();
		}

		/// <summary>Tests sorting on type int, specifying the missing value should be treated as Integer.MAX_VALUE
		/// 	</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestIntMissingLast()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new NumericDocValuesField("value", -1));
			doc.Add(NewStringField("value", "-1", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new NumericDocValuesField("value", 4));
			doc.Add(NewStringField("value", "4", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.GetReader();
			writer.Close();
			IndexSearcher searcher = NewSearcher(ir);
			SortField sortField = new SortField("value", SortField.Type.INT);
			sortField.SetMissingValue(int.MaxValue);
			Sort sort = new Sort(sortField);
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(3, td.TotalHits);
			// null is treated as a Integer.MAX_VALUE
			AreEqual("-1", searcher.Doc(td.scoreDocs[0].doc).Get("value"
				));
			AreEqual("4", searcher.Doc(td.scoreDocs[1].doc).Get("value"
				));
			IsNull(searcher.Doc(td.scoreDocs[2].doc).Get("value"));
			ir.Close();
			dir.Close();
		}

		/// <summary>Tests sorting on type long</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestLong()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new NumericDocValuesField("value", 3000000000L));
			doc.Add(NewStringField("value", "3000000000", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new NumericDocValuesField("value", -1));
			doc.Add(NewStringField("value", "-1", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new NumericDocValuesField("value", 4));
			doc.Add(NewStringField("value", "4", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.GetReader();
			writer.Close();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.LONG));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(3, td.TotalHits);
			// numeric order
			AreEqual("-1", searcher.Doc(td.scoreDocs[0].doc).Get("value"
				));
			AreEqual("4", searcher.Doc(td.scoreDocs[1].doc).Get("value"
				));
			AreEqual("3000000000", searcher.Doc(td.scoreDocs[2].doc).Get
				("value"));
			AssertNoFieldCaches();
			ir.Close();
			dir.Close();
		}

		/// <summary>Tests sorting on type long in reverse</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestLongReverse()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new NumericDocValuesField("value", 3000000000L));
			doc.Add(NewStringField("value", "3000000000", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new NumericDocValuesField("value", -1));
			doc.Add(NewStringField("value", "-1", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new NumericDocValuesField("value", 4));
			doc.Add(NewStringField("value", "4", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.GetReader();
			writer.Close();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.LONG, true));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(3, td.TotalHits);
			// reverse numeric order
			AreEqual("3000000000", searcher.Doc(td.scoreDocs[0].doc).Get
				("value"));
			AreEqual("4", searcher.Doc(td.scoreDocs[1].doc).Get("value"
				));
			AreEqual("-1", searcher.Doc(td.scoreDocs[2].doc).Get("value"
				));
			AssertNoFieldCaches();
			ir.Close();
			dir.Close();
		}

		/// <summary>Tests sorting on type long with a missing value</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestLongMissing()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new NumericDocValuesField("value", -1));
			doc.Add(NewStringField("value", "-1", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new NumericDocValuesField("value", 4));
			doc.Add(NewStringField("value", "4", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.GetReader();
			writer.Close();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.LONG));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(3, td.TotalHits);
			// null is treated as 0
			AreEqual("-1", searcher.Doc(td.scoreDocs[0].doc).Get("value"
				));
			IsNull(searcher.Doc(td.scoreDocs[1].doc).Get("value"));
			AreEqual("4", searcher.Doc(td.scoreDocs[2].doc).Get("value"
				));
			ir.Close();
			dir.Close();
		}

		/// <summary>Tests sorting on type long, specifying the missing value should be treated as Long.MAX_VALUE
		/// 	</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestLongMissingLast()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new NumericDocValuesField("value", -1));
			doc.Add(NewStringField("value", "-1", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new NumericDocValuesField("value", 4));
			doc.Add(NewStringField("value", "4", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.GetReader();
			writer.Close();
			IndexSearcher searcher = NewSearcher(ir);
			SortField sortField = new SortField("value", SortField.Type.LONG);
			sortField.SetMissingValue(long.MaxValue);
			Sort sort = new Sort(sortField);
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(3, td.TotalHits);
			// null is treated as Long.MAX_VALUE
			AreEqual("-1", searcher.Doc(td.scoreDocs[0].doc).Get("value"
				));
			AreEqual("4", searcher.Doc(td.scoreDocs[1].doc).Get("value"
				));
			IsNull(searcher.Doc(td.scoreDocs[2].doc).Get("value"));
			ir.Close();
			dir.Close();
		}

		/// <summary>Tests sorting on type float</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestFloat()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new FloatDocValuesField("value", 30.1F));
			doc.Add(NewStringField("value", "30.1", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new FloatDocValuesField("value", -1.3F));
			doc.Add(NewStringField("value", "-1.3", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new FloatDocValuesField("value", 4.2F));
			doc.Add(NewStringField("value", "4.2", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.GetReader();
			writer.Close();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.FLOAT));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(3, td.TotalHits);
			// numeric order
			AreEqual("-1.3", searcher.Doc(td.scoreDocs[0].doc).Get("value"
				));
			AreEqual("4.2", searcher.Doc(td.scoreDocs[1].doc).Get("value"
				));
			AreEqual("30.1", searcher.Doc(td.scoreDocs[2].doc).Get("value"
				));
			AssertNoFieldCaches();
			ir.Close();
			dir.Close();
		}

		/// <summary>Tests sorting on type float in reverse</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestFloatReverse()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new FloatDocValuesField("value", 30.1F));
			doc.Add(NewStringField("value", "30.1", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new FloatDocValuesField("value", -1.3F));
			doc.Add(NewStringField("value", "-1.3", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new FloatDocValuesField("value", 4.2F));
			doc.Add(NewStringField("value", "4.2", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.GetReader();
			writer.Close();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.FLOAT, true));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(3, td.TotalHits);
			// reverse numeric order
			AreEqual("30.1", searcher.Doc(td.scoreDocs[0].doc).Get("value"
				));
			AreEqual("4.2", searcher.Doc(td.scoreDocs[1].doc).Get("value"
				));
			AreEqual("-1.3", searcher.Doc(td.scoreDocs[2].doc).Get("value"
				));
			AssertNoFieldCaches();
			ir.Close();
			dir.Close();
		}

		/// <summary>Tests sorting on type float with a missing value</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestFloatMissing()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new FloatDocValuesField("value", -1.3F));
			doc.Add(NewStringField("value", "-1.3", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new FloatDocValuesField("value", 4.2F));
			doc.Add(NewStringField("value", "4.2", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.GetReader();
			writer.Close();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.FLOAT));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(3, td.TotalHits);
			// null is treated as 0
			AreEqual("-1.3", searcher.Doc(td.scoreDocs[0].doc).Get("value"
				));
			IsNull(searcher.Doc(td.scoreDocs[1].doc).Get("value"));
			AreEqual("4.2", searcher.Doc(td.scoreDocs[2].doc).Get("value"
				));
			ir.Close();
			dir.Close();
		}

		/// <summary>Tests sorting on type float, specifying the missing value should be treated as Float.MAX_VALUE
		/// 	</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestFloatMissingLast()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new FloatDocValuesField("value", -1.3F));
			doc.Add(NewStringField("value", "-1.3", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new FloatDocValuesField("value", 4.2F));
			doc.Add(NewStringField("value", "4.2", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.GetReader();
			writer.Close();
			IndexSearcher searcher = NewSearcher(ir);
			SortField sortField = new SortField("value", SortField.Type.FLOAT);
			sortField.SetMissingValue(float.MaxValue);
			Sort sort = new Sort(sortField);
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(3, td.TotalHits);
			// null is treated as Float.MAX_VALUE
			AreEqual("-1.3", searcher.Doc(td.scoreDocs[0].doc).Get("value"
				));
			AreEqual("4.2", searcher.Doc(td.scoreDocs[1].doc).Get("value"
				));
			IsNull(searcher.Doc(td.scoreDocs[2].doc).Get("value"));
			ir.Close();
			dir.Close();
		}

		/// <summary>Tests sorting on type double</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestDouble()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new DoubleDocValuesField("value", 30.1));
			doc.Add(NewStringField("value", "30.1", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new DoubleDocValuesField("value", -1.3));
			doc.Add(NewStringField("value", "-1.3", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new DoubleDocValuesField("value", 4.2333333333333));
			doc.Add(NewStringField("value", "4.2333333333333", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new DoubleDocValuesField("value", 4.2333333333332));
			doc.Add(NewStringField("value", "4.2333333333332", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.GetReader();
			writer.Close();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.DOUBLE));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(4, td.TotalHits);
			// numeric order
			AreEqual("-1.3", searcher.Doc(td.scoreDocs[0].doc).Get("value"
				));
			AreEqual("4.2333333333332", searcher.Doc(td.scoreDocs[1].doc
				).Get("value"));
			AreEqual("4.2333333333333", searcher.Doc(td.scoreDocs[2].doc
				).Get("value"));
			AreEqual("30.1", searcher.Doc(td.scoreDocs[3].doc).Get("value"
				));
			AssertNoFieldCaches();
			ir.Close();
			dir.Close();
		}

		/// <summary>Tests sorting on type double with +/- zero</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestDoubleSignedZero()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new DoubleDocValuesField("value", +0D));
			doc.Add(NewStringField("value", "+0", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new DoubleDocValuesField("value", -0D));
			doc.Add(NewStringField("value", "-0", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			IndexReader ir = writer.GetReader();
			writer.Close();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.DOUBLE));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(2, td.TotalHits);
			// numeric order
			AreEqual("-0", searcher.Doc(td.scoreDocs[0].doc).Get("value"
				));
			AreEqual("+0", searcher.Doc(td.scoreDocs[1].doc).Get("value"
				));
			ir.Close();
			dir.Close();
		}

		/// <summary>Tests sorting on type double in reverse</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestDoubleReverse()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new DoubleDocValuesField("value", 30.1));
			doc.Add(NewStringField("value", "30.1", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new DoubleDocValuesField("value", -1.3));
			doc.Add(NewStringField("value", "-1.3", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new DoubleDocValuesField("value", 4.2333333333333));
			doc.Add(NewStringField("value", "4.2333333333333", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new DoubleDocValuesField("value", 4.2333333333332));
			doc.Add(NewStringField("value", "4.2333333333332", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.GetReader();
			writer.Close();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.DOUBLE, true));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(4, td.TotalHits);
			// numeric order
			AreEqual("30.1", searcher.Doc(td.scoreDocs[0].doc).Get("value"
				));
			AreEqual("4.2333333333333", searcher.Doc(td.scoreDocs[1].doc
				).Get("value"));
			AreEqual("4.2333333333332", searcher.Doc(td.scoreDocs[2].doc
				).Get("value"));
			AreEqual("-1.3", searcher.Doc(td.scoreDocs[3].doc).Get("value"
				));
			AssertNoFieldCaches();
			ir.Close();
			dir.Close();
		}

		/// <summary>Tests sorting on type double with a missing value</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestDoubleMissing()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new DoubleDocValuesField("value", -1.3));
			doc.Add(NewStringField("value", "-1.3", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new DoubleDocValuesField("value", 4.2333333333333));
			doc.Add(NewStringField("value", "4.2333333333333", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new DoubleDocValuesField("value", 4.2333333333332));
			doc.Add(NewStringField("value", "4.2333333333332", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.GetReader();
			writer.Close();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.DOUBLE));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(4, td.TotalHits);
			// null treated as a 0
			AreEqual("-1.3", searcher.Doc(td.scoreDocs[0].doc).Get("value"
				));
			IsNull(searcher.Doc(td.scoreDocs[1].doc).Get("value"));
			AreEqual("4.2333333333332", searcher.Doc(td.scoreDocs[2].doc
				).Get("value"));
			AreEqual("4.2333333333333", searcher.Doc(td.scoreDocs[3].doc
				).Get("value"));
			ir.Close();
			dir.Close();
		}

		/// <summary>Tests sorting on type double, specifying the missing value should be treated as Double.MAX_VALUE
		/// 	</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestDoubleMissingLast()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new DoubleDocValuesField("value", -1.3));
			doc.Add(NewStringField("value", "-1.3", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new DoubleDocValuesField("value", 4.2333333333333));
			doc.Add(NewStringField("value", "4.2333333333333", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new DoubleDocValuesField("value", 4.2333333333332));
			doc.Add(NewStringField("value", "4.2333333333332", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.GetReader();
			writer.Close();
			IndexSearcher searcher = NewSearcher(ir);
			SortField sortField = new SortField("value", SortField.Type.DOUBLE);
			sortField.SetMissingValue(double.MaxValue);
			Sort sort = new Sort(sortField);
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(4, td.TotalHits);
			// null treated as Double.MAX_VALUE
			AreEqual("-1.3", searcher.Doc(td.scoreDocs[0].doc).Get("value"
				));
			AreEqual("4.2333333333332", searcher.Doc(td.scoreDocs[1].doc
				).Get("value"));
			AreEqual("4.2333333333333", searcher.Doc(td.scoreDocs[2].doc
				).Get("value"));
			IsNull(searcher.Doc(td.scoreDocs[3].doc).Get("value"));
			ir.Close();
			dir.Close();
		}
	}
}
