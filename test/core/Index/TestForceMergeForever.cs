using System;
using System.Threading;
using Lucene.Net.Analysis;
using Lucene.Net.Support;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.TestFramework;
using Lucene.Net.TestFramework.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Index
{
    [TestFixture]
	public class TestForceMergeForever : LuceneTestCase
	{
		private class MyIndexWriter : IndexWriter
		{
			internal AtomicInteger mergeCount = new AtomicInteger();

			private bool first;

			/// <exception cref="System.Exception"></exception>
			public MyIndexWriter(Directory dir, IndexWriterConfig conf) : base(dir, conf)
			{
			}

			// Just counts how many merges are done
			/// <exception cref="System.IO.IOException"></exception>
			public override void Merge(MergePolicy.OneMerge merge)
			{
				if (merge.maxNumSegments != -1 && (first || merge.segments.Count == 1))
				{
					first = false;
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("TEST: maxNumSegments merge");
					}
					mergeCount.IncrementAndGet();
				}
				base.Merge(merge);
			}
		}

		[Test]
		public virtual void TestForceMerge1()
		{
			Directory d = NewDirectory();
			MockAnalyzer analyzer = new MockAnalyzer(Random());
			analyzer.SetMaxTokenLength(Random().NextInt(1, IndexWriter.MAX_TERM_LENGTH));
			var w = new MyIndexWriter(d
				, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer));
			// Try to make an index that requires merging:
			w.Config.SetMaxBufferedDocs(TestUtil.NextInt(Random(), 2, 11));
			int numStartDocs = AtLeast(20);
			LineFileDocs docs = new LineFileDocs(Random(), DefaultCodecSupportsDocValues());
			for (int docIDX = 0; docIDX < numStartDocs; docIDX++)
			{
				w.AddDocument(docs.NextDoc());
			}
			MergePolicy mp = w.Config.MergePolicy;
			int mergeAtOnce = 1 + w.segmentInfos.Count;
			if (mp is TieredMergePolicy)
			{
				((TieredMergePolicy)mp).SetMaxMergeAtOnce(mergeAtOnce);
			}
			else
			{
				if (mp is LogMergePolicy)
				{
					((LogMergePolicy)mp).MergeFactor = (mergeAtOnce);
				}
				else
				{
					// skip test
					w.Dispose();
					d.Dispose();
					return;
				}
			}
			AtomicBoolean doStop = new AtomicBoolean();
			w.Config.SetMaxBufferedDocs(2);
		    Thread t = new Thread(new ForceMergeThread(doStop, w, numStartDocs, docs).Run);
			// Force deletes to apply
			t.Start();
			w.ForceMerge(1);
			doStop.Set(true);
			t.Join();
			AssertTrue("merge count is " + w.mergeCount.Get(), w.mergeCount
				.Get() <= 1);
			w.Dispose();
			d.Dispose();
			docs.Close();
		}

		private sealed class ForceMergeThread
		{
			public ForceMergeThread(AtomicBoolean doStop, TestForceMergeForever.MyIndexWriter w, int
				 numStartDocs, LineFileDocs docs)
			{
				this.doStop = doStop;
				this.w = w;
				this.numStartDocs = numStartDocs;
				this.docs = docs;
			}

			public void Run()
			{
				try
				{
					while (!doStop.Get())
					{
						w.UpdateDocument(new Term("docid", string.Empty + LuceneTestCase.Random().Next(numStartDocs
							)), docs.NextDoc());
						w.Reader.Dispose();
					}
				}
				catch (Exception t)
				{
					throw new SystemException(t.Message,t);
				}
			}

			private readonly AtomicBoolean doStop;

			private readonly TestForceMergeForever.MyIndexWriter w;

			private readonly int numStartDocs;

			private readonly LineFileDocs docs;
		}
	}
}
