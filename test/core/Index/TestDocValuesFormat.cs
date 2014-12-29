using Lucene.Net.Codecs;
using Lucene.Net.TestFramework.Index;
using Lucene.Net.TestFramework.Util;

namespace Lucene.Net.Test.Index
{
	/// <summary>
	/// Tests the codec configuration defined by LuceneTestCase randomly
	/// (typically a mix across different fields).
	/// </summary>
	/// <remarks>
	/// Tests the codec configuration defined by LuceneTestCase randomly
	/// (typically a mix across different fields).
	/// </remarks>
	public class TestDocValuesFormat : BaseDocValuesFormatTestCase
	{
		protected override Codec Codec
		{
		    get { return Codec.Default; }
		}

		protected override bool CodecAcceptsHugeBinaryValues(string field)
		{
			return TestUtil.FieldSupportsHugeBinaryDocValues(field);
		}
	}
}
