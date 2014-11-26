namespace Lucene.Net.Search
{
	/// <summary>
	/// Re-scores the topN results (
	/// <see cref="TopDocs">TopDocs</see>
	/// ) from an original
	/// query.  See
	/// <see cref="QueryRescorer">QueryRescorer</see>
	/// for an actual
	/// implementation.  Typically, you run a low-cost
	/// first-pass query across the entire index, collecting the
	/// top few hundred hits perhaps, and then use this class to
	/// mix in a more costly second pass scoring.
	/// <p>See
	/// <see cref="QueryRescorer.Rescore(IndexSearcher, TopDocs, Query, double, int)">QueryRescorer.Rescore(IndexSearcher, TopDocs, Query, double, int)
	/// 	</see>
	/// for a simple static method to call to rescore using a 2nd
	/// pass
	/// <see cref="Query">Query</see>
	/// .
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public abstract class Rescorer
	{
		/// <summary>
		/// Rescore an initial first-pass
		/// <see cref="TopDocs">TopDocs</see>
		/// .
		/// </summary>
		/// <param name="searcher">
		/// 
		/// <see cref="IndexSearcher">IndexSearcher</see>
		/// used to produce the
		/// first pass topDocs
		/// </param>
		/// <param name="firstPassTopDocs">
		/// Hits from the first pass
		/// search.  It's very important that these hits were
		/// produced by the provided searcher; otherwise the doc
		/// IDs will not match!
		/// </param>
		/// <param name="topN">How many re-scored hits to return</param>
		/// <exception cref="System.IO.IOException"></exception>
		public abstract TopDocs Rescore(IndexSearcher searcher, TopDocs firstPassTopDocs, int topN);

		/// <summary>
		/// Explains how the score for the specified document was
		/// computed.
		/// </summary>
		/// <remarks>
		/// Explains how the score for the specified document was
		/// computed.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public abstract Explanation Explain(IndexSearcher searcher, Explanation firstPassExplanation, int docID);
	}
}
