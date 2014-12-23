using Lucene.Net.Index;

namespace Lucene.Net.Codecs.Lucene40.TestFramework
{
	/// <summary>
	/// Read-write version of
	/// <see cref="Lucene40NormsFormat">Lucene40NormsFormat</see>
	/// for testing
	/// </summary>
	public class Lucene40RWNormsFormat : Lucene40NormsFormat
	{
		/// <exception cref="System.IO.IOException"></exception>
		public override DocValuesConsumer NormsConsumer(SegmentWriteState state)
		{
			if (!LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE)
			{
				return base.NormsConsumer(state);
			}
			else
			{
				string filename = IndexFileNames.SegmentFileName(state.segmentInfo.name, "nrm", IndexFileNames
					.COMPOUND_FILE_EXTENSION);
				return new Lucene40DocValuesWriter(state, filename, Lucene40FieldInfosReader.LEGACY_NORM_TYPE_KEY
					);
			}
		}
	}
}
