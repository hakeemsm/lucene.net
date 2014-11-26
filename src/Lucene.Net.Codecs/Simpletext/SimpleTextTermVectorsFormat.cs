/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Simpletext;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Sharpen;

namespace Lucene.Net.Codecs.Simpletext
{
	/// <summary>plain text term vectors format.</summary>
	/// <remarks>
	/// plain text term vectors format.
	/// <p>
	/// <b><font color="red">FOR RECREATIONAL USE ONLY</font></B>
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public class SimpleTextTermVectorsFormat : TermVectorsFormat
	{
		/// <exception cref="System.IO.IOException"></exception>
		public override TermVectorsReader VectorsReader(Directory directory, SegmentInfo 
			segmentInfo, FieldInfos fieldInfos, IOContext context)
		{
			return new SimpleTextTermVectorsReader(directory, segmentInfo, context);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override TermVectorsWriter VectorsWriter(Directory directory, SegmentInfo 
			segmentInfo, IOContext context)
		{
			return new SimpleTextTermVectorsWriter(directory, segmentInfo.name, context);
		}
	}
}
