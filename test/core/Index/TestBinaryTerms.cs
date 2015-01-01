using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.TestFramework;
using Lucene.Net.TestFramework.Index;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Index
{
	/// <summary>Test indexing and searching some byte[] terms</summary>
    [TestFixture]
    public class TestBinaryTerms : LuceneTestCase
	{
		[Test]
		public virtual void TestBinary()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir);
			BytesRef bytes = new BytesRef(2);
			BinaryTokenStream tokenStream = new BinaryTokenStream(bytes);
			for (int i = 0; i < 256; i++)
			{
				bytes.bytes[0] = (sbyte)i;
				bytes.bytes[1] = (sbyte)(255 - i);
				bytes.length = 2;
			    FieldType customType = new FieldType {Stored = (true)};
			    var doc = new Lucene.Net.Documents.Document
			    {
			        new Field("id", string.Empty + i, customType),
			        new TextField("bytes", tokenStream)
			    };
			    iw.AddDocument(doc);
			}
			IndexReader ir = iw.Reader;
			iw.Close();
			IndexSearcher @is = NewSearcher(ir);
			for (int i = 0; i < 256; i++)
			{
				bytes.bytes[0] = (sbyte)i;
				bytes.bytes[1] = (sbyte)(255 - i);
				bytes.length = 2;
				TopDocs docs = @is.Search(new TermQuery(new Term("bytes", bytes)), 5);
				AreEqual(1, docs.TotalHits);
				AreEqual(string.Empty + i, @is.Doc(docs.ScoreDocs[0].Doc
					).Get("id"));
			}
			ir.Dispose();
			dir.Dispose();
		}

        [Test]
		public virtual void TestToString()
		{
			Term term = new Term("foo", new BytesRef(new[] { unchecked((sbyte)(0xff)), unchecked((sbyte)(0xfe)) }));
			AreEqual("foo:[ff fe]", term.ToString());
		}
	}
}
