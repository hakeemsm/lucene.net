/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Pulsing;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Codecs.Pulsing
{
	/// <summary>Writer for the pulsing format.</summary>
	/// <remarks>
	/// Writer for the pulsing format.
	/// <p>
	/// Wraps another postings implementation and decides
	/// (based on total number of occurrences), whether a terms
	/// postings should be inlined into the term dictionary,
	/// or passed through to the wrapped writer.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public sealed class PulsingPostingsWriter : PostingsWriterBase
	{
		internal static readonly string CODEC = "PulsedPostingsWriter";

		internal static readonly string SUMMARY_EXTENSION = "smy";

		internal const int VERSION_START = 0;

		internal const int VERSION_META_ARRAY = 1;

		internal const int VERSION_CURRENT = VERSION_META_ARRAY;

		private SegmentWriteState segmentState;

		private IndexOutput termsOut;

		private IList<PulsingPostingsWriter.FieldMetaData> fields;

		private FieldInfo.IndexOptions indexOptions;

		private bool storePayloads;

		private int longsSize;

		private long[] longs;

		internal bool absolute;

		private class PulsingTermState : BlockTermState
		{
			private byte[] bytes;

			private BlockTermState wrappedState;

			// TODO: we now inline based on total TF of the term,
			// but it might be better to inline by "net bytes used"
			// so that a term that has only 1 posting but a huge
			// payload would not be inlined.  Though this is
			// presumably rare in practice...
			// recording field summary
			// To add a new version, increment from the last one, and
			// change VERSION_CURRENT to point to your new version:
			// information for wrapped PF, in current field
			public override string ToString()
			{
				if (bytes != null)
				{
					return "inlined";
				}
				else
				{
					return "not inlined wrapped=" + wrappedState;
				}
			}
		}

		private readonly PulsingPostingsWriter.Position[] pending;

		private int pendingCount = 0;

		private PulsingPostingsWriter.Position currentDoc;

		private sealed class Position
		{
			internal BytesRef payload;

			internal int termFreq;

			internal int pos;

			internal int docID;

			internal int startOffset;

			internal int endOffset;
			// one entry per position
			// -1 once we've hit too many positions
			// first Position entry of current doc
			// only incremented on first position for a given doc
		}

		private sealed class FieldMetaData
		{
			internal int fieldNumber;

			internal int longsSize;

			internal FieldMetaData(int number, int size)
			{
				fieldNumber = number;
				longsSize = size;
			}
		}

		internal readonly PostingsWriterBase wrappedPostingsWriter;

		/// <summary>
		/// If the total number of positions (summed across all docs
		/// for this term) is &lt;= maxPositions, then the postings are
		/// inlined into terms dict
		/// </summary>
		public PulsingPostingsWriter(SegmentWriteState state, int maxPositions, PostingsWriterBase
			 wrappedPostingsWriter)
		{
			// TODO: -- lazy init this?  ie, if every single term
			// was inlined (eg for a "primary key" field) then we
			// never need to use this fallback?  Fallback writer for
			// non-inlined terms:
			pending = new PulsingPostingsWriter.Position[maxPositions];
			for (int i = 0; i < maxPositions; i++)
			{
				pending[i] = new PulsingPostingsWriter.Position();
			}
			fields = new AList<PulsingPostingsWriter.FieldMetaData>();
			// We simply wrap another postings writer, but only call
			// on it when tot positions is >= the cutoff:
			this.wrappedPostingsWriter = wrappedPostingsWriter;
			this.segmentState = state;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Init(IndexOutput termsOut)
		{
			this.termsOut = termsOut;
			CodecUtil.WriteHeader(termsOut, CODEC, VERSION_CURRENT);
			termsOut.WriteVInt(pending.Length);
			// encode maxPositions in header
			wrappedPostingsWriter.Init(termsOut);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override BlockTermState NewTermState()
		{
			PulsingPostingsWriter.PulsingTermState state = new PulsingPostingsWriter.PulsingTermState
				();
			state.wrappedState = wrappedPostingsWriter.NewTermState();
			return state;
		}

		public override void StartTerm()
		{
		}

		//if (DEBUG) System.out.println("PW   startTerm");
		//HM:revisit 
		//assert pendingCount == 0;
		// TODO: -- should we NOT reuse across fields?  would
		// be cleaner
		// Currently, this instance is re-used across fields, so
		// our parent calls setField whenever the field changes
		public override int SetField(FieldInfo fieldInfo)
		{
			this.indexOptions = fieldInfo.GetIndexOptions();
			//if (DEBUG) System.out.println("PW field=" + fieldInfo.name + " indexOptions=" + indexOptions);
			storePayloads = fieldInfo.HasPayloads();
			absolute = false;
			longsSize = wrappedPostingsWriter.SetField(fieldInfo);
			longs = new long[longsSize];
			fields.AddItem(new PulsingPostingsWriter.FieldMetaData(fieldInfo.number, longsSize
				));
			return 0;
		}

		private bool DEBUG;

		//DEBUG = BlockTreeTermsWriter.DEBUG;
		/// <exception cref="System.IO.IOException"></exception>
		public override void StartDoc(int docID, int termDocFreq)
		{
			//HM:revisit 
			//assert docID >= 0: "got docID=" + docID;
			//if (DEBUG) System.out.println("PW     doc=" + docID);
			if (pendingCount == pending.Length)
			{
				Push();
				//if (DEBUG) System.out.println("PW: wrapped.finishDoc");
				wrappedPostingsWriter.FinishDoc();
			}
			if (pendingCount != -1)
			{
				//HM:revisit 
				//assert pendingCount < pending.length;
				currentDoc = pending[pendingCount];
				currentDoc.docID = docID;
				if (indexOptions == FieldInfo.IndexOptions.DOCS_ONLY)
				{
					pendingCount++;
				}
				else
				{
					if (indexOptions == FieldInfo.IndexOptions.DOCS_AND_FREQS)
					{
						pendingCount++;
						currentDoc.termFreq = termDocFreq;
					}
					else
					{
						currentDoc.termFreq = termDocFreq;
					}
				}
			}
			else
			{
				// We've already seen too many docs for this term --
				// just forward to our fallback writer
				wrappedPostingsWriter.StartDoc(docID, termDocFreq);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void AddPosition(int position, BytesRef payload, int startOffset, 
			int endOffset)
		{
			//if (DEBUG) System.out.println("PW       pos=" + position + " payload=" + (payload == null ? "null" : payload.length + " bytes"));
			if (pendingCount == pending.Length)
			{
				Push();
			}
			if (pendingCount == -1)
			{
				// We've already seen too many docs for this term --
				// just forward to our fallback writer
				wrappedPostingsWriter.AddPosition(position, payload, startOffset, endOffset);
			}
			else
			{
				// buffer up
				PulsingPostingsWriter.Position pos = pending[pendingCount++];
				pos.pos = position;
				pos.startOffset = startOffset;
				pos.endOffset = endOffset;
				pos.docID = currentDoc.docID;
				if (payload != null && payload.length > 0)
				{
					if (pos.payload == null)
					{
						pos.payload = BytesRef.DeepCopyOf(payload);
					}
					else
					{
						pos.payload.CopyBytes(payload);
					}
				}
				else
				{
					if (pos.payload != null)
					{
						pos.payload.length = 0;
					}
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void FinishDoc()
		{
			// if (DEBUG) System.out.println("PW     finishDoc");
			if (pendingCount == -1)
			{
				wrappedPostingsWriter.FinishDoc();
			}
		}

		private readonly RAMOutputStream buffer = new RAMOutputStream();

		// private int baseDocID;
		/// <summary>Called when we are done adding docs to this term</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public override void FinishTerm(BlockTermState _state)
		{
			PulsingPostingsWriter.PulsingTermState state = (PulsingPostingsWriter.PulsingTermState
				)_state;
			// if (DEBUG) System.out.println("PW   finishTerm docCount=" + stats.docFreq + " pendingCount=" + pendingCount + " pendingTerms.size()=" + pendingTerms.size());
			//HM:revisit 
			//assert pendingCount > 0 || pendingCount == -1;
			if (pendingCount == -1)
			{
				state.wrappedState.docFreq = state.docFreq;
				state.wrappedState.totalTermFreq = state.totalTermFreq;
				state.bytes = null;
				wrappedPostingsWriter.FinishTerm(state.wrappedState);
			}
			else
			{
				// There were few enough total occurrences for this
				// term, so we fully inline our postings data into
				// terms dict, now:
				// TODO: it'd be better to share this encoding logic
				// in some inner codec that knows how to write a
				// single doc / single position, etc.  This way if a
				// given codec wants to store other interesting
				// stuff, it could use this pulsing codec to do so
				if (indexOptions.CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >=
					 0)
				{
					int lastDocID = 0;
					int pendingIDX = 0;
					int lastPayloadLength = -1;
					int lastOffsetLength = -1;
					while (pendingIDX < pendingCount)
					{
						PulsingPostingsWriter.Position doc = pending[pendingIDX];
						int delta = doc.docID - lastDocID;
						lastDocID = doc.docID;
						// if (DEBUG) System.out.println("  write doc=" + doc.docID + " freq=" + doc.termFreq);
						if (doc.termFreq == 1)
						{
							buffer.WriteVInt((delta << 1) | 1);
						}
						else
						{
							buffer.WriteVInt(delta << 1);
							buffer.WriteVInt(doc.termFreq);
						}
						int lastPos = 0;
						int lastOffset = 0;
						for (int posIDX = 0; posIDX < doc.termFreq; posIDX++)
						{
							PulsingPostingsWriter.Position pos = pending[pendingIDX++];
							//HM:revisit 
							//assert pos.docID == doc.docID;
							int posDelta = pos.pos - lastPos;
							lastPos = pos.pos;
							// if (DEBUG) System.out.println("    write pos=" + pos.pos);
							int payloadLength = pos.payload == null ? 0 : pos.payload.length;
							if (storePayloads)
							{
								if (payloadLength != lastPayloadLength)
								{
									buffer.WriteVInt((posDelta << 1) | 1);
									buffer.WriteVInt(payloadLength);
									lastPayloadLength = payloadLength;
								}
								else
								{
									buffer.WriteVInt(posDelta << 1);
								}
							}
							else
							{
								buffer.WriteVInt(posDelta);
							}
							if (indexOptions.CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
								) >= 0)
							{
								//System.out.println("write=" + pos.startOffset + "," + pos.endOffset);
								int offsetDelta = pos.startOffset - lastOffset;
								int offsetLength = pos.endOffset - pos.startOffset;
								if (offsetLength != lastOffsetLength)
								{
									buffer.WriteVInt(offsetDelta << 1 | 1);
									buffer.WriteVInt(offsetLength);
								}
								else
								{
									buffer.WriteVInt(offsetDelta << 1);
								}
								lastOffset = pos.startOffset;
								lastOffsetLength = offsetLength;
							}
							if (payloadLength > 0)
							{
								//HM:revisit 
								//assert storePayloads;
								buffer.WriteBytes(pos.payload.bytes, 0, pos.payload.length);
							}
						}
					}
				}
				else
				{
					if (indexOptions == FieldInfo.IndexOptions.DOCS_AND_FREQS)
					{
						int lastDocID = 0;
						for (int posIDX = 0; posIDX < pendingCount; posIDX++)
						{
							PulsingPostingsWriter.Position doc = pending[posIDX];
							int delta = doc.docID - lastDocID;
							//HM:revisit 
							//assert doc.termFreq != 0;
							if (doc.termFreq == 1)
							{
								buffer.WriteVInt((delta << 1) | 1);
							}
							else
							{
								buffer.WriteVInt(delta << 1);
								buffer.WriteVInt(doc.termFreq);
							}
							lastDocID = doc.docID;
						}
					}
					else
					{
						if (indexOptions == FieldInfo.IndexOptions.DOCS_ONLY)
						{
							int lastDocID = 0;
							for (int posIDX = 0; posIDX < pendingCount; posIDX++)
							{
								PulsingPostingsWriter.Position doc = pending[posIDX];
								buffer.WriteVInt(doc.docID - lastDocID);
								lastDocID = doc.docID;
							}
						}
					}
				}
				state.bytes = new byte[(int)buffer.GetFilePointer()];
				buffer.WriteTo(state.bytes, 0);
				buffer.Reset();
			}
			pendingCount = 0;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void EncodeTerm(long[] empty, DataOutput @out, FieldInfo fieldInfo
			, BlockTermState _state, bool absolute)
		{
			PulsingPostingsWriter.PulsingTermState state = (PulsingPostingsWriter.PulsingTermState
				)_state;
			//HM:revisit 
			//assert empty.length == 0;
			this.absolute = this.absolute || absolute;
			if (state.bytes == null)
			{
				wrappedPostingsWriter.EncodeTerm(longs, buffer, fieldInfo, state.wrappedState, this
					.absolute);
				for (int i = 0; i < longsSize; i++)
				{
					@out.WriteVLong(longs[i]);
				}
				buffer.WriteTo(@out);
				buffer.Reset();
				this.absolute = false;
			}
			else
			{
				@out.WriteVInt(state.bytes.Length);
				@out.WriteBytes(state.bytes, 0, state.bytes.Length);
				this.absolute = this.absolute || absolute;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			wrappedPostingsWriter.Close();
			if (wrappedPostingsWriter is PulsingPostingsWriter || VERSION_CURRENT < VERSION_META_ARRAY)
			{
				return;
			}
			string summaryFileName = IndexFileNames.SegmentFileName(segmentState.segmentInfo.
				name, segmentState.segmentSuffix, SUMMARY_EXTENSION);
			IndexOutput @out = null;
			try
			{
				@out = segmentState.directory.CreateOutput(summaryFileName, segmentState.context);
				CodecUtil.WriteHeader(@out, CODEC, VERSION_CURRENT);
				@out.WriteVInt(fields.Count);
				foreach (PulsingPostingsWriter.FieldMetaData field in fields)
				{
					@out.WriteVInt(field.fieldNumber);
					@out.WriteVInt(field.longsSize);
				}
				@out.Close();
			}
			finally
			{
				IOUtils.CloseWhileHandlingException(@out);
			}
		}

		// Pushes pending positions to the wrapped codec
		/// <exception cref="System.IO.IOException"></exception>
		private void Push()
		{
			// if (DEBUG) System.out.println("PW now push @ " + pendingCount + " wrapped=" + wrappedPostingsWriter);
			//HM:revisit 
			//assert pendingCount == pending.length;
			wrappedPostingsWriter.StartTerm();
			// Flush all buffered docs
			if (indexOptions.CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >=
				 0)
			{
				PulsingPostingsWriter.Position doc = null;
				foreach (PulsingPostingsWriter.Position pos in pending)
				{
					if (doc == null)
					{
						doc = pos;
						// if (DEBUG) System.out.println("PW: wrapped.startDoc docID=" + doc.docID + " tf=" + doc.termFreq);
						wrappedPostingsWriter.StartDoc(doc.docID, doc.termFreq);
					}
					else
					{
						if (doc.docID != pos.docID)
						{
							//HM:revisit 
							//assert pos.docID > doc.docID;
							// if (DEBUG) System.out.println("PW: wrapped.finishDoc");
							wrappedPostingsWriter.FinishDoc();
							doc = pos;
							// if (DEBUG) System.out.println("PW: wrapped.startDoc docID=" + doc.docID + " tf=" + doc.termFreq);
							wrappedPostingsWriter.StartDoc(doc.docID, doc.termFreq);
						}
					}
					// if (DEBUG) System.out.println("PW:   wrapped.addPos pos=" + pos.pos);
					wrappedPostingsWriter.AddPosition(pos.pos, pos.payload, pos.startOffset, pos.endOffset
						);
				}
			}
			else
			{
				//wrappedPostingsWriter.finishDoc();
				foreach (PulsingPostingsWriter.Position doc in pending)
				{
					wrappedPostingsWriter.StartDoc(doc.docID, indexOptions == FieldInfo.IndexOptions.
						DOCS_ONLY ? 0 : doc.termFreq);
				}
			}
			pendingCount = -1;
		}
	}
}
