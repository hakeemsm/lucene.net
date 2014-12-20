/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Codecs;
using Org.Apache.Lucene.Codecs.Lucene3x;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Store;
using Sharpen;

namespace Org.Apache.Lucene.Codecs.Lucene3x
{
	/// <summary>
	/// PreFlex implementation of
	/// <see cref="Org.Apache.Lucene.Codecs.SegmentInfoWriter">Org.Apache.Lucene.Codecs.SegmentInfoWriter
	/// 	</see>
	/// .
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	internal class PreFlexRWSegmentInfoWriter : SegmentInfoWriter
	{
		// NOTE: this is not "really" 3.x format, because we are
		// writing each SI to its own file, vs 3.x where the list
		// of segments and SI for each segment is written into a
		// single segments_N file
		/// <summary>Save a single segment's info.</summary>
		/// <remarks>Save a single segment's info.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public override void Write(Directory dir, SegmentInfo si, FieldInfos fis, IOContext
			 ioContext)
		{
			SegmentInfos.Write3xInfo(dir, si, ioContext);
		}
	}
}
