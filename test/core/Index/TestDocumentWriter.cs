/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.IO;
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
	public class TestDocumentWriter : LuceneTestCase
	{
		private Directory dir;

		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			dir = NewDirectory();
		}

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			dir.Close();
			base.TearDown();
		}

		public virtual void Test()
		{
			IsTrue(dir != null);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestAddDocument()
		{
			Lucene.Net.Documents.Document testDoc = new Lucene.Net.Documents.Document
				();
			DocHelper.SetupDoc(testDoc);
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			writer.AddDocument(testDoc);
			writer.Commit();
			SegmentCommitInfo info = writer.NewestSegment();
			writer.Close();
			//After adding the document, we should be able to read it back in
			SegmentReader reader = new SegmentReader(info, DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR
				, NewIOContext(Random()));
			IsTrue(reader != null);
			Lucene.Net.Documents.Document doc = reader.Document(0);
			IsTrue(doc != null);
			//System.out.println("Document: " + doc);
			IIndexableField[] fields = doc.GetFields("textField2");
			IsTrue(fields != null && fields.Length == 1);
			IsTrue(fields[0].StringValue = ).Equals(DocHelper.FIELD_2_TEXT
				));
			IsTrue(fields[0].FieldType().StoreTermVectors());
			fields = doc.GetFields("textField1");
			IsTrue(fields != null && fields.Length == 1);
			IsTrue(fields[0].StringValue = ).Equals(DocHelper.FIELD_1_TEXT
				));
			IsFalse(fields[0].FieldType().StoreTermVectors());
			fields = doc.GetFields("keyField");
			IsTrue(fields != null && fields.Length == 1);
			IsTrue(fields[0].StringValue = ).Equals(DocHelper.KEYWORD_TEXT
				));
			fields = doc.GetFields(DocHelper.NO_NORMS_KEY);
			IsTrue(fields != null && fields.Length == 1);
			IsTrue(fields[0].StringValue = ).Equals(DocHelper.NO_NORMS_TEXT
				));
			fields = doc.GetFields(DocHelper.TEXT_FIELD_3_KEY);
			IsTrue(fields != null && fields.Length == 1);
			IsTrue(fields[0].StringValue = ).Equals(DocHelper.FIELD_3_TEXT
				));
			// test that the norms are not present in the segment if
			// omitNorms is true
			foreach (FieldInfo fi in reader.GetFieldInfos())
			{
				if (fi.IsIndexed())
				{
					IsTrue(fi.OmitsNorms() == (reader.GetNormValues(fi.name) ==
						 null));
				}
			}
			reader.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestPositionIncrementGap()
		{
			Analyzer analyzer = new _Analyzer_108();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, analyzer));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewTextField("repeated", "repeated one", Field.Store.YES));
			doc.Add(NewTextField("repeated", "repeated two", Field.Store.YES));
			writer.AddDocument(doc);
			writer.Commit();
			SegmentCommitInfo info = writer.NewestSegment();
			writer.Close();
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
			reader.Close();
		}

		private sealed class _Analyzer_108 : Analyzer
		{
			public _Analyzer_108()
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
				return 500;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestTokenReuse()
		{
			Analyzer analyzer = new _Analyzer_143();
			// set payload on first position only
			// index a "synonym" for every token
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, analyzer));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewTextField("f1", "a 5 a a", Field.Store.YES));
			writer.AddDocument(doc);
			writer.Commit();
			SegmentCommitInfo info = writer.NewestSegment();
			writer.Close();
			SegmentReader reader = new SegmentReader(info, DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR
				, NewIOContext(Random()));
			DocsAndPositionsEnum termPositions = MultiFields.GetTermPositionsEnum(reader, reader
				.GetLiveDocs(), "f1", new BytesRef("a"));
			IsTrue(termPositions.NextDoc() != DocIdSetIterator.NO_MORE_DOCS
				);
			int freq = termPositions.Freq;
			AreEqual(3, freq);
			AreEqual(0, termPositions.NextPosition());
			IsNotNull(termPositions.GetPayload());
			AreEqual(6, termPositions.NextPosition());
			IsNull(termPositions.GetPayload());
			AreEqual(7, termPositions.NextPosition());
			IsNull(termPositions.GetPayload());
			reader.Close();
		}

		private sealed class _Analyzer_143 : Analyzer
		{
			public _Analyzer_143()
			{
			}

			protected override Analyzer.TokenStreamComponents CreateComponents(string fieldName
				, StreamReader reader)
			{
				Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
				return new Analyzer.TokenStreamComponents(tokenizer, new _TokenFilter_147(tokenizer
					));
			}

			private sealed class _TokenFilter_147 : TokenFilter
			{
				public _TokenFilter_147(TokenStream baseArg1) : base(baseArg1)
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
						this.payloadAtt.SetPayload(null);
						this.posIncrAtt.SetPositionIncrement(0);
						this.termAtt.SetEmpty().Append("b");
						this.state = null;
						return true;
					}
					bool hasNext = this.input.IncrementToken();
					if (!hasNext)
					{
						return false;
					}
					if (char.IsDigit(this.termAtt.Buffer()[0]))
					{
						this.posIncrAtt.SetPositionIncrement(this.termAtt.Buffer()[0] - '0');
					}
					if (this.first)
					{
						this.payloadAtt.SetPayload(new BytesRef(new byte[] { 100 }));
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

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestPreAnalyzedField()
		{
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new TextField("preanalyzed", new _TokenStream_223()));
			writer.AddDocument(doc);
			writer.Commit();
			SegmentCommitInfo info = writer.NewestSegment();
			writer.Close();
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
			reader.Close();
		}

		private sealed class _TokenStream_223 : TokenStream
		{
			public _TokenStream_223()
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
		/// <exception cref="System.Exception"></exception>
		public virtual void TestMixedTermVectorSettingsSameField()
		{
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			// f1 first without tv then with tv
			doc.Add(NewStringField("f1", "v1", Field.Store.YES));
			FieldType customType2 = new FieldType(StringField.TYPE_STORED);
			customType2.StoreTermVectors = true;
			customType2.StoreTermVectorOffsets = true;
			customType2.StoreTermVectorPositions = true;
			doc.Add(NewField("f1", "v2", customType2));
			// f2 first with tv then without tv
			doc.Add(NewField("f2", "v1", customType2));
			doc.Add(NewStringField("f2", "v2", Field.Store.YES));
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			writer.AddDocument(doc);
			writer.Close();
			TestUtil.CheckIndex(dir);
			IndexReader reader = DirectoryReader.Open(dir);
			// f1
			Terms tfv1 = reader.GetTermVectors(0).Terms("f1");
			IsNotNull(tfv1);
			AreEqual("the 'with_tv' setting should rule!", 2, tfv1.Size
				());
			// f2
			Terms tfv2 = reader.GetTermVectors(0).Terms("f2");
			IsNotNull(tfv2);
			AreEqual("the 'with_tv' setting should rule!", 2, tfv2.Size
				());
			reader.Close();
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
		/// <exception cref="System.Exception"></exception>
		public virtual void TestLUCENE_1590()
		{
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			// f1 has no norms
			FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
			customType.SetOmitNorms(true);
			FieldType customType2 = new FieldType();
			customType2.SetStored(true);
			doc.Add(NewField("f1", "v1", customType));
			doc.Add(NewField("f1", "v2", customType2));
			// f2 has no TF
			FieldType customType3 = new FieldType(TextField.TYPE_NOT_STORED);
			customType3.SetIndexOptions(FieldInfo.IndexOptions.DOCS_ONLY);
			Field f = NewField("f2", "v1", customType3);
			doc.Add(f);
			doc.Add(NewField("f2", "v2", customType2));
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			writer.AddDocument(doc);
			writer.ForceMerge(1);
			// be sure to have a single segment
			writer.Close();
			TestUtil.CheckIndex(dir);
			SegmentReader reader = GetOnlySegmentReader(DirectoryReader.Open(dir));
			FieldInfos fi = reader.GetFieldInfos();
			// f1
			IsFalse("f1 should have no norms", fi.FieldInfo("f1").HasNorms
				());
			AreEqual("omitTermFreqAndPositions field bit should not be set for f1"
				, FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS, fi.FieldInfo("f1").GetIndexOptions
				());
			// f2
			IsTrue("f2 should have norms", fi.FieldInfo("f2").HasNorms
				());
			AreEqual("omitTermFreqAndPositions field bit should be set for f2"
				, FieldInfo.IndexOptions.DOCS_ONLY, fi.FieldInfo("f2").GetIndexOptions());
			reader.Close();
		}
	}
}
