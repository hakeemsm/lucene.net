/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.TestFramework.Index;
using Lucene.Net.Util;
using Lucene.Net.TestFramework.Index;
using Directory = System.IO.Directory;

namespace Lucene.Net.TestFramework.Search
{
    /// <summary>Utility class for sanity-checking queries.</summary>
    /// <remarks>Utility class for sanity-checking queries.</remarks>
    public class QueryUtils
    {
        /// <summary>Check the types of things query objects should be able to do.</summary>
        /// <remarks>Check the types of things query objects should be able to do.</remarks>
        public static void Check(Query q)
        {
            CheckHashEquals(q);
        }

        /// <summary>check very basic hashCode and equals</summary>
        public static void CheckHashEquals(Query q)
        {
            Query q2 = (Query)q.Clone();
            CheckEqual(q, q2);
            Query q3 = (Query)q.Clone();
            q3.Boost = 7.21792348f;
            CheckUnequal(q, q3);
            // test that a class check is done so that no exception is thrown
            // in the implementation of equals()
            Query whacky = new AnonymousQuery();
            whacky.Boost = q.Boost;
            CheckUnequal(q, whacky);
        }

        private sealed class AnonymousQuery : Query
        {
            public override string ToString(string field)
            {
                return "My Whacky Query";
            }
        }

        // null test
         
        //assert.assertFalse(q.equals(null));
        public static void CheckEqual(Query q1, Query q2)
        {
        }

         
        //assert.assertEquals(q1, q2);
         
        //assert.assertEquals(q1.hashCode(), q2.hashCode());
        public static void CheckUnequal(Query q1, Query q2)
        {
        }

         
        //assert.assertFalse(q1 + " equal to " + q2, q1.equals(q2));
         
        //assert.assertFalse(q2 + " equal to " + q1, q2.equals(q1));
        // possible this test can fail on a hash collision... if that
        // happens, please change test to use a different example.
         
        //assert.assertTrue(q1.hashCode() != q2.hashCode());
        /// <summary>deep check that explanations of a query 'score' correctly</summary>
        /// <exception cref="System.IO.IOException"></exception>
        public static void CheckExplanations(Query q, IndexSearcher s)
        {
            CheckHits.CheckExplanations(q, null, s, true);
        }

        /// <summary>
        /// Various query sanity checks on a searcher, some checks are only done for
        /// instanceof IndexSearcher.
        /// </summary>
        /// <remarks>
        /// Various query sanity checks on a searcher, some checks are only done for
        /// instanceof IndexSearcher.
        /// </remarks>
        /// <seealso cref="Check(Query)">Check(Query)</seealso>
        /// <seealso cref="CheckFirstSkipTo(Query, IndexSearcher)">CheckFirstSkipTo(Query, IndexSearcher)
        /// 	</seealso>
        /// <seealso cref="CheckSkipTo(Query, IndexSearcher)">CheckSkipTo(Query, IndexSearcher)
        /// 	</seealso>
        /// <seealso cref="CheckExplanations(Query, IndexSearcher)">CheckExplanations(Query, IndexSearcher)
        /// 	</seealso>
        /// <seealso cref="CheckEqual(Query, Query)">CheckEqual(Query, Query)</seealso>
        public static void Check(Random random, Query q1, IndexSearcher s)
        {
            Check(random, q1, s, true);
        }

        public static void Check(Random random, Query q1, IndexSearcher s, bool wrap)
        {
            try
            {
                Check(q1);
                if (s != null)
                {
                    CheckFirstSkipTo(q1, s);
                    CheckSkipTo(q1, s);
                    if (wrap)
                    {
                        Check(random, q1, WrapUnderlyingReader(random, s, -1), false);
                        Check(random, q1, WrapUnderlyingReader(random, s, 0), false);
                        Check(random, q1, WrapUnderlyingReader(random, s, +1), false);
                    }
                    CheckExplanations(q1, s);
                    Query q2 = (Query)q1.Clone();
                    CheckEqual(s.Rewrite(q1), s.Rewrite(q2));
                }
            }
            catch (IOException e)
            {
                throw;
            }
        }

        /// <exception cref="System.IO.IOException"></exception>
        public static void PurgeFieldCache(IndexReader r)
        {
            // this is just a hack, to get an atomic reader that contains all subreaders for insanity checks
            FieldCache.DEFAULT.PurgeByCacheKey(SlowCompositeReaderWrapper.Wrap(r).CoreCacheKey);
        }

        /// <summary>
        /// This is a MultiReader that can be used for randomly wrapping other readers
        /// without creating FieldCache insanity.
        /// </summary>
        /// <remarks>
        /// This is a MultiReader that can be used for randomly wrapping other readers
        /// without creating FieldCache insanity.
        /// The trick is to use an opaque/fake cache key.
        /// </remarks>
        public class FCInvisibleMultiReader : MultiReader
        {
            private readonly object cacheKey = new object();

            protected internal FCInvisibleMultiReader(params IndexReader[] readers)
                : base(readers)
            {
            }

            public override object CoreCacheKey
            {
                get { return cacheKey; }
            }

            public override object CombinedCoreAndDeletesKey
            {
                get { return cacheKey; }
            }
        }

        /// <summary>
        /// Given an IndexSearcher, returns a new IndexSearcher whose IndexReader
        /// is a MultiReader containing the Reader of the original IndexSearcher,
        /// as well as several "empty" IndexReaders -- some of which will have
        /// deleted documents in them.
        /// </summary>
        /// <remarks>
        /// Given an IndexSearcher, returns a new IndexSearcher whose IndexReader
        /// is a MultiReader containing the Reader of the original IndexSearcher,
        /// as well as several "empty" IndexReaders -- some of which will have
        /// deleted documents in them.  This new IndexSearcher should
        /// behave exactly the same as the original IndexSearcher.
        /// </remarks>
        /// <param name="s">the searcher to wrap</param>
        /// <param name="edge">if negative, s will be the first sub; if 0, s will be in the middle, if positive s will be the last sub
        /// 	</param>
        /// <exception cref="System.IO.IOException"></exception>
        public static IndexSearcher WrapUnderlyingReader(Random random, IndexSearcher s,
            int edge)
        {
            IndexReader r = s.IndexReader;
            // we can't put deleted docs before the nested reader, because
            // it will throw off the docIds
            IndexReader[] readers =
			{ edge < 0 ? r : emptyReaders[0], emptyReaders
			    [0], new FCInvisibleMultiReader(edge < 0 ? emptyReaders[4] : emptyReaders
			        [0], emptyReaders[0], 0 == edge ? r : emptyReaders[0]), 0 < edge ? emptyReaders[
			            0] : emptyReaders[7], emptyReaders[0], new FCInvisibleMultiReader(0 <
			                                                                                         edge ? emptyReaders[0] : emptyReaders[5], emptyReaders[0], 0 < edge ? r : emptyReaders
			                                                                                             [0]) };
            IndexSearcher @out = LuceneTestCase.NewSearcher(new FCInvisibleMultiReader
                (readers));
            @out.Similarity = s.Similarity;
            return @out;
        }

        internal static readonly IndexReader[] emptyReaders = new IndexReader[8];

        static QueryUtils()
        {
            emptyReaders[0] = new MultiReader();
            emptyReaders[4] = MakeEmptyIndex(new Random(0), 4);
            emptyReaders[5] = MakeEmptyIndex(new Random(0), 5);
            emptyReaders[7] = MakeEmptyIndex(new Random(0), 7);
        }

        /// <exception cref="System.IO.IOException"></exception>
        private static IndexReader MakeEmptyIndex(Random random, int numDocs)
        {
            
            //assert numDocs > 0;
            Store.Directory d = new MockDirectoryWrapper(random, new RAMDirectory());
            var w = new IndexWriter(d, new IndexWriterConfig(LuceneTestCase.TEST_VERSION_CURRENT, new MockAnalyzer(random)));
            for (int i = 0; i < numDocs; i++)
            {
                w.AddDocument(new Document());
            }
            w.ForceMerge(1);
            w.Commit();
            w.Dispose();
            DirectoryReader reader = DirectoryReader.Open(d);
            return new AllDeletedFilterReader(LuceneTestCase.GetOnlySegmentReader(reader));
        }

        /// <summary>
        /// alternate scorer skipTo(),skipTo(),next(),next(),skipTo(),skipTo(), etc
        /// and ensure a hitcollector receives same docs and scores
        /// </summary>
        /// <exception cref="System.IO.IOException"></exception>
        public static void CheckSkipTo(Query q, IndexSearcher s)
        {
            //System.out.println("Checking "+q);
            IList<AtomicReaderContext> readerContextArray = s.TopReaderContext.Leaves;
            if (s.CreateNormalizedWeight(q).ScoresDocsOutOfOrder)
            {
                return;
            }
            // in this case order of skipTo() might differ from that of next().
            int skip_op = 0;
            int next_op = 1;
            int[][] orders =
            { new[] { next_op }, new[] { skip_op }, new[] { skip_op, next_op }, new[] { next_op, skip_op }, new[] { skip_op, 
                    skip_op, next_op, next_op }, new[] { next_op, next_op, skip_op, skip_op }, new[] { skip_op, skip_op, skip_op, next_op, next_op } };
            for (int k = 0; k < orders.Length; k++)
            {
                int[] order = orders[k];
                // System.out.print("Order:");for (int i = 0; i < order.length; i++)
                // System.out.print(order[i]==skip_op ? " skip()":" next()");
                // System.out.println();
                int[] opidx = new int[] { 0 };
                int[] lastDoc = new int[] { -1 };
                // FUTURE: ensure scorer.doc()==-1
                float maxDiff = 1e-5f;
                AtomicReader[] lastReader = new AtomicReader[] { null };
                s.Search(q, new AnonymousCollector(lastDoc, s, q, readerContextArray, order, opidx, skip_op
                    , maxDiff, lastReader));
                // System.out.println(op==skip_op ?
                // "skip("+(sdoc[0]+1)+")":"next()");
                // confirm that skipping beyond the last doc, on the
                // previous reader, hits NO_MORE_DOCS
                 
                //assert.assertFalse("query's last doc was "+ lastDoc[0] +" but skipTo("+(lastDoc[0]+1)+") got to "+scorer.docID(),more);
                 
                //assert readerContextArray.get(leafPtr).reader() == context.reader();
                if (lastReader[0] != null)
                {
                    // confirm that skipping beyond the last doc, on the
                    // previous reader, hits NO_MORE_DOCS
                    AtomicReader previousReader = lastReader[0];
                    IndexSearcher indexSearcher = LuceneTestCase.NewSearcher(previousReader, false);
                    indexSearcher.Similarity = s.Similarity;
                    Weight w = indexSearcher.CreateNormalizedWeight(q);
                    AtomicReaderContext ctx = ((AtomicReaderContext)previousReader.Context);
                    Scorer scorer = w.Scorer(ctx, ((AtomicReader)ctx.Reader).LiveDocs);
                    if (scorer != null)
                    {
                        bool more = scorer.Advance(lastDoc[0] + 1) != DocIdSetIterator.NO_MORE_DOCS;
                    }
                }
            }
        }

        private sealed class AnonymousCollector : Collector
        {
            public AnonymousCollector(int[] lastDoc, IndexSearcher s, Query q, IList<AtomicReaderContext
                > readerContextArray, int[] order, int[] opidx, int skip_op, float maxDiff, AtomicReader
                [] lastReader)
            {
                this.lastDoc = lastDoc;
                this.s = s;
                this.q = q;
                this.readerContextArray = readerContextArray;
                this.order = order;
                this.opidx = opidx;
                this.skip_op = skip_op;
                this.maxDiff = maxDiff;
                this.lastReader = lastReader;
            }

            private Scorer sc;

            private Scorer scorer;

            private int leafPtr;

            public override void SetScorer(Scorer scorer)
            {
                this.sc = scorer;
            }

            /// <exception cref="System.IO.IOException"></exception>
            public override void Collect(int doc)
            {
                float score = this.sc.Score();
                lastDoc[0] = doc;
                if (this.scorer == null)
                {
                    Weight w = s.CreateNormalizedWeight(q);
                    AtomicReaderContext context = readerContextArray[this.leafPtr];
                    this.scorer = w.Scorer(context, ((AtomicReader)context.Reader).LiveDocs);
                }
                int op = order[(opidx[0]++) % order.Length];
                bool more = op == skip_op ? this.scorer.Advance(this.scorer.DocID + 1) != DocIdSetIterator
                    .NO_MORE_DOCS : this.scorer.NextDoc() != DocIdSetIterator.NO_MORE_DOCS;
                int scorerDoc = this.scorer.DocID;
                float scorerScore = this.scorer.Score();
                float scorerScore2 = this.scorer.Score();
                float scoreDiff = Math.Abs(score - scorerScore);
                float scorerDiff = Math.Abs(scorerScore2 - scorerScore);
                if (!more || doc != scorerDoc || scoreDiff > maxDiff || scorerDiff > maxDiff)
                {
                    StringBuilder sbord = new StringBuilder();
                    for (int i = 0; i < order.Length; i++)
                    {
                        sbord.Append(order[i] == skip_op ? " skip()" : " next()");
                    }
                    throw new Exception("ERROR matching docs:" + "\n\t" + (doc != scorerDoc ?
                        "--> " : string.Empty) + "doc=" + doc + ", scorerDoc=" + scorerDoc + "\n\t" + (!
                            more ? "--> " : string.Empty) + "tscorer.more=" + more + "\n\t" + (scoreDiff > maxDiff
                                ? "--> " : string.Empty) + "scorerScore=" + scorerScore + " scoreDiff=" + scoreDiff
                                        + " maxDiff=" + maxDiff + "\n\t" + (scorerDiff > maxDiff ? "--> " : string.Empty
                                            ) + "scorerScore2=" + scorerScore2 + " scorerDiff=" + scorerDiff + "\n\thitCollector.doc="
                                        + doc + " score=" + score + "\n\t Scorer=" + this.scorer + "\n\t Query=" + q +
                                        "  " + q.GetType().FullName + "\n\t Searcher=" + s + "\n\t Order=" + sbord + "\n\t Op="
                                        + (op == skip_op ? " skip()" : " next()"));
                }
            }

            /// <exception cref="System.IO.IOException"></exception>
            public override void SetNextReader(AtomicReaderContext context)
            {
                if (lastReader[0] != null)
                {
                    AtomicReader previousReader = lastReader[0];
                    IndexSearcher indexSearcher = LuceneTestCase.NewSearcher(previousReader);
                    indexSearcher.Similarity = s.Similarity;
                    Weight w = indexSearcher.CreateNormalizedWeight(q);
                    AtomicReaderContext ctx = (AtomicReaderContext)indexSearcher.TopReaderContext;
                    Scorer scorer = w.Scorer(ctx, ((AtomicReader)ctx.Reader).LiveDocs);
                    if (scorer != null)
                    {
                        bool more = scorer.Advance(lastDoc[0] + 1) != DocIdSetIterator.NO_MORE_DOCS;
                    }
                    this.leafPtr++;
                }
                lastReader[0] = ((AtomicReader)context.Reader);
                this.scorer = null;
                lastDoc[0] = -1;
            }

            public override bool AcceptsDocsOutOfOrder
            {
                get { return false; }
            }

            private readonly int[] lastDoc;

            private readonly IndexSearcher s;

            private readonly Query q;

            private readonly IList<AtomicReaderContext> readerContextArray;

            private readonly int[] order;

            private readonly int[] opidx;

            private readonly int skip_op;

            private readonly float maxDiff;

            private readonly AtomicReader[] lastReader;
        }

         
        //assert.assertFalse("query's last doc was "+ lastDoc[0] +" but skipTo("+(lastDoc[0]+1)+") got to "+scorer.docID(),more);
        /// <summary>check that first skip on just created scorers always goes to the right doc
        /// 	</summary>
        /// <exception cref="System.IO.IOException"></exception>
        public static void CheckFirstSkipTo(Query q, IndexSearcher s)
        {
            //System.out.println("checkFirstSkipTo: "+q);
            float maxDiff = 1e-3f;
            int[] lastDoc = new int[] { -1 };
            AtomicReader[] lastReader = { null };
            IList<AtomicReaderContext> context = s.TopReaderContext.Leaves;
            s.Search(q, new AnoynmousCollector2(lastDoc, s, q, context, lastReader));
             
            //assert.assertTrue("query collected "+doc+" but skipTo("+i+") says no more docs!",scorer.advance(i) != DocIdSetIterator.NO_MORE_DOCS);
             
            //assert.assertEquals("query collected "+doc+" but skipTo("+i+") got to "+scorer.docID(),doc,scorer.docID());
             
            //assert.assertEquals("unstable skipTo("+i+") score!",skipToScore,scorer.score(),maxDiff); 
             
            //assert.assertEquals("query assigned doc "+doc+" a score of <"+score+"> but skipTo("+i+") has <"+skipToScore+">!",score,skipToScore,maxDiff);
            // Hurry things along if they are going slow (eg
            // if you got SimpleText codec this will kick in):
            // confirm that skipping beyond the last doc, on the
            // previous reader, hits NO_MORE_DOCS
             
            //assert.assertFalse("query's last doc was "+ lastDoc[0] +" but skipTo("+(lastDoc[0]+1)+") got to "+scorer.docID(),more);
            if (lastReader[0] != null)
            {
                // confirm that skipping beyond the last doc, on the
                // previous reader, hits NO_MORE_DOCS
                AtomicReader previousReader = lastReader[0];
                IndexSearcher indexSearcher = LuceneTestCase.NewSearcher(previousReader);
                indexSearcher.Similarity = s.Similarity;
                Weight w = indexSearcher.CreateNormalizedWeight(q);
                Scorer scorer = w.Scorer((AtomicReaderContext)indexSearcher.TopReaderContext, previousReader.LiveDocs);
                if (scorer != null)
                {
                    bool more = scorer.Advance(lastDoc[0] + 1) != DocIdSetIterator.NO_MORE_DOCS;
                }
            }
        }

        private sealed class AnoynmousCollector2 : Collector
        {
            public AnoynmousCollector2(int[] lastDoc, IndexSearcher s, Query q, IList<AtomicReaderContext
                > context, AtomicReader[] lastReader)
            {
                this.lastDoc = lastDoc;
                this.s = s;
                this.q = q;
                this.context = context;
                this.lastReader = lastReader;
            }

            private Scorer scorer;

            private int leafPtr;

            private IBits liveDocs;

            public override void SetScorer(Scorer scorer)
            {
                this.scorer = scorer;
            }

            /// <exception cref="System.IO.IOException"></exception>
            public override void Collect(int doc)
            {
                float score = this.scorer.Score();
                long startMS = DateTime.Now.CurrentTimeMillis();
                for (int i = lastDoc[0] + 1; i <= doc; i++)
                {
                    Weight w = s.CreateNormalizedWeight(q);
                    Scorer scorer = w.Scorer(context[this.leafPtr], this.liveDocs);
                    float skipToScore = scorer.Score();
                    if (i < doc && DateTime.Now.CurrentTimeMillis() - startMS > 5)
                    {
                        i = doc - 1;
                    }
                }
                lastDoc[0] = doc;
            }

            /// <exception cref="System.IO.IOException"></exception>
            public override void SetNextReader(AtomicReaderContext context)
            {
                if (lastReader[0] != null)
                {
                    AtomicReader previousReader = lastReader[0];
                    IndexSearcher indexSearcher = LuceneTestCase.NewSearcher(previousReader);
                    indexSearcher.Similarity = s.Similarity;
                    Weight w = indexSearcher.CreateNormalizedWeight(q);
                    Scorer scorer = w.Scorer((AtomicReaderContext)indexSearcher.TopReaderContext, previousReader.LiveDocs);
                    if (scorer != null)
                    {
                        bool more = scorer.Advance(lastDoc[0] + 1) != DocIdSetIterator.NO_MORE_DOCS;
                    }
                    this.leafPtr++;
                }
                lastReader[0] = ((AtomicReader)context.Reader);
                lastDoc[0] = -1;
                this.liveDocs = ((AtomicReader)context.Reader).LiveDocs;
            }

            public override bool AcceptsDocsOutOfOrder
            {
                get { return false; }
            }

            private readonly int[] lastDoc;

            private readonly IndexSearcher s;

            private readonly Query q;

            private readonly IList<AtomicReaderContext> context;

            private readonly AtomicReader[] lastReader;
        }
         
        //assert.assertFalse("query's last doc was "+ lastDoc[0] +" but skipTo("+(lastDoc[0]+1)+") got to "+scorer.docID(),more);
    }
}
