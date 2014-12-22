using System;
using Lucene.Net.Search;

namespace Lucene.Net.TestFramework.Search
{
	/// <summary>
	/// Wraps another Collector and checks that
	/// acceptsDocsOutOfOrder is respected.
	/// </summary>
	/// <remarks>
	/// Wraps another Collector and checks that
	/// acceptsDocsOutOfOrder is respected.
	/// </remarks>
	public class AssertingCollector : Collector
	{
		public static Collector Wrap(Random random, Collector other, bool inOrder)
		{
			return other isLucene.Net.TestFramework.Search.AssertingCollector ? other : newLucene.Net.TestFramework.Search.AssertingCollector
				(random, other, inOrder);
		}

		internal readonly Random random;

		internal readonly Collector @in;

		internal readonly bool inOrder;

		internal int lastCollected;

		internal AssertingCollector(Random random, Collector @in, bool inOrder)
		{
			this.random = random;
			this.@in = @in;
			this.inOrder = inOrder;
			lastCollected = -1;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void SetScorer(Scorer scorer)
		{
			@in.SetScorer(AssertingScorer.GetAssertingScorer(random, scorer));
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Collect(int doc)
		{
			if (inOrder || !AcceptsDocsOutOfOrder())
			{
			}
			 
			//assert doc > lastCollected : "Out of order : " + lastCollected + " " + doc;
			@in.Collect(doc);
			lastCollected = doc;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void SetNextReader(AtomicReaderContext context)
		{
			lastCollected = -1;
		}

		public override bool AcceptsDocsOutOfOrder()
		{
			return @in.AcceptsDocsOutOfOrder();
		}
	}
}
