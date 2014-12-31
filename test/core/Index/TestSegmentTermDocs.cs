using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.TestFramework;
using Lucene.Net.TestFramework.Index;
using Lucene.Net.TestFramework.Util;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Index
{
	public class TestSegmentTermDocs : LuceneTestCase
	{
		private Lucene.Net.Documents.Document testDoc = new Lucene.Net.Documents.Document
			();

		private Directory dir;

		private SegmentCommitInfo info;

		[SetUp]
		public override void SetUp()
		{
			base.SetUp();
			dir = NewDirectory();
			DocHelper.SetupDoc(testDoc);
			info = DocHelper.WriteDoc(Random(), dir, testDoc);
		}

		[TearDown]
		public override void TearDown()
		{
			dir.Dispose();
			base.TearDown();
		}

        [Test]
		public virtual void TestDirNotNull()
		{
			IsTrue(dir != null);
		}

		[Test]
		public virtual void TestTermDocs()
		{
			TestTermDocs(1);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestTermDocs(int indexDivisor)
		{
			//After adding the document, we should be able to read it back in
			SegmentReader reader = new SegmentReader(info, indexDivisor, NewIOContext(Random(
				)));
			IsTrue(reader != null);
			AreEqual(indexDivisor, reader.TermInfosIndexDivisor);
			TermsEnum terms = reader.Fields.Terms(DocHelper.TEXT_FIELD_2_KEY).Iterator(null);
			terms.SeekCeil(new BytesRef("field"));
			DocsEnum termDocs = TestUtil.Docs(Random(), terms, reader.LiveDocs, null, DocsEnum
				.FLAG_FREQS);
			if (termDocs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
			{
				int docId = termDocs.DocID;
				IsTrue(docId == 0);
				int freq = termDocs.Freq;
				IsTrue(freq == 3);
			}
			reader.Dispose();
		}

		[Test]
		public virtual void TestBadSeek()
		{
			TestBadSeek(1);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestBadSeek(int indexDivisor)
		{
			{
				//After adding the document, we should be able to read it back in
				SegmentReader reader = new SegmentReader(info, indexDivisor, NewIOContext(Random(
					)));
				IsTrue(reader != null);
				DocsEnum termDocs = TestUtil.Docs(Random(), reader, "textField2", new BytesRef("bad"
					), reader.LiveDocs, null, 0);
				IsNull(termDocs);
				reader.Dispose();
			}
			{
				//After adding the document, we should be able to read it back in
				SegmentReader reader = new SegmentReader(info, indexDivisor, NewIOContext(Random(
					)));
				IsTrue(reader != null);
				DocsEnum termDocs = TestUtil.Docs(Random(), reader, "junk", new BytesRef("bad"), 
					reader.LiveDocs, null, 0);
				IsNull(termDocs);
				reader.Dispose();
			}
		}

		[Test]
		public virtual void TestSkipTo()
		{
			TestSkipTo(1);
		}

		[Test]
		public virtual void TestSkipTo(int indexDivisor)
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy()));
			Term ta = new Term("content", "aaa");
			for (int i = 0; i < 10; i++)
			{
				AddDoc(writer, "aaa aaa aaa aaa");
			}
			Term tb = new Term("content", "bbb");
			for (int i_1 = 0; i_1 < 16; i_1++)
			{
				AddDoc(writer, "bbb bbb bbb bbb");
			}
			Term tc = new Term("content", "ccc");
			for (int i_2 = 0; i_2 < 50; i_2++)
			{
				AddDoc(writer, "ccc ccc ccc ccc");
			}
			// assure that we deal with a single segment  
			writer.ForceMerge(1);
			writer.Dispose();
			IndexReader reader = DirectoryReader.Open(dir, indexDivisor);
			DocsEnum tdocs = TestUtil.Docs(Random(), reader, ta.Field, new BytesRef(ta.Text), MultiFields.GetLiveDocs(reader), null, DocsEnum.FLAG_FREQS);
			// without optimization (assumption skipInterval == 16)
			// with next
			IsTrue(tdocs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
			AreEqual(0, tdocs.DocID);
			AreEqual(4, tdocs.Freq);
			IsTrue(tdocs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
			AreEqual(1, tdocs.DocID);
			AreEqual(4, tdocs.Freq);
			IsTrue(tdocs.Advance(2) != DocIdSetIterator.NO_MORE_DOCS);
			AreEqual(2, tdocs.DocID);
			IsTrue(tdocs.Advance(4) != DocIdSetIterator.NO_MORE_DOCS);
			AreEqual(4, tdocs.DocID);
			IsTrue(tdocs.Advance(9) != DocIdSetIterator.NO_MORE_DOCS);
			AreEqual(9, tdocs.DocID);
			IsFalse(tdocs.Advance(10) != DocIdSetIterator.NO_MORE_DOCS
				);
			// without next
			tdocs = TestUtil.Docs(Random(), reader, ta.Field, new BytesRef(ta.Text), MultiFields
				.GetLiveDocs(reader), null, 0);
			IsTrue(tdocs.Advance(0) != DocIdSetIterator.NO_MORE_DOCS);
			AreEqual(0, tdocs.DocID);
			IsTrue(tdocs.Advance(4) != DocIdSetIterator.NO_MORE_DOCS);
			AreEqual(4, tdocs.DocID);
			IsTrue(tdocs.Advance(9) != DocIdSetIterator.NO_MORE_DOCS);
			AreEqual(9, tdocs.DocID);
			IsFalse(tdocs.Advance(10) != DocIdSetIterator.NO_MORE_DOCS
				);
			// exactly skipInterval documents and therefore with optimization
			// with next
			tdocs = TestUtil.Docs(Random(), reader, tb.Field, new BytesRef(tb.Text), MultiFields
				.GetLiveDocs(reader), null, DocsEnum.FLAG_FREQS);
			IsTrue(tdocs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
			AreEqual(10, tdocs.DocID);
			AreEqual(4, tdocs.Freq);
			IsTrue(tdocs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
			AreEqual(11, tdocs.DocID);
			AreEqual(4, tdocs.Freq);
			IsTrue(tdocs.Advance(12) != DocIdSetIterator.NO_MORE_DOCS);
			AreEqual(12, tdocs.DocID);
			IsTrue(tdocs.Advance(15) != DocIdSetIterator.NO_MORE_DOCS);
			AreEqual(15, tdocs.DocID);
			IsTrue(tdocs.Advance(24) != DocIdSetIterator.NO_MORE_DOCS);
			AreEqual(24, tdocs.DocID);
			IsTrue(tdocs.Advance(25) != DocIdSetIterator.NO_MORE_DOCS);
			AreEqual(25, tdocs.DocID);
			IsFalse(tdocs.Advance(26) != DocIdSetIterator.NO_MORE_DOCS
				);
			// without next
			tdocs = TestUtil.Docs(Random(), reader, tb.Field, new BytesRef(tb.Text), MultiFields
				.GetLiveDocs(reader), null, DocsEnum.FLAG_FREQS);
			IsTrue(tdocs.Advance(5) != DocIdSetIterator.NO_MORE_DOCS);
			AreEqual(10, tdocs.DocID);
			IsTrue(tdocs.Advance(15) != DocIdSetIterator.NO_MORE_DOCS);
			AreEqual(15, tdocs.DocID);
			IsTrue(tdocs.Advance(24) != DocIdSetIterator.NO_MORE_DOCS);
			AreEqual(24, tdocs.DocID);
			IsTrue(tdocs.Advance(25) != DocIdSetIterator.NO_MORE_DOCS);
			AreEqual(25, tdocs.DocID);
			IsFalse(tdocs.Advance(26) != DocIdSetIterator.NO_MORE_DOCS
				);
			// much more than skipInterval documents and therefore with optimization
			// with next
			tdocs = TestUtil.Docs(Random(), reader, tc.Field, new BytesRef(tc.Text), MultiFields
				.GetLiveDocs(reader), null, DocsEnum.FLAG_FREQS);
			IsTrue(tdocs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
			AreEqual(26, tdocs.DocID);
			AreEqual(4, tdocs.Freq);
			IsTrue(tdocs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
			AreEqual(27, tdocs.DocID);
			AreEqual(4, tdocs.Freq);
			IsTrue(tdocs.Advance(28) != DocIdSetIterator.NO_MORE_DOCS);
			AreEqual(28, tdocs.DocID);
			IsTrue(tdocs.Advance(40) != DocIdSetIterator.NO_MORE_DOCS);
			AreEqual(40, tdocs.DocID);
			IsTrue(tdocs.Advance(57) != DocIdSetIterator.NO_MORE_DOCS);
			AreEqual(57, tdocs.DocID);
			IsTrue(tdocs.Advance(74) != DocIdSetIterator.NO_MORE_DOCS);
			AreEqual(74, tdocs.DocID);
			IsTrue(tdocs.Advance(75) != DocIdSetIterator.NO_MORE_DOCS);
			AreEqual(75, tdocs.DocID);
			IsFalse(tdocs.Advance(76) != DocIdSetIterator.NO_MORE_DOCS
				);
			//without next
			tdocs = TestUtil.Docs(Random(), reader, tc.Field, new BytesRef(tc.Text), MultiFields
				.GetLiveDocs(reader), null, 0);
			IsTrue(tdocs.Advance(5) != DocIdSetIterator.NO_MORE_DOCS);
			AreEqual(26, tdocs.DocID);
			IsTrue(tdocs.Advance(40) != DocIdSetIterator.NO_MORE_DOCS);
			AreEqual(40, tdocs.DocID);
			IsTrue(tdocs.Advance(57) != DocIdSetIterator.NO_MORE_DOCS);
			AreEqual(57, tdocs.DocID);
			IsTrue(tdocs.Advance(74) != DocIdSetIterator.NO_MORE_DOCS);
			AreEqual(74, tdocs.DocID);
			IsTrue(tdocs.Advance(75) != DocIdSetIterator.NO_MORE_DOCS);
			AreEqual(75, tdocs.DocID);
			IsFalse(tdocs.Advance(76) != DocIdSetIterator.NO_MORE_DOCS
				);
			reader.Dispose();
			dir.Dispose();
		}

		[Test]
		public virtual void TestIndexDivisor()
		{
			testDoc = new Lucene.Net.Documents.Document();
			DocHelper.SetupDoc(testDoc);
			DocHelper.WriteDoc(Random(), dir, testDoc);
			TestTermDocs(2);
			TestBadSeek(2);
			TestSkipTo(2);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void AddDoc(IndexWriter writer, string value)
		{
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewTextField("content", value, Field.Store.NO));
			writer.AddDocument(doc);
		}
	}
}
