using Lucene.Net.Codecs.Lucene42;
using Lucene.Net.Util.Packed;

namespace Lucene.Net.Codecs.Compressing.TestFramework
{
	/// <summary>
	/// CompressionCodec that uses
	/// <see cref="CompressionMode.HIGH_COMPRESSION">CompressionMode.HIGH_COMPRESSION</see>
	/// 
	/// </summary>
	public class HighCompressionCompressingCodec : CompressingCodec
	{
		/// <summary>Constructor that allows to configure the chunk size.</summary>
		/// <remarks>Constructor that allows to configure the chunk size.</remarks>
		public HighCompressionCompressingCodec(int chunkSize, bool withSegmentSuffix) : base
			("HighCompressionCompressingStoredFields", withSegmentSuffix ? "HighCompressionCompressingStoredFields"
			 : string.Empty, new CompressionMode.CompressionModeHigh(), chunkSize)
		{
		}

		/// <summary>Default constructor.</summary>
		/// <remarks>Default constructor.</remarks>
		public HighCompressionCompressingCodec() : this(1 << 14, false)
		{
		}

		public override NormsFormat NormsFormat
		{
		    get { return new Lucene42NormsFormat(PackedInts.COMPACT); }
		}
	}
}
