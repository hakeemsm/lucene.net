/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search
{
	public class TestShardSearching : ShardSearchingTestBase
	{
		private class PreviousSearchState
		{
			public readonly long searchTimeNanos;

			public readonly long[] versions;

			public readonly ScoreDoc searchAfterLocal;

			public readonly ScoreDoc searchAfterShard;

			public readonly Sort sort;

			public readonly Query query;

			public readonly int numHitsPaged;

			public PreviousSearchState(Query query, Sort sort, ScoreDoc searchAfterLocal, ScoreDoc
				 searchAfterShard, long[] versions, int numHitsPaged)
			{
				// TODO
				//   - other queries besides PrefixQuery & TermQuery (but:
				//     FuzzyQ will be problematic... the top N terms it
				//     takes means results will differ)
				//   - NRQ/F
				//   - BQ, negated clauses, negated prefix clauses
				//   - test pulling docs in 2nd round trip...
				//   - filter too
				this.versions = versions.Clone();
				this.searchAfterLocal = searchAfterLocal;
				this.searchAfterShard = searchAfterShard;
				this.sort = sort;
				this.query = query;
				this.numHitsPaged = numHitsPaged;
				searchTimeNanos = Runtime.NanoTime();
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSimple()
		{
			int numNodes = TestUtil.NextInt(Random(), 1, 10);
			double runTimeSec = AtLeast(3);
			int minDocsToMakeTerms = TestUtil.NextInt(Random(), 5, 20);
			int maxSearcherAgeSeconds = TestUtil.NextInt(Random(), 1, 3);
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: numNodes=" + numNodes + " runTimeSec=" + runTimeSec
					 + " maxSearcherAgeSeconds=" + maxSearcherAgeSeconds);
			}
			Start(numNodes, runTimeSec, maxSearcherAgeSeconds);
			IList<TestShardSearching.PreviousSearchState> priorSearches = new AList<TestShardSearching.PreviousSearchState
				>();
			IList<BytesRef> terms = null;
			while (Runtime.NanoTime() < endTimeNanos)
			{
				bool doFollowon = priorSearches.Count > 0 && Random().Next(7) == 1;
				// Pick a random node; we will run the query on this node:
				int myNodeID = Random().Next(numNodes);
				ShardSearchingTestBase.NodeState.ShardIndexSearcher localShardSearcher;
				TestShardSearching.PreviousSearchState prevSearchState;
				if (doFollowon)
				{
					// Pretend user issued a followon query:
					prevSearchState = priorSearches[Random().Next(priorSearches.Count)];
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("\nTEST: follow-on query age=" + ((Runtime.NanoTime(
							) - prevSearchState.searchTimeNanos) / 1000000000.0));
					}
					try
					{
						localShardSearcher = nodes[myNodeID].Acquire(prevSearchState.versions);
					}
					catch (ShardSearchingTestBase.SearcherExpiredException see)
					{
						// Expected, sometimes; in a "real" app we would
						// either forward this error to the user ("too
						// much time has passed; please re-run your
						// search") or sneakily just switch to newest
						// searcher w/o telling them...
						if (VERBOSE)
						{
							System.Console.Out.WriteLine("  searcher expired during local shard searcher init: "
								 + see);
						}
						priorSearches.Remove(prevSearchState);
						continue;
					}
				}
				else
				{
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("\nTEST: fresh query");
					}
					// Do fresh query:
					localShardSearcher = nodes[myNodeID].Acquire();
					prevSearchState = null;
				}
				IndexReader[] subs = new IndexReader[numNodes];
				TestShardSearching.PreviousSearchState searchState = null;
				try
				{
					// Mock: now make a single reader (MultiReader) from all node
					// searchers.  In a real shard env you can't do this... we
					// do it to confirm results from the shard searcher
					// are correct:
					int docCount = 0;
					try
					{
						for (int nodeID = 0; nodeID < numNodes; nodeID++)
						{
							long subVersion = localShardSearcher.nodeVersions[nodeID];
							IndexSearcher sub = nodes[nodeID].searchers.Acquire(subVersion);
							if (sub == null)
							{
								nodeID--;
								while (nodeID >= 0)
								{
									subs[nodeID].DecRef();
									subs[nodeID] = null;
									nodeID--;
								}
								throw new ShardSearchingTestBase.SearcherExpiredException("nodeID=" + nodeID + " version="
									 + subVersion);
							}
							subs[nodeID] = sub.IndexReader;
							docCount += subs[nodeID].MaxDoc;
						}
					}
					catch (ShardSearchingTestBase.SearcherExpiredException see)
					{
						// Expected
						if (VERBOSE)
						{
							System.Console.Out.WriteLine("  searcher expired during mock reader init: " + see
								);
						}
						continue;
					}
					IndexReader mockReader = new MultiReader(subs);
					IndexSearcher mockSearcher = new IndexSearcher(mockReader);
					Query query;
					Sort sort;
					if (prevSearchState != null)
					{
						query = prevSearchState.query;
						sort = prevSearchState.sort;
					}
					else
					{
						if (terms == null && docCount > minDocsToMakeTerms)
						{
							// TODO: try to "focus" on high freq terms sometimes too
							// TODO: maybe also periodically reset the terms...?
							TermsEnum termsEnum = MultiFields.GetTerms(mockReader, "body").Iterator(null);
							terms = new AList<BytesRef>();
							while (termsEnum.Next() != null)
							{
								terms.AddItem(BytesRef.DeepCopyOf(termsEnum.Term()));
							}
							if (VERBOSE)
							{
								System.Console.Out.WriteLine("TEST: init terms: " + terms.Count + " terms");
							}
							if (terms.Count == 0)
							{
								terms = null;
							}
						}
						if (VERBOSE)
						{
							System.Console.Out.WriteLine("  maxDoc=" + mockReader.MaxDoc);
						}
						if (terms != null)
						{
							if (Random().NextBoolean())
							{
								query = new TermQuery(new Term("body", terms[Random().Next(terms.Count)]));
							}
							else
							{
								string t = terms[Random().Next(terms.Count)].Utf8ToString();
								string prefix;
								if (t.Length <= 1)
								{
									prefix = t;
								}
								else
								{
									prefix = Sharpen.Runtime.Substring(t, 0, TestUtil.NextInt(Random(), 1, 2));
								}
								query = new PrefixQuery(new Term("body", prefix));
							}
							if (Random().NextBoolean())
							{
								sort = null;
							}
							else
							{
								// TODO: sort by more than 1 field
								int what = Random().Next(3);
								if (what == 0)
								{
									sort = new Sort(SortField.FIELD_SCORE);
								}
								else
								{
									if (what == 1)
									{
										// TODO: this sort doesn't merge
										// correctly... it's tricky because you
										// could have > 2.1B docs across all shards: 
										//sort = new Sort(SortField.FIELD_DOC);
										sort = null;
									}
									else
									{
										if (what == 2)
										{
											sort = new Sort(new SortField[] { new SortField("docid", SortField.Type.INT, Random
												().NextBoolean()) });
										}
										else
										{
											sort = new Sort(new SortField[] { new SortField("title", SortField.Type.STRING, Random
												().NextBoolean()) });
										}
									}
								}
							}
						}
						else
						{
							query = null;
							sort = null;
						}
					}
					if (query != null)
					{
						try
						{
							searchState = AssertSame(mockSearcher, localShardSearcher, query, sort, prevSearchState
								);
						}
						catch (ShardSearchingTestBase.SearcherExpiredException see)
						{
							// Expected; in a "real" app we would
							// either forward this error to the user ("too
							// much time has passed; please re-run your
							// search") or sneakily just switch to newest
							// searcher w/o telling them...
							if (VERBOSE)
							{
								System.Console.Out.WriteLine("  searcher expired during search: " + see);
								Sharpen.Runtime.PrintStackTrace(see, System.Console.Out);
							}
							// We can't do this in general: on a very slow
							// computer it's possible the local searcher
							// expires before we can finish our search:
							// 
							//HM:revisit 
							//assert prevSearchState != null;
							if (prevSearchState != null)
							{
								priorSearches.Remove(prevSearchState);
							}
						}
					}
				}
				finally
				{
					nodes[myNodeID].Release(localShardSearcher);
					foreach (IndexReader sub in subs)
					{
						if (sub != null)
						{
							sub.DecRef();
						}
					}
				}
				if (searchState != null && searchState.searchAfterLocal != null && Random().Next(
					5) == 3)
				{
					priorSearches.AddItem(searchState);
					if (priorSearches.Count > 200)
					{
						Sharpen.Collections.Shuffle(priorSearches, Random());
						priorSearches.SubList(100, priorSearches.Count).Clear();
					}
				}
			}
			Finish();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private TestShardSearching.PreviousSearchState AssertSame(IndexSearcher mockSearcher
			, ShardSearchingTestBase.NodeState.ShardIndexSearcher shardSearcher, Query q, Sort
			 sort, TestShardSearching.PreviousSearchState state)
		{
			int numHits = TestUtil.NextInt(Random(), 1, 100);
			if (state != null && state.searchAfterLocal == null)
			{
				// In addition to what we last searched:
				numHits += state.numHitsPaged;
			}
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: query=" + q + " sort=" + sort + " numHits=" +
					 numHits);
				if (state != null)
				{
					System.Console.Out.WriteLine("  prev: searchAfterLocal=" + state.searchAfterLocal
						 + " searchAfterShard=" + state.searchAfterShard + " numHitsPaged=" + state.numHitsPaged
						);
				}
			}
			// Single (mock local) searcher:
			TopDocs hits;
			if (sort == null)
			{
				if (state != null && state.searchAfterLocal != null)
				{
					hits = mockSearcher.SearchAfter(state.searchAfterLocal, q, numHits);
				}
				else
				{
					hits = mockSearcher.Search(q, numHits);
				}
			}
			else
			{
				hits = mockSearcher.Search(q, numHits, sort);
			}
			// Shard searcher
			TopDocs shardHits;
			if (sort == null)
			{
				if (state != null && state.searchAfterShard != null)
				{
					shardHits = shardSearcher.SearchAfter(state.searchAfterShard, q, numHits);
				}
				else
				{
					shardHits = shardSearcher.Search(q, numHits);
				}
			}
			else
			{
				shardHits = shardSearcher.Search(q, numHits, sort);
			}
			int numNodes = shardSearcher.nodeVersions.Length;
			int[] @base = new int[numNodes];
			IList<IndexReaderContext> subs = mockSearcher.GetTopReaderContext().Children();
			AreEqual(numNodes, subs.Count);
			for (int nodeID = 0; nodeID < numNodes; nodeID++)
			{
				@base[nodeID] = subs[nodeID].docBaseInParent;
			}
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("  single searcher: " + hits.TotalHits + " TotalHits maxScore="
					 + hits.GetMaxScore());
				for (int i = 0; i < hits.ScoreDocs.Length; i++)
				{
					ScoreDoc sd = hits.ScoreDocs[i];
					System.Console.Out.WriteLine("    doc=" + sd.Doc + " score=" + sd.score);
				}
				System.Console.Out.WriteLine("  shard searcher: " + shardHits.TotalHits + " TotalHits maxScore="
					 + shardHits.GetMaxScore());
				for (int i_1 = 0; i_1 < shardHits.ScoreDocs.Length; i_1++)
				{
					ScoreDoc sd = shardHits.ScoreDocs[i_1];
					System.Console.Out.WriteLine("    doc=" + sd.Doc + " (rebased: " + (sd.Doc + @base
						[sd.shardIndex]) + ") score=" + sd.score + " shard=" + sd.shardIndex);
				}
			}
			int numHitsPaged;
			if (state != null && state.searchAfterLocal != null)
			{
				numHitsPaged = hits.ScoreDocs.Length;
				if (state != null)
				{
					numHitsPaged += state.numHitsPaged;
				}
			}
			else
			{
				numHitsPaged = hits.ScoreDocs.Length;
			}
			bool moreHits;
			ScoreDoc bottomHit;
			ScoreDoc bottomHitShards;
			if (numHitsPaged < hits.TotalHits)
			{
				// More hits to page through
				moreHits = true;
				if (sort == null)
				{
					bottomHit = hits.ScoreDocs[hits.ScoreDocs.Length - 1];
					ScoreDoc sd = shardHits.ScoreDocs[shardHits.ScoreDocs.Length - 1];
					// Must copy because below we rebase:
					bottomHitShards = new ScoreDoc(sd.Doc, sd.score, sd.shardIndex);
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("  save bottomHit=" + bottomHit);
					}
				}
				else
				{
					bottomHit = null;
					bottomHitShards = null;
				}
			}
			else
			{
				AreEqual(hits.TotalHits, numHitsPaged);
				bottomHit = null;
				bottomHitShards = null;
				moreHits = false;
			}
			// Must rebase so assertEquals passes:
			for (int hitID = 0; hitID < shardHits.ScoreDocs.Length; hitID++)
			{
				ScoreDoc sd = shardHits.ScoreDocs[hitID];
				sd.Doc += @base[sd.shardIndex];
			}
			TestUtil.AssertEquals(hits, shardHits);
			if (moreHits)
			{
				// Return a continuation:
				return new TestShardSearching.PreviousSearchState(q, sort, bottomHit, bottomHitShards
					, shardSearcher.nodeVersions, numHitsPaged);
			}
			else
			{
				return null;
			}
		}
	}
}
