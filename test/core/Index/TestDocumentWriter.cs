using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.TestFramework;
using Lucene.Net.TestFramework.Index;
using Lucene.Net.TestFramework.Util;
using Lucene.Net.Util;
using Lucene.Net.Store;
using NUnit.Framework;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Test.Index
{
    [TestFixture]
	public class TestDocumentWriter : LuceneTestCase
	{
		private Directory dir;

		[SetUp]
		public override void SetUp()
		{
			base.SetUp();
			dir = NewDirectory();
		}

		[TearDown]
		public override void TearDown()
		{
			dir.Dispose();
			base.TearDown();
		}

        [Test]
		public virtual void TestDirNotNull()
		{
			IsTrue(dir != null);
		}

		[Test]
		public virtual void TestAddDocument()
		{
			var testDoc = new Lucene.Net.Documents.Document();
			DocHelper.SetupDoc(testDoc);
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			writer.AddDocument(testDoc);
			writer.Commit();
			SegmentCommitInfo info = writer.NewestSegment;
			writer.Dispose();
			//After adding the document, we should be able to read it back in
			SegmentReader reader = new SegmentReader(info, DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR
				, NewIOContext(Random()));
			IsTrue(reader != null);
			Lucene.Net.Documents.Document doc = reader.Document(0);
			IsTrue(doc != null);
			//System.out.println("Document: " + doc);
			IIndexableField[] fields = doc.GetFields("textField2");
			IsTrue(fields != null && fields.Length == 1);
			IsTrue(fields[0].StringValue.Equals(DocHelper.FIELD_2_TEXT
				));
			IsTrue(fields[0].FieldTypeValue.StoreTermVectors);
			fields = doc.GetFields("textField1");
			IsTrue(fields != null && fields.Length == 1);
			IsTrue(fields[0].StringValue.Equals(DocHelper.FIELD_1_TEXT
				));
			IsFalse(fields[0].FieldTypeValue.StoreTermVectors);
			fields = doc.GetFields("keyField");
			IsTrue(fields != null && fields.Length == 1);
			IsTrue(fields[0].StringValue.Equals(DocHelper.KEYWORD_TEXT
				));
			fields = doc.GetFields(DocHelper.NO_NORMS_KEY);
			IsTrue(fields != null && fields.Length == 1);
			IsTrue(fields[0].StringValue.Equals(DocHelper.NO_NORMS_TEXT
				));
			fields = doc.GetFields(DocHelper.TEXT_FIELD_3_KEY);
			IsTrue(fields != null && fields.Length == 1);
			IsTrue(fields[0].StringValue.Equals(DocHelper.FIELD_3_TEXT
				));
			// test that the norms are not present in the segment if
			// omitNorms is true
			foreach (FieldInfo fi in reader.FieldInfos)
			{
				if (fi.IsIndexed)
				{
					IsTrue(fi.OmitsNorms == (reader.GetNormValues(fi.name) ==
						 null));
				}
			}
			reader.Dispose();
		}

		[Test]
		public virtual void TestPositionIncrementGap()
		{
			Analyzer analyzer = new AnonymousAnalyzer();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, analyzer));
			var doc = new Lucene.Net.Documents.Document
			{
			    NewTextField("repeated", "repeated one", Field.Store.YES),
			    NewTextField("repeated", "repeated two", Field.Store.YES)
			};
		    writer.AddDocument(doc);
			writer.Commit();
			SegmentCommitInfo info = writer.NewestSegment;
			writer.Dispose();
			SegmentReader reader = new SegmentReader(info, DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR
				, NewIOContext(Random()));
			DocsAndPositionsEnum termPositions = MultiFields.GetTermPositionsEnum(reader, MultiFields
				.GetLiveDocs(reader), "repeated", new BytesRef("repeated"));
			IsTrue(termPositions.NextDoc() != DocIdSetIterator.NO_MORE_DOCS
				);
			int freq = termPositions.Freq;
			AreEqual(2, freq);
			AreEqual(0, termPositions.NextPosition());
			AreEqual(502, termPositions.NextPosition());
			reader.Dispose();
		}

		private sealed class AnonymousAnalyzer : Analyzer
		{
		    public override TokenStreamComponents CreateComponents(string fieldName
				, TextReader reader)
			{
				return new TokenStreamComponents(new MockTokenizer(reader, MockTokenizer
					.WHITESPACE, false));
			}

			public override int GetPositionIncrementGap(string fieldName)
			{
				return 500;
			}
		}

		[Test]
		public virtual void TestTokenReuse()
		{
			Analyzer analyzer = new AnonymousAnalyzer2();
			// set payload on first position only
			// index a "synonym" for every token
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, analyzer));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
			doc.Add(NewTextField("f1", "a 5 a a", Field.Store.YES));
			writer.AddDocument(doc);
			writer.Commit();
			SegmentCommitInfo info = writer.NewestSegment;
			writer.Dispose();
			SegmentReader reader = new SegmentReader(info, DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR
				, NewIOContext(Random()));
			DocsAndPositionsEnum termPositions = MultiFields.GetTermPositionsEnum(reader, reader
				.LiveDocs, "f1", new BytesRef("a"));
			IsTrue(termPositions.NextDoc() != DocIdSetIterator.NO_MORE_DOCS
				);
			int freq = termPositions.Freq;
			AreEqual(3, freq);
			AreEqual(0, termPositions.NextPosition());
			IsNotNull(termPositions.Payload);
			AreEqual(6, termPositions.NextPosition());
			IsNull(termPositions.Payload);
			AreEqual(7, termPositions.NextPosition());
			IsNull(termPositions.Payload);
			reader.Dispose();
		}

		private sealed class AnonymousAnalyzer2 : Analyzer
		{
		    public override Analyzer.TokenStreamComponents CreateComponents(string fieldName
				, TextReader reader)
			{
				Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
				return new Analyzer.TokenStreamComponents(tokenizer, new AnonymousTokenFilter(tokenizer
					));
			}

			private sealed class AnonymousTokenFilter : TokenFilter
			{
				public AnonymousTokenFilter(TokenStream baseArg1) : base(baseArg1)
				{
					this.first = true;
					this.termAtt = this.AddAttribute<CharTermAttribute>();
					this.payloadAtt = this.AddAttribute<PayloadAttribute>();
					this.posIncrAtt = this.AddAttribute<PositionIncrementAttribute>();
				}

				internal bool first;

				internal AttributeSource.State state;

				/// <exception cref="System.IO.IOException"></exception>
				public override bool IncrementToken()
				{
					if (this.state != null)
					{
						this.RestoreState(this.state);
						this.payloadAtt.Payload = (null);
						this.posIncrAtt.PositionIncrement = (0);
						this.termAtt.SetEmpty().Append("b");
						this.state = null;
						return true;
					}
					bool hasNext = this.input.IncrementToken();
					if (!hasNext)
					{
						return false;
					}
					if (char.IsDigit(this.termAtt.Buffer[0]))
					{
						this.posIncrAtt.PositionIncrement = (this.termAtt.Buffer[0] - '0');
					}
					if (this.first)
					{
						this.payloadAtt.Payload = (new BytesRef(new sbyte[] { 100 }));
						this.first = false;
					}
					this.state = this.CaptureState();
					return true;
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override void Reset()
				{
					base.Reset();
					this.first = true;
					this.state = null;
				}

				internal readonly CharTermAttribute termAtt;

				internal readonly PayloadAttribute payloadAtt;

				internal readonly PositionIncrementAttribute posIncrAtt;
			}
		}

		[Test]
		public virtual void TestPreAnalyzedField()
		{
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			var doc = new Lucene.Net.Documents.Document {new TextField("preanalyzed", new AnonymousTokenStream())};
		    writer.AddDocument(doc);
			writer.Commit();
			SegmentCommitInfo info = writer.NewestSegment;
			writer.Dispose();
			SegmentReader reader = new SegmentReader(info, DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR
				, NewIOContext(Random()));
			DocsAndPositionsEnum termPositions = reader.TermPositionsEnum(new Term("preanalyzed"
				, "term1"));
			IsTrue(termPositions.NextDoc() != DocIdSetIterator.NO_MORE_DOCS
				);
			AreEqual(1, termPositions.Freq);
			AreEqual(0, termPositions.NextPosition());
			termPositions = reader.TermPositionsEnum(new Term("preanalyzed", "term2"));
			IsTrue(termPositions.NextDoc() != DocIdSetIterator.NO_MORE_DOCS
				);
			AreEqual(2, termPositions.Freq);
			AreEqual(1, termPositions.NextPosition());
			AreEqual(3, termPositions.NextPosition());
			termPositions = reader.TermPositionsEnum(new Term("preanalyzed", "term3"));
			IsTrue(termPositions.NextDoc() != DocIdSetIterator.NO_MORE_DOCS
				);
			AreEqual(1, termPositions.Freq);
			AreEqual(2, termPositions.NextPosition());
			reader.Dispose();
		}

		private sealed class AnonymousTokenStream : TokenStream
		{
			public AnonymousTokenStream()
			{
				this.tokens = new string[] { "term1", "term2", "term3", "term2" };
				this.index = 0;
				this.termAtt = this.AddAttribute<CharTermAttribute>();
			}

			private string[] tokens;

			private int index;

			private CharTermAttribute termAtt;

			public override bool IncrementToken()
			{
				if (this.index == this.tokens.Length)
				{
					return false;
				}
				else
				{
					this.ClearAttributes();
					this.termAtt.SetEmpty().Append(this.tokens[this.index++]);
					return true;
				}
			}
		}

		/// <summary>
		/// Test adding two fields with the same name, but
		/// with different term vector setting (LUCENE-766).
		/// </summary>
		/// <remarks>
		/// Test adding two fields with the same name, but
		/// with different term vector setting (LUCENE-766).
		/// </remarks>
		[Test]
		public virtual void TestMixedTermVectorSettingsSameField()
		{
		    // f1 first without tv then with tv
		    var customType2 = new FieldType(StringField.TYPE_STORED)
		    {
		        StoreTermVectors = true,
		        StoreTermVectorOffsets = true,
		        StoreTermVectorPositions = true
		    };
		    var doc = new Lucene.Net.Documents.Document
		    {
		        NewStringField("f1", "v1", Field.Store.YES),
		        NewField("f1", "v2", customType2),
		        NewField("f2", "v1", customType2),
		        NewStringField("f2", "v2", Field.Store.YES)
		    };
		    // f2 first with tv then without tv
		    IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			writer.AddDocument(doc);
			writer.Dispose();
			TestUtil.CheckIndex(dir);
			IndexReader reader = DirectoryReader.Open(dir);
			// f1
			Terms tfv1 = reader.GetTermVectors(0).Terms("f1");
			IsNotNull(tfv1);
			AssertEquals("the 'with_tv' setting should rule!", 2, tfv1.Size);
			// f2
			Terms tfv2 = reader.GetTermVectors(0).Terms("f2");
			IsNotNull(tfv2);
			AssertEquals("the 'with_tv' setting should rule!", 2, tfv2.Size);
			reader.Dispose();
		}

		/// <summary>
		/// Test adding two fields with the same name, one indexed
		/// the other stored only.
		/// </summary>
		/// <remarks>
		/// Test adding two fields with the same name, one indexed
		/// the other stored only. The omitNorms and omitTermFreqAndPositions setting
		/// of the stored field should not affect the indexed one (LUCENE-1590)
		/// </remarks>
		[Test]
		public virtual void TestLUCENE_1590()
		{
		    // f1 has no norms
		    FieldType customType = new FieldType(TextField.TYPE_NOT_STORED) {OmitNorms = (true)};
		    FieldType customType2 = new FieldType {Stored = (true)};
		    var doc = new Lucene.Net.Documents.Document {NewField("f1", "v1", customType), NewField("f1", "v2", customType2)};
		    // f2 has no TF
			FieldType customType3 = new FieldType(TextField.TYPE_NOT_STORED) {IndexOptions = (FieldInfo.IndexOptions.DOCS_ONLY)};
		    Field f = NewField("f2", "v1", customType3);
			doc.Add(f);
			doc.Add(NewField("f2", "v2", customType2));
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			writer.AddDocument(doc);
			writer.ForceMerge(1);
			// be sure to have a single segment
			writer.Dispose();
			TestUtil.CheckIndex(dir);
			SegmentReader reader = GetOnlySegmentReader(DirectoryReader.Open(dir));
			FieldInfos fi = reader.FieldInfos;
			// f1
			AssertFalse("f1 should have no norms", fi.FieldInfo("f1").HasNorms);
			AssertEquals("omitTermFreqAndPositions field bit should not be set for f1"
				, FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS, fi.FieldInfo("f1").IndexOptionsValue);
			// f2
			AssertTrue("f2 should have norms", fi.FieldInfo("f2").HasNorms);
			AssertEquals("omitTermFreqAndPositions field bit should be set for f2"
				, FieldInfo.IndexOptions.DOCS_ONLY, fi.FieldInfo("f2").IndexOptionsValue);
			reader.Dispose();
		}
	}
}
