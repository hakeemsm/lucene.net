using System;
using Lucene.Net.Support;

namespace Lucene.Net.Util
{
	/// <summary>
	/// BitSet of fixed length (numBits), backed by accessible (
	/// <see cref="GetBits()">GetBits()</see>
	/// )
	/// long[], accessed with a long index. Use it only if you intend to store more
	/// than 2.1B bits, otherwise you should use
	/// <see cref="FixedBitSet">FixedBitSet</see>
	/// .
	/// </summary>
	/// <lucene.internal></lucene.internal>
	public sealed class LongBitSet
	{
		private readonly long[] bits;

		private readonly long numBits;

		private readonly int numWords;

		/// <summary>
		/// If the given
		/// <see cref="LongBitSet">LongBitSet</see>
		/// is large enough to hold
		/// <code>numBits</code>
		/// , returns the given bits, otherwise returns a new
		/// <see cref="LongBitSet">LongBitSet</see>
		/// which can hold the requested number of bits.
		/// <p>
		/// <b>NOTE:</b> the returned bitset reuses the underlying
		/// <code>long[]</code>
		/// of
		/// the given
		/// <code>bits</code>
		/// if possible. Also, calling
		/// <see cref="Length()">Length()</see>
		/// on the
		/// returned bits may return a value greater than
		/// <code>numBits</code>
		/// .
		/// </summary>
		public static LongBitSet EnsureCapacity(LongBitSet bits, long numBits)
		{
			if (numBits < bits.Length())
			{
				return bits;
			}
		    int numWords = Bits2words(numBits);
		    long[] arr = bits.GetBits();
		    if (numWords >= arr.Length)
		    {
		        arr = ArrayUtil.Grow(arr, numWords + 1);
		    }
		    return new LongBitSet(arr, arr.Length << 6);
		}

		/// <summary>returns the number of 64 bit words it would take to hold numBits</summary>
		public static int Bits2words(long numBits)
		{
			int numLong = (int)((long)(((ulong)numBits) >> 6));
			if ((numBits & 63) != 0)
			{
				numLong++;
			}
			return numLong;
		}

		public LongBitSet(long numBits)
		{
			this.numBits = numBits;
			bits = new long[Bits2words(numBits)];
			numWords = bits.Length;
		}

		public LongBitSet(long[] storedBits, long numBits)
		{
			this.numWords = Bits2words(numBits);
			if (numWords > storedBits.Length)
			{
				throw new ArgumentException("The given long array is too small  to hold " + numBits
					 + " bits");
			}
			this.numBits = numBits;
			this.bits = storedBits;
		}

		/// <summary>Returns the number of bits stored in this bitset.</summary>
		/// <remarks>Returns the number of bits stored in this bitset.</remarks>
		public long Length()
		{
			return numBits;
		}

		/// <summary>Expert.</summary>
		/// <remarks>Expert.</remarks>
		public long[] GetBits()
		{
			return bits;
		}

		/// <summary>Returns number of set bits.</summary>
		/// <remarks>
		/// Returns number of set bits.  NOTE: this visits every
		/// long in the backing bits array, and the result is not
		/// internally cached!
		/// </remarks>
		public long Cardinality()
		{
			return BitUtil.Pop_array(bits, 0, bits.Length);
		}

		public bool Get(long index)
		{
			 
			//assert index >= 0 && index < numBits: "index=" + index;
			int i = (int)(index >> 6);
			// div 64
			// signed shift will keep a negative index and force an
			// array-index-out-of-bounds-exception, removing the need for an explicit check.
			int bit = (int)(index & unchecked((int)(0x3f)));
			// mod 64
			long bitmask = 1L << bit;
			return (bits[i] & bitmask) != 0;
		}

		public void Set(long index)
		{
			
			//assert index >= 0 && index < numBits: "index=" + index + " numBits=" + numBits;
			int wordNum = (int)(index >> 6);
			// div 64
			int bit = (int)(index & unchecked((int)(0x3f)));
			// mod 64
			long bitmask = 1L << bit;
			bits[wordNum] |= bitmask;
		}

		public bool GetAndSet(long index)
		{
			//HM:revisit 
			//assert index >= 0 && index < numBits;
			int wordNum = (int)(index >> 6);
			// div 64
			int bit = (int)(index & unchecked((int)(0x3f)));
			// mod 64
			long bitmask = 1L << bit;
			bool val = (bits[wordNum] & bitmask) != 0;
			bits[wordNum] |= bitmask;
			return val;
		}

		public void Clear(long index)
		{
			//HM:revisit 
			//assert index >= 0 && index < numBits;
			int wordNum = (int)(index >> 6);
			int bit = (int)(index & unchecked((int)(0x03f)));
			long bitmask = 1L << bit;
			bits[wordNum] &= ~bitmask;
		}

		public bool GetAndClear(long index)
		{
			//HM:revisit 
			//assert index >= 0 && index < numBits;
			int wordNum = (int)(index >> 6);
			// div 64
			int bit = (int)(index & unchecked((int)(0x3f)));
			// mod 64
			long bitmask = 1L << bit;
			bool val = (bits[wordNum] & bitmask) != 0;
			bits[wordNum] &= ~bitmask;
			return val;
		}

		/// <summary>Returns the index of the first set bit starting at the index specified.</summary>
		/// <remarks>
		/// Returns the index of the first set bit starting at the index specified.
		/// -1 is returned if there are no more set bits.
		/// </remarks>
		public long NextSetBit(long index)
		{
			//HM:revisit 
			//assert index >= 0 && index < numBits;
			int i = (int)(index >> 6);
			int subIndex = (int)(index & unchecked((int)(0x3f)));
			// index within the word
			long word = bits[i] >> subIndex;
			// skip all the bits to the right of index
			if (word != 0)
			{
				return index + word.NumberOfTrailingZeros();
			}
			while (++i < numWords)
			{
				word = bits[i];
				if (word != 0)
				{
					return (i << 6) + word.NumberOfTrailingZeros();
				}
			}
			return -1;
		}

		/// <summary>Returns the index of the last set bit before or on the index specified.</summary>
		/// <remarks>
		/// Returns the index of the last set bit before or on the index specified.
		/// -1 is returned if there are no more set bits.
		/// </remarks>
		public long PrevSetBit(long index)
		{
			//HM:revisit 
			//assert index >= 0 && index < numBits: "index=" + index + " numBits=" + numBits;
			int i = (int)(index >> 6);
			int subIndex = (int)(index & unchecked((int)(0x3f)));
			// index within the word
			long word = (bits[i] << (63 - subIndex));
			// skip all the bits to the left of index
			if (word != 0)
			{
				return (i << 6) + subIndex - word.NumberOfLeadingZeros();
			}
			// See LUCENE-3197
			while (--i >= 0)
			{
				word = bits[i];
				if (word != 0)
				{
					return (i << 6) + 63 - word.NumberOfLeadingZeros();
				}
			}
			return -1;
		}

		/// <summary>this = this OR other</summary>
		public void Or(Lucene.Net.Util.LongBitSet other)
		{
			//HM:revisit 
			//assert other.numWords <= numWords : "numWords=" + numWords + ", other.numWords=" + other.numWords;
			int pos = Math.Min(numWords, other.numWords);
			while (--pos >= 0)
			{
				bits[pos] |= other.bits[pos];
			}
		}

		/// <summary>this = this XOR other</summary>
		public void Xor(Lucene.Net.Util.LongBitSet other)
		{
			//HM:revisit 
			//assert other.numWords <= numWords : "numWords=" + numWords + ", other.numWords=" + other.numWords;
			int pos = Math.Min(numWords, other.numWords);
			while (--pos >= 0)
			{
				bits[pos] ^= other.bits[pos];
			}
		}

		/// <summary>returns true if the sets have any elements in common</summary>
		public bool Intersects(Lucene.Net.Util.LongBitSet other)
		{
			int pos = Math.Min(numWords, other.numWords);
			while (--pos >= 0)
			{
				if ((bits[pos] & other.bits[pos]) != 0)
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>this = this AND other</summary>
		public void And(Lucene.Net.Util.LongBitSet other)
		{
			int pos = Math.Min(numWords, other.numWords);
			while (--pos >= 0)
			{
				bits[pos] &= other.bits[pos];
			}
			if (numWords > other.numWords)
			{
				Arrays.Fill(bits, other.numWords, numWords, 0L);
			}
		}

		/// <summary>this = this AND NOT other</summary>
		public void AndNot(Lucene.Net.Util.LongBitSet other)
		{
			int pos = Math.Min(numWords, other.bits.Length);
			while (--pos >= 0)
			{
				bits[pos] &= ~other.bits[pos];
			}
		}

		// NOTE: no .isEmpty() here because that's trappy (ie,
		// typically isEmpty is low cost, but this one wouldn't
		// be)
		/// <summary>Flips a range of bits</summary>
		/// <param name="startIndex">lower index</param>
		/// <param name="endIndex">one-past the last bit to flip</param>
		public void Flip(long startIndex, long endIndex)
		{
			//HM:revisit 
			//assert startIndex >= 0 && startIndex < numBits;
			//HM:revisit 
			//assert endIndex >= 0 && endIndex <= numBits;
			if (endIndex <= startIndex)
			{
				return;
			}
			int startWord = (int)(startIndex >> 6);
			int endWord = (int)((endIndex - 1) >> 6);
			long startmask = -1L << (int)startIndex;
			long endmask = (long)(unchecked ((ulong)-1L) >> (int)-endIndex);
			// 64-(endIndex&0x3f) is the same as -endIndex due to wrap
			if (startWord == endWord)
			{
				bits[startWord] ^= (startmask & endmask);
				return;
			}
			bits[startWord] ^= startmask;
			for (int i = startWord + 1; i < endWord; i++)
			{
				bits[i] = ~bits[i];
			}
			bits[endWord] ^= endmask;
		}

		/// <summary>Sets a range of bits</summary>
		/// <param name="startIndex">lower index</param>
		/// <param name="endIndex">one-past the last bit to set</param>
		public void Set(long startIndex, long endIndex)
		{
			//HM:revisit 
			//assert startIndex >= 0 && startIndex < numBits;
			//HM:revisit 
			//assert endIndex >= 0 && endIndex <= numBits;
			if (endIndex <= startIndex)
			{
				return;
			}
			int startWord = (int)(startIndex >> 6);
			int endWord = (int)((endIndex - 1) >> 6);
			long startmask = -1L << (int)startIndex;
			long endmask = (long)(unchecked ((ulong)-1L) >> (int)-endIndex);
			// 64-(endIndex&0x3f) is the same as -endIndex due to wrap
			if (startWord == endWord)
			{
				bits[startWord] |= (startmask & endmask);
				return;
			}
			bits[startWord] |= startmask;
			Arrays.Fill(bits, startWord + 1, endWord, -1L);
			bits[endWord] |= endmask;
		}

		/// <summary>Clears a range of bits.</summary>
		/// <remarks>Clears a range of bits.</remarks>
		/// <param name="startIndex">lower index</param>
		/// <param name="endIndex">one-past the last bit to clear</param>
		public void Clear(long startIndex, long endIndex)
		{
			//HM:revisit 
			//assert startIndex >= 0 && startIndex < numBits;
			//HM:revisit 
			//assert endIndex >= 0 && endIndex <= numBits;
			if (endIndex <= startIndex)
			{
				return;
			}
			int startWord = (int)(startIndex >> 6);
			int endWord = (int)((endIndex - 1) >> 6);
			long startmask = -1L << (int)startIndex;
			long endmask = (long)(unchecked ((ulong)-1L) >> (int)-endIndex);
			// 64-(endIndex&0x3f) is the same as -endIndex due to wrap
			// invert masks since we are clearing
			startmask = ~startmask;
			endmask = ~endmask;
			if (startWord == endWord)
			{
				bits[startWord] &= (startmask | endmask);
				return;
			}
			bits[startWord] &= startmask;
			Arrays.Fill(bits, startWord + 1, endWord, 0L);
			bits[endWord] &= endmask;
		}

		public LongBitSet Clone()
		{
			long[] bits = new long[this.bits.Length];
			Array.Copy(this.bits, 0, bits, 0, bits.Length);
			return new LongBitSet(bits, numBits);
		}

		/// <summary>returns true if both sets have the same bits set</summary>
		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			if (!(o is LongBitSet))
			{
				return false;
			}
			var other = (LongBitSet)o;
			if (numBits != other.Length())
			{
				return false;
			}
			return Arrays.Equals(bits, other.bits);
		}

		public override int GetHashCode()
		{
			long h = 0;
			for (int i = numWords; --i >= 0; )
			{
				h ^= bits[i];
				h = (h << 1) | ((long)(((ulong)h) >> 63));
			}
			// rotate left
			// fold leftmost bits into right and add a constant to prevent
			// empty sets from returning 0, which is too common.
			return (int)((h >> 32) ^ h) + unchecked((int)(0x98761234));
		}
	}
}
