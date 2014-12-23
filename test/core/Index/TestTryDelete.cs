/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
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
				Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
					();
				doc.Add(new StringField("foo", i.ToString(), Field.Store.YES));
				writer.AddDocument(doc);
			}
			writer.Commit();
			writer.Close();
			return directory;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestTryDeleteDocument()
		{
			Directory directory = CreateIndex();
			IndexWriter writer = GetWriter(directory);
			ReferenceManager<IndexSearcher> mgr = new SearcherManager(writer, true, new SearcherFactory
				());
			TrackingIndexWriter mgrWriter = new TrackingIndexWriter(writer);
			IndexSearcher searcher = mgr.Acquire();
			TopDocs topDocs = searcher.Search(new TermQuery(new Term("foo", "0")), 100);
			NUnit.Framework.Assert.AreEqual(1, topDocs.totalHits);
			long result;
			if (Random().NextBoolean())
			{
				IndexReader r = DirectoryReader.Open(writer, true);
				result = mgrWriter.TryDeleteDocument(r, 0);
				r.Close();
			}
			else
			{
				result = mgrWriter.TryDeleteDocument(searcher.GetIndexReader(), 0);
			}
			// The tryDeleteDocument should have succeeded:
			NUnit.Framework.Assert.IsTrue(result != -1);
			NUnit.Framework.Assert.IsTrue(writer.HasDeletions());
			if (Random().NextBoolean())
			{
				writer.Commit();
			}
			NUnit.Framework.Assert.IsTrue(writer.HasDeletions());
			mgr.MaybeRefresh();
			searcher = mgr.Acquire();
			topDocs = searcher.Search(new TermQuery(new Term("foo", "0")), 100);
			NUnit.Framework.Assert.AreEqual(0, topDocs.totalHits);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestTryDeleteDocumentCloseAndReopen()
		{
			Directory directory = CreateIndex();
			IndexWriter writer = GetWriter(directory);
			ReferenceManager<IndexSearcher> mgr = new SearcherManager(writer, true, new SearcherFactory
				());
			IndexSearcher searcher = mgr.Acquire();
			TopDocs topDocs = searcher.Search(new TermQuery(new Term("foo", "0")), 100);
			NUnit.Framework.Assert.AreEqual(1, topDocs.totalHits);
			TrackingIndexWriter mgrWriter = new TrackingIndexWriter(writer);
			long result = mgrWriter.TryDeleteDocument(DirectoryReader.Open(writer, true), 0);
			NUnit.Framework.Assert.AreEqual(1, result);
			writer.Commit();
			NUnit.Framework.Assert.IsTrue(writer.HasDeletions());
			mgr.MaybeRefresh();
			searcher = mgr.Acquire();
			topDocs = searcher.Search(new TermQuery(new Term("foo", "0")), 100);
			NUnit.Framework.Assert.AreEqual(0, topDocs.totalHits);
			writer.Close();
			searcher = new IndexSearcher(DirectoryReader.Open(directory));
			topDocs = searcher.Search(new TermQuery(new Term("foo", "0")), 100);
			NUnit.Framework.Assert.AreEqual(0, topDocs.totalHits);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestDeleteDocuments()
		{
			Directory directory = CreateIndex();
			IndexWriter writer = GetWriter(directory);
			ReferenceManager<IndexSearcher> mgr = new SearcherManager(writer, true, new SearcherFactory
				());
			IndexSearcher searcher = mgr.Acquire();
			TopDocs topDocs = searcher.Search(new TermQuery(new Term("foo", "0")), 100);
			NUnit.Framework.Assert.AreEqual(1, topDocs.totalHits);
			TrackingIndexWriter mgrWriter = new TrackingIndexWriter(writer);
			long result = mgrWriter.DeleteDocuments(new TermQuery(new Term("foo", "0")));
			NUnit.Framework.Assert.AreEqual(1, result);
			// writer.commit();
			NUnit.Framework.Assert.IsTrue(writer.HasDeletions());
			mgr.MaybeRefresh();
			searcher = mgr.Acquire();
			topDocs = searcher.Search(new TermQuery(new Term("foo", "0")), 100);
			NUnit.Framework.Assert.AreEqual(0, topDocs.totalHits);
		}
	}
}
