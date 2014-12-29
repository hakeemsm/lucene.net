using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Lucene.Net.Index;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Support;
using Lucene.Net.TestFramework;
using NUnit.Framework;

namespace Lucene.Net.Test.Index
{
	/// <summary>
	/// Tests for
	/// <see cref="DocumentsWriterStallControl">DocumentsWriterStallControl</see>
	/// </summary>
	[TestFixture]
    public class TestDocumentsWriterStallControl : LuceneTestCase
	{
		[Test]
		public virtual void TestSimpleStall()
		{
			DocumentsWriterStallControl ctrl = new DocumentsWriterStallControl();
			ctrl.UpdateStalled(false);
			Thread[] waitThreads = WaitThreads(AtLeast(1), ctrl);
			Start(waitThreads);
			IsFalse(ctrl.HasBlocked);
			IsFalse(ctrl.AnyStalledThreads);
			Join(waitThreads);
			// now stall threads and wake them up again
			ctrl.UpdateStalled(true);
			waitThreads = WaitThreads(AtLeast(1), ctrl);
			Start(waitThreads);
			AwaitState(ThreadState.WaitSleepJoin, waitThreads);
			IsTrue(ctrl.HasBlocked);
			IsTrue(ctrl.AnyStalledThreads);
			ctrl.UpdateStalled(false);
			IsFalse(ctrl.AnyStalledThreads);
			Join(waitThreads);
		}

		[Test]
		public virtual void TestRandom()
		{
			DocumentsWriterStallControl ctrl = new DocumentsWriterStallControl();
			ctrl.UpdateStalled(false);
			Thread[] stallThreads = new Thread[AtLeast(3)];
			for (int i = 0; i < stallThreads.Length; i++)
			{
				int stallProbability = 1 + Random().Next(10);
				stallThreads[i] = new Thread(() =>
				{
                    int iters = AtLeast(1000);
                    for (int j = 0; j < iters; j++)
                    {
                        ctrl.UpdateStalled(Random().Next(stallProbability) == 0);
                        if (Random().Next(5) == 0)
                        {
                            ctrl.WaitIfStalled();
                        }
                    }
				});
			}
			// thread 0 only updates
			Start(stallThreads);
			long time = DateTime.Now.CurrentTimeMillis();
			while ((DateTime.Now.CurrentTimeMillis() - time) < 100 * 1000 && !Terminated(stallThreads
				))
			{
				ctrl.UpdateStalled(false);
				if (Random().NextBoolean())
				{
					Thread.Yield();
				}
				else
				{
					Thread.Sleep(1);
				}
			}
			Join(stallThreads);
		}

	    [Test]
		public virtual void TestAccquireReleaseRace()
		{
			DocumentsWriterStallControl ctrl = new DocumentsWriterStallControl();
			ctrl.UpdateStalled(false);
			AtomicBoolean stop = new AtomicBoolean(false);
			AtomicBoolean checkPoint = new AtomicBoolean(true);
			int numStallers = AtLeast(1);
			int numReleasers = AtLeast(1);
			int numWaiters = AtLeast(1);
			var sync = new Synchronizer
				(numStallers + numReleasers, numStallers + numReleasers + numWaiters);
			Thread[] threads = new Thread[numReleasers + numStallers + numWaiters];
	        var exceptions = new ConcurrentHashSet<Exception>();
			for (int i = 0; i < numReleasers; i++)
			{
				threads[i] = new Thread(new Updater(stop, checkPoint, ctrl, 
					sync, true, exceptions).Run);
			}
			for (int i_1 = numReleasers; i_1 < numReleasers + numStallers; i_1++)
			{
				threads[i_1] = new Thread(new Updater(stop, checkPoint, ctrl
					, sync, false, exceptions).Run);
			}
			for (int i_2 = numReleasers + numStallers; i_2 < numReleasers + numStallers + numWaiters
				; i_2++)
			{
				threads[i_2] = new Thread(new WaiterThread2(stop, checkPoint, ctrl, 
					sync, exceptions).Run);
			}
			Start(threads);
			int iters = AtLeast(10000);
			float checkPointProbability = TEST_NIGHTLY ? 0.5f : 0.1f;
			for (int i_3 = 0; i_3 < iters; i_3++)
			{
				if (checkPoint.Get())
				{
					AssertTrue("timed out waiting for update threads - deadlock?", 
						sync.updateJoin.Signal(TimeSpan.FromTicks(10).Seconds));
					if (exceptions.Any())
					{
						foreach (Exception throwable in exceptions)
						{
							throwable.printStackTrace();
						}
						Fail("got exceptions in threads");
					}
					if (ctrl.HasBlocked && ctrl.IsHealthy)
					{
						AssertState(numReleasers, numStallers, numWaiters, threads, ctrl);
					}
					checkPoint.Set(false);
				    sync.waiter.Signal();
					sync.leftCheckpoint.Wait();
				}
				IsFalse(checkPoint.Get());
				AreEqual(0, sync.waiter.CurrentCount);
				if (checkPointProbability >= Random().NextDouble())
				{
					sync.Reset(numStallers + numReleasers, numStallers + numReleasers + numWaiters);
					checkPoint.Set(true);
				}
			}
			if (!checkPoint.Get())
			{
				sync.Reset(numStallers + numReleasers, numStallers + numReleasers + numWaiters);
				checkPoint.Set(true);
			}
			IsTrue(sync.updateJoin.Wait(new TimeSpan(0,0,10)));
			AssertState(numReleasers, numStallers, numWaiters, threads, ctrl);
			checkPoint.Set(false);
			stop.Set(true);
			sync.waiter.Signal();
			sync.leftCheckpoint.Wait();
			for (int i_4 = 0; i_4 < threads.Length; i_4++)
			{
				ctrl.UpdateStalled(false);
				threads[i_4].Join(2000);
				if (threads[i_4].IsAlive)
				{
					if (threads[i_4].ThreadState == ThreadState.WaitSleepJoin)
					{
						Fail("waiter is not released - anyThreadsStalled: " + ctrl
							.AnyStalledThreads);
					}
				}
			}
		}

		/// <exception cref="System.Exception"></exception>
		private void AssertState(int numReleasers, int numStallers, int numWaiters, Thread
			[] threads, DocumentsWriterStallControl ctrl)
		{
			int millisToSleep = 100;
			while (true)
			{
			    if (ctrl.HasBlocked && ctrl.IsHealthy)
				{
					for (int n = numReleasers + numStallers; n < numReleasers + numStallers + numWaiters
						; n++)
					{
						if (ctrl.IsThreadQueued(threads[n]))
						{
						    if (millisToSleep < 60000)
							{
								Thread.Sleep(millisToSleep);
								millisToSleep *= 2;
								break;
							}
						    Fail("control claims no stalled threads but waiter seems to be blocked "
						        );
						}
					}
				}
			    break;
			}
		}

		public class WaiterThread2
		{
			private TestDocumentsWriterStallControl.Synchronizer sync;

			private DocumentsWriterStallControl ctrl;

			private AtomicBoolean checkPoint;

			private AtomicBoolean stop;

			private ConcurrentHashSet<Exception> exceptions;

			public WaiterThread2(AtomicBoolean stop, AtomicBoolean checkPoint, DocumentsWriterStallControl
				 ctrl, TestDocumentsWriterStallControl.Synchronizer sync, ConcurrentHashSet<Exception> exceptions
				)
			{
				this.stop = stop;
				this.checkPoint = checkPoint;
				this.ctrl = ctrl;
				this.sync = sync;
				this.exceptions = exceptions;
			}

			public void Run()
			{
				try
				{
					while (!stop.Get())
					{
						ctrl.WaitIfStalled();
						if (checkPoint.Get())
						{
							try
							{
								IsTrue(sync.Await());
							}
							catch (Exception e)
							{
								System.Console.Out.WriteLine("[Waiter] got interrupted - wait count: " + sync.waiter
									.Signal());
								throw new ThreadInterruptedException();
							}
						}
					}
				}
				catch (Exception e)
				{
					e.printStackTrace();
					exceptions.Add(e);
				}
			}
		}

		public class Updater
		{
			private Synchronizer sync;

			private DocumentsWriterStallControl ctrl;

			private AtomicBoolean checkPoint;

			private AtomicBoolean stop;

			private bool release;

			private ConcurrentHashSet<Exception> exceptions;

			public Updater(AtomicBoolean stop, AtomicBoolean checkPoint, DocumentsWriterStallControl
				 ctrl, Synchronizer sync, bool release, ConcurrentHashSet<Exception> exceptions) 
			{
				this.stop = stop;
				this.checkPoint = checkPoint;
				this.ctrl = ctrl;
				this.sync = sync;
				this.release = release;
				this.exceptions = exceptions;
			}

			public void Run()
			{
				try
				{
					while (!stop.Get())
					{
						int internalIters = release && Random().NextBoolean() ? AtLeast(5) : 1;
						for (int i = 0; i < internalIters; i++)
						{
							ctrl.UpdateStalled(Random().NextBoolean());
						}
						if (checkPoint.Get())
						{
							sync.updateJoin.Signal();
							try
							{
								IsTrue(sync.Await());
							}
							catch (Exception e)
							{
								System.Console.Out.WriteLine("[Updater] got interrupted - wait count: " + sync.waiter
									.CurrentCount);
								throw new ThreadInterruptedException();
							}
							sync.leftCheckpoint.Signal();
						}
						if (Random().NextBoolean())
						{
							Thread.Yield();
						}
					}
				}
				catch (Exception e)
				{
					e.printStackTrace();
					exceptions.Add(e);
				}
				sync.updateJoin.Signal();
			}
		}

		public static bool Terminated(Thread[] threads)
		{
		    return threads.All(thread => ThreadState.Aborted == thread.ThreadState);
		}

	    /// <exception cref="System.Exception"></exception>
		public static void Start(Thread[] tostart)
		{
			foreach (Thread thread in tostart)
			{
				thread.Start();
			}
			Thread.Sleep(1);
		}

		// let them start
		/// <exception cref="System.Exception"></exception>
		public static void Join(Thread[] toJoin)
		{
			foreach (Thread thread in toJoin)
			{
				thread.Join();
			}
		}

		public static Thread[] WaitThreads(int num, DocumentsWriterStallControl ctrl)
		{
			Thread[] array = new Thread[num];
			for (int i = 0; i < array.Length; i++)
			{
			    array[i] = new Thread(new WaiterThread(ctrl).Run);
			}
			return array;
		}

		private sealed class WaiterThread
		{
			public WaiterThread(DocumentsWriterStallControl ctrl)
			{
				this.ctrl = ctrl;
			}

			public void Run()
			{
				ctrl.WaitIfStalled();
			}

			private readonly DocumentsWriterStallControl ctrl;
		}

		/// <summary>
		/// Waits for all incoming threads to be in wait()
		/// methods.
		/// </summary>
		/// <remarks>
		/// Waits for all incoming threads to be in wait()
		/// methods.
		/// </remarks>
		/// <exception cref="System.Exception"></exception>
		public static void AwaitState(ThreadState state, params Thread[]
			 threads)
		{
			while (true)
			{
				bool done = true;
				foreach (Thread thread in threads)
				{
					if (thread.ThreadState != state)
					{
						done = false;
						break;
					}
				}
				if (done)
				{
					return;
				}
				if (Random().NextBoolean())
				{
					Thread.Yield();
				}
				else
				{
					Thread.Sleep(1);
				}
			}
		}

	    public sealed class Synchronizer
		{
			internal volatile CountdownEvent waiter;

			internal volatile CountdownEvent updateJoin;

			internal volatile CountdownEvent leftCheckpoint;

			public Synchronizer(int numUpdater, int numThreads)
			{
				Reset(numUpdater, numThreads);
			}

			public void Reset(int numUpdaters, int numThreads)
			{
				this.waiter = new CountdownEvent(1);
				this.updateJoin = new CountdownEvent(numUpdaters);
				this.leftCheckpoint = new CountdownEvent(numUpdaters);
			}

			/// <exception cref="System.Exception"></exception>
			public bool Await()
			{
				return waiter.Wait(TimeSpan.FromSeconds(10));
			}
		}
	}
}
