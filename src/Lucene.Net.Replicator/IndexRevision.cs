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
using Sharpen;

namespace Lucene.Net.Replicator
{
	/// <summary>
	/// A
	/// <see cref="Revision">Revision</see>
	/// of a single index files which comprises the list of files
	/// that are part of the current
	/// <see cref="Lucene.Net.Index.IndexCommit">Lucene.Net.Index.IndexCommit
	/// 	</see>
	/// . To ensure the files are not
	/// deleted by
	/// <see cref="Lucene.Net.Index.IndexWriter">Lucene.Net.Index.IndexWriter
	/// 	</see>
	/// for as long as this revision stays alive (i.e.
	/// until
	/// <see cref="Release()">Release()</see>
	/// ), the current commit point is snapshotted, using
	/// <see cref="Lucene.Net.Index.SnapshotDeletionPolicy">Lucene.Net.Index.SnapshotDeletionPolicy
	/// 	</see>
	/// (this means that the given writer's
	/// <see cref="Lucene.Net.Index.IndexWriterConfig.GetIndexDeletionPolicy()">config
	/// 	</see>
	/// should return
	/// <see cref="Lucene.Net.Index.SnapshotDeletionPolicy">Lucene.Net.Index.SnapshotDeletionPolicy
	/// 	</see>
	/// ).
	/// <p>
	/// When this revision is
	/// <see cref="Release()">released</see>
	/// , it releases the obtained
	/// snapshot as well as calls
	/// <see cref="Lucene.Net.Index.IndexWriter.DeleteUnusedFiles()">Lucene.Net.Index.IndexWriter.DeleteUnusedFiles()
	/// 	</see>
	/// so that the
	/// snapshotted files are deleted (if they are no longer needed).
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class IndexRevision : Revision
	{
		private const int RADIX = 16;

		private static readonly string SOURCE = "index";

		private readonly IndexWriter writer;

		private readonly IndexCommit commit;

		private readonly SnapshotDeletionPolicy sdp;

		private readonly string version;

		private readonly IDictionary<string, IList<RevisionFile>> sourceFiles;

		// returns a RevisionFile with some metadata
		/// <exception cref="System.IO.IOException"></exception>
		private static RevisionFile NewRevisionFile(string file, Directory dir)
		{
			RevisionFile revFile = new RevisionFile(file);
			revFile.size = dir.FileLength(file);
			return revFile;
		}

		/// <summary>
		/// Returns a singleton map of the revision files from the given
		/// <see cref="Lucene.Net.Index.IndexCommit">Lucene.Net.Index.IndexCommit
		/// 	</see>
		/// .
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public static IDictionary<string, IList<RevisionFile>> RevisionFiles(IndexCommit 
			commit)
		{
			ICollection<string> commitFiles = commit.GetFileNames();
			IList<RevisionFile> revisionFiles = new AList<RevisionFile>(commitFiles.Count);
			string segmentsFile = commit.GetSegmentsFileName();
			Directory dir = commit.GetDirectory();
			foreach (string file in commitFiles)
			{
				if (!file.Equals(segmentsFile))
				{
					revisionFiles.AddItem(NewRevisionFile(file, dir));
				}
			}
			revisionFiles.AddItem(NewRevisionFile(segmentsFile, dir));
			// segments_N must be last
			return Sharpen.Collections.SingletonMap(SOURCE, revisionFiles);
		}

		/// <summary>
		/// Returns a String representation of a revision's version from the given
		/// <see cref="Lucene.Net.Index.IndexCommit">Lucene.Net.Index.IndexCommit
		/// 	</see>
		/// .
		/// </summary>
		public static string RevisionVersion(IndexCommit commit)
		{
			return System.Convert.ToString(commit.GetGeneration(), RADIX);
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
		public IndexRevision(IndexWriter writer)
		{
			IndexDeletionPolicy delPolicy = writer.GetConfig().GetIndexDeletionPolicy();
			if (!(delPolicy is SnapshotDeletionPolicy))
			{
				throw new ArgumentException("IndexWriter must be created with SnapshotDeletionPolicy"
					);
			}
			this.writer = writer;
			this.sdp = (SnapshotDeletionPolicy)delPolicy;
			this.commit = sdp.Snapshot();
			this.version = RevisionVersion(commit);
			this.sourceFiles = RevisionFiles(commit);
		}

		public virtual int CompareTo(string version)
		{
			long gen = long.Parse(version, RADIX);
			long commitGen = commit.GetGeneration();
			return commitGen < gen ? -1 : (commitGen > gen ? 1 : 0);
		}

		public virtual int CompareTo(Revision o)
		{
			Lucene.Net.Replicator.IndexRevision other = (Lucene.Net.Replicator.IndexRevision
				)o;
			return commit.CompareTo(other.commit);
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
			//assert source.equals(SOURCE) : "invalid source; expected=" + SOURCE + " got=" + source;
			return new IndexInputInputStream(commit.GetDirectory().OpenInput(fileName, IOContext
				.READONCE));
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void Release()
		{
			sdp.Release(commit);
			writer.DeleteUnusedFiles();
		}

		public override string ToString()
		{
			return "IndexRevision version=" + version + " files=" + sourceFiles;
		}
	}
}
