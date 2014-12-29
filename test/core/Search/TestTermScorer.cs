/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using NUnit.Framework;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search
{
	public class TestTermScorer : LuceneTestCase
	{
		protected internal Directory directory;

		private static readonly string FIELD = "field";

		protected internal string[] values = new string[] { "all", "dogs dogs", "like", "playing"
			, "fetch", "all" };

		protected internal IndexSearcher indexSearcher;

		protected internal IndexReader indexReader;

		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			directory = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), directory, NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy
				()).SetSimilarity(new DefaultSimilarity()));
			for (int i = 0; i < values.Length; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(NewTextField(FIELD, values[i], Field.Store.YES));
				writer.AddDocument(doc);
			}
			indexReader = SlowCompositeReaderWrapper.Wrap(writer.Reader);
			writer.Dispose();
			indexSearcher = NewSearcher(indexReader);
			indexSearcher.SetSimilarity(new DefaultSimilarity());
		}

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			indexReader.Dispose();
			directory.Dispose();
			base.TearDown();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void Test()
		{
			Term allTerm = new Term(FIELD, "all");
			TermQuery termQuery = new TermQuery(allTerm);
			Weight weight = indexSearcher.CreateNormalizedWeight(termQuery);
			IsTrue(indexSearcher.GetTopReaderContext() is AtomicReaderContext
				);
			AtomicReaderContext context = (AtomicReaderContext)indexSearcher.GetTopReaderContext
				();
			BulkScorer ts = weight.BulkScorer(context, true, ((AtomicReader)context.Reader)
				.LiveDocs);
			// we have 2 documents with the term all in them, one document for all the
			// other values
			IList<TestTermScorer.TestHit> docs = new List<TestTermScorer.TestHit>();
			// must call next first
			ts.Score(new _Collector_87(docs));
			IsTrue("docs Size: " + docs.Count + " is not: " + 2, docs.
				Count == 2);
			TestTermScorer.TestHit doc0 = docs[0];
			TestTermScorer.TestHit doc5 = docs[1];
			// The scores should be the same
			IsTrue(doc0.score + " does not equal: " + doc5.score, doc0
				.score == doc5.score);
			IsTrue(doc0.score + " does not equal: " + 1.6931472f, doc0
				.score == 1.6931472f);
		}

		private sealed class _Collector_87 : Collector
		{
			public _Collector_87(IList<TestTermScorer.TestHit> docs)
			{
				this.docs = docs;
				this.@base = 0;
			}

			private int @base;

			private Scorer scorer;

			public override void SetScorer(Scorer scorer)
			{
				this.scorer = scorer;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Collect(int doc)
			{
				float score = this.scorer.Score();
				doc = doc + this.@base;
				docs.Add(new TestTermScorer.TestHit(this, doc, score));
				IsTrue("score " + score + " is not greater than 0", score 
					> 0);
				IsTrue("Doc: " + doc + " does not equal 0 or doc does not equal 5"
					, doc == 0 || doc == 5);
			}

			public override void SetNextReader(AtomicReaderContext context)
			{
				this.@base = context.docBase;
			}

			public override bool AcceptsDocsOutOfOrder()
			{
				return true;
			}

			private readonly IList<TestTermScorer.TestHit> docs;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestNext()
		{
			Term allTerm = new Term(FIELD, "all");
			TermQuery termQuery = new TermQuery(allTerm);
			Weight weight = indexSearcher.CreateNormalizedWeight(termQuery);
			IsTrue(indexSearcher.GetTopReaderContext() is AtomicReaderContext
				);
			AtomicReaderContext context = (AtomicReaderContext)indexSearcher.GetTopReaderContext
				();
			Scorer ts = weight.Scorer(context, ((AtomicReader)context.Reader).LiveDocs
				);
			IsTrue("next did not return a doc", ts.NextDoc() != DocIdSetIterator
				.NO_MORE_DOCS);
			IsTrue("score is not correct", ts.Score() == 1.6931472f);
			IsTrue("next did not return a doc", ts.NextDoc() != DocIdSetIterator
				.NO_MORE_DOCS);
			IsTrue("score is not correct", ts.Score() == 1.6931472f);
			IsTrue("next returned a doc and it should not have", ts.NextDoc
				() == DocIdSetIterator.NO_MORE_DOCS);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestAdvance()
		{
			Term allTerm = new Term(FIELD, "all");
			TermQuery termQuery = new TermQuery(allTerm);
			Weight weight = indexSearcher.CreateNormalizedWeight(termQuery);
			IsTrue(indexSearcher.GetTopReaderContext() is AtomicReaderContext
				);
			AtomicReaderContext context = (AtomicReaderContext)indexSearcher.GetTopReaderContext
				();
			Scorer ts = weight.Scorer(context, ((AtomicReader)context.Reader).LiveDocs
				);
			IsTrue("Didn't skip", ts.Advance(3) != DocIdSetIterator.NO_MORE_DOCS
				);
			// The next doc should be doc 5
			IsTrue("doc should be number 5", ts.DocID == 5);
		}

		private class TestHit
		{
			public int doc;

			public float score;

			public TestHit(TestTermScorer _enclosing, int doc, float score)
			{
				this._enclosing = _enclosing;
				this.Doc = doc;
				this.score = score;
			}

			public override string ToString()
			{
				return "TestHit{" + "doc=" + this.Doc + ", score=" + this.score + "}";
			}

			private readonly TestTermScorer _enclosing;
		}
	}
}
