using System;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using NUnit.Framework;

namespace Lucene.Net.Test.Util.Automaton
{
    [TestFixture]
    public class TestSpecialOperations : LuceneTestCase
    {
        [Test]
        public void TestIsFinite()
        {
            int num = AtLeast(200);
            for (var i = 0; i < num; i++)
            {
                var a = AutomatonTestUtil.RandomAutomaton(new Random());
                var b = a.clone();
                Assert.Equals(AutomatonTestUtil.isFiniteSlow(a), SpecialOperations.IsFinite(b));
            }
        }

		/// <summary>
		/// Pass false for testRecursive if the expected strings
		/// may be too long
		/// </summary>
		private ICollection<IntsRef> GetFiniteStrings(Lucene.Net.Util.Automaton.Automaton
			 a, int limit, bool testRecursive)
		{
			ICollection<IntsRef> result = SpecialOperations.GetFiniteStrings(a, limit);
			if (testRecursive)
			{
				AreEqual(AutomatonTestUtil.GetFiniteStringsRecursive(a, limit
					), result);
			}
			return result;
		}
        [Test]
		public virtual void TestFiniteStringsBasic()
        {
            var a = BasicOperations.Union(BasicAutomata.MakeString("dog"), BasicAutomata.MakeString("duck"));
            MinimizationOperations.Minimize(a);
            var strings = SpecialOperations.GetFiniteStrings(a, -1);
            assertEquals(2, strings.Count);
            var dog = new IntsRef();
            Util.ToIntsRef(new BytesRef("dog"), dog);
            assertTrue(strings.Contains(dog));
            var duck = new IntsRef();
            Util.ToIntsRef(new BytesRef("duck"), duck);
            assertTrue(strings.Contains(duck));
        }
		public virtual void TestFiniteStringsEatsStack()
		{
			char[] chars = new char[50000];
			TestUtil.RandomFixedLengthUnicodeString(Random(), chars, 0, chars.Length);
			string bigString1 = new string(chars);
			TestUtil.RandomFixedLengthUnicodeString(Random(), chars, 0, chars.Length);
			string bigString2 = new string(chars);
			Lucene.Net.Util.Automaton.Automaton a = BasicOperations.Union(BasicAutomata
				.MakeString(bigString1), BasicAutomata.MakeString(bigString2));
			ICollection<IntsRef> strings = GetFiniteStrings(a, -1, false);
			AreEqual(2, strings.Count);
			IntsRef scratch = new IntsRef();
			Lucene.Net.Util.Fst.Util.ToUTF32(bigString1.ToCharArray(), 0, bigString1.Length
				, scratch);
			IsTrue(strings.Contains(scratch));
			Lucene.Net.Util.Fst.Util.ToUTF32(bigString2.ToCharArray(), 0, bigString2.Length
				, scratch);
			IsTrue(strings.Contains(scratch));
		}
		public virtual void TestRandomFiniteStrings1()
		{
			int numStrings = AtLeast(100);
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: numStrings=" + numStrings);
			}
			ICollection<IntsRef> strings = new HashSet<IntsRef>();
			IList<Lucene.Net.Util.Automaton.Automaton> automata = new List<Lucene.Net.Util.Automaton.Automaton
				>();
			for (int i = 0; i < numStrings; i++)
			{
				string s = TestUtil.RandomSimpleString(Random(), 1, 200);
				automata.Add(BasicAutomata.MakeString(s));
				IntsRef scratch = new IntsRef();
				Lucene.Net.Util.Fst.Util.ToUTF32(s.ToCharArray(), 0, s.Length, scratch);
				strings.Add(scratch);
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("  add string=" + s);
				}
			}
			// TODO: we could sometimes use
			// DaciukMihovAutomatonBuilder here
			// TODO: what other random things can we do here...
			Lucene.Net.Util.Automaton.Automaton a = BasicOperations.Union(automata);
			if (Random().NextBoolean())
			{
				Lucene.Net.Util.Automaton.Automaton.Minimize(a);
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: a.minimize numStates=" + a.GetNumberOfStates(
						));
				}
			}
			else
			{
				if (Random().NextBoolean())
				{
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("TEST: a.determinize");
					}
					a.Determinize();
				}
				else
				{
					if (Random().NextBoolean())
					{
						if (VERBOSE)
						{
							System.Console.Out.WriteLine("TEST: a.reduce");
						}
						a.Reduce();
					}
					else
					{
						if (Random().NextBoolean())
						{
							if (VERBOSE)
							{
								System.Console.Out.WriteLine("TEST: a.getNumberedStates");
							}
							a.GetNumberedStates();
						}
					}
				}
			}
			ICollection<IntsRef> actual = GetFiniteStrings(a, -1, true);
			if (strings.Equals(actual) == false)
			{
				System.Console.Out.WriteLine("strings.size()=" + strings.Count + " actual.size=" 
					+ actual.Count);
				IList<IntsRef> x = new List<IntsRef>(strings);
				x.Sort();
				IList<IntsRef> y = new List<IntsRef>(actual);
				y.Sort();
				int end = Math.Min(x.Count, y.Count);
				for (int i_1 = 0; i_1 < end; i_1++)
				{
					System.Console.Out.WriteLine("  i=" + i_1 + " string=" + ToString(x[i_1]) + " actual="
						 + ToString(y[i_1]));
				}
				Fail("wrong strings found");
			}
		}

		// ascii only!
		private static string ToString(IntsRef ints)
		{
			BytesRef br = new BytesRef(ints.length);
			for (int i = 0; i < ints.length; i++)
			{
				br.bytes[i] = unchecked((byte)ints.ints[i]);
			}
			br.length = ints.length;
			return br.Utf8ToString();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestWithCycle()
		{
			try
			{
				SpecialOperations.GetFiniteStrings(new RegExp("abc.*", RegExp.NONE).ToAutomaton()
					, -1);
				Fail("did not hit exception");
			}
			catch (ArgumentException)
			{
			}
		}

		// expected
		public virtual void TestRandomFiniteStrings2()
		{
			// Just makes sure we can run on any random finite
			// automaton:
			int iters = AtLeast(100);
			for (int i = 0; i < iters; i++)
			{
				Lucene.Net.Util.Automaton.Automaton a = AutomatonTestUtil.RandomAutomaton(
					Random());
				try
				{
					// Must pass a limit because the random automaton
					// can accept MANY strings:
					SpecialOperations.GetFiniteStrings(a, TestUtil.NextInt(Random(), 1, 1000));
				}
				catch (ArgumentException)
				{
					// NOTE: cannot do this, because the method is not
					// guaranteed to detect cycles when you have a limit
					//assertTrue(SpecialOperations.isFinite(a));
					IsFalse(SpecialOperations.IsFinite(a));
				}
			}
		}

		public virtual void TestInvalidLimit()
		{
			Lucene.Net.Util.Automaton.Automaton a = AutomatonTestUtil.RandomAutomaton(
				Random());
			try
			{
				SpecialOperations.GetFiniteStrings(a, -7);
				Fail("did not hit exception");
			}
			catch (ArgumentException)
			{
			}
		}

		// expected
		public virtual void TestInvalidLimit2()
		{
			Lucene.Net.Util.Automaton.Automaton a = AutomatonTestUtil.RandomAutomaton(
				Random());
			try
			{
				SpecialOperations.GetFiniteStrings(a, 0);
				Fail("did not hit exception");
			}
			catch (ArgumentException)
			{
			}
		}

		// expected
		public virtual void TestSingletonNoLimit()
		{
			ICollection<IntsRef> result = SpecialOperations.GetFiniteStrings(BasicAutomata.MakeString
				("foobar"), -1);
			AreEqual(1, result.Count);
			IntsRef scratch = new IntsRef();
			Lucene.Net.Util.Fst.Util.ToUTF32("foobar".ToCharArray(), 0, 6, scratch);
			IsTrue(result.Contains(scratch));
		}

		public virtual void TestSingletonLimit1()
		{
			ICollection<IntsRef> result = SpecialOperations.GetFiniteStrings(BasicAutomata.MakeString
				("foobar"), 1);
			AreEqual(1, result.Count);
			IntsRef scratch = new IntsRef();
			Lucene.Net.Util.Fst.Util.ToUTF32("foobar".ToCharArray(), 0, 6, scratch);
			IsTrue(result.Contains(scratch));
		}
    }
}
