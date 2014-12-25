using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;
using Lucene.Net.Util.Packed;

namespace Lucene.Net.Codecs.Memory
{
    /// <summary>
    /// Writer for
    /// <see cref="MemoryDocValuesFormat">MemoryDocValuesFormat</see>
    /// </summary>
    internal class MemoryDocValuesConsumer : DocValuesConsumer
    {
        internal IndexOutput data;

        internal IndexOutput meta;

        internal readonly int maxDoc;

        internal readonly float acceptableOverheadRatio;

        /// <exception cref="System.IO.IOException"></exception>
        internal MemoryDocValuesConsumer(SegmentWriteState state, string dataCodec, string
             dataExtension, string metaCodec, string metaExtension, float acceptableOverheadRatio
            )
        {
            this.acceptableOverheadRatio = acceptableOverheadRatio;
            maxDoc = state.segmentInfo.DocCount;
            bool success = false;
            try
            {
                string dataName = IndexFileNames.SegmentFileName(state.segmentInfo.name, state.segmentSuffix
                    , dataExtension);
                data = state.directory.CreateOutput(dataName, state.context);
                CodecUtil.WriteHeader(data, dataCodec, MemoryDocValuesProducer.VERSION_CURRENT);
                string metaName = IndexFileNames.SegmentFileName(state.segmentInfo.name, state.segmentSuffix
                    , metaExtension);
                meta = state.directory.CreateOutput(metaName, state.context);
                CodecUtil.WriteHeader(meta, metaCodec, MemoryDocValuesProducer.VERSION_CURRENT);
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.CloseWhileHandlingException((IDisposable)this);
                }
            }
        }


        public override void AddNumericField(FieldInfo field, IEnumerable<long?> values)
        {
            AddNumericField(field, values, true);
        }


        internal virtual void AddNumericField(FieldInfo field, IEnumerable<long?> values, bool optimizeStorage)
        {
            meta.WriteVInt(field.number);
            meta.WriteByte(MemoryDocValuesProducer.NUMBER);
            meta.WriteLong(data.FilePointer);
            long minValue = long.MaxValue;
            long maxValue = long.MinValue;
            long gcd = 0;
            bool missing = false;
            // TODO: more efficient?
            HashSet<long> uniqueValues = null;
            var valuesList = values as IList<long?> ?? values.ToList();
            if (optimizeStorage)
            {
                uniqueValues = new HashSet<long>();
                long count = 0;
                foreach (var nv in valuesList)
                {
                    long v;
                    if (nv == null)
                    {
                        v = 0;
                        missing = true;
                    }
                    else
                    {
                        v = nv.Value;
                    }
                    if (gcd != 1)
                    {
                        if (v < long.MinValue / 2 || v > long.MaxValue / 2)
                        {
                            // in that case v - minValue might overflow and make the GCD computation return
                            // wrong results. Since these extreme values are unlikely, we just discard
                            // GCD computation for them
                            gcd = 1;
                        }
                        else
                        {
                            if (count != 0)
                            {
                                // minValue needs to be set first
                                gcd = MathUtil.Gcd(gcd, v - minValue);
                            }
                        }
                    }
                    minValue = Math.Min(minValue, v);
                    maxValue = Math.Max(maxValue, v);
                    if (uniqueValues != null)
                    {
                        if (uniqueValues.Add(v))
                        {
                            if (uniqueValues.Count > 256)
                            {
                                uniqueValues = null;
                            }
                        }
                    }
                    ++count;
                }
            }
            //HM:revisit 
            //assert count == maxDoc;
            if (missing)
            {
                long start = data.FilePointer;
                WriteMissingBitset(valuesList);
                meta.WriteLong(start);
                meta.WriteLong(data.FilePointer - start);
            }
            else
            {
                meta.WriteLong(-1L);
            }
            if (uniqueValues != null)
            {
                // small number of unique values
                int bitsPerValue = PackedInts.BitsRequired(uniqueValues.Count - 1);
                PackedInts.FormatAndBits formatAndBits = PackedInts.FastestFormatAndBits(maxDoc,
                    bitsPerValue, acceptableOverheadRatio);
                if (formatAndBits.bitsPerValue == 8 && minValue >= byte.MinValue && maxValue <= byte.MaxValue)
                {
                    meta.WriteByte(MemoryDocValuesProducer.UNCOMPRESSED);
                    // uncompressed
                    foreach (var nv in valuesList)
                    {
                        data.WriteLong(nv == null ? 0 : nv.Value);
                    }
                }
                else
                {
                    meta.WriteByte(MemoryDocValuesProducer.TABLE_COMPRESSED);
                    // table-compressed
                    long[] decode = uniqueValues.ToArray();
                    Dictionary<long, int> encode = new Dictionary<long, int>();
                    data.WriteVInt(decode.Length);
                    for (int i = 0; i < decode.Length; i++)
                    {
                        data.WriteLong(decode[i]);
                        encode[decode[i]] = i;
                    }
                    meta.WriteVInt(PackedInts.VERSION_CURRENT);
                    data.WriteVInt(formatAndBits.format.GetId());
                    data.WriteVInt(formatAndBits.bitsPerValue);
                    PackedInts.Writer writer = PackedInts.GetWriterNoHeader(data, formatAndBits.format
                        , maxDoc, formatAndBits.bitsPerValue, PackedInts.DEFAULT_BUFFER_SIZE);
                    foreach (var nv in valuesList)
                    {
                        writer.Add(encode[nv == null ? 0 : nv.Value]);
                    }
                    writer.Finish();
                }
            }
            else
            {
                if (gcd != 0 && gcd != 1)
                {
                    meta.WriteByte(MemoryDocValuesProducer.GCD_COMPRESSED);
                    meta.WriteVInt(PackedInts.VERSION_CURRENT);
                    data.WriteLong(minValue);
                    data.WriteLong(gcd);
                    data.WriteVInt(MemoryDocValuesProducer.BLOCK_SIZE);
                    var writer = new BlockPackedWriter(data, MemoryDocValuesProducer.BLOCK_SIZE);
                    foreach (var nv in valuesList)
                    {
                        long value = nv == null ? 0 : nv.Value;
                        writer.Add((value - minValue) / gcd);
                    }
                    writer.Finish();
                }
                else
                {
                    meta.WriteByte(MemoryDocValuesProducer.DELTA_COMPRESSED);
                    // delta-compressed
                    meta.WriteVInt(PackedInts.VERSION_CURRENT);
                    data.WriteVInt(MemoryDocValuesProducer.BLOCK_SIZE);
                    var writer = new BlockPackedWriter(data, MemoryDocValuesProducer.BLOCK_SIZE);
                    foreach (var nv in valuesList)
                    {
                        writer.Add(nv == null ? 0 : nv.Value);
                    }
                    writer.Finish();
                }
            }
        }


        protected override void Dispose(bool disposing)
        {
            bool success = false;
            try
            {
                if (meta != null)
                {
                    meta.WriteVInt(-1);
                    // write EOF marker
                    CodecUtil.WriteFooter(meta);
                }
                // write checksum
                if (data != null)
                {
                    CodecUtil.WriteFooter(data);
                }
                success = true;
            }
            finally
            {
                if (success)
                {
                    IOUtils.Close(data, meta);
                }
                else
                {
                    IOUtils.CloseWhileHandlingException((IDisposable)data, meta);
                }
                data = meta = null;
            }
        }

        /// <exception cref="System.IO.IOException"></exception>
        public override void AddBinaryField(FieldInfo field, IEnumerable<BytesRef> values)
        {
            // write the byte[] data
            meta.WriteVInt(field.number);
            meta.WriteByte(MemoryDocValuesProducer.BYTES);
            int minLength = int.MaxValue;
            int maxLength = int.MinValue;
            long startFP = data.FilePointer;
            bool missing = false;
            foreach (BytesRef v in values)
            {
                int length;
                if (v == null)
                {
                    length = 0;
                    missing = true;
                }
                else
                {
                    length = v.length;
                }
                if (length > MemoryDocValuesFormat.MAX_BINARY_FIELD_LENGTH)
                {
                    throw new ArgumentException("DocValuesField \"" + field.name + "\" is too large, must be <= "
                         + MemoryDocValuesFormat.MAX_BINARY_FIELD_LENGTH);
                }
                minLength = Math.Min(minLength, length);
                maxLength = Math.Max(maxLength, length);
                if (v != null)
                {
                    data.WriteBytes(v.bytes, v.offset, v.length);
                }
            }
            meta.WriteLong(startFP);
            meta.WriteLong(data.FilePointer - startFP);
            if (missing)
            {
                long start = data.FilePointer;
                WriteMissingBitset(values);
                meta.WriteLong(start);
                meta.WriteLong(data.FilePointer - start);
            }
            else
            {
                meta.WriteLong(-1L);
            }
            meta.WriteVInt(minLength);
            meta.WriteVInt(maxLength);
            // if minLength == maxLength, its a fixed-length byte[], we are done (the addresses are implicit)
            // otherwise, we need to record the length fields...
            if (minLength != maxLength)
            {
                meta.WriteVInt(PackedInts.VERSION_CURRENT);
                meta.WriteVInt(MemoryDocValuesProducer.BLOCK_SIZE);
                MonotonicBlockPackedWriter writer = new MonotonicBlockPackedWriter(data, MemoryDocValuesProducer
                    .BLOCK_SIZE);
                long addr = 0;
                foreach (BytesRef v_1 in values)
                {
                    if (v_1 != null)
                    {
                        addr += v_1.length;
                    }
                    writer.Add(addr);
                }
                writer.Finish();
            }
        }

        /// <exception cref="System.IO.IOException"></exception>
        private void WriteFST(FieldInfo field, IEnumerable<BytesRef> values)
        {
            meta.WriteVInt(field.number);
            meta.WriteByte(MemoryDocValuesProducer.FST);
            meta.WriteLong(data.FilePointer);
            PositiveIntOutputs outputs = PositiveIntOutputs.GetSingleton();
            Builder<long> builder = new Builder<long>(FST.INPUT_TYPE.BYTE1, outputs);
            IntsRef scratch = new IntsRef();
            long ord = 0;
            foreach (BytesRef v in values)
            {
                builder.Add(Lucene.Net.Util.Fst.Util.ToIntsRef(v, scratch), ord);
                ord++;
            }
            FST<long> fst = builder.Finish();
            if (fst != null)
            {
                fst.Save(data);
            }
            meta.WriteVLong(ord);
        }

        // TODO: in some cases representing missing with minValue-1 wouldn't take up additional space and so on,
        // but this is very simple, and algorithms only check this for values of 0 anyway (doesnt slow down normal decode)
        /// <exception cref="System.IO.IOException"></exception>
        internal virtual void WriteMissingBitset<T>(IEnumerable<T> values)
        {
            long bits = 0;
            int count = 0;
            foreach (object v in values)
            {
                if (count == 64)
                {
                    data.WriteLong(bits);
                    count = 0;
                    bits = 0;
                }
                if (v != null)
                {
                    bits |= 1L << (count & unchecked((int)(0x3f)));
                }
                count++;
            }
            if (count > 0)
            {
                data.WriteLong(bits);
            }
        }

        /// <exception cref="System.IO.IOException"></exception>
        public override void AddSortedField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<int?> docToOrd)
        {
            // write the ordinals as numerics
            AddNumericField(field, docToOrd.ToList().ConvertAll(i=>(long?)i), false);
            // write the values as FST
            WriteFST(field, values);
        }

        // note: this might not be the most efficient... but its fairly simple
        /// <exception cref="System.IO.IOException"></exception>
        public override void AddSortedSetField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<int?> docToOrdCount, IEnumerable<long?> ords)
        {
            // write the ordinals as a binary field
            AddBinaryField(field, GetByteRefEnumerable(docToOrdCount, ords));

            // write the values as FST
            WriteFST(field, values);
        }

        private IEnumerable<BytesRef> GetByteRefEnumerable(IEnumerable<int?> docToOrdCount, IEnumerable<long?> ords)
        {
            var buffer = new sbyte[10];
            var byteArray = new ByteArrayDataOutput();
            var longList = ords as IList<long?> ?? ords.ToList();
            BytesRef bytesRef = new BytesRef();
            foreach (var i in docToOrdCount)
            {
                int maxSize = i.Value * 9;
                if (maxSize > buffer.Length)
                {
                    buffer = ArrayUtil.Grow(buffer, maxSize);
                }


                byteArray.Reset(buffer);
                long lastOrd = 0;
                for (int i2 = 0; i2 < i; i2++)
                {
                    long ord = longList[i2].Value;
                    byteArray.WriteVLong(ord - lastOrd);
                    lastOrd = ord;
                }
                bytesRef.bytes = buffer;
                bytesRef.offset = 0;
                bytesRef.length = byteArray.Position;
                yield return bytesRef;
            }
        }

    }
}
