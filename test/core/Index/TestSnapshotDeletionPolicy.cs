/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Lucene.Net.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	public class TestSnapshotDeletionPolicy : LuceneTestCase
	{
		public static readonly string INDEX_PATH = "test.snapshots";

		//
		// This was developed for Lucene In Action,
		// http://lucenebook.com
		//
		protected internal virtual IndexWriterConfig GetConfig(Random random, IndexDeletionPolicy
			 dp)
		{
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(random));
			if (dp != null)
			{
				conf.SetIndexDeletionPolicy(dp);
			}
			return conf;
		}

		/// <exception cref="System.Exception"></exception>
		protected internal virtual void CheckSnapshotExists(Directory dir, IndexCommit c)
		{
			string segFileName = c.GetSegmentsFileName();
			NUnit.Framework.Assert.IsTrue("segments file not found in directory: " + segFileName
				, SlowFileExists(dir, segFileName));
		}

		/// <exception cref="System.Exception"></exception>
		protected internal virtual void CheckMaxDoc(IndexCommit commit, int expectedMaxDoc
			)
		{
			IndexReader reader = DirectoryReader.Open(commit);
			try
			{
				NUnit.Framework.Assert.AreEqual(expectedMaxDoc, reader.MaxDoc());
			}
			finally
			{
				reader.Close();
			}
		}

		protected internal IList<IndexCommit> snapshots = new AList<IndexCommit>();

		/// <exception cref="Sharpen.RuntimeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual void PrepareIndexAndSnapshots(SnapshotDeletionPolicy sdp
			, IndexWriter writer, int numSnapshots)
		{
			for (int i = 0; i < numSnapshots; i++)
			{
				// create dummy document to trigger commit.
				writer.AddDocument(new Lucene.Net.Document.Document());
				writer.Commit();
				snapshots.AddItem(sdp.Snapshot());
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual SnapshotDeletionPolicy GetDeletionPolicy()
		{
			return new SnapshotDeletionPolicy(new KeepOnlyLastCommitDeletionPolicy());
		}

		/// <exception cref="System.Exception"></exception>
		protected internal virtual void AssertSnapshotExists(Directory dir, SnapshotDeletionPolicy
			 sdp, int numSnapshots, bool checkIndexCommitSame)
		{
			for (int i = 0; i < numSnapshots; i++)
			{
				IndexCommit snapshot = snapshots[i];
				CheckMaxDoc(snapshot, i + 1);
				CheckSnapshotExists(dir, snapshot);
				if (checkIndexCommitSame)
				{
					NUnit.Framework.Assert.AreSame(snapshot, sdp.GetIndexCommit(snapshot.GetGeneration
						()));
				}
				else
				{
					NUnit.Framework.Assert.AreEqual(snapshot.GetGeneration(), sdp.GetIndexCommit(snapshot
						.GetGeneration()).GetGeneration());
				}
			}
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestSnapshotDeletionPolicy()
		{
			Directory fsDir = NewDirectory();
			RunTest(Random(), fsDir);
			fsDir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		private void RunTest(Random random, Directory dir)
		{
			// Run for ~1 seconds
			long stopTime = Runtime.CurrentTimeMillis() + 1000;
			SnapshotDeletionPolicy dp = GetDeletionPolicy();
			IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(random)).SetIndexDeletionPolicy(dp).SetMaxBufferedDocs
				(2)));
			// Verify we catch misuse:
			try
			{
				dp.Snapshot();
				NUnit.Framework.Assert.Fail("did not hit exception");
			}
			catch (InvalidOperationException)
			{
			}
			// expected
			dp = (SnapshotDeletionPolicy)writer.GetConfig().GetIndexDeletionPolicy();
			writer.Commit();
			Sharpen.Thread t = new _Thread_122(writer, stopTime);
			t.Start();
			do
			{
				// While the above indexing thread is running, take many
				// backups:
				BackupIndex(dir, dp);
				Sharpen.Thread.Sleep(20);
			}
			while (t.IsAlive());
			t.Join();
			// Add one more document to force writer to commit a
			// final segment, so deletion policy has a chance to
			// delete again:
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			FieldType customType = new FieldType(TextField.TYPE_STORED);
			customType.SetStoreTermVectors(true);
			customType.SetStoreTermVectorPositions(true);
			customType.SetStoreTermVectorOffsets(true);
			doc.Add(NewField("content", "aaa", customType));
			writer.AddDocument(doc);
			// Make sure we don't have any leftover files in the
			// directory:
			writer.Close();
			TestIndexWriter.AssertNoUnreferencedFiles(dir, "some files were not deleted but should have been"
				);
		}

		private sealed class _Thread_122 : Sharpen.Thread
		{
			public _Thread_122(IndexWriter writer, long stopTime)
			{
				this.writer = writer;
				this.stopTime = stopTime;
			}

			public override void Run()
			{
				Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
					();
				FieldType customType = new FieldType(TextField.TYPE_STORED);
				customType.SetStoreTermVectors(true);
				customType.SetStoreTermVectorPositions(true);
				customType.SetStoreTermVectorOffsets(true);
				doc.Add(LuceneTestCase.NewField("content", "aaa", customType));
				do
				{
					for (int i = 0; i < 27; i++)
					{
						try
						{
							writer.AddDocument(doc);
						}
						catch (Exception t)
						{
							Sharpen.Runtime.PrintStackTrace(t, System.Console.Out);
							NUnit.Framework.Assert.Fail("addDocument failed");
						}
						if (i % 2 == 0)
						{
							try
							{
								writer.Commit();
							}
							catch (Exception e)
							{
								throw new RuntimeException(e);
							}
						}
					}
					try
					{
						Sharpen.Thread.Sleep(1);
					}
					catch (Exception ie)
					{
						throw new ThreadInterruptedException(ie);
					}
				}
				while (Runtime.CurrentTimeMillis() < stopTime);
			}

			private readonly IndexWriter writer;

			private readonly long stopTime;
		}

		/// <summary>Example showing how to use the SnapshotDeletionPolicy to take a backup.</summary>
		/// <remarks>
		/// Example showing how to use the SnapshotDeletionPolicy to take a backup.
		/// This method does not really do a backup; instead, it reads every byte of
		/// every file just to test that the files indeed exist and are readable even
		/// while the index is changing.
		/// </remarks>
		/// <exception cref="System.Exception"></exception>
		public virtual void BackupIndex(Directory dir, SnapshotDeletionPolicy dp)
		{
			// To backup an index we first take a snapshot:
			IndexCommit snapshot = dp.Snapshot();
			try
			{
				CopyFiles(dir, snapshot);
			}
			finally
			{
				// Make sure to release the snapshot, otherwise these
				// files will never be deleted during this IndexWriter
				// session:
				dp.Release(snapshot);
			}
		}

		/// <exception cref="System.Exception"></exception>
		private void CopyFiles(Directory dir, IndexCommit cp)
		{
			// While we hold the snapshot, and nomatter how long
			// we take to do the backup, the IndexWriter will
			// never delete the files in the snapshot:
			ICollection<string> files = cp.GetFileNames();
			foreach (string fileName in files)
			{
				// NOTE: in a real backup you would not use
				// readFile; you would need to use something else
				// that copies the file to a backup location.  This
				// could even be a spawned shell process (eg "tar",
				// "zip") that takes the list of files and builds a
				// backup.
				ReadFile(dir, fileName);
			}
		}

		internal byte[] buffer = new byte[4096];

		/// <exception cref="System.Exception"></exception>
		private void ReadFile(Directory dir, string name)
		{
			IndexInput input = dir.OpenInput(name, NewIOContext(Random()));
			try
			{
				long size = dir.FileLength(name);
				long bytesLeft = size;
				while (bytesLeft > 0)
				{
					int numToRead;
					if (bytesLeft < buffer.Length)
					{
						numToRead = (int)bytesLeft;
					}
					else
					{
						numToRead = buffer.Length;
					}
					input.ReadBytes(buffer, 0, numToRead, false);
					bytesLeft -= numToRead;
				}
				// Don't do this in your real backups!  This is just
				// to force a backup to take a somewhat long time, to
				// make sure we are exercising the fact that the
				// IndexWriter should not delete this file even when I
				// take my time reading it.
				Sharpen.Thread.Sleep(1);
			}
			finally
			{
				input.Close();
			}
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestBasicSnapshots()
		{
			int numSnapshots = 3;
			// Create 3 snapshots: snapshot0, snapshot1, snapshot2
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, GetConfig(Random(), GetDeletionPolicy()
				));
			SnapshotDeletionPolicy sdp = (SnapshotDeletionPolicy)writer.GetConfig().GetIndexDeletionPolicy
				();
			PrepareIndexAndSnapshots(sdp, writer, numSnapshots);
			writer.Close();
			NUnit.Framework.Assert.AreEqual(numSnapshots, sdp.GetSnapshots().Count);
			NUnit.Framework.Assert.AreEqual(numSnapshots, sdp.GetSnapshotCount());
			AssertSnapshotExists(dir, sdp, numSnapshots, true);
			// open a reader on a snapshot - should succeed.
			DirectoryReader.Open(snapshots[0]).Close();
			// open a new IndexWriter w/ no snapshots to keep and 
			//HM:revisit 
			//assert that all snapshots are gone.
			sdp = GetDeletionPolicy();
			writer = new IndexWriter(dir, GetConfig(Random(), sdp));
			writer.DeleteUnusedFiles();
			writer.Close();
			NUnit.Framework.Assert.AreEqual("no snapshots should exist", 1, DirectoryReader.ListCommits
				(dir).Count);
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestMultiThreadedSnapshotting()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, GetConfig(Random(), GetDeletionPolicy()
				));
			SnapshotDeletionPolicy sdp = (SnapshotDeletionPolicy)writer.GetConfig().GetIndexDeletionPolicy
				();
			Sharpen.Thread[] threads = new Sharpen.Thread[10];
			IndexCommit[] snapshots = new IndexCommit[threads.Length];
			for (int i = 0; i < threads.Length; i++)
			{
				int finalI = i;
				threads[i] = new _Thread_287(writer, snapshots, finalI, sdp);
				threads[i].SetName("t" + i);
			}
			foreach (Sharpen.Thread t in threads)
			{
				t.Start();
			}
			foreach (Sharpen.Thread t_1 in threads)
			{
				t_1.Join();
			}
			// Do one last commit, so that after we release all snapshots, we stay w/ one commit
			writer.AddDocument(new Lucene.Net.Document.Document());
			writer.Commit();
			for (int i_1 = 0; i_1 < threads.Length; i_1++)
			{
				sdp.Release(snapshots[i_1]);
				writer.DeleteUnusedFiles();
			}
			NUnit.Framework.Assert.AreEqual(1, DirectoryReader.ListCommits(dir).Count);
			writer.Close();
			dir.Close();
		}

		private sealed class _Thread_287 : Sharpen.Thread
		{
			public _Thread_287(IndexWriter writer, IndexCommit[] snapshots, int finalI, SnapshotDeletionPolicy
				 sdp)
			{
				this.writer = writer;
				this.snapshots = snapshots;
				this.finalI = finalI;
				this.sdp = sdp;
			}

			public override void Run()
			{
				try
				{
					writer.AddDocument(new Lucene.Net.Document.Document());
					writer.Commit();
					snapshots[finalI] = sdp.Snapshot();
				}
				catch (Exception e)
				{
					throw new RuntimeException(e);
				}
			}

			private readonly IndexWriter writer;

			private readonly IndexCommit[] snapshots;

			private readonly int finalI;

			private readonly SnapshotDeletionPolicy sdp;
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestRollbackToOldSnapshot()
		{
			int numSnapshots = 2;
			Directory dir = NewDirectory();
			SnapshotDeletionPolicy sdp = GetDeletionPolicy();
			IndexWriter writer = new IndexWriter(dir, GetConfig(Random(), sdp));
			PrepareIndexAndSnapshots(sdp, writer, numSnapshots);
			writer.Close();
			// now open the writer on "snapshot0" - make sure it succeeds
			writer = new IndexWriter(dir, GetConfig(Random(), sdp).SetIndexCommit(snapshots[0
				]));
			// this does the actual rollback
			writer.Commit();
			writer.DeleteUnusedFiles();
			AssertSnapshotExists(dir, sdp, numSnapshots - 1, false);
			writer.Close();
			// but 'snapshot1' files will still exist (need to release snapshot before they can be deleted).
			string segFileName = snapshots[1].GetSegmentsFileName();
			NUnit.Framework.Assert.IsTrue("snapshot files should exist in the directory: " + 
				segFileName, SlowFileExists(dir, segFileName));
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestReleaseSnapshot()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, GetConfig(Random(), GetDeletionPolicy()
				));
			SnapshotDeletionPolicy sdp = (SnapshotDeletionPolicy)writer.GetConfig().GetIndexDeletionPolicy
				();
			PrepareIndexAndSnapshots(sdp, writer, 1);
			// Create another commit - we must do that, because otherwise the "snapshot"
			// files will still remain in the index, since it's the last commit.
			writer.AddDocument(new Lucene.Net.Document.Document());
			writer.Commit();
			// Release
			string segFileName = snapshots[0].GetSegmentsFileName();
			sdp.Release(snapshots[0]);
			writer.DeleteUnusedFiles();
			writer.Close();
			NUnit.Framework.Assert.IsFalse("segments file should not be found in dirctory: " 
				+ segFileName, SlowFileExists(dir, segFileName));
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestSnapshotLastCommitTwice()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, GetConfig(Random(), GetDeletionPolicy()
				));
			SnapshotDeletionPolicy sdp = (SnapshotDeletionPolicy)writer.GetConfig().GetIndexDeletionPolicy
				();
			writer.AddDocument(new Lucene.Net.Document.Document());
			writer.Commit();
			IndexCommit s1 = sdp.Snapshot();
			IndexCommit s2 = sdp.Snapshot();
			NUnit.Framework.Assert.AreSame(s1, s2);
			// should be the same instance
			// create another commit
			writer.AddDocument(new Lucene.Net.Document.Document());
			writer.Commit();
			// release "s1" should not delete "s2"
			sdp.Release(s1);
			writer.DeleteUnusedFiles();
			CheckSnapshotExists(dir, s2);
			writer.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestMissingCommits()
		{
			// Tests the behavior of SDP when commits that are given at ctor are missing
			// on onInit().
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, GetConfig(Random(), GetDeletionPolicy()
				));
			SnapshotDeletionPolicy sdp = (SnapshotDeletionPolicy)writer.GetConfig().GetIndexDeletionPolicy
				();
			writer.AddDocument(new Lucene.Net.Document.Document());
			writer.Commit();
			IndexCommit s1 = sdp.Snapshot();
			// create another commit, not snapshotted.
			writer.AddDocument(new Lucene.Net.Document.Document());
			writer.Close();
			// open a new writer w/ KeepOnlyLastCommit policy, so it will delete "s1"
			// commit.
			new IndexWriter(dir, GetConfig(Random(), null)).Close();
			NUnit.Framework.Assert.IsFalse("snapshotted commit should not exist", SlowFileExists
				(dir, s1.GetSegmentsFileName()));
			dir.Close();
		}
	}
}
