using Lucene.Net.Codecs.Lucene45;
using Lucene.Net.Index;

namespace Lucene.Net.Codecs.Diskdv
{
	/// <summary>Norms format that keeps all norms on disk</summary>
	public sealed class DiskNormsFormat : NormsFormat
	{
		
		public override DocValuesConsumer NormsConsumer(SegmentWriteState state)
		{
			return new Lucene45DocValuesConsumer(state, DATA_CODEC, DATA_EXTENSION, META_CODEC
				, META_EXTENSION);
		}

		
		public override DocValuesProducer NormsProducer(SegmentReadState state)
		{
			return new DiskDocValuesProducer(state, DATA_CODEC, DATA_EXTENSION, META_CODEC, META_EXTENSION);
		}

		internal static readonly string DATA_CODEC = "DiskNormsData";

		internal static readonly string DATA_EXTENSION = "dnvd";

		internal static readonly string META_CODEC = "DiskNormsMetadata";

		internal static readonly string META_EXTENSION = "dnvm";
	}
}
