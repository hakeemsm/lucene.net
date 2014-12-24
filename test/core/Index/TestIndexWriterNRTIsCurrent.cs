/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.IO;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	public class TestIndexWriterNRTIsCurrent : LuceneTestCase
	{
		public class ReaderHolder
		{
			internal volatile DirectoryReader reader;

			internal volatile bool stop = false;
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestIsCurrentWithThreads()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			IndexWriter writer = new IndexWriter(dir, conf);
			TestIndexWriterNRTIsCurrent.ReaderHolder holder = new TestIndexWriterNRTIsCurrent.ReaderHolder
				();
			TestIndexWriterNRTIsCurrent.ReaderThread[] threads = new TestIndexWriterNRTIsCurrent.ReaderThread
				[AtLeast(3)];
			CountDownLatch latch = new CountDownLatch(1);
			TestIndexWriterNRTIsCurrent.WriterThread writerThread = new TestIndexWriterNRTIsCurrent.WriterThread
				(holder, writer, AtLeast(500), Random(), latch);
			for (int i = 0; i < threads.Length; i++)
			{
				threads[i] = new TestIndexWriterNRTIsCurrent.ReaderThread(holder, latch);
				threads[i].Start();
			}
			writerThread.Start();
			writerThread.Join();
			bool failed = writerThread.failed != null;
			if (failed)
			{
				Sharpen.Runtime.PrintStackTrace(writerThread.failed);
			}
			for (int i_1 = 0; i_1 < threads.Length; i_1++)
			{
				threads[i_1].Join();
				if (threads[i_1].failed != null)
				{
					Sharpen.Runtime.PrintStackTrace(threads[i_1].failed);
					failed = true;
				}
			}
			IsFalse(failed);
			writer.Close();
			dir.Close();
		}

		public class WriterThread : Sharpen.Thread
		{
			private readonly TestIndexWriterNRTIsCurrent.ReaderHolder holder;

			private readonly IndexWriter writer;

			private readonly int numOps;

			private bool countdown = true;

			private readonly CountDownLatch latch;

			internal Exception failed;

			internal WriterThread(TestIndexWriterNRTIsCurrent.ReaderHolder holder, IndexWriter
				 writer, int numOps, Random random, CountDownLatch latch) : base()
			{
				this.holder = holder;
				this.writer = writer;
				this.numOps = numOps;
				this.latch = latch;
			}

			public override void Run()
			{
				DirectoryReader currentReader = null;
				Random random = LuceneTestCase.Random();
				try
				{
					Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
						();
					doc.Add(new TextField("id", "1", Field.Store.NO));
					writer.AddDocument(doc);
					holder.reader = currentReader = writer.GetReader(true);
					Term term = new Term("id");
					for (int i = 0; i < numOps && !holder.stop; i++)
					{
						float nextOp = random.NextFloat();
						if (nextOp < 0.3)
						{
							term.Set("id", new BytesRef("1"));
							writer.UpdateDocument(term, doc);
						}
						else
						{
							if (nextOp < 0.5)
							{
								writer.AddDocument(doc);
							}
							else
							{
								term.Set("id", new BytesRef("1"));
								writer.DeleteDocuments(term);
							}
						}
						if (holder.reader != currentReader)
						{
							holder.reader = currentReader;
							if (countdown)
							{
								countdown = false;
								latch.CountDown();
							}
						}
						if (random.NextBoolean())
						{
							writer.Commit();
							DirectoryReader newReader = DirectoryReader.OpenIfChanged(currentReader);
							if (newReader != null)
							{
								currentReader.DecRef();
								currentReader = newReader;
							}
							if (currentReader.NumDocs() == 0)
							{
								writer.AddDocument(doc);
							}
						}
					}
				}
				catch (Exception e)
				{
					failed = e;
				}
				finally
				{
					holder.reader = null;
					if (countdown)
					{
						latch.CountDown();
					}
					if (currentReader != null)
					{
						try
						{
							currentReader.DecRef();
						}
						catch (IOException)
						{
						}
					}
				}
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("writer stopped - forced by reader: " + holder.stop);
				}
			}
		}

		public sealed class ReaderThread : Sharpen.Thread
		{
			private readonly TestIndexWriterNRTIsCurrent.ReaderHolder holder;

			private readonly CountDownLatch latch;

			internal Exception failed;

			internal ReaderThread(TestIndexWriterNRTIsCurrent.ReaderHolder holder, CountDownLatch
				 latch) : base()
			{
				this.holder = holder;
				this.latch = latch;
			}

			public override void Run()
			{
				try
				{
					latch.Await();
				}
				catch (Exception e)
				{
					failed = e;
					return;
				}
				DirectoryReader reader;
				while ((reader = holder.reader) != null)
				{
					if (reader.TryIncRef())
					{
						try
						{
							bool current = reader.IsCurrent();
							if (VERBOSE)
							{
								System.Console.Out.WriteLine("Thread: " + Sharpen.Thread.CurrentThread() + " Reader: "
									 + reader + " isCurrent:" + current);
							}
							IsFalse(current);
						}
						catch (Exception e)
						{
							if (VERBOSE)
							{
								System.Console.Out.WriteLine("FAILED Thread: " + Sharpen.Thread.CurrentThread() +
									 " Reader: " + reader + " isCurrent: false");
							}
							failed = e;
							holder.stop = true;
							return;
						}
						finally
						{
							try
							{
								reader.DecRef();
							}
							catch (IOException e)
							{
								if (failed == null)
								{
									failed = e;
								}
								return;
							}
						}
					}
				}
			}
		}
	}
}
