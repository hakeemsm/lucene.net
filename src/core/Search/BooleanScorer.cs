/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.Text;
using IndexReader = Lucene.Net.Index.IndexReader;

namespace Lucene.Net.Search
{

    /* Description from Doug Cutting (excerpted from
    * LUCENE-1483):
    *
    * BooleanScorer uses a ~16k array to score windows of
    * docs. So it scores docs 0-16k first, then docs 16-32k,
    * etc. For each window it iterates through all query terms
    * and accumulates a score in table[doc%16k]. It also stores
    * in the table a bitmask representing which terms
    * contributed to the score. Non-zero scores are chained in
    * a linked list. At the end of scoring each window it then
    * iterates through the linked list and, if the bitmask
    * matches the boolean constraints, collects a hit. For
    * boolean queries with lots of frequent terms this can be
    * much faster, since it does not need to update a priority
    * queue for each posting, instead performing constant-time
    * operations per posting. The only downside is that it
    * results in hits being delivered out-of-order within the
    * window, which means it cannot be nested within other
    * scorers. But it works well as a top-level scorer.
    *
    * The new BooleanScorer2 implementation instead works by
    * merging priority queues of postings, albeit with some
    * clever tricks. For example, a pure conjunction (all terms
    * required) does not require a priority queue. Instead it
    * sorts the posting streams at the start, then repeatedly
    * skips the first to to the last. If the first ever equals
    * the last, then there's a hit. When some terms are
    * required and some terms are optional, the conjunction can
    * be evaluated first, then the optional terms can all skip
    * to the match and be added to the score. Thus the
    * conjunction can reduce the number of priority queue
    * updates for the optional terms. */

    public sealed class BooleanScorer : BulkScorer
    {
        private sealed class BooleanScorerCollector : Collector
        {
            private BucketTable bucketTable;
            private int mask;
            private Scorer scorer;

            public BooleanScorerCollector(int mask, BucketTable bucketTable)
            {
                this.mask = mask;
                this.bucketTable = bucketTable;
            }

            public override void Collect(int doc)
            {
                BucketTable table = bucketTable;
                int i = doc & BucketTable.MASK;
                Bucket bucket = table.buckets[i];

                if (bucket.doc != doc)
                {
                    // invalid bucket
                    bucket.doc = doc; // set doc
                    bucket.score = scorer.Score(); // initialize score
                    bucket.bits = mask; // initialize mask
                    bucket.coord = 1; // initialize coord

                    bucket.next = table.first; // push onto valid list
                    table.first = bucket;
                }
                else
                {
                    // valid bucket
                    bucket.score += scorer.Score(); // increment score
                    bucket.bits |= mask; // add bits in mask
                    bucket.coord++; // increment coord
                }
            }

            public override void SetNextReader(AtomicReaderContext context)
            {
                // not needed by this implementation
            }

            public override void SetScorer(Scorer scorer)
            {
                this.scorer = scorer;
            }

            public override bool AcceptsDocsOutOfOrder
            {
                get { return true; }
            }
        }

        // An internal class which is used in score(Collector, int) for setting the
        // current score. This is required since Collector exposes a setScorer method
        // and implementations that need the score will call scorer.score().
        // Therefore the only methods that are implemented are score() and doc().
        private sealed class BucketScorer : Scorer
        {
            internal double score;
            internal int doc = NO_MORE_DOCS;
            internal int freq;

            public BucketScorer(Weight weight)
                : base(weight)
            {
            }

            public override int Advance(int target)
            {
                return NO_MORE_DOCS;
            }

            public override int DocID
            {
                get
                {
                    return doc;
                }
            }

            public override int Freq
            {
                get
                {
                    return freq;
                }
            }

            public override int NextDoc()
            {
                return NO_MORE_DOCS;
            }

            public override float Score()
            {
                return (float)score;
            }

            public override long Cost
            {
                get { return 1; }
            }
        }

        internal sealed class Bucket
        {
            internal int doc = -1; // tells if bucket is valid
            internal double score; // incremental score
            internal int bits; // used for bool constraints
            internal int coord; // count of terms in score
            internal Bucket next; // next valid bucket
        }

        /// <summary>A simple hash table of document scores within a range. </summary>
        internal sealed class BucketTable
        {
            public const int SIZE = 1 << 11;
            public const int MASK = SIZE - 1;

            internal readonly Bucket[] buckets = new Bucket[SIZE];
            internal Bucket first = null; // head of valid list

            public BucketTable()
            {
                // Pre-fill to save the lazy init when collecting
                // each sub:
                for (int idx = 0; idx < SIZE; idx++)
                {
                    buckets[idx] = new Bucket();
                }
            }

            public Collector NewCollector(int mask)
            {
                return new BooleanScorerCollector(mask, this);
            }

            public int Size()
            {
                return SIZE;
            }
        }

        internal sealed class SubScorer
        {
            public BulkScorer scorer;
            // TODO: re-enable this if BQ ever sends us required clauses
            //public bool required = false;
            public bool prohibited;
            public Collector collector;
            public SubScorer next;

            public bool more;
            public SubScorer(BulkScorer scorer, bool required, bool prohibited, Collector collector, SubScorer next)
            {
                if (required)
                {
                    throw new ArgumentException("this scorer cannot handle required=true");
                }
                this.scorer = scorer;
                this.more = true;
                // TODO: re-enable this if BQ ever sends us required clauses
                //this.required = required;
                this.prohibited = prohibited;
                this.collector = collector;
                this.next = next;
            }
        }

        private SubScorer scorers = null;
        private BucketTable bucketTable = new BucketTable();
        private readonly float[] coordFactors;
        // TODO: re-enable this if BQ ever sends us required clauses
        //private int requiredMask = 0;
        private int minNrShouldMatch;
        private int end;
        private Bucket current;
        // Any time a prohibited clause matches we set bit 0:
        private const int PROHIBITED_MASK = 1;

        private readonly Weight weight;
        public BooleanScorer(BooleanQuery.BooleanWeight weight, bool disableCoord, int minNrShouldMatch,
            IList<BulkScorer> optionalScorers, IEnumerable<BulkScorer> prohibitedScorers, int maxCoord)
        {
            this.minNrShouldMatch = minNrShouldMatch;
            this.weight = weight;
            foreach (BulkScorer scorer in optionalScorers)
            {
                scorers = new SubScorer(scorer, false, false, bucketTable.NewCollector(0), scorers);
            }

            foreach (BulkScorer scorer in prohibitedScorers)
            {
                scorers = new SubScorer(scorer, false, true, bucketTable.NewCollector(PROHIBITED_MASK), scorers);

            }

            coordFactors = new float[optionalScorers.Count + 1];
            for (int i = 0; i < coordFactors.Length; i++)
            {
                coordFactors[i] = disableCoord ? 1.0f : weight.Coord(i, maxCoord);
            }
        }

        // firstDocID is ignored since nextDoc() initializes 'current'
        public override bool Score(Collector collector, int max)
        {
            bool more;
            Bucket tmp;
            FakeScorer fs = new FakeScorer();

            // The internal loop will set the score and doc before calling collect.
            collector.SetScorer(fs);
            do
            {
                bucketTable.first = null;

                while (current != null)
                {
                    // more queued 

                    // check prohibited & required
                    if ((current.bits & PROHIBITED_MASK) == 0)
                    {
                        // TODO: re-enable this if BQ ever sends us required
                        // clauses
                        //&& (current.bits & requiredMask) == requiredMask) {

                        // NOTE: Lucene always passes max =
                        // Integer.MAX_VALUE today, because we never embed
                        // a BooleanScorer inside another (even though
                        // that should work)... but in theory an outside
                        // app could pass a different max so we must check
                        // it:
                        if (current.doc >= max)
                        {
                            tmp = current;
                            current = current.next;
                            tmp.next = bucketTable.first;
                            bucketTable.first = tmp;
                            continue;
                        }

                        if (current.coord >= minNrShouldMatch)
                        {
                            fs.score = (float) (current.score * coordFactors[current.coord]);
                            fs.doc = current.doc;
                            fs.freq = current.coord;
                            collector.Collect(current.doc);
                        }
                    }

                    current = current.next; // pop the queue
                }

                if (bucketTable.first != null)
                {
                    current = bucketTable.first;
                    bucketTable.first = current.next;
                    return true;
                }

                // refill the queue
                more = false;
                end += BucketTable.SIZE;
                for (SubScorer sub = scorers; sub != null; sub = sub.next)
                {
                    if (sub.more)
                    {
                        sub.more = sub.scorer.Score(sub.collector, end);
                        more |= sub.more;
                    }
                }
                current = bucketTable.first;
            }
            while (current != null || more);

            return false;
        }


        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append("boolean(");
            for (SubScorer sub = scorers; sub != null; sub = sub.next)
            {
                buffer.Append(sub.scorer.ToString());
                buffer.Append(" ");
            }
            buffer.Append(")");
            return buffer.ToString();
        }
    }
}