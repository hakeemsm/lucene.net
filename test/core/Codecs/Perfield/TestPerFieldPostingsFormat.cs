/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Perfield;
using Lucene.Net.Index;
using Sharpen;

namespace Lucene.Net.Codecs.Perfield
{
	/// <summary>Basic tests of PerFieldPostingsFormat</summary>
	public class TestPerFieldPostingsFormat : BasePostingsFormatTestCase
	{
		protected override Codec GetCodec()
		{
			return new RandomCodec(new Random(Random().NextLong()), Collections.EmptySet<string
				>());
		}

		/// <exception cref="System.Exception"></exception>
		public override void TestMergeStability()
		{
			AssumeTrue("The MockRandom PF randomizes content on the fly, so we can't check it"
				, false);
		}
	}
}
