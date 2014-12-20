/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Codecs;
using Org.Apache.Lucene.Codecs.Lucene42;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Codecs.Lucene42
{
	/// <summary>
	/// Read-write version of
	/// <see cref="Lucene42DocValuesFormat">Lucene42DocValuesFormat</see>
	/// for testing.
	/// </summary>
	public class Lucene42RWDocValuesFormat : Lucene42DocValuesFormat
	{
		/// <exception cref="System.IO.IOException"></exception>
		public override DocValuesConsumer FieldsConsumer(SegmentWriteState state)
		{
			if (!LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE)
			{
				return base.FieldsConsumer(state);
			}
			else
			{
				// note: we choose DEFAULT here (its reasonably fast, and for small bpv has tiny waste)
				return new Lucene42DocValuesConsumer(state, DATA_CODEC, DATA_EXTENSION, METADATA_CODEC
					, METADATA_EXTENSION, acceptableOverheadRatio);
			}
		}
	}
}
