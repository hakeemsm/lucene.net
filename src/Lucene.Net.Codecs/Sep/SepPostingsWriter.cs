/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Sep;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Codecs.Sep
{
	/// <summary>
	/// Writes frq to .frq, docs to .doc, pos to .pos, payloads
	/// to .pyl, skip data to .skp
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public sealed class SepPostingsWriter : PostingsWriterBase
	{
		internal static readonly string CODEC = "SepPostingsWriter";

		internal static readonly string DOC_EXTENSION = "doc";

		internal static readonly string SKIP_EXTENSION = "skp";

		internal static readonly string FREQ_EXTENSION = "frq";

		internal static readonly string POS_EXTENSION = "pos";

		internal static readonly string PAYLOAD_EXTENSION = "pyl";

		internal const int VERSION_START = 0;

		internal const int VERSION_CURRENT = VERSION_START;

		internal IntIndexOutput freqOut;

		internal IntIndexOutput.Index freqIndex;

		internal IntIndexOutput posOut;

		internal IntIndexOutput.Index posIndex;

		internal IntIndexOutput docOut;

		internal IntIndexOutput.Index docIndex;

		internal IndexOutput payloadOut;

		internal IndexOutput skipOut;

		internal readonly SepSkipListWriter skipListWriter;

		/// <summary>
		/// Expert: The fraction of TermDocs entries stored in skip tables,
		/// used to accelerate
		/// <see cref="Lucene.Net.Search.DocIdSetIterator.Advance(int)">Lucene.Net.Search.DocIdSetIterator.Advance(int)
		/// 	</see>
		/// .  Larger values result in
		/// smaller indexes, greater acceleration, but fewer accelerable cases, while
		/// smaller values result in bigger indexes, less acceleration and more
		/// accelerable cases. More detailed experiments would be useful here.
		/// </summary>
		internal readonly int skipInterval;

		internal const int DEFAULT_SKIP_INTERVAL = 16;

		/// <summary>Expert: minimum docFreq to write any skip data at all</summary>
		internal readonly int skipMinimum;

		/// <summary>Expert: The maximum number of skip levels.</summary>
		/// <remarks>
		/// Expert: The maximum number of skip levels. Smaller values result in
		/// slightly smaller indexes, but slower skipping in big posting lists.
		/// </remarks>
		internal readonly int maxSkipLevels = 10;

		internal readonly int totalNumDocs;

		internal bool storePayloads;

		internal FieldInfo.IndexOptions indexOptions;

		internal FieldInfo fieldInfo;

		internal int lastPayloadLength;

		internal int lastPosition;

		internal long payloadStart;

		internal int lastDocID;

		internal int df;

		internal SepPostingsWriter.SepTermState lastState;

		internal long lastPayloadFP;

		internal long lastSkipFP;

		/// <exception cref="System.IO.IOException"></exception>
		public SepPostingsWriter(SegmentWriteState state, IntStreamFactory factory) : this
			(state, factory, DEFAULT_SKIP_INTERVAL)
		{
		}

		/// <exception cref="System.IO.IOException"></exception>
		public SepPostingsWriter(SegmentWriteState state, IntStreamFactory factory, int skipInterval
			)
		{
			// Increment version to change it:
			freqOut = null;
			freqIndex = null;
			posOut = null;
			posIndex = null;
			payloadOut = null;
			bool success = false;
			try
			{
				this.skipInterval = skipInterval;
				this.skipMinimum = skipInterval;
				string docFileName = IndexFileNames.SegmentFileName(state.segmentInfo.name, state
					.segmentSuffix, DOC_EXTENSION);
				docOut = factory.CreateOutput(state.directory, docFileName, state.context);
				docIndex = docOut.Index();
				if (state.fieldInfos.HasFreq())
				{
					string frqFileName = IndexFileNames.SegmentFileName(state.segmentInfo.name, state
						.segmentSuffix, FREQ_EXTENSION);
					freqOut = factory.CreateOutput(state.directory, frqFileName, state.context);
					freqIndex = freqOut.Index();
				}
				if (state.fieldInfos.HasProx())
				{
					string posFileName = IndexFileNames.SegmentFileName(state.segmentInfo.name, state
						.segmentSuffix, POS_EXTENSION);
					posOut = factory.CreateOutput(state.directory, posFileName, state.context);
					posIndex = posOut.Index();
					// TODO: -- only if at least one field stores payloads?
					string payloadFileName = IndexFileNames.SegmentFileName(state.segmentInfo.name, state
						.segmentSuffix, PAYLOAD_EXTENSION);
					payloadOut = state.directory.CreateOutput(payloadFileName, state.context);
				}
				string skipFileName = IndexFileNames.SegmentFileName(state.segmentInfo.name, state
					.segmentSuffix, SKIP_EXTENSION);
				skipOut = state.directory.CreateOutput(skipFileName, state.context);
				totalNumDocs = state.segmentInfo.GetDocCount();
				skipListWriter = new SepSkipListWriter(skipInterval, maxSkipLevels, totalNumDocs, 
					freqOut, docOut, posOut, payloadOut);
				success = true;
			}
			finally
			{
				if (!success)
				{
					IOUtils.CloseWhileHandlingException(docOut, skipOut, freqOut, posOut, payloadOut);
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Init(IndexOutput termsOut)
		{
			CodecUtil.WriteHeader(termsOut, CODEC, VERSION_CURRENT);
			// TODO: -- just ask skipper to "start" here
			termsOut.WriteInt(skipInterval);
			// write skipInterval
			termsOut.WriteInt(maxSkipLevels);
			// write maxSkipLevels
			termsOut.WriteInt(skipMinimum);
		}

		// write skipMinimum
		public override BlockTermState NewTermState()
		{
			return new SepPostingsWriter.SepTermState();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void StartTerm()
		{
			docIndex.Mark();
			//System.out.println("SEPW: startTerm docIndex=" + docIndex);
			if (indexOptions != FieldInfo.IndexOptions.DOCS_ONLY)
			{
				freqIndex.Mark();
			}
			if (indexOptions == FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
			{
				posIndex.Mark();
				payloadStart = payloadOut.GetFilePointer();
				lastPayloadLength = -1;
			}
			skipListWriter.ResetSkip(docIndex, freqIndex, posIndex);
		}

		// Currently, this instance is re-used across fields, so
		// our parent calls setField whenever the field changes
		public override int SetField(FieldInfo fieldInfo)
		{
			this.fieldInfo = fieldInfo;
			this.indexOptions = fieldInfo.GetIndexOptions();
			if (indexOptions.CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
				) >= 0)
			{
				throw new NotSupportedException("this codec cannot index offsets");
			}
			skipListWriter.SetIndexOptions(indexOptions);
			storePayloads = indexOptions == FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS
				 && fieldInfo.HasPayloads();
			lastPayloadFP = 0;
			lastSkipFP = 0;
			lastState = SetEmptyState();
			return 0;
		}

		private SepPostingsWriter.SepTermState SetEmptyState()
		{
			SepPostingsWriter.SepTermState emptyState = new SepPostingsWriter.SepTermState();
			emptyState.docIndex = docOut.Index();
			if (indexOptions != FieldInfo.IndexOptions.DOCS_ONLY)
			{
				emptyState.freqIndex = freqOut.Index();
				if (indexOptions == FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
				{
					emptyState.posIndex = posOut.Index();
				}
			}
			emptyState.payloadFP = 0;
			emptyState.skipFP = 0;
			return emptyState;
		}

		/// <summary>Adds a new doc in this term.</summary>
		/// <remarks>
		/// Adds a new doc in this term.  If this returns null
		/// then we just skip consuming positions/payloads.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public override void StartDoc(int docID, int termDocFreq)
		{
			int delta = docID - lastDocID;
			//System.out.println("SEPW: startDoc: write doc=" + docID + " delta=" + delta + " out.fp=" + docOut);
			if (docID < 0 || (df > 0 && delta <= 0))
			{
				throw new CorruptIndexException("docs out of order (" + docID + " <= " + lastDocID
					 + " ) (docOut: " + docOut + ")");
			}
			if ((++df % skipInterval) == 0)
			{
				// TODO: -- awkward we have to make these two
				// separate calls to skipper
				//System.out.println("    buffer skip lastDocID=" + lastDocID);
				skipListWriter.SetSkipData(lastDocID, storePayloads, lastPayloadLength);
				skipListWriter.BufferSkip(df);
			}
			lastDocID = docID;
			docOut.Write(delta);
			if (indexOptions != FieldInfo.IndexOptions.DOCS_ONLY)
			{
				//System.out.println("    sepw startDoc: write freq=" + termDocFreq);
				freqOut.Write(termDocFreq);
			}
		}

		/// <summary>Add a new position & payload</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public override void AddPosition(int position, BytesRef payload, int startOffset, 
			int endOffset)
		{
			//HM:revisit 
			//assert indexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
			int delta = position - lastPosition;
			//HM:revisit 
			//assert delta >= 0: "position=" + position + " lastPosition=" + lastPosition;            // not quite right (if pos=0 is repeated twice we don't catch it)
			lastPosition = position;
			if (storePayloads)
			{
				int payloadLength = payload == null ? 0 : payload.length;
				if (payloadLength != lastPayloadLength)
				{
					lastPayloadLength = payloadLength;
					// TODO: explore whether we get better compression
					// by not storing payloadLength into prox stream?
					posOut.Write((delta << 1) | 1);
					posOut.Write(payloadLength);
				}
				else
				{
					posOut.Write(delta << 1);
				}
				if (payloadLength > 0)
				{
					payloadOut.WriteBytes(payload.bytes, payload.offset, payloadLength);
				}
			}
			else
			{
				posOut.Write(delta);
			}
			lastPosition = position;
		}

		/// <summary>Called when we are done adding positions & payloads</summary>
		public override void FinishDoc()
		{
			lastPosition = 0;
		}

		private class SepTermState : BlockTermState
		{
			public IntIndexOutput.Index docIndex;

			public IntIndexOutput.Index freqIndex;

			public IntIndexOutput.Index posIndex;

			public long payloadFP;

			public long skipFP;
		}

		/// <summary>Called when we are done adding docs to this term</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public override void FinishTerm(BlockTermState _state)
		{
			SepPostingsWriter.SepTermState state = (SepPostingsWriter.SepTermState)_state;
			// TODO: -- wasteful we are counting this in two places?
			//HM:revisit 
			//assert state.docFreq > 0;
			//HM:revisit 
			//assert state.docFreq == df;
			state.docIndex = docOut.Index();
			state.docIndex.CopyFrom(docIndex, false);
			if (indexOptions != FieldInfo.IndexOptions.DOCS_ONLY)
			{
				state.freqIndex = freqOut.Index();
				state.freqIndex.CopyFrom(freqIndex, false);
				if (indexOptions == FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
				{
					state.posIndex = posOut.Index();
					state.posIndex.CopyFrom(posIndex, false);
				}
				else
				{
					state.posIndex = null;
				}
			}
			else
			{
				state.freqIndex = null;
				state.posIndex = null;
			}
			if (df >= skipMinimum)
			{
				state.skipFP = skipOut.GetFilePointer();
				//System.out.println("  skipFP=" + skipFP);
				skipListWriter.WriteSkip(skipOut);
			}
			else
			{
				//System.out.println("    numBytes=" + (skipOut.getFilePointer()-skipFP));
				state.skipFP = -1;
			}
			state.payloadFP = payloadStart;
			lastDocID = 0;
			df = 0;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void EncodeTerm(long[] longs, DataOutput @out, FieldInfo fieldInfo
			, BlockTermState _state, bool absolute)
		{
			SepPostingsWriter.SepTermState state = (SepPostingsWriter.SepTermState)_state;
			if (absolute)
			{
				lastSkipFP = 0;
				lastPayloadFP = 0;
				lastState = state;
			}
			lastState.docIndex.CopyFrom(state.docIndex, false);
			lastState.docIndex.Write(@out, absolute);
			if (indexOptions != FieldInfo.IndexOptions.DOCS_ONLY)
			{
				lastState.freqIndex.CopyFrom(state.freqIndex, false);
				lastState.freqIndex.Write(@out, absolute);
				if (indexOptions == FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
				{
					lastState.posIndex.CopyFrom(state.posIndex, false);
					lastState.posIndex.Write(@out, absolute);
					if (storePayloads)
					{
						if (absolute)
						{
							@out.WriteVLong(state.payloadFP);
						}
						else
						{
							@out.WriteVLong(state.payloadFP - lastPayloadFP);
						}
						lastPayloadFP = state.payloadFP;
					}
				}
			}
			if (state.skipFP != -1)
			{
				if (absolute)
				{
					@out.WriteVLong(state.skipFP);
				}
				else
				{
					@out.WriteVLong(state.skipFP - lastSkipFP);
				}
				lastSkipFP = state.skipFP;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			IOUtils.Close(docOut, skipOut, freqOut, posOut, payloadOut);
		}
	}
}
