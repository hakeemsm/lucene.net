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
	/// <summary>
	/// Concrete class that reads the current doc/freq/skip
	/// postings format
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class PulsingPostingsReader : PostingsReaderBase
	{
		internal readonly PostingsReaderBase wrappedPostingsReader;

		internal readonly SegmentReadState segmentState;

		internal int maxPositions;

		internal int version;

		internal SortedDictionary<int, int> fields;

		public PulsingPostingsReader(SegmentReadState state, PostingsReaderBase wrappedPostingsReader
			)
		{
			// TODO: -- should we switch "hasProx" higher up?  and
			// create two separate docs readers, one that also reads
			// prox and one that doesn't?
			// Fallback reader for non-pulsed terms:
			this.wrappedPostingsReader = wrappedPostingsReader;
			this.segmentState = state;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Init(IndexInput termsIn)
		{
			version = CodecUtil.CheckHeader(termsIn, PulsingPostingsWriter.CODEC, PulsingPostingsWriter
				.VERSION_START, PulsingPostingsWriter.VERSION_CURRENT);
			maxPositions = termsIn.ReadVInt();
			wrappedPostingsReader.Init(termsIn);
			if (wrappedPostingsReader is Lucene.Net.Codecs.Pulsing.PulsingPostingsReader
				 || version < PulsingPostingsWriter.VERSION_META_ARRAY)
			{
				fields = null;
			}
			else
			{
				fields = new SortedDictionary<int, int>();
				string summaryFileName = IndexFileNames.SegmentFileName(segmentState.segmentInfo.
					name, segmentState.segmentSuffix, PulsingPostingsWriter.SUMMARY_EXTENSION);
				IndexInput @in = null;
				try
				{
					@in = segmentState.directory.OpenInput(summaryFileName, segmentState.context);
					CodecUtil.CheckHeader(@in, PulsingPostingsWriter.CODEC, version, PulsingPostingsWriter
						.VERSION_CURRENT);
					int numField = @in.ReadVInt();
					for (int i = 0; i < numField; i++)
					{
						int fieldNum = @in.ReadVInt();
						int longsSize = @in.ReadVInt();
						fields.Put(fieldNum, longsSize);
					}
				}
				finally
				{
					IOUtils.CloseWhileHandlingException(@in);
				}
			}
		}

		private class PulsingTermState : BlockTermState
		{
			private bool absolute = false;

			private long[] longs;

			private byte[] postings;

			private int postingsSize;

			private BlockTermState wrappedTermState;

			// -1 if this term was not inlined
			public override TermState Clone()
			{
				PulsingPostingsReader.PulsingTermState clone;
				clone = (PulsingPostingsReader.PulsingTermState)base.Clone();
				if (postingsSize != -1)
				{
					clone.postings = new byte[postingsSize];
					System.Array.Copy(postings, 0, clone.postings, 0, postingsSize);
				}
				else
				{
					//HM:revisit 
					//assert wrappedTermState != null;
					clone.wrappedTermState = (BlockTermState)wrappedTermState.Clone();
					clone.absolute = absolute;
					if (longs != null)
					{
						clone.longs = new long[longs.Length];
						System.Array.Copy(longs, 0, clone.longs, 0, longs.Length);
					}
				}
				return clone;
			}

			public override void CopyFrom(TermState _other)
			{
				base.CopyFrom(_other);
				PulsingPostingsReader.PulsingTermState other = (PulsingPostingsReader.PulsingTermState
					)_other;
				postingsSize = other.postingsSize;
				if (other.postingsSize != -1)
				{
					if (postings == null || postings.Length < other.postingsSize)
					{
						postings = new byte[ArrayUtil.Oversize(other.postingsSize, 1)];
					}
					System.Array.Copy(other.postings, 0, postings, 0, other.postingsSize);
				}
				else
				{
					wrappedTermState.CopyFrom(other.wrappedTermState);
				}
			}

			public override string ToString()
			{
				if (postingsSize == -1)
				{
					return "PulsingTermState: not inlined: wrapped=" + wrappedTermState;
				}
				else
				{
					return "PulsingTermState: inlined size=" + postingsSize + " " + base.ToString();
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override BlockTermState NewTermState()
		{
			PulsingPostingsReader.PulsingTermState state = new PulsingPostingsReader.PulsingTermState
				();
			state.wrappedTermState = wrappedPostingsReader.NewTermState();
			return state;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void DecodeTerm(long[] empty, DataInput @in, FieldInfo fieldInfo, 
			BlockTermState _termState, bool absolute)
		{
			//System.out.println("PR nextTerm");
			PulsingPostingsReader.PulsingTermState termState = (PulsingPostingsReader.PulsingTermState
				)_termState;
			//HM:revisit 
			//assert empty.length == 0;
			termState.absolute = termState.absolute || absolute;
			// if we have positions, its total TF, otherwise its computed based on docFreq.
			long count = fieldInfo.GetIndexOptions().CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS
				) >= 0 ? termState.totalTermFreq : termState.docFreq;
			//System.out.println("  count=" + count + " threshold=" + maxPositions);
			if (count <= maxPositions)
			{
				// Inlined into terms dict -- just read the byte[] blob in,
				// but don't decode it now (we only decode when a DocsEnum
				// or D&PEnum is pulled):
				termState.postingsSize = @in.ReadVInt();
				if (termState.postings == null || termState.postings.Length < termState.postingsSize)
				{
					termState.postings = new byte[ArrayUtil.Oversize(termState.postingsSize, 1)];
				}
				// TODO: sort of silly to copy from one big byte[]
				// (the blob holding all inlined terms' blobs for
				// current term block) into another byte[] (just the
				// blob for this term)...
				@in.ReadBytes(termState.postings, 0, termState.postingsSize);
				//System.out.println("  inlined bytes=" + termState.postingsSize);
				termState.absolute = termState.absolute || absolute;
			}
			else
			{
				//System.out.println("  not inlined");
				int longsSize = fields == null ? 0 : fields.Get(fieldInfo.number);
				if (termState.longs == null)
				{
					termState.longs = new long[longsSize];
				}
				for (int i = 0; i < longsSize; i++)
				{
					termState.longs[i] = @in.ReadVLong();
				}
				termState.postingsSize = -1;
				termState.wrappedTermState.docFreq = termState.docFreq;
				termState.wrappedTermState.totalTermFreq = termState.totalTermFreq;
				wrappedPostingsReader.DecodeTerm(termState.longs, @in, fieldInfo, termState.wrappedTermState
					, termState.absolute);
				termState.absolute = false;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override DocsEnum Docs(FieldInfo field, BlockTermState _termState, Bits liveDocs
			, DocsEnum reuse, int flags)
		{
			PulsingPostingsReader.PulsingTermState termState = (PulsingPostingsReader.PulsingTermState
				)_termState;
			if (termState.postingsSize != -1)
			{
				PulsingPostingsReader.PulsingDocsEnum postings;
				if (reuse is PulsingPostingsReader.PulsingDocsEnum)
				{
					postings = (PulsingPostingsReader.PulsingDocsEnum)reuse;
					if (!postings.CanReuse(field))
					{
						postings = new PulsingPostingsReader.PulsingDocsEnum(field);
					}
				}
				else
				{
					// the 'reuse' is actually the wrapped enum
					PulsingPostingsReader.PulsingDocsEnum previous = (PulsingPostingsReader.PulsingDocsEnum
						)GetOther(reuse);
					if (previous != null && previous.CanReuse(field))
					{
						postings = previous;
					}
					else
					{
						postings = new PulsingPostingsReader.PulsingDocsEnum(field);
					}
				}
				if (reuse != postings)
				{
					SetOther(postings, reuse);
				}
				// postings.other = reuse
				return postings.Reset(liveDocs, termState);
			}
			else
			{
				if (reuse is PulsingPostingsReader.PulsingDocsEnum)
				{
					DocsEnum wrapped = wrappedPostingsReader.Docs(field, termState.wrappedTermState, 
						liveDocs, GetOther(reuse), flags);
					SetOther(wrapped, reuse);
					// wrapped.other = reuse
					return wrapped;
				}
				else
				{
					return wrappedPostingsReader.Docs(field, termState.wrappedTermState, liveDocs, reuse
						, flags);
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override DocsAndPositionsEnum DocsAndPositions(FieldInfo field, BlockTermState
			 _termState, Bits liveDocs, DocsAndPositionsEnum reuse, int flags)
		{
			PulsingPostingsReader.PulsingTermState termState = (PulsingPostingsReader.PulsingTermState
				)_termState;
			if (termState.postingsSize != -1)
			{
				PulsingPostingsReader.PulsingDocsAndPositionsEnum postings;
				if (reuse is PulsingPostingsReader.PulsingDocsAndPositionsEnum)
				{
					postings = (PulsingPostingsReader.PulsingDocsAndPositionsEnum)reuse;
					if (!postings.CanReuse(field))
					{
						postings = new PulsingPostingsReader.PulsingDocsAndPositionsEnum(field);
					}
				}
				else
				{
					// the 'reuse' is actually the wrapped enum
					PulsingPostingsReader.PulsingDocsAndPositionsEnum previous = (PulsingPostingsReader.PulsingDocsAndPositionsEnum
						)GetOther(reuse);
					if (previous != null && previous.CanReuse(field))
					{
						postings = previous;
					}
					else
					{
						postings = new PulsingPostingsReader.PulsingDocsAndPositionsEnum(field);
					}
				}
				if (reuse != postings)
				{
					SetOther(postings, reuse);
				}
				// postings.other = reuse 
				return postings.Reset(liveDocs, termState);
			}
			else
			{
				if (reuse is PulsingPostingsReader.PulsingDocsAndPositionsEnum)
				{
					DocsAndPositionsEnum wrapped = wrappedPostingsReader.DocsAndPositions(field, termState
						.wrappedTermState, liveDocs, (DocsAndPositionsEnum)GetOther(reuse), flags);
					SetOther(wrapped, reuse);
					// wrapped.other = reuse
					return wrapped;
				}
				else
				{
					return wrappedPostingsReader.DocsAndPositions(field, termState.wrappedTermState, 
						liveDocs, reuse, flags);
				}
			}
		}

		private class PulsingDocsEnum : DocsEnum
		{
			private byte[] postingsBytes;

			private readonly ByteArrayDataInput postings = new ByteArrayDataInput();

			private readonly FieldInfo.IndexOptions indexOptions;

			private readonly bool storePayloads;

			private readonly bool storeOffsets;

			private Bits liveDocs;

			private int docID = -1;

			private int accum;

			private int freq;

			private int payloadLength;

			private int cost;

			public PulsingDocsEnum(FieldInfo fieldInfo)
			{
				indexOptions = fieldInfo.GetIndexOptions();
				storePayloads = fieldInfo.HasPayloads();
				storeOffsets = indexOptions.CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
					) >= 0;
			}

			public virtual PulsingPostingsReader.PulsingDocsEnum Reset(Bits liveDocs, PulsingPostingsReader.PulsingTermState
				 termState)
			{
				//System.out.println("PR docsEnum termState=" + termState + " docFreq=" + termState.docFreq);
				//HM:revisit 
				//assert termState.postingsSize != -1;
				// Must make a copy of termState's byte[] so that if
				// app does TermsEnum.next(), this DocsEnum is not affected
				if (postingsBytes == null)
				{
					postingsBytes = new byte[termState.postingsSize];
				}
				else
				{
					if (postingsBytes.Length < termState.postingsSize)
					{
						postingsBytes = ArrayUtil.Grow(postingsBytes, termState.postingsSize);
					}
				}
				System.Array.Copy(termState.postings, 0, postingsBytes, 0, termState.postingsSize
					);
				postings.Reset(postingsBytes, 0, termState.postingsSize);
				docID = -1;
				accum = 0;
				freq = 1;
				cost = termState.docFreq;
				payloadLength = 0;
				this.liveDocs = liveDocs;
				return this;
			}

			internal virtual bool CanReuse(FieldInfo fieldInfo)
			{
				return indexOptions == fieldInfo.GetIndexOptions() && storePayloads == fieldInfo.
					HasPayloads();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int NextDoc()
			{
				//System.out.println("PR nextDoc this= "+ this);
				while (true)
				{
					if (postings.Eof())
					{
						//System.out.println("PR   END");
						return docID = NO_MORE_DOCS;
					}
					int code = postings.ReadVInt();
					//System.out.println("  read code=" + code);
					if (indexOptions == FieldInfo.IndexOptions.DOCS_ONLY)
					{
						accum += code;
					}
					else
					{
						accum += (int)(((uint)code) >> 1);
						// shift off low bit
						if ((code & 1) != 0)
						{
							// if low bit is set
							freq = 1;
						}
						else
						{
							// freq is one
							freq = postings.ReadVInt();
						}
						// else read freq
						if (indexOptions.CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >=
							 0)
						{
							// Skip positions
							if (storePayloads)
							{
								for (int pos = 0; pos < freq; pos++)
								{
									int posCode = postings.ReadVInt();
									if ((posCode & 1) != 0)
									{
										payloadLength = postings.ReadVInt();
									}
									if (storeOffsets && (postings.ReadVInt() & 1) != 0)
									{
										// new offset length
										postings.ReadVInt();
									}
									if (payloadLength != 0)
									{
										postings.SkipBytes(payloadLength);
									}
								}
							}
							else
							{
								for (int pos = 0; pos < freq; pos++)
								{
									// TODO: skipVInt
									postings.ReadVInt();
									if (storeOffsets && (postings.ReadVInt() & 1) != 0)
									{
										// new offset length
										postings.ReadVInt();
									}
								}
							}
						}
					}
					if (liveDocs == null || liveDocs.Get(accum))
					{
						return (docID = accum);
					}
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Freq()
			{
				return freq;
			}

			public override int DocID()
			{
				return docID;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Advance(int target)
			{
				return docID = SlowAdvance(target);
			}

			public override long Cost()
			{
				return cost;
			}
		}

		private class PulsingDocsAndPositionsEnum : DocsAndPositionsEnum
		{
			private byte[] postingsBytes;

			private readonly ByteArrayDataInput postings = new ByteArrayDataInput();

			private readonly bool storePayloads;

			private readonly bool storeOffsets;

			private readonly FieldInfo.IndexOptions indexOptions;

			private Bits liveDocs;

			private int docID = -1;

			private int accum;

			private int freq;

			private int posPending;

			private int position;

			private int payloadLength;

			private BytesRef payload;

			private int startOffset;

			private int offsetLength;

			private bool payloadRetrieved;

			private int cost;

			public PulsingDocsAndPositionsEnum(FieldInfo fieldInfo)
			{
				// note: we could actually reuse across different options, if we passed this to reset()
				// and re-init'ed storeOffsets accordingly (made it non-final)
				indexOptions = fieldInfo.GetIndexOptions();
				storePayloads = fieldInfo.HasPayloads();
				storeOffsets = indexOptions.CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
					) >= 0;
			}

			internal virtual bool CanReuse(FieldInfo fieldInfo)
			{
				return indexOptions == fieldInfo.GetIndexOptions() && storePayloads == fieldInfo.
					HasPayloads();
			}

			public virtual PulsingPostingsReader.PulsingDocsAndPositionsEnum Reset(Bits liveDocs
				, PulsingPostingsReader.PulsingTermState termState)
			{
				//HM:revisit 
				//assert termState.postingsSize != -1;
				if (postingsBytes == null)
				{
					postingsBytes = new byte[termState.postingsSize];
				}
				else
				{
					if (postingsBytes.Length < termState.postingsSize)
					{
						postingsBytes = ArrayUtil.Grow(postingsBytes, termState.postingsSize);
					}
				}
				System.Array.Copy(termState.postings, 0, postingsBytes, 0, termState.postingsSize
					);
				postings.Reset(postingsBytes, 0, termState.postingsSize);
				this.liveDocs = liveDocs;
				payloadLength = 0;
				posPending = 0;
				docID = -1;
				accum = 0;
				cost = termState.docFreq;
				startOffset = storeOffsets ? 0 : -1;
				// always return -1 if no offsets are stored
				offsetLength = 0;
				//System.out.println("PR d&p reset storesPayloads=" + storePayloads + " bytes=" + bytes.length + " this=" + this);
				return this;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int NextDoc()
			{
				//System.out.println("PR d&p nextDoc this=" + this);
				while (true)
				{
					//System.out.println("  cycle skip posPending=" + posPending);
					SkipPositions();
					if (postings.Eof())
					{
						//System.out.println("PR   END");
						return docID = NO_MORE_DOCS;
					}
					int code = postings.ReadVInt();
					accum += (int)(((uint)code) >> 1);
					// shift off low bit
					if ((code & 1) != 0)
					{
						// if low bit is set
						freq = 1;
					}
					else
					{
						// freq is one
						freq = postings.ReadVInt();
					}
					// else read freq
					posPending = freq;
					startOffset = storeOffsets ? 0 : -1;
					// always return -1 if no offsets are stored
					if (liveDocs == null || liveDocs.Get(accum))
					{
						//System.out.println("  return docID=" + docID + " freq=" + freq);
						position = 0;
						return (docID = accum);
					}
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Freq()
			{
				return freq;
			}

			public override int DocID()
			{
				return docID;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Advance(int target)
			{
				return docID = SlowAdvance(target);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int NextPosition()
			{
				//System.out.println("PR d&p nextPosition posPending=" + posPending + " vs freq=" + freq);
				//HM:revisit 
				//assert posPending > 0;
				posPending--;
				if (storePayloads)
				{
					if (!payloadRetrieved)
					{
						//System.out.println("PR     skip payload=" + payloadLength);
						postings.SkipBytes(payloadLength);
					}
					int code = postings.ReadVInt();
					//System.out.println("PR     code=" + code);
					if ((code & 1) != 0)
					{
						payloadLength = postings.ReadVInt();
					}
					//System.out.println("PR     new payload len=" + payloadLength);
					position += (int)(((uint)code) >> 1);
					payloadRetrieved = false;
				}
				else
				{
					position += postings.ReadVInt();
				}
				if (storeOffsets)
				{
					int offsetCode = postings.ReadVInt();
					if ((offsetCode & 1) != 0)
					{
						// new offset length
						offsetLength = postings.ReadVInt();
					}
					startOffset += (int)(((uint)offsetCode) >> 1);
				}
				//System.out.println("PR d&p nextPos return pos=" + position + " this=" + this);
				return position;
			}

			public override int StartOffset()
			{
				return startOffset;
			}

			public override int EndOffset()
			{
				return startOffset + offsetLength;
			}

			/// <exception cref="System.IO.IOException"></exception>
			private void SkipPositions()
			{
				while (posPending != 0)
				{
					NextPosition();
				}
				if (storePayloads && !payloadRetrieved)
				{
					//System.out.println("  skip payload len=" + payloadLength);
					postings.SkipBytes(payloadLength);
					payloadRetrieved = true;
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override BytesRef GetPayload()
			{
				//System.out.println("PR  getPayload payloadLength=" + payloadLength + " this=" + this);
				if (payloadRetrieved)
				{
					return payload;
				}
				else
				{
					if (storePayloads && payloadLength > 0)
					{
						payloadRetrieved = true;
						if (payload == null)
						{
							payload = new BytesRef(payloadLength);
						}
						else
						{
							payload.Grow(payloadLength);
						}
						postings.ReadBytes(payload.bytes, 0, payloadLength);
						payload.length = payloadLength;
						return payload;
					}
					else
					{
						return null;
					}
				}
			}

			public override long Cost()
			{
				return cost;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			wrappedPostingsReader.Close();
		}

		/// <summary>for a docsenum, gets the 'other' reused enum.</summary>
		/// <remarks>
		/// for a docsenum, gets the 'other' reused enum.
		/// Example: Pulsing(Standard).
		/// when doing a term range query you are switching back and forth
		/// between Pulsing and Standard
		/// The way the reuse works is that Pulsing.other = Standard and
		/// Standard.other = Pulsing.
		/// </remarks>
		private DocsEnum GetOther(DocsEnum de)
		{
			if (de == null)
			{
				return null;
			}
			else
			{
				AttributeSource atts = de.Attributes();
				return atts.AddAttribute<PulsingPostingsReader.PulsingEnumAttribute>().Enums().Get
					(this);
			}
		}

		/// <summary>for a docsenum, sets the 'other' reused enum.</summary>
		/// <remarks>
		/// for a docsenum, sets the 'other' reused enum.
		/// see getOther for an example.
		/// </remarks>
		private DocsEnum SetOther(DocsEnum de, DocsEnum other)
		{
			AttributeSource atts = de.Attributes();
			return atts.AddAttribute<PulsingPostingsReader.PulsingEnumAttribute>().Enums().Put
				(this, other);
		}

		/// <summary>
		/// A per-docsenum attribute that stores additional reuse information
		/// so that pulsing enums can keep a reference to their wrapped enums,
		/// and vice versa.
		/// </summary>
		/// <remarks>
		/// A per-docsenum attribute that stores additional reuse information
		/// so that pulsing enums can keep a reference to their wrapped enums,
		/// and vice versa. this way we can always reuse.
		/// </remarks>
		/// <lucene.internal></lucene.internal>
		public interface PulsingEnumAttribute : Attribute
		{
			IDictionary<PulsingPostingsReader, DocsEnum> Enums();
		}

		/// <summary>
		/// Implementation of
		/// <see cref="PulsingEnumAttribute">PulsingEnumAttribute</see>
		/// for reuse of
		/// wrapped postings readers underneath pulsing.
		/// </summary>
		/// <lucene.internal></lucene.internal>
		public sealed class PulsingEnumAttributeImpl : AttributeImpl, PulsingPostingsReader.PulsingEnumAttribute
		{
			private readonly IDictionary<PulsingPostingsReader, DocsEnum> enums = new IdentityHashMap
				<PulsingPostingsReader, DocsEnum>();

			// we could store 'other', but what if someone 'chained' multiple postings readers,
			// this could cause problems?
			// TODO: we should consider nuking this map and just making it so if you do this,
			// you don't reuse? and maybe pulsingPostingsReader should throw an exc if it wraps
			// another pulsing, because this is just stupid and wasteful. 
			// we still have to be careful in case someone does Pulsing(Stomping(Pulsing(...
			public IDictionary<PulsingPostingsReader, DocsEnum> Enums()
			{
				return enums;
			}

			public override void Clear()
			{
			}

			// our state is per-docsenum, so this makes no sense.
			// its best not to clear, in case a wrapped enum has a per-doc attribute or something
			// and is calling clearAttributes(), so they don't nuke the reuse information!
			public override void CopyTo(AttributeImpl target)
			{
			}
			// this makes no sense for us, because our state is per-docsenum.
			// we don't want to copy any stuff over to another docsenum ever!
		}

		public override long RamBytesUsed()
		{
			return ((wrappedPostingsReader != null) ? wrappedPostingsReader.RamBytesUsed() : 
				0);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void CheckIntegrity()
		{
			wrappedPostingsReader.CheckIntegrity();
		}
	}
}
