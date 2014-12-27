/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Test.Index
{
	public class TestIndexWriterCommit : LuceneTestCase
	{
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestCommitOnClose()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			for (int i = 0; i < 14; i++)
			{
				TestIndexWriter.AddDoc(writer);
			}
			writer.Dispose();
			Term searchTerm = new Term("content", "aaa");
			DirectoryReader reader = DirectoryReader.Open(dir);
			IndexSearcher searcher = NewSearcher(reader);
			ScoreDoc[] hits = searcher.Search(new TermQuery(searchTerm), null, 1000).ScoreDocs;
			AreEqual("first number of hits", 14, hits.Length);
			reader.Dispose();
			reader = DirectoryReader.Open(dir);
			writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())));
			for (int i_1 = 0; i_1 < 3; i_1++)
			{
				for (int j = 0; j < 11; j++)
				{
					TestIndexWriter.AddDoc(writer);
				}
				IndexReader r = DirectoryReader.Open(dir);
				searcher = NewSearcher(r);
				hits = searcher.Search(new TermQuery(searchTerm), null, 1000).ScoreDocs;
				AreEqual("reader incorrectly sees changes from writer", 14
					, hits.Length);
				r.Dispose();
				IsTrue("reader should have still been current", reader.IsCurrent
					());
			}
			// Now, close the writer:
			writer.Dispose();
			IsFalse("reader should not be current now", reader.IsCurrent
				());
			IndexReader r_1 = DirectoryReader.Open(dir);
			searcher = NewSearcher(r_1);
			hits = searcher.Search(new TermQuery(searchTerm), null, 1000).ScoreDocs;
			AreEqual("reader did not see changes after writer was closed"
				, 47, hits.Length);
			r_1.Dispose();
			reader.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestCommitOnCloseAbort()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(10)));
			for (int i = 0; i < 14; i++)
			{
				TestIndexWriter.AddDoc(writer);
			}
			writer.Dispose();
			Term searchTerm = new Term("content", "aaa");
			IndexReader reader = DirectoryReader.Open(dir);
			IndexSearcher searcher = NewSearcher(reader);
			ScoreDoc[] hits = searcher.Search(new TermQuery(searchTerm), null, 1000).ScoreDocs;
			AreEqual("first number of hits", 14, hits.Length);
			reader.Dispose();
			writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND).SetMaxBufferedDocs
				(10)));
			for (int j = 0; j < 17; j++)
			{
				TestIndexWriter.AddDoc(writer);
			}
			// Delete all docs:
			writer.DeleteDocuments(searchTerm);
			reader = DirectoryReader.Open(dir);
			searcher = NewSearcher(reader);
			hits = searcher.Search(new TermQuery(searchTerm), null, 1000).ScoreDocs;
			AreEqual("reader incorrectly sees changes from writer", 14
				, hits.Length);
			reader.Dispose();
			// Now, close the writer:
			writer.Rollback();
			TestIndexWriter.AssertNoUnreferencedFiles(dir, "unreferenced files remain after rollback()"
				);
			reader = DirectoryReader.Open(dir);
			searcher = NewSearcher(reader);
			hits = searcher.Search(new TermQuery(searchTerm), null, 1000).ScoreDocs;
			AreEqual("saw changes after writer.abort", 14, hits.Length
				);
			reader.Dispose();
			// Now make sure we can re-open the index, add docs,
			// and all is good:
			writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND).SetMaxBufferedDocs
				(10)));
			// On abort, writer in fact may write to the same
			// segments_N file:
			if (dir is MockDirectoryWrapper)
			{
				((MockDirectoryWrapper)dir).SetPreventDoubleWrite(false);
			}
			for (int i_1 = 0; i_1 < 12; i_1++)
			{
				for (int j_1 = 0; j_1 < 17; j_1++)
				{
					TestIndexWriter.AddDoc(writer);
				}
				IndexReader r = DirectoryReader.Open(dir);
				searcher = NewSearcher(r);
				hits = searcher.Search(new TermQuery(searchTerm), null, 1000).ScoreDocs;
				AreEqual("reader incorrectly sees changes from writer", 14
					, hits.Length);
				r.Dispose();
			}
			writer.Dispose();
			IndexReader r_1 = DirectoryReader.Open(dir);
			searcher = NewSearcher(r_1);
			hits = searcher.Search(new TermQuery(searchTerm), null, 1000).ScoreDocs;
			AreEqual("didn't see changes after close", 218, hits.Length
				);
			r_1.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestCommitOnCloseDiskUsage()
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
			MockDirectoryWrapper dir = NewMockDirectory();
			Analyzer analyzer;
			if (Random().NextBoolean())
			{
				// no payloads
				analyzer = new _Analyzer_188();
			}
			else
			{
				// fixed length payloads
				int length = Random().Next(200);
				analyzer = new _Analyzer_197(length);
			}
			IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, analyzer).SetMaxBufferedDocs(10)).SetReaderPooling(false)
				.SetMergePolicy(NewLogMergePolicy(10)));
			for (int j = 0; j < 30; j++)
			{
				TestIndexWriter.AddDocWithIndex(writer, j);
			}
			writer.Dispose();
			dir.ResetMaxUsedSizeInBytes();
			dir.SetTrackDiskUsage(true);
			long startDiskUsage = dir.GetMaxUsedSizeInBytes();
			writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
				, analyzer).SetOpenMode(IndexWriterConfig.OpenMode.APPEND).SetMaxBufferedDocs(10
				)).SetMergeScheduler(new SerialMergeScheduler()).SetReaderPooling(false).SetMergePolicy
				(NewLogMergePolicy(10)));
			for (int j_1 = 0; j_1 < 1470; j_1++)
			{
				TestIndexWriter.AddDocWithIndex(writer, j_1);
			}
			long midDiskUsage = dir.GetMaxUsedSizeInBytes();
			dir.ResetMaxUsedSizeInBytes();
			writer.ForceMerge(1);
			writer.Dispose();
			DirectoryReader.Open(dir).Dispose();
			long endDiskUsage = dir.GetMaxUsedSizeInBytes();
			// Ending index is 50X as large as starting index; due
			// to 3X disk usage normally we allow 150X max
			// transient usage.  If something is wrong w/ deleter
			// and it doesn't delete intermediate segments then it
			// will exceed this 150X:
			// System.out.println("start " + startDiskUsage + "; mid " + midDiskUsage + ";end " + endDiskUsage);
			IsTrue("writer used too much space while adding documents: mid="
				 + midDiskUsage + " start=" + startDiskUsage + " end=" + endDiskUsage + " max=" 
				+ (startDiskUsage * 150), midDiskUsage < 150 * startDiskUsage);
			IsTrue("writer used too much space after close: endDiskUsage="
				 + endDiskUsage + " startDiskUsage=" + startDiskUsage + " max=" + (startDiskUsage
				 * 150), endDiskUsage < 150 * startDiskUsage);
			dir.Dispose();
		}

		private sealed class _Analyzer_188 : Analyzer
		{
			public _Analyzer_188()
			{
			}

			protected override Analyzer.TokenStreamComponents CreateComponents(string fieldName
				, StreamReader reader)
			{
				return new Analyzer.TokenStreamComponents(new MockTokenizer(reader, MockTokenizer
					.WHITESPACE, true));
			}
		}

		private sealed class _Analyzer_197 : Analyzer
		{
			public _Analyzer_197(int length)
			{
				this.length = length;
			}

			protected override Analyzer.TokenStreamComponents CreateComponents(string fieldName
				, StreamReader reader)
			{
				Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, true);
				return new Analyzer.TokenStreamComponents(tokenizer, new MockFixedLengthPayloadFilter
					(LuceneTestCase.Random(), tokenizer, length));
			}

			private readonly int length;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestCommitOnCloseForceMerge()
		{
			Directory dir = NewDirectory();
			// Must disable throwing exc on double-write: this
			// test uses IW.rollback which easily results in
			// writing to same file more than once
			if (dir is MockDirectoryWrapper)
			{
				((MockDirectoryWrapper)dir).SetPreventDoubleWrite(false);
			}
			IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(10)).SetMergePolicy
				(NewLogMergePolicy(10)));
			for (int j = 0; j < 17; j++)
			{
				TestIndexWriter.AddDocWithIndex(writer, j);
			}
			writer.Dispose();
			writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND));
			writer.ForceMerge(1);
			// Open a reader before closing (commiting) the writer:
			DirectoryReader reader = DirectoryReader.Open(dir);
			// Reader should see index as multi-seg at this
			// point:
			IsTrue("Reader incorrectly sees one segment", reader.Leaves
				().Count > 1);
			reader.Dispose();
			// Abort the writer:
			writer.Rollback();
			TestIndexWriter.AssertNoUnreferencedFiles(dir, "aborted writer after forceMerge");
			// Open a reader after aborting writer:
			reader = DirectoryReader.Open(dir);
			// Reader should still see index as multi-segment
			IsTrue("Reader incorrectly sees one segment", reader.Leaves
				().Count > 1);
			reader.Dispose();
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: do real full merge");
			}
			writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND));
			writer.ForceMerge(1);
			writer.Dispose();
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: writer closed");
			}
			TestIndexWriter.AssertNoUnreferencedFiles(dir, "aborted writer after forceMerge");
			// Open a reader after aborting writer:
			reader = DirectoryReader.Open(dir);
			// Reader should see index as one segment
			AreEqual("Reader incorrectly sees more than one segment", 
				1, reader.Leaves.Count);
			reader.Dispose();
			dir.Dispose();
		}

		// LUCENE-2095: make sure with multiple threads commit
		// doesn't return until all changes are in fact in the
		// index
		/// <exception cref="System.Exception"></exception>
		public virtual void TestCommitThreadSafety()
		{
			int NUM_THREADS = 5;
			double RUN_SEC = 0.5;
			Directory dir = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy()));
			TestUtil.ReduceOpenFiles(w.w);
			w.Commit();
			AtomicBoolean failed = new AtomicBoolean();
			Sharpen.Thread[] threads = new Sharpen.Thread[NUM_THREADS];
			long endTime = DateTime.Now.CurrentTimeMillis() + ((long)(RUN_SEC * 1000));
			for (int i = 0; i < NUM_THREADS; i++)
			{
				int finalI = i;
				threads[i] = new _Thread_340(dir, failed, finalI, w, endTime);
				threads[i].Start();
			}
			for (int i_1 = 0; i_1 < NUM_THREADS; i_1++)
			{
				threads[i_1].Join();
			}
			IsFalse(failed.Get());
			w.Dispose();
			dir.Dispose();
		}

		private sealed class _Thread_340 : Sharpen.Thread
		{
			public _Thread_340(Directory dir, AtomicBoolean failed, int finalI, RandomIndexWriter
				 w, long endTime)
			{
				this.dir = dir;
				this.failed = failed;
				this.finalI = finalI;
				this.w = w;
				this.endTime = endTime;
			}

			public override void Run()
			{
				try
				{
					Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
						();
					DirectoryReader r = DirectoryReader.Open(dir);
					Field f = LuceneTestCase.NewStringField("f", string.Empty, Field.Store.NO);
					doc.Add(f);
					int count = 0;
					do
					{
						if (failed.Get())
						{
							break;
						}
						for (int j = 0; j < 10; j++)
						{
							string s = finalI + "_" + (count++).ToString();
							f.StringValue = s);
							w.AddDocument(doc);
							w.Commit();
							DirectoryReader r2 = DirectoryReader.OpenIfChanged(r);
							IsNotNull(r2);
							IsTrue(r2 != r);
							r.Dispose();
							r = r2;
							AreEqual("term=f:" + s + "; r=" + r, 1, r.DocFreq(new Term
								("f", s)));
						}
					}
					while (DateTime.Now.CurrentTimeMillis() < endTime);
					r.Dispose();
				}
				catch (Exception t)
				{
					failed.Set(true);
					throw new RuntimeException(t);
				}
			}

			private readonly Directory dir;

			private readonly AtomicBoolean failed;

			private readonly int finalI;

			private readonly RandomIndexWriter w;

			private readonly long endTime;
		}

		// LUCENE-1044: test writer.commit() when ac=false
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestForceCommit()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2)).SetMergePolicy
				(NewLogMergePolicy(5)));
			writer.Commit();
			for (int i = 0; i < 23; i++)
			{
				TestIndexWriter.AddDoc(writer);
			}
			DirectoryReader reader = DirectoryReader.Open(dir);
			AreEqual(0, reader.NumDocs);
			writer.Commit();
			DirectoryReader reader2 = DirectoryReader.OpenIfChanged(reader);
			IsNotNull(reader2);
			AreEqual(0, reader.NumDocs);
			AreEqual(23, reader2.NumDocs);
			reader.Dispose();
			for (int i_1 = 0; i_1 < 17; i_1++)
			{
				TestIndexWriter.AddDoc(writer);
			}
			AreEqual(23, reader2.NumDocs);
			reader2.Dispose();
			reader = DirectoryReader.Open(dir);
			AreEqual(23, reader.NumDocs);
			reader.Dispose();
			writer.Commit();
			reader = DirectoryReader.Open(dir);
			AreEqual(40, reader.NumDocs);
			reader.Dispose();
			writer.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestFutureCommit()
		{
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())).SetIndexDeletionPolicy(NoDeletionPolicy.INSTANCE));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			w.AddDocument(doc);
			// commit to "first"
			IDictionary<string, string> commitData = new Dictionary<string, string>();
			commitData.Put("tag", "first");
			w.SetCommitData(commitData);
			w.Commit();
			// commit to "second"
			w.AddDocument(doc);
			commitData.Put("tag", "second");
			w.SetCommitData(commitData);
			w.Dispose();
			// open "first" with IndexWriter
			IndexCommit commit = null;
			foreach (IndexCommit c in DirectoryReader.ListCommits(dir))
			{
				if (c.GetUserData().Get("tag").Equals("first"))
				{
					commit = c;
					break;
				}
			}
			IsNotNull(commit);
			w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetIndexDeletionPolicy(NoDeletionPolicy.INSTANCE).SetIndexCommit(commit
				));
			AreEqual(1, w.NumDocs);
			// commit IndexWriter to "third"
			w.AddDocument(doc);
			commitData.Put("tag", "third");
			w.SetCommitData(commitData);
			w.Dispose();
			// make sure "second" commit is still there
			commit = null;
			foreach (IndexCommit c_1 in DirectoryReader.ListCommits(dir))
			{
				if (c_1.GetUserData().Get("tag").Equals("second"))
				{
					commit = c_1;
					break;
				}
			}
			IsNotNull(commit);
			dir.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestZeroCommits()
		{
			// Tests that if we don't call commit(), the directory has 0 commits. This has
			// changed since LUCENE-2386, where before IW would always commit on a fresh
			// new index.
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			try
			{
				DirectoryReader.ListCommits(dir);
				Fail("listCommits should have thrown an exception over empty index"
					);
			}
			catch (IndexNotFoundException)
			{
			}
			// that's expected !
			// No changes still should generate a commit, because it's a new index.
			writer.Dispose();
			AreEqual("expected 1 commits!", 1, DirectoryReader.ListCommits
				(dir).Count);
			dir.Dispose();
		}

		// LUCENE-1274: test writer.prepareCommit()
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestPrepareCommit()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2)).SetMergePolicy
				(NewLogMergePolicy(5)));
			writer.Commit();
			for (int i = 0; i < 23; i++)
			{
				TestIndexWriter.AddDoc(writer);
			}
			DirectoryReader reader = DirectoryReader.Open(dir);
			AreEqual(0, reader.NumDocs);
			writer.PrepareCommit();
			IndexReader reader2 = DirectoryReader.Open(dir);
			AreEqual(0, reader2.NumDocs);
			writer.Commit();
			IndexReader reader3 = DirectoryReader.OpenIfChanged(reader);
			IsNotNull(reader3);
			AreEqual(0, reader.NumDocs);
			AreEqual(0, reader2.NumDocs);
			AreEqual(23, reader3.NumDocs);
			reader.Dispose();
			reader2.Dispose();
			for (int i_1 = 0; i_1 < 17; i_1++)
			{
				TestIndexWriter.AddDoc(writer);
			}
			AreEqual(23, reader3.NumDocs);
			reader3.Dispose();
			reader = DirectoryReader.Open(dir);
			AreEqual(23, reader.NumDocs);
			reader.Dispose();
			writer.PrepareCommit();
			reader = DirectoryReader.Open(dir);
			AreEqual(23, reader.NumDocs);
			reader.Dispose();
			writer.Commit();
			reader = DirectoryReader.Open(dir);
			AreEqual(40, reader.NumDocs);
			reader.Dispose();
			writer.Dispose();
			dir.Dispose();
		}

		// LUCENE-1274: test writer.prepareCommit()
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestPrepareCommitRollback()
		{
			Directory dir = NewDirectory();
			if (dir is MockDirectoryWrapper)
			{
				((MockDirectoryWrapper)dir).SetPreventDoubleWrite(false);
			}
			IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2)).SetMergePolicy
				(NewLogMergePolicy(5)));
			writer.Commit();
			for (int i = 0; i < 23; i++)
			{
				TestIndexWriter.AddDoc(writer);
			}
			DirectoryReader reader = DirectoryReader.Open(dir);
			AreEqual(0, reader.NumDocs);
			writer.PrepareCommit();
			IndexReader reader2 = DirectoryReader.Open(dir);
			AreEqual(0, reader2.NumDocs);
			writer.Rollback();
			IndexReader reader3 = DirectoryReader.OpenIfChanged(reader);
			IsNull(reader3);
			AreEqual(0, reader.NumDocs);
			AreEqual(0, reader2.NumDocs);
			reader.Dispose();
			reader2.Dispose();
			writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())));
			for (int i_1 = 0; i_1 < 17; i_1++)
			{
				TestIndexWriter.AddDoc(writer);
			}
			reader = DirectoryReader.Open(dir);
			AreEqual(0, reader.NumDocs);
			reader.Dispose();
			writer.PrepareCommit();
			reader = DirectoryReader.Open(dir);
			AreEqual(0, reader.NumDocs);
			reader.Dispose();
			writer.Commit();
			reader = DirectoryReader.Open(dir);
			AreEqual(17, reader.NumDocs);
			reader.Dispose();
			writer.Dispose();
			dir.Dispose();
		}

		// LUCENE-1274
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestPrepareCommitNoChanges()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			writer.PrepareCommit();
			writer.Commit();
			writer.Dispose();
			IndexReader reader = DirectoryReader.Open(dir);
			AreEqual(0, reader.NumDocs);
			reader.Dispose();
			dir.Dispose();
		}

		// LUCENE-1382
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestCommitUserData()
		{
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetMaxBufferedDocs(2)));
			for (int j = 0; j < 17; j++)
			{
				TestIndexWriter.AddDoc(w);
			}
			w.Dispose();
			DirectoryReader r = DirectoryReader.Open(dir);
			// commit(Map) never called for this index
			AreEqual(0, r.GetIndexCommit().GetUserData().Count);
			r.Dispose();
			w = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetMaxBufferedDocs(2)));
			for (int j_1 = 0; j_1 < 17; j_1++)
			{
				TestIndexWriter.AddDoc(w);
			}
			IDictionary<string, string> data = new Dictionary<string, string>();
			data.Put("label", "test1");
			w.SetCommitData(data);
			w.Dispose();
			r = DirectoryReader.Open(dir);
			AreEqual("test1", r.GetIndexCommit().GetUserData().Get("label"
				));
			r.Dispose();
			w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())));
			w.ForceMerge(1);
			w.Dispose();
			dir.Dispose();
		}
	}
}
