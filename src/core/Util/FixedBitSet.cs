using Lucene.Net.Search;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util
{
    public sealed class FixedBitSet : DocIdSet, IBits
    {
        public sealed class FixedBitSetIterator : DocIdSetIterator
        {
            internal readonly int numBits;

            internal readonly int numWords;

            internal readonly long[] bits;

            internal int doc = -1;

            /// <summary>
            /// Creates an iterator over the given
            /// <see cref="FixedBitSet">FixedBitSet</see>
            /// .
            /// </summary>
            public FixedBitSetIterator(FixedBitSet bits)
                : this(bits.bits, bits.numBits, bits
                    .numWords)
            {
            }

            /// <summary>Creates an iterator over the given array of bits.</summary>
            /// <remarks>Creates an iterator over the given array of bits.</remarks>
            public FixedBitSetIterator(long[] bits, int numBits, int wordLength)
            {
                this.bits = bits;
                this.numBits = numBits;
                this.numWords = wordLength;
            }

            public override int NextDoc()
            {
                if (doc == NO_MORE_DOCS || ++doc >= numBits)
                {
                    return doc = NO_MORE_DOCS;
                }
                int i = doc >> 6;
                int subIndex = doc & unchecked((int)(0x3f));
                // index within the word
                long word = bits[i] >> subIndex;
                // skip all the bits to the right of index
                if (word != 0)
                {
                    return doc = doc + word.NumberOfTrailingZeros();
                }
                while (++i < numWords)
                {
                    word = bits[i];
                    if (word != 0)
                    {
                        return doc = (i << 6) + word.NumberOfTrailingZeros();
                    }
                }
                return doc = NO_MORE_DOCS;
            }

            public override int DocID
            {
                get { return doc; }
            }

            public override long Cost
            {
                get { return numBits; }
            }

            public override int Advance(int target)
            {
                if (doc == NO_MORE_DOCS || target >= numBits)
                {
                    return doc = NO_MORE_DOCS;
                }
                int i = target >> 6;
                int subIndex = target & unchecked((int)(0x3f));
                // index within the word
                long word = bits[i] >> subIndex;
                // skip all the bits to the right of index
                if (word != 0)
                {
                    return doc = target + word.NumberOfTrailingZeros();
                }
                while (++i < numWords)
                {
                    word = bits[i];
                    if (word != 0)
                    {
                        return doc = (i << 6) + word.NumberOfTrailingZeros();
                    }
                }
                return doc = NO_MORE_DOCS;
            }
        }
        public static FixedBitSet EnsureCapacity(FixedBitSet bits, int numBits)
        {
            if (numBits < bits.Length)
            {
                return bits;
            }
            else
            {
                int numWords = Bits2Words(numBits);
                long[] arr = bits.GetBits();
                if (numWords >= arr.Length)
                {
                    arr = ArrayUtil.Grow(arr, numWords + 1);
                }
                return new FixedBitSet(arr, arr.Length << 6);
            }
        }
        private readonly long[] bits;
        private readonly int numBits;
        internal readonly int numWords;

        public static int Bits2Words(int numBits)
        {
            int numLong = Number.URShift(numBits, 6);
            if ((numBits & 63) != 0)
            {
                numLong++;
            }
            return numLong;
        }

        public static long IntersectionCount(FixedBitSet a, FixedBitSet b)
        {
            return BitUtil.Pop_intersect(a.bits, b.bits, 0, Math.Min(a.numWords, b.numWords));
        }
        public static long UnionCount(FixedBitSet a, FixedBitSet b)
        {
            long tot = BitUtil.Pop_union(a.bits, b.bits, 0, Math.Min(a.numWords, b.numWords));
            if (a.numWords < b.numWords)
            {
                tot += BitUtil.Pop_array(b.bits, a.numWords, b.numWords - a.numWords);
            }
            else
            {
                if (a.numWords > b.numWords)
                {
                    tot += BitUtil.Pop_array(a.bits, b.numWords, a.numWords - b.numWords);
                }
            }
            return tot;
        }
        public static long AndNotCount(FixedBitSet a, FixedBitSet b)
        {
            long tot = BitUtil.Pop_andnot(a.bits, b.bits, 0, Math.Min(a.numWords, b.numWords)
                );
            if (a.numWords > b.numWords)
            {
                tot += BitUtil.Pop_array(a.bits, b.numWords, a.numWords - b.numWords);
            }
            return tot;
        }
        public FixedBitSet(int numBits)
        {
            this.numBits = numBits;
            bits = new long[Bits2Words(numBits)];
            numWords = bits.Length;
        }

        public FixedBitSet(long[] storedBits, int numBits)
        {
            this.numWords = Bits2Words(numBits);
            if (numWords > storedBits.Length)
            {
                throw new ArgumentException("The given long array is too small to hold " + numBits + " bits");
            }
            this.numBits = numBits;
            this.bits = storedBits;
        }

        public override DocIdSetIterator Iterator()
        {
            return new FixedBitSetIterator(bits, numBits, numWords);
        }

        public IBits Bits
        {
            get
            {
                return this;
            }
        }

        public int Length
        {
            get
            {
                return numBits;
            }
        }

        public override bool IsCacheable
        {
            get
            {
                return true;
            }
        }

        public long[] GetBits()
        {
            return bits;
        }

        public int Cardinality()
        {
            return (int)BitUtil.Pop_array(bits, 0, bits.Length);
        }

        public bool this[int index]
        {
            get
            {
                //assert index >= 0 && index < numBits: "index=" + index;
                int i = index >> 6;               // div 64
                // signed shift will keep a negative index and force an
                // array-index-out-of-bounds-exception, removing the need for an explicit check.
                int bit = index & 0x3f;           // mod 64
                long bitmask = 1L << bit;
                return (bits[i] & bitmask) != 0;
            }
        }

        public void Set(int index)
        {
            //assert index >= 0 && index < numBits: "index=" + index + " numBits=" + numBits;
            int wordNum = index >> 6;      // div 64
            int bit = index & 0x3f;     // mod 64
            long bitmask = 1L << bit;
            bits[wordNum] |= bitmask;
        }

        public bool GetAndSet(int index)
        {
            //assert index >= 0 && index < numBits;
            int wordNum = index >> 6;      // div 64
            int bit = index & 0x3f;     // mod 64
            long bitmask = 1L << bit;
            bool val = (bits[wordNum] & bitmask) != 0;
            bits[wordNum] |= bitmask;
            return val;
        }

        public void Clear(int index)
        {
            //assert index >= 0 && index < numBits;
            int wordNum = index >> 6;
            int bit = index & 0x03f;
            long bitmask = 1L << bit;
            bits[wordNum] &= ~bitmask;
        }

        public bool GetAndClear(int index)
        {
            //assert index >= 0 && index < numBits;
            int wordNum = index >> 6;      // div 64
            int bit = index & 0x3f;     // mod 64
            long bitmask = 1L << bit;
            bool val = (bits[wordNum] & bitmask) != 0;
            bits[wordNum] &= ~bitmask;
            return val;
        }

        public int NextSetBit(int index)
        {
            //assert index >= 0 && index < numBits;
            int i = index >> 6;
            int subIndex = index & 0x3f;      // index within the word
            long word = bits[i] >> subIndex;  // skip all the bits to the right of index

            if (word != 0)
            {
                return index + word.NumberOfTrailingZeros();
            }

            while (++i < numWords)
            {
                word = bits[i];
                if (word != 0)
                {
                    return (i << 6) + word.NumberOfTrailingZeros();
                }
            }

            return -1;
        }

        public int PrevBitSet(int index)
        {
            //assert index >= 0 && index < numBits: "index=" + index + " numBits=" + numBits;
            int i = index >> 6;
            int subIndex = index & 0x3f;  // index within the word
            long word = (bits[i] << (63 - subIndex));  // skip all the bits to the left of index

            if (word != 0)
            {
                return (i << 6) + subIndex - word.NumberOfLeadingZeros(); // See LUCENE-3197
            }

            while (--i >= 0)
            {
                word = bits[i];
                if (word != 0)
                {
                    return (i << 6) + 63 - word.NumberOfLeadingZeros();
                }
            }

            return -1;
        }

        public void Or(DocIdSetIterator iter)
        {
            if (iter is OpenBitSetIterator && iter.DocID == -1)
            {
                OpenBitSetIterator obs = (OpenBitSetIterator)iter;
                Or(obs.arr, obs.words);
                // advance after last doc that would be accepted if standard
                // iteration is used (to exhaust it):
                obs.Advance(numBits);
            }
            else
            {
                if (iter is FixedBitSet.FixedBitSetIterator && iter.DocID == -1)
                {
                    FixedBitSet.FixedBitSetIterator fbs = (FixedBitSetIterator)iter;
                    Or(fbs.bits, fbs.numWords);
                    // advance after last doc that would be accepted if standard
                    // iteration is used (to exhaust it):
                    fbs.Advance(numBits);
                }
                else
                {
                    int doc;
                    while ((doc = iter.NextDoc()) < numBits)
                    {
                        Set(doc);
                    }
                }
            }
        }

        public void Or(FixedBitSet other)
        {
            Or(other.bits, other.numWords);
        }

        private void Or(long[] otherArr, int otherNumWords)
        {
            long[] thisArr = this.bits;
            int pos = Math.Min(numWords, otherNumWords);
            while (--pos >= 0)
            {
                thisArr[pos] |= otherArr[pos];
            }
        }

        public void Xor(FixedBitSet other)
        {
            //HM:revisit 
            //assert other.numWords <= numWords : "numWords=" + numWords + ", other.numWords=" + other.numWords;
            long[] thisBits = this.bits;
            long[] otherBits = other.bits;
            int pos = Math.Min(numWords, other.numWords);
            while (--pos >= 0)
            {
                thisBits[pos] ^= otherBits[pos];
            }
        }
        public void Xor(DocIdSetIterator iter)
        {
            int doc;
            while ((doc = iter.NextDoc()) < numBits)
            {
                Flip(doc, doc + 1);
            }
        }
        public void And(DocIdSetIterator iter)
        {
            if (iter is OpenBitSetIterator && iter.DocID == -1)
            {
                OpenBitSetIterator obs = (OpenBitSetIterator)iter;
                And(obs.arr, obs.words);
                // advance after last doc that would be accepted if standard
                // iteration is used (to exhaust it):
                obs.Advance(numBits);
            }
            else
            {
                if (iter is FixedBitSetIterator && iter.DocID == -1)
                {
                    FixedBitSet.FixedBitSetIterator fbs = (FixedBitSetIterator)iter;
                    And(fbs.bits, fbs.numWords);
                    // advance after last doc that would be accepted if standard
                    // iteration is used (to exhaust it):
                    fbs.Advance(numBits);
                }
                else
                {
                    if (numBits == 0)
                    {
                        return;
                    }
                    int disiDoc;
                    int bitSetDoc = NextSetBit(0);
                    while (bitSetDoc != -1 && (disiDoc = iter.Advance(bitSetDoc)) < numBits)
                    {
                        Clear(bitSetDoc, disiDoc);
                        disiDoc++;
                        bitSetDoc = (disiDoc < numBits) ? NextSetBit(disiDoc) : -1;
                    }
                    if (bitSetDoc != -1)
                    {
                        Clear(bitSetDoc, numBits);
                    }
                }
            }
        }

        public bool Intersects(FixedBitSet other)
        {
            int pos = Math.Min(numWords, other.numWords);
            while (--pos >= 0)
            {
                if ((bits[pos] & other.bits[pos]) != 0)
                {
                    return true;
                }
            }
            return false;
        }
        public void And(FixedBitSet other)
        {
            And(other.bits, other.numWords);
        }

        private void And(long[] otherArr, int otherNumWords)
        {
            long[] thisArr = this.bits;
            int pos = Math.Min(this.numWords, otherNumWords);
            while (--pos >= 0)
            {
                thisArr[pos] &= otherArr[pos];
            }
            if (this.numWords > otherNumWords)
            {
                Arrays.Fill(thisArr, otherNumWords, this.numWords, 0L);
            }
        }

        public void AndNot(DocIdSetIterator iter)
        {
            if (iter is OpenBitSetIterator && iter.DocID == -1)
            {
                OpenBitSetIterator obs = (OpenBitSetIterator)iter;
                AndNot(obs.arr, obs.words);
                // advance after last doc that would be accepted if standard
                // iteration is used (to exhaust it):
                obs.Advance(numBits);
            }
            else
            {
                if (iter is FixedBitSet.FixedBitSetIterator && iter.DocID == -1)
                {
                    FixedBitSetIterator fbs = (FixedBitSetIterator)iter;
                    AndNot(fbs.bits, fbs.numWords);
                    // advance after last doc that would be accepted if standard
                    // iteration is used (to exhaust it):
                    fbs.Advance(numBits);
                }
                else
                {
                    int doc;
                    while ((doc = iter.NextDoc()) < numBits)
                    {
                        Clear(doc);
                    }
                }
            }
        }

        public void AndNot(FixedBitSet other)
        {
            AndNot(other.bits, other.bits.Length);
        }

        private void AndNot(long[] otherArr, int otherNumWords)
        {
            long[] thisArr = this.bits;
            int pos = Math.Min(this.numWords, otherNumWords);
            while (--pos >= 0)
            {
                thisArr[pos] &= ~otherArr[pos];
            }
        }

        public void Flip(int startIndex, int endIndex)
        {
            //assert startIndex >= 0 && startIndex < numBits;
            //assert endIndex >= 0 && endIndex <= numBits;
            if (endIndex <= startIndex)
            {
                return;
            }

            int startWord = startIndex >> 6;
            int endWord = (endIndex - 1) >> 6;

            /*** Grrr, java shifting wraps around so -1L>>>64 == -1
             * for that reason, make sure not to use endmask if the bits to flip will
             * be zero in the last word (redefine endWord to be the last changed...)
            long startmask = -1L << (startIndex & 0x3f);     // example: 11111...111000
            long endmask = -1L >>> (64-(endIndex & 0x3f));   // example: 00111...111111
            ***/

            long startmask = -1L << startIndex;
            long endmask = Number.URShift(-1L, -endIndex);  // 64-(endIndex&0x3f) is the same as -endIndex due to wrap

            if (startWord == endWord)
            {
                bits[startWord] ^= (startmask & endmask);
                return;
            }

            bits[startWord] ^= startmask;

            for (int i = startWord + 1; i < endWord; i++)
            {
                bits[i] = ~bits[i];
            }

            bits[endWord] ^= endmask;
        }

        public void Set(int startIndex, int endIndex)
        {
            //assert startIndex >= 0 && startIndex < numBits;
            //assert endIndex >= 0 && endIndex <= numBits;
            if (endIndex <= startIndex)
            {
                return;
            }

            int startWord = startIndex >> 6;
            int endWord = (endIndex - 1) >> 6;

            long startmask = -1L << startIndex;
            long endmask = Number.URShift(-1L, -endIndex);  // 64-(endIndex&0x3f) is the same as -endIndex due to wrap

            if (startWord == endWord)
            {
                bits[startWord] |= (startmask & endmask);
                return;
            }

            bits[startWord] |= startmask;
            Arrays.Fill(bits, startWord + 1, endWord, -1L);
            bits[endWord] |= endmask;
        }

        public void Clear(int startIndex, int endIndex)
        {
            //assert startIndex >= 0 && startIndex < numBits;
            //assert endIndex >= 0 && endIndex <= numBits;
            if (endIndex <= startIndex)
            {
                return;
            }

            int startWord = startIndex >> 6;
            int endWord = (endIndex - 1) >> 6;

            long startmask = -1L << startIndex;
            long endmask = Number.URShift(-1L, -endIndex);  // 64-(endIndex&0x3f) is the same as -endIndex due to wrap

            // invert masks since we are clearing
            startmask = ~startmask;
            endmask = ~endmask;

            if (startWord == endWord)
            {
                bits[startWord] &= (startmask | endmask);
                return;
            }

            bits[startWord] &= startmask;
            Arrays.Fill(bits, startWord + 1, endWord, 0L);
            bits[endWord] &= endmask;
        }

        public /* override? */ FixedBitSet Clone()
        {
            long[] bits = new long[this.bits.Length];
            System.Array.Copy(this.bits, 0, bits, 0, bits.Length);
            return new FixedBitSet(bits, numBits);
        }

        public override bool Equals(object o)
        {
            if (this == o)
            {
                return true;
            }
            if (!(o is FixedBitSet))
            {
                return false;
            }
            FixedBitSet other = (FixedBitSet)o;
            if (numBits != other.Length)
            {
                return false;
            }
            return Arrays.Equals(bits, other.bits);
        }

        public override int GetHashCode()
        {
            long h = 0;
			for (int i = numWords; --i >= 0; )
            {
                h ^= bits[i];
                h = (h << 1) | Number.URShift(h, 63); // rotate left
            }
            // fold leftmost bits into right and add a constant to prevent
            // empty sets from returning 0, which is too common.
            return (int)(((h >> 32) ^ h) + 0x98761234);
        }
    }
}
