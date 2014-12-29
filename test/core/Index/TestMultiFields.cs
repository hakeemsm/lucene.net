/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Test.Index
{
	public class TestMultiFields : LuceneTestCase
	{
		/// <exception cref="System.Exception"></exception>
		public virtual void TestRandom()
		{
			int num = AtLeast(2);
			for (int iter = 0; iter < num; iter++)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: iter=" + iter);
				}
				Directory dir = NewDirectory();
				IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
					MockAnalyzer(Random())).SetMergePolicy(NoMergePolicy.COMPOUND_FILES));
				// we can do this because we use NoMergePolicy (and dont merge to "nothing")
				w.SetKeepFullyDeletedSegments(true);
				IDictionary<BytesRef, IList<int>> docs = new Dictionary<BytesRef, IList<int>>();
				ICollection<int> deleted = new HashSet<int>();
				IList<BytesRef> terms = new List<BytesRef>();
				int numDocs = TestUtil.NextInt(Random(), 1, 100 * RANDOM_MULTIPLIER);
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				Field f = NewStringField("field", string.Empty, Field.Store.NO);
				doc.Add(f);
				Field id = NewStringField("id", string.Empty, Field.Store.NO);
				doc.Add(id);
				bool onlyUniqueTerms = Random().NextBoolean();
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: onlyUniqueTerms=" + onlyUniqueTerms + " numDocs="
						 + numDocs);
				}
				ICollection<BytesRef> uniqueTerms = new HashSet<BytesRef>();
				for (int i = 0; i < numDocs; i++)
				{
					if (!onlyUniqueTerms && Random().NextBoolean() && terms.Count > 0)
					{
						// re-use existing term
						BytesRef term = terms[Random().Next(terms.Count)];
						docs.Get(term).Add(i);
						f.StringValue = term.Utf8ToString());
					}
					else
					{
						string s = TestUtil.RandomUnicodeString(Random(), 10);
						BytesRef term = new BytesRef(s);
						if (!docs.ContainsKey(term))
						{
							docs.Put(term, new List<int>());
						}
						docs.Get(term).Add(i);
						terms.Add(term);
						uniqueTerms.Add(term);
						f.StringValue = s);
					}
					id.StringValue = string.Empty + i);
					w.AddDocument(doc);
					if (Random().Next(4) == 1)
					{
						w.Commit();
					}
					if (i > 0 && Random().Next(20) == 1)
					{
						int delID = Random().Next(i);
						deleted.Add(delID);
						w.DeleteDocuments(new Term("id", string.Empty + delID));
						if (VERBOSE)
						{
							System.Console.Out.WriteLine("TEST: delete " + delID);
						}
					}
				}
				if (VERBOSE)
				{
					IList<BytesRef> termsList = new List<BytesRef>(uniqueTerms);
					termsList.Sort(BytesRef.GetUTF8SortedAsUTF16Comparator());
					System.Console.Out.WriteLine("TEST: terms in UTF16 order:");
					foreach (BytesRef b in termsList)
					{
						System.Console.Out.WriteLine("  " + UnicodeUtil.ToHexString(b.Utf8ToString()) + " "
							 + b);
						foreach (int docID in docs.Get(b))
						{
							if (deleted.Contains(docID))
							{
								System.Console.Out.WriteLine("    " + docID + " (deleted)");
							}
							else
							{
								System.Console.Out.WriteLine("    " + docID);
							}
						}
					}
				}
				IndexReader reader = w.Reader;
				w.Dispose();
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: reader=" + reader);
				}
				Bits liveDocs = MultiFields.GetLiveDocs(reader);
				foreach (int delDoc in deleted)
				{
					IsFalse(liveDocs.Get(delDoc));
				}
				for (int i_1 = 0; i_1 < 100; i_1++)
				{
					BytesRef term = terms[Random().Next(terms.Count)];
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("TEST: seek term=" + UnicodeUtil.ToHexString(term.Utf8ToString
							()) + " " + term);
					}
					DocsEnum docsEnum = TestUtil.Docs(Random(), reader, "field", term, liveDocs, null
						, DocsEnum.FLAG_NONE);
					IsNotNull(docsEnum);
					foreach (int docID in docs.Get(term))
					{
						if (!deleted.Contains(docID))
						{
							AreEqual(docID, docsEnum.NextDoc());
						}
					}
					AreEqual(DocIdSetIterator.NO_MORE_DOCS, docsEnum.NextDoc()
						);
				}
				reader.Dispose();
				dir.Dispose();
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSeparateEnums()
		{
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())));
			Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
			d.Add(NewStringField("f", "j", Field.Store.NO));
			w.AddDocument(d);
			w.Commit();
			w.AddDocument(d);
			IndexReader r = w.Reader;
			w.Dispose();
			DocsEnum d1 = TestUtil.Docs(Random(), r, "f", new BytesRef("j"), null, null, DocsEnum
				.FLAG_NONE);
			DocsEnum d2 = TestUtil.Docs(Random(), r, "f", new BytesRef("j"), null, null, DocsEnum
				.FLAG_NONE);
			AreEqual(0, d1.NextDoc());
			AreEqual(0, d2.NextDoc());
			r.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestTermDocsEnum()
		{
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())));
			Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
			d.Add(NewStringField("f", "j", Field.Store.NO));
			w.AddDocument(d);
			w.Commit();
			w.AddDocument(d);
			IndexReader r = w.Reader;
			w.Dispose();
			DocsEnum de = MultiFields.GetTermDocsEnum(r, null, "f", new BytesRef("j"));
			AreEqual(0, de.NextDoc());
			AreEqual(1, de.NextDoc());
			AreEqual(DocIdSetIterator.NO_MORE_DOCS, de.NextDoc());
			r.Dispose();
			dir.Dispose();
		}
	}
}
