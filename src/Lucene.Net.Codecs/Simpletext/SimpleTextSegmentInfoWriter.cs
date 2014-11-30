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
	/// writes plaintext segments files
	/// <p>
	/// <b><font color="red">FOR RECREATIONAL USE ONLY</font></B>
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class SimpleTextSegmentInfoWriter : SegmentInfoWriter
	{
		internal static readonly BytesRef SI_VERSION = new BytesRef("    version ");

		internal static readonly BytesRef SI_DOCCOUNT = new BytesRef("    number of documents "
			);

		internal static readonly BytesRef SI_USECOMPOUND = new BytesRef("    uses compound file "
			);

		internal static readonly BytesRef SI_NUM_DIAG = new BytesRef("    diagnostics ");

		internal static readonly BytesRef SI_DIAG_KEY = new BytesRef("      key ");

		internal static readonly BytesRef SI_DIAG_VALUE = new BytesRef("      value ");

		internal static readonly BytesRef SI_NUM_FILES = new BytesRef("    files ");

		internal static readonly BytesRef SI_FILE = new BytesRef("      file ");

		/// <exception cref="System.IO.IOException"></exception>
		public override void Write(Directory dir, SegmentInfo si, FieldInfos fis, IOContext
			 ioContext)
		{
			string segFileName = IndexFileNames.SegmentFileName(si.name, string.Empty, SimpleTextSegmentInfoFormat
				.SI_EXTENSION);
			si.AddFile(segFileName);
			bool success = false;
			IndexOutput output = dir.CreateOutput(segFileName, ioContext);
			try
			{
				BytesRef scratch = new BytesRef();
				SimpleTextUtil.Write(output, SI_VERSION);
				SimpleTextUtil.Write(output, si.GetVersion(), scratch);
				SimpleTextUtil.WriteNewline(output);
				SimpleTextUtil.Write(output, SI_DOCCOUNT);
				SimpleTextUtil.Write(output, Sharpen.Extensions.ToString(si.GetDocCount()), scratch
					);
				SimpleTextUtil.WriteNewline(output);
				SimpleTextUtil.Write(output, SI_USECOMPOUND);
				SimpleTextUtil.Write(output, bool.ToString(si.GetUseCompoundFile()), scratch);
				SimpleTextUtil.WriteNewline(output);
				IDictionary<string, string> diagnostics = si.GetDiagnostics();
				int numDiagnostics = diagnostics == null ? 0 : diagnostics.Count;
				SimpleTextUtil.Write(output, SI_NUM_DIAG);
				SimpleTextUtil.Write(output, Sharpen.Extensions.ToString(numDiagnostics), scratch
					);
				SimpleTextUtil.WriteNewline(output);
				if (numDiagnostics > 0)
				{
					foreach (KeyValuePair<string, string> diagEntry in diagnostics.EntrySet())
					{
						SimpleTextUtil.Write(output, SI_DIAG_KEY);
						SimpleTextUtil.Write(output, diagEntry.Key, scratch);
						SimpleTextUtil.WriteNewline(output);
						SimpleTextUtil.Write(output, SI_DIAG_VALUE);
						SimpleTextUtil.Write(output, diagEntry.Value, scratch);
						SimpleTextUtil.WriteNewline(output);
					}
				}
				ICollection<string> files = si.Files;
				int numFiles = files == null ? 0 : files.Count;
				SimpleTextUtil.Write(output, SI_NUM_FILES);
				SimpleTextUtil.Write(output, Sharpen.Extensions.ToString(numFiles), scratch);
				SimpleTextUtil.WriteNewline(output);
				if (numFiles > 0)
				{
					foreach (string fileName in files)
					{
						SimpleTextUtil.Write(output, SI_FILE);
						SimpleTextUtil.Write(output, fileName, scratch);
						SimpleTextUtil.WriteNewline(output);
					}
				}
				SimpleTextUtil.WriteChecksum(output, scratch);
				success = true;
			}
			finally
			{
				if (!success)
				{
					IOUtils.CloseWhileHandlingException(output);
					try
					{
						dir.DeleteFile(segFileName);
					}
					catch
					{
					}
				}
				else
				{
					output.Close();
				}
			}
		}
	}
}
