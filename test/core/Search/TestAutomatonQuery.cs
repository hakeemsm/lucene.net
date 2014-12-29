/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using Sharpen;

namespace Lucene.Net.Search
{
	public class TestAutomatonQuery : LuceneTestCase
	{
		private Directory directory;

		private IndexReader reader;

		private IndexSearcher searcher;

		private readonly string FN = "field";

		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			directory = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), directory);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			Field titleField = NewTextField("title", "some title", Field.Store.NO);
			Field field = NewTextField(FN, "this is document one 2345", Field.Store.NO);
			Field footerField = NewTextField("footer", "a footer", Field.Store.NO);
			doc.Add(titleField);
			doc.Add(field);
			doc.Add(footerField);
			writer.AddDocument(doc);
			field.StringValue = "some text from doc two a short piece 5678.91");
			writer.AddDocument(doc);
			field.StringValue = "doc three has some different stuff" + " with numbers 1234 5678.9 and letter b"
				);
			writer.AddDocument(doc);
			reader = writer.Reader;
			searcher = NewSearcher(reader);
			writer.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			reader.Dispose();
			directory.Dispose();
			base.TearDown();
		}

		private Term NewTerm(string value)
		{
			return new Term(FN, value);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private int AutomatonQueryNrHits(AutomatonQuery query)
		{
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: run aq=" + query);
			}
			return searcher.Search(query, 5).TotalHits;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void AssertAutomatonHits(int expected, Lucene.Net.Util.Automaton.Automaton
			 automaton)
		{
			AutomatonQuery query = new AutomatonQuery(NewTerm("bogus"), automaton);
			query.SetRewriteMethod(MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE);
			AreEqual(expected, AutomatonQueryNrHits(query));
			query.SetRewriteMethod(MultiTermQuery.CONSTANT_SCORE_FILTER_REWRITE);
			AreEqual(expected, AutomatonQueryNrHits(query));
			query.SetRewriteMethod(MultiTermQuery.CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE);
			AreEqual(expected, AutomatonQueryNrHits(query));
			query.SetRewriteMethod(MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT);
			AreEqual(expected, AutomatonQueryNrHits(query));
		}

		/// <summary>Test some very simple automata.</summary>
		/// <remarks>Test some very simple automata.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestBasicAutomata()
		{
			AssertAutomatonHits(0, BasicAutomata.MakeEmpty());
			AssertAutomatonHits(0, BasicAutomata.MakeEmptyString());
			AssertAutomatonHits(2, BasicAutomata.MakeAnyChar());
			AssertAutomatonHits(3, BasicAutomata.MakeAnyString());
			AssertAutomatonHits(2, BasicAutomata.MakeString("doc"));
			AssertAutomatonHits(1, BasicAutomata.MakeChar('a'));
			AssertAutomatonHits(2, BasicAutomata.MakeCharRange('a', 'b'));
			AssertAutomatonHits(2, BasicAutomata.MakeInterval(1233, 2346, 0));
			AssertAutomatonHits(1, BasicAutomata.MakeInterval(0, 2000, 0));
			AssertAutomatonHits(2, BasicOperations.Union(BasicAutomata.MakeChar('a'), BasicAutomata
				.MakeChar('b')));
			AssertAutomatonHits(0, BasicOperations.Intersection(BasicAutomata.MakeChar('a'), 
				BasicAutomata.MakeChar('b')));
			AssertAutomatonHits(1, BasicOperations.Minus(BasicAutomata.MakeCharRange('a', 'b'
				), BasicAutomata.MakeChar('a')));
		}

		/// <summary>Test that a nondeterministic automaton works correctly.</summary>
		/// <remarks>
		/// Test that a nondeterministic automaton works correctly. (It should will be
		/// determinized)
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestNFA()
		{
			// accept this or three, the union is an NFA (two transitions for 't' from
			// initial state)
			Lucene.Net.Util.Automaton.Automaton nfa = BasicOperations.Union(BasicAutomata
				.MakeString("this"), BasicAutomata.MakeString("three"));
			AssertAutomatonHits(2, nfa);
		}

		public virtual void TestEquals()
		{
			AutomatonQuery a1 = new AutomatonQuery(NewTerm("foobar"), BasicAutomata.MakeString
				("foobar"));
			// reference to a1
			AutomatonQuery a2 = a1;
			// same as a1 (accepts the same language, same term)
			AutomatonQuery a3 = new AutomatonQuery(NewTerm("foobar"), BasicOperations.Concatenate
				(BasicAutomata.MakeString("foo"), BasicAutomata.MakeString("bar")));
			// different than a1 (same term, but different language)
			AutomatonQuery a4 = new AutomatonQuery(NewTerm("foobar"), BasicAutomata.MakeString
				("different"));
			// different than a1 (different term, same language)
			AutomatonQuery a5 = new AutomatonQuery(NewTerm("blah"), BasicAutomata.MakeString(
				"foobar"));
			AreEqual(a1.GetHashCode(), a2.GetHashCode());
			AreEqual(a1, a2);
			AreEqual(a1.GetHashCode(), a3.GetHashCode());
			AreEqual(a1, a3);
			// different class
			AutomatonQuery w1 = new WildcardQuery(NewTerm("foobar"));
			// different class
			AutomatonQuery w2 = new RegexpQuery(NewTerm("foobar"));
			IsFalse(a1.Equals(w1));
			IsFalse(a1.Equals(w2));
			IsFalse(w1.Equals(w2));
			IsFalse(a1.Equals(a4));
			IsFalse(a1.Equals(a5));
			IsFalse(a1.Equals(null));
		}

		/// <summary>
		/// Test that rewriting to a single term works as expected, preserves
		/// MultiTermQuery semantics.
		/// </summary>
		/// <remarks>
		/// Test that rewriting to a single term works as expected, preserves
		/// MultiTermQuery semantics.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestRewriteSingleTerm()
		{
			AutomatonQuery aq = new AutomatonQuery(NewTerm("bogus"), BasicAutomata.MakeString
				("piece"));
			Terms terms = MultiFields.GetTerms(searcher.IndexReader, FN);
			IsTrue(aq.GetTermsEnum(terms) is SingleTermsEnum);
			AreEqual(1, AutomatonQueryNrHits(aq));
		}

		/// <summary>
		/// Test that rewriting to a prefix query works as expected, preserves
		/// MultiTermQuery semantics.
		/// </summary>
		/// <remarks>
		/// Test that rewriting to a prefix query works as expected, preserves
		/// MultiTermQuery semantics.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestRewritePrefix()
		{
			Lucene.Net.Util.Automaton.Automaton pfx = BasicAutomata.MakeString("do");
			pfx.ExpandSingleton();
			// expand singleton representation for testing
			Lucene.Net.Util.Automaton.Automaton prefixAutomaton = BasicOperations.Concatenate
				(pfx, BasicAutomata.MakeAnyString());
			AutomatonQuery aq = new AutomatonQuery(NewTerm("bogus"), prefixAutomaton);
			Terms terms = MultiFields.GetTerms(searcher.IndexReader, FN);
			IsTrue(aq.GetTermsEnum(terms) is PrefixTermsEnum);
			AreEqual(3, AutomatonQueryNrHits(aq));
		}

		/// <summary>Test handling of the empty language</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestEmptyOptimization()
		{
			AutomatonQuery aq = new AutomatonQuery(NewTerm("bogus"), BasicAutomata.MakeEmpty(
				));
			// not yet available: assertTrue(aq.getEnum(searcher.getIndexReader())
			// instanceof EmptyTermEnum);
			Terms terms = MultiFields.GetTerms(searcher.IndexReader, FN);
			AreSame(TermsEnum.EMPTY, aq.GetTermsEnum(terms));
			AreEqual(0, AutomatonQueryNrHits(aq));
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestHashCodeWithThreads()
		{
			AutomatonQuery[] queries = new AutomatonQuery[1000];
			for (int i = 0; i < queries.Length; i++)
			{
				queries[i] = new AutomatonQuery(new Term("bogus", "bogus"), AutomatonTestUtil.RandomAutomaton
					(Random()));
			}
			CountdownEvent startingGun = new CountdownEvent(1);
			int numThreads = TestUtil.NextInt(Random(), 2, 5);
			Thread[] threads = new Thread[numThreads];
			for (int threadID = 0; threadID < numThreads; threadID++)
			{
				Thread thread = new _Thread_223(startingGun, queries);
				threads[threadID] = thread;
				thread.Start();
			}
			startingGun.CountDown();
			foreach (Thread thread_1 in threads)
			{
				thread_1.Join();
			}
		}

		private sealed class _Thread_223 : Thread
		{
			public _Thread_223(CountdownEvent startingGun, AutomatonQuery[] queries)
			{
				this.startingGun = startingGun;
				this.queries = queries;
			}

			public override void Run()
			{
				try
				{
					startingGun.Await();
					for (int i = 0; i < queries.Length; i++)
					{
						queries[i].GetHashCode();
					}
				}
				catch (Exception e)
				{
					Rethrow.Rethrow(e);
				}
			}

			private readonly CountdownEvent startingGun;

			private readonly AutomatonQuery[] queries;
		}
	}
}
