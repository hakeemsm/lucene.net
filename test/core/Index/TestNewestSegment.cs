using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.TestFramework;
using NUnit.Framework;

namespace Lucene.Net.Test.Index
{
	public class TestNewestSegment : LuceneTestCase
	{
		[Test]
		public virtual void TestNewSegment()
		{
			Directory directory = NewDirectory();
			IndexWriter writer = new IndexWriter(directory, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			IsNull(writer.NewestSegment);
			writer.Dispose();
			directory.Dispose();
		}
	}
}
