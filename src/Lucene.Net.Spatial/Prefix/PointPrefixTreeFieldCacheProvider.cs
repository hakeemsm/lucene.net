/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Com.Spatial4j.Core.Shape;
using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Spatial.Util;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Spatial.Prefix
{
	/// <summary>
	/// Implementation of
	/// <see cref="Lucene.Net.Spatial.Util.ShapeFieldCacheProvider{T}">Lucene.Net.Spatial.Util.ShapeFieldCacheProvider&lt;T&gt;
	/// 	</see>
	/// designed for
	/// <see cref="PrefixTreeStrategy">PrefixTreeStrategy</see>
	/// s.
	/// Note, due to the fragmented representation of Shapes in these Strategies, this implementation
	/// can only retrieve the central
	/// <see cref="Com.Spatial4j.Core.Shape.Point">Com.Spatial4j.Core.Shape.Point</see>
	/// of the original Shapes.
	/// </summary>
	/// <lucene.internal></lucene.internal>
	public class PointPrefixTreeFieldCacheProvider : ShapeFieldCacheProvider<Point>
	{
		internal readonly SpatialPrefixTree grid;

		public PointPrefixTreeFieldCacheProvider(SpatialPrefixTree grid, string shapeField
			, int defaultSize) : base(shapeField, defaultSize)
		{
			//
			this.grid = grid;
		}

		private Cell scanCell = null;

		//re-used in readShape to save GC
		protected internal override Point ReadShape(BytesRef term)
		{
			scanCell = grid.GetCell(term.bytes, term.offset, term.length, scanCell);
			if (scanCell.GetLevel() == grid.GetMaxLevels() && !scanCell.IsLeaf())
			{
				return scanCell.GetCenter();
			}
			return null;
		}
	}
}
