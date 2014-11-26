/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Index;
using Lucene.Net.Sandbox.Queries;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Sandbox.Queries
{
	/// <summary>
	/// Potentially slow fuzzy TermsEnum for enumerating all terms that are similar
	/// to the specified filter term.
	/// </summary>
	/// <remarks>
	/// Potentially slow fuzzy TermsEnum for enumerating all terms that are similar
	/// to the specified filter term.
	/// <p> If the minSimilarity or maxEdits is greater than the Automaton's
	/// allowable range, this backs off to the classic (brute force)
	/// fuzzy terms enum method by calling FuzzyTermsEnum's getAutomatonEnum.
	/// </p>
	/// <p>Term enumerations are always ordered by
	/// <see cref="Lucene.Net.Search.FuzzyTermsEnum.GetComparator()">Lucene.Net.Search.FuzzyTermsEnum.GetComparator()
	/// 	</see>
	/// .  Each term in the enumeration is
	/// greater than all that precede it.</p>
	/// </remarks>
	[System.ObsoleteAttribute(@"Use Lucene.Net.Search.FuzzyTermsEnum instead."
		)]
	public sealed class SlowFuzzyTermsEnum : FuzzyTermsEnum
	{
		/// <exception cref="System.IO.IOException"></exception>
		public SlowFuzzyTermsEnum(Terms terms, AttributeSource atts, Term term, float minSimilarity
			, int prefixLength) : base(terms, atts, term, minSimilarity, prefixLength, false
			)
		{
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected override void MaxEditDistanceChanged(BytesRef lastTerm, int maxEdits, bool
			 init)
		{
			TermsEnum newEnum = GetAutomatonEnum(maxEdits, lastTerm);
			if (newEnum != null)
			{
				SetEnum(newEnum);
			}
			else
			{
				if (init)
				{
					SetEnum(new SlowFuzzyTermsEnum.LinearFuzzyTermsEnum(this));
				}
			}
		}

		/// <summary>Implement fuzzy enumeration with linear brute force.</summary>
		/// <remarks>Implement fuzzy enumeration with linear brute force.</remarks>
		private class LinearFuzzyTermsEnum : FilteredTermsEnum
		{
			private int[] d;

			private int[] p;

			private readonly int[] text;

			private readonly BoostAttribute boostAtt = this.Attributes().AddAttribute<BoostAttribute
				>();

			/// <summary>
			/// Constructor for enumeration of all terms from specified <code>reader</code> which share a prefix of
			/// length <code>prefixLength</code> with <code>term</code> and which have a fuzzy similarity &gt;
			/// <code>minSimilarity</code>.
			/// </summary>
			/// <remarks>
			/// Constructor for enumeration of all terms from specified <code>reader</code> which share a prefix of
			/// length <code>prefixLength</code> with <code>term</code> and which have a fuzzy similarity &gt;
			/// <code>minSimilarity</code>.
			/// <p>
			/// After calling the constructor the enumeration is already pointing to the first
			/// valid term if such a term exists.
			/// </remarks>
			/// <exception cref="System.IO.IOException">If there is a low-level I/O error.</exception>
			public LinearFuzzyTermsEnum(SlowFuzzyTermsEnum _enclosing) : base(this._enclosing
				.terms.Iterator(null))
			{
				this._enclosing = _enclosing;
				// this is the text, minus the prefix
				this.text = new int[this._enclosing.termLength - this._enclosing.realPrefixLength
					];
				System.Array.Copy(this._enclosing.termText, this._enclosing.realPrefixLength, this
					.text, 0, this.text.Length);
				string prefix = UnicodeUtil.NewString(this._enclosing.termText, 0, this._enclosing
					.realPrefixLength);
				this.prefixBytesRef = new BytesRef(prefix);
				this.d = new int[this.text.Length + 1];
				this.p = new int[this.text.Length + 1];
				this.SetInitialSeekTerm(this.prefixBytesRef);
			}

			private readonly BytesRef prefixBytesRef;

			private readonly IntsRef utf32 = new IntsRef(20);

			// used for unicode conversion from BytesRef byte[] to int[]
			/// <summary>
			/// <p>The termCompare method in FuzzyTermEnum uses Levenshtein distance to
			/// calculate the distance between the given term and the comparing term.
			/// </summary>
			/// <remarks>
			/// <p>The termCompare method in FuzzyTermEnum uses Levenshtein distance to
			/// calculate the distance between the given term and the comparing term.
			/// </p>
			/// <p>If the minSimilarity is &gt;= 1.0, this uses the maxEdits as the comparison.
			/// Otherwise, this method uses the following logic to calculate similarity.
			/// <pre>
			/// similarity = 1 - ((float)distance / (float) (prefixLength + Math.min(textlen, targetlen)));
			/// </pre>
			/// where distance is the Levenshtein distance for the two words.
			/// </p>
			/// </remarks>
			protected sealed override FilteredTermsEnum.AcceptStatus Accept(BytesRef term)
			{
				if (StringHelper.StartsWith(term, this.prefixBytesRef))
				{
					UnicodeUtil.UTF8toUTF32(term, this.utf32);
					int distance = this.CalcDistance(this.utf32.ints, this._enclosing.realPrefixLength
						, this.utf32.length - this._enclosing.realPrefixLength);
					//Integer.MIN_VALUE is the sentinel that Levenshtein stopped early
					if (distance == int.MinValue)
					{
						return FilteredTermsEnum.AcceptStatus.NO;
					}
					//no need to calc similarity, if raw is true and distance > maxEdits
					if (this._enclosing.raw == true && distance > this._enclosing.maxEdits)
					{
						return FilteredTermsEnum.AcceptStatus.NO;
					}
					float similarity = this.CalcSimilarity(distance, (this.utf32.length - this._enclosing
						.realPrefixLength), this.text.Length);
					//if raw is true, then distance must also be <= maxEdits by now
					//given the previous if statement
					if (this._enclosing.raw == true || (this._enclosing.raw == false && similarity > 
						this._enclosing.minSimilarity))
					{
						this.boostAtt.SetBoost((similarity - this._enclosing.minSimilarity) * this._enclosing
							.scale_factor);
						return FilteredTermsEnum.AcceptStatus.YES;
					}
					else
					{
						return FilteredTermsEnum.AcceptStatus.NO;
					}
				}
				else
				{
					return FilteredTermsEnum.AcceptStatus.END;
				}
			}

			/// <summary>
			/// <p>calcDistance returns the Levenshtein distance between the query term
			/// and the target term.</p>
			/// <p>Embedded within this algorithm is a fail-fast Levenshtein distance
			/// algorithm.
			/// </summary>
			/// <remarks>
			/// <p>calcDistance returns the Levenshtein distance between the query term
			/// and the target term.</p>
			/// <p>Embedded within this algorithm is a fail-fast Levenshtein distance
			/// algorithm.  The fail-fast algorithm differs from the standard Levenshtein
			/// distance algorithm in that it is aborted if it is discovered that the
			/// minimum distance between the words is greater than some threshold.
			/// <p>Levenshtein distance (also known as edit distance) is a measure of similarity
			/// between two strings where the distance is measured as the number of character
			/// deletions, insertions or substitutions required to transform one string to
			/// the other string.
			/// </remarks>
			/// <param name="target">the target word or phrase</param>
			/// <param name="offset">the offset at which to start the comparison</param>
			/// <param name="length">the length of what's left of the string to compare</param>
			/// <returns>
			/// the number of edits or Integer.MIN_VALUE if the edit distance is
			/// greater than maxDistance.
			/// </returns>
			private int CalcDistance(int[] target, int offset, int length)
			{
				int m = length;
				int n = this.text.Length;
				if (n == 0)
				{
					//we don't have anything to compare.  That means if we just add
					//the letters for m we get the new word
					return m;
				}
				if (m == 0)
				{
					return n;
				}
				int maxDistance = this.CalculateMaxDistance(m);
				if (maxDistance < Math.Abs(m - n))
				{
					//just adding the characters of m to n or vice-versa results in
					//too many edits
					//for example "pre" length is 3 and "prefixes" length is 8.  We can see that
					//given this optimal circumstance, the edit distance cannot be less than 5.
					//which is 8-3 or more precisely Math.abs(3-8).
					//if our maximum edit distance is 4, then we can discard this word
					//without looking at it.
					return int.MinValue;
				}
				// init matrix d
				for (int i = 0; i <= n; ++i)
				{
					this.p[i] = i;
				}
				// start computing edit distance
				for (int j = 1; j <= m; ++j)
				{
					// iterates through target
					int bestPossibleEditDistance = m;
					int t_j = target[offset + j - 1];
					// jth character of t
					this.d[0] = j;
					for (int i_1 = 1; i_1 <= n; ++i_1)
					{
						// iterates through text
						// minimum of cell to the left+1, to the top+1, diagonally left and up +(0|1)
						if (t_j != this.text[i_1 - 1])
						{
							this.d[i_1] = Math.Min(Math.Min(this.d[i_1 - 1], this.p[i_1]), this.p[i_1 - 1]) +
								 1;
						}
						else
						{
							this.d[i_1] = Math.Min(Math.Min(this.d[i_1 - 1] + 1, this.p[i_1] + 1), this.p[i_1
								 - 1]);
						}
						bestPossibleEditDistance = Math.Min(bestPossibleEditDistance, this.d[i_1]);
					}
					//After calculating row i, the best possible edit distance
					//can be found by found by finding the smallest value in a given column.
					//If the bestPossibleEditDistance is greater than the max distance, abort.
					if (j > maxDistance && bestPossibleEditDistance > maxDistance)
					{
						//equal is okay, but not greater
						//the closest the target can be to the text is just too far away.
						//this target is leaving the party early.
						return int.MinValue;
					}
					// copy current distance counts to 'previous row' distance counts: swap p and d
					int[] _d = this.p;
					this.p = this.d;
					this.d = _d;
				}
				// our last action in the above loop was to switch d and p, so p now
				// actually has the most recent cost counts
				return this.p[n];
			}

			private float CalcSimilarity(int edits, int m, int n)
			{
				// this will return less than 0.0 when the edit distance is
				// greater than the number of characters in the shorter word.
				// but this was the formula that was previously used in FuzzyTermEnum,
				// so it has not been changed (even though minimumSimilarity must be
				// greater than 0.0)
				return 1.0f - ((float)edits / (float)(this._enclosing.realPrefixLength + Math.Min
					(n, m)));
			}

			/// <summary>
			/// The max Distance is the maximum Levenshtein distance for the text
			/// compared to some other value that results in score that is
			/// better than the minimum similarity.
			/// </summary>
			/// <remarks>
			/// The max Distance is the maximum Levenshtein distance for the text
			/// compared to some other value that results in score that is
			/// better than the minimum similarity.
			/// </remarks>
			/// <param name="m">the length of the "other value"</param>
			/// <returns>the maximum levenshtein distance that we care about</returns>
			private int CalculateMaxDistance(int m)
			{
				return this._enclosing.raw ? this._enclosing.maxEdits : Math.Min(this._enclosing.
					maxEdits, (int)((1 - this._enclosing.minSimilarity) * (Math.Min(this.text.Length
					, m) + this._enclosing.realPrefixLength)));
			}

			private readonly SlowFuzzyTermsEnum _enclosing;
		}
	}
}
