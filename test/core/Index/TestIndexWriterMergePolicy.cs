/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	public class TestIndexWriterMergePolicy : LuceneTestCase
	{
		// Test the normal case
		/// <exception cref="System.IO.IOException"></exception>
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
			writer.Close();
			dir.Close();
		}

		// Test to see if there is over merge
		/// <exception cref="System.IO.IOException"></exception>
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
				if (writer.GetNumBufferedDocuments() + writer.GetSegmentCount() >= 18)
				{
					noOverMerge = true;
				}
			}
			NUnit.Framework.Assert.IsTrue(noOverMerge);
			writer.Close();
			dir.Close();
		}

		// Test the case where flush is forced after every addDoc
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestForceFlush()
		{
			Directory dir = NewDirectory();
			LogDocMergePolicy mp = new LogDocMergePolicy();
			mp.SetMinMergeDocs(100);
			mp.SetMergeFactor(10);
			IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(10)).SetMergePolicy
				(mp));
			for (int i = 0; i < 100; i++)
			{
				AddDoc(writer);
				writer.Close();
				mp = new LogDocMergePolicy();
				mp.SetMergeFactor(10);
				writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
					, new MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND).SetMaxBufferedDocs
					(10)).SetMergePolicy(mp));
				mp.SetMinMergeDocs(100);
				CheckInvariants(writer);
			}
			writer.Close();
			dir.Close();
		}

		// Test the case where mergeFactor changes
		/// <exception cref="System.IO.IOException"></exception>
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
			((LogMergePolicy)writer.GetConfig().GetMergePolicy()).SetMergeFactor(5);
			// merge policy only fixes segments on levels where merges
			// have been triggered, so check invariants after all adds
			for (int i_1 = 0; i_1 < 10; i_1++)
			{
				AddDoc(writer);
			}
			CheckInvariants(writer);
			writer.Close();
			dir.Close();
		}

		// Test the case where both mergeFactor and maxBufferedDocs change
		/// <exception cref="System.IO.IOException"></exception>
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
				writer.Close();
				writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
					, new MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND).SetMaxBufferedDocs
					(101)).SetMergePolicy(new LogDocMergePolicy()).SetMergeScheduler(new SerialMergeScheduler
					()));
			}
			writer.Close();
			LogDocMergePolicy ldmp = new LogDocMergePolicy();
			ldmp.SetMergeFactor(10);
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
			writer.Close();
			dir.Close();
		}

		// Test the case where a merge results in no doc at all
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestMergeDocCount0()
		{
			Directory dir = NewDirectory();
			LogDocMergePolicy ldmp = new LogDocMergePolicy();
			ldmp.SetMergeFactor(100);
			IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(10)).SetMergePolicy
				(ldmp));
			for (int i = 0; i < 250; i++)
			{
				AddDoc(writer);
				CheckInvariants(writer);
			}
			writer.Close();
			// delete some docs without merging
			writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetMergePolicy(NoMergePolicy.NO_COMPOUND_FILES));
			writer.DeleteDocuments(new Term("content", "aaa"));
			writer.Close();
			ldmp = new LogDocMergePolicy();
			ldmp.SetMergeFactor(5);
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
			NUnit.Framework.Assert.AreEqual(10, writer.MaxDoc());
			writer.Close();
			dir.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void AddDoc(IndexWriter writer)
		{
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			doc.Add(NewTextField("content", "aaa", Field.Store.NO));
			writer.AddDocument(doc);
		}

		private void CheckInvariants(IndexWriter writer)
		{
			writer.WaitForMerges();
			int maxBufferedDocs = writer.GetConfig().GetMaxBufferedDocs();
			int mergeFactor = ((LogMergePolicy)writer.GetConfig().GetMergePolicy()).GetMergeFactor
				();
			int maxMergeDocs = ((LogMergePolicy)writer.GetConfig().GetMergePolicy()).GetMaxMergeDocs
				();
			int ramSegmentCount = writer.GetNumBufferedDocuments();
			NUnit.Framework.Assert.IsTrue(ramSegmentCount < maxBufferedDocs);
			int lowerBound = -1;
			int upperBound = maxBufferedDocs;
			int numSegments = 0;
			int segmentCount = writer.GetSegmentCount();
			for (int i = segmentCount - 1; i >= 0; i--)
			{
				int docCount = writer.GetDocCount(i);
				NUnit.Framework.Assert.IsTrue("docCount=" + docCount + " lowerBound=" + lowerBound
					 + " upperBound=" + upperBound + " i=" + i + " segmentCount=" + segmentCount + " index="
					 + writer.SegString() + " config=" + writer.GetConfig(), docCount > lowerBound);
				if (docCount <= upperBound)
				{
					numSegments++;
				}
				else
				{
					if (upperBound * mergeFactor <= maxMergeDocs)
					{
						NUnit.Framework.Assert.IsTrue("maxMergeDocs=" + maxMergeDocs + "; numSegments=" +
							 numSegments + "; upperBound=" + upperBound + "; mergeFactor=" + mergeFactor + "; segs="
							 + writer.SegString() + " config=" + writer.GetConfig(), numSegments < mergeFactor
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
				NUnit.Framework.Assert.IsTrue(numSegments < mergeFactor);
			}
		}

		private const double EPSILON = 1E-14;

		public virtual void TestSetters()
		{
			AssertSetters(new LogByteSizeMergePolicy());
			AssertSetters(new LogDocMergePolicy());
		}

		private void AssertSetters(MergePolicy lmp)
		{
			lmp.SetMaxCFSSegmentSizeMB(2.0);
			NUnit.Framework.Assert.AreEqual(2.0, lmp.GetMaxCFSSegmentSizeMB(), EPSILON);
			lmp.SetMaxCFSSegmentSizeMB(double.PositiveInfinity);
			NUnit.Framework.Assert.AreEqual(long.MaxValue / 1024 / 1024., lmp.GetMaxCFSSegmentSizeMB
				(), EPSILON * long.MaxValue);
			lmp.SetMaxCFSSegmentSizeMB(long.MaxValue / 1024 / 1024.);
			NUnit.Framework.Assert.AreEqual(long.MaxValue / 1024 / 1024., lmp.GetMaxCFSSegmentSizeMB
				(), EPSILON * long.MaxValue);
			try
			{
				lmp.SetMaxCFSSegmentSizeMB(-2.0);
				NUnit.Framework.Assert.Fail("Didn't throw IllegalArgumentException");
			}
			catch (ArgumentException)
			{
			}
		}
		// pass
		// TODO: Add more checks for other non-double setters!
	}
}
