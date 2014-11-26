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
	internal class CoalescedUpdates
	{
		internal readonly IDictionary<Query, int> queries = new Dictionary<Query, int>();

		internal readonly IList<Iterable<Term>> iterables = new AList<Iterable<Term>>();

		internal readonly IList<DocValuesUpdate.NumericDocValuesUpdate> numericDVUpdates = 
			new AList<DocValuesUpdate.NumericDocValuesUpdate>();

		internal readonly IList<DocValuesUpdate.BinaryDocValuesUpdate> binaryDVUpdates = 
			new AList<DocValuesUpdate.BinaryDocValuesUpdate>();

		public override string ToString()
		{
			// note: we could add/collect more debugging information
			return "CoalescedUpdates(termSets=" + iterables.Count + ",queries=" + queries.Count
				 + ",numericDVUpdates=" + numericDVUpdates.Count + ",binaryDVUpdates=" + binaryDVUpdates
				.Count + ")";
		}

		internal virtual void Update(FrozenBufferedUpdates @in)
		{
			iterables.AddItem(@in.TermsIterable());
			for (int queryIdx = 0; queryIdx < @in.queries.Length; queryIdx++)
			{
				Query query = @in.queries[queryIdx];
				queries.Put(query, BufferedUpdates.MAX_INT);
			}
			foreach (DocValuesUpdate.NumericDocValuesUpdate nu in @in.numericDVUpdates)
			{
				DocValuesUpdate.NumericDocValuesUpdate clone = new DocValuesUpdate.NumericDocValuesUpdate
					(nu.term, nu.field, (long)nu.value);
				clone.docIDUpto = int.MaxValue;
				numericDVUpdates.AddItem(clone);
			}
			foreach (DocValuesUpdate.BinaryDocValuesUpdate bu in @in.binaryDVUpdates)
			{
				DocValuesUpdate.BinaryDocValuesUpdate clone = new DocValuesUpdate.BinaryDocValuesUpdate
					(bu.term, bu.field, (BytesRef)bu.value);
				clone.docIDUpto = int.MaxValue;
				binaryDVUpdates.AddItem(clone);
			}
		}

		public virtual Iterable<Term> TermsIterable()
		{
			return new _Iterable_69(this);
		}

		private sealed class _Iterable_69 : Iterable<Term>
		{
			public _Iterable_69(CoalescedUpdates _enclosing)
			{
				this._enclosing = _enclosing;
			}

			public override Iterator<Term> Iterator()
			{
				Iterator<Term>[] subs = new Iterator[this._enclosing.iterables.Count];
				for (int i = 0; i < this._enclosing.iterables.Count; i++)
				{
					subs[i] = this._enclosing.iterables[i].Iterator();
				}
				return new MergedIterator<Term>(subs);
			}

			private readonly CoalescedUpdates _enclosing;
		}

		public virtual Iterable<BufferedUpdatesStream.QueryAndLimit> QueriesIterable()
		{
			return new _Iterable_83(this);
		}

		private sealed class _Iterable_83 : Iterable<BufferedUpdatesStream.QueryAndLimit>
		{
			public _Iterable_83(CoalescedUpdates _enclosing)
			{
				this._enclosing = _enclosing;
			}

			public override Iterator<BufferedUpdatesStream.QueryAndLimit> Iterator()
			{
				return new _Iterator_87(this);
			}

			private sealed class _Iterator_87 : Iterator<BufferedUpdatesStream.QueryAndLimit>
			{
				public _Iterator_87()
				{
					this.iter = this._enclosing._enclosing.queries.EntrySet().Iterator();
				}

				private readonly Iterator<KeyValuePair<Query, int>> iter;

				public override bool HasNext()
				{
					return this.iter.HasNext();
				}

				public override BufferedUpdatesStream.QueryAndLimit Next()
				{
					KeyValuePair<Query, int> ent = this.iter.Next();
					return new BufferedUpdatesStream.QueryAndLimit(ent.Key, ent.Value);
				}

				public override void Remove()
				{
					throw new NotSupportedException();
				}
			}

			private readonly CoalescedUpdates _enclosing;
		}
	}
}
