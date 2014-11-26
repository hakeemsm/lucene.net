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
	/// Aggregates sum of int values previously indexed with
	/// <see cref="IntAssociationFacetField">IntAssociationFacetField</see>
	/// , assuming the default
	/// encoding.
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class TaxonomyFacetSumIntAssociations : IntTaxonomyFacets
	{
		/// <summary>
		/// Create
		/// <code>TaxonomyFacetSumIntAssociations</code>
		/// against
		/// the default index field.
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public TaxonomyFacetSumIntAssociations(TaxonomyReader taxoReader, FacetsConfig config
			, FacetsCollector fc) : this(FacetsConfig.DEFAULT_INDEX_FIELD_NAME, taxoReader, 
			config, fc)
		{
		}

		/// <summary>
		/// Create
		/// <code>TaxonomyFacetSumIntAssociations</code>
		/// against
		/// the specified index field.
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public TaxonomyFacetSumIntAssociations(string indexFieldName, TaxonomyReader taxoReader
			, FacetsConfig config, FacetsCollector fc) : base(indexFieldName, taxoReader, config
			)
		{
			SumValues(fc.GetMatchingDocs());
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void SumValues(IList<FacetsCollector.MatchingDocs> matchingDocs)
		{
			//System.out.println("count matchingDocs=" + matchingDocs + " facetsField=" + facetsFieldName);
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
					//System.out.println("  doc=" + doc);
					// TODO: use OrdinalsReader?  we'd need to add a
					// BytesRef getAssociation()?
					dv.Get(doc, scratch);
					byte[] bytes = scratch.bytes;
					int end = scratch.offset + scratch.length;
					int offset = scratch.offset;
					while (offset < end)
					{
						int ord = ((bytes[offset] & unchecked((int)(0xFF))) << 24) | ((bytes[offset + 1] 
							& unchecked((int)(0xFF))) << 16) | ((bytes[offset + 2] & unchecked((int)(0xFF)))
							 << 8) | (bytes[offset + 3] & unchecked((int)(0xFF)));
						offset += 4;
						int value = ((bytes[offset] & unchecked((int)(0xFF))) << 24) | ((bytes[offset + 1
							] & unchecked((int)(0xFF))) << 16) | ((bytes[offset + 2] & unchecked((int)(0xFF)
							)) << 8) | (bytes[offset + 3] & unchecked((int)(0xFF)));
						offset += 4;
						values[ord] += value;
					}
				}
			}
		}
	}
}
