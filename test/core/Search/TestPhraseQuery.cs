/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Test.Analysis.Tokenattributes;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Store;
using Lucene.Net.Util;


namespace Lucene.Net.Search
{
	/// <summary>
	/// Tests
	/// <see cref="PhraseQuery">PhraseQuery</see>
	/// .
	/// </summary>
	/// <seealso cref="TestPositionIncrement">TestPositionIncrement</seealso>
	public class TestPhraseQuery : LuceneTestCase
	{
		/// <summary>threshold for comparing floats</summary>
		public const float SCORE_COMP_THRESH = 1e-6f;

		private static IndexSearcher searcher;

		private static IndexReader reader;

		private PhraseQuery query;

		private static Directory directory;

		// @ThreadLeaks(linger = 1000, leakedThreadsBelongToSuite = true)
		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.BeforeClass]
		public static void BeforeClass()
		{
			directory = NewDirectory();
			Analyzer analyzer = new _Analyzer_61();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), directory, analyzer);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewTextField("field", "one two three four five", Field.Store.YES));
			doc.Add(NewTextField("repeated", "this is a repeated field - first part", Field.Store
				.YES));
			IIndexableField repeatedField = NewTextField("repeated", "second part of a repeated field"
				, Field.Store.YES);
			doc.Add(repeatedField);
			doc.Add(NewTextField("palindrome", "one two three two one", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewTextField("nonexist", "phrase exist notexist exist found", Field.Store
				.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewTextField("nonexist", "phrase exist notexist exist found", Field.Store
				.YES));
			writer.AddDocument(doc);
			reader = writer.Reader;
			writer.Dispose();
			searcher = NewSearcher(reader);
		}

		private sealed class _Analyzer_61 : Analyzer
		{
			public _Analyzer_61()
			{
			}

			protected override Analyzer.TokenStreamComponents CreateComponents(string fieldName
				, StreamReader reader)
			{
				return new Analyzer.TokenStreamComponents(new MockTokenizer(reader, MockTokenizer
					.WHITESPACE, false));
			}

			public override int GetPositionIncrementGap(string fieldName)
			{
				return 100;
			}
		}

		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			query = new PhraseQuery();
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.AfterClass]
		public static void AfterClass()
		{
			searcher = null;
			reader.Dispose();
			reader = null;
			directory.Dispose();
			directory = null;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestNotCloseEnough()
		{
			query.SetSlop(2);
			query.Add(new Term("field", "one"));
			query.Add(new Term("field", "five"));
			ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
			AreEqual(0, hits.Length);
			QueryUtils.Check(Random(), query, searcher);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestBarelyCloseEnough()
		{
			query.SetSlop(3);
			query.Add(new Term("field", "one"));
			query.Add(new Term("field", "five"));
			ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
			AreEqual(1, hits.Length);
			QueryUtils.Check(Random(), query, searcher);
		}

		/// <summary>Ensures slop of 0 works for exact matches, but not reversed</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestExact()
		{
			// slop is zero by default
			query.Add(new Term("field", "four"));
			query.Add(new Term("field", "five"));
			ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
			AreEqual("exact match", 1, hits.Length);
			QueryUtils.Check(Random(), query, searcher);
			query = new PhraseQuery();
			query.Add(new Term("field", "two"));
			query.Add(new Term("field", "one"));
			hits = searcher.Search(query, null, 1000).ScoreDocs;
			AreEqual("reverse not exact", 0, hits.Length);
			QueryUtils.Check(Random(), query, searcher);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSlop1()
		{
			// Ensures slop of 1 works with terms in order.
			query.SetSlop(1);
			query.Add(new Term("field", "one"));
			query.Add(new Term("field", "two"));
			ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
			AreEqual("in order", 1, hits.Length);
			QueryUtils.Check(Random(), query, searcher);
			// Ensures slop of 1 does not work for phrases out of order;
			// must be at least 2.
			query = new PhraseQuery();
			query.SetSlop(1);
			query.Add(new Term("field", "two"));
			query.Add(new Term("field", "one"));
			hits = searcher.Search(query, null, 1000).ScoreDocs;
			AreEqual("reversed, slop not 2 or more", 0, hits.Length);
			QueryUtils.Check(Random(), query, searcher);
		}

		/// <summary>As long as slop is at least 2, terms can be reversed</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestOrderDoesntMatter()
		{
			query.SetSlop(2);
			// must be at least two for reverse order match
			query.Add(new Term("field", "two"));
			query.Add(new Term("field", "one"));
			ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
			AreEqual("just sloppy enough", 1, hits.Length);
			QueryUtils.Check(Random(), query, searcher);
			query = new PhraseQuery();
			query.SetSlop(2);
			query.Add(new Term("field", "three"));
			query.Add(new Term("field", "one"));
			hits = searcher.Search(query, null, 1000).ScoreDocs;
			AreEqual("not sloppy enough", 0, hits.Length);
			QueryUtils.Check(Random(), query, searcher);
		}

		/// <summary>
		/// slop is the total number of positional moves allowed
		/// to line up a phrase
		/// </summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestMulipleTerms()
		{
			query.SetSlop(2);
			query.Add(new Term("field", "one"));
			query.Add(new Term("field", "three"));
			query.Add(new Term("field", "five"));
			ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
			AreEqual("two total moves", 1, hits.Length);
			QueryUtils.Check(Random(), query, searcher);
			query = new PhraseQuery();
			query.SetSlop(5);
			// it takes six moves to match this phrase
			query.Add(new Term("field", "five"));
			query.Add(new Term("field", "three"));
			query.Add(new Term("field", "one"));
			hits = searcher.Search(query, null, 1000).ScoreDocs;
			AreEqual("slop of 5 not close enough", 0, hits.Length);
			QueryUtils.Check(Random(), query, searcher);
			query.SetSlop(6);
			hits = searcher.Search(query, null, 1000).ScoreDocs;
			AreEqual("slop of 6 just right", 1, hits.Length);
			QueryUtils.Check(Random(), query, searcher);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestPhraseQueryWithStopAnalyzer()
		{
			Directory directory = NewDirectory();
			Analyzer stopAnalyzer = new MockAnalyzer(Random(), MockTokenizer.SIMPLE, true, MockTokenFilter
				.ENGLISH_STOPSET);
			RandomIndexWriter writer = new RandomIndexWriter(Random(), directory, NewIndexWriterConfig
				(TEST_VERSION_CURRENT, stopAnalyzer));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewTextField("field", "the stop words are here", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader reader = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(reader);
			// valid exact phrase query
			PhraseQuery query = new PhraseQuery();
			query.Add(new Term("field", "stop"));
			query.Add(new Term("field", "words"));
			ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
			AreEqual(1, hits.Length);
			QueryUtils.Check(Random(), query, searcher);
			reader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestPhraseQueryInConjunctionScorer()
		{
			Directory directory = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), directory);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewTextField("source", "marketing info", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewTextField("contents", "foobar", Field.Store.YES));
			doc.Add(NewTextField("source", "marketing info", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader reader = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(reader);
			PhraseQuery phraseQuery = new PhraseQuery();
			phraseQuery.Add(new Term("source", "marketing"));
			phraseQuery.Add(new Term("source", "info"));
			ScoreDoc[] hits = searcher.Search(phraseQuery, null, 1000).ScoreDocs;
			AreEqual(2, hits.Length);
			QueryUtils.Check(Random(), phraseQuery, searcher);
			TermQuery termQuery = new TermQuery(new Term("contents", "foobar"));
			BooleanQuery booleanQuery = new BooleanQuery();
			booleanQuery.Add(termQuery, BooleanClause.Occur.MUST);
			booleanQuery.Add(phraseQuery, BooleanClause.Occur.MUST);
			hits = searcher.Search(booleanQuery, null, 1000).ScoreDocs;
			AreEqual(1, hits.Length);
			QueryUtils.Check(Random(), termQuery, searcher);
			reader.Dispose();
			writer = new RandomIndexWriter(Random(), directory, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode.CREATE));
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewTextField("contents", "map entry woo", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewTextField("contents", "woo map entry", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewTextField("contents", "map foobarword entry woo", Field.Store.YES));
			writer.AddDocument(doc);
			reader = writer.Reader;
			writer.Dispose();
			searcher = NewSearcher(reader);
			termQuery = new TermQuery(new Term("contents", "woo"));
			phraseQuery = new PhraseQuery();
			phraseQuery.Add(new Term("contents", "map"));
			phraseQuery.Add(new Term("contents", "entry"));
			hits = searcher.Search(termQuery, null, 1000).ScoreDocs;
			AreEqual(3, hits.Length);
			hits = searcher.Search(phraseQuery, null, 1000).ScoreDocs;
			AreEqual(2, hits.Length);
			booleanQuery = new BooleanQuery();
			booleanQuery.Add(termQuery, BooleanClause.Occur.MUST);
			booleanQuery.Add(phraseQuery, BooleanClause.Occur.MUST);
			hits = searcher.Search(booleanQuery, null, 1000).ScoreDocs;
			AreEqual(2, hits.Length);
			booleanQuery = new BooleanQuery();
			booleanQuery.Add(phraseQuery, BooleanClause.Occur.MUST);
			booleanQuery.Add(termQuery, BooleanClause.Occur.MUST);
			hits = searcher.Search(booleanQuery, null, 1000).ScoreDocs;
			AreEqual(2, hits.Length);
			QueryUtils.Check(Random(), booleanQuery, searcher);
			reader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestSlopScoring()
		{
			Directory directory = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), directory, NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy
				()).SetSimilarity(new DefaultSimilarity()));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewTextField("field", "foo firstname lastname foo", Field.Store.YES));
			writer.AddDocument(doc);
			Lucene.Net.Documents.Document doc2 = new Lucene.Net.Documents.Document
				();
			doc2.Add(NewTextField("field", "foo firstname zzz lastname foo", Field.Store.YES)
				);
			writer.AddDocument(doc2);
			Lucene.Net.Documents.Document doc3 = new Lucene.Net.Documents.Document
				();
			doc3.Add(NewTextField("field", "foo firstname zzz yyy lastname foo", Field.Store.
				YES));
			writer.AddDocument(doc3);
			IndexReader reader = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(reader);
			searcher.SetSimilarity(new DefaultSimilarity());
			PhraseQuery query = new PhraseQuery();
			query.Add(new Term("field", "firstname"));
			query.Add(new Term("field", "lastname"));
			query.SetSlop(int.MaxValue);
			ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
			AreEqual(3, hits.Length);
			// Make sure that those matches where the terms appear closer to
			// each other get a higher score:
			AreEqual(0.71, hits[0].score, 0.01);
			AreEqual(0, hits[0].Doc);
			AreEqual(0.44, hits[1].score, 0.01);
			AreEqual(1, hits[1].Doc);
			AreEqual(0.31, hits[2].score, 0.01);
			AreEqual(2, hits[2].Doc);
			QueryUtils.Check(Random(), query, searcher);
			reader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestToString()
		{
			PhraseQuery q = new PhraseQuery();
			// Query "this hi this is a test is"
			q.Add(new Term("field", "hi"), 1);
			q.Add(new Term("field", "test"), 5);
			AreEqual("field:\"? hi ? ? ? test\"", q.ToString());
			q.Add(new Term("field", "hello"), 1);
			AreEqual("field:\"? hi|hello ? ? ? test\"", q.ToString());
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestWrappedPhrase()
		{
			query.Add(new Term("repeated", "first"));
			query.Add(new Term("repeated", "part"));
			query.Add(new Term("repeated", "second"));
			query.Add(new Term("repeated", "part"));
			query.SetSlop(100);
			ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
			AreEqual("slop of 100 just right", 1, hits.Length);
			QueryUtils.Check(Random(), query, searcher);
			query.SetSlop(99);
			hits = searcher.Search(query, null, 1000).ScoreDocs;
			AreEqual("slop of 99 not enough", 0, hits.Length);
			QueryUtils.Check(Random(), query, searcher);
		}

		// work on two docs like this: "phrase exist notexist exist found"
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestNonExistingPhrase()
		{
			// phrase without repetitions that exists in 2 docs
			query.Add(new Term("nonexist", "phrase"));
			query.Add(new Term("nonexist", "notexist"));
			query.Add(new Term("nonexist", "found"));
			query.SetSlop(2);
			// would be found this way
			ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
			AreEqual("phrase without repetitions exists in 2 docs", 2, 
				hits.Length);
			QueryUtils.Check(Random(), query, searcher);
			// phrase with repetitions that exists in 2 docs
			query = new PhraseQuery();
			query.Add(new Term("nonexist", "phrase"));
			query.Add(new Term("nonexist", "exist"));
			query.Add(new Term("nonexist", "exist"));
			query.SetSlop(1);
			// would be found 
			hits = searcher.Search(query, null, 1000).ScoreDocs;
			AreEqual("phrase with repetitions exists in two docs", 2, 
				hits.Length);
			QueryUtils.Check(Random(), query, searcher);
			// phrase I with repetitions that does not exist in any doc
			query = new PhraseQuery();
			query.Add(new Term("nonexist", "phrase"));
			query.Add(new Term("nonexist", "notexist"));
			query.Add(new Term("nonexist", "phrase"));
			query.SetSlop(1000);
			// would not be found no matter how high the slop is
			hits = searcher.Search(query, null, 1000).ScoreDocs;
			AreEqual("nonexisting phrase with repetitions does not exist in any doc"
				, 0, hits.Length);
			QueryUtils.Check(Random(), query, searcher);
			// phrase II with repetitions that does not exist in any doc
			query = new PhraseQuery();
			query.Add(new Term("nonexist", "phrase"));
			query.Add(new Term("nonexist", "exist"));
			query.Add(new Term("nonexist", "exist"));
			query.Add(new Term("nonexist", "exist"));
			query.SetSlop(1000);
			// would not be found no matter how high the slop is
			hits = searcher.Search(query, null, 1000).ScoreDocs;
			AreEqual("nonexisting phrase with repetitions does not exist in any doc"
				, 0, hits.Length);
			QueryUtils.Check(Random(), query, searcher);
		}

		/// <summary>
		/// Working on a 2 fields like this:
		/// Field("field", "one two three four five")
		/// Field("palindrome", "one two three two one")
		/// Phrase of size 2 occuriong twice, once in order and once in reverse,
		/// because doc is a palyndrome, is counted twice.
		/// </summary>
		/// <remarks>
		/// Working on a 2 fields like this:
		/// Field("field", "one two three four five")
		/// Field("palindrome", "one two three two one")
		/// Phrase of size 2 occuriong twice, once in order and once in reverse,
		/// because doc is a palyndrome, is counted twice.
		/// Also, in this case order in query does not matter.
		/// Also, when an exact match is found, both sloppy scorer and exact scorer scores the same.
		/// </remarks>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestPalyndrome2()
		{
			// search on non palyndrome, find phrase with no slop, using exact phrase scorer
			query.SetSlop(0);
			// to use exact phrase scorer
			query.Add(new Term("field", "two"));
			query.Add(new Term("field", "three"));
			ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
			AreEqual("phrase found with exact phrase scorer", 1, hits.
				Length);
			float score0 = hits[0].score;
			//System.out.println("(exact) field: two three: "+score0);
			QueryUtils.Check(Random(), query, searcher);
			// search on non palyndrome, find phrase with slop 2, though no slop required here.
			query.SetSlop(2);
			// to use sloppy scorer 
			hits = searcher.Search(query, null, 1000).ScoreDocs;
			AreEqual("just sloppy enough", 1, hits.Length);
			float score1 = hits[0].score;
			//System.out.println("(sloppy) field: two three: "+score1);
			AreEqual("exact scorer and sloppy scorer score the same when slop does not matter"
				, score0, score1, SCORE_COMP_THRESH);
			QueryUtils.Check(Random(), query, searcher);
			// search ordered in palyndrome, find it twice
			query = new PhraseQuery();
			query.SetSlop(2);
			// must be at least two for both ordered and reversed to match
			query.Add(new Term("palindrome", "two"));
			query.Add(new Term("palindrome", "three"));
			hits = searcher.Search(query, null, 1000).ScoreDocs;
			AreEqual("just sloppy enough", 1, hits.Length);
			//float score2 = hits[0].score;
			//System.out.println("palindrome: two three: "+score2);
			QueryUtils.Check(Random(), query, searcher);
			//commented out for sloppy-phrase efficiency (issue 736) - see SloppyPhraseScorer.phraseFreq(). 
			//assertTrue("ordered scores higher in palindrome",score1+SCORE_COMP_THRESH<score2);
			// search reveresed in palyndrome, find it twice
			query = new PhraseQuery();
			query.SetSlop(2);
			// must be at least two for both ordered and reversed to match
			query.Add(new Term("palindrome", "three"));
			query.Add(new Term("palindrome", "two"));
			hits = searcher.Search(query, null, 1000).ScoreDocs;
			AreEqual("just sloppy enough", 1, hits.Length);
			//float score3 = hits[0].score;
			//System.out.println("palindrome: three two: "+score3);
			QueryUtils.Check(Random(), query, searcher);
		}

		//commented out for sloppy-phrase efficiency (issue 736) - see SloppyPhraseScorer.phraseFreq(). 
		//assertTrue("reversed scores higher in palindrome",score1+SCORE_COMP_THRESH<score3);
		//assertEquals("ordered or reversed does not matter",score2, score3, SCORE_COMP_THRESH);
		/// <summary>
		/// Working on a 2 fields like this:
		/// Field("field", "one two three four five")
		/// Field("palindrome", "one two three two one")
		/// Phrase of size 3 occuriong twice, once in order and once in reverse,
		/// because doc is a palyndrome, is counted twice.
		/// </summary>
		/// <remarks>
		/// Working on a 2 fields like this:
		/// Field("field", "one two three four five")
		/// Field("palindrome", "one two three two one")
		/// Phrase of size 3 occuriong twice, once in order and once in reverse,
		/// because doc is a palyndrome, is counted twice.
		/// Also, in this case order in query does not matter.
		/// Also, when an exact match is found, both sloppy scorer and exact scorer scores the same.
		/// </remarks>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestPalyndrome3()
		{
			// search on non palyndrome, find phrase with no slop, using exact phrase scorer
			query.SetSlop(0);
			// to use exact phrase scorer
			query.Add(new Term("field", "one"));
			query.Add(new Term("field", "two"));
			query.Add(new Term("field", "three"));
			ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
			AreEqual("phrase found with exact phrase scorer", 1, hits.
				Length);
			float score0 = hits[0].score;
			//System.out.println("(exact) field: one two three: "+score0);
			QueryUtils.Check(Random(), query, searcher);
			// just make sure no exc:
			searcher.Explain(query, 0);
			// search on non palyndrome, find phrase with slop 3, though no slop required here.
			query.SetSlop(4);
			// to use sloppy scorer 
			hits = searcher.Search(query, null, 1000).ScoreDocs;
			AreEqual("just sloppy enough", 1, hits.Length);
			float score1 = hits[0].score;
			//System.out.println("(sloppy) field: one two three: "+score1);
			AreEqual("exact scorer and sloppy scorer score the same when slop does not matter"
				, score0, score1, SCORE_COMP_THRESH);
			QueryUtils.Check(Random(), query, searcher);
			// search ordered in palyndrome, find it twice
			query = new PhraseQuery();
			query.SetSlop(4);
			// must be at least four for both ordered and reversed to match
			query.Add(new Term("palindrome", "one"));
			query.Add(new Term("palindrome", "two"));
			query.Add(new Term("palindrome", "three"));
			hits = searcher.Search(query, null, 1000).ScoreDocs;
			// just make sure no exc:
			searcher.Explain(query, 0);
			AreEqual("just sloppy enough", 1, hits.Length);
			//float score2 = hits[0].score;
			//System.out.println("palindrome: one two three: "+score2);
			QueryUtils.Check(Random(), query, searcher);
			//commented out for sloppy-phrase efficiency (issue 736) - see SloppyPhraseScorer.phraseFreq(). 
			//assertTrue("ordered scores higher in palindrome",score1+SCORE_COMP_THRESH<score2);
			// search reveresed in palyndrome, find it twice
			query = new PhraseQuery();
			query.SetSlop(4);
			// must be at least four for both ordered and reversed to match
			query.Add(new Term("palindrome", "three"));
			query.Add(new Term("palindrome", "two"));
			query.Add(new Term("palindrome", "one"));
			hits = searcher.Search(query, null, 1000).ScoreDocs;
			AreEqual("just sloppy enough", 1, hits.Length);
			//float score3 = hits[0].score;
			//System.out.println("palindrome: three two one: "+score3);
			QueryUtils.Check(Random(), query, searcher);
		}

		//commented out for sloppy-phrase efficiency (issue 736) - see SloppyPhraseScorer.phraseFreq(). 
		//assertTrue("reversed scores higher in palindrome",score1+SCORE_COMP_THRESH<score3);
		//assertEquals("ordered or reversed does not matter",score2, score3, SCORE_COMP_THRESH);
		// LUCENE-1280
		/// <exception cref="System.Exception"></exception>
		public virtual void TestEmptyPhraseQuery()
		{
			BooleanQuery q2 = new BooleanQuery();
			q2.Add(new PhraseQuery(), BooleanClause.Occur.MUST);
			q2.ToString();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestRewrite()
		{
			PhraseQuery pq = new PhraseQuery();
			pq.Add(new Term("foo", "bar"));
			Query rewritten = pq.Rewrite(searcher.IndexReader);
			IsTrue(rewritten is TermQuery);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestRandomPhrases()
		{
			Directory dir = NewDirectory();
			Analyzer analyzer = new MockAnalyzer(Random());
			RandomIndexWriter w = new RandomIndexWriter(Random(), dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, analyzer).SetMergePolicy(NewLogMergePolicy()));
			IList<IList<string>> docs = new List<IList<string>>();
			Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
			Field f = NewTextField("f", string.Empty, Field.Store.NO);
			d.Add(f);
			Random r = Random();
			int NUM_DOCS = AtLeast(10);
			for (int i = 0; i < NUM_DOCS; i++)
			{
				// must be > 4096 so it spans multiple chunks
				int termCount = TestUtil.NextInt(Random(), 4097, 8200);
				IList<string> doc = new List<string>();
				StringBuilder sb = new StringBuilder();
				while (doc.Count < termCount)
				{
					if (r.Next(5) == 1 || docs.Count == 0)
					{
						// make new non-empty-string term
						string term;
						while (true)
						{
							term = TestUtil.RandomUnicodeString(r);
							if (term.Length > 0)
							{
								break;
							}
						}
						CharTermAttribute termAttr = ts.AddAttribute<CharTermAttribute>();
						ts.Reset();
						while (ts.IncrementToken())
						{
							string text = termAttr.ToString();
							doc.Add(text);
							sb.Append(text).Append(' ');
						}
						ts.End();
					}
					else
					{
						// pick existing sub-phrase
						IList<string> lastDoc = docs[r.Next(docs.Count)];
						int len = TestUtil.NextInt(r, 1, 10);
						int start = r.Next(lastDoc.Count - len);
						for (int k = start; k < start + len; k++)
						{
							string t = lastDoc[k];
							doc.Add(t);
							sb.Append(t).Append(' ');
						}
					}
				}
				docs.Add(doc);
				f.StringValue = sb.ToString());
				w.AddDocument(d);
			}
			IndexReader reader = w.Reader;
			IndexSearcher s = NewSearcher(reader);
			w.Dispose();
			// now search
			int num = AtLeast(10);
			for (int i_1 = 0; i_1 < num; i_1++)
			{
				int docID = r.Next(docs.Count);
				IList<string> doc = docs[docID];
				int numTerm = TestUtil.NextInt(r, 2, 20);
				int start = r.Next(doc.Count - numTerm);
				PhraseQuery pq = new PhraseQuery();
				StringBuilder sb = new StringBuilder();
				for (int t = start; t < start + numTerm; t++)
				{
					pq.Add(new Term("f", doc[t]));
					sb.Append(doc[t]).Append(' ');
				}
				TopDocs hits = s.Search(pq, NUM_DOCS);
				bool found = false;
				for (int j = 0; j < hits.ScoreDocs.Length; j++)
				{
					if (hits.ScoreDocs[j].Doc == docID)
					{
						found = true;
						break;
					}
				}
				IsTrue("phrase '" + sb + "' not found; start=" + start, found
					);
			}
			reader.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestNegativeSlop()
		{
			PhraseQuery query = new PhraseQuery();
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
		// expected exception
	}
}
