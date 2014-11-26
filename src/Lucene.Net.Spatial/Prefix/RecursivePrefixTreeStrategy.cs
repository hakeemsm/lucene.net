/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Search;
using Lucene.Net.Spatial;
using Lucene.Net.Spatial.Prefix;
using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Spatial.Query;
using Sharpen;

namespace Lucene.Net.Spatial.Prefix
{
	/// <summary>
	/// A
	/// <see cref="PrefixTreeStrategy">PrefixTreeStrategy</see>
	/// which uses
	/// <see cref="AbstractVisitingPrefixTreeFilter">AbstractVisitingPrefixTreeFilter</see>
	/// .
	/// This strategy has support for searching non-point shapes (note: not tested).
	/// Even a query shape with distErrPct=0 (fully precise to the grid) should have
	/// good performance for typical data, unless there is a lot of indexed data
	/// coincident with the shape's edge.
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class RecursivePrefixTreeStrategy : PrefixTreeStrategy
	{
		private int prefixGridScanLevel;

		/// <summary>True if only indexed points shall be supported.</summary>
		/// <remarks>
		/// True if only indexed points shall be supported.  See
		/// <see cref="IntersectsPrefixTreeFilter#hasIndexedLeaves">IntersectsPrefixTreeFilter#hasIndexedLeaves
		/// 	</see>
		/// .
		/// </remarks>
		protected internal bool pointsOnly = false;

		/// <summary>
		/// See
		/// <see cref="ContainsPrefixTreeFilter.multiOverlappingIndexedShapes">ContainsPrefixTreeFilter.multiOverlappingIndexedShapes
		/// 	</see>
		/// .
		/// </summary>
		protected internal bool multiOverlappingIndexedShapes = true;

		public RecursivePrefixTreeStrategy(SpatialPrefixTree grid, string fieldName) : base
			(grid, fieldName, true)
		{
			//simplify indexed cells
			prefixGridScanLevel = grid.GetMaxLevels() - 4;
		}

		//TODO this default constant is dependent on the prefix grid size
		/// <summary>
		/// Sets the grid level [1-maxLevels] at which indexed terms are scanned brute-force
		/// instead of by grid decomposition.
		/// </summary>
		/// <remarks>
		/// Sets the grid level [1-maxLevels] at which indexed terms are scanned brute-force
		/// instead of by grid decomposition.  By default this is maxLevels - 4.  The
		/// final level, maxLevels, is always scanned.
		/// </remarks>
		/// <param name="prefixGridScanLevel">1 to maxLevels</param>
		public virtual void SetPrefixGridScanLevel(int prefixGridScanLevel)
		{
			//TODO if negative then subtract from maxlevels
			this.prefixGridScanLevel = prefixGridScanLevel;
		}

		public override string ToString()
		{
			return GetType().Name + "(prefixGridScanLevel:" + prefixGridScanLevel + ",SPG:(" 
				+ grid + "))";
		}

		public override Filter MakeFilter(SpatialArgs args)
		{
			SpatialOperation op = args.GetOperation();
			if (op == SpatialOperation.IsDisjointTo)
			{
				return new DisjointSpatialFilter(this, args, GetFieldName());
			}
			Com.Spatial4j.Core.Shape.Shape shape = args.GetShape();
			int detailLevel = grid.GetLevelForDistance(args.ResolveDistErr(ctx, distErrPct));
			if (pointsOnly || op == SpatialOperation.Intersects)
			{
				return new IntersectsPrefixTreeFilter(shape, GetFieldName(), grid, detailLevel, prefixGridScanLevel
					, !pointsOnly);
			}
			else
			{
				if (op == SpatialOperation.IsWithin)
				{
					return new WithinPrefixTreeFilter(shape, GetFieldName(), grid, detailLevel, prefixGridScanLevel
						, -1);
				}
				else
				{
					//-1 flag is slower but ensures correct results
					if (op == SpatialOperation.Contains)
					{
						return new ContainsPrefixTreeFilter(shape, GetFieldName(), grid, detailLevel, multiOverlappingIndexedShapes
							);
					}
				}
			}
			throw new UnsupportedSpatialOperation(op);
		}
	}
}
