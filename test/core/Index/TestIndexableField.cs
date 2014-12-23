/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Lucene3x;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	public class TestIndexableField : LuceneTestCase
	{
		private class MyField : IndexableField
		{
			private readonly int counter;

			private sealed class _IndexableFieldType_48 : IndexableFieldType
			{
				public _IndexableFieldType_48(MyField _enclosing)
				{
					this._enclosing = _enclosing;
				}

				public bool Indexed()
				{
					return (this._enclosing.counter % 10) != 3;
				}

				public bool Stored()
				{
					return (this._enclosing.counter & 1) == 0 || (this._enclosing.counter % 10) == 3;
				}

				public bool Tokenized()
				{
					return true;
				}

				public bool StoreTermVectors()
				{
					return this.Indexed() && this._enclosing.counter % 2 == 1 && this._enclosing.counter
						 % 10 != 9;
				}

				public bool StoreTermVectorOffsets()
				{
					return this.StoreTermVectors() && this._enclosing.counter % 10 != 9;
				}

				public bool StoreTermVectorPositions()
				{
					return this.StoreTermVectors() && this._enclosing.counter % 10 != 9;
				}

				public bool StoreTermVectorPayloads()
				{
					if (Codec.GetDefault() is Lucene3xCodec)
					{
						return false;
					}
					else
					{
						// 3.x doesnt support
						return this.StoreTermVectors() && this._enclosing.counter % 10 != 9;
					}
				}

				public bool OmitNorms()
				{
					return false;
				}

				public FieldInfo.IndexOptions IndexOptions()
				{
					return FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
				}

				public FieldInfo.DocValuesType DocValueType()
				{
					return null;
				}

				private readonly MyField _enclosing;
			}

			private readonly IndexableFieldType fieldType;

			public MyField(TestIndexableField _enclosing, int counter)
			{
				this._enclosing = _enclosing;
				fieldType = new _IndexableFieldType_48(this);
				this.counter = counter;
			}

			public virtual string Name()
			{
				return "f" + this.counter;
			}

			public virtual float Boost()
			{
				return 1.0f + LuceneTestCase.Random().NextFloat();
			}

			public virtual BytesRef BinaryValue()
			{
				if ((this.counter % 10) == 3)
				{
					byte[] bytes = new byte[10];
					for (int idx = 0; idx < bytes.Length; idx++)
					{
						bytes[idx] = unchecked((byte)(this.counter + idx));
					}
					return new BytesRef(bytes, 0, bytes.Length);
				}
				else
				{
					return null;
				}
			}

			public virtual string StringValue()
			{
				int fieldID = this.counter % 10;
				if (fieldID != 3 && fieldID != 7)
				{
					return "text " + this.counter;
				}
				else
				{
					return null;
				}
			}

			public virtual StreamReader ReaderValue()
			{
				if (this.counter % 10 == 7)
				{
					return new StringReader("text " + this.counter);
				}
				else
				{
					return null;
				}
			}

			public virtual Number NumericValue()
			{
				return null;
			}

			public virtual IndexableFieldType FieldType()
			{
				return this.fieldType;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public virtual Lucene.Net.Analysis.TokenStream TokenStream(Analyzer analyzer
				)
			{
				return this.ReaderValue() != null ? analyzer.TokenStream(this.Name(), this.ReaderValue
					()) : analyzer.TokenStream(this.Name(), new StringReader(this.StringValue()));
			}

			private readonly TestIndexableField _enclosing;
		}

		// Silly test showing how to index documents w/o using Lucene's core
		// Document nor Field class
		/// <exception cref="System.Exception"></exception>
		public virtual void TestArbitraryFields()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), dir);
			int NUM_DOCS = AtLeast(27);
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: " + NUM_DOCS + " docs");
			}
			int[] fieldsPerDoc = new int[NUM_DOCS];
			int baseCount = 0;
			for (int docCount = 0; docCount < NUM_DOCS; docCount++)
			{
				int fieldCount = TestUtil.NextInt(Random(), 1, 17);
				fieldsPerDoc[docCount] = fieldCount - 1;
				int finalDocCount = docCount;
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: " + fieldCount + " fields in doc " + docCount
						);
				}
				int finalBaseCount = baseCount;
				baseCount += fieldCount - 1;
				w.AddDocument(new _Iterable_193(fieldCount, finalDocCount, finalBaseCount));
			}
			//HM:revisit 
			//assert fieldUpto < fieldCount;
			IndexReader r = w.GetReader();
			w.Close();
			IndexSearcher s = NewSearcher(r);
			int counter = 0;
			for (int id = 0; id < NUM_DOCS; id++)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: verify doc id=" + id + " (" + fieldsPerDoc[id
						] + " fields) counter=" + counter);
				}
				TopDocs hits = s.Search(new TermQuery(new Term("id", string.Empty + id)), 1);
				NUnit.Framework.Assert.AreEqual(1, hits.totalHits);
				int docID = hits.scoreDocs[0].doc;
				Lucene.Net.Document.Document doc = s.Doc(docID);
				int endCounter = counter + fieldsPerDoc[id];
				while (counter < endCounter)
				{
					string name = "f" + counter;
					int fieldID = counter % 10;
					bool stored = (counter & 1) == 0 || fieldID == 3;
					bool binary = fieldID == 3;
					bool indexed = fieldID != 3;
					string stringValue;
					if (fieldID != 3 && fieldID != 9)
					{
						stringValue = "text " + counter;
					}
					else
					{
						stringValue = null;
					}
					// stored:
					if (stored)
					{
						IndexableField f = doc.GetField(name);
						NUnit.Framework.Assert.IsNotNull("doc " + id + " doesn't have field f" + counter, 
							f);
						if (binary)
						{
							NUnit.Framework.Assert.IsNotNull("doc " + id + " doesn't have field f" + counter, 
								f);
							BytesRef b = f.BinaryValue();
							NUnit.Framework.Assert.IsNotNull(b);
							NUnit.Framework.Assert.AreEqual(10, b.length);
							for (int idx = 0; idx < 10; idx++)
							{
								NUnit.Framework.Assert.AreEqual(unchecked((byte)(idx + counter)), b.bytes[b.offset
									 + idx]);
							}
						}
						else
						{
							//HM:revisit 
							//assert stringValue != null;
							NUnit.Framework.Assert.AreEqual(stringValue, f.StringValue());
						}
					}
					if (indexed)
					{
						bool tv = counter % 2 == 1 && fieldID != 9;
						if (tv)
						{
							Terms tfv = r.GetTermVectors(docID).Terms(name);
							NUnit.Framework.Assert.IsNotNull(tfv);
							TermsEnum termsEnum = tfv.Iterator(null);
							NUnit.Framework.Assert.AreEqual(new BytesRef(string.Empty + counter), termsEnum.Next
								());
							NUnit.Framework.Assert.AreEqual(1, termsEnum.TotalTermFreq());
							DocsAndPositionsEnum dpEnum = termsEnum.DocsAndPositions(null, null);
							NUnit.Framework.Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
							NUnit.Framework.Assert.AreEqual(1, dpEnum.Freq());
							NUnit.Framework.Assert.AreEqual(1, dpEnum.NextPosition());
							NUnit.Framework.Assert.AreEqual(new BytesRef("text"), termsEnum.Next());
							NUnit.Framework.Assert.AreEqual(1, termsEnum.TotalTermFreq());
							dpEnum = termsEnum.DocsAndPositions(null, dpEnum);
							NUnit.Framework.Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
							NUnit.Framework.Assert.AreEqual(1, dpEnum.Freq());
							NUnit.Framework.Assert.AreEqual(0, dpEnum.NextPosition());
							NUnit.Framework.Assert.IsNull(termsEnum.Next());
						}
						else
						{
							// TODO: offsets
							Fields vectors = r.GetTermVectors(docID);
							NUnit.Framework.Assert.IsTrue(vectors == null || vectors.Terms(name) == null);
						}
						BooleanQuery bq = new BooleanQuery();
						bq.Add(new TermQuery(new Term("id", string.Empty + id)), BooleanClause.Occur.MUST
							);
						bq.Add(new TermQuery(new Term(name, "text")), BooleanClause.Occur.MUST);
						TopDocs hits2 = s.Search(bq, 1);
						NUnit.Framework.Assert.AreEqual(1, hits2.totalHits);
						NUnit.Framework.Assert.AreEqual(docID, hits2.scoreDocs[0].doc);
						bq = new BooleanQuery();
						bq.Add(new TermQuery(new Term("id", string.Empty + id)), BooleanClause.Occur.MUST
							);
						bq.Add(new TermQuery(new Term(name, string.Empty + counter)), BooleanClause.Occur
							.MUST);
						TopDocs hits3 = s.Search(bq, 1);
						NUnit.Framework.Assert.AreEqual(1, hits3.totalHits);
						NUnit.Framework.Assert.AreEqual(docID, hits3.scoreDocs[0].doc);
					}
					counter++;
				}
			}
			r.Close();
			dir.Close();
		}

		private sealed class _Iterable_193 : Iterable<IndexableField>
		{
			public _Iterable_193(int fieldCount, int finalDocCount, int finalBaseCount)
			{
				this.fieldCount = fieldCount;
				this.finalDocCount = finalDocCount;
				this.finalBaseCount = finalBaseCount;
			}

			public override Iterator<IndexableField> Iterator()
			{
				return new _Iterator_196(fieldCount, finalDocCount, finalBaseCount);
			}

			private sealed class _Iterator_196 : Iterator<IndexableField>
			{
				public _Iterator_196(int fieldCount, int finalDocCount, int finalBaseCount)
				{
					this.fieldCount = fieldCount;
					this.finalDocCount = finalDocCount;
					this.finalBaseCount = finalBaseCount;
				}

				internal int fieldUpto;

				public override bool HasNext()
				{
					return this.fieldUpto < fieldCount;
				}

				public override IndexableField Next()
				{
					if (this.fieldUpto == 0)
					{
						this.fieldUpto = 1;
						return LuceneTestCase.NewStringField("id", string.Empty + finalDocCount, Field.Store
							.YES);
					}
					else
					{
						return new TestIndexableField.MyField(this, finalBaseCount + (this.fieldUpto++ - 
							1));
					}
				}

				public override void Remove()
				{
					throw new NotSupportedException();
				}

				private readonly int fieldCount;

				private readonly int finalDocCount;

				private readonly int finalBaseCount;
			}

			private readonly int fieldCount;

			private readonly int finalDocCount;

			private readonly int finalBaseCount;
		}
	}
}
