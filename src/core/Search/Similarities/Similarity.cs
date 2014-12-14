using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Search.Similarities
{
    public abstract class Similarity
    {
        public virtual float Coord(int overlap, int maxOverlap)
        {
            return 1f;
        }

        public virtual float QueryNorm(float valueForNormalization)
        {
            return 1f;
        }

        public abstract long ComputeNorm(FieldInvertState state);

        public abstract SimWeight ComputeWeight(float queryBoost, CollectionStatistics collectionStats,
                                                params TermStatistics[] termStats);

		/// <summary>
		/// Creates a new
		/// <see cref="GetSimScorer">SimScorer</see>
		/// to score matching documents from a segment of the inverted index.
		/// </summary>
		/// <param name="weight">
		/// collection information from
		/// <see cref="ComputeWeight(float, Org.Apache.Lucene.Search.CollectionStatistics, Org.Apache.Lucene.Search.TermStatistics[])
		/// 	">ComputeWeight(float, Org.Apache.Lucene.Search.CollectionStatistics, Org.Apache.Lucene.Search.TermStatistics[])
		/// 	</see>
		/// </param>
		/// <param name="context">segment of the inverted index to be scored.</param>
		/// <returns>SloppySimScorer for scoring documents across <code>context</code></returns>
		/// <exception cref="System.IO.IOException">if there is a low-level I/O error</exception>
		public abstract SimScorer GetSimScorer(SimWeight weight, AtomicReaderContext context);

		

        public abstract class SimWeight
        {
            public abstract float ValueForNormalization { get; }

            public abstract void Normalize(float queryNorm, float topLevelBoost);
        }

        public abstract class SloppySimScorer
        {
            public abstract float Score(int doc, float freq);

            public abstract float ComputeSlopFactor(int distance);

            public abstract float ComputePayloadFactor(int doc, int start, int end, BytesRef payload);

            public virtual Explanation Explain(int doc, Explanation freq)
            {
                var result = new Explanation(Score(doc, freq.Value),
                                             "score(doc=" + doc + ",freq=" + freq.Value + "), with freq of:");
                result.AddDetail(freq);
                return result;
            }
        }
    }

    //.NET Port. Moved out to avoid conflict with property
    public abstract class SimScorer
    {
        public abstract float Score(int doc, float freq);

        public abstract float ComputeSlopFactor(int distance);
        public abstract float ComputePayloadFactor(int doc, int start, int end, BytesRef
            payload);
        public virtual Explanation Explain(int doc, Explanation freq)
        {
            var result = new Explanation(Score(doc, (int)freq.Value),
                                         "score(doc=" + doc + ",freq=" + freq.Value + "), with freq of:");
            result.AddDetail(freq);
            return result;
        }
    }
}