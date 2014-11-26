/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Memory;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;
using Lucene.Net.Util.Packed;
using Sharpen;

namespace Lucene.Net.Codecs.Memory
{
	/// <summary>
	/// Stores terms & postings (docs, positions, payloads) in
	/// RAM, using an FST.
	/// </summary>
	/// <remarks>
	/// Stores terms & postings (docs, positions, payloads) in
	/// RAM, using an FST.
	/// <p>Note that this codec implements advance as a linear
	/// scan!  This means if you store large fields in here,
	/// queries that rely on advance will (AND BooleanQuery,
	/// PhraseQuery) will be relatively slow!
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public sealed class MemoryPostingsFormat : PostingsFormat
	{
		private readonly bool doPackFST;

		private readonly float acceptableOverheadRatio;

		public MemoryPostingsFormat() : this(false, PackedInts.DEFAULT)
		{
		}

		/// <summary>Create MemoryPostingsFormat, specifying advanced FST options.</summary>
		/// <remarks>Create MemoryPostingsFormat, specifying advanced FST options.</remarks>
		/// <param name="doPackFST">
		/// true if a packed FST should be built.
		/// NOTE: packed FSTs are limited to ~2.1 GB of postings.
		/// </param>
		/// <param name="acceptableOverheadRatio">
		/// allowable overhead for packed ints
		/// during FST construction.
		/// </param>
		public MemoryPostingsFormat(bool doPackFST, float acceptableOverheadRatio) : base
			("Memory")
		{
			// TODO: would be nice to somehow allow this to act like
			// InstantiatedIndex, by never writing to disk; ie you write
			// to this Codec in RAM only and then when you open a reader
			// it pulls the FST directly from what you wrote w/o going
			// to disk.
			// TODO: Maybe name this 'Cached' or something to reflect
			// the reality that it is actually written to disk, but
			// loads itself in ram?
			this.doPackFST = doPackFST;
			this.acceptableOverheadRatio = acceptableOverheadRatio;
		}

		public override string ToString()
		{
			return "PostingsFormat(name=" + GetName() + " doPackFST= " + doPackFST + ")";
		}

		private sealed class TermsWriter : TermsConsumer
		{
			private readonly IndexOutput @out;

			private readonly FieldInfo field;

			private readonly Builder<BytesRef> builder;

			private readonly ByteSequenceOutputs outputs = ByteSequenceOutputs.GetSingleton();

			private readonly bool doPackFST;

			private readonly float acceptableOverheadRatio;

			private int termCount;

			public TermsWriter(IndexOutput @out, FieldInfo field, bool doPackFST, float acceptableOverheadRatio
				)
			{
				postingsWriter = new MemoryPostingsFormat.TermsWriter.PostingsWriter(this);
				this.@out = @out;
				this.field = field;
				this.doPackFST = doPackFST;
				this.acceptableOverheadRatio = acceptableOverheadRatio;
				builder = new Builder<BytesRef>(FST.INPUT_TYPE.BYTE1, 0, 0, true, true, int.MaxValue
					, outputs, null, doPackFST, acceptableOverheadRatio, true, 15);
			}

			private class PostingsWriter : PostingsConsumer
			{
				private int lastDocID;

				private int lastPos;

				private int lastPayloadLen;

				internal int docCount;

				internal RAMOutputStream buffer = new RAMOutputStream();

				internal int lastOffsetLength;

				internal int lastOffset;

				// NOTE: not private so we don't pay access check at runtime:
				/// <exception cref="System.IO.IOException"></exception>
				public override void StartDoc(int docID, int termDocFreq)
				{
					//System.out.println("    startDoc docID=" + docID + " freq=" + termDocFreq);
					int delta = docID - this.lastDocID;
					//HM:revisit 
					//assert docID == 0 || delta > 0;
					this.lastDocID = docID;
					this.docCount++;
					if (this._enclosing.field.GetIndexOptions() == FieldInfo.IndexOptions.DOCS_ONLY)
					{
						this.buffer.WriteVInt(delta);
					}
					else
					{
						if (termDocFreq == 1)
						{
							this.buffer.WriteVInt((delta << 1) | 1);
						}
						else
						{
							this.buffer.WriteVInt(delta << 1);
							//HM:revisit 
							//assert termDocFreq > 0;
							this.buffer.WriteVInt(termDocFreq);
						}
					}
					this.lastPos = 0;
					this.lastOffset = 0;
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override void AddPosition(int pos, BytesRef payload, int startOffset, int 
					endOffset)
				{
					//HM:revisit 
					//assert payload == null || field.hasPayloads();
					//System.out.println("      addPos pos=" + pos + " payload=" + payload);
					int delta = pos - this.lastPos;
					//HM:revisit 
					//assert delta >= 0;
					this.lastPos = pos;
					int payloadLen = 0;
					if (this._enclosing.field.HasPayloads())
					{
						payloadLen = payload == null ? 0 : payload.length;
						if (payloadLen != this.lastPayloadLen)
						{
							this.lastPayloadLen = payloadLen;
							this.buffer.WriteVInt((delta << 1) | 1);
							this.buffer.WriteVInt(payloadLen);
						}
						else
						{
							this.buffer.WriteVInt(delta << 1);
						}
					}
					else
					{
						this.buffer.WriteVInt(delta);
					}
					if (this._enclosing.field.GetIndexOptions().CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
						) >= 0)
					{
						// don't use startOffset - lastEndOffset, because this creates lots of negative vints for synonyms,
						// and the numbers aren't that much smaller anyways.
						int offsetDelta = startOffset - this.lastOffset;
						int offsetLength = endOffset - startOffset;
						if (offsetLength != this.lastOffsetLength)
						{
							this.buffer.WriteVInt(offsetDelta << 1 | 1);
							this.buffer.WriteVInt(offsetLength);
						}
						else
						{
							this.buffer.WriteVInt(offsetDelta << 1);
						}
						this.lastOffset = startOffset;
						this.lastOffsetLength = offsetLength;
					}
					if (payloadLen > 0)
					{
						this.buffer.WriteBytes(payload.bytes, payload.offset, payloadLen);
					}
				}

				public override void FinishDoc()
				{
				}

				public virtual MemoryPostingsFormat.TermsWriter.PostingsWriter Reset()
				{
					//HM:revisit 
					//assert buffer.getFilePointer() == 0;
					this.lastDocID = 0;
					this.docCount = 0;
					this.lastPayloadLen = 0;
					// force first offset to write its length
					this.lastOffsetLength = -1;
					return this;
				}

				internal PostingsWriter(TermsWriter _enclosing)
				{
					this._enclosing = _enclosing;
				}

				private readonly TermsWriter _enclosing;
			}

			private readonly MemoryPostingsFormat.TermsWriter.PostingsWriter postingsWriter;

			public override PostingsConsumer StartTerm(BytesRef text)
			{
				//System.out.println("  startTerm term=" + text.utf8ToString());
				return postingsWriter.Reset();
			}

			private readonly RAMOutputStream buffer2 = new RAMOutputStream();

			private readonly BytesRef spare = new BytesRef();

			private byte[] finalBuffer = new byte[128];

			private readonly IntsRef scratchIntsRef = new IntsRef();

			/// <exception cref="System.IO.IOException"></exception>
			public override void FinishTerm(BytesRef text, TermStats stats)
			{
				//HM:revisit 
				//assert postingsWriter.docCount == stats.docFreq;
				//HM:revisit 
				//assert buffer2.getFilePointer() == 0;
				buffer2.WriteVInt(stats.docFreq);
				if (field.GetIndexOptions() != FieldInfo.IndexOptions.DOCS_ONLY)
				{
					buffer2.WriteVLong(stats.totalTermFreq - stats.docFreq);
				}
				int pos = (int)buffer2.GetFilePointer();
				buffer2.WriteTo(finalBuffer, 0);
				buffer2.Reset();
				int totalBytes = pos + (int)postingsWriter.buffer.GetFilePointer();
				if (totalBytes > finalBuffer.Length)
				{
					finalBuffer = ArrayUtil.Grow(finalBuffer, totalBytes);
				}
				postingsWriter.buffer.WriteTo(finalBuffer, pos);
				postingsWriter.buffer.Reset();
				spare.bytes = finalBuffer;
				spare.length = totalBytes;
				//System.out.println("    finishTerm term=" + text.utf8ToString() + " " + totalBytes + " bytes totalTF=" + stats.totalTermFreq);
				//for(int i=0;i<totalBytes;i++) {
				//  System.out.println("      " + Integer.toHexString(finalBuffer[i]&0xFF));
				//}
				builder.Add(Lucene.Net.Util.Fst.Util.ToIntsRef(text, scratchIntsRef), BytesRef
					.DeepCopyOf(spare));
				termCount++;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Finish(long sumTotalTermFreq, long sumDocFreq, int docCount)
			{
				if (termCount > 0)
				{
					@out.WriteVInt(termCount);
					@out.WriteVInt(field.number);
					if (field.GetIndexOptions() != FieldInfo.IndexOptions.DOCS_ONLY)
					{
						@out.WriteVLong(sumTotalTermFreq);
					}
					@out.WriteVLong(sumDocFreq);
					@out.WriteVInt(docCount);
					FST<BytesRef> fst = builder.Finish();
					fst.Save(@out);
				}
			}

			//System.out.println("finish field=" + field.name + " fp=" + out.getFilePointer());
			public override IComparer<BytesRef> GetComparator()
			{
				return BytesRef.GetUTF8SortedAsUnicodeComparator();
			}
		}

		private static string EXTENSION = "ram";

		private static readonly string CODEC_NAME = "MemoryPostings";

		private const int VERSION_START = 0;

		private const int VERSION_CURRENT = VERSION_START;

		/// <exception cref="System.IO.IOException"></exception>
		public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
		{
			string fileName = IndexFileNames.SegmentFileName(state.segmentInfo.name, state.segmentSuffix
				, EXTENSION);
			IndexOutput @out = state.directory.CreateOutput(fileName, state.context);
			bool success = false;
			try
			{
				CodecUtil.WriteHeader(@out, CODEC_NAME, VERSION_CURRENT);
				success = true;
			}
			finally
			{
				if (!success)
				{
					IOUtils.CloseWhileHandlingException(@out);
				}
			}
			return new _FieldsConsumer_325(this, @out);
		}

		private sealed class _FieldsConsumer_325 : FieldsConsumer
		{
			public _FieldsConsumer_325(MemoryPostingsFormat _enclosing, IndexOutput @out)
			{
				this._enclosing = _enclosing;
				this.@out = @out;
			}

			public override TermsConsumer AddField(FieldInfo field)
			{
				//System.out.println("\naddField field=" + field.name);
				return new MemoryPostingsFormat.TermsWriter(@out, field, this._enclosing.doPackFST
					, this._enclosing.acceptableOverheadRatio);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Close()
			{
				// EOF marker:
				try
				{
					@out.WriteVInt(0);
					CodecUtil.WriteFooter(@out);
				}
				finally
				{
					@out.Close();
				}
			}

			private readonly MemoryPostingsFormat _enclosing;

			private readonly IndexOutput @out;
		}

		private sealed class FSTDocsEnum : DocsEnum
		{
			private readonly FieldInfo.IndexOptions indexOptions;

			private readonly bool storePayloads;

			private byte[] buffer = new byte[16];

			private readonly ByteArrayDataInput @in;

			private Bits liveDocs;

			private int docUpto;

			private int docID = -1;

			private int accum;

			private int freq;

			private int payloadLen;

			private int numDocs;

			public FSTDocsEnum(FieldInfo.IndexOptions indexOptions, bool storePayloads)
			{
				@in = new ByteArrayDataInput(buffer);
				this.indexOptions = indexOptions;
				this.storePayloads = storePayloads;
			}

			public bool CanReuse(FieldInfo.IndexOptions indexOptions, bool storePayloads)
			{
				return indexOptions == this.indexOptions && storePayloads == this.storePayloads;
			}

			public MemoryPostingsFormat.FSTDocsEnum Reset(BytesRef bufferIn, Bits liveDocs, int
				 numDocs)
			{
				//HM:revisit 
				//assert numDocs > 0;
				if (buffer.Length < bufferIn.length)
				{
					buffer = ArrayUtil.Grow(buffer, bufferIn.length);
				}
				@in.Reset(buffer, 0, bufferIn.length);
				System.Array.Copy(bufferIn.bytes, bufferIn.offset, buffer, 0, bufferIn.length);
				this.liveDocs = liveDocs;
				docID = -1;
				accum = 0;
				docUpto = 0;
				freq = 1;
				payloadLen = 0;
				this.numDocs = numDocs;
				return this;
			}

			public override int NextDoc()
			{
				while (true)
				{
					//System.out.println("  nextDoc cycle docUpto=" + docUpto + " numDocs=" + numDocs + " fp=" + in.getPosition() + " this=" + this);
					if (docUpto == numDocs)
					{
						// System.out.println("    END");
						return docID = NO_MORE_DOCS;
					}
					docUpto++;
					if (indexOptions == FieldInfo.IndexOptions.DOCS_ONLY)
					{
						accum += @in.ReadVInt();
					}
					else
					{
						int code = @in.ReadVInt();
						accum += (int)(((uint)code) >> 1);
						//System.out.println("  docID=" + accum + " code=" + code);
						if ((code & 1) != 0)
						{
							freq = 1;
						}
						else
						{
							freq = @in.ReadVInt();
						}
						//HM:revisit 
						//assert freq > 0;
						if (indexOptions == FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
						{
							// Skip positions/payloads
							for (int posUpto = 0; posUpto < freq; posUpto++)
							{
								if (!storePayloads)
								{
									@in.ReadVInt();
								}
								else
								{
									int posCode = @in.ReadVInt();
									if ((posCode & 1) != 0)
									{
										payloadLen = @in.ReadVInt();
									}
									@in.SkipBytes(payloadLen);
								}
							}
						}
						else
						{
							if (indexOptions == FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS)
							{
								// Skip positions/offsets/payloads
								for (int posUpto = 0; posUpto < freq; posUpto++)
								{
									int posCode = @in.ReadVInt();
									if (storePayloads && ((posCode & 1) != 0))
									{
										payloadLen = @in.ReadVInt();
									}
									if ((@in.ReadVInt() & 1) != 0)
									{
										// new offset length
										@in.ReadVInt();
									}
									if (storePayloads)
									{
										@in.SkipBytes(payloadLen);
									}
								}
							}
						}
					}
					if (liveDocs == null || liveDocs.Get(accum))
					{
						//System.out.println("    return docID=" + accum + " freq=" + freq);
						return (docID = accum);
					}
				}
			}

			public override int DocID()
			{
				return docID;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Advance(int target)
			{
				// TODO: we could make more efficient version, but, it
				// should be rare that this will matter in practice
				// since usually apps will not store "big" fields in
				// this codec!
				return SlowAdvance(target);
			}

			public override int Freq()
			{
				return freq;
			}

			public override long Cost()
			{
				return numDocs;
			}
		}

		private sealed class FSTDocsAndPositionsEnum : DocsAndPositionsEnum
		{
			private readonly bool storePayloads;

			private byte[] buffer = new byte[16];

			private readonly ByteArrayDataInput @in;

			private Bits liveDocs;

			private int docUpto;

			private int docID = -1;

			private int accum;

			private int freq;

			private int numDocs;

			private int posPending;

			private int payloadLength;

			internal readonly bool storeOffsets;

			internal int offsetLength;

			internal int startOffset;

			private int pos;

			private readonly BytesRef payload = new BytesRef();

			public FSTDocsAndPositionsEnum(bool storePayloads, bool storeOffsets)
			{
				@in = new ByteArrayDataInput(buffer);
				this.storePayloads = storePayloads;
				this.storeOffsets = storeOffsets;
			}

			public bool CanReuse(bool storePayloads, bool storeOffsets)
			{
				return storePayloads == this.storePayloads && storeOffsets == this.storeOffsets;
			}

			public MemoryPostingsFormat.FSTDocsAndPositionsEnum Reset(BytesRef bufferIn, Bits
				 liveDocs, int numDocs)
			{
				//HM:revisit 
				//assert numDocs > 0;
				// System.out.println("D&P reset bytes this=" + this);
				// for(int i=bufferIn.offset;i<bufferIn.length;i++) {
				//   System.out.println("  " + Integer.toHexString(bufferIn.bytes[i]&0xFF));
				// }
				if (buffer.Length < bufferIn.length)
				{
					buffer = ArrayUtil.Grow(buffer, bufferIn.length);
				}
				@in.Reset(buffer, 0, bufferIn.length - bufferIn.offset);
				System.Array.Copy(bufferIn.bytes, bufferIn.offset, buffer, 0, bufferIn.length);
				this.liveDocs = liveDocs;
				docID = -1;
				accum = 0;
				docUpto = 0;
				payload.bytes = buffer;
				payloadLength = 0;
				this.numDocs = numDocs;
				posPending = 0;
				startOffset = storeOffsets ? 0 : -1;
				// always return -1 if no offsets are stored
				offsetLength = 0;
				return this;
			}

			public override int NextDoc()
			{
				while (posPending > 0)
				{
					NextPosition();
				}
				while (true)
				{
					//System.out.println("  nextDoc cycle docUpto=" + docUpto + " numDocs=" + numDocs + " fp=" + in.getPosition() + " this=" + this);
					if (docUpto == numDocs)
					{
						//System.out.println("    END");
						return docID = NO_MORE_DOCS;
					}
					docUpto++;
					int code = @in.ReadVInt();
					accum += (int)(((uint)code) >> 1);
					if ((code & 1) != 0)
					{
						freq = 1;
					}
					else
					{
						freq = @in.ReadVInt();
					}
					//HM:revisit 
					//assert freq > 0;
					if (liveDocs == null || liveDocs.Get(accum))
					{
						pos = 0;
						startOffset = storeOffsets ? 0 : -1;
						posPending = freq;
						//System.out.println("    return docID=" + accum + " freq=" + freq);
						return (docID = accum);
					}
					// Skip positions
					for (int posUpto = 0; posUpto < freq; posUpto++)
					{
						if (!storePayloads)
						{
							@in.ReadVInt();
						}
						else
						{
							int skipCode = @in.ReadVInt();
							if ((skipCode & 1) != 0)
							{
								payloadLength = @in.ReadVInt();
							}
						}
						//System.out.println("    new payloadLen=" + payloadLength);
						if (storeOffsets)
						{
							if ((@in.ReadVInt() & 1) != 0)
							{
								// new offset length
								offsetLength = @in.ReadVInt();
							}
						}
						if (storePayloads)
						{
							@in.SkipBytes(payloadLength);
						}
					}
				}
			}

			public override int NextPosition()
			{
				//System.out.println("    nextPos storePayloads=" + storePayloads + " this=" + this);
				//HM:revisit 
				//assert posPending > 0;
				posPending--;
				if (!storePayloads)
				{
					pos += @in.ReadVInt();
				}
				else
				{
					int code = @in.ReadVInt();
					pos += (int)(((uint)code) >> 1);
					if ((code & 1) != 0)
					{
						payloadLength = @in.ReadVInt();
					}
				}
				//System.out.println("      new payloadLen=" + payloadLength);
				//} else {
				//System.out.println("      same payloadLen=" + payloadLength);
				if (storeOffsets)
				{
					int offsetCode = @in.ReadVInt();
					if ((offsetCode & 1) != 0)
					{
						// new offset length
						offsetLength = @in.ReadVInt();
					}
					startOffset += (int)(((uint)offsetCode) >> 1);
				}
				if (storePayloads)
				{
					payload.offset = @in.GetPosition();
					@in.SkipBytes(payloadLength);
					payload.length = payloadLength;
				}
				//System.out.println("      pos=" + pos + " payload=" + payload + " fp=" + in.getPosition());
				return pos;
			}

			public override int StartOffset()
			{
				return startOffset;
			}

			public override int EndOffset()
			{
				return startOffset + offsetLength;
			}

			public override BytesRef GetPayload()
			{
				return payload.length > 0 ? payload : null;
			}

			public override int DocID()
			{
				return docID;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Advance(int target)
			{
				// TODO: we could make more efficient version, but, it
				// should be rare that this will matter in practice
				// since usually apps will not store "big" fields in
				// this codec!
				return SlowAdvance(target);
			}

			public override int Freq()
			{
				return freq;
			}

			public override long Cost()
			{
				return numDocs;
			}
		}

		private sealed class FSTTermsEnum : TermsEnum
		{
			private readonly FieldInfo field;

			private readonly BytesRefFSTEnum<BytesRef> fstEnum;

			private readonly ByteArrayDataInput buffer = new ByteArrayDataInput();

			private bool didDecode;

			private int docFreq;

			private long totalTermFreq;

			private BytesRefFSTEnum.InputOutput<BytesRef> current;

			private BytesRef postingsSpare = new BytesRef();

			public FSTTermsEnum(FieldInfo field, FST<BytesRef> fst)
			{
				this.field = field;
				fstEnum = new BytesRefFSTEnum<BytesRef>(fst);
			}

			private void DecodeMetaData()
			{
				if (!didDecode)
				{
					buffer.Reset(current.output.bytes, current.output.offset, current.output.length);
					docFreq = buffer.ReadVInt();
					if (field.GetIndexOptions() != FieldInfo.IndexOptions.DOCS_ONLY)
					{
						totalTermFreq = docFreq + buffer.ReadVLong();
					}
					else
					{
						totalTermFreq = -1;
					}
					postingsSpare.bytes = current.output.bytes;
					postingsSpare.offset = buffer.GetPosition();
					postingsSpare.length = current.output.length - (buffer.GetPosition() - current.output
						.offset);
					//System.out.println("  df=" + docFreq + " totTF=" + totalTermFreq + " offset=" + buffer.getPosition() + " len=" + current.output.length);
					didDecode = true;
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override bool SeekExact(BytesRef text)
			{
				//System.out.println("te.seekExact text=" + field.name + ":" + text.utf8ToString() + " this=" + this);
				current = fstEnum.SeekExact(text);
				didDecode = false;
				return current != null;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override TermsEnum.SeekStatus SeekCeil(BytesRef text)
			{
				//System.out.println("te.seek text=" + field.name + ":" + text.utf8ToString() + " this=" + this);
				current = fstEnum.SeekCeil(text);
				if (current == null)
				{
					return TermsEnum.SeekStatus.END;
				}
				else
				{
					// System.out.println("  got term=" + current.input.utf8ToString());
					// for(int i=0;i<current.output.length;i++) {
					//   System.out.println("    " + Integer.toHexString(current.output.bytes[i]&0xFF));
					// }
					didDecode = false;
					if (text.Equals(current.input))
					{
						//System.out.println("  found!");
						return TermsEnum.SeekStatus.FOUND;
					}
					else
					{
						//System.out.println("  not found: " + current.input.utf8ToString());
						return TermsEnum.SeekStatus.NOT_FOUND;
					}
				}
			}

			public override DocsEnum Docs(Bits liveDocs, DocsEnum reuse, int flags)
			{
				DecodeMetaData();
				MemoryPostingsFormat.FSTDocsEnum docsEnum;
				if (reuse == null || !(reuse is MemoryPostingsFormat.FSTDocsEnum))
				{
					docsEnum = new MemoryPostingsFormat.FSTDocsEnum(field.GetIndexOptions(), field.HasPayloads
						());
				}
				else
				{
					docsEnum = (MemoryPostingsFormat.FSTDocsEnum)reuse;
					if (!docsEnum.CanReuse(field.GetIndexOptions(), field.HasPayloads()))
					{
						docsEnum = new MemoryPostingsFormat.FSTDocsEnum(field.GetIndexOptions(), field.HasPayloads
							());
					}
				}
				return docsEnum.Reset(this.postingsSpare, liveDocs, docFreq);
			}

			public override DocsAndPositionsEnum DocsAndPositions(Bits liveDocs, DocsAndPositionsEnum
				 reuse, int flags)
			{
				bool hasOffsets = field.GetIndexOptions().CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
					) >= 0;
				if (field.GetIndexOptions().CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS
					) < 0)
				{
					return null;
				}
				DecodeMetaData();
				MemoryPostingsFormat.FSTDocsAndPositionsEnum docsAndPositionsEnum;
				if (reuse == null || !(reuse is MemoryPostingsFormat.FSTDocsAndPositionsEnum))
				{
					docsAndPositionsEnum = new MemoryPostingsFormat.FSTDocsAndPositionsEnum(field.HasPayloads
						(), hasOffsets);
				}
				else
				{
					docsAndPositionsEnum = (MemoryPostingsFormat.FSTDocsAndPositionsEnum)reuse;
					if (!docsAndPositionsEnum.CanReuse(field.HasPayloads(), hasOffsets))
					{
						docsAndPositionsEnum = new MemoryPostingsFormat.FSTDocsAndPositionsEnum(field.HasPayloads
							(), hasOffsets);
					}
				}
				//System.out.println("D&P reset this=" + this);
				return docsAndPositionsEnum.Reset(postingsSpare, liveDocs, docFreq);
			}

			public override BytesRef Term()
			{
				return current.input;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override BytesRef Next()
			{
				//System.out.println("te.next");
				current = fstEnum.Next();
				if (current == null)
				{
					//System.out.println("  END");
					return null;
				}
				didDecode = false;
				//System.out.println("  term=" + field.name + ":" + current.input.utf8ToString());
				return current.input;
			}

			public override int DocFreq()
			{
				DecodeMetaData();
				return docFreq;
			}

			public override long TotalTermFreq()
			{
				DecodeMetaData();
				return totalTermFreq;
			}

			public override IComparer<BytesRef> GetComparator()
			{
				return BytesRef.GetUTF8SortedAsUnicodeComparator();
			}

			public override void SeekExact(long ord)
			{
				// NOTE: we could add this...
				throw new NotSupportedException();
			}

			public override long Ord()
			{
				// NOTE: we could add this...
				throw new NotSupportedException();
			}
		}

		private sealed class TermsReader : Terms
		{
			private readonly long sumTotalTermFreq;

			private readonly long sumDocFreq;

			private readonly int docCount;

			private readonly int termCount;

			private FST<BytesRef> fst;

			private readonly ByteSequenceOutputs outputs = ByteSequenceOutputs.GetSingleton();

			private readonly FieldInfo field;

			/// <exception cref="System.IO.IOException"></exception>
			public TermsReader(FieldInfos fieldInfos, IndexInput @in, int termCount)
			{
				this.termCount = termCount;
				int fieldNumber = @in.ReadVInt();
				field = fieldInfos.FieldInfo(fieldNumber);
				if (field.GetIndexOptions() != FieldInfo.IndexOptions.DOCS_ONLY)
				{
					sumTotalTermFreq = @in.ReadVLong();
				}
				else
				{
					sumTotalTermFreq = -1;
				}
				sumDocFreq = @in.ReadVLong();
				docCount = @in.ReadVInt();
				fst = new FST<BytesRef>(@in, outputs);
			}

			public override long GetSumTotalTermFreq()
			{
				return sumTotalTermFreq;
			}

			public override long GetSumDocFreq()
			{
				return sumDocFreq;
			}

			public override int GetDocCount()
			{
				return docCount;
			}

			public override long Size()
			{
				return termCount;
			}

			public override TermsEnum Iterator(TermsEnum reuse)
			{
				return new MemoryPostingsFormat.FSTTermsEnum(field, fst);
			}

			public override IComparer<BytesRef> GetComparator()
			{
				return BytesRef.GetUTF8SortedAsUnicodeComparator();
			}

			public override bool HasFreqs()
			{
				return field.GetIndexOptions().CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS) >=
					 0;
			}

			public override bool HasOffsets()
			{
				return field.GetIndexOptions().CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
					) >= 0;
			}

			public override bool HasPositions()
			{
				return field.GetIndexOptions().CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS
					) >= 0;
			}

			public override bool HasPayloads()
			{
				return field.HasPayloads();
			}

			public long RamBytesUsed()
			{
				return ((fst != null) ? fst.SizeInBytes() : 0);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FieldsProducer FieldsProducer(SegmentReadState state)
		{
			string fileName = IndexFileNames.SegmentFileName(state.segmentInfo.name, state.segmentSuffix
				, EXTENSION);
			ChecksumIndexInput @in = state.directory.OpenChecksumInput(fileName, IOContext.READONCE
				);
			SortedDictionary<string, MemoryPostingsFormat.TermsReader> fields = new SortedDictionary
				<string, MemoryPostingsFormat.TermsReader>();
			try
			{
				CodecUtil.CheckHeader(@in, CODEC_NAME, VERSION_START, VERSION_CURRENT);
				while (true)
				{
					int termCount = @in.ReadVInt();
					if (termCount == 0)
					{
						break;
					}
					MemoryPostingsFormat.TermsReader termsReader = new MemoryPostingsFormat.TermsReader
						(state.fieldInfos, @in, termCount);
					// System.out.println("load field=" + termsReader.field.name);
					fields.Put(termsReader.field.name, termsReader);
				}
				CodecUtil.CheckFooter(@in);
			}
			finally
			{
				@in.Close();
			}
			return new _FieldsProducer_922(fields);
		}

		private sealed class _FieldsProducer_922 : FieldsProducer
		{
			public _FieldsProducer_922(SortedDictionary<string, MemoryPostingsFormat.TermsReader
				> fields)
			{
				this.fields = fields;
			}

			public override Sharpen.Iterator<string> Iterator()
			{
				return Sharpen.Collections.UnmodifiableSet(fields.Keys).Iterator();
			}

			public override Terms Terms(string field)
			{
				return fields.Get(field);
			}

			public override int Size()
			{
				return fields.Count;
			}

			public override void Close()
			{
				// Drop ref to FST:
				foreach (MemoryPostingsFormat.TermsReader termsReader in fields.Values)
				{
					termsReader.fst = null;
				}
			}

			public override long RamBytesUsed()
			{
				long sizeInBytes = 0;
				foreach (KeyValuePair<string, MemoryPostingsFormat.TermsReader> entry in fields.EntrySet
					())
				{
					sizeInBytes += (entry.Key.Length * RamUsageEstimator.NUM_BYTES_CHAR);
					sizeInBytes += entry.Value.RamBytesUsed();
				}
				return sizeInBytes;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void CheckIntegrity()
			{
			}

			private readonly SortedDictionary<string, MemoryPostingsFormat.TermsReader> fields;
		}
	}
}
