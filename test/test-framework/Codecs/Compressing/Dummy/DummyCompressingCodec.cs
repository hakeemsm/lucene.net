/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Codecs.Compressing;
using Org.Apache.Lucene.Store;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Codecs.Compressing.Dummy
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
				return Org.Apache.Lucene.Codecs.Compressing.Dummy.DummyCompressingCodec.DUMMY_COMPRESSOR;
			}

			public override Decompressor NewDecompressor()
			{
				return Org.Apache.Lucene.Codecs.Compressing.Dummy.DummyCompressingCodec.DUMMY_DECOMPRESSOR;
			}

			public override string ToString()
			{
				return "DUMMY";
			}
		}

		public static readonly CompressionMode DUMMY = new _CompressionMode_36();

		private sealed class _Decompressor_55 : Decompressor
		{
			public _Decompressor_55()
			{
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Decompress(DataInput @in, int originalLength, int offset, int
				 length, BytesRef bytes)
			{
				//HM:revisit 
				//assert offset + length <= originalLength;
				if (bytes.bytes.Length < originalLength)
				{
					bytes.bytes = new byte[ArrayUtil.Oversize(originalLength, 1)];
				}
				@in.ReadBytes(bytes.bytes, 0, offset + length);
				bytes.offset = offset;
				bytes.length = length;
			}

			public override Decompressor Clone()
			{
				return this;
			}
		}

		private static readonly Decompressor DUMMY_DECOMPRESSOR = new _Decompressor_55();

		private sealed class _Compressor_78 : Compressor
		{
			public _Compressor_78()
			{
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Compress(byte[] bytes, int off, int len, DataOutput @out)
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
