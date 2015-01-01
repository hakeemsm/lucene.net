using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.TestFramework;
using NUnit.Framework;


namespace Lucene.Net.Test.Index
{
	public class TestTryDelete : LuceneTestCase
	{
		/// <exception cref="System.IO.IOException"></exception>
		private static IndexWriter GetWriter(Directory directory)
		{
			MergePolicy policy = new LogByteSizeMergePolicy();
			IndexWriterConfig conf = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			conf.SetMergePolicy(policy);
			conf.SetOpenMode(IndexWriterConfig.OpenMode.CREATE_OR_APPEND);
			IndexWriter writer = new IndexWriter(directory, conf);
			return writer;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private static Directory CreateIndex()
		{
			Directory directory = new RAMDirectory();
			IndexWriter writer = GetWriter(directory);
			for (int i = 0; i < 10; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(new StringField("foo", i.ToString(), Field.Store.YES));
				writer.AddDocument(doc);
			}
			writer.Commit();
			writer.Dispose();
			return directory;
		}

		[Test]
		public virtual void TestTryDeleteDocument()
		{
			Directory directory = CreateIndex();
			IndexWriter writer = GetWriter(directory);
			ReferenceManager<IndexSearcher> mgr = new SearcherManager(writer, true, new SearcherFactory
				());
			var mgrWriter = new NRTManager.TrackingIndexWriter(writer);
			IndexSearcher searcher = mgr.Acquire();
			TopDocs topDocs = searcher.Search(new TermQuery(new Term("foo", "0")), 100);
			AreEqual(1, topDocs.TotalHits);
			long result;
			if (Random().NextBoolean())
			{
				IndexReader r = DirectoryReader.Open(writer, true);
				result = mgrWriter.TryDeleteDocument(r, 0);
				r.Dispose();
			}
			else
			{
				result = mgrWriter.TryDeleteDocument(searcher.IndexReader, 0);
			}
			// The tryDeleteDocument should have succeeded:
			IsTrue(result != -1);
			IsTrue(writer.HasDeletions);
			if (Random().NextBoolean())
			{
				writer.Commit();
			}
			IsTrue(writer.HasDeletions);
			mgr.MaybeRefresh();
			searcher = mgr.Acquire();
			topDocs = searcher.Search(new TermQuery(new Term("foo", "0")), 100);
			AreEqual(0, topDocs.TotalHits);
		}

		[Test]
		public virtual void TestTryDeleteDocumentCloseAndReopen()
		{
			Directory directory = CreateIndex();
			IndexWriter writer = GetWriter(directory);
			ReferenceManager<IndexSearcher> mgr = new SearcherManager(writer, true, new SearcherFactory
				());
			IndexSearcher searcher = mgr.Acquire();
			TopDocs topDocs = searcher.Search(new TermQuery(new Term("foo", "0")), 100);
			AreEqual(1, topDocs.TotalHits);
			var mgrWriter = new NRTManager.TrackingIndexWriter(writer);
			long result = mgrWriter.TryDeleteDocument(DirectoryReader.Open(writer, true), 0);
			AreEqual(1, result);
			writer.Commit();
			IsTrue(writer.HasDeletions);
			mgr.MaybeRefresh();
			searcher = mgr.Acquire();
			topDocs = searcher.Search(new TermQuery(new Term("foo", "0")), 100);
			AreEqual(0, topDocs.TotalHits);
			writer.Dispose();
			searcher = new IndexSearcher(DirectoryReader.Open(directory));
			topDocs = searcher.Search(new TermQuery(new Term("foo", "0")), 100);
			AreEqual(0, topDocs.TotalHits);
		}

		[Test]
		public virtual void TestDeleteDocuments()
		{
			Directory directory = CreateIndex();
			IndexWriter writer = GetWriter(directory);
			ReferenceManager<IndexSearcher> mgr = new SearcherManager(writer, true, new SearcherFactory
				());
			IndexSearcher searcher = mgr.Acquire();
			TopDocs topDocs = searcher.Search(new TermQuery(new Term("foo", "0")), 100);
			AreEqual(1, topDocs.TotalHits);
			var mgrWriter = new NRTManager.TrackingIndexWriter(writer);
			long result = mgrWriter.DeleteDocuments(new TermQuery(new Term("foo", "0")));
			AreEqual(1, result);
			// writer.commit();
			IsTrue(writer.HasDeletions);
			mgr.MaybeRefresh();
			searcher = mgr.Acquire();
			topDocs = searcher.Search(new TermQuery(new Term("foo", "0")), 100);
			AreEqual(0, topDocs.TotalHits);
		}
	}
}
