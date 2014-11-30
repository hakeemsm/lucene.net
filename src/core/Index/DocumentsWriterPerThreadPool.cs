using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using FieldNumbers = Lucene.Net.Index.FieldInfos.FieldNumbers;

namespace Lucene.Net.Index
{
    public class DocumentsWriterPerThreadPool : ICloneable
    {
        [System.Serializable]
        public sealed class ThreadState : ReentrantLock
        {
            internal DocumentsWriterPerThread dwpt;
            // TODO this should really be part of DocumentsWriterFlushControl
            // write access guarded by DocumentsWriterFlushControl
            internal volatile bool flushPending = false;
            // TODO this should really be part of DocumentsWriterFlushControl
            // write access guarded by DocumentsWriterFlushControl
            internal long bytesUsed = 0;
            // guarded by Reentrant lock
            private bool isActive = true;

            public ThreadState(DocumentsWriterPerThread dpwt)
            {
                this.dwpt = dpwt;
            }

            internal void Deactivate()
            {
                //HM:revisit 
                //assert this.isHeldByCurrentThread();
                isActive = false;
                Reset();
            }

            internal void Reset()
            {
                //assert this.isHeldByCurrentThread();
                this.dwpt = null;
                this.bytesUsed = 0;
                this.flushPending = false;
            }

            internal bool IsActive
            {
                get
                {
                    //assert this.isHeldByCurrentThread();
                    return isActive;
                }
            }

            internal bool IsInitialized()
            {
                //HM:revisit 
                //assert this.isHeldByCurrentThread();
                return IsActive && dwpt != null;
            }
            public long BytesUsedPerThread
            {
                get
                {
                    //assert this.isHeldByCurrentThread();
                    // public for FlushPolicy
                    return bytesUsed;
                }
            }

            public DocumentsWriterPerThread DocumentsWriterPerThread
            {
                get
                {
                    //assert this.isHeldByCurrentThread();
                    // public for FlushPolicy
                    return dwpt;
                }
            }

            public bool IsFlushPending
            {
                get
                {
                    return flushPending;
                }
            }
        }

        private ThreadState[] threadStates;
        private volatile int numThreadStatesActive;

        private readonly DocumentsWriterPerThreadPool.ThreadState[] freeList;

        private int freeCount;
        public DocumentsWriterPerThreadPool(int maxNumThreadStates)
        {
            if (maxNumThreadStates < 1)
            {
                throw new ArgumentException("maxNumThreadStates must be >= 1 but was: " + maxNumThreadStates);
            }
            threadStates = new ThreadState[maxNumThreadStates];
            numThreadStatesActive = 0;
            for (int i = 0; i < threadStates.Length; i++)
            {
                threadStates[i] = new DocumentsWriterPerThreadPool.ThreadState(null);
            }
            freeList = new DocumentsWriterPerThreadPool.ThreadState[maxNumThreadStates];
        }


        public virtual object Clone()
        {
            // We should only be cloned before being used:
            if (numThreadStatesActive != 0)
            {
                throw new InvalidOperationException("clone this object before it is used!");
            }
            return new DocumentsWriterPerThreadPool(threadStates.Length);
        }

        internal virtual int MaxThreadStates
        {
            get { return threadStates.Length; }
        }

        internal virtual int ActiveThreadState
        {
            get { return numThreadStatesActive; }
        }

        internal virtual ThreadState NewThreadState()
        {
            lock (this)
            {
                if (numThreadStatesActive < threadStates.Length)
                {
                    ThreadState threadState = threadStates[numThreadStatesActive];
                    threadState.Lock(); // lock so nobody else will get this ThreadState
                    bool unlock = true;
                    try
                    {
                        if (threadState.IsActive)
                        {
                            // unreleased thread states are deactivated during DW#close()
                            numThreadStatesActive++; // increment will publish the ThreadState
                            //assert threadState.dwpt != null;
                            unlock = false;
                            return threadState;
                        }
                        // unlock since the threadstate is not active anymore - we are closed!
                        //assert assertUnreleasedThreadStatesInactive();
                        return null;
                    }
                    finally
                    {
                        if (unlock)
                        {
                            // in any case make sure we unlock if we fail 
                            threadState.Unlock();
                        }
                    }
                }
                return null;
            }
        }

        private bool AssertUnreleasedThreadStatesInactive()
        {
            for (int i = numThreadStatesActive; i < threadStates.Length; i++)
            {
                //assert threadStates[i].tryLock() : "unreleased threadstate should not be locked";
                try
                {
                    //assert !threadStates[i].isActive() : "expected unreleased thread state to be inactive";
                }
                finally
                {
                    threadStates[i].Unlock();
                }
            }
            return true;
        }

        internal virtual void DeactivateUnreleasedStates()
        {
            for (int i = numThreadStatesActive; i < threadStates.Length; i++)
            {
                ThreadState threadState = threadStates[i];
                threadState.Lock();
                try
                {
                    threadState.Deactivate();
                }
                finally
                {
                    threadState.Unlock();
                }
            }
            Monitor.PulseAll(this);
        }

        internal DocumentsWriterPerThread Reset(DocumentsWriterPerThreadPool.ThreadState
            threadState, bool closed)
        {
            //assert threadState.isHeldByCurrentThread();
            //assert globalFieldMap.get() != null;
            DocumentsWriterPerThread dwpt = threadState.dwpt;
            if (!closed)
            {
                threadState.Reset();
            }
            else
            {
                threadState.Deactivate();
            }
            return dwpt;
        }

        internal virtual void Recycle(DocumentsWriterPerThread dwpt)
        {
            // don't recycle DWPT by default
        }

        internal ThreadState GetAndLock(Thread requestingThread, DocumentsWriter documentsWriter)
        {
            ThreadState threadState;
            lock (this)
            {
                while (true)
                {
                    if (freeCount > 0)
                    {
                        // Important that we are LIFO here! This way if number of concurrent indexing threads was once high, but has now reduced, we only use a
                        // limited number of thread states:
                        threadState = freeList[freeCount - 1];
                        if (threadState.dwpt == null)
                        {
                            // This thread-state is not initialized, e.g. it
                            // was just flushed. See if we can instead find
                            // another free thread state that already has docs
                            // indexed. This way if incoming thread concurrency
                            // has decreased, we don't leave docs
                            // indefinitely buffered, tying up RAM.  This
                            // will instead get those thread states flushed,
                            // freeing up RAM for larger segment flushes:
                            for (int i = 0; i < freeCount; i++)
                            {
                                if (freeList[i].dwpt != null)
                                {
                                    // Use this one instead, and swap it with
                                    // the un-initialized one:
                                    DocumentsWriterPerThreadPool.ThreadState ts = freeList[i];
                                    freeList[i] = threadState;
                                    threadState = ts;
                                    break;
                                }
                            }
                        }
                        freeCount--;
                        break;
                    }
                    if (numThreadStatesActive < threadStates.Length)
                    {
                        // ThreadState is already locked before return by this method:
                        return NewThreadState();
                    }
                    // Wait until a thread state frees up:
                    try
                    {
                        Monitor.Wait(this);
                    }
                    catch (Exception ie)
                    {
                        throw new ThreadInterruptedException(ie.Message);
                    }
                }
            }
            // This could take time, e.g. if the threadState is [briefly] checked for flushing:
            threadState.Lock();
            return threadState;
        }

        internal void Release(ThreadState state)
        {
            state.Unlock();
            lock (this)
            {
                //HM:revisit 
                //assert freeCount < freeList.length;
                freeList[freeCount++] = state;
                // In case any thread is waiting, wake one of them up since we just released a thread state; notify() should be sufficient but we do
                // notifyAll defensively:
                Monitor.PulseAll(this);
            }
        }
        internal virtual ThreadState GetThreadState(int ord)
        {
            return threadStates[ord];
        }

        internal ThreadState MinContendedThreadState
        {
            get
            {
                ThreadState minThreadState = null;
                int limit = numThreadStatesActive;
                for (int i = 0; i < limit; i++)
                {
                    ThreadState state = threadStates[i];
                    if (minThreadState == null || state.QueueLength < minThreadState.QueueLength)
                    {
                        minThreadState = state;
                    }
                }
                return minThreadState;
            }
        }

        internal int NumDeactivatedThreadStates()
        {
            int count = 0;
            for (int i = 0; i < threadStates.Length; i++)
            {
                ThreadState threadState = threadStates[i];
                threadState.Lock();
                try
                {
                    if (!threadState.IsActive)
                    {
                        count++;
                    }
                }
                finally
                {
                    threadState.Unlock();
                }
            }
            return count;
        }
        internal void DeactivateThreadState(ThreadState threadState)
        {
            //assert threadState.isActive();
            threadState.Deactivate();
        }

    }
}
