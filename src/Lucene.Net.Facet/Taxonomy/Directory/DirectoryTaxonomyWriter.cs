/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Document;
using Lucene.Net.Facet;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Facet.Taxonomy.Directory;
using Lucene.Net.Facet.Taxonomy.Writercache;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Facet.Taxonomy.Directory
{
	/// <summary>
	/// <see cref="Lucene.Net.Facet.Taxonomy.TaxonomyWriter">Lucene.Net.Facet.Taxonomy.TaxonomyWriter
	/// 	</see>
	/// which uses a
	/// <see cref="Lucene.Net.Store.Directory">Lucene.Net.Store.Directory</see>
	/// to store the taxonomy
	/// information on disk, and keeps an additional in-memory cache of some or all
	/// categories.
	/// <p>
	/// In addition to the permanently-stored information in the
	/// <see cref="Lucene.Net.Store.Directory">Lucene.Net.Store.Directory</see>
	/// ,
	/// efficiency dictates that we also keep an in-memory cache of <B>recently
	/// seen</B> or <B>all</B> categories, so that we do not need to go back to disk
	/// for every category addition to see which ordinal this category already has,
	/// if any. A
	/// <see cref="Lucene.Net.Facet.Taxonomy.Writercache.TaxonomyWriterCache">Lucene.Net.Facet.Taxonomy.Writercache.TaxonomyWriterCache
	/// 	</see>
	/// object determines the specific caching
	/// algorithm used.
	/// <p>
	/// This class offers some hooks for extending classes to control the
	/// <see cref="Lucene.Net.Index.IndexWriter">Lucene.Net.Index.IndexWriter
	/// 	</see>
	/// instance that is used. See
	/// <see cref="OpenIndexWriter(Lucene.Net.Store.Directory, Lucene.Net.Index.IndexWriterConfig)
	/// 	">OpenIndexWriter(Lucene.Net.Store.Directory, Lucene.Net.Index.IndexWriterConfig)
	/// 	</see>
	/// .
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class DirectoryTaxonomyWriter : TaxonomyWriter
	{
		/// <summary>Property name of user commit data that contains the index epoch.</summary>
		/// <remarks>
		/// Property name of user commit data that contains the index epoch. The epoch
		/// changes whenever the taxonomy is recreated (i.e. opened with
		/// <see cref="Lucene.Net.Index.IndexWriterConfig.OpenMode.CREATE">Lucene.Net.Index.IndexWriterConfig.OpenMode.CREATE
		/// 	</see>
		/// .
		/// <p>
		/// Applications should not use this property in their commit data because it
		/// will be overridden by this taxonomy writer.
		/// </remarks>
		public static readonly string INDEX_EPOCH = "index.epoch";

		private readonly Lucene.Net.Store.Directory dir;

		private readonly IndexWriter indexWriter;

		private readonly TaxonomyWriterCache cache;

		private readonly AtomicInteger cacheMisses = new AtomicInteger(0);

		private long indexEpoch;

		private DirectoryTaxonomyWriter.SinglePositionTokenStream parentStream = new DirectoryTaxonomyWriter.SinglePositionTokenStream
			(Consts.PAYLOAD_PARENT);

		private Field parentStreamField;

		private Field fullPathField;

		private int cacheMissesUntilFill = 11;

		private bool shouldFillCache = true;

		private ReaderManager readerManager;

		private volatile bool initializedReaderManager = false;

		private volatile bool shouldRefreshReaderManager;

		/// <summary>
		/// We call the cache "complete" if we know that every category in our
		/// taxonomy is in the cache.
		/// </summary>
		/// <remarks>
		/// We call the cache "complete" if we know that every category in our
		/// taxonomy is in the cache. When the cache is <B>not</B> complete, and
		/// we can't find a category in the cache, we still need to look for it
		/// in the on-disk index; Therefore when the cache is not complete, we
		/// need to open a "reader" to the taxonomy index.
		/// The cache becomes incomplete if it was never filled with the existing
		/// categories, or if a put() to the cache ever returned true (meaning
		/// that some of the cached data was cleared).
		/// </remarks>
		private volatile bool cacheIsComplete;

		private volatile bool isClosed = false;

		private volatile TaxonomyIndexArrays taxoArrays;

		private volatile int nextID;

		// javadocs
		// javadocs
		// Records the taxonomy index epoch, updated on replaceTaxonomy as well.
		// even though lazily initialized, not volatile so that access to it is
		// faster. we keep a volatile boolean init instead.
		/// <summary>Reads the commit data from a Directory.</summary>
		/// <remarks>Reads the commit data from a Directory.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		private static IDictionary<string, string> ReadCommitData(Lucene.Net.Store.Directory
			 dir)
		{
			SegmentInfos infos = new SegmentInfos();
			infos.Read(dir);
			return infos.GetUserData();
		}

		/// <summary>Forcibly unlocks the taxonomy in the named directory.</summary>
		/// <remarks>
		/// Forcibly unlocks the taxonomy in the named directory.
		/// <P>
		/// Caution: this should only be used by failure recovery code, when it is
		/// known that no other process nor thread is in fact currently accessing
		/// this taxonomy.
		/// <P>
		/// This method is unnecessary if your
		/// <see cref="Lucene.Net.Store.Directory">Lucene.Net.Store.Directory</see>
		/// uses a
		/// <see cref="Lucene.Net.Store.NativeFSLockFactory">Lucene.Net.Store.NativeFSLockFactory
		/// 	</see>
		/// instead of the default
		/// <see cref="Lucene.Net.Store.SimpleFSLockFactory">Lucene.Net.Store.SimpleFSLockFactory
		/// 	</see>
		/// . When the "native" lock is used, a lock
		/// does not stay behind forever when the process using it dies.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public static void Unlock(Lucene.Net.Store.Directory directory)
		{
			IndexWriter.Unlock(directory);
		}

		/// <summary>Construct a Taxonomy writer.</summary>
		/// <remarks>Construct a Taxonomy writer.</remarks>
		/// <param name="directory">
		/// The
		/// <see cref="Lucene.Net.Store.Directory">Lucene.Net.Store.Directory</see>
		/// in which to store the taxonomy. Note that
		/// the taxonomy is written directly to that directory (not to a
		/// subdirectory of it).
		/// </param>
		/// <param name="openMode">
		/// Specifies how to open a taxonomy for writing: <code>APPEND</code>
		/// means open an existing index for append (failing if the index does
		/// not yet exist). <code>CREATE</code> means create a new index (first
		/// deleting the old one if it already existed).
		/// <code>APPEND_OR_CREATE</code> appends to an existing index if there
		/// is one, otherwise it creates a new index.
		/// </param>
		/// <param name="cache">
		/// A
		/// <see cref="Lucene.Net.Facet.Taxonomy.Writercache.TaxonomyWriterCache">Lucene.Net.Facet.Taxonomy.Writercache.TaxonomyWriterCache
		/// 	</see>
		/// implementation which determines
		/// the in-memory caching policy. See for example
		/// <see cref="Lucene.Net.Facet.Taxonomy.Writercache.LruTaxonomyWriterCache">Lucene.Net.Facet.Taxonomy.Writercache.LruTaxonomyWriterCache
		/// 	</see>
		/// and
		/// <see cref="Lucene.Net.Facet.Taxonomy.Writercache.Cl2oTaxonomyWriterCache">Lucene.Net.Facet.Taxonomy.Writercache.Cl2oTaxonomyWriterCache
		/// 	</see>
		/// .
		/// If null or missing,
		/// <see cref="DefaultTaxonomyWriterCache()">DefaultTaxonomyWriterCache()</see>
		/// is used.
		/// </param>
		/// <exception cref="Lucene.Net.Index.CorruptIndexException">if the taxonomy is corrupted.
		/// 	</exception>
		/// <exception cref="Lucene.Net.Store.LockObtainFailedException">
		/// if the taxonomy is locked by another writer. If it is known
		/// that no other concurrent writer is active, the lock might
		/// have been left around by an old dead process, and should be
		/// removed using
		/// <see cref="Unlock(Lucene.Net.Store.Directory)">Unlock(Lucene.Net.Store.Directory)
		/// 	</see>
		/// .
		/// </exception>
		/// <exception cref="System.IO.IOException">if another error occurred.</exception>
		public DirectoryTaxonomyWriter(Lucene.Net.Store.Directory directory, IndexWriterConfig.OpenMode
			 openMode, TaxonomyWriterCache cache)
		{
			dir = directory;
			IndexWriterConfig config = CreateIndexWriterConfig(openMode);
			indexWriter = OpenIndexWriter(dir, config);
			// verify (to some extent) that merge policy in effect would preserve category docids
			//HM:revisit
			// after we opened the writer, and the index is locked, it's safe to check
			// the commit data and read the index epoch
			openMode = config.GetOpenMode();
			if (!DirectoryReader.IndexExists(directory))
			{
				indexEpoch = 1;
			}
			else
			{
				string epochStr = null;
				IDictionary<string, string> commitData = ReadCommitData(directory);
				if (commitData != null)
				{
					epochStr = commitData.Get(INDEX_EPOCH);
				}
				// no commit data, or no epoch in it means an old taxonomy, so set its epoch to 1, for lack
				// of a better value.
				indexEpoch = epochStr == null ? 1 : long.Parse(epochStr, 16);
			}
			if (openMode == IndexWriterConfig.OpenMode.CREATE)
			{
				++indexEpoch;
			}
			FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
			ft.SetOmitNorms(true);
			parentStreamField = new Field(Consts.FIELD_PAYLOADS, parentStream, ft);
			fullPathField = new StringField(Consts.FULL, string.Empty, Field.Store.YES);
			nextID = indexWriter.MaxDoc();
			if (cache == null)
			{
				cache = DefaultTaxonomyWriterCache();
			}
			this.cache = cache;
			if (nextID == 0)
			{
				cacheIsComplete = true;
				// Make sure that the taxonomy always contain the root category
				// with category id 0.
				AddCategory(new FacetLabel());
			}
			else
			{
				// There are some categories on the disk, which we have not yet
				// read into the cache, and therefore the cache is incomplete.
				// We choose not to read all the categories into the cache now,
				// to avoid terrible performance when a taxonomy index is opened
				// to add just a single category. We will do it later, after we
				// notice a few cache misses.
				cacheIsComplete = false;
			}
		}

		/// <summary>Open internal index writer, which contains the taxonomy data.</summary>
		/// <remarks>
		/// Open internal index writer, which contains the taxonomy data.
		/// <p>
		/// Extensions may provide their own
		/// <see cref="Lucene.Net.Index.IndexWriter">Lucene.Net.Index.IndexWriter
		/// 	</see>
		/// implementation or instance.
		/// <br /><b>NOTE:</b> the instance this method returns will be closed upon calling
		/// to
		/// <see cref="Close()">Close()</see>
		/// .
		/// <br /><b>NOTE:</b> the merge policy in effect must not merge none adjacent segments. See
		/// comment in
		/// <see cref="CreateIndexWriterConfig(Lucene.Net.Index.IndexWriterConfig.OpenMode)
		/// 	">CreateIndexWriterConfig(Lucene.Net.Index.IndexWriterConfig.OpenMode)</see>
		/// for the logic behind this.
		/// </remarks>
		/// <seealso cref="CreateIndexWriterConfig(Lucene.Net.Index.IndexWriterConfig.OpenMode)
		/// 	">CreateIndexWriterConfig(Lucene.Net.Index.IndexWriterConfig.OpenMode)</seealso>
		/// <param name="directory">
		/// the
		/// <see cref="Lucene.Net.Store.Directory">Lucene.Net.Store.Directory</see>
		/// on top of which an
		/// <see cref="Lucene.Net.Index.IndexWriter">Lucene.Net.Index.IndexWriter
		/// 	</see>
		/// should be opened.
		/// </param>
		/// <param name="config">configuration for the internal index writer.</param>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual IndexWriter OpenIndexWriter(Lucene.Net.Store.Directory
			 directory, IndexWriterConfig config)
		{
			return new IndexWriter(directory, config);
		}

		/// <summary>
		/// Create the
		/// <see cref="Lucene.Net.Index.IndexWriterConfig">Lucene.Net.Index.IndexWriterConfig
		/// 	</see>
		/// that would be used for opening the internal index writer.
		/// <br />Extensions can configure the
		/// <see cref="Lucene.Net.Index.IndexWriter">Lucene.Net.Index.IndexWriter
		/// 	</see>
		/// as they see fit,
		/// including setting a
		/// <see cref="Lucene.Net.Index.MergeScheduler">merge-scheduler</see>
		/// , or
		/// <see cref="Lucene.Net.Index.IndexDeletionPolicy">deletion-policy</see>
		/// , different RAM size
		/// etc.<br />
		/// <br /><b>NOTE:</b> internal docids of the configured index must not be altered.
		/// For that, categories are never deleted from the taxonomy index.
		/// In addition, merge policy in effect must not merge none adjacent segments.
		/// </summary>
		/// <seealso cref="OpenIndexWriter(Lucene.Net.Store.Directory, Lucene.Net.Index.IndexWriterConfig)
		/// 	">OpenIndexWriter(Lucene.Net.Store.Directory, Lucene.Net.Index.IndexWriterConfig)
		/// 	</seealso>
		/// <param name="openMode">
		/// see
		/// <see cref="Lucene.Net.Index.IndexWriterConfig.OpenMode">Lucene.Net.Index.IndexWriterConfig.OpenMode
		/// 	</see>
		/// </param>
		protected internal virtual IndexWriterConfig CreateIndexWriterConfig(IndexWriterConfig.OpenMode
			 openMode)
		{
			// TODO: should we use a more optimized Codec, e.g. Pulsing (or write custom)?
			// The taxonomy has a unique structure, where each term is associated with one document
			// :Post-Release-Update-Version.LUCENE_XY:
			// Make sure we use a MergePolicy which always merges adjacent segments and thus
			// keeps the doc IDs ordered as well (this is crucial for the taxonomy index).
			return new IndexWriterConfig(Version.LUCENE_48, null).SetOpenMode(openMode).SetMergePolicy
				(new LogByteSizeMergePolicy());
		}

		/// <summary>
		/// Opens a
		/// <see cref="Lucene.Net.Index.ReaderManager">Lucene.Net.Index.ReaderManager
		/// 	</see>
		/// from the internal
		/// <see cref="Lucene.Net.Index.IndexWriter">Lucene.Net.Index.IndexWriter
		/// 	</see>
		/// .
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		private void InitReaderManager()
		{
			if (!initializedReaderManager)
			{
				lock (this)
				{
					// verify that the taxo-writer hasn't been closed on us.
					EnsureOpen();
					if (!initializedReaderManager)
					{
						readerManager = new ReaderManager(indexWriter, false);
						shouldRefreshReaderManager = false;
						initializedReaderManager = true;
					}
				}
			}
		}

		/// <summary>
		/// Creates a new instance with a default cache as defined by
		/// <see cref="DefaultTaxonomyWriterCache()">DefaultTaxonomyWriterCache()</see>
		/// .
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public DirectoryTaxonomyWriter(Lucene.Net.Store.Directory directory, IndexWriterConfig.OpenMode
			 openMode) : this(directory, openMode, DefaultTaxonomyWriterCache())
		{
		}

		/// <summary>
		/// Defines the default
		/// <see cref="Lucene.Net.Facet.Taxonomy.Writercache.TaxonomyWriterCache">Lucene.Net.Facet.Taxonomy.Writercache.TaxonomyWriterCache
		/// 	</see>
		/// to use in constructors
		/// which do not specify one.
		/// <P>
		/// The current default is
		/// <see cref="Lucene.Net.Facet.Taxonomy.Writercache.Cl2oTaxonomyWriterCache">Lucene.Net.Facet.Taxonomy.Writercache.Cl2oTaxonomyWriterCache
		/// 	</see>
		/// constructed
		/// with the parameters (1024, 0.15f, 3), i.e., the entire taxonomy is
		/// cached in memory while building it.
		/// </summary>
		public static TaxonomyWriterCache DefaultTaxonomyWriterCache()
		{
			return new Cl2oTaxonomyWriterCache(1024, 0.15f, 3);
		}

		/// <summary>
		/// Create this with
		/// <code>OpenMode.CREATE_OR_APPEND</code>
		/// .
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public DirectoryTaxonomyWriter(Lucene.Net.Store.Directory d) : this(d, IndexWriterConfig.OpenMode
			.CREATE_OR_APPEND)
		{
		}

		/// <summary>
		/// Frees used resources as well as closes the underlying
		/// <see cref="Lucene.Net.Index.IndexWriter">Lucene.Net.Index.IndexWriter
		/// 	</see>
		/// ,
		/// which commits whatever changes made to it to the underlying
		/// <see cref="Lucene.Net.Store.Directory">Lucene.Net.Store.Directory</see>
		/// .
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void Close()
		{
			lock (this)
			{
				if (!isClosed)
				{
					Commit();
					DoClose();
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void DoClose()
		{
			indexWriter.Close();
			isClosed = true;
			CloseResources();
		}

		/// <summary>A hook for extending classes to close additional resources that were used.
		/// 	</summary>
		/// <remarks>
		/// A hook for extending classes to close additional resources that were used.
		/// The default implementation closes the
		/// <see cref="Lucene.Net.Index.IndexReader">Lucene.Net.Index.IndexReader
		/// 	</see>
		/// as well as the
		/// <see cref="Lucene.Net.Facet.Taxonomy.Writercache.TaxonomyWriterCache">Lucene.Net.Facet.Taxonomy.Writercache.TaxonomyWriterCache
		/// 	</see>
		/// instances that were used. <br />
		/// <b>NOTE:</b> if you override this method, you should include a
		/// <code>super.closeResources()</code> call in your implementation.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual void CloseResources()
		{
			lock (this)
			{
				if (initializedReaderManager)
				{
					readerManager.Close();
					readerManager = null;
					initializedReaderManager = false;
				}
				if (cache != null)
				{
					cache.Close();
				}
			}
		}

		/// <summary>
		/// Look up the given category in the cache and/or the on-disk storage,
		/// returning the category's ordinal, or a negative number in case the
		/// category does not yet exist in the taxonomy.
		/// </summary>
		/// <remarks>
		/// Look up the given category in the cache and/or the on-disk storage,
		/// returning the category's ordinal, or a negative number in case the
		/// category does not yet exist in the taxonomy.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual int FindCategory(FacetLabel categoryPath)
		{
			lock (this)
			{
				// If we can find the category in the cache, or we know the cache is
				// complete, we can return the response directly from it
				int res = cache.Get(categoryPath);
				if (res >= 0 || cacheIsComplete)
				{
					return res;
				}
				cacheMisses.IncrementAndGet();
				// After a few cache misses, it makes sense to read all the categories
				// from disk and into the cache. The reason not to do this on the first
				// cache miss (or even when opening the writer) is that it will
				// significantly slow down the case when a taxonomy is opened just to
				// add one category. The idea only spending a long time on reading
				// after enough time was spent on cache misses is known as an "online
				// algorithm".
				PerhapsFillCache();
				res = cache.Get(categoryPath);
				if (res >= 0 || cacheIsComplete)
				{
					// if after filling the cache from the info on disk, the category is in it
					// or the cache is complete, return whatever cache.get returned.
					return res;
				}
				// if we get here, it means the category is not in the cache, and it is not
				// complete, and therefore we must look for the category on disk.
				// We need to get an answer from the on-disk index.
				InitReaderManager();
				int doc = -1;
				DirectoryReader reader = readerManager.Acquire();
				try
				{
					BytesRef catTerm = new BytesRef(FacetsConfig.PathToString(categoryPath.components
						, categoryPath.length));
					TermsEnum termsEnum = null;
					// reuse
					DocsEnum docs = null;
					// reuse
					foreach (AtomicReaderContext ctx in reader.Leaves())
					{
						Terms terms = ((AtomicReader)ctx.Reader()).Terms(Consts.FULL);
						if (terms != null)
						{
							termsEnum = terms.Iterator(termsEnum);
							if (termsEnum.SeekExact(catTerm))
							{
								// liveDocs=null because the taxonomy has no deletes
								docs = termsEnum.Docs(null, docs, 0);
								// if the term was found, we know it has exactly one document.
								doc = docs.NextDoc() + ctx.docBase;
								break;
							}
						}
					}
				}
				finally
				{
					readerManager.Release(reader);
				}
				if (doc > 0)
				{
					AddToCache(categoryPath, doc);
				}
				return doc;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual int AddCategory(FacetLabel categoryPath)
		{
			EnsureOpen();
			// check the cache outside the synchronized block. this results in better
			// concurrency when categories are there.
			int res = cache.Get(categoryPath);
			if (res < 0)
			{
				// the category is not in the cache - following code cannot be executed in parallel.
				lock (this)
				{
					res = FindCategory(categoryPath);
					if (res < 0)
					{
						// This is a new category, and we need to insert it into the index
						// (and the cache). Actually, we might also need to add some of
						// the category's ancestors before we can add the category itself
						// (while keeping the invariant that a parent is always added to
						// the taxonomy before its child). internalAddCategory() does all
						// this recursively
						res = InternalAddCategory(categoryPath);
					}
				}
			}
			return res;
		}

		/// <summary>
		/// Add a new category into the index (and the cache), and return its new
		/// ordinal.
		/// </summary>
		/// <remarks>
		/// Add a new category into the index (and the cache), and return its new
		/// ordinal.
		/// <p>
		/// Actually, we might also need to add some of the category's ancestors
		/// before we can add the category itself (while keeping the invariant that a
		/// parent is always added to the taxonomy before its child). We do this by
		/// recursion.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		private int InternalAddCategory(FacetLabel cp)
		{
			// Find our parent's ordinal (recursively adding the parent category
			// to the taxonomy if it's not already there). Then add the parent
			// ordinal as payloads (rather than a stored field; payloads can be
			// more efficiently read into memory in bulk by LuceneTaxonomyReader)
			int parent;
			if (cp.length > 1)
			{
				FacetLabel parentPath = cp.Subpath(cp.length - 1);
				parent = FindCategory(parentPath);
				if (parent < 0)
				{
					parent = InternalAddCategory(parentPath);
				}
			}
			else
			{
				if (cp.length == 1)
				{
					parent = TaxonomyReader.ROOT_ORDINAL;
				}
				else
				{
					parent = TaxonomyReader.INVALID_ORDINAL;
				}
			}
			int id = AddCategoryDocument(cp, parent);
			return id;
		}

		/// <summary>
		/// Verifies that this instance wasn't closed, or throws
		/// <see cref="Lucene.Net.Store.AlreadyClosedException">Lucene.Net.Store.AlreadyClosedException
		/// 	</see>
		/// if it is.
		/// </summary>
		protected internal void EnsureOpen()
		{
			if (isClosed)
			{
				throw new AlreadyClosedException("The taxonomy writer has already been closed");
			}
		}

		/// <summary>
		/// Note that the methods calling addCategoryDocument() are synchornized, so
		/// this method is effectively synchronized as well.
		/// </summary>
		/// <remarks>
		/// Note that the methods calling addCategoryDocument() are synchornized, so
		/// this method is effectively synchronized as well.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		private int AddCategoryDocument(FacetLabel categoryPath, int parent)
		{
			// Before Lucene 2.9, position increments >=0 were supported, so we
			// added 1 to parent to allow the parent -1 (the parent of the root).
			// Unfortunately, starting with Lucene 2.9, after LUCENE-1542, this is
			// no longer enough, since 0 is not encoded consistently either (see
			// comment in SinglePositionTokenStream). But because we must be
			// backward-compatible with existing indexes, we can't just fix what
			// we write here (e.g., to write parent+2), and need to do a workaround
			// in the reader (which knows that anyway only category 0 has a parent
			// -1).    
			parentStream.Set(Math.Max(parent + 1, 1));
			Lucene.Net.Document.Document d = new Lucene.Net.Document.Document();
			d.Add(parentStreamField);
			fullPathField.SetStringValue(FacetsConfig.PathToString(categoryPath.components, categoryPath
				.length));
			d.Add(fullPathField);
			// Note that we do no pass an Analyzer here because the fields that are
			// added to the Document are untokenized or contains their own TokenStream.
			// Therefore the IndexWriter's Analyzer has no effect.
			indexWriter.AddDocument(d);
			int id = nextID++;
			// added a category document, mark that ReaderManager is not up-to-date
			shouldRefreshReaderManager = true;
			// also add to the parent array
			taxoArrays = GetTaxoArrays().Add(id, parent);
			// NOTE: this line must be executed last, or else the cache gets updated
			// before the parents array (LUCENE-4596)
			AddToCache(categoryPath, id);
			return id;
		}

		private class SinglePositionTokenStream : TokenStream
		{
			private CharTermAttribute termAtt;

			private PositionIncrementAttribute posIncrAtt;

			private bool returned;

			private int val;

			private readonly string word;

			public SinglePositionTokenStream(string word)
			{
				termAtt = AddAttribute<CharTermAttribute>();
				posIncrAtt = AddAttribute<PositionIncrementAttribute>();
				this.word = word;
				returned = true;
			}

			/// <summary>Set the value we want to keep, as the position increment.</summary>
			/// <remarks>
			/// Set the value we want to keep, as the position increment.
			/// Note that when TermPositions.nextPosition() is later used to
			/// retrieve this value, val-1 will be returned, not val.
			/// <P>
			/// IMPORTANT NOTE: Before Lucene 2.9, val&gt;=0 were safe (for val==0,
			/// the retrieved position would be -1). But starting with Lucene 2.9,
			/// this unfortunately changed, and only val&gt;0 are safe. val=0 can
			/// still be used, but don't count on the value you retrieve later
			/// (it could be 0 or -1, depending on circumstances or versions).
			/// This change is described in Lucene's JIRA: LUCENE-1542.
			/// </remarks>
			public virtual void Set(int val)
			{
				this.val = val;
				returned = false;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override bool IncrementToken()
			{
				if (returned)
				{
					return false;
				}
				ClearAttributes();
				posIncrAtt.SetPositionIncrement(val);
				termAtt.SetEmpty();
				termAtt.Append(word);
				returned = true;
				return true;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void AddToCache(FacetLabel categoryPath, int id)
		{
			if (cache.Put(categoryPath, id))
			{
				// If cache.put() returned true, it means the cache was limited in
				// size, became full, and parts of it had to be evicted. It is
				// possible that a relatively-new category that isn't yet visible
				// to our 'reader' was evicted, and therefore we must now refresh 
				// the reader.
				RefreshReaderManager();
				cacheIsComplete = false;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void RefreshReaderManager()
		{
			lock (this)
			{
				// this method is synchronized since it cannot happen concurrently with
				// addCategoryDocument -- when this method returns, we must know that the
				// reader manager's state is current. also, it sets shouldRefresh to false, 
				// and this cannot overlap with addCatDoc too.
				// NOTE: since this method is sync'ed, it can call maybeRefresh, instead of
				// maybeRefreshBlocking. If ever this is changed, make sure to change the
				// call too.
				if (shouldRefreshReaderManager && initializedReaderManager)
				{
					readerManager.MaybeRefresh();
					shouldRefreshReaderManager = false;
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void Commit()
		{
			lock (this)
			{
				EnsureOpen();
				// LUCENE-4972: if we always call setCommitData, we create empty commits
				string epochStr = indexWriter.GetCommitData().Get(INDEX_EPOCH);
				if (epochStr == null || long.Parse(epochStr, 16) != indexEpoch)
				{
					indexWriter.SetCommitData(CombinedCommitData(indexWriter.GetCommitData()));
				}
				indexWriter.Commit();
			}
		}

		/// <summary>Combine original user data with the taxonomy epoch.</summary>
		/// <remarks>Combine original user data with the taxonomy epoch.</remarks>
		private IDictionary<string, string> CombinedCommitData(IDictionary<string, string
			> commitData)
		{
			IDictionary<string, string> m = new Dictionary<string, string>();
			if (commitData != null)
			{
				m.PutAll(commitData);
			}
			m.Put(INDEX_EPOCH, System.Convert.ToString(indexEpoch, 16));
			return m;
		}

		public virtual void SetCommitData(IDictionary<string, string> commitUserData)
		{
			indexWriter.SetCommitData(CombinedCommitData(commitUserData));
		}

		public virtual IDictionary<string, string> GetCommitData()
		{
			return CombinedCommitData(indexWriter.GetCommitData());
		}

		/// <summary>prepare most of the work needed for a two-phase commit.</summary>
		/// <remarks>
		/// prepare most of the work needed for a two-phase commit.
		/// See
		/// <see cref="Lucene.Net.Index.IndexWriter.PrepareCommit()">Lucene.Net.Index.IndexWriter.PrepareCommit()
		/// 	</see>
		/// .
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void PrepareCommit()
		{
			lock (this)
			{
				EnsureOpen();
				// LUCENE-4972: if we always call setCommitData, we create empty commits
				string epochStr = indexWriter.GetCommitData().Get(INDEX_EPOCH);
				if (epochStr == null || long.Parse(epochStr, 16) != indexEpoch)
				{
					indexWriter.SetCommitData(CombinedCommitData(indexWriter.GetCommitData()));
				}
				indexWriter.PrepareCommit();
			}
		}

		public virtual int GetSize()
		{
			EnsureOpen();
			return nextID;
		}

		/// <summary>
		/// Set the number of cache misses before an attempt is made to read the entire
		/// taxonomy into the in-memory cache.
		/// </summary>
		/// <remarks>
		/// Set the number of cache misses before an attempt is made to read the entire
		/// taxonomy into the in-memory cache.
		/// <p>
		/// This taxonomy writer holds an in-memory cache of recently seen categories
		/// to speed up operation. On each cache-miss, the on-disk index needs to be
		/// consulted. When an existing taxonomy is opened, a lot of slow disk reads
		/// like that are needed until the cache is filled, so it is more efficient to
		/// read the entire taxonomy into memory at once. We do this complete read
		/// after a certain number (defined by this method) of cache misses.
		/// <p>
		/// If the number is set to
		/// <code>0</code>
		/// , the entire taxonomy is read into the
		/// cache on first use, without fetching individual categories first.
		/// <p>
		/// NOTE: it is assumed that this method is called immediately after the
		/// taxonomy writer has been created.
		/// </remarks>
		public virtual void SetCacheMissesUntilFill(int i)
		{
			EnsureOpen();
			cacheMissesUntilFill = i;
		}

		// we need to guarantee that if several threads call this concurrently, only
		// one executes it, and after it returns, the cache is updated and is either
		// complete or not.
		/// <exception cref="System.IO.IOException"></exception>
		private void PerhapsFillCache()
		{
			lock (this)
			{
				if (cacheMisses.Get() < cacheMissesUntilFill)
				{
					return;
				}
				if (!shouldFillCache)
				{
					// we already filled the cache once, there's no need to re-fill it
					return;
				}
				shouldFillCache = false;
				InitReaderManager();
				bool aborted = false;
				DirectoryReader reader = readerManager.Acquire();
				try
				{
					TermsEnum termsEnum = null;
					DocsEnum docsEnum = null;
					foreach (AtomicReaderContext ctx in reader.Leaves())
					{
						Terms terms = ((AtomicReader)ctx.Reader()).Terms(Consts.FULL);
						if (terms != null)
						{
							// cannot really happen, but be on the safe side
							termsEnum = terms.Iterator(termsEnum);
							while (termsEnum.Next() != null)
							{
								if (!cache.IsFull())
								{
									BytesRef t = termsEnum.Term();
									// Since we guarantee uniqueness of categories, each term has exactly
									// one document. Also, since we do not allow removing categories (and
									// hence documents), there are no deletions in the index. Therefore, it
									// is sufficient to call next(), and then doc(), exactly once with no
									// 'validation' checks.
									FacetLabel cp = new FacetLabel(FacetsConfig.StringToPath(t.Utf8ToString()));
									docsEnum = termsEnum.Docs(null, docsEnum, DocsEnum.FLAG_NONE);
									bool res = cache.Put(cp, docsEnum.NextDoc() + ctx.docBase);
								}
								else
								{
									//HM:revisit
									//assert !res : "entries should not have been evicted from the cache";
									// the cache is full and the next put() will evict entries from it, therefore abort the iteration.
									aborted = true;
									break;
								}
							}
						}
						if (aborted)
						{
							break;
						}
					}
				}
				finally
				{
					readerManager.Release(reader);
				}
				cacheIsComplete = !aborted;
				if (cacheIsComplete)
				{
					lock (this)
					{
						// everything is in the cache, so no need to keep readerManager open.
						// this block is executed in a sync block so that it works well with
						// initReaderManager called in parallel.
						readerManager.Close();
						readerManager = null;
						initializedReaderManager = false;
					}
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private TaxonomyIndexArrays GetTaxoArrays()
		{
			if (taxoArrays == null)
			{
				lock (this)
				{
					if (taxoArrays == null)
					{
						InitReaderManager();
						DirectoryReader reader = readerManager.Acquire();
						try
						{
							// according to Java Concurrency, this might perform better on some
							// JVMs, since the object initialization doesn't happen on the
							// volatile member.
							TaxonomyIndexArrays tmpArrays = new TaxonomyIndexArrays(reader);
							taxoArrays = tmpArrays;
						}
						finally
						{
							readerManager.Release(reader);
						}
					}
				}
			}
			return taxoArrays;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual int GetParent(int ordinal)
		{
			EnsureOpen();
			// Note: the following if() just enforces that a user can never ask
			// for the parent of a nonexistant category - even if the parent array
			// was allocated bigger than it really needs to be.
			if (ordinal >= nextID)
			{
				throw new IndexOutOfRangeException("requested ordinal is bigger than the largest ordinal in the taxonomy"
					);
			}
			int[] parents = GetTaxoArrays().Parents();
			//HM:revisit
			//assert ordinal < parents.length : "requested ordinal (" + ordinal + "); parents.length (" + parents.length + ") !";
			return parents[ordinal];
		}

		/// <summary>
		/// Takes the categories from the given taxonomy directory, and adds the
		/// missing ones to this taxonomy.
		/// </summary>
		/// <remarks>
		/// Takes the categories from the given taxonomy directory, and adds the
		/// missing ones to this taxonomy. Additionally, it fills the given
		/// <see cref="OrdinalMap">OrdinalMap</see>
		/// with a mapping from the original ordinal to the new
		/// ordinal.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void AddTaxonomy(Lucene.Net.Store.Directory taxoDir, DirectoryTaxonomyWriter.OrdinalMap
			 map)
		{
			EnsureOpen();
			DirectoryReader r = DirectoryReader.Open(taxoDir);
			try
			{
				int size = r.NumDocs();
				DirectoryTaxonomyWriter.OrdinalMap ordinalMap = map;
				ordinalMap.SetSize(size);
				int @base = 0;
				TermsEnum te = null;
				DocsEnum docs = null;
				foreach (AtomicReaderContext ctx in r.Leaves())
				{
					AtomicReader ar = ((AtomicReader)ctx.Reader());
					Terms terms = ar.Terms(Consts.FULL);
					te = terms.Iterator(te);
					while (te.Next() != null)
					{
						FacetLabel cp = new FacetLabel(FacetsConfig.StringToPath(te.Term().Utf8ToString()
							));
						int ordinal = AddCategory(cp);
						docs = te.Docs(null, docs, DocsEnum.FLAG_NONE);
						ordinalMap.AddMapping(docs.NextDoc() + @base, ordinal);
					}
					@base += ar.MaxDoc();
				}
				// no deletions, so we're ok
				ordinalMap.AddDone();
			}
			finally
			{
				r.Close();
			}
		}

		/// <summary>
		/// Mapping from old ordinal to new ordinals, used when merging indexes
		/// wit separate taxonomies.
		/// </summary>
		/// <remarks>
		/// Mapping from old ordinal to new ordinals, used when merging indexes
		/// wit separate taxonomies.
		/// <p>
		/// addToTaxonomies() merges one or more taxonomies into the given taxonomy
		/// (this). An OrdinalMap is filled for each of the added taxonomies,
		/// containing the new ordinal (in the merged taxonomy) of each of the
		/// categories in the old taxonomy.
		/// <P>
		/// There exist two implementations of OrdinalMap: MemoryOrdinalMap and
		/// DiskOrdinalMap. As their names suggest, the former keeps the map in
		/// memory and the latter in a temporary disk file. Because these maps will
		/// later be needed one by one (to remap the counting lists), not all at the
		/// same time, it is recommended to put the first taxonomy's map in memory,
		/// and all the rest on disk (later to be automatically read into memory one
		/// by one, when needed).
		/// </remarks>
		public interface OrdinalMap
		{
			/// <summary>Set the size of the map.</summary>
			/// <remarks>
			/// Set the size of the map. This MUST be called before addMapping().
			/// It is assumed (but not verified) that addMapping() will then be
			/// called exactly 'size' times, with different origOrdinals between 0
			/// and size-1.
			/// </remarks>
			/// <exception cref="System.IO.IOException"></exception>
			void SetSize(int size);

			/// <summary>Record a mapping.</summary>
			/// <remarks>Record a mapping.</remarks>
			/// <exception cref="System.IO.IOException"></exception>
			void AddMapping(int origOrdinal, int newOrdinal);

			/// <summary>Call addDone() to say that all addMapping() have been done.</summary>
			/// <remarks>
			/// Call addDone() to say that all addMapping() have been done.
			/// In some implementations this might free some resources.
			/// </remarks>
			/// <exception cref="System.IO.IOException"></exception>
			void AddDone();

			/// <summary>
			/// Return the map from the taxonomy's original (consecutive) ordinals
			/// to the new taxonomy's ordinals.
			/// </summary>
			/// <remarks>
			/// Return the map from the taxonomy's original (consecutive) ordinals
			/// to the new taxonomy's ordinals. If the map has to be read from disk
			/// and ordered appropriately, it is done when getMap() is called.
			/// getMap() should only be called once, and only when the map is actually
			/// needed. Calling it will also free all resources that the map might
			/// be holding (such as temporary disk space), other than the returned int[].
			/// </remarks>
			/// <exception cref="System.IO.IOException"></exception>
			int[] GetMap();
		}

		/// <summary>
		/// <see cref="OrdinalMap">OrdinalMap</see>
		/// maintained in memory
		/// </summary>
		public sealed class MemoryOrdinalMap : DirectoryTaxonomyWriter.OrdinalMap
		{
			internal int[] map;

			/// <summary>Sole constructor.</summary>
			/// <remarks>Sole constructor.</remarks>
			public MemoryOrdinalMap()
			{
			}

			public void SetSize(int taxonomySize)
			{
				map = new int[taxonomySize];
			}

			public void AddMapping(int origOrdinal, int newOrdinal)
			{
				map[origOrdinal] = newOrdinal;
			}

			public void AddDone()
			{
			}

			public int[] GetMap()
			{
				return map;
			}
		}

		/// <summary>
		/// <see cref="OrdinalMap">OrdinalMap</see>
		/// maintained on file system
		/// </summary>
		public sealed class DiskOrdinalMap : DirectoryTaxonomyWriter.OrdinalMap
		{
			internal FilePath tmpfile;

			internal DataOutputStream @out;

			/// <summary>Sole constructor.</summary>
			/// <remarks>Sole constructor.</remarks>
			/// <exception cref="System.IO.FileNotFoundException"></exception>
			public DiskOrdinalMap(FilePath tmpfile)
			{
				this.tmpfile = tmpfile;
				@out = new DataOutputStream(new BufferedOutputStream(new FileOutputStream(tmpfile
					)));
			}

			/// <exception cref="System.IO.IOException"></exception>
			public void AddMapping(int origOrdinal, int newOrdinal)
			{
				@out.WriteInt(origOrdinal);
				@out.WriteInt(newOrdinal);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public void SetSize(int taxonomySize)
			{
				@out.WriteInt(taxonomySize);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public void AddDone()
			{
				if (@out != null)
				{
					@out.Close();
					@out = null;
				}
			}

			internal int[] map = null;

			/// <exception cref="System.IO.IOException"></exception>
			public int[] GetMap()
			{
				if (map != null)
				{
					return map;
				}
				AddDone();
				// in case this wasn't previously called
				DataInputStream @in = new DataInputStream(new BufferedInputStream(new FileInputStream
					(tmpfile)));
				map = new int[@in.ReadInt()];
				// NOTE: The current code assumes here that the map is complete,
				// i.e., every ordinal gets one and exactly one value. Otherwise,
				// we may run into an EOF here, or vice versa, not read everything.
				for (int i = 0; i < map.Length; i++)
				{
					int origordinal = @in.ReadInt();
					int newordinal = @in.ReadInt();
					map[origordinal] = newordinal;
				}
				@in.Close();
				// Delete the temporary file, which is no longer needed.
				if (!tmpfile.Delete())
				{
					tmpfile.DeleteOnExit();
				}
				return map;
			}
		}

		/// <summary>Rollback changes to the taxonomy writer and closes the instance.</summary>
		/// <remarks>
		/// Rollback changes to the taxonomy writer and closes the instance. Following
		/// this method the instance becomes unusable (calling any of its API methods
		/// will yield an
		/// <see cref="Lucene.Net.Store.AlreadyClosedException">Lucene.Net.Store.AlreadyClosedException
		/// 	</see>
		/// ).
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void Rollback()
		{
			lock (this)
			{
				EnsureOpen();
				indexWriter.Rollback();
				DoClose();
			}
		}

		/// <summary>Replaces the current taxonomy with the given one.</summary>
		/// <remarks>
		/// Replaces the current taxonomy with the given one. This method should
		/// generally be called in conjunction with
		/// <see cref="Lucene.Net.Index.IndexWriter.AddIndexes(Lucene.Net.Store.Directory[])
		/// 	">Lucene.Net.Index.IndexWriter.AddIndexes(Lucene.Net.Store.Directory[])
		/// 	</see>
		/// to replace both the taxonomy
		/// as well as the search index content.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void ReplaceTaxonomy(Lucene.Net.Store.Directory taxoDir)
		{
			lock (this)
			{
				// replace the taxonomy by doing IW optimized operations
				indexWriter.DeleteAll();
				indexWriter.AddIndexes(taxoDir);
				shouldRefreshReaderManager = true;
				InitReaderManager();
				// ensure that it's initialized
				RefreshReaderManager();
				nextID = indexWriter.MaxDoc();
				taxoArrays = null;
				// must nullify so that it's re-computed next time it's needed
				// need to clear the cache, so that addCategory won't accidentally return
				// old categories that are in the cache.
				cache.Clear();
				cacheIsComplete = false;
				shouldFillCache = true;
				cacheMisses.Set(0);
				// update indexEpoch as a taxonomy replace is just like it has be recreated
				++indexEpoch;
			}
		}

		/// <summary>
		/// Returns the
		/// <see cref="Lucene.Net.Store.Directory">Lucene.Net.Store.Directory</see>
		/// of this taxonomy writer.
		/// </summary>
		public virtual Lucene.Net.Store.Directory GetDirectory()
		{
			return dir;
		}

		/// <summary>
		/// Used by
		/// <see cref="DirectoryTaxonomyReader">DirectoryTaxonomyReader</see>
		/// to support NRT.
		/// <p>
		/// <b>NOTE:</b> you should not use the obtained
		/// <see cref="Lucene.Net.Index.IndexWriter">Lucene.Net.Index.IndexWriter
		/// 	</see>
		/// in any
		/// way, other than opening an IndexReader on it, or otherwise, the taxonomy
		/// index may become corrupt!
		/// </summary>
		internal IndexWriter GetInternalIndexWriter()
		{
			return indexWriter;
		}

		/// <summary>
		/// Expert: returns current index epoch, if this is a
		/// near-real-time reader.
		/// </summary>
		/// <remarks>
		/// Expert: returns current index epoch, if this is a
		/// near-real-time reader.  Used by
		/// <see cref="DirectoryTaxonomyReader">DirectoryTaxonomyReader</see>
		/// to support NRT.
		/// </remarks>
		/// <lucene.internal></lucene.internal>
		public long GetTaxonomyEpoch()
		{
			return indexEpoch;
		}
	}
}
