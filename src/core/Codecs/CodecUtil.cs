using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;

namespace Lucene.Net.Codecs
{
    public static class CodecUtil
    {
        public const int CODEC_MAGIC = 0x3fd76c17;

        /// <remarks>Constant to identify the start of a codec footer.</remarks>
        public const int FOOTER_MAGIC = ~CODEC_MAGIC;

        public static void WriteHeader(DataOutput output, string codec, int version)
        {
            BytesRef bytes = new BytesRef(codec);
            if (bytes.length != codec.Length || bytes.length >= 128)
            {
                throw new ArgumentException("codec must be simple ASCII, less than 128 characters in length [got " +
                                            codec + "]");
            }
            output.WriteInt(CODEC_MAGIC);
            output.WriteString(codec);
            output.WriteInt(version);
        }

        public static int HeaderLength(string codec)
        {
            return 9 + codec.Length;
        }

        public static int CheckHeader(DataInput input, string codec, int minVersion, int maxVersion)
        {
            // Safety to guard against reading a bogus string:
            int actualHeader = input.ReadInt();
            if (actualHeader != CODEC_MAGIC)
            {
                throw new CorruptIndexException("codec header mismatch: actual header=" + actualHeader +
                                                " vs expected header=" + CODEC_MAGIC + " (resource: " + input + ")");
            }
            return CheckHeaderNoMagic(input, codec, minVersion, maxVersion);
        }

        public static int CheckHeaderNoMagic(DataInput input, String codec, int minVersion, int maxVersion)
        {
            String actualCodec = input.ReadString();
            if (!actualCodec.Equals(codec))
            {
                throw new CorruptIndexException("codec mismatch: actual codec=" + actualCodec + " vs expected codec=" +
                                                codec + " (resource: " + input + ")");
            }

            int actualVersion = input.ReadInt();
            if (actualVersion < minVersion)
            {
                throw new IndexFormatTooOldException(input, actualVersion, minVersion, maxVersion);
            }
            if (actualVersion > maxVersion)
            {
                throw new IndexFormatTooNewException(input, actualVersion, minVersion, maxVersion);
            }

            return actualVersion;
        }

        /// <summary>
        /// Writes a codec footer, which records both a checksum
        /// algorithm ID and a checksum.
        /// </summary>
        public static void WriteFooter(IndexOutput output)
        {
            output.WriteInt(FOOTER_MAGIC);
            output.WriteInt(0);
            output.WriteLong(output.GetChecksum());
        }

        /// <summary>Computes the length of a codec footer.</summary>
        /// <remarks>Computes the length of a codec footer.</remarks>
        /// <returns>length of the entire codec footer.</returns>
        /// <seealso cref="WriteFooter(Lucene.Net.Store.IndexOutput)">WriteFooter(Lucene.Net.Store.IndexOutput)
        /// 	</seealso>
        public static int FooterLength()
        {
            return 16;
        }

        /// <summary>
        /// Validates the codec footer previously written by
        /// <see cref="WriteFooter(Lucene.Net.Store.IndexOutput)">WriteFooter(Lucene.Net.Store.IndexOutput)
        /// 	</see>
        /// .
        /// </summary>
        /// <returns>actual checksum value</returns>
        /// <exception cref="System.IO.IOException">
        /// if the footer is invalid, if the checksum does not match,
        /// or if
        /// <code>in</code>
        /// is not properly positioned before the footer
        /// at the end of the stream.
        /// </exception>
        public static long CheckFooter(ChecksumIndexInput input)
        {
            ValidateFooter(input);
            long actualChecksum = input.GetChecksum();
            long expectedChecksum = input.ReadLong();
            if (expectedChecksum != actualChecksum)
            {
                throw new CorruptIndexException("checksum failed (hardware problem?) : expected="
                                                + long.ToHexString(expectedChecksum) + " actual=" +
                                                long.ToHexString(actualChecksum
                                                    ) + " (resource=" + input + ")");
            }
            if (input.FilePointer != input.Length)
            {
                throw new CorruptIndexException("did not read all bytes from file: read " + input.FilePointer +
                                                " vs size " + input.Length + " (resource: " + input + ")");
            }
            return actualChecksum;
        }

        /// <summary>
        /// Returns (but does not validate) the checksum previously written by
        /// <see cref="CheckFooter(Lucene.Net.Store.ChecksumIndexInput)">CheckFooter(Lucene.Net.Store.ChecksumIndexInput)
        /// 	</see>
        /// .
        /// </summary>
        /// <returns>actual checksum value</returns>
        /// <exception cref="System.IO.IOException">if the footer is invalid</exception>
        public static long RetrieveChecksum(IndexInput input)
        {
            input.Seek(input.Length - FooterLength());
            ValidateFooter(input);
            return input.ReadLong();
        }

        /// <exception cref="System.IO.IOException"></exception>
        private static void ValidateFooter(IndexInput @in)
        {
            int magic = @in.ReadInt();
            if (magic != FOOTER_MAGIC)
            {
                throw new CorruptIndexException("codec footer mismatch: actual footer=" + magic +
                                                " vs expected footer=" + FOOTER_MAGIC + " (resource: " + @in + ")");
            }
            int algorithmID = @in.ReadInt();
            if (algorithmID != 0)
            {
                throw new CorruptIndexException("codec footer mismatch: unknown algorithmID: " +
                                                algorithmID);
            }
        }

        /// <summary>
        /// Checks that the stream is positioned at the end, and throws exception
        /// if it is not.
        /// </summary>
        /// <remarks>
        /// Checks that the stream is positioned at the end, and throws exception
        /// if it is not.
        /// </remarks>
        /// <exception cref="System.IO.IOException"></exception>

        [Obsolete(
            @"Use CheckFooter(Lucene.Net.Store.ChecksumIndexInput) instead, this should only used for files without checksums"
            )]
        public static void CheckEOF(IndexInput input)
        {
            if (input.FilePointer != input.Length)
            {
                throw new CorruptIndexException("did not read all bytes from file: read " + input.FilePointer +
                                                " vs size " + input.Length + " (resource: " + input + ")");
            }
        }

        /// <summary>
        /// Clones the provided input, reads all bytes from the file, and calls
        /// <see cref="CheckFooter(Lucene.Net.Store.ChecksumIndexInput)">CheckFooter(Lucene.Net.Store.ChecksumIndexInput)
        /// 	</see>
        /// 
        /// <p>
        /// Note that this method may be slow, as it must process the entire file.
        /// If you just need to extract the checksum value, call
        /// <see cref="RetrieveChecksum(Lucene.Net.Store.IndexInput)">RetrieveChecksum(Lucene.Net.Store.IndexInput)
        /// 	</see>
        /// .
        /// </summary>
        /// <exception cref="System.IO.IOException"></exception>
        public static long ChecksumEntireFile(IndexInput input)
        {
            IndexInput clone = ((IndexInput) input.Clone());
            clone.Seek(0);
            ChecksumIndexInput bufferedInput = new BufferedChecksumIndexInput(clone);
            //HM:revisit 
            //assert in.getFilePointer() == 0;
            bufferedInput.Seek(bufferedInput.Length - FooterLength());
            return CheckFooter(bufferedInput);
        }
    }
}
