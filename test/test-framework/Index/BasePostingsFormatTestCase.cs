
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lucene.Net.Codecs;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.TestFramework.Util;
using Lucene.Net.Util;
using NUnit.Framework;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.TestFramework.Index
{
	/// <summary>Abstract class to do basic tests for a postings format.</summary>
	/// <remarks>
	/// Abstract class to do basic tests for a postings format.
	/// NOTE: This test focuses on the postings
	/// (docs/freqs/positions/payloads/offsets) impl, not the
	/// terms dict.  The [stretch] goal is for this test to be
	/// so thorough in testing a new PostingsFormat that if this
	/// test passes, then all Lucene/Solr tests should also pass.  Ie,
	/// if there is some bug in a given PostingsFormat that this
	/// test fails to catch then this test needs to be improved!
	/// </remarks>
	public abstract class BasePostingsFormatTestCase : BaseIndexFileFormatTestCase
	{
		private enum Option
		{
			SKIPPING,
			REUSE_ENUMS,
			LIVE_DOCS,
			TERM_STATE,
			PARTIAL_DOC_CONSUME,
			PARTIAL_POS_CONSUME,
			PAYLOADS,
			THREADS
		}

		/// <summary>
		/// Given the same random seed this always enumerates the
		/// same random postings
		/// </summary>
		private class SeedPostings : DocsAndPositionsEnum
		{
			private readonly Random docRandom;

			private readonly Random random;

			public int docFreq;

			private readonly int maxDocSpacing;

			private readonly int payloadSize;

			private readonly bool fixedPayloads;

			private readonly IBits liveDocs;

			private readonly BytesRef payload;

			private readonly FieldInfo.IndexOptions options;

			private readonly bool doPositions;

			private int docID;

			private int freq;

			public int upto;

			private int pos;

			private int offset;

			private int startOffset;

			private int endOffset;

			private int posSpacing;

			private int posUpto;

			public SeedPostings(long seed, int minDocFreq, int maxDocFreq, IBits liveDocs, FieldInfo.IndexOptions
				 options)
			{
				// TODO can we make it easy for testing to pair up a "random terms dict impl" with your postings base format...
				// TODO test when you reuse after skipping a term or two, eg the block reuse case
				// Sometimes use .advance():
				// Sometimes reuse the Docs/AndPositionsEnum across terms:
				// Sometimes pass non-null live docs:
				// Sometimes seek to term using previously saved TermState:
				// Sometimes don't fully consume docs from the enum
				// Sometimes don't fully consume positions at each doc
				// Sometimes check payloads
				// Test w/ multiple threads
				// Used only to generate docIDs; this way if you pull w/
				// or w/o positions you get the same docID sequence:
				random = new Random((int) seed);
				docRandom = Random();
				docFreq = TestUtil.NextInt(random, minDocFreq, maxDocFreq);
				this.liveDocs = liveDocs;
				// TODO: more realistic to inversely tie this to numDocs:
				maxDocSpacing = TestUtil.NextInt(random, 1, 100);
				if (random.Next(10) == 7)
				{
					// 10% of the time create big payloads:
					payloadSize = 1 + random.Next(3);
				}
				else
				{
					payloadSize = 1 + random.Next(1);
				}
				fixedPayloads = random.NextBoolean();
				var payloadBytes = new sbyte[payloadSize];
				payload = new BytesRef(payloadBytes);
				this.options = options;
				doPositions = FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS.CompareTo(options
					) <= 0;
			}

			public override int NextDoc()
			{
				while (true)
				{
					_nextDoc();
					if (liveDocs == null || docID == NO_MORE_DOCS || liveDocs[docID])
					{
						return docID;
					}
				}
			}

			private int _nextDoc()
			{
				// Must consume random:
				while (posUpto < freq)
				{
					NextPosition();
				}
				if (upto < docFreq)
				{
					if (upto == 0 && docRandom.NextBoolean())
					{
					}
					else
					{
						// Sometimes index docID = 0
						if (maxDocSpacing == 1)
						{
							docID++;
						}
						else
						{
							// TODO: sometimes have a biggish gap here!
							docID += TestUtil.NextInt(docRandom, 1, maxDocSpacing);
						}
					}
					if (random.Next(200) == 17)
					{
						freq = TestUtil.NextInt(random, 1, 1000);
					}
					else
					{
						if (random.Next(10) == 17)
						{
							freq = TestUtil.NextInt(random, 1, 20);
						}
						else
						{
							freq = TestUtil.NextInt(random, 1, 4);
						}
					}
					pos = 0;
					offset = 0;
					posUpto = 0;
					posSpacing = TestUtil.NextInt(random, 1, 100);
					upto++;
					return docID;
				}
				else
				{
					return docID = NO_MORE_DOCS;
				}
			}

			public override int DocID
			{
			    get { return docID; }
			}

			public override int Freq
			{
			    get { return freq; }
			}

			public override int NextPosition()
			{
				if (!doPositions)
				{
					posUpto = freq;
					return 0;
				}
				//HM:revisit 
				//assert posUpto < freq;
				if (posUpto == 0 && random.NextBoolean())
				{
				}
				else
				{
					// Sometimes index pos = 0
					if (posSpacing == 1)
					{
						pos++;
					}
					else
					{
						pos += TestUtil.NextInt(random, 1, posSpacing);
					}
				}
				if (payloadSize != 0)
				{
				    var bytes = Array.ConvertAll(payload.bytes, s => (byte) s);
                    
					if (fixedPayloads)
					{
						payload.length = payloadSize;
						random.NextBytes(bytes);
					}
					else
					{
						int thisPayloadSize = random.Next(payloadSize);
						if (thisPayloadSize != 0)
						{
							payload.length = payloadSize;
							random.NextBytes(bytes);
						}
						else
						{
							payload.length = 0;
						}
					}
				}
				else
				{
					payload.length = 0;
				}
				startOffset = offset + random.Next(5);
				endOffset = startOffset + random.Next(10);
				offset = endOffset;
				posUpto++;
				return pos;
			}

			public override int StartOffset
			{
			    get { return startOffset; }
			}

			public override int EndOffset
			{
			    get { return endOffset; }
			}

			public override BytesRef Payload
			{
			    get { return payload.length == 0 ? null : payload; }
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Advance(int target)
			{
				return SlowAdvance(target);
			}

			public override long Cost
			{
			    get { return docFreq; }
			}
		}

		private class FieldAndTerm
		{
			internal string field;

			internal BytesRef term;

			public FieldAndTerm(string field, BytesRef term)
			{
				this.field = field;
				this.term = BytesRef.DeepCopyOf(term);
			}
		}

		private static IDictionary<string, IDictionary<BytesRef, long>> fields;

		private static FieldInfos fieldInfos;

		private static FixedBitSet globalLiveDocs;

		private static IList<FieldAndTerm> allTerms;

		private static int maxDoc;

		private static long totalPostings;

		private static long totalPayloadBytes;

		// Holds all postings:
		private static SeedPostings GetSeedPostings(string term, long seed, bool withLiveDocs, FieldInfo.IndexOptions options)
		{
			int minDocFreq;
			int maxDocFreq;
			if (term.StartsWith("big_"))
			{
				minDocFreq = RANDOM_MULTIPLIER * 50000;
				maxDocFreq = RANDOM_MULTIPLIER * 70000;
			}
			else
			{
				if (term.StartsWith("medium_"))
				{
					minDocFreq = RANDOM_MULTIPLIER * 3000;
					maxDocFreq = RANDOM_MULTIPLIER * 6000;
				}
				else
				{
					if (term.StartsWith("low_"))
					{
						minDocFreq = RANDOM_MULTIPLIER;
						maxDocFreq = RANDOM_MULTIPLIER * 40;
					}
					else
					{
						minDocFreq = 1;
						maxDocFreq = 3;
					}
				}
			}
			return new SeedPostings(seed, minDocFreq, maxDocFreq, 
				withLiveDocs ? globalLiveDocs : null, options);
		}

		/// <exception cref="System.IO.IOException"></exception>
		[TestFixtureSetUp]
		public static void CreatePostings()
		{
			totalPostings = 0;
			totalPayloadBytes = 0;
			fields = new SortedDictionary<string, IDictionary<BytesRef, long>>();
			int numFields = TestUtil.NextInt(Random(), 1, 5);
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: " + numFields + " fields");
			}
			maxDoc = 0;
			FieldInfo[] fieldInfoArray = new FieldInfo[numFields];
			int fieldUpto = 0;
			while (fieldUpto < numFields)
			{
				string field = TestUtil.RandomSimpleString(Random());
				if (fields.ContainsKey(field))
				{
					continue;
				}
				fieldInfoArray[fieldUpto] = new FieldInfo(field, true, fieldUpto, false, false, true
					, FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS, null, FieldInfo.DocValuesType
					.NUMERIC, null);
				fieldUpto++;
				IDictionary<BytesRef, long> postings = new SortedDictionary<BytesRef, long>();
				fields[field] = postings;
				ICollection<string> seenTerms = new HashSet<string>();
				int numTerms;
				if (Random().Next(10) == 7)
				{
					numTerms = AtLeast(50);
				}
				else
				{
					numTerms = TestUtil.NextInt(Random(), 2, 20);
				}
				for (int termUpto = 0; termUpto < numTerms; termUpto++)
				{
					string term = TestUtil.RandomSimpleString(Random());
					if (seenTerms.Contains(term))
					{
						continue;
					}
					seenTerms.Add(term);
					if (TEST_NIGHTLY && termUpto == 0 && fieldUpto == 1)
					{
						// Make 1 big term:
						term = "big_" + term;
					}
					else
					{
						if (termUpto == 1 && fieldUpto == 1)
						{
							// Make 1 medium term:
							term = "medium_" + term;
						}
						else
						{
							if (Random().NextBoolean())
							{
								// Low freq term:
								term = "low_" + term;
							}
							else
							{
								// Very low freq term (don't multiply by RANDOM_MULTIPLIER):
								term = "verylow_" + term;
							}
						}
					}
					int termSeed = Random().NextInt(0,int.MaxValue);
					postings[new BytesRef(term)] = termSeed;
					// NOTE: sort of silly: we enum all the docs just to
					// get the maxDoc
					DocsEnum docsEnum = GetSeedPostings(term, termSeed, false, FieldInfo.IndexOptions
						.DOCS_ONLY);
					int doc;
					int lastDoc = 0;
					while ((doc = docsEnum.NextDoc()) != DocsEnum.NO_MORE_DOCS)
					{
						lastDoc = doc;
					}
					maxDoc = Math.Max(lastDoc, maxDoc);
				}
			}
			fieldInfos = new FieldInfos(fieldInfoArray);
			// It's the count, not the last docID:
			maxDoc++;
			globalLiveDocs = new FixedBitSet(maxDoc);
			double liveRatio = Random().NextDouble();
			for (int i = 0; i < maxDoc; i++)
			{
				if (Random().NextDouble() <= liveRatio)
				{
					globalLiveDocs.Set(i);
				}
			}
			allTerms = new List<FieldAndTerm>();
			foreach (KeyValuePair<string, IDictionary<BytesRef, long>> fieldEnt in fields)
			{
				string field = fieldEnt.Key;
				foreach (KeyValuePair<BytesRef, long> termEnt in fieldEnt.Value)
				{
					allTerms.Add(new FieldAndTerm(field, termEnt.Key));
				}
			}
			if (VERBOSE)
			{
				Console.Out.WriteLine("TEST: done init postings; " + allTerms.Count + " total terms, across "
					 + fieldInfos.Size + " fields");
			}
		}

		/// <exception cref="System.Exception"></exception>
		[TestFixtureTearDown]
		public static void AfterClass()
		{
			allTerms = null;
			fieldInfos = null;
			fields = null;
			globalLiveDocs = null;
		}

		private FieldInfos currentFieldInfos;

		// TODO maybe instead of @BeforeClass just make a single test run: build postings & index & test it?
		// maxAllowed = the "highest" we can index, but we will still
		// randomly index at lower IndexOption
		/// <exception cref="System.IO.IOException"></exception>
		private FieldsProducer BuildIndex(Directory dir, FieldInfo.IndexOptions maxAllowed, bool allowPayloads, bool alwaysTestMax)
		{
			Codec codec = GetCodec();
			var segmentInfo = new SegmentInfo(dir, Constants.LUCENE_MAIN_VERSION, "_0", maxDoc, false, codec, null);
		    int maxIndexOption = Enum.GetNames(typeof (FieldInfo.IndexOptions)).ToList().IndexOf(maxAllowed.ToString());
		    
			if (VERBOSE)
			{
				Console.Out.WriteLine("\nTEST: now build index");
			}
			int maxIndexOptionNoOffsets = Enum.GetNames(typeof(FieldInfo.IndexOptions)).ToList().IndexOf
				(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS.ToString());
			// TODO use allowPayloads
			FieldInfo[] newFieldInfoArray = new FieldInfo[fields.Count];
			for (int fieldUpto = 0; fieldUpto < fields.Count; fieldUpto++)
			{
				FieldInfo oldFieldInfo = fieldInfos.FieldInfo(fieldUpto);
				string pf = TestUtil.GetPostingsFormat(codec, oldFieldInfo.name);
				int fieldMaxIndexOption;
				if (doesntSupportOffsets.Contains(pf))
				{
					fieldMaxIndexOption = Math.Min(maxIndexOptionNoOffsets, maxIndexOption);
				}
				else
				{
					fieldMaxIndexOption = maxIndexOption;
				}
				// Randomly picked the IndexOptions to index this
				// field with:
			    string fieldOptValue = Enum.GetNames(typeof(FieldInfo.IndexOptions))[alwaysTestMax
			        ? fieldMaxIndexOption : Random().Next(1 + fieldMaxIndexOption)];
			    FieldInfo.IndexOptions indexOptions = (FieldInfo.IndexOptions) Enum.Parse(typeof(FieldInfo.IndexOptions), fieldOptValue);
				bool doPayloads = indexOptions.CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS
					) >= 0 && allowPayloads;
				newFieldInfoArray[fieldUpto] = new FieldInfo(oldFieldInfo.name, true, fieldUpto, 
					false, false, doPayloads, indexOptions, null, FieldInfo.DocValuesType.NUMERIC, null
					);
			}
			FieldInfos newFieldInfos = new FieldInfos(newFieldInfoArray);
			// Estimate that flushed segment size will be 25% of
			// what we use in RAM:
			long bytes = totalPostings * 8 + totalPayloadBytes;
			SegmentWriteState writeState = new SegmentWriteState(null, dir, segmentInfo, newFieldInfos
				, 32, null, new IOContext(new FlushInfo(maxDoc, bytes)));
			FieldsConsumer fieldsConsumer = codec.PostingsFormat.FieldsConsumer(writeState);
			foreach (KeyValuePair<string, IDictionary<BytesRef, long>> fieldEnt in fields)
			{
				string field = fieldEnt.Key;
				IDictionary<BytesRef, long> terms = fieldEnt.Value;
				FieldInfo fieldInfo = newFieldInfos.FieldInfo(field);
				FieldInfo.IndexOptions indexOptions = fieldInfo.IndexOptionsValue.Value;
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("field=" + field + " indexOtions=" + indexOptions);
				}
				bool doFreq = indexOptions.CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS) >= 0;
				bool doPos = indexOptions.CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS
					) >= 0;
				bool doPayloads = indexOptions.CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS
					) >= 0 && allowPayloads;
				bool doOffsets = indexOptions.CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
					) >= 0;
				TermsConsumer termsConsumer = fieldsConsumer.AddField(fieldInfo);
				long sumTotalTF = 0;
				long sumDF = 0;
				FixedBitSet seenDocs = new FixedBitSet(maxDoc);
				foreach (KeyValuePair<BytesRef, long> termEnt in terms)
				{
					BytesRef term = termEnt.Key;
					SeedPostings postings = GetSeedPostings(term.Utf8ToString
						(), (int) termEnt.Value, false, maxAllowed);
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("  term=" + field + ":" + term.Utf8ToString() + " docFreq="
							 + postings.docFreq + " seed=" + termEnt.Value);
					}
					PostingsConsumer postingsConsumer = termsConsumer.StartTerm(term);
					long totalTF = 0;
					int docID = 0;
					while ((docID = postings.NextDoc()) != DocsEnum.NO_MORE_DOCS)
					{
						int freq = postings.Freq;
						if (VERBOSE)
						{
							Console.Out.WriteLine("    " + postings.upto + ": docID=" + docID + " freq="+ postings.Freq);
						}
						postingsConsumer.StartDoc(docID, doFreq ? postings.Freq : -1);
						seenDocs.Set(docID);
						if (doPos)
						{
							totalTF += postings.Freq;
							for (int posUpto = 0; posUpto < freq; posUpto++)
							{
								int pos = postings.NextPosition();
								BytesRef payload = postings.Payload;
								if (VERBOSE)
								{
									if (doPayloads)
									{
										Console.Out.WriteLine("      pos=" + pos + " payload=" + (payload == null ? 
											"null" : payload.length + " bytes"));
									}
									else
									{
										Console.Out.WriteLine("      pos=" + pos);
									}
								}
								postingsConsumer.AddPosition(pos, doPayloads ? payload : null, doOffsets ? postings
									.StartOffset : -1, doOffsets ? postings.EndOffset : -1);
							}
						}
						else
						{
							if (doFreq)
							{
								totalTF += freq;
							}
							else
							{
								totalTF++;
							}
						}
						postingsConsumer.FinishDoc();
					}
					termsConsumer.FinishTerm(term, new TermStats(postings.docFreq, doFreq ? totalTF : 
						-1));
					sumTotalTF += totalTF;
					sumDF += postings.docFreq;
				}
				termsConsumer.Finish(doFreq ? sumTotalTF : -1, sumDF, seenDocs.Cardinality());
			}
			fieldsConsumer.Dispose();
			if (VERBOSE)
			{
				Console.Out.WriteLine("TEST: after indexing: files=");
				foreach (string file in dir.ListAll())
				{
					Console.Out.WriteLine("  " + file + ": " + dir.FileLength(file) + " bytes");
				}
			}
			currentFieldInfos = newFieldInfos;
			SegmentReadState readState = new SegmentReadState(dir, segmentInfo, newFieldInfos, IOContext.READ, 1);
			return codec.PostingsFormat.FieldsProducer(readState);
		}

		private class ThreadState
		{
			public DocsEnum reuseDocsEnum;

			public DocsAndPositionsEnum reuseDocsAndPositionsEnum;
			// Only used with REUSE option:
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void VerifyEnum(ThreadState threadState, string
			 field, BytesRef term, TermsEnum termsEnum, FieldInfo.IndexOptions maxTestOptions
			, FieldInfo.IndexOptions maxIndexOptions, List<string> options, bool alwaysTestMax)
		{
			// Maximum options (docs/freqs/positions/offsets) to test:
			if (VERBOSE)
			{
				Console.Out.WriteLine("  verifyEnum: options=" + options + " maxTestOptions="
					 + maxTestOptions);
			}
			// Make sure TermsEnum really is positioned on the
			// expected term:
			NUnit.Framework.Assert.AreEqual(term, termsEnum.Term);
			// 50% of the time time pass liveDocs:
			bool useLiveDocs = options.Contains(Option.LIVE_DOCS.ToString()) 
				&& Random().NextBoolean();
			IBits liveDocs;
			if (useLiveDocs)
			{
				liveDocs = globalLiveDocs;
				if (VERBOSE)
				{
					Console.Out.WriteLine("  use liveDocs");
				}
			}
			else
			{
				liveDocs = null;
				if (VERBOSE)
				{
					Console.Out.WriteLine("  no liveDocs");
				}
			}
			FieldInfo fieldInfo = currentFieldInfos.FieldInfo(field);
			// NOTE: can be empty list if we are using liveDocs:
			SeedPostings expected = GetSeedPostings(term.Utf8ToString
				(), (fields[field])[term], useLiveDocs, maxIndexOptions);
			AreEqual(expected.docFreq, termsEnum.DocFreq);
			bool allowFreqs = fieldInfo.IndexOptionsValue.Value.CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS
				) >= 0 && maxTestOptions.CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS) >= 0;
			bool doCheckFreqs = allowFreqs && (alwaysTestMax || Random().Next(3) <= 2);
			bool allowPositions = fieldInfo.IndexOptionsValue.Value.CompareTo(FieldInfo.IndexOptions
				.DOCS_AND_FREQS_AND_POSITIONS) >= 0 && maxTestOptions.CompareTo(FieldInfo.IndexOptions
				.DOCS_AND_FREQS_AND_POSITIONS) >= 0;
			bool doCheckPositions = allowPositions && (alwaysTestMax || Random().Next(3) <= 2);
            bool allowOffsets = fieldInfo.IndexOptionsValue.Value.CompareTo(FieldInfo.IndexOptions.
				DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0 && maxTestOptions.CompareTo(FieldInfo.IndexOptions
				.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;
			bool doCheckOffsets = allowOffsets && (alwaysTestMax || Random().Next(3) <= 2);
			bool doCheckPayloads = options.Contains(Option.PAYLOADS.ToString()
				) && allowPositions && fieldInfo.HasPayloads && (alwaysTestMax || Random().Next
				(3) <= 2);
			DocsEnum prevDocsEnum = null;
			DocsEnum docsEnum;
			DocsAndPositionsEnum docsAndPositionsEnum;
			if (!doCheckPositions)
			{
				if (allowPositions && Random().Next(10) == 7)
				{
					// 10% of the time, even though we will not check positions, pull a DocsAndPositions enum
					if (options.Contains(Option.REUSE_ENUMS.ToString()) && Random().Next
						(10) < 9)
					{
						prevDocsEnum = threadState.reuseDocsAndPositionsEnum;
					}
					int flags = 0;
					if (alwaysTestMax || Random().NextBoolean())
					{
						flags |= DocsAndPositionsEnum.FLAG_OFFSETS;
					}
					if (alwaysTestMax || Random().NextBoolean())
					{
						flags |= DocsAndPositionsEnum.FLAG_PAYLOADS;
					}
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("  get DocsAndPositionsEnum (but we won't check positions) flags="
							 + flags);
					}
					threadState.reuseDocsAndPositionsEnum = termsEnum.DocsAndPositions(liveDocs, (DocsAndPositionsEnum
						)prevDocsEnum, flags);
					docsEnum = threadState.reuseDocsAndPositionsEnum;
					docsAndPositionsEnum = threadState.reuseDocsAndPositionsEnum;
				}
				else
				{
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("  get DocsEnum");
					}
					if (options.Contains(Option.REUSE_ENUMS.ToString()) && Random().Next
						(10) < 9)
					{
						prevDocsEnum = threadState.reuseDocsEnum;
					}
					threadState.reuseDocsEnum = termsEnum.Docs(liveDocs, prevDocsEnum, doCheckFreqs ? 
						DocsEnum.FLAG_FREQS : DocsEnum.FLAG_NONE);
					docsEnum = threadState.reuseDocsEnum;
					docsAndPositionsEnum = null;
				}
			}
			else
			{
				if (options.Contains(Option.REUSE_ENUMS.ToString()) && Random().Next
					(10) < 9)
				{
					prevDocsEnum = threadState.reuseDocsAndPositionsEnum;
				}
				int flags = 0;
				if (alwaysTestMax || doCheckOffsets || Random().Next(3) == 1)
				{
					flags |= DocsAndPositionsEnum.FLAG_OFFSETS;
				}
				if (alwaysTestMax || doCheckPayloads || Random().Next(3) == 1)
				{
					flags |= DocsAndPositionsEnum.FLAG_PAYLOADS;
				}
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("  get DocsAndPositionsEnum flags=" + flags);
				}
				threadState.reuseDocsAndPositionsEnum = termsEnum.DocsAndPositions(liveDocs, (DocsAndPositionsEnum
					)prevDocsEnum, flags);
				docsEnum = threadState.reuseDocsAndPositionsEnum;
				docsAndPositionsEnum = threadState.reuseDocsAndPositionsEnum;
			}
			NUnit.Framework.Assert.IsNotNull(docsEnum, "null DocsEnum");
			int initialDocID = docsEnum.DocID;
			AreEqual(-1, initialDocID, "inital docID should be -1" + docsEnum);
			if (VERBOSE)
			{
				if (prevDocsEnum == null)
				{
					Console.Out.WriteLine("  got enum=" + docsEnum);
				}
				else
				{
					if (prevDocsEnum == docsEnum)
					{
						Console.Out.WriteLine("  got reuse enum=" + docsEnum);
					}
					else
					{
						Console.Out.WriteLine("  got enum=" + docsEnum + " (reuse of " + prevDocsEnum+ " failed)");
					}
				}
			}
			// 10% of the time don't consume all docs:
			int stopAt;
			if (!alwaysTestMax && options.Contains(Option.PARTIAL_DOC_CONSUME.ToString()) && expected.docFreq > 1 && Random().Next(10) == 7)
			{
				stopAt = Random().Next(expected.docFreq - 1);
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("  will not consume all docs (" + stopAt + " vs " + 
						expected.docFreq + ")");
				}
			}
			else
			{
				stopAt = expected.docFreq;
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("  consume all docs");
				}
			}
			double skipChance = alwaysTestMax ? 0.5 : Random().NextDouble();
			int numSkips = expected.docFreq < 3 ? 1 : Random().NextInt(1, Math.Min(20, expected.docFreq / 3));
			int skipInc = expected.docFreq / numSkips;
			int skipDocInc = maxDoc / numSkips;
			// Sometimes do 100% skipping:
			bool doAllSkipping = options.Contains(Option.SKIPPING.ToString())
				 && Random().Next(7) == 1;
			double freqAskChance = alwaysTestMax ? 1.0 : Random().NextDouble();
			double payloadCheckChance = alwaysTestMax ? 1.0 : Random().NextDouble();
			double offsetCheckChance = alwaysTestMax ? 1.0 : Random().NextDouble();
			if (VERBOSE)
			{
				if (options.Contains(Option.SKIPPING.ToString()))
				{
					Console.Out.WriteLine("  skipChance=" + skipChance + " numSkips=" + numSkips);
				}
				else
				{
					Console.Out.WriteLine("  no skipping");
				}
				if (doCheckFreqs)
				{
					Console.Out.WriteLine("  freqAskChance=" + freqAskChance);
				}
				if (doCheckPayloads)
				{
					Console.Out.WriteLine("  payloadCheckChance=" + payloadCheckChance);
				}
				if (doCheckOffsets)
				{
					Console.Out.WriteLine("  offsetCheckChance=" + offsetCheckChance);
				}
			}
			while (expected.upto <= stopAt)
			{
				if (expected.upto == stopAt)
				{
					if (stopAt == expected.docFreq)
					{
						AreEqual(DocsEnum
						    .NO_MORE_DOCS, docsEnum.NextDoc(), "DocsEnum should have ended but didn't");
						// Common bug is to forget to set this.doc=NO_MORE_DOCS in the enum!:
						AreEqual(DocsEnum
						    .NO_MORE_DOCS, docsEnum.DocID, "DocsEnum should have ended but didn't");
					}
					break;
				}
				if (options.Contains(BasePostingsFormatTestCase.Option.SKIPPING.ToString()) && (doAllSkipping
					 || Random().NextDouble() <= skipChance))
				{
					int targetDocID = -1;
					if (expected.upto < stopAt && Random().NextBoolean())
					{
						// Pick target we know exists:
						int skipCount = Random().NextInt(1, skipInc);
						for (int skip = 0; skip < skipCount; skip++)
						{
							if (expected.NextDoc() == DocsEnum.NO_MORE_DOCS)
							{
								break;
							}
						}
					}
					else
					{
						// Pick random target (might not exist):
						int skipDocIDs = Random().NextInt(1, skipDocInc);
						if (skipDocIDs > 0)
						{
							targetDocID = expected.DocID + skipDocIDs;
							expected.Advance(targetDocID);
						}
					}
					if (expected.upto >= stopAt)
					{
						int target = Random().NextBoolean() ? maxDoc : DocsEnum.NO_MORE_DOCS;
						if (VERBOSE)
						{
							Console.Out.WriteLine("  now advance to end (target=" + target + ")");
						}
						AreEqual(DocsEnum
						    .NO_MORE_DOCS, docsEnum.Advance(target), "DocsEnum should have ended but didn't");
						break;
					}
				    if (VERBOSE)
				    {
				        if (targetDocID != -1)
				        {
				            Console.Out.WriteLine("  now advance to random target=" + targetDocID + " ("
				                                         + expected.upto + " of " + stopAt + ") current=" + docsEnum.DocID);
				        }
				        else
				        {
				            Console.Out.WriteLine("  now advance to known-exists target=" + expected.DocID + " (" + expected.upto + " of " + stopAt + ") current=" + docsEnum.DocID);
				        }
				    }
				    int docID = docsEnum.Advance(targetDocID != -1 ? targetDocID : expected.DocID);
				    AreEqual(expected.DocID, docID, "docID is wrong");
				}
				else
				{
					expected.NextDoc();
					if (VERBOSE)
					{
						Console.Out.WriteLine("  now nextDoc to " + expected.DocID + " (" + expected
							.upto + " of " + stopAt + ")");
					}
					int docID = docsEnum.NextDoc();
					AreEqual(expected.DocID, docID, "docID is wrong");
					if (docID == DocsEnum.NO_MORE_DOCS)
					{
						break;
					}
				}
				if (doCheckFreqs && Random().NextDouble() <= freqAskChance)
				{
					if (VERBOSE)
					{
						Console.Out.WriteLine("    now freq()=" + expected.Freq);
					}
					int freq = docsEnum.Freq;
					AreEqual(expected.Freq, freq, "freq is wrong");
				}
				if (doCheckPositions)
				{
					int freq = docsEnum.Freq;
					int numPosToConsume;
					if (!alwaysTestMax && options.Contains(Option.PARTIAL_POS_CONSUME.ToString()) && Random().Next(5) == 1)
					{
						numPosToConsume = Random().Next(freq);
					}
					else
					{
						numPosToConsume = freq;
					}
					for (int i = 0; i < numPosToConsume; i++)
					{
						int pos = expected.NextPosition();
						if (VERBOSE)
						{
							System.Console.Out.WriteLine("    now nextPosition to " + pos);
						}
						AreEqual(pos, docsAndPositionsEnum.NextPosition(), "position is wrong");
						if (doCheckPayloads)
						{
							BytesRef expectedPayload = expected.Payload;
							if (Random().NextDouble() <= payloadCheckChance)
							{
								if (VERBOSE)
								{
									Console.Out.WriteLine("      now check expectedPayload length=" + (expectedPayload
										 == null ? 0 : expectedPayload.length));
								}
								if (expectedPayload == null || expectedPayload.length == 0)
								{
									IsNull(docsAndPositionsEnum.Payload, "should not have payload");
								}
								else
								{
									BytesRef payload = docsAndPositionsEnum.Payload;
									IsNotNull(payload, "should have payload but doesn't");
									AreEqual(expectedPayload.length, payload.length, "payload length is wrong");
									for (int byteUpto = 0; byteUpto < expectedPayload.length; byteUpto++)
									{
										AreEqual(expectedPayload.bytes[
										    expectedPayload.offset + byteUpto], payload.bytes[payload.offset + byteUpto], "payload bytes are wrong");
									}
									// make a deep copy
									payload = BytesRef.DeepCopyOf(payload);
									AreEqual(payload
										, docsAndPositionsEnum.Payload, "2nd call to getPayload returns something different!");
								}
							}
							else
							{
								if (VERBOSE)
								{
									Console.Out.WriteLine("      skip check payload length=" + (expectedPayload
										 == null ? 0 : expectedPayload.length));
								}
							}
						}
						if (doCheckOffsets)
						{
							if (Random().NextDouble() <= offsetCheckChance)
							{
								if (VERBOSE)
								{
									Console.Out.WriteLine("      now check offsets: startOff=" + expected.StartOffset + " endOffset=" + expected.EndOffset);
								}
								AreEqual(expected.StartOffset, docsAndPositionsEnum.StartOffset, "startOffset is wrong");
								AreEqual(expected.EndOffset, docsAndPositionsEnum.EndOffset, "endOffset is wrong");
							}
							else
							{
								if (VERBOSE)
								{
									Console.Out.WriteLine("      skip check offsets");
								}
							}
						}
						else
						{
							if (fieldInfo.IndexOptionsValue.Value.CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
								) < 0)
							{
								if (VERBOSE)
								{
									Console.Out.WriteLine("      now check offsets are -1");
								}
								AreEqual(-1, docsAndPositionsEnum.StartOffset, "startOffset isn't -1");
								AreEqual(-1, docsAndPositionsEnum.EndOffset, "endOffset isn't -1");
							}
						}
					}
				}
			}
		}

		

		/// <exception cref="System.Exception"></exception>
		private void TestTerms(Fields fieldsSource, List<string> options, FieldInfo.IndexOptions maxTestOptions, FieldInfo.IndexOptions maxIndexOptions
			, bool alwaysTestMax)
		{
			if (options.Contains(Option.THREADS.ToString()))
			{
				int numThreads = Random().NextInt(2, 5);
				Thread[] threads = new Thread[numThreads];
				for (int threadUpto = 0; threadUpto < numThreads; threadUpto++)
				{
					threads[threadUpto] = new Thread(()=>TestTermsOneThread(fieldsSource
						, options, maxTestOptions, maxIndexOptions, alwaysTestMax));
					threads[threadUpto].Start();
				}
				for (int threadUpto2 = 0; threadUpto2 < numThreads; threadUpto2++)
				{
					threads[threadUpto2].Join();
				}
			}
			else
			{
				TestTermsOneThread(fieldsSource, options, maxTestOptions, maxIndexOptions, alwaysTestMax);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void TestTermsOneThread(Fields fieldsSource, List<string> options, FieldInfo.IndexOptions maxTestOptions, FieldInfo.IndexOptions maxIndexOptions
			, bool alwaysTestMax)
		{
			var threadState = new ThreadState();
			// Test random terms/fields:
			IList<TermState> termStates = new List<TermState>();
			IList<FieldAndTerm> termStateTerms = new List<FieldAndTerm>();
			allTerms.Shuffle(Random());
			int upto = 0;
			while (upto < allTerms.Count)
			{
				bool useTermState = termStates.Count != 0 && Random().Next(5) == 1;
				FieldAndTerm fieldAndTerm;
				TermsEnum termsEnum;
				TermState termState = null;
				if (!useTermState)
				{
					// Seek by random field+term:
					fieldAndTerm = allTerms[upto++];
					if (VERBOSE)
					{
						Console.Out.WriteLine("\nTEST: seek to term=" + fieldAndTerm.field + ":" +
							 fieldAndTerm.term.Utf8ToString());
					}
				}
				else
				{
					// Seek by previous saved TermState
					int idx = Random().Next(termStates.Count);
					fieldAndTerm = termStateTerms[idx];
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("\nTEST: seek using TermState to term=" + fieldAndTerm
							.field + ":" + fieldAndTerm.term.Utf8ToString());
					}
					termState = termStates[idx];
				}
				Terms terms = fieldsSource.Terms(fieldAndTerm.field);
				IsNotNull(terms);
				termsEnum = terms.Iterator(null);
				if (!useTermState)
				{
					IsTrue(termsEnum.SeekExact(fieldAndTerm.term));
				}
				else
				{
					termsEnum.SeekExact(fieldAndTerm.term, termState);
				}
				bool savedTermState = false;
				if (options.Contains(Option.TERM_STATE.ToString()) && !useTermState
					 && Random().Next(5) == 1)
				{
					// Save away this TermState:
					termStates.Add(termsEnum.TermState);
					termStateTerms.Add(fieldAndTerm);
					savedTermState = true;
				}
				VerifyEnum(threadState, fieldAndTerm.field, fieldAndTerm.term, termsEnum, maxTestOptions
					, maxIndexOptions, options, alwaysTestMax);
				// Sometimes save term state after pulling the enum:
				if (options.Contains(Option.TERM_STATE.ToString()) && !useTermState
					 && !savedTermState && Random().Next(5) == 1)
				{
					// Save away this TermState:
					termStates.Add(termsEnum.TermState);
					termStateTerms.Add(fieldAndTerm);
					useTermState = true;
				}
				// 10% of the time make sure you can pull another enum
				// from the same term:
				if (alwaysTestMax || Random().Next(10) == 7)
				{
					// Try same term again
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("TEST: try enum again on same term");
					}
					VerifyEnum(threadState, fieldAndTerm.field, fieldAndTerm.term, termsEnum, maxTestOptions
						, maxIndexOptions, options, alwaysTestMax);
				}
			}
		}

		

		// expected
		/// <summary>
		/// Indexes all fields/terms at the specified
		/// IndexOptions, and fully tests at that IndexOptions.
		/// </summary>
		/// <remarks>
		/// Indexes all fields/terms at the specified
		/// IndexOptions, and fully tests at that IndexOptions.
		/// </remarks>
		/// <exception cref="System.Exception"></exception>
		private void TestFull(FieldInfo.IndexOptions options, bool withPayloads)
		{
			FileInfo path = new FileInfo(Path.GetTempPath() + "testPostingsFormat.testExact");
		    FileStream tmpStream = path.Create();
		    Directory dir = NewFSDirectory(path);
		    // TODO test thread safety of buildIndex too
			FieldsProducer fieldsProducer = BuildIndex(dir, options, withPayloads, true);
			
			var allOptions = Enum.GetNames(typeof(FieldInfo.IndexOptions)).ToList();
			int maxIndexOption = allOptions.IndexOf(options.ToString());
			for (int i = 0; i <= maxIndexOption; i++)
			{
			    var optEnum = (FieldInfo.IndexOptions) Enum.Parse(typeof(Option), allOptions[i]);
			    TestTerms(fieldsProducer, Enum.GetNames(typeof(Option)).ToList(), optEnum, options, true);
				if (withPayloads)
				{
					// If we indexed w/ payloads, also test enums w/o accessing payloads:

					TestTerms(fieldsProducer, allOptions.Where(s=>!s.Equals(Option.PAYLOADS.ToString())).ToList(), optEnum, options, true);
				}
			}
			fieldsProducer.Dispose();
			dir.Dispose();
            tmpStream.Close();
			TestUtil.Rm(path);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestDocsOnly()
		{
			TestFull(FieldInfo.IndexOptions.DOCS_ONLY, false);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestDocsAndFreqs()
		{
			TestFull(FieldInfo.IndexOptions.DOCS_AND_FREQS, false);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestDocsAndFreqsAndPositions()
		{
			TestFull(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS, false);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestDocsAndFreqsAndPositionsAndPayloads()
		{
			TestFull(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS, true);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestDocsAndFreqsAndPositionsAndOffsets()
		{
			TestFull(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS, false);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestDocsAndFreqsAndPositionsAndOffsetsAndPayloads()
		{
			TestFull(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS, true);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestRandom()
		{
			int iters = 5;
			for (int iter = 0; iter < iters; iter++)
			{
                FileInfo path = new FileInfo(Path.GetTempPath() + "testPostingsFormat");
                FileStream tmpStream = path.Create();
				
				Directory dir = NewFSDirectory(path);
				bool indexPayloads = Random().NextBoolean();
				// TODO test thread safety of buildIndex too
				FieldsProducer fieldsProducer = BuildIndex(dir, FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
					, indexPayloads, false);
				
				// NOTE: you can also test "weaker" index options than
				// you indexed with:
                TestTerms(fieldsProducer, Enum.GetNames(typeof(Option)).ToList(), FieldInfo.IndexOptions
					.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS, FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
					, false);
				fieldsProducer.Dispose();
				fieldsProducer = null;
                tmpStream.Close();
				dir.Dispose();
				TestUtil.Rm(path);
			}
		}

		protected internal override void AddRandomFields(Document doc)
		{
		    var optionValues = Enum.GetNames(typeof(FieldInfo.IndexOptions));
		    foreach (var opts in optionValues)
			{
				string field = "f_" + opts;
				string pf = TestUtil.GetPostingsFormat(Codec.Default, field);
				if (opts.Equals(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS.ToString()) && doesntSupportOffsets.Contains(pf))
				{
					continue;
				}
				FieldType ft = new FieldType {IndexOptions = (FieldInfo.IndexOptions) Enum.Parse(typeof(FieldInfo.IndexOptions), opts), Indexed = true, OmitNorms = true};
			    ft.Freeze();
				int numFields = Random().Next(5);
				for (int j = 0; j < numFields; ++j)
				{
					doc.Add(new Field("f_" + opts, TestUtil.RandomSimpleString(Random(), 2), ft));
				}
			}
		}
	}
}
