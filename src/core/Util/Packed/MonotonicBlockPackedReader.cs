using Lucene.Net.Store;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Packed
{
	public sealed class MonotonicBlockPackedReader : LongValues
    {
        private readonly int blockShift, blockMask;
        private readonly long valueCount;
        private readonly long[] minValues;
        private readonly float[] averages;
        private readonly PackedInts.IReader[] subReaders;

        public MonotonicBlockPackedReader(IndexInput input, int packedIntsVersion, int blockSize, long valueCount, bool direct)
        {
            this.valueCount = valueCount;
			blockShift = PackedInts.CheckBlockSize(blockSize, AbstractBlockPackedWriter.MIN_BLOCK_SIZE
				, AbstractBlockPackedWriter.MAX_BLOCK_SIZE);
            blockMask = blockSize - 1;
			int numBlocks = PackedInts.NumBlocks(valueCount, blockSize);
            minValues = new long[numBlocks];
            averages = new float[numBlocks];
            subReaders = new PackedInts.Reader[numBlocks];
            for (int i = 0; i < numBlocks; ++i)
            {
                minValues[i] = input.ReadVLong();
                averages[i] = input.ReadInt().IntBitsToFloat();
                int bitsPerValue = input.ReadVInt();
                if (bitsPerValue > 64)
                {
                    throw new System.IO.IOException("Corrupted");
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
        }

		public override long Get(long index)
        {
            //assert index >= 0 && index < valueCount;
            int block = (int) Number.URShift(index, blockShift);
            int idx = (int) (index & blockMask);
            return minValues[block] + (long)(idx * averages[block]) + BlockPackedReaderIterator.ZigZagDecode(subReaders[block].Get(idx));
        }
		public long Size()
		{
			return valueCount;
		}
		public long RamBytesUsed()
		{
			long sizeInBytes = 0;
			sizeInBytes += RamUsageEstimator.SizeOf(minValues);
			sizeInBytes += RamUsageEstimator.SizeOf(averages);
			foreach (PackedInts.Reader reader in subReaders)
			{
				sizeInBytes += reader.RamBytesUsed();
			}
			return sizeInBytes;
		}
    }
}
