/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Analysis;
using Lucene.Net.Codecs.Lucene41;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	public class TestFlex : LuceneTestCase
	{
		// Test non-flex API emulated on flex index
		/// <exception cref="System.Exception"></exception>
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
					Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
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
				IndexReader r = w.GetReader();
				TermsEnum terms = MultiFields.GetTerms(r, "field3").Iterator(null);
				NUnit.Framework.Assert.AreEqual(TermsEnum.SeekStatus.END, terms.SeekCeil(new BytesRef
					("abc")));
				r.Close();
			}
			w.Close();
			d.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestTermOrd()
		{
			Directory d = NewDirectory();
			IndexWriter w = new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())).SetCodec(TestUtil.AlwaysPostingsFormat(new Lucene41PostingsFormat
				())));
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			doc.Add(NewTextField("f", "a b c", Field.Store.NO));
			w.AddDocument(doc);
			w.ForceMerge(1);
			DirectoryReader r = w.GetReader();
			TermsEnum terms = GetOnlySegmentReader(r).Fields().Terms("f").Iterator(null);
			NUnit.Framework.Assert.IsTrue(terms.Next() != null);
			try
			{
				NUnit.Framework.Assert.AreEqual(0, terms.Ord());
			}
			catch (NotSupportedException)
			{
			}
			// ok -- codec is not required to support this op
			r.Close();
			w.Close();
			d.Close();
		}
	}
}
