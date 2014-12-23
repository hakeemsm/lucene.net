/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Spans;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search.Spans
{
	/// <summary>
	/// Tests for
	/// <see cref="SpanMultiTermQueryWrapper{Q}">SpanMultiTermQueryWrapper&lt;Q&gt;</see>
	/// , wrapping a few MultiTermQueries.
	/// </summary>
	public class TestSpanMultiTermQueryWrapper : LuceneTestCase
	{
		private Directory directory;

		private IndexReader reader;

		private IndexSearcher searcher;

		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			directory = NewDirectory();
			RandomIndexWriter iw = new RandomIndexWriter(Random(), directory);
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			Field field = NewTextField("field", string.Empty, Field.Store.NO);
			doc.Add(field);
			field.SetStringValue("quick brown fox");
			iw.AddDocument(doc);
			field.SetStringValue("jumps over lazy broun dog");
			iw.AddDocument(doc);
			field.SetStringValue("jumps over extremely very lazy broxn dog");
			iw.AddDocument(doc);
			reader = iw.GetReader();
			iw.Close();
			searcher = NewSearcher(reader);
		}

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			reader.Close();
			directory.Close();
			base.TearDown();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestWildcard()
		{
			WildcardQuery wq = new WildcardQuery(new Term("field", "bro?n"));
			SpanQuery swq = new SpanMultiTermQueryWrapper<WildcardQuery>(wq);
			// will only match quick brown fox
			SpanFirstQuery sfq = new SpanFirstQuery(swq, 2);
			NUnit.Framework.Assert.AreEqual(1, searcher.Search(sfq, 10).totalHits);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestPrefix()
		{
			WildcardQuery wq = new WildcardQuery(new Term("field", "extrem*"));
			SpanQuery swq = new SpanMultiTermQueryWrapper<WildcardQuery>(wq);
			// will only match "jumps over extremely very lazy broxn dog"
			SpanFirstQuery sfq = new SpanFirstQuery(swq, 3);
			NUnit.Framework.Assert.AreEqual(1, searcher.Search(sfq, 10).totalHits);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestFuzzy()
		{
			FuzzyQuery fq = new FuzzyQuery(new Term("field", "broan"));
			SpanQuery sfq = new SpanMultiTermQueryWrapper<FuzzyQuery>(fq);
			// will not match quick brown fox
			SpanPositionRangeQuery sprq = new SpanPositionRangeQuery(sfq, 3, 6);
			NUnit.Framework.Assert.AreEqual(2, searcher.Search(sprq, 10).totalHits);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestFuzzy2()
		{
			// maximum of 1 term expansion
			FuzzyQuery fq = new FuzzyQuery(new Term("field", "broan"), 1, 0, 1, false);
			SpanQuery sfq = new SpanMultiTermQueryWrapper<FuzzyQuery>(fq);
			// will only match jumps over lazy broun dog
			SpanPositionRangeQuery sprq = new SpanPositionRangeQuery(sfq, 0, 100);
			NUnit.Framework.Assert.AreEqual(1, searcher.Search(sprq, 10).totalHits);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestNoSuchMultiTermsInNear()
		{
			//test to make sure non existent multiterms aren't throwing null pointer exceptions  
			FuzzyQuery fuzzyNoSuch = new FuzzyQuery(new Term("field", "noSuch"), 1, 0, 1, false
				);
			SpanQuery spanNoSuch = new SpanMultiTermQueryWrapper<FuzzyQuery>(fuzzyNoSuch);
			SpanQuery term = new SpanTermQuery(new Term("field", "brown"));
			SpanQuery near = new SpanNearQuery(new SpanQuery[] { term, spanNoSuch }, 1, true);
			NUnit.Framework.Assert.AreEqual(0, searcher.Search(near, 10).totalHits);
			//flip order
			near = new SpanNearQuery(new SpanQuery[] { spanNoSuch, term }, 1, true);
			NUnit.Framework.Assert.AreEqual(0, searcher.Search(near, 10).totalHits);
			WildcardQuery wcNoSuch = new WildcardQuery(new Term("field", "noSuch*"));
			SpanQuery spanWCNoSuch = new SpanMultiTermQueryWrapper<WildcardQuery>(wcNoSuch);
			near = new SpanNearQuery(new SpanQuery[] { term, spanWCNoSuch }, 1, true);
			NUnit.Framework.Assert.AreEqual(0, searcher.Search(near, 10).totalHits);
			RegexpQuery rgxNoSuch = new RegexpQuery(new Term("field", "noSuch"));
			SpanQuery spanRgxNoSuch = new SpanMultiTermQueryWrapper<RegexpQuery>(rgxNoSuch);
			near = new SpanNearQuery(new SpanQuery[] { term, spanRgxNoSuch }, 1, true);
			NUnit.Framework.Assert.AreEqual(0, searcher.Search(near, 10).totalHits);
			PrefixQuery prfxNoSuch = new PrefixQuery(new Term("field", "noSuch"));
			SpanQuery spanPrfxNoSuch = new SpanMultiTermQueryWrapper<PrefixQuery>(prfxNoSuch);
			near = new SpanNearQuery(new SpanQuery[] { term, spanPrfxNoSuch }, 1, true);
			NUnit.Framework.Assert.AreEqual(0, searcher.Search(near, 10).totalHits);
			//test single noSuch
			near = new SpanNearQuery(new SpanQuery[] { spanPrfxNoSuch }, 1, true);
			NUnit.Framework.Assert.AreEqual(0, searcher.Search(near, 10).totalHits);
			//test double noSuch
			near = new SpanNearQuery(new SpanQuery[] { spanPrfxNoSuch, spanPrfxNoSuch }, 1, true
				);
			NUnit.Framework.Assert.AreEqual(0, searcher.Search(near, 10).totalHits);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestNoSuchMultiTermsInNotNear()
		{
			//test to make sure non existent multiterms aren't throwing non-matching field exceptions  
			FuzzyQuery fuzzyNoSuch = new FuzzyQuery(new Term("field", "noSuch"), 1, 0, 1, false
				);
			SpanQuery spanNoSuch = new SpanMultiTermQueryWrapper<FuzzyQuery>(fuzzyNoSuch);
			SpanQuery term = new SpanTermQuery(new Term("field", "brown"));
			SpanNotQuery notNear = new SpanNotQuery(term, spanNoSuch, 0, 0);
			NUnit.Framework.Assert.AreEqual(1, searcher.Search(notNear, 10).totalHits);
			//flip
			notNear = new SpanNotQuery(spanNoSuch, term, 0, 0);
			NUnit.Framework.Assert.AreEqual(0, searcher.Search(notNear, 10).totalHits);
			//both noSuch
			notNear = new SpanNotQuery(spanNoSuch, spanNoSuch, 0, 0);
			NUnit.Framework.Assert.AreEqual(0, searcher.Search(notNear, 10).totalHits);
			WildcardQuery wcNoSuch = new WildcardQuery(new Term("field", "noSuch*"));
			SpanQuery spanWCNoSuch = new SpanMultiTermQueryWrapper<WildcardQuery>(wcNoSuch);
			notNear = new SpanNotQuery(term, spanWCNoSuch, 0, 0);
			NUnit.Framework.Assert.AreEqual(1, searcher.Search(notNear, 10).totalHits);
			RegexpQuery rgxNoSuch = new RegexpQuery(new Term("field", "noSuch"));
			SpanQuery spanRgxNoSuch = new SpanMultiTermQueryWrapper<RegexpQuery>(rgxNoSuch);
			notNear = new SpanNotQuery(term, spanRgxNoSuch, 1, 1);
			NUnit.Framework.Assert.AreEqual(1, searcher.Search(notNear, 10).totalHits);
			PrefixQuery prfxNoSuch = new PrefixQuery(new Term("field", "noSuch"));
			SpanQuery spanPrfxNoSuch = new SpanMultiTermQueryWrapper<PrefixQuery>(prfxNoSuch);
			notNear = new SpanNotQuery(term, spanPrfxNoSuch, 1, 1);
			NUnit.Framework.Assert.AreEqual(1, searcher.Search(notNear, 10).totalHits);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestNoSuchMultiTermsInOr()
		{
			//test to make sure non existent multiterms aren't throwing null pointer exceptions  
			FuzzyQuery fuzzyNoSuch = new FuzzyQuery(new Term("field", "noSuch"), 1, 0, 1, false
				);
			SpanQuery spanNoSuch = new SpanMultiTermQueryWrapper<FuzzyQuery>(fuzzyNoSuch);
			SpanQuery term = new SpanTermQuery(new Term("field", "brown"));
			SpanOrQuery near = new SpanOrQuery(new SpanQuery[] { term, spanNoSuch });
			NUnit.Framework.Assert.AreEqual(1, searcher.Search(near, 10).totalHits);
			//flip
			near = new SpanOrQuery(new SpanQuery[] { spanNoSuch, term });
			NUnit.Framework.Assert.AreEqual(1, searcher.Search(near, 10).totalHits);
			WildcardQuery wcNoSuch = new WildcardQuery(new Term("field", "noSuch*"));
			SpanQuery spanWCNoSuch = new SpanMultiTermQueryWrapper<WildcardQuery>(wcNoSuch);
			near = new SpanOrQuery(new SpanQuery[] { term, spanWCNoSuch });
			NUnit.Framework.Assert.AreEqual(1, searcher.Search(near, 10).totalHits);
			RegexpQuery rgxNoSuch = new RegexpQuery(new Term("field", "noSuch"));
			SpanQuery spanRgxNoSuch = new SpanMultiTermQueryWrapper<RegexpQuery>(rgxNoSuch);
			near = new SpanOrQuery(new SpanQuery[] { term, spanRgxNoSuch });
			NUnit.Framework.Assert.AreEqual(1, searcher.Search(near, 10).totalHits);
			PrefixQuery prfxNoSuch = new PrefixQuery(new Term("field", "noSuch"));
			SpanQuery spanPrfxNoSuch = new SpanMultiTermQueryWrapper<PrefixQuery>(prfxNoSuch);
			near = new SpanOrQuery(new SpanQuery[] { term, spanPrfxNoSuch });
			NUnit.Framework.Assert.AreEqual(1, searcher.Search(near, 10).totalHits);
			near = new SpanOrQuery(new SpanQuery[] { spanPrfxNoSuch });
			NUnit.Framework.Assert.AreEqual(0, searcher.Search(near, 10).totalHits);
			near = new SpanOrQuery(new SpanQuery[] { spanPrfxNoSuch, spanPrfxNoSuch });
			NUnit.Framework.Assert.AreEqual(0, searcher.Search(near, 10).totalHits);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestNoSuchMultiTermsInSpanFirst()
		{
			//this hasn't been a problem  
			FuzzyQuery fuzzyNoSuch = new FuzzyQuery(new Term("field", "noSuch"), 1, 0, 1, false
				);
			SpanQuery spanNoSuch = new SpanMultiTermQueryWrapper<FuzzyQuery>(fuzzyNoSuch);
			SpanQuery spanFirst = new SpanFirstQuery(spanNoSuch, 10);
			NUnit.Framework.Assert.AreEqual(0, searcher.Search(spanFirst, 10).totalHits);
			WildcardQuery wcNoSuch = new WildcardQuery(new Term("field", "noSuch*"));
			SpanQuery spanWCNoSuch = new SpanMultiTermQueryWrapper<WildcardQuery>(wcNoSuch);
			spanFirst = new SpanFirstQuery(spanWCNoSuch, 10);
			NUnit.Framework.Assert.AreEqual(0, searcher.Search(spanFirst, 10).totalHits);
			RegexpQuery rgxNoSuch = new RegexpQuery(new Term("field", "noSuch"));
			SpanQuery spanRgxNoSuch = new SpanMultiTermQueryWrapper<RegexpQuery>(rgxNoSuch);
			spanFirst = new SpanFirstQuery(spanRgxNoSuch, 10);
			NUnit.Framework.Assert.AreEqual(0, searcher.Search(spanFirst, 10).totalHits);
			PrefixQuery prfxNoSuch = new PrefixQuery(new Term("field", "noSuch"));
			SpanQuery spanPrfxNoSuch = new SpanMultiTermQueryWrapper<PrefixQuery>(prfxNoSuch);
			spanFirst = new SpanFirstQuery(spanPrfxNoSuch, 10);
			NUnit.Framework.Assert.AreEqual(0, searcher.Search(spanFirst, 10).totalHits);
		}
	}
}
