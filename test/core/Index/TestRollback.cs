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
	public class TestRollback : LuceneTestCase
	{
		// LUCENE-2536
		/// <exception cref="System.Exception"></exception>
		public virtual void TestRollbackIntegrityWithBufferFlush()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter rw = new RandomIndexWriter(Random(), dir);
			for (int i = 0; i < 5; i++)
			{
				Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
					();
				doc.Add(NewStringField("pk", Sharpen.Extensions.ToString(i), Field.Store.YES));
				rw.AddDocument(doc);
			}
			rw.Close();
			// If buffer size is small enough to cause a flush, errors ensue...
			IndexWriter w = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetMaxBufferedDocs(2)).SetOpenMode(IndexWriterConfig.OpenMode
				.APPEND));
			for (int i_1 = 0; i_1 < 3; i_1++)
			{
				Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
					();
				string value = Sharpen.Extensions.ToString(i_1);
				doc.Add(NewStringField("pk", value, Field.Store.YES));
				doc.Add(NewStringField("text", "foo", Field.Store.YES));
				w.UpdateDocument(new Term("pk", value), doc);
			}
			w.Rollback();
			IndexReader r = DirectoryReader.Open(dir);
			NUnit.Framework.Assert.AreEqual("index should contain same number of docs post rollback"
				, 5, r.NumDocs());
			r.Close();
			dir.Close();
		}
	}
}
