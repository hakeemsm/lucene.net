﻿using System.Collections;
using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Support;
using Lucene.Net.Util;

namespace Lucene.Net.Queries.Function
{

    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */
    /// <summary>
    /// Returns a score for each document based on a ValueSource,
    /// often some function of the value of a field.
    /// 
    /// <b>Note: This API is experimental and may change in non backward-compatible ways in the future</b>
    /// 
    /// 
    /// </summary>
    public class FunctionQuery : Query
    {
        internal readonly ValueSource func;

        /// <param name="func"> defines the function to be used for scoring </param>
        public FunctionQuery(ValueSource func)
        {
            this.func = func;
        }

        /// <returns> The associated ValueSource </returns>
        public virtual ValueSource ValueSource
        {
            get
            {
                return func;
            }
        }

        public override Query Rewrite(IndexReader reader)
        {
            return this;
        }

        public override void ExtractTerms(ISet<Term> terms)
        {
        }

        protected internal class FunctionWeight : Weight
        {
            private readonly FunctionQuery outerInstance;

            protected readonly IndexSearcher searcher;
            protected internal float queryNorm;
            protected float queryWeight;
            protected internal readonly IDictionary context;

            public FunctionWeight(FunctionQuery outerInstance, IndexSearcher searcher)
            {
                this.outerInstance = outerInstance;
                this.searcher = searcher;
                this.context = ValueSource.NewContext(searcher);
                outerInstance.func.CreateWeight(context, searcher);
            }

            public override Query Query
            {
                get
                {
                    return outerInstance;
                }
            }

            public override float ValueForNormalization
            {
                get
                {
                    queryWeight = outerInstance.Boost;
                    return queryWeight * queryWeight;
                }
            }

            public override void Normalize(float norm, float topLevelBoost)
            {
                this.queryNorm = norm * topLevelBoost;
                queryWeight *= this.queryNorm;
            }

            public override Scorer Scorer(AtomicReaderContext ctx, Bits acceptDocs)
            {
                return new AllScorer(outerInstance, ctx, acceptDocs, this, queryWeight);
            }

            public override Explanation Explain(AtomicReaderContext ctx, int doc)
            {
                return ((AllScorer)Scorer(ctx, ctx.AtomicReader.LiveDocs)).Explain(doc);
            }
        }

        protected internal class AllScorer : Scorer
        {
            private readonly FunctionQuery outerInstance;

            internal readonly IndexReader reader;
            internal readonly FunctionWeight weight;
            internal readonly int maxDoc;
            internal readonly float qWeight;
            internal int doc = -1;
            internal readonly FunctionValues vals;
            internal readonly Bits acceptDocs;

            //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
            //ORIGINAL LINE: public AllScorer(org.apache.lucene.index.AtomicReaderContext context, org.apache.lucene.util.Bits acceptDocs, FunctionWeight w, float qWeight) throws java.io.IOException
            public AllScorer(FunctionQuery outerInstance, AtomicReaderContext context, Bits acceptDocs, FunctionWeight w, float qWeight)
                : base(w)
            {
                this.outerInstance = outerInstance;
                this.weight = w;
                this.qWeight = qWeight;
                this.reader = context.Reader;
                this.maxDoc = reader.MaxDoc;
                this.acceptDocs = acceptDocs;
                vals = outerInstance.func.GetValues(weight.context, context);
            }

            public override int DocID()
            {
                return doc;
            }

            // instead of matching all docs, we could also embed a query.
            // the score could either ignore the subscore, or boost it.
            // Containment:  floatline(foo:myTerm, "myFloatField", 1.0, 0.0f)
            // Boost:        foo:myTerm^floatline("myFloatField",1.0,0.0f)
            public override int NextDoc()
            {
                for (; ; )
                {
                    ++doc;
                    if (doc >= maxDoc)
                    {
                        return doc = NO_MORE_DOCS;
                    }
                    if (acceptDocs != null && !acceptDocs.Get(doc))
                    {
                        continue;
                    }
                    return doc;
                }
            }

            public override int Advance(int target)
            {
                // this will work even if target==NO_MORE_DOCS
                doc = target - 1;
                return NextDoc();
            }

            public override float Score()
            {
                float score = qWeight * vals.FloatVal(doc);

                // Current Lucene priority queues can't handle NaN and -Infinity, so
                // map to -Float.MAX_VALUE. This conditional handles both -infinity
                // and NaN since comparisons with NaN are always false.
                return score > float.NegativeInfinity ? score : -float.MaxValue;
            }

            public override long Cost()
            {
                return maxDoc;
            }

            public override int Freq()
            {
                return 1;
            }

            public virtual Explanation Explain(int d)
            {
                float sc = qWeight * vals.FloatVal(d);

                Explanation result = new ComplexExplanation(true, sc, "FunctionQuery(" + outerInstance.func + "), product of:");

                result.AddDetail(vals.Explain(d));
                result.AddDetail(new Explanation(outerInstance.Boost, "boost"));
                result.AddDetail(new Explanation(weight.queryNorm, "queryNorm"));
                return result;
            }
        }

        public override Weight CreateWeight(IndexSearcher searcher)
        {
            return new FunctionQuery.FunctionWeight(this, searcher);
        }

        /// <summary>
        /// Prints a user-readable version of this query.
        /// </summary>
        public override string ToString(string field)
        {
            float boost = Boost;
            return (boost != 1.0 ? "(" : "") + func + (boost == 1.0 ? "" : ")^" + boost);
        }


        /// <summary>
        /// Returns true if <code>o</code> is equal to this. </summary>
        public override bool Equals(object o)
        {
            var other = o as FunctionQuery;
            if (other == null)
            {
                return false;
            }
            return Boost == other.Boost && func.Equals(other.func);
        }

        /// <summary>
        /// Returns a hash code value for this object. </summary>
        public override int GetHashCode()
        {
            return func.GetHashCode() * 31 + Number.FloatToIntBits(Boost);
        }
    }
}