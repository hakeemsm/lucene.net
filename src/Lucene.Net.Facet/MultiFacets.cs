/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Facet;
using Sharpen;

namespace Lucene.Net.Facet
{
	/// <summary>
	/// Maps specified dims to provided Facets impls; else, uses
	/// the default Facets impl.
	/// </summary>
	/// <remarks>
	/// Maps specified dims to provided Facets impls; else, uses
	/// the default Facets impl.
	/// </remarks>
	public class MultiFacets : Facets
	{
		private readonly IDictionary<string, Facets> dimToFacets;

		private readonly Facets defaultFacets;

		/// <summary>
		/// Create this, with no default
		/// <see cref="Facets">Facets</see>
		/// .
		/// </summary>
		public MultiFacets(IDictionary<string, Facets> dimToFacets) : this(dimToFacets, null
			)
		{
		}

		/// <summary>
		/// Create this, with the specified default
		/// <see cref="Facets">Facets</see>
		/// for fields not included in
		/// <code>dimToFacets</code>
		/// .
		/// </summary>
		public MultiFacets(IDictionary<string, Facets> dimToFacets, Facets defaultFacets)
		{
			this.dimToFacets = dimToFacets;
			this.defaultFacets = defaultFacets;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FacetResult GetTopChildren(int topN, string dim, params string[] 
			path)
		{
			Facets facets = dimToFacets.Get(dim);
			if (facets == null)
			{
				if (defaultFacets == null)
				{
					throw new ArgumentException("invalid dim \"" + dim + "\"");
				}
				facets = defaultFacets;
			}
			return facets.GetTopChildren(topN, dim, path);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Number GetSpecificValue(string dim, params string[] path)
		{
			Facets facets = dimToFacets.Get(dim);
			if (facets == null)
			{
				if (defaultFacets == null)
				{
					throw new ArgumentException("invalid dim \"" + dim + "\"");
				}
				facets = defaultFacets;
			}
			return facets.GetSpecificValue(dim, path);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override IList<FacetResult> GetAllDims(int topN)
		{
			IList<FacetResult> results = new AList<FacetResult>();
			// First add the specific dim's facets:
			foreach (KeyValuePair<string, Facets> ent in dimToFacets.EntrySet())
			{
				results.AddItem(ent.Value.GetTopChildren(topN, ent.Key));
			}
			if (defaultFacets != null)
			{
				// Then add all default facets as long as we didn't
				// already add that dim:
				foreach (FacetResult result in defaultFacets.GetAllDims(topN))
				{
					if (dimToFacets.ContainsKey(result.dim) == false)
					{
						results.AddItem(result);
					}
				}
			}
			return results;
		}
	}
}
