/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Facet;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Facet
{
	/// <summary>
	/// Only purpose is to punch through and return a
	/// DrillSidewaysScorer
	/// </summary>
	internal class DrillSidewaysQuery : Query
	{
		internal readonly Query baseQuery;

		internal readonly Collector drillDownCollector;

		internal readonly Collector[] drillSidewaysCollectors;

		internal readonly Query[] drillDownQueries;

		internal readonly bool scoreSubDocsAtOnce;

		internal DrillSidewaysQuery(Query baseQuery, Collector drillDownCollector, Collector
			[] drillSidewaysCollectors, Query[] drillDownQueries, bool scoreSubDocsAtOnce)
		{
			this.baseQuery = baseQuery;
			this.drillDownCollector = drillDownCollector;
			this.drillSidewaysCollectors = drillSidewaysCollectors;
			this.drillDownQueries = drillDownQueries;
			this.scoreSubDocsAtOnce = scoreSubDocsAtOnce;
		}

		public override string ToString(string field)
		{
			return "DrillSidewaysQuery";
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Query Rewrite(IndexReader reader)
		{
			Query newQuery = baseQuery;
			while (true)
			{
				Query rewrittenQuery = newQuery.Rewrite(reader);
				if (rewrittenQuery == newQuery)
				{
					break;
				}
				newQuery = rewrittenQuery;
			}
			if (newQuery == baseQuery)
			{
				return this;
			}
			else
			{
				return new Lucene.Net.Facet.DrillSidewaysQuery(newQuery, drillDownCollector
					, drillSidewaysCollectors, drillDownQueries, scoreSubDocsAtOnce);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Weight CreateWeight(IndexSearcher searcher)
		{
			Weight baseWeight = baseQuery.CreateWeight(searcher);
			object[] drillDowns = new object[drillDownQueries.Length];
			for (int dim = 0; dim < drillDownQueries.Length; dim++)
			{
				Query query = drillDownQueries[dim];
				Filter filter = DrillDownQuery.GetFilter(query);
				if (filter != null)
				{
					drillDowns[dim] = filter;
				}
				else
				{
					// TODO: would be nice if we could say "we will do no
					// scoring" here....
					drillDowns[dim] = searcher.Rewrite(query).CreateWeight(searcher);
				}
			}
			return new _Weight_92(this, baseWeight, drillDowns);
		}

		private sealed class _Weight_92 : Weight
		{
			public _Weight_92(DrillSidewaysQuery _enclosing, Weight baseWeight, object[] drillDowns
				)
			{
				this._enclosing = _enclosing;
				this.baseWeight = baseWeight;
				this.drillDowns = drillDowns;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override Explanation Explain(AtomicReaderContext context, int doc)
			{
				return baseWeight.Explain(context, doc);
			}

			public override Query GetQuery()
			{
				return this._enclosing.baseQuery;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override float GetValueForNormalization()
			{
				return baseWeight.GetValueForNormalization();
			}

			public override void Normalize(float norm, float topLevelBoost)
			{
				baseWeight.Normalize(norm, topLevelBoost);
			}

			public override bool ScoresDocsOutOfOrder()
			{
				// TODO: would be nice if AssertingIndexSearcher
				// confirmed this for us
				return false;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override Scorer Scorer(AtomicReaderContext context, Bits acceptDocs)
			{
				// We can only run as a top scorer:
				throw new NotSupportedException();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override BulkScorer BulkScorer(AtomicReaderContext context, bool scoreDocsInOrder
				, Bits acceptDocs)
			{
				// TODO: it could be better if we take acceptDocs
				// into account instead of baseScorer?
				Scorer baseScorer = baseWeight.Scorer(context, acceptDocs);
				DrillSidewaysScorer.DocsAndCost[] dims = new DrillSidewaysScorer.DocsAndCost[drillDowns
					.Length];
				int nullCount = 0;
				for (int dim = 0; dim < dims.Length; dim++)
				{
					dims[dim] = new DrillSidewaysScorer.DocsAndCost();
					dims[dim].sidewaysCollector = this._enclosing.drillSidewaysCollectors[dim];
					if (drillDowns[dim] is Filter)
					{
						// Pass null for acceptDocs because we already
						// passed it to baseScorer and baseScorer is
						// MUST'd here
						DocIdSet dis = ((Filter)drillDowns[dim]).GetDocIdSet(context, null);
						if (dis == null)
						{
							continue;
						}
						Bits bits = dis.Bits();
						if (bits != null)
						{
							// TODO: this logic is too naive: the
							// existence of bits() in DIS today means
							// either "I'm a cheap FixedBitSet so apply me down
							// low as you decode the postings" or "I'm so
							// horribly expensive so apply me after all
							// other Query/Filter clauses pass"
							// Filter supports random access; use that to
							// prevent .advance() on costly filters:
							dims[dim].bits = bits;
						}
						else
						{
							// TODO: Filter needs to express its expected
							// cost somehow, before pulling the iterator;
							// we should use that here to set the order to
							// check the filters:
							DocIdSetIterator disi = dis.Iterator();
							if (disi == null)
							{
								nullCount++;
								continue;
							}
							dims[dim].disi = disi;
						}
					}
					else
					{
						DocIdSetIterator disi = ((Weight)drillDowns[dim]).Scorer(context, null);
						if (disi == null)
						{
							nullCount++;
							continue;
						}
						dims[dim].disi = disi;
					}
				}
				// If more than one dim has no matches, then there
				// are no hits nor drill-sideways counts.  Or, if we
				// have only one dim and that dim has no matches,
				// same thing.
				//if (nullCount > 1 || (nullCount == 1 && dims.length == 1)) {
				if (nullCount > 1)
				{
					return null;
				}
				// Sort drill-downs by most restrictive first:
				Arrays.Sort(dims);
				if (baseScorer == null)
				{
					return null;
				}
				return new DrillSidewaysScorer(context, baseScorer, this._enclosing.drillDownCollector
					, dims, this._enclosing.scoreSubDocsAtOnce);
			}

			private readonly DrillSidewaysQuery _enclosing;

			private readonly Weight baseWeight;

			private readonly object[] drillDowns;
		}

		// TODO: these should do "deeper" equals/hash on the 2-D drillDownTerms array
		public override int GetHashCode()
		{
			int prime = 31;
			int result = base.GetHashCode();
			result = prime * result + ((baseQuery == null) ? 0 : baseQuery.GetHashCode());
			result = prime * result + ((drillDownCollector == null) ? 0 : drillDownCollector.
				GetHashCode());
			result = prime * result + Arrays.HashCode(drillDownQueries);
			result = prime * result + Arrays.HashCode(drillSidewaysCollectors);
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
			Lucene.Net.Facet.DrillSidewaysQuery other = (Lucene.Net.Facet.DrillSidewaysQuery
				)obj;
			if (baseQuery == null)
			{
				if (other.baseQuery != null)
				{
					return false;
				}
			}
			else
			{
				if (!baseQuery.Equals(other.baseQuery))
				{
					return false;
				}
			}
			if (drillDownCollector == null)
			{
				if (other.drillDownCollector != null)
				{
					return false;
				}
			}
			else
			{
				if (!drillDownCollector.Equals(other.drillDownCollector))
				{
					return false;
				}
			}
			if (!Arrays.Equals(drillDownQueries, other.drillDownQueries))
			{
				return false;
			}
			if (!Arrays.Equals(drillSidewaysCollectors, other.drillSidewaysCollectors))
			{
				return false;
			}
			return true;
		}
	}
}
