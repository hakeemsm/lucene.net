/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace Lucene.Net.TestFramework.Search
{
	/// <summary>Utility class for asserting expected hits in tests.</summary>
	/// <remarks>Utility class for asserting expected hits in tests.</remarks>
	public class CheckHits
	{
		/// <summary>
		/// Some explains methods calculate their values though a slightly
		/// different  order of operations from the actual scoring method ...
		/// </summary>
		/// <remarks>
		/// Some explains methods calculate their values though a slightly
		/// different  order of operations from the actual scoring method ...
		/// this allows for a small amount of relative variation
		/// </remarks>
		public static float EXPLAIN_SCORE_TOLERANCE_DELTA = 0.001f;

		/// <summary>
		/// In general we use a relative epsilon, but some tests do crazy things
		/// like boost documents with 0, creating tiny tiny scores where the
		/// relative difference is large but the absolute difference is tiny.
		/// </summary>
		/// <remarks>
		/// In general we use a relative epsilon, but some tests do crazy things
		/// like boost documents with 0, creating tiny tiny scores where the
		/// relative difference is large but the absolute difference is tiny.
		/// we ensure the the epsilon is always at least this big.
		/// </remarks>
		public static float EXPLAIN_SCORE_TOLERANCE_MINIMUM = 1e-6f;

		/// <summary>
		/// Tests that all documents up to maxDoc which are *not* in the
		/// expected result set, have an explanation which indicates that
		/// the document does not match
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public static void CheckNoMatchExplanations(Query q, string defaultFieldName, IndexSearcher
			 searcher, int[] results)
		{
			string d = q.ToString(defaultFieldName);
			ICollection<int> ignore = results.ToList();
		    int maxDoc = searcher.IndexReader.MaxDoc;
			for (int doc = 0; doc < maxDoc; doc++)
			{
				if (ignore.Contains(doc))
				{
					continue;
				}
				Explanation exp = searcher.Explain(q, doc);
			}
		}

		
		//assert.assertNotNull("Explanation of [["+d+"]] for #"+doc+" is null", exp);
		
		//assert.assertFalse("Explanation of [["+d+"]] for #"+doc+
		/// <summary>
		/// Tests that a query matches the an expected set of documents using a
		/// HitCollector.
		/// </summary>
		/// <remarks>
		/// Tests that a query matches the an expected set of documents using a
		/// HitCollector.
		/// <p>
		/// Note that when using the HitCollector API, documents will be collected
		/// if they "match" regardless of what their score is.
		/// </p>
		/// </remarks>
		/// <param name="query">the query to test</param>
		/// <param name="searcher">the searcher to test the query against</param>
		/// <param name="defaultFieldName">used for displaying the query in assertion messages
		/// 	</param>
		/// <param name="results">a list of documentIds that must match the query</param>
		/// <seealso cref="CheckHits(Sharpen.Random, Query, string, IndexSearcher, int[])">CheckHits(Sharpen.Random, Query, string, IndexSearcher, int[])
		/// 	</seealso>
		/// <exception cref="System.IO.IOException"></exception>
		public static void CheckHitCollector(Random random, Query query, string defaultFieldName
			, IndexSearcher searcher, int[] results)
		{
			QueryUtils.Check(random, query, searcher);
			ICollection<int> correct = results.ToList();
		    ICollection<int> actual = new List<int>();
			Collector c = new SetCollector(actual);
			searcher.Search(query, c);
			
			//assert.assertEquals("Simple: " + query.toString(defaultFieldName), correct, actual);
			for (int i_1 = -1; i_1 < 2; i_1++)
			{
				actual.Clear();
				IndexSearcher s = QueryUtils.WrapUnderlyingReader(random, searcher, i_1);
				s.Search(query, c);
			}
		}

		/// <summary>Just collects document ids into a set.</summary>
		/// <remarks>Just collects document ids into a set.</remarks>
		public class SetCollector : Collector
		{
			internal readonly ICollection<int> bag;

			public SetCollector(ICollection<int> bag)
			{
				//HM:revisit 
				//assert.assertEquals("Wrap Reader " + i + ": " + query.toString(defaultFieldName), correct, actual);
				this.bag = bag;
			}

			private int @base = 0;

			/// <exception cref="System.IO.IOException"></exception>
			public override void SetScorer(Scorer scorer)
			{
			}

			public override void Collect(int doc)
			{
				bag.Add(doc + @base);
			}

			public override void SetNextReader(AtomicReaderContext context)
			{
				@base = context.docBase;
			}

			public override bool AcceptsDocsOutOfOrder
			{
			    get { return true; }
			}
		}

		/// <summary>Tests that a query matches the an expected set of documents using Hits.</summary>
		/// <remarks>
		/// Tests that a query matches the an expected set of documents using Hits.
		/// <p>
		/// Note that when using the Hits API, documents will only be returned
		/// if they have a positive normalized score.
		/// </p>
		/// </remarks>
		/// <param name="query">the query to test</param>
		/// <param name="searcher">the searcher to test the query against</param>
		/// <param name="defaultFieldName">used for displaing the query in assertion messages
		/// 	</param>
		/// <param name="results">a list of documentIds that must match the query</param>
		/// <seealso cref="CheckHitCollector(Sharpen.Random, Query, string, IndexSearcher, int[])
		/// 	">CheckHitCollector(Sharpen.Random, Query, string, IndexSearcher, int[])</seealso>
		/// <exception cref="System.IO.IOException"></exception>
		public static void Check(Random random, Query query, string defaultFieldName, 
			IndexSearcher searcher, int[] results)
		{
			ScoreDoc[] hits = searcher.Search(query, 1000).ScoreDocs;
			ICollection<int> correct = results.ToList();
		    ICollection<int> actual = new List<int>();
			for (int i_1 = 0; i_1 < hits.Length; i_1++)
			{
				actual.Add(hits[i_1].Doc);
			}
			
			//assert.assertEquals(query.toString(defaultFieldName), correct, actual);
			QueryUtils.Check(random, query, searcher, LuceneTestCase.Rarely(random));
		}

		/// <summary>Tests that a Hits has an expected order of documents</summary>
		public static void CheckDocIds(string mes, int[] results, ScoreDoc[] hits)
		{
			
			//assert.assertEquals(mes + " nr of hits", hits.length, results.length);
            //for (int i = 0; i < results.Length; i++)
            //{
            //}
		}

		//HM:revisit 
		//assert.assertEquals(mes + " doc nrs for hit " + i, results[i], hits[i].doc);
		/// <summary>
		/// Tests that two queries have an expected order of documents,
		/// and that the two queries have the same score values.
		/// </summary>
		/// <remarks>
		/// Tests that two queries have an expected order of documents,
		/// and that the two queries have the same score values.
		/// </remarks>
		public static void CheckHitsQuery(Query query, ScoreDoc[] hits1, ScoreDoc[] hits2
			, int[] results)
		{
			CheckDocIds("hits1", results, hits1);
			CheckDocIds("hits2", results, hits2);
			CheckEqual(query, hits1, hits2);
		}

		public static void CheckEqual(Query query, ScoreDoc[] hits1, ScoreDoc[] hits2)
		{
			float scoreTolerance = 1.0e-6f;
			if (hits1.Length != hits2.Length)
			{
			}
			
			//assert.fail("Unequal lengths: hits1="+hits1.length+",hits2="+hits2.length);
			for (int i = 0; i < hits1.Length; i++)
			{
				if (hits1[i].Doc != hits2[i].Doc)
				{
				}
				
				//assert.fail("Hit " + i + " docnumbers don't match\n" + hits2str(hits1, hits2,0,0) + "for query:" + query.toString());
				if ((hits1[i].Doc != hits2[i].Doc) || Math.Abs(hits1[i].Score - hits2[i].Score) >
					 scoreTolerance)
				{
				}
			}
		}

		//HM:revisit 
		//assert.fail("Hit " + i + ", doc nrs " + hits1[i].doc + " and " + hits2[i].doc
		public static string Hits2str(ScoreDoc[] hits1, ScoreDoc[] hits2, int start, int 
			end)
		{
			StringBuilder sb = new StringBuilder();
			int len1 = hits1 == null ? 0 : hits1.Length;
			int len2 = hits2 == null ? 0 : hits2.Length;
			if (end <= 0)
			{
				end = Math.Max(len1, len2);
			}
			sb.Append("Hits length1=").Append(len1).Append("\tlength2=").Append(len2);
			sb.Append('\n');
			for (int i = start; i < end; i++)
			{
				sb.Append("hit=").Append(i).Append(':');
				if (i < len1)
				{
					sb.Append(" doc").Append(hits1[i].Doc).Append('=').Append(hits1[i].Score);
				}
				else
				{
					sb.Append("               ");
				}
				sb.Append(",\t");
				if (i < len2)
				{
					sb.Append(" doc").Append(hits2[i].Doc).Append('=').Append(hits2[i].Score);
				}
				sb.Append('\n');
			}
			return sb.ToString();
		}

		public static string TopdocsString(TopDocs docs, int start, int end)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("TopDocs totalHits=").Append(docs.TotalHits).Append(" top=").Append(docs.ScoreDocs.Length).Append('\n');
			if (end <= 0)
			{
				end = docs.ScoreDocs.Length;
			}
			else
			{
				end = Math.Min(end, docs.ScoreDocs.Length);
			}
			for (int i = start; i < end; i++)
			{
				sb.Append('\t');
				sb.Append(i);
				sb.Append(") doc=");
				sb.Append(docs.ScoreDocs[i].Doc);
				sb.Append("\tscore=");
				sb.Append(docs.ScoreDocs[i].Score);
				sb.Append('\n');
			}
			return sb.ToString();
		}

		/// <summary>
		/// Asserts that the explanation value for every document matching a
		/// query corresponds with the true score.
		/// </summary>
		/// <remarks>
		/// Asserts that the explanation value for every document matching a
		/// query corresponds with the true score.
		/// </remarks>
		/// <seealso cref="ExplanationAsserter">ExplanationAsserter</seealso>
		/// <seealso cref="CheckExplanations(Query, string, IndexSearcher, bool)">
		/// for a
		/// "deep" testing of the explanation details.
		/// </seealso>
		/// <param name="query">the query to test</param>
		/// <param name="searcher">the searcher to test the query against</param>
		/// <param name="defaultFieldName">used for displaing the query in assertion messages
		/// 	</param>
		/// <exception cref="System.IO.IOException"></exception>
		public static void CheckExplanations(Query query, string defaultFieldName, IndexSearcher
			 searcher)
		{
			CheckExplanations(query, defaultFieldName, searcher, false);
		}

		/// <summary>
		/// Asserts that the explanation value for every document matching a
		/// query corresponds with the true score.
		/// </summary>
		/// <remarks>
		/// Asserts that the explanation value for every document matching a
		/// query corresponds with the true score.  Optionally does "deep"
		/// testing of the explanation details.
		/// </remarks>
		/// <seealso cref="ExplanationAsserter">ExplanationAsserter</seealso>
		/// <param name="query">the query to test</param>
		/// <param name="searcher">the searcher to test the query against</param>
		/// <param name="defaultFieldName">used for displaing the query in assertion messages
		/// 	</param>
		/// <param name="deep">indicates whether a deep comparison of sub-Explanation details should be executed
		/// 	</param>
		/// <exception cref="System.IO.IOException"></exception>
		public static void CheckExplanations(Query query, string defaultFieldName, IndexSearcher
			 searcher, bool deep)
		{
			searcher.Search(query, new CheckHits.ExplanationAsserter(query, defaultFieldName, 
				searcher, deep));
		}

		/// <summary>
		/// returns a reasonable epsilon for comparing two floats,
		/// where minor differences are acceptable such as score vs.
		/// </summary>
		/// <remarks>
		/// returns a reasonable epsilon for comparing two floats,
		/// where minor differences are acceptable such as score vs. explain
		/// </remarks>
		public static float ExplainToleranceDelta(float f1, float f2)
		{
			return Math.Max(EXPLAIN_SCORE_TOLERANCE_MINIMUM, Math.Max(Math.Abs(f1), Math.Abs(
				f2)) * EXPLAIN_SCORE_TOLERANCE_DELTA);
		}

		/// <summary>
		/// //HM:revisit
		/// //assert that an explanation has the expected score, and optionally that its
		/// sub-details max/sum/factor match to that score.
		/// </summary>
		/// <remarks>
		/// //HM:revisit
		/// //assert that an explanation has the expected score, and optionally that its
		/// sub-details max/sum/factor match to that score.
		/// </remarks>
		/// <param name="q">String representation of the query for assertion messages</param>
		/// <param name="doc">Document ID for assertion messages</param>
		/// <param name="score">Real score value of doc with query q</param>
		/// <param name="deep">indicates whether a deep comparison of sub-Explanation details should be executed
		/// 	</param>
		/// <param name="expl">The Explanation to match against score</param>
		public static void VerifyExplanation(string q, int doc, float score, bool deep, Explanation
			 expl)
		{
			float value = expl.Value;
			//HM:revisit 
			//assert.assertEquals(q+": score(doc="+doc+")="+score+ " != explanationScore="+value+" Explanation: "+expl,
			//score,value,explainToleranceDelta(score, value));
			if (!deep)
			{
				return;
			}
			Explanation[] detail = expl.GetDetails();
			// TODO: can we improve this entire method? its really geared to work only with TF/IDF
			if (expl.Description.EndsWith("computed from:"))
			{
				return;
			}
			// something more complicated.
			if (detail != null)
			{
				if (detail.Length == 1)
				{
					// simple containment, unless its a freq of: (which lets a query explain how the freq is calculated), 
					// just verify contained expl has same score
					if (!expl.Description.EndsWith("with freq of:"))
					{
						VerifyExplanation(q, doc, score, deep, detail[0]);
					}
				}
				else
				{
					// explanation must either:
					// - end with one of: "product of:", "sum of:", "max of:", or
					// - have "max plus <x> times others" (where <x> is float).
					float x = 0;
					string descr = expl.Description.ToLower(CultureInfo.CurrentCulture);
					bool productOf = descr.EndsWith("product of:");
					bool sumOf = descr.EndsWith("sum of:");
					bool maxOf = descr.EndsWith("max of:");
					bool maxTimesOthers = false;
					if (!(productOf || sumOf || maxOf))
					{
						// maybe 'max plus x times others'
						int k1 = descr.IndexOf("max plus ");
						if (k1 >= 0)
						{
							k1 += "max plus ".Length;
							int k2 = descr.IndexOf(" ", k1);
							try
							{
								x = float.Parse(descr.Substring(k1, (k2-k1)).Trim()); //.NET Port. Substring behavior is different
								if (descr.Substring(k2).Trim().Equals("times others of:"))
								{
									maxTimesOthers = true;
								}
							}
							catch (FormatException)
							{
							}
						}
					}
					// TODO: this is a TERRIBLE assertion!!!!
					//HM:revisit 
					//assert.assertTrue(
					float sum = 0;
					float product = 1;
					float max = 0;
					for (int i = 0; i < detail.Length; i++)
					{
						float dval = detail[i].Value;
						VerifyExplanation(q, doc, dval, deep, detail[i]);
						product *= dval;
						sum += dval;
						max = Math.Max(max, dval);
					}
					float combined = 0;
					if (productOf)
					{
						combined = product;
					}
					else
					{
						if (sumOf)
						{
							combined = sum;
						}
						else
						{
							if (maxOf)
							{
								combined = max;
							}
							else
							{
								if (maxTimesOthers)
								{
									combined = max + x * (sum - max);
								}
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// an IndexSearcher that implicitly checks hte explanation of every match
		/// whenever it executes a search.
		/// </summary>
		/// <remarks>
		/// an IndexSearcher that implicitly checks hte explanation of every match
		/// whenever it executes a search.
		/// </remarks>
		/// <seealso cref="ExplanationAsserter">ExplanationAsserter</seealso>
		public class ExplanationAssertingSearcher : IndexSearcher
		{
			public ExplanationAssertingSearcher(IndexReader r) : base(r)
			{
			}

			//HM:revisit 
			//assert.assertTrue("should never get here!",false);
			//HM:revisit 
			//assert.assertEquals(q+": actual subDetails combined=="+combined+
			/// <exception cref="System.IO.IOException"></exception>
			protected internal virtual void CheckExplanations(Query q)
			{
				base.Search(q, null, new CheckHits.ExplanationAsserter(q, null, this));
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override TopFieldDocs Search(Query query, Filter filter, int n, Sort sort)
			{
				CheckExplanations(query);
				return base.Search(query, filter, n, sort);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Search(Query query, Collector results)
			{
				CheckExplanations(query);
				base.Search(query, results);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Search(Query query, Filter filter, Collector results)
			{
				CheckExplanations(query);
				base.Search(query, filter, results);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override TopDocs Search(Query query, Filter filter, int n)
			{
				CheckExplanations(query);
				return base.Search(query, filter, n);
			}
		}

		/// <summary>
		/// Asserts that the score explanation for every document matching a
		/// query corresponds with the true score.
		/// </summary>
		/// <remarks>
		/// Asserts that the score explanation for every document matching a
		/// query corresponds with the true score.
		/// NOTE: this HitCollector should only be used with the Query and Searcher
		/// specified at when it is constructed.
		/// </remarks>
		/// <seealso cref="Check.VerifyExplanation(string, int, float, bool, Explanation)
		/// 	">CheckHits.VerifyExplanation(string, int, float, bool, Explanation)</seealso>
		public class ExplanationAsserter : Collector
		{
			internal Query q;

			internal IndexSearcher s;

			internal string d;

			internal bool deep;

			internal Scorer scorer;

			private int @base = 0;

			/// <summary>Constructs an instance which does shallow tests on the Explanation</summary>
			public ExplanationAsserter(Query q, string defaultFieldName, IndexSearcher s) : this
				(q, defaultFieldName, s, false)
			{
			}

			public ExplanationAsserter(Query q, string defaultFieldName, IndexSearcher s, bool
				 deep)
			{
				this.q = q;
				this.s = s;
				this.d = q.ToString(defaultFieldName);
				this.deep = deep;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void SetScorer(Scorer scorer)
			{
				this.scorer = scorer;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Collect(int doc)
			{
				Explanation exp = null;
				doc = doc + @base;
				try
				{
					exp = s.Explain(q, doc);
				}
				catch (IOException e)
				{
					throw new Exception("exception in hitcollector of [[" + d + "]] for #" + doc, e);
				}
				//HM:revisit 
				//assert.assertNotNull("Explanation of [["+d+"]] for #"+doc+" is null", exp);
				VerifyExplanation(d, doc, scorer.Score(), deep, exp);
			}

			//HM:revisit 
			//assert.assertTrue("Explanation of [["+d+"]] for #"+ doc + 
			public override void SetNextReader(AtomicReaderContext context)
			{
				@base = context.docBase;
			}

			public override bool AcceptsDocsOutOfOrder
			{
			    get { return true; }
			}
		}
	}
}
