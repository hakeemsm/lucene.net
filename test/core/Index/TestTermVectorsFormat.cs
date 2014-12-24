/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Lucene3x;
using Lucene.Net.Index;
using Sharpen;

namespace Lucene.Net.Index
{
	/// <summary>Tests with the default randomized codec.</summary>
	/// <remarks>
	/// Tests with the default randomized codec. Not really redundant with
	/// other specific instantiations since we want to test some test-only impls
	/// like Asserting, as well as make it easy to write a codec and pass -Dtests.codec
	/// </remarks>
	public class TestTermVectorsFormat : BaseTermVectorsFormatTestCase
	{
		protected override Codec Codec
		{
			return Codec.GetDefault();
		}

		protected override ICollection<BaseTermVectorsFormatTestCase.Options> ValidOptions
			()
		{
			if (Codec is Lucene3xCodec)
			{
				// payloads are not supported on vectors in 3.x indexes
				return EnumSet.Range(BaseTermVectorsFormatTestCase.Options.NONE, BaseTermVectorsFormatTestCase.Options
					.POSITIONS_AND_OFFSETS);
			}
			else
			{
				return base.ValidOptions();
			}
		}

		/// <exception cref="System.Exception"></exception>
		public override void TestMergeStability()
		{
			AssumeTrue("The MockRandom PF randomizes content on the fly, so we can't check it"
				, false);
		}
	}
}
