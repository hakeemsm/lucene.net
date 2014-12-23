/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	public class Test2BBinaryDocValues : LuceneTestCase
	{
		// indexes Integer.MAX_VALUE docs with a fixed binary field
		/// <exception cref="System.Exception"></exception>
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
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			byte[] bytes = new byte[4];
			BytesRef data = new BytesRef(bytes);
			BinaryDocValuesField dvField = new BinaryDocValuesField("dv", data);
			doc.Add(dvField);
			for (int i = 0; i < int.MaxValue; i++)
			{
				bytes[0] = unchecked((byte)(i >> 24));
				bytes[1] = unchecked((byte)(i >> 16));
				bytes[2] = unchecked((byte)(i >> 8));
				bytes[3] = unchecked((byte)i);
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
			int expectedValue = 0;
			foreach (AtomicReaderContext context in r.Leaves())
			{
				AtomicReader reader = ((AtomicReader)context.Reader());
				BytesRef scratch = new BytesRef();
				BinaryDocValues dv = reader.GetBinaryDocValues("dv");
				for (int i_1 = 0; i_1 < reader.MaxDoc(); i_1++)
				{
					bytes[0] = unchecked((byte)(expectedValue >> 24));
					bytes[1] = unchecked((byte)(expectedValue >> 16));
					bytes[2] = unchecked((byte)(expectedValue >> 8));
					bytes[3] = unchecked((byte)expectedValue);
					dv.Get(i_1, scratch);
					NUnit.Framework.Assert.AreEqual(data, scratch);
					expectedValue++;
				}
			}
			r.Close();
			dir.Close();
		}

		// indexes Integer.MAX_VALUE docs with a variable binary field
		/// <exception cref="System.Exception"></exception>
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
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			byte[] bytes = new byte[4];
			ByteArrayDataOutput encoder = new ByteArrayDataOutput(bytes);
			BytesRef data = new BytesRef(bytes);
			BinaryDocValuesField dvField = new BinaryDocValuesField("dv", data);
			doc.Add(dvField);
			for (int i = 0; i < int.MaxValue; i++)
			{
				encoder.Reset(bytes);
				encoder.WriteVInt(i % 65535);
				// 1, 2, or 3 bytes
				data.length = encoder.GetPosition();
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
			int expectedValue = 0;
			ByteArrayDataInput input = new ByteArrayDataInput();
			foreach (AtomicReaderContext context in r.Leaves())
			{
				AtomicReader reader = ((AtomicReader)context.Reader());
				BytesRef scratch = new BytesRef(bytes);
				BinaryDocValues dv = reader.GetBinaryDocValues("dv");
				for (int i_1 = 0; i_1 < reader.MaxDoc(); i_1++)
				{
					dv.Get(i_1, scratch);
					input.Reset(scratch.bytes, scratch.offset, scratch.length);
					NUnit.Framework.Assert.AreEqual(expectedValue % 65535, input.ReadVInt());
					NUnit.Framework.Assert.IsTrue(input.Eof());
					expectedValue++;
				}
			}
			r.Close();
			dir.Close();
		}
	}
}
