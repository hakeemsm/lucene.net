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
	public class TestSegmentTermEnum : LuceneTestCase
	{
		internal Directory dir;

		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			dir = NewDirectory();
		}

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			dir.Close();
			base.TearDown();
		}

		/// <exception cref="System.IO.IOException"></exception>
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
			writer.Close();
			// verify document frequency of terms in an multi segment index
			VerifyDocFreq();
			// merge segments
			writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND));
			writer.ForceMerge(1);
			writer.Close();
			// verify document frequency of terms in a single segment index
			VerifyDocFreq();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestPrevTermAtEnd()
		{
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetCodec(TestUtil.AlwaysPostingsFormat(new Lucene41PostingsFormat
				())));
			AddDoc(writer, "aaa bbb");
			writer.Close();
			SegmentReader reader = GetOnlySegmentReader(DirectoryReader.Open(dir));
			TermsEnum terms = reader.Fields().Terms("content").Iterator(null);
			NUnit.Framework.Assert.IsNotNull(terms.Next());
			NUnit.Framework.Assert.AreEqual("aaa", terms.Term().Utf8ToString());
			NUnit.Framework.Assert.IsNotNull(terms.Next());
			long ordB;
			try
			{
				ordB = terms.Ord();
			}
			catch (NotSupportedException)
			{
				// ok -- codec is not required to support ord
				reader.Close();
				return;
			}
			NUnit.Framework.Assert.AreEqual("bbb", terms.Term().Utf8ToString());
			NUnit.Framework.Assert.IsNull(terms.Next());
			terms.SeekExact(ordB);
			NUnit.Framework.Assert.AreEqual("bbb", terms.Term().Utf8ToString());
			reader.Close();
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
			NUnit.Framework.Assert.AreEqual("aaa", termEnum.Term().Utf8ToString());
			NUnit.Framework.Assert.AreEqual(200, termEnum.DocFreq());
			// go to the second term (bbb)
			termEnum.Next();
			// 
			//HM:revisit 
			//assert that term is 'bbb'
			NUnit.Framework.Assert.AreEqual("bbb", termEnum.Term().Utf8ToString());
			NUnit.Framework.Assert.AreEqual(100, termEnum.DocFreq());
			// create enumeration of terms after term 'aaa',
			// including 'aaa'
			termEnum.SeekCeil(new BytesRef("aaa"));
			// 
			//HM:revisit 
			//assert that term is 'aaa'
			NUnit.Framework.Assert.AreEqual("aaa", termEnum.Term().Utf8ToString());
			NUnit.Framework.Assert.AreEqual(200, termEnum.DocFreq());
			// go to term 'bbb'
			termEnum.Next();
			// 
			//HM:revisit 
			//assert that term is 'bbb'
			NUnit.Framework.Assert.AreEqual("bbb", termEnum.Term().Utf8ToString());
			NUnit.Framework.Assert.AreEqual(100, termEnum.DocFreq());
			reader.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void AddDoc(IndexWriter writer, string value)
		{
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			doc.Add(NewTextField("content", value, Field.Store.NO));
			writer.AddDocument(doc);
		}
	}
}
