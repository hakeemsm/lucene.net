/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Linq;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Index
{

    /// <summary>A <see cref="IndexDeletionPolicy" /> that wraps around any other
    /// <see cref="IndexDeletionPolicy" /> and adds the ability to hold and
    /// later release a single "snapshot" of an index.  While
    /// the snapshot is held, the <see cref="IndexWriter" /> will not
    /// remove any files associated with it even if the index is
    /// otherwise being actively, arbitrarily changed.  Because
    /// we wrap another arbitrary <see cref="IndexDeletionPolicy" />, this
    /// gives you the freedom to continue using whatever <see cref="IndexDeletionPolicy" />
    /// you would normally want to use with your
    /// index.  Note that you can re-use a single instance of
    /// SnapshotDeletionPolicy across multiple writers as long
    /// as they are against the same index Directory.  Any
    /// snapshot held when a writer is closed will "survive"
    /// when the next writer is opened.
    /// 
    /// <p/><b>WARNING</b>: This API is a new and experimental and
    /// may suddenly change.<p/> 
    /// </summary>

    public class SnapshotDeletionPolicy : IndexDeletionPolicy
    {
		/// <summary>
		/// Records how many snapshots are held against each
		/// commit generation
		/// </summary>
		protected internal IDictionary<long, int> refCounts = new Dictionary<long, int>();

		/// <summary>Used to map gen to IndexCommit.</summary>
		/// <remarks>Used to map gen to IndexCommit.</remarks>
		protected internal IDictionary<long, IndexCommit> indexCommits = new Dictionary<long
			, IndexCommit>();

		/// <summary>
		/// Wrapped
		/// <see cref="IndexDeletionPolicy">IndexDeletionPolicy</see>
		/// 
		/// </summary>
		private IndexDeletionPolicy primary;

		/// <summary>
		/// Most recently committed
		/// <see cref="IndexCommit">IndexCommit</see>
		/// .
		/// </summary>
		protected internal IndexCommit lastCommit;

		/// <summary>Used to detect misuse</summary>
		private bool initCalled;

		/// <summary>
		/// Sole constructor, taking the incoming
		/// <see cref="IndexDeletionPolicy">IndexDeletionPolicy</see>
		/// to wrap.
		/// </summary>
		public SnapshotDeletionPolicy(IndexDeletionPolicy primary)
		{
			this.primary = primary;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void OnCommit<_T0>(IList<_T0> commits)
		{
			lock (this)
			{
				primary.OnCommit(WrapCommits(commits));
				lastCommit = commits[commits.Count - 1];
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void OnInit<T>(IList<T> commits)
		{
			lock (this)
			{
				initCalled = true;
				primary.OnInit(WrapCommits(commits));
				foreach (T commit in commits)
				{
					if (refCounts.ContainsKey(commit.Generation))
					{
						indexCommits[commit.Generation] = commit;
					}
				}
				if (commits.Any())
				{
					lastCommit = commits[commits.Count - 1];
				}
			}
		}

		/// <summary>Release a snapshotted commit.</summary>
		/// <remarks>Release a snapshotted commit.</remarks>
		/// <param name="commit">
		/// the commit previously returned by
		/// <see cref="Snapshot()">Snapshot()</see>
		/// </param>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void Release(IndexCommit commit)
		{
			lock (this)
			{
				long gen = commit.Generation;
				ReleaseGen(gen);
			}
		}

		/// <summary>Release a snapshot by generation.</summary>
		/// <remarks>Release a snapshot by generation.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual void ReleaseGen(long gen)
		{
			if (!initCalled)
			{
				throw new InvalidOperationException("this instance is not being used by IndexWriter; be sure to use the instance returned from writer.getConfig().getIndexDeletionPolicy()"
					);
			}
			int refCount = refCounts[gen];
			if (refCount == null)
			{
				throw new ArgumentException("commit gen=" + gen + " is not currently snapshotted"
					);
			}
			int refCountInt = refCount;
			//HM:revisit 
			//assert refCountInt > 0;
			refCountInt--;
			if (refCountInt == 0)
			{
				refCounts.Remove(gen);
				indexCommits.Remove(gen);
			}
			else
			{
				refCounts[gen] = refCountInt;
			}
		}

		/// <summary>
		/// Increments the refCount for this
		/// <see cref="IndexCommit">IndexCommit</see>
		/// .
		/// </summary>
		protected internal virtual void IncRef(IndexCommit ic)
		{
			lock (this)
			{
				long gen = ic.Generation;
				int refCount = refCounts[gen];
				int refCountInt;
				if (refCount == 0) //.NET Port. checking for 0 since it is not long?
				{
					indexCommits[gen] = lastCommit;
					refCountInt = 0;
				}
				else
				{
					refCountInt = refCount;
				}
				refCounts[gen] = refCountInt + 1;
			}
		}

		/// <summary>Snapshots the last commit and returns it.</summary>
		/// <remarks>
		/// Snapshots the last commit and returns it. Once a commit is 'snapshotted,' it is protected
		/// from deletion (as long as this
		/// <see cref="IndexDeletionPolicy">IndexDeletionPolicy</see>
		/// is used). The
		/// snapshot can be removed by calling
		/// <see cref="Release(IndexCommit)">Release(IndexCommit)</see>
		/// followed
		/// by a call to
		/// <see cref="IndexWriter.DeleteUnusedFiles()">IndexWriter.DeleteUnusedFiles()</see>
		/// .
		/// <p>
		/// <b>NOTE:</b> while the snapshot is held, the files it references will not
		/// be deleted, which will consume additional disk space in your index. If you
		/// take a snapshot at a particularly bad time (say just before you call
		/// forceMerge) then in the worst case this could consume an extra 1X of your
		/// total index size, until you release the snapshot.
		/// </remarks>
		/// <exception cref="System.InvalidOperationException">if this index does not have any commits yet
		/// 	</exception>
		/// <returns>
		/// the
		/// <see cref="IndexCommit">IndexCommit</see>
		/// that was snapshotted.
		/// </returns>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual IndexCommit Snapshot()
		{
			lock (this)
			{
				if (!initCalled)
				{
					throw new InvalidOperationException("this instance is not being used by IndexWriter; be sure to use the instance returned from writer.getConfig().getIndexDeletionPolicy()"
						);
				}
				if (lastCommit == null)
				{
					// No commit yet, eg this is a new IndexWriter:
					throw new InvalidOperationException("No index commit to snapshot");
				}
				IncRef(lastCommit);
				return lastCommit;
			}
		}

		/// <summary>Returns all IndexCommits held by at least one snapshot.</summary>
		/// <remarks>Returns all IndexCommits held by at least one snapshot.</remarks>
		public virtual IList<IndexCommit> GetSnapshots()
		{
			lock (this)
			{
				return new List<IndexCommit>(indexCommits.Values);
			}
		}

		/// <summary>Returns the total number of snapshots currently held.</summary>
		/// <remarks>Returns the total number of snapshots currently held.</remarks>
		public virtual int GetSnapshotCount()
		{
			lock (this)
			{
				int total = 0;
				foreach (int refCount in refCounts.Values)
				{
					total += refCount;
				}
				return total;
			}
		}

		/// <summary>
		/// Retrieve an
		/// <see cref="IndexCommit">IndexCommit</see>
		/// from its generation;
		/// returns null if this IndexCommit is not currently
		/// snapshotted
		/// </summary>
		public virtual IndexCommit GetIndexCommit(long gen)
		{
			lock (this)
			{
				return indexCommits[gen];
			}
		}

		public override object Clone()
		{
			lock (this)
			{
				var other = (SnapshotDeletionPolicy)base.Clone();
				other.primary = (IndexDeletionPolicy) this.primary.Clone();
				other.lastCommit = null;
				other.refCounts = new Dictionary<long, int>(refCounts);
				other.indexCommits = new Dictionary<long, IndexCommit>(indexCommits);
				return other;
			}
		}

		/// <summary>
		/// Wraps each
		/// <see cref="IndexCommit">IndexCommit</see>
		/// as a
		/// <see cref="SnapshotCommitPoint">SnapshotCommitPoint</see>
		/// .
		/// </summary>
		private IList<IndexCommit> WrapCommits<T>(IList<T> commits) where T:IndexCommit
		{
			IList<IndexCommit> wrappedCommits = new List<IndexCommit>(commits.Count);
			foreach (T ic in commits)
			{
				wrappedCommits.Add(new SnapshotCommitPoint(this, ic));
			}
			return wrappedCommits;
		}
        protected class SnapshotCommitPoint : IndexCommit
        {
            protected IndexCommit cp;
            private readonly SnapshotDeletionPolicy parent;

            public SnapshotCommitPoint(SnapshotDeletionPolicy parent, IndexCommit cp)
            {
                this.parent = parent;
                this.cp = cp;
            }

            public override string ToString()
            {
                return "SnapshotDeletionPolicy.SnapshotCommitPoint(" + cp + ")";
            }

            protected bool ShouldDelete(string segmentsFileName)
            {
                return !parent.refCounts.ContainsKey(this.cp.Generation);
            }

            public override void Delete()
            {
                lock (parent)
                {
                    // Suppress the delete request if this commit point is
                    // currently snapshotted.
                    if (ShouldDelete(SegmentsFileName))
                    {
                        cp.Delete();
                    }
                }
            }

            public override Directory Directory
            {
                get { return cp.Directory; }
            }

            public override ICollection<string> FileNames
            {
                get { return cp.FileNames; }
            }

            public override long Generation
            {
                get { return cp.Generation; }
            }

            public override string SegmentsFileName
            {
                get { return cp.SegmentsFileName; }
            }

            public override IDictionary<string, string> UserData
            {
                get { return cp.UserData; }
            }

            public override bool IsDeleted
            {
                get { return cp.IsDeleted; }
            }

            public override int SegmentCount
            {
                get { return cp.SegmentCount; }
            }
        }

    }
}