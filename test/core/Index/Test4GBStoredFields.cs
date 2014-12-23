/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Com.Carrotsearch.Randomizedtesting.Generators;
using Lucene.Net.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	/// <summary>This test creates an index with one segment that is a little larger than 4GB.
	/// 	</summary>
	/// <remarks>This test creates an index with one segment that is a little larger than 4GB.
	/// 	</remarks>
	public class Test4GBStoredFields : LuceneTestCase
	{
		/// <exception cref="System.Exception"></exception>
		[LuceneTestCase.Nightly]
		public virtual void Test()
		{
			MockDirectoryWrapper dir = new MockDirectoryWrapper(Random(), new MMapDirectory(CreateTempDir
				("4GBStoredFields")));
			dir.SetThrottling(MockDirectoryWrapper.Throttling.NEVER);
			IndexWriter w = new IndexWriter(dir, ((IndexWriterConfig)((IndexWriterConfig)new 
				IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs
				(IndexWriterConfig.DISABLE_AUTO_FLUSH)).SetRAMBufferSizeMB(256.0)).SetMergeScheduler
				(new ConcurrentMergeScheduler()).SetMergePolicy(NewLogMergePolicy(false, 10)).SetOpenMode
				(IndexWriterConfig.OpenMode.CREATE));
			MergePolicy mp = w.GetConfig().GetMergePolicy();
			if (mp is LogByteSizeMergePolicy)
			{
				// 1 petabyte:
				((LogByteSizeMergePolicy)mp).SetMaxMergeMB(1024 * 1024 * 1024);
			}
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			FieldType ft = new FieldType();
			ft.SetIndexed(false);
			ft.SetStored(true);
			ft.Freeze();
			int valueLength = RandomInts.RandomIntBetween(Random(), 1 << 13, 1 << 20);
			byte[] value = new byte[valueLength];
			for (int i = 0; i < valueLength; ++i)
			{
				// random so that even compressing codecs can't compress it
				value[i] = unchecked((byte)Random().Next(256));
			}
			Field f = new Field("fld", value, ft);
			doc.Add(f);
			int numDocs = (int)((1L << 32) / valueLength + 100);
			for (int i_1 = 0; i_1 < numDocs; ++i_1)
			{
				w.AddDocument(doc);
				if (VERBOSE && i_1 % (numDocs / 10) == 0)
				{
					System.Console.Out.WriteLine(i_1 + " of " + numDocs + "...");
				}
			}
			w.ForceMerge(1);
			w.Close();
			if (VERBOSE)
			{
				bool found = false;
				foreach (string file in dir.ListAll())
				{
					if (file.EndsWith(".fdt"))
					{
						long fileLength = dir.FileLength(file);
						if (fileLength >= 1L << 32)
						{
							found = true;
						}
						System.Console.Out.WriteLine("File length of " + file + " : " + fileLength);
					}
				}
				if (!found)
				{
					System.Console.Out.WriteLine("No .fdt file larger than 4GB, test bug?");
				}
			}
			DirectoryReader rd = DirectoryReader.Open(dir);
			Lucene.Net.Document.Document sd = rd.Document(numDocs - 1);
			NUnit.Framework.Assert.IsNotNull(sd);
			NUnit.Framework.Assert.AreEqual(1, sd.GetFields().Count);
			BytesRef valueRef = sd.GetBinaryValue("fld");
			NUnit.Framework.Assert.IsNotNull(valueRef);
			NUnit.Framework.Assert.AreEqual(new BytesRef(value), valueRef);
			rd.Close();
			dir.Close();
		}
	}
}
