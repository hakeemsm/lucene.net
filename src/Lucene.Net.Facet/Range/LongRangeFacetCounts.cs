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
	/// dynamic long ranges from a provided
	/// <see cref="Lucene.Net.Queries.Function.ValueSource">Lucene.Net.Queries.Function.ValueSource
	/// 	</see>
	/// ,
	/// using
	/// <see cref="Lucene.Net.Queries.Function.FunctionValues.LongVal(int)">Lucene.Net.Queries.Function.FunctionValues.LongVal(int)
	/// 	</see>
	/// .  Use
	/// this for dimensions that change in real-time (e.g. a
	/// relative time based dimension like "Past day", "Past 2
	/// days", etc.) or that change for each request (e.g.
	/// distance from the user's location, "&lt; 1 km", "&lt; 2 km",
	/// etc.).
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class LongRangeFacetCounts : RangeFacetCounts
	{
		/// <summary>
		/// Create
		/// <code>LongRangeFacetCounts</code>
		/// , using
		/// <see cref="Lucene.Net.Queries.Function.Valuesource.LongFieldSource">Lucene.Net.Queries.Function.Valuesource.LongFieldSource
		/// 	</see>
		/// from the specified field.
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public LongRangeFacetCounts(string field, FacetsCollector hits, params LongRange[]
			 ranges) : this(field, new LongFieldSource(field), hits, ranges)
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
		public LongRangeFacetCounts(string field, ValueSource valueSource, FacetsCollector
			 hits, params LongRange[] ranges) : this(field, valueSource, hits, null, ranges)
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
		public LongRangeFacetCounts(string field, ValueSource valueSource, FacetsCollector
			 hits, Filter fastMatchFilter, params LongRange[] ranges) : base(field, ranges, 
			fastMatchFilter)
		{
			Count(valueSource, hits.GetMatchingDocs());
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void Count(ValueSource valueSource, IList<FacetsCollector.MatchingDocs> matchingDocs
			)
		{
			LongRange[] ranges = (LongRange[])this.ranges;
			LongRangeCounter counter = new LongRangeCounter(ranges);
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
						counter.Add(fv.LongVal(doc));
					}
					else
					{
						missingCount++;
					}
				}
			}
			int x = counter.FillCounts(counts);
			missingCount += x;
			//System.out.println("totCount " + totCount + " missingCount " + counter.missingCount);
			totCount -= missingCount;
		}
	}
}
