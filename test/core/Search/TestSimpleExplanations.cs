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

using StandardAnalyzer = Lucene.Net.Test.Analysis.Standard.StandardAnalyzer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using Term = Lucene.Net.Index.Term;
using QueryParser = Lucene.Net.QueryParsers.QueryParser;
using Directory = Lucene.Net.Store.Directory;
using MockRAMDirectory = Lucene.Net.Store.MockRAMDirectory;
using SpanNearQuery = Lucene.Net.Search.Spans.SpanNearQuery;
using SpanQuery = Lucene.Net.Search.Spans.SpanQuery;
using SpanTermQuery = Lucene.Net.Search.Spans.SpanTermQuery;

namespace Lucene.Net.Search
{
	
	
	/// <summary> TestExplanations subclass focusing on basic query types</summary>
    [TestFixture]
	public class TestSimpleExplanations:TestExplanations
	{
		
		// we focus on queries that don't rewrite to other queries.
		// if we get those covered well, then the ones that rewrite should
		// also be covered.
		
		
		/* simple term tests */
		
		[Test]
		public virtual void  TestT1()
		{
			Qtest(new TermQuery(new Term(FIELD, "w1")), new int[] { 0, 1, 2, 3 });
		}
		[Test]
		public virtual void  TestT2()
		{
			TermQuery termQuery = new TermQuery(new Term(FIELD, "w1"));
			termQuery.SetBoost(100);
			Qtest(termQuery, new int[] { 0, 1, 2, 3 });
		}
		
		/* MatchAllDocs */
		
		[Test]
		public virtual void  TestMA1()
		{
			Qtest(new MatchAllDocsQuery(), new int[]{0, 1, 2, 3});
		}
		[Test]
		public virtual void  TestMA2()
		{
			Query q = new MatchAllDocsQuery();
			q.Boost = 1000;
			Qtest(q, new int[]{0, 1, 2, 3});
		}
		
		/* some simple phrase tests */
		
		[Test]
		public virtual void  TestP1()
		{
			PhraseQuery phraseQuery = new PhraseQuery();
			phraseQuery.Add(new Term(FIELD, "w1"));
			phraseQuery.Add(new Term(FIELD, "w2"));
			Qtest(phraseQuery, new int[] { 0 });
		}
		[Test]
		public virtual void  TestP2()
		{
			PhraseQuery phraseQuery = new PhraseQuery();
			phraseQuery.Add(new Term(FIELD, "w1"));
			phraseQuery.Add(new Term(FIELD, "w3"));
			Qtest(phraseQuery, new int[] { 1, 3 });
		}
		[Test]
		public virtual void  TestP3()
		{
			PhraseQuery phraseQuery = new PhraseQuery();
			phraseQuery.SetSlop(1);
			phraseQuery.Add(new Term(FIELD, "w1"));
			phraseQuery.Add(new Term(FIELD, "w2"));
			Qtest(phraseQuery, new int[] { 0, 1, 2 });
		}
		[Test]
		public virtual void  TestP4()
		{
			PhraseQuery phraseQuery = new PhraseQuery();
			phraseQuery.SetSlop(1);
			phraseQuery.Add(new Term(FIELD, "w2"));
			phraseQuery.Add(new Term(FIELD, "w3"));
			Qtest(phraseQuery, new int[] { 0, 1, 2, 3 });
		}
		[Test]
		public virtual void  TestP5()
		{
			PhraseQuery phraseQuery = new PhraseQuery();
			phraseQuery.SetSlop(1);
			phraseQuery.Add(new Term(FIELD, "w3"));
			phraseQuery.Add(new Term(FIELD, "w2"));
			Qtest(phraseQuery, new int[] { 1, 3 });
		}
		[Test]
		public virtual void  TestP6()
		{
			PhraseQuery phraseQuery = new PhraseQuery();
			phraseQuery.SetSlop(2);
			phraseQuery.Add(new Term(FIELD, "w3"));
			phraseQuery.Add(new Term(FIELD, "w2"));
			Qtest(phraseQuery, new int[] { 0, 1, 3 });
		}
        [Test]
        public virtual void TestP7()
		{
			PhraseQuery phraseQuery = new PhraseQuery();
			phraseQuery.SetSlop(3);
			phraseQuery.Add(new Term(FIELD, "w3"));
			phraseQuery.Add(new Term(FIELD, "w2"));
			Qtest(phraseQuery, new int[] { 0, 1, 2, 3 });
		}
		
		/* some simple filtered query tests */
		
		[Test]
		public virtual void  TestFQ1()
		{
			Qtest(new FilteredQuery(new TermQuery(new Term(FIELD, "w1")), new TestExplanations.ItemizedFilter
				(new int[] { 0, 1, 2, 3 })), new int[] { 0, 1, 2, 3 });
		}
		[Test]
		public virtual void  TestFQ2()
		{
			Qtest(new FilteredQuery(new TermQuery(new Term(FIELD, "w1")), new TestExplanations.ItemizedFilter
				(new int[] { 0, 2, 3 })), new int[] { 0, 2, 3 });
		}
		[Test]
		public virtual void  TestFQ3()
		{
			Qtest(new FilteredQuery(new TermQuery(new Term(FIELD, "xx")), new TestExplanations.ItemizedFilter
				(new int[] { 1, 3 })), new int[] { 3 });
		}
		[Test]
		public virtual void  TestFQ4()
		{
			TermQuery termQuery = new TermQuery(new Term(FIELD, "xx"));
			termQuery.SetBoost(1000);
			Qtest(new FilteredQuery(termQuery, new TestExplanations.ItemizedFilter(new int[] 
				{ 1, 3 })), new int[] { 3 });
		}
		[Test]
		public virtual void  TestFQ6()
		{
			Query q = new FilteredQuery(new TermQuery(new Term(FIELD, "xx")), new TestExplanations.ItemizedFilter
				(new int[] { 1, 3 }));
			q.Boost = 1000;
			Qtest(q, new int[]{3});
		}
		
		/* ConstantScoreQueries */
		
		[Test]
		public virtual void  TestCSQ1()
		{
			Query q = new ConstantScoreQuery(new ItemizedFilter(new int[]{0, 1, 2, 3}));
			Qtest(q, new int[]{0, 1, 2, 3});
		}
		[Test]
		public virtual void  TestCSQ2()
		{
			Query q = new ConstantScoreQuery(new ItemizedFilter(new int[]{1, 3}));
			Qtest(q, new int[]{1, 3});
		}
		[Test]
		public virtual void  TestCSQ3()
		{
			Query q = new ConstantScoreQuery(new ItemizedFilter(new int[]{0, 2}));
			q.Boost = 1000;
			Qtest(q, new int[]{0, 2});
		}
		
		/* DisjunctionMaxQuery */

        [Test]
        public virtual void TestDMQ1()
		{
			DisjunctionMaxQuery q = new DisjunctionMaxQuery(0.0f);
			q.Add(new TermQuery(new Term(FIELD, "w1")));
			q.Add(new TermQuery(new Term(FIELD, "w5")));
			Qtest(q, new int[]{0, 1, 2, 3});
		}
		[Test]
		public virtual void  TestDMQ2()
		{
			DisjunctionMaxQuery q = new DisjunctionMaxQuery(0.5f);
			q.Add(new TermQuery(new Term(FIELD, "w1")));
			q.Add(new TermQuery(new Term(FIELD, "w5")));
			Qtest(q, new int[]{0, 1, 2, 3});
		}
		[Test]
		public virtual void  TestDMQ3()
		{
			DisjunctionMaxQuery q = new DisjunctionMaxQuery(0.5f);
			q.Add(new TermQuery(new Term(FIELD, "QQ")));
			q.Add(new TermQuery(new Term(FIELD, "w5")));
			Qtest(q, new int[]{0});
		}
		[Test]
		public virtual void  TestDMQ4()
		{
			DisjunctionMaxQuery q = new DisjunctionMaxQuery(0.5f);
			q.Add(new TermQuery(new Term(FIELD, "QQ")));
			q.Add(new TermQuery(new Term(FIELD, "xx")));
			Qtest(q, new int[]{2, 3});
		}
		[Test]
		public virtual void  TestDMQ5()
		{
			DisjunctionMaxQuery q = new DisjunctionMaxQuery(0.5f);
			BooleanQuery booleanQuery = new BooleanQuery();
			booleanQuery.Add(new TermQuery(new Term(FIELD, "yy")), BooleanClause.Occur.SHOULD
				);
			booleanQuery.Add(new TermQuery(new Term(FIELD, "QQ")), BooleanClause.Occur.MUST_NOT
				);
			q.Add(booleanQuery);
			q.Add(new TermQuery(new Term(FIELD, "xx")));
			Qtest(q, new int[]{2, 3});
		}
        [Test]
        public virtual void TestDMQ6()
		{
			DisjunctionMaxQuery q = new DisjunctionMaxQuery(0.5f);
			BooleanQuery booleanQuery = new BooleanQuery();
			booleanQuery.Add(new TermQuery(new Term(FIELD, "yy")), BooleanClause.Occur.MUST_NOT
				);
			booleanQuery.Add(new TermQuery(new Term(FIELD, "w3")), BooleanClause.Occur.SHOULD
				);
			q.Add(booleanQuery);
			q.Add(new TermQuery(new Term(FIELD, "xx")));
			Qtest(q, new int[]{0, 1, 2, 3});
		}
		[Test]
		public virtual void  TestDMQ7()
		{
			DisjunctionMaxQuery q = new DisjunctionMaxQuery(0.5f);
			BooleanQuery booleanQuery = new BooleanQuery();
			booleanQuery.Add(new TermQuery(new Term(FIELD, "yy")), BooleanClause.Occur.MUST_NOT
				);
			booleanQuery.Add(new TermQuery(new Term(FIELD, "w3")), BooleanClause.Occur.SHOULD
				);
			q.Add(booleanQuery);
			q.Add(new TermQuery(new Term(FIELD, "w2")));
			Qtest(q, new int[]{0, 1, 2, 3});
		}
		[Test]
		public virtual void  TestDMQ8()
		{
			DisjunctionMaxQuery q = new DisjunctionMaxQuery(0.5f);
			BooleanQuery booleanQuery = new BooleanQuery();
			booleanQuery.Add(new TermQuery(new Term(FIELD, "yy")), BooleanClause.Occur.SHOULD
				);
			TermQuery boostedQuery = new TermQuery(new Term(FIELD, "w5"));
			boostedQuery.SetBoost(100);
			booleanQuery.Add(boostedQuery, BooleanClause.Occur.SHOULD);
			q.Add(booleanQuery);
			TermQuery xxBoostedQuery = new TermQuery(new Term(FIELD, "xx"));
			xxBoostedQuery.SetBoost(100000);
			q.Add(xxBoostedQuery);
			Qtest(q, new int[]{0, 2, 3});
		}
		[Test]
		public virtual void  TestDMQ9()
		{
			DisjunctionMaxQuery q = new DisjunctionMaxQuery(0.5f);
			BooleanQuery booleanQuery = new BooleanQuery();
			booleanQuery.Add(new TermQuery(new Term(FIELD, "yy")), BooleanClause.Occur.SHOULD
				);
			TermQuery boostedQuery = new TermQuery(new Term(FIELD, "w5"));
			boostedQuery.SetBoost(100);
			booleanQuery.Add(boostedQuery, BooleanClause.Occur.SHOULD);
			q.Add(booleanQuery);
			TermQuery xxBoostedQuery = new TermQuery(new Term(FIELD, "xx"));
			xxBoostedQuery.SetBoost(0);
			q.Add(xxBoostedQuery);
			Qtest(q, new int[]{0, 2, 3});
		}
		
		/* MultiPhraseQuery */
		
		[Test]
		public virtual void  TestMPQ1()
		{
			MultiPhraseQuery q = new MultiPhraseQuery();
			q.Add(Ta(new System.String[]{"w1"}));
			q.Add(Ta(new System.String[]{"w2", "w3", "xx"}));
			Qtest(q, new int[]{0, 1, 2, 3});
		}
		[Test]
		public virtual void  TestMPQ2()
		{
			MultiPhraseQuery q = new MultiPhraseQuery();
			q.Add(Ta(new System.String[]{"w1"}));
			q.Add(Ta(new System.String[]{"w2", "w3"}));
			Qtest(q, new int[]{0, 1, 3});
		}
		[Test]
		public virtual void  TestMPQ3()
		{
			MultiPhraseQuery q = new MultiPhraseQuery();
			q.Add(Ta(new System.String[]{"w1", "xx"}));
			q.Add(Ta(new System.String[]{"w2", "w3"}));
			Qtest(q, new int[]{0, 1, 2, 3});
		}
		[Test]
		public virtual void  TestMPQ4()
		{
			MultiPhraseQuery q = new MultiPhraseQuery();
			q.Add(Ta(new System.String[]{"w1"}));
			q.Add(Ta(new System.String[]{"w2"}));
			Qtest(q, new int[]{0});
		}
		[Test]
		public virtual void  TestMPQ5()
		{
			MultiPhraseQuery q = new MultiPhraseQuery();
			q.Add(Ta(new System.String[]{"w1"}));
			q.Add(Ta(new System.String[]{"w2"}));
			q.Slop = 1;
			Qtest(q, new int[]{0, 1, 2});
		}
		[Test]
		public virtual void  TestMPQ6()
		{
			MultiPhraseQuery q = new MultiPhraseQuery();
			q.Add(Ta(new System.String[]{"w1", "w3"}));
			q.Add(Ta(new System.String[]{"w2"}));
			q.Slop = 1;
			Qtest(q, new int[]{0, 1, 2, 3});
		}
		
		/* some simple tests of boolean queries containing term queries */
		
		[Test]
		public virtual void  TestBQ1()
		{
			BooleanQuery query = new BooleanQuery();
			query.Add(new TermQuery(new Term(FIELD, "w1")), BooleanClause.Occur.MUST);
			query.Add(new TermQuery(new Term(FIELD, "w2")), BooleanClause.Occur.MUST);
			Qtest(query, new int[] { 0, 1, 2, 3 });
		}
		[Test]
		public virtual void  TestBQ2()
		{
			BooleanQuery query = new BooleanQuery();
			query.Add(new TermQuery(new Term(FIELD, "yy")), BooleanClause.Occur.MUST);
			query.Add(new TermQuery(new Term(FIELD, "w3")), BooleanClause.Occur.MUST);
			Qtest(query, new int[] { 2, 3 });
		}
		[Test]
		public virtual void  TestBQ3()
		{
			BooleanQuery query = new BooleanQuery();
			query.Add(new TermQuery(new Term(FIELD, "yy")), BooleanClause.Occur.SHOULD);
			query.Add(new TermQuery(new Term(FIELD, "w3")), BooleanClause.Occur.MUST);
			Qtest(query, new int[] { 0, 1, 2, 3 });
		}
		[Test]
		public virtual void  TestBQ4()
		{
			BooleanQuery outerQuery = new BooleanQuery();
			outerQuery.Add(new TermQuery(new Term(FIELD, "w1")), BooleanClause.Occur.SHOULD);
			BooleanQuery innerQuery = new BooleanQuery();
			innerQuery.Add(new TermQuery(new Term(FIELD, "xx")), BooleanClause.Occur.MUST_NOT
				);
			innerQuery.Add(new TermQuery(new Term(FIELD, "w2")), BooleanClause.Occur.SHOULD);
			outerQuery.Add(innerQuery, BooleanClause.Occur.SHOULD);
			Qtest(outerQuery, new int[] { 0, 1, 2, 3 });
		}
		[Test]
		public virtual void  TestBQ5()
		{
			BooleanQuery outerQuery = new BooleanQuery();
			outerQuery.Add(new TermQuery(new Term(FIELD, "w1")), BooleanClause.Occur.SHOULD);
			BooleanQuery innerQuery = new BooleanQuery();
			innerQuery.Add(new TermQuery(new Term(FIELD, "qq")), BooleanClause.Occur.MUST);
			innerQuery.Add(new TermQuery(new Term(FIELD, "w2")), BooleanClause.Occur.SHOULD);
			outerQuery.Add(innerQuery, BooleanClause.Occur.SHOULD);
			Qtest(outerQuery, new int[] { 0, 1, 2, 3 });
		}
		[Test]
		public virtual void  TestBQ6()
		{
			BooleanQuery outerQuery = new BooleanQuery();
			outerQuery.Add(new TermQuery(new Term(FIELD, "w1")), BooleanClause.Occur.SHOULD);
			BooleanQuery innerQuery = new BooleanQuery();
			innerQuery.Add(new TermQuery(new Term(FIELD, "qq")), BooleanClause.Occur.MUST_NOT
				);
			innerQuery.Add(new TermQuery(new Term(FIELD, "w5")), BooleanClause.Occur.SHOULD);
			outerQuery.Add(innerQuery, BooleanClause.Occur.MUST_NOT);
			Qtest(outerQuery, new int[] { 1, 2, 3 });
		}
		[Test]
		public virtual void  TestBQ7()
		{
			BooleanQuery outerQuery = new BooleanQuery();
			outerQuery.Add(new TermQuery(new Term(FIELD, "w1")), BooleanClause.Occur.MUST);
			BooleanQuery innerQuery = new BooleanQuery();
			innerQuery.Add(new TermQuery(new Term(FIELD, "qq")), BooleanClause.Occur.SHOULD);
			BooleanQuery childLeft = new BooleanQuery();
			childLeft.Add(new TermQuery(new Term(FIELD, "xx")), BooleanClause.Occur.SHOULD);
			childLeft.Add(new TermQuery(new Term(FIELD, "w2")), BooleanClause.Occur.MUST_NOT);
			innerQuery.Add(childLeft, BooleanClause.Occur.SHOULD);
			BooleanQuery childRight = new BooleanQuery();
			childRight.Add(new TermQuery(new Term(FIELD, "w3")), BooleanClause.Occur.MUST);
			childRight.Add(new TermQuery(new Term(FIELD, "w4")), BooleanClause.Occur.MUST);
			innerQuery.Add(childRight, BooleanClause.Occur.SHOULD);
			outerQuery.Add(innerQuery, BooleanClause.Occur.MUST);
			Qtest(outerQuery, new int[] { 0 });
		}
		[Test]
		public virtual void  TestBQ8()
		{
			BooleanQuery outerQuery = new BooleanQuery();
			outerQuery.Add(new TermQuery(new Term(FIELD, "w1")), BooleanClause.Occur.MUST);
			BooleanQuery innerQuery = new BooleanQuery();
			innerQuery.Add(new TermQuery(new Term(FIELD, "qq")), BooleanClause.Occur.SHOULD);
			BooleanQuery childLeft = new BooleanQuery();
			childLeft.Add(new TermQuery(new Term(FIELD, "xx")), BooleanClause.Occur.SHOULD);
			childLeft.Add(new TermQuery(new Term(FIELD, "w2")), BooleanClause.Occur.MUST_NOT);
			innerQuery.Add(childLeft, BooleanClause.Occur.SHOULD);
			BooleanQuery childRight = new BooleanQuery();
			childRight.Add(new TermQuery(new Term(FIELD, "w3")), BooleanClause.Occur.MUST);
			childRight.Add(new TermQuery(new Term(FIELD, "w4")), BooleanClause.Occur.MUST);
			innerQuery.Add(childRight, BooleanClause.Occur.SHOULD);
			outerQuery.Add(innerQuery, BooleanClause.Occur.SHOULD);
			Qtest(outerQuery, new int[] { 0, 1, 2, 3 });
		}
		[Test]
		public virtual void  TestBQ9()
		{
			BooleanQuery outerQuery = new BooleanQuery();
			outerQuery.Add(new TermQuery(new Term(FIELD, "w1")), BooleanClause.Occur.MUST);
			BooleanQuery innerQuery = new BooleanQuery();
			innerQuery.Add(new TermQuery(new Term(FIELD, "qq")), BooleanClause.Occur.SHOULD);
			BooleanQuery childLeft = new BooleanQuery();
			childLeft.Add(new TermQuery(new Term(FIELD, "xx")), BooleanClause.Occur.MUST_NOT);
			childLeft.Add(new TermQuery(new Term(FIELD, "w2")), BooleanClause.Occur.SHOULD);
			innerQuery.Add(childLeft, BooleanClause.Occur.SHOULD);
			BooleanQuery childRight = new BooleanQuery();
			childRight.Add(new TermQuery(new Term(FIELD, "w3")), BooleanClause.Occur.MUST);
			childRight.Add(new TermQuery(new Term(FIELD, "w4")), BooleanClause.Occur.MUST);
			innerQuery.Add(childRight, BooleanClause.Occur.MUST_NOT);
			outerQuery.Add(innerQuery, BooleanClause.Occur.SHOULD);
			Qtest(outerQuery, new int[] { 0, 1, 2, 3 });
		}
		[Test]
		public virtual void  TestBQ10()
		{
			BooleanQuery outerQuery = new BooleanQuery();
			outerQuery.Add(new TermQuery(new Term(FIELD, "w1")), BooleanClause.Occur.MUST);
			BooleanQuery innerQuery = new BooleanQuery();
			innerQuery.Add(new TermQuery(new Term(FIELD, "qq")), BooleanClause.Occur.SHOULD);
			BooleanQuery childLeft = new BooleanQuery();
			childLeft.Add(new TermQuery(new Term(FIELD, "xx")), BooleanClause.Occur.MUST_NOT);
			childLeft.Add(new TermQuery(new Term(FIELD, "w2")), BooleanClause.Occur.SHOULD);
			innerQuery.Add(childLeft, BooleanClause.Occur.SHOULD);
			BooleanQuery childRight = new BooleanQuery();
			childRight.Add(new TermQuery(new Term(FIELD, "w3")), BooleanClause.Occur.MUST);
			childRight.Add(new TermQuery(new Term(FIELD, "w4")), BooleanClause.Occur.MUST);
			innerQuery.Add(childRight, BooleanClause.Occur.MUST_NOT);
			outerQuery.Add(innerQuery, BooleanClause.Occur.MUST);
			Qtest(outerQuery, new int[] { 1 });
		}
		[Test]
		public virtual void  TestBQ11()
		{
			BooleanQuery query = new BooleanQuery();
			query.Add(new TermQuery(new Term(FIELD, "w1")), BooleanClause.Occur.SHOULD);
			TermQuery boostedQuery = new TermQuery(new Term(FIELD, "w1"));
			boostedQuery.SetBoost(1000);
			query.Add(boostedQuery, BooleanClause.Occur.SHOULD);
			Qtest(query, new int[] { 0, 1, 2, 3 });
		}
		[Test]
		public virtual void  TestBQ14()
		{
			BooleanQuery q = new BooleanQuery(true);
			q.Add(new TermQuery(new Term(FIELD, "QQQQQ")), BooleanClause.Occur.SHOULD);
			q.Add(new TermQuery(new Term(FIELD, "w1")), BooleanClause.Occur.SHOULD);
			Qtest(q, new int[]{0, 1, 2, 3});
		}
		[Test]
		public virtual void  TestBQ15()
		{
			BooleanQuery q = new BooleanQuery(true);
			q.Add(new TermQuery(new Term(FIELD, "QQQQQ")), BooleanClause.Occur.MUST_NOT);
			q.Add(new TermQuery(new Term(FIELD, "w1")), BooleanClause.Occur.SHOULD);
			Qtest(q, new int[]{0, 1, 2, 3});
		}
		[Test]
		public virtual void  TestBQ16()
		{
			BooleanQuery q = new BooleanQuery(true);
			q.Add(new TermQuery(new Term(FIELD, "QQQQQ")), BooleanClause.Occur.SHOULD);
			BooleanQuery booleanQuery = new BooleanQuery();
			booleanQuery.Add(new TermQuery(new Term(FIELD, "w1")), BooleanClause.Occur.SHOULD
				);
			booleanQuery.Add(new TermQuery(new Term(FIELD, "xx")), BooleanClause.Occur.MUST_NOT
				);
			q.Add(booleanQuery, BooleanClause.Occur.SHOULD);
			Qtest(q, new int[]{0, 1});
		}
        [Test]
        public virtual void TestBQ17()
		{
			BooleanQuery q = new BooleanQuery(true);
			q.Add(new TermQuery(new Term(FIELD, "w2")), BooleanClause.Occur.SHOULD);
			BooleanQuery booleanQuery = new BooleanQuery();
			booleanQuery.Add(new TermQuery(new Term(FIELD, "w1")), BooleanClause.Occur.SHOULD
				);
			booleanQuery.Add(new TermQuery(new Term(FIELD, "xx")), BooleanClause.Occur.MUST_NOT
				);
			q.Add(booleanQuery, BooleanClause.Occur.SHOULD);
			Qtest(q, new int[]{0, 1, 2, 3});
		}
		[Test]
		public virtual void  TestBQ19()
		{
			BooleanQuery query = new BooleanQuery();
			query.Add(new TermQuery(new Term(FIELD, "yy")), BooleanClause.Occur.MUST_NOT);
			query.Add(new TermQuery(new Term(FIELD, "w3")), BooleanClause.Occur.SHOULD);
			Qtest(query, new int[] { 0, 1 });
		}
		
		[Test]
		public virtual void  TestBQ20()
		{
			BooleanQuery q = new BooleanQuery();
			q.MinimumNumberShouldMatch = 2;
			q.Add(new TermQuery(new Term(FIELD, "QQQQQ")), BooleanClause.Occur.SHOULD);
			q.Add(new TermQuery(new Term(FIELD, "yy")), BooleanClause.Occur.SHOULD);
			q.Add(new TermQuery(new Term(FIELD, "zz")), BooleanClause.Occur.SHOULD);
			q.Add(new TermQuery(new Term(FIELD, "w5")), BooleanClause.Occur.SHOULD);
			q.Add(new TermQuery(new Term(FIELD, "w4")), BooleanClause.Occur.SHOULD);
			
			Qtest(q, new int[]{0, 3});
		}
		
		public virtual void TestMultiFieldBQ1()
		{
			BooleanQuery query = new BooleanQuery();
			query.Add(new TermQuery(new Term(FIELD, "w1")), BooleanClause.Occur.MUST);
			query.Add(new TermQuery(new Term(ALTFIELD, "w2")), BooleanClause.Occur.MUST);
			Qtest(query, new int[] { 0, 1, 2, 3 });
		}
		
		public virtual void TestMultiFieldBQ2()
		{
			BooleanQuery query = new BooleanQuery();
			query.Add(new TermQuery(new Term(FIELD, "yy")), BooleanClause.Occur.MUST);
			query.Add(new TermQuery(new Term(ALTFIELD, "w3")), BooleanClause.Occur.MUST);
			Qtest(query, new int[] { 2, 3 });
		}
		public virtual void TestMultiFieldBQ3()
		{
			BooleanQuery query = new BooleanQuery();
			query.Add(new TermQuery(new Term(FIELD, "yy")), BooleanClause.Occur.SHOULD);
			query.Add(new TermQuery(new Term(ALTFIELD, "w3")), BooleanClause.Occur.MUST);
			Qtest(query, new int[] { 0, 1, 2, 3 });
		}
		public virtual void TestMultiFieldBQ4()
		{
			BooleanQuery outerQuery = new BooleanQuery();
			outerQuery.Add(new TermQuery(new Term(FIELD, "w1")), BooleanClause.Occur.SHOULD);
			BooleanQuery innerQuery = new BooleanQuery();
			innerQuery.Add(new TermQuery(new Term(FIELD, "xx")), BooleanClause.Occur.MUST_NOT
				);
			innerQuery.Add(new TermQuery(new Term(ALTFIELD, "w2")), BooleanClause.Occur.SHOULD
				);
			outerQuery.Add(innerQuery, BooleanClause.Occur.SHOULD);
			Qtest(outerQuery, new int[] { 0, 1, 2, 3 });
		}
		public virtual void TestMultiFieldBQ5()
		{
			BooleanQuery outerQuery = new BooleanQuery();
			outerQuery.Add(new TermQuery(new Term(FIELD, "w1")), BooleanClause.Occur.SHOULD);
			BooleanQuery innerQuery = new BooleanQuery();
			innerQuery.Add(new TermQuery(new Term(ALTFIELD, "qq")), BooleanClause.Occur.MUST);
			innerQuery.Add(new TermQuery(new Term(ALTFIELD, "w2")), BooleanClause.Occur.SHOULD
				);
			outerQuery.Add(innerQuery, BooleanClause.Occur.SHOULD);
			Qtest(outerQuery, new int[] { 0, 1, 2, 3 });
		}
		public virtual void TestMultiFieldBQ6()
		{
			BooleanQuery outerQuery = new BooleanQuery();
			outerQuery.Add(new TermQuery(new Term(FIELD, "w1")), BooleanClause.Occur.SHOULD);
			BooleanQuery innerQuery = new BooleanQuery();
			innerQuery.Add(new TermQuery(new Term(ALTFIELD, "qq")), BooleanClause.Occur.MUST_NOT
				);
			innerQuery.Add(new TermQuery(new Term(ALTFIELD, "w5")), BooleanClause.Occur.SHOULD
				);
			outerQuery.Add(innerQuery, BooleanClause.Occur.MUST_NOT);
			Qtest(outerQuery, new int[] { 1, 2, 3 });
		}
		public virtual void TestMultiFieldBQ7()
		{
			BooleanQuery outerQuery = new BooleanQuery();
			outerQuery.Add(new TermQuery(new Term(FIELD, "w1")), BooleanClause.Occur.MUST);
			BooleanQuery innerQuery = new BooleanQuery();
			innerQuery.Add(new TermQuery(new Term(ALTFIELD, "qq")), BooleanClause.Occur.SHOULD
				);
			BooleanQuery childLeft = new BooleanQuery();
			childLeft.Add(new TermQuery(new Term(ALTFIELD, "xx")), BooleanClause.Occur.SHOULD
				);
			childLeft.Add(new TermQuery(new Term(ALTFIELD, "w2")), BooleanClause.Occur.MUST_NOT
				);
			innerQuery.Add(childLeft, BooleanClause.Occur.SHOULD);
			BooleanQuery childRight = new BooleanQuery();
			childRight.Add(new TermQuery(new Term(ALTFIELD, "w3")), BooleanClause.Occur.MUST);
			childRight.Add(new TermQuery(new Term(ALTFIELD, "w4")), BooleanClause.Occur.MUST);
			innerQuery.Add(childRight, BooleanClause.Occur.SHOULD);
			outerQuery.Add(innerQuery, BooleanClause.Occur.MUST);
			Qtest(outerQuery, new int[] { 0 });
		}
		public virtual void TestMultiFieldBQ8()
		{
			BooleanQuery outerQuery = new BooleanQuery();
			outerQuery.Add(new TermQuery(new Term(ALTFIELD, "w1")), BooleanClause.Occur.MUST);
			BooleanQuery innerQuery = new BooleanQuery();
			innerQuery.Add(new TermQuery(new Term(FIELD, "qq")), BooleanClause.Occur.SHOULD);
			BooleanQuery childLeft = new BooleanQuery();
			childLeft.Add(new TermQuery(new Term(ALTFIELD, "xx")), BooleanClause.Occur.SHOULD
				);
			childLeft.Add(new TermQuery(new Term(FIELD, "w2")), BooleanClause.Occur.MUST_NOT);
			innerQuery.Add(childLeft, BooleanClause.Occur.SHOULD);
			BooleanQuery childRight = new BooleanQuery();
			childRight.Add(new TermQuery(new Term(ALTFIELD, "w3")), BooleanClause.Occur.MUST);
			childRight.Add(new TermQuery(new Term(FIELD, "w4")), BooleanClause.Occur.MUST);
			innerQuery.Add(childRight, BooleanClause.Occur.SHOULD);
			outerQuery.Add(innerQuery, BooleanClause.Occur.SHOULD);
			Qtest(outerQuery, new int[] { 0, 1, 2, 3 });
		}
		public virtual void TestMultiFieldBQ9()
		{
			BooleanQuery outerQuery = new BooleanQuery();
			outerQuery.Add(new TermQuery(new Term(FIELD, "w1")), BooleanClause.Occur.MUST);
			BooleanQuery innerQuery = new BooleanQuery();
			innerQuery.Add(new TermQuery(new Term(ALTFIELD, "qq")), BooleanClause.Occur.SHOULD
				);
			BooleanQuery childLeft = new BooleanQuery();
			childLeft.Add(new TermQuery(new Term(FIELD, "xx")), BooleanClause.Occur.MUST_NOT);
			childLeft.Add(new TermQuery(new Term(FIELD, "w2")), BooleanClause.Occur.SHOULD);
			innerQuery.Add(childLeft, BooleanClause.Occur.SHOULD);
			BooleanQuery childRight = new BooleanQuery();
			childRight.Add(new TermQuery(new Term(ALTFIELD, "w3")), BooleanClause.Occur.MUST);
			childRight.Add(new TermQuery(new Term(FIELD, "w4")), BooleanClause.Occur.MUST);
			innerQuery.Add(childRight, BooleanClause.Occur.MUST_NOT);
			outerQuery.Add(innerQuery, BooleanClause.Occur.SHOULD);
			Qtest(outerQuery, new int[] { 0, 1, 2, 3 });
		}
		public virtual void TestMultiFieldBQ10()
		{
			BooleanQuery outerQuery = new BooleanQuery();
			outerQuery.Add(new TermQuery(new Term(FIELD, "w1")), BooleanClause.Occur.MUST);
			BooleanQuery innerQuery = new BooleanQuery();
			innerQuery.Add(new TermQuery(new Term(ALTFIELD, "qq")), BooleanClause.Occur.SHOULD
				);
			BooleanQuery childLeft = new BooleanQuery();
			childLeft.Add(new TermQuery(new Term(FIELD, "xx")), BooleanClause.Occur.MUST_NOT);
			childLeft.Add(new TermQuery(new Term(ALTFIELD, "w2")), BooleanClause.Occur.SHOULD
				);
			innerQuery.Add(childLeft, BooleanClause.Occur.SHOULD);
			BooleanQuery childRight = new BooleanQuery();
			childRight.Add(new TermQuery(new Term(ALTFIELD, "w3")), BooleanClause.Occur.MUST);
			childRight.Add(new TermQuery(new Term(FIELD, "w4")), BooleanClause.Occur.MUST);
			innerQuery.Add(childRight, BooleanClause.Occur.MUST_NOT);
			outerQuery.Add(innerQuery, BooleanClause.Occur.MUST);
			Qtest(outerQuery, new int[] { 1 });
		}
		public virtual void TestMultiFieldBQofPQ1()
		{
			BooleanQuery query = new BooleanQuery();
			PhraseQuery leftChild = new PhraseQuery();
			leftChild.Add(new Term(FIELD, "w1"));
			leftChild.Add(new Term(FIELD, "w2"));
			query.Add(leftChild, BooleanClause.Occur.SHOULD);
			PhraseQuery rightChild = new PhraseQuery();
			rightChild.Add(new Term(ALTFIELD, "w1"));
			rightChild.Add(new Term(ALTFIELD, "w2"));
			query.Add(rightChild, BooleanClause.Occur.SHOULD);
			Qtest(query, new int[] { 0 });
		}
		public virtual void TestMultiFieldBQofPQ2()
		{
			BooleanQuery query = new BooleanQuery();
			PhraseQuery leftChild = new PhraseQuery();
			leftChild.Add(new Term(FIELD, "w1"));
			leftChild.Add(new Term(FIELD, "w3"));
			query.Add(leftChild, BooleanClause.Occur.SHOULD);
			PhraseQuery rightChild = new PhraseQuery();
			rightChild.Add(new Term(ALTFIELD, "w1"));
			rightChild.Add(new Term(ALTFIELD, "w3"));
			query.Add(rightChild, BooleanClause.Occur.SHOULD);
			Qtest(query, new int[] { 1, 3 });
		}
		public virtual void TestMultiFieldBQofPQ3()
		{
			BooleanQuery query = new BooleanQuery();
			PhraseQuery leftChild = new PhraseQuery();
			leftChild.SetSlop(1);
			leftChild.Add(new Term(FIELD, "w1"));
			leftChild.Add(new Term(FIELD, "w2"));
			query.Add(leftChild, BooleanClause.Occur.SHOULD);
			PhraseQuery rightChild = new PhraseQuery();
			rightChild.SetSlop(1);
			rightChild.Add(new Term(ALTFIELD, "w1"));
			rightChild.Add(new Term(ALTFIELD, "w2"));
			query.Add(rightChild, BooleanClause.Occur.SHOULD);
			Qtest(query, new int[] { 0, 1, 2 });
		}
		public virtual void TestMultiFieldBQofPQ4()
		{
			BooleanQuery query = new BooleanQuery();
			PhraseQuery leftChild = new PhraseQuery();
			leftChild.SetSlop(1);
			leftChild.Add(new Term(FIELD, "w2"));
			leftChild.Add(new Term(FIELD, "w3"));
			query.Add(leftChild, BooleanClause.Occur.SHOULD);
			PhraseQuery rightChild = new PhraseQuery();
			rightChild.SetSlop(1);
			rightChild.Add(new Term(ALTFIELD, "w2"));
			rightChild.Add(new Term(ALTFIELD, "w3"));
			query.Add(rightChild, BooleanClause.Occur.SHOULD);
			Qtest(query, new int[] { 0, 1, 2, 3 });
		}
		public virtual void TestMultiFieldBQofPQ5()
		{
			BooleanQuery query = new BooleanQuery();
			PhraseQuery leftChild = new PhraseQuery();
			leftChild.SetSlop(1);
			leftChild.Add(new Term(FIELD, "w3"));
			leftChild.Add(new Term(FIELD, "w2"));
			query.Add(leftChild, BooleanClause.Occur.SHOULD);
			PhraseQuery rightChild = new PhraseQuery();
			rightChild.SetSlop(1);
			rightChild.Add(new Term(ALTFIELD, "w3"));
			rightChild.Add(new Term(ALTFIELD, "w2"));
			query.Add(rightChild, BooleanClause.Occur.SHOULD);
			Qtest(query, new int[] { 1, 3 });
		}
		public virtual void TestMultiFieldBQofPQ6()
		{
			BooleanQuery query = new BooleanQuery();
			PhraseQuery leftChild = new PhraseQuery();
			leftChild.SetSlop(2);
			leftChild.Add(new Term(FIELD, "w3"));
			leftChild.Add(new Term(FIELD, "w2"));
			query.Add(leftChild, BooleanClause.Occur.SHOULD);
			PhraseQuery rightChild = new PhraseQuery();
			rightChild.SetSlop(2);
			rightChild.Add(new Term(ALTFIELD, "w3"));
			rightChild.Add(new Term(ALTFIELD, "w2"));
			query.Add(rightChild, BooleanClause.Occur.SHOULD);
			Qtest(query, new int[] { 0, 1, 3 });
		}
		public virtual void TestMultiFieldBQofPQ7()
		{
			BooleanQuery query = new BooleanQuery();
			PhraseQuery leftChild = new PhraseQuery();
			leftChild.SetSlop(3);
			leftChild.Add(new Term(FIELD, "w3"));
			leftChild.Add(new Term(FIELD, "w2"));
			query.Add(leftChild, BooleanClause.Occur.SHOULD);
			PhraseQuery rightChild = new PhraseQuery();
			rightChild.SetSlop(1);
			rightChild.Add(new Term(ALTFIELD, "w3"));
			rightChild.Add(new Term(ALTFIELD, "w2"));
			query.Add(rightChild, BooleanClause.Occur.SHOULD);
			Qtest(query, new int[] { 0, 1, 2, 3 });
		}
		[Test]
		public virtual void  TestTermQueryMultiSearcherExplain()
		{
			// creating two directories for indices
			Directory indexStoreA = new MockRAMDirectory();
			Directory indexStoreB = new MockRAMDirectory();
			
			Document lDoc = new Document();
			lDoc.Add(new Field("handle", "1 2", Field.Store.YES, Field.Index.ANALYZED));
			Document lDoc2 = new Document();
			lDoc2.Add(new Field("handle", "1 2", Field.Store.YES, Field.Index.ANALYZED));
			Document lDoc3 = new Document();
			lDoc3.Add(new Field("handle", "1 2", Field.Store.YES, Field.Index.ANALYZED));
			
			IndexWriter writerA = new IndexWriter(indexStoreA, new StandardAnalyzer(Util.Version.LUCENE_CURRENT), true, IndexWriter.MaxFieldLength.LIMITED);
			IndexWriter writerB = new IndexWriter(indexStoreB, new StandardAnalyzer(Util.Version.LUCENE_CURRENT), true, IndexWriter.MaxFieldLength.LIMITED);
			
			writerA.AddDocument(lDoc);
			writerA.AddDocument(lDoc2);
			writerA.Optimize();
			writerA.Close();
			
			writerB.AddDocument(lDoc3);
			writerB.Close();
			
			QueryParser parser = new QueryParser(Util.Version.LUCENE_CURRENT, "fulltext", new StandardAnalyzer(Util.Version.LUCENE_CURRENT));
			Query query = parser.Parse("handle:1");
			
			Searcher[] searchers = new Searcher[2];
			searchers[0] = new IndexSearcher(indexStoreB, true);
            searchers[1] = new IndexSearcher(indexStoreA, true);
			Searcher mSearcher = new MultiSearcher(searchers);
			ScoreDoc[] hits = mSearcher.Search(query, null, 1000).ScoreDocs;
			
			Assert.AreEqual(3, hits.Length);
			
			Explanation explain = mSearcher.Explain(query, hits[0].Doc);
			System.String exp = explain.ToString(0);
			Assert.IsTrue(exp.IndexOf("maxDocs=3") > - 1, exp);
			Assert.IsTrue(exp.IndexOf("docFreq=3") > - 1, exp);
			
			query = parser.Parse("handle:\"1 2\"");
			hits = mSearcher.Search(query, null, 1000).ScoreDocs;
			
			Assert.AreEqual(3, hits.Length);
			
			explain = mSearcher.Explain(query, hits[0].Doc);
			exp = explain.ToString(0);
			Assert.IsTrue(exp.IndexOf("1=3") > - 1, exp);
			Assert.IsTrue(exp.IndexOf("2=3") > - 1, exp);
			
			query = new SpanNearQuery(new SpanQuery[]{new SpanTermQuery(new Term("handle", "1")), new SpanTermQuery(new Term("handle", "2"))}, 0, true);
			hits = mSearcher.Search(query, null, 1000).ScoreDocs;
			
			Assert.AreEqual(3, hits.Length);
			
			explain = mSearcher.Explain(query, hits[0].Doc);
			exp = explain.ToString(0);
			Assert.IsTrue(exp.IndexOf("1=3") > - 1, exp);
			Assert.IsTrue(exp.IndexOf("2=3") > - 1, exp);
			mSearcher.Close();
		}
	}
}