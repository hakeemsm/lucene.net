/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Codecs;
using Org.Apache.Lucene.Codecs.Lucene40;
using Org.Apache.Lucene.Codecs.Lucene41;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Codecs.Lucene41
{
	/// <summary>
	/// Read-write version of
	/// <see cref="Lucene41Codec">Lucene41Codec</see>
	/// for testing.
	/// </summary>
	public class Lucene41RWCodec : Lucene41Codec
	{
		private readonly Org.Apache.Lucene.Codecs.StoredFieldsFormat fieldsFormat = new Lucene41StoredFieldsFormat
			();

		private sealed class _Lucene40FieldInfosFormat_39 : Lucene40FieldInfosFormat
		{
			public _Lucene40FieldInfosFormat_39()
			{
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override FieldInfosWriter GetFieldInfosWriter()
			{
				if (!LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE)
				{
					return base.GetFieldInfosWriter();
				}
				else
				{
					return new Lucene40FieldInfosWriter();
				}
			}
		}

		private readonly Org.Apache.Lucene.Codecs.FieldInfosFormat fieldInfos = new _Lucene40FieldInfosFormat_39
			();

		private readonly Org.Apache.Lucene.Codecs.DocValuesFormat docValues = new Lucene40RWDocValuesFormat
			();

		private readonly Org.Apache.Lucene.Codecs.NormsFormat norms = new Lucene40RWNormsFormat
			();

		public override Org.Apache.Lucene.Codecs.FieldInfosFormat FieldInfosFormat()
		{
			return fieldInfos;
		}

		public override Org.Apache.Lucene.Codecs.StoredFieldsFormat StoredFieldsFormat()
		{
			return fieldsFormat;
		}

		public override Org.Apache.Lucene.Codecs.DocValuesFormat DocValuesFormat()
		{
			return docValues;
		}

		public override Org.Apache.Lucene.Codecs.NormsFormat NormsFormat()
		{
			return norms;
		}
	}
}
