using System;
using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;

namespace Lucene.Net.Codecs.Lucene46
{
	/// <summary>
	/// Lucene 4.6 implementation of
	/// <see cref="Lucene.Net.Codecs.SegmentInfoReader">Lucene.Net.Codecs.SegmentInfoReader
	/// 	</see>
	/// .
	/// </summary>
	/// <seealso cref="Lucene46SegmentInfoFormat">Lucene46SegmentInfoFormat</seealso>
	/// <lucene.experimental></lucene.experimental>
	public class Lucene46SegmentInfoReader : SegmentInfoReader
	{
	    /// <exception cref="System.IO.IOException"></exception>
		public override SegmentInfo Read(Directory dir, string segment, IOContext context
			)
		{
			string fileName = IndexFileNames.SegmentFileName(segment, string.Empty, Lucene46SegmentInfoFormat
				.SI_EXTENSION);
			ChecksumIndexInput input = dir.OpenChecksumInput(fileName, context);
			bool success = false;
			try
			{
				int codecVersion = CodecUtil.CheckHeader(input, Lucene46SegmentInfoFormat.CODEC_NAME
					, Lucene46SegmentInfoFormat.VERSION_START, Lucene46SegmentInfoFormat.VERSION_CURRENT
					);
				string version = input.ReadString();
				int docCount = input.ReadInt();
				if (docCount < 0)
				{
					throw new CorruptIndexException("invalid docCount: " + docCount + " (resource=" +
						 input + ")");
				}
				bool isCompoundFile = input.ReadByte() == SegmentInfo.YES;
				IDictionary<string, string> diagnostics = input.ReadStringStringMap();
				ICollection<string> files = input.ReadStringSet();
				if (codecVersion >= Lucene46SegmentInfoFormat.VERSION_CHECKSUM)
				{
					CodecUtil.CheckFooter(input);
				}
				else
				{
					CodecUtil.CheckEOF(input);
				}
				SegmentInfo si = new SegmentInfo(dir, version, segment, docCount, isCompoundFile, null, diagnostics);
				si.SetFiles(files);
				success = true;
				return si;
			}
			finally
			{
				if (!success)
				{
					IOUtils.CloseWhileHandlingException((IDisposable)input);
				}
				else
				{
					input.Dispose();
				}
			}
		}
	}
}
