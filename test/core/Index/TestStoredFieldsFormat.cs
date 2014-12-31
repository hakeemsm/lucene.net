using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Lucene3x;
using Lucene.Net.Index;
using Lucene.Net.TestFramework.Index;


namespace Lucene.Net.Test.Index
{
	/// <summary>Tests with the default randomized codec.</summary>
	/// <remarks>
	/// Tests with the default randomized codec. Not really redundant with
	/// other specific instantiations since we want to test some test-only impls
	/// like Asserting, as well as make it easy to write a codec and pass -Dtests.codec
	/// </remarks>
	public class TestStoredFieldsFormat : BaseStoredFieldsFormatTestCase
	{
		protected override Codec Codec
		{
		    get { return Codec.Default; }
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void TestWriteReadMerge()
		{
			AssumeFalse("impersonation isnt good enough", Codec is Lucene3xCodec);
			// this test tries to switch up between the codec and another codec.
			// for 3.x: we currently cannot take an index with existing 4.x segments
			// and merge into newly formed 3.x segments.
			base.TestWriteReadMerge();
		}
	}
}
