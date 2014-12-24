using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Lucene41.TestFramrwork;
using Lucene.Net.TestFramework.Index;
using NUnit.Framework;

namespace Lucene.Net.Test.Codecs.Lucene41
{
	public class TestLucene41StoredFieldsFormat : BaseStoredFieldsFormatTestCase
	{
		[SetUp]
		public static void SetUp()
		{
			OLD_FORMAT_IMPERSONATION_IS_ACTIVE = true;
		}

		// explicitly instantiates ancient codec
		protected override Codec Codec
		{
			return new Lucene41RWCodec();
		}

	    protected override void AddRandomFields(Documents.Document doc)
	    {
	        throw new System.NotImplementedException();
	    }
	}
}
