namespace Lucene.Net.Util.Packed
{
	/// <summary>
	/// A
	/// <see cref="PagedMutable">PagedMutable</see>
	/// . This class slices data into fixed-size blocks
	/// which have the same number of bits per value. It can be a useful replacement
	/// for
	/// <see cref="Mutable">Mutable</see>
	/// to store more than 2B values.
	/// </summary>
	/// <lucene.internal></lucene.internal>
	internal sealed class PagedMutable : AbstractPagedMutable<PagedMutable>
	{
		internal readonly PackedInts.Format format;

		/// <summary>
		/// Create a new
		/// <see cref="PagedMutable">PagedMutable</see>
		/// instance.
		/// </summary>
		/// <param name="size">the number of values to store.</param>
		/// <param name="pageSize">the number of values per page</param>
		/// <param name="bitsPerValue">the number of bits per value</param>
		/// <param name="acceptableOverheadRatio">an acceptable overhead ratio</param>
		public PagedMutable(long size, int pageSize, int bitsPerValue, float acceptableOverheadRatio
			) : this(size, pageSize, PackedInts.FastestFormatAndBits(pageSize, bitsPerValue, 
			acceptableOverheadRatio))
		{
			FillPages();
		}

		internal PagedMutable(long size, int pageSize, PackedInts.FormatAndBits formatAndBits
			) : this(size, pageSize, formatAndBits.BitsPerValue, formatAndBits.Format)
		{
		}

		internal PagedMutable(long size, int pageSize, int bitsPerValue, PackedInts.Format
			 format) : base(bitsPerValue, size, pageSize)
		{
			this.format = format;
		}

		protected internal override PackedInts.Mutable NewMutable(int valueCount, int bitsPerValue)
		{
			//HM:revisit 
			//assert this.bitsPerValue >= bitsPerValue;
			return PackedInts.GetMutable(valueCount, this.bitsPerValue, format);
		}

		protected internal override Lucene.Net.Util.Packed.PagedMutable NewUnfilledCopy
			(long newSize)
		{
			return new Lucene.Net.Util.Packed.PagedMutable(newSize, PageSize(), bitsPerValue
				, format);
		}

		protected internal override long BaseRamBytesUsed()
		{
			return base.BaseRamBytesUsed() + RamUsageEstimator.NUM_BYTES_OBJECT_REF;
		}
	}
}
