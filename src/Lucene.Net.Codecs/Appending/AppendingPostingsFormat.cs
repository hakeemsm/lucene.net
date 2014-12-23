/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Codecs.Lucene40;
using Lucene.Net.Index;

namespace Lucene.Net.Codecs.Appending
{
	/// <summary>Appending postings impl.</summary>
	/// <remarks>Appending postings impl.</remarks>
	internal class AppendingPostingsFormat : PostingsFormat
	{
		public static string CODEC_NAME = "Appending";

		public AppendingPostingsFormat() : base(CODEC_NAME)
		{
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Lucene.Net.Codecs.FieldsConsumer FieldsConsumer(SegmentWriteState
			 state)
		{
			throw new NotSupportedException("this codec can only be used for reading");
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Lucene.Net.Codecs.FieldsProducer FieldsProducer(SegmentReadState
			 state)
		{
			PostingsReaderBase postings = new Lucene40PostingsReader(state.directory, state.fieldInfos
				, state.segmentInfo, state.context, state.segmentSuffix);
			bool success = false;
			try
			{
				Lucene.Net.Codecs.FieldsProducer ret = new AppendingTermsReader(state.directory
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
