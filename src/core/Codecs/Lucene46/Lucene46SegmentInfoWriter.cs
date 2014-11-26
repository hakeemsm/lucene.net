using System;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;

namespace Lucene.Net.Codecs.Lucene46
{
	/// <summary>
	/// Lucene 4.0 implementation of
	/// <see cref="Lucene.Net.Codecs.SegmentInfoWriter">Lucene.Net.Codecs.SegmentInfoWriter
	/// 	</see>
	/// .
	/// </summary>
	/// <seealso cref="Lucene46SegmentInfoFormat">Lucene46SegmentInfoFormat</seealso>
	/// <lucene.experimental></lucene.experimental>
	public class Lucene46SegmentInfoWriter : SegmentInfoWriter
	{
	    /// <summary>Save a single segment's info.</summary>
		/// <remarks>Save a single segment's info.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public override void Write(Directory dir, SegmentInfo si, FieldInfos fis, IOContext
			 ioContext)
		{
			string fileName = IndexFileNames.SegmentFileName(si.name, string.Empty, Lucene46SegmentInfoFormat
				.SI_EXTENSION);
			si.AddFile(fileName);
			IndexOutput output = dir.CreateOutput(fileName, ioContext);
			bool success = false;
			try
			{
				CodecUtil.WriteHeader(output, Lucene46SegmentInfoFormat.CODEC_NAME, Lucene46SegmentInfoFormat
					.VERSION_CURRENT);
				// Write the Lucene version that created this segment, since 3.1
				output.WriteString(si.Version);
				output.WriteInt(si.DocCount);
				output.WriteByte(unchecked((byte)(si.UseCompoundFile ? SegmentInfo.YES : SegmentInfo
					.NO)));
				output.WriteStringStringMap(si.Diagnostics);
				output.WriteStringSet(si.Files);
				CodecUtil.WriteFooter(output);
				success = true;
			}
			finally
			{
				if (!success)
				{
					IOUtils.CloseWhileHandlingException((IDisposable)output);
					si.dir.DeleteFile(fileName);
				}
				else
				{
					output.Dispose();
				}
			}
		}
	}
}
