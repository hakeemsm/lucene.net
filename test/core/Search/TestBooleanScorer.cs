/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search
{
	public class TestBooleanScorer : LuceneTestCase
	{
		private static readonly string FIELD = "category";

		/// <exception cref="System.Exception"></exception>
		public virtual void TestMethod()
		{
			Directory directory = NewDirectory();
			string[] values = new string[] { "1", "2", "3", "4" };
			RandomIndexWriter writer = new RandomIndexWriter(Random(), directory);
			for (int i = 0; i < values.Length; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(NewStringField(FIELD, values[i], Field.Store.YES));
				writer.AddDocument(doc);
			}
			IndexReader ir = writer.GetReader();
			writer.Close();
			BooleanQuery booleanQuery1 = new BooleanQuery();
			booleanQuery1.Add(new TermQuery(new Term(FIELD, "1")), BooleanClause.Occur.SHOULD
				);
			booleanQuery1.Add(new TermQuery(new Term(FIELD, "2")), BooleanClause.Occur.SHOULD
				);
			BooleanQuery query = new BooleanQuery();
			query.Add(booleanQuery1, BooleanClause.Occur.MUST);
			query.Add(new TermQuery(new Term(FIELD, "9")), BooleanClause.Occur.MUST_NOT);
			IndexSearcher indexSearcher = NewSearcher(ir);
			ScoreDoc[] hits = indexSearcher.Search(query, null, 1000).scoreDocs;
			AreEqual("Number of matched documents", 2, hits.Length);
			ir.Close();
			directory.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestEmptyBucketWithMoreDocs()
		{
			// This test checks the logic of nextDoc() when all sub scorers have docs
			// beyond the first bucket (for example). Currently, the code relies on the
			// 'more' variable to work properly, and this test ensures that if the logic
			// changes, we have a test to back it up.
			Directory directory = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), directory);
			writer.Commit();
			IndexReader ir = writer.GetReader();
			writer.Close();
			IndexSearcher searcher = NewSearcher(ir);
			BooleanQuery.BooleanWeight weight = (BooleanQuery.BooleanWeight)new BooleanQuery(
				).CreateWeight(searcher);
			BulkScorer[] scorers = new BulkScorer[] { new _BulkScorer_83() };
			//HM:revisit 
			//assert doc == -1;
			BooleanScorer bs = new BooleanScorer(weight, false, 1, Arrays.AsList(scorers), Collections
				.EmptyList<BulkScorer>(), scorers.Length);
			IList<int> hits = new AList<int>();
			bs.Score(new _Collector_104(hits));
			AreEqual("should have only 1 hit", 1, hits.Count);
			AreEqual("hit should have been docID=3000", 3000, hits[0]);
			ir.Close();
			directory.Close();
		}

		private sealed class _BulkScorer_83 : BulkScorer
		{
			public _BulkScorer_83()
			{
				this.doc = -1;
			}

			private int doc;

			/// <exception cref="System.IO.IOException"></exception>
			public override bool Score(Collector c, int maxDoc)
			{
				this.doc = 3000;
				FakeScorer fs = new FakeScorer();
				fs.doc = this.doc;
				fs.score = 1.0f;
				c.SetScorer(fs);
				c.Collect(3000);
				return false;
			}
		}

		private sealed class _Collector_104 : Collector
		{
			public _Collector_104(IList<int> hits)
			{
				this.hits = hits;
			}

			internal int docBase;

			public override void SetScorer(Scorer scorer)
			{
			}

			public override void Collect(int doc)
			{
				hits.AddItem(this.docBase + doc);
			}

			public override void SetNextReader(AtomicReaderContext context)
			{
				this.docBase = context.docBase;
			}

			public override bool AcceptsDocsOutOfOrder()
			{
				return true;
			}

			private readonly IList<int> hits;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestMoreThan32ProhibitedClauses()
		{
			Directory d = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), d);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new TextField("field", "0 1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 16 17 18 19 20 21 22 23 24 25 26 27 28 29 30 31 32 33"
				, Field.Store.NO));
			w.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new TextField("field", "33", Field.Store.NO));
			w.AddDocument(doc);
			IndexReader r = w.GetReader();
			w.Close();
			// we don't wrap with AssertingIndexSearcher in order to have the original scorer in setScorer.
			IndexSearcher s = NewSearcher(r, true, false);
			BooleanQuery q = new BooleanQuery();
			for (int term = 0; term < 33; term++)
			{
				q.Add(new BooleanClause(new TermQuery(new Term("field", string.Empty + term)), BooleanClause.Occur
					.MUST_NOT));
			}
			q.Add(new BooleanClause(new TermQuery(new Term("field", "33")), BooleanClause.Occur
				.SHOULD));
			int[] count = new int[1];
			s.Search(q, new _Collector_155(count));
			// Make sure we got BooleanScorer:
			AreEqual(1, count[0]);
			r.Close();
			d.Close();
		}

		private sealed class _Collector_155 : Collector
		{
			public _Collector_155(int[] count)
			{
				this.count = count;
			}

			public override void SetScorer(Scorer scorer)
			{
				Type clazz = scorer.GetType();
				AreEqual("Scorer is implemented by wrong class", typeof(FakeScorer
					).FullName, clazz.FullName);
			}

			public override void Collect(int doc)
			{
				count[0]++;
			}

			public override void SetNextReader(AtomicReaderContext context)
			{
			}

			public override bool AcceptsDocsOutOfOrder()
			{
				return true;
			}

			private readonly int[] count;
		}

		/// <summary>Throws UOE if Weight.scorer is called</summary>
		private class CrazyMustUseBulkScorerQuery : Query
		{
			public override string ToString(string field)
			{
				return "MustUseBulkScorerQuery";
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override Weight CreateWeight(IndexSearcher searcher)
			{
				return new _Weight_195(this);
			}

			private sealed class _Weight_195 : Weight
			{
				public _Weight_195(CrazyMustUseBulkScorerQuery _enclosing)
				{
					this._enclosing = _enclosing;
				}

				public override Explanation Explain(AtomicReaderContext context, int doc)
				{
					throw new NotSupportedException();
				}

				public override Query GetQuery()
				{
					return this._enclosing;
				}

				public override float GetValueForNormalization()
				{
					return 1.0f;
				}

				public override void Normalize(float norm, float topLevelBoost)
				{
				}

				public override Scorer Scorer(AtomicReaderContext context, Bits acceptDocs)
				{
					throw new NotSupportedException();
				}

				public override BulkScorer BulkScorer(AtomicReaderContext context, bool scoreDocsInOrder
					, Bits acceptDocs)
				{
					return new _BulkScorer_222();
				}

				private sealed class _BulkScorer_222 : BulkScorer
				{
					public _BulkScorer_222()
					{
					}

					/// <exception cref="System.IO.IOException"></exception>
					public override bool Score(Collector collector, int max)
					{
						collector.SetScorer(new FakeScorer());
						collector.Collect(0);
						return false;
					}
				}

				private readonly CrazyMustUseBulkScorerQuery _enclosing;
			}
		}

		/// <summary>
		/// Make sure BooleanScorer can embed another
		/// BooleanScorer.
		/// </summary>
		/// <remarks>
		/// Make sure BooleanScorer can embed another
		/// BooleanScorer.
		/// </remarks>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestEmbeddedBooleanScorer()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewTextField("field", "doctors are people who prescribe medicines of which they know little, to cure diseases of which they know less, in human beings of whom they know nothing"
				, Field.Store.NO));
			w.AddDocument(doc);
			IndexReader r = w.GetReader();
			w.Close();
			IndexSearcher s = NewSearcher(r);
			BooleanQuery q1 = new BooleanQuery();
			q1.Add(new TermQuery(new Term("field", "little")), BooleanClause.Occur.SHOULD);
			q1.Add(new TermQuery(new Term("field", "diseases")), BooleanClause.Occur.SHOULD);
			BooleanQuery q2 = new BooleanQuery();
			q2.Add(q1, BooleanClause.Occur.SHOULD);
			q2.Add(new TestBooleanScorer.CrazyMustUseBulkScorerQuery(), BooleanClause.Occur.SHOULD
				);
			AreEqual(1, s.Search(q2, 10).TotalHits);
			r.Close();
			dir.Close();
		}
	}
}
