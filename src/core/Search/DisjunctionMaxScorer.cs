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

namespace Lucene.Net.Search
{

    /// <summary> The Scorer for DisjunctionMaxQuery's.  The union of all documents generated by the the subquery scorers
    /// is generated in document number order.  The score for each document is the maximum of the scores computed
    /// by the subquery scorers that generate that document, plus tieBreakerMultiplier times the sum of the scores
    /// for the other subqueries that generate the document.
    /// </summary>
    class DisjunctionMaxScorer : DisjunctionScorer
    {
        /* Multiplier applied to non-maximum-scoring subqueries for a document as they are summed into the result. */
        private readonly float tieBreakerMultiplier;
        private int doc = -1;
        private int freq = -1;

        /* Used when scoring currently matching doc. */
        private float scoreSum;
        private float scoreMax;

		public DisjunctionMaxScorer(Weight weight, float tieBreakerMultiplier, Scorer[] subScorers
			) : base(weight, subScorers)
        {
            this.tieBreakerMultiplier = tieBreakerMultiplier;
        }


        /// <summary>Determine the current document score.  Initially invalid, until <see cref="NextDoc()" /> is called the first time.</summary>
        /// <returns> the score of the current generated document
        /// </returns>
        public override float Score()
        {
            return scoreMax + (scoreSum - scoreMax) * tieBreakerMultiplier;
        }

		protected internal override void AfterNext()
        {
            doc = subScorers[0].DocID;
            if (doc != NO_MORE_DOCS)
            {
                scoreSum = scoreMax = subScorers[0].Score();
                freq = 1;
                ScoreAll(1);
                ScoreAll(2);
            }
        }

        // Recursively iterate all subScorers that generated last doc computing sum and max
        private void ScoreAll(int root)
        {
            if (root < numScorers && subScorers[root].DocID == doc)
            {
                float sub = subScorers[root].Score();
                freq++;
                scoreSum += sub;
                scoreMax = Math.Max(scoreMax, sub);
                ScoreAll((root << 1) + 1);
                ScoreAll((root << 1) + 2);
            }
        }

        public override int Freq
        {
            get { return freq; }
        }

    }
}