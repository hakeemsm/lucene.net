/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Codecs.Compressing;
using Org.Apache.Lucene.Codecs.Lucene42;
using Org.Apache.Lucene.Util.Packed;
using Sharpen;

namespace Org.Apache.Lucene.Codecs.Compressing
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
			 : string.Empty, CompressionMode.FAST_DECOMPRESSION, chunkSize)
		{
		}

		/// <summary>Default constructor.</summary>
		/// <remarks>Default constructor.</remarks>
		public FastDecompressionCompressingCodec() : this(1 << 14, false)
		{
		}

		public override Org.Apache.Lucene.Codecs.NormsFormat NormsFormat()
		{
			return new Lucene42NormsFormat(PackedInts.DEFAULT);
		}
	}
}
