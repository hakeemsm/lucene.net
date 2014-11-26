/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.IO;

namespace Lucene.Net.Analysis
{
	/// <summary>
	/// Internal class to enable reuse of the string reader by
	/// <see cref="Analyzer.TokenStream(string, string)">Analyzer.TokenStream(string, string)
	/// 	</see>
	/// 
	/// </summary>
	internal sealed class ReusableStringReader : TextReader
	{
		private int pos = 0;

		private int size = 0;

		private string s = null;

		internal void SetValue(string s)
		{
			this.s = s;
			this.size = s.Length;
			this.pos = 0;
		}

		public override int Read()
		{
			if (pos < size)
			{
				return s[pos++];
			}
			else
			{
				s = null;
				return -1;
			}
		}

		public override int Read(char[] c, int off, int len)
		{
			if (pos < size)
			{
				len = Math.Min(len, size - pos);
			    string s2 = s.Substring(pos, len);
			    c = s2.ToCharArray();
				
				pos += len;
				return len;
			}
		    s = null;
		    return -1;
		}

		public override void Close()
		{
			pos = size;
			// this prevents NPE when reading after close!
			s = null;
		}
	}
}
