/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Index;
using Lucene.Net.Sandbox.Queries.Regex;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Sandbox.Queries.Regex
{
	/// <summary>
	/// Subclass of FilteredTermEnum for enumerating all terms that match the
	/// specified regular expression term using the specified regular expression
	/// implementation.
	/// </summary>
	/// <remarks>
	/// Subclass of FilteredTermEnum for enumerating all terms that match the
	/// specified regular expression term using the specified regular expression
	/// implementation.
	/// <p>
	/// Term enumerations are always ordered by Term.compareTo().  Each term in
	/// the enumeration is greater than all that precede it.
	/// </remarks>
	public class RegexTermsEnum : FilteredTermsEnum
	{
		private RegexCapabilities.RegexMatcher regexImpl;

		private readonly BytesRef prefixRef;

		public RegexTermsEnum(TermsEnum tenum, Term term, RegexCapabilities regexCap) : base
			(tenum)
		{
			string text = term.Text();
			this.regexImpl = regexCap.Compile(text);
			string pre = regexImpl.Prefix();
			if (pre == null)
			{
				pre = string.Empty;
			}
			SetInitialSeekTerm(prefixRef = new BytesRef(pre));
		}

		protected override FilteredTermsEnum.AcceptStatus Accept(BytesRef term)
		{
			if (StringHelper.StartsWith(term, prefixRef))
			{
				// TODO: set BoostAttr based on distance of
				// searchTerm.text() and term().text()
				return regexImpl.Match(term) ? FilteredTermsEnum.AcceptStatus.YES : FilteredTermsEnum.AcceptStatus
					.NO;
			}
			else
			{
				return FilteredTermsEnum.AcceptStatus.NO;
			}
		}
	}
}
