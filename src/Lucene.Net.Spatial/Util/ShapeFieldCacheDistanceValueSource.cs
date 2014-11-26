/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections;
using System.Collections.Generic;
using Com.Spatial4j.Core.Context;
using Com.Spatial4j.Core.Distance;
using Com.Spatial4j.Core.Shape;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Spatial.Util;
using Sharpen;

namespace Lucene.Net.Spatial.Util
{
	/// <summary>
	/// An implementation of the Lucene ValueSource that returns the spatial distance
	/// between an input point and a document's points in
	/// <see cref="ShapeFieldCacheProvider{T}">ShapeFieldCacheProvider&lt;T&gt;</see>
	/// . The shortest distance is returned if a
	/// document has more than one point.
	/// </summary>
	/// <lucene.internal></lucene.internal>
	public class ShapeFieldCacheDistanceValueSource : ValueSource
	{
		private readonly SpatialContext ctx;

		private readonly Point from;

		private readonly ShapeFieldCacheProvider<Point> provider;

		private readonly double multiplier;

		public ShapeFieldCacheDistanceValueSource(SpatialContext ctx, ShapeFieldCacheProvider
			<Point> provider, Point from, double multiplier)
		{
			this.ctx = ctx;
			this.from = from;
			this.provider = provider;
			this.multiplier = multiplier;
		}

		public override string Description()
		{
			return GetType().Name + "(" + provider + ", " + from + ")";
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FunctionValues GetValues(IDictionary context, AtomicReaderContext
			 readerContext)
		{
			return new _FunctionValues_61(this, readerContext);
		}

		private sealed class _FunctionValues_61 : FunctionValues
		{
			public _FunctionValues_61(ShapeFieldCacheDistanceValueSource _enclosing, AtomicReaderContext
				 readerContext)
			{
				this._enclosing = _enclosing;
				this.readerContext = readerContext;
				this.cache = this._enclosing.provider.GetCache(((AtomicReader)readerContext.Reader
					()));
				this.from = this._enclosing.from;
				this.calculator = this._enclosing.ctx.GetDistCalc();
				this.nullValue = (this._enclosing.ctx.IsGeo() ? 180 * this._enclosing.multiplier : 
					double.MaxValue);
			}

			private readonly ShapeFieldCache<Point> cache;

			private readonly Point from;

			private readonly DistanceCalculator calculator;

			private readonly double nullValue;

			public override float FloatVal(int doc)
			{
				return (float)this.DoubleVal(doc);
			}

			public override double DoubleVal(int doc)
			{
				IList<Point> vals = this.cache.GetShapes(doc);
				if (vals != null)
				{
					double v = this.calculator.Distance(this.from, vals[0]);
					for (int i = 1; i < vals.Count; i++)
					{
						v = Math.Min(v, this.calculator.Distance(this.from, vals[i]));
					}
					return v * this._enclosing.multiplier;
				}
				return this.nullValue;
			}

			public override string ToString(int doc)
			{
				return this._enclosing.Description() + "=" + this.FloatVal(doc);
			}

			private readonly ShapeFieldCacheDistanceValueSource _enclosing;

			private readonly AtomicReaderContext readerContext;
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
			Lucene.Net.Spatial.Util.ShapeFieldCacheDistanceValueSource that = (Lucene.Net.Spatial.Util.ShapeFieldCacheDistanceValueSource
				)o;
			if (!ctx.Equals(that.ctx))
			{
				return false;
			}
			if (!from.Equals(that.from))
			{
				return false;
			}
			if (!provider.Equals(that.provider))
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
	}
}
