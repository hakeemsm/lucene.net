using Lucene.Net.Codecs.Lucene41;
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Codecs.Memory
{
	/// <summary>FSTOrd term dict + Lucene41PBF</summary>
	public sealed class FSTOrdPostingsFormat : PostingsFormat
	{
		public FSTOrdPostingsFormat() : base("FSTOrd41")
		{
		}

		public override string ToString()
		{
			return Name;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Lucene.Net.Codecs.FieldsConsumer FieldsConsumer(SegmentWriteState
			 state)
		{
			PostingsWriterBase postingsWriter = new Lucene41PostingsWriter(state);
			bool success = false;
			try
			{
				FieldsConsumer ret = new FSTOrdTermsWriter(state, postingsWriter);
				success = true;
				return ret;
			}
			finally
			{
				if (!success)
				{
					IOUtils.CloseWhileHandlingException(postingsWriter);
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Lucene.Net.Codecs.FieldsProducer FieldsProducer(SegmentReadState
			 state)
		{
			PostingsReaderBase postingsReader = new Lucene41PostingsReader(state.directory, state
				.fieldInfos, state.segmentInfo, state.context, state.segmentSuffix);
			bool success = false;
			try
			{
				Lucene.Net.Codecs.FieldsProducer ret = new FSTOrdTermsReader(state, postingsReader
					);
				success = true;
				return ret;
			}
			finally
			{
				if (!success)
				{
					IOUtils.CloseWhileHandlingException(postingsReader);
				}
			}
		}
	}
}
