/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Com.Spatial4j.Core.Shape;
using Lucene.Net.Spatial.Prefix.Tree;
using Sharpen;

namespace Lucene.Net.Spatial.Prefix.Tree
{
	/// <summary>Represents a grid cell.</summary>
	/// <remarks>
	/// Represents a grid cell. These are not necessarily thread-safe, although new
	/// Cell("") (world cell) must be.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public abstract class Cell : Comparable<Lucene.Net.Spatial.Prefix.Tree.Cell
		>
	{
		public const byte LEAF_BYTE = (byte)('+');

		private byte[] bytes;

		private int b_off;

		private int b_len;

		private string token;

		/// <summary>
		/// When set via getSubCells(filter), it is the relationship between this cell
		/// and the given shape filter.
		/// </summary>
		/// <remarks>
		/// When set via getSubCells(filter), it is the relationship between this cell
		/// and the given shape filter.
		/// </remarks>
		protected internal SpatialRelation shapeRel;

		/// <summary>Always false for points.</summary>
		/// <remarks>
		/// Always false for points. Otherwise, indicate no further sub-cells are going
		/// to be provided because shapeRel is WITHIN or maxLevels or a detailLevel is
		/// hit.
		/// </remarks>
		protected internal bool leaf;

		protected internal Cell(string token)
		{
			//NOTE: must sort before letters & numbers
			//this is the only part of equality
			this.token = token;
			if (token.Length > 0 && token[token.Length - 1] == (char)LEAF_BYTE)
			{
				this.token = Sharpen.Runtime.Substring(token, 0, token.Length - 1);
				SetLeaf();
			}
			if (GetLevel() == 0)
			{
				GetShape();
			}
		}

		protected internal Cell(byte[] bytes, int off, int len)
		{
			//ensure any lazy instantiation completes to make this threadsafe
			this.bytes = bytes;
			this.b_off = off;
			this.b_len = len;
			B_fixLeaf();
		}

		public virtual void Reset(byte[] bytes, int off, int len)
		{
			GetLevel() != 0 = null;
			shapeRel = null;
			this.bytes = bytes;
			this.b_off = off;
			this.b_len = len;
			B_fixLeaf();
		}

		private void B_fixLeaf()
		{
			//note that non-point shapes always have the maxLevels cell set with setLeaf
			if (bytes[b_off + b_len - 1] == LEAF_BYTE)
			{
				b_len--;
				SetLeaf();
			}
			else
			{
				leaf = false;
			}
		}

		public virtual SpatialRelation GetShapeRel()
		{
			return shapeRel;
		}

		/// <summary>For points, this is always false.</summary>
		/// <remarks>
		/// For points, this is always false.  Otherwise this is true if there are no
		/// further cells with this prefix for the shape (always true at maxLevels).
		/// </remarks>
		public virtual bool IsLeaf()
		{
			return leaf;
		}

		/// <summary>Note: not supported at level 0.</summary>
		/// <remarks>Note: not supported at level 0.</remarks>
		public virtual void SetLeaf()
		{
			GetLevel() != 0 = true;
		}

		/// <summary>Note: doesn't contain a trailing leaf byte.</summary>
		/// <remarks>Note: doesn't contain a trailing leaf byte.</remarks>
		public virtual string GetTokenString()
		{
			if (token == null)
			{
				token = new string(bytes, b_off, b_len, SpatialPrefixTree.UTF8);
			}
			return token;
		}

		/// <summary>Note: doesn't contain a trailing leaf byte.</summary>
		/// <remarks>Note: doesn't contain a trailing leaf byte.</remarks>
		public virtual byte[] GetTokenBytes()
		{
			if (bytes != null)
			{
				if (b_off != 0 || b_len != bytes.Length)
				{
					throw new InvalidOperationException("Not supported if byte[] needs to be recreated."
						);
				}
			}
			else
			{
				bytes = Sharpen.Runtime.GetBytesForString(token, SpatialPrefixTree.UTF8);
				b_off = 0;
				b_len = bytes.Length;
			}
			return bytes;
		}

		public virtual int GetLevel()
		{
			return token != null ? token.Length : b_len;
		}

		//TODO add getParent() and update some algorithms to use this?
		//public Cell getParent();
		/// <summary>
		/// Like
		/// <see cref="GetSubCells()">GetSubCells()</see>
		/// but with the results filtered by a shape. If
		/// that shape is a
		/// <see cref="Com.Spatial4j.Core.Shape.Point">Com.Spatial4j.Core.Shape.Point</see>
		/// then it must call
		/// <see cref="GetSubCell(Com.Spatial4j.Core.Shape.Point)">GetSubCell(Com.Spatial4j.Core.Shape.Point)
		/// 	</see>
		/// . The returned cells
		/// should have
		/// <see cref="GetShapeRel()">GetShapeRel()</see>
		/// set to their relation with
		/// <code>shapeFilter</code>
		/// . In addition,
		/// <see cref="IsLeaf()">IsLeaf()</see>
		/// must be true when that relation is WITHIN.
		/// <p/>
		/// Precondition: Never called when getLevel() == maxLevel.
		/// </summary>
		/// <param name="shapeFilter">an optional filter for the returned cells.</param>
		/// <returns>A set of cells (no dups), sorted. Not Modifiable.</returns>
		public virtual ICollection<Lucene.Net.Spatial.Prefix.Tree.Cell> GetSubCells
			(Com.Spatial4j.Core.Shape.Shape shapeFilter)
		{
			//Note: Higher-performing subclasses might override to consider the shape filter to generate fewer cells.
			if (shapeFilter is Point)
			{
				Lucene.Net.Spatial.Prefix.Tree.Cell subCell = GetSubCell((Point)shapeFilter
					);
				subCell.shapeRel = SpatialRelation.CONTAINS;
				return Sharpen.Collections.SingletonList(subCell);
			}
			ICollection<Lucene.Net.Spatial.Prefix.Tree.Cell> cells = GetSubCells();
			if (shapeFilter == null)
			{
				return cells;
			}
			//TODO change API to return a filtering iterator
			IList<Lucene.Net.Spatial.Prefix.Tree.Cell> copy = new AList<Lucene.Net.Spatial.Prefix.Tree.Cell
				>(cells.Count);
			foreach (Lucene.Net.Spatial.Prefix.Tree.Cell cell in cells)
			{
				SpatialRelation rel = cell.GetShape().Relate(shapeFilter);
				if (rel == SpatialRelation.DISJOINT)
				{
					continue;
				}
				cell.shapeRel = rel;
				if (rel == SpatialRelation.WITHIN)
				{
					cell.SetLeaf();
				}
				copy.AddItem(cell);
			}
			return copy;
		}

		/// <summary>
		/// Performant implementations are expected to implement this efficiently by
		/// considering the current cell's boundary.
		/// </summary>
		/// <remarks>
		/// Performant implementations are expected to implement this efficiently by
		/// considering the current cell's boundary. Precondition: Never called when
		/// getLevel() == maxLevel.
		/// <p/>
		/// Precondition: this.getShape().relate(p) != DISJOINT.
		/// </remarks>
		public abstract Lucene.Net.Spatial.Prefix.Tree.Cell GetSubCell(Point p);

		//TODO Cell getSubCell(byte b)
		/// <summary>Gets the cells at the next grid cell level that cover this cell.</summary>
		/// <remarks>
		/// Gets the cells at the next grid cell level that cover this cell.
		/// Precondition: Never called when getLevel() == maxLevel.
		/// </remarks>
		/// <returns>A set of cells (no dups), sorted, modifiable, not empty, not null.</returns>
		protected internal abstract ICollection<Lucene.Net.Spatial.Prefix.Tree.Cell
			> GetSubCells();

		/// <summary>
		/// <see cref="GetSubCells()">GetSubCells()</see>
		/// .size() -- usually a constant. Should be &gt;=2
		/// </summary>
		public abstract int GetSubCellsSize();

		public abstract Com.Spatial4j.Core.Shape.Shape GetShape();

		public virtual Point GetCenter()
		{
			return GetShape().GetCenter();
		}

		public virtual int CompareTo(Lucene.Net.Spatial.Prefix.Tree.Cell o)
		{
			return Sharpen.Runtime.CompareOrdinal(GetTokenString(), o.GetTokenString());
		}

		public override bool Equals(object obj)
		{
			return !(obj == null || !(obj is Lucene.Net.Spatial.Prefix.Tree.Cell)) && 
				GetTokenString().Equals(((Lucene.Net.Spatial.Prefix.Tree.Cell)obj).GetTokenString
				());
		}

		public override int GetHashCode()
		{
			return GetTokenString().GetHashCode();
		}

		public override string ToString()
		{
			return GetTokenString() + (IsLeaf() ? (char)LEAF_BYTE : string.Empty);
		}
	}
}
