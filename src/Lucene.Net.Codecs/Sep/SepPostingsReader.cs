/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Sep;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Codecs.Sep
{
	/// <summary>
	/// Concrete class that reads the current doc/freq/skip
	/// postings format.
	/// </summary>
	/// <remarks>
	/// Concrete class that reads the current doc/freq/skip
	/// postings format.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public class SepPostingsReader : PostingsReaderBase
	{
		internal readonly IntIndexInput freqIn;

		internal readonly IntIndexInput docIn;

		internal readonly IntIndexInput posIn;

		internal readonly IndexInput payloadIn;

		internal readonly IndexInput skipIn;

		internal int skipInterval;

		internal int maxSkipLevels;

		internal int skipMinimum;

		/// <exception cref="System.IO.IOException"></exception>
		public SepPostingsReader(Directory dir, FieldInfos fieldInfos, SegmentInfo segmentInfo
			, IOContext context, IntStreamFactory intFactory, string segmentSuffix)
		{
			// TODO: -- should we switch "hasProx" higher up?  and
			// create two separate docs readers, one that also reads
			// prox and one that doesn't?
			bool success = false;
			try
			{
				string docFileName = IndexFileNames.SegmentFileName(segmentInfo.name, segmentSuffix
					, SepPostingsWriter.DOC_EXTENSION);
				docIn = intFactory.OpenInput(dir, docFileName, context);
				skipIn = dir.OpenInput(IndexFileNames.SegmentFileName(segmentInfo.name, segmentSuffix
					, SepPostingsWriter.SKIP_EXTENSION), context);
				if (fieldInfos.HasFreq())
				{
					freqIn = intFactory.OpenInput(dir, IndexFileNames.SegmentFileName(segmentInfo.name
						, segmentSuffix, SepPostingsWriter.FREQ_EXTENSION), context);
				}
				else
				{
					freqIn = null;
				}
				if (fieldInfos.HasProx())
				{
					posIn = intFactory.OpenInput(dir, IndexFileNames.SegmentFileName(segmentInfo.name
						, segmentSuffix, SepPostingsWriter.POS_EXTENSION), context);
					payloadIn = dir.OpenInput(IndexFileNames.SegmentFileName(segmentInfo.name, segmentSuffix
						, SepPostingsWriter.PAYLOAD_EXTENSION), context);
				}
				else
				{
					posIn = null;
					payloadIn = null;
				}
				success = true;
			}
			finally
			{
				if (!success)
				{
					Close();
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Init(IndexInput termsIn)
		{
			// Make sure we are talking to the matching past writer
			CodecUtil.CheckHeader(termsIn, SepPostingsWriter.CODEC, SepPostingsWriter.VERSION_START
				, SepPostingsWriter.VERSION_START);
			skipInterval = termsIn.ReadInt();
			maxSkipLevels = termsIn.ReadInt();
			skipMinimum = termsIn.ReadInt();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			IOUtils.Close(freqIn, docIn, skipIn, posIn, payloadIn);
		}

		private sealed class SepTermState : BlockTermState
		{
			internal IntIndexInput.Index docIndex;

			internal IntIndexInput.Index posIndex;

			internal IntIndexInput.Index freqIndex;

			internal long payloadFP;

			internal long skipFP;

			// We store only the seek point to the docs file because
			// the rest of the info (freqIndex, posIndex, etc.) is
			// stored in the docs file:
			public override TermState Clone()
			{
				SepPostingsReader.SepTermState other = new SepPostingsReader.SepTermState();
				other.CopyFrom(this);
				return other;
			}

			public override void CopyFrom(TermState _other)
			{
				base.CopyFrom(_other);
				SepPostingsReader.SepTermState other = (SepPostingsReader.SepTermState)_other;
				if (docIndex == null)
				{
					docIndex = other.docIndex.Clone();
				}
				else
				{
					docIndex.CopyFrom(other.docIndex);
				}
				if (other.freqIndex != null)
				{
					if (freqIndex == null)
					{
						freqIndex = other.freqIndex.Clone();
					}
					else
					{
						freqIndex.CopyFrom(other.freqIndex);
					}
				}
				else
				{
					freqIndex = null;
				}
				if (other.posIndex != null)
				{
					if (posIndex == null)
					{
						posIndex = other.posIndex.Clone();
					}
					else
					{
						posIndex.CopyFrom(other.posIndex);
					}
				}
				else
				{
					posIndex = null;
				}
				payloadFP = other.payloadFP;
				skipFP = other.skipFP;
			}

			public override string ToString()
			{
				return base.ToString() + " docIndex=" + docIndex + " freqIndex=" + freqIndex + " posIndex="
					 + posIndex + " payloadFP=" + payloadFP + " skipFP=" + skipFP;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override BlockTermState NewTermState()
		{
			SepPostingsReader.SepTermState state = new SepPostingsReader.SepTermState();
			state.docIndex = docIn.Index();
			if (freqIn != null)
			{
				state.freqIndex = freqIn.Index();
			}
			if (posIn != null)
			{
				state.posIndex = posIn.Index();
			}
			return state;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void DecodeTerm(long[] empty, DataInput @in, FieldInfo fieldInfo, 
			BlockTermState _termState, bool absolute)
		{
			SepPostingsReader.SepTermState termState = (SepPostingsReader.SepTermState)_termState;
			termState.docIndex.Read(@in, absolute);
			if (fieldInfo.GetIndexOptions() != FieldInfo.IndexOptions.DOCS_ONLY)
			{
				termState.freqIndex.Read(@in, absolute);
				if (fieldInfo.GetIndexOptions() == FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
				{
					//System.out.println("  freqIndex=" + termState.freqIndex);
					termState.posIndex.Read(@in, absolute);
					//System.out.println("  posIndex=" + termState.posIndex);
					if (fieldInfo.HasPayloads())
					{
						if (absolute)
						{
							termState.payloadFP = @in.ReadVLong();
						}
						else
						{
							termState.payloadFP += @in.ReadVLong();
						}
					}
				}
			}
			//System.out.println("  payloadFP=" + termState.payloadFP);
			if (termState.docFreq >= skipMinimum)
			{
				//System.out.println("   readSkip @ " + in.getPosition());
				if (absolute)
				{
					termState.skipFP = @in.ReadVLong();
				}
				else
				{
					termState.skipFP += @in.ReadVLong();
				}
			}
			else
			{
				//System.out.println("  skipFP=" + termState.skipFP);
				if (absolute)
				{
					termState.skipFP = 0;
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override DocsEnum Docs(FieldInfo fieldInfo, BlockTermState _termState, Bits
			 liveDocs, DocsEnum reuse, int flags)
		{
			SepPostingsReader.SepTermState termState = (SepPostingsReader.SepTermState)_termState;
			SepPostingsReader.SepDocsEnum docsEnum;
			if (reuse == null || !(reuse is SepPostingsReader.SepDocsEnum))
			{
				docsEnum = new SepPostingsReader.SepDocsEnum(this);
			}
			else
			{
				docsEnum = (SepPostingsReader.SepDocsEnum)reuse;
				if (docsEnum.startDocIn != docIn)
				{
					// If you are using ParellelReader, and pass in a
					// reused DocsAndPositionsEnum, it could have come
					// from another reader also using sep codec
					docsEnum = new SepPostingsReader.SepDocsEnum(this);
				}
			}
			return docsEnum.Init(fieldInfo, termState, liveDocs);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override DocsAndPositionsEnum DocsAndPositions(FieldInfo fieldInfo, BlockTermState
			 _termState, Bits liveDocs, DocsAndPositionsEnum reuse, int flags)
		{
			//HM:revisit 
			//assert fieldInfo.getIndexOptions() == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
			SepPostingsReader.SepTermState termState = (SepPostingsReader.SepTermState)_termState;
			SepPostingsReader.SepDocsAndPositionsEnum postingsEnum;
			if (reuse == null || !(reuse is SepPostingsReader.SepDocsAndPositionsEnum))
			{
				postingsEnum = new SepPostingsReader.SepDocsAndPositionsEnum(this);
			}
			else
			{
				postingsEnum = (SepPostingsReader.SepDocsAndPositionsEnum)reuse;
				if (postingsEnum.startDocIn != docIn)
				{
					// If you are using ParellelReader, and pass in a
					// reused DocsAndPositionsEnum, it could have come
					// from another reader also using sep codec
					postingsEnum = new SepPostingsReader.SepDocsAndPositionsEnum(this);
				}
			}
			return postingsEnum.Init(fieldInfo, termState, liveDocs);
		}

		internal class SepDocsEnum : DocsEnum
		{
			internal int docFreq;

			internal int doc = -1;

			internal int accum;

			internal int count;

			internal int freq;

			internal long freqStart;

			private bool omitTF;

			private FieldInfo.IndexOptions indexOptions;

			private bool storePayloads;

			private Bits liveDocs;

			private readonly IntIndexInput.Reader docReader;

			private readonly IntIndexInput.Reader freqReader;

			private long skipFP;

			private readonly IntIndexInput.Index docIndex;

			private readonly IntIndexInput.Index freqIndex;

			private readonly IntIndexInput.Index posIndex;

			private readonly IntIndexInput startDocIn;

			internal bool skipped;

			internal SepSkipListReader skipper;

			/// <exception cref="System.IO.IOException"></exception>
			public SepDocsEnum(SepPostingsReader _enclosing)
			{
				this._enclosing = _enclosing;
				// TODO: -- should we do omitTF with 2 different enum classes?
				// TODO: -- should we do hasProx with 2 different enum classes?
				this.startDocIn = this._enclosing.docIn;
				this.docReader = this._enclosing.docIn.Reader();
				this.docIndex = this._enclosing.docIn.Index();
				if (this._enclosing.freqIn != null)
				{
					this.freqReader = this._enclosing.freqIn.Reader();
					this.freqIndex = this._enclosing.freqIn.Index();
				}
				else
				{
					this.freqReader = null;
					this.freqIndex = null;
				}
				if (this._enclosing.posIn != null)
				{
					this.posIndex = this._enclosing.posIn.Index();
				}
				else
				{
					// only init this so skipper can read it
					this.posIndex = null;
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal virtual SepPostingsReader.SepDocsEnum Init(FieldInfo fieldInfo, SepPostingsReader.SepTermState
				 termState, Bits liveDocs)
			{
				this.liveDocs = liveDocs;
				this.indexOptions = fieldInfo.GetIndexOptions();
				this.omitTF = this.indexOptions == FieldInfo.IndexOptions.DOCS_ONLY;
				this.storePayloads = fieldInfo.HasPayloads();
				// TODO: can't we only do this if consumer
				// skipped consuming the previous docs?
				this.docIndex.CopyFrom(termState.docIndex);
				this.docIndex.Seek(this.docReader);
				if (!this.omitTF)
				{
					this.freqIndex.CopyFrom(termState.freqIndex);
					this.freqIndex.Seek(this.freqReader);
				}
				this.docFreq = termState.docFreq;
				// NOTE: unused if docFreq < skipMinimum:
				this.skipFP = termState.skipFP;
				this.count = 0;
				this.doc = -1;
				this.accum = 0;
				this.freq = 1;
				this.skipped = false;
				return this;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int NextDoc()
			{
				while (true)
				{
					if (this.count == this.docFreq)
					{
						return this.doc = DocIdSetIterator.NO_MORE_DOCS;
					}
					this.count++;
					// Decode next doc
					//System.out.println("decode docDelta:");
					this.accum += this.docReader.Next();
					if (!this.omitTF)
					{
						//System.out.println("decode freq:");
						this.freq = this.freqReader.Next();
					}
					if (this.liveDocs == null || this.liveDocs.Get(this.accum))
					{
						break;
					}
				}
				return (this.doc = this.accum);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Freq()
			{
				return this.freq;
			}

			public override int DocID()
			{
				return this.doc;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Advance(int target)
			{
				if ((target - this._enclosing.skipInterval) >= this.doc && this.docFreq >= this._enclosing
					.skipMinimum)
				{
					// There are enough docs in the posting to have
					// skip data, and its not too close
					if (this.skipper == null)
					{
						// This DocsEnum has never done any skipping
						this.skipper = new SepSkipListReader(((IndexInput)this._enclosing.skipIn.Clone())
							, this._enclosing.freqIn, this._enclosing.docIn, this._enclosing.posIn, this._enclosing
							.maxSkipLevels, this._enclosing.skipInterval);
					}
					if (!this.skipped)
					{
						// We haven't yet skipped for this posting
						this.skipper.Init(this.skipFP, this.docIndex, this.freqIndex, this.posIndex, 0, this
							.docFreq, this.storePayloads);
						this.skipper.SetIndexOptions(this.indexOptions);
						this.skipped = true;
					}
					int newCount = this.skipper.SkipTo(target);
					if (newCount > this.count)
					{
						// Skipper did move
						if (!this.omitTF)
						{
							this.skipper.GetFreqIndex().Seek(this.freqReader);
						}
						this.skipper.GetDocIndex().Seek(this.docReader);
						this.count = newCount;
						this.doc = this.accum = this.skipper.GetDoc();
					}
				}
				do
				{
					// Now, linear scan for the rest:
					if (this.NextDoc() == DocIdSetIterator.NO_MORE_DOCS)
					{
						return DocIdSetIterator.NO_MORE_DOCS;
					}
				}
				while (target > this.doc);
				return this.doc;
			}

			public override long Cost()
			{
				return this.docFreq;
			}

			private readonly SepPostingsReader _enclosing;
		}

		internal class SepDocsAndPositionsEnum : DocsAndPositionsEnum
		{
			internal int docFreq;

			internal int doc = -1;

			internal int accum;

			internal int count;

			internal int freq;

			internal long freqStart;

			private bool storePayloads;

			private Bits liveDocs;

			private readonly IntIndexInput.Reader docReader;

			private readonly IntIndexInput.Reader freqReader;

			private readonly IntIndexInput.Reader posReader;

			private readonly IndexInput payloadIn;

			private long skipFP;

			private readonly IntIndexInput.Index docIndex;

			private readonly IntIndexInput.Index freqIndex;

			private readonly IntIndexInput.Index posIndex;

			private readonly IntIndexInput startDocIn;

			private long payloadFP;

			private int pendingPosCount;

			private int position;

			private int payloadLength;

			private long pendingPayloadBytes;

			private bool skipped;

			private SepSkipListReader skipper;

			private bool payloadPending;

			private bool posSeekPending;

			/// <exception cref="System.IO.IOException"></exception>
			public SepDocsAndPositionsEnum(SepPostingsReader _enclosing)
			{
				this._enclosing = _enclosing;
				this.startDocIn = this._enclosing.docIn;
				this.docReader = this._enclosing.docIn.Reader();
				this.docIndex = this._enclosing.docIn.Index();
				this.freqReader = this._enclosing.freqIn.Reader();
				this.freqIndex = this._enclosing.freqIn.Index();
				this.posReader = this._enclosing.posIn.Reader();
				this.posIndex = this._enclosing.posIn.Index();
				this.payloadIn = ((IndexInput)this._enclosing.payloadIn.Clone());
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal virtual SepPostingsReader.SepDocsAndPositionsEnum Init(FieldInfo fieldInfo
				, SepPostingsReader.SepTermState termState, Bits liveDocs)
			{
				this.liveDocs = liveDocs;
				this.storePayloads = fieldInfo.HasPayloads();
				//System.out.println("Sep D&P init");
				// TODO: can't we only do this if consumer
				// skipped consuming the previous docs?
				this.docIndex.CopyFrom(termState.docIndex);
				this.docIndex.Seek(this.docReader);
				//System.out.println("  docIndex=" + docIndex);
				this.freqIndex.CopyFrom(termState.freqIndex);
				this.freqIndex.Seek(this.freqReader);
				//System.out.println("  freqIndex=" + freqIndex);
				this.posIndex.CopyFrom(termState.posIndex);
				//System.out.println("  posIndex=" + posIndex);
				this.posSeekPending = true;
				this.payloadPending = false;
				this.payloadFP = termState.payloadFP;
				this.skipFP = termState.skipFP;
				//System.out.println("  skipFP=" + skipFP);
				this.docFreq = termState.docFreq;
				this.count = 0;
				this.doc = -1;
				this.accum = 0;
				this.pendingPosCount = 0;
				this.pendingPayloadBytes = 0;
				this.skipped = false;
				return this;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int NextDoc()
			{
				while (true)
				{
					if (this.count == this.docFreq)
					{
						return this.doc = DocIdSetIterator.NO_MORE_DOCS;
					}
					this.count++;
					// TODO: maybe we should do the 1-bit trick for encoding
					// freq=1 case?
					// Decode next doc
					//System.out.println("  sep d&p read doc");
					this.accum += this.docReader.Next();
					//System.out.println("  sep d&p read freq");
					this.freq = this.freqReader.Next();
					this.pendingPosCount += this.freq;
					if (this.liveDocs == null || this.liveDocs.Get(this.accum))
					{
						break;
					}
				}
				this.position = 0;
				return (this.doc = this.accum);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Freq()
			{
				return this.freq;
			}

			public override int DocID()
			{
				return this.doc;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Advance(int target)
			{
				//System.out.println("SepD&P advance target=" + target + " vs current=" + doc + " this=" + this);
				if ((target - this._enclosing.skipInterval) >= this.doc && this.docFreq >= this._enclosing
					.skipMinimum)
				{
					// There are enough docs in the posting to have
					// skip data, and its not too close
					if (this.skipper == null)
					{
						//System.out.println("  create skipper");
						// This DocsEnum has never done any skipping
						this.skipper = new SepSkipListReader(((IndexInput)this._enclosing.skipIn.Clone())
							, this._enclosing.freqIn, this._enclosing.docIn, this._enclosing.posIn, this._enclosing
							.maxSkipLevels, this._enclosing.skipInterval);
					}
					if (!this.skipped)
					{
						//System.out.println("  init skip data skipFP=" + skipFP);
						// We haven't yet skipped for this posting
						this.skipper.Init(this.skipFP, this.docIndex, this.freqIndex, this.posIndex, this
							.payloadFP, this.docFreq, this.storePayloads);
						this.skipper.SetIndexOptions(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS);
						this.skipped = true;
					}
					int newCount = this.skipper.SkipTo(target);
					//System.out.println("  skip newCount=" + newCount + " vs " + count);
					if (newCount > this.count)
					{
						// Skipper did move
						this.skipper.GetFreqIndex().Seek(this.freqReader);
						this.skipper.GetDocIndex().Seek(this.docReader);
						//System.out.println("  doc seek'd to " + skipper.getDocIndex());
						// NOTE: don't seek pos here; do it lazily
						// instead.  Eg a PhraseQuery may skip to many
						// docs before finally asking for positions...
						this.posIndex.CopyFrom(this.skipper.GetPosIndex());
						this.posSeekPending = true;
						this.count = newCount;
						this.doc = this.accum = this.skipper.GetDoc();
						//System.out.println("    moved to doc=" + doc);
						//payloadIn.seek(skipper.getPayloadPointer());
						this.payloadFP = this.skipper.GetPayloadPointer();
						this.pendingPosCount = 0;
						this.pendingPayloadBytes = 0;
						this.payloadPending = false;
						this.payloadLength = this.skipper.GetPayloadLength();
					}
				}
				do
				{
					//System.out.println("    move payloadLen=" + payloadLength);
					// Now, linear scan for the rest:
					if (this.NextDoc() == DocIdSetIterator.NO_MORE_DOCS)
					{
						//System.out.println("  advance nextDoc=END");
						return DocIdSetIterator.NO_MORE_DOCS;
					}
				}
				while (target > this.doc);
				//System.out.println("  advance nextDoc=" + doc);
				//System.out.println("  return doc=" + doc);
				return this.doc;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int NextPosition()
			{
				if (this.posSeekPending)
				{
					this.posIndex.Seek(this.posReader);
					this.payloadIn.Seek(this.payloadFP);
					this.posSeekPending = false;
				}
				// scan over any docs that were iterated without their
				// positions
				while (this.pendingPosCount > this.freq)
				{
					int code = this.posReader.Next();
					if (this.storePayloads && (code & 1) != 0)
					{
						// Payload length has changed
						this.payloadLength = this.posReader.Next();
					}
					//HM:revisit 
					//assert payloadLength >= 0;
					this.pendingPosCount--;
					this.position = 0;
					this.pendingPayloadBytes += this.payloadLength;
				}
				int code_1 = this.posReader.Next();
				if (this.storePayloads)
				{
					if ((code_1 & 1) != 0)
					{
						// Payload length has changed
						this.payloadLength = this.posReader.Next();
					}
					//HM:revisit 
					//assert payloadLength >= 0;
					this.position += (int)(((uint)code_1) >> 1);
					this.pendingPayloadBytes += this.payloadLength;
					this.payloadPending = this.payloadLength > 0;
				}
				else
				{
					this.position += code_1;
				}
				this.pendingPosCount--;
				//HM:revisit 
				//assert pendingPosCount >= 0;
				return this.position;
			}

			public override int StartOffset()
			{
				return -1;
			}

			public override int EndOffset()
			{
				return -1;
			}

			private BytesRef payload;

			/// <exception cref="System.IO.IOException"></exception>
			public override BytesRef GetPayload()
			{
				if (!this.payloadPending)
				{
					return null;
				}
				if (this.pendingPayloadBytes == 0)
				{
					return this.payload;
				}
				//HM:revisit 
				//assert pendingPayloadBytes >= payloadLength;
				if (this.pendingPayloadBytes > this.payloadLength)
				{
					this.payloadIn.Seek(this.payloadIn.FilePointer + (this.pendingPayloadBytes -
						 this.payloadLength));
				}
				if (this.payload == null)
				{
					this.payload = new BytesRef();
					this.payload.bytes = new byte[this.payloadLength];
				}
				else
				{
					if (this.payload.bytes.Length < this.payloadLength)
					{
						this.payload.Grow(this.payloadLength);
					}
				}
				this.payloadIn.ReadBytes(this.payload.bytes, 0, this.payloadLength);
				this.payload.length = this.payloadLength;
				this.pendingPayloadBytes = 0;
				return this.payload;
			}

			public override long Cost()
			{
				return this.docFreq;
			}

			private readonly SepPostingsReader _enclosing;
		}

		public override long RamBytesUsed()
		{
			return 0;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void CheckIntegrity()
		{
		}
		// TODO: remove sep layout, its fallen behind on features...
	}
}
