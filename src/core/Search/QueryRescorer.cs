using System;
using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Support;

namespace Lucene.Net.Search
{
	/// <summary>
	/// A
	/// <see cref="Rescorer">Rescorer</see>
	/// that uses a provided Query to assign
	/// scores to the first-pass hits.
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public abstract class QueryRescorer : Rescorer
	{
		private readonly Query query;

		/// <summary>
		/// Sole constructor, passing the 2nd pass query to
		/// assign scores to the 1st pass hits.
		/// </summary>
		/// <remarks>
		/// Sole constructor, passing the 2nd pass query to
		/// assign scores to the 1st pass hits.
		/// </remarks>
		public QueryRescorer(Query query)
		{
			this.query = query;
		}

		/// <summary>
		/// Implement this in a subclass to combine the first pass and
		/// second pass scores.
		/// </summary>
		/// <remarks>
		/// Implement this in a subclass to combine the first pass and
		/// second pass scores.  If secondPassMatches is false then
		/// the second pass query failed to match a hit from the
		/// first pass query, and you should ignore the
		/// secondPassScore.
		/// </remarks>
		protected internal abstract float Combine(float firstPassScore, bool secondPassMatches
			, float secondPassScore);

		/// <exception cref="System.IO.IOException"></exception>
		public override TopDocs Rescore(IndexSearcher searcher, TopDocs firstPassTopDocs, 
			int topN)
		{
			ScoreDoc[] hits = new ScoreDoc[firstPassTopDocs.ScoreDocs.Length];
			Arrays.Sort(hits, new _IComparer_54());
			IList<AtomicReaderContext> leaves = searcher.IndexReader.Leaves;
			Weight weight = searcher.CreateNormalizedWeight(query);
			// Now merge sort docIDs from hits, with reader's leaves:
			int hitUpto = 0;
			int readerUpto = -1;
			int endDoc = 0;
			int docBase = 0;
			Scorer scorer = null;
			while (hitUpto < hits.Length)
			{
				ScoreDoc hit = hits[hitUpto];
				int docID = hit.Doc;
				AtomicReaderContext readerContext = null;
				while (docID >= endDoc)
				{
					readerUpto++;
					readerContext = leaves[readerUpto];
					endDoc = readerContext.docBase + ((AtomicReader)readerContext.Reader).MaxDoc;
				}
				if (readerContext != null)
				{
					// We advanced to another segment:
					docBase = readerContext.docBase;
					scorer = weight.Scorer(readerContext, null);
				}
				int targetDoc = docID - docBase;
				int actualDoc = scorer.DocID;
				if (actualDoc < targetDoc)
				{
					actualDoc = scorer.Advance(targetDoc);
				}
				if (actualDoc == targetDoc)
				{
					// Query did match this doc:
					hit.Score = Combine(hit.Score, true, scorer.Score());
				}
				else
				{
					// Query did not match this doc:
					//HM:revisit 
					//assert actualDoc > targetDoc;
					hit.Score = Combine(hit.Score, false, 0.0f);
				}
				hitUpto++;
			}
			// TODO: we should do a partial sort (of only topN)
			// instead, but typically the number of hits is
			// smallish:
			Arrays.Sort(hits, new _IComparer_112());
			// Sort by score descending, then docID ascending:
			// This subtraction can't overflow int
			// because docIDs are >= 0:
			if (topN < hits.Length)
			{
				ScoreDoc[] subset = new ScoreDoc[topN];
				System.Array.Copy(hits, 0, subset, 0, topN);
				hits = subset;
			}
			return new TopDocs(firstPassTopDocs.TotalHits, hits, hits[0].Score);
		}

		private sealed class _IComparer_54 : IComparer<ScoreDoc>
		{
			public _IComparer_54()
			{
			}

			public int Compare(ScoreDoc a, ScoreDoc b)
			{
				return a.Doc - b.Doc;
			}
		}

		private sealed class _IComparer_112 : IComparer<ScoreDoc>
		{
		    public int Compare(ScoreDoc a, ScoreDoc b)
		    {
		        if (a.Score > b.Score)
				{
					return -1;
				}
		        if (a.Score < b.Score)
		        {
		            return 1;
		        }
		        return a.Doc - b.Doc;
		    }
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Explanation Explain(IndexSearcher searcher, Explanation firstPassExplanation
			, int docID)
		{
			Explanation secondPassExplanation = searcher.Explain(query, docID);
			float? secondPassScore = secondPassExplanation.IsMatch ? secondPassExplanation.Value : new float?();
			float score;
			score = !secondPassScore.HasValue ? Combine(firstPassExplanation.Value, false, 0.0f) : Combine(firstPassExplanation.Value, true, secondPassScore.Value);
			Explanation result = new Explanation(score, "combined first and second pass score using "
				 + GetType());
			Explanation first = new Explanation(firstPassExplanation.Value, "first pass score");
			first.AddDetail(firstPassExplanation);
			result.AddDetail(first);
			Explanation second;
			if (secondPassScore == null)
			{
				second = new Explanation(0.0f, "no second pass score");
			}
			else
			{
				second = new Explanation(secondPassScore.Value, "second pass score");
			}
			second.AddDetail(secondPassExplanation);
			result.AddDetail(second);
			return result;
		}

		/// <summary>
		/// Sugar API, calling {#rescore} using a simple linear
		/// combination of firstPassScore + weight * secondPassScore
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public static TopDocs Rescore(IndexSearcher searcher, TopDocs topDocs, Query query
			, double weight, int topN)
		{
			return new _QueryRescorer_171(weight, query).Rescore(searcher, topDocs, topN);
		}

		private sealed class _QueryRescorer_171 : Lucene.Net.Search.QueryRescorer
		{
			public _QueryRescorer_171(double weight, Query baseArg1) : base(baseArg1)
			{
				this.weight = weight;
			}

			protected internal override float Combine(float firstPassScore, bool secondPassMatches
				, float secondPassScore)
			{
				float score = firstPassScore;
				if (secondPassMatches)
				{
					score += (float)weight * secondPassScore;
				}
				return score;
			}

			private readonly double weight;
		}
	}
}
