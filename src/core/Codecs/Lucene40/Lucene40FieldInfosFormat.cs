using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Lucene40
{
    [Obsolete]
    public class Lucene40FieldInfosFormat : FieldInfosFormat
    {
        private readonly FieldInfosReader reader = new Lucene40FieldInfosReader();

        public Lucene40FieldInfosFormat()
        {
        }

        public override FieldInfosReader FieldInfosReader
        {
            get { return reader; }
        }

        public override FieldInfosWriter FieldInfosWriter
        {
            get { throw new NotSupportedException("this codec can only be used for reading"); }
        }

        /** Extension of field infos */
        public const String FIELD_INFOS_EXTENSION = "fnm";

        public const String CODEC_NAME = "Lucene40FieldInfos";
        internal const int FORMAT_START = 0;
        public const int FORMAT_CURRENT = FORMAT_START;

        public const sbyte IS_INDEXED = 0x1;
        public const sbyte STORE_TERMVECTOR = 0x2;
        public const sbyte STORE_OFFSETS_IN_POSTINGS = 0x4;
        public const sbyte OMIT_NORMS = 0x10;
        public const sbyte STORE_PAYLOADS = 0x20;
        public const sbyte OMIT_TERM_FREQ_AND_POSITIONS = 0x40;
        public const sbyte OMIT_POSITIONS = -128;
    }
}
