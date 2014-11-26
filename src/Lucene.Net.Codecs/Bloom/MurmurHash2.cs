/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Codecs.Bloom;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Codecs.Bloom
{
	/// <summary>
	/// This is a very fast, non-cryptographic hash suitable for general hash-based
	/// lookup.
	/// </summary>
	/// <remarks>
	/// This is a very fast, non-cryptographic hash suitable for general hash-based
	/// lookup. See http://murmurhash.googlepages.com/ for more details.
	/// <p>
	/// The C version of MurmurHash 2.0 found at that site was ported to Java by
	/// Andrzej Bialecki (ab at getopt org).
	/// </p>
	/// <p>
	/// The code from getopt.org was adapted by Mark Harwood in the form here as one of a pluggable choice of
	/// hashing functions as the core function had to be adapted to work with BytesRefs with offsets and lengths
	/// rather than raw byte arrays.
	/// </p>
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public sealed class MurmurHash2 : HashFunction
	{
		public static readonly Lucene.Net.Codecs.Bloom.MurmurHash2 INSTANCE = new 
			Lucene.Net.Codecs.Bloom.MurmurHash2();

		public MurmurHash2()
		{
		}

		public static int Hash(byte[] data, int seed, int offset, int len)
		{
			int m = unchecked((int)(0x5bd1e995));
			int r = 24;
			int h = seed ^ len;
			int len_4 = len >> 2;
			for (int i = 0; i < len_4; i++)
			{
				int i_4 = offset + (i << 2);
				int k = data[i_4 + 3];
				k = k << 8;
				k = k | (data[i_4 + 2] & unchecked((int)(0xff)));
				k = k << 8;
				k = k | (data[i_4 + 1] & unchecked((int)(0xff)));
				k = k << 8;
				k = k | (data[i_4 + 0] & unchecked((int)(0xff)));
				k *= m;
				k ^= (int)(((uint)k) >> r);
				k *= m;
				h *= m;
				h ^= k;
			}
			int len_m = len_4 << 2;
			int left = len - len_m;
			if (left != 0)
			{
				if (left >= 3)
				{
					h ^= data[offset + len - 3] << 16;
				}
				if (left >= 2)
				{
					h ^= data[offset + len - 2] << 8;
				}
				if (left >= 1)
				{
					h ^= data[offset + len - 1];
				}
				h *= m;
			}
			h ^= (int)(((uint)h) >> 13);
			h *= m;
			h ^= (int)(((uint)h) >> 15);
			return h;
		}

		/// <summary>Generates 32 bit hash from byte array with default seed value.</summary>
		/// <remarks>Generates 32 bit hash from byte array with default seed value.</remarks>
		/// <param name="data">
		/// 
		/// byte array to hash
		/// </param>
		/// <param name="offset">the start position in the array to hash</param>
		/// <param name="len">length of the array elements to hash</param>
		/// <returns>32 bit hash of the given array</returns>
		public static int Hash32(byte[] data, int offset, int len)
		{
			return Lucene.Net.Codecs.Bloom.MurmurHash2.Hash(data, unchecked((int)(0x9747b28c
				)), offset, len);
		}

		public sealed override int Hash(BytesRef br)
		{
			return Hash32(br.bytes, br.offset, br.length);
		}
	}
}
