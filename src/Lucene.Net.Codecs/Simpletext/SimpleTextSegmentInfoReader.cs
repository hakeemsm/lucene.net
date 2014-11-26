/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Simpletext;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Codecs.Simpletext
{
	/// <summary>
	/// reads plaintext segments files
	/// <p>
	/// <b><font color="red">FOR RECREATIONAL USE ONLY</font></B>
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class SimpleTextSegmentInfoReader : SegmentInfoReader
	{
		/// <exception cref="System.IO.IOException"></exception>
		public override SegmentInfo Read(Directory directory, string segmentName, IOContext
			 context)
		{
			BytesRef scratch = new BytesRef();
			string segFileName = IndexFileNames.SegmentFileName(segmentName, string.Empty, SimpleTextSegmentInfoFormat
				.SI_EXTENSION);
			ChecksumIndexInput input = directory.OpenChecksumInput(segFileName, context);
			bool success = false;
			try
			{
				SimpleTextUtil.ReadLine(input, scratch);
				//HM:revisit 
				//assert StringHelper.startsWith(scratch, SI_VERSION);
				string version = ReadString(SimpleTextSegmentInfoWriter.SI_VERSION.length, scratch
					);
				SimpleTextUtil.ReadLine(input, scratch);
				//HM:revisit 
				//assert StringHelper.startsWith(scratch, SI_DOCCOUNT);
				int docCount = System.Convert.ToInt32(ReadString(SimpleTextSegmentInfoWriter.SI_DOCCOUNT
					.length, scratch));
				SimpleTextUtil.ReadLine(input, scratch);
				//HM:revisit 
				//assert StringHelper.startsWith(scratch, SI_USECOMPOUND);
				bool isCompoundFile = System.Boolean.Parse(ReadString(SimpleTextSegmentInfoWriter
					.SI_USECOMPOUND.length, scratch));
				SimpleTextUtil.ReadLine(input, scratch);
				//HM:revisit 
				//assert StringHelper.startsWith(scratch, SI_NUM_DIAG);
				int numDiag = System.Convert.ToInt32(ReadString(SimpleTextSegmentInfoWriter.SI_NUM_DIAG
					.length, scratch));
				IDictionary<string, string> diagnostics = new Dictionary<string, string>();
				for (int i = 0; i < numDiag; i++)
				{
					SimpleTextUtil.ReadLine(input, scratch);
					//HM:revisit 
					//assert StringHelper.startsWith(scratch, SI_DIAG_KEY);
					string key = ReadString(SimpleTextSegmentInfoWriter.SI_DIAG_KEY.length, scratch);
					SimpleTextUtil.ReadLine(input, scratch);
					//HM:revisit 
					//assert StringHelper.startsWith(scratch, SI_DIAG_VALUE);
					string value = ReadString(SimpleTextSegmentInfoWriter.SI_DIAG_VALUE.length, scratch
						);
					diagnostics.Put(key, value);
				}
				SimpleTextUtil.ReadLine(input, scratch);
				//HM:revisit 
				//assert StringHelper.startsWith(scratch, SI_NUM_FILES);
				int numFiles = System.Convert.ToInt32(ReadString(SimpleTextSegmentInfoWriter.SI_NUM_FILES
					.length, scratch));
				ICollection<string> files = new HashSet<string>();
				for (int i_1 = 0; i_1 < numFiles; i_1++)
				{
					SimpleTextUtil.ReadLine(input, scratch);
					//HM:revisit 
					//assert StringHelper.startsWith(scratch, SI_FILE);
					string fileName = ReadString(SimpleTextSegmentInfoWriter.SI_FILE.length, scratch);
					files.AddItem(fileName);
				}
				SimpleTextUtil.CheckFooter(input);
				SegmentInfo info = new SegmentInfo(directory, version, segmentName, docCount, isCompoundFile
					, null, diagnostics);
				info.SetFiles(files);
				success = true;
				return info;
			}
			finally
			{
				if (!success)
				{
					IOUtils.CloseWhileHandlingException(input);
				}
				else
				{
					input.Close();
				}
			}
		}

		private string ReadString(int offset, BytesRef scratch)
		{
			return new string(scratch.bytes, scratch.offset + offset, scratch.length - offset
				, StandardCharsets.UTF_8);
		}
	}
}
