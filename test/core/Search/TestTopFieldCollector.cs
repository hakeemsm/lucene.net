/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search
{
	public class TestTopFieldCollector : LuceneTestCase
	{
		private IndexSearcher @is;

		private IndexReader ir;

		private Directory dir;

		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			dir = NewDirectory();
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir);
			int numDocs = AtLeast(100);
			for (int i = 0; i < numDocs; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				iw.AddDocument(doc);
			}
			ir = iw.GetReader();
			iw.Close();
			@is = NewSearcher(ir);
		}

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			ir.Close();
			dir.Close();
			base.TearDown();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSortWithoutFillFields()
		{
			// There was previously a bug in TopFieldCollector when fillFields was set
			// to false - the same doc and score was set in ScoreDoc[] array. This test
			// asserts that if fillFields is false, the documents are set properly. It
			// does not use Searcher's default search methods (with Sort) since all set
			// fillFields to true.
			Sort[] sort = new Sort[] { new Sort(SortField.FIELD_DOC), new Sort() };
			for (int i = 0; i < sort.Length; i++)
			{
				Query q = new MatchAllDocsQuery();
				TopDocsCollector<FieldValueHitQueue.Entry> tdc = TopFieldCollector.Create(sort[i]
					, 10, false, false, false, true);
				@is.Search(q, tdc);
				ScoreDoc[] sd = tdc.TopDocs().scoreDocs;
				for (int j = 1; j < sd.Length; j++)
				{
					IsTrue(sd[j].doc != sd[j - 1].doc);
				}
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSortWithoutScoreTracking()
		{
			// Two Sort criteria to instantiate the multi/single comparators.
			Sort[] sort = new Sort[] { new Sort(SortField.FIELD_DOC), new Sort() };
			for (int i = 0; i < sort.Length; i++)
			{
				Query q = new MatchAllDocsQuery();
				TopDocsCollector<FieldValueHitQueue.Entry> tdc = TopFieldCollector.Create(sort[i]
					, 10, true, false, false, true);
				@is.Search(q, tdc);
				TopDocs td = tdc.TopDocs();
				ScoreDoc[] sd = td.scoreDocs;
				for (int j = 0; j < sd.Length; j++)
				{
					IsTrue(float.IsNaN(sd[j].score));
				}
				IsTrue(float.IsNaN(td.GetMaxScore()));
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSortWithScoreNoMaxScoreTracking()
		{
			// Two Sort criteria to instantiate the multi/single comparators.
			Sort[] sort = new Sort[] { new Sort(SortField.FIELD_DOC), new Sort() };
			for (int i = 0; i < sort.Length; i++)
			{
				Query q = new MatchAllDocsQuery();
				TopDocsCollector<FieldValueHitQueue.Entry> tdc = TopFieldCollector.Create(sort[i]
					, 10, true, true, false, true);
				@is.Search(q, tdc);
				TopDocs td = tdc.TopDocs();
				ScoreDoc[] sd = td.scoreDocs;
				for (int j = 0; j < sd.Length; j++)
				{
					IsTrue(!float.IsNaN(sd[j].score));
				}
				IsTrue(float.IsNaN(td.GetMaxScore()));
			}
		}

		// MultiComparatorScoringNoMaxScoreCollector
		/// <exception cref="System.Exception"></exception>
		public virtual void TestSortWithScoreNoMaxScoreTrackingMulti()
		{
			// Two Sort criteria to instantiate the multi/single comparators.
			Sort[] sort = new Sort[] { new Sort(SortField.FIELD_DOC, SortField.FIELD_SCORE) };
			for (int i = 0; i < sort.Length; i++)
			{
				Query q = new MatchAllDocsQuery();
				TopDocsCollector<FieldValueHitQueue.Entry> tdc = TopFieldCollector.Create(sort[i]
					, 10, true, true, false, true);
				@is.Search(q, tdc);
				TopDocs td = tdc.TopDocs();
				ScoreDoc[] sd = td.scoreDocs;
				for (int j = 0; j < sd.Length; j++)
				{
					IsTrue(!float.IsNaN(sd[j].score));
				}
				IsTrue(float.IsNaN(td.GetMaxScore()));
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSortWithScoreAndMaxScoreTracking()
		{
			// Two Sort criteria to instantiate the multi/single comparators.
			Sort[] sort = new Sort[] { new Sort(SortField.FIELD_DOC), new Sort() };
			for (int i = 0; i < sort.Length; i++)
			{
				Query q = new MatchAllDocsQuery();
				TopDocsCollector<FieldValueHitQueue.Entry> tdc = TopFieldCollector.Create(sort[i]
					, 10, true, true, true, true);
				@is.Search(q, tdc);
				TopDocs td = tdc.TopDocs();
				ScoreDoc[] sd = td.scoreDocs;
				for (int j = 0; j < sd.Length; j++)
				{
					IsTrue(!float.IsNaN(sd[j].score));
				}
				IsTrue(!float.IsNaN(td.GetMaxScore()));
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestOutOfOrderDocsScoringSort()
		{
			// Two Sort criteria to instantiate the multi/single comparators.
			Sort[] sort = new Sort[] { new Sort(SortField.FIELD_DOC), new Sort() };
			bool[][] tfcOptions = new bool[][] { new bool[] { false, false, false }, new bool
				[] { false, false, true }, new bool[] { false, true, false }, new bool[] { false
				, true, true }, new bool[] { true, false, false }, new bool[] { true, false, true
				 }, new bool[] { true, true, false }, new bool[] { true, true, true } };
			string[] actualTFCClasses = new string[] { "OutOfOrderOneComparatorNonScoringCollector"
				, "OutOfOrderOneComparatorScoringMaxScoreCollector", "OutOfOrderOneComparatorScoringNoMaxScoreCollector"
				, "OutOfOrderOneComparatorScoringMaxScoreCollector", "OutOfOrderOneComparatorNonScoringCollector"
				, "OutOfOrderOneComparatorScoringMaxScoreCollector", "OutOfOrderOneComparatorScoringNoMaxScoreCollector"
				, "OutOfOrderOneComparatorScoringMaxScoreCollector" };
			BooleanQuery bq = new BooleanQuery();
			// Add a Query with SHOULD, since bw.scorer() returns BooleanScorer2
			// which delegates to BS if there are no mandatory clauses.
			bq.Add(new MatchAllDocsQuery(), BooleanClause.Occur.SHOULD);
			// Set minNrShouldMatch to 1 so that BQ will not optimize rewrite to return
			// the clause instead of BQ.
			bq.SetMinimumNumberShouldMatch(1);
			for (int i = 0; i < sort.Length; i++)
			{
				for (int j = 0; j < tfcOptions.Length; j++)
				{
					TopDocsCollector<FieldValueHitQueue.Entry> tdc = TopFieldCollector.Create(sort[i]
						, 10, tfcOptions[j][0], tfcOptions[j][1], tfcOptions[j][2], false);
					IsTrue(tdc.GetType().FullName.EndsWith("$" + actualTFCClasses
						[j]));
					@is.Search(bq, tdc);
					TopDocs td = tdc.TopDocs();
					ScoreDoc[] sd = td.scoreDocs;
					AreEqual(10, sd.Length);
				}
			}
		}

		// OutOfOrderMulti*Collector
		/// <exception cref="System.Exception"></exception>
		public virtual void TestOutOfOrderDocsScoringSortMulti()
		{
			// Two Sort criteria to instantiate the multi/single comparators.
			Sort[] sort = new Sort[] { new Sort(SortField.FIELD_DOC, SortField.FIELD_SCORE) };
			bool[][] tfcOptions = new bool[][] { new bool[] { false, false, false }, new bool
				[] { false, false, true }, new bool[] { false, true, false }, new bool[] { false
				, true, true }, new bool[] { true, false, false }, new bool[] { true, false, true
				 }, new bool[] { true, true, false }, new bool[] { true, true, true } };
			string[] actualTFCClasses = new string[] { "OutOfOrderMultiComparatorNonScoringCollector"
				, "OutOfOrderMultiComparatorScoringMaxScoreCollector", "OutOfOrderMultiComparatorScoringNoMaxScoreCollector"
				, "OutOfOrderMultiComparatorScoringMaxScoreCollector", "OutOfOrderMultiComparatorNonScoringCollector"
				, "OutOfOrderMultiComparatorScoringMaxScoreCollector", "OutOfOrderMultiComparatorScoringNoMaxScoreCollector"
				, "OutOfOrderMultiComparatorScoringMaxScoreCollector" };
			BooleanQuery bq = new BooleanQuery();
			// Add a Query with SHOULD, since bw.scorer() returns BooleanScorer2
			// which delegates to BS if there are no mandatory clauses.
			bq.Add(new MatchAllDocsQuery(), BooleanClause.Occur.SHOULD);
			// Set minNrShouldMatch to 1 so that BQ will not optimize rewrite to return
			// the clause instead of BQ.
			bq.SetMinimumNumberShouldMatch(1);
			for (int i = 0; i < sort.Length; i++)
			{
				for (int j = 0; j < tfcOptions.Length; j++)
				{
					TopDocsCollector<FieldValueHitQueue.Entry> tdc = TopFieldCollector.Create(sort[i]
						, 10, tfcOptions[j][0], tfcOptions[j][1], tfcOptions[j][2], false);
					IsTrue(tdc.GetType().FullName.EndsWith("$" + actualTFCClasses
						[j]));
					@is.Search(bq, tdc);
					TopDocs td = tdc.TopDocs();
					ScoreDoc[] sd = td.scoreDocs;
					AreEqual(10, sd.Length);
				}
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSortWithScoreAndMaxScoreTrackingNoResults()
		{
			// Two Sort criteria to instantiate the multi/single comparators.
			Sort[] sort = new Sort[] { new Sort(SortField.FIELD_DOC), new Sort() };
			for (int i = 0; i < sort.Length; i++)
			{
				TopDocsCollector<FieldValueHitQueue.Entry> tdc = TopFieldCollector.Create(sort[i]
					, 10, true, true, true, true);
				TopDocs td = tdc.TopDocs();
				AreEqual(0, td.TotalHits);
				IsTrue(float.IsNaN(td.GetMaxScore()));
			}
		}
	}
}
