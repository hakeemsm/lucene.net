using Lucene.Net.Store;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Packed
{
	public sealed class BlockPackedReader : LongValues
    {
        private readonly int blockShift, blockMask;
        private readonly long valueCount;
        private readonly long[] minValues;
        private readonly PackedInts.IReader[] subReaders;

        public BlockPackedReader(IndexInput input, int packedIntsVersion, int blockSize, long valueCount, bool direct)
        {
            this.valueCount = valueCount;
			blockShift = PackedInts.CheckBlockSize(blockSize, AbstractBlockPackedWriter.MIN_BLOCK_SIZE
				, AbstractBlockPackedWriter.MAX_BLOCK_SIZE);
            blockMask = blockSize - 1;
			int numBlocks = PackedInts.NumBlocks(valueCount, blockSize);
            long[] minValues = null;
            subReaders = new PackedInts.Reader[numBlocks];
            for (int i = 0; i < numBlocks; ++i)
            {
                int token = input.ReadByte() & 0xFF;
                int bitsPerValue = Number.URShift(token, BlockPackedWriter.BPV_SHIFT);
                if (bitsPerValue > 64)
                {
                    throw new System.IO.IOException("Corrupted");
                }
				if ((token & AbstractBlockPackedWriter.MIN_VALUE_EQUALS_0) == 0)
                {
                    if (minValues == null)
                    {
                        minValues = new long[numBlocks];
                    }
                    minValues[i] = BlockPackedReaderIterator.ZigZagDecode(1L + BlockPackedReaderIterator.ReadVLong(input));
                }
                if (bitsPerValue == 0)
                {
                    subReaders[i] = new PackedInts.NullReader(blockSize);
                }
                else
                {
                    int size = (int)Math.Min(blockSize, valueCount - (long)i * blockSize);
                    if (direct)
                    {
                        long pointer = input.FilePointer;
                        subReaders[i] = PackedInts.GetDirectReaderNoHeader(input, PackedInts.Format.PACKED, packedIntsVersion, size, bitsPerValue);
                        input.Seek(pointer + PackedInts.Format.PACKED.ByteCount(packedIntsVersion, size, bitsPerValue));
                    }
                    else
                    {
                        subReaders[i] = PackedInts.GetReaderNoHeader(input, PackedInts.Format.PACKED, packedIntsVersion, size, bitsPerValue);
                    }
                }
            }
            this.minValues = minValues;
        }

		public override long Get(long index)
        {
            //assert index >= 0 && index < valueCount;
            int block = (int)Number.URShift(index, blockShift);
            int idx = (int)(index & blockMask);
            return (minValues == null ? 0 : minValues[block]) + subReaders[block].Get(idx);
        }
		/// <summary>Returns approximate RAM bytes used</summary>
		public long RamBytesUsed()
		{
			long size = 0;
			foreach (PackedInts.Reader reader in subReaders)
			{
				size += reader.RamBytesUsed();
			}
			return size;
		}
    }
}
