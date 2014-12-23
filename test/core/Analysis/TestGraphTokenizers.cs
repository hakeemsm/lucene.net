/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Test.Analysis.TokenAttributes;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using Sharpen;

namespace Lucene.Net.Analysis
{
	public class TestGraphTokenizers : BaseTokenStreamTestCase
	{
		private class GraphTokenizer : Tokenizer
		{
			private IList<Token> tokens;

			private int upto;

			private int inputLength;

			private readonly CharTermAttribute termAtt = AddAttribute<CharTermAttribute>();

			private readonly OffsetAttribute offsetAtt = AddAttribute<OffsetAttribute>();

			private readonly PositionIncrementAttribute posIncrAtt = AddAttribute<PositionIncrementAttribute
				>();

			private readonly PositionLengthAttribute posLengthAtt = AddAttribute<PositionLengthAttribute
				>();

			protected GraphTokenizer(StreamReader input) : base(input)
			{
			}

			// Makes a graph TokenStream from the string; separate
			// positions with single space, multiple tokens at the same
			// position with /, and add optional position length with
			// :.  EG "a b c" is a simple chain, "a/x b c" adds 'x'
			// over 'a' at position 0 with posLen=1, "a/x:3 b c" adds
			// 'x' over a with posLen=3.  Tokens are in normal-form!
			// So, offsets are computed based on the first token at a
			// given position.  NOTE: each token must be a single
			// character!  We assume this when computing offsets...
			// NOTE: all input tokens must be length 1!!!  This means
			// you cannot turn on MockCharFilter when random
			// testing...
			/// <exception cref="System.IO.IOException"></exception>
			public override void Reset()
			{
				base.Reset();
				tokens = null;
				upto = 0;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override bool IncrementToken()
			{
				if (tokens == null)
				{
					FillTokens();
				}
				//System.out.println("graphTokenizer: incr upto=" + upto + " vs " + tokens.size());
				if (upto == tokens.Count)
				{
					//System.out.println("  END @ " + tokens.size());
					return false;
				}
				Token t = tokens[upto++];
				//System.out.println("  return token=" + t);
				ClearAttributes();
				termAtt.Append(t.ToString());
				offsetAtt.SetOffset(t.StartOffset(), t.EndOffset());
				posIncrAtt.SetPositionIncrement(t.GetPositionIncrement());
				posLengthAtt.SetPositionLength(t.GetPositionLength());
				return true;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void End()
			{
				base.End();
				// NOTE: somewhat... hackish, but we need this to
				// satisfy BTSTC:
				int lastOffset;
				if (tokens != null && !tokens.IsEmpty())
				{
					lastOffset = tokens[tokens.Count - 1].EndOffset();
				}
				else
				{
					lastOffset = 0;
				}
				offsetAtt.SetOffset(CorrectOffset(lastOffset), CorrectOffset(inputLength));
			}

			/// <exception cref="System.IO.IOException"></exception>
			private void FillTokens()
			{
				StringBuilder sb = new StringBuilder();
				char[] buffer = new char[256];
				while (true)
				{
					int count = input.Read(buffer);
					if (count == -1)
					{
						break;
					}
					sb.Append(buffer, 0, count);
				}
				//System.out.println("got count=" + count);
				//System.out.println("fillTokens: " + sb);
				inputLength = sb.Length;
				string[] parts = sb.ToString().Split(" ");
				tokens = new AList<Token>();
				int pos = 0;
				int maxPos = -1;
				int offset = 0;
				//System.out.println("again");
				foreach (string part in parts)
				{
					string[] overlapped = part.Split("/");
					bool firstAtPos = true;
					int minPosLength = int.MaxValue;
					foreach (string part2 in overlapped)
					{
						int colonIndex = part2.IndexOf(':');
						string token;
						int posLength;
						if (colonIndex != -1)
						{
							token = Sharpen.Runtime.Substring(part2, 0, colonIndex);
							posLength = System.Convert.ToInt32(Sharpen.Runtime.Substring(part2, 1 + colonIndex
								));
						}
						else
						{
							token = part2;
							posLength = 1;
						}
						maxPos = Math.Max(maxPos, pos + posLength);
						minPosLength = Math.Min(minPosLength, posLength);
						Token t = new Token(token, offset, offset + 2 * posLength - 1);
						t.SetPositionLength(posLength);
						t.SetPositionIncrement(firstAtPos ? 1 : 0);
						firstAtPos = false;
						//System.out.println("  add token=" + t + " startOff=" + t.startOffset() + " endOff=" + t.endOffset());
						tokens.AddItem(t);
					}
					pos += minPosLength;
					offset = 2 * pos;
				}
			}
			//HM:revisit 
			//assert maxPos <= pos: "input string mal-formed: posLength>1 tokens hang over the end";
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestMockGraphTokenFilterBasic()
		{
			for (int iter = 0; iter < 10 * RANDOM_MULTIPLIER; iter++)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("\nTEST: iter=" + iter);
				}
				// Make new analyzer each time, because MGTF has fixed
				// seed:
				Analyzer a = new _Analyzer_177();
				CheckAnalysisConsistency(Random(), a, false, "a b c d e f g h i j k");
			}
		}

		private sealed class _Analyzer_177 : Analyzer
		{
			public _Analyzer_177()
			{
			}

			protected override Analyzer.TokenStreamComponents CreateComponents(string fieldName
				, StreamReader reader)
			{
				Tokenizer t = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
				TokenStream t2 = new MockGraphTokenFilter(LuceneTestCase.Random(), t);
				return new Analyzer.TokenStreamComponents(t, t2);
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestMockGraphTokenFilterOnGraphInput()
		{
			for (int iter = 0; iter < 100 * RANDOM_MULTIPLIER; iter++)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("\nTEST: iter=" + iter);
				}
				// Make new analyzer each time, because MGTF has fixed
				// seed:
				Analyzer a = new _Analyzer_199();
				CheckAnalysisConsistency(Random(), a, false, "a/x:3 c/y:2 d e f/z:4 g h i j k");
			}
		}

		private sealed class _Analyzer_199 : Analyzer
		{
			public _Analyzer_199()
			{
			}

			protected override Analyzer.TokenStreamComponents CreateComponents(string fieldName
				, StreamReader reader)
			{
				Tokenizer t = new TestGraphTokenizers.GraphTokenizer(reader);
				TokenStream t2 = new MockGraphTokenFilter(LuceneTestCase.Random(), t);
				return new Analyzer.TokenStreamComponents(t, t2);
			}
		}

		private sealed class RemoveATokens : TokenFilter
		{
			private int pendingPosInc;

			private readonly CharTermAttribute termAtt = AddAttribute<CharTermAttribute>();

			private readonly PositionIncrementAttribute posIncAtt = AddAttribute<PositionIncrementAttribute
				>();

			protected RemoveATokens(TokenStream @in) : base(@in)
			{
			}

			// Just deletes (leaving hole) token 'a':
			/// <exception cref="System.IO.IOException"></exception>
			public override void Reset()
			{
				base.Reset();
				pendingPosInc = 0;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void End()
			{
				base.End();
				posIncAtt.SetPositionIncrement(pendingPosInc + posIncAtt.GetPositionIncrement());
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override bool IncrementToken()
			{
				while (true)
				{
					bool gotOne = input.IncrementToken();
					if (!gotOne)
					{
						return false;
					}
					else
					{
						if (termAtt.ToString().Equals("a"))
						{
							pendingPosInc += posIncAtt.GetPositionIncrement();
						}
						else
						{
							posIncAtt.SetPositionIncrement(pendingPosInc + posIncAtt.GetPositionIncrement());
							pendingPosInc = 0;
							return true;
						}
					}
				}
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestMockGraphTokenFilterBeforeHoles()
		{
			for (int iter = 0; iter < 100 * RANDOM_MULTIPLIER; iter++)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("\nTEST: iter=" + iter);
				}
				// Make new analyzer each time, because MGTF has fixed
				// seed:
				Analyzer a = new _Analyzer_261();
				Random random = Random();
				CheckAnalysisConsistency(random, a, false, "a b c d e f g h i j k");
				CheckAnalysisConsistency(random, a, false, "x y a b c d e f g h i j k");
				CheckAnalysisConsistency(random, a, false, "a b c d e f g h i j k a");
				CheckAnalysisConsistency(random, a, false, "a b c d e f g h i j k a x y");
			}
		}

		private sealed class _Analyzer_261 : Analyzer
		{
			public _Analyzer_261()
			{
			}

			protected override Analyzer.TokenStreamComponents CreateComponents(string fieldName
				, StreamReader reader)
			{
				Tokenizer t = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
				TokenStream t2 = new MockGraphTokenFilter(LuceneTestCase.Random(), t);
				TokenStream t3 = new TestGraphTokenizers.RemoveATokens(t2);
				return new Analyzer.TokenStreamComponents(t, t3);
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestMockGraphTokenFilterAfterHoles()
		{
			for (int iter = 0; iter < 100 * RANDOM_MULTIPLIER; iter++)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("\nTEST: iter=" + iter);
				}
				// Make new analyzer each time, because MGTF has fixed
				// seed:
				Analyzer a = new _Analyzer_288();
				Random random = Random();
				CheckAnalysisConsistency(random, a, false, "a b c d e f g h i j k");
				CheckAnalysisConsistency(random, a, false, "x y a b c d e f g h i j k");
				CheckAnalysisConsistency(random, a, false, "a b c d e f g h i j k a");
				CheckAnalysisConsistency(random, a, false, "a b c d e f g h i j k a x y");
			}
		}

		private sealed class _Analyzer_288 : Analyzer
		{
			public _Analyzer_288()
			{
			}

			protected override Analyzer.TokenStreamComponents CreateComponents(string fieldName
				, StreamReader reader)
			{
				Tokenizer t = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
				TokenStream t2 = new TestGraphTokenizers.RemoveATokens(t);
				TokenStream t3 = new MockGraphTokenFilter(LuceneTestCase.Random(), t2);
				return new Analyzer.TokenStreamComponents(t, t3);
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestMockGraphTokenFilterRandom()
		{
			for (int iter = 0; iter < 10 * RANDOM_MULTIPLIER; iter++)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("\nTEST: iter=" + iter);
				}
				// Make new analyzer each time, because MGTF has fixed
				// seed:
				Analyzer a = new _Analyzer_315();
				Random random = Random();
				CheckRandomData(random, a, 5, AtLeast(100));
			}
		}

		private sealed class _Analyzer_315 : Analyzer
		{
			public _Analyzer_315()
			{
			}

			protected override Analyzer.TokenStreamComponents CreateComponents(string fieldName
				, StreamReader reader)
			{
				Tokenizer t = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
				TokenStream t2 = new MockGraphTokenFilter(LuceneTestCase.Random(), t);
				return new Analyzer.TokenStreamComponents(t, t2);
			}
		}

		// Two MockGraphTokenFilters
		/// <exception cref="System.Exception"></exception>
		public virtual void TestDoubleMockGraphTokenFilterRandom()
		{
			for (int iter = 0; iter < 10 * RANDOM_MULTIPLIER; iter++)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("\nTEST: iter=" + iter);
				}
				// Make new analyzer each time, because MGTF has fixed
				// seed:
				Analyzer a = new _Analyzer_339();
				Random random = Random();
				CheckRandomData(random, a, 5, AtLeast(100));
			}
		}

		private sealed class _Analyzer_339 : Analyzer
		{
			public _Analyzer_339()
			{
			}

			protected override Analyzer.TokenStreamComponents CreateComponents(string fieldName
				, StreamReader reader)
			{
				Tokenizer t = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
				TokenStream t1 = new MockGraphTokenFilter(LuceneTestCase.Random(), t);
				TokenStream t2 = new MockGraphTokenFilter(LuceneTestCase.Random(), t1);
				return new Analyzer.TokenStreamComponents(t, t2);
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestMockGraphTokenFilterBeforeHolesRandom()
		{
			for (int iter = 0; iter < 10 * RANDOM_MULTIPLIER; iter++)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("\nTEST: iter=" + iter);
				}
				// Make new analyzer each time, because MGTF has fixed
				// seed:
				Analyzer a = new _Analyzer_363();
				Random random = Random();
				CheckRandomData(random, a, 5, AtLeast(100));
			}
		}

		private sealed class _Analyzer_363 : Analyzer
		{
			public _Analyzer_363()
			{
			}

			protected override Analyzer.TokenStreamComponents CreateComponents(string fieldName
				, StreamReader reader)
			{
				Tokenizer t = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
				TokenStream t1 = new MockGraphTokenFilter(LuceneTestCase.Random(), t);
				TokenStream t2 = new MockHoleInjectingTokenFilter(LuceneTestCase.Random(), t1);
				return new Analyzer.TokenStreamComponents(t, t2);
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestMockGraphTokenFilterAfterHolesRandom()
		{
			for (int iter = 0; iter < 10 * RANDOM_MULTIPLIER; iter++)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("\nTEST: iter=" + iter);
				}
				// Make new analyzer each time, because MGTF has fixed
				// seed:
				Analyzer a = new _Analyzer_387();
				Random random = Random();
				CheckRandomData(random, a, 5, AtLeast(100));
			}
		}

		private sealed class _Analyzer_387 : Analyzer
		{
			public _Analyzer_387()
			{
			}

			protected override Analyzer.TokenStreamComponents CreateComponents(string fieldName
				, StreamReader reader)
			{
				Tokenizer t = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
				TokenStream t1 = new MockHoleInjectingTokenFilter(LuceneTestCase.Random(), t);
				TokenStream t2 = new MockGraphTokenFilter(LuceneTestCase.Random(), t1);
				return new Analyzer.TokenStreamComponents(t, t2);
			}
		}

		private static Token Token(string term, int posInc, int posLength)
		{
			Token t = new Token(term, 0, 0);
			t.SetPositionIncrement(posInc);
			t.SetPositionLength(posLength);
			return t;
		}

		private static Token Token(string term, int posInc, int posLength, int startOffset
			, int endOffset)
		{
			Token t = new Token(term, startOffset, endOffset);
			t.SetPositionIncrement(posInc);
			t.SetPositionLength(posLength);
			return t;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSingleToken()
		{
			TokenStream ts = new CannedTokenStream(new Token[] { Token("abc", 1, 1) });
			Lucene.Net.Util.Automaton.Automaton actual = (new TokenStreamToAutomaton()
				).ToAutomaton(ts);
			Lucene.Net.Util.Automaton.Automaton expected = BasicAutomata.MakeString("abc"
				);
			NUnit.Framework.Assert.IsTrue(BasicOperations.SameLanguage(expected, actual));
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestMultipleHoles()
		{
			TokenStream ts = new CannedTokenStream(new Token[] { Token("a", 1, 1), Token("b", 
				3, 1) });
			Lucene.Net.Util.Automaton.Automaton actual = (new TokenStreamToAutomaton()
				).ToAutomaton(ts);
			Lucene.Net.Util.Automaton.Automaton expected = Join(S2a("a"), SEP_A, HOLE_A
				, SEP_A, HOLE_A, SEP_A, S2a("b"));
			NUnit.Framework.Assert.IsTrue(BasicOperations.SameLanguage(expected, actual));
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSynOverMultipleHoles()
		{
			TokenStream ts = new CannedTokenStream(new Token[] { Token("a", 1, 1), Token("x", 
				0, 3), Token("b", 3, 1) });
			Lucene.Net.Util.Automaton.Automaton actual = (new TokenStreamToAutomaton()
				).ToAutomaton(ts);
			Lucene.Net.Util.Automaton.Automaton a1 = Join(S2a("a"), SEP_A, HOLE_A, SEP_A
				, HOLE_A, SEP_A, S2a("b"));
			Lucene.Net.Util.Automaton.Automaton a2 = Join(S2a("x"), SEP_A, S2a("b"));
			Lucene.Net.Util.Automaton.Automaton expected = BasicOperations.Union(a1, a2
				);
			NUnit.Framework.Assert.IsTrue(BasicOperations.SameLanguage(expected, actual));
		}

		private static readonly Lucene.Net.Util.Automaton.Automaton SEP_A = BasicAutomata
			.MakeChar(TokenStreamToAutomaton.POS_SEP);

		private static readonly Lucene.Net.Util.Automaton.Automaton HOLE_A = BasicAutomata
			.MakeChar(TokenStreamToAutomaton.HOLE);

		// for debugging!
		private Lucene.Net.Util.Automaton.Automaton Join(params string[] strings)
		{
			IList<Lucene.Net.Util.Automaton.Automaton> @as = new AList<Lucene.Net.Util.Automaton.Automaton
				>();
			foreach (string s in strings)
			{
				@as.AddItem(BasicAutomata.MakeString(s));
				@as.AddItem(SEP_A);
			}
			@as.Remove(@as.Count - 1);
			return BasicOperations.Concatenate(@as);
		}

		private Lucene.Net.Util.Automaton.Automaton Join(params Lucene.Net.Util.Automaton.Automaton
			[] @as)
		{
			return BasicOperations.Concatenate(Arrays.AsList(@as));
		}

		private Lucene.Net.Util.Automaton.Automaton S2a(string s)
		{
			return BasicAutomata.MakeString(s);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestTwoTokens()
		{
			TokenStream ts = new CannedTokenStream(new Token[] { Token("abc", 1, 1), Token("def"
				, 1, 1) });
			Lucene.Net.Util.Automaton.Automaton actual = (new TokenStreamToAutomaton()
				).ToAutomaton(ts);
			Lucene.Net.Util.Automaton.Automaton expected = Join("abc", "def");
			//toDot(actual);
			NUnit.Framework.Assert.IsTrue(BasicOperations.SameLanguage(expected, actual));
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestHole()
		{
			TokenStream ts = new CannedTokenStream(new Token[] { Token("abc", 1, 1), Token("def"
				, 2, 1) });
			Lucene.Net.Util.Automaton.Automaton actual = (new TokenStreamToAutomaton()
				).ToAutomaton(ts);
			Lucene.Net.Util.Automaton.Automaton expected = Join(S2a("abc"), SEP_A, HOLE_A
				, SEP_A, S2a("def"));
			//toDot(actual);
			NUnit.Framework.Assert.IsTrue(BasicOperations.SameLanguage(expected, actual));
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestOverlappedTokensSausage()
		{
			// Two tokens on top of each other (sausage):
			TokenStream ts = new CannedTokenStream(new Token[] { Token("abc", 1, 1), Token("xyz"
				, 0, 1) });
			Lucene.Net.Util.Automaton.Automaton actual = (new TokenStreamToAutomaton()
				).ToAutomaton(ts);
			Lucene.Net.Util.Automaton.Automaton a1 = BasicAutomata.MakeString("abc");
			Lucene.Net.Util.Automaton.Automaton a2 = BasicAutomata.MakeString("xyz");
			Lucene.Net.Util.Automaton.Automaton expected = BasicOperations.Union(a1, a2
				);
			NUnit.Framework.Assert.IsTrue(BasicOperations.SameLanguage(expected, actual));
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestOverlappedTokensLattice()
		{
			TokenStream ts = new CannedTokenStream(new Token[] { Token("abc", 1, 1), Token("xyz"
				, 0, 2), Token("def", 1, 1) });
			Lucene.Net.Util.Automaton.Automaton actual = (new TokenStreamToAutomaton()
				).ToAutomaton(ts);
			Lucene.Net.Util.Automaton.Automaton a1 = BasicAutomata.MakeString("xyz");
			Lucene.Net.Util.Automaton.Automaton a2 = Join("abc", "def");
			Lucene.Net.Util.Automaton.Automaton expected = BasicOperations.Union(a1, a2
				);
			//toDot(actual);
			NUnit.Framework.Assert.IsTrue(BasicOperations.SameLanguage(expected, actual));
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSynOverHole()
		{
			TokenStream ts = new CannedTokenStream(new Token[] { Token("a", 1, 1), Token("X", 
				0, 2), Token("b", 2, 1) });
			Lucene.Net.Util.Automaton.Automaton actual = (new TokenStreamToAutomaton()
				).ToAutomaton(ts);
			Lucene.Net.Util.Automaton.Automaton a1 = BasicOperations.Union(Join(S2a("a"
				), SEP_A, HOLE_A), BasicAutomata.MakeString("X"));
			Lucene.Net.Util.Automaton.Automaton expected = BasicOperations.Concatenate
				(a1, Join(SEP_A, S2a("b")));
			//toDot(actual);
			NUnit.Framework.Assert.IsTrue(BasicOperations.SameLanguage(expected, actual));
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSynOverHole2()
		{
			TokenStream ts = new CannedTokenStream(new Token[] { Token("xyz", 1, 1), Token("abc"
				, 0, 3), Token("def", 2, 1) });
			Lucene.Net.Util.Automaton.Automaton actual = (new TokenStreamToAutomaton()
				).ToAutomaton(ts);
			Lucene.Net.Util.Automaton.Automaton expected = BasicOperations.Union(Join(
				S2a("xyz"), SEP_A, HOLE_A, SEP_A, S2a("def")), BasicAutomata.MakeString("abc"));
			NUnit.Framework.Assert.IsTrue(BasicOperations.SameLanguage(expected, actual));
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestOverlappedTokensLattice2()
		{
			TokenStream ts = new CannedTokenStream(new Token[] { Token("abc", 1, 1), Token("xyz"
				, 0, 3), Token("def", 1, 1), Token("ghi", 1, 1) });
			Lucene.Net.Util.Automaton.Automaton actual = (new TokenStreamToAutomaton()
				).ToAutomaton(ts);
			Lucene.Net.Util.Automaton.Automaton a1 = BasicAutomata.MakeString("xyz");
			Lucene.Net.Util.Automaton.Automaton a2 = Join("abc", "def", "ghi");
			Lucene.Net.Util.Automaton.Automaton expected = BasicOperations.Union(a1, a2
				);
			//toDot(actual);
			NUnit.Framework.Assert.IsTrue(BasicOperations.SameLanguage(expected, actual));
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestToDot()
		{
			TokenStream ts = new CannedTokenStream(new Token[] { Token("abc", 1, 1, 0, 4) });
			StringWriter w = new StringWriter();
			new TokenStreamToDot("abcd", ts, new PrintWriter(w)).ToDot();
			NUnit.Framework.Assert.IsTrue(w.ToString().IndexOf("abc / abcd") != -1);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestStartsWithHole()
		{
			TokenStream ts = new CannedTokenStream(new Token[] { Token("abc", 2, 1) });
			Lucene.Net.Util.Automaton.Automaton actual = (new TokenStreamToAutomaton()
				).ToAutomaton(ts);
			Lucene.Net.Util.Automaton.Automaton expected = Join(HOLE_A, SEP_A, S2a("abc"
				));
			//toDot(actual);
			NUnit.Framework.Assert.IsTrue(BasicOperations.SameLanguage(expected, actual));
		}

		// TODO: testEndsWithHole... but we need posInc to set in TS.end()
		/// <exception cref="System.Exception"></exception>
		public virtual void TestSynHangingOverEnd()
		{
			TokenStream ts = new CannedTokenStream(new Token[] { Token("a", 1, 1), Token("X", 
				0, 10) });
			Lucene.Net.Util.Automaton.Automaton actual = (new TokenStreamToAutomaton()
				).ToAutomaton(ts);
			Lucene.Net.Util.Automaton.Automaton expected = BasicOperations.Union(BasicAutomata
				.MakeString("a"), BasicAutomata.MakeString("X"));
			NUnit.Framework.Assert.IsTrue(BasicOperations.SameLanguage(expected, actual));
		}
	}
}
