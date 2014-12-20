/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Codecs;
using Org.Apache.Lucene.Codecs.Lucene3x;
using Sharpen;

namespace Org.Apache.Lucene.Codecs.Lucene3x
{
	/// <lucene.experimental></lucene.experimental>
	internal class PreFlexRWSegmentInfoFormat : Lucene3xSegmentInfoFormat
	{
		private readonly SegmentInfoWriter writer = new PreFlexRWSegmentInfoWriter();

		public override SegmentInfoWriter GetSegmentInfoWriter()
		{
			return writer;
		}
	}
}
