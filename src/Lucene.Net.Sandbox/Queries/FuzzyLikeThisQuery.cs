/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Index;
using Lucene.Net.Sandbox.Queries;
using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Sandbox.Queries
{
	/// <summary>Fuzzifies ALL terms provided as strings and then picks the best n differentiating terms.
	/// 	</summary>
	/// <remarks>
	/// Fuzzifies ALL terms provided as strings and then picks the best n differentiating terms.
	/// In effect this mixes the behaviour of FuzzyQuery and MoreLikeThis but with special consideration
	/// of fuzzy scoring factors.
	/// This generally produces good results for queries where users may provide details in a number of
	/// fields and have no knowledge of boolean query syntax and also want a degree of fuzzy matching and
	/// a fast query.
	/// For each source term the fuzzy variants are held in a BooleanQuery with no coord factor (because
	/// we are not looking for matches on multiple variants in any one doc). Additionally, a specialized
	/// TermQuery is used for variants and does not use that variant term's IDF because this would favour rarer
	/// terms eg misspellings. Instead, all variants use the same IDF ranking (the one for the source query
	/// term) and this is factored into the variant's boost. If the source query term does not exist in the
	/// index the average IDF of the variants is used.
	/// </remarks>
	public class FuzzyLikeThisQuery : Query
	{
		internal static TFIDFSimilarity sim = new DefaultSimilarity();

		internal Query rewrittenQuery = null;

		internal AList<FuzzyLikeThisQuery.FieldVals> fieldVals = new AList<FuzzyLikeThisQuery.FieldVals
			>();

		internal Analyzer analyzer;

		internal FuzzyLikeThisQuery.ScoreTermQueue q;

		internal int MAX_VARIANTS_PER_TERM = 50;

		internal bool ignoreTF = false;

		private int maxNumTerms;

		// TODO: generalize this query (at least it should not reuse this static sim!
		// a better way might be to convert this into multitermquery rewrite methods.
		// the rewrite method can 'average' the TermContext's term statistics (docfreq,totalTermFreq) 
		// provided to TermQuery, so that the general idea is agnostic to any scoring system...
		public override int GetHashCode()
		{
			int prime = 31;
			int result = base.GetHashCode();
			result = prime * result + ((analyzer == null) ? 0 : analyzer.GetHashCode());
			result = prime * result + ((fieldVals == null) ? 0 : fieldVals.GetHashCode());
			result = prime * result + (ignoreTF ? 1231 : 1237);
			result = prime * result + maxNumTerms;
			return result;
		}

		public override bool Equals(object obj)
		{
			if (this == obj)
			{
				return true;
			}
			if (obj == null)
			{
				return false;
			}
			if (GetType() != obj.GetType())
			{
				return false;
			}
			if (!base.Equals(obj))
			{
				return false;
			}
			Lucene.Net.Sandbox.Queries.FuzzyLikeThisQuery other = (Lucene.Net.Sandbox.Queries.FuzzyLikeThisQuery
				)obj;
			if (analyzer == null)
			{
				if (other.analyzer != null)
				{
					return false;
				}
			}
			else
			{
				if (!analyzer.Equals(other.analyzer))
				{
					return false;
				}
			}
			if (fieldVals == null)
			{
				if (other.fieldVals != null)
				{
					return false;
				}
			}
			else
			{
				if (!fieldVals.Equals(other.fieldVals))
				{
					return false;
				}
			}
			if (ignoreTF != other.ignoreTF)
			{
				return false;
			}
			if (maxNumTerms != other.maxNumTerms)
			{
				return false;
			}
			return true;
		}

		/// <param name="maxNumTerms">The total number of terms clauses that will appear once rewritten as a BooleanQuery
		/// 	</param>
		public FuzzyLikeThisQuery(int maxNumTerms, Analyzer analyzer)
		{
			q = new FuzzyLikeThisQuery.ScoreTermQueue(maxNumTerms);
			this.analyzer = analyzer;
			this.maxNumTerms = maxNumTerms;
		}

		internal class FieldVals
		{
			internal string queryString;

			internal string fieldName;

			internal float minSimilarity;

			internal int prefixLength;

			public FieldVals(FuzzyLikeThisQuery _enclosing, string name, float similarity, int
				 length, string queryString)
			{
				this._enclosing = _enclosing;
				this.fieldName = name;
				this.minSimilarity = similarity;
				this.prefixLength = length;
				this.queryString = queryString;
			}

			public override int GetHashCode()
			{
				int prime = 31;
				int result = 1;
				result = prime * result + ((this.fieldName == null) ? 0 : this.fieldName.GetHashCode
					());
				result = prime * result + Sharpen.Runtime.FloatToIntBits(this.minSimilarity);
				result = prime * result + this.prefixLength;
				result = prime * result + ((this.queryString == null) ? 0 : this.queryString.GetHashCode
					());
				return result;
			}

			public override bool Equals(object obj)
			{
				if (this == obj)
				{
					return true;
				}
				if (obj == null)
				{
					return false;
				}
				if (this.GetType() != obj.GetType())
				{
					return false;
				}
				FuzzyLikeThisQuery.FieldVals other = (FuzzyLikeThisQuery.FieldVals)obj;
				if (this.fieldName == null)
				{
					if (other.fieldName != null)
					{
						return false;
					}
				}
				else
				{
					if (!this.fieldName.Equals(other.fieldName))
					{
						return false;
					}
				}
				if (Sharpen.Runtime.FloatToIntBits(this.minSimilarity) != Sharpen.Runtime.FloatToIntBits
					(other.minSimilarity))
				{
					return false;
				}
				if (this.prefixLength != other.prefixLength)
				{
					return false;
				}
				if (this.queryString == null)
				{
					if (other.queryString != null)
					{
						return false;
					}
				}
				else
				{
					if (!this.queryString.Equals(other.queryString))
					{
						return false;
					}
				}
				return true;
			}

			private readonly FuzzyLikeThisQuery _enclosing;
		}

		/// <summary>Adds user input for "fuzzification"</summary>
		/// <param name="queryString">The string which will be parsed by the analyzer and for which fuzzy variants will be parsed
		/// 	</param>
		/// <param name="minSimilarity">The minimum similarity of the term variants (see FuzzyTermsEnum)
		/// 	</param>
		/// <param name="prefixLength">Length of required common prefix on variant terms (see FuzzyTermsEnum)
		/// 	</param>
		public virtual void AddTerms(string queryString, string fieldName, float minSimilarity
			, int prefixLength)
		{
			fieldVals.AddItem(new FuzzyLikeThisQuery.FieldVals(this, fieldName, minSimilarity
				, prefixLength, queryString));
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void AddTerms(IndexReader reader, FuzzyLikeThisQuery.FieldVals f)
		{
			if (f.queryString == null)
			{
				return;
			}
			Terms terms = MultiFields.GetTerms(reader, f.fieldName);
			if (terms == null)
			{
				return;
			}
			TokenStream ts = analyzer.TokenStream(f.fieldName, f.queryString);
			try
			{
				CharTermAttribute termAtt = ts.AddAttribute<CharTermAttribute>();
				int corpusNumDocs = reader.NumDocs();
				HashSet<string> processedTerms = new HashSet<string>();
				ts.Reset();
				while (ts.IncrementToken())
				{
					string term = termAtt.ToString();
					if (!processedTerms.Contains(term))
					{
						processedTerms.AddItem(term);
						FuzzyLikeThisQuery.ScoreTermQueue variantsQ = new FuzzyLikeThisQuery.ScoreTermQueue
							(MAX_VARIANTS_PER_TERM);
						//maxNum variants considered for any one term
						float minScore = 0;
						Term startTerm = new Term(f.fieldName, term);
						AttributeSource atts = new AttributeSource();
						MaxNonCompetitiveBoostAttribute maxBoostAtt = atts.AddAttribute<MaxNonCompetitiveBoostAttribute
							>();
						SlowFuzzyTermsEnum fe = new SlowFuzzyTermsEnum(terms, atts, startTerm, f.minSimilarity
							, f.prefixLength);
						//store the df so all variants use same idf
						int df = reader.DocFreq(startTerm);
						int numVariants = 0;
						int totalVariantDocFreqs = 0;
						BytesRef possibleMatch;
						BoostAttribute boostAtt = fe.Attributes().AddAttribute<BoostAttribute>();
						while ((possibleMatch = fe.Next()) != null)
						{
							numVariants++;
							totalVariantDocFreqs += fe.DocFreq();
							float score = boostAtt.GetBoost();
							if (variantsQ.Size() < MAX_VARIANTS_PER_TERM || score > minScore)
							{
								FuzzyLikeThisQuery.ScoreTerm st = new FuzzyLikeThisQuery.ScoreTerm(new Term(startTerm
									.Field(), BytesRef.DeepCopyOf(possibleMatch)), score, startTerm);
								variantsQ.InsertWithOverflow(st);
								minScore = variantsQ.Top().score;
							}
							// maintain minScore
							maxBoostAtt.SetMaxNonCompetitiveBoost(variantsQ.Size() >= MAX_VARIANTS_PER_TERM ? 
								minScore : float.NegativeInfinity);
						}
						if (numVariants > 0)
						{
							int avgDf = totalVariantDocFreqs / numVariants;
							if (df == 0)
							{
								//no direct match we can use as df for all variants
								df = avgDf;
							}
							//use avg df of all variants
							// take the top variants (scored by edit distance) and reset the score
							// to include an IDF factor then add to the global queue for ranking
							// overall top query terms
							int size = variantsQ.Size();
							for (int i = 0; i < size; i++)
							{
								FuzzyLikeThisQuery.ScoreTerm st = variantsQ.Pop();
								st.score = (st.score * st.score) * sim.Idf(df, corpusNumDocs);
								q.InsertWithOverflow(st);
							}
						}
					}
				}
				ts.End();
			}
			finally
			{
				IOUtils.CloseWhileHandlingException(ts);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Query Rewrite(IndexReader reader)
		{
			if (rewrittenQuery != null)
			{
				return rewrittenQuery;
			}
			//load up the list of possible terms
			for (Iterator<FuzzyLikeThisQuery.FieldVals> iter = fieldVals.Iterator(); iter.HasNext
				(); )
			{
				FuzzyLikeThisQuery.FieldVals f = iter.Next();
				AddTerms(reader, f);
			}
			//clear the list of fields
			fieldVals.Clear();
			BooleanQuery bq = new BooleanQuery();
			//create BooleanQueries to hold the variants for each token/field pair and ensure it
			// has no coord factor
			//Step 1: sort the termqueries by term/field
			Dictionary<Term, AList<FuzzyLikeThisQuery.ScoreTerm>> variantQueries = new Dictionary
				<Term, AList<FuzzyLikeThisQuery.ScoreTerm>>();
			int size = q.Size();
			for (int i = 0; i < size; i++)
			{
				FuzzyLikeThisQuery.ScoreTerm st = q.Pop();
				AList<FuzzyLikeThisQuery.ScoreTerm> l = variantQueries.Get(st.fuzziedSourceTerm);
				if (l == null)
				{
					l = new AList<FuzzyLikeThisQuery.ScoreTerm>();
					variantQueries.Put(st.fuzziedSourceTerm, l);
				}
				l.AddItem(st);
			}
			//Step 2: Organize the sorted termqueries into zero-coord scoring boolean queries
			for (Iterator<AList<FuzzyLikeThisQuery.ScoreTerm>> iter_1 = variantQueries.Values
				.Iterator(); iter_1.HasNext(); )
			{
				AList<FuzzyLikeThisQuery.ScoreTerm> variants = iter_1.Next();
				if (variants.Count == 1)
				{
					//optimize where only one selected variant
					FuzzyLikeThisQuery.ScoreTerm st = variants[0];
					Query tq = ignoreTF ? new ConstantScoreQuery(new TermQuery(st.term)) : new TermQuery
						(st.term, 1);
					tq.SetBoost(st.score);
					// set the boost to a mix of IDF and score
					bq.Add(tq, BooleanClause.Occur.SHOULD);
				}
				else
				{
					BooleanQuery termVariants = new BooleanQuery(true);
					//disable coord and IDF for these term variants
					for (Iterator<FuzzyLikeThisQuery.ScoreTerm> iterator2 = variants.Iterator(); iterator2
						.HasNext(); )
					{
						FuzzyLikeThisQuery.ScoreTerm st = iterator2.Next();
						// found a match
						Query tq = ignoreTF ? new ConstantScoreQuery(new TermQuery(st.term)) : new TermQuery
							(st.term, 1);
						tq.SetBoost(st.score);
						// set the boost using the ScoreTerm's score
						termVariants.Add(tq, BooleanClause.Occur.SHOULD);
					}
					// add to query                    
					bq.Add(termVariants, BooleanClause.Occur.SHOULD);
				}
			}
			// add to query
			//TODO possible alternative step 3 - organize above booleans into a new layer of field-based
			// booleans with a minimum-should-match of NumFields-1?
			bq.SetBoost(GetBoost());
			this.rewrittenQuery = bq;
			return bq;
		}

		private class ScoreTerm
		{
			public Term term;

			public float score;

			internal Term fuzziedSourceTerm;

			public ScoreTerm(Term term, float score, Term fuzziedSourceTerm)
			{
				//Holds info for a fuzzy term variant - initially score is set to edit distance (for ranking best
				// term variants) then is reset with IDF for use in ranking against all other
				// terms/fields
				this.term = term;
				this.score = score;
				this.fuzziedSourceTerm = fuzziedSourceTerm;
			}
		}

		private class ScoreTermQueue : PriorityQueue<FuzzyLikeThisQuery.ScoreTerm>
		{
			public ScoreTermQueue(int size) : base(size)
			{
			}

			protected override bool LessThan(FuzzyLikeThisQuery.ScoreTerm termA, FuzzyLikeThisQuery.ScoreTerm
				 termB)
			{
				if (termA.score == termB.score)
				{
					return termA.term.CompareTo(termB.term) > 0;
				}
				else
				{
					return termA.score < termB.score;
				}
			}
		}

		public override string ToString(string field)
		{
			return null;
		}

		public virtual bool IsIgnoreTF()
		{
			return ignoreTF;
		}

		public virtual void SetIgnoreTF(bool ignoreTF)
		{
			this.ignoreTF = ignoreTF;
		}
	}
}
