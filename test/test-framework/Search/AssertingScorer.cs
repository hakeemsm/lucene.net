/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.TestFramework.Index;
using Lucene.Net.TestFramework.Search;
using Sharpen;

namespace Lucene.Net.TestFramework.Search
{
	/// <summary>Wraps a Scorer with additional checks</summary>
	public class AssertingScorer : Scorer
	{
		private static IDictionary<Scorer, WeakReference<Lucene.NetSearch.AssertingScorer
			>> ASSERTING_INSTANCES = Sharpen.Collections.SynchronizedMap(new WeakHashMap<Scorer
			, WeakReference<Lucene.NetSearch.AssertingScorer>>());

		// we need to track scorers using a weak hash map because otherwise we
		// could loose references because of eg.
		// AssertingScorer.score(Collector) which needs to delegate to work correctly
		public static Scorer Wrap(Random random, Scorer other)
		{
			if (other == null || other isLucene.Net.TestFramework.Search.AssertingScorer)
			{
				return other;
			}
			Lucene.NetSearch.AssertingScorer assertScorer = newLucene.Net.TestFramework.Search.AssertingScorer
				(random, other);
			ASSERTING_INSTANCES.Put(other, new WeakReference<Lucene.NetSearch.AssertingScorer
				>(assertScorer));
			return assertScorer;
		}

		internal static Scorer GetAssertingScorer(Random random, Scorer other)
		{
			if (other == null || other isLucene.Net.TestFramework.Search.AssertingScorer)
			{
				return other;
			}
			WeakReference<Lucene.NetSearch.AssertingScorer> assertingScorerRef = ASSERTING_INSTANCES
				.Get(other);
			Lucene.NetSearch.AssertingScorer assertingScorer = assertingScorerRef == 
				null ? null : assertingScorerRef.Get();
			if (assertingScorer == null)
			{
				// can happen in case of memory pressure or if
				// scorer1.score(collector) calls
				// collector.setScorer(scorer2) with scorer1 != scorer2, such as
				// BooleanScorer. In that case we can't enable all assertions
				return newLucene.Net.TestFramework.Search.AssertingScorer(random, other);
			}
			else
			{
				return assertingScorer;
			}
		}

		internal readonly Random random;

		internal readonly Scorer @in;

		internal readonly AssertingAtomicReader.AssertingDocsEnum docsEnumIn;

		private AssertingScorer(Random random, Scorer @in) : base(@in.weight)
		{
			this.random = random;
			this.@in = @in;
			this.docsEnumIn = new AssertingAtomicReader.AssertingDocsEnum(@in);
		}

		public virtual Scorer GetIn()
		{
			return @in;
		}

		internal virtual bool Iterating()
		{
			switch (DocID())
			{
				case -1:
				case NO_MORE_DOCS:
				{
					return false;
				}

				default:
				{
					return true;
					break;
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override float Score()
		{
			 
			//assert iterating();
			float score = @in.Score();
			 
			//assert !Float.isNaN(score);
			 
			//assert !Float.isNaN(score);
			return score;
		}

		public override ICollection<Scorer.ChildScorer> GetChildren()
		{
			// We cannot hide that we hold a single child, else
			// collectors (e.g. ToParentBlockJoinCollector) that
			// need to walk the scorer tree will miss/skip the
			// Scorer we wrap:
			return Sharpen.Collections.SingletonList(new Scorer.ChildScorer(@in, "SHOULD"));
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override int Freq()
		{
			 
			//assert iterating();
			return @in.Freq();
		}

		public override int DocID()
		{
			return @in.DocID();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override int NextDoc()
		{
			return docsEnumIn.NextDoc();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override int Advance(int target)
		{
			return docsEnumIn.Advance(target);
		}

		public override long Cost()
		{
			return @in.Cost();
		}

		public override string ToString()
		{
			return "AssertingScorer(" + @in + ")";
		}
	}
}
