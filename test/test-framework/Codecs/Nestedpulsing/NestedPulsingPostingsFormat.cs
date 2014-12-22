/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Codecs;
using Lucene.Net.Codecs.Lucene41;
using Lucene.Net.Codecs.Pulsing;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Lucene.Net.Codecs.Nestedpulsing
{
	/// <summary>Pulsing(1, Pulsing(2, Lucene41))</summary>
	/// <lucene.experimental></lucene.experimental>
	public sealed class NestedPulsingPostingsFormat : PostingsFormat
	{
		public NestedPulsingPostingsFormat() : base("NestedPulsing")
		{
		}

		// TODO: if we create PulsingPostingsBaseFormat then we
		// can simplify this? note: I don't like the *BaseFormat
		// hierarchy, maybe we can clean that up...
		/// <exception cref="System.IO.IOException"></exception>
		public override Lucene.Net.Codecs.FieldsConsumer FieldsConsumer(SegmentWriteState
			 state)
		{
			PostingsWriterBase docsWriter = null;
			PostingsWriterBase pulsingWriterInner = null;
			PostingsWriterBase pulsingWriter = null;
			// Terms dict
			bool success = false;
			try
			{
				docsWriter = new Lucene41PostingsWriter(state);
				pulsingWriterInner = new PulsingPostingsWriter(state, 2, docsWriter);
				pulsingWriter = new PulsingPostingsWriter(state, 1, pulsingWriterInner);
				Lucene.Net.Codecs.FieldsConsumer ret = new BlockTreeTermsWriter(state, pulsingWriter
					, BlockTreeTermsWriter.DEFAULT_MIN_BLOCK_SIZE, BlockTreeTermsWriter.DEFAULT_MAX_BLOCK_SIZE
					);
				success = true;
				return ret;
			}
			finally
			{
				if (!success)
				{
					IOUtils.CloseWhileHandlingException(docsWriter, pulsingWriterInner, pulsingWriter
						);
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Lucene.Net.Codecs.FieldsProducer FieldsProducer(SegmentReadState
			 state)
		{
			PostingsReaderBase docsReader = null;
			PostingsReaderBase pulsingReaderInner = null;
			PostingsReaderBase pulsingReader = null;
			bool success = false;
			try
			{
				docsReader = new Lucene41PostingsReader(state.directory, state.fieldInfos, state.
					segmentInfo, state.context, state.segmentSuffix);
				pulsingReaderInner = new PulsingPostingsReader(state, docsReader);
				pulsingReader = new PulsingPostingsReader(state, pulsingReaderInner);
				Lucene.Net.Codecs.FieldsProducer ret = new BlockTreeTermsReader(state.directory
					, state.fieldInfos, state.segmentInfo, pulsingReader, state.context, state.segmentSuffix
					, state.termsIndexDivisor);
				success = true;
				return ret;
			}
			finally
			{
				if (!success)
				{
					IOUtils.CloseWhileHandlingException(docsReader, pulsingReaderInner, pulsingReader
						);
				}
			}
		}
	}
}
