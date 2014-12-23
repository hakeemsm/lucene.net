/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
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

		/// <exception cref="System.Exception"></exception>
		public virtual void Test()
		{
			Directory d = NewDirectory();
			MockAnalyzer analyzer = new MockAnalyzer(Random());
			analyzer.SetMaxTokenLength(TestUtil.NextInt(Random(), 1, IndexWriter.MAX_TERM_LENGTH
				));
			TestForceMergeForever.MyIndexWriter w = new TestForceMergeForever.MyIndexWriter(d
				, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer));
			// Try to make an index that requires merging:
			w.GetConfig().SetMaxBufferedDocs(TestUtil.NextInt(Random(), 2, 11));
			int numStartDocs = AtLeast(20);
			LineFileDocs docs = new LineFileDocs(Random(), DefaultCodecSupportsDocValues());
			for (int docIDX = 0; docIDX < numStartDocs; docIDX++)
			{
				w.AddDocument(docs.NextDoc());
			}
			MergePolicy mp = w.GetConfig().GetMergePolicy();
			int mergeAtOnce = 1 + w.segmentInfos.Size();
			if (mp is TieredMergePolicy)
			{
				((TieredMergePolicy)mp).SetMaxMergeAtOnce(mergeAtOnce);
			}
			else
			{
				if (mp is LogMergePolicy)
				{
					((LogMergePolicy)mp).SetMergeFactor(mergeAtOnce);
				}
				else
				{
					// skip test
					w.Close();
					d.Close();
					return;
				}
			}
			AtomicBoolean doStop = new AtomicBoolean();
			w.GetConfig().SetMaxBufferedDocs(2);
			Sharpen.Thread t = new _Thread_84(doStop, w, numStartDocs, docs);
			// Force deletes to apply
			t.Start();
			w.ForceMerge(1);
			doStop.Set(true);
			t.Join();
			NUnit.Framework.Assert.IsTrue("merge count is " + w.mergeCount.Get(), w.mergeCount
				.Get() <= 1);
			w.Close();
			d.Close();
			docs.Close();
		}

		private sealed class _Thread_84 : Sharpen.Thread
		{
			public _Thread_84(AtomicBoolean doStop, TestForceMergeForever.MyIndexWriter w, int
				 numStartDocs, LineFileDocs docs)
			{
				this.doStop = doStop;
				this.w = w;
				this.numStartDocs = numStartDocs;
				this.docs = docs;
			}

			public override void Run()
			{
				try
				{
					while (!doStop.Get())
					{
						w.UpdateDocument(new Term("docid", string.Empty + LuceneTestCase.Random().Next(numStartDocs
							)), docs.NextDoc());
						w.GetReader().Close();
					}
				}
				catch (Exception t)
				{
					throw new RuntimeException(t);
				}
			}

			private readonly AtomicBoolean doStop;

			private readonly TestForceMergeForever.MyIndexWriter w;

			private readonly int numStartDocs;

			private readonly LineFileDocs docs;
		}
	}
}
