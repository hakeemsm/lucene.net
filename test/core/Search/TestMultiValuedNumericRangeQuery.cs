/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Globalization;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search
{
	public class TestMultiValuedNumericRangeQuery : LuceneTestCase
	{
		/// <summary>Tests NumericRangeQuery on a multi-valued field (multiple numeric values per document).
		/// 	</summary>
		/// <remarks>
		/// Tests NumericRangeQuery on a multi-valued field (multiple numeric values per document).
		/// This test ensures, that a classical TermRangeQuery returns exactly the same document numbers as
		/// NumericRangeQuery (see SOLR-1322 for discussion) and the multiple precision terms per numeric value
		/// do not interfere with multiple numeric values.
		/// </remarks>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestMultiValuedNRQ()
		{
			Directory directory = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), directory, ((IndexWriterConfig
				)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs
				(TestUtil.NextInt(Random(), 50, 1000))));
			DecimalFormat format = new DecimalFormat("00000000000", new DecimalFormatSymbols(
				CultureInfo.ROOT));
			int num = AtLeast(500);
			for (int l = 0; l < num; l++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				for (int m = 0; m <= c; m++)
				{
					int value = Random().Next(int.MaxValue);
					doc.Add(NewStringField("asc", format.Format(value), Field.Store.NO));
					doc.Add(new IntField("trie", value, Field.Store.NO));
				}
				writer.AddDocument(doc);
			}
			IndexReader reader = writer.GetReader();
			writer.Close();
			IndexSearcher searcher = NewSearcher(reader);
			num = AtLeast(50);
			for (int i = 0; i < num; i++)
			{
				int lower = Random().Next(int.MaxValue);
				int upper = Random().Next(int.MaxValue);
				if (lower > upper)
				{
					int a = lower;
					lower = upper;
					upper = a;
				}
				TermRangeQuery cq = TermRangeQuery.NewStringRange("asc", format.Format(lower), format
					.Format(upper), true, true);
				NumericRangeQuery<int> tq = NumericRangeQuery.NewIntRange("trie", lower, upper, true
					, true);
				TopDocs trTopDocs = searcher.Search(cq, 1);
				TopDocs nrTopDocs = searcher.Search(tq, 1);
				AreEqual("Returned count for NumericRangeQuery and TermRangeQuery must be equal"
					, trTopDocs.TotalHits, nrTopDocs.TotalHits);
			}
			reader.Close();
			directory.Close();
		}
	}
}
