/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

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
	/// A Filter matching documents that have an
	/// <see cref="Com.Spatial4j.Core.Shape.SpatialRelation.INTERSECTS">Com.Spatial4j.Core.Shape.SpatialRelation.INTERSECTS
	/// 	</see>
	/// (i.e. not DISTINCT) relationship with a provided query shape.
	/// </summary>
	/// <lucene.internal></lucene.internal>
	public class IntersectsPrefixTreeFilter : AbstractVisitingPrefixTreeFilter
	{
		private readonly bool hasIndexedLeaves;

		public IntersectsPrefixTreeFilter(Com.Spatial4j.Core.Shape.Shape queryShape, string
			 fieldName, SpatialPrefixTree grid, int detailLevel, int prefixGridScanLevel, bool
			 hasIndexedLeaves) : base(queryShape, fieldName, grid, detailLevel, prefixGridScanLevel
			)
		{
			this.hasIndexedLeaves = hasIndexedLeaves;
		}

		public override bool Equals(object o)
		{
			return base.Equals(o) && hasIndexedLeaves == ((Lucene.Net.Spatial.Prefix.IntersectsPrefixTreeFilter
				)o).hasIndexedLeaves;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs
			)
		{
			return new _VisitorTemplate_55(this, context, acceptDocs, hasIndexedLeaves).GetDocIdSet
				();
		}

		private sealed class _VisitorTemplate_55 : AbstractVisitingPrefixTreeFilter.VisitorTemplate
		{
			public _VisitorTemplate_55(IntersectsPrefixTreeFilter _enclosing, AtomicReaderContext
				 baseArg1, Bits baseArg2, bool baseArg3) : base(_enclosing, baseArg1, baseArg2, 
				baseArg3)
			{
				this._enclosing = _enclosing;
			}

			private FixedBitSet results;

			protected internal override void Start()
			{
				this.results = new FixedBitSet(this.maxDoc);
			}

			protected internal override DocIdSet Finish()
			{
				return this.results;
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected internal override bool Visit(Cell cell)
			{
				if (cell.GetShapeRel() == SpatialRelation.WITHIN || cell.GetLevel() == this._enclosing
					.detailLevel)
				{
					this.CollectDocs(this.results);
					return false;
				}
				return true;
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected internal override void VisitLeaf(Cell cell)
			{
				this.CollectDocs(this.results);
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected internal override void VisitScanned(Cell cell)
			{
				if (this._enclosing.queryShape.Relate(cell.GetShape()).Intersects())
				{
					this.CollectDocs(this.results);
				}
			}

			private readonly IntersectsPrefixTreeFilter _enclosing;
		}
	}
}
