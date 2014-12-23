using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Codecs.Lucene3x;

namespace Lucene.Net.Codecs.Lucene3x.TestFramework
{
	internal class PreFlexRWStoredFieldsFormat : Lucene3xStoredFieldsFormat
	{
		/// <exception cref="System.IO.IOException"></exception>
		public override StoredFieldsWriter FieldsWriter(Directory directory, SegmentInfo 
			segmentInfo, IOContext context)
		{
			return new PreFlexRWStoredFieldsWriter(directory, segmentInfo.name, context);
		}
	}
}
