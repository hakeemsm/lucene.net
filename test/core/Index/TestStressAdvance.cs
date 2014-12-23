/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	public class TestStressAdvance : LuceneTestCase
	{
		/// <exception cref="System.Exception"></exception>
		public virtual void TestStressAdvance()
		{
			for (int iter = 0; iter < 3; iter++)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("\nTEST: iter=" + iter);
				}
				Directory dir = NewDirectory();
				RandomIndexWriter w = new RandomIndexWriter(Random(), dir);
				ICollection<int> aDocs = new HashSet<int>();
				Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
					();
				Field f = NewStringField("field", string.Empty, Field.Store.NO);
				doc.Add(f);
				Field idField = NewStringField("id", string.Empty, Field.Store.YES);
				doc.Add(idField);
				int num = AtLeast(4097);
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("\nTEST: numDocs=" + num);
				}
				for (int id = 0; id < num; id++)
				{
					if (Random().Next(4) == 3)
					{
						f.SetStringValue("a");
						aDocs.AddItem(id);
					}
					else
					{
						f.SetStringValue("b");
					}
					idField.SetStringValue(string.Empty + id);
					w.AddDocument(doc);
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("\nTEST: doc upto " + id);
					}
				}
				w.ForceMerge(1);
				IList<int> aDocIDs = new AList<int>();
				IList<int> bDocIDs = new AList<int>();
				DirectoryReader r = w.GetReader();
				int[] idToDocID = new int[r.MaxDoc()];
				for (int docID = 0; docID < idToDocID.Length; docID++)
				{
					int id_1 = System.Convert.ToInt32(r.Document(docID).Get("id"));
					if (aDocs.Contains(id_1))
					{
						aDocIDs.AddItem(docID);
					}
					else
					{
						bDocIDs.AddItem(docID);
					}
				}
				TermsEnum te = GetOnlySegmentReader(r).Fields().Terms("field").Iterator(null);
				DocsEnum de = null;
				for (int iter2 = 0; iter2 < 10; iter2++)
				{
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("\nTEST: iter=" + iter + " iter2=" + iter2);
					}
					NUnit.Framework.Assert.AreEqual(TermsEnum.SeekStatus.FOUND, te.SeekCeil(new BytesRef
						("a")));
					de = TestUtil.Docs(Random(), te, null, de, DocsEnum.FLAG_NONE);
					TestOne(de, aDocIDs);
					NUnit.Framework.Assert.AreEqual(TermsEnum.SeekStatus.FOUND, te.SeekCeil(new BytesRef
						("b")));
					de = TestUtil.Docs(Random(), te, null, de, DocsEnum.FLAG_NONE);
					TestOne(de, bDocIDs);
				}
				w.Close();
				r.Close();
				dir.Close();
			}
		}

		/// <exception cref="System.Exception"></exception>
		private void TestOne(DocsEnum docs, IList<int> expected)
		{
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("test");
			}
			int upto = -1;
			while (upto < expected.Count)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("  cycle upto=" + upto + " of " + expected.Count);
				}
				int docID;
				if (Random().Next(4) == 1 || upto == expected.Count - 1)
				{
					// test nextDoc()
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("    do nextDoc");
					}
					upto++;
					docID = docs.NextDoc();
				}
				else
				{
					// test advance()
					int inc = TestUtil.NextInt(Random(), 1, expected.Count - 1 - upto);
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("    do advance inc=" + inc);
					}
					upto += inc;
					docID = docs.Advance(expected[upto]);
				}
				if (upto == expected.Count)
				{
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("  expect docID=" + DocIdSetIterator.NO_MORE_DOCS + 
							" actual=" + docID);
					}
					NUnit.Framework.Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, docID);
				}
				else
				{
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("  expect docID=" + expected[upto] + " actual=" + docID
							);
					}
					NUnit.Framework.Assert.IsTrue(docID != DocIdSetIterator.NO_MORE_DOCS);
					NUnit.Framework.Assert.AreEqual(expected[upto], docID);
				}
			}
		}
	}
}
