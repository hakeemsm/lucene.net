using Lucene.Net.Codecs.Lucene46;

namespace Lucene.Net.Codecs.Asserting.TestFramework
{
	/// <summary>
	/// Acts like
	/// <see cref="Lucene.Net.Codecs.Lucene46.Lucene46Codec">Lucene.Net.Codecs.Lucene46.Lucene46Codec
	/// 	</see>
	/// but with additional asserts.
	/// </summary>
	public sealed class AssertingCodec : FilterCodec
	{
		private readonly PostingsFormat postings = new AssertingPostingsFormat();

		private readonly TermVectorsFormat vectors = new AssertingTermVectorsFormat();

		private readonly StoredFieldsFormat storedFields = new AssertingStoredFieldsFormat();

		private readonly DocValuesFormat docValues = new AssertingDocValuesFormat();

		private readonly NormsFormat norms = new AssertingNormsFormat();

		public AssertingCodec() : base("Asserting", new Lucene46Codec())
		{
		}

		public override PostingsFormat PostingsFormat
		{
		    get { return postings; }
		}

		public override TermVectorsFormat TermVectorsFormat
		{
		    get { return vectors; }
		}

		public override StoredFieldsFormat StoredFieldsFormat
		{
		    get { return storedFields; }
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
