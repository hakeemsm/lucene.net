/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.Text;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search
{
	public class TestSort : LuceneTestCase
	{
		/// <summary>Tests sorting on type string</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestString()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewStringField("value", "foo", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "bar", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.STRING));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(2, td.TotalHits);
			// 'bar' comes before 'foo'
			AreEqual("bar", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"
				));
			AreEqual("foo", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"
				));
			ir.Dispose();
			dir.Dispose();
		}

		/// <summary>Tests sorting on type string with a missing value</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestStringMissing()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "foo", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "bar", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.STRING));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(3, td.TotalHits);
			// null comes first
			IsNull(searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
			AreEqual("bar", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"
				));
			AreEqual("foo", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"
				));
			ir.Dispose();
			dir.Dispose();
		}

		/// <summary>Tests reverse sorting on type string</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestStringReverse()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewStringField("value", "bar", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "foo", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.STRING, true));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(2, td.TotalHits);
			// 'foo' comes after 'bar' in reverse order
			AreEqual("foo", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"
				));
			AreEqual("bar", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"
				));
			ir.Dispose();
			dir.Dispose();
		}

		/// <summary>Tests sorting on type string_val</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestStringVal()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewStringField("value", "foo", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "bar", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.STRING_VAL));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(2, td.TotalHits);
			// 'bar' comes before 'foo'
			AreEqual("bar", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"
				));
			AreEqual("foo", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"
				));
			ir.Dispose();
			dir.Dispose();
		}

		/// <summary>Tests sorting on type string_val with a missing value</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestStringValMissing()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "foo", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "bar", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.STRING_VAL));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(3, td.TotalHits);
			// null comes first
			IsNull(searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
			AreEqual("bar", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"
				));
			AreEqual("foo", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"
				));
			ir.Dispose();
			dir.Dispose();
		}

		/// <summary>
		/// Tests sorting on type string with a missing
		/// value sorted first
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestStringMissingSortedFirst()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "foo", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "bar", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			SortField sf = new SortField("value", SortField.Type.STRING);
			Sort sort = new Sort(sf);
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(3, td.TotalHits);
			// null comes first
			IsNull(searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
			AreEqual("bar", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"
				));
			AreEqual("foo", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"
				));
			ir.Dispose();
			dir.Dispose();
		}

		/// <summary>
		/// Tests reverse sorting on type string with a missing
		/// value sorted first
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestStringMissingSortedFirstReverse()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "foo", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "bar", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			SortField sf = new SortField("value", SortField.Type.STRING, true);
			Sort sort = new Sort(sf);
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(3, td.TotalHits);
			AreEqual("foo", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"
				));
			AreEqual("bar", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"
				));
			// null comes last
			IsNull(searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));
			ir.Dispose();
			dir.Dispose();
		}

		/// <summary>
		/// Tests sorting on type string with a missing
		/// value sorted last
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestStringValMissingSortedLast()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "foo", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "bar", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			SortField sf = new SortField("value", SortField.Type.STRING);
			sf.SetMissingValue(SortField.STRING_LAST);
			Sort sort = new Sort(sf);
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(3, td.TotalHits);
			AreEqual("bar", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"
				));
			AreEqual("foo", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"
				));
			// null comes last
			IsNull(searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));
			ir.Dispose();
			dir.Dispose();
		}

		/// <summary>
		/// Tests reverse sorting on type string with a missing
		/// value sorted last
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestStringValMissingSortedLastReverse()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "foo", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "bar", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			SortField sf = new SortField("value", SortField.Type.STRING, true);
			sf.SetMissingValue(SortField.STRING_LAST);
			Sort sort = new Sort(sf);
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(3, td.TotalHits);
			// null comes first
			IsNull(searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
			AreEqual("foo", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"
				));
			AreEqual("bar", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"
				));
			ir.Dispose();
			dir.Dispose();
		}

		/// <summary>Tests reverse sorting on type string_val</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestStringValReverse()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewStringField("value", "bar", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "foo", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.STRING_VAL, true));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(2, td.TotalHits);
			// 'foo' comes after 'bar' in reverse order
			AreEqual("foo", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"
				));
			AreEqual("bar", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"
				));
			ir.Dispose();
			dir.Dispose();
		}

		/// <summary>Tests sorting on internal docid order</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestFieldDoc()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewStringField("value", "foo", Field.Store.NO));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "bar", Field.Store.NO));
			writer.AddDocument(doc);
			IndexReader ir = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(SortField.FIELD_DOC);
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(2, td.TotalHits);
			// docid 0, then docid 1
			AreEqual(0, td.ScoreDocs[0].Doc);
			AreEqual(1, td.ScoreDocs[1].Doc);
			ir.Dispose();
			dir.Dispose();
		}

		/// <summary>Tests sorting on reverse internal docid order</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestFieldDocReverse()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewStringField("value", "foo", Field.Store.NO));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "bar", Field.Store.NO));
			writer.AddDocument(doc);
			IndexReader ir = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField(null, SortField.Type.DOC, true));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(2, td.TotalHits);
			// docid 1, then docid 0
			AreEqual(1, td.ScoreDocs[0].Doc);
			AreEqual(0, td.ScoreDocs[1].Doc);
			ir.Dispose();
			dir.Dispose();
		}

		/// <summary>Tests default sort (by score)</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestFieldScore()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewTextField("value", "foo bar bar bar bar", Field.Store.NO));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewTextField("value", "foo foo foo foo foo", Field.Store.NO));
			writer.AddDocument(doc);
			IndexReader ir = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort();
			TopDocs actual = searcher.Search(new TermQuery(new Term("value", "foo")), 10, sort
				);
			AreEqual(2, actual.TotalHits);
			TopDocs expected = searcher.Search(new TermQuery(new Term("value", "foo")), 10);
			// the two topdocs should be the same
			AreEqual(expected.TotalHits, actual.TotalHits);
			for (int i = 0; i < actual.ScoreDocs.Length; i++)
			{
				AreEqual(actual.ScoreDocs[i].Doc, expected.ScoreDocs[i].Doc
					);
			}
			ir.Dispose();
			dir.Dispose();
		}

		/// <summary>Tests default sort (by score) in reverse</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestFieldScoreReverse()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewTextField("value", "foo bar bar bar bar", Field.Store.NO));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewTextField("value", "foo foo foo foo foo", Field.Store.NO));
			writer.AddDocument(doc);
			IndexReader ir = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField(null, SortField.Type.SCORE, true));
			TopDocs actual = searcher.Search(new TermQuery(new Term("value", "foo")), 10, sort
				);
			AreEqual(2, actual.TotalHits);
			TopDocs expected = searcher.Search(new TermQuery(new Term("value", "foo")), 10);
			// the two topdocs should be the reverse of each other
			AreEqual(expected.TotalHits, actual.TotalHits);
			AreEqual(actual.ScoreDocs[0].Doc, expected.ScoreDocs[1].Doc
				);
			AreEqual(actual.ScoreDocs[1].Doc, expected.ScoreDocs[0].Doc
				);
			ir.Dispose();
			dir.Dispose();
		}

		/// <summary>Tests sorting on type byte</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestByte()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewStringField("value", "23", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "-1", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "4", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.BYTE));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(3, td.TotalHits);
			// numeric order
			AreEqual("-1", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"
				));
			AreEqual("4", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"
				));
			AreEqual("23", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"
				));
			ir.Dispose();
			dir.Dispose();
		}

		/// <summary>Tests sorting on type byte with a missing value</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestByteMissing()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "-1", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "4", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.BYTE));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(3, td.TotalHits);
			// null value is treated as a 0
			AreEqual("-1", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"
				));
			IsNull(searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
			AreEqual("4", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"
				));
			ir.Dispose();
			dir.Dispose();
		}

		/// <summary>Tests sorting on type byte, specifying the missing value should be treated as Byte.MAX_VALUE
		/// 	</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestByteMissingLast()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "-1", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "4", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			SortField sortField = new SortField("value", SortField.Type.BYTE);
			sortField.SetMissingValue(byte.MaxValue);
			Sort sort = new Sort(sortField);
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(3, td.TotalHits);
			// null value is treated Byte.MAX_VALUE
			AreEqual("-1", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"
				));
			AreEqual("4", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"
				));
			IsNull(searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));
			ir.Dispose();
			dir.Dispose();
		}

		/// <summary>Tests sorting on type byte in reverse</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestByteReverse()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewStringField("value", "23", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "-1", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "4", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.BYTE, true));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(3, td.TotalHits);
			// reverse numeric order
			AreEqual("23", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"
				));
			AreEqual("4", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"
				));
			AreEqual("-1", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"
				));
			ir.Dispose();
			dir.Dispose();
		}

		/// <summary>Tests sorting on type short</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestShort()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewStringField("value", "300", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "-1", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "4", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.SHORT));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(3, td.TotalHits);
			// numeric order
			AreEqual("-1", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"
				));
			AreEqual("4", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"
				));
			AreEqual("300", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"
				));
			ir.Dispose();
			dir.Dispose();
		}

		/// <summary>Tests sorting on type short with a missing value</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestShortMissing()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "-1", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "4", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.SHORT));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(3, td.TotalHits);
			// null is treated as a 0
			AreEqual("-1", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"
				));
			IsNull(searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
			AreEqual("4", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"
				));
			ir.Dispose();
			dir.Dispose();
		}

		/// <summary>Tests sorting on type short, specifying the missing value should be treated as Short.MAX_VALUE
		/// 	</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestShortMissingLast()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "-1", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "4", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			SortField sortField = new SortField("value", SortField.Type.SHORT);
			sortField.SetMissingValue(short.MaxValue);
			Sort sort = new Sort(sortField);
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(3, td.TotalHits);
			// null is treated as Short.MAX_VALUE
			AreEqual("-1", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"
				));
			AreEqual("4", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"
				));
			IsNull(searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));
			ir.Dispose();
			dir.Dispose();
		}

		/// <summary>Tests sorting on type short in reverse</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestShortReverse()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewStringField("value", "300", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "-1", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "4", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.SHORT, true));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(3, td.TotalHits);
			// reverse numeric order
			AreEqual("300", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"
				));
			AreEqual("4", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"
				));
			AreEqual("-1", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"
				));
			ir.Dispose();
			dir.Dispose();
		}

		/// <summary>Tests sorting on type int</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestInt()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewStringField("value", "300000", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "-1", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "4", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.INT));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(3, td.TotalHits);
			// numeric order
			AreEqual("-1", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"
				));
			AreEqual("4", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"
				));
			AreEqual("300000", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"
				));
			ir.Dispose();
			dir.Dispose();
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
			doc.Add(NewStringField("value", "-1", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "4", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.INT));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(3, td.TotalHits);
			// null is treated as a 0
			AreEqual("-1", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"
				));
			IsNull(searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
			AreEqual("4", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"
				));
			ir.Dispose();
			dir.Dispose();
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
			doc.Add(NewStringField("value", "-1", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "4", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			SortField sortField = new SortField("value", SortField.Type.INT);
			sortField.SetMissingValue(int.MaxValue);
			Sort sort = new Sort(sortField);
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(3, td.TotalHits);
			// null is treated as a Integer.MAX_VALUE
			AreEqual("-1", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"
				));
			AreEqual("4", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"
				));
			IsNull(searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));
			ir.Dispose();
			dir.Dispose();
		}

		/// <summary>Tests sorting on type int in reverse</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestIntReverse()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewStringField("value", "300000", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "-1", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "4", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.INT, true));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(3, td.TotalHits);
			// reverse numeric order
			AreEqual("300000", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"
				));
			AreEqual("4", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"
				));
			AreEqual("-1", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"
				));
			ir.Dispose();
			dir.Dispose();
		}

		/// <summary>Tests sorting on type long</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestLong()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewStringField("value", "3000000000", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "-1", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "4", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.LONG));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(3, td.TotalHits);
			// numeric order
			AreEqual("-1", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"
				));
			AreEqual("4", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"
				));
			AreEqual("3000000000", searcher.Doc(td.ScoreDocs[2].Doc).Get
				("value"));
			ir.Dispose();
			dir.Dispose();
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
			doc.Add(NewStringField("value", "-1", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "4", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.LONG));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(3, td.TotalHits);
			// null is treated as 0
			AreEqual("-1", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"
				));
			IsNull(searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
			AreEqual("4", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"
				));
			ir.Dispose();
			dir.Dispose();
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
			doc.Add(NewStringField("value", "-1", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "4", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			SortField sortField = new SortField("value", SortField.Type.LONG);
			sortField.SetMissingValue(long.MaxValue);
			Sort sort = new Sort(sortField);
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(3, td.TotalHits);
			// null is treated as Long.MAX_VALUE
			AreEqual("-1", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"
				));
			AreEqual("4", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"
				));
			IsNull(searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));
			ir.Dispose();
			dir.Dispose();
		}

		/// <summary>Tests sorting on type long in reverse</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestLongReverse()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewStringField("value", "3000000000", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "-1", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "4", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.LONG, true));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(3, td.TotalHits);
			// reverse numeric order
			AreEqual("3000000000", searcher.Doc(td.ScoreDocs[0].Doc).Get
				("value"));
			AreEqual("4", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"
				));
			AreEqual("-1", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"
				));
			ir.Dispose();
			dir.Dispose();
		}

		/// <summary>Tests sorting on type float</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestFloat()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewStringField("value", "30.1", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "-1.3", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "4.2", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.FLOAT));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(3, td.TotalHits);
			// numeric order
			AreEqual("-1.3", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"
				));
			AreEqual("4.2", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"
				));
			AreEqual("30.1", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"
				));
			ir.Dispose();
			dir.Dispose();
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
			doc.Add(NewStringField("value", "-1.3", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "4.2", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.FLOAT));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(3, td.TotalHits);
			// null is treated as 0
			AreEqual("-1.3", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"
				));
			IsNull(searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
			AreEqual("4.2", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"
				));
			ir.Dispose();
			dir.Dispose();
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
			doc.Add(NewStringField("value", "-1.3", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "4.2", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			SortField sortField = new SortField("value", SortField.Type.FLOAT);
			sortField.SetMissingValue(float.MaxValue);
			Sort sort = new Sort(sortField);
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(3, td.TotalHits);
			// null is treated as Float.MAX_VALUE
			AreEqual("-1.3", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"
				));
			AreEqual("4.2", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"
				));
			IsNull(searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));
			ir.Dispose();
			dir.Dispose();
		}

		/// <summary>Tests sorting on type float in reverse</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestFloatReverse()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewStringField("value", "30.1", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "-1.3", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "4.2", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.FLOAT, true));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(3, td.TotalHits);
			// reverse numeric order
			AreEqual("30.1", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"
				));
			AreEqual("4.2", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"
				));
			AreEqual("-1.3", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"
				));
			ir.Dispose();
			dir.Dispose();
		}

		/// <summary>Tests sorting on type double</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestDouble()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewStringField("value", "30.1", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "-1.3", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "4.2333333333333", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "4.2333333333332", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.DOUBLE));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(4, td.TotalHits);
			// numeric order
			AreEqual("-1.3", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"
				));
			AreEqual("4.2333333333332", searcher.Doc(td.ScoreDocs[1].Doc
				).Get("value"));
			AreEqual("4.2333333333333", searcher.Doc(td.ScoreDocs[2].Doc
				).Get("value"));
			AreEqual("30.1", searcher.Doc(td.ScoreDocs[3].Doc).Get("value"
				));
			ir.Dispose();
			dir.Dispose();
		}

		/// <summary>Tests sorting on type double with +/- zero</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestDoubleSignedZero()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewStringField("value", "+0", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "-0", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			IndexReader ir = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.DOUBLE));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(2, td.TotalHits);
			// numeric order
			AreEqual("-0", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"
				));
			AreEqual("+0", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"
				));
			ir.Dispose();
			dir.Dispose();
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
			doc.Add(NewStringField("value", "-1.3", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "4.2333333333333", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "4.2333333333332", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.DOUBLE));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(4, td.TotalHits);
			// null treated as a 0
			AreEqual("-1.3", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"
				));
			IsNull(searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
			AreEqual("4.2333333333332", searcher.Doc(td.ScoreDocs[2].Doc
				).Get("value"));
			AreEqual("4.2333333333333", searcher.Doc(td.ScoreDocs[3].Doc
				).Get("value"));
			ir.Dispose();
			dir.Dispose();
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
			doc.Add(NewStringField("value", "-1.3", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "4.2333333333333", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "4.2333333333332", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			SortField sortField = new SortField("value", SortField.Type.DOUBLE);
			sortField.SetMissingValue(double.MaxValue);
			Sort sort = new Sort(sortField);
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(4, td.TotalHits);
			// null treated as Double.MAX_VALUE
			AreEqual("-1.3", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"
				));
			AreEqual("4.2333333333332", searcher.Doc(td.ScoreDocs[1].Doc
				).Get("value"));
			AreEqual("4.2333333333333", searcher.Doc(td.ScoreDocs[2].Doc
				).Get("value"));
			IsNull(searcher.Doc(td.ScoreDocs[3].Doc).Get("value"));
			ir.Dispose();
			dir.Dispose();
		}

		/// <summary>Tests sorting on type double in reverse</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestDoubleReverse()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewStringField("value", "30.1", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "-1.3", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "4.2333333333333", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "4.2333333333332", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.DOUBLE, true));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(4, td.TotalHits);
			// numeric order
			AreEqual("30.1", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"
				));
			AreEqual("4.2333333333333", searcher.Doc(td.ScoreDocs[1].Doc
				).Get("value"));
			AreEqual("4.2333333333332", searcher.Doc(td.ScoreDocs[2].Doc
				).Get("value"));
			AreEqual("-1.3", searcher.Doc(td.ScoreDocs[3].Doc).Get("value"
				));
			ir.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestEmptyStringVsNullStringSort()
		{
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewStringField("f", string.Empty, Field.Store.NO));
			doc.Add(NewStringField("t", "1", Field.Store.NO));
			w.AddDocument(doc);
			w.Commit();
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("t", "1", Field.Store.NO));
			w.AddDocument(doc);
			IndexReader r = DirectoryReader.Open(w, true);
			w.Dispose();
			IndexSearcher s = NewSearcher(r);
			TopDocs hits = s.Search(new TermQuery(new Term("t", "1")), null, 10, new Sort(new 
				SortField("f", SortField.Type.STRING)));
			AreEqual(2, hits.TotalHits);
			// null sorts first
			AreEqual(1, hits.ScoreDocs[0].Doc);
			AreEqual(0, hits.ScoreDocs[1].Doc);
			r.Dispose();
			dir.Dispose();
		}

		/// <summary>test that we don't throw exception on multi-valued field (LUCENE-2142)</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestMultiValuedField()
		{
			Directory indexStore = NewDirectory();
			IndexWriter writer = new IndexWriter(indexStore, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			for (int i = 0; i < 5; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(new StringField("string", "a" + i, Field.Store.NO));
				doc.Add(new StringField("string", "b" + i, Field.Store.NO));
				writer.AddDocument(doc);
			}
			writer.ForceMerge(1);
			// enforce one segment to have a higher unique term count in all cases
			writer.Dispose();
			Sort sort = new Sort(new SortField("string", SortField.Type.STRING), SortField.FIELD_DOC
				);
			// this should not throw AIOOBE or RuntimeEx
			IndexReader reader = DirectoryReader.Open(indexStore);
			IndexSearcher searcher = NewSearcher(reader);
			searcher.Search(new MatchAllDocsQuery(), null, 500, sort);
			reader.Dispose();
			indexStore.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestMaxScore()
		{
			Directory d = NewDirectory();
			// Not RIW because we need exactly 2 segs:
			IndexWriter w = new IndexWriter(d, new IndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())));
			int id = 0;
			for (int seg = 0; seg < 2; seg++)
			{
				for (int docIDX = 0; docIDX < 10; docIDX++)
				{
					Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
						();
					doc.Add(NewStringField("id", string.Empty + docIDX, Field.Store.YES));
					StringBuilder sb = new StringBuilder();
					for (int i = 0; i < id; i++)
					{
						sb.Append(' ');
						sb.Append("text");
					}
					doc.Add(NewTextField("body", sb.ToString(), Field.Store.NO));
					w.AddDocument(doc);
					id++;
				}
				w.Commit();
			}
			IndexReader r = DirectoryReader.Open(w, true);
			w.Dispose();
			Query q = new TermQuery(new Term("body", "text"));
			IndexSearcher s = NewSearcher(r);
			float maxScore = s.Search(q, 10).GetMaxScore();
			AreEqual(maxScore, s.Search(q, null, 3, Sort.INDEXORDER, Random
				().NextBoolean(), true).GetMaxScore(), 0.0);
			AreEqual(maxScore, s.Search(q, null, 3, Sort.RELEVANCE, Random
				().NextBoolean(), true).GetMaxScore(), 0.0);
			AreEqual(maxScore, s.Search(q, null, 3, new Sort(new SortField
				[] { new SortField("id", SortField.Type.INT, false) }), Random().NextBoolean(), 
				true).GetMaxScore(), 0.0);
			AreEqual(maxScore, s.Search(q, null, 3, new Sort(new SortField
				[] { new SortField("id", SortField.Type.INT, true) }), Random().NextBoolean(), true
				).GetMaxScore(), 0.0);
			r.Dispose();
			d.Dispose();
		}

		/// <summary>test sorts when there's nothing in the index</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestEmptyIndex()
		{
			IndexSearcher empty = NewSearcher(new MultiReader());
			Query query = new TermQuery(new Term("contents", "foo"));
			Sort sort = new Sort();
			TopDocs td = empty.Search(query, null, 10, sort, true, true);
			AreEqual(0, td.TotalHits);
			sort.SetSort(SortField.FIELD_DOC);
			td = empty.Search(query, null, 10, sort, true, true);
			AreEqual(0, td.TotalHits);
			sort.SetSort(new SortField("int", SortField.Type.INT), SortField.FIELD_DOC);
			td = empty.Search(query, null, 10, sort, true, true);
			AreEqual(0, td.TotalHits);
			sort.SetSort(new SortField("string", SortField.Type.STRING, true), SortField.FIELD_DOC
				);
			td = empty.Search(query, null, 10, sort, true, true);
			AreEqual(0, td.TotalHits);
			sort.SetSort(new SortField("string_val", SortField.Type.STRING_VAL, true), SortField
				.FIELD_DOC);
			td = empty.Search(query, null, 10, sort, true, true);
			AreEqual(0, td.TotalHits);
			sort.SetSort(new SortField("float", SortField.Type.FLOAT), new SortField("string"
				, SortField.Type.STRING));
			td = empty.Search(query, null, 10, sort, true, true);
			AreEqual(0, td.TotalHits);
		}

		/// <summary>test sorts for a custom int parser that uses a simple char encoding</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestCustomIntParser()
		{
			IList<string> letters = Arrays.AsList(new string[] { "A", "B", "C", "D", "E", "F"
				, "G", "H", "I", "J" });
			Collections.Shuffle(letters, Random());
			Directory dir = NewDirectory();
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir);
			foreach (string letter in letters)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(NewStringField("parser", letter, Field.Store.YES));
				iw.AddDocument(doc);
			}
			IndexReader ir = iw.Reader;
			iw.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("parser", new _IntParser_1354()), SortField.FIELD_DOC
				);
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			// results should be in alphabetical order
			AreEqual(10, td.TotalHits);
			letters.Sort();
			for (int i = 0; i < letters.Count; i++)
			{
				AreEqual(letters[i], searcher.Doc(td.ScoreDocs[i].Doc).Get
					("parser"));
			}
			ir.Dispose();
			dir.Dispose();
		}

		private sealed class _IntParser_1354 : FieldCache.IntParser
		{
			public _IntParser_1354()
			{
			}

			public int ParseInt(BytesRef term)
			{
				return (term.bytes[term.offset] - 'A') * 123456;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public TermsEnum TermsEnum(Terms terms)
			{
				return terms.IEnumerator(null);
			}
		}

		/// <summary>test sorts for a custom byte parser that uses a simple char encoding</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestCustomByteParser()
		{
			IList<string> letters = Arrays.AsList(new string[] { "A", "B", "C", "D", "E", "F"
				, "G", "H", "I", "J" });
			Collections.Shuffle(letters, Random());
			Directory dir = NewDirectory();
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir);
			foreach (string letter in letters)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(NewStringField("parser", letter, Field.Store.YES));
				iw.AddDocument(doc);
			}
			IndexReader ir = iw.Reader;
			iw.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("parser", new _ByteParser_1398()), SortField.FIELD_DOC
				);
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			// results should be in alphabetical order
			AreEqual(10, td.TotalHits);
			letters.Sort();
			for (int i = 0; i < letters.Count; i++)
			{
				AreEqual(letters[i], searcher.Doc(td.ScoreDocs[i].Doc).Get
					("parser"));
			}
			ir.Dispose();
			dir.Dispose();
		}

		private sealed class _ByteParser_1398 : FieldCache.ByteParser
		{
			public _ByteParser_1398()
			{
			}

			public byte ParseByte(BytesRef term)
			{
				return unchecked((byte)(term.bytes[term.offset] - 'A'));
			}

			/// <exception cref="System.IO.IOException"></exception>
			public TermsEnum TermsEnum(Terms terms)
			{
				return terms.IEnumerator(null);
			}
		}

		/// <summary>test sorts for a custom short parser that uses a simple char encoding</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestCustomShortParser()
		{
			IList<string> letters = Arrays.AsList(new string[] { "A", "B", "C", "D", "E", "F"
				, "G", "H", "I", "J" });
			Collections.Shuffle(letters, Random());
			Directory dir = NewDirectory();
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir);
			foreach (string letter in letters)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(NewStringField("parser", letter, Field.Store.YES));
				iw.AddDocument(doc);
			}
			IndexReader ir = iw.Reader;
			iw.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("parser", new _ShortParser_1442()), SortField.
				FIELD_DOC);
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			// results should be in alphabetical order
			AreEqual(10, td.TotalHits);
			letters.Sort();
			for (int i = 0; i < letters.Count; i++)
			{
				AreEqual(letters[i], searcher.Doc(td.ScoreDocs[i].Doc).Get
					("parser"));
			}
			ir.Dispose();
			dir.Dispose();
		}

		private sealed class _ShortParser_1442 : FieldCache.ShortParser
		{
			public _ShortParser_1442()
			{
			}

			public short ParseShort(BytesRef term)
			{
				return (short)(term.bytes[term.offset] - 'A');
			}

			/// <exception cref="System.IO.IOException"></exception>
			public TermsEnum TermsEnum(Terms terms)
			{
				return terms.IEnumerator(null);
			}
		}

		/// <summary>test sorts for a custom long parser that uses a simple char encoding</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestCustomLongParser()
		{
			IList<string> letters = Arrays.AsList(new string[] { "A", "B", "C", "D", "E", "F"
				, "G", "H", "I", "J" });
			Collections.Shuffle(letters, Random());
			Directory dir = NewDirectory();
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir);
			foreach (string letter in letters)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(NewStringField("parser", letter, Field.Store.YES));
				iw.AddDocument(doc);
			}
			IndexReader ir = iw.Reader;
			iw.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("parser", new _LongParser_1486()), SortField.FIELD_DOC
				);
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			// results should be in alphabetical order
			AreEqual(10, td.TotalHits);
			letters.Sort();
			for (int i = 0; i < letters.Count; i++)
			{
				AreEqual(letters[i], searcher.Doc(td.ScoreDocs[i].Doc).Get
					("parser"));
			}
			ir.Dispose();
			dir.Dispose();
		}

		private sealed class _LongParser_1486 : FieldCache.LongParser
		{
			public _LongParser_1486()
			{
			}

			public long ParseLong(BytesRef term)
			{
				return (term.bytes[term.offset] - 'A') * 1234567890L;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public TermsEnum TermsEnum(Terms terms)
			{
				return terms.IEnumerator(null);
			}
		}

		/// <summary>test sorts for a custom float parser that uses a simple char encoding</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestCustomFloatParser()
		{
			IList<string> letters = Arrays.AsList(new string[] { "A", "B", "C", "D", "E", "F"
				, "G", "H", "I", "J" });
			Collections.Shuffle(letters, Random());
			Directory dir = NewDirectory();
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir);
			foreach (string letter in letters)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(NewStringField("parser", letter, Field.Store.YES));
				iw.AddDocument(doc);
			}
			IndexReader ir = iw.Reader;
			iw.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("parser", new _FloatParser_1530()), SortField.
				FIELD_DOC);
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			// results should be in alphabetical order
			AreEqual(10, td.TotalHits);
			letters.Sort();
			for (int i = 0; i < letters.Count; i++)
			{
				AreEqual(letters[i], searcher.Doc(td.ScoreDocs[i].Doc).Get
					("parser"));
			}
			ir.Dispose();
			dir.Dispose();
		}

		private sealed class _FloatParser_1530 : FieldCache.FloatParser
		{
			public _FloatParser_1530()
			{
			}

			public float ParseFloat(BytesRef term)
			{
				return (float)Math.Sqrt(term.bytes[term.offset]);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public TermsEnum TermsEnum(Terms terms)
			{
				return terms.IEnumerator(null);
			}
		}

		/// <summary>test sorts for a custom double parser that uses a simple char encoding</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestCustomDoubleParser()
		{
			IList<string> letters = Arrays.AsList(new string[] { "A", "B", "C", "D", "E", "F"
				, "G", "H", "I", "J" });
			Collections.Shuffle(letters, Random());
			Directory dir = NewDirectory();
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir);
			foreach (string letter in letters)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(NewStringField("parser", letter, Field.Store.YES));
				iw.AddDocument(doc);
			}
			IndexReader ir = iw.Reader;
			iw.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("parser", new _DoubleParser_1574()), SortField
				.FIELD_DOC);
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			// results should be in alphabetical order
			AreEqual(10, td.TotalHits);
			letters.Sort();
			for (int i = 0; i < letters.Count; i++)
			{
				AreEqual(letters[i], searcher.Doc(td.ScoreDocs[i].Doc).Get
					("parser"));
			}
			ir.Dispose();
			dir.Dispose();
		}

		private sealed class _DoubleParser_1574 : FieldCache.DoubleParser
		{
			public _DoubleParser_1574()
			{
			}

			public double ParseDouble(BytesRef term)
			{
				return Math.Pow(term.bytes[term.offset], (term.bytes[term.offset] - 'A'));
			}

			/// <exception cref="System.IO.IOException"></exception>
			public TermsEnum TermsEnum(Terms terms)
			{
				return terms.IEnumerator(null);
			}
		}

		/// <summary>Tests sorting a single document</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestSortOneDocument()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewStringField("value", "foo", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.STRING));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(1, td.TotalHits);
			AreEqual("foo", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"
				));
			ir.Dispose();
			dir.Dispose();
		}

		/// <summary>Tests sorting a single document with scores</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestSortOneDocumentWithScores()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewStringField("value", "foo", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(new SortField("value", SortField.Type.STRING));
			TopDocs expected = searcher.Search(new TermQuery(new Term("value", "foo")), 10);
			AreEqual(1, expected.TotalHits);
			TopDocs actual = searcher.Search(new TermQuery(new Term("value", "foo")), null, 10
				, sort, true, true);
			AreEqual(expected.TotalHits, actual.TotalHits);
			AreEqual(expected.ScoreDocs[0].score, actual.ScoreDocs[0].
				score, 0F);
			ir.Dispose();
			dir.Dispose();
		}

		/// <summary>Tests sorting with two fields</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestSortTwoFields()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewStringField("tievalue", "tied", Field.Store.NO));
			doc.Add(NewStringField("value", "foo", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("tievalue", "tied", Field.Store.NO));
			doc.Add(NewStringField("value", "bar", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader ir = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			// tievalue, then value
			Sort sort = new Sort(new SortField("tievalue", SortField.Type.STRING), new SortField
				("value", SortField.Type.STRING));
			TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
			AreEqual(2, td.TotalHits);
			// 'bar' comes before 'foo'
			AreEqual("bar", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"
				));
			AreEqual("foo", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"
				));
			ir.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestScore()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewStringField("value", "bar", Field.Store.NO));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("value", "foo", Field.Store.NO));
			writer.AddDocument(doc);
			IndexReader ir = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(ir);
			Sort sort = new Sort(SortField.FIELD_SCORE);
			BooleanQuery bq = new BooleanQuery();
			bq.Add(new TermQuery(new Term("value", "foo")), BooleanClause.Occur.SHOULD);
			bq.Add(new MatchAllDocsQuery(), BooleanClause.Occur.SHOULD);
			TopDocs td = searcher.Search(bq, 10, sort);
			AreEqual(2, td.TotalHits);
			AreEqual(1, td.ScoreDocs[0].Doc);
			AreEqual(0, td.ScoreDocs[1].Doc);
			ir.Dispose();
			dir.Dispose();
		}
	}
}
