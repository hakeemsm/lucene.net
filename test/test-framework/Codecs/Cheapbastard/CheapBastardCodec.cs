using Lucene.Net.Codecs.Lucene40;
using Lucene.Net.Codecs.Lucene41;
using Lucene.Net.Codecs.Lucene46;

namespace Lucene.Net.Codecs.Cheapbastard.TestFramework
{
	/// <summary>Codec that tries to use as little ram as possible because he spent all his money on beer
	/// 	</summary>
	public class CheapBastardCodec : FilterCodec
	{
		private readonly PostingsFormat postings = new Lucene41PostingsFormat(100, 200);

		private readonly StoredFieldsFormat storedFields = new Lucene40StoredFieldsFormat();

		private readonly TermVectorsFormat termVectors = new Lucene40TermVectorsFormat();

		private readonly DocValuesFormat docValues = new DiskDocValuesFormat();

		private readonly NormsFormat norms = new DiskNormsFormat();

		public CheapBastardCodec() : base("CheapBastard", new Lucene46Codec())
		{
		}

		// TODO: better name :) 
		// but if we named it "LowMemory" in codecs/ package, it would be irresistible like optimize()!
		// TODO: would be better to have no terms index at all and bsearch a terms dict
		// uncompressing versions, waste lots of disk but no ram
		// these go to disk for all docvalues/norms datastructures
		public override Lucene.Net.Codecs.PostingsFormat PostingsFormat()
		{
			return postings;
		}

		public override Lucene.Net.Codecs.DocValuesFormat DocValuesFormat()
		{
			return docValues;
		}

		public override Lucene.Net.Codecs.NormsFormat NormsFormat()
		{
			return norms;
		}

		public override Lucene.Net.Codecs.StoredFieldsFormat StoredFieldsFormat()
		{
			return storedFields;
		}

		public override Lucene.Net.Codecs.TermVectorsFormat TermVectorsFormat()
		{
			return termVectors;
		}
	}
}
