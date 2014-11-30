using System;

namespace Lucene.Net.Util.Packed
{
	/// <summary>Utility class to buffer a list of signed longs in memory.</summary>
	/// <remarks>
	/// Utility class to buffer a list of signed longs in memory. This class only
	/// supports appending and is optimized for non-negative numbers with a uniform distribution over a fixed (limited) range
	/// </remarks>
	/// <lucene.internal></lucene.internal>
	public sealed class AppendingPackedLongBuffer : AbstractAppendingLongBuffer
	{
		/// <summary><see cref="AppendingPackedLongBuffer">AppendingPackedLongBuffer</see></summary>
		/// <param name="initialPageCount">the initial number of pages</param>
		/// <param name="pageSize">the size of a single page</param>
		/// <param name="acceptableOverheadRatio">an acceptable overhead ratio per value</param>
		internal AppendingPackedLongBuffer(int initialPageCount, int pageSize, float acceptableOverheadRatio
			) : base(initialPageCount, pageSize, acceptableOverheadRatio)
		{
		}

		/// <summary>
		/// Create an
		/// <see cref="AppendingPackedLongBuffer">AppendingPackedLongBuffer</see>
		/// with initialPageCount=16,
		/// pageSize=1024 and acceptableOverheadRatio=
		/// <see cref="PackedInts.DEFAULT">PackedInts.DEFAULT</see>
		/// </summary>
		public AppendingPackedLongBuffer() : this(16, 1024, PackedInts.DEFAULT)
		{
		}

		/// <summary>
		/// Create an
		/// <see cref="AppendingPackedLongBuffer">AppendingPackedLongBuffer</see>
		/// with initialPageCount=16,
		/// pageSize=1024
		/// </summary>
		public AppendingPackedLongBuffer(float acceptableOverheadRatio) : this(16, 1024, 
			acceptableOverheadRatio)
		{
		}

		internal override long Get(int block, int element)
		{
		    return block == valuesOff ? pending[element] : values[block].Get(element);
		}

	    internal override int Get(int block, int element, long[] arr, int off, int len)
	    {
	        if (block == valuesOff)
			{
				int sysCopyToRead = Math.Min(len, pendingOff - element);
				Array.Copy(pending, element, arr, off, sysCopyToRead);
				return sysCopyToRead;
			}
	        return values[block].Get(element, arr, off, len);
	    }

	    internal override void PackPendingValues()
		{
			// compute max delta
			long minValue = pending[0];
			long maxValue = pending[0];
			for (int i = 1; i < pendingOff; ++i)
			{
				minValue = Math.Min(minValue, pending[i]);
				maxValue = Math.Max(maxValue, pending[i]);
			}
			// build a new packed reader
			int bitsRequired = minValue < 0 ? 64 : PackedInts.BitsRequired(maxValue);
			PackedInts.IMutable mutable = PackedInts.GetMutable(pendingOff, bitsRequired, acceptableOverheadRatio);
			for (int i1 = 0; i1 < pendingOff; )
			{
				i1 += mutable.Set(i1, pending, i1, pendingOff - i1);
			}
			values[valuesOff] = mutable;
		}
	}
}
