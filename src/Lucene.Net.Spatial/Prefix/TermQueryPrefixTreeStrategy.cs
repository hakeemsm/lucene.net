/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Queries;
using Lucene.Net.Search;
using Lucene.Net.Spatial.Prefix;
using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Spatial.Query;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Spatial.Prefix
{
	/// <summary>
	/// A basic implementation of
	/// <see cref="PrefixTreeStrategy">PrefixTreeStrategy</see>
	/// using a large
	/// <see cref="Lucene.Net.Queries.TermsFilter">Lucene.Net.Queries.TermsFilter
	/// 	</see>
	/// of all the cells from
	/// <see cref="Lucene.Net.Spatial.Prefix.Tree.SpatialPrefixTree.GetCells(Com.Spatial4j.Core.Shape.Shape, int, bool, bool)
	/// 	">Lucene.Net.Spatial.Prefix.Tree.SpatialPrefixTree.GetCells(Com.Spatial4j.Core.Shape.Shape, int, bool, bool)
	/// 	</see>
	/// . It only supports the search of indexed Point shapes.
	/// <p/>
	/// The precision of query shapes (distErrPct) is an important factor in using
	/// this Strategy. If the precision is too precise then it will result in many
	/// terms which will amount to a slower query.
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class TermQueryPrefixTreeStrategy : PrefixTreeStrategy
	{
		public TermQueryPrefixTreeStrategy(SpatialPrefixTree grid, string fieldName) : base
			(grid, fieldName, false)
		{
		}

		//do not simplify indexed cells
		public override Filter MakeFilter(SpatialArgs args)
		{
			SpatialOperation op = args.GetOperation();
			if (op != SpatialOperation.Intersects)
			{
				throw new UnsupportedSpatialOperation(op);
			}
			Com.Spatial4j.Core.Shape.Shape shape = args.GetShape();
			int detailLevel = grid.GetLevelForDistance(args.ResolveDistErr(ctx, distErrPct));
			IList<Cell> cells = grid.GetCells(shape, detailLevel, false, true);
			//no parents
			//simplify
			BytesRef[] terms = new BytesRef[cells.Count];
			int i = 0;
			foreach (Cell cell in cells)
			{
				terms[i++] = new BytesRef(cell.GetTokenString());
			}
			//TODO use cell.getTokenBytes()
			return new TermsFilter(GetFieldName(), terms);
		}
	}
}
