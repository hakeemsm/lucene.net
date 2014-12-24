/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
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
	/// <summary>
	/// Tests
	/// <see cref="FuzzyQuery">FuzzyQuery</see>
	/// .
	/// </summary>
	public class TestFuzzyQuery : LuceneTestCase
	{
		/// <exception cref="System.Exception"></exception>
		public virtual void TestFuzziness()
		{
			Directory directory = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), directory);
			AddDoc("aaaaa", writer);
			AddDoc("aaaab", writer);
			AddDoc("aaabb", writer);
			AddDoc("aabbb", writer);
			AddDoc("abbbb", writer);
			AddDoc("bbbbb", writer);
			AddDoc("ddddd", writer);
			IndexReader reader = writer.GetReader();
			IndexSearcher searcher = NewSearcher(reader);
			writer.Close();
			FuzzyQuery query = new FuzzyQuery(new Term("field", "aaaaa"), FuzzyQuery.defaultMaxEdits
				, 0);
			ScoreDoc[] hits = searcher.Search(query, null, 1000).scoreDocs;
			AreEqual(3, hits.Length);
			// same with prefix
			query = new FuzzyQuery(new Term("field", "aaaaa"), FuzzyQuery.defaultMaxEdits, 1);
			hits = searcher.Search(query, null, 1000).scoreDocs;
			AreEqual(3, hits.Length);
			query = new FuzzyQuery(new Term("field", "aaaaa"), FuzzyQuery.defaultMaxEdits, 2);
			hits = searcher.Search(query, null, 1000).scoreDocs;
			AreEqual(3, hits.Length);
			query = new FuzzyQuery(new Term("field", "aaaaa"), FuzzyQuery.defaultMaxEdits, 3);
			hits = searcher.Search(query, null, 1000).scoreDocs;
			AreEqual(3, hits.Length);
			query = new FuzzyQuery(new Term("field", "aaaaa"), FuzzyQuery.defaultMaxEdits, 4);
			hits = searcher.Search(query, null, 1000).scoreDocs;
			AreEqual(2, hits.Length);
			query = new FuzzyQuery(new Term("field", "aaaaa"), FuzzyQuery.defaultMaxEdits, 5);
			hits = searcher.Search(query, null, 1000).scoreDocs;
			AreEqual(1, hits.Length);
			query = new FuzzyQuery(new Term("field", "aaaaa"), FuzzyQuery.defaultMaxEdits, 6);
			hits = searcher.Search(query, null, 1000).scoreDocs;
			AreEqual(1, hits.Length);
			// test scoring
			query = new FuzzyQuery(new Term("field", "bbbbb"), FuzzyQuery.defaultMaxEdits, 0);
			hits = searcher.Search(query, null, 1000).scoreDocs;
			AreEqual("3 documents should match", 3, hits.Length);
			IList<string> order = Arrays.AsList("bbbbb", "abbbb", "aabbb");
			for (int i = 0; i < hits.Length; i++)
			{
				string term = searcher.Doc(hits[i].doc).Get("field");
				//System.out.println(hits[i].score);
				AreEqual(order[i], term);
			}
			// test pq size by supplying maxExpansions=2
			// This query would normally return 3 documents, because 3 terms match (see above):
			query = new FuzzyQuery(new Term("field", "bbbbb"), FuzzyQuery.defaultMaxEdits, 0, 
				2, false);
			hits = searcher.Search(query, null, 1000).scoreDocs;
			AreEqual("only 2 documents should match", 2, hits.Length);
			order = Arrays.AsList("bbbbb", "abbbb");
			for (int i_1 = 0; i_1 < hits.Length; i_1++)
			{
				string term = searcher.Doc(hits[i_1].doc).Get("field");
				//System.out.println(hits[i].score);
				AreEqual(order[i_1], term);
			}
			// not similar enough:
			query = new FuzzyQuery(new Term("field", "xxxxx"), FuzzyQuery.defaultMaxEdits, 0);
			hits = searcher.Search(query, null, 1000).scoreDocs;
			AreEqual(0, hits.Length);
			query = new FuzzyQuery(new Term("field", "aaccc"), FuzzyQuery.defaultMaxEdits, 0);
			// edit distance to "aaaaa" = 3
			hits = searcher.Search(query, null, 1000).scoreDocs;
			AreEqual(0, hits.Length);
			// query identical to a word in the index:
			query = new FuzzyQuery(new Term("field", "aaaaa"), FuzzyQuery.defaultMaxEdits, 0);
			hits = searcher.Search(query, null, 1000).scoreDocs;
			AreEqual(3, hits.Length);
			AreEqual(searcher.Doc(hits[0].doc).Get("field"), ("aaaaa")
				);
			// default allows for up to two edits:
			AreEqual(searcher.Doc(hits[1].doc).Get("field"), ("aaaab")
				);
			AreEqual(searcher.Doc(hits[2].doc).Get("field"), ("aaabb")
				);
			// query similar to a word in the index:
			query = new FuzzyQuery(new Term("field", "aaaac"), FuzzyQuery.defaultMaxEdits, 0);
			hits = searcher.Search(query, null, 1000).scoreDocs;
			AreEqual(3, hits.Length);
			AreEqual(searcher.Doc(hits[0].doc).Get("field"), ("aaaaa")
				);
			AreEqual(searcher.Doc(hits[1].doc).Get("field"), ("aaaab")
				);
			AreEqual(searcher.Doc(hits[2].doc).Get("field"), ("aaabb")
				);
			// now with prefix
			query = new FuzzyQuery(new Term("field", "aaaac"), FuzzyQuery.defaultMaxEdits, 1);
			hits = searcher.Search(query, null, 1000).scoreDocs;
			AreEqual(3, hits.Length);
			AreEqual(searcher.Doc(hits[0].doc).Get("field"), ("aaaaa")
				);
			AreEqual(searcher.Doc(hits[1].doc).Get("field"), ("aaaab")
				);
			AreEqual(searcher.Doc(hits[2].doc).Get("field"), ("aaabb")
				);
			query = new FuzzyQuery(new Term("field", "aaaac"), FuzzyQuery.defaultMaxEdits, 2);
			hits = searcher.Search(query, null, 1000).scoreDocs;
			AreEqual(3, hits.Length);
			AreEqual(searcher.Doc(hits[0].doc).Get("field"), ("aaaaa")
				);
			AreEqual(searcher.Doc(hits[1].doc).Get("field"), ("aaaab")
				);
			AreEqual(searcher.Doc(hits[2].doc).Get("field"), ("aaabb")
				);
			query = new FuzzyQuery(new Term("field", "aaaac"), FuzzyQuery.defaultMaxEdits, 3);
			hits = searcher.Search(query, null, 1000).scoreDocs;
			AreEqual(3, hits.Length);
			AreEqual(searcher.Doc(hits[0].doc).Get("field"), ("aaaaa")
				);
			AreEqual(searcher.Doc(hits[1].doc).Get("field"), ("aaaab")
				);
			AreEqual(searcher.Doc(hits[2].doc).Get("field"), ("aaabb")
				);
			query = new FuzzyQuery(new Term("field", "aaaac"), FuzzyQuery.defaultMaxEdits, 4);
			hits = searcher.Search(query, null, 1000).scoreDocs;
			AreEqual(2, hits.Length);
			AreEqual(searcher.Doc(hits[0].doc).Get("field"), ("aaaaa")
				);
			AreEqual(searcher.Doc(hits[1].doc).Get("field"), ("aaaab")
				);
			query = new FuzzyQuery(new Term("field", "aaaac"), FuzzyQuery.defaultMaxEdits, 5);
			hits = searcher.Search(query, null, 1000).scoreDocs;
			AreEqual(0, hits.Length);
			query = new FuzzyQuery(new Term("field", "ddddX"), FuzzyQuery.defaultMaxEdits, 0);
			hits = searcher.Search(query, null, 1000).scoreDocs;
			AreEqual(1, hits.Length);
			AreEqual(searcher.Doc(hits[0].doc).Get("field"), ("ddddd")
				);
			// now with prefix
			query = new FuzzyQuery(new Term("field", "ddddX"), FuzzyQuery.defaultMaxEdits, 1);
			hits = searcher.Search(query, null, 1000).scoreDocs;
			AreEqual(1, hits.Length);
			AreEqual(searcher.Doc(hits[0].doc).Get("field"), ("ddddd")
				);
			query = new FuzzyQuery(new Term("field", "ddddX"), FuzzyQuery.defaultMaxEdits, 2);
			hits = searcher.Search(query, null, 1000).scoreDocs;
			AreEqual(1, hits.Length);
			AreEqual(searcher.Doc(hits[0].doc).Get("field"), ("ddddd")
				);
			query = new FuzzyQuery(new Term("field", "ddddX"), FuzzyQuery.defaultMaxEdits, 3);
			hits = searcher.Search(query, null, 1000).scoreDocs;
			AreEqual(1, hits.Length);
			AreEqual(searcher.Doc(hits[0].doc).Get("field"), ("ddddd")
				);
			query = new FuzzyQuery(new Term("field", "ddddX"), FuzzyQuery.defaultMaxEdits, 4);
			hits = searcher.Search(query, null, 1000).scoreDocs;
			AreEqual(1, hits.Length);
			AreEqual(searcher.Doc(hits[0].doc).Get("field"), ("ddddd")
				);
			query = new FuzzyQuery(new Term("field", "ddddX"), FuzzyQuery.defaultMaxEdits, 5);
			hits = searcher.Search(query, null, 1000).scoreDocs;
			AreEqual(0, hits.Length);
			// different field = no match:
			query = new FuzzyQuery(new Term("anotherfield", "ddddX"), FuzzyQuery.defaultMaxEdits
				, 0);
			hits = searcher.Search(query, null, 1000).scoreDocs;
			AreEqual(0, hits.Length);
			reader.Close();
			directory.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void Test2()
		{
			Directory directory = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), directory, new MockAnalyzer
				(Random(), MockTokenizer.KEYWORD, false));
			AddDoc("LANGE", writer);
			AddDoc("LUETH", writer);
			AddDoc("PIRSING", writer);
			AddDoc("RIEGEL", writer);
			AddDoc("TRZECZIAK", writer);
			AddDoc("WALKER", writer);
			AddDoc("WBR", writer);
			AddDoc("WE", writer);
			AddDoc("WEB", writer);
			AddDoc("WEBE", writer);
			AddDoc("WEBER", writer);
			AddDoc("WEBERE", writer);
			AddDoc("WEBREE", writer);
			AddDoc("WEBEREI", writer);
			AddDoc("WBRE", writer);
			AddDoc("WITTKOPF", writer);
			AddDoc("WOJNAROWSKI", writer);
			AddDoc("WRICKE", writer);
			IndexReader reader = writer.GetReader();
			IndexSearcher searcher = NewSearcher(reader);
			writer.Close();
			FuzzyQuery query = new FuzzyQuery(new Term("field", "WEBER"), 2, 1);
			//query.setRewriteMethod(FuzzyQuery.SCORING_BOOLEAN_QUERY_REWRITE);
			ScoreDoc[] hits = searcher.Search(query, null, 1000).scoreDocs;
			AreEqual(8, hits.Length);
			reader.Close();
			directory.Close();
		}

		/// <summary>
		/// MultiTermQuery provides (via attribute) information about which values
		/// must be competitive to enter the priority queue.
		/// </summary>
		/// <remarks>
		/// MultiTermQuery provides (via attribute) information about which values
		/// must be competitive to enter the priority queue.
		/// FuzzyQuery optimizes itself around this information, if the attribute
		/// is not implemented correctly, there will be problems!
		/// </remarks>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestTieBreaker()
		{
			Directory directory = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), directory);
			AddDoc("a123456", writer);
			AddDoc("c123456", writer);
			AddDoc("d123456", writer);
			AddDoc("e123456", writer);
			Directory directory2 = NewDirectory();
			RandomIndexWriter writer2 = new RandomIndexWriter(Random(), directory2);
			AddDoc("a123456", writer2);
			AddDoc("b123456", writer2);
			AddDoc("b123456", writer2);
			AddDoc("b123456", writer2);
			AddDoc("c123456", writer2);
			AddDoc("f123456", writer2);
			IndexReader ir1 = writer.GetReader();
			IndexReader ir2 = writer2.GetReader();
			MultiReader mr = new MultiReader(ir1, ir2);
			IndexSearcher searcher = NewSearcher(mr);
			FuzzyQuery fq = new FuzzyQuery(new Term("field", "z123456"), 1, 0, 2, false);
			TopDocs docs = searcher.Search(fq, 2);
			AreEqual(5, docs.TotalHits);
			// 5 docs, from the a and b's
			mr.Close();
			ir1.Close();
			ir2.Close();
			writer.Close();
			writer2.Close();
			directory.Close();
			directory2.Close();
		}

		/// <summary>Test the TopTermsBoostOnlyBooleanQueryRewrite rewrite method.</summary>
		/// <remarks>Test the TopTermsBoostOnlyBooleanQueryRewrite rewrite method.</remarks>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestBoostOnlyRewrite()
		{
			Directory directory = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), directory);
			AddDoc("Lucene", writer);
			AddDoc("Lucene", writer);
			AddDoc("Lucenne", writer);
			IndexReader reader = writer.GetReader();
			IndexSearcher searcher = NewSearcher(reader);
			writer.Close();
			FuzzyQuery query = new FuzzyQuery(new Term("field", "lucene"));
			query.SetRewriteMethod(new MultiTermQuery.TopTermsBoostOnlyBooleanQueryRewrite(50
				));
			ScoreDoc[] hits = searcher.Search(query, null, 1000).scoreDocs;
			AreEqual(3, hits.Length);
			// normally, 'Lucenne' would be the first result as IDF will skew the score.
			AreEqual("Lucene", reader.Document(hits[0].doc).Get("field"
				));
			AreEqual("Lucene", reader.Document(hits[1].doc).Get("field"
				));
			AreEqual("Lucenne", reader.Document(hits[2].doc).Get("field"
				));
			reader.Close();
			directory.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestGiga()
		{
			MockAnalyzer analyzer = new MockAnalyzer(Random());
			Directory index = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), index);
			AddDoc("Lucene in Action", w);
			AddDoc("Lucene for Dummies", w);
			//addDoc("Giga", w);
			AddDoc("Giga byte", w);
			AddDoc("ManagingGigabytesManagingGigabyte", w);
			AddDoc("ManagingGigabytesManagingGigabytes", w);
			AddDoc("The Art of Computer Science", w);
			AddDoc("J. K. Rowling", w);
			AddDoc("JK Rowling", w);
			AddDoc("Joanne K Roling", w);
			AddDoc("Bruce Willis", w);
			AddDoc("Willis bruce", w);
			AddDoc("Brute willis", w);
			AddDoc("B. willis", w);
			IndexReader r = w.GetReader();
			w.Close();
			Query q = new FuzzyQuery(new Term("field", "giga"), 0);
			// 3. search
			IndexSearcher searcher = NewSearcher(r);
			ScoreDoc[] hits = searcher.Search(q, 10).scoreDocs;
			AreEqual(1, hits.Length);
			AreEqual("Giga byte", searcher.Doc(hits[0].doc).Get("field"
				));
			r.Close();
			index.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestDistanceAsEditsSearching()
		{
			Directory index = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), index);
			AddDoc("foobar", w);
			AddDoc("test", w);
			AddDoc("working", w);
			IndexReader reader = w.GetReader();
			IndexSearcher searcher = NewSearcher(reader);
			w.Close();
			FuzzyQuery q = new FuzzyQuery(new Term("field", "fouba"), 2);
			ScoreDoc[] hits = searcher.Search(q, 10).scoreDocs;
			AreEqual(1, hits.Length);
			AreEqual("foobar", searcher.Doc(hits[0].doc).Get("field"));
			q = new FuzzyQuery(new Term("field", "foubara"), 2);
			hits = searcher.Search(q, 10).scoreDocs;
			AreEqual(1, hits.Length);
			AreEqual("foobar", searcher.Doc(hits[0].doc).Get("field"));
			try
			{
				q = new FuzzyQuery(new Term("field", "t"), 3);
				Fail();
			}
			catch (ArgumentException)
			{
			}
			// expected
			reader.Close();
			index.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void AddDoc(string text, RandomIndexWriter writer)
		{
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewTextField("field", text, Field.Store.YES));
			writer.AddDocument(doc);
		}
	}
}
