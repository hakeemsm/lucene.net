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
	/// <summary>Tests the DocTermOrdsRangeFilter</summary>
	public class TestDocTermOrdsRangeFilter : LuceneTestCase
	{
		protected internal IndexSearcher searcher1;

		protected internal IndexSearcher searcher2;

		private IndexReader reader;

		private Directory dir;

		protected internal string fieldName;

		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			dir = NewDirectory();
			fieldName = Random().NextBoolean() ? "field" : string.Empty;
			// sometimes use an empty string as field name
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, ((IndexWriterConfig
				)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random(), MockTokenizer
				.KEYWORD, false)).SetMaxBufferedDocs(TestUtil.NextInt(Random(), 50, 1000))));
			IList<string> terms = new AList<string>();
			int num = AtLeast(200);
			for (int i = 0; i < num; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(NewStringField("id", i.ToString(), Field.Store.NO));
				int numTerms = Random().Next(4);
				for (int j = 0; j < numTerms; j++)
				{
					string s = TestUtil.RandomUnicodeString(Random());
					doc.Add(NewStringField(fieldName, s, Field.Store.NO));
					// if the default codec doesn't support sortedset, we will uninvert at search time
					if (DefaultCodecSupportsSortedSet())
					{
						doc.Add(new SortedSetDocValuesField(fieldName, new BytesRef(s)));
					}
					terms.AddItem(s);
				}
				writer.AddDocument(doc);
			}
			if (VERBOSE)
			{
				// utf16 order
				terms.Sort();
				System.Console.Out.WriteLine("UTF16 order:");
				foreach (string s in terms)
				{
					System.Console.Out.WriteLine("  " + UnicodeUtil.ToHexString(s));
				}
			}
			int numDeletions = Random().Next(num / 10);
			for (int i_1 = 0; i_1 < numDeletions; i_1++)
			{
				writer.DeleteDocuments(new Term("id", Sharpen.Extensions.ToString(Random().Next(num
					))));
			}
			reader = writer.GetReader();
			searcher1 = NewSearcher(reader);
			searcher2 = NewSearcher(reader);
			writer.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			reader.Dispose();
			dir.Dispose();
			base.TearDown();
		}

		/// <summary>test a bunch of random ranges</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestRanges()
		{
			int num = AtLeast(1000);
			for (int i = 0; i < num; i++)
			{
				BytesRef lowerVal = new BytesRef(TestUtil.RandomUnicodeString(Random()));
				BytesRef upperVal = new BytesRef(TestUtil.RandomUnicodeString(Random()));
				if (upperVal.CompareTo(lowerVal) < 0)
				{
					AssertSame(upperVal, lowerVal, Random().NextBoolean(), Random().NextBoolean());
				}
				else
				{
					AssertSame(lowerVal, upperVal, Random().NextBoolean(), Random().NextBoolean());
				}
			}
		}

		/// <summary>
		/// check that the # of hits is the same as if the query
		/// is run against the inverted index
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual void AssertSame(BytesRef lowerVal, BytesRef upperVal, 
			bool includeLower, bool includeUpper)
		{
			Query docValues = new ConstantScoreQuery(DocTermOrdsRangeFilter.NewBytesRefRange(
				fieldName, lowerVal, upperVal, includeLower, includeUpper));
			MultiTermQuery inverted = new TermRangeQuery(fieldName, lowerVal, upperVal, includeLower
				, includeUpper);
			inverted.SetRewriteMethod(MultiTermQuery.CONSTANT_SCORE_FILTER_REWRITE);
			TopDocs invertedDocs = searcher1.Search(inverted, 25);
			TopDocs docValuesDocs = searcher2.Search(docValues, 25);
			CheckHits.CheckEqual(inverted, invertedDocs.ScoreDocs, docValuesDocs.ScoreDocs);
		}
	}
}
