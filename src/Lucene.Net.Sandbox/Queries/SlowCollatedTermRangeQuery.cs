/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Sandbox.Queries;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Sandbox.Queries
{
	/// <summary>A Query that matches documents within an range of terms.</summary>
	/// <remarks>
	/// A Query that matches documents within an range of terms.
	/// <p>This query matches the documents looking for terms that fall into the
	/// supplied range according to
	/// <see cref="Sharpen.Runtime.CompareOrdinal(string)">Sharpen.Runtime.CompareOrdinal(string)
	/// 	</see>
	/// , unless a <code>Collator</code> is provided. It is not intended
	/// for numerical ranges; use
	/// <see cref="Lucene.Net.Search.NumericRangeQuery{T}">Lucene.Net.Search.NumericRangeQuery&lt;T&gt;
	/// 	</see>
	/// instead.
	/// <p>This query uses the
	/// <see cref="Lucene.Net.Search.MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT
	/// 	">Lucene.Net.Search.MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT</see>
	/// rewrite method.
	/// </remarks>
	[System.ObsoleteAttribute(@"Index collation keys with CollationKeyAnalyzer or ICUCollationKeyAnalyzer instead. This class will be removed in Lucene 5.0"
		)]
	public class SlowCollatedTermRangeQuery : MultiTermQuery
	{
		private string lowerTerm;

		private string upperTerm;

		private bool includeLower;

		private bool includeUpper;

		private Collator collator;

		/// <summary>
		/// Constructs a query selecting all terms greater/equal than
		/// <code>lowerTerm</code> but less/equal than <code>upperTerm</code>.
		/// </summary>
		/// <remarks>
		/// Constructs a query selecting all terms greater/equal than
		/// <code>lowerTerm</code> but less/equal than <code>upperTerm</code>.
		/// <p>
		/// If an endpoint is null, it is said
		/// to be "open". Either or both endpoints may be open.  Open endpoints may not
		/// be exclusive (you can't select all but the first or last term without
		/// explicitly specifying the term to exclude.)
		/// <p>
		/// </remarks>
		/// <param name="lowerTerm">The Term text at the lower end of the range</param>
		/// <param name="upperTerm">The Term text at the upper end of the range</param>
		/// <param name="includeLower">
		/// If true, the <code>lowerTerm</code> is
		/// included in the range.
		/// </param>
		/// <param name="includeUpper">
		/// If true, the <code>upperTerm</code> is
		/// included in the range.
		/// </param>
		/// <param name="collator">
		/// The collator to use to collate index Terms, to determine
		/// their membership in the range bounded by <code>lowerTerm</code> and
		/// <code>upperTerm</code>.
		/// </param>
		public SlowCollatedTermRangeQuery(string field, string lowerTerm, string upperTerm
			, bool includeLower, bool includeUpper, Collator collator) : base(field)
		{
			// javadoc
			// javadoc
			this.lowerTerm = lowerTerm;
			this.upperTerm = upperTerm;
			this.includeLower = includeLower;
			this.includeUpper = includeUpper;
			this.collator = collator;
		}

		/// <summary>Returns the lower value of this range query</summary>
		public virtual string GetLowerTerm()
		{
			return lowerTerm;
		}

		/// <summary>Returns the upper value of this range query</summary>
		public virtual string GetUpperTerm()
		{
			return upperTerm;
		}

		/// <summary>Returns <code>true</code> if the lower endpoint is inclusive</summary>
		public virtual bool IncludesLower()
		{
			return includeLower;
		}

		/// <summary>Returns <code>true</code> if the upper endpoint is inclusive</summary>
		public virtual bool IncludesUpper()
		{
			return includeUpper;
		}

		/// <summary>Returns the collator used to determine range inclusion</summary>
		public virtual Collator GetCollator()
		{
			return collator;
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected override TermsEnum GetTermsEnum(Terms terms, AttributeSource atts)
		{
			if (lowerTerm != null && upperTerm != null && collator.Compare(lowerTerm, upperTerm
				) > 0)
			{
				return TermsEnum.EMPTY;
			}
			TermsEnum tenum = terms.Iterator(null);
			if (lowerTerm == null && upperTerm == null)
			{
				return tenum;
			}
			return new SlowCollatedTermRangeTermsEnum(tenum, lowerTerm, upperTerm, includeLower
				, includeUpper, collator);
		}

		[Obsolete]
		[System.ObsoleteAttribute(@"Use Lucene.Net.Search.MultiTermQuery.GetField() instead."
			)]
		public virtual string Field()
		{
			return GetField();
		}

		/// <summary>Prints a user-readable version of this query.</summary>
		/// <remarks>Prints a user-readable version of this query.</remarks>
		public override string ToString(string field)
		{
			StringBuilder buffer = new StringBuilder();
			if (!GetField().Equals(field))
			{
				buffer.Append(GetField());
				buffer.Append(":");
			}
			buffer.Append(includeLower ? '[' : '{');
			buffer.Append(lowerTerm != null ? lowerTerm : "*");
			buffer.Append(" TO ");
			buffer.Append(upperTerm != null ? upperTerm : "*");
			buffer.Append(includeUpper ? ']' : '}');
			buffer.Append(ToStringUtils.Boost(GetBoost()));
			return buffer.ToString();
		}

		public override int GetHashCode()
		{
			int prime = 31;
			int result = base.GetHashCode();
			result = prime * result + ((collator == null) ? 0 : collator.GetHashCode());
			result = prime * result + (includeLower ? 1231 : 1237);
			result = prime * result + (includeUpper ? 1231 : 1237);
			result = prime * result + ((lowerTerm == null) ? 0 : lowerTerm.GetHashCode());
			result = prime * result + ((upperTerm == null) ? 0 : upperTerm.GetHashCode());
			return result;
		}

		public override bool Equals(object obj)
		{
			if (this == obj)
			{
				return true;
			}
			if (!base.Equals(obj))
			{
				return false;
			}
			if (GetType() != obj.GetType())
			{
				return false;
			}
			Lucene.Net.Sandbox.Queries.SlowCollatedTermRangeQuery other = (Lucene.Net.Sandbox.Queries.SlowCollatedTermRangeQuery
				)obj;
			if (collator == null)
			{
				if (other.collator != null)
				{
					return false;
				}
			}
			else
			{
				if (!collator.Equals(other.collator))
				{
					return false;
				}
			}
			if (includeLower != other.includeLower)
			{
				return false;
			}
			if (includeUpper != other.includeUpper)
			{
				return false;
			}
			if (lowerTerm == null)
			{
				if (other.lowerTerm != null)
				{
					return false;
				}
			}
			else
			{
				if (!lowerTerm.Equals(other.lowerTerm))
				{
					return false;
				}
			}
			if (upperTerm == null)
			{
				if (other.upperTerm != null)
				{
					return false;
				}
			}
			else
			{
				if (!upperTerm.Equals(other.upperTerm))
				{
					return false;
				}
			}
			return true;
		}
	}
}
