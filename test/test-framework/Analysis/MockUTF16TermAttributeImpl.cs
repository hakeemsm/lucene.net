using System;
using System.Text;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Util;

namespace Lucene.Net.TestFramework.Analysis
{
	/// <summary>
	/// Extension of
	/// <see cref="Lucene.Net.TestFramework.Analysis.Tokenattributes.CharTermAttributeImpl">Lucene.Net.TestFramework.Analysis.Tokenattributes.CharTermAttributeImpl
	/// 	</see>
	/// that encodes the term
	/// text as UTF-16 bytes instead of as UTF-8 bytes.
	/// </summary>
	public class MockUTF16TermAttributeImpl : CharTermAttribute
	{
	    internal static readonly Encoding charset = Encoding.Default; //.NET Port. default is UTF16
			

		public override void FillBytesRef()
		{
			BytesRef bytes = BytesRef;
		    var utf16 = Array.ConvertAll(base.ToString().ToCharArray(), c => (sbyte) c);
			bytes.bytes = utf16;
			bytes.offset = 0;
			bytes.length = utf16.Length;
		}
	}
}
