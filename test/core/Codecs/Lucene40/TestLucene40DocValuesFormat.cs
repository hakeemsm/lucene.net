using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Lucene40.TestFramework;
using Lucene.Net.TestFramework.Index;
using NUnit.Framework;

namespace Lucene.Net.Test.Codecs.Lucene40
{
	/// <summary>Tests Lucene40DocValuesFormat</summary>
	[TestFixture]
    public class TestLucene40DocValuesFormat : BaseDocValuesFormatTestCase
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

	    protected override void AddRandomFields(Documents.Document doc)
	    {
	        throw new System.NotImplementedException();
	    }

	    // LUCENE-4583: This codec should throw IAE on huge binary values:
		protected override bool CodecAcceptsHugeBinaryValues(string field)
		{
			return false;
		}
	}
}
