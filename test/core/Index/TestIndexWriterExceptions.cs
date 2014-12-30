using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Support;
using Lucene.Net.TestFramework;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.TestFramework.Analysis;
using Lucene.Net.TestFramework.Index;
using Lucene.Net.TestFramework.Util;
using Lucene.Net.Util;
using NUnit.Framework;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Test.Index
{
    [TestFixture]
    public class TestIndexWriterExceptions : LuceneTestCase
    {
        private class DocCopyIterator : IEnumerable<Lucene.Net.Documents.Document>
        {
            private readonly Lucene.Net.Documents.Document doc;

            private readonly int count;

            internal static readonly FieldType custom1 = new FieldType(TextField.TYPE_NOT_STORED
                );

            internal static readonly FieldType custom2 = new FieldType();

            public static readonly FieldType custom3 = new FieldType();

            internal static readonly FieldType custom4 = new FieldType(StringField.TYPE_NOT_STORED
                );

            internal static readonly FieldType custom5 = new FieldType(TextField.TYPE_STORED);

            static DocCopyIterator()
            {
                custom1.StoreTermVectors = true;
                custom1.StoreTermVectorPositions = true;
                custom1.StoreTermVectorOffsets = true;
                custom2.Stored = (true);
                custom2.Indexed = (true);
                custom3.Stored = (true);
                custom4.StoreTermVectors = true;
                custom4.StoreTermVectorPositions = true;
                custom4.StoreTermVectorOffsets = true;
                custom5.StoreTermVectors = true;
                custom5.StoreTermVectorPositions = true;
                custom5.StoreTermVectorOffsets = true;
            }

            public DocCopyIterator(Lucene.Net.Documents.Document doc, int count)
            {
                this.count = count;
                this.doc = doc;
            }

            public IEnumerator<Lucene.Net.Documents.Document> GetEnumerator()
            {
                return new DocumentsEnumerator(this);
            }

            private sealed class DocumentsEnumerator : IEnumerator<Lucene.Net.Documents.Document>
            {
                public DocumentsEnumerator(DocCopyIterator _enclosing)
                {
                    this._enclosing = _enclosing;
                }

                internal int upto;

                public bool MoveNext()
                {
                    return this.upto < this._enclosing.count;
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
                    get
                    {
                        this.upto++;
                        return this._enclosing.doc;
                    }
                }



                private readonly DocCopyIterator _enclosing;
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

        private class IndexerThread
        {
            internal IndexWriter writer;

            internal readonly Random r = new Random(LuceneTestCase.Random().NextInt(0, int.MaxValue));

            internal volatile Exception failure;

            public IndexerThread(TestIndexWriterExceptions _enclosing, int i, IndexWriter writer
                )
            {
                this._enclosing = _enclosing;
                Thread.CurrentThread.Name = ("Indexer " + i);
                this.writer = writer;
            }

            public void Run()
            {
                Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				{
				    NewTextField(this.r, "content1", "aaa bbb ccc ddd", Field.Store
				        .YES),
				    NewField(this.r, "content6", "aaa bbb ccc ddd", DocCopyIterator
				        .custom1),
				    NewField(this.r, "content2", "aaa bbb ccc ddd", DocCopyIterator
				        .custom2),
				    NewField(this.r, "content3", "aaa bbb ccc ddd", DocCopyIterator
				        .custom3),
				    NewTextField(this.r, "content4", "aaa bbb ccc ddd", Field.Store
				        .NO),
				    NewStringField(this.r, "content5", "aaa bbb ccc ddd", Field.Store
				        .NO)
				};
                if (DefaultCodecSupportsDocValues())
                {
                    doc.Add(new NumericDocValuesField("numericdv", 5));
                    doc.Add(new BinaryDocValuesField("binarydv", new BytesRef("hello")));
                    doc.Add(new SortedDocValuesField("sorteddv", new BytesRef("world")));
                }
                if (LuceneTestCase.DefaultCodecSupportsSortedSet())
                {
                    doc.Add(new SortedSetDocValuesField("sortedsetdv", new BytesRef("hellllo")));
                    doc.Add(new SortedSetDocValuesField("sortedsetdv", new BytesRef("again")));
                }
                doc.Add(LuceneTestCase.NewField(this.r, "content7", "aaa bbb ccc ddd", DocCopyIterator
                    .custom4));
                Field idField = LuceneTestCase.NewField(this.r, "id", string.Empty, DocCopyIterator
                    .custom2);
                doc.Add(idField);
                long stopTime = DateTime.Now.CurrentTimeMillis() + 500;
                do
                {
                    if (LuceneTestCase.VERBOSE)
                    {
                        System.Console.Out.WriteLine(Thread.CurrentThread.Name + ": TEST: IndexerThread: cycle"
                            );
                    }
                    this._enclosing.doFail.Set(Thread.CurrentThread);
                    string id = string.Empty + this.r.Next(50);
                    idField.StringValue = id;
                    Term idTerm = new Term("id", id);
                    try
                    {
                        if (this.r.NextBoolean())
                        {
                            this.writer.UpdateDocuments(idTerm, new TestIndexWriterExceptions.DocCopyIterator
                                (doc, TestUtil.NextInt(this.r, 1, 20)));
                        }
                        else
                        {
                            this.writer.UpdateDocument(idTerm, doc);
                        }
                    }
                    catch (SystemException re)
                    {
                        if (LuceneTestCase.VERBOSE)
                        {
                            System.Console.Out.WriteLine(Thread.CurrentThread.Name + ": EXC: ");
                            re.printStackTrace();

                        }
                        try
                        {
                            TestUtil.CheckIndex(this.writer.Directory);
                        }
                        catch (IOException ioe)
                        {
                            System.Console.Out.WriteLine(Thread.CurrentThread.Name + ": unexpected exception1"
                                );
                            ioe.printStackTrace();
                            this.failure = ioe;
                            break;
                        }
                    }
                    catch (Exception t)
                    {
                        System.Console.Out.WriteLine(Thread.CurrentThread.Name + ": unexpected exception2"
                            );
                        t.printStackTrace();
                        this.failure = t;
                        break;
                    }
                    this._enclosing.doFail.Set(null);
                    // After a possible exception (above) I should be able
                    // to add a new document without hitting an
                    // exception:
                    try
                    {
                        this.writer.UpdateDocument(idTerm, doc);
                    }
                    catch (Exception t)
                    {
                        System.Console.Out.WriteLine(Thread.CurrentThread.Name + ": unexpected exception3"
                            );
                        t.printStackTrace();
                        this.failure = t;
                        break;
                    }
                }
                while (DateTime.Now.CurrentTimeMillis() < stopTime);
            }

            private readonly TestIndexWriterExceptions _enclosing;
        }

        internal ThreadLocal<Thread> doFail = new ThreadLocal<Thread>();

        private class TestPoint1 : RandomIndexWriter.TestPoint
        {
            internal Random r = new Random(LuceneTestCase.Random().NextInt(0, int.MaxValue));

            public virtual void Apply(string name)
            {
                if (this._enclosing.doFail.Get() != null && !name.Equals("startDoFlush") && this.
                    r.Next(40) == 17)
                {
                    if (LuceneTestCase.VERBOSE)
                    {
                        System.Console.Out.WriteLine(Thread.CurrentThread.Name + ": NOW FAIL: "
                             + name);
                        new Exception().printStackTrace();
                    }
                    throw new SystemException(Thread.CurrentThread.Name + ": intentionally failing at "
                         + name);
                }
            }

            internal TestPoint1(TestIndexWriterExceptions _enclosing)
            {
                this._enclosing = _enclosing;
            }

            private readonly TestIndexWriterExceptions _enclosing;
        }

        [Test]
        public virtual void TestRandomExceptions()
        {
            if (VERBOSE)
            {
                System.Console.Out.WriteLine("\nTEST: start testRandomExceptions");
            }
            Directory dir = NewDirectory();
            MockAnalyzer analyzer = new MockAnalyzer(Random());
            analyzer.SetEnableChecks(false);
            // disable workflow checking as we forcefully close() in exceptional cases.
            IndexWriter writer = RandomIndexWriter.MockIndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
                (TEST_VERSION_CURRENT, analyzer).SetRAMBufferSizeMB(0.1)).SetMergeScheduler(new
                ConcurrentMergeScheduler()), new TestPoint1(this));
            ((ConcurrentMergeScheduler)writer.Config.MergeScheduler).SetSuppressExceptions
                ();
            //writer.setMaxBufferedDocs(10);
            if (VERBOSE)
            {
                System.Console.Out.WriteLine("TEST: initial commit");
            }
            writer.Commit();
            var thread = new IndexerThread(this, 0, writer);
            thread.Run();
            if (thread.failure != null)
            {
                thread.failure.printStackTrace();
                Fail("thread " + thread + ": hit unexpected failure"
                    );
            }
            if (VERBOSE)
            {
                System.Console.Out.WriteLine("TEST: commit after thread start");
            }
            writer.Commit();
            try
            {
                writer.Dispose();
            }
            catch (Exception t)
            {
                System.Console.Out.WriteLine("exception during close:");
                t.printStackTrace();
                writer.Rollback();
            }
            // Confirm that when doc hits exception partway through tokenization, it's deleted:
            IndexReader r2 = DirectoryReader.Open(dir);
            int count = r2.DocFreq(new Term("content4", "aaa"));
            int count2 = r2.DocFreq(new Term("content4", "ddd"));
            AssertEquals(count, count2);
            r2.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestRandomExceptionsThreads()
        {
            Directory dir = NewDirectory();
            MockAnalyzer analyzer = new MockAnalyzer(Random());
            analyzer.SetEnableChecks(false);
            // disable workflow checking as we forcefully close() in exceptional cases.
            IndexWriter writer = RandomIndexWriter.MockIndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
                (TEST_VERSION_CURRENT, analyzer).SetRAMBufferSizeMB(0.2)).SetMergeScheduler(new
                ConcurrentMergeScheduler()), new TestIndexWriterExceptions.TestPoint1(this));
            ((ConcurrentMergeScheduler)writer.Config.MergeScheduler).SetSuppressExceptions
                ();
            //writer.setMaxBufferedDocs(10);
            writer.Commit();
            int NUM_THREADS = 4;
            var threads = new Thread[NUM_THREADS];
            var indexerThreads = new IndexerThread[NUM_THREADS];

            for (int i = 0; i < NUM_THREADS; i++)
            {
                var indexerThread = new IndexerThread(this, i, writer);
                threads[i] = new Thread(indexerThread.Run);
                indexerThreads[i] = indexerThread;
                threads[i].Start();
            }
            for (int x = 0; x < NUM_THREADS; x++)
            {
                threads[x].Join();
            }
            for (int y = 0; y < NUM_THREADS; y++)
            {
                if (indexerThreads[y].failure != null)
                {
                    Fail("thread " + threads[y].Name + ": hit unexpected failure"
                        );
                }
            }
            writer.Commit();
            try
            {
                writer.Dispose();
            }
            catch (Exception t)
            {
                System.Console.Out.WriteLine("exception during close:");
                t.printStackTrace();
                writer.Rollback();
            }
            // Confirm that when doc hits exception partway through tokenization, it's deleted:
            IndexReader r2 = DirectoryReader.Open(dir);
            int count = r2.DocFreq(new Term("content4", "aaa"));
            int count2 = r2.DocFreq(new Term("content4", "ddd"));
            AssertEquals(count, count2);
            r2.Dispose();
            dir.Dispose();
        }

        private sealed class TestPoint2 : RandomIndexWriter.TestPoint
        {
            internal bool doFail;

            // LUCENE-1198
            public void Apply(string name)
            {
                if (doFail && name.Equals("DocumentsWriterPerThread addDocument start"))
                {
                    throw new SystemException("intentionally failing");
                }
            }
        }

        private static string CRASH_FAIL_MESSAGE = "I'm experiencing problems";

        private class CrashingFilter : TokenFilter
        {
            internal string fieldName;

            internal int count;

            public CrashingFilter(TestIndexWriterExceptions _enclosing, string fieldName, TokenStream
                 input)
                : base(input)
            {
                this._enclosing = _enclosing;
                this.fieldName = fieldName;
            }

            /// <exception cref="System.IO.IOException"></exception>
            public override bool IncrementToken()
            {
                if (this.fieldName.Equals("crash") && this.count++ >= 4)
                {
                    throw new IOException(TestIndexWriterExceptions.CRASH_FAIL_MESSAGE);
                }
                return this.input.IncrementToken();
            }

            /// <exception cref="System.IO.IOException"></exception>
            public override void Reset()
            {
                base.Reset();
                this.count = 0;
            }

            private readonly TestIndexWriterExceptions _enclosing;
        }

        [Test]
        public virtual void TestExceptionDocumentsWriterInit()
        {
            Directory dir = NewDirectory();
            var testPoint = new TestPoint2();
            IndexWriter w = RandomIndexWriter.MockIndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
                , new MockAnalyzer(Random())), testPoint);
            var doc = new Lucene.Net.Documents.Document
			{
			    NewTextField("field", "a field", Field.Store.YES)
			};
            w.AddDocument(doc);
            testPoint.doFail = true;
            try
            {
                w.AddDocument(doc);
                Fail("did not hit exception");
            }
            catch (SystemException)
            {
            }
            // expected
            w.Dispose();
            dir.Dispose();
        }

        // LUCENE-1208
        [Test]
        public virtual void TestExceptionJustBeforeFlush()
        {
            Directory dir = NewDirectory();
            IndexWriter w = RandomIndexWriter.MockIndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
                (TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2)), new TestIndexWriterExceptions.TestPoint1
                (this));
            var doc = new Lucene.Net.Documents.Document { NewTextField("field", "a field", Field.Store.YES) };
            w.AddDocument(doc);
            Analyzer analyzer = new AnonymousAnalyzer(Analyzer.PER_FIELD_REUSE_STRATEGY, this);
            // disable workflow checking as we forcefully close() in exceptional cases.
            var crashDoc = new Lucene.Net.Documents.Document { NewTextField("crash", "do it on token 4", Field.Store.YES) };
            try
            {
                w.AddDocument(crashDoc, analyzer);
                Fail("did not hit expected exception");
            }
            catch (IOException)
            {
            }
            // expected
            w.AddDocument(doc);
            w.Dispose();
            dir.Dispose();
        }

        private sealed class AnonymousAnalyzer : Analyzer
        {
            private TestIndexWriterExceptions parent;

            public AnonymousAnalyzer(ReuseStrategy baseArg1, TestIndexWriterExceptions testIndexWriterExceptions)
                : base(baseArg1)
            {
                parent = testIndexWriterExceptions;
            }

            public override Analyzer.TokenStreamComponents CreateComponents(string fieldName
                , TextReader reader)
            {
                MockTokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false
                    );
                tokenizer.setEnableChecks(false);
                return new TokenStreamComponents(tokenizer, new CrashingFilter(parent, fieldName, tokenizer));
            }
        }

        private sealed class TestPoint3 : RandomIndexWriter.TestPoint
        {
            internal bool doFail;

            internal bool failed;

            public void Apply(string name)
            {
                if (doFail && name.Equals("startMergeInit"))
                {
                    failed = true;
                    throw new SystemException("intentionally failing");
                }
            }
        }

        // LUCENE-1210
        [Test]
        public virtual void TestExceptionOnMergeInit()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig conf = ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
                , new MockAnalyzer(Random())).SetMaxBufferedDocs(2)).SetMergePolicy(NewLogMergePolicy
                ());
            ConcurrentMergeScheduler cms = new ConcurrentMergeScheduler();
            cms.SetSuppressExceptions();
            conf.SetMergeScheduler(cms);
            ((LogMergePolicy)conf.MergePolicy).MergeFactor = (2);
            var testPoint = new TestPoint3();
            IndexWriter w = RandomIndexWriter.MockIndexWriter(dir, conf, testPoint);
            testPoint.doFail = true;
            var doc = new Lucene.Net.Documents.Document { NewTextField("field", "a field", Field.Store.YES) };
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    w.AddDocument(doc);
                }
                catch (SystemException)
                {
                    break;
                }
            }
            ((ConcurrentMergeScheduler)w.Config.MergeScheduler).Sync();
            AssertTrue(testPoint.failed);
            w.Dispose();
            dir.Dispose();
        }

        // LUCENE-1072
        [Test]
        public virtual void TestExceptionFromTokenStream()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new AnonymousAnalyzer2
                ());
            // disable workflow checking as we forcefully close() in exceptional cases.
            conf.SetMaxBufferedDocs(Math.Max(3, conf.MaxBufferedDocs));
            IndexWriter writer = new IndexWriter(dir, conf);
            Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                ();
            string contents = "aa bb cc dd ee ff gg hh ii jj kk";
            doc.Add(NewTextField("content", contents, Field.Store.NO));
            try
            {
                writer.AddDocument(doc);
                Fail("did not hit expected exception");
            }
            catch (Exception)
            {
            }
            // Make sure we can add another normal document
            doc = new Lucene.Net.Documents.Document();
            doc.Add(NewTextField("content", "aa bb cc dd", Field.Store.NO));
            writer.AddDocument(doc);
            // Make sure we can add another normal document
            doc = new Lucene.Net.Documents.Document();
            doc.Add(NewTextField("content", "aa bb cc dd", Field.Store.NO));
            writer.AddDocument(doc);
            writer.Dispose();
            IndexReader reader = DirectoryReader.Open(dir);
            Term t = new Term("content", "aa");
            AssertEquals(3, reader.DocFreq(t));
            // Make sure the doc that hit the exception was marked
            // as deleted:
            DocsEnum tdocs = TestUtil.Docs(Random(), reader, t.Field, new BytesRef(t.Text
                ), MultiFields.GetLiveDocs(reader), null, 0);
            int count = 0;
            while (tdocs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
            {
                count++;
            }
            AssertEquals(2, count);
            AssertEquals(reader.DocFreq(new Term("content", "gg")), 0);
            reader.Dispose();
            dir.Dispose();
        }

        private sealed class AnonymousAnalyzer2 : Analyzer
        {
            public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                MockTokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.SIMPLE, true);
                tokenizer.setEnableChecks(false);
                return new Analyzer.TokenStreamComponents(tokenizer, new AnonymousTokenFilter(tokenizer
                    ));
            }

            private sealed class AnonymousTokenFilter : TokenFilter
            {
                public AnonymousTokenFilter(TokenStream baseArg1)
                    : base(baseArg1)
                {
                    this.count = 0;
                }

                private int count;

                /// <exception cref="System.IO.IOException"></exception>
                public override bool IncrementToken()
                {
                    if (this.count++ == 5)
                    {
                        throw new IOException();
                    }
                    return this.input.IncrementToken();
                }

                /// <exception cref="System.IO.IOException"></exception>
                public override void Reset()
                {
                    base.Reset();
                    this.count = 0;
                }
            }
        }

        private class FailOnlyOnFlush : MockDirectoryWrapper.Failure
        {
            internal bool doFail = false;

            internal int count;

            public override void SetDoFail()
            {
                this.doFail = true;
            }

            public override void ClearDoFail()
            {
                this.doFail = false;
            }

            /// <exception cref="System.IO.IOException"></exception>
            public override void Eval(MockDirectoryWrapper dir)
            {
                if (doFail)
                {

                    bool sawAppend = false;
                    bool sawFlush = false;
                    var trace = new StackTrace(new Exception()).GetFrames();

                    for (int i = 0; i < trace.Length; i++)
                    {
                        if (sawAppend && sawFlush)
                        {
                            break;
                        }
                        if (typeof(FreqProxTermsWriterPerField).FullName.Equals(trace[i].GetMethod().DeclaringType.FullName) &&
                            "flush".equals(trace[i].GetMethod().Name))
                        {
                            sawAppend = true;
                        }
                        if ("flush".equals(trace[i].GetMethod().Name))
                        {
                            sawFlush = true;
                        }
                    }

                    if (sawAppend && sawFlush && count++ >= 30)
                    {
                        doFail = false;
                        throw new IOException("now failing during flush");
                    }
                }
            }
        }

        // LUCENE-1072: make sure an errant exception on flushing
        // one segment only takes out those docs in that one flush
        [Test]
        public virtual void TestDocumentsWriterAbort()
        {
            MockDirectoryWrapper dir = NewMockDirectory();
            var failure = new FailOnlyOnFlush();
            failure.SetDoFail();
            dir.FailOn(failure);
            IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
                (TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2)));
            Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                ();
            string contents = "aa bb cc dd ee ff gg hh ii jj kk";
            doc.Add(NewTextField("content", contents, Field.Store.NO));
            bool hitError = false;
            for (int i = 0; i < 200; i++)
            {
                try
                {
                    writer.AddDocument(doc);
                }
                catch (IOException)
                {
                    // only one flush should fail:
                    IsFalse(hitError);
                    hitError = true;
                }
            }
            AssertTrue(hitError);
            writer.Dispose();
            IndexReader reader = DirectoryReader.Open(dir);
            AssertEquals(198, reader.DocFreq(new Term("content", "aa")));
            reader.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestDocumentsWriterExceptions()
        {
            Analyzer analyzer = new AnonymousAnalyzer3(Analyzer.PER_FIELD_REUSE_STRATEGY, this);
            // disable workflow checking as we forcefully close() in exceptional cases.
            for (int i = 0; i < 2; i++)
            {
                if (VERBOSE)
                {
                    System.Console.Out.WriteLine("TEST: cycle i=" + i);
                }
                Directory dir = NewDirectory();
                IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
                    , analyzer).SetMergePolicy(NewLogMergePolicy()));
                // don't allow a sudden merge to clean up the deleted
                // doc below:
                LogMergePolicy lmp = (LogMergePolicy)writer.Config.MergePolicy;
                lmp.MergeFactor = (Math.Max(lmp.MergeFactor, 5));
                Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				{
				    NewField("contents", "here are some contents", DocCopyIterator.custom5),
				    NewField("crash", "this should crash after 4 terms", DocCopyIterator.custom5),
				    NewField("other", "this will not get indexed", DocCopyIterator.custom5)
				};
                writer.AddDocument(doc);
                writer.AddDocument(doc);
                try
                {
                    writer.AddDocument(doc);
                    Fail("did not hit expected exception");
                }
                catch (IOException ioe)
                {
                    if (VERBOSE)
                    {
                        System.Console.Out.WriteLine("TEST: hit expected exception");
                        ioe.printStackTrace();
                    }
                }
                if (0 == i)
                {
                    doc = new Lucene.Net.Documents.Document
					{
					    NewField("contents", "here are some contents", TestIndexWriterExceptions.DocCopyIterator
					        .custom5)
					};
                    writer.AddDocument(doc);
                    writer.AddDocument(doc);
                }
                writer.Dispose();
                if (VERBOSE)
                {
                    System.Console.Out.WriteLine("TEST: open reader");
                }
                IndexReader reader = DirectoryReader.Open(dir);
                if (i == 0)
                {
                    int expected = 5;
                    AssertEquals(expected, reader.DocFreq(new Term("contents", "here"
                        )));
                    AssertEquals(expected, reader.MaxDoc);
                    int numDel = 0;
                    IBits liveDocs = MultiFields.GetLiveDocs(reader);
                    IsNotNull(liveDocs);
                    for (int j = 0; j < reader.MaxDoc; j++)
                    {
                        if (!liveDocs[j])
                        {
                            numDel++;
                        }
                        else
                        {
                            reader.Document(j);
                            reader.GetTermVectors(j);
                        }
                    }
                    AssertEquals(1, numDel);
                }
                reader.Dispose();
                writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
                    , analyzer).SetMaxBufferedDocs(10)));
                doc = new Lucene.Net.Documents.Document { NewField("contents", "here are some contents", DocCopyIterator.custom5) };
                for (int j_1 = 0; j_1 < 17; j_1++)
                {
                    writer.AddDocument(doc);
                }
                writer.ForceMerge(1);
                writer.Dispose();
                reader = DirectoryReader.Open(dir);
                int expected_1 = 19 + (1 - i) * 2;
                AssertEquals(expected_1, reader.DocFreq(new Term("contents", "here"
                    )));
                AssertEquals(expected_1, reader.MaxDoc);
                int numDel_1 = 0;
                IsNull(MultiFields.GetLiveDocs(reader));
                for (int j_2 = 0; j_2 < reader.MaxDoc; j_2++)
                {
                    reader.Document(j_2);
                    reader.GetTermVectors(j_2);
                }
                reader.Dispose();
                AssertEquals(0, numDel_1);
                dir.Dispose();
            }
        }

        private sealed class AnonymousAnalyzer3 : Analyzer
        {
            private TestIndexWriterExceptions parent;

            public AnonymousAnalyzer3(ReuseStrategy baseArg1, TestIndexWriterExceptions testIndexWriterExceptions)
                : base(baseArg1)
            {
                this.parent = testIndexWriterExceptions;
            }

            public override TokenStreamComponents CreateComponents(string fieldName
                , TextReader reader)
            {
                MockTokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false
                    );
                tokenizer.setEnableChecks(false);
                return new Analyzer.TokenStreamComponents(tokenizer, new TestIndexWriterExceptions.CrashingFilter
                    (parent, fieldName, tokenizer));
            }
        }

        [Test]
        public virtual void TestDocumentsWriterExceptionThreads()
        {
            Analyzer analyzer = new AnonymousAnalyzer4(Analyzer.PER_FIELD_REUSE_STRATEGY, this);
            // disable workflow checking as we forcefully close() in exceptional cases.
            int NUM_THREAD = 3;
            int NUM_ITER = 100;
            for (int i = 0; i < 2; i++)
            {
                Directory dir = NewDirectory();
                {
                    IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
                        (TEST_VERSION_CURRENT, analyzer).SetMaxBufferedDocs(-1)).SetMergePolicy(Random()
                        .NextBoolean() ? NoMergePolicy.COMPOUND_FILES : NoMergePolicy.NO_COMPOUND_FILES)
                        );
                    // don't use a merge policy here they depend on the DWPThreadPool and its max thread states etc.
                    int finalI = i;
                    Thread[] threads = new Thread[NUM_THREAD];
                    for (int t = 0; t < NUM_THREAD; t++)
                    {
                        threads[t] = new Thread(new NewFieldThread(NUM_ITER, writer, finalI).Run);
                        threads[t].Start();
                    }
                    for (int t_1 = 0; t_1 < NUM_THREAD; t_1++)
                    {
                        threads[t_1].Join();
                    }
                    writer.Dispose();
                }
                IndexReader reader = DirectoryReader.Open(dir);
                int expected = (3 + (1 - i) * 2) * NUM_THREAD * NUM_ITER;
                AssertEquals("i=" + i, expected, reader.DocFreq(new Term("contents"
                    , "here")));
                AssertEquals(expected, reader.MaxDoc);
                int numDel = 0;
                IBits liveDocs = MultiFields.GetLiveDocs(reader);
                IsNotNull(liveDocs);
                for (int j = 0; j < reader.MaxDoc; j++)
                {
                    if (!liveDocs[j])
                    {
                        numDel++;
                    }
                    else
                    {
                        reader.Document(j);
                        reader.GetTermVectors(j);
                    }
                }
                reader.Dispose();
                AssertEquals(NUM_THREAD * NUM_ITER, numDel);
                IndexWriter writer_1 = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
                    (TEST_VERSION_CURRENT, analyzer).SetMaxBufferedDocs(10)));
                Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				{
				    NewField("contents", "here are some contents", DocCopyIterator
				        .custom5)
				};
                for (int j_1 = 0; j_1 < 17; j_1++)
                {
                    writer_1.AddDocument(doc);
                }
                writer_1.ForceMerge(1);
                writer_1.Dispose();
                reader = DirectoryReader.Open(dir);
                expected += 17 - NUM_THREAD * NUM_ITER;
                AssertEquals(expected, reader.DocFreq(new Term("contents", "here"
                    )));
                AssertEquals(expected, reader.MaxDoc);
                IsNull(MultiFields.GetLiveDocs(reader));
                for (int j_2 = 0; j_2 < reader.MaxDoc; j_2++)
                {
                    reader.Document(j_2);
                    reader.GetTermVectors(j_2);
                }
                reader.Dispose();
                dir.Dispose();
            }
        }

        private sealed class AnonymousAnalyzer4 : Analyzer
        {
            private TestIndexWriterExceptions parent;

            public AnonymousAnalyzer4(ReuseStrategy baseArg1, TestIndexWriterExceptions testIndexWriterExceptions)
                : base(baseArg1)
            {
                this.parent = testIndexWriterExceptions;
            }

            public override Analyzer.TokenStreamComponents CreateComponents(string fieldName
                , TextReader reader)
            {
                MockTokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false
                    );
                tokenizer.setEnableChecks(false);
                return new Analyzer.TokenStreamComponents(tokenizer, new TestIndexWriterExceptions.CrashingFilter
                    (parent, fieldName, tokenizer));
            }
        }

        private sealed class NewFieldThread
        {
            public NewFieldThread(int NUM_ITER, IndexWriter writer, int finalI)
            {
                this.NUM_ITER = NUM_ITER;
                this.writer = writer;
                this.finalI = finalI;
            }

            public void Run()
            {
                try
                {
                    for (int iter = 0; iter < NUM_ITER; iter++)
                    {
                        var doc = new Lucene.Net.Documents.Document
						{
						    NewField("contents", "here are some contents", DocCopyIterator.custom5),
						    NewField("crash", "this should crash after 4 terms", DocCopyIterator.custom5),
						    NewField("other", "this will not get indexed", DocCopyIterator.custom5)
						};
                        writer.AddDocument(doc);
                        writer.AddDocument(doc);
                        try
                        {
                            writer.AddDocument(doc);
                            Fail("did not hit expected exception");
                        }
                        catch (IOException)
                        {
                        }
                        if (0 == finalI)
                        {
                            doc = new Lucene.Net.Documents.Document();
                            doc.Add(LuceneTestCase.NewField("contents", "here are some contents", TestIndexWriterExceptions.DocCopyIterator
                                .custom5));
                            writer.AddDocument(doc);
                            writer.AddDocument(doc);
                        }
                    }
                }
                catch (Exception t)
                {
                    lock (this)
                    {
                        System.Console.Out.WriteLine(Thread.CurrentThread.Name + ": ERROR: hit unexpected exception"
                            );
                        t.printStackTrace();
                    }
                    Fail();
                }
            }

            private readonly int NUM_ITER;

            private readonly IndexWriter writer;

            private readonly int finalI;
        }

        private class FailOnlyInSync : MockDirectoryWrapper.Failure
        {
            internal bool didFail;

            // Throws IOException during MockDirectoryWrapper.sync
            /// <exception cref="System.IO.IOException"></exception>
            public override void Eval(MockDirectoryWrapper dir)
            {
                if (doFail)
                {
                    var trace = new StackTrace(new Exception()).GetFrames();
                    for (int i = 0; i < trace.Length; i++)
                    {
                        if (doFail && typeof(MockDirectoryWrapper).FullName.Equals(trace[i].GetMethod().DeclaringType.FullName) &&
                            "sync".Equals(trace[i].GetMethod().Name))
                        {
                            didFail = true;
                            if (VERBOSE)
                            {
                                System.Console.Out.WriteLine("TEST: now throw exc:");
                                new Exception().printStackTrace();
                            }
                            throw new IOException("now failing on purpose during sync");
                        }
                    }
                }
            }
        }

        // TODO: these are also in TestIndexWriter... add a simple doc-writing method
        // like this to LuceneTestCase?
        /// <exception cref="System.IO.IOException"></exception>
        private void AddDoc(IndexWriter writer)
        {
            Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                ();
            doc.Add(NewTextField("content", "aaa", Field.Store.NO));
            writer.AddDocument(doc);
        }

        // LUCENE-1044: test exception during sync
        [Test]
        public virtual void TestExceptionDuringSync()
        {
            MockDirectoryWrapper dir = NewMockDirectory();
            var failure = new FailOnlyInSync();
            dir.FailOn(failure);
            IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
                (TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2)).SetMergeScheduler
                (new ConcurrentMergeScheduler()).SetMergePolicy(NewLogMergePolicy(5)));
            failure.SetDoFail();
            for (int i = 0; i < 23; i++)
            {
                AddDoc(writer);
                if ((i - 1) % 2 == 0)
                {
                    try
                    {
                        writer.Commit();
                    }
                    catch (IOException)
                    {
                    }
                }
            }
            // expected
            ((ConcurrentMergeScheduler)writer.Config.MergeScheduler).Sync();
            AssertTrue(failure.didFail);
            failure.ClearDoFail();
            writer.Dispose();
            IndexReader reader = DirectoryReader.Open(dir);
            AssertEquals(23, reader.NumDocs);
            reader.Dispose();
            dir.Dispose();
        }

        private class FailOnlyInCommit : MockDirectoryWrapper.Failure
        {
            internal bool failOnCommit;

            internal bool failOnDeleteFile;

            private readonly bool dontFailDuringGlobalFieldMap;

            internal static readonly string PREPARE_STAGE = "prepareCommit";

            internal static readonly string FINISH_STAGE = "finishCommit";

            private readonly string stage;

            public FailOnlyInCommit(bool dontFailDuringGlobalFieldMap, string stage)
            {
                this.dontFailDuringGlobalFieldMap = dontFailDuringGlobalFieldMap;
                this.stage = stage;
            }

            /// <exception cref="System.IO.IOException"></exception>
            public override void Eval(MockDirectoryWrapper dir)
            {
                var trace = new StackTrace(new Exception()).GetFrames();
                bool isCommit = false;
                bool isDelete = false;
                bool isInGlobalFieldMap = false;
                for (int i = 0; i < trace.Length; i++)
                {
                    if (isCommit && isDelete && isInGlobalFieldMap)
                    {
                        break;
                    }
                    if (typeof(SegmentInfos).FullName.Equals(trace[i].GetMethod().DeclaringType.FullName) && stage.Equals
                        (trace[i].GetMethod().Name))
                    {
                        isCommit = true;
                    }
                    if (typeof(MockDirectoryWrapper).FullName.Equals(trace[i].GetMethod().DeclaringType.FullName) && "deleteFile"
                        .Equals(trace[i].GetMethod().Name))
                    {
                        isDelete = true;
                    }
                    if (typeof(SegmentInfos).FullName.Equals(trace[i].GetMethod().DeclaringType.FullName) && "writeGlobalFieldMap"
                        .Equals(trace[i].GetMethod().Name))
                    {
                        isInGlobalFieldMap = true;
                    }
                }
                if (isInGlobalFieldMap && dontFailDuringGlobalFieldMap)
                {
                    isCommit = false;
                }
                if (isCommit)
                {
                    if (!isDelete)
                    {
                        failOnCommit = true;
                        throw new SystemException("now fail first");
                    }
                    failOnDeleteFile = true;
                    throw new IOException("now fail during delete");
                }
            }
        }

        [Test]
        public virtual void TestExceptionsDuringCommit()
        {
            var failures = new[] { new FailOnlyInCommit(false, FailOnlyInCommit.PREPARE_STAGE), 
                                   new FailOnlyInCommit(true, FailOnlyInCommit.PREPARE_STAGE), 
                                   new FailOnlyInCommit(false, FailOnlyInCommit.FINISH_STAGE) };
            // LUCENE-1214
            // fail during global field map is written
            // fail after global field map is written
            // fail while running finishCommit    
            foreach (TestIndexWriterExceptions.FailOnlyInCommit failure in failures)
            {
                MockDirectoryWrapper dir = NewMockDirectory();
                dir.SetFailOnCreateOutput(false);
                IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new
                    MockAnalyzer(Random())));
                Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                {
                    NewTextField("field", "a field", Field.Store.YES)
                };
                w.AddDocument(doc);
                dir.FailOn(failure);
                try
                {
                    w.Dispose();
                    Fail();
                }
                catch (IOException)
                {
                    Fail("expected only SystemException");
                }
                catch (SystemException)
                {
                }
                // Expected
                AssertTrue(failure.failOnCommit && failure.failOnDeleteFile);
                w.Rollback();
                string[] files = dir.ListAll();
                AssertTrue(files.Length == 0 || Arrays.Equals(files, new string
                    [] { IndexWriter.WRITE_LOCK_NAME }));
                dir.Dispose();
            }
        }

        [Test]
        public virtual void TestForceMergeExceptions()
        {
            Directory startDir = NewDirectory();
            IndexWriterConfig conf = ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
                , new MockAnalyzer(Random())).SetMaxBufferedDocs(2)).SetMergePolicy(NewLogMergePolicy
                ());
            ((LogMergePolicy)conf.MergePolicy).MergeFactor = (100);
            IndexWriter w = new IndexWriter(startDir, conf);
            for (int i = 0; i < 27; i++)
            {
                AddDoc(w);
            }
            w.Dispose();
            int iter = TEST_NIGHTLY ? 200 : 10;
            for (int i_1 = 0; i_1 < iter; i_1++)
            {
                if (VERBOSE)
                {
                    System.Console.Out.WriteLine("TEST: iter " + i_1);
                }
                MockDirectoryWrapper dir = new MockDirectoryWrapper(Random(), new RAMDirectory(startDir
                    , NewIOContext(Random())));
                conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMergeScheduler
                    (new ConcurrentMergeScheduler());
                ((ConcurrentMergeScheduler)conf.MergeScheduler).SetSuppressExceptions();
                w = new IndexWriter(dir, conf);
                dir.SetRandomIOExceptionRate(0.5);
                try
                {
                    w.ForceMerge(1);
                }
                catch (IOException ioe)
                {
                    if (ioe.InnerException == null)
                    {
                        Fail("forceMerge threw IOException without root cause");
                    }
                }
                dir.SetRandomIOExceptionRate(0);
                w.Dispose();
                dir.Dispose();
            }
            startDir.Dispose();
        }

        // LUCENE-1429
        [Test]
        public virtual void TestOutOfMemoryErrorCausesCloseToFail()
        {
            AtomicBoolean thrown = new AtomicBoolean(false);
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
                , new MockAnalyzer(Random())).SetInfoStream(new AnonymousInfoStream(thrown)));
            try
            {
                writer.Dispose();
                Fail("OutOfMemoryError expected");
            }
            catch (OutOfMemoryException)
            {
            }
            // throws IllegalStateEx w/o bug fix
            writer.Dispose();
            dir.Dispose();
        }

        private sealed class AnonymousInfoStream : InfoStream
        {
            public AnonymousInfoStream(AtomicBoolean thrown)
            {
                this.thrown = thrown;
            }

            public override void Message(string component, string message)
            {
                if (message.StartsWith("now flush at close") && thrown.CompareAndSet(false, true))
                {
                    throw new OutOfMemoryException("fake OOME at " + message);
                }
            }

            public override bool IsEnabled(string component)
            {
                return true;
            }

            

            private readonly AtomicBoolean thrown;
        }

        private sealed class TestPoint4 : RandomIndexWriter.TestPoint
        {
            internal bool doFail;

            // LUCENE-1347
            public void Apply(string name)
            {
                if (doFail && name.Equals("rollback before checkpoint"))
                {
                    throw new SystemException("intentionally failing");
                }
            }
        }

        // LUCENE-1347
        [Test]
        public virtual void TestRollbackExceptionHang()
        {
            Directory dir = NewDirectory();
            var testPoint = new TestPoint4();
            IndexWriter w = RandomIndexWriter.MockIndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
                , new MockAnalyzer(Random())), testPoint);
            AddDoc(w);
            testPoint.doFail = true;
            try
            {
                w.Rollback();
                Fail("did not hit intentional SystemException");
            }
            catch (SystemException)
            {
            }
            // expected
            testPoint.doFail = false;
            w.Rollback();
            dir.Dispose();
        }

        // LUCENE-1044: Simulate checksum error in segments_N
        [Test]
        public virtual void TestSegmentsChecksumError()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = null;
            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
                (Random())));
            // add 100 documents
            for (int i = 0; i < 100; i++)
            {
                AddDoc(writer);
            }
            // close
            writer.Dispose();
            long gen = SegmentInfos.GetLastCommitGeneration(dir);
            AssertTrue("segment generation should be > 0 but got " + gen,
                gen > 0);
            string segmentsFileName = SegmentInfos.GetLastCommitSegmentsFileName(dir);
            IndexInput @in = dir.OpenInput(segmentsFileName, NewIOContext(Random()));
            IndexOutput @out = dir.CreateOutput(IndexFileNames.FileNameFromGeneration(IndexFileNames
                .SEGMENTS, string.Empty, 1 + gen), NewIOContext(Random()));
            @out.CopyBytes(@in, @in.Length - 1);
            byte b = @in.ReadByte();
            @out.WriteByte(unchecked((byte)(1 + b)));
            @out.Dispose();
            @in.Dispose();
            IndexReader reader = null;
            try
            {
                reader = DirectoryReader.Open(dir);
            }
            catch (IOException e)
            {
                e.printStackTrace();
                Fail("segmentInfos failed to retry fallback to correct segments_N file"
                    );
            }
            reader.Dispose();
            // should remove the corrumpted segments_N
            new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, null)).Dispose();
            dir.Dispose();
        }

        // Simulate a corrupt index by removing last byte of
        // latest segments file and make sure we get an
        // IOException trying to open the index:
        [Test]
        public virtual void TestSimulatedCorruptIndex1()
        {
            BaseDirectoryWrapper dir = NewDirectory();
            dir.SetCheckIndexOnClose(false);
            // we are corrupting it!
            IndexWriter writer = null;
            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
                (Random())));
            // add 100 documents
            for (int i = 0; i < 100; i++)
            {
                AddDoc(writer);
            }
            // close
            writer.Dispose();
            long gen = SegmentInfos.GetLastCommitGeneration(dir);
            AssertTrue("segment generation should be > 0 but got " + gen,
                gen > 0);
            string fileNameIn = SegmentInfos.GetLastCommitSegmentsFileName(dir);
            string fileNameOut = IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS
                , string.Empty, 1 + gen);
            IndexInput @in = dir.OpenInput(fileNameIn, NewIOContext(Random()));
            IndexOutput @out = dir.CreateOutput(fileNameOut, NewIOContext(Random()));
            long length = @in.Length;
            for (int i_1 = 0; i_1 < length - 1; i_1++)
            {
                @out.WriteByte(@in.ReadByte());
            }
            @in.Dispose();
            @out.Dispose();
            dir.DeleteFile(fileNameIn);
            IndexReader reader = null;
            try
            {
                reader = DirectoryReader.Open(dir);
                Fail("reader did not hit IOException on opening a corrupt index"
                    );
            }
            catch (Exception)
            {
            }
            if (reader != null)
            {
                reader.Dispose();
            }
            dir.Dispose();
        }

        // Simulate a corrupt index by removing one of the cfs
        // files and make sure we get an IOException trying to
        // open the index:
        [Test]
        public virtual void TestSimulatedCorruptIndex2()
        {
            BaseDirectoryWrapper dir = NewDirectory();
            dir.SetCheckIndexOnClose(false);
            // we are corrupting it!
            IndexWriter writer = null;
            var indexWriterConfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            indexWriterConfig.SetMergePolicy(NewLogMergePolicy(true)).UseCompoundFile = (true);
            writer = new IndexWriter(dir, indexWriterConfig);
            MergePolicy lmp = writer.Config.MergePolicy;
            // Force creation of CFS:
            lmp.SetNoCFSRatio(1.0);
            lmp.SetMaxCFSSegmentSizeMB(double.PositiveInfinity);
            // add 100 documents
            for (int i = 0; i < 100; i++)
            {
                AddDoc(writer);
            }
            // close
            writer.Dispose();
            long gen = SegmentInfos.GetLastCommitGeneration(dir);
            AssertTrue("segment generation should be > 0 but got " + gen,
                gen > 0);
            string[] files = dir.ListAll();
            bool corrupted = false;
            for (int i_1 = 0; i_1 < files.Length; i_1++)
            {
                if (files[i_1].EndsWith(".cfs"))
                {
                    dir.DeleteFile(files[i_1]);
                    corrupted = true;
                    break;
                }
            }
            AssertTrue("failed to find cfs file to remove", corrupted);
            IndexReader reader = null;
            try
            {
                reader = DirectoryReader.Open(dir);
                Fail("reader did not hit IOException on opening a corrupt index"
                    );
            }
            catch (Exception)
            {
            }
            if (reader != null)
            {
                reader.Dispose();
            }
            dir.Dispose();
        }

        // Simulate a writer that crashed while writing segments
        // file: make sure we can still open the index (ie,
        // gracefully fallback to the previous segments file),
        // and that we can add to the index:
        [Test]
        public virtual void TestSimulatedCrashedWriter()
        {
            Directory dir = NewDirectory();
            if (dir is MockDirectoryWrapper)
            {
                ((MockDirectoryWrapper)dir).SetPreventDoubleWrite(false);
            }
            IndexWriter writer = null;
            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
                (Random())));
            // add 100 documents
            for (int i = 0; i < 100; i++)
            {
                AddDoc(writer);
            }
            // close
            writer.Dispose();
            long gen = SegmentInfos.GetLastCommitGeneration(dir);
            AssertTrue("segment generation should be > 0 but got " + gen,
                gen > 0);
            // Make the next segments file, with last byte
            // missing, to simulate a writer that crashed while
            // writing segments file:
            string fileNameIn = SegmentInfos.GetLastCommitSegmentsFileName(dir);
            string fileNameOut = IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS
                , string.Empty, 1 + gen);
            IndexInput @in = dir.OpenInput(fileNameIn, NewIOContext(Random()));
            IndexOutput @out = dir.CreateOutput(fileNameOut, NewIOContext(Random()));
            long length = @in.Length;
            for (int i_1 = 0; i_1 < length - 1; i_1++)
            {
                @out.WriteByte(@in.ReadByte());
            }
            @in.Dispose();
            @out.Dispose();
            IndexReader reader = null;
            try
            {
                reader = DirectoryReader.Open(dir);
            }
            catch (Exception)
            {
                Fail("reader failed to open on a crashed index");
            }
            reader.Dispose();
            try
            {
                writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
                    (Random())).SetOpenMode(IndexWriterConfig.OpenMode.CREATE));
            }
            catch (Exception e)
            {
                e.printStackTrace();
                Fail("writer failed to open on a crashed index");
            }
            // add 100 documents
            for (int i_2 = 0; i_2 < 100; i_2++)
            {
                AddDoc(writer);
            }
            // close
            writer.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestTermVectorExceptions()
        {
            var failures = new[] { new FailOnTermVectors(FailOnTermVectors.AFTER_INIT_STAGE), 
                                   new FailOnTermVectors(FailOnTermVectors.INIT_STAGE) };
            int num = AtLeast(1);
            for (int j = 0; j < num; j++)
            {
                foreach (FailOnTermVectors failure in failures)
                {
                    MockDirectoryWrapper dir = NewMockDirectory();
                    IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new
                        MockAnalyzer(Random())));
                    dir.FailOn(failure);
                    int numDocs = 10 + Random().Next(30);
                    for (int i = 0; i < numDocs; i++)
                    {
                        Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                            ();
                        Field field = NewTextField(Random(), "field", "a field", Field.Store.YES);
                        doc.Add(field);
                        // random TV
                        try
                        {
                            w.AddDocument(doc);
                            IsFalse(field.FieldTypeValue.StoreTermVectors);
                        }
                        catch (SystemException e)
                        {
                            AssertTrue(e.Message.StartsWith(TestIndexWriterExceptions.FailOnTermVectors
                                .EXC_MSG));
                        }
                        if (Random().Next(20) == 0)
                        {
                            w.Commit();
                            TestUtil.CheckIndex(dir);
                        }
                    }
                    Lucene.Net.Documents.Document document = new Lucene.Net.Documents.Document
                    {
                        new TextField("field", "a field", Field.Store.YES)
                    };
                    w.AddDocument(document);
                    for (int i_1 = 0; i_1 < numDocs; i_1++)
                    {
                        Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                            ();
                        Field field = NewTextField(Random(), "field", "a field", Field.Store.YES);
                        doc.Add(field);
                        // random TV
                        try
                        {
                            w.AddDocument(doc);
                            IsFalse(field.FieldTypeValue.StoreTermVectors);
                        }
                        catch (SystemException e)
                        {
                            AssertTrue(e.Message.StartsWith(TestIndexWriterExceptions.FailOnTermVectors
                                .EXC_MSG));
                        }
                        if (Random().Next(20) == 0)
                        {
                            w.Commit();
                            TestUtil.CheckIndex(dir);
                        }
                    }
                    document = new Lucene.Net.Documents.Document();
                    document.Add(new TextField("field", "a field", Field.Store.YES));
                    w.AddDocument(document);
                    w.Dispose();
                    IndexReader reader = DirectoryReader.Open(dir);
                    AssertTrue(reader.NumDocs > 0);
                    SegmentInfos sis = new SegmentInfos();
                    sis.Read(dir);
                    foreach (AtomicReaderContext context in reader.Leaves)
                    {
                        IsFalse(((AtomicReader)context.Reader).FieldInfos.HasVectors);
                    }
                    reader.Dispose();
                    dir.Dispose();
                }
            }
        }

        private class FailOnTermVectors : MockDirectoryWrapper.Failure
        {
            internal static readonly string INIT_STAGE = "initTermVectorsWriter";

            internal static readonly string AFTER_INIT_STAGE = "finishDocument";

            internal static readonly string EXC_MSG = "FOTV";

            private readonly string stage;

            public FailOnTermVectors(string stage)
            {
                this.stage = stage;
            }

            /// <exception cref="System.IO.IOException"></exception>
            public override void Eval(MockDirectoryWrapper dir)
            {
                var trace = new StackTrace(new Exception()).GetFrames();
                bool fail = false;
                for (int i = 0; i < trace.Length; i++)
                {
                    if (typeof(TermVectorsConsumer).FullName.Equals(trace[i].GetMethod().DeclaringType.FullName) && stage
                        .Equals(trace[i].GetMethod().Name))
                    {
                        fail = true;
                        break;
                    }
                }
                if (fail)
                {
                    throw new SystemException(EXC_MSG);
                }
            }
        }

        [Test]
        public virtual void TestAddDocsNonAbortingException()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random(), dir);
            int numDocs1 = Random().Next(25);
            for (int docCount = 0; docCount < numDocs1; docCount++)
            {
                Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                    ();
                doc.Add(NewTextField("content", "good content", Field.Store.NO));
                w.AddDocument(doc);
            }
            IList<Lucene.Net.Documents.Document> docs = new List<Lucene.Net.Documents.Document
                >();
            for (int docCount_1 = 0; docCount_1 < 7; docCount_1++)
            {
                Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                    ();
                docs.Add(doc);
                doc.Add(NewStringField("id", docCount_1 + string.Empty, Field.Store.NO));
                doc.Add(NewTextField("content", "silly content " + docCount_1, Field.Store.NO));
                if (docCount_1 == 4)
                {
                    Field f = NewTextField("crash", string.Empty, Field.Store.NO);
                    doc.Add(f);
                    MockTokenizer tokenizer = new MockTokenizer(new StringReader("crash me on the 4th token"
                        ), MockTokenizer.WHITESPACE, false);
                    tokenizer.setEnableChecks(false);
                    // disable workflow checking as we forcefully close() in exceptional cases.
                    f.SetTokenStream(new TestIndexWriterExceptions.CrashingFilter(this, "crash", tokenizer
                        ));
                }
            }
            try
            {
                w.AddDocuments(docs.AsEnumerable());
                // BUG: CrashingFilter didn't
                Fail("did not hit expected exception");
            }
            catch (IOException ioe)
            {
                // expected
                AssertEquals(CRASH_FAIL_MESSAGE, ioe.Message);
            }
            int numDocs2 = Random().Next(25);
            for (int docCount_2 = 0; docCount_2 < numDocs2; docCount_2++)
            {
                Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                {
                    NewTextField("content", "good content", Field.Store.NO)
                };
                w.AddDocument(doc);
            }
            IndexReader r = w.GetReader();
            w.Close();
            IndexSearcher s = NewSearcher(r);
            PhraseQuery pq = new PhraseQuery();
            pq.Add(new Term("content", "silly"));
            pq.Add(new Term("content", "content"));
            AssertEquals(0, s.Search(pq, 1).TotalHits);
            pq = new PhraseQuery();
            pq.Add(new Term("content", "good"));
            pq.Add(new Term("content", "content"));
            AssertEquals(numDocs1 + numDocs2, s.Search(pq, 1).TotalHits);
            r.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestUpdateDocsNonAbortingException()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random(), dir);
            int numDocs1 = Random().Next(25);
            for (int docCount = 0; docCount < numDocs1; docCount++)
            {
                Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                    ();
                doc.Add(NewTextField("content", "good content", Field.Store.NO));
                w.AddDocument(doc);
            }
            // Use addDocs (no exception) to get docs in the index:
            IList<Lucene.Net.Documents.Document> docs = new List<Lucene.Net.Documents.Document
                >();
            int numDocs2 = Random().Next(25);
            for (int docCount_1 = 0; docCount_1 < numDocs2; docCount_1++)
            {
                Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                    ();
                docs.Add(doc);
                doc.Add(NewStringField("subid", "subs", Field.Store.NO));
                doc.Add(NewStringField("id", docCount_1 + string.Empty, Field.Store.NO));
                doc.Add(NewTextField("content", "silly content " + docCount_1, Field.Store.NO));
            }
            w.AddDocuments(docs.AsEnumerable());
            int numDocs3 = Random().Next(25);
            for (int docCount_2 = 0; docCount_2 < numDocs3; docCount_2++)
            {
                Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                {
                    NewTextField("content", "good content", Field.Store.NO)
                };
                w.AddDocument(doc);
            }
            docs.Clear();
            int limit = TestUtil.NextInt(Random(), 2, 25);
            int crashAt = Random().Next(limit);
            for (int docCount_3 = 0; docCount_3 < limit; docCount_3++)
            {
                Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                    ();
                docs.Add(doc);
                doc.Add(NewStringField("id", docCount_3 + string.Empty, Field.Store.NO));
                doc.Add(NewTextField("content", "silly content " + docCount_3, Field.Store.NO));
                if (docCount_3 == crashAt)
                {
                    Field f = NewTextField("crash", string.Empty, Field.Store.NO);
                    doc.Add(f);
                    MockTokenizer tokenizer = new MockTokenizer(new StringReader("crash me on the 4th token"
                        ), MockTokenizer.WHITESPACE, false);
                    tokenizer.setEnableChecks(false);
                    // disable workflow checking as we forcefully close() in exceptional cases.
                    f.SetTokenStream(new TestIndexWriterExceptions.CrashingFilter(this, "crash", tokenizer
                        ));
                }
            }
            try
            {
                w.UpdateDocuments(new Term("subid", "subs"), docs.AsEnumerable());
                // BUG: CrashingFilter didn't
                Fail("did not hit expected exception");
            }
            catch (IOException ioe)
            {
                // expected
                AssertEquals(CRASH_FAIL_MESSAGE, ioe.Message);
            }
            int numDocs4 = Random().Next(25);
            for (int docCount_4 = 0; docCount_4 < numDocs4; docCount_4++)
            {
                Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                    ();
                doc.Add(NewTextField("content", "good content", Field.Store.NO));
                w.AddDocument(doc);
            }
            IndexReader r = w.GetReader();
            w.Close();
            IndexSearcher s = NewSearcher(r);
            PhraseQuery pq = new PhraseQuery();
            pq.Add(new Term("content", "silly"));
            pq.Add(new Term("content", "content"));
            AssertEquals(numDocs2, s.Search(pq, 1).TotalHits);
            pq = new PhraseQuery();
            pq.Add(new Term("content", "good"));
            pq.Add(new Term("content", "content"));
            AssertEquals(numDocs1 + numDocs3 + numDocs4, s.Search(pq, 1).TotalHits
                );
            r.Dispose();
            dir.Dispose();
        }

        internal class UOEDirectory : RAMDirectory
        {
            internal bool doFail = false;

            /// <exception cref="System.IO.IOException"></exception>
            public override IndexInput OpenInput(string name, IOContext context)
            {
                if (doFail && name.StartsWith("segments_"))
                {
                    var trace = new StackTrace(new Exception()).GetFrames();
                    for (int i = 0; i < trace.Length; i++)
                    {
                        if ("read".Equals(trace[i].GetMethod().Name))
                        {
                            throw new NotSupportedException("expected UOE");
                        }
                    }
                }
                return base.OpenInput(name, context);
            }
        }

        [Test]
        public virtual void TestExceptionOnCtor()
        {
            var uoe = new UOEDirectory();
            Directory d = new MockDirectoryWrapper(Random(), uoe);
            IndexWriter iw = new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT, null
                ));
            iw.AddDocument(new Lucene.Net.Documents.Document());
            iw.Dispose();
            uoe.doFail = true;
            try
            {
                new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT, null));
                Fail("should have gotten a UOE");
            }
            catch (NotSupportedException)
            {
            }
            uoe.doFail = false;
            d.Dispose();
        }

        [Test]
        public virtual void TestIllegalPositions()
        {
            Directory dir = NewDirectory();
            IndexWriter iw = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT,
                null));
            Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                ();
            Token t1 = new Token("foo", 0, 3);
            t1.PositionIncrement = (int.MaxValue);
            Token t2 = new Token("bar", 4, 7);
            t2.PositionIncrement = (200);
            TokenStream overflowingTokenStream = new CannedTokenStream(new Token[] { t1, t2 }
                );
            Field field = new TextField("foo", overflowingTokenStream);
            doc.Add(field);
            try
            {
                iw.AddDocument(doc);
                Fail();
            }
            catch (ArgumentException)
            {
            }
            // expected exception
            iw.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestLegalbutVeryLargePositions()
        {
            Directory dir = NewDirectory();
            IndexWriter iw = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT,
                null));
            Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                ();
            Token t1 = new Token("foo", 0, 3);
            t1.PositionIncrement = (int.MaxValue - 500);
            if (Random().NextBoolean())
            {
                t1.Payload = (new BytesRef(new sbyte[] { (0x1) }));
            }
            TokenStream overflowingTokenStream = new CannedTokenStream(new Token[] { t1 });
            Field field = new TextField("foo", overflowingTokenStream);
            doc.Add(field);
            iw.AddDocument(doc);
            iw.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestBoostOmitNorms()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
                (Random()));
            iwc.SetMergePolicy(NewLogMergePolicy());
            IndexWriter iw = new IndexWriter(dir, iwc);
            Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
            {
                new StringField("field1", "sometext", Field.Store.YES),
                new TextField("field2", "sometext", Field.Store.NO),
                new StringField("foo", "bar", Field.Store.NO)
            };
            iw.AddDocument(doc);
            // add an 'ok' document
            try
            {
                doc = new Lucene.Net.Documents.Document();
                // try to boost with norms omitted
                IList<IIndexableField> list = new List<IIndexableField>();
                list.Add(new AnonymousIndexableField());
                iw.AddDocument(list.AsEnumerable());
                Fail("didn't get any exception, boost silently discarded");
            }
            catch (NotSupportedException)
            {
            }
            // expected
            DirectoryReader ir = DirectoryReader.Open(iw, false);
            AssertEquals(1, ir.NumDocs);
            AssertEquals("sometext", ir.Document(0).Get("field1"));
            ir.Dispose();
            iw.Dispose();
            dir.Dispose();
        }

        private sealed class AnonymousIndexableField : IIndexableField
        {
            public string Name
            {
                get { return "foo"; }
            }

            public IIndexableFieldType FieldTypeValue
            {
                get { return StringField.TYPE_NOT_STORED; }
            }

            public float Boost
            {
                get { return 5f; }
            }

            public BytesRef BinaryValue
            {
                get { return null; }
            }

            public string StringValue
            {
                get { return "baz"; }
            }

            public TextReader ReaderValue
            {
                get { return null; }
            }

            public object NumericValue
            {
                get { return null; }
            }

            /// <exception cref="System.IO.IOException"></exception>
            public TokenStream TokenStream(Analyzer analyzer)
            {
                return null;
            }
        }

        // See LUCENE-4870 TooManyOpenFiles errors are thrown as
        // FNFExceptions which can trigger data loss.
        [Test]
        public virtual void TestTooManyFileException()
        {
            // Create failure that throws Too many open files exception randomly
            MockDirectoryWrapper.Failure failure = new AnonymousFailure();
            MockDirectoryWrapper dir = NewMockDirectory();
            // The exception is only thrown on open input
            dir.SetFailOnOpenInput(true);
            dir.FailOn(failure);
            // Create an index with one document
            IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
                (Random()));
            IndexWriter iw = new IndexWriter(dir, iwc);
            Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                ();
            doc.Add(new StringField("foo", "bar", Field.Store.NO));
            iw.AddDocument(doc);
            // add a document
            iw.Commit();
            DirectoryReader ir = DirectoryReader.Open(dir);
            AssertEquals(1, ir.NumDocs);
            ir.Dispose();
            iw.Dispose();
            // Open and close the index a few times
            for (int i = 0; i < 10; i++)
            {
                failure.SetDoFail();
                iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
                try
                {
                    iw = new IndexWriter(dir, iwc);
                }
                catch (CorruptIndexException)
                {
                    // Exceptions are fine - we are running out of file handlers here
                    continue;
                }
                catch (IOException)
                {
                    continue;
                }
                failure.ClearDoFail();
                iw.Dispose();
                ir = DirectoryReader.Open(dir);
                AssertEquals("lost document after iteration: " + i, 1, ir.NumDocs);
                ir.Dispose();
            }
            // Check if document is still there
            failure.ClearDoFail();
            ir = DirectoryReader.Open(dir);
            AssertEquals(1, ir.NumDocs);
            ir.Dispose();
            dir.Dispose();
        }

        private sealed class AnonymousFailure : MockDirectoryWrapper.Failure
        {
            public override MockDirectoryWrapper.Failure Reset()
            {
                this.doFail = false;
                return this;
            }

            /// <exception cref="System.IO.IOException"></exception>
            public override void Eval(MockDirectoryWrapper dir)
            {
                if (this.doFail)
                {
                    if (LuceneTestCase.Random().NextBoolean())
                    {
                        throw new FileNotFoundException("some/file/name.ext (Too many open files)");
                    }
                }
            }
        }

        // Make sure if we hit a transient IOException (e.g., disk
        // full), and then the exception stops (e.g., disk frees
        // up), so we successfully close IW or open an NRT
        // reader, we don't lose any deletes or updates:
        [Test]
        public virtual void TestNoLostDeletesOrUpdates()
        {
            int deleteCount = 0;
            int docBase = 0;
            int docCount = 0;
            MockDirectoryWrapper dir = NewMockDirectory();
            AtomicBoolean shouldFail = new AtomicBoolean();
            dir.FailOn(new AnonymousFailure2(shouldFail));
            // Don't throw exc if we are "flushing", else
            // the segment is aborted and docs are lost:
            // Only sometimes throw the exc, so we get
            // it sometimes on creating the file, on
            // flushing buffer, on closing the file:
            RandomIndexWriter w = null;
            for (int iter = 0; iter < 10 * RANDOM_MULTIPLIER; iter++)
            {
                int numDocs = AtLeast(100);
                if (VERBOSE)
                {
                    System.Console.Out.WriteLine("\nTEST: iter=" + iter + " numDocs=" + numDocs + " docBase="
                         + docBase + " delCount=" + deleteCount);
                }
                if (w == null)
                {
                    IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
                        (Random()));
                    MergeScheduler ms = iwc.MergeScheduler;
                    if (ms is ConcurrentMergeScheduler)
                    {
                        ConcurrentMergeScheduler suppressFakeIOE = new AnonConcurrentMergeScheduler();
                        // suppress only FakeIOException:
                        ConcurrentMergeScheduler cms = (ConcurrentMergeScheduler)ms;
                        suppressFakeIOE.SetMaxMergesAndThreads(cms.MaxMergeCount, cms.MaxThreadCount);
                        suppressFakeIOE.MergeThreadPriority = (cms.MergeThreadPriority);
                        iwc.SetMergeScheduler(suppressFakeIOE);
                    }
                    w = new RandomIndexWriter(Random(), dir, iwc);
                    // Since we hit exc during merging, a partial
                    // forceMerge can easily return when there are still
                    // too many segments in the index:
                    w.SetDoRandomForceMergeAssert(false);
                }
                for (int i = 0; i < numDocs; i++)
                {
                    Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                        ();
                    doc.Add(new StringField("id", string.Empty + (docBase + i), Field.Store.NO));
                    if (DefaultCodecSupportsDocValues())
                    {
                        doc.Add(new NumericDocValuesField("f", 1L));
                        doc.Add(new NumericDocValuesField("cf", 2L));
                        doc.Add(new BinaryDocValuesField("bf", TestBinaryDocValuesUpdates.ToBytes(1L)));
                        doc.Add(new BinaryDocValuesField("bcf", TestBinaryDocValuesUpdates.ToBytes(2L)));
                    }
                    w.AddDocument(doc);
                }
                docCount += numDocs;
                // TODO: we could make the test more evil, by letting
                // it throw more than one exc, randomly, before "recovering"
                // TODO: we could also install an infoStream and try
                // to fail in "more evil" places inside BDS
                shouldFail.Set(true);
                bool doClose = false;
                int updatingDocID = -1;
                long updatingValue = -1;
                try
                {
                    bool defaultCodecSupportsFieldUpdates = DefaultCodecSupportsFieldUpdates();
                    for (int i_1 = 0; i_1 < numDocs; i_1++)
                    {
                        if (Random().Next(10) == 7)
                        {
                            bool fieldUpdate = defaultCodecSupportsFieldUpdates && Random().NextBoolean();
                            int docid = docBase + i_1;
                            if (fieldUpdate)
                            {
                                long value = iter;
                                if (VERBOSE)
                                {
                                    System.Console.Out.WriteLine("  update id=" + docid + " to value " + value);
                                }
                                Term idTerm = new Term("id", docid.ToString());
                                updatingDocID = docid;
                                // record that we're updating that document
                                updatingValue = value;
                                // and its updating value
                                if (Random().NextBoolean())
                                {
                                    // update only numeric field
                                    w.UpdateNumericDocValue(idTerm, "f", value);
                                    w.UpdateNumericDocValue(idTerm, "cf", value * 2);
                                }
                                else
                                {
                                    if (Random().NextBoolean())
                                    {
                                        w.UpdateBinaryDocValue(idTerm, "bf", TestBinaryDocValuesUpdates.ToBytes(value));
                                        w.UpdateBinaryDocValue(idTerm, "bcf", TestBinaryDocValuesUpdates.ToBytes(value *
                                            2));
                                    }
                                    else
                                    {
                                        w.UpdateNumericDocValue(idTerm, "f", value);
                                        w.UpdateNumericDocValue(idTerm, "cf", value * 2);
                                        w.UpdateBinaryDocValue(idTerm, "bf", TestBinaryDocValuesUpdates.ToBytes(value));
                                        w.UpdateBinaryDocValue(idTerm, "bcf", TestBinaryDocValuesUpdates.ToBytes(value *
                                            2));
                                    }
                                }
                                // record that we successfully updated the document. this is
                                // important when we later 
                                //HM:revisit 
                                //assert the value of the DV fields of
                                // that document - since we update two fields that depend on each
                                // other, could be that one of the fields successfully updates,
                                // while the other fails (since we turn on random exceptions).
                                // while this is supported, it makes the test raise false alarms.
                                updatingDocID = -1;
                                updatingValue = -1;
                            }
                            // sometimes do both deletes and updates
                            if (!fieldUpdate || Random().NextBoolean())
                            {
                                if (VERBOSE)
                                {
                                    System.Console.Out.WriteLine("  delete id=" + docid);
                                }
                                deleteCount++;
                                w.DeleteDocuments(new Term("id", string.Empty + docid));
                            }
                        }
                    }
                    // Trigger writeLiveDocs + writeFieldUpdates so we hit fake exc:
                    IndexReader r = w.GetReader(true);
                    // Sometimes we will make it here (we only randomly
                    // throw the exc):
                    AssertEquals(docCount - deleteCount, r.NumDocs);
                    r.Dispose();
                    // Sometimes close, so the disk full happens on close:
                    if (Random().NextBoolean())
                    {
                        if (VERBOSE)
                        {
                            System.Console.Out.WriteLine("  now close writer");
                        }
                        doClose = true;
                        w.Close();
                        w = null;
                    }
                }
                catch (IOException ioe)
                {
                    // FakeIOException can be thrown from mergeMiddle, in which case IW
                    // registers it before our CMS gets to suppress it. IW.forceMerge later
                    // throws it as a wrapped IOE, so don't fail in this case.
                    if (ioe is MockDirectoryWrapper.FakeIOException || (ioe.InnerException != null &&
                         ioe.InnerException is MockDirectoryWrapper.FakeIOException))
                    {
                        // expected
                        if (VERBOSE)
                        {
                            System.Console.Out.WriteLine("TEST: w.close() hit expected IOE");
                        }
                    }
                    else
                    {
                        throw;
                    }
                }
                shouldFail.Set(false);
                if (updatingDocID != -1)
                {
                    // Updating this document did not succeed. Since the fields we 
                    //HM:revisit 
                    //assert on
                    // depend on each other, and the update may have gone through halfway,
                    // replay the update on both numeric and binary DV fields, so later
                    // asserts succeed.
                    Term idTerm = new Term("id", string.Empty + updatingDocID);
                    w.UpdateNumericDocValue(idTerm, "f", updatingValue);
                    w.UpdateNumericDocValue(idTerm, "cf", updatingValue * 2);
                    w.UpdateBinaryDocValue(idTerm, "bf", TestBinaryDocValuesUpdates.ToBytes(updatingValue
                        ));
                    w.UpdateBinaryDocValue(idTerm, "bcf", TestBinaryDocValuesUpdates.ToBytes(updatingValue
                         * 2));
                }
                IndexReader r_1;
                if (doClose && w != null)
                {
                    if (VERBOSE)
                    {
                        System.Console.Out.WriteLine("  now 2nd close writer");
                    }
                    w.Close();
                    w = null;
                }
                if (w == null || Random().NextBoolean())
                {
                    // Open non-NRT reader, to make sure the "on
                    // disk" bits are good:
                    if (VERBOSE)
                    {
                        System.Console.Out.WriteLine("TEST: verify against non-NRT reader");
                    }
                    if (w != null)
                    {
                        w.Commit();
                    }
                    r_1 = DirectoryReader.Open(dir);
                }
                else
                {
                    if (VERBOSE)
                    {
                        System.Console.Out.WriteLine("TEST: verify against NRT reader");
                    }
                    r_1 = w.GetReader();
                }
                AssertEquals(docCount - deleteCount, r_1.NumDocs);
                if (DefaultCodecSupportsDocValues())
                {
                    BytesRef scratch = new BytesRef();
                    foreach (AtomicReaderContext context in r_1.Leaves)
                    {
                        AtomicReader reader = ((AtomicReader)context.Reader);
                        IBits liveDocs = reader.LiveDocs;
                        NumericDocValues f = reader.GetNumericDocValues("f");
                        NumericDocValues cf = reader.GetNumericDocValues("cf");
                        BinaryDocValues bf = reader.GetBinaryDocValues("bf");
                        BinaryDocValues bcf = reader.GetBinaryDocValues("bcf");
                        for (int i = 0; i < reader.MaxDoc; i++)
                        {
                            if (liveDocs == null || liveDocs[i])
                            {
                                AssertEquals("doc=" + (docBase + i), cf.Get(i), f.Get(i)
                                     * 2);
                                AssertEquals("doc=" + (docBase + i), TestBinaryDocValuesUpdates
                                    .GetValue(bcf, i, scratch), TestBinaryDocValuesUpdates.GetValue(bf, i, scratch
                                    ) * 2);
                            }
                        }
                    }
                }
                r_1.Dispose();
                // Sometimes re-use RIW, other times open new one:
                if (w != null && Random().NextBoolean())
                {
                    if (VERBOSE)
                    {
                        System.Console.Out.WriteLine("TEST: close writer");
                    }
                    w.Close();
                    w = null;
                }
                docBase += numDocs;
            }
            if (w != null)
            {
                w.Close();
            }
            // Final verify:
            IndexReader r_2 = DirectoryReader.Open(dir);
            AssertEquals(docCount - deleteCount, r_2.NumDocs);
            r_2.Dispose();
            dir.Dispose();
        }

        private sealed class AnonymousFailure2 : MockDirectoryWrapper.Failure
        {
            public AnonymousFailure2(AtomicBoolean shouldFail)
            {
                this.shouldFail = shouldFail;
            }

            /// <exception cref="System.IO.IOException"></exception>
            public override void Eval(MockDirectoryWrapper dir)
            {
                var trace = new StackTrace(new Exception()).GetFrames();
                if (shouldFail.Get() == false)
                {
                    return;
                }
                bool sawSeal = false;
                bool sawWrite = false;
                for (int i = 0; i < trace.Length; i++)
                {
                    if ("sealFlushedSegment".Equals(trace[i].GetMethod().Name))
                    {
                        sawSeal = true;
                        break;
                    }
                    if ("writeLiveDocs".Equals(trace[i].GetMethod().Name) || "writeFieldUpdates".Equals
                        (trace[i].GetMethod().Name))
                    {
                        sawWrite = true;
                    }
                }
                if (sawWrite && sawSeal == false && LuceneTestCase.Random().Next(3) == 2)
                {
                    if (LuceneTestCase.VERBOSE)
                    {
                        System.Console.Out.WriteLine("TEST: now fail; thread=" + Thread.CurrentThread.Name + " exc:");
                        new Exception().printStackTrace();
                    }
                    shouldFail.Set(false);
                    throw new MockDirectoryWrapper.FakeIOException();
                }
            }

            private readonly AtomicBoolean shouldFail;
        }

        private sealed class AnonConcurrentMergeScheduler : ConcurrentMergeScheduler
        {
            protected internal override void HandleMergeException(Exception exc)
            {
                if (!(exc is MockDirectoryWrapper.FakeIOException))
                {
                    base.HandleMergeException(exc);
                }
            }
        }

        [Test]
        public virtual void TestExceptionDuringRollback()
        {
            // currently: fail in two different places
            string messageToFailOn = Random().NextBoolean() ? "rollback: done finish merges" :
                "rollback before checkpoint";
            // infostream that throws exception during rollback
            InfoStream evilInfoStream = new AnonymousInfoStream2(messageToFailOn);
            Directory dir = NewMockDirectory();
            // we want to ensure we don't leak any locks or file handles
            IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, null);
            iwc.SetInfoStream(evilInfoStream);
            IndexWriter iw = new IndexWriter(dir, iwc);
            var doc = new Lucene.Net.Documents.Document();
            for (int i = 0; i < 10; i++)
            {
                iw.AddDocument(doc);
            }
            iw.Commit();
            iw.AddDocument(doc);
            // pool readers
            DirectoryReader r = DirectoryReader.Open(iw, false);
            // sometimes sneak in a pending commit: we don't want to leak a file handle to that segments_N
            if (Random().NextBoolean())
            {
                iw.PrepareCommit();
            }
            try
            {
                iw.Rollback();
                Fail();
            }
            catch (SystemException expected)
            {
                AssertEquals("BOOM!", expected.Message);
            }
            r.Dispose();
            // even though we hit exception: we are closed, no locks or files held, index in good state
            AssertTrue(iw.IsClosed);
            IsFalse(IndexWriter.IsLocked(dir));
            r = DirectoryReader.Open(dir);
            AssertEquals(10, r.MaxDoc);
            r.Dispose();
            // no leaks
            dir.Dispose();
        }

        private sealed class AnonymousInfoStream2 : InfoStream
        {
            public AnonymousInfoStream2(string messageToFailOn)
            {
                this.messageToFailOn = messageToFailOn;
            }

            public override void Message(string component, string message)
            {
                if (messageToFailOn.Equals(message))
                {
                    throw new SystemException("BOOM!");
                }
            }

            public override bool IsEnabled(string component)
            {
                return true;
            }

            

            private readonly string messageToFailOn;
        }

        [Test]
        public virtual void TestRandomExceptionDuringRollback()
        {
            // fail in random places on i/o
            int numIters = RANDOM_MULTIPLIER * 75;
            for (int iter = 0; iter < numIters; iter++)
            {
                MockDirectoryWrapper dir = NewMockDirectory();
                dir.FailOn(new AnonymousFailure3());
                IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, null);
                IndexWriter iw = new IndexWriter(dir, iwc);
                Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
                    ();
                for (int i = 0; i < 10; i++)
                {
                    iw.AddDocument(doc);
                }
                iw.Commit();
                iw.AddDocument(doc);
                // pool readers
                DirectoryReader r = DirectoryReader.Open(iw, false);
                // sometimes sneak in a pending commit: we don't want to leak a file handle to that segments_N
                if (Random().NextBoolean())
                {
                    iw.PrepareCommit();
                }
                try
                {
                    iw.Rollback();
                }
                catch (MockDirectoryWrapper.FakeIOException)
                {
                }
                r.Dispose();
                // even though we hit exception: we are closed, no locks or files held, index in good state
                AssertTrue(iw.IsClosed);
                IsFalse(IndexWriter.IsLocked(dir));
                r = DirectoryReader.Open(dir);
                AssertEquals(10, r.MaxDoc);
                r.Dispose();
                // no leaks
                dir.Dispose();
            }
        }

        private sealed class AnonymousFailure3 : MockDirectoryWrapper.Failure
        {
            /// <exception cref="System.IO.IOException"></exception>
            public override void Eval(MockDirectoryWrapper dir)
            {
                bool maybeFail = false;
                var trace = new StackTrace(new Exception()).GetFrames();
                for (int i = 0; i < trace.Length; i++)
                {
                    if ("rollbackInternal".Equals(trace[i].GetMethod().Name))
                    {
                        maybeFail = true;
                        break;
                    }
                }
                if (maybeFail && LuceneTestCase.Random().Next(10) == 0)
                {
                    if (LuceneTestCase.VERBOSE)
                    {
                        System.Console.Out.WriteLine("TEST: now fail; thread=" + Thread.CurrentThread.Name + " exc:");
                        new Exception().printStackTrace();
                    }
                    throw new MockDirectoryWrapper.FakeIOException();
                }
            }
        }
    }
}
