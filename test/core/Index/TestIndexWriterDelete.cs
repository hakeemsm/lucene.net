using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Support;
using Lucene.Net.TestFramework;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.TestFramework.Index;
using Lucene.Net.TestFramework.Util;
using Lucene.Net.Util;
using NUnit.Framework;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Test.Index
{
    [TestFixture]
    public class TestIndexWriterDelete : LuceneTestCase
    {
        // test the simple case
        [Test]
        public virtual void TestSimpleCase()
        {
            string[] keywords = { "1", "2" };
            string[] unindexed = { "Netherlands", "Italy" };
            string[] unstored = { "Amsterdam has lots of bridges", "Venice has lots of canals" };
            string[] text = { "Amsterdam", "Venice" };
            Directory dir = NewDirectory();
            IndexWriter modifier = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
                (TEST_VERSION_CURRENT, new MockAnalyzer(Random(), MockTokenizer.WHITESPACE, false
                )).SetMaxBufferedDeleteTerms(1)));
            FieldType custom1 = new FieldType();
            custom1.Stored = (true);
            for (int i = 0; i < keywords.Length; i++)
            {
                var doc = new Lucene.Net.Documents.Document
				{
				    NewStringField("id", keywords[i], Field.Store.YES),
				    NewField("country", unindexed[i], custom1),
				    NewTextField("contents", unstored[i], Field.Store.NO),
				    NewTextField("city", text[i], Field.Store.YES)
				};
                modifier.AddDocument(doc);
            }
            modifier.ForceMerge(1);
            modifier.Commit();
            Term term = new Term("city", "Amsterdam");
            int hitCount = GetHitCount(dir, term);
            AreEqual(1, hitCount);
            if (VERBOSE)
            {
                System.Console.Out.WriteLine("\nTEST: now delete by term=" + term);
            }
            modifier.DeleteDocuments(term);
            modifier.Commit();
            if (VERBOSE)
            {
                System.Console.Out.WriteLine("\nTEST: now getHitCount");
            }
            hitCount = GetHitCount(dir, term);
            AreEqual(0, hitCount);
            modifier.Dispose();
            dir.Dispose();
        }

        // test when delete terms only apply to disk segments
        [Test]
        public virtual void TestNonRAMDelete()
        {
            Directory dir = NewDirectory();
            IndexWriter modifier = new IndexWriter(dir, ((IndexWriterConfig)((IndexWriterConfig
                )NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random(), MockTokenizer
                .WHITESPACE, false)).SetMaxBufferedDocs(2)).SetMaxBufferedDeleteTerms(2)));
            int id = 0;
            int value = 100;
            for (int i = 0; i < 7; i++)
            {
                AddDoc(modifier, ++id, value);
            }
            modifier.Commit();
            AreEqual(0, modifier.NumBufferedDocuments);
            IsTrue(0 < modifier.SegmentCount);
            modifier.Commit();
            IndexReader reader = DirectoryReader.Open(dir);
            AreEqual(7, reader.NumDocs);
            reader.Dispose();
            modifier.DeleteDocuments(new Term("value", value.ToString()));
            modifier.Commit();
            reader = DirectoryReader.Open(dir);
            AreEqual(0, reader.NumDocs);
            reader.Dispose();
            modifier.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestMaxBufferedDeletes()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
                (TEST_VERSION_CURRENT, new MockAnalyzer(Random(), MockTokenizer.WHITESPACE, false
                )).SetMaxBufferedDeleteTerms(1)));
            writer.AddDocument(new Lucene.Net.Documents.Document());
            writer.DeleteDocuments(new Term("foobar", "1"));
            writer.DeleteDocuments(new Term("foobar", "1"));
            writer.DeleteDocuments(new Term("foobar", "1"));
            AreEqual(3, writer.FlushDeletesCount);
            writer.Dispose();
            dir.Dispose();
        }

        // test when delete terms only apply to ram segments
        [Test]
        public virtual void TestRAMDeletes()
        {
            for (int t = 0; t < 2; t++)
            {
                if (VERBOSE)
                {
                    System.Console.Out.WriteLine("TEST: t=" + t);
                }
                Directory dir = NewDirectory();
                IndexWriter modifier = new IndexWriter(dir, ((IndexWriterConfig)((IndexWriterConfig
                    )NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random(), MockTokenizer
                    .WHITESPACE, false)).SetMaxBufferedDocs(4)).SetMaxBufferedDeleteTerms(4)));
                int id = 0;
                int value = 100;
                AddDoc(modifier, ++id, value);
                if (0 == t)
                {
                    modifier.DeleteDocuments(new Term("value", value.ToString()));
                }
                else
                {
                    modifier.DeleteDocuments(new TermQuery(new Term("value", value.ToString())));
                }
                AddDoc(modifier, ++id, value);
                if (0 == t)
                {
                    modifier.DeleteDocuments(new Term("value", value.ToString()));
                    AreEqual(2, modifier.NumBufferedDeleteTerms);
                    AreEqual(1, modifier.BufferedDeleteTermsSize);
                }
                else
                {
                    modifier.DeleteDocuments(new TermQuery(new Term("value", value.ToString())));
                }
                AddDoc(modifier, ++id, value);
                AreEqual(0, modifier.SegmentCount);
                modifier.Commit();
                IndexReader reader = DirectoryReader.Open(dir);
                AreEqual(1, reader.NumDocs);
                int hitCount = GetHitCount(dir, new Term("id", id.ToString()));
                AreEqual(1, hitCount);
                reader.Dispose();
                modifier.Dispose();
                dir.Dispose();
            }
        }

        // test when delete terms apply to both disk and ram segments
        [Test]
        public virtual void TestBothDeletes()
        {
            Directory dir = NewDirectory();
            IndexWriter modifier = new IndexWriter(dir, ((IndexWriterConfig)((IndexWriterConfig
                )NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random(), MockTokenizer
                .WHITESPACE, false)).SetMaxBufferedDocs(100)).SetMaxBufferedDeleteTerms(100)));
            int id = 0;
            int value = 100;
            for (int i = 0; i < 5; i++)
            {
                AddDoc(modifier, ++id, value);
            }
            value = 200;
            for (int i_1 = 0; i_1 < 5; i_1++)
            {
                AddDoc(modifier, ++id, value);
            }
            modifier.Commit();
            for (int i_2 = 0; i_2 < 5; i_2++)
            {
                AddDoc(modifier, ++id, value);
            }
            modifier.DeleteDocuments(new Term("value", value.ToString()));
            modifier.Commit();
            IndexReader reader = DirectoryReader.Open(dir);
            AreEqual(5, reader.NumDocs);
            modifier.Dispose();
            reader.Dispose();
            dir.Dispose();
        }

        // test that batched delete terms are flushed together
        [Test]
        public virtual void TestBatchDeletes()
        {
            Directory dir = NewDirectory();
            IndexWriter modifier = new IndexWriter(dir, ((IndexWriterConfig)((IndexWriterConfig
                )NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random(), MockTokenizer
                .WHITESPACE, false)).SetMaxBufferedDocs(2)).SetMaxBufferedDeleteTerms(2)));
            int id = 0;
            int value = 100;
            for (int i = 0; i < 7; i++)
            {
                AddDoc(modifier, ++id, value);
            }
            modifier.Commit();
            IndexReader reader = DirectoryReader.Open(dir);
            AreEqual(7, reader.NumDocs);
            reader.Dispose();
            id = 0;
            modifier.DeleteDocuments(new Term("id", (++id).ToString()));
            modifier.DeleteDocuments(new Term("id", (++id).ToString()));
            modifier.Commit();
            reader = DirectoryReader.Open(dir);
            AreEqual(5, reader.NumDocs);
            reader.Dispose();
            Term[] terms = new Term[3];
            for (int i_1 = 0; i_1 < terms.Length; i_1++)
            {
                terms[i_1] = new Term("id", (++id).ToString());
            }
            modifier.DeleteDocuments(terms);
            modifier.Commit();
            reader = DirectoryReader.Open(dir);
            AreEqual(2, reader.NumDocs);
            reader.Dispose();
            modifier.Dispose();
            dir.Dispose();
        }

        // test deleteAll()
        [Test]
        public virtual void TestDeleteAll()
        {
            Directory dir = NewDirectory();
            IndexWriter modifier = new IndexWriter(dir, ((IndexWriterConfig)((IndexWriterConfig
                )NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random(), MockTokenizer
                .WHITESPACE, false)).SetMaxBufferedDocs(2)).SetMaxBufferedDeleteTerms(2)));
            int id = 0;
            int value = 100;
            for (int i = 0; i < 7; i++)
            {
                AddDoc(modifier, ++id, value);
            }
            modifier.Commit();
            IndexReader reader = DirectoryReader.Open(dir);
            AreEqual(7, reader.NumDocs);
            reader.Dispose();
            // Add 1 doc (so we will have something buffered)
            AddDoc(modifier, 99, value);
            // Delete all
            modifier.DeleteAll();
            // Delete all shouldn't be on disk yet
            reader = DirectoryReader.Open(dir);
            AreEqual(7, reader.NumDocs);
            reader.Dispose();
            // Add a doc and update a doc (after the deleteAll, before the commit)
            AddDoc(modifier, 101, value);
            UpdateDoc(modifier, 102, value);
            // commit the delete all
            modifier.Commit();
            // Validate there are no docs left
            reader = DirectoryReader.Open(dir);
            AreEqual(2, reader.NumDocs);
            reader.Dispose();
            modifier.Dispose();
            dir.Dispose();
        }

        /// <exception cref="System.IO.IOException"></exception>
        [Test]
        public virtual void TestDeleteAllNoDeadLock()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter modifier = new RandomIndexWriter(Random(), dir);
            int numThreads = AtLeast(2);
            Thread[] threads = new Thread[numThreads];
            CountdownEvent latch = new CountdownEvent(1);
            CountdownEvent doneLatch = new CountdownEvent(numThreads);
            for (int i = 0; i < numThreads; i++)
            {
                int offset = i;
                threads[i] = new Thread(new DocThread(offset, latch, modifier, doneLatch).Run);
                threads[i].Start();
            }
            latch.Signal();
            while (!doneLatch.Wait(1))
            {
                modifier.DeleteAll();
                if (VERBOSE)
                {
                    System.Console.Out.WriteLine("del all");
                }
            }
            modifier.DeleteAll();
            foreach (Thread thread in threads)
            {
                thread.Join();
            }
            modifier.Close();
            DirectoryReader reader = DirectoryReader.Open(dir);
            AreEqual(reader.MaxDoc, 0);
            AreEqual(reader.NumDocs, 0);
            AreEqual(reader.NumDeletedDocs, 0);
            reader.Dispose();
            dir.Dispose();
        }

        private sealed class DocThread
        {
            public DocThread(int offset, CountdownEvent latch, RandomIndexWriter modifier,
                CountdownEvent doneLatch)
            {
                this.offset = offset;
                this.latch = latch;
                this.modifier = modifier;
                this.doneLatch = doneLatch;
            }

            public void Run()
            {
                int id = offset * 1000;
                int value = 100;
                try
                {
                    latch.Wait();
                    for (int j = 0; j < 1000; j++)
                    {
                        Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
						{
						    LuceneTestCase.NewTextField("content", "aaa", Field.Store.NO),
						    LuceneTestCase.NewStringField("id", (id++).ToString(), Field.Store.YES),
						    LuceneTestCase.NewStringField("value", value.ToString(), Field.Store.NO)
						};
                        if (LuceneTestCase.DefaultCodecSupportsDocValues())
                        {
                            doc.Add(new NumericDocValuesField("dv", value));
                        }
                        modifier.AddDocument(doc);
                        if (LuceneTestCase.VERBOSE)
                        {
                            System.Console.Out.WriteLine("\tThread[" + offset + "]: add doc: " + id);
                        }
                    }
                }
                catch (Exception e)
                {
                    throw new SystemException(e.Message, e);
                }
                finally
                {
                    doneLatch.Signal();
                    if (LuceneTestCase.VERBOSE)
                    {
                        System.Console.Out.WriteLine("\tThread[" + offset + "]: done indexing");
                    }
                }
            }

            private readonly int offset;

            private readonly CountdownEvent latch;

            private readonly RandomIndexWriter modifier;

            private readonly CountdownEvent doneLatch;
        }

        // test rollback of deleteAll()
        [Test]
        public virtual void TestDeleteAllRollback()
        {
            Directory dir = NewDirectory();
            IndexWriter modifier = new IndexWriter(dir, ((IndexWriterConfig)((IndexWriterConfig
                )NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random(), MockTokenizer
                .WHITESPACE, false)).SetMaxBufferedDocs(2)).SetMaxBufferedDeleteTerms(2)));
            int id = 0;
            int value = 100;
            for (int i = 0; i < 7; i++)
            {
                AddDoc(modifier, ++id, value);
            }
            modifier.Commit();
            AddDoc(modifier, ++id, value);
            IndexReader reader = DirectoryReader.Open(dir);
            AreEqual(7, reader.NumDocs);
            reader.Dispose();
            // Delete all
            modifier.DeleteAll();
            // Roll it back
            modifier.Rollback();
            modifier.Dispose();
            // Validate that the docs are still there
            reader = DirectoryReader.Open(dir);
            AreEqual(7, reader.NumDocs);
            reader.Dispose();
            dir.Dispose();
        }

        // test deleteAll() w/ near real-time reader
        [Test]
        public virtual void TestDeleteAllNRT()
        {
            Directory dir = NewDirectory();
            IndexWriter modifier = new IndexWriter(dir, ((IndexWriterConfig)((IndexWriterConfig
                )NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random(), MockTokenizer
                .WHITESPACE, false)).SetMaxBufferedDocs(2)).SetMaxBufferedDeleteTerms(2)));
            int id = 0;
            int value = 100;
            for (int i = 0; i < 7; i++)
            {
                AddDoc(modifier, ++id, value);
            }
            modifier.Commit();
            IndexReader reader = modifier.Reader;
            AreEqual(7, reader.NumDocs);
            reader.Dispose();
            AddDoc(modifier, ++id, value);
            AddDoc(modifier, ++id, value);
            // Delete all
            modifier.DeleteAll();
            reader = modifier.Reader;
            AreEqual(0, reader.NumDocs);
            reader.Dispose();
            // Roll it back
            modifier.Rollback();
            modifier.Dispose();
            // Validate that the docs are still there
            reader = DirectoryReader.Open(dir);
            AreEqual(7, reader.NumDocs);
            reader.Dispose();
            dir.Dispose();
        }

        /// <exception cref="System.IO.IOException"></exception>
        private void UpdateDoc(IndexWriter modifier, int id, int value)
        {
            Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                ();
            doc.Add(NewTextField("content", "aaa", Field.Store.NO));
            doc.Add(NewStringField("id", id.ToString(), Field.Store.YES));
            doc.Add(NewStringField("value", value.ToString(), Field.Store.NO));
            if (DefaultCodecSupportsDocValues())
            {
                doc.Add(new NumericDocValuesField("dv", value));
            }
            modifier.UpdateDocument(new Term("id", id.ToString()), doc);
        }

        /// <exception cref="System.IO.IOException"></exception>
        private void AddDoc(IndexWriter modifier, int id, int value)
        {
            Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                ();
            doc.Add(NewTextField("content", "aaa", Field.Store.NO));
            doc.Add(NewStringField("id", id.ToString(), Field.Store.YES));
            doc.Add(NewStringField("value", value.ToString(), Field.Store.NO));
            if (DefaultCodecSupportsDocValues())
            {
                doc.Add(new NumericDocValuesField("dv", value));
            }
            modifier.AddDocument(doc);
        }

        /// <exception cref="System.IO.IOException"></exception>
        private int GetHitCount(Directory dir, Term term)
        {
            IndexReader reader = DirectoryReader.Open(dir);
            IndexSearcher searcher = NewSearcher(reader);
            int hitCount = searcher.Search(new TermQuery(term), null, 1000).TotalHits;
            reader.Dispose();
            return hitCount;
        }

        [Test]
        public virtual void TestDeletesOnDiskFull()
        {
            DoTestOperationsOnDiskFull(false);
        }

        [Test]
        public virtual void TestUpdatesOnDiskFull()
        {
            DoTestOperationsOnDiskFull(true);
        }

        /// <summary>
        /// Make sure if modifier tries to commit but hits disk full that modifier
        /// remains consistent and usable.
        /// </summary>
        /// <remarks>
        /// Make sure if modifier tries to commit but hits disk full that modifier
        /// remains consistent and usable. Similar to TestIndexReader.testDiskFull().
        /// </remarks>
        /// <exception cref="System.IO.IOException"></exception>
        private void DoTestOperationsOnDiskFull(bool updates)
        {
            Term searchTerm = new Term("content", "aaa");
            int START_COUNT = 157;
            int END_COUNT = 144;
            // First build up a starting index:
            MockDirectoryWrapper startDir = NewMockDirectory();
            // TODO: find the resource leak that only occurs sometimes here.
            startDir.SetNoDeleteOpenFile(false);
            IndexWriter writer = new IndexWriter(startDir, NewIndexWriterConfig(TEST_VERSION_CURRENT
                , new MockAnalyzer(Random(), MockTokenizer.WHITESPACE, false)));
            for (int i = 0; i < 157; i++)
            {
                Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
                d.Add(NewStringField("id", i.ToString(), Field.Store.YES));
                d.Add(NewTextField("content", "aaa " + i, Field.Store.NO));
                if (DefaultCodecSupportsDocValues())
                {
                    d.Add(new NumericDocValuesField("dv", i));
                }
                writer.AddDocument(d);
            }
            writer.Dispose();
            long diskUsage = startDir.SizeInBytes();
            long diskFree = diskUsage + 10;
            IOException err = null;
            bool done = false;
            // Iterate w/ ever increasing free disk space:
            while (!done)
            {
                if (VERBOSE)
                {
                    System.Console.Out.WriteLine("TEST: cycle");
                }
                MockDirectoryWrapper dir = new MockDirectoryWrapper(Random(), new RAMDirectory(startDir
                    , NewIOContext(Random())));
                dir.SetPreventDoubleWrite(false);
                dir.SetAllowRandomFileNotFoundException(false);
                IndexWriter modifier = new IndexWriter(dir, ((IndexWriterConfig)((IndexWriterConfig
                    )NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random(), MockTokenizer
                    .WHITESPACE, false)).SetMaxBufferedDocs(1000)).SetMaxBufferedDeleteTerms(1000)).
                    SetMergeScheduler(new ConcurrentMergeScheduler()));
                ((ConcurrentMergeScheduler)modifier.Config.MergeScheduler).SetSuppressExceptions
                    ();
                // For each disk size, first try to commit against
                // dir that will hit random IOExceptions & disk
                // full; after, give it infinite disk space & turn
                // off random IOExceptions & retry w/ same reader:
                bool success = false;
                for (int x = 0; x < 2; x++)
                {
                    if (VERBOSE)
                    {
                        System.Console.Out.WriteLine("TEST: x=" + x);
                    }
                    double rate = 0.1;
                    double diskRatio = ((double)diskFree) / diskUsage;
                    long thisDiskFree;
                    string testName;
                    if (0 == x)
                    {
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
                            System.Console.Out.WriteLine("\ncycle: " + diskFree + " bytes");
                        }
                        testName = "disk full during reader.close() @ " + thisDiskFree + " bytes";
                        dir.SetRandomIOExceptionRateOnOpen(Random().NextDouble() * 0.01);
                    }
                    else
                    {
                        thisDiskFree = 0;
                        rate = 0.0;
                        if (VERBOSE)
                        {
                            System.Console.Out.WriteLine("\ncycle: same writer: unlimited disk space");
                        }
                        testName = "reader re-use after disk full";
                        dir.SetRandomIOExceptionRateOnOpen(0.0);
                    }
                    dir.SetMaxSizeInBytes(thisDiskFree);
                    dir.SetRandomIOExceptionRate(rate);
                    try
                    {
                        if (0 == x)
                        {
                            int docId = 12;
                            for (int i = 0; i < 13; i++)
                            {
                                if (updates)
                                {
                                    Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document
									{
									    NewStringField("id", i.ToString(), Field.Store.YES),
									    NewTextField("content", "bbb " + i, Field.Store.NO)
									};
                                    if (DefaultCodecSupportsDocValues())
                                    {
                                        d.Add(new NumericDocValuesField("dv", i));
                                    }
                                    modifier.UpdateDocument(new Term("id", docId.ToString()), d);
                                }
                                else
                                {
                                    // deletes
                                    modifier.DeleteDocuments(new Term("id", docId.ToString()));
                                }
                                // modifier.setNorm(docId, "contents", (float)2.0);
                                docId += 12;
                            }
                        }
                        modifier.Dispose();
                        success = true;
                        if (0 == x)
                        {
                            done = true;
                        }
                    }
                    catch (IOException e)
                    {
                        if (VERBOSE)
                        {
                            System.Console.Out.WriteLine("  hit IOException: " + e);
                            e.printStackTrace();
                        }
                        err = e;
                        if (1 == x)
                        {
                            e.printStackTrace();
                            Fail(testName + " hit IOException after disk space was freed up"
                                );
                        }
                    }
                    // prevent throwing a random exception here!!
                    double randomIOExceptionRate = dir.GetRandomIOExceptionRate();
                    long maxSizeInBytes = dir.GetMaxSizeInBytes();
                    dir.SetRandomIOExceptionRate(0.0);
                    dir.SetRandomIOExceptionRateOnOpen(0.0);
                    dir.SetMaxSizeInBytes(0);
                    if (!success)
                    {
                        // Must force the close else the writer can have
                        // open files which cause exc in MockRAMDir.close
                        if (VERBOSE)
                        {
                            System.Console.Out.WriteLine("TEST: now rollback");
                        }
                        modifier.Rollback();
                    }
                    // If the close() succeeded, make sure there are
                    // no unreferenced files.
                    if (success)
                    {
                        TestUtil.CheckIndex(dir);
                        TestIndexWriter.AssertNoUnreferencedFiles(dir, "after writer.close");
                    }
                    dir.SetRandomIOExceptionRate(randomIOExceptionRate);
                    dir.SetMaxSizeInBytes(maxSizeInBytes);
                    // Finally, verify index is not corrupt, and, if
                    // we succeeded, we see all docs changed, and if
                    // we failed, we see either all docs or no docs
                    // changed (transactional semantics):
                    IndexReader newReader = null;
                    try
                    {
                        newReader = DirectoryReader.Open(dir);
                    }
                    catch (IOException e)
                    {
                        e.printStackTrace();
                        Fail(testName + ":exception when creating IndexReader after disk full during close: "
                             + e);
                    }
                    IndexSearcher searcher = NewSearcher(newReader);
                    ScoreDoc[] hits = null;
                    try
                    {
                        hits = searcher.Search(new TermQuery(searchTerm), null, 1000).ScoreDocs;
                    }
                    catch (IOException e)
                    {
                        e.printStackTrace();
                        Fail(testName + ": exception when searching: " + e);
                    }
                    int result2 = hits.Length;
                    if (success)
                    {
                        if (x == 0 && result2 != END_COUNT)
                        {
                            Fail(testName + ": method did not throw exception but hits.length for search on term 'aaa' is "
                                 + result2 + " instead of expected " + END_COUNT);
                        }
                        else
                        {
                            if (x == 1 && result2 != START_COUNT && result2 != END_COUNT)
                            {
                                // It's possible that the first exception was
                                // "recoverable" wrt pending deletes, in which
                                // case the pending deletes are retained and
                                // then re-flushing (with plenty of disk
                                // space) will succeed in flushing the
                                // deletes:
                                Fail(testName + ": method did not throw exception but hits.length for search on term 'aaa' is "
                                     + result2 + " instead of expected " + START_COUNT + " or " + END_COUNT);
                            }
                        }
                    }
                    else
                    {
                        // On hitting exception we still may have added
                        // all docs:
                        if (result2 != START_COUNT && result2 != END_COUNT)
                        {
                            err.printStackTrace();
                            Fail(testName + ": method did throw exception but hits.length for search on term 'aaa' is "
                                 + result2 + " instead of expected " + START_COUNT + " or " + END_COUNT);
                        }
                    }
                    newReader.Dispose();
                    if (result2 == END_COUNT)
                    {
                        break;
                    }
                }
                dir.Dispose();
                modifier.Dispose();
                // Try again with 10 more bytes of free space:
                diskFree += 10;
            }
            startDir.Dispose();
        }

        // This test tests that buffered deletes are cleared when
        // an Exception is hit during flush.
        [Test]
        public virtual void TestErrorAfterApplyDeletes()
        {
            MockDirectoryWrapper.Failure failure = new AnonymousFailure();
            // don't fail during merging
            // Only fail once we are no longer in applyDeletes
            // create a couple of files
            string[] keywords = new string[] { "1", "2" };
            string[] unindexed = new string[] { "Netherlands", "Italy" };
            string[] unstored = new string[] { "Amsterdam has lots of bridges", "Venice has lots of canals"
				 };
            string[] text = new string[] { "Amsterdam", "Venice" };
            MockDirectoryWrapper dir = NewMockDirectory();
            IndexWriter modifier = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
                (TEST_VERSION_CURRENT, new MockAnalyzer(Random(), MockTokenizer.WHITESPACE, false
                )).SetMaxBufferedDeleteTerms(2)).SetReaderPooling(false).SetMergePolicy(NewLogMergePolicy
                ()));
            MergePolicy lmp = modifier.Config.MergePolicy;
            lmp.SetNoCFSRatio(1.0);
            dir.FailOn(failure.Reset());
            FieldType custom1 = new FieldType();
            custom1.Stored = (true);
            for (int i = 0; i < keywords.Length; i++)
            {
                Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				{
				    NewStringField("id", keywords[i], Field.Store.YES),
				    NewField("country", unindexed[i], custom1),
				    NewTextField("contents", unstored[i], Field.Store.NO),
				    NewTextField("city", text[i], Field.Store.YES)
				};
                modifier.AddDocument(doc);
            }
            // flush (and commit if ac)
            if (VERBOSE)
            {
                System.Console.Out.WriteLine("TEST: now full merge");
            }
            modifier.ForceMerge(1);
            if (VERBOSE)
            {
                System.Console.Out.WriteLine("TEST: now commit");
            }
            modifier.Commit();
            // one of the two files hits
            Term term = new Term("city", "Amsterdam");
            int hitCount = GetHitCount(dir, term);
            AreEqual(1, hitCount);
            // open the writer again (closed above)
            // delete the doc
            // max buf del terms is two, so this is buffered
            if (VERBOSE)
            {
                System.Console.Out.WriteLine("TEST: delete term=" + term);
            }
            modifier.DeleteDocuments(term);
            // add a doc (needed for the !ac case; see below)
            // doc remains buffered
            if (VERBOSE)
            {
                System.Console.Out.WriteLine("TEST: add empty doc");
            }
            Lucene.Net.Documents.Document doc_1 = new Lucene.Net.Documents.Document();
            modifier.AddDocument(doc_1);
            // commit the changes, the buffered deletes, and the new doc
            // The failure object will fail on the first write after the del
            // file gets created when processing the buffered delete
            // in the ac case, this will be when writing the new segments
            // files so we really don't need the new doc, but it's harmless
            // a new segments file won't be created but in this
            // case, creation of the cfs file happens next so we
            // need the doc (to test that it's okay that we don't
            // lose deletes if failing while creating the cfs file)
            bool failed = false;
            try
            {
                if (VERBOSE)
                {
                    System.Console.Out.WriteLine("TEST: now commit for failure");
                }
                modifier.Commit();
            }
            catch (IOException)
            {
                // expected
                failed = true;
            }
            IsTrue(failed);
            // The commit above failed, so we need to retry it (which will
            // succeed, because the failure is a one-shot)
            modifier.Commit();
            hitCount = GetHitCount(dir, term);
            // Make sure the delete was successfully flushed:
            AreEqual(0, hitCount);
            modifier.Dispose();
            dir.Dispose();
        }

        private sealed class AnonymousFailure : MockDirectoryWrapper.Failure
        {
            public AnonymousFailure()
            {
                this.sawMaybe = false;
                this.failed = false;
            }

            internal bool sawMaybe;

            internal bool failed;

            internal Thread thread;

            public override MockDirectoryWrapper.Failure Reset()
            {
                this.thread = Thread.CurrentThread;
                this.sawMaybe = false;
                this.failed = false;
                return this;
            }

            /// <exception cref="System.IO.IOException"></exception>
            public override void Eval(MockDirectoryWrapper dir)
            {
                if (Thread.CurrentThread != this.thread)
                {
                    return;
                }
                if (this.sawMaybe && !this.failed)
                {
                    bool seen = false;
                    var trace = new Exception().StackTrace;

                    if (trace.Contains("applyDeletesAndUpdates") || trace.Contains("slowFileExists"))
                    {
                        seen = true;
                    }

                    if (!seen)
                    {
                        this.failed = true;
                        if (LuceneTestCase.VERBOSE)
                        {
                            System.Console.Out.WriteLine("TEST: mock failure: now fail");

                        }
                        throw new IOException("fail after applyDeletes");
                    }
                }
                if (!this.failed)
                {
                    var trace = new Exception().StackTrace;

                    if (trace.Contains("applyDeletesAndUpdates"))
                    {
                        if (LuceneTestCase.VERBOSE)
                        {
                            System.Console.Out.WriteLine("TEST: mock failure: saw applyDeletes");

                        }
                        this.sawMaybe = true;

                    }

                }
            }
        }

        // This test tests that the files created by the docs writer before
        // a segment is written are cleaned up if there's an i/o error
       [Test]
        public virtual void TestErrorInDocsWriterAdd()
        {
            MockDirectoryWrapper.Failure failure = new AnonymousFailure2();
            // create a couple of files
            string[] keywords = { "1", "2" };
            string[] unindexed = { "Netherlands", "Italy" };
            string[] unstored = { "Amsterdam has lots of bridges", "Venice has lots of canals"
            };
            string[] text = new string[] { "Amsterdam", "Venice" };
            MockDirectoryWrapper dir = NewMockDirectory();
            IndexWriter modifier = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
                , new MockAnalyzer(Random(), MockTokenizer.WHITESPACE, false)));
            modifier.Commit();
            dir.FailOn(failure.Reset());
            FieldType custom1 = new FieldType();
            custom1.Stored = (true);
            for (int i = 0; i < keywords.Length; i++)
            {
                Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                {
                    NewStringField("id", keywords[i], Field.Store.YES),
                    NewField("country", unindexed[i], custom1),
                    NewTextField("contents", unstored[i], Field.Store.NO),
                    NewTextField("city", text[i], Field.Store.YES)
                };
                try
                {
                    modifier.AddDocument(doc);
                }
                catch (IOException io)
                {
                    if (VERBOSE)
                    {
                        System.Console.Out.WriteLine("TEST: got expected exc:");
                        io.printStackTrace();
                    }
                    break;
                }
            }
            modifier.Dispose();
            TestIndexWriter.AssertNoUnreferencedFiles(dir, "docsWriter.abort() failed to delete unreferenced files"
                );
            dir.Dispose();
        }

        private sealed class AnonymousFailure2 : MockDirectoryWrapper.Failure
        {
            public AnonymousFailure2()
            {
                this.failed = false;
            }

            internal bool failed;

            public override MockDirectoryWrapper.Failure Reset()
            {
                this.failed = false;
                return this;
            }

            /// <exception cref="System.IO.IOException"></exception>
            public override void Eval(MockDirectoryWrapper dir)
            {
                if (!this.failed)
                {
                    this.failed = true;
                    throw new IOException("fail in add doc");
                }
            }
        }

        [Test]
        public virtual void TestDeleteNullQuery()
        {
            Directory dir = NewDirectory();
            IndexWriter modifier = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT
                , new MockAnalyzer(Random(), MockTokenizer.WHITESPACE, false)));
            for (int i = 0; i < 5; i++)
            {
                AddDoc(modifier, i, 2 * i);
            }
            modifier.DeleteDocuments(new TermQuery(new Term("nada", "nada")));
            modifier.Commit();
            AreEqual(5, modifier.NumDocs);
            modifier.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestDeleteAllSlowly()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random(), dir);
            int NUM_DOCS = AtLeast(1000);
            IList<int> ids = new List<int>(NUM_DOCS);
            for (int id = 0; id < NUM_DOCS; id++)
            {
                ids.Add(id);
            }
            ids.Shuffle(Random());
            foreach (int id_1 in ids)
            {
                Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                {
                    NewStringField("id", string.Empty + id_1, Field.Store.NO)
                };
                w.AddDocument(doc);
            }
            ids.Shuffle(Random());
            int upto = 0;
            while (upto < ids.Count)
            {
                int left = ids.Count - upto;
                int inc = Math.Min(left, Random().NextInt(1, 20));
                int limit = upto + inc;
                while (upto < limit)
                {
                    w.DeleteDocuments(new Term("id", string.Empty + ids[upto++]));
                }
                IndexReader r = w.GetReader();
                AreEqual(NUM_DOCS - upto, r.NumDocs);
                r.Dispose();
            }
            w.Close();
            dir.Dispose();
        }

        [Test]
        public virtual void TestIndexingThenDeleting()
        {
            // TODO: move this test to its own class and just @SuppressCodecs?
            // TODO: is it enough to just use newFSDirectory?
            string fieldFormat = TestUtil.GetPostingsFormat("field");
            AssumeFalse("This test cannot run with Memory codec", fieldFormat.Equals("Memory"
                ));
            AssumeFalse("This test cannot run with SimpleText codec", fieldFormat.Equals("SimpleText"
                ));
            AssumeFalse("This test cannot run with Direct codec", fieldFormat.Equals("Direct"
                ));
            Random r = Random();
            Directory dir = NewDirectory();
            // note this test explicitly disables payloads
            Analyzer analyzer = new _Analyzer_994();
            IndexWriter w = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).SetRAMBufferSizeMB(1.0).SetMaxBufferedDocs
                (IndexWriterConfig.DISABLE_AUTO_FLUSH).SetMaxBufferedDeleteTerms(IndexWriterConfig
                .DISABLE_AUTO_FLUSH)));
            Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
            {
                NewTextField("field", "go 1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 16 17 18 19 20"
                    , Field.Store.NO)
            };
            int num = AtLeast(3);
            for (int iter = 0; iter < num; iter++)
            {
                int count = 0;
                bool doIndexing = r.NextBoolean();
                if (VERBOSE)
                {
                    System.Console.Out.WriteLine("TEST: iter doIndexing=" + doIndexing);
                }
                if (doIndexing)
                {
                    // Add docs until a flush is triggered
                    int startFlushCount = w.FlushCount;
                    while (w.FlushCount == startFlushCount)
                    {
                        w.AddDocument(doc);
                        count++;
                    }
                }
                else
                {
                    // Delete docs until a flush is triggered
                    int startFlushCount = w.FlushCount;
                    while (w.FlushCount == startFlushCount)
                    {
                        w.DeleteDocuments(new Term("foo", string.Empty + count));
                        count++;
                    }
                }
                AssertTrue("flush happened too quickly during " + (doIndexing ?
                    "indexing" : "deleting") + " count=" + count, count > 2500);
            }
            w.Dispose();
            dir.Dispose();
        }

        private sealed class _Analyzer_994 : Analyzer
        {
            public override Analyzer.TokenStreamComponents CreateComponents(string fieldName
                , TextReader reader)
            {
                return new Analyzer.TokenStreamComponents(new MockTokenizer(reader, MockTokenizer
                    .WHITESPACE, true));
            }
        }

        // LUCENE-3340: make sure deletes that we don't apply
        // during flush (ie are just pushed into the stream) are
        // in fact later flushed due to their RAM usage:
        [Test]
        public virtual void TestFlushPushedDeletesByRAM()
        {
            Directory dir = NewDirectory();
            // Cannot use RandomIndexWriter because we don't want to
            // ever call commit() for this test:
            // note: tiny rambuffer used, as with a 1MB buffer the test is too slow (flush @ 128,999)
            IndexWriter w = new IndexWriter(dir, ((IndexWriterConfig)((IndexWriterConfig)NewIndexWriterConfig
                (TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetRAMBufferSizeMB(0.1f)).SetMaxBufferedDocs
                (1000)).SetMergePolicy(NoMergePolicy.NO_COMPOUND_FILES).SetReaderPooling(false));
            int count = 0;
            while (true)
            {
                Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                    ();
                doc.Add(new StringField("id", count + string.Empty, Field.Store.NO));
                Term delTerm;
                if (count == 1010)
                {
                    // This is the only delete that applies
                    delTerm = new Term("id", string.Empty + 0);
                }
                else
                {
                    // These get buffered, taking up RAM, but delete
                    // nothing when applied:
                    delTerm = new Term("id", "x" + count);
                }
                w.UpdateDocument(delTerm, doc);
                // Eventually segment 0 should get a del docs:
                // TODO: fix this test
                if (SlowFileExists(dir, "_0_1.del") || SlowFileExists(dir, "_0_1.liv"))
                {
                    if (VERBOSE)
                    {
                        System.Console.Out.WriteLine("TEST: deletes created @ count=" + count);
                    }
                    break;
                }
                count++;
                // Today we applyDeletes @ count=21553; even if we make
                // sizable improvements to RAM efficiency of buffered
                // del term we're unlikely to go over 100K:
                if (count > 100000)
                {
                    Fail("delete's were not applied");
                }
            }
            w.Dispose();
            dir.Dispose();
        }

        // LUCENE-3340: make sure deletes that we don't apply
        // during flush (ie are just pushed into the stream) are
        // in fact later flushed due to their RAM usage:
        [Test]
        public virtual void TestFlushPushedDeletesByCount()
        {
            Directory dir = NewDirectory();
            // Cannot use RandomIndexWriter because we don't want to
            // ever call commit() for this test:
            int flushAtDelCount = AtLeast(1020);
            IndexWriter w = new IndexWriter(dir, ((IndexWriterConfig)((IndexWriterConfig)((IndexWriterConfig
                )NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDeleteTerms
                (flushAtDelCount)).SetMaxBufferedDocs(1000)).SetRAMBufferSizeMB(IndexWriterConfig
                .DISABLE_AUTO_FLUSH)).SetMergePolicy(NoMergePolicy.NO_COMPOUND_FILES).SetReaderPooling
                (false));
            int count = 0;
            while (true)
            {
                Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                    ();
                doc.Add(new StringField("id", count + string.Empty, Field.Store.NO));
                Term delTerm;
                if (count == 1010)
                {
                    // This is the only delete that applies
                    delTerm = new Term("id", string.Empty + 0);
                }
                else
                {
                    // These get buffered, taking up RAM, but delete
                    // nothing when applied:
                    delTerm = new Term("id", "x" + count);
                }
                w.UpdateDocument(delTerm, doc);
                // Eventually segment 0 should get a del docs:
                // TODO: fix this test
                if (SlowFileExists(dir, "_0_1.del") || SlowFileExists(dir, "_0_1.liv"))
                {
                    break;
                }
                count++;
                if (count > flushAtDelCount)
                {
                    Fail("delete's were not applied at count=" + flushAtDelCount
                        );
                }
            }
            w.Dispose();
            dir.Dispose();
        }

        // Make sure buffered (pushed) deletes don't use up so
        // much RAM that it forces long tail of tiny segments:
        /// <exception cref="System.Exception"></exception>
        //[LuceneTestCase.Nightly]
        [Test]
        public virtual void TestApplyDeletesOnFlush()
        {
            Directory dir = NewDirectory();
            // Cannot use RandomIndexWriter because we don't want to
            // ever call commit() for this test:
            AtomicInteger docsInSegment = new AtomicInteger();
            AtomicBoolean closing = new AtomicBoolean();
            AtomicBoolean sawAfterFlush = new AtomicBoolean();
            IndexWriter w = new AnonymousIndexWriter(docsInSegment, closing, sawAfterFlush, dir,
                ((IndexWriterConfig)((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
                , new MockAnalyzer(Random())).SetRAMBufferSizeMB(0.5)).SetMaxBufferedDocs(-1)).SetMergePolicy
                (NoMergePolicy.NO_COMPOUND_FILES).SetReaderPooling(false));
            int id = 0;
            while (true)
            {
                StringBuilder sb = new StringBuilder();
                for (int termIDX = 0; termIDX < 100; termIDX++)
                {
                    sb.Append(' ').Append(TestUtil.RandomRealisticUnicodeString(Random()));
                }
                if (id == 500)
                {
                    w.DeleteDocuments(new Term("id", "0"));
                }
                Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                    ();
                doc.Add(NewStringField("id", string.Empty + id, Field.Store.NO));
                doc.Add(NewTextField("body", sb.ToString(), Field.Store.NO));
                w.UpdateDocument(new Term("id", string.Empty + id), doc);
                docsInSegment.IncrementAndGet();
                // TODO: fix this test
                if (SlowFileExists(dir, "_0_1.del") || SlowFileExists(dir, "_0_1.liv"))
                {
                    if (VERBOSE)
                    {
                        System.Console.Out.WriteLine("TEST: deletes created @ id=" + id);
                    }
                    break;
                }
                id++;
            }
            closing.Set(true);
            IsTrue(sawAfterFlush.Get());
            w.Dispose();
            dir.Dispose();
        }

        private sealed class AnonymousIndexWriter : IndexWriter
        {
            public AnonymousIndexWriter(AtomicInteger docsInSegment, AtomicBoolean closing, AtomicBoolean
                 sawAfterFlush, Directory baseArg1, IndexWriterConfig baseArg2)
                : base(baseArg1,
                    baseArg2)
            {
                this.docsInSegment = docsInSegment;
                this.closing = closing;
                this.sawAfterFlush = sawAfterFlush;
            }

            protected internal override void DoAfterFlush()
            {
                AssertTrue("only " + docsInSegment.Get() + " in segment", closing
                    .Get() || docsInSegment.Get() >= 7);
                docsInSegment.Set(0);
                sawAfterFlush.Set(true);
            }

            private readonly AtomicInteger docsInSegment;

            private readonly AtomicBoolean closing;

            private readonly AtomicBoolean sawAfterFlush;
        }

        // LUCENE-4455
        [Test]
        public virtual void TestDeletesCheckIndexOutput()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
                (Random()));
            iwc.SetMaxBufferedDocs(2);
            IndexWriter w = new IndexWriter(dir, (IndexWriterConfig) iwc.Clone());
            Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                ();
            doc.Add(NewField("field", "0", StringField.TYPE_NOT_STORED));
            w.AddDocument(doc);
            doc = new Lucene.Net.Documents.Document();
            doc.Add(NewField("field", "1", StringField.TYPE_NOT_STORED));
            w.AddDocument(doc);
            w.Commit();
            AreEqual(1, w.SegmentCount);
            w.DeleteDocuments(new Term("field", "0"));
            w.Commit();
            AreEqual(1, w.SegmentCount);
            w.Dispose();
            var bos = new MemoryStream(1024);
            CheckIndex checker = new CheckIndex(dir);
            checker.SetInfoStream(new StreamWriter(bos, Encoding.UTF8), false);
            CheckIndex.Status indexStatus = checker.CheckIndex_Renamed_Method(null);
            IsTrue(indexStatus.clean);
            string s = Encoding.UTF8.GetString(bos.GetBuffer());
            // Segment should have deletions:
            IsTrue(s.Contains("has deletions"));
            w = new IndexWriter(dir, (IndexWriterConfig) iwc.Clone());
            w.ForceMerge(1);
            w.Dispose();
            bos = new MemoryStream(1024);
            checker.SetInfoStream(new StreamWriter(bos, Encoding.UTF8), false);
            indexStatus = checker.CheckIndex_Renamed_Method(null);
            IsTrue(indexStatus.clean);
            s = Encoding.UTF8.GetString(bos.GetBuffer());
            IsFalse(s.Contains("has deletions"));
            dir.Dispose();
        }

        [Test]
        public virtual void TestTryDeleteDocument()
        {
            Directory d = NewDirectory();
            IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
                (Random()));
            IndexWriter w = new IndexWriter(d, iwc);
            Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                ();
            w.AddDocument(doc);
            w.AddDocument(doc);
            w.AddDocument(doc);
            w.Dispose();
            iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            iwc.SetOpenMode(IndexWriterConfig.OpenMode.APPEND);
            w = new IndexWriter(d, iwc);
            IndexReader r = DirectoryReader.Open(w, false);
            IsTrue(w.TryDeleteDocument(r, 1));
            IsTrue(w.TryDeleteDocument(((AtomicReader)r.Leaves[0].Reader), 0));
            r.Dispose();
            w.Dispose();
            r = DirectoryReader.Open(d);
            AreEqual(2, r.NumDeletedDocs);
            IsNotNull(MultiFields.GetLiveDocs(r));
            r.Dispose();
            d.Dispose();
        }
    }
}
