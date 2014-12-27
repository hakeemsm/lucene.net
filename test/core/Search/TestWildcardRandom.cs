/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Globalization;
using System.Text;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search
{
	/// <summary>Create an index with terms from 000-999.</summary>
	/// <remarks>
	/// Create an index with terms from 000-999.
	/// Generates random wildcards according to patterns,
	/// and validates the correct number of hits are returned.
	/// </remarks>
	public class TestWildcardRandom : LuceneTestCase
	{
		private IndexSearcher searcher;

		private IndexReader reader;

		private Directory dir;

		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, ((IndexWriterConfig
				)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs
				(TestUtil.NextInt(Random(), 50, 1000))));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			Field field = NewStringField("field", string.Empty, Field.Store.NO);
			doc.Add(field);
			NumberFormat df = new DecimalFormat("000", new DecimalFormatSymbols(CultureInfo.ROOT
				));
			for (int i = 0; i < 1000; i++)
			{
				field.StringValue = df.Format(i));
				writer.AddDocument(doc);
			}
			reader = writer.GetReader();
			searcher = NewSearcher(reader);
			writer.Dispose();
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: setUp searcher=" + searcher);
			}
		}

		private char N()
		{
			return (char)(unchecked((int)(0x30)) + Random().Next(10));
		}

		private string FillPattern(string wildcardPattern)
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < wildcardPattern.Length; i++)
			{
				switch (wildcardPattern[i])
				{
					case 'N':
					{
						sb.Append(N());
						break;
					}

					default:
					{
						sb.Append(wildcardPattern[i]);
						break;
					}
				}
			}
			return sb.ToString();
		}

		/// <exception cref="System.Exception"></exception>
		private void AssertPatternHits(string pattern, int numHits)
		{
			// TODO: run with different rewrites
			string filledPattern = FillPattern(pattern);
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: run wildcard pattern=" + pattern + " filled="
					 + filledPattern);
			}
			Query wq = new WildcardQuery(new Term("field", filledPattern));
			TopDocs docs = searcher.Search(wq, 25);
			AreEqual("Incorrect hits for pattern: " + pattern, numHits
				, docs.TotalHits);
		}

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			reader.Dispose();
			dir.Dispose();
			base.TearDown();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestWildcards()
		{
			int num = AtLeast(1);
			for (int i = 0; i < num; i++)
			{
				AssertPatternHits("NNN", 1);
				AssertPatternHits("?NN", 10);
				AssertPatternHits("N?N", 10);
				AssertPatternHits("NN?", 10);
			}
			for (int i_1 = 0; i_1 < num; i_1++)
			{
				AssertPatternHits("??N", 100);
				AssertPatternHits("N??", 100);
				AssertPatternHits("???", 1000);
				AssertPatternHits("NN*", 10);
				AssertPatternHits("N*", 100);
				AssertPatternHits("*", 1000);
				AssertPatternHits("*NN", 10);
				AssertPatternHits("*N", 100);
				AssertPatternHits("N*N", 10);
				// combo of ? and * operators
				AssertPatternHits("?N*", 100);
				AssertPatternHits("N?*", 100);
				AssertPatternHits("*N?", 100);
				AssertPatternHits("*??", 1000);
				AssertPatternHits("*?N", 100);
			}
		}
	}
}
