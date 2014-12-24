using Lucene.Net.Codecs.Compressing;

namespace Lucene.Net.Test.Codecs.Compressing
{
	public class TestHighCompressionMode : AbstractTestCompressionMode
	{
		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			mode = new CompressionMode.CompressionModeHigh();
		}
	}
}
