/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Facet.Taxonomy;
using Sharpen;

namespace Lucene.Net.Facet.Taxonomy.Writercache
{
	/// <summary>An an LRU cache of mapping from name to int.</summary>
	/// <remarks>
	/// An an LRU cache of mapping from name to int.
	/// Used to cache Ordinals of category paths.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	internal class NameIntCacheLRU
	{
		private Dictionary<object, int> cache;

		internal long nMisses = 0;

		internal long nHits = 0;

		private int maxCacheSize;

		internal NameIntCacheLRU(int maxCacheSize)
		{
			// Note: Nothing in this class is synchronized. The caller is assumed to be
			// synchronized so that no two methods of this class are called concurrently.
			// for debug
			// for debug
			this.maxCacheSize = maxCacheSize;
			CreateCache(maxCacheSize);
		}

		/// <summary>Maximum number of cache entries before eviction.</summary>
		/// <remarks>Maximum number of cache entries before eviction.</remarks>
		public virtual int GetMaxSize()
		{
			return maxCacheSize;
		}

		/// <summary>Number of entries currently in the cache.</summary>
		/// <remarks>Number of entries currently in the cache.</remarks>
		public virtual int GetSize()
		{
			return cache.Count;
		}

		private void CreateCache(int maxSize)
		{
			if (maxSize < int.MaxValue)
			{
				cache = new LinkedHashMap<object, int>(1000, (float)0.7, true);
			}
			else
			{
				//for LRU
				cache = new Dictionary<object, int>(1000, (float)0.7);
			}
		}

		//no need for LRU
		internal virtual int Get(FacetLabel name)
		{
			int res = cache.Get(Key(name));
			if (res == null)
			{
				nMisses++;
			}
			else
			{
				nHits++;
			}
			return res;
		}

		/// <summary>Subclasses can override this to provide caching by e.g.</summary>
		/// <remarks>Subclasses can override this to provide caching by e.g. hash of the string.
		/// 	</remarks>
		internal virtual object Key(FacetLabel name)
		{
			return name;
		}

		internal virtual object Key(FacetLabel name, int prefixLen)
		{
			return name.Subpath(prefixLen);
		}

		/// <summary>Add a new value to cache.</summary>
		/// <remarks>
		/// Add a new value to cache.
		/// Return true if cache became full and some room need to be made.
		/// </remarks>
		internal virtual bool Put(FacetLabel name, int val)
		{
			cache.Put(Key(name), val);
			return IsCacheFull();
		}

		internal virtual bool Put(FacetLabel name, int prefixLen, int val)
		{
			cache.Put(Key(name, prefixLen), val);
			return IsCacheFull();
		}

		private bool IsCacheFull()
		{
			return cache.Count > maxCacheSize;
		}

		internal virtual void Clear()
		{
			cache.Clear();
		}

		internal virtual string Stats()
		{
			return "#miss=" + nMisses + " #hit=" + nHits;
		}

		/// <summary>If cache is full remove least recently used entries from cache.</summary>
		/// <remarks>
		/// If cache is full remove least recently used entries from cache. Return true
		/// if anything was removed, false otherwise.
		/// See comment in DirectoryTaxonomyWriter.addToCache(CategoryPath, int) for an
		/// explanation why we clean 2/3rds of the cache, and not just one entry.
		/// </remarks>
		internal virtual bool MakeRoomLRU()
		{
			if (!IsCacheFull())
			{
				return false;
			}
			int n = cache.Count - (2 * maxCacheSize) / 3;
			if (n <= 0)
			{
				return false;
			}
			Iterator<object> it = cache.Keys.Iterator();
			int i = 0;
			while (i < n && it.HasNext())
			{
				it.Next();
				it.Remove();
				i++;
			}
			return true;
		}
	}
}
