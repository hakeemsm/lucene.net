/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	/// <summary>
	/// Tests for
	/// <see cref="DocumentsWriterStallControl">DocumentsWriterStallControl</see>
	/// </summary>
	public class TestDocumentsWriterStallControl : LuceneTestCase
	{
		/// <exception cref="System.Exception"></exception>
		public virtual void TestSimpleStall()
		{
			DocumentsWriterStallControl ctrl = new DocumentsWriterStallControl();
			ctrl.UpdateStalled(false);
			Sharpen.Thread[] waitThreads = WaitThreads(AtLeast(1), ctrl);
			Start(waitThreads);
			IsFalse(ctrl.HasBlocked());
			IsFalse(ctrl.AnyStalledThreads());
			Join(waitThreads);
			// now stall threads and wake them up again
			ctrl.UpdateStalled(true);
			waitThreads = WaitThreads(AtLeast(1), ctrl);
			Start(waitThreads);
			AwaitState(Sharpen.Thread.State.WAITING, waitThreads);
			IsTrue(ctrl.HasBlocked());
			IsTrue(ctrl.AnyStalledThreads());
			ctrl.UpdateStalled(false);
			IsFalse(ctrl.AnyStalledThreads());
			Join(waitThreads);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestRandom()
		{
			DocumentsWriterStallControl ctrl = new DocumentsWriterStallControl();
			ctrl.UpdateStalled(false);
			Sharpen.Thread[] stallThreads = new Sharpen.Thread[AtLeast(3)];
			for (int i = 0; i < stallThreads.Length; i++)
			{
				int stallProbability = 1 + Random().Next(10);
				stallThreads[i] = new _Thread_64(ctrl, stallProbability);
			}
			// thread 0 only updates
			Start(stallThreads);
			long time = Runtime.CurrentTimeMillis();
			while ((Runtime.CurrentTimeMillis() - time) < 100 * 1000 && !Terminated(stallThreads
				))
			{
				ctrl.UpdateStalled(false);
				if (Random().NextBoolean())
				{
					Sharpen.Thread.Yield();
				}
				else
				{
					Sharpen.Thread.Sleep(1);
				}
			}
			Join(stallThreads);
		}

		private sealed class _Thread_64 : Sharpen.Thread
		{
			public _Thread_64(DocumentsWriterStallControl ctrl, int stallProbability)
			{
				this.ctrl = ctrl;
				this.stallProbability = stallProbability;
			}

			public override void Run()
			{
				int iters = LuceneTestCase.AtLeast(1000);
				for (int j = 0; j < iters; j++)
				{
					ctrl.UpdateStalled(LuceneTestCase.Random().Next(stallProbability) == 0);
					if (LuceneTestCase.Random().Next(5) == 0)
					{
						ctrl.WaitIfStalled();
					}
				}
			}

			private readonly DocumentsWriterStallControl ctrl;

			private readonly int stallProbability;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestAccquireReleaseRace()
		{
			DocumentsWriterStallControl ctrl = new DocumentsWriterStallControl();
			ctrl.UpdateStalled(false);
			AtomicBoolean stop = new AtomicBoolean(false);
			AtomicBoolean checkPoint = new AtomicBoolean(true);
			int numStallers = AtLeast(1);
			int numReleasers = AtLeast(1);
			int numWaiters = AtLeast(1);
			TestDocumentsWriterStallControl.Synchronizer sync = new TestDocumentsWriterStallControl.Synchronizer
				(numStallers + numReleasers, numStallers + numReleasers + numWaiters);
			Sharpen.Thread[] threads = new Sharpen.Thread[numReleasers + numStallers + numWaiters
				];
			IList<Exception> exceptions = Collections.SynchronizedList(new AList<Exception>()
				);
			for (int i = 0; i < numReleasers; i++)
			{
				threads[i] = new TestDocumentsWriterStallControl.Updater(stop, checkPoint, ctrl, 
					sync, true, exceptions);
			}
			for (int i_1 = numReleasers; i_1 < numReleasers + numStallers; i_1++)
			{
				threads[i_1] = new TestDocumentsWriterStallControl.Updater(stop, checkPoint, ctrl
					, sync, false, exceptions);
			}
			for (int i_2 = numReleasers + numStallers; i_2 < numReleasers + numStallers + numWaiters
				; i_2++)
			{
				threads[i_2] = new TestDocumentsWriterStallControl.Waiter(stop, checkPoint, ctrl, 
					sync, exceptions);
			}
			Start(threads);
			int iters = AtLeast(10000);
			float checkPointProbability = TEST_NIGHTLY ? 0.5f : 0.1f;
			for (int i_3 = 0; i_3 < iters; i_3++)
			{
				if (checkPoint.Get())
				{
					IsTrue("timed out waiting for update threads - deadlock?", 
						sync.updateJoin.Await(10, TimeUnit.SECONDS));
					if (!exceptions.IsEmpty())
					{
						foreach (Exception throwable in exceptions)
						{
							Sharpen.Runtime.PrintStackTrace(throwable);
						}
						Fail("got exceptions in threads");
					}
					if (ctrl.HasBlocked() && ctrl.IsHealthy())
					{
						AssertState(numReleasers, numStallers, numWaiters, threads, ctrl);
					}
					checkPoint.Set(false);
					sync.waiter.CountDown();
					sync.leftCheckpoint.Await();
				}
				IsFalse(checkPoint.Get());
				AreEqual(0, sync.waiter.GetCount());
				if (checkPointProbability >= Random().NextFloat())
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
			IsTrue(sync.updateJoin.Await(10, TimeUnit.SECONDS));
			AssertState(numReleasers, numStallers, numWaiters, threads, ctrl);
			checkPoint.Set(false);
			stop.Set(true);
			sync.waiter.CountDown();
			sync.leftCheckpoint.Await();
			for (int i_4 = 0; i_4 < threads.Length; i_4++)
			{
				ctrl.UpdateStalled(false);
				threads[i_4].Join(2000);
				if (threads[i_4].IsAlive() && threads[i_4] is TestDocumentsWriterStallControl.Waiter)
				{
					if (threads[i_4].GetState() == Sharpen.Thread.State.WAITING)
					{
						Fail("waiter is not released - anyThreadsStalled: " + ctrl
							.AnyStalledThreads());
					}
				}
			}
		}

		/// <exception cref="System.Exception"></exception>
		private void AssertState(int numReleasers, int numStallers, int numWaiters, Sharpen.Thread
			[] threads, DocumentsWriterStallControl ctrl)
		{
			int millisToSleep = 100;
			while (true)
			{
				if (ctrl.HasBlocked() && ctrl.IsHealthy())
				{
					for (int n = numReleasers + numStallers; n < numReleasers + numStallers + numWaiters
						; n++)
					{
						if (ctrl.IsThreadQueued(threads[n]))
						{
							if (millisToSleep < 60000)
							{
								Sharpen.Thread.Sleep(millisToSleep);
								millisToSleep *= 2;
								break;
							}
							else
							{
								Fail("control claims no stalled threads but waiter seems to be blocked "
									);
							}
						}
					}
					break;
				}
				else
				{
					break;
				}
			}
		}

		public class Waiter : Sharpen.Thread
		{
			private TestDocumentsWriterStallControl.Synchronizer sync;

			private DocumentsWriterStallControl ctrl;

			private AtomicBoolean checkPoint;

			private AtomicBoolean stop;

			private IList<Exception> exceptions;

			public Waiter(AtomicBoolean stop, AtomicBoolean checkPoint, DocumentsWriterStallControl
				 ctrl, TestDocumentsWriterStallControl.Synchronizer sync, IList<Exception> exceptions
				) : base("waiter")
			{
				this.stop = stop;
				this.checkPoint = checkPoint;
				this.ctrl = ctrl;
				this.sync = sync;
				this.exceptions = exceptions;
			}

			public override void Run()
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
									.GetCount());
								throw new ThreadInterruptedException(e);
							}
						}
					}
				}
				catch (Exception e)
				{
					Sharpen.Runtime.PrintStackTrace(e);
					exceptions.AddItem(e);
				}
			}
		}

		public class Updater : Sharpen.Thread
		{
			private TestDocumentsWriterStallControl.Synchronizer sync;

			private DocumentsWriterStallControl ctrl;

			private AtomicBoolean checkPoint;

			private AtomicBoolean stop;

			private bool release;

			private IList<Exception> exceptions;

			public Updater(AtomicBoolean stop, AtomicBoolean checkPoint, DocumentsWriterStallControl
				 ctrl, TestDocumentsWriterStallControl.Synchronizer sync, bool release, IList<Exception
				> exceptions) : base("updater")
			{
				this.stop = stop;
				this.checkPoint = checkPoint;
				this.ctrl = ctrl;
				this.sync = sync;
				this.release = release;
				this.exceptions = exceptions;
			}

			public override void Run()
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
							sync.updateJoin.CountDown();
							try
							{
								IsTrue(sync.Await());
							}
							catch (Exception e)
							{
								System.Console.Out.WriteLine("[Updater] got interrupted - wait count: " + sync.waiter
									.GetCount());
								throw new ThreadInterruptedException(e);
							}
							sync.leftCheckpoint.CountDown();
						}
						if (Random().NextBoolean())
						{
							Sharpen.Thread.Yield();
						}
					}
				}
				catch (Exception e)
				{
					Sharpen.Runtime.PrintStackTrace(e);
					exceptions.AddItem(e);
				}
				sync.updateJoin.CountDown();
			}
		}

		public static bool Terminated(Sharpen.Thread[] threads)
		{
			foreach (Sharpen.Thread thread in threads)
			{
				if (Sharpen.Thread.State.TERMINATED != thread.GetState())
				{
					return false;
				}
			}
			return true;
		}

		/// <exception cref="System.Exception"></exception>
		public static void Start(Sharpen.Thread[] tostart)
		{
			foreach (Sharpen.Thread thread in tostart)
			{
				thread.Start();
			}
			Sharpen.Thread.Sleep(1);
		}

		// let them start
		/// <exception cref="System.Exception"></exception>
		public static void Join(Sharpen.Thread[] toJoin)
		{
			foreach (Sharpen.Thread thread in toJoin)
			{
				thread.Join();
			}
		}

		public static Sharpen.Thread[] WaitThreads(int num, DocumentsWriterStallControl ctrl
			)
		{
			Sharpen.Thread[] array = new Sharpen.Thread[num];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = new _Thread_323(ctrl);
			}
			return array;
		}

		private sealed class _Thread_323 : Sharpen.Thread
		{
			public _Thread_323(DocumentsWriterStallControl ctrl)
			{
				this.ctrl = ctrl;
			}

			public override void Run()
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
		public static void AwaitState(Sharpen.Thread.State state, params Sharpen.Thread[]
			 threads)
		{
			while (true)
			{
				bool done = true;
				foreach (Sharpen.Thread thread in threads)
				{
					if (thread.GetState() != state)
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
					Sharpen.Thread.Yield();
				}
				else
				{
					Sharpen.Thread.Sleep(1);
				}
			}
		}

		private sealed class Synchronizer
		{
			internal volatile CountDownLatch waiter;

			internal volatile CountDownLatch updateJoin;

			internal volatile CountDownLatch leftCheckpoint;

			public Synchronizer(int numUpdater, int numThreads)
			{
				Reset(numUpdater, numThreads);
			}

			public void Reset(int numUpdaters, int numThreads)
			{
				this.waiter = new CountDownLatch(1);
				this.updateJoin = new CountDownLatch(numUpdaters);
				this.leftCheckpoint = new CountDownLatch(numUpdaters);
			}

			/// <exception cref="System.Exception"></exception>
			public bool Await()
			{
				return waiter.Await(10, TimeUnit.SECONDS);
			}
		}
	}
}
