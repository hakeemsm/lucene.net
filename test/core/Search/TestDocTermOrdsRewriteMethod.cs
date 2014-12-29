/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using Sharpen;

namespace Lucene.Net.Search
{
	/// <summary>Tests the DocTermOrdsRewriteMethod</summary>
	public class TestDocTermOrdsRewriteMethod : LuceneTestCase
	{
		protected internal IndexSearcher searcher1;

		protected internal IndexSearcher searcher2;

		private IndexReader reader;

		private Directory dir;

		protected internal string fieldName;

		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			dir = NewDirectory();
			fieldName = Random().NextBoolean() ? "field" : string.Empty;
			// sometimes use an empty string as field name
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, ((IndexWriterConfig
				)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random(), MockTokenizer
				.KEYWORD, false)).SetMaxBufferedDocs(TestUtil.NextInt(Random(), 50, 1000))));
			IList<string> terms = new List<string>();
			int num = AtLeast(200);
			for (int i = 0; i < num; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(NewStringField("id", i.ToString(), Field.Store.NO));
				int numTerms = Random().Next(4);
				for (int j = 0; j < numTerms; j++)
				{
					string s = TestUtil.RandomUnicodeString(Random());
					doc.Add(NewStringField(fieldName, s, Field.Store.NO));
					// if the default codec doesn't support sortedset, we will uninvert at search time
					if (DefaultCodecSupportsSortedSet())
					{
						doc.Add(new SortedSetDocValuesField(fieldName, new BytesRef(s)));
					}
					terms.Add(s);
				}
				writer.AddDocument(doc);
			}
			if (VERBOSE)
			{
				// utf16 order
				terms.Sort();
				System.Console.Out.WriteLine("UTF16 order:");
				foreach (string s in terms)
				{
					System.Console.Out.WriteLine("  " + UnicodeUtil.ToHexString(s));
				}
			}
			int numDeletions = Random().Next(num / 10);
			for (int i_1 = 0; i_1 < numDeletions; i_1++)
			{
				writer.DeleteDocuments(new Term("id", Sharpen.Extensions.ToString(Random().Next(num
					))));
			}
			reader = writer.Reader;
			searcher1 = NewSearcher(reader);
			searcher2 = NewSearcher(reader);
			writer.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			reader.Dispose();
			dir.Dispose();
			base.TearDown();
		}

		/// <summary>test a bunch of random regular expressions</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestRegexps()
		{
			int num = AtLeast(1000);
			for (int i = 0; i < num; i++)
			{
				string reg = AutomatonTestUtil.RandomRegexp(Random());
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: regexp=" + reg);
				}
				AssertSame(reg);
			}
		}

		/// <summary>
		/// check that the # of hits is the same as if the query
		/// is run against the inverted index
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual void AssertSame(string regexp)
		{
			RegexpQuery docValues = new RegexpQuery(new Term(fieldName, regexp), RegExp.NONE);
			docValues.SetRewriteMethod(new DocTermOrdsRewriteMethod());
			RegexpQuery inverted = new RegexpQuery(new Term(fieldName, regexp), RegExp.NONE);
			TopDocs invertedDocs = searcher1.Search(inverted, 25);
			TopDocs docValuesDocs = searcher2.Search(docValues, 25);
			CheckHits.CheckEqual(inverted, invertedDocs.ScoreDocs, docValuesDocs.ScoreDocs);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestEquals()
		{
			RegexpQuery a1 = new RegexpQuery(new Term(fieldName, "[aA]"), RegExp.NONE);
			RegexpQuery a2 = new RegexpQuery(new Term(fieldName, "[aA]"), RegExp.NONE);
			RegexpQuery b = new RegexpQuery(new Term(fieldName, "[bB]"), RegExp.NONE);
			AreEqual(a1, a2);
			IsFalse(a1.Equals(b));
			a1.SetRewriteMethod(new DocTermOrdsRewriteMethod());
			a2.SetRewriteMethod(new DocTermOrdsRewriteMethod());
			b.SetRewriteMethod(new DocTermOrdsRewriteMethod());
			AreEqual(a1, a2);
			IsFalse(a1.Equals(b));
			QueryUtils.Check(a1);
		}
	}
}
