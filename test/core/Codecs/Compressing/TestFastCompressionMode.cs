/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Codecs.Compressing;
using Sharpen;

namespace Lucene.Net.Codecs.Compressing
{
	public class TestFastCompressionMode : AbstractTestLZ4CompressionMode
	{
		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			mode = CompressionMode.FAST;
		}
	}
}
