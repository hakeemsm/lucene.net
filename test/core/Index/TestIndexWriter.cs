using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Documents;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Support;
using Lucene.Net.TestFramework;
using Lucene.Net.Codecs;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.TestFramework.Util;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using Lucene.Net.Util.Packed;
using NUnit.Framework;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Test.Index
{
    [TestFixture]
    public class TestIndexWriter : LuceneTestCase
    {
        private static readonly FieldType storedTextType = new FieldType(TextField.TYPE_NOT_STORED
            );

        [Test]
        public virtual void TestDocCount()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = null;
            IndexReader reader = null;
            int i;
            long savedWriteLockTimeout = IndexWriterConfig.DefaultWriteLockTimeout;
            try
            {
                IndexWriterConfig.DefaultWriteLockTimeout = (2000);
                AssertEquals(2000, IndexWriterConfig.DefaultWriteLockTimeout);
                writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
                    (Random())));
            }
            finally
            {
                IndexWriterConfig.DefaultWriteLockTimeout = (savedWriteLockTimeout);
            }
            // add 100 documents
            for (i = 0; i < 100; i++)
            {
                AddDocWithIndex(writer, i);
            }
            AssertEquals(100, writer.MaxDoc);
            writer.Dispose();
            // delete 40 documents
            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
                (Random())).SetMergePolicy(NoMergePolicy.NO_COMPOUND_FILES));
            for (i = 0; i < 40; i++)
            {
                writer.DeleteDocuments(new Term("id", string.Empty + i));
            }
            writer.Dispose();
            reader = DirectoryReader.Open(dir);
            AssertEquals(60, reader.NumDocs);
            reader.Dispose();
            // merge the index down and check that the new doc count is correct
            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
                (Random())));
            AssertEquals(60, writer.NumDocs);
            writer.ForceMerge(1);
            AssertEquals(60, writer.MaxDoc);
            AssertEquals(60, writer.NumDocs);
            writer.Dispose();
            // check that the index reader gives the same numbers.
            reader = DirectoryReader.Open(dir);
            AssertEquals(60, reader.MaxDoc);
            AssertEquals(60, reader.NumDocs);
            reader.Dispose();
            // make sure opening a new index for create over
            // this existing one works correctly:
            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
                (Random())).SetOpenMode(IndexWriterConfig.OpenMode.CREATE));
            AssertEquals(0, writer.MaxDoc);
            AssertEquals(0, writer.NumDocs);
            writer.Dispose();
            dir.Dispose();
        }

        /// <exception cref="System.IO.IOException"></exception>
        internal static void AddDoc(IndexWriter writer)
        {
            var doc = new Lucene.Net.Documents.Document
			{
			    NewTextField("content", "aaa", Field.Store.NO)
			};
            writer.AddDocument(doc);
        }

        /// <exception cref="System.IO.IOException"></exception>
        internal static void AddDocWithIndex(IndexWriter writer, int index)
        {
            var doc = new Lucene.Net.Documents.Document
			{
			    NewField("content", "aaa " + index, storedTextType),
			    NewField("id", string.Empty + index, storedTextType)
			};
            writer.AddDocument(doc);
        }

        /// <exception cref="System.IO.IOException"></exception>
        public static void AssertNoUnreferencedFiles(Directory dir, string message)
        {
            string[] startFiles = dir.ListAll();
            new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
                (Random()))).Rollback();
            string[] endFiles = dir.ListAll();
            Arrays.Sort(startFiles);
            Arrays.Sort(endFiles);
            if (!Arrays.Equals(startFiles, endFiles))
            {
                Fail(message + ": before delete:\n    " + ArrayToString(startFiles
                    ) + "\n  after delete:\n    " + ArrayToString(endFiles));
            }
        }

        internal static string ArrayToString(string[] l)
        {
            string s = string.Empty;
            for (int i = 0; i < l.Length; i++)
            {
                if (i > 0)
                {
                    s += "\n    ";
                }
                s += l[i];
            }
            return s;
        }

        // Make sure we can open an index for create even when a
        // reader holds it open (this fails pre lock-less
        // commits on windows):
        [Test]
        public virtual void TestCreateWithReader()
        {
            Directory dir = NewDirectory();
            // add one document & close writer
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
                , new MockAnalyzer(Random())));
            AddDoc(writer);
            writer.Dispose();
            // now open reader:
            IndexReader reader = DirectoryReader.Open(dir);
            AssertEquals("should be one document", reader.NumDocs, 1);
            // now open index for create:
            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
                (Random())).SetOpenMode(IndexWriterConfig.OpenMode.CREATE));
            AssertEquals("should be zero documents", writer.MaxDoc, 0);
            AddDoc(writer);
            writer.Dispose();
            AssertEquals("should be one document", reader.NumDocs, 1);
            IndexReader reader2 = DirectoryReader.Open(dir);
            AssertEquals("should be one document", reader2.NumDocs, 1);
            reader.Dispose();
            reader2.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestChangesAfterClose()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = null;
            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
                (Random())));
            AddDoc(writer);
            // close
            writer.Dispose();
            try
            {
                AddDoc(writer);
                Fail("did not hit AlreadyClosedException");
            }
            catch (AlreadyClosedException)
            {
            }
            // expected
            dir.Dispose();
        }

        [Test]
        public virtual void TestIndexNoDocuments()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
                , new MockAnalyzer(Random())));
            writer.Commit();
            writer.Dispose();
            IndexReader reader = DirectoryReader.Open(dir);
            AssertEquals(0, reader.MaxDoc);
            AssertEquals(0, reader.NumDocs);
            reader.Dispose();
            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
                (Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND));
            writer.Commit();
            writer.Dispose();
            reader = DirectoryReader.Open(dir);
            AssertEquals(0, reader.MaxDoc);
            AssertEquals(0, reader.NumDocs);
            reader.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestManyFields()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
                (TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(10)));
            for (int j = 0; j < 100; j++)
            {
                var doc = new Lucene.Net.Documents.Document
				{
				    NewField("a" + j, "aaa" + j, storedTextType),
				    NewField("b" + j, "aaa" + j, storedTextType),
				    NewField("c" + j, "aaa" + j, storedTextType),
				    NewField("d" + j, "aaa", storedTextType),
				    NewField("e" + j, "aaa", storedTextType),
				    NewField("f" + j, "aaa", storedTextType)
				};
                writer.AddDocument(doc);
            }
            writer.Dispose();
            IndexReader reader = DirectoryReader.Open(dir);
            AssertEquals(100, reader.MaxDoc);
            AssertEquals(100, reader.NumDocs);
            for (int j_1 = 0; j_1 < 100; j_1++)
            {
                AssertEquals(1, reader.DocFreq(new Term("a" + j_1, "aaa" + j_1
                    )));
                AssertEquals(1, reader.DocFreq(new Term("b" + j_1, "aaa" + j_1
                    )));
                AssertEquals(1, reader.DocFreq(new Term("c" + j_1, "aaa" + j_1
                    )));
                AssertEquals(1, reader.DocFreq(new Term("d" + j_1, "aaa")));
                AssertEquals(1, reader.DocFreq(new Term("e" + j_1, "aaa")));
                AssertEquals(1, reader.DocFreq(new Term("f" + j_1, "aaa")));
            }
            reader.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestSmallRAMBuffer()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
                (TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetRAMBufferSizeMB(0.000001))
                .SetMergePolicy(NewLogMergePolicy(10)));
            int lastNumFile = dir.ListAll().Length;
            for (int j = 0; j < 9; j++)
            {
                Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                    ();
                doc.Add(NewField("field", "aaa" + j, storedTextType));
                writer.AddDocument(doc);
                int numFile = dir.ListAll().Length;
                // Verify that with a tiny RAM buffer we see new
                // segment after every doc
                AssertTrue(numFile > lastNumFile);
                lastNumFile = numFile;
            }
            writer.Dispose();
            dir.Dispose();
        }

        // Make sure it's OK to change RAM buffer size and
        // maxBufferedDocs in a write session
        [Test]
        public virtual void TestChangingRAMBuffer()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
                , new MockAnalyzer(Random())));
            writer.Config.SetMaxBufferedDocs(10);
            writer.Config.SetRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH);
            int lastFlushCount = -1;
            for (int j = 1; j < 52; j++)
            {
                Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                    ();
                doc.Add(new Field("field", "aaa" + j, storedTextType));
                writer.AddDocument(doc);
                TestUtil.SyncConcurrentMerges(writer);
                int flushCount = writer.FlushCount;
                if (j == 1)
                {
                    lastFlushCount = flushCount;
                }
                else
                {
                    if (j < 10)
                    {
                        // No new files should be created
                        AssertEquals(flushCount, lastFlushCount);
                    }
                    else
                    {
                        if (10 == j)
                        {
                            AssertTrue(flushCount > lastFlushCount);
                            lastFlushCount = flushCount;
                            writer.Config.SetRAMBufferSizeMB(0.000001);
                            writer.Config.SetMaxBufferedDocs(IndexWriterConfig.DISABLE_AUTO_FLUSH);
                        }
                        else
                        {
                            if (j < 20)
                            {
                                AssertTrue(flushCount > lastFlushCount);
                                lastFlushCount = flushCount;
                            }
                            else
                            {
                                if (20 == j)
                                {
                                    writer.Config.SetRAMBufferSizeMB(16);
                                    writer.Config.SetMaxBufferedDocs(IndexWriterConfig.DISABLE_AUTO_FLUSH);
                                    lastFlushCount = flushCount;
                                }
                                else
                                {
                                    if (j < 30)
                                    {
                                        AssertEquals(flushCount, lastFlushCount);
                                    }
                                    else
                                    {
                                        if (30 == j)
                                        {
                                            writer.Config.SetRAMBufferSizeMB(0.000001);
                                            writer.Config.SetMaxBufferedDocs(IndexWriterConfig.DISABLE_AUTO_FLUSH);
                                        }
                                        else
                                        {
                                            if (j < 40)
                                            {
                                                AssertTrue(flushCount > lastFlushCount);
                                                lastFlushCount = flushCount;
                                            }
                                            else
                                            {
                                                if (40 == j)
                                                {
                                                    writer.Config.SetMaxBufferedDocs(10);
                                                    writer.Config.SetRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH);
                                                    lastFlushCount = flushCount;
                                                }
                                                else
                                                {
                                                    if (j < 50)
                                                    {
                                                        AssertEquals(flushCount, lastFlushCount);
                                                        writer.Config.SetMaxBufferedDocs(10);
                                                        writer.Config.SetRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH);
                                                    }
                                                    else
                                                    {
                                                        if (50 == j)
                                                        {
                                                            AssertTrue(flushCount > lastFlushCount);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            writer.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestChangingRAMBuffer2()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
                , new MockAnalyzer(Random())));
            writer.Config.SetMaxBufferedDocs(10);
            writer.Config.SetMaxBufferedDeleteTerms(10);
            writer.Config.SetRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH);
            for (int j = 1; j < 52; j++)
            {
                Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                    ();
                doc.Add(new Field("field", "aaa" + j, storedTextType));
                writer.AddDocument(doc);
            }
            int lastFlushCount = -1;
            for (int j_1 = 1; j_1 < 52; j_1++)
            {
                writer.DeleteDocuments(new Term("field", "aaa" + j_1));
                TestUtil.SyncConcurrentMerges(writer);
                int flushCount = writer.FlushCount;
                if (j_1 == 1)
                {
                    lastFlushCount = flushCount;
                }
                else
                {
                    if (j_1 < 10)
                    {
                        // No new files should be created
                        AssertEquals(flushCount, lastFlushCount);
                    }
                    else
                    {
                        if (10 == j_1)
                        {
                            AssertTrue(string.Empty + j_1, flushCount > lastFlushCount);
                            lastFlushCount = flushCount;
                            writer.Config.SetRAMBufferSizeMB(0.000001);
                            writer.Config.SetMaxBufferedDeleteTerms(1);
                        }
                        else
                        {
                            if (j_1 < 20)
                            {
                                AssertTrue(flushCount > lastFlushCount);
                                lastFlushCount = flushCount;
                            }
                            else
                            {
                                if (20 == j_1)
                                {
                                    writer.Config.SetRAMBufferSizeMB(16);
                                    writer.Config.SetMaxBufferedDeleteTerms(IndexWriterConfig.DISABLE_AUTO_FLUSH
                                        );
                                    lastFlushCount = flushCount;
                                }
                                else
                                {
                                    if (j_1 < 30)
                                    {
                                        AssertEquals(flushCount, lastFlushCount);
                                    }
                                    else
                                    {
                                        if (30 == j_1)
                                        {
                                            writer.Config.SetRAMBufferSizeMB(0.000001);
                                            writer.Config.SetMaxBufferedDeleteTerms(IndexWriterConfig.DISABLE_AUTO_FLUSH
                                                );
                                            writer.Config.SetMaxBufferedDeleteTerms(1);
                                        }
                                        else
                                        {
                                            if (j_1 < 40)
                                            {
                                                AssertTrue(flushCount > lastFlushCount);
                                                lastFlushCount = flushCount;
                                            }
                                            else
                                            {
                                                if (40 == j_1)
                                                {
                                                    writer.Config.SetMaxBufferedDeleteTerms(10);
                                                    writer.Config.SetRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH);
                                                    lastFlushCount = flushCount;
                                                }
                                                else
                                                {
                                                    if (j_1 < 50)
                                                    {
                                                        AssertEquals(flushCount, lastFlushCount);
                                                        writer.Config.SetMaxBufferedDeleteTerms(10);
                                                        writer.Config.SetRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH);
                                                    }
                                                    else
                                                    {
                                                        if (50 == j_1)
                                                        {
                                                            AssertTrue(flushCount > lastFlushCount);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            writer.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestDiverseDocs()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
                (TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetRAMBufferSizeMB(0.5)));
            int n = AtLeast(1);
            for (int i = 0; i < n; i++)
            {
                // First, docs where every term is unique (heavy on
                // Posting instances)
                for (int j = 0; j < 100; j++)
                {
                    Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                        ();
                    for (int k = 0; k < 100; k++)
                    {
                        doc.Add(NewField("field", Random().Next().ToString(), storedTextType
                            ));
                    }
                    writer.AddDocument(doc);
                }
                // Next, many single term docs where only one term
                // occurs (heavy on byte blocks)
                for (int j_1 = 0; j_1 < 100; j_1++)
                {
                    Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                        ();
                    doc.Add(NewField("field", "aaa aaa aaa aaa aaa aaa aaa aaa aaa aaa", storedTextType
                        ));
                    writer.AddDocument(doc);
                }
                // Next, many single term docs where only one term
                // occurs but the terms are very long (heavy on
                // char[] arrays)
                for (int j = 0; j < 100; j++)
                {
                    StringBuilder b = new StringBuilder();
                    string x = j + ".";
                    for (int k = 0; k < 1000; k++)
                    {
                        b.Append(x);
                    }
                    string longTerm = b.ToString();
                    Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                        ();
                    doc.Add(NewField("field", longTerm, storedTextType));
                    writer.AddDocument(doc);
                }
            }
            writer.Dispose();
            IndexReader reader = DirectoryReader.Open(dir);
            IndexSearcher searcher = NewSearcher(reader);
            int TotalHits = searcher.Search(new TermQuery(new Term("field", "aaa")), null, 1)
                .TotalHits;
            AssertEquals(n * 100, TotalHits);
            reader.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestEnablingNorms()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
                (TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(10)));
            // Enable norms for only 1 doc, pre flush
            FieldType customType = new FieldType(TextField.TYPE_STORED);
            customType.OmitNorms = (true);
            for (int j = 0; j < 10; j++)
            {
                Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                    ();
                Field f = null;
                if (j != 8)
                {
                    f = NewField("field", "aaa", customType);
                }
                else
                {
                    f = NewField("field", "aaa", storedTextType);
                }
                doc.Add(f);
                writer.AddDocument(doc);
            }
            writer.Dispose();
            Term searchTerm = new Term("field", "aaa");
            IndexReader reader = DirectoryReader.Open(dir);
            IndexSearcher searcher = NewSearcher(reader);
            ScoreDoc[] hits = searcher.Search(new TermQuery(searchTerm), null, 1000).ScoreDocs;
            AssertEquals(10, hits.Length);
            reader.Dispose();
            writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
                , new MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode.CREATE).SetMaxBufferedDocs
                (10)));
            // Enable norms for only 1 doc, post flush
            for (int j_1 = 0; j_1 < 27; j_1++)
            {
                Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                    ();
                Field f = null;
                if (j_1 != 26)
                {
                    f = NewField("field", "aaa", customType);
                }
                else
                {
                    f = NewField("field", "aaa", storedTextType);
                }
                doc.Add(f);
                writer.AddDocument(doc);
            }
            writer.Dispose();
            reader = DirectoryReader.Open(dir);
            searcher = NewSearcher(reader);
            hits = searcher.Search(new TermQuery(searchTerm), null, 1000).ScoreDocs;
            AssertEquals(27, hits.Length);
            reader.Dispose();
            reader = DirectoryReader.Open(dir);
            reader.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestHighFreqTerm()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
                (TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetRAMBufferSizeMB(0.01)));
            // Massive doc that has 128 K a's
            StringBuilder b = new StringBuilder(1024 * 1024);
            for (int i = 0; i < 4096; i++)
            {
                b.Append(" a a a a a a a a");
                b.Append(" a a a a a a a a");
                b.Append(" a a a a a a a a");
                b.Append(" a a a a a a a a");
            }
            Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                ();
            FieldType customType = new FieldType(TextField.TYPE_STORED);
            customType.StoreTermVectors = true;
            customType.StoreTermVectorPositions = true;
            customType.StoreTermVectorOffsets = true;
            doc.Add(NewField("field", b.ToString(), customType));
            writer.AddDocument(doc);
            writer.Dispose();
            IndexReader reader = DirectoryReader.Open(dir);
            AssertEquals(1, reader.MaxDoc);
            AssertEquals(1, reader.NumDocs);
            Term t = new Term("field", "a");
            AssertEquals(1, reader.DocFreq(t));
            DocsEnum td = TestUtil.Docs(Random(), reader, "field", new BytesRef("a"), MultiFields
                .GetLiveDocs(reader), null, DocsEnum.FLAG_FREQS);
            td.NextDoc();
            AssertEquals(128 * 1024, td.Freq);
            reader.Dispose();
            dir.Dispose();
        }

        // Make sure that a Directory implementation that does
        // not use LockFactory at all (ie overrides makeLock and
        // implements its own private locking) works OK.  This
        // was raised on java-dev as loss of backwards
        // compatibility.
        [Test]
        public virtual void TestNullLockFactory()
        {
            Directory dir = new AnonymousMockDirectoryWrapper(this, new RAMDirectory());
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
                , new MockAnalyzer(Random())));
            for (int i = 0; i < 100; i++)
            {
                AddDoc(writer);
            }
            writer.Dispose();
            Term searchTerm = new Term("content", "aaa");
            IndexReader reader = DirectoryReader.Open(dir);
            IndexSearcher searcher = NewSearcher(reader);
            ScoreDoc[] hits = searcher.Search(new TermQuery(searchTerm), null, 1000).ScoreDocs;
            AssertEquals("did not get right number of hits", 100, hits.Length
                );
            reader.Dispose();
            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
                (Random())).SetOpenMode(IndexWriterConfig.OpenMode.CREATE));
            writer.Dispose();
            dir.Dispose();
        }

        internal sealed class AnonymousMockDirectoryWrapper : MockDirectoryWrapper
        {
            private LockFactory myLockFactory;

            protected internal AnonymousMockDirectoryWrapper(TestIndexWriter _enclosing, Directory delegate_)
                : base(LuceneTestCase
                    .Random(), delegate_)
            {
                this._enclosing = _enclosing;
                this.lockFactory = null;
                this.myLockFactory = new SingleInstanceLockFactory();
            }

            public override Lock MakeLock(string name)
            {
                return this.myLockFactory.MakeLock(name);
            }

            private readonly TestIndexWriter _enclosing;
        }

        [Test]
        public virtual void TestFlushWithNoMerging()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
                (TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2)).SetMergePolicy
                (NewLogMergePolicy(10)));
            Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                ();
            FieldType customType = new FieldType(TextField.TYPE_STORED);
            customType.StoreTermVectors = true;
            customType.StoreTermVectorPositions = true;
            customType.StoreTermVectorOffsets = true;
            doc.Add(NewField("field", "aaa", customType));
            for (int i = 0; i < 19; i++)
            {
                writer.AddDocument(doc);
            }
            writer.Flush(false, true);
            writer.Dispose();
            SegmentInfos sis = new SegmentInfos();
            sis.Read(dir);
            // Since we flushed w/o allowing merging we should now
            // have 10 segments
            AssertEquals(10, sis.Count);
            dir.Dispose();
        }

        // Make sure we can flush segment w/ norms, then add
        // empty doc (no norms) and flush
        [Test]
        public virtual void TestEmptyDocAfterFlushingRealDoc()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
                , new MockAnalyzer(Random())));
            Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                ();
            FieldType customType = new FieldType(TextField.TYPE_STORED);
            customType.StoreTermVectors = true;
            customType.StoreTermVectorPositions = true;
            customType.StoreTermVectorOffsets = true;
            doc.Add(NewField("field", "aaa", customType));
            writer.AddDocument(doc);
            writer.Commit();
            if (VERBOSE)
            {
                System.Console.Out.WriteLine("\nTEST: now add empty doc");
            }
            writer.AddDocument(new Lucene.Net.Documents.Document());
            writer.Dispose();
            IndexReader reader = DirectoryReader.Open(dir);
            AssertEquals(2, reader.NumDocs);
            reader.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Test that no NullPointerException will be raised,
        /// when adding one document with a single, empty field
        /// and term vectors enabled.
        /// </summary>
        /// <remarks>
        /// Test that no NullPointerException will be raised,
        /// when adding one document with a single, empty field
        /// and term vectors enabled.
        /// </remarks>
        [Test]
        public virtual void TestBadSegment()
        {
            Directory dir = NewDirectory();
            IndexWriter iw = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT,
                new MockAnalyzer(Random())));
            Lucene.Net.Documents.Document document = new Lucene.Net.Documents.Document
                ();
            FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
            customType.StoreTermVectors = true;
            document.Add(NewField("tvtest", string.Empty, customType));
            iw.AddDocument(document);
            iw.Dispose();
            dir.Dispose();
        }

        // LUCENE-1036
        [Test]
        public virtual void TestMaxThreadPriority()
        {
            var pri = Thread.CurrentThread.Priority;
            try
            {
                Directory dir = NewDirectory();
                IndexWriterConfig conf = ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
                    , new MockAnalyzer(Random())).SetMaxBufferedDocs(2)).SetMergePolicy(NewLogMergePolicy
                    ());
                ((LogMergePolicy)conf.MergePolicy).MergeFactor = (2);
                IndexWriter iw = new IndexWriter(dir, conf);
                Lucene.Net.Documents.Document document = new Lucene.Net.Documents.Document
                    ();
                FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
                customType.StoreTermVectors = true;
                document.Add(NewField("tvtest", "a b c", customType));
                Thread.CurrentThread.Priority = ThreadPriority.Highest;
                for (int i = 0; i < 4; i++)
                {
                    iw.AddDocument(document);
                }
                iw.Dispose();
                dir.Dispose();
            }
            finally
            {
                Thread.CurrentThread.Priority = (pri);
            }
        }

        [Test]
        public virtual void TestVariableSchema()
        {
            Directory dir = NewDirectory();
            for (int i = 0; i < 20; i++)
            {
                if (VERBOSE)
                {
                    System.Console.Out.WriteLine("TEST: iter=" + i);
                }
                IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
                    (TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2)).SetMergePolicy
                    (NewLogMergePolicy()));
                //LogMergePolicy lmp = (LogMergePolicy) writer.getConfig().getMergePolicy();
                //lmp.setMergeFactor(2);
                //lmp.setNoCFSRatio(0.0);
                Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                    ();
                string contents = "aa bb cc dd ee ff gg hh ii jj kk";
                FieldType customType = new FieldType(TextField.TYPE_STORED);
                FieldType type = null;
                if (i == 7)
                {
                    // Add empty docs here
                    doc.Add(NewTextField("content3", string.Empty, Field.Store.NO));
                }
                else
                {
                    if (i % 2 == 0)
                    {
                        doc.Add(NewField("content4", contents, customType));
                        type = customType;
                    }
                    else
                    {
                        type = TextField.TYPE_NOT_STORED;
                    }
                    doc.Add(NewTextField("content1", contents, Field.Store.NO));
                    doc.Add(NewField("content3", string.Empty, customType));
                    doc.Add(NewField("content5", string.Empty, type));
                }
                for (int j = 0; j < 4; j++)
                {
                    writer.AddDocument(doc);
                }
                writer.Dispose();
                if (0 == i % 4)
                {
                    writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
                        (Random())));
                    //LogMergePolicy lmp2 = (LogMergePolicy) writer.getConfig().getMergePolicy();
                    //lmp2.setNoCFSRatio(0.0);
                    writer.ForceMerge(1);
                    writer.Dispose();
                }
            }
            dir.Dispose();
        }

        // LUCENE-1084: test unlimited field length
        [Test]
        public virtual void TestUnlimitedMaxFieldLength()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
                , new MockAnalyzer(Random())));
            Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                ();
            StringBuilder b = new StringBuilder();
            for (int i = 0; i < 10000; i++)
            {
                b.Append(" a");
            }
            b.Append(" x");
            doc.Add(NewTextField("field", b.ToString(), Field.Store.NO));
            writer.AddDocument(doc);
            writer.Dispose();
            IndexReader reader = DirectoryReader.Open(dir);
            Term t = new Term("field", "x");
            AssertEquals(1, reader.DocFreq(t));
            reader.Dispose();
            dir.Dispose();
        }

        // LUCENE-1179
        [Test]
        public virtual void TestEmptyFieldName()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
                , new MockAnalyzer(Random())));
            Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
			{
			    NewTextField(string.Empty, "a b c", Field.Store.NO)
			};
            writer.AddDocument(doc);
            writer.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestEmptyFieldNameTerms()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
                , new MockAnalyzer(Random())));
            var doc = new Lucene.Net.Documents.Document
			{
			    NewTextField(string.Empty, "a b c", Field.Store.NO)
			};
            writer.AddDocument(doc);
            writer.Dispose();
            DirectoryReader reader = DirectoryReader.Open(dir);
            AtomicReader subreader = GetOnlySegmentReader(reader);
            TermsEnum te = subreader.Fields.Terms(string.Empty).IEnumerator(null);
            AssertEquals(new BytesRef("a"), te.Next());
            AssertEquals(new BytesRef("b"), te.Next());
            AssertEquals(new BytesRef("c"), te.Next());
            IsNull(te.Next());
            reader.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestEmptyFieldNameWithEmptyTerm()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
                , new MockAnalyzer(Random())));
            var doc = new Lucene.Net.Documents.Document
			{
			    NewStringField(string.Empty, string.Empty, Field.Store.NO),
			    NewStringField(string.Empty, "a", Field.Store.NO),
			    NewStringField(string.Empty, "b", Field.Store.NO),
			    NewStringField(string.Empty, "c", Field.Store.NO)
			};
            writer.AddDocument(doc);
            writer.Dispose();
            DirectoryReader reader = DirectoryReader.Open(dir);
            AtomicReader subreader = GetOnlySegmentReader(reader);
            TermsEnum te = subreader.Fields.Terms(string.Empty).IEnumerator(null);
            AssertEquals(new BytesRef(string.Empty), te.Next());
            AssertEquals(new BytesRef("a"), te.Next());
            AssertEquals(new BytesRef("b"), te.Next());
            AssertEquals(new BytesRef("c"), te.Next());
            IsNull(te.Next());
            reader.Dispose();
            dir.Dispose();
        }

        private sealed class MockIndexWriter : IndexWriter
        {
            /// <exception cref="System.IO.IOException"></exception>
            public MockIndexWriter(Directory dir, IndexWriterConfig conf)
                : base(dir, conf)
            {
            }

            internal bool afterWasCalled;

            internal bool beforeWasCalled;

            protected internal override void DoAfterFlush()
            {
                afterWasCalled = true;
            }

            protected internal override void DoBeforeFlush()
            {
                beforeWasCalled = true;
            }
        }

        // LUCENE-1222
        [Test]
        public virtual void TestDoBeforeAfterFlush()
        {
            Directory dir = NewDirectory();
            TestIndexWriter.MockIndexWriter w = new TestIndexWriter.MockIndexWriter(dir, NewIndexWriterConfig
                (TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                ();
            FieldType customType = new FieldType(TextField.TYPE_STORED);
            doc.Add(NewField("field", "a field", customType));
            w.AddDocument(doc);
            w.Commit();
            AssertTrue(w.beforeWasCalled);
            AssertTrue(w.afterWasCalled);
            w.beforeWasCalled = false;
            w.afterWasCalled = false;
            w.DeleteDocuments(new Term("field", "field"));
            w.Commit();
            AssertTrue(w.beforeWasCalled);
            AssertTrue(w.afterWasCalled);
            w.Dispose();
            IndexReader ir = DirectoryReader.Open(dir);
            AssertEquals(0, ir.NumDocs);
            ir.Dispose();
            dir.Dispose();
        }

        // LUCENE-1255
        [Test]
        public virtual void TestNegativePositions()
        {
            TokenStream tokens = new AnonymousTokenStream();
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new
                MockAnalyzer(Random())));
            Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                ();
            doc.Add(new TextField("field", tokens));
            try
            {
                w.AddDocument(doc);
                Fail("did not hit expected exception");
            }
            catch (ArgumentException)
            {
            }
            // expected
            w.Dispose();
            dir.Dispose();
        }

        private sealed class AnonymousTokenStream : TokenStream
        {
            public AnonymousTokenStream()
            {
                this.termAtt = this.AddAttribute<CharTermAttribute>();
                this.posIncrAtt = this.AddAttribute<PositionIncrementAttribute>();
                this.terms = Arrays.AsList("a", "b", "c").GetEnumerator();
                this.first = true;
            }

            internal readonly CharTermAttribute termAtt;

            internal readonly PositionIncrementAttribute posIncrAtt;

            internal readonly List<string>.Enumerator terms;

            internal bool first;

            public override bool IncrementToken()
            {
                if (!this.terms.MoveNext())
                {
                    return false;
                }
                this.ClearAttributes();
                this.termAtt.Append(this.terms.Current);
                this.posIncrAtt.PositionIncrement = (this.first ? 0 : 1);
                this.first = false;
                return true;
            }
        }

        // LUCENE-2529
        [Test]
        public virtual void TestPositionIncrementGapEmptyField()
        {
            Directory dir = NewDirectory();
            MockAnalyzer analyzer = new MockAnalyzer(Random());
            analyzer.PositionIncrementGap = (100);
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer
                ));
            Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                ();
            FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
            customType.StoreTermVectors = true;
            customType.StoreTermVectorPositions = true;
            Field f = NewField("field", string.Empty, customType);
            Field f2 = NewField("field", "crunch man", customType);
            doc.Add(f);
            doc.Add(f2);
            w.AddDocument(doc);
            w.Dispose();
            IndexReader r = DirectoryReader.Open(dir);
            Terms tpv = r.GetTermVectors(0).Terms("field");
            TermsEnum termsEnum = tpv.IEnumerator(null);
            IsNotNull(termsEnum.Next());
            DocsAndPositionsEnum dpEnum = termsEnum.DocsAndPositions(null, null);
            IsNotNull(dpEnum);
            AssertTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            AssertEquals(1, dpEnum.Freq);
            AssertEquals(100, dpEnum.NextPosition());
            IsNotNull(termsEnum.Next());
            dpEnum = termsEnum.DocsAndPositions(null, dpEnum);
            IsNotNull(dpEnum);
            AssertTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            AssertEquals(1, dpEnum.Freq);
            AssertEquals(101, dpEnum.NextPosition());
            IsNull(termsEnum.Next());
            r.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestDeadlock()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
                (TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2)));
            Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                ();
            FieldType customType = new FieldType(TextField.TYPE_STORED);
            customType.StoreTermVectors = true;
            customType.StoreTermVectorPositions = true;
            customType.StoreTermVectorOffsets = true;
            doc.Add(NewField("content", "aaa bbb ccc ddd eee fff ggg hhh iii", customType));
            writer.AddDocument(doc);
            writer.AddDocument(doc);
            writer.AddDocument(doc);
            writer.Commit();
            // index has 2 segments
            Directory dir2 = NewDirectory();
            IndexWriter writer2 = new IndexWriter(dir2, NewIndexWriterConfig(TEST_VERSION_CURRENT
                , new MockAnalyzer(Random())));
            writer2.AddDocument(doc);
            writer2.Dispose();
            IndexReader r1 = DirectoryReader.Open(dir2);
            writer.AddIndexes(r1, r1);
            writer.Dispose();
            IndexReader r3 = DirectoryReader.Open(dir);
            AssertEquals(5, r3.NumDocs);
            r3.Dispose();
            r1.Dispose();
            dir2.Dispose();
            dir.Dispose();
        }

        private class IndexerThreadInterrupt
        {
            internal volatile bool failed;

            internal volatile bool finish;

            internal volatile bool allowInterrupt = false;

            internal readonly Random random;

            internal readonly Directory adder;

            /// <exception cref="System.IO.IOException"></exception>
            public IndexerThreadInterrupt(TestIndexWriter _enclosing)
            {
                this._enclosing = _enclosing;
                this.random = new Random(LuceneTestCase.Random().NextInt(0,int.MaxValue));
                // make a little directory for addIndexes
                // LUCENE-2239: won't work with NIOFS/MMAP
                this.adder = new MockDirectoryWrapper(this.random, new RAMDirectory());
                IndexWriterConfig conf = LuceneTestCase.NewIndexWriterConfig(this.random, LuceneTestCase
                    .TEST_VERSION_CURRENT, new MockAnalyzer(this.random));
                IndexWriter w = new IndexWriter(this.adder, conf);
                Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                    ();
                doc.Add(LuceneTestCase.NewStringField(this.random, "id", "500", Field.Store.NO));
                doc.Add(LuceneTestCase.NewField(this.random, "field", "some prepackaged text contents"
                    , TestIndexWriter.storedTextType));
                if (LuceneTestCase.DefaultCodecSupportsDocValues())
                {
                    doc.Add(new BinaryDocValuesField("binarydv", new BytesRef("500")));
                    doc.Add(new NumericDocValuesField("numericdv", 500));
                    doc.Add(new SortedDocValuesField("sorteddv", new BytesRef("500")));
                }
                if (LuceneTestCase.DefaultCodecSupportsSortedSet())
                {
                    doc.Add(new SortedSetDocValuesField("sortedsetdv", new BytesRef("one")));
                    doc.Add(new SortedSetDocValuesField("sortedsetdv", new BytesRef("two")));
                }
                w.AddDocument(doc);
                doc = new Lucene.Net.Documents.Document();
                doc.Add(LuceneTestCase.NewStringField(this.random, "id", "501", Field.Store.NO));
                doc.Add(LuceneTestCase.NewField(this.random, "field", "some more contents", TestIndexWriter
                    .storedTextType));
                if (LuceneTestCase.DefaultCodecSupportsDocValues())
                {
                    doc.Add(new BinaryDocValuesField("binarydv", new BytesRef("501")));
                    doc.Add(new NumericDocValuesField("numericdv", 501));
                    doc.Add(new SortedDocValuesField("sorteddv", new BytesRef("501")));
                }
                if (LuceneTestCase.DefaultCodecSupportsSortedSet())
                {
                    doc.Add(new SortedSetDocValuesField("sortedsetdv", new BytesRef("two")));
                    doc.Add(new SortedSetDocValuesField("sortedsetdv", new BytesRef("three")));
                }
                w.AddDocument(doc);
                w.DeleteDocuments(new Term("id", "500"));
                w.Dispose();
            }

            public void Run()
			{
				// LUCENE-2239: won't work with NIOFS/MMAP
				MockDirectoryWrapper dir = new MockDirectoryWrapper(this.random, new RAMDirectory
					());
				// When interrupt arrives in w.close(), when it's
				// writing liveDocs, this can lead to double-write of
				// _X_N.del:
				//dir.setPreventDoubleWrite(false);
				IndexWriter w = null;
				while (!this.finish)
				{
					try
					{
						while (!this.finish)
						{
							if (w != null)
							{
								// If interrupt arrives inside here, it's
								// fine: we will cycle back and the first
								// thing we do is try to close again,
								// i.e. we'll never try to open a new writer
								// until this one successfully closes:
								w.Dispose();
								w = null;
							}
							IndexWriterConfig conf = ((IndexWriterConfig)LuceneTestCase.NewIndexWriterConfig(
								this.random, LuceneTestCase.TEST_VERSION_CURRENT, new MockAnalyzer(this.random))
								.SetMaxBufferedDocs(2));
							w = new IndexWriter(dir, conf);
							Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
								();
							Field idField = LuceneTestCase.NewStringField(this.random, "id", string.Empty, Field.Store
								.NO);
							Field binaryDVField = null;
							Field numericDVField = null;
							Field sortedDVField = null;
							Field sortedSetDVField = new SortedSetDocValuesField("sortedsetdv", new BytesRef(
								));
							doc.Add(idField);
							doc.Add(LuceneTestCase.NewField(this.random, "field", "some text contents", TestIndexWriter
								.storedTextType));
							if (LuceneTestCase.DefaultCodecSupportsDocValues())
							{
								binaryDVField = new BinaryDocValuesField("binarydv", new BytesRef());
								numericDVField = new NumericDocValuesField("numericdv", 0);
								sortedDVField = new SortedDocValuesField("sorteddv", new BytesRef());
								doc.Add(binaryDVField);
								doc.Add(numericDVField);
								doc.Add(sortedDVField);
							}
							if (LuceneTestCase.DefaultCodecSupportsSortedSet())
							{
								doc.Add(sortedSetDVField);
							}
							for (int i = 0; i < 100; i++)
							{
								idField.StringValue = i.ToString();
								if (LuceneTestCase.DefaultCodecSupportsDocValues())
								{
									binaryDVField.SetBytesValue(new BytesRef(idField.StringValue));
									numericDVField.SetLongValue(i);
									sortedDVField.SetBytesValue(new BytesRef(idField.StringValue));
								}
								sortedSetDVField.SetBytesValue(new BytesRef(idField.StringValue));
								int action = this.random.Next(100);
								if (action == 17)
								{
									w.AddIndexes(this.adder);
								}
								else
								{
									if (action % 30 == 0)
									{
										w.DeleteAll();
									}
									else
									{
										if (action % 2 == 0)
										{
											w.UpdateDocument(new Term("id", idField.StringValue), doc);
										}
										else
										{
											w.AddDocument(doc);
										}
									}
								}
								if (this.random.Next(3) == 0)
								{
									IndexReader r = null;
									try
									{
										r = DirectoryReader.Open(w, this.random.NextBoolean());
										if (this.random.NextBoolean() && r.MaxDoc > 0)
										{
											int docid = this.random.Next(r.MaxDoc);
											w.TryDeleteDocument(r, docid);
										}
									}
									finally
									{
										IOUtils.CloseWhileHandlingException((IDisposable)r);
									}
								}
								if (i % 10 == 0)
								{
									w.Commit();
								}
								if (this.random.Next(50) == 0)
								{
									w.ForceMerge(1);
								}
							}
							w.Dispose();
							w = null;
							DirectoryReader.Open(dir).Dispose();
							// Strangely, if we interrupt a thread before
							// all classes are loaded, the class loader
							// seems to do scary things with the interrupt
							// status.  In java 1.5, it'll throw an
							// incorrect ClassNotFoundException.  In java
							// 1.6, it'll silently clear the interrupt.
							// So, on first iteration through here we
							// don't open ourselves up for interrupts
							// until we've done the above loop.
							this.allowInterrupt = true;
						}
					}
					catch (ThreadInterruptedException re)
					{
						// NOTE: important to leave this verbosity/noise
						// on!!  This test doesn't repro easily so when
						// Jenkins hits a fail we need to study where the
						// interrupts struck!
						System.Console.Out.WriteLine("TEST: got interrupt");
						re.printStackTrace();
						Exception e = re.InnerException;
						AssertTrue(e is Exception);
						if (this.finish)
						{
							break;
						}
					}
					catch (Exception t)
					{
						System.Console.Out.WriteLine("FAILED; unexpected exception");
						t.printStackTrace();
						this.failed = true;
						break;
					}
				}
				if (!this.failed)
				{
					if (LuceneTestCase.VERBOSE)
					{
						System.Console.Out.WriteLine("TEST: now rollback");
					}
					// clear interrupt state:
					
					if (w != null)
					{
						
							w.Rollback();
					
					}
					try
					{
						TestUtil.CheckIndex(dir);
					}
					catch (Exception e)
					{
						this.failed = true;
						System.Console.Out.WriteLine("CheckIndex FAILED: unexpected exception");
						e.printStackTrace();
					}
					try
					{
						IndexReader r = DirectoryReader.Open(dir);
						//System.out.println("doc count=" + r.numDocs());
						r.Dispose();
					}
					catch (Exception e)
					{
						this.failed = true;
						System.Console.Out.WriteLine("DirectoryReader.open FAILED: unexpected exception");
						e.printStackTrace();
					}
				}
				try
				{
					IOUtils.Close(dir);
				}
				catch (IOException e)
				{
					throw new SystemException(e.Message,e);
				}
				try
				{
					IOUtils.Close(this.adder);
				}
				catch (IOException e)
				{
					throw new SystemException(e.Message,e);
				}
			}

            private readonly TestIndexWriter _enclosing;
        }

        [Test]
        public virtual void TestThreadInterruptDeadlock()
        {
            var t = new TestIndexWriter.IndexerThreadInterrupt(this);
            var thread = new Thread(t.Run);
            thread.IsBackground = true;
            thread.Start();
            // Force class loader to load ThreadInterruptedException
            // up front... else we can see a false failure if 2nd
            // interrupt arrives while class loader is trying to
            // init this class (in servicing a first interrupt):
            //TODO: This might not be a valid test. revisit
            AssertTrue(new ThreadInterruptedException(new Exception().Message).InnerException
                 is Exception);
            // issue 300 interrupts to child thread
            int numInterrupts = AtLeast(300);
            int i = 0;
            while (i < numInterrupts)
            {
                // TODO: would be nice to also sometimes interrupt the
                // CMS merge threads too ...
                Thread.Sleep(10);
                if (t.allowInterrupt)
                {
                    i++;
                    thread.Interrupt();
                }
                if (!thread.IsAlive)
                {
                    break;
                }
            }
            t.finish = true;
            thread.Join();
            IsFalse(t.failed);
        }

        /// <summary>testThreadInterruptDeadlock but with 2 indexer threads</summary>
        [Test]
        public virtual void TestTwoThreadsInterruptDeadlock()
        {
            var t1 = new IndexerThreadInterrupt(this);
            var thread1 = new Thread(t1.Run);
            thread1.IsBackground = true;
            thread1.Start();
            var t2 = new IndexerThreadInterrupt(this);
            var thread2 = new Thread(t2.Run);
            thread2.IsBackground = true;
            thread2.Start();
            // Force class loader to load ThreadInterruptedException
            // up front... else we can see a false failure if 2nd
            // interrupt arrives while class loader is trying to
            // init this class (in servicing a first interrupt):
            AssertTrue(new ThreadInterruptedException(new Exception().Message).InnerException
                 is Exception);
            // issue 300 interrupts to child thread
            int numInterrupts = AtLeast(300);
            int i = 0;
            while (i < numInterrupts)
            {
                // TODO: would be nice to also sometimes interrupt the
                // CMS merge threads too ...
                Thread.Sleep(10);
                TestIndexWriter.IndexerThreadInterrupt t = Random().NextBoolean() ? t1 : t2;
                Thread th = t == t1 ? thread1 : thread2;
                if (t.allowInterrupt)
                {
                    i++;
                    th.Interrupt();
                }
                if (!thread1.IsAlive && !thread2.IsAlive)
                {
                    break;
                }
            }
            t1.finish = true;
            t2.finish = true;
            thread1.Join();
            thread2.Join();
            IsFalse(t1.failed);
            IsFalse(t2.failed);
        }

        [Test]
        public virtual void TestIndexStoreCombos()
        {
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new
                MockAnalyzer(Random())));
            var b = new sbyte[50];
            for (int i = 0; i < 50; i++)
            {
                b[i] = ((sbyte)(i + 77));
            }
            Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                ();
            FieldType customType = new FieldType(StoredField.TYPE);
            customType.Tokenized = (true);
            Field f = new Field("binary", b, 10, 17, customType);
            customType.Indexed = (true);
            f.SetTokenStream(new MockTokenizer(new StringReader("doc1field1"), MockTokenizer.
                WHITESPACE, false));
            FieldType customType2 = new FieldType(TextField.TYPE_STORED);
            Field f2 = NewField("string", "value", customType2);
            f2.SetTokenStream(new MockTokenizer(new StringReader("doc1field2"), MockTokenizer
                .WHITESPACE, false));
            doc.Add(f);
            doc.Add(f2);
            w.AddDocument(doc);
            // add 2 docs to test in-memory merging
            f.SetTokenStream(new MockTokenizer(new StringReader("doc2field1"), MockTokenizer.
                WHITESPACE, false));
            f2.SetTokenStream(new MockTokenizer(new StringReader("doc2field2"), MockTokenizer
                .WHITESPACE, false));
            w.AddDocument(doc);
            // force segment flush so we can force a segment merge with doc3 later.
            w.Commit();
            f.SetTokenStream(new MockTokenizer(new StringReader("doc3field1"), MockTokenizer.
                WHITESPACE, false));
            f2.SetTokenStream(new MockTokenizer(new StringReader("doc3field2"), MockTokenizer
                .WHITESPACE, false));
            w.AddDocument(doc);
            w.Commit();
            w.ForceMerge(1);
            // force segment merge.
            w.Dispose();
            IndexReader ir = DirectoryReader.Open(dir);
            Lucene.Net.Documents.Document doc2 = ir.Document(0);
            IIndexableField f3 = doc2.GetField("binary");
            b = f3.BinaryValue.bytes;
            AssertTrue(b != null);
            AssertEquals(17, b.Length, 17);
            AssertEquals(87, b[0]);
            AssertTrue(ir.Document(0).GetField("binary").BinaryValue != null);
            AssertTrue(ir.Document(1).GetField("binary").BinaryValue != null);
            AssertTrue(ir.Document(2).GetField("binary").BinaryValue != null);
            AssertEquals("value", ir.Document(0).Get("string"));
            AssertEquals("value", ir.Document(1).Get("string"));
            AssertEquals("value", ir.Document(2).Get("string"));
            // test that the terms were indexed.
            AssertTrue(TestUtil.Docs(Random(), ir, "binary", new BytesRef(
                "doc1field1"), null, null, DocsEnum.FLAG_NONE).NextDoc() != DocIdSetIterator.NO_MORE_DOCS
                );
            AssertTrue(TestUtil.Docs(Random(), ir, "binary", new BytesRef(
                "doc2field1"), null, null, DocsEnum.FLAG_NONE).NextDoc() != DocIdSetIterator.NO_MORE_DOCS
                );
            AssertTrue(TestUtil.Docs(Random(), ir, "binary", new BytesRef(
                "doc3field1"), null, null, DocsEnum.FLAG_NONE).NextDoc() != DocIdSetIterator.NO_MORE_DOCS
                );
            AssertTrue(TestUtil.Docs(Random(), ir, "string", new BytesRef(
                "doc1field2"), null, null, DocsEnum.FLAG_NONE).NextDoc() != DocIdSetIterator.NO_MORE_DOCS
                );
            AssertTrue(TestUtil.Docs(Random(), ir, "string", new BytesRef(
                "doc2field2"), null, null, DocsEnum.FLAG_NONE).NextDoc() != DocIdSetIterator.NO_MORE_DOCS
                );
            AssertTrue(TestUtil.Docs(Random(), ir, "string", new BytesRef(
                "doc3field2"), null, null, DocsEnum.FLAG_NONE).NextDoc() != DocIdSetIterator.NO_MORE_DOCS
                );
            ir.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestNoDocsIndex()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
                , new MockAnalyzer(Random())));
            writer.AddDocument(new Lucene.Net.Documents.Document());
            writer.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestIndexDivisor()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig config = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
                (Random()));
            config.SetTermIndexInterval(2);
            IndexWriter w = new IndexWriter(dir, config);
            StringBuilder s = new StringBuilder();
            // must be > 256
            for (int i = 0; i < 300; i++)
            {
                s.Append(' ').Append(i);
            }
            Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
            Field f = NewTextField("field", s.ToString(), Field.Store.NO);
            d.Add(f);
            w.AddDocument(d);
            AtomicReader r = GetOnlySegmentReader(w.Reader);
            TermsEnum t = r.Fields.Terms("field").IEnumerator(null);
            int count = 0;
            while (t.Next() != null)
            {
                DocsEnum docs = TestUtil.Docs(Random(), t, null, null, DocsEnum.FLAG_NONE);
                AssertEquals(0, docs.NextDoc());
                AssertEquals(DocIdSetIterator.NO_MORE_DOCS, docs.NextDoc());
                count++;
            }
            AssertEquals(300, count);
            r.Dispose();
            w.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestDeleteUnusedFiles()
        {
            for (int iter = 0; iter < 2; iter++)
            {
                Directory dir = NewMockDirectory();
                // relies on windows semantics
                MergePolicy mergePolicy = NewLogMergePolicy(true);
                // This test expects all of its segments to be in CFS
                mergePolicy.SetNoCFSRatio(1.0);
                mergePolicy.SetMaxCFSSegmentSizeMB(double.PositiveInfinity);
                var iwConfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
                iwConfig.SetMergePolicy(mergePolicy);
                iwConfig.UseCompoundFile = (true);
                IndexWriter w = new IndexWriter(dir, iwConfig);
                var doc = new Lucene.Net.Documents.Document { NewTextField("field", "go", Field.Store.NO) };
                w.AddDocument(doc);
                DirectoryReader r;
                if (iter == 0)
                {
                    // use NRT
                    r = w.Reader;
                }
                else
                {
                    // don't use NRT
                    w.Commit();
                    r = DirectoryReader.Open(dir);
                }
                IList<string> files = new List<string>(Arrays.AsList(dir.ListAll()));
                // RAMDir won't have a write.lock, but fs dirs will:
                files.Remove("write.lock");
                AssertTrue(files.Contains("_0.cfs"));
                AssertTrue(files.Contains("_0.cfe"));
                AssertTrue(files.Contains("_0.si"));
                if (iter == 1)
                {
                    // we run a full commit so there should be a segments file etc.
                    AssertTrue(files.Contains("segments_1"));
                    AssertTrue(files.Contains("segments.gen"));
                    AssertEquals(files.ToString(), files.Count, 5);
                }
                else
                {
                    // this is an NRT reopen - no segments files yet
                    AssertEquals(files.ToString(), files.Count, 3);
                }
                w.AddDocument(doc);
                w.ForceMerge(1);
                if (iter == 1)
                {
                    w.Commit();
                }
                IndexReader r2 = DirectoryReader.OpenIfChanged(r);
                IsNotNull(r2);
                AssertTrue(r != r2);
                files = Arrays.AsList(dir.ListAll());
                // NOTE: here we rely on "Windows" behavior, ie, even
                // though IW wanted to delete _0.cfs since it was
                // merged away, because we have a reader open
                // against this file, it should still be here:
                AssertTrue(files.Contains("_0.cfs"));
                // forceMerge created this
                //assertTrue(files.contains("_2.cfs"));
                w.DeleteUnusedFiles();
                files = Arrays.AsList(dir.ListAll());
                // r still holds this file open
                AssertTrue(files.Contains("_0.cfs"));
                //assertTrue(files.contains("_2.cfs"));
                r.Dispose();
                if (iter == 0)
                {
                    // on closing NRT reader, it calls writer.deleteUnusedFiles
                    files = Arrays.AsList(dir.ListAll());
                    IsFalse(files.Contains("_0.cfs"));
                }
                else
                {
                    // now writer can remove it
                    w.DeleteUnusedFiles();
                    files = Arrays.AsList(dir.ListAll());
                    IsFalse(files.Contains("_0.cfs"));
                }
                //assertTrue(files.contains("_2.cfs"));
                w.Dispose();
                r2.Dispose();
                dir.Dispose();
            }
        }

        [Test]
        public virtual void TestDeleteUnsedFiles2()
        {
            // Validates that iw.deleteUnusedFiles() also deletes unused index commits
            // in case a deletion policy which holds onto commits is used.
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
                , new MockAnalyzer(Random())).SetIndexDeletionPolicy(new SnapshotDeletionPolicy(
                new KeepOnlyLastCommitDeletionPolicy())));
            SnapshotDeletionPolicy sdp = (SnapshotDeletionPolicy)writer.Config.IndexDeletionPolicy;
            // First commit
            Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                ();
            FieldType customType = new FieldType(TextField.TYPE_STORED);
            customType.StoreTermVectors = true;
            customType.StoreTermVectorPositions = true;
            customType.StoreTermVectorOffsets = true;
            doc.Add(NewField("c", "val", customType));
            writer.AddDocument(doc);
            writer.Commit();
            AssertEquals(1, DirectoryReader.ListCommits(dir).Count);
            // Keep that commit
            IndexCommit id = sdp.Snapshot();
            // Second commit - now KeepOnlyLastCommit cannot delete the prev commit.
            doc = new Lucene.Net.Documents.Document();
            doc.Add(NewField("c", "val", customType));
            writer.AddDocument(doc);
            writer.Commit();
            AssertEquals(2, DirectoryReader.ListCommits(dir).Count);
            // Should delete the unreferenced commit
            sdp.Release(id);
            writer.DeleteUnusedFiles();
            AssertEquals(1, DirectoryReader.ListCommits(dir).Count);
            writer.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestEmptyFSDirWithNoLock()
        {
            // Tests that if FSDir is opened w/ a NoLockFactory (or SingleInstanceLF),
            // then IndexWriter ctor succeeds. Previously (LUCENE-2386) it failed
            // when listAll() was called in IndexFileDeleter.
            Directory dir = NewFSDirectory(CreateTempDir("emptyFSDirNoLock"), NoLockFactory.Instance);
            new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(
                Random()))).Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestEmptyDirRollback()
        {
            // TODO: generalize this test
            //TODO: uncomment the below test after codecs are done
            //AssumeFalse("test makes assumptions about file counts", Codec.Default is SimpleTextCodec);
            // Tests that if IW is created over an empty Directory, some documents are
            // indexed, flushed (but not committed) and then IW rolls back, then no
            // files are left in the Directory.
            Directory dir = NewDirectory();
            var iwConfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            iwConfig.SetMaxBufferedDocs(2);
            iwConfig.SetMergePolicy(NewLogMergePolicy());
            iwConfig.UseCompoundFile = (false);
            IndexWriter writer = new IndexWriter(dir, iwConfig);
            string[] files = dir.ListAll();
            // Creating over empty dir should not create any files,
            // or, at most the write.lock file
            int extraFileCount;
            if (files.Length == 1)
            {
                AssertTrue(files[0].EndsWith("write.lock"));
                extraFileCount = 1;
            }
            else
            {
                AssertEquals(0, files.Length);
                extraFileCount = 0;
            }
            Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                ();
            FieldType customType = new FieldType(TextField.TYPE_STORED);
            customType.StoreTermVectors = true;
            customType.StoreTermVectorPositions = true;
            customType.StoreTermVectorOffsets = true;
            // create as many files as possible
            doc.Add(NewField("c", "val", customType));
            writer.AddDocument(doc);
            // Adding just one document does not call flush yet.
            int computedExtraFileCount = dir.ListAll().Count(file => file.LastIndexOf('.') < 0 || !Arrays.AsList("fdx", "fdt", "tvx", "tvd", "tvf").Contains(file.Substring(file.LastIndexOf('.') + 1)));
            AssertEquals("only the stored and term vector files should exist in the directory"
                , extraFileCount, computedExtraFileCount);
            doc = new Lucene.Net.Documents.Document();
            doc.Add(NewField("c", "val", customType));
            writer.AddDocument(doc);
            // The second document should cause a flush.
            AssertTrue("flush should have occurred and files should have been created"
                , dir.ListAll().Length > 5 + extraFileCount);
            // After rollback, IW should remove all files
            writer.Rollback();
            string[] allFiles = dir.ListAll();
            AssertTrue("no files should exist in the directory after rollback"
                , allFiles.Length == 0 || Arrays.Equals(allFiles, new[] { IndexWriter.WRITE_LOCK_NAME
				 }));
            // Since we rolled-back above, that close should be a no-op
            writer.Dispose();
            allFiles = dir.ListAll();
            AssertTrue("expected a no-op close after IW.rollback()", allFiles
                .Length == 0 || Arrays.Equals(allFiles, new[] { IndexWriter.WRITE_LOCK_NAME
				 }));
            dir.Dispose();
        }

        [Test]
        public virtual void TestNoSegmentFile()
        {
            BaseDirectoryWrapper dir = NewDirectory();
            dir.LockFactory = (NoLockFactory.Instance);
            IndexWriter w = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
                , new MockAnalyzer(Random())).SetMaxBufferedDocs(2)));
            Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                ();
            FieldType customType = new FieldType(TextField.TYPE_STORED);
            customType.StoreTermVectors = true;
            customType.StoreTermVectorPositions = true;
            customType.StoreTermVectorOffsets = true;
            doc.Add(NewField("c", "val", customType));
            w.AddDocument(doc);
            w.AddDocument(doc);
            IndexWriter w2 = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
                , new MockAnalyzer(Random())).SetMaxBufferedDocs(2)).SetOpenMode(IndexWriterConfig.OpenMode
                .CREATE));
            w2.Dispose();
            // If we don't do that, the test fails on Windows
            w.Rollback();
            // This test leaves only segments.gen, which causes
            // DirectoryReader.indexExists to return true:
            dir.SetCheckIndexOnClose(false);
            dir.Dispose();
        }

        [Test]
        public virtual void TestNoUnwantedTVFiles()
        {
            Directory dir = NewDirectory();
            IndexWriter indexWriter = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
                (TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetRAMBufferSizeMB(0.01)).SetMergePolicy
                (NewLogMergePolicy()));
            indexWriter.Config.MergePolicy.SetNoCFSRatio(0.0);
            string BIG = "alskjhlaksjghlaksjfhalksvjepgjioefgjnsdfjgefgjhelkgjhqewlrkhgwlekgrhwelkgjhwelkgrhwlkejg";
            BIG = BIG + BIG + BIG + BIG;
            FieldType customType = new FieldType(TextField.TYPE_STORED);
            customType.OmitNorms = (true);
            FieldType customType2 = new FieldType(TextField.TYPE_STORED);
            customType2.Tokenized = (false);
            FieldType customType3 = new FieldType(TextField.TYPE_STORED);
            customType3.Tokenized = (false);
            customType3.OmitNorms = (true);
            for (int i = 0; i < 2; i++)
            {
                Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                    ();
                doc.Add(new Field("id", i.ToString() + BIG, customType3));
                doc.Add(new Field("str", i.ToString() + BIG, customType2));
                doc.Add(new Field("str2", i.ToString() + BIG, storedTextType));
                doc.Add(new Field("str3", i.ToString() + BIG, customType));
                indexWriter.AddDocument(doc);
            }
            indexWriter.Dispose();
            TestUtil.CheckIndex(dir);
            AssertNoUnreferencedFiles(dir, "no tv files");
            DirectoryReader r0 = DirectoryReader.Open(dir);
            foreach (AtomicReaderContext ctx in r0.Leaves)
            {
                SegmentReader sr = (SegmentReader)((AtomicReader)ctx.Reader);
                IsFalse(sr.FieldInfos.HasVectors);
            }
            r0.Dispose();
            dir.Dispose();
        }

        internal sealed class StringSplitAnalyzer : Analyzer
        {
            public override Analyzer.TokenStreamComponents CreateComponents(string fieldName
                , TextReader reader)
            {
                return new Analyzer.TokenStreamComponents(new TestIndexWriter.StringSplitTokenizer
                    (reader));
            }
        }

        private class StringSplitTokenizer : Tokenizer
        {
            private string[] tokens;

            private int upto;

            private readonly CharTermAttribute termAtt;

            protected internal StringSplitTokenizer(TextReader r)
                : base(r)
            {
                termAtt = AddAttribute<CharTermAttribute>();
                Reader = (r);

            }

            public sealed override bool IncrementToken()
            {
                ClearAttributes();
                if (upto < tokens.Length)
                {
                    termAtt.SetEmpty();
                    termAtt.Append(tokens[upto]);
                    upto++;
                    return true;
                }
                return false;
            }

            /// <exception cref="System.IO.IOException"></exception>
            public override void Reset()
            {
                base.Reset();
                this.upto = 0;
                StringBuilder b = new StringBuilder();
                char[] buffer = new char[1024];
                int n;
                while ((n = input.Read()) != -1)
                {
                    b.Append(buffer, 0, n);
                }
                this.tokens = b.ToString().Split(new[]{' '});
            }
        }

        /// <summary>Make sure we skip wicked long terms.</summary>
        /// <remarks>Make sure we skip wicked long terms.</remarks>
        [Test]
        public virtual void TestWickedLongTerm()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), dir, new TestIndexWriter.StringSplitAnalyzer
				());
			char[] chars = new char[DocumentsWriterPerThread.MAX_TERM_LENGTH_UTF8];
			Arrays.Fill(chars, 'x');
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			string bigTerm = new string(chars);
			BytesRef bigTermBytesRef = new BytesRef(bigTerm);
			// This contents produces a too-long term:
			string contents = "abc xyz x" + bigTerm + " another term";
			doc.Add(new TextField("content", contents, Field.Store.NO));
			try
			{
				w.AddDocument(doc);
				Fail("should have hit exception");
			}
			catch (ArgumentException)
			{
			}
			// expected
			// Make sure we can add another normal document
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new TextField("content", "abc bbb ccc", Field.Store.NO));
			w.AddDocument(doc);
			// So we remove the deleted doc:
			w.ForceMerge(1);
			IndexReader reader = w.GetReader();
			w.Close();
			// Make sure all terms < max size were indexed
			AssertEquals(1, reader.DocFreq(new Term("content", "abc")));
			AssertEquals(1, reader.DocFreq(new Term("content", "bbb")));
			AssertEquals(0, reader.DocFreq(new Term("content", "term")));
			// Make sure the doc that has the massive term is NOT in
			// the index:
			AssertEquals("document with wicked long term is in the index!"
				, 1, reader.NumDocs);
			reader.Dispose();
			dir.Dispose();
			dir = NewDirectory();
			// Make sure we can add a document with exactly the
			// maximum length term, and search on that term:
			doc = new Lucene.Net.Documents.Document();
			FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
			customType.Tokenized = (false);
			Field contentField = new Field("content", string.Empty, customType);
			doc.Add(contentField);
			w = new RandomIndexWriter(Random(), dir);
			contentField.StringValue = "other";
			w.AddDocument(doc);
			contentField.StringValue = "term";
			w.AddDocument(doc);
			contentField.StringValue = bigTerm;
			w.AddDocument(doc);
			contentField.StringValue = "zzz";
			w.AddDocument(doc);
			reader = w.GetReader();
			w.Close();
			AssertEquals(1, reader.DocFreq(new Term("content", bigTerm)));
			SortedDocValues dti = FieldCache.DEFAULT.GetTermsIndex(SlowCompositeReaderWrapper
				.Wrap(reader), "content", (float) (Random().NextDouble() * PackedInts.FAST));
			AssertEquals(4, dti.ValueCount);
			BytesRef br = new BytesRef();
			dti.LookupOrd(2, br);
			AssertEquals(bigTermBytesRef, br);
			reader.Dispose();
			dir.Dispose();
		}

        // LUCENE-3183
        [Test]
        public virtual void TestEmptyFieldNameTIIOne()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
                (Random()));
            iwc.SetTermIndexInterval(1);
            iwc.SetReaderTermsIndexDivisor(1);
            IndexWriter writer = new IndexWriter(dir, iwc);
            Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                ();
            doc.Add(NewTextField(string.Empty, "a b c", Field.Store.NO));
            writer.AddDocument(doc);
            writer.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestDeleteAllNRTLeftoverFiles()
        {
            Directory d = new MockDirectoryWrapper(Random(), new RAMDirectory());
            IndexWriter w = new IndexWriter(d, new IndexWriterConfig(TEST_VERSION_CURRENT, new
                MockAnalyzer(Random())));
            Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                ();
            for (int i = 0; i < 20; i++)
            {
                for (int j = 0; j < 100; ++j)
                {
                    w.AddDocument(doc);
                }
                w.Commit();
                DirectoryReader.Open(w, true).Dispose();
                w.DeleteAll();
                w.Commit();
                // Make sure we accumulate no files except for empty
                // segments_N and segments.gen:
                AssertTrue(d.ListAll().Length <= 2);
            }
            w.Dispose();
            d.Dispose();
        }

        [Test]
        public virtual void TestNRTReaderVersion()
        {
            Directory d = new MockDirectoryWrapper(Random(), new RAMDirectory());
            IndexWriter w = new IndexWriter(d, new IndexWriterConfig(TEST_VERSION_CURRENT, new
                MockAnalyzer(Random())));
            var doc = new Lucene.Net.Documents.Document
            {
                NewStringField("id", "0", Field.Store.YES)
            };
            w.AddDocument(doc);
            DirectoryReader r = w.Reader;
            long version = r.Version;
            r.Dispose();
            w.AddDocument(doc);
            r = w.Reader;
            long version2 = r.Version;
            r.Dispose();
            
            //assert(version2 > version);
            w.DeleteDocuments(new Term("id", "0"));
            r = w.Reader;
            w.Dispose();
            long version3 = r.Version;
            r.Dispose();
            
            //assert(version3 > version2);
            d.Dispose();
        }

        [Test]
        public virtual void TestWhetherDeleteAllDeletesWriteLock()
        {
            Directory d = NewFSDirectory(CreateTempDir("TestIndexWriter.testWhetherDeleteAllDeletesWriteLock"
                ));
            // Must use SimpleFSLockFactory... NativeFSLockFactory
            // somehow "knows" a lock is held against write.lock
            // even if you remove that file:
            d.LockFactory = (new SimpleFSLockFactory());
            RandomIndexWriter w1 = new RandomIndexWriter(Random(), d);
            w1.DeleteAll();
            try
            {
                new RandomIndexWriter(Random(), d, NewIndexWriterConfig(TEST_VERSION_CURRENT, null
                    ).SetWriteLockTimeout(100));
                Fail("should not be able to create another writer");
            }
            catch (LockObtainFailedException)
            {
            }
            // expected
            w1.Close();
            d.Dispose();
        }

        [Test]
        public virtual void TestChangeIndexOptions()
        {
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT,
                new MockAnalyzer(Random())));
            FieldType docsAndFreqs = new FieldType(TextField.TYPE_NOT_STORED);
            docsAndFreqs.IndexOptions = (FieldInfo.IndexOptions.DOCS_AND_FREQS);
            FieldType docsOnly = new FieldType(TextField.TYPE_NOT_STORED);
            docsOnly.IndexOptions = (FieldInfo.IndexOptions.DOCS_ONLY);
            var doc = new Lucene.Net.Documents.Document {new Field("field", "a b c", docsAndFreqs)};
            w.AddDocument(doc);
            w.AddDocument(doc);
            doc = new Lucene.Net.Documents.Document();
            doc.Add(new Field("field", "a b c", docsOnly));
            w.AddDocument(doc);
            w.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestOnlyUpdateDocuments()
        {
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT,
                new MockAnalyzer(Random())));
            IList<Lucene.Net.Documents.Document> docs = new List<Lucene.Net.Documents.Document
                >();
            docs.Add(new Lucene.Net.Documents.Document());
            w.UpdateDocuments(new Term("foo", "bar"), docs.AsEnumerable());
            w.Dispose();
            dir.Dispose();
        }

        // LUCENE-3872
        [Test]
        public virtual void TestPrepareCommitThenClose()
        {
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT,
                new MockAnalyzer(Random())));
            w.PrepareCommit();
            try
            {
                w.Dispose();
                Fail("should have hit exception");
            }
            catch (InvalidOperationException)
            {
            }
            // expected
            w.Commit();
            w.Dispose();
            IndexReader r = DirectoryReader.Open(dir);
            AssertEquals(0, r.MaxDoc);
            r.Dispose();
            dir.Dispose();
        }

        // LUCENE-3872
        [Test]
        public virtual void TestPrepareCommitThenRollback()
        {
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT,
                new MockAnalyzer(Random())));
            w.PrepareCommit();
            w.Rollback();
            IsFalse(DirectoryReader.IndexExists(dir));
            dir.Dispose();
        }

        // LUCENE-3872
        [Test]
        public virtual void TestPrepareCommitThenRollback2()
        {
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT,
                new MockAnalyzer(Random())));
            w.Commit();
            w.AddDocument(new Lucene.Net.Documents.Document());
            w.PrepareCommit();
            w.Rollback();
            AssertTrue(DirectoryReader.IndexExists(dir));
            IndexReader r = DirectoryReader.Open(dir);
            AssertEquals(0, r.MaxDoc);
            r.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestDontInvokeAnalyzerForUnAnalyzedFields()
        {
            Analyzer analyzer = new AnonymousAnalyzer();
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer
                ));
            Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                ();
            FieldType customType = new FieldType(StringField.TYPE_NOT_STORED);
            customType.StoreTermVectors = true;
            customType.StoreTermVectorPositions = true;
            customType.StoreTermVectorOffsets = true;
            Field f = NewField("field", "abcd", customType);
            doc.Add(f);
            doc.Add(f);
            Field f2 = NewField("field", string.Empty, customType);
            doc.Add(f2);
            doc.Add(f);
            w.AddDocument(doc);
            w.Dispose();
            dir.Dispose();
        }

        private sealed class AnonymousAnalyzer : Analyzer
        {
            public override Analyzer.TokenStreamComponents CreateComponents(string fieldName
                , TextReader reader)
            {
                throw new InvalidOperationException("don't invoke me!");
            }

            public override int GetPositionIncrementGap(string fieldName)
            {
                throw new InvalidOperationException("don't invoke me!");
            }

            public override int GetOffsetGap(string fieldName)
            {
                throw new InvalidOperationException("don't invoke me!");
            }
        }

        //LUCENE-1468 -- make sure opening an IndexWriter with
        // create=true does not remove non-index files
        [Test]
        public virtual void TestOtherFiles()
        {
            Directory dir = NewDirectory();
            IndexWriter iw = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT,
                new MockAnalyzer(Random())));
            iw.AddDocument(new Lucene.Net.Documents.Document());
            iw.Dispose();
            try
            {
                // Create my own random file:
                IndexOutput @out = dir.CreateOutput("myrandomfile", NewIOContext(Random()));
                @out.WriteByte(unchecked((byte)42));
                @out.Dispose();
                new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(
                    Random()))).Dispose();
                AssertTrue(SlowFileExists(dir, "myrandomfile"));
            }
            finally
            {
                dir.Dispose();
            }
        }

        // LUCENE-3849
        [Test]
        public virtual void TestStopwordsPosIncHole()
        {
            Directory dir = NewDirectory();
            Analyzer a = new AnonymousAnalyzer2();
            RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, a);
            Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                ();
            doc.Add(new TextField("body", "just a", Field.Store.NO));
            doc.Add(new TextField("body", "test of gaps", Field.Store.NO));
            iw.AddDocument(doc);
            IndexReader ir = iw.GetReader();
            iw.Close();
            IndexSearcher @is = NewSearcher(ir);
            PhraseQuery pq = new PhraseQuery();
            pq.Add(new Term("body", "just"), 0);
            pq.Add(new Term("body", "test"), 2);
            // body:"just ? test"
            AssertEquals(1, @is.Search(pq, 5).TotalHits);
            ir.Dispose();
            dir.Dispose();
        }

        private sealed class AnonymousAnalyzer2 : Analyzer
        {
            public override Analyzer.TokenStreamComponents CreateComponents(string fieldName
                , TextReader reader)
            {
                Tokenizer tokenizer = new MockTokenizer(reader);
                TokenStream stream = new MockTokenFilter(tokenizer, MockTokenFilter.ENGLISH_STOPSET
                    );
                return new Analyzer.TokenStreamComponents(tokenizer, stream);
            }
        }

        // LUCENE-3849
        [Test]
        public virtual void TestStopwordsPosIncHole2()
        {
            // use two stopfilters for testing here
            Directory dir = NewDirectory();
            Lucene.Net.Util.Automaton.Automaton secondSet = BasicAutomata.MakeString("foobar");
            Analyzer a = new AnonymousAnalyzer3(secondSet);
            RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, a);
            var doc = new Lucene.Net.Documents.Document
            {
                new TextField("body", "just a foobar", Field.Store.NO),
                new TextField("body", "test of gaps", Field.Store.NO)
            };
            iw.AddDocument(doc);
            IndexReader ir = iw.GetReader();
            iw.Close();
            IndexSearcher @is = NewSearcher(ir);
            PhraseQuery pq = new PhraseQuery();
            pq.Add(new Term("body", "just"), 0);
            pq.Add(new Term("body", "test"), 3);
            // body:"just ? ? test"
            AssertEquals(1, @is.Search(pq, 5).TotalHits);
            ir.Dispose();
            dir.Dispose();
        }

        private sealed class AnonymousAnalyzer3 : Analyzer
        {
            public AnonymousAnalyzer3(Lucene.Net.Util.Automaton.Automaton secondSet)
            {
                this.secondSet = secondSet;
            }

            public override Analyzer.TokenStreamComponents CreateComponents(string fieldName
                , TextReader reader)
            {
                Tokenizer tokenizer = new MockTokenizer(reader);
                TokenStream stream = new MockTokenFilter(tokenizer, MockTokenFilter.ENGLISH_STOPSET
                    );
                stream = new MockTokenFilter(stream, new CharacterRunAutomaton(secondSet));
                return new Analyzer.TokenStreamComponents(tokenizer, stream);
            }

            private readonly Lucene.Net.Util.Automaton.Automaton secondSet;
        }

        // here we do better, there is no current segments file, so we don't delete anything.
        // however, if you actually go and make a commit, the next time you run indexwriter
        // this file will be gone.
        [Test]
        public virtual void TestOtherFiles2()
        {
            Directory dir = NewDirectory();
            try
            {
                // Create my own random file:
                IndexOutput @out = dir.CreateOutput("_a.frq", NewIOContext(Random()));
                @out.WriteByte(unchecked((byte)42));
                @out.Dispose();
                new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(
                    Random()))).Dispose();
                AssertTrue(SlowFileExists(dir, "_a.frq"));
                IndexWriter iw = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT,
                    new MockAnalyzer(Random())));
                iw.AddDocument(new Lucene.Net.Documents.Document());
                iw.Dispose();
                IsFalse(SlowFileExists(dir, "_a.frq"));
            }
            finally
            {
                dir.Dispose();
            }
        }

        // LUCENE-4398
        [Test]
        public virtual void TestRotatingFieldNames()
        {
            Directory dir = NewFSDirectory(CreateTempDir("TestIndexWriter.testChangingFields"
                ));
            IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
                (Random()));
            iwc.SetRAMBufferSizeMB(0.2);
            iwc.SetMaxBufferedDocs(-1);
            IndexWriter w = new IndexWriter(dir, iwc);
            int upto = 0;
            FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
            ft.OmitNorms = (true);
            int firstDocCount = -1;
            for (int iter = 0; iter < 10; iter++)
            {
                int startFlushCount = w.FlushCount;
                int docCount = 0;
                while (w.FlushCount == startFlushCount)
                {
                    Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                        ();
                    for (int i = 0; i < 10; i++)
                    {
                        doc.Add(new Field("field" + (upto++), "content", ft));
                    }
                    w.AddDocument(doc);
                    docCount++;
                }
                if (VERBOSE)
                {
                    System.Console.Out.WriteLine("TEST: iter=" + iter + " flushed after docCount=" +
                        docCount);
                }
                if (iter == 0)
                {
                    firstDocCount = docCount;
                }
                AssertTrue("flushed after too few docs: first segment flushed at docCount="
                     + firstDocCount + ", but current segment flushed after docCount=" + docCount +
                    "; iter=" + iter, ((float)docCount) / firstDocCount > 0.9);
                if (upto > 5000)
                {
                    // Start re-using field names after a while
                    // ... important because otherwise we can OOME due
                    // to too many FieldInfo instances.
                    upto = 0;
                }
            }
            w.Dispose();
            dir.Dispose();
        }

        // LUCENE-4575
        [Test]
        public virtual void TestCommitWithUserDataOnly()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
                , null));
            writer.Commit();
            // first commit to complete IW create transaction.
            // this should store the commit data, even though no other changes were made
            writer.CommitData = new Dictionary<string, string> {{"key","value1"}};
            writer.Commit();
            DirectoryReader r = DirectoryReader.Open(dir);
            AssertEquals("value", r.IndexCommit.UserData["key"]);
            r.Dispose();
            // now check setCommitData and prepareCommit/commit sequence
            writer.CommitData = new Dictionary<string, string> { { "key", "value1" } };
            writer.PrepareCommit();
            writer.CommitData = new Dictionary<string, string> { { "key", "value2" } };
            writer.Commit();
            // should commit the first commitData only, per protocol
            r = DirectoryReader.Open(dir);
            AssertEquals("value1", r.IndexCommit.UserData["key"]);
            r.Dispose();
            // now should commit the second commitData - there was a bug where 
            // IndexWriter.finishCommit overrode the second commitData
            writer.Commit();
            r = DirectoryReader.Open(dir);
            AssertEquals("IndexWriter.finishCommit may have overridden the second commitData"
                , "value2", r.IndexCommit.UserData["key"]);
            r.Dispose();
            writer.Dispose();
            dir.Dispose();
        }

       

        /// <exception cref="System.Exception"></exception>
        [NUnit.Framework.Test]
        public virtual void TestGetCommitData()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
                , null));
            writer.CommitData = new Dictionary<string, string> { { "key", "value" } };
            AssertEquals("value", writer.CommitData["key"]);
            writer.Dispose();
            // validate that it's also visible when opening a new IndexWriter
            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, null).SetOpenMode
                (IndexWriterConfig.OpenMode.APPEND));
            AssertEquals("value", writer.CommitData["key"]);
            writer.Dispose();
            dir.Dispose();
        }
        
        [Test]
        public virtual void TestNullAnalyzer()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwConf = NewIndexWriterConfig(TEST_VERSION_CURRENT, null);
            RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, iwConf);
            // add 3 good docs
            for (int i = 0; i < 3; i++)
            {
                Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                {
                    new StringField("id", i.ToString(), Field.Store.NO)
                };
                iw.AddDocument(doc);
            }
            // add broken doc
            try
            {
                Lucene.Net.Documents.Document broke = new Lucene.Net.Documents.Document
                    ();
                broke.Add(NewTextField("test", "broken", Field.Store.NO));
                iw.AddDocument(broke);
                Fail();
            }
            catch (ArgumentNullException)
            {
            }
            // ensure good docs are still ok
            IndexReader ir = iw.GetReader();
            AssertEquals(3, ir.NumDocs);
            ir.Dispose();
            iw.Close();
            dir.Dispose();
        }

        [Test]
        public virtual void TestNullDocument()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter iw = new RandomIndexWriter(Random(), dir);
            // add 3 good docs
            for (int i = 0; i < 3; i++)
            {
                var doc = new Lucene.Net.Documents.Document
                {
                    new StringField("id", i.ToString(), Field.Store.NO)
                };
                iw.AddDocument(doc);
            }
            // add broken doc
            try
            {
                iw.AddDocument((Documents.Document)null);
                Fail();
            }
            catch (ArgumentNullException)
            {
            }
            // ensure good docs are still ok
            IndexReader ir = iw.GetReader();
            AssertEquals(3, ir.NumDocs);
            ir.Dispose();
            iw.Close();
            dir.Dispose();
        }

        [Test]
        public virtual void TestNullDocuments()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter iw = new RandomIndexWriter(Random(), dir);
            // add 3 good docs
            for (int i = 0; i < 3; i++)
            {
                Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                    ();
                doc.Add(new StringField("id", i.ToString(), Field.Store.NO));
                iw.AddDocument(doc);
            }
            // add broken doc block
            try
            {
                iw.AddDocuments((IEnumerable<IEnumerable<IIndexableField>>)null);
                Fail();
            }
            catch (ArgumentNullException)
            {
            }
            // ensure good docs are still ok
            IndexReader ir = iw.GetReader();
            AssertEquals(3, ir.NumDocs);
            ir.Dispose();
            iw.Close();
            dir.Dispose();
        }

        [Test]
        public virtual void TestIterableFieldThrowsException()
        {
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new
                MockAnalyzer(Random())));
            int iters = AtLeast(100);
            int docCount = 0;
            int docId = 0;
            ICollection<string> liveIds = new HashSet<string>();
            for (int i = 0; i < iters; i++)
            {
                int numDocs = AtLeast(4);
                for (int j = 0; j < numDocs; j++)
                {
                    string id = (docId++).ToString();
                    IList<IIndexableField> fields = new List<IIndexableField>();
                    fields.Add(new StringField("id", id, Field.Store.YES));
                    fields.Add(new StringField("foo", TestUtil.RandomSimpleString(Random()), Field.Store
                        .NO));
                    docId++;
                    bool success = false;
                    try
                    {
                        w.AddDocument(new RandomFailingIterable<IIndexableField>(fields, Random
                            ()));
                        success = true;
                    }
                    catch (SystemException e)
                    {
                        AssertEquals("boom", e.Message);
                    }
                    finally
                    {
                        if (success)
                        {
                            docCount++;
                            liveIds.Add(id);
                        }
                    }
                }
            }
            DirectoryReader reader = w.Reader;
            AssertEquals(docCount, reader.NumDocs);
            IList<AtomicReaderContext> leaves = reader.Leaves;
            foreach (AtomicReaderContext atomicReaderContext in leaves)
            {
                AtomicReader ar = ((AtomicReader)atomicReaderContext.Reader);
                IBits liveDocs = ar.LiveDocs;
                int maxDoc = ar.MaxDoc;
                for (int i = 0; i < maxDoc; i++)
                {
                    if (liveDocs == null || liveDocs[i])
                    {
                        AssertTrue(liveIds.Remove(ar.Document(i).Get("id")));
                    }
                }
            }
            AssertTrue(!liveIds.Any());
            w.Dispose();
            IOUtils.Close(reader, dir);
        }

        [Test]
        public virtual void TestIterableThrowsException()
        {
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new
                MockAnalyzer(Random())));
            int iters = AtLeast(100);
            int docCount = 0;
            int docId = 0;
            ICollection<string> liveIds = new HashSet<string>();
            for (int i = 0; i < iters; i++)
            {
                var docs = new List<IEnumerable<IIndexableField>>();
                FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
                FieldType idFt = new FieldType(TextField.TYPE_STORED);
                int numDocs = AtLeast(4);
                for (int j = 0; j < numDocs; j++)
                {
                    Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                    {
                        NewField("id", string.Empty + (docId++), idFt),
                        NewField("foo", TestUtil.RandomSimpleString(Random()), ft)
                    };
                    docs.Add(doc);
                }
                bool success = false;
                try
                {
                    w.AddDocuments(new RandomFailingIterable<IEnumerable<IIndexableField>
                        >(docs, Random()));
                    success = true;
                }
                catch (SystemException e)
                {
                    AssertEquals("boom", e.Message);
                }
                finally
                {
                    if (success)
                    {
                        docCount += docs.Count;
                        foreach (var indexDocument in docs)
                        {
                            liveIds.Add(((Lucene.Net.Documents.Document)indexDocument).Get("id"));
                        }
                    }
                }
            }
            DirectoryReader reader = w.Reader;
            AssertEquals(docCount, reader.NumDocs);
            IList<AtomicReaderContext> leaves = reader.Leaves;
            foreach (AtomicReaderContext atomicReaderContext in leaves)
            {
                AtomicReader ar = ((AtomicReader)atomicReaderContext.Reader);
                IBits liveDocs = ar.LiveDocs;
                int maxDoc = ar.MaxDoc;
                for (int i = 0; i < maxDoc; i++)
                {
                    if (liveDocs == null || liveDocs[i])
                    {
                        AssertTrue(liveIds.Remove(ar.Document(i).Get("id")));
                    }
                }
            }
            AssertTrue(!liveIds.Any());
            IOUtils.Close(reader, w, dir);
        }

        [Test]
        public virtual void TestIterableThrowsException2()
        {
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new
                MockAnalyzer(Random())));
            try
            {
                w.AddDocuments(new BoomEnumerable());
            }
            catch (Exception e)
            {
                //HM:revisit 
                IsNotNull(e.Message);
                AssertEquals("boom", e.Message);
            }
            w.Dispose();
            IOUtils.Close(dir);
        }

        private sealed class BoomEnumerable : IEnumerable<Lucene.Net.Documents.Document>
        {
            public IEnumerator<Lucene.Net.Documents.Document> GetEnumerator()
            {
                return new BoomEnumerator();
            }

            private sealed class BoomEnumerator : IEnumerator<Lucene.Net.Documents.Document>
            {
                public bool MoveNext()
                {
                    return true;
                }

                public void Reset()
                {
                    throw new NotImplementedException();
                }

                object IEnumerator.Current
                {
                    get { return Current; }
                }

                public Lucene.Net.Documents.Document Current
                {
                    get { throw new SystemException("boom"); }
                }


                public void Dispose()
                {
                    throw new NotImplementedException();
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private class RandomFailingIterable<T> : IEnumerable<T>
        {
            private readonly IEnumerable<T> list;

            private readonly int failOn;

            public RandomFailingIterable(IEnumerable<T> list, Random random)
            {
                this.list = list;
                this.failOn = random.Next(5);
            }

            public IEnumerator<T> GetEnumerator()
            {
                var docIter = list.GetEnumerator();
                return new RandomFailingEnumerator(this, docIter);
            }

            private sealed class RandomFailingEnumerator : IEnumerator<T>
            {
                public RandomFailingEnumerator(RandomFailingIterable<T> _enclosing, IEnumerator<T> docIter)
                {
                    this._enclosing = _enclosing;
                    this.docIter = docIter;
                    this.count = 0;
                }

                internal int count;

                public bool MoveNext()
                {
                    return docIter.MoveNext();
                }

                public void Reset()
                {
                    throw new NotImplementedException();
                }

                object IEnumerator.Current
                {
                    get { return Current; }
                }

                public T Current
                {
                    get
                    {
                        if (this.count == this._enclosing.failOn)
                        {
                            throw new SystemException("boom");
                        }
                        this.count++;
                        return docIter.Current;
                    }
                }

                

                private readonly RandomFailingIterable<T> _enclosing;

                private readonly IEnumerator<T> docIter;
                public void Dispose()
                {
                    throw new NotImplementedException();
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        // LUCENE-2727/LUCENE-2812/LUCENE-4738:
        [Test]
        public virtual void TestCorruptFirstCommit()
        {
            for (int i = 0; i < 6; i++)
            {
                BaseDirectoryWrapper dir = NewDirectory();
                dir.CreateOutput("segments_0", IOContext.DEFAULT).Dispose();
                IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
                    (Random()));
                int mode = i / 2;
                if (mode == 0)
                {
                    iwc.SetOpenMode(IndexWriterConfig.OpenMode.CREATE);
                }
                else
                {
                    if (mode == 1)
                    {
                        iwc.SetOpenMode(IndexWriterConfig.OpenMode.APPEND);
                    }
                    else
                    {
                        if (mode == 2)
                        {
                            iwc.SetOpenMode(IndexWriterConfig.OpenMode.CREATE_OR_APPEND);
                        }
                    }
                }
                if (VERBOSE)
                {
                    System.Console.Out.WriteLine("\nTEST: i=" + i);
                }
                try
                {
                    if ((i & 1) == 0)
                    {
                        new IndexWriter(dir, iwc).Dispose();
                    }
                    else
                    {
                        new IndexWriter(dir, iwc).Rollback();
                    }
                    if (mode != 0)
                    {
                        Fail("expected exception");
                    }
                }
                catch (IOException ioe)
                {
                    // OpenMode.APPEND should throw an exception since no
                    // index exists:
                    if (mode == 0)
                    {
                        // Unexpected
                        throw;
                    }
                }
                if (VERBOSE)
                {
                    System.Console.Out.WriteLine("  at close: " + Arrays.ToString(dir.ListAll()));
                }
                if (mode != 0)
                {
                    dir.SetCheckIndexOnClose(false);
                }
                dir.Dispose();
            }
        }

        [Test]
        public virtual void TestHasUncommittedChanges()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
                , new MockAnalyzer(Random())));
            AssertTrue(writer.HasUncommittedChanges());
            // this will be true because a commit will create an empty index
            Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                ();
            doc.Add(NewTextField("myfield", "a b c", Field.Store.NO));
            writer.AddDocument(doc);
            AssertTrue(writer.HasUncommittedChanges());
            // Must commit, waitForMerges, commit again, to be
            // certain that hasUncommittedChanges returns false:
            writer.Commit();
            writer.WaitForMerges();
            writer.Commit();
            IsFalse(writer.HasUncommittedChanges());
            writer.AddDocument(doc);
            AssertTrue(writer.HasUncommittedChanges());
            writer.Commit();
            doc = new Lucene.Net.Documents.Document();
            doc.Add(NewStringField("id", "xyz", Field.Store.YES));
            writer.AddDocument(doc);
            AssertTrue(writer.HasUncommittedChanges());
            // Must commit, waitForMerges, commit again, to be
            // certain that hasUncommittedChanges returns false:
            writer.Commit();
            writer.WaitForMerges();
            writer.Commit();
            IsFalse(writer.HasUncommittedChanges());
            writer.DeleteDocuments(new Term("id", "xyz"));
            AssertTrue(writer.HasUncommittedChanges());
            // Must commit, waitForMerges, commit again, to be
            // certain that hasUncommittedChanges returns false:
            writer.Commit();
            writer.WaitForMerges();
            writer.Commit();
            IsFalse(writer.HasUncommittedChanges());
            writer.Dispose();
            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
                (Random())));
            IsFalse(writer.HasUncommittedChanges());
            writer.AddDocument(doc);
            AssertTrue(writer.HasUncommittedChanges());
            writer.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestMergeAllDeleted()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
                (Random()));
            SetOnce<IndexWriter> iwRef = new SetOnce<IndexWriter>();
            iwc.SetInfoStream(new RandomIndexWriter.TestPointInfoStream(iwc.InfoStream,new AnonTestPoint(iwRef)));
            IndexWriter evilWriter = new IndexWriter(dir, iwc);
            iwRef.Set(evilWriter);
            for (int i = 0; i < 1000; i++)
            {
                AddDoc(evilWriter);
                if (Random().Next(17) == 0)
                {
                    evilWriter.Commit();
                }
            }
            evilWriter.DeleteDocuments(new MatchAllDocsQuery());
            evilWriter.ForceMerge(1);
            evilWriter.Dispose();
            dir.Dispose();
        }

        private sealed class AnonTestPoint : RandomIndexWriter.TestPoint
        {
            public AnonTestPoint(SetOnce<IndexWriter> iwRef)
            {
                this.iwRef = iwRef;
            }

            public void Apply(string message)
            {
                if ("startCommitMerge".Equals(message))
                {
                    iwRef.Get().KeepFullyDeletedSegments = (false);
                }
                else
                {
                    if ("startMergeInit".Equals(message))
                    {
                        iwRef.Get().KeepFullyDeletedSegments = (true);
                    }
                }
            }

            private readonly SetOnce<IndexWriter> iwRef;
        }

        // LUCENE-5239
        [Test]
        public virtual void TestDeleteSameTermAcrossFields()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
                (Random()));
            IndexWriter w = new IndexWriter(dir, iwc);
            Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                ();
            doc.Add(new TextField("a", "foo", Field.Store.NO));
            w.AddDocument(doc);
            // Should not delete the document; with LUCENE-5239 the
            // "foo" from the 2nd delete term would incorrectly
            // match field a's "foo":
            w.DeleteDocuments(new Term("a", "xxx"));
            w.DeleteDocuments(new Term("b", "foo"));
            IndexReader r = w.Reader;
            w.Dispose();
            // Make sure document was not (incorrectly) deleted:
            AssertEquals(1, r.NumDocs);
            r.Dispose();
            dir.Dispose();
        }

        // LUCENE-5574
        [Test]
        public virtual void TestClosingNRTReaderDoesNotCorruptYourIndex()
        {
            // Windows disallows deleting & overwriting files still
            // open for reading:
            AssumeFalse("this test can't run on Windows", Constants.WINDOWS);
            MockDirectoryWrapper dir = NewMockDirectory();
            // Allow deletion of still open files:
            dir.SetNoDeleteOpenFile(false);
            // Allow writing to same file more than once:
            dir.SetPreventDoubleWrite(false);
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
                (Random()));
            LogMergePolicy lmp = new LogDocMergePolicy();
            lmp.MergeFactor = (2);
            iwc.SetMergePolicy(lmp);
            RandomIndexWriter w = new RandomIndexWriter(Random(), dir, iwc);
            Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                ();
            doc.Add(new TextField("a", "foo", Field.Store.NO));
            w.AddDocument(doc);
            w.Commit();
            w.AddDocument(doc);
            // Get a new reader, but this also sets off a merge:
            IndexReader r = w.GetReader();
            w.Close();
            // Blow away index and make a new writer:
            foreach (string fileName in dir.ListAll())
            {
                dir.DeleteFile(fileName);
            }
            w = new RandomIndexWriter(Random(), dir);
            w.AddDocument(doc);
            w.Close();
            r.Dispose();
            dir.Dispose();
        }
    }
}
