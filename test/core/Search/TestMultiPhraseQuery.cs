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
using System.Collections.Generic;
using NUnit.Framework;

using SimpleAnalyzer = Lucene.Net.Test.Analysis.SimpleAnalyzer;
using StandardAnalyzer = Lucene.Net.Test.Analysis.Standard.StandardAnalyzer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using IndexReader = Lucene.Net.Index.IndexReader;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using Term = Lucene.Net.Index.Term;
using TermEnum = Lucene.Net.Index.TermEnum;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

namespace Lucene.Net.Search
{
	
	/// <summary>This class tests the MultiPhraseQuery class.</summary>
    [TestFixture]
	public class TestMultiPhraseQuery:LuceneTestCase
	{
		
		[Test]
		public virtual void  TestPhrasePrefix()
		{
			Directory indexStore = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), indexStore);
			Add("blueberry pie", writer);
			Add("blueberry strudel", writer);
			Add("blueberry pizza", writer);
			Add("blueberry chewing gum", writer);
			Add("bluebird pizza", writer);
			Add("bluebird foobar pizza", writer);
			Add("piccadilly circus", writer);
			IndexReader reader = writer.Reader;
			IndexSearcher searcher = NewSearcher(reader);
			
			// search for "blueberry pi*":
			MultiPhraseQuery query1 = new MultiPhraseQuery();
			// search for "strawberry pi*":
			MultiPhraseQuery query2 = new MultiPhraseQuery();
			query1.Add(new Term("body", "blueberry"));
			query2.Add(new Term("body", "strawberry"));
			List<Term> termsWithPrefix = new List<Term>();
			
			// this TermEnum gives "piccadilly", "pie" and "pizza".
			System.String prefix = "pi";
			TermsEnum te = MultiFields.GetFields(reader).Terms("body").Iterator(null);
			te.SeekCeil(new BytesRef(prefix));
			do 
			{
				string s = te.Term().Utf8ToString();
				if (s.StartsWith(prefix))
				{
					termsWithPrefix.Add(new Term("body", s));
				}
				else
				{
					break;
				}
			}
			while (te.Next() != null);
			
			query1.Add((Term[]) termsWithPrefix.ToArray(typeof(Term)));
			Assert.AreEqual("body:\"blueberry (piccadilly pie pizza)\"", query1.ToString());
			query2.Add((Term[]) termsWithPrefix.ToArray(typeof(Term)));
			Assert.AreEqual("body:\"strawberry (piccadilly pie pizza)\"", query2.ToString());
			
			ScoreDoc[] result;
			result = searcher.Search(query1, null, 1000).ScoreDocs;
			Assert.AreEqual(2, result.Length);
			result = searcher.Search(query2, null, 1000).ScoreDocs;
			Assert.AreEqual(0, result.Length);
			
			// search for "blue* pizza":
			MultiPhraseQuery query3 = new MultiPhraseQuery();
			termsWithPrefix.Clear();
			prefix = "blue";
			te.SeekCeil(new BytesRef(prefix));
			do 
			{
				if (te.Term().Utf8ToString().StartsWith(prefix))
				{
					termsWithPrefix.Add(new Term("body", te.Term().Utf8ToString());
				}
			}
			while (te.Next() != null);
			query3.Add((Term[]) termsWithPrefix.ToArray(typeof(Term)));
			query3.Add(new Term("body", "pizza"));
			
			result = searcher.Search(query3, null, 1000).ScoreDocs;
			Assert.AreEqual(2, result.Length); // blueberry pizza, bluebird pizza
			Assert.AreEqual("body:\"(blueberry bluebird) pizza\"", query3.ToString());
			
			// test slop:
			query3.Slop = 1;
			result = searcher.Search(query3, null, 1000).ScoreDocs;
			searcher.Explain(query3, 0);
			Assert.AreEqual(3, result.Length); // blueberry pizza, bluebird pizza, bluebird foobar pizza
			
			MultiPhraseQuery query4 = new MultiPhraseQuery();

            // okay, all terms must belong to the same field
		    Assert.Throws<ArgumentException>(() =>
		                                         {
		                                             query4.Add(new Term("field1", "foo"));
		                                             query4.Add(new Term("field2", "foobar"));
		                                         });
			
			searcher.Dispose();
			indexStore.Dispose();
		}
		
		public virtual void TestTall()
		{
			Directory indexStore = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), indexStore);
			Add("blueberry chocolate pie", writer);
			Add("blueberry chocolate tart", writer);
			IndexReader r = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(r);
			MultiPhraseQuery q = new MultiPhraseQuery();
			q.Add(new Term("body", "blueberry"));
			q.Add(new Term("body", "chocolate"));
			q.Add(new Term[] { new Term("body", "pie"), new Term("body", "tart") });
			AreEqual(2, searcher.Search(q, 1).TotalHits);
			r.Dispose();
			indexStore.Dispose();
		}
		[Ignore]
		public virtual void TestMultiSloppyWithRepeats()
		{
			//LUCENE-3821 fixes sloppy phrase scoring, except for this known problem 
			Directory indexStore = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), indexStore);
			Add("a b c d e f g h i k", writer);
			IndexReader r = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(r);
			MultiPhraseQuery q = new MultiPhraseQuery();
			// this will fail, when the scorer would propagate [a] rather than [a,b],
			q.Add(new Term[] { new Term("body", "a"), new Term("body", "b") });
			q.Add(new Term[] { new Term("body", "a") });
			q.SetSlop(6);
			AreEqual(1, searcher.Search(q, 1).TotalHits);
			// should match on "a b"
			r.Dispose();
			indexStore.Dispose();
		}
		public virtual void TestMultiExactWithRepeats()
		{
			Directory indexStore = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), indexStore);
			Add("a b c d e f g h i k", writer);
			IndexReader r = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(r);
			MultiPhraseQuery q = new MultiPhraseQuery();
			q.Add(new Term[] { new Term("body", "a"), new Term("body", "d") }, 0);
			q.Add(new Term[] { new Term("body", "a"), new Term("body", "f") }, 2);
			AreEqual(1, searcher.Search(q, 1).TotalHits);
			// should match on "a b"
			r.Dispose();
			indexStore.Dispose();
		}
		private void Add(string s, RandomIndexWriter writer)
		{
			Document doc = new Document();
			doc.Add(NewTextField("body", s, Field.Store.YES));
			writer.AddDocument(doc);
		}
		
		[Test]
		public virtual void  TestBooleanQueryContainingSingleTermPrefixQuery()
		{
			// this tests against bug 33161 (now fixed)
			// In order to cause the bug, the outer query must have more than one term 
			// and all terms required.
			// The contained PhraseMultiQuery must contain exactly one term array.
			
			Directory indexStore = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), indexStore);
			Add("blueberry pie", writer);
			Add("blueberry chewing gum", writer);
			Add("blue raspberry pie", writer);
			IndexReader reader = writer.Reader;
			IndexSearcher searcher = NewSearcher(reader);
			// This query will be equivalent to +body:pie +body:"blue*"
			BooleanQuery q = new BooleanQuery();
			q.Add(new TermQuery(new Term("body", "pie")), Occur.MUST);
			
			MultiPhraseQuery trouble = new MultiPhraseQuery();
			trouble.Add(new Term[]{new Term("body", "blueberry"), new Term("body", "blue")});
			q.Add(trouble, Occur.MUST);
			
			// exception will be thrown here without fix
			ScoreDoc[] hits = searcher.Search(q, null, 1000).ScoreDocs;
			
			Assert.AreEqual(2, hits.Length, "Wrong number of hits");
			searcher.Explain(q, 0);
			writer.Dispose();
			reader.Dispose();
			indexStore.Dispose();
		}
		
		[Test]
		public virtual void  TestPhrasePrefixWithBooleanQuery()
		{
			Directory indexStore = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), indexStore);
			Add("This is a test", "object", writer);
			Add("a note", "note", writer);
			IndexReader reader = writer.Reader;

			IndexSearcher searcher = NewSearcher(reader);
			
			// This query will be equivalent to +type:note +body:"a t*"
			BooleanQuery q = new BooleanQuery();
			q.Add(new TermQuery(new Term("type", "note")), Occur.MUST);
			
			MultiPhraseQuery trouble = new MultiPhraseQuery();
			trouble.Add(new Term("body", "a"));
			trouble.Add(new Term[]{new Term("body", "test"), new Term("body", "this")});
			q.Add(trouble, Occur.MUST);
			
			// exception will be thrown here without fix for #35626:
			ScoreDoc[] hits = searcher.Search(q, null, 1000).ScoreDocs;
			Assert.AreEqual(0, hits.Length, "Wrong number of hits");
			writer.Dispose();
			reader.Dispose();
			indexStore.Dispose();
		}
		
		public virtual void TestNoDocs()
		{
			Directory indexStore = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), indexStore);
			Add("a note", "note", writer);
			IndexReader reader = writer.Reader;
			IndexSearcher searcher = NewSearcher(reader);
			MultiPhraseQuery q = new MultiPhraseQuery();
			q.Add(new Term("body", "a"));
			q.Add(new Term[] { new Term("body", "nope"), new Term("body", "nope") });
			AreEqual("Wrong number of hits", 0, searcher.Search(q, null
				, 1).TotalHits);
			// just make sure no exc:
			searcher.Explain(q, 0);
			writer.Dispose();
			reader.Dispose();
			indexStore.Dispose();
		}
		[Test]
		public virtual void  TestHashCodeAndEquals()
		{
			MultiPhraseQuery query1 = new MultiPhraseQuery();
			MultiPhraseQuery query2 = new MultiPhraseQuery();
			
			Assert.AreEqual(query1.GetHashCode(), query2.GetHashCode());
			Assert.AreEqual(query1, query2);
			
			Term term1 = new Term("someField", "someText");
			
			query1.Add(term1);
			query2.Add(term1);
			
			Assert.AreEqual(query1.GetHashCode(), query2.GetHashCode());
			Assert.AreEqual(query1, query2);
			
			Term term2 = new Term("someField", "someMoreText");
			
			query1.Add(term2);
			
			Assert.IsFalse(query1.GetHashCode() == query2.GetHashCode());
			Assert.IsFalse(query1.Equals(query2));
			
			query2.Add(term2);
			
			Assert.AreEqual(query1.GetHashCode(), query2.GetHashCode());
			Assert.AreEqual(query1, query2);
		}
		
		
		private void Add(string s, string type, RandomIndexWriter writer)
		{
			Document doc = new Document();
			doc.Add(NewTextField("body", s, Field.Store.YES));
			doc.Add(NewStringField("type", type, Field.Store.NO));
			writer.AddDocument(doc);
		}
		public virtual void TestEmptyToString()
		{
			new MultiPhraseQuery().ToString();
		}
		public virtual void TestCustomIDF()
		{
			Directory indexStore = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), indexStore);
			Add("This is a test", "object", writer);
			Add("a note", "note", writer);
			IndexReader reader = writer.Reader;
			IndexSearcher searcher = NewSearcher(reader);
			searcher.SetSimilarity(new _DefaultSimilarity_339());
			MultiPhraseQuery query = new MultiPhraseQuery();
			query.Add(new Term[] { new Term("body", "this"), new Term("body", "that") });
			query.Add(new Term("body", "is"));
			Weight weight = query.CreateWeight(searcher);
			AreEqual(10f * 10f, weight.GetValueForNormalization(), 0.001f
				);
			writer.Dispose();
			reader.Dispose();
			indexStore.Dispose();
		}
		private sealed class _DefaultSimilarity_339 : DefaultSimilarity
		{
			public _DefaultSimilarity_339()
			{
			}

			public override Explanation IdfExplain(CollectionStatistics collectionStats, TermStatistics
				[] termStats)
			{
				return new Explanation(10f, "just a test");
			}
		}
		public virtual void TestZeroPosIncr()
		{
			Directory dir = new RAMDirectory();
			Token[] tokens = new Token[3];
			tokens[0] = new Token();
			tokens[0].Append("a");
			tokens[0].PositionIncrement = (1);
			tokens[1] = new Token();
			tokens[1].Append("b");
			tokens[1].PositionIncrement = (0);
			tokens[2] = new Token();
			tokens[2].Append("c");
			tokens[2].PositionIncrement = (0);
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new TextField("field", new CannedTokenStream(tokens)));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new TextField("field", new CannedTokenStream(tokens)));
			writer.AddDocument(doc);
			IndexReader r = writer.Reader;
			writer.Dispose();
			IndexSearcher s = NewSearcher(r);
			MultiPhraseQuery mpq = new MultiPhraseQuery();
			//mpq.setSlop(1);
			// NOTE: not great that if we do the else clause here we
			// get different scores!  MultiPhraseQuery counts that
			// phrase as occurring twice per doc (it should be 1, I
			// think?).  This is because MultipleTermPositions is able to
			// return the same position more than once (0, in this
			// case):
			if (true)
			{
				mpq.Add(new Term[] { new Term("field", "b"), new Term("field", "c") }, 0);
				mpq.Add(new Term[] { new Term("field", "a") }, 0);
			}
			else
			{
				mpq.Add(new Term[] { new Term("field", "a") }, 0);
				mpq.Add(new Term[] { new Term("field", "b"), new Term("field", "c") }, 0);
			}
			TopDocs hits = s.Search(mpq, 2);
			AreEqual(2, hits.TotalHits);
			AreEqual(hits.ScoreDocs[0].score, hits.ScoreDocs[1].score, 
				1e-5);
			r.Dispose();
			dir.Dispose();
		}
		private static Token MakeToken(string text, int posIncr)
		{
			Token t = new Token();
			t.Append(text);
			t.PositionIncrement = (posIncr);
			return t;
		}
		private static readonly Token[] INCR_0_DOC_TOKENS = new Token[] { MakeToken("x", 
			1), MakeToken("a", 1), MakeToken("1", 0), MakeToken("m", 1), MakeToken("b", 1), 
			MakeToken("1", 0), MakeToken("n", 1), MakeToken("c", 1), MakeToken("y", 1) };
		private static readonly Token[] INCR_0_QUERY_TOKENS_AND = new Token[] { MakeToken
			("a", 1), MakeToken("1", 0), MakeToken("b", 1), MakeToken("1", 0), MakeToken("c"
			, 1) };
		private static readonly Token[][] INCR_0_QUERY_TOKENS_AND_OR_MATCH = new Token[][]
			 { new Token[] { MakeToken("a", 1) }, new Token[] { MakeToken("x", 1), MakeToken
			("1", 0) }, new Token[] { MakeToken("b", 2) }, new Token[] { MakeToken("x", 2), 
			MakeToken("1", 0) }, new Token[] { MakeToken("c", 3) } };
		private static readonly Token[][] INCR_0_QUERY_TOKENS_AND_OR_NO_MATCHN = new Token
			[][] { new Token[] { MakeToken("x", 1) }, new Token[] { MakeToken("a", 1), MakeToken
			("1", 0) }, new Token[] { MakeToken("x", 2) }, new Token[] { MakeToken("b", 2), 
			MakeToken("1", 0) }, new Token[] { MakeToken("c", 3) } };
		public virtual void TestZeroPosIncrSloppyParsedAnd()
		{
			MultiPhraseQuery q = new MultiPhraseQuery();
			q.Add(new Term[] { new Term("field", "a"), new Term("field", "1") }, -1);
			q.Add(new Term[] { new Term("field", "b"), new Term("field", "1") }, 0);
			q.Add(new Term[] { new Term("field", "c") }, 1);
			DoTestZeroPosIncrSloppy(q, 0);
			q.SetSlop(1);
			DoTestZeroPosIncrSloppy(q, 0);
			q.SetSlop(2);
			DoTestZeroPosIncrSloppy(q, 1);
		}
		private void DoTestZeroPosIncrSloppy(Query q, int nExpected)
		{
			Directory dir = NewDirectory();
			// random dir
			IndexWriterConfig cfg = NewIndexWriterConfig(TEST_VERSION_CURRENT, null);
			IndexWriter writer = new IndexWriter(dir, cfg);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new TextField("field", new CannedTokenStream(INCR_0_DOC_TOKENS)));
			writer.AddDocument(doc);
			IndexReader r = DirectoryReader.Open(writer, false);
			writer.Dispose();
			IndexSearcher s = NewSearcher(r);
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("QUERY=" + q);
			}
			TopDocs hits = s.Search(q, 1);
			AreEqual("wrong number of results", nExpected, hits.TotalHits
				);
			if (VERBOSE)
			{
				for (int hit = 0; hit < hits.TotalHits; hit++)
				{
					ScoreDoc sd = hits.ScoreDocs[hit];
					System.Console.Out.WriteLine("  hit doc=" + sd.Doc + " score=" + sd.score);
				}
			}
			r.Dispose();
			dir.Dispose();
		}

		/// <summary>PQ AND Mode - Manually creating a phrase query</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestZeroPosIncrSloppyPqAnd()
		{
			PhraseQuery pq = new PhraseQuery();
			int pos = -1;
			foreach (Token tap in INCR_0_QUERY_TOKENS_AND)
			{
				pos += tap.GetPositionIncrement();
				pq.Add(new Term("field", tap.ToString()), pos);
			}
			DoTestZeroPosIncrSloppy(pq, 0);
			pq.SetSlop(1);
			DoTestZeroPosIncrSloppy(pq, 0);
			pq.SetSlop(2);
			DoTestZeroPosIncrSloppy(pq, 1);
		}

		/// <summary>MPQ AND Mode - Manually creating a multiple phrase query</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestZeroPosIncrSloppyMpqAnd()
		{
			MultiPhraseQuery mpq = new MultiPhraseQuery();
			int pos = -1;
			foreach (Token tap in INCR_0_QUERY_TOKENS_AND)
			{
				pos += tap.GetPositionIncrement();
				mpq.Add(new Term[] { new Term("field", tap.ToString()) }, pos);
			}
			//AND logic
			DoTestZeroPosIncrSloppy(mpq, 0);
			mpq.SetSlop(1);
			DoTestZeroPosIncrSloppy(mpq, 0);
			mpq.SetSlop(2);
			DoTestZeroPosIncrSloppy(mpq, 1);
		}

		/// <summary>MPQ Combined AND OR Mode - Manually creating a multiple phrase query</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestZeroPosIncrSloppyMpqAndOrMatch()
		{
			MultiPhraseQuery mpq = new MultiPhraseQuery();
			foreach (Token[] tap in INCR_0_QUERY_TOKENS_AND_OR_MATCH)
			{
				Term[] terms = TapTerms(tap);
				int pos = tap[0].GetPositionIncrement() - 1;
				mpq.Add(terms, pos);
			}
			//AND logic in pos, OR across lines 
			DoTestZeroPosIncrSloppy(mpq, 0);
			mpq.SetSlop(1);
			DoTestZeroPosIncrSloppy(mpq, 0);
			mpq.SetSlop(2);
			DoTestZeroPosIncrSloppy(mpq, 1);
		}

		/// <summary>MPQ Combined AND OR Mode - Manually creating a multiple phrase query - with no match
		/// 	</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestZeroPosIncrSloppyMpqAndOrNoMatch()
		{
			MultiPhraseQuery mpq = new MultiPhraseQuery();
			foreach (Token[] tap in INCR_0_QUERY_TOKENS_AND_OR_NO_MATCHN)
			{
				Term[] terms = TapTerms(tap);
				int pos = tap[0].GetPositionIncrement() - 1;
				mpq.Add(terms, pos);
			}
			//AND logic in pos, OR across lines 
			DoTestZeroPosIncrSloppy(mpq, 0);
			mpq.SetSlop(2);
			DoTestZeroPosIncrSloppy(mpq, 0);
		}

		private Term[] TapTerms(Token[] tap)
		{
			Term[] terms = new Term[tap.Length];
			for (int i = 0; i < terms.Length; i++)
			{
				terms[i] = new Term("field", tap[i].ToString());
			}
			return terms;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestNegativeSlop()
		{
			MultiPhraseQuery query = new MultiPhraseQuery();
			query.Add(new Term("field", "two"));
			query.Add(new Term("field", "one"));
			try
			{
				query.SetSlop(-2);
				Fail("didn't get expected exception");
			}
			catch (ArgumentException)
			{
			}
		}
	}
}