//using System;
//using System.IO;
//using System.Threading;
//using Lucene.Net.Index;
//using Lucene.Net.Search;

//namespace Lucene.Net.Search
//{
//    /// <summary>
//    /// Utility class that runs a thread to manage periodicc
//    /// reopens of a
//    /// <see cref="ReferenceManager{G}">ReferenceManager&lt;G&gt;</see>
//    /// , with methods to wait for a specific
//    /// index changes to become visible.  To use this class you
//    /// must first wrap your
//    /// <see cref="Lucene.Net.Index.IndexWriter">Lucene.Net.Index.IndexWriter
//    /// 	</see>
//    /// with a
//    /// <see cref="Lucene.Net.Index.TrackingIndexWriter">Lucene.Net.Index.TrackingIndexWriter
//    /// 	</see>
//    /// and always use it to make changes
//    /// to the index, saving the returned generation.  Then,
//    /// when a given search request needs to see a specific
//    /// index change, call the {#waitForGeneration} to wait for
//    /// that change to be visible.  Note that this will only
//    /// scale well if most searches do not need to wait for a
//    /// specific index generation.
//    /// </summary>
//    /// <lucene.experimental></lucene.experimental>
//    public class ControlledRealTimeReopenThread<T> : Thread, IDisposable
//    {
//        private readonly ReferenceManager<T> manager;

//        private readonly long targetMaxStaleNS;

//        private readonly long targetMinStaleNS;

//        private readonly TrackingIndexWriter writer;

//        private volatile bool finish;

//        private volatile long waitingGen;

//        private volatile long searchingGen;

//        private long refreshStartGen;

//        private readonly ReentrantLock reopenLock = new ReentrantLock();

//        private readonly Condition reopenCond = reopenLock.NewCondition();

//        /// <summary>
//        /// Create ControlledRealTimeReopenThread, to periodically
//        /// reopen the a
//        /// <see cref="ReferenceManager{G}">ReferenceManager&lt;G&gt;</see>
//        /// .
//        /// </summary>
//        /// <param name="targetMaxStaleSec">
//        /// Maximum time until a new
//        /// reader must be opened; this sets the upper bound
//        /// on how slowly reopens may occur, when no
//        /// caller is waiting for a specific generation to
//        /// become visible.
//        /// </param>
//        /// <param name="targetMinStaleSec">
//        /// Mininum time until a new
//        /// reader can be opened; this sets the lower bound
//        /// on how quickly reopens may occur, when a caller
//        /// is waiting for a specific generation to
//        /// become visible.
//        /// </param>
//        public ControlledRealTimeReopenThread(TrackingIndexWriter writer, ReferenceManager
//            <T> manager, double targetMaxStaleSec, double targetMinStaleSec)
//        {
//            if (targetMaxStaleSec < targetMinStaleSec)
//            {
//                throw new ArgumentException("targetMaxScaleSec (= " + targetMaxStaleSec + ") < targetMinStaleSec (="
//                     + targetMinStaleSec + ")");
//            }
//            this.writer = writer;
//            this.manager = manager;
//            this.targetMaxStaleNS = (long)(1000000000 * targetMaxStaleSec);
//            this.targetMinStaleNS = (long)(1000000000 * targetMinStaleSec);
//            manager.AddListener(new ControlledRealTimeReopenThread.HandleRefresh(this));
//        }

//        private class HandleRefresh : ReferenceManager.RefreshListener
//        {
//            public virtual void BeforeRefresh()
//            {
//            }

//            public virtual void AfterRefresh(bool didRefresh)
//            {
//                this._enclosing.RefreshDone();
//            }

//            internal HandleRefresh(ControlledRealTimeReopenThread<T> _enclosing)
//            {
//                this._enclosing = _enclosing;
//            }

//            private readonly ControlledRealTimeReopenThread<T> _enclosing;
//        }

//        private void RefreshDone()
//        {
//            lock (this)
//            {
//                searchingGen = refreshStartGen;
//                Sharpen.Runtime.NotifyAll(this);
//            }
//        }

//        public virtual void Close()
//        {
//            lock (this)
//            {
//                //System.out.println("NRT: set finish");
//                finish = true;
//                // So thread wakes up and notices it should finish:
//                reopenLock.Lock();
//                try
//                {
//                    reopenCond.Signal();
//                }
//                finally
//                {
//                    reopenLock.Unlock();
//                }
//                try
//                {
//                    Join();
//                }
//                catch (Exception ie)
//                {
//                    throw new ThreadInterruptedException(ie);
//                }
//                // Max it out so any waiting search threads will return:
//                searchingGen = long.MaxValue;
//                Sharpen.Runtime.NotifyAll(this);
//            }
//        }

//        /// <summary>
//        /// Waits for the target generation to become visible in
//        /// the searcher.
//        /// </summary>
//        /// <remarks>
//        /// Waits for the target generation to become visible in
//        /// the searcher.
//        /// If the current searcher is older than the
//        /// target generation, this method will block
//        /// until the searcher is reopened, by another via
//        /// <see cref="ReferenceManager{G}.MaybeRefresh()">ReferenceManager&lt;G&gt;.MaybeRefresh()
//        /// 	</see>
//        /// or until the
//        /// <see cref="ReferenceManager{G}">ReferenceManager&lt;G&gt;</see>
//        /// is closed.
//        /// </remarks>
//        /// <param name="targetGen">the generation to wait for</param>
//        /// <exception cref="System.Exception"></exception>
//        public virtual void WaitForGeneration(long targetGen)
//        {
//            WaitForGeneration(targetGen, -1);
//        }

//        /// <summary>
//        /// Waits for the target generation to become visible in
//        /// the searcher, up to a maximum specified milli-seconds.
//        /// </summary>
//        /// <remarks>
//        /// Waits for the target generation to become visible in
//        /// the searcher, up to a maximum specified milli-seconds.
//        /// If the current searcher is older than the target
//        /// generation, this method will block until the
//        /// searcher has been reopened by another thread via
//        /// <see cref="ReferenceManager{G}.MaybeRefresh()">ReferenceManager&lt;G&gt;.MaybeRefresh()
//        /// 	</see>
//        /// , the given waiting time has elapsed, or until
//        /// the
//        /// <see cref="ReferenceManager{G}">ReferenceManager&lt;G&gt;</see>
//        /// is closed.
//        /// <p>
//        /// NOTE: if the waiting time elapses before the requested target generation is
//        /// available the current
//        /// <see cref="SearcherManager">SearcherManager</see>
//        /// is returned instead.
//        /// </remarks>
//        /// <param name="targetGen">the generation to wait for</param>
//        /// <param name="maxMS">maximum milliseconds to wait, or -1 to wait indefinitely</param>
//        /// <returns>
//        /// true if the targetGeneration is now available,
//        /// or false if maxMS wait time was exceeded
//        /// </returns>
//        /// <exception cref="System.Exception"></exception>
//        public virtual bool WaitForGeneration(long targetGen, int maxMS)
//        {
//            lock (this)
//            {
//                long curGen = writer.GetGeneration();
//                if (targetGen > curGen)
//                {
//                    throw new ArgumentException("targetGen=" + targetGen + " was never returned by the ReferenceManager instance (current gen="
//                         + curGen + ")");
//                }
//                if (targetGen > searchingGen)
//                {
//                    // Notify the reopen thread that the waitingGen has
//                    // changed, so it may wake up and realize it should
//                    // not sleep for much or any longer before reopening:
//                    reopenLock.Lock();
//                    // Need to find waitingGen inside lock as its used to determine
//                    // stale time
//                    waitingGen = Math.Max(waitingGen, targetGen);
//                    try
//                    {
//                        reopenCond.Signal();
//                    }
//                    finally
//                    {
//                        reopenLock.Unlock();
//                    }
//                    long startMS = Runtime.NanoTime() / 1000000;
//                    while (targetGen > searchingGen)
//                    {
//                        if (maxMS < 0)
//                        {
//                            Sharpen.Runtime.Wait(this);
//                        }
//                        else
//                        {
//                            long msLeft = (startMS + maxMS) - (Runtime.NanoTime()) / 1000000;
//                            if (msLeft <= 0)
//                            {
//                                return false;
//                            }
//                            else
//                            {
//                                Sharpen.Runtime.Wait(this, msLeft);
//                            }
//                        }
//                    }
//                }
//                return true;
//            }
//        }

//        public override void Run()
//        {
//            // TODO: maybe use private thread ticktock timer, in
//            // case clock shift messes up nanoTime?
//            long lastReopenStartNS = Runtime.NanoTime();
//            //System.out.println("reopen: start");
//            while (!finish)
//            {
//                // TODO: try to guestimate how long reopen might
//                // take based on past data?
//                // Loop until we've waiting long enough before the
//                // next reopen:
//                while (!finish)
//                {
//                    // Need lock before finding out if has waiting
//                    reopenLock.Lock();
//                    try
//                    {
//                        // True if we have someone waiting for reopened searcher:
//                        bool hasWaiting = waitingGen > searchingGen;
//                        long nextReopenStartNS = lastReopenStartNS + (hasWaiting ? targetMinStaleNS : targetMaxStaleNS
//                            );
//                        long sleepNS = nextReopenStartNS - Runtime.NanoTime();
//                        if (sleepNS > 0)
//                        {
//                            reopenCond.AwaitNanos(sleepNS);
//                        }
//                        else
//                        {
//                            break;
//                        }
//                    }
//                    catch (Exception)
//                    {
//                        Sharpen.Thread.CurrentThread().Interrupt();
//                        return;
//                    }
//                    finally
//                    {
//                        reopenLock.Unlock();
//                    }
//                }
//                if (finish)
//                {
//                    break;
//                }
//                lastReopenStartNS = Runtime.NanoTime();
//                // Save the gen as of when we started the reopen; the
//                // listener (HandleRefresh above) copies this to
//                // searchingGen once the reopen completes:
//                refreshStartGen = writer.GetAndIncrementGeneration();
//                try
//                {
//                    manager.MaybeRefreshBlocking();
//                }
//                catch (IOException ioe)
//                {
//                    throw new RuntimeException(ioe);
//                }
//            }
//        }
//    }
//}
