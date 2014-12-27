/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Test.Index
{
	/// <summary>Test class to illustrate using IndexDeletionPolicy to provide multi-level rollback capability.
	/// 	</summary>
	/// <remarks>
	/// Test class to illustrate using IndexDeletionPolicy to provide multi-level rollback capability.
	/// This test case creates an index of records 1 to 100, introducing a commit point every 10 records.
	/// A "keep all" deletion policy is used to ensure we keep all commit points for testing purposes
	/// </remarks>
	public class TestTransactionRollback : LuceneTestCase
	{
		private static readonly string FIELD_RECORD_ID = "record_id";

		private Directory dir;

		//Rolls back index to a chosen ID
		/// <exception cref="System.Exception"></exception>
		private void RollBackLast(int id)
		{
			// System.out.println("Attempting to rollback to "+id);
			string ids = "-" + id;
			IndexCommit last = null;
			ICollection<IndexCommit> commits = DirectoryReader.ListCommits(dir);
			for (Iterator<IndexCommit> iterator = commits.Iterator(); iterator.HasNext(); )
			{
				IndexCommit commit = iterator.Next();
				IDictionary<string, string> ud = commit.GetUserData();
				if (ud.Count > 0)
				{
					if (ud.Get("index").EndsWith(ids))
					{
						last = commit;
					}
				}
			}
			if (last == null)
			{
				throw new RuntimeException("Couldn't find commit point " + id);
			}
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())).SetIndexDeletionPolicy(new TestTransactionRollback.RollbackDeletionPolicy
				(this, id)).SetIndexCommit(last));
			IDictionary<string, string> data = new Dictionary<string, string>();
			data.Put("index", "Rolled back to 1-" + id);
			w.SetCommitData(data);
			w.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestRepeatedRollBacks()
		{
			int expectedLastRecordId = 100;
			while (expectedLastRecordId > 10)
			{
				expectedLastRecordId -= 10;
				RollBackLast(expectedLastRecordId);
				BitSet expecteds = new BitSet(100);
				expecteds.Set(1, (expectedLastRecordId + 1), true);
				CheckExpecteds(expecteds);
			}
		}

		/// <exception cref="System.Exception"></exception>
		private void CheckExpecteds(BitSet expecteds)
		{
			IndexReader r = DirectoryReader.Open(dir);
			//Perhaps not the most efficient approach but meets our
			//needs here.
			Bits liveDocs = MultiFields.GetLiveDocs(r);
			for (int i = 0; i < r.MaxDoc; i++)
			{
				if (liveDocs == null || liveDocs.Get(i))
				{
					string sval = r.Document(i).Get(FIELD_RECORD_ID);
					if (sval != null)
					{
						int val = System.Convert.ToInt32(sval);
						IsTrue("Did not expect document #" + val, expecteds.Get(val
							));
						expecteds.Set(val, false);
					}
				}
			}
			r.Dispose();
			AreEqual("Should have 0 docs remaining ", 0, expecteds.Cardinality
				());
		}

		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			dir = NewDirectory();
			//Build index, of records 1 to 100, committing after each batch of 10
			IndexDeletionPolicy sdp = new TestTransactionRollback.KeepAllDeletionPolicy(this);
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())).SetIndexDeletionPolicy(sdp));
			for (int currentRecordId = 1; currentRecordId <= 100; currentRecordId++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(NewTextField(FIELD_RECORD_ID, string.Empty + currentRecordId, Field.Store
					.YES));
				w.AddDocument(doc);
				if (currentRecordId % 10 == 0)
				{
					IDictionary<string, string> data = new Dictionary<string, string>();
					data.Put("index", "records 1-" + currentRecordId);
					w.SetCommitData(data);
					w.Commit();
				}
			}
			w.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			dir.Dispose();
			base.TearDown();
		}

		internal class RollbackDeletionPolicy : IndexDeletionPolicy
		{
			private int rollbackPoint;

			public RollbackDeletionPolicy(TestTransactionRollback _enclosing, int rollbackPoint
				)
			{
				this._enclosing = _enclosing;
				// Rolls back to previous commit point
				this.rollbackPoint = rollbackPoint;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void OnCommit<_T0>(IList<_T0> commits)
			{
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void OnInit<_T0>(IList<_T0> commits)
			{
				foreach (IndexCommit commit in commits)
				{
					IDictionary<string, string> userData = commit.GetUserData();
					if (userData.Count > 0)
					{
						// Label for a commit point is "Records 1-30"
						// This code reads the last id ("30" in this example) and deletes it
						// if it is after the desired rollback point
						string x = userData.Get("index");
						string lastVal = Sharpen.Runtime.Substring(x, x.LastIndexOf("-") + 1);
						int last = System.Convert.ToInt32(lastVal);
						if (last > this.rollbackPoint)
						{
							commit.Delete();
						}
					}
				}
			}

			private readonly TestTransactionRollback _enclosing;
		}

		internal class DeleteLastCommitPolicy : IndexDeletionPolicy
		{
			/// <exception cref="System.IO.IOException"></exception>
			public override void OnCommit<_T0>(IList<_T0> commits)
			{
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void OnInit<_T0>(IList<_T0> commits)
			{
				commits[commits.Count - 1].Delete();
			}

			internal DeleteLastCommitPolicy(TestTransactionRollback _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly TestTransactionRollback _enclosing;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestRollbackDeletionPolicy()
		{
			for (int i = 0; i < 2; i++)
			{
				// Unless you specify a prior commit point, rollback
				// should not work:
				new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(
					Random())).SetIndexDeletionPolicy(new TestTransactionRollback.DeleteLastCommitPolicy
					(this))).Dispose();
				IndexReader r = DirectoryReader.Open(dir);
				AreEqual(100, r.NumDocs);
				r.Dispose();
			}
		}

		internal class KeepAllDeletionPolicy : IndexDeletionPolicy
		{
			// Keeps all commit points (used to build index)
			/// <exception cref="System.IO.IOException"></exception>
			public override void OnCommit<_T0>(IList<_T0> commits)
			{
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void OnInit<_T0>(IList<_T0> commits)
			{
			}

			internal KeepAllDeletionPolicy(TestTransactionRollback _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly TestTransactionRollback _enclosing;
		}
	}
}
