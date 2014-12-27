/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Test.Index
{
	public class TestNeverDelete : LuceneTestCase
	{
		// Make sure if you use NoDeletionPolicy that no file
		// referenced by a commit point is ever deleted
		/// <exception cref="System.Exception"></exception>
		public virtual void TestIndexing()
		{
			FilePath tmpDir = CreateTempDir("TestNeverDelete");
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
			w.w.Config.SetMaxBufferedDocs(TestUtil.NextInt(Random(), 5, 30));
			w.Commit();
			Sharpen.Thread[] indexThreads = new Sharpen.Thread[Random().Next(4)];
			long stopTime = DateTime.Now.CurrentTimeMillis() + AtLeast(1000);
			for (int x = 0; x < indexThreads.Length; x++)
			{
				indexThreads[x] = new _Thread_58(stopTime, w);
				indexThreads[x].SetName("Thread " + x);
				indexThreads[x].Start();
			}
			ICollection<string> allFiles = new HashSet<string>();
			DirectoryReader r = DirectoryReader.Open(d);
			while (DateTime.Now.CurrentTimeMillis() < stopTime)
			{
				IndexCommit ic = r.GetIndexCommit();
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: check files: " + ic.GetFileNames());
				}
				Sharpen.Collections.AddAll(allFiles, ic.GetFileNames());
				// Make sure no old files were removed
				foreach (string fileName in allFiles)
				{
					IsTrue("file " + fileName + " does not exist", SlowFileExists
						(d, fileName));
				}
				DirectoryReader r2 = DirectoryReader.OpenIfChanged(r);
				if (r2 != null)
				{
					r.Dispose();
					r = r2;
				}
				Sharpen.Thread.Sleep(1);
			}
			r.Dispose();
			foreach (Sharpen.Thread t in indexThreads)
			{
				t.Join();
			}
			w.Dispose();
			d.Dispose();
			TestUtil.Rm(tmpDir);
		}

		private sealed class _Thread_58 : Sharpen.Thread
		{
			public _Thread_58(long stopTime, RandomIndexWriter w)
			{
				this.stopTime = stopTime;
				this.w = w;
			}

			public override void Run()
			{
				try
				{
					int docCount = 0;
					while (DateTime.Now.CurrentTimeMillis() < stopTime)
					{
						Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
							();
						doc.Add(LuceneTestCase.NewStringField("dc", string.Empty + docCount, Field.Store.
							YES));
						doc.Add(LuceneTestCase.NewTextField("field", "here is some text", Field.Store.YES
							));
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
					throw new RuntimeException(e);
				}
			}

			private readonly long stopTime;

			private readonly RandomIndexWriter w;
		}
	}
}
