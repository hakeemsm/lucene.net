/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Store
{
	public class TestNRTCachingDirectory : LuceneTestCase
	{
		/// <exception cref="System.Exception"></exception>
		public virtual void TestNRTAndCommit()
		{
			Directory dir = NewDirectory();
			NRTCachingDirectory cachedDir = new NRTCachingDirectory(dir, 2.0, 25.0);
			MockAnalyzer analyzer = new MockAnalyzer(Random());
			analyzer.SetMaxTokenLength(TestUtil.NextInt(Random(), 1, IndexWriter.MAX_TERM_LENGTH
				));
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			RandomIndexWriter w = new RandomIndexWriter(Random(), cachedDir, conf);
			LineFileDocs docs = new LineFileDocs(Random(), DefaultCodecSupportsDocValues());
			int numDocs = TestUtil.NextInt(Random(), 100, 400);
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: numDocs=" + numDocs);
			}
			IList<BytesRef> ids = new List<BytesRef>();
			DirectoryReader r = null;
			for (int docCount = 0; docCount < numDocs; docCount++)
			{
				Lucene.Net.Documents.Document doc = docs.NextDoc();
				ids.Add(new BytesRef(doc.Get("docid")));
				w.AddDocument(doc);
				if (Random().Next(20) == 17)
				{
					if (r == null)
					{
						r = DirectoryReader.Open(w.w, false);
					}
					else
					{
						DirectoryReader r2 = DirectoryReader.OpenIfChanged(r);
						if (r2 != null)
						{
							r.Dispose();
							r = r2;
						}
					}
					AreEqual(1 + docCount, r.NumDocs);
					IndexSearcher s = NewSearcher(r);
					// Just make sure search can run; we can't 
					//HM:revisit 
					//assert
					// totHits since it could be 0
					TopDocs hits = s.Search(new TermQuery(new Term("body", "the")), 10);
				}
			}
			// System.out.println("tot hits " + hits.TotalHits);
			if (r != null)
			{
				r.Dispose();
			}
			// Close should force cache to clear since all files are sync'd
			w.Dispose();
			string[] cachedFiles = cachedDir.ListCachedFiles();
			foreach (string file in cachedFiles)
			{
				System.Console.Out.WriteLine("FAIL: cached file " + file + " remains after sync");
			}
			AreEqual(0, cachedFiles.Length);
			r = DirectoryReader.Open(dir);
			foreach (BytesRef id in ids)
			{
				AreEqual(1, r.DocFreq(new Term("docid", id)));
			}
			r.Dispose();
			cachedDir.Dispose();
			docs.Dispose();
		}

		// NOTE: not a test; just here to make sure the code frag
		// in the javadocs is correct!
		/// <exception cref="System.Exception"></exception>
		public virtual void VerifyCompiles()
		{
			Analyzer analyzer = null;
			Directory fsDir = FSDirectory.Open(new DirectoryInfo("/path/to/index"));
			NRTCachingDirectory cachedFSDir = new NRTCachingDirectory(fsDir, 2.0, 25.0);
			IndexWriterConfig conf = new IndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			IndexWriter writer = new IndexWriter(cachedFSDir, conf);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestDeleteFile()
		{
			Directory dir = new NRTCachingDirectory(NewDirectory(), 2.0, 25.0);
			dir.CreateOutput("foo.txt", IOContext.DEFAULT).Dispose();
			dir.DeleteFile("foo.txt");
			AreEqual(0, dir.ListAll().Length);
			dir.Dispose();
		}

		// LUCENE-3382 -- make sure we get exception if the directory really does not exist.
		/// <exception cref="System.Exception"></exception>
		public virtual void TestNoDir()
		{
			DirectoryInfo tempDir = CreateTempDir("doesnotexist");
			TestUtil.Rm(tempDir);
			Directory dir = new NRTCachingDirectory(NewFSDirectory(tempDir), 2.0, 25.0);
			try
			{
				DirectoryReader.Open(dir);
				Fail("did not hit expected exception");
			}
			catch (NoSuchDirectoryException)
			{
			}
			// expected
			dir.Dispose();
		}

		// LUCENE-3382 test that we can add a file, and then when we call list() we get it back
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestDirectoryFilter()
		{
			Directory dir = new NRTCachingDirectory(NewFSDirectory(CreateTempDir("foo")), 2.0
				, 25.0);
			string name = "file";
			try
			{
				dir.CreateOutput(name, NewIOContext(Random())).Dispose();
				IsTrue(SlowFileExists(dir, name));
				IsTrue(Arrays.AsList(dir.ListAll()).Contains(name));
			}
			finally
			{
				dir.Dispose();
			}
		}

		// LUCENE-3382 test that delegate compound files correctly.
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestCompoundFileAppendTwice()
		{
			Directory newDir = new NRTCachingDirectory(NewDirectory(), 2.0, 25.0);
			CompoundFileDirectory csw = new CompoundFileDirectory(newDir, "d.cfs", NewIOContext
				(Random()), true);
			CreateSequenceFile(newDir, "d1", unchecked((byte)0), 15);
			IndexOutput @out = csw.CreateOutput("d.xyz", NewIOContext(Random()));
			@out.WriteInt(0);
			@out.Dispose();
			AreEqual(1, csw.ListAll().Length);
			AreEqual("d.xyz", csw.ListAll()[0]);
			csw.Dispose();
			CompoundFileDirectory cfr = new CompoundFileDirectory(newDir, "d.cfs", NewIOContext
				(Random()), false);
			AreEqual(1, cfr.ListAll().Length);
			AreEqual("d.xyz", cfr.ListAll()[0]);
			cfr.Dispose();
			newDir.Dispose();
		}

		/// <summary>Creates a file of the specified size with sequential data.</summary>
		/// <remarks>
		/// Creates a file of the specified size with sequential data. The first
		/// byte is written as the start byte provided. All subsequent bytes are
		/// computed as start + offset where offset is the number of the byte.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		private void CreateSequenceFile(Directory dir, string name, byte start, int size)
		{
			IndexOutput os = dir.CreateOutput(name, NewIOContext(Random()));
			for (int i = 0; i < size; i++)
			{
				os.WriteByte(start);
				start++;
			}
			os.Dispose();
		}
	}
}
