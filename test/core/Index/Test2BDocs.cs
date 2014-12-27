using System;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using NUnit.Framework;

namespace Lucene.Net.Test.Index
{
    [TestFixture]
    public class Test2BDocs : LuceneTestCase
	{
		internal static Directory dir;

		/// <exception cref="System.Exception"></exception>
		[SetUp]
		public static void Setup()
		{
			dir = NewFSDirectory(CreateTempDir("2Bdocs"));
			var iw = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, null));
			var doc = new Lucene.Net.Documents.Document();
			for (int i = 0; i < 262144; i++)
			{
				iw.AddDocument(doc);
			}
			iw.ForceMerge(1);
			iw.Dispose();
		}

		
		[TearDown]
		public static void TearDown()
		{
			dir.Dispose();
			dir = null;
		}

		[Test]
		public virtual void TestOverflow()
		{
			DirectoryReader ir = DirectoryReader.Open(dir);
			IndexReader[] subReaders = new IndexReader[8192];
			Arrays.Fill(subReaders, ir);
			try
			{
				new MultiReader(subReaders);
				Fail();
			}
			catch (ArgumentException)
			{
			}
			// expected
			ir.Dispose();
		}

		[Test]
		public virtual void TestExactlyAtLimit()
		{
			Directory dir2 = NewFSDirectory(CreateTempDir("2BDocs2"));
			var iw = new IndexWriter(dir2, new IndexWriterConfig(TEST_VERSION_CURRENT, null));
			var doc = new Lucene.Net.Documents.Document();
			for (int i = 0; i < 262143; i++)
			{
				iw.AddDocument(doc);
			}
			iw.Dispose();
			DirectoryReader ir = DirectoryReader.Open(dir);
			DirectoryReader ir2 = DirectoryReader.Open(dir2);
			IndexReader[] subReaders = new IndexReader[8192];
			Arrays.Fill(subReaders, ir);
			subReaders[subReaders.Length - 1] = ir2;
			MultiReader mr = new MultiReader(subReaders);
			AreEqual(int.MaxValue, mr.MaxDoc);
			AreEqual(int.MaxValue, mr.NumDocs);
			ir.Dispose();
			ir2.Dispose();
			dir2.Dispose();
		}
	}
}
