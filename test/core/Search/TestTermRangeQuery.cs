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
using Lucene.Net.Test.Analysis.TokenAttributes;
using NUnit.Framework;

using Analyzer = Lucene.Net.Analysis.Analyzer;
using TokenStream = Lucene.Net.Analysis.TokenStream;
using Tokenizer = Lucene.Net.Analysis.Tokenizer;
using WhitespaceAnalyzer = Lucene.Net.Analysis.WhitespaceAnalyzer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using Term = Lucene.Net.Index.Term;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

namespace Lucene.Net.Search
{
    [TestFixture]
	public class TestTermRangeQuery:LuceneTestCase
	{
		private int docCount = 0;
		private Directory dir;
		
		[SetUp]
		public override void  SetUp()
		{
			base.SetUp();
			dir = NewDirectory();
		}
		
		public override void TearDown()
		{
			dir.Close();
			base.TearDown();
		}
        [Test]
		public virtual void  TestExclusive()
		{
			Query query = TermRangeQuery.NewStringRange("content", "A", "C", false, false);
			InitializeIndex(new System.String[]{"A", "B", "C", "D"});
			IndexReader reader = DirectoryReader.Open(dir);
			IndexSearcher searcher = NewSearcher(reader);
			ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
			Assert.AreEqual(1, hits.Length, "A,B,C,D, only B in range");
			reader.Close();
			
			InitializeIndex(new System.String[]{"A", "B", "D"});
			reader = DirectoryReader.Open(dir);
			searcher = NewSearcher(reader);
			hits = searcher.Search(query, null, 1000).ScoreDocs;
			Assert.AreEqual(1, hits.Length, "A,B,D, only B in range");
			reader.Close();
			
			AddDoc("C");
			reader = DirectoryReader.Open(dir);
			searcher = NewSearcher(reader);
			hits = searcher.Search(query, null, 1000).ScoreDocs;
			Assert.AreEqual(1, hits.Length, "C added, still only B in range");
			searcher.Close();
		}
		
        [Test]
		public virtual void  TestInclusive()
		{
			Query query = TermRangeQuery.NewStringRange("content", "A", "C", true, true);
			
			InitializeIndex(new System.String[]{"A", "B", "C", "D"});
			IndexReader reader = DirectoryReader.Open(dir);
			IndexSearcher searcher = NewSearcher(reader);
			ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
			Assert.AreEqual(3, hits.Length, "A,B,C,D - A,B,C in range");
			reader.Close();
			
			InitializeIndex(new System.String[]{"A", "B", "D"});
			reader = DirectoryReader.Open(dir);
			searcher = NewSearcher(reader);
			hits = searcher.Search(query, null, 1000).ScoreDocs;
			Assert.AreEqual(2, hits.Length, "A,B,D - A and B in range");
			reader.Close();
			
			AddDoc("C");
			reader = DirectoryReader.Open(dir);
			searcher = NewSearcher(reader);
			hits = searcher.Search(query, null, 1000).ScoreDocs;
			Assert.AreEqual(3, hits.Length, "C added - A, B, C in range");
			reader.Close();
		}
		public virtual void TestAllDocs()
		{
			InitializeIndex(new string[] { "A", "B", "C", "D" });
			IndexReader reader = DirectoryReader.Open(dir);
			IndexSearcher searcher = NewSearcher(reader);
			TermRangeQuery query = new TermRangeQuery("content", null, null, true, true);
			Terms terms = MultiFields.GetTerms(searcher.GetIndexReader(), "content");
			NUnit.Framework.Assert.IsFalse(query.GetTermsEnum(terms) is TermRangeTermsEnum);
			NUnit.Framework.Assert.AreEqual(4, searcher.Search(query, null, 1000).scoreDocs.Length
				);
			query = new TermRangeQuery("content", null, null, false, false);
			NUnit.Framework.Assert.IsFalse(query.GetTermsEnum(terms) is TermRangeTermsEnum);
			NUnit.Framework.Assert.AreEqual(4, searcher.Search(query, null, 1000).scoreDocs.Length
				);
			query = TermRangeQuery.NewStringRange("content", string.Empty, null, true, false);
			NUnit.Framework.Assert.IsFalse(query.GetTermsEnum(terms) is TermRangeTermsEnum);
			NUnit.Framework.Assert.AreEqual(4, searcher.Search(query, null, 1000).scoreDocs.Length
				);
			// and now anothe one
			query = TermRangeQuery.NewStringRange("content", "B", null, true, false);
			NUnit.Framework.Assert.IsTrue(query.GetTermsEnum(terms) is TermRangeTermsEnum);
			NUnit.Framework.Assert.AreEqual(3, searcher.Search(query, null, 1000).scoreDocs.Length
				);
			reader.Close();
		}
		public virtual void TestTopTermsRewrite()
		{
			InitializeIndex(new string[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", 
				"K" });
			IndexReader reader = DirectoryReader.Open(dir);
			IndexSearcher searcher = NewSearcher(reader);
			TermRangeQuery query = TermRangeQuery.NewStringRange("content", "B", "J", true, true
				);
			CheckBooleanTerms(searcher, query, "B", "C", "D", "E", "F", "G", "H", "I", "J");
			int savedClauseCount = BooleanQuery.GetMaxClauseCount();
			try
			{
				BooleanQuery.SetMaxClauseCount(3);
				CheckBooleanTerms(searcher, query, "B", "C", "D");
			}
			finally
			{
				BooleanQuery.SetMaxClauseCount(savedClauseCount);
			}
			reader.Close();
		}
		private void CheckBooleanTerms(IndexSearcher searcher, TermRangeQuery query, params 
			string[] terms)
		{
			query.SetRewriteMethod(new MultiTermQuery.TopTermsScoringBooleanQueryRewrite(50));
			BooleanQuery bq = (BooleanQuery)searcher.Rewrite(query);
			ICollection<string> allowedTerms = AsSet(terms);
			NUnit.Framework.Assert.AreEqual(allowedTerms.Count, bq.Clauses().Count);
			foreach (BooleanClause c in bq.Clauses())
			{
				NUnit.Framework.Assert.IsTrue(c.GetQuery() is TermQuery);
				TermQuery tq = (TermQuery)c.GetQuery();
				string term = tq.GetTerm().Text();
				NUnit.Framework.Assert.IsTrue("invalid term: " + term, allowedTerms.Contains(term
					));
				allowedTerms.Remove(term);
			}
			// remove to fail on double terms
			NUnit.Framework.Assert.AreEqual(0, allowedTerms.Count);
		}
        [Test]
		public virtual void  TestEqualsHashcode()
		{
			Query query = TermRangeQuery.NewStringRange("content", "A", "C", true, true);
			
			query.Boost = 1.0f;
			Query other = TermRangeQuery.NewStringRange("content", "A", "C", true, true);
			other.Boost = 1.0f;
			
			Assert.AreEqual(query, query, "query equals itself is true");
			Assert.AreEqual(query, other, "equivalent queries are equal");
			Assert.AreEqual(query.GetHashCode(), other.GetHashCode(), "hashcode must return same value when equals is true");
			
			other.Boost = 2.0f;
			Assert.IsFalse(query.Equals(other), "Different boost queries are not equal");
			
			other = new TermRangeQuery("notcontent", "A", "C", true, true);
			Assert.IsFalse(query.Equals(other), "Different fields are not equal");
			
			other = new TermRangeQuery("content", "X", "C", true, true);
			Assert.IsFalse(query.Equals(other), "Different lower terms are not equal");
			
			other = new TermRangeQuery("content", "A", "Z", true, true);
			Assert.IsFalse(query.Equals(other), "Different upper terms are not equal");
			
			query = new TermRangeQuery("content", null, "C", true, true);
			other = new TermRangeQuery("content", null, "C", true, true);
			Assert.AreEqual(query, other, "equivalent queries with null lowerterms are equal()");
			Assert.AreEqual(query.GetHashCode(), other.GetHashCode(), "hashcode must return same value when equals is true");
			
			query = TermRangeQuery.NewStringRange("content", "C", null, true, true);
			other = TermRangeQuery.NewStringRange("content", "C", null, true, true);
			Assert.AreEqual(query, other, "equivalent queries with null upperterms are equal()");
			Assert.AreEqual(query.GetHashCode(), other.GetHashCode(), "hashcode returns same value");
			
			query = TermRangeQuery.NewStringRange("content", null, "C", true, true);
			other = TermRangeQuery.NewStringRange("content", "C", null, true, true);
			Assert.IsFalse(query.Equals(other), "queries with different upper and lower terms are not equal");
			
			query = new TermRangeQuery("content", "A", "C", false, false);
			other = new TermRangeQuery("content", "A", "C", true, true);
			Assert.IsFalse(query.Equals(other), "queries with different inclusive are not equal");
			
		}
		
        [Test]
		public virtual void  TestExclusiveCollating()
		{
			Query query = new TermRangeQuery("content", "A", "C", false, false, new System.Globalization.CultureInfo("en").CompareInfo);
			InitializeIndex(new System.String[]{"A", "B", "C", "D"});
			IndexSearcher searcher = new IndexSearcher(dir, true);
			ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
			Assert.AreEqual(1, hits.Length, "A,B,C,D, only B in range");
			searcher.Close();
			
			InitializeIndex(new System.String[]{"A", "B", "D"});
            searcher = new IndexSearcher(dir, true);
			hits = searcher.Search(query, null, 1000).ScoreDocs;
			Assert.AreEqual(1, hits.Length, "A,B,D, only B in range");
			searcher.Close();
			
			AddDoc("C");
            searcher = new IndexSearcher(dir, true);
			hits = searcher.Search(query, null, 1000).ScoreDocs;
			Assert.AreEqual(1, hits.Length, "C added, still only B in range");
			searcher.Close();
		}
		
        [Test]
		public virtual void  TestInclusiveCollating()
		{
			Query query = new TermRangeQuery("content", "A", "C", true, true, new System.Globalization.CultureInfo("en").CompareInfo);
			
			InitializeIndex(new System.String[]{"A", "B", "C", "D"});
            IndexSearcher searcher = new IndexSearcher(dir, true);
			ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
			Assert.AreEqual(3, hits.Length, "A,B,C,D - A,B,C in range");
			searcher.Close();
			
			InitializeIndex(new System.String[]{"A", "B", "D"});
            searcher = new IndexSearcher(dir, true);
			hits = searcher.Search(query, null, 1000).ScoreDocs;
			Assert.AreEqual(2, hits.Length, "A,B,D - A and B in range");
			searcher.Close();
			
			AddDoc("C");
            searcher = new IndexSearcher(dir, true);
			hits = searcher.Search(query, null, 1000).ScoreDocs;
			Assert.AreEqual(3, hits.Length, "C added - A, B, C in range");
			searcher.Close();
		}
		
        [Test]
		public virtual void  TestFarsi()
		{
			// Neither Java 1.4.2 nor 1.5.0 has Farsi Locale collation available in
			// RuleBasedCollator.  However, the Arabic Locale seems to order the Farsi
			// characters properly.
			System.Globalization.CompareInfo collator = new System.Globalization.CultureInfo("ar").CompareInfo;
			Query query = new TermRangeQuery("content", "\u062F", "\u0698", true, true, collator);
			// Unicode order would include U+0633 in [ U+062F - U+0698 ], but Farsi
			// orders the U+0698 character before the U+0633 character, so the single
			// index Term below should NOT be returned by a TermRangeQuery with a Farsi
			// Collator (or an Arabic one for the case when Farsi is not supported).
			InitializeIndex(new System.String[]{"\u0633\u0627\u0628"});
            IndexSearcher searcher = new IndexSearcher(dir, true);
			ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
			Assert.AreEqual(0, hits.Length, "The index Term should not be included.");
			
			query = new TermRangeQuery("content", "\u0633", "\u0638", true, true, collator);
			hits = searcher.Search(query, null, 1000).ScoreDocs;
			Assert.AreEqual(1, hits.Length, "The index Term should be included.");
			searcher.Close();
		}
		
        [Test]
		public virtual void  TestDanish()
		{
			System.Globalization.CompareInfo collator = new System.Globalization.CultureInfo("da" + "-" + "dk").CompareInfo;
			// Danish collation orders the words below in the given order (example taken
			// from TestSort.testInternationalSort() ).
			System.String[] words = new System.String[]{"H\u00D8T", "H\u00C5T", "MAND"};
			Query query = new TermRangeQuery("content", "H\u00D8T", "MAND", false, false, collator);
			
			// Unicode order would not include "H\u00C5T" in [ "H\u00D8T", "MAND" ],
			// but Danish collation does.
			InitializeIndex(words);
            IndexSearcher searcher = new IndexSearcher(dir, true);
			ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
			Assert.AreEqual(1, hits.Length, "The index Term should be included.");
			
			query = new TermRangeQuery("content", "H\u00C5T", "MAND", false, false, collator);
			hits = searcher.Search(query, null, 1000).ScoreDocs;
			Assert.AreEqual(0, hits.Length, "The index Term should not be included.");
			searcher.Close();
		}
		
		private class SingleCharAnalyzer:Analyzer
		{
			
			private class SingleCharTokenizer:Tokenizer
			{
				internal char[] buffer = new char[1];
				internal bool done;
				internal CharTermAttribute termAtt;
				
				public SingleCharTokenizer(System.IO.TextReader r):base(r)
				{
					termAtt = AddAttribute<CharTermAttribute>();
				}
				
				public override bool IncrementToken()
				{
					int count = input.Read(buffer, 0, buffer.Length);
					if (done)
						return false;
					else
					{
                        ClearAttributes();
						done = true;
						if (count == 1)
						{
							termAtt.CopyBuffer(buffer, 0, 1);
						}
						return true;
					}
				}
				
				public override void  Reset(System.IO.TextReader reader)
				{
					base.Reset(reader);
					done = false;
				}
			}
			
			protected override Analyzer.TokenStreamComponents CreateComponents(string fieldName
				, StreamReader reader)
			{
				return new Analyzer.TokenStreamComponents(new TestTermRangeQuery.SingleCharAnalyzer.SingleCharTokenizer
					(reader));
			}
		}
		
		private void  InitializeIndex(System.String[] values)
		{
			InitializeIndex(values, new MockAnalyzer(Random(), MockTokenizer.WHITESPACE, false
				));
		}
		
		private void  InitializeIndex(System.String[] values, Analyzer analyzer)
		{
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, analyzer).SetOpenMode(IndexWriterConfig.OpenMode.CREATE));
			for (int i = 0; i < values.Length; i++)
			{
				InsertDoc(writer, values[i]);
			}
			writer.Close();
		}
		
		private void  AddDoc(System.String content)
		{
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random(), MockTokenizer.WHITESPACE, false)).SetOpenMode(IndexWriterConfig.OpenMode
				.APPEND));
			InsertDoc(writer, content);
			writer.Close();
		}
		
		private void  InsertDoc(IndexWriter writer, System.String content)
		{
			Document doc = new Document();
			
			doc.Add(NewStringField("id", "id" + docCount, Field.Store.YES));
			doc.Add(NewTextField("content", content, Field.Store.NO));
			
			writer.AddDocument(doc);
			docCount++;
		}
		
		// LUCENE-38
        [Test]
		public virtual void  TestExclusiveLowerNull()
		{
			Analyzer analyzer = new SingleCharAnalyzer();
			//http://issues.apache.org/jira/browse/LUCENE-38
			Query query = TermRangeQuery.NewStringRange("content", null, "C", false, false);
			InitializeIndex(new System.String[]{"A", "B", "", "C", "D"}, analyzer);
			IndexReader reader = DirectoryReader.Open(dir);
			IndexSearcher searcher = NewSearcher(reader);
			int numHits = searcher.Search(query, null, 1000).TotalHits;
			// When Lucene-38 is fixed, use the assert on the next line:
            Assert.AreEqual(3, numHits, "A,B,<empty string>,C,D => A, B & <empty string> are in range");
			// until Lucene-38 is fixed, use this assert:
            //Assert.AreEqual(2, hits.length(),"A,B,<empty string>,C,D => A, B & <empty string> are in range");
			
			reader.Close();
			InitializeIndex(new System.String[]{"A", "B", "", "D"}, analyzer);
			reader = DirectoryReader.Open(dir);
			searcher = NewSearcher(reader);
            numHits = searcher.Search(query, null, 1000).TotalHits;
			// When Lucene-38 is fixed, use the assert on the next line:
            Assert.AreEqual(3, numHits, "A,B,<empty string>,D => A, B & <empty string> are in range");
			// until Lucene-38 is fixed, use this assert:
            //Assert.AreEqual(2, hits.length(), "A,B,<empty string>,D => A, B & <empty string> are in range");
			reader.Close();
			AddDoc("C");
			reader = DirectoryReader.Open(dir);
			searcher = NewSearcher(reader);
            numHits = searcher.Search(query, null, 1000).TotalHits;
			// When Lucene-38 is fixed, use the assert on the next line:
            Assert.AreEqual(3, numHits, "C added, still A, B & <empty string> are in range");
			// until Lucene-38 is fixed, use this assert
            //Assert.AreEqual(2, hits.length(), "C added, still A, B & <empty string> are in range");
			reader.Close();
		}
		
		// LUCENE-38
        [Test]
		public virtual void  TestInclusiveLowerNull()
		{
			//http://issues.apache.org/jira/browse/LUCENE-38
			Analyzer analyzer = new SingleCharAnalyzer();
			Query query = TermRangeQuery.NewStringRange("content", null, "C", true, true);
			InitializeIndex(new System.String[]{"A", "B", "", "C", "D"}, analyzer);
			IndexReader reader = DirectoryReader.Open(dir);
			IndexSearcher searcher = NewSearcher(reader);
            int numHits = searcher.Search(query, null, 1000).TotalHits;
			// When Lucene-38 is fixed, use the assert on the next line:
            Assert.AreEqual(4, numHits, "A,B,<empty string>,C,D => A,B,<empty string>,C in range");
			// until Lucene-38 is fixed, use this assert
            //Assert.AreEqual(3, hits.length(), "A,B,<empty string>,C,D => A,B,<empty string>,C in range");
			reader.Close();
			InitializeIndex(new System.String[]{"A", "B", "", "D"}, analyzer);
			reader = DirectoryReader.Open(dir);
			searcher = NewSearcher(reader);
            numHits = searcher.Search(query, null, 1000).TotalHits;
			// When Lucene-38 is fixed, use the assert on the next line:
            Assert.AreEqual(3, numHits, "A,B,<empty string>,D - A, B and <empty string> in range");
			// until Lucene-38 is fixed, use this assert
            //Assert.AreEqual(2, hits.length(), "A,B,<empty string>,D => A, B and <empty string> in range");
			reader.Close();
			AddDoc("C");
			reader = DirectoryReader.Open(dir);
			searcher = NewSearcher(reader);
            numHits = searcher.Search(query, null, 1000).TotalHits;
			// When Lucene-38 is fixed, use the assert on the next line:
            Assert.AreEqual(4, numHits, "C added => A,B,<empty string>,C in range");
			// until Lucene-38 is fixed, use this assert
            //Assert.AreEqual(3, hits.length(), "C added => A,B,<empty string>,C in range");
			reader.Close();
		}
	}
}