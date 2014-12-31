using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.TestFramework;
using Lucene.Net.TestFramework.Index;
using Lucene.Net.TestFramework.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Index
{
	public class TestReaderClosed : LuceneTestCase
	{
		private IndexReader reader;

		private Directory dir;

		[SetUp]
		public override void SetUp()
		{
			base.SetUp();
			dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, ((IndexWriterConfig
				)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random(), MockTokenizer
				.KEYWORD, false)).SetMaxBufferedDocs(TestUtil.NextInt(Random(), 50, 1000))));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			Field field = NewStringField("field", string.Empty, Field.Store.NO);
			doc.Add(field);
			// we generate aweful prefixes: good for testing.
			// but for preflex codec, the test can be very slow, so use less iterations.
			int num = AtLeast(10);
			for (int i = 0; i < num; i++)
			{
				field.StringValue = TestUtil.RandomUnicodeString(Random(), 10);
				writer.AddDocument(doc);
			}
			reader = writer.Reader;
			writer.Dispose();
		}

		[Test]
		public virtual void TestSearch()
		{
			IsTrue(reader.RefCount > 0);
			IndexSearcher searcher = NewSearcher(reader);
			TermRangeQuery query = TermRangeQuery.NewStringRange("field", "a", "z", true, true
				);
			searcher.Search(query, 5);
			reader.Dispose();
			try
			{
				searcher.Search(query, 5);
			}
			catch (AlreadyClosedException)
			{
			}
		}

		// expected
		// LUCENE-3800
		[Test]
		public virtual void TestReaderChaining()
		{
			IsTrue(reader.RefCount > 0);
			IndexReader wrappedReader = SlowCompositeReaderWrapper.Wrap(reader);
			wrappedReader = new ParallelAtomicReader((AtomicReader)wrappedReader);
			IndexSearcher searcher = NewSearcher(wrappedReader);
			TermRangeQuery query = TermRangeQuery.NewStringRange("field", "a", "z", true, true
				);
			searcher.Search(query, 5);
			reader.Dispose();
			// close original child reader
			try
			{
				searcher.Search(query, 5);
			}
			catch (AlreadyClosedException ace)
			{
				AreEqual("this IndexReader cannot be used anymore as one of its child readers was closed"
					, ace.Message);
			}
			finally
			{
				// shutdown executor: in case of wrap-wrap-wrapping
				searcher.IndexReader.Dispose();
			}
		}

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			dir.Dispose();
			base.TearDown();
		}
	}
}
