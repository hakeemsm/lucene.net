/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Analysis.NO;
using Lucene.Net.Analysis.Util;
using Sharpen;

namespace Lucene.Net.Analysis.NO
{
	/// <summary>
	/// Minimal Stemmer for Norwegian BokmÃ¥l (no-nb) and Nynorsk (no-nn)
	/// <p>
	/// Stems known plural forms for Norwegian nouns only, together with genitiv -s
	/// </summary>
	public class NorwegianMinimalStemmer
	{
		internal readonly bool useBokmaal;

		internal readonly bool useNynorsk;

		/// <summary>Creates a new NorwegianMinimalStemmer</summary>
		/// <param name="flags">
		/// set to
		/// <see cref="NorwegianLightStemmer.BOKMAAL">NorwegianLightStemmer.BOKMAAL</see>
		/// ,
		/// <see cref="NorwegianLightStemmer.NYNORSK">NorwegianLightStemmer.NYNORSK</see>
		/// , or both.
		/// </param>
		public NorwegianMinimalStemmer(int flags)
		{
			if (flags <= 0 || flags > NorwegianLightStemmer.BOKMAAL + NorwegianLightStemmer.NYNORSK)
			{
				throw new ArgumentException("invalid flags");
			}
			useBokmaal = (flags & NorwegianLightStemmer.BOKMAAL) != 0;
			useNynorsk = (flags & NorwegianLightStemmer.NYNORSK) != 0;
		}

		public virtual int Stem(char[] s, int len)
		{
			// Remove genitiv s
			if (len > 4 && s[len - 1] == 's')
			{
				len--;
			}
			if (len > 5 && (StemmerUtil.EndsWith(s, len, "ene") || (StemmerUtil.EndsWith(s, len
				, "ane") && useNynorsk)))
			{
				// masc/fem/neutr pl definite (hus-ene)
				// masc pl definite (gut-ane)
				return len - 3;
			}
			if (len > 4 && (StemmerUtil.EndsWith(s, len, "er") || StemmerUtil.EndsWith(s, len
				, "en") || StemmerUtil.EndsWith(s, len, "et") || (StemmerUtil.EndsWith(s, len, "ar"
				) && useNynorsk)))
			{
				// masc/fem indefinite
				// masc/fem definite
				// neutr definite
				// masc pl indefinite
				return len - 2;
			}
			if (len > 3)
			{
				switch (s[len - 1])
				{
					case 'a':
					case 'e':
					{
						// fem definite
						// to get correct stem for nouns ending in -e (kake -> kak, kaker -> kak)
						return len - 1;
					}
				}
			}
			return len;
		}
	}
}
