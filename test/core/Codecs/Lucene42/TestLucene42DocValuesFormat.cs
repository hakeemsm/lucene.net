using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Lucene42.TestFramework;
using Lucene.Net.TestFramework.Index;
using NUnit.Framework;

namespace Lucene.Net.Test.Codecs.Lucene42
{
	/// <summary>Tests Lucene42DocValuesFormat</summary>
	public class TestLucene42DocValuesFormat : BaseCompressingDocValuesFormatTestCase
	{
		private readonly Codec codec = new Lucene42RWCodec();

		[SetUp]
		public static void BeforeClass()
		{
			OLD_FORMAT_IMPERSONATION_IS_ACTIVE = true;
		}

		// explicitly instantiates ancient codec
		protected override Codec Codec
		{
			return codec;
		}

	    protected override void AddRandomFields(Documents.Document doc)
	    {
	        throw new System.NotImplementedException();
	    }

	    protected override bool CodecAcceptsHugeBinaryValues(string field)
		{
			return false;
		}
	}
}
