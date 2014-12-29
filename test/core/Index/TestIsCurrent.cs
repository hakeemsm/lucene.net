/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Test.Index
{
	public class TestIsCurrent : LuceneTestCase
	{
		private RandomIndexWriter writer;

		private Directory directory;

		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			// initialize directory
			directory = NewDirectory();
			writer = new RandomIndexWriter(Random(), directory);
			// write document
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewTextField("UUID", "1", Field.Store.YES));
			writer.AddDocument(doc);
			writer.Commit();
		}

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			base.TearDown();
			writer.Dispose();
			directory.Dispose();
		}

		/// <summary>Failing testcase showing the trouble</summary>
		/// <exception cref="System.IO.IOException"></exception>
		[NUnit.Framework.Test]
		public virtual void TestDeleteByTermIsCurrent()
		{
			// get reader
			DirectoryReader reader = writer.Reader;
			// 
			//HM:revisit 
			//assert index has a document and reader is up2date 
			AreEqual("One document should be in the index", 1, writer.
				NumDocs());
			IsTrue("One document added, reader should be current", reader
				.IsCurrent);
			// remove document
			Term idTerm = new Term("UUID", "1");
			writer.DeleteDocuments(idTerm);
			writer.Commit();
			// 
			//HM:revisit 
			//assert document has been deleted (index changed), reader is stale
			AreEqual("Document should be removed", 0, writer.NumDocs
				);
			IsFalse("Reader should be stale", reader.IsCurrent);
			reader.Dispose();
		}

		/// <summary>Testcase for example to show that writer.deleteAll() is working as expected
		/// 	</summary>
		/// <exception cref="System.IO.IOException"></exception>
		[NUnit.Framework.Test]
		public virtual void TestDeleteAllIsCurrent()
		{
			// get reader
			DirectoryReader reader = writer.Reader;
			// 
			//HM:revisit 
			//assert index has a document and reader is up2date 
			AreEqual("One document should be in the index", 1, writer.
				NumDocs());
			IsTrue("Document added, reader should be stale ", reader.IsCurrent
				());
			// remove all documents
			writer.DeleteAll();
			writer.Commit();
			// 
			//HM:revisit 
			//assert document has been deleted (index changed), reader is stale
			AreEqual("Document should be removed", 0, writer.NumDocs
				);
			IsFalse("Reader should be stale", reader.IsCurrent);
			reader.Dispose();
		}
	}
}
