/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Facet;
using Lucene.Net.Facet.Range;
using Lucene.Net.Queries.Function;
using Lucene.Net.Queries.Function.Valuesource;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Facet.Range
{
	/// <summary>
	/// <see cref="Lucene.Net.Facet.Facets">Lucene.Net.Facet.Facets</see>
	/// implementation that computes counts for
	/// dynamic double ranges from a provided
	/// <see cref="Lucene.Net.Queries.Function.ValueSource">Lucene.Net.Queries.Function.ValueSource
	/// 	</see>
	/// , using
	/// <see cref="Lucene.Net.Queries.Function.FunctionValues.DoubleVal(int)">Lucene.Net.Queries.Function.FunctionValues.DoubleVal(int)
	/// 	</see>
	/// .  Use
	/// this for dimensions that change in real-time (e.g. a
	/// relative time based dimension like "Past day", "Past 2
	/// days", etc.) or that change for each request (e.g.
	/// distance from the user's location, "&lt; 1 km", "&lt; 2 km",
	/// etc.).
	/// <p> If you had indexed your field using
	/// <see cref="Lucene.Net.Document.FloatDocValuesField">Lucene.Net.Document.FloatDocValuesField
	/// 	</see>
	/// then pass
	/// <see cref="Lucene.Net.Queries.Function.Valuesource.FloatFieldSource">Lucene.Net.Queries.Function.Valuesource.FloatFieldSource
	/// 	</see>
	/// as the
	/// <see cref="Lucene.Net.Queries.Function.ValueSource">Lucene.Net.Queries.Function.ValueSource
	/// 	</see>
	/// ; if you used
	/// <see cref="Lucene.Net.Document.DoubleDocValuesField">Lucene.Net.Document.DoubleDocValuesField
	/// 	</see>
	/// then pass
	/// <see cref="Lucene.Net.Queries.Function.Valuesource.DoubleFieldSource">Lucene.Net.Queries.Function.Valuesource.DoubleFieldSource
	/// 	</see>
	/// (this is the default used when you
	/// pass just a the field name).
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class DoubleRangeFacetCounts : RangeFacetCounts
	{
		/// <summary>
		/// Create
		/// <code>RangeFacetCounts</code>
		/// , using
		/// <see cref="Lucene.Net.Queries.Function.Valuesource.DoubleFieldSource">Lucene.Net.Queries.Function.Valuesource.DoubleFieldSource
		/// 	</see>
		/// from the specified field.
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public DoubleRangeFacetCounts(string field, FacetsCollector hits, params DoubleRange
			[] ranges) : this(field, new DoubleFieldSource(field), hits, ranges)
		{
		}

		/// <summary>
		/// Create
		/// <code>RangeFacetCounts</code>
		/// , using the provided
		/// <see cref="Lucene.Net.Queries.Function.ValueSource">Lucene.Net.Queries.Function.ValueSource
		/// 	</see>
		/// .
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public DoubleRangeFacetCounts(string field, ValueSource valueSource, FacetsCollector
			 hits, params DoubleRange[] ranges) : this(field, valueSource, hits, null, ranges
			)
		{
		}

		/// <summary>
		/// Create
		/// <code>RangeFacetCounts</code>
		/// , using the provided
		/// <see cref="Lucene.Net.Queries.Function.ValueSource">Lucene.Net.Queries.Function.ValueSource
		/// 	</see>
		/// , and using the provided Filter as
		/// a fastmatch: only documents passing the filter are
		/// checked for the matching ranges.  The filter must be
		/// random access (implement
		/// <see cref="Lucene.Net.Search.DocIdSet.Bits()">Lucene.Net.Search.DocIdSet.Bits()
		/// 	</see>
		/// ).
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public DoubleRangeFacetCounts(string field, ValueSource valueSource, FacetsCollector
			 hits, Filter fastMatchFilter, params DoubleRange[] ranges) : base(field, ranges
			, fastMatchFilter)
		{
			// javadocs
			// javadocs
			// javadocs
			Count(valueSource, hits.GetMatchingDocs());
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void Count(ValueSource valueSource, IList<FacetsCollector.MatchingDocs> matchingDocs
			)
		{
			DoubleRange[] ranges = (DoubleRange[])this.ranges;
			LongRange[] longRanges = new LongRange[ranges.Length];
			for (int i = 0; i < ranges.Length; i++)
			{
				DoubleRange range = ranges[i];
				longRanges[i] = new LongRange(range.label, NumericUtils.DoubleToSortableLong(range
					.minIncl), true, NumericUtils.DoubleToSortableLong(range.maxIncl), true);
			}
			LongRangeCounter counter = new LongRangeCounter(longRanges);
			int missingCount = 0;
			foreach (FacetsCollector.MatchingDocs hits in matchingDocs)
			{
				FunctionValues fv = valueSource.GetValues(Sharpen.Collections.EmptyMap(), hits.context
					);
				totCount += hits.totalHits;
				Bits bits;
				if (fastMatchFilter != null)
				{
					DocIdSet dis = fastMatchFilter.GetDocIdSet(hits.context, null);
					if (dis == null)
					{
						// No documents match
						continue;
					}
					bits = dis.Bits();
					if (bits == null)
					{
						throw new ArgumentException("fastMatchFilter does not implement DocIdSet.bits");
					}
				}
				else
				{
					bits = null;
				}
				DocIdSetIterator docs = hits.bits.Iterator();
				int doc;
				while ((doc = docs.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
				{
					if (bits != null && bits.Get(doc) == false)
					{
						doc++;
						continue;
					}
					// Skip missing docs:
					if (fv.Exists(doc))
					{
						counter.Add(NumericUtils.DoubleToSortableLong(fv.DoubleVal(doc)));
					}
					else
					{
						missingCount++;
					}
				}
			}
			missingCount += counter.FillCounts(counts);
			totCount -= missingCount;
		}
	}
}
