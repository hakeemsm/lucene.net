/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	/// <summary>
	/// Causes a bunch of fake OOM and checks that no other exceptions are delivered instead,
	/// no index corruption is ever created.
	/// </summary>
	/// <remarks>
	/// Causes a bunch of fake OOM and checks that no other exceptions are delivered instead,
	/// no index corruption is ever created.
	/// </remarks>
	public class TestIndexWriterOutOfMemory : LuceneTestCase
	{
		// just one thread, serial merge policy, hopefully debuggable
		/// <exception cref="System.Exception"></exception>
		public virtual void TestBasics()
		{
			// log all exceptions we hit, in case we fail (for debugging)
			ByteArrayOutputStream exceptionLog = new ByteArrayOutputStream();
			TextWriter exceptionStream = new TextWriter(exceptionLog, true, "UTF-8");
			//PrintStream exceptionStream = System.out;
			long analyzerSeed = Random().NextLong();
			Analyzer analyzer = new _Analyzer_64(analyzerSeed);
			// we are gonna make it angry
			// emit some payloads
			MockDirectoryWrapper dir = null;
			int numIterations = TEST_NIGHTLY ? AtLeast(500) : AtLeast(20);
			for (int iter = 0; iter < numIterations; iter++)
			{
				try
				{
					// close from last run
					if (dir != null)
					{
						dir.Close();
					}
					// disable slow things: we don't rely upon sleeps here.
					dir = NewMockDirectory();
					dir.SetThrottling(MockDirectoryWrapper.Throttling.NEVER);
					dir.SetUseSlowOpenClosers(false);
					IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
					// just for now, try to keep this test reproducible
					conf.SetMergeScheduler(new SerialMergeScheduler());
					// test never makes it this far...
					int numDocs = AtLeast(2000);
					IndexWriter iw = new IndexWriter(dir, conf);
					Random r = new Random(Random().NextLong());
					dir.FailOn(new _Failure_104(r));
					// don't make life difficult though
					for (int i = 0; i < numDocs; i++)
					{
						Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
							();
						doc.Add(NewStringField("id", Sharpen.Extensions.ToString(i), Field.Store.NO));
						doc.Add(new NumericDocValuesField("dv", i));
						doc.Add(new BinaryDocValuesField("dv2", new BytesRef(Sharpen.Extensions.ToString(
							i))));
						doc.Add(new SortedDocValuesField("dv3", new BytesRef(Sharpen.Extensions.ToString(
							i))));
						if (DefaultCodecSupportsSortedSet())
						{
							doc.Add(new SortedSetDocValuesField("dv4", new BytesRef(Sharpen.Extensions.ToString
								(i))));
							doc.Add(new SortedSetDocValuesField("dv4", new BytesRef(Sharpen.Extensions.ToString
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
						ft.SetStoreTermVectors(true);
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
									iw.DeleteDocuments(new Term("id", Sharpen.Extensions.ToString(i)));
								}
								else
								{
									if (thingToDo == 1 && DefaultCodecSupportsFieldUpdates())
									{
										iw.UpdateNumericDocValue(new Term("id", Sharpen.Extensions.ToString(i)), "dv", i 
											+ 1L);
									}
									else
									{
										if (thingToDo == 2 && DefaultCodecSupportsFieldUpdates())
										{
											iw.UpdateBinaryDocValue(new Term("id", Sharpen.Extensions.ToString(i)), "dv2", new 
												BytesRef(Sharpen.Extensions.ToString(i + 1)));
										}
									}
								}
							}
							catch (OutOfMemoryException e)
							{
								if (e.Message != null && e.Message.StartsWith("Fake OutOfMemoryError"))
								{
									exceptionStream.WriteLine("\nTEST: got expected fake exc:" + e.Message);
									Sharpen.Runtime.PrintStackTrace(e, exceptionStream);
									try
									{
										iw.Rollback();
									}
									catch
									{
									}
									goto STARTOVER_continue;
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
							Lucene.Net.Document.Document doc2 = new Lucene.Net.Document.Document
								();
							doc2.Add(NewStringField("id", Sharpen.Extensions.ToString(-i), Field.Store.NO));
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
									iw.DeleteDocuments(new Term("id", Sharpen.Extensions.ToString(i)), new Term("id", 
										Sharpen.Extensions.ToString(-i)));
								}
							}
							catch (OutOfMemoryException e)
							{
								if (e.Message != null && e.Message.StartsWith("Fake OutOfMemoryError"))
								{
									exceptionStream.WriteLine("\nTEST: got expected fake exc:" + e.Message);
									Sharpen.Runtime.PrintStackTrace(e, exceptionStream);
								}
								else
								{
									Rethrow.Rethrow(e);
								}
								try
								{
									iw.Rollback();
								}
								catch
								{
								}
								goto STARTOVER_continue;
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
							catch (OutOfMemoryException e)
							{
								if (e.Message != null && e.Message.StartsWith("Fake OutOfMemoryError"))
								{
									exceptionStream.WriteLine("\nTEST: got expected fake exc:" + e.Message);
									Sharpen.Runtime.PrintStackTrace(e, exceptionStream);
								}
								else
								{
									Rethrow.Rethrow(e);
								}
								try
								{
									iw.Rollback();
								}
								catch
								{
								}
								goto STARTOVER_continue;
							}
						}
					}
					try
					{
						iw.Close();
					}
					catch (OutOfMemoryException e)
					{
						if (e.Message != null && e.Message.StartsWith("Fake OutOfMemoryError"))
						{
							exceptionStream.WriteLine("\nTEST: got expected fake exc:" + e.Message);
							Sharpen.Runtime.PrintStackTrace(e, exceptionStream);
							try
							{
								iw.Rollback();
							}
							catch
							{
							}
							goto STARTOVER_continue;
						}
						else
						{
							Rethrow.Rethrow(e);
						}
					}
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
STARTOVER_continue: ;
			}
STARTOVER_break: ;
			dir.Close();
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST PASSED: dumping fake-exception-log:...");
				System.Console.Out.WriteLine(exceptionLog.ToString("UTF-8"));
			}
		}

		private sealed class _Analyzer_64 : Analyzer
		{
			public _Analyzer_64(long analyzerSeed)
			{
				this.analyzerSeed = analyzerSeed;
			}

			protected override Analyzer.TokenStreamComponents CreateComponents(string fieldName
				, StreamReader reader)
			{
				MockTokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false
					);
				tokenizer.SetEnableChecks(false);
				TokenStream stream = tokenizer;
				if (fieldName.Contains("payloads"))
				{
					stream = new MockVariableLengthPayloadFilter(new Random(analyzerSeed), stream);
				}
				return new Analyzer.TokenStreamComponents(tokenizer, stream);
			}

			private readonly long analyzerSeed;
		}

		private sealed class _Failure_104 : MockDirectoryWrapper.Failure
		{
			public _Failure_104(Random r)
			{
				this.r = r;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Eval(MockDirectoryWrapper dir)
			{
				Exception e = new Exception();
				StackTraceElement[] stack = e.GetStackTrace();
				bool ok = false;
				for (int i = 0; i < stack.Length; i++)
				{
					if (stack[i].GetClassName().Equals(typeof(IndexWriter).FullName))
					{
						ok = true;
						if (stack[i].GetMethodName().Equals("rollback"))
						{
							return;
						}
					}
				}
				if (ok && r.Next(3000) == 0)
				{
					throw new OutOfMemoryException("Fake OutOfMemoryError");
				}
			}

			private readonly Random r;
		}
	}
}
