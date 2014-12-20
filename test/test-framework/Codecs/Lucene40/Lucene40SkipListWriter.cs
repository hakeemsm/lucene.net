/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Codecs;
using Org.Apache.Lucene.Store;
using Sharpen;

namespace Org.Apache.Lucene.Codecs.Lucene40
{
	/// <summary>
	/// Implements the skip list writer for the 4.0 posting list format
	/// that stores positions and payloads.
	/// </summary>
	/// <remarks>
	/// Implements the skip list writer for the 4.0 posting list format
	/// that stores positions and payloads.
	/// </remarks>
	/// <seealso cref="Lucene40PostingsFormat">Lucene40PostingsFormat</seealso>
	[System.ObsoleteAttribute(@"Only for reading old 4.0 segments")]
	public class Lucene40SkipListWriter : MultiLevelSkipListWriter
	{
		private int[] lastSkipDoc;

		private int[] lastSkipPayloadLength;

		private int[] lastSkipOffsetLength;

		private long[] lastSkipFreqPointer;

		private long[] lastSkipProxPointer;

		private IndexOutput freqOutput;

		private IndexOutput proxOutput;

		private int curDoc;

		private bool curStorePayloads;

		private bool curStoreOffsets;

		private int curPayloadLength;

		private int curOffsetLength;

		private long curFreqPointer;

		private long curProxPointer;

		/// <summary>Sole constructor.</summary>
		/// <remarks>Sole constructor.</remarks>
		public Lucene40SkipListWriter(int skipInterval, int numberOfSkipLevels, int docCount
			, IndexOutput freqOutput, IndexOutput proxOutput) : base(skipInterval, numberOfSkipLevels
			, docCount)
		{
			this.freqOutput = freqOutput;
			this.proxOutput = proxOutput;
			lastSkipDoc = new int[numberOfSkipLevels];
			lastSkipPayloadLength = new int[numberOfSkipLevels];
			lastSkipOffsetLength = new int[numberOfSkipLevels];
			lastSkipFreqPointer = new long[numberOfSkipLevels];
			lastSkipProxPointer = new long[numberOfSkipLevels];
		}

		/// <summary>Sets the values for the current skip data.</summary>
		/// <remarks>Sets the values for the current skip data.</remarks>
		public virtual void SetSkipData(int doc, bool storePayloads, int payloadLength, bool
			 storeOffsets, int offsetLength)
		{
			//HM:revisit 
			//assert storePayloads || payloadLength == -1;
			//HM:revisit 
			//assert storeOffsets  || offsetLength == -1;
			this.curDoc = doc;
			this.curStorePayloads = storePayloads;
			this.curPayloadLength = payloadLength;
			this.curStoreOffsets = storeOffsets;
			this.curOffsetLength = offsetLength;
			this.curFreqPointer = freqOutput.GetFilePointer();
			if (proxOutput != null)
			{
				this.curProxPointer = proxOutput.GetFilePointer();
			}
		}

		protected override void ResetSkip()
		{
			base.ResetSkip();
			Arrays.Fill(lastSkipDoc, 0);
			Arrays.Fill(lastSkipPayloadLength, -1);
			// we don't have to write the first length in the skip list
			Arrays.Fill(lastSkipOffsetLength, -1);
			// we don't have to write the first length in the skip list
			Arrays.Fill(lastSkipFreqPointer, freqOutput.GetFilePointer());
			if (proxOutput != null)
			{
				Arrays.Fill(lastSkipProxPointer, proxOutput.GetFilePointer());
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected override void WriteSkipData(int level, IndexOutput skipBuffer)
		{
			// To efficiently store payloads/offsets in the posting lists we do not store the length of
			// every payload/offset. Instead we omit the length if the previous lengths were the same
			//
			// However, in order to support skipping, the length at every skip point must be known.
			// So we use the same length encoding that we use for the posting lists for the skip data as well:
			// Case 1: current field does not store payloads/offsets
			//           SkipDatum                 --> DocSkip, FreqSkip, ProxSkip
			//           DocSkip,FreqSkip,ProxSkip --> VInt
			//           DocSkip records the document number before every SkipInterval th  document in TermFreqs. 
			//           Document numbers are represented as differences from the previous value in the sequence.
			// Case 2: current field stores payloads/offsets
			//           SkipDatum                 --> DocSkip, PayloadLength?,OffsetLength?,FreqSkip,ProxSkip
			//           DocSkip,FreqSkip,ProxSkip --> VInt
			//           PayloadLength,OffsetLength--> VInt    
			//         In this case DocSkip/2 is the difference between
			//         the current and the previous value. If DocSkip
			//         is odd, then a PayloadLength encoded as VInt follows,
			//         if DocSkip is even, then it is assumed that the
			//         current payload/offset lengths equals the lengths at the previous
			//         skip point
			int delta = curDoc - lastSkipDoc[level];
			if (curStorePayloads || curStoreOffsets)
			{
				//HM:revisit 
				//assert curStorePayloads || curPayloadLength == lastSkipPayloadLength[level];
				//HM:revisit 
				//assert curStoreOffsets  || curOffsetLength == lastSkipOffsetLength[level];
				if (curPayloadLength == lastSkipPayloadLength[level] && curOffsetLength == lastSkipOffsetLength
					[level])
				{
					// the current payload/offset lengths equals the lengths at the previous skip point,
					// so we don't store the lengths again
					skipBuffer.WriteVInt(delta << 1);
				}
				else
				{
					// the payload and/or offset length is different from the previous one. We shift the DocSkip, 
					// set the lowest bit and store the current payload and/or offset lengths as VInts.
					skipBuffer.WriteVInt(delta << 1 | 1);
					if (curStorePayloads)
					{
						skipBuffer.WriteVInt(curPayloadLength);
						lastSkipPayloadLength[level] = curPayloadLength;
					}
					if (curStoreOffsets)
					{
						skipBuffer.WriteVInt(curOffsetLength);
						lastSkipOffsetLength[level] = curOffsetLength;
					}
				}
			}
			else
			{
				// current field does not store payloads or offsets
				skipBuffer.WriteVInt(delta);
			}
			skipBuffer.WriteVInt((int)(curFreqPointer - lastSkipFreqPointer[level]));
			skipBuffer.WriteVInt((int)(curProxPointer - lastSkipProxPointer[level]));
			lastSkipDoc[level] = curDoc;
			lastSkipFreqPointer[level] = curFreqPointer;
			lastSkipProxPointer[level] = curProxPointer;
		}
	}
}
