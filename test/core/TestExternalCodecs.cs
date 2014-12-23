/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene;
using Lucene.Net.Analysis;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Lucene46;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Org.Apache.Lucene
{
	public class TestExternalCodecs : LuceneTestCase
	{
		private sealed class CustomPerFieldCodec : Lucene46Codec
		{
			private readonly PostingsFormat ramFormat = PostingsFormat.ForName("RAMOnly");

			private readonly PostingsFormat defaultFormat = PostingsFormat.ForName("Lucene41"
				);

			private readonly PostingsFormat pulsingFormat = PostingsFormat.ForName("Pulsing41"
				);

			public override PostingsFormat GetPostingsFormatForField(string field)
			{
				if (field.Equals("field2") || field.Equals("id"))
				{
					return pulsingFormat;
				}
				else
				{
					if (field.Equals("field1"))
					{
						return defaultFormat;
					}
					else
					{
						return ramFormat;
					}
				}
			}
		}

		// tests storing "id" and "field2" fields as pulsing codec,
		// whose term sort is backwards unicode code point, and
		// storing "field1" as a custom entirely-in-RAM codec
		/// <exception cref="System.Exception"></exception>
		public virtual void TestPerFieldCodec()
		{
			int NUM_DOCS = AtLeast(173);
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: NUM_DOCS=" + NUM_DOCS);
			}
			BaseDirectoryWrapper dir = NewDirectory();
			dir.SetCheckIndexOnClose(false);
			// we use a custom codec provider
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())).SetCodec(new TestExternalCodecs.CustomPerFieldCodec()).SetMergePolicy
				(NewLogMergePolicy(3)));
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			// uses default codec:
			doc.Add(NewTextField("field1", "this field uses the standard codec as the test", 
				Field.Store.NO));
			// uses pulsing codec:
			Field field2 = NewTextField("field2", "this field uses the pulsing codec as the test"
				, Field.Store.NO);
			doc.Add(field2);
			Field idField = NewStringField("id", string.Empty, Field.Store.NO);
			doc.Add(idField);
			for (int i = 0; i < NUM_DOCS; i++)
			{
				idField.SetStringValue(string.Empty + i);
				w.AddDocument(doc);
				if ((i + 1) % 10 == 0)
				{
					w.Commit();
				}
			}
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: now delete id=77");
			}
			w.DeleteDocuments(new Term("id", "77"));
			IndexReader r = DirectoryReader.Open(w, true);
			NUnit.Framework.Assert.AreEqual(NUM_DOCS - 1, r.NumDocs());
			IndexSearcher s = NewSearcher(r);
			NUnit.Framework.Assert.AreEqual(NUM_DOCS - 1, s.Search(new TermQuery(new Term("field1"
				, "standard")), 1).totalHits);
			NUnit.Framework.Assert.AreEqual(NUM_DOCS - 1, s.Search(new TermQuery(new Term("field2"
				, "pulsing")), 1).totalHits);
			r.Close();
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("\nTEST: now delete 2nd doc");
			}
			w.DeleteDocuments(new Term("id", "44"));
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("\nTEST: now force merge");
			}
			w.ForceMerge(1);
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("\nTEST: now open reader");
			}
			r = DirectoryReader.Open(w, true);
			NUnit.Framework.Assert.AreEqual(NUM_DOCS - 2, r.MaxDoc());
			NUnit.Framework.Assert.AreEqual(NUM_DOCS - 2, r.NumDocs());
			s = NewSearcher(r);
			NUnit.Framework.Assert.AreEqual(NUM_DOCS - 2, s.Search(new TermQuery(new Term("field1"
				, "standard")), 1).totalHits);
			NUnit.Framework.Assert.AreEqual(NUM_DOCS - 2, s.Search(new TermQuery(new Term("field2"
				, "pulsing")), 1).totalHits);
			NUnit.Framework.Assert.AreEqual(1, s.Search(new TermQuery(new Term("id", "76")), 
				1).totalHits);
			NUnit.Framework.Assert.AreEqual(0, s.Search(new TermQuery(new Term("id", "77")), 
				1).totalHits);
			NUnit.Framework.Assert.AreEqual(0, s.Search(new TermQuery(new Term("id", "44")), 
				1).totalHits);
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("\nTEST: now close NRT reader");
			}
			r.Close();
			w.Close();
			dir.Close();
		}
	}
}
