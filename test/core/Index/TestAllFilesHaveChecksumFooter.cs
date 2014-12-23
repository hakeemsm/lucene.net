/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Analysis;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Lucene46;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	/// <summary>Test that a plain default puts CRC32 footers in all files.</summary>
	/// <remarks>Test that a plain default puts CRC32 footers in all files.</remarks>
	public class TestAllFilesHaveChecksumFooter : LuceneTestCase
	{
		/// <exception cref="System.Exception"></exception>
		public virtual void Test()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			conf.SetCodec(new Lucene46Codec());
			RandomIndexWriter riw = new RandomIndexWriter(Random(), dir, conf);
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			// these fields should sometimes get term vectors, etc
			Field idField = NewStringField("id", string.Empty, Field.Store.NO);
			Field bodyField = NewTextField("body", string.Empty, Field.Store.NO);
			Field dvField = new NumericDocValuesField("dv", 5);
			doc.Add(idField);
			doc.Add(bodyField);
			doc.Add(dvField);
			for (int i = 0; i < 100; i++)
			{
				idField.SetStringValue(Sharpen.Extensions.ToString(i));
				bodyField.SetStringValue(TestUtil.RandomUnicodeString(Random()));
				riw.AddDocument(doc);
				if (Random().Next(7) == 0)
				{
					riw.Commit();
				}
				if (Random().Next(20) == 0)
				{
					riw.DeleteDocuments(new Term("id", Sharpen.Extensions.ToString(i)));
				}
			}
			riw.Close();
			CheckHeaders(dir);
			dir.Close();
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
					cfsDir.Close();
				}
				IndexInput @in = null;
				bool success = false;
				try
				{
					@in = dir.OpenInput(file, NewIOContext(Random()));
					CodecUtil.ChecksumEntireFile(@in);
					success = true;
				}
				finally
				{
					if (success)
					{
						IOUtils.Close(@in);
					}
					else
					{
						IOUtils.CloseWhileHandlingException(@in);
					}
				}
			}
		}
	}
}
