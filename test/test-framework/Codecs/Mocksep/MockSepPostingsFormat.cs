/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Codecs;
using Lucene.Net.Codecs.Blockterms;
using Lucene.Net.Codecs.Mocksep;
using Lucene.Net.Codecs.Sep;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Lucene.Net.Codecs.Mocksep
{
	/// <summary>
	/// A silly codec that simply writes each file separately as
	/// single vInts.
	/// </summary>
	/// <remarks>
	/// A silly codec that simply writes each file separately as
	/// single vInts.  Don't use this (performance will be poor)!
	/// This is here just to test the core sep codec
	/// classes.
	/// </remarks>
	public sealed class MockSepPostingsFormat : PostingsFormat
	{
		public MockSepPostingsFormat() : base("MockSep")
		{
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Lucene.Net.Codecs.FieldsConsumer FieldsConsumer(SegmentWriteState
			 state)
		{
			PostingsWriterBase postingsWriter = new SepPostingsWriter(state, new MockSingleIntFactory
				());
			bool success = false;
			TermsIndexWriterBase indexWriter;
			try
			{
				indexWriter = new FixedGapTermsIndexWriter(state);
				success = true;
			}
			finally
			{
				if (!success)
				{
					postingsWriter.Close();
				}
			}
			success = false;
			try
			{
				Lucene.Net.Codecs.FieldsConsumer ret = new BlockTermsWriter(indexWriter, state
					, postingsWriter);
				success = true;
				return ret;
			}
			finally
			{
				if (!success)
				{
					try
					{
						postingsWriter.Close();
					}
					finally
					{
						indexWriter.Close();
					}
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Lucene.Net.Codecs.FieldsProducer FieldsProducer(SegmentReadState
			 state)
		{
			PostingsReaderBase postingsReader = new SepPostingsReader(state.directory, state.
				fieldInfos, state.segmentInfo, state.context, new MockSingleIntFactory(), state.
				segmentSuffix);
			TermsIndexReaderBase indexReader;
			bool success = false;
			try
			{
				indexReader = new FixedGapTermsIndexReader(state.directory, state.fieldInfos, state
					.segmentInfo.name, state.termsIndexDivisor, BytesRef.GetUTF8SortedAsUnicodeComparator
					(), state.segmentSuffix, state.context);
				success = true;
			}
			finally
			{
				if (!success)
				{
					postingsReader.Close();
				}
			}
			success = false;
			try
			{
				Lucene.Net.Codecs.FieldsProducer ret = new BlockTermsReader(indexReader, state
					.directory, state.fieldInfos, state.segmentInfo, postingsReader, state.context, 
					state.segmentSuffix);
				success = true;
				return ret;
			}
			finally
			{
				if (!success)
				{
					try
					{
						postingsReader.Close();
					}
					finally
					{
						indexReader.Close();
					}
				}
			}
		}
	}
}
