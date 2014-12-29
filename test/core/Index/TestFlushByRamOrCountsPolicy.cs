using System;
using System.Collections.Generic;
using System.Threading;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.TestFramework;
using Lucene.Net.TestFramework.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Index
{
    [TestFixture]
	public class TestFlushByRamOrCountsPolicy : LuceneTestCase
	{
		private static LineFileDocs lineDocFile;

		/// <exception cref="System.Exception"></exception>
		[SetUp]
		public void Setup()
		{
			lineDocFile = new LineFileDocs(Random(), DefaultCodecSupportsDocValues());
		}

		/// <exception cref="System.Exception"></exception>
		[TearDown]
		public static void TearDown()
		{
			lineDocFile.Close();
			lineDocFile = null;
		}

		/// <exception cref="System.IO.IOException"></exception>
		[Test]
		public virtual void TestFlushByRam()
		{
			double ramBuffer = (TEST_NIGHTLY ? 1 : 10) + AtLeast(2) + Random().NextDouble();
			RunFlushByRam(1 + Random().Next(TEST_NIGHTLY ? 5 : 1), ramBuffer, false);
		}

		/// <exception cref="System.IO.IOException"></exception>
		[Test]
		public virtual void TestFlushByRamLargeBuffer()
		{
			// with a 256 mb ram buffer we should never stall
			RunFlushByRam(1 + Random().Next(TEST_NIGHTLY ? 5 : 1), 256, true);
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="System.Exception"></exception>
		protected internal virtual void RunFlushByRam(int numThreads, double maxRamMB, bool
			 ensureNotStalled)
		{
			int numDocumentsToIndex = 10 + AtLeast(30);
			AtomicInteger numDocs = new AtomicInteger(numDocumentsToIndex);
			Directory dir = NewDirectory();
			var flushPolicy = new MockDefaultFlushPolicy();
			MockAnalyzer analyzer = new MockAnalyzer(Random());
			analyzer.SetMaxTokenLength(Random().NextInt(1, IndexWriter.MAX_TERM_LENGTH));
			IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).SetFlushPolicy
				(flushPolicy);
			int numDWPT = 1 + AtLeast(2);
			DocumentsWriterPerThreadPool threadPool = new DocumentsWriterPerThreadPool(numDWPT
				);
			iwc.SetIndexerThreadPool(threadPool);
			iwc.SetRAMBufferSizeMB(maxRamMB);
			iwc.SetMaxBufferedDocs(IndexWriterConfig.DISABLE_AUTO_FLUSH);
			iwc.SetMaxBufferedDeleteTerms(IndexWriterConfig.DISABLE_AUTO_FLUSH);
			IndexWriter writer = new IndexWriter(dir, iwc);
			flushPolicy = (MockDefaultFlushPolicy)writer.Config.FlushPolicy;
			AssertFalse(flushPolicy.FlushOnDocCount);
			AssertFalse(flushPolicy.FlushOnDeleteTerms);
			AssertTrue(flushPolicy.FlushOnRAM);
			DocumentsWriter docsWriter = writer.DocsWriter;
			IsNotNull(docsWriter);
			DocumentsWriterFlushControl flushControl = docsWriter.flushControl;
			AssertEquals(" bytes must be 0 after init", 0, flushControl.FlushBytes);
			var threads = new Thread[numThreads];
			for (int x = 0; x < threads.Length; x++)
			{
			    threads[x] = new Thread(new IndexThread(this, numDocs, numThreads, writer, lineDocFile, false).Run);
				threads[x].Start();
			}
			for (int x_1 = 0; x_1 < threads.Length; x_1++)
			{
				threads[x_1].Join();
			}
			long maxRAMBytes = (long)(iwc.RAMBufferSizeMB * 1024 * 1024);
			AssertEquals(" all flushes must be due numThreads=" + numThreads
				, 0, flushControl.FlushBytes);
			AssertEquals(numDocumentsToIndex, writer.NumDocs);
			AssertEquals(numDocumentsToIndex, writer.MaxDoc);
			AssertTrue("peak bytes without flush exceeded watermark", flushPolicy
				.peakBytesWithoutFlush <= maxRAMBytes);
			AssertActiveBytesAfter(flushControl);
			if (flushPolicy.hasMarkedPending)
			{
				AssertTrue(maxRAMBytes < flushControl.peakActiveBytes);
			}
			if (ensureNotStalled)
			{
				AssertFalse(docsWriter.flushControl.stallControl.WasStalled);
			}
			writer.Dispose();
			AssertEquals(0, flushControl.ActiveBytes);
			dir.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		[Test]
		public virtual void TestFlushDocCount()
		{
			int[] numThreads = new int[] { 2 + AtLeast(1), 1 };
			for (int i = 0; i < numThreads.Length; i++)
			{
				int numDocumentsToIndex = 50 + AtLeast(30);
				AtomicInteger numDocs = new AtomicInteger(numDocumentsToIndex);
				Directory dir = NewDirectory();
				var flushPolicy = new MockDefaultFlushPolicy();
				IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
					(Random())).SetFlushPolicy(flushPolicy);
				int numDWPT = 1 + AtLeast(2);
				DocumentsWriterPerThreadPool threadPool = new DocumentsWriterPerThreadPool(numDWPT
					);
				iwc.SetIndexerThreadPool(threadPool);
				iwc.SetMaxBufferedDocs(2 + AtLeast(10));
				iwc.SetRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH);
				iwc.SetMaxBufferedDeleteTerms(IndexWriterConfig.DISABLE_AUTO_FLUSH);
				IndexWriter writer = new IndexWriter(dir, iwc);
				flushPolicy = (TestFlushByRamOrCountsPolicy.MockDefaultFlushPolicy)writer.Config.FlushPolicy;
				AssertTrue(flushPolicy.FlushOnDocCount);
				AssertFalse(flushPolicy.FlushOnDeleteTerms);
				AssertFalse(flushPolicy.FlushOnRAM);
				DocumentsWriter docsWriter = writer.DocsWriter;
				IsNotNull(docsWriter);
				DocumentsWriterFlushControl flushControl = docsWriter.flushControl;
				AssertEquals(" bytes must be 0 after init", 0, flushControl.FlushBytes);
				var threads = new Thread[numThreads[i]];
				for (int x = 0; x < threads.Length; x++)
				{
					threads[x] = new Thread(new IndexThread(this, numDocs, numThreads
						[i], writer, lineDocFile, false).Run);
					threads[x].Start();
				}
				for (int x_1 = 0; x_1 < threads.Length; x_1++)
				{
					threads[x_1].Join();
				}
				AssertEquals(" all flushes must be due numThreads=" + numThreads
					[i], 0, flushControl.FlushBytes);
				AssertEquals(numDocumentsToIndex, writer.NumDocs);
				AssertEquals(numDocumentsToIndex, writer.MaxDoc);
				AssertTrue("peak bytes without flush exceeded watermark", flushPolicy
					.peakDocCountWithoutFlush <= iwc.MaxBufferedDocs);
				AssertActiveBytesAfter(flushControl);
				writer.Dispose();
				AssertEquals(0, flushControl.ActiveBytes);
				dir.Dispose();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		[Test]
		public virtual void TestRandom()
		{
			int numThreads = 1 + Random().Next(8);
			int numDocumentsToIndex = 50 + AtLeast(70);
			AtomicInteger numDocs = new AtomicInteger(numDocumentsToIndex);
			Directory dir = NewDirectory();
			IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			TestFlushByRamOrCountsPolicy.MockDefaultFlushPolicy flushPolicy = new TestFlushByRamOrCountsPolicy.MockDefaultFlushPolicy
				();
			iwc.SetFlushPolicy(flushPolicy);
			int numDWPT = 1 + Random().Next(8);
			var threadPool = new DocumentsWriterPerThreadPool(numDWPT);
			iwc.SetIndexerThreadPool(threadPool);
			IndexWriter writer = new IndexWriter(dir, iwc);
			flushPolicy = (TestFlushByRamOrCountsPolicy.MockDefaultFlushPolicy)writer.Config.FlushPolicy;
			DocumentsWriter docsWriter = writer.DocsWriter;
			IsNotNull(docsWriter);
			DocumentsWriterFlushControl flushControl = docsWriter.flushControl;
			AssertEquals(" bytes must be 0 after init", 0, flushControl.FlushBytes);
			var threads = new Thread[numThreads];
			for (int x = 0; x < threads.Length; x++)
			{
				threads[x] = new Thread(new IndexThread(this, numDocs, numThreads
					, writer, lineDocFile, true).Run);
				threads[x].Start();
			}
			for (int x_1 = 0; x_1 < threads.Length; x_1++)
			{
				threads[x_1].Join();
			}
			AssertEquals(" all flushes must be due", 0, flushControl.FlushBytes);
			AssertEquals(numDocumentsToIndex, writer.NumDocs);
			AssertEquals(numDocumentsToIndex, writer.MaxDoc);
			if (flushPolicy.FlushOnRAM && !flushPolicy.FlushOnDocCount && !flushPolicy.FlushOnDeleteTerms)
			{
				long maxRAMBytes = (long)(iwc.RAMBufferSizeMB * 1024 * 1024);
				AssertTrue("peak bytes without flush exceeded watermark", flushPolicy
					.peakBytesWithoutFlush <= maxRAMBytes);
				if (flushPolicy.hasMarkedPending)
				{
					AssertTrue("max: " + maxRAMBytes + " " + flushControl.peakActiveBytes
						, maxRAMBytes <= flushControl.peakActiveBytes);
				}
			}
			AssertActiveBytesAfter(flushControl);
			writer.Commit();
			AssertEquals(0, flushControl.ActiveBytes);
			IndexReader r = DirectoryReader.Open(dir);
			AssertEquals(numDocumentsToIndex, r.NumDocs);
			AssertEquals(numDocumentsToIndex, r.MaxDoc);
			if (!flushPolicy.FlushOnRAM)
			{
				AssertFalse("never stall if we don't flush on RAM", docsWriter
					.flushControl.stallControl.WasStalled);
				AssertFalse("never block if we don't flush on RAM", docsWriter
					.flushControl.stallControl.HasBlocked);
			}
			r.Dispose();
			writer.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		[Test]
		public virtual void TestStallControl()
		{
			int[] numThreads = { 4 + Random().Next(8), 1 };
			int numDocumentsToIndex = 50 + Random().Next(50);
			for (int i = 0; i < numThreads.Length; i++)
			{
				AtomicInteger numDocs = new AtomicInteger(numDocumentsToIndex);
				MockDirectoryWrapper dir = NewMockDirectory();
				// mock a very slow harddisk sometimes here so that flushing is very slow
				dir.SetThrottling(MockDirectoryWrapper.Throttling.SOMETIMES);
				IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
					(Random()));
				iwc.SetMaxBufferedDocs(IndexWriterConfig.DISABLE_AUTO_FLUSH);
				iwc.SetMaxBufferedDeleteTerms(IndexWriterConfig.DISABLE_AUTO_FLUSH);
				FlushPolicy flushPolicy = new FlushByRamOrCountsPolicy();
				iwc.SetFlushPolicy(flushPolicy);
				DocumentsWriterPerThreadPool threadPool = new DocumentsWriterPerThreadPool(numThreads
					[i] == 1 ? 1 : 2);
				iwc.SetIndexerThreadPool(threadPool);
				// with such a small ram buffer we should be stalled quiet quickly
				iwc.SetRAMBufferSizeMB(0.25);
				IndexWriter writer = new IndexWriter(dir, iwc);
				var threads = new Thread[numThreads[i]];
				for (int x = 0; x < threads.Length; x++)
				{
				    threads[x] = new Thread(new IndexThread(this, numDocs, numThreads
				        [i], writer, lineDocFile, false).Run);
					threads[x].Start();
				}
				for (int x_1 = 0; x_1 < threads.Length; x_1++)
				{
					threads[x_1].Join();
				}
				DocumentsWriter docsWriter = writer.DocsWriter;
				IsNotNull(docsWriter);
				DocumentsWriterFlushControl flushControl = docsWriter.flushControl;
				AssertEquals(" all flushes must be due", 0, flushControl.FlushBytes);
				AssertEquals(numDocumentsToIndex, writer.NumDocs);
				AssertEquals(numDocumentsToIndex, writer.MaxDoc);
				if (numThreads[i] == 1)
				{
					AssertFalse("single thread must not block numThreads: " + numThreads
						[i], docsWriter.flushControl.stallControl.HasBlocked);
				}
				if (docsWriter.flushControl.peakNetBytes > (2 * iwc.RAMBufferSizeMB * 1024
					 * 1024))
				{
					AssertTrue(docsWriter.flushControl.stallControl.WasStalled);
				}
				AssertActiveBytesAfter(flushControl);
				writer.Dispose(true);
				dir.Dispose();
			}
		}

		protected internal virtual void AssertActiveBytesAfter(DocumentsWriterFlushControl
			 flushControl)
		{
			IEnumerator<DocumentsWriterPerThreadPool.ThreadState> allActiveThreads = flushControl
				.AllActiveThreadStates;
			long bytesUsed = 0;
			while (allActiveThreads.MoveNext())
			{
				DocumentsWriterPerThreadPool.ThreadState next = allActiveThreads.Current;
				if (next.dwpt != null)
				{
					bytesUsed += next.dwpt.BytesUsed;
				}
			}
			AssertEquals(bytesUsed, flushControl.ActiveBytes);
		}

		public class IndexThread
		{
			internal IndexWriter writer;

			internal LiveIndexWriterConfig iwc;

			internal LineFileDocs docs;

			private AtomicInteger pendingDocs;

			private readonly bool doRandomCommit;

			public IndexThread(TestFlushByRamOrCountsPolicy _enclosing, AtomicInteger pendingDocs
				, int numThreads, IndexWriter writer, LineFileDocs docs, bool doRandomCommit)
			{
				this._enclosing = _enclosing;
				this.pendingDocs = pendingDocs;
				this.writer = writer;
				this.iwc = writer.Config;
				this.docs = docs;
				this.doRandomCommit = doRandomCommit;
			}

			public void Run()
			{
				try
				{
					long ramSize = 0;
					while (this.pendingDocs.DecrementAndGet() > -1)
					{
						Lucene.Net.Documents.Document doc = this.docs.NextDoc();
						this.writer.AddDocument(doc);
						long newRamSize = this.writer.RamSizeInBytes;
						if (newRamSize != ramSize)
						{
							ramSize = newRamSize;
						}
						if (this.doRandomCommit)
						{
							if (LuceneTestCase.Rarely())
							{
								this.writer.Commit();
							}
						}
					}
					this.writer.Commit();
				}
				catch (Exception ex)
				{
					System.Console.Out.WriteLine("FAILED exc:");
                    ex.printStackTrace();
					
					throw new SystemException(ex.Message,ex);
				}
			}

			private readonly TestFlushByRamOrCountsPolicy _enclosing;
		}

		private class MockDefaultFlushPolicy : FlushByRamOrCountsPolicy
		{
			internal long peakBytesWithoutFlush = int.MinValue;

			internal long peakDocCountWithoutFlush = int.MinValue;

			internal bool hasMarkedPending = false;

			public override void OnDelete(DocumentsWriterFlushControl control, DocumentsWriterPerThreadPool.ThreadState
				 state)
			{
				List<DocumentsWriterPerThreadPool.ThreadState> pending = new List<DocumentsWriterPerThreadPool.ThreadState
					>();
				List<DocumentsWriterPerThreadPool.ThreadState> notPending = new List<DocumentsWriterPerThreadPool.ThreadState
					>();
				FindPending(control, pending, notPending);
				bool flushCurrent = state.flushPending;
				DocumentsWriterPerThreadPool.ThreadState toFlush;
				if (state.flushPending)
				{
					toFlush = state;
				}
				else
				{
					if (FlushOnDeleteTerms && state.dwpt.pendingUpdates.numTermDeletes.Get() >= indexWriterConfig
						.MaxBufferedDeleteTerms)
					{
						toFlush = state;
					}
					else
					{
						toFlush = null;
					}
				}
				base.OnDelete(control, state);
				if (toFlush != null)
				{
					if (flushCurrent)
					{
						AssertTrue(pending.Remove(toFlush));
					}
					else
					{
						AssertTrue(notPending.Remove(toFlush));
					}
					AssertTrue(toFlush.flushPending);
					hasMarkedPending = true;
				}
				foreach (DocumentsWriterPerThreadPool.ThreadState threadState in notPending)
				{
					AssertFalse(threadState.flushPending);
				}
			}

			public override void OnInsert(DocumentsWriterFlushControl control, DocumentsWriterPerThreadPool.ThreadState
				 state)
			{
				List<DocumentsWriterPerThreadPool.ThreadState> pending = new List<DocumentsWriterPerThreadPool.ThreadState
					>();
				List<DocumentsWriterPerThreadPool.ThreadState> notPending = new List<DocumentsWriterPerThreadPool.ThreadState
					>();
				FindPending(control, pending, notPending);
				bool flushCurrent = state.flushPending;
				long activeBytes = control.ActiveBytes;
				DocumentsWriterPerThreadPool.ThreadState toFlush;
				if (state.flushPending)
				{
					toFlush = state;
				}
				else
				{
					if (FlushOnDocCount && state.dwpt.NumDocsInRAM >= indexWriterConfig.MaxBufferedDocs)
					{
						toFlush = state;
					}
					else
					{
						if (FlushOnRAM && activeBytes >= (long)(indexWriterConfig.RAMBufferSizeMB 
							* 1024 * 1024))
						{
							toFlush = FindLargestNonPendingWriter(control, state);
							AssertFalse(toFlush.flushPending);
						}
						else
						{
							toFlush = null;
						}
					}
				}
				base.OnInsert(control, state);
				if (toFlush != null)
				{
					if (flushCurrent)
					{
						AssertTrue(pending.Remove(toFlush));
					}
					else
					{
						AssertTrue(notPending.Remove(toFlush));
					}
					AssertTrue(toFlush.flushPending);
					hasMarkedPending = true;
				}
				else
				{
					peakBytesWithoutFlush = Math.Max(activeBytes, peakBytesWithoutFlush);
					peakDocCountWithoutFlush = Math.Max(state.dwpt.NumDocsInRAM, peakDocCountWithoutFlush
						);
				}
				foreach (DocumentsWriterPerThreadPool.ThreadState threadState in notPending)
				{
					AssertFalse(threadState.flushPending);
				}
			}
		}

		internal static void FindPending(DocumentsWriterFlushControl flushControl, List<
			DocumentsWriterPerThreadPool.ThreadState> pending, List<DocumentsWriterPerThreadPool.ThreadState
			> notPending)
		{
			IEnumerator<DocumentsWriterPerThreadPool.ThreadState> allActiveThreads = flushControl.AllActiveThreadStates;
			while (allActiveThreads.MoveNext())
			{
				DocumentsWriterPerThreadPool.ThreadState next = allActiveThreads.Current;
				if (next.flushPending)
				{
					pending.Add(next);
				}
				else
				{
					notPending.Add(next);
				}
			}
		}
	}
}
