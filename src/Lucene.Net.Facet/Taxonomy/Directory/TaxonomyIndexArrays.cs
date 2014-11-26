/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Facet.Taxonomy.Directory;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Facet.Taxonomy.Directory
{
	/// <summary>
	/// A
	/// <see cref="Lucene.Net.Facet.Taxonomy.ParallelTaxonomyArrays">Lucene.Net.Facet.Taxonomy.ParallelTaxonomyArrays
	/// 	</see>
	/// that are initialized from the taxonomy
	/// index.
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	internal class TaxonomyIndexArrays : ParallelTaxonomyArrays
	{
		private readonly int[] parents;

		private volatile bool initializedChildren = false;

		private int[] children;

		private int[] siblings;

		/// <summary>
		/// Used by
		/// <see cref="Add(int, int)">Add(int, int)</see>
		/// after the array grew.
		/// </summary>
		private TaxonomyIndexArrays(int[] parents)
		{
			// the following two arrays are lazily intialized. note that we only keep a
			// single boolean member as volatile, instead of declaring the arrays
			// volatile. the code guarantees that only after the boolean is set to true,
			// the arrays are returned.
			this.parents = parents;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public TaxonomyIndexArrays(IndexReader reader)
		{
			parents = new int[reader.MaxDoc()];
			if (parents.Length > 0)
			{
				InitParents(reader, 0);
				// Starting Lucene 2.9, following the change LUCENE-1542, we can
				// no longer reliably read the parent "-1" (see comment in
				// LuceneTaxonomyWriter.SinglePositionTokenStream). We have no way
				// to fix this in indexing without breaking backward-compatibility
				// with existing indexes, so what we'll do instead is just
				// hard-code the parent of ordinal 0 to be -1, and assume (as is
				// indeed the case) that no other parent can be -1.
				parents[0] = TaxonomyReader.INVALID_ORDINAL;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public TaxonomyIndexArrays(IndexReader reader, Lucene.Net.Facet.Taxonomy.Directory.TaxonomyIndexArrays
			 copyFrom)
		{
			// note that copyParents.length may be equal to reader.maxDoc(). this is not a bug
			// it may be caused if e.g. the taxonomy segments were merged, and so an updated
			// NRT reader was obtained, even though nothing was changed. this is not very likely
			// to happen.
			int[] copyParents = copyFrom != null.Parents();
			this.parents = new int[reader.MaxDoc()];
			System.Array.Copy(copyParents, 0, parents, 0, copyParents.Length);
			InitParents(reader, copyParents.Length);
			if (copyFrom.initializedChildren)
			{
				InitChildrenSiblings(copyFrom);
			}
		}

		private void InitChildrenSiblings(Lucene.Net.Facet.Taxonomy.Directory.TaxonomyIndexArrays
			 copyFrom)
		{
			lock (this)
			{
				if (!initializedChildren)
				{
					// must do this check !
					children = new int[parents.Length];
					siblings = new int[parents.Length];
					if (copyFrom != null)
					{
						// called from the ctor, after we know copyFrom has initialized children/siblings
						System.Array.Copy(copyFrom.Children(), 0, children, 0, copyFrom.Children().Length
							);
						System.Array.Copy(copyFrom.Siblings(), 0, siblings, 0, copyFrom.Siblings().Length
							);
						ComputeChildrenSiblings(copyFrom.parents.Length);
					}
					else
					{
						ComputeChildrenSiblings(0);
					}
					initializedChildren = true;
				}
			}
		}

		private void ComputeChildrenSiblings(int first)
		{
			// reset the youngest child of all ordinals. while this should be done only
			// for the leaves, we don't know up front which are the leaves, so we reset
			// all of them.
			for (int i = first; i < parents.Length; i++)
			{
				children[i] = TaxonomyReader.INVALID_ORDINAL;
			}
			// the root category has no parent, and therefore no siblings
			if (first == 0)
			{
				first = 1;
				siblings[0] = TaxonomyReader.INVALID_ORDINAL;
			}
			for (int i_1 = first; i_1 < parents.Length; i_1++)
			{
				// note that parents[i] is always < i, so the right-hand-side of
				// the following line is already set when we get here
				siblings[i_1] = children[parents[i_1]];
				children[parents[i_1]] = i_1;
			}
		}

		// Read the parents of the new categories
		/// <exception cref="System.IO.IOException"></exception>
		private void InitParents(IndexReader reader, int first)
		{
			if (reader.MaxDoc() == first)
			{
				return;
			}
			// it's ok to use MultiFields because we only iterate on one posting list.
			// breaking it to loop over the leaves() only complicates code for no
			// apparent gain.
			DocsAndPositionsEnum positions = MultiFields.GetTermPositionsEnum(reader, null, Consts
				.FIELD_PAYLOADS, Consts.PAYLOAD_PARENT_BYTES_REF, DocsAndPositionsEnum.FLAG_PAYLOADS
				);
			// shouldn't really happen, if it does, something's wrong
			if (positions == null || positions.Advance(first) == DocIdSetIterator.NO_MORE_DOCS)
			{
				throw new CorruptIndexException("Missing parent data for category " + first);
			}
			int num = reader.MaxDoc();
			for (int i = first; i < num; i++)
			{
				if (positions.DocID() == i)
				{
					if (positions.Freq() == 0)
					{
						// shouldn't happen
						throw new CorruptIndexException("Missing parent data for category " + i);
					}
					parents[i] = positions.NextPosition();
					if (positions.NextDoc() == DocIdSetIterator.NO_MORE_DOCS)
					{
						if (i + 1 < num)
						{
							throw new CorruptIndexException("Missing parent data for category " + (i + 1));
						}
						break;
					}
				}
				else
				{
					// this shouldn't happen
					throw new CorruptIndexException("Missing parent data for category " + i);
				}
			}
		}

		/// <summary>
		/// Adds the given ordinal/parent info and returns either a new instance if the
		/// underlying array had to grow, or this instance otherwise.
		/// </summary>
		/// <remarks>
		/// Adds the given ordinal/parent info and returns either a new instance if the
		/// underlying array had to grow, or this instance otherwise.
		/// <p>
		/// <b>NOTE:</b> you should call this method from a thread-safe code.
		/// </remarks>
		internal virtual Lucene.Net.Facet.Taxonomy.Directory.TaxonomyIndexArrays Add
			(int ordinal, int parentOrdinal)
		{
			if (ordinal >= parents.Length)
			{
				int[] newarray = ArrayUtil.Grow(parents, ordinal + 1);
				newarray[ordinal] = parentOrdinal;
				return new Lucene.Net.Facet.Taxonomy.Directory.TaxonomyIndexArrays(newarray
					);
			}
			parents[ordinal] = parentOrdinal;
			return this;
		}

		/// <summary>
		/// Returns the parents array, where
		/// <code>parents[i]</code>
		/// denotes the parent of
		/// category ordinal
		/// <code>i</code>
		/// .
		/// </summary>
		public override int[] Parents()
		{
			return parents;
		}

		/// <summary>
		/// Returns the children array, where
		/// <code>children[i]</code>
		/// denotes the youngest
		/// child of category ordinal
		/// <code>i</code>
		/// . The youngest child is defined as the
		/// category that was added last to the taxonomy as an immediate child of
		/// <code>i</code>
		/// .
		/// </summary>
		public override int[] Children()
		{
			if (!initializedChildren)
			{
				InitChildrenSiblings(null);
			}
			// the array is guaranteed to be populated
			return children;
		}

		/// <summary>
		/// Returns the siblings array, where
		/// <code>siblings[i]</code>
		/// denotes the sibling
		/// of category ordinal
		/// <code>i</code>
		/// . The sibling is defined as the previous
		/// youngest child of
		/// <code>parents[i]</code>
		/// .
		/// </summary>
		public override int[] Siblings()
		{
			if (!initializedChildren)
			{
				InitChildrenSiblings(null);
			}
			// the array is guaranteed to be populated
			return siblings;
		}
	}
}
