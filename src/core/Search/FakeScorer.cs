using System;
using System.Collections.Generic;

namespace Lucene.Net.Search
{
	/// <summary>
	/// Used by
	/// <see cref="BulkScorer">BulkScorer</see>
	/// s that need to pass a
	/// <see cref="Scorer">Scorer</see>
	/// to
	/// <see cref="Collector.SetScorer(Scorer)">Collector.SetScorer(Scorer)</see>
	/// .
	/// </summary>
	internal sealed class FakeScorer : Scorer
	{
		internal float score;

		internal int doc = -1;

		internal int freq = 1;

		public FakeScorer() : base(null)
		{
		}

		public override int Advance(int target)
		{
			throw new NotSupportedException("FakeScorer doesn't support advance(int)");
		}

		public override int DocID
		{
		    get { return doc; }
		}

		public override int Freq
		{
		    get { return freq; }
		}

		public override int NextDoc()
		{
			throw new NotSupportedException("FakeScorer doesn't support nextDoc()");
		}

		public override float Score()
		{
			return score;
		}

		public override long Cost
		{
		    get { return 1; }
		}

	    protected internal override Weight Weight
		{
		    get { throw new NotSupportedException(); }
		}

		public override ICollection<Scorer.ChildScorer> Children
		{
		    get { throw new NotSupportedException(); }
		}
	}
}
