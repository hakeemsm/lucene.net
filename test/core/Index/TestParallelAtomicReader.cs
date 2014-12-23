/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
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
			single.GetIndexReader().Close();
			single = null;
			parallel.GetIndexReader().Close();
			parallel = null;
			dir.Close();
			dir = null;
			dir1.Close();
			dir1 = null;
			dir2.Close();
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
			NUnit.Framework.Assert.AreEqual(4, fieldInfos.Size());
			NUnit.Framework.Assert.IsNotNull(fieldInfos.FieldInfo("f1"));
			NUnit.Framework.Assert.IsNotNull(fieldInfos.FieldInfo("f2"));
			NUnit.Framework.Assert.IsNotNull(fieldInfos.FieldInfo("f3"));
			NUnit.Framework.Assert.IsNotNull(fieldInfos.FieldInfo("f4"));
			pr.Close();
			dir1.Close();
			dir2.Close();
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
			NUnit.Framework.Assert.AreEqual(1, ir1.GetRefCount());
			NUnit.Framework.Assert.AreEqual(1, ir2.GetRefCount());
			pr.Close();
			NUnit.Framework.Assert.AreEqual(0, ir1.GetRefCount());
			NUnit.Framework.Assert.AreEqual(0, ir2.GetRefCount());
			dir1.Close();
			dir2.Close();
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
			NUnit.Framework.Assert.AreEqual(2, ir1.GetRefCount());
			NUnit.Framework.Assert.AreEqual(2, ir2.GetRefCount());
			pr.Close();
			NUnit.Framework.Assert.AreEqual(1, ir1.GetRefCount());
			NUnit.Framework.Assert.AreEqual(1, ir2.GetRefCount());
			ir1.Close();
			ir2.Close();
			NUnit.Framework.Assert.AreEqual(0, ir1.GetRefCount());
			NUnit.Framework.Assert.AreEqual(0, ir2.GetRefCount());
			dir1.Close();
			dir2.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestCloseInnerReader()
		{
			Directory dir1 = GetDir1(Random());
			AtomicReader ir1 = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(dir1));
			// with overlapping
			ParallelAtomicReader pr = new ParallelAtomicReader(true, new AtomicReader[] { ir1
				 }, new AtomicReader[] { ir1 });
			ir1.Close();
			try
			{
				pr.Document(0);
				NUnit.Framework.Assert.Fail("ParallelAtomicReader should be already closed because inner reader was closed!"
					);
			}
			catch (AlreadyClosedException)
			{
			}
			// pass
			// noop:
			pr.Close();
			dir1.Close();
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
			Lucene.Net.Document.Document d3 = new Lucene.Net.Document.Document(
				);
			d3.Add(NewTextField("f3", "v1", Field.Store.YES));
			w2.AddDocument(d3);
			w2.Close();
			AtomicReader ir1 = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(dir1));
			AtomicReader ir2 = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(dir2));
			try
			{
				new ParallelAtomicReader(ir1, ir2);
				NUnit.Framework.Assert.Fail("didn't get exptected exception: indexes don't have same number of documents"
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
				NUnit.Framework.Assert.Fail("didn't get expected exception: indexes don't have same number of documents"
					);
			}
			catch (ArgumentException)
			{
			}
			// expected exception
			// check RefCounts
			NUnit.Framework.Assert.AreEqual(1, ir1.GetRefCount());
			NUnit.Framework.Assert.AreEqual(1, ir2.GetRefCount());
			ir1.Close();
			ir2.Close();
			dir1.Close();
			dir2.Close();
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
			NUnit.Framework.Assert.AreEqual("v1", pr.Document(0).Get("f1"));
			NUnit.Framework.Assert.AreEqual("v1", pr.Document(0).Get("f2"));
			NUnit.Framework.Assert.IsNull(pr.Document(0).Get("f3"));
			NUnit.Framework.Assert.IsNull(pr.Document(0).Get("f4"));
			// check that fields are there
			NUnit.Framework.Assert.IsNotNull(pr.Terms("f1"));
			NUnit.Framework.Assert.IsNotNull(pr.Terms("f2"));
			NUnit.Framework.Assert.IsNotNull(pr.Terms("f3"));
			NUnit.Framework.Assert.IsNotNull(pr.Terms("f4"));
			pr.Close();
			// no stored fields at all
			pr = new ParallelAtomicReader(false, new AtomicReader[] { ir2 }, new AtomicReader
				[0]);
			NUnit.Framework.Assert.IsNull(pr.Document(0).Get("f1"));
			NUnit.Framework.Assert.IsNull(pr.Document(0).Get("f2"));
			NUnit.Framework.Assert.IsNull(pr.Document(0).Get("f3"));
			NUnit.Framework.Assert.IsNull(pr.Document(0).Get("f4"));
			// check that fields are there
			NUnit.Framework.Assert.IsNull(pr.Terms("f1"));
			NUnit.Framework.Assert.IsNull(pr.Terms("f2"));
			NUnit.Framework.Assert.IsNotNull(pr.Terms("f3"));
			NUnit.Framework.Assert.IsNotNull(pr.Terms("f4"));
			pr.Close();
			// without overlapping
			pr = new ParallelAtomicReader(true, new AtomicReader[] { ir2 }, new AtomicReader[
				] { ir1 });
			NUnit.Framework.Assert.AreEqual("v1", pr.Document(0).Get("f1"));
			NUnit.Framework.Assert.AreEqual("v1", pr.Document(0).Get("f2"));
			NUnit.Framework.Assert.IsNull(pr.Document(0).Get("f3"));
			NUnit.Framework.Assert.IsNull(pr.Document(0).Get("f4"));
			// check that fields are there
			NUnit.Framework.Assert.IsNull(pr.Terms("f1"));
			NUnit.Framework.Assert.IsNull(pr.Terms("f2"));
			NUnit.Framework.Assert.IsNotNull(pr.Terms("f3"));
			NUnit.Framework.Assert.IsNotNull(pr.Terms("f4"));
			pr.Close();
			// no main readers
			try
			{
				new ParallelAtomicReader(true, new AtomicReader[0], new AtomicReader[] { ir1 });
				NUnit.Framework.Assert.Fail("didn't get expected exception: need a non-empty main-reader array"
					);
			}
			catch (ArgumentException)
			{
			}
			// pass
			dir1.Close();
			dir2.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void QueryTest(Query query)
		{
			ScoreDoc[] parallelHits = parallel.Search(query, null, 1000).scoreDocs;
			ScoreDoc[] singleHits = single.Search(query, null, 1000).scoreDocs;
			NUnit.Framework.Assert.AreEqual(parallelHits.Length, singleHits.Length);
			for (int i = 0; i < parallelHits.Length; i++)
			{
				NUnit.Framework.Assert.AreEqual(parallelHits[i].score, singleHits[i].score, 0.001f
					);
				Lucene.Net.Document.Document docParallel = parallel.Doc(parallelHits[i].doc
					);
				Lucene.Net.Document.Document docSingle = single.Doc(singleHits[i].doc);
				NUnit.Framework.Assert.AreEqual(docParallel.Get("f1"), docSingle.Get("f1"));
				NUnit.Framework.Assert.AreEqual(docParallel.Get("f2"), docSingle.Get("f2"));
				NUnit.Framework.Assert.AreEqual(docParallel.Get("f3"), docSingle.Get("f3"));
				NUnit.Framework.Assert.AreEqual(docParallel.Get("f4"), docSingle.Get("f4"));
			}
		}

		// Fields 1-4 indexed together:
		/// <exception cref="System.IO.IOException"></exception>
		private IndexSearcher Single(Random random)
		{
			dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(random)));
			Lucene.Net.Document.Document d1 = new Lucene.Net.Document.Document(
				);
			d1.Add(NewTextField("f1", "v1", Field.Store.YES));
			d1.Add(NewTextField("f2", "v1", Field.Store.YES));
			d1.Add(NewTextField("f3", "v1", Field.Store.YES));
			d1.Add(NewTextField("f4", "v1", Field.Store.YES));
			w.AddDocument(d1);
			Lucene.Net.Document.Document d2 = new Lucene.Net.Document.Document(
				);
			d2.Add(NewTextField("f1", "v2", Field.Store.YES));
			d2.Add(NewTextField("f2", "v2", Field.Store.YES));
			d2.Add(NewTextField("f3", "v2", Field.Store.YES));
			d2.Add(NewTextField("f4", "v2", Field.Store.YES));
			w.AddDocument(d2);
			w.Close();
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
			Lucene.Net.Document.Document d1 = new Lucene.Net.Document.Document(
				);
			d1.Add(NewTextField("f1", "v1", Field.Store.YES));
			d1.Add(NewTextField("f2", "v1", Field.Store.YES));
			w1.AddDocument(d1);
			Lucene.Net.Document.Document d2 = new Lucene.Net.Document.Document(
				);
			d2.Add(NewTextField("f1", "v2", Field.Store.YES));
			d2.Add(NewTextField("f2", "v2", Field.Store.YES));
			w1.AddDocument(d2);
			w1.Close();
			return dir1;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private Directory GetDir2(Random random)
		{
			Directory dir2 = NewDirectory();
			IndexWriter w2 = new IndexWriter(dir2, NewIndexWriterConfig(TEST_VERSION_CURRENT, 
				new MockAnalyzer(random)));
			Lucene.Net.Document.Document d3 = new Lucene.Net.Document.Document(
				);
			d3.Add(NewTextField("f3", "v1", Field.Store.YES));
			d3.Add(NewTextField("f4", "v1", Field.Store.YES));
			w2.AddDocument(d3);
			Lucene.Net.Document.Document d4 = new Lucene.Net.Document.Document(
				);
			d4.Add(NewTextField("f3", "v2", Field.Store.YES));
			d4.Add(NewTextField("f4", "v2", Field.Store.YES));
			w2.AddDocument(d4);
			w2.Close();
			return dir2;
		}
	}
}
