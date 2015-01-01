using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Index;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.TestFramework;
using NUnit.Framework;


namespace Lucene.Net.Test.Index
{
	public class TestTwoPhaseCommitTool : LuceneTestCase
	{
		private class TwoPhaseCommitImpl : ITwoPhaseCommit
		{
			internal static bool commitCalled = false;

			internal readonly bool failOnPrepare;

			internal readonly bool failOnCommit;

			internal readonly bool failOnRollback;

			internal bool rollbackCalled = false;

			internal IDictionary<string, string> prepareCommitData = null;

			internal IDictionary<string, string> commitData = null;

			public TwoPhaseCommitImpl(bool failOnPrepare, bool failOnCommit, bool failOnRollback
				)
			{
				this.failOnPrepare = failOnPrepare;
				this.failOnCommit = failOnCommit;
				this.failOnRollback = failOnRollback;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public virtual void PrepareCommit()
			{
				PrepareCommit(null);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public virtual void PrepareCommit(IDictionary<string, string> commitData)
			{
				this.prepareCommitData = commitData;
				AssertFalse("commit should not have been called before all prepareCommit were"
					, commitCalled);
				if (failOnPrepare)
				{
					throw new IOException("failOnPrepare");
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public virtual void Commit()
			{
				Commit(null);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public virtual void Commit(IDictionary<string, string> commitData)
			{
				this.commitData = commitData;
				commitCalled = true;
				if (failOnCommit)
				{
					throw new SystemException("failOnCommit");
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public virtual void Rollback()
			{
				rollbackCalled = true;
				if (failOnRollback)
				{
					throw new Exception("failOnRollback");
				}
			}
		}

		[SetUp]
		public override void SetUp()
		{
			base.SetUp();
			TwoPhaseCommitImpl.commitCalled = false;
		}

		// reset count before every test
		[Test]
		public virtual void TestPrepareThenCommit()
		{
			// tests that prepareCommit() is called on all objects before commit()
			var objects = new TwoPhaseCommitImpl[2];
			for (int i = 0; i < objects.Length; i++)
			{
				objects[i] = new TwoPhaseCommitImpl(false, false, false);
			}
			// following call will fail if commit() is called before all prepare() were
			TwoPhaseCommitTool.Execute(objects);
		}

		[Test]
		public virtual void TestRollback()
		{
			// tests that rollback is called if failure occurs at any stage
			int numObjects = Random().Next(8) + 3;
			// between [3, 10]
			var objects = new TwoPhaseCommitImpl[numObjects];
			for (int i = 0; i < objects.Length; i++)
			{
				bool failOnPrepare = Random().NextBoolean();
				// we should not hit failures on commit usually
				bool failOnCommit = Random().NextDouble() < 0.05;
				bool railOnRollback = Random().NextBoolean();
				objects[i] = new TwoPhaseCommitImpl(failOnPrepare, failOnCommit
					, railOnRollback);
			}
			bool anyFailure = false;
			try
			{
				TwoPhaseCommitTool.Execute(objects);
			}
			catch
			{
				anyFailure = true;
			}
			if (anyFailure)
			{
				// if any failure happened, ensure that rollback was called on all.
				foreach (TestTwoPhaseCommitTool.TwoPhaseCommitImpl tpc in objects)
				{
					AssertTrue("rollback was not called while a failure occurred during the 2-phase commit"
						, tpc.rollbackCalled);
				}
			}
		}

		[Test]
		public virtual void TestNullTPCs()
		{
			int numObjects = Random().Next(4) + 3;
			// between [3, 6]
			ITwoPhaseCommit[] tpcs = new ITwoPhaseCommit[numObjects];
			bool setNull = false;
			for (int i = 0; i < tpcs.Length; i++)
			{
				bool isNull = Random().NextDouble() < 0.3;
				if (isNull)
				{
					setNull = true;
					tpcs[i] = null;
				}
				else
				{
					tpcs[i] = new TwoPhaseCommitImpl(false, false, false);
				}
			}
			if (!setNull)
			{
				// none of the TPCs were picked to be null, pick one at random
				int idx = Random().Next(numObjects);
				tpcs[idx] = null;
			}
			// following call would fail if TPCTool won't handle null TPCs properly
			TwoPhaseCommitTool.Execute(tpcs);
		}
	}
}
