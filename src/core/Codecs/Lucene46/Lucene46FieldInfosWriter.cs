using System;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;

namespace Lucene.Net.Codecs.Lucene46
{
	/// <summary>Lucene 4.6 FieldInfos writer.</summary>
	/// <remarks>Lucene 4.6 FieldInfos writer.</remarks>
	/// <seealso cref="Lucene46FieldInfosFormat">Lucene46FieldInfosFormat</seealso>
	/// <lucene.experimental></lucene.experimental>
	internal sealed class Lucene46FieldInfosWriter : FieldInfosWriter
	{
	    /// <exception cref="System.IO.IOException"></exception>
		public override void Write(Directory directory, string segmentName, string segmentSuffix
			, FieldInfos infos, IOContext context)
		{
			string fileName = IndexFileNames.SegmentFileName(segmentName, segmentSuffix, Lucene46FieldInfosFormat
				.EXTENSION);
			IndexOutput output = directory.CreateOutput(fileName, context);
			bool success = false;
			try
			{
				CodecUtil.WriteHeader(output, Lucene46FieldInfosFormat.CODEC_NAME, Lucene46FieldInfosFormat
					.FORMAT_CURRENT);
				output.WriteVInt(infos.Size);
				foreach (FieldInfo fi in infos)
				{
					FieldInfo.IndexOptions? indexOptions = fi.IndexOptionsValue;
					byte bits = unchecked((int)(0x0));
					if (fi.HasVectors)
					{
						bits |= Lucene46FieldInfosFormat.STORE_TERMVECTOR;
					}
					if (fi.OmitsNorms)
					{
						bits |= Lucene46FieldInfosFormat.OMIT_NORMS;
					}
					if (fi.HasPayloads)
					{
						bits |= Lucene46FieldInfosFormat.STORE_PAYLOADS;
					}
					if (fi.IsIndexed)
					{
						bits |= Lucene46FieldInfosFormat.IS_INDEXED;
						
						//assert indexOptions.compareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0 || !fi.hasPayloads();
						if (indexOptions == FieldInfo.IndexOptions.DOCS_ONLY)
						{
							bits |= Lucene46FieldInfosFormat.OMIT_TERM_FREQ_AND_POSITIONS;
						}
						else
						{
							if (indexOptions == FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS)
							{
								bits |= Lucene46FieldInfosFormat.STORE_OFFSETS_IN_POSTINGS;
							}
							else
							{
								if (indexOptions == FieldInfo.IndexOptions.DOCS_AND_FREQS)
								{
									bits |= Lucene46FieldInfosFormat.OMIT_POSITIONS;
								}
							}
						}
					}
					output.WriteString(fi.name);
					output.WriteVInt(fi.number);
					output.WriteByte(bits);
					// pack the DV types in one byte
					byte dv = DocValuesByte(fi.GetDocValuesType());
					byte nrm = DocValuesByte(fi.NormType.Value);
					//HM:revisit 
					//assert (dv & (~0xF)) == 0 && (nrm & (~0x0F)) == 0;
					byte val = unchecked((byte)(unchecked((int)(0xff)) & ((nrm << 4) | dv)));
					output.WriteByte(val);
					output.WriteLong(fi.DocValuesGen);
					output.WriteStringStringMap(fi.Attributes);
				}
				CodecUtil.WriteFooter(output);
				success = true;
			}
			finally
			{
				if (success)
				{
					output.Dispose();
				}
				else
				{
					IOUtils.CloseWhileHandlingException((IDisposable)output);
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
