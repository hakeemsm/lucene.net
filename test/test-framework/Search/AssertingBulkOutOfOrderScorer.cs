using Lucene.Net.Search;

namespace Lucene.Net.TestFramework.Search
{
	/// <summary>
	/// A crazy
	/// <see cref="BulkScorer">BulkScorer</see>
	/// that wraps another
	/// <see cref="BulkScorer">BulkScorer</see>
	/// but shuffles the order of the collected documents.
	/// </summary>
	public class AssertingBulkOutOfOrderScorer : BulkScorer
	{
		internal readonly BulkScorer @in;

		internal readonly Random random;

		public AssertingBulkOutOfOrderScorer(Random random, BulkScorer @in)
		{
			this.@in = @in;
			this.random = random;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override bool Score(Collector collector, int max)
		{
			RandomOrderCollector randomCollector = new RandomOrderCollector(random, collector
				);
			bool remaining = @in.Score(randomCollector, max);
			randomCollector.Flush();
			return remaining;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Score(Collector collector)
		{
			RandomOrderCollector randomCollector = new RandomOrderCollector(random, collector
				);
			@in.Score(randomCollector);
			randomCollector.Flush();
		}

		public override string ToString()
		{
			return "AssertingBulkOutOfOrderScorer(" + @in + ")";
		}
	}
}
