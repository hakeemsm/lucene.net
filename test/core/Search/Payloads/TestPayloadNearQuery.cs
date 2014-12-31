/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.IO;
using NUnit.Framework;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Test.Analysis.Tokenattributes;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Payloads;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Search.Spans;
using Lucene.Net.Store;
using Lucene.Net.Util;


namespace Lucene.Net.Search.Payloads
{
	public class TestPayloadNearQuery : LuceneTestCase
	{
		private static IndexSearcher searcher;

		private static IndexReader reader;

		private static Directory directory;

		private static TestPayloadNearQuery.BoostingSimilarity similarity = new TestPayloadNearQuery.BoostingSimilarity
			();

		private static byte[] payload2 = new byte[] { 2 };

		private static byte[] payload4 = new byte[] { 4 };

		private class PayloadAnalyzer : Analyzer
		{
			protected override Analyzer.TokenStreamComponents CreateComponents(string fieldName
				, StreamReader reader)
			{
				Tokenizer result = new MockTokenizer(reader, MockTokenizer.SIMPLE, true);
				return new Analyzer.TokenStreamComponents(result, new TestPayloadNearQuery.PayloadFilter
					(result, fieldName));
			}
		}

		private class PayloadFilter : TokenFilter
		{
			private readonly string fieldName;

			private int numSeen = 0;

			private readonly PayloadAttribute payAtt;

			public PayloadFilter(TokenStream input, string fieldName) : base(input)
			{
				this.fieldName = fieldName;
				payAtt = AddAttribute<PayloadAttribute>();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override bool IncrementToken()
			{
				bool result = false;
				if (input.IncrementToken())
				{
					if (numSeen % 2 == 0)
					{
						payAtt.Payload = (new BytesRef(payload2));
					}
					else
					{
						payAtt.Payload = (new BytesRef(payload4));
					}
					numSeen++;
					result = true;
				}
				return result;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Reset()
			{
				base.Reset();
				this.numSeen = 0;
			}
		}

		private PayloadNearQuery NewPhraseQuery(string fieldName, string phrase, bool inOrder
			, PayloadFunction function)
		{
			string[] words = phrase.Split("[\\s]+");
			SpanQuery[] clauses = new SpanQuery[words.Length];
			for (int i = 0; i < clauses.Length; i++)
			{
				clauses[i] = new SpanTermQuery(new Term(fieldName, words[i]));
			}
			return new PayloadNearQuery(clauses, 0, inOrder, function);
		}

		/// <exception cref="System.Exception"></exception>
		[BeforeClass]
		public static void BeforeClass()
		{
			directory = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), directory, NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new TestPayloadNearQuery.PayloadAnalyzer()).SetSimilarity
				(similarity));
			//writer.infoStream = System.out;
			for (int i = 0; i < 1000; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(NewTextField("field", English.IntToEnglish(i), Field.Store.YES));
				string txt = English.IntToEnglish(i) + ' ' + English.IntToEnglish(i + 1);
				doc.Add(NewTextField("field2", txt, Field.Store.YES));
				writer.AddDocument(doc);
			}
			reader = writer.Reader;
			writer.Dispose();
			searcher = NewSearcher(reader);
			searcher.SetSimilarity(similarity);
		}

		/// <exception cref="System.Exception"></exception>
		[AfterClass]
		public static void AfterClass()
		{
			searcher = null;
			reader.Dispose();
			reader = null;
			directory.Dispose();
			directory = null;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void Test()
		{
			PayloadNearQuery query;
			TopDocs hits;
			query = NewPhraseQuery("field", "twenty two", true, new AveragePayloadFunction());
			QueryUtils.Check(query);
			// all 10 hits should have score = 3 because adjacent terms have payloads of 2,4
			// and all the similarity factors are set to 1
			hits = searcher.Search(query, null, 100);
			IsTrue("hits is null and it shouldn't be", hits != null);
			IsTrue("should be 10 hits", hits.TotalHits == 10);
			for (int j = 0; j < hits.ScoreDocs.Length; j++)
			{
				ScoreDoc doc = hits.ScoreDocs[j];
				IsTrue(doc.score + " does not equal: " + 3, doc.score == 3
					);
			}
			for (int i = 1; i < 10; i++)
			{
				query = NewPhraseQuery("field", English.IntToEnglish(i) + " hundred", true, new AveragePayloadFunction
					());
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: run query=" + query);
				}
				// all should have score = 3 because adjacent terms have payloads of 2,4
				// and all the similarity factors are set to 1
				hits = searcher.Search(query, null, 100);
				IsTrue("hits is null and it shouldn't be", hits != null);
				AreEqual("should be 100 hits", 100, hits.TotalHits);
				for (int j_1 = 0; j_1 < hits.ScoreDocs.Length; j_1++)
				{
					ScoreDoc doc = hits.ScoreDocs[j_1];
					//        System.out.println("Doc: " + doc.toString());
					//        System.out.println("Explain: " + searcher.explain(query, doc.Doc));
					IsTrue(doc.score + " does not equal: " + 3, doc.score == 3
						);
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestPayloadNear()
		{
			SpanNearQuery q1;
			SpanNearQuery q2;
			PayloadNearQuery query;
			//SpanNearQuery(clauses, 10000, false)
			q1 = SpanNearQuery("field2", "twenty two");
			q2 = SpanNearQuery("field2", "twenty three");
			SpanQuery[] clauses = new SpanQuery[2];
			clauses[0] = q1;
			clauses[1] = q2;
			query = new PayloadNearQuery(clauses, 10, false);
			//System.out.println(query.toString());
			AreEqual(12, searcher.Search(query, null, 100).TotalHits);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestAverageFunction()
		{
			PayloadNearQuery query;
			TopDocs hits;
			query = NewPhraseQuery("field", "twenty two", true, new AveragePayloadFunction());
			QueryUtils.Check(query);
			// all 10 hits should have score = 3 because adjacent terms have payloads of 2,4
			// and all the similarity factors are set to 1
			hits = searcher.Search(query, null, 100);
			IsTrue("hits is null and it shouldn't be", hits != null);
			IsTrue("should be 10 hits", hits.TotalHits == 10);
			for (int j = 0; j < hits.ScoreDocs.Length; j++)
			{
				ScoreDoc doc = hits.ScoreDocs[j];
				IsTrue(doc.score + " does not equal: " + 3, doc.score == 3
					);
				Explanation explain = searcher.Explain(query, hits.ScoreDocs[j].Doc);
				string exp = explain.ToString();
				IsTrue(exp, exp.IndexOf("AveragePayloadFunction") > -1);
				IsTrue(hits.ScoreDocs[j].score + " explain value does not equal: "
					 + 3, explain.GetValue() == 3f);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestMaxFunction()
		{
			PayloadNearQuery query;
			TopDocs hits;
			query = NewPhraseQuery("field", "twenty two", true, new MaxPayloadFunction());
			QueryUtils.Check(query);
			// all 10 hits should have score = 4 (max payload value)
			hits = searcher.Search(query, null, 100);
			IsTrue("hits is null and it shouldn't be", hits != null);
			IsTrue("should be 10 hits", hits.TotalHits == 10);
			for (int j = 0; j < hits.ScoreDocs.Length; j++)
			{
				ScoreDoc doc = hits.ScoreDocs[j];
				IsTrue(doc.score + " does not equal: " + 4, doc.score == 4
					);
				Explanation explain = searcher.Explain(query, hits.ScoreDocs[j].Doc);
				string exp = explain.ToString();
				IsTrue(exp, exp.IndexOf("MaxPayloadFunction") > -1);
				IsTrue(hits.ScoreDocs[j].score + " explain value does not equal: "
					 + 4, explain.GetValue() == 4f);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestMinFunction()
		{
			PayloadNearQuery query;
			TopDocs hits;
			query = NewPhraseQuery("field", "twenty two", true, new MinPayloadFunction());
			QueryUtils.Check(query);
			// all 10 hits should have score = 2 (min payload value)
			hits = searcher.Search(query, null, 100);
			IsTrue("hits is null and it shouldn't be", hits != null);
			IsTrue("should be 10 hits", hits.TotalHits == 10);
			for (int j = 0; j < hits.ScoreDocs.Length; j++)
			{
				ScoreDoc doc = hits.ScoreDocs[j];
				IsTrue(doc.score + " does not equal: " + 2, doc.score == 2
					);
				Explanation explain = searcher.Explain(query, hits.ScoreDocs[j].Doc);
				string exp = explain.ToString();
				IsTrue(exp, exp.IndexOf("MinPayloadFunction") > -1);
				IsTrue(hits.ScoreDocs[j].score + " explain value does not equal: "
					 + 2, explain.GetValue() == 2f);
			}
		}

		private SpanQuery[] GetClauses()
		{
			SpanNearQuery q1;
			SpanNearQuery q2;
			q1 = SpanNearQuery("field2", "twenty two");
			q2 = SpanNearQuery("field2", "twenty three");
			SpanQuery[] clauses = new SpanQuery[2];
			clauses[0] = q1;
			clauses[1] = q2;
			return clauses;
		}

		private SpanNearQuery SpanNearQuery(string fieldName, string words)
		{
			string[] wordList = words.Split("[\\s]+");
			SpanQuery[] clauses = new SpanQuery[wordList.Length];
			for (int i = 0; i < clauses.Length; i++)
			{
				clauses[i] = new PayloadTermQuery(new Term(fieldName, wordList[i]), new AveragePayloadFunction
					());
			}
			return new SpanNearQuery(clauses, 10000, false);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestLongerSpan()
		{
			PayloadNearQuery query;
			TopDocs hits;
			query = NewPhraseQuery("field", "nine hundred ninety nine", true, new AveragePayloadFunction
				());
			hits = searcher.Search(query, null, 100);
			IsTrue("hits is null and it shouldn't be", hits != null);
			ScoreDoc doc = hits.ScoreDocs[0];
			//    System.out.println("Doc: " + doc.toString());
			//    System.out.println("Explain: " + searcher.explain(query, doc.Doc));
			IsTrue("there should only be one hit", hits.TotalHits == 1
				);
			// should have score = 3 because adjacent terms have payloads of 2,4
			IsTrue(doc.score + " does not equal: " + 3, doc.score == 3
				);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestComplexNested()
		{
			PayloadNearQuery query;
			TopDocs hits;
			// combine ordered and unordered spans with some nesting to make sure all payloads are counted
			SpanQuery q1 = NewPhraseQuery("field", "nine hundred", true, new AveragePayloadFunction
				());
			SpanQuery q2 = NewPhraseQuery("field", "ninety nine", true, new AveragePayloadFunction
				());
			SpanQuery q3 = NewPhraseQuery("field", "nine ninety", false, new AveragePayloadFunction
				());
			SpanQuery q4 = NewPhraseQuery("field", "hundred nine", false, new AveragePayloadFunction
				());
			SpanQuery[] clauses = new SpanQuery[] { new PayloadNearQuery(new SpanQuery[] { q1
				, q2 }, 0, true), new PayloadNearQuery(new SpanQuery[] { q3, q4 }, 0, false) };
			query = new PayloadNearQuery(clauses, 0, false);
			hits = searcher.Search(query, null, 100);
			IsTrue("hits is null and it shouldn't be", hits != null);
			// should be only 1 hit - doc 999
			IsTrue("should only be one hit", hits.ScoreDocs.Length == 
				1);
			// the score should be 3 - the average of all the underlying payloads
			ScoreDoc doc = hits.ScoreDocs[0];
			//    System.out.println("Doc: " + doc.toString());
			//    System.out.println("Explain: " + searcher.explain(query, doc.Doc));
			IsTrue(doc.score + " does not equal: " + 3, doc.score == 3
				);
		}

		internal class BoostingSimilarity : DefaultSimilarity
		{
			public override float QueryNorm(float sumOfSquaredWeights)
			{
				return 1.0f;
			}

			public override float Coord(int overlap, int maxOverlap)
			{
				return 1.0f;
			}

			public override float ScorePayload(int docId, int start, int end, BytesRef payload
				)
			{
				//we know it is size 4 here, so ignore the offset/length
				return payload.bytes[payload.offset];
			}

			//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
			//Make everything else 1 so we see the effect of the payload
			//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
			public override float LengthNorm(FieldInvertState state)
			{
				return state.GetBoost();
			}

			public override float SloppyFreq(int distance)
			{
				return 1.0f;
			}

			public override float Tf(float freq)
			{
				return 1.0f;
			}

			// idf used for phrase queries
			public override Explanation IdfExplain(CollectionStatistics collectionStats, TermStatistics
				[] termStats)
			{
				return new Explanation(1.0f, "Inexplicable");
			}
		}
	}
}
