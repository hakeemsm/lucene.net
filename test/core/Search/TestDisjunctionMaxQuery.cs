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

using WhitespaceAnalyzer = Lucene.Net.Analysis.WhitespaceAnalyzer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using IndexReader = Lucene.Net.Index.IndexReader;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using Term = Lucene.Net.Index.Term;
using Directory = Lucene.Net.Store.Directory;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

namespace Lucene.Net.Search
{
	
	/// <summary> Test of the DisjunctionMaxQuery.
	/// 
	/// </summary>
    [TestFixture]
	public class TestDisjunctionMaxQuery:LuceneTestCase
	{
		public TestDisjunctionMaxQuery()
		{
			InitBlock();
		}
		private void  InitBlock()
		{
			sim = new TestSimilarity();
		}
		
		/// <summary>threshold for comparing floats </summary>
        public const float SCORE_COMP_THRESH = 0.000001f;
		
		/// <summary> Similarity to eliminate tf, idf and lengthNorm effects to
		/// isolate test case.
		/// 
		/// <p/>
		/// same as TestRankingSimilarity in TestRanking.zip from
		/// http://issues.apache.org/jira/browse/LUCENE-323
		/// </summary>
		[Serializable]
		private class TestSimilarity:DefaultSimilarity
		{
			
			public TestSimilarity()
			{
			}
			public override float Tf(float freq)
			{
				if (freq > 0.0f)
					return 1.0f;
				else
					return 0.0f;
			}
			public override float LengthNorm(FieldInvertState state)
			{
				// Disable length norm
				return state.GetBoost();
			}
			public override float Idf(long docFreq, long numDocs)
			{
				return 1.0f;
			}
		}
		
		public Similarity sim;
		public Directory index;
		public IndexReader r;
		public IndexSearcher s;
		
		private static readonly FieldType nonAnalyzedType = new FieldType(TextField.TYPE_STORED
			);

		static TestDisjunctionMaxQuery()
		{
			nonAnalyzedType.SetTokenized(false);
		}
		[SetUp]
		public override void  SetUp()
		{
			base.SetUp();
			
			index = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), index, NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetSimilarity(sim).SetMergePolicy
				(NewLogMergePolicy()));
			// hed is the most important field, dek is secondary
			
			// d1 is an "ok" match for:  albino elephant
			{
				Document d1 = new Document();
				d1.Add(NewField("id", "d1", nonAnalyzedType));
				d1.Add(NewTextField("hed", "elephant", Field.Store.YES));
				d1.Add(NewTextField("dek", "elephant", Field.Store.YES));
				writer.AddDocument(d1);
			}
			
			// d2 is a "good" match for:  albino elephant
			{
				Document d2 = new Document();
				d2.Add(NewField("id", "d2", nonAnalyzedType));
				d2.Add(NewTextField("hed", "elephant", Field.Store.YES));
				d2.Add(NewTextField("dek", "albino", Field.Store.YES));
				d2.Add(NewTextField("dek", "elephant", Field.Store.YES));
				writer.AddDocument(d2);
			}
			
			// d3 is a "better" match for:  albino elephant
			{
				Document d3 = new Document();
				d3.Add(NewField("id", "d3", nonAnalyzedType));
				d3.Add(NewTextField("hed", "albino", Field.Store.YES));
				d3.Add(NewTextField("hed", "elephant", Field.Store.YES));
				writer.AddDocument(d3);
			}
			
			// d4 is the "best" match for:  albino elephant
			{
				Document d4 = new Document();
				d4.Add(NewField("id", "d4", nonAnalyzedType));
				d4.Add(NewTextField("hed", "albino", Field.Store.YES));
				d4.Add(NewField("hed", "elephant", nonAnalyzedType));
				d4.Add(NewTextField("dek", "albino", Field.Store.YES));
				writer.AddDocument(d4);
			}
			r = SlowCompositeReaderWrapper.Wrap(writer.GetReader());
			writer.Close();

			s = NewSearcher(r);
			s.SetSimilarity(sim);
		}
		
		public override void TearDown()
		{
			r.Close();
			index.Close();
			base.TearDown();
		}
		[Test]
		public virtual void  TestSkipToFirsttimeMiss()
		{
			DisjunctionMaxQuery dq = new DisjunctionMaxQuery(0.0f);
			dq.Add(Tq("id", "d1"));
			dq.Add(Tq("dek", "DOES_NOT_EXIST"));
			
			QueryUtils.Check(Random(), dq, s);
			NUnit.Framework.Assert.IsTrue(s.GetTopReaderContext() is AtomicReaderContext);
			Weight dw = s.CreateNormalizedWeight(dq);
			AtomicReaderContext context = (AtomicReaderContext)s.GetTopReaderContext();
			Scorer ds = dw.Scorer(context, ((AtomicReader)context.Reader()).GetLiveDocs());
			bool skipOk = ds.Advance(3) != DocIdSetIterator.NO_MORE_DOCS;
			if (skipOk)
			{
				Assert.Fail("firsttime skipTo found a match? ... " + r.Document(ds.DocID()).Get("id"));
			}
		}
		
		[Test]
		public virtual void  TestSkipToFirsttimeHit()
		{
			DisjunctionMaxQuery dq = new DisjunctionMaxQuery(0.0f);
			dq.Add(Tq("dek", "albino"));
			dq.Add(Tq("dek", "DOES_NOT_EXIST"));
			NUnit.Framework.Assert.IsTrue(s.GetTopReaderContext() is AtomicReaderContext);
			QueryUtils.Check(Random(), dq, s);
			
			Weight dw = s.CreateNormalizedWeight(dq);
			AtomicReaderContext context = (AtomicReaderContext)s.GetTopReaderContext();
			Scorer ds = dw.Scorer(context, ((AtomicReader)context.Reader()).GetLiveDocs());
			Assert.IsTrue(ds.Advance(3) != DocIdSetIterator.NO_MORE_DOCS, "firsttime skipTo found no match");
			Assert.AreEqual("d4", r.Document(ds.DocID()).Get("id"), "found wrong docid");
		}
		
		[Test]
		public virtual void  TestSimpleEqualScores1()
		{
			
			DisjunctionMaxQuery q = new DisjunctionMaxQuery(0.0f);
			q.Add(Tq("hed", "albino"));
			q.Add(Tq("hed", "elephant"));
			QueryUtils.Check(Random(), q, s);
			
			ScoreDoc[] h = s.Search(q, null, 1000).ScoreDocs;
			
			try
			{
				Assert.AreEqual(4, h.Length, "all docs should match " + q.ToString());
				
				float score = h[0].Score;
				for (int i = 1; i < h.Length; i++)
				{
					Assert.AreEqual(score, h[i].Score, SCORE_COMP_THRESH, "score #" + i + " is not the same");
				}
			}
			catch (System.ApplicationException e)
			{
				PrintHits("testSimpleEqualScores1", h, s);
				throw e;
			}
		}
		
		[Test]
		public virtual void  TestSimpleEqualScores2()
		{
			
			DisjunctionMaxQuery q = new DisjunctionMaxQuery(0.0f);
			q.Add(Tq("dek", "albino"));
			q.Add(Tq("dek", "elephant"));
			QueryUtils.Check(Random(), q, s);
			
			
			ScoreDoc[] h = s.Search(q, null, 1000).ScoreDocs;
			
			try
			{
				Assert.AreEqual(3, h.Length, "3 docs should match " + q.ToString());
				float score = h[0].Score;
				for (int i = 1; i < h.Length; i++)
				{
					Assert.AreEqual(score, h[i].Score, SCORE_COMP_THRESH, "score #" + i + " is not the same");
				}
			}
			catch (System.ApplicationException e)
			{
				PrintHits("testSimpleEqualScores2", h, s);
				throw e;
			}
		}
		
		[Test]
		public virtual void  TestSimpleEqualScores3()
		{
			
			DisjunctionMaxQuery q = new DisjunctionMaxQuery(0.0f);
			q.Add(Tq("hed", "albino"));
			q.Add(Tq("hed", "elephant"));
			q.Add(Tq("dek", "albino"));
			q.Add(Tq("dek", "elephant"));
			QueryUtils.Check(Random(), q, s);
			
			
			ScoreDoc[] h = s.Search(q, null, 1000).ScoreDocs;
			
			try
			{
				Assert.AreEqual(4, h.Length, "all docs should match " + q.ToString());
				float score = h[0].Score;
				for (int i = 1; i < h.Length; i++)
				{
					Assert.AreEqual(score, h[i].Score, SCORE_COMP_THRESH, "score #" + i + " is not the same");
				}
			}
			catch (System.ApplicationException e)
			{
				PrintHits("testSimpleEqualScores3", h, s);
				throw e;
			}
		}
		
		[Test]
		public virtual void  TestSimpleTiebreaker()
		{
			
			DisjunctionMaxQuery q = new DisjunctionMaxQuery(0.01f);
			q.Add(Tq("dek", "albino"));
			q.Add(Tq("dek", "elephant"));
			QueryUtils.Check(Random(), q, s);
			
			
			ScoreDoc[] h = s.Search(q, null, 1000).ScoreDocs;
			
			try
			{
				Assert.AreEqual(3, h.Length, "3 docs should match " + q.ToString());
				Assert.AreEqual(s.Doc(h[0].Doc).Get("id"), "d2", "wrong first");
				float score0 = h[0].Score;
				float score1 = h[1].Score;
				float score2 = h[2].Score;
				Assert.IsTrue(score0 > score1, "d2 does not have better score then others: " + score0 + " >? " + score1);
				Assert.AreEqual(score1, score2, SCORE_COMP_THRESH, "d4 and d1 don't have equal scores");
			}
			catch (System.ApplicationException e)
			{
				PrintHits("testSimpleTiebreaker", h, s);
				throw e;
			}
		}
		
		[Test]
		public virtual void  TestBooleanRequiredEqualScores()
		{
			
			BooleanQuery q = new BooleanQuery();
			{
				DisjunctionMaxQuery q1 = new DisjunctionMaxQuery(0.0f);
				q1.Add(Tq("hed", "albino"));
				q1.Add(Tq("dek", "albino"));
				q.Add(q1, Occur.MUST); //true,false);
				QueryUtils.Check(Random(), q1, s);
			}
			{
				DisjunctionMaxQuery q2 = new DisjunctionMaxQuery(0.0f);
				q2.Add(Tq("hed", "elephant"));
				q2.Add(Tq("dek", "elephant"));
				q.Add(q2, Occur.MUST); //true,false);
				QueryUtils.Check(Random(), q2, s);
			}
			
			QueryUtils.Check(Random(), q, s);
			
			ScoreDoc[] h = s.Search(q, null, 1000).ScoreDocs;
			
			try
			{
				Assert.AreEqual(3, h.Length, "3 docs should match " + q.ToString());
				float score = h[0].Score;
				for (int i = 1; i < h.Length; i++)
				{
					Assert.AreEqual(score, h[i].Score, SCORE_COMP_THRESH, "score #" + i + " is not the same");
				}
			}
			catch (System.ApplicationException e)
			{
				PrintHits("testBooleanRequiredEqualScores1", h, s);
				throw e;
			}
		}
		
		
		[Test]
		public virtual void  TestBooleanOptionalNoTiebreaker()
		{
			
			BooleanQuery q = new BooleanQuery();
			{
				DisjunctionMaxQuery q1 = new DisjunctionMaxQuery(0.0f);
				q1.Add(Tq("hed", "albino"));
				q1.Add(Tq("dek", "albino"));
				q.Add(q1, Occur.SHOULD); //false,false);
			}
			{
				DisjunctionMaxQuery q2 = new DisjunctionMaxQuery(0.0f);
				q2.Add(Tq("hed", "elephant"));
				q2.Add(Tq("dek", "elephant"));
				q.Add(q2, Occur.SHOULD); //false,false);
			}
			QueryUtils.Check(Random(), q, s);
			
			
			ScoreDoc[] h = s.Search(q, null, 1000).ScoreDocs;
			
			try
			{
				Assert.AreEqual(4, h.Length, "4 docs should match " + q.ToString());
				float score = h[0].Score;
				for (int i = 1; i < h.Length - 1; i++)
				{
					/* note: -1 */
					Assert.AreEqual(score, h[i].Score, SCORE_COMP_THRESH, "score #" + i + " is not the same");
				}
				Assert.AreEqual("d1", s.Doc(h[h.Length - 1].Doc).Get("id"), "wrong last");
				float score1 = h[h.Length - 1].Score;
				Assert.IsTrue(score > score1, "d1 does not have worse score then others: " + score + " >? " + score1);
			}
			catch (System.ApplicationException e)
			{
				PrintHits("testBooleanOptionalNoTiebreaker", h, s);
				throw e;
			}
		}
		
		
		[Test]
		public virtual void  TestBooleanOptionalWithTiebreaker()
		{
			
			BooleanQuery q = new BooleanQuery();
			{
				DisjunctionMaxQuery q1 = new DisjunctionMaxQuery(0.01f);
				q1.Add(Tq("hed", "albino"));
				q1.Add(Tq("dek", "albino"));
				q.Add(q1, Occur.SHOULD); //false,false);
			}
			{
				DisjunctionMaxQuery q2 = new DisjunctionMaxQuery(0.01f);
				q2.Add(Tq("hed", "elephant"));
				q2.Add(Tq("dek", "elephant"));
				q.Add(q2, Occur.SHOULD); //false,false);
			}
			QueryUtils.Check(Random(), q, s);
			
			
			ScoreDoc[] h = s.Search(q, null, 1000).ScoreDocs;
			
			try
			{
				
				Assert.AreEqual(4, h.Length, "4 docs should match " + q.ToString());
				
				float score0 = h[0].Score;
				float score1 = h[1].Score;
				float score2 = h[2].Score;
				float score3 = h[3].Score;
				
				System.String doc0 = s.Doc(h[0].Doc).Get("id");
				System.String doc1 = s.Doc(h[1].Doc).Get("id");
				System.String doc2 = s.Doc(h[2].Doc).Get("id");
				System.String doc3 = s.Doc(h[3].Doc).Get("id");
				
				Assert.IsTrue(doc0.Equals("d2") || doc0.Equals("d4"), "doc0 should be d2 or d4: " + doc0);
				Assert.IsTrue(doc1.Equals("d2") || doc1.Equals("d4"), "doc1 should be d2 or d4: " + doc0);
				Assert.AreEqual(score0, score1, SCORE_COMP_THRESH, "score0 and score1 should match");
				Assert.AreEqual("d3", doc2, "wrong third");
				Assert.IsTrue(score1 > score2, "d3 does not have worse score then d2 and d4: " + score1 + " >? " + score2);
				
				Assert.AreEqual("d1", doc3, "wrong fourth");
				Assert.IsTrue(score2 > score3, "d1 does not have worse score then d3: " + score2 + " >? " + score3);
			}
			catch (System.ApplicationException e)
			{
				PrintHits("testBooleanOptionalWithTiebreaker", h, s);
				throw e;
			}
		}
		
		
		[Test]
		public virtual void  TestBooleanOptionalWithTiebreakerAndBoost()
		{
			
			BooleanQuery q = new BooleanQuery();
			{
				DisjunctionMaxQuery q1 = new DisjunctionMaxQuery(0.01f);
				q1.Add(Tq("hed", "albino", 1.5f));
				q1.Add(Tq("dek", "albino"));
				q.Add(q1, Occur.SHOULD); //false,false);
			}
			{
				DisjunctionMaxQuery q2 = new DisjunctionMaxQuery(0.01f);
				q2.Add(Tq("hed", "elephant", 1.5f));
				q2.Add(Tq("dek", "elephant"));
				q.Add(q2, Occur.SHOULD); //false,false);
			}
			QueryUtils.Check(Random(), q, s);
			
			
			ScoreDoc[] h = s.Search(q, null, 1000).ScoreDocs;
			
			try
			{
				
				Assert.AreEqual(4, h.Length, "4 docs should match " + q.ToString());
				
				float score0 = h[0].Score;
				float score1 = h[1].Score;
				float score2 = h[2].Score;
				float score3 = h[3].Score;
				
				System.String doc0 = s.Doc(h[0].Doc).Get("id");
				System.String doc1 = s.Doc(h[1].Doc).Get("id");
				System.String doc2 = s.Doc(h[2].Doc).Get("id");
				System.String doc3 = s.Doc(h[3].Doc).Get("id");
				
				Assert.AreEqual("d4", doc0, "doc0 should be d4: ");
				Assert.AreEqual("d3", doc1, "doc1 should be d3: ");
				Assert.AreEqual("d2", doc2, "doc2 should be d2: ");
				Assert.AreEqual("d1", doc3, "doc3 should be d1: ");
				
				Assert.IsTrue(score0 > score1, "d4 does not have a better score then d3: " + score0 + " >? " + score1);
				Assert.IsTrue(score1 > score2, "d3 does not have a better score then d2: " + score1 + " >? " + score2);
				Assert.IsTrue(score2 > score3, "d3 does not have a better score then d1: " + score2 + " >? " + score3);
			}
			catch (System.ApplicationException e)
			{
				PrintHits("testBooleanOptionalWithTiebreakerAndBoost", h, s);
				throw e;
			}
		}
		
		public virtual void TestBooleanSpanQuery()
		{
			int hits = 0;
			Directory directory = NewDirectory();
			Analyzer indexerAnalyzer = new MockAnalyzer(Random());
			IndexWriterConfig config = new IndexWriterConfig(TEST_VERSION_CURRENT, indexerAnalyzer
				);
			IndexWriter writer = new IndexWriter(directory, config);
			string FIELD = "content";
			Lucene.Net.Document.Document d = new Lucene.Net.Document.Document();
			d.Add(new TextField(FIELD, "clockwork orange", Field.Store.YES));
			writer.AddDocument(d);
			writer.Close();
			IndexReader indexReader = DirectoryReader.Open(directory);
			IndexSearcher searcher = NewSearcher(indexReader);
			DisjunctionMaxQuery query = new DisjunctionMaxQuery(1.0f);
			SpanQuery sq1 = new SpanTermQuery(new Term(FIELD, "clockwork"));
			SpanQuery sq2 = new SpanTermQuery(new Term(FIELD, "clckwork"));
			query.Add(sq1);
			query.Add(sq2);
			TopScoreDocCollector collector = TopScoreDocCollector.Create(1000, true);
			searcher.Search(query, collector);
			hits = collector.TopDocs().scoreDocs.Length;
			foreach (ScoreDoc scoreDoc in collector.TopDocs().scoreDocs)
			{
				System.Console.Out.WriteLine(scoreDoc.doc);
			}
			indexReader.Close();
			NUnit.Framework.Assert.AreEqual(hits, 1);
			directory.Close();
		}
		
		/// <summary>macro </summary>
		protected internal virtual Query Tq(string f, string t)
		{
			return new TermQuery(new Term(f, t));
		}
		/// <summary>macro </summary>
		protected internal virtual Query Tq(string f, string t, float b)
		{
			Query q = Tq(f, t);
			q.Boost = b;
			return q;
		}
		
		
		protected internal virtual void PrintHits(string test, ScoreDoc[] h, IndexSearcher
			 searcher)
		{
			
			System.Console.Error.WriteLine("------- " + test + " -------");
			DecimalFormat f = new DecimalFormat("0.000000000", DecimalFormatSymbols.GetInstance
				(CultureInfo.ROOT));
			for (int i = 0; i < h.Length; i++)
			{
				Document d = searcher.Doc(h[i].Doc);
				float score = h[i].Score;
				System.Console.Error.WriteLine("#" + i + ": {0.000000000}" + score + " - " + d.Get("id"));
			}
		}
	}
}