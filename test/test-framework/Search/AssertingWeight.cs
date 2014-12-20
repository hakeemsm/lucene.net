/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.TestFramework.Index;
using Lucene.Net.TestFramework.Search;
using Lucene.Net.TestFramework.Util;
using Sharpen;

namespace Lucene.Net.TestFramework.Search
{
	internal class AssertingWeight : Weight
	{
		internal static Weight Wrap(Random random, Weight other)
		{
			return other isLucene.Net.TestFramework.Search.AssertingWeight ? other : newLucene.Net.TestFramework.Search.AssertingWeight
				(random, other);
		}

		internal readonly bool scoresDocsOutOfOrder;

		internal readonly Random random;

		internal readonly Weight @in;

		internal AssertingWeight(Random random, Weight @in)
		{
			this.random = random;
			this.@in = @in;
			scoresDocsOutOfOrder = @in.ScoresDocsOutOfOrder() || random.NextBoolean();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Explanation Explain(AtomicReaderContext context, int doc)
		{
			return @in.Explain(context, doc);
		}

		public override Query GetQuery()
		{
			return @in.GetQuery();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override float GetValueForNormalization()
		{
			return @in.GetValueForNormalization();
		}

		public override void Normalize(float norm, float topLevelBoost)
		{
			@in.Normalize(norm, topLevelBoost);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public overrideLucene.Net.TestFramework.Search.Scorer Scorer(AtomicReaderContext context
			, Bits acceptDocs)
		{
			// if the caller asks for in-order scoring or if the weight does not support
			// out-of order scoring then collection will have to happen in-order.
			Lucene.NetSearch.Scorer inScorer = @in.Scorer(context, acceptDocs);
			return AssertingScorer.Wrap(new Random(random.NextLong()), inScorer);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public overrideLucene.Net.TestFramework.Search.BulkScorer BulkScorer(AtomicReaderContext
			 context, bool scoreDocsInOrder, Bits acceptDocs)
		{
			// if the caller asks for in-order scoring or if the weight does not support
			// out-of order scoring then collection will have to happen in-order.
			Lucene.NetSearch.BulkScorer inScorer = @in.BulkScorer(context, scoreDocsInOrder
				, acceptDocs);
			if (inScorer == null)
			{
				return null;
			}
			if (AssertingBulkScorer.ShouldWrap(inScorer))
			{
				// The incoming scorer already has a specialized
				// implementation for BulkScorer, so we should use it:
				inScorer = AssertingBulkScorer.Wrap(new Random(random.NextLong()), inScorer);
			}
			else
			{
				if (random.NextBoolean())
				{
					// Let super wrap this.scorer instead, so we use
					// AssertingScorer:
					inScorer = base.BulkScorer(context, scoreDocsInOrder, acceptDocs);
				}
			}
			if (scoreDocsInOrder == false && random.NextBoolean())
			{
				// The caller claims it can handle out-of-order
				// docs; let's confirm that by pulling docs and
				// randomly shuffling them before collection:
				inScorer = new AssertingBulkOutOfOrderScorer(new Random(random.NextLong()), inScorer
					);
			}
			return inScorer;
		}

		public override bool ScoresDocsOutOfOrder()
		{
			return scoresDocsOutOfOrder;
		}
	}
}
