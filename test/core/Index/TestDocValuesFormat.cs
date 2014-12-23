/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Codecs;
using Lucene.Net.Index;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
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
		protected override Codec GetCodec()
		{
			return Codec.GetDefault();
		}

		protected override bool CodecAcceptsHugeBinaryValues(string field)
		{
			return TestUtil.FieldSupportsHugeBinaryDocValues(field);
		}
	}
}
