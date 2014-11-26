/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Com.Spatial4j.Core.Context;
using Com.Spatial4j.Core.Shape;
using Lucene.Net.Document;
using Lucene.Net.Queries.Function;
using Lucene.Net.Queries.Function.Valuesource;
using Lucene.Net.Search;
using Lucene.Net.Spatial.Query;
using Sharpen;

namespace Lucene.Net.Spatial
{
	/// <summary>
	/// The SpatialStrategy encapsulates an approach to indexing and searching based
	/// on shapes.
	/// </summary>
	/// <remarks>
	/// The SpatialStrategy encapsulates an approach to indexing and searching based
	/// on shapes.
	/// <p/>
	/// Different implementations will support different features. A strategy should
	/// document these common elements:
	/// <ul>
	/// <li>Can it index more than one shape per field?</li>
	/// <li>What types of shapes can be indexed?</li>
	/// <li>What types of query shapes can be used?</li>
	/// <li>What types of query operations are supported?
	/// This might vary per shape.</li>
	/// <li>Does it use the
	/// <see cref="Lucene.Net.Search.FieldCache">Lucene.Net.Search.FieldCache
	/// 	</see>
	/// ,
	/// or some other type of cache?  When?
	/// </ul>
	/// If a strategy only supports certain shapes at index or query time, then in
	/// general it will throw an exception if given an incompatible one.  It will not
	/// be coerced into compatibility.
	/// <p/>
	/// Note that a SpatialStrategy is not involved with the Lucene stored field
	/// values of shapes, which is immaterial to indexing & search.
	/// <p/>
	/// Thread-safe.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public abstract class SpatialStrategy
	{
		protected internal readonly SpatialContext ctx;

		private readonly string fieldName;

		/// <summary>Constructs the spatial strategy with its mandatory arguments.</summary>
		/// <remarks>Constructs the spatial strategy with its mandatory arguments.</remarks>
		public SpatialStrategy(SpatialContext ctx, string fieldName)
		{
			if (ctx == null)
			{
				throw new ArgumentException("ctx is required");
			}
			this.ctx = ctx;
			if (fieldName == null || fieldName.Length == 0)
			{
				throw new ArgumentException("fieldName is required");
			}
			this.fieldName = fieldName;
		}

		public virtual SpatialContext GetSpatialContext()
		{
			return ctx;
		}

		/// <summary>
		/// The name of the field or the prefix of them if there are multiple
		/// fields needed internally.
		/// </summary>
		/// <remarks>
		/// The name of the field or the prefix of them if there are multiple
		/// fields needed internally.
		/// </remarks>
		/// <returns>Not null.</returns>
		public virtual string GetFieldName()
		{
			return fieldName;
		}

		/// <summary>
		/// Returns the IndexableField(s) from the
		/// <code>shape</code>
		/// that are to be
		/// added to the
		/// <see cref="Lucene.Net.Document.Document">Lucene.Net.Document.Document
		/// 	</see>
		/// .  These fields
		/// are expected to be marked as indexed and not stored.
		/// <p/>
		/// Note: If you want to <i>store</i> the shape as a string for retrieval in
		/// search results, you could add it like this:
		/// <pre>document.add(new StoredField(fieldName,ctx.toString(shape)));</pre>
		/// The particular string representation used doesn't matter to the Strategy
		/// since it doesn't use it.
		/// </summary>
		/// <returns>Not null nor will it have null elements.</returns>
		/// <exception cref="System.NotSupportedException">if given a shape incompatible with the strategy
		/// 	</exception>
		public abstract Field[] CreateIndexableFields(Com.Spatial4j.Core.Shape.Shape shape
			);

		/// <summary>
		/// See
		/// <see cref="MakeDistanceValueSource(Com.Spatial4j.Core.Shape.Point, double)">MakeDistanceValueSource(Com.Spatial4j.Core.Shape.Point, double)
		/// 	</see>
		/// called with
		/// a multiplier of 1.0 (i.e. units of degrees).
		/// </summary>
		public virtual ValueSource MakeDistanceValueSource(Point queryPoint)
		{
			return MakeDistanceValueSource(queryPoint, 1.0);
		}

		/// <summary>
		/// Make a ValueSource returning the distance between the center of the
		/// indexed shape and
		/// <code>queryPoint</code>
		/// .  If there are multiple indexed shapes
		/// then the closest one is chosen. The result is multiplied by
		/// <code>multiplier</code>
		/// , which
		/// conveniently is used to get the desired units.
		/// </summary>
		public abstract ValueSource MakeDistanceValueSource(Point queryPoint, double multiplier
			);

		/// <summary>
		/// Make a Query based principally on
		/// <see cref="Lucene.Net.Spatial.Query.SpatialOperation">Lucene.Net.Spatial.Query.SpatialOperation
		/// 	</see>
		/// and
		/// <see cref="Com.Spatial4j.Core.Shape.Shape">Com.Spatial4j.Core.Shape.Shape</see>
		/// from the supplied
		/// <code>args</code>
		/// .
		/// The default implementation is
		/// <pre>return new ConstantScoreQuery(makeFilter(args));</pre>
		/// </summary>
		/// <exception cref="System.NotSupportedException">
		/// If the strategy does not support the shape in
		/// <code>args</code>
		/// </exception>
		/// <exception cref="Lucene.Net.Spatial.Query.UnsupportedSpatialOperation">
		/// If the strategy does not support the
		/// <see cref="Lucene.Net.Spatial.Query.SpatialOperation">Lucene.Net.Spatial.Query.SpatialOperation
		/// 	</see>
		/// in
		/// <code>args</code>
		/// .
		/// </exception>
		public virtual Query MakeQuery(SpatialArgs args)
		{
			return new ConstantScoreQuery(MakeFilter(args));
		}

		/// <summary>
		/// Make a Filter based principally on
		/// <see cref="Lucene.Net.Spatial.Query.SpatialOperation">Lucene.Net.Spatial.Query.SpatialOperation
		/// 	</see>
		/// and
		/// <see cref="Com.Spatial4j.Core.Shape.Shape">Com.Spatial4j.Core.Shape.Shape</see>
		/// from the supplied
		/// <code>args</code>
		/// .
		/// <p />
		/// If a subclasses implements
		/// <see cref="MakeQuery(Lucene.Net.Spatial.Query.SpatialArgs)">MakeQuery(Lucene.Net.Spatial.Query.SpatialArgs)
		/// 	</see>
		/// then this method could be simply:
		/// <pre>return new QueryWrapperFilter(makeQuery(args).getQuery());</pre>
		/// </summary>
		/// <exception cref="System.NotSupportedException">
		/// If the strategy does not support the shape in
		/// <code>args</code>
		/// </exception>
		/// <exception cref="Lucene.Net.Spatial.Query.UnsupportedSpatialOperation">
		/// If the strategy does not support the
		/// <see cref="Lucene.Net.Spatial.Query.SpatialOperation">Lucene.Net.Spatial.Query.SpatialOperation
		/// 	</see>
		/// in
		/// <code>args</code>
		/// .
		/// </exception>
		public abstract Filter MakeFilter(SpatialArgs args);

		/// <summary>
		/// Returns a ValueSource with values ranging from 1 to 0, depending inversely
		/// on the distance from
		/// <see cref="MakeDistanceValueSource(Com.Spatial4j.Core.Shape.Point, double)">MakeDistanceValueSource(Com.Spatial4j.Core.Shape.Point, double)
		/// 	</see>
		/// .
		/// The formula is
		/// <code>c/(d + c)</code>
		/// where 'd' is the distance and 'c' is
		/// one tenth the distance to the farthest edge from the center. Thus the
		/// scores will be 1 for indexed points at the center of the query shape and as
		/// low as ~0.1 at its furthest edges.
		/// </summary>
		public ValueSource MakeRecipDistanceValueSource(Com.Spatial4j.Core.Shape.Shape queryShape
			)
		{
			Rectangle bbox = queryShape.GetBoundingBox();
			double diagonalDist = ctx.GetDistCalc().Distance(ctx.MakePoint(bbox.GetMinX(), bbox
				.GetMinY()), bbox.GetMaxX(), bbox.GetMaxY());
			double distToEdge = diagonalDist * 0.5;
			float c = (float)distToEdge * 0.1f;
			//one tenth
			return new ReciprocalFloatFunction(MakeDistanceValueSource(queryShape.GetCenter()
				, 1.0), 1f, c, c);
		}

		public override string ToString()
		{
			return GetType().Name + " field:" + fieldName + " ctx=" + ctx;
		}
	}
}
