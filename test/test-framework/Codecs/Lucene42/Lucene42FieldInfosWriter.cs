/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Org.Apache.Lucene.Codecs;
using Lucene.Net.Codecs.Lucene42;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Store;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Lucene.Net.Codecs.Lucene42.TestFramework
{
	/// <summary>Lucene 4.2 FieldInfos writer.</summary>
	/// <remarks>Lucene 4.2 FieldInfos writer.</remarks>
	/// <seealso cref="Lucene42FieldInfosFormat">Lucene42FieldInfosFormat</seealso>
	/// <lucene.experimental></lucene.experimental>
	public sealed class Lucene42FieldInfosWriter : FieldInfosWriter
	{
		/// <summary>Sole constructor.</summary>
		/// <remarks>Sole constructor.</remarks>
		public Lucene42FieldInfosWriter()
		{
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Write(Directory directory, string segmentName, string segmentSuffix
			, FieldInfos infos, IOContext context)
		{
			string fileName = IndexFileNames.SegmentFileName(segmentName, string.Empty, Lucene42FieldInfosFormat
				.EXTENSION);
			IndexOutput output = directory.CreateOutput(fileName, context);
			bool success = false;
			try
			{
				CodecUtil.WriteHeader(output, Lucene42FieldInfosFormat.CODEC_NAME, Lucene42FieldInfosFormat
					.FORMAT_CURRENT);
				output.WriteVInt(infos.Size());
				foreach (FieldInfo fi in infos)
				{
					FieldInfo.IndexOptions indexOptions = fi.GetIndexOptions();
					byte bits = unchecked((int)(0x0));
					if (fi.HasVectors())
					{
						bits |= Lucene42FieldInfosFormat.STORE_TERMVECTOR;
					}
					if (fi.OmitsNorms())
					{
						bits |= Lucene42FieldInfosFormat.OMIT_NORMS;
					}
					if (fi.HasPayloads())
					{
						bits |= Lucene42FieldInfosFormat.STORE_PAYLOADS;
					}
					if (fi.IsIndexed())
					{
						bits |= Lucene42FieldInfosFormat.IS_INDEXED;
						 
						//assert indexOptions.compareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0 || !fi.hasPayloads();
						if (indexOptions == FieldInfo.IndexOptions.DOCS_ONLY)
						{
							bits |= Lucene42FieldInfosFormat.OMIT_TERM_FREQ_AND_POSITIONS;
						}
						else
						{
							if (indexOptions == FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS)
							{
								bits |= Lucene42FieldInfosFormat.STORE_OFFSETS_IN_POSTINGS;
							}
							else
							{
								if (indexOptions == FieldInfo.IndexOptions.DOCS_AND_FREQS)
								{
									bits |= Lucene42FieldInfosFormat.OMIT_POSITIONS;
								}
							}
						}
					}
					output.WriteString(fi.name);
					output.WriteVInt(fi.number);
					output.WriteByte(bits);
					// pack the DV types in one byte
					byte dv = DocValuesByte(fi.GetDocValuesType());
					byte nrm = DocValuesByte(fi.GetNormType());
					 
					//assert (dv & (~0xF)) == 0 && (nrm & (~0x0F)) == 0;
					byte val = unchecked((byte)(unchecked((int)(0xff)) & ((nrm << 4) | dv)));
					output.WriteByte(val);
					output.WriteStringStringMap(fi.Attributes());
				}
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

		private static byte DocValuesByte(FieldInfo.DocValuesType type)
		{
			if (type == null)
			{
				return 0;
			}
			else
			{
				if (type == FieldInfo.DocValuesType.NUMERIC)
				{
					return 1;
				}
				else
				{
					if (type == FieldInfo.DocValuesType.BINARY)
					{
						return 2;
					}
					else
					{
						if (type == FieldInfo.DocValuesType.SORTED)
						{
							return 3;
						}
						else
						{
							if (type == FieldInfo.DocValuesType.SORTED_SET)
							{
								return 4;
							}
							else
							{
								throw new Exception();
							}
						}
					}
				}
			}
		}
	}
}
