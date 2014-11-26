/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Com.Spatial4j.Core.Shape;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Spatial;
using Lucene.Net.Spatial.Prefix;
using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Spatial.Query;
using Lucene.Net.Spatial.Util;
using Sharpen;

namespace Lucene.Net.Spatial.Prefix
{
	/// <summary>
	/// An abstract SpatialStrategy based on
	/// <see cref="Lucene.Net.Spatial.Prefix.Tree.SpatialPrefixTree">Lucene.Net.Spatial.Prefix.Tree.SpatialPrefixTree
	/// 	</see>
	/// . The two
	/// subclasses are
	/// <see cref="RecursivePrefixTreeStrategy">RecursivePrefixTreeStrategy</see>
	/// and
	/// <see cref="TermQueryPrefixTreeStrategy">TermQueryPrefixTreeStrategy</see>
	/// .  This strategy is most effective as a fast
	/// approximate spatial search filter.
	/// <h4>Characteristics:</h4>
	/// <ul>
	/// <li>Can index any shape; however only
	/// <see cref="RecursivePrefixTreeStrategy">RecursivePrefixTreeStrategy</see>
	/// can effectively search non-point shapes.</li>
	/// <li>Can index a variable number of shapes per field value. This strategy
	/// can do it via multiple calls to
	/// <see cref="CreateIndexableFields(Com.Spatial4j.Core.Shape.Shape)">CreateIndexableFields(Com.Spatial4j.Core.Shape.Shape)
	/// 	</see>
	/// for a document or by giving it some sort of Shape aggregate (e.g. JTS
	/// WKT MultiPoint).  The shape's boundary is approximated to a grid precision.
	/// </li>
	/// <li>Can query with any shape.  The shape's boundary is approximated to a grid
	/// precision.</li>
	/// <li>Only
	/// <see cref="Lucene.Net.Spatial.Query.SpatialOperation.Intersects">Lucene.Net.Spatial.Query.SpatialOperation.Intersects
	/// 	</see>
	/// is supported.  If only points are indexed then this is effectively equivalent
	/// to IsWithin.</li>
	/// <li>The strategy supports
	/// <see cref="MakeDistanceValueSource(Com.Spatial4j.Core.Shape.Point, double)">MakeDistanceValueSource(Com.Spatial4j.Core.Shape.Point, double)
	/// 	</see>
	/// even for multi-valued data, so long as the indexed data is all points; the
	/// behavior is undefined otherwise.  However, <em>it will likely be removed in
	/// the future</em> in lieu of using another strategy with a more scalable
	/// implementation.  Use of this call is the only
	/// circumstance in which a cache is used.  The cache is simple but as such
	/// it doesn't scale to large numbers of points nor is it real-time-search
	/// friendly.</li>
	/// </ul>
	/// <h4>Implementation:</h4>
	/// The
	/// <see cref="Lucene.Net.Spatial.Prefix.Tree.SpatialPrefixTree">Lucene.Net.Spatial.Prefix.Tree.SpatialPrefixTree
	/// 	</see>
	/// does most of the work, for example returning
	/// a list of terms representing grids of various sizes for a supplied shape.
	/// An important
	/// configuration item is
	/// <see cref="SetDistErrPct(double)">SetDistErrPct(double)</see>
	/// which balances
	/// shape precision against scalability.  See those javadocs.
	/// </summary>
	/// <lucene.internal></lucene.internal>
	public abstract class PrefixTreeStrategy : SpatialStrategy
	{
		protected internal readonly SpatialPrefixTree grid;

		private readonly IDictionary<string, PointPrefixTreeFieldCacheProvider> provider = 
			new ConcurrentHashMap<string, PointPrefixTreeFieldCacheProvider>();

		protected internal readonly bool simplifyIndexedCells;

		protected internal int defaultFieldValuesArrayLen = 2;

		protected internal double distErrPct = SpatialArgs.DEFAULT_DISTERRPCT;

		public PrefixTreeStrategy(SpatialPrefixTree grid, string fieldName, bool simplifyIndexedCells
			) : base(grid.GetSpatialContext(), fieldName)
		{
			// [ 0 TO 0.5 ]
			this.grid = grid;
			this.simplifyIndexedCells = simplifyIndexedCells;
		}

		/// <summary>
		/// A memory hint used by
		/// <see cref="Lucene.Net.Spatial.SpatialStrategy.MakeDistanceValueSource(Com.Spatial4j.Core.Shape.Point)
		/// 	">Lucene.Net.Spatial.SpatialStrategy.MakeDistanceValueSource(Com.Spatial4j.Core.Shape.Point)
		/// 	</see>
		/// for how big the initial size of each Document's array should be. The
		/// default is 2.  Set this to slightly more than the default expected number
		/// of points per document.
		/// </summary>
		public virtual void SetDefaultFieldValuesArrayLen(int defaultFieldValuesArrayLen)
		{
			this.defaultFieldValuesArrayLen = defaultFieldValuesArrayLen;
		}

		public virtual double GetDistErrPct()
		{
			return distErrPct;
		}

		/// <summary>
		/// The default measure of shape precision affecting shapes at index and query
		/// times.
		/// </summary>
		/// <remarks>
		/// The default measure of shape precision affecting shapes at index and query
		/// times. Points don't use this as they are always indexed at the configured
		/// maximum precision (
		/// <see cref="Lucene.Net.Spatial.Prefix.Tree.SpatialPrefixTree.GetMaxLevels()
		/// 	">Lucene.Net.Spatial.Prefix.Tree.SpatialPrefixTree.GetMaxLevels()</see>
		/// );
		/// this applies to all other shapes. Specific shapes at index and query time
		/// can use something different than this default value.  If you don't set a
		/// default then the default is
		/// <see cref="Lucene.Net.Spatial.Query.SpatialArgs.DEFAULT_DISTERRPCT">Lucene.Net.Spatial.Query.SpatialArgs.DEFAULT_DISTERRPCT
		/// 	</see>
		/// --
		/// 2.5%.
		/// </remarks>
		/// <seealso cref="Lucene.Net.Spatial.Query.SpatialArgs.GetDistErrPct()">Lucene.Net.Spatial.Query.SpatialArgs.GetDistErrPct()
		/// 	</seealso>
		public virtual void SetDistErrPct(double distErrPct)
		{
			this.distErrPct = distErrPct;
		}

		public override Field[] CreateIndexableFields(Com.Spatial4j.Core.Shape.Shape shape
			)
		{
			double distErr = SpatialArgs.CalcDistanceFromErrPct(shape, distErrPct, ctx);
			return CreateIndexableFields(shape, distErr);
		}

		public virtual Field[] CreateIndexableFields(Com.Spatial4j.Core.Shape.Shape shape
			, double distErr)
		{
			int detailLevel = grid.GetLevelForDistance(distErr);
			IList<Cell> cells = grid.GetCells(shape, detailLevel, true, simplifyIndexedCells);
			//intermediates cells
			//TODO is CellTokenStream supposed to be re-used somehow? see Uwe's comments:
			//  http://code.google.com/p/lucene-spatial-playground/issues/detail?id=4
			Field field = new Field(GetFieldName(), new PrefixTreeStrategy.CellTokenStream(cells
				.Iterator()), FIELD_TYPE);
			return new Field[] { field };
		}

		public static readonly FieldType FIELD_TYPE = new FieldType();

		static PrefixTreeStrategy()
		{
			FIELD_TYPE.SetIndexed(true);
			FIELD_TYPE.SetTokenized(true);
			FIELD_TYPE.SetOmitNorms(true);
			FIELD_TYPE.SetIndexOptions(FieldInfo.IndexOptions.DOCS_ONLY);
			FIELD_TYPE.Freeze();
		}

		/// <summary>Outputs the tokenString of a cell, and if its a leaf, outputs it again with the leaf byte.
		/// 	</summary>
		/// <remarks>Outputs the tokenString of a cell, and if its a leaf, outputs it again with the leaf byte.
		/// 	</remarks>
		internal sealed class CellTokenStream : TokenStream
		{
			private readonly CharTermAttribute termAtt = AddAttribute<CharTermAttribute>();

			private Iterator<Cell> iter = null;

			public CellTokenStream(Iterator<Cell> tokens)
			{
				this.iter = tokens;
			}

			internal CharSequence nextTokenStringNeedingLeaf = null;

			public override bool IncrementToken()
			{
				ClearAttributes();
				if (nextTokenStringNeedingLeaf != null)
				{
					termAtt.Append(nextTokenStringNeedingLeaf);
					termAtt.Append((char)Cell.LEAF_BYTE);
					nextTokenStringNeedingLeaf = null;
					return true;
				}
				if (iter.HasNext())
				{
					Cell cell = iter.Next();
					CharSequence token = cell.GetTokenString();
					termAtt.Append(token);
					if (cell.IsLeaf())
					{
						nextTokenStringNeedingLeaf = token;
					}
					return true;
				}
				return false;
			}
		}

		public override ValueSource MakeDistanceValueSource(Point queryPoint, double multiplier
			)
		{
			PointPrefixTreeFieldCacheProvider p = provider.Get(GetFieldName());
			if (p == null)
			{
				lock (this)
				{
					//double checked locking idiom is okay since provider is threadsafe
					p = provider.Get(GetFieldName());
					if (p == null)
					{
						p = new PointPrefixTreeFieldCacheProvider(grid, GetFieldName(), defaultFieldValuesArrayLen
							);
						provider.Put(GetFieldName(), p);
					}
				}
			}
			return new ShapeFieldCacheDistanceValueSource(ctx, p, queryPoint, multiplier);
		}

		public virtual SpatialPrefixTree GetGrid()
		{
			return grid;
		}
	}
}
