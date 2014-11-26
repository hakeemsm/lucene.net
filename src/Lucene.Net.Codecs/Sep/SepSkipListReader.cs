/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Sep;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Sharpen;

namespace Lucene.Net.Codecs.Sep
{
	/// <summary>
	/// Implements the skip list reader for the default posting list format
	/// that stores positions and payloads.
	/// </summary>
	/// <remarks>
	/// Implements the skip list reader for the default posting list format
	/// that stores positions and payloads.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	internal class SepSkipListReader : MultiLevelSkipListReader
	{
		private bool currentFieldStoresPayloads;

		private IntIndexInput.Index freqIndex;

		private IntIndexInput.Index docIndex;

		private IntIndexInput.Index posIndex;

		private long payloadPointer;

		private int payloadLength;

		private readonly IntIndexInput.Index lastFreqIndex;

		private readonly IntIndexInput.Index lastDocIndex;

		internal readonly IntIndexInput.Index lastPosIndex;

		private long lastPayloadPointer;

		private int lastPayloadLength;

		/// <exception cref="System.IO.IOException"></exception>
		internal SepSkipListReader(IndexInput skipStream, IntIndexInput freqIn, IntIndexInput
			 docIn, IntIndexInput posIn, int maxSkipLevels, int skipInterval) : base(skipStream
			, maxSkipLevels, skipInterval)
		{
			// TODO: rewrite this as recursive classes?
			// TODO: -- make private again
			if (freqIn != null)
			{
				freqIndex = new IntIndexInput.Index[maxSkipLevels];
			}
			docIndex = new IntIndexInput.Index[maxSkipLevels];
			if (posIn != null)
			{
				posIndex = new IntIndexInput.Index[maxNumberOfSkipLevels];
			}
			for (int i = 0; i < maxSkipLevels; i++)
			{
				if (freqIn != null)
				{
					freqIndex[i] = freqIn.Index();
				}
				docIndex[i] = docIn.Index();
				if (posIn != null)
				{
					posIndex[i] = posIn.Index();
				}
			}
			payloadPointer = new long[maxSkipLevels];
			payloadLength = new int[maxSkipLevels];
			if (freqIn != null)
			{
				lastFreqIndex = freqIn.Index();
			}
			else
			{
				lastFreqIndex = null;
			}
			lastDocIndex = docIn.Index();
			if (posIn != null)
			{
				lastPosIndex = posIn.Index();
			}
			else
			{
				lastPosIndex = null;
			}
		}

		internal FieldInfo.IndexOptions indexOptions;

		internal virtual void SetIndexOptions(FieldInfo.IndexOptions v)
		{
			indexOptions = v;
		}

		internal virtual void Init(long skipPointer, IntIndexInput.Index docBaseIndex, IntIndexInput.Index
			 freqBaseIndex, IntIndexInput.Index posBaseIndex, long payloadBasePointer, int df
			, bool storesPayloads)
		{
			base.Init(skipPointer, df);
			this.currentFieldStoresPayloads = storesPayloads;
			lastPayloadPointer = payloadBasePointer;
			for (int i = 0; i < maxNumberOfSkipLevels; i++)
			{
				docIndex[i].CopyFrom(docBaseIndex);
				if (freqIndex != null)
				{
					freqIndex[i].CopyFrom(freqBaseIndex);
				}
				if (posBaseIndex != null)
				{
					posIndex[i].CopyFrom(posBaseIndex);
				}
			}
			Arrays.Fill(payloadPointer, payloadBasePointer);
			Arrays.Fill(payloadLength, 0);
		}

		internal virtual long GetPayloadPointer()
		{
			return lastPayloadPointer;
		}

		/// <summary>
		/// Returns the payload length of the payload stored just before
		/// the doc to which the last call of
		/// <see cref="Lucene.Net.Codecs.MultiLevelSkipListReader.SkipTo(int)">Lucene.Net.Codecs.MultiLevelSkipListReader.SkipTo(int)
		/// 	</see>
		/// 
		/// has skipped.
		/// </summary>
		internal virtual int GetPayloadLength()
		{
			return lastPayloadLength;
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected override void SeekChild(int level)
		{
			base.SeekChild(level);
			payloadPointer[level] = lastPayloadPointer;
			payloadLength[level] = lastPayloadLength;
		}

		protected override void SetLastSkipData(int level)
		{
			base.SetLastSkipData(level);
			lastPayloadPointer = payloadPointer[level];
			lastPayloadLength = payloadLength[level];
			if (freqIndex != null)
			{
				lastFreqIndex.CopyFrom(freqIndex[level]);
			}
			lastDocIndex.CopyFrom(docIndex[level]);
			if (lastPosIndex != null)
			{
				lastPosIndex.CopyFrom(posIndex[level]);
			}
			if (level > 0)
			{
				if (freqIndex != null)
				{
					freqIndex[level - 1].CopyFrom(freqIndex[level]);
				}
				docIndex[level - 1].CopyFrom(docIndex[level]);
				if (posIndex != null)
				{
					posIndex[level - 1].CopyFrom(posIndex[level]);
				}
			}
		}

		internal virtual IntIndexInput.Index GetFreqIndex()
		{
			return lastFreqIndex;
		}

		internal virtual IntIndexInput.Index GetPosIndex()
		{
			return lastPosIndex;
		}

		internal virtual IntIndexInput.Index GetDocIndex()
		{
			return lastDocIndex;
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected override int ReadSkipData(int level, IndexInput skipStream)
		{
			int delta;
			//HM:revisit 
			//assert indexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS || !currentFieldStoresPayloads;
			if (currentFieldStoresPayloads)
			{
				// the current field stores payloads.
				// if the doc delta is odd then we have
				// to read the current payload length
				// because it differs from the length of the
				// previous payload
				delta = skipStream.ReadVInt();
				if ((delta & 1) != 0)
				{
					payloadLength[level] = skipStream.ReadVInt();
				}
				delta = (int)(((uint)delta) >> 1);
			}
			else
			{
				delta = skipStream.ReadVInt();
			}
			if (indexOptions != FieldInfo.IndexOptions.DOCS_ONLY)
			{
				freqIndex[level].Read(skipStream, false);
			}
			docIndex[level].Read(skipStream, false);
			if (indexOptions == FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
			{
				posIndex[level].Read(skipStream, false);
				if (currentFieldStoresPayloads)
				{
					payloadPointer[level] += skipStream.ReadVInt();
				}
			}
			return delta;
		}
	}
}
