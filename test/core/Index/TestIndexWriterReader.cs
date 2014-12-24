/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using NUnit.Framework;
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
	public class TestIndexWriterReader : LuceneTestCase
	{
		private readonly int numThreads = TEST_NIGHTLY ? 5 : 3;

		/// <exception cref="System.IO.IOException"></exception>
		public static int Count(Term t, IndexReader r)
		{
			int count = 0;
			DocsEnum td = TestUtil.Docs(Random(), r, t.Field(), new BytesRef(t.Text()), MultiFields
				.GetLiveDocs(r), null, 0);
			if (td != null)
			{
				while (td.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
				{
					td.DocID;
					count++;
				}
			}
			return count;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestAddCloseOpen()
		{
			// Can't use assertNoDeletes: this test pulls a non-NRT
			// reader in the end:
			Directory dir1 = NewDirectory();
			IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			IndexWriter writer = new IndexWriter(dir1, iwc);
			for (int i = 0; i < 97; i++)
			{
				DirectoryReader reader = writer.GetReader();
				if (i == 0)
				{
					writer.AddDocument(DocHelper.CreateDocument(i, "x", 1 + Random().Next(5)));
				}
				else
				{
					int previous = Random().Next(i);
					switch (Random().Next(5))
					{
						case 0:
						case 1:
						case 2:
						{
							// a check if the reader is current here could fail since there might be
							// merges going on.
							writer.AddDocument(DocHelper.CreateDocument(i, "x", 1 + Random().Next(5)));
							break;
						}

						case 3:
						{
							writer.UpdateDocument(new Term("id", string.Empty + previous), DocHelper.CreateDocument
								(previous, "x", 1 + Random().Next(5)));
							break;
						}

						case 4:
						{
							writer.DeleteDocuments(new Term("id", string.Empty + previous));
						}
					}
				}
				IsFalse(reader.IsCurrent());
				reader.Close();
			}
			writer.ForceMerge(1);
			// make sure all merging is done etc.
			DirectoryReader reader_1 = writer.GetReader();
			writer.Commit();
			// no changes that are not visible to the reader
			IsTrue(reader_1.IsCurrent());
			writer.Close();
			IsTrue(reader_1.IsCurrent());
			// all changes are visible to the reader
			iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
			writer = new IndexWriter(dir1, iwc);
			IsTrue(reader_1.IsCurrent());
			writer.AddDocument(DocHelper.CreateDocument(1, "x", 1 + Random().Next(5)));
			IsTrue(reader_1.IsCurrent());
			// segments in ram but IW is different to the readers one
			writer.Close();
			IsFalse(reader_1.IsCurrent());
			// segments written
			reader_1.Close();
			dir1.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestUpdateDocument()
		{
			bool doFullMerge = true;
			Directory dir1 = NewDirectory();
			IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			if (iwc.GetMaxBufferedDocs() < 20)
			{
				iwc.SetMaxBufferedDocs(20);
			}
			// no merging
			if (Random().NextBoolean())
			{
				iwc.SetMergePolicy(NoMergePolicy.NO_COMPOUND_FILES);
			}
			else
			{
				iwc.SetMergePolicy(NoMergePolicy.COMPOUND_FILES);
			}
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: make index");
			}
			IndexWriter writer = new IndexWriter(dir1, iwc);
			// create the index
			CreateIndexNoClose(!doFullMerge, "index1", writer);
			// writer.flush(false, true, true);
			// get a reader
			DirectoryReader r1 = writer.GetReader();
			IsTrue(r1.IsCurrent());
			string id10 = r1.Document(10).GetField("id").StringValue = );
			Lucene.Net.Documents.Document newDoc = r1.Document(10);
			newDoc.RemoveField("id");
			newDoc.Add(NewStringField("id", Sharpen.Extensions.ToString(8000), Field.Store.YES
				));
			writer.UpdateDocument(new Term("id", id10), newDoc);
			IsFalse(r1.IsCurrent());
			DirectoryReader r2 = writer.GetReader();
			IsTrue(r2.IsCurrent());
			AreEqual(0, Count(new Term("id", id10), r2));
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: verify id");
			}
			AreEqual(1, Count(new Term("id", Sharpen.Extensions.ToString
				(8000)), r2));
			r1.Close();
			IsTrue(r2.IsCurrent());
			writer.Close();
			IsTrue(r2.IsCurrent());
			DirectoryReader r3 = DirectoryReader.Open(dir1);
			IsTrue(r3.IsCurrent());
			IsTrue(r2.IsCurrent());
			AreEqual(0, Count(new Term("id", id10), r3));
			AreEqual(1, Count(new Term("id", Sharpen.Extensions.ToString
				(8000)), r3));
			writer = new IndexWriter(dir1, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewTextField("field", "a b c", Field.Store.NO));
			writer.AddDocument(doc);
			IsTrue(r2.IsCurrent());
			IsTrue(r3.IsCurrent());
			writer.Close();
			IsFalse(r2.IsCurrent());
			IsTrue(!r3.IsCurrent());
			r2.Close();
			r3.Close();
			dir1.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestIsCurrent()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			IndexWriter writer = new IndexWriter(dir, iwc);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewTextField("field", "a b c", Field.Store.NO));
			writer.AddDocument(doc);
			writer.Close();
			iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
			writer = new IndexWriter(dir, iwc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewTextField("field", "a b c", Field.Store.NO));
			DirectoryReader nrtReader = writer.GetReader();
			IsTrue(nrtReader.IsCurrent());
			writer.AddDocument(doc);
			IsFalse(nrtReader.IsCurrent());
			// should see the changes
			writer.ForceMerge(1);
			// make sure we don't have a merge going on
			IsFalse(nrtReader.IsCurrent());
			nrtReader.Close();
			DirectoryReader dirReader = DirectoryReader.Open(dir);
			nrtReader = writer.GetReader();
			IsTrue(dirReader.IsCurrent());
			IsTrue(nrtReader.IsCurrent());
			// nothing was committed yet so we are still current
			AreEqual(2, nrtReader.MaxDoc);
			// sees the actual document added
			AreEqual(1, dirReader.MaxDoc);
			writer.Close();
			// close is actually a commit both should see the changes
			IsTrue(nrtReader.IsCurrent());
			IsFalse(dirReader.IsCurrent());
			// this reader has been opened before the writer was closed / committed
			dirReader.Close();
			nrtReader.Close();
			dir.Close();
		}

		/// <summary>Test using IW.addIndexes</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestAddIndexes()
		{
			bool doFullMerge = false;
			Directory dir1 = GetAssertNoDeletesDirectory(NewDirectory());
			IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			if (iwc.GetMaxBufferedDocs() < 20)
			{
				iwc.SetMaxBufferedDocs(20);
			}
			// no merging
			if (Random().NextBoolean())
			{
				iwc.SetMergePolicy(NoMergePolicy.NO_COMPOUND_FILES);
			}
			else
			{
				iwc.SetMergePolicy(NoMergePolicy.COMPOUND_FILES);
			}
			IndexWriter writer = new IndexWriter(dir1, iwc);
			// create the index
			CreateIndexNoClose(!doFullMerge, "index1", writer);
			writer.Flush(false, true);
			// create a 2nd index
			Directory dir2 = NewDirectory();
			IndexWriter writer2 = new IndexWriter(dir2, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			CreateIndexNoClose(!doFullMerge, "index2", writer2);
			writer2.Close();
			DirectoryReader r0 = writer.GetReader();
			IsTrue(r0.IsCurrent());
			writer.AddIndexes(dir2);
			IsFalse(r0.IsCurrent());
			r0.Close();
			DirectoryReader r1 = writer.GetReader();
			IsTrue(r1.IsCurrent());
			writer.Commit();
			IsTrue(r1.IsCurrent());
			// we have seen all changes - no change after opening the NRT reader
			AreEqual(200, r1.MaxDoc);
			int index2df = r1.DocFreq(new Term("indexname", "index2"));
			AreEqual(100, index2df);
			// verify the docs are from different indexes
			Lucene.Net.Documents.Document doc5 = r1.Document(5);
			AreEqual("index1", doc5.Get("indexname"));
			Lucene.Net.Documents.Document doc150 = r1.Document(150);
			AreEqual("index2", doc150.Get("indexname"));
			r1.Close();
			writer.Close();
			dir1.Close();
			dir2.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestAddIndexes2()
		{
			bool doFullMerge = false;
			Directory dir1 = GetAssertNoDeletesDirectory(NewDirectory());
			IndexWriter writer = new IndexWriter(dir1, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			// create a 2nd index
			Directory dir2 = NewDirectory();
			IndexWriter writer2 = new IndexWriter(dir2, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			CreateIndexNoClose(!doFullMerge, "index2", writer2);
			writer2.Close();
			writer.AddIndexes(dir2);
			writer.AddIndexes(dir2);
			writer.AddIndexes(dir2);
			writer.AddIndexes(dir2);
			writer.AddIndexes(dir2);
			IndexReader r1 = writer.GetReader();
			AreEqual(500, r1.MaxDoc);
			r1.Close();
			writer.Close();
			dir1.Close();
			dir2.Close();
		}

		/// <summary>Deletes using IW.deleteDocuments</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestDeleteFromIndexWriter()
		{
			bool doFullMerge = true;
			Directory dir1 = GetAssertNoDeletesDirectory(NewDirectory());
			IndexWriter writer = new IndexWriter(dir1, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetReaderTermsIndexDivisor(2)
				));
			// create the index
			CreateIndexNoClose(!doFullMerge, "index1", writer);
			writer.Flush(false, true);
			// get a reader
			IndexReader r1 = writer.GetReader();
			string id10 = r1.Document(10).GetField("id").StringValue = );
			// deleted IW docs should not show up in the next getReader
			writer.DeleteDocuments(new Term("id", id10));
			IndexReader r2 = writer.GetReader();
			AreEqual(1, Count(new Term("id", id10), r1));
			AreEqual(0, Count(new Term("id", id10), r2));
			string id50 = r1.Document(50).GetField("id").StringValue = );
			AreEqual(1, Count(new Term("id", id50), r1));
			writer.DeleteDocuments(new Term("id", id50));
			IndexReader r3 = writer.GetReader();
			AreEqual(0, Count(new Term("id", id10), r3));
			AreEqual(0, Count(new Term("id", id50), r3));
			string id75 = r1.Document(75).GetField("id").StringValue = );
			writer.DeleteDocuments(new TermQuery(new Term("id", id75)));
			IndexReader r4 = writer.GetReader();
			AreEqual(1, Count(new Term("id", id75), r3));
			AreEqual(0, Count(new Term("id", id75), r4));
			r1.Close();
			r2.Close();
			r3.Close();
			r4.Close();
			writer.Close();
			// reopen the writer to verify the delete made it to the directory
			writer = new IndexWriter(dir1, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())));
			IndexReader w2r1 = writer.GetReader();
			AreEqual(0, Count(new Term("id", id10), w2r1));
			w2r1.Close();
			writer.Close();
			dir1.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestAddIndexesAndDoDeletesThreads()
		{
			int numIter = 2;
			int numDirs = 3;
			Directory mainDir = GetAssertNoDeletesDirectory(NewDirectory());
			IndexWriter mainWriter = new IndexWriter(mainDir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy()));
			TestUtil.ReduceOpenFiles(mainWriter);
			TestIndexWriterReader.AddDirectoriesThreads addDirThreads = new TestIndexWriterReader.AddDirectoriesThreads
				(this, numIter, mainWriter);
			addDirThreads.LaunchThreads(numDirs);
			addDirThreads.JoinThreads();
			//assertEquals(100 + numDirs * (3 * numIter / 4) * addDirThreads.numThreads
			//    * addDirThreads.NUM_INIT_DOCS, addDirThreads.mainWriter.numDocs());
			AreEqual(addDirThreads.count, addDirThreads.mainWriter.NumDocs
				());
			addDirThreads.Close(true);
			IsTrue(addDirThreads.failures.Count == 0);
			TestUtil.CheckIndex(mainDir);
			IndexReader reader = DirectoryReader.Open(mainDir);
			AreEqual(addDirThreads.count, reader.NumDocs());
			//assertEquals(100 + numDirs * (3 * numIter / 4) * addDirThreads.numThreads
			//    * addDirThreads.NUM_INIT_DOCS, reader.numDocs());
			reader.Close();
			addDirThreads.CloseDir();
			mainDir.Close();
		}

		private class AddDirectoriesThreads
		{
			internal Directory addDir;

			internal const int NUM_INIT_DOCS = 100;

			internal int numDirs;

			internal readonly Sharpen.Thread[] threads = new Sharpen.Thread[this._enclosing.numThreads
				];

			internal IndexWriter mainWriter;

			internal readonly IList<Exception> failures = new AList<Exception>();

			internal IndexReader[] readers;

			internal bool didClose = false;

			internal AtomicInteger count = new AtomicInteger(0);

			internal AtomicInteger numaddIndexes = new AtomicInteger(0);

			/// <exception cref="System.Exception"></exception>
			public AddDirectoriesThreads(TestIndexWriterReader _enclosing, int numDirs, IndexWriter
				 mainWriter)
			{
				this._enclosing = _enclosing;
				this.numDirs = numDirs;
				this.mainWriter = mainWriter;
				this.addDir = LuceneTestCase.NewDirectory();
				IndexWriter writer = new IndexWriter(this.addDir, ((IndexWriterConfig)LuceneTestCase
					.NewIndexWriterConfig(LuceneTestCase.TEST_VERSION_CURRENT, new MockAnalyzer(LuceneTestCase
					.Random())).SetMaxBufferedDocs(2)));
				TestUtil.ReduceOpenFiles(writer);
				for (int i = 0; i < TestIndexWriterReader.AddDirectoriesThreads.NUM_INIT_DOCS; i++)
				{
					Lucene.Net.Documents.Document doc = DocHelper.CreateDocument(i, "addindex", 
						4);
					writer.AddDocument(doc);
				}
				writer.Close();
				this.readers = new IndexReader[numDirs];
				for (int i_1 = 0; i_1 < numDirs; i_1++)
				{
					this.readers[i_1] = DirectoryReader.Open(this.addDir);
				}
			}

			internal virtual void JoinThreads()
			{
				for (int i = 0; i < this._enclosing.numThreads; i++)
				{
					try
					{
						this.threads[i].Join();
					}
					catch (Exception ie)
					{
						throw new ThreadInterruptedException(ie);
					}
				}
			}

			/// <exception cref="System.Exception"></exception>
			internal virtual void Close(bool doWait)
			{
				this.didClose = true;
				if (doWait)
				{
					this.mainWriter.WaitForMerges();
				}
				this.mainWriter.Close(doWait);
			}

			/// <exception cref="System.Exception"></exception>
			internal virtual void CloseDir()
			{
				for (int i = 0; i < this.numDirs; i++)
				{
					this.readers[i].Close();
				}
				this.addDir.Close();
			}

			internal virtual void Handle(Exception t)
			{
				Sharpen.Runtime.PrintStackTrace(t, System.Console.Out);
				lock (this.failures)
				{
					this.failures.AddItem(t);
				}
			}

			internal virtual void LaunchThreads(int numIter)
			{
				for (int i = 0; i < this._enclosing.numThreads; i++)
				{
					this.threads[i] = new _Thread_463(this, numIter);
				}
				//int j = 0;
				//while (true) {
				// System.out.println(Thread.currentThread().getName() + ": iter
				// j=" + j);
				// only do addIndexes
				//if (numIter > 0 && j == numIter)
				//  break;
				//doBody(j++, dirs);
				//doBody(5, dirs);
				//}
				for (int i_1 = 0; i_1 < this._enclosing.numThreads; i_1++)
				{
					this.threads[i_1].Start();
				}
			}

			private sealed class _Thread_463 : Sharpen.Thread
			{
				public _Thread_463(AddDirectoriesThreads _enclosing, int numIter)
				{
					this._enclosing = _enclosing;
					this.numIter = numIter;
				}

				public override void Run()
				{
					try
					{
						Directory[] dirs = new Directory[this._enclosing.numDirs];
						for (int k = 0; k < this._enclosing.numDirs; k++)
						{
							dirs[k] = new MockDirectoryWrapper(LuceneTestCase.Random(), new RAMDirectory(this
								._enclosing.addDir, LuceneTestCase.NewIOContext(LuceneTestCase.Random())));
						}
						for (int x = 0; x < numIter; x++)
						{
							this._enclosing.DoBody(x, dirs);
						}
					}
					catch (Exception t)
					{
						this._enclosing.Handle(t);
					}
				}

				private readonly AddDirectoriesThreads _enclosing;

				private readonly int numIter;
			}

			/// <exception cref="System.Exception"></exception>
			internal virtual void DoBody(int j, Directory[] dirs)
			{
				switch (j % 4)
				{
					case 0:
					{
						this.mainWriter.AddIndexes(dirs);
						this.mainWriter.ForceMerge(1);
						break;
					}

					case 1:
					{
						this.mainWriter.AddIndexes(dirs);
						this.numaddIndexes.IncrementAndGet();
						break;
					}

					case 2:
					{
						this.mainWriter.AddIndexes(this.readers);
						break;
					}

					case 3:
					{
						this.mainWriter.Commit();
					}
				}
				this.count.AddAndGet(dirs.Length * TestIndexWriterReader.AddDirectoriesThreads.NUM_INIT_DOCS
					);
			}

			private readonly TestIndexWriterReader _enclosing;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestIndexWriterReopenSegmentFullMerge()
		{
			DoTestIndexWriterReopenSegment(true);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestIndexWriterReopenSegment()
		{
			DoTestIndexWriterReopenSegment(false);
		}

		/// <summary>
		/// Tests creating a segment, then check to insure the segment can be seen via
		/// IW.getReader
		/// </summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void DoTestIndexWriterReopenSegment(bool doFullMerge)
		{
			Directory dir1 = GetAssertNoDeletesDirectory(NewDirectory());
			IndexWriter writer = new IndexWriter(dir1, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			IndexReader r1 = writer.GetReader();
			AreEqual(0, r1.MaxDoc);
			CreateIndexNoClose(false, "index1", writer);
			writer.Flush(!doFullMerge, true);
			IndexReader iwr1 = writer.GetReader();
			AreEqual(100, iwr1.MaxDoc);
			IndexReader r2 = writer.GetReader();
			AreEqual(r2.MaxDoc, 100);
			// add 100 documents
			for (int x = 10000; x < 10000 + 100; x++)
			{
				Lucene.Net.Documents.Document d = DocHelper.CreateDocument(x, "index1", 5);
				writer.AddDocument(d);
			}
			writer.Flush(false, true);
			// verify the reader was reopened internally
			IndexReader iwr2 = writer.GetReader();
			IsTrue(iwr2 != r1);
			AreEqual(200, iwr2.MaxDoc);
			// should have flushed out a segment
			IndexReader r3 = writer.GetReader();
			IsTrue(r2 != r3);
			AreEqual(200, r3.MaxDoc);
			// dec ref the readers rather than close them because
			// closing flushes changes to the writer
			r1.Close();
			iwr1.Close();
			r2.Close();
			r3.Close();
			iwr2.Close();
			writer.Close();
			// test whether the changes made it to the directory
			writer = new IndexWriter(dir1, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())));
			IndexReader w2r1 = writer.GetReader();
			// insure the deletes were actually flushed to the directory
			AreEqual(200, w2r1.MaxDoc);
			w2r1.Close();
			writer.Close();
			dir1.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static void CreateIndex(Random random, Directory dir1, string indexName, bool
			 multiSegment)
		{
			IndexWriter w = new IndexWriter(dir1, LuceneTestCase.NewIndexWriterConfig(random, 
				TEST_VERSION_CURRENT, new MockAnalyzer(random)).SetMergePolicy(new LogDocMergePolicy
				()));
			for (int i = 0; i < 100; i++)
			{
				w.AddDocument(DocHelper.CreateDocument(i, indexName, 4));
			}
			if (!multiSegment)
			{
				w.ForceMerge(1);
			}
			w.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static void CreateIndexNoClose(bool multiSegment, string indexName, IndexWriter
			 w)
		{
			for (int i = 0; i < 100; i++)
			{
				w.AddDocument(DocHelper.CreateDocument(i, indexName, 4));
			}
			if (!multiSegment)
			{
				w.ForceMerge(1);
			}
		}

		private class MyWarmer : IndexWriter.IndexReaderWarmer
		{
			internal int warmCount;

			/// <exception cref="System.IO.IOException"></exception>
			public override void Warm(AtomicReader reader)
			{
				warmCount++;
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestMergeWarmer()
		{
			Directory dir1 = GetAssertNoDeletesDirectory(NewDirectory());
			// Enroll warmer
			TestIndexWriterReader.MyWarmer warmer = new TestIndexWriterReader.MyWarmer();
			IndexWriter writer = new IndexWriter(dir1, ((IndexWriterConfig)((IndexWriterConfig
				)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs
				(2)).SetMergedSegmentWarmer(warmer)).SetMergeScheduler(new ConcurrentMergeScheduler
				()).SetMergePolicy(NewLogMergePolicy()));
			// create the index
			CreateIndexNoClose(false, "test", writer);
			// get a reader to put writer into near real-time mode
			IndexReader r1 = writer.GetReader();
			((LogMergePolicy)writer.GetConfig().GetMergePolicy()).SetMergeFactor(2);
			int num = AtLeast(100);
			for (int i = 0; i < num; i++)
			{
				writer.AddDocument(DocHelper.CreateDocument(i, "test", 4));
			}
			((ConcurrentMergeScheduler)writer.GetConfig().GetMergeScheduler()).Sync();
			IsTrue(warmer.warmCount > 0);
			int count = warmer.warmCount;
			writer.AddDocument(DocHelper.CreateDocument(17, "test", 4));
			writer.ForceMerge(1);
			IsTrue(warmer.warmCount > count);
			writer.Close();
			r1.Close();
			dir1.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestAfterCommit()
		{
			Directory dir1 = GetAssertNoDeletesDirectory(NewDirectory());
			IndexWriter writer = new IndexWriter(dir1, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetMergeScheduler(new ConcurrentMergeScheduler()));
			writer.Commit();
			// create the index
			CreateIndexNoClose(false, "test", writer);
			// get a reader to put writer into near real-time mode
			DirectoryReader r1 = writer.GetReader();
			TestUtil.CheckIndex(dir1);
			writer.Commit();
			TestUtil.CheckIndex(dir1);
			AreEqual(100, r1.NumDocs());
			for (int i = 0; i < 10; i++)
			{
				writer.AddDocument(DocHelper.CreateDocument(i, "test", 4));
			}
			((ConcurrentMergeScheduler)writer.GetConfig().GetMergeScheduler()).Sync();
			DirectoryReader r2 = DirectoryReader.OpenIfChanged(r1);
			if (r2 != null)
			{
				r1.Close();
				r1 = r2;
			}
			AreEqual(110, r1.NumDocs());
			writer.Close();
			r1.Close();
			dir1.Close();
		}

		// Make sure reader remains usable even if IndexWriter closes
		/// <exception cref="System.Exception"></exception>
		public virtual void TestAfterClose()
		{
			Directory dir1 = GetAssertNoDeletesDirectory(NewDirectory());
			IndexWriter writer = new IndexWriter(dir1, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			// create the index
			CreateIndexNoClose(false, "test", writer);
			DirectoryReader r = writer.GetReader();
			writer.Close();
			TestUtil.CheckIndex(dir1);
			// reader should remain usable even after IndexWriter is closed:
			AreEqual(100, r.NumDocs());
			Query q = new TermQuery(new Term("indexname", "test"));
			IndexSearcher searcher = NewSearcher(r);
			AreEqual(100, searcher.Search(q, 10).TotalHits);
			try
			{
				DirectoryReader.OpenIfChanged(r);
				Fail("failed to hit AlreadyClosedException");
			}
			catch (AlreadyClosedException)
			{
			}
			// expected
			r.Close();
			dir1.Close();
		}

		// Stress test reopen during addIndexes
		/// <exception cref="System.Exception"></exception>
		public virtual void TestDuringAddIndexes()
		{
			Directory dir1 = GetAssertNoDeletesDirectory(NewDirectory());
			IndexWriter writer = new IndexWriter(dir1, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy(2)));
			// create the index
			CreateIndexNoClose(false, "test", writer);
			writer.Commit();
			Directory[] dirs = new Directory[10];
			for (int i = 0; i < 10; i++)
			{
				dirs[i] = new MockDirectoryWrapper(Random(), new RAMDirectory(dir1, NewIOContext(
					Random())));
			}
			DirectoryReader r = writer.GetReader();
			float SECONDS = 0.5f;
			long endTime = (long)(Runtime.CurrentTimeMillis() + 1000. * SECONDS);
			IList<Exception> excs = Sharpen.Collections.SynchronizedList(new AList<Exception>
				());
			// Only one thread can addIndexes at a time, because
			// IndexWriter acquires a write lock in each directory:
			Sharpen.Thread[] threads = new Sharpen.Thread[1];
			for (int i_1 = 0; i_1 < threads.Length; i_1++)
			{
				threads[i_1] = new _Thread_742(writer, dirs, excs, endTime);
				threads[i_1].SetDaemon(true);
				threads[i_1].Start();
			}
			int lastCount = 0;
			while (Runtime.CurrentTimeMillis() < endTime)
			{
				DirectoryReader r2 = DirectoryReader.OpenIfChanged(r);
				if (r2 != null)
				{
					r.Close();
					r = r2;
				}
				Query q = new TermQuery(new Term("indexname", "test"));
				IndexSearcher searcher = NewSearcher(r);
				int count = searcher.Search(q, 10).TotalHits;
				IsTrue(count >= lastCount);
				lastCount = count;
			}
			for (int i_2 = 0; i_2 < threads.Length; i_2++)
			{
				threads[i_2].Join();
			}
			// final check
			DirectoryReader r2_1 = DirectoryReader.OpenIfChanged(r);
			if (r2_1 != null)
			{
				r.Close();
				r = r2_1;
			}
			Query q_1 = new TermQuery(new Term("indexname", "test"));
			IndexSearcher searcher_1 = NewSearcher(r);
			int count_1 = searcher_1.Search(q_1, 10).TotalHits;
			IsTrue(count_1 >= lastCount);
			AreEqual(0, excs.Count);
			r.Close();
			if (dir1 is MockDirectoryWrapper)
			{
				ICollection<string> openDeletedFiles = ((MockDirectoryWrapper)dir1).GetOpenDeletedFiles
					();
				AreEqual("openDeleted=" + openDeletedFiles, 0, openDeletedFiles
					.Count);
			}
			writer.Close();
			dir1.Close();
		}

		private sealed class _Thread_742 : Sharpen.Thread
		{
			public _Thread_742(IndexWriter writer, Directory[] dirs, IList<Exception> excs, long
				 endTime)
			{
				this.writer = writer;
				this.dirs = dirs;
				this.excs = excs;
				this.endTime = endTime;
			}

			public override void Run()
			{
				do
				{
					try
					{
						writer.AddIndexes(dirs);
						writer.MaybeMerge();
					}
					catch (Exception t)
					{
						excs.AddItem(t);
						throw new RuntimeException(t);
					}
				}
				while (Runtime.CurrentTimeMillis() < endTime);
			}

			private readonly IndexWriter writer;

			private readonly Directory[] dirs;

			private readonly IList<Exception> excs;

			private readonly long endTime;
		}

		private Directory GetAssertNoDeletesDirectory(Directory directory)
		{
			if (directory is MockDirectoryWrapper)
			{
				((MockDirectoryWrapper)directory).SetAssertNoDeleteOpenFile(true);
			}
			return directory;
		}

		// Stress test reopen during add/delete
		/// <exception cref="System.Exception"></exception>
		public virtual void TestDuringAddDelete()
		{
			Directory dir1 = NewDirectory();
			IndexWriter writer = new IndexWriter(dir1, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy(2)));
			// create the index
			CreateIndexNoClose(false, "test", writer);
			writer.Commit();
			DirectoryReader r = writer.GetReader();
			float SECONDS = 0.5f;
			long endTime = (long)(Runtime.CurrentTimeMillis() + 1000. * SECONDS);
			IList<Exception> excs = Sharpen.Collections.SynchronizedList(new AList<Exception>
				());
			Sharpen.Thread[] threads = new Sharpen.Thread[numThreads];
			for (int i = 0; i < numThreads; i++)
			{
				threads[i] = new _Thread_829(writer, excs, endTime);
				threads[i].SetDaemon(true);
				threads[i].Start();
			}
			int sum = 0;
			while (Runtime.CurrentTimeMillis() < endTime)
			{
				DirectoryReader r2 = DirectoryReader.OpenIfChanged(r);
				if (r2 != null)
				{
					r.Close();
					r = r2;
				}
				Query q = new TermQuery(new Term("indexname", "test"));
				IndexSearcher searcher = NewSearcher(r);
				sum += searcher.Search(q, 10).TotalHits;
			}
			for (int i_1 = 0; i_1 < numThreads; i_1++)
			{
				threads[i_1].Join();
			}
			// at least search once
			DirectoryReader r2_1 = DirectoryReader.OpenIfChanged(r);
			if (r2_1 != null)
			{
				r.Close();
				r = r2_1;
			}
			Query q_1 = new TermQuery(new Term("indexname", "test"));
			IndexSearcher searcher_1 = NewSearcher(r);
			sum += searcher_1.Search(q_1, 10).TotalHits;
			IsTrue("no documents found at all", sum > 0);
			AreEqual(0, excs.Count);
			writer.Close();
			r.Close();
			dir1.Close();
		}

		private sealed class _Thread_829 : Sharpen.Thread
		{
			public _Thread_829(IndexWriter writer, IList<Exception> excs, long endTime)
			{
				this.writer = writer;
				this.excs = excs;
				this.endTime = endTime;
				this.r = new Random(LuceneTestCase.Random().NextLong());
			}

			internal readonly Random r;

			public override void Run()
			{
				int count = 0;
				do
				{
					try
					{
						for (int docUpto = 0; docUpto < 10; docUpto++)
						{
							writer.AddDocument(DocHelper.CreateDocument(10 * count + docUpto, "test", 4));
						}
						count++;
						int limit = count * 10;
						for (int delUpto = 0; delUpto < 5; delUpto++)
						{
							int x = this.r.Next(limit);
							writer.DeleteDocuments(new Term("field3", "b" + x));
						}
					}
					catch (Exception t)
					{
						excs.AddItem(t);
						throw new RuntimeException(t);
					}
				}
				while (Runtime.CurrentTimeMillis() < endTime);
			}

			private readonly IndexWriter writer;

			private readonly IList<Exception> excs;

			private readonly long endTime;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestForceMergeDeletes()
		{
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy()));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewTextField("field", "a b c", Field.Store.NO));
			Field id = NewStringField("id", string.Empty, Field.Store.NO);
			doc.Add(id);
			id.StringValue = "0");
			w.AddDocument(doc);
			id.StringValue = "1");
			w.AddDocument(doc);
			w.DeleteDocuments(new Term("id", "0"));
			IndexReader r = w.GetReader();
			w.ForceMergeDeletes();
			w.Close();
			r.Close();
			r = DirectoryReader.Open(dir);
			AreEqual(1, r.NumDocs());
			IsFalse(r.HasDeletions());
			r.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestDeletesNumDocs()
		{
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewTextField("field", "a b c", Field.Store.NO));
			Field id = NewStringField("id", string.Empty, Field.Store.NO);
			doc.Add(id);
			id.StringValue = "0");
			w.AddDocument(doc);
			id.StringValue = "1");
			w.AddDocument(doc);
			IndexReader r = w.GetReader();
			AreEqual(2, r.NumDocs());
			r.Close();
			w.DeleteDocuments(new Term("id", "0"));
			r = w.GetReader();
			AreEqual(1, r.NumDocs());
			r.Close();
			w.DeleteDocuments(new Term("id", "1"));
			r = w.GetReader();
			AreEqual(0, r.NumDocs());
			r.Close();
			w.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestEmptyIndex()
		{
			// Ensures that getReader works on an empty index, which hasn't been committed yet.
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())));
			IndexReader r = w.GetReader();
			AreEqual(0, r.NumDocs());
			r.Close();
			w.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSegmentWarmer()
		{
			Directory dir = NewDirectory();
			AtomicBoolean didWarm = new AtomicBoolean();
			IndexWriter w = new IndexWriter(dir, ((IndexWriterConfig)((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2)).SetReaderPooling
				(true).SetMergedSegmentWarmer(new _IndexReaderWarmer_962(didWarm))).SetMergePolicy
				(NewLogMergePolicy(10)));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewStringField("foo", "bar", Field.Store.NO));
			for (int i = 0; i < 20; i++)
			{
				w.AddDocument(doc);
			}
			w.WaitForMerges();
			w.Close();
			dir.Close();
			IsTrue(didWarm.Get());
		}

		private sealed class _IndexReaderWarmer_962 : IndexWriter.IndexReaderWarmer
		{
			public _IndexReaderWarmer_962(AtomicBoolean didWarm)
			{
				this.didWarm = didWarm;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Warm(AtomicReader r)
			{
				IndexSearcher s = LuceneTestCase.NewSearcher(r);
				TopDocs hits = s.Search(new TermQuery(new Term("foo", "bar")), 10);
				AreEqual(20, hits.TotalHits);
				didWarm.Set(true);
			}

			private readonly AtomicBoolean didWarm;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSimpleMergedSegmentWramer()
		{
			Directory dir = NewDirectory();
			AtomicBoolean didWarm = new AtomicBoolean();
			InfoStream infoStream = new _InfoStream_988(didWarm);
			IndexWriter w = new IndexWriter(dir, ((IndexWriterConfig)((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2)).SetReaderPooling
				(true).SetInfoStream(infoStream).SetMergedSegmentWarmer(new SimpleMergedSegmentWarmer
				(infoStream))).SetMergePolicy(NewLogMergePolicy(10)));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewStringField("foo", "bar", Field.Store.NO));
			for (int i = 0; i < 20; i++)
			{
				w.AddDocument(doc);
			}
			w.WaitForMerges();
			w.Close();
			dir.Close();
			IsTrue(didWarm.Get());
		}

		private sealed class _InfoStream_988 : InfoStream
		{
			public _InfoStream_988(AtomicBoolean didWarm)
			{
				this.didWarm = didWarm;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Close()
			{
			}

			public override void Message(string component, string message)
			{
				if ("SMSW".Equals(component))
				{
					didWarm.Set(true);
				}
			}

			public override bool IsEnabled(string component)
			{
				return true;
			}

			private readonly AtomicBoolean didWarm;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestNoTermsIndex()
		{
			// Some Codecs don't honor the ReaderTermsIndexDivisor, so skip the test if
			// they're picked.
			AssumeFalse("PreFlex codec does not support ReaderTermsIndexDivisor!", "Lucene3x"
				.Equals(Codec.GetDefault().GetName()));
			IndexWriterConfig conf = ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetReaderTermsIndexDivisor(-1));
			// Don't proceed if picked Codec is in the list of illegal ones.
			string format = TestUtil.GetPostingsFormat("f");
			AssumeFalse("Format: " + format + " does not support ReaderTermsIndexDivisor!", (
				format.Equals("FSTPulsing41") || format.Equals("FSTOrdPulsing41") || format.Equals
				("FST41") || format.Equals("FSTOrd41") || format.Equals("SimpleText") || format.
				Equals("Memory") || format.Equals("MockRandom") || format.Equals("Direct")));
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, conf);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new TextField("f", "val", Field.Store.NO));
			w.AddDocument(doc);
			SegmentReader r = GetOnlySegmentReader(DirectoryReader.Open(w, true));
			try
			{
				TestUtil.Docs(Random(), r, "f", new BytesRef("val"), null, null, DocsEnum.FLAG_NONE
					);
				Fail("should have failed to seek since terms index was not loaded."
					);
			}
			catch (InvalidOperationException)
			{
			}
			finally
			{
				// expected - we didn't load the term index
				r.Close();
				w.Close();
				dir.Close();
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestReopenAfterNoRealChange()
		{
			Directory d = GetAssertNoDeletesDirectory(NewDirectory());
			IndexWriter w = new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())));
			DirectoryReader r = w.GetReader();
			// start pooling readers
			DirectoryReader r2 = DirectoryReader.OpenIfChanged(r);
			IsNull(r2);
			w.AddDocument(new Lucene.Net.Documents.Document());
			DirectoryReader r3 = DirectoryReader.OpenIfChanged(r);
			IsNotNull(r3);
			IsTrue(r3.GetVersion() != r.GetVersion());
			IsTrue(r3.IsCurrent());
			// Deletes nothing in reality...:
			w.DeleteDocuments(new Term("foo", "bar"));
			// ... but IW marks this as not current:
			IsFalse(r3.IsCurrent());
			DirectoryReader r4 = DirectoryReader.OpenIfChanged(r3);
			IsNull(r4);
			// Deletes nothing in reality...:
			w.DeleteDocuments(new Term("foo", "bar"));
			DirectoryReader r5 = DirectoryReader.OpenIfChanged(r3, w, true);
			IsNull(r5);
			r3.Close();
			w.Close();
			d.Close();
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestNRTOpenExceptions()
		{
			// LUCENE-5262: test that several failed attempts to obtain an NRT reader
			// don't leak file handles.
			MockDirectoryWrapper dir = (MockDirectoryWrapper)GetAssertNoDeletesDirectory(NewMockDirectory
				());
			AtomicBoolean shouldFail = new AtomicBoolean();
			dir.FailOn(new _Failure_1106(shouldFail));
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			conf.SetMergePolicy(NoMergePolicy.COMPOUND_FILES);
			// prevent merges from getting in the way
			IndexWriter writer = new IndexWriter(dir, conf);
			// create a segment and open an NRT reader
			writer.AddDocument(new Lucene.Net.Documents.Document());
			writer.GetReader().Close();
			// add a new document so a new NRT reader is required
			writer.AddDocument(new Lucene.Net.Documents.Document());
			// try to obtain an NRT reader twice: first time it fails and closes all the
			// other NRT readers. second time it fails, but also fails to close the
			// other NRT reader, since it is already marked closed!
			for (int i = 0; i < 2; i++)
			{
				shouldFail.Set(true);
				try
				{
					writer.GetReader().Close();
				}
				catch (MockDirectoryWrapper.FakeIOException)
				{
					// expected
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("hit expected fake IOE");
					}
				}
			}
			writer.Close();
			dir.Close();
		}

		private sealed class _Failure_1106 : MockDirectoryWrapper.Failure
		{
			public _Failure_1106(AtomicBoolean shouldFail)
			{
				this.shouldFail = shouldFail;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Eval(MockDirectoryWrapper dir)
			{
				StackTraceElement[] trace = new Exception().GetStackTrace();
				if (shouldFail.Get())
				{
					for (int i = 0; i < trace.Length; i++)
					{
						if ("getReadOnlyClone".Equals(trace[i].GetMethodName()))
						{
							if (LuceneTestCase.VERBOSE)
							{
								System.Console.Out.WriteLine("TEST: now fail; exc:");
								Sharpen.Runtime.PrintStackTrace(new Exception(), System.Console.Out);
							}
							shouldFail.Set(false);
							throw new MockDirectoryWrapper.FakeIOException();
						}
					}
				}
			}

			private readonly AtomicBoolean shouldFail;
		}

		/// <summary>
		/// Make sure if all we do is open NRT reader against
		/// writer, we don't see merge starvation.
		/// </summary>
		/// <remarks>
		/// Make sure if all we do is open NRT reader against
		/// writer, we don't see merge starvation.
		/// </remarks>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestTooManySegments()
		{
			Directory dir = GetAssertNoDeletesDirectory(NewDirectory());
			// Don't use newIndexWriterConfig, because we need a
			// "sane" mergePolicy:
			IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			IndexWriter w = new IndexWriter(dir, iwc);
			// Create 500 segments:
			for (int i = 0; i < 500; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(NewStringField("id", string.Empty + i, Field.Store.NO));
				w.AddDocument(doc);
				IndexReader r = DirectoryReader.Open(w, true);
				// Make sure segment count never exceeds 100:
				IsTrue(r.Leaves().Count < 100);
				r.Close();
			}
			w.Close();
			dir.Close();
		}
	}
}
