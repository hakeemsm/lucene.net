using System;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.TestFramework;
using NUnit.Framework;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Test.Index
{
    [TestFixture]
	public class TestCrashCausesCorruptIndex : LuceneTestCase
	{
		internal DirectoryInfo path;

		/// <summary>LUCENE-3627: This test fails.</summary>
		/// <remarks>LUCENE-3627: This test fails.</remarks>
		[Test]
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
		[Test]
		private void IndexAndCrashOnCreateOutputSegments2()
		{
			Directory realDirectory = FSDirectory.Open(path);
			var crashAfterCreateOutput = new CrashAfterCreateOutput(realDirectory);
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
			catch (CrashingException)
			{
			}
			// expected
			// writes segments_3
			indexWriter.Dispose();
			IsFalse(SlowFileExists(realDirectory, "segments_2"));
			crashAfterCreateOutput.Dispose();
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
			indexWriter.Dispose();
			IsFalse(SlowFileExists(realDirectory, "segments_2"));
			realDirectory.Dispose();
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
			indexReader.Dispose();
			realDirectory.Dispose();
		}

		private static readonly string TEXT_FIELD = "text";

		/// <summary>Gets a document with content "my dog has fleas".</summary>
		/// <remarks>Gets a document with content "my dog has fleas".</remarks>
		private Lucene.Net.Documents.Document GetDocument()
		{
			var document = new Lucene.Net.Documents.Document {NewTextField(TEXT_FIELD, "my dog has fleas", Field.Store.NO)};
		    return document;
		}

		/// <summary>
		/// The marker SystemException that we use in lieu of an
		/// actual machine crash.
		/// </summary>
		/// <remarks>
		/// The marker SystemException that we use in lieu of an
		/// actual machine crash.
		/// </remarks>
		[System.Serializable]
		private class CrashingException : SystemException
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
			protected internal CrashAfterCreateOutput(Directory realDirectory) : base(realDirectory)
			{
				LockFactory = (realDirectory.LockFactory);
			}

			public virtual void SetCrashAfterCreateOutput(string name)
			{
				this.crashAfterCreateOutput = name;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override IndexOutput CreateOutput(string name, IOContext cxt)
			{
				IndexOutput indexOutput = dir.CreateOutput(name, cxt);
				if (null != crashAfterCreateOutput && name.Equals(crashAfterCreateOutput))
				{
					// CRASH!
					indexOutput.Dispose();
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("TEST: now crash");
						Console.Out.WriteLine(new Exception().StackTrace);
					}
					throw new TestCrashCausesCorruptIndex.CrashingException("crashAfterCreateOutput "
						 + crashAfterCreateOutput);
				}
				return indexOutput;
			}
		}
	}
}
