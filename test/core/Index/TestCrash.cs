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

namespace Lucene.Net.Index
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
			((ConcurrentMergeScheduler)writer.GetConfig().GetMergeScheduler()).SetSuppressExceptions
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
			ConcurrentMergeScheduler cms = (ConcurrentMergeScheduler)writer.GetConfig().GetMergeScheduler
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
			IsTrue(reader.NumDocs() < 157);
			reader.Close();
			// Make a new dir, copying from the crashed dir, and
			// open IW on it, to confirm IW "recovers" after a
			// crash:
			Directory dir2 = NewDirectory(dir);
			dir.Close();
			new RandomIndexWriter(Random(), dir2).Close();
			dir2.Close();
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
			writer.Close();
			IndexReader reader = DirectoryReader.Open(dir);
			IsTrue(reader.NumDocs() < 314);
			reader.Close();
			// Make a new dir, copying from the crashed dir, and
			// open IW on it, to confirm IW "recovers" after a
			// crash:
			Directory dir2 = NewDirectory(dir);
			dir.Close();
			new RandomIndexWriter(Random(), dir2).Close();
			dir2.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestCrashAfterReopen()
		{
			IndexWriter writer = InitIndex(Random(), false);
			MockDirectoryWrapper dir = (MockDirectoryWrapper)writer.GetDirectory();
			// We create leftover files because merging could be
			// running when we crash:
			dir.SetAssertNoUnrefencedFilesOnClose(false);
			writer.Close();
			writer = InitIndex(Random(), dir, false);
			AreEqual(314, writer.MaxDoc);
			Crash(writer);
			IndexReader reader = DirectoryReader.Open(dir);
			IsTrue(reader.NumDocs() >= 157);
			reader.Close();
			// Make a new dir, copying from the crashed dir, and
			// open IW on it, to confirm IW "recovers" after a
			// crash:
			Directory dir2 = NewDirectory(dir);
			dir.Close();
			new RandomIndexWriter(Random(), dir2).Close();
			dir2.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestCrashAfterClose()
		{
			IndexWriter writer = InitIndex(Random(), false);
			MockDirectoryWrapper dir = (MockDirectoryWrapper)writer.GetDirectory();
			writer.Close();
			dir.Crash();
			IndexReader reader = DirectoryReader.Open(dir);
			AreEqual(157, reader.NumDocs());
			reader.Close();
			dir.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestCrashAfterCloseNoWait()
		{
			IndexWriter writer = InitIndex(Random(), false);
			MockDirectoryWrapper dir = (MockDirectoryWrapper)writer.GetDirectory();
			writer.Close(false);
			dir.Crash();
			IndexReader reader = DirectoryReader.Open(dir);
			AreEqual(157, reader.NumDocs());
			reader.Close();
			dir.Close();
		}
	}
}
