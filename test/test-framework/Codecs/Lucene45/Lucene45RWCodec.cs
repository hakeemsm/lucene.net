using Lucene.Net.Codecs.Lucene42;
using Lucene.Net.Codecs.Lucene42.TestFramework;

namespace Lucene.Net.Codecs.Lucene45.TestFramework
{
	/// <summary>
	/// Read-write version of
	/// <see cref="Lucene45Codec">Lucene45Codec</see>
	/// for testing.
	/// </summary>
	public class Lucene45RWCodec : Lucene45Codec
	{
		private sealed class AnonLucene42FieldInfosFormat : Lucene42FieldInfosFormat
		{
		    /// <exception cref="System.IO.IOException"></exception>
			public override FieldInfosWriter FieldInfosWriter
			{
		        get
		        {
		            return !LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE ? base.FieldInfosWriter : new Lucene42FieldInfosWriter();
		        }
			}
		}

		private readonly FieldInfosFormat fieldInfosFormat = new AnonLucene42FieldInfosFormat();

		public override FieldInfosFormat FieldInfosFormat
		{
		    get { return fieldInfosFormat; }
		}
	}
}
