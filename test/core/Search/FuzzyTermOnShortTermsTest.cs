/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.IO;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;


namespace Lucene.Net.Search
{
	public class FuzzyTermOnShortTermsTest : LuceneTestCase
	{
		private static readonly string FIELD = "field";

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void Test()
		{
			// proves rule that edit distance between the two terms
			// must be > smaller term for there to be a match
			Analyzer a = GetAnalyzer();
			//these work
			CountHits(a, new string[] { "abc" }, new FuzzyQuery(new Term(FIELD, "ab"), 1), 1);
			CountHits(a, new string[] { "ab" }, new FuzzyQuery(new Term(FIELD, "abc"), 1), 1);
			CountHits(a, new string[] { "abcde" }, new FuzzyQuery(new Term(FIELD, "abc"), 2), 
				1);
			CountHits(a, new string[] { "abc" }, new FuzzyQuery(new Term(FIELD, "abcde"), 2), 
				1);
			//these don't      
			CountHits(a, new string[] { "ab" }, new FuzzyQuery(new Term(FIELD, "a"), 1), 0);
			CountHits(a, new string[] { "a" }, new FuzzyQuery(new Term(FIELD, "ab"), 1), 0);
			CountHits(a, new string[] { "abc" }, new FuzzyQuery(new Term(FIELD, "a"), 2), 0);
			CountHits(a, new string[] { "a" }, new FuzzyQuery(new Term(FIELD, "abc"), 2), 0);
			CountHits(a, new string[] { "abcd" }, new FuzzyQuery(new Term(FIELD, "ab"), 2), 0
				);
			CountHits(a, new string[] { "ab" }, new FuzzyQuery(new Term(FIELD, "abcd"), 2), 0
				);
		}

		/// <exception cref="System.Exception"></exception>
		private void CountHits(Analyzer analyzer, string[] docs, Query q, int expected)
		{
			Directory d = GetDirectory(analyzer, docs);
			IndexReader r = DirectoryReader.Open(d);
			IndexSearcher s = new IndexSearcher(r);
			TotalHitCountCollector c = new TotalHitCountCollector();
			s.Search(q, c);
			AreEqual(q.ToString(), expected, c.GetTotalHits());
			r.Dispose();
			d.Dispose();
		}

		public static Analyzer GetAnalyzer()
		{
			return new _Analyzer_76();
		}

		private sealed class _Analyzer_76 : Analyzer
		{
			public _Analyzer_76()
			{
			}

			protected override Analyzer.TokenStreamComponents CreateComponents(string fieldName
				, StreamReader reader)
			{
				Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.SIMPLE, true);
				return new Analyzer.TokenStreamComponents(tokenizer, tokenizer);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static Directory GetDirectory(Analyzer analyzer, string[] vals)
		{
			Directory directory = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), directory, ((IndexWriterConfig
				)NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).SetMaxBufferedDocs(TestUtil
				.NextInt(Random(), 100, 1000))).SetMergePolicy(NewLogMergePolicy()));
			foreach (string s in vals)
			{
				Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
				d.Add(NewTextField(FIELD, s, Field.Store.YES));
				writer.AddDocument(d);
			}
			writer.Dispose();
			return directory;
		}
	}
}
