/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Search;
using Lucene.Net.Search.Spans;


namespace Lucene.Net.Search.Spans
{
	/// <summary>subclass of TestSimpleExplanations that verifies non matches.</summary>
	/// <remarks>subclass of TestSimpleExplanations that verifies non matches.</remarks>
	public class TestSpanExplanationsOfNonMatches : TestSpanExplanations
	{
		/// <summary>Overrides superclass to ignore matches and focus on non-matches</summary>
		/// <seealso cref="Lucene.Net.Search.CheckHits.CheckNoMatchExplanations(Lucene.Net.Search.Query, string, Lucene.Net.Search.IndexSearcher, int[])
		/// 	">Lucene.Net.Search.CheckHits.CheckNoMatchExplanations(Lucene.Net.Search.Query, string, Lucene.Net.Search.IndexSearcher, int[])
		/// 	</seealso>
		/// <exception cref="System.Exception"></exception>
		public override void Qtest(Query q, int[] expDocNrs)
		{
			CheckHits.CheckNoMatchExplanations(q, FIELD, searcher, expDocNrs);
		}
	}
}
