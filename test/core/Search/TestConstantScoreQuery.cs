/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using NUnit.Framework;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search
{
	/// <summary>
	/// This class only tests some basic functionality in CSQ, the main parts are mostly
	/// tested by MultiTermQuery tests, explanations seems to be tested in TestExplanations!
	/// </summary>
	public class TestConstantScoreQuery : LuceneTestCase
	{
		/// <exception cref="System.Exception"></exception>
		public virtual void TestCSQ()
		{
			Query q1 = new ConstantScoreQuery(new TermQuery(new Term("a", "b")));
			Query q2 = new ConstantScoreQuery(new TermQuery(new Term("a", "c")));
			Query q3 = new ConstantScoreQuery(TermRangeFilter.NewStringRange("a", "b", "c", true
				, true));
			QueryUtils.Check(q1);
			QueryUtils.Check(q2);
			QueryUtils.CheckEqual(q1, q1);
			QueryUtils.CheckEqual(q2, q2);
			QueryUtils.CheckEqual(q3, q3);
			QueryUtils.CheckUnequal(q1, q2);
			QueryUtils.CheckUnequal(q2, q3);
			QueryUtils.CheckUnequal(q1, q3);
			QueryUtils.CheckUnequal(q1, new TermQuery(new Term("a", "b")));
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void CheckHits(IndexSearcher searcher, Query q, float expectedScore, string
			 scorerClassName, string innerScorerClassName)
		{
			int[] count = new int[1];
			searcher.Search(q, new _Collector_53(scorerClassName, innerScorerClassName, expectedScore
				, count));
			NUnit.Framework.Assert.AreEqual("invalid number of results", 1, count[0]);
		}

		private sealed class _Collector_53 : Collector
		{
			public _Collector_53(string scorerClassName, string innerScorerClassName, float expectedScore
				, int[] count)
			{
				this.scorerClassName = scorerClassName;
				this.innerScorerClassName = innerScorerClassName;
				this.expectedScore = expectedScore;
				this.count = count;
			}

			private Scorer scorer;

			public override void SetScorer(Scorer scorer)
			{
				this.scorer = scorer;
				NUnit.Framework.Assert.AreEqual("Scorer is implemented by wrong class", scorerClassName
					, scorer.GetType().FullName);
				if (innerScorerClassName != null && scorer is ConstantScoreQuery.ConstantScorer)
				{
					ConstantScoreQuery.ConstantScorer innerScorer = (ConstantScoreQuery.ConstantScorer
						)scorer;
					NUnit.Framework.Assert.AreEqual("inner Scorer is implemented by wrong class", innerScorerClassName
						, innerScorer.docIdSetIterator.GetType().FullName);
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Collect(int doc)
			{
				NUnit.Framework.Assert.AreEqual("Score differs from expected", expectedScore, this
					.scorer.Score(), 0);
				count[0]++;
			}

			public override void SetNextReader(AtomicReaderContext context)
			{
			}

			public override bool AcceptsDocsOutOfOrder()
			{
				return true;
			}

			private readonly string scorerClassName;

			private readonly string innerScorerClassName;

			private readonly float expectedScore;

			private readonly int[] count;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestWrapped2Times()
		{
			Directory directory = null;
			IndexReader reader = null;
			IndexSearcher searcher = null;
			try
			{
				directory = NewDirectory();
				RandomIndexWriter writer = new RandomIndexWriter(Random(), directory);
				Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
					();
				doc.Add(NewStringField("field", "term", Field.Store.NO));
				writer.AddDocument(doc);
				reader = writer.GetReader();
				writer.Close();
				// we don't wrap with AssertingIndexSearcher in order to have the original scorer in setScorer.
				searcher = NewSearcher(reader, true, false);
				// set a similarity that does not normalize our boost away
				searcher.SetSimilarity(new _DefaultSimilarity_102());
				Query csq1 = new ConstantScoreQuery(new TermQuery(new Term("field", "term")));
				csq1.SetBoost(2.0f);
				Query csq2 = new ConstantScoreQuery(csq1);
				csq2.SetBoost(5.0f);
				BooleanQuery bq = new BooleanQuery();
				bq.Add(csq1, BooleanClause.Occur.SHOULD);
				bq.Add(csq2, BooleanClause.Occur.SHOULD);
				Query csqbq = new ConstantScoreQuery(bq);
				csqbq.SetBoost(17.0f);
				CheckHits(searcher, csq1, csq1.GetBoost(), typeof(ConstantScoreQuery.ConstantScorer
					).FullName, null);
				CheckHits(searcher, csq2, csq2.GetBoost(), typeof(ConstantScoreQuery.ConstantScorer
					).FullName, typeof(ConstantScoreQuery.ConstantScorer).FullName);
				// for the combined BQ, the scorer should always be BooleanScorer's BucketScorer, because our scorer supports out-of order collection!
				string bucketScorerClass = typeof(FakeScorer).FullName;
				CheckHits(searcher, bq, csq1.GetBoost() + csq2.GetBoost(), bucketScorerClass, null
					);
				CheckHits(searcher, csqbq, csqbq.GetBoost(), typeof(ConstantScoreQuery.ConstantScorer
					).FullName, bucketScorerClass);
			}
			finally
			{
				if (reader != null)
				{
					reader.Close();
				}
				if (directory != null)
				{
					directory.Close();
				}
			}
		}

		private sealed class _DefaultSimilarity_102 : DefaultSimilarity
		{
			public _DefaultSimilarity_102()
			{
			}

			public override float QueryNorm(float sumOfSquaredWeights)
			{
				return 1.0f;
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestConstantScoreQueryAndFilter()
		{
			Directory d = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), d);
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			doc.Add(NewStringField("field", "a", Field.Store.NO));
			w.AddDocument(doc);
			doc = new Lucene.Net.Document.Document();
			doc.Add(NewStringField("field", "b", Field.Store.NO));
			w.AddDocument(doc);
			IndexReader r = w.GetReader();
			w.Close();
			Filter filterB = new CachingWrapperFilter(new QueryWrapperFilter(new TermQuery(new 
				Term("field", "b"))));
			Query query = new ConstantScoreQuery(filterB);
			IndexSearcher s = NewSearcher(r);
			NUnit.Framework.Assert.AreEqual(1, s.Search(query, filterB, 1).totalHits);
			// Query for field:b, Filter field:b
			Filter filterA = new CachingWrapperFilter(new QueryWrapperFilter(new TermQuery(new 
				Term("field", "a"))));
			query = new ConstantScoreQuery(filterA);
			NUnit.Framework.Assert.AreEqual(0, s.Search(query, filterB, 1).totalHits);
			// Query field:b, Filter field:a
			r.Close();
			d.Close();
		}

		// LUCENE-5307
		// don't reuse the scorer of filters since they have been created with bulkScorer=false
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestQueryWrapperFilter()
		{
			Directory d = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), d);
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			doc.Add(NewStringField("field", "a", Field.Store.NO));
			w.AddDocument(doc);
			IndexReader r = w.GetReader();
			w.Close();
			Filter filter = new QueryWrapperFilter(AssertingQuery.Wrap(Random(), new TermQuery
				(new Term("field", "a"))));
			IndexSearcher s = NewSearcher(r);
			//HM:revisit 
			//assert s instanceof AssertingIndexSearcher;
			// this used to fail
			s.Search(new ConstantScoreQuery(filter), new TotalHitCountCollector());
			// check the rewrite
			Query rewritten = new ConstantScoreQuery(filter).Rewrite(r);
			NUnit.Framework.Assert.IsTrue(rewritten is ConstantScoreQuery);
			NUnit.Framework.Assert.IsTrue(((ConstantScoreQuery)rewritten).GetQuery() is AssertingQuery
				);
			r.Close();
			d.Close();
		}
	}
}
