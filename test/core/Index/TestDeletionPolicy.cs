/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Test.Index
{
	public class TestDeletionPolicy : LuceneTestCase
	{
		private void VerifyCommitOrder<_T0>(IList<_T0> commits) where _T0:IndexCommit
		{
			if (commits.IsEmpty())
			{
				return;
			}
			IndexCommit firstCommit = commits[0];
			long last = SegmentInfos.GenerationFromSegmentsFileName(firstCommit.GetSegmentsFileName
				());
			AreEqual(last, firstCommit.GetGeneration());
			for (int i = 1; i < commits.Count; i++)
			{
				IndexCommit commit = commits[i];
				long now = SegmentInfos.GenerationFromSegmentsFileName(commit.GetSegmentsFileName
					());
				IsTrue("SegmentInfos commits are out-of-order", now > last
					);
				AreEqual(now, commit.GetGeneration());
				last = now;
			}
		}

		internal class KeepAllDeletionPolicy : IndexDeletionPolicy
		{
			internal int numOnInit;

			internal int numOnCommit;

			internal Directory dir;

			internal KeepAllDeletionPolicy(TestDeletionPolicy _enclosing, Directory dir)
			{
				this._enclosing = _enclosing;
				this.dir = dir;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void OnInit<_T0>(IList<_T0> commits)
			{
				this._enclosing.VerifyCommitOrder(commits);
				this.numOnInit++;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void OnCommit<_T0>(IList<_T0> commits)
			{
				IndexCommit lastCommit = commits[commits.Count - 1];
				DirectoryReader r = DirectoryReader.Open(this.dir);
				AreEqual("lastCommit.segmentCount()=" + lastCommit.GetSegmentCount
					() + " vs IndexReader.segmentCount=" + r.Leaves.Count, r.Leaves.Count, lastCommit
					.SegmentCount);
				r.Dispose();
				this._enclosing.VerifyCommitOrder(commits);
				this.numOnCommit++;
			}

			private readonly TestDeletionPolicy _enclosing;
		}

		/// <summary>
		/// This is useful for adding to a big index when you know
		/// readers are not using it.
		/// </summary>
		/// <remarks>
		/// This is useful for adding to a big index when you know
		/// readers are not using it.
		/// </remarks>
		internal class KeepNoneOnInitDeletionPolicy : IndexDeletionPolicy
		{
			internal int numOnInit;

			internal int numOnCommit;

			/// <exception cref="System.IO.IOException"></exception>
			public override void OnInit<_T0>(IList<_T0> commits)
			{
				this._enclosing.VerifyCommitOrder(commits);
				this.numOnInit++;
				// On init, delete all commit points:
				foreach (IndexCommit commit in commits)
				{
					commit.Delete();
					IsTrue(commit.IsDeleted());
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void OnCommit<_T0>(IList<_T0> commits)
			{
				this._enclosing.VerifyCommitOrder(commits);
				int size = commits.Count;
				// Delete all but last one:
				for (int i = 0; i < size - 1; i++)
				{
					((IndexCommit)commits[i]).Delete();
				}
				this.numOnCommit++;
			}

			internal KeepNoneOnInitDeletionPolicy(TestDeletionPolicy _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly TestDeletionPolicy _enclosing;
		}

		internal class KeepLastNDeletionPolicy : IndexDeletionPolicy
		{
			internal int numOnInit;

			internal int numOnCommit;

			internal int numToKeep;

			internal int numDelete;

			internal ICollection<string> seen = new HashSet<string>();

			public KeepLastNDeletionPolicy(TestDeletionPolicy _enclosing, int numToKeep)
			{
				this._enclosing = _enclosing;
				this.numToKeep = numToKeep;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void OnInit<_T0>(IList<_T0> commits)
			{
				if (LuceneTestCase.VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: onInit");
				}
				this._enclosing.VerifyCommitOrder(commits);
				this.numOnInit++;
				// do no deletions on init
				this.DoDeletes(commits, false);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void OnCommit<_T0>(IList<_T0> commits)
			{
				if (LuceneTestCase.VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: onCommit");
				}
				this._enclosing.VerifyCommitOrder(commits);
				this.DoDeletes(commits, true);
			}

			private void DoDeletes<_T0>(IList<_T0> commits, bool isCommit) where _T0:IndexCommit
			{
				// 
				//HM:revisit 
				//assert that we really are only called for each new
				// commit:
				if (isCommit)
				{
					string fileName = ((IndexCommit)commits[commits.Count - 1]).GetSegmentsFileName();
					if (this.seen.Contains(fileName))
					{
						throw new RuntimeException("onCommit was called twice on the same commit point: "
							 + fileName);
					}
					this.seen.AddItem(fileName);
					this.numOnCommit++;
				}
				int size = commits.Count;
				for (int i = 0; i < size - this.numToKeep; i++)
				{
					((IndexCommit)commits[i]).Delete();
					this.numDelete++;
				}
			}

			private readonly TestDeletionPolicy _enclosing;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal static long GetCommitTime(IndexCommit commit)
		{
			return long.Parse(commit.GetUserData().Get("commitTime"));
		}

		internal class ExpirationTimeDeletionPolicy : IndexDeletionPolicy
		{
			internal Directory dir;

			internal double expirationTimeSeconds;

			internal int numDelete;

			public ExpirationTimeDeletionPolicy(TestDeletionPolicy _enclosing, Directory dir, 
				double seconds)
			{
				this._enclosing = _enclosing;
				this.dir = dir;
				this.expirationTimeSeconds = seconds;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void OnInit<_T0>(IList<_T0> commits)
			{
				if (commits.IsEmpty())
				{
					return;
				}
				this._enclosing.VerifyCommitOrder(commits);
				this.OnCommit(commits);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void OnCommit<_T0>(IList<_T0> commits)
			{
				this._enclosing.VerifyCommitOrder(commits);
				IndexCommit lastCommit = commits[commits.Count - 1];
				// Any commit older than expireTime should be deleted:
				double expireTime = TestDeletionPolicy.GetCommitTime(lastCommit) / 1000.0 - this.
					expirationTimeSeconds;
				foreach (IndexCommit commit in commits)
				{
					double modTime = TestDeletionPolicy.GetCommitTime(commit) / 1000.0;
					if (commit != lastCommit && modTime < expireTime)
					{
						commit.Delete();
						this.numDelete += 1;
					}
				}
			}

			private readonly TestDeletionPolicy _enclosing;
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestExpirationTimeDeletionPolicy()
		{
			double SECONDS = 2.0;
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetIndexDeletionPolicy(new TestDeletionPolicy.ExpirationTimeDeletionPolicy
				(this, dir, SECONDS));
			MergePolicy mp = conf.MergePolicy;
			mp.SetNoCFSRatio(1.0);
			IndexWriter writer = new IndexWriter(dir, conf);
			TestDeletionPolicy.ExpirationTimeDeletionPolicy policy = (TestDeletionPolicy.ExpirationTimeDeletionPolicy
				)writer.Config.GetIndexDeletionPolicy();
			IDictionary<string, string> commitData = new Dictionary<string, string>();
			commitData.Put("commitTime", DateTime.Now.CurrentTimeMillis().ToString());
			writer.SetCommitData(commitData);
			writer.Commit();
			writer.Dispose();
			long lastDeleteTime = 0;
			int targetNumDelete = TestUtil.NextInt(Random(), 1, 5);
			while (policy.numDelete < targetNumDelete)
			{
				// Record last time when writer performed deletes of
				// past commits
				lastDeleteTime = DateTime.Now.CurrentTimeMillis();
				conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetOpenMode
					(IndexWriterConfig.OpenMode.APPEND).SetIndexDeletionPolicy(policy);
				mp = conf.MergePolicy;
				mp.SetNoCFSRatio(1.0);
				writer = new IndexWriter(dir, conf);
				policy = (TestDeletionPolicy.ExpirationTimeDeletionPolicy)writer.Config.GetIndexDeletionPolicy
					();
				for (int j = 0; j < 17; j++)
				{
					AddDoc(writer);
				}
				commitData = new Dictionary<string, string>();
				commitData.Put("commitTime", DateTime.Now.CurrentTimeMillis().ToString());
				writer.SetCommitData(commitData);
				writer.Commit();
				writer.Dispose();
				Sharpen.Thread.Sleep((int)(1000.0 * (SECONDS / 5.0)));
			}
			// Then simplistic check: just verify that the
			// segments_N's that still exist are in fact within SECONDS
			// seconds of the last one's mod time, and, that I can
			// open a reader on each:
			long gen = SegmentInfos.GetLastCommitGeneration(dir);
			string fileName = IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, 
				string.Empty, gen);
			dir.DeleteFile(IndexFileNames.SEGMENTS_GEN);
			bool oneSecondResolution = true;
			while (gen > 0)
			{
				try
				{
					IndexReader reader = DirectoryReader.Open(dir);
					reader.Dispose();
					fileName = IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, string.Empty
						, gen);
					// if we are on a filesystem that seems to have only
					// 1 second resolution, allow +1 second in commit
					// age tolerance:
					SegmentInfos sis = new SegmentInfos();
					sis.Read(dir, fileName);
					long modTime = long.Parse(sis.GetUserData().Get("commitTime"));
					oneSecondResolution &= (modTime % 1000) == 0;
					long leeway = (long)((SECONDS + (oneSecondResolution ? 1.0 : 0.0)) * 1000);
					IsTrue("commit point was older than " + SECONDS + " seconds ("
						 + (lastDeleteTime - modTime) + " msec) but did not get deleted ", lastDeleteTime
						 - modTime <= leeway);
				}
				catch (IOException)
				{
					// OK
					break;
				}
				dir.DeleteFile(IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, string.Empty
					, gen));
				gen--;
			}
			dir.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestKeepAllDeletionPolicy()
		{
			for (int pass = 0; pass < 2; pass++)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: cycle pass=" + pass);
				}
				bool useCompoundFile = (pass % 2) != 0;
				Directory dir = NewDirectory();
				IndexWriterConfig conf = ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
					, new MockAnalyzer(Random())).SetIndexDeletionPolicy(new TestDeletionPolicy.KeepAllDeletionPolicy
					(this, dir)).SetMaxBufferedDocs(10)).SetMergeScheduler(new SerialMergeScheduler(
					));
				MergePolicy mp = conf.MergePolicy;
				mp.SetNoCFSRatio(useCompoundFile ? 1.0 : 0.0);
				IndexWriter writer = new IndexWriter(dir, conf);
				TestDeletionPolicy.KeepAllDeletionPolicy policy = (TestDeletionPolicy.KeepAllDeletionPolicy
					)writer.Config.GetIndexDeletionPolicy();
				for (int i = 0; i < 107; i++)
				{
					AddDoc(writer);
				}
				writer.Dispose();
				bool needsMerging;
				{
					DirectoryReader r = DirectoryReader.Open(dir);
					needsMerging = r.Leaves.Count != 1;
					r.Dispose();
				}
				if (needsMerging)
				{
					conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetOpenMode
						(IndexWriterConfig.OpenMode.APPEND).SetIndexDeletionPolicy(policy);
					mp = conf.MergePolicy;
					mp.SetNoCFSRatio(useCompoundFile ? 1.0 : 0.0);
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("TEST: open writer for forceMerge");
					}
					writer = new IndexWriter(dir, conf);
					policy = (TestDeletionPolicy.KeepAllDeletionPolicy)writer.Config.GetIndexDeletionPolicy
						();
					writer.ForceMerge(1);
					writer.Dispose();
				}
				AreEqual(needsMerging ? 2 : 1, policy.numOnInit);
				// If we are not auto committing then there should
				// be exactly 2 commits (one per close above):
				AreEqual(1 + (needsMerging ? 1 : 0), policy.numOnCommit);
				// Test listCommits
				ICollection<IndexCommit> commits = DirectoryReader.ListCommits(dir);
				// 2 from closing writer
				AreEqual(1 + (needsMerging ? 1 : 0), commits.Count);
				// Make sure we can open a reader on each commit:
				foreach (IndexCommit commit in commits)
				{
					IndexReader r = DirectoryReader.Open(commit);
					r.Dispose();
				}
				// Simplistic check: just verify all segments_N's still
				// exist, and, I can open a reader on each:
				dir.DeleteFile(IndexFileNames.SEGMENTS_GEN);
				long gen = SegmentInfos.GetLastCommitGeneration(dir);
				while (gen > 0)
				{
					IndexReader reader = DirectoryReader.Open(dir);
					reader.Dispose();
					dir.DeleteFile(IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, string.Empty
						, gen));
					gen--;
					if (gen > 0)
					{
						// Now that we've removed a commit point, which
						// should have orphan'd at least one index file.
						// Open & close a writer and 
						//HM:revisit 
						//assert that it
						// actually removed something:
						int preCount = dir.ListAll().Length;
						writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
							(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND).SetIndexDeletionPolicy
							(policy));
						writer.Dispose();
						int postCount = dir.ListAll().Length;
						IsTrue(postCount < preCount);
					}
				}
				dir.Dispose();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestOpenPriorSnapshot()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetIndexDeletionPolicy(new TestDeletionPolicy.KeepAllDeletionPolicy
				(this, dir)).SetMaxBufferedDocs(2)).SetMergePolicy(NewLogMergePolicy(10)));
			TestDeletionPolicy.KeepAllDeletionPolicy policy = (TestDeletionPolicy.KeepAllDeletionPolicy
				)writer.Config.GetIndexDeletionPolicy();
			for (int i = 0; i < 10; i++)
			{
				AddDoc(writer);
				if ((1 + i) % 2 == 0)
				{
					writer.Commit();
				}
			}
			writer.Dispose();
			ICollection<IndexCommit> commits = DirectoryReader.ListCommits(dir);
			AreEqual(5, commits.Count);
			IndexCommit lastCommit = null;
			foreach (IndexCommit commit in commits)
			{
				if (lastCommit == null || commit.GetGeneration() > lastCommit.GetGeneration())
				{
					lastCommit = commit;
				}
			}
			IsTrue(lastCommit != null);
			// Now add 1 doc and merge
			writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetIndexDeletionPolicy(policy));
			AddDoc(writer);
			AreEqual(11, writer.NumDocs);
			writer.ForceMerge(1);
			writer.Dispose();
			AreEqual(6, DirectoryReader.ListCommits(dir).Count);
			// Now open writer on the commit just before merge:
			writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetIndexDeletionPolicy(policy).SetIndexCommit(lastCommit));
			AreEqual(10, writer.NumDocs);
			// Should undo our rollback:
			writer.Rollback();
			DirectoryReader r = DirectoryReader.Open(dir);
			// Still merged, still 11 docs
			AreEqual(1, r.Leaves.Count);
			AreEqual(11, r.NumDocs);
			r.Dispose();
			writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetIndexDeletionPolicy(policy).SetIndexCommit(lastCommit));
			AreEqual(10, writer.NumDocs);
			// Commits the rollback:
			writer.Dispose();
			// Now 8 because we made another commit
			AreEqual(7, DirectoryReader.ListCommits(dir).Count);
			r = DirectoryReader.Open(dir);
			// Not fully merged because we rolled it back, and now only
			// 10 docs
			IsTrue(r.Leaves.Count > 1);
			AreEqual(10, r.NumDocs);
			r.Dispose();
			// Re-merge
			writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetIndexDeletionPolicy(policy));
			writer.ForceMerge(1);
			writer.Dispose();
			r = DirectoryReader.Open(dir);
			AreEqual(1, r.Leaves.Count);
			AreEqual(10, r.NumDocs);
			r.Dispose();
			// Now open writer on the commit just before merging,
			// but this time keeping only the last commit:
			writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetIndexCommit(lastCommit));
			AreEqual(10, writer.NumDocs);
			// Reader still sees fully merged index, because writer
			// opened on the prior commit has not yet committed:
			r = DirectoryReader.Open(dir);
			AreEqual(1, r.Leaves.Count);
			AreEqual(10, r.NumDocs);
			r.Dispose();
			writer.Dispose();
			// Now reader sees not-fully-merged index:
			r = DirectoryReader.Open(dir);
			IsTrue(r.Leaves.Count > 1);
			AreEqual(10, r.NumDocs);
			r.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestKeepNoneOnInitDeletionPolicy()
		{
			for (int pass = 0; pass < 2; pass++)
			{
				bool useCompoundFile = (pass % 2) != 0;
				Directory dir = NewDirectory();
				IndexWriterConfig conf = ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
					, new MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode.CREATE).SetIndexDeletionPolicy
					(new TestDeletionPolicy.KeepNoneOnInitDeletionPolicy(this)).SetMaxBufferedDocs(10
					));
				MergePolicy mp = conf.MergePolicy;
				mp.SetNoCFSRatio(useCompoundFile ? 1.0 : 0.0);
				IndexWriter writer = new IndexWriter(dir, conf);
				TestDeletionPolicy.KeepNoneOnInitDeletionPolicy policy = (TestDeletionPolicy.KeepNoneOnInitDeletionPolicy
					)writer.Config.GetIndexDeletionPolicy();
				for (int i = 0; i < 107; i++)
				{
					AddDoc(writer);
				}
				writer.Dispose();
				conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetOpenMode
					(IndexWriterConfig.OpenMode.APPEND).SetIndexDeletionPolicy(policy);
				mp = conf.MergePolicy;
				mp.SetNoCFSRatio(1.0);
				writer = new IndexWriter(dir, conf);
				policy = (TestDeletionPolicy.KeepNoneOnInitDeletionPolicy)writer.Config.GetIndexDeletionPolicy
					();
				writer.ForceMerge(1);
				writer.Dispose();
				AreEqual(2, policy.numOnInit);
				// If we are not auto committing then there should
				// be exactly 2 commits (one per close above):
				AreEqual(2, policy.numOnCommit);
				// Simplistic check: just verify the index is in fact
				// readable:
				IndexReader reader = DirectoryReader.Open(dir);
				reader.Dispose();
				dir.Dispose();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestKeepLastNDeletionPolicy()
		{
			int N = 5;
			for (int pass = 0; pass < 2; pass++)
			{
				bool useCompoundFile = (pass % 2) != 0;
				Directory dir = NewDirectory();
				TestDeletionPolicy.KeepLastNDeletionPolicy policy = new TestDeletionPolicy.KeepLastNDeletionPolicy
					(this, N);
				for (int j = 0; j < N + 1; j++)
				{
					IndexWriterConfig conf = ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
						, new MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode.CREATE).SetIndexDeletionPolicy
						(policy).SetMaxBufferedDocs(10));
					MergePolicy mp = conf.MergePolicy;
					mp.SetNoCFSRatio(useCompoundFile ? 1.0 : 0.0);
					IndexWriter writer = new IndexWriter(dir, conf);
					policy = (TestDeletionPolicy.KeepLastNDeletionPolicy)writer.Config.GetIndexDeletionPolicy
						();
					for (int i = 0; i < 17; i++)
					{
						AddDoc(writer);
					}
					writer.ForceMerge(1);
					writer.Dispose();
				}
				IsTrue(policy.numDelete > 0);
				AreEqual(N + 1, policy.numOnInit);
				AreEqual(N + 1, policy.numOnCommit);
				// Simplistic check: just verify only the past N segments_N's still
				// exist, and, I can open a reader on each:
				dir.DeleteFile(IndexFileNames.SEGMENTS_GEN);
				long gen = SegmentInfos.GetLastCommitGeneration(dir);
				for (int i_1 = 0; i_1 < N + 1; i_1++)
				{
					try
					{
						IndexReader reader = DirectoryReader.Open(dir);
						reader.Dispose();
						if (i_1 == N)
						{
							Fail("should have failed on commits prior to last " + N);
						}
					}
					catch (IOException e)
					{
						if (i_1 != N)
						{
							throw;
						}
					}
					if (i_1 < N)
					{
						dir.DeleteFile(IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, string.Empty
							, gen));
					}
					gen--;
				}
				dir.Dispose();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestKeepLastNDeletionPolicyWithCreates()
		{
			int N = 10;
			for (int pass = 0; pass < 2; pass++)
			{
				bool useCompoundFile = (pass % 2) != 0;
				Directory dir = NewDirectory();
				IndexWriterConfig conf = ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
					, new MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode.CREATE).SetIndexDeletionPolicy
					(new TestDeletionPolicy.KeepLastNDeletionPolicy(this, N)).SetMaxBufferedDocs(10)
					);
				MergePolicy mp = conf.MergePolicy;
				mp.SetNoCFSRatio(useCompoundFile ? 1.0 : 0.0);
				IndexWriter writer = new IndexWriter(dir, conf);
				TestDeletionPolicy.KeepLastNDeletionPolicy policy = (TestDeletionPolicy.KeepLastNDeletionPolicy
					)writer.Config.GetIndexDeletionPolicy();
				writer.Dispose();
				Term searchTerm = new Term("content", "aaa");
				Query query = new TermQuery(searchTerm);
				for (int i = 0; i < N + 1; i++)
				{
					conf = ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
						(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND).SetIndexDeletionPolicy
						(policy).SetMaxBufferedDocs(10));
					mp = conf.MergePolicy;
					mp.SetNoCFSRatio(useCompoundFile ? 1.0 : 0.0);
					writer = new IndexWriter(dir, conf);
					policy = (TestDeletionPolicy.KeepLastNDeletionPolicy)writer.Config.GetIndexDeletionPolicy
						();
					for (int j = 0; j < 17; j++)
					{
						AddDocWithID(writer, i * (N + 1) + j);
					}
					// this is a commit
					writer.Dispose();
					conf = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetIndexDeletionPolicy
						(policy).SetMergePolicy(NoMergePolicy.COMPOUND_FILES);
					writer = new IndexWriter(dir, conf);
					policy = (TestDeletionPolicy.KeepLastNDeletionPolicy)writer.Config.GetIndexDeletionPolicy
						();
					writer.DeleteDocuments(new Term("id", string.Empty + (i * (N + 1) + 3)));
					// this is a commit
					writer.Dispose();
					IndexReader reader = DirectoryReader.Open(dir);
					IndexSearcher searcher = NewSearcher(reader);
					ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
					AreEqual(16, hits.Length);
					reader.Dispose();
					writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
						(Random())).SetOpenMode(IndexWriterConfig.OpenMode.CREATE).SetIndexDeletionPolicy
						(policy));
					policy = (TestDeletionPolicy.KeepLastNDeletionPolicy)writer.Config.GetIndexDeletionPolicy
						();
					// This will not commit: there are no changes
					// pending because we opened for "create":
					writer.Dispose();
				}
				AreEqual(3 * (N + 1) + 1, policy.numOnInit);
				AreEqual(3 * (N + 1) + 1, policy.numOnCommit);
				IndexReader rwReader = DirectoryReader.Open(dir);
				IndexSearcher searcher_1 = NewSearcher(rwReader);
				ScoreDoc[] hits_1 = searcher_1.Search(query, null, 1000).ScoreDocs;
				AreEqual(0, hits_1.Length);
				// Simplistic check: just verify only the past N segments_N's still
				// exist, and, I can open a reader on each:
				long gen = SegmentInfos.GetLastCommitGeneration(dir);
				dir.DeleteFile(IndexFileNames.SEGMENTS_GEN);
				int expectedCount = 0;
				rwReader.Dispose();
				for (int i_1 = 0; i_1 < N + 1; i_1++)
				{
					try
					{
						IndexReader reader = DirectoryReader.Open(dir);
						// Work backwards in commits on what the expected
						// count should be.
						searcher_1 = NewSearcher(reader);
						hits_1 = searcher_1.Search(query, null, 1000).ScoreDocs;
						AreEqual(expectedCount, hits_1.Length);
						if (expectedCount == 0)
						{
							expectedCount = 16;
						}
						else
						{
							if (expectedCount == 16)
							{
								expectedCount = 17;
							}
							else
							{
								if (expectedCount == 17)
								{
									expectedCount = 0;
								}
							}
						}
						reader.Dispose();
						if (i_1 == N)
						{
							Fail("should have failed on commits before last " + N);
						}
					}
					catch (IOException e)
					{
						if (i_1 != N)
						{
							throw;
						}
					}
					if (i_1 < N)
					{
						dir.DeleteFile(IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, string.Empty
							, gen));
					}
					gen--;
				}
				dir.Dispose();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void AddDocWithID(IndexWriter writer, int id)
		{
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewTextField("content", "aaa", Field.Store.NO));
			doc.Add(NewStringField("id", string.Empty + id, Field.Store.NO));
			writer.AddDocument(doc);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void AddDoc(IndexWriter writer)
		{
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewTextField("content", "aaa", Field.Store.NO));
			writer.AddDocument(doc);
		}
	}
}
