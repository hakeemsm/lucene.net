/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Sandbox.Queries;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using Sharpen;

namespace Lucene.Net.Sandbox.Queries
{
	/// <summary>Implements the classic fuzzy search query.</summary>
	/// <remarks>
	/// Implements the classic fuzzy search query. The similarity measurement
	/// is based on the Levenshtein (edit distance) algorithm.
	/// <p>
	/// Note that, unlike
	/// <see cref="Lucene.Net.Search.FuzzyQuery">Lucene.Net.Search.FuzzyQuery
	/// 	</see>
	/// , this query will silently allow
	/// for a (possibly huge) number of edit distances in comparisons, and may
	/// be extremely slow (comparing every term in the index).
	/// </remarks>
	[System.ObsoleteAttribute(@"Use Lucene.Net.Search.FuzzyQuery instead.")]
	public class SlowFuzzyQuery : MultiTermQuery
	{
		public const float defaultMinSimilarity = LevenshteinAutomata.MAXIMUM_SUPPORTED_DISTANCE;

		public const int defaultPrefixLength = 0;

		public const int defaultMaxExpansions = 50;

		private float minimumSimilarity;

		private int prefixLength;

		private bool termLongEnough = false;

		protected internal Term term;

		/// <summary>
		/// Create a new SlowFuzzyQuery that will match terms with a similarity
		/// of at least <code>minimumSimilarity</code> to <code>term</code>.
		/// </summary>
		/// <remarks>
		/// Create a new SlowFuzzyQuery that will match terms with a similarity
		/// of at least <code>minimumSimilarity</code> to <code>term</code>.
		/// If a <code>prefixLength</code> &gt; 0 is specified, a common prefix
		/// of that length is also required.
		/// </remarks>
		/// <param name="term">the term to search for</param>
		/// <param name="minimumSimilarity">
		/// a value between 0 and 1 to set the required similarity
		/// between the query term and the matching terms. For example, for a
		/// <code>minimumSimilarity</code> of <code>0.5</code> a term of the same length
		/// as the query term is considered similar to the query term if the edit distance
		/// between both terms is less than <code>length(term)*0.5</code>
		/// <p>
		/// Alternatively, if <code>minimumSimilarity</code> is &gt;= 1f, it is interpreted
		/// as a pure Levenshtein edit distance. For example, a value of <code>2f</code>
		/// will match all terms within an edit distance of <code>2</code> from the
		/// query term. Edit distances specified in this way may not be fractional.
		/// </param>
		/// <param name="prefixLength">length of common (non-fuzzy) prefix</param>
		/// <param name="maxExpansions">
		/// the maximum number of terms to match. If this number is
		/// greater than
		/// <see cref="Lucene.Net.Search.BooleanQuery.GetMaxClauseCount()">Lucene.Net.Search.BooleanQuery.GetMaxClauseCount()
		/// 	</see>
		/// when the query is rewritten,
		/// then the maxClauseCount will be used instead.
		/// </param>
		/// <exception cref="System.ArgumentException">
		/// if minimumSimilarity is &gt;= 1 or &lt; 0
		/// or if prefixLength &lt; 0
		/// </exception>
		public SlowFuzzyQuery(Term term, float minimumSimilarity, int prefixLength, int maxExpansions
			) : base(term.Field())
		{
			// javadocs
			// javadocs
			this.term = term;
			if (minimumSimilarity >= 1.0f && minimumSimilarity != (int)minimumSimilarity)
			{
				throw new ArgumentException("fractional edit distances are not allowed");
			}
			if (minimumSimilarity < 0.0f)
			{
				throw new ArgumentException("minimumSimilarity < 0");
			}
			if (prefixLength < 0)
			{
				throw new ArgumentException("prefixLength < 0");
			}
			if (maxExpansions < 0)
			{
				throw new ArgumentException("maxExpansions < 0");
			}
			SetRewriteMethod(new MultiTermQuery.TopTermsScoringBooleanQueryRewrite(maxExpansions
				));
			string text = term.Text();
			int len = text.CodePointCount(0, text.Length);
			if (len > 0 && (minimumSimilarity >= 1f || len > 1.0f / (1.0f - minimumSimilarity
				)))
			{
				this.termLongEnough = true;
			}
			this.minimumSimilarity = minimumSimilarity;
			this.prefixLength = prefixLength;
		}

		/// <summary>
		/// Calls
		/// <see cref="SlowFuzzyQuery(Lucene.Net.Index.Term, float)">SlowFuzzyQuery(term, minimumSimilarity, prefixLength, defaultMaxExpansions)
		/// 	</see>
		/// .
		/// </summary>
		public SlowFuzzyQuery(Term term, float minimumSimilarity, int prefixLength) : this
			(term, minimumSimilarity, prefixLength, defaultMaxExpansions)
		{
		}

		/// <summary>
		/// Calls
		/// <see cref="SlowFuzzyQuery(Lucene.Net.Index.Term, float)">SlowFuzzyQuery(term, minimumSimilarity, 0, defaultMaxExpansions)
		/// 	</see>
		/// .
		/// </summary>
		public SlowFuzzyQuery(Term term, float minimumSimilarity) : this(term, minimumSimilarity
			, defaultPrefixLength, defaultMaxExpansions)
		{
		}

		/// <summary>
		/// Calls
		/// <see cref="SlowFuzzyQuery(Lucene.Net.Index.Term, float)">SlowFuzzyQuery(term, defaultMinSimilarity, 0, defaultMaxExpansions)
		/// 	</see>
		/// .
		/// </summary>
		public SlowFuzzyQuery(Term term) : this(term, defaultMinSimilarity, defaultPrefixLength
			, defaultMaxExpansions)
		{
		}

		/// <summary>Returns the minimum similarity that is required for this query to match.
		/// 	</summary>
		/// <remarks>Returns the minimum similarity that is required for this query to match.
		/// 	</remarks>
		/// <returns>float value between 0.0 and 1.0</returns>
		public virtual float GetMinSimilarity()
		{
			return minimumSimilarity;
		}

		/// <summary>Returns the non-fuzzy prefix length.</summary>
		/// <remarks>
		/// Returns the non-fuzzy prefix length. This is the number of characters at the start
		/// of a term that must be identical (not fuzzy) to the query term if the query
		/// is to match that term.
		/// </remarks>
		public virtual int GetPrefixLength()
		{
			return prefixLength;
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected override TermsEnum GetTermsEnum(Terms terms, AttributeSource atts)
		{
			if (!termLongEnough)
			{
				// can only match if it's exact
				return new SingleTermsEnum(terms.Iterator(null), term.Bytes());
			}
			return new SlowFuzzyTermsEnum(terms, atts, GetTerm(), minimumSimilarity, prefixLength
				);
		}

		/// <summary>Returns the pattern term.</summary>
		/// <remarks>Returns the pattern term.</remarks>
		public virtual Term GetTerm()
		{
			return term;
		}

		public override string ToString(string field)
		{
			StringBuilder buffer = new StringBuilder();
			if (!term.Field().Equals(field))
			{
				buffer.Append(term.Field());
				buffer.Append(":");
			}
			buffer.Append(term.Text());
			buffer.Append('~');
			buffer.Append(float.ToString(minimumSimilarity));
			buffer.Append(ToStringUtils.Boost(GetBoost()));
			return buffer.ToString();
		}

		public override int GetHashCode()
		{
			int prime = 31;
			int result = base.GetHashCode();
			result = prime * result + Sharpen.Runtime.FloatToIntBits(minimumSimilarity);
			result = prime * result + prefixLength;
			result = prime * result + ((term == null) ? 0 : term.GetHashCode());
			return result;
		}

		public override bool Equals(object obj)
		{
			if (this == obj)
			{
				return true;
			}
			if (!base.Equals(obj))
			{
				return false;
			}
			if (GetType() != obj.GetType())
			{
				return false;
			}
			Lucene.Net.Sandbox.Queries.SlowFuzzyQuery other = (Lucene.Net.Sandbox.Queries.SlowFuzzyQuery
				)obj;
			if (Sharpen.Runtime.FloatToIntBits(minimumSimilarity) != Sharpen.Runtime.FloatToIntBits
				(other.minimumSimilarity))
			{
				return false;
			}
			if (prefixLength != other.prefixLength)
			{
				return false;
			}
			if (term == null)
			{
				if (other.term != null)
				{
					return false;
				}
			}
			else
			{
				if (!term.Equals(other.term))
				{
					return false;
				}
			}
			return true;
		}
	}
}
