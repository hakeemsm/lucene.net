using System;
using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;

namespace Lucene.Net.Codecs.Lucene46
{
	/// <summary>Lucene 4.6 FieldInfos reader.</summary>
	/// <remarks>Lucene 4.6 FieldInfos reader.</remarks>
	/// <lucene.experimental></lucene.experimental>
	/// <seealso cref="Lucene46FieldInfosFormat">Lucene46FieldInfosFormat</seealso>
	internal sealed class Lucene46FieldInfosReader : FieldInfosReader
	{
	    /// <exception cref="System.IO.IOException"></exception>
		public override FieldInfos Read(Directory directory, string segmentName, string segmentSuffix
			, IOContext context)
		{
			string fileName = IndexFileNames.SegmentFileName(segmentName, segmentSuffix, Lucene46FieldInfosFormat
				.EXTENSION);
			ChecksumIndexInput input = directory.OpenChecksumInput(fileName, context);
			bool success = false;
			try
			{
				int codecVersion = CodecUtil.CheckHeader(input, Lucene46FieldInfosFormat.CODEC_NAME
					, Lucene46FieldInfosFormat.FORMAT_START, Lucene46FieldInfosFormat.FORMAT_CURRENT
					);
				int size = input.ReadVInt();
				//read in the size
				FieldInfo[] infos = new FieldInfo[size];
				for (int i = 0; i < size; i++)
				{
					string name = input.ReadString();
					int fieldNumber = input.ReadVInt();
					byte bits = input.ReadByte();
					bool isIndexed = (bits & Lucene46FieldInfosFormat.IS_INDEXED) != 0;
					bool storeTermVector = (bits & Lucene46FieldInfosFormat.STORE_TERMVECTOR) != 0;
					bool omitNorms = (bits & Lucene46FieldInfosFormat.OMIT_NORMS) != 0;
					bool storePayloads = (bits & Lucene46FieldInfosFormat.STORE_PAYLOADS) != 0;
                    FieldInfo.IndexOptions? indexOptions = null; //.NET port cant set enum to null since its value type
                    
                    if (isIndexed)
					{
						if ((bits & Lucene46FieldInfosFormat.OMIT_TERM_FREQ_AND_POSITIONS) != 0)
						{
							indexOptions = FieldInfo.IndexOptions.DOCS_ONLY;
						}
						else
						{
							if ((bits & Lucene46FieldInfosFormat.OMIT_POSITIONS) != 0)
							{
								indexOptions = FieldInfo.IndexOptions.DOCS_AND_FREQS;
							}
							else
							{
								if ((bits & Lucene46FieldInfosFormat.STORE_OFFSETS_IN_POSTINGS) != 0)
								{
									indexOptions = FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
								}
								else
								{
									indexOptions = FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
								}
							}
						}
					}
					// DV Types are packed in one byte
					byte val = input.ReadByte();
					FieldInfo.DocValuesType docValuesType = GetDocValuesType(input, unchecked((byte)(
						val & unchecked((int)(0x0F)))));
					FieldInfo.DocValuesType normsType = GetDocValuesType(input, unchecked((byte)((val
						 >> 4) & unchecked((int)(0x0F)))));
					long dvGen = input.ReadLong();
					IDictionary<string, string> attributes = input.ReadStringStringMap();
					infos[i] = new FieldInfo(name, isIndexed, fieldNumber, storeTermVector, omitNorms
						, storePayloads, indexOptions, docValuesType, normsType, attributes) {DocValuesGen = dvGen};
				}
				if (codecVersion >= Lucene46FieldInfosFormat.FORMAT_CHECKSUM)
				{
					CodecUtil.CheckFooter(input);
				}
				else
				{
					CodecUtil.CheckEOF(input);
				}
				FieldInfos fieldInfos = new FieldInfos(infos);
				success = true;
				return fieldInfos;
			}
			finally
			{
				if (success)
				{
					input.Dispose();
				}
				else
				{
					IOUtils.CloseWhileHandlingException((IDisposable)input);
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private static FieldInfo.DocValuesType GetDocValuesType(IndexInput input, byte b)
		{
		    if (b == 0)
			{
                return FieldInfo.DocValuesType.UNKNOWN; //.NET port can't set null to enums
			}
		    if (b == 1)
		    {
		        return FieldInfo.DocValuesType.NUMERIC;
		    }
		    if (b == 2)
		    {
		        return FieldInfo.DocValuesType.BINARY;
		    }
		    if (b == 3)
		    {
		        return FieldInfo.DocValuesType.SORTED;
		    }
		    if (b == 4)
		    {
		        return FieldInfo.DocValuesType.SORTED_SET;
		    }
		    throw new CorruptIndexException("invalid docvalues byte: " + b + " (resource=" + input + ")");
		}
	}
}
