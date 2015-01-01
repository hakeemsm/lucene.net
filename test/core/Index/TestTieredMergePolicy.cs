using System;
using Lucene.Net.Analysis;
using Lucene.Net.Codecs;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.TestFramework.Index;
using Lucene.Net.TestFramework.Util;
using NUnit.Framework;


namespace Lucene.Net.Test.Index
{
	public class TestTieredMergePolicy : BaseMergePolicyTestCase
	{
		protected override Lucene.Net.Index.MergePolicy MergePolicy()
		{
			return NewTieredMergePolicy();
		}

		[Test]
		public virtual void TestForceMergeDeletes()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			TieredMergePolicy tmp = NewTieredMergePolicy();
			conf.SetMergePolicy(tmp);
			conf.SetMaxBufferedDocs(4);
			tmp.SetMaxMergeAtOnce(100);
			tmp.SetSegmentsPerTier(100);
			tmp.SetForceMergeDeletesPctAllowed(30.0);
			IndexWriter w = new IndexWriter(dir, conf);
			for (int i = 0; i < 80; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				{
				    NewTextField("content", "aaa " + (i%4), Field.Store.NO)
				};
			    w.AddDocument(doc);
			}
			AreEqual(80, w.MaxDoc);
			AreEqual(80, w.NumDocs);
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("\nTEST: delete docs");
			}
			w.DeleteDocuments(new Term("content", "0"));
			w.ForceMergeDeletes();
			AreEqual(80, w.MaxDoc);
			AreEqual(60, w.NumDocs);
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("\nTEST: forceMergeDeletes2");
			}
			((TieredMergePolicy)w.Config.MergePolicy).SetForceMergeDeletesPctAllowed
				(10.0);
			w.ForceMergeDeletes();
			AreEqual(60, w.MaxDoc);
			AreEqual(60, w.NumDocs);
			w.Dispose();
			dir.Dispose();
		}

		[Test]
		public virtual void TestPartialMerge()
		{
			int num = AtLeast(10);
			for (int iter = 0; iter < num; iter++)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: iter=" + iter);
				}
				Directory dir = NewDirectory();
				IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
					(Random()));
				conf.SetMergeScheduler(new SerialMergeScheduler());
				TieredMergePolicy tmp = NewTieredMergePolicy();
				conf.SetMergePolicy(tmp);
				conf.SetMaxBufferedDocs(2);
				tmp.SetMaxMergeAtOnce(3);
				tmp.SetSegmentsPerTier(6);
				IndexWriter w = new IndexWriter(dir, conf);
				int maxCount = 0;
				int numDocs = TestUtil.NextInt(Random(), 20, 100);
				for (int i = 0; i < numDocs; i++)
				{
					Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
						();
					doc.Add(NewTextField("content", "aaa " + (i % 4), Field.Store.NO));
					w.AddDocument(doc);
					int count = w.SegmentCount;
					maxCount = Math.Max(count, maxCount);
					AssertTrue("count=" + count + " maxCount=" + maxCount, count >=
						 maxCount - 3);
				}
				w.Flush(true, true);
				int segmentCount = w.SegmentCount;
				int targetCount = Random().NextInt(1, segmentCount);
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: merge to " + targetCount + " segs (current count="
						 + segmentCount + ")");
				}
				w.ForceMerge(targetCount);
				AreEqual(targetCount, w.SegmentCount);
				w.Dispose();
				dir.Dispose();
			}
		}

		[Test]
		public virtual void TestForceMergeDeletesMaxSegSize()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			TieredMergePolicy tmp = new TieredMergePolicy();
			tmp.SetMaxMergedSegmentMB(0.01);
			tmp.SetForceMergeDeletesPctAllowed(0.0);
			conf.SetMergePolicy(tmp);
			IndexWriter w = new IndexWriter(dir, conf);
			int numDocs = AtLeast(200);
			for (int i = 0; i < numDocs; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				{
				    NewStringField("id", string.Empty + i, Field.Store.NO),
				    NewTextField("content", "aaa " + i, Field.Store.NO)
				};
			    w.AddDocument(doc);
			}
			w.ForceMerge(1);
			IndexReader r = w.Reader;
			AreEqual(numDocs, r.MaxDoc);
			AreEqual(numDocs, r.NumDocs);
			r.Dispose();
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("\nTEST: delete doc");
			}
			w.DeleteDocuments(new Term("id", string.Empty + (42 + 17)));
			r = w.Reader;
			AreEqual(numDocs, r.MaxDoc);
			AreEqual(numDocs - 1, r.NumDocs);
			r.Dispose();
			w.ForceMergeDeletes();
			r = w.Reader;
			AreEqual(numDocs - 1, r.MaxDoc);
			AreEqual(numDocs - 1, r.NumDocs);
			r.Dispose();
			w.Dispose();
			dir.Dispose();
		}

		private const double EPSILON = 1E-14;

		[Test]
        public virtual void TestSetters()
		{
			TieredMergePolicy tmp = new TieredMergePolicy();
			tmp.SetMaxMergedSegmentMB(0.5);
			AreEqual(0.5, tmp.MaxMergedSegmentMB, EPSILON);
			tmp.SetMaxMergedSegmentMB(double.PositiveInfinity);
			AreEqual(long.MaxValue / 1024 / 1024, tmp.MaxMergedSegmentMB, EPSILON * long.MaxValue);
			tmp.SetMaxMergedSegmentMB(long.MaxValue / 1024 / 1024);
			AreEqual(long.MaxValue / 1024 / 1024, tmp.MaxMergedSegmentMB, EPSILON * long.MaxValue);
			try
			{
				tmp.SetMaxMergedSegmentMB(-2.0);
				Fail("Didn't throw IllegalArgumentException");
			}
			catch (ArgumentException)
			{
			}
			// pass
			tmp.SetFloorSegmentMB(2.0);
			AreEqual(2.0, tmp.FloorSegmentMB, EPSILON);
			tmp.SetFloorSegmentMB(double.PositiveInfinity);
			AreEqual(long.MaxValue / 1024 / 1024, tmp.FloorSegmentMB, EPSILON * long.MaxValue);
			tmp.SetFloorSegmentMB(long.MaxValue / 1024 / 1024);
			AreEqual(long.MaxValue / 1024 / 1024, tmp.FloorSegmentMB, EPSILON * long.MaxValue);
			try
			{
				tmp.SetFloorSegmentMB(-2.0);
				Fail("Didn't throw IllegalArgumentException");
			}
			catch (ArgumentException)
			{
			}
			// pass
			tmp.SetMaxCFSSegmentSizeMB(2.0);
			AreEqual(2.0, tmp.GetMaxCFSSegmentSizeMB(), EPSILON);
			tmp.SetMaxCFSSegmentSizeMB(double.PositiveInfinity);
			AreEqual(long.MaxValue / 1024 / 1024, tmp.GetMaxCFSSegmentSizeMB
				(), EPSILON * long.MaxValue);
			tmp.SetMaxCFSSegmentSizeMB(long.MaxValue / 1024 / 1024);
			AreEqual(long.MaxValue / 1024 / 1024, tmp.GetMaxCFSSegmentSizeMB
				(), EPSILON * long.MaxValue);
			try
			{
				tmp.SetMaxCFSSegmentSizeMB(-2.0);
				Fail("Didn't throw IllegalArgumentException");
			}
			catch (ArgumentException)
			{
			}
		}

		// pass
		// TODO: Add more checks for other non-double setters!
		// LUCENE-5668
		[Test]
		public virtual void TestUnbalancedMergeSelection()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			TieredMergePolicy tmp = (TieredMergePolicy)iwc.MergePolicy;
			tmp.SetFloorSegmentMB(0.00001);
			// We need stable sizes for each segment:
			iwc.SetCodec(Codec.ForName("Lucene46"));
			iwc.SetMergeScheduler(new SerialMergeScheduler());
			iwc.SetMaxBufferedDocs(100);
			iwc.SetRAMBufferSizeMB(-1);
			IndexWriter w = new IndexWriter(dir, iwc);
			for (int i = 0; i < 100000; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				{
				    NewTextField("id", Random().Next() + string.Empty + Random().Next(), Field.Store.YES)
				};
			    w.AddDocument(doc);
			}
			IndexReader r = DirectoryReader.Open(w, true);
			// Make sure TMP always merged equal-number-of-docs segments:
			foreach (AtomicReaderContext ctx in r.Leaves)
			{
				int numDocs = ctx.Reader.NumDocs;
				AssertTrue("got numDocs=" + numDocs, numDocs == 100 || numDocs
					 == 1000 || numDocs == 10000);
			}
			r.Dispose();
			w.Dispose();
			dir.Dispose();
		}
	}
}
