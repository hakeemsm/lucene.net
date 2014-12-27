/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Search.Spans;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search
{
	public class TestBooleanQuery : LuceneTestCase
	{
		/// <exception cref="System.Exception"></exception>
		public virtual void TestEquality()
		{
			BooleanQuery bq1 = new BooleanQuery();
			bq1.Add(new TermQuery(new Term("field", "value1")), BooleanClause.Occur.SHOULD);
			bq1.Add(new TermQuery(new Term("field", "value2")), BooleanClause.Occur.SHOULD);
			BooleanQuery nested1 = new BooleanQuery();
			nested1.Add(new TermQuery(new Term("field", "nestedvalue1")), BooleanClause.Occur
				.SHOULD);
			nested1.Add(new TermQuery(new Term("field", "nestedvalue2")), BooleanClause.Occur
				.SHOULD);
			bq1.Add(nested1, BooleanClause.Occur.SHOULD);
			BooleanQuery bq2 = new BooleanQuery();
			bq2.Add(new TermQuery(new Term("field", "value1")), BooleanClause.Occur.SHOULD);
			bq2.Add(new TermQuery(new Term("field", "value2")), BooleanClause.Occur.SHOULD);
			BooleanQuery nested2 = new BooleanQuery();
			nested2.Add(new TermQuery(new Term("field", "nestedvalue1")), BooleanClause.Occur
				.SHOULD);
			nested2.Add(new TermQuery(new Term("field", "nestedvalue2")), BooleanClause.Occur
				.SHOULD);
			bq2.Add(nested2, BooleanClause.Occur.SHOULD);
			AreEqual(bq1, bq2);
		}

		public virtual void TestException()
		{
			try
			{
				BooleanQuery.SetMaxClauseCount(0);
				Fail();
			}
			catch (ArgumentException)
			{
			}
		}

		// okay
		// LUCENE-1630
		/// <exception cref="System.Exception"></exception>
		public virtual void TestNullOrSubScorer()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewTextField("field", "a b c d", Field.Store.NO));
			w.AddDocument(doc);
			IndexReader r = w.GetReader();
			IndexSearcher s = NewSearcher(r);
			// this test relies upon coord being the default implementation,
			// otherwise scores are different!
			s.SetSimilarity(new DefaultSimilarity());
			BooleanQuery q = new BooleanQuery();
			q.Add(new TermQuery(new Term("field", "a")), BooleanClause.Occur.SHOULD);
			// LUCENE-2617: make sure that a term not in the index still contributes to the score via coord factor
			float score = s.Search(q, 10).GetMaxScore();
			Query subQuery = new TermQuery(new Term("field", "not_in_index"));
			subQuery.SetBoost(0);
			q.Add(subQuery, BooleanClause.Occur.SHOULD);
			float score2 = s.Search(q, 10).GetMaxScore();
			AreEqual(score * .5F, score2, 1e-6);
			// LUCENE-2617: make sure that a clause not in the index still contributes to the score via coord factor
			BooleanQuery qq = ((BooleanQuery)q.Clone());
			PhraseQuery phrase = new PhraseQuery();
			phrase.Add(new Term("field", "not_in_index"));
			phrase.Add(new Term("field", "another_not_in_index"));
			phrase.SetBoost(0);
			qq.Add(phrase, BooleanClause.Occur.SHOULD);
			score2 = s.Search(qq, 10).GetMaxScore();
			AreEqual(score * (1 / 3F), score2, 1e-6);
			// now test BooleanScorer2
			subQuery = new TermQuery(new Term("field", "b"));
			subQuery.SetBoost(0);
			q.Add(subQuery, BooleanClause.Occur.MUST);
			score2 = s.Search(q, 10).GetMaxScore();
			AreEqual(score * (2 / 3F), score2, 1e-6);
			// PhraseQuery w/ no terms added returns a null scorer
			PhraseQuery pq = new PhraseQuery();
			q.Add(pq, BooleanClause.Occur.SHOULD);
			AreEqual(1, s.Search(q, 10).TotalHits);
			// A required clause which returns null scorer should return null scorer to
			// IndexSearcher.
			q = new BooleanQuery();
			pq = new PhraseQuery();
			q.Add(new TermQuery(new Term("field", "a")), BooleanClause.Occur.SHOULD);
			q.Add(pq, BooleanClause.Occur.MUST);
			AreEqual(0, s.Search(q, 10).TotalHits);
			DisjunctionMaxQuery dmq = new DisjunctionMaxQuery(1.0f);
			dmq.Add(new TermQuery(new Term("field", "a")));
			dmq.Add(pq);
			AreEqual(1, s.Search(dmq, 10).TotalHits);
			r.Dispose();
			w.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestDeMorgan()
		{
			Directory dir1 = NewDirectory();
			RandomIndexWriter iw1 = new RandomIndexWriter(Random(), dir1);
			Lucene.Net.Documents.Document doc1 = new Lucene.Net.Documents.Document
				();
			doc1.Add(NewTextField("field", "foo bar", Field.Store.NO));
			iw1.AddDocument(doc1);
			IndexReader reader1 = iw1.GetReader();
			iw1.Dispose();
			Directory dir2 = NewDirectory();
			RandomIndexWriter iw2 = new RandomIndexWriter(Random(), dir2);
			Lucene.Net.Documents.Document doc2 = new Lucene.Net.Documents.Document
				();
			doc2.Add(NewTextField("field", "foo baz", Field.Store.NO));
			iw2.AddDocument(doc2);
			IndexReader reader2 = iw2.GetReader();
			iw2.Dispose();
			BooleanQuery query = new BooleanQuery();
			// Query: +foo -ba*
			query.Add(new TermQuery(new Term("field", "foo")), BooleanClause.Occur.MUST);
			WildcardQuery wildcardQuery = new WildcardQuery(new Term("field", "ba*"));
			wildcardQuery.SetRewriteMethod(MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE);
			query.Add(wildcardQuery, BooleanClause.Occur.MUST_NOT);
			MultiReader multireader = new MultiReader(reader1, reader2);
			IndexSearcher searcher = NewSearcher(multireader);
			AreEqual(0, searcher.Search(query, 10).TotalHits);
			ExecutorService es = Executors.NewCachedThreadPool(new NamedThreadFactory("NRT search threads"
				));
			searcher = new IndexSearcher(multireader, es);
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("rewritten form: " + searcher.Rewrite(query));
			}
			AreEqual(0, searcher.Search(query, 10).TotalHits);
			es.Shutdown();
			es.AwaitTermination(1, TimeUnit.SECONDS);
			multireader.Dispose();
			reader1.Dispose();
			reader2.Dispose();
			dir1.Dispose();
			dir2.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestBS2DisjunctionNextVsAdvance()
		{
			Directory d = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), d);
			int numDocs = AtLeast(300);
			for (int docUpto = 0; docUpto < numDocs; docUpto++)
			{
				string contents = "a";
				if (Random().Next(20) <= 16)
				{
					contents += " b";
				}
				if (Random().Next(20) <= 8)
				{
					contents += " c";
				}
				if (Random().Next(20) <= 4)
				{
					contents += " d";
				}
				if (Random().Next(20) <= 2)
				{
					contents += " e";
				}
				if (Random().Next(20) <= 1)
				{
					contents += " f";
				}
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(new TextField("field", contents, Field.Store.NO));
				w.AddDocument(doc);
			}
			w.ForceMerge(1);
			IndexReader r = w.GetReader();
			IndexSearcher s = NewSearcher(r);
			w.Dispose();
			for (int iter = 0; iter < 10 * RANDOM_MULTIPLIER; iter++)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("iter=" + iter);
				}
				IList<string> terms = new AList<string>(Arrays.AsList("a", "b", "c", "d", "e", "f"
					));
				int numTerms = TestUtil.NextInt(Random(), 1, terms.Count);
				while (terms.Count > numTerms)
				{
					terms.Remove(Random().Next(terms.Count));
				}
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("  terms=" + terms);
				}
				BooleanQuery q = new BooleanQuery();
				foreach (string term in terms)
				{
					q.Add(new BooleanClause(new TermQuery(new Term("field", term)), BooleanClause.Occur
						.SHOULD));
				}
				Weight weight = s.CreateNormalizedWeight(q);
				Scorer scorer = weight.Scorer(s.leafContexts[0], null);
				// First pass: just use .nextDoc() to gather all hits
				IList<ScoreDoc> hits = new AList<ScoreDoc>();
				while (scorer.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
				{
					hits.AddItem(new ScoreDoc(scorer.DocID, scorer.Score()));
				}
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("  " + hits.Count + " hits");
				}
				// Now, randomly next/advance through the list and
				// verify exact match:
				for (int iter2 = 0; iter2 < 10; iter2++)
				{
					weight = s.CreateNormalizedWeight(q);
					scorer = weight.Scorer(s.leafContexts[0], null);
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("  iter2=" + iter2);
					}
					int upto = -1;
					while (upto < hits.Count)
					{
						int nextUpto;
						int nextDoc;
						int left = hits.Count - upto;
						if (left == 1 || Random().NextBoolean())
						{
							// next
							nextUpto = 1 + upto;
							nextDoc = scorer.NextDoc();
						}
						else
						{
							// advance
							int inc = TestUtil.NextInt(Random(), 1, left - 1);
							nextUpto = inc + upto;
							nextDoc = scorer.Advance(hits[nextUpto].Doc);
						}
						if (nextUpto == hits.Count)
						{
							AreEqual(DocIdSetIterator.NO_MORE_DOCS, nextDoc);
						}
						else
						{
							ScoreDoc hit = hits[nextUpto];
							AreEqual(hit.Doc, nextDoc);
							// Test for precise float equality:
							IsTrue("doc " + hit.Doc + " has wrong score: expected=" + 
								hit.score + " actual=" + scorer.Score(), hit.score == scorer.Score());
						}
						upto = nextUpto;
					}
				}
			}
			r.Dispose();
			d.Dispose();
		}

		// LUCENE-4477 / LUCENE-4401:
		/// <exception cref="System.Exception"></exception>
		public virtual void TestBooleanSpanQuery()
		{
			bool failed = false;
			int hits = 0;
			Directory directory = NewDirectory();
			Analyzer indexerAnalyzer = new MockAnalyzer(Random());
			IndexWriterConfig config = new IndexWriterConfig(TEST_VERSION_CURRENT, indexerAnalyzer
				);
			IndexWriter writer = new IndexWriter(directory, config);
			string FIELD = "content";
			Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
			d.Add(new TextField(FIELD, "clockwork orange", Field.Store.YES));
			writer.AddDocument(d);
			writer.Dispose();
			IndexReader indexReader = DirectoryReader.Open(directory);
			IndexSearcher searcher = NewSearcher(indexReader);
			BooleanQuery query = new BooleanQuery();
			SpanQuery sq1 = new SpanTermQuery(new Term(FIELD, "clockwork"));
			SpanQuery sq2 = new SpanTermQuery(new Term(FIELD, "clckwork"));
			query.Add(sq1, BooleanClause.Occur.SHOULD);
			query.Add(sq2, BooleanClause.Occur.SHOULD);
			TopScoreDocCollector collector = TopScoreDocCollector.Create(1000, true);
			searcher.Search(query, collector);
			hits = collector.TopDocs().ScoreDocs.Length;
			foreach (ScoreDoc scoreDoc in collector.TopDocs().ScoreDocs)
			{
				System.Console.Out.WriteLine(scoreDoc.Doc);
			}
			indexReader.Dispose();
			AreEqual("Bug in boolean query composed of span queries", 
				failed, false);
			AreEqual("Bug in boolean query composed of span queries", 
				hits, 1);
			directory.Dispose();
		}

		// LUCENE-5487
		/// <exception cref="System.Exception"></exception>
		public virtual void TestInOrderWithMinShouldMatch()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewTextField("field", "some text here", Field.Store.NO));
			w.AddDocument(doc);
			IndexReader r = w.GetReader();
			w.Dispose();
			IndexSearcher s = new _IndexSearcher_338(r);
			BooleanQuery bq = new BooleanQuery();
			bq.Add(new TermQuery(new Term("field", "some")), BooleanClause.Occur.SHOULD);
			bq.Add(new TermQuery(new Term("field", "text")), BooleanClause.Occur.SHOULD);
			bq.Add(new TermQuery(new Term("field", "here")), BooleanClause.Occur.SHOULD);
			bq.SetMinimumNumberShouldMatch(2);
			s.Search(bq, 10);
			r.Dispose();
			dir.Dispose();
		}

		private sealed class _IndexSearcher_338 : IndexSearcher
		{
			public _IndexSearcher_338(IndexReader baseArg1) : base(baseArg1)
			{
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected override void Search(IList<AtomicReaderContext> leaves, Weight weight, 
				Collector collector)
			{
				AreEqual(-1, collector.GetType().Name.IndexOf("OutOfOrder"
					));
				base.Search(leaves, weight, collector);
			}
		}
	}
}
