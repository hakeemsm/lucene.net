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
	public class TestFieldMaskingSpanQuery : LuceneTestCase
	{
		protected internal static Lucene.Net.Documents.Document Doc(Lucene.Net.Document.Field
			[] fields)
		{
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			for (int i = 0; i < fields.Length; i++)
			{
				doc.Add(fields[i]);
			}
			return doc;
		}

		protected internal static Lucene.Net.Document.Field Field(string name, string
			 value)
		{
			return NewTextField(name, value, Field.Store.NO);
		}

		protected internal static IndexSearcher searcher;

		protected internal static Directory directory;

		protected internal static IndexReader reader;

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.BeforeClass]
		public static void BeforeClass()
		{
			directory = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), directory, NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy
				()));
			writer.AddDocument(Doc(new Lucene.Net.Document.Field[] { Field("id", "0"), 
				Field("gender", "male"), Field("first", "james"), Field("last", "jones") }));
			writer.AddDocument(Doc(new Lucene.Net.Document.Field[] { Field("id", "1"), 
				Field("gender", "male"), Field("first", "james"), Field("last", "smith"), Field(
				"gender", "female"), Field("first", "sally"), Field("last", "jones") }));
			writer.AddDocument(Doc(new Lucene.Net.Document.Field[] { Field("id", "2"), 
				Field("gender", "female"), Field("first", "greta"), Field("last", "jones"), Field
				("gender", "female"), Field("first", "sally"), Field("last", "smith"), Field("gender"
				, "male"), Field("first", "james"), Field("last", "jones") }));
			writer.AddDocument(Doc(new Lucene.Net.Document.Field[] { Field("id", "3"), 
				Field("gender", "female"), Field("first", "lisa"), Field("last", "jones"), Field
				("gender", "male"), Field("first", "bob"), Field("last", "costas") }));
			writer.AddDocument(Doc(new Lucene.Net.Document.Field[] { Field("id", "4"), 
				Field("gender", "female"), Field("first", "sally"), Field("last", "smith"), Field
				("gender", "female"), Field("first", "linda"), Field("last", "dixit"), Field("gender"
				, "male"), Field("first", "bubba"), Field("last", "jones") }));
			reader = writer.GetReader();
			writer.Dispose();
			searcher = NewSearcher(reader);
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
		protected internal virtual void Check(SpanQuery q, int[] docs)
		{
			CheckHits.CheckHitCollector(Random(), q, null, searcher, docs);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestRewrite0()
		{
			SpanQuery q = new FieldMaskingSpanQuery(new SpanTermQuery(new Term("last", "sally"
				)), "first");
			q.SetBoost(8.7654321f);
			SpanQuery qr = (SpanQuery)searcher.Rewrite(q);
			QueryUtils.CheckEqual(q, qr);
			ICollection<Term> terms = new HashSet<Term>();
			qr.ExtractTerms(terms);
			AreEqual(1, terms.Count);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestRewrite1()
		{
			// mask an anon SpanQuery class that rewrites to something else.
			SpanQuery q = new FieldMaskingSpanQuery(new _SpanTermQuery_149(new Term("last", "sally"
				)), "first");
			SpanQuery qr = (SpanQuery)searcher.Rewrite(q);
			QueryUtils.CheckUnequal(q, qr);
			ICollection<Term> terms = new HashSet<Term>();
			qr.ExtractTerms(terms);
			AreEqual(2, terms.Count);
		}

		private sealed class _SpanTermQuery_149 : SpanTermQuery
		{
			public _SpanTermQuery_149(Term baseArg1) : base(baseArg1)
			{
			}

			public override Query Rewrite(IndexReader reader)
			{
				return new SpanOrQuery(new SpanTermQuery(new Term("first", "sally")), new SpanTermQuery
					(new Term("first", "james")));
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestRewrite2()
		{
			SpanQuery q1 = new SpanTermQuery(new Term("last", "smith"));
			SpanQuery q2 = new SpanTermQuery(new Term("last", "jones"));
			SpanQuery q = new SpanNearQuery(new SpanQuery[] { q1, new FieldMaskingSpanQuery(q2
				, "last") }, 1, true);
			Query qr = searcher.Rewrite(q);
			QueryUtils.CheckEqual(q, qr);
			HashSet<Term> set = new HashSet<Term>();
			qr.ExtractTerms(set);
			AreEqual(2, set.Count);
		}

		public virtual void TestEquality1()
		{
			SpanQuery q1 = new FieldMaskingSpanQuery(new SpanTermQuery(new Term("last", "sally"
				)), "first");
			SpanQuery q2 = new FieldMaskingSpanQuery(new SpanTermQuery(new Term("last", "sally"
				)), "first");
			SpanQuery q3 = new FieldMaskingSpanQuery(new SpanTermQuery(new Term("last", "sally"
				)), "XXXXX");
			SpanQuery q4 = new FieldMaskingSpanQuery(new SpanTermQuery(new Term("last", "XXXXX"
				)), "first");
			SpanQuery q5 = new FieldMaskingSpanQuery(new SpanTermQuery(new Term("xXXX", "sally"
				)), "first");
			QueryUtils.CheckEqual(q1, q2);
			QueryUtils.CheckUnequal(q1, q3);
			QueryUtils.CheckUnequal(q1, q4);
			QueryUtils.CheckUnequal(q1, q5);
			SpanQuery qA = new FieldMaskingSpanQuery(new SpanTermQuery(new Term("last", "sally"
				)), "first");
			qA.SetBoost(9f);
			SpanQuery qB = new FieldMaskingSpanQuery(new SpanTermQuery(new Term("last", "sally"
				)), "first");
			QueryUtils.CheckUnequal(qA, qB);
			qB.SetBoost(9f);
			QueryUtils.CheckEqual(qA, qB);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestNoop0()
		{
			SpanQuery q1 = new SpanTermQuery(new Term("last", "sally"));
			SpanQuery q = new FieldMaskingSpanQuery(q1, "first");
			Check(q, new int[] {  });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestNoop1()
		{
			SpanQuery q1 = new SpanTermQuery(new Term("last", "smith"));
			SpanQuery q2 = new SpanTermQuery(new Term("last", "jones"));
			SpanQuery q = new SpanNearQuery(new SpanQuery[] { q1, new FieldMaskingSpanQuery(q2
				, "last") }, 0, true);
			Check(q, new int[] { 1, 2 });
			q = new SpanNearQuery(new SpanQuery[] { new FieldMaskingSpanQuery(q1, "last"), new 
				FieldMaskingSpanQuery(q2, "last") }, 0, true);
			Check(q, new int[] { 1, 2 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSimple1()
		{
			SpanQuery q1 = new SpanTermQuery(new Term("first", "james"));
			SpanQuery q2 = new SpanTermQuery(new Term("last", "jones"));
			SpanQuery q = new SpanNearQuery(new SpanQuery[] { q1, new FieldMaskingSpanQuery(q2
				, "first") }, -1, false);
			Check(q, new int[] { 0, 2 });
			q = new SpanNearQuery(new SpanQuery[] { new FieldMaskingSpanQuery(q2, "first"), q1
				 }, -1, false);
			Check(q, new int[] { 0, 2 });
			q = new SpanNearQuery(new SpanQuery[] { q2, new FieldMaskingSpanQuery(q1, "last")
				 }, -1, false);
			Check(q, new int[] { 0, 2 });
			q = new SpanNearQuery(new SpanQuery[] { new FieldMaskingSpanQuery(q1, "last"), q2
				 }, -1, false);
			Check(q, new int[] { 0, 2 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSimple2()
		{
			AssumeTrue("Broken scoring: LUCENE-3723", searcher.GetSimilarity() is TFIDFSimilarity
				);
			SpanQuery q1 = new SpanTermQuery(new Term("gender", "female"));
			SpanQuery q2 = new SpanTermQuery(new Term("last", "smith"));
			SpanQuery q = new SpanNearQuery(new SpanQuery[] { q1, new FieldMaskingSpanQuery(q2
				, "gender") }, -1, false);
			Check(q, new int[] { 2, 4 });
			q = new SpanNearQuery(new SpanQuery[] { new FieldMaskingSpanQuery(q1, "id"), new 
				FieldMaskingSpanQuery(q2, "id") }, -1, false);
			Check(q, new int[] { 2, 4 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSpans0()
		{
			SpanQuery q1 = new SpanTermQuery(new Term("gender", "female"));
			SpanQuery q2 = new SpanTermQuery(new Term("first", "james"));
			SpanQuery q = new SpanOrQuery(q1, new FieldMaskingSpanQuery(q2, "gender"));
			Check(q, new int[] { 0, 1, 2, 3, 4 });
			Lucene.Net.Search.Spans.Spans span = MultiSpansWrapper.Wrap(searcher.GetTopReaderContext
				(), q);
			AreEqual(true, span.Next());
			AreEqual(S(0, 0, 1), S(span));
			AreEqual(true, span.Next());
			AreEqual(S(1, 0, 1), S(span));
			AreEqual(true, span.Next());
			AreEqual(S(1, 1, 2), S(span));
			AreEqual(true, span.Next());
			AreEqual(S(2, 0, 1), S(span));
			AreEqual(true, span.Next());
			AreEqual(S(2, 1, 2), S(span));
			AreEqual(true, span.Next());
			AreEqual(S(2, 2, 3), S(span));
			AreEqual(true, span.Next());
			AreEqual(S(3, 0, 1), S(span));
			AreEqual(true, span.Next());
			AreEqual(S(4, 0, 1), S(span));
			AreEqual(true, span.Next());
			AreEqual(S(4, 1, 2), S(span));
			AreEqual(false, span.Next());
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSpans1()
		{
			SpanQuery q1 = new SpanTermQuery(new Term("first", "sally"));
			SpanQuery q2 = new SpanTermQuery(new Term("first", "james"));
			SpanQuery qA = new SpanOrQuery(q1, q2);
			SpanQuery qB = new FieldMaskingSpanQuery(qA, "id");
			Check(qA, new int[] { 0, 1, 2, 4 });
			Check(qB, new int[] { 0, 1, 2, 4 });
			Lucene.Net.Search.Spans.Spans spanA = MultiSpansWrapper.Wrap(searcher.GetTopReaderContext
				(), qA);
			Lucene.Net.Search.Spans.Spans spanB = MultiSpansWrapper.Wrap(searcher.GetTopReaderContext
				(), qB);
			while (spanA.Next())
			{
				IsTrue("spanB not still going", spanB.Next());
				AreEqual("spanA not equal spanB", S(spanA), S(spanB));
			}
			IsTrue("spanB still going even tough spanA is done", !(spanB
				.Next()));
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSpans2()
		{
			AssumeTrue("Broken scoring: LUCENE-3723", searcher.GetSimilarity() is TFIDFSimilarity
				);
			SpanQuery qA1 = new SpanTermQuery(new Term("gender", "female"));
			SpanQuery qA2 = new SpanTermQuery(new Term("first", "james"));
			SpanQuery qA = new SpanOrQuery(qA1, new FieldMaskingSpanQuery(qA2, "gender"));
			SpanQuery qB = new SpanTermQuery(new Term("last", "jones"));
			SpanQuery q = new SpanNearQuery(new SpanQuery[] { new FieldMaskingSpanQuery(qA, "id"
				), new FieldMaskingSpanQuery(qB, "id") }, -1, false);
			Check(q, new int[] { 0, 1, 2, 3 });
			Lucene.Net.Search.Spans.Spans span = MultiSpansWrapper.Wrap(searcher.GetTopReaderContext
				(), q);
			AreEqual(true, span.Next());
			AreEqual(S(0, 0, 1), S(span));
			AreEqual(true, span.Next());
			AreEqual(S(1, 1, 2), S(span));
			AreEqual(true, span.Next());
			AreEqual(S(2, 0, 1), S(span));
			AreEqual(true, span.Next());
			AreEqual(S(2, 2, 3), S(span));
			AreEqual(true, span.Next());
			AreEqual(S(3, 0, 1), S(span));
			AreEqual(false, span.Next());
		}

		public virtual string S(Lucene.Net.Search.Spans.Spans span)
		{
			return S(span.Doc(), span.Start(), span.End());
		}

		public virtual string S(int doc, int start, int end)
		{
			return "s(" + doc + "," + start + "," + end + ")";
		}
	}
}
