/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Queries.Function;
using Lucene.Net.Search;
using Sharpen;

namespace Lucene.Net.Facet.Range
{
	/// <summary>Base class for a single labeled range.</summary>
	/// <remarks>Base class for a single labeled range.</remarks>
	/// <lucene.experimental></lucene.experimental>
	public abstract class Range
	{
		/// <summary>Label that identifies this range.</summary>
		/// <remarks>Label that identifies this range.</remarks>
		public readonly string label;

		/// <summary>Sole constructor.</summary>
		/// <remarks>Sole constructor.</remarks>
		protected internal Range(string label)
		{
			// javadocs
			// javadocs
			// javadocs
			// javadocs
			if (label == null)
			{
				throw new ArgumentNullException("label cannot be null");
			}
			this.label = label;
		}

		/// <summary>
		/// Returns a new
		/// <see cref="Lucene.Net.Search.Filter">Lucene.Net.Search.Filter</see>
		/// accepting only documents
		/// in this range.  This filter is not general-purpose;
		/// you should either use it with
		/// <see cref="Lucene.Net.Facet.DrillSideways">Lucene.Net.Facet.DrillSideways
		/// 	</see>
		/// by
		/// adding it to
		/// <see cref="Lucene.Net.Facet.DrillDownQuery.Add(string, string[])">Lucene.Net.Facet.DrillDownQuery.Add(string, string[])
		/// 	</see>
		/// , or pass it to
		/// <see cref="Lucene.Net.Search.FilteredQuery">Lucene.Net.Search.FilteredQuery
		/// 	</see>
		/// using its
		/// <see cref="Lucene.Net.Search.FilteredQuery.QUERY_FIRST_FILTER_STRATEGY">Lucene.Net.Search.FilteredQuery.QUERY_FIRST_FILTER_STRATEGY
		/// 	</see>
		/// .  If the
		/// <see cref="Lucene.Net.Queries.Function.ValueSource">Lucene.Net.Queries.Function.ValueSource
		/// 	</see>
		/// is static, e.g. an indexed numeric
		/// field, then it may be more efficient to use
		/// <see cref="Lucene.Net.Search.NumericRangeFilter{T}">Lucene.Net.Search.NumericRangeFilter&lt;T&gt;
		/// 	</see>
		/// .  The provided fastMatchFilter,
		/// if non-null, will first be consulted, and only if
		/// that is set for each document will the range then be
		/// checked.
		/// </summary>
		public abstract Filter GetFilter(Filter fastMatchFilter, ValueSource valueSource);

		/// <summary>
		/// Returns a new
		/// <see cref="Lucene.Net.Search.Filter">Lucene.Net.Search.Filter</see>
		/// accepting only documents
		/// in this range.  This filter is not general-purpose;
		/// you should either use it with
		/// <see cref="Lucene.Net.Facet.DrillSideways">Lucene.Net.Facet.DrillSideways
		/// 	</see>
		/// by
		/// adding it to
		/// <see cref="Lucene.Net.Facet.DrillDownQuery.Add(string, string[])">Lucene.Net.Facet.DrillDownQuery.Add(string, string[])
		/// 	</see>
		/// , or pass it to
		/// <see cref="Lucene.Net.Search.FilteredQuery">Lucene.Net.Search.FilteredQuery
		/// 	</see>
		/// using its
		/// <see cref="Lucene.Net.Search.FilteredQuery.QUERY_FIRST_FILTER_STRATEGY">Lucene.Net.Search.FilteredQuery.QUERY_FIRST_FILTER_STRATEGY
		/// 	</see>
		/// .  If the
		/// <see cref="Lucene.Net.Queries.Function.ValueSource">Lucene.Net.Queries.Function.ValueSource
		/// 	</see>
		/// is static, e.g. an indexed numeric
		/// field, then it may be more efficient to use
		/// <see cref="Lucene.Net.Search.NumericRangeFilter{T}">Lucene.Net.Search.NumericRangeFilter&lt;T&gt;
		/// 	</see>
		/// .
		/// </summary>
		public virtual Filter GetFilter(ValueSource valueSource)
		{
			return GetFilter(null, valueSource);
		}

		/// <summary>Invoke this for a useless range.</summary>
		/// <remarks>Invoke this for a useless range.</remarks>
		protected internal virtual void FailNoMatch()
		{
			throw new ArgumentException("range \"" + label + "\" matches nothing");
		}
	}
}
