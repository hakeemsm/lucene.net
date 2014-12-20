/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Codecs;
using Org.Apache.Lucene.Codecs.Blockterms;
using Org.Apache.Lucene.Codecs.Lucene41;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Codecs.Lucene41ords
{
	/// <summary>
	/// Customized version of
	/// <see cref="Org.Apache.Lucene.Codecs.Lucene41.Lucene41PostingsFormat">Org.Apache.Lucene.Codecs.Lucene41.Lucene41PostingsFormat
	/// 	</see>
	/// that uses
	/// <see cref="Org.Apache.Lucene.Codecs.Blockterms.FixedGapTermsIndexWriter">Org.Apache.Lucene.Codecs.Blockterms.FixedGapTermsIndexWriter
	/// 	</see>
	/// .
	/// </summary>
	public sealed class Lucene41WithOrds : PostingsFormat
	{
		public Lucene41WithOrds() : base("Lucene41WithOrds")
		{
		}

		// javadocs
		// TODO: we could make separate base class that can wrapp
		// any PostingsBaseFormat and make it ord-able...
		/// <exception cref="System.IO.IOException"></exception>
		public override Org.Apache.Lucene.Codecs.FieldsConsumer FieldsConsumer(SegmentWriteState
			 state)
		{
			PostingsWriterBase docs = new Lucene41PostingsWriter(state);
			// TODO: should we make the terms index more easily
			// pluggable?  Ie so that this codec would record which
			// index impl was used, and switch on loading?
			// Or... you must make a new Codec for this?
			TermsIndexWriterBase indexWriter;
			bool success = false;
			try
			{
				indexWriter = new FixedGapTermsIndexWriter(state);
				success = true;
			}
			finally
			{
				if (!success)
				{
					docs.Close();
				}
			}
			success = false;
			try
			{
				// Must use BlockTermsWriter (not BlockTree) because
				// BlockTree doens't support ords (yet)...
				Org.Apache.Lucene.Codecs.FieldsConsumer ret = new BlockTermsWriter(indexWriter, state
					, docs);
				success = true;
				return ret;
			}
			finally
			{
				if (!success)
				{
					try
					{
						docs.Close();
					}
					finally
					{
						indexWriter.Close();
					}
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Org.Apache.Lucene.Codecs.FieldsProducer FieldsProducer(SegmentReadState
			 state)
		{
			PostingsReaderBase postings = new Lucene41PostingsReader(state.directory, state.fieldInfos
				, state.segmentInfo, state.context, state.segmentSuffix);
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
					postings.Close();
				}
			}
			success = false;
			try
			{
				Org.Apache.Lucene.Codecs.FieldsProducer ret = new BlockTermsReader(indexReader, state
					.directory, state.fieldInfos, state.segmentInfo, postings, state.context, state.
					segmentSuffix);
				success = true;
				return ret;
			}
			finally
			{
				if (!success)
				{
					try
					{
						postings.Close();
					}
					finally
					{
						indexReader.Close();
					}
				}
			}
		}

		/// <summary>Extension of freq postings file</summary>
		internal static readonly string FREQ_EXTENSION = "frq";

		/// <summary>Extension of prox postings file</summary>
		internal static readonly string PROX_EXTENSION = "prx";
	}
}
