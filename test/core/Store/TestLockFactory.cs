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

using WhitespaceAnalyzer = Lucene.Net.Test.Analysis.WhitespaceAnalyzer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using Term = Lucene.Net.Index.Term;
using IndexSearcher = Lucene.Net.Search.IndexSearcher;
using Query = Lucene.Net.Search.Query;
using ScoreDoc = Lucene.Net.Search.ScoreDoc;
using TermQuery = Lucene.Net.Search.TermQuery;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
using _TestUtil = Lucene.Net.Util._TestUtil;

namespace Lucene.Net.Store
{
	
	[TestFixture]
	public class TestLockFactory:LuceneTestCase
	{
		
		// Verify: we can provide our own LockFactory implementation, the right
		// methods are called at the right time, locks are created, etc.
		
		[Test]
		public virtual void  TestCustomLockFactory()
		{
			Directory dir = new MockDirectoryWrapper(Random(), new RAMDirectory());
			MockLockFactory lf = new MockLockFactory(this);
			dir.SetLockFactory(lf);
			
			// Lock prefix should have been set:
			Assert.IsTrue(lf.lockPrefixSet, "lock prefix was not set by the RAMDirectory");
			
			IndexWriter writer = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			// add 100 documents (so that commit lock is used)
			for (int i = 0; i < 100; i++)
			{
				AddDoc(writer);
			}
			
			// Both write lock and commit lock should have been created:
			Assert.AreEqual(1, lf.locksCreated.Count, "# of unique locks created (after instantiating IndexWriter)");
			Assert.IsTrue(lf.makeLockCount >= 1, "# calls to makeLock is 0 (after instantiating IndexWriter)");
			
			foreach (string lockName in lf.locksCreated.Keys)
			{
				System.String lockName = (System.String) e.Current;
				MockLockFactory.MockLock lock_Renamed = (MockLockFactory.MockLock) lf.locksCreated[lockName];
				Assert.IsTrue(lock_Renamed.lockAttempts > 0, "# calls to Lock.obtain is 0 (after instantiating IndexWriter)");
			}
			
			writer.Dispose();
		}
		
		// Verify: we can use the NoLockFactory with RAMDirectory w/ no
		// exceptions raised:
		// Verify: NoLockFactory allows two IndexWriters
		[Test]
		public virtual void  TestRAMDirectoryNoLocking()
		{
			MockDirectoryWrapper dir = new MockDirectoryWrapper(Random(), new RAMDirectory());
			dir.SetLockFactory(NoLockFactory.GetNoLockFactory());
			dir.SetWrapLockFactory(false);
			Assert.IsTrue(typeof(NoLockFactory).IsInstanceOfType(dir.LockFactory), "RAMDirectory.setLockFactory did not take");
			
			IndexWriter writer = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			writer.Commit();
			// Create a 2nd IndexWriter.  This is normally not allowed but it should run through since we're not
			// using any locks:
			IndexWriter writer2 = null;
			try
			{
				writer2 = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
					(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND));
			}
			catch (System.Exception e)
			{
				System.Console.Out.WriteLine(e.StackTrace);
				Assert.Fail("Should not have hit an IOException with no locking");
			}
			
			writer.Dispose();
			if (writer2 != null)
			{
				writer2.Dispose();
			}
		}
		
		// Verify: SingleInstanceLockFactory is the default lock for RAMDirectory
		// Verify: RAMDirectory does basic locking correctly (can't create two IndexWriters)
		[Test]
		public virtual void  TestDefaultRAMDirectory()
		{
			Directory dir = new RAMDirectory();
			
			Assert.IsTrue(typeof(SingleInstanceLockFactory).IsInstanceOfType(dir.LockFactory), "RAMDirectory did not use correct LockFactory: got " + dir.LockFactory);
			
			IndexWriter writer = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			// Create a 2nd IndexWriter.  This should fail:
			IndexWriter writer2 = null;

		    Assert.Throws<LockObtainFailedException>(
		        () => writer2 = new IndexWriter(dir, new WhitespaceAnalyzer(), false, IndexWriter.MaxFieldLength.LIMITED),
		        "Should have hit an IOException with two IndexWriters on default SingleInstanceLockFactory");
			
			writer.Dispose();
			if (writer2 != null)
			{
				writer2.Dispose();
			}
		}

        [Test]
        public void TestSimpleFSLockFactory()
        {
            // test string file instantiation
            new SimpleFSLockFactory("test");
        }
		
		// Verify: do stress test, by opening IndexReaders and
		// IndexWriters over & over in 2 threads and making sure
		// no unexpected exceptions are raised:
		[Test]
		[LuceneTestCase.Nightly]
		public virtual void  TestStressLocks()
		{
			_testStressLocks(null, _TestUtil.GetTempDir("index.TestLockFactory6"));
		}
		
		// Verify: do stress test, by opening IndexReaders and
		// IndexWriters over & over in 2 threads and making sure
		// no unexpected exceptions are raised, but use
		// NativeFSLockFactory:
		[Test]
		[LuceneTestCase.Nightly]
		public virtual void  TestStressLocksNativeFSLockFactory()
		{
			System.IO.DirectoryInfo dir = _TestUtil.GetTempDir("index.TestLockFactory7");
			_testStressLocks(new NativeFSLockFactory(dir), dir);
		}
		
		public virtual void  _testStressLocks(LockFactory lockFactory, System.IO.DirectoryInfo indexDir)
		{
			Directory dir = NewFSDirectory(indexDir, lockFactory);
			
			// First create a 1 doc index:
			IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, 
				new MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode.CREATE));
			AddDoc(w);
			w.Dispose();
			
			TestLockFactory.WriterThread writer = new TestLockFactory.WriterThread(this, 100, 
				dir);
			TestLockFactory.SearcherThread searcher = new TestLockFactory.SearcherThread(this
				, 100, dir);
			writer.Start();
			searcher.Start();
			
			while (writer.IsAlive || searcher.IsAlive)
			{
				System.Threading.Thread.Sleep(new System.TimeSpan((System.Int64) 10000 * 1000));
			}
			
			Assert.IsTrue(!writer.hitException, "IndexWriter hit unexpected exceptions");
			Assert.IsTrue(!searcher.hitException, "IndexSearcher hit unexpected exceptions");
			
			dir.Dispose();
			// Cleanup
			_TestUtil.RmDir(indexDir);
		}
		
		// Verify: NativeFSLockFactory works correctly
		[Test]
		public virtual void  TestNativeFSLockFactory()
		{
			
			NativeFSLockFactory f = new NativeFSLockFactory(AppSettings.Get("tempDir", System.IO.Path.GetTempPath()));
			
			f.LockPrefix = "test";
			Lock l = f.MakeLock("commit");
			Lock l2 = f.MakeLock("commit");
			
			Assert.IsTrue(l.Obtain(), "failed to obtain lock");
			Assert.IsTrue(!l2.Obtain(), "succeeded in obtaining lock twice");
			l.Dispose();
			
			Assert.IsTrue(l2.Obtain(), "failed to obtain 2nd lock after first one was freed");
			l2.Dispose();
			
			// Make sure we can obtain first one again, test isLocked():
			Assert.IsTrue(l.Obtain(), "failed to obtain lock");
			Assert.IsTrue(l.IsLocked());
			Assert.IsTrue(l2.IsLocked());
			l.Dispose();
			Assert.IsFalse(l.IsLocked());
			Assert.IsFalse(l2.IsLocked());
		}
		
		public virtual void TestNativeFSLockFactoryLockExists()
		{
			FilePath tempDir = CreateTempDir(LuceneTestCase.GetTestClass().Name);
			FilePath lockFile = new FilePath(tempDir, "test.lock");
			lockFile.CreateNewFile();
			Lock l = new NativeFSLockFactory(tempDir).MakeLock("test.lock");
			IsTrue("failed to obtain lock", l.Obtain());
			l.Dispose();
			IsFalse("failed to release lock", l.IsLocked());
			if (lockFile.Exists())
			{
				lockFile.Delete();
			}
		}
		// Verify: NativeFSLockFactory assigns null as lockPrefix if the lockDir is inside directory
		[Test]
		public virtual void  TestNativeFSLockFactoryPrefix()
		{
			
			System.IO.DirectoryInfo fdir1 = _TestUtil.GetTempDir("TestLockFactory.8");
			System.IO.DirectoryInfo fdir2 = _TestUtil.GetTempDir("TestLockFactory.8.Lockdir");
			Directory dir1 = NewFSDirectory(new System.IO.DirectoryInfo(fdir1.FullName), new NativeFSLockFactory(fdir1));
			// same directory, but locks are stored somewhere else. The prefix of the lock factory should != null
			Directory dir2 = NewFSDirectory(new System.IO.DirectoryInfo(fdir1.FullName), new NativeFSLockFactory(fdir2));
			
			System.String prefix1 = dir1.LockFactory.LockPrefix;
			Assert.IsNull(prefix1, "Lock prefix for lockDir same as directory should be null");
			
			System.String prefix2 = dir2.LockFactory.LockPrefix;
			Assert.IsNotNull(prefix2, "Lock prefix for lockDir outside of directory should be not null");
			
			dir1.Dispose();
			dir2.Dispose();
			_TestUtil.RmDir(fdir1);
			_TestUtil.RmDir(fdir2);
		}
		
		// Verify: default LockFactory has no prefix (ie
		// write.lock is stored in index):
		[Test]
		public virtual void  TestDefaultFSLockFactoryPrefix()
		{
			
			// Make sure we get null prefix:
			System.IO.DirectoryInfo dirName = _TestUtil.GetTempDir("TestLockFactory.10");
			Directory dir = new SimpleFSDirectory(dirName);
			
			System.String prefix = dir.LockFactory.LockPrefix;
			
			Assert.IsTrue(null == prefix, "Default lock prefix should be null");
			dir.Dispose();
			dir = new MMapDirectory(dirName);
			IsNull("Default lock prefix should be null", dir.GetLockFactory
				().GetLockPrefix());
			dir.Dispose();
			dir = new NIOFSDirectory(dirName);
			IsNull("Default lock prefix should be null", dir.GetLockFactory
				().GetLockPrefix());
			dir.Dispose();
			_TestUtil.RmDir(dirName);
		}
		
		private class WriterThread:ThreadClass
		{
			private void  InitBlock(TestLockFactory enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private TestLockFactory enclosingInstance;
			public TestLockFactory Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			private Directory dir;
			private int numIteration;
			public bool hitException = false;
			public WriterThread(TestLockFactory enclosingInstance, int numIteration, Directory dir)
			{
				InitBlock(enclosingInstance);
				this.numIteration = numIteration;
				this.dir = dir;
			}
			override public void  Run()
			{
				WhitespaceAnalyzer analyzer = new WhitespaceAnalyzer();
				IndexWriter writer = null;
				for (int i = 0; i < this.numIteration; i++)
				{
					try
					{
						writer = new IndexWriter(dir, analyzer, false, IndexWriter.MaxFieldLength.LIMITED);
					}
					catch (System.IO.IOException e)
					{
						if (e.ToString().IndexOf(" timed out:") == - 1)
						{
							hitException = true;
							System.Console.Out.WriteLine("Stress Test Index Writer: creation hit unexpected IOException: " + e.ToString());
							System.Console.Out.WriteLine(e.StackTrace);
						}
						else
						{
							// lock obtain timed out
							// NOTE: we should at some point
							// consider this a failure?  The lock
							// obtains, across IndexReader &
							// IndexWriters should be "fair" (ie
							// FIFO).
						}
					}
					catch (System.Exception e)
					{
						hitException = true;
						System.Console.Out.WriteLine("Stress Test Index Writer: creation hit unexpected exception: " + e.ToString());
						System.Console.Out.WriteLine(e.StackTrace);
						break;
					}
					if (writer != null)
					{
						try
						{
							Enclosing_Instance.AddDoc(writer);
						}
						catch (System.IO.IOException e)
						{
							hitException = true;
							System.Console.Out.WriteLine("Stress Test Index Writer: addDoc hit unexpected exception: " + e.ToString());
							System.Console.Out.WriteLine(e.StackTrace);
							break;
						}
						try
						{
							writer.Dispose();
						}
						catch (System.IO.IOException e)
						{
							hitException = true;
							System.Console.Out.WriteLine("Stress Test Index Writer: close hit unexpected exception: " + e.ToString());
							System.Console.Out.WriteLine(e.StackTrace);
							break;
						}
						writer = null;
					}
				}
			}
		}
		
		private class SearcherThread:ThreadClass
		{
			private void  InitBlock(TestLockFactory enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private TestLockFactory enclosingInstance;
			public TestLockFactory Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			private Directory dir;
			private int numIteration;
			public bool hitException = false;
			public SearcherThread(TestLockFactory enclosingInstance, int numIteration, Directory dir)
			{
				InitBlock(enclosingInstance);
				this.numIteration = numIteration;
				this.dir = dir;
			}
			override public void  Run()
			{
				IndexSearcher searcher = null;
				Query query = new TermQuery(new Term("content", "aaa"));
				for (int i = 0; i < this.numIteration; i++)
				{
					try
					{
						searcher = new IndexSearcher(dir, false);
					}
					catch (System.Exception e)
					{
						hitException = true;
						System.Console.Out.WriteLine("Stress Test Index Searcher: create hit unexpected exception: " + e.ToString());
						System.Console.Out.WriteLine(e.StackTrace);
						break;
					}
					if (searcher != null)
					{
						ScoreDoc[] hits = null;
						try
						{
							hits = searcher.Search(query, null, 1000).ScoreDocs;
						}
						catch (System.IO.IOException e)
						{
							hitException = true;
							System.Console.Out.WriteLine("Stress Test Index Searcher: search hit unexpected exception: " + e.ToString());
							System.Console.Out.WriteLine(e.StackTrace);
							break;
						}
						// System.out.println(hits.length() + " total results");
						try
						{
							searcher.Dispose();
						}
						catch (System.IO.IOException e)
						{
							hitException = true;
							System.Console.Out.WriteLine("Stress Test Index Searcher: close hit unexpected exception: " + e.ToString());
							System.Console.Out.WriteLine(e.StackTrace);
							break;
						}
						searcher = null;
					}
				}
			}
		}
		
		public class MockLockFactory:LockFactory
		{
			public MockLockFactory(TestLockFactory enclosingInstance)
			{
				InitBlock(enclosingInstance);
			}
			private void  InitBlock(TestLockFactory enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private TestLockFactory enclosingInstance;
			public TestLockFactory Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			
			public bool lockPrefixSet;
			public System.Collections.IDictionary locksCreated = System.Collections.Hashtable.Synchronized(new System.Collections.Hashtable(new System.Collections.Hashtable()));
			public int makeLockCount = 0;

		    public override string LockPrefix
		    {
		        set
		        {
		            base.LockPrefix = value;
		            lockPrefixSet = true;
		        }
		    }

		    public override Lock MakeLock(System.String lockName)
			{
				lock (this)
				{
					Lock lock_Renamed = new MockLock(this);
					locksCreated[lockName] = lock_Renamed;
					makeLockCount++;
					return lock_Renamed;
				}
			}
			
			public override void  ClearLock(System.String specificLockName)
			{
			}
			
			public class MockLock:Lock
			{
				public MockLock(MockLockFactory enclosingInstance)
				{
					InitBlock(enclosingInstance);
				}
				private void  InitBlock(MockLockFactory enclosingInstance)
				{
					this.enclosingInstance = enclosingInstance;
				}
				private MockLockFactory enclosingInstance;
				public MockLockFactory Enclosing_Instance
				{
					get
					{
						return enclosingInstance;
					}
					
				}
				public int lockAttempts;
				
				public override bool Obtain()
				{
					lockAttempts++;
					return true;
				}
				public override void  Release()
				{
					// do nothing
				}
				public override bool IsLocked()
				{
					return false;
				}
			}
		}
		
		private void  AddDoc(IndexWriter writer)
		{
			Document doc = new Document();
			doc.Add(new Field("content", "aaa", Field.Store.NO, Field.Index.ANALYZED));
			writer.AddDocument(doc);
		}
	}
}