/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Facet;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Facet
{
	/// <summary>Collects hits for subsequent faceting, using sampling if needed.</summary>
	/// <remarks>
	/// Collects hits for subsequent faceting, using sampling if needed. Once you've
	/// run a search and collect hits into this, instantiate one of the
	/// <see cref="Facets">Facets</see>
	/// subclasses to do the facet counting. Note that this collector
	/// does not collect the scores of matching docs (i.e.
	/// <see cref="MatchingDocs.scores">MatchingDocs.scores</see>
	/// ) is
	/// <code>null</code>
	/// .
	/// <p>
	/// If you require the original set of hits, you can call
	/// <see cref="GetOriginalMatchingDocs()">GetOriginalMatchingDocs()</see>
	/// . Also, since the counts of the top-facets
	/// is based on the sampled set, you can amortize the counts by calling
	/// <see cref="AmortizeFacetCounts(FacetResult, FacetsConfig, Lucene.Net.Search.IndexSearcher)
	/// 	">AmortizeFacetCounts(FacetResult, FacetsConfig, Lucene.Net.Search.IndexSearcher)
	/// 	</see>
	/// .
	/// </remarks>
	public class RandomSamplingFacetsCollector : FacetsCollector
	{
		/// <summary>
		/// Faster alternative for java.util.Random, inspired by
		/// http://dmurphy747.wordpress.com/2011/03/23/xorshift-vs-random-
		/// performance-in-java/
		/// <p>
		/// Has a period of 2^64-1
		/// </summary>
		private class XORShift64Random
		{
			private long x;

			/// <summary>Creates a xorshift random generator using the provided seed</summary>
			public XORShift64Random(long seed)
			{
				x = seed == 0 ? unchecked((int)(0xdeadbeef)) : seed;
			}

			/// <summary>Get the next random long value</summary>
			public virtual long RandomLong()
			{
				x ^= (x << 21);
				x ^= ((long)(((ulong)x) >> 35));
				x ^= (x << 4);
				return x;
			}

			/// <summary>Get the next random int, between 0 (inclusive) and n (exclusive)</summary>
			public virtual int NextInt(int n)
			{
				int res = (int)(RandomLong() % n);
				return (res < 0) ? -res : res;
			}
		}

		private const int NOT_CALCULATED = -1;

		private readonly int sampleSize;

		private readonly RandomSamplingFacetsCollector.XORShift64Random random;

		private double samplingRate;

		private IList<FacetsCollector.MatchingDocs> sampledDocs;

		private int totalHits = NOT_CALCULATED;

		private int leftoverBin = NOT_CALCULATED;

		private int leftoverIndex = NOT_CALCULATED;

		/// <summary>Constructor with the given sample size and default seed.</summary>
		/// <remarks>Constructor with the given sample size and default seed.</remarks>
		/// <seealso cref="RandomSamplingFacetsCollector(int, long)">RandomSamplingFacetsCollector(int, long)
		/// 	</seealso>
		public RandomSamplingFacetsCollector(int sampleSize) : this(sampleSize, 0)
		{
		}

		/// <summary>Constructor with the given sample size and seed.</summary>
		/// <remarks>Constructor with the given sample size and seed.</remarks>
		/// <param name="sampleSize">
		/// The preferred sample size. If the number of hits is greater than
		/// the size, sampling will be done using a sample ratio of sampling
		/// size / totalN. For example: 1000 hits, sample size = 10 results in
		/// samplingRatio of 0.01. If the number of hits is lower, no sampling
		/// is done at all
		/// </param>
		/// <param name="seed">
		/// The random seed. If
		/// <code>0</code>
		/// then a seed will be chosen for you.
		/// </param>
		public RandomSamplingFacetsCollector(int sampleSize, long seed) : base(false)
		{
			this.sampleSize = sampleSize;
			this.random = new RandomSamplingFacetsCollector.XORShift64Random(seed);
			this.sampledDocs = null;
		}

		/// <summary>Returns the sampled list of the matching documents.</summary>
		/// <remarks>
		/// Returns the sampled list of the matching documents. Note that a
		/// <see cref="MatchingDocs">MatchingDocs</see>
		/// instance is returned per segment, even
		/// if no hits from that segment are included in the sampled set.
		/// <p>
		/// Note: One or more of the MatchingDocs might be empty (not containing any
		/// hits) as result of sampling.
		/// <p>
		/// Note:
		/// <code>MatchingDocs.totalHits</code>
		/// is copied from the original
		/// MatchingDocs, scores is set to
		/// <code>null</code>
		/// </remarks>
		public override IList<FacetsCollector.MatchingDocs> GetMatchingDocs()
		{
			IList<FacetsCollector.MatchingDocs> matchingDocs = base.GetMatchingDocs();
			if (totalHits == NOT_CALCULATED)
			{
				totalHits = 0;
				foreach (FacetsCollector.MatchingDocs md in matchingDocs)
				{
					totalHits += md.totalHits;
				}
			}
			if (totalHits <= sampleSize)
			{
				return matchingDocs;
			}
			if (sampledDocs == null)
			{
				samplingRate = (1.0 * sampleSize) / totalHits;
				sampledDocs = CreateSampledDocs(matchingDocs);
			}
			return sampledDocs;
		}

		/// <summary>Returns the original matching documents.</summary>
		/// <remarks>Returns the original matching documents.</remarks>
		public virtual IList<FacetsCollector.MatchingDocs> GetOriginalMatchingDocs()
		{
			return base.GetMatchingDocs();
		}

		/// <summary>Create a sampled copy of the matching documents list.</summary>
		/// <remarks>Create a sampled copy of the matching documents list.</remarks>
		private IList<FacetsCollector.MatchingDocs> CreateSampledDocs(IList<FacetsCollector.MatchingDocs
			> matchingDocsList)
		{
			IList<FacetsCollector.MatchingDocs> sampledDocsList = new AList<FacetsCollector.MatchingDocs
				>(matchingDocsList.Count);
			foreach (FacetsCollector.MatchingDocs docs in matchingDocsList)
			{
				sampledDocsList.AddItem(CreateSample(docs));
			}
			return sampledDocsList;
		}

		/// <summary>Create a sampled of the given hits.</summary>
		/// <remarks>Create a sampled of the given hits.</remarks>
		private FacetsCollector.MatchingDocs CreateSample(FacetsCollector.MatchingDocs docs
			)
		{
			int maxdoc = ((AtomicReader)docs.context.Reader()).MaxDoc();
			// TODO: we could try the WAH8DocIdSet here as well, as the results will be sparse
			FixedBitSet sampleDocs = new FixedBitSet(maxdoc);
			int binSize = (int)(1.0 / samplingRate);
			try
			{
				int counter = 0;
				int limit;
				int randomIndex;
				if (leftoverBin != NOT_CALCULATED)
				{
					limit = leftoverBin;
					// either NOT_CALCULATED, which means we already sampled from that bin,
					// or the next document to sample
					randomIndex = leftoverIndex;
				}
				else
				{
					limit = binSize;
					randomIndex = random.NextInt(binSize);
				}
				DocIdSetIterator it = docs.bits.Iterator();
				for (int doc = it.NextDoc(); doc != DocIdSetIterator.NO_MORE_DOCS; doc = it.NextDoc
					())
				{
					if (counter == randomIndex)
					{
						sampleDocs.Set(doc);
					}
					counter++;
					if (counter >= limit)
					{
						counter = 0;
						limit = binSize;
						randomIndex = random.NextInt(binSize);
					}
				}
				if (counter == 0)
				{
					// we either exhausted the bin and the iterator at the same time, or
					// this segment had no results. in the latter case we might want to
					// carry leftover to the next segment as is, but that complicates the
					// code and doesn't seem so important.
					leftoverBin = leftoverIndex = NOT_CALCULATED;
				}
				else
				{
					leftoverBin = limit - counter;
					if (randomIndex > counter)
					{
						// the document to sample is in the next bin
						leftoverIndex = randomIndex - counter;
					}
					else
					{
						if (randomIndex < counter)
						{
							// we sampled a document from the bin, so just skip over remaining
							// documents in the bin in the next segment.
							leftoverIndex = NOT_CALCULATED;
						}
					}
				}
				return new FacetsCollector.MatchingDocs(docs.context, sampleDocs, docs.totalHits, 
					null);
			}
			catch (IOException)
			{
				throw new RuntimeException();
			}
		}

		/// <summary>
		/// Note: if you use a counting
		/// <see cref="Facets">Facets</see>
		/// implementation, you can amortize the
		/// sampled counts by calling this method. Uses the
		/// <see cref="FacetsConfig">FacetsConfig</see>
		/// and
		/// the
		/// <see cref="Lucene.Net.Search.IndexSearcher">Lucene.Net.Search.IndexSearcher
		/// 	</see>
		/// to determine the upper bound for each facet value.
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual FacetResult AmortizeFacetCounts(FacetResult res, FacetsConfig config
			, IndexSearcher searcher)
		{
			if (res == null || totalHits <= sampleSize)
			{
				return res;
			}
			LabelAndValue[] fixedLabelValues = new LabelAndValue[res.labelValues.Length];
			IndexReader reader = searcher.GetIndexReader();
			FacetsConfig.DimConfig dimConfig = config.GetDimConfig(res.dim);
			// +2 to prepend dimension, append child label
			string[] childPath = new string[res.path.Length + 2];
			childPath[0] = res.dim;
			System.Array.Copy(res.path, 0, childPath, 1, res.path.Length);
			// reuse
			for (int i = 0; i < res.labelValues.Length; i++)
			{
				childPath[res.path.Length + 1] = res.labelValues[i].label;
				string fullPath = FacetsConfig.PathToString(childPath, childPath.Length);
				int max = reader.DocFreq(new Term(dimConfig.indexFieldName, fullPath));
				int correctedCount = (int)(res.labelValues[i].value / samplingRate);
				correctedCount = Math.Min(max, correctedCount);
				fixedLabelValues[i] = new LabelAndValue(res.labelValues[i].label, correctedCount);
			}
			// cap the total count on the total number of non-deleted documents in the reader
			int correctedTotalCount = res.value;
			if (correctedTotalCount > 0)
			{
				correctedTotalCount = Math.Min(reader.NumDocs(), (int)(res.value / samplingRate));
			}
			return new FacetResult(res.dim, res.path, correctedTotalCount, fixedLabelValues, 
				res.childCount);
		}

		/// <summary>Returns the sampling rate that was used.</summary>
		/// <remarks>Returns the sampling rate that was used.</remarks>
		public virtual double GetSamplingRate()
		{
			return samplingRate;
		}
	}
}
