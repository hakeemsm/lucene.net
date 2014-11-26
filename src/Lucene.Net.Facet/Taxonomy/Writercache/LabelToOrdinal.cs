/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Facet.Taxonomy;
using Sharpen;

namespace Lucene.Net.Facet.Taxonomy.Writercache
{
	/// <summary>Abstract class for storing Label-&gt;Ordinal mappings in a taxonomy.</summary>
	/// <remarks>Abstract class for storing Label-&gt;Ordinal mappings in a taxonomy.</remarks>
	/// <lucene.experimental></lucene.experimental>
	public abstract class LabelToOrdinal
	{
		/// <summary>How many ordinals we've seen.</summary>
		/// <remarks>How many ordinals we've seen.</remarks>
		protected internal int counter;

		/// <summary>
		/// Returned by
		/// <see cref="GetOrdinal(Lucene.Net.Facet.Taxonomy.FacetLabel)">GetOrdinal(Lucene.Net.Facet.Taxonomy.FacetLabel)
		/// 	</see>
		/// when the label isn't
		/// recognized.
		/// </summary>
		public const int INVALID_ORDINAL = -2;

		/// <summary>Default constructor.</summary>
		/// <remarks>Default constructor.</remarks>
		public LabelToOrdinal()
		{
		}

		/// <summary>return the maximal Ordinal assigned so far</summary>
		public virtual int GetMaxOrdinal()
		{
			return this.counter;
		}

		/// <summary>Returns the next unassigned ordinal.</summary>
		/// <remarks>
		/// Returns the next unassigned ordinal. The default behavior of this method
		/// is to simply increment a counter.
		/// </remarks>
		public virtual int GetNextOrdinal()
		{
			return this.counter++;
		}

		/// <summary>Adds a new label if its not yet in the table.</summary>
		/// <remarks>
		/// Adds a new label if its not yet in the table.
		/// Throws an
		/// <see cref="System.ArgumentException">System.ArgumentException</see>
		/// if the same label with
		/// a different ordinal was previoulsy added to this table.
		/// </remarks>
		public abstract void AddLabel(FacetLabel label, int ordinal);

		/// <summary>
		/// Returns the ordinal assigned to the given label,
		/// or
		/// <see cref="INVALID_ORDINAL">INVALID_ORDINAL</see>
		/// if the label cannot be found in this table.
		/// </summary>
		public abstract int GetOrdinal(FacetLabel label);
	}
}
