/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Index;
using Lucene.Net.Replicator;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Replicator
{
	/// <summary>
	/// A
	/// <see cref="ReplicationHandler">ReplicationHandler</see>
	/// for replication of an index and taxonomy pair.
	/// See
	/// <see cref="IndexReplicationHandler">IndexReplicationHandler</see>
	/// for more detail. This handler ensures
	/// that the search and taxonomy indexes are replicated in a consistent way.
	/// <p>
	/// <b>NOTE:</b> if you intend to recreate a taxonomy index, you should make sure
	/// to reopen an IndexSearcher and TaxonomyReader pair via the provided callback,
	/// to guarantee that both indexes are in sync. This handler does not prevent
	/// replicating such index and taxonomy pairs, and if they are reopened by a
	/// different thread, unexpected errors can occur, as well as inconsistency
	/// between the taxonomy and index readers.
	/// </summary>
	/// <seealso cref="IndexReplicationHandler">IndexReplicationHandler</seealso>
	/// <lucene.experimental></lucene.experimental>
	public class IndexAndTaxonomyReplicationHandler : ReplicationClient.ReplicationHandler
	{
		/// <summary>
		/// The component used to log messages to the
		/// <see cref="Lucene.Net.Util.InfoStream.GetDefault()">default</see>
		/// 
		/// <see cref="Lucene.Net.Util.InfoStream">Lucene.Net.Util.InfoStream</see>
		/// .
		/// </summary>
		public static readonly string INFO_STREAM_COMPONENT = "IndexAndTaxonomyReplicationHandler";

		private readonly Directory indexDir;

		private readonly Directory taxoDir;

		private readonly Callable<bool> callback;

		private volatile IDictionary<string, IList<RevisionFile>> currentRevisionFiles;

		private volatile string currentVersion;

		private volatile InfoStream infoStream = InfoStream.GetDefault();

		/// <summary>
		/// Constructor with the given index directory and callback to notify when the
		/// indexes were updated.
		/// </summary>
		/// <remarks>
		/// Constructor with the given index directory and callback to notify when the
		/// indexes were updated.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public IndexAndTaxonomyReplicationHandler(Directory indexDir, Directory taxoDir, 
			Callable<bool> callback)
		{
			this.callback = callback;
			this.indexDir = indexDir;
			this.taxoDir = taxoDir;
			currentRevisionFiles = null;
			currentVersion = null;
			bool indexExists = DirectoryReader.IndexExists(indexDir);
			bool taxoExists = DirectoryReader.IndexExists(taxoDir);
			if (indexExists != taxoExists)
			{
				throw new InvalidOperationException("search and taxonomy indexes must either both exist or not: index="
					 + indexExists + " taxo=" + taxoExists);
			}
			if (indexExists)
			{
				// both indexes exist
				IndexCommit indexCommit = IndexReplicationHandler.GetLastCommit(indexDir);
				IndexCommit taxoCommit = IndexReplicationHandler.GetLastCommit(taxoDir);
				currentRevisionFiles = IndexAndTaxonomyRevision.RevisionFiles(indexCommit, taxoCommit
					);
				currentVersion = IndexAndTaxonomyRevision.RevisionVersion(indexCommit, taxoCommit
					);
				InfoStream infoStream = InfoStream.GetDefault();
				if (infoStream.IsEnabled(INFO_STREAM_COMPONENT))
				{
					infoStream.Message(INFO_STREAM_COMPONENT, "constructor(): currentVersion=" + currentVersion
						 + " currentRevisionFiles=" + currentRevisionFiles);
					infoStream.Message(INFO_STREAM_COMPONENT, "constructor(): indexCommit=" + indexCommit
						 + " taxoCommit=" + taxoCommit);
				}
			}
		}

		public virtual string CurrentVersion()
		{
			return currentVersion;
		}

		public virtual IDictionary<string, IList<RevisionFile>> CurrentRevisionFiles()
		{
			return currentRevisionFiles;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void RevisionReady(string version, IDictionary<string, IList<RevisionFile
			>> revisionFiles, IDictionary<string, IList<string>> copiedFiles, IDictionary<string
			, Directory> sourceDirectory)
		{
			Directory taxoClientDir = sourceDirectory.Get(IndexAndTaxonomyRevision.TAXONOMY_SOURCE
				);
			Directory indexClientDir = sourceDirectory.Get(IndexAndTaxonomyRevision.INDEX_SOURCE
				);
			IList<string> taxoFiles = copiedFiles.Get(IndexAndTaxonomyRevision.TAXONOMY_SOURCE
				);
			IList<string> indexFiles = copiedFiles.Get(IndexAndTaxonomyRevision.INDEX_SOURCE);
			string taxoSegmentsFile = IndexReplicationHandler.GetSegmentsFile(taxoFiles, true
				);
			string indexSegmentsFile = IndexReplicationHandler.GetSegmentsFile(indexFiles, false
				);
			bool success = false;
			try
			{
				// copy taxonomy files before index files
				IndexReplicationHandler.CopyFiles(taxoClientDir, taxoDir, taxoFiles);
				IndexReplicationHandler.CopyFiles(indexClientDir, indexDir, indexFiles);
				// fsync all copied files (except segmentsFile)
				if (!taxoFiles.IsEmpty())
				{
					taxoDir.Sync(taxoFiles);
				}
				indexDir.Sync(indexFiles);
				// now copy and fsync segmentsFile, taxonomy first because it is ok if a
				// reader sees a more advanced taxonomy than the index.
				if (taxoSegmentsFile != null)
				{
					taxoClientDir.Copy(taxoDir, taxoSegmentsFile, taxoSegmentsFile, IOContext.READONCE
						);
				}
				indexClientDir.Copy(indexDir, indexSegmentsFile, indexSegmentsFile, IOContext.READONCE
					);
				if (taxoSegmentsFile != null)
				{
					taxoDir.Sync(Sharpen.Collections.SingletonList(taxoSegmentsFile));
				}
				indexDir.Sync(Sharpen.Collections.SingletonList(indexSegmentsFile));
				success = true;
			}
			finally
			{
				if (!success)
				{
					taxoFiles.AddItem(taxoSegmentsFile);
					// add it back so it gets deleted too
					IndexReplicationHandler.CleanupFilesOnFailure(taxoDir, taxoFiles);
					indexFiles.AddItem(indexSegmentsFile);
					// add it back so it gets deleted too
					IndexReplicationHandler.CleanupFilesOnFailure(indexDir, indexFiles);
				}
			}
			// all files have been successfully copied + sync'd. update the handler's state
			currentRevisionFiles = revisionFiles;
			currentVersion = version;
			if (infoStream.IsEnabled(INFO_STREAM_COMPONENT))
			{
				infoStream.Message(INFO_STREAM_COMPONENT, "revisionReady(): currentVersion=" + currentVersion
					 + " currentRevisionFiles=" + currentRevisionFiles);
			}
			// update the segments.gen file
			IndexReplicationHandler.WriteSegmentsGen(taxoSegmentsFile, taxoDir);
			IndexReplicationHandler.WriteSegmentsGen(indexSegmentsFile, indexDir);
			// Cleanup the index directory from old and unused index files.
			// NOTE: we don't use IndexWriter.deleteUnusedFiles here since it may have
			// side-effects, e.g. if it hits sudden IO errors while opening the index
			// (and can end up deleting the entire index). It is not our job to protect
			// against those errors, app will probably hit them elsewhere.
			IndexReplicationHandler.CleanupOldIndexFiles(indexDir, indexSegmentsFile);
			IndexReplicationHandler.CleanupOldIndexFiles(taxoDir, taxoSegmentsFile);
			// successfully updated the index, notify the callback that the index is
			// ready.
			if (callback != null)
			{
				try
				{
					callback.Call();
				}
				catch (Exception e)
				{
					throw new IOException(e);
				}
			}
		}

		/// <summary>
		/// Sets the
		/// <see cref="Lucene.Net.Util.InfoStream">Lucene.Net.Util.InfoStream</see>
		/// to use for logging messages.
		/// </summary>
		public virtual void SetInfoStream(InfoStream infoStream)
		{
			if (infoStream == null)
			{
				infoStream = InfoStream.NO_OUTPUT;
			}
			this.infoStream = infoStream;
		}
	}
}
