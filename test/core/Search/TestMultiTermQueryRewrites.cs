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

namespace Lucene.Net.Search
{
	public class TestMultiTermQueryRewrites : LuceneTestCase
	{
		internal static Directory dir;

		internal static Directory sdir1;

		internal static Directory sdir2;

		internal static IndexReader reader;

		internal static IndexReader multiReader;

		internal static IndexReader multiReaderDupls;

		internal static IndexSearcher searcher;

		internal static IndexSearcher multiSearcher;

		internal static IndexSearcher multiSearcherDupls;

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.BeforeClass]
		public static void BeforeClass()
		{
			dir = NewDirectory();
			sdir1 = NewDirectory();
			sdir2 = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, new MockAnalyzer(
				Random()));
			RandomIndexWriter swriter1 = new RandomIndexWriter(Random(), sdir1, new MockAnalyzer
				(Random()));
			RandomIndexWriter swriter2 = new RandomIndexWriter(Random(), sdir2, new MockAnalyzer
				(Random()));
			for (int i = 0; i < 10; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(NewStringField("data", i.ToString(), Field.Store.NO));
				writer.AddDocument(doc);
				((i % 2 == 0) ? swriter1 : swriter2).AddDocument(doc);
			}
			writer.ForceMerge(1);
			swriter1.ForceMerge(1);
			swriter2.ForceMerge(1);
			writer.Dispose();
			swriter1.Dispose();
			swriter2.Dispose();
			reader = DirectoryReader.Open(dir);
			searcher = NewSearcher(reader);
			multiReader = new MultiReader(new IndexReader[] { DirectoryReader.Open(sdir1), DirectoryReader
				.Open(sdir2) }, true);
			multiSearcher = NewSearcher(multiReader);
			multiReaderDupls = new MultiReader(new IndexReader[] { DirectoryReader.Open(sdir1
				), DirectoryReader.Open(dir) }, true);
			multiSearcherDupls = NewSearcher(multiReaderDupls);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.AfterClass]
		public static void AfterClass()
		{
			reader.Dispose();
			multiReader.Dispose();
			multiReaderDupls.Dispose();
			dir.Dispose();
			sdir1.Dispose();
			sdir2.Dispose();
			reader = multiReader = multiReaderDupls = null;
			searcher = multiSearcher = multiSearcherDupls = null;
			dir = sdir1 = sdir2 = null;
		}

		private Query ExtractInnerQuery(Query q)
		{
			if (q is ConstantScoreQuery)
			{
				// wrapped as ConstantScoreQuery
				q = ((ConstantScoreQuery)q).GetQuery();
			}
			return q;
		}

		private Term ExtractTerm(Query q)
		{
			q = ExtractInnerQuery(q);
			return ((TermQuery)q).GetTerm();
		}

		private void CheckBooleanQueryOrder(Query q)
		{
			q = ExtractInnerQuery(q);
			BooleanQuery bq = (BooleanQuery)q;
			Term last = null;
			Term act;
			foreach (BooleanClause clause in bq.Clauses())
			{
				act = ExtractTerm(clause.GetQuery());
				if (last != null)
				{
					IsTrue("sort order of terms in BQ violated", last.CompareTo
						(act) < 0);
				}
				last = act;
			}
		}

		/// <exception cref="System.Exception"></exception>
		private void CheckDuplicateTerms(MultiTermQuery.RewriteMethod method)
		{
			MultiTermQuery mtq = TermRangeQuery.NewStringRange("data", "2", "7", true, true);
			mtq.SetRewriteMethod(method);
			Query q1 = searcher.Rewrite(mtq);
			Query q2 = multiSearcher.Rewrite(mtq);
			Query q3 = multiSearcherDupls.Rewrite(mtq);
			if (VERBOSE)
			{
				System.Console.Out.WriteLine();
				System.Console.Out.WriteLine("single segment: " + q1);
				System.Console.Out.WriteLine("multi segment: " + q2);
				System.Console.Out.WriteLine("multi segment with duplicates: " + q3);
			}
			AreEqual("The multi-segment case must produce same rewritten query"
				, q1, q2);
			AreEqual("The multi-segment case with duplicates must produce same rewritten query"
				, q1, q3);
			CheckBooleanQueryOrder(q1);
			CheckBooleanQueryOrder(q2);
			CheckBooleanQueryOrder(q3);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestRewritesWithDuplicateTerms()
		{
			CheckDuplicateTerms(MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE);
			CheckDuplicateTerms(MultiTermQuery.CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE);
			// use a large PQ here to only test duplicate terms and dont mix up when all scores are equal
			CheckDuplicateTerms(new MultiTermQuery.TopTermsScoringBooleanQueryRewrite(1024));
			CheckDuplicateTerms(new MultiTermQuery.TopTermsBoostOnlyBooleanQueryRewrite(1024)
				);
			// Test auto rewrite (but only boolean mode), so we set the limits to large values to always get a BQ
			MultiTermQuery.ConstantScoreAutoRewrite rewrite = new MultiTermQuery.ConstantScoreAutoRewrite
				();
			rewrite.SetTermCountCutoff(int.MaxValue);
			rewrite.SetDocCountPercent(100.);
			CheckDuplicateTerms(rewrite);
		}

		private void CheckBooleanQueryBoosts(BooleanQuery bq)
		{
			foreach (BooleanClause clause in bq.Clauses())
			{
				TermQuery mtq = (TermQuery)clause.GetQuery();
				AreEqual("Parallel sorting of boosts in rewrite mode broken"
					, float.ParseFloat(mtq.GetTerm().Text()), mtq.GetBoost(), 0);
			}
		}

		/// <exception cref="System.Exception"></exception>
		private void CheckBoosts(MultiTermQuery.RewriteMethod method)
		{
			MultiTermQuery mtq = new _MultiTermQuery_158("data");
			mtq.SetRewriteMethod(method);
			Query q1 = searcher.Rewrite(mtq);
			Query q2 = multiSearcher.Rewrite(mtq);
			Query q3 = multiSearcherDupls.Rewrite(mtq);
			if (VERBOSE)
			{
				System.Console.Out.WriteLine();
				System.Console.Out.WriteLine("single segment: " + q1);
				System.Console.Out.WriteLine("multi segment: " + q2);
				System.Console.Out.WriteLine("multi segment with duplicates: " + q3);
			}
			AreEqual("The multi-segment case must produce same rewritten query"
				, q1, q2);
			AreEqual("The multi-segment case with duplicates must produce same rewritten query"
				, q1, q3);
			CheckBooleanQueryBoosts((BooleanQuery)q1);
			CheckBooleanQueryBoosts((BooleanQuery)q2);
			CheckBooleanQueryBoosts((BooleanQuery)q3);
		}

		private sealed class _MultiTermQuery_158 : MultiTermQuery
		{
			public _MultiTermQuery_158(string baseArg1) : base(baseArg1)
			{
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected override TermsEnum GetTermsEnum(Terms terms, AttributeSource atts)
			{
				return new _TermRangeTermsEnum_161(terms.IEnumerator(null), new BytesRef("2"), new BytesRef
					("7"), true, true);
			}

			private sealed class _TermRangeTermsEnum_161 : TermRangeTermsEnum
			{
				public _TermRangeTermsEnum_161(TermsEnum baseArg1, BytesRef baseArg2, BytesRef baseArg3
					, bool baseArg4, bool baseArg5) : base(baseArg1, baseArg2, baseArg3, baseArg4, baseArg5
					)
				{
					this.boostAtt = this.Attributes().AddAttribute<BoostAttribute>();
				}

				internal readonly BoostAttribute boostAtt;

				protected override FilteredTermsEnum.AcceptStatus Accept(BytesRef term)
				{
					this.boostAtt.SetBoost(float.ParseFloat(term.Utf8ToString()));
					return base.Accept(term);
				}
			}

			public override string ToString(string field)
			{
				return "dummy";
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestBoosts()
		{
			CheckBoosts(MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE);
			// use a large PQ here to only test boosts and dont mix up when all scores are equal
			CheckBoosts(new MultiTermQuery.TopTermsScoringBooleanQueryRewrite(1024));
		}

		/// <exception cref="System.Exception"></exception>
		private void CheckMaxClauseLimitation(MultiTermQuery.RewriteMethod method)
		{
			int savedMaxClauseCount = BooleanQuery.GetMaxClauseCount();
			BooleanQuery.SetMaxClauseCount(3);
			MultiTermQuery mtq = TermRangeQuery.NewStringRange("data", "2", "7", true, true);
			mtq.SetRewriteMethod(method);
			try
			{
				multiSearcherDupls.Rewrite(mtq);
				Fail("Should throw BooleanQuery.TooManyClauses");
			}
			catch (BooleanQuery.TooManyClauses e)
			{
				//  Maybe remove this 
				//HM:revisit 
				//assert in later versions, when internal API changes:
				AreEqual("Should throw BooleanQuery.TooManyClauses with a stacktrace containing checkMaxClauseCount()"
					, "checkMaxClauseCount", e.GetStackTrace()[0].GetMethodName());
			}
			finally
			{
				BooleanQuery.SetMaxClauseCount(savedMaxClauseCount);
			}
		}

		/// <exception cref="System.Exception"></exception>
		private void CheckNoMaxClauseLimitation(MultiTermQuery.RewriteMethod method)
		{
			int savedMaxClauseCount = BooleanQuery.GetMaxClauseCount();
			BooleanQuery.SetMaxClauseCount(3);
			MultiTermQuery mtq = TermRangeQuery.NewStringRange("data", "2", "7", true, true);
			mtq.SetRewriteMethod(method);
			try
			{
				multiSearcherDupls.Rewrite(mtq);
			}
			finally
			{
				BooleanQuery.SetMaxClauseCount(savedMaxClauseCount);
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestMaxClauseLimitations()
		{
			CheckMaxClauseLimitation(MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE);
			CheckMaxClauseLimitation(MultiTermQuery.CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE);
			CheckNoMaxClauseLimitation(MultiTermQuery.CONSTANT_SCORE_FILTER_REWRITE);
			CheckNoMaxClauseLimitation(MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT);
			CheckNoMaxClauseLimitation(new MultiTermQuery.TopTermsScoringBooleanQueryRewrite(
				1024));
			CheckNoMaxClauseLimitation(new MultiTermQuery.TopTermsBoostOnlyBooleanQueryRewrite
				(1024));
		}
	}
}
