using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Lucene.Net.Index;
using Lucene.Net.Store;
using NUnit.Framework;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Test.Index
{
	public class TestPersistentSnapshotDeletionPolicy : TestSnapshotDeletionPolicy
	{
		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.SetUp]
		public override void SetUp()
		{
			base.SetUp();
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.TearDown]
		public override void TearDown()
		{
			base.TearDown();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private SnapshotDeletionPolicy GetDeletionPolicy(Directory dir)
		{
			return new PersistentSnapshotDeletionPolicy(new KeepOnlyLastCommitDeletionPolicy(
				), dir, IndexWriterConfig.OpenMode.CREATE);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestExistingSnapshots()
		{
			int numSnapshots = 3;
			MockDirectoryWrapper dir = NewMockDirectory();
			IndexWriter writer = new IndexWriter(dir, GetConfig(Random(), GetDeletionPolicy(dir
				)));
			PersistentSnapshotDeletionPolicy psdp = (PersistentSnapshotDeletionPolicy)writer.Config.IndexDeletionPolicy;
			IsNull(psdp.GetLastSaveFile());
			PrepareIndexAndSnapshots(psdp, writer, numSnapshots);
			IsNotNull(psdp.GetLastSaveFile());
			writer.Dispose();
			// Make sure only 1 save file exists:
			int count = 0;
			foreach (string file in dir.ListAll())
			{
				if (file.StartsWith(PersistentSnapshotDeletionPolicy.SNAPSHOTS_PREFIX))
				{
					count++;
				}
			}
			AreEqual(1, count);
			// Make sure we fsync:
			dir.Crash();
			dir.ClearCrash();
			// Re-initialize and verify snapshots were persisted
			psdp = new PersistentSnapshotDeletionPolicy(new KeepOnlyLastCommitDeletionPolicy(
				), dir, IndexWriterConfig.OpenMode.APPEND);
			writer = new IndexWriter(dir, GetConfig(Random(), psdp));
			psdp = (PersistentSnapshotDeletionPolicy)writer.Config.IndexDeletionPolicy;
			AreEqual(numSnapshots, psdp.GetSnapshots().Count);
			AreEqual(numSnapshots, psdp.GetSnapshotCount());
			AssertSnapshotExists(dir, psdp, numSnapshots, false);
			writer.AddDocument(new Lucene.Net.Documents.Document());
			writer.Commit();
			snapshots.Add(psdp.Snapshot());
			AreEqual(numSnapshots + 1, psdp.GetSnapshots().Count);
			AreEqual(numSnapshots + 1, psdp.GetSnapshotCount());
			AssertSnapshotExists(dir, psdp, numSnapshots + 1, false);
			writer.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestNoSnapshotInfos()
		{
			Directory dir = NewDirectory();
			new PersistentSnapshotDeletionPolicy(new KeepOnlyLastCommitDeletionPolicy(), dir, 
				IndexWriterConfig.OpenMode.CREATE);
			dir.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestMissingSnapshots()
		{
			Directory dir = NewDirectory();
			try
			{
				new PersistentSnapshotDeletionPolicy(new KeepOnlyLastCommitDeletionPolicy(), dir, 
					IndexWriterConfig.OpenMode.APPEND);
				Fail("did not hit expected exception");
			}
			catch (InvalidOperationException)
			{
			}
			// expected
			dir.Dispose();
		}

		[Test]
		public virtual void TestExceptionDuringSave()
		{
			MockDirectoryWrapper dir = NewMockDirectory();
			dir.FailOn(new AnonymousFailure());
			IndexWriter writer = new IndexWriter(dir, GetConfig(Random(), new PersistentSnapshotDeletionPolicy
				(new KeepOnlyLastCommitDeletionPolicy(), dir, IndexWriterConfig.OpenMode.CREATE_OR_APPEND
				)));
			writer.AddDocument(new Lucene.Net.Documents.Document());
			writer.Commit();
			PersistentSnapshotDeletionPolicy psdp = (PersistentSnapshotDeletionPolicy)writer.Config.IndexDeletionPolicy;
			try
			{
				psdp.Snapshot();
			}
			catch (IOException ioe)
			{
				if (ioe.Message.Equals("now fail on purpose"))
				{
				}
				else
				{
					// ok
					throw;
				}
			}
			AreEqual(0, psdp.GetSnapshotCount());
			writer.Dispose();
			AreEqual(1, DirectoryReader.ListCommits(dir).Count);
			dir.Dispose();
		}

		private sealed class AnonymousFailure : MockDirectoryWrapper.Failure
		{
		    /// <exception cref="System.IO.IOException"></exception>
			public override void Eval(MockDirectoryWrapper dir)
		    {

		        var trace = new StackTrace(Thread.CurrentThread, true).GetFrames();
				for (int i = 0; i < trace.Length; i++)
				{
					if (typeof(PersistentSnapshotDeletionPolicy).FullName.Equals(trace[i].GetMethod().DeclaringType.FullName) && "persist".Equals(trace[i].GetMethod().Name,StringComparison.CurrentCultureIgnoreCase))
					{
						throw new IOException("now fail on purpose");
					}
				}
			}
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestSnapshotRelease()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, GetConfig(Random(), GetDeletionPolicy(dir
				)));
			PersistentSnapshotDeletionPolicy psdp = (PersistentSnapshotDeletionPolicy)writer.Config.IndexDeletionPolicy;
			PrepareIndexAndSnapshots(psdp, writer, 1);
			writer.Dispose();
			psdp.Release(snapshots[0]);
			psdp = new PersistentSnapshotDeletionPolicy(new KeepOnlyLastCommitDeletionPolicy(
				), dir, IndexWriterConfig.OpenMode.APPEND);
			AssertEquals("Should have no snapshots !", 0, psdp.GetSnapshotCount
				());
			dir.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestSnapshotReleaseByGeneration()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, GetConfig(Random(), GetDeletionPolicy(dir
				)));
			PersistentSnapshotDeletionPolicy psdp = (PersistentSnapshotDeletionPolicy)writer.Config.IndexDeletionPolicy;
			PrepareIndexAndSnapshots(psdp, writer, 1);
			writer.Dispose();
			psdp.Release(snapshots[0]);
			psdp = new PersistentSnapshotDeletionPolicy(new KeepOnlyLastCommitDeletionPolicy(
				), dir, IndexWriterConfig.OpenMode.APPEND);
			AssertEquals("Should have no snapshots !", 0, psdp.GetSnapshotCount());
			dir.Dispose();
		}
	}
}
