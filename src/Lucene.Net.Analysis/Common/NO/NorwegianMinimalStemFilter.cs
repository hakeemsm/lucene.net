/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.NO;
using Lucene.Net.Analysis.Tokenattributes;
using Sharpen;

namespace Lucene.Net.Analysis.NO
{
	/// <summary>
	/// A
	/// <see cref="Lucene.Net.Analysis.TokenFilter">Lucene.Net.Analysis.TokenFilter
	/// 	</see>
	/// that applies
	/// <see cref="NorwegianMinimalStemmer">NorwegianMinimalStemmer</see>
	/// to stem Norwegian
	/// words.
	/// <p>
	/// To prevent terms from being stemmed use an instance of
	/// <see cref="Lucene.Net.Analysis.Miscellaneous.SetKeywordMarkerFilter">Lucene.Net.Analysis.Miscellaneous.SetKeywordMarkerFilter
	/// 	</see>
	/// or a custom
	/// <see cref="Lucene.Net.Analysis.TokenFilter">Lucene.Net.Analysis.TokenFilter
	/// 	</see>
	/// that sets
	/// the
	/// <see cref="Lucene.Net.Analysis.Tokenattributes.KeywordAttribute">Lucene.Net.Analysis.Tokenattributes.KeywordAttribute
	/// 	</see>
	/// before this
	/// <see cref="Lucene.Net.Analysis.TokenStream">Lucene.Net.Analysis.TokenStream
	/// 	</see>
	/// .
	/// </p>
	/// </summary>
	public sealed class NorwegianMinimalStemFilter : TokenFilter
	{
		private readonly NorwegianMinimalStemmer stemmer;

		private readonly CharTermAttribute termAtt = AddAttribute<CharTermAttribute>();

		private readonly KeywordAttribute keywordAttr = AddAttribute<KeywordAttribute>();

		/// <summary>
		/// Calls
		/// <see cref="NorwegianMinimalStemFilter(Lucene.Net.Analysis.TokenStream, int)
		/// 	">
		/// 
		/// NorwegianMinimalStemFilter(input, BOKMAAL)
		/// </see>
		/// </summary>
		protected NorwegianMinimalStemFilter(TokenStream input) : this(input, NorwegianLightStemmer
			.BOKMAAL)
		{
		}

		/// <summary>Creates a new NorwegianLightStemFilter</summary>
		/// <param name="flags">
		/// set to
		/// <see cref="NorwegianLightStemmer.BOKMAAL">NorwegianLightStemmer.BOKMAAL</see>
		/// ,
		/// <see cref="NorwegianLightStemmer.NYNORSK">NorwegianLightStemmer.NYNORSK</see>
		/// , or both.
		/// </param>
		public NorwegianMinimalStemFilter(TokenStream input, int flags) : base(input)
		{
			this.stemmer = new NorwegianMinimalStemmer(flags);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override bool IncrementToken()
		{
			if (input.IncrementToken())
			{
				if (!keywordAttr.IsKeyword)
				{
					int newlen = stemmer.Stem(termAtt.Buffer, termAtt.Length);
					termAtt.SetLength(newlen);
				}
				return true;
			}
			else
			{
				return false;
			}
		}
	}
}
