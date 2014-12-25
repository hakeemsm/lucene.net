using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;

namespace Lucene.Net.Codecs.Simpletext
{
    internal class SimpleTextDocValuesReader : DocValuesProducer
    {
        internal class OneField
        {
            internal long dataStartFilePointer;

            internal string pattern;

            internal string ordPattern;

            internal int maxLength;

            internal bool fixedLength;

            internal long minValue;

            internal long numValues;
        }

        internal readonly int maxDoc;

        internal readonly IndexInput data;

        internal readonly BytesRef scratch = new BytesRef();

        internal readonly IDictionary<string, SimpleTextDocValuesReader.OneField> fields =
            new Dictionary<string, SimpleTextDocValuesReader.OneField>();

        /// <exception cref="System.IO.IOException"></exception>
        public SimpleTextDocValuesReader(SegmentReadState state, string ext)
        {
            // System.out.println("dir=" + state.directory + " seg=" + state.segmentInfo.name + " file=" + IndexFileNames.segmentFileName(state.segmentInfo.name, state.segmentSuffix, ext));
            data = state.directory.OpenInput(IndexFileNames.SegmentFileName(state.segmentInfo
                .name, state.segmentSuffix, ext), state.context);
            maxDoc = state.segmentInfo.DocCount;
            while (true)
            {
                ReadLine();
                //System.out.println("READ field=" + scratch.utf8ToString());
                if (scratch.Equals(SimpleTextDocValuesWriter.END))
                {
                    break;
                }

                //assert startsWith(FIELD) : scratch.utf8ToString();
                string fieldName = StripPrefix(SimpleTextDocValuesWriter.FIELD);
                //System.out.println("  field=" + fieldName);
                var field = new OneField();
                fields[fieldName] = field;
                ReadLine();

                //assert startsWith(TYPE) : scratch.utf8ToString();
                FieldInfo.DocValuesType dvType = (FieldInfo.DocValuesType)Enum.Parse(typeof(FieldInfo.DocValuesType), StripPrefix(SimpleTextDocValuesWriter.TYPE));

                //assert dvType != null;
                if (dvType == FieldInfo.DocValuesType.NUMERIC)
                {
                    ReadLine();

                    //assert startsWith(MINVALUE): "got " + scratch.utf8ToString() + " field=" + fieldName + " ext=" + ext;
                    field.minValue = long.Parse(StripPrefix(SimpleTextDocValuesWriter.MINVALUE));
                    ReadLine();

                    //assert startsWith(PATTERN);
                    field.pattern = StripPrefix(SimpleTextDocValuesWriter.PATTERN);
                    field.dataStartFilePointer = data.FilePointer;
                    data.Seek(data.FilePointer + (1 + field.pattern.Length + 2) * maxDoc);
                }
                else
                {
                    if (dvType == FieldInfo.DocValuesType.BINARY)
                    {
                        ReadLine();
                        //HM:revisit 
                        //assert startsWith(MAXLENGTH);
                        field.maxLength = System.Convert.ToInt32(StripPrefix(SimpleTextDocValuesWriter.MAXLENGTH
                            ));
                        ReadLine();
                        //HM:revisit 
                        //assert startsWith(PATTERN);
                        field.pattern = StripPrefix(SimpleTextDocValuesWriter.PATTERN);
                        field.dataStartFilePointer = data.FilePointer;
                        data.Seek(data.FilePointer + (9 + field.pattern.Length + field.maxLength + 2
                            ) * maxDoc);
                    }
                    else
                    {
                        if (dvType == FieldInfo.DocValuesType.SORTED || dvType == FieldInfo.DocValuesType
                            .SORTED_SET)
                        {
                            ReadLine();
                            //HM:revisit 
                            //assert startsWith(NUMVALUES);
                            field.numValues = long.Parse(StripPrefix(SimpleTextDocValuesWriter.NUMVALUES));
                            ReadLine();
                            //HM:revisit 
                            //assert startsWith(MAXLENGTH);
                            field.maxLength = System.Convert.ToInt32(StripPrefix(SimpleTextDocValuesWriter.MAXLENGTH
                                ));
                            ReadLine();
                            //HM:revisit 
                            //assert startsWith(PATTERN);
                            field.pattern = StripPrefix(SimpleTextDocValuesWriter.PATTERN);
                            ReadLine();
                            //HM:revisit 
                            //assert startsWith(ORDPATTERN);
                            field.ordPattern = StripPrefix(SimpleTextDocValuesWriter.ORDPATTERN);
                            field.dataStartFilePointer = data.FilePointer;
                            data.Seek(data.FilePointer + (9 + field.pattern.Length + field.maxLength) *
                                field.numValues + (1 + field.ordPattern.Length) * maxDoc);
                        }
                        else
                        {
                            throw new Exception();
                        }
                    }
                }
            }
        }

        // We should only be called from above if at least one
        // field has DVs:
        //HM:revisit 
        //assert !fields.isEmpty();
        /// <exception cref="System.IO.IOException"></exception>
        public override NumericDocValues GetNumeric(FieldInfo fieldInfo)
        {
            OneField field = fields[fieldInfo.name];

            //assert field != null;
            // SegmentCoreReaders already verifies this field is
            // valid:
            //HM:revisit 
            //assert field != null: "field=" + fieldInfo.name + " fields=" + fields;
            IndexInput @in = ((IndexInput)data.Clone());
            BytesRef scratch = new BytesRef();
            decimal decoder = decimal.Parse(field.pattern, CultureInfo.CurrentCulture);
            //DecimalFormat decoder = new DecimalFormat(field.pattern, new DecimalFormatSymbols
            //    (CultureInfo.ROOT));
            //decoder.SetParseBigDecimal(true);
            return new AnonymousNumericDocValues(this, @in, field, scratch, decoder);
        }

        private sealed class AnonymousNumericDocValues : NumericDocValues
        {
            public AnonymousNumericDocValues(SimpleTextDocValuesReader _enclosing, IndexInput @in
                , SimpleTextDocValuesReader.OneField field, BytesRef scratch, decimal decoder
                )
            {
                this._enclosing = _enclosing;
                this.@in = @in;
                this.field = field;
                this.scratch = scratch;
                this.decoder = decoder;
            }

            public override long Get(int docID)
            {

                //System.out.println(Thread.currentThread().getName() + ": get docID=" + docID + " in=" + in);
                if (docID < 0 || docID >= this._enclosing.maxDoc)
                {
                    throw new IndexOutOfRangeException("docID must be 0 .. " + (this._enclosing.maxDoc
                         - 1) + "; got " + docID);
                }
                @in.Seek(field.dataStartFilePointer + (1 + field.pattern.Length + 2) * docID);
                SimpleTextUtil.ReadLine(@in, scratch);
                //System.out.println("parsing delta: " + scratch.utf8ToString());
                BigInteger bd;
                try
                {
                    bd = new BigInteger(decimal.Parse(scratch.Utf8ToString()));
                }
                catch (FormatException pe)
                {
                    var e = new CorruptIndexException("failed to parse BigDecimal value (resource=" + @in + ")", pe);

                    throw e;
                }
                SimpleTextUtil.ReadLine(@in, scratch);
                // read the line telling us if its real or not
                return (long)BigInteger.Add(new BigInteger(field.minValue), bd);

            }

            private readonly SimpleTextDocValuesReader _enclosing;

            private readonly IndexInput @in;

            private readonly SimpleTextDocValuesReader.OneField field;

            private readonly BytesRef scratch;

            private readonly decimal decoder;
        }

        /// <exception cref="System.IO.IOException"></exception>
        private IBits GetNumericDocsWithField(FieldInfo fieldInfo)
        {
            OneField field = fields[fieldInfo.name];
            IndexInput indexInput = ((IndexInput)data.Clone());
            BytesRef scratch = new BytesRef();
            return new AnonIBitsImpl(this, indexInput, field, scratch);
        }

        private sealed class AnonIBitsImpl : IBits
        {
            public AnonIBitsImpl(SimpleTextDocValuesReader enclosing, IndexInput indexInput, OneField field, BytesRef scratch)
            {
                this.enclosingInstance = enclosing;
                this.indexInput = indexInput;
                this.field = field;
                this.scratch = scratch;
            }

            public bool this[int index]
            {
                get
                {

                    indexInput.Seek(field.dataStartFilePointer + (1 + field.pattern.Length + 2) * index);
                    SimpleTextUtil.ReadLine(indexInput, scratch);
                    // data
                    SimpleTextUtil.ReadLine(indexInput, scratch);
                    // 'T' or 'F'
                    return scratch.bytes[scratch.offset] == unchecked((byte)'T');

                }
            }

            public int Length
            {
                get { return this.enclosingInstance.maxDoc; }
            }

            private readonly SimpleTextDocValuesReader enclosingInstance;

            private readonly IndexInput indexInput;

            private readonly OneField field;

            private readonly BytesRef scratch;
        }


        public override BinaryDocValues GetBinary(FieldInfo fieldInfo)
        {
            OneField field = fields[fieldInfo.name];
            // SegmentCoreReaders already verifies this field is
            // valid:
            //HM:revisit 
            //assert field != null;
            IndexInput @in = ((IndexInput)data.Clone());
            BytesRef scratch = new BytesRef();
            decimal decoder = decimal.Parse(field.pattern, CultureInfo.CurrentCulture);

            return new AnonymousBinaryDocValues(this, @in, field, scratch, decoder);
        }

        private sealed class AnonymousBinaryDocValues : BinaryDocValues
        {
            public AnonymousBinaryDocValues(SimpleTextDocValuesReader enclosing, IndexInput indexInput,
                SimpleTextDocValuesReader.OneField field, BytesRef scratch, decimal decoder
                )
            {
                this.enclosingInstance = enclosing;
                this.idxInput = indexInput;
                this.field = field;
                this.scratch = scratch;
                this.decoder = decoder;
            }

            public override void Get(int docID, BytesRef result)
            {

                if (docID < 0 || docID >= this.enclosingInstance.maxDoc)
                {
                    throw new IndexOutOfRangeException("docID must be 0 .. " + (this.enclosingInstance.maxDoc
                         - 1) + "; got " + docID);
                }
                idxInput.Seek(field.dataStartFilePointer + (9 + field.pattern.Length + field.maxLength
                     + 2) * docID);
                SimpleTextUtil.ReadLine(idxInput, scratch);

                //assert StringHelper.startsWith(scratch, LENGTH);
                int len;
                try
                {
                    byte[] bigBytes = Array.ConvertAll(scratch.bytes, Convert.ToByte);
                    string s = Encoding.UTF8.GetString(bigBytes, scratch.offset + SimpleTextDocValuesWriter
                        .LENGTH.length, scratch.length - SimpleTextDocValuesWriter.LENGTH.length);
                    len = int.Parse(s);
                }
                catch (FormatException pe)
                {
                    var e = new CorruptIndexException("failed to parse int length (resource="+ idxInput + ")", pe);

                    throw e;
                }
                result.bytes = new sbyte[len];
                result.offset = 0;
                result.length = len;
                idxInput.ReadBytes(result.bytes, 0, len);

            }

            private readonly SimpleTextDocValuesReader enclosingInstance;

            private readonly IndexInput idxInput;

            private readonly OneField field;

            private readonly BytesRef scratch;

            private readonly decimal decoder;
        }


        private IBits GetBinaryDocsWithField(FieldInfo fieldInfo)
        {
            SimpleTextDocValuesReader.OneField field = fields.Get(fieldInfo.name);
            IndexInput @in = ((IndexInput)data.Clone());
            BytesRef scratch = new BytesRef();
            DecimalFormat decoder = new DecimalFormat(field.pattern, new DecimalFormatSymbols
                (CultureInfo.ROOT));
            return new _Bits_287(this, @in, field, scratch, decoder);
        }

        private sealed class _Bits_287 : Bits
        {
            public _Bits_287(SimpleTextDocValuesReader _enclosing, IndexInput @in, SimpleTextDocValuesReader.OneField
                 field, BytesRef scratch, DecimalFormat decoder)
            {
                this._enclosing = _enclosing;
                this.@in = @in;
                this.field = field;
                this.scratch = scratch;
                this.decoder = decoder;
            }

            public override bool Get(int index)
            {
                try
                {
                    @in.Seek(field.dataStartFilePointer + (9 + field.pattern.Length + field.maxLength
                         + 2) * index);
                    SimpleTextUtil.ReadLine(@in, scratch);
                    //HM:revisit 
                    //assert StringHelper.startsWith(scratch, LENGTH);
                    int len;
                    try
                    {
                        len = decoder.Parse(new string(scratch.bytes, scratch.offset + SimpleTextDocValuesWriter
                            .LENGTH.length, scratch.length - SimpleTextDocValuesWriter.LENGTH.length, StandardCharsets
                            .UTF_8));
                    }
                    catch (ParseException pe)
                    {
                        CorruptIndexException e = new CorruptIndexException("failed to parse int length (resource="
                             + @in + ")");
                        Sharpen.Extensions.InitCause(e, pe);
                        throw e;
                    }
                    // skip past bytes
                    byte[] bytes = new byte[len];
                    @in.ReadBytes(bytes, 0, len);
                    SimpleTextUtil.ReadLine(@in, scratch);
                    // newline
                    SimpleTextUtil.ReadLine(@in, scratch);
                    // 'T' or 'F'
                    return scratch.bytes[scratch.offset] == unchecked((byte)'T');
                }
                catch (IOException ioe)
                {
                    throw new RuntimeException(ioe);
                }
            }

            public override int Length()
            {
                return this._enclosing.maxDoc;
            }

            private readonly SimpleTextDocValuesReader _enclosing;

            private readonly IndexInput @in;

            private readonly SimpleTextDocValuesReader.OneField field;

            private readonly BytesRef scratch;

            private readonly DecimalFormat decoder;
        }

        /// <exception cref="System.IO.IOException"></exception>
        public override SortedDocValues GetSorted(FieldInfo fieldInfo)
        {
            SimpleTextDocValuesReader.OneField field = fields.Get(fieldInfo.name);
            // SegmentCoreReaders already verifies this field is
            // valid:
            //HM:revisit 
            //assert field != null;
            IndexInput @in = ((IndexInput)data.Clone());
            BytesRef scratch = new BytesRef();
            DecimalFormat decoder = new DecimalFormat(field.pattern, new DecimalFormatSymbols
                (CultureInfo.ROOT));
            DecimalFormat ordDecoder = new DecimalFormat(field.ordPattern, new DecimalFormatSymbols
                (CultureInfo.ROOT));
            return new _SortedDocValues_337(this, @in, field, scratch, ordDecoder, decoder);
        }

        private sealed class _SortedDocValues_337 : SortedDocValues
        {
            public _SortedDocValues_337(SimpleTextDocValuesReader _enclosing, IndexInput @in,
                SimpleTextDocValuesReader.OneField field, BytesRef scratch, DecimalFormat ordDecoder
                , DecimalFormat decoder)
            {
                this._enclosing = _enclosing;
                this.@in = @in;
                this.field = field;
                this.scratch = scratch;
                this.ordDecoder = ordDecoder;
                this.decoder = decoder;
            }

            public override int GetOrd(int docID)
            {
                if (docID < 0 || docID >= this._enclosing.maxDoc)
                {
                    throw new IndexOutOfRangeException("docID must be 0 .. " + (this._enclosing.maxDoc
                         - 1) + "; got " + docID);
                }
                try
                {
                    @in.Seek(field.dataStartFilePointer + field.numValues * (9 + field.pattern.Length
                         + field.maxLength) + docID * (1 + field.ordPattern.Length));
                    SimpleTextUtil.ReadLine(@in, scratch);
                    try
                    {
                        return (int)ordDecoder.Parse(scratch.Utf8ToString()) - 1;
                    }
                    catch (ParseException pe)
                    {
                        CorruptIndexException e = new CorruptIndexException("failed to parse ord (resource="
                             + @in + ")");
                        Sharpen.Extensions.InitCause(e, pe);
                        throw e;
                    }
                }
                catch (IOException ioe)
                {
                    throw new RuntimeException(ioe);
                }
            }

            public override void LookupOrd(int ord, BytesRef result)
            {
                try
                {
                    if (ord < 0 || ord >= field.numValues)
                    {
                        throw new IndexOutOfRangeException("ord must be 0 .. " + (field.numValues - 1) +
                            "; got " + ord);
                    }
                    @in.Seek(field.dataStartFilePointer + ord * (9 + field.pattern.Length + field.maxLength
                        ));
                    SimpleTextUtil.ReadLine(@in, scratch);
                    //HM:revisit 
                    //assert StringHelper.startsWith(scratch, LENGTH): "got " + scratch.utf8ToString() + " in=" + in;
                    int len;
                    try
                    {
                        len = decoder.Parse(new string(scratch.bytes, scratch.offset + SimpleTextDocValuesWriter
                            .LENGTH.length, scratch.length - SimpleTextDocValuesWriter.LENGTH.length, StandardCharsets
                            .UTF_8));
                    }
                    catch (ParseException pe)
                    {
                        CorruptIndexException e = new CorruptIndexException("failed to parse int length (resource="
                             + @in + ")");
                        Sharpen.Extensions.InitCause(e, pe);
                        throw e;
                    }
                    result.bytes = new byte[len];
                    result.offset = 0;
                    result.length = len;
                    @in.ReadBytes(result.bytes, 0, len);
                }
                catch (IOException ioe)
                {
                    throw new RuntimeException(ioe);
                }
            }

            public override int GetValueCount()
            {
                return (int)field.numValues;
            }

            private readonly SimpleTextDocValuesReader _enclosing;

            private readonly IndexInput @in;

            private readonly SimpleTextDocValuesReader.OneField field;

            private readonly BytesRef scratch;

            private readonly DecimalFormat ordDecoder;

            private readonly DecimalFormat decoder;
        }

        /// <exception cref="System.IO.IOException"></exception>
        public override SortedSetDocValues GetSortedSet(FieldInfo fieldInfo)
        {
            SimpleTextDocValuesReader.OneField field = fields.Get(fieldInfo.name);
            // SegmentCoreReaders already verifies this field is
            // valid:
            //HM:revisit 
            //assert field != null;
            IndexInput @in = ((IndexInput)data.Clone());
            BytesRef scratch = new BytesRef();
            DecimalFormat decoder = new DecimalFormat(field.pattern, new DecimalFormatSymbols
                (CultureInfo.ROOT));
            return new _SortedSetDocValues_407(this, @in, field, scratch, decoder);
        }

        private sealed class _SortedSetDocValues_407 : SortedSetDocValues
        {
            public _SortedSetDocValues_407(SimpleTextDocValuesReader _enclosing, IndexInput @in
                , SimpleTextDocValuesReader.OneField field, BytesRef scratch, DecimalFormat decoder
                )
            {
                this._enclosing = _enclosing;
                this.@in = @in;
                this.field = field;
                this.scratch = scratch;
                this.decoder = decoder;
                this.currentOrds = new string[0];
                this.currentIndex = 0;
            }

            internal string[] currentOrds;

            internal int currentIndex;

            public override long NextOrd()
            {
                if (this.currentIndex == this.currentOrds.Length)
                {
                    return SortedSetDocValues.NO_MORE_ORDS;
                }
                else
                {
                    return long.Parse(this.currentOrds[this.currentIndex++]);
                }
            }

            public override void SetDocument(int docID)
            {
                if (docID < 0 || docID >= this._enclosing.maxDoc)
                {
                    throw new IndexOutOfRangeException("docID must be 0 .. " + (this._enclosing.maxDoc
                         - 1) + "; got " + docID);
                }
                try
                {
                    @in.Seek(field.dataStartFilePointer + field.numValues * (9 + field.pattern.Length
                         + field.maxLength) + docID * (1 + field.ordPattern.Length));
                    SimpleTextUtil.ReadLine(@in, scratch);
                    string ordList = scratch.Utf8ToString().Trim();
                    if (ordList.IsEmpty())
                    {
                        this.currentOrds = new string[0];
                    }
                    else
                    {
                        this.currentOrds = ordList.Split(",");
                    }
                    this.currentIndex = 0;
                }
                catch (IOException ioe)
                {
                    throw new RuntimeException(ioe);
                }
            }

            public override void LookupOrd(long ord, BytesRef result)
            {
                try
                {
                    if (ord < 0 || ord >= field.numValues)
                    {
                        throw new IndexOutOfRangeException("ord must be 0 .. " + (field.numValues - 1) +
                            "; got " + ord);
                    }
                    @in.Seek(field.dataStartFilePointer + ord * (9 + field.pattern.Length + field.maxLength
                        ));
                    SimpleTextUtil.ReadLine(@in, scratch);
                    //HM:revisit 
                    //assert StringHelper.startsWith(scratch, LENGTH): "got " + scratch.utf8ToString() + " in=" + in;
                    int len;
                    try
                    {
                        len = decoder.Parse(new string(scratch.bytes, scratch.offset + SimpleTextDocValuesWriter
                            .LENGTH.length, scratch.length - SimpleTextDocValuesWriter.LENGTH.length, StandardCharsets
                            .UTF_8));
                    }
                    catch (ParseException pe)
                    {
                        CorruptIndexException e = new CorruptIndexException("failed to parse int length (resource="
                             + @in + ")");
                        Sharpen.Extensions.InitCause(e, pe);
                        throw e;
                    }
                    result.bytes = new byte[len];
                    result.offset = 0;
                    result.length = len;
                    @in.ReadBytes(result.bytes, 0, len);
                }
                catch (IOException ioe)
                {
                    throw new RuntimeException(ioe);
                }
            }

            public override long GetValueCount()
            {
                return field.numValues;
            }

            private readonly SimpleTextDocValuesReader _enclosing;

            private readonly IndexInput @in;

            private readonly SimpleTextDocValuesReader.OneField field;

            private readonly BytesRef scratch;

            private readonly DecimalFormat decoder;
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
                        return GetBinaryDocsWithField(field);
                    }

                case FieldInfo.DocValuesType.NUMERIC:
                    {
                        return GetNumericDocsWithField(field);
                    }

                default:
                    {
                        throw new Exception();
                    }
            }
        }

        /// <exception cref="System.IO.IOException"></exception>
        public override void Close()
        {
            data.Close();
        }

        /// <summary>Used only in ctor:</summary>
        /// <exception cref="System.IO.IOException"></exception>
        private void ReadLine()
        {
            SimpleTextUtil.ReadLine(data, scratch);
        }

        //System.out.println("line: " + scratch.utf8ToString());
        /// <summary>Used only in ctor:</summary>
        private bool StartsWith(BytesRef prefix)
        {
            return StringHelper.StartsWith(scratch, prefix);
        }

        /// <summary>Used only in ctor:</summary>
        /// <exception cref="System.IO.IOException"></exception>
        private string StripPrefix(BytesRef prefix)
        {
            return new string(scratch.bytes, scratch.offset + prefix.length, scratch.length -
                 prefix.length, StandardCharsets.UTF_8);
        }

        public override long RamBytesUsed()
        {
            return 0;
        }

        /// <exception cref="System.IO.IOException"></exception>
        public override void CheckIntegrity()
        {
            BytesRef scratch = new BytesRef();
            IndexInput clone = ((IndexInput)data.Clone());
            clone.Seek(0);
            ChecksumIndexInput input = new BufferedChecksumIndexInput(clone);
            while (true)
            {
                SimpleTextUtil.ReadLine(input, scratch);
                if (scratch.Equals(SimpleTextDocValuesWriter.END))
                {
                    SimpleTextUtil.CheckFooter(input);
                    break;
                }
            }
        }
    }
}
