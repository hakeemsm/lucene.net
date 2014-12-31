/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.TestFramework.Util;
using Lucene.Net.TestFramework.Util.Automaton;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using Sharpen;

namespace Lucene.Net.TestFramework.Util.Automaton
{
	/// <summary>Utilities for testing automata.</summary>
	/// <remarks>
	/// Utilities for testing automata.
	/// <p>
	/// Capable of generating random regular expressions,
	/// and automata, and also provides a number of very
	/// basic unoptimized implementations (*slow) for testing.
	/// </remarks>
	public class AutomatonTestUtil
	{
		/// <summary>Returns random string, including full unicode range.</summary>
		/// <remarks>Returns random string, including full unicode range.</remarks>
		public static string RandomRegexp(Random r)
		{
			while (true)
			{
				string regexp = RandomRegexpString(r);
				// we will also generate some undefined unicode queries
				if (!UnicodeUtil.ValidUTF16String(regexp))
				{
					continue;
				}
				try
				{
					new RegExp(regexp, RegExp.NONE);
					return regexp;
				}
				catch (Exception)
				{
				}
			}
		}

		private static string RandomRegexpString(Random r)
		{
			int end = r.Next(20);
			if (end == 0)
			{
				// allow 0 length
				return string.Empty;
			}
			char[] buffer = new char[end];
			for (int i = 0; i < end; i++)
			{
				int t = r.Next(15);
				if (0 == t && i < end - 1)
				{
					// Make a surrogate pair
					// High surrogate
					buffer[i++] = (char)TestUtil.NextInt(r, unchecked((int)(0xd800)), unchecked((int)
						(0xdbff)));
					// Low surrogate
					buffer[i] = (char)TestUtil.NextInt(r, unchecked((int)(0xdc00)), unchecked((int)(0xdfff
						)));
				}
				else
				{
					if (t <= 1)
					{
						buffer[i] = (char)r.Next(unchecked((int)(0x80)));
					}
					else
					{
						if (2 == t)
						{
							buffer[i] = (char)TestUtil.NextInt(r, unchecked((int)(0x80)), unchecked((int)(0x800
								)));
						}
						else
						{
							if (3 == t)
							{
								buffer[i] = (char)TestUtil.NextInt(r, unchecked((int)(0x800)), unchecked((int)(0xd7ff
									)));
							}
							else
							{
								if (4 == t)
								{
									buffer[i] = (char)TestUtil.NextInt(r, unchecked((int)(0xe000)), unchecked((int)(0xffff
										)));
								}
								else
								{
									if (5 == t)
									{
										buffer[i] = '.';
									}
									else
									{
										if (6 == t)
										{
											buffer[i] = '?';
										}
										else
										{
											if (7 == t)
											{
												buffer[i] = '*';
											}
											else
											{
												if (8 == t)
												{
													buffer[i] = '+';
												}
												else
												{
													if (9 == t)
													{
														buffer[i] = '(';
													}
													else
													{
														if (10 == t)
														{
															buffer[i] = ')';
														}
														else
														{
															if (11 == t)
															{
																buffer[i] = '-';
															}
															else
															{
																if (12 == t)
																{
																	buffer[i] = '[';
																}
																else
																{
																	if (13 == t)
																	{
																		buffer[i] = ']';
																	}
																	else
																	{
																		if (14 == t)
																		{
																			buffer[i] = '|';
																		}
																	}
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			return new string(buffer, 0, end);
		}

		/// <summary>
		/// picks a random int code point, avoiding surrogates;
		/// throws IllegalArgumentException if this transition only
		/// accepts surrogates
		/// </summary>
		private static int GetRandomCodePoint(Random r, Transition t)
		{
			int code;
			if (t.max < UnicodeUtil.UNI_SUR_HIGH_START || t.min > UnicodeUtil.UNI_SUR_HIGH_END)
			{
				// easy: entire range is before or after surrogates
				code = t.min + r.Next(t.max - t.min + 1);
			}
			else
			{
				if (t.min >= UnicodeUtil.UNI_SUR_HIGH_START)
				{
					if (t.max > UnicodeUtil.UNI_SUR_LOW_END)
					{
						// after surrogates
						code = 1 + UnicodeUtil.UNI_SUR_LOW_END + r.Next(t.max - UnicodeUtil.UNI_SUR_LOW_END
							);
					}
					else
					{
						throw new ArgumentException("transition accepts only surrogates: " + t);
					}
				}
				else
				{
					if (t.max <= UnicodeUtil.UNI_SUR_LOW_END)
					{
						if (t.min < UnicodeUtil.UNI_SUR_HIGH_START)
						{
							// before surrogates
							code = t.min + r.Next(UnicodeUtil.UNI_SUR_HIGH_START - t.min);
						}
						else
						{
							throw new ArgumentException("transition accepts only surrogates: " + t);
						}
					}
					else
					{
						// range includes all surrogates
						int gap1 = UnicodeUtil.UNI_SUR_HIGH_START - t.min;
						int gap2 = t.max - UnicodeUtil.UNI_SUR_LOW_END;
						int c = r.Next(gap1 + gap2);
						if (c < gap1)
						{
							code = t.min + c;
						}
						else
						{
							code = UnicodeUtil.UNI_SUR_LOW_END + c - gap1 + 1;
						}
					}
				}
			}
			 
			//assert code >= t.min && code <= t.max && (code < UnicodeUtil.UNI_SUR_HIGH_START || code > UnicodeUtil.UNI_SUR_LOW_END):
			// "code=" + code + " min=" + t.min + " max=" + t.max;
			return code;
		}

		/// <summary>
		/// Lets you retrieve random strings accepted
		/// by an Automaton.
		/// </summary>
		/// <remarks>
		/// Lets you retrieve random strings accepted
		/// by an Automaton.
		/// <p>
		/// Once created, call
		/// <see cref="GetRandomAcceptedString(Sharpen.Random)">GetRandomAcceptedString(Sharpen.Random)
		/// 	</see>
		/// to get a new string (in UTF-32 codepoints).
		/// </remarks>
		public class RandomAcceptedStrings
		{
			private readonly IDictionary<Transition, bool> leadsToAccept;

			private readonly Lucene.Net.TestFramework.Util.Automaton.Automaton a;

			private class ArrivingTransition
			{
				internal readonly State from;

				internal readonly Transition t;

				public ArrivingTransition(State from, Transition t)
				{
					this.from = from;
					this.t = t;
				}
			}

			public RandomAcceptedStrings(Lucene.Net.TestFramework.Util.Automaton.Automaton a)
			{
				this.a = a;
				if (a.IsSingleton())
				{
					leadsToAccept = null;
					return;
				}
				// must use IdentityHashmap because two Transitions w/
				// different start nodes can be considered the same
				leadsToAccept = new IdentityHashMap<Transition, bool>();
				IDictionary<State, IList<AutomatonTestUtil.RandomAcceptedStrings.ArrivingTransition
					>> allArriving = new Dictionary<State, IList<AutomatonTestUtil.RandomAcceptedStrings.ArrivingTransition
					>>();
				List<State> q = new List<State>();
				ICollection<State> seen = new HashSet<State>();
				// reverse map the transitions, so we can quickly look
				// up all arriving transitions to a given state
				foreach (State s in a.GetNumberedStates())
				{
					for (int i = 0; i < s.numTransitions; i++)
					{
						Transition t = s.transitionsArray[i];
						IList<AutomatonTestUtil.RandomAcceptedStrings.ArrivingTransition> tl = allArriving
							.Get(t.to);
						if (tl == null)
						{
							tl = new List<AutomatonTestUtil.RandomAcceptedStrings.ArrivingTransition>();
							allArriving.Put(t.to, tl);
						}
						tl.Add(new AutomatonTestUtil.RandomAcceptedStrings.ArrivingTransition(s, t));
					}
					if (s.accept)
					{
						q.Add(s);
						seen.Add(s);
					}
				}
				// Breadth-first search, from accept states,
				// backwards:
				while (!q.IsEmpty())
				{
					State s_1 = q.RemoveFirst();
					IList<AutomatonTestUtil.RandomAcceptedStrings.ArrivingTransition> arriving = allArriving
						.Get(s_1);
					if (arriving != null)
					{
						foreach (AutomatonTestUtil.RandomAcceptedStrings.ArrivingTransition at in arriving)
						{
							State from = at.from;
							if (!seen.Contains(from))
							{
								q.Add(from);
								seen.Add(from);
								leadsToAccept.Put(at.t, true);
							}
						}
					}
				}
			}

			public virtual int[] GetRandomAcceptedString(Random r)
			{
				IList<int> soFar = new List<int>();
				if (a.IsSingleton())
				{
					// accepts only one
					string s = a.singleton;
					int charUpto = 0;
					while (charUpto < s.Length)
					{
						int cp = s.CodePointAt(charUpto);
						charUpto += char.CharCount(cp);
						soFar.Add(cp);
					}
				}
				else
				{
					State s = a.initial;
					while (true)
					{
						if (s.accept)
						{
							if (s.numTransitions == 0)
							{
								// stop now
								break;
							}
							else
							{
								if (r.NextBoolean())
								{
									break;
								}
							}
						}
						if (s.numTransitions == 0)
						{
							throw new SystemException("this automaton has dead states");
						}
						bool cheat = r.NextBoolean();
						Transition t;
						if (cheat)
						{
							// pick a transition that we know is the fastest
							// path to an accept state
							IList<Transition> toAccept = new List<Transition>();
							for (int i = 0; i < s.numTransitions; i++)
							{
								Transition t0 = s.transitionsArray[i];
								if (leadsToAccept.ContainsKey(t0))
								{
									toAccept.Add(t0);
								}
							}
							if (toAccept.Count == 0)
							{
								// this is OK -- it means we jumped into a cycle
								t = s.transitionsArray[r.Next(s.numTransitions)];
							}
							else
							{
								t = toAccept[r.Next(toAccept.Count)];
							}
						}
						else
						{
							t = s.transitionsArray[r.Next(s.numTransitions)];
						}
						soFar.Add(GetRandomCodePoint(r, t));
						s = t.to;
					}
				}
				return ArrayUtil.ToIntArray(soFar);
			}
		}

		/// <summary>return a random NFA/DFA for testing</summary>
		public static Lucene.Net.Util.Automaton.Automaton RandomAutomaton(Random random
			)
		{
			// get two random Automata from regexps
			Lucene.Net.TestFramework.Util.Automaton.Automaton a1 = new RegExp(AutomatonTestUtil.RandomRegexp
				(random), RegExp.NONE).ToAutomaton();
			if (random.NextBoolean())
			{
				a1 = BasicOperations.Complement(a1);
			}
			Lucene.Net.TestFramework.Util.Automaton.Automaton a2 = new RegExp(AutomatonTestUtil.RandomRegexp
				(random), RegExp.NONE).ToAutomaton();
			if (random.NextBoolean())
			{
				a2 = BasicOperations.Complement(a2);
			}
			switch (random.Next(4))
			{
				case 0:
				{
					// combine them in random ways
					return BasicOperations.Concatenate(a1, a2);
				}

				case 1:
				{
					return BasicOperations.Union(a1, a2);
				}

				case 2:
				{
					return BasicOperations.Intersection(a1, a2);
				}

				default:
				{
					return BasicOperations.Minus(a1, a2);
					break;
				}
			}
		}

		/// <summary>Simple, original brics implementation of Brzozowski minimize()</summary>
		public static void MinimizeSimple(Lucene.Net.TestFramework.Util.Automaton.Automaton a)
		{
			if (a.IsSingleton())
			{
				return;
			}
			DeterminizeSimple(a, SpecialOperations.Reverse(a));
			DeterminizeSimple(a, SpecialOperations.Reverse(a));
		}

		/// <summary>Simple, original brics implementation of determinize()</summary>
		public static void DeterminizeSimple(Lucene.Net.TestFramework.Util.Automaton.Automaton a
			)
		{
			if (a.deterministic || a.IsSingleton())
			{
				return;
			}
			ICollection<State> initialset = new HashSet<State>();
			initialset.Add(a.initial);
			DeterminizeSimple(a, initialset);
		}

		/// <summary>
		/// Simple, original brics implementation of determinize()
		/// Determinizes the given automaton using the given set of initial states.
		/// </summary>
		/// <remarks>
		/// Simple, original brics implementation of determinize()
		/// Determinizes the given automaton using the given set of initial states.
		/// </remarks>
		public static void DeterminizeSimple(Lucene.Net.TestFramework.Util.Automaton.Automaton a
			, ICollection<State> initialset)
		{
			int[] points = a.GetStartPoints();
			// subset construction
			IDictionary<ICollection<State>, ICollection<State>> sets = new Dictionary<ICollection
				<State>, ICollection<State>>();
			List<ICollection<State>> worklist = new List<ICollection<State>>();
			IDictionary<ICollection<State>, State> newstate = new Dictionary<ICollection<State
				>, State>();
			sets.Put(initialset, initialset);
			worklist.Add(initialset);
			a.initial = new State();
			newstate.Put(initialset, a.initial);
			while (worklist.Count > 0)
			{
				ICollection<State> s = worklist.RemoveFirst();
				State r = newstate.Get(s);
				foreach (State q in s)
				{
					if (q.accept)
					{
						r.accept = true;
						break;
					}
				}
				for (int n = 0; n < points.Length; n++)
				{
					ICollection<State> p = new HashSet<State>();
					foreach (State q_1 in s)
					{
						foreach (Transition t in q_1.GetTransitions())
						{
							if (t.min <= points[n] && points[n] <= t.max)
							{
								p.Add(t.to);
							}
						}
					}
					if (!sets.ContainsKey(p))
					{
						sets.Put(p, p);
						worklist.Add(p);
						newstate.Put(p, new State());
					}
					State q_2 = newstate.Get(p);
					int min = points[n];
					int max;
					if (n + 1 < points.Length)
					{
						max = points[n + 1] - 1;
					}
					else
					{
						max = char.MAX_CODE_POINT;
					}
					r.AddTransition(new Transition(min, max, q_2));
				}
			}
			a.deterministic = true;
			a.ClearNumberedStates();
			a.RemoveDeadTransitions();
		}

		/// <summary>Simple, original implementation of getFiniteStrings.</summary>
		/// <remarks>
		/// Simple, original implementation of getFiniteStrings.
		/// <p>Returns the set of accepted strings, assuming that at most
		/// <code>limit</code> strings are accepted. If more than <code>limit</code>
		/// strings are accepted, the first limit strings found are returned. If <code>limit</code>&lt;0, then
		/// the limit is infinite.
		/// <p>This implementation is recursive: it uses one stack
		/// frame for each digit in the returned strings (ie, max
		/// is the max length returned string).
		/// </remarks>
		public static ICollection<IntsRef> GetFiniteStringsRecursive(Lucene.Net.TestFramework.Util.Automaton.Automaton
			 a, int limit)
		{
			HashSet<IntsRef> strings = new HashSet<IntsRef>();
			if (a.IsSingleton())
			{
				if (limit > 0)
				{
					strings.Add(Lucene.Net.TestFramework.Util.Fst.Util.ToUTF32(a.singleton, new IntsRef(
						)));
				}
			}
			else
			{
				if (!GetFiniteStrings(a.initial, new HashSet<State>(), strings, new IntsRef(), limit
					))
				{
					return strings;
				}
			}
			return strings;
		}

		/// <summary>
		/// Returns the strings that can be produced from the given state, or
		/// false if more than <code>limit</code> strings are found.
		/// </summary>
		/// <remarks>
		/// Returns the strings that can be produced from the given state, or
		/// false if more than <code>limit</code> strings are found.
		/// <code>limit</code>&lt;0 means "infinite".
		/// </remarks>
		private static bool GetFiniteStrings(State s, HashSet<State> pathstates, HashSet<
			IntsRef> strings, IntsRef path, int limit)
		{
			pathstates.Add(s);
			foreach (Transition t in s.GetTransitions())
			{
				if (pathstates.Contains(t.to))
				{
					return false;
				}
				for (int n = t.min; n <= t.max; n++)
				{
					path.Grow(path.length + 1);
					path.ints[path.length] = n;
					path.length++;
					if (t.to.accept)
					{
						strings.Add(IntsRef.DeepCopyOf(path));
						if (limit >= 0 && strings.Count > limit)
						{
							return false;
						}
					}
					if (!GetFiniteStrings(t.to, pathstates, strings, path, limit))
					{
						return false;
					}
					path.length--;
				}
			}
			pathstates.Remove(s);
			return true;
		}

		/// <summary>Returns true if the language of this automaton is finite.</summary>
		/// <remarks>
		/// Returns true if the language of this automaton is finite.
		/// <p>
		/// WARNING: this method is slow, it will blow up if the automaton is large.
		/// this is only used to test the correctness of our faster implementation.
		/// </remarks>
		public static bool IsFiniteSlow(Lucene.Net.TestFramework.Util.Automaton.Automaton a)
		{
			if (a.IsSingleton())
			{
				return true;
			}
			return IsFiniteSlow(a.initial, new HashSet<State>());
		}

		/// <summary>Checks whether there is a loop containing s.</summary>
		/// <remarks>
		/// Checks whether there is a loop containing s. (This is sufficient since
		/// there are never transitions to dead states.)
		/// </remarks>
		private static bool IsFiniteSlow(State s, HashSet<State> path)
		{
			// TODO: not great that this is recursive... in theory a
			// large automata could exceed java's stack
			path.Add(s);
			foreach (Transition t in s.GetTransitions())
			{
				if (path.Contains(t.to) || !IsFiniteSlow(t.to, path))
				{
					return false;
				}
			}
			path.Remove(s);
			return true;
		}

		/// <summary>
		/// Checks that an automaton has no detached states that are unreachable
		/// from the initial state.
		/// </summary>
		/// <remarks>
		/// Checks that an automaton has no detached states that are unreachable
		/// from the initial state.
		/// </remarks>
		public static void AssertNoDetachedStates(Lucene.Net.TestFramework.Util.Automaton.Automaton
			 a)
		{
			int numStates = a.GetNumberOfStates();
			a.ClearNumberedStates();
		}
		// force recomputation of cached numbered states
		 
		//assert numStates == a.getNumberOfStates() : "automaton has " + (numStates - a.getNumberOfStates()) + " detached states";
	}
}
