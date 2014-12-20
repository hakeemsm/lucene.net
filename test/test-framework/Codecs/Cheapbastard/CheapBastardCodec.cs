/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Codecs;
using Org.Apache.Lucene.Codecs.Diskdv;
using Org.Apache.Lucene.Codecs.Lucene40;
using Org.Apache.Lucene.Codecs.Lucene41;
using Org.Apache.Lucene.Codecs.Lucene46;
using Sharpen;

namespace Org.Apache.Lucene.Codecs.Cheapbastard
{
	/// <summary>Codec that tries to use as little ram as possible because he spent all his money on beer
	/// 	</summary>
	public class CheapBastardCodec : FilterCodec
	{
		private readonly Org.Apache.Lucene.Codecs.PostingsFormat postings = new Lucene41PostingsFormat
			(100, 200);

		private readonly Org.Apache.Lucene.Codecs.StoredFieldsFormat storedFields = new Lucene40StoredFieldsFormat
			();

		private readonly Org.Apache.Lucene.Codecs.TermVectorsFormat termVectors = new Lucene40TermVectorsFormat
			();

		private readonly Org.Apache.Lucene.Codecs.DocValuesFormat docValues = new DiskDocValuesFormat
			();

		private readonly Org.Apache.Lucene.Codecs.NormsFormat norms = new DiskNormsFormat
			();

		public CheapBastardCodec() : base("CheapBastard", new Lucene46Codec())
		{
		}

		// TODO: better name :) 
		// but if we named it "LowMemory" in codecs/ package, it would be irresistible like optimize()!
		// TODO: would be better to have no terms index at all and bsearch a terms dict
		// uncompressing versions, waste lots of disk but no ram
		// these go to disk for all docvalues/norms datastructures
		public override Org.Apache.Lucene.Codecs.PostingsFormat PostingsFormat()
		{
			return postings;
		}

		public override Org.Apache.Lucene.Codecs.DocValuesFormat DocValuesFormat()
		{
			return docValues;
		}

		public override Org.Apache.Lucene.Codecs.NormsFormat NormsFormat()
		{
			return norms;
		}

		public override Org.Apache.Lucene.Codecs.StoredFieldsFormat StoredFieldsFormat()
		{
			return storedFields;
		}

		public override Org.Apache.Lucene.Codecs.TermVectorsFormat TermVectorsFormat()
		{
			return termVectors;
		}
	}
}
