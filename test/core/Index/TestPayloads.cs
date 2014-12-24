/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Test.Analysis.Tokenattributes;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	public class TestPayloads : LuceneTestCase
	{
		// Simple tests to test the payloads
		/// <exception cref="System.Exception"></exception>
		public virtual void TestPayload()
		{
			BytesRef payload = new BytesRef("This is a test!");
			AreEqual("Wrong payload length.", "This is a test!".Length
				, payload.length);
			BytesRef clone = payload.Clone();
			AreEqual(payload.length, clone.length);
			for (int i = 0; i < payload.length; i++)
			{
				AreEqual(payload.bytes[i + payload.offset], clone.bytes[i 
					+ clone.offset]);
			}
		}

		// Tests whether the DocumentWriter and SegmentMerger correctly enable the
		// payload bit in the FieldInfo
		/// <exception cref="System.Exception"></exception>
		public virtual void TestPayloadFieldBit()
		{
			Directory ram = NewDirectory();
			TestPayloads.PayloadAnalyzer analyzer = new TestPayloads.PayloadAnalyzer();
			IndexWriter writer = new IndexWriter(ram, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, analyzer));
			Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
			// this field won't have any payloads
			d.Add(NewTextField("f1", "This field has no payloads", Field.Store.NO));
			// this field will have payloads in all docs, however not for all term positions,
			// so this field is used to check if the DocumentWriter correctly enables the payloads bit
			// even if only some term positions have payloads
			d.Add(NewTextField("f2", "This field has payloads in all docs", Field.Store.NO));
			d.Add(NewTextField("f2", "This field has payloads in all docs NO PAYLOAD", Field.Store
				.NO));
			// this field is used to verify if the SegmentMerger enables payloads for a field if it has payloads 
			// enabled in only some documents
			d.Add(NewTextField("f3", "This field has payloads in some docs", Field.Store.NO));
			// only add payload data for field f2
			analyzer.SetPayloadData("f2", Sharpen.Runtime.GetBytesForString("somedata", StandardCharsets
				.UTF_8), 0, 1);
			writer.AddDocument(d);
			// flush
			writer.Close();
			SegmentReader reader = GetOnlySegmentReader(DirectoryReader.Open(ram));
			FieldInfos fi = reader.GetFieldInfos();
			IsFalse("Payload field bit should not be set.", fi.FieldInfo
				("f1").HasPayloads());
			IsTrue("Payload field bit should be set.", fi.FieldInfo("f2"
				).HasPayloads());
			IsFalse("Payload field bit should not be set.", fi.FieldInfo
				("f3").HasPayloads());
			reader.Close();
			// now we add another document which has payloads for field f3 and verify if the SegmentMerger
			// enabled payloads for that field
			analyzer = new TestPayloads.PayloadAnalyzer();
			// Clear payload state for each field
			writer = new IndexWriter(ram, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer
				).SetOpenMode(IndexWriterConfig.OpenMode.CREATE));
			d = new Lucene.Net.Documents.Document();
			d.Add(NewTextField("f1", "This field has no payloads", Field.Store.NO));
			d.Add(NewTextField("f2", "This field has payloads in all docs", Field.Store.NO));
			d.Add(NewTextField("f2", "This field has payloads in all docs", Field.Store.NO));
			d.Add(NewTextField("f3", "This field has payloads in some docs", Field.Store.NO));
			// add payload data for field f2 and f3
			analyzer.SetPayloadData("f2", Sharpen.Runtime.GetBytesForString("somedata", StandardCharsets
				.UTF_8), 0, 1);
			analyzer.SetPayloadData("f3", Sharpen.Runtime.GetBytesForString("somedata", StandardCharsets
				.UTF_8), 0, 3);
			writer.AddDocument(d);
			// force merge
			writer.ForceMerge(1);
			// flush
			writer.Close();
			reader = GetOnlySegmentReader(DirectoryReader.Open(ram));
			fi = reader.GetFieldInfos();
			IsFalse("Payload field bit should not be set.", fi.FieldInfo
				("f1").HasPayloads());
			IsTrue("Payload field bit should be set.", fi.FieldInfo("f2"
				).HasPayloads());
			IsTrue("Payload field bit should be set.", fi.FieldInfo("f3"
				).HasPayloads());
			reader.Close();
			ram.Close();
		}

		// Tests if payloads are correctly stored and loaded using both RamDirectory and FSDirectory
		/// <exception cref="System.Exception"></exception>
		public virtual void TestPayloadsEncoding()
		{
			Directory dir = NewDirectory();
			PerformTest(dir);
			dir.Close();
		}

		// builds an index with payloads in the given Directory and performs
		// different tests to verify the payload encoding
		/// <exception cref="System.Exception"></exception>
		private void PerformTest(Directory dir)
		{
			TestPayloads.PayloadAnalyzer analyzer = new TestPayloads.PayloadAnalyzer();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, analyzer).SetOpenMode(IndexWriterConfig.OpenMode.CREATE).SetMergePolicy(NewLogMergePolicy
				()));
			// should be in sync with value in TermInfosWriter
			int skipInterval = 16;
			int numTerms = 5;
			string fieldName = "f1";
			int numDocs = skipInterval + 1;
			// create content for the test documents with just a few terms
			Term[] terms = GenerateTerms(fieldName, numTerms);
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < terms.Length; i++)
			{
				sb.Append(terms[i].Text());
				sb.Append(" ");
			}
			string content = sb.ToString();
			int payloadDataLength = numTerms * numDocs * 2 + numTerms * numDocs * (numDocs - 
				1) / 2;
			byte[] payloadData = GenerateRandomData(payloadDataLength);
			Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
			d.Add(NewTextField(fieldName, content, Field.Store.NO));
			// add the same document multiple times to have the same payload lengths for all
			// occurrences within two consecutive skip intervals
			int offset = 0;
			for (int i_1 = 0; i_1 < 2 * numDocs; i_1++)
			{
				analyzer = new TestPayloads.PayloadAnalyzer(fieldName, payloadData, offset, 1);
				offset += numTerms;
				writer.AddDocument(d, analyzer);
			}
			// make sure we create more than one segment to test merging
			writer.Commit();
			// now we make sure to have different payload lengths next at the next skip point        
			for (int i_2 = 0; i_2 < numDocs; i_2++)
			{
				analyzer = new TestPayloads.PayloadAnalyzer(fieldName, payloadData, offset, i_2);
				offset += i_2 * numTerms;
				writer.AddDocument(d, analyzer);
			}
			writer.ForceMerge(1);
			// flush
			writer.Close();
			IndexReader reader = DirectoryReader.Open(dir);
			byte[] verifyPayloadData = new byte[payloadDataLength];
			offset = 0;
			DocsAndPositionsEnum[] tps = new DocsAndPositionsEnum[numTerms];
			for (int i_3 = 0; i_3 < numTerms; i_3++)
			{
				tps[i_3] = MultiFields.GetTermPositionsEnum(reader, MultiFields.GetLiveDocs(reader
					), terms[i_3].Field(), new BytesRef(terms[i_3].Text()));
			}
			while (tps[0].NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
			{
				for (int i_4 = 1; i_4 < numTerms; i_4++)
				{
					tps[i_4].NextDoc();
				}
				int freq = tps[0].Freq;
				for (int i_5 = 0; i_5 < freq; i_5++)
				{
					for (int j = 0; j < numTerms; j++)
					{
						tps[j].NextPosition();
						BytesRef br = tps[j].GetPayload();
						if (br != null)
						{
							System.Array.Copy(br.bytes, br.offset, verifyPayloadData, offset, br.length);
							offset += br.length;
						}
					}
				}
			}
			AssertByteArrayEquals(payloadData, verifyPayloadData);
			DocsAndPositionsEnum tp = MultiFields.GetTermPositionsEnum(reader, MultiFields.GetLiveDocs
				(reader), terms[0].Field(), new BytesRef(terms[0].Text()));
			tp.NextDoc();
			tp.NextPosition();
			// NOTE: prior rev of this test was failing to first
			// call next here:
			tp.NextDoc();
			// now we don't read this payload
			tp.NextPosition();
			BytesRef payload = tp.GetPayload();
			AreEqual("Wrong payload length.", 1, payload.length);
			AreEqual(payload.bytes[payload.offset], payloadData[numTerms
				]);
			tp.NextDoc();
			tp.NextPosition();
			// we don't read this payload and skip to a different document
			tp.Advance(5);
			tp.NextPosition();
			payload = tp.GetPayload();
			AreEqual("Wrong payload length.", 1, payload.length);
			AreEqual(payload.bytes[payload.offset], payloadData[5 * numTerms
				]);
			tp = MultiFields.GetTermPositionsEnum(reader, MultiFields.GetLiveDocs(reader), terms
				[1].Field(), new BytesRef(terms[1].Text()));
			tp.NextDoc();
			tp.NextPosition();
			AreEqual("Wrong payload length.", 1, tp.GetPayload().length
				);
			tp.Advance(skipInterval - 1);
			tp.NextPosition();
			AreEqual("Wrong payload length.", 1, tp.GetPayload().length
				);
			tp.Advance(2 * skipInterval - 1);
			tp.NextPosition();
			AreEqual("Wrong payload length.", 1, tp.GetPayload().length
				);
			tp.Advance(3 * skipInterval - 1);
			tp.NextPosition();
			AreEqual("Wrong payload length.", 3 * skipInterval - 2 * numDocs
				 - 1, tp.GetPayload().length);
			reader.Close();
			// test long payload
			analyzer = new TestPayloads.PayloadAnalyzer();
			writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer
				).SetOpenMode(IndexWriterConfig.OpenMode.CREATE));
			string singleTerm = "lucene";
			d = new Lucene.Net.Documents.Document();
			d.Add(NewTextField(fieldName, singleTerm, Field.Store.NO));
			// add a payload whose length is greater than the buffer size of BufferedIndexOutput
			payloadData = GenerateRandomData(2000);
			analyzer.SetPayloadData(fieldName, payloadData, 100, 1500);
			writer.AddDocument(d);
			writer.ForceMerge(1);
			// flush
			writer.Close();
			reader = DirectoryReader.Open(dir);
			tp = MultiFields.GetTermPositionsEnum(reader, MultiFields.GetLiveDocs(reader), fieldName
				, new BytesRef(singleTerm));
			tp.NextDoc();
			tp.NextPosition();
			BytesRef br_1 = tp.GetPayload();
			verifyPayloadData = new byte[br_1.length];
			byte[] portion = new byte[1500];
			System.Array.Copy(payloadData, 100, portion, 0, 1500);
			AssertByteArrayEquals(portion, br_1.bytes, br_1.offset, br_1.length);
			reader.Close();
		}

		internal static readonly Encoding utf8 = StandardCharsets.UTF_8;

		private void GenerateRandomData(byte[] data)
		{
			// this test needs the random data to be valid unicode
			string s = TestUtil.RandomFixedByteLengthUnicodeString(Random(), data.Length);
			byte[] b = Sharpen.Runtime.GetBytesForString(s, utf8);
			//HM:revisit 
			//assert b.length == data.length;
			System.Array.Copy(b, 0, data, 0, b.Length);
		}

		private byte[] GenerateRandomData(int n)
		{
			byte[] data = new byte[n];
			GenerateRandomData(data);
			return data;
		}

		private Term[] GenerateTerms(string fieldName, int n)
		{
			int maxDigits = (int)(Math.Log(n) / Math.Log(10));
			Term[] terms = new Term[n];
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < n; i++)
			{
				sb.Length = 0;
				sb.Append("t");
				int zeros = maxDigits - (int)(Math.Log(i) / Math.Log(10));
				for (int j = 0; j < zeros; j++)
				{
					sb.Append("0");
				}
				sb.Append(i);
				terms[i] = new Term(fieldName, sb.ToString());
			}
			return terms;
		}

		internal virtual void AssertByteArrayEquals(byte[] b1, byte[] b2)
		{
			if (b1.Length != b2.Length)
			{
				Fail("Byte arrays have different lengths: " + b1.Length + 
					", " + b2.Length);
			}
			for (int i = 0; i < b1.Length; i++)
			{
				if (b1[i] != b2[i])
				{
					Fail("Byte arrays different at index " + i + ": " + b1[i] 
						+ ", " + b2[i]);
				}
			}
		}

		internal virtual void AssertByteArrayEquals(byte[] b1, byte[] b2, int b2offset, int
			 b2length)
		{
			if (b1.Length != b2length)
			{
				Fail("Byte arrays have different lengths: " + b1.Length + 
					", " + b2length);
			}
			for (int i = 0; i < b1.Length; i++)
			{
				if (b1[i] != b2[b2offset + i])
				{
					Fail("Byte arrays different at index " + i + ": " + b1[i] 
						+ ", " + b2[b2offset + i]);
				}
			}
		}

		/// <summary>This Analyzer uses an WhitespaceTokenizer and PayloadFilter.</summary>
		/// <remarks>This Analyzer uses an WhitespaceTokenizer and PayloadFilter.</remarks>
		private class PayloadAnalyzer : Analyzer
		{
			internal IDictionary<string, TestPayloads.PayloadAnalyzer.PayloadData> fieldToData
				 = new Dictionary<string, TestPayloads.PayloadAnalyzer.PayloadData>();

			public PayloadAnalyzer() : base(PER_FIELD_REUSE_STRATEGY)
			{
			}

			public PayloadAnalyzer(string field, byte[] data, int offset, int length) : base(
				PER_FIELD_REUSE_STRATEGY)
			{
				SetPayloadData(field, data, offset, length);
			}

			internal virtual void SetPayloadData(string field, byte[] data, int offset, int length
				)
			{
				fieldToData.Put(field, new TestPayloads.PayloadAnalyzer.PayloadData(data, offset, 
					length));
			}

			protected override Analyzer.TokenStreamComponents CreateComponents(string fieldName
				, StreamReader reader)
			{
				TestPayloads.PayloadAnalyzer.PayloadData payload = fieldToData.Get(fieldName);
				Tokenizer ts = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
				TokenStream tokenStream = (payload != null) ? new TestPayloads.PayloadFilter(ts, 
					payload.data, payload.offset, payload.length) : ts;
				return new Analyzer.TokenStreamComponents(ts, tokenStream);
			}

			private class PayloadData
			{
				internal byte[] data;

				internal int offset;

				internal int length;

				internal PayloadData(byte[] data, int offset, int length)
				{
					this.data = data;
					this.offset = offset;
					this.length = length;
				}
			}
		}

		/// <summary>This Filter adds payloads to the tokens.</summary>
		/// <remarks>This Filter adds payloads to the tokens.</remarks>
		private class PayloadFilter : TokenFilter
		{
			private byte[] data;

			private int length;

			private int offset;

			private int startOffset;

			internal PayloadAttribute payloadAtt;

			internal CharTermAttribute termAttribute;

			public PayloadFilter(TokenStream @in, byte[] data, int offset, int length) : base
				(@in)
			{
				this.data = data;
				this.length = length;
				this.offset = offset;
				this.startOffset = offset;
				payloadAtt = AddAttribute<PayloadAttribute>();
				termAttribute = AddAttribute<CharTermAttribute>();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override bool IncrementToken()
			{
				bool hasNext = input.IncrementToken();
				if (!hasNext)
				{
					return false;
				}
				// Some values of the same field are to have payloads and others not
				if (offset + length <= data.Length && !termAttribute.ToString().EndsWith("NO PAYLOAD"
					))
				{
					BytesRef p = new BytesRef(data, offset, length);
					payloadAtt.SetPayload(p);
					offset += length;
				}
				else
				{
					payloadAtt.SetPayload(null);
				}
				return true;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Reset()
			{
				base.Reset();
				this.offset = startOffset;
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestThreadSafety()
		{
			int numThreads = 5;
			int numDocs = AtLeast(50);
			TestPayloads.ByteArrayPool pool = new TestPayloads.ByteArrayPool(numThreads, 5);
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			string field = "test";
			Sharpen.Thread[] ingesters = new Sharpen.Thread[numThreads];
			for (int i = 0; i < numThreads; i++)
			{
				ingesters[i] = new _Thread_464(numDocs, field, pool, writer);
				ingesters[i].Start();
			}
			for (int i_1 = 0; i_1 < numThreads; i_1++)
			{
				ingesters[i_1].Join();
			}
			writer.Close();
			IndexReader reader = DirectoryReader.Open(dir);
			TermsEnum terms = MultiFields.GetFields(reader).Terms(field).Iterator(null);
			Bits liveDocs = MultiFields.GetLiveDocs(reader);
			DocsAndPositionsEnum tp = null;
			while (terms.Next() != null)
			{
				string termText = terms.Term().Utf8ToString();
				tp = terms.DocsAndPositions(liveDocs, tp);
				while (tp.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
				{
					int freq = tp.Freq;
					for (int i_2 = 0; i_2 < freq; i_2++)
					{
						tp.NextPosition();
						BytesRef payload = tp.GetPayload();
						AreEqual(termText, payload.Utf8ToString());
					}
				}
			}
			reader.Close();
			dir.Close();
			AreEqual(pool.Size(), numThreads);
		}

		private sealed class _Thread_464 : Sharpen.Thread
		{
			public _Thread_464(int numDocs, string field, TestPayloads.ByteArrayPool pool, IndexWriter
				 writer)
			{
				this.numDocs = numDocs;
				this.field = field;
				this.pool = pool;
				this.writer = writer;
			}

			public override void Run()
			{
				try
				{
					for (int j = 0; j < numDocs; j++)
					{
						Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
						d.Add(new TextField(field, new TestPayloads.PoolingPayloadTokenStream(this, pool)
							));
						writer.AddDocument(d);
					}
				}
				catch (Exception e)
				{
					Sharpen.Runtime.PrintStackTrace(e);
					Fail(e.ToString());
				}
			}

			private readonly int numDocs;

			private readonly string field;

			private readonly TestPayloads.ByteArrayPool pool;

			private readonly IndexWriter writer;
		}

		private class PoolingPayloadTokenStream : TokenStream
		{
			private byte[] payload;

			private bool first;

			private TestPayloads.ByteArrayPool pool;

			private string term;

			internal CharTermAttribute termAtt;

			internal PayloadAttribute payloadAtt;

			internal PoolingPayloadTokenStream(TestPayloads _enclosing, TestPayloads.ByteArrayPool
				 pool)
			{
				this._enclosing = _enclosing;
				this.pool = pool;
				this.payload = pool.Get();
				this._enclosing.GenerateRandomData(this.payload);
				this.term = new string(this.payload, 0, this.payload.Length, TestPayloads.utf8);
				this.first = true;
				this.payloadAtt = this.AddAttribute<PayloadAttribute>();
				this.termAtt = this.AddAttribute<CharTermAttribute>();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override bool IncrementToken()
			{
				if (!this.first)
				{
					return false;
				}
				this.first = false;
				this.ClearAttributes();
				this.termAtt.Append(this.term);
				this.payloadAtt.SetPayload(new BytesRef(this.payload));
				return true;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Close()
			{
				this.pool.Release(this.payload);
			}

			private readonly TestPayloads _enclosing;
		}

		private class ByteArrayPool
		{
			private IList<byte[]> pool;

			internal ByteArrayPool(int capacity, int size)
			{
				pool = new AList<byte[]>();
				for (int i = 0; i < capacity; i++)
				{
					pool.AddItem(new byte[size]);
				}
			}

			internal virtual byte[] Get()
			{
				lock (this)
				{
					return pool.Remove(0);
				}
			}

			internal virtual void Release(byte[] b)
			{
				lock (this)
				{
					pool.AddItem(b);
				}
			}

			internal virtual int Size()
			{
				lock (this)
				{
					return pool.Count;
				}
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestAcrossFields()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, new MockAnalyzer(
				Random(), MockTokenizer.WHITESPACE, true));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new TextField("hasMaybepayload", "here we go", Field.Store.YES));
			writer.AddDocument(doc);
			writer.Close();
			writer = new RandomIndexWriter(Random(), dir, new MockAnalyzer(Random(), MockTokenizer
				.WHITESPACE, true));
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new TextField("hasMaybepayload2", "here we go", Field.Store.YES));
			writer.AddDocument(doc);
			writer.AddDocument(doc);
			writer.ForceMerge(1);
			writer.Close();
			dir.Close();
		}

		/// <summary>some docs have payload att, some not</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestMixupDocs()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, null);
			iwc.SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, iwc);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			Field field = new TextField("field", string.Empty, Field.Store.NO);
			TokenStream ts = new MockTokenizer(new StringReader("here we go"), MockTokenizer.
				WHITESPACE, true);
			IsFalse(ts.HasAttribute(typeof(PayloadAttribute)));
			field.SetTokenStream(ts);
			doc.Add(field);
			writer.AddDocument(doc);
			Token withPayload = new Token("withPayload", 0, 11);
			withPayload.SetPayload(new BytesRef("test"));
			ts = new CannedTokenStream(withPayload);
			IsTrue(ts.HasAttribute(typeof(PayloadAttribute)));
			field.SetTokenStream(ts);
			writer.AddDocument(doc);
			ts = new MockTokenizer(new StringReader("another"), MockTokenizer.WHITESPACE, true
				);
			IsFalse(ts.HasAttribute(typeof(PayloadAttribute)));
			field.SetTokenStream(ts);
			writer.AddDocument(doc);
			DirectoryReader reader = writer.GetReader();
			AtomicReader sr = SlowCompositeReaderWrapper.Wrap(reader);
			DocsAndPositionsEnum de = sr.TermPositionsEnum(new Term("field", "withPayload"));
			de.NextDoc();
			de.NextPosition();
			AreEqual(new BytesRef("test"), de.GetPayload());
			writer.Close();
			reader.Close();
			dir.Close();
		}

		/// <summary>some field instances have payload att, some not</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestMixupMultiValued()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			Field field = new TextField("field", string.Empty, Field.Store.NO);
			TokenStream ts = new MockTokenizer(new StringReader("here we go"), MockTokenizer.
				WHITESPACE, true);
			IsFalse(ts.HasAttribute(typeof(PayloadAttribute)));
			field.SetTokenStream(ts);
			doc.Add(field);
			Field field2 = new TextField("field", string.Empty, Field.Store.NO);
			Token withPayload = new Token("withPayload", 0, 11);
			withPayload.SetPayload(new BytesRef("test"));
			ts = new CannedTokenStream(withPayload);
			IsTrue(ts.HasAttribute(typeof(PayloadAttribute)));
			field2.SetTokenStream(ts);
			doc.Add(field2);
			Field field3 = new TextField("field", string.Empty, Field.Store.NO);
			ts = new MockTokenizer(new StringReader("nopayload"), MockTokenizer.WHITESPACE, true
				);
			IsFalse(ts.HasAttribute(typeof(PayloadAttribute)));
			field3.SetTokenStream(ts);
			doc.Add(field3);
			writer.AddDocument(doc);
			DirectoryReader reader = writer.GetReader();
			SegmentReader sr = GetOnlySegmentReader(reader);
			DocsAndPositionsEnum de = sr.TermPositionsEnum(new Term("field", "withPayload"));
			de.NextDoc();
			de.NextPosition();
			AreEqual(new BytesRef("test"), de.GetPayload());
			writer.Close();
			reader.Close();
			dir.Close();
		}
	}
}
