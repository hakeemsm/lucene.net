/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections;
using Lucene.Net.Queries.Function;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Queries.Function.Docvalues;
using Lucene.Net.Queries.Function.Valuesource;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Lucene.Net.Util.Mutable;
using Sharpen;

namespace Lucene.Net.Queries.Function.Valuesource
{
	/// <summary>
	/// Obtains double field values from
	/// <see cref="Lucene.Net.Search.FieldCache.GetDoubles(Lucene.Net.Index.AtomicReader, string, bool)
	/// 	">Lucene.Net.Search.FieldCache.GetDoubles(Lucene.Net.Index.AtomicReader, string, bool)
	/// 	</see>
	/// and makes
	/// those values available as other numeric types, casting as needed.
	/// </summary>
	public class DoubleFieldSource : FieldCacheSource
	{
		protected internal readonly FieldCache.DoubleParser parser;

		public DoubleFieldSource(string field) : this(field, null)
		{
		}

		public DoubleFieldSource(string field, FieldCache.DoubleParser parser) : base(field
			)
		{
			this.parser = parser;
		}

		public override string Description()
		{
			return "double(" + field + ')';
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FunctionValues GetValues(IDictionary context, AtomicReaderContext
			 readerContext)
		{
			FieldCache.Doubles arr = cache.GetDoubles(((AtomicReader)readerContext.Reader()), 
				field, parser, true);
			Bits valid = cache.GetDocsWithField(((AtomicReader)readerContext.Reader()), field
				);
			return new _DoubleDocValues_59(arr, valid, this);
		}

		private sealed class _DoubleDocValues_59 : DoubleDocValues
		{
			public _DoubleDocValues_59(FieldCache.Doubles arr, Bits valid, ValueSource baseArg1
				) : base(baseArg1)
			{
				this.arr = arr;
				this.valid = valid;
			}

			public override double DoubleVal(int doc)
			{
				return arr.Get(doc);
			}

			public override bool Exists(int doc)
			{
				return arr.Get(doc) != 0 || valid.Get(doc);
			}

			public override FunctionValues.ValueFiller GetValueFiller()
			{
				return new _ValueFiller_72(arr, valid);
			}

			private sealed class _ValueFiller_72 : FunctionValues.ValueFiller
			{
				public _ValueFiller_72(FieldCache.Doubles arr, Bits valid)
				{
					this.arr = arr;
					this.valid = valid;
					this.mval = new MutableValueDouble();
				}

				private readonly MutableValueDouble mval;

				public override MutableValue GetValue()
				{
					return this.mval;
				}

				public override void FillValue(int doc)
				{
					this.mval.value = arr.Get(doc);
					this.mval.exists = this.mval.value != 0 || valid.Get(doc);
				}

				private readonly FieldCache.Doubles arr;

				private readonly Bits valid;
			}

			private readonly FieldCache.Doubles arr;

			private readonly Bits valid;
		}

		public override bool Equals(object o)
		{
			if (o.GetType() != typeof(Lucene.Net.Queries.Function.Valuesource.DoubleFieldSource
				))
			{
				return false;
			}
			Lucene.Net.Queries.Function.Valuesource.DoubleFieldSource other = (Lucene.Net.Queries.Function.Valuesource.DoubleFieldSource
				)o;
			return base.Equals(other) && (this.parser == null ? other.parser == null : this.parser
				.GetType() == other.parser.GetType());
		}

		public override int GetHashCode()
		{
			int h = parser == null ? typeof(double).GetHashCode() : parser.GetType().GetHashCode
				();
			h += base.GetHashCode();
			return h;
		}
	}
}
