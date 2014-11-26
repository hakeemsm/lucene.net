namespace Lucene.Net.Util.Packed
{
	/// <summary>
	/// A
	/// <see cref="PagedGrowableWriter">PagedGrowableWriter</see>
	/// . This class slices data into fixed-size blocks
	/// which have independent numbers of bits per value and grow on-demand.
	/// <p>You should use this class instead of the
	/// <see cref="AbstractAppendingLongBuffer">AbstractAppendingLongBuffer</see>
	/// related ones only when
	/// you need random write-access. Otherwise this class will likely be slower and
	/// less memory-efficient.
	/// </summary>
	/// <lucene.internal></lucene.internal>
	internal sealed class PagedGrowableWriter : AbstractPagedMutable<PagedGrowableWriter>
	{
		internal readonly float acceptableOverheadRatio;

		/// <summary>
		/// Create a new
		/// <see cref="PagedGrowableWriter">PagedGrowableWriter</see>
		/// instance.
		/// </summary>
		/// <param name="size">the number of values to store.</param>
		/// <param name="pageSize">the number of values per page</param>
		/// <param name="startBitsPerValue">the initial number of bits per value</param>
		/// <param name="acceptableOverheadRatio">an acceptable overhead ratio</param>
		public PagedGrowableWriter(long size, int pageSize, int startBitsPerValue, float 
			acceptableOverheadRatio) : this(size, pageSize, startBitsPerValue, acceptableOverheadRatio
			, true)
		{
		}

		internal PagedGrowableWriter(long size, int pageSize, int startBitsPerValue, float
			 acceptableOverheadRatio, bool fillPages) : base(startBitsPerValue, size, pageSize
			)
		{
			this.acceptableOverheadRatio = acceptableOverheadRatio;
			if (fillPages)
			{
				FillPages();
			}
		}

		protected internal override PackedInts.IMutable NewMutable(int valueCount, int bitsPerValue)
		{
			return new GrowableWriter(bitsPerValue, valueCount, acceptableOverheadRatio);
		}

		protected internal override Lucene.Net.Util.Packed.PagedGrowableWriter NewUnfilledCopy
			(long newSize)
		{
			return new Lucene.Net.Util.Packed.PagedGrowableWriter(newSize, PageSize(), 
				bitsPerValue, acceptableOverheadRatio, false);
		}

		protected internal override long BaseRamBytesUsed()
		{
			return base.BaseRamBytesUsed() + RamUsageEstimator.NUM_BYTES_FLOAT;
		}
	}
}
