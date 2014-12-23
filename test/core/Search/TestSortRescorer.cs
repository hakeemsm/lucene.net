/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using System.IO;
using System.Text;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search
{
	public class TestSortRescorer : LuceneTestCase
	{
		internal IndexSearcher searcher;

		internal DirectoryReader reader;

		internal Directory dir;

		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			dir = NewDirectory();
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			doc.Add(NewStringField("id", "1", Field.Store.YES));
			doc.Add(NewTextField("body", "some contents and more contents", Field.Store.NO));
			doc.Add(new NumericDocValuesField("popularity", 5));
			iw.AddDocument(doc);
			doc = new Lucene.Net.Document.Document();
			doc.Add(NewStringField("id", "2", Field.Store.YES));
			doc.Add(NewTextField("body", "another document with different contents", Field.Store
				.NO));
			doc.Add(new NumericDocValuesField("popularity", 20));
			iw.AddDocument(doc);
			doc = new Lucene.Net.Document.Document();
			doc.Add(NewStringField("id", "3", Field.Store.YES));
			doc.Add(NewTextField("body", "crappy contents", Field.Store.NO));
			doc.Add(new NumericDocValuesField("popularity", 2));
			iw.AddDocument(doc);
			reader = iw.GetReader();
			searcher = new IndexSearcher(reader);
			iw.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			reader.Close();
			dir.Close();
			base.TearDown();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestBasic()
		{
			// create a sort field and sort by it (reverse order)
			Query query = new TermQuery(new Term("body", "contents"));
			IndexReader r = searcher.GetIndexReader();
			// Just first pass query
			TopDocs hits = searcher.Search(query, 10);
			NUnit.Framework.Assert.AreEqual(3, hits.totalHits);
			NUnit.Framework.Assert.AreEqual("3", r.Document(hits.scoreDocs[0].doc).Get("id"));
			NUnit.Framework.Assert.AreEqual("1", r.Document(hits.scoreDocs[1].doc).Get("id"));
			NUnit.Framework.Assert.AreEqual("2", r.Document(hits.scoreDocs[2].doc).Get("id"));
			// Now, rescore:
			Sort sort = new Sort(new SortField("popularity", SortField.Type.INT, true));
			Rescorer rescorer = new SortRescorer(sort);
			hits = rescorer.Rescore(searcher, hits, 10);
			NUnit.Framework.Assert.AreEqual(3, hits.totalHits);
			NUnit.Framework.Assert.AreEqual("2", r.Document(hits.scoreDocs[0].doc).Get("id"));
			NUnit.Framework.Assert.AreEqual("1", r.Document(hits.scoreDocs[1].doc).Get("id"));
			NUnit.Framework.Assert.AreEqual("3", r.Document(hits.scoreDocs[2].doc).Get("id"));
			string expl = rescorer.Explain(searcher, searcher.Explain(query, hits.scoreDocs[0
				].doc), hits.scoreDocs[0].doc).ToString();
			// Confirm the explanation breaks out the individual
			// sort fields:
			NUnit.Framework.Assert.IsTrue(expl.Contains("= sort field <int: \"popularity\">! value=20"
				));
			// Confirm the explanation includes first pass details:
			NUnit.Framework.Assert.IsTrue(expl.Contains("= first pass score"));
			NUnit.Framework.Assert.IsTrue(expl.Contains("body:contents in"));
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestRandom()
		{
			Directory dir = NewDirectory();
			int numDocs = AtLeast(1000);
			RandomIndexWriter w = new RandomIndexWriter(Random(), dir);
			int[] idToNum = new int[numDocs];
			int maxValue = TestUtil.NextInt(Random(), 10, 1000000);
			for (int i = 0; i < numDocs; i++)
			{
				Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
					();
				doc.Add(NewStringField("id", string.Empty + i, Field.Store.YES));
				int numTokens = TestUtil.NextInt(Random(), 1, 10);
				StringBuilder b = new StringBuilder();
				for (int j = 0; j < numTokens; j++)
				{
					b.Append("a ");
				}
				doc.Add(NewTextField("field", b.ToString(), Field.Store.NO));
				idToNum[i] = Random().Next(maxValue);
				doc.Add(new NumericDocValuesField("num", idToNum[i]));
				w.AddDocument(doc);
			}
			IndexReader r = w.GetReader();
			w.Close();
			IndexSearcher s = NewSearcher(r);
			int numHits = TestUtil.NextInt(Random(), 1, numDocs);
			bool reverse = Random().NextBoolean();
			TopDocs hits = s.Search(new TermQuery(new Term("field", "a")), numHits);
			Rescorer rescorer = new SortRescorer(new Sort(new SortField("num", SortField.Type
				.INT, reverse)));
			TopDocs hits2 = rescorer.Rescore(s, hits, numHits);
			int[] expected = new int[numHits];
			for (int i_1 = 0; i_1 < numHits; i_1++)
			{
				expected[i_1] = hits.scoreDocs[i_1].doc;
			}
			int reverseInt = reverse ? -1 : 1;
			Arrays.Sort(expected, new _IComparer_153(idToNum, r, reverseInt));
			// Tie break by docID
			bool fail = false;
			for (int i_2 = 0; i_2 < numHits; i_2++)
			{
				fail |= expected[i_2] != hits2.scoreDocs[i_2].doc;
			}
			NUnit.Framework.Assert.IsFalse(fail);
			r.Close();
			dir.Close();
		}

		private sealed class _IComparer_153 : IComparer<int>
		{
			public _IComparer_153(int[] idToNum, IndexReader r, int reverseInt)
			{
				this.idToNum = idToNum;
				this.r = r;
				this.reverseInt = reverseInt;
			}

			public int Compare(int a, int b)
			{
				try
				{
					int av = idToNum[System.Convert.ToInt32(r.Document(a).Get("id"))];
					int bv = idToNum[System.Convert.ToInt32(r.Document(b).Get("id"))];
					if (av < bv)
					{
						return -reverseInt;
					}
					else
					{
						if (bv < av)
						{
							return reverseInt;
						}
						else
						{
							return a - b;
						}
					}
				}
				catch (IOException ioe)
				{
					throw new RuntimeException(ioe);
				}
			}

			private readonly int[] idToNum;

			private readonly IndexReader r;

			private readonly int reverseInt;
		}
	}
}
