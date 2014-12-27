/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Codecs;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using Sharpen;

namespace Lucene.Net.Test.Index
{
	public class TestTermsEnum2 : LuceneTestCase
	{
		private Directory dir;

		private IndexReader reader;

		private IndexSearcher searcher;

		private ICollection<BytesRef> terms;

		private Lucene.Net.Util.Automaton.Automaton termsAutomaton;

		internal int numIterations;

		// the terms we put in the index
		// automata of the same
		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			// we generate aweful regexps: good for testing.
			// but for preflex codec, the test can be very slow, so use less iterations.
			numIterations = Codec.GetDefault().GetName().Equals("Lucene3x") ? 10 * RANDOM_MULTIPLIER
				 : AtLeast(50);
			dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, ((IndexWriterConfig
				)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random(), MockTokenizer
				.KEYWORD, false)).SetMaxBufferedDocs(TestUtil.NextInt(Random(), 50, 1000))));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			Field field = NewStringField("field", string.Empty, Field.Store.YES);
			doc.Add(field);
			terms = new TreeSet<BytesRef>();
			int num = AtLeast(200);
			for (int i = 0; i < num; i++)
			{
				string s = TestUtil.RandomUnicodeString(Random());
				field.StringValue = s);
				terms.AddItem(new BytesRef(s));
				writer.AddDocument(doc);
			}
			termsAutomaton = BasicAutomata.MakeStringUnion(terms);
			reader = writer.GetReader();
			searcher = NewSearcher(reader);
			writer.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			reader.Dispose();
			dir.Dispose();
			base.TearDown();
		}

		/// <summary>tests a pre-intersected automaton against the original</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestFiniteVersusInfinite()
		{
			for (int i = 0; i < numIterations; i++)
			{
				string reg = AutomatonTestUtil.RandomRegexp(Random());
				Lucene.Net.Util.Automaton.Automaton automaton = new RegExp(reg, RegExp.NONE
					).ToAutomaton();
				IList<BytesRef> matchedTerms = new AList<BytesRef>();
				foreach (BytesRef t in terms)
				{
					if (BasicOperations.Run(automaton, t.Utf8ToString()))
					{
						matchedTerms.AddItem(t);
					}
				}
				Lucene.Net.Util.Automaton.Automaton alternate = BasicAutomata.MakeStringUnion
					(matchedTerms);
				//System.out.println("match " + matchedTerms.size() + " " + alternate.getNumberOfStates() + " states, sigma=" + alternate.getStartPoints().length);
				//AutomatonTestUtil.minimizeSimple(alternate);
				//System.out.println("minmize done");
				AutomatonQuery a1 = new AutomatonQuery(new Term("field", string.Empty), automaton
					);
				AutomatonQuery a2 = new AutomatonQuery(new Term("field", string.Empty), alternate
					);
				CheckHits.CheckEqual(a1, searcher.Search(a1, 25).ScoreDocs, searcher.Search(a2, 25
					).ScoreDocs);
			}
		}

		/// <summary>seeks to every term accepted by some automata</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestSeeking()
		{
			for (int i = 0; i < numIterations; i++)
			{
				string reg = AutomatonTestUtil.RandomRegexp(Random());
				Lucene.Net.Util.Automaton.Automaton automaton = new RegExp(reg, RegExp.NONE
					).ToAutomaton();
				TermsEnum te = MultiFields.GetTerms(reader, "field").Iterator(null);
				AList<BytesRef> unsortedTerms = new AList<BytesRef>(terms);
				Sharpen.Collections.Shuffle(unsortedTerms, Random());
				foreach (BytesRef term in unsortedTerms)
				{
					if (BasicOperations.Run(automaton, term.Utf8ToString()))
					{
						// term is accepted
						if (Random().NextBoolean())
						{
							// seek exact
							IsTrue(te.SeekExact(term));
						}
						else
						{
							// seek ceil
							AreEqual(TermsEnum.SeekStatus.FOUND, te.SeekCeil(term));
							AreEqual(term, te.Term());
						}
					}
				}
			}
		}

		/// <summary>mixes up seek and next for all terms</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestSeekingAndNexting()
		{
			for (int i = 0; i < numIterations; i++)
			{
				TermsEnum te = MultiFields.GetTerms(reader, "field").Iterator(null);
				foreach (BytesRef term in terms)
				{
					int c = Random().Next(3);
					if (c == 0)
					{
						AreEqual(term, te.Next());
					}
					else
					{
						if (c == 1)
						{
							AreEqual(TermsEnum.SeekStatus.FOUND, te.SeekCeil(term));
							AreEqual(term, te.Term());
						}
						else
						{
							IsTrue(te.SeekExact(term));
						}
					}
				}
			}
		}

		/// <summary>tests intersect: TODO start at a random term!</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestIntersect()
		{
			for (int i = 0; i < numIterations; i++)
			{
				string reg = AutomatonTestUtil.RandomRegexp(Random());
				Lucene.Net.Util.Automaton.Automaton automaton = new RegExp(reg, RegExp.NONE
					).ToAutomaton();
				CompiledAutomaton ca = new CompiledAutomaton(automaton, SpecialOperations.IsFinite
					(automaton), false);
				TermsEnum te = MultiFields.GetTerms(reader, "field").Intersect(ca, null);
				Lucene.Net.Util.Automaton.Automaton expected = BasicOperations.Intersection
					(termsAutomaton, automaton);
				TreeSet<BytesRef> found = new TreeSet<BytesRef>();
				while (te.Next() != null)
				{
					found.AddItem(BytesRef.DeepCopyOf(te.Term()));
				}
				Lucene.Net.Util.Automaton.Automaton actual = BasicAutomata.MakeStringUnion
					(found);
				IsTrue(BasicOperations.SameLanguage(expected, actual));
			}
		}
	}
}
