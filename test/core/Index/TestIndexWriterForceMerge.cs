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
	public class TestIndexWriterForceMerge : LuceneTestCase
	{
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestPartialMerge()
		{
			Directory dir = NewDirectory();
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewStringField("content", "aaa", Field.Store.NO));
			int incrMin = TEST_NIGHTLY ? 15 : 40;
			for (int numDocs = 10; numDocs < 500; numDocs += TestUtil.NextInt(Random(), incrMin
				, 5 * incrMin))
			{
				LogDocMergePolicy ldmp = new LogDocMergePolicy();
				ldmp.SetMinMergeDocs(1);
				ldmp.SetMergeFactor(5);
				IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
					(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode
					.CREATE).SetMaxBufferedDocs(2)).SetMergePolicy(ldmp));
				for (int j = 0; j < numDocs; j++)
				{
					writer.AddDocument(doc);
				}
				writer.Close();
				SegmentInfos sis = new SegmentInfos();
				sis.Read(dir);
				int segCount = sis.Size();
				ldmp = new LogDocMergePolicy();
				ldmp.SetMergeFactor(5);
				writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
					(Random())).SetMergePolicy(ldmp));
				writer.ForceMerge(3);
				writer.Close();
				sis = new SegmentInfos();
				sis.Read(dir);
				int optSegCount = sis.Size();
				if (segCount < 3)
				{
					AreEqual(segCount, optSegCount);
				}
				else
				{
					AreEqual(3, optSegCount);
				}
			}
			dir.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestMaxNumSegments2()
		{
			Directory dir = NewDirectory();
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewStringField("content", "aaa", Field.Store.NO));
			LogDocMergePolicy ldmp = new LogDocMergePolicy();
			ldmp.SetMinMergeDocs(1);
			ldmp.SetMergeFactor(4);
			IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2)).SetMergePolicy
				(ldmp).SetMergeScheduler(new ConcurrentMergeScheduler()));
			for (int iter = 0; iter < 10; iter++)
			{
				for (int i = 0; i < 19; i++)
				{
					writer.AddDocument(doc);
				}
				writer.Commit();
				writer.WaitForMerges();
				writer.Commit();
				SegmentInfos sis = new SegmentInfos();
				sis.Read(dir);
				int segCount = sis.Size();
				writer.ForceMerge(7);
				writer.Commit();
				writer.WaitForMerges();
				sis = new SegmentInfos();
				sis.Read(dir);
				int optSegCount = sis.Size();
				if (segCount < 7)
				{
					AreEqual(segCount, optSegCount);
				}
				else
				{
					AreEqual("seg: " + segCount, 7, optSegCount);
				}
			}
			writer.Close();
			dir.Close();
		}

		/// <summary>
		/// Make sure forceMerge doesn't use any more than 1X
		/// starting index size as its temporary free space
		/// required.
		/// </summary>
		/// <remarks>
		/// Make sure forceMerge doesn't use any more than 1X
		/// starting index size as its temporary free space
		/// required.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestForceMergeTempSpaceUsage()
		{
			MockDirectoryWrapper dir = NewMockDirectory();
			IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(10)).SetMergePolicy
				(NewLogMergePolicy()));
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: config1=" + writer.GetConfig());
			}
			for (int j = 0; j < 500; j++)
			{
				TestIndexWriter.AddDocWithIndex(writer, j);
			}
			int termIndexInterval = writer.GetConfig().GetTermIndexInterval();
			// force one extra segment w/ different doc store so
			// we see the doc stores get merged
			writer.Commit();
			TestIndexWriter.AddDocWithIndex(writer, 500);
			writer.Close();
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: start disk usage");
			}
			long startDiskUsage = 0;
			string[] files = dir.ListAll();
			for (int i = 0; i < files.Length; i++)
			{
				startDiskUsage += dir.FileLength(files[i]);
				if (VERBOSE)
				{
					System.Console.Out.WriteLine(files[i] + ": " + dir.FileLength(files[i]));
				}
			}
			dir.ResetMaxUsedSizeInBytes();
			dir.SetTrackDiskUsage(true);
			// Import to use same term index interval else a
			// smaller one here could increase the disk usage and
			// cause a false failure:
			writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND).SetTermIndexInterval
				(termIndexInterval)).SetMergePolicy(NewLogMergePolicy()));
			writer.ForceMerge(1);
			writer.Close();
			long maxDiskUsage = dir.GetMaxUsedSizeInBytes();
			IsTrue("forceMerge used too much temporary space: starting usage was "
				 + startDiskUsage + " bytes; max temp usage was " + maxDiskUsage + " but should have been "
				 + (4 * startDiskUsage) + " (= 4X starting usage)", maxDiskUsage <= 4 * startDiskUsage
				);
			dir.Close();
		}

		// Test calling forceMerge(1, false) whereby forceMerge is kicked
		// off but we don't wait for it to finish (but
		// writer.close()) does wait
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestBackgroundForceMerge()
		{
			Directory dir = NewDirectory();
			for (int pass = 0; pass < 2; pass++)
			{
				IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
					(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode
					.CREATE).SetMaxBufferedDocs(2)).SetMergePolicy(NewLogMergePolicy(51)));
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(NewStringField("field", "aaa", Field.Store.NO));
				for (int i = 0; i < 100; i++)
				{
					writer.AddDocument(doc);
				}
				writer.ForceMerge(1, false);
				if (0 == pass)
				{
					writer.Close();
					DirectoryReader reader = DirectoryReader.Open(dir);
					AreEqual(1, reader.Leaves().Count);
					reader.Close();
				}
				else
				{
					// Get another segment to flush so we can verify it is
					// NOT included in the merging
					writer.AddDocument(doc);
					writer.AddDocument(doc);
					writer.Close();
					DirectoryReader reader = DirectoryReader.Open(dir);
					IsTrue(reader.Leaves().Count > 1);
					reader.Close();
					SegmentInfos infos = new SegmentInfos();
					infos.Read(dir);
					AreEqual(2, infos.Size());
				}
			}
			dir.Close();
		}
	}
}
