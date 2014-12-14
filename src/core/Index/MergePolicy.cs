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
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Threading;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Index
{

    /// <summary> <p/>Expert: a MergePolicy determines the sequence of
    /// primitive merge operations to be used for overall merge
    /// and optimize operations.<p/>
    /// 
    /// <p/>Whenever the segments in an index have been altered by
    /// <see cref="IndexWriter" />, either the addition of a newly
    /// flushed segment, addition of many segments from
    /// addIndexes* calls, or a previous merge that may now need
    /// to cascade, <see cref="IndexWriter" /> invokes <see cref="FindMerges" />
    /// to give the MergePolicy a chance to pick
    /// merges that are now required.  This method returns a
    /// <see cref="MergeSpecification" /> instance describing the set of
    /// merges that should be done, or null if no merges are
    /// necessary.  When IndexWriter.optimize is called, it calls
    /// <see cref="FindMergesForOptimize" /> and the MergePolicy should
    /// then return the necessary merges.<p/>
    /// 
    /// <p/>Note that the policy can return more than one merge at
    /// a time.  In this case, if the writer is using <see cref="SerialMergeScheduler" />
    ///, the merges will be run
    /// sequentially but if it is using <see cref="ConcurrentMergeScheduler" />
    /// they will be run concurrently.<p/>
    /// 
    /// <p/>The default MergePolicy is <see cref="LogByteSizeMergePolicy" />
    ///.<p/>
    /// 
    /// <p/><b>NOTE:</b> This API is new and still experimental
    /// (subject to change suddenly in the next release)<p/>
    /// 
    /// <p/><b>NOTE</b>: This class typically requires access to
    /// package-private APIs (e.g. <c>SegmentInfos</c>) to do its job;
    /// if you implement your own MergePolicy, you'll need to put
    /// it in package Lucene.Net.Index in order to use
    /// these APIs.
    /// </summary>

    public abstract class MergePolicy : IDisposable, ICloneable
    {
        public abstract class DocMap
        {
            protected DocMap()
            {
            }

            public abstract int Map(int old);

            internal bool IsConsistent(int maxDoc)
            {
                FixedBitSet targets = new FixedBitSet(maxDoc);
                for (int i = 0; i < maxDoc; ++i)
                {
                    int target = Map(i);
                    if (target < 0 || target >= maxDoc)
                    {
                        //assert false : "out of range: " + target + " not in [0-" + maxDoc + "[";
                        return false;
                    }
                    else if (targets[target])
                    {
                        //assert false : target + " is already taken (" + i + ")";
                        return false;
                    }
                }
                return true;
            }
        }

        /// <summary>OneMerge provides the information necessary to perform
        /// an individual primitive merge operation, resulting in
        /// a single new segment.  The merge spec includes the
        /// subset of segments to be merged as well as whether the
        /// new segment should use the compound file format. 
        /// </summary>
        public class OneMerge
        {
			internal SegmentCommitInfo info;
            internal bool registerDone;           // used by IndexWriter
            internal long mergeGen;                  // used by IndexWriter
            internal bool isExternal;             // used by IndexWriter
            internal int maxNumSegments = -1;        // used by IndexWriter

            /** Estimated size in bytes of the merged segment. */
            public long estimatedMergeBytes;       // used by IndexWriter

            // Sum of sizeInBytes of all SegmentInfos; set by IW.mergeInit
            internal long totalMergeBytes;

            internal IList<SegmentReader> readers;        // used by IndexWriter

            /** Segments to be merged. */
			public readonly IList<SegmentCommitInfo> segments;

            /** Number of documents in the merged segment. */
            public readonly int totalDocCount;
            internal bool aborted;
            internal Exception error;
            internal bool paused;

			public OneMerge(IList<SegmentCommitInfo> segments)
            {
                if (0 == segments.Count)
                    throw new SystemException("segments must include at least one segment");
                // clone the list, as the in list may be based off original SegmentInfos and may be modified
                this.segments = new List<SegmentCommitInfo>(segments);
                int count = 0;
				foreach (SegmentCommitInfo info in segments)
                {
                    count += info.info.DocCount;
                }
                totalDocCount = count;
            }

            public virtual IList<AtomicReader> MergeReaders
            {
                get
                {
                    if (this.readers == null)
                    {
                        throw new InvalidOperationException("IndexWriter has not initialized readers from the segment infos yet");
                    }
                    List<AtomicReader> readers = new List<AtomicReader>(this.readers.Count);
                    foreach (AtomicReader reader in this.readers)
                    {
                        if (reader.NumDocs > 0)
                        {
                            readers.Add(reader);
                        }
                    }
                    return readers;
                }
            }

            public virtual SegmentCommitInfo Info
            {
                get { return info; }
                set { this.info = value; }
            }

            public virtual DocMap GetDocMap(MergeState mergeState)
            {
                return new AnonymousGetDocMap();
            }

            private sealed class AnonymousGetDocMap : DocMap
            {
                public override int Map(int docID)
                {
                    return docID;
                }
            }

            internal virtual Exception Exception
            {
                get
                {
                    lock (this)
                    {
                        return this.error;
                    }
                }
                set
                {
                    lock (this)
                    {
                        this.error = value;
                    }
                }
            }

            /// <summary>Mark this merge as aborted.  If this is called
            /// before the merge is committed then the merge will
            /// not be committed. 
            /// </summary>
            internal virtual void Abort()
            {
                lock (this)
                {
                    aborted = true;
                    Monitor.PulseAll(this);
                }
            }

            /// <summary>Returns true if this merge was aborted. </summary>
            internal virtual bool IsAborted
            {
                get
                {
                    lock (this)
                    {
                        return aborted;
                    }
                }
            }

            internal virtual void CheckAborted(Directory dir)
            {
                lock (this)
                {
                    if (aborted)
                        throw new MergeAbortedException("merge is aborted: " + SegString(dir));

                    while (paused)
                    {
                        try
                        {
                            // In theory we could wait() indefinitely, but we
                            // do 1000 msec, defensively
                            Monitor.Wait(this, 1000);
                        }
                        catch (ThreadInterruptedException ie)
                        {
                            throw;
                        }
                        if (aborted)
                        {
                            throw new MergeAbortedException("merge is aborted: " + SegString(dir));
                        }
                    }
                }
            }

            public virtual bool Pause
            {
                get
                {
                    lock (this)
                    {
                        return paused;
                    }
                }
                set
                {
                    lock (this)
                    {
                        this.paused = value;
                        if (!paused)
                        {
                            // Wakeup merge thread, if it's waiting
                            Monitor.PulseAll(this);
                        }
                    }
                }
            }

            internal virtual String SegString(Directory dir)
            {
                var b = new System.Text.StringBuilder();
                int numSegments = segments.Count;
                for (int i = 0; i < numSegments; i++)
                {
                    if (i > 0)
                        b.Append(' ');
                    b.Append(segments[i].ToString(dir, 0));
                }
                if (info != null)
                    b.Append(" into ").Append(info.info.name);
                if (maxNumSegments != -1)
                    b.Append(" [maxNumSegments=" + maxNumSegments + "]");
                if (aborted)
                {
                    b.Append(" [ABORTED]");
                }
                return b.ToString();
            }

            public virtual long TotalBytesSize
            {
                get
                {
                    return Interlocked.Read(ref totalMergeBytes);
                }
            }

            public virtual int TotalNumDocs
            {
                get
                {
                    return segments.Sum(i => i.info.DocCount);
                }
            }

            public virtual MergeInfo MergeInfo
            {
                get
                {
                    return new MergeInfo(totalDocCount, Interlocked.Read(ref estimatedMergeBytes), isExternal, maxNumSegments);
                }
            }
        }

        /// <summary> A MergeSpecification instance provides the information
        /// necessary to perform multiple merges.  It simply
        /// contains a list of <see cref="OneMerge" /> instances.
        /// </summary>
        public class MergeSpecification
        {
            /// <summary> The subset of segments to be included in the primitive merge.</summary>
            public IList<OneMerge> merges = new List<OneMerge>();

            public MergeSpecification()
            {
            }

            public virtual void Add(OneMerge merge)
            {
                merges.Add(merge);
            }

            public virtual String SegString(Directory dir)
            {
                var b = new System.Text.StringBuilder();
                b.Append("MergeSpec:\n");
                int count = merges.Count;
                for (int i = 0; i < count; i++)
                    b.Append("  ").Append(1 + i).Append(": ").Append(merges[i].SegString(dir));
                return b.ToString();
            }
        }

        /// <summary>Exception thrown if there are any problems while
        /// executing a merge. 
        /// </summary>
        [Serializable]
        public class MergeException : SystemException
        {
            private readonly Directory dir;

            public MergeException(string message, Directory dir)
                : base(message)
            {
                this.dir = dir;
            }

			public MergeException(Exception exc, Directory dir) : base(exc.Message)
            {
                this.dir = dir;
            }

            /// <summary>Returns the <see cref="Directory" /> of the index that hit
            /// the exception. 
            /// </summary>
            public virtual Directory Directory
            {
                get { return dir; }
            }
        }

        [Serializable]
        public class MergeAbortedException : System.IO.IOException
        {
            public MergeAbortedException()
                : base("merge is aborted")
            {
            }
            public MergeAbortedException(string message)
                : base(message)
            {
            }
        }

		protected internal const double DEFAULT_NO_CFS_RATIO = 1.0;
		protected internal const long DEFAULT_MAX_CFS_SEGMENT_SIZE = long.MaxValue;
        protected SetOnce<IndexWriter> writer;

		protected internal double noCFSRatio = DEFAULT_NO_CFS_RATIO;
		protected internal long maxCFSSegmentSize = DEFAULT_MAX_CFS_SEGMENT_SIZE;
        public object Clone()
        {
            MergePolicy clone = (MergePolicy)this.MemberwiseClone();
            
            clone.writer = new SetOnce<IndexWriter>();
            return clone;
        }

		/// <summary>Creates a new merge policy instance.</summary>
		/// <remarks>
		/// Creates a new merge policy instance. Note that if you intend to use it
		/// without passing it to
		/// <see cref="IndexWriter">IndexWriter</see>
		/// , you should call
		/// <see cref="SetIndexWriter(IndexWriter)">SetIndexWriter(IndexWriter)</see>
		/// .
		/// </remarks>
		public MergePolicy() : this(DEFAULT_NO_CFS_RATIO, DEFAULT_MAX_CFS_SEGMENT_SIZE)
		{
		}

		/// <summary>
		/// Creates a new merge policy instance with default settings for noCFSRatio
		/// and maxCFSSegmentSize.
		/// </summary>
		/// <remarks>
		/// Creates a new merge policy instance with default settings for noCFSRatio
		/// and maxCFSSegmentSize. This ctor should be used by subclasses using different
		/// defaults than the
		/// <see cref="MergePolicy">MergePolicy</see>
		/// </remarks>
		protected internal MergePolicy(double defaultNoCFSRatio, long defaultMaxCFSSegmentSize
			)
		{
			writer = new SetOnce<IndexWriter>();
			this.noCFSRatio = defaultNoCFSRatio;
			this.maxCFSSegmentSize = defaultMaxCFSSegmentSize;
		}

        public virtual void SetIndexWriter(IndexWriter writer)
        {
            this.writer.Set(writer);
        }

        /// <summary> Determine what set of merge operations are now necessary on the index.
        /// <see cref="IndexWriter" /> calls this whenever there is a change to the segments.
        /// This call is always synchronized on the <see cref="IndexWriter" /> instance so
        /// only one thread at a time will call this method.
        /// 
        /// </summary>
        /// <param name="segmentInfos">the total set of segments in the index
        /// </param>
        public abstract MergeSpecification FindMerges(MergeTrigger? mergeTrigger, SegmentInfos segmentInfos);

        
        public abstract MergeSpecification FindForcedMerges(SegmentInfos segmentInfos, int maxSegmentCount,
                                                            IDictionary<SegmentCommitInfo, bool> segmentsToMerge);

        /// <summary> Determine what set of merge operations is necessary in order to expunge all
        /// deletes from the index.
        /// 
        /// </summary>
        /// <param name="segmentInfos">the total set of segments in the index
        /// </param>
        public abstract MergeSpecification FindForcedDeletesMerges(SegmentInfos segmentInfos);
        
        /// <summary> Release all resources for the policy.</summary>
        public void Dispose()
        {
            Dispose(true);
        }

        protected abstract void Dispose(bool disposing);

        /// <summary> Returns true if a newly flushed (not from merge)
        /// segment should use the compound file format.
        /// </summary>
		public virtual bool UseCompoundFile(SegmentInfos infos, SegmentCommitInfo mergedInfo
			)
		{
			if (GetNoCFSRatio() == 0.0)
			{
				return false;
			}
			long mergedInfoSize = Size(mergedInfo);
			if (mergedInfoSize > maxCFSSegmentSize)
			{
				return false;
			}
			if (GetNoCFSRatio() >= 1.0)
			{
				return true;
			}
			long totalSize = 0;
			foreach (SegmentCommitInfo info in infos)
			{
				totalSize += Size(info);
			}
			return mergedInfoSize <= GetNoCFSRatio() * totalSize;
		}
		protected internal virtual long Size(SegmentCommitInfo info)
		{
			long byteSize = info.SizeInBytes();
			int delCount = writer.Get().NumDeletedDocs(info);
			double delRatio = (info.info.DocCount <= 0 ? 0.0f : ((float)delCount / (float)info.info.DocCount));
			//HM:revisit 
			//assert delRatio <= 1.0;
			return (info.info.DocCount <= 0 ? byteSize : (long)(byteSize * (1.0 - delRatio)));
		}
        public enum MergeTrigger
        {
            SEGMENT_FLUSH,
            FULL_FLUSH,
            EXPLICIT,
            MERGE_FINISHED,
            CLOSING
        }
		protected internal bool IsMerged(SegmentInfos infos, SegmentCommitInfo info)
		{
			IndexWriter w = writer.Get();
			//HM:revisit 
			//assert w != null;
			bool hasDeletions = w.NumDeletedDocs(info) > 0;
			return !hasDeletions && !info.info.HasSeparateNorms && info.info.dir == w.Directory && UseCompoundFile(infos, info) == info.info.UseCompoundFile;
		}
		public double GetNoCFSRatio()
		{
			return noCFSRatio;
		}
		public void SetNoCFSRatio(double noCFSRatio)
		{
			if (noCFSRatio < 0.0 || noCFSRatio > 1.0)
			{
				throw new ArgumentException("noCFSRatio must be 0.0 to 1.0 inclusive; got " + noCFSRatio
					);
			}
			this.noCFSRatio = noCFSRatio;
		}
		public double GetMaxCFSSegmentSizeMB()
		{
			return maxCFSSegmentSize / 1024.0 / 1024.0;
		}
		public void SetMaxCFSSegmentSizeMB(double v)
		{
			if (v < 0.0)
			{
				throw new ArgumentException("maxCFSSegmentSizeMB must be >=0 (got " + v + ")");
			}
			v *= 1024 * 1024;
			this.maxCFSSegmentSize = (v > long.MaxValue) ? long.MaxValue : (long)v;
		}
    }
}