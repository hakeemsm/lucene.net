/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections;
using Com.Spatial4j.Core.Distance;
using Com.Spatial4j.Core.Shape;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Search;
using Lucene.Net.Spatial.Vector;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Spatial.Vector
{
	/// <summary>
	/// An implementation of the Lucene ValueSource model that returns the distance
	/// for a
	/// <see cref="PointVectorStrategy">PointVectorStrategy</see>
	/// .
	/// </summary>
	/// <lucene.internal></lucene.internal>
	public class DistanceValueSource : ValueSource
	{
		private PointVectorStrategy strategy;

		private readonly Point from;

		private readonly double multiplier;

		/// <summary>Constructor.</summary>
		/// <remarks>Constructor.</remarks>
		public DistanceValueSource(PointVectorStrategy strategy, Point from, double multiplier
			)
		{
			this.strategy = strategy;
			this.from = from;
			this.multiplier = multiplier;
		}

		/// <summary>Returns the ValueSource description.</summary>
		/// <remarks>Returns the ValueSource description.</remarks>
		public override string Description()
		{
			return "DistanceValueSource(" + strategy + ", " + from + ")";
		}

		/// <summary>Returns the FunctionValues used by the function query.</summary>
		/// <remarks>Returns the FunctionValues used by the function query.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public override FunctionValues GetValues(IDictionary context, AtomicReaderContext
			 readerContext)
		{
			AtomicReader reader = ((AtomicReader)readerContext.Reader());
			DistanceValueSource.FunctionValuesImpl fvImpl = new DistanceValueSource.FunctionValuesImpl
				(this, readerContext);
			return fvImpl;
		}

		//HM:revisit refactored anon class to explicit Impl
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
			Lucene.Net.Spatial.Vector.DistanceValueSource that = (Lucene.Net.Spatial.Vector.DistanceValueSource
				)o;
			if (!from.Equals(that.from))
			{
				return false;
			}
			if (!strategy.Equals(that.strategy))
			{
				return false;
			}
			if (multiplier != that.multiplier)
			{
				return false;
			}
			return true;
		}

		public override int GetHashCode()
		{
			return from.GetHashCode();
		}

		internal class FunctionValuesImpl : FunctionValues
		{
			internal AtomicReader reader;

			/// <exception cref="System.IO.IOException"></exception>
			public FunctionValuesImpl(DistanceValueSource _enclosing, AtomicReaderContext atmReaderCtxt
				)
			{
				this._enclosing = _enclosing;
				this.reader = ((AtomicReader)atmReaderCtxt.Reader());
			}

			internal readonly FieldCache.Doubles ptX = FieldCache.DEFAULT.GetDoubles(this.reader
				, this._enclosing.strategy.GetFieldNameX(), true);

			internal readonly FieldCache.Doubles ptY = FieldCache.DEFAULT.GetDoubles(this.reader
				, this._enclosing.strategy.GetFieldNameY(), true);

			internal readonly Bits validX = FieldCache.DEFAULT.GetDocsWithField(this.reader, 
				this._enclosing.strategy.GetFieldNameX());

			internal readonly Bits validY = FieldCache.DEFAULT.GetDocsWithField(this.reader, 
				this._enclosing.strategy.GetFieldNameY());

			private readonly Point from = this._enclosing.from;

			private readonly DistanceCalculator calculator = this._enclosing.strategy.GetSpatialContext
				().GetDistCalc();

			private readonly double nullValue = (this._enclosing.strategy.GetSpatialContext()
				.IsGeo() ? 180 * this._enclosing.multiplier : double.MaxValue);

			public override float FloatVal(int doc)
			{
				return (float)this.DoubleVal(doc);
			}

			public override double DoubleVal(int doc)
			{
				// make sure it has minX and area
				if (this.validX.Get(doc))
				{
					//HM:revisit
					//assert validY.get(doc);
					return this.calculator.Distance(this.from, this.ptX.Get(doc), this.ptY.Get(doc)) 
						* this._enclosing.multiplier;
				}
				return this.nullValue;
			}

			public override string ToString(int doc)
			{
				return this._enclosing.Description() + "=" + this.FloatVal(doc);
			}

			private readonly DistanceValueSource _enclosing;
		}
	}
}
