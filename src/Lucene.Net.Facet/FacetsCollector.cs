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
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Facet
{
	/// <summary>Collects hits for subsequent faceting.</summary>
	/// <remarks>
	/// Collects hits for subsequent faceting.  Once you've run
	/// a search and collect hits into this, instantiate one of
	/// the
	/// <see cref="Facets">Facets</see>
	/// subclasses to do the facet
	/// counting.  Use the
	/// <code>search</code>
	/// utility methods to
	/// perform an "ordinary" search but also collect into a
	/// <see cref="Lucene.Net.Search.Collector">Lucene.Net.Search.Collector
	/// 	</see>
	/// .
	/// </remarks>
	public class FacetsCollector : Collector
	{
		private AtomicReaderContext context;

		private Scorer scorer;

		private int totalHits;

		private float[] scores;

		private readonly bool keepScores;

		private readonly IList<FacetsCollector.MatchingDocs> matchingDocs = new AList<FacetsCollector.MatchingDocs
			>();

		private FacetsCollector.Docs docs;

		/// <summary>
		/// Used during collection to record matching docs and then return a
		/// <see cref="Lucene.Net.Search.DocIdSet">Lucene.Net.Search.DocIdSet</see>
		/// that contains them.
		/// </summary>
		protected internal abstract class Docs
		{
			/// <summary>Solr constructor.</summary>
			/// <remarks>Solr constructor.</remarks>
			public Docs()
			{
			}

			/// <summary>Record the given document.</summary>
			/// <remarks>Record the given document.</remarks>
			/// <exception cref="System.IO.IOException"></exception>
			public abstract void AddDoc(int docId);

			/// <summary>
			/// Return the
			/// <see cref="Lucene.Net.Search.DocIdSet">Lucene.Net.Search.DocIdSet</see>
			/// which contains all the recorded docs.
			/// </summary>
			public abstract DocIdSet GetDocIdSet();
		}

		/// <summary>
		/// Holds the documents that were matched in the
		/// <see cref="Lucene.Net.Index.AtomicReaderContext">Lucene.Net.Index.AtomicReaderContext
		/// 	</see>
		/// .
		/// If scores were required, then
		/// <code>scores</code>
		/// is not null.
		/// </summary>
		public sealed class MatchingDocs
		{
			/// <summary>Context for this segment.</summary>
			/// <remarks>Context for this segment.</remarks>
			public readonly AtomicReaderContext context;

			/// <summary>Which documents were seen.</summary>
			/// <remarks>Which documents were seen.</remarks>
			public readonly DocIdSet bits;

			/// <summary>Non-sparse scores array.</summary>
			/// <remarks>Non-sparse scores array.</remarks>
			public readonly float[] scores;

			/// <summary>Total number of hits</summary>
			public readonly int totalHits;

			/// <summary>Sole constructor.</summary>
			/// <remarks>Sole constructor.</remarks>
			public MatchingDocs(AtomicReaderContext context, DocIdSet bits, int totalHits, float
				[] scores)
			{
				this.context = context;
				this.bits = bits;
				this.scores = scores;
				this.totalHits = totalHits;
			}
		}

		/// <summary>Default constructor</summary>
		public FacetsCollector() : this(false)
		{
		}

		/// <summary>
		/// Create this; if
		/// <code>keepScores</code>
		/// is true then a
		/// float[] is allocated to hold score of all hits.
		/// </summary>
		public FacetsCollector(bool keepScores)
		{
			this.keepScores = keepScores;
		}

		/// <summary>
		/// Creates a
		/// <see cref="Docs">Docs</see>
		/// to record hits. The default uses
		/// <see cref="Lucene.Net.Util.FixedBitSet">Lucene.Net.Util.FixedBitSet
		/// 	</see>
		/// to record hits and you can override to e.g. record the docs in your own
		/// <see cref="Lucene.Net.Search.DocIdSet">Lucene.Net.Search.DocIdSet</see>
		/// .
		/// </summary>
		protected internal virtual FacetsCollector.Docs CreateDocs(int maxDoc)
		{
			return new _Docs_120(maxDoc);
		}

		private sealed class _Docs_120 : FacetsCollector.Docs
		{
			public _Docs_120(int maxDoc)
			{
				this.maxDoc = maxDoc;
				this.bits = new FixedBitSet(maxDoc);
			}

			private readonly FixedBitSet bits;

			/// <exception cref="System.IO.IOException"></exception>
			public override void AddDoc(int docId)
			{
				this.bits.Set(docId);
			}

			public override DocIdSet GetDocIdSet()
			{
				return this.bits;
			}

			private readonly int maxDoc;
		}

		/// <summary>True if scores were saved.</summary>
		/// <remarks>True if scores were saved.</remarks>
		public bool GetKeepScores()
		{
			return keepScores;
		}

		/// <summary>
		/// Returns the documents matched by the query, one
		/// <see cref="MatchingDocs">MatchingDocs</see>
		/// per
		/// visited segment.
		/// </summary>
		public virtual IList<FacetsCollector.MatchingDocs> GetMatchingDocs()
		{
			if (docs != null)
			{
				matchingDocs.AddItem(new FacetsCollector.MatchingDocs(this.context, docs.GetDocIdSet
					(), totalHits, scores));
				docs = null;
				scores = null;
				context = null;
			}
			return matchingDocs;
		}

		public sealed override bool AcceptsDocsOutOfOrder()
		{
			// If we are keeping scores then we require in-order
			// because we append each score to the float[] and
			// expect that they correlate in order to the hits:
			return keepScores == false;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public sealed override void Collect(int doc)
		{
			docs.AddDoc(doc);
			if (keepScores)
			{
				if (totalHits >= scores.Length)
				{
					float[] newScores = new float[ArrayUtil.Oversize(totalHits + 1, 4)];
					System.Array.Copy(scores, 0, newScores, 0, totalHits);
					scores = newScores;
				}
				scores[totalHits] = scorer.Score();
			}
			totalHits++;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public sealed override void SetScorer(Scorer scorer)
		{
			this.scorer = scorer;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public sealed override void SetNextReader(AtomicReaderContext context)
		{
			if (docs != null)
			{
				matchingDocs.AddItem(new FacetsCollector.MatchingDocs(this.context, docs.GetDocIdSet
					(), totalHits, scores));
			}
			docs = CreateDocs(((AtomicReader)context.Reader()).MaxDoc());
			totalHits = 0;
			if (keepScores)
			{
				scores = new float[64];
			}
			// some initial size
			this.context = context;
		}

		/// <summary>
		/// Utility method, to search and also collect all hits
		/// into the provided
		/// <see cref="Lucene.Net.Search.Collector">Lucene.Net.Search.Collector
		/// 	</see>
		/// .
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public static TopDocs Search(IndexSearcher searcher, Query q, int n, Collector fc
			)
		{
			return DoSearch(searcher, null, q, null, n, null, false, false, fc);
		}

		/// <summary>
		/// Utility method, to search and also collect all hits
		/// into the provided
		/// <see cref="Lucene.Net.Search.Collector">Lucene.Net.Search.Collector
		/// 	</see>
		/// .
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public static TopDocs Search(IndexSearcher searcher, Query q, Filter filter, int 
			n, Collector fc)
		{
			return DoSearch(searcher, null, q, filter, n, null, false, false, fc);
		}

		/// <summary>
		/// Utility method, to search and also collect all hits
		/// into the provided
		/// <see cref="Lucene.Net.Search.Collector">Lucene.Net.Search.Collector
		/// 	</see>
		/// .
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public static TopFieldDocs Search(IndexSearcher searcher, Query q, Filter filter, 
			int n, Sort sort, Collector fc)
		{
			if (sort == null)
			{
				throw new ArgumentException("sort must not be null");
			}
			return (TopFieldDocs)DoSearch(searcher, null, q, filter, n, sort, false, false, fc
				);
		}

		/// <summary>
		/// Utility method, to search and also collect all hits
		/// into the provided
		/// <see cref="Lucene.Net.Search.Collector">Lucene.Net.Search.Collector
		/// 	</see>
		/// .
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public static TopFieldDocs Search(IndexSearcher searcher, Query q, Filter filter, 
			int n, Sort sort, bool doDocScores, bool doMaxScore, Collector fc)
		{
			if (sort == null)
			{
				throw new ArgumentException("sort must not be null");
			}
			return (TopFieldDocs)DoSearch(searcher, null, q, filter, n, sort, doDocScores, doMaxScore
				, fc);
		}

		/// <summary>
		/// Utility method, to search and also collect all hits
		/// into the provided
		/// <see cref="Lucene.Net.Search.Collector">Lucene.Net.Search.Collector
		/// 	</see>
		/// .
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual TopDocs SearchAfter(IndexSearcher searcher, ScoreDoc after, Query 
			q, int n, Collector fc)
		{
			return DoSearch(searcher, after, q, null, n, null, false, false, fc);
		}

		/// <summary>
		/// Utility method, to search and also collect all hits
		/// into the provided
		/// <see cref="Lucene.Net.Search.Collector">Lucene.Net.Search.Collector
		/// 	</see>
		/// .
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public static TopDocs SearchAfter(IndexSearcher searcher, ScoreDoc after, Query q
			, Filter filter, int n, Collector fc)
		{
			return DoSearch(searcher, after, q, filter, n, null, false, false, fc);
		}

		/// <summary>
		/// Utility method, to search and also collect all hits
		/// into the provided
		/// <see cref="Lucene.Net.Search.Collector">Lucene.Net.Search.Collector
		/// 	</see>
		/// .
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public static TopDocs SearchAfter(IndexSearcher searcher, ScoreDoc after, Query q
			, Filter filter, int n, Sort sort, Collector fc)
		{
			if (sort == null)
			{
				throw new ArgumentException("sort must not be null");
			}
			return DoSearch(searcher, after, q, filter, n, sort, false, false, fc);
		}

		/// <summary>
		/// Utility method, to search and also collect all hits
		/// into the provided
		/// <see cref="Lucene.Net.Search.Collector">Lucene.Net.Search.Collector
		/// 	</see>
		/// .
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public static TopDocs SearchAfter(IndexSearcher searcher, ScoreDoc after, Query q
			, Filter filter, int n, Sort sort, bool doDocScores, bool doMaxScore, Collector 
			fc)
		{
			if (sort == null)
			{
				throw new ArgumentException("sort must not be null");
			}
			return DoSearch(searcher, after, q, filter, n, sort, doDocScores, doMaxScore, fc);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private static TopDocs DoSearch(IndexSearcher searcher, ScoreDoc after, Query q, 
			Filter filter, int n, Sort sort, bool doDocScores, bool doMaxScore, Collector fc
			)
		{
			if (filter != null)
			{
				q = new FilteredQuery(q, filter);
			}
			int limit = searcher.GetIndexReader().MaxDoc();
			if (limit == 0)
			{
				limit = 1;
			}
			n = Math.Min(n, limit);
			if (after != null && after.doc >= limit)
			{
				throw new ArgumentException("after.doc exceeds the number of documents in the reader: after.doc="
					 + after.doc + " limit=" + limit);
			}
			TopDocsCollector<object> hitsCollector;
			if (sort != null)
			{
				if (after != null && !(after is FieldDoc))
				{
					// TODO: if we fix type safety of TopFieldDocs we can
					// remove this
					throw new ArgumentException("after must be a FieldDoc; got " + after);
				}
				bool fillFields = true;
				hitsCollector = TopFieldCollector.Create(sort, n, (FieldDoc)after, fillFields, doDocScores
					, doMaxScore, false);
			}
			else
			{
				// TODO: can we pass the right boolean for
				// in-order instead of hardwired to false...?  we'd
				// need access to the protected IS.search methods
				// taking Weight... could use reflection...
				hitsCollector = TopScoreDocCollector.Create(n, after, false);
			}
			searcher.Search(q, MultiCollector.Wrap(hitsCollector, fc));
			return hitsCollector.TopDocs();
		}
	}
}
