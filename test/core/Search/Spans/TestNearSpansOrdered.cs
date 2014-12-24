/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Spans;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search.Spans
{
	public class TestNearSpansOrdered : LuceneTestCase
	{
		protected internal IndexSearcher searcher;

		protected internal Directory directory;

		protected internal IndexReader reader;

		public static readonly string FIELD = "field";

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			reader.Close();
			directory.Close();
			base.TearDown();
		}

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
				doc.Add(NewTextField(FIELD, docFields[i], Field.Store.NO));
				writer.AddDocument(doc);
			}
			reader = writer.GetReader();
			writer.Close();
			searcher = NewSearcher(reader);
		}

		protected internal string[] docFields = new string[] { "w1 w2 w3 w4 w5", "w1 w3 w2 w3 zz"
			, "w1 xx w2 yy w3", "w1 w3 xx w2 yy w3 zz" };

		protected internal virtual SpanNearQuery MakeQuery(string s1, string s2, string s3
			, int slop, bool inOrder)
		{
			return new SpanNearQuery(new SpanQuery[] { new SpanTermQuery(new Term(FIELD, s1))
				, new SpanTermQuery(new Term(FIELD, s2)), new SpanTermQuery(new Term(FIELD, s3))
				 }, slop, inOrder);
		}

		protected internal virtual SpanNearQuery MakeQuery()
		{
			return MakeQuery("w1", "w2", "w3", 1, true);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSpanNearQuery()
		{
			SpanNearQuery q = MakeQuery();
			CheckHits.CheckHits(Random(), q, FIELD, searcher, new int[] { 0, 1 });
		}

		public virtual string S(Lucene.Net.Search.Spans.Spans span)
		{
			return S(span.Doc(), span.Start(), span.End());
		}

		public virtual string S(int doc, int start, int end)
		{
			return "s(" + doc + "," + start + "," + end + ")";
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestNearSpansNext()
		{
			SpanNearQuery q = MakeQuery();
			Lucene.Net.Search.Spans.Spans span = MultiSpansWrapper.Wrap(searcher.GetTopReaderContext
				(), q);
			AreEqual(true, span.Next());
			AreEqual(S(0, 0, 3), S(span));
			AreEqual(true, span.Next());
			AreEqual(S(1, 0, 4), S(span));
			AreEqual(false, span.Next());
		}

		/// <summary>
		/// test does not imply that skipTo(doc+1) should work exactly the
		/// same as next -- it's only applicable in this case since we know doc
		/// does not contain more than one span
		/// </summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestNearSpansSkipToLikeNext()
		{
			SpanNearQuery q = MakeQuery();
			Lucene.Net.Search.Spans.Spans span = MultiSpansWrapper.Wrap(searcher.GetTopReaderContext
				(), q);
			AreEqual(true, span.SkipTo(0));
			AreEqual(S(0, 0, 3), S(span));
			AreEqual(true, span.SkipTo(1));
			AreEqual(S(1, 0, 4), S(span));
			AreEqual(false, span.SkipTo(2));
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestNearSpansNextThenSkipTo()
		{
			SpanNearQuery q = MakeQuery();
			Lucene.Net.Search.Spans.Spans span = MultiSpansWrapper.Wrap(searcher.GetTopReaderContext
				(), q);
			AreEqual(true, span.Next());
			AreEqual(S(0, 0, 3), S(span));
			AreEqual(true, span.SkipTo(1));
			AreEqual(S(1, 0, 4), S(span));
			AreEqual(false, span.Next());
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestNearSpansNextThenSkipPast()
		{
			SpanNearQuery q = MakeQuery();
			Lucene.Net.Search.Spans.Spans span = MultiSpansWrapper.Wrap(searcher.GetTopReaderContext
				(), q);
			AreEqual(true, span.Next());
			AreEqual(S(0, 0, 3), S(span));
			AreEqual(false, span.SkipTo(2));
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestNearSpansSkipPast()
		{
			SpanNearQuery q = MakeQuery();
			Lucene.Net.Search.Spans.Spans span = MultiSpansWrapper.Wrap(searcher.GetTopReaderContext
				(), q);
			AreEqual(false, span.SkipTo(2));
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestNearSpansSkipTo0()
		{
			SpanNearQuery q = MakeQuery();
			Lucene.Net.Search.Spans.Spans span = MultiSpansWrapper.Wrap(searcher.GetTopReaderContext
				(), q);
			AreEqual(true, span.SkipTo(0));
			AreEqual(S(0, 0, 3), S(span));
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestNearSpansSkipTo1()
		{
			SpanNearQuery q = MakeQuery();
			Lucene.Net.Search.Spans.Spans span = MultiSpansWrapper.Wrap(searcher.GetTopReaderContext
				(), q);
			AreEqual(true, span.SkipTo(1));
			AreEqual(S(1, 0, 4), S(span));
		}

		/// <summary>
		/// not a direct test of NearSpans, but a demonstration of how/when
		/// this causes problems
		/// </summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestSpanNearScorerSkipTo1()
		{
			SpanNearQuery q = MakeQuery();
			Weight w = searcher.CreateNormalizedWeight(q);
			IndexReaderContext topReaderContext = searcher.GetTopReaderContext();
			AtomicReaderContext leave = topReaderContext.Leaves()[0];
			Scorer s = w.Scorer(leave, ((AtomicReader)leave.Reader()).GetLiveDocs());
			AreEqual(1, s.Advance(1));
		}

		/// <summary>
		/// not a direct test of NearSpans, but a demonstration of how/when
		/// this causes problems
		/// </summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestSpanNearScorerExplain()
		{
			SpanNearQuery q = MakeQuery();
			Explanation e = searcher.Explain(q, 1);
			IsTrue("Scorer explanation value for doc#1 isn't positive: "
				 + e.ToString(), 0.0f < e.GetValue());
		}
	}
}
