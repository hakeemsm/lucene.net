/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Analysis.Util;
using Sharpen;

namespace Lucene.Net.Analysis.NO
{
	/// <summary>Light Stemmer for Norwegian.</summary>
	/// <remarks>
	/// Light Stemmer for Norwegian.
	/// <p>
	/// Parts of this stemmer is adapted from SwedishLightStemFilter, except
	/// that while the Swedish one has a pre-defined rule set and a corresponding
	/// corpus to validate against whereas the Norwegian one is hand crafted.
	/// </remarks>
	public class NorwegianLightStemmer
	{
		/// <summary>Constant to remove BokmÃ¥l-specific endings</summary>
		public const int BOKMAAL = 1;

		/// <summary>Constant to remove Nynorsk-specific endings</summary>
		public const int NYNORSK = 2;

		internal readonly bool useBokmaal;

		internal readonly bool useNynorsk;

		/// <summary>Creates a new NorwegianLightStemmer</summary>
		/// <param name="flags">
		/// set to
		/// <see cref="BOKMAAL">BOKMAAL</see>
		/// ,
		/// <see cref="NYNORSK">NYNORSK</see>
		/// , or both.
		/// </param>
		public NorwegianLightStemmer(int flags)
		{
			if (flags <= 0 || flags > BOKMAAL + NYNORSK)
			{
				throw new ArgumentException("invalid flags");
			}
			useBokmaal = (flags & BOKMAAL) != 0;
			useNynorsk = (flags & NYNORSK) != 0;
		}

		public virtual int Stem(char[] s, int len)
		{
			// Remove posessive -s (bilens -> bilen) and continue checking 
			if (len > 4 && s[len - 1] == 's')
			{
				len--;
			}
			// Remove common endings, single-pass
			if (len > 7 && ((StemmerUtil.EndsWith(s, len, "heter") && useBokmaal) || (StemmerUtil.EndsWith
				(s, len, "heten") && useBokmaal) || (StemmerUtil.EndsWith(s, len, "heita") && useNynorsk
				)))
			{
				// general ending (hemmelig-heter -> hemmelig)
				// general ending (hemmelig-heten -> hemmelig)
				// general ending (hemmeleg-heita -> hemmeleg)
				return len - 5;
			}
			// Remove Nynorsk common endings, single-pass
			if (len > 8 && useNynorsk && (StemmerUtil.EndsWith(s, len, "heiter") || StemmerUtil.EndsWith
				(s, len, "leiken") || StemmerUtil.EndsWith(s, len, "leikar")))
			{
				// general ending (hemmeleg-heiter -> hemmeleg)
				// general ending (trygg-leiken -> trygg)
				// general ending (trygg-leikar -> trygg)
				return len - 6;
			}
			if (len > 5 && (StemmerUtil.EndsWith(s, len, "dom") || (StemmerUtil.EndsWith(s, len
				, "het") && useBokmaal)))
			{
				// general ending (kristen-dom -> kristen)
				// general ending (hemmelig-het -> hemmelig)
				return len - 3;
			}
			if (len > 6 && useNynorsk && (StemmerUtil.EndsWith(s, len, "heit") || StemmerUtil.EndsWith
				(s, len, "semd") || StemmerUtil.EndsWith(s, len, "leik")))
			{
				// general ending (hemmeleg-heit -> hemmeleg)
				// general ending (verk-semd -> verk)
				// general ending (trygg-leik -> trygg)
				return len - 4;
			}
			if (len > 7 && (StemmerUtil.EndsWith(s, len, "elser") || StemmerUtil.EndsWith(s, 
				len, "elsen")))
			{
				// general ending (fÃ¸l-elser -> fÃ¸l)
				// general ending (fÃ¸l-elsen -> fÃ¸l)
				return len - 5;
			}
			if (len > 6 && ((StemmerUtil.EndsWith(s, len, "ende") && useBokmaal) || (StemmerUtil.EndsWith
				(s, len, "ande") && useNynorsk) || StemmerUtil.EndsWith(s, len, "else") || (StemmerUtil.EndsWith
				(s, len, "este") && useBokmaal) || (StemmerUtil.EndsWith(s, len, "aste") && useNynorsk
				) || (StemmerUtil.EndsWith(s, len, "eren") && useBokmaal) || (StemmerUtil.EndsWith
				(s, len, "aren") && useNynorsk)))
			{
				// (sov-ende -> sov)
				// (sov-ande -> sov)
				// general ending (fÃ¸l-else -> fÃ¸l)
				// adj (fin-este -> fin)
				// adj (fin-aste -> fin)
				// masc
				// masc 
				return len - 4;
			}
			if (len > 5 && ((StemmerUtil.EndsWith(s, len, "ere") && useBokmaal) || (StemmerUtil.EndsWith
				(s, len, "are") && useNynorsk) || (StemmerUtil.EndsWith(s, len, "est") && useBokmaal
				) || (StemmerUtil.EndsWith(s, len, "ast") && useNynorsk) || StemmerUtil.EndsWith
				(s, len, "ene") || (StemmerUtil.EndsWith(s, len, "ane") && useNynorsk)))
			{
				// adj (fin-ere -> fin)
				// adj (fin-are -> fin)
				// adj (fin-est -> fin)
				// adj (fin-ast -> fin)
				// masc/fem/neutr pl definite (hus-ene)
				// masc pl definite (gut-ane)
				return len - 3;
			}
			if (len > 4 && (StemmerUtil.EndsWith(s, len, "er") || StemmerUtil.EndsWith(s, len
				, "en") || StemmerUtil.EndsWith(s, len, "et") || (StemmerUtil.EndsWith(s, len, "ar"
				) && useNynorsk) || (StemmerUtil.EndsWith(s, len, "st") && useBokmaal) || StemmerUtil.EndsWith
				(s, len, "te")))
			{
				// masc/fem indefinite
				// masc/fem definite
				// neutr definite
				// masc pl indefinite
				// adj (billig-st -> billig)
				return len - 2;
			}
			if (len > 3)
			{
				switch (s[len - 1])
				{
					case 'a':
					case 'e':
					case 'n':
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
