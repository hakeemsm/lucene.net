/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search
{
	public class TestNumericRangeQuery64 : LuceneTestCase
	{
		private static long distance;

		private const long startOffset = -1L << 31;

		private static int noDocs;

		private static Directory directory = null;

		private static IndexReader reader = null;

		private static IndexSearcher searcher = null;

		// NaN arrays
		// distance of entries
		// shift the starting of the values to the left, to also have negative values:
		// number of docs to generate for testing
		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.BeforeClass]
		public static void BeforeClass()
		{
			noDocs = AtLeast(4096);
			distance = (1L << 60) / noDocs;
			directory = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), directory, ((IndexWriterConfig
				)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs
				(TestUtil.NextInt(Random(), 100, 1000))).SetMergePolicy(NewLogMergePolicy()));
			FieldType storedLong = new FieldType(LongField.TYPE_NOT_STORED);
			storedLong.Stored = (true);
			storedLong.Freeze();
			FieldType storedLong8 = new FieldType(storedLong);
			storedLong8.SetNumericPrecisionStep(8);
			FieldType storedLong4 = new FieldType(storedLong);
			storedLong4.SetNumericPrecisionStep(4);
			FieldType storedLong6 = new FieldType(storedLong);
			storedLong6.SetNumericPrecisionStep(6);
			FieldType storedLong2 = new FieldType(storedLong);
			storedLong2.SetNumericPrecisionStep(2);
			FieldType storedLongNone = new FieldType(storedLong);
			storedLongNone.SetNumericPrecisionStep(int.MaxValue);
			FieldType unstoredLong = LongField.TYPE_NOT_STORED;
			FieldType unstoredLong8 = new FieldType(unstoredLong);
			unstoredLong8.SetNumericPrecisionStep(8);
			FieldType unstoredLong6 = new FieldType(unstoredLong);
			unstoredLong6.SetNumericPrecisionStep(6);
			FieldType unstoredLong4 = new FieldType(unstoredLong);
			unstoredLong4.SetNumericPrecisionStep(4);
			FieldType unstoredLong2 = new FieldType(unstoredLong);
			unstoredLong2.SetNumericPrecisionStep(2);
			LongField field8 = new LongField("field8", 0L, storedLong8);
			LongField field6 = new LongField("field6", 0L, storedLong6);
			LongField field4 = new LongField("field4", 0L, storedLong4);
			LongField field2 = new LongField("field2", 0L, storedLong2);
			LongField fieldNoTrie = new LongField("field" + int.MaxValue, 0L, storedLongNone);
			LongField ascfield8 = new LongField("ascfield8", 0L, unstoredLong8);
			LongField ascfield6 = new LongField("ascfield6", 0L, unstoredLong6);
			LongField ascfield4 = new LongField("ascfield4", 0L, unstoredLong4);
			LongField ascfield2 = new LongField("ascfield2", 0L, unstoredLong2);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			// add fields, that have a distance to test general functionality
			doc.Add(field8);
			doc.Add(field6);
			doc.Add(field4);
			doc.Add(field2);
			doc.Add(fieldNoTrie);
			// add ascending fields with a distance of 1, beginning at -noDocs/2 to test the correct splitting of range and inclusive/exclusive
			doc.Add(ascfield8);
			doc.Add(ascfield6);
			doc.Add(ascfield4);
			doc.Add(ascfield2);
			// Add a series of noDocs docs with increasing long values, by updating the fields
			for (int l = 0; l < noDocs; l++)
			{
				long val = distance * l + startOffset;
				field8.SetLongValue(val);
				field6.SetLongValue(val);
				field4.SetLongValue(val);
				field2.SetLongValue(val);
				fieldNoTrie.SetLongValue(val);
				val = l - (noDocs / 2);
				ascfield8.SetLongValue(val);
				ascfield6.SetLongValue(val);
				ascfield4.SetLongValue(val);
				ascfield2.SetLongValue(val);
				writer.AddDocument(doc);
			}
			reader = writer.GetReader();
			searcher = NewSearcher(reader);
			writer.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.AfterClass]
		public static void AfterClass()
		{
			searcher = null;
			reader.Dispose();
			reader = null;
			directory.Dispose();
			directory = null;
		}

		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			// set the theoretical maximum term count for 8bit (see docs for the number)
			// super.tearDown will restore the default
			BooleanQuery.SetMaxClauseCount(7 * 255 * 2 + 255);
		}

		/// <summary>test for constant score + boolean query + filter, the other tests only use the constant score mode
		/// 	</summary>
		/// <exception cref="System.Exception"></exception>
		private void TestRange(int precisionStep)
		{
			string field = "field" + precisionStep;
			int count = 3000;
			long lower = (distance * 3 / 2) + startOffset;
			long upper = lower + count * distance + (distance / 3);
			NumericRangeQuery<long> q = NumericRangeQuery.NewLongRange(field, precisionStep, 
				lower, upper, true, true);
			NumericRangeFilter<long> f = NumericRangeFilter.NewLongRange(field, precisionStep
				, lower, upper, true, true);
			for (byte i = 0; ((sbyte)i) < 3; i++)
			{
				TopDocs topDocs;
				string type;
				switch (i)
				{
					case 0:
					{
						type = " (constant score filter rewrite)";
						q.SetRewriteMethod(MultiTermQuery.CONSTANT_SCORE_FILTER_REWRITE);
						topDocs = searcher.Search(q, null, noDocs, Sort.INDEXORDER);
						break;
					}

					case 1:
					{
						type = " (constant score boolean rewrite)";
						q.SetRewriteMethod(MultiTermQuery.CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE);
						topDocs = searcher.Search(q, null, noDocs, Sort.INDEXORDER);
						break;
					}

					case 2:
					{
						type = " (filter)";
						topDocs = searcher.Search(new MatchAllDocsQuery(), f, noDocs, Sort.INDEXORDER);
						break;
					}

					default:
					{
						return;
						break;
					}
				}
				ScoreDoc[] sd = topDocs.ScoreDocs;
				IsNotNull(sd);
				AreEqual("Score doc count" + type, count, sd.Length);
				Lucene.Net.Documents.Document doc = searcher.Doc(sd[0].Doc);
				AreEqual("First doc" + type, 2 * distance + startOffset, doc
					.GetField(field).NumericValue());
				doc = searcher.Doc(sd[sd.Length - 1].Doc);
				AreEqual("Last doc" + type, (1 + count) * distance + startOffset
					, doc.GetField(field).NumericValue());
			}
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestRange_8bit()
		{
			TestRange(8);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestRange_6bit()
		{
			TestRange(6);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestRange_4bit()
		{
			TestRange(4);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestRange_2bit()
		{
			TestRange(2);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestInverseRange()
		{
			AtomicReaderContext context = ((AtomicReaderContext)SlowCompositeReaderWrapper.Wrap
				(searcher.IndexReader).GetContext());
			NumericRangeFilter<long> f = NumericRangeFilter.NewLongRange("field8", 8, 1000L, 
				-1000L, true, true);
			IsNull("A inverse range should return the null instance", 
				f.GetDocIdSet(context, ((AtomicReader)context.Reader).LiveDocs));
			f = NumericRangeFilter.NewLongRange("field8", 8, long.MaxValue, null, false, false
				);
			IsNull("A exclusive range starting with Long.MAX_VALUE should return the null instance"
				, f.GetDocIdSet(context, ((AtomicReader)context.Reader).LiveDocs));
			f = NumericRangeFilter.NewLongRange("field8", 8, null, long.MinValue, false, false
				);
			IsNull("A exclusive range ending with Long.MIN_VALUE should return the null instance"
				, f.GetDocIdSet(context, ((AtomicReader)context.Reader).LiveDocs));
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestOneMatchQuery()
		{
			NumericRangeQuery<long> q = NumericRangeQuery.NewLongRange("ascfield8", 8, 1000L, 
				1000L, true, true);
			TopDocs topDocs = searcher.Search(q, noDocs);
			ScoreDoc[] sd = topDocs.ScoreDocs;
			IsNotNull(sd);
			AreEqual("Score doc count", 1, sd.Length);
		}

		/// <exception cref="System.Exception"></exception>
		private void TestLeftOpenRange(int precisionStep)
		{
			string field = "field" + precisionStep;
			int count = 3000;
			long upper = (count - 1) * distance + (distance / 3) + startOffset;
			NumericRangeQuery<long> q = NumericRangeQuery.NewLongRange(field, precisionStep, 
				null, upper, true, true);
			TopDocs topDocs = searcher.Search(q, null, noDocs, Sort.INDEXORDER);
			ScoreDoc[] sd = topDocs.ScoreDocs;
			IsNotNull(sd);
			AreEqual("Score doc count", count, sd.Length);
			Lucene.Net.Documents.Document doc = searcher.Doc(sd[0].Doc);
			AreEqual("First doc", startOffset, doc.GetField(field).NumericValue
				());
			doc = searcher.Doc(sd[sd.Length - 1].Doc);
			AreEqual("Last doc", (count - 1) * distance + startOffset, 
				doc.GetField(field).NumericValue());
			q = NumericRangeQuery.NewLongRange(field, precisionStep, null, upper, false, true
				);
			topDocs = searcher.Search(q, null, noDocs, Sort.INDEXORDER);
			sd = topDocs.ScoreDocs;
			IsNotNull(sd);
			AreEqual("Score doc count", count, sd.Length);
			doc = searcher.Doc(sd[0].Doc);
			AreEqual("First doc", startOffset, doc.GetField(field).NumericValue
				());
			doc = searcher.Doc(sd[sd.Length - 1].Doc);
			AreEqual("Last doc", (count - 1) * distance + startOffset, 
				doc.GetField(field).NumericValue());
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestLeftOpenRange_8bit()
		{
			TestLeftOpenRange(8);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestLeftOpenRange_6bit()
		{
			TestLeftOpenRange(6);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestLeftOpenRange_4bit()
		{
			TestLeftOpenRange(4);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestLeftOpenRange_2bit()
		{
			TestLeftOpenRange(2);
		}

		/// <exception cref="System.Exception"></exception>
		private void TestRightOpenRange(int precisionStep)
		{
			string field = "field" + precisionStep;
			int count = 3000;
			long lower = (count - 1) * distance + (distance / 3) + startOffset;
			NumericRangeQuery<long> q = NumericRangeQuery.NewLongRange(field, precisionStep, 
				lower, null, true, true);
			TopDocs topDocs = searcher.Search(q, null, noDocs, Sort.INDEXORDER);
			ScoreDoc[] sd = topDocs.ScoreDocs;
			IsNotNull(sd);
			AreEqual("Score doc count", noDocs - count, sd.Length);
			Lucene.Net.Documents.Document doc = searcher.Doc(sd[0].Doc);
			AreEqual("First doc", count * distance + startOffset, doc.
				GetField(field).NumericValue());
			doc = searcher.Doc(sd[sd.Length - 1].Doc);
			AreEqual("Last doc", (noDocs - 1) * distance + startOffset
				, doc.GetField(field).NumericValue());
			q = NumericRangeQuery.NewLongRange(field, precisionStep, lower, null, true, false
				);
			topDocs = searcher.Search(q, null, noDocs, Sort.INDEXORDER);
			sd = topDocs.ScoreDocs;
			IsNotNull(sd);
			AreEqual("Score doc count", noDocs - count, sd.Length);
			doc = searcher.Doc(sd[0].Doc);
			AreEqual("First doc", count * distance + startOffset, doc.
				GetField(field).NumericValue());
			doc = searcher.Doc(sd[sd.Length - 1].Doc);
			AreEqual("Last doc", (noDocs - 1) * distance + startOffset
				, doc.GetField(field).NumericValue());
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestRightOpenRange_8bit()
		{
			TestRightOpenRange(8);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestRightOpenRange_6bit()
		{
			TestRightOpenRange(6);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestRightOpenRange_4bit()
		{
			TestRightOpenRange(4);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestRightOpenRange_2bit()
		{
			TestRightOpenRange(2);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestInfiniteValues()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new DoubleField("double", double.NegativeInfinity, Field.Store.NO));
			doc.Add(new LongField("long", long.MinValue, Field.Store.NO));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new DoubleField("double", double.PositiveInfinity, Field.Store.NO));
			doc.Add(new LongField("long", long.MaxValue, Field.Store.NO));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new DoubleField("double", 0.0, Field.Store.NO));
			doc.Add(new LongField("long", 0L, Field.Store.NO));
			writer.AddDocument(doc);
			foreach (double d in TestNumericUtils.DOUBLE_NANs)
			{
				doc = new Lucene.Net.Documents.Document();
				doc.Add(new DoubleField("double", d, Field.Store.NO));
				writer.AddDocument(doc);
			}
			writer.Dispose();
			IndexReader r = DirectoryReader.Open(dir);
			IndexSearcher s = NewSearcher(r);
			Query q = NumericRangeQuery.NewLongRange("long", null, null, true, true);
			TopDocs topDocs = s.Search(q, 10);
			AreEqual("Score doc count", 3, topDocs.ScoreDocs.Length);
			q = NumericRangeQuery.NewLongRange("long", null, null, false, false);
			topDocs = s.Search(q, 10);
			AreEqual("Score doc count", 3, topDocs.ScoreDocs.Length);
			q = NumericRangeQuery.NewLongRange("long", long.MinValue, long.MaxValue, true, true
				);
			topDocs = s.Search(q, 10);
			AreEqual("Score doc count", 3, topDocs.ScoreDocs.Length);
			q = NumericRangeQuery.NewLongRange("long", long.MinValue, long.MaxValue, false, false
				);
			topDocs = s.Search(q, 10);
			AreEqual("Score doc count", 1, topDocs.ScoreDocs.Length);
			q = NumericRangeQuery.NewDoubleRange("double", null, null, true, true);
			topDocs = s.Search(q, 10);
			AreEqual("Score doc count", 3, topDocs.ScoreDocs.Length);
			q = NumericRangeQuery.NewDoubleRange("double", null, null, false, false);
			topDocs = s.Search(q, 10);
			AreEqual("Score doc count", 3, topDocs.ScoreDocs.Length);
			q = NumericRangeQuery.NewDoubleRange("double", double.NegativeInfinity, double.PositiveInfinity
				, true, true);
			topDocs = s.Search(q, 10);
			AreEqual("Score doc count", 3, topDocs.ScoreDocs.Length);
			q = NumericRangeQuery.NewDoubleRange("double", double.NegativeInfinity, double.PositiveInfinity
				, false, false);
			topDocs = s.Search(q, 10);
			AreEqual("Score doc count", 1, topDocs.ScoreDocs.Length);
			q = NumericRangeQuery.NewDoubleRange("double", double.NaN, double.NaN, true, true
				);
			topDocs = s.Search(q, 10);
			AreEqual("Score doc count", TestNumericUtils.DOUBLE_NANs.Length
				, topDocs.ScoreDocs.Length);
			r.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		private void TestRandomTrieAndClassicRangeQuery(int precisionStep)
		{
			string field = "field" + precisionStep;
			int totalTermCountT = 0;
			int totalTermCountC = 0;
			int termCountT;
			int termCountC;
			int num = TestUtil.NextInt(Random(), 10, 20);
			for (int i = 0; i < num; i++)
			{
				long lower = (long)(Random().NextDouble() * noDocs * distance) + startOffset;
				long upper = (long)(Random().NextDouble() * noDocs * distance) + startOffset;
				if (lower > upper)
				{
					long a = lower;
					lower = upper;
					upper = a;
				}
				BytesRef lowerBytes = new BytesRef(NumericUtils.BUF_SIZE_LONG);
				BytesRef upperBytes = new BytesRef(NumericUtils.BUF_SIZE_LONG);
				NumericUtils.LongToPrefixCodedBytes(lower, 0, lowerBytes);
				NumericUtils.LongToPrefixCodedBytes(upper, 0, upperBytes);
				// test inclusive range
				NumericRangeQuery<long> tq = NumericRangeQuery.NewLongRange(field, precisionStep, 
					lower, upper, true, true);
				TermRangeQuery cq = new TermRangeQuery(field, lowerBytes, upperBytes, true, true);
				TopDocs tTopDocs = searcher.Search(tq, 1);
				TopDocs cTopDocs = searcher.Search(cq, 1);
				AreEqual("Returned count for NumericRangeQuery and TermRangeQuery must be equal"
					, cTopDocs.TotalHits, tTopDocs.TotalHits);
				totalTermCountT += termCountT = CountTerms(tq);
				totalTermCountC += termCountC = CountTerms(cq);
				CheckTermCounts(precisionStep, termCountT, termCountC);
				// test exclusive range
				tq = NumericRangeQuery.NewLongRange(field, precisionStep, lower, upper, false, false
					);
				cq = new TermRangeQuery(field, lowerBytes, upperBytes, false, false);
				tTopDocs = searcher.Search(tq, 1);
				cTopDocs = searcher.Search(cq, 1);
				AreEqual("Returned count for NumericRangeQuery and TermRangeQuery must be equal"
					, cTopDocs.TotalHits, tTopDocs.TotalHits);
				totalTermCountT += termCountT = CountTerms(tq);
				totalTermCountC += termCountC = CountTerms(cq);
				CheckTermCounts(precisionStep, termCountT, termCountC);
				// test left exclusive range
				tq = NumericRangeQuery.NewLongRange(field, precisionStep, lower, upper, false, true
					);
				cq = new TermRangeQuery(field, lowerBytes, upperBytes, false, true);
				tTopDocs = searcher.Search(tq, 1);
				cTopDocs = searcher.Search(cq, 1);
				AreEqual("Returned count for NumericRangeQuery and TermRangeQuery must be equal"
					, cTopDocs.TotalHits, tTopDocs.TotalHits);
				totalTermCountT += termCountT = CountTerms(tq);
				totalTermCountC += termCountC = CountTerms(cq);
				CheckTermCounts(precisionStep, termCountT, termCountC);
				// test right exclusive range
				tq = NumericRangeQuery.NewLongRange(field, precisionStep, lower, upper, true, false
					);
				cq = new TermRangeQuery(field, lowerBytes, upperBytes, true, false);
				tTopDocs = searcher.Search(tq, 1);
				cTopDocs = searcher.Search(cq, 1);
				AreEqual("Returned count for NumericRangeQuery and TermRangeQuery must be equal"
					, cTopDocs.TotalHits, tTopDocs.TotalHits);
				totalTermCountT += termCountT = CountTerms(tq);
				totalTermCountC += termCountC = CountTerms(cq);
				CheckTermCounts(precisionStep, termCountT, termCountC);
			}
			CheckTermCounts(precisionStep, totalTermCountT, totalTermCountC);
			if (VERBOSE && precisionStep != int.MaxValue)
			{
				System.Console.Out.WriteLine("Average number of terms during random search on '" 
					+ field + "':");
				System.Console.Out.WriteLine(" Numeric query: " + (((double)totalTermCountT) / (num
					 * 4)));
				System.Console.Out.WriteLine(" Classical query: " + (((double)totalTermCountC) / 
					(num * 4)));
			}
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestEmptyEnums()
		{
			int count = 3000;
			long lower = (distance * 3 / 2) + startOffset;
			long upper = lower + count * distance + (distance / 3);
			// test empty enum
			//HM:revisit 
			//assert lower < upper;
			IsTrue(0 < CountTerms(NumericRangeQuery.NewLongRange("field4"
				, 4, lower, upper, true, true)));
			AreEqual(0, CountTerms(NumericRangeQuery.NewLongRange("field4"
				, 4, upper, lower, true, true)));
			// test empty enum outside of bounds
			lower = distance * noDocs + startOffset;
			upper = 2L * lower;
			//HM:revisit 
			//assert lower < upper;
			AreEqual(0, CountTerms(NumericRangeQuery.NewLongRange("field4"
				, 4, lower, upper, true, true)));
		}

		/// <exception cref="System.Exception"></exception>
		private int CountTerms(MultiTermQuery q)
		{
			Terms terms = MultiFields.GetTerms(reader, q.GetField());
			if (terms == null)
			{
				return 0;
			}
			TermsEnum termEnum = q.GetTermsEnum(terms);
			IsNotNull(termEnum);
			int count = 0;
			BytesRef cur;
			BytesRef last = null;
			while ((cur = termEnum.Next()) != null)
			{
				count++;
				if (last != null)
				{
					IsTrue(last.CompareTo(cur) < 0);
				}
				last = BytesRef.DeepCopyOf(cur);
			}
			// LUCENE-3314: the results after next() already returned null are undefined,
			// assertNull(termEnum.next());
			return count;
		}

		private void CheckTermCounts(int precisionStep, int termCountT, int termCountC)
		{
			if (precisionStep == int.MaxValue)
			{
				AreEqual("Number of terms should be equal for unlimited precStep"
					, termCountC, termCountT);
			}
			else
			{
				IsTrue("Number of terms for NRQ should be <= compared to classical TRQ"
					, termCountT <= termCountC);
			}
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestRandomTrieAndClassicRangeQuery_8bit()
		{
			TestRandomTrieAndClassicRangeQuery(8);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestRandomTrieAndClassicRangeQuery_6bit()
		{
			TestRandomTrieAndClassicRangeQuery(6);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestRandomTrieAndClassicRangeQuery_4bit()
		{
			TestRandomTrieAndClassicRangeQuery(4);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestRandomTrieAndClassicRangeQuery_2bit()
		{
			TestRandomTrieAndClassicRangeQuery(2);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestRandomTrieAndClassicRangeQuery_NoTrie()
		{
			TestRandomTrieAndClassicRangeQuery(int.MaxValue);
		}

		/// <exception cref="System.Exception"></exception>
		private void TestRangeSplit(int precisionStep)
		{
			string field = "ascfield" + precisionStep;
			// 10 random tests
			int num = TestUtil.NextInt(Random(), 10, 20);
			for (int i = 0; i < num; i++)
			{
				long lower = (long)(Random().NextDouble() * noDocs - noDocs / 2);
				long upper = (long)(Random().NextDouble() * noDocs - noDocs / 2);
				if (lower > upper)
				{
					long a = lower;
					lower = upper;
					upper = a;
				}
				// test inclusive range
				Query tq = NumericRangeQuery.NewLongRange(field, precisionStep, lower, upper, true
					, true);
				TopDocs tTopDocs = searcher.Search(tq, 1);
				AreEqual("Returned count of range query must be equal to inclusive range length"
					, upper - lower + 1, tTopDocs.TotalHits);
				// test exclusive range
				tq = NumericRangeQuery.NewLongRange(field, precisionStep, lower, upper, false, false
					);
				tTopDocs = searcher.Search(tq, 1);
				AreEqual("Returned count of range query must be equal to exclusive range length"
					, Math.Max(upper - lower - 1, 0), tTopDocs.TotalHits);
				// test left exclusive range
				tq = NumericRangeQuery.NewLongRange(field, precisionStep, lower, upper, false, true
					);
				tTopDocs = searcher.Search(tq, 1);
				AreEqual("Returned count of range query must be equal to half exclusive range length"
					, upper - lower, tTopDocs.TotalHits);
				// test right exclusive range
				tq = NumericRangeQuery.NewLongRange(field, precisionStep, lower, upper, true, false
					);
				tTopDocs = searcher.Search(tq, 1);
				AreEqual("Returned count of range query must be equal to half exclusive range length"
					, upper - lower, tTopDocs.TotalHits);
			}
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestRangeSplit_8bit()
		{
			TestRangeSplit(8);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestRangeSplit_6bit()
		{
			TestRangeSplit(6);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestRangeSplit_4bit()
		{
			TestRangeSplit(4);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestRangeSplit_2bit()
		{
			TestRangeSplit(2);
		}

		/// <summary>we fake a double test using long2double conversion of NumericUtils</summary>
		/// <exception cref="System.Exception"></exception>
		private void TestDoubleRange(int precisionStep)
		{
			string field = "ascfield" + precisionStep;
			long lower = -1000L;
			long upper = +2000L;
			Query tq = NumericRangeQuery.NewDoubleRange(field, precisionStep, NumericUtils.SortableLongToDouble
				(lower), NumericUtils.SortableLongToDouble(upper), true, true);
			TopDocs tTopDocs = searcher.Search(tq, 1);
			AreEqual("Returned count of range query must be equal to inclusive range length"
				, upper - lower + 1, tTopDocs.TotalHits);
			Filter tf = NumericRangeFilter.NewDoubleRange(field, precisionStep, NumericUtils.
				SortableLongToDouble(lower), NumericUtils.SortableLongToDouble(upper), true, true
				);
			tTopDocs = searcher.Search(new MatchAllDocsQuery(), tf, 1);
			AreEqual("Returned count of range filter must be equal to inclusive range length"
				, upper - lower + 1, tTopDocs.TotalHits);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestDoubleRange_8bit()
		{
			TestDoubleRange(8);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestDoubleRange_6bit()
		{
			TestDoubleRange(6);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestDoubleRange_4bit()
		{
			TestDoubleRange(4);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestDoubleRange_2bit()
		{
			TestDoubleRange(2);
		}

		/// <exception cref="System.Exception"></exception>
		private void TestSorting(int precisionStep)
		{
			string field = "field" + precisionStep;
			// 10 random tests, the index order is ascending,
			// so using a reverse sort field should retun descending documents
			int num = TestUtil.NextInt(Random(), 10, 20);
			for (int i = 0; i < num; i++)
			{
				long lower = (long)(Random().NextDouble() * noDocs * distance) + startOffset;
				long upper = (long)(Random().NextDouble() * noDocs * distance) + startOffset;
				if (lower > upper)
				{
					long a = lower;
					lower = upper;
					upper = a;
				}
				Query tq = NumericRangeQuery.NewLongRange(field, precisionStep, lower, upper, true
					, true);
				TopDocs topDocs = searcher.Search(tq, null, noDocs, new Sort(new SortField(field, 
					SortField.Type.LONG, true)));
				if (topDocs.TotalHits == 0)
				{
					continue;
				}
				ScoreDoc[] sd = topDocs.ScoreDocs;
				IsNotNull(sd);
				long last = searcher.Doc(sd[0].Doc).GetField(field).NumericValue();
				for (int j = 1; j < sd.Length; j++)
				{
					long act = searcher.Doc(sd[j].Doc).GetField(field).NumericValue();
					IsTrue("Docs should be sorted backwards", last > act);
					last = act;
				}
			}
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestSorting_8bit()
		{
			TestSorting(8);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestSorting_6bit()
		{
			TestSorting(6);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestSorting_4bit()
		{
			TestSorting(4);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestSorting_2bit()
		{
			TestSorting(2);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestEqualsAndHash()
		{
			QueryUtils.CheckHashEquals(NumericRangeQuery.NewLongRange("test1", 4, 10L, 20L, true
				, true));
			QueryUtils.CheckHashEquals(NumericRangeQuery.NewLongRange("test2", 4, 10L, 20L, false
				, true));
			QueryUtils.CheckHashEquals(NumericRangeQuery.NewLongRange("test3", 4, 10L, 20L, true
				, false));
			QueryUtils.CheckHashEquals(NumericRangeQuery.NewLongRange("test4", 4, 10L, 20L, false
				, false));
			QueryUtils.CheckHashEquals(NumericRangeQuery.NewLongRange("test5", 4, 10L, null, 
				true, true));
			QueryUtils.CheckHashEquals(NumericRangeQuery.NewLongRange("test6", 4, null, 20L, 
				true, true));
			QueryUtils.CheckHashEquals(NumericRangeQuery.NewLongRange("test7", 4, null, null, 
				true, true));
			QueryUtils.CheckEqual(NumericRangeQuery.NewLongRange("test8", 4, 10L, 20L, true, 
				true), NumericRangeQuery.NewLongRange("test8", 4, 10L, 20L, true, true));
			QueryUtils.CheckUnequal(NumericRangeQuery.NewLongRange("test9", 4, 10L, 20L, true
				, true), NumericRangeQuery.NewLongRange("test9", 8, 10L, 20L, true, true));
			QueryUtils.CheckUnequal(NumericRangeQuery.NewLongRange("test10a", 4, 10L, 20L, true
				, true), NumericRangeQuery.NewLongRange("test10b", 4, 10L, 20L, true, true));
			QueryUtils.CheckUnequal(NumericRangeQuery.NewLongRange("test11", 4, 10L, 20L, true
				, true), NumericRangeQuery.NewLongRange("test11", 4, 20L, 10L, true, true));
			QueryUtils.CheckUnequal(NumericRangeQuery.NewLongRange("test12", 4, 10L, 20L, true
				, true), NumericRangeQuery.NewLongRange("test12", 4, 10L, 20L, false, true));
			QueryUtils.CheckUnequal(NumericRangeQuery.NewLongRange("test13", 4, 10L, 20L, true
				, true), NumericRangeQuery.NewFloatRange("test13", 4, 10f, 20f, true, true));
		}
		// difference to int range is tested in TestNumericRangeQuery32
	}
}
