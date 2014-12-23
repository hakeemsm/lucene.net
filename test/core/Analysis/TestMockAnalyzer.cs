/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using Sharpen;

namespace Lucene.Net.Analysis
{
	public class TestMockAnalyzer : BaseTokenStreamTestCase
	{
		/// <summary>Test a configuration that behaves a lot like WhitespaceAnalyzer</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestWhitespace()
		{
			Analyzer a = new MockAnalyzer(Random());
			AssertAnalyzesTo(a, "A bc defg hiJklmn opqrstuv wxy z ", new string[] { "a", "bc"
				, "defg", "hijklmn", "opqrstuv", "wxy", "z" });
			AssertAnalyzesTo(a, "aba cadaba shazam", new string[] { "aba", "cadaba", "shazam"
				 });
			AssertAnalyzesTo(a, "break on whitespace", new string[] { "break", "on", "whitespace"
				 });
		}

		/// <summary>Test a configuration that behaves a lot like SimpleAnalyzer</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestSimple()
		{
			Analyzer a = new MockAnalyzer(Random(), MockTokenizer.SIMPLE, true);
			AssertAnalyzesTo(a, "a-bc123 defg+hijklmn567opqrstuv78wxy_z ", new string[] { "a"
				, "bc", "defg", "hijklmn", "opqrstuv", "wxy", "z" });
			AssertAnalyzesTo(a, "aba4cadaba-Shazam", new string[] { "aba", "cadaba", "shazam"
				 });
			AssertAnalyzesTo(a, "break+on/Letters", new string[] { "break", "on", "letters" }
				);
		}

		/// <summary>Test a configuration that behaves a lot like KeywordAnalyzer</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestKeyword()
		{
			Analyzer a = new MockAnalyzer(Random(), MockTokenizer.KEYWORD, false);
			AssertAnalyzesTo(a, "a-bc123 defg+hijklmn567opqrstuv78wxy_z ", new string[] { "a-bc123 defg+hijklmn567opqrstuv78wxy_z "
				 });
			AssertAnalyzesTo(a, "aba4cadaba-Shazam", new string[] { "aba4cadaba-Shazam" });
			AssertAnalyzesTo(a, "break+on/Nothing", new string[] { "break+on/Nothing" });
			// currently though emits no tokens for empty string: maybe we can do it,
			// but we don't want to emit tokens infinitely...
			AssertAnalyzesTo(a, string.Empty, new string[0]);
		}

		// Test some regular expressions as tokenization patterns
		/// <summary>Test a configuration where each character is a term</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestSingleChar()
		{
			CharacterRunAutomaton single = new CharacterRunAutomaton(new RegExp(".").ToAutomaton
				());
			Analyzer a = new MockAnalyzer(Random(), single, false);
			AssertAnalyzesTo(a, "foobar", new string[] { "f", "o", "o", "b", "a", "r" }, new 
				int[] { 0, 1, 2, 3, 4, 5 }, new int[] { 1, 2, 3, 4, 5, 6 });
			CheckRandomData(Random(), a, 100);
		}

		/// <summary>Test a configuration where two characters makes a term</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestTwoChars()
		{
			CharacterRunAutomaton single = new CharacterRunAutomaton(new RegExp("..").ToAutomaton
				());
			Analyzer a = new MockAnalyzer(Random(), single, false);
			AssertAnalyzesTo(a, "foobar", new string[] { "fo", "ob", "ar" }, new int[] { 0, 2
				, 4 }, new int[] { 2, 4, 6 });
			// make sure when last term is a "partial" match that end() is correct
			AssertTokenStreamContents(a.TokenStream("bogus", "fooba"), new string[] { "fo", "ob"
				 }, new int[] { 0, 2 }, new int[] { 2, 4 }, new int[] { 1, 1 }, 5);
			CheckRandomData(Random(), a, 100);
		}

		/// <summary>Test a configuration where three characters makes a term</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestThreeChars()
		{
			CharacterRunAutomaton single = new CharacterRunAutomaton(new RegExp("...").ToAutomaton
				());
			Analyzer a = new MockAnalyzer(Random(), single, false);
			AssertAnalyzesTo(a, "foobar", new string[] { "foo", "bar" }, new int[] { 0, 3 }, 
				new int[] { 3, 6 });
			// make sure when last term is a "partial" match that end() is correct
			AssertTokenStreamContents(a.TokenStream("bogus", "fooba"), new string[] { "foo" }
				, new int[] { 0 }, new int[] { 3 }, new int[] { 1 }, 5);
			CheckRandomData(Random(), a, 100);
		}

		/// <summary>Test a configuration where word starts with one uppercase</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestUppercase()
		{
			CharacterRunAutomaton single = new CharacterRunAutomaton(new RegExp("[A-Z][a-z]*"
				).ToAutomaton());
			Analyzer a = new MockAnalyzer(Random(), single, false);
			AssertAnalyzesTo(a, "FooBarBAZ", new string[] { "Foo", "Bar", "B", "A", "Z" }, new 
				int[] { 0, 3, 6, 7, 8 }, new int[] { 3, 6, 7, 8, 9 });
			AssertAnalyzesTo(a, "aFooBar", new string[] { "Foo", "Bar" }, new int[] { 1, 4 }, 
				new int[] { 4, 7 });
			CheckRandomData(Random(), a, 100);
		}

		/// <summary>Test a configuration that behaves a lot like StopAnalyzer</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestStop()
		{
			Analyzer a = new MockAnalyzer(Random(), MockTokenizer.SIMPLE, true, MockTokenFilter
				.ENGLISH_STOPSET);
			AssertAnalyzesTo(a, "the quick brown a fox", new string[] { "quick", "brown", "fox"
				 }, new int[] { 2, 1, 2 });
		}

		/// <summary>Test a configuration that behaves a lot like KeepWordFilter</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestKeep()
		{
			CharacterRunAutomaton keepWords = new CharacterRunAutomaton(BasicOperations.Complement
				(Lucene.Net.Util.Automaton.Automaton.Union(Arrays.AsList(BasicAutomata.MakeString
				("foo"), BasicAutomata.MakeString("bar")))));
			Analyzer a = new MockAnalyzer(Random(), MockTokenizer.SIMPLE, true, keepWords);
			AssertAnalyzesTo(a, "quick foo brown bar bar fox foo", new string[] { "foo", "bar"
				, "bar", "foo" }, new int[] { 2, 2, 1, 2 });
		}

		/// <summary>Test a configuration that behaves a lot like LengthFilter</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestLength()
		{
			CharacterRunAutomaton length5 = new CharacterRunAutomaton(new RegExp(".{5,}").ToAutomaton
				());
			Analyzer a = new MockAnalyzer(Random(), MockTokenizer.WHITESPACE, true, length5);
			AssertAnalyzesTo(a, "ok toolong fine notfine", new string[] { "ok", "fine" }, new 
				int[] { 1, 2 });
		}

		/// <summary>Test MockTokenizer encountering a too long token</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestTooLongToken()
		{
			Analyzer whitespace = new _Analyzer_191();
			AssertTokenStreamContents(whitespace.TokenStream("bogus", "test 123 toolong ok ")
				, new string[] { "test", "123", "toolo", "ng", "ok" }, new int[] { 0, 5, 9, 14, 
				17 }, new int[] { 4, 8, 14, 16, 19 }, 20);
			AssertTokenStreamContents(whitespace.TokenStream("bogus", "test 123 toolo"), new 
				string[] { "test", "123", "toolo" }, new int[] { 0, 5, 9 }, new int[] { 4, 8, 14
				 }, 14);
		}

		private sealed class _Analyzer_191 : Analyzer
		{
			public _Analyzer_191()
			{
			}

			protected override Analyzer.TokenStreamComponents CreateComponents(string fieldName
				, StreamReader reader)
			{
				Tokenizer t = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false, 5);
				return new Analyzer.TokenStreamComponents(t, t);
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestLUCENE_3042()
		{
			string testString = "t";
			Analyzer analyzer = new MockAnalyzer(Random());
			stream.Reset();
			while (stream.IncrementToken())
			{
			}
			// consume
			stream.End();
			AssertAnalyzesTo(analyzer, testString, new string[] { "t" });
		}

		/// <summary>blast some random strings through the analyzer</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestRandomStrings()
		{
			CheckRandomData(Random(), new MockAnalyzer(Random()), AtLeast(1000));
		}

		/// <summary>blast some random strings through differently configured tokenizers</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestRandomRegexps()
		{
			int iters = AtLeast(30);
			for (int i = 0; i < iters; i++)
			{
				CharacterRunAutomaton dfa = new CharacterRunAutomaton(AutomatonTestUtil.RandomAutomaton
					(Random()));
				bool lowercase = Random().NextBoolean();
				int limit = TestUtil.NextInt(Random(), 0, 500);
				Analyzer a = new _Analyzer_239(dfa, lowercase, limit);
				CheckRandomData(Random(), a, 100);
				a.Close();
			}
		}

		private sealed class _Analyzer_239 : Analyzer
		{
			public _Analyzer_239(CharacterRunAutomaton dfa, bool lowercase, int limit)
			{
				this.dfa = dfa;
				this.lowercase = lowercase;
				this.limit = limit;
			}

			protected override Analyzer.TokenStreamComponents CreateComponents(string fieldName
				, StreamReader reader)
			{
				Tokenizer t = new MockTokenizer(reader, dfa, lowercase, limit);
				return new Analyzer.TokenStreamComponents(t, t);
			}

			private readonly CharacterRunAutomaton dfa;

			private readonly bool lowercase;

			private readonly int limit;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestForwardOffsets()
		{
			int num = AtLeast(10000);
			for (int i = 0; i < num; i++)
			{
				string s = TestUtil.RandomHtmlishString(Random(), 20);
				StringReader reader = new StringReader(s);
				MockCharFilter charfilter = new MockCharFilter(reader, 2);
				MockAnalyzer analyzer = new MockAnalyzer(Random());
				ts.Reset();
				while (ts.IncrementToken())
				{
				}
				ts.End();
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestWrapReader()
		{
			// LUCENE-5153: test that wrapping an analyzer's reader is allowed
			Random random = Random();
			Analyzer delegate_ = new MockAnalyzer(random);
			Analyzer a = new _AnalyzerWrapper_273(delegate_, delegate_.GetReuseStrategy());
			CheckOneTerm(a, "abc", "aabc");
		}

		private sealed class _AnalyzerWrapper_273 : AnalyzerWrapper
		{
			public _AnalyzerWrapper_273(Analyzer delegate_, Analyzer.ReuseStrategy baseArg1) : 
				base(baseArg1)
			{
				this.delegate_ = delegate_;
			}

			protected override StreamReader WrapReader(string fieldName, StreamReader reader)
			{
				return new MockCharFilter(reader, 7);
			}

			protected override Analyzer.TokenStreamComponents WrapComponents(string fieldName
				, Analyzer.TokenStreamComponents components)
			{
				return components;
			}

			protected override Analyzer GetWrappedAnalyzer(string fieldName)
			{
				return delegate_;
			}

			private readonly Analyzer delegate_;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestChangeGaps()
		{
			// LUCENE-5324: check that it is possible to change the wrapper's gaps
			int positionGap = Random().Next(1000);
			int offsetGap = Random().Next(1000);
			Analyzer delegate_ = new MockAnalyzer(Random());
			Analyzer a = new _AnalyzerWrapper_299(delegate_, positionGap, offsetGap, delegate_
				.GetReuseStrategy());
			RandomIndexWriter writer = new RandomIndexWriter(Random(), NewDirectory());
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			FieldType ft = new FieldType();
			ft.SetIndexed(true);
			ft.SetIndexOptions(FieldInfo.IndexOptions.DOCS_ONLY);
			ft.SetTokenized(true);
			ft.SetStoreTermVectors(true);
			ft.SetStoreTermVectorPositions(true);
			ft.SetStoreTermVectorOffsets(true);
			doc.Add(new Field("f", "a", ft));
			doc.Add(new Field("f", "a", ft));
			writer.AddDocument(doc, a);
			AtomicReader reader = GetOnlySegmentReader(writer.GetReader());
			Fields fields = reader.GetTermVectors(0);
			Terms terms = fields.Terms("f");
			TermsEnum te = terms.Iterator(null);
			NUnit.Framework.Assert.AreEqual(new BytesRef("a"), te.Next());
			DocsAndPositionsEnum dpe = te.DocsAndPositions(null, null);
			NUnit.Framework.Assert.AreEqual(0, dpe.NextDoc());
			NUnit.Framework.Assert.AreEqual(2, dpe.Freq());
			NUnit.Framework.Assert.AreEqual(0, dpe.NextPosition());
			NUnit.Framework.Assert.AreEqual(0, dpe.StartOffset());
			int endOffset = dpe.EndOffset();
			NUnit.Framework.Assert.AreEqual(1 + positionGap, dpe.NextPosition());
			NUnit.Framework.Assert.AreEqual(1 + endOffset + offsetGap, dpe.EndOffset());
			NUnit.Framework.Assert.AreEqual(null, te.Next());
			reader.Close();
			writer.Close();
			writer.w.GetDirectory().Close();
		}

		private sealed class _AnalyzerWrapper_299 : AnalyzerWrapper
		{
			public _AnalyzerWrapper_299(Analyzer delegate_, int positionGap, int offsetGap, Analyzer.ReuseStrategy
				 baseArg1) : base(baseArg1)
			{
				this.delegate_ = delegate_;
				this.positionGap = positionGap;
				this.offsetGap = offsetGap;
			}

			protected override Analyzer GetWrappedAnalyzer(string fieldName)
			{
				return delegate_;
			}

			public override int GetPositionIncrementGap(string fieldName)
			{
				return positionGap;
			}

			public override int GetOffsetGap(string fieldName)
			{
				return offsetGap;
			}

			private readonly Analyzer delegate_;

			private readonly int positionGap;

			private readonly int offsetGap;
		}
	}
}
