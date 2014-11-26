using System;
using Lucene.Net.Support;

namespace Lucene.Net.Util.Packed
{
	/// <summary>
	/// A decoder for an
	/// <see cref="EliasFanoEncoder">EliasFanoEncoder</see>
	/// .
	/// </summary>
	/// <lucene.internal></lucene.internal>
	public class EliasFanoDecoder
	{
		private static readonly int LOG2_LONG_SIZE = sizeof(long).NumberOfTrailingZeros();

		private readonly EliasFanoEncoder efEncoder;

		private readonly long numEncoded;

		private long efIndex = -1;

		private long setBitForIndex = -1;

		public const long NO_MORE_VALUES = -1L;

		private readonly long numIndexEntries;

		private readonly long indexMask;

		/// <summary>
		/// Construct a decoder for a given
		/// <see cref="EliasFanoEncoder">EliasFanoEncoder</see>
		/// .
		/// The decoding index is set to just before the first encoded value.
		/// </summary>
		public EliasFanoDecoder(EliasFanoEncoder efEncoder)
		{
			// bit selection in long
			// the decoding index.
			// the index of the high bit at the decoding index.
			this.efEncoder = efEncoder;
			this.numEncoded = efEncoder.numEncoded;
			// not final in EliasFanoEncoder
			this.numIndexEntries = efEncoder.currentEntryIndex;
			// not final in EliasFanoEncoder
			this.indexMask = (1L << efEncoder.nIndexEntryBits) - 1;
		}

		/// <returns>The Elias-Fano encoder that is decoded.</returns>
		public virtual EliasFanoEncoder GetEliasFanoEncoder()
		{
			return efEncoder;
		}

		/// <summary>The number of values encoded by the encoder.</summary>
		/// <remarks>The number of values encoded by the encoder.</remarks>
		/// <returns>The number of values encoded by the encoder.</returns>
		public virtual long NumEncoded()
		{
			return numEncoded;
		}

		/// <summary>The current decoding index.</summary>
		/// <remarks>
		/// The current decoding index.
		/// The first value encoded by
		/// <see cref="EliasFanoEncoder.EncodeNext(long)">EliasFanoEncoder.EncodeNext(long)</see>
		/// has index 0.
		/// Only valid directly after
		/// <see cref="NextValue()">NextValue()</see>
		/// ,
		/// <see cref="AdvanceToValue(long)">AdvanceToValue(long)</see>
		/// ,
		/// <see cref="PreviousValue()">PreviousValue()</see>
		/// , or
		/// <see cref="BackToValue(long)">BackToValue(long)</see>
		/// returned another value than
		/// <see cref="NO_MORE_VALUES">NO_MORE_VALUES</see>
		/// ,
		/// or
		/// <see cref="AdvanceToIndex(long)">AdvanceToIndex(long)</see>
		/// returned true.
		/// </remarks>
		/// <returns>
		/// The decoding index of the last decoded value, or as last set by
		/// <see cref="AdvanceToIndex(long)">AdvanceToIndex(long)</see>
		/// .
		/// </returns>
		public virtual long CurrentIndex()
		{
			if (efIndex < 0)
			{
				throw new InvalidOperationException("index before sequence");
			}
			if (efIndex >= numEncoded)
			{
				throw new InvalidOperationException("index after sequence");
			}
			return efIndex;
		}

		/// <summary>The value at the current decoding index.</summary>
		/// <remarks>
		/// The value at the current decoding index.
		/// Only valid when
		/// <see cref="CurrentIndex()">CurrentIndex()</see>
		/// would return a valid result.
		/// <br />This is only intended for use after
		/// <see cref="AdvanceToIndex(long)">AdvanceToIndex(long)</see>
		/// returned true.
		/// </remarks>
		/// <returns>
		/// The value encoded at
		/// <see cref="CurrentIndex()">CurrentIndex()</see>
		/// .
		/// </returns>
		public virtual long CurrentValue()
		{
			return CombineHighLowValues(CurrentHighValue(), CurrentLowValue());
		}

		/// <returns>The high value for the current decoding index.</returns>
		private long CurrentHighValue()
		{
			return setBitForIndex - efIndex;
		}

		// sequence of unary gaps
		/// <summary>
		/// See also
		/// <see cref="EliasFanoEncoder.PackValue(long, long[], int, long)">EliasFanoEncoder.PackValue(long, long[], int, long)
		/// 	</see>
		/// 
		/// </summary>
		private static long UnPackValue(long[] longArray, int numBits, long packIndex, long
			 bitsMask)
		{
			if (numBits == 0)
			{
				return 0;
			}
			long bitPos = packIndex * numBits;
			int index = (int)((long)(((ulong)bitPos) >> LOG2_LONG_SIZE));
			int bitPosAtIndex = (int)(bitPos & (sizeof(long) - 1));
			long value = (long)(((ulong)longArray[index]) >> bitPosAtIndex);
			if ((bitPosAtIndex + numBits) > sizeof(long))
			{
				value |= (longArray[index + 1] << (sizeof(long) - bitPosAtIndex));
			}
			value &= bitsMask;
			return value;
		}

		/// <returns>The low value for the current decoding index.</returns>
		private long CurrentLowValue()
		{
			//HM:revisit 
			//assert ((efIndex >= 0) && (efIndex < numEncoded)) : "efIndex " + efIndex;
			return UnPackValue(efEncoder.lowerLongs, efEncoder.numLowBits, efIndex, efEncoder
				.lowerBitsMask);
		}

		/// <returns>
		/// The given highValue shifted left by the number of low bits from by the EliasFanoSequence,
		/// logically OR-ed with the given lowValue.
		/// </returns>
		private long CombineHighLowValues(long highValue, long lowValue)
		{
			return (highValue << efEncoder.numLowBits) | lowValue;
		}

		private long curHighLong;

		/// <summary>Set the decoding index to just before the first encoded value.</summary>
		/// <remarks>Set the decoding index to just before the first encoded value.</remarks>
		public virtual void ToBeforeSequence()
		{
			efIndex = -1;
			setBitForIndex = -1;
		}

		/// <returns>the number of bits in a long after (setBitForIndex modulo sizeof(long))</returns>
		private int GetCurrentRightShift()
		{
			int s = (int)(setBitForIndex & (sizeof(long) - 1));
			return s;
		}

		/// <summary>
		/// Increment efIndex and setBitForIndex and
		/// shift curHighLong so that it does not contain the high bits before setBitForIndex.
		/// </summary>
		/// <remarks>
		/// Increment efIndex and setBitForIndex and
		/// shift curHighLong so that it does not contain the high bits before setBitForIndex.
		/// </remarks>
		/// <returns>true iff efIndex still smaller than numEncoded.</returns>
		private bool ToAfterCurrentHighBit()
		{
			efIndex += 1;
			if (efIndex >= numEncoded)
			{
				return false;
			}
			setBitForIndex += 1;
			int highIndex = (int)((long)(((ulong)setBitForIndex) >> LOG2_LONG_SIZE));
			curHighLong = (long)(((ulong)efEncoder.upperLongs[highIndex]) >> GetCurrentRightShift
				());
			return true;
		}

		/// <summary>The current high long has been determined to not contain the set bit that is needed.
		/// 	</summary>
		/// <remarks>
		/// The current high long has been determined to not contain the set bit that is needed.
		/// Increment setBitForIndex to the next high long and set curHighLong accordingly.
		/// </remarks>
		private void ToNextHighLong()
		{
			setBitForIndex += sizeof(long) - (setBitForIndex & (sizeof(long) - 1));
			//
			//HM:revisit 
			//assert getCurrentRightShift() == 0;
			int highIndex = (int)((long)(((ulong)setBitForIndex) >> LOG2_LONG_SIZE));
			curHighLong = efEncoder.upperLongs[highIndex];
		}

		/// <summary>
		/// setBitForIndex and efIndex have just been incremented, scan to the next high set bit
		/// by incrementing setBitForIndex, and by setting curHighLong accordingly.
		/// </summary>
		/// <remarks>
		/// setBitForIndex and efIndex have just been incremented, scan to the next high set bit
		/// by incrementing setBitForIndex, and by setting curHighLong accordingly.
		/// </remarks>
		private void ToNextHighValue()
		{
			while (curHighLong == 0L)
			{
				ToNextHighLong();
			}
			// inlining and unrolling would simplify somewhat
            setBitForIndex += curHighLong.NumberOfTrailingZeros();
		}

		/// <summary>
		/// setBitForIndex and efIndex have just been incremented, scan to the next high set bit
		/// by incrementing setBitForIndex, and by setting curHighLong accordingly.
		/// </summary>
		/// <remarks>
		/// setBitForIndex and efIndex have just been incremented, scan to the next high set bit
		/// by incrementing setBitForIndex, and by setting curHighLong accordingly.
		/// </remarks>
		/// <returns>the next encoded high value.</returns>
		private long NextHighValue()
		{
			ToNextHighValue();
			return CurrentHighValue();
		}

		/// <summary>
		/// If another value is available after the current decoding index, return this value and
		/// and increase the decoding index by 1.
		/// </summary>
		/// <remarks>
		/// If another value is available after the current decoding index, return this value and
		/// and increase the decoding index by 1. Otherwise return
		/// <see cref="NO_MORE_VALUES">NO_MORE_VALUES</see>
		/// .
		/// </remarks>
		public virtual long NextValue()
		{
			if (!ToAfterCurrentHighBit())
			{
				return NO_MORE_VALUES;
			}
			long highValue = NextHighValue();
			return CombineHighLowValues(highValue, CurrentLowValue());
		}

		/// <summary>Advance the decoding index to a given index.</summary>
		/// <remarks>
		/// Advance the decoding index to a given index.
		/// and return <code>true</code> iff it is available.
		/// <br />See also
		/// <see cref="CurrentValue()">CurrentValue()</see>
		/// .
		/// <br />The current implementation does not use the index on the upper bit zero bit positions.
		/// <br />Note: there is currently no implementation of <code>backToIndex</code>.
		/// </remarks>
		public virtual bool AdvanceToIndex(long index)
		{
			//HM:revisit 
			//assert index > efIndex;
			if (index >= numEncoded)
			{
				efIndex = numEncoded;
				return false;
			}
			if (!ToAfterCurrentHighBit())
			{
			}
			//HM:revisit 
			//assert false;
            int curSetBits = curHighLong.BitCount();
			while ((efIndex + curSetBits) < index)
			{
				// curHighLong has not enough set bits to reach index
				efIndex += curSetBits;
				ToNextHighLong();
				curSetBits = long.BitCount(curHighLong);
			}
			// curHighLong has enough set bits to reach index
			while (efIndex < index)
			{
				if (!ToAfterCurrentHighBit())
				{
				}
				//HM:revisit 
				//assert false;
				ToNextHighValue();
			}
			return true;
		}

		/// <summary>
		/// Given a target value, advance the decoding index to the first bigger or equal value
		/// and return it if it is available.
		/// </summary>
		/// <remarks>
		/// Given a target value, advance the decoding index to the first bigger or equal value
		/// and return it if it is available. Otherwise return
		/// <see cref="NO_MORE_VALUES">NO_MORE_VALUES</see>
		/// .
		/// <br />The current implementation uses the index on the upper zero bit positions.
		/// </remarks>
		public virtual long AdvanceToValue(long target)
		{
			efIndex += 1;
			if (efIndex >= numEncoded)
			{
				return NO_MORE_VALUES;
			}
			setBitForIndex += 1;
			// the high bit at setBitForIndex belongs to the unary code for efIndex
			int highIndex = (int)((long)(((ulong)setBitForIndex) >> LOG2_LONG_SIZE));
			long upperLong = efEncoder.upperLongs[highIndex];
			curHighLong = (long)(((ulong)upperLong) >> ((int)(setBitForIndex & (sizeof(long) - 1
				))));
			// may contain the unary 1 bit for efIndex
			// determine index entry to advance to
			long highTarget = (long)(((ulong)target) >> efEncoder.numLowBits);
			long indexEntryIndex = (highTarget / efEncoder.indexInterval) - 1;
			if (indexEntryIndex >= 0)
			{
				// not before first index entry
				if (indexEntryIndex >= numIndexEntries)
				{
					indexEntryIndex = numIndexEntries - 1;
				}
				// no further than last index entry
				long indexHighValue = (indexEntryIndex + 1) * efEncoder.indexInterval;
				//HM:revisit 
				//assert indexHighValue <= highTarget;
				if (indexHighValue > (setBitForIndex - efIndex))
				{
					// advance to just after zero bit position of index entry.
					setBitForIndex = UnPackValue(efEncoder.upperZeroBitPositionIndex, efEncoder.nIndexEntryBits
						, indexEntryIndex, indexMask);
					efIndex = setBitForIndex - indexHighValue;
					// the high bit at setBitForIndex belongs to the unary code for efIndex
					highIndex = (int)((long)(((ulong)setBitForIndex) >> LOG2_LONG_SIZE));
					upperLong = efEncoder.upperLongs[highIndex];
					curHighLong = (long)(((ulong)upperLong) >> ((int)(setBitForIndex & (sizeof(long) - 1
						))));
				}
			}
			// may contain the unary 1 bit for efIndex
			//HM:revisit 
			//assert efIndex < numEncoded; // there is a high value to be found.
			int curSetBits = long.BitCount(curHighLong);
			// shifted right.
			int curClearBits = sizeof(long) - curSetBits - ((int)(setBitForIndex & (sizeof(long) - 
				1)));
			// subtract right shift, may be more than encoded
			while (((setBitForIndex - efIndex) + curClearBits) < highTarget)
			{
				// curHighLong has not enough clear bits to reach highTarget
				efIndex += curSetBits;
				if (efIndex >= numEncoded)
				{
					return NO_MORE_VALUES;
				}
				setBitForIndex += sizeof(long) - (setBitForIndex & (sizeof(long) - 1));
				// highIndex = (int)(setBitForIndex >>> LOG2_LONG_SIZE);
				//HM:revisit 
				//assert (highIndex + 1) == (int)(setBitForIndex >>> LOG2_LONG_SIZE);
				highIndex += 1;
				upperLong = efEncoder.upperLongs[highIndex];
				curHighLong = upperLong;
				curSetBits = long.BitCount(curHighLong);
				curClearBits = sizeof(long) - curSetBits;
			}
			// curHighLong has enough clear bits to reach highTarget, and may not have enough set bits.
			while (curHighLong == 0L)
			{
				setBitForIndex += sizeof(long) - (setBitForIndex & (sizeof(long) - 1));
				//HM:revisit 
				//assert (highIndex + 1) == (int)(setBitForIndex >>> LOG2_LONG_SIZE);
				highIndex += 1;
				upperLong = efEncoder.upperLongs[highIndex];
				curHighLong = upperLong;
			}
			// curHighLong has enough clear bits to reach highTarget, has at least 1 set bit, and may not have enough set bits.
			int rank = (int)(highTarget - (setBitForIndex - efIndex));
			// the rank of the zero bit for highValue.
			//HM:revisit 
			//assert (rank <= sizeof(long)) : ("rank " + rank);
			if (rank >= 1)
			{
				long invCurHighLong = ~curHighLong;
				int clearBitForValue = (rank <= 8) ? BroadWord.SelectNaive(invCurHighLong, rank) : 
					BroadWord.Select(invCurHighLong, rank);
				//HM:revisit 
				//assert clearBitForValue <= (sizeof(long)-1);
				setBitForIndex += clearBitForValue + 1;
				// the high bit just before setBitForIndex is zero
				int oneBitsBeforeClearBit = clearBitForValue - rank + 1;
				efIndex += oneBitsBeforeClearBit;
				// the high bit at setBitForIndex and belongs to the unary code for efIndex
				if (efIndex >= numEncoded)
				{
					return NO_MORE_VALUES;
				}
				if ((setBitForIndex & (sizeof(long) - 1)) == 0L)
				{
					// exhausted curHighLong
					//HM:revisit 
					//assert (highIndex + 1) == (int)(setBitForIndex >>> LOG2_LONG_SIZE);
					highIndex += 1;
					upperLong = efEncoder.upperLongs[highIndex];
					curHighLong = upperLong;
				}
				else
				{
					//HM:revisit 
					//assert highIndex == (int)(setBitForIndex >>> LOG2_LONG_SIZE);
					curHighLong = (long)(((ulong)upperLong) >> ((int)(setBitForIndex & (sizeof(long) - 1
						))));
				}
				// curHighLong has enough clear bits to reach highTarget, and may not have enough set bits.
				while (curHighLong == 0L)
				{
					setBitForIndex += sizeof(long) - (setBitForIndex & (sizeof(long) - 1));
					//HM:revisit 
					//assert (highIndex + 1) == (int)(setBitForIndex >>> LOG2_LONG_SIZE);
					highIndex += 1;
					upperLong = efEncoder.upperLongs[highIndex];
					curHighLong = upperLong;
				}
			}
			setBitForIndex += long.NumberOfTrailingZeros(curHighLong);
			//HM:revisit 
			//assert (setBitForIndex - efIndex) >= highTarget; // highTarget reached
			// Linear search also with low values
			long currentValue = CombineHighLowValues((setBitForIndex - efIndex), CurrentLowValue
				());
			while (currentValue < target)
			{
				currentValue = NextValue();
				if (currentValue == NO_MORE_VALUES)
				{
					return NO_MORE_VALUES;
				}
			}
			return currentValue;
		}

		/// <summary>Set the decoding index to just after the last encoded value.</summary>
		/// <remarks>Set the decoding index to just after the last encoded value.</remarks>
		public virtual void ToAfterSequence()
		{
			efIndex = numEncoded;
			// just after last index
			setBitForIndex = ((long)(((ulong)efEncoder.lastEncoded) >> efEncoder.numLowBits))
				 + numEncoded;
		}

		/// <returns>the number of bits in a long before (setBitForIndex modulo sizeof(long))</returns>
		private int GetCurrentLeftShift()
		{
			int s = sizeof(long) - 1 - (int)(setBitForIndex & (sizeof(long) - 1));
			return s;
		}

		/// <summary>
		/// Decrement efindex and setBitForIndex and
		/// shift curHighLong so that it does not contain the high bits after setBitForIndex.
		/// </summary>
		/// <remarks>
		/// Decrement efindex and setBitForIndex and
		/// shift curHighLong so that it does not contain the high bits after setBitForIndex.
		/// </remarks>
		/// <returns>true iff efindex still &gt;= 0</returns>
		private bool ToBeforeCurrentHighBit()
		{
			efIndex -= 1;
			if (efIndex < 0)
			{
				return false;
			}
			setBitForIndex -= 1;
			int highIndex = (int)((long)(((ulong)setBitForIndex) >> LOG2_LONG_SIZE));
			curHighLong = efEncoder.upperLongs[highIndex] << GetCurrentLeftShift();
			return true;
		}

		/// <summary>The current high long has been determined to not contain the set bit that is needed.
		/// 	</summary>
		/// <remarks>
		/// The current high long has been determined to not contain the set bit that is needed.
		/// Decrement setBitForIndex to the previous high long and set curHighLong accordingly.
		/// </remarks>
		private void ToPreviousHighLong()
		{
			setBitForIndex -= (setBitForIndex & (sizeof(long) - 1)) + 1;
			//
			//HM:revisit 
			//assert getCurrentLeftShift() == 0;
			int highIndex = (int)((long)(((ulong)setBitForIndex) >> LOG2_LONG_SIZE));
			curHighLong = efEncoder.upperLongs[highIndex];
		}

		/// <summary>
		/// setBitForIndex and efIndex have just been decremented, scan to the previous high set bit
		/// by decrementing setBitForIndex and by setting curHighLong accordingly.
		/// </summary>
		/// <remarks>
		/// setBitForIndex and efIndex have just been decremented, scan to the previous high set bit
		/// by decrementing setBitForIndex and by setting curHighLong accordingly.
		/// </remarks>
		/// <returns>the previous encoded high value.</returns>
		private long PreviousHighValue()
		{
			while (curHighLong == 0L)
			{
				ToPreviousHighLong();
			}
			// inlining and unrolling would simplify somewhat
			setBitForIndex -= long.NumberOfLeadingZeros(curHighLong);
			return CurrentHighValue();
		}

		/// <summary>
		/// If another value is available before the current decoding index, return this value
		/// and decrease the decoding index by 1.
		/// </summary>
		/// <remarks>
		/// If another value is available before the current decoding index, return this value
		/// and decrease the decoding index by 1. Otherwise return
		/// <see cref="NO_MORE_VALUES">NO_MORE_VALUES</see>
		/// .
		/// </remarks>
		public virtual long PreviousValue()
		{
			if (!ToBeforeCurrentHighBit())
			{
				return NO_MORE_VALUES;
			}
			long highValue = PreviousHighValue();
			return CombineHighLowValues(highValue, CurrentLowValue());
		}

		/// <summary>
		/// setBitForIndex and efIndex have just been decremented, scan backward to the high set bit
		/// of at most a given high value
		/// by decrementing setBitForIndex and by setting curHighLong accordingly.
		/// </summary>
		/// <remarks>
		/// setBitForIndex and efIndex have just been decremented, scan backward to the high set bit
		/// of at most a given high value
		/// by decrementing setBitForIndex and by setting curHighLong accordingly.
		/// <br />The current implementation does not use the index on the upper zero bit positions.
		/// </remarks>
		/// <returns>the largest encoded high value that is at most the given one.</returns>
		private long BackToHighValue(long highTarget)
		{
			int curSetBits = long.BitCount(curHighLong);
			// is shifted by getCurrentLeftShift()
			int curClearBits = sizeof(long) - curSetBits - GetCurrentLeftShift();
			while ((CurrentHighValue() - curClearBits) > highTarget)
			{
				// curHighLong has not enough clear bits to reach highTarget
				efIndex -= curSetBits;
				if (efIndex < 0)
				{
					return NO_MORE_VALUES;
				}
				ToPreviousHighLong();
				//
				//HM:revisit 
				//assert getCurrentLeftShift() == 0;
				curSetBits = long.BitCount(curHighLong);
				curClearBits = sizeof(long) - curSetBits;
			}
			// curHighLong has enough clear bits to reach highTarget, but may not have enough set bits.
			long highValue = PreviousHighValue();
			while (highValue > highTarget)
			{
				if (!ToBeforeCurrentHighBit())
				{
					return NO_MORE_VALUES;
				}
				highValue = PreviousHighValue();
			}
			return highValue;
		}

		/// <summary>
		/// Given a target value, go back to the first smaller or equal value
		/// and return it if it is available.
		/// </summary>
		/// <remarks>
		/// Given a target value, go back to the first smaller or equal value
		/// and return it if it is available. Otherwise return
		/// <see cref="NO_MORE_VALUES">NO_MORE_VALUES</see>
		/// .
		/// <br />The current implementation does not use the index on the upper zero bit positions.
		/// </remarks>
		public virtual long BackToValue(long target)
		{
			if (!ToBeforeCurrentHighBit())
			{
				return NO_MORE_VALUES;
			}
			long highTarget = (long)(((ulong)target) >> efEncoder.numLowBits);
			long highValue = BackToHighValue(highTarget);
			if (highValue == NO_MORE_VALUES)
			{
				return NO_MORE_VALUES;
			}
			// Linear search with low values:
			long currentValue = CombineHighLowValues(highValue, CurrentLowValue());
			while (currentValue > target)
			{
				currentValue = PreviousValue();
				if (currentValue == NO_MORE_VALUES)
				{
					return NO_MORE_VALUES;
				}
			}
			return currentValue;
		}
	}
}
