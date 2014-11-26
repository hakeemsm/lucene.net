/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Codecs.Bloom;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Codecs.Bloom
{
	/// <summary>
	/// <p>
	/// A class used to represent a set of many, potentially large, values (e.g.
	/// </summary>
	/// <remarks>
	/// <p>
	/// A class used to represent a set of many, potentially large, values (e.g. many
	/// long strings such as URLs), using a significantly smaller amount of memory.
	/// </p>
	/// <p>
	/// The set is "lossy" in that it cannot definitively state that is does contain
	/// a value but it <em>can</em> definitively say if a value is <em>not</em> in
	/// the set. It can therefore be used as a Bloom Filter.
	/// </p>
	/// Another application of the set is that it can be used to perform fuzzy counting because
	/// it can estimate reasonably accurately how many unique values are contained in the set.
	/// </p>
	/// <p>This class is NOT threadsafe.</p>
	/// <p>
	/// Internally a Bitset is used to record values and once a client has finished recording
	/// a stream of values the
	/// <see cref="Downsize(float)">Downsize(float)</see>
	/// method can be used to create a suitably smaller set that
	/// is sized appropriately for the number of values recorded and desired saturation levels.
	/// </p>
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public class FuzzySet
	{
		public const int VERSION_SPI = 1;

		public const int VERSION_START = VERSION_SPI;

		public const int VERSION_CURRENT = 2;

		// HashFunction used to be loaded through a SPI
		public static HashFunction HashFunctionForVersion(int version)
		{
			if (version < VERSION_START)
			{
				throw new ArgumentException("Version " + version + " is too old, expected at least "
					 + VERSION_START);
			}
			else
			{
				if (version > VERSION_CURRENT)
				{
					throw new ArgumentException("Version " + version + " is too new, expected at most "
						 + VERSION_CURRENT);
				}
			}
			return MurmurHash2.INSTANCE;
		}

		/// <summary>
		/// Result from
		/// <see cref="Contains(BytesRef)">Contains(BytesRef)</see>
		/// :
		/// can never return definitively YES (always MAYBE),
		/// but can sometimes definitely return NO.
		/// </summary>
		public enum ContainsResult
		{
			MAYBE,
			NO
		}

		private HashFunction hashFunction;

		private FixedBitSet filter;

		private int bloomSize;

		internal static readonly int usableBitSetSizes;

		static FuzzySet()
		{
			//The sizes of BitSet used are all numbers that, when expressed in binary form,
			//are all ones. This is to enable fast downsizing from one bitset to another
			//by simply ANDing each set index in one bitset with the size of the target bitset
			// - this provides a fast modulo of the number. Values previously accumulated in
			// a large bitset and then mapped to a smaller set can be looked up using a single
			// AND operation of the query term's hash rather than needing to perform a 2-step
			// translation of the query term that mirrors the stored content's reprojections.
			usableBitSetSizes = new int[30];
			int mask = 1;
			int size = mask;
			for (int i = 0; i < usableBitSetSizes.Length; i++)
			{
				size = (size << 1) | mask;
				usableBitSetSizes[i] = size;
			}
		}

		/// <summary>
		/// Rounds down required maxNumberOfBits to the nearest number that is made up
		/// of all ones as a binary number.
		/// </summary>
		/// <remarks>
		/// Rounds down required maxNumberOfBits to the nearest number that is made up
		/// of all ones as a binary number.
		/// Use this method where controlling memory use is paramount.
		/// </remarks>
		public static int GetNearestSetSize(int maxNumberOfBits)
		{
			int result = usableBitSetSizes[0];
			for (int i = 0; i < usableBitSetSizes.Length; i++)
			{
				if (usableBitSetSizes[i] <= maxNumberOfBits)
				{
					result = usableBitSetSizes[i];
				}
			}
			return result;
		}

		/// <summary>
		/// Use this method to choose a set size where accuracy (low content saturation) is more important
		/// than deciding how much memory to throw at the problem.
		/// </summary>
		/// <remarks>
		/// Use this method to choose a set size where accuracy (low content saturation) is more important
		/// than deciding how much memory to throw at the problem.
		/// </remarks>
		/// <param name="desiredSaturation">A number between 0 and 1 expressing the % of bits set once all values have been recorded
		/// 	</param>
		/// <returns>The size of the set nearest to the required size</returns>
		public static int GetNearestSetSize(int maxNumberOfValuesExpected, float desiredSaturation
			)
		{
			// Iterate around the various scales of bitset from smallest to largest looking for the first that
			// satisfies value volumes at the chosen saturation level
			for (int i = 0; i < usableBitSetSizes.Length; i++)
			{
				int numSetBitsAtDesiredSaturation = (int)(usableBitSetSizes[i] * desiredSaturation
					);
				int estimatedNumUniqueValues = GetEstimatedNumberUniqueValuesAllowingForCollisions
					(usableBitSetSizes[i], numSetBitsAtDesiredSaturation);
				if (estimatedNumUniqueValues > maxNumberOfValuesExpected)
				{
					return usableBitSetSizes[i];
				}
			}
			return -1;
		}

		public static Lucene.Net.Codecs.Bloom.FuzzySet CreateSetBasedOnMaxMemory(int
			 maxNumBytes)
		{
			int setSize = GetNearestSetSize(maxNumBytes);
			return new Lucene.Net.Codecs.Bloom.FuzzySet(new FixedBitSet(setSize + 1), 
				setSize, HashFunctionForVersion(VERSION_CURRENT));
		}

		public static Lucene.Net.Codecs.Bloom.FuzzySet CreateSetBasedOnQuality(int
			 maxNumUniqueValues, float desiredMaxSaturation)
		{
			int setSize = GetNearestSetSize(maxNumUniqueValues, desiredMaxSaturation);
			return new Lucene.Net.Codecs.Bloom.FuzzySet(new FixedBitSet(setSize + 1), 
				setSize, HashFunctionForVersion(VERSION_CURRENT));
		}

		private FuzzySet(FixedBitSet filter, int bloomSize, HashFunction hashFunction) : 
			base()
		{
			this.filter = filter;
			this.bloomSize = bloomSize;
			this.hashFunction = hashFunction;
		}

		/// <summary>The main method required for a Bloom filter which, given a value determines set membership.
		/// 	</summary>
		/// <remarks>
		/// The main method required for a Bloom filter which, given a value determines set membership.
		/// Unlike a conventional set, the fuzzy set returns NO or MAYBE rather than true or false.
		/// </remarks>
		/// <returns>NO or MAYBE</returns>
		public virtual FuzzySet.ContainsResult Contains(BytesRef value)
		{
			int hash = hashFunction.Hash(value);
			if (hash < 0)
			{
				hash = hash * -1;
			}
			return MayContainValue(hash);
		}

		/// <summary>
		/// Serializes the data set to file using the following format:
		/// <ul>
		/// <li>FuzzySet --&gt;FuzzySetVersion,HashFunctionName,BloomSize,
		/// NumBitSetWords,BitSetWord<sup>NumBitSetWords</sup></li>
		/// <li>HashFunctionName --&gt;
		/// <see cref="Lucene.Net.Store.DataOutput.WriteString(string)">String</see>
		/// The
		/// name of a ServiceProvider registered
		/// <see cref="HashFunction">HashFunction</see>
		/// </li>
		/// <li>FuzzySetVersion --&gt;
		/// <see cref="Lucene.Net.Store.DataOutput.WriteInt(int)">Uint32</see>
		/// The version number of the
		/// <see cref="FuzzySet">FuzzySet</see>
		/// class</li>
		/// <li>BloomSize --&gt;
		/// <see cref="Lucene.Net.Store.DataOutput.WriteInt(int)">Uint32</see>
		/// The modulo value used
		/// to project hashes into the field's Bitset</li>
		/// <li>NumBitSetWords --&gt;
		/// <see cref="Lucene.Net.Store.DataOutput.WriteInt(int)">Uint32</see>
		/// The number of
		/// longs (as returned from
		/// <see cref="Lucene.Net.Util.FixedBitSet.GetBits()">Lucene.Net.Util.FixedBitSet.GetBits()
		/// 	</see>
		/// )</li>
		/// <li>BitSetWord --&gt;
		/// <see cref="Lucene.Net.Store.DataOutput.WriteLong(long)">Long</see>
		/// A long from the array
		/// returned by
		/// <see cref="Lucene.Net.Util.FixedBitSet.GetBits()">Lucene.Net.Util.FixedBitSet.GetBits()
		/// 	</see>
		/// </li>
		/// </ul>
		/// </summary>
		/// <param name="out">Data output stream</param>
		/// <exception cref="System.IO.IOException">If there is a low-level I/O error</exception>
		public virtual void Serialize(DataOutput @out)
		{
			@out.WriteInt(VERSION_CURRENT);
			@out.WriteInt(bloomSize);
			long[] bits = filter.GetBits();
			@out.WriteInt(bits.Length);
			for (int i = 0; i < bits.Length; i++)
			{
				// Can't used VLong encoding because cant cope with negative numbers
				// output by FixedBitSet
				@out.WriteLong(bits[i]);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static Lucene.Net.Codecs.Bloom.FuzzySet Deserialize(DataInput @in)
		{
			int version = @in.ReadInt();
			if (version == VERSION_SPI)
			{
				@in.ReadString();
			}
			HashFunction hashFunction = HashFunctionForVersion(version);
			int bloomSize = @in.ReadInt();
			int numLongs = @in.ReadInt();
			long[] longs = new long[numLongs];
			for (int i = 0; i < numLongs; i++)
			{
				longs[i] = @in.ReadLong();
			}
			FixedBitSet bits = new FixedBitSet(longs, bloomSize + 1);
			return new Lucene.Net.Codecs.Bloom.FuzzySet(bits, bloomSize, hashFunction);
		}

		private FuzzySet.ContainsResult MayContainValue(int positiveHash)
		{
			//HM:revisit 
			//assert positiveHash >= 0;
			// Bloom sizes are always base 2 and so can be ANDed for a fast modulo
			int pos = positiveHash & bloomSize;
			if (filter.Get(pos))
			{
				// This term may be recorded in this index (but could be a collision)
				return FuzzySet.ContainsResult.MAYBE;
			}
			// definitely NOT in this segment
			return FuzzySet.ContainsResult.NO;
		}

		/// <summary>Records a value in the set.</summary>
		/// <remarks>
		/// Records a value in the set. The referenced bytes are hashed and then modulo n'd where n is the
		/// chosen size of the internal bitset.
		/// </remarks>
		/// <param name="value">the key value to be hashed</param>
		/// <exception cref="System.IO.IOException">If there is a low-level I/O error</exception>
		public virtual void AddValue(BytesRef value)
		{
			int hash = hashFunction.Hash(value);
			if (hash < 0)
			{
				hash = hash * -1;
			}
			// Bitmasking using bloomSize is effectively a modulo operation.
			int bloomPos = hash & bloomSize;
			filter.Set(bloomPos);
		}

		/// <param name="targetMaxSaturation">
		/// A number between 0 and 1 describing the % of bits that would ideally be set in the
		/// result. Lower values have better accuracy but require more space.
		/// </param>
		/// <returns>a smaller FuzzySet or null if the current set is already over-saturated</returns>
		public virtual Lucene.Net.Codecs.Bloom.FuzzySet Downsize(float targetMaxSaturation
			)
		{
			int numBitsSet = filter.Cardinality();
			FixedBitSet rightSizedBitSet = filter;
			int rightSizedBitSetSize = bloomSize;
			//Hopefully find a smaller size bitset into which we can project accumulated values while maintaining desired saturation level
			for (int i = 0; i < usableBitSetSizes.Length; i++)
			{
				int candidateBitsetSize = usableBitSetSizes[i];
				float candidateSaturation = (float)numBitsSet / (float)candidateBitsetSize;
				if (candidateSaturation <= targetMaxSaturation)
				{
					rightSizedBitSetSize = candidateBitsetSize;
					break;
				}
			}
			// Re-project the numbers to a smaller space if necessary
			if (rightSizedBitSetSize < bloomSize)
			{
				// Reset the choice of bitset to the smaller version
				rightSizedBitSet = new FixedBitSet(rightSizedBitSetSize + 1);
				// Map across the bits from the large set to the smaller one
				int bitIndex = 0;
				do
				{
					bitIndex = filter.NextSetBit(bitIndex);
					if (bitIndex >= 0)
					{
						// Project the larger number into a smaller one effectively
						// modulo-ing by using the target bitset size as a mask
						int downSizedBitIndex = bitIndex & rightSizedBitSetSize;
						rightSizedBitSet.Set(downSizedBitIndex);
						bitIndex++;
					}
				}
				while ((bitIndex >= 0) && (bitIndex <= bloomSize));
			}
			else
			{
				return null;
			}
			return new Lucene.Net.Codecs.Bloom.FuzzySet(rightSizedBitSet, rightSizedBitSetSize
				, hashFunction);
		}

		public virtual int GetEstimatedUniqueValues()
		{
			return GetEstimatedNumberUniqueValuesAllowingForCollisions(bloomSize, filter.Cardinality
				());
		}

		// Given a set size and a the number of set bits, produces an estimate of the number of unique values recorded
		public static int GetEstimatedNumberUniqueValuesAllowingForCollisions(int setSize
			, int numRecordedBits)
		{
			double setSizeAsDouble = setSize;
			double numRecordedBitsAsDouble = numRecordedBits;
			double saturation = numRecordedBitsAsDouble / setSizeAsDouble;
			double logInverseSaturation = Math.Log(1 - saturation) * -1;
			return (int)(setSizeAsDouble * logInverseSaturation);
		}

		public virtual float GetSaturation()
		{
			int numBitsSet = filter.Cardinality();
			return (float)numBitsSet / (float)bloomSize;
		}

		public virtual long RamBytesUsed()
		{
			return RamUsageEstimator.SizeOf(filter.GetBits());
		}
	}
}
