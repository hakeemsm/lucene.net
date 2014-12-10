using Lucene.Net.Codecs;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Packed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    internal class BinaryDocValuesWriter : DocValuesWriter
    {
		private int MAX_LENGTH = ArrayUtil.MAX_ARRAY_LENGTH;
		private const int BLOCK_BITS = 15;
		private readonly PagedBytes bytes;
		private readonly DataOutput bytesOut;
		private readonly Counter iwBytesUsed;
		private readonly AppendingDeltaPackedLongBuffer lengths;
		private FixedBitSet docsWithField;
        private readonly FieldInfo fieldInfo;
        private int addedValues = 0;

		private long bytesUsed;
        public BinaryDocValuesWriter(FieldInfo fieldInfo, Counter iwBytesUsed)
        {
            this.fieldInfo = fieldInfo;
			this.bytes = new PagedBytes(BLOCK_BITS);
			this.bytesOut = bytes.GetDataOutput();
			this.lengths = new AppendingDeltaPackedLongBuffer(PackedInts.COMPACT);
			this.iwBytesUsed = iwBytesUsed;
			this.docsWithField = new FixedBitSet(64);
			this.bytesUsed = DocsWithFieldBytesUsed();
			iwBytesUsed.AddAndGet(bytesUsed);
        }

        public void AddValue(int docID, BytesRef value)
        {
            if (docID < addedValues)
            {
                throw new ArgumentException("DocValuesField \"" + fieldInfo.name + "\" appears more than once in this document (only one value is allowed per field)");
            }
            if (value == null)
            {
                throw new ArgumentException("field=\"" + fieldInfo.name + "\": null value not allowed");
            }
			if (value.length > MAX_LENGTH)
            {
                throw new ArgumentException("DocValuesField \"" + fieldInfo.name + "\" is too large, must be <= " + (ByteBlockPool.BYTE_BLOCK_SIZE - 2));
            }

            // Fill in any holes:
            while (addedValues < docID)
            {
                addedValues++;
                lengths.Add(0);
            }
            addedValues++;
            lengths.Add(value.length);
			try
			{
				bytesOut.WriteBytes(value.bytes, value.offset, value.length);
			}
			catch (IOException ioe)
			{
				// Should never happen!
				throw new RuntimeException(ioe);
			}
			docsWithField = FixedBitSet.EnsureCapacity(docsWithField, docID);
			docsWithField.Set(docID);
			UpdateBytesUsed();
        }

		private long DocsWithFieldBytesUsed()
		{
			// size of the long[] + some overhead
			return RamUsageEstimator.SizeOf(docsWithField.GetBits()) + 64;
		}
		private void UpdateBytesUsed()
		{
			long newBytesUsed = lengths.RamBytesUsed() + bytes.RamBytesUsed() + DocsWithFieldBytesUsed
				();
			iwBytesUsed.AddAndGet(newBytesUsed - bytesUsed);
			bytesUsed = newBytesUsed;
		}
		internal override void Finish(int maxDoc)
        {
        }

        internal override void Flush(SegmentWriteState state, DocValuesConsumer dvConsumer)
        {
            int maxDoc = state.segmentInfo.DocCount;
			bytes.Freeze(false);
            dvConsumer.AddBinaryField(fieldInfo, GetBytesIterator(maxDoc));
        }

        internal override void Abort()
        {
        }

        private IEnumerable<BytesRef> GetBytesIterator(int maxDocParam)
        { 
            // .NET port: using yield return instead of a custom IEnumerable type
            
            BytesRef value = new BytesRef();
            AppendingLongBuffer.Iterator lengthsIterator = (AppendingLongBuffer.Iterator)lengths.GetIterator();
            int size = (int) lengths.Size;
            int maxDoc = maxDocParam;
            int upto = 0;
            long byteOffset = 0L;

            while (upto < maxDoc)
            {
                if (upto < size)
                {
                    int length = (int)lengthsIterator.Next();
                    value.Grow(length);
                    value.length = length;
                    pool.ReadBytes(byteOffset, value.bytes, value.offset, value.length);
                    byteOffset += length;
                }
                else
                {
                    // This is to handle last N documents not having
                    // this DV field in the end of the segment:
                    value.length = 0;
                }

                upto++;
                yield return value;
            }
        }
    }
}
