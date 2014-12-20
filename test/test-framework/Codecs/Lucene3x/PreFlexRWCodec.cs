/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Codecs.Lucene3x;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Codecs.Lucene3x
{
	/// <summary>Writes 3.x-like indexes (not perfect emulation yet) for testing only!</summary>
	/// <lucene.experimental></lucene.experimental>
	public class PreFlexRWCodec : Lucene3xCodec
	{
		private readonly Org.Apache.Lucene.Codecs.PostingsFormat postings = new PreFlexRWPostingsFormat
			();

		private readonly Lucene3xNormsFormat norms = new PreFlexRWNormsFormat();

		private readonly Org.Apache.Lucene.Codecs.FieldInfosFormat fieldInfos = new PreFlexRWFieldInfosFormat
			();

		private readonly Org.Apache.Lucene.Codecs.TermVectorsFormat termVectors = new PreFlexRWTermVectorsFormat
			();

		private readonly Org.Apache.Lucene.Codecs.SegmentInfoFormat segmentInfos = new PreFlexRWSegmentInfoFormat
			();

		private readonly Org.Apache.Lucene.Codecs.StoredFieldsFormat storedFields = new PreFlexRWStoredFieldsFormat
			();

		public override Org.Apache.Lucene.Codecs.PostingsFormat PostingsFormat()
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

		public override Org.Apache.Lucene.Codecs.NormsFormat NormsFormat()
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

		public override Org.Apache.Lucene.Codecs.SegmentInfoFormat SegmentInfoFormat()
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

		public override Org.Apache.Lucene.Codecs.FieldInfosFormat FieldInfosFormat()
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

		public override Org.Apache.Lucene.Codecs.TermVectorsFormat TermVectorsFormat()
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

		public override Org.Apache.Lucene.Codecs.StoredFieldsFormat StoredFieldsFormat()
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
