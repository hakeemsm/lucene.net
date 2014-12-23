using System;
using Lucene.Net.Index;
using Lucene.Net.Util.Packed;

namespace Lucene.Net.Codecs.Lucene42
{
    public class Lucene42DocValuesFormat : DocValuesFormat
    {
		public const int MAX_BINARY_FIELD_LENGTH = (1 << 15) - 2;
        protected internal readonly float acceptableOverheadRatio;
		public Lucene42DocValuesFormat() : this(PackedInts.DEFAULT)
		{
		}
		public Lucene42DocValuesFormat(float acceptableOverheadRatio) : base("Lucene42")
        {
			this.acceptableOverheadRatio = acceptableOverheadRatio;
        }

        public override DocValuesConsumer FieldsConsumer(SegmentWriteState state)
        {
			throw new NotSupportedException("this codec can only be used for reading");
        }

        public override DocValuesProducer FieldsProducer(SegmentReadState state)
        {
            return new Lucene42DocValuesProducer(state, DATA_CODEC, DATA_EXTENSION, METADATA_CODEC, METADATA_EXTENSION);
        }

        protected const string DATA_CODEC = "Lucene42DocValuesData";
        protected const string DATA_EXTENSION = "dvd";
        protected const string METADATA_CODEC = "Lucene42DocValuesMetadata";
        protected const string METADATA_EXTENSION = "dvm";
    }
}
