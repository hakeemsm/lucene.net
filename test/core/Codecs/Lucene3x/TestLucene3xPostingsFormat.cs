using Lucene.Net.TestFramework.Index;
using Lucene.Net.Codecs;

namespace Lucene.Net.Test.Codecs.Lucene3x
{
	/// <summary>Tests Lucene3x postings format</summary>
	public class TestLucene3xPostingsFormat : BasePostingsFormatTestCase
	{
		private readonly Codec codec = new PreFlexRWCodec();

		/// <summary>we will manually instantiate preflex-rw here</summary>
		[BeforeClass]
		public static void BeforeClass3xPostingsFormat()
		{
			LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE = true;
		}

		protected override Codec GetCodec()
		{
			return codec;
		}
	}
}
