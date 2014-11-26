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
	/// <summary>plain text stored fields format.</summary>
	/// <remarks>
	/// plain text stored fields format.
	/// <p>
	/// <b><font color="red">FOR RECREATIONAL USE ONLY</font></B>
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public class SimpleTextStoredFieldsFormat : StoredFieldsFormat
	{
		/// <exception cref="System.IO.IOException"></exception>
		public override StoredFieldsReader FieldsReader(Directory directory, SegmentInfo 
			si, FieldInfos fn, IOContext context)
		{
			return new SimpleTextStoredFieldsReader(directory, si, fn, context);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override StoredFieldsWriter FieldsWriter(Directory directory, SegmentInfo 
			si, IOContext context)
		{
			return new SimpleTextStoredFieldsWriter(directory, si.name, context);
		}
	}
}
