using System;
using Lucene.Net.Codecs.Lucene40;
using Lucene.Net.Index;

namespace Lucene.Net.Codecs.Appending
{
	/// <summary>Appending postings impl.</summary>
	
	internal class AppendingPostingsFormat : PostingsFormat
	{
		public static string CODEC_NAME = "Appending";

		public AppendingPostingsFormat() : base(CODEC_NAME)
		{
		}

		
		public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
		{
			throw new NotSupportedException("this codec can only be used for reading");
		}

		
		public override FieldsProducer FieldsProducer(SegmentReadState state)
		{
			PostingsReaderBase postings = new Lucene40PostingsReader(state.directory, state.fieldInfos
				, state.segmentInfo, state.context, state.segmentSuffix);
			bool success = false;
			try
			{
				FieldsProducer ret = new AppendingTermsReader(state.directory
					, state.fieldInfos, state.segmentInfo, postings, state.context, state.segmentSuffix
					, state.termsIndexDivisor);
				success = true;
				return ret;
			}
			finally
			{
				if (!success)
				{
					postings.Dispose();
				}
			}
		}
	}
}
