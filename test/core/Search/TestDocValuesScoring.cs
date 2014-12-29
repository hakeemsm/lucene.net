/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search
{
	/// <summary>Tests the use of indexdocvalues in scoring.</summary>
	/// <remarks>
	/// Tests the use of indexdocvalues in scoring.
	/// In the example, a docvalues field is used as a per-document boost (separate from the norm)
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public class TestDocValuesScoring : LuceneTestCase
	{
		private const float SCORE_EPSILON = 0.001f;

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSimple()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			Field field = NewTextField("foo", string.Empty, Field.Store.NO);
			doc.Add(field);
			Field dvField = new FloatDocValuesField("foo_boost", 0.0F);
			doc.Add(dvField);
			Field field2 = NewTextField("bar", string.Empty, Field.Store.NO);
			doc.Add(field2);
			field.StringValue = "quick brown fox");
			field2.StringValue = "quick brown fox");
			dvField.SetFloatValue(2f);
			// boost x2
			iw.AddDocument(doc);
			field.StringValue = "jumps over lazy brown dog");
			field2.StringValue = "jumps over lazy brown dog");
			dvField.SetFloatValue(4f);
			// boost x4
			iw.AddDocument(doc);
			IndexReader ir = iw.Reader;
			iw.Dispose();
			// no boosting
			IndexSearcher searcher1 = NewSearcher(ir, false);
			Similarity @base = searcher1.GetSimilarity();
			// boosting
			IndexSearcher searcher2 = NewSearcher(ir, false);
			searcher2.SetSimilarity(new _PerFieldSimilarityWrapper_74(@base));
			// in this case, we searched on field "foo". first document should have 2x the score.
			TermQuery tq = new TermQuery(new Term("foo", "quick"));
			QueryUtils.Check(Random(), tq, searcher1);
			QueryUtils.Check(Random(), tq, searcher2);
			TopDocs noboost = searcher1.Search(tq, 10);
			TopDocs boost = searcher2.Search(tq, 10);
			AreEqual(1, noboost.TotalHits);
			AreEqual(1, boost.TotalHits);
			//System.out.println(searcher2.explain(tq, boost.ScoreDocs[0].Doc));
			AreEqual(boost.ScoreDocs[0].score, noboost.ScoreDocs[0].score
				 * 2f, SCORE_EPSILON);
			// this query matches only the second document, which should have 4x the score.
			tq = new TermQuery(new Term("foo", "jumps"));
			QueryUtils.Check(Random(), tq, searcher1);
			QueryUtils.Check(Random(), tq, searcher2);
			noboost = searcher1.Search(tq, 10);
			boost = searcher2.Search(tq, 10);
			AreEqual(1, noboost.TotalHits);
			AreEqual(1, boost.TotalHits);
			AreEqual(boost.ScoreDocs[0].score, noboost.ScoreDocs[0].score
				 * 4f, SCORE_EPSILON);
			// search on on field bar just for kicks, nothing should happen, since we setup
			// our sim provider to only use foo_boost for field foo.
			tq = new TermQuery(new Term("bar", "quick"));
			QueryUtils.Check(Random(), tq, searcher1);
			QueryUtils.Check(Random(), tq, searcher2);
			noboost = searcher1.Search(tq, 10);
			boost = searcher2.Search(tq, 10);
			AreEqual(1, noboost.TotalHits);
			AreEqual(1, boost.TotalHits);
			AreEqual(boost.ScoreDocs[0].score, noboost.ScoreDocs[0].score
				, SCORE_EPSILON);
			ir.Dispose();
			dir.Dispose();
		}

		private sealed class _PerFieldSimilarityWrapper_74 : PerFieldSimilarityWrapper
		{
			public _PerFieldSimilarityWrapper_74(Similarity @base)
			{
				this.@base = @base;
				this.fooSim = new TestDocValuesScoring.BoostingSimilarity(@base, "foo_boost");
			}

			internal readonly Similarity fooSim;

			public override Similarity Get(string field)
			{
				return "foo".Equals(field) ? this.fooSim : @base;
			}

			public override float Coord(int overlap, int maxOverlap)
			{
				return @base.Coord(overlap, maxOverlap);
			}

			public override float QueryNorm(float sumOfSquaredWeights)
			{
				return @base.QueryNorm(sumOfSquaredWeights);
			}

			private readonly Similarity @base;
		}

		/// <summary>
		/// Similarity that wraps another similarity and boosts the final score
		/// according to whats in a docvalues field.
		/// </summary>
		/// <remarks>
		/// Similarity that wraps another similarity and boosts the final score
		/// according to whats in a docvalues field.
		/// </remarks>
		/// <lucene.experimental></lucene.experimental>
		internal class BoostingSimilarity : Similarity
		{
			private readonly Similarity sim;

			private readonly string boostField;

			public BoostingSimilarity(Similarity sim, string boostField)
			{
				this.sim = sim;
				this.boostField = boostField;
			}

			public override long ComputeNorm(FieldInvertState state)
			{
				return sim.ComputeNorm(state);
			}

			public override Similarity.SimWeight ComputeWeight(float queryBoost, CollectionStatistics
				 collectionStats, params TermStatistics[] termStats)
			{
				return sim.ComputeWeight(queryBoost, collectionStats, termStats);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override Similarity.SimScorer SimScorer(Similarity.SimWeight stats, AtomicReaderContext
				 context)
			{
				Similarity.SimScorer sub = sim.SimScorer(stats, context);
				FieldCache.Floats values = FieldCache.DEFAULT.GetFloats(((AtomicReader)context.Reader
					()), boostField, false);
				return new _SimScorer_165(this, values, sub);
			}

			private sealed class _SimScorer_165 : Similarity.SimScorer
			{
				public _SimScorer_165(BoostingSimilarity _enclosing, FieldCache.Floats values, Similarity.SimScorer
					 sub)
				{
					this._enclosing = _enclosing;
					this.values = values;
					this.sub = sub;
				}

				public override float Score(int doc, float freq)
				{
					return values.Get(doc) * sub.Score(doc, freq);
				}

				public override float ComputeSlopFactor(int distance)
				{
					return sub.ComputeSlopFactor(distance);
				}

				public override float ComputePayloadFactor(int doc, int start, int end, BytesRef 
					payload)
				{
					return sub.ComputePayloadFactor(doc, start, end, payload);
				}

				public override Explanation Explain(int doc, Explanation freq)
				{
					Explanation boostExplanation = new Explanation(values.Get(doc), "indexDocValue(" 
						+ this._enclosing.boostField + ")");
					Explanation simExplanation = sub.Explain(doc, freq);
					Explanation expl = new Explanation(boostExplanation.GetValue() * simExplanation.GetValue
						(), "product of:");
					expl.AddDetail(boostExplanation);
					expl.AddDetail(simExplanation);
					return expl;
				}

				private readonly BoostingSimilarity _enclosing;

				private readonly FieldCache.Floats values;

				private readonly Similarity.SimScorer sub;
			}
		}
	}
}
