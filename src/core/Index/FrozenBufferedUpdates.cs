using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Search;
using Lucene.Net.Support;
using Lucene.Net.Util;

namespace Lucene.Net.Index
{
	/// <summary>Holds buffered deletes and updates by term or query, once pushed.</summary>
	/// <remarks>
	/// Holds buffered deletes and updates by term or query, once pushed. Pushed
	/// deletes/updates are write-once, so we shift to more memory efficient data
	/// structure to hold them. We don't hold docIDs because these are applied on
	/// flush.
	/// </remarks>
	internal class FrozenBufferedUpdates
	{
		internal static readonly int BYTES_PER_DEL_QUERY = RamUsageEstimator.NUM_BYTES_OBJECT_REF
			 + RamUsageEstimator.NUM_BYTES_INT + 24;

		internal readonly PrefixCodedTerms terms;

		internal int termCount;

		internal readonly Query[] queries;

		internal readonly int[] queryLimits;

		internal readonly DocValuesUpdate.NumericDocValuesUpdate[] numericDVUpdates;

		internal readonly DocValuesUpdate.BinaryDocValuesUpdate[] binaryDVUpdates;

		internal readonly int bytesUsed;

		internal readonly int numTermDeletes;

		private long gen = -1;

		internal readonly bool isSegmentPrivate;

		public FrozenBufferedUpdates(BufferedUpdates deletes, bool isSegmentPrivate)
		{
			// Terms, in sorted order:
			// just for debugging
			// Parallel array of deleted query, and the docIDUpto for each
			// numeric DV update term and their updates
			// binary DV update term and their updates
			// assigned by BufferedDeletesStream once pushed
			// set to true iff this frozen packet represents 
			// a segment private deletes. in that case is should
			// only have Queries 
			this.isSegmentPrivate = isSegmentPrivate;
			//HM:revisit 
			//assert !isSegmentPrivate || deletes.terms.size() == 0 : "segment private package should only have del queries"; 
            Term[] termsArray = deletes.terms.Keys.ToArray();
			termCount = termsArray.Length;
			ArrayUtil.TimSort(termsArray);
			var builder = new PrefixCodedTerms.Builder();
			foreach (Term term in termsArray)
			{
				builder.Add(term);
			}
			terms = builder.Finish();
			queries = new Query[deletes.queries.Count];
			queryLimits = new int[deletes.queries.Count];
			int upto = 0;
			foreach (KeyValuePair<Query, int?> ent in deletes.queries)
			{
				queries[upto] = ent.Key;
				queryLimits[upto] = ent.Value.Value;
				upto++;
			}
			// TODO if a Term affects multiple fields, we could keep the updates key'd by Term
			// so that it maps to all fields it affects, sorted by their docUpto, and traverse
			// that Term only once, applying the update to all fields that still need to be
			// updated. 
			IList<DocValuesUpdate.NumericDocValuesUpdate> allNumericUpdates = new List<DocValuesUpdate.NumericDocValuesUpdate
				>();
			int numericUpdatesSize = 0;
			foreach (var hashMap in deletes.numericUpdates.Values)
			{
			    var numericUpdates = hashMap;
			    foreach (DocValuesUpdate.NumericDocValuesUpdate update in numericUpdates.Values)
				{
					allNumericUpdates.Add(update);
					numericUpdatesSize += (int)update.SizeInBytes();
				}
			}
            numericDVUpdates = allNumericUpdates.ToArray();
			// TODO if a Term affects multiple fields, we could keep the updates key'd by Term
			// so that it maps to all fields it affects, sorted by their docUpto, and traverse
			// that Term only once, applying the update to all fields that still need to be
			// updated. 
			IList<DocValuesUpdate.BinaryDocValuesUpdate> allBinaryUpdates = new List<DocValuesUpdate.BinaryDocValuesUpdate>();
			int binaryUpdatesSize = 0;
			foreach (HashMap<Term, DocValuesUpdate.BinaryDocValuesUpdate> binaryUpdates
				 in deletes.binaryUpdates.Values)
			{
				foreach (DocValuesUpdate.BinaryDocValuesUpdate update in binaryUpdates.Values)
				{
					allBinaryUpdates.Add(update);
					binaryUpdatesSize += (int)update.SizeInBytes();
				}
			}
			binaryDVUpdates = allBinaryUpdates.ToArray();
			bytesUsed = (int)terms.SizeInBytes + queries.Length * BYTES_PER_DEL_QUERY + 
				numericUpdatesSize + numericDVUpdates.Length * RamUsageEstimator.NUM_BYTES_OBJECT_REF
				 + binaryUpdatesSize + binaryDVUpdates.Length * RamUsageEstimator.NUM_BYTES_OBJECT_REF;
			numTermDeletes = deletes.numTermDeletes.Get();
		}

		public virtual void SetDelGen(long gen)
		{
			//HM:revisit 
			//assert this.gen == -1;
			this.gen = gen;
		}

		public virtual long DelGen()
		{
			//HM:revisit 
			//assert gen != -1;
			return gen;
		}

		public virtual IEnumerable<Term> TermsIterable()
		{
			return new AnonymousEnumerableTerms(this);
		}

		private sealed class AnonymousEnumerableTerms : IEnumerable<Term>
		{
			public AnonymousEnumerableTerms(FrozenBufferedUpdates _enclosing)
			{
				this._enclosing = _enclosing;
			}

			public IEnumerator<Term> GetEnumerator()
			{
				return this._enclosing.terms.GetEnumerator();
			}

			private readonly FrozenBufferedUpdates _enclosing;
		    IEnumerator IEnumerable.GetEnumerator()
		    {
		        return GetEnumerator();
		    }
		}

		public virtual IEnumerable<BufferedUpdatesStream.QueryAndLimit> QueriesIterable()
		{
			return new AnonymousQueryLimitEnumerable(this);
		}

		private sealed class AnonymousQueryLimitEnumerable : IEnumerable<BufferedUpdatesStream.QueryAndLimit>
		{
			public AnonymousQueryLimitEnumerable(FrozenBufferedUpdates _enclosing)
			{
				this._enclosing = _enclosing;
			}

			public IEnumerator<BufferedUpdatesStream.QueryAndLimit> GetEnumerator()
			{
				return new AnonymousQueryLimitEnumerator(this);
			}

			private sealed class AnonymousQueryLimitEnumerator : IEnumerator<BufferedUpdatesStream.QueryAndLimit>
			{
				public AnonymousQueryLimitEnumerator(AnonymousQueryLimitEnumerable _enclosing)
				{
					this._enclosing = _enclosing;
				}

				private int upto;

				public bool MoveNext()
				{
					return this.upto < this._enclosing._enclosing.queries.Length;
				}

				public BufferedUpdatesStream.QueryAndLimit Current
				{
				    get
				    {
				        BufferedUpdatesStream.QueryAndLimit ret = new BufferedUpdatesStream.QueryAndLimit
				            (this._enclosing._enclosing.queries[this.upto], this._enclosing._enclosing.queryLimits
				                [this.upto]);
				        this.upto++;
				        return ret;
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

			    private readonly AnonymousQueryLimitEnumerable _enclosing;
			    public void Dispose()
			    {
			        
			    }
			}

			private readonly FrozenBufferedUpdates _enclosing;
		    IEnumerator IEnumerable.GetEnumerator()
		    {
		        return GetEnumerator();
		    }
		}

		public override string ToString()
		{
			string s = string.Empty;
			if (numTermDeletes != 0)
			{
				s += " " + numTermDeletes + " deleted terms (unique count=" + termCount + ")";
			}
			if (queries.Length != 0)
			{
				s += " " + queries.Length + " deleted queries";
			}
			if (bytesUsed != 0)
			{
				s += " bytesUsed=" + bytesUsed;
			}
			return s;
		}

		internal virtual bool Any()
		{
			return termCount > 0 || queries.Length > 0 || numericDVUpdates.Length > 0 || binaryDVUpdates
				.Length > 0;
		}
	}
}
