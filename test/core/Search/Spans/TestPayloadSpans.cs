/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using System.IO;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Test.Analysis.Tokenattributes;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Payloads;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Search.Spans;
using Lucene.Net.Store;
using Lucene.Net.Util;


namespace Lucene.Net.Search.Spans
{
	public class TestPayloadSpans : LuceneTestCase
	{
		private IndexSearcher searcher;

		private Similarity similarity = new DefaultSimilarity();

		protected internal IndexReader indexReader;

		private IndexReader closeIndexReader;

		private Directory directory;

		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			PayloadHelper helper = new PayloadHelper();
			searcher = helper.SetUp(Random(), similarity, 1000);
			indexReader = searcher.IndexReader;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSpanTermQuery()
		{
			SpanTermQuery stq;
			Lucene.Net.Search.Spans.Spans spans;
			stq = new SpanTermQuery(new Term(PayloadHelper.FIELD, "seventy"));
			spans = MultiSpansWrapper.Wrap(indexReader.GetContext(), stq);
			IsTrue("spans is null and it shouldn't be", spans != null);
			CheckSpans(spans, 100, 1, 1, 1);
			stq = new SpanTermQuery(new Term(PayloadHelper.NO_PAYLOAD_FIELD, "seventy"));
			spans = MultiSpansWrapper.Wrap(indexReader.GetContext(), stq);
			IsTrue("spans is null and it shouldn't be", spans != null);
			CheckSpans(spans, 100, 0, 0, 0);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestSpanFirst()
		{
			SpanQuery match;
			SpanFirstQuery sfq;
			match = new SpanTermQuery(new Term(PayloadHelper.FIELD, "one"));
			sfq = new SpanFirstQuery(match, 2);
			Lucene.Net.Search.Spans.Spans spans = MultiSpansWrapper.Wrap(indexReader.GetContext
				(), sfq);
			CheckSpans(spans, 109, 1, 1, 1);
			//Test more complicated subclause
			SpanQuery[] clauses = new SpanQuery[2];
			clauses[0] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "one"));
			clauses[1] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "hundred"));
			match = new SpanNearQuery(clauses, 0, true);
			sfq = new SpanFirstQuery(match, 2);
			CheckSpans(MultiSpansWrapper.Wrap(indexReader.GetContext(), sfq), 100, 2, 1, 1);
			match = new SpanNearQuery(clauses, 0, false);
			sfq = new SpanFirstQuery(match, 2);
			CheckSpans(MultiSpansWrapper.Wrap(indexReader.GetContext(), sfq), 100, 2, 1, 1);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSpanNot()
		{
			SpanQuery[] clauses = new SpanQuery[2];
			clauses[0] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "one"));
			clauses[1] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "three"));
			SpanQuery spq = new SpanNearQuery(clauses, 5, true);
			SpanNotQuery snq = new SpanNotQuery(spq, new SpanTermQuery(new Term(PayloadHelper
				.FIELD, "two")));
			Directory directory = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), directory, NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new TestPayloadSpans.PayloadAnalyzer(this)).SetSimilarity
				(similarity));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewTextField(PayloadHelper.FIELD, "one two three one four three", Field.Store
				.YES));
			writer.AddDocument(doc);
			IndexReader reader = writer.Reader;
			writer.Dispose();
			CheckSpans(MultiSpansWrapper.Wrap(reader.GetContext(), snq), 1, new int[] { 2 });
			reader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestNestedSpans()
		{
			SpanTermQuery stq;
			Lucene.Net.Search.Spans.Spans spans;
			IndexSearcher searcher = GetSearcher();
			stq = new SpanTermQuery(new Term(PayloadHelper.FIELD, "mark"));
			spans = MultiSpansWrapper.Wrap(searcher.GetTopReaderContext(), stq);
			IsTrue("spans is null and it shouldn't be", spans != null);
			CheckSpans(spans, 0, null);
			SpanQuery[] clauses = new SpanQuery[3];
			clauses[0] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "rr"));
			clauses[1] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "yy"));
			clauses[2] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "xx"));
			SpanNearQuery spanNearQuery = new SpanNearQuery(clauses, 12, false);
			spans = MultiSpansWrapper.Wrap(searcher.GetTopReaderContext(), spanNearQuery);
			IsTrue("spans is null and it shouldn't be", spans != null);
			CheckSpans(spans, 2, new int[] { 3, 3 });
			clauses[0] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "xx"));
			clauses[1] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "rr"));
			clauses[2] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "yy"));
			spanNearQuery = new SpanNearQuery(clauses, 6, true);
			spans = MultiSpansWrapper.Wrap(searcher.GetTopReaderContext(), spanNearQuery);
			IsTrue("spans is null and it shouldn't be", spans != null);
			CheckSpans(spans, 1, new int[] { 3 });
			clauses = new SpanQuery[2];
			clauses[0] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "xx"));
			clauses[1] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "rr"));
			spanNearQuery = new SpanNearQuery(clauses, 6, true);
			// xx within 6 of rr
			SpanQuery[] clauses2 = new SpanQuery[2];
			clauses2[0] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "yy"));
			clauses2[1] = spanNearQuery;
			SpanNearQuery nestedSpanNearQuery = new SpanNearQuery(clauses2, 6, false);
			// yy within 6 of xx within 6 of rr
			spans = MultiSpansWrapper.Wrap(searcher.GetTopReaderContext(), nestedSpanNearQuery
				);
			IsTrue("spans is null and it shouldn't be", spans != null);
			CheckSpans(spans, 2, new int[] { 3, 3 });
			closeIndexReader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestFirstClauseWithoutPayload()
		{
			Lucene.Net.Search.Spans.Spans spans;
			IndexSearcher searcher = GetSearcher();
			SpanQuery[] clauses = new SpanQuery[3];
			clauses[0] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "nopayload"));
			clauses[1] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "qq"));
			clauses[2] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "ss"));
			SpanNearQuery spanNearQuery = new SpanNearQuery(clauses, 6, true);
			SpanQuery[] clauses2 = new SpanQuery[2];
			clauses2[0] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "pp"));
			clauses2[1] = spanNearQuery;
			SpanNearQuery snq = new SpanNearQuery(clauses2, 6, false);
			SpanQuery[] clauses3 = new SpanQuery[2];
			clauses3[0] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "np"));
			clauses3[1] = snq;
			SpanNearQuery nestedSpanNearQuery = new SpanNearQuery(clauses3, 6, false);
			spans = MultiSpansWrapper.Wrap(searcher.GetTopReaderContext(), nestedSpanNearQuery
				);
			IsTrue("spans is null and it shouldn't be", spans != null);
			CheckSpans(spans, 1, new int[] { 3 });
			closeIndexReader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestHeavilyNestedSpanQuery()
		{
			Lucene.Net.Search.Spans.Spans spans;
			IndexSearcher searcher = GetSearcher();
			SpanQuery[] clauses = new SpanQuery[3];
			clauses[0] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "one"));
			clauses[1] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "two"));
			clauses[2] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "three"));
			SpanNearQuery spanNearQuery = new SpanNearQuery(clauses, 5, true);
			clauses = new SpanQuery[3];
			clauses[0] = spanNearQuery;
			clauses[1] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "five"));
			clauses[2] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "six"));
			SpanNearQuery spanNearQuery2 = new SpanNearQuery(clauses, 6, true);
			SpanQuery[] clauses2 = new SpanQuery[2];
			clauses2[0] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "eleven"));
			clauses2[1] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "ten"));
			SpanNearQuery spanNearQuery3 = new SpanNearQuery(clauses2, 2, false);
			SpanQuery[] clauses3 = new SpanQuery[3];
			clauses3[0] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "nine"));
			clauses3[1] = spanNearQuery2;
			clauses3[2] = spanNearQuery3;
			SpanNearQuery nestedSpanNearQuery = new SpanNearQuery(clauses3, 6, false);
			spans = MultiSpansWrapper.Wrap(searcher.GetTopReaderContext(), nestedSpanNearQuery
				);
			IsTrue("spans is null and it shouldn't be", spans != null);
			CheckSpans(spans, 2, new int[] { 8, 8 });
			closeIndexReader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestShrinkToAfterShortestMatch()
		{
			Directory directory = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), directory, NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new TestPayloadSpans.TestPayloadAnalyzer(this)));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new TextField("content", new StringReader("a b c d e f g h i j a k")));
			writer.AddDocument(doc);
			IndexReader reader = writer.Reader;
			IndexSearcher @is = NewSearcher(reader);
			writer.Dispose();
			SpanTermQuery stq1 = new SpanTermQuery(new Term("content", "a"));
			SpanTermQuery stq2 = new SpanTermQuery(new Term("content", "k"));
			SpanQuery[] sqs = new SpanQuery[] { stq1, stq2 };
			SpanNearQuery snq = new SpanNearQuery(sqs, 1, true);
			Lucene.Net.Search.Spans.Spans spans = MultiSpansWrapper.Wrap(@is.GetTopReaderContext
				(), snq);
			TopDocs topDocs = @is.Search(snq, 1);
			ICollection<string> payloadSet = new HashSet<string>();
			for (int i = 0; i < topDocs.ScoreDocs.Length; i++)
			{
				while (spans.Next())
				{
					ICollection<byte[]> payloads = spans.Payload;
					foreach (byte[] payload in payloads)
					{
						payloadSet.Add(new string(payload, StandardCharsets.UTF_8));
					}
				}
			}
			AreEqual(2, payloadSet.Count);
			IsTrue(payloadSet.Contains("a:Noise:10"));
			IsTrue(payloadSet.Contains("k:Noise:11"));
			reader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestShrinkToAfterShortestMatch2()
		{
			Directory directory = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), directory, NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new TestPayloadSpans.TestPayloadAnalyzer(this)));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new TextField("content", new StringReader("a b a d k f a h i k a k")));
			writer.AddDocument(doc);
			IndexReader reader = writer.Reader;
			IndexSearcher @is = NewSearcher(reader);
			writer.Dispose();
			SpanTermQuery stq1 = new SpanTermQuery(new Term("content", "a"));
			SpanTermQuery stq2 = new SpanTermQuery(new Term("content", "k"));
			SpanQuery[] sqs = new SpanQuery[] { stq1, stq2 };
			SpanNearQuery snq = new SpanNearQuery(sqs, 0, true);
			Lucene.Net.Search.Spans.Spans spans = MultiSpansWrapper.Wrap(@is.GetTopReaderContext
				(), snq);
			TopDocs topDocs = @is.Search(snq, 1);
			ICollection<string> payloadSet = new HashSet<string>();
			for (int i = 0; i < topDocs.ScoreDocs.Length; i++)
			{
				while (spans.Next())
				{
					ICollection<byte[]> payloads = spans.Payload;
					foreach (byte[] payload in payloads)
					{
						payloadSet.Add(new string(payload, StandardCharsets.UTF_8));
					}
				}
			}
			AreEqual(2, payloadSet.Count);
			IsTrue(payloadSet.Contains("a:Noise:10"));
			IsTrue(payloadSet.Contains("k:Noise:11"));
			reader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestShrinkToAfterShortestMatch3()
		{
			Directory directory = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), directory, NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new TestPayloadSpans.TestPayloadAnalyzer(this)));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new TextField("content", new StringReader("j k a l f k k p a t a k l k t a"
				)));
			writer.AddDocument(doc);
			IndexReader reader = writer.Reader;
			IndexSearcher @is = NewSearcher(reader);
			writer.Dispose();
			SpanTermQuery stq1 = new SpanTermQuery(new Term("content", "a"));
			SpanTermQuery stq2 = new SpanTermQuery(new Term("content", "k"));
			SpanQuery[] sqs = new SpanQuery[] { stq1, stq2 };
			SpanNearQuery snq = new SpanNearQuery(sqs, 0, true);
			Lucene.Net.Search.Spans.Spans spans = MultiSpansWrapper.Wrap(@is.GetTopReaderContext
				(), snq);
			TopDocs topDocs = @is.Search(snq, 1);
			ICollection<string> payloadSet = new HashSet<string>();
			for (int i = 0; i < topDocs.ScoreDocs.Length; i++)
			{
				while (spans.Next())
				{
					ICollection<byte[]> payloads = spans.Payload;
					foreach (byte[] payload in payloads)
					{
						payloadSet.Add(new string(payload, StandardCharsets.UTF_8));
					}
				}
			}
			AreEqual(2, payloadSet.Count);
			if (VERBOSE)
			{
				foreach (string payload in payloadSet)
				{
					System.Console.Out.WriteLine("match:" + payload);
				}
			}
			IsTrue(payloadSet.Contains("a:Noise:10"));
			IsTrue(payloadSet.Contains("k:Noise:11"));
			reader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestPayloadSpanUtil()
		{
			Directory directory = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), directory, NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new TestPayloadSpans.PayloadAnalyzer(this)).SetSimilarity
				(similarity));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewTextField(PayloadHelper.FIELD, "xx rr yy mm  pp", Field.Store.YES));
			writer.AddDocument(doc);
			IndexReader reader = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(reader);
			PayloadSpanUtil psu = new PayloadSpanUtil(searcher.GetTopReaderContext());
			ICollection<byte[]> payloads = psu.GetPayloadsForQuery(new TermQuery(new Term(PayloadHelper
				.FIELD, "rr")));
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("Num payloads:" + payloads.Count);
				foreach (byte[] bytes in payloads)
				{
					System.Console.Out.WriteLine(new string(bytes, StandardCharsets.UTF_8));
				}
			}
			reader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void CheckSpans(Lucene.Net.Search.Spans.Spans spans, int expectedNumSpans
			, int expectedNumPayloads, int expectedPayloadLength, int expectedFirstByte)
		{
			IsTrue("spans is null and it shouldn't be", spans != null);
			//each position match should have a span associated with it, since there is just one underlying term query, there should
			//only be one entry in the span
			int seen = 0;
			while (spans.Next() == true)
			{
				//if we expect payloads, then isPayloadAvailable should be true
				if (expectedNumPayloads > 0)
				{
					IsTrue("isPayloadAvailable is not returning the correct value: "
						 + spans.IsPayloadAvailable() + " and it should be: " + (expectedNumPayloads > 0
						), spans.IsPayloadAvailable() == true);
				}
				else
				{
					IsTrue("isPayloadAvailable should be false", spans.IsPayloadAvailable
						() == false);
				}
				//See payload helper, for the PayloadHelper.FIELD field, there is a single byte payload at every token
				if (spans.IsPayloadAvailable())
				{
					ICollection<byte[]> payload = spans.Payload;
					IsTrue("payload Size: " + payload.Count + " is not: " + expectedNumPayloads
						, payload.Count == expectedNumPayloads);
					foreach (byte[] thePayload in payload)
					{
						IsTrue("payload[0] Size: " + thePayload.Length + " is not: "
							 + expectedPayloadLength, thePayload.Length == expectedPayloadLength);
						IsTrue(thePayload[0] + " does not equal: " + expectedFirstByte
							, thePayload[0] == expectedFirstByte);
					}
				}
				seen++;
			}
			IsTrue(seen + " does not equal: " + expectedNumSpans, seen
				 == expectedNumSpans);
		}

		/// <exception cref="System.Exception"></exception>
		private IndexSearcher GetSearcher()
		{
			directory = NewDirectory();
			string[] docs = new string[] { "xx rr yy mm  pp", "xx yy mm rr pp", "nopayload qq ss pp np"
				, "one two three four five six seven eight nine ten eleven", "nine one two three four five six seven eight eleven ten"
				 };
			RandomIndexWriter writer = new RandomIndexWriter(Random(), directory, NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new TestPayloadSpans.PayloadAnalyzer(this)).SetSimilarity
				(similarity));
			Lucene.Net.Documents.Document doc = null;
			for (int i = 0; i < docs.Length; i++)
			{
				doc = new Lucene.Net.Documents.Document();
				string docText = docs[i];
				doc.Add(NewTextField(PayloadHelper.FIELD, docText, Field.Store.YES));
				writer.AddDocument(doc);
			}
			closeIndexReader = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(closeIndexReader);
			return searcher;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void CheckSpans(Lucene.Net.Search.Spans.Spans spans, int numSpans, 
			int[] numPayloads)
		{
			int cnt = 0;
			while (spans.Next() == true)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("\nSpans Dump --");
				}
				if (spans.IsPayloadAvailable())
				{
					ICollection<byte[]> payload = spans.Payload;
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("payloads for span:" + payload.Count);
						foreach (byte[] bytes in payload)
						{
							System.Console.Out.WriteLine("doc:" + spans.Doc() + " s:" + spans.Start() + " e:"
								 + spans.End() + " " + new string(bytes, StandardCharsets.UTF_8));
						}
					}
					AreEqual(numPayloads[cnt], payload.Count);
				}
				else
				{
					IsFalse("Expected spans:" + numPayloads[cnt] + " found: 0"
						, numPayloads.Length > 0 && numPayloads[cnt] > 0);
				}
				cnt++;
			}
			AreEqual(numSpans, cnt);
		}

		internal sealed class PayloadAnalyzer : Analyzer
		{
			protected override Analyzer.TokenStreamComponents CreateComponents(string fieldName
				, StreamReader reader)
			{
				Tokenizer result = new MockTokenizer(reader, MockTokenizer.SIMPLE, true);
				return new Analyzer.TokenStreamComponents(result, new TestPayloadSpans.PayloadFilter
					(this, result));
			}

			internal PayloadAnalyzer(TestPayloadSpans _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly TestPayloadSpans _enclosing;
		}

		internal sealed class PayloadFilter : TokenFilter
		{
			internal ICollection<string> entities = new HashSet<string>();

			internal ICollection<string> nopayload = new HashSet<string>();

			internal int pos;

			internal PayloadAttribute payloadAtt;

			internal CharTermAttribute termAtt;

			internal PositionIncrementAttribute posIncrAtt;

			protected PayloadFilter(TestPayloadSpans _enclosing, TokenStream input) : base(input
				)
			{
				this._enclosing = _enclosing;
				this.pos = 0;
				this.entities.Add("xx");
				this.entities.Add("one");
				this.nopayload.Add("nopayload");
				this.nopayload.Add("np");
				this.termAtt = this.AddAttribute<CharTermAttribute>();
				this.posIncrAtt = this.AddAttribute<PositionIncrementAttribute>();
				this.payloadAtt = this.AddAttribute<PayloadAttribute>();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override bool IncrementToken()
			{
				if (this.input.IncrementToken())
				{
					string token = this.termAtt.ToString();
					if (!this.nopayload.Contains(token))
					{
						if (this.entities.Contains(token))
						{
							this.payloadAtt.Payload = (new BytesRef(token + ":Entity:" + this.pos));
						}
						else
						{
							this.payloadAtt.Payload = (new BytesRef(token + ":Noise:" + this.pos));
						}
					}
					this.pos += this.posIncrAtt.GetPositionIncrement();
					return true;
				}
				return false;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Reset()
			{
				base.Reset();
				this.pos = 0;
			}

			private readonly TestPayloadSpans _enclosing;
		}

		public sealed class TestPayloadAnalyzer : Analyzer
		{
			protected override Analyzer.TokenStreamComponents CreateComponents(string fieldName
				, StreamReader reader)
			{
				Tokenizer result = new MockTokenizer(reader, MockTokenizer.SIMPLE, true);
				return new Analyzer.TokenStreamComponents(result, new TestPayloadSpans.PayloadFilter
					(this, result));
			}

			internal TestPayloadAnalyzer(TestPayloadSpans _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly TestPayloadSpans _enclosing;
		}
	}
}
