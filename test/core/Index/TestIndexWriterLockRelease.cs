/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.IO;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Test.Index
{
	/// <summary>
	/// This tests the patch for issue #LUCENE-715 (IndexWriter does not
	/// release its write lock when trying to open an index which does not yet
	/// exist).
	/// </summary>
	/// <remarks>
	/// This tests the patch for issue #LUCENE-715 (IndexWriter does not
	/// release its write lock when trying to open an index which does not yet
	/// exist).
	/// </remarks>
	public class TestIndexWriterLockRelease : LuceneTestCase
	{
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestIndexWriterLockRelease()
		{
			Directory dir = NewFSDirectory(CreateTempDir("testLockRelease"));
			try
			{
				new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
					(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND));
			}
			catch (IOException)
			{
				try
				{
					new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
						(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND));
				}
				catch (IOException)
				{
				}
			}
			finally
			{
				dir.Dispose();
			}
		}
	}
}
