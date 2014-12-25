using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Packed;

namespace Lucene.Net.Codecs.Lucene45
{
    /// <summary>
    /// writer for
    /// <see cref="Lucene45DocValuesFormat">Lucene45DocValuesFormat</see>
    /// 
    /// </summary>
    public class Lucene45DocValuesConsumer : DocValuesConsumer, IDisposable
    {
        internal const int BLOCK_SIZE = 16384;

        internal const int ADDRESS_INTERVAL = 16;

        internal static readonly sbyte MISSING_ORD = -1;

        /// <summary>Compressed using packed blocks of ints.</summary>
        /// <remarks>Compressed using packed blocks of ints.</remarks>
        public const int DELTA_COMPRESSED = 0;

        /// <summary>Compressed by computing the GCD.</summary>
        /// <remarks>Compressed by computing the GCD.</remarks>
        public const int GCD_COMPRESSED = 1;

        /// <summary>Compressed by giving IDs to unique values.</summary>
        /// <remarks>Compressed by giving IDs to unique values.</remarks>
        public const int TABLE_COMPRESSED = 2;

        /// <summary>Uncompressed binary, written directly (fixed length).</summary>
        /// <remarks>Uncompressed binary, written directly (fixed length).</remarks>
        public const int BINARY_FIXED_UNCOMPRESSED = 0;

        /// <summary>Uncompressed binary, written directly (variable length).</summary>
        /// <remarks>Uncompressed binary, written directly (variable length).</remarks>
        public const int BINARY_VARIABLE_UNCOMPRESSED = 1;

        /// <summary>Compressed binary with shared prefixes</summary>
        public const int BINARY_PREFIX_COMPRESSED = 2;

        /// <summary>
        /// Standard storage for sorted set values with 1 level of indirection:
        /// docId -&gt; address -&gt; ord.
        /// </summary>
        /// <remarks>
        /// Standard storage for sorted set values with 1 level of indirection:
        /// docId -&gt; address -&gt; ord.
        /// </remarks>
        public const int SORTED_SET_WITH_ADDRESSES = 0;

        /// <summary>
        /// Single-valued sorted set values, encoded as sorted values, so no level
        /// of indirection: docId -&gt; ord.
        /// </summary>
        /// <remarks>
        /// Single-valued sorted set values, encoded as sorted values, so no level
        /// of indirection: docId -&gt; ord.
        /// </remarks>
        public const int SORTED_SET_SINGLE_VALUED_SORTED = 1;

        internal IndexOutput data;

        internal IndexOutput meta;

        internal readonly int maxDoc;

        /// <summary>expert: Creates a new writer</summary>
        /// <exception cref="System.IO.IOException"></exception>
        public Lucene45DocValuesConsumer(SegmentWriteState state, string dataCodec, string
             dataExtension, string metaCodec, string metaExtension)
        {
            // javadocs
            bool success = false;
            try
            {
                string dataName = IndexFileNames.SegmentFileName(state.segmentInfo.name, state.segmentSuffix
                    , dataExtension);
                data = state.directory.CreateOutput(dataName, state.context);
                CodecUtil.WriteHeader(data, dataCodec, Lucene45DocValuesFormat.VERSION_CURRENT);
                string metaName = IndexFileNames.SegmentFileName(state.segmentInfo.name, state.segmentSuffix
                    , metaExtension);
                meta = state.directory.CreateOutput(metaName, state.context);
                CodecUtil.WriteHeader(meta, metaCodec, Lucene45DocValuesFormat.VERSION_CURRENT);
                maxDoc = state.segmentInfo.DocCount;
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

        /// <exception cref="System.IO.IOException"></exception>
        public override void AddNumericField(FieldInfo field, IEnumerable<long?> values)
        {
            AddNumericField(field, values, true);
        }

        /// <exception cref="System.IO.IOException"></exception>
        internal virtual void AddNumericField(FieldInfo field, IEnumerable<long?> values, bool
             optimizeStorage)
        {
            long count = 0;
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
                foreach (var nv in valuesList)
                {
                    long v = nv.Value;
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
            else
            {
                count += valuesList.LongCount();
            }
            long delta = maxValue - minValue;
            int format;
            if (uniqueValues != null && (delta < 0L || PackedInts.BitsRequired(uniqueValues.Count
                 - 1) < PackedInts.BitsRequired(delta)) && count <= int.MaxValue)
            {
                format = TABLE_COMPRESSED;
            }
            else
            {
                if (gcd != 0 && gcd != 1)
                {
                    format = GCD_COMPRESSED;
                }
                else
                {
                    format = DELTA_COMPRESSED;
                }
            }
            meta.WriteVInt(field.number);
            meta.WriteByte(Lucene45DocValuesFormat.NUMERIC);
            meta.WriteVInt(format);
            meta.WriteLong(-1L);
            meta.WriteVInt(PackedInts.VERSION_CURRENT);
            meta.WriteLong(data.FilePointer);
            meta.WriteVLong(count);
            meta.WriteVInt(BLOCK_SIZE);
            switch (format)
            {
                case GCD_COMPRESSED:
                    {
                        meta.WriteLong(minValue);
                        meta.WriteLong(gcd);
                        var quotientWriter = new BlockPackedWriter(data, BLOCK_SIZE);
                        foreach (var nv1 in valuesList)
                        {
                            var value = nv1;
                            quotientWriter.Add((value.Value - minValue) / gcd);
                        }
                        quotientWriter.Finish();
                        break;
                    }

                case DELTA_COMPRESSED:
                    {
                        var writer = new BlockPackedWriter(data, BLOCK_SIZE);
                        foreach (var nv2 in valuesList)
                        {
                            writer.Add(nv2.Value);
                        }
                        writer.Finish();
                        break;
                    }

                case TABLE_COMPRESSED:
                    {
                        long[] decode = uniqueValues.ToArray();
                        var encode = new Dictionary<long, int>();
                        meta.WriteVInt(decode.Length);
                        for (int i = 0; i < decode.Length; i++)
                        {
                            meta.WriteLong(decode[i]);
                            encode[decode[i]] = i;
                        }
                        int bitsRequired = PackedInts.BitsRequired(uniqueValues.Count - 1);
                        PackedInts.Writer ordsWriter = PackedInts.GetWriterNoHeader(data, PackedInts.Format
                            .PACKED, (int)count, bitsRequired, PackedInts.DEFAULT_BUFFER_SIZE);
                        foreach (var nv3 in valuesList)
                        {
                            ordsWriter.Add(encode[nv3.Value]);
                        }
                        ordsWriter.Finish();
                        break;
                    }

                default:
                    {
                        throw new Exception();
                    }
            }
        }

        // TODO: in some cases representing missing with minValue-1 wouldn't take up additional space and so on,
        // but this is very simple, and algorithms only check this for values of 0 anyway (doesnt slow down normal decode)
        /// <exception cref="System.IO.IOException"></exception>
        internal virtual void WriteMissingBitset<T>(IEnumerable<T> values)
        {
            byte bits = 0;
            int count = 0;
            foreach (object v in values)
            {
                if (count == 8)
                {
                    data.WriteByte(bits);
                    count = 0;
                    bits = 0;
                }
                if (v != null)
                {
                    bits |= (byte)(1 << (count & 7));
                }
                count++;
            }
            if (count > 0)
            {
                data.WriteByte(bits);
            }
        }

        /// <exception cref="System.IO.IOException"></exception>
        public override void AddBinaryField(FieldInfo field, IEnumerable<BytesRef> values)
        {
            // write the byte[] data
            meta.WriteVInt(field.number);
            meta.WriteByte(Lucene45DocValuesFormat.BINARY);
            int minLength = int.MaxValue;
            int maxLength = int.MinValue;
            long startFP = data.FilePointer;
            long count = 0;
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
                minLength = Math.Min(minLength, length);
                maxLength = Math.Max(maxLength, length);
                if (v != null)
                {
                    data.WriteBytes(v.bytes, v.offset, v.length);
                }
                count++;
            }
            meta.WriteVInt(minLength == maxLength ? BINARY_FIXED_UNCOMPRESSED : BINARY_VARIABLE_UNCOMPRESSED
                );
            if (missing)
            {
                meta.WriteLong(data.FilePointer);
                WriteMissingBitset(values);
            }
            else
            {
                meta.WriteLong(-1L);
            }
            meta.WriteVInt(minLength);
            meta.WriteVInt(maxLength);
            meta.WriteVLong(count);
            meta.WriteLong(startFP);
            // if minLength == maxLength, its a fixed-length byte[], we are done (the addresses are implicit)
            // otherwise, we need to record the length fields...
            if (minLength != maxLength)
            {
                meta.WriteLong(data.FilePointer);
                meta.WriteVInt(PackedInts.VERSION_CURRENT);
                meta.WriteVInt(BLOCK_SIZE);
                MonotonicBlockPackedWriter writer = new MonotonicBlockPackedWriter(data, BLOCK_SIZE
                    );
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

        /// <summary>expert: writes a value dictionary for a sorted/sortedset field</summary>
        /// <exception cref="System.IO.IOException"></exception>
        protected internal virtual void AddTermsDict(FieldInfo field, IEnumerable<BytesRef>
            values)
        {
            // first check if its a "fixed-length" terms dict
            int minLength = int.MaxValue;
            int maxLength = int.MinValue;
            foreach (BytesRef v in values)
            {
                minLength = Math.Min(minLength, v.length);
                maxLength = Math.Max(maxLength, v.length);
            }
            if (minLength == maxLength)
            {
                // no index needed: direct addressing by mult
                AddBinaryField(field, values);
            }
            else
            {
                // header
                meta.WriteVInt(field.number);
                meta.WriteByte(Lucene45DocValuesFormat.BINARY);
                meta.WriteVInt(BINARY_PREFIX_COMPRESSED);
                meta.WriteLong(-1L);
                // now write the bytes: sharing prefixes within a block
                long startFP = data.FilePointer;
                // currently, we have to store the delta from expected for every 1/nth term
                // we could avoid this, but its not much and less overall RAM than the previous approach!
                RAMOutputStream addressBuffer = new RAMOutputStream();
                MonotonicBlockPackedWriter termAddresses = new MonotonicBlockPackedWriter(addressBuffer
                    , BLOCK_SIZE);
                BytesRef lastTerm = new BytesRef();
                long count = 0;
                foreach (BytesRef v_1 in values)
                {
                    if (count % ADDRESS_INTERVAL == 0)
                    {
                        termAddresses.Add(data.FilePointer - startFP);
                        // force the first term in a block to be abs-encoded
                        lastTerm.length = 0;
                    }
                    // prefix-code
                    int sharedPrefix = StringHelper.BytesDifference(lastTerm, v_1);
                    data.WriteVInt(sharedPrefix);
                    data.WriteVInt(v_1.length - sharedPrefix);
                    data.WriteBytes(v_1.bytes, v_1.offset + sharedPrefix, v_1.length - sharedPrefix);
                    lastTerm.CopyBytes(v_1);
                    count++;
                }
                long indexStartFP = data.FilePointer;
                // write addresses of indexed terms
                termAddresses.Finish();
                addressBuffer.WriteTo(data);
                addressBuffer = null;
                termAddresses = null;
                meta.WriteVInt(minLength);
                meta.WriteVInt(maxLength);
                meta.WriteVLong(count);
                meta.WriteLong(startFP);
                meta.WriteVInt(ADDRESS_INTERVAL);
                meta.WriteLong(indexStartFP);
                meta.WriteVInt(PackedInts.VERSION_CURRENT);
                meta.WriteVInt(BLOCK_SIZE);
            }
        }

        /// <exception cref="System.IO.IOException"></exception>
        public override void AddSortedField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<int?> docToOrd)
        {
            meta.WriteVInt(field.number);
            meta.WriteByte(Lucene45DocValuesFormat.SORTED);
            AddTermsDict(field, values);
            var docToOrdLong = new List<long?>();
            docToOrd.ToList().ForEach(i=>docToOrdLong.Add(i)); //this was needed because of the constraints imposed by the method being overridden
            AddNumericField(field, docToOrdLong, false);
        }

        private static bool IsSingleValued(IEnumerable<int?> docToOrdCount)
        {
            return docToOrdCount.All(ordCount => ordCount <= 1);
        }

        /// <exception cref="System.IO.IOException"></exception>
        public override void AddSortedSetField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<int?> docToOrdCount, IEnumerable<long?> ords)
        {
            meta.WriteVInt(field.number);
            meta.WriteByte(Lucene45DocValuesFormat.SORTED_SET);
            var ordList = docToOrdCount as IList<int?> ?? docToOrdCount.ToList();
            if (IsSingleValued(ordList))
            {
                meta.WriteVInt(SORTED_SET_SINGLE_VALUED_SORTED);
                // The field is single-valued, we can encode it as SORTED
                AddSortedField(field, values, new SortedFieldEnumerable(ordList, ords));
                return;
            }
            meta.WriteVInt(SORTED_SET_WITH_ADDRESSES);
            // write the ord -> byte[] as a binary field
            AddTermsDict(field, values);
            // write the stream of ords as a numeric field
            // NOTE: we could return an iterator that delta-encodes these within a doc
            AddNumericField(field, ords, false);
            // write the doc -> ord count as a absolute index to the stream
            meta.WriteVInt(field.number);
            meta.WriteByte(Lucene45DocValuesFormat.NUMERIC);
            meta.WriteVInt(DELTA_COMPRESSED);
            meta.WriteLong(-1L);
            meta.WriteVInt(PackedInts.VERSION_CURRENT);
            meta.WriteLong(data.FilePointer);
            meta.WriteVLong(maxDoc);
            meta.WriteVInt(BLOCK_SIZE);
            MonotonicBlockPackedWriter writer = new MonotonicBlockPackedWriter(data, BLOCK_SIZE);
            long? addr = 0;
            foreach (var v in ordList)
            {
                addr += v;
                writer.Add(addr.Value);
            }
            writer.Finish();
        }

        private sealed class SortedFieldEnumerable : IEnumerable<int?>
        {
            private readonly IEnumerable<int?> _docToOrdCount;
            private readonly IEnumerable<long?> _ords;

            public SortedFieldEnumerable(IList<int?> docToOrdCount, IEnumerable<long?> ords)
            {
                _docToOrdCount = docToOrdCount;
                _ords = ords;
            }

            public IEnumerator<int?> GetEnumerator()
            {
                var docToOrdCountIt = _docToOrdCount.GetEnumerator();
                var ordsIt = _ords.GetEnumerator();
                return new SortedFieldEnumerator(docToOrdCountIt, ordsIt);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private sealed class SortedFieldEnumerator : IEnumerator<int?>
        {
            private readonly IEnumerator<int?> _docToOrdCountIt;
            private readonly IEnumerator<long?> _ordsIt;

            public SortedFieldEnumerator(IEnumerator<int?> docToOrdCountIt, IEnumerator<long?> ordsIt)
            {
                _docToOrdCountIt = docToOrdCountIt;
                _ordsIt = ordsIt;
            }

            public void Dispose()
            {
                _docToOrdCountIt.Dispose();
                _ordsIt.Dispose();
            }

            public bool MoveNext()
            {
                return _docToOrdCountIt.MoveNext(); 
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }

            public int? Current
            {
                //the cast should not be needed if the overridden AddSetSortedField is updated
                get { return _docToOrdCountIt.Current == 0 ? MISSING_ORD : (int) _ordsIt.Current; }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }
        }

        /// <exception cref="System.IO.IOException"></exception>
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
                // write checksum
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
                meta = data = null;
            }
        }
    }
}
