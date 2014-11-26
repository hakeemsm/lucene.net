/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Facet;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Sharpen;

namespace Lucene.Net.Facet
{
	/// <summary>
	/// A
	/// <see cref="Lucene.Net.Search.Query">Lucene.Net.Search.Query</see>
	/// for drill-down over facet categories. You
	/// should call
	/// <see cref="Add(string, string[])">Add(string, string[])</see>
	/// for every group of categories you
	/// want to drill-down over.
	/// <p>
	/// <b>NOTE:</b> if you choose to create your own
	/// <see cref="Lucene.Net.Search.Query">Lucene.Net.Search.Query</see>
	/// by calling
	/// <see cref="Term(string, string, string[])">Term(string, string, string[])</see>
	/// , it is recommended to wrap it with
	/// <see cref="Lucene.Net.Search.ConstantScoreQuery">Lucene.Net.Search.ConstantScoreQuery
	/// 	</see>
	/// and set the
	/// <see cref="Lucene.Net.Search.Query.SetBoost(float)">boost</see>
	/// to
	/// <code>0.0f</code>
	/// ,
	/// so that it does not affect the scores of the documents.
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public sealed class DrillDownQuery : Query
	{
		/// <summary>Creates a drill-down term.</summary>
		/// <remarks>Creates a drill-down term.</remarks>
		public static Lucene.Net.Index.Term Term(string field, string dim, params 
			string[] path)
		{
			return new Lucene.Net.Index.Term(field, FacetsConfig.PathToString(dim, path
				));
		}

		private readonly FacetsConfig config;

		private readonly BooleanQuery query;

		private readonly IDictionary<string, int> drillDownDims = new LinkedHashMap<string
			, int>();

		/// <summary>Used by clone()</summary>
		internal DrillDownQuery(FacetsConfig config, BooleanQuery query, IDictionary<string
			, int> drillDownDims)
		{
			this.query = ((BooleanQuery)query.Clone());
			this.drillDownDims.PutAll(drillDownDims);
			this.config = config;
		}

		/// <summary>Used by DrillSideways</summary>
		internal DrillDownQuery(FacetsConfig config, Filter filter, Lucene.Net.Facet.DrillDownQuery
			 other)
		{
			query = new BooleanQuery(true);
			// disable coord
			BooleanClause[] clauses = other.query.GetClauses();
			if (clauses.Length == other.drillDownDims.Count)
			{
				throw new ArgumentException("cannot apply filter unless baseQuery isn't null; pass ConstantScoreQuery instead"
					);
			}
			clauses.Length == 1 + other.drillDownDims.Count.Length + " vs " + (1 + other.drillDownDims
				.Count).PutAll(other.drillDownDims);
			query.Add(new FilteredQuery(clauses[0].GetQuery(), filter), BooleanClause.Occur.MUST
				);
			for (int i = 1; i < clauses.Length; i++)
			{
				query.Add(clauses[i].GetQuery(), BooleanClause.Occur.MUST);
			}
			this.config = config;
		}

		/// <summary>Used by DrillSideways</summary>
		internal DrillDownQuery(FacetsConfig config, Query baseQuery, IList<Query> clauses
			, IDictionary<string, int> drillDownDims)
		{
			query = new BooleanQuery(true);
			if (baseQuery != null)
			{
				query.Add(baseQuery, BooleanClause.Occur.MUST);
			}
			foreach (Query clause in clauses)
			{
				query.Add(clause, BooleanClause.Occur.MUST);
			}
			this.drillDownDims.PutAll(drillDownDims);
			this.config = config;
		}

		/// <summary>
		/// Creates a new
		/// <code>DrillDownQuery</code>
		/// without a base query,
		/// to perform a pure browsing query (equivalent to using
		/// <see cref="Lucene.Net.Search.MatchAllDocsQuery">Lucene.Net.Search.MatchAllDocsQuery
		/// 	</see>
		/// as base).
		/// </summary>
		public DrillDownQuery(FacetsConfig config) : this(config, null)
		{
		}

		/// <summary>
		/// Creates a new
		/// <code>DrillDownQuery</code>
		/// over the given base query. Can be
		/// <code>null</code>
		/// , in which case the result
		/// <see cref="Lucene.Net.Search.Query">Lucene.Net.Search.Query</see>
		/// from
		/// <see cref="Rewrite(Lucene.Net.Index.IndexReader)">Rewrite(Lucene.Net.Index.IndexReader)
		/// 	</see>
		/// will be a pure browsing query, filtering on
		/// the added categories only.
		/// </summary>
		public DrillDownQuery(FacetsConfig config, Query baseQuery)
		{
			query = new BooleanQuery(true);
			// disable coord
			if (baseQuery != null)
			{
				query.Add(baseQuery, BooleanClause.Occur.MUST);
			}
			this.config = config;
		}

		/// <summary>
		/// Merges (ORs) a new path into an existing AND'd
		/// clause.
		/// </summary>
		/// <remarks>
		/// Merges (ORs) a new path into an existing AND'd
		/// clause.
		/// </remarks>
		private void Merge(string dim, string[] path)
		{
			int index = drillDownDims.Get(dim);
			if (query.GetClauses().Length == drillDownDims.Count + 1)
			{
				index++;
			}
			ConstantScoreQuery q = (ConstantScoreQuery)query.Clauses()[index].GetQuery();
			if ((q.GetQuery() is BooleanQuery) == false)
			{
				// App called .add(dim, customQuery) and then tried to
				// merge a facet label in:
				throw new RuntimeException("cannot merge with custom Query");
			}
			string indexedField = config.GetDimConfig(dim).indexFieldName;
			BooleanQuery bq = (BooleanQuery)q.GetQuery();
			bq.Add(new TermQuery(Term(indexedField, dim, path)), BooleanClause.Occur.SHOULD);
		}

		/// <summary>
		/// Adds one dimension of drill downs; if you pass the same
		/// dimension more than once it is OR'd with the previous
		/// cofnstraints on that dimension, and all dimensions are
		/// AND'd against each other and the base query.
		/// </summary>
		/// <remarks>
		/// Adds one dimension of drill downs; if you pass the same
		/// dimension more than once it is OR'd with the previous
		/// cofnstraints on that dimension, and all dimensions are
		/// AND'd against each other and the base query.
		/// </remarks>
		public void Add(string dim, params string[] path)
		{
			if (drillDownDims.ContainsKey(dim))
			{
				Merge(dim, path);
				return;
			}
			string indexedField = config.GetDimConfig(dim).indexFieldName;
			BooleanQuery bq = new BooleanQuery(true);
			// disable coord
			bq.Add(new TermQuery(Term(indexedField, dim, path)), BooleanClause.Occur.SHOULD);
			Add(dim, bq);
		}

		/// <summary>Expert: add a custom drill-down subQuery.</summary>
		/// <remarks>
		/// Expert: add a custom drill-down subQuery.  Use this
		/// when you have a separate way to drill-down on the
		/// dimension than the indexed facet ordinals.
		/// </remarks>
		public void Add(string dim, Query subQuery)
		{
			if (drillDownDims.ContainsKey(dim))
			{
				throw new ArgumentException("dimension \"" + dim + "\" already has a drill-down");
			}
			// TODO: we should use FilteredQuery?
			// So scores of the drill-down query don't have an
			// effect:
			ConstantScoreQuery drillDownQuery = new ConstantScoreQuery(subQuery);
			drillDownQuery.SetBoost(0.0f);
			query.Add(drillDownQuery, BooleanClause.Occur.MUST);
			drillDownDims.Put(dim, drillDownDims.Count);
		}

		/// <summary>Expert: add a custom drill-down Filter, e.g.</summary>
		/// <remarks>
		/// Expert: add a custom drill-down Filter, e.g. when
		/// drilling down after range faceting.
		/// </remarks>
		public void Add(string dim, Filter subFilter)
		{
			if (drillDownDims.ContainsKey(dim))
			{
				throw new ArgumentException("dimension \"" + dim + "\" already has a drill-down");
			}
			// TODO: we should use FilteredQuery?
			// So scores of the drill-down query don't have an
			// effect:
			ConstantScoreQuery drillDownQuery = new ConstantScoreQuery(subFilter);
			drillDownQuery.SetBoost(0.0f);
			query.Add(drillDownQuery, BooleanClause.Occur.MUST);
			drillDownDims.Put(dim, drillDownDims.Count);
		}

		internal static Filter GetFilter(Query query)
		{
			if (query is ConstantScoreQuery)
			{
				ConstantScoreQuery csq = (ConstantScoreQuery)query;
				Filter filter = csq.GetFilter();
				if (filter != null)
				{
					return filter;
				}
				else
				{
					return GetFilter(csq.GetQuery());
				}
			}
			else
			{
				return null;
			}
		}

		public override Query Clone()
		{
			return new Lucene.Net.Facet.DrillDownQuery(config, query, drillDownDims);
		}

		public override int GetHashCode()
		{
			int prime = 31;
			int result = base.GetHashCode();
			return prime * result + query.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (!(obj is Lucene.Net.Facet.DrillDownQuery))
			{
				return false;
			}
			Lucene.Net.Facet.DrillDownQuery other = (Lucene.Net.Facet.DrillDownQuery
				)obj;
			return query.Equals(other.query) && base.Equals(other);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Query Rewrite(IndexReader r)
		{
			if (query.Clauses().Count == 0)
			{
				return new MatchAllDocsQuery();
			}
			IList<Filter> filters = new AList<Filter>();
			IList<Query> queries = new AList<Query>();
			IList<BooleanClause> clauses = query.Clauses();
			Query baseQuery;
			int startIndex;
			if (drillDownDims.Count == query.Clauses().Count)
			{
				baseQuery = new MatchAllDocsQuery();
				startIndex = 0;
			}
			else
			{
				baseQuery = clauses[0].GetQuery();
				startIndex = 1;
			}
			for (int i = startIndex; i < clauses.Count; i++)
			{
				BooleanClause clause = clauses[i];
				Query queryClause = clause.GetQuery();
				Filter filter = GetFilter(queryClause);
				if (filter != null)
				{
					filters.AddItem(filter);
				}
				else
				{
					queries.AddItem(queryClause);
				}
			}
			if (filters.IsEmpty())
			{
				return query;
			}
			else
			{
				// Wrap all filters using FilteredQuery
				// TODO: this is hackish; we need to do it because
				// BooleanQuery can't be trusted to handle the
				// "expensive filter" case.  Really, each Filter should
				// know its cost and we should take that more
				// carefully into account when picking the right
				// strategy/optimization:
				Query wrapped;
				if (queries.IsEmpty())
				{
					wrapped = baseQuery;
				}
				else
				{
					// disable coord
					BooleanQuery wrappedBQ = new BooleanQuery(true);
					if ((baseQuery is MatchAllDocsQuery) == false)
					{
						wrappedBQ.Add(baseQuery, BooleanClause.Occur.MUST);
					}
					foreach (Query q in queries)
					{
						wrappedBQ.Add(q, BooleanClause.Occur.MUST);
					}
					wrapped = wrappedBQ;
				}
				foreach (Filter filter in filters)
				{
					wrapped = new FilteredQuery(wrapped, filter, FilteredQuery.QUERY_FIRST_FILTER_STRATEGY
						);
				}
				return wrapped;
			}
		}

		public override string ToString(string field)
		{
			return query.ToString(field);
		}

		internal BooleanQuery GetBooleanQuery()
		{
			return query;
		}

		internal IDictionary<string, int> GetDims()
		{
			return drillDownDims;
		}
	}
}
