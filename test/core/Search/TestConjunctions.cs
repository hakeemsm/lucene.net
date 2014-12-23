/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

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
	public class TestConjunctions : LuceneTestCase
	{
		internal Analyzer analyzer;

		internal Directory dir;

		internal IndexReader reader;

		internal IndexSearcher searcher;

		internal static readonly string F1 = "title";

		internal static readonly string F2 = "body";

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
			searcher.SetSimilarity(new TestConjunctions.TFSimilarity());
		}

		internal static Lucene.Net.Document.Document Doc(string v1, string v2)
		{
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			doc.Add(new StringField(F1, v1, Field.Store.YES));
			doc.Add(new TextField(F2, v2, Field.Store.YES));
			return doc;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestTermConjunctionsWithOmitTF()
		{
			BooleanQuery bq = new BooleanQuery();
			bq.Add(new TermQuery(new Term(F1, "nutch")), BooleanClause.Occur.MUST);
			bq.Add(new TermQuery(new Term(F2, "is")), BooleanClause.Occur.MUST);
			TopDocs td = searcher.Search(bq, 3);
			NUnit.Framework.Assert.AreEqual(1, td.totalHits);
			NUnit.Framework.Assert.AreEqual(3F, td.scoreDocs[0].score, 0.001F);
		}

		// f1:nutch + f2:is + f2:is
		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			reader.Close();
			dir.Close();
			base.TearDown();
		}

		private class TFSimilarity : Similarity
		{
			// Similarity that returns the TF as score
			public override long ComputeNorm(FieldInvertState state)
			{
				return 1;
			}

			// we dont care
			public override Similarity.SimWeight ComputeWeight(float queryBoost, CollectionStatistics
				 collectionStats, params TermStatistics[] termStats)
			{
				return new _SimWeight_99();
			}

			private sealed class _SimWeight_99 : Similarity.SimWeight
			{
				public _SimWeight_99()
				{
				}

				public override float GetValueForNormalization()
				{
					return 1;
				}

				// we don't care
				public override void Normalize(float queryNorm, float topLevelBoost)
				{
				}
			}

			// we don't care
			/// <exception cref="System.IO.IOException"></exception>
			public override Similarity.SimScorer SimScorer(Similarity.SimWeight weight, AtomicReaderContext
				 context)
			{
				return new _SimScorer_113();
			}

			private sealed class _SimScorer_113 : Similarity.SimScorer
			{
				public _SimScorer_113()
				{
				}

				public override float Score(int doc, float freq)
				{
					return freq;
				}

				public override float ComputeSlopFactor(int distance)
				{
					return 1F;
				}

				public override float ComputePayloadFactor(int doc, int start, int end, BytesRef 
					payload)
				{
					return 1F;
				}
			}
		}
	}
}
