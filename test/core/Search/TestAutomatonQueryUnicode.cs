/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using Sharpen;

namespace Lucene.Net.Search
{
	/// <summary>
	/// Test the automaton query for several unicode corner cases,
	/// specifically enumerating strings/indexes containing supplementary characters,
	/// and the differences between UTF-8/UTF-32 and UTF-16 binary sort order.
	/// </summary>
	/// <remarks>
	/// Test the automaton query for several unicode corner cases,
	/// specifically enumerating strings/indexes containing supplementary characters,
	/// and the differences between UTF-8/UTF-32 and UTF-16 binary sort order.
	/// </remarks>
	public class TestAutomatonQueryUnicode : LuceneTestCase
	{
		private IndexReader reader;

		private IndexSearcher searcher;

		private Directory directory;

		private readonly string FN = "field";

		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			directory = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), directory);
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			Field titleField = NewTextField("title", "some title", Field.Store.NO);
			Field field = NewTextField(FN, string.Empty, Field.Store.NO);
			Field footerField = NewTextField("footer", "a footer", Field.Store.NO);
			doc.Add(titleField);
			doc.Add(field);
			doc.Add(footerField);
			field.SetStringValue("\uD866\uDF05abcdef");
			writer.AddDocument(doc);
			field.SetStringValue("\uD866\uDF06ghijkl");
			writer.AddDocument(doc);
			// this sorts before the previous two in UTF-8/UTF-32, but after in UTF-16!!!
			field.SetStringValue("\uFB94mnopqr");
			writer.AddDocument(doc);
			field.SetStringValue("\uFB95stuvwx");
			// this one too.
			writer.AddDocument(doc);
			field.SetStringValue("a\uFFFCbc");
			writer.AddDocument(doc);
			field.SetStringValue("a\uFFFDbc");
			writer.AddDocument(doc);
			field.SetStringValue("a\uFFFEbc");
			writer.AddDocument(doc);
			field.SetStringValue("a\uFB94bc");
			writer.AddDocument(doc);
			field.SetStringValue("bacadaba");
			writer.AddDocument(doc);
			field.SetStringValue("\uFFFD");
			writer.AddDocument(doc);
			field.SetStringValue("\uFFFD\uD866\uDF05");
			writer.AddDocument(doc);
			field.SetStringValue("\uFFFD\uFFFD");
			writer.AddDocument(doc);
			reader = writer.GetReader();
			searcher = NewSearcher(reader);
			writer.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			reader.Close();
			directory.Close();
			base.TearDown();
		}

		private Term NewTerm(string value)
		{
			return new Term(FN, value);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private int AutomatonQueryNrHits(AutomatonQuery query)
		{
			return searcher.Search(query, 5).totalHits;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void AssertAutomatonHits(int expected, Lucene.Net.Util.Automaton.Automaton
			 automaton)
		{
			AutomatonQuery query = new AutomatonQuery(NewTerm("bogus"), automaton);
			query.SetRewriteMethod(MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE);
			NUnit.Framework.Assert.AreEqual(expected, AutomatonQueryNrHits(query));
			query.SetRewriteMethod(MultiTermQuery.CONSTANT_SCORE_FILTER_REWRITE);
			NUnit.Framework.Assert.AreEqual(expected, AutomatonQueryNrHits(query));
			query.SetRewriteMethod(MultiTermQuery.CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE);
			NUnit.Framework.Assert.AreEqual(expected, AutomatonQueryNrHits(query));
			query.SetRewriteMethod(MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT);
			NUnit.Framework.Assert.AreEqual(expected, AutomatonQueryNrHits(query));
		}

		/// <summary>Test that AutomatonQuery interacts with lucene's sort order correctly.</summary>
		/// <remarks>
		/// Test that AutomatonQuery interacts with lucene's sort order correctly.
		/// This expression matches something either starting with the arabic
		/// presentation forms block, or a supplementary character.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestSortOrder()
		{
			Lucene.Net.Util.Automaton.Automaton a = new RegExp("((\uD866\uDF05)|\uFB94).*"
				).ToAutomaton();
			AssertAutomatonHits(2, a);
		}
	}
}
