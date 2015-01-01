using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.TestFramework;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Index
{
    [TestFixture]
	public class Test2BBinaryDocValues : LuceneTestCase
	{
		// indexes Integer.MAX_VALUE docs with a fixed binary field
		[Test]
		public virtual void TestFixedBinary()
		{
			BaseDirectoryWrapper dir = NewFSDirectory(CreateTempDir("2BFixedBinary"));
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
			var bytes = new sbyte[4];
			BytesRef data = new BytesRef(bytes);
			BinaryDocValuesField dvField = new BinaryDocValuesField("dv", data);
			doc.Add(dvField);
			for (int i = 0; i < int.MaxValue; i++)
			{
				bytes[0] = (sbyte) (i >> 24);
				bytes[1] = (sbyte)(i >> 16);
				bytes[2] = (sbyte)(i >> 8);
				bytes[3] = (sbyte)i;
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
				BinaryDocValues dv = reader.GetBinaryDocValues("dv");
				for (int i = 0; i < reader.MaxDoc; i++)
				{
					bytes[0] = (sbyte)(expectedValue >> 24);
					bytes[1] = (sbyte)(expectedValue >> 16);
					bytes[2] = (sbyte)(expectedValue >> 8);
					bytes[3] = (sbyte)expectedValue;
					dv.Get(i, scratch);
					AreEqual(data, scratch);
					expectedValue++;
				}
			}
			r.Dispose();
			dir.Dispose();
		}

		// indexes Integer.MAX_VALUE docs with a variable binary field
		[Test]
		public virtual void TestVariableBinary()
		{
			BaseDirectoryWrapper dir = NewFSDirectory(CreateTempDir("2BVariableBinary"));
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
			var bytes = new sbyte[4];
			ByteArrayDataOutput encoder = new ByteArrayDataOutput(bytes.ToBytes());
			BytesRef data = new BytesRef(bytes);
			BinaryDocValuesField dvField = new BinaryDocValuesField("dv", data);
			doc.Add(dvField);
			for (int i = 0; i < int.MaxValue; i++)
			{
				encoder.Reset(bytes);
				encoder.WriteVInt(i % 65535);
				// 1, 2, or 3 bytes
				data.length = encoder.Position;
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
			ByteArrayDataInput input = new ByteArrayDataInput();
			foreach (AtomicReaderContext context in r.Leaves)
			{
				AtomicReader reader = ((AtomicReader)context.Reader);
				BytesRef scratch = new BytesRef(bytes);
				BinaryDocValues dv = reader.GetBinaryDocValues("dv");
				for (int i_1 = 0; i_1 < reader.MaxDoc; i_1++)
				{
					dv.Get(i_1, scratch);
					input.Reset(scratch.bytes.ToBytes(), scratch.offset, scratch.length);
					AreEqual(expectedValue % 65535, input.ReadVInt());
					IsTrue(input.EOF);
					expectedValue++;
				}
			}
			r.Dispose();
			dir.Dispose();
		}
	}
}
