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
	/// for replication of an index. Implements
	/// <see cref="RevisionReady(string, System.Collections.Generic.IDictionary{K, V}, System.Collections.Generic.IDictionary{K, V}, System.Collections.Generic.IDictionary{K, V})
	/// 	">RevisionReady(string, System.Collections.Generic.IDictionary&lt;K, V&gt;, System.Collections.Generic.IDictionary&lt;K, V&gt;, System.Collections.Generic.IDictionary&lt;K, V&gt;)
	/// 	</see>
	/// by copying the files pointed by the client resolver to
	/// the index
	/// <see cref="Lucene.Net.Store.Directory">Lucene.Net.Store.Directory</see>
	/// and then touches the index with
	/// <see cref="Lucene.Net.Index.IndexWriter">Lucene.Net.Index.IndexWriter
	/// 	</see>
	/// to make sure any unused files are deleted.
	/// <p>
	/// <b>NOTE:</b> this handler assumes that
	/// <see cref="Lucene.Net.Index.IndexWriter">Lucene.Net.Index.IndexWriter
	/// 	</see>
	/// is not opened by
	/// another process on the index directory. In fact, opening an
	/// <see cref="Lucene.Net.Index.IndexWriter">Lucene.Net.Index.IndexWriter
	/// 	</see>
	/// on the same directory to which files are copied can lead
	/// to undefined behavior, where some or all the files will be deleted, override
	/// other files or simply create a mess. When you replicate an index, it is best
	/// if the index is never modified by
	/// <see cref="Lucene.Net.Index.IndexWriter">Lucene.Net.Index.IndexWriter
	/// 	</see>
	/// , except the one that is
	/// open on the source index, from which you replicate.
	/// <p>
	/// This handler notifies the application via a provided
	/// <see cref="Sharpen.Callable{V}">Sharpen.Callable&lt;V&gt;</see>
	/// when an
	/// updated index commit was made available for it.
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class IndexReplicationHandler : ReplicationClient.ReplicationHandler
	{
		/// <summary>
		/// The component used to log messages to the
		/// <see cref="Lucene.Net.Util.InfoStream.GetDefault()">default</see>
		/// 
		/// <see cref="Lucene.Net.Util.InfoStream">Lucene.Net.Util.InfoStream</see>
		/// .
		/// </summary>
		public static readonly string INFO_STREAM_COMPONENT = "IndexReplicationHandler";

		private readonly Directory indexDir;

		private readonly Callable<bool> callback;

		private volatile IDictionary<string, IList<RevisionFile>> currentRevisionFiles;

		private volatile string currentVersion;

		private volatile InfoStream infoStream = InfoStream.GetDefault();

		/// <summary>
		/// Returns the last
		/// <see cref="Lucene.Net.Index.IndexCommit">Lucene.Net.Index.IndexCommit
		/// 	</see>
		/// found in the
		/// <see cref="Lucene.Net.Store.Directory">Lucene.Net.Store.Directory</see>
		/// , or
		/// <code>null</code>
		/// if there are no commits.
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public static IndexCommit GetLastCommit(Directory dir)
		{
			try
			{
				if (DirectoryReader.IndexExists(dir))
				{
					IList<IndexCommit> commits = DirectoryReader.ListCommits(dir);
					// listCommits guarantees that we get at least one commit back, or
					// IndexNotFoundException which we handle below
					return commits[commits.Count - 1];
				}
			}
			catch (IndexNotFoundException)
			{
			}
			// ignore the exception and return null
			return null;
		}

		/// <summary>Verifies that the last file is segments_N and fails otherwise.</summary>
		/// <remarks>
		/// Verifies that the last file is segments_N and fails otherwise. It also
		/// removes and returns the file from the list, because it needs to be handled
		/// last, after all files. This is important in order to guarantee that if a
		/// reader sees the new segments_N, all other segment files are already on
		/// stable storage.
		/// <p>
		/// The reason why the code fails instead of putting segments_N file last is
		/// that this indicates an error in the Revision implementation.
		/// </remarks>
		public static string GetSegmentsFile(IList<string> files, bool allowEmpty)
		{
			if (files.IsEmpty())
			{
				if (allowEmpty)
				{
					return null;
				}
				else
				{
					throw new InvalidOperationException("empty list of files not allowed");
				}
			}
			string segmentsFile = files.Remove(files.Count - 1);
			if (!segmentsFile.StartsWith(IndexFileNames.SEGMENTS) || segmentsFile.Equals(IndexFileNames
				.SEGMENTS_GEN))
			{
				throw new InvalidOperationException("last file to copy+sync must be segments_N but got "
					 + segmentsFile + "; check your Revision implementation!");
			}
			return segmentsFile;
		}

		/// <summary>Cleanup the index directory by deleting all given files.</summary>
		/// <remarks>
		/// Cleanup the index directory by deleting all given files. Called when file
		/// copy or sync failed.
		/// </remarks>
		public static void CleanupFilesOnFailure(Directory dir, IList<string> files)
		{
			foreach (string file in files)
			{
				try
				{
					dir.DeleteFile(file);
				}
				catch
				{
				}
			}
		}

		// suppress any exception because if we're here, it means copy
		// failed, and we must cleanup after ourselves.
		/// <summary>Cleans up the index directory from old index files.</summary>
		/// <remarks>
		/// Cleans up the index directory from old index files. This method uses the
		/// last commit found by
		/// <see cref="GetLastCommit(Lucene.Net.Store.Directory)">GetLastCommit(Lucene.Net.Store.Directory)
		/// 	</see>
		/// . If it matches the
		/// expected segmentsFile, then all files not referenced by this commit point
		/// are deleted.
		/// <p>
		/// <b>NOTE:</b> this method does a best effort attempt to clean the index
		/// directory. It suppresses any exceptions that occur, as this can be retried
		/// the next time.
		/// </remarks>
		public static void CleanupOldIndexFiles(Directory dir, string segmentsFile)
		{
			try
			{
				IndexCommit commit = GetLastCommit(dir);
				// commit == null means weird IO errors occurred, ignore them
				// if there were any IO errors reading the expected commit point (i.e.
				// segments files mismatch), then ignore that commit either.
				if (commit != null && commit.GetSegmentsFileName().Equals(segmentsFile))
				{
					ICollection<string> commitFiles = new HashSet<string>();
					Sharpen.Collections.AddAll(commitFiles, commit.GetFileNames());
					commitFiles.AddItem(IndexFileNames.SEGMENTS_GEN);
					Matcher matcher = IndexFileNames.CODEC_FILE_PATTERN.Matcher(string.Empty);
					foreach (string file in dir.ListAll())
					{
						if (!commitFiles.Contains(file) && (matcher.Reset(file).Matches() || file.StartsWith
							(IndexFileNames.SEGMENTS)))
						{
							try
							{
								dir.DeleteFile(file);
							}
							catch
							{
							}
						}
					}
				}
			}
			catch
			{
			}
		}

		// suppress, it's just a best effort
		// ignore any errors that happens during this state and only log it. this
		// cleanup will have a chance to succeed the next time we get a new
		// revision.
		/// <summary>
		/// Copies the files from the source directory to the target one, if they are
		/// not the same.
		/// </summary>
		/// <remarks>
		/// Copies the files from the source directory to the target one, if they are
		/// not the same.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public static void CopyFiles(Directory source, Directory target, IList<string> files
			)
		{
			if (!source.Equals(target))
			{
				foreach (string file in files)
				{
					source.Copy(target, file, file, IOContext.READONCE);
				}
			}
		}

		/// <summary>
		/// Writes
		/// <see cref="Lucene.Net.Index.IndexFileNames.SEGMENTS_GEN">Lucene.Net.Index.IndexFileNames.SEGMENTS_GEN
		/// 	</see>
		/// file to the directory, reading
		/// the generation from the given
		/// <code>segmentsFile</code>
		/// . If it is
		/// <code>null</code>
		/// ,
		/// this method deletes segments.gen from the directory.
		/// </summary>
		public static void WriteSegmentsGen(string segmentsFile, Directory dir)
		{
			if (segmentsFile != null)
			{
				SegmentInfos.WriteSegmentsGen(dir, SegmentInfos.GenerationFromSegmentsFileName(segmentsFile
					));
			}
			else
			{
				try
				{
					dir.DeleteFile(IndexFileNames.SEGMENTS_GEN);
				}
				catch
				{
				}
			}
		}

		/// <summary>
		/// Constructor with the given index directory and callback to notify when the
		/// indexes were updated.
		/// </summary>
		/// <remarks>
		/// Constructor with the given index directory and callback to notify when the
		/// indexes were updated.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public IndexReplicationHandler(Directory indexDir, Callable<bool> callback)
		{
			// suppress any errors while deleting this file.
			this.callback = callback;
			this.indexDir = indexDir;
			currentRevisionFiles = null;
			currentVersion = null;
			if (DirectoryReader.IndexExists(indexDir))
			{
				IList<IndexCommit> commits = DirectoryReader.ListCommits(indexDir);
				IndexCommit commit = commits[commits.Count - 1];
				currentRevisionFiles = IndexRevision.RevisionFiles(commit);
				currentVersion = IndexRevision.RevisionVersion(commit);
				InfoStream infoStream = InfoStream.GetDefault();
				if (infoStream.IsEnabled(INFO_STREAM_COMPONENT))
				{
					infoStream.Message(INFO_STREAM_COMPONENT, "constructor(): currentVersion=" + currentVersion
						 + " currentRevisionFiles=" + currentRevisionFiles);
					infoStream.Message(INFO_STREAM_COMPONENT, "constructor(): commit=" + commit);
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
			if (revisionFiles.Count > 1)
			{
				throw new ArgumentException("this handler handles only a single source; got " + revisionFiles
					.Keys);
			}
			Directory clientDir = sourceDirectory.Values.Iterator().Next();
			IList<string> files = copiedFiles.Values.Iterator().Next();
			string segmentsFile = GetSegmentsFile(files, false);
			bool success = false;
			try
			{
				// copy files from the client to index directory
				CopyFiles(clientDir, indexDir, files);
				// fsync all copied files (except segmentsFile)
				indexDir.Sync(files);
				// now copy and fsync segmentsFile
				clientDir.Copy(indexDir, segmentsFile, segmentsFile, IOContext.READONCE);
				indexDir.Sync(Sharpen.Collections.SingletonList(segmentsFile));
				success = true;
			}
			finally
			{
				if (!success)
				{
					files.AddItem(segmentsFile);
					// add it back so it gets deleted too
					CleanupFilesOnFailure(indexDir, files);
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
			WriteSegmentsGen(segmentsFile, indexDir);
			// Cleanup the index directory from old and unused index files.
			// NOTE: we don't use IndexWriter.deleteUnusedFiles here since it may have
			// side-effects, e.g. if it hits sudden IO errors while opening the index
			// (and can end up deleting the entire index). It is not our job to protect
			// against those errors, app will probably hit them elsewhere.
			CleanupOldIndexFiles(indexDir, segmentsFile);
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
