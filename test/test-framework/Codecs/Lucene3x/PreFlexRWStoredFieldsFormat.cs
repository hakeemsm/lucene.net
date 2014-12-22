/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Codecs;
using Lucene.Net.Codecs.Lucene3x;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Store;
using Sharpen;

namespace Lucene.Net.Codecs.Lucene3x
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
