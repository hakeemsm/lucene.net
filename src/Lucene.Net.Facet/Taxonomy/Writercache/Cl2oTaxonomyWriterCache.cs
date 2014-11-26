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
	/// <see cref="TaxonomyWriterCache">TaxonomyWriterCache</see>
	/// using
	/// <see cref="CompactLabelToOrdinal">CompactLabelToOrdinal</see>
	/// . Although
	/// called cache, it maintains in memory all the mappings from category to
	/// ordinal, relying on that
	/// <see cref="CompactLabelToOrdinal">CompactLabelToOrdinal</see>
	/// is an efficient
	/// mapping for this purpose.
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class Cl2oTaxonomyWriterCache : TaxonomyWriterCache
	{
		private readonly ReadWriteLock Lock = new ReentrantReadWriteLock();

		private readonly int initialCapcity;

		private readonly int numHashArrays;

		private readonly float loadFactor;

		private volatile CompactLabelToOrdinal cache;

		/// <summary>Sole constructor.</summary>
		/// <remarks>Sole constructor.</remarks>
		public Cl2oTaxonomyWriterCache(int initialCapcity, float loadFactor, int numHashArrays
			)
		{
			this.cache = new CompactLabelToOrdinal(initialCapcity, loadFactor, numHashArrays);
			this.initialCapcity = initialCapcity;
			this.numHashArrays = numHashArrays;
			this.loadFactor = loadFactor;
		}

		public virtual void Clear()
		{
			Lock.WriteLock().Lock();
			try
			{
				cache = new CompactLabelToOrdinal(initialCapcity, loadFactor, numHashArrays);
			}
			finally
			{
				Lock.WriteLock().Unlock();
			}
		}

		public virtual void Close()
		{
			lock (this)
			{
				cache = null;
			}
		}

		public virtual bool IsFull()
		{
			// This cache is never full
			return false;
		}

		public virtual int Get(FacetLabel categoryPath)
		{
			Lock.ReadLock().Lock();
			try
			{
				return cache.GetOrdinal(categoryPath);
			}
			finally
			{
				Lock.ReadLock().Unlock();
			}
		}

		public virtual bool Put(FacetLabel categoryPath, int ordinal)
		{
			Lock.WriteLock().Lock();
			try
			{
				cache.AddLabel(categoryPath, ordinal);
				// Tell the caller we didn't clear part of the cache, so it doesn't
				// have to flush its on-disk index now
				return false;
			}
			finally
			{
				Lock.WriteLock().Unlock();
			}
		}

		/// <summary>Returns the number of bytes in memory used by this object.</summary>
		/// <remarks>Returns the number of bytes in memory used by this object.</remarks>
		public virtual int GetMemoryUsage()
		{
			return cache == null ? 0 : cache.GetMemoryUsage();
		}
	}
}
