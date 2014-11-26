/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Lucene.Net.Facet;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Facet.Taxonomy.Directory;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Sharpen;
using Sharpen.Logging;

namespace Lucene.Net.Facet.Taxonomy.Directory
{
	/// <summary>
	/// A
	/// <see cref="Lucene.Net.Facet.Taxonomy.TaxonomyReader">Lucene.Net.Facet.Taxonomy.TaxonomyReader
	/// 	</see>
	/// which retrieves stored taxonomy information from a
	/// <see cref="Lucene.Net.Store.Directory">Lucene.Net.Store.Directory</see>
	/// .
	/// <P>
	/// Reading from the on-disk index on every method call is too slow, so this
	/// implementation employs caching: Some methods cache recent requests and their
	/// results, while other methods prefetch all the data into memory and then
	/// provide answers directly from in-memory tables. See the documentation of
	/// individual methods for comments on their performance.
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class DirectoryTaxonomyReader : TaxonomyReader
	{
		private static readonly Logger logger = Logger.GetLogger(typeof(Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyReader
			).FullName);

		private const int DEFAULT_CACHE_VALUE = 4000;

		private readonly DirectoryTaxonomyWriter taxoWriter;

		private readonly long taxoEpoch;

		private readonly DirectoryReader indexReader;

		private LRUHashMap<FacetLabel, int> ordinalCache;

		private LRUHashMap<int, FacetLabel> categoryCache;

		private volatile TaxonomyIndexArrays taxoArrays;

		/// <summary>
		/// Called only from
		/// <see cref="DoOpenIfChanged()">DoOpenIfChanged()</see>
		/// . If the taxonomy has been
		/// recreated, you should pass
		/// <code>null</code>
		/// as the caches and parent/children
		/// arrays.
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		internal DirectoryTaxonomyReader(DirectoryReader indexReader, DirectoryTaxonomyWriter
			 taxoWriter, LRUHashMap<FacetLabel, int> ordinalCache, LRUHashMap<int, FacetLabel
			> categoryCache, TaxonomyIndexArrays taxoArrays)
		{
			// javadocs
			// used in doOpenIfChanged 
			// TODO: test DoubleBarrelLRUCache and consider using it instead
			this.indexReader = indexReader;
			this.taxoWriter = taxoWriter;
			this.taxoEpoch = taxoWriter == null ? -1 : taxoWriter.GetTaxonomyEpoch();
			// use the same instance of the cache, note the protective code in getOrdinal and getPath
			this.ordinalCache = ordinalCache == null ? new LRUHashMap<FacetLabel, int>(DEFAULT_CACHE_VALUE
				) : ordinalCache;
			this.categoryCache = categoryCache == null ? new LRUHashMap<int, FacetLabel>(DEFAULT_CACHE_VALUE
				) : categoryCache;
			this.taxoArrays = taxoArrays != null ? new TaxonomyIndexArrays(indexReader, taxoArrays
				) : null;
		}

		/// <summary>
		/// Open for reading a taxonomy stored in a given
		/// <see cref="Lucene.Net.Store.Directory">Lucene.Net.Store.Directory</see>
		/// .
		/// </summary>
		/// <param name="directory">
		/// The
		/// <see cref="Lucene.Net.Store.Directory">Lucene.Net.Store.Directory</see>
		/// in which the taxonomy resides.
		/// </param>
		/// <exception cref="Lucene.Net.Index.CorruptIndexException">if the Taxonomy is corrupt.
		/// 	</exception>
		/// <exception cref="System.IO.IOException">if another error occurred.</exception>
		public DirectoryTaxonomyReader(Lucene.Net.Store.Directory directory)
		{
			indexReader = OpenIndexReader(directory);
			taxoWriter = null;
			taxoEpoch = -1;
			// These are the default cache sizes; they can be configured after
			// construction with the cache's setMaxSize() method
			ordinalCache = new LRUHashMap<FacetLabel, int>(DEFAULT_CACHE_VALUE);
			categoryCache = new LRUHashMap<int, FacetLabel>(DEFAULT_CACHE_VALUE);
		}

		/// <summary>
		/// Opens a
		/// <see cref="DirectoryTaxonomyReader">DirectoryTaxonomyReader</see>
		/// over the given
		/// <see cref="DirectoryTaxonomyWriter">DirectoryTaxonomyWriter</see>
		/// (for NRT).
		/// </summary>
		/// <param name="taxoWriter">
		/// The
		/// <see cref="DirectoryTaxonomyWriter">DirectoryTaxonomyWriter</see>
		/// from which to obtain newly
		/// added categories, in real-time.
		/// </param>
		/// <exception cref="System.IO.IOException"></exception>
		public DirectoryTaxonomyReader(DirectoryTaxonomyWriter taxoWriter)
		{
			this.taxoWriter = taxoWriter;
			taxoEpoch = taxoWriter.GetTaxonomyEpoch();
			indexReader = OpenIndexReader(taxoWriter.GetInternalIndexWriter());
			// These are the default cache sizes; they can be configured after
			// construction with the cache's setMaxSize() method
			ordinalCache = new LRUHashMap<FacetLabel, int>(DEFAULT_CACHE_VALUE);
			categoryCache = new LRUHashMap<int, FacetLabel>(DEFAULT_CACHE_VALUE);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void InitTaxoArrays()
		{
			lock (this)
			{
				if (taxoArrays == null)
				{
					// according to Java Concurrency in Practice, this might perform better on
					// some JVMs, because the array initialization doesn't happen on the
					// volatile member.
					TaxonomyIndexArrays tmpArrays = new TaxonomyIndexArrays(indexReader);
					taxoArrays = tmpArrays;
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal override void DoClose()
		{
			indexReader.Close();
			taxoArrays = null;
			// do not clear() the caches, as they may be used by other DTR instances.
			ordinalCache = null;
			categoryCache = null;
		}

		/// <summary>
		/// Implements the opening of a new
		/// <see cref="DirectoryTaxonomyReader">DirectoryTaxonomyReader</see>
		/// instance if
		/// the taxonomy has changed.
		/// <p>
		/// <b>NOTE:</b> the returned
		/// <see cref="DirectoryTaxonomyReader">DirectoryTaxonomyReader</see>
		/// shares the
		/// ordinal and category caches with this reader. This is not expected to cause
		/// any issues, unless the two instances continue to live. The reader
		/// guarantees that the two instances cannot affect each other in terms of
		/// correctness of the caches, however if the size of the cache is changed
		/// through
		/// <see cref="SetCacheSize(int)">SetCacheSize(int)</see>
		/// , it will affect both reader instances.
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal override TaxonomyReader DoOpenIfChanged()
		{
			EnsureOpen();
			// This works for both NRT and non-NRT readers (i.e. an NRT reader remains NRT).
			DirectoryReader r2 = DirectoryReader.OpenIfChanged(indexReader);
			if (r2 == null)
			{
				return null;
			}
			// no changes, nothing to do
			// check if the taxonomy was recreated
			bool success = false;
			try
			{
				bool recreated = false;
				if (taxoWriter == null)
				{
					// not NRT, check epoch from commit data
					string t1 = indexReader.GetIndexCommit().GetUserData().Get(DirectoryTaxonomyWriter
						.INDEX_EPOCH);
					string t2 = r2.GetIndexCommit().GetUserData().Get(DirectoryTaxonomyWriter.INDEX_EPOCH
						);
					if (t1 == null)
					{
						if (t2 != null)
						{
							recreated = true;
						}
					}
					else
					{
						if (!t1.Equals(t2))
						{
							// t1 != null and t2 cannot be null b/c DirTaxoWriter always puts the commit data.
							// it's ok to use String.equals because we require the two epoch values to be the same.
							recreated = true;
						}
					}
				}
				else
				{
					// NRT, compare current taxoWriter.epoch() vs the one that was given at construction
					if (taxoEpoch != taxoWriter.GetTaxonomyEpoch())
					{
						recreated = true;
					}
				}
				Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyReader newtr;
				if (recreated)
				{
					// if recreated, do not reuse anything from this instace. the information
					// will be lazily computed by the new instance when needed.
					newtr = new Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyReader(r2
						, taxoWriter, null, null, null);
				}
				else
				{
					newtr = new Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyReader(r2
						, taxoWriter, ordinalCache, categoryCache, taxoArrays);
				}
				success = true;
				return newtr;
			}
			finally
			{
				if (!success)
				{
					IOUtils.CloseWhileHandlingException(r2);
				}
			}
		}

		/// <summary>
		/// Open the
		/// <see cref="Lucene.Net.Index.DirectoryReader">Lucene.Net.Index.DirectoryReader
		/// 	</see>
		/// from this
		/// <see cref="Lucene.Net.Store.Directory">Lucene.Net.Store.Directory</see>
		/// .
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual DirectoryReader OpenIndexReader(Lucene.Net.Store.Directory
			 directory)
		{
			return DirectoryReader.Open(directory);
		}

		/// <summary>
		/// Open the
		/// <see cref="Lucene.Net.Index.DirectoryReader">Lucene.Net.Index.DirectoryReader
		/// 	</see>
		/// from this
		/// <see cref="Lucene.Net.Index.IndexWriter">Lucene.Net.Index.IndexWriter
		/// 	</see>
		/// .
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual DirectoryReader OpenIndexReader(IndexWriter writer)
		{
			return DirectoryReader.Open(writer, false);
		}

		/// <summary>
		/// Expert: returns the underlying
		/// <see cref="Lucene.Net.Index.DirectoryReader">Lucene.Net.Index.DirectoryReader
		/// 	</see>
		/// instance that is
		/// used by this
		/// <see cref="Lucene.Net.Facet.Taxonomy.TaxonomyReader">Lucene.Net.Facet.Taxonomy.TaxonomyReader
		/// 	</see>
		/// .
		/// </summary>
		internal virtual DirectoryReader GetInternalIndexReader()
		{
			EnsureOpen();
			return indexReader;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override ParallelTaxonomyArrays GetParallelTaxonomyArrays()
		{
			EnsureOpen();
			if (taxoArrays == null)
			{
				InitTaxoArrays();
			}
			return taxoArrays;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override IDictionary<string, string> GetCommitUserData()
		{
			EnsureOpen();
			return indexReader.GetIndexCommit().GetUserData();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override int GetOrdinal(FacetLabel cp)
		{
			EnsureOpen();
			if (cp.length == 0)
			{
				return ROOT_ORDINAL;
			}
			// First try to find the answer in the LRU cache:
			lock (ordinalCache)
			{
				int res = ordinalCache.Get(cp);
				if (res != null)
				{
					if (res < indexReader.MaxDoc())
					{
						// Since the cache is shared with DTR instances allocated from
						// doOpenIfChanged, we need to ensure that the ordinal is one that
						// this DTR instance recognizes.
						return res;
					}
					else
					{
						// if we get here, it means that the category was found in the cache,
						// but is not recognized by this TR instance. Therefore there's no
						// need to continue search for the path on disk, because we won't find
						// it there too.
						return TaxonomyReader.INVALID_ORDINAL;
					}
				}
			}
			// If we're still here, we have a cache miss. We need to fetch the
			// value from disk, and then also put it in the cache:
			int ret = TaxonomyReader.INVALID_ORDINAL;
			DocsEnum docs = MultiFields.GetTermDocsEnum(indexReader, null, Consts.FULL, new BytesRef
				(FacetsConfig.PathToString(cp.components, cp.length)), 0);
			if (docs != null && docs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
			{
				ret = docs.DocID();
				// we only store the fact that a category exists, not its inexistence.
				// This is required because the caches are shared with new DTR instances
				// that are allocated from doOpenIfChanged. Therefore, if we only store
				// information about found categories, we cannot accidently tell a new
				// generation of DTR that a category does not exist.
				lock (ordinalCache)
				{
					ordinalCache.Put(cp, Sharpen.Extensions.ValueOf(ret));
				}
			}
			return ret;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FacetLabel GetPath(int ordinal)
		{
			EnsureOpen();
			// Since the cache is shared with DTR instances allocated from
			// doOpenIfChanged, we need to ensure that the ordinal is one that this DTR
			// instance recognizes. Therefore we do this check up front, before we hit
			// the cache.
			if (ordinal < 0 || ordinal >= indexReader.MaxDoc())
			{
				return null;
			}
			// TODO: can we use an int-based hash impl, such as IntToObjectMap,
			// wrapped as LRU?
			int catIDInteger = Sharpen.Extensions.ValueOf(ordinal);
			lock (categoryCache)
			{
				FacetLabel res = categoryCache.Get(catIDInteger);
				if (res != null)
				{
					return res;
				}
			}
			Lucene.Net.Document.Document doc = indexReader.Document(ordinal);
			FacetLabel ret = new FacetLabel(FacetsConfig.StringToPath(doc.Get(Consts.FULL)));
			lock (categoryCache)
			{
				categoryCache.Put(catIDInteger, ret);
			}
			return ret;
		}

		public override int GetSize()
		{
			EnsureOpen();
			return indexReader.NumDocs();
		}

		/// <summary>
		/// setCacheSize controls the maximum allowed size of each of the caches
		/// used by
		/// <see cref="GetPath(int)">GetPath(int)</see>
		/// and
		/// <see cref="GetOrdinal(Lucene.Net.Facet.Taxonomy.FacetLabel)">GetOrdinal(Lucene.Net.Facet.Taxonomy.FacetLabel)
		/// 	</see>
		/// .
		/// <P>
		/// Currently, if the given size is smaller than the current size of
		/// a cache, it will not shrink, and rather we be limited to its current
		/// size.
		/// </summary>
		/// <param name="size">the new maximum cache size, in number of entries.</param>
		public virtual void SetCacheSize(int size)
		{
			EnsureOpen();
			lock (categoryCache)
			{
				categoryCache.SetMaxSize(size);
			}
			lock (ordinalCache)
			{
				ordinalCache.SetMaxSize(size);
			}
		}

		/// <summary>
		/// Returns ordinal -&gt; label mapping, up to the provided
		/// max ordinal or number of ordinals, whichever is
		/// smaller.
		/// </summary>
		/// <remarks>
		/// Returns ordinal -&gt; label mapping, up to the provided
		/// max ordinal or number of ordinals, whichever is
		/// smaller.
		/// </remarks>
		public virtual string ToString(int max)
		{
			EnsureOpen();
			StringBuilder sb = new StringBuilder();
			int upperl = Math.Min(max, indexReader.MaxDoc());
			for (int i = 0; i < upperl; i++)
			{
				try
				{
					FacetLabel category = this.GetPath(i);
					if (category == null)
					{
						sb.Append(i + ": NULL!! \n");
						continue;
					}
					if (category.length == 0)
					{
						sb.Append(i + ": EMPTY STRING!! \n");
						continue;
					}
					sb.Append(i + ": " + category.ToString() + "\n");
				}
				catch (IOException e)
				{
					if (logger.IsLoggable(Level.FINEST))
					{
						logger.Log(Level.FINEST, e.Message, e);
					}
				}
			}
			return sb.ToString();
		}
	}
}
