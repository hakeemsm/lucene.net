using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Support;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.TestFramework;
using Lucene.Net.TestFramework.Index;
using Lucene.Net.TestFramework.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Index
{
	public class TestNeverDelete : LuceneTestCase
	{
		// Make sure if you use NoDeletionPolicy that no file
		// referenced by a commit point is ever deleted
		[Test]
		public virtual void TestIndexing()
		{
			DirectoryInfo tmpDir = CreateTempDir("TestNeverDelete");
			BaseDirectoryWrapper d = NewFSDirectory(tmpDir);
			// We want to "see" files removed if Lucene removed
			// them.  This is still worth running on Windows since
			// some files the IR opens and closes.
			if (d is MockDirectoryWrapper)
			{
				((MockDirectoryWrapper)d).SetNoDeleteOpenFile(false);
			}
			RandomIndexWriter w = new RandomIndexWriter(Random(), d, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetIndexDeletionPolicy(NoDeletionPolicy.INSTANCE));
			w.w.Config.SetMaxBufferedDocs(Random().NextInt(5, 30));
			w.Commit();
			Thread[] indexThreads = new Thread[Random().Next(4)];
			long stopTime = DateTime.Now.CurrentTimeMillis() + AtLeast(1000);
			for (int x = 0; x < indexThreads.Length; x++)
			{
			    indexThreads[x] = new Thread(new DocThread(stopTime, w).Run) {Name = ("Thread " + x)};
			    indexThreads[x].Start();
			}
			var allFiles = new HashSet<string>();
			DirectoryReader r = DirectoryReader.Open(d);
			while (DateTime.Now.CurrentTimeMillis() < stopTime)
			{
				IndexCommit ic = r.IndexCommit;
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: check files: " + ic.FileNames);
				}
				allFiles = (HashSet<string>) allFiles.Concat(ic.FileNames);
				// Make sure no old files were removed
				foreach (string fileName in allFiles)
				{
					AssertTrue("file " + fileName + " does not exist", SlowFileExists
						(d, fileName));
				}
				DirectoryReader r2 = DirectoryReader.OpenIfChanged(r);
				if (r2 != null)
				{
					r.Dispose();
					r = r2;
				}
				Thread.Sleep(1);
			}
			r.Dispose();
			foreach (Thread t in indexThreads)
			{
				t.Join();
			}
			w.Dispose();
			d.Dispose();
            tmpDir.Delete(true);
			
		}

		private sealed class DocThread
		{
			public DocThread(long stopTime, RandomIndexWriter w)
			{
				this.stopTime = stopTime;
				this.w = w;
			}

			public void Run()
			{
				try
				{
					int docCount = 0;
					while (DateTime.Now.CurrentTimeMillis() < stopTime)
					{
						var doc = new Lucene.Net.Documents.Document
						{
						    NewStringField("dc", string.Empty + docCount, Field.Store.YES),
						    NewTextField("field", "here is some text", Field.Store.YES)
						};
					    w.AddDocument(doc);
						if (docCount % 13 == 0)
						{
							w.Commit();
						}
						docCount++;
					}
				}
				catch (Exception e)
				{
					throw new SystemException(e.Message,e);
				}
			}

			private readonly long stopTime;

			private readonly RandomIndexWriter w;
		}
	}
}
