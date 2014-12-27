/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Test.Index
{
	public class TestPostingsOffsets : LuceneTestCase
	{
		internal IndexWriterConfig iwc;

		// TODO: we really need to test indexingoffsets, but then getting only docs / docs + freqs.
		// not all codecs store prx separate...
		// TODO: fix sep codec to index offsets so we can greatly reduce this list!
		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestBasic()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), dir, iwc);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
			ft.IndexOptions = (FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
				);
			if (Random().NextBoolean())
			{
				ft.StoreTermVectors = true;
				ft.StoreTermVectorPositions = (Random().NextBoolean());
				ft.SetStoreTermVectorOffsets(Random().NextBoolean());
			}
			Token[] tokens = new Token[] { MakeToken("a", 1, 0, 6), MakeToken("b", 1, 8, 9), 
				MakeToken("a", 1, 9, 17), MakeToken("c", 1, 19, 50) };
			doc.Add(new Field("content", new CannedTokenStream(tokens), ft));
			w.AddDocument(doc);
			IndexReader r = w.GetReader();
			w.Dispose();
			DocsAndPositionsEnum dp = MultiFields.GetTermPositionsEnum(r, null, "content", new 
				BytesRef("a"));
			IsNotNull(dp);
			AreEqual(0, dp.NextDoc());
			AreEqual(2, dp.Freq);
			AreEqual(0, dp.NextPosition());
			AreEqual(0, dp.StartOffset());
			AreEqual(6, dp.EndOffset());
			AreEqual(2, dp.NextPosition());
			AreEqual(9, dp.StartOffset());
			AreEqual(17, dp.EndOffset());
			AreEqual(DocIdSetIterator.NO_MORE_DOCS, dp.NextDoc());
			dp = MultiFields.GetTermPositionsEnum(r, null, "content", new BytesRef("b"));
			IsNotNull(dp);
			AreEqual(0, dp.NextDoc());
			AreEqual(1, dp.Freq);
			AreEqual(1, dp.NextPosition());
			AreEqual(8, dp.StartOffset());
			AreEqual(9, dp.EndOffset());
			AreEqual(DocIdSetIterator.NO_MORE_DOCS, dp.NextDoc());
			dp = MultiFields.GetTermPositionsEnum(r, null, "content", new BytesRef("c"));
			IsNotNull(dp);
			AreEqual(0, dp.NextDoc());
			AreEqual(1, dp.Freq);
			AreEqual(3, dp.NextPosition());
			AreEqual(19, dp.StartOffset());
			AreEqual(50, dp.EndOffset());
			AreEqual(DocIdSetIterator.NO_MORE_DOCS, dp.NextDoc());
			r.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSkipping()
		{
			DoTestNumbers(false);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestPayloads()
		{
			DoTestNumbers(true);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void DoTestNumbers(bool withPayloads)
		{
			Directory dir = NewDirectory();
			Analyzer analyzer = withPayloads ? new MockPayloadAnalyzer() : new MockAnalyzer(Random
				());
			iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			iwc.SetMergePolicy(NewLogMergePolicy());
			// will rely on docids a bit for skipping
			RandomIndexWriter w = new RandomIndexWriter(Random(), dir, iwc);
			FieldType ft = new FieldType(TextField.TYPE_STORED);
			ft.IndexOptions = (FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
				);
			if (Random().NextBoolean())
			{
				ft.StoreTermVectors = true;
				ft.SetStoreTermVectorOffsets(Random().NextBoolean());
				ft.StoreTermVectorPositions = (Random().NextBoolean());
			}
			int numDocs = AtLeast(500);
			for (int i = 0; i < numDocs; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(new Field("numbers", English.IntToEnglish(i), ft));
				doc.Add(new Field("oddeven", (i % 2) == 0 ? "even" : "odd", ft));
				doc.Add(new StringField("id", string.Empty + i, Field.Store.NO));
				w.AddDocument(doc);
			}
			IndexReader reader = w.GetReader();
			w.Dispose();
			string[] terms = new string[] { "one", "two", "three", "four", "five", "six", "seven"
				, "eight", "nine", "ten", "hundred" };
			foreach (string term in terms)
			{
				DocsAndPositionsEnum dp = MultiFields.GetTermPositionsEnum(reader, null, "numbers"
					, new BytesRef(term));
				int doc;
				while ((doc = dp.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
				{
					string storedNumbers = reader.Document(doc).Get("numbers");
					int freq = dp.Freq;
					for (int i_1 = 0; i_1 < freq; i_1++)
					{
						dp.NextPosition();
						int start = dp.StartOffset();
						//HM:revisit 
						//assert start >= 0;
						int end = dp.EndOffset();
						//HM:revisit 
						//assert end >= 0 && end >= start;
						// check that the offsets correspond to the term in the src text
						IsTrue(Sharpen.Runtime.Substring(storedNumbers, start, end
							).Equals(term));
						if (withPayloads)
						{
							// check that we have a payload and it starts with "pos"
							IsNotNull(dp.GetPayload());
							BytesRef payload = dp.GetPayload();
							IsTrue(payload.Utf8ToString().StartsWith("pos:"));
						}
					}
				}
			}
			// note: withPayloads=false doesnt necessarily mean we dont have them from MockAnalyzer!
			// check we can skip correctly
			int numSkippingTests = AtLeast(50);
			for (int j = 0; j < numSkippingTests; j++)
			{
				int num = TestUtil.NextInt(Random(), 100, Math.Min(numDocs - 1, 999));
				DocsAndPositionsEnum dp = MultiFields.GetTermPositionsEnum(reader, null, "numbers"
					, new BytesRef("hundred"));
				int doc = dp.Advance(num);
				AreEqual(num, doc);
				int freq = dp.Freq;
				for (int i_1 = 0; i_1 < freq; i_1++)
				{
					string storedNumbers = reader.Document(doc).Get("numbers");
					dp.NextPosition();
					int start = dp.StartOffset();
					//HM:revisit 
					//assert start >= 0;
					int end = dp.EndOffset();
					//HM:revisit 
					//assert end >= 0 && end >= start;
					// check that the offsets correspond to the term in the src text
					IsTrue(Sharpen.Runtime.Substring(storedNumbers, start, end
						).Equals("hundred"));
					if (withPayloads)
					{
						// check that we have a payload and it starts with "pos"
						IsNotNull(dp.GetPayload());
						BytesRef payload = dp.GetPayload();
						IsTrue(payload.Utf8ToString().StartsWith("pos:"));
					}
				}
			}
			// note: withPayloads=false doesnt necessarily mean we dont have them from MockAnalyzer!
			// check that other fields (without offsets) work correctly
			for (int i_2 = 0; i_2 < numDocs; i_2++)
			{
				DocsEnum dp = MultiFields.GetTermDocsEnum(reader, null, "id", new BytesRef(string.Empty
					 + i_2), 0);
				AreEqual(i_2, dp.NextDoc());
				AreEqual(DocIdSetIterator.NO_MORE_DOCS, dp.NextDoc());
			}
			reader.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestRandom()
		{
			// token -> docID -> tokens
			IDictionary<string, IDictionary<int, IList<Token>>> actualTokens = new Dictionary
				<string, IDictionary<int, IList<Token>>>();
			Directory dir = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), dir, iwc);
			int numDocs = AtLeast(20);
			//final int numDocs = atLeast(5);
			FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
			// TODO: randomize what IndexOptions we use; also test
			// changing this up in one IW buffered segment...:
			ft.IndexOptions = (FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
				);
			if (Random().NextBoolean())
			{
				ft.StoreTermVectors = true;
				ft.SetStoreTermVectorOffsets(Random().NextBoolean());
				ft.StoreTermVectorPositions = (Random().NextBoolean());
			}
			for (int docCount = 0; docCount < numDocs; docCount++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(new IntField("id", docCount, Field.Store.NO));
				IList<Token> tokens = new AList<Token>();
				int numTokens = AtLeast(100);
				//final int numTokens = atLeast(20);
				int pos = -1;
				int offset = 0;
				//System.out.println("doc id=" + docCount);
				for (int tokenCount = 0; tokenCount < numTokens; tokenCount++)
				{
					string text;
					if (Random().NextBoolean())
					{
						text = "a";
					}
					else
					{
						if (Random().NextBoolean())
						{
							text = "b";
						}
						else
						{
							if (Random().NextBoolean())
							{
								text = "c";
							}
							else
							{
								text = "d";
							}
						}
					}
					int posIncr = Random().NextBoolean() ? 1 : Random().Next(5);
					if (tokenCount == 0 && posIncr == 0)
					{
						posIncr = 1;
					}
					int offIncr = Random().NextBoolean() ? 0 : Random().Next(5);
					int tokenOffset = Random().Next(5);
					Token token = MakeToken(text, posIncr, offset + offIncr, offset + offIncr + tokenOffset
						);
					if (!actualTokens.ContainsKey(text))
					{
						actualTokens.Put(text, new Dictionary<int, IList<Token>>());
					}
					IDictionary<int, IList<Token>> postingsByDoc = actualTokens.Get(text);
					if (!postingsByDoc.ContainsKey(docCount))
					{
						postingsByDoc.Put(docCount, new AList<Token>());
					}
					postingsByDoc.Get(docCount).AddItem(token);
					tokens.AddItem(token);
					pos += posIncr;
					// stuff abs position into type:
					token.SetType(string.Empty + pos);
					offset += offIncr + tokenOffset;
				}
				//System.out.println("  " + token + " posIncr=" + token.getPositionIncrement() + " pos=" + pos + " off=" + token.startOffset() + "/" + token.endOffset() + " (freq=" + postingsByDoc.get(docCount).size() + ")");
				doc.Add(new Field("content", new CannedTokenStream(Sharpen.Collections.ToArray(tokens
					, new Token[tokens.Count])), ft));
				w.AddDocument(doc);
			}
			DirectoryReader r = w.GetReader();
			w.Dispose();
			string[] terms = new string[] { "a", "b", "c", "d" };
			foreach (AtomicReaderContext ctx in r.Leaves)
			{
				// TODO: improve this
				AtomicReader sub = ((AtomicReader)ctx.Reader);
				//System.out.println("\nsub=" + sub);
				TermsEnum termsEnum = sub.Fields().Terms("content").Iterator(null);
				DocsEnum docs = null;
				DocsAndPositionsEnum docsAndPositions = null;
				DocsAndPositionsEnum docsAndPositionsAndOffsets = null;
				FieldCache.Ints docIDToID = FieldCache.DEFAULT.GetInts(sub, "id", false);
				foreach (string term in terms)
				{
					//System.out.println("  term=" + term);
					if (termsEnum.SeekExact(new BytesRef(term)))
					{
						docs = termsEnum.Docs(null, docs);
						IsNotNull(docs);
						int doc;
						//System.out.println("    doc/freq");
						while ((doc = docs.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
						{
							IList<Token> expected = actualTokens.Get(term).Get(docIDToID.Get(doc));
							//System.out.println("      doc=" + docIDToID.get(doc) + " docID=" + doc + " " + expected.size() + " freq");
							IsNotNull(expected);
							AreEqual(expected.Count, docs.Freq);
						}
						// explicitly exclude offsets here
						docsAndPositions = termsEnum.DocsAndPositions(null, docsAndPositions, DocsAndPositionsEnum
							.FLAG_PAYLOADS);
						IsNotNull(docsAndPositions);
						//System.out.println("    doc/freq/pos");
						while ((doc = docsAndPositions.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
						{
							IList<Token> expected = actualTokens.Get(term).Get(docIDToID.Get(doc));
							//System.out.println("      doc=" + docIDToID.get(doc) + " " + expected.size() + " freq");
							IsNotNull(expected);
							AreEqual(expected.Count, docsAndPositions.Freq);
							foreach (Token token in expected)
							{
								int pos = System.Convert.ToInt32(token.Type());
								//System.out.println("        pos=" + pos);
								AreEqual(pos, docsAndPositions.NextPosition());
							}
						}
						docsAndPositionsAndOffsets = termsEnum.DocsAndPositions(null, docsAndPositions);
						IsNotNull(docsAndPositionsAndOffsets);
						//System.out.println("    doc/freq/pos/offs");
						while ((doc = docsAndPositionsAndOffsets.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS
							)
						{
							IList<Token> expected = actualTokens.Get(term).Get(docIDToID.Get(doc));
							//System.out.println("      doc=" + docIDToID.get(doc) + " " + expected.size() + " freq");
							IsNotNull(expected);
							AreEqual(expected.Count, docsAndPositionsAndOffsets.Freq
								);
							foreach (Token token in expected)
							{
								int pos = System.Convert.ToInt32(token.Type());
								//System.out.println("        pos=" + pos);
								AreEqual(pos, docsAndPositionsAndOffsets.NextPosition());
								AreEqual(token.StartOffset(), docsAndPositionsAndOffsets.StartOffset
									());
								AreEqual(token.EndOffset(), docsAndPositionsAndOffsets.EndOffset
									());
							}
						}
					}
				}
			}
			// TODO: test advance:
			r.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestWithUnindexedFields()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter riw = new RandomIndexWriter(Random(), dir, iwc);
			for (int i = 0; i < 100; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				// ensure at least one doc is indexed with offsets
				if (i < 99 && Random().Next(2) == 0)
				{
					// stored only
					FieldType ft = new FieldType();
					ft.Indexed(false);
					ft.Stored = (true);
					doc.Add(new Field("foo", "boo!", ft));
				}
				else
				{
					FieldType ft = new FieldType(TextField.TYPE_STORED);
					ft.IndexOptions = (FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
						);
					if (Random().NextBoolean())
					{
						// store some term vectors for the checkindex cross-check
						ft.StoreTermVectors = true;
						ft.StoreTermVectorPositions = true;
						ft.StoreTermVectorOffsets = true;
					}
					doc.Add(new Field("foo", "bar", ft));
				}
				riw.AddDocument(doc);
			}
			CompositeReader ir = riw.GetReader();
			AtomicReader slow = SlowCompositeReaderWrapper.Wrap(ir);
			FieldInfos fis = slow.GetFieldInfos();
			AreEqual(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
				, fis.FieldInfo("foo").GetIndexOptions());
			slow.Dispose();
			ir.Dispose();
			riw.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestAddFieldTwice()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			FieldType customType3 = new FieldType(TextField.TYPE_STORED);
			customType3.StoreTermVectors = true;
			customType3.StoreTermVectorPositions = true;
			customType3.StoreTermVectorOffsets = true;
			customType3.IndexOptions = (FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
				);
			doc.Add(new Field("content3", "here is more content with aaa aaa aaa", customType3
				));
			doc.Add(new Field("content3", "here is more content with aaa aaa aaa", customType3
				));
			iw.AddDocument(doc);
			iw.Dispose();
			dir.Dispose();
		}

		// checkindex
		// NOTE: the next two tests aren't that good as we need an EvilToken...
		/// <exception cref="System.Exception"></exception>
		public virtual void TestNegativeOffsets()
		{
			try
			{
				CheckTokens(new Token[] { MakeToken("foo", 1, -1, -1) });
				Fail();
			}
			catch (ArgumentException)
			{
			}
		}

		//expected
		/// <exception cref="System.Exception"></exception>
		public virtual void TestIllegalOffsets()
		{
			try
			{
				CheckTokens(new Token[] { MakeToken("foo", 1, 1, 0) });
				Fail();
			}
			catch (ArgumentException)
			{
			}
		}

		//expected
		/// <exception cref="System.Exception"></exception>
		public virtual void TestBackwardsOffsets()
		{
			try
			{
				CheckTokens(new Token[] { MakeToken("foo", 1, 0, 3), MakeToken("foo", 1, 4, 7), MakeToken
					("foo", 0, 3, 6) });
				Fail();
			}
			catch (ArgumentException)
			{
			}
		}

		// expected
		/// <exception cref="System.Exception"></exception>
		public virtual void TestStackedTokens()
		{
			CheckTokens(new Token[] { MakeToken("foo", 1, 0, 3), MakeToken("foo", 0, 0, 3), MakeToken
				("foo", 0, 0, 3) });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestLegalbutVeryLargeOffsets()
		{
			Directory dir = NewDirectory();
			IndexWriter iw = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, 
				null));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			Token t1 = new Token("foo", 0, int.MaxValue - 500);
			if (Random().NextBoolean())
			{
				t1.SetPayload(new BytesRef("test"));
			}
			Token t2 = new Token("foo", int.MaxValue - 500, int.MaxValue);
			TokenStream tokenStream = new CannedTokenStream(new Token[] { t1, t2 });
			FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
			ft.IndexOptions = (FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
				);
			// store some term vectors for the checkindex cross-check
			ft.StoreTermVectors = true;
			ft.StoreTermVectorPositions = true;
			ft.StoreTermVectorOffsets = true;
			Field field = new Field("foo", tokenStream, ft);
			doc.Add(field);
			iw.AddDocument(doc);
			iw.Dispose();
			dir.Dispose();
		}

		// TODO: more tests with other possibilities
		/// <exception cref="System.IO.IOException"></exception>
		private void CheckTokens(Token[] tokens)
		{
			Directory dir = NewDirectory();
			RandomIndexWriter riw = new RandomIndexWriter(Random(), dir, iwc);
			bool success = false;
			try
			{
				FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
				ft.IndexOptions = (FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
					);
				// store some term vectors for the checkindex cross-check
				ft.StoreTermVectors = true;
				ft.StoreTermVectorPositions = true;
				ft.StoreTermVectorOffsets = true;
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(new Field("body", new CannedTokenStream(tokens), ft));
				riw.AddDocument(doc);
				success = true;
			}
			finally
			{
				if (success)
				{
					IOUtils.Close(riw, dir);
				}
				else
				{
					IOUtils.CloseWhileHandlingException(riw, dir);
				}
			}
		}

		private Token MakeToken(string text, int posIncr, int startOffset, int endOffset)
		{
			Token t = new Token();
			t.Append(text);
			t.SetPositionIncrement(posIncr);
			t.SetOffset(startOffset, endOffset);
			return t;
		}
	}
}
