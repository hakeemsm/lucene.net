/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Test.Index
{
	public class TestCrash : LuceneTestCase
	{
		/// <exception cref="System.IO.IOException"></exception>
		private IndexWriter InitIndex(Random random, bool initialCommit)
		{
			return InitIndex(random, NewMockDirectory(random), initialCommit);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private IndexWriter InitIndex(Random random, MockDirectoryWrapper dir, bool initialCommit
			)
		{
			dir.SetLockFactory(NoLockFactory.GetNoLockFactory());
			IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(random)).SetMaxBufferedDocs(10)).SetMergeScheduler
				(new ConcurrentMergeScheduler()));
			((ConcurrentMergeScheduler)writer.Config.GetMergeScheduler()).SetSuppressExceptions
				();
			if (initialCommit)
			{
				writer.Commit();
			}
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewTextField("content", "aaa", Field.Store.NO));
			doc.Add(NewTextField("id", "0", Field.Store.NO));
			for (int i = 0; i < 157; i++)
			{
				writer.AddDocument(doc);
			}
			return writer;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void Crash(IndexWriter writer)
		{
			MockDirectoryWrapper dir = (MockDirectoryWrapper)writer.GetDirectory();
			ConcurrentMergeScheduler cms = (ConcurrentMergeScheduler)writer.Config.GetMergeScheduler
				();
			cms.Sync();
			dir.Crash();
			cms.Sync();
			dir.ClearCrash();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestCrashWhileIndexing()
		{
			// This test relies on being able to open a reader before any commit
			// happened, so we must create an initial commit just to allow that, but
			// before any documents were added.
			IndexWriter writer = InitIndex(Random(), true);
			MockDirectoryWrapper dir = (MockDirectoryWrapper)writer.GetDirectory();
			// We create leftover files because merging could be
			// running when we crash:
			dir.SetAssertNoUnrefencedFilesOnClose(false);
			Crash(writer);
			IndexReader reader = DirectoryReader.Open(dir);
			IsTrue(reader.NumDocs < 157);
			reader.Dispose();
			// Make a new dir, copying from the crashed dir, and
			// open IW on it, to confirm IW "recovers" after a
			// crash:
			Directory dir2 = NewDirectory(dir);
			dir.Dispose();
			new RandomIndexWriter(Random(), dir2).Dispose();
			dir2.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestWriterAfterCrash()
		{
			// This test relies on being able to open a reader before any commit
			// happened, so we must create an initial commit just to allow that, but
			// before any documents were added.
			System.Console.Out.WriteLine("TEST: initIndex");
			IndexWriter writer = InitIndex(Random(), true);
			System.Console.Out.WriteLine("TEST: done initIndex");
			MockDirectoryWrapper dir = (MockDirectoryWrapper)writer.GetDirectory();
			// We create leftover files because merging could be
			// running / store files could be open when we crash:
			dir.SetAssertNoUnrefencedFilesOnClose(false);
			dir.SetPreventDoubleWrite(false);
			System.Console.Out.WriteLine("TEST: now crash");
			Crash(writer);
			writer = InitIndex(Random(), dir, false);
			writer.Dispose();
			IndexReader reader = DirectoryReader.Open(dir);
			IsTrue(reader.NumDocs < 314);
			reader.Dispose();
			// Make a new dir, copying from the crashed dir, and
			// open IW on it, to confirm IW "recovers" after a
			// crash:
			Directory dir2 = NewDirectory(dir);
			dir.Dispose();
			new RandomIndexWriter(Random(), dir2).Dispose();
			dir2.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestCrashAfterReopen()
		{
			IndexWriter writer = InitIndex(Random(), false);
			MockDirectoryWrapper dir = (MockDirectoryWrapper)writer.GetDirectory();
			// We create leftover files because merging could be
			// running when we crash:
			dir.SetAssertNoUnrefencedFilesOnClose(false);
			writer.Dispose();
			writer = InitIndex(Random(), dir, false);
			AreEqual(314, writer.MaxDoc);
			Crash(writer);
			IndexReader reader = DirectoryReader.Open(dir);
			IsTrue(reader.NumDocs >= 157);
			reader.Dispose();
			// Make a new dir, copying from the crashed dir, and
			// open IW on it, to confirm IW "recovers" after a
			// crash:
			Directory dir2 = NewDirectory(dir);
			dir.Dispose();
			new RandomIndexWriter(Random(), dir2).Dispose();
			dir2.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestCrashAfterClose()
		{
			IndexWriter writer = InitIndex(Random(), false);
			MockDirectoryWrapper dir = (MockDirectoryWrapper)writer.GetDirectory();
			writer.Dispose();
			dir.Crash();
			IndexReader reader = DirectoryReader.Open(dir);
			AreEqual(157, reader.NumDocs);
			reader.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestCrashAfterCloseNoWait()
		{
			IndexWriter writer = InitIndex(Random(), false);
			MockDirectoryWrapper dir = (MockDirectoryWrapper)writer.GetDirectory();
			writer.Close(false);
			dir.Crash();
			IndexReader reader = DirectoryReader.Open(dir);
			AreEqual(157, reader.NumDocs);
			reader.Dispose();
			dir.Dispose();
		}
	}
}
