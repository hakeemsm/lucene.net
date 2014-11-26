/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.Text;
using Com.Spatial4j.Core.Context;
using Com.Spatial4j.Core.Shape;
using Lucene.Net.Spatial.Prefix.Tree;
using Sharpen;

namespace Lucene.Net.Spatial.Prefix.Tree
{
	/// <summary>
	/// A spatial Prefix Tree, or Trie, which decomposes shapes into prefixed strings
	/// at variable lengths corresponding to variable precision.
	/// </summary>
	/// <remarks>
	/// A spatial Prefix Tree, or Trie, which decomposes shapes into prefixed strings
	/// at variable lengths corresponding to variable precision.   Each string
	/// corresponds to a rectangular spatial region.  This approach is
	/// also referred to "Grids", "Tiles", and "Spatial Tiers".
	/// <p/>
	/// Implementations of this class should be thread-safe and immutable once
	/// initialized.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public abstract class SpatialPrefixTree
	{
		protected internal static readonly Encoding UTF8 = StandardCharsets.UTF_8;

		protected internal readonly int maxLevels;

		protected internal readonly SpatialContext ctx;

		public SpatialPrefixTree(SpatialContext ctx, int maxLevels)
		{
			//HM:revisit
			//assert maxLevels > 0;
			this.ctx = ctx;
			this.maxLevels = maxLevels;
		}

		public virtual SpatialContext GetSpatialContext()
		{
			return ctx;
		}

		public virtual int GetMaxLevels()
		{
			return maxLevels;
		}

		public override string ToString()
		{
			return GetType().Name + "(maxLevels:" + maxLevels + ",ctx:" + ctx + ")";
		}

		/// <summary>
		/// Returns the level of the largest grid in which its longest side is less
		/// than or equal to the provided distance (in degrees).
		/// </summary>
		/// <remarks>
		/// Returns the level of the largest grid in which its longest side is less
		/// than or equal to the provided distance (in degrees). Consequently
		/// <code>dist</code>
		/// acts as an error epsilon declaring the amount of detail needed in the
		/// grid, such that you can get a grid with just the right amount of
		/// precision.
		/// </remarks>
		/// <param name="dist">&gt;= 0</param>
		/// <returns>level [1 to maxLevels]</returns>
		public abstract int GetLevelForDistance(double dist);

		/// <summary>
		/// Given a cell having the specified level, returns the distance from opposite
		/// corners.
		/// </summary>
		/// <remarks>
		/// Given a cell having the specified level, returns the distance from opposite
		/// corners. Since this might very depending on where the cell is, this method
		/// may over-estimate.
		/// </remarks>
		/// <param name="level">[1 to maxLevels]</param>
		/// <returns>&gt; 0</returns>
		public virtual double GetDistanceForLevel(int level)
		{
			if (level < 1 || level > GetMaxLevels())
			{
				throw new ArgumentException("Level must be in 1 to maxLevels range");
			}
			//TODO cache for each level
			Cell cell = GetCell(ctx.GetWorldBounds().GetCenter(), level);
			Rectangle bbox = cell.GetShape().GetBoundingBox();
			double width = bbox.GetWidth();
			double height = bbox.GetHeight();
			//Use standard cartesian hypotenuse. For geospatial, this answer is larger
			// than the correct one but it's okay to over-estimate.
			return Math.Sqrt(width * width + height * height);
		}

		[System.NonSerialized]
		private Cell worldCell;

		//cached
		/// <summary>Returns the level 0 cell which encompasses all spatial data.</summary>
		/// <remarks>
		/// Returns the level 0 cell which encompasses all spatial data. Equivalent to
		/// <see cref="GetCell(string)">GetCell(string)</see>
		/// with "".
		/// This cell is threadsafe, just like a spatial prefix grid is, although cells aren't
		/// generally threadsafe.
		/// TODO rename to getTopCell or is this fine?
		/// </remarks>
		public virtual Cell GetWorldCell()
		{
			if (worldCell == null)
			{
				worldCell = GetCell(string.Empty);
			}
			return worldCell;
		}

		/// <summary>The cell for the specified token.</summary>
		/// <remarks>
		/// The cell for the specified token. The empty string should be equal to
		/// <see cref="GetWorldCell()">GetWorldCell()</see>
		/// .
		/// Precondition: Never called when token length &gt; maxLevel.
		/// </remarks>
		public abstract Cell GetCell(string token);

		public abstract Cell GetCell(byte[] bytes, int offset, int len);

		public Cell GetCell(byte[] bytes, int offset, int len, Cell target)
		{
			if (target == null)
			{
				return GetCell(bytes, offset, len);
			}
			target.Reset(bytes, offset, len);
			return target;
		}

		/// <summary>
		/// Returns the cell containing point
		/// <code>p</code>
		/// at the specified
		/// <code>level</code>
		/// .
		/// </summary>
		protected internal virtual Cell GetCell(Point p, int level)
		{
			return GetCells(p, level, false)[0];
		}

		/// <summary>
		/// Gets the intersecting cells for the specified shape, without exceeding
		/// detail level.
		/// </summary>
		/// <remarks>
		/// Gets the intersecting cells for the specified shape, without exceeding
		/// detail level. If a cell is within the query shape then it's marked as a
		/// leaf and none of its children are added.
		/// <p/>
		/// This implementation checks if shape is a Point and if so returns
		/// <see cref="GetCells(Com.Spatial4j.Core.Shape.Point, int, bool)">GetCells(Com.Spatial4j.Core.Shape.Point, int, bool)
		/// 	</see>
		/// .
		/// </remarks>
		/// <param name="shape">the shape; non-null</param>
		/// <param name="detailLevel">the maximum detail level to get cells for</param>
		/// <param name="inclParents">
		/// if true then all parent cells of leaves are returned
		/// too. The top world cell is never returned.
		/// </param>
		/// <param name="simplify">
		/// for non-point shapes, this will simply/aggregate sets of
		/// complete leaves in a cell to its parent, resulting in
		/// ~20-25% fewer cells.
		/// </param>
		/// <returns>a set of cells (no dups), sorted, immutable, non-null</returns>
		public virtual IList<Cell> GetCells(Com.Spatial4j.Core.Shape.Shape shape, int detailLevel
			, bool inclParents, bool simplify)
		{
			//TODO consider an on-demand iterator -- it won't build up all cells in memory.
			if (detailLevel > maxLevels)
			{
				throw new ArgumentException("detailLevel > maxLevels");
			}
			if (shape is Point)
			{
				return GetCells((Point)shape, detailLevel, inclParents);
			}
			IList<Cell> cells = new AList<Cell>(inclParents ? 4096 : 2048);
			RecursiveGetCells(GetWorldCell(), shape, detailLevel, inclParents, simplify, cells
				);
			return cells;
		}

		/// <summary>Returns true if cell was added as a leaf.</summary>
		/// <remarks>
		/// Returns true if cell was added as a leaf. If it wasn't it recursively
		/// descends.
		/// </remarks>
		private bool RecursiveGetCells(Cell cell, Com.Spatial4j.Core.Shape.Shape shape, int
			 detailLevel, bool inclParents, bool simplify, IList<Cell> result)
		{
			if (cell.GetLevel() == detailLevel)
			{
				cell.SetLeaf();
			}
			//FYI might already be a leaf
			if (cell.IsLeaf())
			{
				result.AddItem(cell);
				return true;
			}
			if (inclParents && cell.GetLevel() != 0)
			{
				result.AddItem(cell);
			}
			ICollection<Cell> subCells = cell.GetSubCells(shape);
			int leaves = 0;
			foreach (Cell subCell in subCells)
			{
				if (RecursiveGetCells(subCell, shape, detailLevel, inclParents, simplify, result))
				{
					leaves++;
				}
			}
			//can we simplify?
			if (simplify && leaves == cell.GetSubCellsSize() && cell.GetLevel() != 0)
			{
				do
				{
					//Optimization: substitute the parent as a leaf instead of adding all
					// children as leaves
					//remove the leaves
					result.Remove(result.Count - 1);
				}
				while (--leaves > 0);
				//remove last
				//add cell as the leaf
				cell.SetLeaf();
				if (!inclParents)
				{
					// otherwise it was already added up above
					result.AddItem(cell);
				}
				return true;
			}
			return false;
		}

		/// <summary>
		/// A Point-optimized implementation of
		/// <see cref="GetCells(Com.Spatial4j.Core.Shape.Shape, int, bool, bool)">GetCells(Com.Spatial4j.Core.Shape.Shape, int, bool, bool)
		/// 	</see>
		/// . That
		/// method in facts calls this for points.
		/// <p/>
		/// This implementation depends on
		/// <see cref="GetCell(string)">GetCell(string)</see>
		/// being fast, as its
		/// called repeatedly when incPlarents is true.
		/// </summary>
		public virtual IList<Cell> GetCells(Point p, int detailLevel, bool inclParents)
		{
			Cell cell = GetCell(p, detailLevel);
			if (!inclParents)
			{
				return Sharpen.Collections.SingletonList(cell);
			}
			string endToken = cell.GetTokenString();
			IList<Cell> cells = new AList<Cell>(endToken.Length == detailLevel);
			for (int i = 1; i < detailLevel; i++)
			{
				cells.AddItem(GetCell(Sharpen.Runtime.Substring(endToken, 0, i)));
			}
			//TODO refactor: add a cell.getParent()
			cells.AddItem(cell);
			return cells;
		}

		/// <summary>Will add the trailing leaf byte for leaves.</summary>
		/// <remarks>Will add the trailing leaf byte for leaves. This isn't particularly efficient.
		/// 	</remarks>
		[System.ObsoleteAttribute(@"TODO remove; not used and not interesting, don't need collection in & out"
			)]
		public static IList<string> CellsToTokenStrings(ICollection<Cell> cells)
		{
			IList<string> tokens = new AList<string>((cells.Count));
			foreach (Cell cell in cells)
			{
				string token = cell.GetTokenString();
				if (cell.IsLeaf())
				{
					tokens.AddItem(token + (char)Cell.LEAF_BYTE);
				}
				else
				{
					tokens.AddItem(token);
				}
			}
			return tokens;
		}
	}
}
