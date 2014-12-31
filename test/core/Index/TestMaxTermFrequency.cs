using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.TestFramework;
using Lucene.Net.TestFramework.Index;
using Lucene.Net.TestFramework.Util;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Index
{
	/// <summary>Tests the maxTermFrequency statistic in FieldInvertState</summary>
	[TestFixture]
    public class TestMaxTermFrequency : LuceneTestCase
	{
		internal Directory dir;

		internal IndexReader reader;

		internal List<int> expected = new List<int>();

		[SetUp]
		public override void SetUp()
		{
			base.SetUp();
			dir = NewDirectory();
			IndexWriterConfig config = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random(), MockTokenizer.SIMPLE, true)).SetMergePolicy(NewLogMergePolicy());
			config.SetSimilarity(new TestSimilarity(this));
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, config);
			var doc = new Lucene.Net.Documents.Document();
			Field foo = NewTextField("foo", string.Empty, Field.Store.NO);
			doc.Add(foo);
			for (int i = 0; i < 100; i++)
			{
				foo.StringValue = AddValue();
				writer.AddDocument(doc);
			}
			reader = writer.GetReader();
			writer.Close();
		}

		[TearDown]
		public override void TearDown()
		{
			reader.Dispose();
			dir.Dispose();
			base.TearDown();
		}

		[Test]
		public virtual void TestNormValues()
		{
			NumericDocValues fooNorms = MultiDocValues.GetNormValues(reader, "foo");
			for (int i = 0; i < reader.MaxDoc; i++)
			{
				AreEqual(expected[i], fooNorms.Get(i) & unchecked((int)(0xff
					)));
			}
		}

		/// <summary>Makes a bunch of single-char tokens (the max freq will at most be 255).</summary>
		/// <remarks>
		/// Makes a bunch of single-char tokens (the max freq will at most be 255).
		/// shuffles them around, and returns the whole list with Arrays.toString().
		/// This works fine because we use lettertokenizer.
		/// puts the max-frequency term into expected, to be checked against the norm.
		/// </remarks>
		private string AddValue()
		{
			IList<string> terms = new List<string>();
			int maxCeiling = TestUtil.NextInt(Random(), 0, 255);
			int max = 0;
			for (char ch = 'a'; ch <= 'z'; ch++)
			{
				int num = TestUtil.NextInt(Random(), 0, maxCeiling);
				for (int i = 0; i < num; i++)
				{
					terms.Add(char.ToString(ch));
				}
				max = Math.Max(max, num);
			}
			expected.Add(max);
			Collections.Shuffle(terms, Random());
			return Arrays.ToString(terms.ToArray());
		}

		/// <summary>Simple similarity that encodes maxTermFrequency directly as a byte</summary>
		internal class TestSimilarity : TFIDFSimilarity
		{
			public override float LengthNorm(FieldInvertState state)
			{
				return state.MaxTermFrequency;
			}

			public override long EncodeNormValue(float f)
			{
				return unchecked((byte)f);
			}

			public override float DecodeNormValue(long norm)
			{
				return norm;
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

			internal TestSimilarity(TestMaxTermFrequency _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly TestMaxTermFrequency _enclosing;
		}
	}
}
