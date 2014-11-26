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
	/// Implements the skip list writer for the default posting list format
	/// that stores positions and payloads.
	/// </summary>
	/// <remarks>
	/// Implements the skip list writer for the default posting list format
	/// that stores positions and payloads.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	internal class SepSkipListWriter : MultiLevelSkipListWriter
	{
		private int[] lastSkipDoc;

		private int[] lastSkipPayloadLength;

		private long[] lastSkipPayloadPointer;

		private IntIndexOutput.Index[] docIndex;

		private IntIndexOutput.Index[] freqIndex;

		private IntIndexOutput.Index[] posIndex;

		private IntIndexOutput freqOutput;

		internal IntIndexOutput posOutput;

		internal IndexOutput payloadOutput;

		private int curDoc;

		private bool curStorePayloads;

		private int curPayloadLength;

		private long curPayloadPointer;

		/// <exception cref="System.IO.IOException"></exception>
		internal SepSkipListWriter(int skipInterval, int numberOfSkipLevels, int docCount
			, IntIndexOutput freqOutput, IntIndexOutput docOutput, IntIndexOutput posOutput, 
			IndexOutput payloadOutput) : base(skipInterval, numberOfSkipLevels, docCount)
		{
			// TODO: -- skip data should somehow be more local to the
			// particular stream (doc, freq, pos, payload)
			// TODO: -- private again
			// TODO: -- private again
			this.freqOutput = freqOutput;
			this.posOutput = posOutput;
			this.payloadOutput = payloadOutput;
			lastSkipDoc = new int[numberOfSkipLevels];
			lastSkipPayloadLength = new int[numberOfSkipLevels];
			// TODO: -- also cutover normal IndexOutput to use getIndex()?
			lastSkipPayloadPointer = new long[numberOfSkipLevels];
			freqIndex = new IntIndexOutput.Index[numberOfSkipLevels];
			docIndex = new IntIndexOutput.Index[numberOfSkipLevels];
			posIndex = new IntIndexOutput.Index[numberOfSkipLevels];
			for (int i = 0; i < numberOfSkipLevels; i++)
			{
				if (freqOutput != null)
				{
					freqIndex[i] = freqOutput.Index();
				}
				docIndex[i] = docOutput.Index();
				if (posOutput != null)
				{
					posIndex[i] = posOutput.Index();
				}
			}
		}

		internal FieldInfo.IndexOptions indexOptions;

		internal virtual void SetIndexOptions(FieldInfo.IndexOptions v)
		{
			indexOptions = v;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void SetPosOutput(IntIndexOutput posOutput)
		{
			this.posOutput = posOutput;
			for (int i = 0; i < numberOfSkipLevels; i++)
			{
				posIndex[i] = posOutput.Index();
			}
		}

		internal virtual void SetPayloadOutput(IndexOutput payloadOutput)
		{
			this.payloadOutput = payloadOutput;
		}

		/// <summary>Sets the values for the current skip data.</summary>
		/// <remarks>Sets the values for the current skip data.</remarks>
		internal virtual void SetSkipData(int doc, bool storePayloads, int payloadLength)
		{
			// Called @ every index interval (every 128th (by default)
			// doc)
			this.curDoc = doc;
			this.curStorePayloads = storePayloads;
			this.curPayloadLength = payloadLength;
			if (payloadOutput != null)
			{
				this.curPayloadPointer = payloadOutput.GetFilePointer();
			}
		}

		// Called @ start of new term
		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual void ResetSkip(IntIndexOutput.Index topDocIndex, IntIndexOutput.Index
			 topFreqIndex, IntIndexOutput.Index topPosIndex)
		{
			base.ResetSkip();
			Arrays.Fill(lastSkipDoc, 0);
			Arrays.Fill(lastSkipPayloadLength, -1);
			// we don't have to write the first length in the skip list
			for (int i = 0; i < numberOfSkipLevels; i++)
			{
				docIndex[i].CopyFrom(topDocIndex, true);
				if (freqOutput != null)
				{
					freqIndex[i].CopyFrom(topFreqIndex, true);
				}
				if (posOutput != null)
				{
					posIndex[i].CopyFrom(topPosIndex, true);
				}
			}
			if (payloadOutput != null)
			{
				Arrays.Fill(lastSkipPayloadPointer, payloadOutput.GetFilePointer());
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected override void WriteSkipData(int level, IndexOutput skipBuffer)
		{
			// To efficiently store payloads in the posting lists we do not store the length of
			// every payload. Instead we omit the length for a payload if the previous payload had
			// the same length.
			// However, in order to support skipping the payload length at every skip point must be known.
			// So we use the same length encoding that we use for the posting lists for the skip data as well:
			// Case 1: current field does not store payloads
			//           SkipDatum                 --> DocSkip, FreqSkip, ProxSkip
			//           DocSkip,FreqSkip,ProxSkip --> VInt
			//           DocSkip records the document number before every SkipInterval th  document in TermFreqs. 
			//           Document numbers are represented as differences from the previous value in the sequence.
			// Case 2: current field stores payloads
			//           SkipDatum                 --> DocSkip, PayloadLength?, FreqSkip,ProxSkip
			//           DocSkip,FreqSkip,ProxSkip --> VInt
			//           PayloadLength             --> VInt    
			//         In this case DocSkip/2 is the difference between
			//         the current and the previous value. If DocSkip
			//         is odd, then a PayloadLength encoded as VInt follows,
			//         if DocSkip is even, then it is assumed that the
			//         current payload length equals the length at the previous
			//         skip point
			//HM:revisit 
			//assert indexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS || !curStorePayloads;
			if (curStorePayloads)
			{
				int delta = curDoc - lastSkipDoc[level];
				if (curPayloadLength == lastSkipPayloadLength[level])
				{
					// the current payload length equals the length at the previous skip point,
					// so we don't store the length again
					skipBuffer.WriteVInt(delta << 1);
				}
				else
				{
					// the payload length is different from the previous one. We shift the DocSkip, 
					// set the lowest bit and store the current payload length as VInt.
					skipBuffer.WriteVInt(delta << 1 | 1);
					skipBuffer.WriteVInt(curPayloadLength);
					lastSkipPayloadLength[level] = curPayloadLength;
				}
			}
			else
			{
				// current field does not store payloads
				skipBuffer.WriteVInt(curDoc - lastSkipDoc[level]);
			}
			if (indexOptions != FieldInfo.IndexOptions.DOCS_ONLY)
			{
				freqIndex[level].Mark();
				freqIndex[level].Write(skipBuffer, false);
			}
			docIndex[level].Mark();
			docIndex[level].Write(skipBuffer, false);
			if (indexOptions == FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
			{
				posIndex[level].Mark();
				posIndex[level].Write(skipBuffer, false);
				if (curStorePayloads)
				{
					skipBuffer.WriteVInt((int)(curPayloadPointer - lastSkipPayloadPointer[level]));
				}
			}
			lastSkipDoc[level] = curDoc;
			lastSkipPayloadPointer[level] = curPayloadPointer;
		}
	}
}
