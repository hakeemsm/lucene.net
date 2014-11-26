/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Facet.Taxonomy.Directory;
using Lucene.Net.Facet.Taxonomy.Writercache;
using Lucene.Net.Index;
using Lucene.Net.Replicator;
using Lucene.Net.Store;
using Sharpen;

namespace Lucene.Net.Replicator
{
	/// <summary>
	/// A
	/// <see cref="Revision">Revision</see>
	/// of a single index and taxonomy index files which comprises
	/// the list of files from both indexes. This revision should be used whenever a
	/// pair of search and taxonomy indexes need to be replicated together to
	/// guarantee consistency of both on the replicating (client) side.
	/// </summary>
	/// <seealso cref="IndexRevision">IndexRevision</seealso>
	/// <lucene.experimental></lucene.experimental>
	public class IndexAndTaxonomyRevision : Revision
	{
		/// <summary>
		/// A
		/// <see cref="Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyWriter">Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyWriter
		/// 	</see>
		/// which sets the underlying
		/// <see cref="Lucene.Net.Index.IndexWriter">Lucene.Net.Index.IndexWriter
		/// 	</see>
		/// 's
		/// <see cref="Lucene.Net.Index.IndexDeletionPolicy">Lucene.Net.Index.IndexDeletionPolicy
		/// 	</see>
		/// to
		/// <see cref="Lucene.Net.Index.SnapshotDeletionPolicy">Lucene.Net.Index.SnapshotDeletionPolicy
		/// 	</see>
		/// .
		/// </summary>
		public sealed class SnapshotDirectoryTaxonomyWriter : DirectoryTaxonomyWriter
		{
			private SnapshotDeletionPolicy sdp;

			private IndexWriter writer;

			/// <seealso cref="Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyWriter.DirectoryTaxonomyWriter(Lucene.Net.Store.Directory, Lucene.Net.Index.IndexWriterConfig.OpenMode, Lucene.Net.Facet.Taxonomy.Writercache.TaxonomyWriterCache)
			/// 	">Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyWriter.DirectoryTaxonomyWriter(Lucene.Net.Store.Directory, Lucene.Net.Index.IndexWriterConfig.OpenMode, Lucene.Net.Facet.Taxonomy.Writercache.TaxonomyWriterCache)
			/// 	</seealso>
			/// <exception cref="System.IO.IOException"></exception>
			public SnapshotDirectoryTaxonomyWriter(Lucene.Net.Store.Directory directory
				, IndexWriterConfig.OpenMode openMode, TaxonomyWriterCache cache) : base(directory
				, openMode, cache)
			{
			}

			/// <seealso cref="Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyWriter.DirectoryTaxonomyWriter(Lucene.Net.Store.Directory, Lucene.Net.Index.IndexWriterConfig.OpenMode)
			/// 	"></seealso>
			/// <exception cref="System.IO.IOException"></exception>
			public SnapshotDirectoryTaxonomyWriter(Lucene.Net.Store.Directory directory
				, IndexWriterConfig.OpenMode openMode) : base(directory, openMode)
			{
			}

			/// <seealso cref="Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyWriter.DirectoryTaxonomyWriter(Lucene.Net.Store.Directory)
			/// 	"></seealso>
			/// <exception cref="System.IO.IOException"></exception>
			public SnapshotDirectoryTaxonomyWriter(Lucene.Net.Store.Directory d) : base
				(d)
			{
			}

			protected override IndexWriterConfig CreateIndexWriterConfig(IndexWriterConfig.OpenMode
				 openMode)
			{
				IndexWriterConfig conf = base.CreateIndexWriterConfig(openMode);
				sdp = new SnapshotDeletionPolicy(conf.GetIndexDeletionPolicy());
				conf.SetIndexDeletionPolicy(sdp);
				return conf;
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected override IndexWriter OpenIndexWriter(Lucene.Net.Store.Directory 
				directory, IndexWriterConfig config)
			{
				writer = base.OpenIndexWriter(directory, config);
				return writer;
			}

			/// <summary>
			/// Returns the
			/// <see cref="Lucene.Net.Index.SnapshotDeletionPolicy">Lucene.Net.Index.SnapshotDeletionPolicy
			/// 	</see>
			/// used by the underlying
			/// <see cref="Lucene.Net.Index.IndexWriter">Lucene.Net.Index.IndexWriter
			/// 	</see>
			/// .
			/// </summary>
			public SnapshotDeletionPolicy GetDeletionPolicy()
			{
				return sdp;
			}

			/// <summary>
			/// Returns the
			/// <see cref="Lucene.Net.Index.IndexWriter">Lucene.Net.Index.IndexWriter
			/// 	</see>
			/// used by this
			/// <see cref="Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyWriter">Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyWriter
			/// 	</see>
			/// .
			/// </summary>
			public IndexWriter GetIndexWriter()
			{
				return writer;
			}
		}

		private const int RADIX = 16;

		public static readonly string INDEX_SOURCE = "index";

		public static readonly string TAXONOMY_SOURCE = "taxo";

		private readonly IndexWriter indexWriter;

		private readonly IndexAndTaxonomyRevision.SnapshotDirectoryTaxonomyWriter taxoWriter;

		private readonly IndexCommit indexCommit;

		private readonly IndexCommit taxoCommit;

		private readonly SnapshotDeletionPolicy indexSDP;

		private readonly SnapshotDeletionPolicy taxoSDP;

		private readonly string version;

		private readonly IDictionary<string, IList<RevisionFile>> sourceFiles;

		/// <summary>
		/// Returns a singleton map of the revision files from the given
		/// <see cref="Lucene.Net.Index.IndexCommit">Lucene.Net.Index.IndexCommit
		/// 	</see>
		/// .
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public static IDictionary<string, IList<RevisionFile>> RevisionFiles(IndexCommit 
			indexCommit, IndexCommit taxoCommit)
		{
			Dictionary<string, IList<RevisionFile>> files = new Dictionary<string, IList<RevisionFile
				>>();
			files.Put(INDEX_SOURCE, IndexRevision.RevisionFiles(indexCommit).Values.Iterator(
				).Next());
			files.Put(TAXONOMY_SOURCE, IndexRevision.RevisionFiles(taxoCommit).Values.Iterator
				().Next());
			return files;
		}

		/// <summary>
		/// Returns a String representation of a revision's version from the given
		/// <see cref="Lucene.Net.Index.IndexCommit">Lucene.Net.Index.IndexCommit
		/// 	</see>
		/// s of the search and taxonomy indexes.
		/// </summary>
		public static string RevisionVersion(IndexCommit indexCommit, IndexCommit taxoCommit
			)
		{
			return System.Convert.ToString(indexCommit.GetGeneration(), RADIX) + ":" + System.Convert.ToString
				(taxoCommit.GetGeneration(), RADIX);
		}

		/// <summary>
		/// Constructor over the given
		/// <see cref="Lucene.Net.Index.IndexWriter">Lucene.Net.Index.IndexWriter
		/// 	</see>
		/// . Uses the last
		/// <see cref="Lucene.Net.Index.IndexCommit">Lucene.Net.Index.IndexCommit
		/// 	</see>
		/// found in the
		/// <see cref="Lucene.Net.Store.Directory">Lucene.Net.Store.Directory</see>
		/// managed by the given
		/// writer.
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public IndexAndTaxonomyRevision(IndexWriter indexWriter, IndexAndTaxonomyRevision.SnapshotDirectoryTaxonomyWriter
			 taxoWriter)
		{
			IndexDeletionPolicy delPolicy = indexWriter.GetConfig().GetIndexDeletionPolicy();
			if (!(delPolicy is SnapshotDeletionPolicy))
			{
				throw new ArgumentException("IndexWriter must be created with SnapshotDeletionPolicy"
					);
			}
			this.indexWriter = indexWriter;
			this.taxoWriter = taxoWriter;
			this.indexSDP = (SnapshotDeletionPolicy)delPolicy;
			this.taxoSDP = taxoWriter.GetDeletionPolicy();
			this.indexCommit = indexSDP.Snapshot();
			this.taxoCommit = taxoSDP.Snapshot();
			this.version = RevisionVersion(indexCommit, taxoCommit);
			this.sourceFiles = RevisionFiles(indexCommit, taxoCommit);
		}

		public virtual int CompareTo(string version)
		{
			string[] parts = version.Split(":");
			long indexGen = long.Parse(parts[0], RADIX);
			long taxoGen = long.Parse(parts[1], RADIX);
			long indexCommitGen = indexCommit.GetGeneration();
			long taxoCommitGen = taxoCommit.GetGeneration();
			// if the index generation is not the same as this commit's generation,
			// compare by it. Otherwise, compare by the taxonomy generation.
			if (indexCommitGen < indexGen)
			{
				return -1;
			}
			else
			{
				if (indexCommitGen > indexGen)
				{
					return 1;
				}
				else
				{
					return taxoCommitGen < taxoGen ? -1 : (taxoCommitGen > taxoGen ? 1 : 0);
				}
			}
		}

		public virtual int CompareTo(Revision o)
		{
			IndexAndTaxonomyRevision other = (IndexAndTaxonomyRevision)o;
			int cmp = indexCommit.CompareTo(other.indexCommit);
			return cmp != 0 ? cmp : taxoCommit.CompareTo(other.taxoCommit);
		}

		public virtual string GetVersion()
		{
			return version;
		}

		public virtual IDictionary<string, IList<RevisionFile>> GetSourceFiles()
		{
			return sourceFiles;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual InputStream Open(string source, string fileName)
		{
			//HM:revisit
			IndexCommit ic = source.Equals(INDEX_SOURCE) ? indexCommit : taxoCommit;
			return new IndexInputInputStream(ic.GetDirectory().OpenInput(fileName, IOContext.
				READONCE));
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void Release()
		{
			try
			{
				indexSDP.Release(indexCommit);
			}
			finally
			{
				taxoSDP.Release(taxoCommit);
			}
			try
			{
				indexWriter.DeleteUnusedFiles();
			}
			finally
			{
				taxoWriter.GetIndexWriter().DeleteUnusedFiles();
			}
		}

		public override string ToString()
		{
			return "IndexAndTaxonomyRevision version=" + version + " files=" + sourceFiles;
		}
	}
}
