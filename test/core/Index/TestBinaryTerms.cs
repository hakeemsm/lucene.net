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
	/// <summary>Test indexing and searching some byte[] terms</summary>
	public class TestBinaryTerms : LuceneTestCase
	{
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestBinary()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir);
			BytesRef bytes = new BytesRef(2);
			BinaryTokenStream tokenStream = new BinaryTokenStream(bytes);
			for (int i = 0; i < 256; i++)
			{
				bytes.bytes[0] = unchecked((byte)i);
				bytes.bytes[1] = unchecked((byte)(255 - i));
				bytes.length = 2;
				Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
					();
				FieldType customType = new FieldType();
				customType.SetStored(true);
				doc.Add(new Field("id", string.Empty + i, customType));
				doc.Add(new TextField("bytes", tokenStream));
				iw.AddDocument(doc);
			}
			IndexReader ir = iw.GetReader();
			iw.Close();
			IndexSearcher @is = NewSearcher(ir);
			for (int i_1 = 0; i_1 < 256; i_1++)
			{
				bytes.bytes[0] = unchecked((byte)i_1);
				bytes.bytes[1] = unchecked((byte)(255 - i_1));
				bytes.length = 2;
				TopDocs docs = @is.Search(new TermQuery(new Term("bytes", bytes)), 5);
				NUnit.Framework.Assert.AreEqual(1, docs.totalHits);
				NUnit.Framework.Assert.AreEqual(string.Empty + i_1, @is.Doc(docs.scoreDocs[0].doc
					).Get("id"));
			}
			ir.Close();
			dir.Close();
		}

		public virtual void TestToString()
		{
			Term term = new Term("foo", new BytesRef(new byte[] { unchecked((byte)unchecked((
				int)(0xff))), unchecked((byte)unchecked((int)(0xfe))) }));
			NUnit.Framework.Assert.AreEqual("foo:[ff fe]", term.ToString());
		}
	}
}
