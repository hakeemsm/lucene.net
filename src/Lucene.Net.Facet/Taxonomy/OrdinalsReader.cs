/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Index;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Facet.Taxonomy
{
	/// <summary>Provides per-document ordinals.</summary>
	/// <remarks>Provides per-document ordinals.</remarks>
	public abstract class OrdinalsReader
	{
		/// <summary>Returns ordinals for documents in one segment.</summary>
		/// <remarks>Returns ordinals for documents in one segment.</remarks>
		public abstract class OrdinalsSegmentReader
		{
			/// <summary>Get the ordinals for this document.</summary>
			/// <remarks>
			/// Get the ordinals for this document.  ordinals.offset
			/// must always be 0!
			/// </remarks>
			/// <exception cref="System.IO.IOException"></exception>
			public abstract void Get(int doc, IntsRef ordinals);

			/// <summary>Default constructor.</summary>
			/// <remarks>Default constructor.</remarks>
			public OrdinalsSegmentReader()
			{
			}
		}

		/// <summary>Default constructor.</summary>
		/// <remarks>Default constructor.</remarks>
		public OrdinalsReader()
		{
		}

		/// <summary>Set current atomic reader.</summary>
		/// <remarks>Set current atomic reader.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public abstract OrdinalsReader.OrdinalsSegmentReader GetReader(AtomicReaderContext
			 context);

		/// <summary>
		/// Returns the indexed field name this
		/// <code>OrdinalsReader</code>
		/// is reading from.
		/// </summary>
		public abstract string GetIndexFieldName();
	}
}
