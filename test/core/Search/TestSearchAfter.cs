/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Codecs;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search
{
	/// <summary>Tests IndexSearcher's searchAfter() method</summary>
	public class TestSearchAfter : LuceneTestCase
	{
		private Directory dir;

		private IndexReader reader;

		private IndexSearcher searcher;

		internal bool supportsDocValues = Codec.GetDefault().GetName().Equals("Lucene3x")
			 == false;

		private int iter;

		private IList<SortField> allSortFields;

		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			allSortFields = new List<SortField>(Arrays.AsList(new SortField[] { new SortField
				("byte", SortField.Type.BYTE, false), new SortField("short", SortField.Type.SHORT
				, false), new SortField("int", SortField.Type.INT, false), new SortField("long", 
				SortField.Type.LONG, false), new SortField("float", SortField.Type.FLOAT, false)
				, new SortField("double", SortField.Type.DOUBLE, false), new SortField("bytes", 
				SortField.Type.STRING, false), new SortField("bytesval", SortField.Type.STRING_VAL
				, false), new SortField("byte", SortField.Type.BYTE, true), new SortField("short"
				, SortField.Type.SHORT, true), new SortField("int", SortField.Type.INT, true), new 
				SortField("long", SortField.Type.LONG, true), new SortField("float", SortField.Type
				.FLOAT, true), new SortField("double", SortField.Type.DOUBLE, true), new SortField
				("bytes", SortField.Type.STRING, true), new SortField("bytesval", SortField.Type
				.STRING_VAL, true), SortField.FIELD_SCORE, SortField.FIELD_DOC }));
			if (supportsDocValues)
			{
				Sharpen.Collections.AddAll(allSortFields, Arrays.AsList(new SortField[] { new SortField
					("intdocvalues", SortField.Type.INT, false), new SortField("floatdocvalues", SortField.Type
					.FLOAT, false), new SortField("sortedbytesdocvalues", SortField.Type.STRING, false
					), new SortField("sortedbytesdocvaluesval", SortField.Type.STRING_VAL, false), new 
					SortField("straightbytesdocvalues", SortField.Type.STRING_VAL, false), new SortField
					("intdocvalues", SortField.Type.INT, true), new SortField("floatdocvalues", SortField.Type
					.FLOAT, true), new SortField("sortedbytesdocvalues", SortField.Type.STRING, true
					), new SortField("sortedbytesdocvaluesval", SortField.Type.STRING_VAL, true), new 
					SortField("straightbytesdocvalues", SortField.Type.STRING_VAL, true) }));
			}
			// Also test missing first / last for the "string" sorts:
			foreach (string field in new string[] { "bytes", "sortedbytesdocvalues" })
			{
				for (int rev = 0; rev < 2; rev++)
				{
					bool reversed = rev == 0;
					SortField sf = new SortField(field, SortField.Type.STRING, reversed);
					sf.SetMissingValue(SortField.STRING_FIRST);
					allSortFields.Add(sf);
					sf = new SortField(field, SortField.Type.STRING, reversed);
					sf.SetMissingValue(SortField.STRING_LAST);
					allSortFields.Add(sf);
				}
			}
			int limit = allSortFields.Count;
			for (int i = 0; i < limit; i++)
			{
				SortField sf = allSortFields[i];
				if (sf.GetType() == SortField.Type.INT)
				{
					SortField sf2 = new SortField(sf.GetField(), SortField.Type.INT, sf.GetReverse());
					sf2.SetMissingValue(Random().Next());
					allSortFields.Add(sf2);
				}
				else
				{
					if (sf.GetType() == SortField.Type.LONG)
					{
						SortField sf2 = new SortField(sf.GetField(), SortField.Type.LONG, sf.GetReverse()
							);
						sf2.SetMissingValue(Random().NextLong());
						allSortFields.Add(sf2);
					}
					else
					{
						if (sf.GetType() == SortField.Type.FLOAT)
						{
							SortField sf2 = new SortField(sf.GetField(), SortField.Type.FLOAT, sf.GetReverse(
								));
							sf2.SetMissingValue(Random().NextFloat());
							allSortFields.Add(sf2);
						}
						else
						{
							if (sf.GetType() == SortField.Type.DOUBLE)
							{
								SortField sf2 = new SortField(sf.GetField(), SortField.Type.DOUBLE, sf.GetReverse
									());
								sf2.SetMissingValue(Random().NextDouble());
								allSortFields.Add(sf2);
							}
						}
					}
				}
			}
			dir = NewDirectory();
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir);
			int numDocs = AtLeast(200);
			for (int i_1 = 0; i_1 < numDocs; i_1++)
			{
				IList<Field> fields = new List<Field>();
				fields.Add(NewTextField("english", English.IntToEnglish(i_1), Field.Store.NO)
					);
				fields.Add(NewTextField("oddeven", (i_1 % 2 == 0) ? "even" : "odd", Field.Store
					.NO));
				fields.Add(NewStringField("byte", string.Empty + (unchecked((byte)Random().Next
					())), Field.Store.NO));
				fields.Add(NewStringField("short", string.Empty + ((short)Random().Next()), Field.Store
					.NO));
				fields.Add(new IntField("int", Random().Next(), Field.Store.NO));
				fields.Add(new LongField("long", Random().NextLong(), Field.Store.NO));
				fields.Add(new FloatField("float", Random().NextFloat(), Field.Store.NO));
				fields.Add(new DoubleField("double", Random().NextDouble(), Field.Store.NO));
				fields.Add(NewStringField("bytes", TestUtil.RandomRealisticUnicodeString(Random
					()), Field.Store.NO));
				fields.Add(NewStringField("bytesval", TestUtil.RandomRealisticUnicodeString(Random
					()), Field.Store.NO));
				fields.Add(new DoubleField("double", Random().NextDouble(), Field.Store.NO));
				if (supportsDocValues)
				{
					fields.Add(new NumericDocValuesField("intdocvalues", Random().Next()));
					fields.Add(new FloatDocValuesField("floatdocvalues", Random().NextFloat()));
					fields.Add(new SortedDocValuesField("sortedbytesdocvalues", new BytesRef(TestUtil
						.RandomRealisticUnicodeString(Random()))));
					fields.Add(new SortedDocValuesField("sortedbytesdocvaluesval", new BytesRef(TestUtil
						.RandomRealisticUnicodeString(Random()))));
					fields.Add(new BinaryDocValuesField("straightbytesdocvalues", new BytesRef(TestUtil
						.RandomRealisticUnicodeString(Random()))));
				}
				Lucene.Net.Documents.Document document = new Lucene.Net.Documents.Document
					();
				document.Add(new StoredField("id", string.Empty + i_1));
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("  add doc id=" + i_1);
				}
				foreach (Field field_1 in fields)
				{
					// So we are sometimes missing that field:
					if (Random().Next(5) != 4)
					{
						document.Add(field_1);
						if (VERBOSE)
						{
							System.Console.Out.WriteLine("    " + field_1);
						}
					}
				}
				iw.AddDocument(document);
				if (Random().Next(50) == 17)
				{
					iw.Commit();
				}
			}
			reader = iw.Reader;
			iw.Dispose();
			searcher = NewSearcher(reader);
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("  searcher=" + searcher);
			}
		}

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			reader.Dispose();
			dir.Dispose();
			base.TearDown();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestQueries()
		{
			// because the first page has a null 'after', we get a normal collector.
			// so we need to run the test a few times to ensure we will collect multiple
			// pages.
			int n = AtLeast(20);
			for (int i = 0; i < n; i++)
			{
				Filter odd = new QueryWrapperFilter(new TermQuery(new Term("oddeven", "odd")));
				AssertQuery(new MatchAllDocsQuery(), null);
				AssertQuery(new TermQuery(new Term("english", "one")), null);
				AssertQuery(new MatchAllDocsQuery(), odd);
				AssertQuery(new TermQuery(new Term("english", "four")), odd);
				BooleanQuery bq = new BooleanQuery();
				bq.Add(new TermQuery(new Term("english", "one")), BooleanClause.Occur.SHOULD);
				bq.Add(new TermQuery(new Term("oddeven", "even")), BooleanClause.Occur.SHOULD);
				AssertQuery(bq, null);
			}
		}

		/// <exception cref="System.Exception"></exception>
		internal virtual void AssertQuery(Query query, Filter filter)
		{
			AssertQuery(query, filter, null);
			AssertQuery(query, filter, Sort.RELEVANCE);
			AssertQuery(query, filter, Sort.INDEXORDER);
			foreach (SortField sortField in allSortFields)
			{
				AssertQuery(query, filter, new Sort(new SortField[] { sortField }));
			}
			for (int i = 0; i < 20; i++)
			{
				AssertQuery(query, filter, GetRandomSort());
			}
		}

		internal virtual Sort GetRandomSort()
		{
			SortField[] sortFields = new SortField[TestUtil.NextInt(Random(), 2, 7)];
			for (int i = 0; i < sortFields.Length; i++)
			{
				sortFields[i] = allSortFields[Random().Next(allSortFields.Count)];
			}
			return new Sort(sortFields);
		}

		/// <exception cref="System.Exception"></exception>
		internal virtual void AssertQuery(Query query, Filter filter, Sort sort)
		{
			int maxDoc = searcher.IndexReader.MaxDoc;
			TopDocs all;
			int pageSize = TestUtil.NextInt(Random(), 1, maxDoc * 2);
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("\nassertQuery " + (iter++) + ": query=" + query + " filter="
					 + filter + " sort=" + sort + " pageSize=" + pageSize);
			}
			bool doMaxScore = Random().NextBoolean();
			bool doScores = Random().NextBoolean();
			if (sort == null)
			{
				all = searcher.Search(query, filter, maxDoc);
			}
			else
			{
				if (sort == Sort.RELEVANCE)
				{
					all = searcher.Search(query, filter, maxDoc, sort, true, doMaxScore);
				}
				else
				{
					all = searcher.Search(query, filter, maxDoc, sort, doScores, doMaxScore);
				}
			}
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("  all.TotalHits=" + all.TotalHits);
				int upto = 0;
				foreach (ScoreDoc scoreDoc in all.ScoreDocs)
				{
					System.Console.Out.WriteLine("    hit " + (upto++) + ": id=" + searcher.Doc(scoreDoc
						.Doc).Get("id") + " " + scoreDoc);
				}
			}
			int pageStart = 0;
			ScoreDoc lastBottom = null;
			while (pageStart < all.TotalHits)
			{
				TopDocs paged;
				if (sort == null)
				{
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("  iter lastBottom=" + lastBottom);
					}
					paged = searcher.SearchAfter(lastBottom, query, filter, pageSize);
				}
				else
				{
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("  iter lastBottom=" + lastBottom);
					}
					if (sort == Sort.RELEVANCE)
					{
						paged = searcher.SearchAfter(lastBottom, query, filter, pageSize, sort, true, doMaxScore
							);
					}
					else
					{
						paged = searcher.SearchAfter(lastBottom, query, filter, pageSize, sort, doScores, 
							doMaxScore);
					}
				}
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("    " + paged.ScoreDocs.Length + " hits on page");
				}
				if (paged.ScoreDocs.Length == 0)
				{
					break;
				}
				AssertPage(pageStart, all, paged);
				pageStart += paged.ScoreDocs.Length;
				lastBottom = paged.ScoreDocs[paged.ScoreDocs.Length - 1];
			}
			AreEqual(all.ScoreDocs.Length, pageStart);
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void AssertPage(int pageStart, TopDocs all, TopDocs paged)
		{
			AreEqual(all.TotalHits, paged.TotalHits);
			for (int i = 0; i < paged.ScoreDocs.Length; i++)
			{
				ScoreDoc sd1 = all.ScoreDocs[pageStart + i];
				ScoreDoc sd2 = paged.ScoreDocs[i];
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("    hit " + (pageStart + i));
					System.Console.Out.WriteLine("      expected id=" + searcher.Doc(sd1.Doc).Get("id"
						) + " " + sd1);
					System.Console.Out.WriteLine("        actual id=" + searcher.Doc(sd2.Doc).Get("id"
						) + " " + sd2);
				}
				AreEqual(sd1.Doc, sd2.Doc);
				AreEqual(sd1.score, sd2.score, 0f);
				if (sd1 is FieldDoc)
				{
					IsTrue(sd2 is FieldDoc);
					AreEqual(((FieldDoc)sd1).fields, ((FieldDoc)sd2).fields);
				}
			}
		}
	}
}
