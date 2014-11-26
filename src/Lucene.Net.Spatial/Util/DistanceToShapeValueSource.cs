/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections;
using Com.Spatial4j.Core.Context;
using Com.Spatial4j.Core.Distance;
using Com.Spatial4j.Core.Shape;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Queries.Function.Docvalues;
using Lucene.Net.Search;
using Sharpen;

namespace Lucene.Net.Spatial.Util
{
	/// <summary>
	/// The distance from a provided Point to a Point retrieved from a ValueSource via
	/// <see cref="Lucene.Net.Queries.Function.FunctionValues.ObjectVal(int)">Lucene.Net.Queries.Function.FunctionValues.ObjectVal(int)
	/// 	</see>
	/// . The distance
	/// is calculated via a
	/// <see cref="Com.Spatial4j.Core.Distance.DistanceCalculator">Com.Spatial4j.Core.Distance.DistanceCalculator
	/// 	</see>
	/// .
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class DistanceToShapeValueSource : ValueSource
	{
		private readonly ValueSource shapeValueSource;

		private readonly Point queryPoint;

		private readonly double multiplier;

		private readonly DistanceCalculator distCalc;

		private readonly double nullValue;

		public DistanceToShapeValueSource(ValueSource shapeValueSource, Point queryPoint, 
			double multiplier, SpatialContext ctx)
		{
			//TODO if FunctionValues returns NaN; will things be ok?
			//computed
			this.shapeValueSource = shapeValueSource;
			this.queryPoint = queryPoint;
			this.multiplier = multiplier;
			this.distCalc = ctx.GetDistCalc();
			this.nullValue = (ctx.IsGeo() ? 180 * multiplier : double.MaxValue);
		}

		public override string Description()
		{
			return "distance(" + queryPoint + " to " + shapeValueSource.Description() + ")*" 
				+ multiplier + ")";
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void CreateWeight(IDictionary context, IndexSearcher searcher)
		{
			shapeValueSource.CreateWeight(context, searcher);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FunctionValues GetValues(IDictionary context, AtomicReaderContext
			 readerContext)
		{
			FunctionValues shapeValues = shapeValueSource.GetValues(context, readerContext);
			return new _DoubleDocValues_74(this, shapeValues, this);
		}

		private sealed class _DoubleDocValues_74 : DoubleDocValues
		{
			public _DoubleDocValues_74(DistanceToShapeValueSource _enclosing, FunctionValues 
				shapeValues, ValueSource baseArg1) : base(baseArg1)
			{
				this._enclosing = _enclosing;
				this.shapeValues = shapeValues;
			}

			public override double DoubleVal(int doc)
			{
				Com.Spatial4j.Core.Shape.Shape shape = (Com.Spatial4j.Core.Shape.Shape)shapeValues
					.ObjectVal(doc);
				if (shape == null || shape.IsEmpty())
				{
					return this._enclosing.nullValue;
				}
				Point pt = shape.GetCenter();
				return this._enclosing.distCalc.Distance(this._enclosing.queryPoint, pt) * this._enclosing
					.multiplier;
			}

			public override Explanation Explain(int doc)
			{
				Explanation exp = base.Explain(doc);
				exp.AddDetail(shapeValues.Explain(doc));
				return exp;
			}

			private readonly DistanceToShapeValueSource _enclosing;

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
			Lucene.Net.Spatial.Util.DistanceToShapeValueSource that = (Lucene.Net.Spatial.Util.DistanceToShapeValueSource
				)o;
			if (!queryPoint.Equals(that.queryPoint))
			{
				return false;
			}
			if (double.Compare(that.multiplier, multiplier) != 0)
			{
				return false;
			}
			if (!shapeValueSource.Equals(that.shapeValueSource))
			{
				return false;
			}
			if (!distCalc.Equals(that.distCalc))
			{
				return false;
			}
			return true;
		}

		public override int GetHashCode()
		{
			int result;
			long temp;
			result = shapeValueSource.GetHashCode();
			result = 31 * result + queryPoint.GetHashCode();
			temp = double.DoubleToLongBits(multiplier);
			result = 31 * result + (int)(temp ^ ((long)(((ulong)temp) >> 32)));
			return result;
		}
	}
}
