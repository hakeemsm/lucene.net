/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Facet.Taxonomy.Writercache;
using Sharpen;

namespace Lucene.Net.Facet.Taxonomy.Writercache
{
	/// <summary>
	/// Utilities for use of
	/// <see cref="Lucene.Net.Facet.Taxonomy.FacetLabel">Lucene.Net.Facet.Taxonomy.FacetLabel
	/// 	</see>
	/// by
	/// <see cref="CompactLabelToOrdinal">CompactLabelToOrdinal</see>
	/// .
	/// </summary>
	internal class CategoryPathUtils
	{
		/// <summary>
		/// Serializes the given
		/// <see cref="Lucene.Net.Facet.Taxonomy.FacetLabel">Lucene.Net.Facet.Taxonomy.FacetLabel
		/// 	</see>
		/// to the
		/// <see cref="CharBlockArray">CharBlockArray</see>
		/// .
		/// </summary>
		public static void Serialize(FacetLabel cp, CharBlockArray charBlockArray)
		{
			charBlockArray.Append((char)cp.length);
			if (cp.length == 0)
			{
				return;
			}
			for (int i = 0; i < cp.length; i++)
			{
				charBlockArray.Append((char)cp.components[i].Length);
				charBlockArray.Append(cp.components[i]);
			}
		}

		/// <summary>
		/// Calculates a hash function of a path that was serialized with
		/// <see cref="Serialize(Lucene.Net.Facet.Taxonomy.FacetLabel, CharBlockArray)
		/// 	">Serialize(Lucene.Net.Facet.Taxonomy.FacetLabel, CharBlockArray)</see>
		/// .
		/// </summary>
		public static int HashCodeOfSerialized(CharBlockArray charBlockArray, int offset)
		{
			int length = charBlockArray[offset++];
			if (length == 0)
			{
				return 0;
			}
			int hash = length;
			for (int i = 0; i < length; i++)
			{
				int len = charBlockArray[offset++];
				hash = hash * 31 + charBlockArray.SubSequence(offset, offset + len).GetHashCode();
				offset += len;
			}
			return hash;
		}

		/// <summary>
		/// Check whether the
		/// <see cref="Lucene.Net.Facet.Taxonomy.FacetLabel">Lucene.Net.Facet.Taxonomy.FacetLabel
		/// 	</see>
		/// is equal to the one serialized in
		/// <see cref="CharBlockArray">CharBlockArray</see>
		/// .
		/// </summary>
		public static bool EqualsToSerialized(FacetLabel cp, CharBlockArray charBlockArray
			, int offset)
		{
			int n = charBlockArray[offset++];
			if (cp.length != n)
			{
				return false;
			}
			if (cp.length == 0)
			{
				return true;
			}
			for (int i = 0; i < cp.length; i++)
			{
				int len = charBlockArray[offset++];
				if (len != cp.components[i].Length)
				{
					return false;
				}
				if (!cp.components[i].Equals(charBlockArray.SubSequence(offset, offset + len)))
				{
					return false;
				}
				offset += len;
			}
			return true;
		}
	}
}
