/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

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
	public class TestSimilarityProvider : LuceneTestCase
	{
		private Directory directory;

		private DirectoryReader reader;

		private IndexSearcher searcher;

		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			directory = NewDirectory();
			PerFieldSimilarityWrapper sim = new TestSimilarityProvider.ExampleSimilarityProvider
				(this);
			IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetSimilarity(sim);
			RandomIndexWriter iw = new RandomIndexWriter(Random(), directory, iwc);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			Field field = NewTextField("foo", string.Empty, Field.Store.NO);
			doc.Add(field);
			Field field2 = NewTextField("bar", string.Empty, Field.Store.NO);
			doc.Add(field2);
			field.StringValue = "quick brown fox");
			field2.StringValue = "quick brown fox");
			iw.AddDocument(doc);
			field.StringValue = "jumps over lazy brown dog");
			field2.StringValue = "jumps over lazy brown dog");
			iw.AddDocument(doc);
			reader = iw.GetReader();
			iw.Close();
			searcher = NewSearcher(reader);
			searcher.SetSimilarity(sim);
		}

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			reader.Close();
			directory.Close();
			base.TearDown();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestBasics()
		{
			// sanity check of norms writer
			// TODO: generalize
			AtomicReader slow = SlowCompositeReaderWrapper.Wrap(reader);
			NumericDocValues fooNorms = slow.GetNormValues("foo");
			NumericDocValues barNorms = slow.GetNormValues("bar");
			for (int i = 0; i < slow.MaxDoc; i++)
			{
				IsFalse(fooNorms.Get(i) == barNorms.Get(i));
			}
			// sanity check of searching
			TopDocs foodocs = searcher.Search(new TermQuery(new Term("foo", "brown")), 10);
			IsTrue(foodocs.TotalHits > 0);
			TopDocs bardocs = searcher.Search(new TermQuery(new Term("bar", "brown")), 10);
			IsTrue(bardocs.TotalHits > 0);
			IsTrue(foodocs.scoreDocs[0].score < bardocs.scoreDocs[0].score
				);
		}

		private class ExampleSimilarityProvider : PerFieldSimilarityWrapper
		{
			private Similarity sim1;

			private Similarity sim2;

			public override Similarity Get(string field)
			{
				if (field.Equals("foo"))
				{
					return this.sim1;
				}
				else
				{
					return this.sim2;
				}
			}

			public ExampleSimilarityProvider(TestSimilarityProvider _enclosing)
			{
				this._enclosing = _enclosing;
				sim1 = new TestSimilarityProvider.Sim1(this);
				sim2 = new TestSimilarityProvider.Sim2(this);
			}

			private readonly TestSimilarityProvider _enclosing;
		}

		private class Sim1 : TFIDFSimilarity
		{
			public override long EncodeNormValue(float f)
			{
				return (long)f;
			}

			public override float DecodeNormValue(long norm)
			{
				return norm;
			}

			public override float Coord(int overlap, int maxOverlap)
			{
				return 1f;
			}

			public override float QueryNorm(float sumOfSquaredWeights)
			{
				return 1f;
			}

			public override float LengthNorm(FieldInvertState state)
			{
				return 1f;
			}

			public override float SloppyFreq(int distance)
			{
				return 1f;
			}

			public override float Tf(float freq)
			{
				return 1f;
			}

			public override float Idf(long docFreq, long numDocs)
			{
				return 1f;
			}

			public override float ScorePayload(int doc, int start, int end, BytesRef payload)
			{
				return 1f;
			}

			internal Sim1(TestSimilarityProvider _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly TestSimilarityProvider _enclosing;
		}

		private class Sim2 : TFIDFSimilarity
		{
			public override long EncodeNormValue(float f)
			{
				return (long)f;
			}

			public override float DecodeNormValue(long norm)
			{
				return norm;
			}

			public override float Coord(int overlap, int maxOverlap)
			{
				return 1f;
			}

			public override float QueryNorm(float sumOfSquaredWeights)
			{
				return 1f;
			}

			public override float LengthNorm(FieldInvertState state)
			{
				return 10f;
			}

			public override float SloppyFreq(int distance)
			{
				return 10f;
			}

			public override float Tf(float freq)
			{
				return 10f;
			}

			public override float Idf(long docFreq, long numDocs)
			{
				return 10f;
			}

			public override float ScorePayload(int doc, int start, int end, BytesRef payload)
			{
				return 1f;
			}

			internal Sim2(TestSimilarityProvider _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly TestSimilarityProvider _enclosing;
		}
	}
}
