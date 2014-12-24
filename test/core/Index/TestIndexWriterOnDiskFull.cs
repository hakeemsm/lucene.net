/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.IO;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Codecs;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	/// <summary>Tests for IndexWriter when the disk runs out of space</summary>
	public class TestIndexWriterOnDiskFull : LuceneTestCase
	{
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestAddDocumentOnDiskFull()
		{
			for (int pass = 0; pass < 2; pass++)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: pass=" + pass);
				}
				bool doAbort = pass == 1;
				long diskFree = TestUtil.NextInt(Random(), 100, 300);
				while (true)
				{
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("TEST: cycle: diskFree=" + diskFree);
					}
					MockDirectoryWrapper dir = new MockDirectoryWrapper(Random(), new RAMDirectory());
					dir.SetMaxSizeInBytes(diskFree);
					IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
						, new MockAnalyzer(Random())));
					MergeScheduler ms = writer.GetConfig().GetMergeScheduler();
					if (ms is ConcurrentMergeScheduler)
					{
						// This test intentionally produces exceptions
						// in the threads that CMS launches; we don't
						// want to pollute test output with these.
						((ConcurrentMergeScheduler)ms).SetSuppressExceptions();
					}
					bool hitError = false;
					try
					{
						for (int i = 0; i < 200; i++)
						{
							AddDoc(writer);
						}
						if (VERBOSE)
						{
							System.Console.Out.WriteLine("TEST: done adding docs; now commit");
						}
						writer.Commit();
					}
					catch (IOException e)
					{
						if (VERBOSE)
						{
							System.Console.Out.WriteLine("TEST: exception on addDoc");
							Sharpen.Runtime.PrintStackTrace(e, System.Console.Out);
						}
						hitError = true;
					}
					if (hitError)
					{
						if (doAbort)
						{
							if (VERBOSE)
							{
								System.Console.Out.WriteLine("TEST: now rollback");
							}
							writer.Rollback();
						}
						else
						{
							try
							{
								if (VERBOSE)
								{
									System.Console.Out.WriteLine("TEST: now close");
								}
								writer.Close();
							}
							catch (IOException e)
							{
								if (VERBOSE)
								{
									System.Console.Out.WriteLine("TEST: exception on close; retry w/ no disk space limit"
										);
									Sharpen.Runtime.PrintStackTrace(e, System.Console.Out);
								}
								dir.SetMaxSizeInBytes(0);
								writer.Close();
							}
						}
						//TestUtil.syncConcurrentMerges(ms);
						if (TestUtil.AnyFilesExceptWriteLock(dir))
						{
							TestIndexWriter.AssertNoUnreferencedFiles(dir, "after disk full during addDocument"
								);
							// Make sure reader can open the index:
							DirectoryReader.Open(dir).Close();
						}
						dir.Close();
						// Now try again w/ more space:
						diskFree += TEST_NIGHTLY ? TestUtil.NextInt(Random(), 400, 600) : TestUtil.NextInt
							(Random(), 3000, 5000);
					}
					else
					{
						//TestUtil.syncConcurrentMerges(writer);
						dir.SetMaxSizeInBytes(0);
						writer.Close();
						dir.Close();
						break;
					}
				}
			}
		}

		// TODO: make @Nightly variant that provokes more disk
		// fulls
		// TODO: have test fail if on any given top
		// iter there was not a single IOE hit
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestAddIndexOnDiskFull()
		{
			// MemoryCodec, since it uses FST, is not necessarily
			// "additive", ie if you add up N small FSTs, then merge
			// them, the merged result can easily be larger than the
			// sum because the merged FST may use array encoding for
			// some arcs (which uses more space):
			string idFormat = TestUtil.GetPostingsFormat("id");
			string contentFormat = TestUtil.GetPostingsFormat("content");
			AssumeFalse("This test cannot run with Memory codec", idFormat.Equals("Memory") ||
				 contentFormat.Equals("Memory"));
			int START_COUNT = 57;
			int NUM_DIR = TEST_NIGHTLY ? 50 : 5;
			int END_COUNT = START_COUNT + NUM_DIR * (TEST_NIGHTLY ? 25 : 5);
			// Build up a bunch of dirs that have indexes which we
			// will then merge together by calling addIndexes(*):
			Directory[] dirs = new Directory[NUM_DIR];
			long inputDiskUsage = 0;
			for (int i = 0; i < NUM_DIR; i++)
			{
				dirs[i] = NewDirectory();
				IndexWriter writer = new IndexWriter(dirs[i], NewIndexWriterConfig(TEST_VERSION_CURRENT
					, new MockAnalyzer(Random())));
				for (int j = 0; j < 25; j++)
				{
					AddDocWithIndex(writer, 25 * i + j);
				}
				writer.Close();
				string[] files = dirs[i].ListAll();
				for (int j_1 = 0; j_1 < files.Length; j_1++)
				{
					inputDiskUsage += dirs[i].FileLength(files[j_1]);
				}
			}
			// Now, build a starting index that has START_COUNT docs.  We
			// will then try to addIndexes into a copy of this:
			MockDirectoryWrapper startDir = NewMockDirectory();
			IndexWriter writer_1 = new IndexWriter(startDir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			for (int j_2 = 0; j_2 < START_COUNT; j_2++)
			{
				AddDocWithIndex(writer_1, j_2);
			}
			writer_1.Close();
			// Make sure starting index seems to be working properly:
			Term searchTerm = new Term("content", "aaa");
			IndexReader reader = DirectoryReader.Open(startDir);
			AreEqual("first docFreq", 57, reader.DocFreq(searchTerm));
			IndexSearcher searcher = NewSearcher(reader);
			ScoreDoc[] hits = searcher.Search(new TermQuery(searchTerm), null, 1000).scoreDocs;
			AreEqual("first number of hits", 57, hits.Length);
			reader.Close();
			// Iterate with larger and larger amounts of free
			// disk space.  With little free disk space,
			// addIndexes will certainly run out of space &
			// fail.  Verify that when this happens, index is
			// not corrupt and index in fact has added no
			// documents.  Then, we increase disk space by 2000
			// bytes each iteration.  At some point there is
			// enough free disk space and addIndexes should
			// succeed and index should show all documents were
			// added.
			// String[] files = startDir.listAll();
			long diskUsage = startDir.SizeInBytes();
			long startDiskUsage = 0;
			string[] files_1 = startDir.ListAll();
			for (int i_1 = 0; i_1 < files_1.Length; i_1++)
			{
				startDiskUsage += startDir.FileLength(files_1[i_1]);
			}
			for (int iter = 0; iter < 3; iter++)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: iter=" + iter);
				}
				// Start with 100 bytes more than we are currently using:
				long diskFree = diskUsage + TestUtil.NextInt(Random(), 50, 200);
				int method = iter;
				bool success = false;
				bool done = false;
				string methodName;
				if (0 == method)
				{
					methodName = "addIndexes(Directory[]) + forceMerge(1)";
				}
				else
				{
					if (1 == method)
					{
						methodName = "addIndexes(IndexReader[])";
					}
					else
					{
						methodName = "addIndexes(Directory[])";
					}
				}
				while (!done)
				{
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("TEST: cycle...");
					}
					// Make a new dir that will enforce disk usage:
					MockDirectoryWrapper dir = new MockDirectoryWrapper(Random(), new RAMDirectory(startDir
						, NewIOContext(Random())));
					writer_1 = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
						(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND).SetMergePolicy(NewLogMergePolicy
						(false)));
					IOException err = null;
					MergeScheduler ms = writer_1.GetConfig().GetMergeScheduler();
					for (int x = 0; x < 2; x++)
					{
						if (ms is ConcurrentMergeScheduler)
						{
							// This test intentionally produces exceptions
							// in the threads that CMS launches; we don't
							// want to pollute test output with these.
							if (0 == x)
							{
								((ConcurrentMergeScheduler)ms).SetSuppressExceptions();
							}
							else
							{
								((ConcurrentMergeScheduler)ms).ClearSuppressExceptions();
							}
						}
						// Two loops: first time, limit disk space &
						// throw random IOExceptions; second time, no
						// disk space limit:
						double rate = 0.05;
						double diskRatio = ((double)diskFree) / diskUsage;
						long thisDiskFree;
						string testName = null;
						if (0 == x)
						{
							dir.SetRandomIOExceptionRateOnOpen(Random().NextDouble() * 0.01);
							thisDiskFree = diskFree;
							if (diskRatio >= 2.0)
							{
								rate /= 2;
							}
							if (diskRatio >= 4.0)
							{
								rate /= 2;
							}
							if (diskRatio >= 6.0)
							{
								rate = 0.0;
							}
							if (VERBOSE)
							{
								testName = "disk full test " + methodName + " with disk full at " + diskFree + " bytes";
							}
						}
						else
						{
							dir.SetRandomIOExceptionRateOnOpen(0.0);
							thisDiskFree = 0;
							rate = 0.0;
							if (VERBOSE)
							{
								testName = "disk full test " + methodName + " with unlimited disk space";
							}
						}
						if (VERBOSE)
						{
							System.Console.Out.WriteLine("\ncycle: " + testName);
						}
						dir.SetTrackDiskUsage(true);
						dir.SetMaxSizeInBytes(thisDiskFree);
						dir.SetRandomIOExceptionRate(rate);
						try
						{
							if (0 == method)
							{
								if (VERBOSE)
								{
									System.Console.Out.WriteLine("TEST: now addIndexes count=" + dirs.Length);
								}
								writer_1.AddIndexes(dirs);
								if (VERBOSE)
								{
									System.Console.Out.WriteLine("TEST: now forceMerge");
								}
								writer_1.ForceMerge(1);
							}
							else
							{
								if (1 == method)
								{
									IndexReader[] readers = new IndexReader[dirs.Length];
									for (int i_2 = 0; i_2 < dirs.Length; i_2++)
									{
										readers[i_2] = DirectoryReader.Open(dirs[i_2]);
									}
									try
									{
										writer_1.AddIndexes(readers);
									}
									finally
									{
										for (int i_3 = 0; i_3 < dirs.Length; i_3++)
										{
											readers[i_3].Close();
										}
									}
								}
								else
								{
									writer_1.AddIndexes(dirs);
								}
							}
							success = true;
							if (VERBOSE)
							{
								System.Console.Out.WriteLine("  success!");
							}
							if (0 == x)
							{
								done = true;
							}
						}
						catch (IOException e)
						{
							success = false;
							err = e;
							if (VERBOSE)
							{
								System.Console.Out.WriteLine("  hit IOException: " + e);
								Sharpen.Runtime.PrintStackTrace(e, System.Console.Out);
							}
							if (1 == x)
							{
								Sharpen.Runtime.PrintStackTrace(e, System.Console.Out);
								Fail(methodName + " hit IOException after disk space was freed up"
									);
							}
						}
						// Make sure all threads from
						// ConcurrentMergeScheduler are done
						TestUtil.SyncConcurrentMerges(writer_1);
						if (VERBOSE)
						{
							System.Console.Out.WriteLine("  now test readers");
						}
						// Finally, verify index is not corrupt, and, if
						// we succeeded, we see all docs added, and if we
						// failed, we see either all docs or no docs added
						// (transactional semantics):
						dir.SetRandomIOExceptionRateOnOpen(0.0);
						try
						{
							reader = DirectoryReader.Open(dir);
						}
						catch (IOException e)
						{
							Sharpen.Runtime.PrintStackTrace(e, System.Console.Out);
							Fail(testName + ": exception when creating IndexReader: " 
								+ e);
						}
						int result = reader.DocFreq(searchTerm);
						if (success)
						{
							if (result != START_COUNT)
							{
								Fail(testName + ": method did not throw exception but docFreq('aaa') is "
									 + result + " instead of expected " + START_COUNT);
							}
						}
						else
						{
							// On hitting exception we still may have added
							// all docs:
							if (result != START_COUNT && result != END_COUNT)
							{
								Sharpen.Runtime.PrintStackTrace(err, System.Console.Out);
								Fail(testName + ": method did throw exception but docFreq('aaa') is "
									 + result + " instead of expected " + START_COUNT + " or " + END_COUNT);
							}
						}
						searcher = NewSearcher(reader);
						try
						{
							hits = searcher.Search(new TermQuery(searchTerm), null, END_COUNT).scoreDocs;
						}
						catch (IOException e)
						{
							Sharpen.Runtime.PrintStackTrace(e, System.Console.Out);
							Fail(testName + ": exception when searching: " + e);
						}
						int result2 = hits.Length;
						if (success)
						{
							if (result2 != result)
							{
								Fail(testName + ": method did not throw exception but hits.length for search on term 'aaa' is "
									 + result2 + " instead of expected " + result);
							}
						}
						else
						{
							// On hitting exception we still may have added
							// all docs:
							if (result2 != result)
							{
								Sharpen.Runtime.PrintStackTrace(err, System.Console.Out);
								Fail(testName + ": method did throw exception but hits.length for search on term 'aaa' is "
									 + result2 + " instead of expected " + result);
							}
						}
						reader.Close();
						if (VERBOSE)
						{
							System.Console.Out.WriteLine("  count is " + result);
						}
						if (done || result == END_COUNT)
						{
							break;
						}
					}
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("  start disk = " + startDiskUsage + "; input disk = "
							 + inputDiskUsage + "; max used = " + dir.GetMaxUsedSizeInBytes());
					}
					if (done)
					{
						// Javadocs state that temp free Directory space
						// required is at most 2X total input size of
						// indices so let's make sure:
						IsTrue("max free Directory space required exceeded 1X the total input index sizes during "
							 + methodName + ": max temp usage = " + (dir.GetMaxUsedSizeInBytes() - startDiskUsage
							) + " bytes vs limit=" + (2 * (startDiskUsage + inputDiskUsage)) + "; starting disk usage = "
							 + startDiskUsage + " bytes; " + "input index disk usage = " + inputDiskUsage + 
							" bytes", (dir.GetMaxUsedSizeInBytes() - startDiskUsage) < 2 * (startDiskUsage +
							 inputDiskUsage));
					}
					// Make sure we don't hit disk full during close below:
					dir.SetMaxSizeInBytes(0);
					dir.SetRandomIOExceptionRate(0.0);
					dir.SetRandomIOExceptionRateOnOpen(0.0);
					writer_1.Close();
					// Wait for all BG threads to finish else
					// dir.close() will throw IOException because
					// there are still open files
					TestUtil.SyncConcurrentMerges(ms);
					dir.Close();
					// Try again with more free space:
					diskFree += TEST_NIGHTLY ? TestUtil.NextInt(Random(), 4000, 8000) : TestUtil.NextInt
						(Random(), 40000, 80000);
				}
			}
			startDir.Close();
			foreach (Directory dir_1 in dirs)
			{
				dir_1.Close();
			}
		}

		private class FailTwiceDuringMerge : MockDirectoryWrapper.Failure
		{
			public bool didFail1;

			public bool didFail2;

			/// <exception cref="System.IO.IOException"></exception>
			public override void Eval(MockDirectoryWrapper dir)
			{
				if (!doFail)
				{
					return;
				}
				StackTraceElement[] trace = new Exception().GetStackTrace();
				for (int i = 0; i < trace.Length; i++)
				{
					if (typeof(SegmentMerger).FullName.Equals(trace[i].GetClassName()) && "mergeTerms"
						.Equals(trace[i].GetMethodName()) && !didFail1)
					{
						didFail1 = true;
						throw new IOException("fake disk full during mergeTerms");
					}
					if (typeof(LiveDocsFormat).FullName.Equals(trace[i].GetClassName()) && "writeLiveDocs"
						.Equals(trace[i].GetMethodName()) && !didFail2)
					{
						didFail2 = true;
						throw new IOException("fake disk full while writing LiveDocs");
					}
				}
			}
		}

		// LUCENE-2593
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestCorruptionAfterDiskFullDuringMerge()
		{
			MockDirectoryWrapper dir = NewMockDirectory();
			//IndexWriter w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random)).setReaderPooling(true));
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())).SetMergeScheduler(new SerialMergeScheduler()).SetReaderPooling
				(true).SetMergePolicy(NewLogMergePolicy(2)));
			// we can do this because we add/delete/add (and dont merge to "nothing")
			w.SetKeepFullyDeletedSegments(true);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewTextField("f", "doctor who", Field.Store.NO));
			w.AddDocument(doc);
			w.Commit();
			w.DeleteDocuments(new Term("f", "who"));
			w.AddDocument(doc);
			// disk fills up!
			TestIndexWriterOnDiskFull.FailTwiceDuringMerge ftdm = new TestIndexWriterOnDiskFull.FailTwiceDuringMerge
				();
			ftdm.SetDoFail();
			dir.FailOn(ftdm);
			try
			{
				w.Commit();
				Fail("fake disk full IOExceptions not hit");
			}
			catch (IOException)
			{
				// expected
				IsTrue(ftdm.didFail1 || ftdm.didFail2);
			}
			TestUtil.CheckIndex(dir);
			ftdm.ClearDoFail();
			w.AddDocument(doc);
			w.Close();
			dir.Close();
		}

		// LUCENE-1130: make sure immeidate disk full on creating
		// an IndexWriter (hit during DW.ThreadState.init()) is
		// OK:
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestImmediateDiskFull()
		{
			MockDirectoryWrapper dir = NewMockDirectory();
			IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2)).SetMergeScheduler
				(new ConcurrentMergeScheduler()));
			dir.SetMaxSizeInBytes(Math.Max(1, dir.GetRecomputedActualSizeInBytes()));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			FieldType customType = new FieldType(TextField.TYPE_STORED);
			doc.Add(NewField("field", "aaa bbb ccc ddd eee fff ggg hhh iii jjj", customType));
			try
			{
				writer.AddDocument(doc);
				Fail("did not hit disk full");
			}
			catch (IOException)
			{
			}
			// Without fix for LUCENE-1130: this call will hang:
			try
			{
				writer.AddDocument(doc);
				Fail("did not hit disk full");
			}
			catch (IOException)
			{
			}
			try
			{
				writer.Close(false);
				Fail("did not hit disk full");
			}
			catch (IOException)
			{
			}
			// Make sure once disk space is avail again, we can
			// cleanly close:
			dir.SetMaxSizeInBytes(0);
			writer.Close(false);
			dir.Close();
		}

		// TODO: these are also in TestIndexWriter... add a simple doc-writing method
		// like this to LuceneTestCase?
		/// <exception cref="System.IO.IOException"></exception>
		private void AddDoc(IndexWriter writer)
		{
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewTextField("content", "aaa", Field.Store.NO));
			if (DefaultCodecSupportsDocValues())
			{
				doc.Add(new NumericDocValuesField("numericdv", 1));
			}
			writer.AddDocument(doc);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void AddDocWithIndex(IndexWriter writer, int index)
		{
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewTextField("content", "aaa " + index, Field.Store.NO));
			doc.Add(NewTextField("id", string.Empty + index, Field.Store.NO));
			if (DefaultCodecSupportsDocValues())
			{
				doc.Add(new NumericDocValuesField("numericdv", 1));
			}
			writer.AddDocument(doc);
		}
	}
}
