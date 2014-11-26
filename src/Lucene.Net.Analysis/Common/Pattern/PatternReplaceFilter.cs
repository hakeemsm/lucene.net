/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Sharpen;

namespace Lucene.Net.Analysis.Pattern
{
	/// <summary>
	/// A TokenFilter which applies a Pattern to each token in the stream,
	/// replacing match occurances with the specified replacement string.
	/// </summary>
	/// <remarks>
	/// A TokenFilter which applies a Pattern to each token in the stream,
	/// replacing match occurances with the specified replacement string.
	/// <p>
	/// <b>Note:</b> Depending on the input and the pattern used and the input
	/// TokenStream, this TokenFilter may produce Tokens whose text is the empty
	/// string.
	/// </p>
	/// </remarks>
	/// <seealso cref="Sharpen.Pattern">Sharpen.Pattern</seealso>
	public sealed class PatternReplaceFilter : TokenFilter
	{
		private readonly string replacement;

		private readonly bool all;

		private readonly CharTermAttribute termAtt = AddAttribute<CharTermAttribute>();

		private readonly Matcher m;

		/// <summary>Constructs an instance to replace either the first, or all occurances</summary>
		/// <param name="in">the TokenStream to process</param>
		/// <param name="p">the patterm to apply to each Token</param>
		/// <param name="replacement">
		/// the "replacement string" to substitute, if null a
		/// blank string will be used. Note that this is not the literal
		/// string that will be used, '$' and '\' have special meaning.
		/// </param>
		/// <param name="all">if true, all matches will be replaced otherwise just the first match.
		/// 	</param>
		/// <seealso cref="Sharpen.Matcher.QuoteReplacement(string)">Sharpen.Matcher.QuoteReplacement(string)
		/// 	</seealso>
		public PatternReplaceFilter(TokenStream @in, Sharpen.Pattern p, string replacement
			, bool all) : base(@in)
		{
			this.replacement = (null == replacement) ? string.Empty : replacement;
			this.all = all;
			this.m = p.Matcher(termAtt);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override bool IncrementToken()
		{
			if (!input.IncrementToken())
			{
				return false;
			}
			m.Reset();
			if (m.Find())
			{
				// replaceAll/replaceFirst will reset() this previous find.
				string transformed = all ? m.ReplaceAll(replacement) : m.ReplaceFirst(replacement
					);
				termAtt.SetEmpty().Append(transformed);
			}
			return true;
		}
	}
}
