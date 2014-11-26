/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Sharpen;

namespace Lucene.Net.Facet.Taxonomy
{
	/// <summary>
	/// Returns 3 arrays for traversing the taxonomy:
	/// <ul>
	/// <li>
	/// <code>parents</code>
	/// :
	/// <code>parents[i]</code>
	/// denotes the parent of category
	/// ordinal
	/// <code>i</code>
	/// .</li>
	/// <li>
	/// <code>children</code>
	/// :
	/// <code>children[i]</code>
	/// denotes a child of category ordinal
	/// <code>i</code>
	/// .</li>
	/// <li>
	/// <code>siblings</code>
	/// :
	/// <code>siblings[i]</code>
	/// denotes the sibling of category
	/// ordinal
	/// <code>i</code>
	/// .</li>
	/// </ul>
	/// To traverse the taxonomy tree, you typically start with
	/// <code>children[0]</code>
	/// (ordinal 0 is reserved for ROOT), and then depends if you want to do DFS or
	/// BFS, you call
	/// <code>children[children[0]]</code>
	/// or
	/// <code>siblings[children[0]]</code>
	/// and so forth, respectively.
	/// <p>
	/// <b>NOTE:</b> you are not expected to modify the values of the arrays, since
	/// the arrays are shared with other threads.
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public abstract class ParallelTaxonomyArrays
	{
		/// <summary>Sole constructor.</summary>
		/// <remarks>Sole constructor.</remarks>
		public ParallelTaxonomyArrays()
		{
		}

		/// <summary>
		/// Returns the parents array, where
		/// <code>parents[i]</code>
		/// denotes the parent of
		/// category ordinal
		/// <code>i</code>
		/// .
		/// </summary>
		public abstract int[] Parents();

		/// <summary>
		/// Returns the children array, where
		/// <code>children[i]</code>
		/// denotes a child of
		/// category ordinal
		/// <code>i</code>
		/// .
		/// </summary>
		public abstract int[] Children();

		/// <summary>
		/// Returns the siblings array, where
		/// <code>siblings[i]</code>
		/// denotes the sibling
		/// of category ordinal
		/// <code>i</code>
		/// .
		/// </summary>
		public abstract int[] Siblings();
	}
}
