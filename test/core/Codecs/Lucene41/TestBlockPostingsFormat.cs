/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Lucene41;
using Lucene.Net.Index;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Codecs.Lucene41
{
	/// <summary>Tests BlockPostingsFormat</summary>
	public class TestBlockPostingsFormat : BasePostingsFormatTestCase
	{
		private readonly Codec codec = TestUtil.AlwaysPostingsFormat(new Lucene41PostingsFormat
			());

		protected override Codec GetCodec()
		{
			return codec;
		}
	}
}
