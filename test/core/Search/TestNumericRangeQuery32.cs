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
	public class TestNumericRangeQuery32 : LuceneTestCase
	{
		private static int distance;

		private const int startOffset = -1 << 15;

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
			distance = (1 << 30) / noDocs;
			directory = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), directory, ((IndexWriterConfig
				)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs
				(TestUtil.NextInt(Random(), 100, 1000))).SetMergePolicy(NewLogMergePolicy()));
			FieldType storedInt = new FieldType(IntField.TYPE_NOT_STORED);
			storedInt.Stored = (true);
			storedInt.Freeze();
			FieldType storedInt8 = new FieldType(storedInt);
			storedInt8.SetNumericPrecisionStep(8);
			FieldType storedInt4 = new FieldType(storedInt);
			storedInt4.SetNumericPrecisionStep(4);
			FieldType storedInt2 = new FieldType(storedInt);
			storedInt2.SetNumericPrecisionStep(2);
			FieldType storedIntNone = new FieldType(storedInt);
			storedIntNone.SetNumericPrecisionStep(int.MaxValue);
			FieldType unstoredInt = IntField.TYPE_NOT_STORED;
			FieldType unstoredInt8 = new FieldType(unstoredInt);
			unstoredInt8.SetNumericPrecisionStep(8);
			FieldType unstoredInt4 = new FieldType(unstoredInt);
			unstoredInt4.SetNumericPrecisionStep(4);
			FieldType unstoredInt2 = new FieldType(unstoredInt);
			unstoredInt2.SetNumericPrecisionStep(2);
			IntField field8 = new IntField("field8", 0, storedInt8);
			IntField field4 = new IntField("field4", 0, storedInt4);
			IntField field2 = new IntField("field2", 0, storedInt2);
			IntField fieldNoTrie = new IntField("field" + int.MaxValue, 0, storedIntNone);
			IntField ascfield8 = new IntField("ascfield8", 0, unstoredInt8);
			IntField ascfield4 = new IntField("ascfield4", 0, unstoredInt4);
			IntField ascfield2 = new IntField("ascfield2", 0, unstoredInt2);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			// add fields, that have a distance to test general functionality
			doc.Add(field8);
			doc.Add(field4);
			doc.Add(field2);
			doc.Add(fieldNoTrie);
			// add ascending fields with a distance of 1, beginning at -noDocs/2 to test the correct splitting of range and inclusive/exclusive
			doc.Add(ascfield8);
			doc.Add(ascfield4);
			doc.Add(ascfield2);
			// Add a series of noDocs docs with increasing int values
			for (int l = 0; l < noDocs; l++)
			{
				int val = distance * l + startOffset;
				field8.SetIntValue(val);
				field4.SetIntValue(val);
				field2.SetIntValue(val);
				fieldNoTrie.SetIntValue(val);
				val = l - (noDocs / 2);
				ascfield8.SetIntValue(val);
				ascfield4.SetIntValue(val);
				ascfield2.SetIntValue(val);
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
			BooleanQuery.SetMaxClauseCount(3 * 255 * 2 + 255);
		}

		/// <summary>test for both constant score and boolean query, the other tests only use the constant score mode
		/// 	</summary>
		/// <exception cref="System.Exception"></exception>
		private void TestRange(int precisionStep)
		{
			string field = "field" + precisionStep;
			int count = 3000;
			int lower = (distance * 3 / 2) + startOffset;
			int upper = lower + count * distance + (distance / 3);
			NumericRangeQuery<int> q = NumericRangeQuery.NewIntRange(field, precisionStep, lower
				, upper, true, true);
			NumericRangeFilter<int> f = NumericRangeFilter.NewIntRange(field, precisionStep, 
				lower, upper, true, true);
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
				(reader).GetContext());
			NumericRangeFilter<int> f = NumericRangeFilter.NewIntRange("field8", 8, 1000, -1000
				, true, true);
			IsNull("A inverse range should return the null instance", 
				f.GetDocIdSet(context, ((AtomicReader)context.Reader).LiveDocs));
			f = NumericRangeFilter.NewIntRange("field8", 8, int.MaxValue, null, false, false);
			IsNull("A exclusive range starting with Integer.MAX_VALUE should return the null instance"
				, f.GetDocIdSet(context, ((AtomicReader)context.Reader).LiveDocs));
			f = NumericRangeFilter.NewIntRange("field8", 8, null, int.MinValue, false, false);
			IsNull("A exclusive range ending with Integer.MIN_VALUE should return the null instance"
				, f.GetDocIdSet(context, ((AtomicReader)context.Reader).LiveDocs));
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestOneMatchQuery()
		{
			NumericRangeQuery<int> q = NumericRangeQuery.NewIntRange("ascfield8", 8, 1000, 1000
				, true, true);
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
			int upper = (count - 1) * distance + (distance / 3) + startOffset;
			NumericRangeQuery<int> q = NumericRangeQuery.NewIntRange(field, precisionStep, null
				, upper, true, true);
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
			q = NumericRangeQuery.NewIntRange(field, precisionStep, null, upper, false, true);
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
			int lower = (count - 1) * distance + (distance / 3) + startOffset;
			NumericRangeQuery<int> q = NumericRangeQuery.NewIntRange(field, precisionStep, lower
				, null, true, true);
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
			q = NumericRangeQuery.NewIntRange(field, precisionStep, lower, null, true, false);
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
			doc.Add(new FloatField("float", float.NegativeInfinity, Field.Store.NO));
			doc.Add(new IntField("int", int.MinValue, Field.Store.NO));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new FloatField("float", float.PositiveInfinity, Field.Store.NO));
			doc.Add(new IntField("int", int.MaxValue, Field.Store.NO));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new FloatField("float", 0.0f, Field.Store.NO));
			doc.Add(new IntField("int", 0, Field.Store.NO));
			writer.AddDocument(doc);
			foreach (float f in TestNumericUtils.FLOAT_NANs)
			{
				doc = new Lucene.Net.Documents.Document();
				doc.Add(new FloatField("float", f, Field.Store.NO));
				writer.AddDocument(doc);
			}
			writer.Dispose();
			IndexReader r = DirectoryReader.Open(dir);
			IndexSearcher s = NewSearcher(r);
			Query q = NumericRangeQuery.NewIntRange("int", null, null, true, true);
			TopDocs topDocs = s.Search(q, 10);
			AreEqual("Score doc count", 3, topDocs.ScoreDocs.Length);
			q = NumericRangeQuery.NewIntRange("int", null, null, false, false);
			topDocs = s.Search(q, 10);
			AreEqual("Score doc count", 3, topDocs.ScoreDocs.Length);
			q = NumericRangeQuery.NewIntRange("int", int.MinValue, int.MaxValue, true, true);
			topDocs = s.Search(q, 10);
			AreEqual("Score doc count", 3, topDocs.ScoreDocs.Length);
			q = NumericRangeQuery.NewIntRange("int", int.MinValue, int.MaxValue, false, false
				);
			topDocs = s.Search(q, 10);
			AreEqual("Score doc count", 1, topDocs.ScoreDocs.Length);
			q = NumericRangeQuery.NewFloatRange("float", null, null, true, true);
			topDocs = s.Search(q, 10);
			AreEqual("Score doc count", 3, topDocs.ScoreDocs.Length);
			q = NumericRangeQuery.NewFloatRange("float", null, null, false, false);
			topDocs = s.Search(q, 10);
			AreEqual("Score doc count", 3, topDocs.ScoreDocs.Length);
			q = NumericRangeQuery.NewFloatRange("float", float.NegativeInfinity, float.PositiveInfinity
				, true, true);
			topDocs = s.Search(q, 10);
			AreEqual("Score doc count", 3, topDocs.ScoreDocs.Length);
			q = NumericRangeQuery.NewFloatRange("float", float.NegativeInfinity, float.PositiveInfinity
				, false, false);
			topDocs = s.Search(q, 10);
			AreEqual("Score doc count", 1, topDocs.ScoreDocs.Length);
			q = NumericRangeQuery.NewFloatRange("float", float.NaN, float.NaN, true, true);
			topDocs = s.Search(q, 10);
			AreEqual("Score doc count", TestNumericUtils.FLOAT_NANs.Length
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
				int lower = (int)(Random().NextDouble() * noDocs * distance) + startOffset;
				int upper = (int)(Random().NextDouble() * noDocs * distance) + startOffset;
				if (lower > upper)
				{
					int a = lower;
					lower = upper;
					upper = a;
				}
				BytesRef lowerBytes = new BytesRef(NumericUtils.BUF_SIZE_INT);
				BytesRef upperBytes = new BytesRef(NumericUtils.BUF_SIZE_INT);
				NumericUtils.IntToPrefixCodedBytes(lower, 0, lowerBytes);
				NumericUtils.IntToPrefixCodedBytes(upper, 0, upperBytes);
				// test inclusive range
				NumericRangeQuery<int> tq = NumericRangeQuery.NewIntRange(field, precisionStep, lower
					, upper, true, true);
				TermRangeQuery cq = new TermRangeQuery(field, lowerBytes, upperBytes, true, true);
				TopDocs tTopDocs = searcher.Search(tq, 1);
				TopDocs cTopDocs = searcher.Search(cq, 1);
				AreEqual("Returned count for NumericRangeQuery and TermRangeQuery must be equal"
					, cTopDocs.TotalHits, tTopDocs.TotalHits);
				totalTermCountT += termCountT = CountTerms(tq);
				totalTermCountC += termCountC = CountTerms(cq);
				CheckTermCounts(precisionStep, termCountT, termCountC);
				// test exclusive range
				tq = NumericRangeQuery.NewIntRange(field, precisionStep, lower, upper, false, false
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
				tq = NumericRangeQuery.NewIntRange(field, precisionStep, lower, upper, false, true
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
				tq = NumericRangeQuery.NewIntRange(field, precisionStep, lower, upper, true, false
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
			int lower = (distance * 3 / 2) + startOffset;
			int upper = lower + count * distance + (distance / 3);
			// test empty enum
			//HM:revisit 
			//assert lower < upper;
			IsTrue(0 < CountTerms(NumericRangeQuery.NewIntRange("field4"
				, 4, lower, upper, true, true)));
			AreEqual(0, CountTerms(NumericRangeQuery.NewIntRange("field4"
				, 4, upper, lower, true, true)));
			// test empty enum outside of bounds
			lower = distance * noDocs + startOffset;
			upper = 2 * lower;
			//HM:revisit 
			//assert lower < upper;
			AreEqual(0, CountTerms(NumericRangeQuery.NewIntRange("field4"
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
				int lower = (int)(Random().NextDouble() * noDocs - noDocs / 2);
				int upper = (int)(Random().NextDouble() * noDocs - noDocs / 2);
				if (lower > upper)
				{
					int a = lower;
					lower = upper;
					upper = a;
				}
				// test inclusive range
				Query tq = NumericRangeQuery.NewIntRange(field, precisionStep, lower, upper, true
					, true);
				TopDocs tTopDocs = searcher.Search(tq, 1);
				AreEqual("Returned count of range query must be equal to inclusive range length"
					, upper - lower + 1, tTopDocs.TotalHits);
				// test exclusive range
				tq = NumericRangeQuery.NewIntRange(field, precisionStep, lower, upper, false, false
					);
				tTopDocs = searcher.Search(tq, 1);
				AreEqual("Returned count of range query must be equal to exclusive range length"
					, Math.Max(upper - lower - 1, 0), tTopDocs.TotalHits);
				// test left exclusive range
				tq = NumericRangeQuery.NewIntRange(field, precisionStep, lower, upper, false, true
					);
				tTopDocs = searcher.Search(tq, 1);
				AreEqual("Returned count of range query must be equal to half exclusive range length"
					, upper - lower, tTopDocs.TotalHits);
				// test right exclusive range
				tq = NumericRangeQuery.NewIntRange(field, precisionStep, lower, upper, true, false
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

		/// <summary>we fake a float test using int2float conversion of NumericUtils</summary>
		/// <exception cref="System.Exception"></exception>
		private void TestFloatRange(int precisionStep)
		{
			string field = "ascfield" + precisionStep;
			int lower = -1000;
			int upper = +2000;
			Query tq = NumericRangeQuery.NewFloatRange(field, precisionStep, NumericUtils.SortableIntToFloat
				(lower), NumericUtils.SortableIntToFloat(upper), true, true);
			TopDocs tTopDocs = searcher.Search(tq, 1);
			AreEqual("Returned count of range query must be equal to inclusive range length"
				, upper - lower + 1, tTopDocs.TotalHits);
			Filter tf = NumericRangeFilter.NewFloatRange(field, precisionStep, NumericUtils.SortableIntToFloat
				(lower), NumericUtils.SortableIntToFloat(upper), true, true);
			tTopDocs = searcher.Search(new MatchAllDocsQuery(), tf, 1);
			AreEqual("Returned count of range filter must be equal to inclusive range length"
				, upper - lower + 1, tTopDocs.TotalHits);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestFloatRange_8bit()
		{
			TestFloatRange(8);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestFloatRange_4bit()
		{
			TestFloatRange(4);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestFloatRange_2bit()
		{
			TestFloatRange(2);
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
				int lower = (int)(Random().NextDouble() * noDocs * distance) + startOffset;
				int upper = (int)(Random().NextDouble() * noDocs * distance) + startOffset;
				if (lower > upper)
				{
					int a = lower;
					lower = upper;
					upper = a;
				}
				Query tq = NumericRangeQuery.NewIntRange(field, precisionStep, lower, upper, true
					, true);
				TopDocs topDocs = searcher.Search(tq, null, noDocs, new Sort(new SortField(field, 
					SortField.Type.INT, true)));
				if (topDocs.TotalHits == 0)
				{
					continue;
				}
				ScoreDoc[] sd = topDocs.ScoreDocs;
				IsNotNull(sd);
				int last = searcher.Doc(sd[0].Doc).GetField(field).NumericValue();
				for (int j = 1; j < sd.Length; j++)
				{
					int act = searcher.Doc(sd[j].Doc).GetField(field).NumericValue();
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
			QueryUtils.CheckHashEquals(NumericRangeQuery.NewIntRange("test1", 4, 10, 20, true
				, true));
			QueryUtils.CheckHashEquals(NumericRangeQuery.NewIntRange("test2", 4, 10, 20, false
				, true));
			QueryUtils.CheckHashEquals(NumericRangeQuery.NewIntRange("test3", 4, 10, 20, true
				, false));
			QueryUtils.CheckHashEquals(NumericRangeQuery.NewIntRange("test4", 4, 10, 20, false
				, false));
			QueryUtils.CheckHashEquals(NumericRangeQuery.NewIntRange("test5", 4, 10, null, true
				, true));
			QueryUtils.CheckHashEquals(NumericRangeQuery.NewIntRange("test6", 4, null, 20, true
				, true));
			QueryUtils.CheckHashEquals(NumericRangeQuery.NewIntRange("test7", 4, null, null, 
				true, true));
			QueryUtils.CheckEqual(NumericRangeQuery.NewIntRange("test8", 4, 10, 20, true, true
				), NumericRangeQuery.NewIntRange("test8", 4, 10, 20, true, true));
			QueryUtils.CheckUnequal(NumericRangeQuery.NewIntRange("test9", 4, 10, 20, true, true
				), NumericRangeQuery.NewIntRange("test9", 8, 10, 20, true, true));
			QueryUtils.CheckUnequal(NumericRangeQuery.NewIntRange("test10a", 4, 10, 20, true, 
				true), NumericRangeQuery.NewIntRange("test10b", 4, 10, 20, true, true));
			QueryUtils.CheckUnequal(NumericRangeQuery.NewIntRange("test11", 4, 10, 20, true, 
				true), NumericRangeQuery.NewIntRange("test11", 4, 20, 10, true, true));
			QueryUtils.CheckUnequal(NumericRangeQuery.NewIntRange("test12", 4, 10, 20, true, 
				true), NumericRangeQuery.NewIntRange("test12", 4, 10, 20, false, true));
			QueryUtils.CheckUnequal(NumericRangeQuery.NewIntRange("test13", 4, 10, 20, true, 
				true), NumericRangeQuery.NewFloatRange("test13", 4, 10f, 20f, true, true));
			// the following produces a hash collision, because Long and Integer have the same hashcode, so only test equality:
			Query q1 = NumericRangeQuery.NewIntRange("test14", 4, 10, 20, true, true);
			Query q2 = NumericRangeQuery.NewLongRange("test14", 4, 10L, 20L, true, true);
			IsFalse(q1.Equals(q2));
			IsFalse(q2.Equals(q1));
		}
	}
}
