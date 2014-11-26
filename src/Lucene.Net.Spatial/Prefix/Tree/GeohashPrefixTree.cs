/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Com.Spatial4j.Core.Context;
using Com.Spatial4j.Core.IO;
using Com.Spatial4j.Core.Shape;
using Lucene.Net.Spatial.Prefix.Tree;
using Sharpen;

namespace Lucene.Net.Spatial.Prefix.Tree
{
	/// <summary>
	/// A
	/// <see cref="SpatialPrefixTree">SpatialPrefixTree</see>
	/// based on
	/// <a href="http://en.wikipedia.org/wiki/Geohash">Geohashes</a>.
	/// Uses
	/// <see cref="Com.Spatial4j.Core.IO.GeohashUtils">Com.Spatial4j.Core.IO.GeohashUtils
	/// 	</see>
	/// to do all the geohash work.
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class GeohashPrefixTree : SpatialPrefixTree
	{
		/// <summary>
		/// Factory for creating
		/// <see cref="GeohashPrefixTree">GeohashPrefixTree</see>
		/// instances with useful defaults
		/// </summary>
		public class Factory : SpatialPrefixTreeFactory
		{
			protected internal override int GetLevelForDistance(double degrees)
			{
				GeohashPrefixTree grid = new GeohashPrefixTree(ctx, GeohashPrefixTree.GetMaxLevelsPossible
					());
				return grid.GetLevelForDistance(degrees);
			}

			protected internal override SpatialPrefixTree NewSPT()
			{
				return new GeohashPrefixTree(ctx, maxLevels != null ? maxLevels : GeohashPrefixTree
					.GetMaxLevelsPossible());
			}
		}

		public GeohashPrefixTree(SpatialContext ctx, int maxLevels) : base(ctx, maxLevels
			)
		{
			Rectangle bounds = ctx.GetWorldBounds();
			if (bounds.GetMinX() != -180)
			{
				throw new ArgumentException("Geohash only supports lat-lon world bounds. Got " + 
					bounds);
			}
			int MAXP = GetMaxLevelsPossible();
			if (maxLevels <= 0 || maxLevels > MAXP)
			{
				throw new ArgumentException("maxLen must be [1-" + MAXP + "] but got " + maxLevels
					);
			}
		}

		/// <summary>Any more than this and there's no point (double lat & lon are the same).
		/// 	</summary>
		/// <remarks>Any more than this and there's no point (double lat & lon are the same).
		/// 	</remarks>
		public static int GetMaxLevelsPossible()
		{
			return GeohashUtils.MAX_PRECISION;
		}

		public override int GetLevelForDistance(double dist)
		{
			if (dist == 0)
			{
				return maxLevels;
			}
			//short circuit
			int level = GeohashUtils.LookupHashLenForWidthHeight(dist, dist);
			return Math.Max(Math.Min(level, maxLevels), 1);
		}

		protected internal override Cell GetCell(Point p, int level)
		{
			return new GeohashPrefixTree.GhCell(this, GeohashUtils.EncodeLatLon(p.GetY(), p.GetX
				(), level));
		}

		//args are lat,lon (y,x)
		public override Cell GetCell(string token)
		{
			return new GeohashPrefixTree.GhCell(this, token);
		}

		public override Cell GetCell(byte[] bytes, int offset, int len)
		{
			return new GeohashPrefixTree.GhCell(this, bytes, offset, len);
		}

		internal class GhCell : Cell
		{
			protected internal GhCell(GeohashPrefixTree _enclosing, string token) : base(token
				)
			{
				this._enclosing = _enclosing;
			}

			protected internal GhCell(GeohashPrefixTree _enclosing, byte[] bytes, int off, int
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
				string[] hashes = GeohashUtils.GetSubGeohashes(this.GetGeohash());
				//sorted
				IList<Cell> cells = new AList<Cell>(hashes.Length);
				foreach (string hash in hashes)
				{
					cells.AddItem(new GeohashPrefixTree.GhCell(this, hash));
				}
				return cells;
			}

			public override int GetSubCellsSize()
			{
				return 32;
			}

			//8x4
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
					this.shape = GeohashUtils.DecodeBoundary(this.GetGeohash(), this._enclosing.ctx);
				}
				return this.shape;
			}

			public override Point GetCenter()
			{
				return GeohashUtils.Decode(this.GetGeohash(), this._enclosing.ctx);
			}

			private string GetGeohash()
			{
				return this.GetTokenString();
			}

			private readonly GeohashPrefixTree _enclosing;
			//class GhCell
		}
	}
}
