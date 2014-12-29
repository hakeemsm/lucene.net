using System;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.TestFramework;
using Lucene.Net.TestFramework.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Index
{
    [TestFixture]
	public class TestCustomNorms : LuceneTestCase
	{
		internal readonly string floatTestField = "normsTestFloat";

		internal readonly string exceptionTestField = "normsTestExcp";

		[Test]
		public virtual void TestFloatNorms()
		{
			Directory dir = NewDirectory();
			MockAnalyzer analyzer = new MockAnalyzer(Random());
			analyzer.SetMaxTokenLength(Random().NextInt(1, IndexWriter.MAX_TERM_LENGTH));
			IndexWriterConfig config = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			Similarity provider = new TestCustomNorms.MySimProvider(this);
			config.SetSimilarity(provider);
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, config);
			LineFileDocs docs = new LineFileDocs(Random());
			int num = AtLeast(100);
			for (int i = 0; i < num; i++)
			{
				Lucene.Net.Documents.Document doc = docs.NextDoc();
				float nextFloat = (float) Random().NextDouble();
				Field f = new TextField(floatTestField, string.Empty + nextFloat, Field.Store.YES
					);
				f.Boost = (nextFloat);
				doc.Add(f);
				writer.AddDocument(doc);
				doc.RemoveField(floatTestField);
				if (Rarely())
				{
					writer.Commit();
				}
			}
			writer.Commit();
			writer.Close();
			AtomicReader open = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(dir));
			NumericDocValues norms = open.GetNormValues(floatTestField);
			IsNotNull(norms);
			for (int i_1 = 0; i_1 < open.MaxDoc; i_1++)
			{
				Lucene.Net.Documents.Document document = open.Document(i_1);
				float expected = float.Parse(document.Get(floatTestField));
                AreEqual(expected, ((int)norms.Get(i_1)).IntBitsToFloat(), 0.0f);
			}
			open.Dispose();
			dir.Dispose();
			docs.Close();
		}

		public class MySimProvider : PerFieldSimilarityWrapper
		{
			internal Similarity delegate_ = new DefaultSimilarity();

			public override float QueryNorm(float sumOfSquaredWeights)
			{
				return this.delegate_.QueryNorm(sumOfSquaredWeights);
			}

			public override Similarity Get(string field)
			{
				if (this._enclosing.floatTestField.Equals(field))
				{
					return new TestCustomNorms.FloatEncodingBoostSimilarity();
				}
				else
				{
					return this.delegate_;
				}
			}

			public override float Coord(int overlap, int maxOverlap)
			{
				return this.delegate_.Coord(overlap, maxOverlap);
			}

			internal MySimProvider(TestCustomNorms _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly TestCustomNorms _enclosing;
		}

		public class FloatEncodingBoostSimilarity : Similarity
		{
			public override long ComputeNorm(FieldInvertState state)
			{
                return (state.Boost).FloatToIntBits();
			}

			public override Similarity.SimWeight ComputeWeight(float queryBoost, CollectionStatistics
				 collectionStats, params TermStatistics[] termStats)
			{
				throw new NotSupportedException();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override SimScorer GetSimScorer(SimWeight weight, AtomicReaderContext
				 context)
			{
				throw new NotSupportedException();
			}
		}
	}
}
