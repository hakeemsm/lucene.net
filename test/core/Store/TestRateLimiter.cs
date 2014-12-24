/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Store
{
	/// <summary>Simple testcase for RateLimiter.SimpleRateLimiter</summary>
	public sealed class TestRateLimiter : LuceneTestCase
	{
		public void TestPause()
		{
			RateLimiter.SimpleRateLimiter limiter = new RateLimiter.SimpleRateLimiter(10);
			// 10 MB / Sec
			limiter.Pause(2);
			//init
			long pause = 0;
			for (int i = 0; i < 3; i++)
			{
				pause += limiter.Pause(4 * 1024 * 1024);
			}
			// fire up 3 * 4 MB 
			long convert = TimeUnit.MILLISECONDS.Convert(pause, TimeUnit.NANOSECONDS);
			IsTrue("we should sleep less than 2 seconds but did: " + convert
				 + " millis", convert < 2000l);
			IsTrue("we should sleep at least 1 second but did only: " 
				+ convert + " millis", convert > 1000l);
		}

		/// <exception cref="System.Exception"></exception>
		public void TestThreads()
		{
			double targetMBPerSec = 10.0 + 20 * Random().NextDouble();
			RateLimiter.SimpleRateLimiter limiter = new RateLimiter.SimpleRateLimiter(targetMBPerSec
				);
			CountDownLatch startingGun = new CountDownLatch(1);
			Sharpen.Thread[] threads = new Sharpen.Thread[TestUtil.NextInt(Random(), 3, 6)];
			AtomicLong totBytes = new AtomicLong();
			for (int i = 0; i < threads.Length; i++)
			{
				threads[i] = new _Thread_58(startingGun, totBytes, limiter);
				threads[i].Start();
			}
			long startNS = Runtime.NanoTime();
			startingGun.CountDown();
			foreach (Sharpen.Thread thread in threads)
			{
				thread.Join();
			}
			long endNS = Runtime.NanoTime();
			double actualMBPerSec = (totBytes.Get() / 1024 / 1024.) / ((endNS - startNS) / 1000000000.0
				);
			// TODO: this may false trip .... could be we can only 
			//HM:revisit 
			//assert that it never exceeds the max, so slow jenkins doesn't trip:
			double ratio = actualMBPerSec / targetMBPerSec;
			IsTrue("targetMBPerSec=" + targetMBPerSec + " actualMBPerSec="
				 + actualMBPerSec, ratio >= 0.9 && ratio <= 1.1);
		}

		private sealed class _Thread_58 : Sharpen.Thread
		{
			public _Thread_58(CountDownLatch startingGun, AtomicLong totBytes, RateLimiter.SimpleRateLimiter
				 limiter)
			{
				this.startingGun = startingGun;
				this.totBytes = totBytes;
				this.limiter = limiter;
			}

			public override void Run()
			{
				try
				{
					startingGun.Await();
				}
				catch (Exception ie)
				{
					throw new ThreadInterruptedException(ie);
				}
				long bytesSinceLastPause = 0;
				for (int i = 0; i < 500; i++)
				{
					long numBytes = TestUtil.NextInt(LuceneTestCase.Random(), 1000, 10000);
					totBytes.AddAndGet(numBytes);
					bytesSinceLastPause += numBytes;
					if (bytesSinceLastPause > limiter.GetMinPauseCheckBytes())
					{
						limiter.Pause(bytesSinceLastPause);
						bytesSinceLastPause = 0;
					}
				}
			}

			private readonly CountDownLatch startingGun;

			private readonly AtomicLong totBytes;

			private readonly RateLimiter.SimpleRateLimiter limiter;
		}
	}
}
