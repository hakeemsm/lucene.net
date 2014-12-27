using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Lucene41;
using Lucene.Net.TestFramework.Index;
using Lucene.Net.TestFramework.Util;

namespace Lucene.Net.Test.Codecs.Lucene41
{
	/// <summary>Tests BlockPostingsFormat</summary>
	public class TestBlockPostingsFormat : BasePostingsFormatTestCase
	{
		private readonly Codec codec = TestUtil.AlwaysPostingsFormat(new Lucene41PostingsFormat());

		protected override Codec Codec
		{
		    get { return codec; }
		}
	}
}
