/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Lucene3x;
using Lucene.Net.Index;
using Lucene.Net.TestFramework.Index;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Test.Codecs.Lucene3x
{
	public class TestLucene3xStoredFieldsFormat : BaseStoredFieldsFormatTestCase
	{
		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE = true;
			base.SetUp();
		}

		protected override Codec GetCodec()
		{
			return new PreFlexRWCodec();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void TestWriteReadMerge()
		{
			AssumeFalse("impersonation isnt good enough", true);
		}
		// this test tries to switch up between the codec and another codec.
		// for 3.x: we currently cannot take an index with existing 4.x segments
		// and merge into newly formed 3.x segments.
	}
}
