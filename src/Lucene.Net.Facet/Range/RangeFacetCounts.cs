/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Facet;
using Lucene.Net.Search;
using Sharpen;

namespace Lucene.Net.Facet.Range
{
	/// <summary>Base class for range faceting.</summary>
	/// <remarks>Base class for range faceting.</remarks>
	/// <lucene.experimental></lucene.experimental>
	internal abstract class RangeFacetCounts : Facets
	{
		/// <summary>Ranges passed to constructor.</summary>
		/// <remarks>Ranges passed to constructor.</remarks>
		protected internal readonly Lucene.Net.Facet.Range.Range[] ranges;

		/// <summary>Counts, initialized in by subclass.</summary>
		/// <remarks>Counts, initialized in by subclass.</remarks>
		protected internal readonly int[] counts;

		/// <summary>
		/// Optional: if specified, we first test this Filter to
		/// see whether the document should be checked for
		/// matching ranges.
		/// </summary>
		/// <remarks>
		/// Optional: if specified, we first test this Filter to
		/// see whether the document should be checked for
		/// matching ranges.  If this is null, all documents are
		/// checked.
		/// </remarks>
		protected internal readonly Filter fastMatchFilter;

		/// <summary>Our field name.</summary>
		/// <remarks>Our field name.</remarks>
		protected internal readonly string field;

		/// <summary>Total number of hits.</summary>
		/// <remarks>Total number of hits.</remarks>
		protected internal int totCount;

		/// <summary>
		/// Create
		/// <code>RangeFacetCounts</code>
		/// 
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal RangeFacetCounts(string field, Lucene.Net.Facet.Range.Range
			[] ranges, Filter fastMatchFilter)
		{
			this.field = field;
			this.ranges = ranges;
			this.fastMatchFilter = fastMatchFilter;
			counts = new int[ranges.Length];
		}

		public override FacetResult GetTopChildren(int topN, string dim, params string[] 
			path)
		{
			if (dim.Equals(field) == false)
			{
				throw new ArgumentException("invalid dim \"" + dim + "\"; should be \"" + field +
					 "\"");
			}
			if (path.Length != 0)
			{
				throw new ArgumentException("path.length should be 0");
			}
			LabelAndValue[] labelValues = new LabelAndValue[counts.Length];
			for (int i = 0; i < counts.Length; i++)
			{
				labelValues[i] = new LabelAndValue(ranges[i].label, counts[i]);
			}
			return new FacetResult(dim, path, totCount, labelValues, labelValues.Length);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Number GetSpecificValue(string dim, params string[] path)
		{
			// TODO: should we impl this?
			throw new NotSupportedException();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override IList<FacetResult> GetAllDims(int topN)
		{
			return Sharpen.Collections.SingletonList(GetTopChildren(topN, null));
		}
	}
}
