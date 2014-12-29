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
using Lucene.Net.Search.Spans;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search
{
	/// <summary>Term position unit test.</summary>
	/// <remarks>Term position unit test.</remarks>
	public class TestPositionIncrement : LuceneTestCase
	{
		internal const bool VERBOSE = false;

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSetPosition()
		{
			Analyzer analyzer = new _Analyzer_60();
			// TODO: use CannedTokenStream
			Directory store = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), store, analyzer);
			Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
			d.Add(NewTextField("field", "bogus", Field.Store.YES));
			writer.AddDocument(d);
			IndexReader reader = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(reader);
			DocsAndPositionsEnum pos = MultiFields.GetTermPositionsEnum(searcher.GetIndexReader
				(), MultiFields.GetLiveDocs(searcher.IndexReader), "field", new BytesRef("1"
				));
			pos.NextDoc();
			// first token should be at position 0
			AreEqual(0, pos.NextPosition());
			pos = MultiFields.GetTermPositionsEnum(searcher.IndexReader, MultiFields.GetLiveDocs
				(searcher.IndexReader), "field", new BytesRef("2"));
			pos.NextDoc();
			// second token should be at position 2
			AreEqual(2, pos.NextPosition());
			PhraseQuery q;
			ScoreDoc[] hits;
			q = new PhraseQuery();
			q.Add(new Term("field", "1"));
			q.Add(new Term("field", "2"));
			hits = searcher.Search(q, null, 1000).ScoreDocs;
			AreEqual(0, hits.Length);
			// same as previous, just specify positions explicitely.
			q = new PhraseQuery();
			q.Add(new Term("field", "1"), 0);
			q.Add(new Term("field", "2"), 1);
			hits = searcher.Search(q, null, 1000).ScoreDocs;
			AreEqual(0, hits.Length);
			// specifying correct positions should find the phrase.
			q = new PhraseQuery();
			q.Add(new Term("field", "1"), 0);
			q.Add(new Term("field", "2"), 2);
			hits = searcher.Search(q, null, 1000).ScoreDocs;
			AreEqual(1, hits.Length);
			q = new PhraseQuery();
			q.Add(new Term("field", "2"));
			q.Add(new Term("field", "3"));
			hits = searcher.Search(q, null, 1000).ScoreDocs;
			AreEqual(1, hits.Length);
			q = new PhraseQuery();
			q.Add(new Term("field", "3"));
			q.Add(new Term("field", "4"));
			hits = searcher.Search(q, null, 1000).ScoreDocs;
			AreEqual(0, hits.Length);
			// phrase query would find it when correct positions are specified. 
			q = new PhraseQuery();
			q.Add(new Term("field", "3"), 0);
			q.Add(new Term("field", "4"), 0);
			hits = searcher.Search(q, null, 1000).ScoreDocs;
			AreEqual(1, hits.Length);
			// phrase query should fail for non existing searched term 
			// even if there exist another searched terms in the same searched position. 
			q = new PhraseQuery();
			q.Add(new Term("field", "3"), 0);
			q.Add(new Term("field", "9"), 0);
			hits = searcher.Search(q, null, 1000).ScoreDocs;
			AreEqual(0, hits.Length);
			// multi-phrase query should succed for non existing searched term
			// because there exist another searched terms in the same searched position. 
			MultiPhraseQuery mq = new MultiPhraseQuery();
			mq.Add(new Term[] { new Term("field", "3"), new Term("field", "9") }, 0);
			hits = searcher.Search(mq, null, 1000).ScoreDocs;
			AreEqual(1, hits.Length);
			q = new PhraseQuery();
			q.Add(new Term("field", "2"));
			q.Add(new Term("field", "4"));
			hits = searcher.Search(q, null, 1000).ScoreDocs;
			AreEqual(1, hits.Length);
			q = new PhraseQuery();
			q.Add(new Term("field", "3"));
			q.Add(new Term("field", "5"));
			hits = searcher.Search(q, null, 1000).ScoreDocs;
			AreEqual(1, hits.Length);
			q = new PhraseQuery();
			q.Add(new Term("field", "4"));
			q.Add(new Term("field", "5"));
			hits = searcher.Search(q, null, 1000).ScoreDocs;
			AreEqual(1, hits.Length);
			q = new PhraseQuery();
			q.Add(new Term("field", "2"));
			q.Add(new Term("field", "5"));
			hits = searcher.Search(q, null, 1000).ScoreDocs;
			AreEqual(0, hits.Length);
			reader.Dispose();
			store.Dispose();
		}

		private sealed class _Analyzer_60 : Analyzer
		{
			public _Analyzer_60()
			{
			}

			protected override Analyzer.TokenStreamComponents CreateComponents(string fieldName
				, StreamReader reader)
			{
				return new Analyzer.TokenStreamComponents(new _Tokenizer_63(reader));
			}

			private sealed class _Tokenizer_63 : Tokenizer
			{
				public _Tokenizer_63(StreamReader baseArg1) : base(baseArg1)
				{
					this.TOKENS = new string[] { "1", "2", "3", "4", "5" };
					this.INCREMENTS = new int[] { 1, 2, 1, 0, 1 };
					this.i = 0;
					this.posIncrAtt = this.AddAttribute<PositionIncrementAttribute>();
					this.termAtt = this.AddAttribute<CharTermAttribute>();
					this.offsetAtt = this.AddAttribute<OffsetAttribute>();
				}

				private readonly string[] TOKENS;

				private readonly int[] INCREMENTS;

				private int i;

				internal PositionIncrementAttribute posIncrAtt;

				internal CharTermAttribute termAtt;

				internal OffsetAttribute offsetAtt;

				public override bool IncrementToken()
				{
					if (this.i == this.TOKENS.Length)
					{
						return false;
					}
					this.ClearAttributes();
					this.termAtt.Append(this.TOKENS[this.i]);
					this.offsetAtt.SetOffset(this.i, this.i);
					this.posIncrAtt.PositionIncrement = (this.INCREMENTS[this.i]);
					this.i++;
					return true;
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override void Reset()
				{
					base.Reset();
					this.i = 0;
				}
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestPayloadsPos0()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, new MockPayloadAnalyzer
				());
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new TextField("content", new StringReader("a a b c d e a f g h i j a b k k"
				)));
			writer.AddDocument(doc);
			IndexReader readerFromWriter = writer.Reader;
			AtomicReader r = SlowCompositeReaderWrapper.Wrap(readerFromWriter);
			DocsAndPositionsEnum tp = r.TermPositionsEnum(new Term("content", "a"));
			int count = 0;
			IsTrue(tp.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
			// "a" occurs 4 times
			AreEqual(4, tp.Freq);
			AreEqual(0, tp.NextPosition());
			AreEqual(1, tp.NextPosition());
			AreEqual(3, tp.NextPosition());
			AreEqual(6, tp.NextPosition());
			// only one doc has "a"
			AreEqual(DocIdSetIterator.NO_MORE_DOCS, tp.NextDoc());
			IndexSearcher @is = NewSearcher(readerFromWriter);
			SpanTermQuery stq1 = new SpanTermQuery(new Term("content", "a"));
			SpanTermQuery stq2 = new SpanTermQuery(new Term("content", "k"));
			SpanQuery[] sqs = new SpanQuery[] { stq1, stq2 };
			SpanNearQuery snq = new SpanNearQuery(sqs, 30, false);
			count = 0;
			bool sawZero = false;
			Lucene.Net.Search.Spans.Spans pspans = MultiSpansWrapper.Wrap(@is.GetTopReaderContext
				(), snq);
			while (pspans.Next())
			{
				ICollection<byte[]> payloads = pspans.Payload;
				sawZero |= pspans.Start() == 0;
				foreach (byte[] bytes in payloads)
				{
					count++;
				}
			}
			IsTrue(sawZero);
			AreEqual(5, count);
			// System.out.println("\ngetSpans test");
			Lucene.Net.Search.Spans.Spans spans = MultiSpansWrapper.Wrap(@is.GetTopReaderContext
				(), snq);
			count = 0;
			sawZero = false;
			while (spans.Next())
			{
				count++;
				sawZero |= spans.Start() == 0;
			}
			// System.out.println(spans.Doc() + " - " + spans.start() + " - " +
			// spans.end());
			AreEqual(4, count);
			IsTrue(sawZero);
			// System.out.println("\nPayloadSpanUtil test");
			sawZero = false;
			PayloadSpanUtil psu = new PayloadSpanUtil(@is.GetTopReaderContext());
			ICollection<byte[]> pls = psu.GetPayloadsForQuery(snq);
			count = pls.Count;
			foreach (byte[] bytes_1 in pls)
			{
				string s = new string(bytes_1, StandardCharsets.UTF_8);
				//System.out.println(s);
				sawZero |= s.Equals("pos: 0");
			}
			AreEqual(5, count);
			IsTrue(sawZero);
			writer.Dispose();
			@is.IndexReader.Dispose();
			dir.Dispose();
		}
	}
}
