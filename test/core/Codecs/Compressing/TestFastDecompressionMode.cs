/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Codecs.Compressing;
using Sharpen;

namespace Lucene.Net.Codecs.Compressing
{
	public class TestFastDecompressionMode : AbstractTestLZ4CompressionMode
	{
		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			mode = CompressionMode.FAST_DECOMPRESSION;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override byte[] Test(byte[] decompressed, int off, int len)
		{
			byte[] compressed = base.Test(decompressed, off, len);
			byte[] compressed2 = Compress(CompressionMode.FAST.NewCompressor(), decompressed, 
				off, len);
			// because of the way this compression mode works, its output is necessarily
			// smaller than the output of CompressionMode.FAST
			NUnit.Framework.Assert.IsTrue(compressed.Length <= compressed2.Length);
			return compressed;
		}
	}
}
