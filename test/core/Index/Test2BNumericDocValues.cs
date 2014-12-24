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
	public class Test2BNumericDocValues : LuceneTestCase
	{
		// indexes Integer.MAX_VALUE docs with an increasing dv field
		/// <exception cref="System.Exception"></exception>
		public virtual void TestNumerics()
		{
			BaseDirectoryWrapper dir = NewFSDirectory(CreateTempDir("2BNumerics"));
			if (dir is MockDirectoryWrapper)
			{
				((MockDirectoryWrapper)dir).SetThrottling(MockDirectoryWrapper.Throttling.NEVER);
			}
			IndexWriter w = new IndexWriter(dir, ((IndexWriterConfig)((IndexWriterConfig)new 
				IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs
				(IndexWriterConfig.DISABLE_AUTO_FLUSH)).SetRAMBufferSizeMB(256.0)).SetMergeScheduler
				(new ConcurrentMergeScheduler()).SetMergePolicy(NewLogMergePolicy(false, 10)).SetOpenMode
				(IndexWriterConfig.OpenMode.CREATE));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			NumericDocValuesField dvField = new NumericDocValuesField("dv", 0);
			doc.Add(dvField);
			for (int i = 0; i < int.MaxValue; i++)
			{
				dvField.SetLongValue(i);
				w.AddDocument(doc);
				if (i % 100000 == 0)
				{
					System.Console.Out.WriteLine("indexed: " + i);
					System.Console.Out.Flush();
				}
			}
			w.ForceMerge(1);
			w.Close();
			System.Console.Out.WriteLine("verifying...");
			System.Console.Out.Flush();
			DirectoryReader r = DirectoryReader.Open(dir);
			long expectedValue = 0;
			foreach (AtomicReaderContext context in r.Leaves())
			{
				AtomicReader reader = ((AtomicReader)context.Reader());
				NumericDocValues dv = reader.GetNumericDocValues("dv");
				for (int i_1 = 0; i_1 < reader.MaxDoc; i_1++)
				{
					AreEqual(expectedValue, dv.Get(i_1));
					expectedValue++;
				}
			}
			r.Close();
			dir.Close();
		}
	}
}
