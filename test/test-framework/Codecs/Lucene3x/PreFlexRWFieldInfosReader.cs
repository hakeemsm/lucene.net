/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Org.Apache.Lucene.Codecs;
using Lucene.Net.Codecs.Lucene3x;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Store;
using Sharpen;

namespace Lucene.Net.Codecs.Lucene3x
{
	/// <lucene.internal></lucene.internal>
	/// <lucene.experimental></lucene.experimental>
	internal class PreFlexRWFieldInfosReader : FieldInfosReader
	{
		internal const int FORMAT_MINIMUM = PreFlexRWFieldInfosWriter.FORMAT_START;

		/// <exception cref="System.IO.IOException"></exception>
		public override FieldInfos Read(Directory directory, string segmentName, string segmentSuffix
			, IOContext iocontext)
		{
			string fileName = IndexFileNames.SegmentFileName(segmentName, string.Empty, PreFlexRWFieldInfosWriter
				.FIELD_INFOS_EXTENSION);
			IndexInput input = directory.OpenInput(fileName, iocontext);
			try
			{
				int format = input.ReadVInt();
				if (format > FORMAT_MINIMUM)
				{
					throw new IndexFormatTooOldException(input, format, FORMAT_MINIMUM, PreFlexRWFieldInfosWriter
						.FORMAT_CURRENT);
				}
				if (format < PreFlexRWFieldInfosWriter.FORMAT_CURRENT && format != PreFlexRWFieldInfosWriter
					.FORMAT_PREFLEX_RW)
				{
					throw new IndexFormatTooNewException(input, format, FORMAT_MINIMUM, PreFlexRWFieldInfosWriter
						.FORMAT_CURRENT);
				}
				int size = input.ReadVInt();
				//read in the size
				FieldInfo[] infos = new FieldInfo[size];
				for (int i = 0; i < size; i++)
				{
					string name = input.ReadString();
					int fieldNumber = format == PreFlexRWFieldInfosWriter.FORMAT_PREFLEX_RW ? input.ReadInt
						() : i;
					byte bits = input.ReadByte();
					bool isIndexed = (bits & PreFlexRWFieldInfosWriter.IS_INDEXED) != 0;
					bool storeTermVector = (bits & PreFlexRWFieldInfosWriter.STORE_TERMVECTOR) != 0;
					bool omitNorms = (bits & PreFlexRWFieldInfosWriter.OMIT_NORMS) != 0;
					bool storePayloads = (bits & PreFlexRWFieldInfosWriter.STORE_PAYLOADS) != 0;
					FieldInfo.IndexOptions indexOptions;
					if (!isIndexed)
					{
						indexOptions = null;
					}
					else
					{
						if ((bits & PreFlexRWFieldInfosWriter.OMIT_TERM_FREQ_AND_POSITIONS) != 0)
						{
							indexOptions = FieldInfo.IndexOptions.DOCS_ONLY;
						}
						else
						{
							if ((bits & PreFlexRWFieldInfosWriter.OMIT_POSITIONS) != 0)
							{
								if (format <= PreFlexRWFieldInfosWriter.FORMAT_OMIT_POSITIONS)
								{
									indexOptions = FieldInfo.IndexOptions.DOCS_AND_FREQS;
								}
								else
								{
									throw new CorruptIndexException("Corrupt fieldinfos, OMIT_POSITIONS set but format="
										 + format + " (resource: " + input + ")");
								}
							}
							else
							{
								indexOptions = FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
							}
						}
					}
					// LUCENE-3027: past indices were able to write
					// storePayloads=true when omitTFAP is also true,
					// which is invalid.  We correct that, here:
					if (indexOptions != FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
					{
						storePayloads = false;
					}
					FieldInfo.DocValuesType normType = isIndexed && !omitNorms ? FieldInfo.DocValuesType
						.NUMERIC : null;
					if (format == PreFlexRWFieldInfosWriter.FORMAT_PREFLEX_RW && normType != null)
					{
						// RW can have norms but doesn't write them
						normType = input.ReadByte() != 0 ? FieldInfo.DocValuesType.NUMERIC : null;
					}
					infos[i] = new FieldInfo(name, isIndexed, fieldNumber, storeTermVector, omitNorms
						, storePayloads, indexOptions, null, normType, null);
				}
				if (input.GetFilePointer() != input.Length())
				{
					throw new CorruptIndexException("did not read all bytes from file \"" + fileName 
						+ "\": read " + input.GetFilePointer() + " vs size " + input.Length() + " (resource: "
						 + input + ")");
				}
				return new FieldInfos(infos);
			}
			finally
			{
				input.Close();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static void Files(Directory dir, SegmentInfo info, ICollection<string> files
			)
		{
			files.AddItem(IndexFileNames.SegmentFileName(info.name, string.Empty, PreFlexRWFieldInfosWriter
				.FIELD_INFOS_EXTENSION));
		}
	}
}
