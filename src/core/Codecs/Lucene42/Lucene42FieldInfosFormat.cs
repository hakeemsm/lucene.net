using System;

namespace Lucene.Net.Codecs.Lucene42
{
	[Obsolete(@"Only for reading old 4.2-4.5 segments")]
    public class Lucene42FieldInfosFormat : FieldInfosFormat
    {
        private readonly FieldInfosReader reader = new Lucene42FieldInfosReader();

	    public override FieldInfosReader FieldInfosReader
        {
            get { return reader; }
        }

        public override FieldInfosWriter FieldInfosWriter
        {
            get { throw new NotSupportedException("this codec can only be used for reading"); }
        }

        /** Extension of field infos */
        internal const string EXTENSION = "fnm";

        // Codec header
        internal const string CODEC_NAME = "Lucene42FieldInfos";
        internal const int FORMAT_START = 0;
        internal const int FORMAT_CURRENT = FORMAT_START;

        // Field flags
        internal const sbyte IS_INDEXED = 0x1;
        internal const sbyte STORE_TERMVECTOR = 0x2;
        internal const sbyte STORE_OFFSETS_IN_POSTINGS = 0x4;
        internal const sbyte OMIT_NORMS = 0x10;
        internal const sbyte STORE_PAYLOADS = 0x20;
        internal const sbyte OMIT_TERM_FREQ_AND_POSITIONS = 0x40;
        internal const sbyte OMIT_POSITIONS = -128;
    }
}
