using Lucene.Net.Support;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Packed
{
    public abstract class AbstractAppendingLongBuffer : LongValues
    {
		internal const int MIN_PAGE_SIZE = 64;

		internal const int MAX_PAGE_SIZE = 1 << 20;

		internal readonly int pageShift;

		internal readonly int pageMask;
		internal PackedInts.IReader[] values;
		private long valuesBytes;
        internal int valuesOff;
        internal long[] pending;
        internal int pendingOff;

		internal float acceptableOverheadRatio;
		internal AbstractAppendingLongBuffer(int initialBlockCount, int pageSize, float acceptableOverheadRatio)
        {
			// More than 1M doesn't really makes sense with these appending buffers
			// since their goal is to try to have small numbers of bits per value
			values = new PackedInts.Reader[initialBlockCount];
			pending = new long[pageSize];
			pageShift = PackedInts.CheckBlockSize(pageSize, MIN_PAGE_SIZE, MAX_PAGE_SIZE);
			pageMask = pageSize - 1;
            valuesOff = 0;
            pendingOff = 0;
			this.acceptableOverheadRatio = acceptableOverheadRatio;
        }

		internal int PageSize()
		{
			return pageMask + 1;
		}
        public long Size
        {
            get
            {
                long size = pendingOff;
                if (valuesOff > 0)
                {
                    size += values[valuesOff - 1].Size();
                }
                if (valuesOff > 1)
                {
                    size += (long) (valuesOff - 1)*PageSize();
                }
                return size;
            }
        }

        public void Add(long l)
        {
			if (pending == null)
			{
				throw new InvalidOperationException("This buffer is frozen");
			}
			if (pendingOff == pending.Length)
            {
                // check size
				if (values.Length == valuesOff)
                {
                    int newLength = ArrayUtil.Oversize(valuesOff + 1, 8);
                    Grow(newLength);
                }
                PackPendingValues();
				valuesBytes += values[valuesOff].RamBytesUsed();
                ++valuesOff;
                // reset pending buffer
                pendingOff = 0;
            }
            pending[pendingOff++] = l;
        }

        internal virtual void Grow(int newBlockCount)
        {
			values = Arrays.CopyOf(values, newBlockCount);
        }

        internal abstract void PackPendingValues();

        public override long Get(long index)
        {
            if (index < 0 || index >= Size)
            {
                throw new IndexOutOfRangeException("" + index);
            }
			int block = (int)(index >> pageShift);
			int element = (int)(index & pageMask);
			return Get(block, element);
        }

		public int Get(long index, long[] arr, int off, int len)
		{
			//HM:revisit 
			//assert len > 0 : "len must be > 0 (got " + len + ")";
			//HM:revisit 
			//assert index >= 0 && index < size();
			//HM:revisit 
			//assert off + len <= arr.length;
			int block = (int)(index >> pageShift);
			int element = (int)(index & pageMask);
			return Get(block, element, arr, off, len);
		}
        internal abstract long Get(int block, int element);

		internal abstract int Get(int block, int element, long[] arr, int off, int len);
		public virtual Iterator GetIterator()
		{
			return new Iterator(this);
		}
        
        public class Iterator
        {
            protected readonly AbstractAppendingLongBuffer parent;
            internal long[] currentValues;
            internal int vOff, pOff;

			internal int currentCount;

            internal Iterator(AbstractAppendingLongBuffer _enclosing)
			{
				this._enclosing = _enclosing;
				// number of entries of the current page
				this.vOff = this.pOff = 0;
				if (this._enclosing.valuesOff == 0)
				{
					this.currentValues = this._enclosing.pending;
					this.currentCount = this._enclosing.pendingOff;
				}
				else
				{
					this.currentValues = new long[this._enclosing.values[0].Size()];
					this.FillValues();
				}
			}

			internal void FillValues()
			{
				if (this.vOff == this._enclosing.valuesOff)
				{
					this.currentValues = this._enclosing.pending;
					this.currentCount = this._enclosing.pendingOff;
				}
				else
				{
					this.currentCount = this._enclosing.values[this.vOff].Size();
					for (int k = 0; k < this.currentCount; )
					{
						k += this._enclosing.Get(this.vOff, k, this.currentValues, k, this.currentCount -
							 k);
					}
				}
			}

            /** Whether or not there are remaining values. */
            public bool HasNext()
            {
				return this.pOff < this.currentCount;
            }

            /** Return the next long in the buffer. */
            public long Next()
            {
                //assert hasNext();
                long result = currentValues[pOff++];
				if (this.pOff == this.currentCount)
				{
					this.vOff += 1;
					this.pOff = 0;
					if (this.vOff <= this._enclosing.valuesOff)
					{
						this.FillValues();
					}
					else
					{
						this.currentCount = 0;
					}
				}
                return result;
            }
			private readonly AbstractAppendingLongBuffer _enclosing;
        }

        internal virtual long BaseRamBytesUsed
        {
            get
            {
			return RamUsageEstimator.NUM_BYTES_OBJECT_HEADER + 2 * RamUsageEstimator.NUM_BYTES_OBJECT_REF
				 + 2 * RamUsageEstimator.NUM_BYTES_INT + 2 * RamUsageEstimator.NUM_BYTES_INT + RamUsageEstimator
				.NUM_BYTES_FLOAT + RamUsageEstimator.NUM_BYTES_LONG;
            }
        }

        public virtual long RamBytesUsed
        {
            get
            {
                // TODO: this is called per-doc-per-norms/dv-field, can we optimize this?
			long bytesUsed = RamUsageEstimator.AlignObjectSize(BaseRamBytesUsed) + (pending
				 != null ? RamUsageEstimator.SizeOf(pending) : 0L) + RamUsageEstimator.AlignObjectSize
				(RamUsageEstimator.NUM_BYTES_ARRAY_HEADER + (long)RamUsageEstimator.NUM_BYTES_OBJECT_REF
				 * values.Length);
			// values
			return bytesUsed + valuesBytes;
            }
        }
		public virtual void Freeze()
		{
			if (pendingOff > 0)
			{
				if (values.Length == valuesOff)
				{
					Grow(valuesOff + 1);
				}
				// don't oversize!
				PackPendingValues();
				valuesBytes += values[valuesOff].RamBytesUsed();
				++valuesOff;
				pendingOff = 0;
			}
			pending = null;
		}
    }
}
