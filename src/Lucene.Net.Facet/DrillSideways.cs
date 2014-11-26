/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Facet;
using Lucene.Net.Facet.Sortedset;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Search;
using Sharpen;

namespace Lucene.Net.Facet
{
	/// <summary>
	/// Computes drill down and sideways counts for the provided
	/// <see cref="DrillDownQuery">DrillDownQuery</see>
	/// .  Drill sideways counts include
	/// alternative values/aggregates for the drill-down
	/// dimensions so that a dimension does not disappear after
	/// the user drills down into it.
	/// <p> Use one of the static search
	/// methods to do the search, and then get the hits and facet
	/// results from the returned
	/// <see cref="DrillSidewaysResult">DrillSidewaysResult</see>
	/// .
	/// <p><b>NOTE</b>: this allocates one
	/// <see cref="FacetsCollector">FacetsCollector</see>
	/// for each drill-down, plus one.  If your
	/// index has high number of facet labels then this will
	/// multiply your memory usage.
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class DrillSideways
	{
		/// <summary>
		/// <see cref="Lucene.Net.Search.IndexSearcher">Lucene.Net.Search.IndexSearcher
		/// 	</see>
		/// passed to constructor.
		/// </summary>
		protected internal readonly IndexSearcher searcher;

		/// <summary>
		/// <see cref="Lucene.Net.Facet.Taxonomy.TaxonomyReader">Lucene.Net.Facet.Taxonomy.TaxonomyReader
		/// 	</see>
		/// passed to constructor.
		/// </summary>
		protected internal readonly TaxonomyReader taxoReader;

		/// <summary>
		/// <see cref="Lucene.Net.Facet.Sortedset.SortedSetDocValuesReaderState">Lucene.Net.Facet.Sortedset.SortedSetDocValuesReaderState
		/// 	</see>
		/// passed to
		/// constructor; can be null.
		/// </summary>
		protected internal readonly SortedSetDocValuesReaderState state;

		/// <summary>
		/// <see cref="FacetsConfig">FacetsConfig</see>
		/// passed to constructor.
		/// </summary>
		protected internal readonly FacetsConfig config;

		/// <summary>
		/// Create a new
		/// <code>DrillSideways</code>
		/// instance.
		/// </summary>
		public DrillSideways(IndexSearcher searcher, FacetsConfig config, TaxonomyReader 
			taxoReader) : this(searcher, config, taxoReader, null)
		{
		}

		/// <summary>
		/// Create a new
		/// <code>DrillSideways</code>
		/// instance, assuming the categories were
		/// indexed with
		/// <see cref="Lucene.Net.Facet.Sortedset.SortedSetDocValuesFacetField">Lucene.Net.Facet.Sortedset.SortedSetDocValuesFacetField
		/// 	</see>
		/// .
		/// </summary>
		public DrillSideways(IndexSearcher searcher, FacetsConfig config, SortedSetDocValuesReaderState
			 state) : this(searcher, config, null, state)
		{
		}

		/// <summary>
		/// Create a new
		/// <code>DrillSideways</code>
		/// instance, where some
		/// dimensions were indexed with
		/// <see cref="Lucene.Net.Facet.Sortedset.SortedSetDocValuesFacetField">Lucene.Net.Facet.Sortedset.SortedSetDocValuesFacetField
		/// 	</see>
		/// and others were indexed
		/// with
		/// <see cref="FacetField">FacetField</see>
		/// .
		/// </summary>
		public DrillSideways(IndexSearcher searcher, FacetsConfig config, TaxonomyReader 
			taxoReader, SortedSetDocValuesReaderState state)
		{
			this.searcher = searcher;
			this.config = config;
			this.taxoReader = taxoReader;
			this.state = state;
		}

		/// <summary>
		/// Subclass can override to customize per-dim Facets
		/// impl.
		/// </summary>
		/// <remarks>
		/// Subclass can override to customize per-dim Facets
		/// impl.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual Facets BuildFacetsResult(FacetsCollector drillDowns, FacetsCollector
			[] drillSideways, string[] drillSidewaysDims)
		{
			Facets drillDownFacets;
			IDictionary<string, Facets> drillSidewaysFacets = new Dictionary<string, Facets>(
				);
			if (taxoReader != null)
			{
				drillDownFacets = new FastTaxonomyFacetCounts(taxoReader, config, drillDowns);
				if (drillSideways != null)
				{
					for (int i = 0; i < drillSideways.Length; i++)
					{
						drillSidewaysFacets.Put(drillSidewaysDims[i], new FastTaxonomyFacetCounts(taxoReader
							, config, drillSideways[i]));
					}
				}
			}
			else
			{
				drillDownFacets = new SortedSetDocValuesFacetCounts(state, drillDowns);
				if (drillSideways != null)
				{
					for (int i = 0; i < drillSideways.Length; i++)
					{
						drillSidewaysFacets.Put(drillSidewaysDims[i], new SortedSetDocValuesFacetCounts(state
							, drillSideways[i]));
					}
				}
			}
			if (drillSidewaysFacets.IsEmpty())
			{
				return drillDownFacets;
			}
			else
			{
				return new MultiFacets(drillSidewaysFacets, drillDownFacets);
			}
		}

		/// <summary>
		/// Search, collecting hits with a
		/// <see cref="Lucene.Net.Search.Collector">Lucene.Net.Search.Collector
		/// 	</see>
		/// , and
		/// computing drill down and sideways counts.
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual DrillSideways.DrillSidewaysResult Search(DrillDownQuery query, Collector
			 hitCollector)
		{
			IDictionary<string, int> drillDownDims = query.GetDims();
			FacetsCollector drillDownCollector = new FacetsCollector();
			if (drillDownDims.IsEmpty())
			{
				// There are no drill-down dims, so there is no
				// drill-sideways to compute:
				searcher.Search(query, MultiCollector.Wrap(hitCollector, drillDownCollector));
				return new DrillSideways.DrillSidewaysResult(BuildFacetsResult(drillDownCollector
					, null, null), null);
			}
			BooleanQuery ddq = query.GetBooleanQuery();
			BooleanClause[] clauses = ddq.GetClauses();
			Query baseQuery;
			int startClause;
			if (clauses.Length == drillDownDims.Count)
			{
				// TODO: we could optimize this pure-browse case by
				// making a custom scorer instead:
				baseQuery = new MatchAllDocsQuery();
				startClause = 0;
			}
			else
			{
				clauses.Length == 1 + drillDownDims.Count = clauses[0].GetQuery();
				startClause = 1;
			}
			FacetsCollector[] drillSidewaysCollectors = new FacetsCollector[drillDownDims.Count
				];
			for (int i = 0; i < drillSidewaysCollectors.Length; i++)
			{
				drillSidewaysCollectors[i] = new FacetsCollector();
			}
			Query[] drillDownQueries = new Query[clauses.Length - startClause];
			for (int i_1 = startClause; i_1 < clauses.Length; i_1++)
			{
				drillDownQueries[i_1 - startClause] = clauses[i_1].GetQuery();
			}
			DrillSidewaysQuery dsq = new DrillSidewaysQuery(baseQuery, drillDownCollector, drillSidewaysCollectors
				, drillDownQueries, ScoreSubDocsAtOnce());
			searcher.Search(dsq, hitCollector);
			return new DrillSideways.DrillSidewaysResult(BuildFacetsResult(drillDownCollector
				, drillSidewaysCollectors, Sharpen.Collections.ToArray(drillDownDims.Keys, new string
				[drillDownDims.Count])), null);
		}

		/// <summary>
		/// Search, sorting by
		/// <see cref="Lucene.Net.Search.Sort">Lucene.Net.Search.Sort</see>
		/// , and computing
		/// drill down and sideways counts.
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual DrillSideways.DrillSidewaysResult Search(DrillDownQuery query, Filter
			 filter, FieldDoc after, int topN, Sort sort, bool doDocScores, bool doMaxScore)
		{
			if (filter != null)
			{
				query = new DrillDownQuery(config, filter, query);
			}
			if (sort != null)
			{
				int limit = searcher.GetIndexReader().MaxDoc();
				if (limit == 0)
				{
					limit = 1;
				}
				// the collector does not alow numHits = 0
				topN = Math.Min(topN, limit);
				TopFieldCollector hitCollector = TopFieldCollector.Create(sort, topN, after, true
					, doDocScores, doMaxScore, true);
				DrillSideways.DrillSidewaysResult r = Search(query, hitCollector);
				return new DrillSideways.DrillSidewaysResult(r.facets, hitCollector.TopDocs());
			}
			else
			{
				return Search(after, query, topN);
			}
		}

		/// <summary>
		/// Search, sorting by score, and computing
		/// drill down and sideways counts.
		/// </summary>
		/// <remarks>
		/// Search, sorting by score, and computing
		/// drill down and sideways counts.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual DrillSideways.DrillSidewaysResult Search(DrillDownQuery query, int
			 topN)
		{
			return Search(null, query, topN);
		}

		/// <summary>
		/// Search, sorting by score, and computing
		/// drill down and sideways counts.
		/// </summary>
		/// <remarks>
		/// Search, sorting by score, and computing
		/// drill down and sideways counts.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual DrillSideways.DrillSidewaysResult Search(ScoreDoc after, DrillDownQuery
			 query, int topN)
		{
			int limit = searcher.GetIndexReader().MaxDoc();
			if (limit == 0)
			{
				limit = 1;
			}
			// the collector does not alow numHits = 0
			topN = Math.Min(topN, limit);
			TopScoreDocCollector hitCollector = TopScoreDocCollector.Create(topN, after, true
				);
			DrillSideways.DrillSidewaysResult r = Search(query, hitCollector);
			return new DrillSideways.DrillSidewaysResult(r.facets, hitCollector.TopDocs());
		}

		/// <summary>
		/// Override this and return true if your collector
		/// (e.g.,
		/// <code>ToParentBlockJoinCollector</code>
		/// ) expects all
		/// sub-scorers to be positioned on the document being
		/// collected.  This will cause some performance loss;
		/// default is false.  Note that if you return true from
		/// this method (in a subclass) be sure your collector
		/// also returns false from
		/// <see cref="Lucene.Net.Search.Collector.AcceptsDocsOutOfOrder()">Lucene.Net.Search.Collector.AcceptsDocsOutOfOrder()
		/// 	</see>
		/// : this will trick
		/// <code>BooleanQuery</code>
		/// into also scoring all subDocs at
		/// once.
		/// </summary>
		protected internal virtual bool ScoreSubDocsAtOnce()
		{
			return false;
		}

		/// <summary>
		/// Result of a drill sideways search, including the
		/// <see cref="Facets">Facets</see>
		/// and
		/// <see cref="Lucene.Net.Search.TopDocs">Lucene.Net.Search.TopDocs</see>
		/// .
		/// </summary>
		public class DrillSidewaysResult
		{
			/// <summary>Combined drill down & sideways results.</summary>
			/// <remarks>Combined drill down & sideways results.</remarks>
			public readonly Facets facets;

			/// <summary>Hits.</summary>
			/// <remarks>Hits.</remarks>
			public readonly TopDocs hits;

			/// <summary>Sole constructor.</summary>
			/// <remarks>Sole constructor.</remarks>
			public DrillSidewaysResult(Facets facets, TopDocs hits)
			{
				this.facets = facets;
				this.hits = hits;
			}
		}
	}
}
