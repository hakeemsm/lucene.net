/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Com.Carrotsearch.Randomizedtesting;
using Lucene.Net.Codecs;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Test.Index
{
	/// <summary>
	/// Runs TestNRTThreads in a separate process, crashes the JRE in the middle
	/// of execution, then runs checkindex to make sure its not corrupt.
	/// </summary>
	/// <remarks>
	/// Runs TestNRTThreads in a separate process, crashes the JRE in the middle
	/// of execution, then runs checkindex to make sure its not corrupt.
	/// </remarks>
	public class TestIndexWriterOnJRECrash : Lucene.Net.Index.TestNRTThreads
	{
		private FilePath tempDir;

		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			tempDir = CreateTempDir("jrecrash");
			tempDir.Delete();
			tempDir.Mkdir();
		}

		/// <exception cref="System.Exception"></exception>
		[LuceneTestCase.Nightly]
		public override void TestNRTThreads()
		{
			// if we are not the fork
			if (Runtime.GetProperty("tests.crashmode") == null)
			{
				// try up to 10 times to create an index
				for (int i = 0; i < 10; i++)
				{
					ForkTest();
					// if we succeeded in finding an index, we are done.
					if (CheckIndexes(tempDir))
					{
						return;
					}
				}
			}
			else
			{
				// TODO: the non-fork code could simply enable impersonation?
				AssumeFalse("does not support PreFlex, see LUCENE-3992", Codec.GetDefault().GetName
					().Equals("Lucene3x"));
				// we are the fork, setup a crashing thread
				int crashTime = TestUtil.NextInt(Random(), 3000, 4000);
				Sharpen.Thread t = new _Thread_69(this, crashTime);
				t.SetPriority(Sharpen.Thread.MAX_PRIORITY);
				t.Start();
				// run the test until we crash.
				for (int i = 0; i < 1000; i++)
				{
					base.TestNRTThreads();
				}
			}
		}

		private sealed class _Thread_69 : Sharpen.Thread
		{
			public _Thread_69(TestIndexWriterOnJRECrash _enclosing, int crashTime)
			{
				this._enclosing = _enclosing;
				this.crashTime = crashTime;
			}

			public override void Run()
			{
				try
				{
					Sharpen.Thread.Sleep(crashTime);
				}
				catch (Exception)
				{
				}
				this._enclosing.CrashJRE();
			}

			private readonly TestIndexWriterOnJRECrash _enclosing;

			private readonly int crashTime;
		}

		/// <summary>fork ourselves in a new jvm.</summary>
		/// <remarks>fork ourselves in a new jvm. sets -Dtests.crashmode=true</remarks>
		/// <exception cref="System.Exception"></exception>
		public virtual void ForkTest()
		{
			IList<string> cmd = new AList<string>();
			cmd.AddItem(Runtime.GetProperty("java.home") + Runtime.GetProperty("file.separator"
				) + "bin" + Runtime.GetProperty("file.separator") + "java");
			cmd.AddItem("-Xmx512m");
			cmd.AddItem("-Dtests.crashmode=true");
			// passing NIGHTLY to this test makes it run for much longer, easier to catch it in the act...
			cmd.AddItem("-Dtests.nightly=true");
			cmd.AddItem("-DtempDir=" + tempDir.GetPath());
			cmd.AddItem("-Dtests.seed=" + SeedUtils.FormatSeed(Random().NextLong()));
			cmd.AddItem("-ea");
			cmd.AddItem("-cp");
			cmd.AddItem(Runtime.GetProperty("java.class.path"));
			cmd.AddItem("org.junit.runner.JUnitCore");
			cmd.AddItem(GetType().FullName);
			ProcessStartInfo pb = new ProcessStartInfo(cmd);
			pb.WorkingDirectory = tempDir;
			pb.RedirectErrorStream(true);
			SystemProcess p = pb.Start();
			// We pump everything to stderr.
			TextWriter childOut = System.Console.Error;
			Sharpen.Thread stdoutPumper = TestIndexWriterOnJRECrash.ThreadPumper.Start(p.GetInputStream
				(), childOut);
			Sharpen.Thread stderrPumper = TestIndexWriterOnJRECrash.ThreadPumper.Start(p.GetErrorStream
				(), childOut);
			if (VERBOSE)
			{
				childOut.WriteLine(">>> Begin subprocess output");
			}
			p.WaitFor();
			stdoutPumper.Join();
			stderrPumper.Join();
			if (VERBOSE)
			{
				childOut.WriteLine("<<< End subprocess output");
			}
		}

		/// <summary>A pipe thread.</summary>
		/// <remarks>A pipe thread. It'd be nice to reuse guava's implementation for this...</remarks>
		internal class ThreadPumper
		{
			public static Sharpen.Thread Start(InputStream from, OutputStream to)
			{
				Sharpen.Thread t = new _Thread_125(from, to);
				t.Start();
				return t;
			}

			private sealed class _Thread_125 : Sharpen.Thread
			{
				public _Thread_125(InputStream from, OutputStream to)
				{
					this.from = from;
					this.to = to;
				}

				public override void Run()
				{
					try
					{
						byte[] buffer = new byte[1024];
						int len;
						while ((len = from.Read(buffer)) != -1)
						{
							if (LuceneTestCase.VERBOSE)
							{
								to.Write(buffer, 0, len);
							}
						}
					}
					catch (IOException e)
					{
						System.Console.Error.WriteLine("Couldn't pipe from the forked process: " + e.ToString
							());
					}
				}

				private readonly InputStream from;

				private readonly OutputStream to;
			}
		}

		/// <summary>
		/// Recursively looks for indexes underneath <code>file</code>,
		/// and runs checkindex on them.
		/// </summary>
		/// <remarks>
		/// Recursively looks for indexes underneath <code>file</code>,
		/// and runs checkindex on them. returns true if it found any indexes.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual bool CheckIndexes(FilePath file)
		{
			if (file.IsDirectory())
			{
				BaseDirectoryWrapper dir = NewFSDirectory(file);
				dir.SetCheckIndexOnClose(false);
				// don't double-checkindex
				if (DirectoryReader.IndexExists(dir))
				{
					if (VERBOSE)
					{
						System.Console.Error.WriteLine("Checking index: " + file);
					}
					// LUCENE-4738: if we crashed while writing first
					// commit it's possible index will be corrupt (by
					// design we don't try to be smart about this case
					// since that too risky):
					if (SegmentInfos.GetLastCommitGeneration(dir) > 1)
					{
						TestUtil.CheckIndex(dir);
					}
					dir.Dispose();
					return true;
				}
				dir.Dispose();
				foreach (FilePath f in file.ListFiles())
				{
					if (CheckIndexes(f))
					{
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>currently, this only works/tested on Sun and IBM.</summary>
		/// <remarks>currently, this only works/tested on Sun and IBM.</remarks>
		public virtual void CrashJRE()
		{
			string vendor = Constants.JAVA_VENDOR;
			bool supportsUnsafeNpeDereference = vendor.StartsWith("Oracle") || vendor.StartsWith
				("Sun") || vendor.StartsWith("Apple");
			try
			{
				if (supportsUnsafeNpeDereference)
				{
					try
					{
						Type clazz = Sharpen.Runtime.GetType("sun.misc.Unsafe");
						FieldInfo field = Sharpen.Runtime.GetDeclaredField(clazz, "theUnsafe");
						object o = field.GetValue(null);
						MethodInfo m = clazz.GetMethod("putAddress", typeof(long), typeof(long));
						m.Invoke(o, 0L, 0L);
					}
					catch (Exception e)
					{
						System.Console.Out.WriteLine("Couldn't kill the JVM via Unsafe.");
						Sharpen.Runtime.PrintStackTrace(e, System.Console.Out);
					}
				}
				// Fallback attempt to Runtime.halt();
				Runtime.GetRuntime().Halt(-1);
			}
			catch (Exception e)
			{
				System.Console.Out.WriteLine("Couldn't kill the JVM.");
				Sharpen.Runtime.PrintStackTrace(e, System.Console.Out);
			}
			// We couldn't get the JVM to crash for some reason.
			Fail();
		}
	}
}
