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
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			Field idField = new StringField("id", string.Empty, Field.Store.NO);
			doc.Add(idField);
			// add 500 docs with id 0..499
			for (int i = 0; i < 500; i++)
			{
				idField.SetStringValue(Sharpen.Extensions.ToString(i));
				iw.AddDocument(doc);
			}
			// delete 20 of them
			for (int i_1 = 0; i_1 < 20; i_1++)
			{
				iw.DeleteDocuments(new Term("id", Sharpen.Extensions.ToString(Random().Next(iw.MaxDoc
					()))));
			}
			ir = iw.GetReader();
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
			TopDocs hits1 = @is.Search(query, f1, ir.MaxDoc());
			TopDocs hits2 = @is.Search(query, f2, ir.MaxDoc());
			NUnit.Framework.Assert.AreEqual(hits1.totalHits, hits2.totalHits);
			CheckHits.CheckEqual(query, hits1.scoreDocs, hits2.scoreDocs);
			// now do it again to confirm caching works
			TopDocs hits3 = @is.Search(query, f1, ir.MaxDoc());
			TopDocs hits4 = @is.Search(query, f2, ir.MaxDoc());
			NUnit.Framework.Assert.AreEqual(hits3.totalHits, hits4.totalHits);
			CheckHits.CheckEqual(query, hits3.scoreDocs, hits4.scoreDocs);
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
				int id = Random().Next(ir.MaxDoc());
				Query query = new TermQuery(new Term("id", Sharpen.Extensions.ToString(id)));
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
				int id_start = Random().Next(ir.MaxDoc() - 1);
				int id_end = id_start + 1;
				Query query = TermRangeQuery.NewStringRange("id", Sharpen.Extensions.ToString(id_start
					), Sharpen.Extensions.ToString(id_end), true, true);
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
			writer.Close();
			IndexReader reader = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(dir));
			AtomicReaderContext context = (AtomicReaderContext)reader.GetContext();
			MockFilter filter = new MockFilter();
			CachingWrapperFilter cacher = new CachingWrapperFilter(filter);
			// first time, nested filter is called
			DocIdSet strongRef = cacher.GetDocIdSet(context, ((AtomicReader)context.Reader())
				.GetLiveDocs());
			NUnit.Framework.Assert.IsTrue("first time", filter.WasCalled());
			// make sure no exception if cache is holding the wrong docIdSet
			cacher.GetDocIdSet(context, ((AtomicReader)context.Reader()).GetLiveDocs());
			// second time, nested filter should not be called
			filter.Clear();
			cacher.GetDocIdSet(context, ((AtomicReader)context.Reader()).GetLiveDocs());
			NUnit.Framework.Assert.IsFalse("second time", filter.WasCalled());
			reader.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestNullDocIdSet()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			writer.Close();
			IndexReader reader = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(dir));
			AtomicReaderContext context = (AtomicReaderContext)reader.GetContext();
			Filter filter = new _Filter_177();
			CachingWrapperFilter cacher = new CachingWrapperFilter(filter);
			// the caching filter should return the empty set constant
			NUnit.Framework.Assert.IsNull(cacher.GetDocIdSet(context, ((AtomicReader)context.
				Reader()).GetLiveDocs()));
			reader.Close();
			dir.Close();
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
			writer.Close();
			IndexReader reader = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(dir));
			AtomicReaderContext context = (AtomicReaderContext)reader.GetContext();
			Filter filter = new _Filter_200();
			CachingWrapperFilter cacher = new CachingWrapperFilter(filter);
			// the caching filter should return the empty set constant
			NUnit.Framework.Assert.IsNull(cacher.GetDocIdSet(context, ((AtomicReader)context.
				Reader()).GetLiveDocs()));
			reader.Close();
			dir.Close();
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

				public override DocIdSetIterator Iterator()
				{
					return null;
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private static void AssertDocIdSetCacheable(IndexReader reader, Filter filter, bool
			 shouldCacheable)
		{
			NUnit.Framework.Assert.IsTrue(reader.GetContext() is AtomicReaderContext);
			AtomicReaderContext context = (AtomicReaderContext)reader.GetContext();
			CachingWrapperFilter cacher = new CachingWrapperFilter(filter);
			DocIdSet originalSet = filter.GetDocIdSet(context, ((AtomicReader)context.Reader(
				)).GetLiveDocs());
			DocIdSet cachedSet = cacher.GetDocIdSet(context, ((AtomicReader)context.Reader())
				.GetLiveDocs());
			if (originalSet == null)
			{
				NUnit.Framework.Assert.IsNull(cachedSet);
			}
			if (cachedSet == null)
			{
				NUnit.Framework.Assert.IsTrue(originalSet == null || originalSet.Iterator() == null
					);
			}
			else
			{
				NUnit.Framework.Assert.IsTrue(cachedSet.IsCacheable());
				NUnit.Framework.Assert.AreEqual(shouldCacheable, originalSet.IsCacheable());
				//System.out.println("Original: "+originalSet.getClass().getName()+" -- cached: "+cachedSet.getClass().getName());
				if (originalSet.IsCacheable())
				{
					NUnit.Framework.Assert.AreEqual("Cached DocIdSet must be of same class like uncached, if cacheable"
						, originalSet.GetType(), cachedSet.GetType());
				}
				else
				{
					NUnit.Framework.Assert.IsTrue("Cached DocIdSet must be an FixedBitSet if the original one was not cacheable"
						, cachedSet is FixedBitSet || cachedSet == null);
				}
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestIsCacheAble()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			writer.AddDocument(new Lucene.Net.Document.Document());
			writer.Close();
			IndexReader reader = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(dir));
			// not cacheable:
			AssertDocIdSetCacheable(reader, new QueryWrapperFilter(new TermQuery(new Term("test"
				, "value"))), false);
			// returns default empty docidset, always cacheable:
			AssertDocIdSetCacheable(reader, NumericRangeFilter.NewIntRange("test", Sharpen.Extensions.ValueOf
				(10000), Sharpen.Extensions.ValueOf(-10000), true, true), true);
			// is cacheable:
			AssertDocIdSetCacheable(reader, FieldCacheRangeFilter.NewIntRange("test", Sharpen.Extensions.ValueOf
				(10), Sharpen.Extensions.ValueOf(20), true, true), true);
			// a fixedbitset filter is always cacheable
			AssertDocIdSetCacheable(reader, new _Filter_258(), true);
			reader.Close();
			dir.Close();
		}

		private sealed class _Filter_258 : Filter
		{
			public _Filter_258()
			{
			}

			public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs
				)
			{
				return new FixedBitSet(((AtomicReader)context.Reader()).MaxDoc());
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
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			doc.Add(NewStringField("id", "1", Field.Store.YES));
			writer.AddDocument(doc);
			reader = RefreshReader(reader);
			searcher = NewSearcher(reader, false);
			TopDocs docs = searcher.Search(new MatchAllDocsQuery(), 1);
			NUnit.Framework.Assert.AreEqual("Should find a hit...", 1, docs.totalHits);
			Filter startFilter = new QueryWrapperFilter(new TermQuery(new Term("id", "1")));
			CachingWrapperFilter filter = new CachingWrapperFilter(startFilter);
			docs = searcher.Search(new MatchAllDocsQuery(), filter, 1);
			NUnit.Framework.Assert.IsTrue(filter.SizeInBytes() > 0);
			NUnit.Framework.Assert.AreEqual("[query + filter] Should find a hit...", 1, docs.
				totalHits);
			Query constantScore = new ConstantScoreQuery(filter);
			docs = searcher.Search(constantScore, 1);
			NUnit.Framework.Assert.AreEqual("[just filter] Should find a hit...", 1, docs.totalHits
				);
			// make sure we get a cache hit when we reopen reader
			// that had no change to deletions
			// fake delete (deletes nothing):
			writer.DeleteDocuments(new Term("foo", "bar"));
			IndexReader oldReader = reader;
			reader = RefreshReader(reader);
			NUnit.Framework.Assert.IsTrue(reader == oldReader);
			int missCount = filter.missCount;
			docs = searcher.Search(constantScore, 1);
			NUnit.Framework.Assert.AreEqual("[just filter] Should find a hit...", 1, docs.totalHits
				);
			// cache hit:
			NUnit.Framework.Assert.AreEqual(missCount, filter.missCount);
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
			NUnit.Framework.Assert.AreEqual("[query + filter] Should *not* find a hit...", 0, 
				docs.totalHits);
			// cache hit
			NUnit.Framework.Assert.AreEqual(missCount, filter.missCount);
			docs = searcher.Search(constantScore, 1);
			NUnit.Framework.Assert.AreEqual("[just filter] Should *not* find a hit...", 0, docs
				.totalHits);
			// apply deletes dynamically:
			filter = new CachingWrapperFilter(startFilter);
			writer.AddDocument(doc);
			reader = RefreshReader(reader);
			searcher = NewSearcher(reader, false);
			docs = searcher.Search(new MatchAllDocsQuery(), filter, 1);
			NUnit.Framework.Assert.AreEqual("[query + filter] Should find a hit...", 1, docs.
				totalHits);
			missCount = filter.missCount;
			NUnit.Framework.Assert.IsTrue(missCount > 0);
			constantScore = new ConstantScoreQuery(filter);
			docs = searcher.Search(constantScore, 1);
			NUnit.Framework.Assert.AreEqual("[just filter] Should find a hit...", 1, docs.totalHits
				);
			NUnit.Framework.Assert.AreEqual(missCount, filter.missCount);
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
			NUnit.Framework.Assert.AreEqual("[query + filter] Should find 2 hits...", 2, docs
				.totalHits);
			NUnit.Framework.Assert.IsTrue(filter.missCount > missCount);
			missCount = filter.missCount;
			constantScore = new ConstantScoreQuery(filter);
			docs = searcher.Search(constantScore, 1);
			NUnit.Framework.Assert.AreEqual("[just filter] Should find a hit...", 2, docs.totalHits
				);
			NUnit.Framework.Assert.AreEqual(missCount, filter.missCount);
			// now delete the doc, refresh the reader, and see that it's not there
			writer.DeleteDocuments(new Term("id", "1"));
			reader = RefreshReader(reader);
			searcher = NewSearcher(reader, false);
			docs = searcher.Search(new MatchAllDocsQuery(), filter, 1);
			NUnit.Framework.Assert.AreEqual("[query + filter] Should *not* find a hit...", 0, 
				docs.totalHits);
			// CWF reused the same entry (it dynamically applied the deletes):
			NUnit.Framework.Assert.AreEqual(missCount, filter.missCount);
			docs = searcher.Search(constantScore, 1);
			NUnit.Framework.Assert.AreEqual("[just filter] Should *not* find a hit...", 0, docs
				.totalHits);
			// CWF reused the same entry (it dynamically applied the deletes):
			NUnit.Framework.Assert.AreEqual(missCount, filter.missCount);
			// NOTE: silliness to make sure JRE does not eliminate
			// our holding onto oldReader to prevent
			// CachingWrapperFilter's WeakHashMap from dropping the
			// entry:
			NUnit.Framework.Assert.IsTrue(oldReader != null);
			reader.Close();
			writer.Close();
			dir.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private static DirectoryReader RefreshReader(DirectoryReader reader)
		{
			DirectoryReader oldReader = reader;
			reader = DirectoryReader.OpenIfChanged(reader);
			if (reader != null)
			{
				oldReader.Close();
				return reader;
			}
			else
			{
				return oldReader;
			}
		}
	}
}
