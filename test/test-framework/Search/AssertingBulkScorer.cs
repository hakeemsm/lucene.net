using System;
using Lucene.Net.Search;

namespace Lucene.Net.TestFramework.Search
{
	/// <summary>Wraps a Scorer with additional checks</summary>
	public class AssertingBulkScorer : BulkScorer
	{
		private static readonly VirtualMethod<BulkScorer> SCORE_COLLECTOR = new VirtualMethod
			<BulkScorer>(typeof(BulkScorer), "score", typeof(Collector));

		private static readonly VirtualMethod<BulkScorer> SCORE_COLLECTOR_RANGE = new VirtualMethod
			<BulkScorer>(typeof(BulkScorer), "score", typeof(Collector), typeof(int));

		public static BulkScorer Wrap(Random random, BulkScorer other)
		{
			if (other == null || other isLucene.Net.TestFramework.Search.AssertingBulkScorer)
			{
				return other;
			}
			return newLucene.Net.TestFramework.Search.AssertingBulkScorer(random, other);
		}

		public static bool ShouldWrap(BulkScorer inScorer)
		{
			return SCORE_COLLECTOR.IsOverriddenAsOf(inScorer.GetType()) || SCORE_COLLECTOR_RANGE
				.IsOverriddenAsOf(inScorer.GetType());
		}

		internal readonly Random random;

		internal readonly BulkScorer @in;

		private AssertingBulkScorer(Random random, BulkScorer @in)
		{
			this.random = random;
			this.@in = @in;
		}

		public virtual BulkScorer GetIn()
		{
			return @in;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Score(Collector collector)
		{
			if (random.NextBoolean())
			{
				try
				{
					bool remaining = @in.Score(collector, DocsEnum.NO_MORE_DOCS);
				}
				catch (NotSupportedException)
				{
					//HM:revisit 
					//assert !remaining;
					@in.Score(collector);
				}
			}
			else
			{
				@in.Score(collector);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override bool Score(Collector collector, int max)
		{
			return @in.Score(collector, max);
		}

		public override string ToString()
		{
			return "AssertingBulkScorer(" + @in + ")";
		}
	}
}
