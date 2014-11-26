/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Sharpen;

namespace Lucene.Net.Analysis.TR
{
	/// <summary>Strips all characters after an apostrophe (including the apostrophe itself).
	/// 	</summary>
	/// <remarks>
	/// Strips all characters after an apostrophe (including the apostrophe itself).
	/// <p>
	/// In Turkish, apostrophe is used to separate suffixes from proper names
	/// (continent, sea, river, lake, mountain, upland, proper names related to
	/// religion and mythology). This filter intended to be used before stem filters.
	/// For more information, see <a href="http://www.ipcsit.com/vol57/015-ICNI2012-M021.pdf">
	/// Role of Apostrophes in Turkish Information Retrieval</a>
	/// </p>
	/// </remarks>
	public sealed class ApostropheFilter : TokenFilter
	{
		private readonly CharTermAttribute termAtt = AddAttribute<CharTermAttribute>();

		protected ApostropheFilter(TokenStream @in) : base(@in)
		{
		}

		/// <exception cref="System.IO.IOException"></exception>
		public sealed override bool IncrementToken()
		{
			if (!input.IncrementToken())
			{
				return false;
			}
			char[] buffer = termAtt.Buffer;
			int length = termAtt.Length;
			for (int i = 0; i < length; i++)
			{
				if (buffer[i] == '\'' || buffer[i] == '\u2019')
				{
					termAtt.SetLength(i);
					return true;
				}
			}
			return true;
		}
	}
}
