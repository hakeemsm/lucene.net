/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Facet.Sortedset;
using Lucene.Net.Index;
using Sharpen;

namespace Lucene.Net.Facet.Sortedset
{
	/// <summary>
	/// Wraps a
	/// <see cref="Lucene.Net.Index.IndexReader">Lucene.Net.Index.IndexReader
	/// 	</see>
	/// and resolves ords
	/// using existing
	/// <see cref="Lucene.Net.Index.SortedSetDocValues">Lucene.Net.Index.SortedSetDocValues
	/// 	</see>
	/// APIs without a
	/// separate taxonomy index.  This only supports flat facets
	/// (dimension + label), and it makes faceting a bit
	/// slower, adds some cost at reopen time, but avoids
	/// managing the separate taxonomy index.  It also requires
	/// less RAM than the taxonomy index, as it manages the flat
	/// (2-level) hierarchy more efficiently.  In addition, the
	/// tie-break during faceting is now meaningful (in label
	/// sorted order).
	/// <p><b>NOTE</b>: creating an instance of this class is
	/// somewhat costly, as it computes per-segment ordinal maps,
	/// so you should create it once and re-use that one instance
	/// for a given
	/// <see cref="Lucene.Net.Index.IndexReader">Lucene.Net.Index.IndexReader
	/// 	</see>
	/// .
	/// </summary>
	public abstract class SortedSetDocValuesReaderState
	{
		/// <summary>
		/// Holds start/end range of ords, which maps to one
		/// dimension (someday we may generalize it to map to
		/// hierarchies within one dimension).
		/// </summary>
		/// <remarks>
		/// Holds start/end range of ords, which maps to one
		/// dimension (someday we may generalize it to map to
		/// hierarchies within one dimension).
		/// </remarks>
		public sealed class OrdRange
		{
			/// <summary>Start of range, inclusive:</summary>
			public readonly int start;

			/// <summary>End of range, inclusive:</summary>
			public readonly int end;

			/// <summary>Start and end are inclusive.</summary>
			/// <remarks>Start and end are inclusive.</remarks>
			public OrdRange(int start, int end)
			{
				this.start = start;
				this.end = end;
			}
		}

		/// <summary>Sole constructor.</summary>
		/// <remarks>Sole constructor.</remarks>
		public SortedSetDocValuesReaderState()
		{
		}

		/// <summary>Return top-level doc values.</summary>
		/// <remarks>Return top-level doc values.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public abstract SortedSetDocValues GetDocValues();

		/// <summary>Indexed field we are reading.</summary>
		/// <remarks>Indexed field we are reading.</remarks>
		public abstract string GetField();

		/// <summary>
		/// Returns the
		/// <see cref="OrdRange">OrdRange</see>
		/// for this dimension.
		/// </summary>
		public abstract SortedSetDocValuesReaderState.OrdRange GetOrdRange(string dim);

		/// <summary>
		/// Returns mapping from prefix to
		/// <see cref="OrdRange">OrdRange</see>
		/// .
		/// </summary>
		public abstract IDictionary<string, SortedSetDocValuesReaderState.OrdRange> GetPrefixToOrdRange
			();

		/// <summary>Returns top-level index reader.</summary>
		/// <remarks>Returns top-level index reader.</remarks>
		public abstract IndexReader GetOrigReader();

		/// <summary>Number of unique labels.</summary>
		/// <remarks>Number of unique labels.</remarks>
		public abstract int GetSize();
	}
}
