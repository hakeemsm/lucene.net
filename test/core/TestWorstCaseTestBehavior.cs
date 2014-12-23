/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Com.Carrotsearch.Randomizedtesting;
using NUnit.Framework;
using Org.Apache.Lucene;
using Lucene.Net.Util;
using Sharpen;

namespace Org.Apache.Lucene
{
	public class TestWorstCaseTestBehavior : LuceneTestCase
	{
		[Ignore]
		public virtual void TestThreadLeak()
		{
			Sharpen.Thread t = new _Thread_29();
			// Ignore.
			t.Start();
			while (!t.IsAlive())
			{
				Sharpen.Thread.Yield();
			}
		}

		private sealed class _Thread_29 : Sharpen.Thread
		{
			public _Thread_29()
			{
			}

			public override void Run()
			{
				try
				{
					Sharpen.Thread.Sleep(10000);
				}
				catch (Exception)
				{
				}
			}
		}

		// once alive, leave it to run outside of the test scope.
		/// <exception cref="System.Exception"></exception>
		[Ignore]
		public virtual void TestLaaaaaargeOutput()
		{
			string message = "I will not OOM on large output";
			int howMuch = 250 * 1024 * 1024;
			for (int i = 0; i < howMuch; i++)
			{
				if (i > 0)
				{
					System.Console.Out.Write(",\n");
				}
				System.Console.Out.Write(message);
				howMuch -= message.Length;
			}
			// approximately.
			System.Console.Out.WriteLine(".");
		}

		/// <exception cref="System.Exception"></exception>
		[Ignore]
		public virtual void TestProgressiveOutput()
		{
			for (int i = 0; i < 20; i++)
			{
				System.Console.Out.WriteLine("Emitting sysout line: " + i);
				System.Console.Error.WriteLine("Emitting syserr line: " + i);
				System.Console.Out.Flush();
				System.Console.Error.Flush();
				RandomizedTest.Sleep(1000);
			}
		}

		/// <exception cref="System.Exception"></exception>
		[Ignore]
		public virtual void TestUncaughtException()
		{
			Sharpen.Thread t = new _Thread_73();
			t.Start();
			t.Join();
		}

		private sealed class _Thread_73 : Sharpen.Thread
		{
			public _Thread_73()
			{
			}

			public override void Run()
			{
				throw new RuntimeException("foobar");
			}
		}

		/// <exception cref="System.Exception"></exception>
		[Ignore]
		public virtual void TestTimeout()
		{
			Sharpen.Thread.Sleep(5000);
		}

		/// <exception cref="System.Exception"></exception>
		[Ignore]
		public virtual void TestZombie()
		{
			while (true)
			{
				try
				{
					Sharpen.Thread.Sleep(1000);
				}
				catch (Exception)
				{
				}
			}
		}
	}
}
