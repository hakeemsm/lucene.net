/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Codecs;
using Org.Apache.Lucene.Codecs.Asserting;
using Org.Apache.Lucene.Codecs.Lucene46;
using Sharpen;

namespace Org.Apache.Lucene.Codecs.Asserting
{
	/// <summary>
	/// Acts like
	/// <see cref="Org.Apache.Lucene.Codecs.Lucene46.Lucene46Codec">Org.Apache.Lucene.Codecs.Lucene46.Lucene46Codec
	/// 	</see>
	/// but with additional asserts.
	/// </summary>
	public sealed class AssertingCodec : FilterCodec
	{
		private readonly Org.Apache.Lucene.Codecs.PostingsFormat postings = new AssertingPostingsFormat
			();

		private readonly Org.Apache.Lucene.Codecs.TermVectorsFormat vectors = new AssertingTermVectorsFormat
			();

		private readonly Org.Apache.Lucene.Codecs.StoredFieldsFormat storedFields = new AssertingStoredFieldsFormat
			();

		private readonly Org.Apache.Lucene.Codecs.DocValuesFormat docValues = new AssertingDocValuesFormat
			();

		private readonly Org.Apache.Lucene.Codecs.NormsFormat norms = new AssertingNormsFormat
			();

		public AssertingCodec() : base("Asserting", new Lucene46Codec())
		{
		}

		public override Org.Apache.Lucene.Codecs.PostingsFormat PostingsFormat()
		{
			return postings;
		}

		public override Org.Apache.Lucene.Codecs.TermVectorsFormat TermVectorsFormat()
		{
			return vectors;
		}

		public override Org.Apache.Lucene.Codecs.StoredFieldsFormat StoredFieldsFormat()
		{
			return storedFields;
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
