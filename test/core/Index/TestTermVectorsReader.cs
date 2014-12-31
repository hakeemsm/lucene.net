/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.IO;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Test.Analysis.Tokenattributes;
using Lucene.Net.Codecs;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;


namespace Lucene.Net.Test.Index
{
	public class TestTermVectorsReader : LuceneTestCase
	{
		private string[] testFields = new string[] { "f1", "f2", "f3", "f4" };

		private bool[] testFieldsStorePos = new bool[] { true, false, true, false };

		private bool[] testFieldsStoreOff = new bool[] { true, false, false, true };

		private string[] testTerms = new string[] { "this", "is", "a", "test" };

		private int[][] positions = new int[testTerms.Length][];

		private Directory dir;

		private SegmentCommitInfo seg;

		private FieldInfos fieldInfos = new FieldInfos(new FieldInfo[0]);

		private static int TERM_FREQ = 3;

		private class TestToken : Comparable<TestTermVectorsReader.TestToken>
		{
			internal string text;

			internal int pos;

			internal int startOffset;

			internal int endOffset;

			//Must be lexicographically sorted, will do in setup, versus trying to maintain here
			public virtual int CompareTo(TestTermVectorsReader.TestToken other)
			{
				return this.pos - other.pos;
			}

			internal TestToken(TestTermVectorsReader _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly TestTermVectorsReader _enclosing;
		}

		internal TestTermVectorsReader.TestToken[] tokens = new TestTermVectorsReader.TestToken
			[testTerms.Length * TERM_FREQ];

		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			Arrays.Sort(testTerms);
			int tokenUpto = 0;
			for (int i = 0; i < testTerms.Length; i++)
			{
				positions[i] = new int[TERM_FREQ];
				// first position must be 0
				for (int j = 0; j < TERM_FREQ; j++)
				{
					// positions are always sorted in increasing order
					positions[i][j] = (int)(j * 10 + Math.Random() * 10);
					TestTermVectorsReader.TestToken token = tokens[tokenUpto++] = new TestTermVectorsReader.TestToken
						(this);
					token.text = testTerms[i];
					token.pos = positions[i][j];
					token.startOffset = j * 10;
					token.endOffset = j * 10 + testTerms[i].Length;
				}
			}
			Arrays.Sort(tokens);
			dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)((IndexWriterConfig
				)NewIndexWriterConfig(TEST_VERSION_CURRENT, new TestTermVectorsReader.MyAnalyzer
				(this)).SetMaxBufferedDocs(-1)).SetMergePolicy(NewLogMergePolicy(false, 10)).UseCompoundFile = 
				(false)));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			for (int i_1 = 0; i_1 < testFields.Length; i_1++)
			{
				FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
				if (testFieldsStorePos[i_1] && testFieldsStoreOff[i_1])
				{
					customType.StoreTermVectors = true;
					customType.StoreTermVectorPositions = true;
					customType.StoreTermVectorOffsets = true;
				}
				else
				{
					if (testFieldsStorePos[i_1] && !testFieldsStoreOff[i_1])
					{
						customType.StoreTermVectors = true;
						customType.StoreTermVectorPositions = true;
					}
					else
					{
						if (!testFieldsStorePos[i_1] && testFieldsStoreOff[i_1])
						{
							customType.StoreTermVectors = true;
							customType.StoreTermVectorOffsets = true;
						}
						else
						{
							customType.StoreTermVectors = true;
						}
					}
				}
				doc.Add(new Field(testFields[i_1], string.Empty, customType));
			}
			//Create 5 documents for testing, they all have the same
			//terms
			for (int j_1 = 0; j_1 < 5; j_1++)
			{
				writer.AddDocument(doc);
			}
			writer.Commit();
			seg = writer.NewestSegment();
			writer.Dispose();
			fieldInfos = SegmentReader.ReadFieldInfos(seg);
		}

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			dir.Dispose();
			base.TearDown();
		}

		private class MyTokenizer : Tokenizer
		{
			private int tokenUpto;

			private readonly CharTermAttribute termAtt;

			private readonly PositionIncrementAttribute posIncrAtt;

			private readonly OffsetAttribute offsetAtt;

			protected MyTokenizer(TestTermVectorsReader _enclosing, StreamReader reader) : base
				(reader)
			{
				this._enclosing = _enclosing;
				this.termAtt = this.AddAttribute<CharTermAttribute>();
				this.posIncrAtt = this.AddAttribute<PositionIncrementAttribute>();
				this.offsetAtt = this.AddAttribute<OffsetAttribute>();
			}

			public override bool IncrementToken()
			{
				if (this.tokenUpto >= this._enclosing.tokens.Length)
				{
					return false;
				}
				else
				{
					TestTermVectorsReader.TestToken testToken = this._enclosing.tokens[this.tokenUpto
						++];
					this.ClearAttributes();
					this.termAtt.Append(testToken.text);
					this.offsetAtt.SetOffset(testToken.startOffset, testToken.endOffset);
					if (this.tokenUpto > 1)
					{
						this.posIncrAtt.PositionIncrement = (testToken.pos - this._enclosing.tokens[this.
							tokenUpto - 2].pos);
					}
					else
					{
						this.posIncrAtt.PositionIncrement = (testToken.pos + 1);
					}
					return true;
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Reset()
			{
				base.Reset();
				this.tokenUpto = 0;
			}

			private readonly TestTermVectorsReader _enclosing;
		}

		private class MyAnalyzer : Analyzer
		{
			protected override Analyzer.TokenStreamComponents CreateComponents(string fieldName
				, StreamReader reader)
			{
				return new Analyzer.TokenStreamComponents(new TestTermVectorsReader.MyTokenizer(this
					, reader));
			}

			internal MyAnalyzer(TestTermVectorsReader _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly TestTermVectorsReader _enclosing;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void Test()
		{
			//Check to see the files were created properly in setup
			DirectoryReader reader = DirectoryReader.Open(dir);
			foreach (AtomicReaderContext ctx in reader.Leaves)
			{
				SegmentReader sr = (SegmentReader)((AtomicReader)ctx.Reader);
				IsTrue(sr.FieldInfos.HasVectors);
			}
			reader.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestReader()
		{
			TermVectorsReader reader = Codec.GetDefault().TermVectorsFormat().VectorsReader(dir
				, seg.info, fieldInfos, NewIOContext(Random()));
			for (int j = 0; j < 5; j++)
			{
				Terms vector = reader.Get(j).Terms(testFields[0]);
				IsNotNull(vector);
				AreEqual(testTerms.Length, vector.Size());
				TermsEnum termsEnum = vector.IEnumerator(null);
				for (int i = 0; i < testTerms.Length; i++)
				{
					BytesRef text = termsEnum.Next();
					IsNotNull(text);
					string term = text.Utf8ToString();
					//System.out.println("Term: " + term);
					AreEqual(testTerms[i], term);
				}
				IsNull(termsEnum.Next());
			}
			reader.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestDocsEnum()
		{
			TermVectorsReader reader = Codec.GetDefault().TermVectorsFormat().VectorsReader(dir
				, seg.info, fieldInfos, NewIOContext(Random()));
			for (int j = 0; j < 5; j++)
			{
				Terms vector = reader.Get(j).Terms(testFields[0]);
				IsNotNull(vector);
				AreEqual(testTerms.Length, vector.Size());
				TermsEnum termsEnum = vector.IEnumerator(null);
				DocsEnum docsEnum = null;
				for (int i = 0; i < testTerms.Length; i++)
				{
					BytesRef text = termsEnum.Next();
					IsNotNull(text);
					string term = text.Utf8ToString();
					//System.out.println("Term: " + term);
					AreEqual(testTerms[i], term);
					docsEnum = TestUtil.Docs(Random(), termsEnum, null, docsEnum, DocsEnum.FLAG_NONE);
					IsNotNull(docsEnum);
					int doc = docsEnum.DocID;
					AreEqual(-1, doc);
					IsTrue(docsEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS
						);
					AreEqual(DocIdSetIterator.NO_MORE_DOCS, docsEnum.NextDoc()
						);
				}
				IsNull(termsEnum.Next());
			}
			reader.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestPositionReader()
		{
			TermVectorsReader reader = Codec.GetDefault().TermVectorsFormat().VectorsReader(dir
				, seg.info, fieldInfos, NewIOContext(Random()));
			BytesRef[] terms;
			Terms vector = reader.Get(0).Terms(testFields[0]);
			IsNotNull(vector);
			AreEqual(testTerms.Length, vector.Size());
			TermsEnum termsEnum = vector.IEnumerator(null);
			DocsAndPositionsEnum dpEnum = null;
			for (int i = 0; i < testTerms.Length; i++)
			{
				BytesRef text = termsEnum.Next();
				IsNotNull(text);
				string term = text.Utf8ToString();
				//System.out.println("Term: " + term);
				AreEqual(testTerms[i], term);
				dpEnum = termsEnum.DocsAndPositions(null, dpEnum);
				IsNotNull(dpEnum);
				int doc = dpEnum.DocID;
				AreEqual(-1, doc);
				IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
				AreEqual(dpEnum.Freq, positions[i].Length);
				for (int j = 0; j < positions[i].Length; j++)
				{
					AreEqual(positions[i][j], dpEnum.NextPosition());
				}
				AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum.NextDoc());
				dpEnum = termsEnum.DocsAndPositions(null, dpEnum);
				doc = dpEnum.DocID;
				AreEqual(-1, doc);
				IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
				IsNotNull(dpEnum);
				AreEqual(dpEnum.Freq, positions[i].Length);
				for (int j_1 = 0; j_1 < positions[i].Length; j_1++)
				{
					AreEqual(positions[i][j_1], dpEnum.NextPosition());
					AreEqual(j_1 * 10, dpEnum.StartOffset());
					AreEqual(j_1 * 10 + testTerms[i].Length, dpEnum.EndOffset(
						));
				}
				AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum.NextDoc());
			}
			Terms freqVector = reader.Get(0).Terms(testFields[1]);
			//no pos, no offset
			IsNotNull(freqVector);
			AreEqual(testTerms.Length, freqVector.Size());
			termsEnum = freqVector.IEnumerator(null);
			IsNotNull(termsEnum);
			for (int i_1 = 0; i_1 < testTerms.Length; i_1++)
			{
				BytesRef text = termsEnum.Next();
				IsNotNull(text);
				string term = text.Utf8ToString();
				//System.out.println("Term: " + term);
				AreEqual(testTerms[i_1], term);
				IsNotNull(termsEnum.Docs(null, null));
				IsNull(termsEnum.DocsAndPositions(null, null));
			}
			// no pos
			reader.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestOffsetReader()
		{
			TermVectorsReader reader = Codec.GetDefault().TermVectorsFormat().VectorsReader(dir
				, seg.info, fieldInfos, NewIOContext(Random()));
			Terms vector = reader.Get(0).Terms(testFields[0]);
			IsNotNull(vector);
			TermsEnum termsEnum = vector.IEnumerator(null);
			IsNotNull(termsEnum);
			AreEqual(testTerms.Length, vector.Size());
			DocsAndPositionsEnum dpEnum = null;
			for (int i = 0; i < testTerms.Length; i++)
			{
				BytesRef text = termsEnum.Next();
				IsNotNull(text);
				string term = text.Utf8ToString();
				AreEqual(testTerms[i], term);
				dpEnum = termsEnum.DocsAndPositions(null, dpEnum);
				IsNotNull(dpEnum);
				IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
				AreEqual(dpEnum.Freq, positions[i].Length);
				for (int j = 0; j < positions[i].Length; j++)
				{
					AreEqual(positions[i][j], dpEnum.NextPosition());
				}
				AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum.NextDoc());
				dpEnum = termsEnum.DocsAndPositions(null, dpEnum);
				IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
				IsNotNull(dpEnum);
				AreEqual(dpEnum.Freq, positions[i].Length);
				for (int j_1 = 0; j_1 < positions[i].Length; j_1++)
				{
					AreEqual(positions[i][j_1], dpEnum.NextPosition());
					AreEqual(j_1 * 10, dpEnum.StartOffset());
					AreEqual(j_1 * 10 + testTerms[i].Length, dpEnum.EndOffset(
						));
				}
				AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum.NextDoc());
			}
			reader.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestIllegalIndexableField()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), dir);
			FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
			ft.StoreTermVectors = true;
			ft.StoreTermVectorPayloads = true;
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new Field("field", "value", ft));
			try
			{
				w.AddDocument(doc);
				Fail("did not hit exception");
			}
			catch (ArgumentException iae)
			{
				// Expected
				AreEqual("cannot index term vector payloads without term vector positions (field=\"field\")"
					, iae.Message);
			}
			ft = new FieldType(TextField.TYPE_NOT_STORED);
			ft.StoreTermVectors = false;
			ft.StoreTermVectorOffsets = true;
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new Field("field", "value", ft));
			try
			{
				w.AddDocument(doc);
				Fail("did not hit exception");
			}
			catch (ArgumentException iae)
			{
				// Expected
				AreEqual("cannot index term vector offsets when term vectors are not indexed (field=\"field\")"
					, iae.Message);
			}
			ft = new FieldType(TextField.TYPE_NOT_STORED);
			ft.StoreTermVectors = false;
			ft.StoreTermVectorPositions = true;
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new Field("field", "value", ft));
			try
			{
				w.AddDocument(doc);
				Fail("did not hit exception");
			}
			catch (ArgumentException iae)
			{
				// Expected
				AreEqual("cannot index term vector positions when term vectors are not indexed (field=\"field\")"
					, iae.Message);
			}
			ft = new FieldType(TextField.TYPE_NOT_STORED);
			ft.StoreTermVectors = false;
			ft.StoreTermVectorPayloads = true;
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new Field("field", "value", ft));
			try
			{
				w.AddDocument(doc);
				Fail("did not hit exception");
			}
			catch (ArgumentException iae)
			{
				// Expected
				AreEqual("cannot index term vector payloads when term vectors are not indexed (field=\"field\")"
					, iae.Message);
			}
			w.Dispose();
			dir.Dispose();
		}
	}
}
