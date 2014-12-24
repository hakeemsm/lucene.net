/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	public class TestCrashCausesCorruptIndex : LuceneTestCase
	{
		internal FilePath path;

		/// <summary>LUCENE-3627: This test fails.</summary>
		/// <remarks>LUCENE-3627: This test fails.</remarks>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestCrashCorruptsIndexing()
		{
			path = CreateTempDir("testCrashCorruptsIndexing");
			IndexAndCrashOnCreateOutputSegments2();
			SearchForFleas(2);
			IndexAfterRestart();
			SearchForFleas(3);
		}

		/// <summary>index 1 document and commit.</summary>
		/// <remarks>
		/// index 1 document and commit.
		/// prepare for crashing.
		/// index 1 more document, and upon commit, creation of segments_2 will crash.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		private void IndexAndCrashOnCreateOutputSegments2()
		{
			Directory realDirectory = FSDirectory.Open(path);
			TestCrashCausesCorruptIndex.CrashAfterCreateOutput crashAfterCreateOutput = new TestCrashCausesCorruptIndex.CrashAfterCreateOutput
				(realDirectory);
			// NOTE: cannot use RandomIndexWriter because it
			// sometimes commits:
			IndexWriter indexWriter = new IndexWriter(crashAfterCreateOutput, NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
			indexWriter.AddDocument(GetDocument());
			// writes segments_1:
			indexWriter.Commit();
			crashAfterCreateOutput.SetCrashAfterCreateOutput("segments_2");
			indexWriter.AddDocument(GetDocument());
			try
			{
				// tries to write segments_2 but hits fake exc:
				indexWriter.Commit();
				Fail("should have hit CrashingException");
			}
			catch (TestCrashCausesCorruptIndex.CrashingException)
			{
			}
			// expected
			// writes segments_3
			indexWriter.Close();
			IsFalse(SlowFileExists(realDirectory, "segments_2"));
			crashAfterCreateOutput.Close();
		}

		/// <summary>Attempts to index another 1 document.</summary>
		/// <remarks>Attempts to index another 1 document.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		private void IndexAfterRestart()
		{
			Directory realDirectory = NewFSDirectory(path);
			// LUCENE-3627 (before the fix): this line fails because
			// it doesn't know what to do with the created but empty
			// segments_2 file
			IndexWriter indexWriter = new IndexWriter(realDirectory, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			// currently the test fails above.
			// however, to test the fix, the following lines should pass as well.
			indexWriter.AddDocument(GetDocument());
			indexWriter.Close();
			IsFalse(SlowFileExists(realDirectory, "segments_2"));
			realDirectory.Close();
		}

		/// <summary>Run an example search.</summary>
		/// <remarks>Run an example search.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		private void SearchForFleas(int expectedTotalHits)
		{
			Directory realDirectory = NewFSDirectory(path);
			IndexReader indexReader = DirectoryReader.Open(realDirectory);
			IndexSearcher indexSearcher = NewSearcher(indexReader);
			TopDocs topDocs = indexSearcher.Search(new TermQuery(new Term(TEXT_FIELD, "fleas"
				)), 10);
			IsNotNull(topDocs);
			AreEqual(expectedTotalHits, topDocs.TotalHits);
			indexReader.Close();
			realDirectory.Close();
		}

		private static readonly string TEXT_FIELD = "text";

		/// <summary>Gets a document with content "my dog has fleas".</summary>
		/// <remarks>Gets a document with content "my dog has fleas".</remarks>
		private Lucene.Net.Documents.Document GetDocument()
		{
			Lucene.Net.Documents.Document document = new Lucene.Net.Documents.Document
				();
			document.Add(NewTextField(TEXT_FIELD, "my dog has fleas", Field.Store.NO));
			return document;
		}

		/// <summary>
		/// The marker RuntimeException that we use in lieu of an
		/// actual machine crash.
		/// </summary>
		/// <remarks>
		/// The marker RuntimeException that we use in lieu of an
		/// actual machine crash.
		/// </remarks>
		[System.Serializable]
		private class CrashingException : RuntimeException
		{
			public CrashingException(string msg) : base(msg)
			{
			}
		}

		/// <summary>
		/// This test class provides direct access to "simulating" a crash right after
		/// realDirectory.createOutput(..) has been called on a certain specified name.
		/// </summary>
		/// <remarks>
		/// This test class provides direct access to "simulating" a crash right after
		/// realDirectory.createOutput(..) has been called on a certain specified name.
		/// </remarks>
		private class CrashAfterCreateOutput : FilterDirectory
		{
			private string crashAfterCreateOutput;

			/// <exception cref="System.IO.IOException"></exception>
			protected CrashAfterCreateOutput(Directory realDirectory) : base(realDirectory)
			{
				SetLockFactory(realDirectory.GetLockFactory());
			}

			public virtual void SetCrashAfterCreateOutput(string name)
			{
				this.crashAfterCreateOutput = name;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override IndexOutput CreateOutput(string name, IOContext cxt)
			{
				IndexOutput indexOutput = @in.CreateOutput(name, cxt);
				if (null != crashAfterCreateOutput && name.Equals(crashAfterCreateOutput))
				{
					// CRASH!
					indexOutput.Close();
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("TEST: now crash");
						Sharpen.Runtime.PrintStackTrace(new Exception(), System.Console.Out);
					}
					throw new TestCrashCausesCorruptIndex.CrashingException("crashAfterCreateOutput "
						 + crashAfterCreateOutput);
				}
				return indexOutput;
			}
		}
	}
}
