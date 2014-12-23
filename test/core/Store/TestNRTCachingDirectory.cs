/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Analysis;
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
			IList<BytesRef> ids = new AList<BytesRef>();
			DirectoryReader r = null;
			for (int docCount = 0; docCount < numDocs; docCount++)
			{
				Lucene.Net.Document.Document doc = docs.NextDoc();
				ids.AddItem(new BytesRef(doc.Get("docid")));
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
							r.Close();
							r = r2;
						}
					}
					NUnit.Framework.Assert.AreEqual(1 + docCount, r.NumDocs());
					IndexSearcher s = NewSearcher(r);
					// Just make sure search can run; we can't 
					//HM:revisit 
					//assert
					// totHits since it could be 0
					TopDocs hits = s.Search(new TermQuery(new Term("body", "the")), 10);
				}
			}
			// System.out.println("tot hits " + hits.totalHits);
			if (r != null)
			{
				r.Close();
			}
			// Close should force cache to clear since all files are sync'd
			w.Close();
			string[] cachedFiles = cachedDir.ListCachedFiles();
			foreach (string file in cachedFiles)
			{
				System.Console.Out.WriteLine("FAIL: cached file " + file + " remains after sync");
			}
			NUnit.Framework.Assert.AreEqual(0, cachedFiles.Length);
			r = DirectoryReader.Open(dir);
			foreach (BytesRef id in ids)
			{
				NUnit.Framework.Assert.AreEqual(1, r.DocFreq(new Term("docid", id)));
			}
			r.Close();
			cachedDir.Close();
			docs.Close();
		}

		// NOTE: not a test; just here to make sure the code frag
		// in the javadocs is correct!
		/// <exception cref="System.Exception"></exception>
		public virtual void VerifyCompiles()
		{
			Analyzer analyzer = null;
			Directory fsDir = FSDirectory.Open(new FilePath("/path/to/index"));
			NRTCachingDirectory cachedFSDir = new NRTCachingDirectory(fsDir, 2.0, 25.0);
			IndexWriterConfig conf = new IndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			IndexWriter writer = new IndexWriter(cachedFSDir, conf);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestDeleteFile()
		{
			Directory dir = new NRTCachingDirectory(NewDirectory(), 2.0, 25.0);
			dir.CreateOutput("foo.txt", IOContext.DEFAULT).Close();
			dir.DeleteFile("foo.txt");
			NUnit.Framework.Assert.AreEqual(0, dir.ListAll().Length);
			dir.Close();
		}

		// LUCENE-3382 -- make sure we get exception if the directory really does not exist.
		/// <exception cref="System.Exception"></exception>
		public virtual void TestNoDir()
		{
			FilePath tempDir = CreateTempDir("doesnotexist");
			TestUtil.Rm(tempDir);
			Directory dir = new NRTCachingDirectory(NewFSDirectory(tempDir), 2.0, 25.0);
			try
			{
				DirectoryReader.Open(dir);
				NUnit.Framework.Assert.Fail("did not hit expected exception");
			}
			catch (NoSuchDirectoryException)
			{
			}
			// expected
			dir.Close();
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
				dir.CreateOutput(name, NewIOContext(Random())).Close();
				NUnit.Framework.Assert.IsTrue(SlowFileExists(dir, name));
				NUnit.Framework.Assert.IsTrue(Arrays.AsList(dir.ListAll()).Contains(name));
			}
			finally
			{
				dir.Close();
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
			@out.Close();
			NUnit.Framework.Assert.AreEqual(1, csw.ListAll().Length);
			NUnit.Framework.Assert.AreEqual("d.xyz", csw.ListAll()[0]);
			csw.Close();
			CompoundFileDirectory cfr = new CompoundFileDirectory(newDir, "d.cfs", NewIOContext
				(Random()), false);
			NUnit.Framework.Assert.AreEqual(1, cfr.ListAll().Length);
			NUnit.Framework.Assert.AreEqual("d.xyz", cfr.ListAll()[0]);
			cfr.Close();
			newDir.Close();
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
			os.Close();
		}
	}
}
