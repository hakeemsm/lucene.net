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
	/// <summary>Base class for hashing functions that can be referred to by name.</summary>
	/// <remarks>
	/// Base class for hashing functions that can be referred to by name.
	/// Subclasses are expected to provide threadsafe implementations of the hash function
	/// on the range of bytes referenced in the provided
	/// <see cref="Lucene.Net.Util.BytesRef">Lucene.Net.Util.BytesRef</see>
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public abstract class HashFunction
	{
		/// <summary>Hashes the contents of the referenced bytes</summary>
		/// <param name="bytes">the data to be hashed</param>
		/// <returns>the hash of the bytes referenced by bytes.offset and length bytes.length
		/// 	</returns>
		public abstract int Hash(BytesRef bytes);
	}
}
