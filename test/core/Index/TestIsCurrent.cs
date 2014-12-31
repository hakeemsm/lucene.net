using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.TestFramework;
using Lucene.Net.TestFramework.Index;
using NUnit.Framework;

namespace Lucene.Net.Test.Index
{
	public class TestIsCurrent : LuceneTestCase
	{
		private RandomIndexWriter writer;

		private Directory directory;

		[SetUp]
		public override void SetUp()
		{
			base.SetUp();
			// initialize directory
			directory = NewDirectory();
			writer = new RandomIndexWriter(Random(), directory);
			// write document
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document {NewTextField("UUID", "1", Field.Store.YES)};
		    writer.AddDocument(doc);
			writer.Commit();
		}

		[TearDown]
		public override void TearDown()
		{
			base.TearDown();
			writer.Close();
			directory.Dispose();
		}

		/// <summary>Failing testcase showing the trouble</summary>
		/// <exception cref="System.IO.IOException"></exception>
		[NUnit.Framework.Test]
		public virtual void TestDeleteByTermIsCurrent()
		{
			// get reader
			DirectoryReader reader = writer.GetReader();
			// 
			
			//assert index has a document and reader is up2date 
			AssertEquals("One document should be in the index", 1, writer.
				NumDocs());
			AssertTrue("One document added, reader should be current", reader.IsCurrent);
			// remove document
			Term idTerm = new Term("UUID", "1");
			writer.DeleteDocuments(idTerm);
			writer.Commit();
			// 
			
			//assert document has been deleted (index changed), reader is stale
			AssertEquals("Document should be removed", 0, writer.NumDocs());
			AssertFalse("Reader should be stale", reader.IsCurrent);
			reader.Dispose();
		}

		/// <summary>Testcase for example to show that writer.deleteAll() is working as expected
		/// 	</summary>
		/// <exception cref="System.IO.IOException"></exception>
		[NUnit.Framework.Test]
		public virtual void TestDeleteAllIsCurrent()
		{
			// get reader
			DirectoryReader reader = writer.GetReader();
			// 
			//HM:revisit 
			//assert index has a document and reader is up2date 
			AssertEquals("One document should be in the index", 1, writer.
				NumDocs());
			AssertTrue("Document added, reader should be stale ", reader.IsCurrent);
			// remove all documents
			writer.DeleteAll();
			writer.Commit();
			// 
			//HM:revisit 
			//assert document has been deleted (index changed), reader is stale
			AssertEquals("Document should be removed", 0, writer.NumDocs());
			AssertFalse("Reader should be stale", reader.IsCurrent);
			reader.Dispose();
		}
	}
}
