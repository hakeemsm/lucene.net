using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Support;
using Lucene.Net.TestFramework.Analysis;
using Lucene.Net.Util.Automaton;
using NUnit.Framework;

namespace Lucene.Net.Test.Analysis
{
    [TestFixture]
	public class TestGraphTokenizers : BaseTokenStreamTestCase
	{
		private class GraphTokenizer : Tokenizer
		{
			private IList<Token> tokens;

			private int upto;

			private int inputLength;

		    private CharTermAttribute termAtt;

		    private OffsetAttribute offsetAtt;

		    private PositionIncrementAttribute posIncrAtt;

		    private PositionLengthAttribute posLengthAtt;

			protected internal GraphTokenizer(TextReader input) : base(input)
			{
			    InitAttributes();
			}

		    private void InitAttributes()
		    {
		        termAtt = AddAttribute<CharTermAttribute>();
		        offsetAtt = AddAttribute<OffsetAttribute>();
		        posIncrAtt = AddAttribute<PositionIncrementAttribute>();
		        posLengthAtt = AddAttribute<PositionLengthAttribute>();
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
				offsetAtt.SetOffset(t.StartOffset, t.EndOffset);
				posIncrAtt.PositionIncrement = t.PositionIncrement;
				posLengthAtt.PositionLength = t.PositionLength;
				return true;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void End()
			{
				base.End();
				// NOTE: somewhat... hackish, but we need this to
				// satisfy BTSTC:
				int lastOffset;
				if (tokens != null && tokens.Any())
				{
					lastOffset = tokens[tokens.Count - 1].EndOffset;
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
					int count = input.Read(buffer,0,256);
					if (count == -1)
					{
						break;
					}
					sb.Append(buffer, 0, count);
				}
				//System.out.println("got count=" + count);
				//System.out.println("fillTokens: " + sb);
				inputLength = sb.Length;
				string[] parts = sb.ToString().Split(new []{' '});
				tokens = new List<Token>();
				int pos = 0;
				int maxPos = -1;
				int offset = 0;
				//System.out.println("again");
				foreach (string part in parts)
				{
					string[] overlapped = part.Split(new []{'/'});
					bool firstAtPos = true;
					int minPosLength = int.MaxValue;
					foreach (string part2 in overlapped)
					{
						int colonIndex = part2.IndexOf(':');
						string token;
						int posLength;
						if (colonIndex != -1)
						{
							token = part2.Substring(0, colonIndex);
							posLength = Convert.ToInt32(part2.Substring(1 + colonIndex));
						}
						else
						{
							token = part2;
							posLength = 1;
						}
						maxPos = Math.Max(maxPos, pos + posLength);
						minPosLength = Math.Min(minPosLength, posLength);
						Token t = new Token(token, offset, offset + 2 * posLength - 1);
						t.PositionLength = posLength;
						t.PositionIncrement = firstAtPos ? 1 : 0;
						firstAtPos = false;
						//System.out.println("  add token=" + t + " startOff=" + t.startOffset() + " endOff=" + t.endOffset());
						tokens.Add(t);
					}
					pos += minPosLength;
					offset = 2 * pos;
				}
			}
			
			//assert maxPos <= pos: "input string mal-formed: posLength>1 tokens hang over the end";
		}

		[Test]
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
				Analyzer a = new AnonymousAnalyzer();
				CheckAnalysisConsistency(Random(), a, false, "a b c d e f g h i j k");
			}
		}

		private sealed class AnonymousAnalyzer : Analyzer
		{
		    public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
			{
				Tokenizer t = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
				TokenStream t2 = new MockGraphTokenFilter(Random(), t);
				return new TokenStreamComponents(t, t2);
			}
		}

		[Test]
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
				Analyzer a = new AnonymousAnalyzer2();
				CheckAnalysisConsistency(Random(), a, false, "a/x:3 c/y:2 d e f/z:4 g h i j k");
			}
		}

		private sealed class AnonymousAnalyzer2 : Analyzer
		{
		    public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
			{
				Tokenizer t = new GraphTokenizer(reader);
				TokenStream t2 = new MockGraphTokenFilter(LuceneTestCase.Random(), t);
				return new Analyzer.TokenStreamComponents(t, t2);
			}
		}

		private sealed class RemoveATokens : TokenFilter
		{
			private int pendingPosInc;

		    private CharTermAttribute termAtt;

		    private PositionIncrementAttribute posIncAtt;

			protected internal RemoveATokens(TokenStream ts) : base(ts)
			{
			    InitAttributes();
			}

		    private void InitAttributes()
		    {
                termAtt = AddAttribute<CharTermAttribute>();
                posIncAtt = AddAttribute<PositionIncrementAttribute>();
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
				posIncAtt.PositionIncrement = pendingPosInc + posIncAtt.PositionIncrement;
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
							pendingPosInc += posIncAtt.PositionIncrement;
						}
						else
						{
							posIncAtt.PositionIncrement = pendingPosInc + posIncAtt.PositionIncrement;
							pendingPosInc = 0;
							return true;
						}
					}
				}
			}
		}

		[Test]
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
				Analyzer a = new AnonymousAnalyzer3();
				Random random = Random();
				CheckAnalysisConsistency(random, a, false, "a b c d e f g h i j k");
				CheckAnalysisConsistency(random, a, false, "x y a b c d e f g h i j k");
				CheckAnalysisConsistency(random, a, false, "a b c d e f g h i j k a");
				CheckAnalysisConsistency(random, a, false, "a b c d e f g h i j k a x y");
			}
		}

		private sealed class AnonymousAnalyzer3 : Analyzer
		{
		    public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
			{
				Tokenizer t = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
				TokenStream t2 = new MockGraphTokenFilter(Random(), t);
				TokenStream t3 = new RemoveATokens(t2);
				return new TokenStreamComponents(t, t3);
			}
		}

		[Test]
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
				Analyzer a = new AnonymousAnalyzer4();
				Random random = Random();
				CheckAnalysisConsistency(random, a, false, "a b c d e f g h i j k");
				CheckAnalysisConsistency(random, a, false, "x y a b c d e f g h i j k");
				CheckAnalysisConsistency(random, a, false, "a b c d e f g h i j k a");
				CheckAnalysisConsistency(random, a, false, "a b c d e f g h i j k a x y");
			}
		}

		private sealed class AnonymousAnalyzer4 : Analyzer
		{
		    public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
			{
				Tokenizer t = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
				TokenStream t2 = new RemoveATokens(t);
				TokenStream t3 = new MockGraphTokenFilter(Random(), t2);
				return new TokenStreamComponents(t, t3);
			}
		}

		[Test]
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
				Analyzer a = new AnonymousAnalyzer5();
				Random random = Random();
				CheckRandomData(random, a, 5, AtLeast(100));
			}
		}

		private sealed class AnonymousAnalyzer5 : Analyzer
		{
		    public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
			{
				Tokenizer t = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
				TokenStream t2 = new MockGraphTokenFilter(LuceneTestCase.Random(), t);
				return new Analyzer.TokenStreamComponents(t, t2);
			}
		}

		[Test]
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
				Analyzer a = new AnonymousAnalyzer6();
				Random random = Random();
				CheckRandomData(random, a, 5, AtLeast(100));
			}
		}

		private sealed class AnonymousAnalyzer6 : Analyzer
		{

		    public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
			{
				Tokenizer t = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
				TokenStream t1 = new MockGraphTokenFilter(LuceneTestCase.Random(), t);
				TokenStream t2 = new MockGraphTokenFilter(LuceneTestCase.Random(), t1);
				return new Analyzer.TokenStreamComponents(t, t2);
			}
		}

		[Test]
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
				Analyzer a = new AnonymousAnalyzer7();
				Random random = Random();
				CheckRandomData(random, a, 5, AtLeast(100));
			}
		}

		private sealed class AnonymousAnalyzer7 : Analyzer
		{
		    public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
			{
				Tokenizer t = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
				TokenStream t1 = new MockGraphTokenFilter(LuceneTestCase.Random(), t);
				TokenStream t2 = new MockHoleInjectingTokenFilter(Random(), t1);
				return new Analyzer.TokenStreamComponents(t, t2);
			}
		}

		[Test]
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
				Analyzer a = new AnonymousAnalyzer8();
				Random random = Random();
				CheckRandomData(random, a, 5, AtLeast(100));
			}
		}

		private sealed class AnonymousAnalyzer8 : Analyzer
		{
		    public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
			{
				Tokenizer t = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
				TokenStream t1 = new MockHoleInjectingTokenFilter(LuceneTestCase.Random(), t);
				TokenStream t2 = new MockGraphTokenFilter(LuceneTestCase.Random(), t1);
				return new Analyzer.TokenStreamComponents(t, t2);
			}
		}

		private static Token Token(string term, int posInc, int posLength)
		{
			return new Token(term, 0, 0) {PositionIncrement = posInc, PositionLength = posLength};
		}

		private static Token Token(string term, int posInc, int posLength, int startOffset
			, int endOffset)
		{
			return new Token(term, startOffset, endOffset) {PositionIncrement = posInc, PositionLength = posLength};
		   
		}

		[Test]
		public virtual void TestSingleToken()
		{
			TokenStream ts = new CannedTokenStream(new Token[] { Token("abc", 1, 1) });
			Automaton actual = (new TokenStreamToAutomaton()
				).ToAutomaton(ts);
			Automaton expected = BasicAutomata.MakeString("abc"
				);
			IsTrue(BasicOperations.SameLanguage(expected, actual));
		}

		[Test]
		public virtual void TestMultipleHoles()
		{
			TokenStream ts = new CannedTokenStream(new Token[] { Token("a", 1, 1), Token("b", 
				3, 1) });
			Automaton actual = (new TokenStreamToAutomaton()
				).ToAutomaton(ts);
			Automaton expected = Join(S2a("a"), SEP_A, HOLE_A
				, SEP_A, HOLE_A, SEP_A, S2a("b"));
			IsTrue(BasicOperations.SameLanguage(expected, actual));
		}

		[Test]
		public virtual void TestSynOverMultipleHoles()
		{
			TokenStream ts = new CannedTokenStream(new Token[] { Token("a", 1, 1), Token("x", 
				0, 3), Token("b", 3, 1) });
			Automaton actual = (new TokenStreamToAutomaton()
				).ToAutomaton(ts);
			Automaton a1 = Join(S2a("a"), SEP_A, HOLE_A, SEP_A
				, HOLE_A, SEP_A, S2a("b"));
			Automaton a2 = Join(S2a("x"), SEP_A, S2a("b"));
			Automaton expected = BasicOperations.Union(a1, a2
				);
			IsTrue(BasicOperations.SameLanguage(expected, actual));
		}

		private static readonly Automaton SEP_A = BasicAutomata
			.MakeChar(TokenStreamToAutomaton.POS_SEP);

		private static readonly Automaton HOLE_A = BasicAutomata
			.MakeChar(TokenStreamToAutomaton.HOLE);

		// for debugging!
		private Automaton Join(params string[] strings)
		{
			var aList = new List<Automaton>();
			foreach (string s in strings)
			{
				aList.Add(BasicAutomata.MakeString(s));
				aList.Add(SEP_A);
			}
			aList.RemoveAt(aList.Count - 1);
			return BasicOperations.Concatenate(aList);
		}

		private Automaton Join(params Automaton[] aList)
		{
			return BasicOperations.Concatenate(Arrays.AsList(aList));
		}

		private Automaton S2a(string s)
		{
			return BasicAutomata.MakeString(s);
		}

		[Test]
		public virtual void TestTwoTokens()
		{
			TokenStream ts = new CannedTokenStream(new Token[] { Token("abc", 1, 1), Token("def", 1, 1) });
			var actual = (new TokenStreamToAutomaton()).ToAutomaton(ts);
			var expected = Join("abc", "def");
			//toDot(actual);
			IsTrue(BasicOperations.SameLanguage(expected, actual));
		}

		[Test]
		public virtual void TestHole()
		{
			TokenStream ts = new CannedTokenStream(new Token[] { Token("abc", 1, 1), Token("def", 2, 1) });
			var actual = (new TokenStreamToAutomaton()).ToAutomaton(ts);
			var expected = Join(S2a("abc"), SEP_A, HOLE_A, SEP_A, S2a("def"));
			//toDot(actual);
			IsTrue(BasicOperations.SameLanguage(expected, actual));
		}

		[Test]
		public virtual void TestOverlappedTokensSausage()
		{
			// Two tokens on top of each other (sausage):
			TokenStream ts = new CannedTokenStream(new Token[] { Token("abc", 1, 1), Token("xyz", 0, 1) });
			Automaton actual = (new TokenStreamToAutomaton()).ToAutomaton(ts);
			var a1 = BasicAutomata.MakeString("abc");
			var a2 = BasicAutomata.MakeString("xyz");
			var expected = BasicOperations.Union(a1, a2);
			IsTrue(BasicOperations.SameLanguage(expected, actual));
		}

		[Test]
		public virtual void TestOverlappedTokensLattice()
		{
			TokenStream ts = new CannedTokenStream(new Token[] { Token("abc", 1, 1), Token("xyz"
				, 0, 2), Token("def", 1, 1) });
			Automaton actual = (new TokenStreamToAutomaton()).ToAutomaton(ts);
			Automaton a1 = BasicAutomata.MakeString("xyz");
			Automaton a2 = Join("abc", "def");
			Automaton expected = BasicOperations.Union(a1, a2);
			//toDot(actual);
			IsTrue(BasicOperations.SameLanguage(expected, actual));
		}

		[Test]
		public virtual void TestSynOverHole()
		{
			TokenStream ts = new CannedTokenStream(new Token[] { Token("a", 1, 1), Token("X", 
				0, 2), Token("b", 2, 1) });
			Automaton actual = (new TokenStreamToAutomaton()
				).ToAutomaton(ts);
			Automaton a1 = BasicOperations.Union(Join(S2a("a"
				), SEP_A, HOLE_A), BasicAutomata.MakeString("X"));
			Automaton expected = BasicOperations.Concatenate
				(a1, Join(SEP_A, S2a("b")));
			//toDot(actual);
			IsTrue(BasicOperations.SameLanguage(expected, actual));
		}

		[Test]
		public virtual void TestSynOverHole2()
		{
			TokenStream ts = new CannedTokenStream(new Token[] { Token("xyz", 1, 1), Token("abc"
				, 0, 3), Token("def", 2, 1) });
			Automaton actual = (new TokenStreamToAutomaton()
				).ToAutomaton(ts);
			Automaton expected = BasicOperations.Union(Join(
				S2a("xyz"), SEP_A, HOLE_A, SEP_A, S2a("def")), BasicAutomata.MakeString("abc"));
			IsTrue(BasicOperations.SameLanguage(expected, actual));
		}

		[Test]
		public virtual void TestOverlappedTokensLattice2()
		{
			TokenStream ts = new CannedTokenStream(new Token[] { Token("abc", 1, 1), Token("xyz"
				, 0, 3), Token("def", 1, 1), Token("ghi", 1, 1) });
			Automaton actual = (new TokenStreamToAutomaton()
				).ToAutomaton(ts);
			Automaton a1 = BasicAutomata.MakeString("xyz");
			Automaton a2 = Join("abc", "def", "ghi");
			Automaton expected = BasicOperations.Union(a1, a2
				);
			//toDot(actual);
			IsTrue(BasicOperations.SameLanguage(expected, actual));
		}

		[Test]
		public virtual void TestToDot()
		{
			TokenStream ts = new CannedTokenStream(new Token[] { Token("abc", 1, 1, 0, 4) });
			var w = new StringWriter();
			new TokenStreamToDot("abcd", ts, new StreamWriter(new MemoryStream())).ToDot();
			IsTrue(w.ToString().IndexOf("abc / abcd") != -1);
		}

		[Test]
		public virtual void TestStartsWithHole()
		{
			TokenStream ts = new CannedTokenStream(new Token[] { Token("abc", 2, 1) });
			Automaton actual = (new TokenStreamToAutomaton()
				).ToAutomaton(ts);
			Automaton expected = Join(HOLE_A, SEP_A, S2a("abc"
				));
			//toDot(actual);
			IsTrue(BasicOperations.SameLanguage(expected, actual));
		}

		// TODO: testEndsWithHole... but we need posInc to set in TS.end()
		[Test]
		public virtual void TestSynHangingOverEnd()
		{
			TokenStream ts = new CannedTokenStream(new Token[] { Token("a", 1, 1), Token("X", 
				0, 10) });
			Automaton actual = (new TokenStreamToAutomaton()
				).ToAutomaton(ts);
			Automaton expected = BasicOperations.Union(BasicAutomata
				.MakeString("a"), BasicAutomata.MakeString("X"));
			IsTrue(BasicOperations.SameLanguage(expected, actual));
		}
	}
}
