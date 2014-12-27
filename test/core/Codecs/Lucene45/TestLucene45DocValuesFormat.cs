using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Lucene45;
using Lucene.Net.TestFramework.Index;
using Lucene.Net.TestFramework.Util;

namespace Lucene.Net.Test.Codecs.Lucene45
{
	/// <summary>Tests Lucene45DocValuesFormat</summary>
	public class TestLucene45DocValuesFormat : BaseCompressingDocValuesFormatTestCase
	{
		private readonly Codec codec = TestUtil.AlwaysDocValuesFormat(new Lucene45DocValuesFormat());

		protected override Codec Codec
		{
		    get { return codec; }
		}

	    protected override void AddRandomFields(Documents.Document doc)
	    {
	        throw new System.NotImplementedException();
	    }
	}
}
