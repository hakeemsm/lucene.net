/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Facet.Taxonomy.Directory;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Facet.Taxonomy
{
	/// <summary>
	/// Manages near-real-time reopen of both an IndexSearcher
	/// and a TaxonomyReader.
	/// </summary>
	/// <remarks>
	/// Manages near-real-time reopen of both an IndexSearcher
	/// and a TaxonomyReader.
	/// <p><b>NOTE</b>: If you call
	/// <see cref="Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyWriter.ReplaceTaxonomy(Lucene.Net.Store.Directory)
	/// 	">Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyWriter.ReplaceTaxonomy(Lucene.Net.Store.Directory)
	/// 	</see>
	/// then you must
	/// open a new
	/// <code>SearcherTaxonomyManager</code>
	/// afterwards.
	/// </remarks>
	public class SearcherTaxonomyManager : ReferenceManager<SearcherTaxonomyManager.SearcherAndTaxonomy
		>
	{
		/// <summary>
		/// Holds a matched pair of
		/// <see cref="Lucene.Net.Search.IndexSearcher">Lucene.Net.Search.IndexSearcher
		/// 	</see>
		/// and
		/// <see cref="TaxonomyReader">TaxonomyReader</see>
		/// 
		/// </summary>
		public class SearcherAndTaxonomy
		{
			/// <summary>
			/// Point-in-time
			/// <see cref="Lucene.Net.Search.IndexSearcher">Lucene.Net.Search.IndexSearcher
			/// 	</see>
			/// .
			/// </summary>
			public readonly IndexSearcher searcher;

			/// <summary>
			/// Matching point-in-time
			/// <see cref="Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyReader">Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyReader
			/// 	</see>
			/// .
			/// </summary>
			public readonly DirectoryTaxonomyReader taxonomyReader;

			/// <summary>Create a SearcherAndTaxonomy</summary>
			public SearcherAndTaxonomy(IndexSearcher searcher, DirectoryTaxonomyReader taxonomyReader
				)
			{
				this.searcher = searcher;
				this.taxonomyReader = taxonomyReader;
			}
		}

		private readonly SearcherFactory searcherFactory;

		private readonly long taxoEpoch;

		private readonly DirectoryTaxonomyWriter taxoWriter;

		/// <summary>
		/// Creates near-real-time searcher and taxonomy reader
		/// from the corresponding writers.
		/// </summary>
		/// <remarks>
		/// Creates near-real-time searcher and taxonomy reader
		/// from the corresponding writers.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public SearcherTaxonomyManager(IndexWriter writer, bool applyAllDeletes, SearcherFactory
			 searcherFactory, DirectoryTaxonomyWriter taxoWriter)
		{
			if (searcherFactory == null)
			{
				searcherFactory = new SearcherFactory();
			}
			this.searcherFactory = searcherFactory;
			this.taxoWriter = taxoWriter;
			DirectoryTaxonomyReader taxoReader = new DirectoryTaxonomyReader(taxoWriter);
			current = new SearcherTaxonomyManager.SearcherAndTaxonomy(SearcherManager.GetSearcher
				(searcherFactory, DirectoryReader.Open(writer, applyAllDeletes)), taxoReader);
			this.taxoEpoch = taxoWriter.GetTaxonomyEpoch();
		}

		/// <summary>Creates search and taxonomy readers over the corresponding directories.</summary>
		/// <remarks>
		/// Creates search and taxonomy readers over the corresponding directories.
		/// <p>
		/// <b>NOTE:</b> you should only use this constructor if you commit and call
		/// <see cref="Lucene.Net.Search.ReferenceManager{G}.MaybeRefresh()">Lucene.Net.Search.ReferenceManager&lt;G&gt;.MaybeRefresh()
		/// 	</see>
		/// in the same thread. Otherwise it could lead to an
		/// unsync'd
		/// <see cref="Lucene.Net.Search.IndexSearcher">Lucene.Net.Search.IndexSearcher
		/// 	</see>
		/// and
		/// <see cref="TaxonomyReader">TaxonomyReader</see>
		/// pair.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public SearcherTaxonomyManager(Lucene.Net.Store.Directory indexDir, Lucene.Net.Store.Directory
			 taxoDir, SearcherFactory searcherFactory)
		{
			if (searcherFactory == null)
			{
				searcherFactory = new SearcherFactory();
			}
			this.searcherFactory = searcherFactory;
			DirectoryTaxonomyReader taxoReader = new DirectoryTaxonomyReader(taxoDir);
			current = new SearcherTaxonomyManager.SearcherAndTaxonomy(SearcherManager.GetSearcher
				(searcherFactory, DirectoryReader.Open(indexDir)), taxoReader);
			this.taxoWriter = null;
			taxoEpoch = -1;
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected override void DecRef(SearcherTaxonomyManager.SearcherAndTaxonomy @ref)
		{
			@ref.searcher.GetIndexReader().DecRef();
			// This decRef can fail, and then in theory we should
			// tryIncRef the searcher to put back the ref count
			// ... but 1) the below decRef should only fail because
			// it decRef'd to 0 and closed and hit some IOException
			// during close, in which case 2) very likely the
			// searcher was also just closed by the above decRef and
			// a tryIncRef would fail:
			@ref.taxonomyReader.DecRef();
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected override bool TryIncRef(SearcherTaxonomyManager.SearcherAndTaxonomy @ref
			)
		{
			if (@ref.searcher.GetIndexReader().TryIncRef())
			{
				if (@ref.taxonomyReader.TryIncRef())
				{
					return true;
				}
				else
				{
					@ref.searcher.GetIndexReader().DecRef();
				}
			}
			return false;
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected override SearcherTaxonomyManager.SearcherAndTaxonomy RefreshIfNeeded(SearcherTaxonomyManager.SearcherAndTaxonomy
			 @ref)
		{
			// Must re-open searcher first, otherwise we may get a
			// new reader that references ords not yet known to the
			// taxonomy reader:
			IndexReader r = @ref.searcher.GetIndexReader();
			IndexReader newReader = DirectoryReader.OpenIfChanged((DirectoryReader)r);
			if (newReader == null)
			{
				return null;
			}
			else
			{
				DirectoryTaxonomyReader tr = TaxonomyReader.OpenIfChanged(@ref.taxonomyReader);
				if (tr == null)
				{
					@ref.taxonomyReader.IncRef();
					tr = @ref.taxonomyReader;
				}
				else
				{
					if (taxoWriter != null && taxoWriter.GetTaxonomyEpoch() != taxoEpoch)
					{
						IOUtils.Close(newReader, tr);
						throw new InvalidOperationException("DirectoryTaxonomyWriter.replaceTaxonomy was called, which is not allowed when using SearcherTaxonomyManager"
							);
					}
				}
				return new SearcherTaxonomyManager.SearcherAndTaxonomy(SearcherManager.GetSearcher
					(searcherFactory, newReader), tr);
			}
		}

		protected override int GetRefCount(SearcherTaxonomyManager.SearcherAndTaxonomy reference
			)
		{
			return reference.searcher.GetIndexReader().GetRefCount();
		}
	}
}
