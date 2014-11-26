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
	/// LRU
	/// <see cref="TaxonomyWriterCache">TaxonomyWriterCache</see>
	/// - good choice for huge taxonomies.
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class LruTaxonomyWriterCache : TaxonomyWriterCache
	{
		/// <summary>Determines cache type.</summary>
		/// <remarks>
		/// Determines cache type.
		/// For guaranteed correctness - not relying on no-collisions in the hash
		/// function, LRU_STRING should be used.
		/// </remarks>
		public enum LRUType
		{
			LRU_HASHED,
			LRU_STRING
		}

		private NameIntCacheLRU cache;

		/// <summary>
		/// Creates this with
		/// <see cref="LRUType.LRU_HASHED">LRUType.LRU_HASHED</see>
		/// method.
		/// </summary>
		public LruTaxonomyWriterCache(int cacheSize) : this(cacheSize, LruTaxonomyWriterCache.LRUType
			.LRU_HASHED)
		{
		}

		/// <summary>Creates this with the specified method.</summary>
		/// <remarks>Creates this with the specified method.</remarks>
		public LruTaxonomyWriterCache(int cacheSize, LruTaxonomyWriterCache.LRUType lruType
			)
		{
			// TODO (Facet): choose between NameHashIntCacheLRU and NameIntCacheLRU.
			// For guaranteed correctness - not relying on no-collisions in the hash
			// function, NameIntCacheLRU should be used:
			// On the other hand, NameHashIntCacheLRU takes less RAM but if there
			// are collisions (which we never found) two different paths would be
			// mapped to the same ordinal...
			// TODO (Facet): choose between NameHashIntCacheLRU and NameIntCacheLRU.
			// For guaranteed correctness - not relying on no-collisions in the hash
			// function, NameIntCacheLRU should be used:
			// On the other hand, NameHashIntCacheLRU takes less RAM but if there
			// are collisions (which we never found) two different paths would be
			// mapped to the same ordinal...
			if (lruType == LruTaxonomyWriterCache.LRUType.LRU_HASHED)
			{
				this.cache = new NameHashIntCacheLRU(cacheSize);
			}
			else
			{
				this.cache = new NameIntCacheLRU(cacheSize);
			}
		}

		public virtual bool IsFull()
		{
			lock (this)
			{
				return cache.GetSize() == cache.GetMaxSize();
			}
		}

		public virtual void Clear()
		{
			lock (this)
			{
				cache.Clear();
			}
		}

		public virtual void Close()
		{
			lock (this)
			{
				cache.Clear();
				cache = null;
			}
		}

		public virtual int Get(FacetLabel categoryPath)
		{
			lock (this)
			{
				int res = cache.Get(categoryPath);
				if (res == null)
				{
					return -1;
				}
				return res;
			}
		}

		public virtual bool Put(FacetLabel categoryPath, int ordinal)
		{
			lock (this)
			{
				bool ret = cache.Put(categoryPath, ordinal);
				// If the cache is full, we need to clear one or more old entries
				// from the cache. However, if we delete from the cache a recent
				// addition that isn't yet in our reader, for this entry to be
				// visible to us we need to make sure that the changes have been
				// committed and we reopen the reader. Because this is a slow
				// operation, we don't delete entries one-by-one but rather in bulk
				// (put() removes the 2/3rd oldest entries).
				if (ret)
				{
					cache.MakeRoomLRU();
				}
				return ret;
			}
		}
	}
}
