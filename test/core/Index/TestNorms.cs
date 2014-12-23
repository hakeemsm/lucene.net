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

namespace Lucene.Net.Index
{
	/// <summary>
	/// Test that norms info is preserved during index life - including
	/// separate norms, addDocument, addIndexes, forceMerge.
	/// </summary>
	/// <remarks>
	/// Test that norms info is preserved during index life - including
	/// separate norms, addDocument, addIndexes, forceMerge.
	/// </remarks>
	public class TestNorms : LuceneTestCase
	{
		internal readonly string byteTestField = "normsTestByte";

		internal class CustomNormEncodingSimilarity : TFIDFSimilarity
		{
			public override long EncodeNormValue(float f)
			{
				return (long)f;
			}

			public override float DecodeNormValue(long norm)
			{
				return norm;
			}

			public override float LengthNorm(FieldInvertState state)
			{
				return state.GetLength();
			}

			public override float Coord(int overlap, int maxOverlap)
			{
				return 0;
			}

			public override float QueryNorm(float sumOfSquaredWeights)
			{
				return 0;
			}

			public override float Tf(float freq)
			{
				return 0;
			}

			public override float Idf(long docFreq, long numDocs)
			{
				return 0;
			}

			public override float SloppyFreq(int distance)
			{
				return 0;
			}

			public override float ScorePayload(int doc, int start, int end, BytesRef payload)
			{
				return 0;
			}

			internal CustomNormEncodingSimilarity(TestNorms _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly TestNorms _enclosing;
		}

		// LUCENE-1260
		/// <exception cref="System.Exception"></exception>
		public virtual void TestCustomEncoder()
		{
			Directory dir = NewDirectory();
			MockAnalyzer analyzer = new MockAnalyzer(Random());
			IndexWriterConfig config = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			config.SetSimilarity(new TestNorms.CustomNormEncodingSimilarity(this));
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, config);
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			Field foo = NewTextField("foo", string.Empty, Field.Store.NO);
			Field bar = NewTextField("bar", string.Empty, Field.Store.NO);
			doc.Add(foo);
			doc.Add(bar);
			for (int i = 0; i < 100; i++)
			{
				bar.SetStringValue("singleton");
				writer.AddDocument(doc);
			}
			IndexReader reader = writer.GetReader();
			writer.Close();
			NumericDocValues fooNorms = MultiDocValues.GetNormValues(reader, "foo");
			for (int i_1 = 0; i_1 < reader.MaxDoc(); i_1++)
			{
				NUnit.Framework.Assert.AreEqual(0, fooNorms.Get(i_1));
			}
			NumericDocValues barNorms = MultiDocValues.GetNormValues(reader, "bar");
			for (int i_2 = 0; i_2 < reader.MaxDoc(); i_2++)
			{
				NUnit.Framework.Assert.AreEqual(1, barNorms.Get(i_2));
			}
			reader.Close();
			dir.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestMaxByteNorms()
		{
			Directory dir = NewFSDirectory(CreateTempDir("TestNorms.testMaxByteNorms"));
			BuildIndex(dir);
			AtomicReader open = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(dir));
			NumericDocValues normValues = open.GetNormValues(byteTestField);
			NUnit.Framework.Assert.IsNotNull(normValues);
			for (int i = 0; i < open.MaxDoc(); i++)
			{
				Lucene.Net.Document.Document document = open.Document(i);
				int expected = System.Convert.ToInt32(document.Get(byteTestField));
				NUnit.Framework.Assert.AreEqual(expected, normValues.Get(i) & unchecked((int)(0xff
					)));
			}
			open.Close();
			dir.Close();
		}

		// TODO: create a testNormsNotPresent ourselves by adding/deleting/merging docs
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void BuildIndex(Directory dir)
		{
			Random random = Random();
			MockAnalyzer analyzer = new MockAnalyzer(Random());
			analyzer.SetMaxTokenLength(TestUtil.NextInt(Random(), 1, IndexWriter.MAX_TERM_LENGTH
				));
			IndexWriterConfig config = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			Similarity provider = new TestNorms.MySimProvider(this);
			config.SetSimilarity(provider);
			RandomIndexWriter writer = new RandomIndexWriter(random, dir, config);
			LineFileDocs docs = new LineFileDocs(random, DefaultCodecSupportsDocValues());
			int num = AtLeast(100);
			for (int i = 0; i < num; i++)
			{
				Lucene.Net.Document.Document doc = docs.NextDoc();
				int boost = Random().Next(255);
				Field f = new TextField(byteTestField, string.Empty + boost, Field.Store.YES);
				f.SetBoost(boost);
				doc.Add(f);
				writer.AddDocument(doc);
				doc.RemoveField(byteTestField);
				if (Rarely())
				{
					writer.Commit();
				}
			}
			writer.Commit();
			writer.Close();
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
				if (this._enclosing.byteTestField.Equals(field))
				{
					return new TestNorms.ByteEncodingBoostSimilarity();
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

			internal MySimProvider(TestNorms _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly TestNorms _enclosing;
		}

		public class ByteEncodingBoostSimilarity : Similarity
		{
			public override long ComputeNorm(FieldInvertState state)
			{
				int boost = (int)state.GetBoost();
				return unchecked((byte)boost);
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
