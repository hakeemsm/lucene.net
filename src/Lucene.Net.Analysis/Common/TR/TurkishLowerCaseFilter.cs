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
	/// <summary>Normalizes Turkish token text to lower case.</summary>
	/// <remarks>
	/// Normalizes Turkish token text to lower case.
	/// <p>
	/// Turkish and Azeri have unique casing behavior for some characters. This
	/// filter applies Turkish lowercase rules. For more information, see &lt;a
	/// href="http://en.wikipedia.org/wiki/Turkish_dotted_and_dotless_I"
	/// &gt;http://en.wikipedia.org/wiki/Turkish_dotted_and_dotless_I</a>
	/// </p>
	/// </remarks>
	public sealed class TurkishLowerCaseFilter : TokenFilter
	{
		private const int LATIN_CAPITAL_LETTER_I = '\u0049';

		private const int LATIN_SMALL_LETTER_I = '\u0069';

		private const int LATIN_SMALL_LETTER_DOTLESS_I = '\u0131';

		private const int COMBINING_DOT_ABOVE = '\u0307';

		private readonly CharTermAttribute termAtt = AddAttribute<CharTermAttribute>();

		/// <summary>
		/// Create a new TurkishLowerCaseFilter, that normalizes Turkish token text
		/// to lower case.
		/// </summary>
		/// <remarks>
		/// Create a new TurkishLowerCaseFilter, that normalizes Turkish token text
		/// to lower case.
		/// </remarks>
		/// <param name="in">TokenStream to filter</param>
		public TurkishLowerCaseFilter(TokenStream @in) : base(@in)
		{
		}

		/// <exception cref="System.IO.IOException"></exception>
		public sealed override bool IncrementToken()
		{
			bool iOrAfter = false;
			if (input.IncrementToken())
			{
				char[] buffer = termAtt.Buffer;
				int length = termAtt.Length;
				for (int i = 0; i < length; )
				{
					int ch = char.CodePointAt(buffer, i, length);
					iOrAfter = (ch == LATIN_CAPITAL_LETTER_I || (iOrAfter && char.GetType(ch) == char
						.NON_SPACING_MARK));
					if (iOrAfter)
					{
						switch (ch)
						{
							case COMBINING_DOT_ABOVE:
							{
								// all the special I turkish handling happens here.
								// remove COMBINING_DOT_ABOVE to mimic composed lowercase
								length = Delete(buffer, i, length);
								continue;
								goto case LATIN_CAPITAL_LETTER_I;
							}

							case LATIN_CAPITAL_LETTER_I:
							{
								// i itself, it depends if it is followed by COMBINING_DOT_ABOVE
								// if it is, we will make it small i and later remove the dot
								if (IsBeforeDot(buffer, i + 1, length))
								{
									buffer[i] = (char)LATIN_SMALL_LETTER_I;
								}
								else
								{
									buffer[i] = (char)LATIN_SMALL_LETTER_DOTLESS_I;
									// below is an optimization. no COMBINING_DOT_ABOVE follows,
									// so don't waste time calculating Character.getType(), etc
									iOrAfter = false;
								}
								i++;
								continue;
							}
						}
					}
					i += char.ToChars(System.Char.ToLower(ch), buffer, i);
				}
				termAtt.SetLength(length);
				return true;
			}
			else
			{
				return false;
			}
		}

		/// <summary>lookahead for a combining dot above.</summary>
		/// <remarks>
		/// lookahead for a combining dot above.
		/// other NSMs may be in between.
		/// </remarks>
		private bool IsBeforeDot(char[] s, int pos, int len)
		{
			for (int i = pos; i < len; )
			{
				int ch = char.CodePointAt(s, i, len);
				if (char.GetType(ch) != char.NON_SPACING_MARK)
				{
					return false;
				}
				if (ch == COMBINING_DOT_ABOVE)
				{
					return true;
				}
				i += char.CharCount(ch);
			}
			return false;
		}

		/// <summary>delete a character in-place.</summary>
		/// <remarks>
		/// delete a character in-place.
		/// rarely happens, only if COMBINING_DOT_ABOVE is found after an i
		/// </remarks>
		private int Delete(char[] s, int pos, int len)
		{
			if (pos < len)
			{
				System.Array.Copy(s, pos + 1, s, pos, len - pos - 1);
			}
			return len - 1;
		}
	}
}
