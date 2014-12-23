/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using Sharpen;

namespace Lucene.Net.Index
{
	public class TestTermsEnum : LuceneTestCase
	{
		/// <exception cref="System.Exception"></exception>
		public virtual void Test()
		{
			Random random = new Random(Random().NextLong());
			LineFileDocs docs = new LineFileDocs(random, DefaultCodecSupportsDocValues());
			Directory d = NewDirectory();
			MockAnalyzer analyzer = new MockAnalyzer(Random());
			analyzer.SetMaxTokenLength(TestUtil.NextInt(Random(), 1, IndexWriter.MAX_TERM_LENGTH
				));
			RandomIndexWriter w = new RandomIndexWriter(Random(), d, analyzer);
			int numDocs = AtLeast(10);
			for (int docCount = 0; docCount < numDocs; docCount++)
			{
				w.AddDocument(docs.NextDoc());
			}
			IndexReader r = w.GetReader();
			w.Close();
			IList<BytesRef> terms = new AList<BytesRef>();
			TermsEnum termsEnum = MultiFields.GetTerms(r, "body").Iterator(null);
			BytesRef term;
			while ((term = termsEnum.Next()) != null)
			{
				terms.AddItem(BytesRef.DeepCopyOf(term));
			}
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: " + terms.Count + " terms");
			}
			int upto = -1;
			int iters = AtLeast(200);
			for (int iter = 0; iter < iters; iter++)
			{
				bool isEnd;
				if (upto != -1 && Random().NextBoolean())
				{
					// next
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("TEST: iter next");
					}
					isEnd = termsEnum.Next() == null;
					upto++;
					if (isEnd)
					{
						if (VERBOSE)
						{
							System.Console.Out.WriteLine("  end");
						}
						NUnit.Framework.Assert.AreEqual(upto, terms.Count);
						upto = -1;
					}
					else
					{
						if (VERBOSE)
						{
							System.Console.Out.WriteLine("  got term=" + termsEnum.Term().Utf8ToString() + " expected="
								 + terms[upto].Utf8ToString());
						}
						NUnit.Framework.Assert.IsTrue(upto < terms.Count);
						NUnit.Framework.Assert.AreEqual(terms[upto], termsEnum.Term());
					}
				}
				else
				{
					BytesRef target;
					string exists;
					if (Random().NextBoolean())
					{
						// likely fake term
						if (Random().NextBoolean())
						{
							target = new BytesRef(TestUtil.RandomSimpleString(Random()));
						}
						else
						{
							target = new BytesRef(TestUtil.RandomRealisticUnicodeString(Random()));
						}
						exists = "likely not";
					}
					else
					{
						// real term
						target = terms[Random().Next(terms.Count)];
						exists = "yes";
					}
					upto = Sharpen.Collections.BinarySearch(terms, target);
					if (Random().NextBoolean())
					{
						if (VERBOSE)
						{
							System.Console.Out.WriteLine("TEST: iter seekCeil target=" + target.Utf8ToString(
								) + " exists=" + exists);
						}
						// seekCeil
						TermsEnum.SeekStatus status = termsEnum.SeekCeil(target);
						if (VERBOSE)
						{
							System.Console.Out.WriteLine("  got " + status);
						}
						if (upto < 0)
						{
							upto = -(upto + 1);
							if (upto >= terms.Count)
							{
								NUnit.Framework.Assert.AreEqual(TermsEnum.SeekStatus.END, status);
								upto = -1;
							}
							else
							{
								NUnit.Framework.Assert.AreEqual(TermsEnum.SeekStatus.NOT_FOUND, status);
								NUnit.Framework.Assert.AreEqual(terms[upto], termsEnum.Term());
							}
						}
						else
						{
							NUnit.Framework.Assert.AreEqual(TermsEnum.SeekStatus.FOUND, status);
							NUnit.Framework.Assert.AreEqual(terms[upto], termsEnum.Term());
						}
					}
					else
					{
						if (VERBOSE)
						{
							System.Console.Out.WriteLine("TEST: iter seekExact target=" + target.Utf8ToString
								() + " exists=" + exists);
						}
						// seekExact
						bool result = termsEnum.SeekExact(target);
						if (VERBOSE)
						{
							System.Console.Out.WriteLine("  got " + result);
						}
						if (upto < 0)
						{
							NUnit.Framework.Assert.IsFalse(result);
							upto = -1;
						}
						else
						{
							NUnit.Framework.Assert.IsTrue(result);
							NUnit.Framework.Assert.AreEqual(target, termsEnum.Term());
						}
					}
				}
			}
			r.Close();
			d.Close();
			docs.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void AddDoc(RandomIndexWriter w, ICollection<string> terms, IDictionary<BytesRef
			, int> termToID, int id)
		{
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			doc.Add(new IntField("id", id, Field.Store.NO));
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: addDoc id:" + id + " terms=" + terms);
			}
			foreach (string s2 in terms)
			{
				doc.Add(NewStringField("f", s2, Field.Store.NO));
				termToID.Put(new BytesRef(s2), id);
			}
			w.AddDocument(doc);
			terms.Clear();
		}

		private bool Accepts(CompiledAutomaton c, BytesRef b)
		{
			int state = c.runAutomaton.GetInitialState();
			for (int idx = 0; idx < b.length; idx++)
			{
				NUnit.Framework.Assert.IsTrue(state != -1);
				state = c.runAutomaton.Step(state, b.bytes[b.offset + idx] & unchecked((int)(0xff
					)));
			}
			return c.runAutomaton.IsAccept(state);
		}

		// Tests Terms.intersect
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestIntersectRandom()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), dir);
			int numTerms = AtLeast(300);
			//final int numTerms = 50;
			ICollection<string> terms = new HashSet<string>();
			ICollection<string> pendingTerms = new AList<string>();
			IDictionary<BytesRef, int> termToID = new Dictionary<BytesRef, int>();
			int id = 0;
			while (terms.Count != numTerms)
			{
				string s = GetRandomString();
				if (!terms.Contains(s))
				{
					terms.AddItem(s);
					pendingTerms.AddItem(s);
					if (Random().Next(20) == 7)
					{
						AddDoc(w, pendingTerms, termToID, id++);
					}
				}
			}
			AddDoc(w, pendingTerms, termToID, id++);
			BytesRef[] termsArray = new BytesRef[terms.Count];
			ICollection<BytesRef> termsSet = new HashSet<BytesRef>();
			{
				int upto = 0;
				foreach (string s in terms)
				{
					BytesRef b = new BytesRef(s);
					termsArray[upto++] = b;
					termsSet.AddItem(b);
				}
				Arrays.Sort(termsArray);
			}
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("\nTEST: indexed terms (unicode order):");
				foreach (BytesRef t in termsArray)
				{
					System.Console.Out.WriteLine("  " + t.Utf8ToString() + " -> id:" + termToID.Get(t
						));
				}
			}
			IndexReader r = w.GetReader();
			w.Close();
			// NOTE: intentional insanity!!
			FieldCache.Ints docIDToID = FieldCache.DEFAULT.GetInts(SlowCompositeReaderWrapper
				.Wrap(r), "id", false);
			for (int iter = 0; iter < 10 * RANDOM_MULTIPLIER; iter++)
			{
				// TODO: can we also test infinite As here...?
				// From the random terms, pick some ratio and compile an
				// automaton:
				ICollection<string> acceptTerms = new HashSet<string>();
				TreeSet<BytesRef> sortedAcceptTerms = new TreeSet<BytesRef>();
				double keepPct = Random().NextDouble();
				Lucene.Net.Util.Automaton.Automaton a;
				if (iter == 0)
				{
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("\nTEST: empty automaton");
					}
					a = BasicAutomata.MakeEmpty();
				}
				else
				{
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("\nTEST: keepPct=" + keepPct);
					}
					foreach (string s in terms)
					{
						string s2;
						if (Random().NextDouble() <= keepPct)
						{
							s2 = s;
						}
						else
						{
							s2 = GetRandomString();
						}
						acceptTerms.AddItem(s2);
						sortedAcceptTerms.AddItem(new BytesRef(s2));
					}
					a = BasicAutomata.MakeStringUnion(sortedAcceptTerms);
				}
				if (Random().NextBoolean())
				{
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("TEST: reduce the automaton");
					}
					a.Reduce();
				}
				CompiledAutomaton c = new CompiledAutomaton(a, true, false);
				BytesRef[] acceptTermsArray = new BytesRef[acceptTerms.Count];
				ICollection<BytesRef> acceptTermsSet = new HashSet<BytesRef>();
				int upto = 0;
				foreach (string s_1 in acceptTerms)
				{
					BytesRef b = new BytesRef(s_1);
					acceptTermsArray[upto++] = b;
					acceptTermsSet.AddItem(b);
					NUnit.Framework.Assert.IsTrue(Accepts(c, b));
				}
				Arrays.Sort(acceptTermsArray);
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("\nTEST: accept terms (unicode order):");
					foreach (BytesRef t in acceptTermsArray)
					{
						System.Console.Out.WriteLine("  " + t.Utf8ToString() + (termsSet.Contains(t) ? " (exists)"
							 : string.Empty));
					}
					System.Console.Out.WriteLine(a.ToDot());
				}
				for (int iter2 = 0; iter2 < 100; iter2++)
				{
					BytesRef startTerm = acceptTermsArray.Length == 0 || Random().NextBoolean() ? null
						 : acceptTermsArray[Random().Next(acceptTermsArray.Length)];
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("\nTEST: iter2=" + iter2 + " startTerm=" + (startTerm
							 == null ? "<null>" : startTerm.Utf8ToString()));
						if (startTerm != null)
						{
							int state = c.runAutomaton.GetInitialState();
							for (int idx = 0; idx < startTerm.length; idx++)
							{
								int label = startTerm.bytes[startTerm.offset + idx] & unchecked((int)(0xff));
								System.Console.Out.WriteLine("  state=" + state + " label=" + label);
								state = c.runAutomaton.Step(state, label);
								NUnit.Framework.Assert.IsTrue(state != -1);
							}
							System.Console.Out.WriteLine("  state=" + state);
						}
					}
					TermsEnum te = MultiFields.GetTerms(r, "f").Intersect(c, startTerm);
					int loc;
					if (startTerm == null)
					{
						loc = 0;
					}
					else
					{
						loc = System.Array.BinarySearch(termsArray, BytesRef.DeepCopyOf(startTerm));
						if (loc < 0)
						{
							loc = -(loc + 1);
						}
						else
						{
							// startTerm exists in index
							loc++;
						}
					}
					while (loc < termsArray.Length && !acceptTermsSet.Contains(termsArray[loc]))
					{
						loc++;
					}
					DocsEnum docsEnum = null;
					while (loc < termsArray.Length)
					{
						BytesRef expected = termsArray[loc];
						BytesRef actual = te.Next();
						if (VERBOSE)
						{
							System.Console.Out.WriteLine("TEST:   next() expected=" + expected.Utf8ToString()
								 + " actual=" + (actual == null ? "null" : actual.Utf8ToString()));
						}
						NUnit.Framework.Assert.AreEqual(expected, actual);
						NUnit.Framework.Assert.AreEqual(1, te.DocFreq());
						docsEnum = TestUtil.Docs(Random(), te, null, docsEnum, DocsEnum.FLAG_NONE);
						int docID = docsEnum.NextDoc();
						NUnit.Framework.Assert.IsTrue(docID != DocIdSetIterator.NO_MORE_DOCS);
						NUnit.Framework.Assert.AreEqual(docIDToID.Get(docID), termToID.Get(expected));
						do
						{
							loc++;
						}
						while (loc < termsArray.Length && !acceptTermsSet.Contains(termsArray[loc]));
					}
					NUnit.Framework.Assert.IsNull(te.Next());
				}
			}
			r.Close();
			dir.Close();
		}

		private Directory d;

		private IndexReader r;

		private readonly string FIELD = "field";

		/// <exception cref="System.Exception"></exception>
		private IndexReader MakeIndex(params string[] terms)
		{
			d = NewDirectory();
			IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			RandomIndexWriter w = new RandomIndexWriter(Random(), d, iwc);
			foreach (string term in terms)
			{
				Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
					();
				Field f = NewStringField(FIELD, term, Field.Store.NO);
				doc.Add(f);
				w.AddDocument(doc);
			}
			if (r != null)
			{
				Close();
			}
			r = w.GetReader();
			w.Close();
			return r;
		}

		/// <exception cref="System.Exception"></exception>
		private void Close()
		{
			r.Close();
			d.Close();
		}

		/// <exception cref="System.Exception"></exception>
		private int DocFreq(IndexReader r, string term)
		{
			return r.DocFreq(new Term(FIELD, term));
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestEasy()
		{
			// No floor arcs:
			r = MakeIndex("aa0", "aa1", "aa2", "aa3", "bb0", "bb1", "bb2", "bb3", "aa");
			// First term in block:
			NUnit.Framework.Assert.AreEqual(1, DocFreq(r, "aa0"));
			// Scan forward to another term in same block
			NUnit.Framework.Assert.AreEqual(1, DocFreq(r, "aa2"));
			NUnit.Framework.Assert.AreEqual(1, DocFreq(r, "aa"));
			// Reset same block then scan forwards
			NUnit.Framework.Assert.AreEqual(1, DocFreq(r, "aa1"));
			// Not found, in same block
			NUnit.Framework.Assert.AreEqual(0, DocFreq(r, "aa5"));
			// Found, in same block
			NUnit.Framework.Assert.AreEqual(1, DocFreq(r, "aa2"));
			// Not found in index:
			NUnit.Framework.Assert.AreEqual(0, DocFreq(r, "b0"));
			// Found:
			NUnit.Framework.Assert.AreEqual(1, DocFreq(r, "aa2"));
			// Found, rewind:
			NUnit.Framework.Assert.AreEqual(1, DocFreq(r, "aa0"));
			// First term in block:
			NUnit.Framework.Assert.AreEqual(1, DocFreq(r, "bb0"));
			// Scan forward to another term in same block
			NUnit.Framework.Assert.AreEqual(1, DocFreq(r, "bb2"));
			// Reset same block then scan forwards
			NUnit.Framework.Assert.AreEqual(1, DocFreq(r, "bb1"));
			// Not found, in same block
			NUnit.Framework.Assert.AreEqual(0, DocFreq(r, "bb5"));
			// Found, in same block
			NUnit.Framework.Assert.AreEqual(1, DocFreq(r, "bb2"));
			// Not found in index:
			NUnit.Framework.Assert.AreEqual(0, DocFreq(r, "b0"));
			// Found:
			NUnit.Framework.Assert.AreEqual(1, DocFreq(r, "bb2"));
			// Found, rewind:
			NUnit.Framework.Assert.AreEqual(1, DocFreq(r, "bb0"));
			Close();
		}

		// tests:
		//   - test same prefix has non-floor block and floor block (ie, has 2 long outputs on same term prefix)
		//   - term that's entirely in the index
		/// <exception cref="System.Exception"></exception>
		public virtual void TestFloorBlocks()
		{
			string[] terms = new string[] { "aa0", "aa1", "aa2", "aa3", "aa4", "aa5", "aa6", 
				"aa7", "aa8", "aa9", "aa", "xx" };
			r = MakeIndex(terms);
			//r = makeIndex("aa0", "aa1", "aa2", "aa3", "aa4", "aa5", "aa6", "aa7", "aa8", "aa9");
			// First term in first block:
			NUnit.Framework.Assert.AreEqual(1, DocFreq(r, "aa0"));
			NUnit.Framework.Assert.AreEqual(1, DocFreq(r, "aa4"));
			// No block
			NUnit.Framework.Assert.AreEqual(0, DocFreq(r, "bb0"));
			// Second block
			NUnit.Framework.Assert.AreEqual(1, DocFreq(r, "aa4"));
			// Backwards to prior floor block:
			NUnit.Framework.Assert.AreEqual(1, DocFreq(r, "aa0"));
			// Forwards to last floor block:
			NUnit.Framework.Assert.AreEqual(1, DocFreq(r, "aa9"));
			NUnit.Framework.Assert.AreEqual(0, DocFreq(r, "a"));
			NUnit.Framework.Assert.AreEqual(1, DocFreq(r, "aa"));
			NUnit.Framework.Assert.AreEqual(0, DocFreq(r, "a"));
			NUnit.Framework.Assert.AreEqual(1, DocFreq(r, "aa"));
			// Forwards to last floor block:
			NUnit.Framework.Assert.AreEqual(1, DocFreq(r, "xx"));
			NUnit.Framework.Assert.AreEqual(1, DocFreq(r, "aa1"));
			NUnit.Framework.Assert.AreEqual(0, DocFreq(r, "yy"));
			NUnit.Framework.Assert.AreEqual(1, DocFreq(r, "xx"));
			NUnit.Framework.Assert.AreEqual(1, DocFreq(r, "aa9"));
			NUnit.Framework.Assert.AreEqual(1, DocFreq(r, "xx"));
			NUnit.Framework.Assert.AreEqual(1, DocFreq(r, "aa4"));
			TermsEnum te = MultiFields.GetTerms(r, FIELD).Iterator(null);
			while (te.Next() != null)
			{
			}
			//System.out.println("TEST: next term=" + te.term().utf8ToString());
			NUnit.Framework.Assert.IsTrue(SeekExact(te, "aa1"));
			NUnit.Framework.Assert.AreEqual("aa2", Next(te));
			NUnit.Framework.Assert.IsTrue(SeekExact(te, "aa8"));
			NUnit.Framework.Assert.AreEqual("aa9", Next(te));
			NUnit.Framework.Assert.AreEqual("xx", Next(te));
			TestRandomSeeks(r, terms);
			Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestZeroTerms()
		{
			d = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), d);
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			doc.Add(NewTextField("field", "one two three", Field.Store.NO));
			doc = new Lucene.Net.Document.Document();
			doc.Add(NewTextField("field2", "one two three", Field.Store.NO));
			w.AddDocument(doc);
			w.Commit();
			w.DeleteDocuments(new Term("field", "one"));
			w.ForceMerge(1);
			IndexReader r = w.GetReader();
			w.Close();
			NUnit.Framework.Assert.AreEqual(1, r.NumDocs());
			NUnit.Framework.Assert.AreEqual(1, r.MaxDoc());
			Terms terms = MultiFields.GetTerms(r, "field");
			if (terms != null)
			{
				NUnit.Framework.Assert.IsNull(terms.Iterator(null).Next());
			}
			r.Close();
			d.Close();
		}

		private string GetRandomString()
		{
			//return TestUtil.randomSimpleString(random());
			return TestUtil.RandomRealisticUnicodeString(Random());
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestRandomTerms()
		{
			string[] terms = new string[TestUtil.NextInt(Random(), 1, AtLeast(1000))];
			ICollection<string> seen = new HashSet<string>();
			bool allowEmptyString = Random().NextBoolean();
			if (Random().Next(10) == 7 && terms.Length > 2)
			{
				// Sometimes add a bunch of terms sharing a longish common prefix:
				int numTermsSamePrefix = Random().Next(terms.Length / 2);
				if (numTermsSamePrefix > 0)
				{
					string prefix;
					while (true)
					{
						prefix = GetRandomString();
						if (prefix.Length < 5)
						{
							continue;
						}
						else
						{
							break;
						}
					}
					while (seen.Count < numTermsSamePrefix)
					{
						string t = prefix + GetRandomString();
						if (!seen.Contains(t))
						{
							terms[seen.Count] = t;
							seen.AddItem(t);
						}
					}
				}
			}
			while (seen.Count < terms.Length)
			{
				string t = GetRandomString();
				if (!seen.Contains(t) && (allowEmptyString || t.Length != 0))
				{
					terms[seen.Count] = t;
					seen.AddItem(t);
				}
			}
			r = MakeIndex(terms);
			TestRandomSeeks(r, terms);
			Close();
		}

		// sugar
		/// <exception cref="System.IO.IOException"></exception>
		private bool SeekExact(TermsEnum te, string term)
		{
			return te.SeekExact(new BytesRef(term));
		}

		// sugar
		/// <exception cref="System.IO.IOException"></exception>
		private string Next(TermsEnum te)
		{
			BytesRef br = te.Next();
			if (br == null)
			{
				return null;
			}
			else
			{
				return br.Utf8ToString();
			}
		}

		private BytesRef GetNonExistTerm(BytesRef[] terms)
		{
			BytesRef t = null;
			while (true)
			{
				string ts = GetRandomString();
				t = new BytesRef(ts);
				if (System.Array.BinarySearch(terms, t) < 0)
				{
					return t;
				}
			}
		}

		private class TermAndState
		{
			public readonly BytesRef term;

			public readonly TermState state;

			public TermAndState(BytesRef term, TermState state)
			{
				this.term = term;
				this.state = state;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void TestRandomSeeks(IndexReader r, params string[] validTermStrings)
		{
			BytesRef[] validTerms = new BytesRef[validTermStrings.Length];
			for (int termIDX = 0; termIDX < validTermStrings.Length; termIDX++)
			{
				validTerms[termIDX] = new BytesRef(validTermStrings[termIDX]);
			}
			Arrays.Sort(validTerms);
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: " + validTerms.Length + " terms:");
				foreach (BytesRef t in validTerms)
				{
					System.Console.Out.WriteLine("  " + t.Utf8ToString() + " " + t);
				}
			}
			TermsEnum te = MultiFields.GetTerms(r, FIELD).Iterator(null);
			int END_LOC = -validTerms.Length - 1;
			IList<TestTermsEnum.TermAndState> termStates = new AList<TestTermsEnum.TermAndState
				>();
			for (int iter = 0; iter < 100 * RANDOM_MULTIPLIER; iter++)
			{
				BytesRef t;
				int loc;
				TermState termState;
				if (Random().Next(6) == 4)
				{
					// pick term that doens't exist:
					t = GetNonExistTerm(validTerms);
					termState = null;
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("\nTEST: invalid term=" + t.Utf8ToString());
					}
					loc = System.Array.BinarySearch(validTerms, t);
				}
				else
				{
					if (termStates.Count != 0 && Random().Next(4) == 1)
					{
						TestTermsEnum.TermAndState ts = termStates[Random().Next(termStates.Count)];
						t = ts.term;
						loc = System.Array.BinarySearch(validTerms, t);
						NUnit.Framework.Assert.IsTrue(loc >= 0);
						termState = ts.state;
						if (VERBOSE)
						{
							System.Console.Out.WriteLine("\nTEST: valid termState term=" + t.Utf8ToString());
						}
					}
					else
					{
						// pick valid term
						loc = Random().Next(validTerms.Length);
						t = BytesRef.DeepCopyOf(validTerms[loc]);
						termState = null;
						if (VERBOSE)
						{
							System.Console.Out.WriteLine("\nTEST: valid term=" + t.Utf8ToString());
						}
					}
				}
				// seekCeil or seekExact:
				bool doSeekExact = Random().NextBoolean();
				if (termState != null)
				{
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("  seekExact termState");
					}
					te.SeekExact(t, termState);
				}
				else
				{
					if (doSeekExact)
					{
						if (VERBOSE)
						{
							System.Console.Out.WriteLine("  seekExact");
						}
						NUnit.Framework.Assert.AreEqual(loc >= 0, te.SeekExact(t));
					}
					else
					{
						if (VERBOSE)
						{
							System.Console.Out.WriteLine("  seekCeil");
						}
						TermsEnum.SeekStatus result = te.SeekCeil(t);
						if (VERBOSE)
						{
							System.Console.Out.WriteLine("  got " + result);
						}
						if (loc >= 0)
						{
							NUnit.Framework.Assert.AreEqual(TermsEnum.SeekStatus.FOUND, result);
						}
						else
						{
							if (loc == END_LOC)
							{
								NUnit.Framework.Assert.AreEqual(TermsEnum.SeekStatus.END, result);
							}
							else
							{
								//HM:revisit 
								//assert loc >= -validTerms.length;
								NUnit.Framework.Assert.AreEqual(TermsEnum.SeekStatus.NOT_FOUND, result);
							}
						}
					}
				}
				if (loc >= 0)
				{
					NUnit.Framework.Assert.AreEqual(t, te.Term());
				}
				else
				{
					if (doSeekExact)
					{
						// TermsEnum is unpositioned if seekExact returns false
						continue;
					}
					else
					{
						if (loc == END_LOC)
						{
							continue;
						}
						else
						{
							loc = -loc - 1;
							NUnit.Framework.Assert.AreEqual(validTerms[loc], te.Term());
						}
					}
				}
				// Do a bunch of next's after the seek
				int numNext = Random().Next(validTerms.Length);
				for (int nextCount = 0; nextCount < numNext; nextCount++)
				{
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("\nTEST: next loc=" + loc + " of " + validTerms.Length
							);
					}
					BytesRef t2 = te.Next();
					loc++;
					if (loc == validTerms.Length)
					{
						NUnit.Framework.Assert.IsNull(t2);
						break;
					}
					else
					{
						NUnit.Framework.Assert.AreEqual(validTerms[loc], t2);
						if (Random().Next(40) == 17 && termStates.Count < 100)
						{
							termStates.AddItem(new TestTermsEnum.TermAndState(validTerms[loc], te.TermState()
								));
						}
					}
				}
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestIntersectBasic()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			iwc.SetMergePolicy(new LogDocMergePolicy());
			RandomIndexWriter w = new RandomIndexWriter(Random(), dir, iwc);
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			doc.Add(NewTextField("field", "aaa", Field.Store.NO));
			w.AddDocument(doc);
			doc = new Lucene.Net.Document.Document();
			doc.Add(NewStringField("field", "bbb", Field.Store.NO));
			w.AddDocument(doc);
			doc = new Lucene.Net.Document.Document();
			doc.Add(NewTextField("field", "ccc", Field.Store.NO));
			w.AddDocument(doc);
			w.ForceMerge(1);
			DirectoryReader r = w.GetReader();
			w.Close();
			AtomicReader sub = GetOnlySegmentReader(r);
			Terms terms = sub.Fields().Terms("field");
			Lucene.Net.Util.Automaton.Automaton automaton = new RegExp(".*", RegExp.NONE
				).ToAutomaton();
			CompiledAutomaton ca = new CompiledAutomaton(automaton, false, false);
			TermsEnum te = terms.Intersect(ca, null);
			NUnit.Framework.Assert.AreEqual("aaa", te.Next().Utf8ToString());
			NUnit.Framework.Assert.AreEqual(0, te.Docs(null, null, DocsEnum.FLAG_NONE).NextDoc
				());
			NUnit.Framework.Assert.AreEqual("bbb", te.Next().Utf8ToString());
			NUnit.Framework.Assert.AreEqual(1, te.Docs(null, null, DocsEnum.FLAG_NONE).NextDoc
				());
			NUnit.Framework.Assert.AreEqual("ccc", te.Next().Utf8ToString());
			NUnit.Framework.Assert.AreEqual(2, te.Docs(null, null, DocsEnum.FLAG_NONE).NextDoc
				());
			NUnit.Framework.Assert.IsNull(te.Next());
			te = terms.Intersect(ca, new BytesRef("abc"));
			NUnit.Framework.Assert.AreEqual("bbb", te.Next().Utf8ToString());
			NUnit.Framework.Assert.AreEqual(1, te.Docs(null, null, DocsEnum.FLAG_NONE).NextDoc
				());
			NUnit.Framework.Assert.AreEqual("ccc", te.Next().Utf8ToString());
			NUnit.Framework.Assert.AreEqual(2, te.Docs(null, null, DocsEnum.FLAG_NONE).NextDoc
				());
			NUnit.Framework.Assert.IsNull(te.Next());
			te = terms.Intersect(ca, new BytesRef("aaa"));
			NUnit.Framework.Assert.AreEqual("bbb", te.Next().Utf8ToString());
			NUnit.Framework.Assert.AreEqual(1, te.Docs(null, null, DocsEnum.FLAG_NONE).NextDoc
				());
			NUnit.Framework.Assert.AreEqual("ccc", te.Next().Utf8ToString());
			NUnit.Framework.Assert.AreEqual(2, te.Docs(null, null, DocsEnum.FLAG_NONE).NextDoc
				());
			NUnit.Framework.Assert.IsNull(te.Next());
			r.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestIntersectStartTerm()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			iwc.SetMergePolicy(new LogDocMergePolicy());
			RandomIndexWriter w = new RandomIndexWriter(Random(), dir, iwc);
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			doc.Add(NewStringField("field", "abc", Field.Store.NO));
			w.AddDocument(doc);
			doc = new Lucene.Net.Document.Document();
			doc.Add(NewStringField("field", "abd", Field.Store.NO));
			w.AddDocument(doc);
			doc = new Lucene.Net.Document.Document();
			doc.Add(NewStringField("field", "acd", Field.Store.NO));
			w.AddDocument(doc);
			doc = new Lucene.Net.Document.Document();
			doc.Add(NewStringField("field", "bcd", Field.Store.NO));
			w.AddDocument(doc);
			w.ForceMerge(1);
			DirectoryReader r = w.GetReader();
			w.Close();
			AtomicReader sub = GetOnlySegmentReader(r);
			Terms terms = sub.Fields().Terms("field");
			Lucene.Net.Util.Automaton.Automaton automaton = new RegExp(".*d", RegExp.NONE
				).ToAutomaton();
			CompiledAutomaton ca = new CompiledAutomaton(automaton, false, false);
			TermsEnum te;
			// should seek to startTerm
			te = terms.Intersect(ca, new BytesRef("aad"));
			NUnit.Framework.Assert.AreEqual("abd", te.Next().Utf8ToString());
			NUnit.Framework.Assert.AreEqual(1, te.Docs(null, null, DocsEnum.FLAG_NONE).NextDoc
				());
			NUnit.Framework.Assert.AreEqual("acd", te.Next().Utf8ToString());
			NUnit.Framework.Assert.AreEqual(2, te.Docs(null, null, DocsEnum.FLAG_NONE).NextDoc
				());
			NUnit.Framework.Assert.AreEqual("bcd", te.Next().Utf8ToString());
			NUnit.Framework.Assert.AreEqual(3, te.Docs(null, null, DocsEnum.FLAG_NONE).NextDoc
				());
			NUnit.Framework.Assert.IsNull(te.Next());
			// should fail to find ceil label on second arc, rewind 
			te = terms.Intersect(ca, new BytesRef("add"));
			NUnit.Framework.Assert.AreEqual("bcd", te.Next().Utf8ToString());
			NUnit.Framework.Assert.AreEqual(3, te.Docs(null, null, DocsEnum.FLAG_NONE).NextDoc
				());
			NUnit.Framework.Assert.IsNull(te.Next());
			// should reach end
			te = terms.Intersect(ca, new BytesRef("bcd"));
			NUnit.Framework.Assert.IsNull(te.Next());
			te = terms.Intersect(ca, new BytesRef("ddd"));
			NUnit.Framework.Assert.IsNull(te.Next());
			r.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestIntersectEmptyString()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			iwc.SetMergePolicy(new LogDocMergePolicy());
			RandomIndexWriter w = new RandomIndexWriter(Random(), dir, iwc);
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			doc.Add(NewStringField("field", string.Empty, Field.Store.NO));
			doc.Add(NewStringField("field", "abc", Field.Store.NO));
			w.AddDocument(doc);
			doc = new Lucene.Net.Document.Document();
			// add empty string to both documents, so that singletonDocID == -1.
			// For a FST-based term dict, we'll expect to see the first arc is 
			// flaged with HAS_FINAL_OUTPUT
			doc.Add(NewStringField("field", "abc", Field.Store.NO));
			doc.Add(NewStringField("field", string.Empty, Field.Store.NO));
			w.AddDocument(doc);
			w.ForceMerge(1);
			DirectoryReader r = w.GetReader();
			w.Close();
			AtomicReader sub = GetOnlySegmentReader(r);
			Terms terms = sub.Fields().Terms("field");
			Lucene.Net.Util.Automaton.Automaton automaton = new RegExp(".*", RegExp.NONE
				).ToAutomaton();
			// accept ALL
			CompiledAutomaton ca = new CompiledAutomaton(automaton, false, false);
			TermsEnum te = terms.Intersect(ca, null);
			DocsEnum de;
			NUnit.Framework.Assert.AreEqual(string.Empty, te.Next().Utf8ToString());
			de = te.Docs(null, null, DocsEnum.FLAG_NONE);
			NUnit.Framework.Assert.AreEqual(0, de.NextDoc());
			NUnit.Framework.Assert.AreEqual(1, de.NextDoc());
			NUnit.Framework.Assert.AreEqual("abc", te.Next().Utf8ToString());
			de = te.Docs(null, null, DocsEnum.FLAG_NONE);
			NUnit.Framework.Assert.AreEqual(0, de.NextDoc());
			NUnit.Framework.Assert.AreEqual(1, de.NextDoc());
			NUnit.Framework.Assert.IsNull(te.Next());
			// pass empty string
			te = terms.Intersect(ca, new BytesRef(string.Empty));
			NUnit.Framework.Assert.AreEqual("abc", te.Next().Utf8ToString());
			de = te.Docs(null, null, DocsEnum.FLAG_NONE);
			NUnit.Framework.Assert.AreEqual(0, de.NextDoc());
			NUnit.Framework.Assert.AreEqual(1, de.NextDoc());
			NUnit.Framework.Assert.IsNull(te.Next());
			r.Close();
			dir.Close();
		}
	}
}
