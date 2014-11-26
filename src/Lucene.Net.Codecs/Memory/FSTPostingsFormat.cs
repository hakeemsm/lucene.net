/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Lucene41;
using Lucene.Net.Codecs.Memory;
using Lucene.Net.Index;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Codecs.Memory
{
	/// <summary>FST term dict + Lucene41PBF</summary>
	public sealed class FSTPostingsFormat : PostingsFormat
	{
		public FSTPostingsFormat() : base("FST41")
		{
		}

		public override string ToString()
		{
			return GetName();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Lucene.Net.Codecs.FieldsConsumer FieldsConsumer(SegmentWriteState
			 state)
		{
			PostingsWriterBase postingsWriter = new Lucene41PostingsWriter(state);
			bool success = false;
			try
			{
				Lucene.Net.Codecs.FieldsConsumer ret = new FSTTermsWriter(state, postingsWriter
					);
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
				Lucene.Net.Codecs.FieldsProducer ret = new FSTTermsReader(state, postingsReader
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
