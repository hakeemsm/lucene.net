/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Codecs;
using Org.Apache.Lucene.Codecs.Cranky;
using Sharpen;

namespace Org.Apache.Lucene.Codecs.Cranky
{
	/// <summary>Codec for testing that throws random IOExceptions</summary>
	public class CrankyCodec : FilterCodec
	{
		internal readonly Random random;

		/// <summary>Wrap the provided codec with crankiness.</summary>
		/// <remarks>
		/// Wrap the provided codec with crankiness.
		/// Try passing Asserting for the most fun.
		/// </remarks>
		public CrankyCodec(Codec delegate_, Random random) : base(delegate_.GetName(), delegate_
			)
		{
			// we impersonate the passed-in codec, so we don't need to be in SPI,
			// and so we dont change file formats
			this.random = random;
		}

		public override Org.Apache.Lucene.Codecs.DocValuesFormat DocValuesFormat()
		{
			return new CrankyDocValuesFormat(delegate_.DocValuesFormat(), random);
		}

		public override Org.Apache.Lucene.Codecs.FieldInfosFormat FieldInfosFormat()
		{
			return new CrankyFieldInfosFormat(delegate_.FieldInfosFormat(), random);
		}

		public override Org.Apache.Lucene.Codecs.LiveDocsFormat LiveDocsFormat()
		{
			return new CrankyLiveDocsFormat(delegate_.LiveDocsFormat(), random);
		}

		public override Org.Apache.Lucene.Codecs.NormsFormat NormsFormat()
		{
			return new CrankyNormsFormat(delegate_.NormsFormat(), random);
		}

		public override Org.Apache.Lucene.Codecs.PostingsFormat PostingsFormat()
		{
			return new CrankyPostingsFormat(delegate_.PostingsFormat(), random);
		}

		public override Org.Apache.Lucene.Codecs.SegmentInfoFormat SegmentInfoFormat()
		{
			return new CrankySegmentInfoFormat(delegate_.SegmentInfoFormat(), random);
		}

		public override Org.Apache.Lucene.Codecs.StoredFieldsFormat StoredFieldsFormat()
		{
			return new CrankyStoredFieldsFormat(delegate_.StoredFieldsFormat(), random);
		}

		public override Org.Apache.Lucene.Codecs.TermVectorsFormat TermVectorsFormat()
		{
			return new CrankyTermVectorsFormat(delegate_.TermVectorsFormat(), random);
		}

		public override string ToString()
		{
			return "Cranky(" + delegate_ + ")";
		}
	}
}
