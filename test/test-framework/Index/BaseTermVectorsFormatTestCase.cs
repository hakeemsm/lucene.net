/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Com.Carrotsearch.Randomizedtesting.Generators;
using Lucene.Net.TestFramework.Analysis;
using Lucene.Net.TestFramework.Analysis.Tokenattributes;
using Lucene.Net.Documents;
using Lucene.Net.TestFramework.Index;
using Lucene.Net.TestFramework.Search;
using Lucene.Net.TestFramework.Store;
using Lucene.Net.TestFramework.Util;
using Sharpen;

namespace Lucene.Net.TestFramework.Index
{
	/// <summary>
	/// Base class aiming at testing
	/// <see cref="Lucene.NetCodecs.TermVectorsFormat">term vectors formats</see>
	/// .
	/// To test a new format, all you need is to register a new
	/// <see cref="Lucene.NetCodecs.Codec">Lucene.NetCodecs.Codec</see>
	/// which
	/// uses it and extend this class and override
	/// <see cref="BaseIndexFileFormatTestCase.GetCodec()">BaseIndexFileFormatTestCase.GetCodec()
	/// 	</see>
	/// .
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public abstract class BaseTermVectorsFormatTestCase : BaseIndexFileFormatTestCase
	{
		/// <summary>A combination of term vectors options.</summary>
		/// <remarks>A combination of term vectors options.</remarks>
		protected internal enum Options
		{
			NONE,
			POSITIONS,
			OFFSETS,
			POSITIONS_AND_OFFSETS,
			POSITIONS_AND_PAYLOADS,
			POSITIONS_AND_OFFSETS_AND_PAYLOADS
		}

		protected internal virtual ICollection<BaseTermVectorsFormatTestCase.Options> ValidOptions
			()
		{
			return EnumSet.AllOf<BaseTermVectorsFormatTestCase.Options>();
		}

		protected internal virtual BaseTermVectorsFormatTestCase.Options RandomOptions()
		{
			return RandomPicks.RandomFrom(Random(), new AList<BaseTermVectorsFormatTestCase.Options
				>(ValidOptions()));
		}

		protected internal virtual Lucene.Net.Documents.FieldType FieldType(BaseTermVectorsFormatTestCase.Options
			 options)
		{
			Lucene.Net.Documents.FieldType ft = new Lucene.Net.Documents.FieldType
				(TextField.TYPE_NOT_STORED);
			ft.StoreTermVectors = (true);
			ft.StoreTermVectorPositions = (options.positions);
			ft.StoreTermVectorOffsets = (options.offsets);
			ft.SetStoreTermVectorPayloads(options.payloads);
			ft.Freeze();
			return ft;
		}

		protected internal virtual BytesRef RandomPayload()
		{
			int len = Random().Next(5);
			if (len == 0)
			{
				return null;
			}
			BytesRef payload = new BytesRef(len);
			Random().NextBytes(payload.bytes);
			payload.length = len;
			return payload;
		}

		protected internal override void AddRandomFields(Lucene.Net.Documents.Document
			 doc)
		{
			foreach (BaseTermVectorsFormatTestCase.Options opts in ValidOptions())
			{
				Lucene.Net.Documents.FieldType ft = FieldType(opts);
				int numFields = Random().Next(5);
				for (int j = 0; j < numFields; ++j)
				{
					doc.Add(new Field("f_" + opts, TestUtil.RandomSimpleString(Random(), 2), ft));
				}
			}
		}

		private class PermissiveOffsetAttributeImpl : AttributeImpl, OffsetAttribute
		{
			internal int start;

			internal int end;

			// custom impl to test cases that are forbidden by the default OffsetAttribute impl
			public virtual int StartOffset()
			{
				return start;
			}

			public virtual int EndOffset()
			{
				return end;
			}

			public virtual void SetOffset(int startOffset, int endOffset)
			{
				// no check!
				start = startOffset;
				end = endOffset;
			}

			public override void Clear()
			{
				start = end = 0;
			}

			public override bool Equals(object other)
			{
				if (other == this)
				{
					return true;
				}
				if (other is BaseTermVectorsFormatTestCase.PermissiveOffsetAttributeImpl)
				{
					BaseTermVectorsFormatTestCase.PermissiveOffsetAttributeImpl o = (BaseTermVectorsFormatTestCase.PermissiveOffsetAttributeImpl
						)other;
					return o.start == start && o.end == end;
				}
				return false;
			}

			public override int GetHashCode()
			{
				return start + 31 * end;
			}

			public override void CopyTo(AttributeImpl target)
			{
				OffsetAttribute t = (OffsetAttribute)target;
				t.SetOffset(start, end);
			}
		}

		protected internal class RandomTokenStream : TokenStream
		{
			internal readonly string[] terms;

			internal readonly BytesRef[] termBytes;

			internal readonly int[] positionsIncrements;

			internal readonly int[] positions;

			internal readonly int[] startOffsets;

			internal readonly int[] endOffsets;

			internal readonly BytesRef[] payloads;

			internal readonly IDictionary<string, int> freqs;

			internal readonly IDictionary<int, ICollection<int>> positionToTerms;

			internal readonly IDictionary<int, ICollection<int>> startOffsetToTerms;

			internal readonly CharTermAttribute termAtt;

			internal readonly PositionIncrementAttribute piAtt;

			internal readonly OffsetAttribute oAtt;

			internal readonly PayloadAttribute pAtt;

			internal int i = 0;

			protected internal RandomTokenStream(BaseTermVectorsFormatTestCase _enclosing, int
				 len, string[] sampleTerms, BytesRef[] sampleTermBytes) : this(len, sampleTerms, 
				sampleTermBytes, LuceneTestCase.Rarely())
			{
				this._enclosing = _enclosing;
			}

			protected internal RandomTokenStream(BaseTermVectorsFormatTestCase _enclosing, int
				 len, string[] sampleTerms, BytesRef[] sampleTermBytes, bool offsetsGoBackwards)
			{
				this._enclosing = _enclosing;
				// TODO: use CannedTokenStream?
				this.terms = new string[len];
				this.termBytes = new BytesRef[len];
				this.positionsIncrements = new int[len];
				this.positions = new int[len];
				this.startOffsets = new int[len];
				this.endOffsets = new int[len];
				this.payloads = new BytesRef[len];
				for (int i = 0; i < len; ++i)
				{
					int o = LuceneTestCase.Random().Next(sampleTerms.Length);
					this.terms[i] = sampleTerms[o];
					this.termBytes[i] = sampleTermBytes[o];
					this.positionsIncrements[i] = TestUtil.NextInt(LuceneTestCase.Random(), i == 0 ? 
						1 : 0, 10);
					if (offsetsGoBackwards)
					{
						this.startOffsets[i] = LuceneTestCase.Random().Next();
						this.endOffsets[i] = LuceneTestCase.Random().Next();
					}
					else
					{
						if (i == 0)
						{
							this.startOffsets[i] = TestUtil.NextInt(LuceneTestCase.Random(), 0, 1 << 16);
						}
						else
						{
							this.startOffsets[i] = this.startOffsets[i - 1] + TestUtil.NextInt(LuceneTestCase
								.Random(), 0, LuceneTestCase.Rarely() ? 1 << 16 : 20);
						}
						this.endOffsets[i] = this.startOffsets[i] + TestUtil.NextInt(LuceneTestCase.Random
							(), 0, LuceneTestCase.Rarely() ? 1 << 10 : 20);
					}
				}
				for (int i_1 = 0; i_1 < len; ++i_1)
				{
					if (i_1 == 0)
					{
						this.positions[i_1] = this.positionsIncrements[i_1] - 1;
					}
					else
					{
						this.positions[i_1] = this.positions[i_1 - 1] + this.positionsIncrements[i_1];
					}
				}
				if (LuceneTestCase.Rarely())
				{
					Arrays.Fill(this.payloads, this._enclosing.RandomPayload());
				}
				else
				{
					for (int i_2 = 0; i_2 < len; ++i_2)
					{
						this.payloads[i_2] = this._enclosing.RandomPayload();
					}
				}
				this.positionToTerms = new Dictionary<int, ICollection<int>>(len);
				this.startOffsetToTerms = new Dictionary<int, ICollection<int>>(len);
				for (int i_3 = 0; i_3 < len; ++i_3)
				{
					if (!this.positionToTerms.ContainsKey(this.positions[i_3]))
					{
						this.positionToTerms.Put(this.positions[i_3], new HashSet<int>(1));
					}
					this.positionToTerms.Get(this.positions[i_3]).AddItem(i_3);
					if (!this.startOffsetToTerms.ContainsKey(this.startOffsets[i_3]))
					{
						this.startOffsetToTerms.Put(this.startOffsets[i_3], new HashSet<int>(1));
					}
					this.startOffsetToTerms.Get(this.startOffsets[i_3]).AddItem(i_3);
				}
				this.freqs = new Dictionary<string, int>();
				foreach (string term in this.terms)
				{
					if (this.freqs.ContainsKey(term))
					{
						this.freqs.Put(term, this.freqs.Get(term) + 1);
					}
					else
					{
						this.freqs.Put(term, 1);
					}
				}
				this.AddAttributeImpl(new BaseTermVectorsFormatTestCase.PermissiveOffsetAttributeImpl
					());
				this.termAtt = this.AddAttribute<CharTermAttribute>();
				this.piAtt = this.AddAttribute<PositionIncrementAttribute>();
				this.oAtt = this.AddAttribute<OffsetAttribute>();
				this.pAtt = this.AddAttribute<PayloadAttribute>();
			}

			public virtual bool HasPayloads()
			{
				foreach (BytesRef payload in this.payloads)
				{
					if (payload != null && payload.length > 0)
					{
						return true;
					}
				}
				return false;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public sealed override bool IncrementToken()
			{
				if (this.i < this.terms.Length)
				{
					this.termAtt.SetLength(0).Append(this.terms[this.i]);
					this.piAtt.SetPositionIncrement(this.positionsIncrements[this.i]);
					this.oAtt.SetOffset(this.startOffsets[this.i], this.endOffsets[this.i]);
					this.pAtt.SetPayload(this.payloads[this.i]);
					++this.i;
					return true;
				}
				else
				{
					return false;
				}
			}

			private readonly BaseTermVectorsFormatTestCase _enclosing;
		}

		protected internal class RandomDocument
		{
			private readonly string[] fieldNames;

			private readonly FieldType[] fieldTypes;

			private readonly BaseTermVectorsFormatTestCase.RandomTokenStream[] tokenStreams;

			protected internal RandomDocument(BaseTermVectorsFormatTestCase _enclosing, int fieldCount
				, int maxTermCount, BaseTermVectorsFormatTestCase.Options options, string[] fieldNames
				, string[] sampleTerms, BytesRef[] sampleTermBytes)
			{
				this._enclosing = _enclosing;
				if (fieldCount > fieldNames.Length)
				{
					throw new ArgumentException();
				}
				this.fieldNames = new string[fieldCount];
				this.fieldTypes = new FieldType[fieldCount];
				this.tokenStreams = new BaseTermVectorsFormatTestCase.RandomTokenStream[fieldCount
					];
				Arrays.Fill(this.fieldTypes, this._enclosing.FieldType(options));
				ICollection<string> usedFileNames = new HashSet<string>();
				for (int i = 0; i < fieldCount; ++i)
				{
					do
					{
						this.fieldNames[i] = RandomPicks.RandomFrom(LuceneTestCase.Random(), fieldNames);
					}
					while (usedFileNames.Contains(this.fieldNames[i]));
					usedFileNames.AddItem(this.fieldNames[i]);
					this.tokenStreams[i] = new BaseTermVectorsFormatTestCase.RandomTokenStream(this, 
						TestUtil.NextInt(LuceneTestCase.Random(), 1, maxTermCount), sampleTerms, sampleTermBytes
						);
				}
			}

			public virtual Lucene.Net.Documents.Document ToDocument()
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				for (int i = 0; i < this.fieldNames.Length; ++i)
				{
					doc.Add(new Field(this.fieldNames[i], this.tokenStreams[i], this.fieldTypes[i]));
				}
				return doc;
			}

			private readonly BaseTermVectorsFormatTestCase _enclosing;
		}

		protected internal class RandomDocumentFactory
		{
			private readonly string[] fieldNames;

			private readonly string[] terms;

			private readonly BytesRef[] termBytes;

			protected internal RandomDocumentFactory(BaseTermVectorsFormatTestCase _enclosing
				, int distinctFieldNames, int disctinctTerms)
			{
				this._enclosing = _enclosing;
				ICollection<string> fieldNames = new HashSet<string>();
				while (fieldNames.Count < distinctFieldNames)
				{
					fieldNames.AddItem(TestUtil.RandomSimpleString(LuceneTestCase.Random()));
					fieldNames.Remove("id");
				}
				this.fieldNames = Sharpen.Collections.ToArray(fieldNames, new string[0]);
				this.terms = new string[disctinctTerms];
				this.termBytes = new BytesRef[disctinctTerms];
				for (int i = 0; i < disctinctTerms; ++i)
				{
					this.terms[i] = TestUtil.RandomRealisticUnicodeString(LuceneTestCase.Random());
					this.termBytes[i] = new BytesRef(this.terms[i]);
				}
			}

			public virtual BaseTermVectorsFormatTestCase.RandomDocument NewDocument(int fieldCount
				, int maxTermCount, BaseTermVectorsFormatTestCase.Options options)
			{
				return new BaseTermVectorsFormatTestCase.RandomDocument(this, fieldCount, maxTermCount
					, options, this.fieldNames, this.terms, this.termBytes);
			}

			private readonly BaseTermVectorsFormatTestCase _enclosing;
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual void AssertEquals(BaseTermVectorsFormatTestCase.RandomDocument
			 doc, Fields fields)
		{
			// compare field names
			NUnit.Framework.Assert.AreEqual(doc == null, fields == null);
			NUnit.Framework.Assert.AreEqual(doc.fieldNames.Length, fields.Size());
			ICollection<string> fields1 = new HashSet<string>();
			ICollection<string> fields2 = new HashSet<string>();
			for (int i = 0; i < doc.fieldNames.Length; ++i)
			{
				fields1.AddItem(doc.fieldNames[i]);
			}
			foreach (string field in fields)
			{
				fields2.AddItem(field);
			}
			NUnit.Framework.Assert.AreEqual(fields1, fields2);
			for (int i_1 = 0; i_1 < doc.fieldNames.Length; ++i_1)
			{
				AssertEquals(doc.tokenStreams[i_1], doc.fieldTypes[i_1], fields.Terms(doc.fieldNames
					[i_1]));
			}
		}

		protected internal static bool Equals(object o1, object o2)
		{
			if (o1 == null)
			{
				return o2 == null;
			}
			else
			{
				return o1.Equals(o2);
			}
		}

		private readonly ThreadLocal<TermsEnum> termsEnum = new ThreadLocal<TermsEnum>();

		private readonly ThreadLocal<DocsEnum> docsEnum = new ThreadLocal<DocsEnum>();

		private readonly ThreadLocal<DocsAndPositionsEnum> docsAndPositionsEnum = new ThreadLocal
			<DocsAndPositionsEnum>();

		// to test reuse
		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual void AssertEquals(BaseTermVectorsFormatTestCase.RandomTokenStream
			 tk, FieldType ft, Terms terms)
		{
			NUnit.Framework.Assert.AreEqual(1, terms.GetDocCount());
			int termCount = new HashSet<string>(Arrays.AsList(tk.terms)).Count;
			NUnit.Framework.Assert.AreEqual(termCount, terms.Size());
			NUnit.Framework.Assert.AreEqual(termCount, terms.GetSumDocFreq());
			NUnit.Framework.Assert.AreEqual(ft.StoreTermVectorPositions(), terms.HasPositions
				());
			NUnit.Framework.Assert.AreEqual(ft.StoreTermVectorOffsets(), terms.HasOffsets());
			NUnit.Framework.Assert.AreEqual(ft.StoreTermVectorPayloads() && tk.HasPayloads(), 
				terms.HasPayloads());
			ICollection<BytesRef> uniqueTerms = new HashSet<BytesRef>();
			foreach (string term in tk.freqs.Keys)
			{
				uniqueTerms.AddItem(new BytesRef(term));
			}
			BytesRef[] sortedTerms = Sharpen.Collections.ToArray(uniqueTerms, new BytesRef[0]
				);
			Arrays.Sort(sortedTerms, terms.GetComparator());
			TermsEnum termsEnum = terms.Iterator(Random().NextBoolean() ? null : this.termsEnum
				.Get());
			this.termsEnum.Set(termsEnum);
			for (int i = 0; i < sortedTerms.Length; ++i)
			{
				BytesRef nextTerm = termsEnum.Next();
				NUnit.Framework.Assert.AreEqual(sortedTerms[i], nextTerm);
				NUnit.Framework.Assert.AreEqual(sortedTerms[i], termsEnum.Term());
				NUnit.Framework.Assert.AreEqual(1, termsEnum.DocFreq());
				FixedBitSet bits = new FixedBitSet(1);
				DocsEnum docsEnum = termsEnum.Docs(bits, Random().NextBoolean() ? null : this.docsEnum
					.Get());
				NUnit.Framework.Assert.AreEqual(DocsEnum.NO_MORE_DOCS, docsEnum.NextDoc());
				bits.Set(0);
				docsEnum = termsEnum.Docs(Random().NextBoolean() ? bits : null, Random().NextBoolean
					() ? null : docsEnum);
				NUnit.Framework.Assert.IsNotNull(docsEnum);
				NUnit.Framework.Assert.AreEqual(0, docsEnum.NextDoc());
				NUnit.Framework.Assert.AreEqual(0, docsEnum.DocID());
				NUnit.Framework.Assert.AreEqual(tk.freqs.Get(termsEnum.Term().Utf8ToString()), (int
					)docsEnum.Freq());
				NUnit.Framework.Assert.AreEqual(DocsEnum.NO_MORE_DOCS, docsEnum.NextDoc());
				this.docsEnum.Set(docsEnum);
				bits.Clear(0);
				DocsAndPositionsEnum docsAndPositionsEnum = termsEnum.DocsAndPositions(bits, Random
					().NextBoolean() ? null : this.docsAndPositionsEnum.Get());
				NUnit.Framework.Assert.AreEqual(ft.StoreTermVectorOffsets() || ft.StoreTermVectorPositions
					(), docsAndPositionsEnum != null);
				if (docsAndPositionsEnum != null)
				{
					NUnit.Framework.Assert.AreEqual(DocsEnum.NO_MORE_DOCS, docsAndPositionsEnum.NextDoc
						());
				}
				bits.Set(0);
				docsAndPositionsEnum = termsEnum.DocsAndPositions(Random().NextBoolean() ? bits : 
					null, Random().NextBoolean() ? null : docsAndPositionsEnum);
				NUnit.Framework.Assert.AreEqual(ft.StoreTermVectorOffsets() || ft.StoreTermVectorPositions
					(), docsAndPositionsEnum != null);
				if (terms.HasPositions() || terms.HasOffsets())
				{
					NUnit.Framework.Assert.AreEqual(0, docsAndPositionsEnum.NextDoc());
					int freq = docsAndPositionsEnum.Freq();
					NUnit.Framework.Assert.AreEqual(tk.freqs.Get(termsEnum.Term().Utf8ToString()), (int
						)freq);
					if (docsAndPositionsEnum != null)
					{
						for (int k = 0; k < freq; ++k)
						{
							int position = docsAndPositionsEnum.NextPosition();
							ICollection<int> indexes;
							if (terms.HasPositions())
							{
								indexes = tk.positionToTerms.Get(position);
								NUnit.Framework.Assert.IsNotNull(indexes);
							}
							else
							{
								indexes = tk.startOffsetToTerms.Get(docsAndPositionsEnum.StartOffset());
								NUnit.Framework.Assert.IsNotNull(indexes);
							}
							if (terms.HasPositions())
							{
								bool foundPosition = false;
								foreach (int index in indexes)
								{
									if (tk.termBytes[index].Equals(termsEnum.Term()) && tk.positions[index] == position)
									{
										foundPosition = true;
										break;
									}
								}
								NUnit.Framework.Assert.IsTrue(foundPosition);
							}
							if (terms.HasOffsets())
							{
								bool foundOffset = false;
								foreach (int index in indexes)
								{
									if (tk.termBytes[index].Equals(termsEnum.Term()) && tk.startOffsets[index] == docsAndPositionsEnum
										.StartOffset() && tk.endOffsets[index] == docsAndPositionsEnum.EndOffset())
									{
										foundOffset = true;
										break;
									}
								}
								NUnit.Framework.Assert.IsTrue(foundOffset);
							}
							if (terms.HasPayloads())
							{
								bool foundPayload = false;
								foreach (int index in indexes)
								{
									if (tk.termBytes[index].Equals(termsEnum.Term()) && Equals(tk.payloads[index], docsAndPositionsEnum
										.GetPayload()))
									{
										foundPayload = true;
										break;
									}
								}
								NUnit.Framework.Assert.IsTrue(foundPayload);
							}
						}
						try
						{
							docsAndPositionsEnum.NextPosition();
							NUnit.Framework.Assert.Fail();
						}
						catch (Exception)
						{
						}
						catch (Exception)
						{
						}
					}
					// ok
					// ok
					NUnit.Framework.Assert.AreEqual(DocsEnum.NO_MORE_DOCS, docsAndPositionsEnum.NextDoc
						());
				}
				this.docsAndPositionsEnum.Set(docsAndPositionsEnum);
			}
			NUnit.Framework.Assert.IsNull(termsEnum.Next());
			for (int i_1 = 0; i_1 < 5; ++i_1)
			{
				if (Random().NextBoolean())
				{
					NUnit.Framework.Assert.IsTrue(termsEnum.SeekExact(RandomPicks.RandomFrom(Random()
						, tk.termBytes)));
				}
				else
				{
					NUnit.Framework.Assert.AreEqual(TermsEnum.SeekStatus.FOUND, termsEnum.SeekCeil(RandomPicks
						.RandomFrom(Random(), tk.termBytes)));
				}
			}
		}

		protected internal virtual Lucene.Net.Documents.Document AddId(Lucene.Net.Documents.Document
			 doc, string id)
		{
			doc.Add(new StringField("id", id, Field.Store.NO));
			return doc;
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual int DocID(IndexReader reader, string id)
		{
			return new IndexSearcher(reader).Search(new TermQuery(new Term("id", id)), 1).scoreDocs
				[0].doc;
		}

		// only one doc with vectors
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestRareVectors()
		{
			BaseTermVectorsFormatTestCase.RandomDocumentFactory docFactory = new BaseTermVectorsFormatTestCase.RandomDocumentFactory
				(this, 10, 20);
			foreach (BaseTermVectorsFormatTestCase.Options options in ValidOptions())
			{
				int numDocs = AtLeast(200);
				int docWithVectors = Random().Next(numDocs);
				Lucene.Net.Documents.Document emptyDoc = new Lucene.Net.Documents.Document
					();
				Directory dir = NewDirectory();
				RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
				BaseTermVectorsFormatTestCase.RandomDocument doc = docFactory.NewDocument(TestUtil
					.NextInt(Random(), 1, 3), 20, options);
				for (int i = 0; i < numDocs; ++i)
				{
					if (i == docWithVectors)
					{
						writer.AddDocument(AddId(doc.ToDocument(), "42"));
					}
					else
					{
						writer.AddDocument(emptyDoc);
					}
				}
				IndexReader reader = writer.GetReader();
				int docWithVectorsID = DocID(reader, "42");
				for (int i_1 = 0; i_1 < 10; ++i_1)
				{
					int docID = Random().Next(numDocs);
					Fields fields = reader.GetTermVectors(docID);
					if (docID == docWithVectorsID)
					{
						AssertEquals(doc, fields);
					}
					else
					{
						NUnit.Framework.Assert.IsNull(fields);
					}
				}
				Fields fields_1 = reader.GetTermVectors(docWithVectorsID);
				AssertEquals(doc, fields_1);
				reader.Close();
				writer.Close();
				dir.Close();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestHighFreqs()
		{
			BaseTermVectorsFormatTestCase.RandomDocumentFactory docFactory = new BaseTermVectorsFormatTestCase.RandomDocumentFactory
				(this, 3, 5);
			foreach (BaseTermVectorsFormatTestCase.Options options in ValidOptions())
			{
				if (options == BaseTermVectorsFormatTestCase.Options.NONE)
				{
					continue;
				}
				Directory dir = NewDirectory();
				RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
				BaseTermVectorsFormatTestCase.RandomDocument doc = docFactory.NewDocument(TestUtil
					.NextInt(Random(), 1, 2), AtLeast(20000), options);
				writer.AddDocument(doc.ToDocument());
				IndexReader reader = writer.GetReader();
				AssertEquals(doc, reader.GetTermVectors(0));
				reader.Close();
				writer.Close();
				dir.Close();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestLotsOfFields()
		{
			BaseTermVectorsFormatTestCase.RandomDocumentFactory docFactory = new BaseTermVectorsFormatTestCase.RandomDocumentFactory
				(this, 5000, 10);
			foreach (BaseTermVectorsFormatTestCase.Options options in ValidOptions())
			{
				Directory dir = NewDirectory();
				RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
				BaseTermVectorsFormatTestCase.RandomDocument doc = docFactory.NewDocument(AtLeast
					(100), 5, options);
				writer.AddDocument(doc.ToDocument());
				IndexReader reader = writer.GetReader();
				AssertEquals(doc, reader.GetTermVectors(0));
				reader.Close();
				writer.Close();
				dir.Close();
			}
		}

		// different options for the same field
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestMixedOptions()
		{
			int numFields = TestUtil.NextInt(Random(), 1, 3);
			BaseTermVectorsFormatTestCase.RandomDocumentFactory docFactory = new BaseTermVectorsFormatTestCase.RandomDocumentFactory
				(this, numFields, 10);
			foreach (BaseTermVectorsFormatTestCase.Options options1 in ValidOptions())
			{
				foreach (BaseTermVectorsFormatTestCase.Options options2 in ValidOptions())
				{
					if (options1 == options2)
					{
						continue;
					}
					Directory dir = NewDirectory();
					RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
					BaseTermVectorsFormatTestCase.RandomDocument doc1 = docFactory.NewDocument(numFields
						, 20, options1);
					BaseTermVectorsFormatTestCase.RandomDocument doc2 = docFactory.NewDocument(numFields
						, 20, options2);
					writer.AddDocument(AddId(doc1.ToDocument(), "1"));
					writer.AddDocument(AddId(doc2.ToDocument(), "2"));
					IndexReader reader = writer.GetReader();
					int doc1ID = DocID(reader, "1");
					AssertEquals(doc1, reader.GetTermVectors(doc1ID));
					int doc2ID = DocID(reader, "2");
					AssertEquals(doc2, reader.GetTermVectors(doc2ID));
					reader.Close();
					writer.Close();
					dir.Close();
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestRandom()
		{
			BaseTermVectorsFormatTestCase.RandomDocumentFactory docFactory = new BaseTermVectorsFormatTestCase.RandomDocumentFactory
				(this, 5, 20);
			int numDocs = AtLeast(100);
			BaseTermVectorsFormatTestCase.RandomDocument[] docs = new BaseTermVectorsFormatTestCase.RandomDocument
				[numDocs];
			for (int i = 0; i < numDocs; ++i)
			{
				docs[i] = docFactory.NewDocument(TestUtil.NextInt(Random(), 1, 3), TestUtil.NextInt
					(Random(), 10, 50), RandomOptions());
			}
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			for (int i_1 = 0; i_1 < numDocs; ++i_1)
			{
				writer.AddDocument(AddId(docs[i_1].ToDocument(), string.Empty + i_1));
			}
			IndexReader reader = writer.GetReader();
			for (int i_2 = 0; i_2 < numDocs; ++i_2)
			{
				int docID = DocID(reader, string.Empty + i_2);
				AssertEquals(docs[i_2], reader.GetTermVectors(docID));
			}
			reader.Close();
			writer.Close();
			dir.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestMerge()
		{
			BaseTermVectorsFormatTestCase.RandomDocumentFactory docFactory = new BaseTermVectorsFormatTestCase.RandomDocumentFactory
				(this, 5, 20);
			int numDocs = AtLeast(100);
			int numDeletes = Random().Next(numDocs);
			ICollection<int> deletes = new HashSet<int>();
			while (deletes.Count < numDeletes)
			{
				deletes.AddItem(Random().Next(numDocs));
			}
			foreach (BaseTermVectorsFormatTestCase.Options options in ValidOptions())
			{
				BaseTermVectorsFormatTestCase.RandomDocument[] docs = new BaseTermVectorsFormatTestCase.RandomDocument
					[numDocs];
				for (int i = 0; i < numDocs; ++i)
				{
					docs[i] = docFactory.NewDocument(TestUtil.NextInt(Random(), 1, 3), AtLeast(10), options
						);
				}
				Directory dir = NewDirectory();
				RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
				for (int i_1 = 0; i_1 < numDocs; ++i_1)
				{
					writer.AddDocument(AddId(docs[i_1].ToDocument(), string.Empty + i_1));
					if (Rarely())
					{
						writer.Commit();
					}
				}
				foreach (int delete in deletes)
				{
					writer.DeleteDocuments(new Term("id", string.Empty + delete));
				}
				// merge with deletes
				writer.ForceMerge(1);
				IndexReader reader = writer.GetReader();
				for (int i_2 = 0; i_2 < numDocs; ++i_2)
				{
					if (!deletes.Contains(i_2))
					{
						int docID = DocID(reader, string.Empty + i_2);
						AssertEquals(docs[i_2], reader.GetTermVectors(docID));
					}
				}
				reader.Close();
				writer.Close();
				dir.Close();
			}
		}

		// run random tests from different threads to make sure the per-thread clones
		// don't share mutable data
		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestClone()
		{
			BaseTermVectorsFormatTestCase.RandomDocumentFactory docFactory = new BaseTermVectorsFormatTestCase.RandomDocumentFactory
				(this, 5, 20);
			int numDocs = AtLeast(100);
			foreach (BaseTermVectorsFormatTestCase.Options options in ValidOptions())
			{
				BaseTermVectorsFormatTestCase.RandomDocument[] docs = new BaseTermVectorsFormatTestCase.RandomDocument
					[numDocs];
				for (int i = 0; i < numDocs; ++i)
				{
					docs[i] = docFactory.NewDocument(TestUtil.NextInt(Random(), 1, 3), AtLeast(10), options
						);
				}
				Directory dir = NewDirectory();
				RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
				for (int i_1 = 0; i_1 < numDocs; ++i_1)
				{
					writer.AddDocument(AddId(docs[i_1].ToDocument(), string.Empty + i_1));
				}
				IndexReader reader = writer.GetReader();
				for (int i_2 = 0; i_2 < numDocs; ++i_2)
				{
					int docID = DocID(reader, string.Empty + i_2);
					AssertEquals(docs[i_2], reader.GetTermVectors(docID));
				}
				AtomicReference<Exception> exception = new AtomicReference<Exception>();
				Sharpen.Thread[] threads = new Sharpen.Thread[2];
				for (int i_3 = 0; i_3 < threads.Length; ++i_3)
				{
					threads[i_3] = new _Thread_692(this, numDocs, reader, docs, exception);
				}
				foreach (Sharpen.Thread thread in threads)
				{
					thread.Start();
				}
				foreach (Sharpen.Thread thread_1 in threads)
				{
					thread_1.Join();
				}
				reader.Close();
				writer.Close();
				dir.Close();
				NUnit.Framework.Assert.IsNull("One thread threw an exception", exception.Get());
			}
		}

		private sealed class _Thread_692 : Sharpen.Thread
		{
			public _Thread_692(BaseTermVectorsFormatTestCase _enclosing, int numDocs, IndexReader
				 reader, BaseTermVectorsFormatTestCase.RandomDocument[] docs, AtomicReference<Exception
				> exception)
			{
				this._enclosing = _enclosing;
				this.numDocs = numDocs;
				this.reader = reader;
				this.docs = docs;
				this.exception = exception;
			}

			public override void Run()
			{
				try
				{
					for (int i = 0; i < LuceneTestCase.AtLeast(100); ++i)
					{
						int idx = LuceneTestCase.Random().Next(numDocs);
						int docID = this._enclosing.DocID(reader, string.Empty + idx);
						this._enclosing.AssertEquals(docs[idx], reader.GetTermVectors(docID));
					}
				}
				catch (Exception t)
				{
					exception.Set(t);
				}
			}

			private readonly BaseTermVectorsFormatTestCase _enclosing;

			private readonly int numDocs;

			private readonly IndexReader reader;

			private readonly BaseTermVectorsFormatTestCase.RandomDocument[] docs;

			private readonly AtomicReference<Exception> exception;
		}
	}
}
