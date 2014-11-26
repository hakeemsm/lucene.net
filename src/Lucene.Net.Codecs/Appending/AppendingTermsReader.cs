/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Codecs;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Sharpen;

namespace Lucene.Net.Codecs.Appending
{
	/// <summary>Reads append-only terms from AppendingTermsWriter.</summary>
	/// <remarks>Reads append-only terms from AppendingTermsWriter.</remarks>
	/// <lucene.experimental></lucene.experimental>
	[System.ObsoleteAttribute(@"Only for reading old Appending segments")]
	public class AppendingTermsReader : BlockTreeTermsReader
	{
		internal static readonly string APPENDING_TERMS_CODEC_NAME = "APPENDING_TERMS_DICT";

		internal static readonly string APPENDING_TERMS_INDEX_CODEC_NAME = "APPENDING_TERMS_INDEX";

		/// <exception cref="System.IO.IOException"></exception>
		public AppendingTermsReader(Directory dir, FieldInfos fieldInfos, SegmentInfo info
			, PostingsReaderBase postingsReader, IOContext ioContext, string segmentSuffix, 
			int indexDivisor) : base(dir, fieldInfos, info, postingsReader, ioContext, segmentSuffix
			, indexDivisor)
		{
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected override int ReadHeader(IndexInput input)
		{
			return CodecUtil.CheckHeader(input, APPENDING_TERMS_CODEC_NAME, BlockTreeTermsWriter
				.VERSION_START, BlockTreeTermsWriter.VERSION_CURRENT);
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected override int ReadIndexHeader(IndexInput input)
		{
			return CodecUtil.CheckHeader(input, APPENDING_TERMS_INDEX_CODEC_NAME, BlockTreeTermsWriter
				.VERSION_START, BlockTreeTermsWriter.VERSION_CURRENT);
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected override void SeekDir(IndexInput input, long dirOffset)
		{
			input.Seek(input.Length() - long.SIZE / 8);
			long offset = input.ReadLong();
			input.Seek(offset);
		}
	}
}
