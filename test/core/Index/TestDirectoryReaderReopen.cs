/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.Text;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	public class TestDirectoryReaderReopen : LuceneTestCase
	{
		/// <exception cref="System.Exception"></exception>
		public virtual void TestReopen()
		{
			Directory dir1 = NewDirectory();
			CreateIndex(Random(), dir1, false);
			PerformDefaultTests(new _TestReopen_49(dir1));
			dir1.Close();
			Directory dir2 = NewDirectory();
			CreateIndex(Random(), dir2, true);
			PerformDefaultTests(new _TestReopen_67(dir2));
			dir2.Close();
		}

		private sealed class _TestReopen_49 : TestDirectoryReaderReopen.TestReopen
		{
			public _TestReopen_49(Directory dir1)
			{
				this.dir1 = dir1;
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected internal override void ModifyIndex(int i)
			{
				TestDirectoryReaderReopen.ModifyIndex(i, dir1);
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected internal override DirectoryReader OpenReader()
			{
				return DirectoryReader.Open(dir1);
			}

			private readonly Directory dir1;
		}

		private sealed class _TestReopen_67 : TestDirectoryReaderReopen.TestReopen
		{
			public _TestReopen_67(Directory dir2)
			{
				this.dir2 = dir2;
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected internal override void ModifyIndex(int i)
			{
				TestDirectoryReaderReopen.ModifyIndex(i, dir2);
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected internal override DirectoryReader OpenReader()
			{
				return DirectoryReader.Open(dir2);
			}

			private readonly Directory dir2;
		}

		// LUCENE-1228: IndexWriter.commit() does not update the index version
		// populate an index in iterations.
		// at the end of every iteration, commit the index and reopen/recreate the reader.
		// in each iteration verify the work of previous iteration. 
		// try this once with reopen once recreate, on both RAMDir and FSDir.
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestCommitReopen()
		{
			Directory dir = NewDirectory();
			DoTestReopenWithCommit(Random(), dir, true);
			dir.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestCommitRecreate()
		{
			Directory dir = NewDirectory();
			DoTestReopenWithCommit(Random(), dir, false);
			dir.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void DoTestReopenWithCommit(Random random, Directory dir, bool withReopen
			)
		{
			IndexWriter iwriter = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(random)).SetOpenMode(IndexWriterConfig.OpenMode.CREATE).SetMergeScheduler
				(new SerialMergeScheduler()).SetMergePolicy(NewLogMergePolicy()));
			iwriter.Commit();
			DirectoryReader reader = DirectoryReader.Open(dir);
			try
			{
				int M = 3;
				FieldType customType = new FieldType(TextField.TYPE_STORED);
				customType.SetTokenized(false);
				FieldType customType2 = new FieldType(TextField.TYPE_STORED);
				customType2.SetTokenized(false);
				customType2.SetOmitNorms(true);
				FieldType customType3 = new FieldType();
				customType3.SetStored(true);
				for (int i = 0; i < 4; i++)
				{
					for (int j = 0; j < M; j++)
					{
						Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
							();
						doc.Add(NewField("id", i + "_" + j, customType));
						doc.Add(NewField("id2", i + "_" + j, customType2));
						doc.Add(NewField("id3", i + "_" + j, customType3));
						iwriter.AddDocument(doc);
						if (i > 0)
						{
							int k = i - 1;
							int n = j + k * M;
							Lucene.Net.Documents.Document prevItereationDoc = reader.Document(n);
							IsNotNull(prevItereationDoc);
							string id = prevItereationDoc.Get("id");
							AreEqual(k + "_" + j, id);
						}
					}
					iwriter.Commit();
					if (withReopen)
					{
						// reopen
						DirectoryReader r2 = DirectoryReader.OpenIfChanged(reader);
						if (r2 != null)
						{
							reader.Close();
							reader = r2;
						}
					}
					else
					{
						// recreate
						reader.Close();
						reader = DirectoryReader.Open(dir);
					}
				}
			}
			finally
			{
				iwriter.Close();
				reader.Close();
			}
		}

		/// <exception cref="System.Exception"></exception>
		private void PerformDefaultTests(TestDirectoryReaderReopen.TestReopen test)
		{
			DirectoryReader index1 = test.OpenReader();
			DirectoryReader index2 = test.OpenReader();
			TestDirectoryReader.AssertIndexEquals(index1, index2);
			// verify that reopen() does not return a new reader instance
			// in case the index has no changes
			TestDirectoryReaderReopen.ReaderCouple couple = RefreshReader(index2, false);
			IsTrue(couple.refreshedReader == index2);
			couple = RefreshReader(index2, test, 0, true);
			index1.Close();
			index1 = couple.newReader;
			DirectoryReader index2_refreshed = couple.refreshedReader;
			index2.Close();
			// test if refreshed reader and newly opened reader return equal results
			TestDirectoryReader.AssertIndexEquals(index1, index2_refreshed);
			index2_refreshed.Close();
			AssertReaderClosed(index2, true);
			AssertReaderClosed(index2_refreshed, true);
			index2 = test.OpenReader();
			for (int i = 1; i < 4; i++)
			{
				index1.Close();
				couple = RefreshReader(index2, test, i, true);
				// refresh DirectoryReader
				index2.Close();
				index2 = couple.refreshedReader;
				index1 = couple.newReader;
				TestDirectoryReader.AssertIndexEquals(index1, index2);
			}
			index1.Close();
			index2.Close();
			AssertReaderClosed(index1, true);
			AssertReaderClosed(index2, true);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestThreadSafety()
		{
			Directory dir = NewDirectory();
			// NOTE: this also controls the number of threads!
			int n = TestUtil.NextInt(Random(), 20, 40);
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			for (int i = 0; i < n; i++)
			{
				writer.AddDocument(CreateDocument(i, 3));
			}
			writer.ForceMerge(1);
			writer.Close();
			TestDirectoryReaderReopen.TestReopen test = new _TestReopen_208(dir, n);
			IList<TestDirectoryReaderReopen.ReaderCouple> readers = Collections.SynchronizedList
				(new AList<TestDirectoryReaderReopen.ReaderCouple>());
			DirectoryReader firstReader = DirectoryReader.Open(dir);
			DirectoryReader reader = firstReader;
			TestDirectoryReaderReopen.ReaderThread[] threads = new TestDirectoryReaderReopen.ReaderThread
				[n];
			ICollection<DirectoryReader> readersToClose = Sharpen.Collections.SynchronizedSet
				(new HashSet<DirectoryReader>());
			for (int i_1 = 0; i_1 < n; i_1++)
			{
				if (i_1 % 2 == 0)
				{
					DirectoryReader refreshed = DirectoryReader.OpenIfChanged(reader);
					if (refreshed != null)
					{
						readersToClose.AddItem(reader);
						reader = refreshed;
					}
				}
				DirectoryReader r = reader;
				int index = i_1;
				TestDirectoryReaderReopen.ReaderThreadTask task;
				if (i_1 < 4 || (i_1 >= 10 && i_1 < 14) || i_1 > 18)
				{
					task = new _ReaderThreadTask_245(this, index, r, test, readersToClose, readers);
				}
				else
				{
					// refresh reader synchronized
					// prevent too many readers
					// not synchronized
					task = new _ReaderThreadTask_285(readers);
				}
				threads[i_1] = new TestDirectoryReaderReopen.ReaderThread(task);
				threads[i_1].Start();
			}
			lock (this)
			{
				Sharpen.Runtime.Wait(this, 1000);
			}
			for (int i_2 = 0; i_2 < n; i_2++)
			{
				if (threads[i_2] != null)
				{
					threads[i_2].StopThread();
				}
			}
			for (int i_3 = 0; i_3 < n; i_3++)
			{
				if (threads[i_3] != null)
				{
					threads[i_3].Join();
					if (threads[i_3].error != null)
					{
						string msg = "Error occurred in thread " + threads[i_3].GetName() + ":\n" + threads
							[i_3].error.Message;
						Fail(msg);
					}
				}
			}
			foreach (DirectoryReader readerToClose in readersToClose)
			{
				readerToClose.Close();
			}
			firstReader.Close();
			reader.Close();
			foreach (DirectoryReader readerToClose_1 in readersToClose)
			{
				AssertReaderClosed(readerToClose_1, true);
			}
			AssertReaderClosed(reader, true);
			AssertReaderClosed(firstReader, true);
			dir.Close();
		}

		private sealed class _TestReopen_208 : TestDirectoryReaderReopen.TestReopen
		{
			public _TestReopen_208(Directory dir, int n)
			{
				this.dir = dir;
				this.n = n;
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected internal override void ModifyIndex(int i)
			{
				IndexWriter modifier = new IndexWriter(dir, new IndexWriterConfig(LuceneTestCase.
					TEST_VERSION_CURRENT, new MockAnalyzer(LuceneTestCase.Random())));
				modifier.AddDocument(TestDirectoryReaderReopen.CreateDocument(n + i, 6));
				modifier.Close();
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected internal override DirectoryReader OpenReader()
			{
				return DirectoryReader.Open(dir);
			}

			private readonly Directory dir;

			private readonly int n;
		}

		private sealed class _ReaderThreadTask_245 : TestDirectoryReaderReopen.ReaderThreadTask
		{
			public _ReaderThreadTask_245(TestDirectoryReaderReopen _enclosing, int index, DirectoryReader
				 r, TestDirectoryReaderReopen.TestReopen test, ICollection<DirectoryReader> readersToClose
				, IList<TestDirectoryReaderReopen.ReaderCouple> readers)
			{
				this._enclosing = _enclosing;
				this.index = index;
				this.r = r;
				this.test = test;
				this.readersToClose = readersToClose;
				this.readers = readers;
			}

			/// <exception cref="System.Exception"></exception>
			public override void Run()
			{
				Random rnd = LuceneTestCase.Random();
				while (!this.stopped)
				{
					if (index % 2 == 0)
					{
						TestDirectoryReaderReopen.ReaderCouple c = (this._enclosing.RefreshReader(r, test
							, index, true));
						readersToClose.AddItem(c.newReader);
						readersToClose.AddItem(c.refreshedReader);
						readers.AddItem(c);
						break;
					}
					else
					{
						DirectoryReader refreshed = DirectoryReader.OpenIfChanged(r);
						if (refreshed == null)
						{
							refreshed = r;
						}
						IndexSearcher searcher = LuceneTestCase.NewSearcher(refreshed);
						ScoreDoc[] hits = searcher.Search(new TermQuery(new Term("field1", "a" + rnd.Next
							(refreshed.MaxDoc))), null, 1000).scoreDocs;
						if (hits.Length > 0)
						{
							searcher.Doc(hits[0].doc);
						}
						if (refreshed != r)
						{
							refreshed.Close();
						}
					}
					lock (this)
					{
						Sharpen.Runtime.Wait(this, TestUtil.NextInt(LuceneTestCase.Random(), 1, 100));
					}
				}
			}

			private readonly TestDirectoryReaderReopen _enclosing;

			private readonly int index;

			private readonly DirectoryReader r;

			private readonly TestDirectoryReaderReopen.TestReopen test;

			private readonly ICollection<DirectoryReader> readersToClose;

			private readonly IList<TestDirectoryReaderReopen.ReaderCouple> readers;
		}

		private sealed class _ReaderThreadTask_285 : TestDirectoryReaderReopen.ReaderThreadTask
		{
			public _ReaderThreadTask_285(IList<TestDirectoryReaderReopen.ReaderCouple> readers
				)
			{
				this.readers = readers;
			}

			/// <exception cref="System.Exception"></exception>
			public override void Run()
			{
				Random rnd = LuceneTestCase.Random();
				while (!this.stopped)
				{
					int numReaders = readers.Count;
					if (numReaders > 0)
					{
						TestDirectoryReaderReopen.ReaderCouple c = readers[rnd.Next(numReaders)];
						TestDirectoryReader.AssertIndexEquals(c.newReader, c.refreshedReader);
					}
					lock (this)
					{
						Sharpen.Runtime.Wait(this, TestUtil.NextInt(LuceneTestCase.Random(), 1, 100));
					}
				}
			}

			private readonly IList<TestDirectoryReaderReopen.ReaderCouple> readers;
		}

		private class ReaderCouple
		{
			internal ReaderCouple(DirectoryReader r1, DirectoryReader r2)
			{
				newReader = r1;
				refreshedReader = r2;
			}

			internal DirectoryReader newReader;

			internal DirectoryReader refreshedReader;
		}

		internal abstract class ReaderThreadTask
		{
			protected internal volatile bool stopped;

			public virtual void Stop()
			{
				this.stopped = true;
			}

			/// <exception cref="System.Exception"></exception>
			public abstract void Run();
		}

		private class ReaderThread : Sharpen.Thread
		{
			internal TestDirectoryReaderReopen.ReaderThreadTask task;

			internal Exception error;

			internal ReaderThread(TestDirectoryReaderReopen.ReaderThreadTask task)
			{
				this.task = task;
			}

			public virtual void StopThread()
			{
				this.task.Stop();
			}

			public override void Run()
			{
				try
				{
					this.task.Run();
				}
				catch (Exception r)
				{
					Sharpen.Runtime.PrintStackTrace(r, System.Console.Out);
					this.error = r;
				}
			}
		}

		private object createReaderMutex = new object();

		/// <exception cref="System.IO.IOException"></exception>
		private TestDirectoryReaderReopen.ReaderCouple RefreshReader(DirectoryReader reader
			, bool hasChanges)
		{
			return RefreshReader(reader, null, -1, hasChanges);
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual TestDirectoryReaderReopen.ReaderCouple RefreshReader(DirectoryReader
			 reader, TestDirectoryReaderReopen.TestReopen test, int modify, bool hasChanges)
		{
			lock (createReaderMutex)
			{
				DirectoryReader r = null;
				if (test != null)
				{
					test.ModifyIndex(modify);
					r = test.OpenReader();
				}
				DirectoryReader refreshed = null;
				try
				{
					refreshed = DirectoryReader.OpenIfChanged(reader);
					if (refreshed == null)
					{
						refreshed = reader;
					}
				}
				finally
				{
					if (refreshed == null && r != null)
					{
						// Hit exception -- close opened reader
						r.Close();
					}
				}
				if (hasChanges)
				{
					if (refreshed == reader)
					{
						Fail("No new DirectoryReader instance created during refresh."
							);
					}
				}
				else
				{
					if (refreshed != reader)
					{
						Fail("New DirectoryReader instance created during refresh even though index had no changes."
							);
					}
				}
				return new TestDirectoryReaderReopen.ReaderCouple(r, refreshed);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static void CreateIndex(Random random, Directory dir, bool multiSegment)
		{
			IndexWriter.Unlock(dir);
			IndexWriter w = new IndexWriter(dir, LuceneTestCase.NewIndexWriterConfig(random, 
				TEST_VERSION_CURRENT, new MockAnalyzer(random)).SetMergePolicy(new LogDocMergePolicy
				()));
			for (int i = 0; i < 100; i++)
			{
				w.AddDocument(CreateDocument(i, 4));
				if (multiSegment && (i % 10) == 0)
				{
					w.Commit();
				}
			}
			if (!multiSegment)
			{
				w.ForceMerge(1);
			}
			w.Close();
			DirectoryReader r = DirectoryReader.Open(dir);
			if (multiSegment)
			{
				IsTrue(r.Leaves().Count > 1);
			}
			else
			{
				IsTrue(r.Leaves().Count == 1);
			}
			r.Close();
		}

		public static Lucene.Net.Documents.Document CreateDocument(int n, int numFields
			)
		{
			StringBuilder sb = new StringBuilder();
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			sb.Append("a");
			sb.Append(n);
			FieldType customType2 = new FieldType(TextField.TYPE_STORED);
			customType2.SetTokenized(false);
			customType2.SetOmitNorms(true);
			FieldType customType3 = new FieldType();
			customType3.SetStored(true);
			doc.Add(new TextField("field1", sb.ToString(), Field.Store.YES));
			doc.Add(new Field("fielda", sb.ToString(), customType2));
			doc.Add(new Field("fieldb", sb.ToString(), customType3));
			sb.Append(" b");
			sb.Append(n);
			for (int i = 1; i < numFields; i++)
			{
				doc.Add(new TextField("field" + (i + 1), sb.ToString(), Field.Store.YES));
			}
			return doc;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal static void ModifyIndex(int i, Directory dir)
		{
			switch (i)
			{
				case 0:
				{
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("TEST: modify index");
					}
					IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, 
						new MockAnalyzer(Random())));
					w.DeleteDocuments(new Term("field2", "a11"));
					w.DeleteDocuments(new Term("field2", "b30"));
					w.Close();
					break;
				}

				case 1:
				{
					IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, 
						new MockAnalyzer(Random())));
					w.ForceMerge(1);
					w.Close();
					break;
				}

				case 2:
				{
					IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, 
						new MockAnalyzer(Random())));
					w.AddDocument(CreateDocument(101, 4));
					w.ForceMerge(1);
					w.AddDocument(CreateDocument(102, 4));
					w.AddDocument(CreateDocument(103, 4));
					w.Close();
					break;
				}

				case 3:
				{
					IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, 
						new MockAnalyzer(Random())));
					w.AddDocument(CreateDocument(101, 4));
					w.Close();
					break;
				}
			}
		}

		internal static void AssertReaderClosed(IndexReader reader, bool checkSubReaders)
		{
			AreEqual(0, reader.GetRefCount());
			if (checkSubReaders && reader is CompositeReader)
			{
				// we cannot use reader context here, as reader is
				// already closed and calling getTopReaderContext() throws AlreadyClosed!
				IList<IndexReader> subReaders = ((CompositeReader)reader).GetSequentialSubReaders
					();
				foreach (IndexReader r in subReaders)
				{
					AssertReaderClosed(r, checkSubReaders);
				}
			}
		}

		internal abstract class TestReopen
		{
			/// <exception cref="System.IO.IOException"></exception>
			protected internal abstract DirectoryReader OpenReader();

			/// <exception cref="System.IO.IOException"></exception>
			protected internal abstract void ModifyIndex(int i);
		}

		internal class KeepAllCommits : IndexDeletionPolicy
		{
			public override void OnInit<_T0>(IList<_T0> commits)
			{
			}

			public override void OnCommit<_T0>(IList<_T0> commits)
			{
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestReopenOnCommit()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetIndexDeletionPolicy(new TestDirectoryReaderReopen.KeepAllCommits
				()).SetMaxBufferedDocs(-1)).SetMergePolicy(NewLogMergePolicy(10)));
			for (int i = 0; i < 4; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(NewStringField("id", string.Empty + i, Field.Store.NO));
				writer.AddDocument(doc);
				IDictionary<string, string> data = new Dictionary<string, string>();
				data.Put("index", i + string.Empty);
				writer.SetCommitData(data);
				writer.Commit();
			}
			for (int i_1 = 0; i_1 < 4; i_1++)
			{
				writer.DeleteDocuments(new Term("id", string.Empty + i_1));
				IDictionary<string, string> data = new Dictionary<string, string>();
				data.Put("index", (4 + i_1) + string.Empty);
				writer.SetCommitData(data);
				writer.Commit();
			}
			writer.Close();
			DirectoryReader r = DirectoryReader.Open(dir);
			AreEqual(0, r.NumDocs());
			ICollection<IndexCommit> commits = DirectoryReader.ListCommits(dir);
			foreach (IndexCommit commit in commits)
			{
				DirectoryReader r2 = DirectoryReader.OpenIfChanged(r, commit);
				IsNotNull(r2);
				IsTrue(r2 != r);
				IDictionary<string, string> s = commit.GetUserData();
				int v;
				if (s.Count == 0)
				{
					// First commit created by IW
					v = -1;
				}
				else
				{
					v = System.Convert.ToInt32(s.Get("index"));
				}
				if (v < 4)
				{
					AreEqual(1 + v, r2.NumDocs());
				}
				else
				{
					AreEqual(7 - v, r2.NumDocs());
				}
				r.Close();
				r = r2;
			}
			r.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestOpenIfChangedNRTToCommit()
		{
			Directory dir = NewDirectory();
			// Can't use RIW because it randomly commits:
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewStringField("field", "value", Field.Store.NO));
			w.AddDocument(doc);
			w.Commit();
			IList<IndexCommit> commits = DirectoryReader.ListCommits(dir);
			AreEqual(1, commits.Count);
			w.AddDocument(doc);
			DirectoryReader r = DirectoryReader.Open(w, true);
			AreEqual(2, r.NumDocs());
			IndexReader r2 = DirectoryReader.OpenIfChanged(r, commits[0]);
			IsNotNull(r2);
			r.Close();
			AreEqual(1, r2.NumDocs());
			w.Close();
			r2.Close();
			dir.Close();
		}
	}
}
