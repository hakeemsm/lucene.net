using System;
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Search.Similarities
{
    public class DefaultSimilarity : TFIDFSimilarity
    {
        protected bool discountOverlaps = true;
		private static readonly float[] NORM_TABLE = new float[256];

		static DefaultSimilarity()
		{
			for (int i = 0; i < 256; i++)
			{
				NORM_TABLE[i] = SmallFloat.Byte315ToFloat((sbyte)i);
			}
		}
        public virtual bool DiscountOverlaps
        {
            get { return discountOverlaps; }
            set { discountOverlaps = value; }
        }

        public override float Coord(int overlap, int maxOverlap)
        {
            return overlap/(float) maxOverlap;
        }

        public override float QueryNorm(float sumOfSquaredWeights)
        {
            return (float) (1.0/Math.Sqrt(sumOfSquaredWeights));
        }

		public sealed override long EncodeNormValue(float f)
		{
			return SmallFloat.FloatToByte315(f);
		}
		public sealed override float DecodeNormValue(long norm)
		{
			return NORM_TABLE[(int)(norm & unchecked((int)(0xFF)))];
		}
        public override float LengthNorm(FieldInvertState state)
        {
            int numTerms;
            if (discountOverlaps)
                numTerms = state.Length - state.NumOverlap;
            else
                numTerms = state.Length;
            return state.Boost*((float) (1.0/Math.Sqrt(numTerms)));
        }

        public override float Tf(float freq)
        {
            return (float) Math.Sqrt(freq);
        }

        public override float SloppyFreq(int distance)
        {
            return 1.0f/(distance + 1);
        }

        public override float ScorePayload(int doc, int start, int end, BytesRef payload)
        {
            return 1;
        }

        public override float Idf(long docFreq, long numDocs)
        {
            return (float) (Math.Log(numDocs/(double) (docFreq + 1)) + 1.0);
        }

        public override string ToString()
        {
            return "DefaultSimilarity";
        }
    }
}