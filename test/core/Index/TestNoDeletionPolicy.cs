/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Reflection;
using Lucene.Net.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;
using Sharpen.Reflect;

namespace Lucene.Net.Index
{
	public class TestNoDeletionPolicy : LuceneTestCase
	{
		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestNoDeletionPolicy()
		{
			IndexDeletionPolicy idp = NoDeletionPolicy.INSTANCE;
			idp.OnInit(null);
			idp.OnCommit(null);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestFinalSingleton()
		{
			NUnit.Framework.Assert.IsTrue(Modifier.IsFinal(typeof(NoDeletionPolicy).GetModifiers
				()));
			Constructor<object>[] ctors = typeof(NoDeletionPolicy).GetDeclaredConstructors();
			NUnit.Framework.Assert.AreEqual("expected 1 private ctor only: " + Arrays.ToString
				(ctors), 1, ctors.Length);
			NUnit.Framework.Assert.IsTrue("that 1 should be private: " + ctors[0], Modifier.IsPrivate
				(ctors[0].GetModifiers()));
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
					NUnit.Framework.Assert.IsTrue(m + " is not overridden !", m.DeclaringType == typeof(
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
				Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
					();
				doc.Add(NewTextField("c", "a" + i, Field.Store.YES));
				writer.AddDocument(doc);
				writer.Commit();
				NUnit.Framework.Assert.AreEqual("wrong number of commits !", i + 1, DirectoryReader
					.ListCommits(dir).Count);
			}
			writer.Close();
			dir.Close();
		}
	}
}
