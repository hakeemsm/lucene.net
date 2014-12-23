namespace Lucene.Net.Codecs.Lucene40.TestFramework
{
	/// <summary>Read-write version of Lucene40Codec for testing</summary>
	public sealed class Lucene40RWCodec : Lucene40Codec
	{
		private sealed class AnonLucene40FieldsFormat : Lucene40FieldInfosFormat
		{
		    /// <exception cref="System.IO.IOException"></exception>
			public override FieldInfosWriter FieldInfosWriter
			{
		        get
		        {
		            return !LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE ? 
                            base.FieldInfosWriter : 
                            new Lucene40FieldInfosWriter();
		        }
			}
		}

		private readonly FieldInfosFormat fieldInfos = new AnonLucene40FieldsFormat();

		private readonly DocValuesFormat docValues = new Lucene40RWDocValuesFormat();

		private readonly NormsFormat norms = new Lucene40RWNormsFormat();

		public override FieldInfosFormat FieldInfosFormat
		{
		    get { return fieldInfos; }
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
