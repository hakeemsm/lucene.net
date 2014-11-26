/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Com.Spatial4j.Core.Context;
using Com.Spatial4j.Core.Distance;
using Com.Spatial4j.Core.Shape;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Spatial.Prefix;
using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Spatial.Prefix
{
	/// <summary>
	/// Finds docs where its indexed shape is
	/// <see cref="Lucene.Net.Spatial.Query.SpatialOperation.IsWithin">WITHIN</see>
	/// the query shape.  It works by looking at cells outside of the query
	/// shape to ensure documents there are excluded. By default, it will
	/// examine all cells, and it's fairly slow.  If you know that the indexed shapes
	/// are never comprised of multiple disjoint parts (which also means it is not multi-valued),
	/// then you can pass
	/// <code>SpatialPrefixTree.getDistanceForLevel(maxLevels)</code>
	/// as
	/// the
	/// <code>queryBuffer</code>
	/// constructor parameter to minimally look this distance
	/// beyond the query shape's edge.  Even if the indexed shapes are sometimes
	/// comprised of multiple disjoint parts, you might want to use this option with
	/// a large buffer as a faster approximation with minimal false-positives.
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class WithinPrefixTreeFilter : AbstractVisitingPrefixTreeFilter
	{
		private readonly Com.Spatial4j.Core.Shape.Shape bufferedQueryShape;

		/// <summary>
		/// See
		/// <see cref="AbstractVisitingPrefixTreeFilter.AbstractVisitingPrefixTreeFilter(Com.Spatial4j.Core.Shape.Shape, string, Lucene.Net.Spatial.Prefix.Tree.SpatialPrefixTree, int, int)
		/// 	">AbstractVisitingPrefixTreeFilter.AbstractVisitingPrefixTreeFilter(Com.Spatial4j.Core.Shape.Shape, string, Lucene.Net.Spatial.Prefix.Tree.SpatialPrefixTree, int, int)
		/// 	</see>
		/// .
		/// <code>queryBuffer</code>
		/// is the (minimum) distance beyond the query shape edge
		/// where non-matching documents are looked for so they can be excluded. If
		/// -1 is used then the whole world is examined (a good default for correctness).
		/// </summary>
		public WithinPrefixTreeFilter(Com.Spatial4j.Core.Shape.Shape queryShape, string fieldName
			, SpatialPrefixTree grid, int detailLevel, int prefixGridScanLevel, double queryBuffer
			) : base(queryShape, fieldName, grid, detailLevel, prefixGridScanLevel)
		{
			//TODO LUCENE-4869: implement faster algorithm based on filtering out false-positives of a
			//  minimal query buffer by looking in a DocValues cache holding a representative
			//  point of each disjoint component of a document's shape(s).
			//if null then the whole world
			if (queryBuffer == -1)
			{
				this.bufferedQueryShape = null;
			}
			else
			{
				this.bufferedQueryShape = BufferShape(queryShape, queryBuffer);
			}
		}

		/// <summary>Returns a new shape that is larger than shape by at distErr.</summary>
		/// <remarks>Returns a new shape that is larger than shape by at distErr.</remarks>
		protected internal virtual Com.Spatial4j.Core.Shape.Shape BufferShape(Com.Spatial4j.Core.Shape.Shape
			 shape, double distErr)
		{
			//TODO move this generic code elsewhere?  Spatial4j?
			if (distErr <= 0)
			{
				throw new ArgumentException("distErr must be > 0");
			}
			SpatialContext ctx = grid.GetSpatialContext();
			if (shape is Point)
			{
				return ctx.MakeCircle((Point)shape, distErr);
			}
			else
			{
				if (shape is Circle)
				{
					Circle circle = (Circle)shape;
					double newDist = circle.GetRadius() + distErr;
					if (ctx.IsGeo() && newDist > 180)
					{
						newDist = 180;
					}
					return ctx.MakeCircle(circle.GetCenter(), newDist);
				}
				else
				{
					Rectangle bbox = shape.GetBoundingBox();
					double newMinX = bbox.GetMinX() - distErr;
					double newMaxX = bbox.GetMaxX() + distErr;
					double newMinY = bbox.GetMinY() - distErr;
					double newMaxY = bbox.GetMaxY() + distErr;
					if (ctx.IsGeo())
					{
						if (newMinY < -90)
						{
							newMinY = -90;
						}
						if (newMaxY > 90)
						{
							newMaxY = 90;
						}
						if (newMinY == -90 || newMaxY == 90 || bbox.GetWidth() + 2 * distErr > 360)
						{
							newMinX = -180;
							newMaxX = 180;
						}
						else
						{
							newMinX = DistanceUtils.NormLonDEG(newMinX);
							newMaxX = DistanceUtils.NormLonDEG(newMaxX);
						}
					}
					else
					{
						//restrict to world bounds
						newMinX = Math.Max(newMinX, ctx.GetWorldBounds().GetMinX());
						newMaxX = Math.Min(newMaxX, ctx.GetWorldBounds().GetMaxX());
						newMinY = Math.Max(newMinY, ctx.GetWorldBounds().GetMinY());
						newMaxY = Math.Min(newMaxY, ctx.GetWorldBounds().GetMaxY());
					}
					return ctx.MakeRectangle(newMinX, newMaxX, newMinY, newMaxY);
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs
			)
		{
			return new WithinPrefixTreeFilter.VisitorTemplateImpl(this, context, acceptDocs, 
				true).GetDocIdSet();
		}

		internal class VisitorTemplateImpl : AbstractVisitingPrefixTreeFilter.VisitorTemplate
		{
			/// <exception cref="System.IO.IOException"></exception>
			public VisitorTemplateImpl(WithinPrefixTreeFilter _enclosing, AtomicReaderContext
				 context, Bits acceptDocs, bool hasIndexedLeaves) : base(_enclosing)
			{
				this._enclosing = _enclosing;
			}

			private FixedBitSet inside;

			private FixedBitSet outside;

			private SpatialRelation visitRelation;

			//HM:revisit refactored anon class implementation to Impl class
			protected internal override void Start()
			{
				this.inside = new FixedBitSet(this.maxDoc);
				this.outside = new FixedBitSet(this.maxDoc);
			}

			protected internal override DocIdSet Finish()
			{
				this.inside.AndNot(this.outside);
				return this.inside;
			}

			protected internal override Iterator<Cell> FindSubCellsToVisit(Cell cell)
			{
				//use buffered query shape instead of orig.  Works with null too.
				return cell.GetSubCells(this._enclosing.bufferedQueryShape).Iterator();
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected internal override bool Visit(Cell cell)
			{
				//cell.relate is based on the bufferedQueryShape; we need to examine what
				// the relation is against the queryShape
				this.visitRelation = cell.GetShape().Relate(this._enclosing.queryShape);
				if (this.visitRelation == SpatialRelation.WITHIN)
				{
					this.CollectDocs(this.inside);
					return false;
				}
				else
				{
					if (this.visitRelation == SpatialRelation.DISJOINT)
					{
						this.CollectDocs(this.outside);
						return false;
					}
					else
					{
						if (cell.GetLevel() == this._enclosing.detailLevel)
						{
							this.CollectDocs(this.inside);
							return false;
						}
					}
				}
				return true;
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected internal override void VisitLeaf(Cell cell)
			{
				//visitRelation is declared as a field, populated by visit() so we don't recompute it
				//HM:revisit
				if (this.AllCellsIntersectQuery(cell, this.visitRelation))
				{
					this.CollectDocs(this.inside);
				}
				else
				{
					this.CollectDocs(this.outside);
				}
			}

			/// <summary>
			/// Returns true if the provided cell, and all its sub-cells down to
			/// detailLevel all intersect the queryShape.
			/// </summary>
			/// <remarks>
			/// Returns true if the provided cell, and all its sub-cells down to
			/// detailLevel all intersect the queryShape.
			/// </remarks>
			private bool AllCellsIntersectQuery(Cell cell, SpatialRelation relate)
			{
				if (relate == null)
				{
					relate = cell.GetShape().Relate(this._enclosing.queryShape);
				}
				if (cell.GetLevel() == this._enclosing.detailLevel)
				{
					return relate.Intersects();
				}
				if (relate == SpatialRelation.WITHIN)
				{
					return true;
				}
				if (relate == SpatialRelation.DISJOINT)
				{
					return false;
				}
				// Note: Generating all these cells just to determine intersection is not ideal.
				// It was easy to implement but could be optimized. For example if the docs
				// in question are already marked in the 'outside' bitset then it can be avoided.
				ICollection<Cell> subCells = cell.GetSubCells(null);
				foreach (Cell subCell in subCells)
				{
					if (!this.AllCellsIntersectQuery(subCell, null))
					{
						//recursion
						return false;
					}
				}
				return true;
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected internal override void VisitScanned(Cell cell)
			{
				if (this.AllCellsIntersectQuery(cell, null))
				{
					this.CollectDocs(this.inside);
				}
				else
				{
					this.CollectDocs(this.outside);
				}
			}

			private readonly WithinPrefixTreeFilter _enclosing;
		}
	}
}
