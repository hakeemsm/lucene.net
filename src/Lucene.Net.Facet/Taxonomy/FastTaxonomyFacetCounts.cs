/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Facet;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Facet.Taxonomy
{
	/// <summary>
	/// Computes facets counts, assuming the default encoding
	/// into DocValues was used.
	/// </summary>
	/// <remarks>
	/// Computes facets counts, assuming the default encoding
	/// into DocValues was used.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public class FastTaxonomyFacetCounts : IntTaxonomyFacets
	{
		/// <summary>
		/// Create
		/// <code>FastTaxonomyFacetCounts</code>
		/// , which also
		/// counts all facet labels.
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public FastTaxonomyFacetCounts(TaxonomyReader taxoReader, FacetsConfig config, FacetsCollector
			 fc) : this(FacetsConfig.DEFAULT_INDEX_FIELD_NAME, taxoReader, config, fc)
		{
		}

		/// <summary>
		/// Create
		/// <code>FastTaxonomyFacetCounts</code>
		/// , using the
		/// specified
		/// <code>indexFieldName</code>
		/// for ordinals.  Use
		/// this if you had set
		/// <see cref="Lucene.Net.Facet.FacetsConfig.SetIndexFieldName(string, string)
		/// 	">Lucene.Net.Facet.FacetsConfig.SetIndexFieldName(string, string)</see>
		/// to change the index
		/// field name for certain dimensions.
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public FastTaxonomyFacetCounts(string indexFieldName, TaxonomyReader taxoReader, 
			FacetsConfig config, FacetsCollector fc) : base(indexFieldName, taxoReader, config
			)
		{
			Count(fc.GetMatchingDocs());
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void Count(IList<FacetsCollector.MatchingDocs> matchingDocs)
		{
			foreach (FacetsCollector.MatchingDocs hits in matchingDocs)
			{
				BinaryDocValues dv = ((AtomicReader)hits.context.Reader()).GetBinaryDocValues(indexFieldName
					);
				if (dv == null)
				{
					// this reader does not have DocValues for the requested category list
					continue;
				}
				BytesRef scratch = new BytesRef();
				DocIdSetIterator docs = hits.bits.Iterator();
				int doc;
				while ((doc = docs.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
				{
					dv.Get(doc, scratch);
					byte[] bytes = scratch.bytes;
					int end = scratch.offset + scratch.length;
					int ord = 0;
					int offset = scratch.offset;
					int prev = 0;
					while (offset < end)
					{
						byte b = bytes[offset++];
						if (b >= 0)
						{
							prev = ord = ((ord << 7) | b) + prev;
							++values[ord];
							ord = 0;
						}
						else
						{
							ord = (ord << 7) | (b & unchecked((int)(0x7F)));
						}
					}
				}
			}
			Rollup();
		}
	}
}
