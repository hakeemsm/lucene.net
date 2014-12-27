/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.IO;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Sharpen;

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
			PersistentSnapshotDeletionPolicy psdp = (PersistentSnapshotDeletionPolicy)writer.
				GetConfig().GetIndexDeletionPolicy();
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
			psdp = (PersistentSnapshotDeletionPolicy)writer.Config.GetIndexDeletionPolicy
				();
			AreEqual(numSnapshots, psdp.GetSnapshots().Count);
			AreEqual(numSnapshots, psdp.GetSnapshotCount());
			AssertSnapshotExists(dir, psdp, numSnapshots, false);
			writer.AddDocument(new Lucene.Net.Documents.Document());
			writer.Commit();
			snapshots.AddItem(psdp.Snapshot());
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

		/// <exception cref="System.Exception"></exception>
		public virtual void TestExceptionDuringSave()
		{
			MockDirectoryWrapper dir = NewMockDirectory();
			dir.FailOn(new _Failure_118());
			IndexWriter writer = new IndexWriter(dir, GetConfig(Random(), new PersistentSnapshotDeletionPolicy
				(new KeepOnlyLastCommitDeletionPolicy(), dir, IndexWriterConfig.OpenMode.CREATE_OR_APPEND
				)));
			writer.AddDocument(new Lucene.Net.Documents.Document());
			writer.Commit();
			PersistentSnapshotDeletionPolicy psdp = (PersistentSnapshotDeletionPolicy)writer.
				GetConfig().GetIndexDeletionPolicy();
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

		private sealed class _Failure_118 : MockDirectoryWrapper.Failure
		{
			public _Failure_118()
			{
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Eval(MockDirectoryWrapper dir)
			{
				StackTraceElement[] trace = Sharpen.Thread.CurrentThread().GetStackTrace();
				for (int i = 0; i < trace.Length; i++)
				{
					if (typeof(PersistentSnapshotDeletionPolicy).FullName.Equals(trace[i].GetClassName
						()) && "persist".Equals(trace[i].GetMethodName()))
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
			PersistentSnapshotDeletionPolicy psdp = (PersistentSnapshotDeletionPolicy)writer.
				GetConfig().GetIndexDeletionPolicy();
			PrepareIndexAndSnapshots(psdp, writer, 1);
			writer.Dispose();
			psdp.Release(snapshots[0]);
			psdp = new PersistentSnapshotDeletionPolicy(new KeepOnlyLastCommitDeletionPolicy(
				), dir, IndexWriterConfig.OpenMode.APPEND);
			AreEqual("Should have no snapshots !", 0, psdp.GetSnapshotCount
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
			PersistentSnapshotDeletionPolicy psdp = (PersistentSnapshotDeletionPolicy)writer.
				GetConfig().GetIndexDeletionPolicy();
			PrepareIndexAndSnapshots(psdp, writer, 1);
			writer.Dispose();
			psdp.Release(snapshots[0].GetGeneration());
			psdp = new PersistentSnapshotDeletionPolicy(new KeepOnlyLastCommitDeletionPolicy(
				), dir, IndexWriterConfig.OpenMode.APPEND);
			AreEqual("Should have no snapshots !", 0, psdp.GetSnapshotCount
				());
			dir.Dispose();
		}
	}
}
