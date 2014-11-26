/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections;
using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Sharpen;

namespace Lucene.Net.Spatial.Util
{
	/// <summary>
	/// Caches the doubleVal of another value source in a HashMap
	/// so that it is computed only once.
	/// </summary>
	/// <remarks>
	/// Caches the doubleVal of another value source in a HashMap
	/// so that it is computed only once.
	/// </remarks>
	/// <lucene.internal></lucene.internal>
	public class CachingDoubleValueSource : ValueSource
	{
		internal readonly ValueSource source;

		internal readonly IDictionary<int, double> cache;

		public CachingDoubleValueSource(ValueSource source)
		{
			this.source = source;
			cache = new Dictionary<int, double>();
		}

		public override string Description()
		{
			return "Cached[" + source.Description() + "]";
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FunctionValues GetValues(IDictionary context, AtomicReaderContext
			 readerContext)
		{
			int @base = readerContext.docBase;
			FunctionValues vals = source.GetValues(context, readerContext);
			return new _FunctionValues_53(this, @base, vals);
		}

		private sealed class _FunctionValues_53 : FunctionValues
		{
			public _FunctionValues_53(CachingDoubleValueSource _enclosing, int @base, FunctionValues
				 vals)
			{
				this._enclosing = _enclosing;
				this.@base = @base;
				this.vals = vals;
			}

			public override double DoubleVal(int doc)
			{
				int key = Sharpen.Extensions.ValueOf(@base + doc);
				double v = this._enclosing.cache.Get(key);
				if (v == null)
				{
					v = double.ValueOf(vals.DoubleVal(doc));
					this._enclosing.cache.Put(key, v);
				}
				return v;
			}

			public override float FloatVal(int doc)
			{
				return (float)this.DoubleVal(doc);
			}

			public override string ToString(int doc)
			{
				return this.DoubleVal(doc) + string.Empty;
			}

			private readonly CachingDoubleValueSource _enclosing;

			private readonly int @base;

			private readonly FunctionValues vals;
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			if (o == null || GetType() != o.GetType())
			{
				return false;
			}
			Lucene.Net.Spatial.Util.CachingDoubleValueSource that = (Lucene.Net.Spatial.Util.CachingDoubleValueSource
				)o;
			if (source != null ? !source.Equals(that.source) : that.source != null)
			{
				return false;
			}
			return true;
		}

		public override int GetHashCode()
		{
			return source != null ? source.GetHashCode() : 0;
		}
	}
}
