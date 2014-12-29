/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using System.IO;
using System.Text;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Search.Spans;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search
{
	public class TestQueryRescorer : LuceneTestCase
	{
		private IndexSearcher GetSearcher(IndexReader r)
		{
			IndexSearcher searcher = NewSearcher(r);
			// We rely on more tokens = lower score:
			searcher.SetSimilarity(new DefaultSimilarity());
			return searcher;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestBasic()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewStringField("id", "0", Field.Store.YES));
			doc.Add(NewTextField("field", "wizard the the the the the oz", Field.Store.NO));
			w.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("id", "1", Field.Store.YES));
			// 1 extra token, but wizard and oz are close;
			doc.Add(NewTextField("field", "wizard oz the the the the the the", Field.Store.NO
				));
			w.AddDocument(doc);
			IndexReader r = w.Reader;
			w.Dispose();
			// Do ordinary BooleanQuery:
			BooleanQuery bq = new BooleanQuery();
			bq.Add(new TermQuery(new Term("field", "wizard")), BooleanClause.Occur.SHOULD);
			bq.Add(new TermQuery(new Term("field", "oz")), BooleanClause.Occur.SHOULD);
			IndexSearcher searcher = GetSearcher(r);
			searcher.SetSimilarity(new DefaultSimilarity());
			TopDocs hits = searcher.Search(bq, 10);
			AreEqual(2, hits.TotalHits);
			AreEqual("0", searcher.Doc(hits.ScoreDocs[0].Doc).Get("id"
				));
			AreEqual("1", searcher.Doc(hits.ScoreDocs[1].Doc).Get("id"
				));
			// Now, resort using PhraseQuery:
			PhraseQuery pq = new PhraseQuery();
			pq.SetSlop(5);
			pq.Add(new Term("field", "wizard"));
			pq.Add(new Term("field", "oz"));
			TopDocs hits2 = QueryRescorer.Rescore(searcher, hits, pq, 2.0, 10);
			// Resorting changed the order:
			AreEqual(2, hits2.TotalHits);
			AreEqual("1", searcher.Doc(hits2.ScoreDocs[0].Doc).Get("id"
				));
			AreEqual("0", searcher.Doc(hits2.ScoreDocs[1].Doc).Get("id"
				));
			// Resort using SpanNearQuery:
			SpanTermQuery t1 = new SpanTermQuery(new Term("field", "wizard"));
			SpanTermQuery t2 = new SpanTermQuery(new Term("field", "oz"));
			SpanNearQuery snq = new SpanNearQuery(new SpanQuery[] { t1, t2 }, 0, true);
			TopDocs hits3 = QueryRescorer.Rescore(searcher, hits, snq, 2.0, 10);
			// Resorting changed the order:
			AreEqual(2, hits3.TotalHits);
			AreEqual("1", searcher.Doc(hits3.ScoreDocs[0].Doc).Get("id"
				));
			AreEqual("0", searcher.Doc(hits3.ScoreDocs[1].Doc).Get("id"
				));
			r.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestCustomCombine()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewStringField("id", "0", Field.Store.YES));
			doc.Add(NewTextField("field", "wizard the the the the the oz", Field.Store.NO));
			w.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("id", "1", Field.Store.YES));
			// 1 extra token, but wizard and oz are close;
			doc.Add(NewTextField("field", "wizard oz the the the the the the", Field.Store.NO
				));
			w.AddDocument(doc);
			IndexReader r = w.Reader;
			w.Dispose();
			// Do ordinary BooleanQuery:
			BooleanQuery bq = new BooleanQuery();
			bq.Add(new TermQuery(new Term("field", "wizard")), BooleanClause.Occur.SHOULD);
			bq.Add(new TermQuery(new Term("field", "oz")), BooleanClause.Occur.SHOULD);
			IndexSearcher searcher = GetSearcher(r);
			TopDocs hits = searcher.Search(bq, 10);
			AreEqual(2, hits.TotalHits);
			AreEqual("0", searcher.Doc(hits.ScoreDocs[0].Doc).Get("id"
				));
			AreEqual("1", searcher.Doc(hits.ScoreDocs[1].Doc).Get("id"
				));
			// Now, resort using PhraseQuery, but with an
			// opposite-world combine:
			PhraseQuery pq = new PhraseQuery();
			pq.SetSlop(5);
			pq.Add(new Term("field", "wizard"));
			pq.Add(new Term("field", "oz"));
			TopDocs hits2 = new _QueryRescorer_146(pq).Rescore(searcher, hits, 10);
			// Resorting didn't change the order:
			AreEqual(2, hits2.TotalHits);
			AreEqual("0", searcher.Doc(hits2.ScoreDocs[0].Doc).Get("id"
				));
			AreEqual("1", searcher.Doc(hits2.ScoreDocs[1].Doc).Get("id"
				));
			r.Dispose();
			dir.Dispose();
		}

		private sealed class _QueryRescorer_146 : QueryRescorer
		{
			public _QueryRescorer_146(Query baseArg1) : base(baseArg1)
			{
			}

			protected override float Combine(float firstPassScore, bool secondPassMatches, float
				 secondPassScore)
			{
				float score = firstPassScore;
				if (secondPassMatches)
				{
					score -= 2.0 * secondPassScore;
				}
				return score;
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestExplain()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewStringField("id", "0", Field.Store.YES));
			doc.Add(NewTextField("field", "wizard the the the the the oz", Field.Store.NO));
			w.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("id", "1", Field.Store.YES));
			// 1 extra token, but wizard and oz are close;
			doc.Add(NewTextField("field", "wizard oz the the the the the the", Field.Store.NO
				));
			w.AddDocument(doc);
			IndexReader r = w.Reader;
			w.Dispose();
			// Do ordinary BooleanQuery:
			BooleanQuery bq = new BooleanQuery();
			bq.Add(new TermQuery(new Term("field", "wizard")), BooleanClause.Occur.SHOULD);
			bq.Add(new TermQuery(new Term("field", "oz")), BooleanClause.Occur.SHOULD);
			IndexSearcher searcher = GetSearcher(r);
			TopDocs hits = searcher.Search(bq, 10);
			AreEqual(2, hits.TotalHits);
			AreEqual("0", searcher.Doc(hits.ScoreDocs[0].Doc).Get("id"
				));
			AreEqual("1", searcher.Doc(hits.ScoreDocs[1].Doc).Get("id"
				));
			// Now, resort using PhraseQuery:
			PhraseQuery pq = new PhraseQuery();
			pq.Add(new Term("field", "wizard"));
			pq.Add(new Term("field", "oz"));
			Rescorer rescorer = new _QueryRescorer_198(pq);
			TopDocs hits2 = rescorer.Rescore(searcher, hits, 10);
			// Resorting changed the order:
			AreEqual(2, hits2.TotalHits);
			AreEqual("1", searcher.Doc(hits2.ScoreDocs[0].Doc).Get("id"
				));
			AreEqual("0", searcher.Doc(hits2.ScoreDocs[1].Doc).Get("id"
				));
			int docID = hits2.ScoreDocs[0].Doc;
			Explanation explain = rescorer.Explain(searcher, searcher.Explain(bq, docID), docID
				);
			string s = explain.ToString();
			IsTrue(s.Contains("TestQueryRescorer$"));
			IsTrue(s.Contains("combined first and second pass score"));
			IsTrue(s.Contains("first pass score"));
			IsTrue(s.Contains("= second pass score"));
			AreEqual(hits2.ScoreDocs[0].score, explain.GetValue(), 0.0f
				);
			docID = hits2.ScoreDocs[1].Doc;
			explain = rescorer.Explain(searcher, searcher.Explain(bq, docID), docID);
			s = explain.ToString();
			IsTrue(s.Contains("TestQueryRescorer$"));
			IsTrue(s.Contains("combined first and second pass score"));
			IsTrue(s.Contains("first pass score"));
			IsTrue(s.Contains("no second pass score"));
			IsFalse(s.Contains("= second pass score"));
			IsTrue(s.Contains("NON-MATCH"));
			AreEqual(hits2.ScoreDocs[1].score, explain.GetValue(), 0.0f
				);
			r.Dispose();
			dir.Dispose();
		}

		private sealed class _QueryRescorer_198 : QueryRescorer
		{
			public _QueryRescorer_198(Query baseArg1) : base(baseArg1)
			{
			}

			protected override float Combine(float firstPassScore, bool secondPassMatches, float
				 secondPassScore)
			{
				float score = firstPassScore;
				if (secondPassMatches)
				{
					score += 2.0 * secondPassScore;
				}
				return score;
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestMissingSecondPassScore()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewStringField("id", "0", Field.Store.YES));
			doc.Add(NewTextField("field", "wizard the the the the the oz", Field.Store.NO));
			w.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("id", "1", Field.Store.YES));
			// 1 extra token, but wizard and oz are close;
			doc.Add(NewTextField("field", "wizard oz the the the the the the", Field.Store.NO
				));
			w.AddDocument(doc);
			IndexReader r = w.Reader;
			w.Dispose();
			// Do ordinary BooleanQuery:
			BooleanQuery bq = new BooleanQuery();
			bq.Add(new TermQuery(new Term("field", "wizard")), BooleanClause.Occur.SHOULD);
			bq.Add(new TermQuery(new Term("field", "oz")), BooleanClause.Occur.SHOULD);
			IndexSearcher searcher = GetSearcher(r);
			TopDocs hits = searcher.Search(bq, 10);
			AreEqual(2, hits.TotalHits);
			AreEqual("0", searcher.Doc(hits.ScoreDocs[0].Doc).Get("id"
				));
			AreEqual("1", searcher.Doc(hits.ScoreDocs[1].Doc).Get("id"
				));
			// Now, resort using PhraseQuery, no slop:
			PhraseQuery pq = new PhraseQuery();
			pq.Add(new Term("field", "wizard"));
			pq.Add(new Term("field", "oz"));
			TopDocs hits2 = QueryRescorer.Rescore(searcher, hits, pq, 2.0, 10);
			// Resorting changed the order:
			AreEqual(2, hits2.TotalHits);
			AreEqual("1", searcher.Doc(hits2.ScoreDocs[0].Doc).Get("id"
				));
			AreEqual("0", searcher.Doc(hits2.ScoreDocs[1].Doc).Get("id"
				));
			// Resort using SpanNearQuery:
			SpanTermQuery t1 = new SpanTermQuery(new Term("field", "wizard"));
			SpanTermQuery t2 = new SpanTermQuery(new Term("field", "oz"));
			SpanNearQuery snq = new SpanNearQuery(new SpanQuery[] { t1, t2 }, 0, true);
			TopDocs hits3 = QueryRescorer.Rescore(searcher, hits, snq, 2.0, 10);
			// Resorting changed the order:
			AreEqual(2, hits3.TotalHits);
			AreEqual("1", searcher.Doc(hits3.ScoreDocs[0].Doc).Get("id"
				));
			AreEqual("0", searcher.Doc(hits3.ScoreDocs[1].Doc).Get("id"
				));
			r.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestRandom()
		{
			Directory dir = NewDirectory();
			int numDocs = AtLeast(1000);
			RandomIndexWriter w = new RandomIndexWriter(Random(), dir);
			int[] idToNum = new int[numDocs];
			int maxValue = TestUtil.NextInt(Random(), 10, 1000000);
			for (int i = 0; i < numDocs; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(NewStringField("id", string.Empty + i, Field.Store.YES));
				int numTokens = TestUtil.NextInt(Random(), 1, 10);
				StringBuilder b = new StringBuilder();
				for (int j = 0; j < numTokens; j++)
				{
					b.Append("a ");
				}
				doc.Add(NewTextField("field", b.ToString(), Field.Store.NO));
				idToNum[i] = Random().Next(maxValue);
				doc.Add(new NumericDocValuesField("num", idToNum[i]));
				w.AddDocument(doc);
			}
			IndexReader r = w.Reader;
			w.Dispose();
			IndexSearcher s = NewSearcher(r);
			int numHits = TestUtil.NextInt(Random(), 1, numDocs);
			bool reverse = Random().NextBoolean();
			//System.out.println("numHits=" + numHits + " reverse=" + reverse);
			TopDocs hits = s.Search(new TermQuery(new Term("field", "a")), numHits);
			TopDocs hits2 = new _QueryRescorer_329(new TestQueryRescorer.FixedScoreQuery(idToNum
				, reverse)).Rescore(s, hits, numHits);
			int[] expected = new int[numHits];
			for (int i_1 = 0; i_1 < numHits; i_1++)
			{
				expected[i_1] = hits.ScoreDocs[i_1].Doc;
			}
			int reverseInt = reverse ? -1 : 1;
			Arrays.Sort(expected, new _IComparer_344(idToNum, r, reverseInt));
			// Tie break by docID, ascending
			bool fail = false;
			for (int i_2 = 0; i_2 < numHits; i_2++)
			{
				//System.out.println("expected=" + expected[i] + " vs " + hits2.ScoreDocs[i].Doc + " v=" + idToNum[Integer.parseInt(r.document(expected[i]).get("id"))]);
				if (expected[i_2] != hits2.ScoreDocs[i_2].Doc)
				{
					//System.out.println("  diff!");
					fail = true;
				}
			}
			IsFalse(fail);
			r.Dispose();
			dir.Dispose();
		}

		private sealed class _QueryRescorer_329 : QueryRescorer
		{
			public _QueryRescorer_329(Query baseArg1) : base(baseArg1)
			{
			}

			protected override float Combine(float firstPassScore, bool secondPassMatches, float
				 secondPassScore)
			{
				return secondPassScore;
			}
		}

		private sealed class _IComparer_344 : IComparer<int>
		{
			public _IComparer_344(int[] idToNum, IndexReader r, int reverseInt)
			{
				this.idToNum = idToNum;
				this.r = r;
				this.reverseInt = reverseInt;
			}

			public int Compare(int a, int b)
			{
				try
				{
					int av = idToNum[System.Convert.ToInt32(r.Document(a).Get("id"))];
					int bv = idToNum[System.Convert.ToInt32(r.Document(b).Get("id"))];
					if (av < bv)
					{
						return -reverseInt;
					}
					else
					{
						if (bv < av)
						{
							return reverseInt;
						}
						else
						{
							return a - b;
						}
					}
				}
				catch (IOException ioe)
				{
					throw new SystemException(ioe);
				}
			}

			private readonly int[] idToNum;

			private readonly IndexReader r;

			private readonly int reverseInt;
		}

		/// <summary>Just assigns score == idToNum[doc("id")] for each doc.</summary>
		/// <remarks>Just assigns score == idToNum[doc("id")] for each doc.</remarks>
		private class FixedScoreQuery : Query
		{
			private readonly int[] idToNum;

			private readonly bool reverse;

			public FixedScoreQuery(int[] idToNum, bool reverse)
			{
				this.idToNum = idToNum;
				this.reverse = reverse;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override Weight CreateWeight(IndexSearcher searcher)
			{
				return new _Weight_391(this);
			}

			private sealed class _Weight_391 : Weight
			{
				public _Weight_391(FixedScoreQuery _enclosing)
				{
					this._enclosing = _enclosing;
				}

				public override Query GetQuery()
				{
					return this._enclosing;
				}

				public override float GetValueForNormalization()
				{
					return 1.0f;
				}

				public override void Normalize(float queryNorm, float topLevelBoost)
				{
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override Scorer Scorer(AtomicReaderContext context, Bits acceptDocs)
				{
					return new _Scorer_410(this, context, null);
				}

				private sealed class _Scorer_410 : Scorer
				{
					public _Scorer_410(_Weight_391 _enclosing, AtomicReaderContext context, Weight baseArg1
						) : base(baseArg1)
					{
						this._enclosing = _enclosing;
						this.context = context;
						this.docID = -1;
					}

					internal int docID;

					public override int DocID
					{
						return this.docID;
					}

					public override int Freq
					{
						return 1;
					}

					public override long Cost()
					{
						return 1;
					}

					public override int NextDoc()
					{
						this.docID++;
						if (this.docID >= ((AtomicReader)context.Reader).MaxDoc)
						{
							return DocIdSetIterator.NO_MORE_DOCS;
						}
						return this.docID;
					}

					public override int Advance(int target)
					{
						this.docID = target;
						return this.docID;
					}

					/// <exception cref="System.IO.IOException"></exception>
					public override float Score()
					{
						int num = this._enclosing._enclosing.idToNum[System.Convert.ToInt32(((AtomicReader
							)context.Reader).Document(this.docID).Get("id"))];
						if (this._enclosing._enclosing.reverse)
						{
							//System.out.println("score doc=" + docID + " num=" + num);
							return num;
						}
						else
						{
							//System.out.println("score doc=" + docID + " num=" + -num);
							return -num;
						}
					}

					private readonly _Weight_391 _enclosing;

					private readonly AtomicReaderContext context;
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override Explanation Explain(AtomicReaderContext context, int doc)
				{
					return null;
				}

				private readonly FixedScoreQuery _enclosing;
			}

			public override void ExtractTerms(ICollection<Term> terms)
			{
			}

			public override string ToString(string field)
			{
				return "FixedScoreQuery " + idToNum.Length + " ids; reverse=" + reverse;
			}

			public override bool Equals(object o)
			{
				if ((o is TestQueryRescorer.FixedScoreQuery) == false)
				{
					return false;
				}
				TestQueryRescorer.FixedScoreQuery other = (TestQueryRescorer.FixedScoreQuery)o;
				return Sharpen.Runtime.FloatToIntBits(GetBoost()) == Sharpen.Runtime.FloatToIntBits
					(other.GetBoost()) && reverse == other.reverse && Arrays.Equals(idToNum, other.idToNum
					);
			}

			public override Query Clone()
			{
				return new TestQueryRescorer.FixedScoreQuery(idToNum, reverse);
			}

			public override int GetHashCode()
			{
				int PRIME = 31;
				int hash = base.GetHashCode();
				if (reverse)
				{
					hash = PRIME * hash + 3623;
				}
				hash = PRIME * hash + Arrays.HashCode(idToNum);
				return hash;
			}
		}
	}
}
