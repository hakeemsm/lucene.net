/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Analysis.PT;
using Lucene.Net.Analysis.Util;
using Sharpen;

namespace Lucene.Net.Analysis.PT
{
	/// <summary>
	/// Light Stemmer for Portuguese
	/// <p>
	/// This stemmer implements the "UniNE" algorithm in:
	/// <i>Light Stemming Approaches for the French, Portuguese, German and Hungarian Languages</i>
	/// Jacques Savoy
	/// </summary>
	public class PortugueseLightStemmer
	{
		public virtual int Stem(char[] s, int len)
		{
			if (len < 4)
			{
				return len;
			}
			len = RemoveSuffix(s, len);
			if (len > 3 && s[len - 1] == 'a')
			{
				len = NormFeminine(s, len);
			}
			if (len > 4)
			{
				switch (s[len - 1])
				{
					case 'e':
					case 'a':
					case 'o':
					{
						len--;
						break;
					}
				}
			}
			//HM:uncomment
			return len;
		}

		private int RemoveSuffix(char[] s, int len)
		{
			if (len > 4 && StemmerUtil.EndsWith(s, len, "es"))
			{
				switch (s[len - 3])
				{
					case 'r':
					case 's':
					case 'l':
					case 'z':
					{
						return len - 2;
					}
				}
			}
			if (len > 3 && StemmerUtil.EndsWith(s, len, "ns"))
			{
				s[len - 2] = 'm';
				return len - 1;
			}
			//HM:uncomment
			if (len > 4 && StemmerUtil.EndsWith(s, len, "ais"))
			{
				s[len - 2] = 'l';
				return len - 1;
			}
			//HM:uncomment
			if (len > 4 && StemmerUtil.EndsWith(s, len, "is"))
			{
				s[len - 1] = 'l';
				return len;
			}
			//HM:uncomment
			if (len > 6 && StemmerUtil.EndsWith(s, len, "mente"))
			{
				return len - 5;
			}
			if (len > 3 && s[len - 1] == 's')
			{
				return len - 1;
			}
			return len;
		}

		private int NormFeminine(char[] s, int len)
		{
			if (len > 7 && (StemmerUtil.EndsWith(s, len, "inha") || StemmerUtil.EndsWith(s, len
				, "iaca") || StemmerUtil.EndsWith(s, len, "eira")))
			{
				s[len - 1] = 'o';
				return len;
			}
			if (len > 6)
			{
				if (StemmerUtil.EndsWith(s, len, "osa") || StemmerUtil.EndsWith(s, len, "ica") ||
					 StemmerUtil.EndsWith(s, len, "ida") || StemmerUtil.EndsWith(s, len, "ada") || StemmerUtil.EndsWith
					(s, len, "iva") || StemmerUtil.EndsWith(s, len, "ama"))
				{
					s[len - 1] = 'o';
					return len;
				}
				if (StemmerUtil.EndsWith(s, len, "ona"))
				{
					//HM:uncomment
					//s[len - 3] = 'Ã£';
					s[len - 2] = 'o';
					return len - 1;
				}
				if (StemmerUtil.EndsWith(s, len, "ora"))
				{
					return len - 1;
				}
				if (StemmerUtil.EndsWith(s, len, "esa"))
				{
					//HM:uncomment
					//s[len - 3] = 'Ãª';
					return len - 1;
				}
				if (StemmerUtil.EndsWith(s, len, "na"))
				{
					s[len - 1] = 'o';
					return len;
				}
			}
			return len;
		}
	}
}
