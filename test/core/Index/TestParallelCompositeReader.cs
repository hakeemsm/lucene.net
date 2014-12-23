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
	public class TestParallelCompositeReader : LuceneTestCase
	{
		private IndexSearcher parallel;

		private IndexSearcher single;

		private Directory dir;

		private Directory dir1;

		private Directory dir2;

		/// <exception cref="System.Exception"></exception>
		public virtual void TestQueries()
		{
			single = Single(Random(), false);
			parallel = Parallel(Random(), false);
			Queries();
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
		public virtual void TestQueriesCompositeComposite()
		{
			single = Single(Random(), true);
			parallel = Parallel(Random(), true);
			Queries();
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
		private void Queries()
		{
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
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestRefCounts1()
		{
			Directory dir1 = GetDir1(Random());
			Directory dir2 = GetDir2(Random());
			DirectoryReader ir1;
			DirectoryReader ir2;
			// close subreaders, ParallelReader will not change refCounts, but close on its own close
			ParallelCompositeReader pr = new ParallelCompositeReader(ir1 = DirectoryReader.Open
				(dir1), ir2 = DirectoryReader.Open(dir2));
			IndexReader psub1 = pr.GetSequentialSubReaders()[0];
			// check RefCounts
			NUnit.Framework.Assert.AreEqual(1, ir1.GetRefCount());
			NUnit.Framework.Assert.AreEqual(1, ir2.GetRefCount());
			NUnit.Framework.Assert.AreEqual(1, psub1.GetRefCount());
			pr.Close();
			NUnit.Framework.Assert.AreEqual(0, ir1.GetRefCount());
			NUnit.Framework.Assert.AreEqual(0, ir2.GetRefCount());
			NUnit.Framework.Assert.AreEqual(0, psub1.GetRefCount());
			dir1.Close();
			dir2.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestRefCounts2()
		{
			Directory dir1 = GetDir1(Random());
			Directory dir2 = GetDir2(Random());
			DirectoryReader ir1 = DirectoryReader.Open(dir1);
			DirectoryReader ir2 = DirectoryReader.Open(dir2);
			// don't close subreaders, so ParallelReader will increment refcounts
			ParallelCompositeReader pr = new ParallelCompositeReader(false, ir1, ir2);
			IndexReader psub1 = pr.GetSequentialSubReaders()[0];
			// check RefCounts
			NUnit.Framework.Assert.AreEqual(2, ir1.GetRefCount());
			NUnit.Framework.Assert.AreEqual(2, ir2.GetRefCount());
			NUnit.Framework.Assert.AreEqual("refCount must be 1, as the synthetic reader was created by ParallelCompositeReader"
				, 1, psub1.GetRefCount());
			pr.Close();
			NUnit.Framework.Assert.AreEqual(1, ir1.GetRefCount());
			NUnit.Framework.Assert.AreEqual(1, ir2.GetRefCount());
			NUnit.Framework.Assert.AreEqual("refcount must be 0 because parent was closed", 0
				, psub1.GetRefCount());
			ir1.Close();
			ir2.Close();
			NUnit.Framework.Assert.AreEqual(0, ir1.GetRefCount());
			NUnit.Framework.Assert.AreEqual(0, ir2.GetRefCount());
			NUnit.Framework.Assert.AreEqual("refcount should not change anymore", 0, psub1.GetRefCount
				());
			dir1.Close();
			dir2.Close();
		}

		// closeSubreaders=false
		/// <exception cref="System.Exception"></exception>
		public virtual void TestReaderClosedListener1()
		{
			Directory dir1 = GetDir1(Random());
			CompositeReader ir1 = DirectoryReader.Open(dir1);
			// with overlapping
			ParallelCompositeReader pr = new ParallelCompositeReader(false, new CompositeReader
				[] { ir1 }, new CompositeReader[] { ir1 });
			int[] listenerClosedCount = new int[1];
			NUnit.Framework.Assert.AreEqual(3, pr.Leaves().Count);
			foreach (AtomicReaderContext cxt in pr.Leaves())
			{
				((AtomicReader)cxt.Reader()).AddReaderClosedListener(new _ReaderClosedListener_141
					(listenerClosedCount));
			}
			pr.Close();
			ir1.Close();
			NUnit.Framework.Assert.AreEqual(3, listenerClosedCount[0]);
			dir1.Close();
		}

		private sealed class _ReaderClosedListener_141 : IndexReader.ReaderClosedListener
		{
			public _ReaderClosedListener_141(int[] listenerClosedCount)
			{
				this.listenerClosedCount = listenerClosedCount;
			}

			public void OnClose(IndexReader reader)
			{
				listenerClosedCount[0]++;
			}

			private readonly int[] listenerClosedCount;
		}

		// closeSubreaders=true
		/// <exception cref="System.Exception"></exception>
		public virtual void TestReaderClosedListener2()
		{
			Directory dir1 = GetDir1(Random());
			CompositeReader ir1 = DirectoryReader.Open(dir1);
			// with overlapping
			ParallelCompositeReader pr = new ParallelCompositeReader(true, new CompositeReader
				[] { ir1 }, new CompositeReader[] { ir1 });
			int[] listenerClosedCount = new int[1];
			NUnit.Framework.Assert.AreEqual(3, pr.Leaves().Count);
			foreach (AtomicReaderContext cxt in pr.Leaves())
			{
				((AtomicReader)cxt.Reader()).AddReaderClosedListener(new _ReaderClosedListener_169
					(listenerClosedCount));
			}
			pr.Close();
			NUnit.Framework.Assert.AreEqual(3, listenerClosedCount[0]);
			dir1.Close();
		}

		private sealed class _ReaderClosedListener_169 : IndexReader.ReaderClosedListener
		{
			public _ReaderClosedListener_169(int[] listenerClosedCount)
			{
				this.listenerClosedCount = listenerClosedCount;
			}

			public void OnClose(IndexReader reader)
			{
				listenerClosedCount[0]++;
			}

			private readonly int[] listenerClosedCount;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestCloseInnerReader()
		{
			Directory dir1 = GetDir1(Random());
			CompositeReader ir1 = DirectoryReader.Open(dir1);
			NUnit.Framework.Assert.AreEqual(1, ir1.GetSequentialSubReaders()[0].GetRefCount()
				);
			// with overlapping
			ParallelCompositeReader pr = new ParallelCompositeReader(true, new CompositeReader
				[] { ir1 }, new CompositeReader[] { ir1 });
			IndexReader psub = pr.GetSequentialSubReaders()[0];
			NUnit.Framework.Assert.AreEqual(1, psub.GetRefCount());
			ir1.Close();
			NUnit.Framework.Assert.AreEqual("refCount of synthetic subreader should be unchanged"
				, 1, psub.GetRefCount());
			try
			{
				psub.Document(0);
				NUnit.Framework.Assert.Fail("Subreader should be already closed because inner reader was closed!"
					);
			}
			catch (AlreadyClosedException)
			{
			}
			// pass
			try
			{
				pr.Document(0);
				NUnit.Framework.Assert.Fail("ParallelCompositeReader should be already closed because inner reader was closed!"
					);
			}
			catch (AlreadyClosedException)
			{
			}
			// pass
			// noop:
			pr.Close();
			NUnit.Framework.Assert.AreEqual(0, psub.GetRefCount());
			dir1.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestIncompatibleIndexes1()
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
			DirectoryReader ir1 = DirectoryReader.Open(dir1);
			DirectoryReader ir2 = DirectoryReader.Open(dir2);
			try
			{
				new ParallelCompositeReader(ir1, ir2);
				NUnit.Framework.Assert.Fail("didn't get expected exception: indexes don't have same number of documents"
					);
			}
			catch (ArgumentException)
			{
			}
			// expected exception
			try
			{
				new ParallelCompositeReader(Random().NextBoolean(), ir1, ir2);
				NUnit.Framework.Assert.Fail("didn't get expected exception: indexes don't have same number of documents"
					);
			}
			catch (ArgumentException)
			{
			}
			// expected exception
			NUnit.Framework.Assert.AreEqual(1, ir1.GetRefCount());
			NUnit.Framework.Assert.AreEqual(1, ir2.GetRefCount());
			ir1.Close();
			ir2.Close();
			NUnit.Framework.Assert.AreEqual(0, ir1.GetRefCount());
			NUnit.Framework.Assert.AreEqual(0, ir2.GetRefCount());
			dir1.Close();
			dir2.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestIncompatibleIndexes2()
		{
			Directory dir1 = GetDir1(Random());
			Directory dir2 = GetInvalidStructuredDir2(Random());
			DirectoryReader ir1 = DirectoryReader.Open(dir1);
			DirectoryReader ir2 = DirectoryReader.Open(dir2);
			CompositeReader[] readers = new CompositeReader[] { ir1, ir2 };
			try
			{
				new ParallelCompositeReader(readers);
				NUnit.Framework.Assert.Fail("didn't get expected exception: indexes don't have same subreader structure"
					);
			}
			catch (ArgumentException)
			{
			}
			// expected exception
			try
			{
				new ParallelCompositeReader(Random().NextBoolean(), readers, readers);
				NUnit.Framework.Assert.Fail("didn't get expected exception: indexes don't have same subreader structure"
					);
			}
			catch (ArgumentException)
			{
			}
			// expected exception
			NUnit.Framework.Assert.AreEqual(1, ir1.GetRefCount());
			NUnit.Framework.Assert.AreEqual(1, ir2.GetRefCount());
			ir1.Close();
			ir2.Close();
			NUnit.Framework.Assert.AreEqual(0, ir1.GetRefCount());
			NUnit.Framework.Assert.AreEqual(0, ir2.GetRefCount());
			dir1.Close();
			dir2.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestIncompatibleIndexes3()
		{
			Directory dir1 = GetDir1(Random());
			Directory dir2 = GetDir2(Random());
			CompositeReader ir1 = new MultiReader(DirectoryReader.Open(dir1), SlowCompositeReaderWrapper
				.Wrap(DirectoryReader.Open(dir1)));
			CompositeReader ir2 = new MultiReader(DirectoryReader.Open(dir2), DirectoryReader
				.Open(dir2));
			CompositeReader[] readers = new CompositeReader[] { ir1, ir2 };
			try
			{
				new ParallelCompositeReader(readers);
				NUnit.Framework.Assert.Fail("didn't get expected exception: indexes don't have same subreader structure"
					);
			}
			catch (ArgumentException)
			{
			}
			// expected exception
			try
			{
				new ParallelCompositeReader(Random().NextBoolean(), readers, readers);
				NUnit.Framework.Assert.Fail("didn't get expected exception: indexes don't have same subreader structure"
					);
			}
			catch (ArgumentException)
			{
			}
			// expected exception
			NUnit.Framework.Assert.AreEqual(1, ir1.GetRefCount());
			NUnit.Framework.Assert.AreEqual(1, ir2.GetRefCount());
			ir1.Close();
			ir2.Close();
			NUnit.Framework.Assert.AreEqual(0, ir1.GetRefCount());
			NUnit.Framework.Assert.AreEqual(0, ir2.GetRefCount());
			dir1.Close();
			dir2.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestIgnoreStoredFields()
		{
			Directory dir1 = GetDir1(Random());
			Directory dir2 = GetDir2(Random());
			CompositeReader ir1 = DirectoryReader.Open(dir1);
			CompositeReader ir2 = DirectoryReader.Open(dir2);
			// with overlapping
			ParallelCompositeReader pr = new ParallelCompositeReader(false, new CompositeReader
				[] { ir1, ir2 }, new CompositeReader[] { ir1 });
			NUnit.Framework.Assert.AreEqual("v1", pr.Document(0).Get("f1"));
			NUnit.Framework.Assert.AreEqual("v1", pr.Document(0).Get("f2"));
			NUnit.Framework.Assert.IsNull(pr.Document(0).Get("f3"));
			NUnit.Framework.Assert.IsNull(pr.Document(0).Get("f4"));
			// check that fields are there
			AtomicReader slow = SlowCompositeReaderWrapper.Wrap(pr);
			NUnit.Framework.Assert.IsNotNull(slow.Terms("f1"));
			NUnit.Framework.Assert.IsNotNull(slow.Terms("f2"));
			NUnit.Framework.Assert.IsNotNull(slow.Terms("f3"));
			NUnit.Framework.Assert.IsNotNull(slow.Terms("f4"));
			pr.Close();
			// no stored fields at all
			pr = new ParallelCompositeReader(false, new CompositeReader[] { ir2 }, new CompositeReader
				[0]);
			NUnit.Framework.Assert.IsNull(pr.Document(0).Get("f1"));
			NUnit.Framework.Assert.IsNull(pr.Document(0).Get("f2"));
			NUnit.Framework.Assert.IsNull(pr.Document(0).Get("f3"));
			NUnit.Framework.Assert.IsNull(pr.Document(0).Get("f4"));
			// check that fields are there
			slow = SlowCompositeReaderWrapper.Wrap(pr);
			NUnit.Framework.Assert.IsNull(slow.Terms("f1"));
			NUnit.Framework.Assert.IsNull(slow.Terms("f2"));
			NUnit.Framework.Assert.IsNotNull(slow.Terms("f3"));
			NUnit.Framework.Assert.IsNotNull(slow.Terms("f4"));
			pr.Close();
			// without overlapping
			pr = new ParallelCompositeReader(true, new CompositeReader[] { ir2 }, new CompositeReader
				[] { ir1 });
			NUnit.Framework.Assert.AreEqual("v1", pr.Document(0).Get("f1"));
			NUnit.Framework.Assert.AreEqual("v1", pr.Document(0).Get("f2"));
			NUnit.Framework.Assert.IsNull(pr.Document(0).Get("f3"));
			NUnit.Framework.Assert.IsNull(pr.Document(0).Get("f4"));
			// check that fields are there
			slow = SlowCompositeReaderWrapper.Wrap(pr);
			NUnit.Framework.Assert.IsNull(slow.Terms("f1"));
			NUnit.Framework.Assert.IsNull(slow.Terms("f2"));
			NUnit.Framework.Assert.IsNotNull(slow.Terms("f3"));
			NUnit.Framework.Assert.IsNotNull(slow.Terms("f4"));
			pr.Close();
			// no main readers
			try
			{
				new ParallelCompositeReader(true, new CompositeReader[0], new CompositeReader[] { 
					ir1 });
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
		public virtual void TestToString()
		{
			Directory dir1 = GetDir1(Random());
			CompositeReader ir1 = DirectoryReader.Open(dir1);
			ParallelCompositeReader pr = new ParallelCompositeReader(new CompositeReader[] { 
				ir1 });
			string s = pr.ToString();
			NUnit.Framework.Assert.IsTrue("toString incorrect: " + s, s.StartsWith("ParallelCompositeReader(ParallelAtomicReader("
				));
			pr.Close();
			dir1.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestToStringCompositeComposite()
		{
			Directory dir1 = GetDir1(Random());
			CompositeReader ir1 = DirectoryReader.Open(dir1);
			ParallelCompositeReader pr = new ParallelCompositeReader(new CompositeReader[] { 
				new MultiReader(ir1) });
			string s = pr.ToString();
			NUnit.Framework.Assert.IsTrue("toString incorrect: " + s, s.StartsWith("ParallelCompositeReader(ParallelCompositeReader(ParallelAtomicReader("
				));
			pr.Close();
			dir1.Close();
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
		private IndexSearcher Single(Random random, bool compositeComposite)
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
			Lucene.Net.Document.Document d3 = new Lucene.Net.Document.Document(
				);
			d3.Add(NewTextField("f1", "v3", Field.Store.YES));
			d3.Add(NewTextField("f2", "v3", Field.Store.YES));
			d3.Add(NewTextField("f3", "v3", Field.Store.YES));
			d3.Add(NewTextField("f4", "v3", Field.Store.YES));
			w.AddDocument(d3);
			Lucene.Net.Document.Document d4 = new Lucene.Net.Document.Document(
				);
			d4.Add(NewTextField("f1", "v4", Field.Store.YES));
			d4.Add(NewTextField("f2", "v4", Field.Store.YES));
			d4.Add(NewTextField("f3", "v4", Field.Store.YES));
			d4.Add(NewTextField("f4", "v4", Field.Store.YES));
			w.AddDocument(d4);
			w.Close();
			CompositeReader ir;
			if (compositeComposite)
			{
				ir = new MultiReader(DirectoryReader.Open(dir), DirectoryReader.Open(dir));
			}
			else
			{
				ir = DirectoryReader.Open(dir);
			}
			return NewSearcher(ir);
		}

		// Fields 1 & 2 in one index, 3 & 4 in other, with ParallelReader:
		/// <exception cref="System.IO.IOException"></exception>
		private IndexSearcher Parallel(Random random, bool compositeComposite)
		{
			dir1 = GetDir1(random);
			dir2 = GetDir2(random);
			CompositeReader rd1;
			CompositeReader rd2;
			if (compositeComposite)
			{
				rd1 = new MultiReader(DirectoryReader.Open(dir1), DirectoryReader.Open(dir1));
				rd2 = new MultiReader(DirectoryReader.Open(dir2), DirectoryReader.Open(dir2));
				NUnit.Framework.Assert.AreEqual(2, ((CompositeReaderContext)rd1.GetContext()).Children
					().Count);
				NUnit.Framework.Assert.AreEqual(2, ((CompositeReaderContext)rd2.GetContext()).Children
					().Count);
			}
			else
			{
				rd1 = DirectoryReader.Open(dir1);
				rd2 = DirectoryReader.Open(dir2);
				NUnit.Framework.Assert.AreEqual(3, ((CompositeReaderContext)rd1.GetContext()).Children
					().Count);
				NUnit.Framework.Assert.AreEqual(3, ((CompositeReaderContext)rd2.GetContext()).Children
					().Count);
			}
			ParallelCompositeReader pr = new ParallelCompositeReader(rd1, rd2);
			return NewSearcher(pr);
		}

		// subreader structure: (1,2,1) 
		/// <exception cref="System.IO.IOException"></exception>
		private Directory GetDir1(Random random)
		{
			Directory dir1 = NewDirectory();
			IndexWriter w1 = new IndexWriter(dir1, NewIndexWriterConfig(TEST_VERSION_CURRENT, 
				new MockAnalyzer(random)).SetMergePolicy(NoMergePolicy.NO_COMPOUND_FILES));
			Lucene.Net.Document.Document d1 = new Lucene.Net.Document.Document(
				);
			d1.Add(NewTextField("f1", "v1", Field.Store.YES));
			d1.Add(NewTextField("f2", "v1", Field.Store.YES));
			w1.AddDocument(d1);
			w1.Commit();
			Lucene.Net.Document.Document d2 = new Lucene.Net.Document.Document(
				);
			d2.Add(NewTextField("f1", "v2", Field.Store.YES));
			d2.Add(NewTextField("f2", "v2", Field.Store.YES));
			w1.AddDocument(d2);
			Lucene.Net.Document.Document d3 = new Lucene.Net.Document.Document(
				);
			d3.Add(NewTextField("f1", "v3", Field.Store.YES));
			d3.Add(NewTextField("f2", "v3", Field.Store.YES));
			w1.AddDocument(d3);
			w1.Commit();
			Lucene.Net.Document.Document d4 = new Lucene.Net.Document.Document(
				);
			d4.Add(NewTextField("f1", "v4", Field.Store.YES));
			d4.Add(NewTextField("f2", "v4", Field.Store.YES));
			w1.AddDocument(d4);
			w1.Close();
			return dir1;
		}

		// subreader structure: (1,2,1) 
		/// <exception cref="System.IO.IOException"></exception>
		private Directory GetDir2(Random random)
		{
			Directory dir2 = NewDirectory();
			IndexWriter w2 = new IndexWriter(dir2, NewIndexWriterConfig(TEST_VERSION_CURRENT, 
				new MockAnalyzer(random)).SetMergePolicy(NoMergePolicy.NO_COMPOUND_FILES));
			Lucene.Net.Document.Document d1 = new Lucene.Net.Document.Document(
				);
			d1.Add(NewTextField("f3", "v1", Field.Store.YES));
			d1.Add(NewTextField("f4", "v1", Field.Store.YES));
			w2.AddDocument(d1);
			w2.Commit();
			Lucene.Net.Document.Document d2 = new Lucene.Net.Document.Document(
				);
			d2.Add(NewTextField("f3", "v2", Field.Store.YES));
			d2.Add(NewTextField("f4", "v2", Field.Store.YES));
			w2.AddDocument(d2);
			Lucene.Net.Document.Document d3 = new Lucene.Net.Document.Document(
				);
			d3.Add(NewTextField("f3", "v3", Field.Store.YES));
			d3.Add(NewTextField("f4", "v3", Field.Store.YES));
			w2.AddDocument(d3);
			w2.Commit();
			Lucene.Net.Document.Document d4 = new Lucene.Net.Document.Document(
				);
			d4.Add(NewTextField("f3", "v4", Field.Store.YES));
			d4.Add(NewTextField("f4", "v4", Field.Store.YES));
			w2.AddDocument(d4);
			w2.Close();
			return dir2;
		}

		// this dir has a different subreader structure (1,1,2);
		/// <exception cref="System.IO.IOException"></exception>
		private Directory GetInvalidStructuredDir2(Random random)
		{
			Directory dir2 = NewDirectory();
			IndexWriter w2 = new IndexWriter(dir2, NewIndexWriterConfig(TEST_VERSION_CURRENT, 
				new MockAnalyzer(random)).SetMergePolicy(NoMergePolicy.NO_COMPOUND_FILES));
			Lucene.Net.Document.Document d1 = new Lucene.Net.Document.Document(
				);
			d1.Add(NewTextField("f3", "v1", Field.Store.YES));
			d1.Add(NewTextField("f4", "v1", Field.Store.YES));
			w2.AddDocument(d1);
			w2.Commit();
			Lucene.Net.Document.Document d2 = new Lucene.Net.Document.Document(
				);
			d2.Add(NewTextField("f3", "v2", Field.Store.YES));
			d2.Add(NewTextField("f4", "v2", Field.Store.YES));
			w2.AddDocument(d2);
			w2.Commit();
			Lucene.Net.Document.Document d3 = new Lucene.Net.Document.Document(
				);
			d3.Add(NewTextField("f3", "v3", Field.Store.YES));
			d3.Add(NewTextField("f4", "v3", Field.Store.YES));
			w2.AddDocument(d3);
			Lucene.Net.Document.Document d4 = new Lucene.Net.Document.Document(
				);
			d4.Add(NewTextField("f3", "v4", Field.Store.YES));
			d4.Add(NewTextField("f4", "v4", Field.Store.YES));
			w2.AddDocument(d4);
			w2.Close();
			return dir2;
		}
	}
}
