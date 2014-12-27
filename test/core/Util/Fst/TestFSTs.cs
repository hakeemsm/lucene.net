/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using Lucene.Net.Util.Fst;
using Lucene.Net.Util.Packed;
using Sharpen;

namespace Lucene.Net.Util.Fst
{
	public class TestFSTs : LuceneTestCase
	{
		private MockDirectoryWrapper dir;

		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			dir = NewMockDirectory();
			dir.SetPreventDoubleWrite(false);
		}

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			// can be null if we force simpletext (funky, some kind of bug in test runner maybe)
			if (dir != null)
			{
				dir.Dispose();
			}
			base.TearDown();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestBasicFSA()
		{
			string[] strings = new string[] { "station", "commotion", "elation", "elastic", "plastic"
				, "stop", "ftop", "ftation", "stat" };
			string[] strings2 = new string[] { "station", "commotion", "elation", "elastic", 
				"plastic", "stop", "ftop", "ftation" };
			IntsRef[] terms = new IntsRef[strings.Length];
			IntsRef[] terms2 = new IntsRef[strings2.Length];
			for (int inputMode = 0; inputMode < 2; inputMode++)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: inputMode=" + InputModeToString(inputMode));
				}
				for (int idx = 0; idx < strings.Length; idx++)
				{
					terms[idx] = FSTTester.ToIntsRef(strings[idx], inputMode);
				}
				for (int idx_1 = 0; idx_1 < strings2.Length; idx_1++)
				{
					terms2[idx_1] = FSTTester.ToIntsRef(strings2[idx_1], inputMode);
				}
				Arrays.Sort(terms2);
				DoTest(inputMode, terms);
				{
					// Test pre-determined FST sizes to make sure we haven't lost minimality (at least on this trivial set of terms):
					// FSA
					Outputs<object> outputs = NoOutputs.GetSingleton();
					object NO_OUTPUT = outputs.GetNoOutput();
					IList<FSTTester.InputOutput<object>> pairs = new AList<FSTTester.InputOutput<object
						>>(terms2.Length);
					foreach (IntsRef term in terms2)
					{
						pairs.AddItem(new FSTTester.InputOutput<object>(term, NO_OUTPUT));
					}
					FST<object> fst = new FSTTester<object>(Random(), dir, inputMode, pairs, outputs, 
						false).DoTest(0, 0, false);
					IsNotNull(fst);
					AreEqual(22, fst.GetNodeCount());
					AreEqual(27, fst.GetArcCount());
				}
				{
					// FST ord pos int
					PositiveIntOutputs outputs = PositiveIntOutputs.GetSingleton();
					IList<FSTTester.InputOutput<long>> pairs = new AList<FSTTester.InputOutput<long>>
						(terms2.Length);
					for (int idx_2 = 0; idx_2 < terms2.Length; idx_2++)
					{
						pairs.AddItem(new FSTTester.InputOutput<long>(terms2[idx_2], (long)idx_2));
					}
					FST<long> fst = new FSTTester<long>(Random(), dir, inputMode, pairs, outputs, true
						).DoTest(0, 0, false);
					IsNotNull(fst);
					AreEqual(22, fst.GetNodeCount());
					AreEqual(27, fst.GetArcCount());
				}
				{
					// FST byte sequence ord
					ByteSequenceOutputs outputs = ByteSequenceOutputs.GetSingleton();
					BytesRef NO_OUTPUT = outputs.GetNoOutput();
					IList<FSTTester.InputOutput<BytesRef>> pairs = new AList<FSTTester.InputOutput<BytesRef
						>>(terms2.Length);
					for (int idx_2 = 0; idx_2 < terms2.Length; idx_2++)
					{
						BytesRef output = Random().Next(30) == 17 ? NO_OUTPUT : new BytesRef(Sharpen.Extensions.ToString
							(idx_2));
						pairs.AddItem(new FSTTester.InputOutput<BytesRef>(terms2[idx_2], output));
					}
					FST<BytesRef> fst = new FSTTester<BytesRef>(Random(), dir, inputMode, pairs, outputs
						, false).DoTest(0, 0, false);
					IsNotNull(fst);
					AreEqual(24, fst.GetNodeCount());
					AreEqual(30, fst.GetArcCount());
				}
			}
		}

		// given set of terms, test the different outputs for them
		/// <exception cref="System.IO.IOException"></exception>
		private void DoTest(int inputMode, IntsRef[] terms)
		{
			Arrays.Sort(terms);
			{
				// NoOutputs (simple FSA)
				Outputs<object> outputs = NoOutputs.GetSingleton();
				object NO_OUTPUT = outputs.GetNoOutput();
				IList<FSTTester.InputOutput<object>> pairs = new AList<FSTTester.InputOutput<object
					>>(terms.Length);
				foreach (IntsRef term in terms)
				{
					pairs.AddItem(new FSTTester.InputOutput<object>(term, NO_OUTPUT));
				}
				new FSTTester<object>(Random(), dir, inputMode, pairs, outputs, false).DoTest(true
					);
			}
			{
				// PositiveIntOutput (ord)
				PositiveIntOutputs outputs = PositiveIntOutputs.GetSingleton();
				IList<FSTTester.InputOutput<long>> pairs = new AList<FSTTester.InputOutput<long>>
					(terms.Length);
				for (int idx = 0; idx < terms.Length; idx++)
				{
					pairs.AddItem(new FSTTester.InputOutput<long>(terms[idx], (long)idx));
				}
				new FSTTester<long>(Random(), dir, inputMode, pairs, outputs, true).DoTest(true);
			}
			{
				// PositiveIntOutput (random monotonically increasing positive number)
				PositiveIntOutputs outputs = PositiveIntOutputs.GetSingleton();
				IList<FSTTester.InputOutput<long>> pairs = new AList<FSTTester.InputOutput<long>>
					(terms.Length);
				long lastOutput = 0;
				for (int idx = 0; idx < terms.Length; idx++)
				{
					long value = lastOutput + TestUtil.NextInt(Random(), 1, 1000);
					lastOutput = value;
					pairs.AddItem(new FSTTester.InputOutput<long>(terms[idx], value));
				}
				new FSTTester<long>(Random(), dir, inputMode, pairs, outputs, true).DoTest(true);
			}
			{
				// PositiveIntOutput (random positive number)
				PositiveIntOutputs outputs = PositiveIntOutputs.GetSingleton();
				IList<FSTTester.InputOutput<long>> pairs = new AList<FSTTester.InputOutput<long>>
					(terms.Length);
				for (int idx = 0; idx < terms.Length; idx++)
				{
					pairs.AddItem(new FSTTester.InputOutput<long>(terms[idx], TestUtil.NextLong(Random
						(), 0, long.MaxValue)));
				}
				new FSTTester<long>(Random(), dir, inputMode, pairs, outputs, false).DoTest(true);
			}
			{
				// Pair<ord, (random monotonically increasing positive number>
				PositiveIntOutputs o1 = PositiveIntOutputs.GetSingleton();
				PositiveIntOutputs o2 = PositiveIntOutputs.GetSingleton();
				PairOutputs<long, long> outputs = new PairOutputs<long, long>(o1, o2);
				IList<FSTTester.InputOutput<PairOutputs.Pair<long, long>>> pairs = new AList<FSTTester.InputOutput
					<PairOutputs.Pair<long, long>>>(terms.Length);
				long lastOutput = 0;
				for (int idx = 0; idx < terms.Length; idx++)
				{
					long value = lastOutput + TestUtil.NextInt(Random(), 1, 1000);
					lastOutput = value;
					pairs.AddItem(new FSTTester.InputOutput<PairOutputs.Pair<long, long>>(terms[idx], 
						outputs.NewPair((long)idx, value)));
				}
				new FSTTester<PairOutputs.Pair<long, long>>(Random(), dir, inputMode, pairs, outputs
					, false).DoTest(true);
			}
			{
				// Sequence-of-bytes
				ByteSequenceOutputs outputs = ByteSequenceOutputs.GetSingleton();
				BytesRef NO_OUTPUT = outputs.GetNoOutput();
				IList<FSTTester.InputOutput<BytesRef>> pairs = new AList<FSTTester.InputOutput<BytesRef
					>>(terms.Length);
				for (int idx = 0; idx < terms.Length; idx++)
				{
					BytesRef output = Random().Next(30) == 17 ? NO_OUTPUT : new BytesRef(Sharpen.Extensions.ToString
						(idx));
					pairs.AddItem(new FSTTester.InputOutput<BytesRef>(terms[idx], output));
				}
				new FSTTester<BytesRef>(Random(), dir, inputMode, pairs, outputs, false).DoTest(true
					);
			}
			{
				// Sequence-of-ints
				IntSequenceOutputs outputs = IntSequenceOutputs.GetSingleton();
				IList<FSTTester.InputOutput<IntsRef>> pairs = new AList<FSTTester.InputOutput<IntsRef
					>>(terms.Length);
				for (int idx = 0; idx < terms.Length; idx++)
				{
					string s = Sharpen.Extensions.ToString(idx);
					IntsRef output = new IntsRef(s.Length);
					output.length = s.Length;
					for (int idx2 = 0; idx2 < output.length; idx2++)
					{
						output.ints[idx2] = s[idx2];
					}
					pairs.AddItem(new FSTTester.InputOutput<IntsRef>(terms[idx], output));
				}
				new FSTTester<IntsRef>(Random(), dir, inputMode, pairs, outputs, false).DoTest(true
					);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestRandomWords()
		{
			TestRandomWords(1000, AtLeast(2));
		}

		//testRandomWords(100, 1);
		internal virtual string InputModeToString(int mode)
		{
			if (mode == 0)
			{
				return "utf8";
			}
			else
			{
				return "utf32";
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void TestRandomWords(int maxNumWords, int numIter)
		{
			Random random = new Random(Random().NextLong());
			for (int iter = 0; iter < numIter; iter++)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("\nTEST: iter " + iter);
				}
				for (int inputMode = 0; inputMode < 2; inputMode++)
				{
					int numWords = random.Next(maxNumWords + 1);
					ICollection<IntsRef> termsSet = new HashSet<IntsRef>();
					IntsRef[] terms = new IntsRef[numWords];
					while (termsSet.Count < numWords)
					{
						string term = FSTTester.GetRandomString(random);
						termsSet.AddItem(FSTTester.ToIntsRef(term, inputMode));
					}
					DoTest(inputMode, Sharpen.Collections.ToArray(termsSet, new IntsRef[termsSet.Count
						]));
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		[LuceneTestCase.Nightly]
		public virtual void TestBigSet()
		{
			TestRandomWords(TestUtil.NextInt(Random(), 50000, 60000), 1);
		}

		// Build FST for all unique terms in the test line docs
		// file, up until a time limit
		/// <exception cref="System.Exception"></exception>
		public virtual void TestRealTerms()
		{
			LineFileDocs docs = new LineFileDocs(Random(), DefaultCodecSupportsDocValues());
			int RUN_TIME_MSEC = AtLeast(500);
			MockAnalyzer analyzer = new MockAnalyzer(Random());
			analyzer.SetMaxTokenLength(TestUtil.NextInt(Random(), 1, IndexWriter.MAX_TERM_LENGTH
				));
			IndexWriterConfig conf = ((IndexWriterConfig)((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, analyzer).SetMaxBufferedDocs(-1)).SetRAMBufferSizeMB(64));
			FilePath tempDir = CreateTempDir("fstlines");
			Directory dir = NewFSDirectory(tempDir);
			IndexWriter writer = new IndexWriter(dir, conf);
			long stopTime = DateTime.Now.CurrentTimeMillis() + RUN_TIME_MSEC;
			Lucene.Net.Documents.Document doc;
			int docCount = 0;
			while ((doc = docs.NextDoc()) != null && DateTime.Now.CurrentTimeMillis() < stopTime)
			{
				writer.AddDocument(doc);
				docCount++;
			}
			IndexReader r = DirectoryReader.Open(writer, true);
			writer.Dispose();
			PositiveIntOutputs outputs = PositiveIntOutputs.GetSingleton();
			bool doRewrite = Random().NextBoolean();
			Builder<long> builder = new Builder<long>(FST.INPUT_TYPE.BYTE1, 0, 0, true, true, 
				int.MaxValue, outputs, null, doRewrite, PackedInts.DEFAULT, true, 15);
			bool storeOrd = Random().NextBoolean();
			if (VERBOSE)
			{
				if (storeOrd)
				{
					System.Console.Out.WriteLine("FST stores ord");
				}
				else
				{
					System.Console.Out.WriteLine("FST stores docFreq");
				}
			}
			Terms terms = MultiFields.GetTerms(r, "body");
			if (terms != null)
			{
				IntsRef scratchIntsRef = new IntsRef();
				TermsEnum termsEnum = terms.Iterator(null);
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: got termsEnum=" + termsEnum);
				}
				BytesRef term;
				int ord = 0;
				Lucene.Net.Util.Automaton.Automaton automaton = new RegExp(".*", RegExp.NONE
					).ToAutomaton();
				TermsEnum termsEnum2 = terms.Intersect(new CompiledAutomaton(automaton, false, false
					), null);
				while ((term = termsEnum.Next()) != null)
				{
					BytesRef term2 = termsEnum2.Next();
					IsNotNull(term2);
					AreEqual(term, term2);
					AreEqual(termsEnum.DocFreq, termsEnum2.DocFreq);
					AreEqual(termsEnum.TotalTermFreq, termsEnum2.TotalTermFreq
						());
					if (ord == 0)
					{
						try
						{
							termsEnum.Ord();
						}
						catch (NotSupportedException)
						{
							if (VERBOSE)
							{
								System.Console.Out.WriteLine("TEST: codec doesn't support ord; FST stores docFreq"
									);
							}
							storeOrd = false;
						}
					}
					int output;
					if (storeOrd)
					{
						output = ord;
					}
					else
					{
						output = termsEnum.DocFreq;
					}
					builder.Add(Lucene.Net.Util.Fst.Util.ToIntsRef(term, scratchIntsRef), (long
						)output);
					ord++;
					if (VERBOSE && ord % 100000 == 0 && LuceneTestCase.TEST_NIGHTLY)
					{
						System.Console.Out.WriteLine(ord + " terms...");
					}
				}
				FST<long> fst = builder.Finish();
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("FST: " + docCount + " docs; " + ord + " terms; " + 
						fst.GetNodeCount() + " nodes; " + fst.GetArcCount() + " arcs;" + " " + fst.SizeInBytes
						() + " bytes");
				}
				if (ord > 0)
				{
					Random random = new Random(Random().NextLong());
					// Now confirm BytesRefFSTEnum and TermsEnum act the
					// same:
					BytesRefFSTEnum<long> fstEnum = new BytesRefFSTEnum<long>(fst);
					int num = AtLeast(1000);
					for (int iter = 0; iter < num; iter++)
					{
						BytesRef randomTerm = new BytesRef(FSTTester.GetRandomString(random));
						if (VERBOSE)
						{
							System.Console.Out.WriteLine("TEST: seek non-exist " + randomTerm.Utf8ToString() 
								+ " " + randomTerm);
						}
						TermsEnum.SeekStatus seekResult = termsEnum.SeekCeil(randomTerm);
						BytesRefFSTEnum.InputOutput<long> fstSeekResult = fstEnum.SeekCeil(randomTerm);
						if (seekResult == TermsEnum.SeekStatus.END)
						{
							IsNull("got " + (fstSeekResult == null ? "null" : fstSeekResult
								.input.Utf8ToString()) + " but expected null", fstSeekResult);
						}
						else
						{
							AssertSame(termsEnum, fstEnum, storeOrd);
							for (int nextIter = 0; nextIter < 10; nextIter++)
							{
								if (VERBOSE)
								{
									System.Console.Out.WriteLine("TEST: next");
									if (storeOrd)
									{
										System.Console.Out.WriteLine("  ord=" + termsEnum.Ord());
									}
								}
								if (termsEnum.Next() != null)
								{
									if (VERBOSE)
									{
										System.Console.Out.WriteLine("  term=" + termsEnum.Term().Utf8ToString());
									}
									IsNotNull(fstEnum.Next());
									AssertSame(termsEnum, fstEnum, storeOrd);
								}
								else
								{
									if (VERBOSE)
									{
										System.Console.Out.WriteLine("  end!");
									}
									BytesRefFSTEnum.InputOutput<long> nextResult = fstEnum.Next();
									if (nextResult != null)
									{
										System.Console.Out.WriteLine("expected null but got: input=" + nextResult.input.Utf8ToString
											() + " output=" + outputs.OutputToString(nextResult.output));
										Fail();
									}
									break;
								}
							}
						}
					}
				}
			}
			r.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		private void AssertSame<_T0>(TermsEnum termsEnum, BytesRefFSTEnum<_T0> fstEnum, bool
			 storeOrd)
		{
			if (termsEnum.Term() == null)
			{
				IsNull(fstEnum.Current());
			}
			else
			{
				IsNotNull(fstEnum.Current());
				AreEqual(termsEnum.Term().Utf8ToString() + " != " + fstEnum
					.Current().input.Utf8ToString(), termsEnum.Term(), fstEnum.Current().input);
				if (storeOrd)
				{
					// fst stored the ord
					AreEqual("term=" + termsEnum.Term().Utf8ToString() + " " +
						 termsEnum.Term(), termsEnum.Ord(), ((long)fstEnum.Current().output));
				}
				else
				{
					// fst stored the docFreq
					AreEqual("term=" + termsEnum.Term().Utf8ToString() + " " +
						 termsEnum.Term(), termsEnum.DocFreq, (int)(((long)fstEnum.Current().output)));
				}
			}
		}

		private abstract class VisitTerms<T>
		{
			private readonly string dirOut;

			private readonly string wordsFileIn;

			private int inputMode;

			private readonly Outputs<T> outputs;

			private readonly Builder<T> builder;

			private readonly bool doPack;

			public VisitTerms(string dirOut, string wordsFileIn, int inputMode, int prune, Outputs
				<T> outputs, bool doPack, bool noArcArrays)
			{
				this.dirOut = dirOut;
				this.wordsFileIn = wordsFileIn;
				this.inputMode = inputMode;
				this.outputs = outputs;
				this.doPack = doPack;
				builder = new Builder<T>(inputMode == 0 ? FST.INPUT_TYPE.BYTE1 : FST.INPUT_TYPE.BYTE4
					, 0, prune, prune == 0, true, int.MaxValue, outputs, null, doPack, PackedInts.DEFAULT
					, !noArcArrays, 15);
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected internal abstract T GetOutput(IntsRef input, int ord);

			/// <exception cref="System.IO.IOException"></exception>
			public virtual void Run(int limit, bool verify, bool verifyByOutput)
			{
				BufferedReader @is = new BufferedReader(new InputStreamReader(new FileInputStream
					(wordsFileIn), StandardCharsets.UTF_8), 65536);
				try
				{
					IntsRef intsRef = new IntsRef(10);
					long tStart = DateTime.Now.CurrentTimeMillis();
					int ord = 0;
					while (true)
					{
						string w = @is.ReadLine();
						if (w == null)
						{
							break;
						}
						FSTTester.ToIntsRef(w, inputMode, intsRef);
						builder.Add(intsRef, GetOutput(intsRef, ord));
						ord++;
						if (ord % 500000 == 0)
						{
							System.Console.Out.WriteLine(string.Format(CultureInfo.ROOT, "%6.2fs: %9d...", ((
								DateTime.Now.CurrentTimeMillis() - tStart) / 1000.0), ord));
						}
						if (ord >= limit)
						{
							break;
						}
					}
					long tMid = DateTime.Now.CurrentTimeMillis();
					System.Console.Out.WriteLine(((tMid - tStart) / 1000.0) + " sec to add all terms"
						);
					//HM:revisit 
					//assert builder.getTermCount() == ord;
					FST<T> fst = builder.Finish();
					long tEnd = DateTime.Now.CurrentTimeMillis();
					System.Console.Out.WriteLine(((tEnd - tMid) / 1000.0) + " sec to finish/pack");
					if (fst == null)
					{
						System.Console.Out.WriteLine("FST was fully pruned!");
						System.Environment.Exit(0);
					}
					if (dirOut == null)
					{
						return;
					}
					System.Console.Out.WriteLine(ord + " terms; " + fst.GetNodeCount() + " nodes; " +
						 fst.GetArcCount() + " arcs; " + fst.GetArcWithOutputCount() + " arcs w/ output; tot size "
						 + fst.SizeInBytes());
					if (fst.GetNodeCount() < 100)
					{
						TextWriter w = new OutputStreamWriter(new FileOutputStream("out.dot"), StandardCharsets
							.UTF_8);
						Lucene.Net.Util.Fst.Util.ToDot(fst, w, false, false);
						w.Dispose();
						System.Console.Out.WriteLine("Wrote FST to out.dot");
					}
					Directory dir = FSDirectory.Open(new FilePath(dirOut));
					IndexOutput @out = dir.CreateOutput("fst.bin", IOContext.DEFAULT);
					fst.Save(@out);
					@out.Dispose();
					System.Console.Out.WriteLine("Saved FST to fst.bin.");
					if (!verify)
					{
						return;
					}
					System.Console.Out.WriteLine("\nNow verify...");
					while (true)
					{
						for (int iter = 0; iter < 2; iter++)
						{
							@is.Dispose();
							@is = new BufferedReader(new InputStreamReader(new FileInputStream(wordsFileIn), 
								StandardCharsets.UTF_8), 65536);
							ord = 0;
							tStart = DateTime.Now.CurrentTimeMillis();
							while (true)
							{
								string w = @is.ReadLine();
								if (w == null)
								{
									break;
								}
								FSTTester.ToIntsRef(w, inputMode, intsRef);
								if (iter == 0)
								{
									T expected = GetOutput(intsRef, ord);
									T actual = Lucene.Net.Util.Fst.Util.Get(fst, intsRef);
									if (actual == null)
									{
										throw new RuntimeException("unexpected null output on input=" + w);
									}
									if (!actual.Equals(expected))
									{
										throw new RuntimeException("wrong output (got " + outputs.OutputToString(actual) 
											+ " but expected " + outputs.OutputToString(expected) + ") on input=" + w);
									}
								}
								else
								{
									// Get by output
									long output = (long)GetOutput(intsRef, ord);
									IntsRef actual = Lucene.Net.Util.Fst.Util.GetByOutput((FST<long>)fst, output
										);
									if (actual == null)
									{
										throw new RuntimeException("unexpected null input from output=" + output);
									}
									if (!actual.Equals(intsRef))
									{
										throw new RuntimeException("wrong input (got " + actual + " but expected " + intsRef
											 + " from output=" + output);
									}
								}
								ord++;
								if (ord % 500000 == 0)
								{
									System.Console.Out.WriteLine(((DateTime.Now.CurrentTimeMillis() - tStart) / 1000.0) + 
										"s: " + ord + "...");
								}
								if (ord >= limit)
								{
									break;
								}
							}
							double totSec = ((DateTime.Now.CurrentTimeMillis() - tStart) / 1000.0);
							System.Console.Out.WriteLine("Verify " + (iter == 1 ? "(by output) " : string.Empty
								) + "took " + totSec + " sec + (" + (int)((totSec * 1000000000 / ord)) + " nsec per lookup)"
								);
							if (!verifyByOutput)
							{
								break;
							}
						}
						// NOTE: comment out to profile lookup...
						break;
					}
				}
				finally
				{
					@is.Dispose();
				}
			}
		}

		// TODO: try experiment: reverse terms before
		// compressing -- how much smaller?
		// TODO: can FST be used to index all internal substrings,
		// mapping to term?
		// java -cp ../build/codecs/classes/java:../test-framework/lib/randomizedtesting-runner-*.jar:../build/core/classes/test:../build/core/classes/test-framework:../build/core/classes/java:../build/test-framework/classes/java:../test-framework/lib/junit-4.10.jar Lucene.Net.util.fst.TestFSTs /xold/tmp/allTerms3.txt out
		/// <exception cref="System.IO.IOException"></exception>
		public static void Main(string[] args)
		{
			int prune = 0;
			int limit = int.MaxValue;
			int inputMode = 0;
			// utf8
			bool storeOrds = false;
			bool storeDocFreqs = false;
			bool verify = true;
			bool doPack = false;
			bool noArcArrays = false;
			string wordsFileIn = null;
			string dirOut = null;
			int idx = 0;
			while (idx < args.Length)
			{
				if (args[idx].Equals("-prune"))
				{
					prune = Sharpen.Extensions.ValueOf(args[1 + idx]);
					idx++;
				}
				else
				{
					if (args[idx].Equals("-limit"))
					{
						limit = Sharpen.Extensions.ValueOf(args[1 + idx]);
						idx++;
					}
					else
					{
						if (args[idx].Equals("-utf8"))
						{
							inputMode = 0;
						}
						else
						{
							if (args[idx].Equals("-utf32"))
							{
								inputMode = 1;
							}
							else
							{
								if (args[idx].Equals("-docFreq"))
								{
									storeDocFreqs = true;
								}
								else
								{
									if (args[idx].Equals("-noArcArrays"))
									{
										noArcArrays = true;
									}
									else
									{
										if (args[idx].Equals("-ords"))
										{
											storeOrds = true;
										}
										else
										{
											if (args[idx].Equals("-noverify"))
											{
												verify = false;
											}
											else
											{
												if (args[idx].Equals("-pack"))
												{
													doPack = true;
												}
												else
												{
													if (args[idx].StartsWith("-"))
													{
														System.Console.Error.WriteLine("Unrecognized option: " + args[idx]);
														System.Environment.Exit(-1);
													}
													else
													{
														if (wordsFileIn == null)
														{
															wordsFileIn = args[idx];
														}
														else
														{
															if (dirOut == null)
															{
																dirOut = args[idx];
															}
															else
															{
																System.Console.Error.WriteLine("Too many arguments, expected: input [output]");
																System.Environment.Exit(-1);
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
				idx++;
			}
			if (wordsFileIn == null)
			{
				System.Console.Error.WriteLine("No input file.");
				System.Environment.Exit(-1);
			}
			// ord benefits from share, docFreqs don't:
			if (storeOrds && storeDocFreqs)
			{
				// Store both ord & docFreq:
				PositiveIntOutputs o1 = PositiveIntOutputs.GetSingleton();
				PositiveIntOutputs o2 = PositiveIntOutputs.GetSingleton();
				PairOutputs<long, long> outputs = new PairOutputs<long, long>(o1, o2);
				new _VisitTerms_677(outputs, dirOut, wordsFileIn, inputMode, prune, outputs, doPack
					, noArcArrays).Run(limit, verify, false);
			}
			else
			{
				if (storeOrds)
				{
					// Store only ords
					PositiveIntOutputs outputs = PositiveIntOutputs.GetSingleton();
					new _VisitTerms_691(dirOut, wordsFileIn, inputMode, prune, outputs, doPack, noArcArrays
						).Run(limit, verify, true);
				}
				else
				{
					if (storeDocFreqs)
					{
						// Store only docFreq
						PositiveIntOutputs outputs = PositiveIntOutputs.GetSingleton();
						new _VisitTerms_700(dirOut, wordsFileIn, inputMode, prune, outputs, doPack, noArcArrays
							).Run(limit, verify, false);
					}
					else
					{
						// Store nothing
						NoOutputs outputs = NoOutputs.GetSingleton();
						object NO_OUTPUT = outputs.GetNoOutput();
						new _VisitTerms_714(NO_OUTPUT, dirOut, wordsFileIn, inputMode, prune, outputs, doPack
							, noArcArrays).Run(limit, verify, false);
					}
				}
			}
		}

		private sealed class _VisitTerms_677 : TestFSTs.VisitTerms<PairOutputs.Pair<long, 
			long>>
		{
			public _VisitTerms_677(PairOutputs<long, long> outputs, string baseArg1, string baseArg2
				, int baseArg3, int baseArg4, Outputs<PairOutputs.Pair<long, long>> baseArg5, bool
				 baseArg6, bool baseArg7) : base(baseArg1, baseArg2, baseArg3, baseArg4, baseArg5
				, baseArg6, baseArg7)
			{
				this.outputs = outputs;
			}

			internal Random rand;

			protected internal override PairOutputs.Pair<long, long> GetOutput(IntsRef input, 
				int ord)
			{
				if (ord == 0)
				{
					this.rand = new Random(17);
				}
				return outputs.NewPair((long)ord, (long)TestUtil.NextInt(this.rand, 1, 5000));
			}

			private readonly PairOutputs<long, long> outputs;
		}

		private sealed class _VisitTerms_691 : TestFSTs.VisitTerms<long>
		{
			public _VisitTerms_691(string baseArg1, string baseArg2, int baseArg3, int baseArg4
				, Outputs<long> baseArg5, bool baseArg6, bool baseArg7) : base(baseArg1, baseArg2
				, baseArg3, baseArg4, baseArg5, baseArg6, baseArg7)
			{
			}

			protected internal override long GetOutput(IntsRef input, int ord)
			{
				return (long)ord;
			}
		}

		private sealed class _VisitTerms_700 : TestFSTs.VisitTerms<long>
		{
			public _VisitTerms_700(string baseArg1, string baseArg2, int baseArg3, int baseArg4
				, Outputs<long> baseArg5, bool baseArg6, bool baseArg7) : base(baseArg1, baseArg2
				, baseArg3, baseArg4, baseArg5, baseArg6, baseArg7)
			{
			}

			internal Random rand;

			protected internal override long GetOutput(IntsRef input, int ord)
			{
				if (ord == 0)
				{
					this.rand = new Random(17);
				}
				return (long)TestUtil.NextInt(this.rand, 1, 5000);
			}
		}

		private sealed class _VisitTerms_714 : TestFSTs.VisitTerms<object>
		{
			public _VisitTerms_714(object NO_OUTPUT, string baseArg1, string baseArg2, int baseArg3
				, int baseArg4, Outputs<object> baseArg5, bool baseArg6, bool baseArg7) : base(baseArg1
				, baseArg2, baseArg3, baseArg4, baseArg5, baseArg6, baseArg7)
			{
				this.NO_OUTPUT = NO_OUTPUT;
			}

			protected internal override object GetOutput(IntsRef input, int ord)
			{
				return NO_OUTPUT;
			}

			private readonly object NO_OUTPUT;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSingleString()
		{
			Outputs<object> outputs = NoOutputs.GetSingleton();
			Builder<object> b = new Builder<object>(FST.INPUT_TYPE.BYTE1, outputs);
			b.Add(Lucene.Net.Util.Fst.Util.ToIntsRef(new BytesRef("foobar"), new IntsRef
				()), outputs.GetNoOutput());
			BytesRefFSTEnum<object> fstEnum = new BytesRefFSTEnum<object>(b.Finish());
			IsNull(fstEnum.SeekFloor(new BytesRef("foo")));
			IsNull(fstEnum.SeekCeil(new BytesRef("foobaz")));
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestDuplicateFSAString()
		{
			string str = "foobar";
			Outputs<object> outputs = NoOutputs.GetSingleton();
			Builder<object> b = new Builder<object>(FST.INPUT_TYPE.BYTE1, outputs);
			IntsRef ints = new IntsRef();
			for (int i = 0; i < 10; i++)
			{
				b.Add(Lucene.Net.Util.Fst.Util.ToIntsRef(new BytesRef(str), ints), outputs
					.GetNoOutput());
			}
			FST<object> fst = b.Finish();
			// count the input paths
			int count = 0;
			BytesRefFSTEnum<object> fstEnum = new BytesRefFSTEnum<object>(fst);
			while (fstEnum.Next() != null)
			{
				count++;
			}
			AreEqual(1, count);
			IsNotNull(Lucene.Net.Util.Fst.Util.Get(fst, new BytesRef
				(str)));
			IsNull(Lucene.Net.Util.Fst.Util.Get(fst, new BytesRef
				("foobaz")));
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSimple()
		{
			// Get outputs -- passing true means FST will share
			// (delta code) the outputs.  This should result in
			// smaller FST if the outputs grow monotonically.  But
			// if numbers are "random", false should give smaller
			// final size:
			PositiveIntOutputs outputs = PositiveIntOutputs.GetSingleton();
			// Build an FST mapping BytesRef -> Long
			Builder<long> builder = new Builder<long>(FST.INPUT_TYPE.BYTE1, outputs);
			BytesRef a = new BytesRef("a");
			BytesRef b = new BytesRef("b");
			BytesRef c = new BytesRef("c");
			builder.Add(Lucene.Net.Util.Fst.Util.ToIntsRef(a, new IntsRef()), 17L);
			builder.Add(Lucene.Net.Util.Fst.Util.ToIntsRef(b, new IntsRef()), 42L);
			builder.Add(Lucene.Net.Util.Fst.Util.ToIntsRef(c, new IntsRef()), 13824324872317238L
				);
			FST<long> fst = builder.Finish();
			AreEqual(13824324872317238L, (long)Lucene.Net.Util.Fst.Util
				.Get(fst, c));
			AreEqual(42, (long)Lucene.Net.Util.Fst.Util.Get(fst
				, b));
			AreEqual(17, (long)Lucene.Net.Util.Fst.Util.Get(fst
				, a));
			BytesRefFSTEnum<long> fstEnum = new BytesRefFSTEnum<long>(fst);
			BytesRefFSTEnum.InputOutput<long> seekResult;
			seekResult = fstEnum.SeekFloor(a);
			IsNotNull(seekResult);
			AreEqual(17, (long)seekResult.output);
			// goes to a
			seekResult = fstEnum.SeekFloor(new BytesRef("aa"));
			IsNotNull(seekResult);
			AreEqual(17, (long)seekResult.output);
			// goes to b
			seekResult = fstEnum.SeekCeil(new BytesRef("aa"));
			IsNotNull(seekResult);
			AreEqual(b, seekResult.input);
			AreEqual(42, (long)seekResult.output);
			AreEqual(Lucene.Net.Util.Fst.Util.ToIntsRef(new BytesRef
				("c"), new IntsRef()), Lucene.Net.Util.Fst.Util.GetByOutput(fst, 13824324872317238L
				));
			IsNull(Lucene.Net.Util.Fst.Util.GetByOutput(fst, 47
				));
			AreEqual(Lucene.Net.Util.Fst.Util.ToIntsRef(new BytesRef
				("b"), new IntsRef()), Lucene.Net.Util.Fst.Util.GetByOutput(fst, 42));
			AreEqual(Lucene.Net.Util.Fst.Util.ToIntsRef(new BytesRef
				("a"), new IntsRef()), Lucene.Net.Util.Fst.Util.GetByOutput(fst, 17));
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestPrimaryKeys()
		{
			Directory dir = NewDirectory();
			for (int cycle = 0; cycle < 2; cycle++)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: cycle=" + cycle);
				}
				RandomIndexWriter w = new RandomIndexWriter(Random(), dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
					, new MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode.CREATE));
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				Field idField = NewStringField("id", string.Empty, Field.Store.NO);
				doc.Add(idField);
				int NUM_IDS = AtLeast(200);
				//final int NUM_IDS = (int) (377 * (1.0+random.nextDouble()));
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: NUM_IDS=" + NUM_IDS);
				}
				ICollection<string> allIDs = new HashSet<string>();
				for (int id = 0; id < NUM_IDS; id++)
				{
					string idString;
					if (cycle == 0)
					{
						// PKs are assigned sequentially
						idString = string.Format(CultureInfo.ROOT, "%07d", id);
					}
					else
					{
						while (true)
						{
							string s = System.Convert.ToString(Random().NextLong());
							if (!allIDs.Contains(s))
							{
								idString = s;
								break;
							}
						}
					}
					allIDs.AddItem(idString);
					idField.StringValue = idString);
					w.AddDocument(doc);
				}
				//w.forceMerge(1);
				// turn writer into reader:
				IndexReader r = w.GetReader();
				IndexSearcher s_1 = NewSearcher(r);
				w.Dispose();
				IList<string> allIDsList = new AList<string>(allIDs);
				IList<string> sortedAllIDsList = new AList<string>(allIDsList);
				sortedAllIDsList.Sort();
				// Sprinkle in some non-existent PKs:
				ICollection<string> outOfBounds = new HashSet<string>();
				for (int idx = 0; idx < NUM_IDS / 10; idx++)
				{
					string idString;
					if (cycle == 0)
					{
						idString = string.Format(CultureInfo.ROOT, "%07d", (NUM_IDS + idx));
					}
					else
					{
						while (true)
						{
							idString = System.Convert.ToString(Random().NextLong());
							if (!allIDs.Contains(idString))
							{
								break;
							}
						}
					}
					outOfBounds.AddItem(idString);
					allIDsList.AddItem(idString);
				}
				// Verify w/ TermQuery
				for (int iter = 0; iter < 2 * NUM_IDS; iter++)
				{
					string id_1 = allIDsList[Random().Next(allIDsList.Count)];
					bool exists = !outOfBounds.Contains(id_1);
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("TEST: TermQuery " + (exists ? string.Empty : "non-exist "
							) + " id=" + id_1);
					}
					AreEqual((exists ? string.Empty : "non-exist ") + "id=" + 
						id_1, exists ? 1 : 0, s_1.Search(new TermQuery(new Term("id", id_1)), 1).TotalHits
						);
				}
				// Verify w/ MultiTermsEnum
				TermsEnum termsEnum = MultiFields.GetTerms(r, "id").Iterator(null);
				for (int iter_1 = 0; iter_1 < 2 * NUM_IDS; iter_1++)
				{
					string id_1;
					string nextID;
					bool exists;
					if (Random().NextBoolean())
					{
						id_1 = allIDsList[Random().Next(allIDsList.Count)];
						exists = !outOfBounds.Contains(id_1);
						nextID = null;
						if (VERBOSE)
						{
							System.Console.Out.WriteLine("TEST: exactOnly " + (exists ? string.Empty : "non-exist "
								) + "id=" + id_1);
						}
					}
					else
					{
						// Pick ID between two IDs:
						exists = false;
						int idv = Random().Next(NUM_IDS - 1);
						if (cycle == 0)
						{
							id_1 = string.Format(CultureInfo.ROOT, "%07da", idv);
							nextID = string.Format(CultureInfo.ROOT, "%07d", idv + 1);
						}
						else
						{
							id_1 = sortedAllIDsList[idv] + "a";
							nextID = sortedAllIDsList[idv + 1];
						}
						if (VERBOSE)
						{
							System.Console.Out.WriteLine("TEST: not exactOnly id=" + id_1 + " nextID=" + nextID
								);
						}
					}
					TermsEnum.SeekStatus status;
					if (nextID == null)
					{
						if (termsEnum.SeekExact(new BytesRef(id_1)))
						{
							status = TermsEnum.SeekStatus.FOUND;
						}
						else
						{
							status = TermsEnum.SeekStatus.NOT_FOUND;
						}
					}
					else
					{
						status = termsEnum.SeekCeil(new BytesRef(id_1));
					}
					if (nextID != null)
					{
						AreEqual(TermsEnum.SeekStatus.NOT_FOUND, status);
						AreEqual("expected=" + nextID + " actual=" + termsEnum.Term
							().Utf8ToString(), new BytesRef(nextID), termsEnum.Term());
					}
					else
					{
						if (!exists)
						{
							IsTrue(status == TermsEnum.SeekStatus.NOT_FOUND || status 
								== TermsEnum.SeekStatus.END);
						}
						else
						{
							AreEqual(TermsEnum.SeekStatus.FOUND, status);
						}
					}
				}
				r.Dispose();
			}
			dir.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestRandomTermLookup()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode.CREATE));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			Field f = NewStringField("field", string.Empty, Field.Store.NO);
			doc.Add(f);
			int NUM_TERMS = (int)(1000 * RANDOM_MULTIPLIER * (1 + Random().NextDouble()));
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: NUM_TERMS=" + NUM_TERMS);
			}
			ICollection<string> allTerms = new HashSet<string>();
			while (allTerms.Count < NUM_TERMS)
			{
				allTerms.AddItem(FSTTester.SimpleRandomString(Random()));
			}
			foreach (string term in allTerms)
			{
				f.StringValue = term);
				w.AddDocument(doc);
			}
			// turn writer into reader:
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: get reader");
			}
			IndexReader r = w.GetReader();
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: got reader=" + r);
			}
			IndexSearcher s = NewSearcher(r);
			w.Dispose();
			IList<string> allTermsList = new AList<string>(allTerms);
			Sharpen.Collections.Shuffle(allTermsList, Random());
			// verify exact lookup
			foreach (string term_1 in allTermsList)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: term=" + term_1);
				}
				AreEqual("term=" + term_1, 1, s.Search(new TermQuery(new Term
					("field", term_1)), 1).TotalHits);
			}
			r.Dispose();
			dir.Dispose();
		}

		/// <summary>Test state expansion (array format) on close-to-root states.</summary>
		/// <remarks>
		/// Test state expansion (array format) on close-to-root states. Creates
		/// synthetic input that has one expanded state on each level.
		/// </remarks>
		/// <seealso>"https://issues.apache.org/jira/browse/LUCENE-2933"</seealso>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestExpandedCloseToRoot()
		{
			// Sanity check.
			IsTrue(FST.FIXED_ARRAY_NUM_ARCS_SHALLOW < FST.FIXED_ARRAY_NUM_ARCS_DEEP
				);
			IsTrue(FST.FIXED_ARRAY_SHALLOW_DISTANCE >= 0);
			_T1856215747 s = new _T1856215747(this);
			AList<string> @out = new AList<string>();
			StringBuilder b = new StringBuilder();
			s.Generate(@out, b, 'a', 'i', 10);
			string[] input = Sharpen.Collections.ToArray(@out, new string[@out.Count]);
			Arrays.Sort(input);
			FST<object> fst = s.Compile(input);
			FST.Arc<object> arc = fst.GetFirstArc(new FST.Arc<object>());
			s.VerifyStateAndBelow(fst, arc, 1);
		}

		internal class _T1856215747
		{
			/// <exception cref="System.IO.IOException"></exception>
			internal virtual FST<object> Compile(string[] lines)
			{
				NoOutputs outputs = NoOutputs.GetSingleton();
				object nothing = outputs.GetNoOutput();
				Builder<object> b = new Builder<object>(FST.INPUT_TYPE.BYTE1, outputs);
				int line = 0;
				BytesRef term = new BytesRef();
				IntsRef scratchIntsRef = new IntsRef();
				while (line < lines.Length)
				{
					string w = lines[line++];
					if (w == null)
					{
						break;
					}
					term.CopyChars(w);
					b.Add(Lucene.Net.Util.Fst.Util.ToIntsRef(term, scratchIntsRef), nothing);
				}
				return b.Finish();
			}

			internal virtual void Generate(AList<string> @out, StringBuilder b, char from, char
				 to, int depth)
			{
				if (depth == 0 || from == to)
				{
					string seq = b.ToString() + "_" + @out.Count + "_end";
					@out.AddItem(seq);
				}
				else
				{
					for (char c = from; c <= to; c++)
					{
						b.Append(c);
						this.Generate(@out, b, from, c == to ? to : from, depth - 1);
						Sharpen.Runtime.DeleteCharAt(b, b.Length - 1);
					}
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public virtual int VerifyStateAndBelow(FST<object> fst, FST.Arc<object> arc, int 
				depth)
			{
				if (FST.TargetHasArcs(arc))
				{
					int childCount = 0;
					FST.BytesReader fstReader = fst.GetBytesReader();
					for (arc = fst.ReadFirstTargetArc(arc, arc, fstReader); ; arc = fst.ReadNextArc(arc
						, fstReader), childCount++)
					{
						bool expanded = fst.IsExpandedTarget(arc, fstReader);
						int children = this.VerifyStateAndBelow(fst, new FST.Arc<object>().CopyFrom(arc), 
							depth + 1);
						AreEqual(expanded, (depth <= FST.FIXED_ARRAY_SHALLOW_DISTANCE
							 && children >= FST.FIXED_ARRAY_NUM_ARCS_SHALLOW) || children >= FST.FIXED_ARRAY_NUM_ARCS_DEEP
							);
						if (arc.IsLast())
						{
							break;
						}
					}
					return childCount;
				}
				return 0;
			}

			internal _T1856215747(TestFSTs _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly TestFSTs _enclosing;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestFinalOutputOnEndState()
		{
			PositiveIntOutputs outputs = PositiveIntOutputs.GetSingleton();
			Builder<long> builder = new Builder<long>(FST.INPUT_TYPE.BYTE4, 2, 0, true, true, 
				int.MaxValue, outputs, null, Random().NextBoolean(), PackedInts.DEFAULT, true, 15
				);
			builder.Add(Lucene.Net.Util.Fst.Util.ToUTF32("stat", new IntsRef()), 17L);
			builder.Add(Lucene.Net.Util.Fst.Util.ToUTF32("station", new IntsRef()), 10L
				);
			FST<long> fst = builder.Finish();
			//Writer w = new OutputStreamWriter(new FileOutputStream("/x/tmp3/out.dot"));
			StringWriter w = new StringWriter();
			Lucene.Net.Util.Fst.Util.ToDot(fst, w, false, false);
			w.Dispose();
			//System.out.println(w.toString());
			IsTrue(w.ToString().IndexOf("label=\"t/[7]\"") != -1);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestInternalFinalState()
		{
			PositiveIntOutputs outputs = PositiveIntOutputs.GetSingleton();
			bool willRewrite = Random().NextBoolean();
			Builder<long> builder = new Builder<long>(FST.INPUT_TYPE.BYTE1, 0, 0, true, true, 
				int.MaxValue, outputs, null, willRewrite, PackedInts.DEFAULT, true, 15);
			builder.Add(Lucene.Net.Util.Fst.Util.ToIntsRef(new BytesRef("stat"), new IntsRef
				()), outputs.GetNoOutput());
			builder.Add(Lucene.Net.Util.Fst.Util.ToIntsRef(new BytesRef("station"), new 
				IntsRef()), outputs.GetNoOutput());
			FST<long> fst = builder.Finish();
			StringWriter w = new StringWriter();
			//Writer w = new OutputStreamWriter(new FileOutputStream("/x/tmp/out.dot"));
			Lucene.Net.Util.Fst.Util.ToDot(fst, w, false, false);
			w.Dispose();
			//System.out.println(w.toString());
			// check for accept state at label t
			IsTrue(w.ToString().IndexOf("[label=\"t\" style=\"bold\"")
				 != -1);
			// check for accept state at label n
			IsTrue(w.ToString().IndexOf("[label=\"n\" style=\"bold\"")
				 != -1);
		}

		// Make sure raw FST can differentiate between final vs
		// non-final end nodes
		/// <exception cref="System.Exception"></exception>
		public virtual void TestNonFinalStopNode()
		{
			PositiveIntOutputs outputs = PositiveIntOutputs.GetSingleton();
			long nothing = outputs.GetNoOutput();
			Builder<long> b = new Builder<long>(FST.INPUT_TYPE.BYTE1, outputs);
			FST<long> fst = new FST<long>(FST.INPUT_TYPE.BYTE1, outputs, false, PackedInts.COMPACT
				, true, 15);
			Builder.UnCompiledNode<long> rootNode = new Builder.UnCompiledNode<long>(b, 0);
			{
				// Add final stop node
				Builder.UnCompiledNode<long> node = new Builder.UnCompiledNode<long>(b, 0);
				node.isFinal = true;
				rootNode.AddArc('a', node);
				Builder.CompiledNode frozen = new Builder.CompiledNode();
				frozen.node = fst.AddNode(node);
				rootNode.arcs[0].nextFinalOutput = 17L;
				rootNode.arcs[0].isFinal = true;
				rootNode.arcs[0].output = nothing;
				rootNode.arcs[0].target = frozen;
			}
			{
				// Add non-final stop node
				Builder.UnCompiledNode<long> node = new Builder.UnCompiledNode<long>(b, 0);
				rootNode.AddArc('b', node);
				Builder.CompiledNode frozen = new Builder.CompiledNode();
				frozen.node = fst.AddNode(node);
				rootNode.arcs[1].nextFinalOutput = nothing;
				rootNode.arcs[1].output = 42L;
				rootNode.arcs[1].target = frozen;
			}
			fst.Finish(fst.AddNode(rootNode));
			StringWriter w = new StringWriter();
			//Writer w = new OutputStreamWriter(new FileOutputStream("/x/tmp3/out.dot"));
			Lucene.Net.Util.Fst.Util.ToDot(fst, w, false, false);
			w.Dispose();
			CheckStopNodes(fst, outputs);
			// Make sure it still works after save/load:
			Directory dir = NewDirectory();
			IndexOutput @out = dir.CreateOutput("fst", IOContext.DEFAULT);
			fst.Save(@out);
			@out.Dispose();
			IndexInput @in = dir.OpenInput("fst", IOContext.DEFAULT);
			FST<long> fst2 = new FST<long>(@in, outputs);
			CheckStopNodes(fst2, outputs);
			@in.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		private void CheckStopNodes(FST<long> fst, PositiveIntOutputs outputs)
		{
			long nothing = outputs.GetNoOutput();
			FST.Arc<long> startArc = fst.GetFirstArc(new FST.Arc<long>());
			AreEqual(nothing, startArc.output);
			AreEqual(nothing, startArc.nextFinalOutput);
			FST.Arc<long> arc = fst.ReadFirstTargetArc(startArc, new FST.Arc<long>(), fst.GetBytesReader
				());
			AreEqual('a', arc.label);
			AreEqual(17, arc.nextFinalOutput);
			IsTrue(arc.IsFinal());
			arc = fst.ReadNextArc(arc, fst.GetBytesReader());
			AreEqual('b', arc.label);
			IsFalse(arc.IsFinal());
			AreEqual(42, arc.output);
		}

		private sealed class _IComparer_1224 : IComparer<long>
		{
			public _IComparer_1224()
			{
			}

			public int Compare(long left, long right)
			{
				return left.CompareTo(right);
			}
		}

		internal static readonly IComparer<long> minLongComparator = new _IComparer_1224(
			);

		/// <exception cref="System.Exception"></exception>
		public virtual void TestShortestPaths()
		{
			PositiveIntOutputs outputs = PositiveIntOutputs.GetSingleton();
			Builder<long> builder = new Builder<long>(FST.INPUT_TYPE.BYTE1, outputs);
			IntsRef scratch = new IntsRef();
			builder.Add(Lucene.Net.Util.Fst.Util.ToIntsRef(new BytesRef("aab"), scratch
				), 22L);
			builder.Add(Lucene.Net.Util.Fst.Util.ToIntsRef(new BytesRef("aac"), scratch
				), 7L);
			builder.Add(Lucene.Net.Util.Fst.Util.ToIntsRef(new BytesRef("ax"), scratch
				), 17L);
			FST<long> fst = builder.Finish();
			//Writer w = new OutputStreamWriter(new FileOutputStream("out.dot"));
			//Util.toDot(fst, w, false, false);
			//w.close();
			Util.TopResults<long> res = Lucene.Net.Util.Fst.Util.ShortestPaths(fst, fst
				.GetFirstArc(new FST.Arc<long>()), outputs.GetNoOutput(), minLongComparator, 3, 
				true);
			IsTrue(res.isComplete);
			AreEqual(3, res.topN.Count);
			AreEqual(Lucene.Net.Util.Fst.Util.ToIntsRef(new BytesRef
				("aac"), scratch), res.topN[0].input);
			AreEqual(7L, res.topN[0].output);
			AreEqual(Lucene.Net.Util.Fst.Util.ToIntsRef(new BytesRef
				("ax"), scratch), res.topN[1].input);
			AreEqual(17L, res.topN[1].output);
			AreEqual(Lucene.Net.Util.Fst.Util.ToIntsRef(new BytesRef
				("aab"), scratch), res.topN[2].input);
			AreEqual(22L, res.topN[2].output);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestRejectNoLimits()
		{
			PositiveIntOutputs outputs = PositiveIntOutputs.GetSingleton();
			Builder<long> builder = new Builder<long>(FST.INPUT_TYPE.BYTE1, outputs);
			IntsRef scratch = new IntsRef();
			builder.Add(Lucene.Net.Util.Fst.Util.ToIntsRef(new BytesRef("aab"), scratch
				), 22L);
			builder.Add(Lucene.Net.Util.Fst.Util.ToIntsRef(new BytesRef("aac"), scratch
				), 7L);
			builder.Add(Lucene.Net.Util.Fst.Util.ToIntsRef(new BytesRef("adcd"), scratch
				), 17L);
			builder.Add(Lucene.Net.Util.Fst.Util.ToIntsRef(new BytesRef("adcde"), scratch
				), 17L);
			builder.Add(Lucene.Net.Util.Fst.Util.ToIntsRef(new BytesRef("ax"), scratch
				), 17L);
			FST<long> fst = builder.Finish();
			AtomicInteger rejectCount = new AtomicInteger();
			Util.TopNSearcher<long> searcher = new _TopNSearcher_1275(rejectCount, fst, 2, 6, 
				minLongComparator);
			searcher.AddStartPaths(fst.GetFirstArc(new FST.Arc<long>()), outputs.GetNoOutput(
				), true, new IntsRef());
			Util.TopResults<long> res = searcher.Search();
			AreEqual(rejectCount.Get(), 4);
			IsTrue(res.isComplete);
			// rejected(4) + topN(2) <= maxQueueSize(6)
			AreEqual(1, res.topN.Count);
			AreEqual(Lucene.Net.Util.Fst.Util.ToIntsRef(new BytesRef
				("aac"), scratch), res.topN[0].input);
			AreEqual(7L, res.topN[0].output);
			rejectCount.Set(0);
			searcher = new _TopNSearcher_1295(rejectCount, fst, 2, 5, minLongComparator);
			searcher.AddStartPaths(fst.GetFirstArc(new FST.Arc<long>()), outputs.GetNoOutput(
				), true, new IntsRef());
			res = searcher.Search();
			AreEqual(rejectCount.Get(), 4);
			IsFalse(res.isComplete);
		}

		private sealed class _TopNSearcher_1275 : Util.TopNSearcher<long>
		{
			public _TopNSearcher_1275(AtomicInteger rejectCount, FST<long> baseArg1, int baseArg2
				, int baseArg3, IComparer<long> baseArg4) : base(baseArg1, baseArg2, baseArg3, baseArg4
				)
			{
				this.rejectCount = rejectCount;
			}

			protected override bool AcceptResult(IntsRef input, long output)
			{
				bool accept = output == 7;
				if (!accept)
				{
					rejectCount.IncrementAndGet();
				}
				return accept;
			}

			private readonly AtomicInteger rejectCount;
		}

		private sealed class _TopNSearcher_1295 : Util.TopNSearcher<long>
		{
			public _TopNSearcher_1295(AtomicInteger rejectCount, FST<long> baseArg1, int baseArg2
				, int baseArg3, IComparer<long> baseArg4) : base(baseArg1, baseArg2, baseArg3, baseArg4
				)
			{
				this.rejectCount = rejectCount;
			}

			protected override bool AcceptResult(IntsRef input, long output)
			{
				bool accept = output == 7;
				if (!accept)
				{
					rejectCount.IncrementAndGet();
				}
				return accept;
			}

			private readonly AtomicInteger rejectCount;
		}

		private sealed class _IComparer_1313 : IComparer<PairOutputs.Pair<long, long>>
		{
			public _IComparer_1313()
			{
			}

			// rejected(4) + topN(2) > maxQueueSize(5)
			// compares just the weight side of the pair
			public int Compare(PairOutputs.Pair<long, long> left, PairOutputs.Pair<long, long
				> right)
			{
				return left.output1.CompareTo(right.output1);
			}
		}

		internal static readonly IComparer<PairOutputs.Pair<long, long>> minPairWeightComparator
			 = new _IComparer_1313();

		/// <summary>like testShortestPaths, but uses pairoutputs so we have both a weight and an output
		/// 	</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestShortestPathsWFST()
		{
			PairOutputs<long, long> outputs = new PairOutputs<long, long>(PositiveIntOutputs.
				GetSingleton(), PositiveIntOutputs.GetSingleton());
			// weight
			// output
			Builder<PairOutputs.Pair<long, long>> builder = new Builder<PairOutputs.Pair<long
				, long>>(FST.INPUT_TYPE.BYTE1, outputs);
			IntsRef scratch = new IntsRef();
			builder.Add(Lucene.Net.Util.Fst.Util.ToIntsRef(new BytesRef("aab"), scratch
				), outputs.NewPair(22L, 57L));
			builder.Add(Lucene.Net.Util.Fst.Util.ToIntsRef(new BytesRef("aac"), scratch
				), outputs.NewPair(7L, 36L));
			builder.Add(Lucene.Net.Util.Fst.Util.ToIntsRef(new BytesRef("ax"), scratch
				), outputs.NewPair(17L, 85L));
			FST<PairOutputs.Pair<long, long>> fst = builder.Finish();
			//Writer w = new OutputStreamWriter(new FileOutputStream("out.dot"));
			//Util.toDot(fst, w, false, false);
			//w.close();
			Util.TopResults<PairOutputs.Pair<long, long>> res = Lucene.Net.Util.Fst.Util
				.ShortestPaths(fst, fst.GetFirstArc(new FST.Arc<PairOutputs.Pair<long, long>>())
				, outputs.GetNoOutput(), minPairWeightComparator, 3, true);
			IsTrue(res.isComplete);
			AreEqual(3, res.topN.Count);
			AreEqual(Lucene.Net.Util.Fst.Util.ToIntsRef(new BytesRef
				("aac"), scratch), res.topN[0].input);
			AreEqual(7L, res.topN[0].output.output1);
			// weight
			AreEqual(36L, res.topN[0].output.output2);
			// output
			AreEqual(Lucene.Net.Util.Fst.Util.ToIntsRef(new BytesRef
				("ax"), scratch), res.topN[1].input);
			AreEqual(17L, res.topN[1].output.output1);
			// weight
			AreEqual(85L, res.topN[1].output.output2);
			// output
			AreEqual(Lucene.Net.Util.Fst.Util.ToIntsRef(new BytesRef
				("aab"), scratch), res.topN[2].input);
			AreEqual(22L, res.topN[2].output.output1);
			// weight
			AreEqual(57L, res.topN[2].output.output2);
		}

		// output
		/// <exception cref="System.Exception"></exception>
		public virtual void TestShortestPathsRandom()
		{
			Random random = Random();
			int numWords = AtLeast(1000);
			SortedDictionary<string, long> slowCompletor = new SortedDictionary<string, long>
				();
			TreeSet<string> allPrefixes = new TreeSet<string>();
			PositiveIntOutputs outputs = PositiveIntOutputs.GetSingleton();
			Builder<long> builder = new Builder<long>(FST.INPUT_TYPE.BYTE1, outputs);
			IntsRef scratch = new IntsRef();
			for (int i = 0; i < numWords; i++)
			{
				string s;
				while (true)
				{
					s = TestUtil.RandomSimpleString(random);
					if (!slowCompletor.ContainsKey(s))
					{
						break;
					}
				}
				for (int j = 1; j < s.Length; j++)
				{
					allPrefixes.AddItem(Sharpen.Runtime.Substring(s, 0, j));
				}
				int weight = TestUtil.NextInt(random, 1, 100);
				// weights 1..100
				slowCompletor.Put(s, (long)weight);
			}
			foreach (KeyValuePair<string, long> e in slowCompletor.EntrySet())
			{
				//System.out.println("add: " + e);
				builder.Add(Lucene.Net.Util.Fst.Util.ToIntsRef(new BytesRef(e.Key), scratch
					), e.Value);
			}
			FST<long> fst = builder.Finish();
			//System.out.println("SAVE out.dot");
			//Writer w = new OutputStreamWriter(new FileOutputStream("out.dot"));
			//Util.toDot(fst, w, false, false);
			//w.close();
			FST.BytesReader reader = fst.GetBytesReader();
			//System.out.println("testing: " + allPrefixes.size() + " prefixes");
			foreach (string prefix in allPrefixes)
			{
				// 1. run prefix against fst, then complete by value
				//System.out.println("TEST: " + prefix);
				long prefixOutput = 0;
				FST.Arc<long> arc = fst.GetFirstArc(new FST.Arc<long>());
				for (int idx = 0; idx < prefix.Length; idx++)
				{
					if (fst.FindTargetArc((int)prefix[idx], arc, arc, reader) == null)
					{
						Fail();
					}
					prefixOutput += arc.output;
				}
				int topN = TestUtil.NextInt(random, 1, 10);
				Util.TopResults<long> r = Lucene.Net.Util.Fst.Util.ShortestPaths(fst, arc, 
					fst.outputs.GetNoOutput(), minLongComparator, topN, true);
				IsTrue(r.isComplete);
				// 2. go thru whole treemap (slowCompletor) and check its actually the best suggestion
				IList<Util.Result<long>> matches = new AList<Util.Result<long>>();
				// TODO: could be faster... but its slowCompletor for a reason
				foreach (KeyValuePair<string, long> e_1 in slowCompletor.EntrySet())
				{
					if (e_1.Key.StartsWith(prefix))
					{
						//System.out.println("  consider " + e.getKey());
						matches.AddItem(new Util.Result<long>(Lucene.Net.Util.Fst.Util.ToIntsRef(new 
							BytesRef(Sharpen.Runtime.Substring(e_1.Key, prefix.Length)), new IntsRef()), e_1
							.Value - prefixOutput));
					}
				}
				IsTrue(matches.Count > 0);
				matches.Sort(new TestFSTs.TieBreakByInputComparator<long>(minLongComparator));
				if (matches.Count > topN)
				{
					matches.SubList(topN, matches.Count).Clear();
				}
				AreEqual(matches.Count, r.topN.Count);
				for (int hit = 0; hit < r.topN.Count; hit++)
				{
					//System.out.println("  check hit " + hit);
					AreEqual(matches[hit].input, r.topN[hit].input);
					AreEqual(matches[hit].output, r.topN[hit].output);
				}
			}
		}

		private class TieBreakByInputComparator<T> : IComparer<Util.Result<T>>
		{
			private readonly IComparer<T> comparator;

			public TieBreakByInputComparator(IComparer<T> comparator)
			{
				this.comparator = comparator;
			}

			public virtual int Compare(Util.Result<T> a, Util.Result<T> b)
			{
				int cmp = comparator.Compare(a.output, b.output);
				if (cmp == 0)
				{
					return a.input.CompareTo(b.input);
				}
				else
				{
					return cmp;
				}
			}
		}

		internal class TwoLongs
		{
			internal long a;

			internal long b;

			internal TwoLongs(TestFSTs _enclosing, long a, long b)
			{
				this._enclosing = _enclosing;
				// used by slowcompletor
				this.a = a;
				this.b = b;
			}

			private readonly TestFSTs _enclosing;
		}

		/// <summary>like testShortestPathsRandom, but uses pairoutputs so we have both a weight and an output
		/// 	</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestShortestPathsWFSTRandom()
		{
			int numWords = AtLeast(1000);
			SortedDictionary<string, TestFSTs.TwoLongs> slowCompletor = new SortedDictionary<
				string, TestFSTs.TwoLongs>();
			TreeSet<string> allPrefixes = new TreeSet<string>();
			PairOutputs<long, long> outputs = new PairOutputs<long, long>(PositiveIntOutputs.
				GetSingleton(), PositiveIntOutputs.GetSingleton());
			// weight
			// output
			Builder<PairOutputs.Pair<long, long>> builder = new Builder<PairOutputs.Pair<long
				, long>>(FST.INPUT_TYPE.BYTE1, outputs);
			IntsRef scratch = new IntsRef();
			Random random = Random();
			for (int i = 0; i < numWords; i++)
			{
				string s;
				while (true)
				{
					s = TestUtil.RandomSimpleString(random);
					if (!slowCompletor.ContainsKey(s))
					{
						break;
					}
				}
				for (int j = 1; j < s.Length; j++)
				{
					allPrefixes.AddItem(Sharpen.Runtime.Substring(s, 0, j));
				}
				int weight = TestUtil.NextInt(random, 1, 100);
				// weights 1..100
				int output = TestUtil.NextInt(random, 0, 500);
				// outputs 0..500
				slowCompletor.Put(s, new TestFSTs.TwoLongs(this, weight, output));
			}
			foreach (KeyValuePair<string, TestFSTs.TwoLongs> e in slowCompletor.EntrySet())
			{
				//System.out.println("add: " + e);
				long weight = e.Value.a;
				long output = e.Value.b;
				builder.Add(Lucene.Net.Util.Fst.Util.ToIntsRef(new BytesRef(e.Key), scratch
					), outputs.NewPair(weight, output));
			}
			FST<PairOutputs.Pair<long, long>> fst = builder.Finish();
			//System.out.println("SAVE out.dot");
			//Writer w = new OutputStreamWriter(new FileOutputStream("out.dot"));
			//Util.toDot(fst, w, false, false);
			//w.close();
			FST.BytesReader reader = fst.GetBytesReader();
			//System.out.println("testing: " + allPrefixes.size() + " prefixes");
			foreach (string prefix in allPrefixes)
			{
				// 1. run prefix against fst, then complete by value
				//System.out.println("TEST: " + prefix);
				PairOutputs.Pair<long, long> prefixOutput = outputs.GetNoOutput();
				FST.Arc<PairOutputs.Pair<long, long>> arc = fst.GetFirstArc(new FST.Arc<PairOutputs.Pair
					<long, long>>());
				for (int idx = 0; idx < prefix.Length; idx++)
				{
					if (fst.FindTargetArc((int)prefix[idx], arc, arc, reader) == null)
					{
						Fail();
					}
					prefixOutput = outputs.Add(prefixOutput, arc.output);
				}
				int topN = TestUtil.NextInt(random, 1, 10);
				Util.TopResults<PairOutputs.Pair<long, long>> r = Lucene.Net.Util.Fst.Util
					.ShortestPaths(fst, arc, fst.outputs.GetNoOutput(), minPairWeightComparator, topN
					, true);
				IsTrue(r.isComplete);
				// 2. go thru whole treemap (slowCompletor) and check its actually the best suggestion
				IList<Util.Result<PairOutputs.Pair<long, long>>> matches = new AList<Util.Result<
					PairOutputs.Pair<long, long>>>();
				// TODO: could be faster... but its slowCompletor for a reason
				foreach (KeyValuePair<string, TestFSTs.TwoLongs> e_1 in slowCompletor.EntrySet())
				{
					if (e_1.Key.StartsWith(prefix))
					{
						//System.out.println("  consider " + e.getKey());
						matches.AddItem(new Util.Result<PairOutputs.Pair<long, long>>(Lucene.Net.Util.Fst.Util
							.ToIntsRef(new BytesRef(Sharpen.Runtime.Substring(e_1.Key, prefix.Length)), new 
							IntsRef()), outputs.NewPair(e_1.Value.a - prefixOutput.output1, e_1.Value.b - prefixOutput
							.output2)));
					}
				}
				IsTrue(matches.Count > 0);
				matches.Sort(new TestFSTs.TieBreakByInputComparator<PairOutputs.Pair<long, long>>
					(minPairWeightComparator));
				if (matches.Count > topN)
				{
					matches.SubList(topN, matches.Count).Clear();
				}
				AreEqual(matches.Count, r.topN.Count);
				for (int hit = 0; hit < r.topN.Count; hit++)
				{
					//System.out.println("  check hit " + hit);
					AreEqual(matches[hit].input, r.topN[hit].input);
					AreEqual(matches[hit].output, r.topN[hit].output);
				}
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestLargeOutputsOnArrayArcs()
		{
			ByteSequenceOutputs outputs = ByteSequenceOutputs.GetSingleton();
			Builder<BytesRef> builder = new Builder<BytesRef>(FST.INPUT_TYPE.BYTE1, outputs);
			byte[] bytes = new byte[300];
			IntsRef input = new IntsRef();
			input.Grow(1);
			input.length = 1;
			BytesRef output = new BytesRef(bytes);
			for (int arc = 0; arc < 6; arc++)
			{
				input.ints[0] = arc;
				output.bytes[0] = unchecked((byte)arc);
				builder.Add(input, BytesRef.DeepCopyOf(output));
			}
			FST<BytesRef> fst = builder.Finish();
			for (int arc_1 = 0; arc_1 < 6; arc_1++)
			{
				input.ints[0] = arc_1;
				BytesRef result = Lucene.Net.Util.Fst.Util.Get(fst, input);
				IsNotNull(result);
				AreEqual(300, result.length);
				AreEqual(result.bytes[result.offset], arc_1);
				for (int byteIDX = 1; byteIDX < result.length; byteIDX++)
				{
					AreEqual(0, result.bytes[result.offset + byteIDX]);
				}
			}
		}
	}
}
