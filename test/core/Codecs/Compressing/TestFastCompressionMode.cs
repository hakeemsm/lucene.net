using Lucene.Net.Codecs.Compressing;

namespace Lucene.Net.Test.Codecs.Compressing
{
	public class TestFastCompressionMode : AbstractTestLZ4CompressionMode
	{
		
		public override void SetUp()
		{
			base.SetUp();
			mode = CompressionMode.FAST;
		}
	}
}
