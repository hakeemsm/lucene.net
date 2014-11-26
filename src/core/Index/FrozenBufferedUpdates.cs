/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Sharpen;

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
			Term[] termsArray = Sharpen.Collections.ToArray(deletes.terms.Keys, new Term[deletes
				.terms.Count]);
			termCount = termsArray.Length;
			ArrayUtil.TimSort(termsArray);
			PrefixCodedTerms.Builder builder = new PrefixCodedTerms.Builder();
			foreach (Term term in termsArray)
			{
				builder.Add(term);
			}
			terms = builder.Finish();
			queries = new Query[deletes.queries.Count];
			queryLimits = new int[deletes.queries.Count];
			int upto = 0;
			foreach (KeyValuePair<Query, int> ent in deletes.queries.EntrySet())
			{
				queries[upto] = ent.Key;
				queryLimits[upto] = ent.Value;
				upto++;
			}
			// TODO if a Term affects multiple fields, we could keep the updates key'd by Term
			// so that it maps to all fields it affects, sorted by their docUpto, and traverse
			// that Term only once, applying the update to all fields that still need to be
			// updated. 
			IList<DocValuesUpdate.NumericDocValuesUpdate> allNumericUpdates = new AList<DocValuesUpdate.NumericDocValuesUpdate
				>();
			int numericUpdatesSize = 0;
			foreach (LinkedHashMap<Term, DocValuesUpdate.NumericDocValuesUpdate> numericUpdates
				 in deletes.numericUpdates.Values)
			{
				foreach (DocValuesUpdate.NumericDocValuesUpdate update in numericUpdates.Values)
				{
					allNumericUpdates.AddItem(update);
					numericUpdatesSize += update.SizeInBytes();
				}
			}
			numericDVUpdates = Sharpen.Collections.ToArray(allNumericUpdates, new DocValuesUpdate.NumericDocValuesUpdate
				[allNumericUpdates.Count]);
			// TODO if a Term affects multiple fields, we could keep the updates key'd by Term
			// so that it maps to all fields it affects, sorted by their docUpto, and traverse
			// that Term only once, applying the update to all fields that still need to be
			// updated. 
			IList<DocValuesUpdate.BinaryDocValuesUpdate> allBinaryUpdates = new AList<DocValuesUpdate.BinaryDocValuesUpdate
				>();
			int binaryUpdatesSize = 0;
			foreach (LinkedHashMap<Term, DocValuesUpdate.BinaryDocValuesUpdate> binaryUpdates
				 in deletes.binaryUpdates.Values)
			{
				foreach (DocValuesUpdate.BinaryDocValuesUpdate update in binaryUpdates.Values)
				{
					allBinaryUpdates.AddItem(update);
					binaryUpdatesSize += update.SizeInBytes();
				}
			}
			binaryDVUpdates = Sharpen.Collections.ToArray(allBinaryUpdates, new DocValuesUpdate.BinaryDocValuesUpdate
				[allBinaryUpdates.Count]);
			bytesUsed = (int)terms.GetSizeInBytes() + queries.Length * BYTES_PER_DEL_QUERY + 
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

		public virtual Iterable<Term> TermsIterable()
		{
			return new _Iterable_139(this);
		}

		private sealed class _Iterable_139 : Iterable<Term>
		{
			public _Iterable_139(FrozenBufferedUpdates _enclosing)
			{
				this._enclosing = _enclosing;
			}

			public override Iterator<Term> Iterator()
			{
				return this._enclosing.terms.Iterator();
			}

			private readonly FrozenBufferedUpdates _enclosing;
		}

		public virtual Iterable<BufferedUpdatesStream.QueryAndLimit> QueriesIterable()
		{
			return new _Iterable_148(this);
		}

		private sealed class _Iterable_148 : Iterable<BufferedUpdatesStream.QueryAndLimit
			>
		{
			public _Iterable_148(FrozenBufferedUpdates _enclosing)
			{
				this._enclosing = _enclosing;
			}

			public override Iterator<BufferedUpdatesStream.QueryAndLimit> Iterator()
			{
				return new _Iterator_151(this);
			}

			private sealed class _Iterator_151 : Iterator<BufferedUpdatesStream.QueryAndLimit
				>
			{
				public _Iterator_151(_Iterable_148 _enclosing)
				{
					this._enclosing = _enclosing;
				}

				private int upto;

				public override bool HasNext()
				{
					return this.upto < this._enclosing._enclosing.queries.Length;
				}

				public override BufferedUpdatesStream.QueryAndLimit Next()
				{
					BufferedUpdatesStream.QueryAndLimit ret = new BufferedUpdatesStream.QueryAndLimit
						(this._enclosing._enclosing.queries[this.upto], this._enclosing._enclosing.queryLimits
						[this.upto]);
					this.upto++;
					return ret;
				}

				public override void Remove()
				{
					throw new NotSupportedException();
				}

				private readonly _Iterable_148 _enclosing;
			}

			private readonly FrozenBufferedUpdates _enclosing;
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
