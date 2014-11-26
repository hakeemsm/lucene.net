/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Facet.Range;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Facet.Range
{
	/// <summary>Represents a range over double values.</summary>
	/// <remarks>Represents a range over double values.</remarks>
	/// <lucene.experimental></lucene.experimental>
	public sealed class DoubleRange : Lucene.Net.Facet.Range.Range
	{
		internal readonly double minIncl;

		internal readonly double maxIncl;

		/// <summary>Minimum.</summary>
		/// <remarks>Minimum.</remarks>
		public readonly double min;

		/// <summary>Maximum.</summary>
		/// <remarks>Maximum.</remarks>
		public readonly double max;

		/// <summary>True if the minimum value is inclusive.</summary>
		/// <remarks>True if the minimum value is inclusive.</remarks>
		public readonly bool minInclusive;

		/// <summary>True if the maximum value is inclusive.</summary>
		/// <remarks>True if the maximum value is inclusive.</remarks>
		public readonly bool maxInclusive;

		/// <summary>Create a DoubleRange.</summary>
		/// <remarks>Create a DoubleRange.</remarks>
		public DoubleRange(string label, double minIn, bool minInclusive, double maxIn, bool
			 maxInclusive) : base(label)
		{
			this.min = minIn;
			this.max = maxIn;
			this.minInclusive = minInclusive;
			this.maxInclusive = maxInclusive;
			// TODO: if DoubleDocValuesField used
			// NumericUtils.doubleToSortableLong format (instead of
			// Double.doubleToRawLongBits) we could do comparisons
			// in long space 
			if (double.IsNaN(min))
			{
				throw new ArgumentException("min cannot be NaN");
			}
			if (!minInclusive)
			{
				minIn = Math.NextUp(minIn);
			}
			if (double.IsNaN(max))
			{
				throw new ArgumentException("max cannot be NaN");
			}
			if (!maxInclusive)
			{
				// Why no Math.nextDown?
				maxIn = Math.NextAfter(maxIn, double.NegativeInfinity);
			}
			if (minIn > maxIn)
			{
				FailNoMatch();
			}
			this.minIncl = minIn;
			this.maxIncl = maxIn;
		}

		/// <summary>True if this range accepts the provided value.</summary>
		/// <remarks>True if this range accepts the provided value.</remarks>
		public bool Accept(double value)
		{
			return value >= minIncl && value <= maxIncl;
		}

		internal LongRange ToLongRange()
		{
			return new LongRange(label, NumericUtils.DoubleToSortableLong(minIncl), true, NumericUtils
				.DoubleToSortableLong(maxIncl), true);
		}

		public override string ToString()
		{
			return "DoubleRange(" + minIncl + " to " + maxIncl + ")";
		}

		public override Filter GetFilter(Filter fastMatchFilter, ValueSource valueSource)
		{
			return new _Filter_105(this, valueSource, fastMatchFilter);
		}

		private sealed class _Filter_105 : Filter
		{
			public _Filter_105(DoubleRange _enclosing, ValueSource valueSource, Filter fastMatchFilter
				)
			{
				this._enclosing = _enclosing;
				this.valueSource = valueSource;
				this.fastMatchFilter = fastMatchFilter;
			}

			public override string ToString()
			{
				return "Filter(" + this._enclosing.ToString() + ")";
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs
				)
			{
				// TODO: this is just like ValueSourceScorer,
				// ValueSourceFilter (spatial),
				// ValueSourceRangeFilter (solr); also,
				// https://issues.apache.org/jira/browse/LUCENE-4251
				FunctionValues values = valueSource.GetValues(Collections.EmptyMap(), context);
				int maxDoc = ((AtomicReader)context.Reader()).MaxDoc();
				Bits fastMatchBits;
				if (fastMatchFilter != null)
				{
					DocIdSet dis = fastMatchFilter.GetDocIdSet(context, null);
					if (dis == null)
					{
						// No documents match
						return null;
					}
					fastMatchBits = dis.Bits();
					if (fastMatchBits == null)
					{
						throw new ArgumentException("fastMatchFilter does not implement DocIdSet.bits");
					}
				}
				else
				{
					fastMatchBits = null;
				}
				return new _DocIdSet_139(this, acceptDocs, fastMatchBits, values, maxDoc);
			}

			private sealed class _DocIdSet_139 : DocIdSet
			{
				public _DocIdSet_139(_Filter_105 _enclosing, Bits acceptDocs, Bits fastMatchBits, 
					FunctionValues values, int maxDoc)
				{
					this._enclosing = _enclosing;
					this.acceptDocs = acceptDocs;
					this.fastMatchBits = fastMatchBits;
					this.values = values;
					this.maxDoc = maxDoc;
				}

				public override Bits Bits()
				{
					return new _Bits_143(this, acceptDocs, fastMatchBits, values, maxDoc);
				}

				private sealed class _Bits_143 : Bits
				{
					public _Bits_143(_DocIdSet_139 _enclosing, Bits acceptDocs, Bits fastMatchBits, FunctionValues
						 values, int maxDoc)
					{
						this._enclosing = _enclosing;
						this.acceptDocs = acceptDocs;
						this.fastMatchBits = fastMatchBits;
						this.values = values;
						this.maxDoc = maxDoc;
					}

					public override bool Get(int docID)
					{
						if (acceptDocs != null && acceptDocs.Get(docID) == false)
						{
							return false;
						}
						if (fastMatchBits != null && fastMatchBits.Get(docID) == false)
						{
							return false;
						}
						return this._enclosing._enclosing._enclosing.Accept(values.DoubleVal(docID));
					}

					public override int Length()
					{
						return maxDoc;
					}

					private readonly _DocIdSet_139 _enclosing;

					private readonly Bits acceptDocs;

					private readonly Bits fastMatchBits;

					private readonly FunctionValues values;

					private readonly int maxDoc;
				}

				public override DocIdSetIterator Iterator()
				{
					throw new NotSupportedException("this filter can only be accessed via bits()");
				}

				private readonly _Filter_105 _enclosing;

				private readonly Bits acceptDocs;

				private readonly Bits fastMatchBits;

				private readonly FunctionValues values;

				private readonly int maxDoc;
			}

			private readonly DoubleRange _enclosing;

			private readonly ValueSource valueSource;

			private readonly Filter fastMatchFilter;
		}
	}
}
