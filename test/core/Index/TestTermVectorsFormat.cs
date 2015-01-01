using System.Collections.Generic;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Lucene3x;
using Lucene.Net.TestFramework.Index;
using NUnit.Framework;


namespace Lucene.Net.Test.Index
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
		    get { return Codec.Default; }
		}

		protected override ICollection<Options> ValidOptions()
		{
		    if (Codec is Lucene3xCodec)
		    {
		        // payloads are not supported on vectors in 3.x indexes
		        return new List<Options> {Options.NONE, Options.POSITIONS_AND_OFFSETS};
		    }
		    return base.ValidOptions();
		}

	    [Test]
		public override void TestMergeStability()
		{
			AssumeTrue("The MockRandom PF randomizes content on the fly, so we can't check it"
				, false);
		}
	}
}
