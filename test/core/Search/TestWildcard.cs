/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

using NUnit.Framework;

using SimpleAnalyzer = Lucene.Net.Test.Analysis.SimpleAnalyzer;
using WhitespaceAnalyzer = Lucene.Net.Test.Analysis.WhitespaceAnalyzer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using Index = Lucene.Net.Documents.Field.Index;
using Store = Lucene.Net.Documents.Field.Store;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using Term = Lucene.Net.Index.Term;
using QueryParser = Lucene.Net.QueryParsers.QueryParser;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

namespace Lucene.Net.Search
{
	
	/// <summary>TestWildcard tests the '*' and '?' wildcard characters.</summary>
    [TestFixture]
	public class TestWildcard:LuceneTestCase
	{
		[Test]
		public virtual void  TestEquals()
		{
			WildcardQuery wq1 = new WildcardQuery(new Term("field", "b*a"));
			WildcardQuery wq2 = new WildcardQuery(new Term("field", "b*a"));
			WildcardQuery wq3 = new WildcardQuery(new Term("field", "b*a"));
			
			// reflexive?
			Assert.AreEqual(wq1, wq2);
			Assert.AreEqual(wq2, wq1);
			
			// transitive?
			Assert.AreEqual(wq2, wq3);
			Assert.AreEqual(wq1, wq3);
			
			Assert.IsFalse(wq1.Equals(null));
			
			FuzzyQuery fq = new FuzzyQuery(new Term("field", "b*a"));
			Assert.IsFalse(wq1.Equals(fq));
			Assert.IsFalse(fq.Equals(wq1));
		}
		
		/// <summary> Tests if a WildcardQuery that has no wildcard in the term is rewritten to a single
		/// TermQuery.  The boost should be prserved, and the rewrite should return
		/// a ConstantScoreQuery if the WildcardQuery had a ConstantScore rewriteMethod.
		/// </summary>
        [Test]
        public virtual void TestTermWithoutWildcard()
		{
		    RAMDirectory indexStore = GetIndexStore("field", new System.String[] {"nowildcard", "nowildcardx"});
			IndexReader reader = DirectoryReader.Open(indexStore);
			IndexSearcher searcher = NewSearcher(reader);

		    MultiTermQuery wq = new WildcardQuery(new Term("field", "nowildcard"));
		    AssertMatches(searcher, wq, 1);

		    wq.RewriteMethod = MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE;
		    wq.Boost = 0.1f;
		    Query q = searcher.Rewrite(wq);
		    Assert.IsTrue(q is TermQuery);
		    Assert.AreEqual(q.Boost, wq.Boost);

		    wq.RewriteMethod = MultiTermQuery.CONSTANT_SCORE_FILTER_REWRITE;
		    wq.Boost = 0.2f;
		    q = searcher.Rewrite(wq);
		    Assert.True(q is ConstantScoreQuery);
		    Assert.AreEqual(q.Boost, wq.Boost);

		    wq.RewriteMethod = MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT;
		    wq.Boost = 0.3F;
		    q = searcher.Rewrite(wq);
		    Assert.True(q is ConstantScoreQuery);
		    Assert.AreEqual(q.Boost, wq.Boost);

		    wq.RewriteMethod = MultiTermQuery.CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE;
		    wq.Boost = 0.4F;
		    q = searcher.Rewrite(wq);
		    Assert.True(q is ConstantScoreQuery);
		    Assert.AreEqual(q.Boost, wq.Boost);
			reader.Close();
			indexStore.Close();
		}

        /// <summary>
        /// Tests if a WildcardQuery with an empty term is rewritten to an empty BooleanQuery
        /// </summary>
        [Test]
        public void TestEmptyTerm()
        {
            RAMDirectory indexStore = GetIndexStore("field", new String[] { "nowildcard", "nowildcardx" });
			IndexReader reader = DirectoryReader.Open(indexStore);
			IndexSearcher searcher = NewSearcher(reader);

            MultiTermQuery wq = new WildcardQuery(new Term("field", ""));
            wq.RewriteMethod = MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE;
            AssertMatches(searcher, wq, 0);
			Query q = searcher.Rewrite(wq);
			IsTrue(q is BooleanQuery);
			AreEqual(0, ((BooleanQuery)q).Clauses().Count);
			reader.Close();
			indexStore.Close();
        }

        /// <summary>
        /// Tests if a WildcardQuery that has only a trailing * in the term is
        /// rewritten to a single PrefixQuery.  The boost and rewriteMethod should be
        /// preserved.
        /// </summary>
        [Test]
        public void TestPrefixTerm()
        {
            RAMDirectory indexStore = GetIndexStore("field", new String[] { "prefix", "prefixx" });
			IndexReader reader = DirectoryReader.Open(indexStore);
			IndexSearcher searcher = NewSearcher(reader);

            MultiTermQuery wq = new WildcardQuery(new Term("field", "prefix*"));
            AssertMatches(searcher, wq, 2);

			Terms terms = MultiFields.GetTerms(searcher.GetIndexReader(), "field");
			IsTrue(wq.GetTermsEnum(terms) is PrefixTermsEnum);
			wq = new WildcardQuery(new Term("field", "*"));
			AssertMatches(searcher, wq, 2);
			IsFalse(wq.GetTermsEnum(terms) is PrefixTermsEnum);
			IsFalse(wq.GetTermsEnum(terms).GetType().Name.Contains("AutomatonTermsEnum"
				));
			reader.Close();
			indexStore.Close();
        }

	    /// <summary> Tests Wildcard queries with an asterisk.</summary>
		[Test]
		public virtual void  TestAsterisk()
		{
			RAMDirectory indexStore = GetIndexStore("body", new System.String[]{"metal", "metals"});
			IndexReader reader = DirectoryReader.Open(indexStore);
			IndexSearcher searcher = NewSearcher(reader);
			Query query1 = new TermQuery(new Term("body", "metal"));
			Query query2 = new WildcardQuery(new Term("body", "metal*"));
			Query query3 = new WildcardQuery(new Term("body", "m*tal"));
			Query query4 = new WildcardQuery(new Term("body", "m*tal*"));
			Query query5 = new WildcardQuery(new Term("body", "m*tals"));
			
			BooleanQuery query6 = new BooleanQuery();
			query6.Add(query5, Occur.SHOULD);
			
			BooleanQuery query7 = new BooleanQuery();
			query7.Add(query3, Occur.SHOULD);
			query7.Add(query5, Occur.SHOULD);
			
			// Queries do not automatically lower-case search terms:
			Query query8 = new WildcardQuery(new Term("body", "M*tal*"));
			
			AssertMatches(searcher, query1, 1);
			AssertMatches(searcher, query2, 2);
			AssertMatches(searcher, query3, 1);
			AssertMatches(searcher, query4, 2);
			AssertMatches(searcher, query5, 1);
			AssertMatches(searcher, query6, 1);
			AssertMatches(searcher, query7, 2);
			AssertMatches(searcher, query8, 0);
			AssertMatches(searcher, new WildcardQuery(new Term("body", "*tall")), 0);
			AssertMatches(searcher, new WildcardQuery(new Term("body", "*tal")), 1);
			AssertMatches(searcher, new WildcardQuery(new Term("body", "*tal*")), 2);
			reader.Close();
			indexStore.Close();
		}
		

        /*
         * LUCENE-2620
         */
        [Test]
        public void TestLotsOfAsterisks()
        {
            RAMDirectory indexStore = GetIndexStore("body", new String[] { "metal", "metals" });
            IndexSearcher searcher = new IndexSearcher(indexStore, true);
            System.Text.StringBuilder term = new System.Text.StringBuilder();
            term.Append("m");
            for (int i = 0; i < 512; i++)
                term.Append("*");
            term.Append("tal");
            Query query3 = new WildcardQuery(new Term("body", term.ToString()));

            AssertMatches(searcher, query3, 1);
            searcher.Close();
            indexStore.Close();
        }

		/// <summary> Tests Wildcard queries with a question mark.
		/// 
		/// </summary>
		/// <throws>  IOException if an error occurs </throws>
		[Test]
		public virtual void  TestQuestionmark()
		{
			RAMDirectory indexStore = GetIndexStore("body", new System.String[]{"metal", "metals", "mXtals", "mXtXls"});
			IndexReader reader = DirectoryReader.Open(indexStore);
			IndexSearcher searcher = NewSearcher(reader);
			Query query1 = new WildcardQuery(new Term("body", "m?tal"));
			Query query2 = new WildcardQuery(new Term("body", "metal?"));
			Query query3 = new WildcardQuery(new Term("body", "metals?"));
			Query query4 = new WildcardQuery(new Term("body", "m?t?ls"));
			Query query5 = new WildcardQuery(new Term("body", "M?t?ls"));
			Query query6 = new WildcardQuery(new Term("body", "meta??"));
			
			AssertMatches(searcher, query1, 1);
			AssertMatches(searcher, query2, 1);
			AssertMatches(searcher, query3, 0);
			AssertMatches(searcher, query4, 3);
			AssertMatches(searcher, query5, 0);
			AssertMatches(searcher, query6, 1); // Query: 'meta??' matches 'metals' not 'metal'
			reader.Close();
			indexStore.Close();
		}
		
		public virtual void TestEscapes()
		{
			Directory indexStore = GetIndexStore("field", new string[] { "foo*bar", "foo??bar"
				, "fooCDbar", "fooSOMETHINGbar", "foo\\" });
			IndexReader reader = DirectoryReader.Open(indexStore);
			IndexSearcher searcher = NewSearcher(reader);
			// without escape: matches foo??bar, fooCDbar, foo*bar, and fooSOMETHINGbar
			WildcardQuery unescaped = new WildcardQuery(new Term("field", "foo*bar"));
			AssertMatches(searcher, unescaped, 4);
			// with escape: only matches foo*bar
			WildcardQuery escaped = new WildcardQuery(new Term("field", "foo\\*bar"));
			AssertMatches(searcher, escaped, 1);
			// without escape: matches foo??bar and fooCDbar
			unescaped = new WildcardQuery(new Term("field", "foo??bar"));
			AssertMatches(searcher, unescaped, 2);
			// with escape: matches foo??bar only
			escaped = new WildcardQuery(new Term("field", "foo\\?\\?bar"));
			AssertMatches(searcher, escaped, 1);
			// check escaping at end: lenient parse yields "foo\"
			WildcardQuery atEnd = new WildcardQuery(new Term("field", "foo\\"));
			AssertMatches(searcher, atEnd, 1);
			reader.Close();
			indexStore.Close();
		}
		private RAMDirectory GetIndexStore(System.String field, System.String[] contents)
		{
			Directory indexStore = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), indexStore);
			for (int i = 0; i < contents.Length; ++i)
			{
				Document doc = new Document();
				doc.Add(NewTextField(field, contents[i], Field.Store.YES));
				writer.AddDocument(doc);
			}
			writer.Close();
			
			return indexStore;
		}
		
		private void  AssertMatches(IndexSearcher searcher, Query q, int expectedMatches)
		{
			ScoreDoc[] result = searcher.Search(q, null, 1000).ScoreDocs;
			Assert.AreEqual(expectedMatches, result.Length);
		}
		
		/// <summary> Test that wild card queries are parsed to the correct type and are searched correctly.
		/// This test looks at both parsing and execution of wildcard queries.
		/// Although placed here, it also tests prefix queries, verifying that
		/// prefix queries are not parsed into wild card queries, and viceversa.
		/// </summary>
		/// <throws>  Exception </throws>
		[Test]
		public virtual void  TestParsingAndSearching()
		{
			System.String field = "content";
			System.String[] docs = new System.String[]{"\\ abcdefg1", "\\79 hijklmn1", "\\\\ opqrstu1"};
			// queries that should find all docs
			Query[] matchAll = new Query[] { new WildcardQuery(new Term(field, "*")), new WildcardQuery
				(new Term(field, "*1")), new WildcardQuery(new Term(field, "**1")), new WildcardQuery
				(new Term(field, "*?")), new WildcardQuery(new Term(field, "*?1")), new WildcardQuery
				(new Term(field, "?*1")), new WildcardQuery(new Term(field, "**")), new WildcardQuery
				(new Term(field, "***")), new WildcardQuery(new Term(field, "\\\\*")) };
			// queries that should find no docs
			Query[] matchNone = new Query[] { new WildcardQuery(new Term(field, "a*h")), new 
				WildcardQuery(new Term(field, "a?h")), new WildcardQuery(new Term(field, "*a*h")
				), new WildcardQuery(new Term(field, "?a")), new WildcardQuery(new Term(field, "a?"
				)) };
			PrefixQuery[][] matchOneDocPrefix = new PrefixQuery[][] { new PrefixQuery[] { new 
				PrefixQuery(new Term(field, "a")), new PrefixQuery(new Term(field, "ab")), new PrefixQuery
				(new Term(field, "abc")) }, new PrefixQuery[] { new PrefixQuery(new Term(field, 
				"h")), new PrefixQuery(new Term(field, "hi")), new PrefixQuery(new Term(field, "hij"
				)), new PrefixQuery(new Term(field, "\\7")) }, new PrefixQuery[] { new PrefixQuery
				(new Term(field, "o")), new PrefixQuery(new Term(field, "op")), new PrefixQuery(
				new Term(field, "opq")), new PrefixQuery(new Term(field, "\\\\")) } };
			// these should find only doc 0
			// these should find only doc 1
			// these should find only doc 2
			WildcardQuery[][] matchOneDocWild = new WildcardQuery[][] { new WildcardQuery[] { 
				new WildcardQuery(new Term(field, "*a*")), new WildcardQuery(new Term(field, "*ab*"
				)), new WildcardQuery(new Term(field, "*abc**")), new WildcardQuery(new Term(field
				, "ab*e*")), new WildcardQuery(new Term(field, "*g?")), new WildcardQuery(new Term
				(field, "*f?1")) }, new WildcardQuery[] { new WildcardQuery(new Term(field, "*h*"
				)), new WildcardQuery(new Term(field, "*hi*")), new WildcardQuery(new Term(field
				, "*hij**")), new WildcardQuery(new Term(field, "hi*k*")), new WildcardQuery(new 
				Term(field, "*n?")), new WildcardQuery(new Term(field, "*m?1")), new WildcardQuery
				(new Term(field, "hij**")) }, new WildcardQuery[] { new WildcardQuery(new Term(field
				, "*o*")), new WildcardQuery(new Term(field, "*op*")), new WildcardQuery(new Term
				(field, "*opq**")), new WildcardQuery(new Term(field, "op*q*")), new WildcardQuery
				(new Term(field, "*u?")), new WildcardQuery(new Term(field, "*t?1")), new WildcardQuery
				(new Term(field, "opq**")) } };
			// these should find only doc 0
			// these should find only doc 1
			// these should find only doc 2
			// prepare the index
			Directory dir = NewDirectory();
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, NewIndexWriterConfig(
				TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy
				()));
			for (int i = 0; i < docs.Length; i++)
			{
				Document doc = new Document();
				doc.Add(NewTextField(field, docs[i], Field.Store.NO));
				iw.AddDocument(doc);
			}
			iw.Close();
			IndexReader reader = DirectoryReader.Open(dir);
			IndexSearcher searcher = NewSearcher(reader);
			
			// test queries that must find all
			foreach (Query q in matchAll)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("matchAll: q=" + q + " " + q.GetType().FullName);
				}
				ScoreDoc[] hits = searcher.Search(q, null, 1000).scoreDocs;
				AreEqual(docs.Length, hits.Length);
			}
			
			// test queries that must find none
			foreach (Query q_1 in matchNone)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("matchNone: q=" + q_1 + " " + q_1.GetType().FullName
						);
				}
				ScoreDoc[] hits = searcher.Search(q_1, null, 1000).scoreDocs;
				AreEqual(0, hits.Length);
			}
			
			// test queries that must be prefix queries and must find only one doc
			for (int i = 0; i < matchOneDocPrefix.Length; i++)
			{
				for (int j = 0; j < matchOneDocPrefix[i].Length; j++)
				{
					Query q_2 = matchOneDocPrefix[i_1][j];
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("match 1 prefix: doc=" + docs[i_1] + " q=" + q_2 + " "
							 + q_2.GetType().FullName);
					}
					ScoreDoc[] hits = searcher.Search(q_2, null, 1000).scoreDocs;
					AreEqual(1, hits.Length);
					AreEqual(i_1, hits[0].doc);
				}
			}
			
			// test queries that must be wildcard queries and must find only one doc
			for (int i_2 = 0; i_2 < matchOneDocWild.Length; i_2++)
			{
				for (int j = 0; j < matchOneDocWild[i_2].Length; j++)
				{
					Query q_2 = matchOneDocWild[i_2][j];
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("match 1 wild: doc=" + docs[i_2] + " q=" + q_2 + " "
							 + q_2.GetType().FullName);
					}
					ScoreDoc[] hits = searcher.Search(q_2, null, 1000).scoreDocs;
					AreEqual(1, hits.Length);
					AreEqual(i_2, hits[0].doc);
				}
			}
			
			reader.Close();
			dir.Close();
		}
	}
}