using System;
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Search.Similarities
{
    public abstract class SimilarityBase : Similarity
    {
        private static readonly double LOG_2 = Math.Log(2);
        private static readonly float[] NORM_TABLE = new float[256];

        static SimilarityBase()
        {
            for (int i = 0; i < 256; i++)
            {
                float floatNorm = SmallFloat.Byte315ToFloat((sbyte) i);
                NORM_TABLE[i] = 1.0f/(floatNorm*floatNorm);
            }
        }

        public bool DiscountOverlaps { get; set; }

        public override sealed SimWeight ComputeWeight(float queryBoost, CollectionStatistics collectionStats,
                                                       TermStatistics[] termStats)
        {
            var stats = new BasicStats[termStats.Length];
            for (int i = 0; i < termStats.Length; i++)
            {
                stats[i] = NewStats(collectionStats.Field, queryBoost);
                FillBasicStats(stats[i], collectionStats, termStats[i]);
            }
            return stats.Length == 1 ? stats[0] : new MultiSimilarity.MultiStats(stats) as SimWeight;
        }

        protected virtual BasicStats NewStats(string field, float queryBoost)
        {
            return new BasicStats(field, queryBoost);
        }

        protected virtual void FillBasicStats(BasicStats stats, CollectionStatistics collectionStats,
                                              TermStatistics termStats)
        {
            // assert collectionStats.sumTotalTermFreq() == -1 || collectionStats.sumTotalTermFreq() >= termStats.totalTermFreq();
            var numberOfDocuments = collectionStats.MaxDoc;

            var docFreq = termStats.DocFreq;
            var totalTermFreq = termStats.TotalTermFreq;

            if (totalTermFreq == -1)
            {
                totalTermFreq = docFreq;
            }

            long numberOfFieldTokens = 0L;
            float avgFieldLength = 0f;

            var sumTotalTermFreq = collectionStats.SumTotalTermFreq;

            if (sumTotalTermFreq <= 0)
            {
                numberOfFieldTokens = docFreq;
                avgFieldLength = 1;
            }
            else
            {
                numberOfFieldTokens = sumTotalTermFreq;
                avgFieldLength = (float) numberOfFieldTokens/numberOfDocuments;
            }

            stats.NumberOfDocuments = numberOfDocuments;
            stats.NumberOfFieldTokens = numberOfFieldTokens;
            stats.AvgFieldLength = avgFieldLength;
            stats.DocFreq = docFreq;
            stats.TotalTermFreq = totalTermFreq;
        }

        protected abstract float Score(BasicStats stats, float freq, float docLen);

        protected virtual void Explain(Explanation expl, BasicStats stats, int doc, float freq, float docLen)
        {
        }

        protected virtual Explanation Explain(BasicStats stats, int doc, Explanation freq, float docLen)
        {
            var result = new Explanation();

            result.Value = Score(stats, freq.Value, docLen);
            result.Description = "score(" + GetType().Name +
                                 ", doc=" + doc + ", freq=" + freq.Value + "), computed from:";
            result.AddDetail(freq);

            Explain(result, stats, doc, freq.Value, docLen);

            return result;
        }

        public override SimScorer GetSimScorer(SimWeight stats, AtomicReaderContext context)
        {
            var multiStats = stats as MultiSimilarity.MultiStats;
            if (multiStats != null)
            {
                SimWeight[] subStats = multiStats.subStats;
                var subScorers = new SimScorer[subStats.Length];
                for (int i = 0; i < subScorers.Length; i++)
                {
                    var basicstats = (BasicStats) subStats[i];
                    subScorers[i] = new BasicSimScorer(this, basicstats, context.AtomicReader.GetNormValues(basicstats.Field));
                }
				return new MultiSimilarity.MultiSimScorer(subScorers);
            }
            else
            {
                var basicstats = (BasicStats) stats;
				return new BasicSimScorer(this, basicstats, context.AtomicReader.GetNormValues(basicstats.Field));
            }
        }


        public abstract override string ToString();

        public override long ComputeNorm(FieldInvertState state)
        {
            float numTerms;
            if (DiscountOverlaps)
                numTerms = state.Length - state.NumOverlap;
            else
				numTerms = state.Length;
            return EncodeNormValue(state.Boost, numTerms);
        }

        protected virtual float DecodeNormValue(sbyte norm)
        {
            return NORM_TABLE[norm & 0xFF];
        }

        protected virtual sbyte EncodeNormValue(float boost, float length)
        {
            return SmallFloat.FloatToByte315((boost/(float) Math.Sqrt(length)));
        }

        public static double Log2(double x)
        {
            return Math.Log(x)/LOG_2;
        }

		private class BasicSimScorer : SimScorer
        {
            private readonly BasicStats stats;

			private readonly NumericDocValues norms;

			/// <exception cref="System.IO.IOException"></exception>
			internal BasicSimScorer(SimilarityBase _enclosing, BasicStats stats, NumericDocValues
				 norms)
			{
				this._enclosing = _enclosing;
				// --------------------------------- Classes ---------------------------------
				this.stats = stats;
				this.norms = norms;
			}

			public override float Score(int doc, float freq)
			{
				// We have to supply something in case norms are omitted
				return this._enclosing.Score(this.stats, freq, this.norms == null ? 1F : this._enclosing
					.DecodeNormValue(unchecked((sbyte)this.norms.Get(doc))));
			}

			public override Explanation Explain(int doc, Explanation freq)
			{
				return this._enclosing.Explain(this.stats, doc, freq, this.norms == null ? 1F : this
					._enclosing.DecodeNormValue(unchecked((sbyte)this.norms.Get(doc))));
			}

			public override float ComputeSlopFactor(int distance)
			{
				return 1.0f / (distance + 1);
			}

			public override float ComputePayloadFactor(int doc, int start, int end, BytesRef 
				payload)
			{
				return 1f;
			}

			private readonly SimilarityBase _enclosing;
        }

    }
}