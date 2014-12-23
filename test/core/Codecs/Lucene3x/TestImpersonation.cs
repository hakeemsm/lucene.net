/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Lucene3x;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Codecs.Lucene3x
{
	/// <summary>Test that the SPI magic is returning "PreFlexRWCodec" for Lucene3x</summary>
	/// <lucene.experimental></lucene.experimental>
	public class TestImpersonation : LuceneTestCase
	{
		/// <exception cref="System.Exception"></exception>
		public virtual void Test()
		{
			Codec codec = Codec.ForName("Lucene3x");
			NUnit.Framework.Assert.IsTrue(codec is PreFlexRWCodec);
		}
	}
}
