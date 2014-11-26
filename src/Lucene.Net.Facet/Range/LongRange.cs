/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Facet.Range
{
	/// <summary>Represents a range over long values.</summary>
	/// <remarks>Represents a range over long values.</remarks>
	/// <lucene.experimental></lucene.experimental>
	public sealed class LongRange : Lucene.Net.Facet.Range.Range
	{
		internal readonly long minIncl;

		internal readonly long maxIncl;

		/// <summary>Minimum.</summary>
		/// <remarks>Minimum.</remarks>
		public readonly long min;

		/// <summary>Maximum.</summary>
		/// <remarks>Maximum.</remarks>
		public readonly long max;

		/// <summary>True if the minimum value is inclusive.</summary>
		/// <remarks>True if the minimum value is inclusive.</remarks>
		public readonly bool minInclusive;

		/// <summary>True if the maximum value is inclusive.</summary>
		/// <remarks>True if the maximum value is inclusive.</remarks>
		public readonly bool maxInclusive;

		/// <summary>Create a LongRange.</summary>
		/// <remarks>Create a LongRange.</remarks>
		public LongRange(string label, long minIn, bool minInclusive, long maxIn, bool maxInclusive
			) : base(label)
		{
			// TODO: can we require fewer args? (same for
			// Double/FloatRange too)
			this.min = minIn;
			this.max = maxIn;
			this.minInclusive = minInclusive;
			this.maxInclusive = maxInclusive;
			if (!minInclusive)
			{
				if (minIn != long.MaxValue)
				{
					minIn++;
				}
				else
				{
					FailNoMatch();
				}
			}
			if (!maxInclusive)
			{
				if (maxIn != long.MinValue)
				{
					maxIn--;
				}
				else
				{
					FailNoMatch();
				}
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
		public bool Accept(long value)
		{
			return value >= minIncl && value <= maxIncl;
		}

		public override string ToString()
		{
			return "LongRange(" + minIncl + " to " + maxIncl + ")";
		}

		public override Filter GetFilter(Filter fastMatchFilter, ValueSource valueSource)
		{
			return new _Filter_97(this, valueSource, fastMatchFilter);
		}

		private sealed class _Filter_97 : Filter
		{
			public _Filter_97(LongRange _enclosing, ValueSource valueSource, Filter fastMatchFilter
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
				return new _DocIdSet_131(this, acceptDocs, fastMatchBits, values, maxDoc);
			}

			private sealed class _DocIdSet_131 : DocIdSet
			{
				public _DocIdSet_131(_Filter_97 _enclosing, Bits acceptDocs, Bits fastMatchBits, 
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
					return new _Bits_135(this, acceptDocs, fastMatchBits, values, maxDoc);
				}

				private sealed class _Bits_135 : Bits
				{
					public _Bits_135(_DocIdSet_131 _enclosing, Bits acceptDocs, Bits fastMatchBits, FunctionValues
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
						return this._enclosing._enclosing._enclosing.Accept(values.LongVal(docID));
					}

					public override int Length()
					{
						return maxDoc;
					}

					private readonly _DocIdSet_131 _enclosing;

					private readonly Bits acceptDocs;

					private readonly Bits fastMatchBits;

					private readonly FunctionValues values;

					private readonly int maxDoc;
				}

				public override DocIdSetIterator Iterator()
				{
					throw new NotSupportedException("this filter can only be accessed via bits()");
				}

				private readonly _Filter_97 _enclosing;

				private readonly Bits acceptDocs;

				private readonly Bits fastMatchBits;

				private readonly FunctionValues values;

				private readonly int maxDoc;
			}

			private readonly LongRange _enclosing;

			private readonly ValueSource valueSource;

			private readonly Filter fastMatchFilter;
		}
	}
}
