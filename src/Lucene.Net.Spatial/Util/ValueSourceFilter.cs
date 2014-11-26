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

namespace Lucene.Net.Spatial.Util
{
	/// <summary>
	/// Filter that matches all documents where a ValueSource is
	/// in between a range of <code>min</code> and <code>max</code> inclusive.
	/// </summary>
	/// <remarks>
	/// Filter that matches all documents where a ValueSource is
	/// in between a range of <code>min</code> and <code>max</code> inclusive.
	/// </remarks>
	/// <lucene.internal></lucene.internal>
	public class ValueSourceFilter : Filter
	{
		internal readonly Filter startingFilter;

		internal readonly ValueSource source;

		internal readonly double min;

		internal readonly double max;

		public ValueSourceFilter(Filter startingFilter, ValueSource source, double min, double
			 max)
		{
			//TODO see https://issues.apache.org/jira/browse/LUCENE-4251  (move out of spatial & improve)
			if (startingFilter == null)
			{
				throw new ArgumentException("please provide a non-null startingFilter; you can use QueryWrapperFilter(MatchAllDocsQuery) as a no-op filter"
					);
			}
			this.startingFilter = startingFilter;
			this.source = source;
			this.min = min;
			this.max = max;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs
			)
		{
			FunctionValues values = source.GetValues(null, context);
			return new _FilteredDocIdSet_57(this, values, startingFilter.GetDocIdSet(context, 
				acceptDocs));
		}

		private sealed class _FilteredDocIdSet_57 : FilteredDocIdSet
		{
			public _FilteredDocIdSet_57(ValueSourceFilter _enclosing, FunctionValues values, 
				DocIdSet baseArg1) : base(baseArg1)
			{
				this._enclosing = _enclosing;
				this.values = values;
			}

			protected override bool Match(int doc)
			{
				double val = values.DoubleVal(doc);
				return val >= this._enclosing.min && val <= this._enclosing.max;
			}

			private readonly ValueSourceFilter _enclosing;

			private readonly FunctionValues values;
		}
	}
}
