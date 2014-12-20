/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Codecs;
using Org.Apache.Lucene.Codecs.Lucene40;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Store;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Codecs.Lucene40
{
	/// <summary>Lucene 4.0 FieldInfos writer.</summary>
	/// <remarks>Lucene 4.0 FieldInfos writer.</remarks>
	/// <seealso cref="Lucene40FieldInfosFormat">Lucene40FieldInfosFormat</seealso>
	/// <lucene.experimental></lucene.experimental>
	public class Lucene40FieldInfosWriter : FieldInfosWriter
	{
		/// <summary>Sole constructor.</summary>
		/// <remarks>Sole constructor.</remarks>
		public Lucene40FieldInfosWriter()
		{
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Write(Directory directory, string segmentName, string segmentSuffix
			, FieldInfos infos, IOContext context)
		{
			string fileName = IndexFileNames.SegmentFileName(segmentName, string.Empty, Lucene40FieldInfosFormat
				.FIELD_INFOS_EXTENSION);
			IndexOutput output = directory.CreateOutput(fileName, context);
			bool success = false;
			try
			{
				CodecUtil.WriteHeader(output, Lucene40FieldInfosFormat.CODEC_NAME, Lucene40FieldInfosFormat
					.FORMAT_CURRENT);
				output.WriteVInt(infos.Size());
				foreach (FieldInfo fi in infos)
				{
					FieldInfo.IndexOptions indexOptions = fi.GetIndexOptions();
					byte bits = unchecked((int)(0x0));
					if (fi.HasVectors())
					{
						bits |= Lucene40FieldInfosFormat.STORE_TERMVECTOR;
					}
					if (fi.OmitsNorms())
					{
						bits |= Lucene40FieldInfosFormat.OMIT_NORMS;
					}
					if (fi.HasPayloads())
					{
						bits |= Lucene40FieldInfosFormat.STORE_PAYLOADS;
					}
					if (fi.IsIndexed())
					{
						bits |= Lucene40FieldInfosFormat.IS_INDEXED;
						//HM:revisit 
						//assert indexOptions.compareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0 || !fi.hasPayloads();
						if (indexOptions == FieldInfo.IndexOptions.DOCS_ONLY)
						{
							bits |= Lucene40FieldInfosFormat.OMIT_TERM_FREQ_AND_POSITIONS;
						}
						else
						{
							if (indexOptions == FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS)
							{
								bits |= Lucene40FieldInfosFormat.STORE_OFFSETS_IN_POSTINGS;
							}
							else
							{
								if (indexOptions == FieldInfo.IndexOptions.DOCS_AND_FREQS)
								{
									bits |= Lucene40FieldInfosFormat.OMIT_POSITIONS;
								}
							}
						}
					}
					output.WriteString(fi.name);
					output.WriteVInt(fi.number);
					output.WriteByte(bits);
					// pack the DV types in one byte
					byte dv = DocValuesByte(fi.GetDocValuesType(), fi.GetAttribute(Lucene40FieldInfosReader
						.LEGACY_DV_TYPE_KEY));
					byte nrm = DocValuesByte(fi.GetNormType(), fi.GetAttribute(Lucene40FieldInfosReader
						.LEGACY_NORM_TYPE_KEY));
					//HM:revisit 
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

		/// <summary>4.0-style docvalues byte</summary>
		public virtual byte DocValuesByte(FieldInfo.DocValuesType type, string legacyTypeAtt
			)
		{
			if (type == null)
			{
				//HM:revisit 
				//assert legacyTypeAtt == null;
				return 0;
			}
			else
			{
				//HM:revisit 
				//assert legacyTypeAtt != null;
				return unchecked((byte)(int)(Lucene40FieldInfosReader.LegacyDocValuesType.ValueOf
					(legacyTypeAtt)));
			}
		}
	}
}
