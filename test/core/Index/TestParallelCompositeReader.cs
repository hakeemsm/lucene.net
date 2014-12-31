using System;
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
	public class TestParallelCompositeReader : LuceneTestCase
	{
		private IndexSearcher parallel;

		private IndexSearcher single;

		private Directory dir;

		private Directory dir1;

		private Directory dir2;

		[Test]
		public virtual void TestQueries()
		{
			single = Single(Random(), false);
			parallel = Parallel(Random(), false);
			Queries();
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

		[Test]
		public virtual void TestQueriesCompositeComposite()
		{
			single = Single(Random(), true);
			parallel = Parallel(Random(), true);
			Queries();
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
			bq1.Add(new TermQuery(new Term("f1", "v1")), Occur.MUST);
			bq1.Add(new TermQuery(new Term("f4", "v1")), Occur.MUST);
			QueryTest(bq1);
		}

		[Test]
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
			AssertEquals(1, ir1.RefCount);
			AssertEquals(1, ir2.RefCount);
			AssertEquals(1, psub1.RefCount);
			pr.Dispose();
			AssertEquals(0, ir1.RefCount);
			AssertEquals(0, ir2.RefCount);
			AssertEquals(0, psub1.RefCount);
			dir1.Dispose();
			dir2.Dispose();
		}

		[Test]
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
			AssertEquals(2, ir1.RefCount);
			AssertEquals(2, ir2.RefCount);
			AssertEquals("refCount must be 1, as the synthetic reader was created by ParallelCompositeReader"
				, 1, psub1.RefCount);
			pr.Dispose();
			AssertEquals(1, ir1.RefCount);
			AssertEquals(1, ir2.RefCount);
			AssertEquals("refcount must be 0 because parent was closed", 0
				, psub1.RefCount);
			ir1.Dispose();
			ir2.Dispose();
			AssertEquals(0, ir1.RefCount);
			AssertEquals(0, ir2.RefCount);
			AssertEquals("refcount should not change anymore", 0, psub1.RefCount);
			dir1.Dispose();
			dir2.Dispose();
		}

		// closeSubreaders=false
		[Test]
		public virtual void TestReaderClosedListener1()
		{
			Directory dir1 = GetDir1(Random());
			CompositeReader ir1 = DirectoryReader.Open(dir1);
			// with overlapping
			ParallelCompositeReader pr = new ParallelCompositeReader(false, new CompositeReader
				[] { ir1 }, new CompositeReader[] { ir1 });
			int[] listenerClosedCount = new int[1];
			AssertEquals(3, pr.Leaves.Count);
			foreach (AtomicReaderContext cxt in pr.Leaves)
			{
				((AtomicReader)cxt.Reader).AddReaderClosedListener(new AnonReaderClosedListener(listenerClosedCount));
			}
			pr.Dispose();
			ir1.Dispose();
			AssertEquals(3, listenerClosedCount[0]);
			dir1.Dispose();
		}

		private sealed class AnonReaderClosedListener : IndexReader.IReaderClosedListener
		{
			public AnonReaderClosedListener(int[] listenerClosedCount)
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
		[Test]
		public virtual void TestReaderClosedListener2()
		{
			Directory dir1 = GetDir1(Random());
			CompositeReader ir1 = DirectoryReader.Open(dir1);
			// with overlapping
			ParallelCompositeReader pr = new ParallelCompositeReader(true, new CompositeReader
				[] { ir1 }, new CompositeReader[] { ir1 });
			int[] listenerClosedCount = new int[1];
			AssertEquals(3, pr.Leaves.Count);
			foreach (AtomicReaderContext cxt in pr.Leaves)
			{
				((AtomicReader)cxt.Reader).AddReaderClosedListener(new AnonReaderClosedListener2
					(listenerClosedCount));
			}
			pr.Dispose();
			AssertEquals(3, listenerClosedCount[0]);
			dir1.Dispose();
		}

		private sealed class AnonReaderClosedListener2 : IndexReader.IReaderClosedListener
		{
			public AnonReaderClosedListener2(int[] listenerClosedCount)
			{
				this.listenerClosedCount = listenerClosedCount;
			}

			public void OnClose(IndexReader reader)
			{
				listenerClosedCount[0]++;
			}

			private readonly int[] listenerClosedCount;
		}

		[Test]
		public virtual void TestCloseInnerReader()
		{
			Directory dir1 = GetDir1(Random());
			CompositeReader ir1 = DirectoryReader.Open(dir1);
			AssertEquals(1, ir1.GetSequentialSubReaders()[0].RefCount
				);
			// with overlapping
			ParallelCompositeReader pr = new ParallelCompositeReader(true, new CompositeReader
				[] { ir1 }, new CompositeReader[] { ir1 });
			IndexReader psub = pr.GetSequentialSubReaders()[0];
			AssertEquals(1, psub.RefCount);
			ir1.Dispose();
			AssertEquals("refCount of synthetic subreader should be unchanged"
				, 1, psub.RefCount);
			try
			{
				psub.Document(0);
				Fail("Subreader should be already closed because inner reader was closed!"
					);
			}
			catch (AlreadyClosedException)
			{
			}
			// pass
			try
			{
				pr.Document(0);
				Fail("ParallelCompositeReader should be already closed because inner reader was closed!"
					);
			}
			catch (AlreadyClosedException)
			{
			}
			// pass
			// noop:
			pr.Dispose();
			AssertEquals(0, psub.RefCount);
			dir1.Dispose();
		}

		[Test]
		public virtual void TestIncompatibleIndexes1()
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
			DirectoryReader ir1 = DirectoryReader.Open(dir1);
			DirectoryReader ir2 = DirectoryReader.Open(dir2);
			try
			{
				new ParallelCompositeReader(ir1, ir2);
				Fail("didn't get expected exception: indexes don't have same number of documents"
					);
			}
			catch (ArgumentException)
			{
			}
			// expected exception
			try
			{
				new ParallelCompositeReader(Random().NextBoolean(), ir1, ir2);
				Fail("didn't get expected exception: indexes don't have same number of documents"
					);
			}
			catch (ArgumentException)
			{
			}
			// expected exception
			AssertEquals(1, ir1.RefCount);
			AssertEquals(1, ir2.RefCount);
			ir1.Dispose();
			ir2.Dispose();
			AssertEquals(0, ir1.RefCount);
			AssertEquals(0, ir2.RefCount);
			dir1.Dispose();
			dir2.Dispose();
		}

		[Test]
		public virtual void TestIncompatibleIndexes2()
		{
			Directory dir1 = GetDir1(Random());
			Directory dir2 = GetInvalidStructuredDir2(Random());
			DirectoryReader ir1 = DirectoryReader.Open(dir1);
			DirectoryReader ir2 = DirectoryReader.Open(dir2);
			CompositeReader[] readers = { ir1, ir2 };
			try
			{
				new ParallelCompositeReader(readers);
				Fail("didn't get expected exception: indexes don't have same subreader structure"
					);
			}
			catch (ArgumentException)
			{
			}
			// expected exception
			try
			{
				new ParallelCompositeReader(Random().NextBoolean(), readers, readers);
				Fail("didn't get expected exception: indexes don't have same subreader structure"
					);
			}
			catch (ArgumentException)
			{
			}
			// expected exception
			AssertEquals(1, ir1.RefCount);
			AssertEquals(1, ir2.RefCount);
			ir1.Dispose();
			ir2.Dispose();
			AssertEquals(0, ir1.RefCount);
			AssertEquals(0, ir2.RefCount);
			dir1.Dispose();
			dir2.Dispose();
		}

		[Test]
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
				Fail("didn't get expected exception: indexes don't have same subreader structure"
					);
			}
			catch (ArgumentException)
			{
			}
			// expected exception
			try
			{
				new ParallelCompositeReader(Random().NextBoolean(), readers, readers);
				Fail("didn't get expected exception: indexes don't have same subreader structure"
					);
			}
			catch (ArgumentException)
			{
			}
			// expected exception
			AssertEquals(1, ir1.RefCount);
			AssertEquals(1, ir2.RefCount);
			ir1.Dispose();
			ir2.Dispose();
			AssertEquals(0, ir1.RefCount);
			AssertEquals(0, ir2.RefCount);
			dir1.Dispose();
			dir2.Dispose();
		}

		[Test]
		public virtual void TestIgnoreStoredFields()
		{
			Directory dir1 = GetDir1(Random());
			Directory dir2 = GetDir2(Random());
			CompositeReader ir1 = DirectoryReader.Open(dir1);
			CompositeReader ir2 = DirectoryReader.Open(dir2);
			// with overlapping
			ParallelCompositeReader pr = new ParallelCompositeReader(false, new CompositeReader
				[] { ir1, ir2 }, new CompositeReader[] { ir1 });
			AssertEquals("v1", pr.Document(0).Get("f1"));
			AssertEquals("v1", pr.Document(0).Get("f2"));
			IsNull(pr.Document(0).Get("f3"));
			IsNull(pr.Document(0).Get("f4"));
			// check that fields are there
			AtomicReader slow = SlowCompositeReaderWrapper.Wrap(pr);
			IsNotNull(slow.Terms("f1"));
			IsNotNull(slow.Terms("f2"));
			IsNotNull(slow.Terms("f3"));
			IsNotNull(slow.Terms("f4"));
			pr.Dispose();
			// no stored fields at all
			pr = new ParallelCompositeReader(false, new CompositeReader[] { ir2 }, new CompositeReader
				[0]);
			IsNull(pr.Document(0).Get("f1"));
			IsNull(pr.Document(0).Get("f2"));
			IsNull(pr.Document(0).Get("f3"));
			IsNull(pr.Document(0).Get("f4"));
			// check that fields are there
			slow = SlowCompositeReaderWrapper.Wrap(pr);
			IsNull(slow.Terms("f1"));
			IsNull(slow.Terms("f2"));
			IsNotNull(slow.Terms("f3"));
			IsNotNull(slow.Terms("f4"));
			pr.Dispose();
			// without overlapping
			pr = new ParallelCompositeReader(true, new CompositeReader[] { ir2 }, new CompositeReader
				[] { ir1 });
			AssertEquals("v1", pr.Document(0).Get("f1"));
			AssertEquals("v1", pr.Document(0).Get("f2"));
			IsNull(pr.Document(0).Get("f3"));
			IsNull(pr.Document(0).Get("f4"));
			// check that fields are there
			slow = SlowCompositeReaderWrapper.Wrap(pr);
			IsNull(slow.Terms("f1"));
			IsNull(slow.Terms("f2"));
			IsNotNull(slow.Terms("f3"));
			IsNotNull(slow.Terms("f4"));
			pr.Dispose();
			// no main readers
			try
			{
				new ParallelCompositeReader(true, new CompositeReader[0], new CompositeReader[] { 
					ir1 });
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

		[Test]
		public virtual void TestToString()
		{
			Directory dir1 = GetDir1(Random());
			CompositeReader ir1 = DirectoryReader.Open(dir1);
			ParallelCompositeReader pr = new ParallelCompositeReader(new CompositeReader[] { 
				ir1 });
			string s = pr.ToString();
			AssertTrue("toString incorrect: " + s, s.StartsWith("ParallelCompositeReader(ParallelAtomicReader("
				));
			pr.Dispose();
			dir1.Dispose();
		}

		[Test]
		public virtual void TestToStringCompositeComposite()
		{
			Directory dir1 = GetDir1(Random());
			CompositeReader ir1 = DirectoryReader.Open(dir1);
			ParallelCompositeReader pr = new ParallelCompositeReader(new CompositeReader[] { 
				new MultiReader(ir1) });
			string s = pr.ToString();
			AssertTrue("toString incorrect: " + s, s.StartsWith("ParallelCompositeReader(ParallelCompositeReader(ParallelAtomicReader("
				));
			pr.Dispose();
			dir1.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void QueryTest(Query query)
		{
			ScoreDoc[] parallelHits = parallel.Search(query, null, 1000).ScoreDocs;
			ScoreDoc[] singleHits = single.Search(query, null, 1000).ScoreDocs;
			AssertEquals(parallelHits.Length, singleHits.Length);
			for (int i = 0; i < parallelHits.Length; i++)
			{
				AssertEquals(parallelHits[i].Score, singleHits[i].Score, 0.001f
					);
				Lucene.Net.Documents.Document docParallel = parallel.Doc(parallelHits[i].Doc
					);
				Lucene.Net.Documents.Document docSingle = single.Doc(singleHits[i].Doc);
				AssertEquals(docParallel.Get("f1"), docSingle.Get("f1"));
				AssertEquals(docParallel.Get("f2"), docSingle.Get("f2"));
				AssertEquals(docParallel.Get("f3"), docSingle.Get("f3"));
				AssertEquals(docParallel.Get("f4"), docSingle.Get("f4"));
			}
		}

		// Fields 1-4 indexed together:
		/// <exception cref="System.IO.IOException"></exception>
		private IndexSearcher Single(Random random, bool compositeComposite)
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
			Lucene.Net.Documents.Document d3 = new Lucene.Net.Documents.Document(
				);
			d3.Add(NewTextField("f1", "v3", Field.Store.YES));
			d3.Add(NewTextField("f2", "v3", Field.Store.YES));
			d3.Add(NewTextField("f3", "v3", Field.Store.YES));
			d3.Add(NewTextField("f4", "v3", Field.Store.YES));
			w.AddDocument(d3);
			Lucene.Net.Documents.Document d4 = new Lucene.Net.Documents.Document(
				);
			d4.Add(NewTextField("f1", "v4", Field.Store.YES));
			d4.Add(NewTextField("f2", "v4", Field.Store.YES));
			d4.Add(NewTextField("f3", "v4", Field.Store.YES));
			d4.Add(NewTextField("f4", "v4", Field.Store.YES));
			w.AddDocument(d4);
			w.Dispose();
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
				AssertEquals(2, rd1.Context.Children.Count);
				AssertEquals(2, rd2.Context.Children.Count);
			}
			else
			{
				rd1 = DirectoryReader.Open(dir1);
				rd2 = DirectoryReader.Open(dir2);
				AssertEquals(3, rd1.Context.Children.Count);
				AssertEquals(3, rd2.Context.Children.Count);
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
			Lucene.Net.Documents.Document d1 = new Lucene.Net.Documents.Document
			{
			    NewTextField("f1", "v1", Field.Store.YES),
			    NewTextField("f2", "v1", Field.Store.YES)
			};
		    w1.AddDocument(d1);
			w1.Commit();
			Lucene.Net.Documents.Document d2 = new Lucene.Net.Documents.Document
			{
			    NewTextField("f1", "v2", Field.Store.YES),
			    NewTextField("f2", "v2", Field.Store.YES)
			};
		    w1.AddDocument(d2);
			Lucene.Net.Documents.Document d3 = new Lucene.Net.Documents.Document
			{
			    NewTextField("f1", "v3", Field.Store.YES),
			    NewTextField("f2", "v3", Field.Store.YES)
			};
		    w1.AddDocument(d3);
			w1.Commit();
			Lucene.Net.Documents.Document d4 = new Lucene.Net.Documents.Document(
				);
			d4.Add(NewTextField("f1", "v4", Field.Store.YES));
			d4.Add(NewTextField("f2", "v4", Field.Store.YES));
			w1.AddDocument(d4);
			w1.Dispose();
			return dir1;
		}

		// subreader structure: (1,2,1) 
		/// <exception cref="System.IO.IOException"></exception>
		private Directory GetDir2(Random random)
		{
			Directory dir2 = NewDirectory();
			IndexWriter w2 = new IndexWriter(dir2, NewIndexWriterConfig(TEST_VERSION_CURRENT, 
				new MockAnalyzer(random)).SetMergePolicy(NoMergePolicy.NO_COMPOUND_FILES));
			Lucene.Net.Documents.Document d1 = new Lucene.Net.Documents.Document
			{
			    NewTextField("f3", "v1", Field.Store.YES),
			    NewTextField("f4", "v1", Field.Store.YES)
			};
		    w2.AddDocument(d1);
			w2.Commit();
			Lucene.Net.Documents.Document d2 = new Lucene.Net.Documents.Document
			{
			    NewTextField("f3", "v2", Field.Store.YES),
			    NewTextField("f4", "v2", Field.Store.YES)
			};
		    w2.AddDocument(d2);
			Lucene.Net.Documents.Document d3 = new Lucene.Net.Documents.Document
			{
			    NewTextField("f3", "v3", Field.Store.YES),
			    NewTextField("f4", "v3", Field.Store.YES)
			};
		    w2.AddDocument(d3);
			w2.Commit();
			Lucene.Net.Documents.Document d4 = new Lucene.Net.Documents.Document
			{
			    NewTextField("f3", "v4", Field.Store.YES),
			    NewTextField("f4", "v4", Field.Store.YES)
			};
		    w2.AddDocument(d4);
			w2.Dispose();
			return dir2;
		}

		// this dir has a different subreader structure (1,1,2);
		/// <exception cref="System.IO.IOException"></exception>
		private Directory GetInvalidStructuredDir2(Random random)
		{
			Directory dir2 = NewDirectory();
			IndexWriter w2 = new IndexWriter(dir2, NewIndexWriterConfig(TEST_VERSION_CURRENT, 
				new MockAnalyzer(random)).SetMergePolicy(NoMergePolicy.NO_COMPOUND_FILES));
			Lucene.Net.Documents.Document d1 = new Lucene.Net.Documents.Document
			{
			    NewTextField("f3", "v1", Field.Store.YES),
			    NewTextField("f4", "v1", Field.Store.YES)
			};
		    w2.AddDocument(d1);
			w2.Commit();
			Lucene.Net.Documents.Document d2 = new Lucene.Net.Documents.Document
			{
			    NewTextField("f3", "v2", Field.Store.YES),
			    NewTextField("f4", "v2", Field.Store.YES)
			};
		    w2.AddDocument(d2);
			w2.Commit();
			Lucene.Net.Documents.Document d3 = new Lucene.Net.Documents.Document
			{
			    NewTextField("f3", "v3", Field.Store.YES),
			    NewTextField("f4", "v3", Field.Store.YES)
			};
		    w2.AddDocument(d3);
			Lucene.Net.Documents.Document d4 = new Lucene.Net.Documents.Document
			{
			    NewTextField("f3", "v4", Field.Store.YES),
			    NewTextField("f4", "v4", Field.Store.YES)
			};
		    w2.AddDocument(d4);
			w2.Dispose();
			return dir2;
		}
	}
}
