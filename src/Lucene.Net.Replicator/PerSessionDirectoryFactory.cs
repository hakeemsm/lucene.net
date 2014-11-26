/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.IO;
using Lucene.Net.Replicator;
using Lucene.Net.Store;
using Sharpen;

namespace Lucene.Net.Replicator
{
	/// <summary>
	/// A
	/// <see cref="SourceDirectoryFactory">SourceDirectoryFactory</see>
	/// which returns
	/// <see cref="Lucene.Net.Store.FSDirectory">Lucene.Net.Store.FSDirectory
	/// 	</see>
	/// under a
	/// dedicated session directory. When a session is over, the entire directory is
	/// deleted.
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class PerSessionDirectoryFactory : ReplicationClient.SourceDirectoryFactory
	{
		private readonly FilePath workDir;

		/// <summary>Constructor with the given sources mapping.</summary>
		/// <remarks>Constructor with the given sources mapping.</remarks>
		public PerSessionDirectoryFactory(FilePath workDir)
		{
			this.workDir = workDir;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void Rm(FilePath file)
		{
			if (file.IsDirectory())
			{
				foreach (FilePath f in file.ListFiles())
				{
					Rm(f);
				}
			}
			// This should be either an empty directory, or a file
			if (!file.Delete() && file.Exists())
			{
				throw new IOException("failed to delete " + file);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual Directory GetDirectory(string sessionID, string source)
		{
			FilePath sessionDir = new FilePath(workDir, sessionID);
			if (!sessionDir.Exists() && !sessionDir.Mkdirs())
			{
				throw new IOException("failed to create session directory " + sessionDir);
			}
			FilePath sourceDir = new FilePath(sessionDir, source);
			if (!sourceDir.Mkdirs())
			{
				throw new IOException("failed to create source directory " + sourceDir);
			}
			return FSDirectory.Open(sourceDir);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void CleanupSession(string sessionID)
		{
			if (sessionID.IsEmpty())
			{
				// protect against deleting workDir entirely!
				throw new ArgumentException("sessionID cannot be empty");
			}
			Rm(new FilePath(workDir, sessionID));
		}
	}
}
