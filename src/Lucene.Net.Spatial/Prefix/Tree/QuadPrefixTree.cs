/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Com.Spatial4j.Core.Context;
using Com.Spatial4j.Core.Shape;
using Lucene.Net.Spatial.Prefix.Tree;
using Sharpen;

namespace Lucene.Net.Spatial.Prefix.Tree
{
	/// <summary>
	/// A
	/// <see cref="SpatialPrefixTree">SpatialPrefixTree</see>
	/// which uses a
	/// <a href="http://en.wikipedia.org/wiki/Quadtree">quad tree</a> in which an
	/// indexed term will be generated for each cell, 'A', 'B', 'C', 'D'.
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class QuadPrefixTree : SpatialPrefixTree
	{
		/// <summary>
		/// Factory for creating
		/// <see cref="QuadPrefixTree">QuadPrefixTree</see>
		/// instances with useful defaults
		/// </summary>
		public class Factory : SpatialPrefixTreeFactory
		{
			protected internal override int GetLevelForDistance(double degrees)
			{
				QuadPrefixTree grid = new QuadPrefixTree(ctx, MAX_LEVELS_POSSIBLE);
				return grid.GetLevelForDistance(degrees);
			}

			protected internal override SpatialPrefixTree NewSPT()
			{
				return new QuadPrefixTree(ctx, maxLevels != null ? maxLevels : MAX_LEVELS_POSSIBLE
					);
			}
		}

		public const int MAX_LEVELS_POSSIBLE = 50;

		public const int DEFAULT_MAX_LEVELS = 12;

		private readonly double xmin;

		private readonly double xmax;

		private readonly double ymin;

		private readonly double ymax;

		private readonly double xmid;

		private readonly double ymid;

		private readonly double gridW;

		public readonly double gridH;

		internal readonly double[] levelW;

		internal readonly double[] levelH;

		internal readonly int[] levelS;

		internal readonly int[] levelN;

		public QuadPrefixTree(SpatialContext ctx, Rectangle bounds, int maxLevels) : base
			(ctx, maxLevels)
		{
			//not really sure how big this should be
			// side
			// number
			this.xmin = bounds.GetMinX();
			this.xmax = bounds.GetMaxX();
			this.ymin = bounds.GetMinY();
			this.ymax = bounds.GetMaxY();
			levelW = new double[maxLevels];
			levelH = new double[maxLevels];
			levelS = new int[maxLevels];
			levelN = new int[maxLevels];
			gridW = xmax - xmin;
			gridH = ymax - ymin;
			this.xmid = xmin + gridW / 2.0;
			this.ymid = ymin + gridH / 2.0;
			levelW[0] = gridW / 2.0;
			levelH[0] = gridH / 2.0;
			levelS[0] = 2;
			levelN[0] = 4;
			for (int i = 1; i < levelW.Length; i++)
			{
				levelW[i] = levelW[i - 1] / 2.0;
				levelH[i] = levelH[i - 1] / 2.0;
				levelS[i] = levelS[i - 1] * 2;
				levelN[i] = levelN[i - 1] * 4;
			}
		}

		public QuadPrefixTree(SpatialContext ctx) : this(ctx, DEFAULT_MAX_LEVELS)
		{
		}

		public QuadPrefixTree(SpatialContext ctx, int maxLevels) : this(ctx, ctx.GetWorldBounds
			(), maxLevels)
		{
		}

		public virtual void PrintInfo(TextWriter @out)
		{
			NumberFormat nf = NumberFormat.GetNumberInstance(CultureInfo.ROOT);
			nf.SetMaximumFractionDigits(5);
			nf.SetMinimumFractionDigits(5);
			nf.SetMinimumIntegerDigits(3);
			for (int i = 0; i < maxLevels; i++)
			{
				@out.WriteLine(i + "]\t" + nf.Format(levelW[i]) + "\t" + nf.Format(levelH[i]) + "\t"
					 + levelS[i] + "\t" + (levelS[i] * levelS[i]));
			}
		}

		public override int GetLevelForDistance(double dist)
		{
			if (dist == 0)
			{
				//short circuit
				return maxLevels;
			}
			for (int i = 0; i < maxLevels - 1; i++)
			{
				//note: level[i] is actually a lookup for level i+1
				if (dist > levelW[i] && dist > levelH[i])
				{
					return i + 1;
				}
			}
			return maxLevels;
		}

		protected internal override Cell GetCell(Point p, int level)
		{
			IList<Cell> cells = new AList<Cell>(1);
			Build(xmid, ymid, 0, cells, new StringBuilder(), ctx.MakePoint(p.GetX(), p.GetY()
				), level);
			return cells[0];
		}

		//note cells could be longer if p on edge
		public override Cell GetCell(string token)
		{
			return new QuadPrefixTree.QuadCell(this, token);
		}

		public override Cell GetCell(byte[] bytes, int offset, int len)
		{
			return new QuadPrefixTree.QuadCell(this, bytes, offset, len);
		}

		private void Build(double x, double y, int level, IList<Cell> matches, StringBuilder
			 str, Com.Spatial4j.Core.Shape.Shape shape, int maxLevel)
		{
			double w = str.Length == level[level] / 2;
			double h = levelH[level] / 2;
			// Z-Order
			// http://en.wikipedia.org/wiki/Z-order_%28curve%29
			CheckBattenberg('A', x - w, y + h, level, matches, str, shape, maxLevel);
			CheckBattenberg('B', x + w, y + h, level, matches, str, shape, maxLevel);
			CheckBattenberg('C', x - w, y - h, level, matches, str, shape, maxLevel);
			CheckBattenberg('D', x + w, y - h, level, matches, str, shape, maxLevel);
		}

		// possibly consider hilbert curve
		// http://en.wikipedia.org/wiki/Hilbert_curve
		// http://blog.notdot.net/2009/11/Damn-Cool-Algorithms-Spatial-indexing-with-Quadtrees-and-Hilbert-Curves
		// if we actually use the range property in the query, this could be useful
		private void CheckBattenberg(char c, double cx, double cy, int level, IList<Cell>
			 matches, StringBuilder str, Com.Spatial4j.Core.Shape.Shape shape, int maxLevel)
		{
			double w = str.Length == level[level] / 2;
			double h = levelH[level] / 2;
			int strlen = str.Length;
			Rectangle rectangle = ctx.MakeRectangle(cx - w, cx + w, cy - h, cy + h);
			SpatialRelation v = shape.Relate(rectangle);
			if (SpatialRelation.CONTAINS == v)
			{
				str.Append(c);
				//str.append(SpatialPrefixGrid.COVER);
				matches.AddItem(new QuadPrefixTree.QuadCell(this, str.ToString(), v.Transpose()));
			}
			else
			{
				if (SpatialRelation.DISJOINT == v)
				{
				}
				else
				{
					// nothing
					// SpatialRelation.WITHIN, SpatialRelation.INTERSECTS
					str.Append(c);
					int nextLevel = level + 1;
					if (nextLevel >= maxLevel)
					{
						//str.append(SpatialPrefixGrid.INTERSECTS);
						matches.AddItem(new QuadPrefixTree.QuadCell(this, str.ToString(), v.Transpose()));
					}
					else
					{
						Build(cx, cy, nextLevel, matches, str, shape, maxLevel);
					}
				}
			}
			str.Length = strlen;
		}

		internal class QuadCell : Cell
		{
			protected internal QuadCell(QuadPrefixTree _enclosing, string token) : base(token
				)
			{
				this._enclosing = _enclosing;
			}

			public QuadCell(QuadPrefixTree _enclosing, string token, SpatialRelation shapeRel
				) : base(token)
			{
				this._enclosing = _enclosing;
				this.shapeRel = shapeRel;
			}

			protected internal QuadCell(QuadPrefixTree _enclosing, byte[] bytes, int off, int
				 len) : base(bytes, off, len)
			{
				this._enclosing = _enclosing;
			}

			public override void Reset(byte[] bytes, int off, int len)
			{
				base.Reset(bytes, off, len);
				this.shape = null;
			}

			protected internal override ICollection<Cell> GetSubCells()
			{
				IList<Cell> cells = new AList<Cell>(4);
				cells.AddItem(new QuadPrefixTree.QuadCell(this, this.GetTokenString() + "A"));
				cells.AddItem(new QuadPrefixTree.QuadCell(this, this.GetTokenString() + "B"));
				cells.AddItem(new QuadPrefixTree.QuadCell(this, this.GetTokenString() + "C"));
				cells.AddItem(new QuadPrefixTree.QuadCell(this, this.GetTokenString() + "D"));
				return cells;
			}

			public override int GetSubCellsSize()
			{
				return 4;
			}

			public override Cell GetSubCell(Point p)
			{
				return this._enclosing.GetCell(p, this.GetLevel() + 1);
			}

			private Com.Spatial4j.Core.Shape.Shape shape;

			//not performant!
			//cache
			public override Com.Spatial4j.Core.Shape.Shape GetShape()
			{
				if (this.shape == null)
				{
					this.shape = this.MakeShape();
				}
				return this.shape;
			}

			private Rectangle MakeShape()
			{
				string token = this.GetTokenString();
				double xmin = this._enclosing.xmin;
				double ymin = this._enclosing.ymin;
				for (int i = 0; i < token.Length; i++)
				{
					char c = token[i];
					if ('A' == c || 'a' == c)
					{
						ymin += this._enclosing.levelH[i];
					}
					else
					{
						if ('B' == c || 'b' == c)
						{
							xmin += this._enclosing.levelW[i];
							ymin += this._enclosing.levelH[i];
						}
						else
						{
							if ('C' == c || 'c' == c)
							{
							}
							else
							{
								// nothing really
								if ('D' == c || 'd' == c)
								{
									xmin += this._enclosing.levelW[i];
								}
								else
								{
									throw new RuntimeException("unexpected char: " + c);
								}
							}
						}
					}
				}
				int len = token.Length;
				double width;
				double height;
				if (len > 0)
				{
					width = this._enclosing.levelW[len - 1];
					height = this._enclosing.levelH[len - 1];
				}
				else
				{
					width = this._enclosing.gridW;
					height = this._enclosing.gridH;
				}
				return this._enclosing.ctx.MakeRectangle(xmin, xmin + width, ymin, ymin + height);
			}

			private readonly QuadPrefixTree _enclosing;
			//QuadCell
		}
	}
}
