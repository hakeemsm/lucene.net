/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.PT;
using Lucene.Net.Analysis.Tokenattributes;
using Sharpen;

namespace Lucene.Net.Analysis.PT
{
	/// <summary>
	/// A
	/// <see cref="Lucene.Net.Analysis.TokenFilter">Lucene.Net.Analysis.TokenFilter
	/// 	</see>
	/// that applies
	/// <see cref="PortugueseStemmer">PortugueseStemmer</see>
	/// to stem
	/// Portuguese words.
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
	public sealed class PortugueseStemFilter : TokenFilter
	{
		private readonly PortugueseStemmer stemmer = new PortugueseStemmer();

		private readonly CharTermAttribute termAtt = AddAttribute<CharTermAttribute>();

		private readonly KeywordAttribute keywordAttr = AddAttribute<KeywordAttribute>();

		protected PortugueseStemFilter(TokenStream input) : base(input)
		{
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override bool IncrementToken()
		{
			if (input.IncrementToken())
			{
				if (!keywordAttr.IsKeyword)
				{
					// this stemmer increases word length by 1: worst case '*Ã£' -> '*Ã£o'
					int len = termAtt.Length;
					int newlen = stemmer.Stem(termAtt.ResizeBuffer(len + 1), len);
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
