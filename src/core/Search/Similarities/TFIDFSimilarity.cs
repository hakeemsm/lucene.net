using System;
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Search.Similarities
{
    public abstract class TFIDFSimilarity : Similarity
    {
        private static readonly float[] NORM_TABLE = new float[256];

        static TFIDFSimilarity()
        {
            for (int i = 0; i < 256; i++)
            {
                NORM_TABLE[i] = SmallFloat.Byte315ToFloat((sbyte) i);
            }
        }

        public abstract override float Coord(int overlap, int maxOverlap);

        public abstract override float QueryNorm(float sumOfSquaredWeights);

        public float Tf(int freq)
        {
            return Tf((float) freq);
        }

        public abstract float Tf(float freq);

        public virtual Explanation IdfExplain(CollectionStatistics collectionStats, TermStatistics termStats)
        {
            var df = termStats.DocFreq;
            var max = collectionStats.MaxDoc;
            float idf = Idf(df, max);
            return new Explanation(idf, "idf(docFreq=" + df + ", maxDocs=" + max + ")");
        }

        public virtual Explanation IdfExplain(CollectionStatistics collectionStats, TermStatistics[] termStats)
        {
            var max = collectionStats.MaxDoc;
            float idf = 0.0f;
            var exp = new Explanation {Description = "idf(), sum of:"};
            foreach (var stat in termStats)
            {
                var df = stat.DocFreq;
                float termIdf = Idf(df, max);
                exp.AddDetail(new Explanation(termIdf, "idf(docFreq=" + df + ", maxDocs=" + max + ")"));
                idf += termIdf;
            }
            exp.Value = idf;
            return exp;
        }

        public abstract float Idf(long docFreq, long numDocs);

        public abstract float LengthNorm(FieldInvertState state);

        public override sealed long ComputeNorm(FieldInvertState state)
        {
            float normValue = LengthNorm(state);
            return EncodeNormValue(normValue);
        }

		public abstract float DecodeNormValue(long norm);

		public abstract long EncodeNormValue(float f);

        public abstract float SloppyFreq(int distance);

        public abstract float ScorePayload(int doc, int start, int end, BytesRef payload);

        public override sealed SimWeight ComputeWeight(float queryBoost, CollectionStatistics collectionStats,
                                                       params TermStatistics[] termStats)
        {
            Explanation idf = termStats.Length == 1
                                  ? IdfExplain(collectionStats, termStats[0])
                                  : IdfExplain(collectionStats, termStats);
            return new IDFStats(collectionStats.Field, idf, queryBoost);
        }

        public override sealed SimScorer GetSimScorer(SimWeight stats, AtomicReaderContext context)
        {
			IDFStats idfstats = (IDFStats)stats;
			return new TFIDFSimScorer(this, idfstats, context.AtomicReader.GetNormValues(idfstats.field));
        }


		private sealed class TFIDFSimScorer : SimScorer
		{
			private readonly TFIDFSimilarity.IDFStats stats;

			private readonly float weightValue;

			private readonly NumericDocValues norms;

			/// <exception cref="System.IO.IOException"></exception>
			internal TFIDFSimScorer(TFIDFSimilarity _enclosing, TFIDFSimilarity.IDFStats stats
				, NumericDocValues norms)
			{
				this._enclosing = _enclosing;
				this.stats = stats;
				this.weightValue = stats.value;
				this.norms = norms;
			}

			public override float Score(int doc, float freq)
			{
				float raw = this._enclosing.Tf(freq) * this.weightValue;
				// compute tf(f)*weight
				return this.norms == null ? raw : raw * this._enclosing.DecodeNormValue(this.norms
					.Get(doc));
			}

			// normalize for field
			public override float ComputeSlopFactor(int distance)
			{
				return this._enclosing.SloppyFreq(distance);
			}

			public override float ComputePayloadFactor(int doc, int start, int end, BytesRef 
				payload)
			{
				return this._enclosing.ScorePayload(doc, start, end, payload);
			}

			public override Explanation Explain(int doc, Explanation freq)
			{
				return this._enclosing.ExplainScore(doc, freq, this.stats, this.norms);
			}

			private readonly TFIDFSimilarity _enclosing;
		}

		/// <summary>Collection statistics for the TF-IDF model.</summary>
		/// <remarks>
		/// Collection statistics for the TF-IDF model. The only statistic of interest
		/// to this model is idf.
		/// </remarks>
		private class IDFStats : SimWeight
		{
		    internal readonly string field;

			/// <summary>The idf and its explanation</summary>
			internal readonly Explanation idf;

		    internal float queryNorm;

			private float queryWeight;

		    internal readonly float queryBoost;

		    internal float value;

			public IDFStats(string field, Explanation idf, float queryBoost)
			{
				// TODO: Validate?
				this.field = field;
				this.idf = idf;
				this.queryBoost = queryBoost;
				this.queryWeight = idf.Value * queryBoost;
			}

			// compute query weight
			public override float ValueForNormalization
			{
			    get
			    {
			        // TODO: (sorta LUCENE-1907) make non-static class and expose this squaring via a nice method to subclasses?
			        return queryWeight*queryWeight;
			    }
			}

			// sum of squared weights
			public override void Normalize(float queryNorm, float topLevelBoost)
			{
				this.queryNorm = queryNorm * topLevelBoost;
				queryWeight *= this.queryNorm;
				// normalize query weight
				value = queryWeight * idf.Value;
			}
			// idf for document
		}
        private Explanation ExplainScore(int doc, Explanation freq, IDFStats stats, NumericDocValues norms)
        {
            var result = new Explanation {Description = "score(doc=" + doc + ",freq=" + freq + "), product of:"};

            // explain query weight
            var queryExpl = new Explanation {Description = "queryWeight, product of:"};

            var boostExpl = new Explanation(stats.queryBoost, "boost");
            if (stats.queryBoost != 1.0f)
                queryExpl.AddDetail(boostExpl);
            queryExpl.AddDetail(stats.idf);

            var queryNormExpl = new Explanation(stats.queryNorm, "queryNorm");
            queryExpl.AddDetail(queryNormExpl);

            queryExpl.Value = boostExpl.Value*stats.idf.Value*queryNormExpl.Value;

            result.AddDetail(queryExpl);

            // explain field weight
            var fieldExpl = new Explanation {Description = "fieldWeight in " + doc + ", product of:"};

            var tfExplanation = new Explanation
                {
                    Value = Tf(freq.Value),
                    Description = "tf(freq=" + freq.Value + "), with freq of:"
                };
            tfExplanation.AddDetail(freq);
            fieldExpl.AddDetail(tfExplanation);
            fieldExpl.AddDetail(stats.idf);

            var fieldNormExpl = new Explanation();
            float fieldNorm = norms != null ? DecodeNormValue((sbyte) norms.Get(doc)) : 1.0f;
            fieldNormExpl.Value = fieldNorm;
            fieldNormExpl.Description = "fieldNorm(doc=" + doc + ")";
            fieldExpl.AddDetail(fieldNormExpl);

            fieldExpl.Value = tfExplanation.Value*stats.idf.Value*fieldNormExpl.Value;

            result.AddDetail(fieldExpl);

            // combine them
            result.Value = queryExpl.Value*fieldExpl.Value;

            return queryExpl.Value == 1.0f ? fieldExpl : result;
        }

        // TODO: we can specialize these for omitNorms up front, but we should test that it doesn't confuse stupid hotspot.

    }
}