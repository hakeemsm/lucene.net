/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;


namespace Lucene.Net.Search
{
	public class TestCachingWrapperFilter : LuceneTestCase
	{
		internal Directory dir;

		internal DirectoryReader ir;

		internal IndexSearcher @is;

		internal RandomIndexWriter iw;

		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			dir = NewDirectory();
			iw = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			Field idField = new StringField("id", string.Empty, Field.Store.NO);
			doc.Add(idField);
			// add 500 docs with id 0..499
			for (int i = 0; i < 500; i++)
			{
				idField.StringValue = i.ToString());
				iw.AddDocument(doc);
			}
			// delete 20 of them
			for (int i_1 = 0; i_1 < 20; i_1++)
			{
				iw.DeleteDocuments(new Term("id", Extensions.ToString(Random().Next(iw.MaxDoc
					()))));
			}
			ir = iw.Reader;
			@is = NewSearcher(ir);
		}

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			IOUtils.Close(iw, ir, dir);
			base.TearDown();
		}

		/// <exception cref="System.Exception"></exception>
		private void AssertFilterEquals(Filter f1, Filter f2)
		{
			Query query = new MatchAllDocsQuery();
			TopDocs hits1 = @is.Search(query, f1, ir.MaxDoc);
			TopDocs hits2 = @is.Search(query, f2, ir.MaxDoc);
			AreEqual(hits1.TotalHits, hits2.TotalHits);
			CheckHits.CheckEqual(query, hits1.ScoreDocs, hits2.ScoreDocs);
			// now do it again to confirm caching works
			TopDocs hits3 = @is.Search(query, f1, ir.MaxDoc);
			TopDocs hits4 = @is.Search(query, f2, ir.MaxDoc);
			AreEqual(hits3.TotalHits, hits4.TotalHits);
			CheckHits.CheckEqual(query, hits3.ScoreDocs, hits4.ScoreDocs);
		}

		/// <summary>test null iterator</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestEmpty()
		{
			Query query = new BooleanQuery();
			Filter expected = new QueryWrapperFilter(query);
			Filter actual = new CachingWrapperFilter(expected);
			AssertFilterEquals(expected, actual);
		}

		/// <summary>test iterator returns NO_MORE_DOCS</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestEmpty2()
		{
			BooleanQuery query = new BooleanQuery();
			query.Add(new TermQuery(new Term("id", "0")), BooleanClause.Occur.MUST);
			query.Add(new TermQuery(new Term("id", "0")), BooleanClause.Occur.MUST_NOT);
			Filter expected = new QueryWrapperFilter(query);
			Filter actual = new CachingWrapperFilter(expected);
			AssertFilterEquals(expected, actual);
		}

		/// <summary>test null docidset</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestEmpty3()
		{
			Filter expected = new PrefixFilter(new Term("bogusField", "bogusVal"));
			Filter actual = new CachingWrapperFilter(expected);
			AssertFilterEquals(expected, actual);
		}

		/// <summary>test iterator returns single document</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestSingle()
		{
			for (int i = 0; i < 10; i++)
			{
				int id = Random().Next(ir.MaxDoc);
				Query query = new TermQuery(new Term("id", Extensions.ToString(id)));
				Filter expected = new QueryWrapperFilter(query);
				Filter actual = new CachingWrapperFilter(expected);
				AssertFilterEquals(expected, actual);
			}
		}

		/// <summary>test sparse filters (match single documents)</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestSparse()
		{
			for (int i = 0; i < 10; i++)
			{
				int id_start = Random().Next(ir.MaxDoc - 1);
				int id_end = id_start + 1;
				Query query = TermRangeQuery.NewStringRange("id", Extensions.ToString(id_start
					), Extensions.ToString(id_end), true, true);
				Filter expected = new QueryWrapperFilter(query);
				Filter actual = new CachingWrapperFilter(expected);
				AssertFilterEquals(expected, actual);
			}
		}

		/// <summary>test dense filters (match entire index)</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestDense()
		{
			Query query = new MatchAllDocsQuery();
			Filter expected = new QueryWrapperFilter(query);
			Filter actual = new CachingWrapperFilter(expected);
			AssertFilterEquals(expected, actual);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestCachingWorks()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			writer.Dispose();
			IndexReader reader = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(dir));
			AtomicReaderContext context = (AtomicReaderContext)reader.GetContext();
			MockFilter filter = new MockFilter();
			CachingWrapperFilter cacher = new CachingWrapperFilter(filter);
			// first time, nested filter is called
			DocIdSet strongRef = cacher.GetDocIdSet(context, ((AtomicReader)context.Reader)
				.LiveDocs);
			IsTrue("first time", filter.WasCalled());
			// make sure no exception if cache is holding the wrong docIdSet
			cacher.GetDocIdSet(context, ((AtomicReader)context.Reader).LiveDocs);
			// second time, nested filter should not be called
			filter.Clear();
			cacher.GetDocIdSet(context, ((AtomicReader)context.Reader).LiveDocs);
			IsFalse("second time", filter.WasCalled());
			reader.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestNullDocIdSet()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			writer.Dispose();
			IndexReader reader = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(dir));
			AtomicReaderContext context = (AtomicReaderContext)reader.GetContext();
			Filter filter = new _Filter_177();
			CachingWrapperFilter cacher = new CachingWrapperFilter(filter);
			// the caching filter should return the empty set constant
			IsNull(cacher.GetDocIdSet(context, ((AtomicReader)context.
				Reader()).LiveDocs));
			reader.Dispose();
			dir.Dispose();
		}

		private sealed class _Filter_177 : Filter
		{
			public _Filter_177()
			{
			}

			public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs
				)
			{
				return null;
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestNullDocIdSetIterator()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			writer.Dispose();
			IndexReader reader = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(dir));
			AtomicReaderContext context = (AtomicReaderContext)reader.GetContext();
			Filter filter = new _Filter_200();
			CachingWrapperFilter cacher = new CachingWrapperFilter(filter);
			// the caching filter should return the empty set constant
			IsNull(cacher.GetDocIdSet(context, ((AtomicReader)context.
				Reader()).LiveDocs));
			reader.Dispose();
			dir.Dispose();
		}

		private sealed class _Filter_200 : Filter
		{
			public _Filter_200()
			{
			}

			public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs
				)
			{
				return new _DocIdSet_203();
			}

			private sealed class _DocIdSet_203 : DocIdSet
			{
				public _DocIdSet_203()
				{
				}

				public override DocIdSetIterator IEnumerator()
				{
					return null;
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private static void AssertDocIdSetCacheable(IndexReader reader, Filter filter, bool
			 shouldCacheable)
		{
			IsTrue(reader.GetContext() is AtomicReaderContext);
			AtomicReaderContext context = (AtomicReaderContext)reader.GetContext();
			CachingWrapperFilter cacher = new CachingWrapperFilter(filter);
			DocIdSet originalSet = filter.GetDocIdSet(context, ((AtomicReader)context.Reader(
				)).LiveDocs);
			DocIdSet cachedSet = cacher.GetDocIdSet(context, ((AtomicReader)context.Reader)
				.LiveDocs);
			if (originalSet == null)
			{
				IsNull(cachedSet);
			}
			if (cachedSet == null)
			{
				IsTrue(originalSet == null || originalSet.IEnumerator() == null
					);
			}
			else
			{
				IsTrue(cachedSet.IsCacheable());
				AreEqual(shouldCacheable, originalSet.IsCacheable());
				//System.out.println("Original: "+originalSet.getClass().getName()+" -- cached: "+cachedSet.getClass().getName());
				if (originalSet.IsCacheable())
				{
					AreEqual("Cached DocIdSet must be of same class like uncached, if cacheable"
						, originalSet.GetType(), cachedSet.GetType());
				}
				else
				{
					IsTrue("Cached DocIdSet must be an FixedBitSet if the original one was not cacheable"
						, cachedSet is FixedBitSet || cachedSet == null);
				}
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestIsCacheAble()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			writer.AddDocument(new Lucene.Net.Documents.Document());
			writer.Dispose();
			IndexReader reader = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(dir));
			// not cacheable:
			AssertDocIdSetCacheable(reader, new QueryWrapperFilter(new TermQuery(new Term("test"
				, "value"))), false);
			// returns default empty docidset, always cacheable:
			AssertDocIdSetCacheable(reader, NumericRangeFilter.NewIntRange("test", Extensions.ValueOf
				(10000), Extensions.ValueOf(-10000), true, true), true);
			// is cacheable:
			AssertDocIdSetCacheable(reader, FieldCacheRangeFilter.NewIntRange("test", Extensions.ValueOf
				(10), Extensions.ValueOf(20), true, true), true);
			// a fixedbitset filter is always cacheable
			AssertDocIdSetCacheable(reader, new _Filter_258(), true);
			reader.Dispose();
			dir.Dispose();
		}

		private sealed class _Filter_258 : Filter
		{
			public _Filter_258()
			{
			}

			public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs
				)
			{
				return new FixedBitSet(((AtomicReader)context.Reader).MaxDoc);
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestEnforceDeletions()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMergeScheduler(new SerialMergeScheduler
				()).SetMergePolicy(NewLogMergePolicy(10)));
			// asserts below requires no unexpected merges:
			// NOTE: cannot use writer.getReader because RIW (on
			// flipping a coin) may give us a newly opened reader,
			// but we use .reopen on this reader below and expect to
			// (must) get an NRT reader:
			DirectoryReader reader = DirectoryReader.Open(writer.w, true);
			// same reason we don't wrap?
			IndexSearcher searcher = NewSearcher(reader, false);
			// add a doc, refresh the reader, and check that it's there
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewStringField("id", "1", Field.Store.YES));
			writer.AddDocument(doc);
			reader = RefreshReader(reader);
			searcher = NewSearcher(reader, false);
			TopDocs docs = searcher.Search(new MatchAllDocsQuery(), 1);
			AreEqual("Should find a hit...", 1, docs.TotalHits);
			Filter startFilter = new QueryWrapperFilter(new TermQuery(new Term("id", "1")));
			CachingWrapperFilter filter = new CachingWrapperFilter(startFilter);
			docs = searcher.Search(new MatchAllDocsQuery(), filter, 1);
			IsTrue(filter.SizeInBytes() > 0);
			AreEqual("[query + filter] Should find a hit...", 1, docs.
				TotalHits);
			Query constantScore = new ConstantScoreQuery(filter);
			docs = searcher.Search(constantScore, 1);
			AreEqual("[just filter] Should find a hit...", 1, docs.TotalHits
				);
			// make sure we get a cache hit when we reopen reader
			// that had no change to deletions
			// fake delete (deletes nothing):
			writer.DeleteDocuments(new Term("foo", "bar"));
			IndexReader oldReader = reader;
			reader = RefreshReader(reader);
			IsTrue(reader == oldReader);
			int missCount = filter.missCount;
			docs = searcher.Search(constantScore, 1);
			AreEqual("[just filter] Should find a hit...", 1, docs.TotalHits
				);
			// cache hit:
			AreEqual(missCount, filter.missCount);
			// now delete the doc, refresh the reader, and see that it's not there
			writer.DeleteDocuments(new Term("id", "1"));
			// NOTE: important to hold ref here so GC doesn't clear
			// the cache entry!  Else the 
			//HM:revisit 
			//assert below may sometimes
			// fail:
			oldReader = reader;
			reader = RefreshReader(reader);
			searcher = NewSearcher(reader, false);
			missCount = filter.missCount;
			docs = searcher.Search(new MatchAllDocsQuery(), filter, 1);
			AreEqual("[query + filter] Should *not* find a hit...", 0, 
				docs.TotalHits);
			// cache hit
			AreEqual(missCount, filter.missCount);
			docs = searcher.Search(constantScore, 1);
			AreEqual("[just filter] Should *not* find a hit...", 0, docs
				.TotalHits);
			// apply deletes dynamically:
			filter = new CachingWrapperFilter(startFilter);
			writer.AddDocument(doc);
			reader = RefreshReader(reader);
			searcher = NewSearcher(reader, false);
			docs = searcher.Search(new MatchAllDocsQuery(), filter, 1);
			AreEqual("[query + filter] Should find a hit...", 1, docs.
				TotalHits);
			missCount = filter.missCount;
			IsTrue(missCount > 0);
			constantScore = new ConstantScoreQuery(filter);
			docs = searcher.Search(constantScore, 1);
			AreEqual("[just filter] Should find a hit...", 1, docs.TotalHits
				);
			AreEqual(missCount, filter.missCount);
			writer.AddDocument(doc);
			// NOTE: important to hold ref here so GC doesn't clear
			// the cache entry!  Else the 
			//HM:revisit 
			//assert below may sometimes
			// fail:
			oldReader = reader;
			reader = RefreshReader(reader);
			searcher = NewSearcher(reader, false);
			docs = searcher.Search(new MatchAllDocsQuery(), filter, 1);
			AreEqual("[query + filter] Should find 2 hits...", 2, docs
				.TotalHits);
			IsTrue(filter.missCount > missCount);
			missCount = filter.missCount;
			constantScore = new ConstantScoreQuery(filter);
			docs = searcher.Search(constantScore, 1);
			AreEqual("[just filter] Should find a hit...", 2, docs.TotalHits
				);
			AreEqual(missCount, filter.missCount);
			// now delete the doc, refresh the reader, and see that it's not there
			writer.DeleteDocuments(new Term("id", "1"));
			reader = RefreshReader(reader);
			searcher = NewSearcher(reader, false);
			docs = searcher.Search(new MatchAllDocsQuery(), filter, 1);
			AreEqual("[query + filter] Should *not* find a hit...", 0, 
				docs.TotalHits);
			// CWF reused the same entry (it dynamically applied the deletes):
			AreEqual(missCount, filter.missCount);
			docs = searcher.Search(constantScore, 1);
			AreEqual("[just filter] Should *not* find a hit...", 0, docs
				.TotalHits);
			// CWF reused the same entry (it dynamically applied the deletes):
			AreEqual(missCount, filter.missCount);
			// NOTE: silliness to make sure JRE does not eliminate
			// our holding onto oldReader to prevent
			// CachingWrapperFilter's WeakHashMap from dropping the
			// entry:
			IsTrue(oldReader != null);
			reader.Dispose();
			writer.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private static DirectoryReader RefreshReader(DirectoryReader reader)
		{
			DirectoryReader oldReader = reader;
			reader = DirectoryReader.OpenIfChanged(reader);
			if (reader != null)
			{
				oldReader.Dispose();
				return reader;
			}
			else
			{
				return oldReader;
			}
		}
	}
}
