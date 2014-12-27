using System;
using System.Collections.Generic;
using Lucene.Net.Codecs;
using Lucene.Net.Support;
using Lucene.Net.TestFramework.Index;
using Lucene.Net.TestFramework.Util;

namespace Lucene.Net.Test.Codecs.Perfield
{
	/// <summary>Basic tests of PerFieldPostingsFormat</summary>
	public class TestPerFieldPostingsFormat : BasePostingsFormatTestCase
	{
		protected override Codec Codec
		{
		    get { return new RandomCodec(new Random(Random().NextInt(0, int.MaxValue)), new List<string>()); }
		}

		
		public override void TestMergeStability()
		{
			AssumeTrue("The MockRandom PF randomizes content on the fly, so we can't check it"
				, false);
		}
	}
}
