/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Simpletext;
using Sharpen;

namespace Lucene.Net.Codecs.Simpletext
{
	/// <summary>plain text segments file format.</summary>
	/// <remarks>
	/// plain text segments file format.
	/// <p>
	/// <b><font color="red">FOR RECREATIONAL USE ONLY</font></B>
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public class SimpleTextSegmentInfoFormat : SegmentInfoFormat
	{
		private readonly SegmentInfoReader reader = new SimpleTextSegmentInfoReader();

		private readonly SegmentInfoWriter writer = new SimpleTextSegmentInfoWriter();

		public static readonly string SI_EXTENSION = "si";

		public override SegmentInfoReader GetSegmentInfoReader()
		{
			return reader;
		}

		public override SegmentInfoWriter GetSegmentInfoWriter()
		{
			return writer;
		}
	}
}
