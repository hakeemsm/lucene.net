/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Codecs.Lucene3x;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Lucene.Net.Codecs.Lucene3x
{
	/// <summary>Writes 3.x-like indexes (not perfect emulation yet) for testing only!</summary>
	/// <lucene.experimental></lucene.experimental>
	public class PreFlexRWCodec : Lucene3xCodec
	{
		private readonly Lucene.Net.Codecs.PostingsFormat postings = new PreFlexRWPostingsFormat
			();

		private readonly Lucene3xNormsFormat norms = new PreFlexRWNormsFormat();

		private readonly Lucene.Net.Codecs.FieldInfosFormat fieldInfos = new PreFlexRWFieldInfosFormat
			();

		private readonly Lucene.Net.Codecs.TermVectorsFormat termVectors = new PreFlexRWTermVectorsFormat
			();

		private readonly Lucene.Net.Codecs.SegmentInfoFormat segmentInfos = new PreFlexRWSegmentInfoFormat
			();

		private readonly Lucene.Net.Codecs.StoredFieldsFormat storedFields = new PreFlexRWStoredFieldsFormat
			();

		public override Lucene.Net.Codecs.PostingsFormat PostingsFormat()
		{
			if (LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE)
			{
				return postings;
			}
			else
			{
				return base.PostingsFormat();
			}
		}

		public override Lucene.Net.Codecs.NormsFormat NormsFormat()
		{
			if (LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE)
			{
				return norms;
			}
			else
			{
				return base.NormsFormat();
			}
		}

		public override Lucene.Net.Codecs.SegmentInfoFormat SegmentInfoFormat()
		{
			if (LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE)
			{
				return segmentInfos;
			}
			else
			{
				return base.SegmentInfoFormat();
			}
		}

		public override Lucene.Net.Codecs.FieldInfosFormat FieldInfosFormat()
		{
			if (LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE)
			{
				return fieldInfos;
			}
			else
			{
				return base.FieldInfosFormat();
			}
		}

		public override Lucene.Net.Codecs.TermVectorsFormat TermVectorsFormat()
		{
			if (LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE)
			{
				return termVectors;
			}
			else
			{
				return base.TermVectorsFormat();
			}
		}

		public override Lucene.Net.Codecs.StoredFieldsFormat StoredFieldsFormat()
		{
			if (LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE)
			{
				return storedFields;
			}
			else
			{
				return base.StoredFieldsFormat();
			}
		}
	}
}
