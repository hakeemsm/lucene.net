using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.TestFramework.Util;

namespace Lucene.Net.TestFramework.Search
{
	/// <summary>
	/// Helper class that adds some extra checks to ensure correct
	/// usage of
	/// <code>IndexSearcher</code>
	/// and
	/// <code>Weight</code>
	/// .
	/// </summary>
	public class AssertingIndexSearcher : IndexSearcher
	{
		internal readonly Random random;

		public AssertingIndexSearcher(Random random, IndexReader r) : base(r)
		{
			this.random = new Random(random.NextLong());
		}

		public AssertingIndexSearcher(Random random, IndexReaderContext context) : base(context
			)
		{
			this.random = new Random(random.NextLong());
		}

		public AssertingIndexSearcher(Random random, IndexReader r, TaskScheduler ex) : base(r, ex)
		{
			this.random = new Random(random.NextLong());
		}

		public AssertingIndexSearcher(Random random, IndexReaderContext context, TaskScheduler ex) : base(context, ex)
		{
			this.random = new Random(random.NextLong());
		}

		/// <summary>
		/// Ensures, that the returned
		/// <code>Weight</code>
		/// is not normalized again, which may produce wrong scores.
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public override Weight CreateNormalizedWeight(Query query)
		{
			Weight w = base.CreateNormalizedWeight(query);
			return new _AssertingWeight_60(random, w);
		}

		private sealed class _AssertingWeight_60 : AssertingWeight
		{
			public _AssertingWeight_60(Random baseArg1, Weight baseArg2) : base(baseArg1, baseArg2
				)
			{
			}

			public override void Normalize(float norm, float topLevelBoost)
			{
				throw new InvalidOperationException("Weight already normalized.");
			}

			public override float GetValueForNormalization()
			{
				throw new InvalidOperationException("Weight already normalized.");
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Query Rewrite(Query original)
		{
			// TODO: use the more sophisticated QueryUtils.check sometimes!
			QueryUtils.Check(original);
			Query rewritten = base.Rewrite(original);
			QueryUtils.Check(rewritten);
			return rewritten;
		}

		protected override Query WrapFilter(Query query, Filter filter)
		{
			if (random.NextBoolean())
			{
				return base.WrapFilter(query, filter);
			}
			return (filter == null) ? query : new FilteredQuery(query, filter, TestUtil.RandomFilterStrategy
				(random));
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected override void Search(IList<AtomicReaderContext> leaves, Weight weight, 
			Collector collector)
		{
			// TODO: shouldn't we AssertingCollector.wrap(collector) here?
			base.Search(leaves, AssertingWeight.Wrap(random, weight), collector);
		}

		public override string ToString()
		{
			return "AssertingIndexSearcher(" + base.ToString() + ")";
		}
	}
}
