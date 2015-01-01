using System;
using System.Collections.Generic;
using System.Threading;
using Lucene.Net.Analysis;
using Lucene.Net.Codecs;
using Lucene.Net.Index;
using Lucene.Net.Randomized;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.TestFramework;
using Lucene.Net.TestFramework.Util;
using Lucene.Net.Util;
using Version = System.Version;

namespace Lucene.Net.TestFramework.Index
{
    public class RandomIndexWriter : IDisposable
    {

        public IndexWriter w;
        private Random r;
        int docCount;
        int flushAt;
        private double flushAtFactor = 1.0;
        private bool getReaderCalled;
        private Codec codec; // sugar

        // Randomly calls Thread.yield so we mixup thread scheduling
        public static IndexWriter MockIndexWriter(Directory dir, IndexWriterConfig conf,
            Random r)
        {
            // Randomly calls Thread.yield so we mixup thread scheduling
            Random random = new Random(r.NextInt(0, int.MaxValue));
            return MockIndexWriter(dir, conf, new _TestPoint_57(random));
        }

        private sealed class _TestPoint_57 : RandomIndexWriter.TestPoint
        {
            public _TestPoint_57(Random random)
            {
                this.random = random;
            }

            public void Apply(string message)
            {
                if (random.Next(4) == 2)
                {
                    Thread.Yield();
                }
            }

            private readonly Random random;
        }

        public static IndexWriter MockIndexWriter(Directory dir, IndexWriterConfig conf,
            RandomIndexWriter.TestPoint testPoint)
        {
            conf.SetInfoStream(new RandomIndexWriter.TestPointInfoStream(conf.InfoStream
                , testPoint));
            return new IndexWriter(dir, conf);
        }
        /** create a RandomIndexWriter with a random config: Uses TEST_VERSION_CURRENT and MockAnalyzer */
        public RandomIndexWriter(Random r, Directory dir) :
            this(r, dir, LuceneTestCase.NewIndexWriterConfig(r, LuceneTestCase.TEST_VERSION_CURRENT, new MockAnalyzer(r)))
        {
        }

        /** create a RandomIndexWriter with a random config: Uses TEST_VERSION_CURRENT */
        public RandomIndexWriter(Random r, Directory dir, Analyzer a)
            : this(r, dir, LuceneTestCase.NewIndexWriterConfig(r, LuceneTestCase.TEST_VERSION_CURRENT, a))
        {

        }

        /** create a RandomIndexWriter with a random config */
        public RandomIndexWriter(Random r, Directory dir, Version v, Analyzer a)
            : this(r, dir, LuceneTestCase.NewIndexWriterConfig(r, v, a))
        {

        }

        /** create a RandomIndexWriter with the provided config */
        public RandomIndexWriter(Random r, Directory dir, IndexWriterConfig c)
        {
            // TODO: this should be solved in a different way; Random should not be shared (!).
            this.r = new Random(r.Next());
            w = MockIndexWriter(dir, c, r);
            flushAt = r.NextInt(10, 1000);
            codec = w.Config.Codec;
            if (LuceneTestCase.VERBOSE)
            {
                Console.WriteLine("RIW dir=" + dir + " config=" + w.Config);
                Console.WriteLine("codec default=" + codec.Name);
            }

            // Make sure we sometimes test indices that don't get
            // any forced merges:
            doRandomForceMerge = !(c.MergePolicy is NoMergePolicy) && r.NextBoolean();
        }

        /**
         * Adds a Document.
         * @see IndexWriter#addDocument(IEnumerable)
         */
        public virtual void AddDocument<T>(IEnumerable<T> doc) where T : IIndexableField
        {
            LuceneTestCase.MaybeChangeLiveIndexWriterConfig(r, w.Config);
            AddDocument(doc, w.Analyzer);
        }

        public virtual void AddDocument<T>(IEnumerable<T> doc, Analyzer a) where T : IIndexableField
        {
            LuceneTestCase.MaybeChangeLiveIndexWriterConfig(r, w.Config);
            if (r.Next(5) == 3)
            {
                // TODO: maybe, we should simply buffer up added docs
                // (but we need to clone them), and only when
                // getReader, commit, etc. are called, we do an
                // addDocuments?  Would be better testing.
                w.AddDocuments(new _Iterable_119(doc), a);
            }
            else
            {
                w.AddDocument(doc, a);
            }
            MaybeCommit();
        }

        private sealed class _Iterable_119 : IEnumerable<IEnumerable<T>>
        {
            public _Iterable_119(IEnumerable<T> doc)
            {
                this.doc = doc;
            }

            public override Iterator<IEnumerable<T>> Iterator()
            {
                return new _Iterator_123(doc);
            }

            private sealed class _Iterator_123 : Iterator<IEnumerable<T>>
            {
                public _Iterator_123(IEnumerable<T> doc)
                {
                    this.doc = doc;
                }

                internal bool done;

                public override bool HasNext()
                {
                    return !this.done;
                }

                public override void Remove()
                {
                    throw new NotSupportedException();
                }

                public override IEnumerable<T> Next()
                {
                    if (this.done)
                    {
                        throw new InvalidOperationException();
                    }
                    this.done = true;
                    return doc;
                }

                private readonly IEnumerable<T> doc;
            }

            private readonly IEnumerable<T> doc;
        }

        private void MaybeCommit()
        {
            LuceneTestCase.MaybeChangeLiveIndexWriterConfig(r, w.GetConfig());
            if (docCount++ == flushAt)
            {
                if (LuceneTestCase.VERBOSE)
                {
                    Console.WriteLine("RIW.add/updateDocument: now doing a commit at docCount=" + docCount);
                }
                w.Commit();
                flushAt += TestUtil.NextInt(r, (int)(flushAtFactor * 10), (int)(flushAtFactor * 1000
                    ));
                if (flushAtFactor < 2e6)
                {
                    // gradually but exponentially increase time b/w flushes
                    flushAtFactor *= 1.05;
                }
            }
        }

        public virtual void AddDocuments<T>(IEnumerable<T> docs) where T : IEnumerable<IIndexableField>
        {
            LuceneTestCase.MaybeChangeLiveIndexWriterConfig(r, w.Config);
            w.AddDocuments(docs);
            MaybeCommit();
        }

        public virtual void UpdateDocuments<T>(Term delTerm, IEnumerable<T> docs) where
            T : IEnumerable<IIndexableField>
        {
            LuceneTestCase.MaybeChangeLiveIndexWriterConfig(r, w.Config);
            w.UpdateDocuments(delTerm, docs);
            MaybeCommit();
        }

        /**
         * Updates a document.
         * @see IndexWriter#updateDocument(Term, IEnumerable)
         */
        public virtual void UpdateDocument<T>(Term t, IEnumerable<T> doc) where T : IIndexableField
        {
            LuceneTestCase.MaybeChangeLiveIndexWriterConfig(r, w.Config);
            if (r.Next(5) == 3)
            {
                w.UpdateDocuments(t, new _Iterable_188(doc));
            }
            else
            {
                w.UpdateDocument(t, doc);
            }
            MaybeCommit();
        }

        private sealed class _Iterable_188 : IEnumerable<IEnumerable<T>>
        {
            public _Iterable_188(IEnumerable<T> doc)
            {
                this.doc = doc;
            }

            public override Iterator<IEnumerable<T>> Iterator()
            {
                return new _Iterator_192(doc);
            }

            private sealed class _Iterator_192 : Iterator<IEnumerable<T>>
            {
                public _Iterator_192(IEnumerable<T> doc)
                {
                    this.doc = doc;
                }

                internal bool done;

                public override bool HasNext()
                {
                    return !this.done;
                }

                public override void Remove()
                {
                    throw new NotSupportedException();
                }

                public override IEnumerable<T> Next()
                {
                    if (this.done)
                    {
                        throw new InvalidOperationException();
                    }
                    this.done = true;
                    return doc;
                }

                private readonly IEnumerable<T> doc;
            }

            private readonly IEnumerable<T> doc;
        }
        public virtual void AddIndexes(params Directory[] dirs)
        {
            LuceneTestCase.MaybeChangeLiveIndexWriterConfig(r, w.GetConfig());
            w.AddIndexes(dirs);
        }

        public virtual void AddIndexes(params IndexReader[] readers)
        {
            LuceneTestCase.MaybeChangeLiveIndexWriterConfig(r, w.GetConfig());
            w.AddIndexes(readers);
        }

        public virtual void UpdateNumericDocValue(Term term, string field, long value)
        {
            LuceneTestCase.MaybeChangeLiveIndexWriterConfig(r, w.GetConfig());
            w.UpdateNumericDocValue(term, field, value);
        }
        public virtual void UpdateBinaryDocValue(Term term, string field, BytesRef value)
        {
            LuceneTestCase.MaybeChangeLiveIndexWriterConfig(r, w.GetConfig());
            w.UpdateBinaryDocValue(term, field, value);
        }
        public virtual void DeleteDocuments(Term term)
        {
            LuceneTestCase.MaybeChangeLiveIndexWriterConfig(r, w.GetConfig());
            w.DeleteDocuments(term);
        }

        public virtual void DeleteDocuments(Query q)
        {
            LuceneTestCase.MaybeChangeLiveIndexWriterConfig(r, w.GetConfig());
            w.DeleteDocuments(q);
        }

        public virtual void Commit()
        {
            LuceneTestCase.MaybeChangeLiveIndexWriterConfig(r, w.GetConfig());
            w.Commit();
        }

        public virtual int NumDocs()
        {
            return w.NumDocs;
        }

        public virtual int MaxDoc()
        {
            return w.MaxDoc;
        }

        public virtual void DeleteAll()
        {
            w.DeleteAll();
        }

        public DirectoryReader Reader
        {
            get { return GetReader(); }
        }

        //TODO: remove all external calls and make this internal just like in IW
        public virtual DirectoryReader GetReader()
        {
            LuceneTestCase.MaybeChangeLiveIndexWriterConfig(r, w.Config);
            return GetReader(true);
        }

        private bool doRandomForceMerge = true;
        private bool doRandomForceMergeAssert = true;

        public virtual void ForceMergeDeletes(bool doWait)
        {
            LuceneTestCase.MaybeChangeLiveIndexWriterConfig(r, w.GetConfig());
            w.ForceMergeDeletes(doWait);
        }

        public virtual void ForceMergeDeletes()
        {
            LuceneTestCase.MaybeChangeLiveIndexWriterConfig(r, w.GetConfig());
            w.ForceMergeDeletes();
        }

        public virtual void SetDoRandomForceMerge(bool v)
        {
            doRandomForceMerge = v;
        }

        public virtual void SetDoRandomForceMergeAssert(bool v)
        {
            doRandomForceMergeAssert = v;
        }

        private void DoRandomForceMerge()
        {
            if (doRandomForceMerge)
            {
                int segCount = w.SegmentCount;
                if (r.nextBoolean() || segCount == 0)
                {
                    // full forceMerge
                    if (LuceneTestCase.VERBOSE)
                    {
                        Console.WriteLine("RIW: doRandomForceMerge(1)");
                    }
                    w.ForceMerge(1);
                }
                else
                {
                    // partial forceMerge
                    int limit = _TestUtil.nextInt(r, 1, segCount);
                    if (LuceneTestCase.VERBOSE)
                    {
                        Console.WriteLine("RIW: doRandomForceMerge(" + limit + ")");
                    }
                    w.ForceMerge(limit);
                    //assert !doRandomForceMergeAssert || w.getSegmentCount() <= limit: "limit=" + limit + " actual=" + w.getSegmentCount();
                }
            }
        }

        public virtual DirectoryReader GetReader(bool applyDeletions)
        {
			LuceneTestCase.MaybeChangeLiveIndexWriterConfig(r, w.GetConfig());
    getReaderCalled = true;
			if (r.Next(20) == 2)
			{
				DoRandomForceMerge();
    }
    // If we are writing with PreFlexRW, force a full
    // IndexReader.open so terms are sorted in codepoint
    // order during searching:
			if (!applyDeletions || !codec.GetName().Equals("Lucene3x") && r.NextBoolean())
			{
      if (LuceneTestCase.VERBOSE) 
      {
        System.Console.WriteLine("RIW.getReader: use NRT reader");
      }
      if (r.NextInt(5) == 1) 
      {
        w.Commit();
      }
      return w.GetReader(applyDeletions);
    }
            if (LuceneTestCase.VERBOSE) 
            {
                System.out.println("RIW.getReader: open new reader");
            }
            w.Commit();
            if (r.nextBoolean()) 
            {
                return DirectoryReader.Open(w.Directory, _TestUtil.nextInt(r, 1, 10));
            }
            return w.getReader(applyDeletions);
        }

        /**
         * Close this writer.
         * @see IndexWriter#close()
         */
        public virtual void Close()
        {
            if (!w.IsClosed)
            {
                LuceneTestCase.MaybeChangeLiveIndexWriterConfig(r, w.GetConfig());
            }
            // if someone isn't using getReader() API, we want to be sure to
            // forceMerge since presumably they might open a reader on the dir.
            if (getReaderCalled == false && r.Next(8) == 2)
            {
                DoRandomForceMerge();
            }
            w.Close();
        }

        /**
         * Forces a forceMerge.
         * <p>
         * NOTE: this should be avoided in tests unless absolutely necessary,
         * as it will result in less test coverage.
         * @see IndexWriter#forceMerge(int)
         */
        public virtual void ForceMerge(int maxSegmentCount)
        {
            LuceneTestCase.MaybeChangeLiveIndexWriterConfig(r, w.GetConfig());
            w.ForceMerge(maxSegmentCount);
        }

        public sealed class TestPointInfoStream : InfoStream
        {
            private readonly InfoStream delegate_;

            private readonly RandomIndexWriter.TestPoint testPoint;

            public TestPointInfoStream(InfoStream delegate_, RandomIndexWriter.TestPoint testPoint
                )
            {
                this.delegate_ = delegate_ == null ? new NullInfoStream() : delegate_;
                this.testPoint = testPoint;
            }

            /// <exception cref="System.IO.IOException"></exception>
            public override void Close()
            {
                delegate_.Close();
            }

            public override void Message(string component, string message)
            {
                if ("TP".Equals(component))
                {
                    testPoint.Apply(message);
                }
                if (delegate_.IsEnabled(component))
                {
                    delegate_.Message(component, message);
                }
            }

            public override bool IsEnabled(string component)
            {
                return "TP".Equals(component) || delegate_.IsEnabled(component);
            }
        }

        /// <summary>
        /// Simple interface that is executed for each <tt>TP</tt>
        /// <see cref="InfoStream">Lucene.Net.TestFramework.Util.InfoStream</see>
        /// component
        /// message. See also
        /// <see cref="RandomIndexWriter.MockIndexWriter(Directory, IndexWriterConfig, TestPoint)
        /// 	">RandomIndexWriter.MockIndexWriter(Directory, IndexWriterConfig, TestPoint)
        /// 	</see>
        /// </summary>
        public interface TestPoint
        {
            void Apply(string message);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }

}
