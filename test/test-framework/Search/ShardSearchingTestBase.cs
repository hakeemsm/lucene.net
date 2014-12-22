/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.TestFramework.Util;
using Lucene.Net.TestFramework.Analysis;
using Lucene.Net.TestFramework.Index;
using Lucene.Net.TestFramework.Search;
using Lucene.Net.TestFramework.Store;
using Lucene.Net.TestFramework.Util;
using Sharpen;

namespace Lucene.Net.TestFramework.Search
{
	/// <summary>Base test class for simulating distributed search across multiple shards.
	/// 	</summary>
	/// <remarks>Base test class for simulating distributed search across multiple shards.
	/// 	</remarks>
	public abstract class ShardSearchingTestBase : LuceneTestCase
	{
		/// <summary>Thrown when the lease for a searcher has expired.</summary>
		/// <remarks>Thrown when the lease for a searcher has expired.</remarks>
		[System.Serializable]
		public class SearcherExpiredException : RuntimeException
		{
			public SearcherExpiredException(string message) : base(message)
			{
			}
			// TODO
			//   - doc blocks?  so we can test joins/grouping...
			//   - controlled consistency (NRTMgr)
			// TODO: maybe SLM should throw this instead of returning null...
		}

		private class FieldAndShardVersion
		{
			private readonly long version;

			private readonly int nodeID;

			private readonly string field;

			public FieldAndShardVersion(int nodeID, long version, string field)
			{
				this.nodeID = nodeID;
				this.version = version;
				this.field = field;
			}

			public override int GetHashCode()
			{
				return (int)(version * nodeID + field.GetHashCode());
			}

			public override bool Equals(object _other)
			{
				if (!(_other is ShardSearchingTestBase.FieldAndShardVersion))
				{
					return false;
				}
				ShardSearchingTestBase.FieldAndShardVersion other = (ShardSearchingTestBase.FieldAndShardVersion
					)_other;
				return field.Equals(other.field) && version == other.version && nodeID == other.nodeID;
			}

			public override string ToString()
			{
				return "FieldAndShardVersion(field=" + field + " nodeID=" + nodeID + " version=" 
					+ version + ")";
			}
		}

		private class TermAndShardVersion
		{
			private readonly long version;

			private readonly int nodeID;

			private readonly Term term;

			public TermAndShardVersion(int nodeID, long version, Term term)
			{
				this.nodeID = nodeID;
				this.version = version;
				this.term = term;
			}

			public override int GetHashCode()
			{
				return (int)(version * nodeID + term.GetHashCode());
			}

			public override bool Equals(object _other)
			{
				if (!(_other is ShardSearchingTestBase.TermAndShardVersion))
				{
					return false;
				}
				ShardSearchingTestBase.TermAndShardVersion other = (ShardSearchingTestBase.TermAndShardVersion
					)_other;
				return term.Equals(other.term) && version == other.version && nodeID == other.nodeID;
			}
		}

		private readonly string[] fieldsToShare = new string[] { "body", "title" };

		// We share collection stats for these fields on each node
		// reopen:
		// Called by one node once it has reopened, to notify all
		// other nodes.  This is just a mock (since it goes and
		// directly updates all other nodes, in RAM)... in a real
		// env this would hit the wire, sending version &
		// collection stats to all other nodes:
		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void BroadcastNodeReopen(int nodeID, long version, IndexSearcher
			 newSearcher)
		{
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("REOPEN: nodeID=" + nodeID + " version=" + version +
					 " maxDoc=" + newSearcher.GetIndexReader().MaxDoc());
			}
			// Broadcast new collection stats for this node to all
			// other nodes:
			foreach (string field in fieldsToShare)
			{
				CollectionStatistics stats = newSearcher.CollectionStatistics(field);
				foreach (ShardSearchingTestBase.NodeState node in nodes)
				{
					// Don't put my own collection stats into the cache;
					// we pull locally:
					if (node.myNodeID != nodeID)
					{
						node.collectionStatsCache.Put(new ShardSearchingTestBase.FieldAndShardVersion(nodeID
							, version, field), stats);
					}
				}
			}
			foreach (ShardSearchingTestBase.NodeState node_1 in nodes)
			{
				node_1.UpdateNodeVersion(nodeID, version);
			}
		}

		// TODO: broadcastNodeExpire?  then we can purge the
		// known-stale cache entries...
		// MOCK: in a real env you have to hit the wire
		// (send this query to all remote nodes
		// concurrently):
		/// <exception cref="System.IO.IOException"></exception>
		internal virtual TopDocs SearchNode(int nodeID, long[] nodeVersions, Query q, Sort
			 sort, int numHits, ScoreDoc searchAfter)
		{
			ShardSearchingTestBase.NodeState.ShardIndexSearcher s = nodes[nodeID].Acquire(nodeVersions
				);
			try
			{
				if (sort == null)
				{
					if (searchAfter != null)
					{
						return s.LocalSearchAfter(searchAfter, q, numHits);
					}
					else
					{
						return s.LocalSearch(q, numHits);
					}
				}
				else
				{
					 
					//assert searchAfter == null;  // not supported yet
					return s.LocalSearch(q, numHits, sort);
				}
			}
			finally
			{
				nodes[nodeID].Release(s);
			}
		}

		// Mock: in a real env, this would hit the wire and get
		// term stats from remote node
		/// <exception cref="System.IO.IOException"></exception>
		internal virtual IDictionary<Term, TermStatistics> GetNodeTermStats(ICollection<Term
			> terms, int nodeID, long version)
		{
			ShardSearchingTestBase.NodeState node = nodes[nodeID];
			IDictionary<Term, TermStatistics> stats = new Dictionary<Term, TermStatistics>();
			IndexSearcher s = node.searchers.Acquire(version);
			if (s == null)
			{
				throw new ShardSearchingTestBase.SearcherExpiredException("node=" + nodeID + " version="
					 + version);
			}
			try
			{
				foreach (Term term in terms)
				{
					TermContext termContext = TermContext.Build(s.GetIndexReader().GetContext(), term
						);
					stats.Put(term, s.TermStatistics(term, termContext));
				}
			}
			finally
			{
				node.searchers.Release(s);
			}
			return stats;
		}

		protected internal sealed class NodeState : IDisposable
		{
			public readonly Directory dir;

			public readonly IndexWriter writer;

			public readonly SearcherLifetimeManager searchers;

			public readonly SearcherManager mgr;

			public readonly int myNodeID;

			public readonly long[] currentNodeVersions;

			private readonly IDictionary<ShardSearchingTestBase.FieldAndShardVersion, CollectionStatistics
				> collectionStatsCache = new ConcurrentHashMap<ShardSearchingTestBase.FieldAndShardVersion
				, CollectionStatistics>();

			private readonly IDictionary<ShardSearchingTestBase.TermAndShardVersion, TermStatistics
				> termStatsCache = new ConcurrentHashMap<ShardSearchingTestBase.TermAndShardVersion
				, TermStatistics>();

			/// <summary>
			/// Matches docs in the local shard but scores based on
			/// aggregated stats ("mock distributed scoring") from all
			/// nodes.
			/// </summary>
			/// <remarks>
			/// Matches docs in the local shard but scores based on
			/// aggregated stats ("mock distributed scoring") from all
			/// nodes.
			/// </remarks>
			public class ShardIndexSearcher : IndexSearcher
			{
				public readonly long[] nodeVersions;

				public readonly int myNodeID;

				public ShardIndexSearcher(NodeState _enclosing, long[] nodeVersions, IndexReader 
					localReader, int nodeID) : base(localReader)
				{
					this._enclosing = _enclosing;
					// TODO: nothing evicts from here!!!  Somehow, on searcher
					// expiration on remote nodes we must evict from our
					// local cache...?  And still LRU otherwise (for the
					// still-live searchers).
					// Version for the node searchers we search:
					this.nodeVersions = nodeVersions;
					this.myNodeID = nodeID;
				}

				 
				//assert myNodeID == NodeState.this.myNodeID: "myNodeID=" + nodeID + " NodeState.this.myNodeID=" + NodeState.this.myNodeID;
				/// <exception cref="System.IO.IOException"></exception>
				public override Query Rewrite(Query original)
				{
					Query rewritten = base.Rewrite(original);
					ICollection<Term> terms = new HashSet<Term>();
					rewritten.ExtractTerms(terms);
					// Make a single request to remote nodes for term
					// stats:
					for (int nodeID = 0; nodeID < this.nodeVersions.Length; nodeID++)
					{
						if (nodeID == this.myNodeID)
						{
							continue;
						}
						ICollection<Term> missing = new HashSet<Term>();
						foreach (Term term in terms)
						{
							ShardSearchingTestBase.TermAndShardVersion key = new ShardSearchingTestBase.TermAndShardVersion
								(nodeID, this.nodeVersions[nodeID], term);
							if (!this._enclosing.termStatsCache.ContainsKey(key))
							{
								missing.AddItem(term);
							}
						}
						if (missing.Count != 0)
						{
							foreach (KeyValuePair<Term,Lucene.Net.TestFramework.Search.TermStatistics> ent in this.
								_enclosing._enclosing.GetNodeTermStats(missing, nodeID, this.nodeVersions[nodeID
								]).EntrySet())
							{
								ShardSearchingTestBase.TermAndShardVersion key = new ShardSearchingTestBase.TermAndShardVersion
									(nodeID, this.nodeVersions[nodeID], ent.Key);
								this._enclosing.termStatsCache.Put(key, ent.Value);
							}
						}
					}
					return rewritten;
				}

				/// <exception cref="System.IO.IOException"></exception>
				public overrideLucene.Net.TestFramework.Search.TermStatistics TermStatistics(Term term, 
					TermContext context)
				{
					 
					//assert term != null;
					long docFreq = 0;
					long totalTermFreq = 0;
					for (int nodeID = 0; nodeID < this.nodeVersions.Length; nodeID++)
					{
						Lucene.NetSearch.TermStatistics subStats;
						if (nodeID == this.myNodeID)
						{
							subStats = base.TermStatistics(term, context);
						}
						else
						{
							ShardSearchingTestBase.TermAndShardVersion key = new ShardSearchingTestBase.TermAndShardVersion
								(nodeID, this.nodeVersions[nodeID], term);
							subStats = this._enclosing.termStatsCache.Get(key);
						}
						// We pre-cached during rewrite so all terms
						// better be here...
						 
						//assert subStats != null;
						long nodeDocFreq = subStats.DocFreq();
						if (docFreq >= 0 && nodeDocFreq >= 0)
						{
							docFreq += nodeDocFreq;
						}
						else
						{
							docFreq = -1;
						}
						long nodeTotalTermFreq = subStats.TotalTermFreq();
						if (totalTermFreq >= 0 && nodeTotalTermFreq >= 0)
						{
							totalTermFreq += nodeTotalTermFreq;
						}
						else
						{
							totalTermFreq = -1;
						}
					}
					return newLucene.Net.TestFramework.Search.TermStatistics(term.Bytes(), docFreq, totalTermFreq
						);
				}

				/// <exception cref="System.IO.IOException"></exception>
				public overrideLucene.Net.TestFramework.Search.CollectionStatistics CollectionStatistics
					(string field)
				{
					// TODO: we could compute this on init and cache,
					// since we are re-inited whenever any nodes have a
					// new reader
					long docCount = 0;
					long sumTotalTermFreq = 0;
					long sumDocFreq = 0;
					long maxDoc = 0;
					for (int nodeID = 0; nodeID < this.nodeVersions.Length; nodeID++)
					{
						ShardSearchingTestBase.FieldAndShardVersion key = new ShardSearchingTestBase.FieldAndShardVersion
							(nodeID, this.nodeVersions[nodeID], field);
						Lucene.NetSearch.CollectionStatistics nodeStats;
						if (nodeID == this.myNodeID)
						{
							nodeStats = base.CollectionStatistics(field);
						}
						else
						{
							nodeStats = this._enclosing.collectionStatsCache.Get(key);
						}
						if (nodeStats == null)
						{
							System.Console.Out.WriteLine("coll stats myNodeID=" + this.myNodeID + ": " + this
								._enclosing.collectionStatsCache.Keys);
						}
						// Collection stats are pre-shared on reopen, so,
						// we better not have a cache miss:
						 
						//assert nodeStats != null: "myNodeID=" + myNodeID + " nodeID=" + nodeID + " version=" + nodeVersions[nodeID] + " field=" + field;
						long nodeDocCount = nodeStats.DocCount();
						if (docCount >= 0 && nodeDocCount >= 0)
						{
							docCount += nodeDocCount;
						}
						else
						{
							docCount = -1;
						}
						long nodeSumTotalTermFreq = nodeStats.SumTotalTermFreq();
						if (sumTotalTermFreq >= 0 && nodeSumTotalTermFreq >= 0)
						{
							sumTotalTermFreq += nodeSumTotalTermFreq;
						}
						else
						{
							sumTotalTermFreq = -1;
						}
						long nodeSumDocFreq = nodeStats.SumDocFreq();
						if (sumDocFreq >= 0 && nodeSumDocFreq >= 0)
						{
							sumDocFreq += nodeSumDocFreq;
						}
						else
						{
							sumDocFreq = -1;
						}
						 
						//assert nodeStats.maxDoc() >= 0;
						maxDoc += nodeStats.MaxDoc();
					}
					return newLucene.Net.TestFramework.Search.CollectionStatistics(field, maxDoc, docCount, 
						sumTotalTermFreq, sumDocFreq);
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override TopDocs Search(Query query, int numHits)
				{
					TopDocs[] shardHits = new TopDocs[this.nodeVersions.Length];
					for (int nodeID = 0; nodeID < this.nodeVersions.Length; nodeID++)
					{
						if (nodeID == this.myNodeID)
						{
							// My node; run using local shard searcher we
							// already aquired:
							shardHits[nodeID] = this.LocalSearch(query, numHits);
						}
						else
						{
							shardHits[nodeID] = this._enclosing._enclosing.SearchNode(nodeID, this.nodeVersions
								, query, null, numHits, null);
						}
					}
					// Merge:
					return TopDocs.Merge(null, numHits, shardHits);
				}

				/// <exception cref="System.IO.IOException"></exception>
				public virtual TopDocs LocalSearch(Query query, int numHits)
				{
					return base.Search(query, numHits);
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override TopDocs SearchAfter(ScoreDoc after, Query query, int numHits)
				{
					TopDocs[] shardHits = new TopDocs[this.nodeVersions.Length];
					// results are merged in that order: score, shardIndex, doc. therefore we set
					// after to after.score and depending on the nodeID we set doc to either:
					// - not collect any more documents with that score (only with worse score)
					// - collect more documents with that score (and worse) following the last collected document
					// - collect all documents with that score (and worse)
					ScoreDoc shardAfter = new ScoreDoc(after.doc, after.score);
					for (int nodeID = 0; nodeID < this.nodeVersions.Length; nodeID++)
					{
						if (nodeID < after.shardIndex)
						{
							// all documents with after.score were already collected, so collect
							// only documents with worse scores.
							ShardSearchingTestBase.NodeState.ShardIndexSearcher s = this._enclosing._enclosing
								.nodes[nodeID].Acquire(this.nodeVersions);
							try
							{
								// Setting after.doc to reader.maxDoc-1 is a way to tell
								// TopScoreDocCollector that no more docs with that score should
								// be collected. note that in practice the shard which sends the
								// request to a remote shard won't have reader.maxDoc at hand, so
								// it will send some arbitrary value which will be fixed on the
								// other end.
								shardAfter.doc = s.GetIndexReader().MaxDoc() - 1;
							}
							finally
							{
								this._enclosing._enclosing.nodes[nodeID].Release(s);
							}
						}
						else
						{
							if (nodeID == after.shardIndex)
							{
								// collect all documents following the last collected doc with
								// after.score + documents with worse scores.  
								shardAfter.doc = after.doc;
							}
							else
							{
								// all documents with after.score (and worse) should be collected
								// because they didn't make it to top-N in the previous round.
								shardAfter.doc = -1;
							}
						}
						if (nodeID == this.myNodeID)
						{
							// My node; run using local shard searcher we
							// already aquired:
							shardHits[nodeID] = this.LocalSearchAfter(shardAfter, query, numHits);
						}
						else
						{
							shardHits[nodeID] = this._enclosing._enclosing.SearchNode(nodeID, this.nodeVersions
								, query, null, numHits, shardAfter);
						}
					}
					//System.out.println("  node=" + nodeID + " totHits=" + shardHits[nodeID].totalHits);
					// Merge:
					return TopDocs.Merge(null, numHits, shardHits);
				}

				/// <exception cref="System.IO.IOException"></exception>
				public virtual TopDocs LocalSearchAfter(ScoreDoc after, Query query, int numHits)
				{
					return base.SearchAfter(after, query, numHits);
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override TopFieldDocs Search(Query query, int numHits, Sort sort)
				{
					 
					//assert sort != null;
					TopDocs[] shardHits = new TopDocs[this.nodeVersions.Length];
					for (int nodeID = 0; nodeID < this.nodeVersions.Length; nodeID++)
					{
						if (nodeID == this.myNodeID)
						{
							// My node; run using local shard searcher we
							// already aquired:
							shardHits[nodeID] = this.LocalSearch(query, numHits, sort);
						}
						else
						{
							shardHits[nodeID] = this._enclosing._enclosing.SearchNode(nodeID, this.nodeVersions
								, query, sort, numHits, null);
						}
					}
					// Merge:
					return (TopFieldDocs)TopDocs.Merge(sort, numHits, shardHits);
				}

				/// <exception cref="System.IO.IOException"></exception>
				public virtual TopFieldDocs LocalSearch(Query query, int numHits, Sort sort)
				{
					return base.Search(query, numHits, sort);
				}

				private readonly NodeState _enclosing;
			}

			private volatile ShardSearchingTestBase.NodeState.ShardIndexSearcher currentShardSearcher;

			/// <exception cref="System.IO.IOException"></exception>
			public NodeState(ShardSearchingTestBase _enclosing, Random random, int nodeID, int
				 numNodes)
			{
				this._enclosing = _enclosing;
				this.myNodeID = nodeID;
				this.dir = LuceneTestCase.NewFSDirectory(LuceneTestCase.CreateTempDir("ShardSearchingTestBase"
					));
				// TODO: set warmer
				MockAnalyzer analyzer = new MockAnalyzer(LuceneTestCase.Random());
				analyzer.SetMaxTokenLength(TestUtil.NextInt(LuceneTestCase.Random(), 1, IndexWriter
					.MAX_TERM_LENGTH));
				IndexWriterConfig iwc = new IndexWriterConfig(LuceneTestCase.TEST_VERSION_CURRENT
					, analyzer);
				iwc.SetOpenMode(IndexWriterConfig.OpenMode.CREATE);
				if (LuceneTestCase.VERBOSE)
				{
					iwc.SetInfoStream(new PrintStreamInfoStream(System.Console.Out));
				}
				this.writer = new IndexWriter(this.dir, iwc);
				this.mgr = new SearcherManager(this.writer, true, null);
				this.searchers = new SearcherLifetimeManager();
				// Init w/ 0s... caller above will do initial
				// "broadcast" by calling initSearcher:
				this.currentNodeVersions = new long[numNodes];
			}

			/// <exception cref="System.IO.IOException"></exception>
			public void InitSearcher(long[] nodeVersions)
			{
				 
				//assert currentShardSearcher == null;
				System.Array.Copy(nodeVersions, 0, this.currentNodeVersions, 0, this.currentNodeVersions
					.Length);
				this.currentShardSearcher = new ShardSearchingTestBase.NodeState.ShardIndexSearcher
					(this, this.currentNodeVersions.Clone(), this.mgr.Acquire().GetIndexReader(), this
					.myNodeID);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public void UpdateNodeVersion(int nodeID, long version)
			{
				this.currentNodeVersions[nodeID] = version;
				if (this.currentShardSearcher != null)
				{
					this.currentShardSearcher.GetIndexReader().DecRef();
				}
				this.currentShardSearcher = new ShardSearchingTestBase.NodeState.ShardIndexSearcher
					(this, this.currentNodeVersions.Clone(), this.mgr.Acquire().GetIndexReader(), this
					.myNodeID);
			}

			// Get the current (fresh) searcher for this node
			public ShardSearchingTestBase.NodeState.ShardIndexSearcher Acquire()
			{
				while (true)
				{
					ShardSearchingTestBase.NodeState.ShardIndexSearcher s = this.currentShardSearcher;
					// In theory the reader could get decRef'd to 0
					// before we have a chance to incRef, ie if a reopen
					// happens right after the above line, this thread
					// gets stalled, and the old IR is closed.  So we
					// must try/retry until incRef succeeds:
					if (s.GetIndexReader().TryIncRef())
					{
						return s;
					}
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public void Release(ShardSearchingTestBase.NodeState.ShardIndexSearcher s)
			{
				s.GetIndexReader().DecRef();
			}

			// Get and old searcher matching the specified versions:
			public ShardSearchingTestBase.NodeState.ShardIndexSearcher Acquire(long[] nodeVersions
				)
			{
				IndexSearcher s = this.searchers.Acquire(nodeVersions[this.myNodeID]);
				if (s == null)
				{
					throw new ShardSearchingTestBase.SearcherExpiredException("nodeID=" + this.myNodeID
						 + " version=" + nodeVersions[this.myNodeID]);
				}
				return new ShardSearchingTestBase.NodeState.ShardIndexSearcher(this, nodeVersions
					, s.GetIndexReader(), this.myNodeID);
			}

			// Reopen local reader
			/// <exception cref="System.IO.IOException"></exception>
			public void Reopen()
			{
				IndexSearcher before = this.mgr.Acquire();
				this.mgr.Release(before);
				this.mgr.MaybeRefresh();
				IndexSearcher after = this.mgr.Acquire();
				try
				{
					if (after != before)
					{
						// New searcher was opened
						long version = this.searchers.Record(after);
						this.searchers.Prune(new SearcherLifetimeManager.PruneByAge(this._enclosing.maxSearcherAgeSeconds
							));
						this._enclosing.BroadcastNodeReopen(this.myNodeID, version, after);
					}
				}
				finally
				{
					this.mgr.Release(after);
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public void Close()
			{
				if (this.currentShardSearcher != null)
				{
					this.currentShardSearcher.GetIndexReader().DecRef();
				}
				this.searchers.Close();
				this.mgr.Close();
				this.writer.Close();
				this.dir.Close();
			}

			private readonly ShardSearchingTestBase _enclosing;
		}

		private sealed class ChangeIndices : Sharpen.Thread
		{
			// TODO: make this more realistic, ie, each node should
			// have its own thread, so we have true node to node
			// concurrency
			public override void Run()
			{
				try
				{
					LineFileDocs docs = new LineFileDocs(LuceneTestCase.Random(), LuceneTestCase.DefaultCodecSupportsDocValues
						());
					int numDocs = 0;
					while (Runtime.NanoTime() < this._enclosing.endTimeNanos)
					{
						int what = LuceneTestCase.Random().Next(3);
						ShardSearchingTestBase.NodeState node = this._enclosing.nodes[LuceneTestCase.Random
							().Next(this._enclosing.nodes.Length)];
						if (numDocs == 0 || what == 0)
						{
							node.writer.AddDocument(docs.NextDoc());
							numDocs++;
						}
						else
						{
							if (what == 1)
							{
								node.writer.UpdateDocument(new Term("docid", string.Empty + LuceneTestCase.Random
									().Next(numDocs)), docs.NextDoc());
								numDocs++;
							}
							else
							{
								node.writer.DeleteDocuments(new Term("docid", string.Empty + LuceneTestCase.Random
									().Next(numDocs)));
							}
						}
						// TODO: doc blocks too
						if (LuceneTestCase.Random().Next(17) == 12)
						{
							node.writer.Commit();
						}
						if (LuceneTestCase.Random().Next(17) == 12)
						{
							this._enclosing.nodes[LuceneTestCase.Random().Next(this._enclosing.nodes.Length)]
								.Reopen();
						}
					}
				}
				catch (Exception t)
				{
					System.Console.Out.WriteLine("FAILED:");
					Sharpen.Runtime.PrintStackTrace(t, System.Console.Out);
					throw new RuntimeException(t);
				}
			}

			internal ChangeIndices(ShardSearchingTestBase _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly ShardSearchingTestBase _enclosing;
		}

		protected internal ShardSearchingTestBase.NodeState[] nodes;

		internal int maxSearcherAgeSeconds;

		internal long endTimeNanos;

		private Sharpen.Thread changeIndicesThread;

		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual void Start(int numNodes, double runTimeSec, int maxSearcherAgeSeconds
			)
		{
			endTimeNanos = Runtime.NanoTime() + (long)(runTimeSec * 1000000000);
			this.maxSearcherAgeSeconds = maxSearcherAgeSeconds;
			nodes = new ShardSearchingTestBase.NodeState[numNodes];
			for (int nodeID = 0; nodeID < numNodes; nodeID++)
			{
				nodes[nodeID] = new ShardSearchingTestBase.NodeState(this, Random(), nodeID, numNodes
					);
			}
			long[] nodeVersions = new long[nodes.Length];
			for (int nodeID_1 = 0; nodeID_1 < numNodes; nodeID_1++)
			{
				IndexSearcher s = nodes[nodeID_1].mgr.Acquire();
				try
				{
					nodeVersions[nodeID_1] = nodes[nodeID_1].searchers.Record(s);
				}
				finally
				{
					nodes[nodeID_1].mgr.Release(s);
				}
			}
			for (int nodeID_2 = 0; nodeID_2 < numNodes; nodeID_2++)
			{
				IndexSearcher s = nodes[nodeID_2].mgr.Acquire();
				 
				//assert nodeVersions[nodeID] == nodes[nodeID].searchers.record(s);
				 
				//assert s != null;
				try
				{
					BroadcastNodeReopen(nodeID_2, nodeVersions[nodeID_2], s);
				}
				finally
				{
					nodes[nodeID_2].mgr.Release(s);
				}
			}
			changeIndicesThread = new ShardSearchingTestBase.ChangeIndices(this);
			changeIndicesThread.Start();
		}

		/// <exception cref="System.Exception"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual void Finish()
		{
			changeIndicesThread.Join();
			foreach (ShardSearchingTestBase.NodeState node in nodes)
			{
				node.Close();
			}
		}

		/// <summary>An IndexSearcher and associated version (lease)</summary>
		protected internal class SearcherAndVersion
		{
			public readonly IndexSearcher searcher;

			public readonly long version;

			public SearcherAndVersion(IndexSearcher searcher, long version)
			{
				this.searcher = searcher;
				this.version = version;
			}
		}
	}
}
