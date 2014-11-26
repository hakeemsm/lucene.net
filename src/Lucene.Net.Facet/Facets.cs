/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Facet;
using Sharpen;

namespace Lucene.Net.Facet
{
	/// <summary>Common base class for all facets implementations.</summary>
	/// <remarks>Common base class for all facets implementations.</remarks>
	/// <lucene.experimental></lucene.experimental>
	public abstract class Facets
	{
		/// <summary>Default constructor.</summary>
		/// <remarks>Default constructor.</remarks>
		public Facets()
		{
		}

		/// <summary>
		/// Returns the topN child labels under the specified
		/// path.
		/// </summary>
		/// <remarks>
		/// Returns the topN child labels under the specified
		/// path.  Returns null if the specified path doesn't
		/// exist or if this dimension was never seen.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public abstract FacetResult GetTopChildren(int topN, string dim, params string[] 
			path);

		/// <summary>
		/// Return the count or value
		/// for a specific path.
		/// </summary>
		/// <remarks>
		/// Return the count or value
		/// for a specific path.  Returns -1 if
		/// this path doesn't exist, else the count.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public abstract Number GetSpecificValue(string dim, params string[] path);

		/// <summary>
		/// Returns topN labels for any dimension that had hits,
		/// sorted by the number of hits that dimension matched;
		/// this is used for "sparse" faceting, where many
		/// different dimensions were indexed, for example
		/// depending on the type of document.
		/// </summary>
		/// <remarks>
		/// Returns topN labels for any dimension that had hits,
		/// sorted by the number of hits that dimension matched;
		/// this is used for "sparse" faceting, where many
		/// different dimensions were indexed, for example
		/// depending on the type of document.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public abstract IList<FacetResult> GetAllDims(int topN);
	}
}
