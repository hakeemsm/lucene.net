/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	/// <summary>Tests the Terms.docCount statistic</summary>
	public class TestDocCount : LuceneTestCase
	{
		/// <exception cref="System.Exception"></exception>
		public virtual void TestSimple()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir);
			int numDocs = AtLeast(100);
			for (int i = 0; i < numDocs; i++)
			{
				iw.AddDocument(Doc());
			}
			IndexReader ir = iw.GetReader();
			VerifyCount(ir);
			ir.Close();
			iw.ForceMerge(1);
			ir = iw.GetReader();
			VerifyCount(ir);
			ir.Close();
			iw.Close();
			dir.Close();
		}

		private Lucene.Net.Document.Document Doc()
		{
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			int numFields = TestUtil.NextInt(Random(), 1, 10);
			for (int i = 0; i < numFields; i++)
			{
				doc.Add(NewStringField(string.Empty + TestUtil.NextInt(Random(), 'a', 'z'), string.Empty
					 + TestUtil.NextInt(Random(), 'a', 'z'), Field.Store.NO));
			}
			return doc;
		}

		/// <exception cref="System.Exception"></exception>
		private void VerifyCount(IndexReader ir)
		{
			Fields fields = MultiFields.GetFields(ir);
			if (fields == null)
			{
				return;
			}
			foreach (string field in fields)
			{
				Terms terms = fields.Terms(field);
				if (terms == null)
				{
					continue;
				}
				int docCount = terms.GetDocCount();
				FixedBitSet visited = new FixedBitSet(ir.MaxDoc());
				TermsEnum te = terms.Iterator(null);
				while (te.Next() != null)
				{
					DocsEnum de = TestUtil.Docs(Random(), te, null, null, DocsEnum.FLAG_NONE);
					while (de.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
					{
						visited.Set(de.DocID());
					}
				}
				NUnit.Framework.Assert.AreEqual(visited.Cardinality(), docCount);
			}
		}
	}
}
