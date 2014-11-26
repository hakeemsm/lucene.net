/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Analysis.PT;
using Sharpen;

namespace Lucene.Net.Analysis.PT
{
	/// <summary>
	/// Minimal Stemmer for Portuguese
	/// <p>
	/// This follows the "RSLP-S" algorithm presented in:
	/// <i>A study on the Use of Stemming for Monolingual Ad-Hoc Portuguese
	/// Information Retrieval</i> (Orengo, et al)
	/// which is just the plural reduction step of the RSLP
	/// algorithm from <i>A Stemming Algorithm for the Portuguese Language</i>,
	/// Orengo et al.
	/// </summary>
	/// <remarks>
	/// Minimal Stemmer for Portuguese
	/// <p>
	/// This follows the "RSLP-S" algorithm presented in:
	/// <i>A study on the Use of Stemming for Monolingual Ad-Hoc Portuguese
	/// Information Retrieval</i> (Orengo, et al)
	/// which is just the plural reduction step of the RSLP
	/// algorithm from <i>A Stemming Algorithm for the Portuguese Language</i>,
	/// Orengo et al.
	/// </remarks>
	/// <seealso cref="RSLPStemmerBase">RSLPStemmerBase</seealso>
	public class PortugueseMinimalStemmer : RSLPStemmerBase
	{
		private static readonly RSLPStemmerBase.Step pluralStep = Parse(typeof(PortugueseMinimalStemmer
			), "portuguese.rslp").Get("Plural");

		public virtual int Stem(char[] s, int len)
		{
			return pluralStep.Apply(s, len);
		}
	}
}
