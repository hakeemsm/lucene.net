using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.TestFramework;
using Lucene.Net.TestFramework.Util;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Index
{
	/// <summary>
	/// Causes a bunch of fake OOM and checks that no other exceptions are delivered instead,
	/// no index corruption is ever created.
	/// </summary>
	/// <remarks>
	/// Causes a bunch of fake OOM and checks that no other exceptions are delivered instead,
	/// no index corruption is ever created.
	/// </remarks>
	[TestFixture]
    public class TestIndexWriterOutOfMemory : LuceneTestCase
	{
		// just one thread, serial merge policy, hopefully debuggable
        // TODO: fix the gotos
		[Test]
		public virtual void TestBasics()
		{
			// log all exceptions we hit, in case we fail (for debugging)
			var exceptionLog = new MemoryStream();
			var exceptionStream = new StreamWriter(exceptionLog, Encoding.UTF8);
			//PrintStream exceptionStream = System.out;
			int analyzerSeed = Random().NextInt(0,int.MaxValue);
			Analyzer analyzer = new AnonymousAnalyzer(analyzerSeed);
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
						dir.Dispose();
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
					Random r = new Random(Random().NextInt(0,int.MaxValue));
					dir.FailOn(new AnonymousFailure(r));
					// don't make life difficult though
					for (int i = 0; i < numDocs; i++)
					{
						Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
						{
						    NewStringField("id", i.ToString(), Field.Store.NO),
						    new NumericDocValuesField("dv", i),
						    new BinaryDocValuesField("dv2", new BytesRef(i.ToString())),
						    new SortedDocValuesField("dv3", new BytesRef(i.ToString()))
						};
					    if (DefaultCodecSupportsSortedSet())
						{
							doc.Add(new SortedSetDocValuesField("dv4", new BytesRef(i.ToString())));
							doc.Add(new SortedSetDocValuesField("dv4", new BytesRef((i-1).ToString())));
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
												BytesRef((i+1).ToString()));
										}
									}
								}
							}
							catch (OutOfMemoryException e)
							{
							    if (e.Message != null && e.Message.StartsWith("Fake OutOfMemoryError"))
								{
									exceptionStream.WriteLine("\nTEST: got expected fake exc:" + e.Message);
									e.printStackTrace();
									try
									{
										iw.Rollback();
									}
									catch
									{
									}
									goto STARTOVER_continue;
								}
							    throw;
							}
						}
						else
						{
							// block docs
							Lucene.Net.Documents.Document doc2 = new Lucene.Net.Documents.Document
							{
							    NewStringField("id", (-i).ToString(), Field.Store.NO),
							    NewTextField("text1", TestUtil.RandomAnalysisString(Random(), 20, true),
							        Field.Store.NO),
							    new StoredField("stored1", "foo"),
							    new StoredField("stored1", "bar"),
							    NewField("text_vectors", TestUtil.RandomAnalysisString(Random(), 6, true
							        ), ft)
							};
						    try
							{
								iw.AddDocuments(Arrays.AsList(doc, doc2).AsEnumerable());
								// we made it, sometimes delete our docs
								if (Random().NextBoolean())
								{
									iw.DeleteDocuments(new Term("id", i.ToString()), new Term("id", 
										(-i).ToString()));
								}
							}
							catch (OutOfMemoryException e)
							{
								if (e.Message != null && e.Message.StartsWith("Fake OutOfMemoryError"))
								{
									exceptionStream.WriteLine("\nTEST: got expected fake exc:" + e.Message);
									e.printStackTrace();
								}
								else
								{
								    throw;
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
										IOUtils.CloseWhileHandlingException((IDisposable)ir);
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
									e.printStackTrace();
								}
								else
								{
								    throw;
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
						iw.Dispose();
					}
					catch (OutOfMemoryException e)
					{
					    if (e.Message != null && e.Message.StartsWith("Fake OutOfMemoryError"))
						{
							exceptionStream.WriteLine("\nTEST: got expected fake exc:" + e.Message);
							e.printStackTrace();
							try
							{
								iw.Rollback();
							}
							catch
							{
							}
							goto STARTOVER_continue;
						}
					    throw;
					}
				}
				catch (Exception t)
				{
					System.Console.Out.WriteLine("Unexpected exception: dumping fake-exception-log:..."
						);
					exceptionStream.Flush();
					System.Console.Out.WriteLine(exceptionLog.ToString());
					System.Console.Out.Flush();
				    throw;
				}
STARTOVER_continue: ;
			}
STARTOVER_break: ;
			dir.Dispose();
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST PASSED: dumping fake-exception-log:...");
				System.Console.Out.WriteLine(exceptionLog.ToString());
			}
		}

		private sealed class AnonymousAnalyzer : Analyzer
		{
			public AnonymousAnalyzer(int analyzerSeed)
			{
				this.analyzerSeed = analyzerSeed;
			}

		    public override Analyzer.TokenStreamComponents CreateComponents(string fieldName
				, TextReader reader)
			{
				MockTokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false
					);
				tokenizer.setEnableChecks(false);
				TokenStream stream = tokenizer;
				if (fieldName.Contains("payloads"))
				{
					stream = new MockVariableLengthPayloadFilter(new Random(analyzerSeed), stream);
				}
				return new Analyzer.TokenStreamComponents(tokenizer, stream);
			}

			private readonly int analyzerSeed;
		}

		private sealed class AnonymousFailure : MockDirectoryWrapper.Failure
		{
			public AnonymousFailure(Random r)
			{
				this.r = r;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Eval(MockDirectoryWrapper dir)
			{
				Exception e = new Exception();
				var stack = new StackTrace(e).GetFrames();
				bool ok = false;
				for (int i = 0; i < stack.Length; i++)
				{
					if (stack[i].GetMethod().DeclaringType.FullName.Equals(typeof(IndexWriter).FullName))
					{
						ok = true;
						if (stack[i].GetMethod().Name.Equals("rollback"))
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
