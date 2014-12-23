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
	public class TestFlushByRamOrCountsPolicy : LuceneTestCase
	{
		private static LineFileDocs lineDocFile;

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.BeforeClass]
		public static void BeforeClass()
		{
			lineDocFile = new LineFileDocs(Random(), DefaultCodecSupportsDocValues());
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.AfterClass]
		public static void AfterClass()
		{
			lineDocFile.Close();
			lineDocFile = null;
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestFlushByRam()
		{
			double ramBuffer = (TEST_NIGHTLY ? 1 : 10) + AtLeast(2) + Random().NextDouble();
			RunFlushByRam(1 + Random().Next(TEST_NIGHTLY ? 5 : 1), ramBuffer, false);
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestFlushByRamLargeBuffer()
		{
			// with a 256 mb ram buffer we should never stall
			RunFlushByRam(1 + Random().Next(TEST_NIGHTLY ? 5 : 1), 256.d, true);
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="System.Exception"></exception>
		protected internal virtual void RunFlushByRam(int numThreads, double maxRamMB, bool
			 ensureNotStalled)
		{
			int numDocumentsToIndex = 10 + AtLeast(30);
			AtomicInteger numDocs = new AtomicInteger(numDocumentsToIndex);
			Directory dir = NewDirectory();
			TestFlushByRamOrCountsPolicy.MockDefaultFlushPolicy flushPolicy = new TestFlushByRamOrCountsPolicy.MockDefaultFlushPolicy
				();
			MockAnalyzer analyzer = new MockAnalyzer(Random());
			analyzer.SetMaxTokenLength(TestUtil.NextInt(Random(), 1, IndexWriter.MAX_TERM_LENGTH
				));
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
			flushPolicy = (TestFlushByRamOrCountsPolicy.MockDefaultFlushPolicy)writer.GetConfig
				().GetFlushPolicy();
			NUnit.Framework.Assert.IsFalse(flushPolicy.FlushOnDocCount());
			NUnit.Framework.Assert.IsFalse(flushPolicy.FlushOnDeleteTerms());
			NUnit.Framework.Assert.IsTrue(flushPolicy.FlushOnRAM());
			DocumentsWriter docsWriter = writer.GetDocsWriter();
			NUnit.Framework.Assert.IsNotNull(docsWriter);
			DocumentsWriterFlushControl flushControl = docsWriter.flushControl;
			NUnit.Framework.Assert.AreEqual(" bytes must be 0 after init", 0, flushControl.FlushBytes
				());
			TestFlushByRamOrCountsPolicy.IndexThread[] threads = new TestFlushByRamOrCountsPolicy.IndexThread
				[numThreads];
			for (int x = 0; x < threads.Length; x++)
			{
				threads[x] = new TestFlushByRamOrCountsPolicy.IndexThread(this, numDocs, numThreads
					, writer, lineDocFile, false);
				threads[x].Start();
			}
			for (int x_1 = 0; x_1 < threads.Length; x_1++)
			{
				threads[x_1].Join();
			}
			long maxRAMBytes = (long)(iwc.GetRAMBufferSizeMB() * 1024. * 1024.);
			NUnit.Framework.Assert.AreEqual(" all flushes must be due numThreads=" + numThreads
				, 0, flushControl.FlushBytes());
			NUnit.Framework.Assert.AreEqual(numDocumentsToIndex, writer.NumDocs());
			NUnit.Framework.Assert.AreEqual(numDocumentsToIndex, writer.MaxDoc());
			NUnit.Framework.Assert.IsTrue("peak bytes without flush exceeded watermark", flushPolicy
				.peakBytesWithoutFlush <= maxRAMBytes);
			AssertActiveBytesAfter(flushControl);
			if (flushPolicy.hasMarkedPending)
			{
				NUnit.Framework.Assert.IsTrue(maxRAMBytes < flushControl.peakActiveBytes);
			}
			if (ensureNotStalled)
			{
				NUnit.Framework.Assert.IsFalse(docsWriter.flushControl.stallControl.WasStalled());
			}
			writer.Close();
			NUnit.Framework.Assert.AreEqual(0, flushControl.ActiveBytes());
			dir.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestFlushDocCount()
		{
			int[] numThreads = new int[] { 2 + AtLeast(1), 1 };
			for (int i = 0; i < numThreads.Length; i++)
			{
				int numDocumentsToIndex = 50 + AtLeast(30);
				AtomicInteger numDocs = new AtomicInteger(numDocumentsToIndex);
				Directory dir = NewDirectory();
				TestFlushByRamOrCountsPolicy.MockDefaultFlushPolicy flushPolicy = new TestFlushByRamOrCountsPolicy.MockDefaultFlushPolicy
					();
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
				flushPolicy = (TestFlushByRamOrCountsPolicy.MockDefaultFlushPolicy)writer.GetConfig
					().GetFlushPolicy();
				NUnit.Framework.Assert.IsTrue(flushPolicy.FlushOnDocCount());
				NUnit.Framework.Assert.IsFalse(flushPolicy.FlushOnDeleteTerms());
				NUnit.Framework.Assert.IsFalse(flushPolicy.FlushOnRAM());
				DocumentsWriter docsWriter = writer.GetDocsWriter();
				NUnit.Framework.Assert.IsNotNull(docsWriter);
				DocumentsWriterFlushControl flushControl = docsWriter.flushControl;
				NUnit.Framework.Assert.AreEqual(" bytes must be 0 after init", 0, flushControl.FlushBytes
					());
				TestFlushByRamOrCountsPolicy.IndexThread[] threads = new TestFlushByRamOrCountsPolicy.IndexThread
					[numThreads[i]];
				for (int x = 0; x < threads.Length; x++)
				{
					threads[x] = new TestFlushByRamOrCountsPolicy.IndexThread(this, numDocs, numThreads
						[i], writer, lineDocFile, false);
					threads[x].Start();
				}
				for (int x_1 = 0; x_1 < threads.Length; x_1++)
				{
					threads[x_1].Join();
				}
				NUnit.Framework.Assert.AreEqual(" all flushes must be due numThreads=" + numThreads
					[i], 0, flushControl.FlushBytes());
				NUnit.Framework.Assert.AreEqual(numDocumentsToIndex, writer.NumDocs());
				NUnit.Framework.Assert.AreEqual(numDocumentsToIndex, writer.MaxDoc());
				NUnit.Framework.Assert.IsTrue("peak bytes without flush exceeded watermark", flushPolicy
					.peakDocCountWithoutFlush <= iwc.GetMaxBufferedDocs());
				AssertActiveBytesAfter(flushControl);
				writer.Close();
				NUnit.Framework.Assert.AreEqual(0, flushControl.ActiveBytes());
				dir.Close();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="System.Exception"></exception>
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
			DocumentsWriterPerThreadPool threadPool = new DocumentsWriterPerThreadPool(numDWPT
				);
			iwc.SetIndexerThreadPool(threadPool);
			IndexWriter writer = new IndexWriter(dir, iwc);
			flushPolicy = (TestFlushByRamOrCountsPolicy.MockDefaultFlushPolicy)writer.GetConfig
				().GetFlushPolicy();
			DocumentsWriter docsWriter = writer.GetDocsWriter();
			NUnit.Framework.Assert.IsNotNull(docsWriter);
			DocumentsWriterFlushControl flushControl = docsWriter.flushControl;
			NUnit.Framework.Assert.AreEqual(" bytes must be 0 after init", 0, flushControl.FlushBytes
				());
			TestFlushByRamOrCountsPolicy.IndexThread[] threads = new TestFlushByRamOrCountsPolicy.IndexThread
				[numThreads];
			for (int x = 0; x < threads.Length; x++)
			{
				threads[x] = new TestFlushByRamOrCountsPolicy.IndexThread(this, numDocs, numThreads
					, writer, lineDocFile, true);
				threads[x].Start();
			}
			for (int x_1 = 0; x_1 < threads.Length; x_1++)
			{
				threads[x_1].Join();
			}
			NUnit.Framework.Assert.AreEqual(" all flushes must be due", 0, flushControl.FlushBytes
				());
			NUnit.Framework.Assert.AreEqual(numDocumentsToIndex, writer.NumDocs());
			NUnit.Framework.Assert.AreEqual(numDocumentsToIndex, writer.MaxDoc());
			if (flushPolicy.FlushOnRAM() && !flushPolicy.FlushOnDocCount() && !flushPolicy.FlushOnDeleteTerms
				())
			{
				long maxRAMBytes = (long)(iwc.GetRAMBufferSizeMB() * 1024. * 1024.);
				NUnit.Framework.Assert.IsTrue("peak bytes without flush exceeded watermark", flushPolicy
					.peakBytesWithoutFlush <= maxRAMBytes);
				if (flushPolicy.hasMarkedPending)
				{
					NUnit.Framework.Assert.IsTrue("max: " + maxRAMBytes + " " + flushControl.peakActiveBytes
						, maxRAMBytes <= flushControl.peakActiveBytes);
				}
			}
			AssertActiveBytesAfter(flushControl);
			writer.Commit();
			NUnit.Framework.Assert.AreEqual(0, flushControl.ActiveBytes());
			IndexReader r = DirectoryReader.Open(dir);
			NUnit.Framework.Assert.AreEqual(numDocumentsToIndex, r.NumDocs());
			NUnit.Framework.Assert.AreEqual(numDocumentsToIndex, r.MaxDoc());
			if (!flushPolicy.FlushOnRAM())
			{
				NUnit.Framework.Assert.IsFalse("never stall if we don't flush on RAM", docsWriter
					.flushControl.stallControl.WasStalled());
				NUnit.Framework.Assert.IsFalse("never block if we don't flush on RAM", docsWriter
					.flushControl.stallControl.HasBlocked());
			}
			r.Close();
			writer.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestStallControl()
		{
			int[] numThreads = new int[] { 4 + Random().Next(8), 1 };
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
				TestFlushByRamOrCountsPolicy.IndexThread[] threads = new TestFlushByRamOrCountsPolicy.IndexThread
					[numThreads[i]];
				for (int x = 0; x < threads.Length; x++)
				{
					threads[x] = new TestFlushByRamOrCountsPolicy.IndexThread(this, numDocs, numThreads
						[i], writer, lineDocFile, false);
					threads[x].Start();
				}
				for (int x_1 = 0; x_1 < threads.Length; x_1++)
				{
					threads[x_1].Join();
				}
				DocumentsWriter docsWriter = writer.GetDocsWriter();
				NUnit.Framework.Assert.IsNotNull(docsWriter);
				DocumentsWriterFlushControl flushControl = docsWriter.flushControl;
				NUnit.Framework.Assert.AreEqual(" all flushes must be due", 0, flushControl.FlushBytes
					());
				NUnit.Framework.Assert.AreEqual(numDocumentsToIndex, writer.NumDocs());
				NUnit.Framework.Assert.AreEqual(numDocumentsToIndex, writer.MaxDoc());
				if (numThreads[i] == 1)
				{
					NUnit.Framework.Assert.IsFalse("single thread must not block numThreads: " + numThreads
						[i], docsWriter.flushControl.stallControl.HasBlocked());
				}
				if (docsWriter.flushControl.peakNetBytes > (2.d * iwc.GetRAMBufferSizeMB() * 1024.d
					 * 1024.d))
				{
					NUnit.Framework.Assert.IsTrue(docsWriter.flushControl.stallControl.WasStalled());
				}
				AssertActiveBytesAfter(flushControl);
				writer.Close(true);
				dir.Close();
			}
		}

		protected internal virtual void AssertActiveBytesAfter(DocumentsWriterFlushControl
			 flushControl)
		{
			Iterator<DocumentsWriterPerThreadPool.ThreadState> allActiveThreads = flushControl
				.AllActiveThreadStates();
			long bytesUsed = 0;
			while (allActiveThreads.HasNext())
			{
				DocumentsWriterPerThreadPool.ThreadState next = allActiveThreads.Next();
				if (next.dwpt != null)
				{
					bytesUsed += next.dwpt.BytesUsed();
				}
			}
			NUnit.Framework.Assert.AreEqual(bytesUsed, flushControl.ActiveBytes());
		}

		public class IndexThread : Sharpen.Thread
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
				this.iwc = writer.GetConfig();
				this.docs = docs;
				this.doRandomCommit = doRandomCommit;
			}

			public override void Run()
			{
				try
				{
					long ramSize = 0;
					while (this.pendingDocs.DecrementAndGet() > -1)
					{
						Lucene.Net.Document.Document doc = this.docs.NextDoc();
						this.writer.AddDocument(doc);
						long newRamSize = this.writer.RamSizeInBytes();
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
					Sharpen.Runtime.PrintStackTrace(ex, System.Console.Out);
					throw new RuntimeException(ex);
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
				AList<DocumentsWriterPerThreadPool.ThreadState> pending = new AList<DocumentsWriterPerThreadPool.ThreadState
					>();
				AList<DocumentsWriterPerThreadPool.ThreadState> notPending = new AList<DocumentsWriterPerThreadPool.ThreadState
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
					if (FlushOnDeleteTerms() && state.dwpt.pendingUpdates.numTermDeletes.Get() >= indexWriterConfig
						.GetMaxBufferedDeleteTerms())
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
						NUnit.Framework.Assert.IsTrue(pending.Remove(toFlush));
					}
					else
					{
						NUnit.Framework.Assert.IsTrue(notPending.Remove(toFlush));
					}
					NUnit.Framework.Assert.IsTrue(toFlush.flushPending);
					hasMarkedPending = true;
				}
				foreach (DocumentsWriterPerThreadPool.ThreadState threadState in notPending)
				{
					NUnit.Framework.Assert.IsFalse(threadState.flushPending);
				}
			}

			public override void OnInsert(DocumentsWriterFlushControl control, DocumentsWriterPerThreadPool.ThreadState
				 state)
			{
				AList<DocumentsWriterPerThreadPool.ThreadState> pending = new AList<DocumentsWriterPerThreadPool.ThreadState
					>();
				AList<DocumentsWriterPerThreadPool.ThreadState> notPending = new AList<DocumentsWriterPerThreadPool.ThreadState
					>();
				FindPending(control, pending, notPending);
				bool flushCurrent = state.flushPending;
				long activeBytes = control.ActiveBytes();
				DocumentsWriterPerThreadPool.ThreadState toFlush;
				if (state.flushPending)
				{
					toFlush = state;
				}
				else
				{
					if (FlushOnDocCount() && state.dwpt.GetNumDocsInRAM() >= indexWriterConfig.GetMaxBufferedDocs
						())
					{
						toFlush = state;
					}
					else
					{
						if (FlushOnRAM() && activeBytes >= (long)(indexWriterConfig.GetRAMBufferSizeMB() 
							* 1024. * 1024.))
						{
							toFlush = FindLargestNonPendingWriter(control, state);
							NUnit.Framework.Assert.IsFalse(toFlush.flushPending);
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
						NUnit.Framework.Assert.IsTrue(pending.Remove(toFlush));
					}
					else
					{
						NUnit.Framework.Assert.IsTrue(notPending.Remove(toFlush));
					}
					NUnit.Framework.Assert.IsTrue(toFlush.flushPending);
					hasMarkedPending = true;
				}
				else
				{
					peakBytesWithoutFlush = Math.Max(activeBytes, peakBytesWithoutFlush);
					peakDocCountWithoutFlush = Math.Max(state.dwpt.GetNumDocsInRAM(), peakDocCountWithoutFlush
						);
				}
				foreach (DocumentsWriterPerThreadPool.ThreadState threadState in notPending)
				{
					NUnit.Framework.Assert.IsFalse(threadState.flushPending);
				}
			}
		}

		internal static void FindPending(DocumentsWriterFlushControl flushControl, AList<
			DocumentsWriterPerThreadPool.ThreadState> pending, AList<DocumentsWriterPerThreadPool.ThreadState
			> notPending)
		{
			Iterator<DocumentsWriterPerThreadPool.ThreadState> allActiveThreads = flushControl
				.AllActiveThreadStates();
			while (allActiveThreads.HasNext())
			{
				DocumentsWriterPerThreadPool.ThreadState next = allActiveThreads.Next();
				if (next.flushPending)
				{
					pending.AddItem(next);
				}
				else
				{
					notPending.AddItem(next);
				}
			}
		}
	}
}
