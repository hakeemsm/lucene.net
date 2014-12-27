using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Lucene40.TestFramework;
using Lucene.Net.TestFramework.Index;
using NUnit.Framework;

namespace Lucene.Net.Test.Codecs.Lucene40
{
	
	public class TestLucene40PostingsFormat : BasePostingsFormatTestCase
	{
		private readonly Codec codec = new Lucene40RWCodec();

		[SetUp]
		public void Setup()
		{
			OLD_FORMAT_IMPERSONATION_IS_ACTIVE = true;
		}

		// explicitly instantiates ancient codec
		protected override Codec Codec
		{
		    get { return codec; }
		}
	}
}
