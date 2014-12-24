using Lucene.Net.Codecs.Compressing;
using NUnit.Framework;

namespace Lucene.Net.Test.Codecs.Compressing
{
    [TestFixture]
	public class TestFastDecompressionMode : AbstractTestLZ4CompressionMode
	{
		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			mode = CompressionMode.FAST;
		}

		[Test]
		public override byte[] Test(byte[] decompressed, int off, int len)
		{
			byte[] compressed = base.Test(decompressed, off, len);
			byte[] compressed2 = Compress(CompressionMode.FAST.NewCompressor(), decompressed, 
				off, len);
			// because of the way this compression mode works, its output is necessarily
			// smaller than the output of CompressionMode.FAST
			IsTrue(compressed.Length <= compressed2.Length);
			return compressed;
		}
	}
}
