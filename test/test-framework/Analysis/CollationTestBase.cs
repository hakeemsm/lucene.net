/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using Lucene.Net.TestFramework.Analysis;
using Lucene.Net.TestFramework.Analysis.Tokenattributes;
using Lucene.NetDocument;
using Lucene.Net.TestFramework.Index;
using Lucene.Net.TestFramework.Search;
using Lucene.Net.TestFramework.Store;
using Lucene.Net.TestFramework.Util;
using Sharpen;

namespace Lucene.Net.TestFramework.Analysis
{
	/// <summary>Base test class for testing Unicode collation.</summary>
	/// <remarks>Base test class for testing Unicode collation.</remarks>
	public abstract class CollationTestBase : LuceneTestCase
	{
		protected internal string firstRangeBeginningOriginal = "\u062F";

		protected internal string firstRangeEndOriginal = "\u0698";

		protected internal string secondRangeBeginningOriginal = "\u0633";

		protected internal string secondRangeEndOriginal = "\u0638";

		/// <summary>Convenience method to perform the same function as CollationKeyFilter.</summary>
		/// <remarks>Convenience method to perform the same function as CollationKeyFilter.</remarks>
		/// <param name="keyBits">
		/// the result from
		/// collator.getCollationKey(original).toByteArray()
		/// </param>
		/// <returns>The encoded collation key for the original String</returns>
		[Obsolete]
		[System.ObsoleteAttribute(@"only for testing deprecated filters")]
		protected internal virtual string EncodeCollationKey(byte[] keyBits)
		{
			// Ensure that the backing char[] array is large enough to hold the encoded
			// Binary String
			int encodedLength = IndexableBinaryStringTools.GetEncodedLength(keyBits, 0, keyBits
				.Length);
			char[] encodedBegArray = new char[encodedLength];
			IndexableBinaryStringTools.Encode(keyBits, 0, keyBits.Length, encodedBegArray, 0, 
				encodedLength);
			return new string(encodedBegArray);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestFarsiRangeFilterCollating(Analyzer analyzer, BytesRef firstBeg
			, BytesRef firstEnd, BytesRef secondBeg, BytesRef secondEnd)
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT
				, analyzer));
			Lucene.NetDocument.Document doc = new Lucene.NetDocument.Document
				();
			doc.Add(new TextField("content", "\u0633\u0627\u0628", Field.Store.YES));
			doc.Add(new StringField("body", "body", Field.Store.YES));
			writer.AddDocument(doc);
			writer.Close();
			IndexReader reader = DirectoryReader.Open(dir);
			IndexSearcher searcher = new IndexSearcher(reader);
			Query query = new TermQuery(new Term("body", "body"));
			// Unicode order would include U+0633 in [ U+062F - U+0698 ], but Farsi
			// orders the U+0698 character before the U+0633 character, so the single
			// index Term below should NOT be returned by a TermRangeFilter with a Farsi
			// Collator (or an Arabic one for the case when Farsi searcher not
			// supported).
			ScoreDoc[] result = searcher.Search(query, new TermRangeFilter("content", firstBeg
				, firstEnd, true, true), 1).scoreDocs;
			NUnit.Framework.Assert.AreEqual("The index Term should not be included.", 0, result
				.Length);
			result = searcher.Search(query, new TermRangeFilter("content", secondBeg, secondEnd
				, true, true), 1).scoreDocs;
			NUnit.Framework.Assert.AreEqual("The index Term should be included.", 1, result.Length
				);
			reader.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestFarsiRangeQueryCollating(Analyzer analyzer, BytesRef firstBeg
			, BytesRef firstEnd, BytesRef secondBeg, BytesRef secondEnd)
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT
				, analyzer));
			Lucene.NetDocument.Document doc = new Lucene.NetDocument.Document
				();
			// Unicode order would include U+0633 in [ U+062F - U+0698 ], but Farsi
			// orders the U+0698 character before the U+0633 character, so the single
			// index Term below should NOT be returned by a TermRangeQuery with a Farsi
			// Collator (or an Arabic one for the case when Farsi is not supported).
			doc.Add(new TextField("content", "\u0633\u0627\u0628", Field.Store.YES));
			writer.AddDocument(doc);
			writer.Close();
			IndexReader reader = DirectoryReader.Open(dir);
			IndexSearcher searcher = new IndexSearcher(reader);
			Query query = new TermRangeQuery("content", firstBeg, firstEnd, true, true);
			ScoreDoc[] hits = searcher.Search(query, null, 1000).scoreDocs;
			NUnit.Framework.Assert.AreEqual("The index Term should not be included.", 0, hits
				.Length);
			query = new TermRangeQuery("content", secondBeg, secondEnd, true, true);
			hits = searcher.Search(query, null, 1000).scoreDocs;
			NUnit.Framework.Assert.AreEqual("The index Term should be included.", 1, hits.Length
				);
			reader.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestFarsiTermRangeQuery(Analyzer analyzer, BytesRef firstBeg, 
			BytesRef firstEnd, BytesRef secondBeg, BytesRef secondEnd)
		{
			Directory farsiIndex = NewDirectory();
			IndexWriter writer = new IndexWriter(farsiIndex, new IndexWriterConfig(TEST_VERSION_CURRENT
				, analyzer));
			Lucene.NetDocument.Document doc = new Lucene.NetDocument.Document
				();
			doc.Add(new TextField("content", "\u0633\u0627\u0628", Field.Store.YES));
			doc.Add(new StringField("body", "body", Field.Store.YES));
			writer.AddDocument(doc);
			writer.Close();
			IndexReader reader = DirectoryReader.Open(farsiIndex);
			IndexSearcher search = NewSearcher(reader);
			// Unicode order would include U+0633 in [ U+062F - U+0698 ], but Farsi
			// orders the U+0698 character before the U+0633 character, so the single
			// index Term below should NOT be returned by a TermRangeQuery
			// with a Farsi Collator (or an Arabic one for the case when Farsi is 
			// not supported).
			Query csrq = new TermRangeQuery("content", firstBeg, firstEnd, true, true);
			ScoreDoc[] result = search.Search(csrq, null, 1000).scoreDocs;
			NUnit.Framework.Assert.AreEqual("The index Term should not be included.", 0, result
				.Length);
			csrq = new TermRangeQuery("content", secondBeg, secondEnd, true, true);
			result = search.Search(csrq, null, 1000).scoreDocs;
			NUnit.Framework.Assert.AreEqual("The index Term should be included.", 1, result.Length
				);
			reader.Close();
			farsiIndex.Close();
		}

		// Test using various international locales with accented characters (which
		// sort differently depending on locale)
		//
		// Copied (and slightly modified) from 
		//Lucene.Net.TestFramework.Search.TestSort.testInternationalSort()
		//  
		// TODO: this test is really fragile. there are already 3 different cases,
		// depending upon unicode version.
		/// <exception cref="System.Exception"></exception>
		public virtual void TestCollationKeySort(Analyzer usAnalyzer, Analyzer franceAnalyzer
			, Analyzer swedenAnalyzer, Analyzer denmarkAnalyzer, string usResult, string frResult
			, string svResult, string dkResult)
		{
			Directory indexStore = NewDirectory();
			IndexWriter writer = new IndexWriter(indexStore, new IndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random(), MockTokenizer.WHITESPACE, false)));
			// document data:
			// the tracer field is used to determine which document was hit
			string[][] sortData = new string[][] { new string[] { "A", "x", "p\u00EAche", "p\u00EAche"
				, "p\u00EAche", "p\u00EAche" }, new string[] { "B", "y", "HAT", "HAT", "HAT", "HAT"
				 }, new string[] { "C", "x", "p\u00E9ch\u00E9", "p\u00E9ch\u00E9", "p\u00E9ch\u00E9"
				, "p\u00E9ch\u00E9" }, new string[] { "D", "y", "HUT", "HUT", "HUT", "HUT" }, new 
				string[] { "E", "x", "peach", "peach", "peach", "peach" }, new string[] { "F", "y"
				, "H\u00C5T", "H\u00C5T", "H\u00C5T", "H\u00C5T" }, new string[] { "G", "x", "sin"
				, "sin", "sin", "sin" }, new string[] { "H", "y", "H\u00D8T", "H\u00D8T", "H\u00D8T"
				, "H\u00D8T" }, new string[] { "I", "x", "s\u00EDn", "s\u00EDn", "s\u00EDn", "s\u00EDn"
				 }, new string[] { "J", "y", "HOT", "HOT", "HOT", "HOT" } };
			// tracer contents US                 France             Sweden (sv_SE)     Denmark (da_DK)
			FieldType customType = new FieldType();
			customType.SetStored(true);
			for (int i = 0; i < sortData.Length; ++i)
			{
				Lucene.NetDocument.Document doc = new Lucene.NetDocument.Document
					();
				doc.Add(new Field("tracer", sortData[i][0], customType));
				doc.Add(new TextField("contents", sortData[i][1], Field.Store.NO));
				if (sortData[i][2] != null)
				{
					doc.Add(new TextField("US", usAnalyzer.TokenStream("US", sortData[i][2])));
				}
				if (sortData[i][3] != null)
				{
					doc.Add(new TextField("France", franceAnalyzer.TokenStream("France", sortData[i][
						3])));
				}
				if (sortData[i][4] != null)
				{
					doc.Add(new TextField("Sweden", swedenAnalyzer.TokenStream("Sweden", sortData[i][
						4])));
				}
				if (sortData[i][5] != null)
				{
					doc.Add(new TextField("Denmark", denmarkAnalyzer.TokenStream("Denmark", sortData[
						i][5])));
				}
				writer.AddDocument(doc);
			}
			writer.ForceMerge(1);
			writer.Close();
			IndexReader reader = DirectoryReader.Open(indexStore);
			IndexSearcher searcher = new IndexSearcher(reader);
			Sort sort = new Sort();
			Query queryX = new TermQuery(new Term("contents", "x"));
			Query queryY = new TermQuery(new Term("contents", "y"));
			sort.SetSort(new SortField("US", SortField.Type.STRING));
			AssertMatches(searcher, queryY, sort, usResult);
			sort.SetSort(new SortField("France", SortField.Type.STRING));
			AssertMatches(searcher, queryX, sort, frResult);
			sort.SetSort(new SortField("Sweden", SortField.Type.STRING));
			AssertMatches(searcher, queryY, sort, svResult);
			sort.SetSort(new SortField("Denmark", SortField.Type.STRING));
			AssertMatches(searcher, queryY, sort, dkResult);
			reader.Close();
			indexStore.Close();
		}

		// Make sure the documents returned by the search match the expected list
		// Copied from TestSort.java
		/// <exception cref="System.IO.IOException"></exception>
		private void AssertMatches(IndexSearcher searcher, Query query, Sort sort, string
			 expectedResult)
		{
			ScoreDoc[] result = searcher.Search(query, null, 1000, sort).scoreDocs;
			StringBuilder buff = new StringBuilder(10);
			int n = result.Length;
			for (int i = 0; i < n; ++i)
			{
				Lucene.NetDocument.Document doc = searcher.Doc(result[i].doc);
				IndexableField[] v = doc.GetFields("tracer");
				for (int j = 0; j < v.Length; ++j)
				{
					buff.Append(v[j].StringValue());
				}
			}
			NUnit.Framework.Assert.AreEqual(expectedResult, buff.ToString());
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void AssertThreadSafe(Analyzer analyzer)
		{
			int numTestPoints = 100;
			int numThreads = TestUtil.NextInt(Random(), 3, 5);
			Dictionary<string, BytesRef> map = new Dictionary<string, BytesRef>();
			// create a map<String,SortKey> up front.
			// then with multiple threads, generate sort keys for all the keys in the map
			// and ensure they are the same as the ones we produced in serial fashion.
			for (int i = 0; i < numTestPoints; i++)
			{
				string term = TestUtil.RandomSimpleString(Random());
				TermToBytesRefAttribute termAtt = ts.AddAttribute<TermToBytesRefAttribute>();
				BytesRef bytes = termAtt.GetBytesRef();
				ts.Reset();
				NUnit.Framework.Assert.IsTrue(ts.IncrementToken());
				termAtt.FillBytesRef();
				// ensure we make a copy of the actual bytes too
				map.Put(term, BytesRef.DeepCopyOf(bytes));
				NUnit.Framework.Assert.IsFalse(ts.IncrementToken());
				ts.End();
			}
			Sharpen.Thread[] threads = new Sharpen.Thread[numThreads];
			for (int i_1 = 0; i_1 < numThreads; i_1++)
			{
				threads[i_1] = new _Thread_296(map);
			}
			for (int i_2 = 0; i_2 < numThreads; i_2++)
			{
				threads[i_2].Start();
			}
			for (int i_3 = 0; i_3 < numThreads; i_3++)
			{
				threads[i_3].Join();
			}
		}

		private sealed class _Thread_296 : Sharpen.Thread
		{
			public _Thread_296(Dictionary<string, BytesRef> map)
			{
				this.map = map;
			}

			public override void Run()
			{
				try
				{
					foreach (KeyValuePair<string, BytesRef> mapping in map.EntrySet())
					{
						string term = mapping.Key;
						BytesRef expected = mapping.Value;
						TermToBytesRefAttribute termAtt = ts.AddAttribute<TermToBytesRefAttribute>();
						BytesRef bytes = termAtt.GetBytesRef();
						ts.Reset();
						NUnit.Framework.Assert.IsTrue(ts.IncrementToken());
						termAtt.FillBytesRef();
						NUnit.Framework.Assert.AreEqual(expected, bytes);
						NUnit.Framework.Assert.IsFalse(ts.IncrementToken());
						ts.End();
					}
				}
				catch (IOException e)
				{
					throw new RuntimeException(e);
				}
			}

			private readonly Dictionary<string, BytesRef> map;
		}
	}
}
