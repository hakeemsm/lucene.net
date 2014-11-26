/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Lucene41;
using Lucene.Net.Codecs.Memory;
using Lucene.Net.Codecs.Pulsing;
using Lucene.Net.Index;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Codecs.Memory
{
	/// <summary>
	/// FST + Pulsing41, test only, since
	/// FST does no delta encoding here!
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class FSTPulsing41PostingsFormat : PostingsFormat
	{
		private readonly PostingsBaseFormat wrappedPostingsBaseFormat;

		private readonly int freqCutoff;

		public FSTPulsing41PostingsFormat() : this(1)
		{
		}

		public FSTPulsing41PostingsFormat(int freqCutoff) : base("FSTPulsing41")
		{
			this.wrappedPostingsBaseFormat = new Lucene41PostingsBaseFormat();
			this.freqCutoff = freqCutoff;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Lucene.Net.Codecs.FieldsConsumer FieldsConsumer(SegmentWriteState
			 state)
		{
			PostingsWriterBase docsWriter = null;
			PostingsWriterBase pulsingWriter = null;
			bool success = false;
			try
			{
				docsWriter = wrappedPostingsBaseFormat.PostingsWriterBase(state);
				pulsingWriter = new PulsingPostingsWriter(state, freqCutoff, docsWriter);
				Lucene.Net.Codecs.FieldsConsumer ret = new FSTTermsWriter(state, pulsingWriter
					);
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
				Lucene.Net.Codecs.FieldsProducer ret = new FSTTermsReader(state, pulsingReader
					);
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
	}
}
