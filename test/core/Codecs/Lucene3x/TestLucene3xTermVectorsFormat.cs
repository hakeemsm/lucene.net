using System.Collections.Generic;
using Lucene.Net.Codecs;
using Lucene.Net.TestFramework.Index;

namespace Lucene.Net.Test.Codecs.Lucene3x
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
