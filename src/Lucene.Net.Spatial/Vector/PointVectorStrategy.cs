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
using Lucene.Net.Search;
using Lucene.Net.Spatial;
using Lucene.Net.Spatial.Query;
using Lucene.Net.Spatial.Util;
using Lucene.Net.Spatial.Vector;
using Sharpen;

namespace Lucene.Net.Spatial.Vector
{
	/// <summary>
	/// Simple
	/// <see cref="Lucene.Net.Spatial.SpatialStrategy">Lucene.Net.Spatial.SpatialStrategy
	/// 	</see>
	/// which represents Points in two numeric
	/// <see cref="Lucene.Net.Document.DoubleField">Lucene.Net.Document.DoubleField
	/// 	</see>
	/// s.  The Strategy's best feature is decent distance sort.
	/// <h4>Characteristics:</h4>
	/// <ul>
	/// <li>Only indexes points; just one per field value.</li>
	/// <li>Can query by a rectangle or circle.</li>
	/// <li>
	/// <see cref="Lucene.Net.Spatial.Query.SpatialOperation.Intersects">Lucene.Net.Spatial.Query.SpatialOperation.Intersects
	/// 	</see>
	/// and
	/// <see cref="Lucene.Net.Spatial.Query.SpatialOperation.IsWithin">Lucene.Net.Spatial.Query.SpatialOperation.IsWithin
	/// 	</see>
	/// is supported.</li>
	/// <li>Uses the FieldCache for
	/// <see cref="Lucene.Net.Spatial.SpatialStrategy.MakeDistanceValueSource(Com.Spatial4j.Core.Shape.Point)
	/// 	">Lucene.Net.Spatial.SpatialStrategy.MakeDistanceValueSource(Com.Spatial4j.Core.Shape.Point)
	/// 	</see>
	/// and for
	/// searching with a Circle.</li>
	/// </ul>
	/// <h4>Implementation:</h4>
	/// This is a simple Strategy.  Search works with
	/// <see cref="Lucene.Net.Search.NumericRangeQuery{T}">Lucene.Net.Search.NumericRangeQuery&lt;T&gt;
	/// 	</see>
	/// s on
	/// an x & y pair of fields.  A Circle query does the same bbox query but adds a
	/// ValueSource filter on
	/// <see cref="Lucene.Net.Spatial.SpatialStrategy.MakeDistanceValueSource(Com.Spatial4j.Core.Shape.Point)
	/// 	">Lucene.Net.Spatial.SpatialStrategy.MakeDistanceValueSource(Com.Spatial4j.Core.Shape.Point)
	/// 	</see>
	/// .
	/// <p />
	/// One performance shortcoming with this strategy is that a scenario involving
	/// both a search using a Circle and sort will result in calculations for the
	/// spatial distance being done twice -- once for the filter and second for the
	/// sort.
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class PointVectorStrategy : SpatialStrategy
	{
		public static readonly string SUFFIX_X = "__x";

		public static readonly string SUFFIX_Y = "__y";

		private readonly string fieldNameX;

		private readonly string fieldNameY;

		public int precisionStep = 8;

		public PointVectorStrategy(SpatialContext ctx, string fieldNamePrefix) : base(ctx
			, fieldNamePrefix)
		{
			// same as solr default
			this.fieldNameX = fieldNamePrefix + SUFFIX_X;
			this.fieldNameY = fieldNamePrefix + SUFFIX_Y;
		}

		public virtual void SetPrecisionStep(int p)
		{
			precisionStep = p;
			if (precisionStep <= 0 || precisionStep >= 64)
			{
				precisionStep = int.MaxValue;
			}
		}

		internal virtual string GetFieldNameX()
		{
			return fieldNameX;
		}

		internal virtual string GetFieldNameY()
		{
			return fieldNameY;
		}

		public override Field[] CreateIndexableFields(Com.Spatial4j.Core.Shape.Shape shape
			)
		{
			if (shape is Point)
			{
				return CreateIndexableFields((Point)shape);
			}
			throw new NotSupportedException("Can only index Point, not " + shape);
		}

		/// <seealso cref="CreateIndexableFields(Com.Spatial4j.Core.Shape.Shape)"></seealso>
		public virtual Field[] CreateIndexableFields(Point point)
		{
			FieldType doubleFieldType = new FieldType(DoubleField.TYPE_NOT_STORED);
			doubleFieldType.SetNumericPrecisionStep(precisionStep);
			Field[] f = new Field[2];
			f[0] = new DoubleField(fieldNameX, point.GetX(), doubleFieldType);
			f[1] = new DoubleField(fieldNameY, point.GetY(), doubleFieldType);
			return f;
		}

		public override ValueSource MakeDistanceValueSource(Point queryPoint, double multiplier
			)
		{
			return new DistanceValueSource(this, queryPoint, multiplier);
		}

		public override Filter MakeFilter(SpatialArgs args)
		{
			//unwrap the CSQ from makeQuery
			ConstantScoreQuery csq = ((ConstantScoreQuery)MakeQuery(args));
			Filter filter = csq.GetFilter();
			if (filter != null)
			{
				return filter;
			}
			else
			{
				return new QueryWrapperFilter(csq.GetQuery());
			}
		}

		public override Lucene.Net.Search.Query MakeQuery(SpatialArgs args)
		{
			if (!SpatialOperation.Is(args.GetOperation(), SpatialOperation.Intersects, SpatialOperation
				.IsWithin))
			{
				throw new UnsupportedSpatialOperation(args.GetOperation());
			}
			Com.Spatial4j.Core.Shape.Shape shape = args.GetShape();
			if (shape is Rectangle)
			{
				Rectangle bbox = (Rectangle)shape;
				return new ConstantScoreQuery(MakeWithin(bbox));
			}
			else
			{
				if (shape is Circle)
				{
					Circle circle = (Circle)shape;
					Rectangle bbox = circle.GetBoundingBox();
					ValueSourceFilter vsf = new ValueSourceFilter(new QueryWrapperFilter(MakeWithin(bbox
						)), MakeDistanceValueSource(circle.GetCenter()), 0, circle.GetRadius());
					return new ConstantScoreQuery(vsf);
				}
				else
				{
					throw new NotSupportedException("Only Rectangles and Circles are currently supported, "
						 + "found [" + shape.GetType() + "]");
				}
			}
		}

		//TODO
		//TODO this is basically old code that hasn't been verified well and should probably be removed
		public virtual Lucene.Net.Search.Query MakeQueryDistanceScore(SpatialArgs 
			args)
		{
			// For starters, just limit the bbox
			Com.Spatial4j.Core.Shape.Shape shape = args.GetShape();
			if (!(shape is Rectangle || shape is Circle))
			{
				throw new NotSupportedException("Only Rectangles and Circles are currently supported, "
					 + "found [" + shape.GetType() + "]");
			}
			//TODO
			Rectangle bbox = shape.GetBoundingBox();
			if (bbox.GetCrossesDateLine())
			{
				throw new NotSupportedException("Crossing dateline not yet supported");
			}
			ValueSource valueSource = null;
			Lucene.Net.Search.Query spatial = null;
			SpatialOperation op = args.GetOperation();
			if (SpatialOperation.Is(op, SpatialOperation.BBoxWithin, SpatialOperation.BBoxIntersects
				))
			{
				spatial = MakeWithin(bbox);
			}
			else
			{
				if (SpatialOperation.Is(op, SpatialOperation.Intersects, SpatialOperation.IsWithin
					))
				{
					spatial = MakeWithin(bbox);
					if (args.GetShape() is Circle)
					{
						Circle circle = (Circle)args.GetShape();
						// Make the ValueSource
						valueSource = MakeDistanceValueSource(shape.GetCenter());
						ValueSourceFilter vsf = new ValueSourceFilter(new QueryWrapperFilter(spatial), valueSource
							, 0, circle.GetRadius());
						spatial = new FilteredQuery(new MatchAllDocsQuery(), vsf);
					}
				}
				else
				{
					if (op == SpatialOperation.IsDisjointTo)
					{
						spatial = MakeDisjoint(bbox);
					}
				}
			}
			if (spatial == null)
			{
				throw new UnsupportedSpatialOperation(args.GetOperation());
			}
			if (valueSource != null)
			{
				valueSource = new CachingDoubleValueSource(valueSource);
			}
			else
			{
				valueSource = MakeDistanceValueSource(shape.GetCenter());
			}
			Lucene.Net.Search.Query spatialRankingQuery = new FunctionQuery(valueSource
				);
			BooleanQuery bq = new BooleanQuery();
			bq.Add(spatial, BooleanClause.Occur.MUST);
			bq.Add(spatialRankingQuery, BooleanClause.Occur.MUST);
			return bq;
		}

		/// <summary>Constructs a query to retrieve documents that fully contain the input envelope.
		/// 	</summary>
		/// <remarks>Constructs a query to retrieve documents that fully contain the input envelope.
		/// 	</remarks>
		private Lucene.Net.Search.Query MakeWithin(Rectangle bbox)
		{
			BooleanQuery bq = new BooleanQuery();
			BooleanClause.Occur MUST = BooleanClause.Occur.MUST;
			if (bbox.GetCrossesDateLine())
			{
				//use null as performance trick since no data will be beyond the world bounds
				bq.Add(RangeQuery(fieldNameX, null, bbox.GetMaxX()), BooleanClause.Occur.SHOULD);
				bq.Add(RangeQuery(fieldNameX, bbox.GetMinX(), null), BooleanClause.Occur.SHOULD);
				bq.SetMinimumNumberShouldMatch(1);
			}
			else
			{
				//must match at least one of the SHOULD
				bq.Add(RangeQuery(fieldNameX, bbox.GetMinX(), bbox.GetMaxX()), MUST);
			}
			bq.Add(RangeQuery(fieldNameY, bbox.GetMinY(), bbox.GetMaxY()), MUST);
			return bq;
		}

		private NumericRangeQuery<double> RangeQuery(string fieldName, double min, double
			 max)
		{
			return NumericRangeQuery.NewDoubleRange(fieldName, precisionStep, min, max, true, 
				true);
		}

		//inclusive
		/// <summary>Constructs a query to retrieve documents that fully contain the input envelope.
		/// 	</summary>
		/// <remarks>Constructs a query to retrieve documents that fully contain the input envelope.
		/// 	</remarks>
		private Lucene.Net.Search.Query MakeDisjoint(Rectangle bbox)
		{
			if (bbox.GetCrossesDateLine())
			{
				throw new NotSupportedException("makeDisjoint doesn't handle dateline cross");
			}
			Lucene.Net.Search.Query qX = RangeQuery(fieldNameX, bbox.GetMinX(), bbox.GetMaxX
				());
			Lucene.Net.Search.Query qY = RangeQuery(fieldNameY, bbox.GetMinY(), bbox.GetMaxY
				());
			BooleanQuery bq = new BooleanQuery();
			bq.Add(qX, BooleanClause.Occur.MUST_NOT);
			bq.Add(qY, BooleanClause.Occur.MUST_NOT);
			return bq;
		}
	}
}
