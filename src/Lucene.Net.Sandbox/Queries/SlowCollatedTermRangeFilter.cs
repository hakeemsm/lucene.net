/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Sandbox.Queries;
using Lucene.Net.Search;
using Sharpen;

namespace Lucene.Net.Sandbox.Queries
{
	/// <summary>
	/// A Filter that restricts search results to a range of term
	/// values in a given field.
	/// </summary>
	/// <remarks>
	/// A Filter that restricts search results to a range of term
	/// values in a given field.
	/// <p>This filter matches the documents looking for terms that fall into the
	/// supplied range according to
	/// <see cref="Sharpen.Runtime.CompareOrdinal(string)">Sharpen.Runtime.CompareOrdinal(string)
	/// 	</see>
	/// , unless a <code>Collator</code> is provided. It is not intended
	/// for numerical ranges; use
	/// <see cref="Lucene.Net.Search.NumericRangeFilter{T}">Lucene.Net.Search.NumericRangeFilter&lt;T&gt;
	/// 	</see>
	/// instead.
	/// <p>If you construct a large number of range filters with different ranges but on the
	/// same field,
	/// <see cref="Lucene.Net.Search.FieldCacheRangeFilter{T}">Lucene.Net.Search.FieldCacheRangeFilter&lt;T&gt;
	/// 	</see>
	/// may have significantly better performance.
	/// </remarks>
	[System.ObsoleteAttribute(@"Index collation keys with CollationKeyAnalyzer or ICUCollationKeyAnalyzer instead. This class will be removed in Lucene 5.0"
		)]
	public class SlowCollatedTermRangeFilter : MultiTermQueryWrapperFilter<SlowCollatedTermRangeQuery
		>
	{
		/// <param name="lowerTerm">The lower bound on this range</param>
		/// <param name="upperTerm">The upper bound on this range</param>
		/// <param name="includeLower">Does this range include the lower bound?</param>
		/// <param name="includeUpper">Does this range include the upper bound?</param>
		/// <param name="collator">
		/// The collator to use when determining range inclusion; set
		/// to null to use Unicode code point ordering instead of collation.
		/// </param>
		/// <exception cref="System.ArgumentException">
		/// if both terms are null or if
		/// lowerTerm is null and includeLower is true (similar for upperTerm
		/// and includeUpper)
		/// </exception>
		public SlowCollatedTermRangeFilter(string fieldName, string lowerTerm, string upperTerm
			, bool includeLower, bool includeUpper, Collator collator) : base(new SlowCollatedTermRangeQuery
			(fieldName, lowerTerm, upperTerm, includeLower, includeUpper, collator))
		{
		}

		// javadoc
		// javadoc
		/// <summary>Returns the lower value of this range filter</summary>
		public virtual string GetLowerTerm()
		{
			return query.GetLowerTerm();
		}

		/// <summary>Returns the upper value of this range filter</summary>
		public virtual string GetUpperTerm()
		{
			return query.GetUpperTerm();
		}

		/// <summary>Returns <code>true</code> if the lower endpoint is inclusive</summary>
		public virtual bool IncludesLower()
		{
			return query.IncludesLower();
		}

		/// <summary>Returns <code>true</code> if the upper endpoint is inclusive</summary>
		public virtual bool IncludesUpper()
		{
			return query.IncludesUpper();
		}

		/// <summary>Returns the collator used to determine range inclusion, if any.</summary>
		/// <remarks>Returns the collator used to determine range inclusion, if any.</remarks>
		public virtual Collator GetCollator()
		{
			return query.GetCollator();
		}
	}
}
