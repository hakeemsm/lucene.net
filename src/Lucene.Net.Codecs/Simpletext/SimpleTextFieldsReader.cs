/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Simpletext;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;
using Sharpen;

namespace Lucene.Net.Codecs.Simpletext
{
	internal class SimpleTextFieldsReader : FieldsProducer
	{
		private readonly SortedDictionary<string, long> fields;

		private readonly IndexInput @in;

		private readonly FieldInfos fieldInfos;

		private readonly int maxDoc;

		/// <exception cref="System.IO.IOException"></exception>
		public SimpleTextFieldsReader(SegmentReadState state)
		{
			this.maxDoc = state.segmentInfo.GetDocCount();
			fieldInfos = state.fieldInfos;
			@in = state.directory.OpenInput(SimpleTextPostingsFormat.GetPostingsFileName(state
				.segmentInfo.name, state.segmentSuffix), state.context);
			bool success = false;
			try
			{
				fields = ReadFields(((IndexInput)@in.Clone()));
				success = true;
			}
			finally
			{
				if (!success)
				{
					IOUtils.CloseWhileHandlingException(this);
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private SortedDictionary<string, long> ReadFields(IndexInput @in)
		{
			ChecksumIndexInput input = new BufferedChecksumIndexInput(@in);
			BytesRef scratch = new BytesRef(10);
			SortedDictionary<string, long> fields = new SortedDictionary<string, long>();
			while (true)
			{
				SimpleTextUtil.ReadLine(input, scratch);
				if (scratch.Equals(SimpleTextFieldsWriter.END))
				{
					SimpleTextUtil.CheckFooter(input);
					return fields;
				}
				else
				{
					if (StringHelper.StartsWith(scratch, SimpleTextFieldsWriter.FIELD))
					{
						string fieldName = new string(scratch.bytes, scratch.offset + SimpleTextFieldsWriter
							.FIELD.length, scratch.length - SimpleTextFieldsWriter.FIELD.length, StandardCharsets
							.UTF_8);
						fields.Put(fieldName, input.GetFilePointer());
					}
				}
			}
		}

		private class SimpleTextTermsEnum : TermsEnum
		{
			private readonly FieldInfo.IndexOptions indexOptions;

			private int docFreq;

			private long totalTermFreq;

			private long docsStart;

			private bool ended;

			private readonly BytesRefFSTEnum<PairOutputs.Pair<long, PairOutputs.Pair<long, long
				>>> fstEnum;

			public SimpleTextTermsEnum(SimpleTextFieldsReader _enclosing, FST<PairOutputs.Pair
				<long, PairOutputs.Pair<long, long>>> fst, FieldInfo.IndexOptions indexOptions)
			{
				this._enclosing = _enclosing;
				this.indexOptions = indexOptions;
				this.fstEnum = new BytesRefFSTEnum<PairOutputs.Pair<long, PairOutputs.Pair<long, 
					long>>>(fst);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override bool SeekExact(BytesRef text)
			{
				BytesRefFSTEnum.InputOutput<PairOutputs.Pair<long, PairOutputs.Pair<long, long>>>
					 result = this.fstEnum.SeekExact(text);
				if (result != null)
				{
					PairOutputs.Pair<long, PairOutputs.Pair<long, long>> pair1 = result.output;
					PairOutputs.Pair<long, long> pair2 = pair1.output2;
					this.docsStart = pair1.output1;
					this.docFreq = pair2.output1;
					this.totalTermFreq = pair2.output2;
					return true;
				}
				else
				{
					return false;
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override TermsEnum.SeekStatus SeekCeil(BytesRef text)
			{
				//System.out.println("seek to text=" + text.utf8ToString());
				BytesRefFSTEnum.InputOutput<PairOutputs.Pair<long, PairOutputs.Pair<long, long>>>
					 result = this.fstEnum.SeekCeil(text);
				if (result == null)
				{
					//System.out.println("  end");
					return TermsEnum.SeekStatus.END;
				}
				else
				{
					//System.out.println("  got text=" + term.utf8ToString());
					PairOutputs.Pair<long, PairOutputs.Pair<long, long>> pair1 = result.output;
					PairOutputs.Pair<long, long> pair2 = pair1.output2;
					this.docsStart = pair1.output1;
					this.docFreq = pair2.output1;
					this.totalTermFreq = pair2.output2;
					if (result.input.Equals(text))
					{
						//System.out.println("  match docsStart=" + docsStart);
						return TermsEnum.SeekStatus.FOUND;
					}
					else
					{
						//System.out.println("  not match docsStart=" + docsStart);
						return TermsEnum.SeekStatus.NOT_FOUND;
					}
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override BytesRef Next()
			{
				//HM:revisit 
				//assert !ended;
				BytesRefFSTEnum.InputOutput<PairOutputs.Pair<long, PairOutputs.Pair<long, long>>>
					 result = this.fstEnum.Next();
				if (result != null)
				{
					PairOutputs.Pair<long, PairOutputs.Pair<long, long>> pair1 = result.output;
					PairOutputs.Pair<long, long> pair2 = pair1.output2;
					this.docsStart = pair1.output1;
					this.docFreq = pair2.output1;
					this.totalTermFreq = pair2.output2;
					return result.input;
				}
				else
				{
					return null;
				}
			}

			public override BytesRef Term()
			{
				return this.fstEnum.Current().input;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override long Ord()
			{
				throw new NotSupportedException();
			}

			public override void SeekExact(long ord)
			{
				throw new NotSupportedException();
			}

			public override int DocFreq()
			{
				return this.docFreq;
			}

			public override long TotalTermFreq()
			{
				return this.indexOptions == FieldInfo.IndexOptions.DOCS_ONLY ? -1 : this.totalTermFreq;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override DocsEnum Docs(Bits liveDocs, DocsEnum reuse, int flags)
			{
				SimpleTextFieldsReader.SimpleTextDocsEnum docsEnum;
				if (reuse != null && reuse is SimpleTextFieldsReader.SimpleTextDocsEnum && ((SimpleTextFieldsReader.SimpleTextDocsEnum
					)reuse).CanReuse(this._enclosing.@in))
				{
					docsEnum = (SimpleTextFieldsReader.SimpleTextDocsEnum)reuse;
				}
				else
				{
					docsEnum = new SimpleTextFieldsReader.SimpleTextDocsEnum(this);
				}
				return docsEnum.Reset(this.docsStart, liveDocs, this.indexOptions == FieldInfo.IndexOptions
					.DOCS_ONLY, this.docFreq);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override DocsAndPositionsEnum DocsAndPositions(Bits liveDocs, DocsAndPositionsEnum
				 reuse, int flags)
			{
				if (this.indexOptions.CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS
					) < 0)
				{
					// Positions were not indexed
					return null;
				}
				SimpleTextFieldsReader.SimpleTextDocsAndPositionsEnum docsAndPositionsEnum;
				if (reuse != null && reuse is SimpleTextFieldsReader.SimpleTextDocsAndPositionsEnum
					 && ((SimpleTextFieldsReader.SimpleTextDocsAndPositionsEnum)reuse).CanReuse(this
					._enclosing.@in))
				{
					docsAndPositionsEnum = (SimpleTextFieldsReader.SimpleTextDocsAndPositionsEnum)reuse;
				}
				else
				{
					docsAndPositionsEnum = new SimpleTextFieldsReader.SimpleTextDocsAndPositionsEnum(
						this);
				}
				return docsAndPositionsEnum.Reset(this.docsStart, liveDocs, this.indexOptions, this
					.docFreq);
			}

			public override IComparer<BytesRef> GetComparator()
			{
				return BytesRef.GetUTF8SortedAsUnicodeComparator();
			}

			private readonly SimpleTextFieldsReader _enclosing;
		}

		private class SimpleTextDocsEnum : DocsEnum
		{
			private readonly IndexInput inStart;

			private readonly IndexInput @in;

			private bool omitTF;

			private int docID = -1;

			private int tf;

			private Bits liveDocs;

			private readonly BytesRef scratch = new BytesRef(10);

			private readonly CharsRef scratchUTF16 = new CharsRef(10);

			private int cost;

			public SimpleTextDocsEnum(SimpleTextFieldsReader _enclosing)
			{
				this._enclosing = _enclosing;
				this.inStart = this._enclosing.@in;
				this.@in = ((IndexInput)this.inStart.Clone());
			}

			public virtual bool CanReuse(IndexInput @in)
			{
				return @in == this.inStart;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public virtual SimpleTextFieldsReader.SimpleTextDocsEnum Reset(long fp, Bits liveDocs
				, bool omitTF, int docFreq)
			{
				this.liveDocs = liveDocs;
				this.@in.Seek(fp);
				this.omitTF = omitTF;
				this.docID = -1;
				this.tf = 1;
				this.cost = docFreq;
				return this;
			}

			public override int DocID()
			{
				return this.docID;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Freq()
			{
				return this.tf;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int NextDoc()
			{
				if (this.docID == DocIdSetIterator.NO_MORE_DOCS)
				{
					return this.docID;
				}
				bool first = true;
				int termFreq = 0;
				while (true)
				{
					long lineStart = this.@in.GetFilePointer();
					SimpleTextUtil.ReadLine(this.@in, this.scratch);
					if (StringHelper.StartsWith(this.scratch, SimpleTextFieldsWriter.DOC))
					{
						if (!first && (this.liveDocs == null || this.liveDocs.Get(this.docID)))
						{
							this.@in.Seek(lineStart);
							if (!this.omitTF)
							{
								this.tf = termFreq;
							}
							return this.docID;
						}
						UnicodeUtil.UTF8toUTF16(this.scratch.bytes, this.scratch.offset + SimpleTextFieldsWriter
							.DOC.length, this.scratch.length - SimpleTextFieldsWriter.DOC.length, this.scratchUTF16
							);
						this.docID = ArrayUtil.ParseInt(this.scratchUTF16.chars, 0, this.scratchUTF16.length
							);
						termFreq = 0;
						first = false;
					}
					else
					{
						if (StringHelper.StartsWith(this.scratch, SimpleTextFieldsWriter.FREQ))
						{
							UnicodeUtil.UTF8toUTF16(this.scratch.bytes, this.scratch.offset + SimpleTextFieldsWriter
								.FREQ.length, this.scratch.length - SimpleTextFieldsWriter.FREQ.length, this.scratchUTF16
								);
							termFreq = ArrayUtil.ParseInt(this.scratchUTF16.chars, 0, this.scratchUTF16.length
								);
						}
						else
						{
							if (StringHelper.StartsWith(this.scratch, SimpleTextFieldsWriter.POS))
							{
							}
							else
							{
								// skip termFreq++;
								if (StringHelper.StartsWith(this.scratch, SimpleTextFieldsWriter.START_OFFSET))
								{
								}
								else
								{
									// skip
									if (StringHelper.StartsWith(this.scratch, SimpleTextFieldsWriter.END_OFFSET))
									{
									}
									else
									{
										// skip
										if (StringHelper.StartsWith(this.scratch, SimpleTextFieldsWriter.PAYLOAD))
										{
										}
										else
										{
											// skip
											//HM:revisit 
											//assert StringHelper.startsWith(scratch, TERM) || StringHelper.startsWith(scratch, FIELD) || StringHelper.startsWith(scratch, END): "scratch=" + scratch.utf8ToString();
											if (!first && (this.liveDocs == null || this.liveDocs.Get(this.docID)))
											{
												this.@in.Seek(lineStart);
												if (!this.omitTF)
												{
													this.tf = termFreq;
												}
												return this.docID;
											}
											return this.docID = DocIdSetIterator.NO_MORE_DOCS;
										}
									}
								}
							}
						}
					}
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Advance(int target)
			{
				// Naive -- better to index skip data
				return this.SlowAdvance(target);
			}

			public override long Cost()
			{
				return this.cost;
			}

			private readonly SimpleTextFieldsReader _enclosing;
		}

		private class SimpleTextDocsAndPositionsEnum : DocsAndPositionsEnum
		{
			private readonly IndexInput inStart;

			private readonly IndexInput @in;

			private int docID = -1;

			private int tf;

			private Bits liveDocs;

			private readonly BytesRef scratch = new BytesRef(10);

			private readonly BytesRef scratch2 = new BytesRef(10);

			private readonly CharsRef scratchUTF16 = new CharsRef(10);

			private readonly CharsRef scratchUTF16_2 = new CharsRef(10);

			private BytesRef payload;

			private long nextDocStart;

			private bool readOffsets;

			private bool readPositions;

			private int startOffset;

			private int endOffset;

			private int cost;

			public SimpleTextDocsAndPositionsEnum(SimpleTextFieldsReader _enclosing)
			{
				this._enclosing = _enclosing;
				this.inStart = this._enclosing.@in;
				this.@in = ((IndexInput)this.inStart.Clone());
			}

			public virtual bool CanReuse(IndexInput @in)
			{
				return @in == this.inStart;
			}

			public virtual SimpleTextFieldsReader.SimpleTextDocsAndPositionsEnum Reset(long fp
				, Bits liveDocs, FieldInfo.IndexOptions indexOptions, int docFreq)
			{
				this.liveDocs = liveDocs;
				this.nextDocStart = fp;
				this.docID = -1;
				this.readPositions = indexOptions.CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS
					) >= 0;
				this.readOffsets = indexOptions.CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
					) >= 0;
				if (!this.readOffsets)
				{
					this.startOffset = -1;
					this.endOffset = -1;
				}
				this.cost = docFreq;
				return this;
			}

			public override int DocID()
			{
				return this.docID;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Freq()
			{
				return this.tf;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int NextDoc()
			{
				bool first = true;
				this.@in.Seek(this.nextDocStart);
				long posStart = 0;
				while (true)
				{
					long lineStart = this.@in.GetFilePointer();
					SimpleTextUtil.ReadLine(this.@in, this.scratch);
					//System.out.println("NEXT DOC: " + scratch.utf8ToString());
					if (StringHelper.StartsWith(this.scratch, SimpleTextFieldsWriter.DOC))
					{
						if (!first && (this.liveDocs == null || this.liveDocs.Get(this.docID)))
						{
							this.nextDocStart = lineStart;
							this.@in.Seek(posStart);
							return this.docID;
						}
						UnicodeUtil.UTF8toUTF16(this.scratch.bytes, this.scratch.offset + SimpleTextFieldsWriter
							.DOC.length, this.scratch.length - SimpleTextFieldsWriter.DOC.length, this.scratchUTF16
							);
						this.docID = ArrayUtil.ParseInt(this.scratchUTF16.chars, 0, this.scratchUTF16.length
							);
						this.tf = 0;
						first = false;
					}
					else
					{
						if (StringHelper.StartsWith(this.scratch, SimpleTextFieldsWriter.FREQ))
						{
							UnicodeUtil.UTF8toUTF16(this.scratch.bytes, this.scratch.offset + SimpleTextFieldsWriter
								.FREQ.length, this.scratch.length - SimpleTextFieldsWriter.FREQ.length, this.scratchUTF16
								);
							this.tf = ArrayUtil.ParseInt(this.scratchUTF16.chars, 0, this.scratchUTF16.length
								);
							posStart = this.@in.GetFilePointer();
						}
						else
						{
							if (StringHelper.StartsWith(this.scratch, SimpleTextFieldsWriter.POS))
							{
							}
							else
							{
								// skip
								if (StringHelper.StartsWith(this.scratch, SimpleTextFieldsWriter.START_OFFSET))
								{
								}
								else
								{
									// skip
									if (StringHelper.StartsWith(this.scratch, SimpleTextFieldsWriter.END_OFFSET))
									{
									}
									else
									{
										// skip
										if (StringHelper.StartsWith(this.scratch, SimpleTextFieldsWriter.PAYLOAD))
										{
										}
										else
										{
											// skip
											//HM:revisit 
											//assert StringHelper.startsWith(scratch, TERM) || StringHelper.startsWith(scratch, FIELD) || StringHelper.startsWith(scratch, END);
											if (!first && (this.liveDocs == null || this.liveDocs.Get(this.docID)))
											{
												this.nextDocStart = lineStart;
												this.@in.Seek(posStart);
												return this.docID;
											}
											return this.docID = DocIdSetIterator.NO_MORE_DOCS;
										}
									}
								}
							}
						}
					}
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Advance(int target)
			{
				// Naive -- better to index skip data
				return this.SlowAdvance(target);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int NextPosition()
			{
				int pos;
				if (this.readPositions)
				{
					SimpleTextUtil.ReadLine(this.@in, this.scratch);
					//HM:revisit 
					//assert StringHelper.startsWith(scratch, POS): "got line=" + scratch.utf8ToString();
					UnicodeUtil.UTF8toUTF16(this.scratch.bytes, this.scratch.offset + SimpleTextFieldsWriter
						.POS.length, this.scratch.length - SimpleTextFieldsWriter.POS.length, this.scratchUTF16_2
						);
					pos = ArrayUtil.ParseInt(this.scratchUTF16_2.chars, 0, this.scratchUTF16_2.length
						);
				}
				else
				{
					pos = -1;
				}
				if (this.readOffsets)
				{
					SimpleTextUtil.ReadLine(this.@in, this.scratch);
					//HM:revisit 
					//assert StringHelper.startsWith(scratch, START_OFFSET): "got line=" + scratch.utf8ToString();
					UnicodeUtil.UTF8toUTF16(this.scratch.bytes, this.scratch.offset + SimpleTextFieldsWriter
						.START_OFFSET.length, this.scratch.length - SimpleTextFieldsWriter.START_OFFSET.
						length, this.scratchUTF16_2);
					this.startOffset = ArrayUtil.ParseInt(this.scratchUTF16_2.chars, 0, this.scratchUTF16_2
						.length);
					SimpleTextUtil.ReadLine(this.@in, this.scratch);
					//HM:revisit 
					//assert StringHelper.startsWith(scratch, END_OFFSET): "got line=" + scratch.utf8ToString();
					UnicodeUtil.UTF8toUTF16(this.scratch.bytes, this.scratch.offset + SimpleTextFieldsWriter
						.END_OFFSET.length, this.scratch.length - SimpleTextFieldsWriter.END_OFFSET.length
						, this.scratchUTF16_2);
					this.endOffset = ArrayUtil.ParseInt(this.scratchUTF16_2.chars, 0, this.scratchUTF16_2
						.length);
				}
				long fp = this.@in.GetFilePointer();
				SimpleTextUtil.ReadLine(this.@in, this.scratch);
				if (StringHelper.StartsWith(this.scratch, SimpleTextFieldsWriter.PAYLOAD))
				{
					int len = this.scratch.length - SimpleTextFieldsWriter.PAYLOAD.length;
					if (this.scratch2.bytes.Length < len)
					{
						this.scratch2.Grow(len);
					}
					System.Array.Copy(this.scratch.bytes, SimpleTextFieldsWriter.PAYLOAD.length, this
						.scratch2.bytes, 0, len);
					this.scratch2.length = len;
					this.payload = this.scratch2;
				}
				else
				{
					this.payload = null;
					this.@in.Seek(fp);
				}
				return pos;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int StartOffset()
			{
				return this.startOffset;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int EndOffset()
			{
				return this.endOffset;
			}

			public override BytesRef GetPayload()
			{
				return this.payload;
			}

			public override long Cost()
			{
				return this.cost;
			}

			private readonly SimpleTextFieldsReader _enclosing;
		}

		internal class TermData
		{
			public long docsStart;

			public int docFreq;

			public TermData(long docsStart, int docFreq)
			{
				this.docsStart = docsStart;
				this.docFreq = docFreq;
			}
		}

		private class SimpleTextTerms : Terms
		{
			private readonly long termsStart;

			private readonly FieldInfo fieldInfo;

			private readonly int maxDoc;

			private long sumTotalTermFreq;

			private long sumDocFreq;

			private int docCount;

			private FST<PairOutputs.Pair<long, PairOutputs.Pair<long, long>>> fst;

			private int termCount;

			private readonly BytesRef scratch = new BytesRef(10);

			private readonly CharsRef scratchUTF16 = new CharsRef(10);

			/// <exception cref="System.IO.IOException"></exception>
			public SimpleTextTerms(SimpleTextFieldsReader _enclosing, string field, long termsStart
				, int maxDoc)
			{
				this._enclosing = _enclosing;
				this.maxDoc = maxDoc;
				this.termsStart = termsStart;
				this.fieldInfo = this._enclosing.fieldInfos.FieldInfo(field);
				this.LoadTerms();
			}

			/// <exception cref="System.IO.IOException"></exception>
			private void LoadTerms()
			{
				PositiveIntOutputs posIntOutputs = PositiveIntOutputs.GetSingleton();
				Builder<PairOutputs.Pair<long, PairOutputs.Pair<long, long>>> b;
				PairOutputs<long, long> outputsInner = new PairOutputs<long, long>(posIntOutputs, 
					posIntOutputs);
				PairOutputs<long, PairOutputs.Pair<long, long>> outputs = new PairOutputs<long, PairOutputs.Pair
					<long, long>>(posIntOutputs, outputsInner);
				b = new Builder<PairOutputs.Pair<long, PairOutputs.Pair<long, long>>>(FST.INPUT_TYPE
					.BYTE1, outputs);
				IndexInput @in = ((IndexInput)this._enclosing.@in.Clone());
				@in.Seek(this.termsStart);
				BytesRef lastTerm = new BytesRef(10);
				long lastDocsStart = -1;
				int docFreq = 0;
				long totalTermFreq = 0;
				FixedBitSet visitedDocs = new FixedBitSet(this.maxDoc);
				IntsRef scratchIntsRef = new IntsRef();
				while (true)
				{
					SimpleTextUtil.ReadLine(@in, this.scratch);
					if (this.scratch.Equals(SimpleTextFieldsWriter.END) || StringHelper.StartsWith(this
						.scratch, SimpleTextFieldsWriter.FIELD))
					{
						if (lastDocsStart != -1)
						{
							b.Add(Lucene.Net.Util.Fst.Util.ToIntsRef(lastTerm, scratchIntsRef), outputs
								.NewPair(lastDocsStart, outputsInner.NewPair((long)docFreq, totalTermFreq)));
							this.sumTotalTermFreq += totalTermFreq;
						}
						break;
					}
					else
					{
						if (StringHelper.StartsWith(this.scratch, SimpleTextFieldsWriter.DOC))
						{
							docFreq++;
							this.sumDocFreq++;
							UnicodeUtil.UTF8toUTF16(this.scratch.bytes, this.scratch.offset + SimpleTextFieldsWriter
								.DOC.length, this.scratch.length - SimpleTextFieldsWriter.DOC.length, this.scratchUTF16
								);
							int docID = ArrayUtil.ParseInt(this.scratchUTF16.chars, 0, this.scratchUTF16.length
								);
							visitedDocs.Set(docID);
						}
						else
						{
							if (StringHelper.StartsWith(this.scratch, SimpleTextFieldsWriter.FREQ))
							{
								UnicodeUtil.UTF8toUTF16(this.scratch.bytes, this.scratch.offset + SimpleTextFieldsWriter
									.FREQ.length, this.scratch.length - SimpleTextFieldsWriter.FREQ.length, this.scratchUTF16
									);
								totalTermFreq += ArrayUtil.ParseInt(this.scratchUTF16.chars, 0, this.scratchUTF16
									.length);
							}
							else
							{
								if (StringHelper.StartsWith(this.scratch, SimpleTextFieldsWriter.TERM))
								{
									if (lastDocsStart != -1)
									{
										b.Add(Lucene.Net.Util.Fst.Util.ToIntsRef(lastTerm, scratchIntsRef), outputs
											.NewPair(lastDocsStart, outputsInner.NewPair((long)docFreq, totalTermFreq)));
									}
									lastDocsStart = @in.GetFilePointer();
									int len = this.scratch.length - SimpleTextFieldsWriter.TERM.length;
									if (len > lastTerm.length)
									{
										lastTerm.Grow(len);
									}
									System.Array.Copy(this.scratch.bytes, SimpleTextFieldsWriter.TERM.length, lastTerm
										.bytes, 0, len);
									lastTerm.length = len;
									docFreq = 0;
									this.sumTotalTermFreq += totalTermFreq;
									totalTermFreq = 0;
									this.termCount++;
								}
							}
						}
					}
				}
				this.docCount = visitedDocs.Cardinality();
				this.fst = b.Finish();
			}

			//System.out.println("FST " + fst.sizeInBytes());
			/// <summary>Returns approximate RAM bytes used</summary>
			public virtual long RamBytesUsed()
			{
				return (this.fst != null) ? this.fst.SizeInBytes() : 0;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override TermsEnum Iterator(TermsEnum reuse)
			{
				if (this.fst != null)
				{
					return new SimpleTextFieldsReader.SimpleTextTermsEnum(this, this.fst, this.fieldInfo
						.GetIndexOptions());
				}
				else
				{
					return TermsEnum.EMPTY;
				}
			}

			public override IComparer<BytesRef> GetComparator()
			{
				return BytesRef.GetUTF8SortedAsUnicodeComparator();
			}

			public override long Size()
			{
				return (long)this.termCount;
			}

			public override long GetSumTotalTermFreq()
			{
				return this.fieldInfo.GetIndexOptions() == FieldInfo.IndexOptions.DOCS_ONLY ? -1 : 
					this.sumTotalTermFreq;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override long GetSumDocFreq()
			{
				return this.sumDocFreq;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int GetDocCount()
			{
				return this.docCount;
			}

			public override bool HasFreqs()
			{
				return this.fieldInfo.GetIndexOptions().CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS
					) >= 0;
			}

			public override bool HasOffsets()
			{
				return this.fieldInfo.GetIndexOptions().CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
					) >= 0;
			}

			public override bool HasPositions()
			{
				return this.fieldInfo.GetIndexOptions().CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS
					) >= 0;
			}

			public override bool HasPayloads()
			{
				return this.fieldInfo.HasPayloads();
			}

			private readonly SimpleTextFieldsReader _enclosing;
		}

		public override Sharpen.Iterator<string> Iterator()
		{
			return Sharpen.Collections.UnmodifiableSet(fields.Keys).Iterator();
		}

		private readonly IDictionary<string, SimpleTextFieldsReader.SimpleTextTerms> termsCache
			 = new Dictionary<string, SimpleTextFieldsReader.SimpleTextTerms>();

		/// <exception cref="System.IO.IOException"></exception>
		public override Terms Terms(string field)
		{
			lock (this)
			{
				Terms terms = termsCache.Get(field);
				if (terms == null)
				{
					long fp = fields.Get(field);
					if (fp == null)
					{
						return null;
					}
					else
					{
						terms = new SimpleTextFieldsReader.SimpleTextTerms(this, field, fp, maxDoc);
						termsCache.Put(field, (SimpleTextFieldsReader.SimpleTextTerms)terms);
					}
				}
				return terms;
			}
		}

		public override int Size()
		{
			return -1;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			@in.Close();
		}

		public override long RamBytesUsed()
		{
			long sizeInBytes = 0;
			foreach (SimpleTextFieldsReader.SimpleTextTerms simpleTextTerms in termsCache.Values)
			{
				sizeInBytes += (simpleTextTerms != null) ? simpleTextTerms.RamBytesUsed() : 0;
			}
			return sizeInBytes;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void CheckIntegrity()
		{
		}
	}
}
