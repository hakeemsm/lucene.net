using Lucene.Net.Codecs;
using Lucene.Net.Util;
using Lucene.Net.Util.Packed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    internal class NumericDocValuesWriter : DocValuesWriter
    {
        private const long MISSING = 0L;

		private AppendingDeltaPackedLongBuffer pending;
        private Counter iwBytesUsed;
        private long bytesUsed;
		private FixedBitSet docsWithField;
        private FieldInfo fieldInfo;

		public NumericDocValuesWriter(FieldInfo fieldInfo, Counter iwBytesUsed, bool trackDocsWithField
			)
        {
			pending = new AppendingDeltaPackedLongBuffer(PackedInts.COMPACT);
			docsWithField = trackDocsWithField ? new FixedBitSet(64) : null;
			bytesUsed = pending.RamBytesUsed + DocsWithFieldBytesUsed();
            this.fieldInfo = fieldInfo;
            this.iwBytesUsed = iwBytesUsed;
            iwBytesUsed.AddAndGet(bytesUsed);
        }

        public void AddValue(int docID, long value)
        {
            if (docID < pending.Size)
            {
                throw new ArgumentException("DocValuesField \"" + fieldInfo.name + "\" appears more than once in this document (only one value is allowed per field)");
            }

            // Fill in any holes:
            for (int i = (int)pending.Size; i < docID; ++i)
            {
                pending.Add(MISSING);
            }

            pending.Add(value);
			if (docsWithField != null)
			{
				docsWithField = FixedBitSet.EnsureCapacity(docsWithField, docID);
				docsWithField.Set(docID);
			}
            UpdateBytesUsed();
        }

		private long DocsWithFieldBytesUsed()
		{
			// size of the long[] + some overhead
			return docsWithField == null ? 0 : RamUsageEstimator.SizeOf(docsWithField.GetBits
				()) + 64;
		}
        private void UpdateBytesUsed()
        {
			long newBytesUsed = pending.RamBytesUsed + DocsWithFieldBytesUsed();
            iwBytesUsed.AddAndGet(newBytesUsed - bytesUsed);
            bytesUsed = newBytesUsed;
        }

		internal override void Finish(int maxDoc)
        {
        }

        internal override void Flush(SegmentWriteState state, DocValuesConsumer dvConsumer)
        {
            int maxDoc = state.segmentInfo.DocCount;

            dvConsumer.AddNumericField(fieldInfo, GetNumericIterator(maxDoc));
        }

        internal override void Abort()
        {
        }

        private IEnumerable<long> GetNumericIterator(int maxDoc)
        {
            // .NET Port: using yield return instead of custom iterator type. Much less code.

            AbstractAppendingLongBuffer.Iterator iter = pending.GetIterator();
            int size = (int)pending.Size;
            int upto = 0;

            while (upto < maxDoc)
            {
                long value;
                if (upto < size)
                {
                    value = iter.Next();
                }
                else
                {
                    value = 0;
                }
                upto++;
                // TODO: make reusable Number
                yield return value;
            }
        }
    }
}
