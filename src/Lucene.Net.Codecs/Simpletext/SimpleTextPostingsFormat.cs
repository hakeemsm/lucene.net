/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Simpletext;
using Lucene.Net.Index;
using Sharpen;

namespace Lucene.Net.Codecs.Simpletext
{
	/// <summary>
	/// For debugging, curiosity, transparency only!!  Do not
	/// use this codec in production.
	/// </summary>
	/// <remarks>
	/// For debugging, curiosity, transparency only!!  Do not
	/// use this codec in production.
	/// <p>This codec stores all postings data in a single
	/// human-readable text file (_N.pst).  You can view this in
	/// any text editor, and even edit it to alter your index.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public sealed class SimpleTextPostingsFormat : PostingsFormat
	{
		public SimpleTextPostingsFormat() : base("SimpleText")
		{
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Lucene.Net.Codecs.FieldsConsumer FieldsConsumer(SegmentWriteState
			 state)
		{
			return new SimpleTextFieldsWriter(state);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Lucene.Net.Codecs.FieldsProducer FieldsProducer(SegmentReadState
			 state)
		{
			return new SimpleTextFieldsReader(state);
		}

		/// <summary>Extension of freq postings file</summary>
		internal static readonly string POSTINGS_EXTENSION = "pst";

		internal static string GetPostingsFileName(string segment, string segmentSuffix)
		{
			return IndexFileNames.SegmentFileName(segment, segmentSuffix, POSTINGS_EXTENSION);
		}
	}
}
