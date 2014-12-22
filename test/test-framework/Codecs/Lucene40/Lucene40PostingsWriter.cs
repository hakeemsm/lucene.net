/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Codecs;
using Lucene.Net.Codecs.Lucene40;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Store;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Lucene.Net.Codecs.Lucene40
{
	/// <summary>Concrete class that writes the 4.0 frq/prx postings format.</summary>
	/// <remarks>Concrete class that writes the 4.0 frq/prx postings format.</remarks>
	/// <seealso cref="Lucene40PostingsFormat">Lucene40PostingsFormat</seealso>
	/// <lucene.experimental></lucene.experimental>
	public sealed class Lucene40PostingsWriter : PostingsWriterBase
	{
		internal readonly IndexOutput freqOut;

		internal readonly IndexOutput proxOut;

		internal readonly Lucene40SkipListWriter skipListWriter;

		/// <summary>
		/// Expert: The fraction of TermDocs entries stored in skip tables,
		/// used to accelerate
		/// <see cref="Org.Apache.Lucene.Search.DocIdSetIterator.Advance(int)">Org.Apache.Lucene.Search.DocIdSetIterator.Advance(int)
		/// 	</see>
		/// .  Larger values result in
		/// smaller indexes, greater acceleration, but fewer accelerable cases, while
		/// smaller values result in bigger indexes, less acceleration and more
		/// accelerable cases. More detailed experiments would be useful here.
		/// </summary>
		internal const int DEFAULT_SKIP_INTERVAL = 16;

		internal readonly int skipInterval;

		/// <summary>Expert: minimum docFreq to write any skip data at all</summary>
		internal readonly int skipMinimum;

		/// <summary>Expert: The maximum number of skip levels.</summary>
		/// <remarks>
		/// Expert: The maximum number of skip levels. Smaller values result in
		/// slightly smaller indexes, but slower skipping in big posting lists.
		/// </remarks>
		internal readonly int maxSkipLevels = 10;

		internal readonly int totalNumDocs;

		internal FieldInfo.IndexOptions indexOptions;

		internal bool storePayloads;

		internal bool storeOffsets;

		internal long freqStart;

		internal long proxStart;

		internal FieldInfo fieldInfo;

		internal int lastPayloadLength;

		internal int lastOffsetLength;

		internal int lastPosition;

		internal int lastOffset;

		internal static readonly Lucene40PostingsWriter.StandardTermState emptyState = new 
			Lucene40PostingsWriter.StandardTermState();

		internal Lucene40PostingsWriter.StandardTermState lastState;

		/// <summary>
		/// Creates a
		/// <see cref="Lucene40PostingsWriter">Lucene40PostingsWriter</see>
		/// , with the
		/// <see cref="DEFAULT_SKIP_INTERVAL">DEFAULT_SKIP_INTERVAL</see>
		/// .
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public Lucene40PostingsWriter(SegmentWriteState state) : this(state, DEFAULT_SKIP_INTERVAL
			)
		{
		}

		/// <summary>
		/// Creates a
		/// <see cref="Lucene40PostingsWriter">Lucene40PostingsWriter</see>
		/// , with the
		/// specified
		/// <code>skipInterval</code>
		/// .
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public Lucene40PostingsWriter(SegmentWriteState state, int skipInterval) : base()
		{
			// Starts a new term
			// private String segment;
			this.skipInterval = skipInterval;
			this.skipMinimum = skipInterval;
			// this.segment = state.segmentName;
			string fileName = IndexFileNames.SegmentFileName(state.segmentInfo.name, state.segmentSuffix
				, Lucene40PostingsFormat.FREQ_EXTENSION);
			freqOut = state.directory.CreateOutput(fileName, state.context);
			bool success = false;
			IndexOutput proxOut = null;
			try
			{
				CodecUtil.WriteHeader(freqOut, Lucene40PostingsReader.FRQ_CODEC, Lucene40PostingsReader
					.VERSION_CURRENT);
				// TODO: this is a best effort, if one of these fields has no postings
				// then we make an empty prx file, same as if we are wrapped in 
				// per-field postingsformat. maybe... we shouldn't
				// bother w/ this opto?  just create empty prx file...?
				if (state.fieldInfos.HasProx())
				{
					// At least one field does not omit TF, so create the
					// prox file
					fileName = IndexFileNames.SegmentFileName(state.segmentInfo.name, state.segmentSuffix
						, Lucene40PostingsFormat.PROX_EXTENSION);
					proxOut = state.directory.CreateOutput(fileName, state.context);
					CodecUtil.WriteHeader(proxOut, Lucene40PostingsReader.PRX_CODEC, Lucene40PostingsReader
						.VERSION_CURRENT);
				}
				else
				{
					// Every field omits TF so we will write no prox file
					proxOut = null;
				}
				this.proxOut = proxOut;
				success = true;
			}
			finally
			{
				if (!success)
				{
					IOUtils.CloseWhileHandlingException(freqOut, proxOut);
				}
			}
			totalNumDocs = state.segmentInfo.GetDocCount();
			skipListWriter = new Lucene40SkipListWriter(skipInterval, maxSkipLevels, totalNumDocs
				, freqOut, proxOut);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Init(IndexOutput termsOut)
		{
			CodecUtil.WriteHeader(termsOut, Lucene40PostingsReader.TERMS_CODEC, Lucene40PostingsReader
				.VERSION_CURRENT);
			termsOut.WriteInt(skipInterval);
			// write skipInterval
			termsOut.WriteInt(maxSkipLevels);
			// write maxSkipLevels
			termsOut.WriteInt(skipMinimum);
		}

		// write skipMinimum
		public override BlockTermState NewTermState()
		{
			return new Lucene40PostingsWriter.StandardTermState();
		}

		public override void StartTerm()
		{
			freqStart = freqOut.GetFilePointer();
			//if (DEBUG) System.out.println("SPW: startTerm freqOut.fp=" + freqStart);
			if (proxOut != null)
			{
				proxStart = proxOut.GetFilePointer();
			}
			// force first payload to write its length
			lastPayloadLength = -1;
			// force first offset to write its length
			lastOffsetLength = -1;
			skipListWriter.ResetSkip();
		}

		// Currently, this instance is re-used across fields, so
		// our parent calls setField whenever the field changes
		public override int SetField(FieldInfo fieldInfo)
		{
			//System.out.println("SPW: setField");
			this.fieldInfo = fieldInfo;
			indexOptions = fieldInfo.GetIndexOptions();
			storeOffsets = indexOptions.CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
				) >= 0;
			storePayloads = fieldInfo.HasPayloads();
			lastState = emptyState;
			//System.out.println("  set init blockFreqStart=" + freqStart);
			//System.out.println("  set init blockProxStart=" + proxStart);
			return 0;
		}

		internal int lastDocID;

		internal int df;

		/// <exception cref="System.IO.IOException"></exception>
		public override void StartDoc(int docID, int termDocFreq)
		{
			// if (DEBUG) System.out.println("SPW:   startDoc seg=" + segment + " docID=" + docID + " tf=" + termDocFreq + " freqOut.fp=" + freqOut.getFilePointer());
			int delta = docID - lastDocID;
			if (docID < 0 || (df > 0 && delta <= 0))
			{
				throw new CorruptIndexException("docs out of order (" + docID + " <= " + lastDocID
					 + " ) (freqOut: " + freqOut + ")");
			}
			if ((++df % skipInterval) == 0)
			{
				skipListWriter.SetSkipData(lastDocID, storePayloads, lastPayloadLength, storeOffsets
					, lastOffsetLength);
				skipListWriter.BufferSkip(df);
			}
			 
			//assert docID < totalNumDocs: "docID=" + docID + " totalNumDocs=" + totalNumDocs;
			lastDocID = docID;
			if (indexOptions == FieldInfo.IndexOptions.DOCS_ONLY)
			{
				freqOut.WriteVInt(delta);
			}
			else
			{
				if (1 == termDocFreq)
				{
					freqOut.WriteVInt((delta << 1) | 1);
				}
				else
				{
					freqOut.WriteVInt(delta << 1);
					freqOut.WriteVInt(termDocFreq);
				}
			}
			lastPosition = 0;
			lastOffset = 0;
		}

		/// <summary>Add a new position & payload</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public override void AddPosition(int position, BytesRef payload, int startOffset, 
			int endOffset)
		{
			//if (DEBUG) System.out.println("SPW:     addPos pos=" + position + " payload=" + (payload == null ? "null" : (payload.length + " bytes")) + " proxFP=" + proxOut.getFilePointer());
			 
			//assert indexOptions.compareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0 : "invalid indexOptions: " + indexOptions;
			 
			//assert proxOut != null;
			int delta = position - lastPosition;
			 
			//assert delta >= 0: "position=" + position + " lastPosition=" + lastPosition;            // not quite right (if pos=0 is repeated twice we don't catch it)
			lastPosition = position;
			int payloadLength = 0;
			if (storePayloads)
			{
				payloadLength = payload == null ? 0 : payload.length;
				if (payloadLength != lastPayloadLength)
				{
					lastPayloadLength = payloadLength;
					proxOut.WriteVInt((delta << 1) | 1);
					proxOut.WriteVInt(payloadLength);
				}
				else
				{
					proxOut.WriteVInt(delta << 1);
				}
			}
			else
			{
				proxOut.WriteVInt(delta);
			}
			if (storeOffsets)
			{
				// don't use startOffset - lastEndOffset, because this creates lots of negative vints for synonyms,
				// and the numbers aren't that much smaller anyways.
				int offsetDelta = startOffset - lastOffset;
				int offsetLength = endOffset - startOffset;
				 
				//assert offsetDelta >= 0 && offsetLength >= 0 : "startOffset=" + startOffset + ",lastOffset=" + lastOffset + ",endOffset=" + endOffset;
				if (offsetLength != lastOffsetLength)
				{
					proxOut.WriteVInt(offsetDelta << 1 | 1);
					proxOut.WriteVInt(offsetLength);
				}
				else
				{
					proxOut.WriteVInt(offsetDelta << 1);
				}
				lastOffset = startOffset;
				lastOffsetLength = offsetLength;
			}
			if (payloadLength > 0)
			{
				proxOut.WriteBytes(payload.bytes, payload.offset, payloadLength);
			}
		}

		public override void FinishDoc()
		{
		}

		private class StandardTermState : BlockTermState
		{
			public long freqStart;

			public long proxStart;

			public long skipOffset;
		}

		/// <summary>Called when we are done adding docs to this term</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public override void FinishTerm(BlockTermState _state)
		{
			Lucene40PostingsWriter.StandardTermState state = (Lucene40PostingsWriter.StandardTermState
				)_state;
			// if (DEBUG) System.out.println("SPW: finishTerm seg=" + segment + " freqStart=" + freqStart);
			 
			//assert state.docFreq > 0;
			// TODO: wasteful we are counting this (counting # docs
			// for this term) in two places?
			 
			//assert state.docFreq == df;
			state.freqStart = freqStart;
			state.proxStart = proxStart;
			if (df >= skipMinimum)
			{
				state.skipOffset = skipListWriter.WriteSkip(freqOut) - freqStart;
			}
			else
			{
				state.skipOffset = -1;
			}
			lastDocID = 0;
			df = 0;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void EncodeTerm(long[] empty, DataOutput @out, FieldInfo fieldInfo
			, BlockTermState _state, bool absolute)
		{
			Lucene40PostingsWriter.StandardTermState state = (Lucene40PostingsWriter.StandardTermState
				)_state;
			if (absolute)
			{
				lastState = emptyState;
			}
			@out.WriteVLong(state.freqStart - lastState.freqStart);
			if (state.skipOffset != -1)
			{
				 
				//assert state.skipOffset > 0;
				@out.WriteVLong(state.skipOffset);
			}
			if (indexOptions.CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >=
				 0)
			{
				@out.WriteVLong(state.proxStart - lastState.proxStart);
			}
			lastState = state;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			try
			{
				freqOut.Close();
			}
			finally
			{
				if (proxOut != null)
				{
					proxOut.Close();
				}
			}
		}
	}
}
