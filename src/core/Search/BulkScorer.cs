namespace Lucene.Net.Search
{
	/// <summary>
	/// This class is used to score a range of documents at
	/// once, and is returned by
	/// <see cref="BulkScorer
	/// 	">Weight.BulkScorer(Lucene.Net.Index.AtomicReaderContext, bool, Lucene.Net.Util.Bits)
	/// 	</see>
	/// .  Only
	/// queries that have a more optimized means of scoring
	/// across a range of documents need to override this.
	/// Otherwise, a default implementation is wrapped around
	/// the
	/// <see cref="Scorer">Scorer</see>
	/// returned by
	/// <see cref="Weight.Scorer(Lucene.Net.Index.AtomicReaderContext, Lucene.Net.Util.Bits)
	/// 	">Weight.Scorer(Lucene.Net.Index.AtomicReaderContext, Lucene.Net.Util.Bits)
	/// 	</see>
	/// .
	/// </summary>
	public abstract class BulkScorer
	{
		/// <summary>Scores and collects all matching documents.</summary>
		/// <remarks>Scores and collects all matching documents.</remarks>
		/// <param name="collector">The collector to which all matching documents are passed.
		/// 	</param>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void Score(Collector collector)
		{
			Score(collector, int.MaxValue);
		}

		/// <summary>Collects matching documents in a range.</summary>
		/// <remarks>Collects matching documents in a range.</remarks>
		/// <param name="collector">The collector to which all matching documents are passed.
		/// 	</param>
		/// <param name="max">Score up to, but not including, this doc</param>
		/// <returns>true if more matching documents may remain.</returns>
		/// <exception cref="System.IO.IOException"></exception>
		public abstract bool Score(Collector collector, int max);
	}
}
