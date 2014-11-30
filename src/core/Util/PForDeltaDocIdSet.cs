using System;
using Lucene.Net.Search;
using Lucene.Net.Support;
using Lucene.Net.Util.Packed;

namespace Lucene.Net.Util
{
    /// <summary>
    /// <see cref="Lucene.Net.Search.DocIdSet">Lucene.Net.Search.DocIdSet</see>
    /// implementation based on pfor-delta encoding.
    /// <p>This implementation is inspired from LinkedIn's Kamikaze
    /// (http://data.linkedin.com/opensource/kamikaze) and Daniel Lemire's JavaFastPFOR
    /// (https://github.com/lemire/JavaFastPFOR).</p>
    /// <p>On the contrary to the original PFOR paper, exceptions are encoded with
    /// FOR instead of Simple16.</p>
    /// </summary>
    public sealed class PForDeltaDocIdSet : DocIdSet
    {
        internal const int BLOCK_SIZE = 128;

        internal const int MAX_EXCEPTIONS = 24;

        internal static readonly PackedInts.IDecoder[] DECODERS = new PackedInts.IDecoder[32];

        internal static readonly int[] ITERATIONS = new int[32];

        internal static readonly int[] BYTE_BLOCK_COUNTS = new int[32];

        internal static readonly int MAX_BYTE_BLOCK_COUNT;

        internal static readonly MonotonicAppendingLongBuffer SINGLE_ZERO_BUFFER = new MonotonicAppendingLongBuffer
            (0, 64, PackedInts.COMPACT);

        internal static readonly Lucene.Net.Util.PForDeltaDocIdSet EMPTY = new PForDeltaDocIdSet
            (null, 0, int.MaxValue, SINGLE_ZERO_BUFFER, SINGLE_ZERO_BUFFER);

        internal const int LAST_BLOCK = 1 << 5;

        internal const int HAS_EXCEPTIONS = 1 << 6;

        internal const int UNARY = 1 << 7;

        static PForDeltaDocIdSet()
        {
            // no more than 24 exceptions per block
            // flag to indicate the last block
            SINGLE_ZERO_BUFFER.Add(0);
            SINGLE_ZERO_BUFFER.Freeze();
            int maxByteBLockCount = 0;
            for (int i = 1; i < ITERATIONS.Length; ++i)
            {
                DECODERS[i] = PackedInts.GetDecoder(PackedInts.Format.PACKED, PackedInts.VERSION_CURRENT
                    , i);
                //HM:revisit 
                //assert BLOCK_SIZE % DECODERS[i].byteValueCount() == 0;
                ITERATIONS[i] = BLOCK_SIZE / DECODERS[i].ByteValueCount;
                BYTE_BLOCK_COUNTS[i] = ITERATIONS[i] * DECODERS[i].ByteBlockCount;
                maxByteBLockCount = Math.Max(maxByteBLockCount, DECODERS[i].ByteBlockCount);
            }
            MAX_BYTE_BLOCK_COUNT = maxByteBLockCount;
        }

        /// <summary>
        /// A builder for
        /// <see cref="PForDeltaDocIdSet">PForDeltaDocIdSet</see>
        /// .
        /// </summary>
        public class Builder
        {
            internal readonly GrowableByteArrayDataOutput data;

            internal readonly int[] buffer = new int[BLOCK_SIZE];

            internal readonly int[] exceptionIndices = new int[BLOCK_SIZE];

            internal readonly int[] exceptions = new int[BLOCK_SIZE];

            internal int bufferSize;

            internal int previousDoc;

            internal int cardinality;

            internal int indexInterval;

            internal int numBlocks;

            internal readonly int[] freqs = new int[32];

            internal int bitsPerValue;

            internal int numExceptions;

            internal int bitsPerException;

            /// <summary>Sole constructor.</summary>
            /// <remarks>Sole constructor.</remarks>
            public Builder()
            {
                // temporary variables used when compressing blocks
                data = new GrowableByteArrayDataOutput(128);
                bufferSize = 0;
                previousDoc = -1;
                indexInterval = 2;
                cardinality = 0;
                numBlocks = 0;
            }

            /// <summary>Set the index interval.</summary>
            /// <remarks>
            /// Set the index interval. Every <code>indexInterval</code>-th block will
            /// be stored in the index. Set to
            /// <see cref="int.MaxValue">int.MaxValue</see>
            /// to disable indexing.
            /// </remarks>
            public virtual PForDeltaDocIdSet.Builder SetIndexInterval(int indexInterval)
            {
                if (indexInterval < 1)
                {
                    throw new ArgumentException("indexInterval must be >= 1");
                }
                this.indexInterval = indexInterval;
                return this;
            }

            /// <summary>Add a document to this builder.</summary>
            /// <remarks>Add a document to this builder. Documents must be added in order.</remarks>
            public virtual PForDeltaDocIdSet.Builder Add(int doc)
            {
                if (doc <= previousDoc)
                {
                    throw new ArgumentException("Doc IDs must be provided in order, but previousDoc="
                         + previousDoc + " and doc=" + doc);
                }
                buffer[bufferSize++] = doc - previousDoc - 1;
                if (bufferSize == BLOCK_SIZE)
                {
                    EncodeBlock();
                    bufferSize = 0;
                }
                previousDoc = doc;
                ++cardinality;
                return this;
            }

            /// <summary>
            /// Convenience method to add the content of a
            /// <see cref="Lucene.Net.Search.DocIdSetIterator">Lucene.Net.Search.DocIdSetIterator
            /// 	</see>
            /// to this builder.
            /// </summary>
            /// <exception cref="System.IO.IOException"></exception>
            public virtual PForDeltaDocIdSet.Builder Add(DocIdSetIterator it)
            {
                for (int doc = it.NextDoc(); doc != DocIdSetIterator.NO_MORE_DOCS; doc = it.NextDoc
                    ())
                {
                    Add(doc);
                }
                return this;
            }

            internal virtual void ComputeFreqs()
            {
                Arrays.Fill(freqs, 0);
                for (int i = 0; i < bufferSize; ++i)
                {
                    ++freqs[32 - buffer[i].NumberOfLeadingZeros()];
                }
            }

            internal virtual int PforBlockSize(int bitsPerValue, int numExceptions, int bitsPerException)
            {
                PackedInts.Format format = PackedInts.Format.PACKED;
                long blockSize = 1 + format.ByteCount(PackedInts.VERSION_CURRENT
                    , BLOCK_SIZE, bitsPerValue);
                // header: number of bits per value
                if (numExceptions > 0)
                {
                    blockSize += 2 + numExceptions + format.ByteCount(PackedInts
                        .VERSION_CURRENT, numExceptions, bitsPerException);
                }
                // 2 additional bytes in case of exceptions: numExceptions and bitsPerException
                // indices of the exceptions
                if (bufferSize < BLOCK_SIZE)
                {
                    blockSize += 1;
                }
                // length of the block
                return (int)blockSize;
            }

            internal virtual int UnaryBlockSize()
            {
                int deltaSum = 0;
                for (int i = 0; i < BLOCK_SIZE; ++i)
                {
                    deltaSum += 1 + buffer[i];
                }
                int blockSize = (int)(((uint)(deltaSum + unchecked((int)(0x07)))) >> 3);
                // round to the next byte
                ++blockSize;
                // header
                if (bufferSize < BLOCK_SIZE)
                {
                    blockSize += 1;
                }
                // length of the block
                return blockSize;
            }

            internal virtual int ComputeOptimalNumberOfBits()
            {
                ComputeFreqs();
                bitsPerValue = 31;
                numExceptions = 0;
                while (bitsPerValue > 0 && freqs[bitsPerValue] == 0)
                {
                    --bitsPerValue;
                }
                int actualBitsPerValue = bitsPerValue;
                int blockSize = PforBlockSize(bitsPerValue, numExceptions, bitsPerException);
                // Now try different values for bitsPerValue and pick the best one
                for (int i = this.bitsPerValue - 1; i >= 0 && numExceptions
                     <= MAX_EXCEPTIONS; numExceptions += freqs[i--])
                {
                    int newBlockSize = PforBlockSize(i, numExceptions, actualBitsPerValue
                        - i);
                    if (newBlockSize < blockSize)
                    {
                        this.bitsPerValue = i;
                        this.numExceptions = numExceptions;
                        blockSize = newBlockSize;
                    }
                }
                this.bitsPerException = actualBitsPerValue - bitsPerValue;
                //HM:revisit 
                //assert bufferSize < BLOCK_SIZE || numExceptions < bufferSize;
                return blockSize;
            }

            internal virtual void PforEncode()
            {
                if (numExceptions > 0)
                {
                    int mask = (1 << bitsPerValue) - 1;
                    int ex = 0;
                    for (int i = 0; i < bufferSize; ++i)
                    {
                        if (buffer[i] > mask)
                        {
                            exceptionIndices[ex] = i;
                            exceptions[ex++] = (int)(((uint)buffer[i]) >> bitsPerValue);
                            buffer[i] &= mask;
                        }
                    }
                    //HM:revisit 
                    //assert ex == numExceptions;
                    Arrays.Fill(exceptions, numExceptions, BLOCK_SIZE, 0);
                }
                if (bitsPerValue > 0)
                {
                    PackedInts.IEncoder encoder = PackedInts.GetEncoder(PackedInts.Format.PACKED, PackedInts
                        .VERSION_CURRENT, bitsPerValue);
                    int numIterations = ITERATIONS[bitsPerValue];
                    sbyte[] sbytes = new sbyte[data.bytes.Length];
                    data.bytes.CopyTo(sbytes, 0);
                    encoder.Encode(buffer, 0, sbytes, data.length, numIterations);
                    data.length += encoder.ByteBlockCount * numIterations;
                }
                if (numExceptions > 0)
                {
                    //HM:revisit 
                    //assert bitsPerException > 0;
                    data.WriteByte(unchecked((byte)numExceptions));
                    data.WriteByte(unchecked((byte)bitsPerException));
                    PackedInts.IEncoder encoder = PackedInts.GetEncoder(PackedInts.Format.PACKED, PackedInts
                        .VERSION_CURRENT, bitsPerException);
                    int numIterations = (numExceptions + encoder.ByteValueCount - 1) / encoder.ByteValueCount;
                    sbyte[] sbytes = new sbyte[data.bytes.Length];
                    data.bytes.CopyTo(sbytes, 0);
                    encoder.Encode(exceptions, 0, sbytes, data.length, numIterations);
                    PackedInts.Format format = PackedInts.Format.PACKED;
                    data.length += (int)format.ByteCount(PackedInts.VERSION_CURRENT, numExceptions, bitsPerException);
                    for (int i = 0; i < numExceptions; ++i)
                    {
                        data.WriteByte(unchecked((byte)exceptionIndices[i]));
                    }
                }
            }

            internal virtual void UnaryEncode()
            {
                int current = 0;
                for (int i = 0, doc = -1; i < BLOCK_SIZE; ++i)
                {
                    doc += 1 + buffer[i];
                    while (doc >= 8)
                    {
                        data.WriteByte(unchecked((byte)current));
                        current = 0;
                        doc -= 8;
                    }
                    current |= 1 << doc;
                }
                if (current != 0)
                {
                    data.WriteByte(unchecked((byte)current));
                }
            }

            internal virtual void EncodeBlock()
            {
                int originalLength = data.length;
                Arrays.Fill(buffer, bufferSize, BLOCK_SIZE, 0);
                int unaryBlockSize = UnaryBlockSize();
                int pforBlockSize = ComputeOptimalNumberOfBits();
                int blockSize;
                if (pforBlockSize <= unaryBlockSize)
                {
                    // use pfor
                    blockSize = pforBlockSize;
                    data.bytes = ArrayUtil.Grow(data.bytes, data.length + blockSize + MAX_BYTE_BLOCK_COUNT
                        );
                    int token = bufferSize < BLOCK_SIZE ? LAST_BLOCK : 0;
                    token |= bitsPerValue;
                    if (numExceptions > 0)
                    {
                        token |= HAS_EXCEPTIONS;
                    }
                    data.WriteByte(unchecked((byte)token));
                    PforEncode();
                }
                else
                {
                    // use unary
                    blockSize = unaryBlockSize;
                    int token = UNARY | (bufferSize < BLOCK_SIZE ? LAST_BLOCK : 0);
                    data.WriteByte(unchecked((byte)token));
                    UnaryEncode();
                }
                if (bufferSize < BLOCK_SIZE)
                {
                    data.WriteByte(unchecked((byte)bufferSize));
                }
                ++numBlocks;
            }

            //HM:revisit 
            //assert data.length - originalLength == blockSize : (data.length - originalLength) + " <> " + blockSize;
            /// <summary>
            /// Build the
            /// <see cref="PForDeltaDocIdSet">PForDeltaDocIdSet</see>
            /// instance.
            /// </summary>
            public virtual PForDeltaDocIdSet Build()
            {
                //HM:revisit 
                //assert bufferSize < BLOCK_SIZE;
                if (cardinality == 0)
                {
                    //HM:revisit 
                    //assert previousDoc == -1;
                    return EMPTY;
                }
                EncodeBlock();
                byte[] dataArr = Arrays.CopyOf(data.bytes, data.length + MAX_BYTE_BLOCK_COUNT);
                int indexSize = (numBlocks - 1) / indexInterval + 1;
                MonotonicAppendingLongBuffer docIDs;
                MonotonicAppendingLongBuffer offsets;
                if (indexSize <= 1)
                {
                    docIDs = offsets = SINGLE_ZERO_BUFFER;
                }
                else
                {
                    int pageSize = 128;
                    int initialPageCount = (indexSize + pageSize - 1) / pageSize;
                    docIDs = new MonotonicAppendingLongBuffer(initialPageCount, pageSize, PackedInts.
                        COMPACT);
                    offsets = new MonotonicAppendingLongBuffer(initialPageCount, pageSize, PackedInts
                        .COMPACT);
                    // Now build the index
                    var it = new DocIdSetIteratorSub(dataArr, cardinality
                        , int.MaxValue, SINGLE_ZERO_BUFFER, SINGLE_ZERO_BUFFER);
                    for (int k = 0; k < indexSize; ++k)
                    {
                        docIDs.Add(it.DocID + 1);
                        offsets.Add(it.offset);
                        for (int i = 0; i < indexInterval; ++i)
                        {
                            it.SkipBlock();
                            if (it.DocID == DocIdSetIterator.NO_MORE_DOCS)
                            {
                                goto index_break;
                            }
                        }
                    index_continue: ;
                    }
                index_break: ;
                    docIDs.Freeze();
                    offsets.Freeze();
                }
                return new PForDeltaDocIdSet(dataArr, cardinality, indexInterval, docIDs, offsets);
            }
        }

        internal readonly byte[] data;

        internal readonly MonotonicAppendingLongBuffer docIDs;

        internal readonly MonotonicAppendingLongBuffer offsets;

        internal readonly int cardinality;

        internal readonly int indexInterval;

        internal PForDeltaDocIdSet(byte[] data, int cardinality, int indexInterval, MonotonicAppendingLongBuffer
             docIDs, MonotonicAppendingLongBuffer offsets)
        {
            // for the index
            this.data = data;
            this.cardinality = cardinality;
            this.indexInterval = indexInterval;
            this.docIDs = docIDs;
            this.offsets = offsets;
        }

        public override bool IsCacheable
        {
            get { return true; }
        }

        public override DocIdSetIterator Iterator()
        {

            return data == null
                ? null
                : new DocIdSetIteratorSub(data, cardinality, indexInterval, docIDs, offsets);

        }

        internal class DocIdSetIteratorSub : DocIdSetIterator
        {
            internal readonly int indexInterval;

            internal readonly MonotonicAppendingLongBuffer docIDs;

            internal readonly MonotonicAppendingLongBuffer offsets;

            internal readonly int cardinality;

            internal readonly sbyte[] sBytes;

            internal int offset;

            internal readonly int[] nextDocs;

            internal int i;

            internal readonly int[] nextExceptions;

            internal int blockIdx;

            internal int docID;

            internal DocIdSetIteratorSub(byte[] data, int cardinality, int indexInterval, MonotonicAppendingLongBuffer
                 docIDs, MonotonicAppendingLongBuffer offsets)
            {
                // index
                // offset in data
                // index in nextDeltas
                this.sBytes = new sbyte[data.Length];
                data.CopyTo(sBytes,0);
                this.cardinality = cardinality;
                this.indexInterval = indexInterval;
                this.docIDs = docIDs;
                this.offsets = offsets;
                offset = 0;
                nextDocs = new int[BLOCK_SIZE];
                Arrays.Fill(nextDocs, -1);
                i = BLOCK_SIZE;
                nextExceptions = new int[BLOCK_SIZE];
                blockIdx = -1;
                docID = -1;
            }

            public override int DocID
            {
                get { return docID; }
            }

            internal virtual void PforDecompress(byte token)
            {
                int bitsPerValue = token & unchecked(0x1F);
                if (bitsPerValue == 0)
                {
                    Arrays.Fill(nextDocs, 0);
                }
                else
                {
                    DECODERS[bitsPerValue].Decode(sBytes, offset, nextDocs, 0, ITERATIONS[bitsPerValue]
                        );
                    offset += BYTE_BLOCK_COUNTS[bitsPerValue];
                }
                if ((token & HAS_EXCEPTIONS) != 0)
                {
                    // there are exceptions
                    int numExceptions = sBytes[offset++];
                    int bitsPerException = sBytes[offset++];
                    int numIterations = (numExceptions + DECODERS[bitsPerException].ByteValueCount
                        - 1) / DECODERS[bitsPerException].ByteValueCount;
                    DECODERS[bitsPerException].Decode(sBytes, offset, nextExceptions, 0, numIterations);
                    var packed = PackedInts.Format.PACKED;
                    offset += (int)packed.ByteCount(PackedInts.VERSION_CURRENT, numExceptions, bitsPerException);
                    for (int i = 0; i < numExceptions; ++i)
                    {
                        nextDocs[sBytes[offset++]] |= nextExceptions[i] << bitsPerValue;
                    }
                }
                for (int previousDoc = docID; i < BLOCK_SIZE; ++i)
                {
                    int doc = previousDoc + 1 + nextDocs[i];
                    previousDoc = nextDocs[i] = doc;
                }
            }

            internal virtual void UnaryDecompress(byte token)
            {
                //HM:revisit 
                //assert (token & HAS_EXCEPTIONS) == 0;
                int docID = this.docID;
                for (int j = 0; j < BLOCK_SIZE; )
                {
                    byte b = (byte)sBytes[offset++];
                    for (int bitList = BitUtil.BitList(b); bitList != 0; ++j, bitList = (int)(((uint)
                        bitList) >> 4))
                    {
                        nextDocs[j] = docID + (bitList & unchecked((int)(0x0F)));
                    }
                    docID += 8;
                }
            }

            internal virtual void DecompressBlock()
            {
                var token = (byte)sBytes[offset++];
                if ((token & UNARY) != 0)
                {
                    UnaryDecompress(token);
                }
                else
                {
                    PforDecompress(token);
                }
                if ((token & LAST_BLOCK) != 0)
                {
                    int blockSize = sBytes[offset++];
                    Arrays.Fill(nextDocs, blockSize, BLOCK_SIZE, NO_MORE_DOCS);
                }
                ++blockIdx;
            }

            internal virtual void SkipBlock()
            {
                //HM:revisit 
                //assert i == BLOCK_SIZE;
                DecompressBlock();
                docID = nextDocs[BLOCK_SIZE - 1];
            }

            public override int NextDoc()
            {
                if (i == BLOCK_SIZE)
                {
                    DecompressBlock();
                    i = 0;
                }
                return docID = nextDocs[i++];
            }

            internal virtual int ForwardBinarySearch(int target)
            {
                // advance forward and double the window at each step
                int indexSize = (int)docIDs.Size;
                int lo = Math.Max(blockIdx / indexInterval, 0);
                int hi = lo + 1;
                //HM:revisit 
                //assert blockIdx == -1 || docIDs.get(lo) <= docID;
                //HM:revisit 
                //assert lo + 1 == docIDs.size() || docIDs.get(lo + 1) > docID;
                while (true)
                {
                    if (hi >= indexSize)
                    {
                        hi = indexSize - 1;
                        break;
                    }
                    if (docIDs.Get(hi) >= target)
                    {
                        break;
                    }
                    int newLo = hi;
                    hi += (hi - lo) << 1;
                    lo = newLo;
                }
                // we found a window containing our target, let's binary search now
                while (lo <= hi)
                {
                    int mid = (int)(((uint)(lo + hi)) >> 1);
                    int midDocID = (int)docIDs.Get(mid);
                    if (midDocID <= target)
                    {
                        lo = mid + 1;
                    }
                    else
                    {
                        hi = mid - 1;
                    }
                }
                //HM:revisit 
                //assert docIDs.get(hi) <= target;
                //HM:revisit 
                //assert hi + 1 == docIDs.size() || docIDs.get(hi + 1) > target;
                return hi;
            }

            /// <exception cref="System.IO.IOException"></exception>
            public override int Advance(int target)
            {
                //HM:revisit 
                //assert target > docID;
                if (nextDocs[BLOCK_SIZE - 1] < target)
                {
                    // not in the next block, now use the index
                    int index = ForwardBinarySearch(target);
                    int offset = (int)offsets.Get(index);
                    if (offset > this.offset)
                    {
                        this.offset = offset;
                        docID = (int)docIDs.Get(index) - 1;
                        blockIdx = index * indexInterval - 1;
                        while (true)
                        {
                            DecompressBlock();
                            if (nextDocs[BLOCK_SIZE - 1] >= target)
                            {
                                break;
                            }
                            docID = nextDocs[BLOCK_SIZE - 1];
                        }
                        i = 0;
                    }
                }
                return SlowAdvance(target);
            }

            public override long Cost
            {
                get { return cardinality; }
            }
        }

        /// <summary>
        /// Return the number of documents in this
        /// <see cref="Lucene.Net.Search.DocIdSet">Lucene.Net.Search.DocIdSet</see>
        /// in constant time.
        /// </summary>
        public int Cardinality()
        {
            return cardinality;
        }

        /// <summary>Return the memory usage of this instance.</summary>
        /// <remarks>Return the memory usage of this instance.</remarks>
        public long RamBytesUsed()
        {
            return RamUsageEstimator.AlignObjectSize(3 * RamUsageEstimator.NUM_BYTES_OBJECT_REF
                ) + docIDs.RamBytesUsed + offsets.RamBytesUsed;
        }
    }
}
