using System;
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Search.Similarities
{
    public class BM25Similarity : Similarity
    {
        private static readonly float[] NORM_TABLE = new float[256];
        private readonly float b;
        private readonly float k1;
        protected bool discountOverlaps = true;

        static BM25Similarity()
        {
            for (int i = 0; i < 256; i++)
            {
                float f = SmallFloat.Byte315ToFloat((sbyte)i);
                NORM_TABLE[i] = 1.0f / (f * f);
            }
        }

        public BM25Similarity(float k1, float b)
        {
            this.k1 = k1;
            this.b = b;
        }

        public BM25Similarity()
        {
            k1 = 1.2f;
            b = 0.75f;
        }

        public virtual bool DiscountOverlaps
        {
            get { return discountOverlaps; }
            set { discountOverlaps = value; }
        }

        public float K1
        {
            get { return k1; }
        }

        public float B
        {
            get { return b; }
        }

        protected virtual float Idf(long docFreq, long numDocs)
        {
            return (float)Math.Log(1 + (numDocs - docFreq + 0.5D) / (docFreq + 0.5D));
        }

        protected virtual float SloppyFreq(int distance)
        {
            return 1.0f / (distance + 1);
        }

        protected virtual float ScorePayload(int doc, int start, int end, BytesRef payload)
        {
            return 1;
        }

        protected virtual float AvgFieldLength(CollectionStatistics collectionStats)
        {
            var sumTotalTermFreq = collectionStats.SumTotalTermFreq;
            if (sumTotalTermFreq <= 0)
                return 1f;
            else
                return (float)(sumTotalTermFreq / (double)collectionStats.MaxDoc);
        }

        protected virtual sbyte EncodeNormValue(float boost, int fieldLength)
        {
            return SmallFloat.FloatToByte315(boost / (float)Math.Sqrt(fieldLength));
        }

        protected virtual float DecodeNormValue(sbyte b)
        {
            return NORM_TABLE[b & 0xFF];
        }

        public override sealed long ComputeNorm(FieldInvertState state)
        {
            int numTerms = discountOverlaps ? state.Length - state.NumOverlap : state.Length;
            return EncodeNormValue(state.Boost, numTerms);
        }

        public virtual Explanation IdfExplain(CollectionStatistics collectionStats, TermStatistics termStats)
        {
            long df = termStats.DocFreq;
            long max = collectionStats.MaxDoc;
            float idf = Idf(df, max);
            return new Explanation(idf, "idf(docFreq=" + df + ", maxDocs=" + max + ")");
        }

        public virtual Explanation IdfExplain(CollectionStatistics collectionStats, TermStatistics[] termStats)
        {
            long max = collectionStats.MaxDoc;
            float idf = 0.0f;
            var exp = new Explanation();
            exp.Description = "idf(), sum of:";
            foreach (var stat in termStats)
            {
                long df = stat.DocFreq;
                float termIdf = Idf(df, max);
                exp.AddDetail(new Explanation(termIdf, "idf(docFreq=" + df + ", maxDocs=" + max + ")"));
                idf += termIdf;
            }
            exp.Value = idf;
            return exp;
        }

        public override sealed SimWeight ComputeWeight(float queryBoost, CollectionStatistics collectionStats,
                                                       params TermStatistics[] termStats)
        {
            Explanation idf = termStats.Length == 1
                                  ? IdfExplain(collectionStats, termStats[0])
                                  : IdfExplain(collectionStats, termStats);

            float avgdl = AvgFieldLength(collectionStats);

            var cache = new float[256];
            for (int i = 0; i < cache.Length; i++)
            {
                cache[i] = k1 * ((1 - b) + b * DecodeNormValue((sbyte)i) / avgdl);
            }
            return new BM25Stats(collectionStats.Field, idf, queryBoost, avgdl, cache);
        }

        public override sealed SimScorer GetSimScorer(SimWeight stats, AtomicReaderContext context)
        {
            var bm25stats = (BM25Stats)stats;
			return new BM25Similarity.BM25DocScorer(this, bm25stats, context.AtomicReader.GetNormValues(bm25stats.Field));
        }


        private Explanation ExplainScore(int doc, Explanation freq, BM25Stats stats, NumericDocValues norms)
        {
            var result = new Explanation { Description = "score(doc=" + doc + ",freq=" + freq + "), product of:" };

            var boostExpl = new Explanation(stats.QueryBoost * stats.TopLevelBoost, "boost");
            if (boostExpl.Value != 1.0f)
                result.AddDetail(boostExpl);

            result.AddDetail(stats.Idf);

            var tfNormExpl = new Explanation { Description = "tfNorm, computed from:" };
            tfNormExpl.AddDetail(freq);
            tfNormExpl.AddDetail(new Explanation(k1, "parameter k1"));
            if (norms == null)
            {
                tfNormExpl.AddDetail(new Explanation(0, "parameter b (norms omitted for field)"));
                tfNormExpl.Value = (freq.Value * (k1 + 1)) / (freq.Value + k1);
            }
            else
            {
                float doclen = DecodeNormValue((sbyte)norms.Get(doc));
                tfNormExpl.AddDetail(new Explanation(b, "parameter b"));
                tfNormExpl.AddDetail(new Explanation(stats.Avgdl, "avgFieldLength"));
                tfNormExpl.AddDetail(new Explanation(doclen, "fieldLength"));
                tfNormExpl.Value = (freq.Value * (k1 + 1)) / (freq.Value + k1 * (1 - b + b * doclen / stats.Avgdl));
            }
            result.AddDetail(tfNormExpl);
            result.Value = boostExpl.Value * stats.Idf.Value * tfNormExpl.Value;
            return result;
        }

        public override string ToString()
        {
            return "BM25(k1=" + k1 + ",b=" + b + ")";
        }

		private class BM25DocScorer : SimScorer
		{
			private readonly BM25Similarity.BM25Stats stats;

			private readonly float weightValue;

			private readonly NumericDocValues norms;

			private readonly float[] cache;

			/// <exception cref="System.IO.IOException"></exception>
			internal BM25DocScorer(BM25Similarity _enclosing, BM25Stats stats, 
				NumericDocValues norms)
			{
				this._enclosing = _enclosing;
				// boost * idf * (k1 + 1)
				this.stats = stats;
				this.weightValue = stats.weight * (this._enclosing.k1 + 1);
				this.cache = stats.cache;
				this.norms = norms;
			}

			public override float Score(int doc, float freq)
			{
				// if there are no norms, we act as if b=0
				float norm = this.norms == null ? this._enclosing.k1 : this.cache[unchecked((byte
					)this.norms.Get(doc)) & unchecked((int)(0xFF))];
				return this.weightValue * freq / (freq + norm);
			}

			public override Explanation Explain(int doc, Explanation freq)
			{
				return this._enclosing.ExplainScore(doc, freq, this.stats, this.norms);
			}

			public override float ComputeSlopFactor(int distance)
			{
				return this._enclosing.SloppyFreq(distance);
			}

			public override float ComputePayloadFactor(int doc, int start, int end, BytesRef 
				payload)
			{
				return this._enclosing.ScorePayload(doc, start, end, payload);
			}

			private readonly BM25Similarity _enclosing;
		}
        private class BM25Stats : SimWeight
        {
            /** BM25's idf */
            private readonly float avgdl;
            internal readonly float[] cache;
            private readonly string field;
            private readonly Explanation idf;
            private readonly float queryBoost;
            private float topLevelBoost;
            internal float weight;

            public BM25Stats(String field, Explanation idf, float queryBoost, float avgdl, float[] cache)
            {
                this.field = field;
                this.idf = idf;
                this.queryBoost = queryBoost;
                this.avgdl = avgdl;
                this.cache = cache;
            }

            public Explanation Idf
            {
                get { return idf; }
            }

            /** The average document length. */

            public float Avgdl
            {
                get { return avgdl; }
            }

            /** query's inner boost */

            public float QueryBoost
            {
                get { return queryBoost; }
            }

            /** query's outer boost (only for explain) */

            public float TopLevelBoost
            {
                get { return topLevelBoost; }
            }

            /** weight (idf * boost) */

            public float Weight
            {
                get { return weight; }
            }

            /** field name, for pulling norms */

            public string Field
            {
                get { return field; }
            }

            /** precomputed norm[256] with k1 * ((1 - b) + b * dl / avgdl) */

            public float[] Cache
            {
                get { return cache; }
            }

            public override float ValueForNormalization
            {
                get
                {
                    // we return a TF-IDF like normalization to be nice, but we don't actually normalize ourselves.
                    float queryWeight = idf.Value * queryBoost;
                    return queryWeight * queryWeight;
                }
            }

            public override void Normalize(float queryNorm, float topLevelBoost)
            {
                // we don't normalize with queryNorm at all, we just capture the top-level boost
                this.topLevelBoost = topLevelBoost;
                weight = idf.Value * queryBoost * topLevelBoost;
            }
        }



		public virtual float GetK1()
		{
			return k1;
		}
		public virtual float GetB()
		{
			return b;
		}
    }
}