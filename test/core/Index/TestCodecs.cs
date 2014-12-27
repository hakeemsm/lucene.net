/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Lucene3x;
using Lucene.Net.Codecs.Lucene40;
using Lucene.Net.Codecs.Lucene41;
using Lucene.Net.Codecs.Lucene42;
using Lucene.Net.Codecs.Mocksep;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Test.Index
{
	public class TestCodecs : LuceneTestCase
	{
		private static string[] fieldNames = new string[] { "one", "two", "three", "four"
			 };

		private static int NUM_TEST_ITER;

		private const int NUM_TEST_THREADS = 3;

		private const int NUM_FIELDS = 4;

		private const int NUM_TERMS_RAND = 50;

		private const int DOC_FREQ_RAND = 500;

		private const int TERM_DOC_FREQ_RAND = 20;

		// TODO: test multiple codecs here?
		// TODO
		//   - test across fields
		//   - fix this test to run once for all codecs
		//   - make more docs per term, to test > 1 level skipping
		//   - test all combinations of payloads/not and omitTF/not
		//   - test w/ different indexDivisor
		//   - test field where payload length rarely changes
		//   - 0-term fields
		//   - seek/skip to same term/doc i'm already on
		//   - mix in deleted docs
		//   - seek, skip beyond end -- 
		//HM:revisit 
		//assert returns false
		//   - seek, skip to things that don't exist -- ensure it
		//     goes to 1 before next one known to exist
		//   - skipTo(term)
		//   - skipTo(doc)
		// must be > 16 to test skipping
		// must be > 16 to test skipping
		[NUnit.Framework.BeforeClass]
		public static void BeforeClass()
		{
			NUM_TEST_ITER = AtLeast(20);
		}

		internal class FieldData : Comparable<TestCodecs.FieldData>
		{
			internal readonly FieldInfo fieldInfo;

			internal readonly TestCodecs.TermData[] terms;

			internal readonly bool omitTF;

			internal readonly bool storePayloads;

			public FieldData(TestCodecs _enclosing, string name, FieldInfos.Builder fieldInfos
				, TestCodecs.TermData[] terms, bool omitTF, bool storePayloads)
			{
				this._enclosing = _enclosing;
				this.omitTF = omitTF;
				this.storePayloads = storePayloads;
				// TODO: change this test to use all three
				this.fieldInfo = fieldInfos.AddOrUpdate(name, new _IndexableFieldType_105(omitTF)
					);
				if (storePayloads)
				{
					this.fieldInfo.SetStorePayloads();
				}
				this.terms = terms;
				for (int i = 0; i < terms.Length; i++)
				{
					terms[i].field = this;
				}
				Arrays.Sort(terms);
			}

			private sealed class _IndexableFieldType_105 : IndexableFieldType
			{
				public _IndexableFieldType_105(bool omitTF)
				{
					this.omitTF = omitTF;
				}

				public bool Indexed()
				{
					return true;
				}

				public bool Stored()
				{
					return false;
				}

				public bool Tokenized()
				{
					return false;
				}

				public bool StoreTermVectors()
				{
					return false;
				}

				public bool StoreTermVectorOffsets()
				{
					return false;
				}

				public bool StoreTermVectorPositions()
				{
					return false;
				}

				public bool StoreTermVectorPayloads()
				{
					return false;
				}

				public bool OmitNorms()
				{
					return false;
				}

				public FieldInfo.IndexOptions IndexOptions()
				{
					return omitTF ? FieldInfo.IndexOptions.DOCS_ONLY : FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
				}

				public FieldInfo.DocValuesType DocValueType()
				{
					return null;
				}

				private readonly bool omitTF;
			}

			public virtual int CompareTo(TestCodecs.FieldData other)
			{
				return Sharpen.Runtime.CompareOrdinal(this.fieldInfo.name, other.fieldInfo.name);
			}

			/// <exception cref="System.Exception"></exception>
			public virtual void Write(FieldsConsumer consumer)
			{
				Arrays.Sort(this.terms);
				TermsConsumer termsConsumer = consumer.AddField(this.fieldInfo);
				long sumTotalTermCount = 0;
				long sumDF = 0;
				OpenBitSet visitedDocs = new OpenBitSet();
				foreach (TestCodecs.TermData term in this.terms)
				{
					for (int i = 0; i < term.docs.Length; i++)
					{
						visitedDocs.Set(term.docs[i]);
					}
					sumDF += term.docs.Length;
					sumTotalTermCount += term.Write(termsConsumer);
				}
				termsConsumer.Finish(this.omitTF ? -1 : sumTotalTermCount, sumDF, (int)visitedDocs
					.Cardinality());
			}

			private readonly TestCodecs _enclosing;
		}

		internal class PositionData
		{
			internal int pos;

			internal BytesRef payload;

			internal PositionData(TestCodecs _enclosing, int pos, BytesRef payload)
			{
				this._enclosing = _enclosing;
				this.pos = pos;
				this.payload = payload;
			}

			private readonly TestCodecs _enclosing;
		}

		internal class TermData : Comparable<TestCodecs.TermData>
		{
			internal string text2;

			internal readonly BytesRef text;

			internal int[] docs;

			internal TestCodecs.PositionData[][] positions;

			internal TestCodecs.FieldData field;

			public TermData(TestCodecs _enclosing, string text, int[] docs, TestCodecs.PositionData
				[][] positions)
			{
				this._enclosing = _enclosing;
				this.text = new BytesRef(text);
				this.text2 = text;
				this.docs = docs;
				this.positions = positions;
			}

			public virtual int CompareTo(TestCodecs.TermData o)
			{
				return this.text.CompareTo(o.text);
			}

			/// <exception cref="System.Exception"></exception>
			public virtual long Write(TermsConsumer termsConsumer)
			{
				PostingsConsumer postingsConsumer = termsConsumer.StartTerm(this.text);
				long totTF = 0;
				for (int i = 0; i < this.docs.Length; i++)
				{
					int termDocFreq;
					if (this.field.omitTF)
					{
						termDocFreq = -1;
					}
					else
					{
						termDocFreq = this.positions[i].Length;
					}
					postingsConsumer.StartDoc(this.docs[i], termDocFreq);
					if (!this.field.omitTF)
					{
						totTF += this.positions[i].Length;
						for (int j = 0; j < this.positions[i].Length; j++)
						{
							TestCodecs.PositionData pos = this.positions[i][j];
							postingsConsumer.AddPosition(pos.pos, pos.payload, -1, -1);
						}
					}
					postingsConsumer.FinishDoc();
				}
				termsConsumer.FinishTerm(this.text, new TermStats(this.docs.Length, this.field.omitTF
					 ? -1 : totTF));
				return totTF;
			}

			private readonly TestCodecs _enclosing;
		}

		private static readonly string SEGMENT = "0";

		internal virtual TestCodecs.TermData[] MakeRandomTerms(bool omitTF, bool storePayloads
			)
		{
			int numTerms = 1 + Random().Next(NUM_TERMS_RAND);
			//final int numTerms = 2;
			TestCodecs.TermData[] terms = new TestCodecs.TermData[numTerms];
			HashSet<string> termsSeen = new HashSet<string>();
			for (int i = 0; i < numTerms; i++)
			{
				// Make term text
				string text2;
				while (true)
				{
					text2 = TestUtil.RandomUnicodeString(Random());
					if (!termsSeen.Contains(text2) && !text2.EndsWith("."))
					{
						termsSeen.AddItem(text2);
						break;
					}
				}
				int docFreq = 1 + Random().Next(DOC_FREQ_RAND);
				int[] docs = new int[docFreq];
				TestCodecs.PositionData[][] positions;
				if (!omitTF)
				{
					positions = new TestCodecs.PositionData[docFreq][];
				}
				else
				{
					positions = null;
				}
				int docID = 0;
				for (int j = 0; j < docFreq; j++)
				{
					docID += TestUtil.NextInt(Random(), 1, 10);
					docs[j] = docID;
					if (!omitTF)
					{
						int termFreq = 1 + Random().Next(TERM_DOC_FREQ_RAND);
						positions[j] = new TestCodecs.PositionData[termFreq];
						int position = 0;
						for (int k = 0; k < termFreq; k++)
						{
							position += TestUtil.NextInt(Random(), 1, 10);
							BytesRef payload;
							if (storePayloads && Random().Next(4) == 0)
							{
								byte[] bytes = new byte[1 + Random().Next(5)];
								for (int l = 0; l < bytes.Length; l++)
								{
									bytes[l] = unchecked((byte)Random().Next(255));
								}
								payload = new BytesRef(bytes);
							}
							else
							{
								payload = null;
							}
							positions[j][k] = new TestCodecs.PositionData(this, position, payload);
						}
					}
				}
				terms[i] = new TestCodecs.TermData(this, text2, docs, positions);
			}
			return terms;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestFixedPostings()
		{
			int NUM_TERMS = 100;
			TestCodecs.TermData[] terms = new TestCodecs.TermData[NUM_TERMS];
			for (int i = 0; i < NUM_TERMS; i++)
			{
				int[] docs = new int[] { i };
				string text = Sharpen.Extensions.ToString(i, char.MAX_RADIX);
				terms[i] = new TestCodecs.TermData(this, text, docs, null);
			}
			FieldInfos.Builder builder = new FieldInfos.Builder();
			TestCodecs.FieldData field = new TestCodecs.FieldData(this, "field", builder, terms
				, true, false);
			TestCodecs.FieldData[] fields = new TestCodecs.FieldData[] { field };
			FieldInfos fieldInfos = builder.Finish();
			Directory dir = NewDirectory();
			this.Write(fieldInfos, dir, fields, true);
			Codec codec = Codec.GetDefault();
			SegmentInfo si = new SegmentInfo(dir, Constants.LUCENE_MAIN_VERSION, SEGMENT, 10000
				, false, codec, null);
			FieldsProducer reader = codec.PostingsFormat().FieldsProducer(new SegmentReadState
				(dir, si, fieldInfos, NewIOContext(Random()), DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR
				));
			Iterator<string> fieldsEnum = reader.Iterator();
			string fieldName = fieldsEnum.Next();
			IsNotNull(fieldName);
			Terms terms2 = reader.Terms(fieldName);
			IsNotNull(terms2);
			TermsEnum termsEnum = terms2.Iterator(null);
			DocsEnum docsEnum = null;
			for (int i_1 = 0; i_1 < NUM_TERMS; i_1++)
			{
				BytesRef term = termsEnum.Next();
				IsNotNull(term);
				AreEqual(terms[i_1].text2, term.Utf8ToString());
				// do this twice to stress test the codec's reuse, ie,
				// make sure it properly fully resets (rewinds) its
				// internal state:
				for (int iter = 0; iter < 2; iter++)
				{
					docsEnum = TestUtil.Docs(Random(), termsEnum, null, docsEnum, DocsEnum.FLAG_NONE);
					AreEqual(terms[i_1].docs[0], docsEnum.NextDoc());
					AreEqual(DocIdSetIterator.NO_MORE_DOCS, docsEnum.NextDoc()
						);
				}
			}
			IsNull(termsEnum.Next());
			for (int i_2 = 0; i_2 < NUM_TERMS; i_2++)
			{
				AreEqual(termsEnum.SeekCeil(new BytesRef(terms[i_2].text2)
					), TermsEnum.SeekStatus.FOUND);
			}
			IsFalse(fieldsEnum.HasNext());
			reader.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestRandomPostings()
		{
			FieldInfos.Builder builder = new FieldInfos.Builder();
			TestCodecs.FieldData[] fields = new TestCodecs.FieldData[NUM_FIELDS];
			for (int i = 0; i < NUM_FIELDS; i++)
			{
				bool omitTF = 0 == (i % 3);
				bool storePayloads = 1 == (i % 3);
				fields[i] = new TestCodecs.FieldData(this, fieldNames[i], builder, this.MakeRandomTerms
					(omitTF, storePayloads), omitTF, storePayloads);
			}
			Directory dir = NewDirectory();
			FieldInfos fieldInfos = builder.Finish();
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: now write postings");
			}
			this.Write(fieldInfos, dir, fields, false);
			Codec codec = Codec.GetDefault();
			SegmentInfo si = new SegmentInfo(dir, Constants.LUCENE_MAIN_VERSION, SEGMENT, 10000
				, false, codec, null);
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: now read postings");
			}
			FieldsProducer terms = codec.PostingsFormat().FieldsProducer(new SegmentReadState
				(dir, si, fieldInfos, NewIOContext(Random()), DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR
				));
			TestCodecs.Verify[] threads = new TestCodecs.Verify[NUM_TEST_THREADS - 1];
			for (int i_1 = 0; i_1 < NUM_TEST_THREADS - 1; i_1++)
			{
				threads[i_1] = new TestCodecs.Verify(this, si, fields, terms);
				threads[i_1].SetDaemon(true);
				threads[i_1].Start();
			}
			new TestCodecs.Verify(this, si, fields, terms).Run();
			for (int i_2 = 0; i_2 < NUM_TEST_THREADS - 1; i_2++)
			{
				threads[i_2].Join();
			}
			//HM:revisit 
			//assert !threads[i].failed;
			terms.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestSepPositionAfterMerge()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig config = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			config.SetMergePolicy(NewLogMergePolicy());
			config.SetCodec(TestUtil.AlwaysPostingsFormat(new MockSepPostingsFormat()));
			IndexWriter writer = new IndexWriter(dir, config);
			try
			{
				PhraseQuery pq = new PhraseQuery();
				pq.Add(new Term("content", "bbb"));
				pq.Add(new Term("content", "ccc"));
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
				customType.OmitNorms = (true);
				doc.Add(NewField("content", "aaa bbb ccc ddd", customType));
				// add document and force commit for creating a first segment
				writer.AddDocument(doc);
				writer.Commit();
				ScoreDoc[] results = this.Search(writer, pq, 5);
				AreEqual(1, results.Length);
				AreEqual(0, results[0].Doc);
				// add document and force commit for creating a second segment
				writer.AddDocument(doc);
				writer.Commit();
				// at this point, there should be at least two segments
				results = this.Search(writer, pq, 5);
				AreEqual(2, results.Length);
				AreEqual(0, results[0].Doc);
				writer.ForceMerge(1);
				// optimise to merge the segments.
				results = this.Search(writer, pq, 5);
				AreEqual(2, results.Length);
				AreEqual(0, results[0].Doc);
			}
			finally
			{
				writer.Dispose();
				dir.Dispose();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private ScoreDoc[] Search(IndexWriter writer, Query q, int n)
		{
			IndexReader reader = writer.GetReader();
			IndexSearcher searcher = NewSearcher(reader);
			try
			{
				return searcher.Search(q, null, n).ScoreDocs;
			}
			finally
			{
				reader.Dispose();
			}
		}

		private class Verify : Sharpen.Thread
		{
			internal readonly Fields termsDict;

			internal readonly TestCodecs.FieldData[] fields;

			internal readonly SegmentInfo si;

			internal volatile bool failed;

			internal Verify(TestCodecs _enclosing, SegmentInfo si, TestCodecs.FieldData[] fields
				, Fields termsDict)
			{
				this._enclosing = _enclosing;
				this.fields = fields;
				this.termsDict = termsDict;
				this.si = si;
			}

			public override void Run()
			{
				try
				{
					this._run();
				}
				catch (Exception t)
				{
					this.failed = true;
					throw new RuntimeException(t);
				}
			}

			/// <exception cref="System.Exception"></exception>
			private void VerifyDocs(int[] docs, TestCodecs.PositionData[][] positions, DocsEnum
				 docsEnum, bool doPos)
			{
				for (int i = 0; i < docs.Length; i++)
				{
					int doc = docsEnum.NextDoc();
					IsTrue(doc != DocIdSetIterator.NO_MORE_DOCS);
					AreEqual(docs[i], doc);
					if (doPos)
					{
						this.VerifyPositions(positions[i], ((DocsAndPositionsEnum)docsEnum));
					}
				}
				AreEqual(DocIdSetIterator.NO_MORE_DOCS, docsEnum.NextDoc()
					);
			}

			internal byte[] data = new byte[10];

			/// <exception cref="System.Exception"></exception>
			private void VerifyPositions(TestCodecs.PositionData[] positions, DocsAndPositionsEnum
				 posEnum)
			{
				for (int i = 0; i < positions.Length; i++)
				{
					int pos = posEnum.NextPosition();
					AreEqual(positions[i].pos, pos);
					if (positions[i].payload != null)
					{
						IsNotNull(posEnum.GetPayload());
						if (LuceneTestCase.Random().Next(3) < 2)
						{
							// Verify the payload bytes
							BytesRef otherPayload = posEnum.GetPayload();
							IsTrue("expected=" + positions[i].payload.ToString() + " got="
								 + otherPayload.ToString(), positions[i].payload.Equals(otherPayload));
						}
					}
					else
					{
						IsNull(posEnum.GetPayload());
					}
				}
			}

			/// <exception cref="System.Exception"></exception>
			public virtual void _run()
			{
				for (int iter = 0; iter < TestCodecs.NUM_TEST_ITER; iter++)
				{
					TestCodecs.FieldData field = this.fields[LuceneTestCase.Random().Next(this.fields
						.Length)];
					TermsEnum termsEnum = this.termsDict.Terms(field.fieldInfo.name).Iterator(null);
					if (this.si.Codec is Lucene3xCodec)
					{
						// code below expects unicode sort order
						continue;
					}
					int upto = 0;
					// Test straight enum of the terms:
					while (true)
					{
						BytesRef term = termsEnum.Next();
						if (term == null)
						{
							break;
						}
						BytesRef expected = new BytesRef(field.terms[upto++].text2);
						IsTrue("expected=" + expected + " vs actual " + term, expected
							.BytesEquals(term));
					}
					AreEqual(upto, field.terms.Length);
					// Test random seek:
					TestCodecs.TermData term_1 = field.terms[LuceneTestCase.Random().Next(field.terms
						.Length)];
					TermsEnum.SeekStatus status = termsEnum.SeekCeil(new BytesRef(term_1.text2));
					AreEqual(status, TermsEnum.SeekStatus.FOUND);
					AreEqual(term_1.docs.Length, termsEnum.DocFreq);
					if (field.omitTF)
					{
						this.VerifyDocs(term_1.docs, term_1.positions, TestUtil.Docs(LuceneTestCase.Random
							(), termsEnum, null, null, DocsEnum.FLAG_NONE), false);
					}
					else
					{
						this.VerifyDocs(term_1.docs, term_1.positions, termsEnum.DocsAndPositions(null, null
							), true);
					}
					// Test random seek by ord:
					int idx = LuceneTestCase.Random().Next(field.terms.Length);
					term_1 = field.terms[idx];
					bool success = false;
					try
					{
						termsEnum.SeekExact(idx);
						success = true;
					}
					catch (NotSupportedException)
					{
					}
					// ok -- skip it
					if (success)
					{
						AreEqual(status, TermsEnum.SeekStatus.FOUND);
						IsTrue(termsEnum.Term().BytesEquals(new BytesRef(term_1.text2
							)));
						AreEqual(term_1.docs.Length, termsEnum.DocFreq);
						if (field.omitTF)
						{
							this.VerifyDocs(term_1.docs, term_1.positions, TestUtil.Docs(LuceneTestCase.Random
								(), termsEnum, null, null, DocsEnum.FLAG_NONE), false);
						}
						else
						{
							this.VerifyDocs(term_1.docs, term_1.positions, termsEnum.DocsAndPositions(null, null
								), true);
						}
					}
					// Test seek to non-existent terms:
					if (LuceneTestCase.VERBOSE)
					{
						System.Console.Out.WriteLine("TEST: seek non-exist terms");
					}
					for (int i = 0; i < 100; i++)
					{
						string text2 = TestUtil.RandomUnicodeString(LuceneTestCase.Random()) + ".";
						status = termsEnum.SeekCeil(new BytesRef(text2));
						IsTrue(status == TermsEnum.SeekStatus.NOT_FOUND || status 
							== TermsEnum.SeekStatus.END);
					}
					// Seek to each term, backwards:
					if (LuceneTestCase.VERBOSE)
					{
						System.Console.Out.WriteLine("TEST: seek terms backwards");
					}
					for (int i_1 = field.terms.Length - 1; i_1 >= 0; i_1--)
					{
						AreEqual(Sharpen.Thread.CurrentThread().GetName() + ": field="
							 + field.fieldInfo.name + " term=" + field.terms[i_1].text2, TermsEnum.SeekStatus
							.FOUND, termsEnum.SeekCeil(new BytesRef(field.terms[i_1].text2)));
						AreEqual(field.terms[i_1].docs.Length, termsEnum.DocFreq
							);
					}
					// Seek to each term by ord, backwards
					for (int i_2 = field.terms.Length - 1; i_2 >= 0; i_2--)
					{
						try
						{
							termsEnum.SeekExact(i_2);
							AreEqual(field.terms[i_2].docs.Length, termsEnum.DocFreq
								);
							IsTrue(termsEnum.Term().BytesEquals(new BytesRef(field.terms
								[i_2].text2)));
						}
						catch (NotSupportedException)
						{
						}
					}
					// Seek to non-existent empty-string term
					status = termsEnum.SeekCeil(new BytesRef(string.Empty));
					IsNotNull(status);
					//assertEquals(TermsEnum.SeekStatus.NOT_FOUND, status);
					// Make sure we're now pointing to first term
					IsTrue(termsEnum.Term().BytesEquals(new BytesRef(field.terms
						[0].text2)));
					// Test docs enum
					termsEnum.SeekCeil(new BytesRef(string.Empty));
					upto = 0;
					do
					{
						term_1 = field.terms[upto];
						if (LuceneTestCase.Random().Next(3) == 1)
						{
							DocsEnum docs;
							DocsEnum docsAndFreqs;
							DocsAndPositionsEnum postings;
							if (!field.omitTF)
							{
								postings = termsEnum.DocsAndPositions(null, null);
								if (postings != null)
								{
									docs = docsAndFreqs = postings;
								}
								else
								{
									docs = docsAndFreqs = TestUtil.Docs(LuceneTestCase.Random(), termsEnum, null, null
										, DocsEnum.FLAG_FREQS);
								}
							}
							else
							{
								postings = null;
								docsAndFreqs = null;
								docs = TestUtil.Docs(LuceneTestCase.Random(), termsEnum, null, null, DocsEnum.FLAG_NONE
									);
							}
							IsNotNull(docs);
							int upto2 = -1;
							bool ended = false;
							while (upto2 < term_1.docs.Length - 1)
							{
								// Maybe skip:
								int left = term_1.docs.Length - upto2;
								int doc;
								if (LuceneTestCase.Random().Next(3) == 1 && left >= 1)
								{
									int inc = 1 + LuceneTestCase.Random().Next(left - 1);
									upto2 += inc;
									if (LuceneTestCase.Random().Next(2) == 1)
									{
										doc = docs.Advance(term_1.docs[upto2]);
										AreEqual(term_1.docs[upto2], doc);
									}
									else
									{
										doc = docs.Advance(1 + term_1.docs[upto2]);
										if (doc == DocIdSetIterator.NO_MORE_DOCS)
										{
											// skipped past last doc
											//HM:revisit 
											//assert upto2 == term.docs.length-1;
											ended = true;
											break;
										}
										else
										{
											// skipped to next doc
											//HM:revisit 
											//assert upto2 < term.docs.length-1;
											if (doc >= term_1.docs[1 + upto2])
											{
												upto2++;
											}
										}
									}
								}
								else
								{
									doc = docs.NextDoc();
									IsTrue(doc != -1);
									upto2++;
								}
								AreEqual(term_1.docs[upto2], doc);
								if (!field.omitTF)
								{
									AreEqual(term_1.positions[upto2].Length, postings.Freq);
									if (LuceneTestCase.Random().Next(2) == 1)
									{
										this.VerifyPositions(term_1.positions[upto2], postings);
									}
								}
							}
							if (!ended)
							{
								AreEqual(DocIdSetIterator.NO_MORE_DOCS, docs.NextDoc());
							}
						}
						upto++;
					}
					while (termsEnum.Next() != null);
					AreEqual(upto, field.terms.Length);
				}
			}

			private readonly TestCodecs _enclosing;
		}

		/// <exception cref="System.Exception"></exception>
		private void Write(FieldInfos fieldInfos, Directory dir, TestCodecs.FieldData[] fields
			, bool allowPreFlex)
		{
			int termIndexInterval = TestUtil.NextInt(Random(), 13, 27);
			Codec codec = Codec.GetDefault();
			SegmentInfo si = new SegmentInfo(dir, Constants.LUCENE_MAIN_VERSION, SEGMENT, 10000
				, false, codec, null);
			SegmentWriteState state = new SegmentWriteState(InfoStream.GetDefault(), dir, si, 
				fieldInfos, termIndexInterval, null, NewIOContext(Random()));
			FieldsConsumer consumer = codec.PostingsFormat().FieldsConsumer(state);
			Arrays.Sort(fields);
			foreach (TestCodecs.FieldData field in fields)
			{
				if (!allowPreFlex && codec is Lucene3xCodec)
				{
					// code below expects unicode sort order
					continue;
				}
				field.Write(consumer);
			}
			consumer.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestDocsOnlyFreq()
		{
			// tests that when fields are indexed with DOCS_ONLY, the Codec
			// returns 1 in docsEnum.freq()
			Directory dir = NewDirectory();
			Random random = Random();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(random)));
			// we don't need many documents to 
			//HM:revisit 
			//assert this, but don't use one document either
			int numDocs = AtLeast(random, 50);
			for (int i = 0; i < numDocs; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(new StringField("f", "doc", Field.Store.NO));
				writer.AddDocument(doc);
			}
			writer.Dispose();
			Term term = new Term("f", new BytesRef("doc"));
			DirectoryReader reader = DirectoryReader.Open(dir);
			foreach (AtomicReaderContext ctx in reader.Leaves)
			{
				DocsEnum de = ((AtomicReader)ctx.Reader).TermDocsEnum(term);
				while (de.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
				{
					AreEqual("wrong freq for doc " + de.DocID, 1, de.Freq);
				}
			}
			reader.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestDisableImpersonation()
		{
			Codec[] oldCodecs = new Codec[] { new Lucene40RWCodec(), new Lucene41RWCodec(), new 
				Lucene42RWCodec() };
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			conf.SetCodec(oldCodecs[Random().Next(oldCodecs.Length)]);
			IndexWriter writer = new IndexWriter(dir, conf);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new StringField("f", "bar", Field.Store.YES));
			doc.Add(new NumericDocValuesField("n", 18L));
			writer.AddDocument(doc);
			OLD_FORMAT_IMPERSONATION_IS_ACTIVE = false;
			try
			{
				writer.Dispose();
				Fail("should not have succeeded to impersonate an old format!"
					);
			}
			catch (NotSupportedException)
			{
				writer.Rollback();
			}
			finally
			{
				OLD_FORMAT_IMPERSONATION_IS_ACTIVE = true;
			}
			dir.Dispose();
		}
	}
}
