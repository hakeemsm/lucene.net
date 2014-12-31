using System;
using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.TestFramework.Index;

namespace Lucene.Net.Test.Index
{
	public class TestNRTThreads : ThreadedIndexingAndSearchingTestCase
	{
		private bool useNonNrtReaders = true;

		// TODO
		//   - mix in forceMerge, addIndexes
		//   - randomoly mix in non-congruent docs
		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.SetUp]
		public override void SetUp()
		{
			base.SetUp();
			useNonNrtReaders = Random().NextBoolean();
		}

		/// <exception cref="System.Exception"></exception>
		protected override void DoSearching(ExecutorService es, long stopTime)
		{
			bool anyOpenDelFiles = false;
			DirectoryReader r = DirectoryReader.Open(writer, true);
			while (DateTime.Now.CurrentTimeMillis() < stopTime && !failed.Get())
			{
				if (Random().NextBoolean())
				{
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("TEST: now reopen r=" + r);
					}
					DirectoryReader r2 = DirectoryReader.OpenIfChanged(r);
					if (r2 != null)
					{
						r.Dispose();
						r = r2;
					}
				}
				else
				{
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("TEST: now close reader=" + r);
					}
					r.Dispose();
					writer.Commit();
					ICollection<string> openDeletedFiles = ((MockDirectoryWrapper)dir).GetOpenDeletedFiles
						();
					if (openDeletedFiles.Count > 0)
					{
						System.Console.Out.WriteLine("OBD files: " + openDeletedFiles);
					}
					anyOpenDelFiles |= openDeletedFiles.Count > 0;
					//assertEquals("open but deleted: " + openDeletedFiles, 0, openDeletedFiles.size());
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("TEST: now open");
					}
					r = DirectoryReader.Open(writer, true);
				}
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: got new reader=" + r);
				}
				//System.out.println("numDocs=" + r.numDocs() + "
				//openDelFileCount=" + dir.openDeleteFileCount());
				if (r.NumDocs > 0)
				{
					fixedSearcher = new IndexSearcher(r, es);
					SmokeTestSearcher(fixedSearcher);
					RunSearchThreads(DateTime.Now.CurrentTimeMillis() + 500);
				}
			}
			r.Dispose();
			//System.out.println("numDocs=" + r.numDocs() + " openDelFileCount=" + dir.openDeleteFileCount());
			ICollection<string> openDeletedFiles_1 = ((MockDirectoryWrapper)dir).GetOpenDeletedFiles
				();
			if (openDeletedFiles_1.Count > 0)
			{
				System.Console.Out.WriteLine("OBD files: " + openDeletedFiles_1);
			}
			anyOpenDelFiles |= openDeletedFiles_1.Count > 0;
			IsFalse("saw non-zero open-but-deleted count", anyOpenDelFiles
				);
		}

		protected override Directory GetDirectory(Directory @in)
		{
			//HM:revisit 
			//assert in instanceof MockDirectoryWrapper;
			if (!useNonNrtReaders)
			{
				((MockDirectoryWrapper)@in).SetAssertNoDeleteOpenFile(true);
			}
			return @in;
		}

		/// <exception cref="System.Exception"></exception>
		protected override void DoAfterWriter(ExecutorService es)
		{
			// Force writer to do reader pooling, always, so that
			// all merged segments, even for merges before
			// doSearching is called, are warmed:
			writer.Reader.Dispose();
		}

		private IndexSearcher fixedSearcher;

		/// <exception cref="System.Exception"></exception>
		protected override IndexSearcher GetCurrentSearcher()
		{
			return fixedSearcher;
		}

		/// <exception cref="System.Exception"></exception>
		protected override void ReleaseSearcher(IndexSearcher s)
		{
			if (s != fixedSearcher)
			{
				// Final searcher:
				s.IndexReader.Dispose();
			}
		}

		/// <exception cref="System.Exception"></exception>
		protected override IndexSearcher GetFinalSearcher()
		{
			IndexReader r2;
			if (useNonNrtReaders)
			{
				if (Random().NextBoolean())
				{
					r2 = writer.Reader;
				}
				else
				{
					writer.Commit();
					r2 = DirectoryReader.Open(dir);
				}
			}
			else
			{
				r2 = writer.Reader;
			}
			return NewSearcher(r2);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestNRTThreads()
		{
			RunTest("TestNRTThreads");
		}
	}
}
