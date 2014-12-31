using System;
using Lucene.Net.Analysis;
using Lucene.Net.Codecs.Lucene41;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.TestFramework;
using Lucene.Net.TestFramework.Util;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Index
{
	public class TestSegmentTermEnum : LuceneTestCase
	{
		internal Directory dir;

		[Test]
		public override void SetUp()
		{
			base.SetUp();
			dir = NewDirectory();
		}

		[TearDown]
		public override void TearDown()
		{
			dir.Dispose();
			base.TearDown();
		}

		[Test]
		public virtual void TestTermEnum()
		{
			IndexWriter writer = null;
			writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())));
			// ADD 100 documents with term : aaa
			// add 100 documents with terms: aaa bbb
			// Therefore, term 'aaa' has document frequency of 200 and term 'bbb' 100
			for (int i = 0; i < 100; i++)
			{
				AddDoc(writer, "aaa");
				AddDoc(writer, "aaa bbb");
			}
			writer.Dispose();
			// verify document frequency of terms in an multi segment index
			VerifyDocFreq();
			// merge segments
			writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND));
			writer.ForceMerge(1);
			writer.Dispose();
			// verify document frequency of terms in a single segment index
			VerifyDocFreq();
		}

		[Test]
		public virtual void TestPrevTermAtEnd()
		{
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetCodec(TestUtil.AlwaysPostingsFormat(new Lucene41PostingsFormat
				())));
			AddDoc(writer, "aaa bbb");
			writer.Dispose();
			SegmentReader reader = GetOnlySegmentReader(DirectoryReader.Open(dir));
			TermsEnum terms = reader.Fields.Terms("content").Iterator(null);
			IsNotNull(terms.Next());
			AreEqual("aaa", terms.Term.Utf8ToString());
			IsNotNull(terms.Next());
			long ordB;
			try
			{
				ordB = terms.Ord;
			}
			catch (NotSupportedException)
			{
				// ok -- codec is not required to support ord
				reader.Dispose();
				return;
			}
			AreEqual("bbb", terms.Term.Utf8ToString());
			IsNull(terms.Next());
			terms.SeekExact(ordB);
			AreEqual("bbb", terms.Term.Utf8ToString());
			reader.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void VerifyDocFreq()
		{
			IndexReader reader = DirectoryReader.Open(dir);
			TermsEnum termEnum = MultiFields.GetTerms(reader, "content").Iterator(null);
			// create enumeration of all terms
			// go to the first term (aaa)
			termEnum.Next();
			// 
			//HM:revisit 
			//assert that term is 'aaa'
			AreEqual("aaa", termEnum.Term.Utf8ToString());
			AreEqual(200, termEnum.DocFreq);
			// go to the second term (bbb)
			termEnum.Next();
			// 
			//HM:revisit 
			//assert that term is 'bbb'
			AreEqual("bbb", termEnum.Term.Utf8ToString());
			AreEqual(100, termEnum.DocFreq);
			// create enumeration of terms after term 'aaa',
			// including 'aaa'
			termEnum.SeekCeil(new BytesRef("aaa"));
			// 
			//HM:revisit 
			//assert that term is 'aaa'
			AreEqual("aaa", termEnum.Term.Utf8ToString());
			AreEqual(200, termEnum.DocFreq);
			// go to term 'bbb'
			termEnum.Next();
			// 
			//HM:revisit 
			//assert that term is 'bbb'
			AreEqual("bbb", termEnum.Term.Utf8ToString());
			AreEqual(100, termEnum.DocFreq);
			reader.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void AddDoc(IndexWriter writer, string value)
		{
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
			{
			    NewTextField("content", value, Field.Store.NO)
			};
		    writer.AddDocument(doc);
		}
	}
}
