/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Codecs;
using Lucene.Net.Index;
using Sharpen;

namespace Lucene.Net.Test.Index
{
	/// <summary>
	/// Tests the codec configuration defined by LuceneTestCase randomly
	/// (typically a mix across different fields).
	/// </summary>
	/// <remarks>
	/// Tests the codec configuration defined by LuceneTestCase randomly
	/// (typically a mix across different fields).
	/// </remarks>
	public class TestPostingsFormat : BasePostingsFormatTestCase
	{
		protected override Codec Codec
		{
			return Codec.GetDefault();
		}

		/// <exception cref="System.Exception"></exception>
		public override void TestMergeStability()
		{
			AssumeTrue("The MockRandom PF randomizes content on the fly, so we can't check it"
				, false);
		}
	}
}
