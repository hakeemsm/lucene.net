/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.IO;
using NUnit.Framework;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Payloads;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Search.Spans;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search.Payloads
{
	public class TestPayloadTermQuery : LuceneTestCase
	{
		private static IndexSearcher searcher;

		private static IndexReader reader;

		private static Similarity similarity = new TestPayloadTermQuery.BoostingSimilarity
			();

		private static readonly byte[] payloadField = new byte[] { 1 };

		private static readonly byte[] payloadMultiField1 = new byte[] { 2 };

		private static readonly byte[] payloadMultiField2 = new byte[] { 4 };

		protected internal static Directory directory;

		private class PayloadAnalyzer : Analyzer
		{
			public PayloadAnalyzer() : base(PER_FIELD_REUSE_STRATEGY)
			{
			}

			protected override Analyzer.TokenStreamComponents CreateComponents(string fieldName
				, StreamReader reader)
			{
				Tokenizer result = new MockTokenizer(reader, MockTokenizer.SIMPLE, true);
				return new Analyzer.TokenStreamComponents(result, new TestPayloadTermQuery.PayloadFilter
					(result, fieldName));
			}
		}

		private class PayloadFilter : TokenFilter
		{
			private readonly string fieldName;

			private int numSeen = 0;

			private readonly PayloadAttribute payloadAtt;

			public PayloadFilter(TokenStream input, string fieldName) : base(input)
			{
				this.fieldName = fieldName;
				payloadAtt = AddAttribute<PayloadAttribute>();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override bool IncrementToken()
			{
				bool hasNext = input.IncrementToken();
				if (hasNext)
				{
					if (fieldName.Equals("field"))
					{
						payloadAtt.SetPayload(new BytesRef(payloadField));
					}
					else
					{
						if (fieldName.Equals("multiField"))
						{
							if (numSeen % 2 == 0)
							{
								payloadAtt.SetPayload(new BytesRef(payloadMultiField1));
							}
							else
							{
								payloadAtt.SetPayload(new BytesRef(payloadMultiField2));
							}
							numSeen++;
						}
					}
					return true;
				}
				else
				{
					return false;
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Reset()
			{
				base.Reset();
				this.numSeen = 0;
			}
		}

		/// <exception cref="System.Exception"></exception>
		[BeforeClass]
		public static void BeforeClass()
		{
			directory = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), directory, NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new TestPayloadTermQuery.PayloadAnalyzer()).SetSimilarity
				(similarity).SetMergePolicy(NewLogMergePolicy()));
			//writer.infoStream = System.out;
			for (int i = 0; i < 1000; i++)
			{
				Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
					();
				Field noPayloadField = NewTextField(PayloadHelper.NO_PAYLOAD_FIELD, English.IntToEnglish
					(i), Field.Store.YES);
				//noPayloadField.setBoost(0);
				doc.Add(noPayloadField);
				doc.Add(NewTextField("field", English.IntToEnglish(i), Field.Store.YES));
				doc.Add(NewTextField("multiField", English.IntToEnglish(i) + "  " + English.IntToEnglish
					(i), Field.Store.YES));
				writer.AddDocument(doc);
			}
			reader = writer.GetReader();
			writer.Close();
			searcher = NewSearcher(reader);
			searcher.SetSimilarity(similarity);
		}

		/// <exception cref="System.Exception"></exception>
		[AfterClass]
		public static void AfterClass()
		{
			searcher = null;
			reader.Close();
			reader = null;
			directory.Close();
			directory = null;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void Test()
		{
			PayloadTermQuery query = new PayloadTermQuery(new Term("field", "seventy"), new MaxPayloadFunction
				());
			TopDocs hits = searcher.Search(query, null, 100);
			NUnit.Framework.Assert.IsTrue("hits is null and it shouldn't be", hits != null);
			NUnit.Framework.Assert.IsTrue("hits Size: " + hits.totalHits + " is not: " + 100, 
				hits.totalHits == 100);
			//they should all have the exact same score, because they all contain seventy once, and we set
			//all the other similarity factors to be 1
			NUnit.Framework.Assert.IsTrue(hits.GetMaxScore() + " does not equal: " + 1, hits.
				GetMaxScore() == 1);
			for (int i = 0; i < hits.scoreDocs.Length; i++)
			{
				ScoreDoc doc = hits.scoreDocs[i];
				NUnit.Framework.Assert.IsTrue(doc.score + " does not equal: " + 1, doc.score == 1
					);
			}
			CheckHits.CheckExplanations(query, PayloadHelper.FIELD, searcher, true);
			Lucene.Net.Search.Spans.Spans spans = MultiSpansWrapper.Wrap(searcher.GetTopReaderContext
				(), query);
			NUnit.Framework.Assert.IsTrue("spans is null and it shouldn't be", spans != null);
		}

		public virtual void TestQuery()
		{
			PayloadTermQuery boostingFuncTermQuery = new PayloadTermQuery(new Term(PayloadHelper
				.MULTI_FIELD, "seventy"), new MaxPayloadFunction());
			QueryUtils.Check(boostingFuncTermQuery);
			SpanTermQuery spanTermQuery = new SpanTermQuery(new Term(PayloadHelper.MULTI_FIELD
				, "seventy"));
			NUnit.Framework.Assert.IsTrue(boostingFuncTermQuery.Equals(spanTermQuery) == spanTermQuery
				.Equals(boostingFuncTermQuery));
			PayloadTermQuery boostingFuncTermQuery2 = new PayloadTermQuery(new Term(PayloadHelper
				.MULTI_FIELD, "seventy"), new AveragePayloadFunction());
			QueryUtils.CheckUnequal(boostingFuncTermQuery, boostingFuncTermQuery2);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestMultipleMatchesPerDoc()
		{
			PayloadTermQuery query = new PayloadTermQuery(new Term(PayloadHelper.MULTI_FIELD, 
				"seventy"), new MaxPayloadFunction());
			TopDocs hits = searcher.Search(query, null, 100);
			NUnit.Framework.Assert.IsTrue("hits is null and it shouldn't be", hits != null);
			NUnit.Framework.Assert.IsTrue("hits Size: " + hits.totalHits + " is not: " + 100, 
				hits.totalHits == 100);
			//they should all have the exact same score, because they all contain seventy once, and we set
			//all the other similarity factors to be 1
			//System.out.println("Hash: " + seventyHash + " Twice Hash: " + 2*seventyHash);
			NUnit.Framework.Assert.IsTrue(hits.GetMaxScore() + " does not equal: " + 4.0, hits
				.GetMaxScore() == 4.0);
			//there should be exactly 10 items that score a 4, all the rest should score a 2
			//The 10 items are: 70 + i*100 where i in [0-9]
			int numTens = 0;
			for (int i = 0; i < hits.scoreDocs.Length; i++)
			{
				ScoreDoc doc = hits.scoreDocs[i];
				if (doc.doc % 10 == 0)
				{
					numTens++;
					NUnit.Framework.Assert.IsTrue(doc.score + " does not equal: " + 4.0, doc.score ==
						 4.0);
				}
				else
				{
					NUnit.Framework.Assert.IsTrue(doc.score + " does not equal: " + 2, doc.score == 2
						);
				}
			}
			NUnit.Framework.Assert.IsTrue(numTens + " does not equal: " + 10, numTens == 10);
			CheckHits.CheckExplanations(query, "field", searcher, true);
			Lucene.Net.Search.Spans.Spans spans = MultiSpansWrapper.Wrap(searcher.GetTopReaderContext
				(), query);
			NUnit.Framework.Assert.IsTrue("spans is null and it shouldn't be", spans != null);
			//should be two matches per document
			int count = 0;
			//100 hits times 2 matches per hit, we should have 200 in count
			while (spans.Next())
			{
				count++;
			}
			NUnit.Framework.Assert.IsTrue(count + " does not equal: " + 200, count == 200);
		}

		//Set includeSpanScore to false, in which case just the payload score comes through.
		/// <exception cref="System.Exception"></exception>
		public virtual void TestIgnoreSpanScorer()
		{
			PayloadTermQuery query = new PayloadTermQuery(new Term(PayloadHelper.MULTI_FIELD, 
				"seventy"), new MaxPayloadFunction(), false);
			IndexReader reader = DirectoryReader.Open(directory);
			IndexSearcher theSearcher = NewSearcher(reader);
			theSearcher.SetSimilarity(new TestPayloadTermQuery.FullSimilarity());
			TopDocs hits = searcher.Search(query, null, 100);
			NUnit.Framework.Assert.IsTrue("hits is null and it shouldn't be", hits != null);
			NUnit.Framework.Assert.IsTrue("hits Size: " + hits.totalHits + " is not: " + 100, 
				hits.totalHits == 100);
			//they should all have the exact same score, because they all contain seventy once, and we set
			//all the other similarity factors to be 1
			//System.out.println("Hash: " + seventyHash + " Twice Hash: " + 2*seventyHash);
			NUnit.Framework.Assert.IsTrue(hits.GetMaxScore() + " does not equal: " + 4.0, hits
				.GetMaxScore() == 4.0);
			//there should be exactly 10 items that score a 4, all the rest should score a 2
			//The 10 items are: 70 + i*100 where i in [0-9]
			int numTens = 0;
			for (int i = 0; i < hits.scoreDocs.Length; i++)
			{
				ScoreDoc doc = hits.scoreDocs[i];
				if (doc.doc % 10 == 0)
				{
					numTens++;
					NUnit.Framework.Assert.IsTrue(doc.score + " does not equal: " + 4.0, doc.score ==
						 4.0);
				}
				else
				{
					NUnit.Framework.Assert.IsTrue(doc.score + " does not equal: " + 2, doc.score == 2
						);
				}
			}
			NUnit.Framework.Assert.IsTrue(numTens + " does not equal: " + 10, numTens == 10);
			CheckHits.CheckExplanations(query, "field", searcher, true);
			Lucene.Net.Search.Spans.Spans spans = MultiSpansWrapper.Wrap(searcher.GetTopReaderContext
				(), query);
			NUnit.Framework.Assert.IsTrue("spans is null and it shouldn't be", spans != null);
			//should be two matches per document
			int count = 0;
			//100 hits times 2 matches per hit, we should have 200 in count
			while (spans.Next())
			{
				count++;
			}
			reader.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestNoMatch()
		{
			PayloadTermQuery query = new PayloadTermQuery(new Term(PayloadHelper.FIELD, "junk"
				), new MaxPayloadFunction());
			TopDocs hits = searcher.Search(query, null, 100);
			NUnit.Framework.Assert.IsTrue("hits is null and it shouldn't be", hits != null);
			NUnit.Framework.Assert.IsTrue("hits Size: " + hits.totalHits + " is not: " + 0, hits
				.totalHits == 0);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestNoPayload()
		{
			PayloadTermQuery q1 = new PayloadTermQuery(new Term(PayloadHelper.NO_PAYLOAD_FIELD
				, "zero"), new MaxPayloadFunction());
			PayloadTermQuery q2 = new PayloadTermQuery(new Term(PayloadHelper.NO_PAYLOAD_FIELD
				, "foo"), new MaxPayloadFunction());
			BooleanClause c1 = new BooleanClause(q1, BooleanClause.Occur.MUST);
			BooleanClause c2 = new BooleanClause(q2, BooleanClause.Occur.MUST_NOT);
			BooleanQuery query = new BooleanQuery();
			query.Add(c1);
			query.Add(c2);
			TopDocs hits = searcher.Search(query, null, 100);
			NUnit.Framework.Assert.IsTrue("hits is null and it shouldn't be", hits != null);
			NUnit.Framework.Assert.IsTrue("hits Size: " + hits.totalHits + " is not: " + 1, hits
				.totalHits == 1);
			int[] results = new int[1];
			results[0] = 0;
			//hits.scoreDocs[0].doc;
			CheckHits.CheckHitCollector(Random(), query, PayloadHelper.NO_PAYLOAD_FIELD, searcher
				, results);
		}

		internal class BoostingSimilarity : DefaultSimilarity
		{
			public override float QueryNorm(float sumOfSquaredWeights)
			{
				return 1;
			}

			public override float Coord(int overlap, int maxOverlap)
			{
				return 1;
			}

			// TODO: Remove warning after API has been finalized
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
				return 1;
			}

			public override float Idf(long docFreq, long numDocs)
			{
				return 1;
			}

			public override float Tf(float freq)
			{
				return freq == 0 ? 0 : 1;
			}
		}

		internal class FullSimilarity : DefaultSimilarity
		{
			public virtual float ScorePayload(int docId, string fieldName, byte[] payload, int
				 offset, int length)
			{
				//we know it is size 4 here, so ignore the offset/length
				return payload[offset];
			}
		}
	}
}
