/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search
{
	/// <summary>Test BooleanQuery2 against BooleanQuery by overriding the standard query parser.
	/// 	</summary>
	/// <remarks>
	/// Test BooleanQuery2 against BooleanQuery by overriding the standard query parser.
	/// This also tests the scoring order of BooleanQuery.
	/// </remarks>
	public class TestBoolean2 : LuceneTestCase
	{
		private static IndexSearcher searcher;

		private static IndexSearcher bigSearcher;

		private static IndexReader reader;

		private static IndexReader littleReader;

		private static int NUM_EXTRA_DOCS = 6000;

		public static readonly string field = "field";

		private static Directory directory;

		private static Directory dir2;

		private static int mulFactor;

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.BeforeClass]
		public static void BeforeClass()
		{
			directory = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), directory, NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy
				()));
			for (int i = 0; i < docFields.Length; i++)
			{
				Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
					();
				doc.Add(NewTextField(field, docFields[i], Field.Store.NO));
				writer.AddDocument(doc);
			}
			writer.Close();
			littleReader = DirectoryReader.Open(directory);
			searcher = NewSearcher(littleReader);
			// this is intentionally using the baseline sim, because it compares against bigSearcher (which uses a random one)
			searcher.SetSimilarity(new DefaultSimilarity());
			// Make big index
			dir2 = new MockDirectoryWrapper(Random(), new RAMDirectory(directory, IOContext.DEFAULT
				));
			// First multiply small test index:
			mulFactor = 1;
			int docCount = 0;
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("\nTEST: now copy index...");
			}
			do
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("\nTEST: cycle...");
				}
				Directory copy = new MockDirectoryWrapper(Random(), new RAMDirectory(dir2, IOContext
					.DEFAULT));
				RandomIndexWriter w = new RandomIndexWriter(Random(), dir2);
				w.AddIndexes(copy);
				docCount = w.MaxDoc();
				w.Close();
				mulFactor *= 2;
			}
			while (docCount < 3000);
			RandomIndexWriter w_1 = new RandomIndexWriter(Random(), dir2, ((IndexWriterConfig
				)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs
				(TestUtil.NextInt(Random(), 50, 1000))));
			Lucene.Net.Document.Document doc_1 = new Lucene.Net.Document.Document
				();
			doc_1.Add(NewTextField("field2", "xxx", Field.Store.NO));
			for (int i_1 = 0; i_1 < NUM_EXTRA_DOCS / 2; i_1++)
			{
				w_1.AddDocument(doc_1);
			}
			doc_1 = new Lucene.Net.Document.Document();
			doc_1.Add(NewTextField("field2", "big bad bug", Field.Store.NO));
			for (int i_2 = 0; i_2 < NUM_EXTRA_DOCS / 2; i_2++)
			{
				w_1.AddDocument(doc_1);
			}
			reader = w_1.GetReader();
			bigSearcher = NewSearcher(reader);
			w_1.Close();
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.AfterClass]
		public static void AfterClass()
		{
			reader.Close();
			littleReader.Close();
			dir2.Close();
			directory.Close();
			searcher = null;
			reader = null;
			littleReader = null;
			dir2 = null;
			directory = null;
			bigSearcher = null;
		}

		private static string[] docFields = new string[] { "w1 w2 w3 w4 w5", "w1 w3 w2 w3"
			, "w1 xx w2 yy w3", "w1 w3 xx w2 yy w3" };

		/// <exception cref="System.Exception"></exception>
		public virtual void QueriesTest(Query query, int[] expDocNrs)
		{
			TopScoreDocCollector collector = TopScoreDocCollector.Create(1000, false);
			searcher.Search(query, null, collector);
			ScoreDoc[] hits1 = collector.TopDocs().scoreDocs;
			collector = TopScoreDocCollector.Create(1000, true);
			searcher.Search(query, null, collector);
			ScoreDoc[] hits2 = collector.TopDocs().scoreDocs;
			NUnit.Framework.Assert.AreEqual(mulFactor * collector.totalHits, bigSearcher.Search
				(query, 1).totalHits);
			CheckHits.CheckHitsQuery(query, hits1, hits2, expDocNrs);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestQueries01()
		{
			BooleanQuery query = new BooleanQuery();
			query.Add(new TermQuery(new Term(field, "w3")), BooleanClause.Occur.MUST);
			query.Add(new TermQuery(new Term(field, "xx")), BooleanClause.Occur.MUST);
			int[] expDocNrs = new int[] { 2, 3 };
			QueriesTest(query, expDocNrs);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestQueries02()
		{
			BooleanQuery query = new BooleanQuery();
			query.Add(new TermQuery(new Term(field, "w3")), BooleanClause.Occur.MUST);
			query.Add(new TermQuery(new Term(field, "xx")), BooleanClause.Occur.SHOULD);
			int[] expDocNrs = new int[] { 2, 3, 1, 0 };
			QueriesTest(query, expDocNrs);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestQueries03()
		{
			BooleanQuery query = new BooleanQuery();
			query.Add(new TermQuery(new Term(field, "w3")), BooleanClause.Occur.SHOULD);
			query.Add(new TermQuery(new Term(field, "xx")), BooleanClause.Occur.SHOULD);
			int[] expDocNrs = new int[] { 2, 3, 1, 0 };
			QueriesTest(query, expDocNrs);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestQueries04()
		{
			BooleanQuery query = new BooleanQuery();
			query.Add(new TermQuery(new Term(field, "w3")), BooleanClause.Occur.SHOULD);
			query.Add(new TermQuery(new Term(field, "xx")), BooleanClause.Occur.MUST_NOT);
			int[] expDocNrs = new int[] { 1, 0 };
			QueriesTest(query, expDocNrs);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestQueries05()
		{
			BooleanQuery query = new BooleanQuery();
			query.Add(new TermQuery(new Term(field, "w3")), BooleanClause.Occur.MUST);
			query.Add(new TermQuery(new Term(field, "xx")), BooleanClause.Occur.MUST_NOT);
			int[] expDocNrs = new int[] { 1, 0 };
			QueriesTest(query, expDocNrs);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestQueries06()
		{
			BooleanQuery query = new BooleanQuery();
			query.Add(new TermQuery(new Term(field, "w3")), BooleanClause.Occur.MUST);
			query.Add(new TermQuery(new Term(field, "xx")), BooleanClause.Occur.MUST_NOT);
			query.Add(new TermQuery(new Term(field, "w5")), BooleanClause.Occur.MUST_NOT);
			int[] expDocNrs = new int[] { 1 };
			QueriesTest(query, expDocNrs);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestQueries07()
		{
			BooleanQuery query = new BooleanQuery();
			query.Add(new TermQuery(new Term(field, "w3")), BooleanClause.Occur.MUST_NOT);
			query.Add(new TermQuery(new Term(field, "xx")), BooleanClause.Occur.MUST_NOT);
			query.Add(new TermQuery(new Term(field, "w5")), BooleanClause.Occur.MUST_NOT);
			int[] expDocNrs = new int[] {  };
			QueriesTest(query, expDocNrs);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestQueries08()
		{
			BooleanQuery query = new BooleanQuery();
			query.Add(new TermQuery(new Term(field, "w3")), BooleanClause.Occur.MUST);
			query.Add(new TermQuery(new Term(field, "xx")), BooleanClause.Occur.SHOULD);
			query.Add(new TermQuery(new Term(field, "w5")), BooleanClause.Occur.MUST_NOT);
			int[] expDocNrs = new int[] { 2, 3, 1 };
			QueriesTest(query, expDocNrs);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestQueries09()
		{
			BooleanQuery query = new BooleanQuery();
			query.Add(new TermQuery(new Term(field, "w3")), BooleanClause.Occur.MUST);
			query.Add(new TermQuery(new Term(field, "xx")), BooleanClause.Occur.MUST);
			query.Add(new TermQuery(new Term(field, "w2")), BooleanClause.Occur.MUST);
			query.Add(new TermQuery(new Term(field, "zz")), BooleanClause.Occur.SHOULD);
			int[] expDocNrs = new int[] { 2, 3 };
			QueriesTest(query, expDocNrs);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestQueries10()
		{
			BooleanQuery query = new BooleanQuery();
			query.Add(new TermQuery(new Term(field, "w3")), BooleanClause.Occur.MUST);
			query.Add(new TermQuery(new Term(field, "xx")), BooleanClause.Occur.MUST);
			query.Add(new TermQuery(new Term(field, "w2")), BooleanClause.Occur.MUST);
			query.Add(new TermQuery(new Term(field, "zz")), BooleanClause.Occur.SHOULD);
			int[] expDocNrs = new int[] { 2, 3 };
			Similarity oldSimilarity = searcher.GetSimilarity();
			try
			{
				searcher.SetSimilarity(new _DefaultSimilarity_245());
				QueriesTest(query, expDocNrs);
			}
			finally
			{
				searcher.SetSimilarity(oldSimilarity);
			}
		}

		private sealed class _DefaultSimilarity_245 : DefaultSimilarity
		{
			public _DefaultSimilarity_245()
			{
			}

			public override float Coord(int overlap, int maxOverlap)
			{
				return overlap / ((float)maxOverlap - 1);
			}
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestRandomQueries()
		{
			string[] vals = new string[] { "w1", "w2", "w3", "w4", "w5", "xx", "yy", "zzz" };
			int tot = 0;
			BooleanQuery q1 = null;
			try
			{
				// increase number of iterations for more complete testing
				int num = AtLeast(20);
				for (int i = 0; i < num; i++)
				{
					int level = Random().Next(3);
					q1 = RandBoolQuery(new Random(Random().NextLong()), Random().NextBoolean(), level
						, field, vals, null);
					// Can't sort by relevance since floating point numbers may not quite
					// match up.
					Sort sort = Sort.INDEXORDER;
					QueryUtils.Check(Random(), q1, searcher);
					// baseline sim
					try
					{
						// a little hackish, QueryUtils.check is too costly to do on bigSearcher in this loop.
						searcher.SetSimilarity(bigSearcher.GetSimilarity());
						// random sim
						QueryUtils.Check(Random(), q1, searcher);
					}
					finally
					{
						searcher.SetSimilarity(new DefaultSimilarity());
					}
					// restore
					TopFieldCollector collector = TopFieldCollector.Create(sort, 1000, false, true, true
						, true);
					searcher.Search(q1, null, collector);
					ScoreDoc[] hits1 = collector.TopDocs().scoreDocs;
					collector = TopFieldCollector.Create(sort, 1000, false, true, true, false);
					searcher.Search(q1, null, collector);
					ScoreDoc[] hits2 = collector.TopDocs().scoreDocs;
					tot += hits2.Length;
					CheckHits.CheckEqual(q1, hits1, hits2);
					BooleanQuery q3 = new BooleanQuery();
					q3.Add(q1, BooleanClause.Occur.SHOULD);
					q3.Add(new PrefixQuery(new Term("field2", "b")), BooleanClause.Occur.SHOULD);
					TopDocs hits4 = bigSearcher.Search(q3, 1);
					NUnit.Framework.Assert.AreEqual(mulFactor * collector.totalHits + NUM_EXTRA_DOCS 
						/ 2, hits4.totalHits);
				}
			}
			catch (Exception e)
			{
				// For easier debugging
				System.Console.Out.WriteLine("failed query: " + q1);
				throw;
			}
		}

		public interface Callback
		{
			// System.out.println("Total hits:"+tot);
			// used to set properties or change every BooleanQuery
			// generated from randBoolQuery.
			void PostCreate(BooleanQuery q);
		}

		// Random rnd is passed in so that the exact same random query may be created
		// more than once.
		public static BooleanQuery RandBoolQuery(Random rnd, bool allowMust, int level, string
			 field, string[] vals, TestBoolean2.Callback cb)
		{
			BooleanQuery current = new BooleanQuery(rnd.Next() < 0);
			for (int i = 0; i < rnd.Next(vals.Length) + 1; i++)
			{
				int qType = 0;
				// term query
				if (level > 0)
				{
					qType = rnd.Next(10);
				}
				Query q;
				if (qType < 3)
				{
					q = new TermQuery(new Term(field, vals[rnd.Next(vals.Length)]));
				}
				else
				{
					if (qType < 4)
					{
						Term t1 = new Term(field, vals[rnd.Next(vals.Length)]);
						Term t2 = new Term(field, vals[rnd.Next(vals.Length)]);
						PhraseQuery pq = new PhraseQuery();
						pq.Add(t1);
						pq.Add(t2);
						pq.SetSlop(10);
						// increase possibility of matching
						q = pq;
					}
					else
					{
						if (qType < 7)
						{
							q = new WildcardQuery(new Term(field, "w*"));
						}
						else
						{
							q = RandBoolQuery(rnd, allowMust, level - 1, field, vals, cb);
						}
					}
				}
				int r = rnd.Next(10);
				BooleanClause.Occur occur;
				if (r < 2)
				{
					occur = BooleanClause.Occur.MUST_NOT;
				}
				else
				{
					if (r < 5)
					{
						if (allowMust)
						{
							occur = BooleanClause.Occur.MUST;
						}
						else
						{
							occur = BooleanClause.Occur.SHOULD;
						}
					}
					else
					{
						occur = BooleanClause.Occur.SHOULD;
					}
				}
				current.Add(q, occur);
			}
			if (cb != null)
			{
				cb.PostCreate(current);
			}
			return current;
		}
	}
}
