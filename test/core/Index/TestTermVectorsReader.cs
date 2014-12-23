/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Codecs;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
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
				(this)).SetMaxBufferedDocs(-1)).SetMergePolicy(NewLogMergePolicy(false, 10)).SetUseCompoundFile
				(false)));
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			for (int i_1 = 0; i_1 < testFields.Length; i_1++)
			{
				FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
				if (testFieldsStorePos[i_1] && testFieldsStoreOff[i_1])
				{
					customType.SetStoreTermVectors(true);
					customType.SetStoreTermVectorPositions(true);
					customType.SetStoreTermVectorOffsets(true);
				}
				else
				{
					if (testFieldsStorePos[i_1] && !testFieldsStoreOff[i_1])
					{
						customType.SetStoreTermVectors(true);
						customType.SetStoreTermVectorPositions(true);
					}
					else
					{
						if (!testFieldsStorePos[i_1] && testFieldsStoreOff[i_1])
						{
							customType.SetStoreTermVectors(true);
							customType.SetStoreTermVectorOffsets(true);
						}
						else
						{
							customType.SetStoreTermVectors(true);
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
			writer.Close();
			fieldInfos = SegmentReader.ReadFieldInfos(seg);
		}

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			dir.Close();
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
						this.posIncrAtt.SetPositionIncrement(testToken.pos - this._enclosing.tokens[this.
							tokenUpto - 2].pos);
					}
					else
					{
						this.posIncrAtt.SetPositionIncrement(testToken.pos + 1);
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
			foreach (AtomicReaderContext ctx in reader.Leaves())
			{
				SegmentReader sr = (SegmentReader)((AtomicReader)ctx.Reader());
				NUnit.Framework.Assert.IsTrue(sr.GetFieldInfos().HasVectors());
			}
			reader.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestReader()
		{
			TermVectorsReader reader = Codec.GetDefault().TermVectorsFormat().VectorsReader(dir
				, seg.info, fieldInfos, NewIOContext(Random()));
			for (int j = 0; j < 5; j++)
			{
				Terms vector = reader.Get(j).Terms(testFields[0]);
				NUnit.Framework.Assert.IsNotNull(vector);
				NUnit.Framework.Assert.AreEqual(testTerms.Length, vector.Size());
				TermsEnum termsEnum = vector.Iterator(null);
				for (int i = 0; i < testTerms.Length; i++)
				{
					BytesRef text = termsEnum.Next();
					NUnit.Framework.Assert.IsNotNull(text);
					string term = text.Utf8ToString();
					//System.out.println("Term: " + term);
					NUnit.Framework.Assert.AreEqual(testTerms[i], term);
				}
				NUnit.Framework.Assert.IsNull(termsEnum.Next());
			}
			reader.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestDocsEnum()
		{
			TermVectorsReader reader = Codec.GetDefault().TermVectorsFormat().VectorsReader(dir
				, seg.info, fieldInfos, NewIOContext(Random()));
			for (int j = 0; j < 5; j++)
			{
				Terms vector = reader.Get(j).Terms(testFields[0]);
				NUnit.Framework.Assert.IsNotNull(vector);
				NUnit.Framework.Assert.AreEqual(testTerms.Length, vector.Size());
				TermsEnum termsEnum = vector.Iterator(null);
				DocsEnum docsEnum = null;
				for (int i = 0; i < testTerms.Length; i++)
				{
					BytesRef text = termsEnum.Next();
					NUnit.Framework.Assert.IsNotNull(text);
					string term = text.Utf8ToString();
					//System.out.println("Term: " + term);
					NUnit.Framework.Assert.AreEqual(testTerms[i], term);
					docsEnum = TestUtil.Docs(Random(), termsEnum, null, docsEnum, DocsEnum.FLAG_NONE);
					NUnit.Framework.Assert.IsNotNull(docsEnum);
					int doc = docsEnum.DocID();
					NUnit.Framework.Assert.AreEqual(-1, doc);
					NUnit.Framework.Assert.IsTrue(docsEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS
						);
					NUnit.Framework.Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, docsEnum.NextDoc()
						);
				}
				NUnit.Framework.Assert.IsNull(termsEnum.Next());
			}
			reader.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestPositionReader()
		{
			TermVectorsReader reader = Codec.GetDefault().TermVectorsFormat().VectorsReader(dir
				, seg.info, fieldInfos, NewIOContext(Random()));
			BytesRef[] terms;
			Terms vector = reader.Get(0).Terms(testFields[0]);
			NUnit.Framework.Assert.IsNotNull(vector);
			NUnit.Framework.Assert.AreEqual(testTerms.Length, vector.Size());
			TermsEnum termsEnum = vector.Iterator(null);
			DocsAndPositionsEnum dpEnum = null;
			for (int i = 0; i < testTerms.Length; i++)
			{
				BytesRef text = termsEnum.Next();
				NUnit.Framework.Assert.IsNotNull(text);
				string term = text.Utf8ToString();
				//System.out.println("Term: " + term);
				NUnit.Framework.Assert.AreEqual(testTerms[i], term);
				dpEnum = termsEnum.DocsAndPositions(null, dpEnum);
				NUnit.Framework.Assert.IsNotNull(dpEnum);
				int doc = dpEnum.DocID();
				NUnit.Framework.Assert.AreEqual(-1, doc);
				NUnit.Framework.Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
				NUnit.Framework.Assert.AreEqual(dpEnum.Freq(), positions[i].Length);
				for (int j = 0; j < positions[i].Length; j++)
				{
					NUnit.Framework.Assert.AreEqual(positions[i][j], dpEnum.NextPosition());
				}
				NUnit.Framework.Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum.NextDoc());
				dpEnum = termsEnum.DocsAndPositions(null, dpEnum);
				doc = dpEnum.DocID();
				NUnit.Framework.Assert.AreEqual(-1, doc);
				NUnit.Framework.Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
				NUnit.Framework.Assert.IsNotNull(dpEnum);
				NUnit.Framework.Assert.AreEqual(dpEnum.Freq(), positions[i].Length);
				for (int j_1 = 0; j_1 < positions[i].Length; j_1++)
				{
					NUnit.Framework.Assert.AreEqual(positions[i][j_1], dpEnum.NextPosition());
					NUnit.Framework.Assert.AreEqual(j_1 * 10, dpEnum.StartOffset());
					NUnit.Framework.Assert.AreEqual(j_1 * 10 + testTerms[i].Length, dpEnum.EndOffset(
						));
				}
				NUnit.Framework.Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum.NextDoc());
			}
			Terms freqVector = reader.Get(0).Terms(testFields[1]);
			//no pos, no offset
			NUnit.Framework.Assert.IsNotNull(freqVector);
			NUnit.Framework.Assert.AreEqual(testTerms.Length, freqVector.Size());
			termsEnum = freqVector.Iterator(null);
			NUnit.Framework.Assert.IsNotNull(termsEnum);
			for (int i_1 = 0; i_1 < testTerms.Length; i_1++)
			{
				BytesRef text = termsEnum.Next();
				NUnit.Framework.Assert.IsNotNull(text);
				string term = text.Utf8ToString();
				//System.out.println("Term: " + term);
				NUnit.Framework.Assert.AreEqual(testTerms[i_1], term);
				NUnit.Framework.Assert.IsNotNull(termsEnum.Docs(null, null));
				NUnit.Framework.Assert.IsNull(termsEnum.DocsAndPositions(null, null));
			}
			// no pos
			reader.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestOffsetReader()
		{
			TermVectorsReader reader = Codec.GetDefault().TermVectorsFormat().VectorsReader(dir
				, seg.info, fieldInfos, NewIOContext(Random()));
			Terms vector = reader.Get(0).Terms(testFields[0]);
			NUnit.Framework.Assert.IsNotNull(vector);
			TermsEnum termsEnum = vector.Iterator(null);
			NUnit.Framework.Assert.IsNotNull(termsEnum);
			NUnit.Framework.Assert.AreEqual(testTerms.Length, vector.Size());
			DocsAndPositionsEnum dpEnum = null;
			for (int i = 0; i < testTerms.Length; i++)
			{
				BytesRef text = termsEnum.Next();
				NUnit.Framework.Assert.IsNotNull(text);
				string term = text.Utf8ToString();
				NUnit.Framework.Assert.AreEqual(testTerms[i], term);
				dpEnum = termsEnum.DocsAndPositions(null, dpEnum);
				NUnit.Framework.Assert.IsNotNull(dpEnum);
				NUnit.Framework.Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
				NUnit.Framework.Assert.AreEqual(dpEnum.Freq(), positions[i].Length);
				for (int j = 0; j < positions[i].Length; j++)
				{
					NUnit.Framework.Assert.AreEqual(positions[i][j], dpEnum.NextPosition());
				}
				NUnit.Framework.Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum.NextDoc());
				dpEnum = termsEnum.DocsAndPositions(null, dpEnum);
				NUnit.Framework.Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
				NUnit.Framework.Assert.IsNotNull(dpEnum);
				NUnit.Framework.Assert.AreEqual(dpEnum.Freq(), positions[i].Length);
				for (int j_1 = 0; j_1 < positions[i].Length; j_1++)
				{
					NUnit.Framework.Assert.AreEqual(positions[i][j_1], dpEnum.NextPosition());
					NUnit.Framework.Assert.AreEqual(j_1 * 10, dpEnum.StartOffset());
					NUnit.Framework.Assert.AreEqual(j_1 * 10 + testTerms[i].Length, dpEnum.EndOffset(
						));
				}
				NUnit.Framework.Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum.NextDoc());
			}
			reader.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestIllegalIndexableField()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), dir);
			FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
			ft.SetStoreTermVectors(true);
			ft.SetStoreTermVectorPayloads(true);
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			doc.Add(new Field("field", "value", ft));
			try
			{
				w.AddDocument(doc);
				NUnit.Framework.Assert.Fail("did not hit exception");
			}
			catch (ArgumentException iae)
			{
				// Expected
				NUnit.Framework.Assert.AreEqual("cannot index term vector payloads without term vector positions (field=\"field\")"
					, iae.Message);
			}
			ft = new FieldType(TextField.TYPE_NOT_STORED);
			ft.SetStoreTermVectors(false);
			ft.SetStoreTermVectorOffsets(true);
			doc = new Lucene.Net.Document.Document();
			doc.Add(new Field("field", "value", ft));
			try
			{
				w.AddDocument(doc);
				NUnit.Framework.Assert.Fail("did not hit exception");
			}
			catch (ArgumentException iae)
			{
				// Expected
				NUnit.Framework.Assert.AreEqual("cannot index term vector offsets when term vectors are not indexed (field=\"field\")"
					, iae.Message);
			}
			ft = new FieldType(TextField.TYPE_NOT_STORED);
			ft.SetStoreTermVectors(false);
			ft.SetStoreTermVectorPositions(true);
			doc = new Lucene.Net.Document.Document();
			doc.Add(new Field("field", "value", ft));
			try
			{
				w.AddDocument(doc);
				NUnit.Framework.Assert.Fail("did not hit exception");
			}
			catch (ArgumentException iae)
			{
				// Expected
				NUnit.Framework.Assert.AreEqual("cannot index term vector positions when term vectors are not indexed (field=\"field\")"
					, iae.Message);
			}
			ft = new FieldType(TextField.TYPE_NOT_STORED);
			ft.SetStoreTermVectors(false);
			ft.SetStoreTermVectorPayloads(true);
			doc = new Lucene.Net.Document.Document();
			doc.Add(new Field("field", "value", ft));
			try
			{
				w.AddDocument(doc);
				NUnit.Framework.Assert.Fail("did not hit exception");
			}
			catch (ArgumentException iae)
			{
				// Expected
				NUnit.Framework.Assert.AreEqual("cannot index term vector payloads when term vectors are not indexed (field=\"field\")"
					, iae.Message);
			}
			w.Close();
			dir.Close();
		}
	}
}
