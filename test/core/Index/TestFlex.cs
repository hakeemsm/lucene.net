using System;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Codecs.Lucene41;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.TestFramework;
using Lucene.Net.TestFramework.Util;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Index
{
    [TestFixture]
	public class TestFlex : LuceneTestCase
	{
		// Test non-flex API emulated on flex index
		[Test]
		public virtual void TestNonFlex()
		{
			Directory d = NewDirectory();
			int DOC_COUNT = 177;
			IndexWriter w = new IndexWriter(d, ((IndexWriterConfig)new IndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetMaxBufferedDocs(7)).SetMergePolicy(NewLogMergePolicy
				()));
			for (int iter = 0; iter < 2; iter++)
			{
				if (iter == 0)
				{
					Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
						();
					doc.Add(NewTextField("field1", "this is field1", Field.Store.NO));
					doc.Add(NewTextField("field2", "this is field2", Field.Store.NO));
					doc.Add(NewTextField("field3", "aaa", Field.Store.NO));
					doc.Add(NewTextField("field4", "bbb", Field.Store.NO));
					for (int i = 0; i < DOC_COUNT; i++)
					{
						w.AddDocument(doc);
					}
				}
				else
				{
					w.ForceMerge(1);
				}
				IndexReader r = w.Reader;
				TermsEnum terms = MultiFields.GetTerms(r, "field3").Iterator(null);
				AreEqual(TermsEnum.SeekStatus.END, terms.SeekCeil(new BytesRef
					("abc")));
				r.Dispose();
			}
			w.Dispose();
			d.Dispose();
		}

		[Test]
		public virtual void TestTermOrd()
		{
			Directory d = NewDirectory();
			IndexWriter w = new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())).SetCodec(TestUtil.AlwaysPostingsFormat(new Lucene41PostingsFormat
				())));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document {NewTextField("f", "a b c", Field.Store.NO)};
		    w.AddDocument(doc);
			w.ForceMerge(1);
			DirectoryReader r = w.Reader;
			TermsEnum terms =  GetOnlySegmentReader(r).Fields.Terms("f").Iterator(null);
			IsTrue(terms.Next() != null);
			try
			{
				AreEqual(0, terms.Ord);
			}
			catch (NotSupportedException)
			{
			}
			// ok -- codec is not required to support this op
			r.Dispose();
			w.Dispose();
			d.Dispose();
		}
	}
}
