/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.TestFramework.Store;
using Lucene.Net.TestFramework.Util;
using Lucene.Net.TestFramework.Util.Fst;
using Lucene.Net.TestFramework.Util.Packed;
using Sharpen;

namespace Lucene.Net.TestFramework.Util.Fst
{
	/// <summary>Helper class to test FSTs.</summary>
	/// <remarks>Helper class to test FSTs.</remarks>
	public class FSTTester<T>
	{
		internal readonly Random random;

		internal readonly IList<FSTTester.InputOutput<T>> pairs;

		internal readonly int inputMode;

		internal readonly Outputs<T> outputs;

		internal readonly Directory dir;

		internal readonly bool doReverseLookup;

		public FSTTester(Random random, Directory dir, int inputMode, IList<FSTTester.InputOutput
			<T>> pairs, Outputs<T> outputs, bool doReverseLookup)
		{
			this.random = random;
			this.dir = dir;
			this.inputMode = inputMode;
			this.pairs = pairs;
			this.outputs = outputs;
			this.doReverseLookup = doReverseLookup;
		}

		internal static string InputToString(int inputMode, IntsRef term)
		{
			return InputToString(inputMode, term, true);
		}

		internal static string InputToString(int inputMode, IntsRef term, bool isValidUnicode
			)
		{
			if (!isValidUnicode)
			{
				return term.ToString();
			}
			else
			{
				if (inputMode == 0)
				{
					// utf8
					return ToBytesRef(term).Utf8ToString() + " " + term;
				}
				else
				{
					// utf32
					return UnicodeUtil.NewString(term.ints, term.offset, term.length) + " " + term;
				}
			}
		}

		private static BytesRef ToBytesRef(IntsRef ir)
		{
			BytesRef br = new BytesRef(ir.length);
			for (int i = 0; i < ir.length; i++)
			{
				int x = ir.ints[ir.offset + i];
				 
				//assert x >= 0 && x <= 255;
				br.bytes[i] = unchecked((byte)x);
			}
			br.length = ir.length;
			return br;
		}

		internal static string GetRandomString(Random random)
		{
			string term;
			if (random.NextBoolean())
			{
				term = TestUtil.RandomRealisticUnicodeString(random);
			}
			else
			{
				// we want to mix in limited-alphabet symbols so
				// we get more sharing of the nodes given how few
				// terms we are testing...
				term = SimpleRandomString(random);
			}
			return term;
		}

		internal static string SimpleRandomString(Random r)
		{
			int end = r.Next(10);
			if (end == 0)
			{
				// allow 0 length
				return string.Empty;
			}
			char[] buffer = new char[end];
			for (int i = 0; i < end; i++)
			{
				buffer[i] = (char)TestUtil.NextInt(r, 97, 102);
			}
			return new string(buffer, 0, end);
		}

		internal static IntsRef ToIntsRef(string s, int inputMode)
		{
			return ToIntsRef(s, inputMode, new IntsRef(10));
		}

		internal static IntsRef ToIntsRef(string s, int inputMode, IntsRef ir)
		{
			if (inputMode == 0)
			{
				// utf8
				return ToIntsRef(new BytesRef(s), ir);
			}
			else
			{
				// utf32
				return ToIntsRefUTF32(s, ir);
			}
		}

		internal static IntsRef ToIntsRefUTF32(string s, IntsRef ir)
		{
			int charLength = s.Length;
			int charIdx = 0;
			int intIdx = 0;
			while (charIdx < charLength)
			{
				if (intIdx == ir.ints.Length)
				{
					ir.Grow(intIdx + 1);
				}
				int utf32 = s.CodePointAt(charIdx);
				ir.ints[intIdx] = utf32;
				charIdx += char.CharCount(utf32);
				intIdx++;
			}
			ir.length = intIdx;
			return ir;
		}

		internal static IntsRef ToIntsRef(BytesRef br, IntsRef ir)
		{
			if (br.length > ir.ints.Length)
			{
				ir.Grow(br.length);
			}
			for (int i = 0; i < br.length; i++)
			{
				ir.ints[i] = br.bytes[br.offset + i] & unchecked((int)(0xFF));
			}
			ir.length = br.length;
			return ir;
		}

		/// <summary>Holds one input/output pair.</summary>
		/// <remarks>Holds one input/output pair.</remarks>
		public class InputOutput<T> : Comparable<FSTTester.InputOutput<T>>
		{
			public readonly IntsRef input;

			public readonly T output;

			public InputOutput(IntsRef input, T output)
			{
				this.input = input;
				this.output = output;
			}

			public virtual int CompareTo(FSTTester.InputOutput<T> other)
			{
				if (other is FSTTester.InputOutput)
				{
					return input.CompareTo((other).input);
				}
				else
				{
					throw new ArgumentException();
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void DoTest(bool testPruning)
		{
			// no pruning
			DoTest(0, 0, true);
			if (testPruning)
			{
				// simple pruning
				DoTest(TestUtil.NextInt(random, 1, 1 + pairs.Count), 0, true);
				// leafy pruning
				DoTest(0, TestUtil.NextInt(random, 1, 1 + pairs.Count), true);
			}
		}

		// runs the term, returning the output, or null if term
		// isn't accepted.  if prefixLength is non-null it must be
		// length 1 int array; prefixLength[0] is set to the length
		// of the term prefix that matches
		/// <exception cref="System.IO.IOException"></exception>
		private T Run(FST<T> fst, IntsRef term, int[] prefixLength)
		{
			 
			//assert prefixLength == null || prefixLength.length == 1;
			FST.Arc<T> arc = fst.GetFirstArc(new FST.Arc<T>());
			T NO_OUTPUT = fst.outputs.GetNoOutput();
			T output = NO_OUTPUT;
			FST.BytesReader fstReader = fst.GetBytesReader();
			for (int i = 0; i <= term.length; i++)
			{
				int label;
				if (i == term.length)
				{
					label = FST.END_LABEL;
				}
				else
				{
					label = term.ints[term.offset + i];
				}
				// System.out.println("   loop i=" + i + " label=" + label + " output=" + fst.outputs.outputToString(output) + " curArc: target=" + arc.target + " isFinal?=" + arc.isFinal());
				if (fst.FindTargetArc(label, arc, arc, fstReader) == null)
				{
					// System.out.println("    not found");
					if (prefixLength != null)
					{
						prefixLength[0] = i;
						return output;
					}
					else
					{
						return null;
					}
				}
				output = fst.outputs.Add(output, arc.output);
			}
			if (prefixLength != null)
			{
				prefixLength[0] = term.length;
			}
			return output;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private T RandomAcceptedWord(FST<T> fst, IntsRef @in)
		{
			FST.Arc<T> arc = fst.GetFirstArc(new FST.Arc<T>());
			IList<FST.Arc<T>> arcs = new AList<FST.Arc<T>>();
			@in.length = 0;
			@in.offset = 0;
			T NO_OUTPUT = fst.outputs.GetNoOutput();
			T output = NO_OUTPUT;
			FST.BytesReader fstReader = fst.GetBytesReader();
			while (true)
			{
				// read all arcs:
				fst.ReadFirstTargetArc(arc, arc, fstReader);
				arcs.AddItem(new FST.Arc<T>().CopyFrom(arc));
				while (!arc.IsLast())
				{
					fst.ReadNextArc(arc, fstReader);
					arcs.AddItem(new FST.Arc<T>().CopyFrom(arc));
				}
				// pick one
				arc = arcs[random.Next(arcs.Count)];
				arcs.Clear();
				// accumulate output
				output = fst.outputs.Add(output, arc.output);
				// append label
				if (arc.label == FST.END_LABEL)
				{
					break;
				}
				if (@in.ints.Length == @in.length)
				{
					@in.Grow(1 + @in.length);
				}
				@in.ints[@in.length++] = arc.label;
			}
			return output;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual FST<T> DoTest(int prune1, int prune2, bool allowRandomSuffixSharing
			)
		{
			if (LuceneTestCase.VERBOSE)
			{
				System.Console.Out.WriteLine("\nTEST: prune1=" + prune1 + " prune2=" + prune2);
			}
			bool willRewrite = random.NextBoolean();
			Builder<T> builder = new Builder<T>(inputMode == 0 ? FST.INPUT_TYPE.BYTE1 : FST.INPUT_TYPE
				.BYTE4, prune1, prune2, prune1 == 0 && prune2 == 0, allowRandomSuffixSharing ? random
				.NextBoolean() : true, allowRandomSuffixSharing ? TestUtil.NextInt(random, 1, 10
				) : int.MaxValue, outputs, null, willRewrite, PackedInts.DEFAULT, true, 15);
			if (LuceneTestCase.VERBOSE)
			{
				if (willRewrite)
				{
					System.Console.Out.WriteLine("TEST: packed FST");
				}
				else
				{
					System.Console.Out.WriteLine("TEST: non-packed FST");
				}
			}
			foreach (FSTTester.InputOutput<T> pair in pairs)
			{
				if (pair.output is IList)
				{
					IList<long> longValues = (IList<long>)pair.output;
					Builder<object> builderObject = (Builder<object>)builder;
					foreach (long value in longValues)
					{
						builderObject.Add(pair.input, value);
					}
				}
				else
				{
					builder.Add(pair.input, pair.output);
				}
			}
			FST<T> fst = builder.Finish();
			if (random.NextBoolean() && fst != null && !willRewrite)
			{
				IOContext context = LuceneTestCase.NewIOContext(random);
				IndexOutput @out = dir.CreateOutput("fst.bin", context);
				fst.Save(@out);
				@out.Close();
				IndexInput @in = dir.OpenInput("fst.bin", context);
				try
				{
					fst = new FST<T>(@in, outputs);
				}
				finally
				{
					@in.Close();
					dir.DeleteFile("fst.bin");
				}
			}
			if (LuceneTestCase.VERBOSE && pairs.Count <= 20 && fst != null)
			{
				TextWriter w = new OutputStreamWriter(new FileOutputStream("out.dot"), StandardCharsets
					.UTF_8);
				Lucene.Net.TestFramework.Util.Fst.Util.ToDot(fst, w, false, false);
				w.Close();
				System.Console.Out.WriteLine("SAVED out.dot");
			}
			if (LuceneTestCase.VERBOSE)
			{
				if (fst == null)
				{
					System.Console.Out.WriteLine("  fst has 0 nodes (fully pruned)");
				}
				else
				{
					System.Console.Out.WriteLine("  fst has " + fst.GetNodeCount() + " nodes and " + 
						fst.GetArcCount() + " arcs");
				}
			}
			if (prune1 == 0 && prune2 == 0)
			{
				VerifyUnPruned(inputMode, fst);
			}
			else
			{
				VerifyPruned(inputMode, fst, prune1, prune2);
			}
			return fst;
		}

		protected internal virtual bool OutputsEqual(T a, T b)
		{
			return a.Equals(b);
		}

		// FST is complete
		/// <exception cref="System.IO.IOException"></exception>
		private void VerifyUnPruned(int inputMode, FST<T> fst)
		{
			FST<long> fstLong;
			ICollection<long> validOutputs;
			long minLong = long.MaxValue;
			long maxLong = long.MinValue;
			if (doReverseLookup)
			{
				FST<long> fstLong0 = (FST<long>)fst;
				fstLong = fstLong0;
				validOutputs = new HashSet<long>();
				foreach (FSTTester.InputOutput<T> pair in pairs)
				{
					long output = (long)pair.output;
					maxLong = Math.Max(maxLong, output);
					minLong = Math.Min(minLong, output);
					validOutputs.AddItem(output);
				}
			}
			else
			{
				fstLong = null;
				validOutputs = null;
			}
			if (pairs.Count == 0)
			{
				NUnit.Framework.Assert.IsNull(fst);
				return;
			}
			if (LuceneTestCase.VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: now verify " + pairs.Count + " terms");
				foreach (FSTTester.InputOutput<T> pair in pairs)
				{
					NUnit.Framework.Assert.IsNotNull(pair);
					NUnit.Framework.Assert.IsNotNull(pair.input);
					NUnit.Framework.Assert.IsNotNull(pair.output);
					System.Console.Out.WriteLine("  " + InputToString(inputMode, pair.input) + ": " +
						 outputs.OutputToString(pair.output));
				}
			}
			NUnit.Framework.Assert.IsNotNull(fst);
			// visit valid pairs in order -- make sure all words
			// are accepted, and FSTEnum's next() steps through
			// them correctly
			if (LuceneTestCase.VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: check valid terms/next()");
			}
			{
				IntsRefFSTEnum<T> fstEnum = new IntsRefFSTEnum<T>(fst);
				foreach (FSTTester.InputOutput<T> pair in pairs)
				{
					IntsRef term = pair.input;
					if (LuceneTestCase.VERBOSE)
					{
						System.Console.Out.WriteLine("TEST: check term=" + InputToString(inputMode, term)
							 + " output=" + fst.outputs.OutputToString(pair.output));
					}
					T output = Run(fst, term, null);
					NUnit.Framework.Assert.IsNotNull("term " + InputToString(inputMode, term) + " is not accepted"
						, output);
					NUnit.Framework.Assert.IsTrue(OutputsEqual(pair.output, output));
					// verify enum's next
					IntsRefFSTEnum.InputOutput<T> t = fstEnum.Next();
					NUnit.Framework.Assert.IsNotNull(t);
					NUnit.Framework.Assert.AreEqual("expected input=" + InputToString(inputMode, term
						) + " but fstEnum returned " + InputToString(inputMode, t.input), term, t.input);
					NUnit.Framework.Assert.IsTrue(OutputsEqual(pair.output, t.output));
				}
				NUnit.Framework.Assert.IsNull(fstEnum.Next());
			}
			IDictionary<IntsRef, T> termsMap = new Dictionary<IntsRef, T>();
			foreach (FSTTester.InputOutput<T> pair_1 in pairs)
			{
				termsMap.Put(pair_1.input, pair_1.output);
			}
			if (doReverseLookup && maxLong > minLong)
			{
				// Do random lookups so we test null (output doesn't
				// exist) case:
				NUnit.Framework.Assert.IsNull(Lucene.Net.TestFramework.Util.Fst.Util.GetByOutput(fstLong
					, minLong - 7));
				NUnit.Framework.Assert.IsNull(Lucene.Net.TestFramework.Util.Fst.Util.GetByOutput(fstLong
					, maxLong + 7));
				int num = LuceneTestCase.AtLeast(random, 100);
				for (int iter = 0; iter < num; iter++)
				{
					long v = TestUtil.NextLong(random, minLong, maxLong);
					IntsRef input = Lucene.Net.TestFramework.Util.Fst.Util.GetByOutput(fstLong, v);
					NUnit.Framework.Assert.IsTrue(validOutputs.Contains(v) || input == null);
				}
			}
			// find random matching word and make sure it's valid
			if (LuceneTestCase.VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: verify random accepted terms");
			}
			IntsRef scratch = new IntsRef(10);
			int num_1 = LuceneTestCase.AtLeast(random, 500);
			for (int iter_1 = 0; iter_1 < num_1; iter_1++)
			{
				T output = RandomAcceptedWord(fst, scratch);
				NUnit.Framework.Assert.IsTrue("accepted word " + InputToString(inputMode, scratch
					) + " is not valid", termsMap.ContainsKey(scratch));
				NUnit.Framework.Assert.IsTrue(OutputsEqual(termsMap.Get(scratch), output));
				if (doReverseLookup)
				{
					//System.out.println("lookup output=" + output + " outs=" + fst.outputs);
					IntsRef input = Lucene.Net.TestFramework.Util.Fst.Util.GetByOutput(fstLong, (long)output
						);
					NUnit.Framework.Assert.IsNotNull(input);
					//System.out.println("  got " + Util.toBytesRef(input, new BytesRef()).utf8ToString());
					NUnit.Framework.Assert.AreEqual(scratch, input);
				}
			}
			// test IntsRefFSTEnum.seek:
			if (LuceneTestCase.VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: verify seek");
			}
			IntsRefFSTEnum<T> fstEnum_1 = new IntsRefFSTEnum<T>(fst);
			num_1 = LuceneTestCase.AtLeast(random, 100);
			for (int iter_2 = 0; iter_2 < num_1; iter_2++)
			{
				if (LuceneTestCase.VERBOSE)
				{
					System.Console.Out.WriteLine("  iter=" + iter_2);
				}
				if (random.NextBoolean())
				{
					// seek to term that doesn't exist:
					while (true)
					{
						IntsRef term = ToIntsRef(GetRandomString(random), inputMode);
						int pos = Sharpen.Collections.BinarySearch(pairs, new FSTTester.InputOutput<T>(term
							, null));
						if (pos < 0)
						{
							pos = -(pos + 1);
							// ok doesn't exist
							//System.out.println("  seek " + inputToString(inputMode, term));
							IntsRefFSTEnum.InputOutput<T> seekResult;
							if (random.Next(3) == 0)
							{
								if (LuceneTestCase.VERBOSE)
								{
									System.Console.Out.WriteLine("  do non-exist seekExact term=" + InputToString(inputMode
										, term));
								}
								seekResult = fstEnum_1.SeekExact(term);
								pos = -1;
							}
							else
							{
								if (random.NextBoolean())
								{
									if (LuceneTestCase.VERBOSE)
									{
										System.Console.Out.WriteLine("  do non-exist seekFloor term=" + InputToString(inputMode
											, term));
									}
									seekResult = fstEnum_1.SeekFloor(term);
									pos--;
								}
								else
								{
									if (LuceneTestCase.VERBOSE)
									{
										System.Console.Out.WriteLine("  do non-exist seekCeil term=" + InputToString(inputMode
											, term));
									}
									seekResult = fstEnum_1.SeekCeil(term);
								}
							}
							if (pos != -1 && pos < pairs.Count)
							{
								//System.out.println("    got " + inputToString(inputMode,seekResult.input) + " output=" + fst.outputs.outputToString(seekResult.output));
								NUnit.Framework.Assert.IsNotNull("got null but expected term=" + InputToString(inputMode
									, pairs[pos].input), seekResult);
								if (LuceneTestCase.VERBOSE)
								{
									System.Console.Out.WriteLine("    got " + InputToString(inputMode, seekResult.input
										));
								}
								NUnit.Framework.Assert.AreEqual("expected " + InputToString(inputMode, pairs[pos]
									.input) + " but got " + InputToString(inputMode, seekResult.input), pairs[pos].input
									, seekResult.input);
								NUnit.Framework.Assert.IsTrue(OutputsEqual(pairs[pos].output, seekResult.output));
							}
							else
							{
								// seeked before start or beyond end
								//System.out.println("seek=" + seekTerm);
								NUnit.Framework.Assert.IsNull("expected null but got " + (seekResult == null ? "null"
									 : InputToString(inputMode, seekResult.input)), seekResult);
								if (LuceneTestCase.VERBOSE)
								{
									System.Console.Out.WriteLine("    got null");
								}
							}
							break;
						}
					}
				}
				else
				{
					// seek to term that does exist:
					FSTTester.InputOutput<T> pair = pairs[random.Next(pairs.Count)];
					IntsRefFSTEnum.InputOutput<T> seekResult;
					if (random.Next(3) == 2)
					{
						if (LuceneTestCase.VERBOSE)
						{
							System.Console.Out.WriteLine("  do exists seekExact term=" + InputToString(inputMode
								, pair_1.input));
						}
						seekResult = fstEnum_1.SeekExact(pair_1.input);
					}
					else
					{
						if (random.NextBoolean())
						{
							if (LuceneTestCase.VERBOSE)
							{
								System.Console.Out.WriteLine("  do exists seekFloor " + InputToString(inputMode, 
									pair_1.input));
							}
							seekResult = fstEnum_1.SeekFloor(pair_1.input);
						}
						else
						{
							if (LuceneTestCase.VERBOSE)
							{
								System.Console.Out.WriteLine("  do exists seekCeil " + InputToString(inputMode, pair_1
									.input));
							}
							seekResult = fstEnum_1.SeekCeil(pair_1.input);
						}
					}
					NUnit.Framework.Assert.IsNotNull(seekResult);
					NUnit.Framework.Assert.AreEqual("got " + InputToString(inputMode, seekResult.input
						) + " but expected " + InputToString(inputMode, pair_1.input), pair_1.input, seekResult
						.input);
					NUnit.Framework.Assert.IsTrue(OutputsEqual(pair_1.output, seekResult.output));
				}
			}
			if (LuceneTestCase.VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: mixed next/seek");
			}
			// test mixed next/seek
			num_1 = LuceneTestCase.AtLeast(random, 100);
			for (int iter_3 = 0; iter_3 < num_1; iter_3++)
			{
				if (LuceneTestCase.VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: iter " + iter_3);
				}
				// reset:
				fstEnum_1 = new IntsRefFSTEnum<T>(fst);
				int upto = -1;
				while (true)
				{
					bool isDone = false;
					if (upto == pairs.Count - 1 || random.NextBoolean())
					{
						// next
						upto++;
						if (LuceneTestCase.VERBOSE)
						{
							System.Console.Out.WriteLine("  do next");
						}
						isDone = fstEnum_1.Next() == null;
					}
					else
					{
						if (upto != -1 && upto < 0.75 * pairs.Count && random.NextBoolean())
						{
							int attempt = 0;
							for (; attempt < 10; attempt++)
							{
								IntsRef term = ToIntsRef(GetRandomString(random), inputMode);
								if (!termsMap.ContainsKey(term) && term.CompareTo(pairs[upto].input) > 0)
								{
									int pos = Sharpen.Collections.BinarySearch(pairs, new FSTTester.InputOutput<T>(term
										, null));
									 
									//assert pos < 0;
									upto = -(pos + 1);
									if (random.NextBoolean())
									{
										upto--;
										NUnit.Framework.Assert.IsTrue(upto != -1);
										if (LuceneTestCase.VERBOSE)
										{
											System.Console.Out.WriteLine("  do non-exist seekFloor(" + InputToString(inputMode
												, term) + ")");
										}
										isDone = fstEnum_1.SeekFloor(term) == null;
									}
									else
									{
										if (LuceneTestCase.VERBOSE)
										{
											System.Console.Out.WriteLine("  do non-exist seekCeil(" + InputToString(inputMode
												, term) + ")");
										}
										isDone = fstEnum_1.SeekCeil(term) == null;
									}
									break;
								}
							}
							if (attempt == 10)
							{
								continue;
							}
						}
						else
						{
							int inc = random.Next(pairs.Count - upto - 1);
							upto += inc;
							if (upto == -1)
							{
								upto = 0;
							}
							if (random.NextBoolean())
							{
								if (LuceneTestCase.VERBOSE)
								{
									System.Console.Out.WriteLine("  do seekCeil(" + InputToString(inputMode, pairs[upto
										].input) + ")");
								}
								isDone = fstEnum_1.SeekCeil(pairs[upto].input) == null;
							}
							else
							{
								if (LuceneTestCase.VERBOSE)
								{
									System.Console.Out.WriteLine("  do seekFloor(" + InputToString(inputMode, pairs[upto
										].input) + ")");
								}
								isDone = fstEnum_1.SeekFloor(pairs[upto].input) == null;
							}
						}
					}
					if (LuceneTestCase.VERBOSE)
					{
						if (!isDone)
						{
							System.Console.Out.WriteLine("    got " + InputToString(inputMode, fstEnum_1.Current
								().input));
						}
						else
						{
							System.Console.Out.WriteLine("    got null");
						}
					}
					if (upto == pairs.Count)
					{
						NUnit.Framework.Assert.IsTrue(isDone);
						break;
					}
					else
					{
						NUnit.Framework.Assert.IsFalse(isDone);
						NUnit.Framework.Assert.AreEqual(pairs[upto].input, fstEnum_1.Current().input);
						NUnit.Framework.Assert.IsTrue(OutputsEqual(pairs[upto].output, fstEnum_1.Current(
							).output));
					}
				}
			}
		}

		private class CountMinOutput<T>
		{
			internal int count;

			internal T output;

			internal T finalOutput;

			internal bool isLeaf = true;

			internal bool isFinal;
		}

		// FST is pruned
		/// <exception cref="System.IO.IOException"></exception>
		private void VerifyPruned(int inputMode, FST<T> fst, int prune1, int prune2)
		{
			if (LuceneTestCase.VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: now verify pruned " + pairs.Count + " terms; outputs="
					 + outputs);
				foreach (FSTTester.InputOutput<T> pair in pairs)
				{
					System.Console.Out.WriteLine("  " + InputToString(inputMode, pair.input) + ": " +
						 outputs.OutputToString(pair.output));
				}
			}
			// To validate the FST, we brute-force compute all prefixes
			// in the terms, matched to their "common" outputs, prune that
			// set according to the prune thresholds, then 
			 
			//assert the FST
			// matches that same set.
			// NOTE: Crazy RAM intensive!!
			//System.out.println("TEST: tally prefixes");
			// build all prefixes
			IDictionary<IntsRef, FSTTester.CountMinOutput<T>> prefixes = new Dictionary<IntsRef
				, FSTTester.CountMinOutput<T>>();
			IntsRef scratch = new IntsRef(10);
			foreach (FSTTester.InputOutput<T> pair_1 in pairs)
			{
				scratch.CopyInts(pair_1.input);
				for (int idx = 0; idx <= pair_1.input.length; idx++)
				{
					scratch.length = idx;
					FSTTester.CountMinOutput<T> cmo = prefixes.Get(scratch);
					if (cmo == null)
					{
						cmo = new FSTTester.CountMinOutput<T>();
						cmo.count = 1;
						cmo.output = pair_1.output;
						prefixes.Put(IntsRef.DeepCopyOf(scratch), cmo);
					}
					else
					{
						cmo.count++;
						T output1 = cmo.output;
						if (output1.Equals(outputs.GetNoOutput()))
						{
							output1 = outputs.GetNoOutput();
						}
						T output2 = pair_1.output;
						if (output2.Equals(outputs.GetNoOutput()))
						{
							output2 = outputs.GetNoOutput();
						}
						cmo.output = outputs.Common(output1, output2);
					}
					if (idx == pair_1.input.length)
					{
						cmo.isFinal = true;
						cmo.finalOutput = cmo.output;
					}
				}
			}
			if (LuceneTestCase.VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: now prune");
			}
			// prune 'em
			Iterator<KeyValuePair<IntsRef, FSTTester.CountMinOutput<T>>> it = prefixes.EntrySet
				().Iterator();
			while (it.HasNext())
			{
				KeyValuePair<IntsRef, FSTTester.CountMinOutput<T>> ent = it.Next();
				IntsRef prefix = ent.Key;
				FSTTester.CountMinOutput<T> cmo = ent.Value;
				if (LuceneTestCase.VERBOSE)
				{
					System.Console.Out.WriteLine("  term prefix=" + InputToString(inputMode, prefix, 
						false) + " count=" + cmo.count + " isLeaf=" + cmo.isLeaf + " output=" + outputs.
						OutputToString(cmo.output) + " isFinal=" + cmo.isFinal);
				}
				bool keep;
				if (prune1 > 0)
				{
					keep = cmo.count >= prune1;
				}
				else
				{
					 
					//assert prune2 > 0;
					if (prune2 > 1 && cmo.count >= prune2)
					{
						keep = true;
					}
					else
					{
						if (prefix.length > 0)
						{
							// consult our parent
							scratch.length = prefix.length - 1;
							System.Array.Copy(prefix.ints, prefix.offset, scratch.ints, 0, scratch.length);
							FSTTester.CountMinOutput<T> cmo2 = prefixes.Get(scratch);
							//System.out.println("    parent count = " + (cmo2 == null ? -1 : cmo2.count));
							keep = cmo2 != null && ((prune2 > 1 && cmo2.count >= prune2) || (prune2 == 1 && (
								cmo2.count >= 2 || prefix.length <= 1)));
						}
						else
						{
							if (cmo.count >= prune2)
							{
								keep = true;
							}
							else
							{
								keep = false;
							}
						}
					}
				}
				if (!keep)
				{
					it.Remove();
				}
				else
				{
					//System.out.println("    remove");
					// clear isLeaf for all ancestors
					//System.out.println("    keep");
					scratch.CopyInts(prefix);
					scratch.length--;
					while (scratch.length >= 0)
					{
						FSTTester.CountMinOutput<T> cmo2 = prefixes.Get(scratch);
						if (cmo2 != null)
						{
							//System.out.println("    clear isLeaf " + inputToString(inputMode, scratch));
							cmo2.isLeaf = false;
						}
						scratch.length--;
					}
				}
			}
			if (LuceneTestCase.VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: after prune");
				foreach (KeyValuePair<IntsRef, FSTTester.CountMinOutput<T>> ent in prefixes.EntrySet
					())
				{
					System.Console.Out.WriteLine("  " + InputToString(inputMode, ent.Key, false) + ": isLeaf="
						 + ent.Value.isLeaf + " isFinal=" + ent.Value.isFinal);
					if (ent.Value.isFinal)
					{
						System.Console.Out.WriteLine("    finalOutput=" + outputs.OutputToString(ent.Value
							.finalOutput));
					}
				}
			}
			if (prefixes.Count <= 1)
			{
				NUnit.Framework.Assert.IsNull(fst);
				return;
			}
			NUnit.Framework.Assert.IsNotNull(fst);
			// make sure FST only enums valid prefixes
			if (LuceneTestCase.VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: check pruned enum");
			}
			IntsRefFSTEnum<T> fstEnum = new IntsRefFSTEnum<T>(fst);
			IntsRefFSTEnum.InputOutput<T> current;
			while ((current = fstEnum.Next()) != null)
			{
				if (LuceneTestCase.VERBOSE)
				{
					System.Console.Out.WriteLine("  fstEnum.next prefix=" + InputToString(inputMode, 
						current.input, false) + " output=" + outputs.OutputToString(current.output));
				}
				FSTTester.CountMinOutput<T> cmo = prefixes.Get(current.input);
				NUnit.Framework.Assert.IsNotNull(cmo);
				NUnit.Framework.Assert.IsTrue(cmo.isLeaf || cmo.isFinal);
				//if (cmo.isFinal && !cmo.isLeaf) {
				if (cmo.isFinal)
				{
					NUnit.Framework.Assert.AreEqual(cmo.finalOutput, current.output);
				}
				else
				{
					NUnit.Framework.Assert.AreEqual(cmo.output, current.output);
				}
			}
			// make sure all non-pruned prefixes are present in the FST
			if (LuceneTestCase.VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: verify all prefixes");
			}
			int[] stopNode = new int[1];
			foreach (KeyValuePair<IntsRef, FSTTester.CountMinOutput<T>> ent_1 in prefixes.EntrySet
				())
			{
				if (ent_1.Key.length > 0)
				{
					FSTTester.CountMinOutput<T> cmo = ent_1.Value;
					T output = Run(fst, ent_1.Key, stopNode);
					if (LuceneTestCase.VERBOSE)
					{
						System.Console.Out.WriteLine("TEST: verify prefix=" + InputToString(inputMode, ent_1
							.Key, false) + " output=" + outputs.OutputToString(cmo.output));
					}
					// if (cmo.isFinal && !cmo.isLeaf) {
					if (cmo.isFinal)
					{
						NUnit.Framework.Assert.AreEqual(cmo.finalOutput, output);
					}
					else
					{
						NUnit.Framework.Assert.AreEqual(cmo.output, output);
					}
					NUnit.Framework.Assert.AreEqual(ent_1.Key.length, stopNode[0]);
				}
			}
		}
	}
}
