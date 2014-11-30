using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Packed
{
    public sealed class MonotonicAppendingLongBuffer : AbstractAppendingLongBuffer
    {
        internal static long ZigZagDecode(long n)
        {
            return (Number.URShift(n, 1) ^ -(n & 1));
        }

        internal static long ZigZagEncode(long n)
        {
            return (n >> 63) ^ (n << 1);
        }

        private float[] averages;

        internal long[] minValues;
        internal MonotonicAppendingLongBuffer(int initialPageCount, int pageSize, float acceptableOverheadRatio
            )
            : base(initialPageCount, pageSize, acceptableOverheadRatio)
        {
            averages = new float[16];
            minValues = new long[values.Length];
        }

        public MonotonicAppendingLongBuffer()
            : this(16, 1024, PackedInts.DEFAULT)
        {
        }
        public MonotonicAppendingLongBuffer(float acceptableOverheadRatio)
            : this(16, 1024
                , acceptableOverheadRatio)
        {
        }
        internal override long Get(int block, int element)
        {
            if (block == valuesOff)
            {
                return pending[element];
            }
            long baselong = minValues[block] + (long)(averages[block] * (long)element);
            if (values[block] == null)
            {
                return baselong;
            }
            return baselong + ZigZagDecode(values[block].Get(element));
        }

        internal override int Get(int block, int element, long[] arr, int off, int len)
        {
            if (block == valuesOff)
            {
                int sysCopyToRead = Math.Min(len, pendingOff - element);
                System.Array.Copy(pending, element, arr, off, sysCopyToRead);
                return sysCopyToRead;
            }
            if (values[block] == null)
            {
                int toFill = Math.Min(len, pending.Length - element);
                for (int r = 0; r < toFill; r++, off++, element++)
                {
                    arr[off] = minValues[block] + (long)(averages[block] * (long)element);
                }
                return toFill;
            }
            int read = values[block].Get(element, arr, off, len);
            for (int r = 0; r < read; r++, off++, element++)
            {
                arr[off] = minValues[block] + (long)(averages[block] * (long)element) + ZigZagDecode
                    (arr[off]);
            }
            return read;
        }
        internal override void Grow(int newBlockCount)
        {
            base.Grow(newBlockCount);
            this.averages = Arrays.CopyOf(averages, newBlockCount);
            this.minValues = Arrays.CopyOf(minValues, newBlockCount);
        }

        internal override void PackPendingValues()
        {
            //assert pendingOff == MAX_PENDING_COUNT;

            minValues[valuesOff] = pending[0];
            averages[valuesOff] = pendingOff == 1 ? 0 : (float)(pending[pendingOff - 1] - pending
                [0]) / (pendingOff - 1);
            for (int i = 0; i < pendingOff; ++i)
            {
                pending[i] = ZigZagEncode(pending[i] - minValues[valuesOff] - (long)(averages[valuesOff] * (long)i));
            }
            long maxDelta = 0;
            for (int i = 0; i < pendingOff; ++i)
            {
                if (pending[i] < 0)
                {
                    maxDelta = -1;
                    break;
                }
                maxDelta = Math.Max(maxDelta, pending[i]);
            }
            if (maxDelta == 0)
            {
                values[valuesOff] = new PackedInts.NullReader(pendingOff);
            }
            else
            {
                int bitsRequired = maxDelta < 0 ? 64 : PackedInts.BitsRequired(maxDelta);
                PackedInts.IMutable mutable = PackedInts.GetMutable(pendingOff, bitsRequired, acceptableOverheadRatio);
                for (int i = 0; i < pendingOff; )
                {
                    i += mutable.Set(i, pending, i, pendingOff - i);
                }
                values[valuesOff] = mutable;
            }
        }


        internal override long BaseRamBytesUsed
        {
            get
            {
                return base.BaseRamBytesUsed + 2 * RamUsageEstimator.NUM_BYTES_OBJECT_REF;
            }
        }

        public override long RamBytesUsed
        {
            get
            {
                return base.RamBytesUsed + RamUsageEstimator.SizeOf(averages) + RamUsageEstimator.SizeOf(minValues);
            }
        }
    }
}
