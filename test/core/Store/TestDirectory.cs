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

using System;
using Lucene.Net.Support;
using NUnit.Framework;

using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
using _TestUtil = Lucene.Net.Util._TestUtil;

namespace Lucene.Net.Store
{
	
	[TestFixture]
	public class TestDirectory:LuceneTestCase
	{
		
		[Test]
		public virtual void  TestDetectClose()
		{
			FilePath tempDir = CreateTempDir(LuceneTestCase.GetTestClass().Name);
			Directory[] dirs = new Directory[] { new RAMDirectory(), new SimpleFSDirectory(tempDir
				), new NIOFSDirectory(tempDir) };
			foreach (Directory dir in dirs)
			{
			dir.Close();

            Assert.Throws<AlreadyClosedException>(() => dir.CreateOutput("test", NewIOContext(Random())), "did not hit expected exception");
			}
			
		}
		
		[LuceneTestCase.Nightly]
		public virtual void TestThreadSafety()
		{
			BaseDirectoryWrapper dir = NewDirectory();
			dir.SetCheckIndexOnClose(false);
			// we arent making an index
			if (dir is MockDirectoryWrapper)
			{
				((MockDirectoryWrapper)dir).SetThrottling(MockDirectoryWrapper.Throttling.NEVER);
			}
			// makes this test really slow
			if (VERBOSE)
			{
				System.Console.Out.WriteLine(dir);
			}
			//System.out.println("create:" + fileName);
			//System.out.println("file:" + file);
			// ignore
			// ignore
			_T803627632 theThread = new _T803627632(this, "t1");
			_T497355656 theThread2 = new _T497355656(this, "t2");
			theThread.Start();
			theThread2.Start();
			theThread.Join();
			theThread2.Join();
			dir.Close();
		}
		
		// Test that different instances of FSDirectory can coexist on the same
		// path, can read, write, and lock files.
		[Test]
		public virtual void  TestDirectInstantiation()
		{
			System.IO.DirectoryInfo path = new System.IO.DirectoryInfo(AppSettings.Get("tempDir", System.IO.Path.GetTempPath()));
			byte[] largeBuffer = new byte[Random().Next(256 * 1024)];
			byte[] largeReadBuffer = new byte[largeBuffer.Length];
			for (int i = 0; i < largeBuffer.Length; i++)
			{
				largeBuffer[i] = unchecked((byte)i);
			}
			// automatically loops with modulo
			FSDirectory[] dirs = new FSDirectory[] { new SimpleFSDirectory(path, null), new NIOFSDirectory
				(path, null), new MMapDirectory(path, null) };
			
			for (int i_1 = 0; i_1 < dirs.Length; i_1++)
			{
				FSDirectory dir = dirs[i_1];
				dir.EnsureOpen();
				string fname = "foo." + i_1;
				string lockname = "foo" + i_1 + ".lck";
				IndexOutput @out = dir.CreateOutput(fname, NewIOContext(Random()));
				out_Renamed.WriteByte((byte) i);
				@out.WriteBytes(largeBuffer, largeBuffer.Length);
				out_Renamed.Close();
				
				for (int j = 0; j < dirs.Length; j++)
				{
					FSDirectory d2 = dirs[j];
					d2.EnsureOpen();
					IsTrue(SlowFileExists(d2, fname));
					AreEqual(1 + largeBuffer.Length, d2.FileLength(fname));
					
					// don't do read tests if unmapping is not supported!
					if (d2 is MMapDirectory && !((MMapDirectory)d2).GetUseUnmap())
					{
						continue;
					}
					IndexInput input = d2.OpenInput(fname, NewIOContext(Random()));
					Assert.AreEqual((byte) i_1, input.ReadByte());
					Arrays.Fill(largeReadBuffer, unchecked((byte)0));
					input.ReadBytes(largeReadBuffer, 0, largeReadBuffer.Length, true);
					AssertArrayEquals(largeBuffer, largeReadBuffer);
					input.Seek(1L);
					Arrays.Fill(largeReadBuffer, unchecked((byte)0));
					input.ReadBytes(largeReadBuffer, 0, largeReadBuffer.Length, false);
					AssertArrayEquals(largeBuffer, largeReadBuffer);
					input.Close();
				}
				
				// delete with a different dir
				dirs[(i_1 + 1) % dirs.Length].DeleteFile(fname);
				
				for (int j_1 = 0; j_1 < dirs.Length; j_1++)
				{
					FSDirectory d2 = dirs[j_1];
					IsFalse(SlowFileExists(d2, fname));
				}
				
				Lock lock_Renamed = dir.MakeLock(lockname);
				Assert.IsTrue(lock_Renamed.Obtain());
				
				for (int j_2 = 0; j_2 < dirs.Length; j_2++)
				{
					FSDirectory d2 = dirs[j_2];
					Lock lock2 = d2.MakeLock(lockname);
					try
					{
						Assert.IsFalse(lock2.Obtain(1));
					}
					catch (LockObtainFailedException)
					{
						// OK
					}
				}
				
				lock_Renamed.Close();
				
				// now lock with different dir
				lock_Renamed = dirs[(i_1 + 1) % dirs.Length].MakeLock(lockname);
				Assert.IsTrue(lock_Renamed.Obtain());
				lock_Renamed.Close();
			}
			
			for (int i_2 = 0; i_2 < dirs.Length; i_2++)
			{
				FSDirectory dir = dirs[i_2];
				dir.EnsureOpen();
				dir.Close();
                Assert.IsFalse(dir.isOpen_ForNUnit);
			}
			TestUtil.Rm(path);
		}
		
		// LUCENE-1464
		[Test]
		public virtual void  TestDontCreate()
		{
			System.IO.DirectoryInfo path = new System.IO.DirectoryInfo(System.IO.Path.Combine(AppSettings.Get("tempDir", ""), "doesnotexist"));
			try
			{
				IsTrue(!path.Exists());
				Directory dir = new SimpleFSDirectory(path, null);
				IsTrue(!path.Exists());
				dir.Close();
			}
			finally
			{
				_TestUtil.RmDir(path);
			}
		}
		
		// LUCENE-1468
		[Test]
		public virtual void  TestRAMDirectoryFilter()
		{
			CheckDirectoryFilter(new RAMDirectory());
		}
		
		// LUCENE-1468
		[Test]
		public virtual void  TestFSDirectoryFilter()
		{
			CheckDirectoryFilter(NewFSDirectory(CreateTempDir("test")));
		}
		
		// LUCENE-1468
		private void  CheckDirectoryFilter(Directory dir)
		{
			System.String name = "file";
			try
			{
				dir.CreateOutput(name, NewIOContext(Random())).Close();
				IsTrue(SlowFileExists(dir, name));
				Assert.IsTrue(new System.Collections.ArrayList(dir.ListAll()).Contains(name));
			}
			finally
			{
				dir.Close();
			}
		}
		
		// LUCENE-1468
		[Test]
		public virtual void  TestCopySubdir()
		{
			System.IO.DirectoryInfo path = new System.IO.DirectoryInfo(System.IO.Path.Combine(AppSettings.Get("tempDir", ""), "testsubdir"));
			try
			{
				System.IO.Directory.CreateDirectory(path.FullName);
				System.IO.Directory.CreateDirectory(new System.IO.DirectoryInfo(System.IO.Path.Combine(path.FullName, "subdir")).FullName);
				Directory fsDir = new SimpleFSDirectory(path, null);
				AreEqual(0, new RAMDirectory(fsDir, NewIOContext(Random())
					).ListAll().Length);
			}
			finally
			{
				_TestUtil.RmDir(path);
			}
		}
		
		// LUCENE-1468
		[Test]
		public virtual void  TestNotDirectory()
		{
			System.IO.DirectoryInfo path = new System.IO.DirectoryInfo(System.IO.Path.Combine(AppSettings.Get("tempDir", ""), "testnotdir"));
			Directory fsDir = new SimpleFSDirectory(path, null);
			try
			{
				IndexOutput out_Renamed = fsDir.CreateOutput("afile", NewIOContext(Random()));
				out_Renamed.Close();
				IsTrue(SlowFileExists(fsDir, "afile"));

			    Assert.Throws<NoSuchDirectoryException>(
			        () =>
			        new SimpleFSDirectory(new System.IO.DirectoryInfo(System.IO.Path.Combine(path.FullName, "afile")), null),
			        "did not hit expected exception");
			}
			finally
			{
				fsDir.Close();
				_TestUtil.RmDir(path);
			}
		}
		public virtual void TestFsyncDoesntCreateNewFiles()
		{
			FilePath path = CreateTempDir("nocreate");
			System.Console.Out.WriteLine(path.GetAbsolutePath());
			Directory fsdir = new SimpleFSDirectory(path);
			// write a file
			IndexOutput @out = fsdir.CreateOutput("afile", NewIOContext(Random()));
			@out.WriteString("boo");
			@out.Close();
			// delete it
			IsTrue(new FilePath(path, "afile").Delete());
			// directory is empty
			AreEqual(0, fsdir.ListAll().Length);
			// fsync it
			try
			{
				fsdir.Sync(Collections.Singleton("afile"));
				Fail("didn't get expected exception, instead fsync created new files: "
					 + Arrays.AsList(fsdir.ListAll()));
			}
			catch (IOException)
			{
			}
			// ok
			// directory is still empty
			AreEqual(0, fsdir.ListAll().Length);
			fsdir.Close();
		}
	}
}