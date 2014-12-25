using Lucene.Net.Util;
using System;

namespace Lucene.Net.Store
{
    public class ByteArrayDataOutput : DataOutput
    {
        private byte[] bytes;
        private sbyte[] sbytes;

        private int pos;
        private int limit;

        public ByteArrayDataOutput(byte[] bytes)
        {
            Reset(bytes);
        }

        public ByteArrayDataOutput(byte[] bytes, int offset, int len)
        {
            Reset(bytes, offset, len);
        }

        public ByteArrayDataOutput()
        {
            Reset((byte[])(Array)BytesRef.EMPTY_BYTES);
        }

        public void Reset(byte[] inputBytes)
        {
            Reset(inputBytes, 0, inputBytes.Length);
        }

        public void Reset(byte[] inputBytes, int offset, int len)
        {
            this.bytes = inputBytes;
            pos = offset;
            limit = offset + len;
        }

        public void Reset(sbyte[] inputBytes)
        {
            Reset(inputBytes, 0, inputBytes.Length);
        }

        public void Reset(sbyte[] inputBytes, int offset, int len)
        {
            this.sbytes = inputBytes;
            pos = offset;
            limit = offset + len;
        }

        public int Position
        {
            get { return pos; }
        }

        public override void WriteByte(byte b)
        {
            //assert pos < limit;
            bytes[pos++] = b;
        }

        public override void WriteBytes(byte[] b, int offset, int length)
        {
            //assert pos + length <= limit;
            Array.Copy(b, offset, bytes, pos, length);
            pos += length;
        }
    }
}
