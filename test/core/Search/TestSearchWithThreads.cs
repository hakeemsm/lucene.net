/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Text;
using NUnit.Framework;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search
{
	public class TestSearchWithThreads : LuceneTestCase
	{
		internal int NUM_DOCS;

		internal readonly int NUM_SEARCH_THREADS = 5;

		internal int RUN_TIME_MSEC;

		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			NUM_DOCS = AtLeast(10000);
			RUN_TIME_MSEC = AtLeast(1000);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void Test()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), dir);
			long startTime = DateTime.Now.CurrentTimeMillis();
			// TODO: replace w/ the @nightly test data; make this
			// into an optional @nightly stress test
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			Field body = NewTextField("body", string.Empty, Field.Store.NO);
			doc.Add(body);
			StringBuilder sb = new StringBuilder();
			for (int docCount = 0; docCount < NUM_DOCS; docCount++)
			{
				int numTerms = Random().Next(10);
				for (int termCount = 0; termCount < numTerms; termCount++)
				{
					sb.Append(Random().NextBoolean() ? "aaa" : "bbb");
					sb.Append(' ');
				}
				body.StringValue = sb.ToString());
				w.AddDocument(doc);
				sb.Delete(0, sb.Length);
			}
			IndexReader r = w.Reader;
			w.Dispose();
			long endTime = DateTime.Now.CurrentTimeMillis();
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("BUILD took " + (endTime - startTime));
			}
			IndexSearcher s = NewSearcher(r);
			AtomicBoolean failed = new AtomicBoolean();
			AtomicLong netSearch = new AtomicLong();
			Thread[] threads = new Thread[NUM_SEARCH_THREADS];
			for (int threadID = 0; threadID < NUM_SEARCH_THREADS; threadID++)
			{
				threads[threadID] = new _Thread_80(this, failed, s, netSearch);
				threads[threadID].SetDaemon(true);
			}
			foreach (Thread t in threads)
			{
				t.Start();
			}
			foreach (Thread t_1 in threads)
			{
				t_1.Join();
			}
			if (VERBOSE)
			{
				System.Console.Out.WriteLine(NUM_SEARCH_THREADS + " threads did " + netSearch.Get
					() + " searches");
			}
			r.Dispose();
			dir.Dispose();
		}

		private sealed class _Thread_80 : Thread
		{
			public _Thread_80(TestSearchWithThreads _enclosing, AtomicBoolean failed, IndexSearcher
				 s, AtomicLong netSearch)
			{
				this._enclosing = _enclosing;
				this.failed = failed;
				this.s = s;
				this.netSearch = netSearch;
				this.col = new TotalHitCountCollector();
			}

			internal TotalHitCountCollector col;

			public override void Run()
			{
				try
				{
					long totHits = 0;
					long totSearch = 0;
					long stopAt = DateTime.Now.CurrentTimeMillis() + this._enclosing.RUN_TIME_MSEC;
					while (DateTime.Now.CurrentTimeMillis() < stopAt && !failed.Get())
					{
						s.Search(new TermQuery(new Term("body", "aaa")), this.col);
						totHits += this.col.GetTotalHits();
						s.Search(new TermQuery(new Term("body", "bbb")), this.col);
						totHits += this.col.GetTotalHits();
						totSearch++;
					}
					IsTrue(totSearch > 0 && totHits > 0);
					netSearch.AddAndGet(totSearch);
				}
				catch (Exception exc)
				{
					failed.Set(true);
					throw new SystemException(exc);
				}
			}

			private readonly TestSearchWithThreads _enclosing;

			private readonly AtomicBoolean failed;

			private readonly IndexSearcher s;

			private readonly AtomicLong netSearch;
		}
	}
}
