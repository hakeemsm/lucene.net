namespace Lucene.Net.Codecs.Lucene42.TestFramework
{
	/// <summary>
	/// Read-write version of
	/// <see cref="Lucene42Codec">Lucene42Codec</see>
	/// for testing.
	/// </summary>
	public class Lucene42RWCodec : Lucene42Codec
	{
		private static readonly DocValuesFormat dv = new Lucene42RWDocValuesFormat();

		private static readonly NormsFormat norms = new Lucene42NormsFormat();

		private sealed class Lucene42FieldInfosFormatInner : Lucene42FieldInfosFormat
		{
		    
			public override FieldInfosWriter FieldInfosWriter
			{
			    get
			    {
			        return !LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE ? 
                            base.FieldInfosWriter : 
                            new Lucene42FieldInfosWriter();
			    }
			}
		}

		private readonly Lucene.Net.Codecs.FieldInfosFormat fieldInfosFormat = new 
			Lucene42FieldInfosFormatInner();

		public override DocValuesFormat GetDocValuesFormatForField(string field)
		{
			return dv;
		}

		public override NormsFormat NormsFormat
		{
		    get { return norms; }
		}

		public override FieldInfosFormat FieldInfosFormat
		{
		    get { return fieldInfosFormat; }
		}
	}
}
