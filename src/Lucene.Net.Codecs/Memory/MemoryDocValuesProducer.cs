using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;
using Lucene.Net.Util.Packed;

namespace Lucene.Net.Codecs.Memory
{
    /// <summary>
    /// Reader for
    /// <see cref="MemoryDocValuesFormat">MemoryDocValuesFormat</see>
    /// </summary>
    internal class MemoryDocValuesProducer : DocValuesProducer
    {
        private readonly IDictionary<int, NumericEntry> numerics;

        private readonly IDictionary<int, BinaryEntry> binaries;

        private readonly IDictionary<int, FSTEntry> fsts;

        private readonly IndexInput data;

        private readonly IDictionary<int, NumericDocValues> numericInstances = new Dictionary<int, NumericDocValues>();

        private readonly IDictionary<int, BinaryDocValues> binaryInstances = new Dictionary<int, BinaryDocValues>();

        private readonly IDictionary<int, FST<long>> fstInstances = new Dictionary<int, FST<long>>();

        private readonly IDictionary<int, IBits> docsWithFieldInstances = new Dictionary<int, IBits>();

        private readonly int maxDoc;

        private readonly AtomicLong ramBytesUsed;

        private readonly int version;

        internal const byte NUMBER = 0;

        internal const byte BYTES = 1;

        internal const byte FST = 2;

        internal const int BLOCK_SIZE = 4096;

        internal const byte DELTA_COMPRESSED = 0;

        internal const byte TABLE_COMPRESSED = 1;

        internal const byte UNCOMPRESSED = 2;

        internal const byte GCD_COMPRESSED = 3;

        internal const int VERSION_START = 0;

        internal const int VERSION_GCD_COMPRESSION = 1;

        internal const int VERSION_CHECKSUM = 2;

        internal const int VERSION_CURRENT = VERSION_CHECKSUM;

        /// <exception cref="System.IO.IOException"></exception>
        internal MemoryDocValuesProducer(SegmentReadState state, string dataCodec, string
             dataExtension, string metaCodec, string metaExtension)
        {
            // metadata maps (just file pointers and minimal stuff)
            // ram instances we have already loaded
            maxDoc = state.segmentInfo.DocCount;
            string metaName = IndexFileNames.SegmentFileName(state.segmentInfo.name, state.segmentSuffix
                , metaExtension);
            // read in the entries from the metadata file.
            ChecksumIndexInput checkSumInput = state.directory.OpenChecksumInput(metaName, state.context
                );
            bool success = false;
            try
            {
                version = CodecUtil.CheckHeader(checkSumInput, metaCodec, VERSION_START, VERSION_CURRENT);
                numerics = new Dictionary<int, MemoryDocValuesProducer.NumericEntry>();
                binaries = new Dictionary<int, MemoryDocValuesProducer.BinaryEntry>();
                fsts = new Dictionary<int, MemoryDocValuesProducer.FSTEntry>();
                ReadFields(checkSumInput, state.fieldInfos);
                if (version >= VERSION_CHECKSUM)
                {
                    CodecUtil.CheckFooter(checkSumInput);
                }
                else
                {
                    CodecUtil.CheckEOF(checkSumInput);
                }
                ramBytesUsed = new AtomicLong(RamUsageEstimator.ShallowSizeOfInstance(GetType()));
                success = true;
            }
            finally
            {
                if (success)
                {
                    IOUtils.Close(checkSumInput);
                }
                else
                {
                    IOUtils.CloseWhileHandlingException((IDisposable)checkSumInput);
                }
            }
            success = false;
            try
            {
                string dataName = IndexFileNames.SegmentFileName(state.segmentInfo.name, state.segmentSuffix
                    , dataExtension);
                data = state.directory.OpenInput(dataName, state.context);
                int version2 = CodecUtil.CheckHeader(data, dataCodec, VERSION_START, VERSION_CURRENT
                    );
                if (version != version2)
                {
                    throw new CorruptIndexException("Format versions mismatch");
                }
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.CloseWhileHandlingException((IDisposable)data);
                }
            }
        }

        /// <exception cref="System.IO.IOException"></exception>
        private void ReadFields(IndexInput meta, FieldInfos infos)
        {
            int fieldNumber = meta.ReadVInt();
            while (fieldNumber != -1)
            {
                int fieldType = meta.ReadByte();
                if (fieldType == NUMBER)
                {
                    var entry = new NumericEntry { offset = meta.ReadLong(), missingOffset = meta.ReadLong() };
                    entry.missingBytes = entry.missingOffset != -1 ? meta.ReadLong() : 0;
                    entry.format = meta.ReadByte();
                    switch (entry.format)
                    {
                        case DELTA_COMPRESSED:
                        case TABLE_COMPRESSED:
                        case GCD_COMPRESSED:
                        case UNCOMPRESSED:
                            {
                                break;
                            }

                        default:
                            {
                                throw new CorruptIndexException("Unknown format: " + entry.format + ", input=" +
                                    meta);
                            }
                    }
                    if (entry.format != UNCOMPRESSED)
                    {
                        entry.packedIntsVersion = meta.ReadVInt();
                    }
                    numerics[fieldNumber] = entry;
                }
                else
                {
                    if (fieldType == BYTES)
                    {
                        var entry = new BinaryEntry
                        {
                            offset = meta.ReadLong(),
                            numBytes = meta.ReadLong(),
                            missingOffset = meta.ReadLong()
                        };
                        entry.missingBytes = entry.missingOffset != -1 ? meta.ReadLong() : 0;
                        entry.minLength = meta.ReadVInt();
                        entry.maxLength = meta.ReadVInt();
                        if (entry.minLength != entry.maxLength)
                        {
                            entry.packedIntsVersion = meta.ReadVInt();
                            entry.blockSize = meta.ReadVInt();
                        }
                        binaries[fieldNumber] = entry;
                    }
                    else
                    {
                        if (fieldType == FST)
                        {
                            var entry = new FSTEntry { offset = meta.ReadLong(), numOrds = meta.ReadVLong() };
                            fsts[fieldNumber] = entry;
                        }
                        else
                        {
                            throw new CorruptIndexException("invalid entry type: " + fieldType + ", input=" +
                                 meta);
                        }
                    }
                }
                fieldNumber = meta.ReadVInt();
            }
        }

        /// <exception cref="System.IO.IOException"></exception>
        public override NumericDocValues GetNumeric(FieldInfo field)
        {
            lock (this)
            {
                NumericDocValues instance = numericInstances[field.number];
                if (instance == null)
                {
                    instance = LoadNumeric(field);
                    numericInstances[field.number] = instance;
                }
                return instance;
            }
        }

        public override long RamBytesUsed
        {
            get { return ramBytesUsed.Get(); }
        }

        /// <exception cref="System.IO.IOException"></exception>
        public override void CheckIntegrity()
        {
            if (version >= VERSION_CHECKSUM)
            {
                CodecUtil.ChecksumEntireFile(data);
            }
        }

        /// <exception cref="System.IO.IOException"></exception>
        private NumericDocValues LoadNumeric(FieldInfo field)
        {
            MemoryDocValuesProducer.NumericEntry entry = numerics[field.number];
            data.Seek(entry.offset + entry.missingBytes);
            switch (entry.format)
            {
                case TABLE_COMPRESSED:
                    {
                        int size = data.ReadVInt();
                        if (size > 256)
                        {
                            throw new CorruptIndexException("TABLE_COMPRESSED cannot have more than 256 distinct values, input="
                                 + data);
                        }
                        long[] decode = new long[size];
                        for (int i = 0; i < decode.Length; i++)
                        {
                            decode[i] = data.ReadLong();
                        }
                        int formatID = data.ReadVInt();
                        int bitsPerValue = data.ReadVInt();
                        var ordsReader = PackedInts.GetReaderNoHeader(data, PackedInts.Format
                            .ById(formatID), entry.packedIntsVersion, maxDoc, bitsPerValue);
                        ramBytesUsed.AddAndGet(RamUsageEstimator.SizeOf(decode) + ordsReader.RamBytesUsed
                            ());
                        return new AnonymousNumericDocValues(decode, ordsReader);
                    }

                case DELTA_COMPRESSED:
                    {
                        int blockSize = data.ReadVInt();
                        BlockPackedReader reader = new BlockPackedReader(data, entry.packedIntsVersion, blockSize
                            , maxDoc, false);
                        ramBytesUsed.AddAndGet(reader.RamBytesUsed());
                        return reader;
                    }

                case UNCOMPRESSED:
                    {
                        byte[] bytes = new byte[maxDoc];
                        data.ReadBytes(bytes, 0, bytes.Length);
                        ramBytesUsed.AddAndGet(RamUsageEstimator.SizeOf(bytes));
                        return new AnonymousNumericDocValues2(bytes);
                    }

                case GCD_COMPRESSED:
                    {
                        long min = data.ReadLong();
                        long mult = data.ReadLong();
                        int quotientBlockSize = data.ReadVInt();
                        BlockPackedReader quotientReader = new BlockPackedReader(data, entry.packedIntsVersion
                            , quotientBlockSize, maxDoc, false);
                        ramBytesUsed.AddAndGet(quotientReader.RamBytesUsed());
                        return new AnonymousNumericDocValues3(min, mult, quotientReader);
                    }

                default:
                    {
                        throw new Exception();
                    }
            }
        }

        private sealed class AnonymousNumericDocValues : NumericDocValues
        {
            public AnonymousNumericDocValues(long[] decode, PackedInts.IReader ordsReader)
            {
                this.decode = decode;
                this.ordsReader = ordsReader;
            }

            public override long Get(int docID)
			{
				return decode[(int)ordsReader.Get(docID)];
			}

            private readonly long[] decode;

            private readonly PackedInts.IReader ordsReader;
        }

        private sealed class AnonymousNumericDocValues2 : NumericDocValues
        {
            public AnonymousNumericDocValues2(byte[] bytes)
            {
                this.bytes = bytes;
            }

            public override long Get(int docID)
            {
                return bytes[docID];
            }

            private readonly byte[] bytes;
        }

        private sealed class AnonymousNumericDocValues3 : NumericDocValues
        {
            public AnonymousNumericDocValues3(long min, long mult, BlockPackedReader quotientReader
                )
            {
                this.min = min;
                this.mult = mult;
                this.quotientReader = quotientReader;
            }

            public override long Get(int docID)
            {
                return min + mult * quotientReader.Get(docID);
            }

            private readonly long min;

            private readonly long mult;

            private readonly BlockPackedReader quotientReader;
        }


        public override BinaryDocValues GetBinary(FieldInfo field)
        {
            lock (this)
            {
                BinaryDocValues instance = binaryInstances[field.number];
                if (instance == null)
                {
                    instance = LoadBinary(field);
                    binaryInstances[field.number] = instance;
                }
                return instance;
            }
        }


        private BinaryDocValues LoadBinary(FieldInfo field)
        {
            BinaryEntry entry = binaries[field.number];
            data.Seek(entry.offset);
            PagedBytes bytes = new PagedBytes(16);
            bytes.Copy(data, entry.numBytes);
            PagedBytes.Reader bytesReader = bytes.Freeze(true);
            if (entry.minLength == entry.maxLength)
            {
                int fixedLength = entry.minLength;
                ramBytesUsed.AddAndGet(bytes.RamBytesUsed());
                return new AnonymousBinaryDocValues(bytesReader, fixedLength);
            }
            data.Seek(data.FilePointer + entry.missingBytes);
            var addresses = new MonotonicBlockPackedReader(data, entry.packedIntsVersion, entry.blockSize, maxDoc, false);
            ramBytesUsed.AddAndGet(bytes.RamBytesUsed() + addresses.RamBytesUsed());
            return new _BinaryDocValues_311(addresses, bytesReader);
        }

        private sealed class AnonymousBinaryDocValues : BinaryDocValues
        {
            public AnonymousBinaryDocValues(PagedBytes.Reader bytesReader, int fixedLength)
            {
                this.bytesReader = bytesReader;
                this.fixedLength = fixedLength;
            }

            public override void Get(int docID, BytesRef result)
            {
                bytesReader.FillSlice(result, fixedLength * (long)docID, fixedLength);
            }

            private readonly PagedBytes.Reader bytesReader;

            private readonly int fixedLength;
        }

        private sealed class _BinaryDocValues_311 : BinaryDocValues
        {
            public _BinaryDocValues_311(MonotonicBlockPackedReader addresses, PagedBytes.Reader
                 bytesReader)
            {
                this.addresses = addresses;
                this.bytesReader = bytesReader;
            }

            public override void Get(int docID, BytesRef result)
            {
                long startAddress = docID == 0 ? 0 : addresses.Get(docID - 1);
                long endAddress = addresses.Get(docID);
                bytesReader.FillSlice(result, startAddress, (int)(endAddress - startAddress));
            }

            private readonly MonotonicBlockPackedReader addresses;

            private readonly PagedBytes.Reader bytesReader;
        }

        /// <exception cref="System.IO.IOException"></exception>
        public override SortedDocValues GetSorted(FieldInfo field)
		{
			MemoryDocValuesProducer.FSTEntry entry = fsts[field.number];
			if (entry.numOrds == 0)
			{
				return DocValues.EMPTY_SORTED;
			}
			FST<long> instance;
			lock (this)
			{
				instance = fstInstances[field.number];
				if (instance == null)
				{
					data.Seek(entry.offset);
					instance = new FST<long>(data, PositiveIntOutputs.GetSingleton());
					ramBytesUsed.AddAndGet(instance.SizeInBytes());
					fstInstances[field.number] = instance;
				}
			}
			NumericDocValues docToOrd = GetNumeric(field);
			FST<long> fst = instance;
			// per-thread resources
			FST.BytesReader fstReader = fst.GetBytesReader();
			var firstArc = new FST.Arc<long>();
			var scratchArc = new FST.Arc<long>();
			var scratchInts = new IntsRef();
			var fstEnum = new BytesRefFSTEnum<long>(fst);
			return new AnonymousSortedValues(docToOrd, fstReader, fst, firstArc, scratchArc, scratchInts
				, fstEnum, entry);
		}

        private sealed class AnonymousSortedValues : SortedDocValues
        {
            public AnonymousSortedValues(NumericDocValues docToOrd, FST.BytesReader bytesReader, FST<long
                > fst, FST.Arc<long> firstArc, FST.Arc<long> scratchArc, IntsRef scratchInts, BytesRefFSTEnum
                <long> fstEnum, FSTEntry entry)
            {
                this.docToOrd = docToOrd;
                this.@in = bytesReader;
                this.fst = fst;
                this.firstArc = firstArc;
                this.scratchArc = scratchArc;
                this.scratchInts = scratchInts;
                this.fstEnum = fstEnum;
                this.entry = entry;
            }

            public override int GetOrd(int docID)
            {
                return (int)docToOrd.Get(docID);
            }

            public override void LookupOrd(int ord, BytesRef result)
            {

                @in.Position = 0;
                fst.GetFirstArc(firstArc);
                IntsRef output = Lucene.Net.Util.Fst.Util.GetByOutput(fst, ord, @in, firstArc
                    , scratchArc, scratchInts);
                result.bytes = new sbyte[output.length];
                result.offset = 0;
                result.length = 0;
                Lucene.Net.Util.Fst.Util.ToBytesRef(output, result);

            }

            public override int LookupTerm(BytesRef key)
            {

                BytesRefFSTEnum<long>.InputOutput<long> o = fstEnum.SeekCeil(key);
                if (o == null)
                {
                    return -this.ValueCount - 1;
                }
                else
                {
                    if (o.Input.Equals(key))
                    {
                        return (int)o.Output;
                    }
                    return (int)-o.Output - 1;
                }

            }

            public override int ValueCount
            {
                get { return (int) entry.numOrds; }
            }

            public override TermsEnum TermsEnum
            {
                get { return new FSTTermsEnum(fst); }
            }

            private readonly NumericDocValues docToOrd;

            private readonly FST.BytesReader @in;

            private readonly FST<long> fst;

            private readonly FST.Arc<long> firstArc;

            private readonly FST.Arc<long> scratchArc;

            private readonly IntsRef scratchInts;

            private readonly BytesRefFSTEnum<long> fstEnum;

            private readonly FSTEntry entry;
        }

        
        public override SortedSetDocValues GetSortedSet(FieldInfo field)
		{
			var entry = fsts[field.number];
			if (entry.numOrds == 0)
			{
				return DocValues.EMPTY_SORTED_SET;
			}
			// empty FST!
			FST<long> instance;
			lock (this)
			{
				instance = fstInstances[field.number];
				if (instance == null)
				{
					data.Seek(entry.offset);
					instance = new FST<long>(data, PositiveIntOutputs.GetSingleton());
					ramBytesUsed.AddAndGet(instance.SizeInBytes());
					fstInstances[field.number] = instance;
				}
			}
			BinaryDocValues docToOrds = GetBinary(field);
			FST<long> fst = instance;
			// per-thread resources
			FST.BytesReader @in = fst.GetBytesReader();
			var firstArc = new FST.Arc<long>();
			var scratchArc = new FST.Arc<long>();
			var scratchInts = new IntsRef();
			var fstEnum = new BytesRefFSTEnum<long>(fst);
			var bRef = new BytesRef();
			var input = new ByteArrayDataInput();
			return new AnonymousSortedSetDocValue(input, docToOrds, bRef, @in, fst, firstArc, scratchArc
				, scratchInts, fstEnum, entry);
		}

        private sealed class AnonymousSortedSetDocValue : SortedSetDocValues
        {
            public AnonymousSortedSetDocValue(ByteArrayDataInput input, BinaryDocValues docToOrds
                , BytesRef @ref, FST.BytesReader @in, FST<long> fst, FST.Arc<long> firstArc, FST.Arc
                <long> scratchArc, IntsRef scratchInts, BytesRefFSTEnum<long> fstEnum, MemoryDocValuesProducer.FSTEntry
                 entry)
            {
                this.input = input;
                this.docToOrds = docToOrds;
                this.@ref = @ref;
                this.@in = @in;
                this.fst = fst;
                this.firstArc = firstArc;
                this.scratchArc = scratchArc;
                this.scratchInts = scratchInts;
                this.fstEnum = fstEnum;
                this.entry = entry;
            }

            internal long currentOrd;

            public override long NextOrd()
            {
                if (input.EOF)
                {
                    return NO_MORE_ORDS;
                }
                this.currentOrd += input.ReadVLong();
                return this.currentOrd;
            }

            public override void SetDocument(int docID)
			{
				docToOrds.Get(docID, @ref);
				input.Reset(Array.ConvertAll(@ref.bytes,Convert.ToByte), @ref.offset, @ref.length);
				this.currentOrd = 0;
			}

            public override void LookupOrd(long ord, BytesRef result)
            {
                
                    @in.Position = 0;
                    fst.GetFirstArc(firstArc);
                    IntsRef output = Lucene.Net.Util.Fst.Util.GetByOutput(fst, ord, @in, firstArc
                        , scratchArc, scratchInts);
                    result.bytes = new sbyte[output.length];
                    result.offset = 0;
                    result.length = 0;
                    Lucene.Net.Util.Fst.Util.ToBytesRef(output, result);
                            }

            public override long LookupTerm(BytesRef key)
            {
               
                    BytesRefFSTEnum<long>.InputOutput<long> o = fstEnum.SeekCeil(key);
                    if (o == null)
                    {
                        return -this.ValueCount - 1;
                    }
                    if (o.Input.Equals(key))
                    {
                        return o.Output;
                    }
                    return -o.Output - 1;
               
            }

            public override long ValueCount
            {
                get { return entry.numOrds; }
            }

            public override TermsEnum TermsEnum
            {
                get { return new FSTTermsEnum(fst); }
            }

            private readonly ByteArrayDataInput input;

            private readonly BinaryDocValues docToOrds;

            private readonly BytesRef @ref;

            private readonly FST.BytesReader @in;

            private readonly FST<long> fst;

            private readonly FST.Arc<long> firstArc;

            private readonly FST.Arc<long> scratchArc;

            private readonly IntsRef scratchInts;

            private readonly BytesRefFSTEnum<long> fstEnum;

            private readonly FSTEntry entry;
        }

        /// <exception cref="System.IO.IOException"></exception>
        private IBits GetMissingBits(int fieldNumber, long offset, long length)
		{
			if (offset == -1)
			{
				return new Bits.MatchAllBits(maxDoc);
			}
            IBits instance;
            lock (this)
            {
                instance = docsWithFieldInstances[fieldNumber];
                if (instance == null)
                {
                    IndexInput data = ((IndexInput)this.data.Clone());
                    data.Seek(offset);
                    //HM:revisit 
                    //assert length % 8 == 0;
                    long[] bits = new long[(int)length >> 3];
                    for (int i = 0; i < bits.Length; i++)
                    {
                        bits[i] = data.ReadLong();
                    }
                    instance = new FixedBitSet(bits, maxDoc);
                    docsWithFieldInstances[fieldNumber] = instance;
                }
            }
            return instance;
		}

        /// <exception cref="System.IO.IOException"></exception>
        public override IBits GetDocsWithField(FieldInfo field)
		{
			switch (field.GetDocValuesType())
			{
				case FieldInfo.DocValuesType.SORTED_SET:
				{
					return DocValues.DocsWithValue(GetSortedSet(field), maxDoc);
				}

				case FieldInfo.DocValuesType.SORTED:
				{
					return DocValues.DocsWithValue(GetSorted(field), maxDoc);
				}

				case FieldInfo.DocValuesType.BINARY:
				{
					MemoryDocValuesProducer.BinaryEntry be = binaries[field.number];
					return GetMissingBits(field.number, be.missingOffset, be.missingBytes);
				}

				case FieldInfo.DocValuesType.NUMERIC:
				{
					MemoryDocValuesProducer.NumericEntry ne = numerics[field.number];
					return GetMissingBits(field.number, ne.missingOffset, ne.missingBytes);
				}

				default:
				{
					throw new Exception();
				}
			}
		}

        /// <exception cref="System.IO.IOException"></exception>
        protected override void Dispose(bool disposing)
        {
            data.Dispose();
        }

        internal class NumericEntry
        {
            internal long offset;

            internal long missingOffset;

            internal long missingBytes;

            internal byte format;

            internal int packedIntsVersion;
        }

        internal class BinaryEntry
        {
            internal long offset;

            internal long missingOffset;

            internal long missingBytes;

            internal long numBytes;

            internal int minLength;

            internal int maxLength;

            internal int packedIntsVersion;

            internal int blockSize;
        }

        internal class FSTEntry
        {
            internal long offset;

            internal long numOrds;
        }

        internal class FSTTermsEnum : TermsEnum
        {
            internal readonly BytesRefFSTEnum<long> bRef;

            internal readonly FST<long> fst;

            internal readonly FST.BytesReader bytesReader;

            internal readonly FST.Arc<long> firstArc = new FST.Arc<long>();

            internal readonly FST.Arc<long> scratchArc = new FST.Arc<long>();

            internal readonly IntsRef scratchInts = new IntsRef();

            internal readonly BytesRef scratchBytes = new BytesRef();

            internal FSTTermsEnum(FST<long> fst)
            {
                // exposes FSTEnum directly as a TermsEnum: avoids binary-search next()
                // this is all for the complicated seek(ord)...
                // maybe we should add a FSTEnum that supports this operation?
                this.fst = fst;
                bRef = new BytesRefFSTEnum<long>(fst);
                bytesReader = fst.GetBytesReader();
            }

            /// <exception cref="System.IO.IOException"></exception>
            public override BytesRef Next()
            {
                BytesRefFSTEnum<long>.InputOutput<long> io = bRef.Next();
                if (io == null)
                {
                    return null;
                }
                else
                {
                    return io.Input;
                }
            }

            public override IComparer<BytesRef> Comparator
            {
                get { return BytesRef.UTF8SortedAsUnicodeComparer; }
            }

            /// <exception cref="System.IO.IOException"></exception>
            public override SeekStatus SeekCeil(BytesRef text)
            {
                if (bRef.SeekCeil(text) == null)
                {
                    return SeekStatus.END;
                }
                if (Term.Equals(text))
                {
                    // TODO: add SeekStatus to FSTEnum like in https://issues.apache.org/jira/browse/LUCENE-3729
                    // to remove this comparision?
                    return SeekStatus.FOUND;
                }
                return SeekStatus.NOT_FOUND;
            }

            
            public override bool SeekExact(BytesRef text)
            {
                return bRef.SeekExact(text) != null;
            }

            
            public override void SeekExact(long ord)
            {
                // TODO: would be better to make this simpler and faster.
                // but we dont want to introduce a bug that corrupts our enum state!
                bytesReader.Position = 0;
                fst.GetFirstArc(firstArc);
                IntsRef output = Lucene.Net.Util.Fst.Util.GetByOutput(fst, ord, bytesReader
                    , firstArc, scratchArc, scratchInts);
                scratchBytes.bytes = new sbyte[output.length];
                scratchBytes.offset = 0;
                scratchBytes.length = 0;
                Lucene.Net.Util.Fst.Util.ToBytesRef(output, scratchBytes);
                // TODO: we could do this lazily, better to try to push into FSTEnum though?
                bRef.SeekExact(scratchBytes);
            }

            
            public override BytesRef Term
            {
                get { return bRef.Current().Input; }
            }

            
            public override long Ord
            {
                get { return bRef.Current().Output; }
            }

            
            public override int DocFreq
            {
                get { throw new NotSupportedException(); }
            }

            
            public override long TotalTermFreq
            {
                get { throw new NotSupportedException(); }
            }

            
            public override DocsEnum Docs(IBits liveDocs, DocsEnum reuse, int flags)
            {
                throw new NotSupportedException();
            }

            /// <exception cref="System.IO.IOException"></exception>
            public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum
                 reuse, int flags)
            {
                throw new NotSupportedException();
            }
        }
    }
}
