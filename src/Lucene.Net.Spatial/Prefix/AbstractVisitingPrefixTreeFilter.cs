/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Spatial.Prefix;
using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Spatial.Prefix
{
	/// <summary>
	/// Traverses a
	/// <see cref="Lucene.Net.Spatial.Prefix.Tree.SpatialPrefixTree">Lucene.Net.Spatial.Prefix.Tree.SpatialPrefixTree
	/// 	</see>
	/// indexed field, using the template &
	/// visitor design patterns for subclasses to guide the traversal and collect
	/// matching documents.
	/// <p/>
	/// Subclasses implement
	/// <see cref="Lucene.Net.Search.Filter.GetDocIdSet(Lucene.Net.Index.AtomicReaderContext, Lucene.Net.Util.Bits)
	/// 	">Lucene.Net.Search.Filter.GetDocIdSet(Lucene.Net.Index.AtomicReaderContext, Lucene.Net.Util.Bits)
	/// 	</see>
	/// by instantiating a custom
	/// <see cref="VisitorTemplate">VisitorTemplate</see>
	/// subclass (i.e. an anonymous inner class) and implement the
	/// required methods.
	/// </summary>
	/// <lucene.internal></lucene.internal>
	public abstract class AbstractVisitingPrefixTreeFilter : AbstractPrefixTreeFilter
	{
		protected internal readonly int prefixGridScanLevel;

		public AbstractVisitingPrefixTreeFilter(Com.Spatial4j.Core.Shape.Shape queryShape
			, string fieldName, SpatialPrefixTree grid, int detailLevel, int prefixGridScanLevel
			) : base(queryShape, fieldName, grid, detailLevel)
		{
			//Historical note: this code resulted from a refactoring of RecursivePrefixTreeFilter,
			// which in turn came out of SOLR-2155
			//at least one less than grid.getMaxLevels()
			this.prefixGridScanLevel = Math.Max(0, Math.Min(prefixGridScanLevel, grid.GetMaxLevels
				() - 1));
		}

		//HM:revisit
		//assert detailLevel <= grid.getMaxLevels();
		public override bool Equals(object o)
		{
			if (!base.Equals(o))
			{
				return false;
			}
			//checks getClass == o.getClass & instanceof
			//Ignore prefixGridScanLevel as it is merely a tuning parameter.
			return true;
		}

		public override int GetHashCode()
		{
			int result = base.GetHashCode();
			return result;
		}

		/// <summary>
		/// An abstract class designed to make it easy to implement predicates or
		/// other operations on a
		/// <see cref="Lucene.Net.Spatial.Prefix.Tree.SpatialPrefixTree">Lucene.Net.Spatial.Prefix.Tree.SpatialPrefixTree
		/// 	</see>
		/// indexed field. An instance
		/// of this class is not designed to be re-used across AtomicReaderContext
		/// instances so simply create a new one for each call to, say a
		/// <see cref="Lucene.Net.Search.Filter.GetDocIdSet(Lucene.Net.Index.AtomicReaderContext, Lucene.Net.Util.Bits)
		/// 	">Lucene.Net.Search.Filter.GetDocIdSet(Lucene.Net.Index.AtomicReaderContext, Lucene.Net.Util.Bits)
		/// 	</see>
		/// .
		/// The
		/// <see cref="GetDocIdSet()">GetDocIdSet()</see>
		/// method here starts the work. It first checks
		/// that there are indexed terms; if not it quickly returns null. Then it calls
		/// <see cref="Start()">Start()</see>
		/// so a subclass can set up a return value, like an
		/// <see cref="Lucene.Net.Util.FixedBitSet">Lucene.Net.Util.FixedBitSet
		/// 	</see>
		/// . Then it starts the traversal
		/// process, calling
		/// <see cref="FindSubCellsToVisit(Lucene.Net.Spatial.Prefix.Tree.Cell)">FindSubCellsToVisit(Lucene.Net.Spatial.Prefix.Tree.Cell)
		/// 	</see>
		/// which by default finds the top cells that intersect
		/// <code>queryShape</code>
		/// . If
		/// there isn't an indexed cell for a corresponding cell returned for this
		/// method then it's short-circuited until it finds one, at which point
		/// <see cref="Visit(Lucene.Net.Spatial.Prefix.Tree.Cell)">Visit(Lucene.Net.Spatial.Prefix.Tree.Cell)
		/// 	</see>
		/// is called. At
		/// some depths, of the tree, the algorithm switches to a scanning mode that
		/// calls
		/// <see cref="VisitScanned(Lucene.Net.Spatial.Prefix.Tree.Cell)">VisitScanned(Lucene.Net.Spatial.Prefix.Tree.Cell)
		/// 	</see>
		/// for each leaf cell found.
		/// </summary>
		/// <lucene.internal></lucene.internal>
		public abstract class VisitorTemplate : AbstractPrefixTreeFilter.BaseTermsEnumTraverser
		{
			protected internal readonly bool hasIndexedLeaves;

			private AbstractVisitingPrefixTreeFilter.VNode curVNode;

			private BytesRef curVNodeTerm = new BytesRef();

			private Cell scanCell;

			private BytesRef thisTerm;

			/// <exception cref="System.IO.IOException"></exception>
			public VisitorTemplate(AbstractVisitingPrefixTreeFilter _enclosing, AtomicReaderContext
				 context, Bits acceptDocs, bool hasIndexedLeaves) : base(_enclosing)
			{
				this._enclosing = _enclosing;
				//if false then we can skip looking for them
				//current pointer, derived from query shape
				//curVNode.cell's term.
				//the result of termsEnum.term()
				this.hasIndexedLeaves = hasIndexedLeaves;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public virtual DocIdSet GetDocIdSet()
			{
				//HM:revisit
				//assert curVNode == null : "Called more than once?";
				if (this.termsEnum == null)
				{
					return null;
				}
				//advance
				if ((this.thisTerm = this.termsEnum.Next()) == null)
				{
					return null;
				}
				// all done
				this.curVNode = new AbstractVisitingPrefixTreeFilter.VNode(null);
				this.curVNode.Reset(this._enclosing.grid.GetWorldCell());
				this.Start();
				this.AddIntersectingChildren();
				while (this.thisTerm != null)
				{
					//terminates for other reasons too!
					//Advance curVNode pointer
					if (this.curVNode.children != null)
					{
						//-- HAVE CHILDREN: DESCEND
						//HM:revisit
						//assert curVNode.children.hasNext();//if we put it there then it has something
						this.PreSiblings(this.curVNode);
						this.curVNode = this.curVNode.children.Next();
					}
					else
					{
						//-- NO CHILDREN: ADVANCE TO NEXT SIBLING
						AbstractVisitingPrefixTreeFilter.VNode parentVNode = this.curVNode.parent;
						while (true)
						{
							if (parentVNode == null)
							{
								goto main_break;
							}
							// all done
							if (parentVNode.children.HasNext())
							{
								//advance next sibling
								this.curVNode = parentVNode.children.Next();
								break;
							}
							else
							{
								//reached end of siblings; pop up
								this.PostSiblings(parentVNode);
								parentVNode.children = null;
								//GC
								parentVNode = parentVNode.parent;
							}
						}
					}
					//Seek to curVNode's cell (or skip if termsEnum has moved beyond)
					this.curVNodeTerm.bytes = this.curVNode.cell.GetTokenBytes();
					this.curVNodeTerm.length = this.curVNodeTerm.bytes.Length;
					int compare = this.termsEnum.GetComparator().Compare(this.thisTerm, this.curVNodeTerm
						);
					if (compare > 0)
					{
					}
					else
					{
						// leap frog (termsEnum is beyond where we would otherwise seek)
						//HM:revisit
						//assert ! context.reader().terms(fieldName).iterator(null).seekExact(curVNodeTerm) : "should be absent";
						if (compare < 0)
						{
							// Seek !
							TermsEnum.SeekStatus seekStatus = this.termsEnum.SeekCeil(this.curVNodeTerm);
							if (seekStatus == TermsEnum.SeekStatus.END)
							{
								break;
							}
							// all done
							this.thisTerm = this.termsEnum.Term();
							if (seekStatus == TermsEnum.SeekStatus.NOT_FOUND)
							{
								continue;
							}
						}
						// leap frog
						// Visit!
						bool descend = this.Visit(this.curVNode.cell);
						//advance
						if ((this.thisTerm = this.termsEnum.Next()) == null)
						{
							break;
						}
						// all done
						if (descend)
						{
							this.AddIntersectingChildren();
						}
					}
main_continue: ;
				}
main_break: ;
				//main loop
				return this.Finish();
			}

			/// <summary>
			/// Called initially, and whenever
			/// <see cref="Visit(Lucene.Net.Spatial.Prefix.Tree.Cell)">Visit(Lucene.Net.Spatial.Prefix.Tree.Cell)
			/// 	</see>
			/// returns true.
			/// </summary>
			/// <exception cref="System.IO.IOException"></exception>
			private void AddIntersectingChildren()
			{
				//HM:revisit
				//assert thisTerm != null;
				Cell cell = this.curVNode.cell;
				if (cell.GetLevel() >= this._enclosing.detailLevel)
				{
					throw new InvalidOperationException("Spatial logic error");
				}
				//Check for adjacent leaf (happens for indexed non-point shapes)
				if (this.hasIndexedLeaves && cell.GetLevel() != 0)
				{
					//If the next indexed term just adds a leaf marker ('+') to cell,
					// then add all of those docs
					//HM:revisit
					//assert StringHelper.startsWith(thisTerm, curVNodeTerm);//TODO refactor to use method on curVNode.cell
					this.scanCell = this._enclosing.grid.GetCell(this.thisTerm.bytes, this.thisTerm.offset
						, this.thisTerm.length, this.scanCell);
					if (this.scanCell.GetLevel() == cell.GetLevel() && this.scanCell.IsLeaf())
					{
						this.VisitLeaf(this.scanCell);
						//advance
						if ((this.thisTerm = this.termsEnum.Next()) == null)
						{
							return;
						}
					}
				}
				// all done
				//Decide whether to continue to divide & conquer, or whether it's time to
				// scan through terms beneath this cell.
				// Scanning is a performance optimization trade-off.
				//TODO use termsEnum.docFreq() as heuristic
				bool scan = cell.GetLevel() >= this._enclosing.prefixGridScanLevel;
				//simple heuristic
				if (!scan)
				{
					//Divide & conquer (ultimately termsEnum.seek())
					Iterator<Cell> subCellsIter = this.FindSubCellsToVisit(cell);
					if (!subCellsIter.HasNext())
					{
						//not expected
						return;
					}
					this.curVNode.children = new AbstractVisitingPrefixTreeFilter.VisitorTemplate.VNodeCellIterator
						(this, subCellsIter, new AbstractVisitingPrefixTreeFilter.VNode(this.curVNode));
				}
				else
				{
					//Scan (loop of termsEnum.next())
					this.Scan(this._enclosing.detailLevel);
				}
			}

			/// <summary>
			/// Called when doing a divide & conquer to find the next intersecting cells
			/// of the query shape that are beneath
			/// <code>cell</code>
			/// .
			/// <code>cell</code>
			/// is
			/// guaranteed to have an intersection and thus this must return some number
			/// of nodes.
			/// </summary>
			protected internal virtual Iterator<Cell> FindSubCellsToVisit(Cell cell)
			{
				return cell.GetSubCells(this._enclosing.queryShape).Iterator();
			}

			/// <summary>
			/// Scans (
			/// <code>termsEnum.next()</code>
			/// ) terms until a term is found that does
			/// not start with curVNode's cell. If it finds a leaf cell or a cell at
			/// level
			/// <code>scanDetailLevel</code>
			/// then it calls
			/// <see cref="VisitScanned(Lucene.Net.Spatial.Prefix.Tree.Cell)">VisitScanned(Lucene.Net.Spatial.Prefix.Tree.Cell)
			/// 	</see>
			/// .
			/// </summary>
			/// <exception cref="System.IO.IOException"></exception>
			protected internal virtual void Scan(int scanDetailLevel)
			{
				for (; this.thisTerm != null && StringHelper.StartsWith(this.thisTerm, this.curVNodeTerm
					); this.thisTerm = this.termsEnum.Next())
				{
					//TODO refactor to use method on curVNode.cell
					this.scanCell = this._enclosing.grid.GetCell(this.thisTerm.bytes, this.thisTerm.offset
						, this.thisTerm.length, this.scanCell);
					int termLevel = this.scanCell.GetLevel();
					if (termLevel < scanDetailLevel)
					{
						if (this.scanCell.IsLeaf())
						{
							this.VisitScanned(this.scanCell);
						}
					}
					else
					{
						if (termLevel == scanDetailLevel)
						{
							if (!this.scanCell.IsLeaf())
							{
								//LUCENE-5529
								this.VisitScanned(this.scanCell);
							}
						}
					}
				}
			}

			/// <summary>
			/// Used for
			/// <see cref="VNode.children">VNode.children</see>
			/// .
			/// </summary>
			private class VNodeCellIterator : Iterator<AbstractVisitingPrefixTreeFilter.VNode
				>
			{
				internal readonly Iterator<Cell> cellIter;

				private readonly AbstractVisitingPrefixTreeFilter.VNode vNode;

				internal VNodeCellIterator(VisitorTemplate _enclosing, Iterator<Cell> cellIter, AbstractVisitingPrefixTreeFilter.VNode
					 vNode)
				{
					this._enclosing = _enclosing;
					//term loop
					this.cellIter = cellIter;
					this.vNode = vNode;
				}

				public override bool HasNext()
				{
					return this.cellIter.HasNext();
				}

				public override AbstractVisitingPrefixTreeFilter.VNode Next()
				{
					//HM:revisit
					//assert hasNext();
					this.vNode.Reset(this.cellIter.Next());
					return this.vNode;
				}

				public override void Remove()
				{
				}

				private readonly VisitorTemplate _enclosing;
				//it always removes
			}

			/// <summary>Called first to setup things.</summary>
			/// <remarks>Called first to setup things.</remarks>
			/// <exception cref="System.IO.IOException"></exception>
			protected internal abstract void Start();

			/// <summary>Called last to return the result.</summary>
			/// <remarks>Called last to return the result.</remarks>
			/// <exception cref="System.IO.IOException"></exception>
			protected internal abstract DocIdSet Finish();

			/// <summary>
			/// Visit an indexed cell returned from
			/// <see cref="FindSubCellsToVisit(Lucene.Net.Spatial.Prefix.Tree.Cell)">FindSubCellsToVisit(Lucene.Net.Spatial.Prefix.Tree.Cell)
			/// 	</see>
			/// .
			/// </summary>
			/// <param name="cell">An intersecting cell.</param>
			/// <returns>
			/// true to descend to more levels. It is an error to return true
			/// if cell.level == detailLevel
			/// </returns>
			/// <exception cref="System.IO.IOException"></exception>
			protected internal abstract bool Visit(Cell cell);

			/// <summary>Called after visit() returns true and an indexed leaf cell is found.</summary>
			/// <remarks>
			/// Called after visit() returns true and an indexed leaf cell is found. An
			/// indexed leaf cell means associated documents generally won't be found at
			/// further detail levels.
			/// </remarks>
			/// <exception cref="System.IO.IOException"></exception>
			protected internal abstract void VisitLeaf(Cell cell);

			/// <summary>The cell is either indexed as a leaf or is the last level of detail.</summary>
			/// <remarks>
			/// The cell is either indexed as a leaf or is the last level of detail. It
			/// might not even intersect the query shape, so be sure to check for that.
			/// </remarks>
			/// <exception cref="System.IO.IOException"></exception>
			protected internal abstract void VisitScanned(Cell cell);

			/// <exception cref="System.IO.IOException"></exception>
			protected internal virtual void PreSiblings(AbstractVisitingPrefixTreeFilter.VNode
				 vNode)
			{
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected internal virtual void PostSiblings(AbstractVisitingPrefixTreeFilter.VNode
				 vNode)
			{
			}

			private readonly AbstractVisitingPrefixTreeFilter _enclosing;
			//class VisitorTemplate
		}

		/// <summary>
		/// A visitor node/cell found via the query shape for
		/// <see cref="VisitorTemplate">VisitorTemplate</see>
		/// .
		/// Sometimes these are reset(cell). It's like a LinkedList node but forms a
		/// tree.
		/// </summary>
		/// <lucene.internal></lucene.internal>
		protected internal class VNode
		{
			internal readonly AbstractVisitingPrefixTreeFilter.VNode parent;

			internal Iterator<AbstractVisitingPrefixTreeFilter.VNode> children;

			internal Cell cell;

			/// <summary>call reset(cell) after to set the cell.</summary>
			/// <remarks>call reset(cell) after to set the cell.</remarks>
			internal VNode(AbstractVisitingPrefixTreeFilter.VNode parent)
			{
				//Note: The VNode tree adds more code to debug/maintain v.s. a flattened
				// LinkedList that we used to have. There is more opportunity here for
				// custom behavior (see preSiblings & postSiblings) but that's not
				// leveraged yet. Maybe this is slightly more GC friendly.
				//only null at the root
				//null, then sometimes set, then null
				//not null (except initially before reset())
				// remember to call reset(cell) after
				this.parent = parent;
			}

			internal virtual void Reset(Cell cell)
			{
				//HM:revisit
				//assert cell != null;
				this.cell = cell;
			}
		}
	}
}
