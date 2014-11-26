/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Facet;
using Lucene.Net.Facet.Sortedset;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Facet.Sortedset
{
	/// <summary>
	/// Compute facets counts from previously
	/// indexed
	/// <see cref="SortedSetDocValuesFacetField">SortedSetDocValuesFacetField</see>
	/// ,
	/// without require a separate taxonomy index.  Faceting is
	/// a bit slower (~25%), and there is added cost on every
	/// <see cref="Lucene.Net.Index.IndexReader">Lucene.Net.Index.IndexReader
	/// 	</see>
	/// open to create a new
	/// <see cref="SortedSetDocValuesReaderState">SortedSetDocValuesReaderState</see>
	/// .  Furthermore, this does
	/// not support hierarchical facets; only flat (dimension +
	/// label) facets, but it uses quite a bit less RAM to do
	/// so.
	/// <p><b>NOTE</b>: this class should be instantiated and
	/// then used from a single thread, because it holds a
	/// thread-private instance of
	/// <see cref="Lucene.Net.Index.SortedSetDocValues">Lucene.Net.Index.SortedSetDocValues
	/// 	</see>
	/// .
	/// <p><b>NOTE:</b>: tie-break is by unicode sort order
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class SortedSetDocValuesFacetCounts : Facets
	{
		internal readonly SortedSetDocValuesReaderState state;

		internal readonly SortedSetDocValues dv;

		internal readonly string field;

		internal readonly int[] counts;

		/// <summary>
		/// Sparse faceting: returns any dimension that had any
		/// hits, topCount labels per dimension.
		/// </summary>
		/// <remarks>
		/// Sparse faceting: returns any dimension that had any
		/// hits, topCount labels per dimension.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public SortedSetDocValuesFacetCounts(SortedSetDocValuesReaderState state, FacetsCollector
			 hits)
		{
			this.state = state;
			this.field = state.GetField();
			dv = state.GetDocValues();
			counts = new int[state.GetSize()];
			//System.out.println("field=" + field);
			Count(hits.GetMatchingDocs());
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FacetResult GetTopChildren(int topN, string dim, params string[] 
			path)
		{
			if (topN <= 0)
			{
				throw new ArgumentException("topN must be > 0 (got: " + topN + ")");
			}
			if (path.Length > 0)
			{
				throw new ArgumentException("path should be 0 length");
			}
			SortedSetDocValuesReaderState.OrdRange ordRange = state.GetOrdRange(dim);
			if (ordRange == null)
			{
				throw new ArgumentException("dimension \"" + dim + "\" was not indexed");
			}
			return GetDim(dim, ordRange, topN);
		}

		private FacetResult GetDim(string dim, SortedSetDocValuesReaderState.OrdRange ordRange
			, int topN)
		{
			TopOrdAndIntQueue q = null;
			int bottomCount = 0;
			int dimCount = 0;
			int childCount = 0;
			TopOrdAndIntQueue.OrdAndValue reuse = null;
			//System.out.println("getDim : " + ordRange.start + " - " + ordRange.end);
			for (int ord = ordRange.start; ord <= ordRange.end; ord++)
			{
				//System.out.println("  ord=" + ord + " count=" + counts[ord]);
				if (counts[ord] > 0)
				{
					dimCount += counts[ord];
					childCount++;
					if (counts[ord] > bottomCount)
					{
						if (reuse == null)
						{
							reuse = new TopOrdAndIntQueue.OrdAndValue();
						}
						reuse.ord = ord;
						reuse.value = counts[ord];
						if (q == null)
						{
							// Lazy init, so we don't create this for the
							// sparse case unnecessarily
							q = new TopOrdAndIntQueue(topN);
						}
						reuse = q.InsertWithOverflow(reuse);
						if (q.Size() == topN)
						{
							bottomCount = q.Top().value;
						}
					}
				}
			}
			if (q == null)
			{
				return null;
			}
			BytesRef scratch = new BytesRef();
			LabelAndValue[] labelValues = new LabelAndValue[q.Size()];
			for (int i = labelValues.Length - 1; i >= 0; i--)
			{
				TopOrdAndIntQueue.OrdAndValue ordAndValue = q.Pop();
				dv.LookupOrd(ordAndValue.ord, scratch);
				string[] parts = FacetsConfig.StringToPath(scratch.Utf8ToString());
				labelValues[i] = new LabelAndValue(parts[1], ordAndValue.value);
			}
			return new FacetResult(dim, new string[0], dimCount, labelValues, childCount);
		}

		/// <summary>Does all the "real work" of tallying up the counts.</summary>
		/// <remarks>Does all the "real work" of tallying up the counts.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		private void Count(IList<FacetsCollector.MatchingDocs> matchingDocs)
		{
			//System.out.println("ssdv count");
			MultiDocValues.OrdinalMap ordinalMap;
			// TODO: is this right?  really, we need a way to
			// verify that this ordinalMap "matches" the leaves in
			// matchingDocs...
			if (dv is MultiDocValues.MultiSortedSetDocValues && matchingDocs.Count > 1)
			{
				ordinalMap = ((MultiDocValues.MultiSortedSetDocValues)dv).mapping;
			}
			else
			{
				ordinalMap = null;
			}
			IndexReader origReader = state.GetOrigReader();
			foreach (FacetsCollector.MatchingDocs hits in matchingDocs)
			{
				AtomicReader reader = ((AtomicReader)hits.context.Reader());
				//System.out.println("  reader=" + reader);
				// LUCENE-5090: make sure the provided reader context "matches"
				// the top-level reader passed to the
				// SortedSetDocValuesReaderState, else cryptic
				// AIOOBE can happen:
				if (ReaderUtil.GetTopLevelContext(hits.context).Reader() != origReader)
				{
					throw new InvalidOperationException("the SortedSetDocValuesReaderState provided to this class does not match the reader being searched; you must create a new SortedSetDocValuesReaderState every time you open a new IndexReader"
						);
				}
				SortedSetDocValues segValues = reader.GetSortedSetDocValues(field);
				if (segValues == null)
				{
					continue;
				}
				DocIdSetIterator docs = hits.bits.Iterator();
				// TODO: yet another option is to count all segs
				// first, only in seg-ord space, and then do a
				// merge-sort-PQ in the end to only "resolve to
				// global" those seg ords that can compete, if we know
				// we just want top K?  ie, this is the same algo
				// that'd be used for merging facets across shards
				// (distributed faceting).  but this has much higher
				// temp ram req'ts (sum of number of ords across all
				// segs)
				if (ordinalMap != null)
				{
					int segOrd = hits.context.ord;
					int numSegOrds = (int)segValues.GetValueCount();
					if (hits.totalHits < numSegOrds / 10)
					{
						//System.out.println("    remap as-we-go");
						// Remap every ord to global ord as we iterate:
						int doc;
						while ((doc = docs.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
						{
							//System.out.println("    doc=" + doc);
							segValues.SetDocument(doc);
							int term = (int)segValues.NextOrd();
							while (term != SortedSetDocValues.NO_MORE_ORDS)
							{
								//System.out.println("      segOrd=" + segOrd + " ord=" + term + " globalOrd=" + ordinalMap.getGlobalOrd(segOrd, term));
								counts[(int)ordinalMap.GetGlobalOrd(segOrd, term)]++;
								term = (int)segValues.NextOrd();
							}
						}
					}
					else
					{
						//System.out.println("    count in seg ord first");
						// First count in seg-ord space:
						int[] segCounts = new int[numSegOrds];
						int doc;
						while ((doc = docs.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
						{
							//System.out.println("    doc=" + doc);
							segValues.SetDocument(doc);
							int term = (int)segValues.NextOrd();
							while (term != SortedSetDocValues.NO_MORE_ORDS)
							{
								//System.out.println("      ord=" + term);
								segCounts[term]++;
								term = (int)segValues.NextOrd();
							}
						}
						// Then, migrate to global ords:
						for (int ord = 0; ord < numSegOrds; ord++)
						{
							int count = segCounts[ord];
							if (count != 0)
							{
								//System.out.println("    migrate segOrd=" + segOrd + " ord=" + ord + " globalOrd=" + ordinalMap.getGlobalOrd(segOrd, ord));
								counts[(int)ordinalMap.GetGlobalOrd(segOrd, ord)] += count;
							}
						}
					}
				}
				else
				{
					// No ord mapping (e.g., single segment index):
					// just aggregate directly into counts:
					int doc;
					while ((doc = docs.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
					{
						segValues.SetDocument(doc);
						int term = (int)segValues.NextOrd();
						while (term != SortedSetDocValues.NO_MORE_ORDS)
						{
							counts[term]++;
							term = (int)segValues.NextOrd();
						}
					}
				}
			}
		}

		public override Number GetSpecificValue(string dim, params string[] path)
		{
			if (path.Length != 1)
			{
				throw new ArgumentException("path must be length=1");
			}
			int ord = (int)dv.LookupTerm(new BytesRef(FacetsConfig.PathToString(dim, path)));
			if (ord < 0)
			{
				return -1;
			}
			return counts[ord];
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override IList<FacetResult> GetAllDims(int topN)
		{
			IList<FacetResult> results = new AList<FacetResult>();
			foreach (KeyValuePair<string, SortedSetDocValuesReaderState.OrdRange> ent in state
				.GetPrefixToOrdRange().EntrySet())
			{
				FacetResult fr = GetDim(ent.Key, ent.Value, topN);
				if (fr != null)
				{
					results.AddItem(fr);
				}
			}
			// Sort by highest count:
			results.Sort(new _IComparer_279());
			return results;
		}

		private sealed class _IComparer_279 : IComparer<FacetResult>
		{
			public _IComparer_279()
			{
			}

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
	}
}
