/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Lucene40;
using Lucene.Net.Index;
using Sharpen;

namespace Lucene.Net.Codecs.Lucene40
{
	public class TestLucene40TermVectorsFormat : BaseTermVectorsFormatTestCase
	{
		[NUnit.Framework.BeforeClass]
		public static void BeforeClass()
		{
			OLD_FORMAT_IMPERSONATION_IS_ACTIVE = true;
		}

		// explicitly instantiates ancient codec
		protected override Codec GetCodec()
		{
			return new Lucene40RWCodec();
		}
	}
}
