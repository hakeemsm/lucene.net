/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Lucene45;
using Lucene.Net.Index;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Codecs.Lucene45
{
	/// <summary>Tests Lucene45DocValuesFormat</summary>
	public class TestLucene45DocValuesFormat : BaseCompressingDocValuesFormatTestCase
	{
		private readonly Codec codec = TestUtil.AlwaysDocValuesFormat(new Lucene45DocValuesFormat
			());

		protected override Codec GetCodec()
		{
			return codec;
		}
	}
}
