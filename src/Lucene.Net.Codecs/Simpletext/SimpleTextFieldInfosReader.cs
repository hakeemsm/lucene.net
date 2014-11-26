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
	/// reads plaintext field infos files
	/// <p>
	/// <b><font color="red">FOR RECREATIONAL USE ONLY</font></B>
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class SimpleTextFieldInfosReader : FieldInfosReader
	{
		/// <exception cref="System.IO.IOException"></exception>
		public override FieldInfos Read(Directory directory, string segmentName, string segmentSuffix
			, IOContext iocontext)
		{
			string fileName = IndexFileNames.SegmentFileName(segmentName, segmentSuffix, FIELD_INFOS_EXTENSION
				);
			ChecksumIndexInput input = directory.OpenChecksumInput(fileName, iocontext);
			BytesRef scratch = new BytesRef();
			bool success = false;
			try
			{
				SimpleTextUtil.ReadLine(input, scratch);
				//HM:revisit 
				//assert StringHelper.startsWith(scratch, NUMFIELDS);
				int size = System.Convert.ToInt32(ReadString(NUMFIELDS.length, scratch));
				FieldInfo[] infos = new FieldInfo[size];
				for (int i = 0; i < size; i++)
				{
					SimpleTextUtil.ReadLine(input, scratch);
					//HM:revisit 
					//assert StringHelper.startsWith(scratch, NAME);
					string name = ReadString(NAME.length, scratch);
					SimpleTextUtil.ReadLine(input, scratch);
					//HM:revisit 
					//assert StringHelper.startsWith(scratch, NUMBER);
					int fieldNumber = System.Convert.ToInt32(ReadString(NUMBER.length, scratch));
					SimpleTextUtil.ReadLine(input, scratch);
					//HM:revisit 
					//assert StringHelper.startsWith(scratch, ISINDEXED);
					bool isIndexed = System.Boolean.Parse(ReadString(ISINDEXED.length, scratch));
					FieldInfo.IndexOptions indexOptions;
					if (isIndexed)
					{
						SimpleTextUtil.ReadLine(input, scratch);
						//HM:revisit 
						//assert StringHelper.startsWith(scratch, INDEXOPTIONS);
						indexOptions = FieldInfo.IndexOptions.ValueOf(ReadString(INDEXOPTIONS.length, scratch
							));
					}
					else
					{
						indexOptions = null;
					}
					SimpleTextUtil.ReadLine(input, scratch);
					//HM:revisit 
					//assert StringHelper.startsWith(scratch, STORETV);
					bool storeTermVector = System.Boolean.Parse(ReadString(STORETV.length, scratch));
					SimpleTextUtil.ReadLine(input, scratch);
					//HM:revisit 
					//assert StringHelper.startsWith(scratch, PAYLOADS);
					bool storePayloads = System.Boolean.Parse(ReadString(PAYLOADS.length, scratch));
					SimpleTextUtil.ReadLine(input, scratch);
					//HM:revisit 
					//assert StringHelper.startsWith(scratch, NORMS);
					bool omitNorms = !System.Boolean.Parse(ReadString(NORMS.length, scratch));
					SimpleTextUtil.ReadLine(input, scratch);
					//HM:revisit 
					//assert StringHelper.startsWith(scratch, NORMS_TYPE);
					string nrmType = ReadString(NORMS_TYPE.length, scratch);
					FieldInfo.DocValuesType normsType = DocValuesType(nrmType);
					SimpleTextUtil.ReadLine(input, scratch);
					//HM:revisit 
					//assert StringHelper.startsWith(scratch, DOCVALUES);
					string dvType = ReadString(DOCVALUES.length, scratch);
					FieldInfo.DocValuesType docValuesType = DocValuesType(dvType);
					SimpleTextUtil.ReadLine(input, scratch);
					//HM:revisit 
					//assert StringHelper.startsWith(scratch, DOCVALUES_GEN);
					long dvGen = long.Parse(ReadString(DOCVALUES_GEN.length, scratch));
					SimpleTextUtil.ReadLine(input, scratch);
					//HM:revisit 
					//assert StringHelper.startsWith(scratch, NUM_ATTS);
					int numAtts = System.Convert.ToInt32(ReadString(NUM_ATTS.length, scratch));
					IDictionary<string, string> atts = new Dictionary<string, string>();
					for (int j = 0; j < numAtts; j++)
					{
						SimpleTextUtil.ReadLine(input, scratch);
						//HM:revisit 
						//assert StringHelper.startsWith(scratch, ATT_KEY);
						string key = ReadString(ATT_KEY.length, scratch);
						SimpleTextUtil.ReadLine(input, scratch);
						//HM:revisit 
						//assert StringHelper.startsWith(scratch, ATT_VALUE);
						string value = ReadString(ATT_VALUE.length, scratch);
						atts.Put(key, value);
					}
					infos[i] = new FieldInfo(name, isIndexed, fieldNumber, storeTermVector, omitNorms
						, storePayloads, indexOptions, docValuesType, normsType, Sharpen.Collections.UnmodifiableMap
						(atts));
					infos[i].SetDocValuesGen(dvGen);
				}
				SimpleTextUtil.CheckFooter(input);
				FieldInfos fieldInfos = new FieldInfos(infos);
				success = true;
				return fieldInfos;
			}
			finally
			{
				if (success)
				{
					input.Close();
				}
				else
				{
					IOUtils.CloseWhileHandlingException(input);
				}
			}
		}

		public virtual FieldInfo.DocValuesType DocValuesType(string dvType)
		{
			if ("false".Equals(dvType))
			{
				return null;
			}
			else
			{
				return FieldInfo.DocValuesType.ValueOf(dvType);
			}
		}

		private string ReadString(int offset, BytesRef scratch)
		{
			return new string(scratch.bytes, scratch.offset + offset, scratch.length - offset
				, StandardCharsets.UTF_8);
		}
	}
}
