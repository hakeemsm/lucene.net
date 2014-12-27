/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Test.Index
{
	public class TestParallelAtomicReader : LuceneTestCase
	{
		private IndexSearcher parallel;

		private IndexSearcher single;

		private Directory dir;

		private Directory dir1;

		private Directory dir2;

		/// <exception cref="System.Exception"></exception>
		public virtual void TestQueries()
		{
			single = Single(Random());
			parallel = Parallel(Random());
			QueryTest(new TermQuery(new Term("f1", "v1")));
			QueryTest(new TermQuery(new Term("f1", "v2")));
			QueryTest(new TermQuery(new Term("f2", "v1")));
			QueryTest(new TermQuery(new Term("f2", "v2")));
			QueryTest(new TermQuery(new Term("f3", "v1")));
			QueryTest(new TermQuery(new Term("f3", "v2")));
			QueryTest(new TermQuery(new Term("f4", "v1")));
			QueryTest(new TermQuery(new Term("f4", "v2")));
			BooleanQuery bq1 = new BooleanQuery();
			bq1.Add(new TermQuery(new Term("f1", "v1")), BooleanClause.Occur.MUST);
			bq1.Add(new TermQuery(new Term("f4", "v1")), BooleanClause.Occur.MUST);
			QueryTest(bq1);
			single.IndexReader.Dispose();
			single = null;
			parallel.IndexReader.Dispose();
			parallel = null;
			dir.Dispose();
			dir = null;
			dir1.Dispose();
			dir1 = null;
			dir2.Dispose();
			dir2 = null;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestFieldNames()
		{
			Directory dir1 = GetDir1(Random());
			Directory dir2 = GetDir2(Random());
			ParallelAtomicReader pr = new ParallelAtomicReader(SlowCompositeReaderWrapper.Wrap
				(DirectoryReader.Open(dir1)), SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open
				(dir2)));
			FieldInfos fieldInfos = pr.GetFieldInfos();
			AreEqual(4, fieldInfos.Size());
			IsNotNull(fieldInfos.FieldInfo("f1"));
			IsNotNull(fieldInfos.FieldInfo("f2"));
			IsNotNull(fieldInfos.FieldInfo("f3"));
			IsNotNull(fieldInfos.FieldInfo("f4"));
			pr.Dispose();
			dir1.Dispose();
			dir2.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestRefCounts1()
		{
			Directory dir1 = GetDir1(Random());
			Directory dir2 = GetDir2(Random());
			AtomicReader ir1;
			AtomicReader ir2;
			// close subreaders, ParallelReader will not change refCounts, but close on its own close
			ParallelAtomicReader pr = new ParallelAtomicReader(ir1 = SlowCompositeReaderWrapper
				.Wrap(DirectoryReader.Open(dir1)), ir2 = SlowCompositeReaderWrapper.Wrap(DirectoryReader
				.Open(dir2)));
			// check RefCounts
			AreEqual(1, ir1.GetRefCount());
			AreEqual(1, ir2.GetRefCount());
			pr.Dispose();
			AreEqual(0, ir1.GetRefCount());
			AreEqual(0, ir2.GetRefCount());
			dir1.Dispose();
			dir2.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestRefCounts2()
		{
			Directory dir1 = GetDir1(Random());
			Directory dir2 = GetDir2(Random());
			AtomicReader ir1 = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(dir1));
			AtomicReader ir2 = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(dir2));
			// don't close subreaders, so ParallelReader will increment refcounts
			ParallelAtomicReader pr = new ParallelAtomicReader(false, ir1, ir2);
			// check RefCounts
			AreEqual(2, ir1.GetRefCount());
			AreEqual(2, ir2.GetRefCount());
			pr.Dispose();
			AreEqual(1, ir1.GetRefCount());
			AreEqual(1, ir2.GetRefCount());
			ir1.Dispose();
			ir2.Dispose();
			AreEqual(0, ir1.GetRefCount());
			AreEqual(0, ir2.GetRefCount());
			dir1.Dispose();
			dir2.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestCloseInnerReader()
		{
			Directory dir1 = GetDir1(Random());
			AtomicReader ir1 = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(dir1));
			// with overlapping
			ParallelAtomicReader pr = new ParallelAtomicReader(true, new AtomicReader[] { ir1
				 }, new AtomicReader[] { ir1 });
			ir1.Dispose();
			try
			{
				pr.Document(0);
				Fail("ParallelAtomicReader should be already closed because inner reader was closed!"
					);
			}
			catch (AlreadyClosedException)
			{
			}
			// pass
			// noop:
			pr.Dispose();
			dir1.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestIncompatibleIndexes()
		{
			// two documents:
			Directory dir1 = GetDir1(Random());
			// one document only:
			Directory dir2 = NewDirectory();
			IndexWriter w2 = new IndexWriter(dir2, NewIndexWriterConfig(TEST_VERSION_CURRENT, 
				new MockAnalyzer(Random())));
			Lucene.Net.Documents.Document d3 = new Lucene.Net.Documents.Document(
				);
			d3.Add(NewTextField("f3", "v1", Field.Store.YES));
			w2.AddDocument(d3);
			w2.Dispose();
			AtomicReader ir1 = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(dir1));
			AtomicReader ir2 = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(dir2));
			try
			{
				new ParallelAtomicReader(ir1, ir2);
				Fail("didn't get exptected exception: indexes don't have same number of documents"
					);
			}
			catch (ArgumentException)
			{
			}
			// expected exception
			try
			{
				new ParallelAtomicReader(Random().NextBoolean(), new AtomicReader[] { ir1, ir2 }, 
					new AtomicReader[] { ir1, ir2 });
				Fail("didn't get expected exception: indexes don't have same number of documents"
					);
			}
			catch (ArgumentException)
			{
			}
			// expected exception
			// check RefCounts
			AreEqual(1, ir1.GetRefCount());
			AreEqual(1, ir2.GetRefCount());
			ir1.Dispose();
			ir2.Dispose();
			dir1.Dispose();
			dir2.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestIgnoreStoredFields()
		{
			Directory dir1 = GetDir1(Random());
			Directory dir2 = GetDir2(Random());
			AtomicReader ir1 = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(dir1));
			AtomicReader ir2 = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(dir2));
			// with overlapping
			ParallelAtomicReader pr = new ParallelAtomicReader(false, new AtomicReader[] { ir1
				, ir2 }, new AtomicReader[] { ir1 });
			AreEqual("v1", pr.Document(0).Get("f1"));
			AreEqual("v1", pr.Document(0).Get("f2"));
			IsNull(pr.Document(0).Get("f3"));
			IsNull(pr.Document(0).Get("f4"));
			// check that fields are there
			IsNotNull(pr.Terms("f1"));
			IsNotNull(pr.Terms("f2"));
			IsNotNull(pr.Terms("f3"));
			IsNotNull(pr.Terms("f4"));
			pr.Dispose();
			// no stored fields at all
			pr = new ParallelAtomicReader(false, new AtomicReader[] { ir2 }, new AtomicReader
				[0]);
			IsNull(pr.Document(0).Get("f1"));
			IsNull(pr.Document(0).Get("f2"));
			IsNull(pr.Document(0).Get("f3"));
			IsNull(pr.Document(0).Get("f4"));
			// check that fields are there
			IsNull(pr.Terms("f1"));
			IsNull(pr.Terms("f2"));
			IsNotNull(pr.Terms("f3"));
			IsNotNull(pr.Terms("f4"));
			pr.Dispose();
			// without overlapping
			pr = new ParallelAtomicReader(true, new AtomicReader[] { ir2 }, new AtomicReader[
				] { ir1 });
			AreEqual("v1", pr.Document(0).Get("f1"));
			AreEqual("v1", pr.Document(0).Get("f2"));
			IsNull(pr.Document(0).Get("f3"));
			IsNull(pr.Document(0).Get("f4"));
			// check that fields are there
			IsNull(pr.Terms("f1"));
			IsNull(pr.Terms("f2"));
			IsNotNull(pr.Terms("f3"));
			IsNotNull(pr.Terms("f4"));
			pr.Dispose();
			// no main readers
			try
			{
				new ParallelAtomicReader(true, new AtomicReader[0], new AtomicReader[] { ir1 });
				Fail("didn't get expected exception: need a non-empty main-reader array"
					);
			}
			catch (ArgumentException)
			{
			}
			// pass
			dir1.Dispose();
			dir2.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void QueryTest(Query query)
		{
			ScoreDoc[] parallelHits = parallel.Search(query, null, 1000).ScoreDocs;
			ScoreDoc[] singleHits = single.Search(query, null, 1000).ScoreDocs;
			AreEqual(parallelHits.Length, singleHits.Length);
			for (int i = 0; i < parallelHits.Length; i++)
			{
				AreEqual(parallelHits[i].score, singleHits[i].score, 0.001f
					);
				Lucene.Net.Documents.Document docParallel = parallel.Doc(parallelHits[i].Doc
					);
				Lucene.Net.Documents.Document docSingle = single.Doc(singleHits[i].Doc);
				AreEqual(docParallel.Get("f1"), docSingle.Get("f1"));
				AreEqual(docParallel.Get("f2"), docSingle.Get("f2"));
				AreEqual(docParallel.Get("f3"), docSingle.Get("f3"));
				AreEqual(docParallel.Get("f4"), docSingle.Get("f4"));
			}
		}

		// Fields 1-4 indexed together:
		/// <exception cref="System.IO.IOException"></exception>
		private IndexSearcher Single(Random random)
		{
			dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(random)));
			Lucene.Net.Documents.Document d1 = new Lucene.Net.Documents.Document(
				);
			d1.Add(NewTextField("f1", "v1", Field.Store.YES));
			d1.Add(NewTextField("f2", "v1", Field.Store.YES));
			d1.Add(NewTextField("f3", "v1", Field.Store.YES));
			d1.Add(NewTextField("f4", "v1", Field.Store.YES));
			w.AddDocument(d1);
			Lucene.Net.Documents.Document d2 = new Lucene.Net.Documents.Document(
				);
			d2.Add(NewTextField("f1", "v2", Field.Store.YES));
			d2.Add(NewTextField("f2", "v2", Field.Store.YES));
			d2.Add(NewTextField("f3", "v2", Field.Store.YES));
			d2.Add(NewTextField("f4", "v2", Field.Store.YES));
			w.AddDocument(d2);
			w.Dispose();
			DirectoryReader ir = DirectoryReader.Open(dir);
			return NewSearcher(ir);
		}

		// Fields 1 & 2 in one index, 3 & 4 in other, with ParallelReader:
		/// <exception cref="System.IO.IOException"></exception>
		private IndexSearcher Parallel(Random random)
		{
			dir1 = GetDir1(random);
			dir2 = GetDir2(random);
			ParallelAtomicReader pr = new ParallelAtomicReader(SlowCompositeReaderWrapper.Wrap
				(DirectoryReader.Open(dir1)), SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open
				(dir2)));
			TestUtil.CheckReader(pr);
			return NewSearcher(pr);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private Directory GetDir1(Random random)
		{
			Directory dir1 = NewDirectory();
			IndexWriter w1 = new IndexWriter(dir1, NewIndexWriterConfig(TEST_VERSION_CURRENT, 
				new MockAnalyzer(random)));
			Lucene.Net.Documents.Document d1 = new Lucene.Net.Documents.Document(
				);
			d1.Add(NewTextField("f1", "v1", Field.Store.YES));
			d1.Add(NewTextField("f2", "v1", Field.Store.YES));
			w1.AddDocument(d1);
			Lucene.Net.Documents.Document d2 = new Lucene.Net.Documents.Document(
				);
			d2.Add(NewTextField("f1", "v2", Field.Store.YES));
			d2.Add(NewTextField("f2", "v2", Field.Store.YES));
			w1.AddDocument(d2);
			w1.Dispose();
			return dir1;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private Directory GetDir2(Random random)
		{
			Directory dir2 = NewDirectory();
			IndexWriter w2 = new IndexWriter(dir2, NewIndexWriterConfig(TEST_VERSION_CURRENT, 
				new MockAnalyzer(random)));
			Lucene.Net.Documents.Document d3 = new Lucene.Net.Documents.Document(
				);
			d3.Add(NewTextField("f3", "v1", Field.Store.YES));
			d3.Add(NewTextField("f4", "v1", Field.Store.YES));
			w2.AddDocument(d3);
			Lucene.Net.Documents.Document d4 = new Lucene.Net.Documents.Document(
				);
			d4.Add(NewTextField("f3", "v2", Field.Store.YES));
			d4.Add(NewTextField("f4", "v2", Field.Store.YES));
			w2.AddDocument(d4);
			w2.Dispose();
			return dir2;
		}
	}
}
