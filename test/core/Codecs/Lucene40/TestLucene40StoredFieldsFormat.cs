using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Lucene40.TestFramework;
using Lucene.Net.TestFramework.Index;
using NUnit.Framework;

namespace Lucene.Net.Test.Codecs.Lucene40
{
	public class TestLucene40StoredFieldsFormat : BaseStoredFieldsFormatTestCase
	{
		[SetUp]
		public static void Setup()
		{
			OLD_FORMAT_IMPERSONATION_IS_ACTIVE = true;
		}

		// explicitly instantiates ancient codec
		protected override Codec Codec
		{
		    get { return new Lucene40RWCodec(); }
		}

	    protected override void AddRandomFields(Documents.Document doc)
	    {
	        throw new System.NotImplementedException();
	    }
	}
}
