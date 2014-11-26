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
	/// Obtains float field values from
	/// <see cref="Lucene.Net.Search.FieldCache.GetFloats(Lucene.Net.Index.AtomicReader, string, bool)
	/// 	">Lucene.Net.Search.FieldCache.GetFloats(Lucene.Net.Index.AtomicReader, string, bool)
	/// 	</see>
	/// and makes those
	/// values available as other numeric types, casting as needed.
	/// </summary>
	public class FloatFieldSource : FieldCacheSource
	{
		protected internal readonly FieldCache.FloatParser parser;

		public FloatFieldSource(string field) : this(field, null)
		{
		}

		public FloatFieldSource(string field, FieldCache.FloatParser parser) : base(field
			)
		{
			this.parser = parser;
		}

		public override string Description()
		{
			return "float(" + field + ')';
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FunctionValues GetValues(IDictionary context, AtomicReaderContext
			 readerContext)
		{
			FieldCache.Floats arr = cache.GetFloats(((AtomicReader)readerContext.Reader()), field
				, parser, true);
			Bits valid = cache.GetDocsWithField(((AtomicReader)readerContext.Reader()), field
				);
			return new _FloatDocValues_58(arr, valid, this);
		}

		private sealed class _FloatDocValues_58 : FloatDocValues
		{
			public _FloatDocValues_58(FieldCache.Floats arr, Bits valid, ValueSource baseArg1
				) : base(baseArg1)
			{
				this.arr = arr;
				this.valid = valid;
			}

			public override float FloatVal(int doc)
			{
				return arr.Get(doc);
			}

			public override object ObjectVal(int doc)
			{
				return valid.Get(doc) ? arr.Get(doc) : null;
			}

			public override bool Exists(int doc)
			{
				return arr.Get(doc) != 0 || valid.Get(doc);
			}

			public override FunctionValues.ValueFiller GetValueFiller()
			{
				return new _ValueFiller_76(arr, valid);
			}

			private sealed class _ValueFiller_76 : FunctionValues.ValueFiller
			{
				public _ValueFiller_76(FieldCache.Floats arr, Bits valid)
				{
					this.arr = arr;
					this.valid = valid;
					this.mval = new MutableValueFloat();
				}

				private readonly MutableValueFloat mval;

				public override MutableValue GetValue()
				{
					return this.mval;
				}

				public override void FillValue(int doc)
				{
					this.mval.value = arr.Get(doc);
					this.mval.exists = this.mval.value != 0 || valid.Get(doc);
				}

				private readonly FieldCache.Floats arr;

				private readonly Bits valid;
			}

			private readonly FieldCache.Floats arr;

			private readonly Bits valid;
		}

		public override bool Equals(object o)
		{
			if (o.GetType() != typeof(Lucene.Net.Queries.Function.Valuesource.FloatFieldSource
				))
			{
				return false;
			}
			Lucene.Net.Queries.Function.Valuesource.FloatFieldSource other = (Lucene.Net.Queries.Function.Valuesource.FloatFieldSource
				)o;
			return base.Equals(other) && (this.parser == null ? other.parser == null : this.parser
				.GetType() == other.parser.GetType());
		}

		public override int GetHashCode()
		{
			int h = parser == null ? typeof(float).GetHashCode() : parser.GetType().GetHashCode
				();
			h += base.GetHashCode();
			return h;
		}
	}
}
