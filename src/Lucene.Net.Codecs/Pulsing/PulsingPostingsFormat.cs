/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Pulsing;
using Lucene.Net.Index;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Codecs.Pulsing
{
	/// <summary>
	/// This postings format "inlines" the postings for terms that have
	/// low docFreq.
	/// </summary>
	/// <remarks>
	/// This postings format "inlines" the postings for terms that have
	/// low docFreq.  It wraps another postings format, which is used for
	/// writing the non-inlined terms.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public abstract class PulsingPostingsFormat : PostingsFormat
	{
		private readonly int freqCutoff;

		private readonly int minBlockSize;

		private readonly int maxBlockSize;

		private readonly PostingsBaseFormat wrappedPostingsBaseFormat;

		public PulsingPostingsFormat(string name, PostingsBaseFormat wrappedPostingsBaseFormat
			, int freqCutoff) : this(name, wrappedPostingsBaseFormat, freqCutoff, BlockTreeTermsWriter
			.DEFAULT_MIN_BLOCK_SIZE, BlockTreeTermsWriter.DEFAULT_MAX_BLOCK_SIZE)
		{
		}

		/// <summary>
		/// Terms with freq &lt;= freqCutoff are inlined into terms
		/// dict.
		/// </summary>
		/// <remarks>
		/// Terms with freq &lt;= freqCutoff are inlined into terms
		/// dict.
		/// </remarks>
		public PulsingPostingsFormat(string name, PostingsBaseFormat wrappedPostingsBaseFormat
			, int freqCutoff, int minBlockSize, int maxBlockSize) : base(name)
		{
			this.freqCutoff = freqCutoff;
			this.minBlockSize = minBlockSize;
			//HM:revisit 
			//assert minBlockSize > 1;
			this.maxBlockSize = maxBlockSize;
			this.wrappedPostingsBaseFormat = wrappedPostingsBaseFormat;
		}

		public override string ToString()
		{
			return GetName() + "(freqCutoff=" + freqCutoff + " minBlockSize=" + minBlockSize 
				+ " maxBlockSize=" + maxBlockSize + ")";
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Lucene.Net.Codecs.FieldsConsumer FieldsConsumer(SegmentWriteState
			 state)
		{
			PostingsWriterBase docsWriter = null;
			// Terms that have <= freqCutoff number of docs are
			// "pulsed" (inlined):
			PostingsWriterBase pulsingWriter = null;
			// Terms dict
			bool success = false;
			try
			{
				docsWriter = wrappedPostingsBaseFormat.PostingsWriterBase(state);
				// Terms that have <= freqCutoff number of docs are
				// "pulsed" (inlined):
				pulsingWriter = new PulsingPostingsWriter(state, freqCutoff, docsWriter);
				Lucene.Net.Codecs.FieldsConsumer ret = new BlockTreeTermsWriter(state, pulsingWriter
					, minBlockSize, maxBlockSize);
				success = true;
				return ret;
			}
			finally
			{
				if (!success)
				{
					IOUtils.CloseWhileHandlingException(docsWriter, pulsingWriter);
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Lucene.Net.Codecs.FieldsProducer FieldsProducer(SegmentReadState
			 state)
		{
			PostingsReaderBase docsReader = null;
			PostingsReaderBase pulsingReader = null;
			bool success = false;
			try
			{
				docsReader = wrappedPostingsBaseFormat.PostingsReaderBase(state);
				pulsingReader = new PulsingPostingsReader(state, docsReader);
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
					IOUtils.CloseWhileHandlingException(docsReader, pulsingReader);
				}
			}
		}

		public virtual int GetFreqCutoff()
		{
			return freqCutoff;
		}
	}
}
