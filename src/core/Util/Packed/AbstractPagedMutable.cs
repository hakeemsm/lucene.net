using System;

namespace Lucene.Net.Util.Packed
{
	/// <summary>
	/// Base implementation for
	/// <see cref="PagedMutable">PagedMutable</see>
	/// and
	/// <see cref="PagedGrowableWriter">PagedGrowableWriter</see>
	/// .
	/// </summary>
	/// <lucene.internal></lucene.internal>
	internal abstract class AbstractPagedMutable<T> : LongValues where T:AbstractPagedMutable
		<T>
	{
		internal const int MIN_BLOCK_SIZE = 1 << 6;

		internal const int MAX_BLOCK_SIZE = 1 << 30;

		internal readonly long size;

		internal readonly int pageShift;

		internal readonly int pageMask;

		internal readonly PackedInts.IMutable[] subMutables;

		internal readonly int bitsPerValue;

		internal AbstractPagedMutable(int bitsPerValue, long size, int pageSize)
		{
			this.bitsPerValue = bitsPerValue;
			this.size = size;
			pageShift = PackedInts.CheckBlockSize(pageSize, MIN_BLOCK_SIZE, MAX_BLOCK_SIZE);
			pageMask = pageSize - 1;
			int numPages = PackedInts.NumBlocks(size, pageSize);
			subMutables = new PackedInts.Mutable[numPages];
		}

		protected internal void FillPages()
		{
			int numPages = PackedInts.NumBlocks(size, PageSize());
			for (int i = 0; i < numPages; ++i)
			{
				// do not allocate for more entries than necessary on the last page
				int valueCount = i == numPages - 1 ? LastPageSize(size) : PageSize();
				subMutables[i] = NewMutable(valueCount, bitsPerValue);
			}
		}

		protected internal abstract PackedInts.IMutable NewMutable(int valueCount, int bitsPerValue);

		internal int LastPageSize(long size)
		{
			int sz = IndexInPage(size);
			return sz == 0 ? PageSize() : sz;
		}

		internal int PageSize()
		{
			return pageMask + 1;
		}

		/// <summary>The number of values.</summary>
		/// <remarks>The number of values.</remarks>
		public long Size()
		{
			return size;
		}

		internal int PageIndex(long index)
		{
			return (int)((long)(((ulong)index) >> pageShift));
		}

		internal int IndexInPage(long index)
		{
			return (int)index & pageMask;
		}

		public sealed override long Get(long index)
		{
			//HM:revisit 
			//assert index >= 0 && index < size;
			int pageIndex = PageIndex(index);
			int indexInPage = IndexInPage(index);
			return subMutables[pageIndex].Get(indexInPage);
		}

		/// <summary>Set value at <code>index</code>.</summary>
		/// <remarks>Set value at <code>index</code>.</remarks>
		public void Set(long index, long value)
		{
			//HM:revisit 
			//assert index >= 0 && index < size;
			int pageIndex = PageIndex(index);
			int indexInPage = IndexInPage(index);
			subMutables[pageIndex].Set(indexInPage, value);
		}

		protected internal virtual long BaseRamBytesUsed()
		{
			return RamUsageEstimator.NUM_BYTES_OBJECT_HEADER + RamUsageEstimator.NUM_BYTES_OBJECT_REF
				 + RamUsageEstimator.NUM_BYTES_LONG + 3 * RamUsageEstimator.NUM_BYTES_INT;
		}

		/// <summary>Return the number of bytes used by this object.</summary>
		/// <remarks>Return the number of bytes used by this object.</remarks>
		public virtual long RamBytesUsed()
		{
			long bytesUsed = RamUsageEstimator.AlignObjectSize(BaseRamBytesUsed());
			bytesUsed += RamUsageEstimator.AlignObjectSize(RamUsageEstimator.NUM_BYTES_ARRAY_HEADER
				 + (long)RamUsageEstimator.NUM_BYTES_OBJECT_REF * subMutables.Length);
			foreach (PackedInts.Mutable gw in subMutables)
			{
				bytesUsed += gw.RamBytesUsed();
			}
			return bytesUsed;
		}

		protected internal abstract T NewUnfilledCopy(long newSize);

		/// <summary>
		/// Create a new copy of size <code>newSize</code> based on the content of
		/// this buffer.
		/// </summary>
		/// <remarks>
		/// Create a new copy of size <code>newSize</code> based on the content of
		/// this buffer. This method is much more efficient than creating a new
		/// instance and copying values one by one.
		/// </remarks>
		public T Resize(long newSize)
		{
			T copy = NewUnfilledCopy(newSize);
			int numCommonPages = Math.Min(copy.subMutables.Length, subMutables.Length);
			long[] copyBuffer = new long[1024];
			for (int i = 0; i < copy.subMutables.Length; ++i)
			{
				int valueCount = i == copy.subMutables.Length - 1 ? LastPageSize(newSize) : PageSize
					();
				int bpv = i < numCommonPages ? subMutables[i].GetBitsPerValue() : this.bitsPerValue;
				copy.subMutables[i] = NewMutable(valueCount, bpv);
				if (i < numCommonPages)
				{
					int copyLength = Math.Min(valueCount, subMutables[i].Size());
					PackedInts.Copy(subMutables[i], 0, copy.subMutables[i], 0, copyLength, copyBuffer);
				}
			}
			return copy;
		}

		/// <summary>
		/// Similar to
		/// <see cref="Lucene.Net.Util.ArrayUtil.Grow(long[], int)">Lucene.Net.Util.ArrayUtil.Grow(long[], int)
		/// 	</see>
		/// .
		/// </summary>
		public T Grow(long minSize)
		{
			//HM:revisit 
			//assert minSize >= 0;
			if (minSize <= Size())
			{
				T result = (T)this;
				return result;
			}
			long extra = (long)(((ulong)minSize) >> 3);
			if (extra < 3)
			{
				extra = 3;
			}
			long newSize = minSize + extra;
			return Resize(newSize);
		}

		/// <summary>
		/// Similar to
		/// <see cref="Lucene.Net.Util.ArrayUtil.Grow(long[])">Lucene.Net.Util.ArrayUtil.Grow(long[])
		/// 	</see>
		/// .
		/// </summary>
		public T Grow()
		{
			return Grow(Size() + 1);
		}

		public sealed override string ToString()
		{
			return GetType().Name + "(size=" + Size() + ",pageSize=" + PageSize() + ")";
		}
	}
}
