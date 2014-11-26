/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Facet;
using Lucene.Net.Facet.Taxonomy;
using Sharpen;

namespace Lucene.Net.Facet.Taxonomy
{
	/// <summary>Base class for all taxonomy-based facets impls.</summary>
	/// <remarks>Base class for all taxonomy-based facets impls.</remarks>
	public abstract class TaxonomyFacets : Facets
	{
		private sealed class _IComparer_34 : IComparer<FacetResult>
		{
			public _IComparer_34()
			{
			}

			// javadocs
			public int Compare(FacetResult a, FacetResult b)
			{
				if (a.value > b.value)
				{
					return -1;
				}
				else
				{
					if (b.value > a.value)
					{
						return 1;
					}
					else
					{
						return Sharpen.Runtime.CompareOrdinal(a.dim, b.dim);
					}
				}
			}
		}

		private static readonly IComparer<FacetResult> BY_VALUE_THEN_DIM = new _IComparer_34
			();

		/// <summary>Index field name provided to the constructor.</summary>
		/// <remarks>Index field name provided to the constructor.</remarks>
		protected internal readonly string indexFieldName;

		/// <summary>
		/// <code>TaxonomyReader</code>
		/// provided to the constructor.
		/// </summary>
		protected internal readonly TaxonomyReader taxoReader;

		/// <summary>
		/// <code>FacetsConfig</code>
		/// provided to the constructor.
		/// </summary>
		protected internal readonly FacetsConfig config;

		/// <summary>
		/// Maps parent ordinal to its child, or -1 if the parent
		/// is childless.
		/// </summary>
		/// <remarks>
		/// Maps parent ordinal to its child, or -1 if the parent
		/// is childless.
		/// </remarks>
		protected internal readonly int[] children;

		/// <summary>
		/// Maps an ordinal to its sibling, or -1 if there is no
		/// sibling.
		/// </summary>
		/// <remarks>
		/// Maps an ordinal to its sibling, or -1 if there is no
		/// sibling.
		/// </remarks>
		protected internal readonly int[] siblings;

		/// <summary>Sole constructor.</summary>
		/// <remarks>Sole constructor.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal TaxonomyFacets(string indexFieldName, TaxonomyReader taxoReader
			, FacetsConfig config)
		{
			this.indexFieldName = indexFieldName;
			this.taxoReader = taxoReader;
			this.config = config;
			ParallelTaxonomyArrays pta = taxoReader.GetParallelTaxonomyArrays();
			children = pta.Children();
			siblings = pta.Siblings();
		}

		/// <summary>
		/// Throws
		/// <code>IllegalArgumentException</code>
		/// if the
		/// dimension is not recognized.  Otherwise, returns the
		/// <see cref="Lucene.Net.Facet.FacetsConfig.DimConfig">Lucene.Net.Facet.FacetsConfig.DimConfig
		/// 	</see>
		/// for this dimension.
		/// </summary>
		protected internal virtual FacetsConfig.DimConfig VerifyDim(string dim)
		{
			FacetsConfig.DimConfig dimConfig = config.GetDimConfig(dim);
			if (!dimConfig.indexFieldName.Equals(indexFieldName))
			{
				throw new ArgumentException("dimension \"" + dim + "\" was not indexed into field \""
					 + indexFieldName);
			}
			return dimConfig;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override IList<FacetResult> GetAllDims(int topN)
		{
			int ord = children[TaxonomyReader.ROOT_ORDINAL];
			IList<FacetResult> results = new AList<FacetResult>();
			while (ord != TaxonomyReader.INVALID_ORDINAL)
			{
				string dim = taxoReader.GetPath(ord).components[0];
				FacetsConfig.DimConfig dimConfig = config.GetDimConfig(dim);
				if (dimConfig.indexFieldName.Equals(indexFieldName))
				{
					FacetResult result = GetTopChildren(topN, dim);
					if (result != null)
					{
						results.AddItem(result);
					}
				}
				ord = siblings[ord];
			}
			// Sort by highest value, tie break by dim:
			results.Sort(BY_VALUE_THEN_DIM);
			return results;
		}
	}
}
