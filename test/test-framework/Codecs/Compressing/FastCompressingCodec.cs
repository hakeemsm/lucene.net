using Lucene.Net.Codecs.Compressing.TestFramework;
using Lucene.Net.Codecs.Lucene42;
using Lucene.Net.Util.Packed;

namespace Lucene.Net.Codecs.Compressing.TestFramework
{
	/// <summary>
	/// CompressionCodec that uses
	/// <see cref="CompressionMode.FAST">CompressionMode.FAST</see>
	/// 
	/// </summary>
	public class FastCompressingCodec : CompressingCodec
	{
		/// <summary>Constructor that allows to configure the chunk size.</summary>
		/// <remarks>Constructor that allows to configure the chunk size.</remarks>
		public FastCompressingCodec(int chunkSize, bool withSegmentSuffix) : base("FastCompressingStoredFields"
			, withSegmentSuffix ? "FastCompressingStoredFields" : string.Empty, CompressionMode
			.FAST, chunkSize)
		{
		}

		/// <summary>Default constructor.</summary>
		/// <remarks>Default constructor.</remarks>
		public FastCompressingCodec() : this(1 << 14, false)
		{
		}

		public override NormsFormat NormsFormat
		{
		    get { return new Lucene42NormsFormat(PackedInts.FAST); }
		}
	}
}
