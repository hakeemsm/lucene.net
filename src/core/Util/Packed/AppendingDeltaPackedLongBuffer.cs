using System;
using Lucene.Net.Support;

namespace Lucene.Net.Util.Packed
{
	/// <summary>Utility class to buffer a list of signed longs in memory.</summary>
	/// <remarks>
	/// Utility class to buffer a list of signed longs in memory. This class only
	/// supports appending and is optimized for the case where values are close to
	/// each other.
	/// </remarks>
	/// <lucene.internal></lucene.internal>
	public sealed class AppendingDeltaPackedLongBuffer : AbstractAppendingLongBuffer
	{
		internal long[] minValues;

		/// <summary>
		/// Create
		/// <see cref="AppendingDeltaPackedLongBuffer">AppendingDeltaPackedLongBuffer</see>
		/// </summary>
		/// <param name="initialPageCount">the initial number of pages</param>
		/// <param name="pageSize">the size of a single page</param>
		/// <param name="acceptableOverheadRatio">an acceptable overhead ratio per value</param>
		internal AppendingDeltaPackedLongBuffer(int initialPageCount, int pageSize, float
			 acceptableOverheadRatio) : base(initialPageCount, pageSize, acceptableOverheadRatio
			)
		{
			minValues = new long[values.Length];
		}

		/// <summary>
		/// Create an
		/// <see cref="AppendingDeltaPackedLongBuffer">AppendingDeltaPackedLongBuffer</see>
		/// with initialPageCount=16,
		/// pageSize=1024 and acceptableOverheadRatio=
		/// <see cref="PackedInts.DEFAULT">PackedInts.DEFAULT</see>
		/// </summary>
		public AppendingDeltaPackedLongBuffer() : this(16, 1024, PackedInts.DEFAULT)
		{
		}

		/// <summary>
		/// Create an
		/// <see cref="AppendingDeltaPackedLongBuffer">AppendingDeltaPackedLongBuffer</see>
		/// with initialPageCount=16,
		/// pageSize=1024
		/// </summary>
		public AppendingDeltaPackedLongBuffer(float acceptableOverheadRatio) : this(16, 1024
			, acceptableOverheadRatio)
		{
		}

		internal override long Get(int block, int element)
		{
			if (block == valuesOff)
			{
				return pending[element];
			}
			else
			{
				if (values[block] == null)
				{
					return minValues[block];
				}
				else
				{
					return minValues[block] + values[block].Get(element);
				}
			}
		}

		internal override int Get(int block, int element, long[] arr, int off, int len)
		{
			if (block == valuesOff)
			{
				int sysCopyToRead = Math.Min(len, pendingOff - element);
				System.Array.Copy(pending, element, arr, off, sysCopyToRead);
				return sysCopyToRead;
			}
			else
			{
				int read = values[block].Get(element, arr, off, len);
				long d = minValues[block];
				for (int r = 0; r < read; r++, off++)
				{
					arr[off] += d;
				}
				return read;
			}
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
			long delta = maxValue - minValue;
			minValues[valuesOff] = minValue;
			if (delta == 0)
			{
				values[valuesOff] = new PackedInts.NullReader(pendingOff);
			}
			else
			{
				// build a new packed reader
				int bitsRequired = delta < 0 ? 64 : PackedInts.BitsRequired(delta);
				for (int i_1 = 0; i_1 < pendingOff; ++i_1)
				{
					pending[i_1] -= minValue;
				}
				PackedInts.IMutable mutable = PackedInts.GetMutable(pendingOff, bitsRequired, acceptableOverheadRatio
					);
				for (int i_2 = 0; i_2 < pendingOff; )
				{
					i_2 += mutable.Set(i_2, pending, i_2, pendingOff - i_2);
				}
				values[valuesOff] = mutable;
			}
		}

		internal override void Grow(int newBlockCount)
		{
			base.Grow(newBlockCount);
			this.minValues = Arrays.CopyOf(minValues, newBlockCount);
		}

		internal override long BaseRamBytesUsed
		{
		    get { return base.BaseRamBytesUsed + RamUsageEstimator.NUM_BYTES_OBJECT_REF; }
		}

		// additional array
		public override long RamBytesUsed
		{
		    get { return base.RamBytesUsed + RamUsageEstimator.SizeOf(minValues); }
		}
	}
}
