/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Analysis.PT;
using Sharpen;

namespace Lucene.Net.Analysis.PT
{
	/// <summary>
	/// Portuguese stemmer implementing the RSLP (Removedor de Sufixos da Lingua Portuguesa)
	/// algorithm.
	/// </summary>
	/// <remarks>
	/// Portuguese stemmer implementing the RSLP (Removedor de Sufixos da Lingua Portuguesa)
	/// algorithm. This is sometimes also referred to as the Orengo stemmer.
	/// </remarks>
	/// <seealso cref="RSLPStemmerBase">RSLPStemmerBase</seealso>
	public class PortugueseStemmer : RSLPStemmerBase
	{
		private static readonly RSLPStemmerBase.Step plural;

		private static readonly RSLPStemmerBase.Step feminine;

		private static readonly RSLPStemmerBase.Step adverb;

		private static readonly RSLPStemmerBase.Step augmentative;

		private static readonly RSLPStemmerBase.Step noun;

		private static readonly RSLPStemmerBase.Step verb;

		private static readonly RSLPStemmerBase.Step vowel;

		static PortugueseStemmer()
		{
			IDictionary<string, RSLPStemmerBase.Step> steps = Parse(typeof(PortugueseStemmer)
				, "portuguese.rslp");
			plural = steps.Get("Plural");
			feminine = steps.Get("Feminine");
			adverb = steps.Get("Adverb");
			augmentative = steps.Get("Augmentative");
			noun = steps.Get("Noun");
			verb = steps.Get("Verb");
			vowel = steps.Get("Vowel");
		}

		/// <param name="s">buffer, oversized to at least <code>len+1</code></param>
		/// <param name="len">initial valid length of buffer</param>
		/// <returns>new valid length, stemmed</returns>
		public virtual int Stem(char[] s, int len)
		{
			//HM:revisit 
			//assert s.length >= len + 1 : "this stemmer requires an oversized array of at least 1";
			len = plural.Apply(s, len);
			len = adverb.Apply(s, len);
			len = feminine.Apply(s, len);
			len = augmentative.Apply(s, len);
			int oldlen = len;
			len = noun.Apply(s, len);
			if (len == oldlen)
			{
				oldlen = len;
				len = verb.Apply(s, len);
				if (len == oldlen)
				{
					len = vowel.Apply(s, len);
				}
			}
			// rslp accent removal
			//HM:uncomment
			return len;
		}
	}
}
