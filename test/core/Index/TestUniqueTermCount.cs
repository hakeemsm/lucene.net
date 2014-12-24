/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.Text;
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
	/// <summary>Tests the uniqueTermCount statistic in FieldInvertState</summary>
	public class TestUniqueTermCount : LuceneTestCase
	{
		internal Directory dir;

		internal IndexReader reader;

		internal AList<int> expected = new AList<int>();

		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			dir = NewDirectory();
			MockAnalyzer analyzer = new MockAnalyzer(Random(), MockTokenizer.SIMPLE, true);
			IndexWriterConfig config = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			config.SetMergePolicy(NewLogMergePolicy());
			config.SetSimilarity(new TestUniqueTermCount.TestSimilarity(this));
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, config);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			Field foo = NewTextField("foo", string.Empty, Field.Store.NO);
			doc.Add(foo);
			for (int i = 0; i < 100; i++)
			{
				foo.StringValue = AddValue());
				writer.AddDocument(doc);
			}
			reader = writer.GetReader();
			writer.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			reader.Close();
			dir.Close();
			base.TearDown();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void Test()
		{
			NumericDocValues fooNorms = MultiDocValues.GetNormValues(reader, "foo");
			IsNotNull(fooNorms);
			for (int i = 0; i < reader.MaxDoc; i++)
			{
				AreEqual(expected[i], fooNorms.Get(i));
			}
		}

		/// <summary>Makes a bunch of single-char tokens (the max # unique terms will at most be 26).
		/// 	</summary>
		/// <remarks>
		/// Makes a bunch of single-char tokens (the max # unique terms will at most be 26).
		/// puts the # unique terms into expected, to be checked against the norm.
		/// </remarks>
		private string AddValue()
		{
			StringBuilder sb = new StringBuilder();
			HashSet<string> terms = new HashSet<string>();
			int num = TestUtil.NextInt(Random(), 0, 255);
			for (int i = 0; i < num; i++)
			{
				sb.Append(' ');
				char term = (char)TestUtil.NextInt(Random(), 'a', 'z');
				sb.Append(term);
				terms.AddItem(string.Empty + term);
			}
			expected.AddItem(terms.Count);
			return sb.ToString();
		}

		/// <summary>Simple similarity that encodes maxTermFrequency directly</summary>
		internal class TestSimilarity : Similarity
		{
			public override long ComputeNorm(FieldInvertState state)
			{
				return state.GetUniqueTermCount();
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

			internal TestSimilarity(TestUniqueTermCount _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly TestUniqueTermCount _enclosing;
		}
	}
}
