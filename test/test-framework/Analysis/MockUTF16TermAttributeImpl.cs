/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Text;
using Lucene.Net.TestFramework.Analysis;
using Lucene.Net.TestFramework.Analysis.Tokenattributes;
using Lucene.Net.TestFramework.Util;
using Sharpen;

namespace Lucene.Net.TestFramework.Analysis
{
	/// <summary>
	/// Extension of
	/// <see cref="Lucene.Net.TestFramework.Analysis.Tokenattributes.CharTermAttributeImpl">Lucene.Net.TestFramework.Analysis.Tokenattributes.CharTermAttributeImpl
	/// 	</see>
	/// that encodes the term
	/// text as UTF-16 bytes instead of as UTF-8 bytes.
	/// </summary>
	public class MockUTF16TermAttributeImpl : CharTermAttributeImpl
	{
		internal static readonly Encoding charset = Sharpen.Extensions.GetEncoding("UTF-16LE"
			);

		public override void FillBytesRef()
		{
			BytesRef bytes = GetBytesRef();
			byte[] utf16 = Sharpen.Runtime.GetBytesForString(ToString(), charset);
			bytes.bytes = utf16;
			bytes.offset = 0;
			bytes.length = utf16.Length;
		}
	}
}
