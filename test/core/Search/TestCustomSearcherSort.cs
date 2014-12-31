/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;


namespace Lucene.Net.Search
{
	/// <summary>Unit test for sorting code.</summary>
	/// <remarks>Unit test for sorting code.</remarks>
	public class TestCustomSearcherSort : LuceneTestCase
	{
		private Directory index = null;

		private IndexReader reader;

		private Query query = null;

		private int INDEX_SIZE;

		// reduced from 20000 to 2000 to speed up test...
		/// <summary>Create index and query for test cases.</summary>
		/// <remarks>Create index and query for test cases.</remarks>
		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			INDEX_SIZE = AtLeast(2000);
			index = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), index);
			TestCustomSearcherSort.RandomGen random = new TestCustomSearcherSort.RandomGen(this
				, Random());
			for (int i = 0; i < INDEX_SIZE; ++i)
			{
				// don't decrease; if to low the
				// problem doesn't show up
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				if ((i % 5) != 0)
				{
					// some documents must not have an entry in the first
					// sort field
					doc.Add(NewStringField("publicationDate_", random.GetLuceneDate(), Field.Store.YES
						));
				}
				if ((i % 7) == 0)
				{
					// some documents to match the query (see below)
					doc.Add(NewTextField("content", "test", Field.Store.YES));
				}
				// every document has a defined 'mandant' field
				doc.Add(NewStringField("mandant", Extensions.ToString(i % 3), Field.Store
					.YES));
				writer.AddDocument(doc);
			}
			reader = writer.Reader;
			writer.Dispose();
			query = new TermQuery(new Term("content", "test"));
		}

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			reader.Dispose();
			index.Dispose();
			base.TearDown();
		}

		/// <summary>Run the test using two CustomSearcher instances.</summary>
		/// <remarks>Run the test using two CustomSearcher instances.</remarks>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestFieldSortCustomSearcher()
		{
			// log("Run testFieldSortCustomSearcher");
			// define the sort criteria
			Sort custSort = new Sort(new SortField("publicationDate_", SortField.Type.STRING)
				, SortField.FIELD_SCORE);
			IndexSearcher searcher = new TestCustomSearcherSort.CustomSearcher(this, reader, 
				2);
			// search and check hits
			MatchHits(searcher, custSort);
		}

		/// <summary>Run the test using one CustomSearcher wrapped by a MultiSearcher.</summary>
		/// <remarks>Run the test using one CustomSearcher wrapped by a MultiSearcher.</remarks>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestFieldSortSingleSearcher()
		{
			// log("Run testFieldSortSingleSearcher");
			// define the sort criteria
			Sort custSort = new Sort(new SortField("publicationDate_", SortField.Type.STRING)
				, SortField.FIELD_SCORE);
			IndexSearcher searcher = new TestCustomSearcherSort.CustomSearcher(this, reader, 
				2);
			// search and check hits
			MatchHits(searcher, custSort);
		}

		// make sure the documents returned by the search match the expected list
		/// <exception cref="System.IO.IOException"></exception>
		private void MatchHits(IndexSearcher searcher, Sort sort)
		{
			// make a query without sorting first
			ScoreDoc[] hitsByRank = searcher.Search(query, null, int.MaxValue).ScoreDocs;
			CheckHits(hitsByRank, "Sort by rank: ");
			// check for duplicates
			IDictionary<int, int> resultMap = new SortedDictionary<int, int>();
			// store hits in TreeMap - TreeMap does not allow duplicates; existing
			// entries are silently overwritten
			for (int hitid = 0; hitid < hitsByRank.Length; ++hitid)
			{
				resultMap.Put(Extensions.ValueOf(hitsByRank[hitid].Doc), Extensions.ValueOf
					(hitid));
			}
			// Key: Lucene
			// Document ID
			// Value: Hits-Objekt Index
			// now make a query using the sort criteria
			ScoreDoc[] resultSort = searcher.Search(query, null, int.MaxValue, sort).ScoreDocs;
			CheckHits(resultSort, "Sort by custom criteria: ");
			// check for duplicates
			// besides the sorting both sets of hits must be identical
			for (int hitid_1 = 0; hitid_1 < resultSort.Length; ++hitid_1)
			{
				int idHitDate = Extensions.ValueOf(resultSort[hitid_1].Doc);
				// document ID
				// from sorted
				// search
				if (!resultMap.ContainsKey(idHitDate))
				{
					Log("ID " + idHitDate + " not found. Possibliy a duplicate.");
				}
				IsTrue(resultMap.ContainsKey(idHitDate));
				// same ID must be in the
				// Map from the rank-sorted
				// search
				// every hit must appear once in both result sets --> remove it from the
				// Map.
				// At the end the Map must be empty!
				Collections.Remove(resultMap, idHitDate);
			}
			if (resultMap.Count == 0)
			{
			}
			else
			{
				// log("All hits matched");
				Log("Couldn't match " + resultMap.Count + " hits.");
			}
			AreEqual(resultMap.Count, 0);
		}

		/// <summary>Check the hits for duplicates.</summary>
		/// <remarks>Check the hits for duplicates.</remarks>
		private void CheckHits(ScoreDoc[] hits, string prefix)
		{
			if (hits != null)
			{
				IDictionary<int, int> idMap = new SortedDictionary<int, int>();
				for (int docnum = 0; docnum < hits.Length; ++docnum)
				{
					int luceneId = null;
					luceneId = Extensions.ValueOf(hits[docnum].Doc);
					if (idMap.ContainsKey(luceneId))
					{
						StringBuilder message = new StringBuilder(prefix);
						message.Append("Duplicate key for hit index = ");
						message.Append(docnum);
						message.Append(", previous index = ");
						message.Append((idMap.Get(luceneId)).ToString());
						message.Append(", Lucene ID = ");
						message.Append(luceneId);
						Log(message.ToString());
					}
					else
					{
						idMap.Put(luceneId, Extensions.ValueOf(docnum));
					}
				}
			}
		}

		// Simply write to console - choosen to be independant of log4j etc
		private void Log(string message)
		{
			if (VERBOSE)
			{
				System.Console.Out.WriteLine(message);
			}
		}

		public class CustomSearcher : IndexSearcher
		{
			private int switcher;

			public CustomSearcher(TestCustomSearcherSort _enclosing, IndexReader r, int switcher
				) : base(r)
			{
				this._enclosing = _enclosing;
				this.switcher = switcher;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override TopFieldDocs Search(Query query, Filter filter, int nDocs, Sort sort
				)
			{
				BooleanQuery bq = new BooleanQuery();
				bq.Add(query, BooleanClause.Occur.MUST);
				bq.Add(new TermQuery(new Term("mandant", Extensions.ToString(this.switcher
					))), BooleanClause.Occur.MUST);
				return base.Search(bq, filter, nDocs, sort);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override TopDocs Search(Query query, Filter filter, int nDocs)
			{
				BooleanQuery bq = new BooleanQuery();
				bq.Add(query, BooleanClause.Occur.MUST);
				bq.Add(new TermQuery(new Term("mandant", Extensions.ToString(this.switcher
					))), BooleanClause.Occur.MUST);
				return base.Search(bq, filter, nDocs);
			}

			private readonly TestCustomSearcherSort _enclosing;
		}

		private class RandomGen
		{
			internal RandomGen(TestCustomSearcherSort _enclosing, Random random)
			{
				this._enclosing = _enclosing;
				this.random = random;
				this.@base.Set(1980, 1, 1);
			}

			private Random random;

			private Calendar @base = new GregorianCalendar(System.TimeZoneInfo.Local, CultureInfo
				.CurrentCulture);

			// we use the default Locale/TZ since LuceneTestCase randomizes it
			// Just to generate some different Lucene Date strings
			private string GetLuceneDate()
			{
				return DateTools.TimeToString(this.@base.GetTimeInMillis() + this.random.Next() -
					 int.MinValue, DateTools.Resolution.DAY);
			}

			private readonly TestCustomSearcherSort _enclosing;
		}
	}
}
