/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
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
	/// Finds docs where its indexed shape
	/// <see cref="Lucene.Net.Spatial.Query.SpatialOperation.Contains">CONTAINS</see>
	/// the query shape. For use on
	/// <see cref="RecursivePrefixTreeStrategy">RecursivePrefixTreeStrategy</see>
	/// .
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class ContainsPrefixTreeFilter : AbstractPrefixTreeFilter
	{
		/// <summary>
		/// If the spatial data for a document is comprised of multiple overlapping or adjacent parts,
		/// it might fail to match a query shape when doing the CONTAINS predicate when the sum of
		/// those shapes contain the query shape but none do individually.
		/// </summary>
		/// <remarks>
		/// If the spatial data for a document is comprised of multiple overlapping or adjacent parts,
		/// it might fail to match a query shape when doing the CONTAINS predicate when the sum of
		/// those shapes contain the query shape but none do individually.  Set this to false to
		/// increase performance if you don't care about that circumstance (such as if your indexed
		/// data doesn't even have such conditions).  See LUCENE-5062.
		/// </remarks>
		protected internal readonly bool multiOverlappingIndexedShapes;

		public ContainsPrefixTreeFilter(Com.Spatial4j.Core.Shape.Shape queryShape, string
			 fieldName, SpatialPrefixTree grid, int detailLevel, bool multiOverlappingIndexedShapes
			) : base(queryShape, fieldName, grid, detailLevel)
		{
			this.multiOverlappingIndexedShapes = multiOverlappingIndexedShapes;
		}

		public override bool Equals(object o)
		{
			if (!base.Equals(o))
			{
				return false;
			}
			return multiOverlappingIndexedShapes == ((Lucene.Net.Spatial.Prefix.ContainsPrefixTreeFilter
				)o).multiOverlappingIndexedShapes;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode() + (multiOverlappingIndexedShapes ? 1 : 0);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs
			)
		{
			return new ContainsPrefixTreeFilter.ContainsVisitor(this, context, acceptDocs).Visit
				(grid.GetWorldCell(), acceptDocs);
		}

		private class ContainsVisitor : AbstractPrefixTreeFilter.BaseTermsEnumTraverser
		{
			/// <exception cref="System.IO.IOException"></exception>
			public ContainsVisitor(ContainsPrefixTreeFilter _enclosing, AtomicReaderContext context
				, Bits acceptDocs) : base(_enclosing)
			{
				this._enclosing = _enclosing;
			}

			internal BytesRef termBytes = new BytesRef();

			internal Cell nextCell;

			//see getLeafDocs
			/// <summary>This is the primary algorithm; recursive.</summary>
			/// <remarks>This is the primary algorithm; recursive.  Returns null if finds none.</remarks>
			/// <exception cref="System.IO.IOException"></exception>
			private ContainsPrefixTreeFilter.SmallDocSet Visit(Cell cell, Bits acceptContains
				)
			{
				if (this.termsEnum == null)
				{
					//signals all done
					return null;
				}
				// Leaf docs match all query shape
				ContainsPrefixTreeFilter.SmallDocSet leafDocs = this.GetLeafDocs(cell, acceptContains
					);
				// Get the AND of all child results (into combinedSubResults)
				ContainsPrefixTreeFilter.SmallDocSet combinedSubResults = null;
				//   Optimization: use null subCellsFilter when we know cell is within the query shape.
				Com.Spatial4j.Core.Shape.Shape subCellsFilter = this._enclosing.queryShape;
				if (cell.GetLevel() != 0 && ((cell.GetShapeRel() == null || cell.GetShapeRel() ==
					 SpatialRelation.WITHIN)))
				{
					subCellsFilter = null;
				}
				ICollection<Cell> subCells = cell.GetShape().Relate(this._enclosing.queryShape) ==
					 SpatialRelation.WITHIN.GetSubCells(subCellsFilter);
				foreach (Cell subCell in subCells)
				{
					if (!this.SeekExact(subCell))
					{
						combinedSubResults = null;
					}
					else
					{
						if (subCell.GetLevel() == this._enclosing.detailLevel)
						{
							combinedSubResults = this.GetDocs(subCell, acceptContains);
						}
						else
						{
							if (!this._enclosing.multiOverlappingIndexedShapes && subCell.GetShapeRel() == SpatialRelation
								.WITHIN)
							{
								combinedSubResults = this.GetLeafDocs(subCell, acceptContains);
							}
							else
							{
								combinedSubResults = this.Visit(subCell, acceptContains);
							}
						}
					}
					//recursion
					if (combinedSubResults == null)
					{
						break;
					}
					acceptContains = combinedSubResults;
				}
				//has the 'AND' effect on next iteration
				// Result: OR the leaf docs with AND of all child results
				if (combinedSubResults != null)
				{
					if (leafDocs == null)
					{
						return combinedSubResults;
					}
					return leafDocs.Union(combinedSubResults);
				}
				//union is 'or'
				return leafDocs;
			}

			/// <exception cref="System.IO.IOException"></exception>
			private bool SeekExact(Cell cell)
			{
				//HM:revisit
				//assert new BytesRef(cell.getTokenBytes()).compareTo(termBytes) > 0;
				this.termBytes.bytes = cell.GetTokenBytes();
				this.termBytes.length = this.termBytes.bytes.Length;
				if (this.termsEnum == null)
				{
					return false;
				}
				return this.termsEnum.SeekExact(this.termBytes);
			}

			/// <exception cref="System.IO.IOException"></exception>
			private ContainsPrefixTreeFilter.SmallDocSet GetDocs(Cell cell, Bits acceptContains
				)
			{
				return this.CollectDocs(new BytesRef(cell.GetTokenBytes()).Equals(this.termBytes)
					);
			}

			private Cell lastLeaf = null;

			//just for assertion
			/// <exception cref="System.IO.IOException"></exception>
			private ContainsPrefixTreeFilter.SmallDocSet GetLeafDocs(Cell leafCell, Bits acceptContains
				)
			{
				//HM:revisit
				//don't call for same leaf again
				this.lastLeaf = leafCell;
				if (this.termsEnum == null)
				{
					return null;
				}
				BytesRef nextTerm = this.termsEnum.Next();
				if (nextTerm == null)
				{
					this.termsEnum = null;
					//signals all done
					return null;
				}
				this.nextCell = this._enclosing.grid.GetCell(nextTerm.bytes, nextTerm.offset, nextTerm
					.length, this.nextCell);
				if (this.nextCell.GetLevel() == leafCell.GetLevel() && this.nextCell.IsLeaf())
				{
					return this.CollectDocs(acceptContains);
				}
				else
				{
					return null;
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			private ContainsPrefixTreeFilter.SmallDocSet CollectDocs(Bits acceptContains)
			{
				ContainsPrefixTreeFilter.SmallDocSet set = null;
				this.docsEnum = this.termsEnum.Docs(acceptContains, this.docsEnum, DocsEnum.FLAG_NONE
					);
				int docid;
				while ((docid = this.docsEnum.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
				{
					if (set == null)
					{
						int size = this.termsEnum.DocFreq();
						if (size <= 0)
						{
							size = 16;
						}
						set = new ContainsPrefixTreeFilter.SmallDocSet(size);
					}
					set.Set(docid);
				}
				return set;
			}

			private readonly ContainsPrefixTreeFilter _enclosing;
			//class ContainsVisitor
		}

		/// <summary>A hash based mutable set of docIds.</summary>
		/// <remarks>
		/// A hash based mutable set of docIds. If this were Solr code then we might
		/// use a combination of HashDocSet and SortedIntDocSet instead.
		/// </remarks>
		private class SmallDocSet : DocIdSet, Lucene.Net.Util.Bits
		{
			private readonly SentinelIntSet intSet;

			private int maxInt = 0;

			public SmallDocSet(int size)
			{
				intSet = new SentinelIntSet(size, -1);
			}

			public override bool Get(int index)
			{
				return intSet.Exists(index);
			}

			public virtual void Set(int index)
			{
				intSet.Put(index);
				if (index > maxInt)
				{
					maxInt = index;
				}
			}

			/// <summary>Largest docid.</summary>
			/// <remarks>Largest docid.</remarks>
			public override int Length()
			{
				return maxInt;
			}

			/// <summary>Number of docids.</summary>
			/// <remarks>Number of docids.</remarks>
			public virtual int Size()
			{
				return intSet.Size();
			}

			/// <summary>NOTE: modifies and returns either "this" or "other"</summary>
			public virtual ContainsPrefixTreeFilter.SmallDocSet Union(ContainsPrefixTreeFilter.SmallDocSet
				 other)
			{
				ContainsPrefixTreeFilter.SmallDocSet bigger;
				ContainsPrefixTreeFilter.SmallDocSet smaller;
				if (other.intSet.Size() > this.intSet.Size())
				{
					bigger = other;
					smaller = this;
				}
				else
				{
					bigger = this;
					smaller = other;
				}
				//modify bigger
				foreach (int v in smaller.intSet.keys)
				{
					if (v == smaller.intSet.emptyVal)
					{
						continue;
					}
					bigger.Set(v);
				}
				return bigger;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override Lucene.Net.Util.Bits Bits()
			{
				//if the # of docids is super small, return null since iteration is going
				// to be faster
				return Size() > 4 ? this : null;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override DocIdSetIterator Iterator()
			{
				if (Size() == 0)
				{
					return null;
				}
				//copy the unsorted values to a new array then sort them
				int d = 0;
				int[] docs = new int[intSet.Size()];
				foreach (int v in intSet.keys)
				{
					if (v == intSet.emptyVal)
					{
						continue;
					}
					docs[d++] = v;
				}
				int size = d == intSet.Size();
				//sort them
				Arrays.Sort(docs, 0, size);
				return new _DocIdSetIterator_270(size, docs);
			}

			private sealed class _DocIdSetIterator_270 : DocIdSetIterator
			{
				public _DocIdSetIterator_270(int size, int[] docs)
				{
					this.size = size;
					this.docs = docs;
					this.idx = -1;
				}

				internal int idx;

				public override int DocID()
				{
					if (this.idx >= 0 && this.idx < size)
					{
						return docs[this.idx];
					}
					else
					{
						return -1;
					}
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override int NextDoc()
				{
					if (++this.idx < size)
					{
						return docs[this.idx];
					}
					return DocIdSetIterator.NO_MORE_DOCS;
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override int Advance(int target)
				{
					//for this small set this is likely faster vs. a binary search
					// into the sorted array
					return this.SlowAdvance(target);
				}

				public override long Cost()
				{
					return size;
				}

				private readonly int size;

				private readonly int[] docs;
			}
			//class SmallDocSet
		}
	}
}
