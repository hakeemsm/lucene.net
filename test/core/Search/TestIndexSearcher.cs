using System;
using Lucene.Net.Document;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.TestFramework;
using Lucene.Net.TestFramework.Index;
using Lucene.Net.TestFramework.Util;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search
{
	public class TestIndexSearcher : LuceneTestCase
	{
		internal Directory dir;

		internal IndexReader reader;

		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			dir = NewDirectory();
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir);
			for (int i = 0; i < 100; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(NewStringField("field", i.ToString(), Field.Store.NO));
				doc.Add(NewStringField("field2", bool.ToString(i % 2 == 0), Field.Store.NO));
				iw.AddDocument(doc);
			}
			reader = iw.Reader;
			iw.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			base.TearDown();
			reader.Dispose();
			dir.Dispose();
		}

		// should not throw exception
		/// <exception cref="System.Exception"></exception>
		public virtual void TestHugeN()
		{
			ExecutorService service = new ThreadPoolExecutor(4, 4, 0L, TimeUnit.MILLISECONDS, 
				new LinkedBlockingQueue<Runnable>(), new NamedThreadFactory("TestIndexSearcher")
				);
			IndexSearcher[] searchers = new IndexSearcher[] { new IndexSearcher(reader), new 
				IndexSearcher(reader, service) };
			Query[] queries = new Query[] { new MatchAllDocsQuery(), new TermQuery(new Term("field"
				, "1")) };
			Sort[] sorts = new Sort[] { null, new Sort(new SortField("field2", SortField.Type
				.STRING)) };
			Filter[] filters = new Filter[] { null, new QueryWrapperFilter(new TermQuery(new 
				Term("field2", "true"))) };
			ScoreDoc[] afters = new ScoreDoc[] { null, new FieldDoc(0, 0f, new object[] { new 
				BytesRef("boo!") }) };
			foreach (IndexSearcher searcher in searchers)
			{
				foreach (ScoreDoc after in afters)
				{
					foreach (Query query in queries)
					{
						foreach (Sort sort in sorts)
						{
							foreach (Filter filter in filters)
							{
								searcher.Search(query, int.MaxValue);
								searcher.SearchAfter(after, query, int.MaxValue);
								searcher.Search(query, filter, int.MaxValue);
								searcher.SearchAfter(after, query, filter, int.MaxValue);
								if (sort != null)
								{
									searcher.Search(query, int.MaxValue, sort);
									searcher.Search(query, filter, int.MaxValue, sort);
									searcher.Search(query, filter, int.MaxValue, sort, true, true);
									searcher.Search(query, filter, int.MaxValue, sort, true, false);
									searcher.Search(query, filter, int.MaxValue, sort, false, true);
									searcher.Search(query, filter, int.MaxValue, sort, false, false);
									searcher.SearchAfter(after, query, filter, int.MaxValue, sort);
									searcher.SearchAfter(after, query, filter, int.MaxValue, sort, true, true);
									searcher.SearchAfter(after, query, filter, int.MaxValue, sort, true, false);
									searcher.SearchAfter(after, query, filter, int.MaxValue, sort, false, true);
									searcher.SearchAfter(after, query, filter, int.MaxValue, sort, false, false);
								}
							}
						}
					}
				}
			}
			TestUtil.ShutdownExecutorService(service);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestSearchAfterPassedMaxDoc()
		{
			// LUCENE-5128: ensure we get a meaningful message if searchAfter exceeds maxDoc
			Directory dir = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), dir);
			w.AddDocument(new Lucene.Net.Documents.Document());
			IndexReader r = w.Reader;
			w.Dispose();
			IndexSearcher s = new IndexSearcher(r);
			try
			{
				s.SearchAfter(new ScoreDoc(r.MaxDoc, 0.54f), new MatchAllDocsQuery(), 10);
				Fail("should have hit IllegalArgumentException when searchAfter exceeds maxDoc"
					);
			}
			catch (ArgumentException)
			{
			}
			finally
			{
				// ok
				IOUtils.Close(r, dir);
			}
		}
	}
}
