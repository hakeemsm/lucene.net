/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using System.IO;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Test.Index
{
	public class TestIndexWriterOutOfFileDescriptors : LuceneTestCase
	{
		/// <exception cref="System.Exception"></exception>
		public virtual void Test()
		{
			MockDirectoryWrapper dir = NewMockFSDirectory(CreateTempDir("TestIndexWriterOutOfFileDescriptors"
				));
			dir.SetPreventDoubleWrite(false);
			double rate = Random().NextDouble() * 0.01;
			//System.out.println("rate=" + rate);
			dir.SetRandomIOExceptionRateOnOpen(rate);
			int iters = AtLeast(20);
			LineFileDocs docs = new LineFileDocs(Random(), DefaultCodecSupportsDocValues());
			IndexReader r = null;
			DirectoryReader r2 = null;
			bool any = false;
			MockDirectoryWrapper dirCopy = null;
			int lastNumDocs = 0;
			for (int iter = 0; iter < iters; iter++)
			{
				IndexWriter w = null;
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: iter=" + iter);
				}
				try
				{
					MockAnalyzer analyzer = new MockAnalyzer(Random());
					analyzer.SetMaxTokenLength(TestUtil.NextInt(Random(), 1, IndexWriter.MAX_TERM_LENGTH
						));
					IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
					if (VERBOSE)
					{
						// Do this ourselves instead of relying on LTC so
						// we see incrementing messageID:
						iwc.SetInfoStream(new PrintStreamInfoStream(System.Console.Out));
					}
					MergeScheduler ms = iwc.GetMergeScheduler();
					if (ms is ConcurrentMergeScheduler)
					{
						((ConcurrentMergeScheduler)ms).SetSuppressExceptions();
					}
					w = new IndexWriter(dir, iwc);
					if (r != null && Random().Next(5) == 3)
					{
						if (Random().NextBoolean())
						{
							if (VERBOSE)
							{
								System.Console.Out.WriteLine("TEST: addIndexes IR[]");
							}
							w.AddIndexes(new IndexReader[] { r });
						}
						else
						{
							if (VERBOSE)
							{
								System.Console.Out.WriteLine("TEST: addIndexes Directory[]");
							}
							w.AddIndexes(new Directory[] { dirCopy });
						}
					}
					else
					{
						if (VERBOSE)
						{
							System.Console.Out.WriteLine("TEST: addDocument");
						}
						w.AddDocument(docs.NextDoc());
					}
					dir.SetRandomIOExceptionRateOnOpen(0.0);
					w.Dispose();
					w = null;
					// NOTE: This is O(N^2)!  Only enable for temporary debugging:
					//dir.setRandomIOExceptionRateOnOpen(0.0);
					//TestUtil.checkIndex(dir);
					//dir.setRandomIOExceptionRateOnOpen(rate);
					// Verify numDocs only increases, to catch IndexWriter
					// accidentally deleting the index:
					dir.SetRandomIOExceptionRateOnOpen(0.0);
					IsTrue(DirectoryReader.IndexExists(dir));
					if (r2 == null)
					{
						r2 = DirectoryReader.Open(dir);
					}
					else
					{
						DirectoryReader r3 = DirectoryReader.OpenIfChanged(r2);
						if (r3 != null)
						{
							r2.Dispose();
							r2 = r3;
						}
					}
					IsTrue("before=" + lastNumDocs + " after=" + r2.NumDocs, 
						r2.NumDocs >= lastNumDocs);
					lastNumDocs = r2.NumDocs;
					//System.out.println("numDocs=" + lastNumDocs);
					dir.SetRandomIOExceptionRateOnOpen(rate);
					any = true;
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("TEST: iter=" + iter + ": success");
					}
				}
				catch (IOException ioe)
				{
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("TEST: iter=" + iter + ": exception");
						Sharpen.Runtime.PrintStackTrace(ioe);
					}
					if (w != null)
					{
						// NOTE: leave random IO exceptions enabled here,
						// to verify that rollback does not try to write
						// anything:
						w.Rollback();
					}
				}
				if (any && r == null && Random().NextBoolean())
				{
					// Make a copy of a non-empty index so we can use
					// it to addIndexes later:
					dir.SetRandomIOExceptionRateOnOpen(0.0);
					r = DirectoryReader.Open(dir);
					dirCopy = NewMockFSDirectory(CreateTempDir("TestIndexWriterOutOfFileDescriptors.copy"
						));
					ICollection<string> files = new HashSet<string>();
					foreach (string file in dir.ListAll())
					{
						dir.Copy(dirCopy, file, file, IOContext.DEFAULT);
						files.Add(file);
					}
					dirCopy.Sync(files);
					// Have IW kiss the dir so we remove any leftover
					// files ... we can easily have leftover files at
					// the time we take a copy because we are holding
					// open a reader:
					new IndexWriter(dirCopy, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
						(Random()))).Dispose();
					dirCopy.SetRandomIOExceptionRate(rate);
					dir.SetRandomIOExceptionRateOnOpen(rate);
				}
			}
			if (r2 != null)
			{
				r2.Dispose();
			}
			if (r != null)
			{
				r.Dispose();
				dirCopy.Dispose();
			}
			dir.Dispose();
		}
	}
}
