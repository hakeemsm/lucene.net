/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using NUnit.Framework;
using Lucene.Net.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search
{
	public class TestBooleanOr : LuceneTestCase
	{
		private static string FIELD_T = "T";

		private static string FIELD_C = "C";

		private TermQuery t1 = new TermQuery(new Term(FIELD_T, "files"));

		private TermQuery t2 = new TermQuery(new Term(FIELD_T, "deleting"));

		private TermQuery c1 = new TermQuery(new Term(FIELD_C, "production"));

		private TermQuery c2 = new TermQuery(new Term(FIELD_C, "optimize"));

		private IndexSearcher searcher = null;

		private Directory dir;

		private IndexReader reader;

		/// <exception cref="System.IO.IOException"></exception>
		private int Search(Query q)
		{
			QueryUtils.Check(Random(), q, searcher);
			return searcher.Search(q, null, 1000).totalHits;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestElements()
		{
			NUnit.Framework.Assert.AreEqual(1, Search(t1));
			NUnit.Framework.Assert.AreEqual(1, Search(t2));
			NUnit.Framework.Assert.AreEqual(1, Search(c1));
			NUnit.Framework.Assert.AreEqual(1, Search(c2));
		}

		/// <summary>
		/// <code>T:files T:deleting C:production C:optimize </code>
		/// it works.
		/// </summary>
		/// <remarks>
		/// <code>T:files T:deleting C:production C:optimize </code>
		/// it works.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestFlat()
		{
			BooleanQuery q = new BooleanQuery();
			q.Add(new BooleanClause(t1, BooleanClause.Occur.SHOULD));
			q.Add(new BooleanClause(t2, BooleanClause.Occur.SHOULD));
			q.Add(new BooleanClause(c1, BooleanClause.Occur.SHOULD));
			q.Add(new BooleanClause(c2, BooleanClause.Occur.SHOULD));
			NUnit.Framework.Assert.AreEqual(1, Search(q));
		}

		/// <summary>
		/// <code>(T:files T:deleting) (+C:production +C:optimize)</code>
		/// it works.
		/// </summary>
		/// <remarks>
		/// <code>(T:files T:deleting) (+C:production +C:optimize)</code>
		/// it works.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestParenthesisMust()
		{
			BooleanQuery q3 = new BooleanQuery();
			q3.Add(new BooleanClause(t1, BooleanClause.Occur.SHOULD));
			q3.Add(new BooleanClause(t2, BooleanClause.Occur.SHOULD));
			BooleanQuery q4 = new BooleanQuery();
			q4.Add(new BooleanClause(c1, BooleanClause.Occur.MUST));
			q4.Add(new BooleanClause(c2, BooleanClause.Occur.MUST));
			BooleanQuery q2 = new BooleanQuery();
			q2.Add(q3, BooleanClause.Occur.SHOULD);
			q2.Add(q4, BooleanClause.Occur.SHOULD);
			NUnit.Framework.Assert.AreEqual(1, Search(q2));
		}

		/// <summary>
		/// <code>(T:files T:deleting) +(C:production C:optimize)</code>
		/// not working.
		/// </summary>
		/// <remarks>
		/// <code>(T:files T:deleting) +(C:production C:optimize)</code>
		/// not working. results NO HIT.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestParenthesisMust2()
		{
			BooleanQuery q3 = new BooleanQuery();
			q3.Add(new BooleanClause(t1, BooleanClause.Occur.SHOULD));
			q3.Add(new BooleanClause(t2, BooleanClause.Occur.SHOULD));
			BooleanQuery q4 = new BooleanQuery();
			q4.Add(new BooleanClause(c1, BooleanClause.Occur.SHOULD));
			q4.Add(new BooleanClause(c2, BooleanClause.Occur.SHOULD));
			BooleanQuery q2 = new BooleanQuery();
			q2.Add(q3, BooleanClause.Occur.SHOULD);
			q2.Add(q4, BooleanClause.Occur.MUST);
			NUnit.Framework.Assert.AreEqual(1, Search(q2));
		}

		/// <summary>
		/// <code>(T:files T:deleting) (C:production C:optimize)</code>
		/// not working.
		/// </summary>
		/// <remarks>
		/// <code>(T:files T:deleting) (C:production C:optimize)</code>
		/// not working. results NO HIT.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestParenthesisShould()
		{
			BooleanQuery q3 = new BooleanQuery();
			q3.Add(new BooleanClause(t1, BooleanClause.Occur.SHOULD));
			q3.Add(new BooleanClause(t2, BooleanClause.Occur.SHOULD));
			BooleanQuery q4 = new BooleanQuery();
			q4.Add(new BooleanClause(c1, BooleanClause.Occur.SHOULD));
			q4.Add(new BooleanClause(c2, BooleanClause.Occur.SHOULD));
			BooleanQuery q2 = new BooleanQuery();
			q2.Add(q3, BooleanClause.Occur.SHOULD);
			q2.Add(q4, BooleanClause.Occur.SHOULD);
			NUnit.Framework.Assert.AreEqual(1, Search(q2));
		}

		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			//
			dir = NewDirectory();
			//
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			//
			Lucene.Net.Document.Document d = new Lucene.Net.Document.Document();
			d.Add(NewField(FIELD_T, "Optimize not deleting all files", TextField.TYPE_STORED)
				);
			d.Add(NewField(FIELD_C, "Deleted When I run an optimize in our production environment."
				, TextField.TYPE_STORED));
			//
			writer.AddDocument(d);
			reader = writer.GetReader();
			//
			searcher = NewSearcher(reader);
			writer.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			reader.Close();
			dir.Close();
			base.TearDown();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestBooleanScorerMax()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter riw = new RandomIndexWriter(Random(), dir, NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
			int docCount = AtLeast(10000);
			for (int i = 0; i < docCount; i++)
			{
				Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
					();
				doc.Add(NewField("field", "a", TextField.TYPE_NOT_STORED));
				riw.AddDocument(doc);
			}
			riw.ForceMerge(1);
			IndexReader r = riw.GetReader();
			riw.Close();
			IndexSearcher s = NewSearcher(r);
			BooleanQuery bq = new BooleanQuery();
			bq.Add(new TermQuery(new Term("field", "a")), BooleanClause.Occur.SHOULD);
			bq.Add(new TermQuery(new Term("field", "a")), BooleanClause.Occur.SHOULD);
			Weight w = s.CreateNormalizedWeight(bq);
			NUnit.Framework.Assert.AreEqual(1, s.GetIndexReader().Leaves().Count);
			BulkScorer scorer = w.BulkScorer(s.GetIndexReader().Leaves()[0], false, null);
			FixedBitSet hits = new FixedBitSet(docCount);
			AtomicInteger end = new AtomicInteger();
			Collector c = new _Collector_190(end, hits);
			while (end < docCount)
			{
				int inc = TestUtil.NextInt(Random(), 1, 1000);
				end.GetAndAdd(inc);
				scorer.Score(c, end);
			}
			NUnit.Framework.Assert.AreEqual(docCount, hits.Cardinality());
			r.Close();
			dir.Close();
		}

		private sealed class _Collector_190 : Collector
		{
			public _Collector_190(AtomicInteger end, FixedBitSet hits)
			{
				this.end = end;
				this.hits = hits;
			}

			public override void SetNextReader(AtomicReaderContext sub)
			{
			}

			public override void Collect(int doc)
			{
				NUnit.Framework.Assert.IsTrue("collected doc=" + doc + " beyond max=" + end, doc 
					< end);
				hits.Set(doc);
			}

			public override void SetScorer(Scorer scorer)
			{
			}

			public override bool AcceptsDocsOutOfOrder()
			{
				return true;
			}

			private readonly AtomicInteger end;

			private readonly FixedBitSet hits;
		}
	}
}
