/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Index;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Sandbox.Queries
{
	/// <summary>
	/// Subclass of FilteredTermEnum for enumerating all terms that match the
	/// specified range parameters.
	/// </summary>
	/// <remarks>
	/// Subclass of FilteredTermEnum for enumerating all terms that match the
	/// specified range parameters.
	/// <p>Term enumerations are always ordered by
	/// <see cref="Lucene.Net.Index.FilteredTermsEnum.GetComparator()">Lucene.Net.Index.FilteredTermsEnum.GetComparator()
	/// 	</see>
	/// .  Each term in the enumeration is
	/// greater than all that precede it.</p>
	/// </remarks>
	[System.ObsoleteAttribute(@"Index collation keys with CollationKeyAnalyzer or ICUCollationKeyAnalyzer instead. This class will be removed in Lucene 5.0"
		)]
	public class SlowCollatedTermRangeTermsEnum : FilteredTermsEnum
	{
		private Collator collator;

		private string upperTermText;

		private string lowerTermText;

		private bool includeLower;

		private bool includeUpper;

		/// <summary>
		/// Enumerates all terms greater/equal than <code>lowerTerm</code>
		/// but less/equal than <code>upperTerm</code>.
		/// </summary>
		/// <remarks>
		/// Enumerates all terms greater/equal than <code>lowerTerm</code>
		/// but less/equal than <code>upperTerm</code>.
		/// If an endpoint is null, it is said to be "open". Either or both
		/// endpoints may be open.  Open endpoints may not be exclusive
		/// (you can't select all but the first or last term without
		/// explicitly specifying the term to exclude.)
		/// </remarks>
		/// <param name="tenum">source of the terms to enumerate.</param>
		/// <param name="lowerTermText">The term text at the lower end of the range</param>
		/// <param name="upperTermText">The term text at the upper end of the range</param>
		/// <param name="includeLower">If true, the <code>lowerTerm</code> is included in the range.
		/// 	</param>
		/// <param name="includeUpper">If true, the <code>upperTerm</code> is included in the range.
		/// 	</param>
		/// <param name="collator">
		/// The collator to use to collate index Terms, to determine their
		/// membership in the range bounded by <code>lowerTerm</code> and
		/// <code>upperTerm</code>.
		/// </param>
		public SlowCollatedTermRangeTermsEnum(TermsEnum tenum, string lowerTermText, string
			 upperTermText, bool includeLower, bool includeUpper, Collator collator) : base(
			tenum)
		{
			this.collator = collator;
			this.upperTermText = upperTermText;
			this.lowerTermText = lowerTermText;
			this.includeLower = includeLower;
			this.includeUpper = includeUpper;
			// do a little bit of normalization...
			// open ended range queries should always be inclusive.
			if (this.lowerTermText == null)
			{
				this.lowerTermText = string.Empty;
				this.includeLower = true;
			}
			// TODO: optimize
			BytesRef startBytesRef = new BytesRef(string.Empty);
			SetInitialSeekTerm(startBytesRef);
		}

		protected override FilteredTermsEnum.AcceptStatus Accept(BytesRef term)
		{
			if ((includeLower ? collator.Compare(term.Utf8ToString(), lowerTermText) >= 0 : collator
				.Compare(term.Utf8ToString(), lowerTermText) > 0) && (upperTermText == null || (
				includeUpper ? collator.Compare(term.Utf8ToString(), upperTermText) <= 0 : collator
				.Compare(term.Utf8ToString(), upperTermText) < 0)))
			{
				return FilteredTermsEnum.AcceptStatus.YES;
			}
			return FilteredTermsEnum.AcceptStatus.NO;
		}
	}
}
