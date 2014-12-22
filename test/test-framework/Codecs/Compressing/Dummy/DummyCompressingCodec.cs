using Lucene.Net.Codecs.Compressing.TestFramework;
using Lucene.Net.Store;
using Lucene.Net.Util;

namespace Lucene.Net.Codecs.Compressing.Dummy.TestFramework
{
	/// <summary>CompressionCodec that does not compress data, useful for testing.</summary>
	/// <remarks>CompressionCodec that does not compress data, useful for testing.</remarks>
	public class DummyCompressingCodec : CompressingCodec
	{
		private sealed class _CompressionMode_36 : CompressionMode
		{
			public _CompressionMode_36()
			{
			}

			// In its own package to make sure the oal.codecs.compressing classes are
			// visible enough to let people write their own CompressionMode
			public override Compressor NewCompressor()
			{
				return DUMMY_COMPRESSOR;
			}

			public override Decompressor NewDecompressor()
			{
				return DUMMY_DECOMPRESSOR;
			}

			public override string ToString()
			{
				return "DUMMY";
			}
		}

		public static readonly CompressionMode DUMMY = new _CompressionMode_36();

		private sealed class _Decompressor_55 : Decompressor
		{
		    /// <exception cref="System.IO.IOException"></exception>
			public override void Decompress(DataInput @in, int originalLength, int offset, int length, BytesRef bytes)
			{
				 
				//assert offset + length <= originalLength;
				if (bytes.bytes.Length < originalLength)
				{
					bytes.bytes = new sbyte[ArrayUtil.Oversize(originalLength, 1)];
				}
				@in.ReadBytes(bytes.bytes, 0, offset + length);
				bytes.offset = offset;
				bytes.length = length;
			}

			public override object Clone()
			{
				return this;
			}
		}

		private static readonly Decompressor DUMMY_DECOMPRESSOR = new _Decompressor_55();

		private sealed class _Compressor_78 : Compressor
		{
		    /// <exception cref="System.IO.IOException"></exception>
			public override void Compress(sbyte[] bytes, int off, int len, DataOutput @out)
			{
				@out.WriteBytes(bytes, off, len);
			}
		}

		private static readonly Compressor DUMMY_COMPRESSOR = new _Compressor_78();

		/// <summary>Constructor that allows to configure the chunk size.</summary>
		/// <remarks>Constructor that allows to configure the chunk size.</remarks>
		public DummyCompressingCodec(int chunkSize, bool withSegmentSuffix) : base("DummyCompressingStoredFields"
			, withSegmentSuffix ? "DummyCompressingStoredFields" : string.Empty, DUMMY, chunkSize
			)
		{
		}

		/// <summary>Default constructor.</summary>
		/// <remarks>Default constructor.</remarks>
		public DummyCompressingCodec() : this(1 << 14, false)
		{
		}
	}
}
