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
	/// <summary>An an LRU cache of mapping from name to int.</summary>
	/// <remarks>
	/// An an LRU cache of mapping from name to int.
	/// Used to cache Ordinals of category paths.
	/// It uses as key, hash of the path instead of the path.
	/// This way the cache takes less RAM, but correctness depends on
	/// assuming no collisions.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public class NameHashIntCacheLRU : NameIntCacheLRU
	{
		internal NameHashIntCacheLRU(int maxCacheSize) : base(maxCacheSize)
		{
		}

		internal override object Key(FacetLabel name)
		{
			return name.LongHashCode();
		}

		internal override object Key(FacetLabel name, int prefixLen)
		{
			return name.Subpath(prefixLen).LongHashCode();
		}
	}
}
