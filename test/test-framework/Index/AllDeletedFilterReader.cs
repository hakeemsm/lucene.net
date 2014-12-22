using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Lucene.Net.TestFramework.Index
{
	/// <summary>Filters the incoming reader and makes all documents appear deleted.</summary>
	/// <remarks>Filters the incoming reader and makes all documents appear deleted.</remarks>
	public class AllDeletedFilterReader : FilterAtomicReader
	{
		internal readonly IBits liveDocs;

		public AllDeletedFilterReader(AtomicReader input) : base(input)
		{
			liveDocs = new Bits.MatchNoBits(input.MaxDoc);
		}

		 
		//assert maxDoc() == 0 || hasDeletions();
		public override IBits LiveDocs
		{
		    get { return liveDocs; }
		}

		public override int NumDocs
		{
		    get { return 0; }
		}
	}
}
