/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Facet;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Facet
{
	internal class DrillSidewaysScorer : BulkScorer
	{
		private readonly Collector drillDownCollector;

		private readonly DrillSidewaysScorer.DocsAndCost[] dims;

		private readonly Scorer baseScorer;

		private readonly AtomicReaderContext context;

		internal readonly bool scoreSubDocsAtOnce;

		private const int CHUNK = 2048;

		private const int MASK = CHUNK - 1;

		private int collectDocID = -1;

		private float collectScore;

		internal DrillSidewaysScorer(AtomicReaderContext context, Scorer baseScorer, Collector
			 drillDownCollector, DrillSidewaysScorer.DocsAndCost[] dims, bool scoreSubDocsAtOnce
			)
		{
			//private static boolean DEBUG = false;
			// DrillDown DocsEnums:
			this.dims = dims;
			this.context = context;
			this.baseScorer = baseScorer;
			this.drillDownCollector = drillDownCollector;
			this.scoreSubDocsAtOnce = scoreSubDocsAtOnce;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override bool Score(Collector collector, int maxDoc)
		{
			if (maxDoc != int.MaxValue)
			{
				throw new ArgumentException("maxDoc must be Integer.MAX_VALUE");
			}
			//if (DEBUG) {
			//  System.out.println("\nscore: reader=" + context.reader());
			//}
			//System.out.println("score r=" + context.reader());
			DrillSidewaysScorer.FakeScorer scorer = new DrillSidewaysScorer.FakeScorer(this);
			collector.SetScorer(scorer);
			if (drillDownCollector != null)
			{
				drillDownCollector.SetScorer(scorer);
				drillDownCollector.SetNextReader(context);
			}
			foreach (DrillSidewaysScorer.DocsAndCost dim in dims)
			{
				dim.sidewaysCollector.SetScorer(scorer);
				dim.sidewaysCollector.SetNextReader(context);
			}
			// TODO: if we ever allow null baseScorer ... it will
			// mean we DO score docs out of order ... hmm, or if we
			// change up the order of the conjuntions below
			// Position all scorers to their first matching doc:
			baseScorer != null.NextDoc();
			int numBits = 0;
			foreach (DrillSidewaysScorer.DocsAndCost dim_1 in dims)
			{
				if (dim_1.disi != null)
				{
					dim_1.disi.NextDoc();
				}
				else
				{
					if (dim_1.bits != null)
					{
						numBits++;
					}
				}
			}
			int numDims = dims.Length;
			Bits[] bits = new Bits[numBits];
			Collector[] bitsSidewaysCollectors = new Collector[numBits];
			DocIdSetIterator[] disis = new DocIdSetIterator[numDims - numBits];
			Collector[] sidewaysCollectors = new Collector[numDims - numBits];
			long drillDownCost = 0;
			int disiUpto = 0;
			int bitsUpto = 0;
			for (int dim_2 = 0; dim_2 < numDims; dim_2++)
			{
				DocIdSetIterator disi = dims[dim_2].disi;
				if (dims[dim_2].bits == null)
				{
					disis[disiUpto] = disi;
					sidewaysCollectors[disiUpto] = dims[dim_2].sidewaysCollector;
					disiUpto++;
					if (disi != null)
					{
						drillDownCost += disi.Cost();
					}
				}
				else
				{
					bits[bitsUpto] = dims[dim_2].bits;
					bitsSidewaysCollectors[bitsUpto] = dims[dim_2].sidewaysCollector;
					bitsUpto++;
				}
			}
			long baseQueryCost = baseScorer.Cost();
			if (bitsUpto > 0 || scoreSubDocsAtOnce || baseQueryCost < drillDownCost / 10)
			{
				//System.out.println("queryFirst: baseScorer=" + baseScorer + " disis.length=" + disis.length + " bits.length=" + bits.length);
				DoQueryFirstScoring(collector, disis, sidewaysCollectors, bits, bitsSidewaysCollectors
					);
			}
			else
			{
				if (numDims > 1 && (dims[1].disi == null || dims[1].disi.Cost() < baseQueryCost /
					 10))
				{
					//System.out.println("drillDownAdvance");
					DoDrillDownAdvanceScoring(collector, disis, sidewaysCollectors);
				}
				else
				{
					//System.out.println("union");
					DoUnionScoring(collector, disis, sidewaysCollectors);
				}
			}
			return false;
		}

		/// <summary>
		/// Used when base query is highly constraining vs the
		/// drilldowns, or when the docs must be scored at once
		/// (i.e., like BooleanScorer2, not BooleanScorer).
		/// </summary>
		/// <remarks>
		/// Used when base query is highly constraining vs the
		/// drilldowns, or when the docs must be scored at once
		/// (i.e., like BooleanScorer2, not BooleanScorer).  In
		/// this case we just .next() on base and .advance() on
		/// the dim filters.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		private void DoQueryFirstScoring(Collector collector, DocIdSetIterator[] disis, Collector
			[] sidewaysCollectors, Bits[] bits, Collector[] bitsSidewaysCollectors)
		{
			//if (DEBUG) {
			//  System.out.println("  doQueryFirstScoring");
			//}
			int docID = baseScorer.DocID();
			while (docID != DocsEnum.NO_MORE_DOCS)
			{
				Collector failedCollector = null;
				for (int i = 0; i < disis.Length; i++)
				{
					// TODO: should we sort this 2nd dimension of
					// docsEnums from most frequent to least?
					DocIdSetIterator disi = disis[i];
					if (disi != null && disi.DocID() < docID)
					{
						disi.Advance(docID);
					}
					if (disi == null || disi.DocID() > docID)
					{
						if (failedCollector != null)
						{
							// More than one dim fails on this document, so
							// it's neither a hit nor a near-miss; move to
							// next doc:
							docID = baseScorer.NextDoc();
							goto nextDoc_continue;
						}
						else
						{
							failedCollector = sidewaysCollectors[i];
						}
					}
				}
				// TODO: for the "non-costly Bits" we really should
				// have passed them down as acceptDocs, but
				// unfortunately we cannot distinguish today betwen
				// "bits() is so costly that you should apply it last"
				// from "bits() is so cheap that you should apply it
				// everywhere down low"
				// Fold in Filter Bits last, since they may be costly:
				for (int i_1 = 0; i_1 < bits.Length; i_1++)
				{
					if (bits[i_1].Get(docID) == false)
					{
						if (failedCollector != null)
						{
							// More than one dim fails on this document, so
							// it's neither a hit nor a near-miss; move to
							// next doc:
							docID = baseScorer.NextDoc();
							goto nextDoc_continue;
						}
						else
						{
							failedCollector = bitsSidewaysCollectors[i_1];
						}
					}
				}
				collectDocID = docID;
				// TODO: we could score on demand instead since we are
				// daat here:
				collectScore = baseScorer.Score();
				if (failedCollector == null)
				{
					// Hit passed all filters, so it's "real":
					CollectHit(collector, sidewaysCollectors, bitsSidewaysCollectors);
				}
				else
				{
					// Hit missed exactly one filter:
					CollectNearMiss(failedCollector);
				}
				docID = baseScorer.NextDoc();
nextDoc_continue: ;
			}
nextDoc_break: ;
		}

		/// <summary>
		/// Used when drill downs are highly constraining vs
		/// baseQuery.
		/// </summary>
		/// <remarks>
		/// Used when drill downs are highly constraining vs
		/// baseQuery.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		private void DoDrillDownAdvanceScoring(Collector collector, DocIdSetIterator[] disis
			, Collector[] sidewaysCollectors)
		{
			int maxDoc = ((AtomicReader)context.Reader()).MaxDoc();
			int numDims = dims.Length;
			//if (DEBUG) {
			//  System.out.println("  doDrillDownAdvanceScoring");
			//}
			// TODO: maybe a class like BS, instead of parallel arrays
			int[] filledSlots = new int[CHUNK];
			int[] docIDs = new int[CHUNK];
			float[] scores = new float[CHUNK];
			int[] missingDims = new int[CHUNK];
			int[] counts = new int[CHUNK];
			docIDs[0] = -1;
			int nextChunkStart = CHUNK;
			FixedBitSet seen = new FixedBitSet(CHUNK);
			while (true)
			{
				//if (DEBUG) {
				//  System.out.println("\ncycle nextChunkStart=" + nextChunkStart + " docIds[0]=" + docIDs[0]);
				//}
				// First dim:
				//if (DEBUG) {
				//  System.out.println("  dim0");
				//}
				DocIdSetIterator disi = disis[0];
				if (disi != null)
				{
					int docID = disi.DocID();
					while (docID < nextChunkStart)
					{
						int slot = docID & MASK;
						if (docIDs[slot] != docID)
						{
							seen.Set(slot);
							// Mark slot as valid:
							//if (DEBUG) {
							//  System.out.println("    set docID=" + docID + " id=" + context.reader().document(docID).get("id"));
							//}
							docIDs[slot] = docID;
							missingDims[slot] = 1;
							counts[slot] = 1;
						}
						docID = disi.NextDoc();
					}
				}
				// Second dim:
				//if (DEBUG) {
				//  System.out.println("  dim1");
				//}
				disi = disis[1];
				if (disi != null)
				{
					int docID = disi.DocID();
					while (docID < nextChunkStart)
					{
						int slot = docID & MASK;
						if (docIDs[slot] != docID)
						{
							// Mark slot as valid:
							seen.Set(slot);
							//if (DEBUG) {
							//  System.out.println("    set docID=" + docID + " missingDim=0 id=" + context.reader().document(docID).get("id"));
							//}
							docIDs[slot] = docID;
							missingDims[slot] = 0;
							counts[slot] = 1;
						}
						else
						{
							// TODO: single-valued dims will always be true
							// below; we could somehow specialize
							if (missingDims[slot] >= 1)
							{
								missingDims[slot] = 2;
								counts[slot] = 2;
							}
							else
							{
								//if (DEBUG) {
								//  System.out.println("    set docID=" + docID + " missingDim=2 id=" + context.reader().document(docID).get("id"));
								//}
								counts[slot] = 1;
							}
						}
						//if (DEBUG) {
						//  System.out.println("    set docID=" + docID + " missingDim=" + missingDims[slot] + " id=" + context.reader().document(docID).get("id"));
						//}
						docID = disi.NextDoc();
					}
				}
				// After this we can "upgrade" to conjunction, because
				// any doc not seen by either dim 0 or dim 1 cannot be
				// a hit or a near miss:
				//if (DEBUG) {
				//  System.out.println("  baseScorer");
				//}
				// Fold in baseScorer, using advance:
				int filledCount = 0;
				int slot0 = 0;
				while (slot0 < CHUNK && (slot0 = seen.NextSetBit(slot0)) != -1)
				{
					int ddDocID = docIDs[slot0];
					int baseDocID = ddDocID != -1.DocID();
					if (baseDocID < ddDocID)
					{
						baseDocID = baseScorer.Advance(ddDocID);
					}
					if (baseDocID == ddDocID)
					{
						//if (DEBUG) {
						//  System.out.println("    keep docID=" + ddDocID + " id=" + context.reader().document(ddDocID).get("id"));
						//}
						scores[slot0] = baseScorer.Score();
						filledSlots[filledCount++] = slot0;
						counts[slot0]++;
					}
					else
					{
						//if (DEBUG) {
						//  System.out.println("    no docID=" + ddDocID + " id=" + context.reader().document(ddDocID).get("id"));
						//}
						docIDs[slot0] = -1;
					}
					// TODO: we could jump slot0 forward to the
					// baseDocID ... but we'd need to set docIDs for
					// intervening slots to -1
					slot0++;
				}
				seen.Clear(0, CHUNK);
				if (filledCount == 0)
				{
					if (nextChunkStart >= maxDoc)
					{
						break;
					}
					nextChunkStart += CHUNK;
					continue;
				}
				// TODO: factor this out & share w/ union scorer,
				// except we start from dim=2 instead:
				for (int dim = 2; dim < numDims; dim++)
				{
					//if (DEBUG) {
					//  System.out.println("  dim=" + dim + " [" + dims[dim].dim + "]");
					//}
					disi = disis[dim];
					if (disi != null)
					{
						int docID = disi.DocID();
						while (docID < nextChunkStart)
						{
							int slot = docID & MASK;
							if (docIDs[slot] == docID && counts[slot] >= dim)
							{
								// TODO: single-valued dims will always be true
								// below; we could somehow specialize
								if (missingDims[slot] >= dim)
								{
									//if (DEBUG) {
									//  System.out.println("    set docID=" + docID + " count=" + (dim+2));
									//}
									missingDims[slot] = dim + 1;
									counts[slot] = dim + 2;
								}
								else
								{
									//if (DEBUG) {
									//  System.out.println("    set docID=" + docID + " missing count=" + (dim+1));
									//}
									counts[slot] = dim + 1;
								}
							}
							// TODO: sometimes use advance?
							docID = disi.NextDoc();
						}
					}
				}
				// Collect:
				//if (DEBUG) {
				//  System.out.println("  now collect: " + filledCount + " hits");
				//}
				for (int i = 0; i < filledCount; i++)
				{
					int slot = filledSlots[i];
					collectDocID = docIDs[slot];
					collectScore = scores[slot];
					//if (DEBUG) {
					//  System.out.println("    docID=" + docIDs[slot] + " count=" + counts[slot]);
					//}
					if (counts[slot] == 1 + numDims)
					{
						CollectHit(collector, sidewaysCollectors);
					}
					else
					{
						if (counts[slot] == numDims)
						{
							CollectNearMiss(sidewaysCollectors[missingDims[slot]]);
						}
					}
				}
				if (nextChunkStart >= maxDoc)
				{
					break;
				}
				nextChunkStart += CHUNK;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void DoUnionScoring(Collector collector, DocIdSetIterator[] disis, Collector
			[] sidewaysCollectors)
		{
			//if (DEBUG) {
			//  System.out.println("  doUnionScoring");
			//}
			int maxDoc = ((AtomicReader)context.Reader()).MaxDoc();
			int numDims = dims.Length;
			// TODO: maybe a class like BS, instead of parallel arrays
			int[] filledSlots = new int[CHUNK];
			int[] docIDs = new int[CHUNK];
			float[] scores = new float[CHUNK];
			int[] missingDims = new int[CHUNK];
			int[] counts = new int[CHUNK];
			docIDs[0] = -1;
			// NOTE: this is basically a specialized version of
			// BooleanScorer, to the minShouldMatch=N-1 case, but
			// carefully tracking which dimension failed to match
			int nextChunkStart = CHUNK;
			while (true)
			{
				//if (DEBUG) {
				//  System.out.println("\ncycle nextChunkStart=" + nextChunkStart + " docIds[0]=" + docIDs[0]);
				//}
				int filledCount = 0;
				int docID = baseScorer.DocID();
				//if (DEBUG) {
				//  System.out.println("  base docID=" + docID);
				//}
				while (docID < nextChunkStart)
				{
					int slot = docID & MASK;
					//if (DEBUG) {
					//  System.out.println("    docIDs[slot=" + slot + "]=" + docID + " id=" + context.reader().document(docID).get("id"));
					//}
					// Mark slot as valid:
					//HM:revisit
					//assert docIDs[slot] != docID: "slot=" + slot + " docID=" + docID;
					docIDs[slot] = docID;
					scores[slot] = baseScorer.Score();
					filledSlots[filledCount++] = slot;
					missingDims[slot] = 0;
					counts[slot] = 1;
					docID = baseScorer.NextDoc();
				}
				if (filledCount == 0)
				{
					if (nextChunkStart >= maxDoc)
					{
						break;
					}
					nextChunkStart += CHUNK;
					continue;
				}
				// First drill-down dim, basically adds SHOULD onto
				// the baseQuery:
				//if (DEBUG) {
				//  System.out.println("  dim=0 [" + dims[0].dim + "]");
				//}
				DocIdSetIterator disi = disis[0];
				if (disi != null)
				{
					docID = disi.DocID();
					//if (DEBUG) {
					//  System.out.println("    start docID=" + docID);
					//}
					while (docID < nextChunkStart)
					{
						int slot = docID & MASK;
						if (docIDs[slot] == docID)
						{
							//if (DEBUG) {
							//  System.out.println("      set docID=" + docID + " count=2");
							//}
							missingDims[slot] = 1;
							counts[slot] = 2;
						}
						docID = disi.NextDoc();
					}
				}
				for (int dim = 1; dim < numDims; dim++)
				{
					//if (DEBUG) {
					//  System.out.println("  dim=" + dim + " [" + dims[dim].dim + "]");
					//}
					disi = disis[dim];
					if (disi != null)
					{
						docID = disi.DocID();
						//if (DEBUG) {
						//  System.out.println("    start docID=" + docID);
						//}
						while (docID < nextChunkStart)
						{
							int slot = docID & MASK;
							if (docIDs[slot] == docID && counts[slot] >= dim)
							{
								// This doc is still in the running...
								// TODO: single-valued dims will always be true
								// below; we could somehow specialize
								if (missingDims[slot] >= dim)
								{
									//if (DEBUG) {
									//  System.out.println("      set docID=" + docID + " count=" + (dim+2));
									//}
									missingDims[slot] = dim + 1;
									counts[slot] = dim + 2;
								}
								else
								{
									//if (DEBUG) {
									//  System.out.println("      set docID=" + docID + " missing count=" + (dim+1));
									//}
									counts[slot] = dim + 1;
								}
							}
							docID = disi.NextDoc();
						}
					}
				}
				// Collect:
				//System.out.println("  now collect: " + filledCount + " hits");
				for (int i = 0; i < filledCount; i++)
				{
					// NOTE: This is actually in-order collection,
					// because we only accept docs originally returned by
					// the baseScorer (ie that Scorer is AND'd)
					int slot = filledSlots[i];
					collectDocID = docIDs[slot];
					collectScore = scores[slot];
					//if (DEBUG) {
					//  System.out.println("    docID=" + docIDs[slot] + " count=" + counts[slot]);
					//}
					//System.out.println("  collect doc=" + collectDocID + " main.freq=" + (counts[slot]-1) + " main.doc=" + collectDocID + " exactCount=" + numDims);
					if (counts[slot] == 1 + numDims)
					{
						//System.out.println("    hit");
						CollectHit(collector, sidewaysCollectors);
					}
					else
					{
						if (counts[slot] == numDims)
						{
							//System.out.println("    sw");
							CollectNearMiss(sidewaysCollectors[missingDims[slot]]);
						}
					}
				}
				if (nextChunkStart >= maxDoc)
				{
					break;
				}
				nextChunkStart += CHUNK;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void CollectHit(Collector collector, Collector[] sidewaysCollectors)
		{
			//if (DEBUG) {
			//  System.out.println("      hit");
			//}
			collector.Collect(collectDocID);
			if (drillDownCollector != null)
			{
				drillDownCollector.Collect(collectDocID);
			}
			// TODO: we could "fix" faceting of the sideways counts
			// to do this "union" (of the drill down hits) in the
			// end instead:
			// Tally sideways counts:
			for (int dim = 0; dim < sidewaysCollectors.Length; dim++)
			{
				sidewaysCollectors[dim].Collect(collectDocID);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void CollectHit(Collector collector, Collector[] sidewaysCollectors, Collector
			[] sidewaysCollectors2)
		{
			//if (DEBUG) {
			//  System.out.println("      hit");
			//}
			collector.Collect(collectDocID);
			if (drillDownCollector != null)
			{
				drillDownCollector.Collect(collectDocID);
			}
			// TODO: we could "fix" faceting of the sideways counts
			// to do this "union" (of the drill down hits) in the
			// end instead:
			// Tally sideways counts:
			for (int i = 0; i < sidewaysCollectors.Length; i++)
			{
				sidewaysCollectors[i].Collect(collectDocID);
			}
			for (int i_1 = 0; i_1 < sidewaysCollectors2.Length; i_1++)
			{
				sidewaysCollectors2[i_1].Collect(collectDocID);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void CollectNearMiss(Collector sidewaysCollector)
		{
			//if (DEBUG) {
			//  System.out.println("      missingDim=" + dim);
			//}
			sidewaysCollector.Collect(collectDocID);
		}

		private sealed class FakeScorer : Scorer
		{
			internal float score;

			internal int doc;

			public FakeScorer(DrillSidewaysScorer _enclosing) : base(null)
			{
				this._enclosing = _enclosing;
			}

			public override int Advance(int target)
			{
				throw new NotSupportedException("FakeScorer doesn't support advance(int)");
			}

			public override int DocID()
			{
				return this._enclosing.collectDocID;
			}

			public override int Freq()
			{
				return 1 + this._enclosing.dims.Length;
			}

			public override int NextDoc()
			{
				throw new NotSupportedException("FakeScorer doesn't support nextDoc()");
			}

			public override float Score()
			{
				return this._enclosing.collectScore;
			}

			public override long Cost()
			{
				return this._enclosing.baseScorer.Cost();
			}

			public override ICollection<Scorer.ChildScorer> GetChildren()
			{
				return Sharpen.Collections.SingletonList(new Scorer.ChildScorer(this._enclosing.baseScorer
					, "MUST"));
			}

			public override Weight GetWeight()
			{
				throw new NotSupportedException();
			}

			private readonly DrillSidewaysScorer _enclosing;
		}

		internal class DocsAndCost : Comparable<DrillSidewaysScorer.DocsAndCost>
		{
			internal DocIdSetIterator disi;

			internal Bits bits;

			internal Collector sidewaysCollector;

			internal string dim;

			// Iterator for docs matching this dim's filter, or ...
			// Random access bits:
			public virtual int CompareTo(DrillSidewaysScorer.DocsAndCost other)
			{
				if (disi == null)
				{
					if (other.disi == null)
					{
						return 0;
					}
					else
					{
						return 1;
					}
				}
				else
				{
					if (other.disi == null)
					{
						return -1;
					}
					else
					{
						if (disi.Cost() < other.disi.Cost())
						{
							return -1;
						}
						else
						{
							if (disi.Cost() > other.disi.Cost())
							{
								return 1;
							}
							else
							{
								return 0;
							}
						}
					}
				}
			}
		}
	}
}
