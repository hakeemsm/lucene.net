/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	public class TestCustomNorms : LuceneTestCase
	{
		internal readonly string floatTestField = "normsTestFloat";

		internal readonly string exceptionTestField = "normsTestExcp";

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestFloatNorms()
		{
			Directory dir = NewDirectory();
			MockAnalyzer analyzer = new MockAnalyzer(Random());
			analyzer.SetMaxTokenLength(TestUtil.NextInt(Random(), 1, IndexWriter.MAX_TERM_LENGTH
				));
			IndexWriterConfig config = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			Similarity provider = new TestCustomNorms.MySimProvider(this);
			config.SetSimilarity(provider);
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, config);
			LineFileDocs docs = new LineFileDocs(Random());
			int num = AtLeast(100);
			for (int i = 0; i < num; i++)
			{
				Lucene.Net.Documents.Document doc = docs.NextDoc();
				float nextFloat = Random().NextFloat();
				Field f = new TextField(floatTestField, string.Empty + nextFloat, Field.Store.YES
					);
				f.SetBoost(nextFloat);
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
				float expected = float.ParseFloat(document.Get(floatTestField));
				AreEqual(expected, Sharpen.Runtime.IntBitsToFloat((int)norms
					.Get(i_1)), 0.0f);
			}
			open.Close();
			dir.Close();
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
				return Sharpen.Runtime.FloatToIntBits(state.GetBoost());
			}

			public override Similarity.SimWeight ComputeWeight(float queryBoost, CollectionStatistics
				 collectionStats, params TermStatistics[] termStats)
			{
				throw new NotSupportedException();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override Similarity.SimScorer SimScorer(Similarity.SimWeight weight, AtomicReaderContext
				 context)
			{
				throw new NotSupportedException();
			}
		}
	}
}
