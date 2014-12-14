/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.IO;
using System.Linq;
using Lucene.Net.Codecs.Lucene40;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Packed;
using System;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Codecs.Compressing
{

    /**
     * {@link StoredFieldsReader} impl for {@link CompressingStoredFieldsFormat}.
     * @lucene.experimental
     */
    public sealed class CompressingStoredFieldsReader : StoredFieldsReader
    {
        private const int BUFFER_REUSE_THRESHOLD = 1 << 15;

        private readonly int version;
        private FieldInfos fieldInfos;
        private CompressingStoredFieldsIndexReader indexReader;
        private readonly long maxPointer;
        private IndexInput fieldsStream;
        private readonly int chunkSize;
        private int packedIntsVersion;
        private CompressionMode compressionMode;
        private Decompressor decompressor;
        private BytesRef bytes;
        private int numDocs;
        private bool closed;

        // used by clone
        private CompressingStoredFieldsReader(CompressingStoredFieldsReader reader)
        {
            this.version = reader.version;
            this.fieldInfos = reader.fieldInfos;
            this.fieldsStream = (IndexInput)reader.fieldsStream.Clone();
            this.indexReader = (CompressingStoredFieldsIndexReader)reader.indexReader.Clone();
            this.maxPointer = reader.maxPointer;
            this.chunkSize = reader.chunkSize;
            this.packedIntsVersion = reader.packedIntsVersion;
            this.compressionMode = reader.compressionMode;
            this.decompressor = (Decompressor)reader.decompressor.Clone();
            this.numDocs = reader.numDocs;
            this.bytes = new BytesRef(reader.bytes.bytes.Length);
            this.closed = false;
        }

        /** Sole constructor. */
        public CompressingStoredFieldsReader(Directory d, SegmentInfo si, string segmentSuffix, FieldInfos fn,
            IOContext context, string formatName, CompressionMode compressionMode)
        {
            this.compressionMode = compressionMode;
            string segment = si.name;
            bool success = false;
            fieldInfos = fn;
            numDocs = si.DocCount;
            ChecksumIndexInput indexStream = null;
            try
            {
                string indexStreamFN = IndexFileNames.SegmentFileName(segment, segmentSuffix, Lucene40StoredFieldsWriter.FIELDS_INDEX_EXTENSION);
                string fieldsStreamFN = IndexFileNames.SegmentFileName(segment, segmentSuffix, Lucene40StoredFieldsWriter
                    .FIELDS_EXTENSION);
				indexStream = d.OpenChecksumInput(indexStreamFN, context);

                string codecNameIdx = formatName + CompressingStoredFieldsWriter.CODEC_SFX_IDX;
                version = CodecUtil.CheckHeader(indexStream, codecNameIdx, CompressingStoredFieldsWriter
                    .VERSION_START, CompressingStoredFieldsWriter.VERSION_CURRENT);

                indexReader = new CompressingStoredFieldsIndexReader(indexStream, si);
                long maxPointer = -1;
                if (version >= CompressingStoredFieldsWriter.VERSION_CHECKSUM)
                {
                    maxPointer = indexStream.ReadVLong();
                    CodecUtil.CheckFooter(indexStream);
                }
                else
                {
                    CodecUtil.CheckEOF(indexStream);
                }
                indexStream.Dispose();
                indexStream = null;
                // Open the data file and read metadata
                fieldsStream = d.OpenInput(fieldsStreamFN, context);
                if (version >= CompressingStoredFieldsWriter.VERSION_CHECKSUM)
                {
                    if (maxPointer + CodecUtil.FooterLength() != fieldsStream.Length)
                    {
                        throw new CorruptIndexException("Invalid fieldsStream maxPointer (file truncated?): maxPointer="
                             + maxPointer + ", length=" + fieldsStream.Length);
                    }
                }
                else
                {
                    maxPointer = fieldsStream.Length;
                }
                this.maxPointer = maxPointer;
                string codecNameDat = formatName + CompressingStoredFieldsWriter.CODEC_SFX_DAT;
                int fieldsVersion = CodecUtil.CheckHeader(fieldsStream, codecNameDat, CompressingStoredFieldsWriter
                    .VERSION_START, CompressingStoredFieldsWriter.VERSION_CURRENT);
                if (version != fieldsVersion)
                {
                    throw new CorruptIndexException("Version mismatch between stored fields index and data: "
                         + version + " != " + fieldsVersion);
                }
                if (version >= CompressingStoredFieldsWriter.VERSION_BIG_CHUNKS)
                {
                    chunkSize = fieldsStream.ReadVInt();
                }
                else
                {
                    chunkSize = -1;
                }
                packedIntsVersion = fieldsStream.ReadVInt();
                decompressor = compressionMode.NewDecompressor();
                this.bytes = new BytesRef();

                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.CloseWhileHandlingException((IDisposable)this, indexStream);
                }
            }
        }

        /**
         * @throws AlreadyClosedException if this FieldsReader is closed
         */
        private void EnsureOpen()
        {
            if (closed)
            {
                throw new AlreadyClosedException("this FieldsReader is closed");
            }
        }

        /** 
         * Close the underlying {@link IndexInput}s.
         */
        protected override void Dispose(bool disposing)
        {
            if (!closed)
            {
				IOUtils.Close(fieldsStream);
                closed = true;
            }
        }

        private static void ReadField(DataInput input, StoredFieldVisitor visitor, FieldInfo info, int bits)
        {
            switch (bits & CompressingStoredFieldsWriter.TYPE_MASK)
            {
                case CompressingStoredFieldsWriter.BYTE_ARR:
                    int length = input.ReadVInt();
                    byte[] data = new byte[length];
                    input.ReadBytes(data, 0, length);
                    visitor.BinaryField(info, (sbyte[])(Array)data);
                    break;
                case CompressingStoredFieldsWriter.STRING:
                    length = input.ReadVInt();
                    data = new byte[length];
                    input.ReadBytes(data, 0, length);
                    visitor.StringField(info, IOUtils.CHARSET_UTF_8.GetString(data));
                    break;
                case CompressingStoredFieldsWriter.NUMERIC_INT:
                    visitor.IntField(info, input.ReadInt());
                    break;
                case CompressingStoredFieldsWriter.NUMERIC_FLOAT:
                    visitor.FloatField(info, input.ReadInt().IntBitsToFloat());
                    break;
                case CompressingStoredFieldsWriter.NUMERIC_LONG:
                    visitor.LongField(info, input.ReadLong());
                    break;
                case CompressingStoredFieldsWriter.NUMERIC_DOUBLE:
                    visitor.DoubleField(info, BitConverter.Int64BitsToDouble(input.ReadLong()));
                    break;
                default:
                    throw new InvalidOperationException("Unknown type flag: " + bits.ToString("X"));
            }
        }

        private static void SkipField(DataInput input, int bits)
        {
            switch (bits & CompressingStoredFieldsWriter.TYPE_MASK)
            {
                case CompressingStoredFieldsWriter.BYTE_ARR:
                case CompressingStoredFieldsWriter.STRING:
                    int length = input.ReadVInt();
                    input.SkipBytes(length);
                    break;
                case CompressingStoredFieldsWriter.NUMERIC_INT:
                case CompressingStoredFieldsWriter.NUMERIC_FLOAT:
                    input.ReadInt();
                    break;
                case CompressingStoredFieldsWriter.NUMERIC_LONG:
                case CompressingStoredFieldsWriter.NUMERIC_DOUBLE:
                    input.ReadLong();
                    break;
                default:
                    throw new InvalidOperationException("Unknown type flag: " + bits.ToString("X"));
            }
        }

        public override void VisitDocument(int docID, StoredFieldVisitor visitor)
        {
            fieldsStream.Seek(indexReader.GetStartPointer(docID));

            int docBase = fieldsStream.ReadVInt();
            int chunkDocs = fieldsStream.ReadVInt();
            if (docID < docBase
                || docID >= docBase + chunkDocs
                || docBase + chunkDocs > numDocs)
            {
				throw new CorruptIndexException("Corrupted: docID=" + docID + ", docBase=" + docBase
					 + ", chunkDocs=" + chunkDocs + ", numDocs=" + numDocs + " (resource=" + fieldsStream
					 + ")");
            }

            int numStoredFields, length, offset, totalLength;
            if (chunkDocs == 1)
            {
                numStoredFields = fieldsStream.ReadVInt();
                offset = 0;
                length = fieldsStream.ReadVInt();
                totalLength = length;
            }
            else
            {
                int bitsPerStoredFields = fieldsStream.ReadVInt();
                if (bitsPerStoredFields == 0)
                {
                    numStoredFields = fieldsStream.ReadVInt();
                }
                else if (bitsPerStoredFields > 31)
                {
                    throw new CorruptIndexException("bitsPerStoredFields=" + bitsPerStoredFields + " (resource="
                         + fieldsStream + ")");
                }
                else
                {
                    long filePointer = fieldsStream.FilePointer;
                    PackedInts.Reader reader = PackedInts.GetDirectReaderNoHeader(fieldsStream, PackedInts.Format.PACKED, packedIntsVersion, chunkDocs, bitsPerStoredFields);
                    numStoredFields = (int)(reader.Get(docID - docBase));
                    fieldsStream.Seek(filePointer + PackedInts.Format.PACKED.ByteCount(packedIntsVersion, chunkDocs, bitsPerStoredFields));
                }

                int bitsPerLength = fieldsStream.ReadVInt();
                if (bitsPerLength == 0)
                {
                    length = fieldsStream.ReadVInt();
                    offset = (docID - docBase) * length;
                    totalLength = chunkDocs * length;
                }
                else if (bitsPerStoredFields > 31)
                {
                    throw new CorruptIndexException("bitsPerLength=" + bitsPerLength + " (resource="
                        + fieldsStream + ")");
                }
                else
                {
                    PackedInts.ReaderIterator it = (PackedInts.ReaderIterator)PackedInts.GetReaderIteratorNoHeader(fieldsStream, PackedInts.Format.PACKED, packedIntsVersion, chunkDocs, bitsPerLength, 1);
                    int off = 0;
                    for (int i = 0; i < docID - docBase; ++i)
                    {
                        //TODO - HACKMP - Paul, this is a point of concern for me, in that everything from this file, and the 
                        //decompressor.Decompress() contract is looking for int.  But, I don't want to simply cast from long to int here.
                        off += (int)it.Next();
                    }
                    offset = off;
                    length = (int)it.Next();
                    off += length;
                    for (int i = docID - docBase + 1; i < chunkDocs; ++i)
                    {
                        off += (int)it.Next();
                    }
                    totalLength = off;
                }
            }

            if ((length == 0) != (numStoredFields == 0))
            {
                throw new CorruptIndexException("length=" + length + ", numStoredFields=" + numStoredFields
                     + " (resource=" + fieldsStream + ")");
            }
            if (numStoredFields == 0)
            {
                // nothing to do
                return;
            }
            DataInput documentInput;
            if (version >= CompressingStoredFieldsWriter.VERSION_BIG_CHUNKS && totalLength >=
                 2 * chunkSize)
            {
                //HM:revisit 
                //assert chunkSize > 0;
                //HM:revisit 
                //assert offset < chunkSize;
                decompressor.Decompress(fieldsStream, chunkSize, offset, Math.Min(length, chunkSize
                     - offset), bytes);
                documentInput = new AnonymousDataInput(this, length);
            }
            else
            {
                //HM:revisit 
                //assert decompressed <= length;
                BytesRef bytes = totalLength <= BUFFER_REUSE_THRESHOLD ? this.bytes : new BytesRef
                    ();
                decompressor.Decompress(fieldsStream, totalLength, offset, length, bytes);
                byte[] byteArray = Array.ConvertAll(bytes.bytes, b => (byte) b);
                //assert bytes.length == length;
                documentInput = new ByteArrayDataInput(byteArray, bytes.offset, bytes.length);
            }
            for (int fieldIDX = 0; fieldIDX < numStoredFields; fieldIDX++)
            {
                long infoAndBits = documentInput.ReadVLong();
                int fieldNumber = (int)Number.URShift(infoAndBits, CompressingStoredFieldsWriter.TYPE_BITS); // (infoAndBits >>> TYPE_BITS);
                FieldInfo fieldInfo = fieldInfos.FieldInfo(fieldNumber);

                int bits = (int)(infoAndBits & CompressingStoredFieldsWriter.TYPE_MASK);

                switch (visitor.NeedsField(fieldInfo))
                {
                    case StoredFieldVisitor.Status.YES:
                        ReadField(documentInput, visitor, fieldInfo, bits);
                        break;
                    case StoredFieldVisitor.Status.NO:
                        SkipField(documentInput, bits);
                        break;
                    case StoredFieldVisitor.Status.STOP:
                        return;
                }
            }
        }

        private sealed class AnonymousDataInput : DataInput
        {
            public AnonymousDataInput(CompressingStoredFieldsReader parent, int length)
            {
                this._parent = parent;
                this.length = length;
                this.decompressed = this._parent.bytes.length;
            }

            internal int decompressed;

            /// <exception cref="System.IO.IOException"></exception>
            internal void FillBuffer()
            {
                if (this.decompressed == length)
                {
                    throw new EndOfStreamException();
                }
                int toDecompress = Math.Min(length - this.decompressed, this._parent.chunkSize);
                this._parent.decompressor.Decompress(this._parent.fieldsStream, toDecompress
                    , 0, toDecompress, this._parent.bytes);
                this.decompressed += toDecompress;
            }

            /// <exception cref="System.IO.IOException"></exception>
            public override byte ReadByte()
            {
                if (this._parent.bytes.length == 0)
                {
                    this.FillBuffer();
                }
                --this._parent.bytes.length;
                return (byte) this._parent.bytes.bytes[this._parent.bytes.offset++];
            }

            /// <exception cref="System.IO.IOException"></exception>
            public override void ReadBytes(byte[] b, int offset, int len)
            {
                while (len > this._parent.bytes.length)
                {
                    System.Array.Copy(this._parent.bytes.bytes, this._parent.bytes.offset, b, offset
                        , this._parent.bytes.length);
                    len -= this._parent.bytes.length;
                    offset += this._parent.bytes.length;
                    this.FillBuffer();
                }
                System.Array.Copy(this._parent.bytes.bytes, this._parent.bytes.offset, b, offset
                    , len);
                this._parent.bytes.offset += len;
                this._parent.bytes.length -= len;
            }

            private readonly CompressingStoredFieldsReader _parent;

            private readonly int length;
        }
        public override object Clone()
        {
            EnsureOpen();
            return new CompressingStoredFieldsReader(this);
        }

        internal int GetVersion()
        {
            return version;
        }
        public CompressionMode CompressionMode
        {
            get
            {
                return compressionMode;
            }
        }

        public int ChunkSize
        {
            get { return chunkSize; }
        }

        // .NET Port: renamed to GetChunkIterator to avoid conflict with nested type.
        internal ChunkIterator GetChunkIterator(int startDocID)
        {
            EnsureOpen();
            return new ChunkIterator(this, startDocID);
        }

        internal sealed class ChunkIterator
        {
            internal readonly ChecksumIndexInput fieldsStream;
            internal readonly BytesRef spare;
            internal BytesRef bytes;
            internal int docBase;
            internal int chunkDocs;
            internal int[] numStoredFields;
            internal int[] lengths;

            private readonly CompressingStoredFieldsReader parent;

            public ChunkIterator(CompressingStoredFieldsReader parent, int startDocId)
            {
                this.parent = parent; // .NET Port

                this.docBase = -1;
                bytes = new BytesRef();
                this.spare = new BytesRef();
                numStoredFields = new int[1];
                lengths = new int[1];
                IndexInput @in = this._enclosing.fieldsStream;
                @in.Seek(0);
                this.fieldsStream = new BufferedChecksumIndexInput(@in);
                this.fieldsStream.Seek(this._enclosing.indexReader.GetStartPointer(startDocId));
            }

            /**
             * Return the decompressed size of the chunk
             */
            public int ChunkSize()
            {
                int sum = 0;
                for (int i = 0; i < chunkDocs; ++i)
                {
                    sum += lengths[i];
                }
                return sum;
            }

            /**
             * Go to the chunk containing the provided doc ID.
             */
            public void Next(int doc)
            {
                this.fieldsStream.Seek(this._enclosing.indexReader.GetStartPointer(doc));
                int docBase = this.fieldsStream.ReadVInt();
                int chunkDocs = this.fieldsStream.ReadVInt();
                if (docBase < this.docBase + this.chunkDocs || docBase + chunkDocs > this._enclosing
                    .numDocs)
                {
                    throw new CorruptIndexException("Corrupted: current docBase=" + this.docBase + ", current numDocs="
                         + this.chunkDocs + ", new docBase=" + docBase + ", new numDocs=" + chunkDocs +
                        " (resource=" + this.fieldsStream + ")");
                }
                this.docBase = docBase;
                this.chunkDocs = chunkDocs;
                if (chunkDocs > this.numStoredFields.Length)
                {
                    int newLength = ArrayUtil.Oversize(chunkDocs, 4);
                    this.numStoredFields = new int[newLength];
                    this.lengths = new int[newLength];
                }
                if (chunkDocs == 1)
                {
                    this.numStoredFields[0] = this.fieldsStream.ReadVInt();
                    this.lengths[0] = this.fieldsStream.ReadVInt();
                }
                else
                {
                    int bitsPerStoredFields = this.fieldsStream.ReadVInt();
                    if (bitsPerStoredFields == 0)
                    {
                        Arrays.Fill(this.numStoredFields, 0, chunkDocs, this.fieldsStream.ReadVInt());
                    }
                    else
                    {
                        if (bitsPerStoredFields > 31)
                        {
                            throw new CorruptIndexException("bitsPerStoredFields=" + bitsPerStoredFields + " (resource="
                                 + this.fieldsStream + ")");
                        }
                        PackedInts.IReaderIterator it = PackedInts.GetReaderIteratorNoHeader(this.fieldsStream
                            , PackedInts.Format.PACKED, this._enclosing.packedIntsVersion, chunkDocs, bitsPerStoredFields
                            , 1);
                        for (int i = 0; i < chunkDocs; ++i)
                        {
                            this.numStoredFields[i] = (int)it.Next();
                        }
                    }
                    int bitsPerLength = this.fieldsStream.ReadVInt();
                    if (bitsPerLength == 0)
                    {
                        Arrays.Fill(this.lengths, 0, chunkDocs, this.fieldsStream.ReadVInt());
                    }
                    else
                    {
                        if (bitsPerLength > 31)
                        {
                            throw new CorruptIndexException("bitsPerLength=" + bitsPerLength);
                        }
                        else
                        {
                            PackedInts.IReaderIterator it = PackedInts.GetReaderIteratorNoHeader(this.fieldsStream
                                , PackedInts.Format.PACKED, this._enclosing.packedIntsVersion, chunkDocs, bitsPerLength
                                , 1);
                            for (int i = 0; i < chunkDocs; ++i)
                            {
                                this.lengths[i] = (int)it.Next();
                            }
                        }
                    }
                }
            }

            /**
             * Decompress the chunk.
             */
            public void Decompress()
            {
                // decompress data
                int chunkSize = this.ChunkSize();
                if (this._enclosing.version >= CompressingStoredFieldsWriter.VERSION_BIG_CHUNKS &&
                     chunkSize >= 2 * this._enclosing.chunkSize)
                {
                    this.bytes.offset = this.bytes.length = 0;
                    for (int decompressed = 0; decompressed < chunkSize; )
                    {
                        int toDecompress = Math.Min(chunkSize - decompressed, this._enclosing.chunkSize);
                        this._enclosing.decompressor.Decompress(this.fieldsStream, toDecompress, 0, toDecompress
                            , this.spare);
                        this.bytes.bytes = ArrayUtil.Grow(this.bytes.bytes, this.bytes.length + this.spare
                            .length);
                        System.Array.Copy(this.spare.bytes, this.spare.offset, this.bytes.bytes, this.bytes
                            .length, this.spare.length);
                        this.bytes.length += this.spare.length;
                        decompressed += toDecompress;
                    }
                }
                else
                {
                    this._enclosing.decompressor.Decompress(this.fieldsStream, chunkSize, 0, chunkSize
                        , this.bytes);
                }
                if (bytes.length != chunkSize)
                {
                    throw new CorruptIndexException("Corrupted: expected chunk size = " + this.ChunkSize
                        () + ", got " + this.bytes.length + " (resource=" + this.fieldsStream + ")");
                }
            }

            /**
             * Copy compressed data.
             */
            public void CopyCompressedData(DataOutput output)
            {
                long chunkEnd = this.docBase + this.chunkDocs == this._enclosing.numDocs ? this._enclosing
                    .maxPointer : this._enclosing.indexReader.GetStartPointer(this.docBase + this.chunkDocs
                    );
                output.CopyBytes(this.fieldsStream, chunkEnd - this.fieldsStream.FilePointer);
            }

            /// <summary>Check integrity of the data.</summary>
            /// <remarks>Check integrity of the data. The iterator is not usable after this method has been called.
            /// 	</remarks>
            /// <exception cref="System.IO.IOException"></exception>
            internal void CheckIntegrity()
            {
                if (this._enclosing.version >= CompressingStoredFieldsWriter.VERSION_CHECKSUM)
                {
                    this.fieldsStream.Seek(this.fieldsStream.Length - CodecUtil.FooterLength());
                    CodecUtil.CheckFooter(this.fieldsStream);
                }
            }

            private readonly CompressingStoredFieldsReader _enclosing;
        }

        public override long RamBytesUsed
        {
            get { return indexReader.RamBytesUsed(); }
        }

        /// <exception cref="System.IO.IOException"></exception>
        public override void CheckIntegrity()
        {
            if (version >= CompressingStoredFieldsWriter.VERSION_CHECKSUM)
            {
                CodecUtil.ChecksumEntireFile(fieldsStream);
            }
        }
    }
}