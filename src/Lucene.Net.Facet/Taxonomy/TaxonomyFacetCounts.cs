/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Facet;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Facet.Taxonomy
{
	/// <summary>
	/// Reads from any
	/// <see cref="OrdinalsReader">OrdinalsReader</see>
	/// ; use
	/// <see cref="FastTaxonomyFacetCounts">FastTaxonomyFacetCounts</see>
	/// if you are using the
	/// default encoding from
	/// <see cref="Lucene.Net.Index.BinaryDocValues">Lucene.Net.Index.BinaryDocValues
	/// 	</see>
	/// .
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class TaxonomyFacetCounts : IntTaxonomyFacets
	{
		private readonly OrdinalsReader ordinalsReader;

		/// <summary>
		/// Create
		/// <code>TaxonomyFacetCounts</code>
		/// , which also
		/// counts all facet labels.  Use this for a non-default
		/// <see cref="OrdinalsReader">OrdinalsReader</see>
		/// ; otherwise use
		/// <see cref="FastTaxonomyFacetCounts">FastTaxonomyFacetCounts</see>
		/// .
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public TaxonomyFacetCounts(OrdinalsReader ordinalsReader, TaxonomyReader taxoReader
			, FacetsConfig config, FacetsCollector fc) : base(ordinalsReader.GetIndexFieldName
			(), taxoReader, config)
		{
			this.ordinalsReader = ordinalsReader;
			Count(fc.GetMatchingDocs());
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void Count(IList<FacetsCollector.MatchingDocs> matchingDocs)
		{
			IntsRef scratch = new IntsRef();
			foreach (FacetsCollector.MatchingDocs hits in matchingDocs)
			{
				OrdinalsReader.OrdinalsSegmentReader ords = ordinalsReader.GetReader(hits.context
					);
				DocIdSetIterator docs = hits.bits.Iterator();
				int doc;
				while ((doc = docs.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
				{
					ords.Get(doc, scratch);
					for (int i = 0; i < scratch.length; i++)
					{
						values[scratch.ints[scratch.offset + i]]++;
					}
				}
			}
			Rollup();
		}
	}
}
