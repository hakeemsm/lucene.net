using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.TestFramework;
using Lucene.Net.TestFramework.Index;
using NUnit.Framework;

namespace Lucene.Net.Test.Index
{
	public class TestRollback : LuceneTestCase
	{
		// LUCENE-2536
		[Test]
		public virtual void TestRollbackIntegrityWithBufferFlush()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter rw = new RandomIndexWriter(Random(), dir);
			for (int i = 0; i < 5; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(NewStringField("pk", i.ToString(), Field.Store.YES));
				rw.AddDocument(doc);
			}
			rw.Dispose();
			// If buffer size is small enough to cause a flush, errors ensue...
			IndexWriter w = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetMaxBufferedDocs(2)).SetOpenMode(IndexWriterConfig.OpenMode
				.APPEND));
			for (int i = 0; i < 3; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				string value = i.ToString();
				doc.Add(NewStringField("pk", value, Field.Store.YES));
				doc.Add(NewStringField("text", "foo", Field.Store.YES));
				w.UpdateDocument(new Term("pk", value), doc);
			}
			w.Rollback();
			IndexReader r = DirectoryReader.Open(dir);
			AssertEquals("index should contain same number of docs post rollback"
				, 5, r.NumDocs);
			r.Dispose();
			dir.Dispose();
		}
	}
}
