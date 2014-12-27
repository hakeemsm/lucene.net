/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search
{
	/// <summary>
	/// Tests the
	/// <see cref="TimeLimitingCollector">TimeLimitingCollector</see>
	/// .  This test checks (1) search
	/// correctness (regardless of timeout), (2) expected timeout behavior,
	/// and (3) a sanity test with multiple searching threads.
	/// </summary>
	public class TestTimeLimitingCollector : LuceneTestCase
	{
		private const int SLOW_DOWN = 3;

		private const long TIME_ALLOWED = 17 * SLOW_DOWN;

		private const double MULTI_THREAD_SLACK = 7;

		private const int N_DOCS = 3000;

		private const int N_THREADS = 50;

		private IndexSearcher searcher;

		private Directory directory;

		private IndexReader reader;

		private readonly string FIELD_NAME = "body";

		private Query query;

		private Counter counter;

		private TimeLimitingCollector.TimerThread counterThread;

		// so searches can find about 17 docs.
		// max time allowed is relaxed for multithreading tests. 
		// the multithread case fails when setting this to 1 (no slack) and launching many threads (>2000).  
		// but this is not a real failure, just noise.
		/// <summary>initializes searcher with a document set</summary>
		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			counter = Counter.NewCounter(true);
			counterThread = new TimeLimitingCollector.TimerThread(counter);
			counterThread.Start();
			string[] docText = new string[] { "docThatNeverMatchesSoWeCanRequireLastDocCollectedToBeGreaterThanZero"
				, "one blah three", "one foo three multiOne", "one foobar three multiThree", "blueberry pancakes"
				, "blueberry pie", "blueberry strudel", "blueberry pizza" };
			directory = NewDirectory();
			RandomIndexWriter iw = new RandomIndexWriter(Random(), directory, NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy
				()));
			for (int i = 0; i < N_DOCS; i++)
			{
				Add(docText[i % docText.Length], iw);
			}
			reader = iw.GetReader();
			iw.Dispose();
			searcher = NewSearcher(reader);
			BooleanQuery booleanQuery = new BooleanQuery();
			booleanQuery.Add(new TermQuery(new Term(FIELD_NAME, "one")), BooleanClause.Occur.
				SHOULD);
			// start from 1, so that the 0th doc never matches
			for (int i_1 = 1; i_1 < docText.Length; i_1++)
			{
				string[] docTextParts = docText[i_1].Split("\\s+");
				foreach (string docTextPart in docTextParts)
				{
					// large query so that search will be longer
					booleanQuery.Add(new TermQuery(new Term(FIELD_NAME, docTextPart)), BooleanClause.Occur
						.SHOULD);
				}
			}
			query = booleanQuery;
			// warm the searcher
			searcher.Search(query, null, 1000);
		}

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			reader.Dispose();
			directory.Dispose();
			counterThread.StopTimer();
			counterThread.Join();
			base.TearDown();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void Add(string value, RandomIndexWriter iw)
		{
			Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
			d.Add(NewTextField(FIELD_NAME, value, Field.Store.NO));
			iw.AddDocument(d);
		}

		/// <exception cref="System.Exception"></exception>
		private void Search(Collector collector)
		{
			searcher.Search(query, collector);
		}

		/// <summary>test search correctness with no timeout</summary>
		public virtual void TestSearch()
		{
			DoTestSearch();
		}

		private void DoTestSearch()
		{
			int totalResults = 0;
			int totalTLCResults = 0;
			try
			{
				TestTimeLimitingCollector.MyHitCollector myHc = new TestTimeLimitingCollector.MyHitCollector
					(this);
				Search(myHc);
				totalResults = myHc.HitCount();
				myHc = new TestTimeLimitingCollector.MyHitCollector(this);
				long oneHour = 3600000;
				Collector tlCollector = CreateTimedCollector(myHc, oneHour, false);
				Search(tlCollector);
				totalTLCResults = myHc.HitCount();
			}
			catch (Exception e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
				IsTrue("Unexpected exception: " + e, false);
			}
			//==fail
			AreEqual("Wrong number of results!", totalResults, totalTLCResults
				);
		}

		private Collector CreateTimedCollector(TestTimeLimitingCollector.MyHitCollector hc
			, long timeAllowed, bool greedy)
		{
			TimeLimitingCollector res = new TimeLimitingCollector(hc, counter, timeAllowed);
			res.SetGreedy(greedy);
			// set to true to make sure at least one doc is collected.
			return res;
		}

		/// <summary>Test that timeout is obtained, and soon enough!</summary>
		public virtual void TestTimeoutGreedy()
		{
			DoTestTimeout(false, true);
		}

		/// <summary>Test that timeout is obtained, and soon enough!</summary>
		public virtual void TestTimeoutNotGreedy()
		{
			DoTestTimeout(false, false);
		}

		private void DoTestTimeout(bool multiThreaded, bool greedy)
		{
			// setup
			TestTimeLimitingCollector.MyHitCollector myHc = new TestTimeLimitingCollector.MyHitCollector
				(this);
			myHc.SetSlowDown(SLOW_DOWN);
			Collector tlCollector = CreateTimedCollector(myHc, TIME_ALLOWED, greedy);
			// search
			TimeLimitingCollector.TimeExceededException timoutException = null;
			try
			{
				Search(tlCollector);
			}
			catch (TimeLimitingCollector.TimeExceededException x)
			{
				timoutException = x;
			}
			catch (Exception e)
			{
				IsTrue("Unexpected exception: " + e, false);
			}
			//==fail
			// must get exception
			IsNotNull("Timeout expected!", timoutException);
			// greediness affect last doc collected
			int exceptionDoc = timoutException.GetLastDocCollected();
			int lastCollected = myHc.GetLastDocCollected();
			IsTrue("doc collected at timeout must be > 0!", exceptionDoc
				 > 0);
			if (greedy)
			{
				IsTrue("greedy=" + greedy + " exceptionDoc=" + exceptionDoc
					 + " != lastCollected=" + lastCollected, exceptionDoc == lastCollected);
				IsTrue("greedy, but no hits found!", myHc.HitCount() > 0);
			}
			else
			{
				IsTrue("greedy=" + greedy + " exceptionDoc=" + exceptionDoc
					 + " not > lastCollected=" + lastCollected, exceptionDoc > lastCollected);
			}
			// verify that elapsed time at exception is within valid limits
			AreEqual(timoutException.GetTimeAllowed(), TIME_ALLOWED);
			// a) Not too early
			IsTrue("elapsed=" + timoutException.GetTimeElapsed() + " <= (allowed-resolution)="
				 + (TIME_ALLOWED - counterThread.GetResolution()), timoutException.GetTimeElapsed
				() > TIME_ALLOWED - counterThread.GetResolution());
			// b) Not too late.
			//    This part is problematic in a busy test system, so we just print a warning.
			//    We already verified that a timeout occurred, we just can't be picky about how long it took.
			if (timoutException.GetTimeElapsed() > MaxTime(multiThreaded))
			{
				System.Console.Out.WriteLine("Informative: timeout exceeded (no action required: most probably just "
					 + " because the test machine is slower than usual):  " + "lastDoc=" + exceptionDoc
					 + " ,&& allowed=" + timoutException.GetTimeAllowed() + " ,&& elapsed=" + timoutException
					.GetTimeElapsed() + " >= " + MaxTimeStr(multiThreaded));
			}
		}

		private long MaxTime(bool multiThreaded)
		{
			long res = 2 * counterThread.GetResolution() + TIME_ALLOWED + SLOW_DOWN;
			// some slack for less noise in this test
			if (multiThreaded)
			{
				res *= MULTI_THREAD_SLACK;
			}
			// larger slack  
			return res;
		}

		private string MaxTimeStr(bool multiThreaded)
		{
			string s = "( " + "2*resolution +  TIME_ALLOWED + SLOW_DOWN = " + "2*" + counterThread
				.GetResolution() + " + " + TIME_ALLOWED + " + " + SLOW_DOWN + ")";
			if (multiThreaded)
			{
				s = MULTI_THREAD_SLACK + " * " + s;
			}
			return MaxTime(multiThreaded) + " = " + s;
		}

		/// <summary>Test timeout behavior when resolution is modified.</summary>
		/// <remarks>Test timeout behavior when resolution is modified.</remarks>
		public virtual void TestModifyResolution()
		{
			try
			{
				// increase and test
				long resolution = 20 * TimeLimitingCollector.TimerThread.DEFAULT_RESOLUTION;
				//400
				counterThread.SetResolution(resolution);
				AreEqual(resolution, counterThread.GetResolution());
				DoTestTimeout(false, true);
				// decrease much and test
				resolution = 5;
				counterThread.SetResolution(resolution);
				AreEqual(resolution, counterThread.GetResolution());
				DoTestTimeout(false, true);
				// return to default and test
				resolution = TimeLimitingCollector.TimerThread.DEFAULT_RESOLUTION;
				counterThread.SetResolution(resolution);
				AreEqual(resolution, counterThread.GetResolution());
				DoTestTimeout(false, true);
			}
			finally
			{
				counterThread.SetResolution(TimeLimitingCollector.TimerThread.DEFAULT_RESOLUTION);
			}
		}

		/// <summary>Test correctness with multiple searching threads.</summary>
		/// <remarks>Test correctness with multiple searching threads.</remarks>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestSearchMultiThreaded()
		{
			DoTestMultiThreads(false);
		}

		/// <summary>Test correctness with multiple searching threads.</summary>
		/// <remarks>Test correctness with multiple searching threads.</remarks>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestTimeoutMultiThreaded()
		{
			DoTestMultiThreads(true);
		}

		/// <exception cref="System.Exception"></exception>
		private void DoTestMultiThreads(bool withTimeout)
		{
			Sharpen.Thread[] threadArray = new Sharpen.Thread[N_THREADS];
			BitSet success = new BitSet(N_THREADS);
			for (int i = 0; i < threadArray.Length; ++i)
			{
				int num = i;
				threadArray[num] = new _Thread_286(this, withTimeout, success, num);
			}
			for (int i_1 = 0; i_1 < threadArray.Length; ++i_1)
			{
				threadArray[i_1].Start();
			}
			for (int i_2 = 0; i_2 < threadArray.Length; ++i_2)
			{
				threadArray[i_2].Join();
			}
			AreEqual("some threads failed!", N_THREADS, success.Cardinality
				());
		}

		private sealed class _Thread_286 : Sharpen.Thread
		{
			public _Thread_286(TestTimeLimitingCollector _enclosing, bool withTimeout, BitSet
				 success, int num)
			{
				this._enclosing = _enclosing;
				this.withTimeout = withTimeout;
				this.success = success;
				this.num = num;
			}

			public override void Run()
			{
				if (withTimeout)
				{
					this._enclosing.DoTestTimeout(true, true);
				}
				else
				{
					this._enclosing.DoTestSearch();
				}
				lock (success)
				{
					success.Set(num);
				}
			}

			private readonly TestTimeLimitingCollector _enclosing;

			private readonly bool withTimeout;

			private readonly BitSet success;

			private readonly int num;
		}

		private class MyHitCollector : Collector
		{
			private readonly BitSet bits = new BitSet();

			private int slowdown = 0;

			private int lastDocCollected = -1;

			private int docBase = 0;

			// counting collector that can slow down at collect().
			/// <summary>amount of time to wait on each collect to simulate a long iteration</summary>
			public virtual void SetSlowDown(int milliseconds)
			{
				this.slowdown = milliseconds;
			}

			public virtual int HitCount()
			{
				return this.bits.Cardinality();
			}

			public virtual int GetLastDocCollected()
			{
				return this.lastDocCollected;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void SetScorer(Scorer scorer)
			{
			}

			// scorer is not needed
			/// <exception cref="System.IO.IOException"></exception>
			public override void Collect(int doc)
			{
				int docId = doc + this.docBase;
				if (this.slowdown > 0)
				{
					try
					{
						Sharpen.Thread.Sleep(this.slowdown);
					}
					catch (Exception ie)
					{
						throw new ThreadInterruptedException(ie);
					}
				}
				//HM:revisit 
				//assert docId >= 0: " base=" + docBase + " doc=" + doc;
				this.bits.Set(docId);
				this.lastDocCollected = docId;
			}

			public override void SetNextReader(AtomicReaderContext context)
			{
				this.docBase = context.docBase;
			}

			public override bool AcceptsDocsOutOfOrder()
			{
				return false;
			}

			internal MyHitCollector(TestTimeLimitingCollector _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly TestTimeLimitingCollector _enclosing;
		}
	}
}
