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

using System;
using Lucene.Net.Index;
using IndexReader = Lucene.Net.Index.IndexReader;
using ToStringUtils = Lucene.Net.Util.ToStringUtils;
using Lucene.Net.Util;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Lucene.Net.Search
{


    /// <summary> A query that applies a filter to the results of another query.
    /// 
    /// <p/>Note: the bits are retrieved from the filter each time this
    /// query is used in a search - use a CachingWrapperFilter to avoid
    /// regenerating the bits every time.
    /// 
    /// <p/>Created: Apr 20, 2004 8:58:29 AM
    /// 
    /// </summary>
    /// <since>1.4</since>
    /// <seealso cref="CachingWrapperFilter"/>
    [Serializable]
    public class FilteredQuery : Query
    {
        private readonly Query query;
        private readonly Filter filter;
        private readonly FilterStrategy strategy;

        /// <summary> Constructs a new query which applies a filter to the results of the original query.
        /// Filter.getDocIdSet() will be called every time this query is used in a search.
        /// </summary>
        /// <param name="query"> Query to be filtered, cannot be <c>null</c>.
        /// </param>
        /// <param name="filter">Filter to apply to query results, cannot be <c>null</c>.
        /// </param>
        public FilteredQuery(Query query, Filter filter)
            : this(query, filter, RANDOM_ACCESS_FILTER_STRATEGY)
        {
        }

        public FilteredQuery(Query query, Filter filter, FilterStrategy strategy)
        {
            if (query == null || filter == null)
                throw new ArgumentException("Query and filter cannot be null.");
            if (strategy == null)
                throw new ArgumentException("FilterStrategy can not be null");
            this.strategy = strategy;
            this.query = query;
            this.filter = filter;
        }

        /// <summary> Returns a Weight that applies the filter to the enclosed query's Weight.
        /// This is accomplished by overriding the Scorer returned by the Weight.
        /// </summary>
        public override Weight CreateWeight(IndexSearcher searcher)
        {
            Weight weight = query.CreateWeight(searcher);

            return new AnonymousClassWeight(weight, this);
        }

        [Serializable]
        private class AnonymousClassWeight : Weight
        {
            private readonly Weight weight;
            private readonly FilteredQuery enclosingInstance;

            private float value;

            public AnonymousClassWeight(Weight weight, FilteredQuery enclosingInstance)
            {
                this.weight = weight;
                this.enclosingInstance = enclosingInstance;
            }

            public override bool ScoresDocsOutOfOrder
            {
                get
                {
                    return true;
                }
            }

            public override float ValueForNormalization
            {
                get
                {
                    return weight.ValueForNormalization * enclosingInstance.Boost * enclosingInstance.Boost; // boost sub-weight
                }
            }

            public override void Normalize(float norm, float topLevelBoost)
            {
                weight.Normalize(norm, topLevelBoost * enclosingInstance.Boost); // incorporate boost
            }

            public override Explanation Explain(AtomicReaderContext ir, int i)
            {
                Explanation inner = weight.Explain(ir, i);
                Filter f = enclosingInstance.filter;
                DocIdSet docIdSet = f.GetDocIdSet(ir, ir.AtomicReader.LiveDocs);
                DocIdSetIterator docIdSetIterator = docIdSet == null ? DocIdSet.EMPTY_DOCIDSET.Iterator() : docIdSet.Iterator();
                if (docIdSetIterator == null)
                {
                    docIdSetIterator = DocIdSet.EMPTY_DOCIDSET.Iterator();
                }
                if (docIdSetIterator.Advance(i) == i)
                {
                    return inner;
                }
                else
                {
                    Explanation result = new Explanation
                      (0.0f, "failure to match filter: " + f.ToString());
                    result.AddDetail(inner);
                    return result;
                }
            }

            // return this query
            public override Query Query
            {
                get { return enclosingInstance; }
            }

			public override Scorer Scorer(AtomicReaderContext context, IBits acceptDocs)
            {
                //assert filter != null;
                DocIdSet filterDocIdSet = enclosingInstance.filter.GetDocIdSet(context, acceptDocs);
                if (filterDocIdSet == null)
                {
                    // this means the filter does not accept any documents.
                    return null;
                }

                return enclosingInstance.strategy.FilteredScorer(context, weight, filterDocIdSet);
            }
			public override BulkScorer BulkScorer(AtomicReaderContext context, bool scoreDocsInOrder
				, IBits acceptDocs)
			{
				//HM:revisit 
				//assert filter != null;
				DocIdSet filterDocIdSet = this.enclosingInstance.filter.GetDocIdSet(context, acceptDocs);
				if (filterDocIdSet == null)
				{
					// this means the filter does not accept any documents.
					return null;
				}
				return this.enclosingInstance.strategy.FilteredBulkScorer(context, weight, scoreDocsInOrder
					, filterDocIdSet);
			}
        }

        private sealed class QueryFirstScorer : Scorer
        {
            private readonly Scorer scorer;
            private int scorerDoc = -1;
            private IBits filterbits;

            internal QueryFirstScorer(Weight weight, IBits filterBits, Scorer other)
                : base(weight)
            {
                this.scorer = other;
                this.filterbits = filterBits;
            }


            public override int NextDoc()
            {
                int doc;
                for (; ; )
                {
                    doc = scorer.NextDoc();
                    if (doc == Scorer.NO_MORE_DOCS || filterbits[doc])
                    {
                        return scorerDoc = doc;
                    }
                }
            }

            public override int Advance(int target)
            {
                int doc = scorer.Advance(target);
                if (doc != Scorer.NO_MORE_DOCS && !filterbits[doc])
                {
                    return scorerDoc = NextDoc();
                }
                return scorerDoc = doc;
            }

            public override int DocID
            {
                get { return scorerDoc; }
            }

            public override float Score()
            {
                return scorer.Score();
            }

            public override int Freq
            {
                get { return scorer.Freq; }
            }

            public override ICollection<ChildScorer> Children
            {
                get
                {
                    return new ReadOnlyCollection<ChildScorer>(new[] { new ChildScorer(scorer, "FILTERED") });
                }
            }

            public override long Cost
            {
                get { return scorer.Cost; }
            }
        }

		private class QueryFirstBulkScorer : BulkScorer
		{
			private readonly Scorer scorer;

			private readonly IBits filterBits;

			public QueryFirstBulkScorer(Scorer scorer, IBits filterBits)
			{
				this.scorer = scorer;
				this.filterBits = filterBits;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override bool Score(Collector collector, int maxDoc)
			{
				// the normalization trick already applies the boost of this query,
				// so we can use the wrapped scorer directly:
				collector.SetScorer(scorer);
				if (scorer.DocID == -1)
				{
					scorer.NextDoc();
				}
				while (true)
				{
					int scorerDoc = scorer.DocID;
					if (scorerDoc < maxDoc)
					{
						if (filterBits[scorerDoc])
						{
							collector.Collect(scorerDoc);
						}
						scorer.NextDoc();
					}
					else
					{
						break;
					}
				}
				return scorer.DocID != Scorer.NO_MORE_DOCS;
			}
		}
        private class LeapFrogScorer : Scorer
        {
            private readonly DocIdSetIterator secondary;
            private readonly DocIdSetIterator primary;
            private readonly Scorer scorer;
            protected int primaryDoc = -1;
            protected int secondaryDoc = -1;

            internal LeapFrogScorer(Weight weight, DocIdSetIterator primary, DocIdSetIterator secondary, Scorer scorer)
                : base(weight)
            {
                this.primary = primary;
                this.secondary = secondary;
                this.scorer = scorer;
            }


            private int AdvanceToNextCommonDoc()
            {
                for (; ; )
                {
                    if (secondaryDoc < primaryDoc)
                    {
                        secondaryDoc = secondary.Advance(primaryDoc);
                    }
                    else if (secondaryDoc == primaryDoc)
                    {
                        return primaryDoc;
                    }
                    else
                    {
                        primaryDoc = primary.Advance(secondaryDoc);
                    }
                }
            }

            public override int NextDoc()
            {
                primaryDoc = PrimaryNext();
                return AdvanceToNextCommonDoc();
            }

            protected virtual int PrimaryNext()
            {
                return primary.NextDoc();
            }

            public override int Advance(int target)
            {
                if (target > primaryDoc)
                {
                    primaryDoc = primary.Advance(target);
                }
                return AdvanceToNextCommonDoc();
            }

            public override int DocID
            {
                get { return secondaryDoc; }
            }

            public override float Score()
            {
                return scorer.Score();
            }

            public override int Freq
            {
                get { return scorer.Freq; }
            }

            public override ICollection<ChildScorer> Children
            {
                get
                {
                    return new ReadOnlyCollection<ChildScorer>(new[] { new ChildScorer(scorer, "FILTERED") });
                }
            }

            public override long Cost
            {
                get { return Math.Min(primary.Cost, secondary.Cost); }
            }
        }

        private sealed class PrimaryAdvancedLeapFrogScorer : LeapFrogScorer
        {
            private readonly int firstFilteredDoc;

            internal PrimaryAdvancedLeapFrogScorer(Weight weight, int firstFilteredDoc, DocIdSetIterator filterIter, Scorer other)
                : base(weight, filterIter, other, other)
            {
                this.firstFilteredDoc = firstFilteredDoc;
                this.primaryDoc = firstFilteredDoc; // initialize to prevent and advance call to move it further
            }

            protected override int PrimaryNext()
            {
                if (secondaryDoc != -1)
                {
                    return base.PrimaryNext();
                }
                else
                {
                    return firstFilteredDoc;
                }
            }
        }

        /// <summary>Rewrites the wrapped query. </summary>
        public override Query Rewrite(IndexReader reader)
        {
            Query queryRewritten = query.Rewrite(reader);


            if (queryRewritten != query)
            {
                // rewrite to a new FilteredQuery wrapping the rewritten query
                Query rewritten = new FilteredQuery(queryRewritten, filter, strategy);
                rewritten.Boost = this.Boost;
                return rewritten;
            }
            else
            {
                // nothing to rewrite, we are done!
                return this;
            }
        }

        public Query Query
        {
            get { return query; }
        }

        public Filter Filter
        {
            get { return filter; }
        }

        public FilterStrategy FilterStrategy
        {
            get { return FilterStrategy; }
        }

        // inherit javadoc
        public override void ExtractTerms(ISet<Term> terms)
        {
            Query.ExtractTerms(terms);
        }

        /// <summary>Prints a user-readable version of this query. </summary>
        public override string ToString(string s)
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append("filtered(");
            buffer.Append(query.ToString(s));
            buffer.Append(")->");
            buffer.Append(filter);
            buffer.Append(ToStringUtils.Boost(Boost));
            return buffer.ToString();
        }

        /// <summary>Returns true iff <c>o</c> is equal to this. </summary>
        public override bool Equals(object o)
        {
            if (o == this)
                return true;
            if (!base.Equals(o))
                return false;
            //assert o instanceof FilteredQuery;
            FilteredQuery fq = (FilteredQuery)o;
            return fq.query.Equals(this.query) && fq.filter.Equals(this.filter) && fq.strategy.Equals(this.strategy);
        }

        /// <summary>Returns a hash code value for this object. </summary>
        public override int GetHashCode()
        {
            int hash = base.GetHashCode();
            hash = hash * 31 + strategy.GetHashCode();
            hash = hash * 31 + query.GetHashCode();
            hash = hash * 31 + filter.GetHashCode();
            return hash;
        }

        public static readonly FilterStrategy RANDOM_ACCESS_FILTER_STRATEGY = new RandomAccessFilterStrategy();

        public static readonly FilterStrategy LEAP_FROG_FILTER_FIRST_STRATEGY = new LeapFrogFilterStrategy(false);

        public static readonly FilterStrategy LEAP_FROG_QUERY_FIRST_STRATEGY = new LeapFrogFilterStrategy(true);

        public static readonly FilterStrategy QUERY_FIRST_FILTER_STRATEGY = new QueryFirstFilterStrategy();

		
        public class RandomAccessFilterStrategy : FilterStrategy
        {
			public override Scorer FilteredScorer(AtomicReaderContext context, Weight weight, 
				DocIdSet docIdSet)
            {
                DocIdSetIterator filterIter = docIdSet.Iterator();
                if (filterIter == null)
                {
                    // this means the filter does not accept any documents.
                    return null;
                }

                int firstFilterDoc = filterIter.NextDoc();
                if (firstFilterDoc == DocIdSetIterator.NO_MORE_DOCS)
                {
                    return null;
                }

                IBits filterAcceptDocs = docIdSet.Bits;
                // force if RA is requested
                bool useRandomAccess = (filterAcceptDocs != null && (UseRandomAccess(filterAcceptDocs, firstFilterDoc)));
                if (useRandomAccess)
                {
                    // if we are using random access, we return the inner scorer, just with other acceptDocs
					return weight.Scorer(context, filterAcceptDocs);
                }
                else
                {
                    //assert firstFilterDoc > -1;
                    // we are gonna advance() this scorer, so we set inorder=true/toplevel=false
                    // we pass null as acceptDocs, as our filter has already respected acceptDocs, no need to do twice
                    Scorer scorer = weight.Scorer(context, null);
                    // TODO once we have way to figure out if we use RA or LeapFrog we can remove this scorer
                    return (scorer == null) ? null : new PrimaryAdvancedLeapFrogScorer(weight, firstFilterDoc, filterIter, scorer);
                }
            }

            protected bool UseRandomAccess(IBits bits, int firstFilterDoc)
            {
                //TODO once we have a cost API on filters and scorers we should rethink this heuristic
                return firstFilterDoc < 100;
            }
        }

        private sealed class LeapFrogFilterStrategy : FilterStrategy
        {
            private readonly bool scorerFirst;

            internal LeapFrogFilterStrategy(bool scorerFirst)
            {
                this.scorerFirst = scorerFirst;
            }

			public override Scorer FilteredScorer(AtomicReaderContext context, Weight weight, 
				DocIdSet docIdSet)
            {
                DocIdSetIterator filterIter = docIdSet.Iterator();
                if (filterIter == null)
                {
                    // this means the filter does not accept any documents.
                    return null;
                }
                // we are gonna advance() this scorer, so we set inorder=true/toplevel=false
                // we pass null as acceptDocs, as our filter has already respected acceptDocs, no need to do twice
				Scorer scorer = weight.Scorer(context, null);
				if (scorer == null)
				{
					return null;
				}
				return scorerFirst ? new LeapFrogScorer(weight, scorer, filterIter, scorer) : new LeapFrogScorer(weight, filterIter, scorer, scorer);
            }
        }

        private sealed class QueryFirstFilterStrategy : FilterStrategy
        {
			public override Scorer FilteredScorer(AtomicReaderContext context, Weight weight, 
				DocIdSet docIdSet)
            {
                IBits filterAcceptDocs = docIdSet.Bits;
                if (filterAcceptDocs == null)
                {
					return LEAP_FROG_QUERY_FIRST_STRATEGY.FilteredScorer(context, weight, docIdSet);
                }
				Scorer scorer = weight.Scorer(context, null);
                return scorer == null ? null : new QueryFirstScorer(weight,
                    filterAcceptDocs, scorer);
            }
        }
    }

    // .NET Port: Moving this out of FilteredQuery to avoid conflict with property
    /// <summary>
    /// Abstract class that defines how the filter (
    /// <see cref="DocIdSet">DocIdSet</see>
    /// ) applied during document collection.
    /// </summary>
    public abstract class FilterStrategy
    {
        /// <summary>
        /// Returns a filtered
        /// <see cref="Scorer">Scorer</see>
        /// based on this strategy.
        /// </summary>
        /// <param name="context">
        /// the
        /// <see cref="AtomicReaderContext">AtomicReaderContext
        /// 	</see>
        /// for which to return the
        /// <see cref="Scorer">Scorer</see>
        /// .
        /// </param>
        /// <param name="weight">
        /// the
        /// <see cref="FilteredQuery">FilteredQuery</see>
        /// 
        /// <see cref="Weight">Weight</see>
        /// to create the filtered scorer.
        /// </param>
        /// <param name="docIdSet">
        /// the filter
        /// <see cref="DocIdSet">DocIdSet</see>
        /// to apply
        /// </param>
        /// <returns>a filtered scorer</returns>
        /// <exception cref="System.IO.IOException">
        /// if an
        /// <see cref="System.IO.IOException">System.IO.IOException</see>
        /// occurs
        /// </exception>
        public abstract Scorer FilteredScorer(AtomicReaderContext context, Weight weight,DocIdSet docIdSet);

        /// <summary>
        /// Returns a filtered
        /// <see cref="BulkScorer">BulkScorer</see>
        /// based on this
        /// strategy.  This is an optional method: the default
        /// implementation just calls
        /// <see cref="FilteredScorer(AtomicReaderContext, Weight, DocIdSet)">FilteredScorer(AtomicReaderContext, Weight, DocIdSet)</see>
        /// and
        /// wraps that into a BulkScorer.
        /// </summary>
        /// <param name="context">
        /// the
        /// <see cref="AtomicReaderContext">AtomicReaderContext
        /// 	</see>
        /// for which to return the
        /// <see cref="Scorer">Scorer</see>
        /// .
        /// </param>
        /// <param name="weight">
        /// the
        /// <see cref="FilteredQuery">FilteredQuery</see>
        /// 
        /// <see cref="Weight">Weight</see>
        /// to create the filtered scorer.
        /// </param>
        /// <param name="docIdSet">
        /// the filter
        /// <see cref="DocIdSet">DocIdSet</see>
        /// to apply
        /// </param>
        /// <returns>a filtered top scorer</returns>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual BulkScorer FilteredBulkScorer(AtomicReaderContext context, Weight weight, bool scoreDocsInOrder, DocIdSet docIdSet)
        {
            Scorer scorer = FilteredScorer(context, weight, docIdSet);
            if (scorer == null)
            {
                return null;
            }
            // This impl always scores docs in order, so we can
            // ignore scoreDocsInOrder:
            return new Weight.DefaultBulkScorer(scorer);
        }
    }

}