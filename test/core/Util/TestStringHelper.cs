/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Util
{
	public class TestStringHelper : LuceneTestCase
	{
		/// <exception cref="System.Exception"></exception>
		public virtual void TestMurmurHash3()
		{
			// Hashes computed using murmur3_32 from https://code.google.com/p/pyfasthash
			NUnit.Framework.Assert.AreEqual(unchecked((int)(0xf6a5c420)), StringHelper.Murmurhash3_x86_32
				(new BytesRef("foo"), 0));
			NUnit.Framework.Assert.AreEqual(unchecked((int)(0xcd018ef6)), StringHelper.Murmurhash3_x86_32
				(new BytesRef("foo"), 16));
			NUnit.Framework.Assert.AreEqual(unchecked((int)(0x111e7435)), StringHelper.Murmurhash3_x86_32
				(new BytesRef("You want weapons? We're in a library! Books! The best weapons in the world!"
				), 0));
			NUnit.Framework.Assert.AreEqual(unchecked((int)(0x2c628cd0)), StringHelper.Murmurhash3_x86_32
				(new BytesRef("You want weapons? We're in a library! Books! The best weapons in the world!"
				), 3476));
		}
	}
}
