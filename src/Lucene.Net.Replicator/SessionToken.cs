/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using System.IO;
using Lucene.Net.Replicator;
using Sharpen;

namespace Lucene.Net.Replicator
{
	/// <summary>
	/// Token for a replication session, for guaranteeing that source replicated
	/// files will be kept safe until the replication completes.
	/// </summary>
	/// <remarks>
	/// Token for a replication session, for guaranteeing that source replicated
	/// files will be kept safe until the replication completes.
	/// </remarks>
	/// <seealso cref="Replicator.CheckForUpdate(string)">Replicator.CheckForUpdate(string)
	/// 	</seealso>
	/// <seealso cref="Replicator.Release(string)">Replicator.Release(string)</seealso>
	/// <seealso cref="LocalReplicator.DEFAULT_SESSION_EXPIRATION_THRESHOLD">LocalReplicator.DEFAULT_SESSION_EXPIRATION_THRESHOLD
	/// 	</seealso>
	/// <lucene.experimental></lucene.experimental>
	public sealed class SessionToken
	{
		/// <summary>ID of this session.</summary>
		/// <remarks>
		/// ID of this session.
		/// Should be passed when releasing the session, thereby acknowledging the
		/// <see cref="Replicator">Replicator</see>
		/// that this session is no longer in use.
		/// </remarks>
		/// <seealso cref="Replicator.Release(string)">Replicator.Release(string)</seealso>
		public readonly string id;

		/// <seealso cref="Revision.GetVersion()">Revision.GetVersion()</seealso>
		public readonly string version;

		/// <seealso cref="Revision.GetSourceFiles()">Revision.GetSourceFiles()</seealso>
		public readonly IDictionary<string, IList<RevisionFile>> sourceFiles;

		/// <summary>
		/// Constructor which deserializes from the given
		/// <see cref="System.IO.DataInput">System.IO.DataInput</see>
		/// .
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public SessionToken(DataInput @in)
		{
			this.id = @in.ReadUTF();
			this.version = @in.ReadUTF();
			this.sourceFiles = new Dictionary<string, IList<RevisionFile>>();
			int numSources = @in.ReadInt();
			while (numSources > 0)
			{
				string source = @in.ReadUTF();
				int numFiles = @in.ReadInt();
				IList<RevisionFile> files = new AList<RevisionFile>(numFiles);
				for (int i = 0; i < numFiles; i++)
				{
					string fileName = @in.ReadUTF();
					RevisionFile file = new RevisionFile(fileName);
					file.size = @in.ReadLong();
					files.AddItem(file);
				}
				this.sourceFiles.Put(source, files);
				--numSources;
			}
		}

		/// <summary>Constructor with the given id and revision.</summary>
		/// <remarks>Constructor with the given id and revision.</remarks>
		public SessionToken(string id, Revision revision)
		{
			this.id = id;
			this.version = revision.GetVersion();
			this.sourceFiles = revision.GetSourceFiles();
		}

		/// <summary>Serialize the token data for communication between server and client.</summary>
		/// <remarks>Serialize the token data for communication between server and client.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public void Serialize(DataOutput @out)
		{
			@out.WriteUTF(id);
			@out.WriteUTF(version);
			@out.WriteInt(sourceFiles.Count);
			foreach (KeyValuePair<string, IList<RevisionFile>> e in sourceFiles.EntrySet())
			{
				@out.WriteUTF(e.Key);
				IList<RevisionFile> files = e.Value;
				@out.WriteInt(files.Count);
				foreach (RevisionFile file in files)
				{
					@out.WriteUTF(file.fileName);
					@out.WriteLong(file.size);
				}
			}
		}

		public override string ToString()
		{
			return "id=" + id + " version=" + version + " files=" + sourceFiles;
		}
	}
}
