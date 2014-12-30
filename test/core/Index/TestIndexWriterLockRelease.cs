using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.TestFramework;
using NUnit.Framework;
using Directory = Lucene.Net.Store.Directory;

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
	[TestFixture]
    public class TestIndexWriterLockRelease : LuceneTestCase
	{
		[Test]
		public virtual void TestIndexWriterReleaseLock()
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
