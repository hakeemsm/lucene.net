/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using NUnit.Framework;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Lucene3x;
using Lucene.Net.Index;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Codecs.Lucene3x
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
