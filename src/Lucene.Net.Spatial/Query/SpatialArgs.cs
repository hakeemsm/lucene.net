/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Com.Spatial4j.Core.Context;
using Com.Spatial4j.Core.Shape;
using Lucene.Net.Spatial.Query;
using Sharpen;

namespace Lucene.Net.Spatial.Query
{
	/// <summary>
	/// Principally holds the query
	/// <see cref="Com.Spatial4j.Core.Shape.Shape">Com.Spatial4j.Core.Shape.Shape</see>
	/// and the
	/// <see cref="SpatialOperation">SpatialOperation</see>
	/// .
	/// It's used as an argument to some methods on
	/// <see cref="Lucene.Net.Spatial.SpatialStrategy">Lucene.Net.Spatial.SpatialStrategy
	/// 	</see>
	/// .
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class SpatialArgs
	{
		public const double DEFAULT_DISTERRPCT = 0.025d;

		private SpatialOperation operation;

		private Com.Spatial4j.Core.Shape.Shape shape;

		private double distErrPct;

		private double distErr;

		public SpatialArgs(SpatialOperation operation, Com.Spatial4j.Core.Shape.Shape shape
			)
		{
			if (operation == null || shape == null)
			{
				throw new ArgumentNullException("operation and shape are required");
			}
			this.operation = operation;
			this.shape = shape;
		}

		/// <summary>
		/// Computes the distance given a shape and the
		/// <code>distErrPct</code>
		/// .  The
		/// algorithm is the fraction of the distance from the center of the query
		/// shape to its closest bounding box corner.
		/// </summary>
		/// <param name="shape">Mandatory.</param>
		/// <param name="distErrPct">0 to 0.5</param>
		/// <param name="ctx">Mandatory</param>
		/// <returns>A distance (in degrees).</returns>
		public static double CalcDistanceFromErrPct(Com.Spatial4j.Core.Shape.Shape shape, 
			double distErrPct, SpatialContext ctx)
		{
			if (distErrPct < 0 || distErrPct > 0.5)
			{
				throw new ArgumentException("distErrPct " + distErrPct + " must be between [0 to 0.5]"
					);
			}
			if (distErrPct == 0 || shape is Point)
			{
				return 0;
			}
			Rectangle bbox = shape.GetBoundingBox();
			//Compute the distance from the center to a corner.  Because the distance
			// to a bottom corner vs a top corner can vary in a geospatial scenario,
			// take the closest one (greater precision).
			Point ctr = bbox.GetCenter();
			double y = (ctr.GetY() >= 0 ? bbox.GetMaxY() : bbox.GetMinY());
			double diagonalDist = ctx.GetDistCalc().Distance(ctr, bbox.GetMaxX(), y);
			return diagonalDist * distErrPct;
		}

		/// <summary>Gets the error distance that specifies how precise the query shape is.</summary>
		/// <remarks>
		/// Gets the error distance that specifies how precise the query shape is. This
		/// looks at
		/// <see cref="GetDistErr()">GetDistErr()</see>
		/// ,
		/// <see cref="GetDistErrPct()">GetDistErrPct()</see>
		/// , and
		/// <code>defaultDistErrPct</code>
		/// .
		/// </remarks>
		/// <param name="defaultDistErrPct">0 to 0.5</param>
		/// <returns>&gt;= 0</returns>
		public virtual double ResolveDistErr(SpatialContext ctx, double defaultDistErrPct
			)
		{
			if (distErr != null)
			{
				return distErr;
			}
			double distErrPct = (this.distErrPct != null ? this.distErrPct : defaultDistErrPct
				);
			return CalcDistanceFromErrPct(shape, distErrPct, ctx);
		}

		/// <summary>Check if the arguments make sense -- throw an exception if not</summary>
		/// <exception cref="System.ArgumentException"></exception>
		public virtual void Validate()
		{
			if (operation.IsTargetNeedsArea() && !shape.HasArea())
			{
				throw new ArgumentException(operation + " only supports geometry with area");
			}
			if (distErr != null && distErrPct != null)
			{
				throw new ArgumentException("Only distErr or distErrPct can be specified.");
			}
		}

		public override string ToString()
		{
			return SpatialArgsParser.WriteSpatialArgs(this);
		}

		//------------------------------------------------
		// Getters & Setters
		//------------------------------------------------
		public virtual SpatialOperation GetOperation()
		{
			return operation;
		}

		public virtual void SetOperation(SpatialOperation operation)
		{
			this.operation = operation;
		}

		public virtual Com.Spatial4j.Core.Shape.Shape GetShape()
		{
			return shape;
		}

		public virtual void SetShape(Com.Spatial4j.Core.Shape.Shape shape)
		{
			this.shape = shape;
		}

		/// <summary>A measure of acceptable error of the shape as a fraction.</summary>
		/// <remarks>
		/// A measure of acceptable error of the shape as a fraction.  This effectively
		/// inflates the size of the shape but should not shrink it.
		/// </remarks>
		/// <returns>0 to 0.5</returns>
		/// <seealso cref="CalcDistanceFromErrPct(Com.Spatial4j.Core.Shape.Shape, double, Com.Spatial4j.Core.Context.SpatialContext)
		/// 	">CalcDistanceFromErrPct(Com.Spatial4j.Core.Shape.Shape, double, Com.Spatial4j.Core.Context.SpatialContext)
		/// 	</seealso>
		public virtual double GetDistErrPct()
		{
			return distErrPct;
		}

		public virtual void SetDistErrPct(double distErrPct)
		{
			if (distErrPct != null)
			{
				this.distErrPct = distErrPct;
			}
		}

		/// <summary>The acceptable error of the shape.</summary>
		/// <remarks>
		/// The acceptable error of the shape.  This effectively inflates the
		/// size of the shape but should not shrink it.
		/// </remarks>
		/// <returns>&gt;= 0</returns>
		public virtual double GetDistErr()
		{
			return distErr;
		}

		public virtual void SetDistErr(double distErr)
		{
			this.distErr = distErr;
		}
	}
}
