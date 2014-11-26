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
	/// <summary>plain text index format.</summary>
	/// <remarks>
	/// plain text index format.
	/// <p>
	/// <b><font color="red">FOR RECREATIONAL USE ONLY</font></B>
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public sealed class SimpleTextCodec : Codec
	{
		private readonly Lucene.Net.Codecs.PostingsFormat postings = new SimpleTextPostingsFormat
			();

		private readonly Lucene.Net.Codecs.StoredFieldsFormat storedFields = new SimpleTextStoredFieldsFormat
			();

		private readonly Lucene.Net.Codecs.SegmentInfoFormat segmentInfos = new SimpleTextSegmentInfoFormat
			();

		private readonly Lucene.Net.Codecs.FieldInfosFormat fieldInfosFormat = new 
			SimpleTextFieldInfosFormat();

		private readonly Lucene.Net.Codecs.TermVectorsFormat vectorsFormat = new SimpleTextTermVectorsFormat
			();

		private readonly Lucene.Net.Codecs.NormsFormat normsFormat = new SimpleTextNormsFormat
			();

		private readonly Lucene.Net.Codecs.LiveDocsFormat liveDocs = new SimpleTextLiveDocsFormat
			();

		private readonly Lucene.Net.Codecs.DocValuesFormat dvFormat = new SimpleTextDocValuesFormat
			();

		public SimpleTextCodec() : base("SimpleText")
		{
		}

		public override Lucene.Net.Codecs.PostingsFormat PostingsFormat()
		{
			return postings;
		}

		public override Lucene.Net.Codecs.StoredFieldsFormat StoredFieldsFormat()
		{
			return storedFields;
		}

		public override Lucene.Net.Codecs.TermVectorsFormat TermVectorsFormat()
		{
			return vectorsFormat;
		}

		public override Lucene.Net.Codecs.FieldInfosFormat FieldInfosFormat()
		{
			return fieldInfosFormat;
		}

		public override Lucene.Net.Codecs.SegmentInfoFormat SegmentInfoFormat()
		{
			return segmentInfos;
		}

		public override Lucene.Net.Codecs.NormsFormat NormsFormat()
		{
			return normsFormat;
		}

		public override Lucene.Net.Codecs.LiveDocsFormat LiveDocsFormat()
		{
			return liveDocs;
		}

		public override Lucene.Net.Codecs.DocValuesFormat DocValuesFormat()
		{
			return dvFormat;
		}
	}
}
