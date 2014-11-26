/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Facet;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Queries.Function.Docvalues;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Facet.Taxonomy
{
	/// <summary>
	/// Aggregates sum of values from
	/// <see cref="Lucene.Net.Queries.Function.FunctionValues.DoubleVal(int)">Lucene.Net.Queries.Function.FunctionValues.DoubleVal(int)
	/// 	</see>
	/// , for each facet label.
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class TaxonomyFacetSumValueSource : FloatTaxonomyFacets
	{
		private readonly OrdinalsReader ordinalsReader;

		/// <summary>
		/// Aggreggates float facet values from the provided
		/// <see cref="Lucene.Net.Queries.Function.ValueSource">Lucene.Net.Queries.Function.ValueSource
		/// 	</see>
		/// , pulling ordinals using
		/// <see cref="DocValuesOrdinalsReader">DocValuesOrdinalsReader</see>
		/// against the default indexed
		/// facet field
		/// <see cref="Lucene.Net.Facet.FacetsConfig.DEFAULT_INDEX_FIELD_NAME">Lucene.Net.Facet.FacetsConfig.DEFAULT_INDEX_FIELD_NAME
		/// 	</see>
		/// .
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public TaxonomyFacetSumValueSource(TaxonomyReader taxoReader, FacetsConfig config
			, FacetsCollector fc, ValueSource valueSource) : this(new DocValuesOrdinalsReader
			(FacetsConfig.DEFAULT_INDEX_FIELD_NAME), taxoReader, config, fc, valueSource)
		{
		}

		/// <summary>
		/// Aggreggates float facet values from the provided
		/// <see cref="Lucene.Net.Queries.Function.ValueSource">Lucene.Net.Queries.Function.ValueSource
		/// 	</see>
		/// , and pulls ordinals from the
		/// provided
		/// <see cref="OrdinalsReader">OrdinalsReader</see>
		/// .
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public TaxonomyFacetSumValueSource(OrdinalsReader ordinalsReader, TaxonomyReader 
			taxoReader, FacetsConfig config, FacetsCollector fc, ValueSource valueSource) : 
			base(ordinalsReader.GetIndexFieldName(), taxoReader, config)
		{
			this.ordinalsReader = ordinalsReader;
			SumValues(fc.GetMatchingDocs(), fc.GetKeepScores(), valueSource);
		}

		private sealed class FakeScorer : Scorer
		{
			internal float score;

			internal int docID;

			public FakeScorer() : base(null)
			{
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override float Score()
			{
				return score;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Freq()
			{
				throw new NotSupportedException();
			}

			public override int DocID()
			{
				return docID;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int NextDoc()
			{
				throw new NotSupportedException();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Advance(int target)
			{
				throw new NotSupportedException();
			}

			public override long Cost()
			{
				return 0;
			}

			public override Weight GetWeight()
			{
				throw new NotSupportedException();
			}

			public override ICollection<Scorer.ChildScorer> GetChildren()
			{
				throw new NotSupportedException();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void SumValues(IList<FacetsCollector.MatchingDocs> matchingDocs, bool keepScores
			, ValueSource valueSource)
		{
			TaxonomyFacetSumValueSource.FakeScorer scorer = new TaxonomyFacetSumValueSource.FakeScorer
				();
			IDictionary<string, Scorer> context = new Dictionary<string, Scorer>();
			if (keepScores)
			{
				context.Put("scorer", scorer);
			}
			IntsRef scratch = new IntsRef();
			foreach (FacetsCollector.MatchingDocs hits in matchingDocs)
			{
				OrdinalsReader.OrdinalsSegmentReader ords = ordinalsReader.GetReader(hits.context
					);
				int scoresIdx = 0;
				float[] scores = hits.scores;
				FunctionValues functionValues = valueSource.GetValues(context, hits.context);
				DocIdSetIterator docs = hits.bits.Iterator();
				int doc;
				while ((doc = docs.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
				{
					ords.Get(doc, scratch);
					if (keepScores)
					{
						scorer.docID = doc;
						scorer.score = scores[scoresIdx++];
					}
					float value = (float)functionValues.DoubleVal(doc);
					for (int i = 0; i < scratch.length; i++)
					{
						values[scratch.ints[i]] += value;
					}
				}
			}
			Rollup();
		}

		/// <summary>
		/// <see cref="Lucene.Net.Queries.Function.ValueSource">Lucene.Net.Queries.Function.ValueSource
		/// 	</see>
		/// that returns the score for each
		/// hit; use this to aggregate the sum of all hit scores
		/// for each facet label.
		/// </summary>
		public class ScoreValueSource : ValueSource
		{
			/// <summary>Sole constructor.</summary>
			/// <remarks>Sole constructor.</remarks>
			public ScoreValueSource()
			{
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override FunctionValues GetValues(IDictionary context, AtomicReaderContext
				 readerContext)
			{
				Scorer scorer = (Scorer)context.Get("scorer");
				if (scorer == null)
				{
					throw new InvalidOperationException("scores are missing; be sure to pass keepScores=true to FacetsCollector"
						);
				}
				return new _DoubleDocValues_135(scorer, this);
			}

			private sealed class _DoubleDocValues_135 : DoubleDocValues
			{
				public _DoubleDocValues_135(Scorer scorer, ValueSource baseArg1) : base(baseArg1)
				{
					this.scorer = scorer;
				}

				public override double DoubleVal(int document)
				{
					try
					{
						return scorer.Score();
					}
					catch (IOException exception)
					{
						throw new RuntimeException(exception);
					}
				}

				private readonly Scorer scorer;
			}

			public override bool Equals(object o)
			{
				return o == this;
			}

			public override int GetHashCode()
			{
				return Runtime.IdentityHashCode(this);
			}

			public override string Description()
			{
				return "score()";
			}
		}
	}
}
