/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Analysis.SV;
using Lucene.Net.Analysis.Util;
using Sharpen;

namespace Lucene.Net.Analysis.SV
{
	/// <summary>Light Stemmer for Swedish.</summary>
	/// <remarks>
	/// Light Stemmer for Swedish.
	/// <p>
	/// This stemmer implements the algorithm described in:
	/// <i>Report on CLEF-2003 Monolingual Tracks</i>
	/// Jacques Savoy
	/// </remarks>
	public class SwedishLightStemmer
	{
		public virtual int Stem(char[] s, int len)
		{
			if (len > 4 && s[len - 1] == 's')
			{
				len--;
			}
			if (len > 7 && (StemmerUtil.EndsWith(s, len, "elser") || StemmerUtil.EndsWith(s, 
				len, "heten")))
			{
				return len - 5;
			}
			if (len > 6 && (StemmerUtil.EndsWith(s, len, "arne") || StemmerUtil.EndsWith(s, len
				, "erna") || StemmerUtil.EndsWith(s, len, "ande") || StemmerUtil.EndsWith(s, len
				, "else") || StemmerUtil.EndsWith(s, len, "aste") || StemmerUtil.EndsWith(s, len
				, "orna") || StemmerUtil.EndsWith(s, len, "aren")))
			{
				return len - 4;
			}
			if (len > 5 && (StemmerUtil.EndsWith(s, len, "are") || StemmerUtil.EndsWith(s, len
				, "ast") || StemmerUtil.EndsWith(s, len, "het")))
			{
				return len - 3;
			}
			if (len > 4 && (StemmerUtil.EndsWith(s, len, "ar") || StemmerUtil.EndsWith(s, len
				, "er") || StemmerUtil.EndsWith(s, len, "or") || StemmerUtil.EndsWith(s, len, "en"
				) || StemmerUtil.EndsWith(s, len, "at") || StemmerUtil.EndsWith(s, len, "te") ||
				 StemmerUtil.EndsWith(s, len, "et")))
			{
				return len - 2;
			}
			if (len > 3)
			{
				switch (s[len - 1])
				{
					case 't':
					case 'a':
					case 'e':
					case 'n':
					{
						return len - 1;
					}
				}
			}
			return len;
		}
	}
}
