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
using Lucene.Net.Search.Similarities;
using Lucene.Net.Search.Spans;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search.Spans
{
	public class TestSpans : LuceneTestCase
	{
		private IndexSearcher searcher;

		private IndexReader reader;

		private Directory directory;

		public static readonly string field = "field";

		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			directory = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), directory, NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy
				()));
			for (int i = 0; i < docFields.Length; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(NewTextField(field, docFields[i], Field.Store.YES));
				writer.AddDocument(doc);
			}
			reader = writer.GetReader();
			writer.Dispose();
			searcher = NewSearcher(reader);
		}

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			reader.Dispose();
			directory.Dispose();
			base.TearDown();
		}

		private string[] docFields = new string[] { "w1 w2 w3 w4 w5", "w1 w3 w2 w3", "w1 xx w2 yy w3"
			, "w1 w3 xx w2 yy w3", "u2 u2 u1", "u2 xx u2 u1", "u2 u2 xx u1", "u2 xx u2 yy u1"
			, "u2 xx u1 u2", "u2 u1 xx u2", "u1 u2 xx u2", "t1 t2 t1 t3 t2 t3", "s2 s1 s1 xx xx s2 xx s2 xx s1 xx xx xx xx xx s2 xx"
			 };

		public virtual SpanTermQuery MakeSpanTermQuery(string text)
		{
			return new SpanTermQuery(new Term(field, text));
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void CheckHits(Query query, int[] results)
		{
			Lucene.Net.Search.CheckHits.CheckHits(Random(), query, field, searcher, results
				);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void OrderedSlopTest3SQ(SpanQuery q1, SpanQuery q2, SpanQuery q3, int slop
			, int[] expectedDocs)
		{
			bool ordered = true;
			SpanNearQuery snq = new SpanNearQuery(new SpanQuery[] { q1, q2, q3 }, slop, ordered
				);
			CheckHits(snq, expectedDocs);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void OrderedSlopTest3(int slop, int[] expectedDocs)
		{
			OrderedSlopTest3SQ(MakeSpanTermQuery("w1"), MakeSpanTermQuery("w2"), MakeSpanTermQuery
				("w3"), slop, expectedDocs);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void OrderedSlopTest3Equal(int slop, int[] expectedDocs)
		{
			OrderedSlopTest3SQ(MakeSpanTermQuery("w1"), MakeSpanTermQuery("w3"), MakeSpanTermQuery
				("w3"), slop, expectedDocs);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void OrderedSlopTest1Equal(int slop, int[] expectedDocs)
		{
			OrderedSlopTest3SQ(MakeSpanTermQuery("u2"), MakeSpanTermQuery("u2"), MakeSpanTermQuery
				("u1"), slop, expectedDocs);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSpanNearOrdered01()
		{
			OrderedSlopTest3(0, new int[] { 0 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSpanNearOrdered02()
		{
			OrderedSlopTest3(1, new int[] { 0, 1 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSpanNearOrdered03()
		{
			OrderedSlopTest3(2, new int[] { 0, 1, 2 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSpanNearOrdered04()
		{
			OrderedSlopTest3(3, new int[] { 0, 1, 2, 3 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSpanNearOrdered05()
		{
			OrderedSlopTest3(4, new int[] { 0, 1, 2, 3 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSpanNearOrderedEqual01()
		{
			OrderedSlopTest3Equal(0, new int[] {  });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSpanNearOrderedEqual02()
		{
			OrderedSlopTest3Equal(1, new int[] { 1 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSpanNearOrderedEqual03()
		{
			OrderedSlopTest3Equal(2, new int[] { 1 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSpanNearOrderedEqual04()
		{
			OrderedSlopTest3Equal(3, new int[] { 1, 3 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSpanNearOrderedEqual11()
		{
			OrderedSlopTest1Equal(0, new int[] { 4 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSpanNearOrderedEqual12()
		{
			OrderedSlopTest1Equal(0, new int[] { 4 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSpanNearOrderedEqual13()
		{
			OrderedSlopTest1Equal(1, new int[] { 4, 5, 6 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSpanNearOrderedEqual14()
		{
			OrderedSlopTest1Equal(2, new int[] { 4, 5, 6, 7 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSpanNearOrderedEqual15()
		{
			OrderedSlopTest1Equal(3, new int[] { 4, 5, 6, 7 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSpanNearOrderedOverlap()
		{
			bool ordered = true;
			int slop = 1;
			SpanNearQuery snq = new SpanNearQuery(new SpanQuery[] { MakeSpanTermQuery("t1"), 
				MakeSpanTermQuery("t2"), MakeSpanTermQuery("t3") }, slop, ordered);
			Lucene.Net.Search.Spans.Spans spans = MultiSpansWrapper.Wrap(searcher.GetTopReaderContext
				(), snq);
			IsTrue("first range", spans.Next());
			AreEqual("first doc", 11, spans.Doc());
			AreEqual("first start", 0, spans.Start());
			AreEqual("first end", 4, spans.End());
			IsTrue("second range", spans.Next());
			AreEqual("second doc", 11, spans.Doc());
			AreEqual("second start", 2, spans.Start());
			AreEqual("second end", 6, spans.End());
			IsFalse("third range", spans.Next());
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSpanNearUnOrdered()
		{
			//See http://www.gossamer-threads.com/lists/lucene/java-dev/52270 for discussion about this test
			SpanNearQuery snq;
			snq = new SpanNearQuery(new SpanQuery[] { MakeSpanTermQuery("u1"), MakeSpanTermQuery
				("u2") }, 0, false);
			Lucene.Net.Search.Spans.Spans spans = MultiSpansWrapper.Wrap(searcher.GetTopReaderContext
				(), snq);
			IsTrue("Does not have next and it should", spans.Next());
			AreEqual("doc", 4, spans.Doc());
			AreEqual("start", 1, spans.Start());
			AreEqual("end", 3, spans.End());
			IsTrue("Does not have next and it should", spans.Next());
			AreEqual("doc", 5, spans.Doc());
			AreEqual("start", 2, spans.Start());
			AreEqual("end", 4, spans.End());
			IsTrue("Does not have next and it should", spans.Next());
			AreEqual("doc", 8, spans.Doc());
			AreEqual("start", 2, spans.Start());
			AreEqual("end", 4, spans.End());
			IsTrue("Does not have next and it should", spans.Next());
			AreEqual("doc", 9, spans.Doc());
			AreEqual("start", 0, spans.Start());
			AreEqual("end", 2, spans.End());
			IsTrue("Does not have next and it should", spans.Next());
			AreEqual("doc", 10, spans.Doc());
			AreEqual("start", 0, spans.Start());
			AreEqual("end", 2, spans.End());
			IsTrue("Has next and it shouldn't: " + spans.Doc(), spans.
				Next() == false);
			SpanNearQuery u1u2 = new SpanNearQuery(new SpanQuery[] { MakeSpanTermQuery("u1"), 
				MakeSpanTermQuery("u2") }, 0, false);
			snq = new SpanNearQuery(new SpanQuery[] { u1u2, MakeSpanTermQuery("u2") }, 1, false
				);
			spans = MultiSpansWrapper.Wrap(searcher.GetTopReaderContext(), snq);
			IsTrue("Does not have next and it should", spans.Next());
			AreEqual("doc", 4, spans.Doc());
			AreEqual("start", 0, spans.Start());
			AreEqual("end", 3, spans.End());
			IsTrue("Does not have next and it should", spans.Next());
			//unordered spans can be subsets
			AreEqual("doc", 4, spans.Doc());
			AreEqual("start", 1, spans.Start());
			AreEqual("end", 3, spans.End());
			IsTrue("Does not have next and it should", spans.Next());
			AreEqual("doc", 5, spans.Doc());
			AreEqual("start", 0, spans.Start());
			AreEqual("end", 4, spans.End());
			IsTrue("Does not have next and it should", spans.Next());
			AreEqual("doc", 5, spans.Doc());
			AreEqual("start", 2, spans.Start());
			AreEqual("end", 4, spans.End());
			IsTrue("Does not have next and it should", spans.Next());
			AreEqual("doc", 8, spans.Doc());
			AreEqual("start", 0, spans.Start());
			AreEqual("end", 4, spans.End());
			IsTrue("Does not have next and it should", spans.Next());
			AreEqual("doc", 8, spans.Doc());
			AreEqual("start", 2, spans.Start());
			AreEqual("end", 4, spans.End());
			IsTrue("Does not have next and it should", spans.Next());
			AreEqual("doc", 9, spans.Doc());
			AreEqual("start", 0, spans.Start());
			AreEqual("end", 2, spans.End());
			IsTrue("Does not have next and it should", spans.Next());
			AreEqual("doc", 9, spans.Doc());
			AreEqual("start", 0, spans.Start());
			AreEqual("end", 4, spans.End());
			IsTrue("Does not have next and it should", spans.Next());
			AreEqual("doc", 10, spans.Doc());
			AreEqual("start", 0, spans.Start());
			AreEqual("end", 2, spans.End());
			IsTrue("Has next and it shouldn't", spans.Next() == false);
		}

		/// <exception cref="System.Exception"></exception>
		private Lucene.Net.Search.Spans.Spans OrSpans(string[] terms)
		{
			SpanQuery[] sqa = new SpanQuery[terms.Length];
			for (int i = 0; i < terms.Length; i++)
			{
				sqa[i] = MakeSpanTermQuery(terms[i]);
			}
			return MultiSpansWrapper.Wrap(searcher.GetTopReaderContext(), new SpanOrQuery(sqa
				));
		}

		/// <exception cref="System.Exception"></exception>
		private void TstNextSpans(Lucene.Net.Search.Spans.Spans spans, int doc, int
			 start, int end)
		{
			IsTrue("next", spans.Next());
			AreEqual("doc", doc, spans.Doc());
			AreEqual("start", start, spans.Start());
			AreEqual("end", end, spans.End());
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSpanOrEmpty()
		{
			Lucene.Net.Search.Spans.Spans spans = OrSpans(new string[0]);
			IsFalse("empty next", spans.Next());
			SpanOrQuery a = new SpanOrQuery();
			SpanOrQuery b = new SpanOrQuery();
			IsTrue("empty should equal", a.Equals(b));
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSpanOrSingle()
		{
			Lucene.Net.Search.Spans.Spans spans = OrSpans(new string[] { "w5" });
			TstNextSpans(spans, 0, 4, 5);
			IsFalse("final next", spans.Next());
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSpanOrMovesForward()
		{
			Lucene.Net.Search.Spans.Spans spans = OrSpans(new string[] { "w1", "xx" });
			spans.Next();
			int doc = spans.Doc();
			AreEqual(0, doc);
			spans.SkipTo(0);
			doc = spans.Doc();
			// LUCENE-1583:
			// according to Spans, a skipTo to the same doc or less
			// should still call next() on the underlying Spans
			AreEqual(1, doc);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSpanOrDouble()
		{
			Lucene.Net.Search.Spans.Spans spans = OrSpans(new string[] { "w5", "yy" });
			TstNextSpans(spans, 0, 4, 5);
			TstNextSpans(spans, 2, 3, 4);
			TstNextSpans(spans, 3, 4, 5);
			TstNextSpans(spans, 7, 3, 4);
			IsFalse("final next", spans.Next());
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSpanOrDoubleSkip()
		{
			Lucene.Net.Search.Spans.Spans spans = OrSpans(new string[] { "w5", "yy" });
			IsTrue("initial skipTo", spans.SkipTo(3));
			AreEqual("doc", 3, spans.Doc());
			AreEqual("start", 4, spans.Start());
			AreEqual("end", 5, spans.End());
			TstNextSpans(spans, 7, 3, 4);
			IsFalse("final next", spans.Next());
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSpanOrUnused()
		{
			Lucene.Net.Search.Spans.Spans spans = OrSpans(new string[] { "w5", "unusedTerm"
				, "yy" });
			TstNextSpans(spans, 0, 4, 5);
			TstNextSpans(spans, 2, 3, 4);
			TstNextSpans(spans, 3, 4, 5);
			TstNextSpans(spans, 7, 3, 4);
			IsFalse("final next", spans.Next());
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSpanOrTripleSameDoc()
		{
			Lucene.Net.Search.Spans.Spans spans = OrSpans(new string[] { "t1", "t2", "t3"
				 });
			TstNextSpans(spans, 11, 0, 1);
			TstNextSpans(spans, 11, 1, 2);
			TstNextSpans(spans, 11, 2, 3);
			TstNextSpans(spans, 11, 3, 4);
			TstNextSpans(spans, 11, 4, 5);
			TstNextSpans(spans, 11, 5, 6);
			IsFalse("final next", spans.Next());
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSpanScorerZeroSloppyFreq()
		{
			bool ordered = true;
			int slop = 1;
			IndexReaderContext topReaderContext = searcher.GetTopReaderContext();
			IList<AtomicReaderContext> leaves = topReaderContext.Leaves;
			int subIndex = ReaderUtil.SubIndex(11, leaves);
			for (int i = 0; i < c; i++)
			{
				AtomicReaderContext ctx = leaves[i];
				Similarity sim = new _DefaultSimilarity_414();
				Similarity oldSim = searcher.GetSimilarity();
				Scorer spanScorer;
				try
				{
					searcher.SetSimilarity(sim);
					SpanNearQuery snq = new SpanNearQuery(new SpanQuery[] { MakeSpanTermQuery("t1"), 
						MakeSpanTermQuery("t2") }, slop, ordered);
					spanScorer = searcher.CreateNormalizedWeight(snq).Scorer(ctx, ((AtomicReader)ctx.
						Reader()).LiveDocs);
				}
				finally
				{
					searcher.SetSimilarity(oldSim);
				}
				if (i == subIndex)
				{
					IsTrue("first doc", spanScorer.NextDoc() != DocIdSetIterator
						.NO_MORE_DOCS);
					AreEqual("first doc number", spanScorer.DocID + ctx.docBase
						, 11);
					float score = spanScorer.Score();
					IsTrue("first doc score should be zero, " + score, score ==
						 0.0f);
				}
				else
				{
					IsTrue("no second doc", spanScorer.NextDoc() == DocIdSetIterator
						.NO_MORE_DOCS);
				}
			}
		}

		private sealed class _DefaultSimilarity_414 : DefaultSimilarity
		{
			public _DefaultSimilarity_414()
			{
			}

			public override float SloppyFreq(int distance)
			{
				return 0.0f;
			}
		}

		// LUCENE-1404
		/// <exception cref="System.IO.IOException"></exception>
		private void AddDoc(IndexWriter writer, string id, string text)
		{
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewStringField("id", id, Field.Store.YES));
			doc.Add(NewTextField("text", text, Field.Store.YES));
			writer.AddDocument(doc);
		}

		// LUCENE-1404
		/// <exception cref="System.Exception"></exception>
		private int HitCount(IndexSearcher searcher, string word)
		{
			return searcher.Search(new TermQuery(new Term("text", word)), 10).TotalHits;
		}

		// LUCENE-1404
		private SpanQuery CreateSpan(string value)
		{
			return new SpanTermQuery(new Term("text", value));
		}

		// LUCENE-1404
		private SpanQuery CreateSpan(int slop, bool ordered, SpanQuery[] clauses)
		{
			return new SpanNearQuery(clauses, slop, ordered);
		}

		// LUCENE-1404
		private SpanQuery CreateSpan(int slop, bool ordered, string term1, string term2)
		{
			return CreateSpan(slop, ordered, new SpanQuery[] { CreateSpan(term1), CreateSpan(
				term2) });
		}

		// LUCENE-1404
		/// <exception cref="System.Exception"></exception>
		public virtual void TestNPESpanQuery()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			// Add documents
			AddDoc(writer, "1", "the big dogs went running to the market");
			AddDoc(writer, "2", "the cat chased the mouse, then the cat ate the mouse quickly"
				);
			// Commit
			writer.Dispose();
			// Get searcher
			IndexReader reader = DirectoryReader.Open(dir);
			IndexSearcher searcher = NewSearcher(reader);
			// Control (make sure docs indexed)
			AreEqual(2, HitCount(searcher, "the"));
			AreEqual(1, HitCount(searcher, "cat"));
			AreEqual(1, HitCount(searcher, "dogs"));
			AreEqual(0, HitCount(searcher, "rabbit"));
			// This throws exception (it shouldn't)
			AreEqual(1, searcher.Search(CreateSpan(0, true, new SpanQuery
				[] { CreateSpan(4, false, "chased", "cat"), CreateSpan("ate") }), 10).TotalHits);
			reader.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSpanNots()
		{
			AreEqual("SpanNotIncludeExcludeSame1", 0, SpanCount("s2", 
				"s2", 0, 0), 0);
			AreEqual("SpanNotIncludeExcludeSame2", 0, SpanCount("s2", 
				"s2", 10, 10), 0);
			//focus on behind
			AreEqual("SpanNotS2NotS1_6_0", 1, SpanCount("s2", "s1", 6, 
				0));
			AreEqual("SpanNotS2NotS1_5_0", 2, SpanCount("s2", "s1", 5, 
				0));
			AreEqual("SpanNotS2NotS1_3_0", 3, SpanCount("s2", "s1", 3, 
				0));
			AreEqual("SpanNotS2NotS1_2_0", 4, SpanCount("s2", "s1", 2, 
				0));
			AreEqual("SpanNotS2NotS1_0_0", 4, SpanCount("s2", "s1", 0, 
				0));
			//focus on both
			AreEqual("SpanNotS2NotS1_3_1", 2, SpanCount("s2", "s1", 3, 
				1));
			AreEqual("SpanNotS2NotS1_2_1", 3, SpanCount("s2", "s1", 2, 
				1));
			AreEqual("SpanNotS2NotS1_1_1", 3, SpanCount("s2", "s1", 1, 
				1));
			AreEqual("SpanNotS2NotS1_10_10", 0, SpanCount("s2", "s1", 
				10, 10));
			//focus on ahead
			AreEqual("SpanNotS1NotS2_10_10", 0, SpanCount("s1", "s2", 
				10, 10));
			AreEqual("SpanNotS1NotS2_0_1", 3, SpanCount("s1", "s2", 0, 
				1));
			AreEqual("SpanNotS1NotS2_0_2", 3, SpanCount("s1", "s2", 0, 
				2));
			AreEqual("SpanNotS1NotS2_0_3", 2, SpanCount("s1", "s2", 0, 
				3));
			AreEqual("SpanNotS1NotS2_0_4", 1, SpanCount("s1", "s2", 0, 
				4));
			AreEqual("SpanNotS1NotS2_0_8", 0, SpanCount("s1", "s2", 0, 
				8));
			//exclude doesn't exist
			AreEqual("SpanNotS1NotS3_8_8", 3, SpanCount("s1", "s3", 8, 
				8));
			//include doesn't exist
			AreEqual("SpanNotS3NotS1_8_8", 0, SpanCount("s3", "s1", 8, 
				8));
		}

		/// <exception cref="System.IO.IOException"></exception>
		private int SpanCount(string include, string exclude, int pre, int post)
		{
			SpanTermQuery iq = new SpanTermQuery(new Term(field, include));
			SpanTermQuery eq = new SpanTermQuery(new Term(field, exclude));
			SpanNotQuery snq = new SpanNotQuery(iq, eq, pre, post);
			Lucene.Net.Search.Spans.Spans spans = MultiSpansWrapper.Wrap(searcher.GetTopReaderContext
				(), snq);
			int i = 0;
			while (spans.Next())
			{
				i++;
			}
			return i;
		}
	}
}
