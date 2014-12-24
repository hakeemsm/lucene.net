/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search
{
	public class TestBooleanQueryVisitSubscorers : LuceneTestCase
	{
		internal Analyzer analyzer;

		internal IndexReader reader;

		internal IndexSearcher searcher;

		internal Directory dir;

		internal static readonly string F1 = "title";

		internal static readonly string F2 = "body";

		// TODO: refactor to a base class, that collects freqs from the scorer tree
		// and test all queries with it
		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			analyzer = new MockAnalyzer(Random());
			dir = NewDirectory();
			IndexWriterConfig config = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			config.SetMergePolicy(NewLogMergePolicy());
			// we will use docids to validate
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, config);
			writer.AddDocument(Doc("lucene", "lucene is a very popular search engine library"
				));
			writer.AddDocument(Doc("solr", "solr is a very popular search server and is using lucene"
				));
			writer.AddDocument(Doc("nutch", "nutch is an internet search engine with web crawler and is using lucene and hadoop"
				));
			reader = writer.GetReader();
			writer.Close();
			searcher = NewSearcher(reader);
		}

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			reader.Close();
			dir.Close();
			base.TearDown();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestDisjunctions()
		{
			BooleanQuery bq = new BooleanQuery();
			bq.Add(new TermQuery(new Term(F1, "lucene")), BooleanClause.Occur.SHOULD);
			bq.Add(new TermQuery(new Term(F2, "lucene")), BooleanClause.Occur.SHOULD);
			bq.Add(new TermQuery(new Term(F2, "search")), BooleanClause.Occur.SHOULD);
			IDictionary<int, int> tfs = GetDocCounts(searcher, bq);
			AreEqual(3, tfs.Count);
			// 3 documents
			AreEqual(3, tfs.Get(0));
			// f1:lucene + f2:lucene + f2:search
			AreEqual(2, tfs.Get(1));
			// f2:search + f2:lucene
			AreEqual(2, tfs.Get(2));
		}

		// f2:search + f2:lucene
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestNestedDisjunctions()
		{
			BooleanQuery bq = new BooleanQuery();
			bq.Add(new TermQuery(new Term(F1, "lucene")), BooleanClause.Occur.SHOULD);
			BooleanQuery bq2 = new BooleanQuery();
			bq2.Add(new TermQuery(new Term(F2, "lucene")), BooleanClause.Occur.SHOULD);
			bq2.Add(new TermQuery(new Term(F2, "search")), BooleanClause.Occur.SHOULD);
			bq.Add(bq2, BooleanClause.Occur.SHOULD);
			IDictionary<int, int> tfs = GetDocCounts(searcher, bq);
			AreEqual(3, tfs.Count);
			// 3 documents
			AreEqual(3, tfs.Get(0));
			// f1:lucene + f2:lucene + f2:search
			AreEqual(2, tfs.Get(1));
			// f2:search + f2:lucene
			AreEqual(2, tfs.Get(2));
		}

		// f2:search + f2:lucene
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestConjunctions()
		{
			BooleanQuery bq = new BooleanQuery();
			bq.Add(new TermQuery(new Term(F2, "lucene")), BooleanClause.Occur.MUST);
			bq.Add(new TermQuery(new Term(F2, "is")), BooleanClause.Occur.MUST);
			IDictionary<int, int> tfs = GetDocCounts(searcher, bq);
			AreEqual(3, tfs.Count);
			// 3 documents
			AreEqual(2, tfs.Get(0));
			// f2:lucene + f2:is
			AreEqual(3, tfs.Get(1));
			// f2:is + f2:is + f2:lucene
			AreEqual(3, tfs.Get(2));
		}

		// f2:is + f2:is + f2:lucene
		internal static Lucene.Net.Documents.Document Doc(string v1, string v2)
		{
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new TextField(F1, v1, Field.Store.YES));
			doc.Add(new TextField(F2, v2, Field.Store.YES));
			return doc;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal static IDictionary<int, int> GetDocCounts(IndexSearcher searcher, Query 
			query)
		{
			TestBooleanQueryVisitSubscorers.MyCollector collector = new TestBooleanQueryVisitSubscorers.MyCollector
				();
			searcher.Search(query, collector);
			return collector.docCounts;
		}

		internal class MyCollector : Collector
		{
			private TopDocsCollector<ScoreDoc> collector;

			private int docBase;

			public readonly IDictionary<int, int> docCounts = new Dictionary<int, int>();

			private readonly ICollection<Scorer> tqsSet = new HashSet<Scorer>();

			public MyCollector()
			{
				collector = TopScoreDocCollector.Create(10, true);
			}

			public override bool AcceptsDocsOutOfOrder()
			{
				return false;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Collect(int doc)
			{
				int freq = 0;
				foreach (Scorer scorer in tqsSet)
				{
					if (doc == scorer.DocID)
					{
						freq += scorer.Freq;
					}
				}
				docCounts.Put(doc + docBase, freq);
				collector.Collect(doc);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void SetNextReader(AtomicReaderContext context)
			{
				this.docBase = context.docBase;
				collector.SetNextReader(context);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void SetScorer(Scorer scorer)
			{
				collector.SetScorer(scorer);
				tqsSet.Clear();
				FillLeaves(scorer, tqsSet);
			}

			private void FillLeaves(Scorer scorer, ICollection<Scorer> set)
			{
				if (scorer.GetWeight().GetQuery() is TermQuery)
				{
					set.AddItem(scorer);
				}
				else
				{
					foreach (Scorer.ChildScorer child in scorer.GetChildren())
					{
						FillLeaves(child.child, set);
					}
				}
			}

			public virtual Lucene.Net.Search.TopDocs TopDocs()
			{
				return collector.TopDocs();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public virtual int Freq(int doc)
			{
				return docCounts.Get(doc);
			}
		}
	}
}
