/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Test.Index
{
	public class TestParallelTermEnum : LuceneTestCase
	{
		private AtomicReader ir1;

		private AtomicReader ir2;

		private Directory rd1;

		private Directory rd2;

		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			Lucene.Net.Documents.Document doc;
			rd1 = NewDirectory();
			IndexWriter iw1 = new IndexWriter(rd1, NewIndexWriterConfig(TEST_VERSION_CURRENT, 
				new MockAnalyzer(Random())));
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewTextField("field1", "the quick brown fox jumps", Field.Store.YES));
			doc.Add(NewTextField("field2", "the quick brown fox jumps", Field.Store.YES));
			iw1.AddDocument(doc);
			iw1.Dispose();
			rd2 = NewDirectory();
			IndexWriter iw2 = new IndexWriter(rd2, NewIndexWriterConfig(TEST_VERSION_CURRENT, 
				new MockAnalyzer(Random())));
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewTextField("field1", "the fox jumps over the lazy dog", Field.Store.YES
				));
			doc.Add(NewTextField("field3", "the fox jumps over the lazy dog", Field.Store.YES
				));
			iw2.AddDocument(doc);
			iw2.Dispose();
			this.ir1 = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(rd1));
			this.ir2 = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(rd2));
		}

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			ir1.Dispose();
			ir2.Dispose();
			rd1.Dispose();
			rd2.Dispose();
			base.TearDown();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void CheckTerms(Terms terms, Bits liveDocs, params string[] termsList)
		{
			IsNotNull(terms);
			TermsEnum te = terms.IEnumerator(null);
			foreach (string t in termsList)
			{
				BytesRef b = te.Next();
				IsNotNull(b);
				AreEqual(t, b.Utf8ToString());
				DocsEnum td = TestUtil.Docs(Random(), te, liveDocs, null, DocsEnum.FLAG_NONE);
				IsTrue(td.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
				AreEqual(0, td.DocID);
				AreEqual(td.NextDoc(), DocIdSetIterator.NO_MORE_DOCS);
			}
			IsNull(te.Next());
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void Test1()
		{
			ParallelAtomicReader pr = new ParallelAtomicReader(ir1, ir2);
			Bits liveDocs = pr.LiveDocs;
			Fields fields = pr.Fields();
			IEnumerator<string> fe = fields.IEnumerator();
			string f = fe.Next();
			AreEqual("field1", f);
			CheckTerms(fields.Terms(f), liveDocs, "brown", "fox", "jumps", "quick", "the");
			f = fe.Next();
			AreEqual("field2", f);
			CheckTerms(fields.Terms(f), liveDocs, "brown", "fox", "jumps", "quick", "the");
			f = fe.Next();
			AreEqual("field3", f);
			CheckTerms(fields.Terms(f), liveDocs, "dog", "fox", "jumps", "lazy", "over", "the"
				);
			IsFalse(fe.HasNext());
		}
	}
}
