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
using Lucene.Net.Store;
using Lucene.Net.Util;


namespace Lucene.Net.Search
{
	public class TestSubScorerFreqs : LuceneTestCase
	{
		private static Directory dir;

		private static IndexSearcher s;

		/// <exception cref="System.Exception"></exception>
		[BeforeClass]
		public static void MakeIndex()
		{
			dir = new RAMDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy()));
			// make sure we have more than one segment occationally
			int num = AtLeast(31);
			for (int i = 0; i < num; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(NewTextField("f", "a b c d b c d c d d", Field.Store.NO));
				w.AddDocument(doc);
				doc = new Lucene.Net.Documents.Document();
				doc.Add(NewTextField("f", "a b c d", Field.Store.NO));
				w.AddDocument(doc);
			}
			s = NewSearcher(w.Reader);
			w.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		[AfterClass]
		public static void Finish()
		{
			s.IndexReader.Dispose();
			s = null;
			dir.Dispose();
			dir = null;
		}

		private class CountingCollector : Collector
		{
			private readonly Collector other;

			private int docBase;

			public readonly IDictionary<int, IDictionary<Query, float>> docCounts = new Dictionary
				<int, IDictionary<Query, float>>();

			private readonly IDictionary<Query, Scorer> subScorers = new Dictionary<Query, Scorer
				>();

			private readonly ICollection<string> relationships;

			public CountingCollector(Collector other) : this(other, new HashSet<string>(Arrays
				.AsList("MUST", "SHOULD", "MUST_NOT")))
			{
			}

			public CountingCollector(Collector other, ICollection<string> relationships)
			{
				this.other = other;
				this.relationships = relationships;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void SetScorer(Scorer scorer)
			{
				other.SetScorer(scorer);
				subScorers.Clear();
				SetSubScorers(scorer, "TOP");
			}

			public virtual void SetSubScorers(Scorer scorer, string relationship)
			{
				foreach (Scorer.ChildScorer child in scorer.GetChildren())
				{
					if (scorer is AssertingScorer || relationships.Contains(child.relationship))
					{
						SetSubScorers(child.child, child.relationship);
					}
				}
				subScorers.Put(scorer.GetWeight().GetQuery(), scorer);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Collect(int doc)
			{
				IDictionary<Query, float> freqs = new Dictionary<Query, float>();
				foreach (KeyValuePair<Query, Scorer> ent in subScorers.EntrySet())
				{
					Scorer value = ent.Value;
					int matchId = value.DocID;
					freqs.Put(ent.Key, matchId == doc ? value.Freq : 0.0f);
				}
				docCounts.Put(doc + docBase, freqs);
				other.Collect(doc);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void SetNextReader(AtomicReaderContext context)
			{
				docBase = context.docBase;
				other.SetNextReader(context);
			}

			public override bool AcceptsDocsOutOfOrder()
			{
				return other.AcceptsDocsOutOfOrder();
			}
		}

		private const float FLOAT_TOLERANCE = 0.00001F;

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestTermQuery()
		{
			TermQuery q = new TermQuery(new Term("f", "d"));
			TestSubScorerFreqs.CountingCollector c = new TestSubScorerFreqs.CountingCollector
				(TopScoreDocCollector.Create(10, true));
			s.Search(q, null, c);
			int maxDocs = s.IndexReader.MaxDoc;
			AreEqual(maxDocs, c.docCounts.Count);
			for (int i = 0; i < maxDocs; i++)
			{
				IDictionary<Query, float> doc0 = c.docCounts.Get(i);
				AreEqual(1, doc0.Count);
				AreEqual(4.0F, doc0.Get(q), FLOAT_TOLERANCE);
				IDictionary<Query, float> doc1 = c.docCounts.Get(++i);
				AreEqual(1, doc1.Count);
				AreEqual(1.0F, doc1.Get(q), FLOAT_TOLERANCE);
			}
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestBooleanQuery()
		{
			TermQuery aQuery = new TermQuery(new Term("f", "a"));
			TermQuery dQuery = new TermQuery(new Term("f", "d"));
			TermQuery cQuery = new TermQuery(new Term("f", "c"));
			TermQuery yQuery = new TermQuery(new Term("f", "y"));
			BooleanQuery query = new BooleanQuery();
			BooleanQuery inner = new BooleanQuery();
			inner.Add(cQuery, BooleanClause.Occur.SHOULD);
			inner.Add(yQuery, BooleanClause.Occur.MUST_NOT);
			query.Add(inner, BooleanClause.Occur.MUST);
			query.Add(aQuery, BooleanClause.Occur.MUST);
			query.Add(dQuery, BooleanClause.Occur.MUST);
			// Only needed in Java6; Java7+ has a @SafeVarargs annotated Arrays#asList()!
			// see http://docs.oracle.com/javase/7/docs/api/java/lang/SafeVarargs.html
			IEnumerable<ICollection<string>> occurList = Arrays.AsList(Collections.Singleton
				("MUST"), new HashSet<string>(Arrays.AsList("MUST", "SHOULD")));
			foreach (ICollection<string> occur in occurList)
			{
				TestSubScorerFreqs.CountingCollector c = new TestSubScorerFreqs.CountingCollector
					(TopScoreDocCollector.Create(10, true), occur);
				s.Search(query, null, c);
				int maxDocs = s.IndexReader.MaxDoc;
				AreEqual(maxDocs, c.docCounts.Count);
				bool includeOptional = occur.Contains("SHOULD");
				for (int i = 0; i < maxDocs; i++)
				{
					IDictionary<Query, float> doc0 = c.docCounts.Get(i);
					AreEqual(includeOptional ? 5 : 4, doc0.Count);
					AreEqual(1.0F, doc0.Get(aQuery), FLOAT_TOLERANCE);
					AreEqual(4.0F, doc0.Get(dQuery), FLOAT_TOLERANCE);
					if (includeOptional)
					{
						AreEqual(3.0F, doc0.Get(cQuery), FLOAT_TOLERANCE);
					}
					IDictionary<Query, float> doc1 = c.docCounts.Get(++i);
					AreEqual(includeOptional ? 5 : 4, doc1.Count);
					AreEqual(1.0F, doc1.Get(aQuery), FLOAT_TOLERANCE);
					AreEqual(1.0F, doc1.Get(dQuery), FLOAT_TOLERANCE);
					if (includeOptional)
					{
						AreEqual(1.0F, doc1.Get(cQuery), FLOAT_TOLERANCE);
					}
				}
			}
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestPhraseQuery()
		{
			PhraseQuery q = new PhraseQuery();
			q.Add(new Term("f", "b"));
			q.Add(new Term("f", "c"));
			TestSubScorerFreqs.CountingCollector c = new TestSubScorerFreqs.CountingCollector
				(TopScoreDocCollector.Create(10, true));
			s.Search(q, null, c);
			int maxDocs = s.IndexReader.MaxDoc;
			AreEqual(maxDocs, c.docCounts.Count);
			for (int i = 0; i < maxDocs; i++)
			{
				IDictionary<Query, float> doc0 = c.docCounts.Get(i);
				AreEqual(1, doc0.Count);
				AreEqual(2.0F, doc0.Get(q), FLOAT_TOLERANCE);
				IDictionary<Query, float> doc1 = c.docCounts.Get(++i);
				AreEqual(1, doc1.Count);
				AreEqual(1.0F, doc1.Get(q), FLOAT_TOLERANCE);
			}
		}
	}
}
