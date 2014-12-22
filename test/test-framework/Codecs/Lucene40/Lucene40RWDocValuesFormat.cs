/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Codecs;
using Lucene.Net.Codecs.Lucene40;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Lucene.Net.Codecs.Lucene40
{
	/// <summary>
	/// Read-write version of
	/// <see cref="Lucene40DocValuesFormat">Lucene40DocValuesFormat</see>
	/// for testing
	/// </summary>
	public class Lucene40RWDocValuesFormat : Lucene40DocValuesFormat
	{
		/// <exception cref="System.IO.IOException"></exception>
		public override DocValuesConsumer FieldsConsumer(SegmentWriteState state)
		{
			if (!LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE)
			{
				return base.FieldsConsumer(state);
			}
			else
			{
				string filename = IndexFileNames.SegmentFileName(state.segmentInfo.name, "dv", IndexFileNames
					.COMPOUND_FILE_EXTENSION);
				return new Lucene40DocValuesWriter(state, filename, Lucene40FieldInfosReader.LEGACY_DV_TYPE_KEY
					);
			}
		}
	}
}
