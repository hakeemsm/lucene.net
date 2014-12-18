/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using Lucene.Net.Analysis;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.TestFramework.Util;
using Lucene.Net.Util;
using NUnit.Framework;
using Lucene.Net.Analysis.Tokenattributes;
using System.Collections.Generic;
using Lucene.Net.Support;
using Lucene.Net.Store;
using System.Threading;
using Attribute = Lucene.Net.Util.Attribute;

namespace Lucene.Net.TestFramework.Analysis
{

    /// <summary>Base class for all Lucene unit tests that use TokenStreams.</summary>
    public abstract class BaseTokenStreamTestCase : LuceneTestCase
    {
        public BaseTokenStreamTestCase()
        { }

        public BaseTokenStreamTestCase(string name)
            : base(name)
        { }

        // some helpers to test Analyzers and TokenStreams:
        public interface ICheckClearAttributesAttribute : IAttribute
        {
            bool GetAndResetClearCalled();
        }

        public class CheckClearAttributesAttribute : Attribute, ICheckClearAttributesAttribute
        {
            private bool clearCalled = false;

            public bool GetAndResetClearCalled()
            {
                try
                {
                    return clearCalled;
                }
                finally
                {
                    clearCalled = false;
                }
            }

            public override void Clear()
            {
                clearCalled = true;
            }

            public override bool Equals(Object other)
            {
                return (
                other is CheckClearAttributesAttribute &&
                ((CheckClearAttributesAttribute)other).clearCalled == this.clearCalled
                );
            }

            public override int GetHashCode()
            {
                //Java: return 76137213 ^ Boolean.valueOf(clearCalled).hashCode();
                return 76137213 ^ clearCalled.GetHashCode();
            }

            public override void CopyTo(Attribute target)
            {
                target.Clear();
            }
        }

		public static void AssertTokenStreamContents(TokenStream ts, string[] output, int
			[] startOffsets, int[] endOffsets, string[] types, int[] posIncrements, int[] posLengths
			, int? finalOffset, int? finalPosInc, bool[] keywordAtts, bool offsetsAreCorrect)
        {
            assertNotNull(output);
            ICheckClearAttributesAttribute checkClearAtt = ts.AddAttribute<ICheckClearAttributesAttribute>();

            ICharTermAttribute termAtt = null;
            if (output.Length > 0)
            {
                assertTrue("has no CharTermAttribute", ts.HasAttribute<ICharTermAttribute>());
                termAtt = ts.GetAttribute<ICharTermAttribute>();
            }

            IOffsetAttribute offsetAtt = null;
            if (startOffsets != null || endOffsets != null || finalOffset != null)
            {
                Assert.IsTrue(ts.HasAttribute<IOffsetAttribute>(), "has no OffsetAttribute");
                offsetAtt = ts.GetAttribute<IOffsetAttribute>();
            }

            ITypeAttribute typeAtt = null;
            if (types != null)
            {
                Assert.IsTrue(ts.HasAttribute<ITypeAttribute>(), "has no TypeAttribute");
                typeAtt = ts.GetAttribute<ITypeAttribute>();
            }

            IPositionIncrementAttribute posIncrAtt = null;
			if (posIncrements != null || finalPosInc != null)
            {
                Assert.IsTrue(ts.HasAttribute<IPositionIncrementAttribute>(), "has no PositionIncrementAttribute");
                posIncrAtt = ts.GetAttribute<IPositionIncrementAttribute>();
            }

            IPositionLengthAttribute posLengthAtt = null;
            if (posLengths != null)
            {
                assertTrue("has no PositionLengthAttribute", ts.HasAttribute<IPositionLengthAttribute>());
                posLengthAtt = ts.GetAttribute<IPositionLengthAttribute>();
            }
			KeywordAttribute keywordAtt = null;
			if (keywordAtts != null)
			{
				assertTrue("has no KeywordAttribute", ts.HasAttribute(typeof(KeywordAttribute)));
				keywordAtt = ts.GetAttribute<KeywordAttribute>();
			}
            // Maps position to the start/end offset:
            IDictionary<int, int> posToStartOffset = new HashMap<int, int>();
            IDictionary<int, int> posToEndOffset = new HashMap<int, int>();

            ts.Reset();
            int pos = -1;
            int lastStartOffset = 0;
            for (int i = 0; i < output.Length; i++)
            {
                // extra safety to enforce, that the state is not preserved and also assign bogus values
                ts.ClearAttributes();
                termAtt.SetEmpty().Append("bogusTerm");
                if (offsetAtt != null) offsetAtt.SetOffset(14584724, 24683243);
                if (typeAtt != null) typeAtt.Type = "bogusType";
                if (posIncrAtt != null) posIncrAtt.PositionIncrement = 45987657;
                if (posLengthAtt != null) posLengthAtt.PositionLength = 45987653;

                checkClearAtt.GetAndResetClearCalled(); // reset it, because we called clearAttribute() before
                Assert.IsTrue(ts.IncrementToken(), "token " + i + " does not exist");
                Assert.IsTrue(checkClearAtt.GetAndResetClearCalled(), "clearAttributes() was not called correctly in TokenStream chain");

                Assert.AreEqual(output[i], termAtt.ToString(), "term " + i);
                if (startOffsets != null)
                    Assert.AreEqual(startOffsets[i], offsetAtt.StartOffset, "startOffset " + i);
                if (endOffsets != null)
                    Assert.AreEqual(endOffsets[i], offsetAtt.EndOffset, "endOffset " + i);
                if (types != null)
                    Assert.AreEqual(types[i], typeAtt.Type, "type " + i);
                if (posIncrements != null)
                    Assert.AreEqual(posIncrements[i], posIncrAtt.PositionIncrement, "posIncrement " + i);
                if (posLengths != null)
                    assertEquals("posLength " + i, posLengths[i], posLengthAtt.PositionLength);

				if (keywordAtts != null)
				{
					assertEquals("keywordAtt " + i, keywordAtts[i], keywordAtt.IsKeyword);
				}
                // we can enforce some basic things about a few attributes even if the caller doesn't check:
                if (offsetAtt != null)
                {
                    int startOffset = offsetAtt.StartOffset;
                    int endOffset = offsetAtt.EndOffset;
                    if (finalOffset != null)
                    {
                        assertTrue("startOffset must be <= finalOffset", startOffset <= finalOffset);
                        assertTrue("endOffset must be <= finalOffset: got endOffset=" + endOffset + " vs finalOffset=" + finalOffset.GetValueOrDefault(),
                                   endOffset <= finalOffset);
                    }

                    if (offsetsAreCorrect)
                    {
                        assertTrue("offsets must not go backwards startOffset=" + startOffset + " is < lastStartOffset=" + lastStartOffset, offsetAtt.StartOffset >= lastStartOffset);
                        lastStartOffset = offsetAtt.StartOffset;
                    }

                    if (offsetsAreCorrect && posLengthAtt != null && posIncrAtt != null)
                    {
                        // Validate offset consistency in the graph, ie
                        // all tokens leaving from a certain pos have the
                        // same startOffset, and all tokens arriving to a
                        // certain pos have the same endOffset:
                        int posInc = posIncrAtt.PositionIncrement;
                        pos += posInc;

                        int posLength = posLengthAtt.PositionLength;

                        if (!posToStartOffset.ContainsKey(pos))
                        {
                            // First time we've seen a token leaving from this position:
                            posToStartOffset[pos] = startOffset;
                            //System.out.println("  + s " + pos + " -> " + startOffset);
                        }
                        else
                        {
                            // We've seen a token leaving from this position
                            // before; verify the startOffset is the same:
                            //System.out.println("  + vs " + pos + " -> " + startOffset);
                            assertEquals("pos=" + pos + " posLen=" + posLength + " token=" + termAtt, posToStartOffset[pos], startOffset);
                        }

                        int endPos = pos + posLength;

                        if (!posToEndOffset.ContainsKey(endPos))
                        {
                            // First time we've seen a token arriving to this position:
                            posToEndOffset[endPos] = endOffset;
                            //System.out.println("  + e " + endPos + " -> " + endOffset);
                        }
                        else
                        {
                            // We've seen a token arriving to this position
                            // before; verify the endOffset is the same:
                            //System.out.println("  + ve " + endPos + " -> " + endOffset);
                            assertEquals("pos=" + pos + " posLen=" + posLength + " token=" + termAtt, posToEndOffset[endPos], endOffset);
                        }
                    }
                }
                if (posIncrAtt != null)
                {
                    if (i == 0)
                    {
                        assertTrue("first posIncrement must be >= 1", posIncrAtt.PositionIncrement >= 1);
                    }
                    else
                    {
                        assertTrue("posIncrement must be >= 0", posIncrAtt.PositionIncrement >= 0);
                    }
                }
                if (posLengthAtt != null)
                {
                    assertTrue("posLength must be >= 1", posLengthAtt.PositionLength >= 1);
                }
            }
			if (ts.IncrementToken())
			{
				NUnit.Framework.Assert.Fail("TokenStream has more tokens than expected (expected count="
					 + output.Length + "); extra token=" + termAtt.ToString());
			}
			// repeat our extra safety checks for end()
			ts.ClearAttributes();
			if (termAtt != null)
			{
				termAtt.SetEmpty().Append("bogusTerm");
			}
			if (offsetAtt != null)
			{
				offsetAtt.SetOffset(14584724, 24683243);
			}
			if (typeAtt != null)
			{
				typeAtt.Type = "bogusType";
			}
			if (posIncrAtt != null)
			{
				posIncrAtt.PositionIncrement = 45987657;
			}
			if (posLengthAtt != null)
			{
				posLengthAtt.PositionLength = 45987653;
			}
			checkClearAtt.GetAndResetClearCalled();
            ts.End();
			assertTrue("super.end()/clearAttributes() was not called correctly in end()", checkClearAtt.GetAndResetClearCalled());
            if (finalOffset.HasValue)
                Assert.AreEqual(finalOffset, offsetAtt.EndOffset, "finalOffset ");
            if (offsetAtt != null)
            {
                assertTrue("finalOffset must be >= 0", offsetAtt.EndOffset >= 0);
            }
			if (finalPosInc != null)
			{
				assertEquals("finalPosInc", finalPosInc, posIncrAtt.PositionIncrement);
			}
            ts.Dispose();
        }

		public static void AssertTokenStreamContents(TokenStream ts, string[] output, int
			[] startOffsets, int[] endOffsets, string[] types, int[] posIncrements, int[] posLengths
			, int? finalOffset, bool[] keywordAtts, bool offsetsAreCorrect)
        {
			AssertTokenStreamContents(ts, output, startOffsets, endOffsets, types, posIncrements
				, posLengths, finalOffset, null, null, offsetsAreCorrect);
        }

		public static void AssertTokenStreamContents(TokenStream ts, string[] output, int
			[] startOffsets, int[] endOffsets, string[] types, int[] posIncrements, int[] posLengths
			, int? finalOffset, bool offsetsAreCorrect)
		{
			AssertTokenStreamContents(ts, output, startOffsets, endOffsets, types, posIncrements
				, posLengths, finalOffset, null, offsetsAreCorrect);
        }

		public static void AssertTokenStreamContents(TokenStream ts, string[] output, int
			[] startOffsets, int[] endOffsets, string[] types, int[] posIncrements, int[] posLengths
			, int? finalOffset)
		{
			AssertTokenStreamContents(ts, output, startOffsets, endOffsets, types, posIncrements
				, posLengths, finalOffset, true);
        }

		public static void AssertTokenStreamContents(TokenStream ts, string[] output, int
			[] startOffsets, int[] endOffsets, string[] types, int[] posIncrements, int finalOffset
			)
		{
			AssertTokenStreamContents(ts, output, startOffsets, endOffsets, types, posIncrements
				, null, finalOffset);
		}
		public static void AssertTokenStreamContents(TokenStream ts, string[] output, int
			[] startOffsets, int[] endOffsets, string[] types, int[] posIncrements)
		{
			AssertTokenStreamContents(ts, output, startOffsets, endOffsets, types, posIncrements
				, null, null);
		}
        public static void AssertTokenStreamContents(TokenStream ts, String[] output)
        {
            AssertTokenStreamContents(ts, output, null, null, null, null, null, null);
        }

        public static void AssertTokenStreamContents(TokenStream ts, String[] output, String[] types)
        {
            AssertTokenStreamContents(ts, output, null, null, types, null, null, null);
        }

        public static void AssertTokenStreamContents(TokenStream ts, String[] output, int[] posIncrements)
        {
            AssertTokenStreamContents(ts, output, null, null, null, posIncrements, null, null);
        }

        public static void AssertTokenStreamContents(TokenStream ts, String[] output, int[] startOffsets, int[] endOffsets)
        {
            AssertTokenStreamContents(ts, output, startOffsets, endOffsets, null, null, null, null);
        }

        public static void AssertTokenStreamContents(TokenStream ts, String[] output, int[] startOffsets, int[] endOffsets, int? finalOffset)
        {
            AssertTokenStreamContents(ts, output, startOffsets, endOffsets, null, null, null, finalOffset);
        }

        public static void AssertTokenStreamContents(TokenStream ts, String[] output, int[] startOffsets, int[] endOffsets, int[] posIncrements)
        {
            AssertTokenStreamContents(ts, output, startOffsets, endOffsets, null, posIncrements, null, null);
        }

        public static void AssertTokenStreamContents(TokenStream ts, String[] output, int[] startOffsets, int[] endOffsets, int[] posIncrements, int? finalOffset)
        {
            AssertTokenStreamContents(ts, output, startOffsets, endOffsets, null, posIncrements, null, finalOffset);
        }

        public static void AssertTokenStreamContents(TokenStream ts, String[] output, int[] startOffsets, int[] endOffsets, int[] posIncrements, int[] posLengths, int? finalOffset)
        {
            AssertTokenStreamContents(ts, output, startOffsets, endOffsets, null, posIncrements, posLengths, finalOffset);
        }

        public static void AssertAnalyzesTo(Analyzer a, String input, String[] output, int[] startOffsets, int[] endOffsets, string[] types, int[] posIncrements)
        {
			CheckResetException(a, input);
            AssertTokenStreamContents(a.TokenStream("dummy", new System.IO.StringReader(input)), output, startOffsets, endOffsets, types, posIncrements, null, input.Length);
        }

        public static void AssertAnalyzesTo(Analyzer a, String input, String[] output, int[] startOffsets, int[] endOffsets, string[] types, int[] posIncrements, int[] posLengths)
        {
			CheckResetException(a, input);
            AssertTokenStreamContents(a.TokenStream("dummy", new System.IO.StringReader(input)), output, startOffsets, endOffsets, types, posIncrements, posLengths, input.Length);
        }

        public static void AssertAnalyzesTo(Analyzer a, String input, String[] output, int[] startOffsets, int[] endOffsets, string[] types, int[] posIncrements, int[] posLengths, bool offsetsAreCorrect)
        {
			CheckResetException(a, input);
            AssertTokenStreamContents(a.TokenStream("dummy", new System.IO.StringReader(input)), output, startOffsets, endOffsets, types, posIncrements, posLengths, input.Length, offsetsAreCorrect);
        }

        public static void AssertAnalyzesTo(Analyzer a, String input, String[] output)
        {
            AssertAnalyzesTo(a, input, output, null, null, null, null, null);
        }

        public static void AssertAnalyzesTo(Analyzer a, String input, String[] output, String[] types)
        {
            AssertAnalyzesTo(a, input, output, null, null, types, null, null);
        }

        public static void AssertAnalyzesTo(Analyzer a, String input, String[] output, int[] posIncrements)
        {
            AssertAnalyzesTo(a, input, output, null, null, null, posIncrements, null);
        }

        public static void AssertAnalyzesToPositions(Analyzer a, String input, String[] output, int[] posIncrements, int[] posLengths)
        {
            AssertAnalyzesTo(a, input, output, null, null, null, posIncrements, posLengths);
        }

        public static void AssertAnalyzesTo(Analyzer a, String input, String[] output, int[] startOffsets, int[] endOffsets)
        {
            AssertAnalyzesTo(a, input, output, startOffsets, endOffsets, null, null, null);
        }

        public static void AssertAnalyzesTo(Analyzer a, String input, String[] output, int[] startOffsets, int[] endOffsets, int[] posIncrements)
        {
            AssertAnalyzesTo(a, input, output, startOffsets, endOffsets, null, posIncrements, null);
        }



        // simple utility method for testing stemmers

		internal static void CheckResetException(Analyzer a, string input)
		{
			TokenStream ts = a.TokenStream("bogus", input);
			try
			{
				if (ts.IncrementToken())
				{
					//System.out.println(ts.reflectAsString(false));
					NUnit.Framework.Assert.Fail("didn't get expected exception when reset() not called"
						);
				}
			}
			catch (InvalidOperationException)
			{
			}
			catch (Exception expected)
			{
				// ok
				// ok: MockTokenizer
				assertTrue(expected.Message, expected.Message.Contains("wrong state"));
			}
			finally
			{
				// consume correctly
				ts.Reset();
				while (ts.IncrementToken())
				{
				}
				ts.End();
				ts.Dispose();
			}
			// check for a missing close()
			ts = a.TokenStream("bogus", input);
			ts.Reset();
			while (ts.IncrementToken())
			{
			}
			ts.End();
			try
			{
				ts = a.TokenStream("bogus", input);
				NUnit.Framework.Assert.Fail("didn't get expected exception when close() not called"
					);
			}
			catch (InvalidOperationException)
			{
			}
			finally
			{
				// ok
				ts.Dispose();
			}
		}
        public static void CheckOneTerm(Analyzer a, String input, String expected)
        {
            AssertAnalyzesTo(a, input, new String[] { expected });
        }


        /** utility method for blasting tokenstreams with data to make sure they don't do anything crazy */
        public static void CheckRandomData(Random random, Analyzer a, int iterations)
        {
            CheckRandomData(random, a, iterations, 20, false, true);
        }

        /** utility method for blasting tokenstreams with data to make sure they don't do anything crazy */
        public static void CheckRandomData(Random random, Analyzer a, int iterations, int maxWordLength)
        {
            CheckRandomData(random, a, iterations, maxWordLength, false, true);
        }

        /** 
         * utility method for blasting tokenstreams with data to make sure they don't do anything crazy 
         * @param simple true if only ascii strings will be used (try to avoid)
         */
        public static void CheckRandomData(Random random, Analyzer a, int iterations, bool simple)
        {
            CheckRandomData(random, a, iterations, 20, simple, true);
        }

        internal class AnalysisThread : ThreadClass
        {
            internal readonly int iterations;
            internal readonly int maxWordLength;
            internal readonly long seed;
            internal readonly Analyzer a;
            internal readonly bool useCharFilter;
            internal readonly bool simple;
            internal readonly bool offsetsAreCorrect;
            internal readonly RandomIndexWriter iw;

			internal readonly ManualResetEvent latch;
            // add memory barriers (ie alter how threads
            // interact)... so this is just "best effort":
            public bool failed;

			internal AnalysisThread(long seed, ManualResetEvent latch, Analyzer a, int iterations
				, int maxWordLength, bool useCharFilter, bool simple, bool offsetsAreCorrect, RandomIndexWriter
				 iw)
            {
                this.seed = seed;
                this.a = a;
                this.iterations = iterations;
                this.maxWordLength = maxWordLength;
                this.useCharFilter = useCharFilter;
                this.simple = simple;
                this.offsetsAreCorrect = offsetsAreCorrect;
                this.iw = iw;
				this.latch = latch;
            }

            public override void Run()
            {
                bool success = false;
                try
                {
					latch.WaitOne();
                    // see the part in checkRandomData where it replays the same text again
                    // to verify reproducability/reuse: hopefully this would catch thread hazards.
                    CheckRandomData(new Random((int)seed), a, iterations, maxWordLength, useCharFilter, simple, offsetsAreCorrect, iw);
                    success = true;
                }
                finally
                {
                    failed = !success;
                }
            }
        }

        public static void CheckRandomData(Random random, Analyzer a, int iterations, int maxWordLength, bool simple)
        {
            CheckRandomData(random, a, iterations, maxWordLength, simple, true);
        }

        public static void CheckRandomData(Random random, Analyzer a, int iterations, int maxWordLength,
            bool simple, bool offsetsAreCorrect)
        {
			CheckResetException(a, "best effort");
            long seed = random.Next();
            bool useCharFilter = random.NextBoolean();
            Directory dir = null;
            RandomIndexWriter iw = null;
            String postingsFormat = TestUtil.GetPostingsFormat("dummy");
            bool codecOk = iterations * maxWordLength < 100000 ||
                !(postingsFormat.Equals("Memory") ||
                    postingsFormat.Equals("SimpleText"));
			if (Rarely(random) && codecOk)
            {
				dir = NewFSDirectory(CreateTempDir("bttc"));
                iw = new RandomIndexWriter(new Random((int)seed), dir, a);
            }
            bool success = false;
            try
            {
                CheckRandomData(new Random(seed), a, iterations, maxWordLength, useCharFilter, simple, offsetsAreCorrect, iw);
                // now test with multiple threads: note we do the EXACT same thing we did before in each thread,
                // so this should only really fail from another thread if its an actual thread problem
                int numThreads = _TestUtil.nextInt(random, 2, 4);
				CountDownLatch startingGun = new CountDownLatch(1);
                AnalysisThread[] threads = new AnalysisThread[numThreads];
                for (int i = 0; i < threads.Length; i++)
                {
                    threads[i] = new AnalysisThread(seed, startingGun, a, iterations, maxWordLength, useCharFilter, simple, offsetsAreCorrect, iw);
                }
                for (int i = 0; i < threads.Length; i++)
                {
                    threads[i].Start();
                }
                for (int i = 0; i < threads.Length; i++)
                {
                    try
                    {
                        threads[i].Join();
                    }
                    catch (ThreadInterruptedException e)
                    {
                        throw;
                    }
                }
                for (int i = 0; i < threads.Length; i++)
                {
                    if (threads[i].failed)
                    {
                        throw new SystemException("some thread(s) failed");
                    }
                }
                success = true;
            }
            finally
            {
                if (success)
                {
                    IOUtils.Close(iw, dir);
                }
                else
                {
                    IOUtils.CloseWhileHandlingException(iw, dir); // checkindex
                }
            }
        }
		private static void CheckRandomData(Random random, Analyzer a, int iterations, int
			 maxWordLength, bool useCharFilter, bool simple, bool offsetsAreCorrect, RandomIndexWriter
			 iw)
		{
			LineFileDocs docs = new LineFileDocs(random);
			Lucene.NetDocument.Document doc = null;
			Field field = null;
			Field currentField = null;
			StringReader bogus = new StringReader(string.Empty);
			if (iw != null)
			{
				doc = new Lucene.NetDocument.Document();
				FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
				if (random.NextBoolean())
				{
					ft.SetStoreTermVectors(true);
					ft.SetStoreTermVectorOffsets(random.NextBoolean());
					ft.SetStoreTermVectorPositions(random.NextBoolean());
					if (ft.StoreTermVectorPositions() && !OLD_FORMAT_IMPERSONATION_IS_ACTIVE)
					{
						ft.SetStoreTermVectorPayloads(random.NextBoolean());
					}
				}
				if (random.NextBoolean())
				{
					ft.SetOmitNorms(true);
				}
				string pf = TestUtil.GetPostingsFormat("dummy");
				bool supportsOffsets = !doesntSupportOffsets.Contains(pf);
				switch (random.Next(4))
				{
					case 0:
					{
						ft.SetIndexOptions(FieldInfo.IndexOptions.DOCS_ONLY);
						break;
					}

					case 1:
					{
						ft.SetIndexOptions(FieldInfo.IndexOptions.DOCS_AND_FREQS);
						break;
					}

					case 2:
					{
						ft.SetIndexOptions(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS);
						break;
					}

					default:
					{
						if (supportsOffsets && offsetsAreCorrect)
						{
							ft.SetIndexOptions(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
								);
						}
						else
						{
							ft.SetIndexOptions(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS);
						}
						break;
					}
				}
				currentField = field = new Field("dummy", bogus, ft);
				doc.Add(currentField);
			}
			try
			{
				for (int i = 0; i < iterations; i++)
				{
					string text;
					if (random.Next(10) == 7)
					{
						// real data from linedocs
						text = docs.NextDoc().Get("body");
						if (text.Length > maxWordLength)
						{
							// Take a random slice from the text...:
							int startPos = random.Next(text.Length - maxWordLength);
							if (startPos > 0 && char.IsLowSurrogate(text[startPos]))
							{
								// Take care not to split up a surrogate pair:
								startPos--;
							}
							//HM:revisit 
							//assert Character.isHighSurrogate(text.charAt(startPos));
							int endPos = startPos + maxWordLength - 1;
							if (char.IsHighSurrogate(text[endPos]))
							{
								// Take care not to split up a surrogate pair:
								endPos--;
							}
							text = Sharpen.Runtime.Substring(text, startPos, 1 + endPos);
						}
					}
					else
					{
						// synthetic
						text = TestUtil.RandomAnalysisString(random, maxWordLength, simple);
					}
					try
					{
						CheckAnalysisConsistency(random, a, useCharFilter, text, offsetsAreCorrect, currentField
							);
						if (iw != null)
						{
							if (random.Next(7) == 0)
							{
								// pile up a multivalued field
								FieldType ft = field.FieldType();
								currentField = new Field("dummy", bogus, ft);
								doc.Add(currentField);
							}
							else
							{
								iw.AddDocument(doc);
								if (doc.GetFields().Count > 1)
								{
									// back to 1 field
									currentField = field;
									doc.RemoveFields("dummy");
									doc.Add(currentField);
								}
							}
						}
					}
					catch (Exception t)
					{
						// TODO: really we should pass a random seed to
						// checkAnalysisConsistency then print it here too:
						System.Console.Error.WriteLine("TEST FAIL: useCharFilter=" + useCharFilter + " text='"
							 + Escape(text) + "'");
						Thrower.Rethrow(t);
					}
				}
			}
			finally
			{
				IOUtils.CloseWhileHandlingException(docs);
			}
		}

		public static string Escape(string s)
		{
			int charUpto = 0;
			StringBuilder sb = new StringBuilder();
			while (charUpto < s.Length)
			{
				int c = s[charUpto];
				if (c == unchecked((int)(0xa)))
				{
					// Strangely, you cannot put \ u000A into Java
					// sources (not in a comment nor a string
					// constant)...:
					sb.Append("\\n");
				}
				else
				{
					if (c == unchecked((int)(0xd)))
					{
						// ... nor \ u000D:
						sb.Append("\\r");
					}
					else
					{
						if (c == '"')
						{
							sb.Append("\\\"");
						}
						else
						{
							if (c == '\\')
							{
								sb.Append("\\\\");
							}
							else
							{
								if (c >= unchecked((int)(0x20)) && c < unchecked((int)(0x80)))
								{
									sb.Append((char)c);
								}
								else
								{
									// TODO: we can make ascii easier to read if we
									// don't escape...
									sb.Append(string.Format(CultureInfo.ROOT, "\\u%04x", c));
								}
							}
						}
					}
				}
				charUpto++;
			}
			return sb.ToString();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static void CheckAnalysisConsistency(Random random, Analyzer a, bool useCharFilter
			, string text)
		{
			CheckAnalysisConsistency(random, a, useCharFilter, text, true);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static void CheckAnalysisConsistency(Random random, Analyzer a, bool useCharFilter
			, string text, bool offsetsAreCorrect)
		{
			CheckAnalysisConsistency(random, a, useCharFilter, text, offsetsAreCorrect, null);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private static void CheckAnalysisConsistency(Random random, Analyzer a, bool useCharFilter
			, string text, bool offsetsAreCorrect, Field field)
		{
			if (VERBOSE)
			{
				System.Console.Out.WriteLine(Sharpen.Thread.CurrentThread().GetName() + ": NOTE: BaseTokenStreamTestCase: get first token stream now text="
					 + text);
			}
			int remainder = random.Next(10);
			StreamReader reader = new StringReader(text);
			TokenStream ts = a.TokenStream("dummy", useCharFilter ? new MockCharFilter(reader
				, remainder) : reader);
			CharTermAttribute termAtt = ts.HasAttribute(typeof(CharTermAttribute)) ? ts.GetAttribute
				<CharTermAttribute>() : null;
			OffsetAttribute offsetAtt = ts.HasAttribute(typeof(OffsetAttribute)) ? ts.GetAttribute
				<OffsetAttribute>() : null;
			PositionIncrementAttribute posIncAtt = ts.HasAttribute(typeof(PositionIncrementAttribute
				)) ? ts.GetAttribute<PositionIncrementAttribute>() : null;
			PositionLengthAttribute posLengthAtt = ts.HasAttribute(typeof(PositionLengthAttribute
				)) ? ts.GetAttribute<PositionLengthAttribute>() : null;
			TypeAttribute typeAtt = ts.HasAttribute(typeof(TypeAttribute)) ? ts.GetAttribute<
				TypeAttribute>() : null;
			IList<string> tokens = new AList<string>();
			IList<string> types = new AList<string>();
			IList<int> positions = new AList<int>();
			IList<int> positionLengths = new AList<int>();
			IList<int> startOffsets = new AList<int>();
			IList<int> endOffsets = new AList<int>();
			ts.Reset();
			// First pass: save away "correct" tokens
			while (ts.IncrementToken())
			{
				NUnit.Framework.Assert.IsNotNull("has no CharTermAttribute", termAtt);
				tokens.AddItem(termAtt.ToString());
				if (typeAtt != null)
				{
					types.AddItem(typeAtt.Type());
				}
				if (posIncAtt != null)
				{
					positions.AddItem(posIncAtt.GetPositionIncrement());
				}
				if (posLengthAtt != null)
				{
					positionLengths.AddItem(posLengthAtt.GetPositionLength());
				}
				if (offsetAtt != null)
				{
					startOffsets.AddItem(offsetAtt.StartOffset());
					endOffsets.AddItem(offsetAtt.EndOffset());
				}
			}
			ts.End();
			ts.Close();
			// verify reusing is "reproducable" and also get the normal tokenstream sanity checks
			if (!tokens.IsEmpty())
			{
				// KWTokenizer (for example) can produce a token
				// even when input is length 0:
				if (text.Length != 0)
				{
					// (Optional) second pass: do something evil:
					int evilness = random.Next(50);
					if (evilness == 17)
					{
						if (VERBOSE)
						{
							System.Console.Out.WriteLine(Sharpen.Thread.CurrentThread().GetName() + ": NOTE: BaseTokenStreamTestCase: re-run analysis w/ exception"
								);
						}
						// Throw an errant exception from the Reader:
						MockReaderWrapper evilReader = new MockReaderWrapper(random, new StringReader(text
							));
						evilReader.ThrowExcAfterChar(random.Next(text.Length + 1));
						reader = evilReader;
						try
						{
							// NOTE: some Tokenizers go and read characters
							// when you call .setReader(Reader), eg
							// PatternTokenizer.  This is a bit
							// iffy... (really, they should only
							// pull from the Reader when you call
							// .incremenToken(), I think?), but we
							// currently allow it, so, we must call
							// a.tokenStream inside the try since we may
							// hit the exc on init:
							ts = a.TokenStream("dummy", useCharFilter ? new MockCharFilter(evilReader, remainder
								) : evilReader);
							ts.Reset();
							while (ts.IncrementToken())
							{
							}
							NUnit.Framework.Assert.Fail("did not hit exception");
						}
						catch (RuntimeException re)
						{
							NUnit.Framework.Assert.IsTrue(MockReaderWrapper.IsMyEvilException(re));
						}
						try
						{
							ts.End();
						}
						catch (Exception ae)
						{
							// Catch & ignore MockTokenizer's
							// anger...
							if ("end() called before incrementToken() returned false!".Equals(ae.Message))
							{
							}
							else
							{
								// OK
								throw;
							}
						}
						ts.Close();
					}
					else
					{
						if (evilness == 7)
						{
							// Only consume a subset of the tokens:
							int numTokensToRead = random.Next(tokens.Count);
							if (VERBOSE)
							{
								System.Console.Out.WriteLine(Sharpen.Thread.CurrentThread().GetName() + ": NOTE: BaseTokenStreamTestCase: re-run analysis, only consuming "
									 + numTokensToRead + " of " + tokens.Count + " tokens");
							}
							reader = new StringReader(text);
							ts = a.TokenStream("dummy", useCharFilter ? new MockCharFilter(reader, remainder)
								 : reader);
							ts.Reset();
							for (int tokenCount = 0; tokenCount < numTokensToRead; tokenCount++)
							{
								NUnit.Framework.Assert.IsTrue(ts.IncrementToken());
							}
							try
							{
								ts.End();
							}
							catch (Exception ae)
							{
								// Catch & ignore MockTokenizer's
								// anger...
								if ("end() called before incrementToken() returned false!".Equals(ae.Message))
								{
								}
								else
								{
									// OK
									throw;
								}
							}
							ts.Close();
						}
					}
				}
			}
			// Final pass: verify clean tokenization matches
			// results from first pass:
			if (VERBOSE)
			{
				System.Console.Out.WriteLine(Sharpen.Thread.CurrentThread().GetName() + ": NOTE: BaseTokenStreamTestCase: re-run analysis; "
					 + tokens.Count + " tokens");
			}
			reader = new StringReader(text);
			long seed = random.NextLong();
			random = new Random(seed);
			if (random.Next(30) == 7)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine(Sharpen.Thread.CurrentThread().GetName() + ": NOTE: BaseTokenStreamTestCase: using spoon-feed reader"
						);
				}
				reader = new MockReaderWrapper(random, reader);
			}
			ts = a.TokenStream("dummy", useCharFilter ? new MockCharFilter(reader, remainder)
				 : reader);
			if (typeAtt != null && posIncAtt != null && posLengthAtt != null && offsetAtt != 
				null)
			{
				// offset + pos + posLength + type
				AssertTokenStreamContents(ts, Sharpen.Collections.ToArray(tokens, new string[tokens
					.Count]), ToIntArray(startOffsets), ToIntArray(endOffsets), Sharpen.Collections.ToArray
					(types, new string[types.Count]), ToIntArray(positions), ToIntArray(positionLengths
					), text.Length, offsetsAreCorrect);
			}
			else
			{
				if (typeAtt != null && posIncAtt != null && offsetAtt != null)
				{
					// offset + pos + type
					AssertTokenStreamContents(ts, Sharpen.Collections.ToArray(tokens, new string[tokens
						.Count]), ToIntArray(startOffsets), ToIntArray(endOffsets), Sharpen.Collections.ToArray
						(types, new string[types.Count]), ToIntArray(positions), null, text.Length, offsetsAreCorrect
						);
				}
				else
				{
					if (posIncAtt != null && posLengthAtt != null && offsetAtt != null)
					{
						// offset + pos + posLength
						AssertTokenStreamContents(ts, Sharpen.Collections.ToArray(tokens, new string[tokens
							.Count]), ToIntArray(startOffsets), ToIntArray(endOffsets), null, ToIntArray(positions
							), ToIntArray(positionLengths), text.Length, offsetsAreCorrect);
					}
					else
					{
						if (posIncAtt != null && offsetAtt != null)
						{
							// offset + pos
							AssertTokenStreamContents(ts, Sharpen.Collections.ToArray(tokens, new string[tokens
								.Count]), ToIntArray(startOffsets), ToIntArray(endOffsets), null, ToIntArray(positions
								), null, text.Length, offsetsAreCorrect);
						}
						else
						{
							if (offsetAtt != null)
							{
								// offset
								AssertTokenStreamContents(ts, Sharpen.Collections.ToArray(tokens, new string[tokens
									.Count]), ToIntArray(startOffsets), ToIntArray(endOffsets), null, null, null, text
									.Length, offsetsAreCorrect);
							}
							else
							{
								// terms only
								AssertTokenStreamContents(ts, Sharpen.Collections.ToArray(tokens, new string[tokens
									.Count]));
							}
						}
					}
				}
			}
			if (field != null)
			{
				reader = new StringReader(text);
				random = new Random(seed);
				if (random.Next(30) == 7)
				{
					if (VERBOSE)
					{
						System.Console.Out.WriteLine(Sharpen.Thread.CurrentThread().GetName() + ": NOTE: BaseTokenStreamTestCase: indexing using spoon-feed reader"
							);
					}
					reader = new MockReaderWrapper(random, reader);
				}
				field.SetReaderValue(useCharFilter ? new MockCharFilter(reader, remainder) : reader
					);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual string ToDot(Analyzer a, string inputText)
		{
			StringWriter sw = new StringWriter();
			TokenStream ts = a.TokenStream("field", inputText);
			ts.Reset();
			new TokenStreamToDot(inputText, ts, new PrintWriter(sw)).ToDot();
			return sw.ToString();
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual void ToDotFile(Analyzer a, string inputText, string localFileName
			)
		{
			TextWriter w = new OutputStreamWriter(new FileOutputStream(localFileName), StandardCharsets
				.UTF_8);
			TokenStream ts = a.TokenStream("field", inputText);
			ts.Reset();
			new TokenStreamToDot(inputText, ts, new PrintWriter(w)).ToDot();
			w.Close();
		}

		internal static int[] ToIntArray(IList<int> list)
		{
			int[] ret = new int[list.Count];
			int offset = 0;
			foreach (int i in list)
			{
				ret[offset++] = i;
			}
			return ret;
		}
    }
}