using System;
using System.Collections;
using System.Collections.Generic;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace Lucene.Net.Index
{
	internal class CoalescedUpdates
	{
		internal readonly IDictionary<Query, int> queries = new Dictionary<Query, int>();

		internal readonly IList<IEnumerable<Term>> iterables = new List<IEnumerable<Term>>();

		internal readonly IList<DocValuesUpdate.NumericDocValuesUpdate> numericDVUpdates = 
			new List<DocValuesUpdate.NumericDocValuesUpdate>();

		internal readonly IList<DocValuesUpdate.BinaryDocValuesUpdate> binaryDVUpdates = 
			new List<DocValuesUpdate.BinaryDocValuesUpdate>();

		public override string ToString()
		{
			// note: we could add/collect more debugging information
			return "CoalescedUpdates(termSets=" + iterables.Count + ",queries=" + queries.Count
				 + ",numericDVUpdates=" + numericDVUpdates.Count + ",binaryDVUpdates=" + binaryDVUpdates
				.Count + ")";
		}

		internal virtual void Update(FrozenBufferedUpdates updates)
		{
			iterables.Add(updates.TermsIterable());
			for (int queryIdx = 0; queryIdx < updates.queries.Length; queryIdx++)
			{
				Query query = updates.queries[queryIdx];
				queries[query] = BufferedUpdates.MAX_INT;
			}
			foreach (DocValuesUpdate.NumericDocValuesUpdate nu in updates.numericDVUpdates)
			{
				var clone = new DocValuesUpdate.NumericDocValuesUpdate
					(nu.term, nu.field, (long)nu.value) {docIDUpto = int.MaxValue};
			    numericDVUpdates.Add(clone);
			}
			foreach (DocValuesUpdate.BinaryDocValuesUpdate bu in updates.binaryDVUpdates)
			{
				var clone = new DocValuesUpdate.BinaryDocValuesUpdate
					(bu.term, bu.field, (BytesRef)bu.value) {docIDUpto = int.MaxValue};
			    binaryDVUpdates.Add(clone);
			}
		}

		public virtual IEnumerable<Term> TermsIterable()
		{
			return new AnonmousTermEnumerable(this);
		}

		private sealed class AnonmousTermEnumerable : IEnumerable<Term>
		{
			public AnonmousTermEnumerable(CoalescedUpdates _enclosing)
			{
				this._enclosing = _enclosing;
			}

			public IEnumerator<Term> GetEnumerator()
			{
				var subs = new IEnumerator<Term>[this._enclosing.iterables.Count];
				for (int i = 0; i < this._enclosing.iterables.Count; i++)
				{
					subs[i] = this._enclosing.iterables[i].GetEnumerator();
				}
				return new MergedIterator<Term>(subs);
			}

			private readonly CoalescedUpdates _enclosing;
		    IEnumerator IEnumerable.GetEnumerator()
		    {
		        return GetEnumerator();
		    }
		}

		public virtual IEnumerable<BufferedUpdatesStream.QueryAndLimit> QueriesIterable()
		{
			return new AnonymousQueryAndLimitEnumerable(this);
		}

		private sealed class AnonymousQueryAndLimitEnumerable : IEnumerable<BufferedUpdatesStream.QueryAndLimit>
		{
		    private readonly CoalescedUpdates _enclosing;
			public AnonymousQueryAndLimitEnumerable(CoalescedUpdates enclosing)
			{
				this._enclosing = enclosing;
			}

			public IEnumerator<BufferedUpdatesStream.QueryAndLimit> GetEnumerator()
			{
				return new AnonymousQueryAndLimitEnumerator(this);
			}

			private sealed class AnonymousQueryAndLimitEnumerator : IEnumerator<BufferedUpdatesStream.QueryAndLimit>
			{
                
				public AnonymousQueryAndLimitEnumerator(AnonymousQueryAndLimitEnumerable parent)
				{
					this.iter = parent._enclosing.queries.GetEnumerator();
				}

				private readonly IEnumerator<KeyValuePair<Query, int>> iter;

				public bool MoveNext()
				{
					return this.iter.MoveNext();
				}

				public BufferedUpdatesStream.QueryAndLimit Current
				{
				    get
				    {
				        KeyValuePair<Query, int> ent = this.iter.Current;
				        return new BufferedUpdatesStream.QueryAndLimit(ent.Key, ent.Value);
				    }
				}

				public void Reset()
				{
					throw new NotSupportedException();
				}

			    object IEnumerator.Current
			    {
			        get { return Current; }
			    }

			    public void Dispose()
			    {
			        this.iter.Dispose();
			    }
			}


		    IEnumerator IEnumerable.GetEnumerator()
		    {
		        return GetEnumerator();
		    }
		}
	}
}
