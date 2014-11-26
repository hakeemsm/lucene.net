/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Queries.Function.Docvalues;
using Lucene.Net.Search;
using Lucene.Net.Spatial.Query;
using Sharpen;

namespace Lucene.Net.Spatial.Util
{
	/// <summary>
	/// A boolean ValueSource that compares a shape from a provided ValueSource with a given Shape and sees
	/// if it matches a given
	/// <see cref="Lucene.Net.Spatial.Query.SpatialOperation">Lucene.Net.Spatial.Query.SpatialOperation
	/// 	</see>
	/// (the predicate).
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class ShapePredicateValueSource : ValueSource
	{
		private readonly ValueSource shapeValuesource;

		private readonly SpatialOperation op;

		private readonly Com.Spatial4j.Core.Shape.Shape queryShape;

		/// <param name="shapeValuesource">
		/// Must yield
		/// <see cref="Com.Spatial4j.Core.Shape.Shape">Com.Spatial4j.Core.Shape.Shape</see>
		/// instances from it's objectVal(doc). If null
		/// then the result is false. This is the left-hand (indexed) side.
		/// </param>
		/// <param name="op">the predicate</param>
		/// <param name="queryShape">The shape on the right-hand (query) side.</param>
		public ShapePredicateValueSource(ValueSource shapeValuesource, SpatialOperation op
			, Com.Spatial4j.Core.Shape.Shape queryShape)
		{
			//the left hand side
			//the right hand side (constant)
			this.shapeValuesource = shapeValuesource;
			this.op = op;
			this.queryShape = queryShape;
		}

		public override string Description()
		{
			return shapeValuesource + " " + op + " " + queryShape;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void CreateWeight(IDictionary context, IndexSearcher searcher)
		{
			shapeValuesource.CreateWeight(context, searcher);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FunctionValues GetValues(IDictionary context, AtomicReaderContext
			 readerContext)
		{
			FunctionValues shapeValues = shapeValuesource.GetValues(context, readerContext);
			return new _BoolDocValues_70(this, shapeValues, this);
		}

		private sealed class _BoolDocValues_70 : BoolDocValues
		{
			public _BoolDocValues_70(ShapePredicateValueSource _enclosing, FunctionValues shapeValues
				, ValueSource baseArg1) : base(baseArg1)
			{
				this._enclosing = _enclosing;
				this.shapeValues = shapeValues;
			}

			public override bool BoolVal(int doc)
			{
				Com.Spatial4j.Core.Shape.Shape indexedShape = (Com.Spatial4j.Core.Shape.Shape)shapeValues
					.ObjectVal(doc);
				if (indexedShape == null)
				{
					return false;
				}
				return this._enclosing.op.Evaluate(indexedShape, this._enclosing.queryShape);
			}

			public override Explanation Explain(int doc)
			{
				Explanation exp = base.Explain(doc);
				exp.AddDetail(shapeValues.Explain(doc));
				return exp;
			}

			private readonly ShapePredicateValueSource _enclosing;

			private readonly FunctionValues shapeValues;
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
			Lucene.Net.Spatial.Util.ShapePredicateValueSource that = (Lucene.Net.Spatial.Util.ShapePredicateValueSource
				)o;
			if (!shapeValuesource.Equals(that.shapeValuesource))
			{
				return false;
			}
			if (!op.Equals(that.op))
			{
				return false;
			}
			if (!queryShape.Equals(that.queryShape))
			{
				return false;
			}
			return true;
		}

		public override int GetHashCode()
		{
			int result = shapeValuesource.GetHashCode();
			result = 31 * result + op.GetHashCode();
			result = 31 * result + queryShape.GetHashCode();
			return result;
		}
	}
}
