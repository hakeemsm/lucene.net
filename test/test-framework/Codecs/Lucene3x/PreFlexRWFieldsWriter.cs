using System;
using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Codecs.Lucene3x;
using Lucene.Net.Util;

namespace Lucene.Net.Codecs.Lucene3x.TestFramework
{
	internal class PreFlexRWFieldsWriter : FieldsConsumer
	{
		private readonly TermInfosWriter termsOut;

		private readonly IndexOutput freqOut;

		private readonly IndexOutput proxOut;

		private readonly PreFlexRWSkipListWriter skipListWriter;

		private readonly int totalNumDocs;

		/// <exception cref="System.IO.IOException"></exception>
		public PreFlexRWFieldsWriter(SegmentWriteState state)
		{
			termsOut = new TermInfosWriter(state.directory, state.segmentInfo.name, state.fieldInfos
				, state.termIndexInterval);
			bool success = false;
			try
			{
				string freqFile = IndexFileNames.SegmentFileName(state.segmentInfo.name, string.Empty
					, Lucene3xPostingsFormat.FREQ_EXTENSION);
				freqOut = state.directory.CreateOutput(freqFile, state.context);
				totalNumDocs = state.segmentInfo.DocCount;
				success = true;
			}
			finally
			{
				if (!success)
				{
					IOUtils.CloseWhileHandlingException((IDisposable)termsOut);
				}
			}
			success = false;
			try
			{
				if (state.fieldInfos.HasProx)
				{
					string proxFile = IndexFileNames.SegmentFileName(state.segmentInfo.name, string.Empty
						, Lucene3xPostingsFormat.PROX_EXTENSION);
					proxOut = state.directory.CreateOutput(proxFile, state.context);
				}
				else
				{
					proxOut = null;
				}
				success = true;
			}
			finally
			{
				if (!success)
				{
					IOUtils.CloseWhileHandlingException((IDisposable)termsOut, freqOut);
				}
			}
			skipListWriter = new PreFlexRWSkipListWriter(termsOut.skipInterval, termsOut.maxSkipLevels
				, totalNumDocs, freqOut, proxOut);
		}

		//System.out.println("\nw start seg=" + segment);
		/// <exception cref="System.IO.IOException"></exception>
		public override TermsConsumer AddField(FieldInfo field)
		{
			 
			//assert field.number != -1;
			if (field.IndexOptionsValue.GetValueOrDefault().CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
				) >= 0)
			{
				throw new NotSupportedException("this codec cannot index offsets");
			}
			//System.out.println("w field=" + field.name + " storePayload=" + field.storePayloads + " number=" + field.number);
			return new PreFlexTermsWriter(this, field);
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected override void Dispose(bool disposing)
		{
			IOUtils.Close(termsOut, freqOut, proxOut);
		}

		private class PreFlexTermsWriter : TermsConsumer
		{
			private readonly FieldInfo fieldInfo;

			private readonly bool omitTF;

			private readonly bool storePayloads;

			private readonly TermInfo termInfo = new TermInfo();

			private readonly PostingsWriter postingsWriter;

			public PreFlexTermsWriter(PreFlexRWFieldsWriter _enclosing, FieldInfo fieldInfo)
			{
				this._enclosing = _enclosing;
				postingsWriter = new PostingsWriter(this);
				this.fieldInfo = fieldInfo;
				this.omitTF = fieldInfo.IndexOptionsValue.GetValueOrDefault() == FieldInfo.IndexOptions.DOCS_ONLY;
				this.storePayloads = fieldInfo.HasPayloads;
			}

			private class PostingsWriter : PostingsConsumer
			{
				private int lastDocID;

				private int lastPayloadLength = -1;

				private int lastPosition;

				private int df;

				public virtual PostingsWriter Reset()
				{
					this.df = 0;
					this.lastDocID = 0;
					this.lastPayloadLength = -1;
					return this;
				}

				
				public override void StartDoc(int docID, int termDocFreq)
				{
					//System.out.println("    w doc=" + docID);
					int delta = docID - this.lastDocID;
					if (docID < 0 || (this.df > 0 && delta <= 0))
					{
						throw new CorruptIndexException("docs out of order (" + docID + " <= " + this.lastDocID
							 + " )");
					}
					if ((++this.df % this._enclosing._enclosing.termsOut.skipInterval) == 0)
					{
						this._enclosing._enclosing.skipListWriter.SetSkipData(this.lastDocID, this._enclosing
							.storePayloads, this.lastPayloadLength);
						this._enclosing._enclosing.skipListWriter.BufferSkip(this.df);
					}
					this.lastDocID = docID;
					 
					//assert docID < totalNumDocs: "docID=" + docID + " totalNumDocs=" + totalNumDocs;
					if (this._enclosing.omitTF)
					{
						this._enclosing._enclosing.freqOut.WriteVInt(delta);
					}
					else
					{
						int code = delta << 1;
						if (termDocFreq == 1)
						{
							this._enclosing._enclosing.freqOut.WriteVInt(code | 1);
						}
						else
						{
							this._enclosing._enclosing.freqOut.WriteVInt(code);
							this._enclosing._enclosing.freqOut.WriteVInt(termDocFreq);
						}
					}
					this.lastPosition = 0;
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override void AddPosition(int position, BytesRef payload, int startOffset, 
					int endOffset)
				{
					 
					//assert proxOut != null;
					 
					//assert startOffset == -1;
					 
					//assert endOffset == -1;
					//System.out.println("      w pos=" + position + " payl=" + payload);
					int delta = position - this.lastPosition;
					this.lastPosition = position;
					if (this._enclosing.storePayloads)
					{
						int payloadLength = payload == null ? 0 : payload.length;
						if (payloadLength != this.lastPayloadLength)
						{
							//System.out.println("        write payload len=" + payloadLength);
							this.lastPayloadLength = payloadLength;
							this._enclosing._enclosing.proxOut.WriteVInt((delta << 1) | 1);
							this._enclosing._enclosing.proxOut.WriteVInt(payloadLength);
						}
						else
						{
							this._enclosing._enclosing.proxOut.WriteVInt(delta << 1);
						}
						if (payloadLength > 0)
						{
							this._enclosing._enclosing.proxOut.WriteBytes(payload.bytes, payload.offset, payload
								.length);
						}
					}
					else
					{
						this._enclosing._enclosing.proxOut.WriteVInt(delta);
					}
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override void FinishDoc()
				{
				}

				internal PostingsWriter(PreFlexTermsWriter _enclosing)
				{
					this._enclosing = _enclosing;
				}

				private readonly PreFlexTermsWriter _enclosing;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override PostingsConsumer StartTerm(BytesRef text)
			{
				//System.out.println("  w term=" + text.utf8ToString());
				this._enclosing.skipListWriter.ResetSkip();
				this.termInfo.freqPointer = this._enclosing.freqOut.FilePointer;
				if (this._enclosing.proxOut != null)
				{
					this.termInfo.proxPointer = this._enclosing.proxOut.FilePointer;
				}
				return this.postingsWriter.Reset();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void FinishTerm(BytesRef text, TermStats stats)
			{
				if (stats.docFreq > 0)
				{
					long skipPointer = this._enclosing.skipListWriter.WriteSkip(this._enclosing.freqOut
						);
					this.termInfo.docFreq = stats.docFreq;
					this.termInfo.skipOffset = (int)(skipPointer - this.termInfo.freqPointer);
					//System.out.println("  w finish term=" + text.utf8ToString() + " fnum=" + fieldInfo.number);
					this._enclosing.termsOut.Add(this.fieldInfo.number, text, this.termInfo);
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Finish(long sumTotalTermCount, long sumDocFreq, int docCount
				)
			{
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override IComparer<BytesRef> Comparator
			{
			    get { return BytesRef.UTF8SortedAsUnicodeComparer; }
			}

			private readonly PreFlexRWFieldsWriter _enclosing;
		}
	}
}
