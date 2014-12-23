/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	public class Test2BDocs : LuceneTestCase
	{
		internal static Directory dir;

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.BeforeClass]
		public static void BeforeClass()
		{
			dir = NewFSDirectory(CreateTempDir("2Bdocs"));
			IndexWriter iw = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, 
				null));
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			for (int i = 0; i < 262144; i++)
			{
				iw.AddDocument(doc);
			}
			iw.ForceMerge(1);
			iw.Close();
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.AfterClass]
		public static void AfterClass()
		{
			dir.Close();
			dir = null;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestOverflow()
		{
			DirectoryReader ir = DirectoryReader.Open(dir);
			IndexReader[] subReaders = new IndexReader[8192];
			Arrays.Fill(subReaders, ir);
			try
			{
				new MultiReader(subReaders);
				NUnit.Framework.Assert.Fail();
			}
			catch (ArgumentException)
			{
			}
			// expected
			ir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestExactlyAtLimit()
		{
			Directory dir2 = NewFSDirectory(CreateTempDir("2BDocs2"));
			IndexWriter iw = new IndexWriter(dir2, new IndexWriterConfig(TEST_VERSION_CURRENT
				, null));
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			for (int i = 0; i < 262143; i++)
			{
				iw.AddDocument(doc);
			}
			iw.Close();
			DirectoryReader ir = DirectoryReader.Open(dir);
			DirectoryReader ir2 = DirectoryReader.Open(dir2);
			IndexReader[] subReaders = new IndexReader[8192];
			Arrays.Fill(subReaders, ir);
			subReaders[subReaders.Length - 1] = ir2;
			MultiReader mr = new MultiReader(subReaders);
			NUnit.Framework.Assert.AreEqual(int.MaxValue, mr.MaxDoc());
			NUnit.Framework.Assert.AreEqual(int.MaxValue, mr.NumDocs());
			ir.Close();
			ir2.Close();
			dir2.Close();
		}
	}
}
