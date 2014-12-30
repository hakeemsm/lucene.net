using System;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.TestFramework;
using NUnit.Framework;

namespace Lucene.Net.Test.Index
{
    [TestFixture]
	public class TestIndexWriterMergePolicy : LuceneTestCase
	{
		// Test the normal case
		[Test]
		public virtual void TestNormalCase()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(10)).SetMergePolicy
				(new LogDocMergePolicy()));
			for (int i = 0; i < 100; i++)
			{
				AddDoc(writer);
				CheckInvariants(writer);
			}
			writer.Dispose();
			dir.Dispose();
		}

		// Test to see if there is over merge
		[Test]
		public virtual void TestNoOverMerge()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(10)).SetMergePolicy
				(new LogDocMergePolicy()));
			bool noOverMerge = false;
			for (int i = 0; i < 100; i++)
			{
				AddDoc(writer);
				CheckInvariants(writer);
				if (writer.NumBufferedDocuments + writer.SegmentCount >= 18)
				{
					noOverMerge = true;
				}
			}
			AssertTrue(noOverMerge);
			writer.Dispose();
			dir.Dispose();
		}

		// Test the case where flush is forced after every addDoc
		[Test]
		public virtual void TestForceFlush()
		{
			Directory dir = NewDirectory();
			LogDocMergePolicy mp = new LogDocMergePolicy {MinMergeDocs = (100), MergeFactor = (10)};
		    IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(10)).SetMergePolicy
				(mp));
			for (int i = 0; i < 100; i++)
			{
				AddDoc(writer);
				writer.Dispose();
				mp = new LogDocMergePolicy();
				mp.MergeFactor = (10);
				writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
					, new MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND).SetMaxBufferedDocs
					(10)).SetMergePolicy(mp));
				mp.MinMergeDocs = (100);
				CheckInvariants(writer);
			}
			writer.Dispose();
			dir.Dispose();
		}

		// Test the case where mergeFactor changes
		[Test]
		public virtual void TestMergeFactorChange()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(10)).SetMergePolicy
				(NewLogMergePolicy()).SetMergeScheduler(new SerialMergeScheduler()));
			for (int i = 0; i < 250; i++)
			{
				AddDoc(writer);
				CheckInvariants(writer);
			}
			((LogMergePolicy)writer.Config.MergePolicy).MergeFactor = (5);
			// merge policy only fixes segments on levels where merges
			// have been triggered, so check invariants after all adds
			for (int i_1 = 0; i_1 < 10; i_1++)
			{
				AddDoc(writer);
			}
			CheckInvariants(writer);
			writer.Dispose();
			dir.Dispose();
		}

		// Test the case where both mergeFactor and maxBufferedDocs change
		[Test]
		public virtual void TestMaxBufferedDocsChange()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(101)).SetMergePolicy
				(new LogDocMergePolicy()).SetMergeScheduler(new SerialMergeScheduler()));
			// leftmost* segment has 1 doc
			// rightmost* segment has 100 docs
			for (int i = 1; i <= 100; i++)
			{
				for (int j = 0; j < i; j++)
				{
					AddDoc(writer);
					CheckInvariants(writer);
				}
				writer.Dispose();
				writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
					, new MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND).SetMaxBufferedDocs
					(101)).SetMergePolicy(new LogDocMergePolicy()).SetMergeScheduler(new SerialMergeScheduler
					()));
			}
			writer.Dispose();
			LogDocMergePolicy ldmp = new LogDocMergePolicy();
			ldmp.MergeFactor = (10);
			writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND).SetMaxBufferedDocs
				(10)).SetMergePolicy(ldmp).SetMergeScheduler(new SerialMergeScheduler()));
			// merge policy only fixes segments on levels where merges
			// have been triggered, so check invariants after all adds
			for (int i_1 = 0; i_1 < 100; i_1++)
			{
				AddDoc(writer);
			}
			CheckInvariants(writer);
			for (int i_2 = 100; i_2 < 1000; i_2++)
			{
				AddDoc(writer);
			}
			writer.Commit();
			writer.WaitForMerges();
			writer.Commit();
			CheckInvariants(writer);
			writer.Dispose();
			dir.Dispose();
		}

		// Test the case where a merge results in no doc at all
		[Test]
		public virtual void TestMergeDocCount0()
		{
			Directory dir = NewDirectory();
			LogDocMergePolicy ldmp = new LogDocMergePolicy();
			ldmp.MergeFactor = (100);
			IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(10)).SetMergePolicy
				(ldmp));
			for (int i = 0; i < 250; i++)
			{
				AddDoc(writer);
				CheckInvariants(writer);
			}
			writer.Dispose();
			// delete some docs without merging
			writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetMergePolicy(NoMergePolicy.NO_COMPOUND_FILES));
			writer.DeleteDocuments(new Term("content", "aaa"));
			writer.Dispose();
			ldmp = new LogDocMergePolicy();
			ldmp.MergeFactor = (5);
			writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND).SetMaxBufferedDocs
				(10)).SetMergePolicy(ldmp).SetMergeScheduler(new ConcurrentMergeScheduler()));
			// merge factor is changed, so check invariants after all adds
			for (int i_1 = 0; i_1 < 10; i_1++)
			{
				AddDoc(writer);
			}
			writer.Commit();
			writer.WaitForMerges();
			writer.Commit();
			CheckInvariants(writer);
			AreEqual(10, writer.MaxDoc);
			writer.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void AddDoc(IndexWriter writer)
		{
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewTextField("content", "aaa", Field.Store.NO));
			writer.AddDocument(doc);
		}

		private void CheckInvariants(IndexWriter writer)
		{
			writer.WaitForMerges();
			int maxBufferedDocs = writer.Config.MaxBufferedDocs;
			int mergeFactor = ((LogMergePolicy)writer.Config.MergePolicy).MergeFactor;
			int maxMergeDocs = ((LogMergePolicy)writer.Config.MergePolicy).MaxMergeDocs;
			int ramSegmentCount = writer.NumBufferedDocuments;
			AssertTrue(ramSegmentCount < maxBufferedDocs);
			int lowerBound = -1;
			int upperBound = maxBufferedDocs;
			int numSegments = 0;
			int segmentCount = writer.SegmentCount;
			for (int i = segmentCount - 1; i >= 0; i--)
			{
				int docCount = writer.GetDocCount(i);
				AssertTrue("docCount=" + docCount + " lowerBound=" + lowerBound
					 + " upperBound=" + upperBound + " i=" + i + " segmentCount=" + segmentCount + " index="
					 + writer.SegString() + " config=" + writer.Config, docCount > lowerBound);
				if (docCount <= upperBound)
				{
					numSegments++;
				}
				else
				{
					if (upperBound * mergeFactor <= maxMergeDocs)
					{
						AssertTrue("maxMergeDocs=" + maxMergeDocs + "; numSegments=" +
							 numSegments + "; upperBound=" + upperBound + "; mergeFactor=" + mergeFactor + "; segs="
							 + writer.SegString() + " config=" + writer.Config, numSegments < mergeFactor
							);
					}
					do
					{
						lowerBound = upperBound;
						upperBound *= mergeFactor;
					}
					while (docCount > upperBound);
					numSegments = 1;
				}
			}
			if (upperBound * mergeFactor <= maxMergeDocs)
			{
				AssertTrue(numSegments < mergeFactor);
			}
		}

		private const double EPSILON = 1E-14;

        [Test]
		public virtual void TestSetters()
		{
			AssertSetters(new LogByteSizeMergePolicy());
			AssertSetters(new LogDocMergePolicy());
		}

		private void AssertSetters(MergePolicy lmp)
		{
			lmp.SetMaxCFSSegmentSizeMB(2.0);
			AreEqual(2.0, lmp.GetMaxCFSSegmentSizeMB(), EPSILON);
			lmp.SetMaxCFSSegmentSizeMB(double.PositiveInfinity);
			AreEqual(long.MaxValue / 1024 / 1024, lmp.GetMaxCFSSegmentSizeMB
				(), EPSILON * long.MaxValue);
			lmp.SetMaxCFSSegmentSizeMB(long.MaxValue / 1024 / 1024);
			AreEqual(long.MaxValue / 1024 / 1024, lmp.GetMaxCFSSegmentSizeMB
				(), EPSILON * long.MaxValue);
			try
			{
				lmp.SetMaxCFSSegmentSizeMB(-2.0);
				Fail("Didn't throw IllegalArgumentException");
			}
			catch (ArgumentException)
			{
			}
		}
		// pass
		// TODO: Add more checks for other non-double setters!
	}
}
