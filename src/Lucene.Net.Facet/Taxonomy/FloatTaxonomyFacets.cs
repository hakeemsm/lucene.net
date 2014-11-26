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
	/// <summary>
	/// Base class for all taxonomy-based facets that aggregate
	/// to a per-ords float[].
	/// </summary>
	/// <remarks>
	/// Base class for all taxonomy-based facets that aggregate
	/// to a per-ords float[].
	/// </remarks>
	public abstract class FloatTaxonomyFacets : TaxonomyFacets
	{
		/// <summary>Per-ordinal value.</summary>
		/// <remarks>Per-ordinal value.</remarks>
		protected internal readonly float[] values;

		/// <summary>Sole constructor.</summary>
		/// <remarks>Sole constructor.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal FloatTaxonomyFacets(string indexFieldName, TaxonomyReader taxoReader
			, FacetsConfig config) : base(indexFieldName, taxoReader, config)
		{
			values = new float[taxoReader.GetSize()];
		}

		/// <summary>Rolls up any single-valued hierarchical dimensions.</summary>
		/// <remarks>Rolls up any single-valued hierarchical dimensions.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual void Rollup()
		{
			// Rollup any necessary dims:
			foreach (KeyValuePair<string, FacetsConfig.DimConfig> ent in config.GetDimConfigs
				().EntrySet())
			{
				string dim = ent.Key;
				FacetsConfig.DimConfig ft = ent.Value;
				if (ft.hierarchical && ft.multiValued == false)
				{
					int dimRootOrd = taxoReader.GetOrdinal(new FacetLabel(dim));
					dimRootOrd > 0[dimRootOrd] += Rollup(children[dimRootOrd]);
				}
			}
		}

		private float Rollup(int ord)
		{
			float sum = 0;
			while (ord != TaxonomyReader.INVALID_ORDINAL)
			{
				float childValue = values[ord] + Rollup(children[ord]);
				values[ord] = childValue;
				sum += childValue;
				ord = siblings[ord];
			}
			return sum;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Number GetSpecificValue(string dim, params string[] path)
		{
			FacetsConfig.DimConfig dimConfig = VerifyDim(dim);
			if (path.Length == 0)
			{
				if (dimConfig.hierarchical && dimConfig.multiValued == false)
				{
				}
				else
				{
					// ok: rolled up at search time
					if (dimConfig.requireDimCount && dimConfig.multiValued)
					{
					}
					else
					{
						// ok: we indexed all ords at index time
						throw new ArgumentException("cannot return dimension-level value alone; use getTopChildren instead"
							);
					}
				}
			}
			int ord = taxoReader.GetOrdinal(new FacetLabel(dim, path));
			if (ord < 0)
			{
				return -1;
			}
			return values[ord];
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FacetResult GetTopChildren(int topN, string dim, params string[] 
			path)
		{
			if (topN <= 0)
			{
				throw new ArgumentException("topN must be > 0 (got: " + topN + ")");
			}
			FacetsConfig.DimConfig dimConfig = VerifyDim(dim);
			FacetLabel cp = new FacetLabel(dim, path);
			int dimOrd = taxoReader.GetOrdinal(cp);
			if (dimOrd == -1)
			{
				return null;
			}
			TopOrdAndFloatQueue q = new TopOrdAndFloatQueue(Math.Min(taxoReader.GetSize(), topN
				));
			float bottomValue = 0;
			int ord = children[dimOrd];
			float sumValues = 0;
			int childCount = 0;
			TopOrdAndFloatQueue.OrdAndValue reuse = null;
			while (ord != TaxonomyReader.INVALID_ORDINAL)
			{
				if (values[ord] > 0)
				{
					sumValues += values[ord];
					childCount++;
					if (values[ord] > bottomValue)
					{
						if (reuse == null)
						{
							reuse = new TopOrdAndFloatQueue.OrdAndValue();
						}
						reuse.ord = ord;
						reuse.value = values[ord];
						reuse = q.InsertWithOverflow(reuse);
						if (q.Size() == topN)
						{
							bottomValue = q.Top().value;
						}
					}
				}
				ord = siblings[ord];
			}
			if (sumValues == 0)
			{
				return null;
			}
			if (dimConfig.multiValued)
			{
				if (dimConfig.requireDimCount)
				{
					sumValues = values[dimOrd];
				}
				else
				{
					// Our sum'd count is not correct, in general:
					sumValues = -1;
				}
			}
			// Our sum'd dim count is accurate, so we keep it
			LabelAndValue[] labelValues = new LabelAndValue[q.Size()];
			for (int i = labelValues.Length - 1; i >= 0; i--)
			{
				TopOrdAndFloatQueue.OrdAndValue ordAndValue = q.Pop();
				FacetLabel child = taxoReader.GetPath(ordAndValue.ord);
				labelValues[i] = new LabelAndValue(child.components[cp.length], ordAndValue.value
					);
			}
			return new FacetResult(dim, path, sumValues, labelValues, childCount);
		}
	}
}
