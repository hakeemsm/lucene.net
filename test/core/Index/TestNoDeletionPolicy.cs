using System.Collections.Generic;
using System.Reflection;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Support;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.TestFramework;

namespace Lucene.Net.Test.Index
{
	public class TestNoDeletionPolicy : LuceneTestCase
	{
		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestDeleteNotPolicy()
		{
			IndexDeletionPolicy idp = NoDeletionPolicy.INSTANCE;
			idp.OnInit<IndexCommit>(null);
			idp.OnCommit<IndexCommit>(null);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestFinalSingleton()
		{
			IsTrue((typeof(NoDeletionPolicy).IsSealed));
			var ctors = typeof(NoDeletionPolicy).GetConstructors();
			AssertEquals("expected 1 private ctor only: " + Arrays.ToString
				(ctors), 1, ctors.Length);
			AssertTrue("that 1 should be private: " + ctors[0], ctors[0].IsPrivate);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestMethodsOverridden()
		{
			// Ensures that all methods of IndexDeletionPolicy are
			// overridden/implemented. That's important to ensure that NoDeletionPolicy 
			// overrides everything, so that no unexpected behavior/error occurs.
			// NOTE: even though IndexDeletionPolicy is an interface today, and so all
			// methods must be implemented by NoDeletionPolicy, this test is important
			// in case one day IDP becomes an abstract class.
			foreach (MethodInfo m in typeof(NoDeletionPolicy).GetMethods())
			{
				// getDeclaredMethods() returns just those methods that are declared on
				// NoDeletionPolicy. getMethods() returns those that are visible in that
				// context, including ones from Object. So just filter out Object. If in
				// the future IndexDeletionPolicy will become a class that extends a
				// different class than Object, this will need to change.
				if (m.DeclaringType != typeof(object))
				{
					AssertTrue(m + " is not overridden !", m.DeclaringType == typeof(
						NoDeletionPolicy));
				}
			}
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestAllCommitsRemain()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetIndexDeletionPolicy(NoDeletionPolicy.INSTANCE));
			for (int i = 0; i < 10; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(NewTextField("c", "a" + i, Field.Store.YES));
				writer.AddDocument(doc);
				writer.Commit();
				AssertEquals("wrong number of commits !", i + 1, DirectoryReader
					.ListCommits(dir).Count);
			}
			writer.Dispose();
			dir.Dispose();
		}
	}
}
