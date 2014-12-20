/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Codecs;
using Org.Apache.Lucene.Codecs.Lucene40;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Codecs.Lucene40
{
	/// <summary>
	/// Read-write version of
	/// <see cref="Lucene40PostingsFormat">Lucene40PostingsFormat</see>
	/// for testing.
	/// </summary>
	public class Lucene40RWPostingsFormat : Lucene40PostingsFormat
	{
		/// <exception cref="System.IO.IOException"></exception>
		public override Org.Apache.Lucene.Codecs.FieldsConsumer FieldsConsumer(SegmentWriteState
			 state)
		{
			if (!LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE)
			{
				return base.FieldsConsumer(state);
			}
			else
			{
				PostingsWriterBase docs = new Lucene40PostingsWriter(state);
				// TODO: should we make the terms index more easily
				// pluggable?  Ie so that this codec would record which
				// index impl was used, and switch on loading?
				// Or... you must make a new Codec for this?
				bool success = false;
				try
				{
					Org.Apache.Lucene.Codecs.FieldsConsumer ret = new BlockTreeTermsWriter(state, docs
						, minBlockSize, maxBlockSize);
					success = true;
					return ret;
				}
				finally
				{
					if (!success)
					{
						docs.Close();
					}
				}
			}
		}
	}
}
