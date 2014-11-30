using System;
using System.Text;
using Lucene.Net.Support;

namespace Lucene.Net.Util.Packed
{
	/// <summary>
	/// Encode a non decreasing sequence of non negative whole numbers in the Elias-Fano encoding
	/// that was introduced in the 1970's by Peter Elias and Robert Fano.
	/// </summary>
	/// <remarks>
	/// Encode a non decreasing sequence of non negative whole numbers in the Elias-Fano encoding
	/// that was introduced in the 1970's by Peter Elias and Robert Fano.
	/// <p>
	/// The Elias-Fano encoding is a high bits / low bits representation of
	/// a monotonically increasing sequence of <code>numValues &gt; 0</code> natural numbers <code>x[i]</code>
	/// <p>
	/// <code>0 <= x[0] &lt;= x[1] &lt;= ... &lt;= x[numValues-2] &lt;= x[numValues-1] &lt;= upperBound&lt;/code>
	/// <p>
	/// where <code>upperBound &gt; 0</code> is an upper bound on the last value.
	/// <br />
	/// The Elias-Fano encoding uses less than half a bit per encoded number more
	/// than the smallest representation
	/// that can encode any monotone sequence with the same bounds.
	/// <p>
	/// The lower <code>L</code> bits of each <code>x[i]</code> are stored explicitly and contiguously
	/// in the lower-bits array, with <code>L</code> chosen as (<code>log()</code> base 2):
	/// <p>
	/// <code>L = max(0, floor(log(upperBound/numValues)))</code>
	/// <p>
	/// The upper bits are stored in the upper-bits array as a sequence of unary-coded gaps (<code>x[-1] = 0</code>):
	/// <p>
	/// <code>(x[i]/2**L) - (x[i-1]/2**L)</code>
	/// <p>
	/// The unary code encodes a natural number <code>n</code> by <code>n</code> 0 bits followed by a 1 bit:
	/// <code>0...01</code>. <br />
	/// In the upper bits the total the number of 1 bits is <code>numValues</code>
	/// and the total number of 0 bits is:<p>
	/// <code>floor(x[numValues-1]/2**L) <= upperBound/(2**max(0, floor(log(upperBound/numValues)))) &lt;= 2*numValues&lt;/code>
	/// <p>
	/// The Elias-Fano encoding uses at most
	/// <p>
	/// <code>2 + ceil(log(upperBound/numValues))</code>
	/// <p>
	/// bits per encoded number. With <code>upperBound</code> in these bounds (<code>p</code> is an integer):
	/// <p>
	/// <code>2**p &lt; x[numValues-1] <= upperBound &lt;= 2**(p+1)&lt;/code>
	/// <p>
	/// the number of bits per encoded number is minimized.
	/// <p>
	/// In this implementation the values in the sequence can be given as <code>long</code>,
	/// <code>numValues = 0</code> and <code>upperBound = 0</code> are allowed,
	/// and each of the upper and lower bit arrays should fit in a <code>long[]</code>.
	/// <br />
	/// An index of positions of zero's in the upper bits is also built.
	/// <p>
	/// This implementation is based on this article:
	/// <br />
	/// Sebastiano Vigna, "Quasi Succinct Indices", June 19, 2012, sections 3, 4 and 9.
	/// Retrieved from http://arxiv.org/pdf/1206.4300 .
	/// <p>The articles originally describing the Elias-Fano representation are:
	/// <br />Peter Elias, "Efficient storage and retrieval by content and address of static files",
	/// J. Assoc. Comput. Mach., 21(2):246â€“260, 1974.
	/// <br />Robert M. Fano, "On the number of bits required to implement an associative memory",
	/// Memorandum 61, Computer Structures Group, Project MAC, MIT, Cambridge, Mass., 1971.
	/// </remarks>
	/// <lucene.internal></lucene.internal>
	public class EliasFanoEncoder
	{
		internal readonly long numValues;

		private readonly long upperBound;

		internal readonly int numLowBits;

		internal readonly long lowerBitsMask;

		internal readonly long[] upperLongs;

		internal readonly long[] lowerLongs;

        private static readonly int LOG2_LONG_SIZE = (sizeof(long) * 8).NumberOfTrailingZeros();

		internal long numEncoded = 0L;

		internal long lastEncoded = 0L;

		/// <summary>The default index interval for zero upper bits.</summary>
		/// <remarks>The default index interval for zero upper bits.</remarks>
		public const long DEFAULT_INDEX_INTERVAL = 256;

		internal readonly long numIndexEntries;

		internal readonly long indexInterval;

		internal readonly int nIndexEntryBits;

		/// <summary>
		/// upperZeroBitPositionIndex[i] (filled using packValue) will contain the bit position
		/// just after the zero bit ((i+1) * indexInterval) in the upper bits.
		/// </summary>
		/// <remarks>
		/// upperZeroBitPositionIndex[i] (filled using packValue) will contain the bit position
		/// just after the zero bit ((i+1) * indexInterval) in the upper bits.
		/// </remarks>
		internal readonly long[] upperZeroBitPositionIndex;

		internal long currentEntryIndex;

		/// <summary>Construct an Elias-Fano encoder.</summary>
		/// <remarks>
		/// Construct an Elias-Fano encoder.
		/// After construction, call
		/// <see cref="EncodeNext(long)">EncodeNext(long)</see>
		/// <code>numValues</code> times to encode
		/// a non decreasing sequence of non negative numbers.
		/// </remarks>
		/// <param name="numValues">The number of values that is to be encoded.</param>
		/// <param name="upperBound">
		/// At least the highest value that will be encoded.
		/// For space efficiency this should not exceed the power of two that equals
		/// or is the first higher than the actual maximum.
		/// <br />When <code>numValues &gt;= (upperBound/3)</code>
		/// a
		/// <see cref="Lucene.Net.Util.FixedBitSet">Lucene.Net.Util.FixedBitSet
		/// 	</see>
		/// will take less space.
		/// </param>
		/// <param name="indexInterval">
		/// The number of high zero bits for which a single index entry is built.
		/// The index will have at most <code>2 * numValues / indexInterval</code> entries
		/// and each index entry will use at most <code>ceil(log2(3 * numValues))</code> bits,
		/// see
		/// <see cref="EliasFanoEncoder">EliasFanoEncoder</see>
		/// .
		/// </param>
		/// <exception cref="System.ArgumentException">
		/// when:
		/// <ul>
		/// <li><code>numValues</code> is negative, or
		/// <li><code>numValues</code> is non negative and <code>upperBound</code> is negative, or
		/// <li>the low bits do not fit in a <code>long[]</code>:
		/// <code>(L * numValues / 64) &gt; Integer.MAX_VALUE</code>, or
		/// <li>the high bits do not fit in a <code>long[]</code>:
		/// <code>(2 * numValues / 64) &gt; Integer.MAX_VALUE</code>, or
		/// <li><code>indexInterval &lt; 2</code>,
		/// <li>the index bits do not fit in a <code>long[]</code>:
		/// <code>(numValues / indexInterval * ceil(2log(3 * numValues)) / 64) &gt; Integer.MAX_VALUE</code>.
		/// </ul>
		/// </exception>
		public EliasFanoEncoder(long numValues, long upperBound, long indexInterval)
		{
			// for javadocs
			// also indicates how many entries in the index are valid.
			if (numValues < 0L)
			{
				throw new ArgumentException("numValues should not be negative: " + numValues);
			}
			this.numValues = numValues;
			if ((numValues > 0L) && (upperBound < 0L))
			{
				throw new ArgumentException("upperBound should not be negative: " + upperBound + 
					" when numValues > 0");
			}
			this.upperBound = numValues > 0 ? upperBound : -1L;
			// if there is no value, -1 is the best upper bound
			int nLowBits = 0;
			if (this.numValues > 0)
			{
				// nLowBits = max(0; floor(2log(upperBound/numValues)))
				long lowBitsFac = this.upperBound / this.numValues;
				if (lowBitsFac > 0)
				{
                    nLowBits = 63 - lowBitsFac.NumberOfLeadingZeros();
				}
			}
			// see Long.numberOfLeadingZeros javadocs
			this.numLowBits = nLowBits;
			this.lowerBitsMask = (long)(((ulong)long.MaxValue) >> ((sizeof(long) * 8) - 1 - this.numLowBits
				));
			long numLongsForLowBits = NumLongsForBits(numValues * numLowBits);
			if (numLongsForLowBits > int.MaxValue)
			{
				throw new ArgumentException("numLongsForLowBits too large to index a long array: "
					 + numLongsForLowBits);
			}
			this.lowerLongs = new long[(int)numLongsForLowBits];
			long numHighBitsClear = (long)(((ulong)((this.upperBound > 0) ? this.upperBound : 
				0)) >> this.numLowBits);
			//HM:revisit 
			//assert numHighBitsClear <= (2 * this.numValues);
			long numHighBitsSet = this.numValues;
			long numLongsForHighBits = NumLongsForBits(numHighBitsClear + numHighBitsSet);
			if (numLongsForHighBits > int.MaxValue)
			{
				throw new ArgumentException("numLongsForHighBits too large to index a long array: "
					 + numLongsForHighBits);
			}
			this.upperLongs = new long[(int)numLongsForHighBits];
			if (indexInterval < 2)
			{
				throw new ArgumentException("indexInterval should at least 2: " + indexInterval);
			}
			// For the index:
			long maxHighValue = (long)(((ulong)upperBound) >> this.numLowBits);
			long nIndexEntries = maxHighValue / indexInterval;
			// no zero value index entry
			this.numIndexEntries = (nIndexEntries >= 0) ? nIndexEntries : 0;
			long maxIndexEntry = maxHighValue + numValues - 1;
			// clear upper bits, set upper bits, start at zero
            this.nIndexEntryBits = (maxIndexEntry <= 0) ? 0 : (64 - maxIndexEntry.NumberOfLeadingZeros());
			long numLongsForIndexBits = NumLongsForBits(numIndexEntries * nIndexEntryBits);
			if (numLongsForIndexBits > int.MaxValue)
			{
				throw new ArgumentException("numLongsForIndexBits too large to index a long array: "
					 + numLongsForIndexBits);
			}
			this.upperZeroBitPositionIndex = new long[(int)numLongsForIndexBits];
			this.currentEntryIndex = 0;
			this.indexInterval = indexInterval;
		}

		/// <summary>
		/// Construct an Elias-Fano encoder using
		/// <see cref="DEFAULT_INDEX_INTERVAL">DEFAULT_INDEX_INTERVAL</see>
		/// .
		/// </summary>
		public EliasFanoEncoder(long numValues, long upperBound) : this(numValues, upperBound
			, DEFAULT_INDEX_INTERVAL)
		{
		}

		private static long NumLongsForBits(long numBits)
		{
			// Note: int version in FixedBitSet.bits2words()
			//HM:revisit 
			//assert numBits >= 0 : numBits;
			return (long)(((ulong)(numBits + ((sizeof(long) * 8) - 1))) >> LOG2_LONG_SIZE);
		}

		/// <summary>Call at most <code>numValues</code> times to encode a non decreasing sequence of non negative numbers.
		/// 	</summary>
		/// <remarks>Call at most <code>numValues</code> times to encode a non decreasing sequence of non negative numbers.
		/// 	</remarks>
		/// <param name="x">The next number to be encoded.</param>
		/// <exception cref="System.InvalidOperationException">when called more than <code>numValues</code> times.
		/// 	</exception>
		/// <exception cref="System.ArgumentException">
		/// when:
		/// <ul>
		/// <li><code>x</code> is smaller than an earlier encoded value, or
		/// <li><code>x</code> is larger than <code>upperBound</code>.
		/// </ul>
		/// </exception>
		public virtual void EncodeNext(long x)
		{
			if (numEncoded >= numValues)
			{
				throw new InvalidOperationException("encodeNext called more than " + numValues + 
					" times.");
			}
			if (lastEncoded > x)
			{
				throw new ArgumentException(x + " smaller than previous " + lastEncoded);
			}
			if (x > upperBound)
			{
				throw new ArgumentException(x + " larger than upperBound " + upperBound);
			}
			long highValue = (long)(((ulong)x) >> numLowBits);
			EncodeUpperBits(highValue);
			EncodeLowerBits(x & lowerBitsMask);
			lastEncoded = x;
			// Add index entries:
			long indexValue = (currentEntryIndex + 1) * indexInterval;
			while (indexValue <= highValue)
			{
				long afterZeroBitPosition = indexValue + numEncoded;
				PackValue(afterZeroBitPosition, upperZeroBitPositionIndex, nIndexEntryBits, currentEntryIndex
					);
				currentEntryIndex += 1;
				indexValue += indexInterval;
			}
			numEncoded++;
		}

		private void EncodeUpperBits(long highValue)
		{
			long nextHighBitNum = numEncoded + highValue;
			// sequence of unary gaps
			upperLongs[(int)((long)(((ulong)nextHighBitNum) >> LOG2_LONG_SIZE))] |= (1L << (int)(nextHighBitNum & ((sizeof(long) * 8) - 1)));
		}

		private void EncodeLowerBits(long lowValue)
		{
			PackValue(lowValue, lowerLongs, numLowBits, numEncoded);
		}

		private static void PackValue(long value, long[] longArray, int numBits, long packIndex
			)
		{
			if (numBits != 0)
			{
				long bitPos = numBits * packIndex;
				int index = (int)((long)(((ulong)bitPos) >> LOG2_LONG_SIZE));
				int bitPosAtIndex = (int)(bitPos & ((sizeof(long) * 8) - 1));
				longArray[index] |= (value << bitPosAtIndex);
				if ((bitPosAtIndex + numBits) > (sizeof(long) * 8))
				{
					longArray[index + 1] = ((long)(((ulong)value) >> ((sizeof(long) * 8) - bitPosAtIndex)));
				}
			}
		}

		/// <summary>
		/// Provide an indication that it is better to use an
		/// <see cref="EliasFanoEncoder">EliasFanoEncoder</see>
		/// than a
		/// <see cref="Lucene.Net.Util.FixedBitSet">Lucene.Net.Util.FixedBitSet
		/// 	</see>
		/// to encode document identifiers.
		/// This indication is not precise and may change in the future.
		/// <br />An EliasFanoEncoder is favoured when the size of the encoding by the EliasFanoEncoder
		/// (including some space for its index) is at most about 5/6 of the size of the FixedBitSet,
		/// this is the same as comparing estimates of the number of bits accessed by a pair of FixedBitSets and
		/// by a pair of non indexed EliasFanoDocIdSets when determining the intersections of the pairs.
		/// <br />A bit set is preferred when <code>upperbound <= 256&lt;/code>.
		/// <br />It is assumed that
		/// <see cref="DEFAULT_INDEX_INTERVAL">DEFAULT_INDEX_INTERVAL</see>
		/// is used.
		/// </summary>
		/// <param name="numValues">The number of document identifiers that is to be encoded. Should be non negative.
		/// 	</param>
		/// <param name="upperBound">The maximum possible value for a document identifier. Should be at least <code>numValues</code>.
		/// 	</param>
		public static bool SufficientlySmallerThanBitSet(long numValues, long upperBound)
		{
			return (upperBound > (4 * (sizeof(long) * 8))) && (upperBound / 7) > numValues;
		}

		// prefer a bit set when it takes no more than 4 longs.
		// 6 + 1 to allow some room for the index.
		/// <summary>
		/// Returns an
		/// <see cref="EliasFanoDecoder">EliasFanoDecoder</see>
		/// to access the encoded values.
		/// Perform all calls to
		/// <see cref="EncodeNext(long)">EncodeNext(long)</see>
		/// before calling
		/// <see cref="GetDecoder()">GetDecoder()</see>
		/// .
		/// </summary>
		public virtual EliasFanoDecoder GetDecoder()
		{
			// decode as far as currently encoded as determined by numEncoded.
			return new EliasFanoDecoder(this);
		}

		/// <summary>Expert.</summary>
		/// <remarks>Expert. The low bits.</remarks>
		public virtual long[] GetLowerBits()
		{
			return lowerLongs;
		}

		/// <summary>Expert.</summary>
		/// <remarks>Expert. The high bits.</remarks>
		public virtual long[] GetUpperBits()
		{
			return upperLongs;
		}

		/// <summary>Expert.</summary>
		/// <remarks>Expert. The index bits.</remarks>
		public virtual long[] GetIndexBits()
		{
			return upperZeroBitPositionIndex;
		}

		public override string ToString()
		{
			StringBuilder s = new StringBuilder("EliasFanoSequence");
			s.Append(" numValues " + numValues);
			s.Append(" numEncoded " + numEncoded);
			s.Append(" upperBound " + upperBound);
			s.Append(" lastEncoded " + lastEncoded);
			s.Append(" numLowBits " + numLowBits);
			s.Append("\nupperLongs[" + upperLongs.Length + "]");
			for (int i = 0; i < upperLongs.Length; i++)
			{
				s.Append(" " + ToStringUtils.LongHex(upperLongs[i]));
			}
			s.Append("\nlowerLongs[" + lowerLongs.Length + "]");
			for (int i_1 = 0; i_1 < lowerLongs.Length; i_1++)
			{
				s.Append(" " + ToStringUtils.LongHex(lowerLongs[i_1]));
			}
			s.Append("\nindexInterval: " + indexInterval + ", nIndexEntryBits: " + nIndexEntryBits
				);
			s.Append("\nupperZeroBitPositionIndex[" + upperZeroBitPositionIndex.Length + "]");
			for (int i_2 = 0; i_2 < upperZeroBitPositionIndex.Length; i_2++)
			{
				s.Append(" " + ToStringUtils.LongHex(upperZeroBitPositionIndex[i_2]));
			}
			return s.ToString();
		}

		public override bool Equals(object other)
		{
			if (!(other is Lucene.Net.Util.Packed.EliasFanoEncoder))
			{
				return false;
			}
			Lucene.Net.Util.Packed.EliasFanoEncoder oefs = (Lucene.Net.Util.Packed.EliasFanoEncoder
				)other;
			// no equality needed for upperBound
			return (this.numValues == oefs.numValues) && (this.numEncoded == oefs.numEncoded)
				 && (this.numLowBits == oefs.numLowBits) && (this.numIndexEntries == oefs.numIndexEntries
				) && (this.indexInterval == oefs.indexInterval) && Arrays.Equals(this.upperLongs
				, oefs.upperLongs) && Arrays.Equals(this.lowerLongs, oefs.lowerLongs);
		}

		// no need to check index content
		public override int GetHashCode()
		{
			int h = ((int)(31 * (numValues + 7 * (numEncoded + 5 * (numLowBits + 3 * (numIndexEntries
				 + 11 * indexInterval)))))) ^ Arrays.HashCode(upperLongs) ^ Arrays.HashCode(lowerLongs
				);
			return h;
		}
	}
}
