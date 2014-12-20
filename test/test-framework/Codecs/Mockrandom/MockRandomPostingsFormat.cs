/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Org.Apache.Lucene.Codecs;
using Org.Apache.Lucene.Codecs.Blockterms;
using Org.Apache.Lucene.Codecs.Lucene41;
using Org.Apache.Lucene.Codecs.Memory;
using Org.Apache.Lucene.Codecs.Mockintblock;
using Org.Apache.Lucene.Codecs.Mockrandom;
using Org.Apache.Lucene.Codecs.Mocksep;
using Org.Apache.Lucene.Codecs.Pulsing;
using Org.Apache.Lucene.Codecs.Sep;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Store;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Codecs.Mockrandom
{
	/// <summary>Randomly combines terms index impl w/ postings impls.</summary>
	/// <remarks>Randomly combines terms index impl w/ postings impls.</remarks>
	public sealed class MockRandomPostingsFormat : PostingsFormat
	{
		private readonly Random seedRandom;

		private readonly string SEED_EXT = "sd";

		public MockRandomPostingsFormat() : this(null)
		{
		}

		public MockRandomPostingsFormat(Random random) : base("MockRandom")
		{
			// This ctor should *only* be used at read-time: get NPE if you use it!
			if (random == null)
			{
				this.seedRandom = new _Random_85(0L);
			}
			else
			{
				this.seedRandom = new Random(random.NextLong());
			}
		}

		private sealed class _Random_85 : Random
		{
			public _Random_85(long baseArg1) : base(baseArg1)
			{
			}

			protected override int Next(int arg0)
			{
				throw new InvalidOperationException("Please use MockRandomPostingsFormat(Random)"
					);
			}
		}

		private class MockIntStreamFactory : IntStreamFactory
		{
			private readonly int salt;

			private readonly IList<IntStreamFactory> delegates = new AList<IntStreamFactory>(
				);

			public MockIntStreamFactory(Random random)
			{
				// Chooses random IntStreamFactory depending on file's extension
				salt = random.Next();
				delegates.AddItem(new MockSingleIntFactory());
				int blockSize = TestUtil.NextInt(random, 1, 2000);
				delegates.AddItem(new MockFixedIntBlockPostingsFormat.MockIntFactory(blockSize));
				int baseBlockSize = TestUtil.NextInt(random, 1, 127);
				delegates.AddItem(new MockVariableIntBlockPostingsFormat.MockIntFactory(baseBlockSize
					));
			}

			// TODO: others
			private static string GetExtension(string fileName)
			{
				int idx = fileName.IndexOf('.');
				//HM:revisit 
				//assert idx != -1;
				return Sharpen.Runtime.Substring(fileName, idx);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override IntIndexInput OpenInput(Directory dir, string fileName, IOContext
				 context)
			{
				// Must only use extension, because IW.addIndexes can
				// rename segment!
				IntStreamFactory f = delegates[(Math.Abs(salt ^ GetExtension(fileName).GetHashCode
					())) % delegates.Count];
				if (LuceneTestCase.VERBOSE)
				{
					System.Console.Out.WriteLine("MockRandomCodec: read using int factory " + f + " from fileName="
						 + fileName);
				}
				return f.OpenInput(dir, fileName, context);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override IntIndexOutput CreateOutput(Directory dir, string fileName, IOContext
				 context)
			{
				IntStreamFactory f = delegates[(Math.Abs(salt ^ GetExtension(fileName).GetHashCode
					())) % delegates.Count];
				if (LuceneTestCase.VERBOSE)
				{
					System.Console.Out.WriteLine("MockRandomCodec: write using int factory " + f + " to fileName="
						 + fileName);
				}
				return f.CreateOutput(dir, fileName, context);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
		{
			int minSkipInterval;
			if (state.segmentInfo.GetDocCount() > 1000000)
			{
				// Test2BPostings can OOME otherwise:
				minSkipInterval = 3;
			}
			else
			{
				minSkipInterval = 2;
			}
			// we pull this before the seed intentionally: because its not consumed at runtime
			// (the skipInterval is written into postings header)
			int skipInterval = TestUtil.NextInt(seedRandom, minSkipInterval, 10);
			if (LuceneTestCase.VERBOSE)
			{
				System.Console.Out.WriteLine("MockRandomCodec: skipInterval=" + skipInterval);
			}
			long seed = seedRandom.NextLong();
			if (LuceneTestCase.VERBOSE)
			{
				System.Console.Out.WriteLine("MockRandomCodec: writing to seg=" + state.segmentInfo
					.name + " formatID=" + state.segmentSuffix + " seed=" + seed);
			}
			string seedFileName = IndexFileNames.SegmentFileName(state.segmentInfo.name, state
				.segmentSuffix, SEED_EXT);
			IndexOutput @out = state.directory.CreateOutput(seedFileName, state.context);
			try
			{
				@out.WriteLong(seed);
			}
			finally
			{
				@out.Close();
			}
			Random random = new Random(seed);
			random.Next();
			// consume a random for buffersize
			PostingsWriterBase postingsWriter;
			if (random.NextBoolean())
			{
				postingsWriter = new SepPostingsWriter(state, new MockRandomPostingsFormat.MockIntStreamFactory
					(random), skipInterval);
			}
			else
			{
				if (LuceneTestCase.VERBOSE)
				{
					System.Console.Out.WriteLine("MockRandomCodec: writing Standard postings");
				}
				// TODO: randomize variables like acceptibleOverHead?!
				postingsWriter = new Lucene41PostingsWriter(state, skipInterval);
			}
			if (random.NextBoolean())
			{
				int totTFCutoff = TestUtil.NextInt(random, 1, 20);
				if (LuceneTestCase.VERBOSE)
				{
					System.Console.Out.WriteLine("MockRandomCodec: writing pulsing postings with totTFCutoff="
						 + totTFCutoff);
				}
				postingsWriter = new PulsingPostingsWriter(state, totTFCutoff, postingsWriter);
			}
			FieldsConsumer fields;
			int t1 = random.Next(4);
			if (t1 == 0)
			{
				bool success = false;
				try
				{
					fields = new FSTTermsWriter(state, postingsWriter);
					success = true;
				}
				finally
				{
					if (!success)
					{
						postingsWriter.Close();
					}
				}
			}
			else
			{
				if (t1 == 1)
				{
					bool success = false;
					try
					{
						fields = new FSTOrdTermsWriter(state, postingsWriter);
						success = true;
					}
					finally
					{
						if (!success)
						{
							postingsWriter.Close();
						}
					}
				}
				else
				{
					if (t1 == 2)
					{
						// Use BlockTree terms dict
						if (LuceneTestCase.VERBOSE)
						{
							System.Console.Out.WriteLine("MockRandomCodec: writing BlockTree terms dict");
						}
						// TODO: would be nice to allow 1 but this is very
						// slow to write
						int minTermsInBlock = TestUtil.NextInt(random, 2, 100);
						int maxTermsInBlock = Math.Max(2, (minTermsInBlock - 1) * 2 + random.Next(100));
						bool success = false;
						try
						{
							fields = new BlockTreeTermsWriter(state, postingsWriter, minTermsInBlock, maxTermsInBlock
								);
							success = true;
						}
						finally
						{
							if (!success)
							{
								postingsWriter.Close();
							}
						}
					}
					else
					{
						if (LuceneTestCase.VERBOSE)
						{
							System.Console.Out.WriteLine("MockRandomCodec: writing Block terms dict");
						}
						bool success = false;
						TermsIndexWriterBase indexWriter;
						try
						{
							if (random.NextBoolean())
							{
								state.termIndexInterval = TestUtil.NextInt(random, 1, 100);
								if (LuceneTestCase.VERBOSE)
								{
									System.Console.Out.WriteLine("MockRandomCodec: fixed-gap terms index (tii=" + state
										.termIndexInterval + ")");
								}
								indexWriter = new FixedGapTermsIndexWriter(state);
							}
							else
							{
								VariableGapTermsIndexWriter.IndexTermSelector selector;
								int n2 = random.Next(3);
								if (n2 == 0)
								{
									int tii = TestUtil.NextInt(random, 1, 100);
									selector = new VariableGapTermsIndexWriter.EveryNTermSelector(tii);
									if (LuceneTestCase.VERBOSE)
									{
										System.Console.Out.WriteLine("MockRandomCodec: variable-gap terms index (tii=" + 
											tii + ")");
									}
								}
								else
								{
									if (n2 == 1)
									{
										int docFreqThresh = TestUtil.NextInt(random, 2, 100);
										int tii = TestUtil.NextInt(random, 1, 100);
										selector = new VariableGapTermsIndexWriter.EveryNOrDocFreqTermSelector(docFreqThresh
											, tii);
									}
									else
									{
										long seed2 = random.NextLong();
										int gap = TestUtil.NextInt(random, 2, 40);
										if (LuceneTestCase.VERBOSE)
										{
											System.Console.Out.WriteLine("MockRandomCodec: random-gap terms index (max gap=" 
												+ gap + ")");
										}
										selector = new _IndexTermSelector_274(seed2, gap);
									}
								}
								indexWriter = new VariableGapTermsIndexWriter(state, selector);
							}
							success = true;
						}
						finally
						{
							if (!success)
							{
								postingsWriter.Close();
							}
						}
						success = false;
						try
						{
							fields = new BlockTermsWriter(indexWriter, state, postingsWriter);
							success = true;
						}
						finally
						{
							if (!success)
							{
								try
								{
									postingsWriter.Close();
								}
								finally
								{
									indexWriter.Close();
								}
							}
						}
					}
				}
			}
			return fields;
		}

		private sealed class _IndexTermSelector_274 : VariableGapTermsIndexWriter.IndexTermSelector
		{
			public _IndexTermSelector_274(long seed2, int gap)
			{
				this.seed2 = seed2;
				this.gap = gap;
				this.rand = new Random(seed2);
			}

			internal readonly Random rand;

			public override bool IsIndexTerm(BytesRef term, TermStats stats)
			{
				return this.rand.Next(gap) == gap / 2;
			}

			public override void NewField(FieldInfo fieldInfo)
			{
			}

			private readonly long seed2;

			private readonly int gap;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FieldsProducer FieldsProducer(SegmentReadState state)
		{
			string seedFileName = IndexFileNames.SegmentFileName(state.segmentInfo.name, state
				.segmentSuffix, SEED_EXT);
			IndexInput @in = state.directory.OpenInput(seedFileName, state.context);
			long seed = @in.ReadLong();
			if (LuceneTestCase.VERBOSE)
			{
				System.Console.Out.WriteLine("MockRandomCodec: reading from seg=" + state.segmentInfo
					.name + " formatID=" + state.segmentSuffix + " seed=" + seed);
			}
			@in.Close();
			Random random = new Random(seed);
			int readBufferSize = TestUtil.NextInt(random, 1, 4096);
			if (LuceneTestCase.VERBOSE)
			{
				System.Console.Out.WriteLine("MockRandomCodec: readBufferSize=" + readBufferSize);
			}
			PostingsReaderBase postingsReader;
			if (random.NextBoolean())
			{
				if (LuceneTestCase.VERBOSE)
				{
					System.Console.Out.WriteLine("MockRandomCodec: reading Sep postings");
				}
				postingsReader = new SepPostingsReader(state.directory, state.fieldInfos, state.segmentInfo
					, state.context, new MockRandomPostingsFormat.MockIntStreamFactory(random), state
					.segmentSuffix);
			}
			else
			{
				if (LuceneTestCase.VERBOSE)
				{
					System.Console.Out.WriteLine("MockRandomCodec: reading Standard postings");
				}
				postingsReader = new Lucene41PostingsReader(state.directory, state.fieldInfos, state
					.segmentInfo, state.context, state.segmentSuffix);
			}
			if (random.NextBoolean())
			{
				int totTFCutoff = TestUtil.NextInt(random, 1, 20);
				if (LuceneTestCase.VERBOSE)
				{
					System.Console.Out.WriteLine("MockRandomCodec: reading pulsing postings with totTFCutoff="
						 + totTFCutoff);
				}
				postingsReader = new PulsingPostingsReader(state, postingsReader);
			}
			FieldsProducer fields;
			int t1 = random.Next(4);
			if (t1 == 0)
			{
				bool success = false;
				try
				{
					fields = new FSTTermsReader(state, postingsReader);
					success = true;
				}
				finally
				{
					if (!success)
					{
						postingsReader.Close();
					}
				}
			}
			else
			{
				if (t1 == 1)
				{
					bool success = false;
					try
					{
						fields = new FSTOrdTermsReader(state, postingsReader);
						success = true;
					}
					finally
					{
						if (!success)
						{
							postingsReader.Close();
						}
					}
				}
				else
				{
					if (t1 == 2)
					{
						// Use BlockTree terms dict
						if (LuceneTestCase.VERBOSE)
						{
							System.Console.Out.WriteLine("MockRandomCodec: reading BlockTree terms dict");
						}
						bool success = false;
						try
						{
							fields = new BlockTreeTermsReader(state.directory, state.fieldInfos, state.segmentInfo
								, postingsReader, state.context, state.segmentSuffix, state.termsIndexDivisor);
							success = true;
						}
						finally
						{
							if (!success)
							{
								postingsReader.Close();
							}
						}
					}
					else
					{
						if (LuceneTestCase.VERBOSE)
						{
							System.Console.Out.WriteLine("MockRandomCodec: reading Block terms dict");
						}
						TermsIndexReaderBase indexReader;
						bool success = false;
						try
						{
							bool doFixedGap = random.NextBoolean();
							// randomness diverges from writer, here:
							if (state.termsIndexDivisor != -1)
							{
								state.termsIndexDivisor = TestUtil.NextInt(random, 1, 10);
							}
							if (doFixedGap)
							{
								// if termsIndexDivisor is set to -1, we should not touch it. It means a
								// test explicitly instructed not to load the terms index.
								if (LuceneTestCase.VERBOSE)
								{
									System.Console.Out.WriteLine("MockRandomCodec: fixed-gap terms index (divisor=" +
										 state.termsIndexDivisor + ")");
								}
								indexReader = new FixedGapTermsIndexReader(state.directory, state.fieldInfos, state
									.segmentInfo.name, state.termsIndexDivisor, BytesRef.GetUTF8SortedAsUnicodeComparator
									(), state.segmentSuffix, state.context);
							}
							else
							{
								int n2 = random.Next(3);
								if (n2 == 1)
								{
									random.Next();
								}
								else
								{
									if (n2 == 2)
									{
										random.NextLong();
									}
								}
								if (LuceneTestCase.VERBOSE)
								{
									System.Console.Out.WriteLine("MockRandomCodec: variable-gap terms index (divisor="
										 + state.termsIndexDivisor + ")");
								}
								indexReader = new VariableGapTermsIndexReader(state.directory, state.fieldInfos, 
									state.segmentInfo.name, state.termsIndexDivisor, state.segmentSuffix, state.context
									);
							}
							success = true;
						}
						finally
						{
							if (!success)
							{
								postingsReader.Close();
							}
						}
						success = false;
						try
						{
							fields = new BlockTermsReader(indexReader, state.directory, state.fieldInfos, state
								.segmentInfo, postingsReader, state.context, state.segmentSuffix);
							success = true;
						}
						finally
						{
							if (!success)
							{
								try
								{
									postingsReader.Close();
								}
								finally
								{
									indexReader.Close();
								}
							}
						}
					}
				}
			}
			return fields;
		}
	}
}
