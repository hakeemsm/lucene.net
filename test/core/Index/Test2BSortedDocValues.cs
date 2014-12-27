using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;

namespace Lucene.Net.Test.Index
{
	public class Test2BSortedDocValues : LuceneTestCase
	{
		// indexes Integer.MAX_VALUE docs with a fixed binary field
		/// <exception cref="System.Exception"></exception>
		public virtual void TestFixedSorted()
		{
			BaseDirectoryWrapper dir = NewFSDirectory(CreateTempDir("2BFixedSorted"));
			if (dir is MockDirectoryWrapper)
			{
				((MockDirectoryWrapper)dir).SetThrottling(MockDirectoryWrapper.Throttling.NEVER);
			}
			IndexWriter w = new IndexWriter(dir, ((IndexWriterConfig)((IndexWriterConfig)new 
				IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs
				(IndexWriterConfig.DISABLE_AUTO_FLUSH)).SetRAMBufferSizeMB(256.0)).SetMergeScheduler
				(new ConcurrentMergeScheduler()).SetMergePolicy(NewLogMergePolicy(false, 10)).SetOpenMode
				(IndexWriterConfig.OpenMode.CREATE));
			var doc = new Lucene.Net.Documents.Document();
			var bytes = new byte[2];
			BytesRef data = new BytesRef(bytes.ToSbytes());
			SortedDocValuesField dvField = new SortedDocValuesField("dv", data);
			doc.Add(dvField);
			for (int i = 0; i < int.MaxValue; i++)
			{
				bytes[0] = ((byte)(i >> 8));
				bytes[1] = ((byte)i);
				w.AddDocument(doc);
				if (i % 100000 == 0)
				{
					System.Console.Out.WriteLine("indexed: " + i);
					System.Console.Out.Flush();
				}
			}
			w.ForceMerge(1);
			w.Dispose();
			System.Console.Out.WriteLine("verifying...");
			System.Console.Out.Flush();
			DirectoryReader r = DirectoryReader.Open(dir);
			int expectedValue = 0;
			foreach (AtomicReaderContext context in r.Leaves)
			{
				AtomicReader reader = ((AtomicReader)context.Reader);
				BytesRef scratch = new BytesRef();
				BinaryDocValues dv = reader.GetSortedDocValues("dv");
				for (int i_1 = 0; i_1 < reader.MaxDoc; i_1++)
				{
					bytes[0] = unchecked((byte)(expectedValue >> 8));
					bytes[1] = unchecked((byte)expectedValue);
					dv.Get(i_1, scratch);
					AreEqual(data, scratch);
					expectedValue++;
				}
			}
			r.Dispose();
			dir.Dispose();
		}

		// indexes Integer.MAX_VALUE docs with a fixed binary field
		/// <exception cref="System.Exception"></exception>
		public virtual void Test2BOrds()
		{
			BaseDirectoryWrapper dir = NewFSDirectory(CreateTempDir("2BOrds"));
			if (dir is MockDirectoryWrapper)
			{
				((MockDirectoryWrapper)dir).SetThrottling(MockDirectoryWrapper.Throttling.NEVER);
			}
			var w = new IndexWriter(dir, ((IndexWriterConfig)((IndexWriterConfig)new 
				IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs
				(IndexWriterConfig.DISABLE_AUTO_FLUSH)).SetRAMBufferSizeMB(256.0)).SetMergeScheduler
				(new ConcurrentMergeScheduler()).SetMergePolicy(NewLogMergePolicy(false, 10)).SetOpenMode
				(IndexWriterConfig.OpenMode.CREATE));
			var doc = new Lucene.Net.Documents.Document();
			byte[] bytes = new byte[4];
			BytesRef data = new BytesRef(bytes.ToSbytes());
			SortedDocValuesField dvField = new SortedDocValuesField("dv", data);
			doc.Add(dvField);
			for (int i = 0; i < int.MaxValue; i++)
			{
				bytes[0] = ((byte)(i >> 24));
				bytes[1] = ((byte)(i >> 16));
				bytes[2] = ((byte)(i >> 8));
				bytes[3] = ((byte)i);
				w.AddDocument(doc);
				if (i % 100000 == 0)
				{
					System.Console.Out.WriteLine("indexed: " + i);
					System.Console.Out.Flush();
				}
			}
			w.ForceMerge(1);
			w.Dispose();
			System.Console.Out.WriteLine("verifying...");
			System.Console.Out.Flush();
			DirectoryReader r = DirectoryReader.Open(dir);
			int counter = 0;
			foreach (AtomicReaderContext context in r.Leaves)
			{
				AtomicReader reader = ((AtomicReader)context.Reader);
				BytesRef scratch = new BytesRef();
				BinaryDocValues dv = reader.GetSortedDocValues("dv");
				for (int i_1 = 0; i_1 < reader.MaxDoc; i_1++)
				{
					bytes[0] = unchecked((byte)(counter >> 24));
					bytes[1] = unchecked((byte)(counter >> 16));
					bytes[2] = unchecked((byte)(counter >> 8));
					bytes[3] = unchecked((byte)counter);
					counter++;
					dv.Get(i_1, scratch);
					AreEqual(data, scratch);
				}
			}
			r.Dispose();
			dir.Dispose();
		}
		// TODO: variable
	}
}
