using System;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Lucene46;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.TestFramework.Util;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Index
{
	/// <summary>Test that a plain default puts CRC32 footers in all files.</summary>
	[TestFixture]
	public class TestAllFilesHaveChecksumFooter : LuceneTestCase
	{
		[Test]
		public virtual void TestFooters()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			conf.SetCodec(new Lucene46Codec());
			RandomIndexWriter riw = new RandomIndexWriter(Random(), dir, conf);
			var doc = new Lucene.Net.Documents.Document();
			// these fields should sometimes get term vectors, etc
			Field idField = NewStringField("id", string.Empty, Field.Store.NO);
			Field bodyField = NewTextField("body", string.Empty, Field.Store.NO);
			Field dvField = new NumericDocValuesField("dv", 5);
			doc.Add(idField);
			doc.Add(bodyField);
			doc.Add(dvField);
			for (int i = 0; i < 100; i++)
			{
				idField.StringValue = i.ToString();
				bodyField.StringValue = TestUtil.RandomUnicodeString(Random());
				riw.AddDocument(doc);
				if (Random().Next(7) == 0)
				{
					riw.Commit();
				}
				if (Random().Next(20) == 0)
				{
					riw.DeleteDocuments(new Term("id", i.ToString()));
				}
			}
			riw.Close();
			CheckHeaders(dir);
			dir.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void CheckHeaders(Directory dir)
		{
			foreach (string file in dir.ListAll())
			{
				if (file.Equals(IndexWriter.WRITE_LOCK_NAME))
				{
					continue;
				}
				// write.lock has no footer, thats ok
				if (file.EndsWith(IndexFileNames.COMPOUND_FILE_EXTENSION))
				{
					CompoundFileDirectory cfsDir = new CompoundFileDirectory(dir, file, NewIOContext(
						Random()), false);
					CheckHeaders(cfsDir);
					// recurse into cfs
					cfsDir.Dispose();
				}
				IndexInput indexInput = null;
				bool success = false;
				try
				{
					indexInput = dir.OpenInput(file, NewIOContext(Random()));
					CodecUtil.ChecksumEntireFile(indexInput);
					success = true;
				}
				finally
				{
					if (success)
					{
						IOUtils.Close(indexInput);
					}
					else
					{
						IOUtils.CloseWhileHandlingException((IDisposable)indexInput);
					}
				}
			}
		}
	}
}
