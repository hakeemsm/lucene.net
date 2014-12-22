using Lucene.Net.Codecs.Lucene42;
using Lucene.Net.Util.Packed;

namespace Lucene.Net.Codecs.Compressing.TestFramework
{
	/// <summary>
	/// CompressionCodec that uses
	/// <see cref="CompressionMode.FAST_DECOMPRESSION">CompressionMode.FAST_DECOMPRESSION
	/// 	</see>
	/// 
	/// </summary>
	public class FastDecompressionCompressingCodec : CompressingCodec
	{
		/// <summary>Constructor that allows to configure the chunk size.</summary>
		/// <remarks>Constructor that allows to configure the chunk size.</remarks>
		public FastDecompressionCompressingCodec(int chunkSize, bool withSegmentSuffix) : 
			base("FastDecompressionCompressingStoredFields", withSegmentSuffix ? "FastDecompressionCompressingStoredFields"
			 : string.Empty, CompressionMode.FAST, chunkSize)
		{
		}

		/// <summary>Default constructor.</summary>
		/// <remarks>Default constructor.</remarks>
		public FastDecompressionCompressingCodec() : this(1 << 14, false)
		{
		}

		public override NormsFormat NormsFormat
		{
		    get { return new Lucene42NormsFormat(PackedInts.DEFAULT); }
		}
	}
}
