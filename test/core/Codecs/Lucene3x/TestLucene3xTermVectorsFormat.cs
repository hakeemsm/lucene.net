/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Lucene3x;
using Lucene.Net.Index;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Codecs.Lucene3x
{
	public class TestLucene3xTermVectorsFormat : BaseTermVectorsFormatTestCase
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

		protected override ICollection<BaseTermVectorsFormatTestCase.Options> ValidOptions
			()
		{
			return EnumSet.Range(BaseTermVectorsFormatTestCase.Options.NONE, BaseTermVectorsFormatTestCase.Options
				.POSITIONS_AND_OFFSETS);
		}
	}
}
