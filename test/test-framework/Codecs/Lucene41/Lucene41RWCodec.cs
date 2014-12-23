using Lucene.Net.Codecs.Lucene40;
using Lucene.Net.Codecs.Lucene40.TestFramework;

namespace Lucene.Net.Codecs.Lucene41.TestFramrwork
{
	/// <summary>
	/// Read-write version of
	/// <see cref="Lucene41Codec">Lucene41Codec</see>
	/// for testing.
	/// </summary>
	public class Lucene41RWCodec : Lucene41Codec
	{
		private readonly Lucene.Net.Codecs.StoredFieldsFormat fieldsFormat = new Lucene41StoredFieldsFormat
			();

		private sealed class _Lucene40FieldInfosFormat_39 : Lucene40FieldInfosFormat
		{
			public _Lucene40FieldInfosFormat_39()
			{
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override FieldInfosWriter FieldInfosWriter
			{
			    get
			    {
			        return !LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE ? base.FieldInfosWriter : new Lucene40FieldInfosWriter();
			    }
			}
		}

		private readonly FieldInfosFormat fieldInfos = new _Lucene40FieldInfosFormat_39();

		private readonly DocValuesFormat docValues = new Lucene40RWDocValuesFormat();

		private readonly NormsFormat norms = new Lucene40RWNormsFormat();

		public override FieldInfosFormat FieldInfosFormat
		{
		    get { return fieldInfos; }
		}

		public override StoredFieldsFormat StoredFieldsFormat
		{
		    get { return fieldsFormat; }
		}

		public override DocValuesFormat DocValuesFormat
		{
		    get { return docValues; }
		}

		public override NormsFormat NormsFormat
		{
		    get { return norms; }
		}
	}
}
