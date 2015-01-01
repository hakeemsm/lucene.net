using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Lucene3x;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.TestFramework;
using Lucene.Net.TestFramework.Index;
using Lucene.Net.TestFramework.Util;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Index
{
    [TestFixture]
	public class TestIndexableField : LuceneTestCase
	{
		private class MyField : IIndexableField
		{
			private readonly int counter;

			private sealed class AnonInnerField : IIndexableFieldType
			{
				public AnonInnerField(MyField _enclosing)
				{
					this._enclosing = _enclosing;
				}

				public bool Indexed
				{
				    get { return (this._enclosing.counter%10) != 3; }
				}

				public bool Stored
				{
				    get { return (this._enclosing.counter & 1) == 0 || (this._enclosing.counter%10) == 3; }
				}

				public bool Tokenized
				{
				    get { return true; }
				}

				public bool StoreTermVectors
				{
				    get
				    {
				        return this.Indexed && this._enclosing.counter%2 == 1 && this._enclosing.counter
				               %10 != 9;
				    }
				}

				public bool StoreTermVectorOffsets
				{
				    get { return this.StoreTermVectors && this._enclosing.counter%10 != 9; }
				}

				public bool StoreTermVectorPositions
				{
				    get { return this.StoreTermVectors && this._enclosing.counter%10 != 9; }
				}

				public bool StoreTermVectorPayloads
				{
				    get
				    {
				        if (Codec.Default is Lucene3xCodec)
				        {
				            return false;
				        }
				        // 3.x doesnt support
				        return this.StoreTermVectors && this._enclosing.counter%10 != 9;
				    }
				}

				public bool OmitNorms
				{
				    get { return false; }
				}

				public FieldInfo.IndexOptions IndexOptions
				{
				    get { return FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS; }
				}

				public FieldInfo.DocValuesType? DocValueType
				{
				    get { return null; }
				}

				private readonly MyField _enclosing;
			}

			private readonly IIndexableFieldType fieldType;

			public MyField(TestIndexableField _enclosing, int counter)
			{
				this._enclosing = _enclosing;
				fieldType = new AnonInnerField(this);
				this.counter = counter;
			}

			public virtual string Name
			{
			    get { return "f" + this.counter; }
			}

			public virtual float Boost
			{
			    get { return (float) (1.0f + LuceneTestCase.Random().NextDouble()); }
			}

			public virtual BytesRef BinaryValue
			{
			    get
			    {
			        if ((this.counter%10) == 3)
			        {
			            var bytes = new sbyte[10];
			            for (int idx = 0; idx < bytes.Length; idx++)
			            {
			                bytes[idx] = ((sbyte) (this.counter + idx));
			            }
			            return new BytesRef(bytes, 0, bytes.Length);
			        }
			        return null;
			    }
			}

			public virtual string StringValue
			{
			    get
			    {
			        int fieldID = this.counter%10;
			        if (fieldID != 3 && fieldID != 7)
			        {
			            return "text " + this.counter;
			        }
			        return null;
			    }
			}

			public virtual TextReader ReaderValue
			{
			    get
			    {
			        if (this.counter%10 == 7)
			        {
			            var s = "text " + this.counter;
			            var bytes = Array.ConvertAll(s.ToCharArray(), Convert.ToByte);
			            return new StreamReader(new MemoryStream(bytes));
			        }
			        return null;
			    }
			}

		    public virtual object NumericValue
			{
		        get { return null; }
			}

			public virtual IIndexableFieldType FieldTypeValue
			{
			    get { return this.fieldType; }
			}

			/// <exception cref="System.IO.IOException"></exception>
			public virtual Lucene.Net.Analysis.TokenStream TokenStream(Analyzer analyzer
				)
			{
				return this.ReaderValue != null ? analyzer.TokenStream(this.Name, this.ReaderValue) : analyzer.TokenStream(this.Name, new StringReader(this.StringValue));
			}

			private readonly TestIndexableField _enclosing;
		}

		// Silly test showing how to index documents w/o using Lucene's core
		// Document nor Field class
		[Test]
		public virtual void TestArbitraryFields()
		{
			var dir = NewDirectory();
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
                w.AddDocument(GetFieldList(fieldCount,finalDocCount,finalBaseCount));
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
				AreEqual(1, hits.TotalHits);
				int docID = hits.ScoreDocs[0].Doc;
				Lucene.Net.Documents.Document doc = s.Doc(docID);
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
						IIndexableField f = doc.GetField(name);
						AssertNotNull("doc " + id + " doesn't have field f" + counter, f);
						if (binary)
						{
							AssertNotNull("doc " + id + " doesn't have field f" + counter, f);
							BytesRef b = f.BinaryValue;
							IsNotNull(b);
							AreEqual(10, b.length);
							for (int idx = 0; idx < 10; idx++)
							{
								AreEqual(unchecked((byte)(idx + counter)), b.bytes[b.offset
									 + idx]);
							}
						}
						else
						{
							
							//assert stringValue != null;
							AreEqual(stringValue, f.StringValue);
						}
					}
					if (indexed)
					{
						bool tv = counter % 2 == 1 && fieldID != 9;
						if (tv)
						{
							Terms tfv = r.GetTermVectors(docID).Terms(name);
							IsNotNull(tfv);
							TermsEnum termsEnum = tfv.Iterator(null);
							AreEqual(new BytesRef(string.Empty + counter), termsEnum.Next
								());
							AreEqual(1, termsEnum.TotalTermFreq);
							DocsAndPositionsEnum dpEnum = termsEnum.DocsAndPositions(null, null);
							IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
							AreEqual(1, dpEnum.Freq);
							AreEqual(1, dpEnum.NextPosition());
							AreEqual(new BytesRef("text"), termsEnum.Next());
							AreEqual(1, termsEnum.TotalTermFreq);
							dpEnum = termsEnum.DocsAndPositions(null, dpEnum);
							IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
							AreEqual(1, dpEnum.Freq);
							AreEqual(0, dpEnum.NextPosition());
							IsNull(termsEnum.Next());
						}
						else
						{
							// TODO: offsets
							Fields vectors = r.GetTermVectors(docID);
							IsTrue(vectors == null || vectors.Terms(name) == null);
						}
						BooleanQuery bq = new BooleanQuery();
						bq.Add(new TermQuery(new Term("id", string.Empty + id)), Occur.MUST
							);
						bq.Add(new TermQuery(new Term(name, "text")), Occur.MUST);
						TopDocs hits2 = s.Search(bq, 1);
						AreEqual(1, hits2.TotalHits);
						AreEqual(docID, hits2.ScoreDocs[0].Doc);
						bq = new BooleanQuery();
						bq.Add(new TermQuery(new Term("id", string.Empty + id)), Occur.MUST
							);
						bq.Add(new TermQuery(new Term(name, string.Empty + counter)), Occur.MUST);
						TopDocs hits3 = s.Search(bq, 1);
						AreEqual(1, hits3.TotalHits);
						AreEqual(docID, hits3.ScoreDocs[0].Doc);
					}
					counter++;
				}
			}
			r.Dispose();
			dir.Dispose();
		}

        private IEnumerable<IIndexableField> GetFieldList(int fieldCount, int finalDocCount, int finalBaseCount)
        {
            int fieldUpto = 0;
            while (fieldUpto < fieldCount)
            {
                if (fieldUpto == 0)
                {
                    fieldUpto = 1;
                    yield return NewStringField("id", string.Empty + finalDocCount, Field.Store.YES);
                }
                else
                {
                    yield return new MyField(this, finalBaseCount + (fieldUpto++ -1));
                }
            }
        }
	}
}
