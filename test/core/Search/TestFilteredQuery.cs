/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using NUnit.Framework;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search
{
	/// <summary>FilteredQuery JUnit tests.</summary>
	/// <remarks>
	/// FilteredQuery JUnit tests.
	/// <p>Created: Apr 21, 2004 1:21:46 PM
	/// </remarks>
	/// <since>1.4</since>
	public class TestFilteredQuery : LuceneTestCase
	{
		private IndexSearcher searcher;

		private IndexReader reader;

		private Directory directory;

		private Query query;

		private Filter filter;

		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			directory = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), directory, NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy
				()));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewTextField("field", "one two three four five", Field.Store.YES));
			doc.Add(NewTextField("sorter", "b", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewTextField("field", "one two three four", Field.Store.YES));
			doc.Add(NewTextField("sorter", "d", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewTextField("field", "one two three y", Field.Store.YES));
			doc.Add(NewTextField("sorter", "a", Field.Store.YES));
			writer.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewTextField("field", "one two x", Field.Store.YES));
			doc.Add(NewTextField("sorter", "c", Field.Store.YES));
			writer.AddDocument(doc);
			// tests here require single segment (eg try seed
			// 8239472272678419952L), because SingleDocTestFilter(x)
			// blindly accepts that docID in any sub-segment
			writer.ForceMerge(1);
			reader = writer.Reader;
			writer.Dispose();
			searcher = NewSearcher(reader);
			query = new TermQuery(new Term("field", "three"));
			filter = NewStaticFilterB();
		}

		// must be static for serialization tests
		private static Filter NewStaticFilterB()
		{
			return new _Filter_100();
		}

		private sealed class _Filter_100 : Filter
		{
			public _Filter_100()
			{
			}

			public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs
				)
			{
				if (acceptDocs == null)
				{
					acceptDocs = new Bits.MatchAllBits(5);
				}
				BitSet bitset = new BitSet(5);
				if (acceptDocs.Get(1))
				{
					bitset.Set(1);
				}
				if (acceptDocs.Get(3))
				{
					bitset.Set(3);
				}
				return new DocIdBitSet(bitset);
			}
		}

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			reader.Dispose();
			directory.Dispose();
			base.TearDown();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestFilteredQuery()
		{
			// force the filter to be executed as bits
			TFilteredQuery(true);
			// force the filter to be executed as iterator
			TFilteredQuery(false);
		}

		/// <exception cref="System.Exception"></exception>
		private void TFilteredQuery(bool useRandomAccess)
		{
			Query filteredquery = new FilteredQuery(query, filter, RandomFilterStrategy(Random
				(), useRandomAccess));
			ScoreDoc[] hits = searcher.Search(filteredquery, null, 1000).ScoreDocs;
			AreEqual(1, hits.Length);
			AreEqual(1, hits[0].Doc);
			QueryUtils.Check(Random(), filteredquery, searcher);
			hits = searcher.Search(filteredquery, null, 1000, new Sort(new SortField("sorter"
				, SortField.Type.STRING))).ScoreDocs;
			AreEqual(1, hits.Length);
			AreEqual(1, hits[0].Doc);
			filteredquery = new FilteredQuery(new TermQuery(new Term("field", "one")), filter
				, RandomFilterStrategy(Random(), useRandomAccess));
			hits = searcher.Search(filteredquery, null, 1000).ScoreDocs;
			AreEqual(2, hits.Length);
			QueryUtils.Check(Random(), filteredquery, searcher);
			filteredquery = new FilteredQuery(new MatchAllDocsQuery(), filter, RandomFilterStrategy
				(Random(), useRandomAccess));
			hits = searcher.Search(filteredquery, null, 1000).ScoreDocs;
			AreEqual(2, hits.Length);
			QueryUtils.Check(Random(), filteredquery, searcher);
			filteredquery = new FilteredQuery(new TermQuery(new Term("field", "x")), filter, 
				RandomFilterStrategy(Random(), useRandomAccess));
			hits = searcher.Search(filteredquery, null, 1000).ScoreDocs;
			AreEqual(1, hits.Length);
			AreEqual(3, hits[0].Doc);
			QueryUtils.Check(Random(), filteredquery, searcher);
			filteredquery = new FilteredQuery(new TermQuery(new Term("field", "y")), filter, 
				RandomFilterStrategy(Random(), useRandomAccess));
			hits = searcher.Search(filteredquery, null, 1000).ScoreDocs;
			AreEqual(0, hits.Length);
			QueryUtils.Check(Random(), filteredquery, searcher);
			// test boost
			Filter f = NewStaticFilterA();
			float boost = 2.5f;
			BooleanQuery bq1 = new BooleanQuery();
			TermQuery tq = new TermQuery(new Term("field", "one"));
			tq.SetBoost(boost);
			bq1.Add(tq, BooleanClause.Occur.MUST);
			bq1.Add(new TermQuery(new Term("field", "five")), BooleanClause.Occur.MUST);
			BooleanQuery bq2 = new BooleanQuery();
			tq = new TermQuery(new Term("field", "one"));
			filteredquery = new FilteredQuery(tq, f, RandomFilterStrategy(Random(), useRandomAccess
				));
			filteredquery.SetBoost(boost);
			bq2.Add(filteredquery, BooleanClause.Occur.MUST);
			bq2.Add(new TermQuery(new Term("field", "five")), BooleanClause.Occur.MUST);
			AssertScoreEquals(bq1, bq2);
			AreEqual(boost, filteredquery.GetBoost(), 0);
			AreEqual(1.0f, tq.GetBoost(), 0);
		}

		// the boost value of the underlying query shouldn't have changed 
		// must be static for serialization tests 
		private static Filter NewStaticFilterA()
		{
			return new _Filter_182();
		}

		private sealed class _Filter_182 : Filter
		{
			public _Filter_182()
			{
			}

			public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs
				)
			{
				IsNull("acceptDocs should be null, as we have an index without deletions"
					, acceptDocs);
				BitSet bitset = new BitSet(5);
				bitset.Set(0, 5);
				return new DocIdBitSet(bitset);
			}
		}

		/// <summary>Tests whether the scores of the two queries are the same.</summary>
		/// <remarks>Tests whether the scores of the two queries are the same.</remarks>
		/// <exception cref="System.Exception"></exception>
		public virtual void AssertScoreEquals(Query q1, Query q2)
		{
			ScoreDoc[] hits1 = searcher.Search(q1, null, 1000).ScoreDocs;
			ScoreDoc[] hits2 = searcher.Search(q2, null, 1000).ScoreDocs;
			AreEqual(hits1.Length, hits2.Length);
			for (int i = 0; i < hits1.Length; i++)
			{
				AreEqual(hits1[i].score, hits2[i].score, 0.000001f);
			}
		}

		/// <summary>This tests FilteredQuery's rewrite correctness</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestRangeQuery()
		{
			// force the filter to be executed as bits
			TRangeQuery(true);
			TRangeQuery(false);
		}

		/// <exception cref="System.Exception"></exception>
		private void TRangeQuery(bool useRandomAccess)
		{
			TermRangeQuery rq = TermRangeQuery.NewStringRange("sorter", "b", "d", true, true);
			Query filteredquery = new FilteredQuery(rq, filter, RandomFilterStrategy(Random()
				, useRandomAccess));
			ScoreDoc[] hits = searcher.Search(filteredquery, null, 1000).ScoreDocs;
			AreEqual(2, hits.Length);
			QueryUtils.Check(Random(), filteredquery, searcher);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestBooleanMUST()
		{
			// force the filter to be executed as bits
			TBooleanMUST(true);
			// force the filter to be executed as iterator
			TBooleanMUST(false);
		}

		/// <exception cref="System.Exception"></exception>
		private void TBooleanMUST(bool useRandomAccess)
		{
			BooleanQuery bq = new BooleanQuery();
			Query query = new FilteredQuery(new TermQuery(new Term("field", "one")), new SingleDocTestFilter
				(0), RandomFilterStrategy(Random(), useRandomAccess));
			bq.Add(query, BooleanClause.Occur.MUST);
			query = new FilteredQuery(new TermQuery(new Term("field", "one")), new SingleDocTestFilter
				(1), RandomFilterStrategy(Random(), useRandomAccess));
			bq.Add(query, BooleanClause.Occur.MUST);
			ScoreDoc[] hits = searcher.Search(bq, null, 1000).ScoreDocs;
			AreEqual(0, hits.Length);
			QueryUtils.Check(Random(), query, searcher);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestBooleanSHOULD()
		{
			// force the filter to be executed as bits
			TBooleanSHOULD(true);
			// force the filter to be executed as iterator
			TBooleanSHOULD(false);
		}

		/// <exception cref="System.Exception"></exception>
		private void TBooleanSHOULD(bool useRandomAccess)
		{
			BooleanQuery bq = new BooleanQuery();
			Query query = new FilteredQuery(new TermQuery(new Term("field", "one")), new SingleDocTestFilter
				(0), RandomFilterStrategy(Random(), useRandomAccess));
			bq.Add(query, BooleanClause.Occur.SHOULD);
			query = new FilteredQuery(new TermQuery(new Term("field", "one")), new SingleDocTestFilter
				(1), RandomFilterStrategy(Random(), useRandomAccess));
			bq.Add(query, BooleanClause.Occur.SHOULD);
			ScoreDoc[] hits = searcher.Search(bq, null, 1000).ScoreDocs;
			AreEqual(2, hits.Length);
			QueryUtils.Check(Random(), query, searcher);
		}

		// Make sure BooleanQuery, which does out-of-order
		// scoring, inside FilteredQuery, works
		/// <exception cref="System.Exception"></exception>
		public virtual void TestBoolean2()
		{
			// force the filter to be executed as bits
			TBoolean2(true);
			// force the filter to be executed as iterator
			TBoolean2(false);
		}

		/// <exception cref="System.Exception"></exception>
		private void TBoolean2(bool useRandomAccess)
		{
			BooleanQuery bq = new BooleanQuery();
			Query query = new FilteredQuery(bq, new SingleDocTestFilter(0), RandomFilterStrategy
				(Random(), useRandomAccess));
			bq.Add(new TermQuery(new Term("field", "one")), BooleanClause.Occur.SHOULD);
			bq.Add(new TermQuery(new Term("field", "two")), BooleanClause.Occur.SHOULD);
			ScoreDoc[] hits = searcher.Search(query, 1000).ScoreDocs;
			AreEqual(1, hits.Length);
			QueryUtils.Check(Random(), query, searcher);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestChainedFilters()
		{
			// force the filter to be executed as bits
			TChainedFilters(true);
			// force the filter to be executed as iterator
			TChainedFilters(false);
		}

		/// <exception cref="System.Exception"></exception>
		private void TChainedFilters(bool useRandomAccess)
		{
			Query query = new FilteredQuery(new FilteredQuery(new MatchAllDocsQuery(), new CachingWrapperFilter
				(new QueryWrapperFilter(new TermQuery(new Term("field", "three")))), RandomFilterStrategy
				(Random(), useRandomAccess)), new CachingWrapperFilter(new QueryWrapperFilter(new 
				TermQuery(new Term("field", "four")))), RandomFilterStrategy(Random(), useRandomAccess
				));
			ScoreDoc[] hits = searcher.Search(query, 10).ScoreDocs;
			AreEqual(2, hits.Length);
			QueryUtils.Check(Random(), query, searcher);
			// one more:
			query = new FilteredQuery(query, new CachingWrapperFilter(new QueryWrapperFilter(
				new TermQuery(new Term("field", "five")))), RandomFilterStrategy(Random(), useRandomAccess
				));
			hits = searcher.Search(query, 10).ScoreDocs;
			AreEqual(1, hits.Length);
			QueryUtils.Check(Random(), query, searcher);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestEqualsHashcode()
		{
			// some tests before, if the used queries and filters work:
			AreEqual(new PrefixFilter(new Term("field", "o")), new PrefixFilter
				(new Term("field", "o")));
			IsFalse(new PrefixFilter(new Term("field", "a")).Equals(new 
				PrefixFilter(new Term("field", "o"))));
			QueryUtils.CheckHashEquals(new TermQuery(new Term("field", "one")));
			QueryUtils.CheckUnequal(new TermQuery(new Term("field", "one")), new TermQuery(new 
				Term("field", "two")));
			// now test FilteredQuery equals/hashcode:
			QueryUtils.CheckHashEquals(new FilteredQuery(new TermQuery(new Term("field", "one"
				)), new PrefixFilter(new Term("field", "o"))));
			QueryUtils.CheckUnequal(new FilteredQuery(new TermQuery(new Term("field", "one"))
				, new PrefixFilter(new Term("field", "o"))), new FilteredQuery(new TermQuery(new 
				Term("field", "two")), new PrefixFilter(new Term("field", "o"))));
			QueryUtils.CheckUnequal(new FilteredQuery(new TermQuery(new Term("field", "one"))
				, new PrefixFilter(new Term("field", "a"))), new FilteredQuery(new TermQuery(new 
				Term("field", "one")), new PrefixFilter(new Term("field", "o"))));
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestInvalidArguments()
		{
			try
			{
				new FilteredQuery(null, null);
				Fail("Should throw IllegalArgumentException");
			}
			catch (ArgumentException)
			{
			}
			// pass
			try
			{
				new FilteredQuery(new TermQuery(new Term("field", "one")), null);
				Fail("Should throw IllegalArgumentException");
			}
			catch (ArgumentException)
			{
			}
			// pass
			try
			{
				new FilteredQuery(null, new PrefixFilter(new Term("field", "o")));
				Fail("Should throw IllegalArgumentException");
			}
			catch (ArgumentException)
			{
			}
		}

		// pass
		private FilteredQuery.FilterStrategy RandomFilterStrategy()
		{
			return RandomFilterStrategy(Random(), true);
		}

		/// <exception cref="System.Exception"></exception>
		private void AssertRewrite<_T0>(FilteredQuery fq, Type<_T0> clazz) where _T0:Query
		{
			// assign crazy boost to FQ
			float boost = Random().NextFloat() * 100.f;
			fq.SetBoost(boost);
			// assign crazy boost to inner
			float innerBoost = Random().NextFloat() * 100.f;
			fq.GetQuery().SetBoost(innerBoost);
			// check the class and boosts of rewritten query
			Query rewritten = searcher.Rewrite(fq);
			IsTrue("is not instance of " + clazz.FullName, clazz.IsInstanceOfType
				(rewritten));
			if (rewritten is FilteredQuery)
			{
				AreEqual(boost, rewritten.GetBoost(), 1.E-5f);
				AreEqual(innerBoost, ((FilteredQuery)rewritten).GetQuery()
					.GetBoost(), 1.E-5f);
				AreEqual(fq.GetFilterStrategy(), ((FilteredQuery)rewritten
					).GetFilterStrategy());
			}
			else
			{
				AreEqual(boost * innerBoost, rewritten.GetBoost(), 1.E-5f);
			}
			// check that the original query was not modified
			AreEqual(boost, fq.GetBoost(), 1.E-5f);
			AreEqual(innerBoost, fq.GetQuery().GetBoost(), 1.E-5f);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestRewrite()
		{
			AssertRewrite(new FilteredQuery(new TermQuery(new Term("field", "one")), new PrefixFilter
				(new Term("field", "o")), RandomFilterStrategy()), typeof(FilteredQuery));
			AssertRewrite(new FilteredQuery(new PrefixQuery(new Term("field", "one")), new PrefixFilter
				(new Term("field", "o")), RandomFilterStrategy()), typeof(FilteredQuery));
		}

		public virtual void TestGetFilterStrategy()
		{
			FilteredQuery.FilterStrategy randomFilterStrategy = RandomFilterStrategy();
			FilteredQuery filteredQuery = new FilteredQuery(new TermQuery(new Term("field", "one"
				)), new PrefixFilter(new Term("field", "o")), randomFilterStrategy);
			AreSame(randomFilterStrategy, filteredQuery.GetFilterStrategy
				());
		}

		private static FilteredQuery.FilterStrategy RandomFilterStrategy(Random random, bool
			 useRandomAccess)
		{
			if (useRandomAccess)
			{
				return new _RandomAccessFilterStrategy_388();
			}
			return TestUtil.RandomFilterStrategy(random);
		}

		private sealed class _RandomAccessFilterStrategy_388 : FilteredQuery.RandomAccessFilterStrategy
		{
			public _RandomAccessFilterStrategy_388()
			{
			}

			protected override bool UseRandomAccess(Bits bits, int firstFilterDoc)
			{
				return true;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestQueryFirstFilterStrategy()
		{
			Directory directory = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), directory, NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
			int numDocs = AtLeast(50);
			int totalDocsWithZero = 0;
			for (int i = 0; i < numDocs; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				int num = Random().Next(5);
				if (num == 0)
				{
					totalDocsWithZero++;
				}
				doc.Add(NewTextField("field", string.Empty + num, Field.Store.YES));
				writer.AddDocument(doc);
			}
			IndexReader reader = writer.Reader;
			writer.Dispose();
			IndexSearcher searcher = NewSearcher(reader);
			Query query = new FilteredQuery(new TermQuery(new Term("field", "0")), new _Filter_422
				(), FilteredQuery.QUERY_FIRST_FILTER_STRATEGY);
			// no docs -- return null
			TopDocs search = searcher.Search(query, 10);
			AreEqual(totalDocsWithZero, search.TotalHits);
			IOUtils.Close(reader, writer, directory);
		}

		private sealed class _Filter_422 : Filter
		{
			public _Filter_422()
			{
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs
				)
			{
				bool nullBitset = LuceneTestCase.Random().Next(10) == 5;
				AtomicReader reader = ((AtomicReader)context.Reader);
				DocsEnum termDocsEnum = reader.TermDocsEnum(new Term("field", "0"));
				if (termDocsEnum == null)
				{
					return null;
				}
				BitSet bitSet = new BitSet(reader.MaxDoc);
				int d;
				while ((d = termDocsEnum.NextDoc()) != DocsEnum.NO_MORE_DOCS)
				{
					bitSet.Set(d, true);
				}
				return new _DocIdSet_437(nullBitset, reader, bitSet);
			}

			private sealed class _DocIdSet_437 : DocIdSet
			{
				public _DocIdSet_437(bool nullBitset, AtomicReader reader, BitSet bitSet)
				{
					this.nullBitset = nullBitset;
					this.reader = reader;
					this.bitSet = bitSet;
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override Bits Bits()
				{
					if (nullBitset)
					{
						return null;
					}
					return new _Bits_444(bitSet);
				}

				private sealed class _Bits_444 : Bits
				{
					public _Bits_444(BitSet bitSet)
					{
						this.bitSet = bitSet;
					}

					public override bool Get(int index)
					{
						IsTrue("filter was called for a non-matching doc", bitSet.
							Get(index));
						return bitSet.Get(index);
					}

					public override int Length()
					{
						return bitSet.Length();
					}

					private readonly BitSet bitSet;
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override DocIdSetIterator IEnumerator()
				{
					IsTrue("iterator should not be called if bitset is present"
						, nullBitset);
					return reader.TermDocsEnum(new Term("field", "0"));
				}

				private readonly bool nullBitset;

				private readonly AtomicReader reader;

				private readonly BitSet bitSet;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestLeapFrogStrategy()
		{
			Directory directory = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), directory, NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
			int numDocs = AtLeast(50);
			int totalDocsWithZero = 0;
			for (int i = 0; i < numDocs; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				int num = Random().Next(10);
				if (num == 0)
				{
					totalDocsWithZero++;
				}
				doc.Add(NewTextField("field", string.Empty + num, Field.Store.YES));
				writer.AddDocument(doc);
			}
			IndexReader reader = writer.Reader;
			writer.Dispose();
			bool queryFirst = Random().NextBoolean();
			IndexSearcher searcher = NewSearcher(reader);
			Query query = new FilteredQuery(new TermQuery(new Term("field", "0")), new _Filter_501
				(queryFirst), queryFirst ? FilteredQuery.LEAP_FROG_QUERY_FIRST_STRATEGY : Random
				().NextBoolean() ? FilteredQuery.RANDOM_ACCESS_FILTER_STRATEGY : FilteredQuery.LEAP_FROG_FILTER_FIRST_STRATEGY
				);
			// if filterFirst, we can use random here since bits are null
			TopDocs search = searcher.Search(query, 10);
			AreEqual(totalDocsWithZero, search.TotalHits);
			IOUtils.Close(reader, writer, directory);
		}

		private sealed class _Filter_501 : Filter
		{
			public _Filter_501(bool queryFirst)
			{
				this.queryFirst = queryFirst;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs
				)
			{
				return new _DocIdSet_505(context, queryFirst);
			}

			private sealed class _DocIdSet_505 : DocIdSet
			{
				public _DocIdSet_505(AtomicReaderContext context, bool queryFirst)
				{
					this.context = context;
					this.queryFirst = queryFirst;
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override Bits Bits()
				{
					return null;
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override DocIdSetIterator IEnumerator()
				{
					DocsEnum termDocsEnum = ((AtomicReader)context.Reader).TermDocsEnum(new Term("field"
						, "0"));
					if (termDocsEnum == null)
					{
						return null;
					}
					return new _DocIdSetIterator_517(queryFirst, termDocsEnum);
				}

				private sealed class _DocIdSetIterator_517 : DocIdSetIterator
				{
					public _DocIdSetIterator_517(bool queryFirst, DocsEnum termDocsEnum)
					{
						this.queryFirst = queryFirst;
						this.termDocsEnum = termDocsEnum;
					}

					internal bool nextCalled;

					internal bool advanceCalled;

					/// <exception cref="System.IO.IOException"></exception>
					public override int NextDoc()
					{
						IsTrue("queryFirst: " + queryFirst + " advanced: " + this.
							advanceCalled + " next: " + this.nextCalled, this.nextCalled || this.advanceCalled
							 ^ !queryFirst);
						this.nextCalled = true;
						return termDocsEnum.NextDoc();
					}

					public override int DocID
					{
						return termDocsEnum.DocID;
					}

					/// <exception cref="System.IO.IOException"></exception>
					public override int Advance(int target)
					{
						IsTrue("queryFirst: " + queryFirst + " advanced: " + this.
							advanceCalled + " next: " + this.nextCalled, this.advanceCalled || this.nextCalled
							 ^ queryFirst);
						this.advanceCalled = true;
						return termDocsEnum.Advance(target);
					}

					public override long Cost()
					{
						return termDocsEnum.Cost();
					}

					private readonly bool queryFirst;

					private readonly DocsEnum termDocsEnum;
				}

				private readonly AtomicReaderContext context;

				private readonly bool queryFirst;
			}

			private readonly bool queryFirst;
		}
	}
}
