/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Codecs;
using Lucene.Net.Codecs.Lucene3x;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Store;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Lucene.Net.Codecs.Lucene3x
{
	/// <lucene.internal></lucene.internal>
	/// <lucene.experimental></lucene.experimental>
	internal class PreFlexRWFieldInfosWriter : FieldInfosWriter
	{
		/// <summary>Extension of field infos</summary>
		internal static readonly string FIELD_INFOS_EXTENSION = "fnm";

		internal const int FORMAT_START = -2;

		internal const int FORMAT_OMIT_POSITIONS = -3;

		internal const int FORMAT_PREFLEX_RW = int.MinValue;

		internal const int FORMAT_CURRENT = FORMAT_OMIT_POSITIONS;

		internal const byte IS_INDEXED = unchecked((int)(0x1));

		internal const byte STORE_TERMVECTOR = unchecked((int)(0x2));

		internal const byte OMIT_NORMS = unchecked((int)(0x10));

		internal const byte STORE_PAYLOADS = unchecked((int)(0x20));

		internal const byte OMIT_TERM_FREQ_AND_POSITIONS = unchecked((int)(0x40));

		internal const byte OMIT_POSITIONS = unchecked((byte)(-128));

		// TODO move to test-framework preflex RW?
		// First used in 2.9; prior to 2.9 there was no format header
		// First used in 3.4: omit only positional information
		// whenever you add a new format, make it 1 smaller (negative version logic)!
		/// <exception cref="System.IO.IOException"></exception>
		public override void Write(Directory directory, string segmentName, string segmentSuffix
			, FieldInfos infos, IOContext context)
		{
			string fileName = IndexFileNames.SegmentFileName(segmentName, string.Empty, FIELD_INFOS_EXTENSION
				);
			IndexOutput output = directory.CreateOutput(fileName, context);
			bool success = false;
			try
			{
				output.WriteVInt(FORMAT_PREFLEX_RW);
				output.WriteVInt(infos.Size());
				foreach (FieldInfo fi in infos)
				{
					byte bits = unchecked((int)(0x0));
					if (fi.HasVectors())
					{
						bits |= STORE_TERMVECTOR;
					}
					if (fi.OmitsNorms())
					{
						bits |= OMIT_NORMS;
					}
					if (fi.HasPayloads())
					{
						bits |= STORE_PAYLOADS;
					}
					if (fi.IsIndexed())
					{
						bits |= IS_INDEXED;
						 
						//assert fi.getIndexOptions() == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS || !fi.hasPayloads();
						if (fi.GetIndexOptions() == FieldInfo.IndexOptions.DOCS_ONLY)
						{
							bits |= OMIT_TERM_FREQ_AND_POSITIONS;
						}
						else
						{
							if (fi.GetIndexOptions() == FieldInfo.IndexOptions.DOCS_AND_FREQS)
							{
								bits |= OMIT_POSITIONS;
							}
						}
					}
					output.WriteString(fi.name);
					output.WriteInt(fi.number);
					output.WriteByte(bits);
					if (fi.IsIndexed() && !fi.OmitsNorms())
					{
						// to allow null norm types we need to indicate if norms are written 
						// only in RW case
						output.WriteByte(unchecked((byte)(fi.GetNormType() == null ? 0 : 1)));
					}
				}
				 
				//assert fi.attributes() == null; // not used or supported
				success = true;
			}
			finally
			{
				if (success)
				{
					output.Close();
				}
				else
				{
					IOUtils.CloseWhileHandlingException(output);
				}
			}
		}
	}
}
