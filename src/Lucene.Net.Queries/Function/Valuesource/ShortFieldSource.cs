/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Queries.Function.Valuesource;
using Lucene.Net.Search;
using Sharpen;

namespace Lucene.Net.Queries.Function.Valuesource
{
	/// <summary>
	/// Obtains short field values from the
	/// <see cref="Lucene.Net.Search.FieldCache">Lucene.Net.Search.FieldCache
	/// 	</see>
	/// using <code>getShorts()</code>
	/// and makes those values available as other numeric types, casting as needed.
	/// </summary>
	public class ShortFieldSource : FieldCacheSource
	{
		internal readonly FieldCache.ShortParser parser;

		public ShortFieldSource(string field) : this(field, null)
		{
		}

		public ShortFieldSource(string field, FieldCache.ShortParser parser) : base(field
			)
		{
			this.parser = parser;
		}

		public override string Description()
		{
			return "short(" + field + ')';
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FunctionValues GetValues(IDictionary context, AtomicReaderContext
			 readerContext)
		{
			FieldCache.Shorts arr = cache.GetShorts(((AtomicReader)readerContext.Reader()), field
				, parser, false);
			return new _FunctionValues_55(this, arr);
		}

		private sealed class _FunctionValues_55 : FunctionValues
		{
			public _FunctionValues_55(ShortFieldSource _enclosing, FieldCache.Shorts arr)
			{
				this._enclosing = _enclosing;
				this.arr = arr;
			}

			public override byte ByteVal(int doc)
			{
				return unchecked((byte)arr.Get(doc));
			}

			public override short ShortVal(int doc)
			{
				return arr.Get(doc);
			}

			public override float FloatVal(int doc)
			{
				return (float)arr.Get(doc);
			}

			public override int IntVal(int doc)
			{
				return (int)arr.Get(doc);
			}

			public override long LongVal(int doc)
			{
				return (long)arr.Get(doc);
			}

			public override double DoubleVal(int doc)
			{
				return (double)arr.Get(doc);
			}

			public override string StrVal(int doc)
			{
				return short.ToString(arr.Get(doc));
			}

			public override string ToString(int doc)
			{
				return this._enclosing.Description() + '=' + this.ShortVal(doc);
			}

			private readonly ShortFieldSource _enclosing;

			private readonly FieldCache.Shorts arr;
		}

		public override bool Equals(object o)
		{
			if (o.GetType() != typeof(Lucene.Net.Queries.Function.Valuesource.ShortFieldSource
				))
			{
				return false;
			}
			Lucene.Net.Queries.Function.Valuesource.ShortFieldSource other = (Lucene.Net.Queries.Function.Valuesource.ShortFieldSource
				)o;
			return base.Equals(other) && (this.parser == null ? other.parser == null : this.parser
				.GetType() == other.parser.GetType());
		}

		public override int GetHashCode()
		{
			int h = parser == null ? typeof(short).GetHashCode() : parser.GetType().GetHashCode
				();
			h += base.GetHashCode();
			return h;
		}
	}
}
