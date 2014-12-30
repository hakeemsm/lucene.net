
using System;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Asserting.TestFramework;
using Lucene.Net.Codecs.Cranky.TestFramework;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.TestFramework;
using Lucene.Net.TestFramework.Analysis;
using Lucene.Net.TestFramework.Util;
using Lucene.Net.Util;

namespace Lucene.Net.Test.Index
{
	/// <summary>
	/// Causes a bunch of non-aborting and aborting exceptions and checks that
	/// no index corruption is ever created
	/// </summary>
	public class TestIndexWriterExceptions2 : LuceneTestCase
	{
		// just one thread, serial merge policy, hopefully debuggable
		/// <exception cref="System.Exception"></exception>
		public virtual void TestBasics()
		{
			// disable slow things: we don't rely upon sleeps here.
			Directory dir = NewDirectory();
			if (dir is MockDirectoryWrapper)
			{
				((MockDirectoryWrapper)dir).SetThrottling(MockDirectoryWrapper.Throttling.NEVER);
				((MockDirectoryWrapper)dir).SetUseSlowOpenClosers(false);
			}
			// log all exceptions we hit, in case we fail (for debugging)
			ByteArrayOutputStream exceptionLog = new ByteArrayOutputStream();
			TextWriter exceptionStream = new TextWriter(exceptionLog, true, "UTF-8");
			//PrintStream exceptionStream = System.out;
			// create lots of non-aborting exceptions with a broken analyzer
			long analyzerSeed = Random().NextLong();
			Analyzer analyzer = new _Analyzer_75(analyzerSeed);
			// TODO: can we turn this on? our filter is probably too evil
			// emit some payloads
			// create lots of aborting exceptions with a broken codec
			// we don't need a random codec, as we aren't trying to find bugs in the codec here.
			Codec inner = RANDOM_MULTIPLIER > 1 ? Codec.GetDefault() : new AssertingCodec();
			Codec codec = new CrankyCodec(inner, new Random(Random().NextLong()));
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			// just for now, try to keep this test reproducible
			conf.SetMergeScheduler(new SerialMergeScheduler());
			conf.SetCodec(codec);
			int numDocs = AtLeast(2000);
			IndexWriter iw = new IndexWriter(dir, conf);
			try
			{
				for (int i = 0; i < numDocs; i++)
				{
					// TODO: add crankyDocValuesFields, etc
					Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
						();
					doc.Add(NewStringField("id", i.ToString(), Field.Store.NO));
					doc.Add(new NumericDocValuesField("dv", i));
					doc.Add(new BinaryDocValuesField("dv2", new BytesRef(Extensions.ToString(
						i))));
					doc.Add(new SortedDocValuesField("dv3", new BytesRef(Extensions.ToString(
						i))));
					if (DefaultCodecSupportsSortedSet())
					{
						doc.Add(new SortedSetDocValuesField("dv4", new BytesRef(Extensions.ToString
							(i))));
						doc.Add(new SortedSetDocValuesField("dv4", new BytesRef(Extensions.ToString
							(i - 1))));
					}
					doc.Add(NewTextField("text1", TestUtil.RandomAnalysisString(Random(), 20, true), 
						Field.Store.NO));
					// ensure we store something
					doc.Add(new StoredField("stored1", "foo"));
					doc.Add(new StoredField("stored1", "bar"));
					// ensure we get some payloads
					doc.Add(NewTextField("text_payloads", TestUtil.RandomAnalysisString(Random(), 6, 
						true), Field.Store.NO));
					// ensure we get some vectors
					FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
					ft.StoreTermVectors = true;
					doc.Add(NewField("text_vectors", TestUtil.RandomAnalysisString(Random(), 6, true)
						, ft));
					if (Random().Next(10) > 0)
					{
						// single doc
						try
						{
							iw.AddDocument(doc);
							// we made it, sometimes delete our doc, or update a dv
							int thingToDo = Random().Next(4);
							if (thingToDo == 0)
							{
								iw.DeleteDocuments(new Term("id", i.ToString()));
							}
							else
							{
								if (thingToDo == 1 && DefaultCodecSupportsFieldUpdates())
								{
									iw.UpdateNumericDocValue(new Term("id", i.ToString()), "dv", i 
										+ 1L);
								}
								else
								{
									if (thingToDo == 2 && DefaultCodecSupportsFieldUpdates())
									{
										iw.UpdateBinaryDocValue(new Term("id", i.ToString()), "dv2", new 
											BytesRef(Extensions.ToString(i + 1)));
									}
								}
							}
						}
						catch (Exception e)
						{
							if (e.Message != null && e.Message.StartsWith("Fake IOException"))
							{
								exceptionStream.WriteLine("\nTEST: got expected fake exc:" + e.Message);
								Runtime.PrintStackTrace(e, exceptionStream);
							}
							else
							{
								Rethrow.Rethrow(e);
							}
						}
					}
					else
					{
						// block docs
						Lucene.Net.Documents.Document doc2 = new Lucene.Net.Documents.Document
							();
						doc2.Add(NewStringField("id", Extensions.ToString(-i), Field.Store.NO));
						doc2.Add(NewTextField("text1", TestUtil.RandomAnalysisString(Random(), 20, true), 
							Field.Store.NO));
						doc2.Add(new StoredField("stored1", "foo"));
						doc2.Add(new StoredField("stored1", "bar"));
						doc2.Add(NewField("text_vectors", TestUtil.RandomAnalysisString(Random(), 6, true
							), ft));
						try
						{
							iw.AddDocuments(Arrays.AsList(doc, doc2).AsIterable());
							// we made it, sometimes delete our docs
							if (Random().NextBoolean())
							{
								iw.DeleteDocuments(new Term("id", i.ToString()), new Term("id", 
									Extensions.ToString(-i)));
							}
						}
						catch (Exception e)
						{
							if (e.Message != null && e.Message.StartsWith("Fake IOException"))
							{
								exceptionStream.WriteLine("\nTEST: got expected fake exc:" + e.Message);
								Runtime.PrintStackTrace(e, exceptionStream);
							}
							else
							{
								Rethrow.Rethrow(e);
							}
						}
					}
					if (Random().Next(10) == 0)
					{
						// trigger flush:
						try
						{
							if (Random().NextBoolean())
							{
								DirectoryReader ir = null;
								try
								{
									ir = DirectoryReader.Open(iw, Random().NextBoolean());
									TestUtil.CheckReader(ir);
								}
								finally
								{
									IOUtils.CloseWhileHandlingException(ir);
								}
							}
							else
							{
								iw.Commit();
							}
							if (DirectoryReader.IndexExists(dir))
							{
								TestUtil.CheckIndex(dir);
							}
						}
						catch (Exception e)
						{
							if (e.Message != null && e.Message.StartsWith("Fake IOException"))
							{
								exceptionStream.WriteLine("\nTEST: got expected fake exc:" + e.Message);
								Runtime.PrintStackTrace(e, exceptionStream);
							}
							else
							{
								Rethrow.Rethrow(e);
							}
						}
					}
				}
				try
				{
					iw.Dispose();
				}
				catch (Exception e)
				{
					if (e.Message != null && e.Message.StartsWith("Fake IOException"))
					{
						exceptionStream.WriteLine("\nTEST: got expected fake exc:" + e.Message);
						Runtime.PrintStackTrace(e, exceptionStream);
						try
						{
							iw.Rollback();
						}
						catch
						{
						}
					}
					else
					{
						Rethrow.Rethrow(e);
					}
				}
				dir.Dispose();
			}
			catch (Exception t)
			{
				System.Console.Out.WriteLine("Unexpected exception: dumping fake-exception-log:..."
					);
				exceptionStream.Flush();
				System.Console.Out.WriteLine(exceptionLog.ToString("UTF-8"));
				System.Console.Out.Flush();
				Rethrow.Rethrow(t);
			}
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST PASSED: dumping fake-exception-log:...");
				System.Console.Out.WriteLine(exceptionLog.ToString("UTF-8"));
			}
		}

		private sealed class _Analyzer_75 : Analyzer
		{
			public _Analyzer_75(long analyzerSeed)
			{
				this.analyzerSeed = analyzerSeed;
			}

			protected override Analyzer.TokenStreamComponents CreateComponents(string fieldName
				, StreamReader reader)
			{
				MockTokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.SIMPLE, false);
				tokenizer.SetEnableChecks(false);
				TokenStream stream = tokenizer;
				if (fieldName.Contains("payloads"))
				{
					stream = new MockVariableLengthPayloadFilter(new Random(analyzerSeed), stream);
				}
				stream = new CrankyTokenFilter(stream, new Random(analyzerSeed));
				return new Analyzer.TokenStreamComponents(tokenizer, stream);
			}

			private readonly long analyzerSeed;
		}
	}
}
