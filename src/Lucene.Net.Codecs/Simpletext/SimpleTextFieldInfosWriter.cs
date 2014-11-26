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
	/// writes plaintext field infos files
	/// <p>
	/// <b><font color="red">FOR RECREATIONAL USE ONLY</font></B>
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class SimpleTextFieldInfosWriter : FieldInfosWriter
	{
		/// <summary>Extension of field infos</summary>
		internal static readonly string FIELD_INFOS_EXTENSION = "inf";

		internal static readonly BytesRef NUMFIELDS = new BytesRef("number of fields ");

		internal static readonly BytesRef NAME = new BytesRef("  name ");

		internal static readonly BytesRef NUMBER = new BytesRef("  number ");

		internal static readonly BytesRef ISINDEXED = new BytesRef("  indexed ");

		internal static readonly BytesRef STORETV = new BytesRef("  term vectors ");

		internal static readonly BytesRef STORETVPOS = new BytesRef("  term vector positions "
			);

		internal static readonly BytesRef STORETVOFF = new BytesRef("  term vector offsets "
			);

		internal static readonly BytesRef PAYLOADS = new BytesRef("  payloads ");

		internal static readonly BytesRef NORMS = new BytesRef("  norms ");

		internal static readonly BytesRef NORMS_TYPE = new BytesRef("  norms type ");

		internal static readonly BytesRef DOCVALUES = new BytesRef("  doc values ");

		internal static readonly BytesRef DOCVALUES_GEN = new BytesRef("  doc values gen "
			);

		internal static readonly BytesRef INDEXOPTIONS = new BytesRef("  index options ");

		internal static readonly BytesRef NUM_ATTS = new BytesRef("  attributes ");

		internal static readonly BytesRef ATT_KEY = new BytesRef("    key ");

		internal static readonly BytesRef ATT_VALUE = new BytesRef("    value ");

		/// <exception cref="System.IO.IOException"></exception>
		public override void Write(Directory directory, string segmentName, string segmentSuffix
			, FieldInfos infos, IOContext context)
		{
			string fileName = IndexFileNames.SegmentFileName(segmentName, segmentSuffix, FIELD_INFOS_EXTENSION
				);
			IndexOutput @out = directory.CreateOutput(fileName, context);
			BytesRef scratch = new BytesRef();
			bool success = false;
			try
			{
				SimpleTextUtil.Write(@out, NUMFIELDS);
				SimpleTextUtil.Write(@out, Sharpen.Extensions.ToString(infos.Size()), scratch);
				SimpleTextUtil.WriteNewline(@out);
				foreach (FieldInfo fi in infos)
				{
					SimpleTextUtil.Write(@out, NAME);
					SimpleTextUtil.Write(@out, fi.name, scratch);
					SimpleTextUtil.WriteNewline(@out);
					SimpleTextUtil.Write(@out, NUMBER);
					SimpleTextUtil.Write(@out, Sharpen.Extensions.ToString(fi.number), scratch);
					SimpleTextUtil.WriteNewline(@out);
					SimpleTextUtil.Write(@out, ISINDEXED);
					SimpleTextUtil.Write(@out, bool.ToString(fi.IsIndexed()), scratch);
					SimpleTextUtil.WriteNewline(@out);
					if (fi.IsIndexed())
					{
						//HM:revisit 
						//assert fi.getIndexOptions().compareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0 || !fi.hasPayloads();
						SimpleTextUtil.Write(@out, INDEXOPTIONS);
						SimpleTextUtil.Write(@out, fi.GetIndexOptions().ToString(), scratch);
						SimpleTextUtil.WriteNewline(@out);
					}
					SimpleTextUtil.Write(@out, STORETV);
					SimpleTextUtil.Write(@out, bool.ToString(fi.HasVectors()), scratch);
					SimpleTextUtil.WriteNewline(@out);
					SimpleTextUtil.Write(@out, PAYLOADS);
					SimpleTextUtil.Write(@out, bool.ToString(fi.HasPayloads()), scratch);
					SimpleTextUtil.WriteNewline(@out);
					SimpleTextUtil.Write(@out, NORMS);
					SimpleTextUtil.Write(@out, bool.ToString(!fi.OmitsNorms()), scratch);
					SimpleTextUtil.WriteNewline(@out);
					SimpleTextUtil.Write(@out, NORMS_TYPE);
					SimpleTextUtil.Write(@out, GetDocValuesType(fi.GetNormType()), scratch);
					SimpleTextUtil.WriteNewline(@out);
					SimpleTextUtil.Write(@out, DOCVALUES);
					SimpleTextUtil.Write(@out, GetDocValuesType(fi.GetDocValuesType()), scratch);
					SimpleTextUtil.WriteNewline(@out);
					SimpleTextUtil.Write(@out, DOCVALUES_GEN);
					SimpleTextUtil.Write(@out, System.Convert.ToString(fi.GetDocValuesGen()), scratch
						);
					SimpleTextUtil.WriteNewline(@out);
					IDictionary<string, string> atts = fi.Attributes();
					int numAtts = atts == null ? 0 : atts.Count;
					SimpleTextUtil.Write(@out, NUM_ATTS);
					SimpleTextUtil.Write(@out, Sharpen.Extensions.ToString(numAtts), scratch);
					SimpleTextUtil.WriteNewline(@out);
					if (numAtts > 0)
					{
						foreach (KeyValuePair<string, string> entry in atts.EntrySet())
						{
							SimpleTextUtil.Write(@out, ATT_KEY);
							SimpleTextUtil.Write(@out, entry.Key, scratch);
							SimpleTextUtil.WriteNewline(@out);
							SimpleTextUtil.Write(@out, ATT_VALUE);
							SimpleTextUtil.Write(@out, entry.Value, scratch);
							SimpleTextUtil.WriteNewline(@out);
						}
					}
				}
				SimpleTextUtil.WriteChecksum(@out, scratch);
				success = true;
			}
			finally
			{
				if (success)
				{
					@out.Close();
				}
				else
				{
					IOUtils.CloseWhileHandlingException(@out);
				}
			}
		}

		private static string GetDocValuesType(FieldInfo.DocValuesType type)
		{
			return type == null ? "false" : type.ToString();
		}
	}
}
