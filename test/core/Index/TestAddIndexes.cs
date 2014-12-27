using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Lucene.Net.Analysis;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Lucene46;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.TestFramework.Index;
using Lucene.Net.TestFramework.Util;
using Lucene.Net.Util;
using NUnit.Framework;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Test.Index
{
    [TestFixture]
	public class TestAddIndexes : LuceneTestCase
	{
		[Test]
		public virtual void TestSimpleCase()
		{
			// main directory
			Directory dir = NewDirectory();
			// two auxiliary directories
			Directory aux = NewDirectory();
			Directory aux2 = NewDirectory();
			IndexWriter writer = null;
			writer = NewWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetOpenMode(IndexWriterConfig.OpenMode.CREATE));
			// add 100 documents
			AddDocs(writer, 100);
			AreEqual(100, writer.MaxDoc);
			writer.Dispose();
			TestUtil.CheckIndex(dir);
			writer = NewWriter(aux, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetOpenMode(IndexWriterConfig.OpenMode.CREATE).SetMergePolicy(NewLogMergePolicy
				(false)));
			// add 40 documents in separate files
			AddDocs(writer, 40);
			AreEqual(40, writer.MaxDoc);
			writer.Dispose();
			writer = NewWriter(aux2, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetOpenMode(IndexWriterConfig.OpenMode.CREATE));
			// add 50 documents in compound files
			AddDocs2(writer, 50);
			AreEqual(50, writer.MaxDoc);
			writer.Dispose();
			// test doc count before segments are merged
			writer = NewWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND));
			AreEqual(100, writer.MaxDoc);
			writer.AddIndexes(aux, aux2);
			AreEqual(190, writer.MaxDoc);
			writer.Dispose();
			TestUtil.CheckIndex(dir);
			// make sure the old index is correct
			VerifyNumDocs(aux, 40);
			// make sure the new index is correct
			VerifyNumDocs(dir, 190);
			// now add another set in.
			Directory aux3 = NewDirectory();
			writer = NewWriter(aux3, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())));
			// add 40 documents
			AddDocs(writer, 40);
			AreEqual(40, writer.MaxDoc);
			writer.Dispose();
			// test doc count before segments are merged
			writer = NewWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND));
			AreEqual(190, writer.MaxDoc);
			writer.AddIndexes(aux3);
			AreEqual(230, writer.MaxDoc);
			writer.Dispose();
			// make sure the new index is correct
			VerifyNumDocs(dir, 230);
			VerifyTermDocs(dir, new Term("content", "aaa"), 180);
			VerifyTermDocs(dir, new Term("content", "bbb"), 50);
			// now fully merge it.
			writer = NewWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND));
			writer.ForceMerge(1);
			writer.Dispose();
			// make sure the new index is correct
			VerifyNumDocs(dir, 230);
			VerifyTermDocs(dir, new Term("content", "aaa"), 180);
			VerifyTermDocs(dir, new Term("content", "bbb"), 50);
			// now add a single document
			Directory aux4 = NewDirectory();
			writer = NewWriter(aux4, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())));
			AddDocs2(writer, 1);
			writer.Dispose();
			writer = NewWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND));
			AreEqual(230, writer.MaxDoc);
			writer.AddIndexes(aux4);
			AreEqual(231, writer.MaxDoc);
			writer.Dispose();
			VerifyNumDocs(dir, 231);
			VerifyTermDocs(dir, new Term("content", "bbb"), 51);
			dir.Dispose();
			aux.Dispose();
			aux2.Dispose();
			aux3.Dispose();
			aux4.Dispose();
		}

		[Test]
		public virtual void TestWithPendingDeletes()
		{
			// main directory
			Directory dir = NewDirectory();
			// auxiliary directory
			Directory aux = NewDirectory();
			SetUpDirs(dir, aux);
			IndexWriter writer = NewWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND));
			writer.AddIndexes(aux);
			// Adds 10 docs, then replaces them with another 10
			// docs, so 10 pending deletes:
			for (int i = 0; i < 20; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(NewStringField("id", string.Empty + (i % 10), Field.Store.NO));
				doc.Add(NewTextField("content", "bbb " + i, Field.Store.NO));
				writer.UpdateDocument(new Term("id", string.Empty + (i % 10)), doc);
			}
			// Deletes one of the 10 added docs, leaving 9:
			PhraseQuery q = new PhraseQuery();
			q.Add(new Term("content", "bbb"));
			q.Add(new Term("content", "14"));
			writer.DeleteDocuments(q);
			writer.ForceMerge(1);
			writer.Commit();
			VerifyNumDocs(dir, 1039);
			VerifyTermDocs(dir, new Term("content", "aaa"), 1030);
			VerifyTermDocs(dir, new Term("content", "bbb"), 9);
			writer.Dispose();
			dir.Dispose();
			aux.Dispose();
		}

		[Test]
		public virtual void TestWithPendingDeletes2()
		{
			// main directory
			Directory dir = NewDirectory();
			// auxiliary directory
			Directory aux = NewDirectory();
			SetUpDirs(dir, aux);
			IndexWriter writer = NewWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND));
			// Adds 10 docs, then replaces them with another 10
			// docs, so 10 pending deletes:
			for (int i = 0; i < 20; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(NewStringField("id", string.Empty + (i % 10), Field.Store.NO));
				doc.Add(NewTextField("content", "bbb " + i, Field.Store.NO));
				writer.UpdateDocument(new Term("id", string.Empty + (i % 10)), doc);
			}
			writer.AddIndexes(aux);
			// Deletes one of the 10 added docs, leaving 9:
			PhraseQuery q = new PhraseQuery();
			q.Add(new Term("content", "bbb"));
			q.Add(new Term("content", "14"));
			writer.DeleteDocuments(q);
			writer.ForceMerge(1);
			writer.Commit();
			VerifyNumDocs(dir, 1039);
			VerifyTermDocs(dir, new Term("content", "aaa"), 1030);
			VerifyTermDocs(dir, new Term("content", "bbb"), 9);
			writer.Dispose();
			dir.Dispose();
			aux.Dispose();
		}

		[Test]
		public virtual void TestWithPendingDeletes3()
		{
			// main directory
			Directory dir = NewDirectory();
			// auxiliary directory
			Directory aux = NewDirectory();
			SetUpDirs(dir, aux);
			IndexWriter writer = NewWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND));
			// Adds 10 docs, then replaces them with another 10
			// docs, so 10 pending deletes:
			for (int i = 0; i < 20; i++)
			{
				var doc = new Lucene.Net.Documents.Document
				{
				    NewStringField("id", string.Empty + (i%10), Field.Store.NO),
				    NewTextField("content", "bbb " + i, Field.Store.NO)
				};
			    writer.UpdateDocument(new Term("id", string.Empty + (i % 10)), doc);
			}
			// Deletes one of the 10 added docs, leaving 9:
			PhraseQuery q = new PhraseQuery();
			q.Add(new Term("content", "bbb"));
			q.Add(new Term("content", "14"));
			writer.DeleteDocuments(q);
			writer.AddIndexes(aux);
			writer.ForceMerge(1);
			writer.Commit();
			VerifyNumDocs(dir, 1039);
			VerifyTermDocs(dir, new Term("content", "aaa"), 1030);
			VerifyTermDocs(dir, new Term("content", "bbb"), 9);
			writer.Dispose();
			dir.Dispose();
			aux.Dispose();
		}

		// case 0: add self or exceed maxMergeDocs, expect exception
		[Test]
		public virtual void TestAddSelf()
		{
			// main directory
			Directory dir = NewDirectory();
			// auxiliary directory
			Directory aux = NewDirectory();
			IndexWriter writer = null;
			writer = NewWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())));
			// add 100 documents
			AddDocs(writer, 100);
			AreEqual(100, writer.MaxDoc);
			writer.Dispose();
			writer = NewWriter(aux, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode.CREATE).SetMaxBufferedDocs
				(1000)).SetMergePolicy(NewLogMergePolicy(false)));
			// add 140 documents in separate files
			AddDocs(writer, 40);
			writer.Dispose();
			writer = NewWriter(aux, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode.CREATE).SetMaxBufferedDocs
				(1000)).SetMergePolicy(NewLogMergePolicy(false)));
			AddDocs(writer, 100);
			writer.Dispose();
			writer = NewWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND));
			try
			{
				// cannot add self
				writer.AddIndexes(aux, dir);
				IsTrue(false);
			}
			catch (ArgumentException)
			{
				AreEqual(100, writer.MaxDoc);
			}
			writer.Dispose();
			// make sure the index is correct
			VerifyNumDocs(dir, 100);
			dir.Dispose();
			aux.Dispose();
		}

		// in all the remaining tests, make the doc count of the oldest segment
		// in dir large so that it is never merged in addIndexes()
		// case 1: no tail segments
		[Test]
		public virtual void TestNoTailSegments()
		{
			// main directory
			Directory dir = NewDirectory();
			// auxiliary directory
			Directory aux = NewDirectory();
			SetUpDirs(dir, aux);
			IndexWriter writer = NewWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND).SetMaxBufferedDocs
				(10)).SetMergePolicy(NewLogMergePolicy(4)));
			AddDocs(writer, 10);
			writer.AddIndexes(aux);
			AreEqual(1040, writer.MaxDoc);
			AreEqual(1000, writer.GetDocCount(0));
			writer.Dispose();
			// make sure the index is correct
			VerifyNumDocs(dir, 1040);
			dir.Dispose();
			aux.Dispose();
		}

		// case 2: tail segments, invariants hold, no copy
		[Test]
		public virtual void TestNoCopySegments()
		{
			// main directory
			Directory dir = NewDirectory();
			// auxiliary directory
			Directory aux = NewDirectory();
			SetUpDirs(dir, aux);
			IndexWriter writer = NewWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND).SetMaxBufferedDocs
				(9)).SetMergePolicy(NewLogMergePolicy(4)));
			AddDocs(writer, 2);
			writer.AddIndexes(aux);
			AreEqual(1032, writer.MaxDoc);
			AreEqual(1000, writer.GetDocCount(0));
			writer.Dispose();
			// make sure the index is correct
			VerifyNumDocs(dir, 1032);
			dir.Dispose();
			aux.Dispose();
		}

		// case 3: tail segments, invariants hold, copy, invariants hold
		[Test]
		public virtual void TestNoMergeAfterCopy()
		{
			// main directory
			Directory dir = NewDirectory();
			// auxiliary directory
			Directory aux = NewDirectory();
			SetUpDirs(dir, aux);
			IndexWriter writer = NewWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND).SetMaxBufferedDocs
				(10)).SetMergePolicy(NewLogMergePolicy(4)));
			writer.AddIndexes(aux, new MockDirectoryWrapper(Random(), new RAMDirectory(aux, NewIOContext
				(Random()))));
			AreEqual(1060, writer.MaxDoc);
			AreEqual(1000, writer.GetDocCount(0));
			writer.Dispose();
			// make sure the index is correct
			VerifyNumDocs(dir, 1060);
			dir.Dispose();
			aux.Dispose();
		}

		// case 4: tail segments, invariants hold, copy, invariants not hold
		[Test]
		public virtual void TestMergeAfterCopy()
		{
			// main directory
			Directory dir = NewDirectory();
			// auxiliary directory
			Directory aux = NewDirectory();
			SetUpDirs(dir, aux, true);
			IndexWriterConfig dontMergeConfig = new IndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())).SetMergePolicy(NoMergePolicy.COMPOUND_FILES);
			IndexWriter writer = new IndexWriter(aux, dontMergeConfig);
			for (int i = 0; i < 20; i++)
			{
				writer.DeleteDocuments(new Term("id", string.Empty + i));
			}
			writer.Dispose();
			IndexReader reader = DirectoryReader.Open(aux);
			AreEqual(10, reader.NumDocs);
			reader.Dispose();
			writer = NewWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND).SetMaxBufferedDocs
				(4)).SetMergePolicy(NewLogMergePolicy(4)));
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("\nTEST: now addIndexes");
			}
			writer.AddIndexes(aux, new MockDirectoryWrapper(Random(), new RAMDirectory(aux, NewIOContext
				(Random()))));
			AreEqual(1020, writer.MaxDoc);
			AreEqual(1000, writer.GetDocCount(0));
			writer.Dispose();
			dir.Dispose();
			aux.Dispose();
		}

		// case 5: tail segments, invariants not hold
		[Test]
		public virtual void TestMoreMerges()
		{
			// main directory
			Directory dir = NewDirectory();
			// auxiliary directory
			Directory aux = NewDirectory();
			Directory aux2 = NewDirectory();
			SetUpDirs(dir, aux, true);
			IndexWriter writer = NewWriter(aux2, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode.CREATE).SetMaxBufferedDocs
				(100)).SetMergePolicy(NewLogMergePolicy(10)));
			writer.AddIndexes(aux);
			AreEqual(30, writer.MaxDoc);
			AreEqual(3, writer.SegmentCount);
			writer.Dispose();
			IndexWriterConfig dontMergeConfig = new IndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())).SetMergePolicy(NoMergePolicy.COMPOUND_FILES);
			writer = new IndexWriter(aux, dontMergeConfig);
			for (int i = 0; i < 27; i++)
			{
				writer.DeleteDocuments(new Term("id", string.Empty + i));
			}
			writer.Dispose();
			IndexReader reader = DirectoryReader.Open(aux);
			AreEqual(3, reader.NumDocs);
			reader.Dispose();
			dontMergeConfig = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random
				())).SetMergePolicy(NoMergePolicy.COMPOUND_FILES);
			writer = new IndexWriter(aux2, dontMergeConfig);
			for (int i_1 = 0; i_1 < 8; i_1++)
			{
				writer.DeleteDocuments(new Term("id", string.Empty + i_1));
			}
			writer.Dispose();
			reader = DirectoryReader.Open(aux2);
			AreEqual(22, reader.NumDocs);
			reader.Dispose();
			writer = NewWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND).SetMaxBufferedDocs
				(6)).SetMergePolicy(NewLogMergePolicy(4)));
			writer.AddIndexes(aux, aux2);
			AreEqual(1040, writer.MaxDoc);
			AreEqual(1000, writer.GetDocCount(0));
			writer.Dispose();
			dir.Dispose();
			aux.Dispose();
			aux2.Dispose();
		}

		
		private IndexWriter NewWriter(Directory dir, IndexWriterConfig conf)
		{
			conf.SetMergePolicy(new LogDocMergePolicy());
			IndexWriter writer = new IndexWriter(dir, conf);
			return writer;
		}

		
		private void AddDocs(IndexWriter writer, int numDocs)
		{
			for (int i = 0; i < numDocs; i++)
			{
				var doc = new Lucene.Net.Documents.Document();
				doc.Add(NewTextField("content", "aaa", Field.Store.NO));
				writer.AddDocument(doc);
			}
		}

		
		private void AddDocs2(IndexWriter writer, int numDocs)
		{
			for (int i = 0; i < numDocs; i++)
			{
				var doc = new Lucene.Net.Documents.Document {NewTextField("content", "bbb", Field.Store.NO)};
			    writer.AddDocument(doc);
			}
		}

		
		private void VerifyNumDocs(Directory dir, int numDocs)
		{
			IndexReader reader = DirectoryReader.Open(dir);
			AreEqual(numDocs, reader.MaxDoc);
			AreEqual(numDocs, reader.NumDocs);
			reader.Dispose();
		}

		
		private void VerifyTermDocs(Directory dir, Term term, int numDocs)
		{
			IndexReader reader = DirectoryReader.Open(dir);
			DocsEnum docsEnum = TestUtil.Docs(Random(), reader, term.Field, term.bytes, null, 
				null, DocsEnum.FLAG_NONE);
			int count = 0;
			while (docsEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
			{
				count++;
			}
			AreEqual(numDocs, count);
			reader.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void SetUpDirs(Directory dir, Directory aux)
		{
			SetUpDirs(dir, aux, false);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void SetUpDirs(Directory dir, Directory aux, bool withID)
		{
			IndexWriter writer = null;
			writer = NewWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode.CREATE).SetMaxBufferedDocs
				(1000)));
			// add 1000 documents in 1 segment
			if (withID)
			{
				AddDocsWithID(writer, 1000, 0);
			}
			else
			{
				AddDocs(writer, 1000);
			}
			AreEqual(1000, writer.MaxDoc);
			AreEqual(1, writer.SegmentCount);
			writer.Dispose();
			writer = NewWriter(aux, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode.CREATE).SetMaxBufferedDocs
				(1000)).SetMergePolicy(NewLogMergePolicy(false, 10)));
			// add 30 documents in 3 segments
			for (int i = 0; i < 3; i++)
			{
				if (withID)
				{
					AddDocsWithID(writer, 10, 10 * i);
				}
				else
				{
					AddDocs(writer, 10);
				}
				writer.Dispose();
				writer = NewWriter(aux, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
					, new MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND).SetMaxBufferedDocs
					(1000)).SetMergePolicy(NewLogMergePolicy(false, 10)));
			}
			AreEqual(30, writer.MaxDoc);
			AreEqual(3, writer.SegmentCount);
			writer.Dispose();
		}

		// LUCENE-1270
		[Test]
		public virtual void TestHangOnClose()
		{
			Directory dir = NewDirectory();
			LogByteSizeMergePolicy lmp = new LogByteSizeMergePolicy();
			lmp.SetNoCFSRatio(0.0);
			lmp.MergeFactor = (100);
			IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(5)).SetMergePolicy
				(lmp));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			FieldType customType = new FieldType(TextField.TYPE_STORED);
			customType.StoreTermVectors = true;
			customType.StoreTermVectorPositions = true;
			customType.StoreTermVectorOffsets = true;
			doc.Add(NewField("content", "aaa bbb ccc ddd eee fff ggg hhh iii", customType));
			for (int i = 0; i < 60; i++)
			{
				writer.AddDocument(doc);
			}
			Lucene.Net.Documents.Document doc2 = new Lucene.Net.Documents.Document
				();
			FieldType customType2 = new FieldType();
			customType2.Stored = (true);
			doc2.Add(NewField("content", "aaa bbb ccc ddd eee fff ggg hhh iii", customType2));
			doc2.Add(NewField("content", "aaa bbb ccc ddd eee fff ggg hhh iii", customType2));
			doc2.Add(NewField("content", "aaa bbb ccc ddd eee fff ggg hhh iii", customType2));
			doc2.Add(NewField("content", "aaa bbb ccc ddd eee fff ggg hhh iii", customType2));
			for (int i_1 = 0; i_1 < 10; i_1++)
			{
				writer.AddDocument(doc2);
			}
			writer.Dispose();
			Directory dir2 = NewDirectory();
			lmp = new LogByteSizeMergePolicy();
			lmp.MinMergeMB = (0.0001);
			lmp.SetNoCFSRatio(0.0);
			lmp.MergeFactor = (4);
			writer = new IndexWriter(dir2, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetMergeScheduler(new SerialMergeScheduler()).SetMergePolicy(lmp));
			writer.AddIndexes(dir);
			writer.Dispose();
			dir.Dispose();
			dir2.Dispose();
		}

		// TODO: these are also in TestIndexWriter... add a simple doc-writing method
		// like this to LuceneTestCase?
		/// <exception cref="System.IO.IOException"></exception>
		private void AddDoc(IndexWriter writer)
		{
			var doc = new Lucene.Net.Documents.Document {NewTextField("content", "aaa", Field.Store.NO)};
		    writer.AddDocument(doc);
		}

		private abstract class RunAddIndexesThreads
		{
			internal Directory dir;

			internal Directory dir2;

			internal const int NUM_INIT_DOCS = 17;

			internal IndexWriter writer2;

			internal readonly IList<Exception> failures = new List<Exception>();

			internal volatile bool didClose;

			internal readonly IndexReader[] readers;

			internal readonly int NUM_COPY;

			internal const int NUM_THREADS = 5;

			internal readonly Thread[] threads = new Thread[NUM_THREADS];

			/// <exception cref="System.Exception"></exception>
			public RunAddIndexesThreads(TestAddIndexes enclosing, int numCopy)
			{
				this.enclosingInstance = enclosing;
				this.NUM_COPY = numCopy;
				this.dir = new MockDirectoryWrapper(LuceneTestCase.Random(), new RAMDirectory());
				IndexWriter writer = new IndexWriter(this.dir, ((IndexWriterConfig)new IndexWriterConfig
					(LuceneTestCase.TEST_VERSION_CURRENT, new MockAnalyzer(LuceneTestCase.Random()))
					.SetMaxBufferedDocs(2)));
				for (int i = 0; i < NUM_INIT_DOCS; i++)
				{
					this.enclosingInstance.AddDoc(writer);
				}
				writer.Dispose();
				this.dir2 = LuceneTestCase.NewDirectory();
				this.writer2 = new IndexWriter(this.dir2, new IndexWriterConfig(LuceneTestCase.TEST_VERSION_CURRENT
					, new MockAnalyzer(LuceneTestCase.Random())));
				this.writer2.Commit();
				this.readers = new IndexReader[this.NUM_COPY];
				for (int i_1 = 0; i_1 < this.NUM_COPY; i_1++)
				{
					this.readers[i_1] = DirectoryReader.Open(this.dir);
				}
			}

			internal virtual void LaunchThreads(int numIter)
			{
				for (int i = 0; i < NUM_THREADS; i++)
				{
					this.threads[i] = new Thread(() =>
					{
					    try
					{
						Directory[] dirs = new Directory[NUM_COPY];
						for (int k = 0; k < NUM_COPY; k++)
						{
							dirs[k] = new MockDirectoryWrapper(Random(), new RAMDirectory(dir, NewIOContext(Random())));
						}
						int j = 0;
						while (true)
						{
							if (numIter > 0 && j == numIter)
							{
								break;
							}
							DoBody(j++, dirs);
						}
					}
					catch (Exception t)
					{
						Handle(t);
					}
					});
				}
				// System.out.println(Thread.currentThread().getName() + ": iter j=" + j);
				for (int i = 0; i < NUM_THREADS; i++)
				{
					this.threads[i].Start();
				}
			}

		    /// <exception cref="System.Exception"></exception>
			internal virtual void JoinThreads()
			{
				for (int i = 0; i < NUM_THREADS; i++)
				{
					this.threads[i].Join();
				}
			}

			/// <exception cref="System.Exception"></exception>
			internal virtual void Close(bool doWait)
			{
				this.didClose = true;
				this.writer2.Dispose(doWait);
			}

			/// <exception cref="System.Exception"></exception>
			internal virtual void CloseDir()
			{
				for (int i = 0; i < this.NUM_COPY; i++)
				{
					this.readers[i].Dispose();
				}
				this.dir2.Dispose();
			}

			/// <exception cref="System.Exception"></exception>
			internal abstract void DoBody(int j, Directory[] dirs);

			internal abstract void Handle(Exception t);

			private readonly TestAddIndexes enclosingInstance;
		}

		private class CommitAndAddIndexes : RunAddIndexesThreads
        {
			/// <exception cref="System.Exception"></exception>
			public CommitAndAddIndexes(TestAddIndexes _enclosing, int numCopy) : base(_enclosing,numCopy)
			{
				this._enclosing = _enclosing;
			}

			internal override void Handle(Exception t)
			{
				Console.Out.WriteLine(t.StackTrace);
				lock (this.failures)
				{
					this.failures.Add(t);
				}
			}

			/// <exception cref="System.Exception"></exception>
			internal override void DoBody(int j, Directory[] dirs)
			{
				switch (j % 5)
				{
					case 0:
					{
						if (VERBOSE)
						{
							System.Console.Out.WriteLine(Thread.CurrentThread.Name + ": TEST: addIndexes(Dir[]) then full merge");
						}
						this.writer2.AddIndexes(dirs);
						this.writer2.ForceMerge(1);
						break;
					}

					case 1:
					{
						if (LuceneTestCase.VERBOSE)
						{
							System.Console.Out.WriteLine(Thread.CurrentThread.Name + ": TEST: addIndexes(Dir[])");
						}
						this.writer2.AddIndexes(dirs);
						break;
					}

					case 2:
					{
						if (LuceneTestCase.VERBOSE)
						{
							System.Console.Out.WriteLine(Thread.CurrentThread.Name + ": TEST: addIndexes(IndexReader[])"
								);
						}
						this.writer2.AddIndexes(this.readers);
						break;
					}

					case 3:
					{
						if (LuceneTestCase.VERBOSE)
						{
							System.Console.Out.WriteLine(Thread.CurrentThread.Name + ": TEST: addIndexes(Dir[]) then maybeMerge"
								);
						}
						this.writer2.AddIndexes(dirs);
						this.writer2.MaybeMerge();
						break;
					}

					case 4:
					{
						if (LuceneTestCase.VERBOSE)
						{
							System.Console.Out.WriteLine(Thread.CurrentThread.Name + ": TEST: commit"
								);
						}
						this.writer2.Commit();
                        break;
					}
				}
			}

			private readonly TestAddIndexes _enclosing;
		}

		// LUCENE-1335: test simultaneous addIndexes & commits
		// from multiple threads
		[Test]
		public virtual void TestAddIndexesWithThreads()
		{
			int NUM_ITER = TEST_NIGHTLY ? 15 : 5;
			int NUM_COPY = 3;
			var c = new CommitAndAddIndexes(this, NUM_COPY);
			c.LaunchThreads(NUM_ITER);
			for (int i = 0; i < 100; i++)
			{
				AddDoc(c.writer2);
			}
			c.JoinThreads();
			int expectedNumDocs = 100 + NUM_COPY * (4 * NUM_ITER / 5) * TestAddIndexes.RunAddIndexesThreads
				.NUM_THREADS * TestAddIndexes.RunAddIndexesThreads.NUM_INIT_DOCS;
			AreEqual(expectedNumDocs, c.writer2.NumDocs, "expected num docs don't match - failures: " + c.failures);
			c.Close(true);
			IsTrue(!c.failures.Any(), "found unexpected failures: " + c.failures);
			IndexReader reader = DirectoryReader.Open(c.dir2);
			AreEqual(expectedNumDocs, reader.NumDocs);
			reader.Dispose();
			c.CloseDir();
		}

		private class CommitAndAddIndexes2 : TestAddIndexes.CommitAndAddIndexes
		{
			/// <exception cref="System.Exception"></exception>
			public CommitAndAddIndexes2(TestAddIndexes _enclosing, int numCopy) : base(_enclosing,numCopy)
				
			{
				this._enclosing = _enclosing;
			}

			internal override void Handle(Exception t)
			{
				if (!(t is AlreadyClosedException) && !(t is ArgumentNullException))
				{
					Console.Out.WriteLine(t.StackTrace);
					lock (this.failures)
					{
						this.failures.Add(t);
					}
				}
			}

			private readonly TestAddIndexes _enclosing;
		}

		// LUCENE-1335: test simultaneous addIndexes & close
		[Test]
		public virtual void TestAddIndexesWithClose()
		{
			int NUM_COPY = 3;
			TestAddIndexes.CommitAndAddIndexes2 c = new TestAddIndexes.CommitAndAddIndexes2(this
				, NUM_COPY);
			//c.writer2.setInfoStream(System.out);
			c.LaunchThreads(-1);
			// Close w/o first stopping/joining the threads
			c.Close(true);
			//c.writer2.close();
			c.JoinThreads();
			c.CloseDir();
			IsTrue(c.failures.Count == 0);
		}

		private class CommitAndAddIndexes3 : TestAddIndexes.RunAddIndexesThreads
		{
			/// <exception cref="System.Exception"></exception>
			public CommitAndAddIndexes3(TestAddIndexes _enclosing, int numCopy) : base(_enclosing,numCopy)
			{
				this._enclosing = _enclosing;
			}

			/// <exception cref="System.Exception"></exception>
			internal override void DoBody(int j, Directory[] dirs)
			{
				switch (j % 5)
				{
					case 0:
					{
						if (LuceneTestCase.VERBOSE)
						{
							System.Console.Out.WriteLine("TEST: " + Thread.CurrentThread.Name 
								+ ": addIndexes + full merge");
						}
						this.writer2.AddIndexes(dirs);
						this.writer2.ForceMerge(1);
						break;
					}

					case 1:
					{
						if (LuceneTestCase.VERBOSE)
						{
							System.Console.Out.WriteLine("TEST: " + Thread.CurrentThread.Name 
								+ ": addIndexes");
						}
						this.writer2.AddIndexes(dirs);
						break;
					}

					case 2:
					{
						if (LuceneTestCase.VERBOSE)
						{
							System.Console.Out.WriteLine("TEST: " + Thread.CurrentThread.Name 
								+ ": addIndexes(IR[])");
						}
						this.writer2.AddIndexes(this.readers);
						break;
					}

					case 3:
					{
						if (LuceneTestCase.VERBOSE)
						{
							System.Console.Out.WriteLine("TEST: " + Thread.CurrentThread.Name 
								+ ": full merge");
						}
						this.writer2.ForceMerge(1);
						break;
					}

					case 4:
					{
						if (LuceneTestCase.VERBOSE)
						{
							System.Console.Out.WriteLine("TEST: " + Thread.CurrentThread.Name 
								+ ": commit");
						}
						this.writer2.Commit();
                        break;
					}
				}
			}

			internal override void Handle(Exception t)
			{
				bool report = true;
				if (t is AlreadyClosedException || t is MergePolicy.MergeAbortedException || t is
					 ArgumentNullException)
				{
					report = !this.didClose;
				}
				else
				{
					if (t is FileNotFoundException)
					{
						report = !this.didClose;
					}
					else
					{
						if (t is IOException)
						{
							Exception t2 = t.InnerException;
							if (t2 is MergePolicy.MergeAbortedException)
							{
								report = !this.didClose;
							}
						}
					}
				}
				if (report)
				{
					Console.Out.WriteLine(t.StackTrace);
					lock (this.failures)
					{
						this.failures.Add(t);
					}
				}
			}

			private readonly TestAddIndexes _enclosing;
		}

		// LUCENE-1335: test simultaneous addIndexes & close
		[Test]
		public virtual void TestAddIndexesWithCloseNoWait()
		{
			int NUM_COPY = 50;
			var c = new CommitAndAddIndexes3(this, NUM_COPY);
			c.LaunchThreads(-1);
			Thread.Sleep(Random().NextInt(10, 500));
			// Close w/o first stopping/joining the threads
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: now close(false)");
			}
			c.Close(false);
			c.JoinThreads();
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: done join threads");
			}
			c.CloseDir();
			IsTrue(c.failures.Count == 0);
		}

		// LUCENE-1335: test simultaneous addIndexes & close
		[Test]
		public virtual void TestAddIndexesWithRollback()
		{
			int NUM_COPY = TEST_NIGHTLY ? 50 : 5;
			var c = new CommitAndAddIndexes3(this, NUM_COPY);
			c.LaunchThreads(-1);
			Thread.Sleep(Random().NextInt(10, 500));
			// Close w/o first stopping/joining the threads
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: now force rollback");
			}
			c.didClose = true;
			c.writer2.Rollback();
			c.JoinThreads();
			c.CloseDir();
			IsTrue(c.failures.Count == 0);
		}

		// LUCENE-2996: tests that addIndexes(IndexReader) applies existing deletes correctly.
		[Test]
		public virtual void TestExistingDeletes()
		{
			Directory[] dirs = new Directory[2];
			for (int i = 0; i < dirs.Length; i++)
			{
				dirs[i] = NewDirectory();
				IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
					(Random()));
				IndexWriter writer = new IndexWriter(dirs[i], conf);
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(new StringField("id", "myid", Field.Store.NO));
				writer.AddDocument(doc);
				writer.Dispose();
			}
			IndexWriterConfig conf_1 = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			IndexWriter writer_1 = new IndexWriter(dirs[0], conf_1);
			// Now delete the document
			writer_1.DeleteDocuments(new Term("id", "myid"));
			IndexReader r = DirectoryReader.Open(dirs[1]);
			try
			{
				writer_1.AddIndexes(r);
			}
			finally
			{
				r.Dispose();
			}
			writer_1.Commit();
			AreEqual(1, writer_1.NumDocs, "Documents from the incoming index should not have been deleted");
			writer_1.Dispose();
			foreach (Directory dir in dirs)
			{
				dir.Dispose();
			}
		}

		// just like addDocs but with ID, starting from docStart
		/// <exception cref="System.IO.IOException"></exception>
		private void AddDocsWithID(IndexWriter writer, int numDocs, int docStart)
		{
			for (int i = 0; i < numDocs; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(NewTextField("content", "aaa", Field.Store.NO));
				doc.Add(NewTextField("id", string.Empty + (docStart + i), Field.Store.YES));
				writer.AddDocument(doc);
			}
		}

		[Test]
		public virtual void TestSimpleCaseCustomCodec()
		{
			// main directory
			Directory dir = NewDirectory();
			// two auxiliary directories
			Directory aux = NewDirectory();
			Directory aux2 = NewDirectory();
			Codec codec = new TestAddIndexes.CustomPerFieldCodec();
			IndexWriter writer = null;
			writer = NewWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetOpenMode(IndexWriterConfig.OpenMode.CREATE).SetCodec(codec));
			// add 100 documents
			AddDocsWithID(writer, 100, 0);
			AreEqual(100, writer.MaxDoc);
			writer.Commit();
			writer.Dispose();
			TestUtil.CheckIndex(dir);
			writer = NewWriter(aux, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode.CREATE).SetCodec
				(codec).SetMaxBufferedDocs(10)).SetMergePolicy(NewLogMergePolicy(false)));
			// add 40 documents in separate files
			AddDocs(writer, 40);
			AreEqual(40, writer.MaxDoc);
			writer.Commit();
			writer.Dispose();
			writer = NewWriter(aux2, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetOpenMode(IndexWriterConfig.OpenMode.CREATE).SetCodec(codec));
			// add 40 documents in compound files
			AddDocs2(writer, 50);
			AreEqual(50, writer.MaxDoc);
			writer.Commit();
			writer.Dispose();
			// test doc count before segments are merged
			writer = NewWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND).SetCodec(codec));
			AreEqual(100, writer.MaxDoc);
			writer.AddIndexes(aux, aux2);
			AreEqual(190, writer.MaxDoc);
			writer.Dispose();
			dir.Dispose();
			aux.Dispose();
			aux2.Dispose();
		}

		private sealed class CustomPerFieldCodec : Lucene46Codec
		{
			private readonly PostingsFormat simpleTextFormat = PostingsFormat.ForName("SimpleText"
				);

			private readonly PostingsFormat defaultFormat = PostingsFormat.ForName("Lucene41"
				);

			private readonly PostingsFormat mockSepFormat = PostingsFormat.ForName("MockSep");

			public override PostingsFormat GetPostingsFormatForField(string field)
			{
				if (field.Equals("id"))
				{
					return simpleTextFormat;
				}
				else
				{
					if (field.Equals("content"))
					{
						return mockSepFormat;
					}
					else
					{
						return defaultFormat;
					}
				}
			}
		}

		// LUCENE-2790: tests that the non CFS files were deleted by addIndexes
		[Test]
		public virtual void TestNonCFSLeftovers()
		{
			Directory[] dirs = new Directory[2];
			for (int i = 0; i < dirs.Length; i++)
			{
				dirs[i] = new RAMDirectory();
				IndexWriter w = new IndexWriter(dirs[i], new IndexWriterConfig(TEST_VERSION_CURRENT
					, new MockAnalyzer(Random())));
				Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
				FieldType customType = new FieldType(TextField.TYPE_STORED);
				customType.StoreTermVectors = true;
				d.Add(new Field("c", "v", customType));
				w.AddDocument(d);
				w.Dispose();
			}
			IndexReader[] readers = new IndexReader[] { DirectoryReader.Open(dirs[0]), DirectoryReader
				.Open(dirs[1]) };
			Directory dir = new MockDirectoryWrapper(Random(), new RAMDirectory());
			IndexWriterConfig conf = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetMergePolicy(NewLogMergePolicy(true));
			MergePolicy lmp = conf.MergePolicy;
			// Force creation of CFS:
			lmp.SetNoCFSRatio(1.0);
			lmp.SetMaxCFSSegmentSizeMB(double.PositiveInfinity);
			IndexWriter w3 = new IndexWriter(dir, conf);
			w3.AddIndexes(readers);
			w3.Dispose();
			// we should now see segments_X,
			// segments.gen,_Y.cfs,_Y.cfe, _Z.si
			AreEqual(5, dir.ListAll().Length, "Only one compound segment should exist, but got: "+ Arrays.ToString(dir.ListAll()));
			dir.Dispose();
		}

		private sealed class UnRegisteredCodec : FilterCodec
		{
			public UnRegisteredCodec() : base("NotRegistered", new Lucene46Codec())
			{
			}
		}

		
        //public virtual void TestAddIndexMissingCodec()
        //{
        //    BaseDirectoryWrapper toAdd = NewDirectory();
        //    // Disable checkIndex, else we get an exception because
        //    // of the unregistered codec:
        //    toAdd.SetCheckIndexOnClose(false);
        //    {
        //        IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
        //            (Random()));
        //        conf.SetCodec(new TestAddIndexes.UnRegisteredCodec());
        //        IndexWriter w = new IndexWriter(toAdd, conf);
        //        Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
        //            ();
        //        FieldType customType = new FieldType();
        //        customType.Indexed = true;
        //        doc.Add(NewField("foo", "bar", customType));
        //        w.AddDocument(doc);
        //        w.Dispose();
        //    }
        //    {
        //        Directory dir = NewDirectory();
        //        IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
        //            (Random()));
        //        conf.SetCodec(TestUtil.AlwaysPostingsFormat(new Pulsing41PostingsFormat(1 + Random
        //            ().Next(20))));
        //        IndexWriter w = new IndexWriter(dir, conf);
        //        try
        //        {
        //            w.AddIndexes(toAdd);
        //            Fail("no such codec");
        //        }
        //        catch (ArgumentException)
        //        {
        //        }
        //        // expected
        //        w.Dispose();
        //        IndexReader open = DirectoryReader.Open(dir);
        //        AreEqual(0, open.NumDocs);
        //        open.Dispose();
        //        dir.Dispose();
        //    }
        //    try
        //    {
        //        DirectoryReader.Open(toAdd);
        //        Fail("no such codec");
        //    }
        //    catch (ArgumentException)
        //    {
        //    }
        //    // expected
        //    toAdd.Dispose();
        //}

		// LUCENE-3575
		[Test]
		public virtual void TestFieldNamesChanged()
		{
			Directory d1 = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), d1);
			var doc = new Lucene.Net.Documents.Document
			{
			    NewStringField("f1", "doc1 field1", Field.Store.YES),
			    NewStringField("id", "1", Field.Store.YES)
			};
		    w.AddDocument(doc);
			IndexReader r1 = w.GetReader();
			w.Close();
			Directory d2 = NewDirectory();
			w = new RandomIndexWriter(Random(), d2);
			doc = new Lucene.Net.Documents.Document
			{
			    NewStringField("f2", "doc2 field2", Field.Store.YES),
			    NewStringField("id", "2", Field.Store.YES)
			};
		    w.AddDocument(doc);
			IndexReader r2 = w.GetReader();
			w.Close();
			Directory d3 = NewDirectory();
			w = new RandomIndexWriter(Random(), d3);
			w.AddIndexes(r1, r2);
			r1.Dispose();
			d1.Dispose();
			r2.Dispose();
			d2.Dispose();
			IndexReader r3 = w.GetReader();
			w.Close();
			AreEqual(2, r3.NumDocs);
			for (int docID = 0; docID < 2; docID++)
			{
				Lucene.Net.Documents.Document d = r3.Document(docID);
				if (d.Get("id").Equals("1"))
				{
					AreEqual("doc1 field1", d.Get("f1"));
				}
				else
				{
					AreEqual("doc2 field2", d.Get("f2"));
				}
			}
			r3.Dispose();
			d3.Dispose();
		}

		[Test]
		public virtual void TestAddEmpty()
		{
			Directory d1 = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), d1);
			MultiReader empty = new MultiReader();
			w.AddIndexes(empty);
			w.Close();
			DirectoryReader dr = DirectoryReader.Open(d1);
			foreach (AtomicReaderContext ctx in dr.Leaves)
			{
				IsTrue(ctx.Reader.MaxDoc > 0, "empty segments should be dropped by addIndexes");
			}
			dr.Dispose();
			d1.Dispose();
		}

		// Currently it's impossible to end up with a segment with all documents
		// deleted, as such segments are dropped. Still, to validate that addIndexes
		// works with such segments, or readers that end up in such state, we fake an
		// all deleted segment.
		[Test]
		public virtual void TestFakeAllDeleted()
		{
			Directory src = NewDirectory();
			Directory dest = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), src);
			w.AddDocument(new Lucene.Net.Documents.Document());
			IndexReader allDeletedReader = new AllDeletedFilterReader(((AtomicReader)w.GetReader
				().Leaves[0].Reader));
			w.Close();
			w = new RandomIndexWriter(Random(), dest);
			w.AddIndexes(allDeletedReader);
			w.Close();
			DirectoryReader dr = DirectoryReader.Open(src);
			foreach (AtomicReaderContext ctx in dr.Leaves)
			{
				IsTrue(ctx.Reader.MaxDoc > 0, "empty segments should be dropped by addIndexes");
			}
			dr.Dispose();
			allDeletedReader.Dispose();
			src.Dispose();
			dest.Dispose();
		}

		/// <summary>
		/// Make sure an open IndexWriter on an incoming Directory
		/// causes a LockObtainFailedException
		/// </summary>
		[Test]
		public virtual void TestLocksBlock()
		{
			Directory src = NewDirectory();
			RandomIndexWriter w1 = new RandomIndexWriter(Random(), src);
			w1.AddDocument(new Lucene.Net.Documents.Document());
			w1.Commit();
			Directory dest = NewDirectory();
			IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			iwc.SetWriteLockTimeout(1);
			RandomIndexWriter w2 = new RandomIndexWriter(Random(), dest, iwc);
			try
			{
				w2.AddIndexes(src);
				Fail("did not hit expected exception");
			}
			catch (LockObtainFailedException)
			{
			}
			// expected
			IOUtils.Close(w1, w2, src, dest);
		}
	}
}
