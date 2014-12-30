using System.IO;
using Lucene.Net.Codecs;
using Lucene.Net.Documents;
using Lucene.Net.Store;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using Lucene.Net.Util;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Index
{
    public class PersistentSnapshotDeletionPolicy : SnapshotDeletionPolicy
    {
		public static readonly string SNAPSHOTS_PREFIX = "snapshots_";
		private const int VERSION_START = 0;
		private const int VERSION_CURRENT = VERSION_START;
		private static readonly string CODEC_NAME = "snapshots";
		private long nextWriteGen;
		private readonly Directory dir;
        private const string SNAPSHOTS_ID = "$SNAPSHOTS_DOC$";
		public PersistentSnapshotDeletionPolicy(IndexDeletionPolicy primary, Directory dir
			) : this(primary, dir, IndexWriterConfig.OpenMode.CREATE_OR_APPEND)
		{
		}

        public static IDictionary<string, string> ReadSnapshotsInfo(Directory dir)
        {
            IndexReader r = DirectoryReader.Open(dir);
            IDictionary<string, string> snapshots = new HashMap<string, string>();
            try
            {
                int numDocs = r.NumDocs;
                // index is allowed to have exactly one document or 0.
                if (numDocs == 1)
                {
                    Document doc = r.Document(r.MaxDoc - 1);
                    if (doc.GetField(SNAPSHOTS_ID) == null)
                    {
                        throw new InvalidOperationException("directory is not a valid snapshots store!");
                    }
                    doc.RemoveField(SNAPSHOTS_ID);
                    foreach (IIndexableField f in doc)
                    {
                        snapshots[f.Name] = f.StringValue;
                    }
                }
                else if (numDocs != 0)
                {
                    throw new InvalidOperationException(
                        "should be at most 1 document in the snapshots directory: " + numDocs);
                }
            }
            finally
            {
                r.Dispose();
            }
            return snapshots;
        }

		public PersistentSnapshotDeletionPolicy(IndexDeletionPolicy primary, Directory dir
			, IndexWriterConfig.OpenMode mode) : base(primary)
		{
			this.dir = dir;
			if (mode == IndexWriterConfig.OpenMode.CREATE)
			{
				ClearPriorSnapshots();
			}
			LoadPriorSnapshots();
			if (mode == IndexWriterConfig.OpenMode.APPEND && nextWriteGen == 0)
			{
				throw new InvalidOperationException("no snapshots stored in this directory");
			}
        }

        

		public override IndexCommit Snapshot()
        {
			lock (this)
			{
				IndexCommit ic = base.Snapshot();
				bool success = false;
				try
				{
					Persist();
					success = true;
				}
				finally
				{
					if (!success)
					{
						try
						{
							base.Release(ic);
						}
						catch (Exception)
						{
						}
					}
				}
				// Suppress so we keep throwing original exception
				return ic;
			}
        }

		public override void Release(IndexCommit commit)
        {
			lock (this)
			{
				base.Release(commit);
				bool success = false;
				try
				{
					Persist();
					success = true;
				}
				finally
				{
					if (!success)
					{
						try
						{
							IncRef(commit);
						}
						catch (Exception)
						{
						}
					}
				}
			}
        }

        

        

        private void Persist()
        {
			lock (this)
			{
				string fileName = SNAPSHOTS_PREFIX + nextWriteGen;
				IndexOutput @out = dir.CreateOutput(fileName, IOContext.DEFAULT);
				bool success = false;
				try
				{
					CodecUtil.WriteHeader(@out, CODEC_NAME, VERSION_CURRENT);
					@out.WriteVInt(refCounts.Count);
					foreach (KeyValuePair<long, int> ent in refCounts)
					{
						@out.WriteVLong(ent.Key);
						@out.WriteVInt(ent.Value);
					}
					success = true;
				}
				finally
				{
					if (!success)
					{
						IOUtils.CloseWhileHandlingException((IDisposable)@out);
						try
						{
							dir.DeleteFile(fileName);
						}
						catch (Exception)
						{
						}
					}
					else
					{
						// Suppress so we keep throwing original exception
						IOUtils.Close(@out);
					}
				}
				dir.Sync(new List<string>{fileName});
				if (nextWriteGen > 0)
				{
					string lastSaveFile = SNAPSHOTS_PREFIX + (nextWriteGen - 1);
					try
					{
						dir.DeleteFile(lastSaveFile);
					}
					catch (IOException)
					{
					}
				}
				// OK: likely it didn't exist
				nextWriteGen++;
			}
        }
		private void ClearPriorSnapshots()
		{
			lock (this)
			{
				foreach (string file in dir.ListAll())
				{
					if (file.StartsWith(SNAPSHOTS_PREFIX))
					{
						dir.DeleteFile(file);
					}
				}
			}
		}
		public virtual string GetLastSaveFile()
		{
			if (nextWriteGen == 0)
			{
				return null;
			}
			else
			{
				return SNAPSHOTS_PREFIX + (nextWriteGen - 1);
			}
		}
		private void LoadPriorSnapshots()
		{
			lock (this)
			{
				long genLoaded = -1;
				IOException ioe = null;
				IList<string> snapshotFiles = new List<string>();
				foreach (string file in dir.ListAll())
				{
					if (file.StartsWith(SNAPSHOTS_PREFIX))
					{
						long gen = long.Parse(file.Substring(SNAPSHOTS_PREFIX.Length));
						if (genLoaded == -1 || gen > genLoaded)
						{
							snapshotFiles.Add(file);
							IDictionary<long, int> m = new Dictionary<long, int>();
							IndexInput @in = dir.OpenInput(file, IOContext.DEFAULT);
							try
							{
								CodecUtil.CheckHeader(@in, CODEC_NAME, VERSION_START, VERSION_START);
								int count = @in.ReadVInt();
								for (int i = 0; i < count; i++)
								{
									long commitGen = @in.ReadVLong();
									int refCount = @in.ReadVInt();
									m[commitGen] = refCount;
								}
							}
							catch (IOException ioe2)
							{
								// Save first exception & throw in the end
								if (ioe == null)
								{
									ioe = ioe2;
								}
							}
							finally
							{
								@in.Dispose();
							}
							genLoaded = gen;
							refCounts.Clear();
							refCounts.PutAll(m);
						}
					}
				}
				if (genLoaded == -1)
				{
					// Nothing was loaded...
					if (ioe != null)
					{
						// ... not for lack of trying:
						throw ioe;
					}
				}
				else
				{
					if (snapshotFiles.Count > 1)
					{
						// Remove any broken / old snapshot files:
						string curFileName = SNAPSHOTS_PREFIX + genLoaded;
						foreach (string file_1 in snapshotFiles)
						{
							if (!curFileName.Equals(file_1))
							{
								dir.DeleteFile(file_1);
							}
						}
					}
					nextWriteGen = 1 + genLoaded;
				}
			}
		}
    }
}
